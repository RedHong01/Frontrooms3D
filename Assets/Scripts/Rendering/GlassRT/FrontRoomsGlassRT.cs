// Public API of the desktop ray-traced glass reflections (G14; design: Documentation/research/glass/rt/
// 10_rt_glass_design.md §2.2). Compiled on EVERY platform so callers (the destruction track's breakable,
// the map, the verify harness) need no #if: off the macOS editor / macOS player every member is a no-op
// and Supported is false. The work happens in FrontRoomsGlassRTSystem (macOS only).
//
// Quality: Off / High / Ultra. Default while Supported: Ultra at quality level 5, High at 3-4, Off at 0-2.
// Overrides: PlayerPrefs "FrontRooms.GlassRT" (off|high|ultra), command line -frGlassRT off|high|ultra.
using System;
using UnityEngine;

public enum FrontRoomsGlassRTQuality { Off = 0, High = 1, Ultra = 2 }

/// <summary>Per-instance flags (NativePlugin/FRGlassRTShared.h FR_FLAG_*). One record per instance, so every
/// fracture piece carries its own.</summary>
[Flags]
public enum FrontRoomsGlassRTFlags : uint
{
    None = 0,
    /// <summary>See-through glass in reflection rays (x(1-F)0.9, 1 layer on High, 2 on Ultra).</summary>
    Glass = 1u << 0,
    /// <summary>Use vertex colour R as the green edge mask at hits.</summary>
    FractureEdge = 1u << 1,
    /// <summary>A small piece (&lt; 4 cm²): kept out of reflection rays (instance mask), still drawn and still
    /// gets a reflection on its own pixels.</summary>
    Shard = 1u << 2,
    /// <summary>Emission read from the renderer's MaterialPropertyBlock every frame (troffer lenses).</summary>
    EmissiveLens = 1u << 3,
    /// <summary>Transform read every frame (doors, keys, the Relay, falling pieces).</summary>
    Dynamic = 1u << 4,
    Relay = 1u << 5,
    Door = 1u << 6,
    MeshUV = 1u << 7,
    SurfacePlanar = 1u << 8,
    /// <summary>The mesh deforms: its BLAS is refit when RefitMesh is called.</summary>
    Deforming = 1u << 9,
    /// <summary>Seen only by reflection rays (design §5 D2, a possible player body).</summary>
    ReflectionOnly = 1u << 10,
    /// <summary>This renderer RECEIVES traced reflections (it is drawn into the glass prepass).</summary>
    Receiver = 1u << 11,
}

/// <summary>One fracture piece inside a shared mesh: an index range (destruction plan §4.7).</summary>
public struct FrontRoomsGlassRTRange
{
    public int indexStart, indexCount, baseVertex;
}

/// <summary>Per-stage GPU times and counts of the last traced frame (FrontRoomsGlassRT.TryGetStats).</summary>
public struct FrontRoomsGlassRTStats
{
    public float gpuMsAccel, gpuMsTrace, gpuMsResolve, gpuMsTotal, gpuMsCommandBuffer, gpuMsFrame;
    public float encodeMs, encodeMsMax, mainThreadMs, mainThreadMsMax, compileMs;
    public int tlasInstances, candidates, meshes, meshesPending, blasBuiltTotal, compacted, retired, lamps, receiversVisible;
    public int glassPixels, missPixels, hitPixels, layerPixels, aaPixels, shadowRays;
    public int events, traceEvents, skippedNotReady, ringBusy, commandBufferErrors, accumulation;
    public long blasBytes, geometryBytes, targetBytes;
    public string residency, hazardModes;
}

public static class FrontRoomsGlassRT
{
    /// <summary>Requested quality. Forced Off while !Supported.</summary>
    public static FrontRoomsGlassRTQuality Quality
    {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        get => FrontRoomsGlassRTSystem.Quality;
        set => FrontRoomsGlassRTSystem.Quality = value;
#else
        get => FrontRoomsGlassRTQuality.Off;
        set { }
#endif
    }

    /// <summary>Apple9 (M3/M4) + kernel compiled + GPU layout test passed + Metal (false off-Mac).</summary>
    public static bool Supported
    {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        get => FrontRoomsGlassRTSystem.Supported;
#else
        get => false;
#endif
    }

    /// <summary>One line: why RT is off, or the tier and residency mode.</summary>
    public static string Status
    {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        get => FrontRoomsGlassRTSystem.Status;
#else
        get => "off: not a macOS build";
#endif
    }

    /// <summary>The camera may trace (FrontRoomsPostStack.ConfigureCamera calls this for the game camera).</summary>
    public static void OptIn(Camera camera)
    {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        FrontRoomsGlassRTSystem.OptIn(camera);
#endif
    }

    /// <summary>Starts the system without a camera (the old Ensure() path). Safe to call repeatedly.</summary>
    public static void EnsureStarted()
    {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        FrontRoomsGlassRTSystem.EnsureStarted();
#endif
    }

    /// <summary>Register a renderer outside the map (destruction pieces, the verify harness). Returns a handle (-1 off-Mac).
    /// It is in the TLAS from this frame on.</summary>
    public static int Register(Renderer renderer, FrontRoomsGlassRTFlags flags, int windowId = -1)
    {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        return FrontRoomsGlassRTSystem.Register(renderer, flags, windowId);
#else
        return -1;
#endif
    }

    /// <summary>Out of the TLAS this frame.</summary>
    public static void Unregister(int handle)
    {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        FrontRoomsGlassRTSystem.Unregister(handle);
#endif
    }

    /// <summary>Beat-frame swaps without re-registering: takes effect in the same frame.</summary>
    public static void SetEnabled(int handle, bool enabled)
    {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        FrontRoomsGlassRTSystem.SetEnabled(handle, enabled);
#endif
    }

    /// <summary>Batched BLAS build, one BLAS per index range of one readable mesh, never before notBeforeFrame.
    /// Returns a ticket (-1 off-Mac). Register a piece renderer with RegisterPiece(ticket, i, ...).</summary>
    public static int PrepareMeshes(Mesh source, FrontRoomsGlassRTRange[] pieces, int notBeforeFrame)
    {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        return FrontRoomsGlassRTSystem.PrepareMeshes(source, pieces, notBeforeFrame);
#else
        return -1;
#endif
    }

    /// <summary>True once every BLAS of the ticket is built.</summary>
    public static bool IsPrepared(int ticket)
    {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        return FrontRoomsGlassRTSystem.IsPrepared(ticket);
#else
        return false;
#endif
    }

    /// <summary>Register piece i of a PrepareMeshes ticket, drawn by renderer (its transform moves the piece).</summary>
    public static int RegisterPiece(int ticket, int piece, Renderer renderer, FrontRoomsGlassRTFlags flags, int windowId = -1)
    {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        return FrontRoomsGlassRTSystem.RegisterPiece(ticket, piece, renderer, flags, windowId);
#else
        return -1;
#endif
    }

    /// <summary>A registered mesh was edited in place: its BLAS is rebuilt (new shape from the next frame).</summary>
    public static void MeshChanged(Mesh mesh)
    {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        FrontRoomsGlassRTSystem.MeshChanged(mesh);
#endif
    }

    /// <summary>Refit the BLAS of a mesh registered with the Deforming flag from its current vertices.</summary>
    public static void RefitMesh(Mesh deforming)
    {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        FrontRoomsGlassRTSystem.RefitMesh(deforming);
#endif
    }

    /// <summary>A pane broke at this point: receivers within 1 m leave the TLAS and the prepass this frame.</summary>
    public static void GlassBroken(Vector3 at)
    {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        FrontRoomsGlassRTSystem.GlassBroken(at);
#endif
    }

    /// <summary>GPU ms per stage, counts, skips, errors. False off-Mac or before the first trace.</summary>
    public static bool TryGetStats(out FrontRoomsGlassRTStats stats)
    {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        return FrontRoomsGlassRTSystem.TryGetStats(out stats);
#else
        stats = default;
        return false;
#endif
    }
}
