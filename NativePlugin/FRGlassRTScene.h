// FRGlassRTScene.h — the render-thread scene of the Metal ray-traced glass bridge (design 10 §1.4).
//
// Ownership: the plugin owns a COPY of every mesh it traces (positions, normals, UV0, vertex colour R,
// 32-bit indices) in its own MTLBuffers; no BLAS ever points into Unity's buffers (R26). One BLAS per
// mesh key, one geometry descriptor per submesh (R14), compacted a few frames after its build.
//
// Threading: only FRGlassRTScene methods called from the render event touch Metal scene state. The main
// thread (C#) allocates and fills upload buffers, then hands them over through the plugin's op queue.
// Lifetime: everything released goes onto a retire list tagged with the event serial and is freed only
// after the completion handler of that command buffer reported done (R25).
#pragma once
#import <Metal/Metal.h>
#include <atomic>
#include <cstdint>
#include <memory>
#include <mutex>
#include <string>
#include <unordered_map>
#include <vector>
#include "FRGlassRTShared.h"

// C# -> native instance (96 B), FrontRoomsGlassRTNative.InstanceIn.
struct FRInstanceIn
{
    float4 o2w0, o2w1, o2w2;
    int32_t meshKey;
    uint32_t materialBase;
    uint32_t flags;
    uint32_t windowId;
    float4 emission;
    uint32_t mask;
    uint32_t pad0, pad1, pad2;
};
static_assert(sizeof(FRInstanceIn) == 96, "FRInstanceIn layout (C# InstanceIn)");

// Mirrors of the MSL gather argument structs.
struct FRGatherVertexArgsCPU
{
    uint32_t vertexCount;
    uint32_t posStride, posOffset;
    uint32_t nrmStride, nrmOffset, nrmFormat;
    uint32_t uvStride, uvOffset, uvFormat;
    uint32_t colStride, colOffset, colFormat;
};
static_assert(sizeof(FRGatherVertexArgsCPU) == 48, "FRGatherVertexArgs layout");
struct FRGatherIndexArgsCPU
{
    uint32_t count, indexSize, srcStart, dstStart, baseVertex, pad0, pad1, pad2;
};
static_assert(sizeof(FRGatherIndexArgsCPU) == 32, "FRGatherIndexArgs layout");

// A mesh copy prepared on the main thread (CPU path) or a gather request (GPU path).
struct FRMeshUpload
{
    int32_t key = -1;
    id<MTLBuffer> buffer;           // positions | attrs | indices | submesh starts
    uint32_t vertexCount = 0, indexCount = 0;
    uint64_t attrOffset = 0, indexOffset = 0, subOffset = 0;
    std::vector<uint32_t> subStart, subCount;
    bool deforming = false;
    int32_t notBeforeFrame = 0;
    // GPU gather (non-readable Unity mesh)
    bool gather = false;
    id<MTLBuffer> streams[4];
    id<MTLBuffer> unityIndexBuffer;
    FRGatherVertexArgsCPU vertexArgs{};
    std::vector<FRGatherIndexArgsCPU> indexArgs;
    // a range of a shared buffer (fracture pieces): FRMeshGPU points into `buffer` at these offsets
    bool range = false;
    uint64_t rangeSubOffset = 0;    // where this range's submeshStart[0] lives in `buffer`
};

struct FRMeshEntry
{
    enum State : int { Pending = 1, Built = 2, Compacted = 3, Failed = -1 };
    int32_t key = -1;
    uint32_t slot = 0, slotAlt = 0;
    id<MTLBuffer> buffer;
    id<MTLBuffer> altPositions;     // deforming: ping-pong positions
    id<MTLBuffer> refitPositions;   // deforming: new positions parked by RefitMesh until the next AS encoder
    uint32_t vertexCount = 0, indexCount = 0, triangles = 0;
    uint64_t attrOffset = 0, indexOffset = 0, subOffset = 0;
    std::vector<uint32_t> subStart, subCount;
    id<MTLAccelerationStructure> blas;
    id<MTLAccelerationStructure> blasAlt;   // deforming: refit target
    id<MTLAccelerationStructure> compactTarget;
    uint64_t compactSerial = 0;
    int64_t compactSizeIndex = -1;          // pending size query
    uint64_t compactedSize = 0;
    bool deforming = false;
    bool usingAlt = false;
    bool gatherPending = false;
    State state = Pending;
    int32_t notBeforeFrame = 0;
    uint64_t builtBytes = 0;
    // gather sources (released once the gather's command buffer completes)
    id<MTLBuffer> gatherStreams[4];
    id<MTLBuffer> gatherIndexBuffer;
    FRGatherVertexArgsCPU gatherVertexArgs{};
    std::vector<FRGatherIndexArgsCPU> gatherIndexArgs;
};

struct FRCameraTargets
{
    id<MTLTexture> depth, normal, raw, aux, rawAA, out, ids;
    uint32_t width = 0, height = 0;
};

struct FRCameraState
{
    int32_t cameraId = -1;
    FRCameraTargets t;
    struct Slot
    {
        id<MTLBuffer> descriptors, records, lamps;
        id<MTLAccelerationStructure> tlas;
        uint32_t capacity = 0;
        uint32_t count = 0;
        uint64_t serial = 0;
        bool valid = false;
    } slots[3];
    id<MTLBuffer> scratch;
    uint32_t next = 0;
    int32_t lastSlot = -1;
    // last frame's trace rectangle: this frame traces and resolves the union, so no texel of the persistent
    // targets outside the new rectangle keeps an old reflection (0 = empty)
    simd_uint4 prevRect = { 0, 0, 0, 0 };
};

struct FRRetired
{
    uint64_t serial;
    id object;
    uint32_t meshSlot;      // UINT32_MAX = none
};

// Completion results handed from the Metal completion thread to the render thread.
struct FRCompletionNote
{
    int32_t key;
    uint64_t compactedSize;
};

class FRGlassRTScene
{
public:
    static constexpr uint32_t kMaxMeshSlots = 16384;
    static constexpr uint32_t kMaxMaterials = 4096;
    static constexpr uint32_t kMaxTextures = 4096;
    static constexpr uint32_t kMaxInstances = 4096;
    static constexpr uint32_t kMaxLamps = 256;
    static constexpr uint32_t kTrianglesPerFrame = 50000;

    bool Init(id<MTLDevice> device, std::string& error);
    void Shutdown();

    // ops (render thread)
    void AddMesh(std::unique_ptr<FRMeshUpload> upload, uint64_t serial);
    void ReleaseMesh(int32_t key, uint64_t serial);
    void RefitMesh(int32_t key, id<MTLBuffer> positions, uint64_t serial);
    void SetTexture(uint32_t slot, id<MTLTexture> texture, uint64_t serial);
    void SetMaterials(uint32_t first, const std::vector<FRMaterial>& materials);
    void Reset(uint64_t serial);

    // per event (render thread)
    void Purge(uint64_t doneSerial);
    void DrainCompletions();
    void EncodeGathers(id<MTLCommandBuffer> cb, id<MTLComputePipelineState> vtx, id<MTLComputePipelineState> idx, uint64_t serial,
                       std::vector<id>& keepAlive);
    // Builds/refits/compactions within budget and this camera's TLAS. Returns the TLAS slot used, or -1.
    int EncodeAccelerationStructures(id<MTLCommandBuffer> cb, FRCameraState& cam, const std::vector<FRInstanceIn>& instances,
                                     const std::vector<FRLamp>& lamps, int32_t frame, uint64_t serial, uint64_t doneSerial,
                                     id<MTLFence> fence, id<MTLCounterSampleBuffer> counters, uint32_t sampleBase,
                                     id<MTLBuffer> compactSizes, std::vector<std::pair<int32_t, uint32_t>>& compactQueries,
                                     uint32_t& instanceCountOut, bool& ringBusy);
    void UseResources(id<MTLComputeCommandEncoder> enc);
    void AttachResidency(id<MTLCommandBuffer> cb);
    void CompleteCompaction(int32_t key, uint64_t size);

    id<MTLBuffer> MeshTable() const { return meshTable; }
    id<MTLBuffer> MaterialTable() const { return materialTable; }
    id<MTLBuffer> TextureTable() const { return textureTable; }

    // stats
    uint64_t BlasBytes() const { return blasBytes; }
    uint64_t GeometryBytes() const { return geometryBytes; }
    uint32_t MeshCount() const { return (uint32_t)live; }
    uint32_t PendingCount() const;
    uint32_t CompactedCount() const { return compacted; }
    uint64_t BuiltTotal() const { return builtTotal; }
    uint32_t LastBuilds() const { return lastBuilds; }
    uint32_t LastTriangles() const { return lastTriangles; }
    uint32_t RetiredCount() const { return (uint32_t)retired.size(); }
    bool UsesResidencySet() const { return residencySet != nil; }
    int MeshState(int32_t key) const;

private:
    void Retire(id object, uint64_t serial, uint32_t meshSlot = UINT32_MAX);
    uint32_t AllocSlot();
    void WriteSlot(uint32_t slot, const FRMeshEntry& e, bool alt);
    void ResidencyAdd(id a);
    void ResidencyRemove(id a);
    MTLPrimitiveAccelerationStructureDescriptor* Describe(const FRMeshEntry& e, id<MTLBuffer> positions) const;

    id<MTLDevice> device;
    id<MTLBuffer> meshTable, materialTable, textureTable;
    id<MTLTexture> defaults[4];
    std::vector<id<MTLTexture>> textures;
    std::vector<std::unique_ptr<FRMeshEntry>> meshes;   // index = key
    std::vector<FRMeshEntry*> pending;
    std::vector<uint32_t> freeSlots;
    uint32_t slotHigh = 0;
    std::vector<FRRetired> retired;
    std::vector<std::unique_ptr<FRMeshEntry>> retiredEntries;   // entries whose objects are retiring
    id residencySet;                                           // id<MTLResidencySet> (macOS 15+), else nil
    bool residencyDirty = false;
    std::vector<id<MTLResource>> residentList;                 // useResources fallback
    bool residentListDirty = true;
    std::unordered_map<void*, id> resident;
    id<MTLBuffer> buildScratch;
    std::mutex completionMutex;
    std::vector<FRCompletionNote> completions;
    mutable std::mutex stateMutex;                             // guards meshState for main-thread queries
    std::unordered_map<int32_t, int> meshState;
    uint64_t blasBytes = 0, geometryBytes = 0, builtTotal = 0;
    size_t live = 0;
    uint32_t compacted = 0, lastBuilds = 0, lastTriangles = 0;
};
