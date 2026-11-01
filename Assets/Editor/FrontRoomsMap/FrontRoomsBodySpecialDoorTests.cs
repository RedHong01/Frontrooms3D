using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using FrontRooms.Map;

// Motion and readiness checks for the shared special-door contact solver.
// Runs the same world-space inputs used by RoomStream's two leaves and the north door.
public static class FrontRoomsBodySpecialDoorTests
{
    [Serializable] sealed class Report { public int passed, failed; public List<string> checks = new List<string>(); }
    static Report report;
    static void Check(bool ok, string text) { if (ok) report.passed++; else report.failed++; report.checks.Add((ok ? "ok " : "FAIL ") + text); }
    static FrontRoomsBodyDoorContact.Verb Step(FrontRoomsBodyDoorContact.Leaf leaf, Vector3 feet, Vector3 delta,
        bool ready = true, bool sprint = false, bool slow = false, Vector3 view = default, bool sealedShut = false)
        => FrontRoomsBodyDoorContact.Tick(leaf, Vector3.zero, Vector3.right, 1.3f, 95f, feet, delta,
            view.sqrMagnitude > .01f ? view : Vector3.forward, 1f / 60f, sprint, slow, sprint ? 0f : 1000f, ready, sealedShut);

    public static void RunBatch()
    {
        report = new Report();
        var leaf = new FrontRoomsBodyDoorContact.Leaf();
        var feet = new Vector3(.9f, 0f, -4f);
        for (var i = 0; i < 120; i++) Step(leaf, feet, Vector3.zero);
        Check(leaf.angle == 0f, "standing four metres from a ready door never opens it");
        feet.z = -.39f;
        for (var i = 0; i < 60; i++) Step(leaf, feet, Vector3.forward * (5.5f / 60f), false, true);
        Check(leaf.angle == 0f, "loading/readiness hold refuses even a sprint contact");
        Step(leaf, feet, Vector3.zero);
        Check(leaf.angle == 0f, "readiness alone is not an open command");
        var burst = Step(leaf, feet, Vector3.forward * (5.5f / 60f), true, true);
        Check(burst == FrontRoomsBodyDoorContact.Verb.Barge && Mathf.Abs(leaf.angle) >= 3f,
            "ready door releases on an actual sprint contact");
        for (var i = 0; i < 30; i++) Step(leaf, new Vector3(3f, 0f, 3f), Vector3.zero);
        Check(Mathf.Abs(leaf.angle) > 90f, "released full-speed leaf reaches its stop without another input action");

        leaf = new FrontRoomsBodyDoorContact.Leaf();
        for (var i = 0; i < 17; i++) Step(leaf, feet, Vector3.forward * (1.44f / 60f), true, false, true);
        Check(leaf.angle == 0f, "slow contact has not opened before the deliberate lean finishes");
        for (var i = 0; i < 8; i++) Step(leaf, feet, Vector3.forward * (1.44f / 60f), true, false, true);
        var rest = leaf.angle;
        for (var i = 0; i < 60; i++) Step(leaf, feet, Vector3.zero);
        Check(Mathf.Abs(rest) >= 3f && Mathf.Abs(rest) < 70f, "slow contact creates a useful partial crack");
        Check(Mathf.Abs(rest - leaf.angle) < .01f, "stopping the body keeps the special leaf at its actual partial angle");

        // A player-side latch-half look, then a backwards step. No key press is supplied.
        leaf = new FrontRoomsBodyDoorContact.Leaf { angle = -85f };
        var direction = FrontRoomsBodyDoorContact.Direction(Vector3.right, leaf.angle);
        var handle = direction * 1.105f;
        var normal = Vector3.Cross(Vector3.up, direction);
        feet = handle + normal * .65f;
        var look = -normal;
        var shut = false;
        for (var i = 0; i < 20; i++) Step(leaf, feet, Vector3.zero, true, false, false, look);
        for (var i = 0; i < 10 && !shut; i++)
        {
            var delta = -look * (3.2f / 60f);
            shut = Step(leaf, feet, delta, true, false, false, look) == FrontRoomsBodyDoorContact.Verb.Shut;
            feet += delta;
        }
        Check(shut, "looking at the latch then stepping back pulls a special door shut without a button");
        for (var i = 0; i < 60; i++) Step(leaf, new Vector3(3f, 0f, 3f), Vector3.zero);
        Check(Mathf.Abs(leaf.angle) < .01f, "body pull reaches the latch when the capsule is clear");
        for (var i = 0; i < 30; i++) Step(leaf, new Vector3(.9f, 0f, -.39f), Vector3.forward * .1f, true, true, false, default, true);
        Check(Mathf.Abs(leaf.angle) < .01f, "sealed terminal leaf cannot reopen on contact");

        try { StreamPairIntegration(); } catch (Exception e) { Check(false, "real stream pair: " + e); }
        try { StairwellIntegration(); } catch (Exception e) { Check(false, "real stairwell: " + e); }

        var path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Verification/body-special-door-tests.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonUtility.ToJson(report, true));
        var text = "[BodySpecialDoorTests] " + report.passed + " passed, " + report.failed + " failed\n" + string.Join("\n", report.checks);
        if (report.failed == 0) Debug.Log(text); else { Debug.LogError(text); throw new Exception(text); }
    }

    static void StreamPairIntegration()
    {
        var root = new GameObject("BODY DOOR TEST / real stream pair");
        var eyeRoot = new GameObject("BODY DOOR TEST / stream eye");
        var material = FrontRoomsSurfaces.Lit("Body door test material", Color.white, .1f);
        try
        {
            var eye = eyeRoot.AddComponent<Camera>();
            eye.transform.position = new Vector3(10000f, ModuleUnits.PlayerEye, 0f);
            var stream = root.AddComponent<FrontRoomsRoomStream>();
            stream.BodyDoors = true;
            stream.Initialize(eye, material, material, material, material, material, material);
            var sequence = stream.FirstClosedDoorSequence();
            Check(sequence >= 0, "real stream has a terminal pair for the body adapter");
            if (sequence < 0) return;
            stream.EndStreamAt(sequence, FrontRoomsRoomStream.RoomWidth * .5f);
            var endZ = stream.RoomStartZ(sequence) + FrontRoomsRoomStream.RoomLength;
            var opened = 0;
            stream.DoorOpeningStarted += (number, point) => { if (number == sequence) opened++; };
            eye.transform.position = new Vector3(stream.CenterX, ModuleUnits.PlayerEye, endZ - 4f);
            for (var n = 0; n < 120; n++)
            {
                stream.TickBodyDoors(eye.transform.position, Vector3.zero, Vector3.forward, 1f / 60f, false, false, 1000f);
                stream.Tick(1f / 60f);
            }
            Check(!stream.TerminalDoorOpen && opened == 0, "real ready terminal pair stays shut at four metres; proximity never opens it");
            eye.transform.position = new Vector3(stream.CenterX, ModuleUnits.PlayerEye, endZ - .36f);
            stream.TerminalDoorHeld = true;
            var held = stream.TickBodyDoors(eye.transform.position, Vector3.forward * .1f, Vector3.forward, 1f / 60f, true, false, 0f);
            Check(held && !stream.TerminalDoorOpen, "real terminal loading hold reports blocked contact without opening");
            stream.TerminalDoorHeld = false;
            for (var n = 0; n < 80; n++)
            {
                var delta = Vector3.forward * (3.2f / 60f);
                stream.TickBodyDoors(eye.transform.position, delta, Vector3.forward, 1f / 60f, false, false, 1000f);
                stream.Move(new Vector2(delta.x, delta.z));
                stream.Tick(1f / 60f);
            }
            Check(stream.TerminalDoorOpen && opened > 0 && eye.transform.position.z > endZ + .6f,
                "real stream movement passes the body-open terminal pair (travel " + (eye.transform.position.z - endZ).ToString("0.00") + " m)");
            stream.CloseTerminalDoor();
            for (var n = 0; n < 90; n++)
            {
                stream.TickBodyDoors(eye.transform.position, Vector3.zero, -Vector3.forward, 1f / 60f, false, false, 1000f);
                stream.Tick(1f / 60f);
            }
            Check(stream.TerminalDoorShut, "real terminal pair completes its scripted seal on zero-move frames");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(eyeRoot);
            UnityEngine.Object.DestroyImmediate(material);
        }
    }

    static void StairwellIntegration()
    {
        var root = new GameObject("BODY DOOR TEST / real stairwell");
        var bodyRoot = new GameObject("BODY DOOR TEST / stairwell capsule");
        FrontRoomsMapWorld map = null;
        var profile = UnityEngine.Object.Instantiate(FrontRoomsLevelProfiles.Resolve());
        try
        {
            profile.dressOffices = false;
            profile.stationModule = profile.stationWaiting = profile.stationDesk = null;
            profile.runRooms = profile.foil = profile.alarmDoor = false;
            map = FrontRoomsMapWorld.CreateEmbedded(root.transform, profile, 2554);
            var cell = new GridCoord(21, 19);
            map.NextFloor(2554, 2, new RectInt(cell.x, cell.y, 1, 1), new GridCoord(cell.x, cell.y + 1));
            map.BuildForCapture();
            var stairs = FrontRoomsStairwell.Create(map, cell, root.transform);
            stairs.BodyDoors = true;
            Check(stairs.JoinNextFloor() && stairs.Joined, "real body stairwell joins its next floor");
            var direction = stairs.transform.forward;
            var middle = stairs.NorthHinge.position + stairs.transform.right * (FrontRoomsStairwell.NorthOpeningWidth * .5f);
            var feet = middle - direction * .36f;
            var held = stairs.TickBodyDoor(feet, direction * .1f, direction, 1f / 60f, true, false, 0f);
            Check(held && stairs.NorthDoorAngle < .01f, "unready real north door reports blocked body contact");
            stairs.Open(feet);
            for (var n = 0; n < 120; n++)
                stairs.TickBodyDoor(middle - direction * 4f, Vector3.zero, direction, 1f / 60f, false, false, 1000f);
            Check(stairs.NorthDoorReady && stairs.NorthDoorAngle < .01f, "real north-door readiness never opens it without contact");
            var body = bodyRoot.AddComponent<CharacterController>();
            body.height = ModuleUnits.PlayerHeight;
            body.radius = ModuleUnits.PlayerRadius;
            body.center = Vector3.up * ModuleUnits.PlayerHeight * .5f;
            body.skinWidth = .03f;
            body.minMoveDistance = 0f;
            bodyRoot.transform.position = middle - direction * .8f;
            Physics.SyncTransforms();
            for (var n = 0; n < 60; n++)
            {
                var delta = direction * (3.2f / 60f);
                stairs.TickBodyDoor(bodyRoot.transform.position, delta, direction, 1f / 60f, false, false, 1000f);
                body.Move(delta);
            }
            var travelledPast = Vector3.Dot(bodyRoot.transform.position - middle, direction);
            Check(stairs.NorthDoorAngle > 70f && travelledPast > .6f,
                "real CharacterController pushes through the real north leaf (past " + travelledPast.ToString("0.00") + " m, angle " + stairs.NorthDoorAngle.ToString("0.0") + ")");
            stairs.Close();
            for (var n = 0; n < 90; n++) stairs.TickForTools(1f / 60f);
            var sealedAngle = stairs.NorthDoorAngle;
            held = stairs.TickBodyDoor(middle - direction * .36f, direction * .1f, direction, 1f / 60f, true, false, 0f);
            Check(stairs.NorthDoorLocked && sealedAngle < .01f && held && stairs.NorthDoorAngle < .01f,
                "real north door script seals permanently and still reports blocked contact");
        }
        finally
        {
            if (map != null) map.Release();
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(bodyRoot);
            UnityEngine.Object.DestroyImmediate(profile);
        }
    }

}
