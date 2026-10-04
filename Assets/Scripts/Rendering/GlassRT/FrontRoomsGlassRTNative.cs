// P/Invoke layer of the desktop Metal ray-traced glass bridge (G14). Compiled ONLY for the macOS editor and the
// macOS player: no other platform (WebGL, Windows) contains a single FRGlassRT_ import (design 10 §2.5, WebGL plan R2).
// Every struct mirrors NativePlugin/FRGlassRTShared.h / FRGlassRTScene.h field for field; the plugin's GPU layout
// self-test and the C++ static_asserts pin the native side, SizeCheck() below pins this side.
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
        MatOcclusionMap = 1u << 5, MatAlphaClip = 1u << 6;
    public const uint MaskOpaque = 0x01, MaskGlass = 0x02, MaskShard = 0x04, MaskDynamic = 0x08, MaskReflectionOnly = 0x10;
    public const uint LampDirectional = 0, LampPoint = 1, LampSpot = 2;
    public const uint DebugNone = 0, DebugParity = 1, DebugHitId = 2;
    public const int TexWhite = 0, TexBlack = 1, TexBump = 2, TexGrey = 3;

    [StructLayout(LayoutKind.Sequential)]
    public struct VertexAttr { public float nx, ny, nz, colorR, u, v; }

    [StructLayout(LayoutKind.Sequential)]
    public struct Material
    {
        public Vector4 baseColor, tile, baseST, emission, stain, p0, p1, p2;
        public uint kind, flags, texBase, texMask, texBump, texMacro, texEmission, texOcclusion;
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

    public static bool SizeCheck(out string error)
    {
        string first = null;
        void Check<T>(int expected) where T : struct
        {
            var s = Marshal.SizeOf<T>();
            if (s != expected && first == null) first = typeof(T).Name + " is " + s + " bytes, native expects " + expected;
        }
        Check<VertexAttr>(24); Check<Material>(160); Check<Lamp>(64); Check<Frame>(496); Check<InstanceIn>(96);
        Check<GatherVertexArgs>(48); Check<GatherIndexArgs>(32);
        error = first;
        return first == null;
    }

    // ---- stats ids (FRGlassRT_GetStat)
    public enum Stat
    {
        Events = 0, TraceEvents, SkippedNotReady, RingBusy, TlasInstances, Meshes, MeshesPending, BlasBuiltTotal,
        BlasBytes, GeometryBytes, Compacted, GpuMsAS, GpuMsTrace, GpuMsResolve, GpuMsTotal, EncodeMs,
        EncodeMsMax, GlassPx, MissPx, HitPx, LayerPx, AAPx, ShadowRays, CbErrors,
        CompileMs, ResidencyMode, HazardDepth, HazardOut, TrianglesLast, BuildsLast, TimerFrameGpuMs, DoneSerial,
        EventSerial, Retired, TimestampMode, GpuMsCB, LayoutOK, SkippedNoFrame, Clears, OpsApplied,
        MeshesUploaded, CounterNsPerTick, TargetsBytes, LastFrameNumber, WarmEvents, PoolBusy
    }

    [DllImport(Plugin)] public static extern int FRGlassRT_QueryCaps();
    [DllImport(Plugin)] static extern int FRGlassRT_CopyLastError(byte[] buffer, int length);
    [DllImport(Plugin)] public static extern double FRGlassRT_GetStat(int id);
    [DllImport(Plugin)] public static extern void FRGlassRT_ResetStatMax();
    [DllImport(Plugin)] public static extern IntPtr FRGlassRT_GetRenderEventFunc();
    [DllImport(Plugin)] public static extern void FRGlassRT_SetCameraTargets(int cam, IntPtr depth, IntPtr normal, IntPtr raw, IntPtr aux, IntPtr rawAA, IntPtr outTex, IntPtr ids, int width, int height);
    [DllImport(Plugin)] public static extern void FRGlassRT_FreeCamera(int cam);
    [DllImport(Plugin)] public static extern int FRGlassRT_UploadMeshCPU(int key, Vector3[] positions, VertexAttr[] attrs, int vertexCount, int[] indices, int indexCount,
                                                                         int[] subStart, int[] subCount, int submeshCount, int deforming, int notBeforeFrame);
    [DllImport(Plugin)] public static extern int FRGlassRT_UploadMeshGPU(int key, IntPtr stream0, IntPtr stream1, IntPtr stream2, IntPtr stream3, ref GatherVertexArgs vertexArgs,
                                                                         IntPtr indexBuffer, GatherIndexArgs[] indexArgs, int submeshCount, int indexCount);
    [DllImport(Plugin)] public static extern int FRGlassRT_UploadMeshRanges(int firstKey, Vector3[] positions, VertexAttr[] attrs, int vertexCount, int[] indices, int indexCount,
                                                                            int[] rangeStart, int[] rangeCount, int ranges, int notBeforeFrame);
    [DllImport(Plugin)] public static extern int FRGlassRT_RefitMesh(int key, Vector3[] positions, int vertexCount);
    [DllImport(Plugin)] public static extern void FRGlassRT_ReleaseMesh(int key);
    [DllImport(Plugin)] public static extern int FRGlassRT_MeshState(int key);
    [DllImport(Plugin)] public static extern void FRGlassRT_MeshStates(int[] keys, int count, int[] states);
    [DllImport(Plugin)] public static extern void FRGlassRT_SetTexture(int slot, IntPtr texture);
    [DllImport(Plugin)] public static extern void FRGlassRT_SetMaterials(int first, Material[] materials, int count);
    [DllImport(Plugin)] public static extern void FRGlassRT_ResetScene();
    [DllImport(Plugin)] public static extern ulong FRGlassRT_SubmitWarmup(int frameNumber);
    [DllImport(Plugin)] public static extern ulong FRGlassRT_SubmitFrame(int cam, int frameNumber, ref Frame frame, InstanceIn[] instances, int instanceCount,
                                                                        Lamp[] lamps, int lampCount, IntPtr envCube);

    public static string LastError()
    {
        var buffer = new byte[1024];
        var n = FRGlassRT_CopyLastError(buffer, buffer.Length);
        return n <= 0 ? "" : System.Text.Encoding.UTF8.GetString(buffer, 0, Math.Min(n, buffer.Length - 1));
    }

    public static double Get(Stat s) => FRGlassRT_GetStat((int)s);
}
#endif
