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

    static string W1Gate()
    {
        Require(!FrontRoomsRunRig.WebGLTier, "WebGLTier is on in the editor");
        return "WebGLTier false (desktop path); the preview-define compile is roslyn_check with FRONTROOMS_WEBGL_PREVIEW";
    }
}
