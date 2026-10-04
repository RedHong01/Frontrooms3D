// FrontRoomsMetalGlassRT.mm — desktop Metal hardware ray-traced glass reflections for FrontRooms (G14 P0 + P1).
//
// Design: Documentation/research/glass/rt/10_rt_glass_design.md. Implementation notes: rt/20_implementation.md.
//
// What runs where
//   Main thread (C#, FrontRoomsGlassRT*): fills plugin-owned upload buffers and pushes ops (mesh add/release/refit,
//     textures, materials, camera targets, one frame packet per traced camera) into a mutex-protected queue. It never
//     touches Metal scene state and never waits on the GPU.
//   Render thread (IssuePluginEventAndData, event 1): pops the ops up to the frame packet it was issued for, then
//     encodes into UNITY'S CURRENT command buffer (EndCurrentCommandEncoder first, never commits):
//       gather (non-readable meshes) -> acceleration structures (budgeted BLAS builds, refits, compaction, this
//       frame's TLAS from a 3-slot ring) -> event barrier -> trace (+ Ultra adaptive rays) -> resolve -> event barrier.
//     A completion handler on Unity's command buffer publishes the done serial, GPU timestamps, stats and
//     compacted sizes, and fails closed on any command-buffer error.
//   Device init (render thread): capability probe, asynchronous MSL compile, then a one-time GPU layout self-test.
//
// Fail closed: until the kernels are compiled and the layout test passed, and after any GPU error, no trace is
// encoded and the camera's output texture is cleared (the glass then keeps its zone cube; C# also keeps weight 0).
#import <Metal/Metal.h>
#import <Foundation/Foundation.h>
#include <mach/mach_time.h>
#include <algorithm>
#include <atomic>
#include <cmath>
#include <cstring>
#include <deque>
#include <memory>
#include <mutex>
#include <string>
#include <unordered_map>
#include <vector>

#include "IUnityInterface.h"
#include "IUnityGraphics.h"
#include "IUnityGraphicsMetal.h"
#include "FRGlassRTShared.h"
#include "FRGlassRTScene.h"
#include "FRGlassRTLayout.h"
#include "FRGlassRTEmbedded.h"

namespace
{
// ------------------------------------------------------------------ capabilities (FRGlassRT_QueryCaps)
enum : int
{
    CapPluginLoaded = 1 << 0,
    CapDevice = 1 << 1,
    CapRaytracing = 1 << 2,    // MTLDevice.supportsRaytracing (also true on M1/M2: software traversal)
    CapApple9 = 1 << 3,        // MTLGPUFamilyApple9: M3/M4, hardware ray tracing
    CapMetal3 = 1 << 4,
    CapResidencySets = 1 << 5, // macOS 15+
    CapCompileStarted = 1 << 6,
    CapKernelReady = 1 << 7,
    CapLayoutOK = 1 << 8,
    CapLayoutFailed = 1 << 9,
    CapCompileFailed = 1 << 10,
    CapGpuError = 1 << 11,
    CapOS14 = 1 << 12,         // indirect instance descriptors (every Apple9 Mac ships with macOS 14+)
    CapCounters = 1 << 13,     // stage-boundary GPU timestamps
};
std::atomic<int> g_Caps{ 0 };

// ------------------------------------------------------------------ stats (FRGlassRT_GetStat ids, C# FrontRoomsGlassRTStat)
enum Stat : int
{
    StEvents = 0, StTraceEvents, StSkippedNotReady, StRingBusy, StTlasInstances, StMeshes, StMeshesPending, StBlasBuiltTotal,
    StBlasBytes, StGeometryBytes, StCompacted, StGpuMsAS, StGpuMsTrace, StGpuMsResolve, StGpuMsTotal, StEncodeMs,
    StEncodeMsMax, StGlassPx, StMissPx, StHitPx, StLayerPx, StAAPx, StShadowRays, StCbErrors,
    StCompileMs, StResidencyMode, StHazardDepth, StHazardOut, StTrianglesLast, StBuildsLast, StTimerFrameGpuMs, StDoneSerial,
    StEventSerial, StRetired, StTimestampMode, StGpuMsCB, StLayoutOK, StSkippedNoFrame, StClears, StOpsApplied,
    StMeshesUploaded, StCounterNsPerTick, StTargetsBytes, StLastFrameNumber, StWarmEvents, StPoolBusy, StCount = 64
};
std::mutex g_StatMutex;
double g_Stats[StCount];
void SetStat(int i, double v) { std::lock_guard<std::mutex> l(g_StatMutex); g_Stats[i] = v; }
void AddStat(int i, double v) { std::lock_guard<std::mutex> l(g_StatMutex); g_Stats[i] += v; }
double GetStatValue(int i) { std::lock_guard<std::mutex> l(g_StatMutex); return g_Stats[i]; }

std::mutex g_ErrorMutex;
std::string g_LastError;
void SetError(const std::string& e)
{
    std::lock_guard<std::mutex> l(g_ErrorMutex);
    g_LastError = e;
    NSLog(@"[FRGlassRT] %s", e.c_str());
}

// ------------------------------------------------------------------ Unity interfaces
IUnityInterfaces* s_Unity = nullptr;
IUnityGraphics* s_Graphics = nullptr;
IUnityGraphicsMetalV2* s_Metal = nullptr;
id<MTLDevice> g_Device;

// ------------------------------------------------------------------ pipelines
enum { PTrace = 0, PTraceAA, PResolve, PParity, PGatherV, PGatherI, PLayout, PCount };
const char* kKernelNames[PCount] = { "fr_trace", "fr_trace_aa", "fr_resolve", "fr_parity", "fr_gather_vertices", "fr_gather_indices", "fr_layout" };
std::mutex g_PsoMutex;
id<MTLComputePipelineState> g_Pso[PCount];
std::atomic<int> g_PsoPending{ 0 };
double g_CompileStart = 0;

// ------------------------------------------------------------------ op queue (main -> render thread)
struct FramePacket
{
    int32_t cam = -1;
    int32_t frameNumber = 0;
    FRFrame frame{};
    std::vector<FRInstanceIn> instances;
    std::vector<FRLamp> lamps;
    id<MTLTexture> env;
};

enum class OpType { AddMesh, ReleaseMesh, RefitMesh, SetTexture, SetMaterials, CameraTargets, FreeCamera, Frame, Reset };
struct Op
{
    OpType type;
    uint64_t seq = 0;
    int32_t key = -1;
    std::unique_ptr<FRMeshUpload> upload;
    id<MTLBuffer> buffer;
    id<MTLTexture> texture;
    uint32_t first = 0;
    std::vector<FRMaterial> materials;
    int32_t cam = -1;
    FRCameraTargets targets;
    std::unique_ptr<FramePacket> frame;
};
std::mutex g_OpMutex;
std::deque<std::unique_ptr<Op>> g_Ops;
uint64_t g_OpSeq = 0;

uint64_t Push(std::unique_ptr<Op> op)
{
    std::lock_guard<std::mutex> l(g_OpMutex);
    op->seq = ++g_OpSeq;
    uint64_t s = op->seq;
    g_Ops.push_back(std::move(op));
    return s;
}

// ------------------------------------------------------------------ render-thread state
FRGlassRTScene g_Scene;
bool g_SceneReady = false;
std::unordered_map<int32_t, std::unique_ptr<FRCameraState>> g_Cameras;
id<MTLFence> g_FenceAS, g_FenceTrace;
id<MTLEvent> g_Event;
uint64_t g_EventValue = 0;
uint64_t g_Serial = 0;
std::atomic<uint64_t> g_DoneSerial{ 0 };
constexpr uint32_t kPool = 16;
id<MTLCounterSampleBuffer> g_Counters[kPool];
id<MTLBuffer> g_StatsBuf[kPool];
id<MTLBuffer> g_CompactBuf[kPool];
id<MTLBuffer> g_StatsOverflow;   // stats of a frame whose pool slot was busy (discarded)
uint64_t g_PoolSerial[kPool];
id<MTLTexture> g_DummyTex, g_DummyCube;
bool g_LayoutQueued = false;
FRLayoutBuffers g_Layout;
double g_EncodeMax = 0;
uint64_t g_TimestampCpu0 = 0, g_TimestampGpu0 = 0;
std::atomic<double> g_NsPerTick{ 0.0 };
mach_timebase_info_data_t g_Timebase;
std::atomic<double> g_TimerStart{ 0.0 };

void AtomicMax(std::atomic<uint64_t>& a, uint64_t v)
{
    uint64_t cur = a.load();
    while (v > cur && !a.compare_exchange_weak(cur, v)) {}
}

double MachToMs(uint64_t ticks) { return (double)ticks * g_Timebase.numer / g_Timebase.denom / 1.0e6; }

void StartCompile()
{
    g_Caps.fetch_or(CapCompileStarted);
    g_CompileStart = CFAbsoluteTimeGetCurrent();
    MTLCompileOptions* options = [MTLCompileOptions new];
    options.languageVersion = MTLLanguageVersion3_0;
    options.fastMathEnabled = YES;
    id<MTLDevice> device = g_Device;
    [device newLibraryWithSource:[NSString stringWithUTF8String:kFRGlassRTMSL] options:options
               completionHandler:^(id<MTLLibrary> library, NSError* error) {
        if (!library)
        {
            g_Caps.fetch_or(CapCompileFailed);
            SetError(std::string("MSL compile failed: ") + (error ? error.localizedDescription.UTF8String : "?"));
            return;
        }
        g_PsoPending.store(PCount);
        for (int i = 0; i < PCount; i++)
        {
            id<MTLFunction> fn = [library newFunctionWithName:[NSString stringWithUTF8String:kKernelNames[i]]];
            if (!fn)
            {
                g_Caps.fetch_or(CapCompileFailed);
                SetError(std::string("kernel missing: ") + kKernelNames[i]);
                return;
            }
            [device newComputePipelineStateWithFunction:fn completionHandler:^(id<MTLComputePipelineState> pso, NSError* perr) {
                if (!pso)
                {
                    g_Caps.fetch_or(CapCompileFailed);
                    SetError(std::string("pipeline failed: ") + kKernelNames[i] + " " + (perr ? perr.localizedDescription.UTF8String : ""));
                    return;
                }
                {
                    std::lock_guard<std::mutex> l(g_PsoMutex);
                    g_Pso[i] = pso;
                }
                if (g_PsoPending.fetch_sub(1) == 1)
                {
                    SetStat(StCompileMs, (CFAbsoluteTimeGetCurrent() - g_CompileStart) * 1000.0);
                    g_Caps.fetch_or(CapKernelReady);
                }
            }];
        }
    }];
}

id<MTLComputePipelineState> Pso(int i)
{
    std::lock_guard<std::mutex> l(g_PsoMutex);
    return g_Pso[i];
}

void InitDevice()
{
    if (!s_Metal || !s_Graphics || s_Graphics->GetRenderer() != kUnityGfxRendererMetal) return;
    id<MTLDevice> device = s_Metal->MetalDevice();
    if (!device) return;
    g_Device = device;
    mach_timebase_info(&g_Timebase);
    int caps = CapDevice;
    if (device.supportsRaytracing) caps |= CapRaytracing;
    if ([device supportsFamily:MTLGPUFamilyApple9]) caps |= CapApple9;
    if ([device supportsFamily:MTLGPUFamilyMetal3]) caps |= CapMetal3;
    if (@available(macOS 14.0, *)) caps |= CapOS14;
    if (@available(macOS 15.0, *)) caps |= CapResidencySets;
    g_Caps.fetch_or(caps);
    if (!(caps & CapRaytracing) || !(caps & CapApple9) || !(caps & CapMetal3) || !(caps & CapOS14)) return;   // fail closed (M1/M2: P2 tier)

    std::string err;
    if (!g_Scene.Init(device, err)) { SetError(err); return; }
    g_SceneReady = true;
    SetStat(StResidencyMode, g_Scene.UsesResidencySet() ? 1.0 : 2.0);
    g_FenceAS = [device newFence];
    g_FenceTrace = [device newFence];
    g_Event = [device newEvent];
    for (uint32_t i = 0; i < kPool; i++)
    {
        g_StatsBuf[i] = [device newBufferWithLength:4 * FR_STAT_COUNT options:MTLResourceStorageModeShared];
        g_CompactBuf[i] = [device newBufferWithLength:4 * 512 options:MTLResourceStorageModeShared];
        g_PoolSerial[i] = 0;
    }
    g_StatsOverflow = [device newBufferWithLength:4 * FR_STAT_COUNT options:MTLResourceStorageModePrivate];
    // stage-boundary GPU timestamps
    if ([device supportsCounterSampling:MTLCounterSamplingPointAtStageBoundary])
    {
        id<MTLCounterSet> timestampSet = nil;
        for (id<MTLCounterSet> s in device.counterSets) if ([s.name isEqualToString:MTLCommonCounterSetTimestamp]) timestampSet = s;
        if (timestampSet)
        {
            bool ok = true;
            for (uint32_t i = 0; i < kPool && ok; i++)
            {
                MTLCounterSampleBufferDescriptor* d = [MTLCounterSampleBufferDescriptor new];
                d.counterSet = timestampSet;
                d.storageMode = MTLStorageModeShared;
                d.sampleCount = 8;
                NSError* e = nil;
                g_Counters[i] = [device newCounterSampleBufferWithDescriptor:d error:&e];
                ok = g_Counters[i] != nil;
            }
            if (ok) g_Caps.fetch_or(CapCounters);
            else for (auto& c : g_Counters) c = nil;
        }
    }
    SetStat(StTimestampMode, (g_Caps.load() & CapCounters) ? 1.0 : 2.0);
    MTLTimestamp cpu = 0, gpu = 0;
    [device sampleTimestamps:&cpu gpuTimestamp:&gpu];
    g_TimestampCpu0 = cpu; g_TimestampGpu0 = gpu;
    MTLTextureDescriptor* td = [MTLTextureDescriptor texture2DDescriptorWithPixelFormat:MTLPixelFormatRGBA16Float width:1 height:1 mipmapped:NO];
    td.usage = MTLTextureUsageShaderRead | MTLTextureUsageShaderWrite;
    td.storageMode = MTLStorageModePrivate;
    g_DummyTex = [device newTextureWithDescriptor:td];
    MTLTextureDescriptor* cd = [MTLTextureDescriptor textureCubeDescriptorWithPixelFormat:MTLPixelFormatRGBA16Float size:1 mipmapped:NO];
    cd.usage = MTLTextureUsageShaderRead;
    cd.storageMode = MTLStorageModePrivate;
    g_DummyCube = [device newTextureWithDescriptor:cd];
    StartCompile();
}

void ShutdownDevice()
{
    {
        std::lock_guard<std::mutex> l(g_OpMutex);
        g_Ops.clear();
    }
    g_Cameras.clear();
    if (g_SceneReady) g_Scene.Shutdown();
    g_SceneReady = false;
    {
        std::lock_guard<std::mutex> l(g_PsoMutex);
        for (auto& p : g_Pso) p = nil;
    }
    for (uint32_t i = 0; i < kPool; i++) { g_Counters[i] = nil; g_StatsBuf[i] = nil; g_CompactBuf[i] = nil; }
    g_StatsOverflow = nil;
    g_FenceAS = nil; g_FenceTrace = nil; g_Event = nil; g_DummyTex = nil; g_DummyCube = nil;
    g_Device = nil;
    g_LayoutQueued = false;
    g_Caps.store(CapPluginLoaded);
}

void OnGraphicsDeviceEvent(UnityGfxDeviceEventType type)
{
    if (type == kUnityGfxDeviceEventInitialize) { if (!g_Device) InitDevice(); }
    else if (type == kUnityGfxDeviceEventShutdown) ShutdownDevice();
}

// A render pass that only clears: works before the kernels exist, so a stale reflection is never shown.
void ClearTexture(id<MTLCommandBuffer> cb, id<MTLTexture> tex)
{
    if (!tex) return;
    MTLRenderPassDescriptor* rp = [MTLRenderPassDescriptor renderPassDescriptor];
    rp.colorAttachments[0].texture = tex;
    rp.colorAttachments[0].loadAction = MTLLoadActionClear;
    rp.colorAttachments[0].clearColor = MTLClearColorMake(0, 0, 0, 0);
    rp.colorAttachments[0].storeAction = MTLStoreActionStore;
    id<MTLRenderCommandEncoder> e = [cb renderCommandEncoderWithDescriptor:rp];
    e.label = @"FRGlassRT clear";
    [e endEncoding];
    AddStat(StClears, 1);
}

void Barrier(id<MTLCommandBuffer> cb)
{
    uint64_t v = ++g_EventValue;
    [cb encodeSignalEvent:g_Event value:v];
    [cb encodeWaitForEvent:g_Event value:v];
}

void ApplyOp(Op& op, uint64_t serial, uint64_t forSeq, std::unique_ptr<FramePacket>& frame)
{
    AddStat(StOpsApplied, 1);
    switch (op.type)
    {
        case OpType::AddMesh: g_Scene.AddMesh(std::move(op.upload), serial); break;
        case OpType::ReleaseMesh: g_Scene.ReleaseMesh(op.key, serial); break;
        case OpType::RefitMesh: g_Scene.RefitMesh(op.key, op.buffer, serial); break;
        case OpType::SetTexture: g_Scene.SetTexture(op.first, op.texture, serial); break;
        case OpType::SetMaterials: g_Scene.SetMaterials(op.first, op.materials); break;
        case OpType::Reset: g_Scene.Reset(serial); break;
        case OpType::CameraTargets:
        {
            auto& c = g_Cameras[op.cam];
            if (!c) { c = std::make_unique<FRCameraState>(); c->cameraId = op.cam; }
            c->t = op.targets;
            // new targets hold undefined memory: the first traced frame resolves the whole screen
            c->prevRect = simd_make_uint4(0, 0, c->t.width, c->t.height);
            if (@available(macOS 10.15, *))
            {
                if (c->t.depth) SetStat(StHazardDepth, (double)c->t.depth.hazardTrackingMode);
                if (c->t.out) SetStat(StHazardOut, (double)c->t.out.hazardTrackingMode);
            }
            break;
        }
        case OpType::FreeCamera:
        {
            auto it = g_Cameras.find(op.cam);
            if (it != g_Cameras.end()) g_Cameras.erase(it);   // ARC releases; in-flight command buffers retain what they bound
            break;
        }
        case OpType::Frame:
            if (op.seq == forSeq) frame = std::move(op.frame);   // older packets nobody traced are dropped
            break;
    }
}

void UpdateTimestampCalibration()
{
    if (!g_Device) return;
    MTLTimestamp cpu = 0, gpu = 0;
    [g_Device sampleTimestamps:&cpu gpuTimestamp:&gpu];
    if (gpu > g_TimestampGpu0 + 1000 && cpu > g_TimestampCpu0)
    {
        // MTLTimestamp CPU values are nanoseconds (measured on the M3 Max: 200 ms sleep = 2.03e8 CPU and GPU units)
        double cpuNs = (double)(cpu - g_TimestampCpu0);
        g_NsPerTick.store(cpuNs / (double)(gpu - g_TimestampGpu0));
        SetStat(StCounterNsPerTick, g_NsPerTick.load());
    }
}

void TraceEvent(uint64_t seq)
{
    uint64_t t0 = mach_absolute_time();
    AddStat(StEvents, 1);
    if (!g_Device || !s_Metal || !g_SceneReady) { AddStat(StSkippedNotReady, 1); return; }
    static uint32_t calibrationTick = 0;
    if (g_NsPerTick.load() <= 0.0 || (++calibrationTick & 63) == 0) UpdateTimestampCalibration();
    id<MTLCommandBuffer> cb = s_Metal->CurrentCommandBuffer();
    if (!cb) { AddStat(StSkippedNotReady, 1); return; }
    s_Metal->EndCurrentCommandEncoder();

    std::vector<std::unique_ptr<Op>> ops;
    {
        std::lock_guard<std::mutex> l(g_OpMutex);
        while (!g_Ops.empty() && g_Ops.front()->seq <= seq) { ops.push_back(std::move(g_Ops.front())); g_Ops.pop_front(); }
    }
    uint64_t serial = ++g_Serial;
    uint64_t done = g_DoneSerial.load();
    g_Scene.Purge(done);
    g_Scene.DrainCompletions();
    std::unique_ptr<FramePacket> frame;
    for (auto& op : ops) ApplyOp(*op, serial, seq, frame);
    SetStat(StEventSerial, (double)serial);

    int caps = g_Caps.load();
    bool kernels = (caps & CapKernelReady) != 0;
    if (kernels && !g_LayoutQueued && FRLayoutCreate(g_Device, g_Layout))
    {
        g_LayoutQueued = true;
        id<MTLComputeCommandEncoder> le = [cb computeCommandEncoder];
        le.label = @"FRGlassRT layout self-test";
        FRLayoutEncode(le, Pso(PLayout), g_Layout);
        [le endEncoding];
        [cb addCompletedHandler:^(id<MTLCommandBuffer> c) {
            std::string mismatch = c.status == MTLCommandBufferStatusCompleted ? FRLayoutCheck(g_Layout) : std::string("layout self-test command buffer failed");
            if (mismatch.empty()) { g_Caps.fetch_or(CapLayoutOK); SetStat(StLayoutOK, 1); }
            else { g_Caps.fetch_or(CapLayoutFailed); SetError(mismatch); SetStat(StLayoutOK, -1); }
        }];
    }

    // Warm-up packet (cam < 0): no camera traces this frame (no receiver in view, or RT still starting), but the scene
    // keeps building: budgeted BLAS builds, compactions and deferred releases run anyway, so the first traced frame
    // after a corridor never pays for a whole chunk's BLASes at once.
    if (frame && frame->cam < 0)
    {
        bool warmReady = kernels && (caps & CapLayoutOK) && !(caps & CapGpuError) && !(caps & CapLayoutFailed);
        uint32_t wpi = (uint32_t)(serial % kPool);
        AddStat(StWarmEvents, 1);
        if (warmReady && g_PoolSerial[wpi] <= done)
        {
            g_PoolSerial[wpi] = serial;
            g_Scene.AttachResidency(cb);
            __block std::vector<id> keepAlive;
            g_Scene.EncodeGathers(cb, Pso(PGatherV), Pso(PGatherI), serial, keepAlive);
            __block std::vector<std::pair<int32_t, uint32_t>> compactQueries;
            static FRCameraState warmCamera;
            static const std::vector<FRInstanceIn> noInstances;
            static const std::vector<FRLamp> noLamps;
            uint32_t ic = 0;
            bool rb = false;
            g_Scene.EncodeAccelerationStructures(cb, warmCamera, noInstances, noLamps, frame->frameNumber, serial, done, g_FenceAS, nil, 0,
                                                 g_CompactBuf[wpi], compactQueries, ic, rb);
            SetStat(StBuildsLast, g_Scene.LastBuilds());
            SetStat(StTrianglesLast, g_Scene.LastTriangles());
            SetStat(StMeshes, g_Scene.MeshCount());
            SetStat(StMeshesPending, g_Scene.PendingCount());
            SetStat(StBlasBuiltTotal, (double)g_Scene.BuiltTotal());
            SetStat(StBlasBytes, (double)g_Scene.BlasBytes());
            SetStat(StGeometryBytes, (double)g_Scene.GeometryBytes());
            SetStat(StCompacted, g_Scene.CompactedCount());
            SetStat(StRetired, g_Scene.RetiredCount());
            id<MTLBuffer> compactBuf = g_CompactBuf[wpi];
            [cb addCompletedHandler:^(id<MTLCommandBuffer> c) {
                if (c.status == MTLCommandBufferStatusError)
                {
                    g_Caps.fetch_or(CapGpuError);
                    AddStat(StCbErrors, 1);
                    SetError(std::string("command buffer error (warm-up): ") + (c.error ? c.error.localizedDescription.UTF8String : "?") + " (RT off for this session)");
                }
                const uint32_t* sizes = (const uint32_t*)compactBuf.contents;
                for (auto& q : compactQueries) g_Scene.CompleteCompaction(q.first, sizes[q.second]);
                keepAlive.clear();
                AtomicMax(g_DoneSerial, serial);
            }];
        }
        else [cb addCompletedHandler:^(id<MTLCommandBuffer>) { AtomicMax(g_DoneSerial, serial); }];
        double encodeMs = MachToMs(mach_absolute_time() - t0);
        SetStat(StEncodeMs, encodeMs);
        return;
    }

    FRCameraState* cam = nullptr;
    if (frame)
    {
        auto it = g_Cameras.find(frame->cam);
        if (it != g_Cameras.end()) cam = it->second.get();
    }
    bool ready = kernels && (caps & CapLayoutOK) && !(caps & CapGpuError) && !(caps & CapLayoutFailed);
    uint32_t pi = (uint32_t)(serial % kPool);
    if (!frame || !cam || !cam->t.out || !cam->t.depth || !cam->t.normal || !cam->t.raw || !cam->t.aux || !ready)
    {
        if (!frame) AddStat(StSkippedNoFrame, 1);
        else AddStat(StSkippedNotReady, 1);
        if (cam && cam->t.out) { ClearTexture(cb, cam->t.out); cam->prevRect = simd_make_uint4(0, 0, 0, 0); }
        [cb addCompletedHandler:^(id<MTLCommandBuffer>) { AtomicMax(g_DoneSerial, serial); }];
        return;
    }
    SetStat(StLastFrameNumber, frame->frameNumber);
    // The stats / timer / compaction pool slot of this serial may still be in flight when the CPU runs far ahead of
    // the GPU (several renders in one frame): trace anyway, without timers, stats or compaction queries for it.
    bool poolFree = g_PoolSerial[pi] <= done;
    if (poolFree) g_PoolSerial[pi] = serial;
    else AddStat(StPoolBusy, 1);
    id<MTLCounterSampleBuffer> counters = poolFree ? g_Counters[pi] : nil;

    g_Scene.AttachResidency(cb);
    __block std::vector<id> keepAlive;
    g_Scene.EncodeGathers(cb, Pso(PGatherV), Pso(PGatherI), serial, keepAlive);
    __block std::vector<std::pair<int32_t, uint32_t>> compactQueries;
    uint32_t instanceCount = 0;
    bool ringBusy = false;
    int slotIndex = g_Scene.EncodeAccelerationStructures(cb, *cam, frame->instances, frame->lamps, frame->frameNumber, serial, done,
                                                         g_FenceAS, counters, 0, poolFree ? g_CompactBuf[pi] : nil, compactQueries, instanceCount, ringBusy);
    if (ringBusy) AddStat(StRingBusy, 1);
    SetStat(StTlasInstances, instanceCount);
    SetStat(StBuildsLast, g_Scene.LastBuilds());
    SetStat(StTrianglesLast, g_Scene.LastTriangles());

    id<MTLBuffer> statsBuf = poolFree ? g_StatsBuf[pi] : g_StatsOverflow;
    id<MTLBuffer> compactBuf = g_CompactBuf[pi];
    bool traced = false;
    if (slotIndex >= 0 && instanceCount > 0)
    {
        FRCameraState::Slot& slot = cam->slots[slotIndex];
        FRFrame F = frame->frame;
        F.counts.w = instanceCount;
        F.counts.x = std::min<uint32_t>(F.counts.x, (uint32_t)std::min<size_t>(frame->lamps.size(), FRGlassRTScene::kMaxLamps));
        id<MTLTexture> env = frame->env;
        if (!env || env.textureType != MTLTextureTypeCube) { env = g_DummyCube; F.envInfo.y = 0; }
        F.envInfo.w = (float)std::max<NSUInteger>(env.mipmapLevelCount, 1) - 1.0f;
        uint32_t x0 = std::min<uint32_t>(F.rect.x, cam->t.width), y0 = std::min<uint32_t>(F.rect.y, cam->t.height);
        uint32_t x1 = std::min<uint32_t>(F.rect.z, cam->t.width), y1 = std::min<uint32_t>(F.rect.w, cam->t.height);
        simd_uint4 cur = simd_make_uint4(x0, y0, x1, y1);
        simd_uint4 pr = cam->prevRect;
        if (pr.z > pr.x && pr.w > pr.y)
        {
            x0 = std::min(x0, pr.x); y0 = std::min(y0, pr.y);
            x1 = std::max(x1, std::min<uint32_t>(pr.z, cam->t.width)); y1 = std::max(y1, std::min<uint32_t>(pr.w, cam->t.height));
        }
        cam->prevRect = cur;
        F.rect = simd_make_uint4(x0, y0, x1, y1);
        if (x1 > x0 && y1 > y0)
        {
            if (poolFree) std::memset(statsBuf.contents, 0, statsBuf.length);
            Barrier(cb);   // (c) Unity's prepass writes -> our reads, whatever hazard mode Unity's textures use
            MTLComputePassDescriptor* cpd = [MTLComputePassDescriptor computePassDescriptor];
            if (counters)
            {
                cpd.sampleBufferAttachments[0].sampleBuffer = counters;
                cpd.sampleBufferAttachments[0].startOfEncoderSampleIndex = 2;
                cpd.sampleBufferAttachments[0].endOfEncoderSampleIndex = 3;
            }
            id<MTLComputeCommandEncoder> enc = [cb computeCommandEncoderWithDescriptor:cpd];
            enc.label = @"FRGlassRT trace";
            [enc waitForFence:g_FenceAS];
            g_Scene.UseResources(enc);
            bool parity = F.counts.z == FR_DEBUG_PARITY;
            [enc setComputePipelineState:Pso(parity ? PParity : PTrace)];
            [enc setBytes:&F length:sizeof(FRFrame) atIndex:0];
            [enc setAccelerationStructure:slot.tlas atBufferIndex:1];
            [enc setBuffer:slot.records offset:0 atIndex:2];
            [enc setBuffer:g_Scene.MeshTable() offset:0 atIndex:3];
            [enc setBuffer:g_Scene.MaterialTable() offset:0 atIndex:4];
            [enc setBuffer:g_Scene.TextureTable() offset:0 atIndex:5];
            [enc setBuffer:slot.lamps offset:0 atIndex:6];
            [enc setBuffer:statsBuf offset:0 atIndex:7];
            [enc setTexture:env atIndex:4];
            MTLSize tg = MTLSizeMake(8, 8, 1);
            if (parity)
            {
                [enc setTexture:cam->t.raw atIndex:2];
                [enc setTexture:(cam->t.ids ? cam->t.ids : g_DummyTex) atIndex:3];
                [enc dispatchThreads:MTLSizeMake(cam->t.width, cam->t.height, 1) threadsPerThreadgroup:tg];
                [enc updateFence:g_FenceTrace];
                [enc endEncoding];
            }
            else
            {
                [enc setTexture:cam->t.depth atIndex:0];
                [enc setTexture:cam->t.normal atIndex:1];
                [enc setTexture:cam->t.raw atIndex:2];
                [enc setTexture:cam->t.aux atIndex:3];
                [enc setTexture:((F.counts.z == FR_DEBUG_HITID && cam->t.ids) ? cam->t.ids : g_DummyTex) atIndex:5];
                MTLSize grid = MTLSizeMake(x1 - x0, y1 - y0, 1);
                [enc dispatchThreads:grid threadsPerThreadgroup:tg];
                bool ultraAA = F.counts.y >= 2 && cam->t.rawAA && F.blur.w > 0.0f;
                if (ultraAA)
                {
                    [enc memoryBarrierWithScope:MTLBarrierScopeTextures | MTLBarrierScopeBuffers];
                    [enc setComputePipelineState:Pso(PTraceAA)];
                    [enc setTexture:cam->t.raw atIndex:2];
                    [enc setTexture:cam->t.rawAA atIndex:3];
                    [enc dispatchThreads:grid threadsPerThreadgroup:tg];
                }
                [enc updateFence:g_FenceTrace];
                [enc endEncoding];

                MTLComputePassDescriptor* rpd = [MTLComputePassDescriptor computePassDescriptor];
                if (counters)
                {
                    rpd.sampleBufferAttachments[0].sampleBuffer = counters;
                    rpd.sampleBufferAttachments[0].startOfEncoderSampleIndex = 4;
                    rpd.sampleBufferAttachments[0].endOfEncoderSampleIndex = 5;
                }
                id<MTLComputeCommandEncoder> res = [cb computeCommandEncoderWithDescriptor:rpd];
                res.label = @"FRGlassRT resolve";
                [res waitForFence:g_FenceTrace];
                [res setComputePipelineState:Pso(PResolve)];
                [res setBytes:&F length:sizeof(FRFrame) atIndex:0];
                [res setTexture:(ultraAA ? cam->t.rawAA : cam->t.raw) atIndex:0];
                [res setTexture:cam->t.aux atIndex:1];
                [res setTexture:cam->t.depth atIndex:2];
                [res setTexture:cam->t.normal atIndex:3];
                [res setTexture:cam->t.out atIndex:4];
                [res dispatchThreads:grid threadsPerThreadgroup:tg];
                [res endEncoding];
            }
            Barrier(cb);   // (f) our writes -> Unity's transparent pass
            traced = true;
            AddStat(StTraceEvents, 1);
        }
    }
    if (!traced) { ClearTexture(cb, cam->t.out); cam->prevRect = simd_make_uint4(0, 0, 0, 0); }

    SetStat(StMeshes, g_Scene.MeshCount());
    SetStat(StMeshesPending, g_Scene.PendingCount());
    SetStat(StBlasBuiltTotal, (double)g_Scene.BuiltTotal());
    SetStat(StBlasBytes, (double)g_Scene.BlasBytes());
    SetStat(StGeometryBytes, (double)g_Scene.GeometryBytes());
    SetStat(StCompacted, g_Scene.CompactedCount());
    SetStat(StRetired, g_Scene.RetiredCount());

    bool haveCounters = counters != nil && traced;
    bool parityMode = frame->frame.counts.z == FR_DEBUG_PARITY;
    [cb addCompletedHandler:^(id<MTLCommandBuffer> c) {
        if (c.status == MTLCommandBufferStatusError)
        {
            g_Caps.fetch_or(CapGpuError);
            AddStat(StCbErrors, 1);
            SetError(std::string("command buffer error: ") + (c.error ? c.error.localizedDescription.UTF8String : "?") + " (RT off for this session)");
        }
        SetStat(StGpuMsCB, (c.GPUEndTime - c.GPUStartTime) * 1000.0);
        if (traced && poolFree)
        {
            const uint32_t* s = (const uint32_t*)statsBuf.contents;
            SetStat(StGlassPx, s[FR_STAT_GLASS_PX]);
            SetStat(StMissPx, s[FR_STAT_MISS_PX]);
            SetStat(StHitPx, s[FR_STAT_HIT_PX]);
            SetStat(StLayerPx, s[FR_STAT_LAYER_PX]);
            SetStat(StAAPx, s[FR_STAT_AA_PX]);
            SetStat(StShadowRays, s[FR_STAT_SHADOW_RAYS]);
        }
        if (haveCounters)
        {
            NSData* data = [counters resolveCounterRange:NSMakeRange(0, parityMode ? 4 : 6)];
            double nsPerTick = g_NsPerTick.load();
            if (data && data.length >= sizeof(MTLCounterResultTimestamp) * 4 && nsPerTick > 0)
            {
                const MTLCounterResultTimestamp* ts = (const MTLCounterResultTimestamp*)data.bytes;
                auto ms = [&](int a, int b) -> double {
                    if (ts[a].timestamp == MTLCounterErrorValue || ts[b].timestamp == MTLCounterErrorValue || ts[b].timestamp < ts[a].timestamp) return -1.0;
                    return (double)(ts[b].timestamp - ts[a].timestamp) * nsPerTick / 1.0e6;
                };
                double as = ms(0, 1), tr = ms(2, 3), rs = parityMode ? 0.0 : ms(4, 5);
                SetStat(StGpuMsAS, as);
                SetStat(StGpuMsTrace, tr);
                SetStat(StGpuMsResolve, rs);
                if (as >= 0 && tr >= 0 && rs >= 0) SetStat(StGpuMsTotal, as + tr + rs);
            }
        }
        const uint32_t* sizes = (const uint32_t*)compactBuf.contents;
        for (auto& q : compactQueries) g_Scene.CompleteCompaction(q.first, sizes[q.second]);
        keepAlive.clear();
        AtomicMax(g_DoneSerial, serial);
        SetStat(StDoneSerial, (double)g_DoneSerial.load());
    }];
    double encodeMs = MachToMs(mach_absolute_time() - t0);
    g_EncodeMax = std::max(g_EncodeMax, encodeMs);
    SetStat(StEncodeMs, encodeMs);
    SetStat(StEncodeMsMax, g_EncodeMax);
}

// Events 2/3 bracket a camera render for the harness: whole-frame GPU time of Unity's command buffer(s).
void TimerEvent(bool begin)
{
    if (!s_Metal) return;
    id<MTLCommandBuffer> cb = s_Metal->CurrentCommandBuffer();
    if (!cb) return;
    if (begin) [cb addCompletedHandler:^(id<MTLCommandBuffer> c) { g_TimerStart.store(c.GPUStartTime); }];
    else [cb addCompletedHandler:^(id<MTLCommandBuffer> c) {
        double s = g_TimerStart.load();
        if (s > 0 && c.GPUEndTime >= s) SetStat(StTimerFrameGpuMs, (c.GPUEndTime - s) * 1000.0);
    }];
}

void UNITY_INTERFACE_API OnRenderEvent(int eventId, void* data)
{
    @autoreleasepool
    {
        if (eventId == 1) TraceEvent((uint64_t)(uintptr_t)data);
        else if (eventId == 2) TimerEvent(true);
        else if (eventId == 3) TimerEvent(false);
    }
}

id<MTLBuffer> NewShared(uint64_t bytes)
{
    if (!g_Device) return nil;
    return [g_Device newBufferWithLength:std::max<uint64_t>(bytes, 16) options:MTLResourceStorageModeShared];
}

uint64_t Align16(uint64_t v) { return (v + 15) & ~15ull; }
} // namespace

extern "C"
{

UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API UnityPluginLoad(IUnityInterfaces* interfaces)
{
    s_Unity = interfaces;
    s_Graphics = interfaces ? interfaces->Get<IUnityGraphics>() : nullptr;
    s_Metal = interfaces ? interfaces->Get<IUnityGraphicsMetalV2>() : nullptr;
    g_Caps.fetch_or(CapPluginLoaded);
    if (s_Graphics)
    {
        s_Graphics->RegisterDeviceEventCallback(OnGraphicsDeviceEvent);
        OnGraphicsDeviceEvent(kUnityGfxDeviceEventInitialize);
    }
}

UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API UnityPluginUnload()
{
    if (s_Graphics) s_Graphics->UnregisterDeviceEventCallback(OnGraphicsDeviceEvent);
    s_Metal = nullptr;
    s_Graphics = nullptr;
    s_Unity = nullptr;
}

UNITY_INTERFACE_EXPORT int UNITY_INTERFACE_API FRGlassRT_QueryCaps() { return g_Caps.load(); }

UNITY_INTERFACE_EXPORT int UNITY_INTERFACE_API FRGlassRT_CopyLastError(char* buffer, int length)
{
    std::lock_guard<std::mutex> l(g_ErrorMutex);
    if (!buffer || length <= 0) return (int)g_LastError.size();
    int n = std::min<int>(length - 1, (int)g_LastError.size());
    std::memcpy(buffer, g_LastError.data(), n);
    buffer[n] = 0;
    return n;
}

UNITY_INTERFACE_EXPORT double UNITY_INTERFACE_API FRGlassRT_GetStat(int id)
{
    if (id < 0 || id >= StCount) return 0;
    return GetStatValue(id);
}

UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API FRGlassRT_ResetStatMax() { g_EncodeMax = 0; }

UNITY_INTERFACE_EXPORT UnityRenderingEventAndData UNITY_INTERFACE_API FRGlassRT_GetRenderEventFunc() { return OnRenderEvent; }

// ---- cameras
UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API FRGlassRT_SetCameraTargets(int cam, void* depth, void* normal, void* raw, void* aux,
                                                                           void* rawAA, void* out, void* ids, int width, int height)
{
    auto op = std::make_unique<Op>();
    op->type = OpType::CameraTargets;
    op->cam = cam;
    op->targets.depth = (__bridge id<MTLTexture>)depth;
    op->targets.normal = (__bridge id<MTLTexture>)normal;
    op->targets.raw = (__bridge id<MTLTexture>)raw;
    op->targets.aux = (__bridge id<MTLTexture>)aux;
    op->targets.rawAA = (__bridge id<MTLTexture>)rawAA;
    op->targets.out = (__bridge id<MTLTexture>)out;
    op->targets.ids = (__bridge id<MTLTexture>)ids;
    op->targets.width = (uint32_t)std::max(width, 0);
    op->targets.height = (uint32_t)std::max(height, 0);
    Push(std::move(op));
}

UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API FRGlassRT_FreeCamera(int cam)
{
    auto op = std::make_unique<Op>();
    op->type = OpType::FreeCamera;
    op->cam = cam;
    Push(std::move(op));
}

// ---- geometry
// CPU path (readable meshes): positions (3 floats per vertex), attributes (FRVertexAttr), 32-bit indices with
// base vertex applied, submesh starts/counts (empty submeshes already removed by C#). Called on the main thread:
// the copy goes into a new plugin-owned buffer here; the render thread only builds the BLAS.
UNITY_INTERFACE_EXPORT int UNITY_INTERFACE_API FRGlassRT_UploadMeshCPU(int key, const float* positions, const FRVertexAttr* attrs, int vertexCount,
                                                                      const uint32_t* indices, int indexCount, const int* subStart,
                                                                      const int* subCount, int submeshCount, int deforming, int notBeforeFrame)
{
    if (key < 0 || !positions || !attrs || !indices || vertexCount <= 0 || indexCount <= 0 || submeshCount <= 0 || !subStart || !subCount) return -1;
    auto u = std::make_unique<FRMeshUpload>();
    u->key = key;
    u->vertexCount = (uint32_t)vertexCount;
    u->indexCount = (uint32_t)indexCount;
    uint64_t posBytes = 12ull * vertexCount;
    u->attrOffset = Align16(posBytes);
    u->indexOffset = Align16(u->attrOffset + sizeof(FRVertexAttr) * (uint64_t)vertexCount);
    u->subOffset = Align16(u->indexOffset + 4ull * indexCount);
    uint64_t total = u->subOffset + 4ull * submeshCount;
    u->buffer = NewShared(total);
    if (!u->buffer) return -2;
    u->buffer.label = @"FRGlassRT mesh";
    uint8_t* base = (uint8_t*)u->buffer.contents;
    std::memcpy(base, positions, posBytes);
    std::memcpy(base + u->attrOffset, attrs, sizeof(FRVertexAttr) * (size_t)vertexCount);
    std::memcpy(base + u->indexOffset, indices, 4ull * indexCount);
    uint32_t* subs = (uint32_t*)(base + u->subOffset);
    for (int k = 0; k < submeshCount; k++)
    {
        if (subStart[k] < 0 || subCount[k] < 3 || subStart[k] + subCount[k] > indexCount) return -3;
        subs[k] = (uint32_t)subStart[k];
        u->subStart.push_back((uint32_t)subStart[k]);
        u->subCount.push_back((uint32_t)subCount[k]);
    }
    u->deforming = deforming != 0;
    u->notBeforeFrame = notBeforeFrame;
    auto op = std::make_unique<Op>();
    op->type = OpType::AddMesh;
    op->key = key;
    op->upload = std::move(u);
    Push(std::move(op));
    AddStat(StMeshesUploaded, 1);
    return 0;
}

// GPU path (non-readable meshes, e.g. the kit FBX): Unity's native vertex streams and index buffer, fetched by C# once
// per Mesh per session; the render thread gathers them into a plugin-owned buffer (fr_gather_*) and releases Unity's
// buffers when that command buffer completes.
UNITY_INTERFACE_EXPORT int UNITY_INTERFACE_API FRGlassRT_UploadMeshGPU(int key, void* stream0, void* stream1, void* stream2, void* stream3,
                                                                      const FRGatherVertexArgsCPU* vertexArgs, void* indexBuffer,
                                                                      const FRGatherIndexArgsCPU* indexArgs, int submeshCount, int indexCount)
{
    if (key < 0 || !stream0 || !vertexArgs || !indexBuffer || !indexArgs || submeshCount <= 0 || indexCount <= 0) return -1;
    auto u = std::make_unique<FRMeshUpload>();
    u->key = key;
    u->gather = true;
    u->vertexCount = vertexArgs->vertexCount;
    u->indexCount = (uint32_t)indexCount;
    uint64_t posBytes = 12ull * u->vertexCount;
    u->attrOffset = Align16(posBytes);
    u->indexOffset = Align16(u->attrOffset + sizeof(FRVertexAttr) * (uint64_t)u->vertexCount);
    u->subOffset = Align16(u->indexOffset + 4ull * indexCount);
    u->buffer = [g_Device newBufferWithLength:std::max<uint64_t>(u->subOffset + 4ull * submeshCount, 16) options:MTLResourceStorageModeShared];
    if (!u->buffer) return -2;
    u->buffer.label = @"FRGlassRT mesh (gathered)";
    uint32_t* subs = (uint32_t*)((uint8_t*)u->buffer.contents + u->subOffset);
    for (int k = 0; k < submeshCount; k++)
    {
        subs[k] = indexArgs[k].dstStart;
        u->subStart.push_back(indexArgs[k].dstStart);
        u->subCount.push_back(indexArgs[k].count);
        u->indexArgs.push_back(indexArgs[k]);
    }
    u->streams[0] = (__bridge id<MTLBuffer>)stream0;
    u->streams[1] = (__bridge id<MTLBuffer>)stream1;
    u->streams[2] = (__bridge id<MTLBuffer>)stream2;
    u->streams[3] = (__bridge id<MTLBuffer>)stream3;
    u->unityIndexBuffer = (__bridge id<MTLBuffer>)indexBuffer;
    u->vertexArgs = *vertexArgs;
    auto op = std::make_unique<Op>();
    op->type = OpType::AddMesh;
    op->key = key;
    op->upload = std::move(u);
    Push(std::move(op));
    AddStat(StMeshesUploaded, 1);
    return 0;
}

// Fracture pieces (destruction contract §4.7): one shared buffer, one mesh key per index range (firstKey + i), all
// BLASes built from that buffer on the render thread, never before notBeforeFrame, within the per-frame budget.
UNITY_INTERFACE_EXPORT int UNITY_INTERFACE_API FRGlassRT_UploadMeshRanges(int firstKey, const float* positions, const FRVertexAttr* attrs, int vertexCount,
                                                                         const uint32_t* indices, int indexCount, const int* rangeStart,
                                                                         const int* rangeCount, int ranges, int notBeforeFrame)
{
    if (firstKey < 0 || !positions || !attrs || !indices || vertexCount <= 0 || indexCount <= 0 || ranges <= 0) return -1;
    uint64_t posBytes = 12ull * vertexCount;
    uint64_t attrOffset = Align16(posBytes);
    uint64_t indexOffset = Align16(attrOffset + sizeof(FRVertexAttr) * (uint64_t)vertexCount);
    uint64_t subOffset = Align16(indexOffset + 4ull * indexCount);
    id<MTLBuffer> buffer = NewShared(subOffset + 4ull * ranges);
    if (!buffer) return -2;
    buffer.label = @"FRGlassRT fracture pieces";
    uint8_t* base = (uint8_t*)buffer.contents;
    std::memcpy(base, positions, posBytes);
    std::memcpy(base + attrOffset, attrs, sizeof(FRVertexAttr) * (size_t)vertexCount);
    std::memcpy(base + indexOffset, indices, 4ull * indexCount);
    uint32_t* subs = (uint32_t*)(base + subOffset);
    for (int r = 0; r < ranges; r++)
    {
        if (rangeStart[r] < 0 || rangeCount[r] < 3 || rangeStart[r] + rangeCount[r] > indexCount) return -3;
        subs[r] = (uint32_t)rangeStart[r];
        auto u = std::make_unique<FRMeshUpload>();
        u->key = firstKey + r;
        u->buffer = buffer;
        u->vertexCount = (uint32_t)vertexCount;
        u->indexCount = (uint32_t)indexCount;
        u->attrOffset = attrOffset;
        u->indexOffset = indexOffset;
        u->range = true;
        u->rangeSubOffset = subOffset + 4ull * r;
        u->subStart.push_back((uint32_t)rangeStart[r]);
        u->subCount.push_back((uint32_t)rangeCount[r]);
        u->notBeforeFrame = notBeforeFrame;
        auto op = std::make_unique<Op>();
        op->type = OpType::AddMesh;
        op->key = u->key;
        op->upload = std::move(u);
        Push(std::move(op));
    }
    AddStat(StMeshesUploaded, ranges);
    return 0;
}

// Deforming meshes only: new positions; the render thread refits into the other half of a ping-pong BLAS pair.
UNITY_INTERFACE_EXPORT int UNITY_INTERFACE_API FRGlassRT_RefitMesh(int key, const float* positions, int vertexCount)
{
    if (key < 0 || !positions || vertexCount <= 0) return -1;
    id<MTLBuffer> b = NewShared(12ull * vertexCount);
    if (!b) return -2;
    std::memcpy(b.contents, positions, 12ull * vertexCount);
    auto op = std::make_unique<Op>();
    op->type = OpType::RefitMesh;
    op->key = key;
    op->buffer = b;
    Push(std::move(op));
    return 0;
}

UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API FRGlassRT_ReleaseMesh(int key)
{
    auto op = std::make_unique<Op>();
    op->type = OpType::ReleaseMesh;
    op->key = key;
    Push(std::move(op));
}

UNITY_INTERFACE_EXPORT int UNITY_INTERFACE_API FRGlassRT_MeshState(int key) { return g_SceneReady ? g_Scene.MeshState(key) : 0; }

UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API FRGlassRT_MeshStates(const int* keys, int count, int* states)
{
    for (int i = 0; i < count; i++) states[i] = g_SceneReady ? g_Scene.MeshState(keys[i]) : 0;
}

// ---- materials and textures
UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API FRGlassRT_SetTexture(int slot, void* texture)
{
    auto op = std::make_unique<Op>();
    op->type = OpType::SetTexture;
    op->first = (uint32_t)std::max(slot, 0);
    op->texture = (__bridge id<MTLTexture>)texture;
    Push(std::move(op));
}

UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API FRGlassRT_SetMaterials(int first, const FRMaterial* materials, int count)
{
    if (!materials || count <= 0 || first < 0) return;
    auto op = std::make_unique<Op>();
    op->type = OpType::SetMaterials;
    op->first = (uint32_t)first;
    op->materials.assign(materials, materials + count);
    Push(std::move(op));
}

UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API FRGlassRT_ResetScene()
{
    auto op = std::make_unique<Op>();
    op->type = OpType::Reset;
    Push(std::move(op));
}

// ---- warm-up: no camera traces this frame, but the render event still applies the queued ops and builds BLASes
UNITY_INTERFACE_EXPORT uint64_t UNITY_INTERFACE_API FRGlassRT_SubmitWarmup(int frameNumber)
{
    auto p = std::make_unique<FramePacket>();
    p->cam = -1;
    p->frameNumber = frameNumber;
    auto op = std::make_unique<Op>();
    op->type = OpType::Frame;
    op->frame = std::move(p);
    return Push(std::move(op));
}

// ---- frame: returns the op sequence number the render event must be issued with (0 = rejected)
UNITY_INTERFACE_EXPORT uint64_t UNITY_INTERFACE_API FRGlassRT_SubmitFrame(int cam, int frameNumber, const FRFrame* frame,
                                                                         const FRInstanceIn* instances, int instanceCount,
                                                                         const FRLamp* lamps, int lampCount, void* envCube)
{
    if (!frame || cam < 0) return 0;
    auto p = std::make_unique<FramePacket>();
    p->cam = cam;
    p->frameNumber = frameNumber;
    p->frame = *frame;
    if (instances && instanceCount > 0) p->instances.assign(instances, instances + std::min<int>(instanceCount, (int)FRGlassRTScene::kMaxInstances));
    if (lamps && lampCount > 0) p->lamps.assign(lamps, lamps + std::min<int>(lampCount, (int)FRGlassRTScene::kMaxLamps));
    p->env = (__bridge id<MTLTexture>)envCube;
    auto op = std::make_unique<Op>();
    op->type = OpType::Frame;
    op->frame = std::move(p);
    return Push(std::move(op));
}

} // extern "C"
