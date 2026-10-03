using System;
using System.Collections.Generic;
using System.IO;
using FrontRooms.Map;
using UnityEditor;
using UnityEngine;

/// <summary>
/// The player camera rig (FrontRoomsCameraRig), checked without playing:
/// gameplay's BaseEye never moves with a shot, shake or offset; shots blend
/// in and out and report ShotStarted / ShotEnded once; the Relay frees the
/// player from a shot at once; E and S are swallowed during a shot (cancel
/// after the delay; one E in the last 0.2 s of a door shot is a buffered
/// push); every offset stops short of a wall; a shake ends at its decay time;
/// FOV changes are deltas; Camera motion Off plays a shot in place with look
/// free; the climb's stance drop is part of BaseEye at once.
/// Writes Verification/camera-rig-tests.json.
/// Headless: -executeMethod FrontRoomsCameraRigTests.RunBatch -quit (throws on FAIL).
/// </summary>
public static class FrontRoomsCameraRigTests
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

    [MenuItem("FrontRooms/Map/Test camera rig")]
    public static void Run() => Execute(false);

    public static void RunBatch() => Execute(true);

    static void Execute(bool throwOnFail)
    {
        report = new Report();
        var root = new GameObject("CAMERA RIG TEST") { hideFlags = HideFlags.DontSave };
        root.transform.position = new Vector3(-40f * ModuleUnits.WorldPeriod, 0f, -40f * ModuleUnits.WorldPeriod);
        FrontRoomsSettings.Load();
        var motion = FrontRoomsSettings.CameraMotionPercent;
        while (FrontRoomsSettings.CameraMotionPercent < 100) FrontRoomsSettings.StepCameraMotion(1);
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
            // Back to the player's own setting, whichever way it lies.
            for (var i = 0; i < 4 && FrontRoomsSettings.CameraMotionPercent != motion; i++)
                FrontRoomsSettings.StepCameraMotion(motion > FrontRoomsSettings.CameraMotionPercent ? 1 : -1);
        }
        var pass = report.failed == 0 && report.passed > 0;
        report.verdict = (pass ? "PASS" : "FAIL") + ": " + report.passed + " passed, " + report.failed + " failed";
        var path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Verification", "camera-rig-tests.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonUtility.ToJson(report, true));
        var text = "[CameraRigTests] " + report.verdict + "\n" + string.Join("\n", report.checks);
        if (pass) Debug.Log(text); else Debug.LogError(text);
        if (throwOnFail && !pass) throw new Exception(report.verdict);
    }

    static void Tick(FrontRoomsCameraRig rig, float seconds)
    {
        for (var t = 0f; t < seconds; t += Dt) rig.Tick(Dt);
    }

    static void Tests(Transform root)
    {
        var body = new GameObject("body").transform;
        body.SetParent(root, false);
        var capsule = body.gameObject.AddComponent<CapsuleCollider>();
        capsule.height = ModuleUnits.PlayerHeight;
        capsule.radius = ModuleUnits.PlayerRadius;
        capsule.center = Vector3.up * (ModuleUnits.PlayerHeight * .5f);
        var camera = new GameObject("camera").AddComponent<Camera>();
        camera.fieldOfView = 76f;
        camera.transform.SetParent(root, false);
        var rig = FrontRoomsCameraRig.Attach(body, camera, ModuleUnits.PlayerEye);
        Physics.SyncTransforms();

        // Rest: the camera is the eye.
        rig.SetBase(10f);
        rig.Tick(Dt);
        var eye = rig.BaseEye;
        Check((eye.position - (body.position + Vector3.up * ModuleUnits.PlayerEye)).magnitude < 1e-4f && Quaternion.Angle(eye.rotation, body.rotation * Quaternion.Euler(10f, 0f, 0f)) < .01f,
            "rest: BaseEye is the body plus 1.62 m with the base pitch");
        Check((camera.transform.position - eye.position).magnitude < 1e-4f && Quaternion.Angle(camera.transform.rotation, eye.rotation) < .01f && Mathf.Abs(camera.fieldOfView - 76f) < .01f,
            "rest: with no layers the camera sits exactly on BaseEye");

        // A shot: the camera goes, gameplay's eye stays.
        int started = 0, ended = 0;
        Action<ShotKind, Vector3> onStart = (k, f) => started++, onEnd = (k, f) => ended++;
        FrontRoomsCameraRig.ShotStarted += onStart;
        FrontRoomsCameraRig.ShotEnded += onEnd;
        try
        {
            var target = new Pose(eye.position + body.forward * .8f - Vector3.up * .25f, Quaternion.LookRotation(body.forward - Vector3.up * .8f));
            var shot = rig.BeginShot(new ShotSpec { kind = ShotKind.Unlock, target = () => target, focus = target.position, blendIn = .4f, blendOut = .3f, fovDelta = -14f, lockMove = true, lockLook = true, cancel = ShotCancel.PlayerInput | ShotCancel.Relay });
            Tick(rig, .5f);
            Check(started == 1 && rig.InShot && rig.MoveLocked && rig.LookLocked, "shot: ShotStarted once; move and look locked");
            Check((camera.transform.position - target.position).magnitude < .02f && Quaternion.Angle(camera.transform.rotation, target.rotation) < 1f, "shot: the camera reaches the shot pose after the blend-in");
            Check((rig.BaseEye.position - eye.position).magnitude < 1e-4f, "shot: BaseEye does not move with the shot");
            Check(Mathf.Abs(camera.fieldOfView - 62f) < .05f, "shot: FOV is the player's 76 minus 14 (" + camera.fieldOfView.ToString("0.0") + ")");
            Check(rig.HudFade > .99f, "shot: the HUD is faded");
            Check(rig.Consume(ShotInput.Use) && shot.Cancelled, "shot: E after the cancel delay is swallowed and cancels the shot");
            Tick(rig, .4f);
            Check(!rig.InShot && ended == 1 && (camera.transform.position - eye.position).magnitude < 1e-3f && Mathf.Abs(camera.fieldOfView - 76f) < .05f,
                "shot: after the cancel the camera is back on the eye, FOV restored, ShotEnded once");

            // E early in a door shot is swallowed; one in its last 0.2 s is buffered as a push, not a cancel.
            var shot2 = rig.BeginShot(new ShotSpec { kind = ShotKind.Open, target = () => target, blendIn = .2f, blendOut = .2f, cancel = ShotCancel.PlayerInput, duration = FrontRoomsShotTimings.Open.Duration });
            Tick(rig, .05f);
            Check(rig.Consume(ShotInput.Use) && !shot2.Cancelled && !rig.BufferedPush, "push: E before the cancel delay is swallowed, neither a cancel nor a push");
            Tick(rig, FrontRoomsShotTimings.Open.Duration - FrontRoomsShotTimings.DoorShotBufferedPush + .05f - shot2.Age);
            Check(rig.Consume(ShotInput.Use) && !shot2.Cancelled && rig.BufferedPush, "push: E in the last 0.2 s of a door shot is kept as a buffered push (age " + shot2.Age.ToString("0.00") + " s)");
            // The Relay frees the player only from shots that allow it.
            rig.CancelForRelay();
            Check(!shot2.Cancelled, "relay: a shot without the Relay flag is not cancelled by it");
            rig.EndShot(shot2);
            Tick(rig, .3f);
            var glass = rig.BeginShot(new ShotSpec { kind = ShotKind.Glass, target = () => target, blendIn = .1f, blendOut = .1f, duration = 1f });
            Check(!rig.BufferedPush, "push: a new shot clears a push buffered in an earlier one");
            Tick(rig, .9f);
            Check(rig.Consume(ShotInput.Use) && !rig.BufferedPush && !glass.Cancelled, "push: E at the end of a glass shot is not a push");
            rig.EndShot(glass);
            Tick(rig, .2f);
            var shot3 = rig.BeginShot(new ShotSpec { kind = ShotKind.Unlock, target = () => target, blendIn = .4f, blendOut = .4f, lockMove = true, cancel = ShotCancel.Relay });
            Tick(rig, .5f);
            rig.CancelForRelay();
            Tick(rig, FrontRoomsShotTimings.Unlock.ChaseFreeSnap + .02f);
            Check(shot3.Cancelled && !rig.InShot && !rig.MoveLocked, "relay: a chase frees the player within " + FrontRoomsShotTimings.Unlock.ChaseFreeSnap + " s");

            // Never through a wall: a box 0.5 m ahead of the eye.
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.SetParent(root, false);
            wall.transform.position = eye.position + body.forward * .5f + body.forward * .05f;
            wall.transform.localScale = new Vector3(2f, 2f, .1f);
            wall.transform.rotation = body.rotation;
            Physics.SyncTransforms();
            rig.SetBase(0f);
            var through = new Pose(eye.position + body.forward * 1.5f, body.rotation);
            rig.BeginShot(new ShotSpec { kind = ShotKind.Open, target = () => through, blendIn = .1f, blendOut = .1f });
            Tick(rig, .3f);
            var reach = Vector3.Dot(camera.transform.position - rig.BaseEye.position, body.forward);
            Check(reach < .5f - FrontRoomsShotTimings.WorldClampRadius + .02f && reach > .2f, "clamp: a shot pose behind a wall stops 0.1 m short of it (" + reach.ToString("0.000") + " m in, wall face at 0.50)");
            rig.ResetLayers();
            UnityEngine.Object.DestroyImmediate(wall);
            Physics.SyncTransforms();

            // Shake and offsets are picture only and decay: a shake is gone at its decay time.
            rig.AddTrauma(1.5f, .25f);
            rig.Tick(Dt);
            var shaken = Quaternion.Angle(camera.transform.rotation, rig.BaseEye.rotation);
            var live = rig.Trauma;
            // 0.25 s is 15 frames: live after 14, gone after 16 (the 15th still shows its last amplitude).
            for (var i = 0; i < 13; i++) rig.Tick(Dt);
            var nearEnd = rig.Trauma;
            rig.Tick(Dt);
            rig.Tick(Dt);
            Check(live > 1f && shaken > .05f && nearEnd > 0f && rig.Trauma == 0f
                  && Quaternion.Angle(camera.transform.rotation, rig.BaseEye.rotation) < .01f && (rig.BaseEye.position - eye.position).magnitude < 1e-4f,
                "shake: rotates the camera (" + shaken.ToString("0.00") + "°), still live just before 0.25 s (" + nearEnd.ToString("0.000") + "°), gone at it, never moves BaseEye");
            rig.AddOffset(new Vector3(0f, -.06f, 0f), .3f);
            Tick(rig, .15f);
            var dipped = rig.BaseEye.position.y - camera.transform.position.y;
            Tick(rig, .2f);
            Check(dipped > .04f && Mathf.Abs(rig.BaseEye.position.y - camera.transform.position.y) < 1e-4f, "offset: a 6 cm dip goes there and back (" + dipped.ToString("0.000") + ")");
            rig.PushFov(-3f, .3f);
            Tick(rig, .15f);
            var pushed = camera.fieldOfView;
            Tick(rig, .2f);
            Check(pushed < 74f && Mathf.Abs(camera.fieldOfView - 76f) < .01f, "fov: a -3° push and back (" + pushed.ToString("0.0") + ")");

            // Camera motion off: the shot plays in place with look free; the move lock stays (gameplay).
            while (FrontRoomsSettings.CameraMotionPercent > 0) FrontRoomsSettings.StepCameraMotion(-1);
            var still = rig.BeginShot(new ShotSpec { kind = ShotKind.Unlock, target = () => target, blendIn = .2f, blendOut = .2f, fovDelta = -14f, lockMove = true, lockLook = true });
            Tick(rig, .4f);
            Check((camera.transform.position - rig.BaseEye.position).magnitude < 1e-4f && Mathf.Abs(camera.fieldOfView - 76f) < .01f && !rig.LookLocked && rig.MoveLocked,
                "motion off: the shot plays in place, look free, move still locked");
            float freeYaw = body.eulerAngles.y + 40f, freePitch = 30f;
            rig.ClampLook(ref freeYaw, ref freePitch);
            Check(Mathf.Abs(Mathf.DeltaAngle(body.eulerAngles.y + 40f, freeYaw)) < 1e-3f && Mathf.Abs(freePitch - 30f) < 1e-3f, "motion off: no look cone, look stays free");
            rig.AddTrauma(2f, .3f);
            rig.Tick(Dt);
            Check(rig.Trauma == 0f && Quaternion.Angle(camera.transform.rotation, rig.BaseEye.rotation) < .01f, "motion off: no shake");
            rig.EndShot(still);
            Tick(rig, .3f);
            while (FrontRoomsSettings.CameraMotionPercent < 100) FrontRoomsSettings.StepCameraMotion(1);

            // The climb's duck is part of the stance: gameplay sees it the moment it is set.
            rig.StanceDrop = .55f;
            var ducked = Mathf.Abs(rig.BaseEye.position.y - (body.position.y + ModuleUnits.PlayerEye - .55f)) < 1e-4f;
            rig.SetBase(0f);
            rig.Tick(Dt);
            Check(ducked && Mathf.Abs(rig.BaseEye.position.y - (body.position.y + ModuleUnits.PlayerEye - .55f)) < 1e-4f && (camera.transform.position - rig.BaseEye.position).magnitude < 1e-4f,
                "stance: the climb duck lowers BaseEye at once, and the camera with it");
            rig.StanceDrop = 0f;
            rig.SetBase(0f);

            // A shot's look cone keeps the base look near the shot's heading.
            var cone = rig.BeginShot(new ShotSpec { kind = ShotKind.Glass, target = () => new Pose(rig.BaseEye.position, body.rotation), blendIn = .1f, blendOut = .1f, lookCone = 8f });
            float yaw = body.eulerAngles.y + 40f, lookPitch = 30f;
            rig.ClampLook(ref yaw, ref lookPitch);
            Check(Mathf.Abs(Mathf.DeltaAngle(body.eulerAngles.y, yaw)) <= 8.01f && lookPitch <= 8.01f, "cone: look is held within ±8° of the shot's heading");
            rig.EndShot(cone);
            yaw = body.eulerAngles.y + 40f;
            lookPitch = 30f;
            rig.ClampLook(ref yaw, ref lookPitch);
            Check(Mathf.Abs(Mathf.DeltaAngle(body.eulerAngles.y + 40f, yaw)) < 1e-3f && Mathf.Abs(lookPitch - 30f) < 1e-3f, "cone: an ending shot frees look at once");
            Tick(rig, .2f);
        }
        finally
        {
            FrontRoomsCameraRig.ShotStarted -= onStart;
            FrontRoomsCameraRig.ShotEnded -= onEnd;
        }
    }
}
