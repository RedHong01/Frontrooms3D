// FrontRoomsGlassBreakable.cs -- GD3 steps 4-5 (plan 10 sec 2.3-2.5, 4.3, 4.4; build/04_stages.md): the visible window glass
// and its breakage stages S0-S2 on the map's window root `Window {a}-{b}`.
//
// The map adds this component to every window root when the type exists (by name, FrontRoomsMapWorld.GlassBreakableType,
// before WindowBuilt) and then hides its own pane cube (the collider stays: the E ray and windowByCollider key on it). The
// window kit then skips its interim slab. From here on this component owns everything visible of the glass:
//   S0  WindowBuilt / E-down: the intact 6 mm slab (FrontRooms/Glass, _PaneFromUV). From GlassHoldStarted the crack pattern
//       is generated in time slices around the TRUE impact (FrontRoomsGlassFracturePattern, the map's GlassBreakRecord:
//       seed, impact, rotation, side), then the stage meshes are built in slices (FrontRoomsGlassFractureMeshes); both are
//       done well before the Crack1 frame (a beat that arrives first finishes them at once). While E is held the pane bows
//       (an analytic normal, up to 3 mm toward the far side, loaded by each strike) and a palm print fades in.
//   S1  GlassCracked(window, 1), ON THAT FRAME (the sound chat's Crack fires from the same event): the fins + crushed crater
//       mesh switches on inside the still-intact slab, the slab takes the crushed bruise. Nothing grows until the next beat.
//   S2  GlassCracked(window, 2), on that frame: the slab and S1 switch off and the ONE combined stage-2 mesh switches on
//       (every piece at its stage-2 pose, the crushed core gone). Same material, same pane metres in UV0: grime and smudges
//       do not move (V7).
//   S3  WindowShattered: the stage-2 mesh goes on that frame (no ghost pane). The debris world (plan step 6) takes over through
//       ShatterHandler; with none registered the glass is simply gone, as today.
// Each beat holds the glass still (its bow) for FrontRoomsShotTimings.GlassBreak.ObjectStop (33 / 40 / 50 ms); the camera
// never stops. WindowBuilt with a record at stage 1 or 2 restores that stage at once (cracks do not heal); stage 3 shows
// nothing yet (teeth and floor glass are plan steps 6 and 9). WindowReleased frees the meshes.
//
// Red's four decisions (plan sec 6) stay switchable: GlassType (decision 1, default Annealed6; Tempered6 shows no crack
// stages), BuildAtPress (decision 4; false = always the nearest of the 18 baked variants). Decisions 2-3 are the map
// chat's shot rig. The WebGL tier (fewer pieces, no bevel) comes only through FrontRoomsGlassTypeProfile.ForTier's
// #if UNITY_WEBGL gate; nothing here lowers desktop.
// G14 P4 (plan sec 4.7): StageChanged fires on every swap frame (switch the TLAS instances there); MeshesReady fires once the
// meshes exist (before the beats: build BLASes then, never on a beat frame); Meshes.BuildPieceSource gives the per-piece
// closed-slab ranges for FrontRoomsGlassRT.PrepareMeshes; pieces only ever move by transforms (no vertex animation).
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

/// <summary>Plan step 6 (the debris world) registers one: it receives the shatter on its frame, with the pattern and the
/// stage-2 mesh still at their poses. Return true when it took the glass over.</summary>
public interface IFrontRoomsGlassShatterHandler
{
    bool Shatter(FrontRoomsGlassBreakable glass, Vector3 impactWorld, Vector3 impulse);
}

[DisallowMultipleComponent]
public sealed class FrontRoomsGlassBreakable : MonoBehaviour
{
    // ---- switches (plan sec 6; defaults = the recommended ones) -----------------------------------------------------------
    /// <summary>Decision 1: the glass type (FrontRoomsGlassProfileData names). Annealed6 = staged cracks and teeth.</summary>
    public static string GlassType = FrontRoomsGlassProfileData.Annealed6;
    /// <summary>The desktop Cinematic tier (up to 170 pieces) instead of High (150).</summary>
    public static bool Cinematic;
    /// <summary>Decision 4: true = built at E-press round the true impact; false = the nearest of the 18 baked variants.</summary>
    public static bool BuildAtPress = true;
    /// <summary>CPU per frame for the sliced work (ms): the pattern, then the meshes. Doubled when fewer than 8 frames are
    /// left before the Crack1 beat and quadrupled under 4 (a slow machine spreads the rest over those frames instead of
    /// finishing it all on the beat frame).</summary>
    public static double GenerateBudgetMs = 2.0, MeshBudgetMs = 1.0;
    /// <summary>Peak bow at the third strike (m), plan sec 2.3 ("up to 3 mm").</summary>
    public static float BowMax = .003f;
    /// <summary>Lab diagnostics (never set by the game): the rest pose at S2, no bow, no palm print.</summary>
    public static bool DebugZeroPose, DebugNoBow, DebugNoPalm;
    /// <summary>
    /// A palm print fading in at the impact (plan sec 2.3 S0, written for a push). OFF by default: Design 2 strikes with the
    /// elbow and forearm (no hand touches the glass), and the print's four finger ellipses read as floating ovals against a dark
    /// far side (build/04_stages.md sec 3.4). Turn it on for Design 1 "Shove", where the hand does touch the pane.
    /// </summary>
    public static bool PalmPrint;
    /// <summary>
    /// Warm the generator and the mesh builder once, off the main thread, when the first window is built (Mono JIT: the first
    /// break otherwise compiles ~4,000 lines on E-down: 38-55 ms CPU, measured). The warm-up's working set (generator pools,
    /// result, mesh lists: about 2 MB once grown) then becomes the first spare that the next struck window takes, so its first
    /// break does not grow fresh pools on the main thread (build/04_stages.md sec 5). IL2CPP players (WebGL, iOS): no JIT,
    /// the spare is still made.
    /// </summary>
    public static bool WarmUpOnBuild = true;
    /// <summary>The warm-up's result for logs: "", "running", "done 41.2 ms (thread)", "skipped (...)", "failed: ...".</summary>
    public static string WarmUpState { get; private set; } = "";
    /// <summary>The clock for this component's CPU accounting (ms; wall time by default, the lab passes the thread's CPU time).
    /// The pattern's and the meshes' own budgets use FrontRoomsGlassFractureJob.ClockMs / FrontRoomsGlassFractureMeshes.ClockMs.</summary>
    public static Func<double> ClockMs = () => Stopwatch.GetTimestamp() * 1000.0 / Stopwatch.Frequency;
    /// <summary>Optional trace (the lab's event log).</summary>
    public static Action<FrontRoomsGlassBreakable, string> Trace;

    // ---- hooks ---------------------------------------------------------------------------------------------------------
    /// <summary>On the frame a stage is shown: (glass, from, to). G14 P4 swaps its instances here.</summary>
    public static event Action<FrontRoomsGlassBreakable, int, int> StageChanged;
    /// <summary>The pattern and every stage mesh exist (before the first beat that needs them).</summary>
    public static event Action<FrontRoomsGlassBreakable> MeshesReady;
    /// <summary>Plan step 6.</summary>
    public static IFrontRoomsGlassShatterHandler ShatterHandler;

    public const string SlabName = "GD3 glass S0 slab", Stage1Name = "GD3 glass S1 fins + crater", Stage2Name = "GD3 glass S2 pieces";
    const float SlabY = (float)FrontRoomsGlassFracturePattern.SlabCentreY;
    const float SlabW = (float)FrontRoomsGlassFracturePattern.SlabW, SlabH = (float)FrontRoomsGlassFracturePattern.SlabH;
    const float HeldHalfX = (float)FrontRoomsGlassFracturePattern.StopX, HeldHalfY = (float)((FrontRoomsGlassFracturePattern.StopTop - FrontRoomsGlassFracturePattern.StopBot) * .5);

    static readonly int ImpactUVId = Shader.PropertyToID("_ImpactUV"), PalmId = Shader.PropertyToID("_Palm"), CrushId = Shader.PropertyToID("_Crush"),
        CrushRadiusId = Shader.PropertyToID("_CrushRadius"), BowCentreId = Shader.PropertyToID("_BowCentre"), BowAmpId = Shader.PropertyToID("_BowAmp");
    static Material fractureMaterial;

    // ---- state -----------------------------------------------------------------------------------------------------------
    public FrontRoomsMapWorld Map { get; private set; }
    public FrontRoomsMapWorld.Window Window { get; private set; }
    public FrontRoomsMapWorld.GlassBreakRecord Record { get; private set; }
    /// <summary>The stage on screen: 0 intact, 1 cracked, 2 spiderweb, 3 shattered.</summary>
    public int Stage { get; private set; }
    public bool Ready => meshesReady;
    public FrontRoomsGlassFracture Fracture => patternReady && ws != null ? ws.res : null;
    public FrontRoomsGlassFractureMeshes Meshes => ws?.meshes;
    public MeshRenderer SlabRenderer { get; private set; }
    public MeshRenderer Stage1Renderer { get; private set; }
    public MeshRenderer Stage2Renderer { get; private set; }
    /// <summary>How the pattern was made: "press", "bake c4 k1 (fallback: ...)", "restore".</summary>
    public string PatternSource { get; private set; } = "";
    public int FramesToReady { get; private set; } = -1;
    public double PatternCpuMs { get; private set; }
    public double MeshCpuMs { get; private set; }
    public double WorstFrameMs { get; private set; }
    public string ForcedAt { get; private set; } = "";
    public float Bow => bow;
    public float Palm => palm;
    public int Side => Record.side;

    // The working set (generator, result, mesh builder): taken only when this window is struck (an intact window holds none),
    // from the spares if one is free (warm pools), returned when the window is released or destroyed.
    sealed class Workset
    {
        public readonly FrontRoomsGlassFractureJob job = new FrontRoomsGlassFractureJob();
        public readonly FrontRoomsGlassFracture res = new FrontRoomsGlassFracture();
        public readonly FrontRoomsGlassFractureMeshes meshes = new FrontRoomsGlassFractureMeshes();
    }
    static readonly Stack<Workset> spares = new Stack<Workset>();
    static readonly object sparesLock = new object();
    /// <summary>Spares kept for reuse (a released pane's set, the warm-up's set).</summary>
    public const int MaxSpares = 2;
    public static int Spares { get { lock (sparesLock) return spares.Count; } }
    Workset ws;
    Workset W
    {
        get
        {
            if (ws != null) return ws;
            lock (sparesLock) ws = spares.Count > 0 ? spares.Pop() : null;
            TookSpare = ws != null;
            return ws ??= new Workset();
        }
    }
    FrontRoomsGlassFractureJob job => W.job;
    FrontRoomsGlassFracture res => W.res;
    FrontRoomsGlassFractureMeshes meshes => W.meshes;
    static void ReturnSpare(Workset w)
    {
        lock (sparesLock) if (spares.Count < MaxSpares) spares.Push(w);
    }
    /// <summary>Lab: whether this window's working set came warm from the spares.</summary>
    public bool TookSpare { get; private set; }
    MaterialPropertyBlock block;
    Mesh slabMesh;
    enum Work { None, Pattern, Meshes }
    Work work;
    bool patternReady, meshesReady, builtForRecord;
    int startFrame;
    float keyA, keyB;
    FrontRoomsMapWorld.GlassBreakRecord built;   // the record the pattern was made for
    // bow and palm (plan sec 2.3 S0)
    float holdTime, bow, bowResidue, residueAge, stopLeft, palm;
    bool holding;
    Vector4 appliedA, appliedB;
    bool applied;
    int lastBeat;

    // ---------------------------------------------------------------------------------------------------------------------
    void Awake()
    {
        Map = GetComponentInParent<FrontRoomsMapWorld>(true);
        if (Map == null) { Debug.LogWarning("[GD3 glass] " + name + ": no FrontRoomsMapWorld above this window root"); return; }
        Map.WindowBuilt += OnWindowBuilt;
        Map.WindowReleased += OnWindowReleased;
        Map.GlassHoldStarted += OnHoldStarted;
        Map.WindowHeld += OnWindowHeld;
        Map.GlassHoldReleased += OnHoldReleased;
        Map.GlassCracked += OnCracked;
        Map.WindowShattered += OnShattered;
    }

    void OnDestroy()
    {
        if (Map != null)
        {
            Map.WindowBuilt -= OnWindowBuilt;
            Map.WindowReleased -= OnWindowReleased;
            Map.GlassHoldStarted -= OnHoldStarted;
            Map.WindowHeld -= OnWindowHeld;
            Map.GlassHoldReleased -= OnHoldReleased;
            Map.GlassCracked -= OnCracked;
            Map.WindowShattered -= OnShattered;
        }
        FreeMeshes();
        NeedSceneColor(false);
    }

    bool Mine(FrontRoomsMapWorld.Window w) => w != null && w.root == transform;

    // The crack faces mirror the scene behind the glass (FrontRoomsGlassSceneColor): asked for from E-down (so every frame of
    // the shot is drawn the same way) while the pane is held or cracked; dropped at the shatter, a release at S0, or unload.
    bool sceneColor;
    void NeedSceneColor(bool on)
    {
        if (on == sceneColor) return;
        sceneColor = on;
        if (on) FrontRoomsGlassSceneColor.Add(transform); else FrontRoomsGlassSceneColor.Remove(transform);
    }

    // ---- map events ----------------------------------------------------------------------------------------------------
    void OnWindowBuilt(FrontRoomsMapWorld.Window w, FrontRoomsMapWorld.GlassBreakRecord record)
    {
        if (!Mine(w)) return;
        if (WarmUpOnBuild) StartWarmUp();
        Window = w;
        Record = record;
        EnsureSlab();
        if (record.stage >= 3)
        {
            // Shattered before this build: nothing of the pane is left to show here yet (teeth and floor glass: steps 6, 9).
            SlabRenderer.enabled = false;
            Stage = 3;
            NeedSceneColor(false);
            return;
        }
        Stage = 0;
        NeedSceneColor(record.stage >= 1);
        if (record.stage >= 1)
        {
            // A cracked pane rebuilt (chunk drop, shift): the same pattern from the record, shown at once. Cracks do not heal.
            StartPattern(record, "restore");
            FinishNow("restore");
            ShowStage(record.stage, false);
        }
        ApplyBlock();
    }

    void OnWindowReleased(FrontRoomsMapWorld.Window w)
    {
        if (!Mine(w)) return;
        FreeMeshes();
        NeedSceneColor(false);
        Window = null;
    }

    void OnHoldStarted(FrontRoomsMapWorld.Window w, Vector3 impactWorld, int seed)
    {
        if (!Mine(w)) return;
        Window = w;
        var r = Map.GlassBreakRecordOf(w);
        var samePattern = builtForRecord && built.seed == r.seed && built.impact == r.impact && built.rotation == r.rotation;
        Record = r;
        holding = true;
        NeedSceneColor(true);
        holdTime = w.hold * FrontRoomsShotTimings.GlassBreak.HoldSeconds;
        if (!samePattern)
        {
            // E-down at stage 0 (or a re-strike that moved the impact before any crack): a new pattern round the true impact.
            StartPattern(r, "press");
        }
        else if (built.side != r.side && Stage < 2 && patternReady)
        {
            // Struck again from the other side before the spiderweb: the pieces will lean the other way (same pattern).
            res.side = r.side;
            res.farZSign = r.side > 0 ? 1 : -1;
            built.side = r.side;
            StartMeshes("re-strike from the other side");
        }
        ApplyBlock();
        Trace?.Invoke(this, "hold started · record " + RecordText(r) + (samePattern ? " · pattern kept" : " · pattern started (" + PatternSource + ")") + " · JIT warm-up " + WarmUpState);
    }

    void OnWindowHeld(FrontRoomsMapWorld.Window w, float progress)
    {
        if (!Mine(w)) return;
        holding = true;
        holdTime = progress * FrontRoomsShotTimings.GlassBreak.HoldSeconds;
    }

    void OnHoldReleased(Vector3 position)
    {
        if (Window == null || (Window.position - position).sqrMagnitude > 1e-4f) return;
        holding = false;
        if (Stage == 0) NeedSceneColor(false);   // cracks keep it: they still mirror the scene
    }

    void OnCracked(FrontRoomsMapWorld.Window w, int stage)
    {
        if (!Mine(w)) return;
        Record = Map.GlassBreakRecordOf(w);
        if (!meshesReady) FinishNow("GlassCracked " + stage);
        ShowStage(stage, true);
        BeginStop(stage);
    }

    void OnShattered(FrontRoomsMapWorld.Window w, Vector3 impactWorld, Vector3 impulse)
    {
        if (!Mine(w)) return;
        Record = Map.GlassBreakRecordOf(w);
        if (!meshesReady && Stage < 2) FinishNow("WindowShattered");
        var from = Stage;
        Stage = 3;
        holding = false;
        NeedSceneColor(false);
        // No ghost pane: everything this component drew goes on this frame.
        if (SlabRenderer != null) SlabRenderer.enabled = false;
        if (Stage1Renderer != null) Stage1Renderer.enabled = false;
        if (Stage2Renderer != null) Stage2Renderer.enabled = false;
        var taken = false;
        if (ShatterHandler != null)
        {
            try { taken = ShatterHandler.Shatter(this, impactWorld, impulse); }
            catch (Exception e) { Debug.LogException(e); }
        }
        Trace?.Invoke(this, "S3 shatter · impulse " + impulse.ToString("F2") + " · " + (taken ? "debris world took over" : "no debris handler: the glass is gone (plan step 6)"));
        RaiseStage(from, 3);
    }

    // ---- per frame -----------------------------------------------------------------------------------------------------
    // LateUpdate: after the game's Update has held E this frame (the map's Hold raises the events), before rendering.
    void LateUpdate()
    {
        if (work != Work.None)
        {
            var f0 = ClockMs();
            var scale = BudgetScale();
            if (work == Work.Pattern)
            {
                var done = job.StepFor(GenerateBudgetMs * scale);
                PatternCpuMs += ClockMs() - f0;
                if (done) PatternDone();
            }
            else if (work == Work.Meshes)
            {
                var done = meshes.StepFor(MeshBudgetMs * scale);
                MeshCpuMs += ClockMs() - f0;
                if (done) MeshesDone();
            }
            WorstFrameMs = Math.Max(WorstFrameMs, ClockMs() - f0);
        }
        TickBow(Time.deltaTime);
    }

    // Frames left before the Crack1 beat at this frame's rate (the hold runs in real time): x2 under 8, x4 under 4.
    double BudgetScale()
    {
        if (!holding || Stage > 0) return 1.0;
        var left = (FrontRoomsShotTimings.GlassBreak.Crack1 * FrontRoomsShotTimings.GlassBreak.HoldSeconds - holdTime) / Mathf.Max(Time.deltaTime, 1f / 240f);
        return left < 4f ? 4.0 : left < 8f ? 2.0 : 1.0;
    }

    // The bow: a brace preload, then each strike loads the pane up to its beat; the beat frame holds still for the glass's
    // object stop, then the load springs off (no overshoot: the facets never wobble). Released: back to flat in 0.2 s.
    void TickBow(float dt)
    {
        if (Stage >= 3 || SlabRenderer == null) return;
        var G = FrontRoomsShotTimings.GlassBreak.StrikeStart;
        float target;
        if (!holding) target = 0f;
        else
        {
            var t = holdTime;
            target = .25f * Smooth01(t / .25f);
            float[] beats = { FrontRoomsShotTimings.GlassBreak.Crack1, FrontRoomsShotTimings.GlassBreak.Crack2, FrontRoomsShotTimings.GlassBreak.Shatter };
            for (var k = 0; k < 3; k++)
                if (t >= G[k] && t < beats[k] && k >= lastBeat)
                {
                    var u = Mathf.Clamp01((t - G[k]) / (beats[k] - G[k]));
                    target = Mathf.Lerp(target, BowPeaks[k], u * u);
                }
            palm = DebugNoPalm || !PalmPrint ? 0f : Mathf.Max(palm, .6f * Smooth01(t / FrontRoomsShotTimings.GlassBreak.Crack1));
        }
        if (stopLeft > 0f)
        {
            stopLeft -= dt;       // the glass holds still for its object stop
        }
        else
        {
            residueAge += dt;
            var residue = bowResidue * Mathf.Exp(-residueAge / .06f);
            var follow = holding ? target : Mathf.MoveTowards(bow, 0f, dt / .2f);
            bow = Mathf.Max(follow, residue);
        }
        if (DebugNoBow) bow = 0f;
        ApplyBlock();
    }

    static readonly float[] BowPeaks = { .8f, .9f, 1f };
    /// <summary>The bruise round the crater at S1 (the S2 rim takes 1).</summary>
    public static float CrushS1 = .45f;

    void BeginStop(int beat)
    {
        var stops = FrontRoomsShotTimings.GlassBreak.ObjectStop;
        var k = Mathf.Clamp(beat - 1, 0, stops.Length - 1);
        stopLeft = stops[k];
        // the strike's full load at the moment of impact; held for the object stop, then it springs off
        if (holding && !DebugNoBow) bow = Mathf.Max(bow, BowPeaks[k]);
        bowResidue = bow;
        residueAge = 0f;
        lastBeat = Mathf.Max(lastBeat, beat);
    }

    static float Smooth01(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }

    // ---- the one-time JIT warm-up ----------------------------------------------------------------------------------------
    static int warmUpStarted;

    /// <summary>
    /// One break of the default profile on a worker thread: the generator (pure C#) and the CPU part of the mesh build
    /// (no Unity API; the GPU upload stays on the main thread and is small). Mono compiles each method once for every thread,
    /// so the first real E-down then runs at its warm cost. Runs once per process; results are thrown away.
    /// </summary>
    public static void StartWarmUp()
    {
        if (Interlocked.Exchange(ref warmUpStarted, 1) != 0) return;
#if ENABLE_IL2CPP || (UNITY_WEBGL && !UNITY_EDITOR)
        WarmUpState = "skipped (IL2CPP: no JIT)";
#else
        var profile = FrontRoomsGlassTypeProfile.ForTier(GlassType, Cinematic);   // main thread (Resources.Load)
        WarmUpState = "running";
        ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                var t0 = Stopwatch.GetTimestamp();
                var w = new Workset();
                FrontRoomsGlassFracturePattern.BeginRoot(w.job, profile, 19900101u, .21, 1.31, FrontRoomsGlassFracturePattern.SlabW,
                    FrontRoomsGlassFracturePattern.SlabH, double.NaN, 1, "band", w.res);
                w.job.Finish();
                if (!w.job.Failed) { w.meshes.Begin(w.res, new FrontRoomsGlassFractureMeshes.Options { keyA = 1f, keyB = 2f, bevel = double.NaN }); w.meshes.BuildCpuOnly(); }
                var failed = w.job.Failed ? ", generator: " + w.job.Error : "";
                ReturnSpare(w);
                WarmUpState = "done " + ((Stopwatch.GetTimestamp() - t0) * 1000.0 / Stopwatch.Frequency).ToString("0.0") + " ms wall (worker thread), its working set kept as a spare" + failed;
            }
            catch (Exception e) { WarmUpState = "failed: " + e.GetType().Name + " " + e.Message; }
        });
#endif
    }

    // ---- the pattern and the meshes ------------------------------------------------------------------------------------
    void StartPattern(FrontRoomsMapWorld.GlassBreakRecord r, string why)
    {
        var profile = FrontRoomsGlassTypeProfile.ForTier(GlassType, Cinematic);
        built = r;
        builtForRecord = true;
        patternReady = meshesReady = false;
        PatternCpuMs = MeshCpuMs = WorstFrameMs = 0;
        ForcedAt = "";
        startFrame = Time.frameCount;
        FramesToReady = -1;
        if (BuildAtPress)
        {
            PatternSource = why;
            FrontRoomsGlassFracturePattern.BeginRoot(job, profile, (uint)r.seed, r.impact.x, r.impact.y, FrontRoomsGlassFracturePattern.SlabW,
                FrontRoomsGlassFracturePattern.SlabH, r.rotation, r.side, "band", res);
        }
        else BeginBake(profile, r, why + ", baked variants only (decision 4)");
        work = Work.Pattern;
    }

    // The 18-variant fallback (plan sec 4.1): the bake centre nearest the impact, variant k from the seed. Regenerated (no data
    // ships): the same bytes the bake wrote (build/03_generator.md sec 6.3).
    void BeginBake(FrontRoomsGlassProfileData profile, FrontRoomsMapWorld.GlassBreakRecord r, string why)
    {
        var centre = FrontRoomsGlassFracturePattern.NearestBakeCentre(r.impact.x, r.impact.y);
        var k = (int)((uint)r.seed % 2u);
        var seed = (uint)(GlassRng.MixKey(19900101 + k, centre) & 0xFFFFFFFFUL);
        var cx = FrontRoomsGlassFracturePattern.BakeX[centre % 3];
        var cy = FrontRoomsGlassFracturePattern.BakeY[centre / 3];
        PatternSource = "bake c" + centre + " k" + k + " (" + why + ")";
        FrontRoomsGlassFracturePattern.BeginRoot(job, profile, seed, cx, cy, FrontRoomsGlassFracturePattern.SlabW, FrontRoomsGlassFracturePattern.SlabH,
            double.NaN, r.side, "band", res);
    }

    void PatternDone()
    {
        if (job.Failed || job.Validation == null || !job.Validation.AllOkButBudget)
        {
            var why = job.Failed ? "generation failed: " + job.Error : "pattern invalid: " + job.Validation.Summary();
            Debug.LogWarning("[GD3 glass] " + name + ": " + why + "; using the nearest baked variant");
            if (PatternSource.StartsWith("bake")) { work = Work.None; return; }   // a bake variant never fails (72 / 72 valid)
            BeginBake(FrontRoomsGlassTypeProfile.ForTier(GlassType, Cinematic), Record, "fallback: " + why);
            work = Work.Pattern;
            return;
        }
        patternReady = true;
        StartMeshes(null);
    }

    void StartMeshes(string why)
    {
        meshesReady = false;
        EnsureSlab();
        meshes.Begin(res, new FrontRoomsGlassFractureMeshes.Options { keyA = keyA, keyB = keyB, zeroPose = DebugZeroPose, bevel = double.NaN });
        work = Work.Meshes;
        if (why != null) Trace?.Invoke(this, "meshes rebuilt: " + why);
    }

    void MeshesDone()
    {
        work = Work.None;
        meshesReady = true;
        FramesToReady = Time.frameCount - startFrame;
        Stage1Renderer = EnsureStageObject(Stage1Renderer, Stage1Name, meshes.Stage1, -1);
        Stage2Renderer = EnsureStageObject(Stage2Renderer, Stage2Name, meshes.Stage2, 0);
        Stage1Renderer.enabled = Stage == 1;
        Stage2Renderer.enabled = Stage == 2;
        ApplyBlock();
        var c = meshes.Stats;
        Trace?.Invoke(this, "ready in " + FramesToReady + " frames (" + PatternSource + ") · pattern " + PatternCpuMs.ToString("0.00") + " ms + meshes " + MeshCpuMs.ToString("0.00")
            + " ms CPU, worst frame " + WorstFrameMs.ToString("0.00") + " ms · " + res.pieces.Count + " pieces · S1 " + c.fins + " fin segments, " + c.finTris + " + " + c.craterTris
            + " tris · S2 " + c.stage2Tris + " tris (" + c.stage2SideTris + " crack faces), " + c.stage2Verts + " verts · bevel clamped on " + c.bevelClamped + (ForcedAt.Length > 0 ? " · FORCED at " + ForcedAt : ""));
        try { MeshesReady?.Invoke(this); } catch (Exception e) { Debug.LogException(e); }
    }

    void FinishNow(string at)
    {
        var f0 = ClockMs();
        if (at != "restore") ForcedAt = at;
        if (!builtForRecord) StartPattern(Record, "press (late)");
        var guard = 0;
        while (work != Work.None && guard++ < 4)
        {
            if (work == Work.Pattern)
            {
                job.Finish();
                PatternDone();
            }
            if (work == Work.Meshes)
            {
                meshes.Finish();
                MeshesDone();
            }
        }
        var ms = ClockMs() - f0;
        if (at != "restore")
        {
            ForcedAt = at + " (" + ms.ToString("0.00") + " ms)";
            Trace?.Invoke(this, "the beat came before the slices finished: finished at once, " + ms.ToString("0.00") + " ms");
        }
        WorstFrameMs = Math.Max(WorstFrameMs, ms);
    }

    void ShowStage(int stage, bool raise)
    {
        if (stage <= Stage || stage > 2) return;
        var from = Stage;
        Stage = stage;
        if (!meshesReady || ws.res.isDice)
        {
            // Tempered glass shows no crack stages (it dices at once at the shatter).
            if (raise) RaiseStage(from, stage);
            return;
        }
        if (stage == 1)
        {
            SlabRenderer.enabled = true;
            Stage1Renderer.enabled = true;
            Stage2Renderer.enabled = false;
        }
        else
        {
            SlabRenderer.enabled = false;
            Stage1Renderer.enabled = false;
            Stage2Renderer.enabled = true;
        }
        ApplyBlock();
        Trace?.Invoke(this, "S" + stage + " shown");
        if (raise) RaiseStage(from, stage);
    }

    void RaiseStage(int from, int to)
    {
        try { StageChanged?.Invoke(this, from, to); } catch (Exception e) { Debug.LogException(e); }
    }

    // ---- objects, material, property block -----------------------------------------------------------------------------
    void EnsureSlab()
    {
        if (SlabRenderer != null) return;
        var go = new GameObject(SlabName);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, SlabY, 0f);
        go.transform.localRotation = Quaternion.identity;
        FrontRoomsGlassFractureMeshes.PaneKeys(go.transform.localToWorldMatrix, out keyA, out keyB);
        slabMesh = FrontRoomsGlassFractureMeshes.BuildSlab(slabMesh, keyA, keyB);
        SlabRenderer = SetUpRenderer(go, slabMesh, 0);
    }

    MeshRenderer EnsureStageObject(MeshRenderer r, string objectName, Mesh mesh, int sortingOrder)
    {
        if (r != null)
        {
            r.GetComponent<MeshFilter>().sharedMesh = mesh;
            return r;
        }
        var go = new GameObject(objectName);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, SlabY, 0f);
        go.transform.localRotation = Quaternion.identity;
        var mr = SetUpRenderer(go, mesh, sortingOrder);
        mr.enabled = false;
        return mr;
    }

    static MeshRenderer SetUpRenderer(GameObject go, Mesh mesh, int sortingOrder)
    {
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = FractureMaterial;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        r.lightProbeUsage = LightProbeUsage.BlendProbes;
        r.sortingOrder = sortingOrder;   // the fins draw before the slab's faces (they are inside it)
        go.AddComponent<FrontRoomsMetalGlassTarget>();
        return r;
    }

    /// <summary>
    /// The pieces' material: Glass_Window (the glass track's look, every value) with the pane frame taken from the mesh
    /// (_PaneFromUV) and _PaneSize = the slab. One shared instance ("Glass_Fracture", plan sec 4.2), so the ray tracer's
    /// receiver flag (_RTReceive) and every look value come from Glass_Window.
    /// </summary>
    public static Material FractureMaterial
    {
        get
        {
            if (fractureMaterial != null) return fractureMaterial;
            var window = Resources.Load<Material>("Surfaces/Glass_Window");
            var shader = Shader.Find("FrontRooms/Glass");
            fractureMaterial = window != null ? new Material(window) : new Material(shader);
            fractureMaterial.name = "Glass_Fracture (Glass_Window, _PaneFromUV)";
            fractureMaterial.SetFloat("_PaneFromUV", 1f);
            fractureMaterial.SetVector("_PaneSize", new Vector4(SlabW, SlabH, (float)FrontRoomsGlassFracturePattern.SlabT, 0f));
            fractureMaterial.hideFlags = HideFlags.DontSave;
            return fractureMaterial;
        }
    }

    void ApplyBlock()
    {
        if (SlabRenderer == null) return;
        block ??= new MaterialPropertyBlock();
        var r = Record;
        // the impact over the whole slab (the shader's pane uv), the bow over the held glass (the stops hold its edges)
        var ix = r.impact.x;
        var iy = r.impact.y - SlabY;
        var far = r.side > 0 ? 1f : -1f;
        // the crushed bruise: light round the crater at S1 (the crater mesh carries the crush), full on the rim of the hole at S2
        var a = new Vector4((ix + SlabW * .5f) / SlabW, (iy + SlabH * .5f) / SlabH, palm, Stage >= 2 ? 1f : Stage >= 1 ? CrushS1 : 0f);
        var b = new Vector4(patternReady && ws != null && ws.res.craterRadius > 0 ? (float)ws.res.craterRadius : .0127f, far * BowMax * bow, Stage, (Stage1Renderer != null ? 1 : 0) + (Stage2Renderer != null ? 2 : 0));
        if (applied && a == appliedA && b == appliedB) return;   // nothing changed: no property-block upload
        applied = true;
        appliedA = a;
        appliedB = b;
        block.SetVector(ImpactUVId, new Vector4(a.x, a.y, 0f, 0f));
        block.SetFloat(PalmId, palm);
        block.SetFloat(CrushId, a.w);
        block.SetFloat(CrushRadiusId, b.x);
        block.SetVector(BowCentreId, new Vector4(ix, iy, HeldHalfX, HeldHalfY));
        block.SetFloat(BowAmpId, b.y);
        SlabRenderer.SetPropertyBlock(block);
        if (Stage1Renderer != null) Stage1Renderer.SetPropertyBlock(block);
        if (Stage2Renderer != null) Stage2Renderer.SetPropertyBlock(block);
    }

    void FreeMeshes()
    {
        work = Work.None;
        meshesReady = patternReady = false;
        builtForRecord = false;
        if (ws != null)
        {
            ws.meshes.ReleaseMeshes();
            ReturnSpare(ws);
            ws = null;
        }
        if (slabMesh != null)
        {
            if (Application.isPlaying) Destroy(slabMesh); else DestroyImmediate(slabMesh);
            slabMesh = null;
        }
    }

    /// <summary>One line for logs and the lab's captures.</summary>
    public string StateLine()
    {
        var c = ws != null ? ws.meshes.Stats : default;
        return "GD3 S" + Stage + (Stage1Renderer != null && Stage1Renderer.enabled ? " [S1 on]" : "") + (Stage2Renderer != null && Stage2Renderer.enabled ? " [S2 on]" : "")
            + (SlabRenderer != null && SlabRenderer.enabled ? " [slab on]" : "") + " · bow " + (bow * BowMax * 1000f).ToString("0.00") + " mm · palm " + palm.ToString("0.00")
            + " · stop " + Mathf.Max(0f, stopLeft * 1000f).ToString("0") + " ms · " + (meshesReady ? "ready (" + FramesToReady + " fr, " + PatternSource + ", " + ws.res.pieces.Count + " pieces, S2 " + c.stage2Tris + " tris" + (TookSpare ? ", warm spare" : "") + ")" : work == Work.None ? "no pattern" : "building " + work);
    }

    static string RecordText(FrontRoomsMapWorld.GlassBreakRecord r) =>
        "stage " + r.stage + " seed " + r.seed + " impact (" + r.impact.x.ToString("0.0000") + ", " + r.impact.y.ToString("0.0000") + ") rot " + r.rotation.ToString("0.00") + " side " + r.side;
}
