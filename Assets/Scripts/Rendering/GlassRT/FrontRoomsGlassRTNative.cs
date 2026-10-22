// P/Invoke layer of the desktop Metal ray-traced glass bridge (G14). Compiled ONLY for the macOS editor and the
// macOS player: no other platform (WebGL, Windows) contains a single FRGlassRT_ import (design 10 §2.5, WebGL plan R2).
// Every struct mirrors NativePlugin/FRGlassRTShared.h / FRGlassRTScene.h field for field; the plugin's GPU layout
// self-test and the C++ static_asserts pin the native side; AbiCheck() below compares this side with the LOADED plugin.
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
using System;
using System.Runtime.InteropServices;
using UnityEngine;

static class FrontRoomsGlassRTNative
{
    const string Plugin = "FrontRoomsMetalGlassRT";

    // ---- capability bits (FRGlassRT_QueryCaps)
    public const int CapPluginLoaded = 1 << 0, CapDevice = 1 << 1, CapRaytracing = 1 << 2, CapApple9 = 1 << 3, CapMetal3 = 1 << 4,
        CapResidencySets = 1 << 5, CapCompileStarted = 1 << 6, CapKernelReady = 1 << 7, CapLayoutOK = 1 << 8, CapLayoutFailed = 1 << 9,
        CapCompileFailed = 1 << 10, CapGpuError = 1 << 11, CapOS14 = 1 << 12, CapCounters = 1 << 13;

    // ---- material kinds / flags (FR_KIND_*, FR_MAT_*)
    public const uint KindFlat = 0, KindSurface = 1, KindLit = 2, KindGlass = 3;
    public const uint MatMeshUV = 1u << 0, MatEmission = 1u << 1, MatMetallicMap = 1u << 2, MatSmoothAlbedo = 1u << 3, MatNormalMap = 1u << 4,
        MatOcclusionMap = 1u << 5, MatAlphaClip = 1u << 6, MatPrint = 1u << 7, MatLampField = 1u << 8;
    /// <summary>Instance flag FR_FLAG_LAMP_WEIGHTS: emission.xyz carries the renderer's _LampField weights (exit-sign diffusers).</summary>
    public const uint FlagLampWeights = 1u << 12;
    /// <summary>Instance masks (FR_MASK_*). MaskCaster: the renderer casts shadows in the raster; shadow rays see only these.</summary>
    public const uint MaskOpaque = 0x01, MaskGlass = 0x02, MaskShard = 0x04, MaskDynamic = 0x08, MaskReflectionOnly = 0x10, MaskCaster = 0x20;
    public const uint LampDirectional = 0, LampPoint = 1, LampSpot = 2;
    public const uint DebugNone = 0, DebugParity = 1, DebugHitId = 2;
    public const int TexWhite = 0, TexBlack = 1, TexBump = 2, TexGrey = 3;

    [StructLayout(LayoutKind.Sequential)]
    public struct VertexAttr { public float nx, ny, nz, colorR, u, v; }

    [StructLayout(LayoutKind.Sequential)]
    public struct Material
    {
        public Vector4 baseColor, tile, baseST, emission, stain, p0, p1, p2;
        public Vector4 printST, inkGround, inkMid, inkDeep, inkCream;   // FrontRooms/Surface print layer (_FR_PRINT)
        public uint kind, flags, texBase, texMask, texBump, texMacro, texEmission, texOcclusion;
        public uint texPrint, texLampField, pad1, pad2;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Lamp { public Vector4 posInvRange2, color, spotDir, atten; }

    [StructLayout(LayoutKind.Sequential)]
    public struct Frame
    {
        public Matrix4x4 invViewProj, viewProj;
        public Vector4 camPos, camFwd, screen;
        public uint rectX0, rectY0, rectX1, rectY1;
        public Vector4 fog, fogColor;
        public Vector4 sh0, sh1, sh2, sh3, sh4, sh5, sh6, sh7, sh8;
        public Vector4 envDecode, envInfo;
        public uint lampCount, quality, debugMode, instanceCount;
        public uint maxShadowLamps, glassLayers, frameIndex, accumulationIndex;
        public Vector4 jitter, thin, blur, fill;
        /// <summary>x lamp area radius (m), y shadow rays per traced lamp, z extra penumbra rays, w no smudge blur at or above this pane smoothness.</summary>
        public Vector4 soft;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct InstanceIn
    {
        public Vector4 o2w0, o2w1, o2w2;
        public int meshKey;
        public uint materialBase, flags, windowId;
        public Vector4 emission;
        public uint mask, pad0, pad1, pad2;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct GatherVertexArgs
    {
        public uint vertexCount, posStride, posOffset, nrmStride, nrmOffset, nrmFormat, uvStride, uvOffset, uvFormat, colStride, colOffset, colFormat;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct GatherIndexArgs { public uint count, indexSize, srcStart, dstStart, baseVertex, pad0, pad1, pad2; }

    /// <summary>The plugin ABI these scripts were written for (NativePlugin/FRGlassRTShared.h FR_GLASS_RT_ABI).</summary>
    public const int Abi = 32;

    /// <summary>Asks the LOADED plugin (fix round 32, review 2 engineering #7; before, this compared the C# struct sizes with
    /// constants and never asked the plugin, so a stale plugin left loaded by an open editor after an apply only failed on
    /// its first missing export, with a console error). FRGlassRT_GetAbi returns the ABI and the sizes the plugin was built
    /// with, in the order FRVertexAttr, FRMaterial, FRLamp, FRFrame, FRInstanceIn, FRGatherVertexArgs, FRGatherIndexArgs.
    /// An old plugin has no such export: EntryPointNotFoundException, which CheckNative reports as "an old build" (RT Off,
    /// no other native call). False with a reason on any mismatch.</summary>
    public static bool AbiCheck(out string error)
    {
        var native = new int[7];
        var abi = FRGlassRT_GetAbi(native, native.Length);
        if (abi != Abi) { error = "plugin ABI " + abi + ", these scripts need " + Abi + " (restart Unity after updating the plugin)"; return false; }
        var mine = new[] { Marshal.SizeOf<VertexAttr>(), Marshal.SizeOf<Material>(), Marshal.SizeOf<Lamp>(), Marshal.SizeOf<Frame>(),
                           Marshal.SizeOf<InstanceIn>(), Marshal.SizeOf<GatherVertexArgs>(), Marshal.SizeOf<GatherIndexArgs>() };
        var names = new[] { "VertexAttr", "Material", "Lamp", "Frame", "InstanceIn", "GatherVertexArgs", "GatherIndexArgs" };
        for (var i = 0; i < mine.Length; i++)
            if (mine[i] != native[i]) { error = names[i] + " is " + mine[i] + " bytes here, the loaded plugin has " + native[i]; return false; }
        error = null;
        return true;
    }

    // ---- stats ids (FRGlassRT_GetStat)
    public enum Stat
    {
        Events = 0, TraceEvents, SkippedNotReady, RingBusy, TlasInstances, Meshes, MeshesPending, BlasBuiltTotal,
        BlasBytes, GeometryBytes, Compacted, GpuMsAS, GpuMsTrace, GpuMsResolve, GpuMsTotal, EncodeMs,
        EncodeMsMax, GlassPx, MissPx, HitPx, LayerPx, AAPx, ShadowRays, CbErrors,
        CompileMs, ResidencyMode, HazardDepth, HazardOut, TrianglesLast, BuildsLast, TimerFrameGpuMs, DoneSerial,
        EventSerial, Retired, TimestampMode, GpuMsCB, LayoutOK, SkippedNoFrame, Clears, OpsApplied,
        MeshesUploaded, CounterNsPerTick, TargetsBytes, LastFrameNumber, WarmEvents, PoolBusy, SubmitMs, SubmitMsMax,
        Barriers, GeoPages, GeoSpareHits, TlasBuilt, TlasReused,
        StreamTriBudget, StreamBuildBudget   // fix round 33: the streamed-mesh BLAS budget in force (0 on an older plugin)
    }

    [DllImport(Plugin)] public static extern int FRGlassRT_QueryCaps();
    /// <summary>ABI + struct sizes of the loaded plugin (AbiCheck). Missing in plugins before fix round 32.</summary>
    [DllImport(Plugin)] static extern int FRGlassRT_GetAbi([Out] int[] sizes, int count);
    /// <summary>Coverage of camera `cam`'s last completed traced frame: [0] frame number (-1 none), [1] glass px, [2] miss px.</summary>
    [DllImport(Plugin)] public static extern int FRGlassRT_GetCameraCoverage(int cam, [Out] long[] values);
    [DllImport(Plugin)] static extern int FRGlassRT_CopyLastError(byte[] buffer, int length);
    [DllImport(Plugin)] public static extern double FRGlassRT_GetStat(int id);
    [DllImport(Plugin)] public static extern void FRGlassRT_ResetStatMax();
    [DllImport(Plugin)] public static extern IntPtr FRGlassRT_GetRenderEventFunc();
    [DllImport(Plugin)] public static extern void FRGlassRT_SetCameraTargets(int cam, IntPtr depth, IntPtr normal, IntPtr raw, IntPtr aux, IntPtr rawAA, IntPtr outTex, IntPtr ids, int width, int height);
    [DllImport(Plugin)] public static extern void FRGlassRT_FreeCamera(int cam);
    // E1 (2026-10-14): array arguments go in as pointers to pinned arrays. Passed as managed arrays, every call marshalled
    // the WHOLE reused scratch array (up to 65,536 entries), not `vertexCount` of them: 3.47 ms for a 96-vertex mesh with
    // 65,536-entry scratch arrays, 0.0067 ms with exact ones. The plugin copies exactly the counts it is given.
    [DllImport(Plugin)] public static extern int FRGlassRT_UploadMeshCPU(int key, IntPtr positions, IntPtr attrs, int vertexCount, IntPtr indices, int indexCount,
                                                                         IntPtr subStart, IntPtr subCount, int submeshCount, int deforming, int notBeforeFrame);
    [DllImport(Plugin)] public static extern int FRGlassRT_UploadMeshGPU(int key, IntPtr stream0, IntPtr stream1, IntPtr stream2, IntPtr stream3, ref GatherVertexArgs vertexArgs,
                                                                         IntPtr indexBuffer, GatherIndexArgs[] indexArgs, int submeshCount, int indexCount);
    [DllImport(Plugin)] public static extern int FRGlassRT_UploadMeshRanges(int firstKey, IntPtr positions, IntPtr attrs, int vertexCount, IntPtr indices, int indexCount,
                                                                            IntPtr rangeStart, IntPtr rangeCount, int ranges, int notBeforeFrame);
    [DllImport(Plugin)] public static extern int FRGlassRT_RefitMesh(int key, IntPtr positions, int vertexCount);
    [DllImport(Plugin)] public static extern void FRGlassRT_ReleaseMesh(int key);
    [DllImport(Plugin)] public static extern int FRGlassRT_MeshState(int key);
    [DllImport(Plugin)] public static extern void FRGlassRT_MeshStates(int[] keys, int count, int[] states);
    [DllImport(Plugin)] public static extern void FRGlassRT_SetTexture(int slot, IntPtr texture);
    [DllImport(Plugin)] public static extern void FRGlassRT_SetMaterials(int first, IntPtr materials, int count);
    [DllImport(Plugin)] public static extern void FRGlassRT_ResetScene();
    [DllImport(Plugin)] public static extern void FRGlassRT_SetBarrierMode(int mode);
    /// <summary>1 (default) = keep a camera's last TLAS while its instance descriptors are byte-identical (fix round 31);
    /// 0 = build it on every traced frame (harness A/B).</summary>
    [DllImport(Plugin)] public static extern void FRGlassRT_SetTlasReuse(int on);
    /// <summary>This thread's CPU time in ns (CLOCK_THREAD_CPUTIME_ID): main-thread cost without preemption by other processes.</summary>
    [DllImport(Plugin)] public static extern ulong FRGlassRT_ThreadCpuNs();
    /// <summary>BLAS budget per event for streamed meshes (fracture pieces keep their own); 0 = the default (10k triangles, 8 builds).</summary>
    [DllImport(Plugin)] public static extern void FRGlassRT_SetBuildBudget(int triangles, int builds);
    [DllImport(Plugin)] public static extern ulong FRGlassRT_SubmitWarmup(int frameNumber);
    // The frame packet goes in as raw pointers into buffers the C# side keeps pinned for the session (GCHandle): no
    // marshalling of the 4,096-entry instance array, only the n entries in use are copied (by the plugin).
    [DllImport(Plugin)] public static extern ulong FRGlassRT_SubmitFrame(int cam, int frameNumber, IntPtr frame, IntPtr instances, int instanceCount,
                                                                        IntPtr lamps, int lampCount, IntPtr envCube);

    public static string LastError()
    {
        var buffer = new byte[1024];
        var n = FRGlassRT_CopyLastError(buffer, buffer.Length);
        return n <= 0 ? "" : System.Text.Encoding.UTF8.GetString(buffer, 0, Math.Min(n, buffer.Length - 1));
    }

    public static double Get(Stat s) => FRGlassRT_GetStat((int)s);

    /// <summary>Pins arrays for one native call (E1): only the counts passed with them are read by the plugin.</summary>
    public struct Pins : IDisposable
    {
        GCHandle a, b, c, d, e;
        public IntPtr A, B, C, D, E;
        public Pins(object pa, object pb = null, object pc = null, object pd = null, object pe = null)
        {
            a = b = c = d = e = default;
            A = Pin(pa, ref a); B = Pin(pb, ref b); C = Pin(pc, ref c); D = Pin(pd, ref d); E = Pin(pe, ref e);
        }
        static IntPtr Pin(object o, ref GCHandle h)
        {
            if (o == null) return IntPtr.Zero;
            h = GCHandle.Alloc(o, GCHandleType.Pinned);
            return h.AddrOfPinnedObject();
        }
        public void Dispose()
        {
            if (a.IsAllocated) a.Free(); if (b.IsAllocated) b.Free(); if (c.IsAllocated) c.Free();
            if (d.IsAllocated) d.Free(); if (e.IsAllocated) e.Free();
        }
    }
}
#endif
