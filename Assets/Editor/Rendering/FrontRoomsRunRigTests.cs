using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Run! B (EMERGENCY POWER) rig checks, gate T2 of research/room_visuals/run_b_production/10_plan.md.
/// Edit mode, headless, no menu item and no InitializeOnLoad hook:
///   Unity -batchmode -projectPath <project> -buildTarget OSXUniversal -executeMethod FrontRoomsRunRigTests.RunBatch -quit [-runRigTestOut dir]
/// Default output: <project>/Verification/run_b/tests (tests.json, log.txt).
///   R1  Evaluate(t) is deterministic (two rigs, any order of calls).
///   R2  photosafety per lamp (WCAG 2.3.1 general flash: a change of 0.1 from the last extreme, darker side
///       below 0.8, a flash is a pair): trip, animated reset, stutter tube, stage-1 warning; at most 3 a second.
///   R3  REDUCE FLASHING: envelopes of at least 0.5 s, no black gap (the brighter state never under 15 %), no flash
///       pair (each lamp changes once, monotone), a monotone re-strike, a steady stutter.
///   R4  the trip is one-shot; R5  the reset is idempotent; R6  Tick allocates nothing.
///   R7  corridor light budget (plan section 3, C41 + C42, F7): armed 10 (9 wraps + the goal window light; every wrap
///       shadowed: 9), blackout 1 (the goal window light), tripped 9 (8 heads shadowed + the goal window light).
///   R8  C41: no EXIT sign anywhere in the room (no FrontRoomsExitSign, no Kit_ExitSign*); the red fire gear stays.
///   R9  C42: the goal window light: unshadowed, lit from the trip line though past the 16 m cull, off as the far lamp comes
///       on (FarRoomLamp 1), half at 0.5, its no-lamp fade near the door, off once the viewer is past the goal wall,
///       untouched by the trip.
///   R10 side doors on the RN-K DA kits: 3 frames + 3 leaves, every lite DEAD, no closer, the frame inside the wall.
///   R11 CR-4 warning burst on the wraps: at most 3 flashes a second, ends armed and whole; REDUCE FLASHING one sag >= 0.69.
///   R12 the map skin (skinShell, the binder's layout): the soffit skin, the arch reveal on the room's half of the wall
///       (2 jambs x lower tone, cove, upper tone + the head), none at the goal door, and the lower tone running on behind
///       the extinguisher cabinet (the map's CR-3 finish shows nowhere).
///   R13 F1 (stage C): nothing culls while the viewer is in the room (trip line: 8 heads + the goal window light); 25 m
///       outside the arch only the goal head runs; farther than the box fade, each light follows the map's 16 m / 3 m fade.
///   R14 F3 (stage C): the reset seen as the frame sees it: the whole corridor and every run of 3 neighbouring wraps, the
///       window's mean against its own maximum, at most 3 flashes a second (the design gives 2); one flick per wrap.
///   R15 step 10 + C39 (stage C): every gateable material on the rig's and the dress's renderers is a gated twin with the
///       room's box in its block; the NOTICE plate outside is not; the goal opening is the lite while shut, the door open.
///   R16 F7 + C39 (stage C): three Run_B volumes (main blend 0.3, two mouths blend 1); a camera in a side neighbour
///       (1.88 m off the axis) is beyond every volume's reach; the Run_B profile overrides every parameter the Office grade
///       does, and SMH / film grain / chromatic aberration with the global look's values (a Level 0 host is unchanged).
///   R17 F8 + F9 + F2 + F6 (stage C): the NOTICE plate under the room root on the host face, the overhead services cast no
///       shadow, the chair and cart albedo well under the floor's, the five LOD groups switching at the new distances.
///   R18 card 1 (71 s.9, the rig in cells): today's room unchanged: Corridor(9) and Corridor1x9 equal the preset as it landed
///       (frozen below) field for field, bit for bit, in three width pairs; the dress (look-dev shell and the map skin) and the
///       rig built from each place the same pieces and lights; the binder's CellsOf reads 9 from today's record and by default.
///   R19 card 1, 7 .. 14 cells: a strip every cell, units every 6 m from z 3 (one 9 m interval, the last, at an even count), the
///       goal unit at L - 6, the goal at L, probes by segment covering 0 .. L on the 9-cell bake, an obstacle in every pool but
///       the goal's (lane >= 1.4 m), the side doors and cabinets by rule, frames only when asked (every third cell line), the
///       lights (armed n + 1 / n shadowed, tripped 2U + 1 / 2U), the trip and the reset photosafe, the reset n x 0.15 s of sweep
///       from the entrance (ticked); 6 and 15 cells refused at 9. The COST line per length goes to log.txt.
///   (S1 / S2 / S3, the title-stream checks, were dropped with the RoomStream hunk: call 6 / C40, 2026-10-31.)
///   W1  the WebGL tier is off in the editor (the FRONTROOMS_WEBGL_PREVIEW compile is checked outside Unity).
///   W2  step 9 (stage C): the handheld tier (ForceHandheldForTools) builds 5 head + 5 wrap Lights, hard shadows, no beams,
///       the pools and the goal kept, photosafe; the desktop build before and after it is the same, light for light.
/// The rig needs the B kits (Kit_EmergencyTwinHead*, Kit_WrapFixture_4ft, the RN-K DA door kits); without them it builds
/// named stand-ins and the light counts still hold.
/// </summary>
public static class FrontRoomsRunRigTests
{
    const float Dt = 1f / 60f;
    static readonly StringBuilder Log = new StringBuilder();
    sealed class Result { public string name; public bool pass; public string detail; }
    static readonly List<Result> Results = new List<Result>();
    static string outDir;

    public static void RunBatch()
    {
        var args = Environment.GetCommandLineArgs();
        outDir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Verification", "run_b", "tests");
        for (var i = 0; i < args.Length - 1; i++) if (args[i] == "-runRigTestOut") outDir = args[i + 1];
        Directory.CreateDirectory(outDir);
        Log.Length = 0;
        Results.Clear();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        FrontRoomsKitLibrary.ClearCache();
        var reducedBefore = FrontRoomsSettings.ReduceFlashing;
        try
        {
            SetReduced(false);
            Run("R1 Evaluate(t) is deterministic", R1Deterministic);
            Run("R2 photosafety per lamp: trip, reset, stutter, warning", R2Photosafety);
            Run("R3 REDUCE FLASHING: no black gap, no flash", R3Reduced);
            Run("R4 the trip is one-shot", R4OneShot);
            Run("R5 the reset is idempotent", R5Reset);
            Run("R6 Tick allocates nothing", R6Alloc);
            Run("R7 corridor light budget (armed 10/9, blackout 1, tripped 9/8)", R7Budget);
            Run("R8 C41: no EXIT sign in the room, the fire gear stays", R8NoExitSign);
            Run("R9 C42: the goal window light", R9GoalLight);
            Run("R10 side doors: RN-K DA kits, dead lites, no closer", R10SideDoors);
            Run("R11 CR-4 warning burst on the wraps", R11WarnBurst);
            Run("R12 the map skin: soffit, arch reveal, lower tone behind the cabinet", R12MapSkin);
            Run("R13 F1: the cull keeps the room whole, the goal head never culls", R13Cull);
            Run("R14 F3: the reset as the frame sees it (one flick per wrap)", R14ResetWindows);
            Run("R15 step 10 + C39: the light gate on every Run! renderer", R15Gate);
            Run("R16 F7 + C39: the Run_B grade stays in the room, host-neutral", R16Post);
            Run("R17 F8 F9 F2 F6: plate outside, overhead unshadowed, dark obstacles, LOD distances", R17Dress);
            Run("R18 card 1: Corridor(9) is today's room, field for field and piece for piece", R18TodayUnchanged);
            Run("R19 card 1: 7 .. 14 cells by rule, photosafe, reset n x 0.15 s", R19Cells);
            Run("W1 WebGL tier off in the editor", W1Gate);
            Run("W2 step 9: the handheld tier, desktop unchanged", W2Handheld);
        }
        finally
        {
            SetReduced(reducedBefore);
        }
        var passed = 0;
        foreach (var r in Results) if (r.pass) passed++;
        var json = new StringBuilder("{\n \"results\": [\n");
        for (var i = 0; i < Results.Count; i++)
            json.Append("  {\"name\": ").Append(Q(Results[i].name)).Append(", \"pass\": ").Append(Results[i].pass ? "true" : "false")
                .Append(", \"detail\": ").Append(Q(Results[i].detail)).Append(i < Results.Count - 1 ? "},\n" : "}\n");
        json.Append(" ],\n \"passed\": ").Append(passed).Append(",\n \"failed\": ").Append(Results.Count - passed).Append("\n}\n");
        File.WriteAllText(Path.Combine(outDir, "tests.json"), json.ToString());
        File.WriteAllText(Path.Combine(outDir, "log.txt"), Log.ToString());
        Debug.Log("[RunRigTests] " + passed + " passed, " + (Results.Count - passed) + " failed | " + outDir);
    }

    static string Q(string s) { return "\"" + (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ") + "\""; }
    static string F(float f) { return f.ToString("0.###", CultureInfo.InvariantCulture); }

    static void Run(string name, Func<string> test)
    {
        string detail; var pass = true;
        try { detail = test(); }
        catch (Exception e) { pass = false; detail = e.GetType().Name + ": " + e.Message; Log.AppendLine(e.ToString()); }
        Results.Add(new Result { name = name, pass = pass, detail = detail });
        Log.AppendLine((pass ? "PASS " : "FAIL ") + name + " | " + detail);
        Debug.Log("[RunRigTests] " + (pass ? "PASS " : "FAIL ") + name + " | " + detail);
    }

    static void Require(bool ok, string message) { if (!ok) throw new Exception(message); }

    static void SetReduced(bool on)
    {
        // The private setter only: never FrontRoomsSettings.SetReduceFlashing, which writes PlayerPrefs.
        typeof(FrontRoomsSettings).GetProperty("ReduceFlashing", BindingFlags.Static | BindingFlags.Public).GetSetMethod(true).Invoke(null, new object[] { on });
    }

    // ------------------------------------------------------------------ helpers
    sealed class Corridor : IDisposable
    {
        public readonly Transform root;
        public readonly FrontRoomsRunRig rig;
        public Corridor()
        {
            root = new GameObject("TEST run b corridor").transform;
            root.position = new Vector3(4992f, 0f, 4992f);
            var layout = RunRigLayout.Corridor1x9();
            var dress = FrontRoomsRunDress.Build(root, layout);
            rig = FrontRoomsRunRig.Create(root, layout, RunRigHost.OwnLamps, null, dress);
        }
        public void Dispose() { UnityEngine.Object.DestroyImmediate(root.gameObject); }
    }

    static Light[] Lights(FrontRoomsRunRig rig) { return rig.GetComponentsInChildren<Light>(true); }

    static string Fingerprint(FrontRoomsRunRig rig)
    {
        var sb = new StringBuilder();
        foreach (var l in Lights(rig)) sb.Append(l.enabled ? '1' : '0').Append(F(l.intensity)).Append(';');
        foreach (var p in rig.GetComponentsInChildren<ReflectionProbe>(true)) sb.Append(p.enabled ? 'P' : 'p').Append(F(p.intensity)).Append(';');
        return sb.ToString();
    }

    /// <summary>The same flash rule as FrontRoomsExitSignTests: transitions of 0.1 from the last extreme with the
    /// darker side below 0.8; a flash is a transition pair; the most flashes in any 1 s window.</summary>
    sealed class FlashCounter
    {
        float extreme; int dir;
        public readonly List<float> transitions = new List<float>();
        public FlashCounter(float start) { extreme = start; }
        public void Add(float t, float v)
        {
            if (dir >= 0)
            {
                if (v > extreme) extreme = v;
                else if (extreme - v >= .1f && v < .8f) { transitions.Add(t); dir = -1; extreme = v; }
            }
            else
            {
                if (v < extreme) extreme = v;
                else if (v - extreme >= .1f && extreme < .8f) { transitions.Add(t); dir = 1; extreme = v; }
            }
        }
        public float MaxFlashesPerSecond()
        {
            var best = 0;
            for (int i = 0, j = 0; i < transitions.Count; i++)
            {
                while (transitions[i] - transitions[j] >= 1f) j++;
                best = Mathf.Max(best, i - j + 1);
            }
            return best / 2f;
        }
    }

    /// <summary>Record every rig light's intensity, normalised by its own maximum over the run, and count flashes.</summary>
    sealed class LampTrace
    {
        readonly Light[] lights;
        readonly List<float>[] values;
        readonly List<float> times = new List<float>();
        public LampTrace(Light[] l)
        {
            lights = l;
            values = new List<float>[l.Length];
            for (var i = 0; i < l.Length; i++) values[i] = new List<float>();
        }
        public void Sample(float t)
        {
            times.Add(t);
            for (var i = 0; i < lights.Length; i++) values[i].Add(lights[i].enabled ? lights[i].intensity : 0f);
        }
        public float WorstFlashes(out string worstName)
        {
            var worst = 0f; worstName = "-";
            for (var i = 0; i < lights.Length; i++)
            {
                var max = 0f;
                foreach (var v in values[i]) max = Mathf.Max(max, v);
                if (max <= 0f) continue;
                var fc = new FlashCounter(values[i][0] / max);
                for (var k = 0; k < values[i].Count; k++) fc.Add(times[k], values[i][k] / max);
                var f = fc.MaxFlashesPerSecond();
                if (f > worst) { worst = f; worstName = lights[i].name; }
            }
            return worst;
        }
        public bool Monotone(int i, float tolerance)
        {
            for (var k = 1; k < values[i].Count; k++) if (values[i][k] < values[i][k - 1] - tolerance) return false;
            return true;
        }
        public int Count { get { return lights.Length; } }
        public string Name(int i) { return lights[i].name; }
    }

    static Light[] Named(FrontRoomsRunRig rig, string prefix)
    {
        var list = new List<Light>();
        foreach (var l in Lights(rig)) if (l.name.StartsWith(prefix, StringComparison.Ordinal)) list.Add(l);
        return list.ToArray();
    }

    // ------------------------------------------------------------------ rig
    static string R1Deterministic()
    {
        var times = new[] { -1f, 0f, .03f, .06f, .2f, .35f, .37f, .45f, .6f, 1f, 3f };
        using (var a = new Corridor())
        using (var b = new Corridor())
        {
            var first = new string[times.Length];
            for (var i = 0; i < times.Length; i++) { a.rig.Evaluate(times[i]); first[i] = Fingerprint(a.rig); }
            for (var i = times.Length - 1; i >= 0; i--)
            {
                a.rig.Evaluate(times[i]);
                Require(Fingerprint(a.rig) == first[i], "rig A at t " + F(times[i]) + " differs when evaluated in reverse order");
                b.rig.Evaluate(times[i]);
                Require(Fingerprint(b.rig) == first[i], "rig B at t " + F(times[i]) + " differs from rig A");
            }
            return times.Length + " times x 2 orders x 2 rigs identical; " + Lights(a.rig).Length + " lights";
        }
    }

    static string R2Photosafety()
    {
        var sb = new StringBuilder();
        using (var c = new Corridor())
        {
            var rig = c.rig;
            // Trip: evaluated at 1 kHz over 3 s.
            var trip = new LampTrace(Lights(rig));
            for (var ms = -100; ms <= 3000; ms++) { rig.Evaluate(ms / 1000f); trip.Sample(ms / 1000f); }
            string n; var f = trip.WorstFlashes(out n);
            sb.Append("trip ").Append(F(f)).Append("/s (").Append(n).Append("); ");
            Require(f <= 3f, "trip: " + F(f) + " flashes/s on " + n);
            // Animated reset (map RoomReset): Trip, settle, StartReset, ticked at 240 Hz.
            rig.ResetImmediate();
            rig.Trip();
            for (var k = 0; k < 240; k++) rig.Tick(Dt);
            var reset = new LampTrace(Lights(rig));
            rig.StartReset();
            var t = 0f;
            reset.Sample(t);
            for (var k = 0; k < 240 * 12 && rig.State == RunRigState.Resetting; k++) { rig.Tick(1f / 240f); t += 1f / 240f; reset.Sample(t); }
            Require(rig.State == RunRigState.Armed, "reset did not end Armed after " + F(t) + " s");
            f = reset.WorstFlashes(out n);
            sb.Append("reset ").Append(F(f)).Append("/s (").Append(n).Append(", ").Append(F(t)).Append(" s); ");
            Require(f <= 3f, "reset: " + F(f) + " flashes/s on " + n);
            // Stutter tube (armed, own lamps): 60 s at 60 Hz.
            var stutter = new LampTrace(Lights(rig));
            t = 0f;
            for (var k = 0; k < 3600; k++) { rig.Tick(Dt); t += Dt; stutter.Sample(t); }
            f = stutter.WorstFlashes(out n);
            sb.Append("stutter ").Append(F(f)).Append("/s (").Append(n).Append("); ");
            Require(f <= 3f, "stutter: " + F(f) + " flashes/s on " + n);
            // Stage-1 warning (CR-4): the map's warning lamps drive ApplyWarn; here a 2 Hz square between 1 and .55,
            // the fastest the warning plan allows, must not exceed the bar on any wrap.
            var warnTrace = new LampTrace(Lights(rig));
            t = 0f;
            for (var k = 0; k < 600; k++)
            {
                rig.ApplyWarn(Mathf.Repeat(t, .5f) < .25f ? 1f : .55f);
                rig.Tick(Dt); t += Dt; warnTrace.Sample(t);
            }
            rig.ApplyWarn(1f);
            f = warnTrace.WorstFlashes(out n);
            sb.Append("warning 2 Hz ").Append(F(f)).Append("/s (").Append(n).Append(")");
            Require(f <= 3f, "warning: " + F(f) + " flashes/s on " + n);
        }
        return sb.ToString();
    }

    static string R3Reduced()
    {
        var sb = new StringBuilder();
        SetReduced(true);
        try
        {
            using (var c = new Corridor())
            {
                var rig = c.rig;
                var wraps = Named(rig, "run b wrap light");
                var heads = Named(rig, "head light");
                Require(wraps.Length == 9 && heads.Length == 8, "corridor: " + wraps.Length + " wraps, " + heads.Length + " heads");
                rig.Trip();
                var trace = new LampTrace(Lights(rig));
                var minRoom = 1f; var minAt = 0f; var t = 0f;
                var wrapMax = RunRigLayout.Corridor1x9().wrapIntensity;
                var headMax = RunRigLayout.Corridor1x9().floodIntensity;
                trace.Sample(0f);
                for (var k = 0; k < 240 * 3; k++)
                {
                    rig.Tick(1f / 240f); t += 1f / 240f; trace.Sample(t);
                    var w = 0f; foreach (var l in wraps) w += l.enabled ? l.intensity / wrapMax : 0f;
                    var h = 0f; foreach (var l in heads) h += l.enabled ? Mathf.Min(1f, l.intensity / headMax) : 0f;
                    var room = Mathf.Max(w / wraps.Length, h / heads.Length);
                    if (room < minRoom) { minRoom = room; minAt = t; }
                }
                string n; var f = trace.WorstFlashes(out n);
                sb.Append("trip: ").Append(F(f)).Append(" flashes/s, darkest room level ").Append(F(minRoom)).Append(" at ").Append(F(minAt)).Append(" s; ");
                Require(f <= .5f, "reduced trip flashes on " + n);   // one monotone change per lamp, never a pair
                Require(minRoom >= .15f, "reduced trip has a black gap (" + F(minRoom) + " at " + F(minAt) + " s)");
                Require(rig.WrapLevel(.25f) > .4f && rig.WrapLevel(.49f) > 0f, "reduced wrap fade is shorter than 0.5 s");
                Require(rig.HeadLevel(.5f, 0f) < 1f && rig.HeadLevel(.74f, 0f) > .9f, "reduced head rise is not 0.5 s from 0.25 s");
                // Re-strike: a rise, no flicks.
                var reset = new LampTrace(wraps);
                rig.StartReset();
                t = 0f; reset.Sample(0f);
                for (var k = 0; k < 240 * 12 && rig.State == RunRigState.Resetting; k++) { rig.Tick(1f / 240f); t += 1f / 240f; reset.Sample(t); }
                f = reset.WorstFlashes(out n);
                var mono = true;
                for (var i = 0; i < reset.Count; i++) if (!reset.Monotone(i, 1e-4f)) mono = false;
                sb.Append("reset: ").Append(F(f)).Append(" flashes/s, monotone ").Append(mono).Append("; ");
                Require(f <= .5f && mono, "reduced re-strike flickers");
                // Stutter: steady.
                var stutter = new LampTrace(wraps);
                for (var k = 0; k < 600; k++) { rig.Tick(Dt); stutter.Sample(k * Dt); }
                f = stutter.WorstFlashes(out n);
                sb.Append("stutter: ").Append(F(f)).Append(" flashes/s");
                Require(f == 0f, "reduced stutter flickers on " + n);
            }
        }
        finally { SetReduced(false); }
        return sb.ToString();
    }

    static string R4OneShot()
    {
        using (var c = new Corridor())
        {
            var rig = c.rig;
            int drops = 0, relays = 0;
            rig.MainsDropped += p => drops++;
            rig.TransferRelay += (i, p) => relays++;
            rig.Trip();
            rig.Tick(.05f);
            var clock = rig.Clock;
            rig.Trip();
            Require(Mathf.Approximately(rig.Clock, clock), "a second Trip restarted the clock");
            for (var k = 0; k < 120; k++) rig.Tick(Dt);
            Require(rig.State == RunRigState.Tripped, "state " + rig.State + " after 2 s");
            rig.Trip();
            for (var k = 0; k < 60; k++) rig.Tick(Dt);
            Require(drops == 1, drops + " mains drops");
            Require(relays == rig.HeadCount, relays + " transfer relays for " + rig.HeadCount + " heads");
            return "1 mains drop, " + relays + " relays, Trip ignored while Tripping and Tripped";
        }
    }

    static string R5Reset()
    {
        using (var fresh = new Corridor())
        using (var c = new Corridor())
        {
            var armed = Fingerprint(fresh.rig);
            var rig = c.rig;
            var changes = 0;
            rig.StateChanged += (s, p) => changes++;
            rig.Trip();
            for (var k = 0; k < 180; k++) rig.Tick(Dt);
            var before = changes;
            rig.ResetImmediate();
            var once = Fingerprint(rig);
            rig.ResetImmediate();
            var twice = Fingerprint(rig);
            Require(changes == before + 1, "ResetImmediate raised " + (changes - before) + " state changes");
            Require(once == twice, "a second ResetImmediate changed the rig");
            Require(once == armed, "reset rig differs from a freshly built armed rig");
            Require(rig.State == RunRigState.Armed && rig.Clock < 0f, "not armed after reset");
            return "armed after one reset, unchanged by a second, identical to a fresh rig";
        }
    }

    static string R6Alloc()
    {
        using (var c = new Corridor())
        {
            var rig = c.rig;
            for (var k = 0; k < 60; k++) rig.Tick(Dt);              // armed warm-up (stutter)
            rig.Warn(1);                                             // the CR-4 burst path inside the armed measure
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var k = 0; k < 600; k++) rig.Tick(Dt);
            var armed = GC.GetAllocatedBytesForCurrentThread() - before;
            rig.Trip();
            for (var k = 0; k < 5; k++) rig.Tick(Dt);
            before = GC.GetAllocatedBytesForCurrentThread();
            for (var k = 0; k < 600; k++) rig.Tick(Dt);
            var tripped = GC.GetAllocatedBytesForCurrentThread() - before;
            rig.StartReset();
            before = GC.GetAllocatedBytesForCurrentThread();
            for (var k = 0; k < 600; k++) rig.Tick(Dt);
            var reset = GC.GetAllocatedBytesForCurrentThread() - before;
            var detail = "armed " + armed + " B, tripping/tripped " + tripped + " B, resetting " + reset + " B over 600 ticks each";
            Require(armed == 0 && tripped == 0 && reset == 0, detail);
            return detail;
        }
    }

    static string R7Budget()
    {
        using (var c = new Corridor())
        {
            var rig = c.rig;
            int total, on, sh;
            rig.Evaluate(-1f); rig.CountLights(out total, out on, out sh);
            var detail = "armed " + on + "/" + total + " (" + sh + " shadowed)";
            Require(on == 10 && sh == 9, detail);
            rig.Evaluate(.2f); rig.CountLights(out total, out on, out sh);
            detail += ", blackout " + on;
            Require(on == 1, detail);
            rig.Evaluate(3f); rig.CountLights(out total, out on, out sh);
            detail += ", tripped " + on + " (" + sh + " shadowed)";
            Require(on == 9 && sh == 8, detail);
            return detail;
        }
    }

    static string R8NoExitSign()
    {
        using (var c = new Corridor())
        {
            var signs = c.root.GetComponentsInChildren<FrontRoomsExitSign>(true).Length;
            var named = 0;
            foreach (var t in c.root.GetComponentsInChildren<Transform>(true))
                if (t.name.IndexOf("exit sign", StringComparison.OrdinalIgnoreCase) >= 0 || t.name.StartsWith("Kit_ExitSign", StringComparison.Ordinal)) named++;
            Require(signs == 0 && named == 0, signs + " FrontRoomsExitSign, " + named + " objects named as EXIT signs");
            var gear = new[] { "run b Kit_PullStation", "run b Kit_FireBell", "run b Kit_ExtinguisherCabinet", "run b sprinkler main" };
            var detail = "0 EXIT signs; fire gear:";
            foreach (var g in gear)
            {
                var n = 0;
                foreach (var t in c.root.GetComponentsInChildren<Transform>(true)) if (t.name == g) n++;
                Require(n > 0, "missing " + g);
                detail += " " + g.Substring(6) + " " + n;
            }
            return detail;
        }
    }

    static string R9GoalLight()
    {
        using (var c = new Corridor())
        {
            var rig = c.rig;
            var gl = rig.GoalWindowLight;
            Require(gl != null, "no goal window light");
            Require(gl.shadows == LightShadows.None, "the goal window light casts shadows");
            var local = c.root.InverseTransformPoint(gl.transform.position);
            Require(local.z > 27.08f, "the goal window light is not past the goal wall (z " + F(local.z) + ")");
            Require(Vector3.Dot(gl.transform.forward, c.root.forward) > .5f, "the goal window light is not aimed away from the door");
            var eye = new GameObject("TEST viewer").transform;
            try
            {
                eye.SetParent(c.root, false);
                rig.SetViewer(eye);
                eye.localPosition = new Vector3(0f, 1.62f, 1.5f);      // the trip line
                rig.Tick(Dt);
                var detail = "trip line: on " + gl.enabled + " w " + F(rig.GoalLightWeight);
                Require(gl.enabled && rig.GoalLightWeight > .99f, detail + " (estimate: the far lamp is 27 m away, unlit)");
                rig.FarRoomLamp = () => 1f; rig.Tick(Dt);
                detail += "; far lamp 1: w " + F(rig.GoalLightWeight);
                Require(!gl.enabled, detail);
                rig.FarRoomLamp = () => .5f; rig.Tick(Dt);
                detail += "; far lamp .5: " + F(gl.intensity);
                Require(Mathf.Abs(rig.GoalLightWeight - .5f) < 1e-4f, detail);
                rig.FarRoomLamp = () => -1f;
                eye.localPosition = new Vector3(0f, 1.62f, 26.0f);     // 1 m from the door, no far lamp
                rig.Tick(Dt);
                detail += "; no lamp at 1 m: w " + F(rig.GoalLightWeight);
                Require(rig.GoalLightWeight < 1e-4f, detail);
                eye.localPosition = new Vector3(0f, 1.62f, 16.0f);
                rig.Tick(Dt);
                detail += ", at 11 m " + F(rig.GoalLightWeight);
                Require(rig.GoalLightWeight > .99f, detail);
                rig.Trip();
                for (var k = 0; k < 120; k++) rig.Tick(Dt);
                detail += ", tripped " + F(rig.GoalLightWeight);
                Require(rig.GoalLightWeight > .99f && gl.enabled, detail);
                eye.localPosition = new Vector3(0f, 1.62f, 28.0f);     // in the far room
                rig.Tick(Dt);
                detail += "; past the wall: on " + gl.enabled;
                Require(!gl.enabled, detail);
                return detail;
            }
            finally { UnityEngine.Object.DestroyImmediate(eye.gameObject); }
        }
    }

    static string R10SideDoors()
    {
        using (var c = new Corridor())
        {
            int frames = 0, leaves = 0, closers = 0;
            foreach (var t in c.root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "run b side door frame") frames++;
                if (t.name == "run b side door leaf") leaves++;
                if (t.name.IndexOf("closer", StringComparison.OrdinalIgnoreCase) >= 0) closers++;
            }
            var lites = c.root.GetComponentsInChildren<FrontRoomsDoorLite>(true);
            var dead = 0;
            foreach (var l in lites) if (l.Dead) dead++;
            var detail = frames + " frames, " + leaves + " leaves, " + dead + "/" + lites.Length + " dead lites, " + closers + " closers";
            Require(frames == 3 && leaves == 3 && dead == 3 && lites.Length == 3 && closers == 0, detail);
            // The frame never shows on the neighbour's face: every frame renderer stays inside the wall's far face.
            var layout = RunRigLayout.Corridor1x9();
            var limit = layout.halfWidth + layout.wallThickness - .001f;
            foreach (var t in c.root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name != "run b side door frame") continue;
                foreach (var r in t.GetComponentsInChildren<Renderer>(true))
                {
                    var b = r.bounds;
                    var a = c.root.InverseTransformPoint(b.min);
                    var z = c.root.InverseTransformPoint(b.max);
                    Require(Mathf.Max(Mathf.Abs(a.x), Mathf.Abs(z.x)) <= limit, "a side door frame reaches |x| " + F(Mathf.Max(Mathf.Abs(a.x), Mathf.Abs(z.x))) + " > " + F(limit));
                }
            }
            return detail + "; frames inside |x| <= " + F(limit);
        }
    }

    static string R11WarnBurst()
    {
        var sb = new StringBuilder();
        using (var c = new Corridor())
        {
            var rig = c.rig;
            var wraps = Named(rig, "run b wrap light");
            var trace = new LampTrace(wraps);
            var t = 0f;
            var minRatio = 1f;
            var armed = new float[wraps.Length];
            for (var i = 0; i < wraps.Length; i++) armed[i] = wraps[i].intensity;
            rig.Warn(1);
            Require(rig.Bursting, "Warn(1) started no burst");
            trace.Sample(0f);
            for (var k = 0; k < 240 * 3; k++)
            {
                rig.Tick(1f / 240f); t += 1f / 240f; trace.Sample(t);
                for (var i = 0; i < wraps.Length; i++)
                    if (i != RunRigLayout.Corridor1x9().stutterLamp && armed[i] > 0f) minRatio = Mathf.Min(minRatio, wraps[i].intensity / armed[i]);
            }
            string n; var f = trace.WorstFlashes(out n);
            sb.Append("stage 1: ").Append(F(f)).Append(" flashes/s (").Append(n).Append("), deepest ").Append(F(minRatio));
            Require(f <= 3f, sb.ToString());
            Require(minRatio > .5f && minRatio < .8f, sb + ": the dip is not the map's 0.55-0.75");
            Require(!rig.Bursting && rig.State == RunRigState.Armed, sb + ": the burst did not end armed");
        }
        SetReduced(true);
        try
        {
            using (var c = new Corridor())
            {
                var rig = c.rig;
                var wraps = Named(rig, "run b wrap light");
                var k0 = RunRigLayout.Corridor1x9().stutterLamp == 0 ? 1 : 0;
                var armed = wraps[k0].intensity;
                var min = 1f;
                rig.Warn(1);
                for (var k = 0; k < 240 * 3; k++) { rig.Tick(1f / 240f); min = Mathf.Min(min, wraps[k0].intensity / armed); }
                sb.Append("; reduced: one sag to ").Append(F(min));
                Require(min >= .69f && min < .75f, sb.ToString());
            }
        }
        finally { SetReduced(false); }
        return sb.ToString();
    }

    static string R12MapSkin()
    {
        var root = new GameObject("TEST run b map skin").transform;
        try
        {
            root.position = new Vector3(4992f, 0f, 4992f);
            var l = RunRigLayout.Corridor1x9();
            l.buildShell = false;                     // as the binder (FrontRoomsRunRigBinder.Layout): the map builds the shell
            l.skinShell = true;
            FrontRoomsRunDress.Build(root, l);
            int soffit = 0, reveal = 0, revealAtGoal = 0, coverCabinet = 0;
            var revealDepth = 0f;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "run b soffit skin") soffit++;
                if (t.name.StartsWith("run b arch reveal", StringComparison.Ordinal))
                {
                    reveal++;
                    var r = t.GetComponent<Renderer>();
                    var a = root.InverseTransformPoint(r.bounds.min);
                    var b = root.InverseTransformPoint(r.bounds.max);
                    if (Mathf.Max(a.z, b.z) > l.length * .5f) revealAtGoal++;
                    revealDepth = Mathf.Max(revealDepth, Mathf.Max(a.z, b.z));
                    Require(Mathf.Min(a.z, b.z) > -.001f, t.name + " reaches the host's half (z " + F(Mathf.Min(a.z, b.z)) + ")");
                }
                if (t.name == "run b lower tone")
                {
                    var r = t.GetComponent<Renderer>();
                    var a = root.InverseTransformPoint(r.bounds.min);
                    var b = root.InverseTransformPoint(r.bounds.max);
                    if (Mathf.Min(a.x, b.x) < -l.halfWidth + .01f && Mathf.Min(a.z, b.z) < 10.04f && Mathf.Max(a.z, b.z) > 10.36f) coverCabinet++;
                }
            }
            var detail = soffit + " soffit, " + reveal + " arch reveal slabs to z " + F(revealDepth) + " (" + revealAtGoal + " at the goal), lower tone behind the cabinet " + coverCabinet;
            Require(soffit == 1 && reveal == 7 && revealAtGoal == 0 && coverCabinet == 1, detail);
            Require(revealDepth > l.WallHalf && revealDepth < l.WallHalf + .005f, detail + ": the reveal does not meet the room face");
            return detail;
        }
        finally { UnityEngine.Object.DestroyImmediate(root.gameObject); }
    }

    // ------------------------------------------------------------------ stage C (60_production_66.md)
    static float XZ(Vector3 a, Vector3 b) { var dx = a.x - b.x; var dz = a.z - b.z; return Mathf.Sqrt(dx * dx + dz * dz); }

    /// <summary>A direct child by name (Transform.Find splits on '/', and the rig's names carry one).</summary>
    static Transform Child(Transform t, string name)
    {
        if (t == null) return null;
        for (var i = 0; i < t.childCount; i++) if (t.GetChild(i).name == name) return t.GetChild(i);
        return null;
    }

    static string R13Cull()
    {
        using (var c = new Corridor())
        {
            var rig = c.rig;
            var layout = RunRigLayout.Corridor1x9();
            var eye = new GameObject("TEST viewer").transform;
            try
            {
                eye.SetParent(c.root, false);
                rig.SetViewer(eye);
                eye.localPosition = new Vector3(0f, 1.62f, 1.5f);          // the trip line
                rig.Trip();
                for (var k = 0; k < 120; k++) rig.Tick(Dt);
                int total, on, sh;
                rig.CountLights(out total, out on, out sh);
                var detail = "trip line, tripped: " + on + " on";
                Require(on == 9, detail + " (want 8 heads + the goal window light)");
                var full = new float[rig.HeadCount];
                for (var i = 0; i < rig.HeadCount; i++) full[i] = rig.HeadLight(i) != null ? rig.HeadLight(i).intensity : 0f;
                eye.localPosition = new Vector3(0f, 1.62f, -25f);          // 25 m outside the arch
                rig.Tick(Dt);
                var goalOn = false; var othersOn = 0;
                for (var i = 0; i < rig.HeadCount; i++)
                {
                    var l = rig.HeadLight(i);
                    if (l == null) continue;
                    if (rig.HeadIsGoal(i)) goalOn = l.enabled && Mathf.Abs(l.intensity - full[i]) < 1e-3f;
                    else if (l.enabled) othersOn++;
                }
                detail += "; 25 m out: goal head at full " + goalOn + ", other heads on " + othersOn;
                Require(goalOn && othersOn == 0, detail);
                eye.localPosition = new Vector3(0f, 1.62f, -10f);          // 9.9 m outside: the box weight is 0
                rig.Tick(Dt);
                var worst = 0f;
                for (var i = 0; i < rig.HeadCount; i++)
                {
                    var l = rig.HeadLight(i);
                    if (l == null || rig.HeadIsGoal(i)) continue;
                    var want = full[i] * Mathf.Clamp01((layout.cullDistance - XZ(l.transform.position, eye.position)) / layout.cullFade);
                    worst = Mathf.Max(worst, Mathf.Abs((l.enabled ? l.intensity : 0f) - want));
                }
                detail += "; 10 m out: worst error against the 16 m / 3 m fade " + F(worst);
                Require(worst < 1e-3f, detail);
                return detail;
            }
            finally { UnityEngine.Object.DestroyImmediate(eye.gameObject); }
        }
    }

    static string R14ResetWindows()
    {
        using (var c = new Corridor())
        {
            var rig = c.rig;
            var wraps = Named(rig, "run b wrap light");
            var heads = Named(rig, "head light");
            Require(wraps.Length == 9, wraps.Length + " wraps");
            rig.Trip();
            for (var k = 0; k < 240; k++) rig.Tick(Dt);
            var series = new List<float[]>();
            var times = new List<float>();
            rig.StartReset();
            var t = 0f;
            for (var k = 0; k < 240 * 12; k++)
            {
                var row = new float[wraps.Length + 1];
                for (var i = 0; i < wraps.Length; i++) row[i] = wraps[i].enabled ? wraps[i].intensity : 0f;
                var h = 0f;
                foreach (var l in heads) h += l.enabled ? l.intensity : 0f;
                row[wraps.Length] = h;
                series.Add(row); times.Add(t);
                if (rig.State != RunRigState.Resetting) break;
                rig.Tick(1f / 240f); t += 1f / 240f;
            }
            // Flicks per wrap: rising edges before its sweep.
            var flicks = 0; var worstWrap = 0;
            for (var i = 0; i < wraps.Length; i++)
            {
                var edges = 0; var prev = 0f;
                for (var k = 0; k < series.Count && times[k] < .44f; k++) { if (series[k][i] > 0f && prev <= 0f) edges++; prev = series[k][i]; }
                flicks += edges; worstWrap = Mathf.Max(worstWrap, edges);
            }
            Require(worstWrap == 1, "a wrap flicks " + worstWrap + " times before the sweep");
            // Windows: everything (the corridor seen whole) and every 3 neighbouring wraps with the heads.
            var worst = 0f; var worstName = "-";
            for (var w = -1; w + 2 < wraps.Length; w++)
            {
                var a = w < 0 ? 0 : w; var b = w < 0 ? wraps.Length - 1 : w + 2;
                var mean = new float[series.Count];
                var max = 0f;
                for (var k = 0; k < series.Count; k++)
                {
                    var v = series[k][wraps.Length];
                    for (var i = a; i <= b; i++) v += series[k][i];
                    mean[k] = v; max = Mathf.Max(max, v);
                }
                if (max <= 0f) continue;
                var fc = new FlashCounter(mean[0] / max);
                for (var k = 0; k < series.Count; k++) fc.Add(times[k], mean[k] / max);
                var f = fc.MaxFlashesPerSecond();
                if (f > worst) { worst = f; worstName = w < 0 ? "whole corridor" : "wraps " + a + "-" + b; }
            }
            var detail = flicks + " flicks over " + wraps.Length + " wraps, worst window " + F(worst) + " flashes/s (" + worstName + "), " + F(t) + " s";
            Require(worst <= 3f, detail);
            return detail;
        }
    }

    static string R15Gate()
    {
        using (var c = new Corridor())
        {
            var rig = c.rig;
            Require(rig.GatedRenderers > 0, "no renderer took a gated twin");
            var mpb = new MaterialPropertyBlock();
            int twins = 0, ungateable = 0, missed = 0, blocks = 0;
            var dress = Child(c.root, "Run B / dress");
            var roots = new[] { rig.transform, dress };
            foreach (var root in roots)
            {
                if (root == null) continue;
                foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                {
                    var mats = r.sharedMaterials;
                    for (var k = 0; k < mats.Length; k++)
                    {
                        if (mats[k] == null) continue;
                        if (FrontRoomsRunGate.IsTwin(mats[k]))
                        {
                            twins++;
                            r.GetPropertyBlock(mpb, k);
                            if (mpb.GetVector("_FR_RunGateB").w > .5f) blocks++;
                        }
                        else if (FrontRoomsRunGate.CanGate(mats[k])) missed++;
                        else ungateable++;
                    }
                }
            }
            var detail = twins + " gated slots (" + blocks + " with the box), " + ungateable + " not gateable (glass, emissive, other shaders), " + missed + " missed";
            Require(twins > 0 && blocks == twins && missed == 0, detail);
            var notice = c.root.Find("run b Kit_SafetyPlate_10x7");
            Require(notice != null, "the NOTICE plate is not under the room root");
            foreach (var r in notice.GetComponentsInChildren<Renderer>(true))
                foreach (var m in r.sharedMaterials) Require(!FrontRoomsRunGate.IsTwin(m), "the NOTICE plate outside took a gated twin");
            // The goal opening: the lite while shut, the whole door when open.
            Renderer probe = null; var slot = -1;
            foreach (var r in dress.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (var k = 0; k < mats.Length && slot < 0; k++) if (FrontRoomsRunGate.IsTwin(mats[k])) { probe = r; slot = k; }
                if (slot >= 0) break;
            }
            Require(probe != null, "no gated dress renderer");
            var l = RunRigLayout.Corridor1x9();
            probe.GetPropertyBlock(mpb, slot);
            var e = mpb.GetVector("_FR_RunGateE");
            Require(!rig.GoalOpen && Mathf.Abs(e.y - l.goalLiteHalf) < 1e-4f && Mathf.Abs(e.z - l.goalLiteBottom) < 1e-4f, "shut: E " + e);
            rig.SetGoalOpen(true);
            probe.GetPropertyBlock(mpb, slot);
            e = mpb.GetVector("_FR_RunGateE");
            Require(rig.GoalOpen && Mathf.Abs(e.y - l.goalHalf) < 1e-4f && e.z < 0f && Mathf.Abs(e.w - l.goalTop) < 1e-4f, "open: E " + e);
            var cvec = mpb.GetVector("_FR_RunGateC");
            Require(Mathf.Abs(cvec.x - (l.halfWidth + l.WallHalf)) < 1e-4f && Mathf.Abs(cvec.y - l.length) < 1e-4f, "box C " + cvec);
            return detail + "; NOTICE plate ungated; goal portal lite / door";
        }
    }

    static float VolumeWeight(Volume v, Vector3 world)
    {
        var box = v.GetComponent<BoxCollider>();
        var p = box.transform.InverseTransformPoint(world) - box.center;
        var h = box.size * .5f;
        var q = new Vector3(Mathf.Clamp(p.x, -h.x, h.x), Mathf.Clamp(p.y, -h.y, h.y), Mathf.Clamp(p.z, -h.z, h.z));
        var d = (box.transform.TransformPoint(q + box.center) - world).magnitude;
        return v.blendDistance > 0f ? Mathf.Clamp01(1f - d / v.blendDistance) : (d <= 0f ? 1f : 0f);
    }

    static string R16Post()
    {
        using (var c = new Corridor())
        {
            var vols = c.rig.GetComponentsInChildren<Volume>(true);
            Require(vols.Length == 3, vols.Length + " Run_B volumes (want main + 2 mouths)");
            var main = 0; var mouths = 0;
            foreach (var v in vols) { if (Mathf.Abs(v.blendDistance - .3f) < 1e-4f) main++; else if (Mathf.Abs(v.blendDistance - 1f) < 1e-4f) mouths++; }
            Require(main == 1 && mouths == 2, main + " main, " + mouths + " mouth volumes");
            var r = PlayerRadius();
            var worstSide = 0f; var worstIn = 1f;
            for (var z = -2f; z <= 29f; z += .25f)
            {
                foreach (var sx in new[] { -1f, 1f })
                {
                    var side = c.root.TransformPoint(new Vector3(sx * (1.42f + .16f + r), 1.62f, z));
                    foreach (var v in vols) worstSide = Mathf.Max(worstSide, VolumeWeight(v, side));
                }
                if (z > .5f && z < 26.5f)
                {
                    var inside = c.root.TransformPoint(new Vector3(1.42f - r, 1.62f, z));
                    var w = 0f;
                    foreach (var v in vols) w = Mathf.Max(w, VolumeWeight(v, inside));
                    worstIn = Mathf.Min(worstIn, w);
                }
            }
            var detail = "side neighbour max weight " + F(worstSide) + ", corridor (hugging a wall) min weight " + F(worstIn);
            Require(worstSide <= 1e-4f && worstIn >= .999f, detail);
            // Host-neutral: everything the Office grade overrides, Run_B overrides; the three it took are the global's values.
            var b = Resources.Load<VolumeProfile>("Rendering/FrontRoomsPost_Run_B");
            var o = Resources.Load<VolumeProfile>("Rendering/FrontRoomsPost_Office");
            var g = Resources.Load<VolumeProfile>("Rendering/FrontRoomsPost");
            Require(b != null && o != null && g != null, "a post profile is missing");
            var missing = 0;
            foreach (var oc in o.components)
            {
                VolumeComponent bc = null;
                foreach (var x in b.components) if (x.GetType() == oc.GetType()) bc = x;
                for (var i = 0; i < oc.parameters.Count; i++)
                    if (oc.parameters[i].overrideState && (bc == null || !bc.parameters[i].overrideState)) { missing++; Log.AppendLine("R16 not overridden by Run_B: " + oc.GetType().Name + " #" + i); }
            }
            Require(missing == 0, detail + "; " + missing + " Office parameters Run_B leaves to the host");
            ShadowsMidtonesHighlights bs, gs; FilmGrain bf, gf; ChromaticAberration bca, gca;
            Require(b.TryGet(out bs) && g.TryGet(out gs) && bs.shadows.value == gs.shadows.value && bs.highlights.value == gs.highlights.value, "SMH differs from the global look");
            Require(b.TryGet(out bf) && g.TryGet(out gf) && bf.intensity.value == gf.intensity.value && bf.response.value == gf.response.value && bf.type.value == gf.type.value, "film grain differs from the global look");
            Require(b.TryGet(out bca) && g.TryGet(out gca) && bca.intensity.value == gca.intensity.value, "chromatic aberration differs from the global look");
            return detail + "; Run_B overrides all Office parameters; SMH, grain, CA = the global look";
        }
    }

    static float PlayerRadius() { return FrontRooms.Map.ModuleUnits.PlayerRadius; }

    static float LinearY(Color srgb) { var l = srgb.linear; return .2126f * l.r + .7152f * l.g + .0722f * l.b; }

    static string R17Dress()
    {
        using (var c = new Corridor())
        {
            var layout = RunRigLayout.Corridor1x9();
            var notice = c.root.Find("run b Kit_SafetyPlate_10x7");
            Require(notice != null && notice.parent == c.root, "the NOTICE plate is not on the room root");
            var lp = c.root.InverseTransformPoint(notice.position);
            Require(Mathf.Abs(lp.z + layout.WallHalf) < 1e-3f && Vector3.Dot(notice.forward, -c.root.forward) > .99f, "the NOTICE plate is not on the host face (z " + F(lp.z) + ")");
            var overhead = Child(Child(c.root, "Run B / dress"), "run b overhead services");
            Require(overhead != null, "no overhead services group");
            var casting = 0; var n = 0;
            foreach (var r in overhead.GetComponentsInChildren<Renderer>(true)) { n++; if (r.shadowCastingMode != ShadowCastingMode.Off) casting++; }
            Require(n > 0 && casting == 0, casting + " of " + n + " overhead renderers cast shadows");
            var floorY = LinearY(new Color(.725f, .706f, .651f));   // the VCT field #B9B4A6
            var chair = FrontRoomsSurfaces.Get("RunB_ChairGrey");
            var cart = FrontRoomsSurfaces.Get("RunB_CartPlastic");
            var cy = LinearY(chair.GetColor("_BaseColor"));
            var ky = LinearY(cart.GetColor("_BaseColor"));
            var detail = "plate on the host face, " + n + " overhead renderers unshadowed, albedo floor/chair " + F(floorY / cy) + ", floor/cart " + F(floorY / ky);
            Require(floorY / cy >= 4f && floorY / ky >= 4f, detail);
            var kits = new[] { "Kit_CableTray_3m", "Kit_ChairDolly8", "Kit_HandTruck_Cartons", "Kit_ExtinguisherCabinet", "Kit_EmergencyTwinHead" };
            var d01 = new[] { 4.5f, 4f, 5.8f, 3.5f, 3.6f };
            var d12 = new[] { 9f, 20f, 12f, 8f, 10f };
            var tan = Mathf.Tan(38f * Mathf.Deg2Rad);
            for (var i = 0; i < kits.Length; i++)
            {
                var go = Resources.Load<GameObject>("Props/Models/" + kits[i]);
                var g = go != null ? go.GetComponent<LODGroup>() : null;
                Require(g != null, kits[i] + ": no LOD group");
                var lods = g.GetLODs();
                Require(lods.Length >= 3, kits[i] + ": " + lods.Length + " LODs");
                var want0 = g.size / (2f * d01[i] * tan);
                var want1 = g.size / (2f * d12[i] * tan);
                Require(Mathf.Abs(lods[0].screenRelativeTransitionHeight - want0) < 1e-3f && Mathf.Abs(lods[1].screenRelativeTransitionHeight - want1) < 1e-3f,
                    kits[i] + ": switches at " + F(lods[0].screenRelativeTransitionHeight) + " / " + F(lods[1].screenRelativeTransitionHeight) + ", want " + F(want0) + " / " + F(want1) + " (re-imported?)");
            }
            return detail + "; 5 LOD groups at the new distances";
        }
    }

    /// <summary>Every light the rig owns, in order: its name, kind, range, cone, intensity, shadows and cookie; and the counts
    /// of beams, halos and renderers. Two builds with the same signature build the same lights.</summary>
    static string Signature(FrontRoomsRunRig rig)
    {
        var sb = new StringBuilder();
        foreach (var l in Lights(rig))
            sb.Append(l.name).Append('|').Append(l.type).Append('|').Append(F(l.range)).Append('|').Append(F(l.spotAngle)).Append('|').Append(F(l.innerSpotAngle))
              .Append('|').Append(F(l.intensity)).Append('|').Append(l.enabled ? 1 : 0).Append('|').Append(l.shadows).Append('|').Append(l.cookie != null ? l.cookie.name : "-").Append(';');
        int beams = 0, halos = 0, renderers = 0;
        foreach (var r in rig.GetComponentsInChildren<Renderer>(true))
        {
            renderers++;
            if (r.name == "beam cone") beams++;
            if (r.name == "run b soffit halo") halos++;
        }
        sb.Append("beams ").Append(beams).Append(" halos ").Append(halos).Append(" renderers ").Append(renderers);
        return sb.ToString();
    }

    static string W2Handheld()
    {
        var before = FrontRoomsRunRig.ForceHandheldForTools;
        string desktopA, desktopB, detail;
        try
        {
            FrontRoomsRunRig.ForceHandheldForTools = false;
            using (var a = new Corridor())
            {
                Require(!a.rig.Handheld, "a desktop build reports the handheld tier");
                a.rig.Evaluate(3f);
                desktopA = Signature(a.rig);
                int total, on, sh;
                a.rig.CountLights(out total, out on, out sh);
                Require(on == 9 && sh == 8 && Named(a.rig, "head light").Length == 8 && Named(a.rig, "run b wrap light").Length == 9, "desktop tripped " + on + "/" + sh);
            }
            FrontRoomsRunRig.ForceHandheldForTools = true;
            using (var h = new Corridor())
            {
                var rig = h.rig;
                Require(rig.Handheld, "the forced handheld build is not the handheld tier");
                var heads = Named(rig, "head light");
                var wraps = Named(rig, "run b wrap light");
                var beams = 0;
                foreach (var r in rig.GetComponentsInChildren<Renderer>(true)) if (r.name == "beam cone") beams++;
                var soft = 0;
                foreach (var l in Lights(rig)) if (l.shadows == LightShadows.Soft) soft++;
                detail = "handheld: " + heads.Length + " head + " + wraps.Length + " wrap Lights, " + beams + " beams, " + soft + " soft shadows";
                Require(heads.Length == 5 && wraps.Length == 5 && beams == 0 && soft == 0, detail);
                var goal = -1;
                for (var i = 0; i < rig.HeadCount; i++) if (rig.HeadIsGoal(i)) goal = i;
                Require(goal >= 0 && rig.HeadLight(goal) != null, "the handheld tier lost the goal head");
                int total, on, sh;
                rig.Evaluate(3f); rig.CountLights(out total, out on, out sh);
                detail += "; tripped " + on + " (" + sh + " shadowed)";
                Require(on == 6 && sh == 5, detail);
                rig.Evaluate(-1f); rig.CountLights(out total, out on, out sh);
                detail += ", armed " + on + " (" + sh + " shadowed)";
                Require(on == 6 && sh == 5, detail);
                var layout = RunRigLayout.Corridor1x9();
                foreach (var l in wraps)
                    if (l.name != "run b wrap light " + layout.stutterLamp)
                        Require(Mathf.Abs(l.intensity - layout.wrapIntensity * 9f / 5f) < 1e-3f, l.name + " at " + F(l.intensity));
                var trip = new LampTrace(Lights(rig));
                for (var ms = -100; ms <= 3000; ms++) { rig.Evaluate(ms / 1000f); trip.Sample(ms / 1000f); }
                string n; var f = trip.WorstFlashes(out n);
                detail += ", trip " + F(f) + " flashes/s";
                Require(f <= 3f, detail);
            }
            FrontRoomsRunRig.ForceHandheldForTools = false;
            using (var b = new Corridor())
            {
                b.rig.Evaluate(3f);
                desktopB = Signature(b.rig);
            }
        }
        finally { FrontRoomsRunRig.ForceHandheldForTools = before; }
        Require(desktopA == desktopB, "the desktop build differs before and after a handheld build");
        return detail + "; desktop signature identical before and after (" + desktopA.Length + " chars)";
    }

    // ------------------------------------------------------------------ card 1: the rig in cells (71 s.9, decisions 3 and 7)
    /// <summary>Today's preset as it landed in main (FrontRoomsRunRig.cs 9e964b3b, 2026-11-03), frozen verbatim: the reference R18
    /// holds Corridor(9) to, field for field. Never edit it to follow the factory.</summary>
    static RunRigLayout TodayCorridor1x9(float archWidth = 1.4f, float doorWidth = 1f)
    {
        var w = 1.42f;
        const float wh = .08f;
        var ah = archWidth * .5f;
        var dh = doorWidth * .5f;
        var l = new RunRigLayout
        {
            name = "Corridor1x9",
            halfWidth = w, length = 27f, height = 2.9f,
            units = new[]
            {
                // F12 (stage C): the near pool (z 6) read .220 x armed against r8's .169 (+30 %): its two heads x 0.77.
                // Zone lamps (Red 2026-10-31, T-A + T-B): the self-contained units stay at the entry and the goal ends; the two
                // between are remote heads on brackets, fed from the entry unit along both walls. Heads, aims and pools unchanged.
                new RunUnitDef { z = 3f, side = 1f, autoAim = true, trimPlus = -.23f },
                new RunUnitDef { z = 9f, side = -1f, autoAim = true, trimMinus = -.23f, remote = true },
                new RunUnitDef { z = 15f, side = 1f, autoAim = true, remote = true },
                new RunUnitDef { z = 21f, side = -1f, autoAim = true, goalPlus = true },
            },
            goalPoint = new Vector3(0f, 1.05f, 27f),
            stutterLamp = 4,
            // F7 (stage C): every wrap shadowed, so none lights the neighbours through the long walls (6 of 9 did, up to
            // 143 levels on the Office wall in the preview). Armed now carries 9 shadowed spots, tripped 8: the peak is
            // the tripped set's, plus one. WebGL keeps its one (ownLampShadows[0]).
            ownLampShadows = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 },
            archHalf = ah, archTop = 2.2f, goalHalf = dh, goalTop = 2.1f, goalLiteX = 0f,
            probes = new[]
            {
                new RunProbeBox { centre = new Vector3(0f, 1.45f, 4.5f), size = new Vector3(3.16f, 3.22f, 9.6f) },
                new RunProbeBox { centre = new Vector3(0f, 1.45f, 13.5f), size = new Vector3(3.16f, 3.22f, 9.6f) },
                new RunProbeBox { centre = new Vector3(0f, 1.45f, 22.46f), size = new Vector3(3.16f, 3.22f, 9.6f) },
            },
            postCentre = new Vector3(0f, 1.45f, 13.5f), postSize = new Vector3(3f, 2.9f, 27f),
            buildShell = true,
            floorX0 = -w, floorX1 = w, floorZ0 = 0f, floorZ1 = 27f,
            laneHalf = .45f, accentRowsX = new[] { -.6f, .6f },
            mainX = .55f, trayX = -.60f, emtWallX = -w, runZ0 = 0f, runZ1 = 27f,
        };
        l.ownLamps = new Vector3[9];
        for (var k = 0; k < 9; k++) l.ownLamps[k] = new Vector3(0f, 2.9f, 1.8f + 3f * k);
        // Long faces run the full 27 m (their first and last 0.08 m lie inside the end walls); the side-door gaps are the
        // leaf (+-0.5) plus 45 mm under the F4 casing (+-0.549), so the lower tone tucks under the casing and the rail stops clear.
        const float dg = .545f;
        var goal = TodayFace(new Vector3(-w, 0f, 27f - wh), Vector3.right, w * 2f, Vector3.back, new Vector2(w - dh, w + dh));
        goal.gapTops = new[] { 2.1f };
        var entry = TodayFace(new Vector3(-w, 0f, wh), Vector3.right, w * 2f, Vector3.forward, new Vector2(w - ah, w + ah));
        entry.gapTops = new[] { 2.2f };
        l.walls = new[]
        {
            TodayFace(new Vector3(-w, 0f, 0f), Vector3.forward, 27f, Vector3.right, new Vector2(10.04f, 10.36f), new Vector2(17f - dg, 17f + dg)),
            TodayFace(new Vector3(w, 0f, 0f), Vector3.forward, 27f, Vector3.left, new Vector2(5f - dg, 5f + dg), new Vector2(23f - dg, 23f + dg)),
            entry,
            goal,
        };
        l.wallItems = new[]
        {
            new RunWallItem { kit = "Kit_PullStation", point = new Vector3(w, 1.2f - .09f, 1f), normal = Vector3.left },
            new RunWallItem { kit = "Kit_FireBell", point = new Vector3(w, 2.3f, 1f), normal = Vector3.left },
            new RunWallItem { kit = "Kit_ExtinguisherCabinet", point = new Vector3(-w, 1.1f - .305f, 10.2f), normal = Vector3.right },
            // The NOTICE plate on the host side of the entrance wall (its outside face, z -0.08: F8, it was buried in the
            // wall), right of the arch seen from outside; the ALARM ZONE card goes on the left (FrontRoomsRunRigBinder).
            new RunWallItem { kit = "Kit_SafetyPlate_10x7", point = new Vector3(Mathf.Min(ah + .40f, w - .15f), 1.5f, -wh), normal = Vector3.back },
        };
        l.sideDoors = new[]
        {
            new RunSideDoor { z = 5f, side = 1f, face = "electrical" },
            new RunSideDoor { z = 17f, side = -1f, face = "staff" },
            new RunSideDoor { z = 23f, side = 1f, face = "stencil" },
        };
        l.obstacles = new[]
        {
            new RunObstacle { kit = "Kit_ChairDolly8", position = new Vector3(1.0f, 0f, 6.4f), yaw = 90f },
            new RunObstacle { kit = "Kit_JanitorCart", position = new Vector3(-.63f, 0f, 12.0f), yaw = 90f },
            new RunObstacle { kit = "Kit_HandTruck_Cartons", position = new Vector3(1.05f, 0f, 18.2f), yaw = -90f },
        };
        return l;
    }

    static RunWallFace TodayFace(Vector3 start, Vector3 along, float length, Vector3 normal, params Vector2[] gaps)
    {
        return new RunWallFace { start = start, along = along, length = length, normal = normal, gaps = gaps };
    }

    static string Bits(float f) { return f.ToString("R", CultureInfo.InvariantCulture); }
    static string Bits(Vector3 v) { return Bits(v.x) + "," + Bits(v.y) + "," + Bits(v.z); }
    static string Bits(Quaternion q) { return Bits(q.x) + "," + Bits(q.y) + "," + Bits(q.z) + "," + Bits(q.w); }

    /// <summary>Every field of a and b, recursively: arrays element by element, structs and classes field by field, floats bit for
    /// bit. Returns the number of leaf values compared; every difference goes to diffs.</summary>
    static int SameFields(object a, object b, string path, List<string> diffs)
    {
        if (a == null || b == null)
        {
            if (a != null || b != null) diffs.Add(path + ": " + (a == null ? "null" : "set") + " / " + (b == null ? "null" : "set"));
            return 1;
        }
        var t = a.GetType();
        if (t != b.GetType()) { diffs.Add(path + ": " + t.Name + " / " + b.GetType().Name); return 1; }
        if (t == typeof(float))
        {
            if (BitConverter.ToInt32(BitConverter.GetBytes((float)a), 0) != BitConverter.ToInt32(BitConverter.GetBytes((float)b), 0))
                diffs.Add(path + ": " + Bits((float)a) + " / " + Bits((float)b));
            return 1;
        }
        if (t.IsPrimitive || t.IsEnum || t == typeof(string))
        {
            if (!a.Equals(b)) diffs.Add(path + ": " + a + " / " + b);
            return 1;
        }
        if (t.IsArray)
        {
            var x = (Array)a;
            var y = (Array)b;
            if (x.Length != y.Length) { diffs.Add(path + ": length " + x.Length + " / " + y.Length); return 1; }
            var n = 0;
            for (var i = 0; i < x.Length; i++) n += SameFields(x.GetValue(i), y.GetValue(i), path + "[" + i + "]", diffs);
            return n;
        }
        var count = 0;
        foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            count += SameFields(f.GetValue(a), f.GetValue(b), path + "." + f.Name, diffs);
        return count;
    }

    /// <summary>Every piece under root, in hierarchy order: name, local transform (to the bit), mesh and its vertex count, materials
    /// and shadow mode, box colliders, lights. Two builds with the same signature placed the same pieces.</summary>
    static string PieceSignature(Transform root, out int pieces)
    {
        var sb = new StringBuilder();
        pieces = 0;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t == root) continue;
            pieces++;
            sb.Append(t.name).Append('|').Append(Bits(t.localPosition)).Append('|').Append(Bits(t.localRotation)).Append('|').Append(Bits(t.localScale));
            var mf = t.GetComponent<MeshFilter>();
            if (mf != null) sb.Append("|m ").Append(mf.sharedMesh != null ? mf.sharedMesh.name + " " + mf.sharedMesh.vertexCount : "-");
            var r = t.GetComponent<Renderer>();
            if (r != null)
            {
                sb.Append("|r ").Append(r.shadowCastingMode).Append(' ').Append(r.enabled ? 1 : 0);
                foreach (var m in r.sharedMaterials) sb.Append(' ').Append(m != null ? m.name : "-");
            }
            foreach (var bc in t.GetComponents<BoxCollider>()) sb.Append("|c ").Append(Bits(bc.center)).Append(' ').Append(Bits(bc.size));
            var lt = t.GetComponent<Light>();
            if (lt != null)
                sb.Append("|l ").Append(lt.type).Append(' ').Append(Bits(lt.range)).Append(' ').Append(Bits(lt.spotAngle)).Append(' ').Append(Bits(lt.innerSpotAngle))
                  .Append(' ').Append(Bits(lt.intensity)).Append(' ').Append(lt.shadows).Append(' ').Append(lt.enabled ? 1 : 0).Append(' ').Append(lt.cookie != null ? lt.cookie.name : "-");
            sb.Append('\n');
        }
        return sb.ToString();
    }

    /// <summary>A room (dress + rig) from a layout under a root at the tests' place; the caller destroys the root.</summary>
    static Transform BuildRoom(string label, RunRigLayout layout, out FrontRoomsRunRig rig)
    {
        var root = new GameObject(label).transform;
        root.position = new Vector3(4992f, 0f, 4992f);
        var dress = FrontRoomsRunDress.Build(root, layout);
        rig = FrontRoomsRunRig.Create(root, layout, RunRigHost.OwnLamps, null, dress);
        return root;
    }

    static string R18TodayUnchanged()
    {
        // 1. The layout, field for field, bit for bit: the factory's defaults, the binder's arguments (the map's arch and door) and
        //    a third pair (the arch and door widths are the factory's only inputs besides the cells).
        var diffs = new List<string>();
        var leaves = 0;
        var widths = new[] { new[] { 1.4f, 1f }, new[] { FrontRooms.Map.FrontRoomsRunRooms.ArchWidth, FrontRooms.Map.ModuleUnits.DoorWidth }, new[] { 1.2f, .9f } };
        foreach (var p in widths)
        {
            leaves += SameFields(TodayCorridor1x9(p[0], p[1]), RunRigLayout.Corridor(9, p[0], p[1]), "Corridor(9, " + F(p[0]) + ", " + F(p[1]) + ")", diffs);
            leaves += SameFields(TodayCorridor1x9(p[0], p[1]), RunRigLayout.Corridor1x9(p[0], p[1]), "Corridor1x9(" + F(p[0]) + ", " + F(p[1]) + ")", diffs);
        }
        leaves += SameFields(TodayCorridor1x9(), RunRigLayout.Corridor1x9(), "Corridor1x9()", diffs);
        Require(diffs.Count == 0, diffs.Count + " differences against today's preset, first: " + (diffs.Count > 0 ? diffs[0] : ""));
        var detail = "Corridor(9) = today's Corridor1x9: " + leaves + " values in 7 builds, 0 differences";
        // 2. The pieces: the dress and the rig from today's preset and from Corridor(9), look-dev shell and the map skin (the binder's
        //    layout), piece by piece and light by light.
        for (var skin = 0; skin < 2; skin++)
        {
            var a = TodayCorridor1x9();
            var b = RunRigLayout.Corridor(9);
            if (skin == 1)
            {
                a.buildShell = false; a.skinShell = true;
                b.buildShell = false; b.skinShell = true;
            }
            FrontRoomsRunRig ra, rb;
            var rootA = BuildRoom("TEST run b today", a, out ra);
            var rootB = BuildRoom("TEST run b cells 9", b, out rb);
            try
            {
                int pa, pb;
                var sa = PieceSignature(rootA, out pa);
                var sb = PieceSignature(rootB, out pb);
                if (sa != sb)
                {
                    var la = sa.Split('\n');
                    var lb = sb.Split('\n');
                    var k = 0;
                    while (k < la.Length && k < lb.Length && la[k] == lb[k]) k++;
                    throw new Exception((skin == 1 ? "map skin" : "shell") + ": piece " + k + " differs: " + (k < la.Length ? la[k] : "-") + " / " + (k < lb.Length ? lb[k] : "-"));
                }
                ra.Evaluate(3f); rb.Evaluate(3f);
                Require(Signature(ra) == Signature(rb), "the rigs' lights differ tripped");
                ra.Evaluate(-1f); rb.Evaluate(-1f);
                Require(Signature(ra) == Signature(rb), "the rigs' lights differ armed");
                Require(Mathf.Abs(ra.ResetDuration - rb.ResetDuration) < 1e-6f, "reset " + F(ra.ResetDuration) + " / " + F(rb.ResetDuration));
                detail += "; " + (skin == 1 ? "map skin" : "shell") + " " + pa + " pieces identical";
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rootA.gameObject);
                UnityEngine.Object.DestroyImmediate(rootB.gameObject);
            }
        }
        // 3. The binder reads the room's cells from its record, 9 by default.
        var room = new FrontRooms.Map.FrontRoomsRunRoom { Cells = new FrontRooms.Map.GridCoord[9] };
        Require(FrontRoomsRunRigBinder.CellsOf(room) == 9 && FrontRoomsRunRigBinder.CellsOf(null) == 9 && FrontRoomsRunRigBinder.CellsOf(new FrontRooms.Map.FrontRoomsRunRoom()) == 9, "CellsOf: not 9 for today's record or none");
        return detail + "; the binder's CellsOf 9 (record), 9 (none)";
    }

    /// <summary>R14's frame windows over one animated reset (the whole corridor, every 3 neighbouring wraps with the heads): the worst
    /// flashes a second, the most flicks a wrap makes before its sweep, and the reset's length as ticked.</summary>
    static float ResetWindows(FrontRoomsRunRig rig, out int worstFlicks, out string worstName, out float seconds)
    {
        var wraps = Named(rig, "run b wrap light");
        var heads = Named(rig, "head light");
        rig.Trip();
        for (var k = 0; k < 240; k++) rig.Tick(Dt);
        var series = new List<float[]>();
        var times = new List<float>();
        rig.StartReset();
        var t = 0f;
        for (var k = 0; k < 240 * 15; k++)
        {
            var row = new float[wraps.Length + 1];
            for (var i = 0; i < wraps.Length; i++) row[i] = wraps[i].enabled ? wraps[i].intensity : 0f;
            var h = 0f;
            foreach (var l in heads) h += l.enabled ? l.intensity : 0f;
            row[wraps.Length] = h;
            series.Add(row); times.Add(t);
            if (rig.State != RunRigState.Resetting) break;
            rig.Tick(1f / 240f); t += 1f / 240f;
        }
        seconds = t;
        worstFlicks = 0;
        for (var i = 0; i < wraps.Length; i++)
        {
            var edges = 0; var prev = 0f;
            for (var k = 0; k < series.Count && times[k] < rig.Layout.resetRiseStart - .01f; k++) { if (series[k][i] > 0f && prev <= 0f) edges++; prev = series[k][i]; }
            worstFlicks = Mathf.Max(worstFlicks, edges);
        }
        var worst = 0f; worstName = "-";
        for (var w = -1; w + 2 < wraps.Length; w++)
        {
            var a = w < 0 ? 0 : w; var b = w < 0 ? wraps.Length - 1 : w + 2;
            var mean = new float[series.Count];
            var max = 0f;
            for (var k = 0; k < series.Count; k++)
            {
                var v = series[k][wraps.Length];
                for (var i = a; i <= b; i++) v += series[k][i];
                mean[k] = v; max = Mathf.Max(max, v);
            }
            if (max <= 0f) continue;
            var fc = new FlashCounter(mean[0] / max);
            for (var k = 0; k < series.Count; k++) fc.Add(times[k], mean[k] / max);
            var f = fc.MaxFlashesPerSecond();
            if (f > worst) { worst = f; worstName = w < 0 ? "whole corridor" : "wraps " + a + "-" + b; }
        }
        return worst;
    }

    static int Tris(Mesh m)
    {
        if (m == null) return 0;
        long n = 0;
        for (var s = 0; s < m.subMeshCount; s++) n += m.GetIndexCount(s);
        return (int)(n / 3);
    }

    /// <summary>COST: triangles at LOD0 / LOD1 / LOD2 (a renderer outside any LOD group counts at every level; a group with fewer
    /// levels shows its last), renderer slots (renderers x materials) at LOD0, and lights.</summary>
    static string Cost(Transform root)
    {
        var inGroup = new HashSet<Renderer>();
        var tris = new long[3];
        long slots = 0;
        foreach (var g in root.GetComponentsInChildren<LODGroup>(true))
        {
            var lods = g.GetLODs();
            for (var level = 0; level < 3; level++)
            {
                var lod = lods[Mathf.Min(level, lods.Length - 1)];
                foreach (var r in lod.renderers)
                {
                    if (r == null) continue;
                    var mf = r.GetComponent<MeshFilter>();
                    tris[level] += Tris(mf != null ? mf.sharedMesh : null);
                    if (level == 0) slots += r.sharedMaterials.Length;
                }
            }
            foreach (var lod in lods) foreach (var r in lod.renderers) if (r != null) inGroup.Add(r);
        }
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            if (inGroup.Contains(r) || !r.enabled) continue;
            var mf = r.GetComponent<MeshFilter>();
            var n = Tris(mf != null ? mf.sharedMesh : null);
            for (var level = 0; level < 3; level++) tris[level] += n;
            slots += r.sharedMaterials.Length;
        }
        return "tris " + tris[0] + "/" + tris[1] + "/" + tris[2] + ", " + slots + " renderer slots";
    }

    static string R19Cells()
    {
        var sb = new StringBuilder();
        var reset9 = 0f;
        var hw = 1.42f;
        for (var n = RunRigLayout.MinCells; n <= RunRigLayout.MaxCells; n++)
        {
            var l = RunRigLayout.Corridor(n);
            var len = 3f * n;
            var tag = n + " cells: ";
            Require(l.Cells == n && l.length == len && l.name == "Corridor1x" + n, tag + "length " + F(l.length) + ", " + l.Cells + " cells, " + l.name);
            // A strip every cell, every one shadowed; the stutter tube in cell 5.
            Require(l.ownLamps.Length == n && l.ownLampShadows.Length == n && l.stutterLamp == 4, tag + l.ownLamps.Length + " strips");
            for (var k = 0; k < n; k++)
                Require(l.ownLamps[k] == new Vector3(0f, 2.9f, 1.8f + 3f * k) && l.ownLampShadows[k] == k, tag + "strip " + k + " at z " + F(l.ownLamps[k].z));
            // Heads every two cells; the goal unit two cells before the goal wall; at most one 9 m interval (even counts), the last.
            var u = l.units;
            var count = (n - 1) / 2;
            Require(u.Length == count, tag + u.Length + " units, want " + count);
            Require(u[0].z == 3f && u[0].side > 0f && !u[0].remote && !u[0].goalPlus && u[0].trimPlus == -.23f && u[1].trimMinus == -.23f, tag + "the entry unit");
            Require(u[count - 1].z == len - 6f && u[count - 1].goalPlus && !u[count - 1].remote, tag + "the goal unit at z " + F(u[count - 1].z));
            var nine = 0;
            for (var i = 1; i < count; i++)
            {
                var gap = u[i].z - u[i - 1].z;
                if (gap == 9f) nine++;
                Require(gap == 6f || (gap == 9f && i == count - 1), tag + "units " + (i - 1) + "-" + i + " " + F(gap) + " m apart");
                Require(u[i].side == -u[i - 1].side && u[i].autoAim, tag + "unit " + i + " not on the other wall");
                Require(u[i].remote == (i < count - 1) && u[i].goalPlus == (i == count - 1), tag + "unit " + i + " remote / goal");
                if (i > 1) Require(u[i].trimMinus == 0f && u[i].trimPlus == 0f, tag + "unit " + i + " trimmed");
            }
            Require(nine == (n % 2 == 0 ? 1 : 0), tag + nine + " intervals of 9 m");
            // The goal assembly at the end; the faces, floor, services, post over 0 .. L.
            Require(l.goalPoint == new Vector3(0f, 1.05f, len) && l.goalNormal == Vector3.back, tag + "goal point " + l.goalPoint);
            Require(l.walls.Length == 4 && l.walls[0].length == len && l.walls[1].length == len && l.walls[2].start.z == .08f && Mathf.Abs(l.walls[3].start.z - (len - .08f)) < 1e-5f, tag + "faces");
            Require(l.floorZ1 == len && l.runZ1 == len && l.postCentre.z == len * .5f && l.postSize.z == len, tag + "floor / runs / post");
            // Probes by segment: (n + 2) / 3 boxes covering the corridor, the goal box at L - 4.54, the 9-cell bake's cubes.
            var segs = (n + 2) / 3;
            Require(l.probes.Length == segs, tag + l.probes.Length + " probe boxes, want " + segs);
            Require(l.probes[0].centre.z == 4.5f && Mathf.Abs(l.probes[segs - 1].centre.z - (len - 4.54f)) < 1e-4f, tag + "end probes");
            var reach = -1f;
            for (var i = 0; i < segs; i++)
            {
                var c = l.probes[i].centre.z; var h = l.probes[i].size.z * .5f;
                Require(l.probes[i].size == new Vector3(3.16f, 3.22f, 9.6f) && c - h <= Mathf.Max(0f, reach) + 1e-4f, tag + "probe " + i + " leaves a gap");
                if (i > 0 && i < segs - 1) Require(c == 4.5f + 9f * i, tag + "probe " + i + " at z " + F(c));
                reach = Mathf.Max(reach, c + h);
            }
            Require(reach >= len, tag + "the probes stop at z " + F(reach));
            if (n == RunRigLayout.TodayCells) Require(string.IsNullOrEmpty(l.probeBake) && l.probeCubes.Length == 0, tag + "probe bake set");
            else
            {
                Require(l.probeBake == RunRigLayout.ProbeBakeName && l.probeCubes.Length == segs && l.probeCubes[0] == 0 && l.probeCubes[segs - 1] == 2, tag + "probe cubes");
                for (var i = 1; i < segs - 1; i++) Require(l.probeCubes[i] == 1, tag + "probe cube " + i);
            }
            // An obstacle in every pool but the goal's, on alternate walls.
            Require(l.obstacles.Length == count - 1, tag + l.obstacles.Length + " obstacles");
            for (var p = 0; p < l.obstacles.Length; p++)
            {
                var o = l.obstacles[p];
                var pool = (u[p].z + u[p + 1].z) * .5f;
                Require(Mathf.Abs(o.position.z - pool) <= .5f && Mathf.Sign(o.position.x) == (p % 2 == 0 ? 1f : -1f), tag + "obstacle " + p + " at " + o.position + ", pool " + F(pool));
            }
            // Side doors: ELECTRICAL in cell 1 (right), NOT AN EXIT two cells before the goal (right), STAFF ONLY in cell 5 (left) when
            // a cell short of it; each 1.2 m or more from a unit on its wall and clear of every other door and cabinet there; gapped.
            var doors = l.sideDoors;
            var staff = 17f <= len - 7f;
            Require(doors.Length == (staff ? 3 : 2) && doors[0].z == 5f && doors[0].side > 0f && doors[0].face == "electrical", tag + doors.Length + " side doors");
            Require(doors[doors.Length - 1].z == len - 4f && doors[doors.Length - 1].side > 0f && doors[doors.Length - 1].face == "stencil", tag + "NOT AN EXIT at z " + F(doors[doors.Length - 1].z));
            if (staff) Require(doors[1].z == 17f && doors[1].side < 0f && doors[1].face == "staff", tag + "STAFF ONLY");
            var cabinets = new List<float>();
            foreach (var it in l.wallItems) if (it.kit == "Kit_ExtinguisherCabinet") cabinets.Add(it.point.z);
            foreach (var d in doors)
            {
                foreach (var unit in u) if (unit.side == d.side) Require(Mathf.Abs(d.z - unit.z) >= 1.2f, tag + "door at z " + F(d.z) + " on the unit at z " + F(unit.z));
                foreach (var e in doors) if (e.z != d.z && e.side == d.side) Require(Mathf.Abs(e.z - d.z) >= 3f, tag + "doors at z " + F(d.z) + " and " + F(e.z));
                if (d.side < 0f) foreach (var c in cabinets) Require(Mathf.Abs(c - d.z) >= .7f, tag + "door at z " + F(d.z) + " on the cabinet at z " + F(c));
                var face = l.walls[d.side > 0f ? 1 : 0];
                var gapped = false;
                foreach (var g in face.gaps) if (Mathf.Abs(g.x - (d.z - .545f)) < 1e-4f && Mathf.Abs(g.y - (d.z + .545f)) < 1e-4f) gapped = true;
                Require(gapped, tag + "no face gap for the door at z " + F(d.z));
            }
            // Extinguisher cabinets on the left: every point of the corridor within 75 ft (22.86 m) of one; each gapped.
            Require(cabinets.Count == (len > 10.2f + 22.86f ? 2 : 1) && cabinets[0] == 10.2f, tag + cabinets.Count + " cabinets");
            for (var z = 0f; z <= len; z += .5f)
            {
                var near = float.MaxValue;
                foreach (var c in cabinets) near = Mathf.Min(near, Mathf.Abs(z - c));
                Require(near <= 22.86f, tag + "z " + F(z) + " is " + F(near) + " m from a cabinet");
            }
            // Compartment frames: none unless asked; asked, every third cell line inside the room.
            Require(l.compartmentFrames.Length == 0, tag + "frames without being asked");
            var framed = RunRigLayout.Corridor(n, 1.4f, 1f, true);
            Require(framed.compartmentFrames.Length == (n - 1) / 3, tag + framed.compartmentFrames.Length + " frames");
            for (var i = 0; i < framed.compartmentFrames.Length; i++) Require(framed.compartmentFrames[i] == 9f * (i + 1) && framed.compartmentFrames[i] < len, tag + "frame " + i);
            // The binder reads the length from the room's record.
            Require(FrontRoomsRunRigBinder.CellsOf(new FrontRooms.Map.FrontRoomsRunRoom { Cells = new FrontRooms.Map.GridCoord[n] }) == n, tag + "CellsOf");
            // Built: lights, the lanes, the trip and the reset over the current length.
            FrontRoomsRunRig rig;
            var root = BuildRoom("TEST run b cells " + n, l, out rig);
            try
            {
                var wraps = Named(rig, "run b wrap light");
                var heads = Named(rig, "head light");
                Require(wraps.Length == n && heads.Length == 2 * count, tag + wraps.Length + " wraps, " + heads.Length + " heads");
                int total, on, sh;
                rig.Evaluate(-1f); rig.CountLights(out total, out on, out sh);
                var armed = on + "/" + sh;
                Require(on == n + 1 && sh == n, tag + "armed " + armed);
                rig.Evaluate(3f); rig.CountLights(out total, out on, out sh);
                var tripped = on + "/" + sh;
                Require(on == 2 * count + 1 && sh == 2 * count, tag + "tripped " + tripped);
                var lane = float.MaxValue;
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (!t.name.StartsWith("run b obstacle ", StringComparison.Ordinal)) continue;
                    float x0 = float.MaxValue, x1 = float.MinValue;
                    foreach (var bc in t.GetComponentsInChildren<BoxCollider>(true))
                        for (var c = 0; c < 8; c++)
                        {
                            var corner = bc.center + Vector3.Scale(bc.size * .5f, new Vector3((c & 1) == 0 ? -1f : 1f, (c & 2) == 0 ? -1f : 1f, (c & 4) == 0 ? -1f : 1f));
                            var x = root.InverseTransformPoint(bc.transform.TransformPoint(corner)).x;
                            x0 = Mathf.Min(x0, x); x1 = Mathf.Max(x1, x);
                        }
                    if (x0 <= x1) lane = Mathf.Min(lane, Mathf.Max(x0 + hw, hw - x1));
                }
                Require(l.obstacles.Length == 0 || lane >= 1.4f, tag + "lane " + F(lane) + " m beside an obstacle");
                var trip = new LampTrace(Lights(rig));
                for (var ms = -100; ms <= 3000; ms += 2) { rig.Evaluate(ms / 1000f); trip.Sample(ms / 1000f); }
                string worstLamp; var tf = trip.WorstFlashes(out worstLamp);
                Require(tf <= 3f, tag + "trip " + F(tf) + " flashes/s on " + worstLamp);
                // The reset: n x resetCellSeconds of sweep from the entrance; the last strip rises once, from its cell's start.
                var want = l.resetRiseStart + n * l.resetCellSeconds + l.resetRise + .2f;
                Require(Mathf.Abs(rig.ResetDuration - want) < 1e-4f, tag + "reset " + F(rig.ResetDuration) + " s, want " + F(want));
                if (n == RunRigLayout.TodayCells) reset9 = rig.ResetDuration;
                var lastZ = l.ownLamps[n - 1].z;
                var lastStart = l.resetRiseStart + (n - 1) * l.resetCellSeconds;
                rig.ResetImmediate();
                Require(rig.RestrikeLevel(lastStart - .01f, lastZ) == 0f && rig.RestrikeLevel(lastStart + .2f, lastZ) > .35f, tag + "the last strip does not rise at " + F(lastStart) + " s");
                int flicks; string window; float seconds;
                var rf = ResetWindows(rig, out flicks, out window, out seconds);
                Require(rig.State == RunRigState.Armed && Mathf.Abs(seconds - want) <= 2f / 240f, tag + "the reset ticked " + F(seconds) + " s, want " + F(want));
                Require(flicks == 1 && rf <= 3f, tag + "reset " + F(rf) + " flashes/s (" + window + "), " + flicks + " flicks a strip");
                sb.Append(n).Append(" cells ").Append(F(len)).Append(" m: units ");
                for (var i = 0; i < count; i++) sb.Append(i > 0 ? "," : "").Append(F(u[i].z)).Append(u[i].side > 0f ? "R" : "L");
                sb.Append("; doors ").Append(doors.Length).Append(", obstacles ").Append(l.obstacles.Length).Append(", cabinets ").Append(cabinets.Count)
                  .Append(", probes ").Append(segs).Append("; lights armed ").Append(armed).Append(", tripped ").Append(tripped)
                  .Append("; reset ").Append(F(rig.ResetDuration)).Append(" s, ").Append(F(rf)).Append(" flashes/s; lane ").Append(F(lane)).Append(" m; ")
                  .Append(Cost(root)).Append(" | ");
            }
            finally { UnityEngine.Object.DestroyImmediate(root.gameObject); }
        }
        // A corridor outside 7 .. 14 cells is refused (a warning) and built at 9.
        Require(RunRigLayout.Corridor(6).Cells == 9 && RunRigLayout.Corridor(15).Cells == 9 && !RunRigLayout.ValidCells(6) && !RunRigLayout.ValidCells(15), "6 or 15 cells not refused");
        Require(Mathf.Abs(reset9 - 2.4f) < 1e-4f, "today's reset " + F(reset9) + " s, not 2.4");
        Log.AppendLine("R19 COST " + sb);
        return sb.Append("6 and 15 cells refused (built at 9)").ToString();
    }

    static string W1Gate()
    {
        Require(!FrontRoomsRunRig.WebGLTier, "WebGLTier is on in the editor");
        return "WebGLTier false (desktop path); the preview-define compile is roslyn_check with FRONTROOMS_WEBGL_PREVIEW";
    }
}
