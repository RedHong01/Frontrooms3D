using FrontRooms.Map;
using UnityEngine;

// Red 2026-10-31: body opens; turn, aim and step back to pull shut. No door button.
public sealed partial class FrontRooms3DGame
{
    public static bool BodyDoorsActive { get; private set; }
    float bodySinceSprint = 1000f;
    float bodyBumpUntil;
    string bodyBumpPrompt;

    public static bool BodyDoorsCompiled
    {
        get
        {
#if FRONTROOMS_RELAY_V2
            return true;
#else
            return false;
#endif
        }
    }

    void ConfigureBodyDoors(FrontRoomsLevelProfile profile)
    {
        var requested = profile != null && profile.bodyDoors;
#if UNITY_EDITOR
        AutoBodyDoorSetup(System.Environment.GetCommandLineArgs());
        requested |= AutoBodyDoorsRequested;
#endif
        map.BodyDoors = BodyDoorsCompiled && requested;
        BodyDoorsActive = map.BodyDoors;
        bodySinceSprint = 1000f;
        if (roomStream != null) roomStream.BodyDoors = map.BodyDoors;
        map.DoorLatchBurst += d =>
        {
            Jolt(d.position, DoorJoltRange, .45f, .16f);
            FrontRoomsMobileHaptics.Play(FrontRoomsMobileHaptics.Channel.Gameplay, FrontRoomsMobileHaptics.Style.Heavy, .65f);
        };
        map.DoorBodyBlocked += d => BodyDoorImpact(d, .4f);
        map.DoorSlammedInto += d => BodyDoorImpact(d, .6f);
        map.DoorHeldAgainstRelay += d => relay?.BodyDoorHeld(d);
        map.DoorThrowStoppedByPlayer += d => relay?.BodyDoorBrokeAgainstPlayer(d);
        if (requested && !BodyDoorsCompiled) Log("BODY DOORS refused: Relay v2 is not compiled; current doors retained");
        if (map.BodyDoors) Log("BODY DOORS enabled for this run; pull only; no USE open/shut; unlock crack 10 degrees");
    }

    void BodyDoorImpact(FrontRoomsMapWorld.Door door, float degrees)
    {
        if (door == null) return;
        Jolt(door.position, DoorJoltRange, degrees, .16f);
        FrontRoomsMobileHaptics.Play(FrontRoomsMobileHaptics.Channel.Gameplay,
            FrontRoomsMobileHaptics.Style.Heavy, .8f);
    }

    float BodyDoorMove(float dt, Vector3 wish, bool sprinting, bool slow, bool playerMove, float rawTilt, out bool blocked)
    {
        blocked = false;
        if (map == null || !map.BodyDoors) return 1f;
        var canMove = phase == Phase.Playing && climbTime < 0f && floorSwap == FloorSwap.None
            && !pullClear.HasValue && (rig == null || !rig.MoveLocked)
            && (glassShot == null || !glassShot.MoveLocked) && (doorShot == null || !doorShot.MoveLocked);
#if UNITY_EDITOR
        if (autopilot && !AutoBodyDoorsRequested) canMove = false;
#endif
        if (!canMove)
        {
            map.ResetBodyDoorInput();
            if (roomStream != null) roomStream.TickBodyDoors(playerRoot.position, Vector3.zero, playerRoot.forward, dt, false, false, 1000f);
            if (startStairwell != null) startStairwell.TickBodyDoor(playerRoot.position, Vector3.zero, playerRoot.forward, dt, false, false, 1000f);
            return 1f;
        }
        bodySinceSprint = sprinting ? 0f : Mathf.Min(1000f, bodySinceSprint + dt);
        var move = wish * (sprinting ? Run : Walk) * dt;
        var deliberateMove = rawTilt >= .2f ? move : Vector3.zero;
        var eye = rig != null ? rig.BaseEye : new Pose(cam.transform.position, cam.transform.rotation);
        FrontRoomsMapWorld.Door aimDoor = null;
        var aimPoint = Vector3.zero;
        if (AimCast(new Ray(eye.position, eye.rotation * Vector3.forward), out var hit))
        {
            aimDoor = map.DoorOf(hit.collider);
            aimPoint = hit.point;
        }
        var gait = sprinting ? FrontRoomsMapWorld.BodyDoorGait.Sprint : slow || wish.magnitude < .7f
            ? FrontRoomsMapWorld.BodyDoorGait.Slow : FrontRoomsMapWorld.BodyDoorGait.Walk;
        var answer = map.SolveBodyDoors(new FrontRoomsMapWorld.BodyDoorInput
        {
            feet = playerRoot.position, inputMove = move, viewForward = eye.rotation * Vector3.forward,
            dt = dt, gait = gait, sprintingNow = sprinting, sinceSprint = bodySinceSprint,
            rawTilt = rawTilt, playerMove = true, aimDoor = aimDoor, aimPoint = aimPoint,
            peekDoor = inspectAimed != null && inspectAimed.Kind == InspectKind.Peek ? aimDoor : null
        });
        blocked = answer.blocked;
        switch (answer.bumpKind)
        {
            case FrontRoomsMapWorld.BodyDoorBump.LockedSilent:
                FrontRoomsMobileHaptics.Play(FrontRoomsMobileHaptics.Channel.Gameplay, FrontRoomsMobileHaptics.Style.Light, .3f);
                BodyDoorBump("LOCKED"); break;
            case FrontRoomsMapWorld.BodyDoorBump.Locked:
                keyHud?.NoKey(); BodyDoorBump("LOCKED"); break;
            case FrontRoomsMapWorld.BodyDoorBump.HaveKey: BodyDoorBump("E  ·  UNLOCK"); break;
            case FrontRoomsMapWorld.BodyDoorBump.Broken: keyHud?.BrokenTry(); BodyDoorBump("BROKEN"); break;
            case FrontRoomsMapWorld.BodyDoorBump.Alarmed: BodyDoorBump("ALARMED"); break;
            case FrontRoomsMapWorld.BodyDoorBump.Sealed: BodyDoorBump("SEALED"); break;
            case FrontRoomsMapWorld.BodyDoorBump.Held: BodyDoorBump("BLOCKED"); break;
        }
        if (roomStream != null)
            blocked |= roomStream.TickBodyDoors(playerRoot.position, deliberateMove, eye.rotation * Vector3.forward, dt, sprinting, gait == FrontRoomsMapWorld.BodyDoorGait.Slow, bodySinceSprint);
        if (startStairwell != null)
        {
            startStairwell.BodyDoors = true;
            blocked |= startStairwell.TickBodyDoor(playerRoot.position, deliberateMove, eye.rotation * Vector3.forward, dt, sprinting, gait == FrontRoomsMapWorld.BodyDoorGait.Slow, bodySinceSprint);
        }
        return answer.moveScale;
    }

    void BodyDoorBump(string value) { bodyBumpPrompt = value; bodyBumpUntil = elapsed + 1f; }

    // Returns true for a body-only target. USE remains available for keys, locks and a vision-lite peek.
    bool BodyDoorAim(Collider collider)
    {
        if (map == null || !map.BodyDoors) return false;
        var door = map.DoorOf(collider);
        if (door == null || door.broken) return false;
        var kind = map.Kind(collider, playerRoot.position, out _, out _);
        if (kind != FrontRoomsMapWorld.AimKind.Open && kind != FrontRoomsMapWorld.AimKind.Shut
            && kind != FrontRoomsMapWorld.AimKind.AlarmDoor) return false;
        aimed = collider;
        aimKind = FrontRoomsMapWorld.AimKind.None;
        prompt = FrontRoomsLanguage.T(kind == FrontRoomsMapWorld.AimKind.AlarmDoor ? "ALARMED"
            : map.BodyDoorPullPrompt(door) ?? (door.angle < 3f ? "WALK INTO IT" : ""));
        aimedHold = false;
        return true;
    }
}
