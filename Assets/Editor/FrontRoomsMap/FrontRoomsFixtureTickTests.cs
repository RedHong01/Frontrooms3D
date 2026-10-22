using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using FrontRooms.Map;
using UnityEditor;
using UnityEngine;

/// <summary>
/// The WebGL lamp tick (FrontRoomsMapWorld.TickFixturesNearOnly) against the
/// desktop lamp formula, checked without playing the game: every lamp's Light
/// and lens are read back from the components after ticking and compared with
/// what the desktop path would have set (lit within lightRadius, at
/// baseIntensity × level × fade, soft shadows within shadowRadius when it
/// casts, the lens at the current glow; otherwise off, the lens within 5% of
/// the glow). The player stands still, crosses chunk borders, walks far and
/// comes back; the clocks run long enough for stutters, dropouts and blinks.
/// The desktop path is checked the same way, so the checker itself is proven.
/// M2, the ambient flicker limits (RELAY_PURSUIT_REDESIGN.md 9.5), on both tick
/// paths with Reduce flashing off and on: every built lamp is put to each mode
/// in turn and run 120 s at 60 fps; this file's own WCAG 2.3.1 counter (a change
/// = a swing of 0.10 or more between turning points with the darker side under
/// 0.80, a flash = two) finds at most 4 changes (2 flashes) in every closed 1 s
/// window of every lamp (hard limit 3 flashes); Stutter episodes of 2-3 dips,
/// at most 1.5 s, at least 2 s apart, never under 0.5 of the level; Failing
/// dropouts held 0.34 s or more, at most one a second; Dead strikes at least
/// 4 s apart; with Reduce flashing one sag to 0.7, at most 0.05 of gain a frame;
/// Steady, Dim, Off and every lamp between its events bit-identical to the
/// formulas before M2; both paths bit-identical to each other; Stutter and
/// Failing again at 30 fps and at a jittered frame time; the limiter itself
/// holds the old strobe to 3 flashes and passes a 2-flash series untouched;
/// SetAmbientHold keeps Dead lamps dark (through a rebuild) until let go.
/// Pinned to main: on a map of its own, the frame every Stutter episode starts
/// (every lamp, 60 s at 60 fps, Reduce flashing off and on) hashes to the
/// value main's own trigger gave; a Dead lamp switched away and back right
/// after a strike has its next blink refused if it comes within 4 s.
/// Writes Verification/fixture-tick-tests.json.
/// Headless: -executeMethod FrontRoomsFixtureTickTests.RunBatch -quit (throws on FAIL).
/// </summary>
public static class FrontRoomsFixtureTickTests
{
    const float Dt = 1f / 60f;

    [Serializable]
    sealed class Report
    {
        public string verdict;
        public int passed, failed;
        public List<string> checks = new List<string>();
        public List<string> flicker = new List<string>();
    }

    static Report report;

    static void Check(bool ok, string what)
    {
        if (ok) report.passed++; else report.failed++;
        report.checks.Add((ok ? "ok   " : "FAIL ") + what);
    }

    [MenuItem("FrontRooms/Map/Test lamp tick (WebGL near-only)")]
    public static void Run() => Execute(false);

    public static void RunBatch() => Execute(true);

    static void Execute(bool throwOnFail)
    {
        report = new Report();
        var ambientMode = RenderSettings.ambientMode;
        var sky = RenderSettings.ambientSkyColor;
        var equator = RenderSettings.ambientEquatorColor;
        var ground = RenderSettings.ambientGroundColor;
        var reflection = RenderSettings.reflectionIntensity;
        var fog = RenderSettings.fog;
        var fogMode = RenderSettings.fogMode;
        var fogColor = RenderSettings.fogColor;
        var fogDensity = RenderSettings.fogDensity;
        var skybox = RenderSettings.skybox;
        var nearOnly = FrontRoomsMapWorld.TickFixturesNearOnly;
        var flashing = FrontRoomsSettings.ReduceFlashing;
        var roots = new List<GameObject>();
        try
        {
            Walk(roots, true, -41f, "WebGL near-only");
            Walk(roots, false, -43f, "desktop");
            Flicker(roots);
            Pin(roots);
        }
        catch (Exception e)
        {
            Check(false, "exception: " + e.GetType().Name + " " + e.Message + "\n" + e.StackTrace);
        }
        finally
        {
            FrontRoomsMapWorld.TickFixturesNearOnly = nearOnly;
            if (FrontRoomsSettings.ReduceFlashing != flashing) FrontRoomsSettings.SetReduceFlashing(flashing);
            foreach (var r in roots)
            {
                if (r == null) continue;
                var w = r.GetComponent<FrontRoomsMapWorld>();
                if (w != null) w.Release();
                UnityEngine.Object.DestroyImmediate(r);
            }
            RenderSettings.ambientMode = ambientMode;
            RenderSettings.ambientSkyColor = sky;
            RenderSettings.ambientEquatorColor = equator;
            RenderSettings.ambientGroundColor = ground;
            RenderSettings.reflectionIntensity = reflection;
            RenderSettings.fog = fog;
            RenderSettings.fogMode = fogMode;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = fogDensity;
            RenderSettings.skybox = skybox;
            DynamicGI.UpdateEnvironment();
        }

        var pass = report.failed == 0 && report.passed > 0;
        report.verdict = (pass ? "PASS" : "FAIL") + ": " + report.passed + " passed, " + report.failed + " failed";
        var path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Verification", "fixture-tick-tests.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonUtility.ToJson(report, true));
        var text = "[FixtureTickTests] " + report.verdict + "\n" + string.Join("\n", report.checks);
        if (pass) Debug.Log(text); else Debug.LogError(text);
        if (throwOnFail && !pass) throw new Exception(report.verdict);
    }

    /// <summary>Build the Level 0 map round its spawn, then tick the lamps while the eye stands, moves and returns.</summary>
    static void Walk(List<GameObject> roots, bool nearOnly, float period, string label)
    {
        FrontRoomsMapWorld.TickFixturesNearOnly = nearOnly;
        var root = new GameObject("FIXTURE TICK TEST / " + label) { hideFlags = HideFlags.DontSave };
        roots.Add(root);
        // Far from the open scene, on whole world periods so surfaces stay on the lattice.
        root.transform.position = new Vector3(period * ModuleUnits.WorldPeriod, 0f, -27f * ModuleUnits.WorldPeriod);
        var world = root.AddComponent<FrontRoomsMapWorld>();
        world.Profile = FrontRoomsLevelProfiles.Resolve();
        world.BuildForCapture();
        // Transform.Find reads "/" as a path separator, so match the child by name.
        Transform eye = null;
        foreach (Transform child in world.transform) if (child.name == "CAPTURE / eye") eye = child;
        if (eye == null) { Check(false, label + ": BuildForCapture left no eye"); return; }
        var start = eye.position;
        var problems = new List<string>();

        void Verify(string where, int frames)
        {
            for (var i = 0; i < frames; i++) world.TickFixturesForTools(Dt);
            problems.Clear();
            var mismatches = world.CheckLampStatesForTools(out var lit, out var total, problems);
            Check(total > 0, label + " · " + where + ": " + total + " lamps built");
            Check(mismatches == 0, label + " · " + where + ": " + lit + " lit of " + total + ", " + mismatches + " disagree with the lamp formula" + (problems.Count > 0 ? "\n      " + string.Join("\n      ", problems) : ""));
        }

        Verify("at the spawn, first frame", 1);
        Verify("at the spawn, 6 s", 360);
        eye.position = start + new Vector3(MapGrid.ChunkSize, 0f, 0f);
        Verify("one chunk east", 30);
        eye.position = start + new Vector3(MapGrid.ChunkSize * 2f, 0f, MapGrid.ChunkSize * 2f);
        Verify("two chunks north-east", 30);
        eye.position = start + new Vector3(MapGrid.ChunkSize * 1.5f, 0f, -MapGrid.ChunkSize * .5f);
        Verify("on a chunk border", 240);
        eye.position = start;
        Verify("back at the spawn", 30);
        eye.position = start;
        Verify("back at the spawn, 10 s", 600);
    }
    // ---------- M2: the ambient flicker limits (RELAY_PURSUIT_REDESIGN.md 9.5) ----------

    // WCAG 2.3.1 by this file's own counter, not the map's limiter: a change is a swing of WcagStep or more
    // between turning points whose darker side is under WcagDark, counted the frame it gets there; a flash is two.
    const float WcagStep = .10f, WcagDark = .80f;
    const int FlickerFrames = 7200;

    static readonly ModuleLamp[] FlickerModes = { ModuleLamp.Steady, ModuleLamp.Stutter, ModuleLamp.Failing, ModuleLamp.Dead, ModuleLamp.Dim, ModuleLamp.Off };

    // One lamp under watch: the counter's zigzag and its changes in the last second, the most in any closed 1 s
    // window, the event runs (a Stutter episode, a Failing dropout, a Dead strike), Reduce flashing's sag, and a
    // hash of its levels for the path comparison.
    sealed class LampWatch
    {
        public GridCoord cell;
        public bool begun, counted;
        public int dir;
        public float pivot, ext, lo, hi;
        public readonly List<double> recent = new List<double>();
        public int changes, maxInWindow;
        public bool hasLast, lastActive;
        public float lastLevel, lastGain;
        public double runStart = double.NaN, lastRunStart = double.NegativeInfinity, lastRunEnd = double.NegativeInfinity;
        public int runDips;
        public bool runBelow;
        public double sagStart = double.NaN, lastDeep = double.NaN;
        public ulong hash = 14695981039346656037UL;
        public int heldBefore;
    }

    // What one run (a mode, Reduce flashing off or on, a tick path, a frame time) found over all its lamps.
    sealed class FlickerRun
    {
        public string label;
        public int lamps, frames;
        public double seconds;
        public int maxInWindow, lampsOver2, lampsOver3, held;
        public long changes, parityFrames, parityWrong, levelWrong;
        public string parityProblem;
        public int runs, runsBadDips;
        public double maxRun, minRun = double.MaxValue, minGapAfter = double.MaxValue, minStartToStart = double.MaxValue;
        public double minDescent = double.MaxValue, minAscent = double.MaxValue;
        public float minGain = 1f, minRunLevel = 1f, maxGainStep, maxLevelStep;
        public ulong[] hashes;
    }

    /// <summary>Every mode on both tick paths, Reduce flashing off and on; two more frame times; the limiter itself; the hold.</summary>
    static void Flicker(List<GameObject> roots)
    {
        var desktop = FlickerWorld(roots, -45f, "desktop", out var cells);
        var webgl = FlickerWorld(roots, -47f, "WebGL near-only", out var cellsWebGL);
        var sameCells = cells.Count == cellsWebGL.Count;
        for (var i = 0; sameCells && i < cells.Count; i++) sameCells = cells[i] == cellsWebGL[i];
        Check(cells.Count >= 500 && sameCells, "flicker: " + cells.Count + " lamps built round the spawn, the same on both paths' maps (" + sameCells + ")");
        if (cells.Count == 0 || !sameCells) return;
        foreach (var reduced in new[] { false, true })
        foreach (var mode in FlickerModes)
        {
            var name = mode + (reduced ? ", Reduce flashing" : "");
            FrontRoomsMapWorld.TickFixturesNearOnly = false;
            var d = Simulate(desktop, cells, mode, reduced, "flicker desktop - " + name, f => Dt, FlickerFrames);
            FrontRoomsMapWorld.TickFixturesNearOnly = true;
            var w = Simulate(webgl, cells, mode, reduced, "flicker WebGL - " + name, f => Dt, FlickerFrames);
            Judge(d, mode, reduced, true);
            Judge(w, mode, reduced, true);
            var differ = 0;
            for (var i = 0; i < cells.Count; i++) if (d.hashes[i] != w.hashes[i]) differ++;
            Check(differ == 0, "flicker - " + name + ": the desktop and WebGL paths give every lamp the same levels, bit for bit (" + differ + " of " + cells.Count + " differ)");
        }

        // Other frame times (the counts are in seconds, so they must hold at any frame rate): 30 fps, and a frame time
        // drawn from 1/144 to 1/20 s each frame.
        FrontRoomsMapWorld.TickFixturesNearOnly = false;
        var lcg = 12345u;
        Func<int, float> jitter = f =>
        {
            lcg = lcg * 1664525u + 1013904223u;
            return Mathf.Lerp(1f / 144f, 1f / 20f, (lcg >> 8) / 16777216f);
        };
        foreach (var reduced in new[] { false, true })
        foreach (var mode in new[] { ModuleLamp.Stutter, ModuleLamp.Failing })
        {
            var name = mode + (reduced ? ", Reduce flashing" : "");
            Judge(Simulate(desktop, cells, mode, reduced, "flicker desktop 30 fps - " + name, f => 1f / 30f, 3600), mode, reduced, false);
            Judge(Simulate(desktop, cells, mode, reduced, "flicker desktop 20-144 fps - " + name, jitter, 4200), mode, reduced, false);
        }

        Limiter();
        Holds(desktop, cells);
        if (FrontRoomsSettings.ReduceFlashing) FrontRoomsSettings.SetReduceFlashing(false);
        foreach (var c in cells) desktop.SetLampMode(c, ModuleLamp.Auto);
        foreach (var c in cells) webgl.SetLampMode(c, ModuleLamp.Auto);
    }

    // The Level 0 map round its spawn (as Walk builds it) and every lamp built on it.
    static FrontRoomsMapWorld FlickerWorld(List<GameObject> roots, float period, string label, out List<GridCoord> cells)
    {
        cells = new List<GridCoord>();
        var root = new GameObject("FIXTURE TICK TEST / flicker " + label) { hideFlags = HideFlags.DontSave };
        roots.Add(root);
        root.transform.position = new Vector3(period * ModuleUnits.WorldPeriod, 0f, -27f * ModuleUnits.WorldPeriod);
        var world = root.AddComponent<FrontRoomsMapWorld>();
        world.Profile = FrontRoomsLevelProfiles.Resolve();
        world.BuildForCapture();
        var mid = world.CellOf(world.SpawnWorldPosition);
        for (var y = mid.y - 48; y <= mid.y + 48; y++)
        for (var x = mid.x - 48; x <= mid.x + 48; x++)
        {
            var c = new GridCoord(x, y);
            if (world.LampLevel(c) != FrontRoomsMapWorld.NoLamp && world.LampModeOf(c) != ModuleLamp.Off) cells.Add(c);
        }
        return world;
    }

    // Put every lamp to the mode and run it, watching each lamp every frame.
    static FlickerRun Simulate(FrontRoomsMapWorld world, List<GridCoord> cells, ModuleLamp mode, bool reduced, string label, Func<int, float> frameTime, int frames)
    {
        if (FrontRoomsSettings.ReduceFlashing != reduced) FrontRoomsSettings.SetReduceFlashing(reduced);
        // Each run starts every lamp on a fresh cycle of the mode (SetLampMode restarts only a lamp whose mode changes).
        var other = mode == ModuleLamp.Steady ? ModuleLamp.Dim : ModuleLamp.Steady;
        foreach (var c in cells)
        {
            world.SetLampMode(c, other);
            world.SetLampMode(c, mode);
        }
        var run = new FlickerRun { label = label, lamps = cells.Count, frames = frames, hashes = new ulong[cells.Count] };
        var watch = new LampWatch[cells.Count];
        for (var i = 0; i < cells.Count; i++)
        {
            watch[i] = new LampWatch { cell = cells[i] };
            world.LampAmbientForTools(cells[i], out _, out _, out var held);
            watch[i].heldBefore = held;
        }
        var time = 0.0;
        for (var f = 0; f < frames; f++)
        {
            var dt = frameTime(f);
            time += dt;
            world.TickFixturesForTools(dt);
            foreach (var w in watch) Observe(world, w, mode, reduced, time, run);
        }
        run.seconds = time;
        for (var i = 0; i < cells.Count; i++)
        {
            var w = watch[i];
            run.hashes[i] = w.hash;
            run.changes += w.changes;
            run.maxInWindow = Mathf.Max(run.maxInWindow, w.maxInWindow);
            if (w.maxInWindow > 4) run.lampsOver2++;
            if (w.maxInWindow > 6) run.lampsOver3++;
            world.LampAmbientForTools(w.cell, out _, out _, out var held);
            run.held += held - w.heldBefore;
        }
        report.flicker.Add(Summary(run));
        return run;
    }

    static void Observe(FrontRoomsMapWorld world, LampWatch w, ModuleLamp mode, bool reduced, double time, FlickerRun run)
    {
        var v = world.LampBaseLevel(w.cell);
        if (world.LampLevel(w.cell) != v) run.levelWrong++;
        world.LampAmbientForTools(w.cell, out var gain, out var active, out _);
        world.LampClockForTools(w.cell, out var clock, out var phase);
        unchecked { w.hash = (w.hash ^ (uint)BitConverter.SingleToInt32Bits(v)) * 1099511628211UL; }

        // Bit-identity with the formulas before M2 (map 7ef0ca45) wherever no event shapes the level.
        var t = clock + phase;
        var compare = true;
        float old;
        switch (mode)
        {
            case ModuleLamp.Steady: old = .98f + .02f * Mathf.Sin(t * 110f); break;
            case ModuleLamp.Stutter: old = .97f + .03f * Mathf.Sin(t * 120f); compare = !active && gain == 1f; break;
            case ModuleLamp.Failing:
                var wave = .5f + .5f * Mathf.Sin(t * 3.1f) * (.7f + .3f * Mathf.Sin(t * 1.27f));
                old = (.25f + .45f * wave) * 1f;
                compare = !active && gain == 1f;
                break;
            case ModuleLamp.Dead: old = 0f; compare = !active; break;
            case ModuleLamp.Dim: old = .42f + .03f * Mathf.Sin(t * 90f); break;
            default: old = 0f; break;
        }
        if (compare)
        {
            run.parityFrames++;
            if (BitConverter.SingleToInt32Bits(old) != BitConverter.SingleToInt32Bits(v))
            {
                run.parityWrong++;
                if (run.parityProblem == null) run.parityProblem = w.cell + " at " + time.ToString("0.000") + " s: " + v.ToString("R") + ", before M2 " + old.ToString("R");
            }
        }

        Count(w, v, time);
        if (w.hasLast)
        {
            run.maxLevelStep = Mathf.Max(run.maxLevelStep, Mathf.Abs(v - w.lastLevel));
            run.maxGainStep = Mathf.Max(run.maxGainStep, Mathf.Abs(gain - w.lastGain));
        }

        // Event runs: a Stutter episode, a Failing dropout, a Dead strike.
        if (active && !w.lastActive)
        {
            if (!double.IsNegativeInfinity(w.lastRunEnd)) run.minGapAfter = Math.Min(run.minGapAfter, time - w.lastRunEnd);
            if (!double.IsNegativeInfinity(w.lastRunStart)) run.minStartToStart = Math.Min(run.minStartToStart, time - w.lastRunStart);
            w.runStart = w.lastRunStart = time;
            w.runDips = 0;
            w.runBelow = false;
        }
        if (active)
        {
            if (gain < 1f && !w.runBelow) { w.runDips++; w.runBelow = true; }
            else if (gain == 1f) w.runBelow = false;
            run.minGain = Mathf.Min(run.minGain, gain);
            run.minRunLevel = Mathf.Min(run.minRunLevel, v);
        }
        else if (w.lastActive && !double.IsNaN(w.runStart))
        {
            var length = time - w.runStart;
            run.runs++;
            run.maxRun = Math.Max(run.maxRun, length);
            run.minRun = Math.Min(run.minRun, length);
            if (mode == ModuleLamp.Stutter && (reduced ? w.runDips != 1 : w.runDips < 2 || w.runDips > 3)) run.runsBadDips++;
            w.lastRunEnd = time;
            w.runStart = double.NaN;
        }

        // Reduce flashing's sag: from leaving 1 to reaching its depth, and from leaving its depth to 1 again.
        if (reduced && w.hasLast)
        {
            var deep = gain <= FrontRoomsMapWorld.ReducedAmbientDepth + 1e-4f;
            if (gain < 1f && w.lastGain == 1f) { w.sagStart = time; w.lastDeep = double.NaN; }
            if (deep && !double.IsNaN(w.sagStart)) { run.minDescent = Math.Min(run.minDescent, time - w.sagStart); w.sagStart = double.NaN; }
            if (deep) w.lastDeep = time;
            if (gain == 1f && w.lastGain < 1f && !double.IsNaN(w.lastDeep)) { run.minAscent = Math.Min(run.minAscent, time - w.lastDeep); w.lastDeep = double.NaN; }
        }
        w.lastActive = active;
        w.lastLevel = v;
        w.lastGain = gain;
        w.hasLast = true;
    }

    // The counter: a zigzag with turning points WcagStep apart; each swing counts once, the frame it is WcagStep
    // from its turning point with its darker side under WcagDark; the window is closed (a hair wider for rounding).
    static void Count(LampWatch w, float v, double time)
    {
        if (!w.begun)
        {
            w.begun = true;
            w.dir = 0;
            w.pivot = w.ext = w.lo = w.hi = v;
            return;
        }
        if (w.dir == 0)
        {
            w.lo = Mathf.Min(w.lo, v);
            w.hi = Mathf.Max(w.hi, v);
            if (v - w.lo >= WcagStep) Turn(w, 1, w.lo, v);
            else if (w.hi - v >= WcagStep) Turn(w, -1, w.hi, v);
        }
        else if (w.dir > 0)
        {
            if (v > w.ext) w.ext = v;
            else if (w.ext - v >= WcagStep) Turn(w, -1, w.ext, v);
        }
        else
        {
            if (v < w.ext) w.ext = v;
            else if (v - w.ext >= WcagStep) Turn(w, 1, w.ext, v);
        }
        if (w.dir == 0 || w.counted) return;
        if (Mathf.Abs(w.ext - w.pivot) < WcagStep || Mathf.Min(w.ext, w.pivot) >= WcagDark) return;
        w.counted = true;
        w.changes++;
        w.recent.Add(time);
        while (w.recent[0] < time - 1.0 - 1e-6) w.recent.RemoveAt(0);
        if (w.recent.Count > w.maxInWindow) w.maxInWindow = w.recent.Count;
    }

    static void Turn(LampWatch w, int dir, float pivot, float v)
    {
        w.dir = dir;
        w.pivot = pivot;
        w.ext = v;
        w.counted = false;
    }

    // The checks for one run. `full`: 60 fps on both paths (episode shapes and parity); otherwise flashes and parity only.
    static void Judge(FlickerRun r, ModuleLamp mode, bool reduced, bool full)
    {
        var flashes = (r.maxInWindow / 2f).ToString("0.0");
        var span = r.lamps + " lamps x " + r.seconds.ToString("0") + " s";
        Check(r.maxInWindow <= 4, r.label + ": at most " + flashes + " flashes in any 1 s, " + span + " (design target 2; " + r.lampsOver2 + " lamps over 2)");
        Check(r.maxInWindow <= 6, r.label + ": WCAG 2.3.1, never more than 3 flashes in any 1 s (" + r.lampsOver3 + " lamps over 3)");
        Check(r.parityFrames > 0 && r.parityWrong == 0, r.label + ": bit-identical to the formula before M2 on " + r.parityFrames + " lamp-frames with no event ("
            + r.parityWrong + " differ" + (r.parityProblem != null ? "; first " + r.parityProblem : "") + ")");
        Check(r.levelWrong == 0 && r.held == 0, r.label + ": with no override LampLevel is LampBaseLevel (" + r.levelWrong + " differ); the flash limiter never had to hold this mode (" + r.held + " lamp-frames)");
        if (!full) return;
        switch (mode)
        {
            case ModuleLamp.Stutter:
                if (!reduced)
                    Check(r.runs >= 1000 && r.runsBadDips == 0 && r.maxRun <= 1.5 + 1e-6 && r.minGapAfter >= 2.0 && r.minGain >= FrontRoomsMapWorld.StutterFloor - 1e-6f
                        && r.minRunLevel >= FrontRoomsMapWorld.StutterFloor * .94f - 1e-6f,
                        r.label + ": " + r.runs + " episodes of 2-3 dips (" + r.runsBadDips + " not), " + r.minRun.ToString("0.000") + "-" + r.maxRun.ToString("0.000") + " s (at most 1.5), then "
                        + r.minGapAfter.ToString("0.00") + " s or more steady (at least 2); dips down to " + r.minGain.ToString("0.000") + " of the level (floor 0.5), level " + r.minRunLevel.ToString("0.000"));
                else
                    Check(r.runs >= 1000 && r.runsBadDips == 0 && r.maxRun <= 2.0 + 1e-6 && r.minGapAfter >= 2.0 && r.minGain >= FrontRoomsMapWorld.ReducedAmbientDepth - 1e-6f
                        && r.maxGainStep <= .05f && r.minDescent >= FrontRoomsMapWorld.ReducedFlashingEnvelopeSeconds - 2.0 * Dt && r.minAscent >= FrontRoomsMapWorld.ReducedFlashingEnvelopeSeconds - 2.0 * Dt,
                        r.label + ": " + r.runs + " episodes, each one sag (" + r.runsBadDips + " not) to " + r.minGain.ToString("0.000") + " (0.7), down in " + r.minDescent.ToString("0.000") + " s and back in "
                        + r.minAscent.ToString("0.000") + " s or more (0.5), largest gain step " + r.maxGainStep.ToString("0.0000") + " a frame (at most 0.05; level step with the shimmer "
                        + r.maxLevelStep.ToString("0.000") + "), " + r.maxRun.ToString("0.00") + " s at most, " + r.minGapAfter.ToString("0.00") + " s or more apart");
                break;
            case ModuleLamp.Failing:
                Check(r.runs >= 1000 && r.minRun >= FrontRoomsMapWorld.FailingHoldMin - 1e-3 && r.minStartToStart >= 1.0 && r.minGapAfter >= FrontRoomsMapWorld.FailingGap - 1e-3,
                    r.label + ": " + r.runs + " dropouts, held " + r.minRun.ToString("0.000") + " s or more (0.34), starts " + r.minStartToStart.ToString("0.000") + " s or more apart (at most 1 a second), "
                    + r.minGapAfter.ToString("0.000") + " s or more after the last ended");
                if (reduced)
                    Check(r.minGain >= FrontRoomsMapWorld.ReducedAmbientDepth - 1e-6f && r.maxGainStep <= .05f && r.minDescent >= FrontRoomsMapWorld.ReducedFlashingEnvelopeSeconds - 2.0 * Dt
                        && r.minAscent >= FrontRoomsMapWorld.ReducedFlashingEnvelopeSeconds - 2.0 * Dt,
                        r.label + ": a dropout is one sag to " + r.minGain.ToString("0.000") + " (0.7), down in " + r.minDescent.ToString("0.000") + " s, back in " + r.minAscent.ToString("0.000")
                        + " s (0.5), largest gain step " + r.maxGainStep.ToString("0.0000") + " a frame (at most 0.05; level step " + r.maxLevelStep.ToString("0.000") + ")");
                break;
            case ModuleLamp.Dead:
                if (!reduced)
                    Check(r.runs >= 100 && r.minStartToStart >= FrontRoomsMapWorld.DeadStrikeGap - 1e-3 && r.maxRun <= .18 + 2.0 * Dt,
                        r.label + ": " + r.runs + " strikes, one blink each (" + r.maxRun.ToString("0.000") + " s at most), " + r.minStartToStart.ToString("0.00") + " s or more apart (at least 4)");
                else Check(r.runs == 0 && r.changes == 0, r.label + ": no strike at all (" + r.runs + ")");
                break;
            default:
                Check(r.changes == 0 && r.runs == 0, r.label + ": no change, no event (" + r.changes + ", " + r.runs + ")");
                break;
        }
    }

    static string Summary(FlickerRun r)
    {
        var perMinute = r.lamps > 0 && r.seconds > 0 ? r.changes / 2.0 / r.lamps / r.seconds * 60.0 : 0.0;
        return r.label + ": " + r.lamps + " lamps x " + r.seconds.ToString("0.0") + " s; max " + (r.maxInWindow / 2f).ToString("0.0") + " flashes/s, lamps over 2 " + r.lampsOver2
            + ", over 3 " + r.lampsOver3 + "; " + perMinute.ToString("0.00") + " flashes/min a lamp; events " + r.runs
            + (r.runs > 0 ? " (" + r.minRun.ToString("0.000") + "-" + r.maxRun.ToString("0.000") + " s, gap after >= " + (r.minGapAfter == double.MaxValue ? "-" : r.minGapAfter.ToString("0.00")) + " s)" : "")
            + "; min gain " + r.minGain.ToString("0.000") + "; max step level " + r.maxLevelStep.ToString("0.000") + " gain " + r.maxGainStep.ToString("0.0000")
            + "; parity " + r.parityWrong + "/" + r.parityFrames + "; limiter held " + r.held;
    }

    /// <summary>
    /// The limiter itself (no mode reaches it now): the old stutter's strobe, PerlinNoise(t x 23) between 0.95 and
    /// 0.05 without a pause, and a 10 Hz square come out with at most 6 changes (3 flashes) in any closed 1 s; a
    /// series of 2 flashes a second passes untouched.
    /// </summary>
    static void Limiter()
    {
        var input = new List<float>();
        var output = new List<float>();
        const float phase = 17.3f;
        for (var i = 0; i < 1800; i++)
        {
            var t = (i + 1) * Dt + phase;
            input.Add(Mathf.PerlinNoise(t * 23f, phase) > .45f ? .95f : .05f);
        }
        var inMax = MaxChanges(input, Dt);
        var held = FrontRoomsMapWorld.LimitFlashesForTools(input, Dt, output);
        var outMax = MaxChanges(output, Dt);
        Check(inMax > 6 && outMax <= 6 && held > 0, "flicker limiter: the old stutter's strobe for 30 s, " + inMax / 2f + " flashes/s in, at most " + outMax / 2f + " out (held " + held + " frames)");

        input.Clear();
        for (var i = 0; i < 1800; i++) input.Add((i / 3) % 2 == 0 ? .95f : .05f);
        inMax = MaxChanges(input, Dt);
        held = FrontRoomsMapWorld.LimitFlashesForTools(input, Dt, output);
        outMax = MaxChanges(output, Dt);
        var varied = 0;
        for (var i = 1; i < output.Count; i++) if (output[i] != output[i - 1]) varied++;
        Check(inMax > 6 && outMax <= 6 && held > 0 && varied > 0, "flicker limiter: a 10 Hz square for 30 s, " + inMax / 2f + " flashes/s in, at most " + outMax / 2f + " out, still moving (" + varied + " changes, held " + held + " frames)");

        input.Clear();
        for (var i = 0; i < 1800; i++)
        {
            var x = ((i + 1) * Dt) % .55f;
            input.Add(x < .12f ? .5f : .97f);
        }
        inMax = MaxChanges(input, Dt);
        held = FrontRoomsMapWorld.LimitFlashesForTools(input, Dt, output);
        var same = output.Count == input.Count;
        for (var i = 0; same && i < input.Count; i++) same = BitConverter.SingleToInt32Bits(input[i]) == BitConverter.SingleToInt32Bits(output[i]);
        Check(inMax <= 4 && inMax > 2 && held == 0 && same, "flicker limiter: a dip every 0.55 s (" + inMax / 2f + " flashes/s) passes untouched (held " + held + " frames, bit-identical " + same + ")");
    }

    static int MaxChanges(List<float> levels, float dt)
    {
        var w = new LampWatch();
        for (var i = 0; i < levels.Count; i++) Count(w, levels[i], (i + 1) * (double)dt);
        return w.maxInWindow;
    }

    /// <summary>
    /// SetAmbientHold: held Dead lamps never strike while the free ones do; a rebuilt chunk keeps the hold; let go,
    /// the held ones strike again.
    /// </summary>
    static void Holds(FrontRoomsMapWorld world, List<GridCoord> cells)
    {
        FrontRoomsMapWorld.TickFixturesNearOnly = false;
        if (FrontRoomsSettings.ReduceFlashing) FrontRoomsSettings.SetReduceFlashing(false);
        foreach (var c in cells) world.SetLampMode(c, ModuleLamp.Dead);
        var held = new List<GridCoord>();
        var free = new List<GridCoord>();
        for (var i = 0; i < cells.Count; i++) (i % 2 == 0 ? held : free).Add(cells[i]);
        world.SetAmbientHold(held, true);
        var struck = new HashSet<GridCoord>();
        int TickHolds(float seconds, List<GridCoord> watch)
        {
            var lit = 0;
            for (var t = 0f; t < seconds - 1e-4f; t += Dt)
            {
                world.TickFixturesForTools(Dt);
                foreach (var c in watch)
                {
                    if (world.LampBaseLevel(c) <= 0f) continue;
                    lit++;
                    struck.Add(c);
                }
            }
            return lit;
        }
        var all = new List<GridCoord>(cells);
        var heldSet = new HashSet<GridCoord>(held);
        var litAll = TickHolds(60f, all);
        var heldLit = 0;
        var freeStruck = 0;
        foreach (var c in struck) if (heldSet.Contains(c)) heldLit++; else freeStruck++;
        var allHeld = held.TrueForAll(world.AmbientHeld);
        Check(heldLit == 0 && freeStruck >= free.Count * 9 / 10 && allHeld && world.AmbientHoldCount == held.Count,
            "flicker hold: in 60 s none of " + held.Count + " held Dead lamps strikes (" + heldLit + "), " + freeStruck + " of " + free.Count + " free ones do (" + litAll + " lit lamp-frames)");

        // A rebuilt chunk's new fixtures keep the hold (it is kept by cell).
        var chunk = MapGrid.ChunkOf(held[0]);
        var inChunk = held.FindAll(c => MapGrid.ChunkOf(c) == chunk);
        world.RebuildChunk(chunk);
        struck.Clear();
        TickHolds(40f, inChunk);
        Check(struck.Count == 0 && inChunk.TrueForAll(world.AmbientHeld), "flicker hold: chunk " + chunk + " rebuilt, its " + inChunk.Count + " held Dead lamps still never strike in 40 s (" + struck.Count + ")");

        // Let go: they strike again.
        world.SetAmbientHold(held, false);
        struck.Clear();
        TickHolds(60f, held);
        Check(world.AmbientHoldCount == 0 && struck.Count >= held.Count * 9 / 10, "flicker hold: let go, " + struck.Count + " of " + held.Count + " strike again within 60 s");
        foreach (var c in cells) world.SetLampMode(c, ModuleLamp.Auto);
    }

    // ---------- Pinned to main: when Stutter episodes start; DeadStrikeGap after a mode switch ----------

    // M2 keeps the old Stutter trigger, timers and draws, so every episode must start on the frame main's stutter
    // event did. Golden values taken 2026-10-22 from main's own files (map 7ef0ca45, warn lamps b11d8f93) swapped
    // into the clone, by a temporary dump that read main's trigger itself (the frame Fixture.eventEnd is
    // reassigned) in the same sequence as StutterPin below; the same dump on M2 (map 223da401) gave the same
    // frames (logs/m2/pin/base_dump.txt, m2_dump.txt). Any change to that sequence (the map round the spawn, the
    // lamps' RNG, SetLampMode's draw, the trigger, an extra draw in it) moves these.
    const int PinLamps = 1597, PinFrames = 3600;
    const ulong PinHashOff = 0xFD913715ACCA1685UL, PinHashReduced = 0xEAED3C1C207C73E8UL;
    const int PinCountOff = 7157, PinCountReduced = 7159;
    const ulong FnvOffset = 14695981039346656037UL, FnvPrime = 1099511628211UL;

    // FNV-1a, 64 bit, over the 4 bytes of v (low byte first).
    static ulong Fnv(ulong h, int v)
    {
        unchecked
        {
            for (var b = 0; b < 4; b++)
            {
                h ^= (byte)(v >> (8 * b));
                h *= FnvPrime;
            }
        }
        return h;
    }

    /// <summary>A map of its own (nothing ticked on it before), so the pinned frames depend on nothing else this file does.</summary>
    static void Pin(List<GameObject> roots)
    {
        var world = FlickerWorld(roots, -49f, "pin", out var cells);
        var lampsOk = cells.Count == PinLamps;
        Check(lampsOk, "flicker pin: " + cells.Count + " lamps built round the spawn (main " + PinLamps + ")");
        if (!lampsOk) return;
        StutterPin(world, cells);
        DeadGapAfterSwitch(world, cells);
        if (FrontRoomsSettings.ReduceFlashing) FrontRoomsSettings.SetReduceFlashing(false);
        foreach (var c in cells) world.SetLampMode(c, ModuleLamp.Auto);
    }

    // Every lamp Steady then Stutter (a fresh cycle), 60 s at 60 fps on the desktop path, Reduce flashing off, then
    // the same again with it on: the frame each lamp enters an episode (LampAmbientForTools' active, off to on),
    // folded as (lamp index, frame) into FNV-1a, against main's.
    static void StutterPin(FrontRoomsMapWorld world, List<GridCoord> cells)
    {
        FrontRoomsMapWorld.TickFixturesNearOnly = false;
        foreach (var reduced in new[] { false, true })
        {
            if (FrontRoomsSettings.ReduceFlashing != reduced) FrontRoomsSettings.SetReduceFlashing(reduced);
            foreach (var c in cells)
            {
                world.SetLampMode(c, ModuleLamp.Steady);
                world.SetLampMode(c, ModuleLamp.Stutter);
            }
            var was = new bool[cells.Count];
            var hash = FnvOffset;
            var count = 0;
            for (var f = 0; f < PinFrames; f++)
            {
                world.TickFixturesForTools(Dt);
                for (var i = 0; i < cells.Count; i++)
                {
                    world.LampAmbientForTools(cells[i], out _, out var active, out _);
                    if (active && !was[i])
                    {
                        hash = Fnv(Fnv(hash, i), f);
                        count++;
                    }
                    was[i] = active;
                }
            }
            var wantHash = reduced ? PinHashReduced : PinHashOff;
            var wantCount = reduced ? PinCountReduced : PinCountOff;
            Check(hash == wantHash && count == wantCount, "flicker pin - Stutter" + (reduced ? ", Reduce flashing" : "") + ": every episode starts on main's frame ("
                + count + " starts over " + cells.Count + " lamps x " + (PinFrames / 60) + " s at 60 fps, hash 0x" + hash.ToString("X16") + "; main " + wantCount + ", 0x" + wantHash.ToString("X16") + ")");
        }
    }

    // DeadStrikeGap reached through SetLampMode (the old timers alone keep strikes 8 s apart): every lamp Dead; on
    // the frame a lamp's strike shows, it is switched to Steady and back to Dead, a fresh cycle whose first blink
    // comes 2-16 s later. Where that blink comes within 4 s of the strike it must be refused (no light); where it
    // comes later it must show. The blink's frame is read from the map (the frame Fixture.nextEvent is drawn
    // again), so the check sees refused blinks too.
    static void DeadGapAfterSwitch(FrontRoomsMapWorld world, List<GridCoord> cells)
    {
        const float Gap = 4f;
        const int SwitchFrames = 2400, Frames = 3480;
        FrontRoomsMapWorld.TickFixturesNearOnly = false;
        if (FrontRoomsSettings.ReduceFlashing) FrontRoomsSettings.SetReduceFlashing(false);
        var n = cells.Count;
        var fixtures = new object[n];
        FieldInfo nextEvent = null;
        var byCellField = typeof(FrontRoomsMapWorld).GetField("fixtureByCell", BindingFlags.NonPublic | BindingFlags.Instance);
        if (byCellField != null && byCellField.GetValue(world) is IDictionary byCell)
        {
            for (var i = 0; i < n; i++) fixtures[i] = byCell.Contains(cells[i]) ? byCell[cells[i]] : null;
            if (fixtures[0] != null) nextEvent = fixtures[0].GetType().GetField("nextEvent");
        }
        if (nextEvent == null || Array.IndexOf(fixtures, null) >= 0)
        {
            Check(false, "flicker Dead gap: cannot read the lamps' next event (FrontRoomsMapWorld.fixtureByCell, Fixture.nextEvent)");
            return;
        }
        foreach (var c in cells)
        {
            world.SetLampMode(c, ModuleLamp.Steady);
            world.SetLampMode(c, ModuleLamp.Dead);
        }
        var strike = new float[n];
        var due = new float[n];
        var switched = new bool[n];
        var resolved = new bool[n];
        var lastLit = new bool[n];
        int switches = 0, inside = 0, refused = 0, insideShown = 0, outsideShown = 0, outsideRefused = 0, early = 0, notFresh = 0;
        string firstEarly = null;
        for (var frame = 0; frame < Frames; frame++)
        {
            world.TickFixturesForTools(Dt);
            for (var i = 0; i < n; i++)
            {
                var lit = world.LampBaseLevel(cells[i]) > 0f;
                var rising = lit && !lastLit[i];
                lastLit[i] = lit;
                world.LampClockForTools(cells[i], out var clock, out _);
                if (!switched[i])
                {
                    if (!rising || frame >= SwitchFrames) continue;
                    strike[i] = clock;
                    world.SetLampMode(cells[i], ModuleLamp.Steady);
                    world.SetLampMode(cells[i], ModuleLamp.Dead);
                    due[i] = (float)nextEvent.GetValue(fixtures[i]);
                    switched[i] = true;
                    switches++;
                    if (due[i] < clock + 2f - 1e-3f) { notFresh++; resolved[i] = true; }
                    continue;
                }
                if (rising && clock - strike[i] < Gap)
                {
                    early++;
                    if (firstEarly == null) firstEarly = cells[i] + " lit " + (clock - strike[i]).ToString("0.000") + " s after its last strike";
                }
                if (resolved[i]) continue;
                if ((float)nextEvent.GetValue(fixtures[i]) == due[i]) continue;
                // Its blink came this frame (lit if shown), inside the gap by the map's own sum.
                resolved[i] = true;
                if (clock - strike[i] < Gap) { inside++; if (lit) insideShown++; else refused++; }
                else { if (lit) outsideShown++; else outsideRefused++; }
            }
        }
        var open = 0;
        for (var i = 0; i < n; i++) if (switched[i] && !resolved[i]) open++;
        Check(switches >= n * 9 / 10 && notFresh == 0 && open == 0 && inside >= 20 && refused == inside && insideShown == 0 && early == 0
                && outsideShown >= 100 && outsideRefused == 0,
            "flicker Dead gap: " + switches + " of " + n + " lamps switched away and back to Dead on a strike's frame; " + inside + " had the next blink come within 4 s of it, "
            + refused + " refused (" + insideShown + " shown), " + early + " lit within 4 s" + (firstEarly != null ? " (first " + firstEarly + ")" : "")
            + "; " + outsideShown + " later shown (" + outsideRefused + " refused); " + open + " not yet due, " + notFresh + " not a fresh cycle");
    }
}
