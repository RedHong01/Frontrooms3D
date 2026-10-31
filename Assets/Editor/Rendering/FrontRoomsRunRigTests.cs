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
///   R7  corridor light budget (plan section 3, C41 + C42): armed 10 (9 wraps + the goal window light; 3 shadowed),
///       blackout 1 (the goal window light), tripped 9 (8 heads shadowed + the goal window light).
///   R8  C41: no EXIT sign anywhere in the room (no FrontRoomsExitSign, no Kit_ExitSign*); the red fire gear stays.
///   R9  C42: the goal window light: unshadowed, lit from the trip line though past the 16 m cull, off as the far lamp comes
///       on (FarRoomLamp 1), half at 0.5, its no-lamp fade near the door, off once the viewer is past the goal wall,
///       untouched by the trip.
///   R10 side doors on the RN-K DA kits: 3 frames + 3 leaves, every lite DEAD, no closer, the frame inside the wall.
///   R11 CR-4 warning burst on the wraps: at most 3 flashes a second, ends armed and whole; REDUCE FLASHING one sag >= 0.69.
///   R12 the map skin (skinShell, the binder's layout): the soffit skin, the arch reveal on the room's half of the wall
///       (2 jambs x lower tone, cove, upper tone + the head), none at the goal door, and the lower tone running on behind
///       the extinguisher cabinet (the map's CR-3 finish shows nowhere).
///   (S1 / S2 / S3, the title-stream checks, were dropped with the RoomStream hunk: call 6 / C40, 2026-10-31.)
///   W1  the WebGL tier is off in the editor (the FRONTROOMS_WEBGL_PREVIEW compile is checked outside Unity).
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
            Run("R7 corridor light budget (armed 10/3, blackout 1, tripped 9/8)", R7Budget);
            Run("R8 C41: no EXIT sign in the room, the fire gear stays", R8NoExitSign);
            Run("R9 C42: the goal window light", R9GoalLight);
            Run("R10 side doors: RN-K DA kits, dead lites, no closer", R10SideDoors);
            Run("R11 CR-4 warning burst on the wraps", R11WarnBurst);
            Run("R12 the map skin: soffit, arch reveal, lower tone behind the cabinet", R12MapSkin);
            Run("W1 WebGL tier off in the editor", W1Gate);
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
            Require(on == 10 && sh == 3, detail);
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

    static string W1Gate()
    {
        Require(!FrontRoomsRunRig.WebGLTier, "WebGLTier is on in the editor");
        return "WebGLTier false (desktop path); the preview-define compile is roslyn_check with FRONTROOMS_WEBGL_PREVIEW";
    }
}
