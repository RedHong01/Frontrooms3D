using System;
using System.Collections.Generic;
using System.IO;
using FrontRooms.Map;
using UnityEditor;
using UnityEngine;

/// <summary>Behaviour tests for the selected body-door rules. Uses a real generated leaf and a real
/// CharacterController. It does not assign door angles, open the passage through tools, or move a
/// character transform during the measured runs. Existing DoorShotTests remains the flag-off suite.</summary>
public static class FrontRoomsBodyDoorTests
{
    [Serializable] sealed class Report
    {
        public string verdict;
        public int passed, failed;
        public List<string> checks = new List<string>();
    }
    static Report report;
    static readonly List<GameObject> roots = new List<GameObject>();
    static readonly List<FrontRoomsLevelProfile> profiles = new List<FrontRoomsLevelProfile>();
    sealed class H
    {
        public FrontRoomsMapWorld map;
        public FrontRoomsMapWorld.Door door;
        public CharacterController body;
        public Vector3 normal, forward, stand;
        public int noises, rattles;
    }

    static void Check(bool condition, string text)
    {
        if (condition) report.passed++; else report.failed++;
        report.checks.Add((condition ? "ok   " : "FAIL ") + text);
    }

    [MenuItem("FrontRooms/Map/Test body doors")]
    public static void Run() => Execute(false);
    public static void RunBatch() => Execute(true);

    static void Execute(bool throwOnFail)
    {
        report = new Report();
        roots.Clear(); profiles.Clear();
        var skybox = RenderSettings.skybox;
        var fog = RenderSettings.fog;
        var fogColor = RenderSettings.fogColor;
        var fogDensity = RenderSettings.fogDensity;
        var mode = RenderSettings.ambientMode;
        var sky = RenderSettings.ambientSkyColor;
        var equator = RenderSettings.ambientEquatorColor;
        var ground = RenderSettings.ambientGroundColor;
        var reflection = RenderSettings.reflectionIntensity;
        try
        {
            for (var x = -1; x <= 1; x++)
            for (var y = -1; y <= 1; y++)
            {
                var c = new GridCoord(x, y);
                var n = FrontRoomsMapWorld.BodyDoorReverseHinge(c, c + new GridCoord(0, 1));
                var e = FrontRoomsMapWorld.BodyDoorReverseHinge(c, c + new GridCoord(1, 0));
                var s = FrontRoomsMapWorld.BodyDoorReverseHinge(c + new GridCoord(0, -1), c);
                var w = FrontRoomsMapWorld.BodyDoorReverseHinge(c + new GridCoord(-1, 0), c);
                Check(!(n && e) && !(!n && w) && !(s && !e) && !(!s && !w),
                    "perpendicular body-door jambs avoid every shared corner at " + c);
            }
            var h = Harness();
            if (h != null)
            {
                OffAndButtons(h);
                foreach (var fps in new[] { 60, 30, 20 }) SprintCrossing(h, fps);
                ApproachAndRelease(h);
                SlowRest(h);
                LockedAndDrift(h);
                AlarmCommit(h);
                UnlockCrack(h);
                PullOnly(h);
            }
        }
        catch (Exception e) { Check(false, e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace); }
        finally
        {
            foreach (var root in roots)
            {
                if (root == null) continue;
                var map = root.GetComponent<FrontRoomsMapWorld>();
                if (map != null) map.Release();
                UnityEngine.Object.DestroyImmediate(root);
            }
            foreach (var p in profiles) if (p != null) UnityEngine.Object.DestroyImmediate(p);
            RenderSettings.skybox = skybox; RenderSettings.fog = fog;
            RenderSettings.fogColor = fogColor; RenderSettings.fogDensity = fogDensity;
            RenderSettings.ambientMode = mode; RenderSettings.ambientSkyColor = sky;
            RenderSettings.ambientEquatorColor = equator; RenderSettings.ambientGroundColor = ground;
            RenderSettings.reflectionIntensity = reflection;
            DynamicGI.UpdateEnvironment();
        }
        report.verdict = (report.failed == 0 && report.passed > 0 ? "PASS" : "FAIL") + ": " + report.passed + " passed, " + report.failed + " failed";
        var path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Verification/body-door-tests.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonUtility.ToJson(report, true));
        var message = "[BodyDoorTests] " + report.verdict + "\n" + string.Join("\n", report.checks.ToArray());
        if (report.failed == 0) Debug.Log(message); else Debug.LogError(message);
        if (throwOnFail && report.failed != 0) throw new Exception(report.verdict);
    }

    static H Harness()
    {
        var profile = UnityEngine.Object.Instantiate(FrontRoomsLevelProfiles.Resolve());
        profiles.Add(profile); profile.hideFlags = HideFlags.DontSave;
        profile.doorsNeedKeys = false; profile.dressOffices = false; profile.pileChance = 0f;
        profile.stationModule = profile.stationWaiting = profile.stationDesk = null;
        profile.runRooms = profile.foil = profile.alarmDoor = false;
        profile.buildRadius = 1;
        var root = new GameObject("BODY DOOR TESTS") { hideFlags = HideFlags.DontSave };
        roots.Add(root);
        root.transform.position = new Vector3(-49f * ModuleUnits.WorldPeriod, 0f, -29f * ModuleUnits.WorldPeriod);
        var map = root.AddComponent<FrontRoomsMapWorld>();
        map.Profile = profile; map.BuildForCapture();
        Physics.SyncTransforms();
        FrontRoomsMapWorld.Door door = null;
        Vector3 normal = Vector3.zero;
        var mid = map.CellOf(map.SpawnWorldPosition);
        for (var y = mid.y - 12; y <= mid.y + 12 && door == null; y++)
        for (var x = mid.x - 12; x <= mid.x + 12 && door == null; x++)
        foreach (var step in new[] { new GridCoord(1, 0), new GridCoord(0, 1) })
        {
            var a = new GridCoord(x, y); var b = a + step;
            if (!map.IsBuilt(a) || !map.IsBuilt(b)) continue;
            var d = map.DoorBetween(a, b);
            if (d == null || d.broken || map.IsExitDoor(d)) continue;
            var n = FrontRoomsMapWorld.LockNormal(d, map.CellCenter(a));
            if (!ClearCapsule(d.position + n * 1.15f) || !ClearCapsule(d.position - n * 1.9f)) continue;
            door = d; normal = n; break;
        }
        Check(door != null, "harness finds an actual built door with controller clearance on both sides");
        if (door == null) return null;
        var cc = map.Player.gameObject.AddComponent<CharacterController>();
        cc.height = ModuleUnits.PlayerHeight; cc.radius = ModuleUnits.PlayerRadius;
        cc.center = Vector3.up * ModuleUnits.PlayerHeight * .5f;
        cc.skinWidth = .03f; cc.minMoveDistance = 0f; cc.stepOffset = .3f;
        var h = new H { map = map, door = door, body = cc, normal = normal, forward = -normal };
        map.DoorMoved += p => { if ((p - h.door.position).sqrMagnitude < .01f) h.noises++; };
        map.DoorRattled += (d, p) => { if (d == h.door) h.rattles++; };
        Reset(h);
        return h;
    }

    static bool ClearCapsule(Vector3 feet) => !Physics.CheckCapsule(feet + Vector3.up * .36f,
        feet + Vector3.up * 1.39f, .30f, ~0, QueryTriggerInteraction.Ignore);

    static void Reset(H h, float distance = 1.15f)
    {
        h.map.BodyDoors = true; h.map.DoorsNeedKeysForTools = false;
        h.map.ResetDoorForTools(h.door); h.map.ResetBodyDoorInput();
        h.door.alarm = false;
        h.stand = h.door.position + h.normal * distance;
        h.body.enabled = false; h.body.transform.SetPositionAndRotation(h.stand, Quaternion.LookRotation(h.forward)); h.body.enabled = true;
        Physics.SyncTransforms();
        h.noises = h.rattles = 0;
    }

    static FrontRoomsMapWorld.BodyDoorResult Frame(H h, Vector3 move, float dt,
        FrontRoomsMapWorld.BodyDoorGait gait, bool sprint = false, float since = 100f,
        float tilt = 1f, FrontRoomsMapWorld.Door aim = null, Vector3 point = default, Vector3 view = default)
    {
        var result = h.map.SolveBodyDoors(new FrontRoomsMapWorld.BodyDoorInput
        {
            feet = h.body.transform.position, inputMove = move, dt = dt, gait = gait,
            sprintingNow = sprint, sinceSprint = since, rawTilt = tilt, playerMove = true,
            viewForward = view.sqrMagnitude > .01f ? view : h.forward, aimDoor = aim, aimPoint = point
        });
        if (result.bodyMoved) Physics.SyncTransforms();
        h.body.Move(move * result.moveScale + Vector3.down * dt * .5f);
        h.map.TickDoorsForTools(dt);
        Physics.SyncTransforms();
        return result;
    }

    static void OffAndButtons(H h)
    {
        Reset(h, .42f);
        h.map.BodyDoors = false;
        for (var i = 0; i < 35; i++) Frame(h, h.forward * (5.5f / 60f), 1f / 60f, FrontRoomsMapWorld.BodyDoorGait.Sprint, true, 0f);
        Check(h.door.angle < .5f && h.noises == 0, "flag off: body contact does not open a door or emit a door noise");
        var use = h.map.Use(h.door.leaf);
        Check(use == FrontRoomsMapWorld.UseResult.Opened, "flag off: legacy USE still opens an unlocked door");
        Reset(h, .65f);
        use = h.map.Use(h.door.leaf);
        Check(use == FrontRoomsMapWorld.UseResult.None && h.door.angle < .5f && h.noises == 0,
            "body doors on: USE cannot open an unlocked door");
    }

    static void SprintCrossing(H h, int fps)
    {
        Reset(h);
        var dt = 1f / fps; var start = h.body.transform.position;
        var intended = 0f; var moved = 0f; var leastDoorFrame = 1f;
        var sawBarge = false;
        for (var i = 0; i < Mathf.RoundToInt(.55f * fps); i++)
        {
            var before = h.body.transform.position;
            var result = Frame(h, h.forward * (5.5f * dt), dt, FrontRoomsMapWorld.BodyDoorGait.Sprint, true, 0f);
            var step = Vector3.Dot(h.body.transform.position - before, h.forward);
            intended += 5.5f * dt; moved += step;
            if (Mathf.Abs(Vector3.Dot(before - h.door.position, h.normal)) < .55f) leastDoorFrame = Mathf.Min(leastDoorFrame, step / (5.5f * dt));
            sawBarge |= result.opened && result.verb == FrontRoomsMapWorld.DoorSwingKind.Barge;
        }
        var crossed = Vector3.Dot(h.body.transform.position - h.door.position, h.forward) > .65f;
        Check(sawBarge && h.noises == 1 && crossed, fps + " fps: real capsule barges through; opened=" + sawBarge + ", noises=" + h.noises + ", crossed=" + crossed);
        Check(moved >= intended - .06f && leastDoorFrame >= .85f, fps + " fps: no door stall; travel=" + moved.ToString("0.000")
            + "/" + intended.ToString("0.000") + " m; least near-leaf step=" + leastDoorFrame.ToString("0.000"));
    }

    static void SlowRest(H h)
    {
        Reset(h, .40f);
        for (var i = 0; i < 29; i++) Frame(h, h.forward * (1.44f / 60f), 1f / 60f, FrontRoomsMapWorld.BodyDoorGait.Slow, false, 100f, .35f);
        var angle = h.door.angle;
        for (var i = 0; i < 60; i++) Frame(h, Vector3.zero, 1f / 60f, FrontRoomsMapWorld.BodyDoorGait.Slow, false, 100f, 0f);
        Check(angle >= 3f && angle < FrontRoomsMapWorld.DoorPassableDegrees, "slow squeeze leaves a partial opening: " + angle.ToString("0.0") + " degrees");
        Check(Mathf.Abs(h.door.angle - angle) <= 5.01f && h.map.IsAjar(h.door), "2A: stopped leaf keeps its partial angle, not an automatic full swing");
        Check(h.noises == 1, "a slow contact episode emits exactly one door noise, not one every frame (" + h.noises + ")");
        Check(h.map.Use(h.door.leaf) == FrontRoomsMapWorld.UseResult.None, "1C: USE cannot close a player-left partial door");
    }

    static void ApproachAndRelease(H h)
    {
        Reset(h, .39f);
        var diagonal = Quaternion.AngleAxis(59f, Vector3.up) * h.forward;
        var accepted = Frame(h, diagonal * (5.5f / 60f), 1f / 60f,
            FrontRoomsMapWorld.BodyDoorGait.Sprint, true, 0f, 1f, null, default, diagonal);
        Check(accepted.opened && accepted.verb == FrontRoomsMapWorld.DoorSwingKind.Barge,
            "9A: a full sprint approaching 59 degrees from the leaf normal barges");
        Reset(h, .39f);
        diagonal = Quaternion.AngleAxis(61f, Vector3.up) * h.forward;
        var rejected = Frame(h, diagonal * (5.5f / 60f), 1f / 60f,
            FrontRoomsMapWorld.BodyDoorGait.Sprint, true, 0f, 1f, null, default, diagonal);
        Check(!rejected.opened && h.noises == 0, "9A: a 61-degree grazing contact does not barge");
        Reset(h, .39f);
        var grace = Frame(h, h.forward * (3.2f / 60f), 1f / 60f,
            FrontRoomsMapWorld.BodyDoorGait.Walk, false, .14f);
        Check(grace.opened && grace.verb == FrontRoomsMapWorld.DoorSwingKind.Barge,
            "9A: letting go of sprint 0.14 seconds before full forward walk contact keeps the barge");
        Reset(h, .39f);
        var expired = Frame(h, h.forward * (3.2f / 60f), 1f / 60f,
            FrontRoomsMapWorld.BodyDoorGait.Walk, false, .16f);
        Check(!expired.opened && h.noises == 0, "9A: after 0.16 seconds, contact starts the walk lean rather than a barge");
    }

    static void LockedAndDrift(H h)
    {
        Reset(h, .39f); h.map.DoorsNeedKeysForTools = true;
        for (var i = 0; i < 40; i++) Frame(h, h.forward * (1.44f / 60f), 1f / 60f, FrontRoomsMapWorld.BodyDoorGait.Slow, false, 100f, .35f);
        Check(h.noises == 0 && h.rattles == 0 && h.door.angle < .5f, "4A: slow locked-door contact remains silent and latched");
        Reset(h, .39f); h.map.DoorsNeedKeysForTools = true;
        for (var i = 0; i < 40; i++) Frame(h, h.forward * (3.2f / 60f), 1f / 60f, FrontRoomsMapWorld.BodyDoorGait.Walk);
        Check(h.door.angle < .5f && h.rattles == 1, "walk locked-door contact gives only one rattle per episode (" + h.rattles + ")");
        Reset(h, .39f);
        for (var i = 0; i < 40; i++) Frame(h, h.forward * (.2f / 60f), 1f / 60f, FrontRoomsMapWorld.BodyDoorGait.Slow, false, 100f, .1f);
        Check(h.door.angle < .5f && h.noises == 0, "pad drift below the deliberate-input threshold does not unlatch a door");
    }

    static void AlarmCommit(H h)
    {
        Reset(h, .39f); h.map.MarkAlarmDoorForTools(h.door, true);
        for (var i = 0; i < 35; i++) Frame(h, h.forward * (5.5f / 60f), 1f / 60f, FrontRoomsMapWorld.BodyDoorGait.Sprint, true, 0f);
        Check(h.door.angle < .5f && !h.map.IsAlarmTripped(h.door.edge), "10A: sprinting into an alarm bar does not open or trigger it");
        Reset(h, .39f); h.map.MarkAlarmDoorForTools(h.door, true);
        for (var i = 0; i < 20; i++) Frame(h, h.forward * (3.2f / 60f), 1f / 60f, FrontRoomsMapWorld.BodyDoorGait.Walk);
        Check(h.door.angle < .5f && !h.map.IsAlarmTripped(h.door.edge), "alarm bar has not committed before 0.4 seconds");
        for (var i = 0; i < 20; i++) Frame(h, h.forward * (3.2f / 60f), 1f / 60f, FrontRoomsMapWorld.BodyDoorGait.Walk);
        Check(h.door.angle > 3f && h.map.IsAlarmTripped(h.door.edge), "front-facing full walk sustained beyond 0.4 seconds opens and triggers alarm bar");
    }

    static void UnlockCrack(H h)
    {
        Reset(h, 1.15f); h.map.DoorsNeedKeysForTools = true;
        h.map.GiveKeyForTools(h.map.ZoneOf(h.map.CellOf(h.body.transform.position)).id);
        var camera = new GameObject("body unlock camera").AddComponent<Camera>();
        camera.transform.SetParent(h.map.transform, false); camera.fieldOfView = 76f;
        var rig = FrontRoomsCameraRig.Attach(h.body.transform, camera, ModuleUnits.PlayerEye);
        var shot = new FrontRoomsDoorShot(rig, h.map, camera, h.body.transform);
        h.map.KeyTurnOwner = shot;
        var use = h.map.Use(h.door.leaf);
        var feet = h.body.transform.position;
        if (use == FrontRoomsMapWorld.UseResult.KeyTurn)
        {
            var pose = h.map.LockPose(h.door, feet);
            var normal = FrontRoomsMapWorld.LockNormal(h.door, feet);
            shot.BeginUnlock(h.door, FrontRoomsMapWorld.FaceOf(h.door, feet), pose, normal, false, true);
            for (var i = 0; i < 150; i++) { shot.Tick(1f / 60f, default(FrontRoomsRelayThreat)); h.map.TickDoorsForTools(1f / 60f); rig.Tick(1f / 60f); }
        }
        Check(use == FrontRoomsMapWorld.UseResult.KeyTurn && h.map.IsUnlockedForTools(h.door) && !shot.Live,
            "11B: real unlock shot commits its key and completes");
        Check(Mathf.Abs(h.door.angle - 10f) < .6f && h.map.IsAjar(h.door), "11B: unlock stops at 10 degrees before the player's body pushes (" + h.door.angle.ToString("0.0") + ")");
        Check((h.body.transform.position - feet).sqrMagnitude < .0001f, "unlock shot does not move the controller through the unopened leaf");
        Check(h.noises == 0, "11B: the 10-degree unlock crack emits no door movement noise");
        shot.Abort(); h.map.KeyTurnOwner = null; rig.ResetLayers();
        UnityEngine.Object.DestroyImmediate(camera.gameObject);
        for (var i = 0; i < 45; i++) Frame(h, h.forward * (3.2f / 60f), 1f / 60f, FrontRoomsMapWorld.BodyDoorGait.Walk);
        Check(h.door.angle >= FrontRoomsMapWorld.DoorPassableDegrees && h.noises == 1,
            "11B: the first body push opens the unlock crack and emits exactly one movement event");
    }

    static void PullOnly(H h)
    {
        Reset(h);
        // A real body opens it and crosses; this also establishes the two-second sprint-crossing record.
        for (var i = 0; i < 26; i++) Frame(h, h.forward * (5.5f / 60f), 1f / 60f, FrontRoomsMapWorld.BodyDoorGait.Sprint, true, 0f);
        Check(h.map.Use(h.door.leaf) == FrontRoomsMapWorld.UseResult.None, "1C: USE cannot shut even a fully open door after sprinting through it");
        var shut = false;
        for (var i = 0; i < 70 && !shut; i++)
        {
            var handle = h.door.hinge.TransformPoint(new Vector3(0f, ModuleUnits.DoorHandleHeight, .92f));
            var view = handle - (h.body.transform.position + Vector3.up * ModuleUnits.PlayerEye);
            view.y = 0f; view.Normalize();
            var move = i < 21 ? Vector3.zero : -view * (3.2f / 60f);
            var result = Frame(h, move, 1f / 60f, FrontRoomsMapWorld.BodyDoorGait.Walk, false, .5f, 1f, h.door, handle, view);
            shut |= result.shut;
        }
        Check(shut, "1C: latch-half aim dwell plus a genuine backward controller step pulls the door shut");
        for (var i = 0; i < 60; i++) Frame(h, Vector3.zero, 1f / 60f, FrontRoomsMapWorld.BodyDoorGait.Walk, false, 2f, 0f);
        Check(h.door.swingKind == FrontRoomsMapWorld.DoorSwingKind.Slam, "pull within the sprint-crossing window is a slam");
    }
}
