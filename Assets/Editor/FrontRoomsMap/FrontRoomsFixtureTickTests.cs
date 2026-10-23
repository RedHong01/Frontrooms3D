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
/// M1 (lamp check 10_lamp_look_check.md), SetLampHold on every lamp, Failing to Dim and Stutter to Steady, both
/// tick paths, Reduce flashing off and on: the crossfade in takes 0.3 s (0.5 s) with no frame step over 0.10;
/// held, a lamp is the Dim or Steady formula bit for bit, through a chunk rebuild; let go, it waits its delay,
/// crosses back over 1 s or more, and is then its own mode bit for bit, the hold dropped; flashes stay at 2 a
/// second at most, the limiter never needed. M3: a Warn override with the tail attacks in 0.05 s and releases in
/// 0.30 s, fast at first, back to exactly 1; linear under Reduce flashing. M1 + M3 on a warning's room set
/// (FrontRoomsWarnLamps, a room of Failing, Stutter and Steady lamps, many entries, Reduce flashing off and on),
/// against the same runs with M1 off (main before it): at most 2 flashes a second on every lamp, no flicker
/// showing through, held lamps dipping with the rest, the let-go 2-6 s after stage 0 over 1 s or more, and the
/// envelope's timing and depths measured against the table.
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

        // V1 (lamp check): a Dip override reaches its lamp's lens as _FR_LampDip, and Verify checks every lit lens's dip.
        var eyeCell = world.CellOf(eye.position);
        var dipCell = eyeCell;
        var found = false;
        for (var r = 0; r <= 3 && !found; r++)
        for (var dy = -r; dy <= r && !found; dy++)
        for (var dx = -r; dx <= r && !found; dx++)
        {
            var c = new GridCoord(eyeCell.x + dx, eyeCell.y + dy);
            if (world.LampLevel(c) > .5f) { dipCell = c; found = true; }
        }
        Check(found, label + " - V1: a lit lamp within 3 cells of the eye");
        if (!found) return;
        Check(Mathf.Abs(world.LensDipForTools(dipCell) - 1f) < 1e-6f, label + " - V1: with no override the lens carries _FR_LampDip exactly 1 (" + world.LensDipForTools(dipCell).ToString("0.000000") + ")");
        var handle = world.SetLampOverride(dipCell, FrontRoomsMapWorld.LampFx.Dip, .55f, 2f);
        Verify("a Dip override at 0.55, 0.6 s in", 36);
        var dip = world.LensDipForTools(dipCell);
        Check(Mathf.Abs(dip - .55f) < 1e-3f, label + " - V1: the dipped lamp's lens carries _FR_LampDip " + dip.ToString("0.000") + " (0.55)");
        world.RemoveLampOverride(handle);
        Verify("the Dip released", 120);
        Check(Mathf.Abs(world.LensDipForTools(dipCell) - 1f) < 1e-6f, label + " - V1: back to exactly 1 once released (" + world.LensDipForTools(dipCell).ToString("0.000000") + ")");
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

        // M1's look hold on both paths, while both maps have run the same frames (the runs below are desktop only).
        LookHolds(desktop, webgl, cells);

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
        TailEnvelope(desktop, cells);
        WarnRoom(desktop);
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

    // ---------- M1: the look hold (FrontRoomsMapWorld.SetLampHold); M3: the release's tail ----------

    // The held look's own formula (Dim, else Steady) at a lamp's time.
    static float LookLevel(bool dim, float t) => dim ? .42f + .03f * Mathf.Sin(t * 90f) : .98f + .02f * Mathf.Sin(t * 110f);

    // A lamp's own Stutter or Failing level with no event (the formulas before M2), for the parity after a hold.
    static float OwnCalmLevel(bool failing, float t)
    {
        if (!failing) return .97f + .03f * Mathf.Sin(t * 120f);
        var wave = .5f + .5f * Mathf.Sin(t * 3.1f) * (.7f + .3f * Mathf.Sin(t * 1.27f));
        return (.25f + .45f * wave) * 1f;
    }

    // What one look-hold run found over its lamps.
    sealed class HoldRun
    {
        public string label;
        public int lamps, inFramesMin = int.MaxValue, inFramesMax, outFramesMin = int.MaxValue, lookFrames, lookWrong, ownFrames, ownWrong;
        public int delayWrong, notRemoved, rebuilt, rebuiltWrong, maxInWindow, held;
        public float maxInStep, maxInWeightStep, maxOutStep, maxOutWeightStep, maxRemoveStep;
        public double delayMin = double.MaxValue, delayMax;
        public string lookProblem, ownProblem;
        public ulong[] hashes;
    }

    /// <summary>
    /// M1 at the map: SetLampHold on every lamp (Failing to Dim, Stutter to Steady) on both tick paths, Reduce
    /// flashing off and on; the paths must agree bit for bit. The two maps must have run the same frames before
    /// (SetLampMode keeps a lamp's clock and RNG): a desktop-only run in between gives every lamp another clock,
    /// so its levels differ on any path (run after the desktop-only runs: 1597 of 1597 differed, and with Reduce
    /// flashing all but the 64 lamps of the chunk the first scenario rebuilt, new on both maps). The clocks are
    /// compared first, so a history mismatch shows as such.
    /// </summary>
    static void LookHolds(FrontRoomsMapWorld desktop, FrontRoomsMapWorld webgl, List<GridCoord> cells)
    {
        foreach (var reduced in new[] { false, true })
        {
            var name = reduced ? ", Reduce flashing" : "";
            var clocksDiffer = 0;
            foreach (var c in cells)
            {
                desktop.LampClockForTools(c, out var dc, out var dp);
                webgl.LampClockForTools(c, out var wc, out var wp);
                if (BitConverter.SingleToInt32Bits(dc) != BitConverter.SingleToInt32Bits(wc) || BitConverter.SingleToInt32Bits(dp) != BitConverter.SingleToInt32Bits(wp)) clocksDiffer++;
            }
            var d = HoldScenario(desktop, cells, reduced, false, "look hold desktop" + name);
            var w = HoldScenario(webgl, cells, reduced, true, "look hold WebGL" + name);
            JudgeHold(d, reduced);
            JudgeHold(w, reduced);
            var differ = 0;
            for (var i = 0; i < cells.Count; i++) if (d.hashes[i] != w.hashes[i]) differ++;
            Check(differ == 0 && clocksDiffer == 0, "look hold" + name + ": the desktop and WebGL paths give every held lamp the same levels, bit for bit (" + differ + " of " + cells.Count
                + " differ; the two maps' lamp clocks before it: " + clocksDiffer + " differ)");
        }
        FrontRoomsMapWorld.TickFixturesNearOnly = false;
        if (FrontRoomsSettings.ReduceFlashing) FrontRoomsSettings.SetReduceFlashing(false);
        foreach (var c in cells)
        {
            desktop.SetLampMode(c, ModuleLamp.Auto);
            webgl.SetLampMode(c, ModuleLamp.Auto);
        }
    }

    // Every lamp a fresh Failing (even index) or Stutter (odd) cycle, 2.5 s on its own; held (asked for 0.3 s) for
    // 3 s, its chunk rebuilt half way; let go with a delay of 2-6 s (an LCG) over 1.2 s; then 12 s more.
    static HoldRun HoldScenario(FrontRoomsMapWorld world, List<GridCoord> cells, bool reduced, bool nearOnly, string label)
    {
        FrontRoomsMapWorld.TickFixturesNearOnly = nearOnly;
        if (FrontRoomsSettings.ReduceFlashing != reduced) FrontRoomsSettings.SetReduceFlashing(reduced);
        const int Warm = 150, Held = 180, After = 720;
        const float Out = 1.2f;
        var n = cells.Count;
        var run = new HoldRun { label = label, lamps = n, hashes = new ulong[n] };
        var dim = new bool[n];
        var watch = new LampWatch[n];
        var heldBefore = new int[n];
        var last = new float[n];
        var lastWeight = new float[n];
        var inFrames = new int[n];
        var rampStart = new int[n];
        var wasHeld = new bool[n];
        var delay = new double[n];
        for (var i = 0; i < n; i++)
        {
            dim[i] = i % 2 == 0;
            world.SetLampMode(cells[i], ModuleLamp.Steady);
            world.SetLampMode(cells[i], dim[i] ? ModuleLamp.Failing : ModuleLamp.Stutter);
            watch[i] = new LampWatch { cell = cells[i] };
            world.LampAmbientForTools(cells[i], out _, out _, out heldBefore[i]);
            rampStart[i] = -1;
            run.hashes[i] = FnvOffset;
        }
        var chunk = MapGrid.ChunkOf(cells[0]);
        var lcg = 4049u;
        var time = 0.0;
        for (var frame = 0; frame < Warm + Held + After; frame++)
        {
            if (frame == Warm)
                for (var i = 0; i < n; i++) world.SetLampHold(cells[i], dim[i] ? ModuleLamp.Dim : ModuleLamp.Steady, .3f);
            if (frame == Warm + Held / 2)
            {
                world.RebuildChunk(chunk);
                for (var i = 0; i < n; i++)
                {
                    if (MapGrid.ChunkOf(cells[i]) != chunk) continue;
                    run.rebuilt++;
                    if (!world.LampHeld(cells[i])) run.rebuiltWrong++;
                }
            }
            if (frame == Warm + Held)
                for (var i = 0; i < n; i++)
                {
                    lcg = lcg * 1664525u + 1013904223u;
                    delay[i] = 2.0 + 4.0 * ((lcg >> 8) / 16777216.0);
                    world.ReleaseLampHold(cells[i], (float)delay[i], Out);
                }
            time += Dt;
            world.TickFixturesForTools(Dt);
            for (var i = 0; i < n; i++)
            {
                var c = cells[i];
                var v = world.LampLevel(c);
                run.hashes[i] = Fnv(run.hashes[i], BitConverter.SingleToInt32Bits(v));
                Count(watch[i], v, time);
                var held = world.LampHoldForTools(c, out var weight, out _, out var releasing, out var left);
                world.LampClockForTools(c, out var clock, out var phase);
                world.LampAmbientForTools(c, out var gain, out var active, out _);
                var t = clock + phase;
                var step = frame > 0 ? Mathf.Abs(v - last[i]) : 0f;
                if (frame >= Warm && held && !releasing && inFrames[i] >= 0)
                {
                    // The crossfade in: from the hold's first frame to the one it reaches the look.
                    inFrames[i]++;
                    run.maxInStep = Mathf.Max(run.maxInStep, step);
                    run.maxInWeightStep = Mathf.Max(run.maxInWeightStep, weight - (inFrames[i] == 1 ? 0f : lastWeight[i]));
                    if (weight >= 1f)
                    {
                        run.inFramesMin = Mathf.Min(run.inFramesMin, inFrames[i]);
                        run.inFramesMax = Mathf.Max(run.inFramesMax, inFrames[i]);
                        inFrames[i] = -1;
                    }
                }
                if (held && weight >= 1f && !(releasing && left <= 0f))
                {
                    run.lookFrames++;
                    var look = LookLevel(dim[i], t);
                    if (BitConverter.SingleToInt32Bits(v) != BitConverter.SingleToInt32Bits(look))
                    {
                        run.lookWrong++;
                        if (run.lookProblem == null) run.lookProblem = c + " at " + time.ToString("0.000") + " s: " + v.ToString("R") + ", look " + look.ToString("R");
                    }
                }
                if (held && releasing && left <= 0f)
                {
                    if (rampStart[i] < 0 && weight < 1f)
                    {
                        rampStart[i] = frame;
                        var waited = (frame - (Warm + Held) + 1) * (double)Dt;
                        run.delayMin = Math.Min(run.delayMin, waited);
                        run.delayMax = Math.Max(run.delayMax, waited);
                        if (Math.Abs(waited - delay[i]) > 2.5 * Dt) run.delayWrong++;
                    }
                    if (rampStart[i] >= 0)
                    {
                        run.maxOutStep = Mathf.Max(run.maxOutStep, step);
                        run.maxOutWeightStep = Mathf.Max(run.maxOutWeightStep, lastWeight[i] - weight);
                        if (weight <= 0f && lastWeight[i] > 0f) run.outFramesMin = Mathf.Min(run.outFramesMin, frame - rampStart[i] + 1);
                    }
                }
                if (wasHeld[i] && !held && !active) run.maxRemoveStep = Mathf.Max(run.maxRemoveStep, step);
                if (!held && wasHeld[i] == false && frame > Warm + Held && rampStart[i] >= 0 && !active && gain == 1f)
                {
                    // Back to its own mode: bit-identical to it between its events.
                    run.ownFrames++;
                    var own = OwnCalmLevel(dim[i], t);
                    if (BitConverter.SingleToInt32Bits(v) != BitConverter.SingleToInt32Bits(own))
                    {
                        run.ownWrong++;
                        if (run.ownProblem == null) run.ownProblem = c + " at " + time.ToString("0.000") + " s: " + v.ToString("R") + ", own " + own.ToString("R");
                    }
                }
                last[i] = v;
                lastWeight[i] = held ? weight : 0f;
                wasHeld[i] = held;
            }
        }
        for (var i = 0; i < n; i++)
        {
            if (world.LampHeld(cells[i])) run.notRemoved++;
            run.maxInWindow = Mathf.Max(run.maxInWindow, watch[i].maxInWindow);
            world.LampAmbientForTools(cells[i], out _, out _, out var h);
            run.held += h - heldBefore[i];
        }
        report.flicker.Add(run.label + ": " + n + " lamps; in " + run.inFramesMin + "-" + run.inFramesMax + " frames, largest step " + run.maxInStep.ToString("0.000")
            + "; held frames " + run.lookFrames + " (" + run.lookWrong + " off the look); let go after " + run.delayMin.ToString("0.00") + "-" + run.delayMax.ToString("0.00") + " s over "
            + run.outFramesMin + "+ frames, largest step " + run.maxOutStep.ToString("0.000") + ", hand-back step " + run.maxRemoveStep.ToString("0.000") + "; max " + (run.maxInWindow / 2f).ToString("0.0")
            + " flashes/s; limiter held " + run.held);
        return run;
    }

    static void JudgeHold(HoldRun r, bool reduced)
    {
        var inSeconds = reduced ? FrontRoomsMapWorld.ReducedFlashingEnvelopeSeconds : .3f;
        var inTarget = inSeconds / Dt;
        Check(r.inFramesMin >= inTarget - 1.01f && r.inFramesMax <= inTarget + 1.01f && r.maxInWeightStep <= Dt / inSeconds + 1e-4f && r.maxInStep <= .10f,
            r.label + ": the crossfade in takes " + r.inFramesMin + "-" + r.inFramesMax + " frames (" + inSeconds.ToString("0.0") + " s = " + inTarget.ToString("0") + "), at most "
            + r.maxInWeightStep.ToString("0.0000") + " of the way a frame; largest level step " + r.maxInStep.ToString("0.000") + " (no frame step over 0.10; the old snap was 0.36-0.96)");
        Check(r.lookFrames > 0 && r.lookWrong == 0 && r.rebuilt > 0 && r.rebuiltWrong == 0,
            r.label + ": held, every lamp is the Dim (Failing) or Steady (Stutter) formula bit for bit on " + r.lookFrames + " lamp-frames (" + r.lookWrong + " not"
            + (r.lookProblem != null ? "; first " + r.lookProblem : "") + "); a rebuilt chunk keeps its " + r.rebuilt + " holds (" + r.rebuiltWrong + " lost)");
        Check(r.delayWrong == 0 && r.delayMin >= 2.0 && r.delayMax <= 6.0 + 3.0 * Dt && r.outFramesMin >= 60 && r.maxOutWeightStep <= Dt / 1f + 1e-4f && r.maxOutStep <= .10f && r.maxRemoveStep <= .10f,
            r.label + ": let go, each waits its delay (" + r.delayMin.ToString("0.00") + "-" + r.delayMax.ToString("0.00") + " s, " + r.delayWrong + " off), then crosses back over "
            + r.outFramesMin + " frames or more (1 s = 60) at most " + r.maxOutWeightStep.ToString("0.0000") + " a frame; largest level step " + r.maxOutStep.ToString("0.000")
            + ", at the hand-back " + r.maxRemoveStep.ToString("0.000") + " (at most 0.10)");
        Check(r.notRemoved == 0 && r.ownFrames > 0 && r.ownWrong == 0,
            r.label + ": every hold dropped (" + r.notRemoved + " left), then each lamp is its own mode bit for bit on " + r.ownFrames + " lamp-frames with no event ("
            + r.ownWrong + " not" + (r.ownProblem != null ? "; first " + r.ownProblem : "") + ")");
        Check(r.maxInWindow <= 4 && r.held == 0,
            r.label + ": at most " + (r.maxInWindow / 2f).ToString("0.0") + " flashes in any 1 s over the crossfades (design 2, limit 3); the flash limiter never had to hold (" + r.held + ")");
    }

    /// <summary>
    /// M3: a Warn override with the tail (attack 0.05 s, hold 0.06 s, release 0.30 s, tail 0.09 s) on one Steady lamp:
    /// down in 0.05 s, back in 0.30 s, most of the way back by half the release, exactly 1 on time; under Reduce
    /// flashing the same override is a linear 0.5 s each way.
    /// </summary>
    static void TailEnvelope(FrontRoomsMapWorld world, List<GridCoord> cells)
    {
        FrontRoomsMapWorld.TickFixturesNearOnly = false;
        var c = cells[0];
        foreach (var reduced in new[] { false, true })
        {
            if (FrontRoomsSettings.ReduceFlashing != reduced) FrontRoomsSettings.SetReduceFlashing(reduced);
            world.SetLampMode(c, ModuleLamp.Steady);
            for (var k = 0; k < 900 && (world.LampOverrideCount > 0 || world.LampHeld(c)); k++) world.TickFixturesForTools(Dt);
            const float Depth = .55f, Hold = .06f;
            world.SetLampOverride(c, FrontRoomsMapWorld.LampFx.Warn, Depth, Hold,
                new FrontRoomsMapWorld.LampEnvelope(FrontRoomsWarnLamps.DipAttack, FrontRoomsWarnLamps.DipRelease, FrontRoomsWarnLamps.DipTail));
            var ratios = new List<float>();
            for (var k = 0; k < 240; k++)
            {
                world.TickFixturesForTools(Dt);
                ratios.Add(world.LampLevel(c) / world.LampBaseLevel(c));
                if (world.LampOverrideCount == 0) break;
            }
            int firstTrough = -1, lastTrough = -1;
            for (var k = 0; k < ratios.Count; k++)
            {
                if (Mathf.Abs(ratios[k] - Depth) > 1e-4f) continue;
                if (firstTrough < 0) firstTrough = k;
                lastTrough = k;
            }
            var end = ratios.Count - 1;
            var exact = ratios[end] == 1f && world.LampOverrideCount == 0;
            var attack = (firstTrough + 1) * Dt;
            var hold = (lastTrough - firstTrough + 1) * Dt;
            var release = (end - lastTrough) * Dt;
            var half = lastTrough >= 0 ? (ratios[lastTrough + (end - lastTrough) / 2] - Depth) / (1f - Depth) : 0f;
            var label = "M3 release tail" + (reduced ? ", Reduce flashing" : "") + ": ";
            var text = "down in " + attack.ToString("0.000") + " s, at " + Depth + " for " + hold.ToString("0.000") + " s, back in " + release.ToString("0.000") + " s, "
                + (half * 100f).ToString("0") + " % of the way back at half the release, exactly 1 after (" + exact + ")";
            if (!reduced)
                Check(firstTrough >= 0 && Mathf.Abs(attack - FrontRoomsWarnLamps.DipAttack) <= Dt + 1e-4f && Mathf.Abs(hold - Hold) <= 1.5f * Dt
                      && Mathf.Abs(release - FrontRoomsWarnLamps.DipRelease) <= 1.5f * Dt && half >= .75f && exact,
                    label + text + " (table: 0.05, 0.04-0.08, 0.30 with the tail; linear would be 50 %)");
            else
                Check(firstTrough >= 0 && Mathf.Abs(attack - FrontRoomsMapWorld.ReducedFlashingEnvelopeSeconds) <= Dt + 1e-4f
                      && Mathf.Abs(release - FrontRoomsMapWorld.ReducedFlashingEnvelopeSeconds) <= 1.5f * Dt && Mathf.Abs(half - .5f) <= .05f && exact,
                    label + text + " (0.5 s each way, linear: no tail under Reduce flashing)");
        }
        if (FrontRoomsSettings.ReduceFlashing) FrontRoomsSettings.SetReduceFlashing(false);
        world.SetLampMode(c, ModuleLamp.Auto);
    }

    // ---------- M1 + M3 on a warning's room set (FrontRoomsWarnLamps driving the map) ----------

    const int WarnTrials = 6;

    // What one variant (M1 on or off, Reduce flashing off or on) found over its trials.
    sealed class WarnRun
    {
        public string label;
        public int bursts, lampsInBursts, minBurstLamps = int.MaxValue, maxBurstLamps;
        public int maxSteady, maxFailing, maxStutter, over2, over3, held;
        public int showThrough, flickerDips, heldFrames, heldWrong, leftHeld, rfDoubleSag;
        public float maxEdgeStep;
        public double restoreMin = double.MaxValue, restoreMax;
        public int restores, restoreRampMin = int.MaxValue;
        public int dips, firstDips, laterDips;
        public float firstMin = 1f, firstMax, laterMin = 1f, laterMax, attackMin = 9f, attackMax, holdMin = 9f, holdMax, releaseMin = 9f, releaseMax;
    }

    /// <summary>
    /// A room near the spawn with 6 live lamps or more (else 4), turned Failing, Stutter, Steady in turn; warnings
    /// played on it by FrontRoomsWarnLamps with M1 off (main before it) and on, Reduce flashing off and on.
    /// </summary>
    static void WarnRoom(FrontRoomsMapWorld world)
    {
        FrontRoomsMapWorld.TickFixturesNearOnly = false;
        if (FrontRoomsSettings.ReduceFlashing) FrontRoomsSettings.SetReduceFlashing(false);
        var mid = world.CellOf(world.SpawnWorldPosition);
        var far = world.CellCenter(new GridCoord(mid.x + 40, mid.y + 40));
        var probe = new FrontRoomsWarnLamps(world, 1);
        var lamps = new List<GridCoord>();
        var at = Vector3.zero;
        foreach (var least in new[] { 6, 4 })
        for (var y = mid.y - 12; y <= mid.y + 12 && lamps.Count == 0; y++)
        for (var x = mid.x - 12; x <= mid.x + 12 && lamps.Count == 0; x++)
        {
            var c = new GridCoord(x, y);
            if (!world.IsBuilt(c)) continue;
            probe.RoomForTools(world.CellCenter(c), far);
            if (probe.RoomSet.Count < 4 || probe.LiveLamps.Count < least) continue;
            lamps.AddRange(probe.LiveLamps);
            at = world.CellCenter(c);
        }
        if (lamps.Count == 0) { Check(false, "warn room: no room with 4 live lamps near the spawn"); return; }
        var n = lamps.Count;
        var kind = new int[n];
        var index = new Dictionary<GridCoord, int>();
        for (var i = 0; i < n; i++)
        {
            // The map's modes 0..2: Steady, Stutter, Failing.
            kind[i] = i % 3 == 0 ? 2 : i % 3 == 1 ? 1 : 0;
            world.SetLampMode(lamps[i], ModuleLamp.Dim);
            world.SetLampMode(lamps[i], kind[i] == 2 ? ModuleLamp.Failing : kind[i] == 1 ? ModuleLamp.Stutter : ModuleLamp.Steady);
            index[lamps[i]] = i;
        }
        probe.RoomForTools(at, far);
        var allLive = probe.LiveLamps.Count == n;
        foreach (var reduced in new[] { false, true })
        {
            var before = WarnScenario(world, lamps, kind, index, at, far, reduced, false);
            var after = WarnScenario(world, lamps, kind, index, at, far, reduced, true);
            JudgeWarn(before, after, reduced, n, allLive);
        }
        if (FrontRoomsSettings.ReduceFlashing) FrontRoomsSettings.SetReduceFlashing(false);
        foreach (var c in lamps) world.SetLampMode(c, ModuleLamp.Auto);
        for (var k = 0; k < 900 && world.LampHoldCount > 0; k++) world.TickFixturesForTools(Dt);
    }

    // WarnTrials warnings, each with its own seed and a longer stage 0 first (so stage 1 meets the lamps' own
    // flicker at other points): stage 1 for 12 s, stage 2 for 5 s, stage 1 for 3 s, stage 0 for 11 s; every lamp
    // of the room watched every frame by this file's WCAG counter (overrides included).
    static WarnRun WarnScenario(FrontRoomsMapWorld world, List<GridCoord> lamps, int[] kind, Dictionary<GridCoord, int> index, Vector3 at, Vector3 far, bool reduced, bool m1)
    {
        if (FrontRoomsSettings.ReduceFlashing != reduced) FrontRoomsSettings.SetReduceFlashing(reduced);
        var n = lamps.Count;
        var run = new WarnRun { label = "warn room, " + (m1 ? "M1 + M3" : "M1 off (main), M3") + (reduced ? ", Reduce flashing" : "") };
        var watch = new LampWatch[n];
        var heldBefore = new int[n];
        var lastBase = new float[n];
        var wasHeld = new bool[n];
        var lastDip = new double[n];
        var fullSince = new double[n];
        var rampStart = new int[n];
        var restoreSeen = new bool[n];
        var dipStart = new int[n];
        var dipTime = new double[n];
        var prevDip = new double[n];
        var troughFirst = new int[n];
        var troughLast = new int[n];
        var trough = new float[n];
        var dipCut = new bool[n];
        for (var i = 0; i < n; i++)
        {
            watch[i] = new LampWatch { cell = lamps[i] };
            world.LampAmbientForTools(lamps[i], out _, out _, out heldBefore[i]);
            lastDip[i] = prevDip[i] = double.NegativeInfinity;
            fullSince[i] = double.PositiveInfinity;
            rampStart[i] = dipStart[i] = -1;
        }
        var time = 0.0;
        var frame = 0;
        var stage = 0;
        var stage0At = double.NaN;
        var lastSegment = false;
        Action<GridCoord, Vector3> dipped = (c, p) =>
        {
            if (!index.TryGetValue(c, out var i)) return;
            if (kind[i] != 0) run.flickerDips++;
            if (reduced && time - lastDip[i] < 1.5) run.rfDoubleSag++;
            lastDip[i] = time;
        };
        world.LampDipped += dipped;
        try
        {
            for (var k = 0; k < WarnTrials; k++)
            {
                var warn = new FrontRoomsWarnLamps(world, 9001 + 37 * k) { HoldFlickeringForTools = m1 };
                warn.WarnBurst += (centre, count) =>
                {
                    run.bursts++;
                    run.lampsInBursts += count;
                    run.minBurstLamps = Mathf.Min(run.minBurstLamps, count);
                    run.maxBurstLamps = Mathf.Max(run.maxBurstLamps, count);
                };
                var plan = new[] { 0f, .7f + .53f * k, 1f, 12f, 2f, 5f, 1f, 3f, 0f, 11f };
                for (var s = 0; s < plan.Length; s += 2)
                {
                    var segStage = (int)plan[s];
                    var frames = Mathf.RoundToInt(plan[s + 1] / Dt);
                    lastSegment = s == plan.Length - 2;
                    if (lastSegment) for (var i = 0; i < n; i++) { restoreSeen[i] = false; rampStart[i] = -1; }
                    for (var f = 0; f < frames; f++)
                    {
                        var entering0 = segStage == 0 && stage != 0;
                        stage = segStage;
                        time += Dt;
                        frame++;
                        if (entering0) stage0At = time;
                        warn.Tick(Dt, stage, false, at, far);
                        world.TickFixturesForTools(Dt);
                        for (var i = 0; i < n; i++)
                            WatchWarnLamp(world, run, lamps[i], i, kind[i], watch[i], reduced, stage, time, frame, stage0At, lastSegment,
                                lastBase, wasHeld, lastDip, fullSince, rampStart, restoreSeen, dipStart, dipTime, prevDip, troughFirst, troughLast, trough, dipCut);
                    }
                }
                run.leftHeld += world.LampHoldCount;
                warn.Reset();
                for (var f = 0; f < 240 && world.LampHoldCount > 0; f++) { time += Dt; frame++; world.TickFixturesForTools(Dt); }
            }
        }
        finally
        {
            world.LampDipped -= dipped;
        }
        for (var i = 0; i < n; i++)
        {
            var m = watch[i].maxInWindow;
            if (kind[i] == 0) run.maxSteady = Mathf.Max(run.maxSteady, m);
            else if (kind[i] == 1) run.maxStutter = Mathf.Max(run.maxStutter, m);
            else run.maxFailing = Mathf.Max(run.maxFailing, m);
            if (m > 4) run.over2++;
            if (m > 6) run.over3++;
            world.LampAmbientForTools(lamps[i], out _, out _, out var h);
            run.held += h - heldBefore[i];
        }
        return run;
    }

    static void WatchWarnLamp(FrontRoomsMapWorld world, WarnRun run, GridCoord c, int i, int kind, LampWatch w, bool reduced, int stage, double time, int frame, double stage0At,
        bool lastSegment, float[] lastBase, bool[] wasHeld, double[] lastDip, double[] fullSince, int[] rampStart, bool[] restoreSeen, int[] dipStart, double[] dipTime,
        double[] prevDip, int[] troughFirst, int[] troughLast, float[] trough, bool[] dipCut)
    {
        var v = world.LampLevel(c);
        var b = world.LampBaseLevel(c);
        var changes = w.changes;
        Count(w, v, time);
        var counted = w.changes > changes;
        if (kind != 0)
        {
            var held = world.LampHoldForTools(c, out var weight, out _, out var releasing, out var delay);
            world.LampAmbientForTools(c, out _, out var active, out _);
            var full = held && weight >= 1f && !(releasing && delay <= 0f);
            if (full && double.IsPositiveInfinity(fullSince[i])) fullSince[i] = time;
            if (!full) fullSince[i] = double.PositiveInfinity;
            var edge = (held && !full) || (wasHeld[i] && !held);
            // The hold's edges (its crossfades and the hand-back; an own event starting on the hand-back frame is the lamp's own).
            if (edge && frame > 1 && !(wasHeld[i] && !held && active)) run.maxEdgeStep = Mathf.Max(run.maxEdgeStep, Mathf.Abs(b - lastBase[i]));
            if (full)
            {
                world.LampClockForTools(c, out var clock, out var phase);
                run.heldFrames++;
                if (BitConverter.SingleToInt32Bits(b) != BitConverter.SingleToInt32Bits(LookLevel(kind == 2, clock + phase))) run.heldWrong++;
            }
            // Own flicker in a warning: a change on a Stutter or Failing lamp away from its dips and its crossfades.
            var settled = !held || (full && time - fullSince[i] > .2);
            if (stage >= 1 && counted && settled && time - lastDip[i] > (reduced ? 1.4 : .7)) run.showThrough++;
            if (lastSegment && held && releasing && delay <= 0f && weight < 1f && !restoreSeen[i])
            {
                restoreSeen[i] = true;
                rampStart[i] = frame;
                var e = time - stage0At;
                run.restores++;
                run.restoreMin = Math.Min(run.restoreMin, e);
                run.restoreMax = Math.Max(run.restoreMax, e);
            }
            if (rampStart[i] >= 0 && (!held || weight <= 0f))
            {
                run.restoreRampMin = Mathf.Min(run.restoreRampMin, frame - rampStart[i] + 1);
                rampStart[i] = -1;
            }
            wasHeld[i] = held;
        }
        else if (!reduced)
        {
            // The envelope (M3) on a Steady lamp: a dip runs from its first frame under 1 to its first frame back at exactly 1.
            var r = b > 0f ? v / b : 1f;
            if (r < 1f)
            {
                if (dipStart[i] < 0)
                {
                    dipStart[i] = frame;
                    dipTime[i] = time;
                    trough[i] = r;
                    troughFirst[i] = troughLast[i] = frame;
                    dipCut[i] = false;
                }
                else if (r < trough[i] - 1e-5f)
                {
                    trough[i] = r;
                    troughFirst[i] = troughLast[i] = frame;
                }
                else if (Mathf.Abs(r - trough[i]) <= 1e-5f) troughLast[i] = frame;
                if (stage == 0) dipCut[i] = true;
            }
            else if (dipStart[i] >= 0)
            {
                if (!dipCut[i])
                {
                    run.dips++;
                    var attack = (troughFirst[i] - dipStart[i] + 1) * Dt;
                    var hold = (troughLast[i] - troughFirst[i] + 1) * Dt;
                    var release = (frame - troughLast[i]) * Dt;
                    run.attackMin = Mathf.Min(run.attackMin, attack);
                    run.attackMax = Mathf.Max(run.attackMax, attack);
                    run.holdMin = Mathf.Min(run.holdMin, hold);
                    run.holdMax = Mathf.Max(run.holdMax, hold);
                    run.releaseMin = Mathf.Min(run.releaseMin, release);
                    run.releaseMax = Mathf.Max(run.releaseMax, release);
                    if (dipTime[i] - prevDip[i] > 1.0)
                    {
                        run.firstDips++;
                        run.firstMin = Mathf.Min(run.firstMin, trough[i]);
                        run.firstMax = Mathf.Max(run.firstMax, trough[i]);
                    }
                    else
                    {
                        run.laterDips++;
                        run.laterMin = Mathf.Min(run.laterMin, trough[i]);
                        run.laterMax = Mathf.Max(run.laterMax, trough[i]);
                    }
                }
                prevDip[i] = dipTime[i];
                dipStart[i] = -1;
            }
        }
        lastBase[i] = b;
    }

    static string WarnSummary(WarnRun r)
    {
        var mean = r.bursts > 0 ? r.lampsInBursts / (double)r.bursts : 0.0;
        return r.label + ": " + r.bursts + " bursts of " + (r.minBurstLamps == int.MaxValue ? 0 : r.minBurstLamps) + "-" + r.maxBurstLamps + " lamps (mean " + mean.ToString("0.0")
            + "); max flashes/s Steady " + (r.maxSteady / 2f).ToString("0.0") + ", Failing " + (r.maxFailing / 2f).ToString("0.0") + ", Stutter " + (r.maxStutter / 2f).ToString("0.0")
            + "; lamps over 2 " + r.over2 + ", over 3 " + r.over3 + "; limiter held " + r.held + "; own changes showing through a warning " + r.showThrough
            + "; dips on Stutter/Failing lamps " + r.flickerDips + "; held frames " + r.heldFrames + " (" + r.heldWrong + " off the look); largest step at a hold's edge "
            + r.maxEdgeStep.ToString("0.000") + "; let-go " + (r.restores > 0 ? r.restoreMin.ToString("0.00") + "-" + r.restoreMax.ToString("0.00") + " s after stage 0 over "
            + r.restoreRampMin + "+ frames" : "none") + "; holds left at the end " + r.leftHeld
            + (r.dips > 0 ? "; envelope on " + r.dips + " Steady dips: attack " + r.attackMin.ToString("0.000") + "-" + r.attackMax.ToString("0.000") + " s, hold " + r.holdMin.ToString("0.000")
                + "-" + r.holdMax.ToString("0.000") + " s, release " + r.releaseMin.ToString("0.000") + "-" + r.releaseMax.ToString("0.000") + " s, first dips " + r.firstDips + " at "
                + r.firstMin.ToString("0.000") + "-" + r.firstMax.ToString("0.000") + ", later " + r.laterDips + " at " + r.laterMin.ToString("0.000") + "-" + r.laterMax.ToString("0.000") : "")
            + (r.rfDoubleSag > 0 ? "; second sag within 1.5 s " + r.rfDoubleSag : "");
    }

    static void JudgeWarn(WarnRun before, WarnRun after, bool reduced, int n, bool allLive)
    {
        report.flicker.Add(WarnSummary(before));
        report.flicker.Add(WarnSummary(after));
        var rf = reduced ? ", Reduce flashing" : "";
        Check(after.bursts >= WarnTrials * 2 && after.maxSteady <= 4 && after.maxFailing <= 4 && after.maxStutter <= 4 && after.over3 == 0 && after.held == 0,
            "warn room" + rf + ": with M1 + M3, " + after.bursts + " bursts, every one of the room's " + n + " lamps stays at 2 flashes a second or under (Steady "
            + (after.maxSteady / 2f).ToString("0.0") + ", held Failing " + (after.maxFailing / 2f).ToString("0.0") + ", held Stutter " + (after.maxStutter / 2f).ToString("0.0")
            + "; over 2: " + after.over2 + ", over 3: " + after.over3 + "), the flash limiter never held (" + after.held + "); M1 off: " + (before.maxSteady / 2f).ToString("0.0")
            + " / " + (before.maxFailing / 2f).ToString("0.0") + " / " + (before.maxStutter / 2f).ToString("0.0"));
        Check(after.showThrough == 0 && after.heldFrames > 0 && after.heldWrong == 0 && after.maxEdgeStep <= .10f,
            "warn room" + rf + ": no own flicker shows through a warning (" + after.showThrough + " changes; M1 off " + before.showThrough + "); held lamps are the Dim / Steady formula bit for bit ("
            + after.heldWrong + " of " + after.heldFrames + " frames off); largest one-frame step at a hold's edges " + after.maxEdgeStep.ToString("0.000") + " (at most 0.10; the old snap 0.36-0.96)");
        var meanBefore = before.bursts > 0 ? before.lampsInBursts / (double)before.bursts : 0.0;
        Check(allLive && after.minBurstLamps == n && after.maxBurstLamps == n && after.flickerDips > 0 && before.flickerDips == 0 && meanBefore < n,
            "warn room" + rf + ": held lamps dip with the rest: every burst on all " + n + " lamps (" + after.minBurstLamps + "-" + after.maxBurstLamps + "), " + after.flickerDips
            + " dips on Stutter/Failing lamps; M1 off: " + meanBefore.ToString("0.0") + " lamps a burst, " + before.flickerDips + " on them");
        Check(after.restores > 0 && after.restoreMin >= FrontRoomsWarnLamps.RestoreMin - Dt && after.restoreMax <= FrontRoomsWarnLamps.RestoreMax + 3.0 * Dt && after.restoreRampMin >= 60 && after.leftHeld == 0,
            "warn room" + rf + ": " + after.restores + " let-gos " + after.restoreMin.ToString("0.00") + "-" + after.restoreMax.ToString("0.00") + " s after stage 0 (2-6), each back over "
            + after.restoreRampMin + " frames or more (1 s = 60); every hold dropped by the end of stage 0 (" + after.leftHeld + " left)");
        if (!reduced)
            Check(after.firstDips >= 10 && after.laterDips >= 10 && after.attackMin >= FrontRoomsWarnLamps.DipAttack - Dt - 1e-4f && after.attackMax <= FrontRoomsWarnLamps.DipAttack + Dt + 1e-4f
                  && after.holdMin >= FrontRoomsWarnLamps.DipHoldMin - Dt - 1e-4f && after.holdMax <= FrontRoomsWarnLamps.DipHoldMax + Dt + 1e-4f
                  && after.releaseMin >= FrontRoomsWarnLamps.DipRelease - 1.5f * Dt && after.releaseMax <= FrontRoomsWarnLamps.DipRelease + 1.5f * Dt
                  && after.firstMin >= FrontRoomsWarnLamps.FirstDepthMin - 1e-3f && after.firstMax <= FrontRoomsWarnLamps.FirstDepthMax + 1e-3f
                  && after.laterMin >= FrontRoomsWarnLamps.LaterDepthMin - 1e-3f && after.laterMax <= FrontRoomsWarnLamps.LaterDepthMax + 1e-3f,
                "warn room: the envelope as measured on " + after.dips + " Steady dips matches the table: attack " + after.attackMin.ToString("0.000") + "-" + after.attackMax.ToString("0.000")
                + " s (0.05), hold " + after.holdMin.ToString("0.000") + "-" + after.holdMax.ToString("0.000") + " s (0.04-0.08), release " + after.releaseMin.ToString("0.000") + "-"
                + after.releaseMax.ToString("0.000") + " s (0.30), first dips " + after.firstMin.ToString("0.000") + "-" + after.firstMax.ToString("0.000") + " (0.55-0.62, " + after.firstDips
                + "), later " + after.laterMin.ToString("0.000") + "-" + after.laterMax.ToString("0.000") + " (0.65-0.75, " + after.laterDips + ")");
        else
            Check(after.rfDoubleSag == 0 && before.rfDoubleSag == 0 && after.bursts > 0,
                "warn room, Reduce flashing: one sag per lamp per burst, M1 on and off (" + after.rfDoubleSag + ", " + before.rfDoubleSag + " lamps with a second within 1.5 s)");
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
