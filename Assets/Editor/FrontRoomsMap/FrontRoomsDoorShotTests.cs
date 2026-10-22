using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FrontRooms.Map;
using UnityEditor;
using UnityEngine;
using U = FrontRoomsShotTimings.Unlock;

/// <summary>
/// The unlock shot (FrontRoomsDoorShot) on a real map door with keys on, checked without playing
/// (door_shots_plan.md §5.2, checks 1–19): pose P (0.45 m out, 0.25 m down, 12° toward the hinge,
/// looking at the keyhole) and the travel; the FOV ramp and its 0.4 s return on cancels; the camera
/// at 100 / 50 / 0 % Camera motion with BaseEye still; the beats, DoorUnlocked at the 0.86 s commit
/// at the keyhole, the open at 1.00 s (drawn latch) from either face (A12: the key turn turns the shut leaf
/// away from the key's side, so the old pull-side open at 1.55 s never runs);
/// the hold (no prompt, Use and TryOpenDoor leave it); the move and look locks; player, Relay and
/// door cancels before and after the commit (locked, or unlocked and shut, or opened by a second E);
/// the buffered push; pause (DoorsPaused); in place; a chunk rebuild; Abort; orphaned holds (no
/// owner, an owner that never ticks, ApplyLive); the step-in; the lock's parts through a fake rig.
/// The whole battery runs at Camera motion 100, 50 and 0; the pure checks once.
/// Then the key pickup (checks 20-24, once at 100; the aim assist also at Off) on the map's own keys:
/// no model (instant: KeyPickupStarted, KeyTaken and the object gone in one call, no collider); the
/// placeholder (held at contact, KeyTaken at 0.25 s, the object on the map's root through a chunk drop
/// and rebuild with no respawn, held at the viewport point, the pocket beat at 0.85, gone at 0.95, kept
/// 0.1 m short of a wall, no spin); a dressed key (FrontRoomsFakeKeyRig: PoseHeld facing the lens, the
/// sway at the arrival, no shrink); the aim assist; look+E behind its flag (a trigger only the key
/// query finds); no presenter (instant, even with the placeholder on).
/// Each tick runs the driver, then the map's doors (TickDoorsForTools, which honours DoorsPaused),
/// then the rig, then the presented key (PlaceKey). Writes Verification/door-shot-tests.json.
/// Headless: -executeMethod FrontRoomsDoorShotTests.RunBatch -quit (throws on FAIL).
/// </summary>
public static class FrontRoomsDoorShotTests
{
    const float Dt = 1f / 60f;
    // The body's clearance from a leaf (FrontRoomsMapWorld.PlayerClearance: the radius and the controller's skin).
    const float LeafClearance = ModuleUnits.PlayerRadius + .04f;
    const float StandOut = .9f;

    [Serializable]
    sealed class Report
    {
        public string verdict;
        public int passed, failed;
        public List<string> checks = new List<string>();
    }

    static Report report;
    static string tag = "";
    static int warnings;

    static void Check(bool ok, string what)
    {
        if (ok) report.passed++; else report.failed++;
        report.checks.Add((ok ? "ok   " : "FAIL ") + tag + what);
    }

    [MenuItem("FrontRooms/Map/Test door shots")]
    public static void Run() => Execute(false);

    public static void RunBatch() => Execute(true);

    static void Execute(bool throwOnFail)
    {
        report = new Report();
        tag = "";
        warnings = 0;
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
        FrontRoomsSettings.Load();
        var motion = FrontRoomsSettings.CameraMotionPercent;
        var placeholder = FrontRoomsMapWorld.ShowPlaceholderKey;
        var roots = new List<GameObject>();
        var profiles = new List<FrontRoomsLevelProfile>();
        H h = null;
        Application.logMessageReceived += OnLog;
        try
        {
            Pure();
            h = Harness(roots, profiles);
            if (h != null)
            {
                StepIn(h);
                foreach (var m in new[] { 100, 50, 0 })
                {
                    SetMotion(m);
                    tag = "[motion " + m + "] ";
                    Battery(h);
                }
                SetMotion(100);
                tag = "[keys] ";
                Keys(h, roots);
            }
        }
        catch (Exception e)
        {
            Check(false, "exception: " + e.GetType().Name + " " + e.Message + "\n" + e.StackTrace);
        }
        finally
        {
            tag = "";
            Application.logMessageReceived -= OnLog;
            h?.Unhook();
            foreach (var r in roots)
            {
                if (r == null) continue;
                var w = r.GetComponent<FrontRoomsMapWorld>();
                if (w != null) w.Release();
                UnityEngine.Object.DestroyImmediate(r);
            }
            foreach (var p in profiles) if (p != null) UnityEngine.Object.DestroyImmediate(p);
            SetMotion(motion);
            FrontRoomsMapWorld.ShowPlaceholderKey = placeholder;
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
        var path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Verification", "door-shot-tests.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonUtility.ToJson(report, true));
        var text = "[DoorShotTests] " + report.verdict + "\n" + string.Join("\n", report.checks);
        if (pass) Debug.Log(text); else Debug.LogError(text);
        if (throwOnFail && !pass) throw new Exception(report.verdict);
    }

    static void OnLog(string condition, string stack, LogType type)
    {
        if (type == LogType.Warning && condition.Contains("A key turn on door")) warnings++;
    }

    static void SetMotion(int percent)
    {
        for (var i = 0; i < 4 && FrontRoomsSettings.CameraMotionPercent != percent; i++)
            FrontRoomsSettings.StepCameraMotion(percent > FrontRoomsSettings.CameraMotionPercent ? 1 : -1);
    }

    static bool Near(float a, float b, float tolerance) => Mathf.Abs(a - b) <= tolerance;

    // ------------------------------------------------------------------ the harness

    /// <summary>A rig on the door that records every call the shot makes, with the shot's clock.</summary>
    sealed class FakeRig : IFrontRoomsDoorRig
    {
        public readonly List<(string part, DoorFace face, float value, float t)> calls = new List<(string, DoorFace, float, float)>();
        public Func<float> clock;
        public Pose s, p;
        // A centimetre above the spec's keyhole, so a pose taken from the rig is told from the fallback.
        public static readonly Vector3 Lift = Vector3.up * .01f;
        Pose Of(DoorFace face) => face == DoorFace.S ? s : p;
        public Vector3 Keyhole(DoorFace face) => Of(face).position + Lift;
        public Vector3 KeyholeDir(DoorFace face) => Of(face).rotation * Vector3.forward;
        public Vector3 KeyholeUp(DoorFace face) => Of(face).rotation * Vector3.up;
        public void SetPlug(DoorFace face, float degrees) => calls.Add(("plug", face, degrees, clock()));
        public void SetKnob(DoorFace face, float degrees) => calls.Add(("knob", face, degrees, clock()));
        public void SetLever(DoorFace face, float degrees) => calls.Add(("lever", face, degrees, clock()));
        public void SetDeadbolt(float t) => calls.Add(("bolt", DoorFace.S, t, clock()));
        public void SetLatch(float t) => calls.Add(("latch", DoorFace.S, t, clock()));
        public void Jolt(float metres) => calls.Add(("jolt", DoorFace.S, metres, clock()));
    }

    sealed class H
    {
        public FrontRoomsMapWorld world;
        public FrontRoomsLevelProfile profile;
        public Transform body;
        public Camera camera;
        public FrontRoomsCameraRig rig;
        public FrontRoomsDoorShot shot;
        public FrontRoomsMapWorld.Door door;
        public GridCoord a, b;
        public float clock;
        // This case: where the body stood, the keyhole, the face and pose P.
        public Vector3 feet, normal;
        public Pose lockPose, poseP, eye0;
        public DoorFace face;
        public bool pull;
        public readonly List<(DoorBeat beat, float t)> beats = new List<(DoorBeat, float)>();
        public readonly List<(float t, GridCoord zone, Vector3 at)> unlocked = new List<(float, GridCoord, Vector3)>();
        public readonly List<float> handles = new List<float>(), moves = new List<float>(), pulls = new List<float>();
        public readonly List<(float t, FrontRoomsMapWorld.DoorOpenSource source, bool latchDrawn)> began = new List<(float, FrontRoomsMapWorld.DoorOpenSource, bool)>();
        public int started, ended;
        public float startedAt = -1f, endedAt = -1f;
        // The key pickup: KeyPickupStarted ("S zone") and KeyTaken ("T zone") in the order raised, their clocks,
        // where the last pickup started, and the pocket beats.
        public readonly List<string> keyOrder = new List<string>();
        public readonly List<float> keyStartedAt = new List<float>(), keyTakenAt = new List<float>(), pocketAt = new List<float>();
        public Vector3 keyStartedPos;

        Action<ShotKind, DoorBeat, Vector3> onBeat;
        Action<ShotKind, Vector3> onStart, onEnd;
        Action<FrontRoomsMapWorld.Door, GridCoord, Vector3> onUnlocked;
        Action<FrontRoomsMapWorld.Door, Vector3> onHandle, onPulled;
        Action<Vector3> onMoved;
        Action<FrontRoomsMapWorld.Door, FrontRoomsMapWorld.DoorOpenSource, bool> onBegan;
        Action<GridCoord, Vector3> onKeyStarted;
        Action<GridCoord> onKeyTaken;

        public void Hook()
        {
            onBeat = (k, beat, p) =>
            {
                if (k == ShotKind.Unlock) beats.Add((beat, clock));
                else if (k == ShotKind.KeyPickup && beat == DoorBeat.Pocket) pocketAt.Add(clock);
            };
            onStart = (k, p) => { if (k != ShotKind.Unlock) return; started++; startedAt = clock; };
            onEnd = (k, p) => { if (k != ShotKind.Unlock) return; ended++; endedAt = clock; };
            onUnlocked = (d, z, p) => unlocked.Add((clock, z, p));
            onHandle = (d, p) => handles.Add(clock);
            onPulled = (d, p) => pulls.Add(clock);
            onMoved = p => moves.Add(clock);
            onBegan = (d, src, latch) => began.Add((clock, src, latch));
            onKeyStarted = (z, p) => { keyOrder.Add("S " + z); keyStartedAt.Add(clock); keyStartedPos = p; };
            onKeyTaken = z => { keyOrder.Add("T " + z); keyTakenAt.Add(clock); };
            FrontRoomsDoorShot.Beat += onBeat;
            FrontRoomsCameraRig.ShotStarted += onStart;
            FrontRoomsCameraRig.ShotEnded += onEnd;
            world.DoorUnlocked += onUnlocked;
            world.DoorHandleTurned += onHandle;
            world.DoorPulled += onPulled;
            world.DoorMoved += onMoved;
            world.DoorOpenBegan += onBegan;
            world.KeyPickupStarted += onKeyStarted;
            world.KeyTaken += onKeyTaken;
        }

        public void Unhook()
        {
            FrontRoomsDoorShot.Beat -= onBeat;
            FrontRoomsCameraRig.ShotStarted -= onStart;
            FrontRoomsCameraRig.ShotEnded -= onEnd;
            if (world == null) return;
            world.DoorUnlocked -= onUnlocked;
            world.DoorHandleTurned -= onHandle;
            world.DoorPulled -= onPulled;
            world.DoorMoved -= onMoved;
            world.DoorOpenBegan -= onBegan;
            world.KeyPickupStarted -= onKeyStarted;
            world.KeyTaken -= onKeyTaken;
        }

        public void Clear()
        {
            clock = 0f;
            beats.Clear();
            unlocked.Clear();
            handles.Clear();
            moves.Clear();
            pulls.Clear();
            began.Clear();
            started = ended = 0;
            startedAt = endedAt = -1f;
            keyOrder.Clear();
            keyStartedAt.Clear();
            keyTakenAt.Clear();
            pocketAt.Clear();
        }

        public float BeatAt(DoorBeat beat)
        {
            var i = beats.FindIndex(x => x.beat == beat);
            return i >= 0 ? beats[i].t : -1f;
        }

        public bool Has(DoorBeat beat) => beats.Exists(x => x.beat == beat);
    }

    /// <summary>A map with keys on, a shut door both of whose sides have room to stand 0.9 m from the lock, a body, a rig and the shot as the key-turn owner.</summary>
    static H Harness(List<GameObject> roots, List<FrontRoomsLevelProfile> profiles)
    {
        var profile = UnityEngine.Object.Instantiate(FrontRoomsLevelProfiles.Resolve());
        profile.hideFlags = HideFlags.DontSave;
        profile.doorsNeedKeys = true;
        profiles.Add(profile);
        var root = new GameObject("DOOR SHOT TEST") { hideFlags = HideFlags.DontSave };
        roots.Add(root);
        // Far from the open scene and the other tests' maps, on whole world periods so surfaces stay on the lattice.
        root.transform.position = new Vector3(-33f * ModuleUnits.WorldPeriod, 0f, -27f * ModuleUnits.WorldPeriod);
        var world = root.AddComponent<FrontRoomsMapWorld>();
        world.Profile = profile;
        world.BuildForCapture();
        var mid = world.CellOf(world.SpawnWorldPosition);
        FrontRoomsMapWorld.Door door = null;
        for (var r = 0; r <= 12 && door == null; r++)
        for (var y = mid.y - r; y <= mid.y + r && door == null; y++)
        for (var x = mid.x - r; x <= mid.x + r && door == null; x++)
        {
            if (Mathf.Max(Mathf.Abs(x - mid.x), Mathf.Abs(y - mid.y)) != r) continue;
            var cell = new GridCoord(x, y);
            foreach (var step in new[] { new GridCoord(1, 0), new GridCoord(0, 1) })
            {
                var candidate = world.DoorBetween(cell, cell + step);
                if (candidate == null || candidate.open || candidate.broken || !world.IsBuilt(cell) || !world.IsBuilt(cell + step)) continue;
                if (!RoomToStand(world, candidate, world.CellCenter(cell)) || !RoomToStand(world, candidate, world.CellCenter(cell + step))) continue;
                door = candidate;
                break;
            }
        }
        if (door == null) { Check(false, "harness: no shut door near the spawn with room to stand on both sides"); return null; }
        var h = new H { world = world, profile = profile, door = door, a = door.a, b = door.b, body = world.Player };
        h.camera = new GameObject("door shot camera").AddComponent<Camera>();
        h.camera.fieldOfView = 76f;
        h.camera.transform.SetParent(root.transform, false);
        h.rig = FrontRoomsCameraRig.Attach(h.body, h.camera, ModuleUnits.PlayerEye);
        h.shot = new FrontRoomsDoorShot(h.rig, world, h.camera, h.body);
        world.KeyTurnOwner = h.shot;
        world.GiveKeyForTools(world.ZoneOf(h.a).id);
        world.GiveKeyForTools(world.ZoneOf(h.b).id);
        h.Hook();
        Check(true, "harness: door " + h.a + "-" + h.b + " (swing " + door.swing + "), keys for both zones, the shot is the key-turn owner");
        return h;
    }

    // A body 0.9 m from the lock on this side stands clear, and the camera's way to pose P is clear.
    static bool RoomToStand(FrontRoomsMapWorld world, FrontRoomsMapWorld.Door door, Vector3 probe)
    {
        var n = FrontRoomsMapWorld.LockNormal(door, probe);
        var key = world.LockPose(door, probe).position;
        var feet = new Vector3(key.x, door.position.y, key.z) + n * StandOut;
        if (Physics.CheckCapsule(feet + Vector3.up * (ModuleUnits.PlayerRadius + .05f), feet + Vector3.up * (ModuleUnits.PlayerHeight - ModuleUnits.PlayerRadius), ModuleUnits.PlayerRadius, ~0, QueryTriggerInteraction.Ignore))
            return false;
        var eye = feet + Vector3.up * ModuleUnits.PlayerEye;
        var p = FrontRoomsDoorShot.UnlockPoseP(new Pose(key, Quaternion.identity), n, FrontRoomsMapWorld.TowardHinge(door), eye.y);
        var to = p.position - eye;
        return !Physics.SphereCast(eye, FrontRoomsShotTimings.WorldClampRadius, to.normalized, out _, to.magnitude, ~0, QueryTriggerInteraction.Ignore);
    }

    // The door shut, locked and still; the body on one side, 0.9 m from the keyhole along the face normal, facing it.
    // leafSide: the side the leaf last went to (the single-acting pull side); A12 makes it a push all the same.
    static void Stand(H h, bool leafSide)
    {
        if (h.shot.Live) h.shot.Abort();
        h.door = h.world.DoorBetween(h.a, h.b);
        h.world.ResetDoorForTools(h.door);
        h.world.SetDoorRigForTools(h.door, null);
        h.world.DoorsPaused = false;
        h.rig.ResetLayers();
        var swingIsA = FrontRoomsMapWorld.OnSwingSide(h.door, h.world.CellCenter(h.a));
        var probe = h.world.CellCenter(leafSide == swingIsA ? h.a : h.b);
        var n = FrontRoomsMapWorld.LockNormal(h.door, probe);
        var key = h.world.LockPose(h.door, probe).position;
        var feet = new Vector3(key.x, h.door.position.y, key.z) + n * StandOut;
        h.body.SetPositionAndRotation(feet, Quaternion.LookRotation(-n, Vector3.up));
        h.rig.SetBase(Mathf.Atan2(ModuleUnits.PlayerEye - ModuleUnits.DoorHandleHeight, StandOut) * Mathf.Rad2Deg);
        Physics.SyncTransforms();
        h.rig.Tick(Dt);
    }

    /// <summary>E on the door from one side (Use must answer KeyTurn), then the shot begins as the game begins it.</summary>
    static bool Start(H h, bool leafSide, bool inPlace = false, FakeRig fake = null)
    {
        Stand(h, leafSide);
        if (fake != null)
        {
            // The rig's keyways: the spec's, read before the rig is set (LockPose then comes from it). S is the door root's +X face (A12: geometric).
            var aIsS = FrontRoomsMapWorld.FaceOf(h.door, h.world.CellCenter(h.a)) == DoorFace.S;
            fake.s = h.world.LockPose(h.door, h.world.CellCenter(aIsS ? h.a : h.b));
            fake.p = h.world.LockPose(h.door, h.world.CellCenter(aIsS ? h.b : h.a));
            fake.clock = () => h.clock;
            h.world.SetDoorRigForTools(h.door, fake);
        }
        var feet = h.body.position;
        var result = h.world.Use(h.door.leaf);
        if (result != FrontRoomsMapWorld.UseResult.KeyTurn)
        {
            Check(false, "start (" + (leafSide ? "leaf side" : "push") + "): Use answered " + result + ", not KeyTurn");
            return false;
        }
        h.Clear();
        h.feet = feet;
        h.face = FrontRoomsMapWorld.FaceOf(h.door, feet);
        h.lockPose = h.world.LockPose(h.door, feet);
        h.normal = FrontRoomsMapWorld.LockNormal(h.door, feet);
        // A12: Use's key turn turned the shut leaf away from the key's side, so this is a push from either face (the game reads it the same way).
        h.pull = FrontRoomsMapWorld.OnSwingSide(h.door, feet);
        h.eye0 = h.rig.BaseEye;
        h.poseP = FrontRoomsDoorShot.UnlockPoseP(h.lockPose, h.normal, FrontRoomsMapWorld.TowardHinge(h.door), h.eye0.position.y);
        h.shot.BeginUnlock(h.door, h.face, h.lockPose, h.normal, h.pull, inPlace);
        return true;
    }

    // One frame: the driver (with the Relay's inputs), the doors, the rig, the presented key (on the rendered camera).
    static void Frame(H h, bool chase = false, bool sees = false, float distance = -1f)
    {
        h.clock += Dt;
        h.shot.Tick(Dt, chase, sees, distance);
        h.world.TickDoorsForTools(Dt);
        h.rig.Tick(Dt);
        h.shot.PlaceKey();
        Physics.SyncTransforms();
    }

    // Frames until the clock reaches a time (to the nearest frame).
    static void RunTo(H h, float time, Action each = null)
    {
        while (h.clock < time - Dt * .5f)
        {
            Frame(h);
            each?.Invoke();
        }
    }

    static float CamToEye(H h) => (h.camera.transform.position - h.rig.BaseEye.position).magnitude;

    // Leave the case behind: the shot dropped, the door free for the next one.
    static void Done(H h)
    {
        if (h.shot.Live) h.shot.Abort();
        h.rig.ResetLayers();
        h.world.DoorsPaused = false;
        for (var i = 0; i < 6; i++) Frame(h);
    }

    // ------------------------------------------------------------------ pure parts (once)

    static void Pure()
    {
        // 1. Pose P: 0.45 m out (flat), 0.25 m down from the eye, 12° toward the hinge, looking at the keyhole; both faces, both handings.
        var l = new Vector3(0f, 1f, 0f);
        var ok = true;
        var detail = "";
        foreach (var n in new[] { Vector3.right, Vector3.left, Vector3.forward })
        foreach (var hs in new[] { 1f, -1f })
        {
            var hinge = Vector3.Cross(Vector3.up, n) * hs;
            var p = FrontRoomsDoorShot.UnlockPoseP(new Pose(l, Quaternion.LookRotation(-n)), n, hinge, ModuleUnits.PlayerEye);
            var flat = p.position - l;
            flat.y = 0f;
            var distance = (p.position - l).magnitude;
            var to = l - p.position;
            var pitch = Mathf.Atan2(-to.y, new Vector2(to.x, to.z).magnitude) * Mathf.Rad2Deg;
            var yaw = Vector3.Angle(flat, n);
            var looks = Vector3.Angle(p.rotation * Vector3.forward, to);
            var good = Near(flat.magnitude, U.DipDistanceFromLock, 1e-4f) && Near(p.position.y, ModuleUnits.PlayerEye - U.DipEyeDrop, 1e-4f)
                && Near(distance, .583f, .005f) && Near(pitch, 39.4f, .5f) && Mathf.Abs(pitch - U.DipPitchDeg) <= 2f
                && Near(yaw, U.PoseYawTowardHingeDeg, .5f) && Vector3.Dot(flat, hinge) > 0f && looks < .01f;
            if (!good) { ok = false; detail = "n " + n + " hinge " + hinge + ": flat " + flat.magnitude.ToString("0.000") + ", dist " + distance.ToString("0.000") + ", pitch " + pitch.ToString("0.0") + ", yaw " + yaw.ToString("0.0"); }
            else if (detail.Length == 0) detail = "0.45 m flat, " + distance.ToString("0.000") + " m, " + pitch.ToString("0.0") + "° down, " + yaw.ToString("0.0") + "° toward the hinge";
        }
        Check(ok, "1 pose P: " + detail + " (both faces, both handings, looking at the keyhole)");
        // 2. The travel.
        float t1 = FrontRoomsDoorShot.UnlockTravel(.3f), t2 = FrontRoomsDoorShot.UnlockTravel(1f), t3 = FrontRoomsDoorShot.UnlockTravel(2.5f);
        Check(Near(t1, .35f, 1e-4f) && Near(t2, .42f, 1e-4f) && Near(t3, .55f, 1e-4f),
            "2 travel: 0.3 / 1.0 / 2.5 m → " + t1.ToString("0.000") + " / " + t2.ToString("0.000") + " / " + t3.ToString("0.000") + " s (0.35 / 0.42 / 0.55)");
        // The Relay rule, in one place.
        Check(!FrontRoomsDoorShot.Threatened(false, false, true, 8f) && FrontRoomsDoorShot.Threatened(false, false, true, 5f) && FrontRoomsDoorShot.Threatened(false, true, false, 20f)
              && FrontRoomsDoorShot.Threatened(true, false, true, 12f) && !FrontRoomsDoorShot.Threatened(false, false, true, -1f) && !FrontRoomsDoorShot.Threatened(true, false, false, 3f),
            "threat rule: before the commit a chase or sight within 6 m; after it a chase or any sight; never without either");
        // The plug's turn: the first 10° slowly, 90° at the end of the turn, back to 0 before the key withdraws.
        var resistEnd = U.TurnStart + U.TurnResistShare * (U.TurnEnd - U.TurnStart);
        Check(Near(FrontRoomsDoorShot.PlugDegrees(resistEnd), U.TurnResistDeg, 1e-3f) && FrontRoomsDoorShot.PlugDegrees(U.TurnEnd - 1e-4f) > 89.9f
              && FrontRoomsDoorShot.PlugDegrees(U.KeyOutEnd) == 0f && FrontRoomsDoorShot.PlugDegrees(U.TurnStart) == 0f,
            "plug: 10° at " + resistEnd.ToString("0.000") + " s, 90° at the end of the turn, 0 by the key's withdraw");
    }

    // 18. The step-in's stand point (pure, on the harness's door).
    static void StepIn(H h)
    {
        var door = h.door;
        var swingIsA = FrontRoomsMapWorld.OnSwingSide(door, h.world.CellCenter(h.a));
        var pushProbe = h.world.CellCenter(swingIsA ? h.b : h.a);
        var n = FrontRoomsMapWorld.LockNormal(door, pushProbe);
        var key = h.world.LockPose(door, pushProbe).position;
        var target = new Vector3(key.x, door.position.y, key.z) + n * U.StandDistance;
        var across = Vector3.Cross(Vector3.up, n);
        var guarded = !FrontRoomsDoorShot.UnlockStepIn(door, target + n * .4f, key, n, false, true, out _);
        var far = !FrontRoomsDoorShot.UnlockStepIn(door, target + n * 1.2f, key, n, false, false, out _);
        var behind = !FrontRoomsDoorShot.UnlockStepIn(door, target - n * .3f, key, n, false, false, out _);
        var stepped = FrontRoomsDoorShot.UnlockStepIn(door, target + n * .5f + across * .1f, key, n, false, false, out var stand);
        Check(guarded && far && behind && stepped && (stand - target).magnitude < 1e-4f,
            "18 step-in: none while a door pull's step runs, none for a target 1.2 m away or behind the body; 0.5 m forward and 0.1 m across steps to 0.75 m from the lock");
        // The pull side: the stand point is cleared of the leaf's sweep, and the step reaches it.
        var pullProbe = h.world.CellCenter(swingIsA ? h.a : h.b);
        var n2 = FrontRoomsMapWorld.LockNormal(door, pullProbe);
        var key2 = h.world.LockPose(door, pullProbe).position;
        var raw = new Vector3(key2.x, door.position.y, key2.z) + n2 * U.StandDistance;
        var cleared = FrontRoomsMapWorld.ClearOfSwing(door, raw, LeafClearance);
        var pulled = FrontRoomsDoorShot.UnlockStepIn(door, cleared + n2 * .3f, key2, n2, true, false, out var stand2);
        Check(pulled && (stand2 - cleared).magnitude < 1e-4f && (FrontRoomsMapWorld.ClearOfSwing(door, stand2, LeafClearance) - stand2).magnitude < 1e-5f && FrontRoomsMapWorld.OnSwingSide(door, stand2),
            "18 step-in, pull side: the stand point " + (cleared - raw).magnitude.ToString("0.00") + " m from the plain one is out of the sweep (ClearOfSwing returns it unchanged)");
        // 1 on the real door: P on both faces is toward the hinge.
        var towardHinge = FrontRoomsMapWorld.TowardHinge(door);
        var pPush = FrontRoomsDoorShot.UnlockPoseP(h.world.LockPose(door, pushProbe), n, towardHinge, ModuleUnits.PlayerEye);
        var pPull = FrontRoomsDoorShot.UnlockPoseP(h.world.LockPose(door, pullProbe), n2, towardHinge, ModuleUnits.PlayerEye);
        Check(Vector3.Dot(pPush.position - key, towardHinge) > .05f && Vector3.Dot(pPull.position - key2, towardHinge) > .05f && Vector3.Dot(pPush.position - key, n) > .4f && Vector3.Dot(pPull.position - key2, n2) > .4f,
            "1 pose P on the map's door: out of each face and toward its hinge");
    }

    // ------------------------------------------------------------------ the battery (each Camera motion)

    static void Battery(H h)
    {
        var m = FrontRoomsSettings.CameraMotionScale;
        FullPush(h, m, false);
        FullPush(h, m, true);
        PullBranch(h, m);
        WithRig(h, false);
        WithRig(h, true);
        PlayerCancelEarly(h);
        PlayerCancelFov(h, m);
        RelayBefore(h);
        RelayAfter(h, m, false);
        RelayAfter(h, m, true);
        PlayerAfter(h);
        BeingBroken(h);
        Pause(h);
        InPlace(h, m);
        Rebuild(h);
        AbortCases(h);
        Orphans(h);
    }

    // 3, 4, 5, 6 (push), 7, and 19 without a rig: one uncancelled unlock, from either side (A12: both are pushes).
    static void FullPush(H h, float m, bool leafSide)
    {
        var sideBefore = h.door != null ? h.world.DoorBetween(h.a, h.b).swing : 0f;
        if (!Start(h, leafSide)) return;
        // A12: from the side the leaf last went to, the key turn turns the shut leaf over; from the other side it stays.
        Check(!h.pull && !FrontRoomsMapWorld.OnSwingSide(h.door, h.feet) && h.face == FrontRoomsMapWorld.FaceOf(h.door, h.feet) && (h.door.swing == -sideBefore) == leafSide,
            "A12 " + (leafSide ? "from the leaf's side" : "from the other side") + ": at the key turn the shut leaf swings away from the key's face (" + h.face + "), so the shot is a push");
        var startedOk = h.started == 1 && h.startedAt == 0f;
        var home = h.door.leaf.transform.localPosition;
        var expected06 = Vector3.Lerp(h.eye0.position, h.poseP.position, m);
        float camErr06 = -1f, weight06 = -1f, hud06 = -1f, fov040 = 0f, fov042 = 0f, fov045 = 0f, fov155 = -1f, hud161 = -1f, weight135 = -1f, weight115 = -1f, weight155 = -1f;
        float maxBase = 0f, maxFovDev = 0f, minGap = float.MaxValue, maxShift = 0f, maxCamOff = 0f, angle102 = -1f, angle156 = -1f, angle167 = -1f;
        var unlockedAt085 = -1;
        var lockBad = "";
        var holdOk = false;
        var holdChecked = false;
        RunTo(h, 1.75f, () =>
        {
            var c = h.clock;
            maxBase = Mathf.Max(maxBase, (h.rig.BaseEye.position - h.eye0.position).magnitude);
            var fov = h.camera.fieldOfView;
            maxFovDev = Mathf.Max(maxFovDev, Mathf.Abs(fov - 76f));
            maxCamOff = Mathf.Max(maxCamOff, CamToEye(h));
            if (c <= U.ReturnEnd + 1e-3f) minGap = Mathf.Min(minGap, (h.door.leaf.ClosestPoint(h.camera.transform.position) - h.camera.transform.position).magnitude);
            if (Near(c, .40f, Dt * .5f)) fov040 = fov;
            if (Near(c, .40f + Dt, Dt * .5f)) fov042 = fov;
            if (Near(c, .45f, Dt * .5f)) fov045 = fov;
            if (Near(c, .5f, Dt * .5f))
            {
                // 6: the hold: no prompt, Use and TryOpenDoor leave it.
                holdChecked = true;
                holdOk = h.world.IsHeldForKey(h.door) && h.world.Describe(h.door.leaf, out _) == null
                    && h.world.Use(h.door.leaf) == FrontRoomsMapWorld.UseResult.None && !h.world.TryOpenDoor(h.a, h.b) && !h.door.open;
            }
            if (Near(c, .6f, Dt * .5f)) { camErr06 = (h.camera.transform.position - expected06).magnitude; weight06 = FrontRoomsDoorShot.Weight; hud06 = h.rig.HudFade; }
            if (Near(c, .85f, Dt * .5f)) unlockedAt085 = h.unlocked.Count;
            if (c > .84f && c < .99f) maxShift = Mathf.Max(maxShift, (h.door.leaf.transform.localPosition - home).magnitude);
            if (Near(c, 1.15f, Dt * .5f)) weight115 = FrontRoomsDoorShot.Weight;
            if (Near(c, 1.35f, Dt * .5f)) weight135 = FrontRoomsDoorShot.Weight;
            if (Near(c, 1.0333f, Dt * .5f)) angle102 = h.door.angle;
            if (Near(c, U.ReturnEnd, Dt * .5f)) { fov155 = fov; }
            if (Near(c, U.ReturnEnd + Dt, Dt * .5f)) weight155 = FrontRoomsDoorShot.Weight;
            if (Near(c, 1.5667f, Dt * .5f)) angle156 = h.door.angle;
            if (Near(c, 1.6167f, Dt * .5f)) hud161 = h.rig.HudFade;
            if (Near(c, 1.6667f, Dt * .5f)) angle167 = h.door.angle;
            // 7: the locks, away from the frames on a boundary.
            var expectMove = c < U.ReturnEnd - Dt * .5f && (c < U.Commit || (m > 0f && c < U.MoveLockEnd));
            var nearEdge = Near(c, U.Commit, Dt * .6f) || Near(c, U.MoveLockEnd, Dt * .6f) || Near(c, U.ReturnEnd, Dt * .6f);
            if (!nearEdge && h.shot.MoveLocked != expectMove && lockBad.Length == 0) lockBad = "MoveLocked " + h.shot.MoveLocked + " at " + c.ToString("0.000");
            var look = h.shot.LookScale;
            if (m <= 0f) { if (look != 1f && lockBad.Length == 0) lockBad = "LookScale " + look + " at " + c.ToString("0.000") + " (Off)"; }
            else if (c < U.LookLockEnd - Dt * .6f && look != 0f && lockBad.Length == 0) lockBad = "LookScale " + look + " at " + c.ToString("0.000");
            else if (Near(c, 1.40f, Dt * .5f) && !Near(look, .5f, .05f) && lockBad.Length == 0) lockBad = "LookScale " + look + " at 1.40";
            else if (c >= 1.50f - 1e-3f && !Near(look, 1f, 1e-4f) && lockBad.Length == 0) lockBad = "LookScale " + look + " at " + c.ToString("0.000");
        });
        var zone = h.world.ZoneOf(h.world.CellOf(h.feet)).id;
        var awayOk = h.door.open && h.door.angle == ModuleUnits.DoorSwingDegrees && FrontRoomsMapWorld.FaceOf(h.door, h.door.leaf.transform.position) != h.face;
        Check(awayOk, "A12 " + (leafSide ? "from the leaf's side" : "from the other side") + ": the leaf rests at 95° on the far face from the key's (" + h.face + ")");
        // 4: the camera.
        Check(maxBase < 1e-4f, "4 push: BaseEye never moves (" + (maxBase * 1000f).ToString("0.000") + " mm)");
        if (m > 0f) Check(camErr06 >= 0f && camErr06 < .02f, "4 push: at 0.6 s the camera is " + (m * 100f).ToString("0") + "% of the way to pose P (off by " + (camErr06 * 100f).ToString("0.0") + " cm)");
        else Check(maxCamOff < 1e-4f && maxFovDev < 1e-3f, "4 push, Off: the camera stays on BaseEye (" + (maxCamOff * 1000f).ToString("0.000") + " mm) with FOV 76 the whole shot");
        // 3: the FOV.
        var fovTarget = 76f + U.FovDeltaDeg * m;
        Check(Near(fov045, fovTarget, .1f) && (m <= 0f || Mathf.Abs(fov042 - fov040) > .01f) && Near(fov155, 76f, .1f) && maxFovDev <= FrontRoomsShotTimings.MaxFovDeltaDeg + 1e-3f,
            "3 FOV: " + fov045.ToString("0.00") + " at 0.45 s (" + fovTarget.ToString("0.0") + "), still moving at 0.40, " + fov155.ToString("0.00") + " at 1.55, never past ±15 (" + maxFovDev.ToString("0.0") + ")");
        // 5: the beats and events.
        var insert = h.BeatAt(DoorBeat.KeyInsert);
        var turn = h.BeatAt(DoorBeat.KeyTurn);
        Check(startedOk && Near(insert, U.InsertStart, Dt * 1.01f) && Near(turn, U.TurnStart, Dt * 1.01f),
            "5 push: ShotStarted at 0, KeyInsert at " + insert.ToString("0.000") + ", KeyTurn at " + turn.ToString("0.000"));
        var keyhole = h.unlocked.Count > 0 ? h.unlocked[0].at : Vector3.zero;
        var across = Vector3.Dot(keyhole - h.door.position, h.normal);
        Check(unlockedAt085 == 0 && h.unlocked.Count == 1 && Near(h.unlocked[0].t, U.Commit, Dt * 1.01f) && h.unlocked[0].zone == zone
              && (keyhole - h.lockPose.position).magnitude < 1e-4f && Near(across, .0315f, 1e-3f) && Near(keyhole.y - h.door.position.y, ModuleUnits.DoorHandleHeight, 1e-3f),
            "5 push: DoorUnlocked 0 at 0.85, once at " + (h.unlocked.Count > 0 ? h.unlocked[0].t.ToString("0.000") : "-") + " with zone " + zone + " at the keyhole, " + (across * 1000f).ToString("0.0") + " mm out on the opener's face");
        var bolt = h.BeatAt(DoorBeat.BoltRetract);
        var knob = h.BeatAt(DoorBeat.KnobTurn);
        var knobBack = h.BeatAt(DoorBeat.KnobReturn);
        var ret = h.BeatAt(DoorBeat.Return);
        Check(h.unlocked.Count > 0 && bolt == h.unlocked[0].t && Near(knob, U.KnobTurnStart, Dt * 1.01f) && Near(knobBack, U.KnobReturnStart, Dt * 1.01f) && Near(ret, U.ReturnStart, Dt * 1.01f),
            "5 push: BoltRetract with DoorUnlocked, KnobTurn " + knob.ToString("0.000") + ", Return " + ret.ToString("0.000") + ", KnobReturn " + knobBack.ToString("0.000"));
        Check(Near(weight06, m, .01f) && Near(weight115, m, .01f) && (m <= 0f || weight135 < weight115 - .2f) && weight155 == 0f,
            "5 push: Weight " + weight06.ToString("0.00") + " at 0.6 (" + m.ToString("0.0") + "), falling from 1.15 (" + weight115.ToString("0.00") + " → " + weight135.ToString("0.00") + " at 1.35), 0 after 1.55");
        Check(h.ended == 1 && h.endedAt >= U.ReturnEnd - 1e-3f && h.endedAt <= U.ReturnEnd + Dt * 2.01f && hud06 > .99f && hud161 >= 0f && hud161 < 1e-3f,
            "5 push: ShotEnded once at " + h.endedAt.ToString("0.000") + " (1.55 + ≤ 2 frames); the HUD faded at 0.6 (" + hud06.ToString("0.00") + ") and back by 1.61 (" + hud161.ToString("0.000") + ")");
        // 6: the hold and the push-side open.
        var open = h.began.Count > 0 ? h.began[0] : default;
        Check(holdChecked && holdOk, "6 push: held at 0.5 s: no prompt, Use answers None, TryOpenDoor refused, shut");
        Check(h.handles.Count == 0 && h.began.Count == 1 && open.source == FrontRoomsMapWorld.DoorOpenSource.Unlock && open.latchDrawn && Near(open.t, U.KnobTurnEnd, Dt * 1.01f)
              && h.moves.Count >= 1 && Near(h.moves[0], U.KnobTurnEnd, Dt * 1.01f) && Near(h.BeatAt(DoorBeat.Open), U.KnobTurnEnd, Dt * 1.01f),
            "6 push: no DoorHandleTurned; DoorOpenBegan(Unlock, latch drawn), DoorMoved and Beat(Open) at " + open.t.ToString("0.000"));
        Check(angle102 > 0f && angle156 >= ModuleUnits.DoorSwingDegrees - .5f && angle167 == ModuleUnits.DoorSwingDegrees && minGap >= .1f,
            "6 push: the leaf moving by 1.02 (" + angle102.ToString("0.0") + "°), at 95° by 1.56 (" + angle156.ToString("0.0") + "°), settled by 1.67 (" + angle167.ToString("0.0") + "°); the leaf ≥ 0.1 m from the camera (" + minGap.ToString("0.00") + ")");
        // 7: the locks.
        Check(lockBad.Length == 0, "7 locks: move held to the commit" + (m > 0f ? " and to 1.45 s; look 0 to 1.30, 0.5 at 1.40, 1 from 1.50" : " only; look free") + (lockBad.Length > 0 ? " — " + lockBad : ""));
        // 19 without a rig: the cube's leaf shifts at the commit (JoltLeaf), no exception.
        Check(maxShift > U.LeafShiftMetres * .5f, "19 no rig: the commit shifts the cube leaf " + (maxShift * 1000f).ToString("0.00") + " mm (JoltLeaf)");
        Done(h);
    }

    // 5 and 6 on the pull branch, kept for a leaf out of the frame (A12): a locked door's leaf is only out of the frame when
    // the key rule came on mid-run (ApplyLive on a profile edit in Play, or DoorsNeedKeysForTools as the autopilot's door
    // scenario sets it) after the door was opened with keys off. Opened toward the body, it rests on it; shut, and E with the
    // key from the leaf's side before it latches: Use's SwingAwayFrom leaves a leaf out of the frame on its side, so the game
    // reads a pull. No knob beats in the shot; the leaf goes on shutting and latches under the hold, and only once the camera
    // is home (1.55 s) does the map open it, a normal open (DoorHandleTurned), away from the key's face.
    static void PullBranch(H h, float m)
    {
        Stand(h, true);
        var feet = h.body.position;
        var near = h.world.CellOf(feet);
        var far = near == h.a ? h.b : h.a;
        try
        {
            h.world.DoorsNeedKeysForTools = false;
            // Opened from the far cell, the leaf swings toward the body and stops on it; then the body shuts it.
            var opened = h.world.TryOpenDoor(far, near);
            for (var k = 0; k < 60; k++) h.world.TickDoorsForTools(Dt);
            var restedAt = h.door.angle;
            var shut = h.world.Use(h.door.leaf);
            for (var k = 0; k < 3; k++) h.world.TickDoorsForTools(Dt);
            var shuttingAt = h.door.angle;
            // The key rule comes on: this door was never unlocked, so it needs the key now.
            h.world.DoorsNeedKeysForTools = true;
            var result = h.world.Use(h.door.leaf);
            if (!opened || shut != FrontRoomsMapWorld.UseResult.Shut || result != FrontRoomsMapWorld.UseResult.KeyTurn || FrontRoomsMapWorld.IsLatched(h.door))
            {
                Check(false, "pull branch: setup (opened " + opened + ", rested at " + restedAt.ToString("0.0") + "°, shut " + shut + ", E with the key at " + shuttingAt.ToString("0.0") + "° answered " + result + ")");
                return;
            }
            // As the game begins it (FrontRooms3DGame.BeginUnlock).
            h.Clear();
            h.feet = feet;
            h.face = FrontRoomsMapWorld.FaceOf(h.door, feet);
            h.lockPose = h.world.LockPose(h.door, feet);
            h.normal = FrontRoomsMapWorld.LockNormal(h.door, feet);
            h.pull = FrontRoomsMapWorld.OnSwingSide(h.door, feet);
            h.eye0 = h.rig.BaseEye;
            h.poseP = FrontRoomsDoorShot.UnlockPoseP(h.lockPose, h.normal, FrontRoomsMapWorld.TowardHinge(h.door), h.eye0.position.y);
            h.shot.BeginUnlock(h.door, h.face, h.lockPose, h.normal, h.pull, false);
            var latchedAt = -1f;
            var opensEarly = false;
            var minGap = float.MaxValue;
            var maxCamOff = 0f;
            var lastAngle = h.door.angle;
            RunTo(h, 3f, () =>
            {
                if (latchedAt < 0f && FrontRoomsMapWorld.IsLatched(h.door)) latchedAt = h.clock;
                if (h.clock < U.ReturnEnd - Dt * .5f && (h.door.angle > lastAngle + 1e-4f || h.handles.Count > 0 || h.began.Count > 0)) opensEarly = true;
                lastAngle = h.door.angle;
                if (h.clock <= U.ReturnEnd + 1e-3f) minGap = Mathf.Min(minGap, (h.door.leaf.ClosestPoint(h.camera.transform.position) - h.camera.transform.position).magnitude);
                maxCamOff = Mathf.Max(maxCamOff, CamToEye(h));
            });
            var open = h.began.Count > 0 ? h.began[0] : default;
            Check(h.pull && !h.Has(DoorBeat.KnobTurn) && !h.Has(DoorBeat.KnobReturn)
                  && h.unlocked.Count == 1 && Near(h.unlocked[0].t, U.Commit, Dt * 1.01f),
                "5 pull branch (keys on mid-run, E at " + shuttingAt.ToString("0.0") + "° out of the frame on the key's side): a pull; DoorUnlocked at "
                + (h.unlocked.Count > 0 ? h.unlocked[0].t.ToString("0.000") : "-") + ", no knob beats in the shot");
            Check(!opensEarly && latchedAt > 0f && latchedAt < U.Commit && minGap >= .1f && h.handles.Count == 1 && Near(h.handles[0], U.ReturnEnd, Dt * 1.01f)
                  && h.began.Count == 1 && open.source == FrontRoomsMapWorld.DoorOpenSource.Unlock && !open.latchDrawn && h.pulls.Count == 0
                  && Near(h.BeatAt(DoorBeat.Open), U.ReturnEnd, Dt * 1.01f) && h.ended == 1 && h.endedAt <= U.ReturnEnd + Dt * 2.01f,
                "6 pull branch: the leaf goes on shutting under the hold and latches at " + latchedAt.ToString("0.000") + " s, nothing opens it before 1.55; then DoorHandleTurned ("
                + (h.handles.Count > 0 ? h.handles[0].ToString("0.000") : "-") + "), DoorOpenBegan(Unlock), no DoorPulled; the leaf ≥ 0.1 m from the camera (" + minGap.ToString("0.00") + ")");
            Check(h.door.open && h.door.angle == ModuleUnits.DoorSwingDegrees && FrontRoomsMapWorld.FaceOf(h.door, h.door.leaf.transform.position) != h.face,
                "A12 pull branch: the open at 1.55 turns the latched leaf away from the key's face (" + h.face + ") and it rests at " + h.door.angle.ToString("0.0") + "° on the far face");
            if (m > 0f) Check(maxCamOff > .1f, "4 pull branch: the camera dips (" + maxCamOff.ToString("0.00") + " m from the eye at most)");
        }
        finally
        {
            h.world.DoorsNeedKeysForTools = true;
            Done(h);
        }
    }

    // 19: the lock's parts through a fake rig: the plug, the deadbolt, the leaf's jolt, the knob and latch (push side), every call on the opener's face.
    static void WithRig(H h, bool leafSide)
    {
        var fake = new FakeRig();
        if (!Start(h, leafSide, false, fake)) return;
        var home = h.door.leaf.transform.localPosition;
        var cubeStill = true;
        RunTo(h, 1.6f, () => { if ((h.door.leaf.transform.localPosition - home).sqrMagnitude > 1e-12f) cubeStill = false; });
        var side = leafSide ? "push, leaf side" : "push";
        var plug = fake.calls.Where(c => c.part == "plug").ToList();
        var resistEnd = U.TurnStart + U.TurnResistShare * (U.TurnEnd - U.TurnStart);
        var before = plug.Where(c => c.t <= resistEnd + 1e-4f).Select(c => c.value).DefaultIfEmpty(0f).Max();
        var firstAt = plug.Count > 0 ? plug[0].t : -1f;
        var full = plug.Where(c => c.value >= U.TurnDeg - .01f).Select(c => c.t).DefaultIfEmpty(-1f).First();
        var back = plug.Count > 0 && plug[plug.Count - 1].value == 0f ? plug[plug.Count - 1].t : -1f;
        Check(firstAt >= U.TurnStart - 1e-4f && before <= U.TurnResistDeg + 1e-3f && before > U.TurnResistDeg * .5f && full > 0f && full <= U.TurnEnd + Dt * 1.01f && back > 0f && back <= U.KeyOutEnd,
            "19 rig " + side + ": SetPlug from " + firstAt.ToString("0.000") + ", " + before.ToString("0.0") + "° by " + resistEnd.ToString("0.000") + ", 90° at " + full.ToString("0.000") + ", back to 0 at " + back.ToString("0.000"));
        var bolt = fake.calls.Where(c => c.part == "bolt").ToList();
        var boltFull = bolt.Where(c => c.value >= 1f - 1e-4f).Select(c => c.t).DefaultIfEmpty(-1f).First();
        var jolts = fake.calls.Where(c => c.part == "jolt").ToList();
        Check(boltFull > 0f && boltFull <= U.Commit + U.BoltJolt + Dt * 1.01f && bolt.All(c => c.t >= U.Commit - 1e-4f) && jolts.Count == 1 && Near(jolts[0].value, U.LeafShiftMetres, 1e-6f)
              && Near(jolts[0].t, U.Commit, Dt * 1.01f) && cubeStill,
            "19 rig " + side + ": SetDeadbolt to 1 by " + boltFull.ToString("0.000") + "; Jolt(" + (jolts.Count > 0 ? jolts[0].value.ToString("0.0000") : "-") + ") once at the commit, and no JoltLeaf on the cube");
        var knob = fake.calls.Where(c => c.part == "knob").ToList();
        var latch = fake.calls.Where(c => c.part == "latch").ToList();
        {
            var knobFull = knob.Where(c => c.value >= U.KnobDeg - .01f).Select(c => c.t).DefaultIfEmpty(-1f).First();
            var knobBack = knob.Count > 0 && knob[knob.Count - 1].value == 0f ? knob[knob.Count - 1].t : -1f;
            var latchFull = latch.Where(c => c.value >= 1f - 1e-4f).Select(c => c.t).DefaultIfEmpty(-1f).First();
            var latchBack = latch.Count > 0 && latch[latch.Count - 1].value == 0f ? latch[latch.Count - 1].t : -1f;
            Check(knobFull > 0f && knobFull <= U.KnobTurnEnd + Dt * 1.01f && knobBack > 0f && knobBack <= U.KnobReturnEnd + Dt * 1.01f && latchFull > 0f && latchFull <= U.KnobTurnEnd + Dt * 1.01f && latchBack > 0f && latchBack <= U.KnobReturnEnd + Dt * 1.01f,
                "19 rig " + side + ": SetKnob 40 by " + knobFull.ToString("0.000") + " and 0 by " + knobBack.ToString("0.000") + "; SetLatch with it");
        }
        var faces = fake.calls.Where(c => c.part == "plug" || c.part == "knob").All(c => c.face == h.face);
        Check(faces && h.unlocked.Count == 1 && (h.unlocked[0].at - fake.Keyhole(h.face)).magnitude < 1e-4f,
            "19 rig " + side + ": every plug and knob call carries the opener's face (" + h.face + "); the keyhole comes from the rig");
        Done(h);
    }

    // 8. The player: S at 0.10 is swallowed; S at 0.40 cancels, the door stays locked, a fresh E turns the key again.
    static void PlayerCancelEarly(H h)
    {
        if (!Start(h, false)) return;
        RunTo(h, .10f);
        var swallowed = h.rig.Consume(ShotInput.Back);
        Frame(h);
        var notCancelled = h.shot.Live;
        RunTo(h, .40f);
        h.rig.Consume(ShotInput.Back);
        var freeAtOnce = !h.shot.MoveLocked;
        var cancelAt = h.clock;
        var home = -1f;
        RunTo(h, 1.40f, () => { if (home < 0f && !h.rig.InShot && CamToEye(h) < 1e-3f) home = h.clock - cancelAt; });
        Check(swallowed && notCancelled, "8 S at 0.10 s is swallowed and does not cancel");
        Check(freeAtOnce && !h.shot.Live && h.unlocked.Count == 0 && !h.Has(DoorBeat.BoltRetract) && !h.world.IsUnlockedForTools(h.door) && !h.world.IsHeldForKey(h.door)
              && h.Has(DoorBeat.KeyWithdraw) && h.Has(DoorBeat.Return) && home > 0f && home <= U.CancelCameraBack + Dt * 1.01f && h.ended == 1,
            "8 S at 0.40 s cancels: move free the same frame, no DoorUnlocked or BoltRetract over 1 s, the door locked, the camera home after " + home.ToString("0.000") + " s");
        var again = h.world.Use(h.door.leaf);
        Check(again == FrontRoomsMapWorld.UseResult.KeyTurn, "8 a fresh E turns the key again (" + again + ")");
        h.world.CancelKeyTurn(h.door);
        Done(h);
    }

    // The FOV through a cancel: never more than 8° in 0.1 s, and back over MinFovChangeSeconds (the pose may go faster).
    static string FovReturn(H h, List<float> fovs, int cancelIndex, float atCancel, out bool ok)
    {
        var worst = 0f;
        for (var i = 6; i < fovs.Count; i++) worst = Mathf.Max(worst, Mathf.Abs(fovs[i] - fovs[i - 6]));
        var settle = FrontRoomsShotTimings.MinFovChangeSeconds;
        var at30 = cancelIndex + Mathf.RoundToInt(.30f / Dt);
        var at40 = cancelIndex + Mathf.RoundToInt(settle / Dt) + 1;
        var off = Mathf.Abs(atCancel - 76f);
        var stillOff = off < .5f || (at30 < fovs.Count && Mathf.Abs(fovs[at30] - 76f) >= off * .1f);
        var back = at40 < fovs.Count && Mathf.Abs(fovs[at40] - 76f) <= .1f;
        ok = worst <= 8f && stillOff && back;
        return "at most " + worst.ToString("0.0") + "° per 0.1 s, " + (at30 < fovs.Count ? Mathf.Abs(fovs[at30] - 76f).ToString("0.00") : "-") + "° off 0.30 s after, back within 0.1° by 0.40 s";
    }

    // 3 (cancels). S at 0.6: the pose back in 0.25 s, the FOV over 0.4 s.
    static void PlayerCancelFov(H h, float m)
    {
        if (!Start(h, false)) return;
        var fovs = new List<float>();
        RunTo(h, .6f, () => fovs.Add(h.camera.fieldOfView));
        var cancelIndex = fovs.Count - 1;
        var atCancel = h.camera.fieldOfView;
        h.rig.Consume(ShotInput.Back);
        var home = -1f;
        var start = h.clock;
        RunTo(h, 1.3f, () =>
        {
            fovs.Add(h.camera.fieldOfView);
            if (home < 0f && (h.camera.transform.position - h.rig.BaseEye.position).magnitude < 1e-3f && !h.rig.InShot) home = h.clock - start;
        });
        var text = FovReturn(h, fovs, cancelIndex, atCancel, out var ok);
        Check(ok && home > 0f && home <= U.CancelCameraBack + Dt * 1.01f && !h.world.IsUnlockedForTools(h.door),
            "3 S at 0.6 s: the pose home after " + home.ToString("0.000") + " s; the FOV " + text + "; the door locked");
        Done(h);
    }

    // 9. The Relay before the commit: sight at 8 m does not cancel; sight at 5 m does; a chase at 20 m does.
    static void RelayBefore(H h)
    {
        if (!Start(h, false)) return;
        RunTo(h, .5f);
        for (var i = 0; i < 6; i++) Frame(h, false, true, 8f);
        var kept = h.shot.Live;
        Frame(h, false, true, 5f);
        var cancelled = !h.shot.Live;
        var start = h.clock;
        var home = -1f;
        RunTo(h, 1.6f, () => { if (home < 0f && CamToEye(h) < 1e-3f && !h.rig.InShot) home = h.clock - start + Dt; });
        Check(kept && cancelled && h.unlocked.Count == 0 && !h.world.IsUnlockedForTools(h.door) && home > 0f && home <= U.CancelCameraBack + Dt * 1.01f,
            "9 before the commit: sight at 8 m keeps the shot, sight at 5 m cancels it; the door locked, the pose home after " + home.ToString("0.000") + " s");
        Done(h);
        if (!Start(h, false)) return;
        RunTo(h, .5f);
        Frame(h, true, false, 20f);
        cancelled = !h.shot.Live;
        start = h.clock;
        home = -1f;
        RunTo(h, 1.6f, () => { if (home < 0f && CamToEye(h) < 1e-3f && !h.rig.InShot) home = h.clock - start + Dt; });
        Check(cancelled && h.unlocked.Count == 0 && !h.world.IsUnlockedForTools(h.door) && home > 0f && home <= U.CancelCameraBack + Dt * 1.01f,
            "9 before the commit: a chase at 20 m cancels; the door locked, the pose home after " + home.ToString("0.000") + " s");
        Done(h);
    }

    // 10 (and 3's Relay cancel). The Relay after the commit: any sight frees the player in 0.15 s; the door stays unlocked and shut; E opens it later.
    static void RelayAfter(H h, float m, bool leafSide)
    {
        if (!Start(h, leafSide)) return;
        var fovs = new List<float>();
        RunTo(h, .95f, () => fovs.Add(h.camera.fieldOfView));
        var cancelIndex = fovs.Count;
        var atCancel = h.camera.fieldOfView;
        Frame(h, false, true, 12f);
        fovs.Add(h.camera.fieldOfView);
        var free = !h.shot.Live && !h.shot.MoveLocked && h.shot.LookScale == 1f;
        var start = h.clock - Dt;
        var home = -1f;
        var shut = true;
        RunTo(h, 3f, () =>
        {
            fovs.Add(h.camera.fieldOfView);
            if (home < 0f && CamToEye(h) < 1e-3f && !h.rig.InShot) home = h.clock - start;
            if (h.door.angle != 0f || h.door.open) shut = false;
        });
        var side = leafSide ? "push, leaf side" : "push";
        var text = FovReturn(h, fovs, cancelIndex - 1, atCancel, out var fovOk);
        Check(free && h.unlocked.Count == 1 && h.world.IsUnlockedForTools(h.door) && !h.world.IsHeldForKey(h.door) && shut && h.handles.Count == 0 && h.pulls.Count == 0 && h.began.Count == 0
              && home > 0f && home <= U.ChaseFreeSnap + Dt * 1.01f,
            "10 " + side + ": sight at 12 m at 0.95 s frees move and look at once, the pose home after " + home.ToString("0.000") + " s; unlocked and shut for 2 s (no hinge, no DoorHandleTurned, no DoorPulled), no hold");
        Check(fovOk, "3 " + side + ": the Relay's cancel at 0.95 s: the FOV " + text);
        var later = h.world.Use(h.door.leaf);
        Check(later == FrontRoomsMapWorld.UseResult.Opened && h.door.open, "10 " + side + ": a later E opens it with no key turn (" + later + ")");
        Done(h);
    }

    // 11. The player after the commit: S leaves it unlocked and shut; a second E opens it (a normal open); E at 1.40 is the buffered push.
    static void PlayerAfter(H h)
    {
        if (Start(h, false))
        {
            RunTo(h, .95f);
            h.rig.Consume(ShotInput.Back);
            var shut = true;
            RunTo(h, 3f, () => { if (h.door.angle != 0f || h.door.open) shut = false; });
            Check(shut && h.world.IsUnlockedForTools(h.door) && !h.world.IsHeldForKey(h.door) && h.pulls.Count == 0 && h.handles.Count == 0,
                "11 S at 0.95 s: unlocked and shut (no hinge motion, no DoorPulled)");
            Done(h);
        }
        foreach (var leafSide in new[] { false, true })
        {
            if (!Start(h, leafSide)) continue;
            RunTo(h, .95f);
            h.rig.Consume(ShotInput.Use);
            RunTo(h, 3f);
            var open = h.began.Count > 0 ? h.began[0] : default;
            Check(h.door.open && h.handles.Count == 1 && h.began.Count == 1 && !open.latchDrawn && open.source == FrontRoomsMapWorld.DoorOpenSource.Unlock && h.pulls.Count == 0
                  && FrontRoomsMapWorld.FaceOf(h.door, h.door.leaf.transform.position) != h.face,
                "11 a second E at 0.95 s (" + (leafSide ? "push, leaf side" : "push") + "): a normal open away from the opener (DoorHandleTurned, no DoorPulled)");
            Done(h);
        }
        if (Start(h, false))
        {
            RunTo(h, 1.40f);
            var taken = h.rig.Consume(ShotInput.Use);
            var buffered = h.rig.BufferedPush;
            Frame(h);
            var stillOn = h.shot.Live;
            RunTo(h, 1.6f);
            Check(taken && buffered && stillOn && !h.rig.BufferedPush && h.door.open && h.unlocked.Count == 1,
                "11 E at 1.40 s: a buffered push, not a cancel; the shot cleared it at its end");
            Done(h);
        }
    }

    // 12. The Relay starts breaking the door at 0.5 s: the shot cancels and the door stays locked; the break still works.
    static void BeingBroken(H h)
    {
        if (!Start(h, false)) return;
        RunTo(h, .5f);
        h.world.MarkBeingBroken(h.door, true);
        Frame(h);
        var cancelled = !h.shot.Live;
        RunTo(h, 1.2f);
        var locked = !h.world.IsUnlockedForTools(h.door) && !h.world.IsHeldForKey(h.door) && h.unlocked.Count == 0;
        h.world.BreakDoor(h.door, h.feet);
        Check(cancelled && locked && h.door.broken && !h.world.IsBeingBroken(h.door), "12 being broken at 0.5 s: the shot cancels, the door stays locked, and the break goes through");
        Done(h);
    }

    // 13. Esc: the driver and the rig stop, DoorsPaused holds the doors and the watchdog; the commit lands at shot time 0.86; a swing freezes too.
    static void Pause(H h)
    {
        if (!Start(h, false)) return;
        RunTo(h, .5f);
        var beats = h.beats.Count;
        var warned = warnings;
        h.world.DoorsPaused = true;
        for (var i = 0; i < 120; i++) h.world.TickDoorsForTools(Dt);
        var frozen = h.beats.Count == beats && h.unlocked.Count == 0 && warnings == warned && h.world.IsHeldForKey(h.door);
        h.world.DoorsPaused = false;
        RunTo(h, 1.02f);
        var commitAt = h.unlocked.Count > 0 ? h.unlocked[0].t : -1f;
        h.world.DoorsPaused = true;
        var angle = h.door.angle;
        var moves = h.moves.Count;
        for (var i = 0; i < 120; i++) h.world.TickDoorsForTools(Dt);
        var swingFrozen = h.door.angle == angle && h.moves.Count == moves && angle > 0f;
        h.world.DoorsPaused = false;
        RunTo(h, 1.8f);
        Check(frozen && Near(commitAt, U.Commit, Dt * 1.01f), "13 paused 2 s at 0.5: no beat, no DoorUnlocked, no watchdog, still held; the commit lands at shot time " + commitAt.ToString("0.000"));
        Check(swingFrozen && h.door.angle >= ModuleUnits.DoorSwingDegrees - .5f, "13 paused 2 s at 1.02 (push): the leaf holds at " + angle.ToString("0.0") + "° with no DoorMoved, then finishes");
        Done(h);
    }

    // 14. In place (chunks building, a climb): no pose and no FOV, the same beats and events, move held to the commit only, look free.
    static void InPlace(H h, float m)
    {
        if (!Start(h, false, true)) return;
        var maxOff = 0f;
        var maxFov = 0f;
        var maxTurn = 0f;
        var lookFree = true;
        var moveHeld = false;
        var moveFreeAfter = true;
        RunTo(h, 1.75f, () =>
        {
            maxOff = Mathf.Max(maxOff, CamToEye(h));
            maxFov = Mathf.Max(maxFov, Mathf.Abs(h.camera.fieldOfView - 76f));
            maxTurn = Mathf.Max(maxTurn, Quaternion.Angle(h.camera.transform.rotation, h.rig.BaseEye.rotation));
            if (h.shot.LookScale != 1f) lookFree = false;
            if (Near(h.clock, .5f, Dt * .5f)) moveHeld = h.shot.MoveLocked;
            if (h.clock > U.Commit + Dt && h.shot.MoveLocked) moveFreeAfter = false;
        });
        Check(maxOff < 1e-4f && maxFov < 1e-3f && maxTurn <= U.BoltJoltDeg * m * 2f + .05f,
            "14 in place: the camera stays on the eye (" + (maxOff * 1000f).ToString("0.000") + " mm), FOV 76, only the bolt's jolt turns it (" + maxTurn.ToString("0.00") + "°)");
        Check(Near(h.BeatAt(DoorBeat.KeyInsert), U.InsertStart, Dt * 1.01f) && h.unlocked.Count == 1 && Near(h.unlocked[0].t, U.Commit, Dt * 1.01f)
              && Near(h.BeatAt(DoorBeat.Open), U.KnobTurnEnd, Dt * 1.01f) && h.ended == 1 && h.endedAt <= U.ReturnEnd + Dt * 2.01f,
            "14 in place: the same beats and events (KeyInsert, DoorUnlocked at the commit, the open at 1.00, ShotEnded at 1.55)");
        Check(lookFree && moveHeld && moveFreeAfter, "14 in place: look free throughout, move held only until the commit");
        Done(h);
        if (!Start(h, false, true)) return;
        RunTo(h, .4f);
        h.rig.Consume(ShotInput.Back);
        RunTo(h, 1.4f);
        Check(!h.shot.Live && !h.world.IsUnlockedForTools(h.door) && !h.world.IsHeldForKey(h.door) && h.unlocked.Count == 0, "14 in place: S at 0.4 s cancels as at full motion (the door locked)");
        Done(h);
    }

    // 15. The door's chunk dropped and rebuilt mid-shot: before the commit the shot cancels and the door stays locked; after it, unlocked by edge.
    static void Rebuild(H h)
    {
        foreach (var at in new[] { .5f, .95f })
        {
            if (!Start(h, false)) return;
            RunTo(h, at);
            var chunks = new HashSet<GridCoord> { MapGrid.ChunkOf(h.a), MapGrid.ChunkOf(h.b) };
            var threw = "";
            try
            {
                foreach (var c in chunks) h.world.DropChunkForTools(c);
                foreach (var c in chunks) h.world.RebuildChunk(c);
                Physics.SyncTransforms();
                Frame(h);
                RunTo(h, at + .6f);
            }
            catch (Exception e) { threw = e.GetType().Name + " " + e.Message; }
            var rebuilt = h.world.DoorBetween(h.a, h.b);
            var committed = at > U.Commit;
            Check(threw.Length == 0 && rebuilt != null && rebuilt != h.door && !h.shot.Live && !h.world.IsHeldForKey(rebuilt) && h.world.IsUnlockedForTools(rebuilt) == committed && !rebuilt.open,
                "15 rebuilt at " + at.ToString("0.00") + " s: the shot cancels, no exception" + threw + ", the new door " + (committed ? "unlocked by edge" : "still locked") + " and shut");
            h.door = rebuilt;
            Done(h);
        }
    }

    // 16. Abort (Caught, a reset): the hold resolved, no later beat; after the commit nothing more is raised.
    static void AbortCases(H h)
    {
        if (Start(h, false))
        {
            RunTo(h, .5f);
            h.shot.Abort();
            var beats = h.beats.Count;
            RunTo(h, 2f);
            Check(!h.world.IsHeldForKey(h.door) && !h.world.IsUnlockedForTools(h.door) && h.beats.Count == beats && h.unlocked.Count == 0 && !h.door.open,
                "16 Abort at 0.5 s: the hold released, the door locked, no later beat");
            Done(h);
        }
        if (Start(h, false))
        {
            RunTo(h, .99f);
            h.shot.Abort();
            var counts = (h.beats.Count, h.unlocked.Count, h.handles.Count, h.began.Count, h.moves.Count, h.pulls.Count);
            RunTo(h, 2.5f);
            Check(counts == (h.beats.Count, h.unlocked.Count, h.handles.Count, h.began.Count, h.moves.Count, h.pulls.Count) && h.world.IsUnlockedForTools(h.door) && !h.world.IsHeldForKey(h.door) && !h.door.open,
                "16 Abort at 0.99 s (committed): nothing more is raised, the door unlocked and shut");
            Done(h);
        }
    }

    // 17. Orphaned holds: no owner (the walker) unlocks at once; an owner that never ticks is resolved by the watchdog; ApplyLive clears a hold.
    static void Orphans(H h)
    {
        Stand(h, false);
        h.Clear();
        h.world.KeyTurnOwner = null;
        var r = h.world.Use(h.door.leaf);
        Check(r == FrontRoomsMapWorld.UseResult.Opened && h.unlocked.Count == 1 && h.door.open && !h.world.IsHeldForKey(h.door),
            "17 no owner: Use answers Opened, DoorUnlocked at once, the door opens, no hold");
        h.world.KeyTurnOwner = h.shot;
        for (var i = 0; i < 6; i++) h.world.TickDoorsForTools(Dt);

        foreach (var commit in new[] { false, true })
        {
            Stand(h, false);
            h.Clear();
            r = h.world.Use(h.door.leaf);
            if (commit) h.world.CommitUnlock(h.door);
            var warned = warnings;
            var grace = U.Duration + U.KeyTurnWatchdogGrace;
            var frames = Mathf.FloorToInt(grace / Dt) - 1;
            for (var i = 0; i < frames; i++) h.world.TickDoorsForTools(Dt);
            var stillHeld = h.world.IsHeldForKey(h.door);
            for (var i = 0; i < 4; i++) h.world.TickDoorsForTools(Dt);
            var resolved = !h.world.IsHeldForKey(h.door) && warnings == warned + 1;
            if (!commit)
            {
                var again = h.world.Use(h.door.leaf);
                Check(r == FrontRoomsMapWorld.UseResult.KeyTurn && stillHeld && resolved && !h.world.IsUnlockedForTools(h.door) && !h.door.open && again == FrontRoomsMapWorld.UseResult.KeyTurn,
                    "17 an owner that never ticks: held to " + grace.ToString("0.00") + " s, then cancelled with one warning, the door locked; a fresh E turns the key (" + again + ")");
                h.world.CancelKeyTurn(h.door);
            }
            else Check(r == FrontRoomsMapWorld.UseResult.KeyTurn && stillHeld && resolved && h.world.IsUnlockedForTools(h.door) && !h.door.open && h.unlocked.Count == 1,
                "17 committed, then never ticked: released after " + grace.ToString("0.00") + " s with one warning, the door unlocked and shut");
        }

        if (!Start(h, false)) return;
        RunTo(h, .5f);
        var threw = "";
        try
        {
            h.world.ApplyLive(h.profile);
            var cleared = !h.world.IsHeldForKey(h.door);
            Frame(h);
            Check(cleared && !h.shot.Live && !h.world.IsUnlockedForTools(h.door), "17 ApplyLive mid-shot clears the hold; the shot cancels on its next tick, the door locked");
        }
        catch (Exception e) { threw = e.GetType().Name + " " + e.Message; }
        if (threw.Length > 0) Check(false, "17 ApplyLive mid-shot threw " + threw);
        Done(h);
    }

    // ------------------------------------------------------------------ the key pickup (20-24, once)

    // The test's own zones for keys that are not the map's (never held, so KeyArrived announces nothing for them).
    static readonly GridCoord FakeZone = new GridCoord(-9999, -9999);

    static void Keys(H h, List<GameObject> roots)
    {
        if (h.shot.Live) h.shot.Abort();
        h.world.DoorsPaused = false;
        try
        {
            KeyNoModel(h);
            KeyNoPresenter(h);
            KeyPlaceholder(h);
            KeyWall(h, roots);
            KeyDressed(h, roots);
            KeyAimAssist(h, roots);
            KeyLookE(h);
        }
        finally
        {
            if (h.shot.Live) h.shot.Abort();
            h.world.KeyPresenter = null;
            h.world.KeyPickupNeedsE = false;
            FrontRoomsMapWorld.ShowPlaceholderKey = false;
            SetMotion(100);
        }
    }

    // The body on the nearest key still lying (on its flat position, the floor's height), facing the first of eight
    // ways whose held point (viewport .65, .30 at 0.35 m) is clear of the world, eyes level; the case's records cleared.
    static Transform OnKey(H h, out GridCoord zone, out GridCoord chunk)
    {
        h.shot.Abort();
        h.rig.ResetLayers();
        var key = h.world.NearestKeyForTools(h.body.position, out zone, out chunk);
        if (key == null) return null;
        var feet = new Vector3(key.position.x, h.door.position.y, key.position.z);
        for (var i = 0; i < 8; i++)
        {
            h.body.SetPositionAndRotation(feet, Quaternion.Euler(0f, i * 45f, 0f));
            h.rig.SetBase(0f);
            Physics.SyncTransforms();
            h.rig.Tick(Dt);
            if ((HeldPoint(h, FrontRoomsShotTimings.KeyPickup.HeldViewportY) - h.rig.ClampToWorld(h.camera.transform.position, HeldPoint(h, FrontRoomsShotTimings.KeyPickup.HeldViewportY))).sqrMagnitude < 1e-8f) break;
        }
        h.Clear();
        return key;
    }

    static Vector3 HeldPoint(H h, float viewportY) =>
        h.camera.ViewportToWorldPoint(new Vector3(FrontRoomsShotTimings.KeyPickup.HeldViewportX, viewportY, FrontRoomsShotTimings.KeyPickup.HeldDistance));

    // A zone's key still lying in a built chunk (near a point).
    static bool KeyLying(H h, GridCoord zone, Vector3 near)
    {
        var key = h.world.NearestKeyForTools(near, out var z, out _);
        return key != null && z == zone && (key.position - near).sqrMagnitude < 4f;
    }

    // 20. No model (no KeyRig, the placeholder off): the presenter declines, and the pickup is today's: KeyPickupStarted,
    // then KeyTaken, in the one call at contact, the object killed. Keys have no collider with look+E off.
    static void KeyNoModel(H h)
    {
        FrontRoomsMapWorld.ShowPlaceholderKey = false;
        h.world.KeyPresenter = h.shot.BeginKeyPickup;
        var key = OnKey(h, out var zone, out _);
        if (key == null) { Check(false, "20 no key lying in the test map"); return; }
        var go = key.gameObject;
        var noCollider = go.GetComponent<Collider>() == null;
        var declined = !h.shot.BeginKeyPickup(zone, key);
        h.world.CollectKeysForTools();
        Check(declined && h.keyOrder.Count == 2 && h.keyOrder[0] == "S " + zone && h.keyOrder[1] == "T " + zone && go == null
              && h.world.HasKeyFor(zone) && !h.shot.Presenting(zone) && noCollider,
            "20 no model: BeginKeyPickup declines; KeyPickupStarted then KeyTaken in the same call at contact (" + string.Join(", ", h.keyOrder) + "), the object killed, the key held, no collider on it");
    }

    // 24. No presenter (tests, tools, the walker): the old instant pickup, even with the placeholder on.
    static void KeyNoPresenter(H h)
    {
        FrontRoomsMapWorld.ShowPlaceholderKey = true;
        h.world.KeyPresenter = null;
        var key = OnKey(h, out var zone, out _);
        if (key == null) { Check(false, "24 no key lying in the test map"); return; }
        var go = key.gameObject;
        h.world.CollectKeysForTools();
        Check(h.keyOrder.Count == 2 && h.keyOrder[0] == "S " + zone && h.keyOrder[1] == "T " + zone && go == null && h.world.HasKeyFor(zone) && !h.shot.Presenting(zone),
            "24 no presenter (the placeholder on): KeyPickupStarted then KeyTaken in the same call at contact (" + string.Join(", ", h.keyOrder) + "), the object killed");
    }

    // 21. The placeholder: held at contact, KeyTaken at the 0.25 s arrival, the object on the map's root through a chunk drop
    // and rebuild (no respawn), held at the viewport point over 0.25-0.75 facing the lens (no spin), shrunk to the key's
    // envelope, the pocket beat at 0.85, gone at 0.95.
    static void KeyPlaceholder(H h)
    {
        FrontRoomsMapWorld.ShowPlaceholderKey = true;
        h.world.KeyPresenter = h.shot.BeginKeyPickup;
        var key = OnKey(h, out var zone, out var chunk);
        if (key == null) { Check(false, "21 no key lying in the test map"); return; }
        var lay = key.position;
        h.world.CollectKeysForTools();
        Check(h.keyOrder.Count == 1 && h.keyOrder[0] == "S " + zone && (h.keyStartedPos - lay).magnitude < 1e-4f && h.world.HasKeyFor(zone)
              && h.shot.Presenting(zone) && key != null && key.parent == h.world.transform,
            "21 placeholder: KeyPickupStarted once at contact with the zone and the key's position; held at once, KeyTaken not yet, presented, the object on the map's root");
        var threw = "";
        var survived = false;
        var respawned = true;
        try
        {
            h.world.DropChunkForTools(chunk);
            survived = key != null;
            h.world.RebuildChunk(chunk);
            Physics.SyncTransforms();
            respawned = KeyLying(h, zone, lay);
        }
        catch (Exception e) { threw = " " + e.GetType().Name + " " + e.Message; }
        Check(threw.Length == 0 && survived && !respawned, "21 placeholder: the chunk that built it dropped and rebuilt: the presented key survives, and none comes back for the zone" + threw);
        var takenAt024 = -1;
        var takenAt025 = -1;
        var heldErr = 0f;
        var facingErr = 0f;
        var scaleErr = 0f;
        var rot030 = Quaternion.identity;
        var spin = -1f;
        var goneAt = -1f;
        RunTo(h, 1.05f, () =>
        {
            var c = h.clock;
            if (Near(c, .24f, Dt * .5f) || Near(c, .25f - Dt, Dt * .5f)) takenAt024 = h.keyTakenAt.Count;
            if (Near(c, .25f, Dt * .5f)) takenAt025 = h.keyTakenAt.Count;
            if (key != null && c >= .25f - 1e-4f && c <= .75f + 1e-4f)
            {
                var lens = h.camera.transform;
                heldErr = Mathf.Max(heldErr, (key.position - HeldPoint(h, FrontRoomsShotTimings.KeyPickup.HeldViewportY)).magnitude);
                facingErr = Mathf.Max(facingErr, Vector3.Angle(key.forward, lens.position - key.position));
                scaleErr = Mathf.Max(scaleErr, (key.lossyScale - FrontRoomsMapWorld.KeyPlaceholderSize).magnitude);
                if (Near(c, .30f, Dt * .5f)) rot030 = key.rotation;
                if (Near(c, .60f, Dt * .5f)) spin = Quaternion.Angle(rot030, key.rotation);
            }
            if (goneAt < 0f && key == null && !h.shot.Presenting(zone)) goneAt = c;
        });
        var takenOk = takenAt024 == 0 && takenAt025 == 1 && h.keyTakenAt.Count == 1 && Near(h.keyTakenAt[0], FrontRoomsShotTimings.KeyPickup.Arrive, Dt * 1.01f) && h.keyOrder.Count == 2 && h.keyOrder[1] == "T " + zone;
        Check(takenOk, "21 placeholder: KeyTaken 0 at 0.24 s, once at " + (h.keyTakenAt.Count > 0 ? h.keyTakenAt[0].ToString("0.000") : "never") + " s (0.25 +/- one frame)");
        Check(heldErr <= .02f && facingErr < 1f && scaleErr < 1e-3f && spin >= 0f && spin < .01f,
            "21 placeholder: held within " + (heldErr * 100f).ToString("0.00") + " cm of the viewport point (.65, .30, 0.35 m) over 0.25-0.75 s, facing the lens (" + facingErr.ToString("0.00")
            + " deg off), at the key's envelope, no spin (" + spin.ToString("0.000") + " deg from 0.30 to 0.60 s)");
        Check(h.pocketAt.Count == 1 && Near(h.pocketAt[0], FrontRoomsShotTimings.KeyPickup.PocketSound, Dt * 1.01f) && Near(goneAt, FrontRoomsShotTimings.KeyPickup.PocketEnd, Dt * 1.01f) && !h.shot.Presenting(zone),
            "21 placeholder: the pocket beat at " + (h.pocketAt.Count > 0 ? h.pocketAt[0].ToString("0.000") : "never") + " s (0.85), the object gone and Presenting false at " + goneAt.ToString("0.000") + " s (0.95)");
    }

    // 21. A wall 0.25 m ahead of the camera: the held key stays 0.1 m short of its face (the rig's spherecast).
    static void KeyWall(H h, List<GameObject> roots)
    {
        FrontRoomsMapWorld.ShowPlaceholderKey = true;
        h.shot.Abort();
        h.rig.ResetLayers();
        h.rig.Tick(Dt);
        h.Clear();
        var lens = h.camera.transform;
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "KEY TEST / wall";
        wall.hideFlags = HideFlags.DontSave;
        roots.Add(wall);
        wall.transform.SetPositionAndRotation(lens.position + lens.forward * .30f, lens.rotation);
        wall.transform.localScale = new Vector3(3f, 3f, .1f);
        var face = lens.position + lens.forward * .25f;
        var fake = GameObject.CreatePrimitive(PrimitiveType.Cube);
        UnityEngine.Object.DestroyImmediate(fake.GetComponent<Collider>());
        fake.hideFlags = HideFlags.DontSave;
        roots.Add(fake);
        fake.transform.position = lens.position + lens.forward * .2f - Vector3.up * .5f;
        fake.transform.localScale = new Vector3(.32f, .12f, .12f);
        Physics.SyncTransforms();
        var key = fake.transform;
        var began = h.shot.BeginKeyPickup(FakeZone, key);
        var gap = float.MaxValue;
        RunTo(h, .75f, () =>
        {
            if (key != null && h.clock >= .25f - 1e-4f) gap = Mathf.Min(gap, Vector3.Dot(face - key.position, lens.forward));
        });
        h.shot.Abort();
        UnityEngine.Object.DestroyImmediate(wall);
        Check(began && gap >= .1f - 1e-3f && gap < .15f, "21 placeholder: with a wall 0.25 m ahead of the camera the held key stays " + gap.ToString("0.000") + " m short of its face (>= 0.1)");
    }

    // 21. A dressed key (FakeKeyRig on the key's object): presented with the placeholder off; posed through PoseHeld at the
    // viewport point facing the lens every frame, the tag's sway once at the arrival, the root never scaled, Release, then
    // gone at 0.95. (A destroyed component's own fields stay readable, so the counts are read after the end.)
    static void KeyDressed(H h, List<GameObject> roots)
    {
        FrontRoomsMapWorld.ShowPlaceholderKey = false;
        h.shot.Abort();
        h.rig.ResetLayers();
        h.rig.Tick(Dt);
        h.Clear();
        var lens = h.camera.transform;
        var root = new GameObject("KEY TEST / dressed key") { hideFlags = HideFlags.DontSave };
        roots.Add(root);
        root.transform.position = lens.position + lens.forward * .5f - Vector3.up * .6f;
        var dressed = root.AddComponent<FrontRoomsFakeKeyRig>();
        dressed.clock = () => h.clock;
        var key = root.transform;
        var began = h.shot.BeginKeyPickup(FakeZone, key);
        var heldErr = 0f;
        var facingErr = 0f;
        var goneAt = -1f;
        var releasedEarly = false;
        var scaled = false;
        RunTo(h, 1.05f, () =>
        {
            if (key != null)
            {
                scaled |= (key.localScale - Vector3.one).sqrMagnitude > 1e-8f;
                releasedEarly |= dressed.releases > 0;
                if (h.clock >= .25f - 1e-4f && h.clock <= .75f + 1e-4f)
                {
                    heldErr = Mathf.Max(heldErr, (key.position - HeldPoint(h, FrontRoomsShotTimings.KeyPickup.HeldViewportY)).magnitude);
                    facingErr = Mathf.Max(facingErr, Vector3.Angle(key.forward, lens.position - key.position));
                }
            }
            else if (goneAt < 0f) goneAt = h.clock;
        });
        Check(began && heldErr <= .02f && facingErr < 1f && dressed.held.Count >= 50 && !scaled,
            "21 dressed key (fake KeyRig, the placeholder off): presented; PoseHeld every frame (" + dressed.held.Count + ") puts it within " + (heldErr * 100f).ToString("0.00")
            + " cm of the viewport point facing the lens (" + facingErr.ToString("0.00") + " deg off) over 0.25-0.75 s; the root never scaled");
        Check(dressed.sways == 1 && Near(dressed.swayAt, FrontRoomsShotTimings.KeyPickup.Arrive, Dt * 1.01f) && Mathf.Approximately(dressed.swayDegrees, FrontRoomsShotTimings.KeyPickup.TagSwingDeg)
              && Mathf.Approximately(dressed.swayHz, FrontRoomsShotTimings.KeyPickup.TagSwingHz) && Mathf.Approximately(dressed.swayDecay, FrontRoomsShotTimings.KeyPickup.TagSwingDecay)
              && !releasedEarly && dressed.releases == 1 && Near(goneAt, FrontRoomsShotTimings.KeyPickup.PocketEnd, Dt * 1.01f),
            "21 dressed key: the tag's sway once at " + dressed.swayAt.ToString("0.000") + " s (25 deg, 2.2 Hz, 2.5); Release once at the end, the root destroyed at " + goneAt.ToString("0.000") + " s");
    }

    // 22. The aim assist: a key 20 deg below the view with the mouse idle draws the pitch 6 deg (x Camera motion) toward it over
    // 0.2 s, never more; a look stops it for good; a key 90 deg to the side or behind gets none; at Off, none.
    static void KeyAimAssist(H h, List<GameObject> roots)
    {
        FrontRoomsMapWorld.ShowPlaceholderKey = true;
        float Assist(float pitchDeg, float yawDeg, int lookAtFrame, out float before)
        {
            h.shot.Abort();
            h.rig.ResetLayers();
            h.rig.SetBase(0f);
            h.rig.Tick(Dt);
            var eye = h.rig.BaseEye;
            var fake = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.DestroyImmediate(fake.GetComponent<Collider>());
            fake.hideFlags = HideFlags.DontSave;
            roots.Add(fake);
            fake.transform.position = eye.position + eye.rotation * (Quaternion.Euler(pitchDeg, yawDeg, 0f) * Vector3.forward) * 1.5f;
            fake.transform.localScale = Vector3.one * .1f;
            h.shot.BeginKeyPickup(FakeZone, fake.transform);
            var total = 0f;
            before = 0f;
            for (var i = 1; i <= 30; i++)
            {
                if (i == lookAtFrame) before = total;
                total += h.shot.AimAssistStep(Dt, i != lookAtFrame);
            }
            if (lookAtFrame <= 0) before = total;
            h.shot.Abort();
            return total;
        }
        var m = FrontRoomsSettings.CameraMotionScale;
        var idle = Assist(20f, 0f, 0, out _);
        var stopped = Assist(20f, 0f, 7, out var beforeLook);
        var side = Assist(0f, 90f, 0, out _);
        var behind = Assist(0f, 180f, 0, out _);
        var up = Assist(-25f, 0f, 0, out _);
        SetMotion(0);
        var off = Assist(20f, 0f, 0, out _);
        SetMotion(100);
        h.shot.Abort();
        Check(Near(idle, FrontRoomsShotTimings.KeyPickup.AimAssistPitchDeg * m, 1e-3f) && Near(up, -FrontRoomsShotTimings.KeyPickup.AimAssistPitchDeg * m, 1e-3f),
            "22 aim assist: a key 20 deg below draws the pitch " + idle.ToString("0.000") + " deg down (6 x motion), one 25 deg above " + up.ToString("0.000") + " deg");
        Check(stopped == beforeLook && beforeLook > 0f && beforeLook < idle, "22 aim assist: a look at 0.1 s stops it for good (" + beforeLook.ToString("0.000") + " deg, then nothing)");
        Check(side == 0f && behind == 0f && off == 0f, "22 aim assist: none for a key 90 deg to the side or behind, none at Camera motion Off");
    }

    // 23. Look+E (the flag on): the key spawns with a trigger; the E ray, the Relay's sight ray and a nav capsule cast pass
    // through it; the key query finds it; Describe says TAKE KEY; walking over it does not take it; Use takes it (KeyTaken)
    // and starts the presentation; the spent trigger is found by nothing.
    static void KeyLookE(H h)
    {
        FrontRoomsMapWorld.ShowPlaceholderKey = true;
        h.world.KeyPresenter = h.shot.BeginKeyPickup;
        h.shot.Abort();
        var old = h.world.NearestKeyForTools(h.body.position, out var zone, out var chunk);
        if (old == null) { Check(false, "23 no key lying in the test map"); return; }
        var lay = old.position;
        h.world.KeyPickupNeedsE = true;
        h.world.RebuildChunk(chunk);
        Physics.SyncTransforms();
        var key = h.world.NearestKeyForTools(lay, out var z2, out _);
        var trigger = key != null && z2 == zone ? key.GetComponent<Collider>() : null;
        if (trigger == null) { Check(false, "23 the rebuilt chunk has no key of zone " + zone + " with a collider"); return; }
        Check(trigger.isTrigger && trigger.enabled, "23 look+E: the key spawns with a trigger collider (no solid one)");
        // Stand 1.5 m from it, where the eye sees it with nothing solid between.
        var seen = false;
        var eye = Vector3.zero;
        for (var i = 0; i < 16 && !seen; i++)
        {
            var dir = Quaternion.Euler(0f, i * 22.5f, 0f) * Vector3.forward;
            var feet = new Vector3(key.position.x, h.door.position.y, key.position.z) - dir * 1.5f;
            eye = feet + Vector3.up * ModuleUnits.PlayerEye;
            var to = key.position - eye;
            if (Physics.Raycast(eye, to.normalized, to.magnitude, ~0, QueryTriggerInteraction.Ignore)) continue;
            h.body.SetPositionAndRotation(feet, Quaternion.LookRotation(new Vector3(to.x, 0f, to.z), Vector3.up));
            h.rig.SetBase(Mathf.Atan2(-to.y, new Vector2(to.x, to.z).magnitude) * Mathf.Rad2Deg);
            Physics.SyncTransforms();
            h.rig.Tick(Dt);
            seen = true;
        }
        if (!seen) { Check(false, "23 no clear 1.5 m view of the key of zone " + zone); return; }
        var toKey = key.position - eye;
        var ray = new Ray(eye, toKey.normalized);
        var eHit = Physics.Raycast(ray, out var hit, toKey.magnitude + .5f, ~0, QueryTriggerInteraction.Ignore) && hit.collider == trigger;
        var sight = Physics.RaycastAll(new Ray(key.position + toKey.normalized * 3f, -toKey.normalized), 6f, ~0, QueryTriggerInteraction.Ignore).Any(x => x.collider == trigger);
        var r = ModuleUnits.PlayerRadius;
        var from = key.position - toKey.normalized * 2f;
        var capsule = Physics.CapsuleCastAll(new Vector3(from.x, h.door.position.y + r + .05f, from.z), new Vector3(from.x, h.door.position.y + ModuleUnits.PlayerHeight - r, from.z), r,
            new Vector3(toKey.x, 0f, toKey.z).normalized, 4f, ~0, QueryTriggerInteraction.Ignore).Any(x => x.collider == trigger);
        var found = h.world.KeyOnRay(ray, 2.4f, h.body) == trigger;
        Check(!eHit && !sight && !capsule && found, "23 look+E: the E ray, a sight ray and a nav capsule cast (Ignore) pass through the trigger; the key query finds it");
        Check(h.world.Describe(trigger, out var hold) == "E  \u00B7  TAKE KEY" && !hold, "23 look+E: Describe says E \u00B7 TAKE KEY");
        // Walking over it does not take it.
        var stand = h.body.position;
        var rotation = h.body.rotation;
        h.body.position = new Vector3(key.position.x, h.door.position.y, key.position.z);
        h.Clear();
        h.world.CollectKeysForTools();
        var walkedOver = h.keyOrder.Count > 0 || !KeyLying(h, zone, lay) || h.world.HasKeyFor(zone);
        h.body.SetPositionAndRotation(stand, rotation);
        Physics.SyncTransforms();
        Check(!walkedOver, "23 look+E: walking over the key does not take it");
        h.Clear();
        var result = h.world.Use(trigger);
        Check(result == FrontRoomsMapWorld.UseResult.KeyTaken && h.keyOrder.Count == 1 && h.world.HasKeyFor(zone) && h.shot.Presenting(zone) && !trigger.enabled
              && h.world.Describe(trigger, out _) == null && h.world.KeyOnRay(ray, 2.4f, h.body) == null,
            "23 look+E: Use answers " + result + ", the key held and presented; the spent trigger is off and found by nothing");
        RunTo(h, 1.0f);
        Check(h.keyTakenAt.Count == 1 && Near(h.keyTakenAt[0], FrontRoomsShotTimings.KeyPickup.Arrive, Dt * 1.01f) && !h.shot.Presenting(zone),
            "23 look+E: KeyTaken at " + (h.keyTakenAt.Count > 0 ? h.keyTakenAt[0].ToString("0.000") : "never") + " s, the presentation over by 1.0 s");
        h.world.KeyPickupNeedsE = false;
    }
}
