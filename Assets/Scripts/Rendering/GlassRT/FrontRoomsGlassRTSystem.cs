// The desktop Metal ray-traced glass reflections, C# side (G14 P0 + P1; design 10 §1.3-§1.8, §2.2, §2.6, §2.7).
// macOS editor / macOS player only. Everything here runs on the main thread and never touches Metal:
//   - scene registry: one native mesh per Unity Mesh (copied, never Unity's buffers), a material table, a
//     texture table, one instance record per renderer (flags + mask per instance, R15);
//   - registration without MapWorld edits (P0): the map is found once, then a cheap per-frame diff of its chunk
//     roots and their child counts; new or changed chunks are rescanned, registration is sliced to 0.2 ms/frame
//     (2 ms, nearest chunk first, while a chunk near the camera is unregistered; fix round 32);
//   - per traced camera: same-frame liveness of receivers / dynamic instances, a TLAS candidate list around the
//     camera (priority: receivers, glass, dynamic, Relay; then nearest, cap 2,048 High / 4,096 Ultra), the lamps
//     that are on this frame, ambient SH, fog, the zone cube, and one frame packet pushed to the plugin.
// The render thread (plugin event) does all Metal work: BLAS builds, TLAS, trace, resolve.
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using FrontRooms.Map;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;
using Native = FrontRoomsGlassRTNative;
using Object = UnityEngine.Object;

// The verify harness (Assets/Editor/RT/FrontRoomsGlassRTVerify.cs) reads the per-camera targets and stats.
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Assembly-CSharp-Editor")]

static class FrontRoomsGlassRTSystem
{
    // ------------------------------------------------------------------ constants
    /// <summary>Rendering-layer bit of receivers drawn with their own FRGlassRTPrepass pass (FrontRooms/Glass).</summary>
    public const int PrepassBit = 30;
    /// <summary>Rendering-layer bit of receivers drawn with Hidden/FrontRooms/GlassRTPrepass (other materials).</summary>
    public const int OverrideBit = 29;
    const int MaxMaterials = 4096, MaxTextures = 4096, MaxLamps = 128;
    const float LampRadius = 30f;
    // Lens glow is read every frame for every fixture within this radius of the camera, lit or not (unlit lenses still
    // change: a stuttering lamp writes its dimmed glow in FarEmissionStep steps). Beyond it: 48 lenses per frame,
    // round-robin. 34 m covers what the reflections show of the corridors around a window (run r2: a stuttering lamp
    // 27.7 m away, refreshed only round-robin, reflected with r 0.02).
    const float LensReadRadius = 34f;
    const int StaticRefreshPerFrame = 32;
    const float NearDynamic2 = 15f * 15f;    // dynamic transforms read every frame within 15 m, every 8th frame beyond
    // Lens emission (MapWorld.TickFixtures): a LIT fixture's lens gets a new glow every frame; an unlit one only on a
    // real change (FarEmissionStep). So a lens is re-read every frame while its fixture's Light is on, on the frame it
    // turns on or off, and round-robin otherwise. Lights are only ever on near the player (lightRadius, 16 m): beyond
    // LitLens2 the Light is not even checked.
    const float LitLens2 = 26f * 26f;
    const float NearLens2 = 20f * 20f;       // lens emission enters the still-frame signature within 20 m
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    static readonly int CeilingHeightId = Shader.PropertyToID("_CeilingHeight");
    static readonly int RTReceiveId = Shader.PropertyToID("_RTReceive");
    static readonly int LampFieldId = Shader.PropertyToID("_LampField");

    // ------------------------------------------------------------------ settings (internal knobs for the harness)
    /// <summary>Main-thread budget per frame for the sliced chunk work: the hierarchy walk, the registrations and the
    /// light read-back of a chunk scan (fix round 31, 2026-10-22: was 1.0 ms, above the 0.3 ms main-thread bar by design;
    /// review 1 engineering #1). At least MinRegistrationsPerSlice registrations / MinNodesPerSlice nodes always run.</summary>
    internal static float RegisterBudgetMs = 0.2f;
    /// <summary>Fix round 32 (review 2 art #1, engineering #6): while a chunk within StreamGateRadius of the traced camera
    /// has not finished its first scan (run start, a jump, the C22 floor change, a chunk rebuilt around the player), the
    /// sliced chunk work gets this budget instead of RegisterBudgetMs, nearest chunk first. Steady streaming keeps 0.2 ms:
    /// the map builds new chunks at its build radius (2 chunks = 48 m ahead), outside StreamGateRadius.</summary>
    internal static float BurstBudgetMs = 2.0f;
    /// <summary>Camera-to-chunk distance (m, to the chunk's 24 m square, XZ) inside which a chunk that has not finished its
    /// first scan is a stream-in backlog. The lamp radius: when no backlog is left, the reflection's lamps are complete too.</summary>
    internal static float StreamGateRadius = LampRadius;
    /// <summary>The stream-in gate (fix round 32, review 2 art #1). Before, a traced frame always had weight 1, so after a
    /// jump the reflection showed holes (up to 90 % of the glass missed) and one lamp of 88 for several seconds. Now a camera
    /// shows the traced reflection only when no backlog is near it AND its own last traced frame submitted after the
    /// backlog cleared missed at most GateMissFraction of its glass px; until then weight 0 (the zone cube, today's glass),
    /// then a fade-in over GateFadeFrames. GateTimeoutFrames after the backlog cleared it opens anyway (a view whose rays
    /// legitimately leave the built map). StreamGate = false: weight 1 on every traced frame (the round 31 behaviour).</summary>
    internal static bool StreamGate = true;
    internal static float GateMissFraction = .005f;
    internal static int GateFadeFrames = 12, GateTimeoutFrames = 90;
    /// <summary>A chunk BORN within StreamGateRadius (the map building around the camera: run start, a jump, the C22 floor
    /// change) holds the gate even when the burst registers it on the same frame: its BLASes, and the chunks the map has not
    /// built yet, still leave holes. The gate stays held until no such birth happened for GateQuietFrames frames. Steady
    /// walking never triggers it: the map builds new chunks at its build radius, 48 m ahead.</summary>
    internal static int GateQuietFrames = 8;
    /// <summary>After a near birth or a camera jump, the gate also waits (at most this many frames after the last near birth)
    /// until every chunk square within StreamGateRadius exists: the map builds ring by ring, so a ring-2 chunk 25 m away can
    /// appear 10-20 frames after the rest (r7b J2), which re-held an already fading reflection.</summary>
    internal static int GateBuildWaitFrames = 60;
    /// <summary>A camera move longer than this in one frame counts as a jump (a near birth for the gate) when a near chunk
    /// square is not built yet.</summary>
    internal static float GateJumpMeters = 8f;
    /// <summary>BLAS budget per render event while any camera is held (triangles, builds); the plugin default otherwise.</summary>
    internal static int GateBlasTriangles = 120000, GateBlasBuilds = 48;
    internal static uint DebugMode;               // Native.DebugNone / DebugParity / DebugHitId
    internal static bool AllowAccumulation = true;
    internal static int AccumulationCap = 32;
    internal static bool BackSurfaceImage = true;
    internal static float EdgeFilterHigh = .8f, EdgeFilterUltra = .6f;
    /// <summary>Fix round 32 (review 2 art #2): the edge filter's strength INSIDE a lit lens (the whole 3x3 lit), 0..1 of the
    /// tier's strength (FRFrame.fogColor.w). Lens borders always get the compressed-domain 1-px AA.</summary>
    internal static float LensInteriorFilter = 1f;
    /// <summary>Shadowed lamps traced per hit, ranked per hit by their contribution (AD-2): the rest take the traced
    /// lamps' visibility. Was 4 / 16 one-ray lamps in camera-distance order before 2026-10-14.</summary>
    /// Fix pass run f3 (parity, 12 variants): with 4 lamps a per-hit ranking makes seams where the 5th lamp enters or
    /// leaves the set (carpet 5.6 %, camera order 4.6 %); with 6 it beats both (3.3 %), 8 matches 16 camera-order rays.
    internal static int HighShadowLamps = 6, UltraShadowLamps = 8;
    /// <summary>Soft shadows (AD-2), the lamp as a disc of ShadowLightRadius in the lens plane: High 1 ray per traced lamp
    /// to a point of the disc hashed from the hit's world position; Ultra 2 rays on a ring plus 2 more where they disagree
    /// (penumbra). ShadowLightRadius 0 = one ray to the lamp's centre (hard, the pre-2026-10-14 shadows).</summary>
    internal static int HighShadowRays = 1, UltraShadowRays = 2, HighExtraRays = 0, UltraExtraRays = 2;
    internal static float ShadowLightRadius = .2f;
    /// <summary>Harness only (FRFrame.fill.xyz): x 1 = untraced shadowed lamps at full light, y 1 = no per-hit ranking
    /// (the first lamps in camera-distance order), z 1 = fixed soft-shadow pattern (no world hash). 0 = shipped.</summary>
    internal static Vector3 HarnessShadowKnobs;
    /// <summary>AD-3: the smudge blur only where the pane smoothness is below this (smudges, prints, grime band).</summary>
    internal static float SmudgeBlurBelow = .85f;
    /// <summary>Harness: at most this many lamps per frame (default MaxLamps = 128; the cost ablation lowers it).</summary>
    internal static int LampCap = 128;

    // ------------------------------------------------------------------ capability
    static bool started, nativeChecked, nativeOk;
    static string nativeError = "";
    static int caps;
    static IntPtr eventFunc;
    static FrontRoomsGlassRTQuality? qualityOverride;

    public static IntPtr EventFunc => eventFunc;

    /// <summary>The quality RT runs at: the requested quality, but Off on a Mac that can never trace this session (no
    /// plugin, not Metal, no Apple9 / ray tracing / macOS 14, a failed compile, layout test or command buffer) and after a
    /// fail-closed (review 1 engineering #4: before, an Intel / M1 / M2 Mac, or any build without the plugin, reported Ultra
    /// and the renderer feature allocated 5-7 full-resolution float targets every session for nothing).</summary>
    public static FrontRoomsGlassRTQuality Quality
    {
        get
        {
            if (failedClosed || !CanTrace) return FrontRoomsGlassRTQuality.Off;
            return RequestedQuality;
        }
        // after a fail-closed RT stays Off for the session: a settings menu cannot switch it back on (codex audit F1)
        set
        {
            if (failedClosed) return;
            qualityOverride = value;
            explicitOverride = true;
            // fix round 33 (review 3 R3-B1): Off drops the stream-in burst BLAS budget at once (no camera traces, so no
            // GateWeight would recompute it); the gates themselves stay, and the next traced frame recomputes the burst
            if (value == FrontRoomsGlassRTQuality.Off) UpdateBlasBurst();
        }
    }

    /// <summary>What was asked for (setting, command line, the API, or the default from the quality level and the GPU core
    /// count, D1 (c)), whether or not this Mac traces.</summary>
    internal static FrontRoomsGlassRTQuality RequestedQuality
    {
        get
        {
            if (qualityOverride.HasValue) return qualityOverride.Value;
            var q = DefaultQuality();
            qualityOverride = q;
            return q;
        }
    }

    /// <summary>The quality was set through the API (a settings menu, the G10 proof's force-off): it survives the reset at
    /// the next Play start (G14V-4), where the derived default does not.</summary>
    static bool explicitOverride;

    /// <summary>This session can trace now or once the kernels and the layout self-test are done: the plugin is loaded on
    /// Metal, the hardware qualifies and nothing failed. False = no per-frame work, no targets, no native calls.</summary>
    internal static bool CanTrace
    {
        get
        {
            CheckNative();
            return nativeOk && !WebGLTargetInEditor && !PermanentlyOff;
        }
    }

    /// <summary>The per-frame build threw once this session (codex audit F1): RT is Off, the public API is a no-op and
    /// nothing calls the plugin any more (design 10 §2.7).</summary>
    static bool failedClosed;
    internal static bool IsFailedClosed => failedClosed;

    internal static FrontRoomsGlassRTQuality DefaultQuality()
    {
        string[] args = null;
        var pref = "";
        try { args = Environment.GetCommandLineArgs(); } catch { }
        try { pref = PlayerPrefs.GetString("FrontRooms.GlassRT", ""); } catch { }
        var level = 0;
        try { level = QualitySettings.GetQualityLevel(); } catch { }
        // the explicit switches first: the core count is only read when the default needs it (it is cached anyway)
        if (TryExplicit(args, pref, out var q)) return q;
        return GatedDefault(level, GpuCoreCount(out _));
    }

    // ------------------------------------------------------------------ D1 (c): the GPU-core gate of the default
    /// <summary>Red's D1 (c) (2026-10-22 12:36, Documentation/research/glass/rt/NOTE_1022_red_decisions.md): RT is on by
    /// DEFAULT only on GPUs with at least this many cores (M3/M4 Pro and Max and up). Smaller GPUs start with today's glass
    /// (Off) and can still switch RT on explicitly: -frGlassRT, the FrontRooms.GlassRT setting or the Quality API.</summary>
    internal const int DefaultHighMinCores = 16;

    /// <summary>EditMode tests / harness: a forced GPU core count (0 or less = unknown). Null = the -frGlassRTCores
    /// argument if given, else the IORegistry. Reset at every Play start.</summary>
    internal static int? HarnessCoreCount;

    /// <summary>Why the last IORegistry read gave no count ("" when it gave one). Harness only.</summary>
    internal static string CoreReadNote = "";

    static bool ioregRead;
    static int ioregCores = -1;

    /// <summary>The default rule, pure (no global state; the EditMode test calls it): an explicit -frGlassRT value, then
    /// the FrontRooms.GlassRT setting, then D1 (c): High at quality level 3 and up on a GPU with at least
    /// DefaultHighMinCores cores, Off otherwise (fewer cores, or unknown). Ultra is never a default.</summary>
    internal static FrontRoomsGlassRTQuality DefaultFor(string[] args, string pref, int level, int cores)
    {
        return TryExplicit(args, pref, out var q) ? q : GatedDefault(level, cores);
    }

    /// <summary>D1 (c) without the explicit switches. cores 0 or less = unknown = Off (fail closed).</summary>
    internal static FrontRoomsGlassRTQuality GatedDefault(int level, int cores)
    {
        return level >= 3 && cores >= DefaultHighMinCores ? FrontRoomsGlassRTQuality.High : FrontRoomsGlassRTQuality.Off;
    }

    static bool TryExplicit(string[] args, string pref, out FrontRoomsGlassRTQuality q)
    {
        if (args != null)
            for (var i = 0; i + 1 < args.Length; i++)
                if (args[i] == "-frGlassRT" && TryParse(args[i + 1], out q)) return true;
        return TryParse(pref, out q);
    }

    /// <summary>-frGlassRTCores &lt;n&gt; (test override, harmless in store builds): true when the argument is given;
    /// cores = n, or -1 (unknown) for 0, a negative number, "unknown" or anything that is not a number.</summary>
    internal static bool TryCoresArgument(string[] args, out int cores)
    {
        cores = -1;
        if (args == null) return false;
        for (var i = 0; i + 1 < args.Length; i++)
        {
            if (args[i] != "-frGlassRTCores") continue;
            if (int.TryParse(args[i + 1], out var n) && n > 0) cores = n;
            return true;
        }
        return false;
    }

    /// <summary>The GPU core count the default uses, -1 when unknown, and where it came from: "override" (HarnessCoreCount
    /// or -frGlassRTCores), "IORegistry", or "none" (no count: Intel / AMD, no service or property, a sandbox denial, an
    /// exception). Never throws.</summary>
    internal static int GpuCoreCount(out string source)
    {
        try
        {
            if (HarnessCoreCount.HasValue)
            {
                source = "override";
                return HarnessCoreCount.Value > 0 ? HarnessCoreCount.Value : -1;
            }
            if (TryCoresArgument(Environment.GetCommandLineArgs(), out var forced)) { source = "override"; return forced; }
            if (!ioregRead) { ioregRead = true; ioregCores = ReadIORegistryCores(AgxClass, CoreCountKey, out CoreReadNote); }
            source = ioregCores > 0 ? "IORegistry" : "none";
            return ioregCores;
        }
        catch (Exception e)
        {
            CoreReadNote = e.GetType().Name;
            source = "none";
            return -1;
        }
    }

    // IOKit / CoreFoundation, read-only: the Apple GPU's AGXAccelerator service carries "gpu-core-count" (what
    // `ioreg -l | grep gpu-core-count` shows). No user client is opened and nothing is written. No dylib change.
    internal const string AgxClass = "AGXAccelerator", CoreCountKey = "gpu-core-count";
    const string IOKitLib = "/System/Library/Frameworks/IOKit.framework/IOKit";
    const string CoreFoundationLib = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    const uint CFStringEncodingUTF8 = 0x08000100;
    const int CFNumberSInt64Type = 4;
    [DllImport(IOKitLib)] static extern IntPtr IOServiceMatching([MarshalAs(UnmanagedType.LPStr)] string name);
    [DllImport(IOKitLib)] static extern uint IOServiceGetMatchingService(uint mainPort, IntPtr matching);
    [DllImport(IOKitLib)] static extern IntPtr IORegistryEntryCreateCFProperty(uint entry, IntPtr key, IntPtr allocator, uint options);
    [DllImport(IOKitLib)] static extern int IOObjectRelease(uint obj);
    [DllImport(CoreFoundationLib)] static extern IntPtr CFStringCreateWithCString(IntPtr allocator, [MarshalAs(UnmanagedType.LPStr)] string text, uint encoding);
    [DllImport(CoreFoundationLib)] static extern UIntPtr CFGetTypeID(IntPtr cf);
    [DllImport(CoreFoundationLib)] static extern UIntPtr CFNumberGetTypeID();
    [DllImport(CoreFoundationLib)] [return: MarshalAs(UnmanagedType.I1)] static extern bool CFNumberGetValue(IntPtr number, int type, out long value);
    [DllImport(CoreFoundationLib)] static extern void CFRelease(IntPtr cf);

    /// <summary>One IORegistry read: the first service of class className (subclasses match: AGXAcceleratorG15X ...), its
    /// numeric property key. -1 on any failure, with the reason in note. Never throws. Internal for the EditMode test
    /// (a missing class and a non-numeric key must both give -1).</summary>
    internal static int ReadIORegistryCores(string className, string key, out string note)
    {
        note = "";
        uint service = 0;
        IntPtr cfKey = IntPtr.Zero, prop = IntPtr.Zero;
        try
        {
            var matching = IOServiceMatching(className);
            if (matching == IntPtr.Zero) { note = "IOServiceMatching returned null"; return -1; }
            service = IOServiceGetMatchingService(0, matching);   // kIOMainPortDefault; consumes 'matching'
            if (service == 0) { note = "no " + className + " service (not an Apple GPU)"; return -1; }
            cfKey = CFStringCreateWithCString(IntPtr.Zero, key, CFStringEncodingUTF8);
            if (cfKey == IntPtr.Zero) { note = "CFStringCreateWithCString failed"; return -1; }
            prop = IORegistryEntryCreateCFProperty(service, cfKey, IntPtr.Zero, 0);
            if (prop == IntPtr.Zero) { note = "no " + key + " property"; return -1; }
            if (CFGetTypeID(prop) != CFNumberGetTypeID()) { note = key + " is not a number"; return -1; }
            if (!CFNumberGetValue(prop, CFNumberSInt64Type, out var n) || n <= 0 || n > 100000) { note = key + " has no usable value"; return -1; }
            return (int)n;
        }
        catch (Exception e)
        {
            note = e.GetType().Name + ": " + e.Message;   // DllNotFound, EntryPointNotFound, a sandbox denial ...
            return -1;
        }
        finally
        {
            try
            {
                if (prop != IntPtr.Zero) CFRelease(prop);
                if (cfKey != IntPtr.Zero) CFRelease(cfKey);
                if (service != 0) IOObjectRelease(service);
            }
            catch { }
        }
    }

    /// <summary>The one startup line of D1 (c): the core count, where it came from, and the default it gives (before any
    /// explicit switch). Logged once per session from EnsureStarted. Never throws.</summary>
    static void LogDefault()
    {
        try
        {
            var level = QualitySettings.GetQualityLevel();
            var cores = GpuCoreCount(out var source);
            var d = GatedDefault(level, cores) == FrontRoomsGlassRTQuality.High ? "High" : "Off";
            Debug.Log("[FrontRoomsGlassRT] GlassRT default: cores=" + (cores > 0 ? cores.ToString() : "unknown") + " source=" + source
                      + " default=" + d + " (level " + level + ")");
        }
        catch { }
    }

    /// <summary>An exception escaped the per-frame RT build: RT is Off for this session (design 10 §2.7, fail closed;
    /// codex audit F1). The exception is logged once, plus one line saying why RT is off.</summary>
    internal static void FailClosed(Exception e, string where = "while building the RT frame")
    {
        // one console entry (design 10 §2.7): why RT is off, where it threw (fix round 31: the API entry points and the
        // map / GD3 handlers fail closed too), then the exception with its stack
        if (!failedClosed)
            Debug.LogError("[FrontRoomsGlassRT] off for this session: " + e.GetType().Name + " " + where + " (fail closed, design 10 §2.7)\n" + e);
        qualityOverride = FrontRoomsGlassRTQuality.Off;
        failedClosed = true;
        FailedClosed++;
        UpdateBlasBurst();   // fix round 33 (R3-B1): no burst BLAS budget after a fail-closed (one guarded native store)
    }

    /// <summary>Harness: how many times the per-frame build failed closed this session.</summary>
    internal static int FailedClosed;

    /// <summary>Harness only (the fail-closed test of every entry point, fix round 31): undo a fail-closed so the next fault
    /// site can be tested in the same session. The game never calls it: after a fail-closed RT stays Off.</summary>
    internal static void HarnessRearm(FrontRoomsGlassRTQuality quality)
    {
        failedClosed = false;
        qualityOverride = quality;
    }

    static bool TryParse(string s, out FrontRoomsGlassRTQuality q)
    {
        q = FrontRoomsGlassRTQuality.Off;
        switch ((s ?? "").Trim().ToLowerInvariant())
        {
            case "off": q = FrontRoomsGlassRTQuality.Off; return true;
            case "high": q = FrontRoomsGlassRTQuality.High; return true;
            case "ultra": q = FrontRoomsGlassRTQuality.Ultra; return true;
        }
        return false;
    }

    static void CheckNative()
    {
        if (nativeChecked) return;
        nativeChecked = true;
        if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Metal) { nativeError = "graphics API is " + SystemInfo.graphicsDeviceType + ", not Metal"; return; }
        try
        {
            // fix round 32 (review 2 engineering #7): the LOADED plugin's ABI and struct sizes, before any other native call.
            // A plugin older than this check has no FRGlassRT_GetAbi: EntryPointNotFoundException below ("an old build").
            if (!Native.AbiCheck(out var abiError)) { nativeError = "plugin ABI: " + abiError; return; }
            caps = Native.FRGlassRT_QueryCaps();
            eventFunc = Native.FRGlassRT_GetRenderEventFunc();
            nativeOk = eventFunc != IntPtr.Zero;
            if (!nativeOk) nativeError = "plugin returned no render event";
        }
        catch (DllNotFoundException e) { nativeError = "plugin not loaded: " + e.Message; }
        catch (EntryPointNotFoundException e) { nativeError = "plugin is an old build: " + e.Message; }
        catch (Exception e) { nativeError = "plugin error: " + e.GetType().Name + " " + e.Message; }
    }

    // Caps are read once per frame at most (review 1 engineering #4: Supported, Status, PermanentlyOff and Quality each
    // called FRGlassRT_QueryCaps, several times per frame). They only change while the kernels compile and the layout
    // test runs, or when a command buffer fails; once the hardware or a failure rules RT out the answer is latched and
    // no native call is made again.
    static int capsFrame = -1;
    static bool offLatched;

    static int Caps
    {
        get
        {
            if (!nativeOk) return 0;
            var f = Time.frameCount;
            if (f != capsFrame || f == 0)
            {
                capsFrame = f;
                caps = Native.FRGlassRT_QueryCaps();
            }
            return caps;
        }
    }

    static bool WebGLTargetInEditor
    {
        get
        {
#if UNITY_EDITOR
            return UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.WebGL;
#else
            return false;
#endif
        }
    }

    public static bool Supported
    {
        get
        {
            CheckNative();
            if (!nativeOk || WebGLTargetInEditor) return false;
            var c = Caps;
            const int need = Native.CapDevice | Native.CapRaytracing | Native.CapApple9 | Native.CapMetal3 | Native.CapOS14 | Native.CapKernelReady | Native.CapLayoutOK;
            const int bad = Native.CapGpuError | Native.CapLayoutFailed | Native.CapCompileFailed;
            return (c & need) == need && (c & bad) == 0;
        }
    }

    public static string Status
    {
        get
        {
            CheckNative();
            if (!nativeOk) return "off: " + nativeError;
            if (WebGLTargetInEditor) return "off: the editor's active build target is WebGL (the WebGL tier has no RT)";
            var c = Caps;
            if ((c & Native.CapDevice) == 0) return "off: no Metal device reached the plugin (UnityPluginLoad / device init missing)";
            if ((c & Native.CapRaytracing) == 0) return "off: this GPU has no Metal ray tracing";
            if ((c & Native.CapApple9) == 0) return "off: not an Apple9 GPU (M1/M2 trace in software: P2 tier, off by default)";
            if ((c & Native.CapOS14) == 0) return "off: macOS 14 or newer is required";
            if ((c & Native.CapCompileFailed) != 0) return "off: kernel compile failed: " + Native.LastError();
            if ((c & Native.CapLayoutFailed) != 0) return "off: GPU layout self-test failed: " + Native.LastError();
            if ((c & Native.CapGpuError) != 0) return "off: a command buffer failed this session: " + Native.LastError();
            if ((c & Native.CapKernelReady) == 0) return "waiting: kernel compiling";
            if ((c & Native.CapLayoutOK) == 0) return "waiting: layout self-test";
            var residency = Native.Get(Native.Stat.ResidencyMode) == 1 ? "residency set" : "useResource";
            return "on: Apple9, " + Quality + ", kernel " + Native.Get(Native.Stat.CompileMs).ToString("0") + " ms (async), layout OK, " + residency
                + ", hazard depth " + Native.Get(Native.Stat.HazardDepth) + " / out " + Native.Get(Native.Stat.HazardOut)
                + (Native.Get(Native.Stat.Barriers) > 0 ? ", event barriers on" : ", event barriers off (tracked)")
                + ", timers " + ((c & Native.CapCounters) != 0 ? "stage-boundary counters" : "command buffer only");
        }
    }

    public static bool Active => Supported && Quality != FrontRoomsGlassRTQuality.Off;

    /// <summary>Hardware or a failure rules RT out for this session (no scanning, no native calls per frame).</summary>
    static bool PermanentlyOff
    {
        get
        {
            if (offLatched) return true;
            var c = Caps;
            const int need = Native.CapDevice | Native.CapRaytracing | Native.CapApple9 | Native.CapMetal3 | Native.CapOS14;
            const int bad = Native.CapGpuError | Native.CapLayoutFailed | Native.CapCompileFailed;
            // CapDevice arrives with the graphics device event, before any script runs; without it RT never starts
            var off = (c & need) != need || (c & bad) != 0;
            if (off && (c & Native.CapDevice) != 0) offLatched = true;
            return off;
        }
    }

    // ------------------------------------------------------------------ cameras
    static int nextCameraId = 1;
    static readonly List<FrontRoomsGlassRTCamera> cameras = new List<FrontRoomsGlassRTCamera>();

    public static void OptIn(Camera camera)
    {
        // Edit-mode cameras never trace (the feature needs Play Mode). Adding the component there saved it into scenes
        // (FrontRoomsScreenAdsDemo.unity), where it is a missing script on every non-macOS editor and player (codex audit F5).
        if (camera == null || !Application.isPlaying) return;
        EnsureStarted();
        // a Mac that can never trace (no plugin, Intel, M1/M2, macOS < 14, a failed compile) gets no per-camera state at all
        if (!CanTrace && (!nativeOk || offLatched)) return;
        if (!camera.TryGetComponent<FrontRoomsGlassRTCamera>(out var state)) state = camera.gameObject.AddComponent<FrontRoomsGlassRTCamera>();
        if (state.id < 0) { state.id = nextCameraId++; cameras.Add(state); }
    }

    public static void ReleaseCamera(FrontRoomsGlassRTCamera state)
    {
        if (state == null || state.id < 0) return;
        cameras.Remove(state);
        // fix round 33 (review 3 R3-B1): the camera's stream-in gate goes with it. Camera ids are never reused, so a gate
        // left behind by a camera destroyed while held (the desktop restart reloads the scene) stayed held for the rest of
        // the session and kept the burst BLAS budget (120k triangles / 48 builds per event) on instead of the default.
        gates.Remove(state.id);
        if (nativeOk) Native.FRGlassRT_FreeCamera(state.id);
        state.id = -1;
        UpdateBlasBurst();
    }

    public static void EnsureStarted()
    {
        if (started) return;
        started = true;
        CheckNative();
        LogDefault();   // D1 (c): one line, cores / source / default
        if (nativeOk) Native.FRGlassRT_ResetScene();   // a new session: the render thread drops the previous scene (generation-safe)
        if (nativeOk) Debug.Log("[FrontRoomsGlassRT] plugin loaded · caps 0x" + Caps.ToString("x") + " · " + Status);
        else Debug.Log("[FrontRoomsGlassRT] " + Status);
        // GD3's stage meshes (S1 fins, S2 pieces) are made under the window root during the hold, after the chunk scan:
        // register them when GD3 says they exist (MeshesReady, before the beats), and again when a stage shows (G14V-1)
        if (nativeOk && !PermanentlyOff)
        {
            FrontRoomsGlassBreakable.MeshesReady -= OnGlassMeshesReady;
            FrontRoomsGlassBreakable.MeshesReady += OnGlassMeshesReady;
            FrontRoomsGlassBreakable.StageChanged -= OnGlassStageChanged;
            FrontRoomsGlassBreakable.StageChanged += OnGlassStageChanged;
        }
    }

    // ------------------------------------------------------------------ object ids
    static int ObjectId(Object o) => FrontRoomsObjectId.Of(o);

    // ------------------------------------------------------------------ registry: meshes
    sealed class MeshEntry
    {
        public Mesh mesh;
        public int id;                  // the Mesh's instance id at registration (still valid after the Mesh is destroyed)
        public int key;
        public int refs;
        public int triangles;
        public int[] geometrySubmesh;   // geometry index -> Unity submesh index
        public bool deforming, failed, gpuPath;
    }

    static readonly Dictionary<int, MeshEntry> meshById = new Dictionary<int, MeshEntry>();
    static readonly Dictionary<int, MeshEntry> meshByKey = new Dictionary<int, MeshEntry>();
    static int nextMeshKey;
    static int meshesUploadedCPU, meshesUploadedGPU, meshUploadFailures;

    static MeshEntry AcquireMesh(Mesh mesh, bool deforming)
    {
        if (mesh == null) return null;
        var id = ObjectId(mesh);
        if (meshById.TryGetValue(id, out var e))
        {
            if (e.failed) return null;
            e.refs++;
            return e;
        }
        e = new MeshEntry { mesh = mesh, id = id, key = nextMeshKey++, deforming = deforming };
        if (!Upload(e)) { e.failed = true; meshById[id] = e; meshUploadFailures++; return null; }
        e.refs = 1;
        meshById[id] = e;
        meshByKey[e.key] = e;
        return e;
    }

    static void ReleaseMeshRef(MeshEntry e)
    {
        if (e == null || --e.refs > 0) return;
        if (nativeOk && !failedClosed) Native.FRGlassRT_ReleaseMesh(e.key);
        meshByKey.Remove(e.key);
        // by the id kept at registration: a chunk drop destroys its Meshes first, and the old "find the entry by
        // reference" scan over every mesh ran once per released mesh (O(n^2): 4.4 ms for the 4-chunk drop of walk frame
        // 113, fix round 31)
        if (meshById.TryGetValue(e.id, out var same) && same == e) meshById.Remove(e.id);
    }

    static bool Upload(MeshEntry e)
    {
        var mesh = e.mesh;
        var subs = new List<int>();
        for (var s = 0; s < mesh.subMeshCount; s++)
            if (mesh.GetTopology(s) == MeshTopology.Triangles && mesh.GetIndexCount(s) >= 3) subs.Add(s);
        if (subs.Count == 0 || mesh.vertexCount == 0) return false;
        e.geometrySubmesh = subs.ToArray();
        if (mesh.isReadable) return UploadCPU(e, subs);
        return UploadGPU(e, subs);
    }

    // Reused upload scratch (main thread only): a chunk build registers hundreds of meshes, and per-mesh arrays from
    // mesh.vertices / normals / uv / colors / GetIndices fed the GC enough garbage for 80-300 ms collections in the
    // 600-frame walk (run 7). The plugin copies exactly `count` elements, so the arrays may be longer than the mesh;
    // they go in as pinned pointers (E1), never as marshalled arrays.
    static readonly List<Vector3> upVerts = new List<Vector3>(4096), upNormals = new List<Vector3>(4096);
    static readonly List<Vector2> upUVs = new List<Vector2>(4096);
    static readonly List<Color> upColors = new List<Color>(256);
    static readonly List<int> upIndexList = new List<int>(8192);
    static Vector3[] upVertArray = new Vector3[4096];
    static Native.VertexAttr[] upAttrArray = new Native.VertexAttr[4096];
    static int[] upIndexArray = new int[8192];
    static int[] upStarts = new int[16], upCounts = new int[16];

    static void Grow<T>(ref T[] a, int n) { if (a.Length < n) a = new T[Mathf.NextPowerOfTwo(n)]; }

    static bool UploadCPU(MeshEntry e, List<int> subs)
    {
        var mesh = e.mesh;
        mesh.GetVertices(upVerts);
        mesh.GetNormals(upNormals);
        mesh.GetUVs(0, upUVs);
        mesh.GetColors(upColors);
        var n = upVerts.Count;
        Grow(ref upVertArray, n);
        Grow(ref upAttrArray, n);
        upVerts.CopyTo(upVertArray);
        var hasN = upNormals.Count == n;
        var hasUV = upUVs.Count == n;
        var hasC = upColors.Count == n;
        for (var i = 0; i < n; i++)
        {
            var a = new Native.VertexAttr();
            if (hasN) { var v = upNormals[i]; a.nx = v.x; a.ny = v.y; a.nz = v.z; } else a.ny = 1f;
            if (hasUV) { var t = upUVs[i]; a.u = t.x; a.v = t.y; }
            if (hasC) a.colorR = upColors[i].r;
            upAttrArray[i] = a;
        }
        Grow(ref upStarts, subs.Count);
        Grow(ref upCounts, subs.Count);
        var total = 0;
        for (var k = 0; k < subs.Count; k++)
        {
            mesh.GetIndices(upIndexList, subs[k], true);
            var c = upIndexList.Count - upIndexList.Count % 3;
            Grow(ref upIndexArray, total + c);
            upIndexList.CopyTo(0, upIndexArray, total, c);
            upStarts[k] = total;
            upCounts[k] = c;
            total += c;
        }
        e.triangles = total / 3;
        var tn = Stopwatch.GetTimestamp();
        int r;
        using (var pins = new Native.Pins(upVertArray, upAttrArray, upIndexArray, upStarts, upCounts))
            r = Native.FRGlassRT_UploadMeshCPU(e.key, pins.A, pins.B, n, pins.C, total, pins.D, pins.E, subs.Count, e.deforming ? 1 : 0, 0);
        var dn = Stopwatch.GetTimestamp() - tn;
        RegProfile[2] += dn;
        if (dn > RegProfile[3]) RegProfile[3] = dn;
        if (r != 0) { Warn("mesh " + mesh.name + " upload failed (" + r + ")"); return false; }
        meshesUploadedCPU++;
        return true;
    }

    static uint FormatCode(VertexAttributeFormat f)
    {
        switch (f)
        {
            case VertexAttributeFormat.Float32: return 1;
            case VertexAttributeFormat.Float16: return 2;
            case VertexAttributeFormat.UNorm8: return 3;
            case VertexAttributeFormat.SNorm8: return 4;
            case VertexAttributeFormat.UNorm16: return 5;
            case VertexAttributeFormat.SNorm16: return 6;
        }
        return 0;
    }

    // Non-readable meshes (the kit FBX): Unity's native buffers, fetched ONCE per Mesh per session, gathered into the
    // plugin's own copy on the render thread (fr_gather_*). Unity's buffers are released when that command buffer completes.
    static bool UploadGPU(MeshEntry e, List<int> subs)
    {
        var mesh = e.mesh;
        if (!mesh.HasVertexAttribute(VertexAttribute.Position) || mesh.GetVertexAttributeFormat(VertexAttribute.Position) != VertexAttributeFormat.Float32
            || mesh.GetVertexAttributeDimension(VertexAttribute.Position) < 3)
        { Warn("mesh " + mesh.name + ": positions are not Float32x3; skipped"); return false; }
        var streams = new IntPtr[4];
        var args = new Native.GatherVertexArgs { vertexCount = (uint)mesh.vertexCount };
        void Attr(VertexAttribute a, ref uint stride, ref uint offset, ref uint format, int slot)
        {
            if (!mesh.HasVertexAttribute(a)) { format = 0; return; }
            var s = mesh.GetVertexAttributeStream(a);
            if (s < 0 || s > 3) { format = 0; return; }
            if (streams[slot] == IntPtr.Zero) streams[slot] = mesh.GetNativeVertexBufferPtr(s);
            stride = (uint)mesh.GetVertexBufferStride(s);
            offset = (uint)mesh.GetVertexAttributeOffset(a);
            format = FormatCode(mesh.GetVertexAttributeFormat(a));
        }
        uint posFormat = 0;
        Attr(VertexAttribute.Position, ref args.posStride, ref args.posOffset, ref posFormat, 0);
        Attr(VertexAttribute.Normal, ref args.nrmStride, ref args.nrmOffset, ref args.nrmFormat, 1);
        Attr(VertexAttribute.TexCoord0, ref args.uvStride, ref args.uvOffset, ref args.uvFormat, 2);
        Attr(VertexAttribute.Color, ref args.colStride, ref args.colOffset, ref args.colFormat, 3);
        if (streams[0] == IntPtr.Zero) { Warn("mesh " + mesh.name + ": no native vertex buffer"); return false; }
        var ib = mesh.GetNativeIndexBufferPtr();
        if (ib == IntPtr.Zero) { Warn("mesh " + mesh.name + ": no native index buffer"); return false; }
        var indexSize = mesh.indexFormat == IndexFormat.UInt16 ? 2u : 4u;
        var ia = new Native.GatherIndexArgs[subs.Count];
        uint dst = 0;
        for (var k = 0; k < subs.Count; k++)
        {
            var count = (uint)mesh.GetIndexCount(subs[k]);
            count -= count % 3;
            ia[k] = new Native.GatherIndexArgs { count = count, indexSize = indexSize, srcStart = (uint)mesh.GetIndexStart(subs[k]), dstStart = dst, baseVertex = (uint)mesh.GetBaseVertex(subs[k]) };
            dst += count;
        }
        e.triangles = (int)(dst / 3);
        e.gpuPath = true;
        var r = Native.FRGlassRT_UploadMeshGPU(e.key, streams[0], streams[1], streams[2], streams[3], ref args, ib, ia, subs.Count, (int)dst);
        if (r != 0) { Warn("mesh " + mesh.name + " GPU upload failed (" + r + ")"); return false; }
        meshesUploadedGPU++;
        return true;
    }

    // ------------------------------------------------------------------ registry: materials and textures
    // Reference-counted (E3, 2026-10-14): a material block (one record per submesh of a renderer's material list) lives
    // while an instance uses it, a texture slot while a block references it. Before, both tables only grew: content that
    // makes a material per object (a CRT's ad surface, the glow placard) added blocks for the whole session (93 -> 120
    // over a 3-lap streaming soak), every texture ever seen stayed referenced, and at the 4,096 cap a new material
    // silently took record 0. Freed ranges and slots are reused only FreeDelayFrames later, so a frame still in flight
    // never reads a record or a texture that changed under it.
    sealed class MatBlock
    {
        public int id;
        public string key;            // multi-material blocks; null for a single material (keyed by id)
        public int first, count, refs;
        public readonly List<int> textures = new List<int>();   // texture instance ids this block's records reference
    }

    sealed class TexSlot { public int slot, refs; }

    const int FreeDelayFrames = 12;
    static readonly Dictionary<int, MatBlock> blockSingle = new Dictionary<int, MatBlock>();
    static readonly Dictionary<string, MatBlock> blockMulti = new Dictionary<string, MatBlock>();
    static readonly Dictionary<int, TexSlot> textureSlot = new Dictionary<int, TexSlot>();
    static readonly List<(int first, int count, int frame)> freeMaterialRanges = new List<(int, int, int)>();
    static readonly Queue<(int slot, int frame)> freeTextureSlots = new Queue<(int, int)>();
    static int nextMaterial, nextTexture = 4, materialsLive, texturesLive;
    static bool warnedMaterials, warnedTextures;
    static List<int> describeTextures;   // collects the textures Describe takes for the block being described
    static readonly MatBlock fallbackBlock = new MatBlock { first = 0, count = 1, refs = int.MaxValue / 2 };
    static readonly System.Text.StringBuilder keyBuilder = new System.Text.StringBuilder();

    static MatBlock AcquireBlock(Material[] materials, int[] geometrySubmesh)
    {
        MatBlock b;
        if (geometrySubmesh.Length == 1)
        {
            var m = Pick(materials, geometrySubmesh[0]);
            var id = m != null ? ObjectId(m) : 0;
            if (blockSingle.TryGetValue(id, out b)) { b.refs++; return b; }
            b = NewBlock(1);
            if (b == null) return fallbackBlock;
            b.id = id;
            blockSingle[id] = b;
            DescribeInto(b, new[] { m });
        }
        else
        {
            keyBuilder.Clear();
            foreach (var s in geometrySubmesh) { var m = Pick(materials, s); keyBuilder.Append(m != null ? ObjectId(m) : 0).Append(','); }
            var key = keyBuilder.ToString();
            if (blockMulti.TryGetValue(key, out b)) { b.refs++; return b; }
            b = NewBlock(geometrySubmesh.Length);
            if (b == null) return fallbackBlock;
            b.key = key;
            blockMulti[key] = b;
            var list = new Material[geometrySubmesh.Length];
            for (var k = 0; k < list.Length; k++) list[k] = Pick(materials, geometrySubmesh[k]);
            DescribeInto(b, list);
        }
        b.refs = 1;
        return b;
    }

    static MatBlock NewBlock(int count)
    {
        var first = -1;
        for (var k = 0; k < freeMaterialRanges.Count; k++)
        {
            var fr = freeMaterialRanges[k];
            if (fr.count < count || Time.frameCount - fr.frame < FreeDelayFrames) continue;
            first = fr.first;
            if (fr.count == count) freeMaterialRanges.RemoveAt(k);
            else freeMaterialRanges[k] = (fr.first + count, fr.count - count, fr.frame);
            break;
        }
        if (first < 0)
        {
            if (nextMaterial + count > MaxMaterials) { WarnCap(ref warnedMaterials, "material table full (" + MaxMaterials + " records): new materials are traced with record 0"); return null; }
            first = nextMaterial;
            nextMaterial += count;
        }
        materialsLive += count;
        if (materialsLive > MaxMaterials * 9 / 10) WarnCap(ref warnedMaterials, "material table above 90 % (" + materialsLive + " of " + MaxMaterials + " records)");
        return new MatBlock { first = first, count = count };
    }

    static void ReleaseBlock(MatBlock b)
    {
        if (b == null || b == fallbackBlock || --b.refs > 0) return;
        if (b.key != null) blockMulti.Remove(b.key);
        else if (blockSingle.TryGetValue(b.id, out var same) && same == b) blockSingle.Remove(b.id);
        foreach (var t in b.textures) ReleaseTexture(t);
        b.textures.Clear();
        freeMaterialRanges.Add((b.first, b.count, Time.frameCount));
        materialsLive -= b.count;
    }

    static void WarnCap(ref bool warned, string what)
    {
        if (warned) return;
        warned = true;
        Warn(what);
    }

    static Material Pick(Material[] materials, int submesh)
    {
        if (materials == null || materials.Length == 0) return null;
        return materials[Mathf.Min(submesh, materials.Length - 1)];
    }

    /// <summary>Harness: the material at each material-table index (parity log).</summary>
    internal static readonly List<string> MaterialNames = new List<string>();

    /// <summary>(Re)describe a block's materials into its records; the textures they take are counted on the block.</summary>
    static void DescribeInto(MatBlock b, Material[] list)
    {
        var old = b.textures.Count > 0 ? b.textures.ToArray() : null;
        b.textures.Clear();
        describeTextures = b.textures;
        var recs = new Native.Material[list.Length];
        try { for (var i = 0; i < list.Length; i++) recs[i] = Describe(list[i]); }
        finally { describeTextures = null; }
        if (old != null) foreach (var t in old) ReleaseTexture(t);
        while (MaterialNames.Count < b.first + list.Length) MaterialNames.Add("");
        for (var i = 0; i < list.Length; i++) MaterialNames[b.first + i] = list[i] != null ? list[i].name + " (" + (list[i].shader != null ? list[i].shader.name : "?") + ")" : "none";
        using (var pins = new Native.Pins(recs)) Native.FRGlassRT_SetMaterials(b.first, pins.A, recs.Length);
    }

    /// <summary>Re-read a material (e.g. after LiveApplied or a harness change).</summary>
    internal static void RefreshMaterial(Material m)
    {
        if (m == null || failedClosed) return;
        if (blockSingle.TryGetValue(ObjectId(m), out var b)) DescribeInto(b, new[] { m });
    }

    static int Slot(Material m, string prop, int fallback)
    {
        if (m == null || !m.HasProperty(prop)) return fallback;
        var t = m.GetTexture(prop);
        return TextureSlotOf(t, fallback);
    }

    static int TextureSlotOf(Texture t, int fallback)
    {
        if (t == null) return fallback;
        if (t.dimension != TextureDimension.Tex2D) return fallback;
        var id = ObjectId(t);
        if (!textureSlot.TryGetValue(id, out var ts))
        {
            var reuse = freeTextureSlots.Count > 0 && Time.frameCount - freeTextureSlots.Peek().frame >= FreeDelayFrames;
            if (!reuse && nextTexture >= MaxTextures) { WarnCap(ref warnedTextures, "texture table full (" + MaxTextures + " slots): new textures are traced as white"); return fallback; }
            var tw = Stopwatch.StartNew();
            var ptr = t.GetNativeTexturePtr();   // synchronises with the render thread: once per texture while it is referenced
            var tms = tw.Elapsed.TotalMilliseconds;
            TexturePtrCalls++; TexturePtrMsTotal += tms; if (tms > TexturePtrMsMax) TexturePtrMsMax = tms;
            if (ptr == IntPtr.Zero) return fallback;
            ts = new TexSlot { slot = reuse ? freeTextureSlots.Dequeue().slot : nextTexture++ };
            Native.FRGlassRT_SetTexture(ts.slot, ptr);
            textureSlot[id] = ts;
            texturesLive++;
            if (texturesLive > (MaxTextures - 4) * 9 / 10) WarnCap(ref warnedTextures, "texture table above 90 % (" + texturesLive + " of " + (MaxTextures - 4) + " slots)");
        }
        ts.refs++;
        describeTextures?.Add(id);
        return ts.slot;
    }

    static void ReleaseTexture(int textureId)
    {
        if (!textureSlot.TryGetValue(textureId, out var ts) || --ts.refs > 0) return;
        textureSlot.Remove(textureId);
        // the plugin retires the texture after the frames in flight and puts the white default in its slot
        if (nativeOk && !failedClosed) Native.FRGlassRT_SetTexture(ts.slot, IntPtr.Zero);
        freeTextureSlots.Enqueue((ts.slot, Time.frameCount));
        texturesLive--;
    }

    static Vector4 Lin(Material m, string prop, Color fallback)
    {
        var c = m != null && m.HasProperty(prop) ? m.GetColor(prop) : fallback;
        var l = c.linear;
        return new Vector4(l.r, l.g, l.b, c.a);
    }

    static float Fl(Material m, string prop, float fallback) => m != null && m.HasProperty(prop) ? m.GetFloat(prop) : fallback;

    internal static bool IsGlassMaterial(Material m)
    {
        if (m == null) return false;
        if (m.shader != null && m.shader.name == "FrontRooms/Glass") return true;
        return m.renderQueue >= 2450 && m.name.IndexOf("glass", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static bool IsReceiverMaterial(Material m) => m != null && m.shader != null && m.shader.name == "FrontRooms/Glass" && m.HasProperty(RTReceiveId) && m.GetFloat(RTReceiveId) > .5f;

    /// <summary>Which prepass draws this receiver: its own FRGlassRTPrepass pass (the glass track's shader with contract
    /// G-1: per-pixel roll / smudge / crack normal and smoothness) or, when the shader has none (the glass track's shader
    /// as merged on main before G-1), Hidden/FrontRooms/GlassRTPrepass (geometric normal, the material's _Smoothness).
    /// Without this check a receiver on a shader without the pass is never drawn into the prepass: no glass pixels, no RT.</summary>
    static int PrepassBitFor(Material m) => m != null && m.FindPass("FRGlassRTPrepass") >= 0 ? PrepassBit : OverrideBit;

    static Native.Material Describe(Material m)
    {
        var r = new Native.Material
        {
            baseColor = new Vector4(.5f, .5f, .5f, 1f), tile = Vector4.one, baseST = new Vector4(1, 1, 0, 0), p0 = new Vector4(.5f, 0f, 1f, 1f),
            kind = Native.KindFlat, texBase = Native.TexWhite, texMask = Native.TexWhite, texBump = Native.TexBump, texMacro = Native.TexGrey,
            texEmission = Native.TexWhite, texOcclusion = Native.TexWhite,
        };
        if (m == null) return r;
        var shader = m.shader != null ? m.shader.name : "";
        Vector4 St(string prop)
        {
            if (!m.HasProperty(prop)) return new Vector4(1, 1, 0, 0);
            var s = m.GetTextureScale(prop); var o = m.GetTextureOffset(prop);
            return new Vector4(s.x, s.y, o.x, o.y);
        }
        if (shader == "FrontRooms/Surface")
        {
            r.kind = Native.KindSurface;
            r.baseColor = Lin(m, "_BaseColor", Color.white);
            var tile = m.HasProperty("_TileSize") ? m.GetVector("_TileSize") : new Vector4(1, 1, 0, 0);
            r.tile = new Vector4(tile.x, tile.y, 0, 0);
            r.baseST = St("_BaseMap");
            r.stain = Lin(m, "_StainColor", new Color(.42f, .31f, .16f));
            r.stain.w = Fl(m, "_StainStrength", 0f);
            r.p0 = new Vector4(Fl(m, "_Smoothness", 1f), Fl(m, "_Metallic", 0f), Fl(m, "_OcclusionStrength", .8f), Fl(m, "_BumpScale", 1f));
            r.p1 = new Vector4(Fl(m, "_MacroTone", .35f), Fl(m, "_MacroDirt", .25f), Fl(m, "_WetStrength", 0f), Fl(m, "_FloorGrime", 0f));
            r.p2 = new Vector4(Fl(m, "_FloorGrimeHeight", .35f), Fl(m, "_CeilingGrime", 0f), Fl(m, "_CeilingHeight", 2.9f), 0f);
            r.texBase = (uint)Slot(m, "_BaseMap", Native.TexWhite);
            r.texMask = (uint)Slot(m, "_MaskMap", Native.TexWhite);
            r.texBump = (uint)Slot(m, "_BumpMap", Native.TexBump);
            r.texMacro = (uint)Slot(m, "_MacroMap", Native.TexGrey);
            if (m.IsKeywordEnabled("_FR_MESH_UV")) r.flags |= Native.MatMeshUV;
            if (m.IsKeywordEnabled("_FR_PRINT"))
            {
                // the wallpaper sandwich (FrontRoomsSurface.shader PrintAlbedo): _BaseMap is the paper modulation and
                // the colour comes from print frame 0 through the ink ramp (colours as the GPU sees them: linear)
                r.flags |= Native.MatPrint;
                r.printST = St("_PrintTex");
                r.inkGround = Lin(m, "_InkGround", new Color(.8235f, .7608f, .4863f));
                r.inkGround.w = Fl(m, "_PrintAmount", 1f);
                r.inkMid = Lin(m, "_InkMid", new Color(.6745f, .6039f, .3216f));
                r.inkDeep = Lin(m, "_InkDeep", new Color(.4627f, .4157f, .2039f));
                r.inkCream = Lin(m, "_InkCream", new Color(.8902f, .8353f, .5804f));
                r.texPrint = (uint)Slot(m, "_PrintTex", Native.TexBlack);
            }
            if (m.IsKeywordEnabled("_EMISSION"))
            {
                r.flags |= Native.MatEmission;
                r.emission = Lin(m, "_EmissionColor", Color.black);
                if (m.IsKeywordEnabled("_FR_LAMP_FIELD"))
                {
                    // exit-sign diffusers (FrontRoomsSurface.shader _FR_LAMP_FIELD): emission = dot(field.rgb, weights) *
                    // field.a * _EmissionColor; the material's weights here, a live renderer's from its property block
                    r.flags |= Native.MatLampField;
                    r.texLampField = (uint)Slot(m, "_LampFieldMap", Native.TexBlack);
                    var w = m.HasProperty(LampFieldId) ? m.GetVector(LampFieldId) : new Vector4(1f, 1f, 0f, 0f);
                    r.tile.z = w.x; r.tile.w = w.y; r.emission.w = w.z;
                }
                else r.texEmission = (uint)Slot(m, "_EmissionMap", Native.TexWhite);
            }
            return r;
        }
        if (IsGlassMaterial(m))
        {
            r.kind = Native.KindGlass;
            r.baseColor = Lin(m, "_BaseColor", new Color(.15f, .17f, .16f));
            r.p0 = new Vector4(Fl(m, "_Smoothness", .96f), 0f, 1f, 1f);
            return r;
        }
        if (m.HasProperty("_BaseMap") || m.HasProperty("_MainTex") || shader.StartsWith("Universal Render Pipeline/"))
        {
            // URP Lit / Simple Lit / Unlit-like: metallic workflow (LitInput.hlsl InitializeStandardLitSurfaceData)
            r.kind = Native.KindLit;
            var baseProp = m.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
            r.baseColor = m.HasProperty("_BaseColor") ? Lin(m, "_BaseColor", Color.white) : Lin(m, "_Color", Color.white);
            r.baseST = St(baseProp);
            r.texBase = (uint)Slot(m, baseProp, Native.TexWhite);
            var smooth = m.HasProperty("_Smoothness") ? m.GetFloat("_Smoothness") : Fl(m, "_Glossiness", .5f);
            r.p0 = new Vector4(smooth, Fl(m, "_Metallic", 0f), Fl(m, "_OcclusionStrength", 1f), Fl(m, "_BumpScale", 1f));
            if (m.IsKeywordEnabled("_METALLICSPECGLOSSMAP")) { r.flags |= Native.MatMetallicMap; r.texMask = (uint)Slot(m, "_MetallicGlossMap", Native.TexWhite); }
            if (m.IsKeywordEnabled("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A")) r.flags |= Native.MatSmoothAlbedo;
            if (m.IsKeywordEnabled("_NORMALMAP")) { r.flags |= Native.MatNormalMap; r.texBump = (uint)Slot(m, "_BumpMap", Native.TexBump); }
            if (m.IsKeywordEnabled("_OCCLUSIONMAP")) { r.flags |= Native.MatOcclusionMap; r.texOcclusion = (uint)Slot(m, "_OcclusionMap", Native.TexWhite); }
            if (m.IsKeywordEnabled("_EMISSION") || (m.HasProperty("_EmissionColor") && m.GetColor("_EmissionColor").maxColorComponent > 0f))
            {
                r.flags |= Native.MatEmission;
                r.emission = Lin(m, "_EmissionColor", Color.black);
                r.texEmission = (uint)Slot(m, "_EmissionMap", Native.TexWhite);
            }
            // unlit shaders, and the CRT ad surface (FrontRooms/ScreenVideo: the video texture x tint, unlit): emission only.
            // Before 2026-10-14 a CRT screen was traced as a diffuse lit quad (dim in the reflection).
            if (shader.Contains("Unlit") || shader == "FrontRooms/ScreenVideo") { r.flags |= Native.MatEmission; r.emission = r.baseColor; r.texEmission = r.texBase; r.baseColor = Vector4.zero; }
            return r;
        }
        r.baseColor = m.HasProperty("_BaseColor") ? Lin(m, "_BaseColor", Color.grey) : Lin(m, "_Color", Color.grey);
        return r;
    }

    // ------------------------------------------------------------------ registry: instances
    sealed class Inst
    {
        public int handle, rendererId;
        public Renderer r;
        public Transform t;
        public GameObject go;
        public MeshEntry mesh;
        public int meshKeyOverride = -1;   // fracture pieces: a PrepareMeshes key
        public MatBlock block;             // its material records (reference-counted, E3)
        public uint materialBase, flags, mask;
        // Live props (E2): renderers whose visibility, material or emission changes without moving (a CRT switched off,
        // an EXIT sign failing, the glow placard). Checked every frame like the dynamic class; LiveVisibility = on/off
        // and material swaps only, LiveEmission also the emission of material slot liveMatIndex, LiveLampField also its
        // exit-sign lamp weights (_LampField, per material slot property block).
        public byte live;
        public int liveMatIndex = -1, liveMatHash;
        public int windowId = -1;
        public Vector4 r0, r1, r2;
        public Bounds bounds;
        public int materialId;              // receivers / glass: watched for a material swap
        public bool enabled = true, alive = true, manual, dynamic, priority, lens, receiver, shown = true;
        public int layer;
        public ChunkRec chunk;
        public Vector4 emission;           // xyz emission (lens, linear), w ceiling height
        public int prepassBit = -1;
        public float d2;
        public Light lensLight;            // the fixture's Light (sibling of the "Lens" under "Fixture (x, y)")
        public bool lensLit;
        public int blockIndex = -1;        // statics: index of this instance's record in staticBlock, -1 = not in it
    }

    sealed class ChunkRec
    {
        public Transform root;
        public int id, childCount = -1, stamp;
        public bool dirty = true, queued;
        public float lateRescanAt;
        public readonly Dictionary<int, Inst> byRenderer = new Dictionary<int, Inst>();
        public readonly List<LampRec> lights = new List<LampRec>();
        // a scan in progress: the chunk's renderers gathered once, registered from `cursor` over several frames
        public readonly List<MeshRenderer> pending = new List<MeshRenderer>();
        public int cursor;
        public bool scanning;
        // the gather itself is sliced too (fix round 31): a depth-first walk of the chunk's hierarchy from `walk`,
        // collecting renderers into `pending` and lights into `walkLights`, within the frame's budget
        public readonly List<Transform> walk = new List<Transform>();
        public readonly List<Light> walkLights = new List<Light>();
        public bool gathering;
        public Vector3 lightCenter;
        public float lightRadius;
        // fix round 32: first-scan bookkeeping (frames from the chunk root's appearance to its full registration)
        public int bornFrame, completeFrame = -1;
        public float bornDistance;
        public bool complete;
        public long coordKey;   // fix round 32: the map chunk coordinate (root local position / 24 m)
    }

    /// <summary>Harness (fix round 32, review 2 engineering #6): one row per chunk whose first scan finished: frames from
    /// the root's appearance to full registration (renderers + lights), the camera distance at birth, renderers.</summary>
    internal struct ChunkTiming
    {
        public string name;
        public int born, done, renderers, lights;
        public float bornDistance, doneDistance;
        public bool burst;
    }
    internal static readonly List<ChunkTiming> ChunkTimings = new List<ChunkTiming>(512);

    // Static data of a chunk's light, cached at (re)scan: map lights never move relative to their chunk and only
    // their enabled state, intensity and shadows change per frame (MapWorld.TickFixtures).
    sealed class LampRec
    {
        public Light light;
        public Vector3 pos, fwd;
        public LightType type;
        public float invRange2, invAngleRange, angleAdd, shadowStrength;
        public Color colorLinear;
        public Inst lens;                  // this fixture's troffer lens (its emission follows the lamp: read with it)
        // The map's lamp cell (parent "Fixture (x, y)"): the lens glow is re-read from its property block only when the
        // map's logical level for that cell moved (FrontRoomsMapWorld.LampLevel), the light switched, or the start-lamp
        // holds changed (fix round 31: lamps_ms was 0.23 ms p50, one GetPropertyBlock per lens per frame).
        public FrontRooms.Map.GridCoord cell;
        public bool hasCell, lastLit;
        public float lastLevel = -2f;
    }

    /// <summary>"Fixture (x, y)" -> cell (no allocation). False for anything else.</summary>
    static bool ParseFixtureCell(string name, out FrontRooms.Map.GridCoord cell)
    {
        cell = default;
        if (name == null || !name.StartsWith("Fixture (", StringComparison.Ordinal)) return false;
        var k = 9;
        if (!ParseInt(name, ref k, out var x) || k + 1 >= name.Length || name[k] != ',' || name[k + 1] != ' ') return false;
        k += 2;
        if (!ParseInt(name, ref k, out var y) || k >= name.Length || name[k] != ')') return false;
        cell = new FrontRooms.Map.GridCoord(x, y);
        return true;
    }

    static bool ParseInt(string s, ref int k, out int v)
    {
        v = 0;
        var neg = k < s.Length && s[k] == '-';
        if (neg) k++;
        var start = k;
        while (k < s.Length && s[k] >= '0' && s[k] <= '9') { v = v * 10 + (s[k] - '0'); k++; }
        if (neg) v = -v;
        return k > start;
    }

    static LampRec MakeLamp(Light l)
    {
        var rec = new LampRec { light = l, pos = l.transform.position, fwd = l.transform.forward, type = l.type, shadowStrength = l.shadowStrength };
        var fixture = l.transform.parent;
        rec.hasCell = fixture != null && ParseFixtureCell(fixture.name, out rec.cell);
        var range = Mathf.Max(l.range, 1e-3f);
        rec.invRange2 = 1f / (range * range);
        var c = l.color.linear;
        if (GraphicsSettings.lightsUseColorTemperature && l.useColorTemperature) c *= Mathf.CorrelatedColorTemperatureToRGB(l.colorTemperature);
        rec.colorLinear = c;
        if (l.type == LightType.Spot)
        {
            var cosOuter = Mathf.Cos(Mathf.Deg2Rad * l.spotAngle * .5f);
            var cosInner = Mathf.Cos(Mathf.Deg2Rad * l.innerSpotAngle * .5f);
            rec.invAngleRange = 1f / Mathf.Max(.001f, cosInner - cosOuter);
            rec.angleAdd = -cosOuter * rec.invAngleRange;
        }
        return rec;
    }

    static float SqrDist(Bounds b, Vector3 p)
    {
        var c = b.center; var e = b.extents;
        var dx = Mathf.Max(Mathf.Abs(p.x - c.x) - e.x, 0f);
        var dy = Mathf.Max(Mathf.Abs(p.y - c.y) - e.y, 0f);
        var dz = Mathf.Max(Mathf.Abs(p.z - c.z) - e.z, 0f);
        return dx * dx + dy * dy + dz * dz;
    }

    static readonly List<Inst> insts = new List<Inst>();
    static readonly Stack<int> freeHandles = new Stack<int>();
    static readonly List<Inst> watch = new List<Inst>();          // receivers, glass, dynamic, manual: checked every frame
    static readonly Dictionary<int, ChunkRec> chunks = new Dictionary<int, ChunkRec>();
    static readonly List<ChunkRec> rescanQueue = new List<ChunkRec>();   // fix round 32: a list, picked nearest first
    static int liveCount, chunkStamp, staticCursor;
    static FrontRoomsMapWorld map;
    static Transform mapRoot;
    static Vector3 mapRootPos;
    static float nextMapSearch, nextDirSearch;
    static Light dirLight;
    static Transform relayRoot;
    static bool warnedMarker;
    static int lastSceneFrame = -1;
    static readonly MaterialPropertyBlock block = new MaterialPropertyBlock();
    static readonly List<MeshRenderer> scratchRenderers = new List<MeshRenderer>();
    static readonly List<Light> scratchLights = new List<Light>();
    static readonly HashSet<int> scratchSeen = new HashSet<int>();
    static readonly List<Inst> scratchInsts = new List<Inst>();
    static readonly Dictionary<int, LampRec> scratchLampByLight = new Dictionary<int, LampRec>();
    static readonly List<Inst> lensList = new List<Inst>();        // every registered lens (round-robin reads)
    static readonly List<Inst> lightlessLenses = new List<Inst>();  // lenses with no Light in their fixture
    static int lensCursor;

    /// <summary>Bumped by every change that alters the static instance set or a static record (register, remove,
    /// enable, rebase): the cached static block is rebuilt on the next traced frame.</summary>
    static int registryVersion;
    /// <summary>Bumped only by changes that move every static record (rebase, reset): forces a full static rebuild.
    /// Registrations, removals and enable flips patch the cached static block in place (StaticAdd / StaticRemove).</summary>
    static int staticLayoutVersion;
    /// <summary>Bumped when a lens within NearLens of the camera changes emission (the still-frame signature).</summary>
    static int nearLensChanges;

    static void Warn(string s) => Debug.LogWarning("[FrontRoomsGlassRT] " + s);

    static Inst NewInst()
    {
        var h = freeHandles.Count > 0 ? freeHandles.Pop() : insts.Count;
        var i = new Inst { handle = h };
        if (h == insts.Count) insts.Add(i); else insts[h] = i;
        liveCount++;
        registryVersion++;
        return i;
    }

    static void RowsFrom(Inst i, Matrix4x4 m)
    {
        i.r0 = m.GetRow(0); i.r1 = m.GetRow(1); i.r2 = m.GetRow(2);
    }

    static Inst RegisterRenderer(Renderer r, FrontRoomsGlassRTFlags flags, int windowId, ChunkRec chunk, bool manual, int meshKeyOverride = -1)
    {
        if (r == null) return null;
        if (!(r is MeshRenderer)) return null;   // particles, trails, lines and skinned meshes are not traced (P0)
        if (r.isPartOfStaticBatch) return null;  // R32
        if (r.shadowCastingMode == ShadowCastingMode.ShadowsOnly) return null;
        var mats = r.sharedMaterials;
        var first = mats != null && mats.Length > 0 ? mats[0] : null;
        var glass = IsGlassMaterial(first) || (flags & FrontRoomsGlassRTFlags.Glass) != 0;
        if (!glass && first != null && first.renderQueue >= 2450) return null;   // other transparents (beams, decals): not traced
        var lod = r.GetComponentInParent<LODGroup>();
        if (lod != null && !InLod0(lod, r)) return null;

        MeshEntry mesh = null;
        if (meshKeyOverride < 0)
        {
            var mf = r.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return null;
            var tm = Stopwatch.GetTimestamp();
            mesh = AcquireMesh(mf.sharedMesh, (flags & FrontRoomsGlassRTFlags.Deforming) != 0);
            RegProfile[0] += Stopwatch.GetTimestamp() - tm;
            if (mesh == null) return null;
        }
        var inst = NewInst();
        inst.r = r; inst.rendererId = ObjectId(r); inst.t = r.transform; inst.go = r.gameObject; inst.mesh = mesh; inst.meshKeyOverride = meshKeyOverride;
        inst.windowId = windowId; inst.chunk = chunk; inst.manual = manual; inst.layer = inst.go.layer;
        var geometry = mesh != null ? mesh.geometrySubmesh : new[] { 0 };
        var tmat = Stopwatch.GetTimestamp();
        inst.block = AcquireBlock(mats, geometry);
        inst.materialBase = (uint)inst.block.first;
        RegProfile[1] += Stopwatch.GetTimestamp() - tmat;
        inst.materialId = first != null ? ObjectId(first) : 0;
        if (!glass) inst.live = LiveKind(r, mats, out inst.liveMatIndex);
        // API registrations (the harness's panels, future content): watched every frame anyway; their material swaps and
        // emission (material or property block) follow too
        if (!glass && inst.live == 0 && manual) inst.live = ManualLiveKind(mats, out inst.liveMatIndex);
        if (inst.live != 0) { inst.liveMatHash = MaterialsHash(mats); LiveInstances++; }

        if (glass) flags |= FrontRoomsGlassRTFlags.Glass;
        if (IsReceiverMaterial(first)) flags |= FrontRoomsGlassRTFlags.Receiver;
        else if (r.GetComponent<FrontRoomsMetalGlassTarget>() != null && !warnedMarker && glass)
        {
            warnedMarker = true;
            Debug.Log("[FrontRoomsGlassRT] a FrontRoomsMetalGlassTarget pane uses " + (first != null ? first.name + " (" + first.shader.name + ")" : "no material")
                      + ", which has no RT input: it is traced as glass but receives no RT reflection until it uses Glass_Window (_RTReceive = 1).");
        }
        inst.flags = (uint)flags;
        inst.receiver = (flags & FrontRoomsGlassRTFlags.Receiver) != 0;
        inst.lens = (flags & FrontRoomsGlassRTFlags.EmissiveLens) != 0;
        inst.dynamic = (flags & (FrontRoomsGlassRTFlags.Dynamic | FrontRoomsGlassRTFlags.Relay | FrontRoomsGlassRTFlags.Door)) != 0 || manual;
        inst.priority = inst.receiver || glass || inst.dynamic || manual || inst.live != 0;
        inst.mask = (flags & FrontRoomsGlassRTFlags.Shard) != 0 ? Native.MaskShard
                  : glass ? Native.MaskGlass
                  : (flags & FrontRoomsGlassRTFlags.ReflectionOnly) != 0 ? Native.MaskReflectionOnly
                  : inst.dynamic ? Native.MaskDynamic : Native.MaskOpaque;
        // shadow rays see exactly the raster's shadow casters (lenses, glass and shadow-less props let light through)
        if (!glass && (flags & FrontRoomsGlassRTFlags.ReflectionOnly) == 0 && r.shadowCastingMode != ShadowCastingMode.Off) inst.mask |= Native.MaskCaster;
        RowsFrom(inst, r.localToWorldMatrix);
        inst.bounds = r.bounds;
        inst.shown = r.enabled && inst.go.activeInHierarchy && !r.forceRenderingOff;
        if (first != null && first.HasProperty(CeilingHeightId) && r.HasPropertyBlock())
        {
            r.GetPropertyBlock(block);
            inst.emission.w = block.GetFloat(CeilingHeightId);
        }
        if (inst.lens)
        {
            var fixture = r.transform.parent;
            inst.lensLight = fixture != null ? fixture.GetComponentInChildren<Light>(true) : null;
            inst.lensLit = inst.lensLight != null && inst.lensLight.enabled;
            ReadLensEmission(inst);
            lensList.Add(inst);
            if (inst.lensLight == null) lightlessLenses.Add(inst);
        }
        if (inst.receiver) SetPrepassBit(inst, PrepassBitFor(first));
        if (inst.live != 0) UpdateLive(inst, false);
        if (inst.priority) watch.Add(inst);
        else StaticAdd(inst);
        return inst;
    }

    const byte LiveVisibility = 1, LiveEmission = 2, LiveLampField = 3;
    /// <summary>Harness: live props registered this session (E2).</summary>
    internal static int LiveInstances;

    /// <summary>E2 (2026-10-14): which renderers are live props. Their owners change them without moving them:
    /// FrontRoomsScreenVideo.TogglePower disables the CRT surface, FrontRoomsExitSign writes _LampField per diffuser slot,
    /// FrontRoomsPlacardGlow swaps in a material instance and animates its _EmissionColor. No owner file is edited.</summary>
    static byte LiveKind(Renderer r, Material[] mats, out int index)
    {
        index = -1;
        if (r.GetComponentInParent<FrontRoomsExitSign>(true) != null)
        {
            for (var k = 0; k < mats.Length; k++) if (mats[k] != null && mats[k].IsKeywordEnabled("_FR_LAMP_FIELD") && mats[k].IsKeywordEnabled("_EMISSION")) { index = k; return LiveLampField; }
            for (var k = 0; k < mats.Length; k++) if (mats[k] != null && mats[k].IsKeywordEnabled("_EMISSION")) { index = k; return LiveEmission; }
            return LiveVisibility;
        }
        if (r.GetComponentInParent<FrontRoomsPlacardGlow>(true) != null)
        {
            for (var k = 0; k < mats.Length; k++) if (mats[k] != null && mats[k].HasProperty(EmissionColorId) && mats[k].IsKeywordEnabled("_EMISSION")) { index = k; return LiveEmission; }
            return LiveVisibility;
        }
        if (r.GetComponentInParent<FrontRoomsScreenVideo>(true) != null) return LiveVisibility;
        return 0;
    }

    static byte ManualLiveKind(Material[] mats, out int index)
    {
        index = -1;
        if (mats != null)
            for (var k = 0; k < mats.Length; k++) if (mats[k] != null && mats[k].IsKeywordEnabled("_EMISSION")) { index = k; return LiveEmission; }
        return LiveVisibility;
    }

    static int MaterialsHash(Material[] mats)
    {
        var h = 17;
        if (mats != null) foreach (var m in mats) h = h * 31 + (m != null ? ObjectId(m) : 0);
        return h;
    }

    static readonly List<Material> liveMats = new List<Material>(8);

    /// <summary>A live prop this frame (it is already known to be on): a material swap re-describes it; its emission or
    /// lamp weights go into the instance record (EmissiveLens / LampWeights). Near changes stop temporal accumulation.</summary>
    static void UpdateLive(Inst i, bool near)
    {
        i.r.GetSharedMaterials(liveMats);
        var h = 17;
        foreach (var m in liveMats) h = h * 31 + (m != null ? ObjectId(m) : 0);
        if (h != i.liveMatHash)
        {
            i.liveMatHash = h;
            var mats = i.r.sharedMaterials;
            var old = i.block;
            i.block = AcquireBlock(mats, i.mesh != null ? i.mesh.geometrySubmesh : new[] { 0 });
            i.materialBase = (uint)i.block.first;
            ReleaseBlock(old);
            i.live = LiveKind(i.r, mats, out i.liveMatIndex);
            if (i.live == 0) i.live = i.manual ? ManualLiveKind(mats, out i.liveMatIndex) : LiveVisibility;
            // the new materials decide again whether the record carries an emission or lamp-weight override
            if (!i.lens) i.flags &= ~((uint)FrontRoomsGlassRTFlags.EmissiveLens | Native.FlagLampWeights);
            i.r.GetSharedMaterials(liveMats);
            if (near) nearLensChanges++;
        }
        var e = i.emission;
        if (i.live == LiveLampField || i.live == LiveEmission)
        {
            var mat = i.liveMatIndex >= 0 && i.liveMatIndex < liveMats.Count ? liveMats[i.liveMatIndex] : null;
            i.r.GetPropertyBlock(block, i.liveMatIndex);   // the slot's own block (empty if none)
            if (i.live == LiveLampField)
            {
                var w = !block.isEmpty && block.HasVector(LampFieldId) ? block.GetVector(LampFieldId)
                      : mat != null && mat.HasProperty(LampFieldId) ? mat.GetVector(LampFieldId) : new Vector4(1f, 1f, 0f, 0f);
                e = new Vector4(w.x, w.y, w.z, i.emission.w);
                i.flags |= Native.FlagLampWeights;
            }
            else
            {
                Color c;
                if (!block.isEmpty && block.HasColor(EmissionColorId)) c = block.GetColor(EmissionColorId);
                else
                {
                    i.r.GetPropertyBlock(block);   // a renderer-wide block
                    c = !block.isEmpty && block.HasColor(EmissionColorId) ? block.GetColor(EmissionColorId)
                      : mat != null && mat.HasProperty(EmissionColorId) ? mat.GetColor(EmissionColorId) : Color.black;
                }
                var l = c.linear;
                e = new Vector4(l.r, l.g, l.b, i.emission.w);
                i.flags |= (uint)FrontRoomsGlassRTFlags.EmissiveLens;
            }
        }
        if (e != i.emission) { i.emission = e; if (near) nearLensChanges++; }
    }

    static bool InLod0(LODGroup group, Renderer r)
    {
        var lods = group.GetLODs();
        if (lods.Length == 0) return true;
        foreach (var x in lods[0].renderers) if (x == r) return true;
        for (var k = 1; k < lods.Length; k++) foreach (var x in lods[k].renderers) if (x == r) return false;
        return true;
    }

    static void SetPrepassBit(Inst i, int bit)
    {
        ClearPrepassBit(i);
        if (i.r == null) return;
        i.r.renderingLayerMask |= 1u << bit;
        i.prepassBit = bit;
    }

    static void ClearPrepassBit(Inst i)
    {
        if (i.prepassBit < 0) return;
        if (i.r != null) i.r.renderingLayerMask &= ~(1u << i.prepassBit);
        i.prepassBit = -1;
    }

    static void ReadLensEmission(Inst i)
    {
        if (i.r == null) return;
        Color c;
        if (i.r.HasPropertyBlock())
        {
            i.r.GetPropertyBlock(block);
            c = block.GetColor(EmissionColorId);   // the value as passed: the GPU sees its sRGB->linear conversion
        }
        else
        {
            var m = i.r.sharedMaterial;
            c = m != null && m.HasProperty(EmissionColorId) ? m.GetColor(EmissionColorId) : Color.black;
        }
        var l = c.linear;
        i.emission.x = l.r; i.emission.y = l.g; i.emission.z = l.b;
    }

    /// <summary>Lens emission read for a frame: keeps the cached static record in step, and counts a change near the
    /// camera for the still-frame signature (temporal accumulation stops while a near lens flickers).</summary>
    static void ReadLensForFrame(Inst i, bool near)
    {
        var before = i.emission;
        ReadLensEmission(i);
        if (i.emission == before) return;
        if (i.blockIndex >= 0 && i.blockIndex < staticBlock.Length) staticBlock[i.blockIndex].emission = i.emission;
        if (near) nearLensChanges++;
    }

    static void RemoveInst(Inst i)
    {
        if (i == null || !i.alive) return;
        i.alive = false;
        if (i.chunk != null && i.chunk.byRenderer.TryGetValue(i.rendererId, out var same) && same == i) i.chunk.byRenderer.Remove(i.rendererId);
        ClearPrepassBit(i);
        if (i.blockIndex >= 0) StaticRemove(i);
        ReleaseMeshRef(i.mesh);
        i.mesh = null;
        ReleaseBlock(i.block);
        i.block = null;
        if (i.live != 0) LiveInstances--;
        if (i.priority) watch.Remove(i);
        // (dead lenses leave lensList lazily, in the round-robin read: no O(n) remove per lens on a chunk drop)
        insts[i.handle] = null;
        freeHandles.Push(i.handle);
        liveCount--;
        registryVersion++;
    }

    // ------------------------------------------------------------------ public API (manual registration)
    // Every entry point other code calls (the map's GlassBroken event, GD3's hooks, the destruction track's API) fails
    // closed like the per-frame build (review 1 engineering #8): an exception in here turns RT Off for the session with one
    // console entry and never reaches the caller (the map, GD3). On a Mac that can never trace they do nothing at all.
    /// <summary>Harness fault injection (the fail-closed tests): 1 = BuildFrame entry, 2 = mid-frame (after the scene update),
    /// 3 = the GlassBroken handler, 4 = Register, 5 = GD3 MeshesReady handler. 0 = off.</summary>
    internal static int HarnessFault;

    static bool ApiOff => failedClosed || !CanTrace;

    static void InjectFault(int at)
    {
        if (HarnessFault == at) throw new InvalidOperationException("harness fault injection at " + at + " (FrontRoomsGlassRTVerify fail-closed test)");
    }

    public static int Register(Renderer renderer, FrontRoomsGlassRTFlags flags, int windowId)
    {
        if (failedClosed) return -1;
        try
        {
            EnsureStarted();
            if (ApiOff) return -1;
            InjectFault(4);
            var i = RegisterRenderer(renderer, flags, windowId, null, true);
            return i != null ? i.handle : -1;
        }
        catch (Exception e) { FailClosed(e, "in Register"); return -1; }
    }

    public static void Unregister(int handle)
    {
        if (failedClosed || handle < 0 || handle >= insts.Count) return;
        try { RemoveInst(insts[handle]); }
        catch (Exception e) { FailClosed(e, "in Unregister"); }
    }

    public static void SetEnabled(int handle, bool enabled)
    {
        if (failedClosed || handle < 0 || handle >= insts.Count || insts[handle] == null) return;
        try
        {
            var i = insts[handle];
            if (i.enabled != enabled) registryVersion++;
            i.enabled = enabled;
            if (!i.priority) { if (enabled) StaticAdd(i); else if (i.blockIndex >= 0) StaticRemove(i); }
            if (i.receiver)
            {
                if (enabled && i.prepassBit < 0) SetPrepassBit(i, PrepassBitFor(i.r != null ? i.r.sharedMaterial : null));
                else if (!enabled) ClearPrepassBit(i);
            }
        }
        catch (Exception e) { FailClosed(e, "in SetEnabled"); }
    }

    sealed class Ticket { public int firstKey, count; public bool failed; }
    static readonly List<Ticket> tickets = new List<Ticket>();

    public static int PrepareMeshes(Mesh source, FrontRoomsGlassRTRange[] pieces, int notBeforeFrame)
    {
        if (failedClosed) return -1;
        try
        {
            EnsureStarted();
            // an Intel / M1 / M2 Mac loads the universal plugin (RD2) but never traces: no CPU copy of the pieces there
            if (ApiOff || source == null || pieces == null || pieces.Length == 0 || !source.isReadable) return -1;
            return PrepareMeshesCore(source, pieces, notBeforeFrame);
        }
        catch (Exception e) { FailClosed(e, "in PrepareMeshes"); return -1; }
    }

    static int PrepareMeshesCore(Mesh source, FrontRoomsGlassRTRange[] pieces, int notBeforeFrame)
    {
        var verts = source.vertices;
        var normals = source.normals;
        var uvs = source.uv;
        var colors = source.colors;
        var n = verts.Length;
        var attrs = new Native.VertexAttr[n];
        for (var v = 0; v < n; v++)
        {
            var a = new Native.VertexAttr { ny = 1f };
            if (normals.Length == n) { a.nx = normals[v].x; a.ny = normals[v].y; a.nz = normals[v].z; }
            if (uvs.Length == n) { a.u = uvs[v].x; a.v = uvs[v].y; }
            if (colors.Length == n) a.colorR = colors[v].r;
            attrs[v] = a;
        }
        // indexStart / indexCount address submesh 0's index list; baseVertex is added to each index of the range
        var idx = source.GetIndices(0, false);
        var outIdx = new int[idx.Length];
        var starts = new int[pieces.Length];
        var counts = new int[pieces.Length];
        for (var k = 0; k < pieces.Length; k++)
        {
            var p = pieces[k];
            starts[k] = p.indexStart;
            counts[k] = p.indexCount - p.indexCount % 3;
            for (var j = 0; j < counts[k] && p.indexStart + j < idx.Length; j++) outIdx[p.indexStart + j] = idx[p.indexStart + j] + p.baseVertex;
        }
        var t = new Ticket { firstKey = nextMeshKey, count = pieces.Length };
        nextMeshKey += pieces.Length;
        int r;
        using (var pins = new Native.Pins(verts, attrs, outIdx, starts, counts))
            r = Native.FRGlassRT_UploadMeshRanges(t.firstKey, pins.A, pins.B, n, pins.C, outIdx.Length, pins.D, pins.E, pieces.Length, notBeforeFrame);
        t.failed = r != 0;
        tickets.Add(t);
        return tickets.Count - 1;
    }

    public static bool IsPrepared(int ticket)
    {
        if (failedClosed || ticket < 0 || ticket >= tickets.Count || tickets[ticket].failed || !nativeOk) return false;
        try
        {
            var t = tickets[ticket];
            var keys = new int[t.count];
            var states = new int[t.count];
            for (var k = 0; k < t.count; k++) keys[k] = t.firstKey + k;
            Native.FRGlassRT_MeshStates(keys, keys.Length, states);
            foreach (var s in states) if (s < 2) return false;
            return true;
        }
        catch (Exception e) { FailClosed(e, "in IsPrepared"); return false; }
    }

    public static int RegisterPiece(int ticket, int piece, Renderer renderer, FrontRoomsGlassRTFlags flags, int windowId)
    {
        if (failedClosed || ticket < 0 || ticket >= tickets.Count || piece < 0 || piece >= tickets[ticket].count) return -1;
        try
        {
            var i = RegisterRenderer(renderer, flags | FrontRoomsGlassRTFlags.Dynamic, windowId, null, true, tickets[ticket].firstKey + piece);
            return i != null ? i.handle : -1;
        }
        catch (Exception e) { FailClosed(e, "in RegisterPiece"); return -1; }
    }

    public static void MeshChanged(Mesh mesh)
    {
        if (failedClosed || mesh == null || !meshById.TryGetValue(ObjectId(mesh), out var e) || e.failed) return;
        try { Upload(e); }   // same key: the plugin replaces the old BLAS (generation-safe) and builds the new one this frame
        catch (Exception ex) { FailClosed(ex, "in MeshChanged"); }
    }

    public static void RefitMesh(Mesh mesh)
    {
        if (failedClosed || mesh == null || !meshById.TryGetValue(ObjectId(mesh), out var e) || !e.deforming || !mesh.isReadable) return;
        try
        {
            var v = mesh.vertices;
            using (var pins = new Native.Pins(v)) Native.FRGlassRT_RefitMesh(e.key, pins.A, v.Length);
        }
        catch (Exception ex) { FailClosed(ex, "in RefitMesh"); }
    }

    public static void GlassBroken(Vector3 at)
    {
        if (failedClosed) return;
        try
        {
            InjectFault(3);
            for (var k = watch.Count - 1; k >= 0; k--)
            {
                if (k >= watch.Count) continue;
                var i = watch[k];
                if (i.receiver && i.alive && (i.bounds.SqrDistance(at) < 1f || (i.bounds.center - at).sqrMagnitude < 1f)) RemoveInst(i);
            }
        }
        catch (Exception e) { FailClosed(e, "in the map's GlassBroken handler"); }
    }

    // ------------------------------------------------------------------ GD3 stage meshes (G14V-1)
    // GD3 (FrontRoomsGlassBreakable) builds the S1 fins + crater and the S2 pieces in slices during the hold and parents
    // them to the window root, two levels below the chunk root: the chunk's child count does not change, so the chunk
    // scan never saw them (fix round 31: S1/S2 not in the TLAS at their beats; a restored S2 was, through WindowBuilt
    // before the scan). Now MeshesReady (before the beats, design §4.7 P4: BLASes are built then, never on a beat frame)
    // queues the window's stage renderers for registration in the window's chunk; a queued renderer is registered at once
    // if its stage shows first (StageChanged). A renderer already registered whose Mesh GD3 refilled (a working set reused
    // for a new break) is re-uploaded. Live / hidden state is read every frame like every receiver (watch list), so the
    // swap on the beat frame is right on that frame. GD3's files are not edited.
    static readonly List<MeshRenderer> stageQueue = new List<MeshRenderer>();
    internal static int StageRegistrations, StageReuploads;

    static void OnGlassMeshesReady(FrontRoomsGlassBreakable glass)
    {
        if (ApiOff || glass == null) return;
        try
        {
            InjectFault(5);
            QueueStage(glass.SlabRenderer, false);
            QueueStage(glass.Stage1Renderer, true);
            QueueStage(glass.Stage2Renderer, true);
        }
        catch (Exception e) { FailClosed(e, "in GD3's MeshesReady handler"); }
    }

    static void OnGlassStageChanged(FrontRoomsGlassBreakable glass, int from, int to)
    {
        if (ApiOff || glass == null) return;
        try
        {
            // the beat came before the queue drained (a beat that forces the meshes): register this window's now
            for (var k = stageQueue.Count - 1; k >= 0; k--)
            {
                var r = stageQueue[k];
                if (r == null) { stageQueue.RemoveAt(k); continue; }
                if (r.transform.IsChildOf(glass.transform)) { stageQueue.RemoveAt(k); RegisterStage(r); }
            }
        }
        catch (Exception e) { FailClosed(e, "in GD3's StageChanged handler"); }
    }

    /// <summary>refilled: GD3 has just (re)built this renderer's Mesh. Its working sets (and their Mesh objects) are reused
    /// from window to window, so a Mesh G14 already holds may carry new triangles: it is uploaded again (same key, the plugin
    /// swaps the BLAS generation-safely), for every instance that uses it.</summary>
    static void QueueStage(MeshRenderer r, bool refilled)
    {
        if (r == null) return;
        var mf = r.GetComponent<MeshFilter>();
        var mesh = mf != null ? mf.sharedMesh : null;
        if (refilled && mesh != null && meshById.TryGetValue(ObjectId(mesh), out var held) && !held.failed) { StageReuploads++; Upload(held); }
        var known = FindByRenderer(r);
        if (known != null)
        {
            if (mesh != null && known.mesh != null && known.mesh.mesh == mesh) return;   // registered, same Mesh: done
            RemoveInst(known);                                                          // the renderer now draws another Mesh
        }
        if (!stageQueue.Contains(r)) stageQueue.Add(r);
    }

    static Inst FindByRenderer(Renderer r)
    {
        var c = ChunkOf(r.transform, false);
        var id = ObjectId(r);
        if (c != null && c.byRenderer.TryGetValue(id, out var inst) && inst.alive) return inst;
        foreach (var w in watch) if (w.alive && w.rendererId == id) return w;
        return null;
    }

    /// <summary>The chunk record of the map chunk that holds this transform (created if the chunk scan has not seen the
    /// chunk yet, so the later scan finds the renderer already registered).</summary>
    static ChunkRec ChunkOf(Transform t, bool create)
    {
        if (map == null || mapRoot == null || t == null) return null;
        var p = t;
        while (p != null && p.parent != mapRoot) p = p.parent;
        if (p == null) return null;
        var id = ObjectId(p);
        if (chunks.TryGetValue(id, out var c)) return c;
        if (!create) return null;
        c = new ChunkRec { root = p, id = id, lateRescanAt = Time.realtimeSinceStartup + 1f };
        chunks[id] = c;
        return c;
    }

    static void RegisterStage(MeshRenderer r)
    {
        if (r == null || FindByRenderer(r) != null) return;
        var c = ChunkOf(r.transform, true);
        var inst = RegisterRenderer(r, c != null ? Classify(r, c.root) : FrontRoomsGlassRTFlags.Glass, -1, c, c == null);
        if (inst == null) return;
        if (c != null) c.byRenderer[inst.rendererId] = inst;
        StageRegistrations++;
    }

    /// <summary>Queued GD3 stage renderers, inside the frame's registration budget (at least one per frame).</summary>
    static void DrainStageQueue(Stopwatch budget)
    {
        var n = 0;
        while (stageQueue.Count > 0 && (n == 0 || budget.Elapsed.TotalMilliseconds < sliceBudgetMs))
        {
            var r = stageQueue[0];
            stageQueue.RemoveAt(0);
            RegisterStage(r);
            n++;
        }
    }

    // ------------------------------------------------------------------ scene update (once per frame)
    static readonly Stopwatch mainWatch = new Stopwatch();
    static float mainMs, mainMsMax;
    static int lastCandidates, lastLamps;

    /// <summary>Once per frame: find the map, diff its chunks, slice registrations, watch materials.</summary>
    internal static void UpdateScene(bool force = false)
    {
        if (!force && lastSceneFrame == Time.frameCount) return;
        lastSceneFrame = Time.frameCount;
        var now = Time.realtimeSinceStartup;
        if (map == null)
        {
            if (mapRoot != null || chunks.Count > 0) OnMapLost();
            if (now >= nextMapSearch)
            {
                nextMapSearch = now + 1f;
                map = Object.FindAnyObjectByType<FrontRoomsMapWorld>();
                if (map != null) OnMapFound();
            }
        }
        // G14V-3: the prepass bit depends on which SubShader of the receiver's material is active, i.e. on the render
        // pipeline. A receiver registered before URP's first render (a harness Flush in batch mode) took the override
        // prepass for the session, which draws almost nothing of GD3's slab. Re-pick every receiver's bit when the active
        // pipeline object changes (null -> URP at the first render), and re-check override receivers now and then (the
        // editor compiles shaders asynchronously: FindPass fails until the variant exists).
        var rp = RenderPipelineManager.currentPipeline;
        if (!ReferenceEquals(rp, lastPipeline)) { lastPipeline = rp; RepickPrepassBits(true); }
        else if ((Time.frameCount & 31) == 0) RepickPrepassBits(false);
        if (map != null)
        {
            if (mapRoot.position != mapRootPos) Rebase(mapRoot.position - mapRootPos);
            DiffChunks(now);
            // fix round 32: a burst budget while a chunk near the camera is unregistered; nearest chunk first always
            nearBacklog = CountNearBacklog();
            burstNow = nearBacklog > 0;
            sliceBudgetMs = burstNow ? Mathf.Max(RegisterBudgetMs, BurstBudgetMs) : RegisterBudgetMs;
            if (burstNow) BurstFrames++;
            var budget = Stopwatch.StartNew();
            DrainStageQueue(budget);
            while (rescanQueue.Count > 0 && budget.Elapsed.TotalMilliseconds < sliceBudgetMs)
            {
                var c = TakeNearest();
                c.queued = false;
                if (c.root == null) continue;
                Rescan(c, budget);
            }
            nearBacklog = CountNearBacklog();
        }
        if (dirLight == null && now >= nextDirSearch)
        {
            nextDirSearch = now + 5f;
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional && l.isActiveAndEnabled) { dirLight = l; break; }
        }
        RefreshStatics();
    }

    static RenderPipeline lastPipeline;
    internal static int PrepassRepicks;

    /// <summary>all: every receiver (the pipeline changed); else only receivers on the override prepass (bit 29).</summary>
    static void RepickPrepassBits(bool all)
    {
        foreach (var i in watch)
        {
            if (!i.receiver || !i.alive || i.r == null || i.prepassBit < 0) continue;
            if (!all && i.prepassBit != OverrideBit) continue;
            var bit = PrepassBitFor(i.r.sharedMaterial);
            if (bit == i.prepassBit) continue;
            SetPrepassBit(i, bit);
            PrepassRepicks++;
        }
    }

    static void OnMapFound()
    {
        mapRoot = map.transform;
        mapRootPos = mapRoot.position;
        map.GlassBroken += GlassBroken;
        map.LiveApplied += OnMapLiveApplied;
        lensForceRead = true;
        // the Relay: 16 rigid MeshRenderers (02 §7), registered as Dynamic | Relay; liveness is checked every frame
        var rig = Object.FindAnyObjectByType<FrontRoomsRelayRig>(FindObjectsInactive.Include);
        if (rig != null && relayRoot == null)
        {
            relayRoot = rig.transform;
            foreach (var r in rig.GetComponentsInChildren<MeshRenderer>(true))
                RegisterRenderer(r, FrontRoomsGlassRTFlags.Dynamic | FrontRoomsGlassRTFlags.Relay, -1, null, false);
        }
        Debug.Log("[FrontRoomsGlassRT] map found: " + map.name + " · Relay parts " + (relayRoot != null ? relayRoot.GetComponentsInChildren<MeshRenderer>(true).Length : 0));
    }

    static void OnMapLost()
    {
        foreach (var c in chunks.Values) DropChunk(c);
        chunks.Clear();
        rescanQueue.Clear();
        chunkCoords.Clear();      // fix round 33: nothing of the lost map stays counted as built
        notChunkRoots.Clear();
        mapRoot = null;
        map = null;
    }

    static void Rebase(Vector3 delta)
    {
        registryVersion++;
        staticLayoutVersion++;
        mapRootPos = mapRoot.position;
        foreach (var i in insts)
        {
            if (i == null || i.dynamic || i.chunk == null) continue;
            i.r0.w += delta.x; i.r1.w += delta.y; i.r2.w += delta.z;
            i.bounds.center += delta;
        }
        foreach (var c in chunks.Values)
        {
            foreach (var l in c.lights) l.pos += delta;
            c.lightCenter += delta;
        }
    }

    // Fix round 33 (review 3 R3-B2): only the map's own chunk roots are chunks. MapWorld.BuildInto creates each one as
    // "Chunk (x, y) · revision n" directly under the map root at local (x, 0, y) * MapGrid.ChunkSize. Other children of the
    // map root are not chunks: a presented key (MapWorld.TakeKey re-parents it to the root while the door shot presents
    // it, then destroys it) and the edit-mode capture eye ("CAPTURE / eye", BuildForCapture). Before, every child counted:
    // a key pickup within 30 m was a "near birth" that dropped the reflection to the zone cube for ~20 frames, and the
    // key's destruction removed its rounded coordinate, usually a real neighbouring chunk's, from the built set for good.
    // Each child is classified once (Transform.name allocates); the ids of non-chunk children are remembered.
    static readonly HashSet<int> notChunkRoots = new HashSet<int>();
    /// <summary>Harness: children of the map root that are not chunk roots (seen this session).</summary>
    internal static int NonChunkRoots;

    static bool IsChunkRoot(Transform t)
    {
        var s = MapGrid.ChunkSize;
        var p = t.localPosition;
        if (Mathf.Abs(p.y) > 1e-3f || Mathf.Abs(p.x - Mathf.Round(p.x / s) * s) > 1e-3f || Mathf.Abs(p.z - Mathf.Round(p.z / s) * s) > 1e-3f) return false;
        return t.name.StartsWith("Chunk ", StringComparison.Ordinal);
    }

    static void DiffChunks(float now)
    {
        chunkStamp++;
        var n = mapRoot.childCount;
        var live = 0;
        for (var k = 0; k < n; k++)
        {
            var root = mapRoot.GetChild(k);
            var id = ObjectId(root);
            if (!chunks.TryGetValue(id, out var c))
            {
                if (notChunkRoots.Contains(id)) continue;
                if (!IsChunkRoot(root))
                {
                    if (notChunkRoots.Count >= 256) notChunkRoots.Clear();   // destroyed ones are only re-classified
                    notChunkRoots.Add(id);
                    NonChunkRoots++;
                    continue;
                }
                c = new ChunkRec { root = root, id = id, lateRescanAt = now + 1f, bornFrame = Time.frameCount, bornDistance = ChunkDistance(root) };
                c.coordKey = CoordKey(Mathf.RoundToInt(root.localPosition.x / MapGrid.ChunkSize), Mathf.RoundToInt(root.localPosition.z / MapGrid.ChunkSize));
                AddCoord(c.coordKey);
                if (c.bornDistance < StreamGateRadius) { lastNearBirth = Time.frameCount; NearBirths++; }
                chunks[id] = c;
            }
            live++;
            c.stamp = chunkStamp;
            var cc = root.childCount;
            if (cc != c.childCount) { c.childCount = cc; c.dirty = true; }
            if (c.lateRescanAt > 0f && now >= c.lateRescanAt) { c.lateRescanAt = 0f; c.dirty = true; }   // catch the room dressing
            if (c.dirty && !c.queued) { c.queued = true; c.dirty = false; rescanQueue.Add(c); }
        }
        // records whose root is gone (compared with the chunk roots seen, not with every child of the map root)
        if (chunks.Count > live)
        {
            List<int> gone = null;
            foreach (var kv in chunks) if (kv.Value.stamp != chunkStamp) (gone ??= new List<int>()).Add(kv.Key);
            if (gone != null) foreach (var id in gone) { DropChunk(chunks[id]); chunks.Remove(id); }
        }
    }

    // Fix round 33 (R3-B2): the built chunk squares are reference-counted. RebuildChunk (the Level Designer's
    // ReplaceModule, the map's own rebuilds) adds the new root before the old one leaves; with a plain set the old root's
    // removal took the square out while its new root stood there, and CountNearUnbuilt counted it unbuilt for good.
    static void AddCoord(long key) { chunkCoords[key] = chunkCoords.TryGetValue(key, out var n) ? n + 1 : 1; }
    static void RemoveCoord(long key)
    {
        if (!chunkCoords.TryGetValue(key, out var n)) return;
        if (n <= 1) chunkCoords.Remove(key); else chunkCoords[key] = n - 1;
    }
    /// <summary>Harness: chunk roots registered on this map square (0 = counted as not built).</summary>
    internal static int ChunkCoordRefs(int x, int z) => chunkCoords.TryGetValue(CoordKey(x, z), out var n) ? n : 0;

    static void DropChunk(ChunkRec c)
    {
        RemoveCoord(c.coordKey);
        // RemoveInst also unregisters the instance from this dictionary. Iterate
        // over a snapshot so chunk teardown cannot mutate the active enumerator
        // (main's 9e754a2 fix; the snapshot list is reused, no garbage per drop).
        scratchInsts.Clear();
        scratchInsts.AddRange(c.byRenderer.Values);
        foreach (var i in scratchInsts) RemoveInst(i);
        scratchInsts.Clear();
        c.byRenderer.Clear();
        c.lights.Clear();
        c.pending.Clear();
        c.walk.Clear();
        c.walkLights.Clear();
        c.gathering = false;
        c.scanning = false;
        c.root = null;
    }

    /// <summary>Registrations a scan slice always makes, budget or not, so a chunk can never stall half-registered.</summary>
    const int MinRegistrationsPerSlice = 4;
    /// <summary>Hierarchy nodes a gather slice always visits, budget or not.</summary>
    const int MinNodesPerSlice = 64;
    internal static int GatherSlices;

    // A chunk scan gathers the chunk's renderers ONCE (and drops the ones that are gone), then registers the new ones in
    // slices of ~RegisterBudgetMs per frame from a cursor, and reads the chunk's lights when the last slice is done.
    // (Run 8 re-gathered the whole chunk on every slice and let one slice run to 4x the budget; with a 1x budget that
    // re-gather alone could eat the slice: 2 ms of main thread per frame with no progress, 2026-10-07 run r2.)
    // Fix round 31: the gather is a sliced depth-first walk too (GetComponentsInChildren over a whole chunk had no budget;
    // review 1 engineering #1). Lights are collected by the same walk. Renderers added under an already visited node
    // during the walk are found by the next scan (child-count diff, the late rescan, or GD3's MeshesReady hook).
    static void Rescan(ChunkRec c, Stopwatch budget)
    {
        if (!c.scanning)
        {
            c.pending.Clear();
            c.walkLights.Clear();
            c.walk.Clear();
            c.walk.Add(c.root);
            c.gathering = true;
            c.cursor = 0;
            c.scanning = true;
        }
        if (c.gathering)
        {
            var nodes = 0;
            while (c.walk.Count > 0)
            {
                if (nodes >= MinNodesPerSlice && budget.Elapsed.TotalMilliseconds > sliceBudgetMs)
                {
                    GatherSlices++;
                    if (!c.queued) { c.queued = true; rescanQueue.Add(c); }
                    return;
                }
                var last = c.walk.Count - 1;
                var t = c.walk[last];
                c.walk.RemoveAt(last);
                if (t == null) continue;
                nodes++;
                if (t.TryGetComponent<MeshRenderer>(out var mr)) c.pending.Add(mr);
                if (t.TryGetComponent<Light>(out var lt)) c.walkLights.Add(lt);
                for (var k = t.childCount - 1; k >= 0; k--) c.walk.Add(t.GetChild(k));
            }
            c.gathering = false;
            scratchSeen.Clear();
            foreach (var r in c.pending) if (r != null) scratchSeen.Add(ObjectId(r));
            // removed renderers: destroyed, or not met by the walk and no longer under this chunk. A renderer created under
            // a node the sliced walk had already passed (GD3's stage meshes, registered on MeshesReady) is still the chunk's.
            List<int> gone = null;
            foreach (var kv in c.byRenderer)
            {
                var ri = kv.Value.r;
                if (ri == null || (!scratchSeen.Contains(kv.Key) && !ri.transform.IsChildOf(c.root))) (gone ??= new List<int>()).Add(kv.Key);
            }
            if (gone != null) foreach (var id in gone) { if (c.byRenderer.TryGetValue(id, out var dead)) RemoveInst(dead); c.byRenderer.Remove(id); }
        }
        // new renderers, from the cursor
        var registered = 0;
        while (c.cursor < c.pending.Count)
        {
            if (registered >= MinRegistrationsPerSlice && budget.Elapsed.TotalMilliseconds > sliceBudgetMs) break;
            var r = c.pending[c.cursor++];
            if (r == null) continue;
            var id = ObjectId(r);
            if (c.byRenderer.ContainsKey(id)) continue;
            // hidden renderers are registered too (shown = false, out of the TLAS): a CRT switched on later, or anything
            // other code shows, joins the reflection without a rescan (E2)
            registered++;
            var t0 = budget.Elapsed.TotalMilliseconds;
            var inst = RegisterRenderer(r, Classify(r, c.root), -1, c, false);
            if (inst != null) c.byRenderer[id] = inst;
            var dt = budget.Elapsed.TotalMilliseconds - t0;
            if (dt > SlowestRegistrationMs)
            {
                SlowestRegistrationMs = dt;
                var mf = r.GetComponent<MeshFilter>();
                var m = mf != null ? mf.sharedMesh : null;
                SlowestRegistration = r.name + " (" + (m != null ? m.name + ", " + m.vertexCount + " vertices, " + (m.isReadable ? "CPU copy" : "GPU gather") : "no mesh") + ")";
            }
        }
        if (c.cursor < c.pending.Count)
        {
            if (!c.queued) { c.queued = true; rescanQueue.Add(c); }
            return;
        }
        c.scanning = false;
        c.pending.Clear();
        // lights of this chunk (positions cached: chunk roots only translate), as the walk found them
        c.lights.Clear();
        scratchLampByLight.Clear();
        var lc = Vector3.zero;
        foreach (var l in c.walkLights)
            if (l != null && l.type != LightType.Directional) { var rec = MakeLamp(l); c.lights.Add(rec); scratchLampByLight[ObjectId(l)] = rec; lc += rec.pos; }
        c.walkLights.Clear();
        // each lens follows its fixture's lamp: BuildLamps reads it with the lamp (every frame while lit, once when it goes out)
        foreach (var inst in c.byRenderer.Values)
            if (inst.lens && inst.lensLight != null && scratchLampByLight.TryGetValue(ObjectId(inst.lensLight), out var lampRec)) lampRec.lens = inst;
        scratchLampByLight.Clear();
        c.lightCenter = c.lights.Count > 0 ? lc / c.lights.Count : c.root.position;
        c.lightRadius = 0f;
        foreach (var rec in c.lights) c.lightRadius = Mathf.Max(c.lightRadius, (rec.pos - c.lightCenter).magnitude);
        if (!c.complete)
        {
            c.complete = true;
            c.completeFrame = Time.frameCount;
            if (ChunkTimings.Count >= 512) ChunkTimings.RemoveAt(0);
            ChunkTimings.Add(new ChunkTiming
            {
                name = c.root.name, born = c.bornFrame, done = c.completeFrame, renderers = c.byRenderer.Count, lights = c.lights.Count,
                bornDistance = c.bornDistance, doneDistance = ChunkDistance(c.root), burst = burstNow,
            });
        }
    }

    // ------------------------------------------------------------------ stream-in backlog and gate (fix round 32)
    static Vector3 sceneCamPos;
    static bool haveSceneCam, burstNow;
    static float sliceBudgetMs = .2f;
    static int nearBacklog, nearUnbuilt;
    static int lastNearBirth = -1000000;
    static readonly Dictionary<long, int> chunkCoords = new Dictionary<long, int>();   // map square -> chunk roots on it (fix round 33)
    static Vector3 lastGateCamPos;
    static int lastGateCamFrame = -1;
    static long CoordKey(int x, int z) => ((long)x << 32) ^ (uint)z;
    /// <summary>Harness: chunk squares within StreamGateRadius of the camera that the map has not built (this frame).</summary>
    internal static int NearUnbuilt => nearUnbuilt;

    /// <summary>Chunk squares within StreamGateRadius of the camera with no chunk root yet (the map still building there).</summary>
    static int CountNearUnbuilt()
    {
        if (!haveSceneCam || mapRoot == null) return 0;
        var size = MapGrid.ChunkSize; var r = StreamGateRadius;
        var lp = sceneCamPos - mapRoot.position;
        int x0 = Mathf.FloorToInt((lp.x - r) / size), x1 = Mathf.FloorToInt((lp.x + r) / size);
        int z0 = Mathf.FloorToInt((lp.z - r) / size), z1 = Mathf.FloorToInt((lp.z + r) / size);
        var n = 0;
        for (var cz = z0; cz <= z1; cz++)
        for (var cx = x0; cx <= x1; cx++)
        {
            var dx = Mathf.Max(Mathf.Max(cx * size - lp.x, lp.x - (cx + 1) * size), 0f);
            var dz = Mathf.Max(Mathf.Max(cz * size - lp.z, lp.z - (cz + 1) * size), 0f);
            if (dx * dx + dz * dz >= r * r) continue;
            if (!chunkCoords.ContainsKey(CoordKey(cx, cz))) n++;
        }
        return n;
    }
    /// <summary>Harness: chunks born within StreamGateRadius of the traced camera this session.</summary>
    internal static int NearBirths;
    /// <summary>Harness: chunks within StreamGateRadius of the traced camera whose first scan is not done (this frame).</summary>
    internal static int NearBacklog => nearBacklog;
    /// <summary>Harness: frames that ran the burst budget, holds started, holds that opened on the timeout.</summary>
    internal static int BurstFrames, GateHolds, GateTimeouts;

    /// <summary>Camera to the chunk's square (XZ, metres): chunk roots sit at their south-west corner (MapWorld.BuildInto).</summary>
    static float ChunkDistance(Transform root)
    {
        if (root == null || !haveSceneCam) return float.MaxValue;
        var o = root.position;
        var size = MapGrid.ChunkSize;
        var dx = Mathf.Max(Mathf.Max(o.x - sceneCamPos.x, sceneCamPos.x - (o.x + size)), 0f);
        var dz = Mathf.Max(Mathf.Max(o.z - sceneCamPos.z, sceneCamPos.z - (o.z + size)), 0f);
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    static int CountNearBacklog()
    {
        if (!haveSceneCam) return 0;
        var n = 0;
        foreach (var c in chunks.Values)
            if (!c.complete && c.root != null && ChunkDistance(c.root) < StreamGateRadius) n++;
        return n;
    }

    /// <summary>The queued chunk nearest the camera (FIFO before any camera traced).</summary>
    static ChunkRec TakeNearest()
    {
        var best = 0;
        if (haveSceneCam && rescanQueue.Count > 1)
        {
            var bestD = float.MaxValue;
            for (var k = 0; k < rescanQueue.Count; k++)
            {
                var d = ChunkDistance(rescanQueue[k].root);
                if (d < bestD) { bestD = d; best = k; }
            }
        }
        var c = rescanQueue[best];
        rescanQueue.RemoveAt(best);
        return c;
    }

    sealed class Gate
    {
        public bool hold;
        public int holdSince, clearFrame = -1, fadeFrom = -1, lastFrame = -1, lastCoverage = -1, readings;
        public float weight = 1f;
        public string last = "";
    }
    static readonly Dictionary<int, Gate> gates = new Dictionary<int, Gate>();
    static readonly long[] coverage = new long[3];
    static bool blasBurst;

    /// <summary>Harness: a camera is held or fading in after a stream-in.</summary>
    internal static bool GateBusy
    {
        get
        {
            foreach (var g in gates.Values) if (g.hold || g.fadeFrom >= 0) return true;
            return false;
        }
    }

    /// <summary>Harness: the gate of this camera ("open", "hold since frame N", "fade k/n", and the last release).</summary>
    internal static string GateLine(FrontRoomsGlassRTCamera state)
    {
        if (state == null || !gates.TryGetValue(state.id, out var g)) return "no gate yet (not traced)";
        var now = g.hold ? "hold since " + g.holdSince + (g.clearFrame >= 0 ? ", quiet since " + g.clearFrame + " (" + g.readings + " traced)" : ", backlog " + nearBacklog + ", unbuilt " + nearUnbuilt + ", last near birth " + lastNearBirth)
                : g.fadeFrom >= 0 ? "fade " + (Time.frameCount - g.fadeFrom + 1) + "/" + GateFadeFrames : "open";
        return now + (g.last.Length > 0 ? " | last release: " + g.last : "");
    }

    /// <summary>This camera's weight for this frame (0..1): 0 while a stream-in backlog is near or its own last frame after
    /// the backlog still missed too much, then a fade-in. Main thread, once per BuildFrame.</summary>
    static float GateWeight(FrontRoomsGlassRTCamera state)
    {
        if (!StreamGate || state == null) return 1f;
        var f = Time.frameCount;
        // a camera's first traced frames are held too: the BLASes of a scene registered during the kernel compile may still
        // be building when RT turns on (the gate opens as soon as its own frame shows no holes)
        if (!gates.TryGetValue(state.id, out var g)) { g = new Gate { hold = true, holdSince = f }; gates[state.id] = g; GateHolds++; }
        if (g.lastFrame == f) return g.weight;   // a second render of this camera in the same frame
        g.lastFrame = f;
        // a jump (the camera moved more than GateJumpMeters since the last frame) into an area the map has not built counts
        // as a near birth, also before the map's first new chunk appears
        if (lastGateCamFrame != f)
        {
            nearUnbuilt = CountNearUnbuilt();
            if (lastGateCamFrame >= 0 && (sceneCamPos - lastGateCamPos).sqrMagnitude > GateJumpMeters * GateJumpMeters && nearUnbuilt > 0) lastNearBirth = f;
            lastGateCamPos = sceneCamPos; lastGateCamFrame = f;
        }
        var busy = nearBacklog > 0 || f - lastNearBirth <= GateQuietFrames || (nearUnbuilt > 0 && f - lastNearBirth <= GateBuildWaitFrames);
        if (busy)
        {
            if (!g.hold) { g.hold = true; g.holdSince = f; GateHolds++; }
            g.clearFrame = -1;
            g.fadeFrom = -1;
        }
        else if (g.hold)
        {
            if (g.clearFrame < 0) { g.clearFrame = f; g.readings = 0; }
            // this camera's own frames submitted after the area went quiet; a frame that saw no glass proves nothing (the
            // weight does not matter then) and does not count towards the timeout
            var ok = false;
            var glass = 0L; var miss = 0L;
            if (Native.FRGlassRT_GetCameraCoverage(state.id, coverage) == 1 && coverage[0] > g.clearFrame && coverage[0] != g.lastCoverage)
            {
                g.lastCoverage = (int)coverage[0];
                glass = coverage[1]; miss = coverage[2];
                if (glass > 0)
                {
                    g.readings++;
                    ok = miss <= glass * GateMissFraction;
                }
            }
            var timeout = !ok && g.readings >= GateTimeoutFrames;
            if (ok || timeout)
            {
                g.hold = false;
                g.fadeFrom = f;
                if (!ok) GateTimeouts++;
                g.last = "held frames " + g.holdSince + "-" + f + " (" + (f - g.holdSince) + "), quiet from " + g.clearFrame
                    + (ok ? ", misses " + miss + " of " + glass + " px" : ", TIMEOUT after " + g.readings + " traced frames");
            }
        }
        UpdateBlasBurst();
        if (g.hold) { g.weight = 0f; return 0f; }
        if (g.fadeFrom >= 0)
        {
            var t = (f - g.fadeFrom + 1) / (float)Mathf.Max(1, GateFadeFrames);
            if (t >= 1f) { t = 1f; g.fadeFrom = -1; }
            g.weight = t;
            return t;
        }
        g.weight = 1f;
        return 1f;
    }

    /// <summary>A held gate counts for the burst only while its camera still traces (within this many frames).</summary>
    internal static int GateStaleFrames = 30;
    /// <summary>A held gate counts for the burst only until its area has been quiet for this many frames (a camera that waits
    /// for its first frame with glass in view, e.g. after a run start in a corridor without windows, needs no burst).</summary>
    internal static int GateBurstQuietFrames = 90;

    /// <summary>While a camera is held the render thread builds more BLASes per event (the hidden frames are a loading
    /// moment); back to the plugin default when no camera is held. Fix round 33 (review 3 R3-B1): recomputed also when a
    /// camera is released (ReleaseCamera), when the quality goes Off and on a fail-closed; Off, a fail-closed, a camera that
    /// stopped tracing (GateStaleFrames) and a hold that is only waiting for glass in a quiet area (GateBurstQuietFrames)
    /// never keep the burst on.</summary>
    internal static void UpdateBlasBurst()
    {
        if (!nativeOk) { blasBurst = false; return; }
        var any = false;
        if (!failedClosed && qualityOverride != FrontRoomsGlassRTQuality.Off)
        {
            var f = Time.frameCount;
            foreach (var g in gates.Values)
                if (g.hold && f - g.lastFrame <= GateStaleFrames && (g.clearFrame < 0 || f - g.clearFrame <= GateBurstQuietFrames)) { any = true; break; }
        }
        if (any == blasBurst) return;
        blasBurst = any;
        try { Native.FRGlassRT_SetBuildBudget(any ? GateBlasTriangles : 0, any ? GateBlasBuilds : 0); }
        catch (Exception) { }   // a plugin without the export (never reached: the ABI check comes first)
    }

    /// <summary>Harness (fix round 33): the burst BLAS budget is on; gates, live cameras, and gates whose camera is gone.</summary>
    internal static bool BlasBurst => blasBurst;
    internal static int GateCount => gates.Count;
    internal static int LiveCameras => cameras.Count;
    internal static int OrphanGates
    {
        get
        {
            var n = 0;
            foreach (var id in gates.Keys)
            {
                var live = false;
                foreach (var c in cameras) if (c != null && c.id == id) { live = true; break; }
                if (!live) n++;
            }
            return n;
        }
    }

    /// <summary>Harness: Stopwatch ticks spent in registration: [0] mesh acquire (incl. upload), [1] material block,
    /// [2] the native CPU upload call, [3] the slowest single native CPU upload.</summary>
    internal static readonly long[] RegProfile = new long[4];
    /// <summary>Harness: GetNativeTexturePtr calls (each one waits for the render thread) and their cost.</summary>
    internal static int TexturePtrCalls;
    internal static double TexturePtrMsTotal, TexturePtrMsMax;
    /// <summary>Harness: the slowest single renderer registration since the last reset (ms and what it was).</summary>
    internal static double SlowestRegistrationMs;
    internal static string SlowestRegistration = "";

    // P0 classification without MapWorld edits (P2 replaces it with the ChunkHandles payload, design §2.4 C5)
    static FrontRoomsGlassRTFlags Classify(Renderer r, Transform chunkRoot)
    {
        var f = FrontRoomsGlassRTFlags.None;
        var name = r.gameObject.name;
        if (name == "Lens") f |= FrontRoomsGlassRTFlags.EmissiveLens;
        if (name.StartsWith("Key", StringComparison.Ordinal)) f |= FrontRoomsGlassRTFlags.Dynamic;
        for (var t = r.transform.parent; t != null && t != chunkRoot; t = t.parent)
            if (t.name.StartsWith("Door hinge", StringComparison.Ordinal)) { f |= FrontRoomsGlassRTFlags.Dynamic | FrontRoomsGlassRTFlags.Door; break; }
        if (r.GetComponent<FrontRoomsMetalGlassTarget>() != null) f |= FrontRoomsGlassRTFlags.Glass;
        // (E13: the project has no Animation module, so nothing animates through an Animator; parts that scripts move are
        // children of a "Door hinge" (doors and their door closers) and already Dynamic above.)
        return f;
    }

    // Static instances: a slow round-robin re-read (props pushed or moved by other code), plus liveness.
    const int StaticShownPerFrame = 256;
    static int shownCursor;

    static void RefreshStatics()
    {
        if (insts.Count == 0) return;
        // E2 (2026-10-14): a static that other code hides or shows (renderer.enabled, its GameObject, forceRenderingOff)
        // leaves or joins the TLAS within one pass of this cursor (~14 frames for 3,600 instances); known live props (CRT,
        // EXIT sign, placard) are checked every frame instead. Before, a hidden static stayed in the reflection for good.
        var nShown = Mathf.Min(StaticShownPerFrame, insts.Count);
        for (var k = 0; k < nShown; k++)
        {
            shownCursor = (shownCursor + 1) % insts.Count;
            var i = insts[shownCursor];
            if (i == null || i.priority || i.r == null) continue;
            var shown = i.r.enabled && i.go.activeInHierarchy && !i.r.forceRenderingOff;
            if (shown == i.shown) continue;
            i.shown = shown;
            if (!shown) { if (i.blockIndex >= 0) StaticRemove(i); }
            else StaticAdd(i);
            registryVersion++;
        }
        for (var k = 0; k < StaticRefreshPerFrame; k++)
        {
            staticCursor = (staticCursor + 1) % insts.Count;
            var i = insts[staticCursor];
            if (i == null || i.priority) continue;
            if (i.r == null) { RemoveInst(i); continue; }
            Vector4 a = i.r0, b = i.r1, c = i.r2;
            RowsFrom(i, i.r.localToWorldMatrix);
            i.bounds = i.r.bounds;
            if (i.blockIndex >= 0 && (a != i.r0 || b != i.r1 || c != i.r2))
            {
                // moved by other code (rare): patch the cached record in place
                staticBlock[i.blockIndex].o2w0 = i.r0; staticBlock[i.blockIndex].o2w1 = i.r1; staticBlock[i.blockIndex].o2w2 = i.r2;
            }
        }
    }

    // ------------------------------------------------------------------ per camera: frame packet
    const int MaxTlas = 4096;                                   // FRGlassRTScene::kMaxInstances
    static readonly Native.InstanceIn[] instBuffer = new Native.InstanceIn[MaxTlas];
    static readonly Native.Lamp[] lampBuffer = new Native.Lamp[MaxLamps + 1];
    static readonly Native.Frame[] frameBox = new Native.Frame[1];
    // pinned for the session: the plugin reads the first n entries straight from these (no P/Invoke marshalling)
    static GCHandle instPin, lampPin, framePin;
    static readonly List<Inst> candidates = new List<Inst>(1024);    // this frame's priority instances
    static readonly List<(float d2, LampRec rec)> lampCandidates = new List<(float, LampRec)>(512);
    static readonly LampOrder lampOrder = new LampOrder();
    sealed class LampOrder : IComparer<(float d2, LampRec rec)>
    {
        public int Compare((float d2, LampRec rec) a, (float d2, LampRec rec) b) => a.d2.CompareTo(b.d2);
    }

    // Static instances (shells, props, lenses: everything not in the watch list) do not move, so their TLAS records
    // are cached: staticList / staticBlock hold every static within far + StaticMargin of staticCamPos, rebuilt only
    // when the registry changes (registryVersion), the camera has moved StaticMargin, or the far plane / culling mask
    // changed. Per frame the block is copied after the priority records (one Array.Copy) and only lens emission is
    // patched (ReadLensForFrame). Run 7 spent 1-2 ms of main thread per frame re-deriving these ~1,700 records.
    const float StaticMargin = 4f;   // 1 s of walking at 4 m/s between full rebuilds
    static readonly List<Inst> staticList = new List<Inst>(8192);
    static Native.InstanceIn[] staticBlock = new Native.InstanceIn[8192];
    static float[] staticKeys = new float[8192];
    static Inst[] staticSortItems = new Inst[8192];
    static int staticVersion = -1, staticMask;
    static float staticFar = -1f;
    static Vector3 staticCamPos;
    static bool staticSorted;
    internal static int StaticRebuilds;
    static readonly int[] histogram = new int[64];
    static readonly Plane[] frustum = new Plane[6];
    static readonly Dictionary<int, IntPtr> cubePtr = new Dictionary<int, IntPtr>();
    static int lastReceiversVisible;
    static uint frameCounter;

    internal struct FrameResult
    {
        public ulong seq;
        public RectInt rect;
        public int visibleReceivers;
        public bool traced;
        /// <summary>The stream-in gate's weight for this camera this frame (fix round 32), 0..1.</summary>
        public float weight;
    }

    /// <summary>Build and submit one camera's frame. Main thread, inside URP's record of that camera.</summary>
    /// <summary>Harness only: BuildFrame throws (the fail-closed test, codex audit F1).</summary>
    internal static bool HarnessThrow;

    internal static FrameResult BuildFrame(Camera cam, FrontRoomsGlassRTCamera state, int width, int height, Matrix4x4 view, Matrix4x4 gpuProjFlipped, bool forceScene = false)
    {
        mainWatch.Restart();
        cpuStartNs = nativeOk && !offLatched ? Native.FRGlassRT_ThreadCpuNs() : 0ul;
        if (HarnessThrow) throw new InvalidOperationException("harness fault injection (FrontRoomsGlassRTVerify fail-closed test)");
        InjectFault(1);
        var result = new FrameResult();
        var quality = Quality;
        CheckNative();
        if (!nativeOk || quality == FrontRoomsGlassRTQuality.Off || WebGLTargetInEditor || PermanentlyOff) { Finish(); return result; }
        sceneCamPos = cam.transform.position;   // fix round 32: nearest-first registration and the stream-in backlog
        haveSceneCam = true;
        UpdateScene(forceScene);   // also while the kernel compiles, so the scene is warm when RT turns on
        Mark(0);
        if (!Supported) { Finish(); return result; }
        result.weight = GateWeight(state);
        InjectFault(2);   // harness: a fault mid-frame, after the scene update changed the registry and the native scene

        var camPos = cam.transform.position;
        var far = cam.farClipPlane;
        var far2 = far * far;
        GeometryUtility.CalculateFrustumPlanes(cam, frustum);
        var viewProjUnflipped = cam.projectionMatrix * view;

        // ---- same-frame liveness of watched instances (receivers, glass, dynamic, manual) and visible receivers
        int x0 = int.MaxValue, y0 = int.MaxValue, x1 = int.MinValue, y1 = int.MinValue;
        var visible = 0;
        var fullScreen = false;
        var stagger = (int)(frameCounter & 7);
        for (var k = watch.Count - 1; k >= 0; k--)
        {
            var i = watch[k];
            var glassOrReceiver = i.receiver || (i.flags & (uint)FrontRoomsGlassRTFlags.Glass) != 0;
            // far dynamic instances (door leaves, keys) are re-read every 8th frame; receivers, glass, manual and
            // near dynamic instances every frame (a broken pane or a moving piece must be right on this frame)
            if (!glassOrReceiver && !i.manual && (i.handle & 7) != stagger && i.d2 >= 0f && SqrDist(i.bounds, camPos) > NearDynamic2) continue;
            if (i.r == null) { RemoveInst(i); continue; }
            var on = i.enabled && i.r.enabled && i.go.activeInHierarchy && !i.r.forceRenderingOff;
            i.d2 = on ? 0f : -1f;
            if (!on) continue;
            if (i.dynamic) { RowsFrom(i, i.r.localToWorldMatrix); i.bounds = i.r.bounds; }
            if (glassOrReceiver)
            {
                var m = i.r.sharedMaterial;
                var mid = m != null ? ObjectId(m) : 0;
                if (mid != i.materialId) Rematerial(i, m);
            }
            if (i.live != 0) UpdateLive(i, SqrDist(i.bounds, camPos) < NearLens2);
            if (!i.receiver || (cam.cullingMask & (1 << i.layer)) == 0) continue;
            if (!GeometryUtility.TestPlanesAABB(frustum, i.bounds)) continue;
            visible++;
            if (!fullScreen && !ProjectBounds(i.bounds, viewProjUnflipped, width, height, ref x0, ref y0, ref x1, ref y1)) fullScreen = true;
        }
        result.visibleReceivers = visible;
        lastReceiversVisible = visible;
        if (DebugMode == Native.DebugParity) { fullScreen = true; visible = Mathf.Max(visible, 1); }   // parity traces the whole screen
        if (visible == 0) { Finish(); return result; }
        if (fullScreen) { x0 = 0; y0 = 0; x1 = width; y1 = height; }
        x0 = Mathf.Clamp(x0 - 2, 0, width); y0 = Mathf.Clamp(y0 - 2, 0, height);
        x1 = Mathf.Clamp(x1 + 2, 0, width); y1 = Mathf.Clamp(y1 + 2, 0, height);
        if (x1 <= x0 || y1 <= y0) { Finish(); return result; }
        result.rect = new RectInt(x0, y0, x1 - x0, y1 - y0);

        Mark(1);
        // ---- lamps on this frame (the raster's lights), nearest first, cap 128; the directional fill last. The lens of
        // each lamp's fixture is read here too (every frame while lit, on the frame it goes out), before the records.
        uint signature = 2166136261u;
        var lampCount = BuildLamps(camPos, quality, ref signature);
        lastLamps = lampCount;
        ReadLensesRoundRobin(camPos);

        Mark(2);
        // ---- TLAS records: the priority class first (receivers, glass, dynamic, Relay, manual; built every frame),
        // then the cached static block, nearest first when the cap cuts it (bounds distance, never the centre)
        var cap = quality == FrontRoomsGlassRTQuality.Ultra ? MaxTlas : 2048;
        var mask = cam.cullingMask;
        candidates.Clear();
        foreach (var i in watch)
        {
            if (!i.enabled || i.d2 < 0f) continue;   // watched and dead / hidden this frame
            if ((mask & (1 << i.layer)) == 0) continue;
            if (SqrDist(i.bounds, camPos) > far2) continue;
            candidates.Add(i);
        }
        var recordOrder = DebugMode == Native.DebugHitId;
        if (recordOrder) SubmittedRenderers.Clear();
        var n = 0;
        foreach (var i in candidates)
        {
            if (n >= cap) break;
            var key = i.meshKeyOverride >= 0 ? i.meshKeyOverride : i.mesh != null ? i.mesh.key : -1;
            if (key < 0) continue;
            if (recordOrder) SubmittedRenderers.Add(i.r);
            instBuffer[n++] = Record(i, key);
            if (i.dynamic) signature = Hash(signature, i.r0, i.r1, i.r2);
        }
        LastPriority = n;
        if (staticVersion != staticLayoutVersion || staticMask != mask || !Mathf.Approximately(staticFar, far)
            || (camPos - staticCamPos).sqrMagnitude > StaticMargin * StaticMargin)
            RebuildStatics(camPos, far, mask);
        var take = Mathf.Min(staticList.Count, cap - n);
        LastDroppedNear = 0;
        if (take < staticList.Count)
        {
            if (!staticSorted) SortStatics();
            for (var k = take; k < staticList.Count; k++) if (SqrDist(staticList[k].bounds, camPos) < 400f) LastDroppedNear++;
        }
        if (take > 0)
        {
            Array.Copy(staticBlock, 0, instBuffer, n, take);
            if (recordOrder) for (var k = 0; k < take; k++) SubmittedRenderers.Add(staticList[k].r);
            n += take;
        }
        signature = Hash(signature, new Vector4(nearLensChanges, registryVersion, n, 0f));
        lastCandidates = n;

        Mark(3);
        // ---- temporal accumulation while nothing moves
        var viewProj = gpuProjFlipped * view;
        var still = AllowAccumulation && state.frameIndex > 0 && viewProj == state.lastViewProj && signature == state.lastSignature && DebugMode == Native.DebugNone;
        state.accumulation = still ? Mathf.Min(state.accumulation + 1, AccumulationCap) : 0;
        state.lastViewProj = viewProj;
        state.lastSignature = signature;
        state.frameIndex++;

        // ---- frame constants
        var f = new Native.Frame();
        f.invViewProj = viewProj.inverse;
        f.viewProj = viewProj;
        f.camPos = new Vector4(camPos.x, camPos.y, camPos.z, cam.nearClipPlane);
        var fwd = cam.transform.forward;
        f.camFwd = new Vector4(fwd.x, fwd.y, fwd.z, far);
        f.screen = new Vector4(width, height, 1f / width, 1f / height);
        f.rectX0 = (uint)x0; f.rectY0 = (uint)y0; f.rectX1 = (uint)x1; f.rectY1 = (uint)y1;
        var fogOn = RenderSettings.fog && RenderSettings.fogMode == FogMode.ExponentialSquared;
        var spread = 2f * Mathf.Tan(.5f * cam.fieldOfView * Mathf.Deg2Rad) / height;
        f.fog = new Vector4(RenderSettings.fogDensity * 1.2011224f, fogOn ? 1f : 0f, spread, Time.time);
        var fc = RenderSettings.fogColor.linear;
        f.fogColor = new Vector4(fc.r, fc.g, fc.b, LensInteriorFilter);
        AmbientSH(ref f);
        var env = EnvCube(out var envValid);
        f.envDecode = ReflectionProbe.defaultTextureHDRDecodeValues;
        f.envInfo = new Vector4(6f, envValid ? 1f : 0f, 1f, 0f);
        f.lampCount = (uint)lampCount;
        f.quality = (uint)quality;
        f.debugMode = DebugMode;
        f.instanceCount = (uint)n;
        f.maxShadowLamps = (uint)(quality == FrontRoomsGlassRTQuality.Ultra ? UltraShadowLamps : HighShadowLamps);
        var ultraQ = quality == FrontRoomsGlassRTQuality.Ultra;
        f.soft = new Vector4(ShadowLightRadius, ultraQ ? UltraShadowRays : HighShadowRays, ultraQ ? UltraExtraRays : HighExtraRays, SmudgeBlurBelow);
        f.glassLayers = quality == FrontRoomsGlassRTQuality.Ultra ? 2u : 1u;
        f.frameIndex = frameCounter++;
        f.accumulationIndex = (uint)state.accumulation;
        var acc = state.accumulation;
        f.jitter = acc > 0 ? new Vector4(Halton(acc, 2) - .5f, Halton(acc, 3) - .5f, 1f / (acc + 1), 1f) : Vector4.zero;
        f.thin = new Vector4(.006f, 1.52f, .002f, BackSurfaceImage ? 1f : 0f);
        f.blur = new Vector4(24f * height / 1440f, quality == FrontRoomsGlassRTQuality.Ultra ? EdgeFilterUltra : EdgeFilterHigh, 4f,
                             quality == FrontRoomsGlassRTQuality.Ultra ? .15f : 0f);
        f.fill = new Vector4(HarnessShadowKnobs.x, HarnessShadowKnobs.y, HarnessShadowKnobs.z, -1f);

        Mark(4);
        if (!instPin.IsAllocated)
        {
            instPin = GCHandle.Alloc(instBuffer, GCHandleType.Pinned);
            lampPin = GCHandle.Alloc(lampBuffer, GCHandleType.Pinned);
            framePin = GCHandle.Alloc(frameBox, GCHandleType.Pinned);
        }
        frameBox[0] = f;
        result.seq = Native.FRGlassRT_SubmitFrame(state.id, Time.frameCount, framePin.AddrOfPinnedObject(), instPin.AddrOfPinnedObject(), n,
                                                  lampPin.AddrOfPinnedObject(), lampCount, env);
        Mark(5);
        result.traced = result.seq != 0;
        Finish();
        return result;
    }

    /// <summary>No trace this frame: the plugin event still applies queued ops and builds BLASes (warm-up packet).</summary>
    internal static ulong Warmup()
    {
        CheckNative();
        if (!nativeOk || failedClosed || PermanentlyOff) return 0;
        return Native.FRGlassRT_SubmitWarmup(Time.frameCount);
    }

    /// <summary>Main-thread ms of the last traced frame: scene diff/registration, liveness+receivers, TLAS candidates,
    /// lamps, constants (SH, cube, fog), native submit (harness / report).</summary>
    internal static readonly float[] Breakdown = new float[6];
    static double markLast;
    static void Mark(int k)
    {
        var now = mainWatch.Elapsed.TotalMilliseconds;
        Breakdown[k] = (float)(now - (k == 0 ? 0 : markLast));
        markLast = now;
    }

    static void Finish()
    {
        mainWatch.Stop();
        mainMs = (float)mainWatch.Elapsed.TotalMilliseconds;
        mainMsMax = Mathf.Max(mainMsMax, mainMs);
        // the same span in this thread's CPU time: what the frame cost without the time other processes took the core
        // away (every run of this verification is UNDER LOAD; review 1 engineering #9)
        mainCpuMs = cpuStartNs != 0ul ? (float)((Native.FRGlassRT_ThreadCpuNs() - cpuStartNs) * 1e-6) : mainMs;
    }

    static ulong cpuStartNs;
    static float mainCpuMs;
    /// <summary>Harness: thread-CPU ms of the last BuildFrame (wall ms is mainMs).</summary>
    internal static float MainCpuMs => mainCpuMs;

    internal static void ResetMaxima() { mainMsMax = 0f; if (nativeOk) Native.FRGlassRT_ResetStatMax(); }

    /// <summary>Harness: true = event barriers around the trace on every frame (the P0 placement), false = only when a
    /// camera texture is not hazard-tracked (default).</summary>
    internal static bool ForceBarriers
    {
        set { CheckNative(); if (nativeOk) Native.FRGlassRT_SetBarrierMode(value ? 1 : 0); }
    }

    /// <summary>Harness: false = build the TLAS on every traced frame (the pre-fix-31 path, for the A/B cost rounds).</summary>
    internal static bool TlasReuse
    {
        set { CheckNative(); if (nativeOk) Native.FRGlassRT_SetTlasReuse(value ? 1 : 0); }
    }

    static void Rematerial(Inst i, Material m)
    {
        i.materialId = m != null ? ObjectId(m) : 0;
        var geometry = i.mesh != null ? i.mesh.geometrySubmesh : new[] { 0 };
        var old = i.block;
        i.block = AcquireBlock(i.r.sharedMaterials, geometry);
        i.materialBase = (uint)i.block.first;
        ReleaseBlock(old);
        var receiver = IsReceiverMaterial(m);
        var flags = (FrontRoomsGlassRTFlags)i.flags;
        if (receiver) flags |= FrontRoomsGlassRTFlags.Receiver | FrontRoomsGlassRTFlags.Glass; else flags &= ~FrontRoomsGlassRTFlags.Receiver;
        i.flags = (uint)flags;
        i.receiver = receiver;
        if (receiver) SetPrepassBit(i, PrepassBitFor(m)); else ClearPrepassBit(i);
    }

    static Native.InstanceIn Record(Inst i, int key) => new Native.InstanceIn
    {
        o2w0 = i.r0, o2w1 = i.r1, o2w2 = i.r2, meshKey = key, materialBase = i.materialBase, flags = i.flags,
        windowId = (uint)i.windowId, emission = i.emission, mask = i.mask,
    };

    /// <summary>Re-derive the cached static block: every enabled static instance whose bounds are within far +
    /// StaticMargin of the camera (so the block stays valid while the camera moves less than StaticMargin).</summary>
    static void RebuildStatics(Vector3 camPos, float far, int mask)
    {
        StaticRebuilds++;
        foreach (var i in staticList) i.blockIndex = -1;
        staticList.Clear();
        var reach = far + StaticMargin;
        var reach2 = reach * reach;
        foreach (var i in insts)
        {
            if (i == null || i.priority || !i.enabled || !i.shown) continue;
            if ((mask & (1 << i.layer)) == 0) continue;
            if (i.meshKeyOverride < 0 && i.mesh == null) continue;
            var d2 = SqrDist(i.bounds, camPos);
            if (d2 > reach2) continue;
            i.d2 = d2;
            staticList.Add(i);
        }
        staticSorted = false;
        WriteStaticBlock();
        staticVersion = staticLayoutVersion;
        staticMask = mask;
        staticFar = far;
        staticCamPos = camPos;
    }

    /// <summary>A static instance joins the cached block (registration, enable): appended, no rebuild.</summary>
    static void StaticAdd(Inst i)
    {
        if (staticVersion < 0 || i.blockIndex >= 0 || i.priority || !i.enabled || !i.alive || !i.shown) return;
        if ((staticMask & (1 << i.layer)) == 0) return;
        var key = i.meshKeyOverride >= 0 ? i.meshKeyOverride : i.mesh != null ? i.mesh.key : -1;
        if (key < 0) return;
        var reach = staticFar + StaticMargin;
        var d2 = SqrDist(i.bounds, staticCamPos);
        if (d2 > reach * reach) return;
        i.d2 = d2;
        var k = staticList.Count;
        if (staticBlock.Length <= k) Array.Resize(ref staticBlock, Mathf.NextPowerOfTwo(k + 1));
        staticList.Add(i);
        i.blockIndex = k;
        staticBlock[k] = Record(i, key);
        staticSorted = false;
        StaticPatches++;
    }

    /// <summary>A static instance leaves the cached block (removal, disable): the last record moves into its slot.</summary>
    static void StaticRemove(Inst i)
    {
        var k = i.blockIndex;
        var last = staticList.Count - 1;
        i.blockIndex = -1;
        if (k < 0 || k > last || staticList[k] != i) return;
        if (k != last)
        {
            var moved = staticList[last];
            staticList[k] = moved;
            moved.blockIndex = k;
            staticBlock[k] = staticBlock[last];
        }
        staticList.RemoveAt(last);
        staticSorted = false;
        StaticPatches++;
    }

    internal static int StaticPatches;

    static void WriteStaticBlock()
    {
        Grow(ref staticBlock, staticList.Count);
        for (var k = 0; k < staticList.Count; k++)
        {
            var i = staticList[k];
            i.blockIndex = k;
            staticBlock[k] = Record(i, i.meshKeyOverride >= 0 ? i.meshKeyOverride : i.mesh.key);
        }
    }

    /// <summary>Only when the cap cuts the static block (more than 2,048 / 4,096 records near the camera): nearest first.</summary>
    static void SortStatics()
    {
        var c = staticList.Count;
        Grow(ref staticKeys, c);
        Grow(ref staticSortItems, c);
        for (var k = 0; k < c; k++) { staticKeys[k] = staticList[k].d2; staticSortItems[k] = staticList[k]; }
        Array.Sort(staticKeys, staticSortItems, 0, c);
        staticList.Clear();
        for (var k = 0; k < c; k++) { staticList.Add(staticSortItems[k]); staticSortItems[k] = null; }
        WriteStaticBlock();
        staticSorted = true;
    }

    /// <summary>Lenses whose emission no lit lamp refreshes this frame (unlit fixtures, lamps beyond LampRadius, a lens
    /// without a Light): 48 per frame, round-robin. Dead entries leave the list here.</summary>
    static void ReadLensesRoundRobin(Vector3 camPos)
    {
        var budget = Mathf.Min(48, lensList.Count);
        for (var k = 0; k < budget && lensList.Count > 0; k++)
        {
            if (lensCursor >= lensList.Count) lensCursor = 0;
            var i = lensList[lensCursor];
            if (i == null || !i.alive)
            {
                lensList[lensCursor] = lensList[lensList.Count - 1];
                lensList.RemoveAt(lensList.Count - 1);
                continue;
            }
            lensCursor++;
            if (i.lensLight == null || !i.lensLit) ReadLensForFrame(i, SqrDist(i.bounds, camPos) < NearLens2);
        }
    }

    static bool ProjectBounds(Bounds b, Matrix4x4 vp, int w, int h, ref int x0, ref int y0, ref int x1, ref int y1)
    {
        var c = b.center; var e = b.extents;
        for (var k = 0; k < 8; k++)
        {
            var p = new Vector3(c.x + ((k & 1) != 0 ? e.x : -e.x), c.y + ((k & 2) != 0 ? e.y : -e.y), c.z + ((k & 4) != 0 ? e.z : -e.z));
            var clip = vp * new Vector4(p.x, p.y, p.z, 1f);
            if (clip.w <= 1e-3f) return false;
            var vx = (clip.x / clip.w * .5f + .5f) * w;
            // the targets are rendered with the flipped (render-into-texture) projection: texture row = viewport y * height
            var vy = (clip.y / clip.w * .5f + .5f) * h;
            x0 = Mathf.Min(x0, Mathf.FloorToInt(vx)); x1 = Mathf.Max(x1, Mathf.CeilToInt(vx));
            y0 = Mathf.Min(y0, Mathf.FloorToInt(vy)); y1 = Mathf.Max(y1, Mathf.CeilToInt(vy));
        }
        return true;
    }

    /// <summary>Harness: lens property-block reads and lens checks skipped by the level test (this session).</summary>
    internal static long LensReads, LensSkips;
    /// <summary>Harness / experiment, OFF (fix round 31): true = skip a lens read while its lamp's logical level moved by
    /// 1 % or less. Measured in the walk: it skipped only 22 % of the reads (the lamps' shimmer moves most levels by more
    /// than 1 % per frame) and the B3 same-frame correlation fell from 1.000 to 0.990 (bar 0.99). Off by default: every
    /// lens within LensReadRadius is read every frame, as before; the main-thread fix for the lamps is the map's P2 lamp
    /// events (CopyLampStates).</summary>
    internal static bool LensLevelSkip = false;
    static float lastHoldNorth = -1f, lastHoldSouth = -1f;
    static bool lensForceRead = true;

    static void OnMapLiveApplied() => lensForceRead = true;

    static int BuildLamps(Vector3 camPos, FrontRoomsGlassRTQuality quality, ref uint signature)
    {
        lampCandidates.Clear();
        var r2 = LampRadius * LampRadius;
        var lensR2 = LensReadRadius * LensReadRadius;
        // the start-area holds scale the lens glow of the fixtures near the start (MapWorld.TickFixtures `held`)
        var force = lensForceRead || !LensLevelSkip || map == null;
        if (map != null && (map.StartLampsNorth != lastHoldNorth || map.StartLampsSouth != lastHoldSouth))
        {
            lastHoldNorth = map.StartLampsNorth; lastHoldSouth = map.StartLampsSouth;
            force = true;
        }
        lensForceRead = false;
        foreach (var c in chunks.Values)
        {
            if (c.lights.Count == 0) continue;
            var cd = (c.lightCenter - camPos).magnitude - c.lightRadius;
            if (cd > LensReadRadius) continue;
            foreach (var rec in c.lights)
            {
                var d2 = (rec.pos - camPos).sqrMagnitude;
                if (d2 > lensR2) continue;
                var l = rec.light;
                if (l == null) continue;
                var lit = l.isActiveAndEnabled;
                var lens = rec.lens;
                if (lens != null && lens.alive)
                {
                    // MapWorld.TickFixtures writes every lens glow every frame on the desktop as emission x max(0.04,
                    // level x held): the glow moves only when the lamp's logical level (LampLevel) or the start hold moves.
                    // The property block is read when the level moved by more than 1 % (a stutter, a dropout, a blink, a
                    // warning dip: read on that frame) or the light switched; the steady 2-3 % shimmer is followed in 1 %
                    // steps. Without a parsable cell, or a cell the map does not know, the lens is read every frame as
                    // before (run 8: an unlit stuttering lamp read only round-robin reflected with r 0.585).
                    var read = true;
                    if (!force && rec.hasCell && lit == rec.lastLit)
                    {
                        var lv = map.LampLevel(rec.cell);
                        if (lv != FrontRoomsMapWorld.NoLamp && Mathf.Abs(lv - rec.lastLevel) <= Mathf.Max(.004f, .01f * rec.lastLevel)) read = false;
                    }
                    if (read)
                    {
                        if (LensLevelSkip && rec.hasCell && map != null) rec.lastLevel = map.LampLevel(rec.cell);
                        ReadLensForFrame(lens, d2 < NearLens2);
                        LensReads++;
                    }
                    else LensSkips++;
                    rec.lastLit = lit;
                    lens.lensLit = lit;
                }
                if (!lit || d2 > r2) continue;
                lampCandidates.Add((d2, rec));
            }
        }
        // lenses without a Light of their own (none in today's map; kept safe): every frame within LitLens
        for (var k = lightlessLenses.Count - 1; k >= 0; k--)
        {
            var i = lightlessLenses[k];
            if (i == null || !i.alive) { lightlessLenses.RemoveAt(k); continue; }
            var d2 = SqrDist(i.bounds, camPos);
            if (d2 < LitLens2) ReadLensForFrame(i, d2 < NearLens2);
        }
        lampCandidates.Sort(lampOrder);
        var n = 0;
        var linear = GraphicsSettings.lightsUseLinearIntensity;
        foreach (var (d2, rec) in lampCandidates)
        {
            if (n >= Mathf.Min(MaxLamps, LampCap)) break;
            var l = rec.light;
            var intensity = l.intensity;
            if (intensity <= 0f) continue;
            var col = rec.colorLinear * intensity;   // URP: lightsUseLinearIntensity -> color.linear * intensity
            var shadow = l.shadows != LightShadows.None ? rec.shadowStrength : 0f;
            var lamp = new Native.Lamp
            {
                posInvRange2 = new Vector4(rec.pos.x, rec.pos.y, rec.pos.z, rec.invRange2),
                color = new Vector4(col.r, col.g, col.b, shadow),
            };
            if (rec.type == LightType.Spot)
            {
                lamp.atten = new Vector4(rec.invRange2, 0f, rec.invAngleRange, rec.angleAdd);
                lamp.spotDir = new Vector4(-rec.fwd.x, -rec.fwd.y, -rec.fwd.z, Native.LampSpot);
            }
            else
            {
                lamp.atten = new Vector4(rec.invRange2, 0f, 0f, 1f);
                lamp.spotDir = new Vector4(0f, -1f, 0f, Native.LampPoint);
            }
            lampBuffer[n++] = lamp;
            if (d2 < 400f) signature = Hash(signature, lamp.color);
        }
        if (dirLight != null && dirLight.isActiveAndEnabled && dirLight.intensity > 0f && n < lampBuffer.Length)
        {
            var col = FinalColor(dirLight, linear);
            var shadowed = dirLight.shadows != LightShadows.None;
            var w = 0f;
            if (shadowed)
            {
                if (quality == FrontRoomsGlassRTQuality.Ultra) w = dirLight.shadowStrength;   // a fill shadow ray
                else col *= 1f - dirLight.shadowStrength;                                        // High: the constant 0.82 of design §1.5
            }
            var d = -dirLight.transform.forward;
            lampBuffer[n++] = new Native.Lamp
            {
                posInvRange2 = new Vector4(d.x, d.y, d.z, 0f),
                color = new Vector4(col.r, col.g, col.b, w),
                spotDir = new Vector4(0f, 0f, 0f, Native.LampDirectional),
                atten = new Vector4(0f, 0f, 0f, 1f),
            };
        }
        return n;
    }

    static Color FinalColor(Light l, bool linearIntensity)
    {
        var c = linearIntensity ? l.color.linear * l.intensity : (l.color * l.intensity).linear;
        if (GraphicsSettings.lightsUseColorTemperature && l.useColorTemperature) c *= Mathf.CorrelatedColorTemperatureToRGB(l.colorTemperature);
        return c;
    }

    // URP SampleSH from RenderSettings.ambientProbe, rewritten in the kernel's basis {1, x, y, z, xy, yz, xz, x², y²}
    // (z² = 1 - x² - y²). Same constants Unity uses for unity_SHAr..unity_SHC.
    static readonly Vector4[] shScratch = new Vector4[9];
    static void AmbientSH(ref Native.Frame f)
    {
        var sh = RenderSettings.ambientProbe;
        var o = shScratch;
        for (var ch = 0; ch < 3; ch++)
        {
            var shaX = sh[ch, 3]; var shaY = sh[ch, 1]; var shaZ = sh[ch, 2]; var shaW = sh[ch, 0] - sh[ch, 6];
            var shbX = sh[ch, 4]; var shbY = sh[ch, 5]; var shbZ = sh[ch, 6] * 3f; var shbW = sh[ch, 7];
            var shc = sh[ch, 8];
            o[0][ch] = shaW + shbZ; o[1][ch] = shaX; o[2][ch] = shaY; o[3][ch] = shaZ; o[4][ch] = shbX;
            o[5][ch] = shbY; o[6][ch] = shbW; o[7][ch] = shc - shbZ; o[8][ch] = -shc - shbZ;
        }
        f.sh0 = o[0]; f.sh1 = o[1]; f.sh2 = o[2]; f.sh3 = o[3]; f.sh4 = o[4]; f.sh5 = o[5]; f.sh6 = o[6]; f.sh7 = o[7]; f.sh8 = o[8];
    }

    static IntPtr EnvCube(out bool valid)
    {
        valid = false;
        Texture t = null;
        if (RenderSettings.defaultReflectionMode == DefaultReflectionMode.Custom && RenderSettings.customReflectionTexture != null) t = RenderSettings.customReflectionTexture;
        else t = ReflectionProbe.defaultTexture;
        if (t == null || t.dimension != TextureDimension.Cube) return IntPtr.Zero;
        var id = ObjectId(t);
        if (!cubePtr.TryGetValue(id, out var p))
        {
            if (t is RenderTexture rt && !rt.IsCreated()) return IntPtr.Zero;
            p = t.GetNativeTexturePtr();   // once per cube object
            cubePtr[id] = p;
        }
        valid = p != IntPtr.Zero;
        return p;
    }

    static float Halton(int index, int b)
    {
        float f = 1f, r = 0f;
        var i = index;
        while (i > 0) { f /= b; r += f * (i % b); i /= b; }
        return r;
    }

    static uint Hash(uint h, Vector4 a)
    {
        unchecked
        {
            h = (h ^ (uint)a.x.GetHashCode()) * 16777619u;
            h = (h ^ (uint)a.y.GetHashCode()) * 16777619u;
            h = (h ^ (uint)a.z.GetHashCode()) * 16777619u;
            h = (h ^ (uint)a.w.GetHashCode()) * 16777619u;
        }
        return h;
    }

    static uint Hash(uint h, Vector4 a, Vector4 b, Vector4 c) => Hash(Hash(Hash(h, a), b), c);

    // ------------------------------------------------------------------ camera targets -> native
    internal static void SendTargets(FrontRoomsGlassRTCamera s)
    {
        if (!nativeOk) return;
        IntPtr P(RTHandle h) => h != null && h.rt != null ? h.rt.GetNativeTexturePtr() : IntPtr.Zero;   // once per (re)allocation (R23)
        Native.FRGlassRT_SetCameraTargets(s.id, P(s.depth), P(s.normal), P(s.raw), P(s.aux), P(s.rawAA), P(s.outTex), P(s.ids), s.width, s.height);
    }

    // ------------------------------------------------------------------ stats
    public static bool TryGetStats(out FrontRoomsGlassRTStats s)
    {
        s = default;
        CheckNative();
        if (!nativeOk) return false;
        float G(Native.Stat x) => (float)Native.Get(x);
        int I(Native.Stat x) => (int)Native.Get(x);
        s.gpuMsAccel = G(Native.Stat.GpuMsAS); s.gpuMsTrace = G(Native.Stat.GpuMsTrace); s.gpuMsResolve = G(Native.Stat.GpuMsResolve);
        s.gpuMsTotal = G(Native.Stat.GpuMsTotal); s.gpuMsCommandBuffer = G(Native.Stat.GpuMsCB); s.gpuMsFrame = G(Native.Stat.TimerFrameGpuMs);
        s.encodeMs = G(Native.Stat.EncodeMs); s.encodeMsMax = G(Native.Stat.EncodeMsMax); s.compileMs = G(Native.Stat.CompileMs);
        s.mainThreadMs = mainMs; s.mainThreadMsMax = mainMsMax; s.mainThreadCpuMs = mainCpuMs;
        s.tlasBuilt = I(Native.Stat.TlasBuilt); s.tlasReused = I(Native.Stat.TlasReused);
        s.tlasInstances = I(Native.Stat.TlasInstances); s.candidates = lastCandidates; s.meshes = I(Native.Stat.Meshes); s.meshesPending = I(Native.Stat.MeshesPending);
        s.blasBuiltTotal = I(Native.Stat.BlasBuiltTotal); s.compacted = I(Native.Stat.Compacted); s.retired = I(Native.Stat.Retired);
        s.lamps = lastLamps; s.receiversVisible = lastReceiversVisible;
        s.glassPixels = I(Native.Stat.GlassPx); s.missPixels = I(Native.Stat.MissPx); s.hitPixels = I(Native.Stat.HitPx); s.layerPixels = I(Native.Stat.LayerPx);
        s.aaPixels = I(Native.Stat.AAPx); s.shadowRays = I(Native.Stat.ShadowRays);
        s.events = I(Native.Stat.Events); s.traceEvents = I(Native.Stat.TraceEvents); s.skippedNotReady = I(Native.Stat.SkippedNotReady);
        s.ringBusy = I(Native.Stat.RingBusy); s.commandBufferErrors = I(Native.Stat.CbErrors);
        s.blasBytes = (long)Native.Get(Native.Stat.BlasBytes); s.geometryBytes = (long)Native.Get(Native.Stat.GeometryBytes);
        foreach (var c in cameras) if (c != null) { s.targetBytes += c.targetBytes; s.accumulation = Mathf.Max(s.accumulation, c.accumulation); }
        s.residency = Native.Get(Native.Stat.ResidencyMode) == 1 ? "residency set" : Native.Get(Native.Stat.ResidencyMode) == 2 ? "useResource" : "none";
        s.hazardModes = "depth " + Native.Get(Native.Stat.HazardDepth) + ", out " + Native.Get(Native.Stat.HazardOut);
        return true;
    }

    // ------------------------------------------------------------------ harness helpers
    internal static int RegisteredInstances => liveCount;
    /// <summary>Hit-id debug only: the renderer of each TLAS instance index of the last frame (all meshes built).</summary>
    internal static readonly List<Renderer> SubmittedRenderers = new List<Renderer>();
    internal static int LastDroppedNear, LastPriority;
    internal static int KnownChunks => chunks.Count;
    internal static int PendingRescans => rescanQueue.Count;
    internal static int MeshUploadsCPU => meshesUploadedCPU;
    internal static int MeshUploadsGPU => meshesUploadedGPU;
    internal static int MeshUploadFailures => meshUploadFailures;
    /// <summary>Material records in use (E3: released with their last instance); MaterialRecordsHigh = the table's high-water mark.</summary>
    internal static int MaterialsUsed => materialsLive;
    internal static int MaterialRecordsHigh => nextMaterial;
    /// <summary>Texture slots in use including the 4 defaults (E3: released with their last material block).</summary>
    internal static int TexturesUsed => 4 + texturesLive;
    internal static int Lens => CountWhere(i => i.lens);
    internal static int Receivers => CountWhere(i => i.receiver);
    internal static int Dynamics => CountWhere(i => i.dynamic);
    internal static List<FrontRoomsGlassRTCamera> Cameras => cameras;

    static int CountWhere(Func<Inst, bool> p)
    {
        var n = 0;
        foreach (var i in insts) if (i != null && p(i)) n++;
        return n;
    }

    /// <summary>Harness: register every pending chunk now (no 1 ms slicing).</summary>
    internal static void Flush()
    {
        var keep = RegisterBudgetMs;
        RegisterBudgetMs = 10000f;
        UpdateScene(true);
        RegisterBudgetMs = keep;
    }

    // ------------------------------------------------------------------ Play-mode exit (codex audit F4)
    // The plugin applies queued ops only inside a render event. After Play Mode ends nothing issues one, so before this the
    // native scene (1,646 meshes, 4.95 MB BLAS, 8.31 MB geometry, retained Unity textures and vertex buffers) stayed alive
    // until the next Play. Now ExitingPlayMode frees every camera and resets the scene, and EditorApplication.update pumps
    // up to ExitPumpLimit warm-up events (each one applies queued ops and purges what the GPU has completed) until the
    // native scene is empty. A player needs nothing: the process ends.
    internal static int ExitPumpEvents;
    internal static bool ExitReleased;
    const int ExitPumpLimit = 120;
#if UNITY_EDITOR
    static int exitPumpsLeft;

    [UnityEditor.InitializeOnLoadMethod]
    static void HookPlayModeExit()
    {
        UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    static void OnPlayModeChanged(UnityEditor.PlayModeStateChange change)
    {
        if (change != UnityEditor.PlayModeStateChange.ExitingPlayMode || !started || !nativeOk) return;
        foreach (var c in cameras.ToArray()) ReleaseCamera(c);
        Native.FRGlassRT_ResetScene();   // generation-safe: everything is retired with the next event's serial
        ResetStatics();                  // the C# registry forgets the dropped keys (the next Play starts clean anyway)
        ExitPumpEvents = 0;
        ExitReleased = false;
        exitPumpsLeft = ExitPumpLimit;
        UnityEditor.EditorApplication.update -= PumpExitRelease;
        UnityEditor.EditorApplication.update += PumpExitRelease;
    }

    static CommandBuffer exitPump;

    static void PumpExitRelease()
    {
        if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode && UnityEditor.EditorApplication.isPlaying) { StopPump(); return; }
        if (exitPump == null) exitPump = new CommandBuffer { name = "FRGlassRT release" };
        exitPump.Clear();
        var seq = Native.FRGlassRT_SubmitWarmup(0);
        exitPump.IssuePluginEventAndData(eventFunc, 1, new IntPtr((long)seq));
        Graphics.ExecuteCommandBuffer(exitPump);
        GL.Flush();   // commit Unity's command buffer so its completion handler (the plugin's "GPU done" serial) can run
        ExitPumpEvents++;
        var empty = Native.Get(Native.Stat.Meshes) == 0 && Native.Get(Native.Stat.Retired) == 0 && Native.Get(Native.Stat.BlasBytes) <= 0
                    && Native.Get(Native.Stat.GeometryBytes) <= 0;
        if ((empty && ExitPumpEvents >= 3) || --exitPumpsLeft <= 0) { ExitReleased = empty; StopPump(); }
    }

    static void StopPump()
    {
        UnityEditor.EditorApplication.update -= PumpExitRelease;
        if (!ExitReleased) Debug.LogWarning("[FrontRoomsGlassRT] Play-mode exit: the native scene was not empty after " + ExitPumpEvents + " release events");
    }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        // Domain reload off or a new Play session: forget every registration (EnsureStarted resets the plugin's scene).
        foreach (var i in insts) if (i != null) ClearPrepassBit(i);
        insts.Clear(); freeHandles.Clear(); watch.Clear(); chunks.Clear(); rescanQueue.Clear();
        meshById.Clear(); meshByKey.Clear(); blockSingle.Clear(); blockMulti.Clear(); textureSlot.Clear(); cubePtr.Clear();
        tickets.Clear(); cameras.Clear(); MaterialNames.Clear(); HarnessThrow = false; HarnessFault = 0;
        FrontRoomsGlassBreakable.MeshesReady -= OnGlassMeshesReady;
        FrontRoomsGlassBreakable.StageChanged -= OnGlassStageChanged;
        stageQueue.Clear(); StageRegistrations = 0; StageReuploads = 0; lastPipeline = null; PrepassRepicks = 0;
        capsFrame = -1; offLatched = false; lensForceRead = true; lastHoldNorth = lastHoldSouth = -1f; LensReads = LensSkips = 0; GatherSlices = 0;
        lensList.Clear(); lightlessLenses.Clear(); staticList.Clear(); candidates.Clear(); scratchInsts.Clear();
        lensCursor = 0; shownCursor = 0; staticVersion = -1; staticFar = -1f; registryVersion++; staticLayoutVersion++; nearLensChanges = 0;
        nextMaterial = 0; nextTexture = 4; liveCount = 0; map = null; mapRoot = null; relayRoot = null; dirLight = null;
        lastSceneFrame = -1; nextMapSearch = 0f; nextDirSearch = 0f; started = false; warnedMarker = false; HarnessShadowKnobs = Vector3.zero;
        nextMeshKey = 0; meshesUploadedCPU = meshesUploadedGPU = meshUploadFailures = 0; FailedClosed = 0;
        // a new session starts from the defaults (codex audit F1 follow-ups; matters only with domain reload off), except a
        // quality set through the API (a settings menu, the G10 proof's force-off before Play): it holds (G14V-4)
        if (!explicitOverride) qualityOverride = null;
        HarnessCoreCount = null;   // D1 (c): a test's forced core count never reaches a Play session (the IORegistry read stays cached)
        failedClosed = false;
        freeMaterialRanges.Clear(); freeTextureSlots.Clear(); materialsLive = 0; texturesLive = 0; warnedMaterials = warnedTextures = false;
        LiveInstances = 0;
        gates.Clear(); ChunkTimings.Clear(); nearBacklog = 0; haveSceneCam = false; burstNow = false; sliceBudgetMs = RegisterBudgetMs;
        BurstFrames = GateHolds = GateTimeouts = NearBirths = 0; lastNearBirth = -1000000;
        chunkCoords.Clear(); notChunkRoots.Clear(); NonChunkRoots = 0; nearUnbuilt = 0; lastGateCamFrame = -1;
        if (blasBurst && nativeOk) Native.FRGlassRT_SetBuildBudget(0, 0);
        blasBurst = false;
    }
}
#endif
