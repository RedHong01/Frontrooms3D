using System;
using System.Collections.Generic;
using System.IO;
using FrontRooms.Map;
using UnityEditor;
using UnityEngine;
using G = FrontRoomsShotTimings.GlassBreak;

/// <summary>
/// The glass break's shot (FrontRoomsGlassShot), checked without playing:
/// the keyframes stay inside the plan's comfort limits and come back to the
/// eye; each beat lands once (a resumed hold does not replay one) and BaseEye
/// never moves; letting go cancels, a resume starts at the stage reached; the
/// hard lock round the shatter and the soft lock after it; a chase demotes the
/// hold and skips the shot after the shatter; Camera motion off plays it in
/// place; the step-in's stand point; the look cone.
/// Writes Verification/glass-shot-tests.json.
/// Headless: -executeMethod FrontRoomsGlassShotTests.RunBatch -quit (throws on FAIL).
/// </summary>
public static class FrontRoomsGlassShotTests
{
    const float Dt = 1f / 60f;

    [Serializable]
    sealed class Report
    {
        public string verdict;
        public int passed, failed;
        public List<string> checks = new List<string>();
    }

    static Report report;

    static void Check(bool ok, string what)
    {
        if (ok) report.passed++; else report.failed++;
        report.checks.Add((ok ? "ok   " : "FAIL ") + what);
    }

    [MenuItem("FrontRooms/Map/Test glass shot")]
    public static void Run() => Execute(false);

    public static void RunBatch() => Execute(true);

    static void Execute(bool throwOnFail)
    {
        report = new Report();
        var root = new GameObject("GLASS SHOT TEST") { hideFlags = HideFlags.DontSave };
        root.transform.position = new Vector3(-41f * ModuleUnits.WorldPeriod, 0f, -40f * ModuleUnits.WorldPeriod);
        FrontRoomsSettings.Load();
        var motion = FrontRoomsSettings.CameraMotionPercent;
        SetMotion(100);
        try
        {
            Tests(root.transform);
        }
        catch (Exception e)
        {
            Check(false, "exception: " + e.GetType().Name + " " + e.Message + "\n" + e.StackTrace);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            SetMotion(motion);
        }
        var pass = report.failed == 0 && report.passed > 0;
        report.verdict = (pass ? "PASS" : "FAIL") + ": " + report.passed + " passed, " + report.failed + " failed";
        var path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Verification", "glass-shot-tests.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonUtility.ToJson(report, true));
        var text = "[GlassShotTests] " + report.verdict + "\n" + string.Join("\n", report.checks);
        if (pass) Debug.Log(text); else Debug.LogError(text);
        if (throwOnFail && !pass) throw new Exception(report.verdict);
    }

    static void SetMotion(int percent)
    {
        for (var i = 0; i < 4 && FrontRoomsSettings.CameraMotionPercent != percent; i++)
            FrontRoomsSettings.StepCameraMotion(percent > FrontRoomsSettings.CameraMotionPercent ? 1 : -1);
    }

    static void Tick(FrontRoomsCameraRig rig, FrontRoomsGlassShot shot, float seconds, bool threat = false)
    {
        for (var t = 0f; t < seconds - 1e-4f; t += Dt)
        {
            shot.Tick(Dt, threat, false, 0f);
            rig.Tick(Dt);
        }
    }

    static void Tests(Transform root)
    {
        // Keyframes: inside the comfort limits, and back to the eye.
        float maxFwd = 0f, maxRoll = 0f, minFov = 0f, maxDrop = 0f;
        for (var t = 0f; t <= G.ReleaseEnd + 1e-4f; t += .005f)
        {
            var p = FrontRoomsGlassShot.PoseAt(t);
            maxFwd = Mathf.Max(maxFwd, Mathf.Abs(p.fwd));
            maxRoll = Mathf.Max(maxRoll, Mathf.Abs(p.roll));
            maxDrop = Mathf.Max(maxDrop, p.drop);
            minFov = Mathf.Min(minFov, FrontRoomsGlassShot.FovAt(t));
        }
        var end = FrontRoomsGlassShot.PoseAt(G.ReleaseEnd);
        Check(maxFwd <= .16f + 1e-4f && maxRoll <= 4f + 1e-4f && minFov >= -8f - 1e-3f && maxDrop <= .08f + 1e-4f,
            "keys: translation ≤ 0.16 m (" + maxFwd.ToString("0.000") + "), roll ≤ 4° (" + maxRoll.ToString("0.0") + "), FOV ≥ −8° (" + minFov.ToString("0.0") + ")");
        Check(end.fwd == 0f && end.drop == 0f && end.yaw == 0f && end.roll == 0f && end.pitch == 0f && Mathf.Abs(FrontRoomsGlassShot.FovAt(G.ReleaseEnd)) < 1e-4f
              && Mathf.Abs(FrontRoomsGlassShot.FovAt(.4f) - G.PoseFovDeg) < 1e-3f && Mathf.Abs(FrontRoomsGlassShot.FovAt(1.1f) - (G.PoseFovDeg + G.ShatterFovPunchDeg)) < 1e-3f,
            "keys: −5° by 0.4 s, −8° after the shatter, everything back to 0 at 1.65 s");
        var strike = FrontRoomsGlassShot.PoseAt(G.Crack1);
        Check(Mathf.Abs(strike.fwd - G.StrikeFwd[0]) < 1e-4f && Mathf.Abs(FrontRoomsGlassShot.PoseAt(G.StrikeStart[0]).yaw - G.WindupYaw[0]) < 1e-4f,
            "keys: wound up at 0.29 s (yaw " + G.WindupYaw[0] + "), struck through at 0.35 s (fwd " + G.StrikeFwd[0] + ")");

        // The step-in's stand point: 0.55 m from the pane, ≤ 0.65 forward, ≤ 0.25 sideways.
        var impact = new Vector3(0f, 1.2f, 0f);
        var stand = FrontRoomsGlassShot.StandPoint(new Vector3(.4f, 0f, -1.4f), impact, Vector3.forward);
        var close = FrontRoomsGlassShot.StandPoint(new Vector3(0f, 0f, -.5f), impact, Vector3.forward);
        Check(Mathf.Abs(stand.z - (-1.4f + .65f)) < 1e-4f && Mathf.Abs(stand.x - (.4f - .25f)) < 1e-4f && close == new Vector3(0f, 0f, -.5f),
            "stand point: from 1.4 m it steps 0.65 forward and 0.25 across (" + stand.ToString("F2") + "); from 0.5 m it stays");

        // A rig to run the shot on.
        var body = new GameObject("body").transform;
        body.SetParent(root, false);
        var camera = new GameObject("camera").AddComponent<Camera>();
        camera.fieldOfView = 76f;
        camera.transform.SetParent(root, false);
        var rig = FrontRoomsCameraRig.Attach(body, camera, ModuleUnits.PlayerEye);
        rig.SetBase(0f);
        rig.Tick(Dt);
        var eye = rig.BaseEye;
        var pane = eye.position + body.forward * .55f;
        var shot = new FrontRoomsGlassShot(rig);
        var beats = new List<(int beat, float intensity)>();
        Action<int, float> onBeat = (b, i) => beats.Add((b, i));
        FrontRoomsGlassShot.Beat += onBeat;
        try
        {
            // A hold to the first crack: beat 1 once, the camera braced, BaseEye still.
            shot.Begin(pane, body.forward, 0f);
            for (var p = 0f; p <= .5f; p += Dt) { shot.Hold(p); shot.Tick(Dt, false, false, 0f); rig.Tick(Dt); }
            var moved = (camera.transform.position - rig.BaseEye.position).magnitude;
            Check(beats.Count == 1 && beats[0].beat == 1 && Mathf.Abs(beats[0].intensity - 1f) < 1e-4f && shot.MoveLocked && !shot.LookLocked
                  && moved > .01f && (rig.BaseEye.position - eye.position).magnitude < 1e-4f,
                "hold: beat 1 lands once at full strength, the camera moves (" + moved.ToString("0.000") + " m), move held, look free, BaseEye still");
            Check(FrontRoomsGlassShot.Weight > .99f, "hold: Weight is 1 once braced (" + FrontRoomsGlassShot.Weight.ToString("0.00") + ")");
            // Let go: back to the eye in 0.2 s.
            shot.Release();
            Tick(rig, shot, G.CancelBlend + .05f);
            Check(!shot.Active && !shot.MoveLocked && (camera.transform.position - rig.BaseEye.position).magnitude < 1e-3f && FrontRoomsGlassShot.Weight == 0f,
                "release: the shot lets go, the camera is back on the eye within 0.2 s, Weight 0");
            // Resume at the stage reached: beat 2 only.
            beats.Clear();
            shot.Begin(pane, body.forward, G.Crack1);
            for (var p = G.Crack1; p <= .8f; p += Dt) { shot.Hold(p); shot.Tick(Dt, false, false, 0f); rig.Tick(Dt); }
            Check(beats.Count == 1 && beats[0].beat == 2, "resume at 0.35: only beat 2 lands (" + beats.Count + " beats)");
            // The shatter: beat 3; move and look held to 1.10, then a move key ends it.
            for (var p = .8f; p < 1f; p += Dt) { shot.Hold(p); shot.Tick(Dt, false, false, 0f); rig.Tick(Dt); }
            shot.Shatter();
            Tick(rig, shot, .05f);
            var hard = shot.LookLocked && shot.MoveLocked;
            Tick(rig, shot, G.HardLockEnd - G.Shatter);
            var soft = !shot.LookLocked && shot.MoveLocked;
            shot.Tick(Dt, false, true, 0f);
            Check(beats.Count == 2 && beats[1].beat == 3 && hard && soft && !shot.Active && !shot.MoveLocked,
                "shatter: beat 3; move and look held to 1.10 s (" + hard + "), then soft (" + soft + "): a move key ends it");
            Tick(rig, shot, .3f);

            // A look of more than 3° in the soft window ends it too; left alone it ends at 1.65 s.
            shot.Begin(pane, body.forward, G.Crack2);
            shot.Hold(.99f);
            shot.Shatter();
            Tick(rig, shot, G.HardLockEnd - G.Shatter + .02f);
            shot.Tick(Dt, false, false, 2f);
            var stillOn = shot.Active;
            shot.Tick(Dt, false, false, 2f);
            Check(stillOn && !shot.Active, "soft look: a 2° look keeps it, 4° in all ends it");
            Tick(rig, shot, .3f);
            shot.Begin(pane, body.forward, G.Crack2);
            shot.Hold(.99f);
            shot.Shatter();
            Tick(rig, shot, G.ReleaseEnd - G.Shatter - .05f);
            var late = shot.Active;
            Tick(rig, shot, .1f);
            Check(late && !shot.Active, "left alone, the shot ends at 1.65 s");
            Tick(rig, shot, .3f);

            // A chase during the hold demotes (the hold goes on, half strength); after the shatter it skips.
            beats.Clear();
            shot.Begin(pane, body.forward, 0f);
            for (var p = 0f; p <= .2f; p += Dt) { shot.Hold(p); shot.Tick(Dt, true, false, 0f); rig.Tick(Dt); }
            var demoted = shot.Demote;
            for (var p = .2f; p <= .5f; p += Dt) { shot.Hold(p); shot.Tick(Dt, true, false, 0f); rig.Tick(Dt); }
            Check(shot.Active && demoted == 1f && beats.Count == 1 && Mathf.Abs(beats[0].intensity - G.ChaseDemoteScale) < 1e-4f,
                "chase in the hold: demoted within 0.12 s (" + demoted.ToString("0.00") + "), the hold goes on, beat at " + (beats.Count > 0 ? beats[0].intensity.ToString("0.00") : "-"));
            shot.Release();
            Tick(rig, shot, .3f);
            shot.Begin(pane, body.forward, G.Crack2);
            shot.Hold(.99f);
            shot.Shatter();
            Tick(rig, shot, .02f);
            shot.Tick(Dt, true, false, 0f);
            var skipped = !shot.Active && !shot.MoveLocked && !shot.LookLocked;
            Tick(rig, shot, G.ChaseSkipBlend + .03f);
            Check(skipped && (camera.transform.position - rig.BaseEye.position).magnitude < 1e-3f, "chase after the shatter: skipped at once, the camera back within 0.12 s");

            // A chase from E-down on: demoted through the hold, and skipped the moment the glass gives.
            shot.Begin(pane, body.forward, G.Crack2);
            for (var p = G.Crack2; p < .99f; p += Dt) { shot.Hold(p); shot.Tick(Dt, true, false, 0f); rig.Tick(Dt); }
            shot.Shatter();
            var heldSkip = !shot.Active && !shot.MoveLocked && !shot.LookLocked;
            Tick(rig, shot, .3f, true);
            Check(heldSkip, "chase from the start: the shatter skips the shot at once (no hard lock under a chase)");

            // Let go and press again within 0.2 s: the shot picks up from where the camera is, no snap.
            shot.Begin(pane, body.forward, 0f);
            for (var p = 0f; p <= .3f; p += Dt) { shot.Hold(p); shot.Tick(Dt, false, false, 0f); rig.Tick(Dt); }
            shot.Release();
            shot.Tick(Dt, false, false, 0f);
            rig.Tick(Dt);
            var blending = shot.Blending;
            var before = camera.transform.position;
            shot.Begin(pane, body.forward, G.Crack1 - .05f);
            shot.Tick(Dt, false, false, 0f);
            rig.Tick(Dt);
            var jump = (camera.transform.position - before).magnitude;
            Check(blending && shot.Active && jump < .01f, "re-press while blending back: the shot picks up again without a snap (" + (jump * 1000f).ToString("0.0") + " mm)");
            shot.Reset();
            Tick(rig, shot, .3f);

            // Camera motion off: the shot plays in place; the beats still land, at 0 strength.
            SetMotion(0);
            beats.Clear();
            shot.Begin(pane, body.forward, 0f);
            for (var p = 0f; p <= .5f; p += Dt) { shot.Hold(p); shot.Tick(Dt, false, false, 0f); rig.Tick(Dt); }
            Check(beats.Count == 1 && beats[0].intensity == 0f && (camera.transform.position - rig.BaseEye.position).magnitude < 1e-4f && Mathf.Abs(camera.fieldOfView - 76f) < 1e-3f,
                "motion off: in place (no pose, no FOV), beat 1 still lands at 0 strength");
            float offYaw = body.eulerAngles.y + 30f, offPitch = 20f;
            shot.ClampLook(ref offYaw, ref offPitch);
            var lookFree = Mathf.Abs(Mathf.DeltaAngle(body.eulerAngles.y + 30f, offYaw)) < 1e-3f && Mathf.Abs(offPitch - 20f) < 1e-3f;
            var holdLocked = shot.MoveLocked;
            shot.Hold(.99f);
            shot.Shatter();
            Check(lookFree && holdLocked && !shot.LookLocked && !shot.MoveLocked,
                "motion off: look free in the hold, move held only while holding (the glass), nothing held after the shatter");
            shot.Reset();
            Tick(rig, shot, .3f);
            SetMotion(100);

            // The look cone: ±5° round the impact while the hold runs.
            shot.Begin(pane, body.forward, 0f);
            float yaw = body.eulerAngles.y + 30f, pitch = 20f;
            shot.ClampLook(ref yaw, ref pitch);
            Check(Mathf.Abs(Mathf.DeltaAngle(body.eulerAngles.y, yaw)) <= 5.01f && pitch <= 5.01f, "look cone: held within ±5° of the impact while the hold runs");
            shot.Reset();
            Tick(rig, shot, .3f);

            // Begun with the look 20° off the impact: the cone starts that wide (no yank) and closes to ±5° over the brace.
            var offImpact = eye.position + Quaternion.Euler(0f, 20f, 0f) * body.forward * .55f;
            shot.Begin(offImpact, body.forward, 0f);
            float y0 = body.eulerAngles.y, p0 = 0f;
            shot.ClampLook(ref y0, ref p0);
            var untouched = Mathf.Abs(Mathf.DeltaAngle(body.eulerAngles.y, y0)) < 1e-3f;
            for (var k = 0; k < 20; k++) { shot.Hold(k * Dt); shot.Tick(Dt, false, false, 0f); rig.Tick(Dt); }
            float y1 = body.eulerAngles.y, p1 = 0f;
            shot.ClampLook(ref y1, ref p1);
            var drawn = Mathf.DeltaAngle(body.eulerAngles.y, y1);
            Check(untouched && drawn > 14.9f && drawn < 15.1f, "look cone: from 20° off it starts open (no yank) and closes to 5° of the impact by the end of the brace (look turned " + drawn.ToString("0.0") + "°)");
            shot.Reset();
            Tick(rig, shot, .3f);

            // A shot already blending out only ever goes faster: the climb hand-off after a soft-lock end.
            shot.Begin(pane, body.forward, G.Crack2);
            shot.Hold(.99f);
            shot.Shatter();
            Tick(rig, shot, G.HardLockEnd - G.Shatter + .02f);
            shot.Tick(Dt, false, true, 0f);
            rig.Tick(Dt);
            var endedByMove = !shot.Active && shot.Blending;
            shot.ClimbStarted();
            Tick(rig, shot, G.ClimbHandOff + Dt);
            Check(endedByMove && !shot.Blending && (camera.transform.position - rig.BaseEye.position).magnitude < 1e-3f,
                "climb hand-off: a climb on the frame the soft lock ends still hands over in 0.10 s");
            shot.Reset();
        }
        finally
        {
            FrontRoomsGlassShot.Beat -= onBeat;
        }
    }
}
