// FrontRoomsGlassDebrisWorld.cs -- GD3 steps 6-7 + GD3c (plan 10 sec 2.3 S3-S4, 2.4, 2.5, 4.3, 4.6, 4.7; build/06_debris.md;
// research/glass/destruction/gd3c/10_design.md): the shatter, the floor glass that stays, and its return after a walk away.
//
// Red 2026-10-22: "broken glass should stay on the floor instead of vanishing (unless the player walks far enough away that
// it is cleared to save memory)". FrontRoomsGlassBreakable hands the shatter over through ShatterHandler (installed here).
//
// One singleton for the whole game. It owns a SEPARATE PHYSICS SCENE (SceneManager.CreateScene with LocalPhysicsMode.Physics3D,
// stepped only by this class with PhysicsScene.Simulate): every gameplay query in the project passes the mask ~0 to the
// static Physics.* calls (the E ray, the Relay's sight ray and nav capsule, the climb), and those read only the default physics
// scene, so no shard, proxy or tooth hull here can block, be aimed at or be stood on. Nothing here adds a collider to the
// game's physics; the floor glass and teeth have no collider at all.
//
// Life of a pane (FrontRoomsGlassBreakable drives it through its static hooks; no map file is touched):
//   Crack2 (StageChanged 1 -> 2), or a pane restored at stage 2 once the camera is within ArmDistance (at most MaxArmed armed):
//     BUILD in time slices before the shatter: one render mesh per shard (slab frame, stage-2 pose: the swap is pixel-exact),
//     the teeth into one static mesh on the window root (FrontRoomsGlassFloorGlass), one convex hull per shard and tooth,
//     COOKED ON WORKER THREADS (FrontRoomsGlassDebrisJobs.HullBakeJob; F1), proxies of the world round the window.
//   Shatter (on the WindowShattered frame): the shard renderers and the teeth come on at exactly the stage-2 pose; the glass
//     holds for the shot's object stop (GlassBreak.ObjectStop[2] = 50 ms), then lets go in the generator's release waves.
//     Fixed 1/120 s substeps; IGlassShotClock.ShardTimeScale (ShardTimeScale here) scales only this step.
//   Settle: a shard freezes after 0.3 s below 0.05 m/s; from GlassBreak.SettleToStatic (1.5 s) below 0.15 m/s and 1.5 rad/s;
//     everything at 2.5 s. F3: any shard left in the clear zone of the opening (X +-0.60, Y 0.39-1.90, |Z| < 0.05) is laid flat
//     on the floor below it, on the side it leans to (crumbs there are dropped).
//   Merge (FrontRoomsGlassDebrisJobs, F2/F5/F6): every shard at its settled pose and every resting crumb into ONE floor-glass
//     mesh on the window root (+ LOD1 of the pane faces), built in a worker job; the bottom faces of flat shards are dropped;
//     44-byte vertices; then non-readable. The swap is one frame. Then the shards go back to the pool, the proxies go, the
//     breakable's working set is returned, and the compact record (FrontRoomsGlassDebrisPack, about 2 KB) is stored.
//   Release (the chunk goes, 48-96 m away: Red's "too far"): the meshes go with the chunk root; the record stays.
//   Return (the chunk is built again, beyond the far plane): the breakable regenerates the pattern in slices
//     (IFrontRoomsGlassRestoreHandler), then the teeth (minus the snapped ones) and the floor glass at the stored poses come
//     back through the same merge job. Nothing is simulated again. A changed neighbourhood (the chunk shifted) drops any
//     shard that would sit inside a new collider; a record that no longer matches the map's break is dropped (teeth only).
// A second shatter while a first window still flies freezes the first one's shards where they are on that frame (cheap) and
// its merge then runs in slices like any other (eng r1 #1: the whole merge on the shatter frame cost 21.9 ms). A chunk released
// while its glass still flies writes the record from the frozen poses and drops the meshes (no merge at all).
// Every restore (patterns, teeth, floor glass) shares ONE per-frame budget, nearest window first (eng r1 #3).
//
// Tiers (gd3c sec 3): desktop and the Editor run the shards as rigid bodies (the reference); so do handheld players until a
// device run (fix r1: FrontRoomsGlassTypeProfile.ReducedTierOnHandheld, off). The reduced tier (WebGL players: the existing
// ForTier gate, and the lab switch) gets a scripted integrator, one skinned draw and teeth + floor merged into one mesh (FrontRoomsGlassDebrisLite); nothing
// on desktop changes for it.
// G14 P4 (plan sec 4.7; a note for the G14 track, not implemented here): PiecesShown(glass, renderers) on the shatter frame;
// Settled(glass, floor) after the merge (unregister the pieces, register the teeth and the floor glass).
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

[DefaultExecutionOrder(200)]
public sealed class FrontRoomsGlassDebrisWorld : MonoBehaviour
{
    // ---- switches --------------------------------------------------------------------------------------------------------
    public static bool Enabled = true;
    public static float Substep = 1f / 120f;
    public static int MaxSubstepsPerFrame = 8;
    /// <summary>CPU per frame for the build and merge slices (ms; the build doubles under 8 frames before the shatter, x4 under 4).</summary>
    public static double BuildBudgetMs = 1.0, MergeBudgetMs = 1.0, PrewarmBudgetMs = .5, RestoreBudgetMs = 1.0;
    public static float SettleSpeed = .05f, SettleAngular = .6f, SettleQuiet = .3f, CapSpeed = .15f, CapAngular = 1.5f, HardCap = 2.5f, StuckCap = 4f;
    /// <summary>The share of shards (by release wave: inner, middle, outer) that falls back toward the breaker.</summary>
    public static float[] Backward = { .06f, .22f, .48f };
    public static float TipOutMin = .12f, TipOutMax = .35f, RadialSpread = .18f;
    /// <summary>A released shard ignores the teeth and the other shards this long (it leaves the plane it was cut from).</summary>
    public static float ExitSeconds = .10f;
    /// <summary>From this long after the shatter, a slow shard in the opening box is pushed out (m/s2) toward the side it leans to.</summary>
    public static float ClearAfter = .6f, ClearPush = 2.5f;
    /// <summary>Hard floors only (none in the live zones): pieces over 60 cm2 landing above this speed would re-break.</summary>
    public static bool FloorRebreak;
    public static float RebreakSpeed = 2f;
    /// <summary>Proxy box round the window (root frame, half sizes in m).</summary>
    public static Vector3 ProxyHalf = new Vector3(3f, 1.6f, 3.2f);
    /// <summary>The clear zone of the opening (root frame): no floor-glass vertex may stay inside it (V9, F3).</summary>
    public static Vector3 ClearMin = new Vector3(-.60f, .39f, -.05f), ClearMax = new Vector3(.60f, 1.90f, .05f);
    /// <summary>F5: a shard whose pane normal is within this many degrees of vertical lies flat (its bottom faces are dropped).</summary>
    public static float FlatDegrees = 10f;
    /// <summary>Kept progress: a pane restored at stage 2 gets its shards built once the camera is this close (m); at most MaxArmed at once.</summary>
    public static float ArmDistance = 4f;
    public static int MaxArmed = 2;
    /// <summary>A2 (art r1): at the settle, a shard more than this many degrees off flat that is not leaning on the wall is laid flat
    /// on what is under it; the dressed frame's parts (stool, nosings, aprons, casings) are proxies in the debris scene.</summary>
    public static float UprightDegrees = 30f, LeanMaxDegrees = 65f;
    public static int MaxLeaningPerWindow = 2;
    public static bool FrameProxies = true;
    /// <summary>Live floor-glass GPU memory cap (bytes): over it the oldest-broken window farther than CapFarMetres keeps LOD1 only.</summary>
    public static long LiveCapBytes = 32L << 20, LiveCapBytesReduced = 6L << 20;
    public static float CapFarMetres = 24f;
    /// <summary>Debris-scene collision layers (unused in the project; the global matrix lets them all collide). Renderers stay on 0.</summary>
    public const int LayerHeld = 28, LayerFree = 29, LayerTooth = 30, LayerWorld = 31;
    /// <summary>The shot's shard clock (A/B slow motion, default off): playback rate at t seconds after the shatter. Null = 1.</summary>
    public static Func<float, float> ShardTimeScale;
    public static Func<double> ClockMs = () => Stopwatch.GetTimestamp() * 1000.0 / Stopwatch.Frequency;
    public static Action<string> Trace;

    // ---- hooks -----------------------------------------------------------------------------------------------------------
    /// <summary>G14 P4: on the shatter frame, the shard renderers in BuildPieceSource order (null where a range is a tooth).</summary>
    public static event Action<FrontRoomsGlassBreakable, IReadOnlyList<Renderer>> PiecesShown;
    /// <summary>After the merge (or a restore): the shards are gone, the teeth and the floor glass are what is left.</summary>
    public static event Action<FrontRoomsGlassBreakable, FrontRoomsGlassFloorGlass> Settled;

    // ---- lab numbers ---------------------------------------------------------------------------------------------------
    /// <summary>The last shatter: main-thread ms and managed bytes allocated inside the handler (on the shatter frame).</summary>
    public static double LastShatterMs { get; private set; }
    public static long LastShatterAlloc { get; private set; }
    /// <summary>This frame's debris-world CPU (ms, LateUpdate: build, step, merge, restore, particles).</summary>
    public static double FrameMs { get; private set; }
    public static double WorstFrameMs { get; private set; }
    public static void ResetWorst() { WorstFrameMs = 0; }

    // ---- the singleton, its scene and the install --------------------------------------------------------------------------
    static FrontRoomsGlassDebrisWorld instance;
    static Scene scene;
    static PhysicsScene physics;
    static Transform container;
    static bool layersOk;
    static readonly Hook hook = new Hook();
    sealed class Hook : IFrontRoomsGlassShatterHandler, IFrontRoomsGlassRestoreHandler
    {
        public bool Shatter(FrontRoomsGlassBreakable glass, Vector3 impactWorld, Vector3 impulse)
        {
            if (!Enabled || !Application.isPlaying || Instance == null) return false;
            var a0 = GC.GetAllocatedBytesForCurrentThread();
            var t0 = ClockMs();
            var ok = Instance.OnShatter(glass, impactWorld, impulse);
            LastShatterMs = ClockMs() - t0;
            LastShatterAlloc = GC.GetAllocatedBytesForCurrentThread() - a0;
            return ok;
        }

        // Every shattered window rebuilt gets its pattern again: its teeth always come back, its floor glass if a record is kept.
        public bool WantsShatteredPattern(FrontRoomsGlassBreakable glass) => Enabled && Application.isPlaying && glass != null && glass.Map != null;

        // eng r1 #6: the regenerated pattern of a window rebuilt at stage 3 comes here, never through MeshesReady (G14 would queue
        // the hidden slab and stage meshes of a pane that is gone, and the merge then frees them under it).
        public void ShatteredPatternReady(FrontRoomsGlassBreakable glass)
        {
            if (!Enabled || !Application.isPlaying || glass == null) return;
            var w = Instance;
            if (w != null) w.StartRestore(glass);
        }
    }

    /// <summary>
    /// Mono (the Editor and the desktop players) compiles each method on its first call: the first shatter and merge of a
    /// session would pay a few ms of JIT on the main thread. At install, a worker thread asks the runtime to compile every method
    /// of the debris code (RuntimeHelpers.PrepareMethod: compiled, never run). IL2CPP players (WebGL, iOS) have no JIT.
    /// </summary>
    public static bool WarmUpJit = true;
    public static string JitWarmUpState { get; private set; } = "";

    static void StartJitWarmUp()
    {
#if ENABLE_IL2CPP || (UNITY_WEBGL && !UNITY_EDITOR)
        JitWarmUpState = "skipped (IL2CPP: no JIT)";
#else
        if (!WarmUpJit || JitWarmUpState.Length > 0) return;
        JitWarmUpState = "running";
        var types = new List<Type> { typeof(FrontRoomsGlassDebrisWorld), typeof(FrontRoomsGlassDebrisPack), typeof(FrontRoomsGlassDebrisJobs), typeof(FrontRoomsGlassFloorGlass),
            typeof(FrontRoomsGlassDebrisLite), typeof(FrontRoomsGlassDebris), typeof(FrontRoomsGlassVfx), typeof(FrontRoomsGlassShard) };
        System.Threading.ThreadPool.QueueUserWorkItem(_ =>
        {
            var t0 = Stopwatch.GetTimestamp();
            var n = 0;
            var failed = 0;
            var all = new List<Type>();
            foreach (var t in types) { all.Add(t); all.AddRange(t.GetNestedTypes(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)); }
            foreach (var t in all)
            {
                if (t.IsGenericTypeDefinition) continue;
                foreach (var m in t.GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly))
                {
                    if (m.IsAbstract || m.ContainsGenericParameters) continue;
                    try { System.Runtime.CompilerServices.RuntimeHelpers.PrepareMethod(m.MethodHandle); n++; } catch { failed++; }
                }
            }
            // PrepareMethod does not compile generic instantiations: run the pure managed paths once on dummy data
            try { WarmUpManaged(); } catch (Exception e) { failed++; JitWarmUpState = "warm-up error " + e.GetType().Name; }
            JitWarmUpState = "done: " + n + " methods prepared, managed paths run once (" + failed + " skipped) in " + ((Stopwatch.GetTimestamp() - t0) * 1000.0 / Stopwatch.Frequency).ToString("0.0") + " ms wall on a worker thread";
        });
#endif
    }

    // pure C#, no Unity object: safe on a worker thread
    static void WarmUpManaged()
    {
        var shards = new List<FrontRoomsGlassDebrisPack.Shard>();
        for (var i = 0; i < 4; i++) shards.Add(new FrontRoomsGlassDebrisPack.Shard { piece = (byte)i, position = new Vector3(i * .1f, 0f, .3f), rotation = Quaternion.Euler(i * 30f, 10f, 5f) });
        var crumbs = new List<FrontRoomsGlassDebris.RestingCrumb>();
        for (var i = 0; i < 24; i++) crumbs.Add(new FrontRoomsGlassDebris.RestingCrumb { position = new Vector3((i % 6) * .7f - 2f, i % 5 == 0 ? .35f : 0f, (i / 6) * .7f), rotation = Quaternion.identity, size = .004f });
        var clusters = Clusters(crumbs, 7u);
        var blob = FrontRoomsGlassDebrisPack.Pack(new FrontRoomsGlassDebrisPack.Header { edge = 1, seed = 2 }, shards, clusters);
        FrontRoomsGlassDebrisPack.Unpack(blob, out var h, new List<FrontRoomsGlassDebrisPack.Shard>(), new List<FrontRoomsGlassDebrisPack.Cluster>());
        var items = new List<FrontRoomsGlassDebrisJobs.MergeItem> { new FrontRoomsGlassDebrisJobs.MergeItem { m = Matrix4x4.TRS(Vector3.one, Quaternion.identity, Vector3.one) } };
        items[0] = items[0];
        var v = new List<FrontRoomsGlassFractureMeshes.Vertex>();
        var ix = new List<int>();
        FrontRoomsGlassVfx.AppendCrumb(v, ix, Vector3.zero, Quaternion.identity, .004f, 1, 1, Vector2.zero, 1f, 2f, 0);
        var map = new Dictionary<int, int>(8) { { 1, 2 } };
        map.TryGetValue(1, out var _);
        var cells = new HashSet<long> { FrontRoomsGlassDebris.CellKey(1f, 2f) };
        cells.Contains(3);
        FrontRoomsGlassDebrisJobs.ToFloor(new FrontRoomsGlassFractureMeshes.Vertex());
        var md = new Matrix4x4[1];
        md[0] = Matrix4x4.identity * Matrix4x4.Translate(Vector3.up);
        var b = new Bounds(Vector3.zero, Vector3.one);
        b.Encapsulate(md[0].MultiplyPoint3x4(Vector3.one));
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        StartJitWarmUp();
        FrontRoomsGlassBreakable.ShatterHandler = hook;
        FrontRoomsGlassBreakable.RestoreHandler = hook;
        FrontRoomsGlassBreakable.StageChanged -= OnStageChanged;
        FrontRoomsGlassBreakable.StageChanged += OnStageChanged;
        FrontRoomsGlassBreakable.MeshesReady -= OnMeshesReady;
        FrontRoomsGlassBreakable.MeshesReady += OnMeshesReady;
        FrontRoomsGlassBreakable.Released -= OnReleased;
        FrontRoomsGlassBreakable.Released += OnReleased;
        FrontRooms3DGame.PlayerClimbed -= OnPlayerClimbed;
        FrontRooms3DGame.PlayerClimbed += OnPlayerClimbed;
    }

    public static FrontRoomsGlassDebrisWorld Instance
    {
        get
        {
            if (instance != null && scene.IsValid() && scene.isLoaded) return instance;
            if (!Application.isPlaying) return null;
            if (!scene.IsValid() || !scene.isLoaded)
            {
                scene = SceneManager.CreateScene("FrontRooms glass debris " + Time.frameCount, new CreateSceneParameters(LocalPhysicsMode.Physics3D));
                physics = scene.GetPhysicsScene();
                shardPool.Clear();
                hullPool.Clear();
                meshPool.Clear();
            }
            // the four debris layers must collide with each other in the (global) matrix; otherwise everything stays on layer 0
            layersOk = true;
            foreach (var a in new[] { LayerHeld, LayerFree, LayerTooth, LayerWorld })
                foreach (var b in new[] { LayerHeld, LayerFree, LayerTooth, LayerWorld })
                    if (UnityEngine.Physics.GetIgnoreLayerCollision(a, b)) layersOk = false;
            var go = new GameObject("GD3 glass debris world");
            SceneManager.MoveGameObjectToScene(go, scene);
            container = go.transform;
            instance = go.AddComponent<FrontRoomsGlassDebrisWorld>();
            instance.vfx = new FrontRoomsGlassVfx();
            return instance;
        }
    }

    /// <summary>The debris physics scene (tests and the lab compare it with the default one).</summary>
    public static PhysicsScene Physics3D => physics;
    public static Scene DebrisScene => scene;
    public static bool HasInstance => instance != null;
    public static bool LayersOk => layersOk;
    public FrontRoomsGlassVfx Vfx => vfx;
    public static int PoolSize => shardPool.Count;

    /// <summary>Put an object into the debris scene (under the world's container).</summary>
    internal static void Adopt(GameObject go)
    {
        if (container == null) { var _ = Instance; }
        if (container != null) go.transform.SetParent(container, true);
    }

    static int L(int layer) => layersOk ? layer : 0;

    FrontRoomsGlassVfx vfx;
    readonly List<Run> runs = new List<Run>();
    static readonly Stack<FrontRoomsGlassShard> shardPool = new Stack<FrontRoomsGlassShard>();
    static readonly Stack<MeshCollider> hullPool = new Stack<MeshCollider>();
    static readonly Stack<Mesh> meshPool = new Stack<Mesh>();
    static readonly List<FrontRoomsGlassBreakable> pendingArm = new List<FrontRoomsGlassBreakable>();
    static PhysicsMaterial shardMaterial, worldMaterial;
    static int brokenCounter;
    int prewarmTarget;
    public double PrewarmMs { get; private set; }

    // ---- one shattering (or returning) pane ---------------------------------------------------------------------------------
    public enum Phase { Building, Ready, Flying, Merging, Done, Aborted, Disarming }

    public sealed class ShardLog
    {
        public int k, piece, wave;
        public float area, mass, speed;
        public bool backward;
        public double releaseT = -1, firstContactT = -1, firstFloorT = -1, firstFloorContactT = -1, firstLowT = -1, settledT = -1;
        public FrontRoomsGlassDebris.Surface firstSurface, lowSurface;
        public Vector3 v0, w0, finalRoot, restRoot;
        public float startHeight, maxSpeed;
        public bool frozenByCap, pushed, stuck, dropped;
        public int contacts;
    }

    public sealed class Run
    {
        public FrontRoomsGlassBreakable glass;
        public Transform root;
        public FrontRoomsMapWorld map;
        public long edge;
        public FrontRoomsGlassFracture f;
        public FrontRoomsGlassVfx.Group group;
        public FrontRoomsGlassFloorGlass floor;
        public Phase phase;
        public string profileTier;
        public int farSign;
        public float floorY;
        /// <summary>Rebuilt from a compact record on the chunk's return (no physics, no shard objects).</summary>
        public bool restore;
        /// <summary>The reduced tier (WebGL / handheld): scripted integrator, one skinned draw, one merged mesh.</summary>
        public bool reduced;
        // source: the stage-2 mesh (slab-local, stage-2 pose)
        internal readonly List<FrontRoomsGlassFractureMeshes.Vertex> srcV = new List<FrontRoomsGlassFractureMeshes.Vertex>(20000);
        internal readonly List<int> srcI = new List<int>(26000);
        internal readonly List<int> rangePiece = new List<int>(256);
        internal NativeArray<FrontRoomsGlassFractureMeshes.Vertex> nSrcV;
        internal NativeArray<int> nSrcI;
        internal void DisposeSource()
        {
            if (nSrcV.IsCreated) nSrcV.Dispose();
            if (nSrcI.IsCreated) nSrcI.Dispose();
        }
        internal readonly List<FrontRoomsGlassRTRange> rangeSide = new List<FrontRoomsGlassRTRange>(256), rangeFace = new List<FrontRoomsGlassRTRange>(256);
        // shards
        internal readonly List<int> shardK = new List<int>(256);
        internal readonly List<FrontRoomsGlassShard> shardObj = new List<FrontRoomsGlassShard>(256);
        internal readonly List<Mesh> shardMesh = new List<Mesh>(256), hullMesh = new List<Mesh>(256), toothHullMesh = new List<Mesh>(64);
        internal readonly List<int> shardV0 = new List<int>(256), shardVn = new List<int>(256), spanV0 = new List<int>(256), spanVn = new List<int>(256);
        internal readonly List<float> quiet = new List<float>(256);
        internal readonly List<bool> released = new List<bool>(256), frozen = new List<bool>(256), exiting = new List<bool>(256);
        internal readonly List<double> lastImpactT = new List<double>(256);
        public readonly List<ShardLog> logs = new List<ShardLog>(256);
        internal Renderer[] renderersByRange;
        // world proxies and tooth hulls in the debris scene
        internal readonly List<GameObject> proxies = new List<GameObject>();
        internal readonly List<MeshCollider> toothHulls = new List<MeshCollider>();
        internal readonly HashSet<Collider> glassColliders = new HashSet<Collider>();
        /// <summary>The dressed frame's parts (root frame, min/max pairs): proxies in the debris scene and the merge's frame check.</summary>
        internal readonly List<Vector3> frameBoxes = new List<Vector3>(32);
        internal Collider[] proxyQuery;
        internal MaterialPropertyBlock block, blockNoBow;
        // build cursor and the hull cooking job
        internal int buildStep, cursor;
        internal NativeArray<EntityId> bakeIds;
        internal JobHandle bakeHandle;
        internal bool baking;
        // flight
        public double t;
        internal double acc;
        internal bool justShattered, teethUnbowed;
        public Vector3 impactWorld, impulse;
        public int substeps;
        internal FrontRoomsGlassDebrisLite.Bodies lite;
        // stats
        public double buildCpuMs, buildWorstMs, mergeCpuMs, mergeWorstMs, swapMs, shatterMs, restoreCpuMs, restoreWorstMs;
        public long shatterAlloc;
        public readonly double[] buildStepMs = new double[12], mergeStepMs = new double[MergeStepCount], mergeStepWorst = new double[MergeStepCount];
        /// <summary>A return's worst time per step (ms): read the pattern, teeth, neighbourhood, start the merge.</summary>
        public readonly double[] restoreStepWorst = new double[4];
        public int buildFrames, mergeFrames, proxyColliders, proxyTris, buildStartFrame = -1, readyFrame = -1, shatterFrame = -1, settleFrame = -1, swapFrame = -1, mergedFrame = -1;
        public string forced = "";
        public readonly List<double> stepMs = new List<double>(512);
        public readonly List<int> stepAwake = new List<int>(512);
        public int teethCount, rebreakWouldFire, pushes, stuck, droppedFromOpening, droppedByEnv, recordBytes, crumbsMerged;
        public double settledAtT = -1;
        public bool settledByCap;
        public string restoreNote = "";
        // eng r1: the merge's own steps (the opening cleared, crumbs taken, record written) so a release mid-merge knows what is left
        internal bool clearPending, cleared, crumbsTaken, recordWritten;
        // fix r1 (art #5): on the reduced tier the smallest outer-wave bodies up to this area (cm2) fall back toward the breaker,
        // so the player's side gets its share with the WebGL profile's big plates (none did before); -1 = desktop
        internal double reducedBackCm2 = -1;
        // fix r1 (eng #11): the sliced merge's own state
        internal bool itemsBegun, envHashSet;
        internal uint envHash;
        internal readonly Dictionary<int, int> pieceToRange = new Dictionary<int, int>(256);
        internal List<FrontRoomsGlassDebrisPack.Cluster> clustersOut;
        // art r1 #2: shards laid flat at the settle (upright and unsupported / inside the frame), left leaning on the wall
        public int laidFlat, frameDrops, leaning, uprightLeft;
        public double freezeMs;
        public float finishedAtOnceM = -1f;
        public int wallBaseBreaker, wallBaseFar, crumbsOnSill;
        public string disarmWhy = "";
        // merge
        internal readonly List<FrontRoomsGlassDebrisJobs.MergeItem> items = new List<FrontRoomsGlassDebrisJobs.MergeItem>(256);
        internal readonly List<FrontRoomsGlassFractureMeshes.Vertex> crumbV = new List<FrontRoomsGlassFractureMeshes.Vertex>(4096);
        internal readonly List<int> crumbI = new List<int>(8192);
        internal readonly List<FrontRoomsGlassDebris.RestingCrumb> crumbs = new List<FrontRoomsGlassDebris.RestingCrumb>(512);
        internal int mergeStep;
        internal readonly MergeJob job = new MergeJob();
        // the record this run restores from
        internal FrontRoomsGlassDebrisPack.Header recHeader;
        internal readonly List<FrontRoomsGlassDebrisPack.Shard> recShards = new List<FrontRoomsGlassDebrisPack.Shard>(128);
        internal readonly List<FrontRoomsGlassDebrisPack.Cluster> recClusters = new List<FrontRoomsGlassDebrisPack.Cluster>(8);
        internal bool hasRecord;
        public int ShardCount => restore ? recShards.Count : shardK.Count;
        public int Released { get { var n = 0; foreach (var r in released) if (r) n++; return n; } }
        public int Frozen { get { var n = 0; foreach (var r in frozen) if (r) n++; return n; } }
        public IReadOnlyList<FrontRoomsGlassShard> Shards => shardObj;
    }

    public IReadOnlyList<Run> Runs => runs;
    public static Run RunOf(FrontRoomsGlassBreakable glass)
    {
        if (instance == null) return null;
        for (var i = instance.runs.Count - 1; i >= 0; i--) if (instance.runs[i].glass == glass) return instance.runs[i];
        return null;
    }

    /// <summary>The lab's "settled": this pane's shards are merged into floor glass (or it never broke into shards).</summary>
    public static bool IsSettled(FrontRoomsGlassBreakable glass)
    {
        var r = RunOf(glass);
        if (r == null) return true;
        return r.phase == Phase.Done || r.phase == Phase.Aborted || r.phase == Phase.Disarming;
    }

    public static int ArmedCount
    {
        get
        {
            if (instance == null) return 0;
            var n = 0;
            foreach (var r in instance.runs) if (!r.restore && (r.phase == Phase.Building || r.phase == Phase.Ready)) n++;
            return n;
        }
    }

    // ---- breakable hooks -----------------------------------------------------------------------------------------------------
    static void OnStageChanged(FrontRoomsGlassBreakable glass, int from, int to)
    {
        if (!Enabled || !Application.isPlaying || glass == null) return;
        var w = Instance;
        if (w == null) return;
        var f = glass.Fracture;
        var tier = f != null ? f.tier : "High";
        var budget = FrontRoomsGlassVfx.Budget.For(tier);
        if (to == 1 && f != null && !f.isDice)
        {
            var g = w.vfx.GroupFor(glass);
            w.vfx.EmitChips(g, f.chips1, 1);
            w.vfx.EmitPowder(g, f.impact, f.craterRadius, budget.s1Powder, 1, .45f, 1);
        }
        else if (to == 2 && f != null && !f.isDice)
        {
            var g = w.vfx.GroupFor(glass);
            w.vfx.EmitChips(g, f.chips2, 2);
            w.vfx.EmitCrack2(g, f, budget);
            // eng r1 #2: a live Crack2 goes through the same arming rule as a restored stage-2 pane (at most MaxArmed sets)
            w.ArmLive(glass);
        }
    }

    static void OnMeshesReady(FrontRoomsGlassBreakable glass)
    {
        if (!Enabled || !Application.isPlaying || glass == null) return;
        var w = Instance;
        if (w == null) return;
        // fill the shard pool before the first Crack2 of the session (a pane has at most the profile's maxBodies shards)
        var f = glass.Fracture;
        if (f != null && glass.Stage < 3) w.prewarmTarget = Math.Max(w.prewarmTarget, f.stats.shard + 8);
        // a pane restored at stage 2 (kept progress) is armed when the camera comes near; a shattered one gets its glass back
        // (a restored pane raises MeshesReady before it shows its stage: its record says where it is)
        if (glass.Stage < 3 && glass.Record.stage == 2 && w.RunOfHere(glass) == null && !pendingArm.Contains(glass)) pendingArm.Add(glass);
    }

    static void OnReleased(FrontRoomsGlassBreakable glass)
    {
        pendingArm.Remove(glass);
        if (instance == null || glass == null) return;
        for (var i = instance.runs.Count - 1; i >= 0; i--)
        {
            var r = instance.runs[i];
            if (r.glass != glass) continue;
            // the chunk goes 48+ m away: a shatter still in flight (or merging) there writes its record from the frozen poses now,
            // and its meshes go with the chunk; no merge is run for meshes that are about to be destroyed (eng r1 #1)
            if (r.phase == Phase.Flying || (r.phase == Phase.Merging && !r.restore)) instance.RecordAndDrop(r, "the chunk was released");
            if (r.phase != Phase.Done && r.phase != Phase.Aborted) instance.Abort(r, "the chunk was released", false);
            instance.runs.RemoveAt(i);
        }
        if (glass.Map != null && glass.Window != null) FrontRoomsGlassDebris.MarkReleased(glass.Map, glass.Window.edge);
    }

    static void OnPlayerClimbed(Vector3 center)
    {
        if (!FrontRoomsGlassDebris.SnapTeethOnClimb) return;
        FrontRoomsGlassFloorGlass best = null;
        var bestD = 1f;
        foreach (var fg in FrontRoomsGlassDebris.liveFloors)
        {
            if (fg == null) continue;
            var d = new Vector2(fg.transform.position.x - center.x, fg.transform.position.z - center.z).magnitude;
            if (d < bestD) { bestD = d; best = fg; }
        }
        best?.SnapBottomTeeth();
    }

    internal static void OnTeethSnapped(FrontRoomsGlassFloorGlass floor, List<Vector3> pointsRoot)
    {
        var glass = floor.GetComponent<FrontRoomsGlassBreakable>();
        if (glass != null && glass.Map != null && glass.Window != null) UpdateRecordTeeth(glass.Map, glass.Window.edge, floor.SnappedMask);
        var w = Instance;
        if (w == null || glass == null) return;
        if (floor.Merged) FrontRoomsGlassDebrisLite.HideBottomTeeth(floor);
        var g = w.vfx.GroupFor(glass);
        g.paneIntact = false;
        var tier = glass.Fracture != null ? glass.Fracture.tier : FrontRoomsGlassTypeProfile.ReducedTier ? "WebGL" : "High";
        w.vfx.EmitTeethCrumbs(g, pointsRoot, FrontRoomsGlassVfx.Budget.For(tier).teeth, 9);
    }

    static void UpdateRecordTeeth(FrontRoomsMapWorld map, long edge, ulong mask)
    {
        var e = FrontRoomsGlassDebris.Find(map, edge);
        if (e == null || e.data == null || e.data.Length < FrontRoomsGlassDebrisPack.HeaderBytes) return;
        // the mask sits at bytes 32-39 of the header
        for (var i = 0; i < 8; i++) e.data[32 + i] = (byte)(mask >> (8 * i));
    }

    // ---- the build (Crack2 -> Shatter) ---------------------------------------------------------------------------------------
    Run StartRun(FrontRoomsGlassBreakable glass)
    {
        var existing = RunOfHere(glass);
        if (existing != null) return existing;
        if (glass.Fracture == null || glass.Meshes == null || !glass.Ready) return null;
        var root = glass.transform;
        var r = new Run
        {
            glass = glass, root = root, map = glass.Map, edge = glass.Window != null ? glass.Window.edge : 0, f = glass.Fracture,
            phase = Phase.Building, profileTier = glass.Fracture.tier, farSign = glass.Fracture.farZSign >= 0 ? 1 : -1,
            floorY = root.position.y, buildStartFrame = Time.frameCount, reduced = FrontRoomsGlassTypeProfile.ReducedTier,
        };
        r.group = vfx.GroupFor(glass);
        r.floor = FrontRoomsGlassFloorGlass.On(root);
        runs.Add(r);
        Trace?.Invoke("debris: build started for " + root.name + " (" + r.f.pieces.Count + " pieces, tier " + r.profileTier + (r.reduced ? ", reduced" : "") + ", pool " + shardPool.Count + ")");
        return r;
    }

    Run RunOfHere(FrontRoomsGlassBreakable glass)
    {
        foreach (var r in runs) if (r.glass == glass && r.phase != Phase.Aborted && r.phase != Phase.Done && r.phase != Phase.Disarming) return r;
        return null;
    }

    double FramesLeftScale(Run r)
    {
        var w = r.glass != null ? r.glass.Window : null;
        if (w == null) return 1.0;
        var left = (FrontRoomsShotTimings.GlassBreak.HoldSeconds - w.hold * FrontRoomsShotTimings.GlassBreak.HoldSeconds) / Mathf.Max(Time.deltaTime, 1f / 240f);
        return left < 4f ? 4.0 : left < 8f ? 2.0 : 1.0;
    }

    /// <summary>Build slices until the budget is spent (each item is small): true when the run is ready to shatter. With
    /// <paramref name="force"/> the cooking job is waited for instead of yielding the frame.</summary>
    bool BuildSlice(Run r, double budgetMs, bool force = false)
    {
        var t0 = ClockMs();
        var yieldFrame = false;
        do
        {
            var s0 = ClockMs();
            var step = r.buildStep;
            switch (r.buildStep)
            {
                case 0: ReadSource(r); r.buildStep++; r.cursor = 0; yieldFrame = !force; break;
                case 1: Teeth(r, 0UL); r.buildStep++; r.cursor = 0; r.shardMesh.Clear(); yieldFrame = !force; break;
                case 2:
                    if (r.reduced)
                    {
                        // the reduced tier: no hulls, no proxies; its one skinned mesh is built now (hidden), not on the shatter frame
                        FrontRoomsGlassDebrisLite.PrepareLogs(r);
                        r.lite = FrontRoomsGlassDebrisLite.Build(r);
                        if (r.lite != null && r.lite.renderer != null) r.lite.renderer.enabled = false;
                        r.buildStep = 10;
                        yieldFrame = !force;
                        break;
                    }
                    if (ShardMeshes(r, 8)) { r.buildStep++; r.cursor = 0; r.hullMesh.Clear(); r.toothHullMesh.Clear(); }
                    break;
                case 3: if (HullMeshes(r, 12)) { r.buildStep++; r.cursor = 0; } break;
                case 4: ScheduleBake(r); r.buildStep++; break;
                case 5:
                    if (r.baking)
                    {
                        if (!force && !r.bakeHandle.IsCompleted) { yieldFrame = true; break; }
                        r.bakeHandle.Complete();
                        r.baking = false;
                        if (r.bakeIds.IsCreated) r.bakeIds.Dispose();
                    }
                    r.buildStep++; r.cursor = 0;
                    break;
                case 6: if (ShardObjects(r, 8)) { r.buildStep++; r.cursor = 0; } break;
                case 7: if (ToothHulls(r, 8)) { r.buildStep++; r.cursor = 0; } break;
                case 8: if (Proxies(r)) { r.buildStep++; r.cursor = 0; } break;
                case 9:
                    // shapes into the broad phase now (everything kinematic or static: nothing moves), not on the first flying step
                    UnityEngine.Physics.SyncTransforms();
                    physics.Simulate(Substep);
                    r.buildStep++;
                    break;
                default:
                    r.phase = Phase.Ready;
                    r.readyFrame = Time.frameCount;
                    break;
            }
            r.buildStepMs[Mathf.Min(step, r.buildStepMs.Length - 1)] += ClockMs() - s0;
        } while (r.phase == Phase.Building && !yieldFrame && ClockMs() - t0 < budgetMs);
        var ms = ClockMs() - t0;
        r.buildCpuMs += ms;
        r.buildWorstMs = Math.Max(r.buildWorstMs, ms);
        r.buildFrames++;
        if (r.phase == Phase.Ready)
            Trace?.Invoke("debris: ready in " + (r.readyFrame - r.buildStartFrame + 1) + " frames, " + r.buildCpuMs.ToString("0.00") + " ms CPU (worst frame " + r.buildWorstMs.ToString("0.00")
                + " ms; read " + r.buildStepMs[0].ToString("0.00") + ", teeth " + r.buildStepMs[1].ToString("0.00") + ", meshes " + r.buildStepMs[2].ToString("0.00") + ", hull meshes " + r.buildStepMs[3].ToString("0.00")
                + ", cook schedule " + r.buildStepMs[4].ToString("0.00") + ", cook wait " + r.buildStepMs[5].ToString("0.00") + ", objects " + r.buildStepMs[6].ToString("0.00") + ", tooth hulls " + r.buildStepMs[7].ToString("0.00")
                + ", proxies " + r.buildStepMs[8].ToString("0.00") + ", broad phase " + r.buildStepMs[9].ToString("0.00") + ") · " + r.shardK.Count + " shards, " + r.teethCount + " teeth, " + r.proxyColliders
                + " proxy colliders (" + r.proxyTris + " cropped tris)" + (r.reduced ? " · reduced tier" : "") + (layersOk ? "" : " · LAYERS UNAVAILABLE: no exit window"));
        return r.phase == Phase.Ready;
    }

    void ReadSource(Run r)
    {
        var m = r.glass.Meshes;
        r.srcV.Clear(); r.srcI.Clear(); r.rangePiece.Clear(); r.rangeSide.Clear(); r.rangeFace.Clear();
        if (m.Stage2 != null && m.PieceIds.Count > 0)
        {
            r.DisposeSource();
            using (var ro = Mesh.AcquireReadOnlyMeshData(m.Stage2))
            {
                var md = ro[0];
                var vv = md.GetVertexData<FrontRoomsGlassFractureMeshes.Vertex>(0);
                for (var i = 0; i < vv.Length; i++) r.srcV.Add(vv[i]);
                r.nSrcV = new NativeArray<FrontRoomsGlassFractureMeshes.Vertex>(vv.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                r.nSrcV.CopyFrom(vv);
                if (md.indexFormat == IndexFormat.UInt16)
                {
                    var ii = md.GetIndexData<ushort>();
                    r.nSrcI = new NativeArray<int>(ii.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                    for (var i = 0; i < ii.Length; i++) { r.srcI.Add(ii[i]); r.nSrcI[i] = ii[i]; }
                }
                else
                {
                    var ii = md.GetIndexData<int>();
                    for (var i = 0; i < ii.Length; i++) r.srcI.Add(ii[i]);
                    r.nSrcI = new NativeArray<int>(ii.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                    r.nSrcI.CopyFrom(ii);
                }
            }
            r.rangePiece.AddRange(m.PieceIds);
            r.rangeSide.AddRange(m.PieceSides);
            r.rangeFace.AddRange(m.PieceFaces);
        }
        else if (r.f.isDice) BuildDiceSource(r);
        r.shardK.Clear(); r.shardV0.Clear(); r.shardVn.Clear();
        r.spanV0.Clear(); r.spanVn.Clear();
        for (var k = 0; k < r.rangePiece.Count; k++)
        {
            VertexSpanScan(r, k, out var sv0, out var svn);
            r.spanV0.Add(sv0);
            r.spanVn.Add(svn);
        }
        for (var k = 0; k < r.rangePiece.Count; k++)
        {
            var kind = r.f.pieces[r.rangePiece[k]].kind;
            if (kind != GlassPieceKind.Shard && kind != GlassPieceKind.Clump) continue;
            VertexSpan(r, k, out var v0, out var vn);
            r.shardK.Add(k);
            r.shardV0.Add(v0);
            r.shardVn.Add(vn);
        }
        r.renderersByRange = new Renderer[r.rangePiece.Count];
    }

    static void VertexSpan(Run r, int k, out int v0, out int vn)
    {
        if (k < r.spanV0.Count) { v0 = r.spanV0[k]; vn = r.spanVn[k]; return; }
        VertexSpanScan(r, k, out v0, out vn);
    }

    static void VertexSpanScan(Run r, int k, out int v0, out int vn)
    {
        int lo = int.MaxValue, hi = -1;
        var a = r.rangeSide[k];
        for (var i = a.indexStart; i < a.indexStart + a.indexCount; i++) { var x = r.srcI[i]; if (x < lo) lo = x; if (x > hi) hi = x; }
        var b = r.rangeFace[k];
        for (var i = b.indexStart; i < b.indexStart + b.indexCount; i++) { var x = r.srcI[i]; if (x < lo) lo = x; if (x > hi) hi = x; }
        v0 = lo == int.MaxValue ? 0 : lo;
        vn = hi < 0 ? 0 : hi - lo + 1;
    }

    // Tempered glass (decision 1 alternative): its stage meshes are the intact pane, so the clumps are built here as flat
    // prisms at rest (no bevel), one range each.
    void BuildDiceSource(Run r)
    {
        var f = r.f;
        FrontRoomsGlassFractureMeshes.PaneKeys(Matrix4x4.TRS(r.root.TransformPoint(new Vector3(0f, (float)FrontRoomsGlassFracturePattern.SlabCentreY, 0f)), r.root.rotation, Vector3.one), out var ka, out var kb);
        var poly = new List<GlassV2>();
        var side = new List<int>();
        var face = new List<int>();
        for (var pi = 0; pi < f.pieces.Count; pi++)
        {
            if (f.pieces[pi].kind != GlassPieceKind.Clump) continue;
            f.GetPoly(pi, poly);
            side.Clear(); face.Clear();
            Prism(r.srcV, side, face, poly, f.slabT * .5, ka, kb, f.pieces[pi].band);
            var s0 = r.srcI.Count;
            r.srcI.AddRange(side);
            var f0 = r.srcI.Count;
            r.srcI.AddRange(face);
            r.rangePiece.Add(pi);
            r.rangeSide.Add(new FrontRoomsGlassRTRange { indexStart = s0, indexCount = side.Count });
            r.rangeFace.Add(new FrontRoomsGlassRTRange { indexStart = f0, indexCount = face.Count });
        }
    }

    static void Prism(List<FrontRoomsGlassFractureMeshes.Vertex> v, List<int> side, List<int> face, List<GlassV2> poly, double h, float ka, float kb, int band)
    {
        var n = poly.Count;
        for (var cap = 0; cap < 2; cap++)
        {
            var z = cap == 0 ? h : -h;
            var b = v.Count;
            for (var i = 0; i < n; i++) v.Add(Vtx(poly[i].x, poly[i].y, z, new Vector3(0, 0, cap == 0 ? 1 : -1), ka, kb, band, FrontRoomsGlassFractureMeshes.KindFace, 0, h));
            for (var i = 1; i + 1 < n; i++)
            {
                if (cap == 0) { face.Add(b); face.Add(b + i); face.Add(b + i + 1); }
                else { face.Add(b); face.Add(b + i + 1); face.Add(b + i); }
            }
        }
        for (var i = 0; i < n; i++)
        {
            var a = poly[i];
            var c = poly[(i + 1) % n];
            var o = new Vector3((float)(c.y - a.y), (float)-(c.x - a.x), 0).normalized;
            var b = v.Count;
            v.Add(Vtx(a.x, a.y, -h, o, ka, kb, band, FrontRoomsGlassFractureMeshes.KindFracture, 255, h));
            v.Add(Vtx(c.x, c.y, -h, o, ka, kb, band, FrontRoomsGlassFractureMeshes.KindFracture, 255, h));
            v.Add(Vtx(c.x, c.y, h, o, ka, kb, band, FrontRoomsGlassFractureMeshes.KindFracture, 255, h));
            v.Add(Vtx(a.x, a.y, h, o, ka, kb, band, FrontRoomsGlassFractureMeshes.KindFracture, 255, h));
            side.Add(b); side.Add(b + 1); side.Add(b + 2);
            side.Add(b); side.Add(b + 2); side.Add(b + 3);
        }
    }

    static FrontRoomsGlassFractureMeshes.Vertex Vtx(double x, double y, double z, Vector3 n, float ka, float kb, int band, byte kind, byte edge, double h) => new FrontRoomsGlassFractureMeshes.Vertex
    {
        position = new Vector3((float)x, (float)y, (float)z),
        normal = n,
        color = new Color32(edge, 128, (byte)Mathf.Clamp(Mathf.RoundToInt((float)((z + h) / (2 * h)) * 255f), 0, 255), 255),
        uv0 = new Vector2((float)x, (float)y),
        uv1 = new Vector4(ka, kb, band, kind),
    };

    readonly List<int> sideTmp = new List<int>(256), faceTmp = new List<int>(256);

    /// <summary>The tooth pieces into one static mesh on the window root (rest pose), minus the snapped ones.</summary>
    void Teeth(Run r, ulong snapped)
    {
        r.floor.BeginTeeth(snapped);
        r.teethCount = 0;
        var y0 = (float)FrontRoomsGlassFracturePattern.SlabCentreY;
        for (var k = 0; k < r.rangePiece.Count; k++)
        {
            var pi = r.rangePiece[k];
            var pc = r.f.pieces[pi];
            if (pc.kind != GlassPieceKind.Tooth) continue;
            VertexSpan(r, k, out var v0, out var vn);
            sideTmp.Clear(); faceTmp.Clear();
            for (var i = 0; i < r.rangeSide[k].indexCount; i++) sideTmp.Add(r.srcI[r.rangeSide[k].indexStart + i]);
            for (var i = 0; i < r.rangeFace[k].indexCount; i++) faceTmp.Add(r.srcI[r.rangeFace[k].indexStart + i]);
            r.floor.AddTooth(r.srcV, v0, vn, sideTmp, faceTmp, pc.side, new Vector3((float)pc.centroid.x, (float)pc.centroid.y + y0, 0f));
            r.teethCount++;
        }
        r.floor.UploadTeeth(false);
    }

    bool ShardMeshes(Run r, int perSlice)
    {
        var end = Math.Min(r.shardK.Count, r.cursor + perSlice);
        var n = end - r.cursor;
        if (n <= 0) return true;
        var meshes = new List<Mesh>(n);
        var arr = Mesh.AllocateWritableMeshData(n);
        var bounds = new Bounds[n];
        for (var j = 0; j < n; j++)
        {
            var s = r.cursor + j;
            var k = r.shardK[s];
            var v0 = r.shardV0[s];
            var vn = r.shardVn[s];
            var md = arr[j];
            md.SetVertexBufferParams(vn, FrontRoomsGlassFractureMeshes.Layout);
            var dv = md.GetVertexData<FrontRoomsGlassFractureMeshes.Vertex>();
            var mn = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var mx = -mn;
            for (var i = 0; i < vn; i++)
            {
                var vx = r.srcV[v0 + i];
                dv[i] = vx;
                mn = Vector3.Min(mn, vx.position);
                mx = Vector3.Max(mx, vx.position);
            }
            var ic = r.rangeSide[k].indexCount + r.rangeFace[k].indexCount;
            md.SetIndexBufferParams(ic, IndexFormat.UInt16);
            var di = md.GetIndexData<ushort>();
            var w = 0;
            for (var i = 0; i < r.rangeSide[k].indexCount; i++) di[w++] = (ushort)(r.srcI[r.rangeSide[k].indexStart + i] - v0);
            for (var i = 0; i < r.rangeFace[k].indexCount; i++) di[w++] = (ushort)(r.srcI[r.rangeFace[k].indexStart + i] - v0);
            bounds[j] = new Bounds((mn + mx) * .5f, mx - mn);
            md.subMeshCount = 1;
            md.SetSubMesh(0, new SubMeshDescriptor(0, ic) { bounds = bounds[j], vertexCount = vn }, MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices);
            meshes.Add(TakeMesh("GD3 shard"));
        }
        Mesh.ApplyAndDisposeWritableMeshData(arr, meshes, MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers);
        for (var j = 0; j < n; j++) { meshes[j].bounds = bounds[j]; r.shardMesh.Add(meshes[j]); }
        r.cursor = end;
        return r.cursor >= r.shardK.Count;
    }

    static Mesh TakeMesh(string name)
    {
        while (meshPool.Count > 0)
        {
            var m = meshPool.Pop();
            if (m != null) { m.Clear(); m.name = name; return m; }
        }
        return new Mesh { name = name };
    }

    static void GiveMesh(Mesh m)
    {
        if (m == null) return;
        if (meshPool.Count < 600) { m.Clear(); meshPool.Push(m); }
        else Destroy(m);
    }

    readonly List<GlassV2> poly = new List<GlassV2>(32), inset = new List<GlassV2>(32);
    readonly List<Vector3> hullV = new List<Vector3>(64);
    readonly List<int> hullT = new List<int>(128);

    /// <summary>The convex hull of piece pi: its un-bevelled inset prism at the stage-2 pose (StagePoint), slab-local.</summary>
    Mesh HullOf(FrontRoomsGlassFracture f, int pi, bool posed)
    {
        f.GetPoly(pi, poly);
        FrontRoomsGlassFractureMeshes.InsetConvex(poly, FrontRoomsGlassFractureMeshes.PieceInset, inset);
        var q = inset.Count >= 3 ? inset : poly;
        hullV.Clear(); hullT.Clear();
        var h = f.slabT * .5;
        var n = q.Count;
        for (var cap = 0; cap < 2; cap++)
            for (var i = 0; i < n; i++)
            {
                var rest = new FrontRoomsGlassFractureMeshes.Vector3d(q[i].x, q[i].y, cap == 0 ? h : -h);
                var p = posed ? FrontRoomsGlassFractureMeshes.StagePoint(f, pi, rest) : rest;
                hullV.Add(new Vector3((float)p.x, (float)p.y, (float)p.z));
            }
        for (var i = 1; i + 1 < n; i++) { hullT.Add(0); hullT.Add(i); hullT.Add(i + 1); hullT.Add(n); hullT.Add(n + i + 1); hullT.Add(n + i); }
        for (var i = 0; i < n; i++)
        {
            var j = (i + 1) % n;
            hullT.Add(i); hullT.Add(n + i); hullT.Add(n + j);
            hullT.Add(i); hullT.Add(n + j); hullT.Add(j);
        }
        var m = TakeMesh("GD3 shard hull");
        m.SetVertices(hullV);
        m.SetTriangles(hullT, 0, true);
        return m;
    }

    // F1: the hull meshes are made here (small); the cooking runs on worker threads (ScheduleBake), never on the main thread.
    bool HullMeshes(Run r, int perSlice)
    {
        var total = r.shardK.Count + r.rangePiece.Count;
        var end = Math.Min(total, r.cursor + perSlice);
        for (; r.cursor < end; r.cursor++)
        {
            if (r.cursor < r.shardK.Count)
            {
                var pi = r.rangePiece[r.shardK[r.cursor]];
                r.hullMesh.Add(HullOf(r.f, pi, r.f.pieces[pi].kind == GlassPieceKind.Shard));
            }
            else
            {
                var k = r.cursor - r.shardK.Count;
                var pi = r.rangePiece[k];
                if (r.f.pieces[pi].kind == GlassPieceKind.Tooth) r.toothHullMesh.Add(HullOf(r.f, pi, false));
            }
        }
        return r.cursor >= total;
    }

    static MeshColliderCookingOptions CookOptions => MeshColliderCookingOptions.CookForFasterSimulation | MeshColliderCookingOptions.EnableMeshCleaning
        | MeshColliderCookingOptions.WeldColocatedVertices | MeshColliderCookingOptions.UseFastMidphase;

    void ScheduleBake(Run r)
    {
        var n = r.hullMesh.Count + r.toothHullMesh.Count;
        if (n == 0) return;
        r.bakeIds = new NativeArray<EntityId>(n, Allocator.Persistent);
        for (var i = 0; i < r.hullMesh.Count; i++) r.bakeIds[i] = r.hullMesh[i].GetEntityId();
        for (var i = 0; i < r.toothHullMesh.Count; i++) r.bakeIds[r.hullMesh.Count + i] = r.toothHullMesh[i].GetEntityId();
        r.bakeHandle = new FrontRoomsGlassDebrisJobs.HullBakeJob { ids = r.bakeIds, options = CookOptions }.Schedule(n, 4);
        JobHandle.ScheduleBatchedJobs();
        r.baking = true;
    }

    static FrontRoomsGlassShard NewShard()
    {
        // the body (hull + rigidbody, a debris layer) and its renderer on a child (layer 0, so every camera draws it)
        var go = new GameObject("GD3 shard");
        Adopt(go);
        go.layer = L(LayerHeld);
        var sh = go.AddComponent<FrontRoomsGlassShard>();
        var view = new GameObject("GD3 shard view");
        view.transform.SetParent(go.transform, false);
        sh.Filter = view.AddComponent<MeshFilter>();
        sh.Renderer = view.AddComponent<MeshRenderer>();
        sh.Renderer.sharedMaterial = FrontRoomsGlassBreakable.FractureMaterial;
        sh.Renderer.shadowCastingMode = ShadowCastingMode.Off;
        sh.Renderer.receiveShadows = false;
        sh.Renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
        sh.Renderer.enabled = false;
        view.AddComponent<FrontRoomsMetalGlassTarget>();
        sh.Hull = go.AddComponent<MeshCollider>();
        sh.Hull.cookingOptions = CookOptions;
        sh.Hull.convex = true;
        sh.Hull.contactOffset = .002f;
        sh.Body = go.AddComponent<Rigidbody>();
        sh.Body.isKinematic = true;
        sh.Body.interpolation = RigidbodyInterpolation.None;
        sh.Body.maxAngularVelocity = 40f;
        sh.Body.solverIterations = 4;
        sh.Body.solverVelocityIterations = 1;
        go.SetActive(false);
        return sh;
    }

    FrontRoomsGlassShard TakeShard()
    {
        while (shardPool.Count > 0)
        {
            var s = shardPool.Pop();
            if (s != null) return s;
        }
        return NewShard();
    }

    void Prewarm()
    {
        if (shardPool.Count >= prewarmTarget) return;
        var t0 = ClockMs();
        do shardPool.Push(NewShard()); while (shardPool.Count < prewarmTarget && ClockMs() - t0 < PrewarmBudgetMs);
        PrewarmMs += ClockMs() - t0;
    }

    bool ShardObjects(Run r, int perSlice)
    {
        EnsureMaterials();
        if (r.cursor == 0)
        {
            r.shardObj.Clear(); r.quiet.Clear(); r.released.Clear(); r.frozen.Clear(); r.exiting.Clear(); r.lastImpactT.Clear(); r.logs.Clear();
        }
        var slabPose = SlabPose(r.root);
        var end = Math.Min(r.shardK.Count, r.cursor + perSlice);
        for (; r.cursor < end; r.cursor++)
        {
            var s = r.cursor;
            var k = r.shardK[s];
            var pi = r.rangePiece[k];
            var pc = r.f.pieces[pi];
            var sh = TakeShard();
            sh.Slot = s;
            sh.Run = r;
            sh.gameObject.layer = L(LayerHeld);
            sh.transform.SetPositionAndRotation(slabPose.position, slabPose.rotation);
            sh.Filter.sharedMesh = r.shardMesh[s];
            sh.Hull.sharedMesh = r.hullMesh[s];
            sh.Hull.sharedMaterial = shardMaterial;
            sh.Hull.excludeLayers = 0;
            sh.Hull.enabled = true;
            sh.Renderer.enabled = false;
            var b = sh.Body;
            b.isKinematic = true;
            b.collisionDetectionMode = CollisionDetectionMode.Discrete;
            var area = (float)pc.area;
            b.mass = Mathf.Max(.002f, area * (float)r.f.slabT * 2500f);
            b.linearDamping = .02f;
            b.angularDamping = .15f;
            sh.gameObject.SetActive(true);
            b.ResetCenterOfMass();
            b.ResetInertiaTensor();
            r.glassColliders.Add(sh.Hull);
            r.shardObj.Add(sh);
            r.renderersByRange[k] = sh.Renderer;
            r.quiet.Add(0f); r.released.Add(false); r.frozen.Add(false); r.exiting.Add(false); r.lastImpactT.Add(-1);
            var c = pc.centroid;
            r.logs.Add(new ShardLog { k = k, piece = pi, wave = pc.releaseWave, area = area, mass = b.mass, restRoot = new Vector3((float)c.x, (float)c.y + (float)FrontRoomsGlassFracturePattern.SlabCentreY, 0f) });
        }
        return r.cursor >= r.shardK.Count;
    }

    static Pose SlabPose(Transform root) => new Pose(root.TransformPoint(new Vector3(0f, (float)FrontRoomsGlassFracturePattern.SlabCentreY, 0f)), root.rotation);

    bool ToothHulls(Run r, int perSlice)
    {
        var slabPose = SlabPose(r.root);
        var done = 0;
        for (; r.cursor < r.toothHullMesh.Count && done < perSlice; r.cursor++)
        {
            done++;
            MeshCollider mc = null;
            while (hullPool.Count > 0 && mc == null) mc = hullPool.Pop();
            if (mc == null)
            {
                var go = new GameObject("GD3 tooth hull");
                Adopt(go);
                mc = go.AddComponent<MeshCollider>();
                mc.cookingOptions = CookOptions;
                mc.convex = true;
                mc.contactOffset = .002f;
            }
            mc.gameObject.layer = L(LayerTooth);
            mc.transform.SetPositionAndRotation(slabPose.position, slabPose.rotation);
            mc.sharedMesh = r.toothHullMesh[r.cursor];
            mc.sharedMaterial = shardMaterial;
            mc.gameObject.SetActive(true);
            r.toothHulls.Add(mc);
            r.glassColliders.Add(mc);
        }
        return r.cursor >= r.toothHullMesh.Count;
    }

    // The world round the window, copied into the debris scene: the chunk collision cropped to the proxy box, the other
    // colliders of the map (furniture, doors, other panes) as they are, and a thick safety floor (a falling 6 mm shard can
    // never tunnel through a one-sided floor triangle into the void). Nothing of the player or the Relay (not under the map).
    // One collider per call.
    bool Proxies(Run r)
    {
        EnsureMaterials();
        var root = r.root;
        if (r.proxyQuery == null)
        {
            var centre = root.TransformPoint(new Vector3(0f, ProxyHalf.y, 0f));
            r.proxyQuery = UnityEngine.Physics.OverlapBox(centre, ProxyHalf, root.rotation, ~0, QueryTriggerInteraction.Ignore);
            r.cursor = 0;
            // the safety floor: 8 x 8 m, 0.5 m thick, its top on the floor
            var floor = new GameObject("GD3 proxy safety floor");
            Adopt(floor);
            floor.layer = L(LayerWorld);
            floor.transform.SetPositionAndRotation(root.TransformPoint(new Vector3(0f, -.25f, 0f)), root.rotation);
            var fb = floor.AddComponent<BoxCollider>();
            fb.size = new Vector3(8f, .5f, 8f);
            fb.sharedMaterial = worldMaterial;
            r.proxies.Add(floor);
            r.proxyColliders++;
            // art r1 #2: the dressed frame has no collider in the game (render only), so its stool, nosings, aprons and casings are
            // boxes here, in the debris scene only (a shard used to stand inside the stool's nose)
            if (FrameProxies)
            {
                FrameBoxes(root, r.frameBoxes);
                if (r.frameBoxes.Count > 0)
                {
                    var fr = new GameObject("GD3 proxy window frame");
                    Adopt(fr);
                    fr.layer = L(LayerWorld);
                    fr.transform.SetPositionAndRotation(root.position, root.rotation);
                    for (var b = 0; b + 1 < r.frameBoxes.Count; b += 2)
                    {
                        var bc = fr.AddComponent<BoxCollider>();
                        bc.center = (r.frameBoxes[b] + r.frameBoxes[b + 1]) * .5f;
                        bc.size = r.frameBoxes[b + 1] - r.frameBoxes[b];
                        bc.sharedMaterial = worldMaterial;
                        r.proxyColliders++;
                    }
                    r.proxies.Add(fr);
                }
            }
            return r.proxyQuery.Length == 0;
        }
        if (r.cursor >= r.proxyQuery.Length) return true;
        var c = r.proxyQuery[r.cursor++];
        var pane = r.glass.Window != null && r.glass.Window.pane != null ? r.glass.Window.pane.GetComponent<Collider>() : null;
        var mapT = r.map != null ? r.map.transform : null;
        if (c == null || c == pane || !c.enabled || c.isTrigger) return r.cursor >= r.proxyQuery.Length;
        if (mapT != null && !c.transform.IsChildOf(mapT)) return r.cursor >= r.proxyQuery.Length;
        if (c.attachedRigidbody != null && !c.attachedRigidbody.isKinematic) return r.cursor >= r.proxyQuery.Length;
        if (c is CharacterController) return r.cursor >= r.proxyQuery.Length;
        var go = new GameObject("GD3 proxy " + c.name);
        Adopt(go);
        go.layer = L(LayerWorld);
        var made = false;
        switch (c)
        {
            case BoxCollider bc:
            {
                go.transform.SetPositionAndRotation(bc.transform.position, bc.transform.rotation);
                go.transform.localScale = bc.transform.lossyScale;
                var nb = go.AddComponent<BoxCollider>();
                nb.center = bc.center; nb.size = bc.size; nb.sharedMaterial = worldMaterial;
                made = true;
                break;
            }
            case SphereCollider sc:
            {
                go.transform.SetPositionAndRotation(sc.transform.position, sc.transform.rotation);
                go.transform.localScale = sc.transform.lossyScale;
                var ns = go.AddComponent<SphereCollider>();
                ns.center = sc.center; ns.radius = sc.radius; ns.sharedMaterial = worldMaterial;
                made = true;
                break;
            }
            case CapsuleCollider cc:
            {
                go.transform.SetPositionAndRotation(cc.transform.position, cc.transform.rotation);
                go.transform.localScale = cc.transform.lossyScale;
                var nc = go.AddComponent<CapsuleCollider>();
                nc.center = cc.center; nc.radius = cc.radius; nc.height = cc.height; nc.direction = cc.direction; nc.sharedMaterial = worldMaterial;
                made = true;
                break;
            }
            case MeshCollider mc when mc.sharedMesh != null:
            {
                if (mc.convex || !mc.sharedMesh.isReadable)
                {
                    go.transform.SetPositionAndRotation(mc.transform.position, mc.transform.rotation);
                    go.transform.localScale = mc.transform.lossyScale;
                    var nm = go.AddComponent<MeshCollider>();
                    nm.convex = mc.convex; nm.sharedMesh = mc.sharedMesh; nm.sharedMaterial = worldMaterial;
                    made = true;
                }
                else
                {
                    var crop = Crop(mc, root, out var tris);
                    if (crop != null)
                    {
                        go.transform.SetPositionAndRotation(root.position, Quaternion.identity);
                        var nm = go.AddComponent<MeshCollider>();
                        nm.sharedMesh = crop; nm.sharedMaterial = worldMaterial;
                        r.proxyTris += tris;
                        made = true;
                    }
                }
                break;
            }
        }
        if (!made) { Destroy(go); return r.cursor >= r.proxyQuery.Length; }
        r.proxies.Add(go);
        r.proxyColliders++;
        return r.cursor >= r.proxyQuery.Length;
    }

    static readonly List<Vector3> cropV = new List<Vector3>(4096);
    static readonly List<int> cropI = new List<int>(8192);
    static readonly Dictionary<int, int> cropMap = new Dictionary<int, int>(4096);

    /// <summary>The triangles of a (static, readable) mesh collider that meet the proxy box, relative to the window root's position.</summary>
    static Mesh Crop(MeshCollider mc, Transform root, out int tris)
    {
        tris = 0;
        var mesh = mc.sharedMesh;
        var l2r = root.worldToLocalMatrix * mc.transform.localToWorldMatrix;   // into the root frame (the box is axis-aligned there)
        var l2w = mc.transform.localToWorldMatrix;
        var min = new Vector3(-ProxyHalf.x, 0f, -ProxyHalf.z);
        var max = new Vector3(ProxyHalf.x, 2f * ProxyHalf.y, ProxyHalf.z);
        var origin = root.position;
        cropV.Clear(); cropI.Clear(); cropMap.Clear();
        using (var ro = Mesh.AcquireReadOnlyMeshData(mesh))
        {
            var md = ro[0];
            var vv = new NativeArray<Vector3>(md.vertexCount, Allocator.Temp);
            md.GetVertices(vv);
            for (var sm = 0; sm < md.subMeshCount; sm++)
            {
                var desc = md.GetSubMesh(sm);
                if (desc.topology != MeshTopology.Triangles) continue;
                var ii = new NativeArray<int>(desc.indexCount, Allocator.Temp);
                md.GetIndices(ii, sm);
                for (var t = 0; t + 2 < ii.Length; t += 3)
                {
                    var a = l2r.MultiplyPoint3x4(vv[ii[t]]);
                    var b = l2r.MultiplyPoint3x4(vv[ii[t + 1]]);
                    var c = l2r.MultiplyPoint3x4(vv[ii[t + 2]]);
                    var tmin = Vector3.Min(a, Vector3.Min(b, c));
                    var tmax = Vector3.Max(a, Vector3.Max(b, c));
                    if (tmax.x < min.x || tmin.x > max.x || tmax.y < min.y || tmin.y > max.y || tmax.z < min.z || tmin.z > max.z) continue;
                    for (var q = 0; q < 3; q++)
                    {
                        var src = ii[t + q];
                        if (!cropMap.TryGetValue(src, out var dst)) { dst = cropV.Count; cropMap[src] = dst; cropV.Add(l2w.MultiplyPoint3x4(vv[src]) - origin); }
                        cropI.Add(dst);
                    }
                    tris++;
                }
                ii.Dispose();
            }
            vv.Dispose();
        }
        if (tris == 0) return null;
        var m = TakeMesh("GD3 proxy crop");
        m.indexFormat = cropV.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        m.SetVertices(cropV);
        m.SetTriangles(cropI, 0, true);
        return m;
    }

    static void EnsureMaterials()
    {
        // glass on glass slides (0.25 / 0.2 after averaging with itself); on carpet and plaster it averages to about 0.55
        if (shardMaterial == null)
            shardMaterial = new PhysicsMaterial("GD3 glass shard") { dynamicFriction = .2f, staticFriction = .25f, bounciness = .08f, frictionCombine = PhysicsMaterialCombine.Average, bounceCombine = PhysicsMaterialCombine.Average };
        if (worldMaterial == null)
            worldMaterial = new PhysicsMaterial("GD3 world proxy (carpet, plaster)") { dynamicFriction = .8f, staticFriction = .9f, bounciness = .02f, frictionCombine = PhysicsMaterialCombine.Average, bounceCombine = PhysicsMaterialCombine.Average };
    }

    // ---- the shatter -----------------------------------------------------------------------------------------------------------
    bool OnShatter(FrontRoomsGlassBreakable glass, Vector3 impactWorld, Vector3 impulse)
    {
        var a0 = GC.GetAllocatedBytesForCurrentThread();
        var t0 = ClockMs();
        pendingArm.Remove(glass);
        var r = RunOfHere(glass) ?? StartRun(glass);
        if (r == null) { Trace?.Invoke("debris: no pattern for " + glass.name + " at the shatter: the glass is gone"); return false; }
        // one window flies at a time (one physics scene): another still flying is frozen where it is on this frame and merges
        // in slices from the next frame on; one already merging just goes on (eng r1 #1: no whole merge inside one frame)
        foreach (var other in runs)
            if (other != r && !other.restore && other.phase == Phase.Flying) FreezeForMerge(other, "another window shattered");
        if (r.phase == Phase.Building)
        {
            var f0 = ClockMs();
            while (r.phase == Phase.Building) BuildSlice(r, 1e9, true);
            r.forced = "finished at the shatter (" + (ClockMs() - f0).ToString("0.00") + " ms)";
            Trace?.Invoke("debris: the shatter came before the build slices finished: " + r.forced);
        }
        r.phase = Phase.Flying;
        r.shatterFrame = Time.frameCount;
        r.impactWorld = impactWorld;
        r.impulse = impulse;
        r.justShattered = true;
        r.t = 0;
        r.acc = 0;
        r.substeps = 0;
        // the pane's look carries over: the stage-2 renderer's block (impact, crush rim, wipe, the bow held through the object stop)
        r.block ??= new MaterialPropertyBlock();
        r.blockNoBow ??= new MaterialPropertyBlock();
        var src = glass.Stage2Renderer != null ? glass.Stage2Renderer : glass.SlabRenderer;
        if (src != null) { src.GetPropertyBlock(r.block); src.GetPropertyBlock(r.blockNoBow); }
        r.blockNoBow.SetFloat(BowAmpId, 0f);
        // F4: the crushed bruise belongs to the crater; once the pane is gone no piece keeps the S2 rim on its faces, nor the
        // sleeve wipe (art r1: it travelled onto the teeth and the floor glass)
        r.blockNoBow.SetFloat(CrushId, 0f);
        NoWipe(r.blockNoBow);
        if (r.reduced)
        {
            if (r.logs.Count == 0) FrontRoomsGlassDebrisLite.PrepareLogs(r);
            r.reducedBackCm2 = ReducedBackArea(r);
            for (var s = 0; s < r.logs.Count; s++) PlanRelease(r, s);
            if (r.lite == null) r.lite = FrontRoomsGlassDebrisLite.Build(r);
            if (r.lite != null && r.lite.renderer != null)
            {
                r.lite.renderer.enabled = true;
                r.lite.renderer.SetPropertyBlock(r.block);
                foreach (var k in r.shardK) r.renderersByRange[k] = r.lite.renderer;
            }
        }
        else
        {
            for (var s = 0; s < r.shardObj.Count; s++)
            {
                var sh = r.shardObj[s];
                sh.Renderer.SetPropertyBlock(r.block);
                sh.Renderer.enabled = true;
                PlanRelease(r, s);
            }
        }
        r.floor.ApplyBlock(r.block);
        r.floor.ShowTeeth(true);
        // particles: the pane is gone; a powder puff at the crater; the backward glitter; crumbs off the tooth edges
        var budget = FrontRoomsGlassVfx.Budget.For(r.profileTier);
        r.group.paneIntact = false;
        vfx.EmitPowder(r.group, r.f.impact, r.f.craterRadius, budget.puff, 3, .7f, 3);
        vfx.EmitBackwardGlitter(r.group, budget.backward);
        teethPts.Clear();
        r.floor.ToothCentres('\0', teethPts);
        vfx.EmitTeethCrumbs(r.group, teethPts, budget.teeth, 8);
        if (r.f.isDice) EmitDiceGranules(r, budget);
        try { PiecesShown?.Invoke(glass, r.renderersByRange); } catch (Exception e) { Debug.LogException(e); }
        r.shatterMs = ClockMs() - t0;
        r.shatterAlloc = GC.GetAllocatedBytesForCurrentThread() - a0;
        Trace?.Invoke("debris: shatter · " + (r.reduced ? (r.lite != null ? r.lite.count : 0) + " bodies (reduced, 1 skinned draw)" : r.shardObj.Count + " shards") + " on at the stage-2 pose, " + r.teethCount
            + " teeth · release waves from " + FrontRoomsShotTimings.GlassBreak.ObjectStop[2] * 1000f + " ms · handler " + r.shatterMs.ToString("0.00") + " ms, " + r.shatterAlloc + " B alloc");
        return true;
    }

    static readonly List<Vector3> teethPts = new List<Vector3>(64);
    static readonly int BowAmpId = Shader.PropertyToID("_BowAmp"), CrushId = Shader.PropertyToID("_Crush"), ImpactUVId = Shader.PropertyToID("_ImpactUV");

    static void NoWipe(MaterialPropertyBlock b)
    {
        var v = b.GetVector(ImpactUVId);
        v.z = 0f;
        b.SetVector(ImpactUVId, v);
    }

    void EmitDiceGranules(Run r, FrontRoomsGlassVfx.Budget b)
    {
        // tempered: most of the pane is granules (the clumps are the bodies); the release budget goes to granules here
        var pts = new List<Vector3>();
        var y0 = (float)FrontRoomsGlassFracturePattern.SlabCentreY;
        foreach (var pc in r.f.pieces) pts.Add(new Vector3((float)pc.centroid.x, (float)pc.centroid.y + y0, 0f));
        vfx.EmitTeethCrumbs(r.group, pts, b.release, 7);
    }

    internal static float Rk(uint seed, int salt, int a, int b) => (float)FrontRoomsGlassFractureMeshes.Rand01(seed, salt, a, b);

    /// <summary>Reduced tier: the area (cm2) under which an outer-wave body falls back toward the breaker: the smallest
    /// ReducedBackShare of them, at least ReducedBackMin (keyed by the pattern, so a restore agrees).</summary>
    public static float ReducedBackShare = .18f;
    public static int ReducedBackMin = 3;
    static readonly List<double> backAreas = new List<double>(64);

    static double ReducedBackArea(Run r)
    {
        backAreas.Clear();
        for (var s = 0; s < r.logs.Count; s++)
        {
            var pc = r.f.pieces[r.rangePiece[r.shardK[s]]];
            if (pc.kind == GlassPieceKind.Clump || pc.releaseWave < 1) continue;
            backAreas.Add(pc.area * 1e4);
        }
        if (backAreas.Count == 0) return -1.0;
        backAreas.Sort();
        var k = Mathf.Clamp(Mathf.Max(ReducedBackMin, Mathf.CeilToInt(ReducedBackShare * backAreas.Count)), 1, backAreas.Count);
        return backAreas[k - 1];
    }

    // The release plan of shard s (root-local velocities; plan sec 2.3 S3 and the generator's waves).
    internal static void PlanRelease(Run r, int s)
    {
        var f = r.f;
        var pi = r.rangePiece[r.shardK[s]];
        var pc = f.pieces[pi];
        var log = r.logs[s];
        var seed = f.seed;
        var stop = FrontRoomsShotTimings.GlassBreak.ObjectStop[2];
        var wave = pc.kind == GlassPieceKind.Clump ? 0 : Mathf.Clamp(pc.releaseWave, 0, 2);
        var ms = pc.hasRelease ? pc.releaseMs : 0.0;
        if (pc.kind == GlassPieceKind.Clump) ms = 0.0;
        log.releaseT = stop + ms / 1000.0;
        log.wave = wave;
        // art r1 (V10): backward fragments are the small ones ([RCMP91]); a big plate of the outer wave very rarely falls back
        var cm2 = pc.area * 1e4;
        var backShare = Backward[wave] * (cm2 > 80.0 ? .1f : cm2 > 25.0 ? .5f : 1.4f);
        var back = Rk(seed, 151, pc.id, 0) < Mathf.Min(.7f, backShare);
        if (r.reduced && r.reducedBackCm2 > 0.0 && wave >= 1 && pc.kind != GlassPieceKind.Clump && cm2 <= r.reducedBackCm2) back = true;
        log.backward = back;
        double cx = pc.centroid.x - f.impact.x, cy = pc.centroid.y - f.impact.y;
        var rr = Math.Sqrt(cx * cx + cy * cy);
        var radial = rr > 1e-6 ? new Vector2((float)(cx / rr), (float)(cy / rr)) : Vector2.zero;
        Vector3 v;
        if (!back)
        {
            var speed = (float)(pc.hasRelease ? pc.releaseSpeed : 2.0);
            if (pc.kind == GlassPieceKind.Clump) speed = Mathf.Lerp(1.5f, 3f, Rk(seed, 152, pc.id, 0));
            speed = Mathf.Max(speed, Mathf.Lerp(TipOutMin, TipOutMax, Rk(seed, 152, pc.id, 1)));
            var spread = RadialSpread * (.6f + .8f * Rk(seed, 153, pc.id, 0));
            // a little of the strike's in-plane direction for the inner shards (the elbow's sweep)
            var strike = r.root.InverseTransformDirection(r.impulse);
            var sweep = new Vector2(strike.x, strike.y) * (wave == 0 ? .05f : 0f);
            v = new Vector3(radial.x * spread + sweep.x, radial.y * spread + sweep.y, r.farSign).normalized * speed;
        }
        else
        {
            var speed = Mathf.Lerp(.15f, .5f, Rk(seed, 154, pc.id, 0));
            v = new Vector3(radial.x * .15f, radial.y * .15f + .05f, -r.farSign).normalized * speed;
        }
        // tumble: a keyed axis, faster for faster shards; frame-adjacent shards tip about their stop edge
        var axis = new Vector3(Rk(seed, 155, pc.id, 0) - .5f, Rk(seed, 155, pc.id, 1) - .5f, (Rk(seed, 155, pc.id, 2) - .5f) * .4f).normalized;
        var w = axis * Mathf.Lerp(1.5f, 7f, Rk(seed, 156, pc.id, 0)) * Mathf.Clamp(v.magnitude / 1.5f, .25f, 1.4f);
        var c = pc.centroid;
        var fr = f.frame;
        double dL = c.x - fr.stopX0, dR = fr.stopX1 - c.x, dB = c.y - fr.stopY0, dT = fr.stopY1 - c.y;
        var dmin = Math.Min(Math.Min(dL, dR), Math.Min(dB, dT));
        if (dmin < .15)
        {
            var e = dmin == dL ? new Vector3(-1, 0, 0) : dmin == dR ? new Vector3(1, 0, 0) : dmin == dB ? new Vector3(0, -1, 0) : new Vector3(0, 1, 0);
            var n = new Vector3(0, 0, Mathf.Sign(v.z));
            var lever = Mathf.Max(.06f, (float)dmin);
            w += Vector3.Cross(n, e) * (Mathf.Abs(v.z) / lever) * .5f;
        }
        log.v0 = v;
        log.w0 = w;
        log.speed = v.magnitude;
        log.startHeight = log.restRoot.y;
    }

    void Release(Run r, int s)
    {
        var sh = r.shardObj[s];
        var log = r.logs[s];
        r.released[s] = true;
        // it leaves the plane it was cut from: no contact with the teeth, the held shards or the other leaving shards for ExitSeconds
        sh.gameObject.layer = L(LayerFree);
        if (layersOk)
        {
            sh.Hull.excludeLayers = (1 << LayerHeld) | (1 << LayerTooth) | (1 << LayerFree);
            r.exiting[s] = true;
        }
        var b = sh.Body;
        b.isKinematic = false;
        b.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        b.linearVelocity = r.root.TransformDirection(log.v0);
        b.angularVelocity = r.root.TransformDirection(log.w0);
        sh.Renderer.SetPropertyBlock(r.blockNoBow);   // a free piece no longer bends with the pane
        EmitShedFor(r, s);
    }

    // crumbs a shard sheds as it lets go, from a point of its outline
    internal void EmitShedFor(Run r, int s)
    {
        var log = r.logs[s];
        var budget = FrontRoomsGlassVfx.Budget.For(r.profileTier);
        var nShards = r.logs.Count;
        var n = nShards > 0 ? budget.release / nShards : 0;
        var extra = budget.release - n * nShards;
        var count = n + (s < extra ? 1 : 0);
        if (count <= 0) return;
        var pc = r.f.pieces[log.piece];
        var vi = (int)(Rk(r.f.seed, 157, pc.id, 0) * pc.vn) % Math.Max(1, pc.vn);
        var pv = r.f.verts[pc.v0 + vi];
        var pLocal = new Vector3((float)pv.x, (float)pv.y + (float)FrontRoomsGlassFracturePattern.SlabCentreY, r.farSign * .004f);
        vfx.EmitShed(r.group, pLocal, log.v0, count, pc.id);
    }

    // ---- per frame ------------------------------------------------------------------------------------------------------------
    /// <summary>Lab: this frame's debris-world time by part (ms): 0 builds, 1 flight, 2 live merges, 3 disarms, 4 returns (restores),
    /// 5 arming, 6 pool prewarm, 7 particles, 8 the live-cap check, 9 aborts of windows whose chunk dropped.</summary>
    public static readonly double[] FrameParts = new double[10];

    void LateUpdate()
    {
        var f0 = ClockMs();
        var dt = Time.deltaTime;
        restoreOrder.Clear();
        Array.Clear(FrameParts, 0, FrameParts.Length);
        for (var i = runs.Count - 1; i >= 0; i--)
        {
            var r = runs[i];
            var p0 = ClockMs();
            if (r.glass == null || r.root == null)
            {
                if (r.phase != Phase.Done && r.phase != Phase.Aborted) Abort(r, "the window went (chunk dropped)", false);
                runs.RemoveAt(i);
                FrameParts[9] += ClockMs() - p0;
                continue;
            }
            switch (r.phase)
            {
                case Phase.Building:
                    if (r.restore) restoreOrder.Add(r);
                    else { BuildSlice(r, BuildBudgetMs * FramesLeftScale(r)); FrameParts[0] += ClockMs() - p0; }
                    break;
                case Phase.Flying:
                    Advance(r, dt);
                    FrameParts[1] += ClockMs() - p0;
                    break;
                case Phase.Merging:
                    if (r.restore) restoreOrder.Add(r);
                    else { MergeSlice(r, MergeBudgetMs, false); FrameParts[2] += ClockMs() - p0; }
                    break;
                case Phase.Disarming:
                    if (DisarmSlice(r)) runs.RemoveAt(i);
                    FrameParts[3] += ClockMs() - p0;
                    break;
            }
        }
        var q0 = ClockMs();
        if (restoreOrder.Count > 0) TickRestores();
        var q1 = ClockMs();
        Arm();
        var q2 = ClockMs();
        var busy = false;
        // (fix r1: a return in progress counts too: the pool's prewarm ran beside the restores and added to their frames)
        foreach (var r in runs) if (r.phase == Phase.Building || r.phase == Phase.Flying || r.phase == Phase.Merging || r.phase == Phase.Disarming) busy = true;
        if (!busy) Prewarm();
        var q3 = ClockMs();
        vfx.Tick(dt);
        var q4 = ClockMs();
        if ((Time.frameCount & 15) == 0) EnforceLiveCap();
        var q5 = ClockMs();
        FrameParts[4] = q1 - q0; FrameParts[5] = q2 - q1; FrameParts[6] = q3 - q2; FrameParts[7] = q4 - q3; FrameParts[8] = q5 - q4;
        FrameMs = q5 - f0;
        WorstFrameMs = Math.Max(WorstFrameMs, FrameMs);
    }

    // eng r1 #3: the windows coming back with a rebuilt chunk share ONE budget per frame with the breakables' pattern restores
    // (FrontRoomsGlassBreakable.RestoreBudgetLeft), nearest first; the budget grows as the nearest one nears the far plane, and a
    // window still unfinished within NearFinishMetres finishes at once.
    readonly List<Run> restoreOrder = new List<Run>(32);
    readonly List<float> restoreDist = new List<float>(32);

    void TickRestores()
    {
        Eye(out var eye);
        restoreDist.Clear();
        foreach (var r in restoreOrder) restoreDist.Add(r.root != null ? (r.root.position - eye).sqrMagnitude : float.MaxValue);
        // nearest first (insertion sort: a handful of windows, no allocation)
        for (var i = 1; i < restoreOrder.Count; i++)
        {
            var rr = restoreOrder[i];
            var dd = restoreDist[i];
            var j = i - 1;
            while (j >= 0 && restoreDist[j] > dd) { restoreOrder[j + 1] = restoreOrder[j]; restoreDist[j + 1] = restoreDist[j]; j--; }
            restoreOrder[j + 1] = rr;
            restoreDist[j + 1] = dd;
        }
        for (var i = 0; i < restoreOrder.Count; i++)
        {
            var r = restoreOrder[i];
            var d = Mathf.Sqrt(restoreDist[i]);
            var now = d <= FrontRoomsGlassBreakable.RestoreNearFinishMetres;
            var left = now ? 1e9 : FrontRoomsGlassBreakable.RestoreBudgetLeft(d);
            if (left <= 0.0) break;
            if (now) { r.finishedAtOnceM = d; r.restoreNote += (r.restoreNote.Length > 0 ? "; " : "") + "finished at once at " + d.ToString("0.0") + " m"; }
            var t0 = ClockMs();
            if (r.phase == Phase.Building) RestoreSlice(r, left);
            if (r.phase == Phase.Merging) MergeSlice(r, now ? 1e9 : Math.Max(0.0, left - (ClockMs() - t0)), now);
            var ms = ClockMs() - t0;
            FrontRoomsGlassBreakable.SpendRestore(ms);
        }
    }

    // Kept progress: a pane restored at stage 2 gets its shard set built when the camera comes within ArmDistance; at most
    // MaxArmed at once (the farthest one that is not being struck goes back to waiting).
    /// <summary>The player's eye: Camera.main, else the screen camera with the highest depth (the game's camera is not tagged).</summary>
    static bool Eye(out Vector3 eye)
    {
        var cam = Camera.main;
        if (cam == null)
        {
            var n = Camera.allCamerasCount;
            if (cameraScratch.Length < n) cameraScratch = new Camera[n + 4];
            Camera.GetAllCameras(cameraScratch);
            for (var i = 0; i < n; i++)
            {
                var c = cameraScratch[i];
                if (c == null || !c.enabled || c.targetTexture != null) continue;
                if (cam == null || c.depth > cam.depth) cam = c;
            }
        }
        eye = cam != null ? cam.transform.position : Vector3.zero;
        return cam != null;
    }

    static Camera[] cameraScratch = new Camera[8];

    static bool Held(FrontRoomsGlassBreakable g) => g != null && g.Window != null && g.Window.started;

    /// <summary>eng r1 #2: a live Crack2 (the player is striking this pane) is armed at once; if that makes more than MaxArmed sets,
    /// the farthest armed pane that is not being struck is taken apart in slices (from the next frame) and waits again.</summary>
    void ArmLive(FrontRoomsGlassBreakable glass)
    {
        pendingArm.Remove(glass);
        if (RunOfHere(glass) != null) return;
        StartRun(glass);
        MakeRoom(glass, true);
    }

    // The farthest armed pane other than `keep` that is not being struck (null if none); its squared distance to the eye.
    Run FarthestArmed(FrontRoomsGlassBreakable keep, Vector3 eye, out float farD)
    {
        Run far = null;
        farD = -1f;
        foreach (var r in runs)
        {
            if (r.restore || (r.phase != Phase.Building && r.phase != Phase.Ready) || r.glass == null || r.glass == keep || Held(r.glass)) continue;
            var d = (r.root.position - eye).sqrMagnitude;
            if (d > farD) { farD = d; far = r; }
        }
        return far;
    }

    // Over MaxArmed (a pane being struck was just armed): take apart the farthest other armed pane that is not being struck.
    void MakeRoom(FrontRoomsGlassBreakable keep, bool force)
    {
        Eye(out var eye);
        while (ArmedCount > MaxArmed)
        {
            var far = FarthestArmed(keep, eye, out var farD);
            if (far == null) break;
            BeginDisarm(far, "disarmed: a pane being struck reached Crack2 (at most " + MaxArmed + " armed)");
        }
    }

    void Arm()
    {
        if (pendingArm.Count == 0) return;
        if (!Eye(out var eye)) return;
        for (var i = pendingArm.Count - 1; i >= 0; i--)
        {
            var g = pendingArm[i];
            if (g == null || g.Stage != 2 || !g.Ready) { pendingArm.RemoveAt(i); continue; }
            if (RunOfHere(g) != null) { pendingArm.RemoveAt(i); continue; }
            var held = Held(g);
            var gd = (g.transform.position + Vector3.up * 1.2f - eye).sqrMagnitude;
            if (!held && gd > ArmDistance * ArmDistance) continue;
            if (ArmedCount >= MaxArmed)
            {
                // a pane being struck always gets a set; a pane merely near only takes the slot of one beyond ArmDistance and farther
                var far = FarthestArmed(g, eye, out var farD);
                if (far == null) continue;
                if (!held && (farD <= ArmDistance * ArmDistance || farD <= gd)) continue;
                BeginDisarm(far, held ? "disarmed: the pane being struck takes its slot" : "disarmed: another pane is nearer");
            }
            pendingArm.RemoveAt(i);
            StartRun(g);
        }
    }

    // A disarmed set goes back to the pools in slices (the shards 32 a frame, the meshes, then the proxies 16 a frame); the pane
    // keeps its stage, its chips and its particles, and waits in pendingArm.
    void BeginDisarm(Run r, string why)
    {
        if (r.baking) { r.bakeHandle.Complete(); r.baking = false; }
        if (r.bakeIds.IsCreated) r.bakeIds.Dispose();
        r.job?.Dispose();
        r.DisposeSource();
        if (r.lite != null) FrontRoomsGlassDebrisLite.Release(r);
        r.phase = Phase.Disarming;
        r.disarmWhy = why;
        r.cursor = 0;
        r.mergeStep = 0;
        if (r.glass != null && r.glass.Stage == 2 && !pendingArm.Contains(r.glass)) pendingArm.Add(r.glass);
        Disarmed++;
        Trace?.Invoke("debris: " + why + " (" + (r.root != null ? r.root.name : "?") + "): its set goes back to the pools in slices");
    }

    /// <summary>Lab: armed sets taken apart since the start.</summary>
    public static int Disarmed { get; private set; }

    bool DisarmSlice(Run r)
    {
        if (r.mergeStep == 0)
        {
            var end = Math.Min(r.shardObj.Count, r.cursor + 32);
            for (; r.cursor < end; r.cursor++) ReturnShard(r.shardObj[r.cursor]);
            if (r.cursor < r.shardObj.Count) return false;
            r.shardObj.Clear();
            r.renderersByRange = new Renderer[0];
            r.cursor = 0;
            r.mergeStep = 1;
            return false;
        }
        if (r.mergeStep == 1)
        {
            var n = 0;
            while (r.shardMesh.Count > 0 && n++ < 48) { GiveMesh(r.shardMesh[r.shardMesh.Count - 1]); r.shardMesh.RemoveAt(r.shardMesh.Count - 1); }
            while (r.hullMesh.Count > 0 && n++ < 96) { GiveMesh(r.hullMesh[r.hullMesh.Count - 1]); r.hullMesh.RemoveAt(r.hullMesh.Count - 1); }
            if (r.shardMesh.Count > 0 || r.hullMesh.Count > 0) return false;
            r.mergeStep = 2;
            return false;
        }
        if (!ReleaseProxiesSlice(r, 16)) return false;
        r.srcV.Clear(); r.srcV.TrimExcess(); r.srcI.Clear(); r.srcI.TrimExcess();
        r.phase = Phase.Aborted;
        Trace?.Invoke("debris: disarm finished (" + r.disarmWhy + ")");
        return true;
    }

    void Advance(Run r, float dt)
    {
        if (r.justShattered) { r.justShattered = false; return; }
        var scale = ShardTimeScale != null ? Mathf.Max(0f, ShardTimeScale((float)r.t)) : 1f;
        r.acc += dt * scale;
        var steps = 0;
        while (r.acc >= Substep - 1e-9 && r.phase == Phase.Flying)
        {
            r.acc -= Substep;
            if (r.reduced) FrontRoomsGlassDebrisLite.Step(this, r, Substep);
            else StepOnce(r);
            if (++steps >= MaxSubstepsPerFrame) { r.acc = 0; break; }
        }
        if (r.reduced && r.lite != null) FrontRoomsGlassDebrisLite.Pose(r);
    }

    static readonly Vector3 OpeningHalf = new Vector3(.70f, .82f, .09f);

    bool InOpening(Run r, Vector3 world)
    {
        var p = r.root.InverseTransformPoint(world);
        return Mathf.Abs(p.x) < OpeningHalf.x && p.y > .36f && p.y < 2.0f && Mathf.Abs(p.z) < OpeningHalf.z;
    }

    void StepOnce(Run r)
    {
        var any = false;
        var awake = 0;
        for (var s = 0; s < r.shardObj.Count; s++)
        {
            if (!r.released[s] && r.logs[s].releaseT <= r.t + 1e-9) Release(r, s);
            if (r.exiting[s] && r.t + 1e-9 >= r.logs[s].releaseT + ExitSeconds) { r.shardObj[s].Hull.excludeLayers = 0; r.exiting[s] = false; }
            if (r.released[s] && !r.frozen[s]) { any = true; awake++; }
        }
        if (!r.teethUnbowed && r.t + 1e-9 >= FrontRoomsShotTimings.GlassBreak.ObjectStop[2]) { r.floor.ApplyBlock(r.blockNoBow); r.teethUnbowed = true; }
        if (any)
        {
            var t0 = ClockMs();
            physics.Simulate(Substep);
            r.stepMs.Add(ClockMs() - t0);
            r.stepAwake.Add(awake);
        }
        r.t += Substep;
        r.substeps++;
        // settle, and keep the opening clear
        var capped = r.t >= FrontRoomsShotTimings.GlassBreak.SettleToStatic;
        var allFrozen = true;
        for (var s = 0; s < r.shardObj.Count; s++)
        {
            if (!r.released[s]) { allFrozen = false; continue; }
            if (r.frozen[s]) continue;
            var sh = r.shardObj[s];
            var b = sh.Body;
            var v = b.linearVelocity.magnitude;
            var w = b.angularVelocity.magnitude;
            var log = r.logs[s];
            if (v > log.maxSpeed) log.maxSpeed = v;
            var bounds = sh.Hull.bounds;
            // backup landing detector (contacts come from OnCollisionEnter): the hull's lowest point on the floor
            if (log.firstFloorT < 0 && bounds.min.y <= r.floorY + .004f) log.firstFloorT = r.t;
            var inOpening = InOpening(r, bounds.center);
            if (inOpening && r.t >= ClearAfter && v < .25f)
            {
                // a shard that stopped in the opening (on the sill, against a tooth): out to the side it leans to
                var lz = r.root.InverseTransformPoint(bounds.center).z;
                var dir = Mathf.Abs(lz) > .003f ? Mathf.Sign(lz) : r.farSign;
                b.AddForce(r.root.forward * (dir * ClearPush), ForceMode.Acceleration);
                if (!log.pushed) { log.pushed = true; r.pushes++; }
            }
            if (v < SettleSpeed && w < SettleAngular) r.quiet[s] += Substep; else r.quiet[s] = 0f;
            var freeze = !inOpening && (r.quiet[s] >= SettleQuiet || (capped && v < CapSpeed && w < CapAngular) || r.t >= HardCap);
            // in the opening at the hard cap: frozen as it is, the merge's clear-the-opening (F3) lays it on the floor below
            if (inOpening && r.t >= HardCap && v < CapSpeed) { if (!log.stuck) { log.stuck = true; r.stuck++; } freeze = true; }
            if (r.t >= StuckCap) { if (inOpening && !log.stuck) { log.stuck = true; r.stuck++; } freeze = true; }
            if (freeze)
            {
                if (capped && r.quiet[s] < SettleQuiet) log.frozenByCap = true;
                Freeze(r, s);
            }
            else allFrozen = false;
        }
        // the window's particles settle with it (the merge takes the resting ones); never past the hard cap
        if (allFrozen && (!vfx.Moving(r.group) || r.t >= HardCap))
        {
            r.settledAtT = r.t;
            r.settledByCap = capped;
            r.settleFrame = Time.frameCount;
            BeginMerge(r);
        }
    }

    void Freeze(Run r, int s)
    {
        var sh = r.shardObj[s];
        sh.Body.linearVelocity = Vector3.zero;
        sh.Body.angularVelocity = Vector3.zero;
        sh.Body.isKinematic = true;
        r.frozen[s] = true;
        r.logs[s].settledT = r.t;
    }

    /// <summary>eng r1 #1: freeze a flying window where it is (every shard kinematic, cheap) and start its merge, which runs in
    /// slices from the next frame on like any other: a second shatter, or the lab.</summary>
    public void FreezeForMerge(Run r, string why)
    {
        if (r.phase != Phase.Flying) return;
        var t0 = ClockMs();
        FreezeAll(r);
        r.settledAtT = r.t;
        r.settledByCap = true;
        r.settleFrame = Time.frameCount;
        BeginMerge(r);
        r.freezeMs = ClockMs() - t0;
        Trace?.Invoke("debris: frozen for the merge (" + why + ") in " + r.freezeMs.ToString("0.00") + " ms; the merge goes on in slices");
    }

    void FreezeAll(Run r)
    {
        if (r.reduced) { FrontRoomsGlassDebrisLite.FreezeAll(this, r); return; }
        for (var s = 0; s < r.shardObj.Count; s++)
        {
            if (!r.released[s]) Release(r, s);
            if (!r.frozen[s]) { r.logs[s].frozenByCap = true; Freeze(r, s); }
        }
    }

    /// <summary>The lab: settle a shattering window at once, merge included (never used by the game).</summary>
    public void ForceSettle(Run r, string why)
    {
        FreezeForMerge(r, why);
        while (r.phase == Phase.Merging) MergeSlice(r, 1e9, true);
        Trace?.Invoke("debris: forced settle (" + why + ")");
    }

    /// <summary>The chunk is released while its glass flies or merges: the record from the frozen poses, no merge (the meshes go with
    /// the chunk). One frame of work on the release frame (the opening cleared, the crumbs clustered, the record packed).</summary>
    void RecordAndDrop(Run r, string why)
    {
        if (r.restore || r.recordWritten) return;
        var t0 = ClockMs();
        if (r.phase == Phase.Flying)
        {
            FreezeAll(r);
            r.settledAtT = r.t;
            r.settledByCap = true;
            r.cursor = 0;   // (the build's cursor) the clear starts at the first shard
        }
        if (!r.cleared)
        {
            if (r.reduced) FrontRoomsGlassDebrisLite.ClearTheOpening(r);
            else while (!ClearTheOpening(r, int.MaxValue)) { }
            r.cleared = true;
        }
        if (!r.crumbsTaken && r.group != null) { vfx.TakeResting(r.group, r.crumbs); r.crumbsTaken = true; }
        r.clustersOut = Clusters(r.crumbs, r.f.seed);
        WriteRecord(r);
        Trace?.Invoke("debris: record written at the release (" + why + ", " + r.recordBytes + " B, " + (ClockMs() - t0).ToString("0.00") + " ms); no merge");
    }

    // ---- contacts (OnCollisionEnter of a shard, during Simulate) ---------------------------------------------------------------
    internal static void OnShardContact(FrontRoomsGlassShard sh, Collision c)
    {
        if (!(sh.Run is Run r) || sh.Slot < 0 || sh.Slot >= r.logs.Count) return;
        var log = r.logs[sh.Slot];
        log.contacts++;
        var cp = c.contactCount > 0 ? c.GetContact(0) : default;
        var point = c.contactCount > 0 ? cp.point : sh.transform.position;
        var n = c.contactCount > 0 ? cp.normal : Vector3.up;
        FrontRoomsGlassDebris.Surface surface;
        if (r.glassColliders.Contains(c.collider)) surface = FrontRoomsGlassDebris.Surface.Glass;
        else if (Mathf.Abs(n.y) > .7f && point.y < r.floorY + .06f) surface = FrontRoomsGlassDebris.Surface.Carpet;
        else if (Mathf.Abs(n.y) > .7f) surface = FrontRoomsGlassDebris.Surface.Furniture;
        else surface = FrontRoomsGlassDebris.Surface.Wall;
        RecordContact(r, sh.Slot, point, surface, c.relativeVelocity.magnitude);
    }

    internal static void RecordContact(Run r, int slot, Vector3 point, FrontRoomsGlassDebris.Surface surface, float speed)
    {
        var log = r.logs[slot];
        var t = r.t + Substep;   // the contact happened during this substep
        if (log.firstContactT < 0) { log.firstContactT = t; log.firstSurface = surface; }
        if (log.firstLowT < 0 && point.y < r.floorY + .12f) { log.firstLowT = t; log.lowSurface = surface; }
        var first = false;
        if (surface == FrontRoomsGlassDebris.Surface.Carpet || surface == FrontRoomsGlassDebris.Surface.Hard)
        {
            if (log.firstFloorContactT < 0) { log.firstFloorContactT = t; first = true; }
            if (log.firstFloorT < 0 || t < log.firstFloorT) log.firstFloorT = t;
        }
        if (surface == FrontRoomsGlassDebris.Surface.Hard && speed > RebreakSpeed && log.area > 60e-4f) r.rebreakWouldFire++;
        if (speed < FrontRoomsGlassDebris.ImpactMinSpeed) return;
        if (r.lastImpactT[slot] >= 0 && t - r.lastImpactT[slot] < FrontRoomsGlassDebris.ImpactMinGap) return;
        r.lastImpactT[slot] = t;
        FrontRoomsGlassDebris.RaiseImpact(new FrontRoomsGlassDebris.Impact { position = point, mass = log.mass, speed = speed, surface = surface, firstLanding = first, edge = r.edge });
    }

    // ---- the merge ---------------------------------------------------------------------------------------------------------------
    // Merge steps (eng r1: each one small, so the slice budget holds; the swap on a frame of its own).
    // fix r1 (eng #11): every step that loops is time-bounded (it stops at the slice deadline and goes on next frame); the copies
    // into the job's arrays are their own sliced step, the environment hash (a physics overlap) is its own step before the record.
    const int MClear = 0, MItems = 1, MCrumbs = 2, MCopy = 3, MCount = 4, MWrite = 5, MApply = 6, MClusters = 7, MEnv = 8, MRecord = 9, MCells = 10,
        MSwap = 11, MShards = 12, MProxies = 13, MNonReadable = 14, MDone = 15;
    public const int MergeStepCount = 16;
    public static readonly string[] MergeStepNames = { "clear", "items", "crumbs", "copy", "count", "write", "apply", "clusters", "env", "record", "cells", "swap", "shards", "proxies", "non-readable", "done" };

    internal void BeginMerge(Run r)
    {
        r.phase = Phase.Merging;
        r.cursor = 0;
        r.mergeStep = MClear;
        r.clearPending = !r.restore;
    }

    // F3 + art r1 #2, per shard (sliced): a shard that came to rest inside the clear zone of the opening (on the sill, against a
    // tooth) is laid flat on the floor below it, on the side it leans to: the opening of a broken window is always clear (V9).
    // A shard inside the dressed frame (the stool, a nosing, an apron, a casing), or more than UprightDegrees off flat without
    // leaning on the wall (a sliver frozen mid-topple at the cap, one standing point-down), is laid flat on what is under it.
    // Keyed, so a restore gives the same pose (the pose is stored anyway). True when every shard is done.
    bool ClearTheOpening(Run r, int perSlice) => ClearTheOpening(r, perSlice, double.MaxValue);

    bool ClearTheOpening(Run r, int perSlice, double deadline)
    {
        var n = r.shardObj.Count;
        var end = perSlice >= n - r.cursor ? n : r.cursor + perSlice;
        if (r.frameBoxes.Count == 0 && FrameProxies) FrameBoxes(r.root, r.frameBoxes);
        while (r.cursor < end)
        {
            SettleCheck(r, r.cursor);
            r.cursor++;
            if (ClockMs() >= deadline) break;
        }
        return r.cursor >= n;
    }

    void SettleCheck(Run r, int s)
    {
        var sh = r.shardObj[s];
        if (sh == null) return;
        var log = r.logs[s];
        var pc = r.f.pieces[log.piece];
        var c = StageCentre(r, log);
        var lc = r.root.InverseTransformPoint(sh.transform.TransformPoint(c));
        var side = Mathf.Abs(lc.z) > .002f ? (int)Mathf.Sign(lc.z) : r.farSign;
        var nW = sh.transform.TransformDirection(Vector3.forward);
        var flatW = Quaternion.FromToRotation(nW, nW.y >= 0f ? Vector3.up : Vector3.down) * sh.transform.rotation;
        if (HullInClearZone(r, sh) || RenderInClearZone(r, sh))
        {
            var spin = Quaternion.AngleAxis(360f * Rk(r.f.seed, 172, pc.id, 0), Vector3.up);
            PlaceFlat(r, s, spin * flatW, new Vector2(Mathf.Clamp(lc.x, -.55f, .55f), side * (.10f + .22f * Rk(r.f.seed, 171, pc.id, 0))), side, .2f);
            log.dropped = true;
            r.droppedFromOpening++;
            return;
        }
        var m = sh.Hull.sharedMesh;
        if (m == null || !m.isReadable) return;
        hullV.Clear();
        m.GetVertices(hullV);
        var l2r = r.root.worldToLocalMatrix * sh.transform.localToWorldMatrix;
        var inFrame = false;
        Vector3 top = new Vector3(0f, float.MinValue, 0f), bottom = new Vector3(0f, float.MaxValue, 0f);
        foreach (var v in hullV)
        {
            var q = l2r.MultiplyPoint3x4(v);
            if (q.y > top.y) top = q;
            if (q.y < bottom.y) bottom = q;
            if (!inFrame) for (var b = 0; b + 1 < r.frameBoxes.Count; b += 2)
                {
                    var mn = r.frameBoxes[b];
                    var mx = r.frameBoxes[b + 1];
                    if (q.x > mn.x + .002f && q.x < mx.x - .002f && q.y > mn.y + .002f && q.y < mx.y - .002f && q.z > mn.z + .002f && q.z < mx.z - .002f) { inFrame = true; break; }
                }
        }
        var nR = r.root.InverseTransformDirection(nW);
        var tilt = Mathf.Acos(Mathf.Clamp01(Mathf.Abs(nR.y))) * Mathf.Rad2Deg;
        // leaning on the wall: its foot on the floor, its top against the wall face, an apron or the stool's nose on its side
        // fix r1 (art #2, second pass): only a real lean (at most LeanMaxDegrees off flat: a plate standing near-vertical topples) and
        // at most MaxLeaningPerWindow of them; the rest are laid flat
        var leaning = bottom.y < .03f && Mathf.Abs(top.z) >= FrontRoomsVfxWallFace - .006f && Mathf.Abs(top.z) <= .135f && Mathf.Abs(bottom.z) > Mathf.Abs(top.z)
            && tilt <= LeanMaxDegrees && r.leaning < MaxLeaningPerWindow;
        if (inFrame)
        {
            PlaceFlat(r, s, flatW, new Vector2(lc.x, lc.z), side, Mathf.Min(lc.y + .05f, .2f));
            r.frameDrops++;
            log.dropped = true;
        }
        else if (tilt > UprightDegrees)
        {
            if (leaning) r.leaning++;
            else
            {
                PlaceFlat(r, s, flatW, new Vector2(lc.x, lc.z), side, lc.y + .05f);
                r.laidFlat++;
                log.dropped = true;
            }
        }
    }

    const float FrontRoomsVfxWallFace = .080f;
    static readonly RaycastHit[] supportHits = new RaycastHit[16];

    // Lay shard s flat (rotation rotW, world) with its centre over (x, z) of the root frame, its footprint out of the wall slab
    // on `side`, its lowest point on the first debris-scene world surface below rayFromY (floor, desk, stool), plus a keyed 0.5-2 mm
    // so two flat pieces never share a plane.
    void PlaceFlat(Run r, int s, Quaternion rotW, Vector2 xz, int side, float rayFromY)
    {
        var sh = r.shardObj[s];
        var log = r.logs[s];
        var pc = r.f.pieces[log.piece];
        var c = StageCentre(r, log);
        var rotR = Quaternion.Inverse(r.root.rotation) * rotW;
        float minY = float.MaxValue, minZ = float.MaxValue, maxZ = float.MinValue;
        var m = sh.Hull.sharedMesh;
        hullV.Clear();
        if (m != null && m.isReadable) m.GetVertices(hullV);
        if (hullV.Count == 0) hullV.Add(c);
        foreach (var v in hullV)
        {
            var q = rotR * (v - c);
            if (q.y < minY) minY = q.y;
            if (q.z < minZ) minZ = q.z;
            if (q.z > maxZ) maxZ = q.z;
        }
        var z = xz.y;
        if (side > 0) z = Mathf.Max(z, FrontRoomsVfxWallFace + .002f - minZ);
        else z = Mathf.Min(z, -(FrontRoomsVfxWallFace + .002f) - maxZ);
        var support = SupportY(r, new Vector3(xz.x, Mathf.Max(rayFromY, .02f), z));
        var y = support - minY + .0005f + .0015f * Rk(r.f.seed, 173, pc.id, 0);
        var pos = r.root.TransformPoint(new Vector3(xz.x, y, z)) - rotW * c;
        sh.transform.SetPositionAndRotation(pos, rotW);
    }

    // The first world surface below a root-frame point in the debris scene (its proxies of the floor, furniture, the frame); 0 = the floor.
    float SupportY(Run r, Vector3 fromRoot)
    {
        if (!physics.IsValid()) return 0f;
        var o = r.root.TransformPoint(fromRoot);
        var n = physics.Raycast(o, -r.root.up, supportHits, fromRoot.y + .5f, layersOk ? 1 << LayerWorld : ~0, QueryTriggerInteraction.Ignore);
        var best = float.MaxValue;
        var y = 0f;
        for (var i = 0; i < n; i++)
        {
            var h = supportHits[i];
            if (h.collider == null || r.glassColliders.Contains(h.collider) || h.collider.attachedRigidbody != null && !h.collider.attachedRigidbody.isKinematic) continue;
            if (h.distance < best) { best = h.distance; y = r.root.InverseTransformPoint(h.point).y; }
        }
        return Mathf.Max(0f, y);
    }

    /// <summary>The dressed frame's parts as boxes in the window root's frame (min, max pairs): the kit's section (10_spec sec 5.2) for the
    /// walnut member (through-stool with nosings, aprons, casings) or the steel one (sill band, face bands). None if no kit frame hangs here.</summary>
    public static void FrameBoxes(Transform root, List<Vector3> dst)
    {
        dst.Clear();
        if (root == null) return;
        Transform frame = null;
        for (var i = 0; i < root.childCount; i++) if (root.GetChild(i).name == FrontRoomsInteractableKit.WindowFrameLabel) { frame = root.GetChild(i); break; }
        if (frame == null) return;
        var steel = false;
        foreach (var mf in frame.GetComponentsInChildren<MeshFilter>(true))
            if (mf.sharedMesh != null && mf.sharedMesh.name.IndexOf("Steel", StringComparison.OrdinalIgnoreCase) >= 0) { steel = true; break; }
        const float face = .775f, lining = .6995f, wallZ = .080f, casingZ = .105f, head = 2.075f, liningSill = .3505f, liningHead = 1.9995f;
        if (!steel)
        {
            Box(dst, new Vector3(-.800f, .3255f, -.121f), new Vector3(.800f, liningSill, .121f));                       // through-stool + nosings
            foreach (var sd in new[] { 1f, -1f })
            {
                Box(dst, new Vector3(-face, .250f, sd > 0 ? wallZ : -casingZ), new Vector3(face, .3255f, sd > 0 ? casingZ : -wallZ));   // apron
                Box(dst, new Vector3(lining, liningSill, sd > 0 ? wallZ : -casingZ), new Vector3(face, head, sd > 0 ? casingZ : -wallZ));    // jamb casings
                Box(dst, new Vector3(-face, liningSill, sd > 0 ? wallZ : -casingZ), new Vector3(-lining, head, sd > 0 ? casingZ : -wallZ));
                Box(dst, new Vector3(-face, liningHead, sd > 0 ? wallZ : -casingZ), new Vector3(face, head, sd > 0 ? casingZ : -wallZ));   // head casing
            }
        }
        else
        {
            Box(dst, new Vector3(-face, .2745f, -casingZ), new Vector3(face, liningSill, casingZ));                          // sill band (C section)
            foreach (var sd in new[] { 1f, -1f })
            {
                Box(dst, new Vector3(lining, liningSill, sd > 0 ? wallZ : -casingZ), new Vector3(face, head, sd > 0 ? casingZ : -wallZ));
                Box(dst, new Vector3(-face, liningSill, sd > 0 ? wallZ : -casingZ), new Vector3(-lining, head, sd > 0 ? casingZ : -wallZ));
                Box(dst, new Vector3(-face, liningHead, sd > 0 ? wallZ : -casingZ), new Vector3(face, head, sd > 0 ? casingZ : -wallZ));
            }
        }
    }

    static void Box(List<Vector3> dst, Vector3 a, Vector3 b) { dst.Add(Vector3.Min(a, b)); dst.Add(Vector3.Max(a, b)); }

    bool HullInClearZone(Run r, FrontRoomsGlassShard sh)
    {
        var b = sh.Hull.bounds;
        var w2r = r.root.worldToLocalMatrix;
        var mn = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        var mx = -mn;
        for (var i = 0; i < 8; i++)
        {
            var p = w2r.MultiplyPoint3x4(new Vector3((i & 1) != 0 ? b.max.x : b.min.x, (i & 2) != 0 ? b.max.y : b.min.y, (i & 4) != 0 ? b.max.z : b.min.z));
            mn = Vector3.Min(mn, p); mx = Vector3.Max(mx, p);
        }
        // the shard must reach into the zone with a real part of itself, not graze it with a bounds corner
        return mx.x > ClearMin.x + .01f && mn.x < ClearMax.x - .01f && mx.y > ClearMin.y + .01f && mn.y < ClearMax.y - .01f && mx.z > ClearMin.z + .005f && mn.z < ClearMax.z - .005f
            && ClearZoneDepth(r, sh) > 0;
    }

    // how many of the shard's hull vertices are really inside the zone (the hull's AABB is loose for a tilted plate)
    // The job's own V9 test on this shard's render vertices (the hull test lets a shallow graze through: fix1 run, 12 and 24 LOD0
    // vertices left in the clear zone with the frame proxies in place).
    static readonly List<Vector3> renderV = new List<Vector3>(512);

    bool RenderInClearZone(Run r, FrontRoomsGlassShard sh)
    {
        var m = sh.Filter != null ? sh.Filter.sharedMesh : null;
        if (m == null || !m.isReadable) return false;
        renderV.Clear();
        m.GetVertices(renderV);
        var l2r = r.root.worldToLocalMatrix * sh.transform.localToWorldMatrix;
        foreach (var v in renderV)
        {
            var p = l2r.MultiplyPoint3x4(v);
            if (p.x > ClearMin.x && p.x < ClearMax.x && p.y > ClearMin.y && p.y < ClearMax.y && p.z > ClearMin.z && p.z < ClearMax.z) return true;
        }
        return false;
    }

    int ClearZoneDepth(Run r, FrontRoomsGlassShard sh)
    {
        var m = sh.Hull.sharedMesh;
        if (m == null || !m.isReadable) return 1;
        hullV.Clear();
        m.GetVertices(hullV);
        var l2r = r.root.worldToLocalMatrix * sh.transform.localToWorldMatrix;
        var n = 0;
        foreach (var v in hullV)
        {
            var p = l2r.MultiplyPoint3x4(v);
            if (p.x > ClearMin.x && p.x < ClearMax.x && p.y > ClearMin.y && p.y < ClearMax.y && p.z > ClearMin.z && p.z < ClearMax.z) n++;
        }
        return n;
    }

    // The merge, one step at a time inside the slice budget (MergeStepNames): the opening cleared and every shard checked (sliced),
    // the items, the crumbs (sliced), the counting job, the writing job, apply (hidden), the crumb clusters, the record (the frame
    // ends), the footstep cells (the frame ends), THE SWAP (one frame), shards back to the pool, proxies and tooth hulls, non-readable,
    // then done (the working set goes back).
    void MergeSlice(Run r, double budgetMs, bool force)
    {
        var t0 = ClockMs();
        var yieldFrame = false;
        // fix r1: a window coming back from its record has no shards on screen and no beat to wait for, so its merge runs as many
        // steps as the shared restore budget allows on one frame instead of one step a frame (but never completes a job in place)
        var go = force || r.restore;
        var deadline = force ? double.MaxValue : t0 + budgetMs;
        do
        {
            var stepT0 = ClockMs();
            var mstep = Mathf.Min(r.mergeStep, MDone);
            switch (r.mergeStep)
            {
                case MClear:
                    if (r.clearPending && !r.cleared)
                    {
                        if (r.reduced) { FrontRoomsGlassDebrisLite.ClearTheOpening(r); r.cleared = true; }
                        else if (ClearTheOpening(r, int.MaxValue, deadline)) r.cleared = true;
                        if (!r.cleared) break;
                    }
                    r.cursor = 0;
                    r.mergeStep++;
                    break;
                case MItems:
                    if (r.cursor == 0 && !r.itemsBegun) BeginItems(r);
                    if (ItemsSlice(r, deadline)) { r.itemsBegun = false; r.cursor = 0; r.mergeStep++; }
                    break;
                case MCrumbs:
                    if (CrumbSlice(r, deadline)) { r.cursor = 0; r.mergeStep++; }
                    break;
                case MCopy:
                    if (r.frameBoxes.Count == 0 && FrameProxies) FrameBoxes(r.root, r.frameBoxes);
                    if (r.job.PrepareSlice(r, deadline)) r.mergeStep++;
                    break;
                case MCount:
                    r.job.ScheduleCount();
                    r.mergeStep++;
                    yieldFrame = !go;
                    break;
                case MWrite:
                    // a job not finished yet always waits a frame (Complete would run it on the main thread: a 15 ms return frame in E1)
                    if (!force && !r.job.countHandle.IsCompleted) { yieldFrame = true; break; }
                    r.job.ScheduleWrite(r, !r.reduced);
                    r.mergeStep++;
                    yieldFrame = !go;
                    break;
                case MApply:
                {
                    if (!force && !r.job.writeHandle.IsCompleted) { yieldFrame = true; break; }
                    r.floor.FloorMeshes(out var lod0, out var lod1);
                    r.job.Apply(r, lod0, lod1);
                    r.floor.FloorApplied(!r.reduced, r.reduced);
                    r.floor.DroppedTris = r.job.dropped;
                    r.floor.ClearZoneVerts = r.job.clearVerts;
                    r.floor.FrameVerts = r.job.frameVerts;
                    r.floor.MergedTail = r.reduced ? r.job.tail : 0;
                    r.floor.ApplyBlock(r.blockNoBow ?? r.block);
                    r.mergeStep++;
                    yieldFrame = !go;
                    break;
                }
                case MClusters:
                    if (!r.restore && !r.recordWritten) r.clustersOut = Clusters(r.crumbs, r.f.seed);
                    r.mergeStep++;
                    break;
                case MEnv:
                    if (!r.restore && !r.recordWritten && !r.envHashSet) { r.envHash = EnvHash(r); r.envHashSet = true; }
                    r.mergeStep++;
                    break;
                case MRecord:
                    if (!r.restore && !r.recordWritten) WriteRecord(r);
                    r.cursor = 0;
                    r.mergeStep++;
                    yieldFrame = !go;
                    break;
                case MCells:
                    if (MarkCellsSlice(r, deadline)) { r.cursor = 0; r.mergeStep++; yieldFrame = !go; }
                    break;
                case MSwap:
                {
                    // THE SWAP: the floor glass on, the shards off, on one frame (the same poses: only the draw order changes)
                    var s0 = ClockMs();
                    r.floor.ShowFloor(true);
                    if (r.reduced) { r.floor.DropTeethMesh(); FrontRoomsGlassDebrisLite.Release(r); }
                    else if (r.restore) r.floor.ShowTeeth(true);
                    for (var s = 0; s < r.shardObj.Count; s++) if (r.shardObj[s] != null) r.shardObj[s].Renderer.enabled = false;
                    r.swapMs = ClockMs() - s0;
                    r.swapFrame = Time.frameCount;
                    r.cursor = 0;
                    r.mergeStep++;
                    break;
                }
                case MShards:
                {
                    var end = Math.Min(r.shardObj.Count, r.cursor + 16);
                    for (; r.cursor < end; r.cursor++) ReturnShard(r.shardObj[r.cursor]);
                    if (r.cursor >= r.shardObj.Count)
                    {
                        foreach (var m in r.shardMesh) GiveMesh(m);
                        foreach (var m in r.hullMesh) GiveMesh(m);
                        r.shardMesh.Clear();
                        r.hullMesh.Clear();
                        r.shardObj.Clear();
                        r.renderersByRange = new Renderer[0];
                        r.cursor = 0;
                        r.mergeStep++;
                    }
                    break;
                }
                case MProxies:
                    if (ReleaseProxiesSlice(r, 16)) r.mergeStep++;
                    break;
                case MNonReadable:
                    if (!r.reduced) r.floor.MakeNonReadable();   // the reduced tier's one mesh stays readable (small; the climb snap)
                    r.mergeStep++;
                    break;
                default:
                    r.phase = Phase.Done;
                    r.mergedFrame = Time.frameCount;
                    r.job.Dispose();
                    r.DisposeSource();
                    try { r.glass.ReleaseWorkingSet(); } catch (Exception e) { Debug.LogException(e); }
                    r.srcV.Clear(); r.srcV.TrimExcess(); r.srcI.Clear(); r.srcI.TrimExcess();
                    r.crumbV.Clear(); r.crumbV.TrimExcess(); r.crumbI.Clear(); r.crumbI.TrimExcess();
                    r.floor.FreeTeethSource();
                    // eng r1 #4: a window's particle group goes once its resting particles are in the floor glass (none left alive);
                    // a group still holding live particles goes when its window does (FrontRoomsGlassVfx.Tick)
                    if (r.group != null && r.group.Alive == 0) { vfx.Release(r.group); r.group = null; }
                    try { Settled?.Invoke(r.glass, r.floor); } catch (Exception e) { Debug.LogException(e); }
                    Trace?.Invoke("debris: " + (r.restore ? "restored from the record (" + r.restoreNote + ")" : "settled at t " + r.settledAtT.ToString("0.000") + " s" + (r.settledByCap ? " (cap)" : ""))
                        + " · merged in " + (r.mergeFrames + 1) + " frames (merge worst " + r.mergeWorstMs.ToString("0.00") + " ms, swap " + r.swapMs.ToString("0.000") + " ms): floor glass " + r.floor.FloorTris
                        + " tris / " + r.floor.FloorVerts + " verts (LOD1 " + r.floor.FloorLod1Tris + ", dropped " + r.floor.DroppedTris + " bottom tris), teeth " + r.floor.TeethTris + " tris, " + r.crumbsMerged
                        + " crumbs · clear zone " + r.floor.ClearZoneVerts + " verts · in the frame " + r.floor.FrameVerts + " verts · dropped out of the opening " + r.droppedFromOpening
                        + " · laid flat " + r.laidFlat + " (upright, unsupported) + " + r.frameDrops + " (inside the frame), leaning on the wall " + r.leaning + ", upright at the record " + r.uprightLeft
                        + " · V10 crumbs at the wall base: player side " + r.wallBaseBreaker + ", far side " + r.wallBaseFar + ", on the sill " + r.crumbsOnSill
                        + " · " + (r.floor.Bytes() / 1024) + " KB GPU, " + (r.floor.CpuBytes() / 1024)
                        + " KB CPU copy · record " + r.recordBytes + " B" + (r.reduced ? " · reduced tier (1 mesh)" : "") + " · merge step worst ms " + MergeSteps(r) + " · JIT warm-up " + JitWarmUpState);
                    break;
            }
            var md = ClockMs() - stepT0;
            r.mergeStepMs[mstep] += md;
            r.mergeStepWorst[mstep] = Math.Max(r.mergeStepWorst[mstep], md);
        } while (r.phase == Phase.Merging && !yieldFrame && ClockMs() - t0 < budgetMs);
        var ms = ClockMs() - t0;
        r.mergeCpuMs += ms;
        r.mergeWorstMs = Math.Max(r.mergeWorstMs, ms);
        r.mergeFrames++;
    }

    // The merge items: every shard at its settled (or stored) pose, in the window root's frame; on the reduced tier the teeth
    // too (one mesh). The resting crumbs of this window are taken from the particles here (moving ones stay particles); their
    // meshes are written in CrumbSlice.
    // The merge items, in slices (fix r1): every shard at its settled (or stored) pose, in the window root's frame; on the reduced
    // tier the teeth too (one mesh). The resting crumbs of this window are taken from the particles here (moving ones stay
    // particles); their meshes are written in CrumbSlice.
    void BeginItems(Run r)
    {
        r.items.Clear();
        r.crumbV.Clear();
        r.crumbI.Clear();
        r.itemsBegun = true;
        if (r.restore)
        {
            r.pieceToRange.Clear();
            for (var k = 0; k < r.rangePiece.Count; k++) r.pieceToRange[r.rangePiece[k]] = k;
        }
        else if (!r.crumbsTaken) { r.crumbs.Clear(); if (r.group != null) vfx.TakeResting(r.group, r.crumbs); r.crumbsTaken = true; }
    }

    bool ItemsSlice(Run r, double deadline)
    {
        var flatCos = Mathf.Cos(FlatDegrees * Mathf.Deg2Rad);
        if (r.restore)
        {
            while (r.cursor < r.recShards.Count)
            {
                var s = r.recShards[r.cursor++];
                if (!r.pieceToRange.TryGetValue(s.piece, out var k)) continue;
                var log = new ShardLog { piece = s.piece };
                var c = StageCentre(r, log);
                var m = Matrix4x4.TRS(s.position - s.rotation * c, s.rotation, Vector3.one);
                AddItem(r, k, m, flatCos, true);
                if ((r.cursor & 7) == 0 && ClockMs() >= deadline) return false;
            }
            if (r.reduced) AddTeethItems(r);
            return true;
        }
        if (r.reduced)
        {
            FrontRoomsGlassDebrisLite.Items(r, flatCos);   // at most 40 bodies
            AddTeethItems(r);
            return true;
        }
        var w2r = r.root.worldToLocalMatrix;
        while (r.cursor < r.shardObj.Count)
        {
            var s = r.cursor++;
            AddItem(r, r.shardK[s], w2r * r.shardObj[s].transform.localToWorldMatrix, flatCos, true);
            if ((r.cursor & 7) == 0 && ClockMs() >= deadline) return false;
        }
        return true;
    }

    // The crumbs into the floor-glass vertex list, `perSlice` at a time (flight: the resting particles; restore: the record's
    // clusters scattered again from their seeds). None inside the clear zone. True when done.
    bool CrumbSlice(Run r, double deadline)
    {
        if (r.restore)
        {
            // whole clusters (each up to a few hundred crumbs) until the deadline
            while (r.cursor < r.recClusters.Count)
            {
                ScatterCluster(r, r.recClusters[r.cursor++]);
                if (ClockMs() >= deadline) break;
            }
            return r.cursor >= r.recClusters.Count;
        }
        var n = r.crumbs.Count;
        for (; r.cursor < n; r.cursor++)
        {
            if ((r.cursor & 15) == 15 && ClockMs() >= deadline) return false;
            var cr = r.crumbs[r.cursor];
            var p = cr.position;
            if (p.x > ClearMin.x && p.x < ClearMax.x && p.y > ClearMin.y && p.y < ClearMax.y && p.z > ClearMin.z && p.z < ClearMax.z) continue;
            FrontRoomsGlassVfx.AppendCrumb(r.crumbV, r.crumbI, cr.position, cr.rotation, cr.size, cr.shape, cr.kind, cr.paneUV, r.group != null ? r.group.keyA : 0f, r.group != null ? r.group.keyB : 0f, r.crumbV.Count);
            r.crumbsMerged++;
            // V10: the fine glass at the wall base (within 0.3 m of the wall face, on the carpet), per side
            if (p.y > .2f) r.crumbsOnSill++;
            else if (Mathf.Abs(p.z) - FrontRoomsGlassVfx.WallHalf < .30f) { if ((int)Mathf.Sign(p.z) == -r.farSign) r.wallBaseBreaker++; else r.wallBaseFar++; }
        }
        return r.cursor >= n;
    }

    internal static void AddItem(Run r, int k, Matrix4x4 m, float flatCos, bool lod1)
    {
        VertexSpan(r, k, out var v0, out var vn);
        var n = m.MultiplyVector(Vector3.forward);
        var flat = Mathf.Abs(n.normalized.y) >= flatCos;
        r.items.Add(new FrontRoomsGlassDebrisJobs.MergeItem
        {
            m = m, v0 = v0, vn = vn,
            sideStart = r.rangeSide[k].indexStart, sideCount = r.rangeSide[k].indexCount,
            faceStart = r.rangeFace[k].indexStart, faceCount = r.rangeFace[k].indexCount,
            dropDown = (byte)(flat ? 1 : 0), lod1 = (byte)(lod1 ? 1 : 0),
        });
    }

    // Reduced tier: the teeth (rest pose, slab frame -> root frame) join the one merged mesh, minus the snapped ones.
    void AddTeethItems(Run r)
    {
        var slab = Matrix4x4.Translate(new Vector3(0f, (float)FrontRoomsGlassFracturePattern.SlabCentreY, 0f));
        var tooth = 0;
        var snapped = r.floor.SnappedMask;
        for (var k = 0; k < r.rangePiece.Count; k++)
        {
            if (r.f.pieces[r.rangePiece[k]].kind != GlassPieceKind.Tooth) continue;
            var gone = tooth < 64 && (snapped & (1UL << tooth)) != 0;
            tooth++;
            if (gone) continue;
            AddItem(r, k, slab, 2f, false);
            if (r.f.pieces[r.rangePiece[k]].side == 'B')
            {
                var it = r.items[r.items.Count - 1];
                it.tail = 1;
                r.items[r.items.Count - 1] = it;
            }
        }
    }

    void ScatterCluster(Run r, FrontRoomsGlassDebrisPack.Cluster c)
    {
        var keys = r.group != null ? new Vector2(r.group.keyA, r.group.keyB) : Vector2.zero;
        if (r.group == null)
        {
            FrontRoomsGlassFractureMeshes.PaneKeys(Matrix4x4.TRS(r.root.TransformPoint(new Vector3(0f, (float)FrontRoomsGlassFracturePattern.SlabCentreY, 0f)), r.root.rotation, Vector3.one), out var ka, out var kb);
            keys = new Vector2(ka, kb);
        }
        var y = c.level == 1 ? FrontRoomsGlassVfx.Sill : 0f;
        for (var i = 0; i < c.count; i++)
        {
            var u = Rk(c.seed, 181, i, 0) * 2f - 1f;
            var v = Rk(c.seed, 181, i, 1) * 2f - 1f;
            var size = Mathf.Lerp(.002f, .009f, Rk(c.seed, 182, i, 0) * Rk(c.seed, 182, i, 1));
            var p = new Vector3(c.centre.x + u * c.extent.x, y + size * .2f, c.centre.y + v * c.extent.y);
            if (p.x > ClearMin.x && p.x < ClearMax.x && p.y > ClearMin.y && p.y < ClearMax.y && p.z > ClearMin.z && p.z < ClearMax.z) continue;
            var q = Quaternion.AngleAxis(360f * Rk(c.seed, 183, i, 0), Vector3.up) * Quaternion.AngleAxis(90f + 20f * (Rk(c.seed, 183, i, 1) - .5f), Vector3.right);
            var shape = (byte)(Rk(c.seed, 184, i, 0) * 8f);
            FrontRoomsGlassVfx.AppendCrumb(r.crumbV, r.crumbI, p, q, size, shape, (byte)FrontRoomsGlassVfx.Kind.Crumb, new Vector2(p.x, 0f), keys.x, keys.y, r.crumbV.Count);
            r.crumbsMerged++;
        }
    }

    // ---- the merge job (two passes off the main thread) ------------------------------------------------------------------------
    internal sealed class MergeJob
    {
        NativeArray<FrontRoomsGlassFractureMeshes.Vertex> srcV, crumbV;
        NativeArray<int> srcI, crumbI, remap0, remap1, counts, stats;
        NativeArray<FrontRoomsGlassDebrisJobs.MergeItem> items;
        NativeArray<byte> keep;
        NativeArray<Vector3> bounds, frameBoxes;
        Mesh.MeshDataArray mda;
        bool mdaLive, hasLod1, compact;
        public JobHandle countHandle, writeHandle;
        public int dropped, clearVerts, tail, frameVerts;

        bool ownsSource;

        // fix r1 (eng #11): the arrays are allocated, then filled in slices (they were filled element by element on one frame: the
        // "count" step cost 2-3 ms); the count job is scheduled once they are full.
        int prepPhase, prepCursor;

        public bool PrepareSlice(Run r, double deadline)
        {
            if (prepPhase == 0)
            {
                Dispose();
                if (r.nSrcV.IsCreated && r.nSrcV.Length == r.srcV.Count && r.nSrcI.IsCreated && r.nSrcI.Length == r.srcI.Count)
                {
                    srcV = r.nSrcV;
                    srcI = r.nSrcI;
                    ownsSource = false;
                }
                else
                {
                    // tempered glass builds its source by hand: copy it once
                    srcV = new NativeArray<FrontRoomsGlassFractureMeshes.Vertex>(r.srcV.Count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                    srcI = new NativeArray<int>(r.srcI.Count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                    ownsSource = true;
                }
                items = new NativeArray<FrontRoomsGlassDebrisJobs.MergeItem>(r.items.Count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                crumbV = new NativeArray<FrontRoomsGlassFractureMeshes.Vertex>(r.crumbV.Count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                crumbI = new NativeArray<int>(r.crumbI.Count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                remap0 = new NativeArray<int>(Math.Max(1, srcV.Length), Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                remap1 = new NativeArray<int>(Math.Max(1, srcV.Length), Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                keep = new NativeArray<byte>(Math.Max(1, srcI.Length / 3), Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                counts = new NativeArray<int>(5, Allocator.Persistent);
                stats = new NativeArray<int>(3, Allocator.Persistent);
                bounds = new NativeArray<Vector3>(4, Allocator.Persistent);
                frameBoxes = new NativeArray<Vector3>(r.frameBoxes.Count, Allocator.Persistent);
                for (var i = 0; i < r.frameBoxes.Count; i++) frameBoxes[i] = r.frameBoxes[i];
                prepPhase = ownsSource ? 1 : 3;
                prepCursor = 0;
                if (ClockMs() >= deadline) return false;
            }
            while (prepPhase < 6)
            {
                int n;
                switch (prepPhase)
                {
                    case 1: n = r.srcV.Count; for (var e = Math.Min(n, prepCursor + 1024); prepCursor < e; prepCursor++) srcV[prepCursor] = r.srcV[prepCursor]; break;
                    case 2: n = r.srcI.Count; for (var e = Math.Min(n, prepCursor + 4096); prepCursor < e; prepCursor++) srcI[prepCursor] = r.srcI[prepCursor]; break;
                    case 3: n = r.items.Count; for (var e = Math.Min(n, prepCursor + 256); prepCursor < e; prepCursor++) items[prepCursor] = r.items[prepCursor]; break;
                    case 4: n = r.crumbV.Count; for (var e = Math.Min(n, prepCursor + 1024); prepCursor < e; prepCursor++) crumbV[prepCursor] = r.crumbV[prepCursor]; break;
                    default: n = r.crumbI.Count; for (var e = Math.Min(n, prepCursor + 4096); prepCursor < e; prepCursor++) crumbI[prepCursor] = r.crumbI[prepCursor]; break;
                }
                if (prepCursor >= n) { prepPhase++; prepCursor = 0; }
                if (prepPhase < 6 && ClockMs() >= deadline) return false;
            }
            return true;
        }

        public void ScheduleCount()
        {
            prepPhase = 0;
            countHandle = new FrontRoomsGlassDebrisJobs.MergeCountJob { srcV = srcV, srcI = srcI, items = items, remap0 = remap0, remap1 = remap1, keep = keep, counts = counts }.Schedule();
            JobHandle.ScheduleBatchedJobs();
        }

        public void ScheduleWrite(Run r, bool lod1)
        {
            countHandle.Complete();
            hasLod1 = lod1;
            var nv0 = counts[0] + crumbV.Length;
            var ni0 = counts[1] + crumbI.Length;
            var nv1 = counts[2];
            var ni1 = counts[3];
            dropped = counts[4];
            mda = Mesh.AllocateWritableMeshData(hasLod1 ? 2 : 1);
            mdaLive = true;
            compact = FrontRoomsGlassDebrisJobs.CompactFloorVertex;
            try
            {
                mda[0].SetVertexBufferParams(nv0, FrontRoomsGlassDebrisJobs.Layout(compact));
                if (hasLod1) mda[1].SetVertexBufferParams(nv1, FrontRoomsGlassDebrisJobs.Layout(compact));
            }
            catch (Exception e)
            {
                // a platform without the compact formats: the full 52-byte layout
                Debug.LogWarning("[GD3c debris] compact floor vertex unavailable (" + e.Message + "): using the 52-byte layout");
                FrontRoomsGlassDebrisJobs.CompactFloorVertex = compact = false;
                mda[0].SetVertexBufferParams(nv0, FrontRoomsGlassDebrisJobs.Layout(false));
                if (hasLod1) mda[1].SetVertexBufferParams(nv1, FrontRoomsGlassDebrisJobs.Layout(false));
            }
            var wide0 = nv0 > 65000;
            var wide1 = nv1 > 65000;
            mda[0].SetIndexBufferParams(ni0, wide0 ? IndexFormat.UInt32 : IndexFormat.UInt16);
            if (hasLod1) mda[1].SetIndexBufferParams(ni1, wide1 ? IndexFormat.UInt32 : IndexFormat.UInt16);
            var job = new FrontRoomsGlassDebrisJobs.MergeWriteJob
            {
                srcV = srcV, srcI = srcI, items = items, remap0 = remap0, remap1 = remap1, keep = keep, crumbV = crumbV, crumbI = crumbI,
                lod0 = mda[0], hasLod1 = hasLod1, compact = compact, wide0 = wide0, wide1 = wide1, pieceVerts0 = counts[0],
                clearMin = ClearMin, clearMax = ClearMax, bounds = bounds, stats = stats, frameBoxes = frameBoxes,
            };
            if (hasLod1) job.lod1 = mda[1];
            writeHandle = job.Schedule();
            JobHandle.ScheduleBatchedJobs();
        }

        public void Apply(Run r, Mesh lod0, Mesh lod1)
        {
            writeHandle.Complete();
            clearVerts = stats[0];
            tail = stats[1];
            frameVerts = stats[2];
            const MeshUpdateFlags flags = MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers;
            lod0.Clear();
            if (hasLod1) { lod1.Clear(); Mesh.ApplyAndDisposeWritableMeshData(mda, new[] { lod0, lod1 }, flags); }
            else Mesh.ApplyAndDisposeWritableMeshData(mda, lod0, flags);
            mdaLive = false;
            lod0.bounds = new Bounds((bounds[0] + bounds[1]) * .5f, bounds[1] - bounds[0]);
            if (hasLod1) lod1.bounds = new Bounds((bounds[2] + bounds[3]) * .5f, bounds[3] - bounds[2]);
            DisposeArrays();
        }

        void DisposeArrays()
        {
            if (ownsSource) { if (srcV.IsCreated) srcV.Dispose(); if (srcI.IsCreated) srcI.Dispose(); }
            srcV = default;
            srcI = default;
            ownsSource = false;
            if (items.IsCreated) items.Dispose();
            if (crumbV.IsCreated) crumbV.Dispose();
            if (crumbI.IsCreated) crumbI.Dispose();
            if (remap0.IsCreated) remap0.Dispose();
            if (remap1.IsCreated) remap1.Dispose();
            if (keep.IsCreated) keep.Dispose();
            if (counts.IsCreated) counts.Dispose();
            if (stats.IsCreated) stats.Dispose();
            if (bounds.IsCreated) bounds.Dispose();
            if (frameBoxes.IsCreated) frameBoxes.Dispose();
        }

        public void Dispose()
        {
            prepPhase = 0;
            countHandle.Complete();
            writeHandle.Complete();
            if (mdaLive) { mda.Dispose(); mdaLive = false; }
            DisposeArrays();
        }
    }

    // ---- the record ------------------------------------------------------------------------------------------------------------
    void WriteRecord(Run r)
    {
        var shards = new List<FrontRoomsGlassDebrisPack.Shard>(r.logs.Count);
        var w2r = r.root.worldToLocalMatrix;
        var invRoot = Quaternion.Inverse(r.root.rotation);
        for (var s = 0; s < r.logs.Count; s++)
        {
            var log = r.logs[s];
            Vector3 pos;
            Quaternion rot;
            if (r.reduced) FrontRoomsGlassDebrisLite.PoseOf(r, s, out pos, out rot);
            else
            {
                var t = r.shardObj[s].transform;
                pos = w2r.MultiplyPoint3x4(t.TransformPoint(StageCentre(r, log)));
                rot = invRoot * t.rotation;
            }
            log.finalRoot = pos;
            // art r1 #2 census: shards left more than UprightDegrees off flat (only those leaning on the wall may be)
            if (Mathf.Acos(Mathf.Clamp01(Mathf.Abs((rot * Vector3.forward).y))) * Mathf.Rad2Deg > UprightDegrees) r.uprightLeft++;
            byte flags = 0;
            if (Mathf.Sign(pos.z) == r.farSign) flags |= FrontRoomsGlassDebrisPack.ShardFar;
            if ((rot * Vector3.forward).y < 0f) flags |= FrontRoomsGlassDebrisPack.ShardUpsideDown;
            if (pos.y > .2f && Mathf.Abs(pos.x) < .75f && Mathf.Abs(pos.z) < .12f) flags |= FrontRoomsGlassDebrisPack.ShardOnSill;
            if (log.dropped) flags |= FrontRoomsGlassDebrisPack.ShardDropped;
            shards.Add(new FrontRoomsGlassDebrisPack.Shard { piece = (byte)Mathf.Min(255, log.piece), flags = flags, position = pos, rotation = rot });
        }
        var clusters = r.clustersOut ?? Clusters(r.crumbs, r.f.seed);
        var rec = r.glass.Record;
        var h = new FrontRoomsGlassDebrisPack.Header
        {
            edge = r.edge, seed = rec.seed, impact = rec.impact, rotation = rec.rotation, side = rec.side,
            tier = r.reduced ? FrontRoomsGlassDebrisPack.TierReduced : FrontRoomsGlassDebrisPack.TierDesktop,
            floorY = r.floorY, breakTime = Time.time, envHash = r.envHashSet ? r.envHash : EnvHash(r), teethSnapped = r.floor.SnappedMask,
        };
        var data = FrontRoomsGlassDebrisPack.Pack(h, shards, clusters);
        r.recordBytes = data.Length;
        r.recordWritten = true;
        var e = FrontRoomsGlassDebris.Put(r.map, r.edge, data);
        r.floor.BrokenOrder = e != null ? e.brokenOrder : ++brokenCounter;
    }

    // The resting crumbs as at most 8 clusters (floor cells of 0.6 m, the sill), each scattered again from its seed on return.
    static List<FrontRoomsGlassDebrisPack.Cluster> Clusters(List<FrontRoomsGlassDebris.RestingCrumb> crumbs, uint seed)
    {
        var cells = new Dictionary<long, List<Vector3>>();
        foreach (var c in crumbs)
        {
            var level = c.position.y > .2f ? 1 : 0;
            var key = ((long)level << 40) ^ ((long)Mathf.FloorToInt(c.position.x / .6f) << 20) ^ (long)(Mathf.FloorToInt(c.position.z / .6f) & 0xFFFFF);
            if (!cells.TryGetValue(key, out var l)) cells[key] = l = new List<Vector3>();
            l.Add(c.position);
        }
        var list = new List<KeyValuePair<long, List<Vector3>>>(cells);
        list.Sort((a, b) => b.Value.Count.CompareTo(a.Value.Count));
        // every crumb counts: the cells past the 8 largest join the nearest kept cell of their level (its extent grows)
        for (var i = list.Count - 1; i >= FrontRoomsGlassDebrisPack.MaxClusters; i--)
        {
            var pts = list[i].Value;
            var lvl = pts[0].y > .2f;
            var best = -1;
            var bestD = float.MaxValue;
            for (var j = 0; j < FrontRoomsGlassDebrisPack.MaxClusters; j++)
            {
                if ((list[j].Value[0].y > .2f) != lvl) continue;
                var d = (list[j].Value[0] - pts[0]).sqrMagnitude;
                if (d < bestD) { bestD = d; best = j; }
            }
            if (best < 0) best = 0;
            list[best].Value.AddRange(pts);
            list.RemoveAt(i);
        }
        var outList = new List<FrontRoomsGlassDebrisPack.Cluster>();
        for (var i = 0; i < list.Count && outList.Count < FrontRoomsGlassDebrisPack.MaxClusters; i++)
        {
            var pts = list[i].Value;
            var mn = new Vector2(float.MaxValue, float.MaxValue);
            var mx = -mn;
            foreach (var p in pts) { mn = Vector2.Min(mn, new Vector2(p.x, p.z)); mx = Vector2.Max(mx, new Vector2(p.x, p.z)); }
            outList.Add(new FrontRoomsGlassDebrisPack.Cluster
            {
                centre = (mn + mx) * .5f, extent = Vector2.Max((mx - mn) * .5f, new Vector2(.01f, .01f)), count = (ushort)Mathf.Min(65535, pts.Count),
                seed = (ushort)((seed + (uint)i * 7919u) & 0x3FFF), level = (byte)(pts[0].y > .2f ? 1 : 0),
            });
        }
        return outList;
    }

    static readonly Collider[] envHits = new Collider[256];

    // The neighbourhood of the window when its glass settled: the colliders of the map within 2.5 m (order-free hash of their
    // quantised bounds). A rebuilt chunk that came back the same gives the same hash; a shifted one usually does not.
    static uint EnvHash(Run r)
    {
        var root = r.root;
        var n = UnityEngine.Physics.OverlapBoxNonAlloc(root.TransformPoint(new Vector3(0f, 1.2f, 0f)), new Vector3(2.5f, 1.2f, 2.5f), envHits, root.rotation, ~0, QueryTriggerInteraction.Ignore);
        uint h = 0;
        var count = 0;
        var mapT = r.map != null ? r.map.transform : null;
        for (var i = 0; i < n; i++)
        {
            var c = envHits[i];
            if (c == null || c is CharacterController || (c.attachedRigidbody != null && !c.attachedRigidbody.isKinematic)) continue;
            if (mapT != null && !c.transform.IsChildOf(mapT)) continue;
            if (c.transform.IsChildOf(root)) continue;
            var b = c.bounds;
            var lc = b.center - root.position;
            var k = (uint)(Mathf.RoundToInt(lc.x * 100f) * 73856093) ^ (uint)(Mathf.RoundToInt(lc.y * 100f) * 19349663) ^ (uint)(Mathf.RoundToInt(lc.z * 100f) * 83492791)
                ^ (uint)(Mathf.RoundToInt(b.size.x * 100f) * 2654435761u) ^ (uint)(Mathf.RoundToInt(b.size.y * 100f) * 40503) ^ (uint)(Mathf.RoundToInt(b.size.z * 100f) * 2246822519u);
            h += k * 2654435761u + 0x9E3779B9u;
            count++;
        }
        return h ^ ((uint)count * 0x85EBCA6Bu);
    }

    // glass footsteps: the floor cells under every shard and crumb lying within FloorBand of the floor (world XZ)
    // glass footsteps: the floor cells under every shard and crumb lying within FloorBand of the floor (world XZ); in slices (fix r1):
    // the cursor walks the items, then the crumbs (every 8th vertex)
    static bool MarkCellsSlice(Run r, double deadline)
    {
        var cells = r.floor.Cells;
        if (r.cursor == 0) cells.Clear();
        var l2w = r.root.localToWorldMatrix;
        var ni = r.items.Count;
        while (r.cursor < ni)
        {
            var it = r.items[r.cursor++];
            if (it.lod1 == 0 && !r.reduced) continue;
            if (r.reduced && it.tail != 0) continue;
            // a dozen of the piece's vertices give its bounds on the floor (cheap and conservative)
            var b = PieceBoundsRoot(r, it);
            if (b.min.y <= FrontRoomsGlassDebris.FloorBand)
                for (var x = b.min.x; x <= b.max.x + FrontRoomsGlassDebris.CellSize; x += FrontRoomsGlassDebris.CellSize)
                    for (var z = b.min.z; z <= b.max.z + FrontRoomsGlassDebris.CellSize; z += FrontRoomsGlassDebris.CellSize)
                    {
                        var w = l2w.MultiplyPoint3x4(new Vector3(Mathf.Min(x, b.max.x), 0f, Mathf.Min(z, b.max.z)));
                        cells.Add(FrontRoomsGlassDebris.CellKey(w.x, w.z));
                    }
            if ((r.cursor & 7) == 0 && ClockMs() >= deadline) return false;
        }
        var nc = r.crumbV.Count;
        while (true)
        {
            var i = (r.cursor - ni) * 8;
            if (i >= nc) return true;
            r.cursor++;
            var p = r.crumbV[i].position;
            if (p.y <= FrontRoomsGlassDebris.FloorBand)
            {
                var w = l2w.MultiplyPoint3x4(p);
                cells.Add(FrontRoomsGlassDebris.CellKey(w.x, w.z));
            }
            if (((r.cursor - ni) & 63) == 0 && ClockMs() >= deadline) return false;
        }
    }

    static Bounds PieceBoundsRoot(Run r, FrontRoomsGlassDebrisJobs.MergeItem it)
    {
        var mn = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        var mx = -mn;
        var step = Math.Max(1, it.vn / 12);
        for (var v = it.v0; v < it.v0 + it.vn; v += step)
        {
            var p = it.m.MultiplyPoint3x4(r.srcV[v].position);
            mn = Vector3.Min(mn, p); mx = Vector3.Max(mx, p);
        }
        return new Bounds((mn + mx) * .5f, mx - mn);
    }

    /// <summary>The merge's worst time per step (ms), in MergeStepNames order: clear, items, crumbs, count, write, apply, clusters, record,
    /// cells, swap, shards, proxies, non-readable, done.</summary>
    public static string MergeSteps(Run r)
    {
        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < r.mergeStepWorst.Length; i++) sb.Append(i == 0 ? "" : " ").Append(MergeStepNames[i]).Append(' ').Append(r.mergeStepWorst[i].ToString("0.00"));
        return sb.ToString();
    }

    internal static Vector3 StageCentre(Run r, ShardLog log)
    {
        var pc = r.f.pieces[log.piece];
        var p = pc.kind == GlassPieceKind.Shard ? FrontRoomsGlassFractureMeshes.StagePoint(r.f, log.piece, new FrontRoomsGlassFractureMeshes.Vector3d(pc.centroid.x, pc.centroid.y, 0)) : new FrontRoomsGlassFractureMeshes.Vector3d(pc.centroid.x, pc.centroid.y, 0);
        return new Vector3((float)p.x, (float)p.y, (float)p.z);
    }

    // ---- the return (WindowBuilt at stage 3: the breakable regenerated the pattern) -----------------------------------------
    void StartRestore(FrontRoomsGlassBreakable glass)
    {
        if (RunOfHere(glass) != null || glass.Fracture == null || glass.Meshes == null) return;
        var root = glass.transform;
        var r = new Run
        {
            glass = glass, root = root, map = glass.Map, edge = glass.Window != null ? glass.Window.edge : 0, f = glass.Fracture,
            phase = Phase.Building, profileTier = glass.Fracture.tier, farSign = glass.Fracture.farZSign >= 0 ? 1 : -1,
            floorY = root.position.y, buildStartFrame = Time.frameCount, restore = true, reduced = FrontRoomsGlassTypeProfile.ReducedTier,
        };
        r.floor = FrontRoomsGlassFloorGlass.On(root);
        r.block = new MaterialPropertyBlock();
        var src = glass.Stage2Renderer != null ? glass.Stage2Renderer : glass.SlabRenderer;
        if (src != null) src.GetPropertyBlock(r.block);
        r.block.SetFloat(BowAmpId, 0f);
        r.block.SetFloat(CrushId, 0f);
        NoWipe(r.block);
        r.blockNoBow = r.block;
        var e = FrontRoomsGlassDebris.KeepFloorRecords ? FrontRoomsGlassDebris.Find(r.map, r.edge) : null;
        r.hasRecord = e != null && FrontRoomsGlassDebrisPack.Unpack(e.data, out r.recHeader, r.recShards, r.recClusters);
        if (r.hasRecord)
        {
            var rec = glass.Record;
            var same = r.recHeader.seed == rec.seed && Mathf.Abs(r.recHeader.impact.x - rec.impact.x) < .003f && Mathf.Abs(r.recHeader.impact.y - rec.impact.y) < .003f
                && Mathf.Abs(Mathf.DeltaAngle(r.recHeader.rotation, rec.rotation)) < .6f && Mathf.Sign(r.recHeader.side) == Mathf.Sign(rec.side)
                && r.recHeader.tier == (r.reduced ? FrontRoomsGlassDebrisPack.TierReduced : FrontRoomsGlassDebrisPack.TierDesktop);
            if (!same)
            {
                r.restoreNote = "record does not match the map's break: teeth only";
                FrontRoomsGlassDebris.Remove(r.map, r.edge);
                r.hasRecord = false;
                r.recShards.Clear(); r.recClusters.Clear();
            }
            else
            {
                FrontRoomsGlassDebris.MarkLive(r.map, r.edge);
                r.floor.BrokenOrder = e.brokenOrder;
            }
        }
        else r.restoreNote = FrontRoomsGlassDebris.KeepFloorRecords ? "no record (evicted or never settled): teeth only" : "KeepFloorRecords off: teeth only";
        runs.Add(r);
    }

    // Restore steps: 0 read the regenerated stage-2 mesh; 1 the teeth (shown at once); 2 neighbourhood check; then the merge.
    void RestoreSlice(Run r, double budgetMs)
    {
        var t0 = ClockMs();
        var guard = 0;
        var wait = false;
        while (r.phase == Phase.Building && ClockMs() - t0 < budgetMs && guard++ < 8 && !wait)
        {
            var step = r.buildStep;
            var s0 = ClockMs();
            switch (r.buildStep)
            {
                case 0: ReadSource(r); r.buildStep++; break;
                case 1:
                    Teeth(r, r.hasRecord ? r.recHeader.teethSnapped : 0UL);
                    r.floor.ApplyBlock(r.block);
                    r.floor.ShowTeeth(!r.reduced || !r.hasRecord);
                    r.buildStep++;
                    break;
                case 2:
                    // fix r1 (eng #3): the neighbourhood check waits for the physics step that syncs the rebuilt chunk's colliders
                    // (it called Physics.SyncTransforms on the rebuild frame: 15-20 ms CPU after a teleport, E1 run)
                    if (r.hasRecord && Time.frameCount - r.buildStartFrame < NeighbourhoodWaitFrames) { wait = true; break; }
                    if (r.hasRecord) CheckNeighbourhood(r);
                    r.buildStep++;
                    break;
                default:
                    if (!r.hasRecord || r.recShards.Count == 0 && r.recClusters.Count == 0)
                    {
                        // teeth only
                        r.DisposeSource();
                        r.phase = Phase.Done;
                        r.mergedFrame = Time.frameCount;
                        r.floor.MakeNonReadable();
                        r.floor.FreeTeethSource();
                        try { r.glass.ReleaseWorkingSet(); } catch (Exception e) { Debug.LogException(e); }
                        Trace?.Invoke("debris: returned with teeth only (" + r.restoreNote + ")");
                        break;
                    }
                    if (r.restoreNote.Length == 0) r.restoreNote = r.recShards.Count + " shards, " + r.recClusters.Count + " crumb clusters" + (r.droppedByEnv > 0 ? ", " + r.droppedByEnv + " dropped (neighbourhood changed)" : "");
                    r.phase = Phase.Merging;
                    r.mergeStep = 0;
                    break;
            }
            r.restoreStepWorst[Mathf.Min(step, r.restoreStepWorst.Length - 1)] = Math.Max(r.restoreStepWorst[Mathf.Min(step, r.restoreStepWorst.Length - 1)], ClockMs() - s0);
        }
        var ms = ClockMs() - t0;
        r.restoreCpuMs += ms;
        r.restoreWorstMs = Math.Max(r.restoreWorstMs, ms);
    }

    // After a shift the chunk may come back with new props: a shard that would sit inside one of them is dropped (teeth stay).
    public static int NeighbourhoodWaitFrames = 3;

    void CheckNeighbourhood(Run r)
    {
        if (EnvHash(r) == r.recHeader.envHash) return;
        var c0 = new Dictionary<int, int>(r.rangePiece.Count);
        for (var k = 0; k < r.rangePiece.Count; k++) c0[r.rangePiece[k]] = k;
        var mapT = r.map != null ? r.map.transform : null;
        for (var i = r.recShards.Count - 1; i >= 0; i--)
        {
            var s = r.recShards[i];
            if (!c0.ContainsKey(s.piece)) { r.recShards.RemoveAt(i); continue; }
            var pc = r.f.pieces[s.piece];
            var half = new Vector3(Mathf.Max(.005f, (float)Math.Sqrt(pc.area) * .35f), Mathf.Max(.005f, (float)Math.Sqrt(pc.area) * .35f), .001f);
            var centre = r.root.TransformPoint(s.position);
            var rot = r.root.rotation * s.rotation;
            var n = UnityEngine.Physics.OverlapBoxNonAlloc(centre, half, envHits, rot, ~0, QueryTriggerInteraction.Ignore);
            var blocked = false;
            for (var h = 0; h < n && !blocked; h++)
            {
                var c = envHits[h];
                if (c == null || c is CharacterController || c.transform.IsChildOf(r.root)) continue;
                if (mapT != null && !c.transform.IsChildOf(mapT)) continue;
                blocked = true;
            }
            if (blocked) { r.recShards.RemoveAt(i); r.droppedByEnv++; }
        }
    }

    // ---- the live-memory cap (gd3c sec 6.2) ------------------------------------------------------------------------------------
    void EnforceLiveCap()
    {
        long total = 0;
        foreach (var f in FrontRoomsGlassDebris.liveFloors) if (f != null) total += f.Bytes();
        var cap = FrontRoomsGlassTypeProfile.ReducedTier ? LiveCapBytesReduced : LiveCapBytes;
        if (total <= cap) return;
        Eye(out var eye);
        while (total > cap)
        {
            FrontRoomsGlassFloorGlass oldest = null;
            foreach (var f in FrontRoomsGlassDebris.liveFloors)
            {
                if (f == null || f.Lod1Only || !f.HasFloor || f.FloorLod1Tris == 0) continue;
                if ((f.transform.position - eye).sqrMagnitude < CapFarMetres * CapFarMetres) continue;
                if (oldest == null || f.BrokenOrder < oldest.BrokenOrder) oldest = f;
            }
            if (oldest == null) break;
            var before = oldest.Bytes();
            oldest.DropToLod1Only();
            total -= before - oldest.Bytes();
            Trace?.Invoke("debris: live floor glass over " + (cap >> 20) + " MB: " + oldest.name + " keeps LOD1 only");
        }
    }

    static void ReturnShard(FrontRoomsGlassShard sh)
    {
        if (sh == null) return;
        sh.Renderer.enabled = false;
        sh.Body.isKinematic = true;
        sh.Filter.sharedMesh = null;
        sh.Hull.sharedMesh = null;
        sh.Hull.excludeLayers = 0;
        sh.Slot = -1;
        sh.Run = null;
        sh.gameObject.SetActive(false);
        shardPool.Push(sh);
    }

    void ReleaseShards(Run r)
    {
        foreach (var sh in r.shardObj) ReturnShard(sh);
        r.shardObj.Clear();
        foreach (var m in r.shardMesh) GiveMesh(m);
        foreach (var m in r.hullMesh) GiveMesh(m);
        r.shardMesh.Clear();
        r.hullMesh.Clear();
        r.renderersByRange = new Renderer[0];
    }

    bool ReleaseProxiesSlice(Run r, int perSlice)
    {
        var n = 0;
        while (r.proxies.Count > 0 && n++ < perSlice)
        {
            var go = r.proxies[r.proxies.Count - 1];
            r.proxies.RemoveAt(r.proxies.Count - 1);
            if (go == null) continue;
            if (go.TryGetComponent<MeshCollider>(out var mc) && mc.sharedMesh != null && mc.sharedMesh.name == "GD3 proxy crop") GiveMesh(mc.sharedMesh);
            Destroy(go);
        }
        while (r.toothHulls.Count > 0 && n++ < perSlice)
        {
            var mc = r.toothHulls[r.toothHulls.Count - 1];
            r.toothHulls.RemoveAt(r.toothHulls.Count - 1);
            if (mc == null) continue;
            GiveMesh(mc.sharedMesh);
            mc.sharedMesh = null;
            mc.gameObject.SetActive(false);
            hullPool.Push(mc);
        }
        if (r.proxies.Count > 0 || r.toothHulls.Count > 0) return false;
        foreach (var m in r.toothHullMesh) if (m != null && m.vertexCount > 0) GiveMesh(m);
        r.toothHullMesh.Clear();
        r.glassColliders.Clear();
        r.proxyQuery = null;
        return true;
    }

    void Abort(Run r, string why, bool keepGroup = false)
    {
        if (r.baking) { r.bakeHandle.Complete(); r.baking = false; }
        if (r.bakeIds.IsCreated) r.bakeIds.Dispose();
        r.job?.Dispose();
        r.DisposeSource();
        ReleaseShards(r);
        while (!ReleaseProxiesSlice(r, 1 << 20)) { }
        if (r.lite != null) FrontRoomsGlassDebrisLite.Release(r);
        if (r.group != null && !keepGroup) vfx.Release(r.group);
        r.phase = Phase.Aborted;
        Trace?.Invoke("debris: aborted (" + why + ")");
    }

    void OnDestroy()
    {
        foreach (var r in runs) if (r.phase != Phase.Done && r.phase != Phase.Aborted) Abort(r, "the debris world went");
        runs.Clear();
    }

    /// <summary>The lab between takes: drop every run, shard, particle and proxy (the map is about to go).</summary>
    public void ResetAll()
    {
        foreach (var r in runs) if (r.phase != Phase.Done) Abort(r, "reset");
        runs.Clear();
        pendingArm.Clear();
        vfx.Clear();
    }

    /// <summary>One line for logs.</summary>
    public static string StateLine(FrontRoomsGlassBreakable glass)
    {
        var r = RunOf(glass);
        if (r == null) return "debris: none";
        var awake = 0;
        for (var s = 0; s < r.shardObj.Count; s++) if (r.released.Count > s && r.released[s] && !r.frozen[s]) awake++;
        if (r.lite != null) awake = r.lite.Awake;
        var g = r.group;
        var census = instance != null && g != null ? instance.vfx.Census(g) : default;
        return "debris " + r.phase + (r.restore ? " (restore)" : "") + (r.reduced ? " (reduced)" : "") + " · t " + r.t.ToString("0.000") + " s · shards " + r.ShardCount + " (released " + r.Released + ", awake " + awake
            + ", frozen " + r.Frozen + ") · particles moving " + census.moving + ", resting floor " + census.floor + " / sill " + census.sill;
    }
}
