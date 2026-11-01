using UnityEngine;

public sealed partial class FrontRoomsRoomStream
{
    /// <summary>Off preserves title/stream proximity doors. In play, the terminal pair is opened and shut by contact.</summary>
    public bool BodyDoors { get; set; }
    readonly FrontRoomsBodyDoorContact.Leaf bodyLeft = new FrontRoomsBodyDoorContact.Leaf();
    readonly FrontRoomsBodyDoorContact.Leaf bodyRight = new FrontRoomsBodyDoorContact.Leaf();
    bool bodyPairReady;
    int bodyPairSequence = int.MinValue;

    /// <summary>Call once before the player's move, including zero-move frames so a released leaf can settle.</summary>
    public bool TickBodyDoors(Vector3 feet, Vector3 inputMove, Vector3 viewForward, float dt,
        bool sprinting, bool slow, float sinceSprint)
    {
        if (!BodyDoors || !initialized || !frozen || !hasControl || dt <= 0f) return false;
        var room = FindSequence(terminalSequence);
        if (room == null || room.leftDoor == null || room.rightDoor == null || room.doorBroken) return false;
        PrepareBodyPair(room);
        if (Mathf.Abs(bodyLeft.angle) < .01f && Mathf.Abs(bodyRight.angle) < .01f)
        {
            room.doorSwing = feet.z <= room.endZ ? 1f : -1f;
            SetHingeSide(room, room.doorSwing < 0f ? -1 : 1);
        }
        var oldLeft = bodyLeft.angle;
        var oldRight = bodyRight.angle;
        var leftHinge = room.leftDoor.position;
        var rightHinge = room.rightDoor.position;
        var closedDirection = room.root.transform.right;
        var ready = !TerminalDoorHeld && !terminalLocked;
        var leftVerb = FrontRoomsBodyDoorContact.Tick(bodyLeft, leftHinge, closedDirection, DoorWidth * .5f, 88f,
            feet, inputMove, viewForward, dt, sprinting, slow, sinceSprint, ready, terminalLocked);
        var rightVerb = FrontRoomsBodyDoorContact.Tick(bodyRight, rightHinge, -closedDirection, DoorWidth * .5f, 88f,
            feet, inputMove, viewForward, dt, sprinting, slow, sinceSprint, ready, terminalLocked);
        if (BodyVerbStarts(leftVerb) || BodyVerbStarts(rightVerb))
        {
            room.doorSoundPlayed = false;
            // Use the existing event/Foley route once for the pair, even when a centre push touches both leaves.
            BeginDoorOpening(room);
            room.doorOpening = false;
            if (terminalSeal != null) terminalSeal.SetActive(false);
        }
        if (leftVerb == FrontRoomsBodyDoorContact.Verb.Latched && rightVerb == FrontRoomsBodyDoorContact.Verb.Latched)
            room.doorSoundPlayed = false;
        room.doorProgress = Mathf.Clamp01(Mathf.Max(Mathf.Abs(bodyLeft.angle), Mathf.Abs(bodyRight.angle)) / 88f);
        room.doorOpen = Mathf.Abs(bodyLeft.angle) >= 70f || Mathf.Abs(bodyRight.angle) >= 70f;
        room.doorOpening = bodyLeft.moving && !bodyLeft.shutting || bodyRight.moving && !bodyRight.shutting;
        if (terminalLocked && !bodyLeft.moving && !bodyRight.moving)
            room.doorClosing = false;
        ApplyBodyPairPose(room);
        if (Mathf.Abs(oldLeft - bodyLeft.angle) > .0001f || Mathf.Abs(oldRight - bodyRight.angle) > .0001f) Physics.SyncTransforms();
        return FrontRoomsBodyDoorContact.BlocksMove(leftHinge, closedDirection, bodyLeft.angle, DoorWidth * .5f, feet, inputMove)
            || FrontRoomsBodyDoorContact.BlocksMove(rightHinge, -closedDirection, bodyRight.angle, DoorWidth * .5f, feet, inputMove);
    }

    static bool BodyVerbStarts(FrontRoomsBodyDoorContact.Verb verb) => verb == FrontRoomsBodyDoorContact.Verb.Ease ||
        verb == FrontRoomsBodyDoorContact.Verb.Barge || verb == FrontRoomsBodyDoorContact.Verb.Shut || verb == FrontRoomsBodyDoorContact.Verb.Slam;

    void ResolveBodyPairOverlap(RoomSlot room, ref Vector3 candidate)
    {
        ResolveBodyLeaf(room.leftDoor.position, room.root.transform.right, bodyLeft.angle, ref candidate);
        ResolveBodyLeaf(room.rightDoor.position, -room.root.transform.right, bodyRight.angle, ref candidate);
    }

    static void ResolveBodyLeaf(Vector3 hinge, Vector3 closedDirection, float angle, ref Vector3 candidate)
    {
        var direction = FrontRoomsBodyDoorContact.Direction(closedDirection, angle);
        var offset = candidate - hinge;
        offset.y = 0f;
        var nearest = hinge + direction * Mathf.Clamp(Vector3.Dot(offset, direction), 0f, DoorWidth * .5f);
        var away = candidate - nearest;
        away.y = 0f;
        const float radius = .34f;
        if (away.sqrMagnitude >= radius * radius) return;
        if (away.sqrMagnitude < .000001f) away = Vector3.Cross(Vector3.up, direction);
        var corrected = nearest + away.normalized * radius;
        candidate.x = corrected.x;
        candidate.z = corrected.z;
    }

    void PrepareBodyPair(RoomSlot room)
    {
        if (bodyPairReady && bodyPairSequence == room.sequence) return;
        bodyPairSequence = room.sequence;
        bodyPairReady = true;
        // Preserve a pair already in a scripted title swing at the handoff.
        var u = Mathf.Clamp01(room.doorProgress);
        var angle = 88f * u * u * (3f - 2f * u) * room.doorSwing;
        bodyLeft.angle = -angle;
        bodyRight.angle = angle;
        bodyLeft.moving = bodyRight.moving = false;
        bodyLeft.shutting = bodyRight.shutting = false;
        room.doorOpening = false;
    }

    void BeginBodyTerminalSeal(RoomSlot room)
    {
        PrepareBodyPair(room);
        FrontRoomsBodyDoorContact.Start(bodyLeft, 0f, DoorOpenSeconds, true);
        FrontRoomsBodyDoorContact.Start(bodyRight, 0f, DoorOpenSeconds, true);
    }

    void ApplyBodyPairPose(RoomSlot room)
    {
        if (room.leftDoor == null || room.rightDoor == null) return;
        // A barrel-face change is allowed only with both leaves shut; no hinge jumps on a partial rest.
        if (Mathf.Abs(bodyLeft.angle) < .01f && Mathf.Abs(bodyRight.angle) < .01f)
            SetHingeSide(room, room.doorSwing < 0f ? -1 : 1);
        room.leftDoor.localRotation = Quaternion.Euler(0f, bodyLeft.angle, 0f);
        room.rightDoor.localRotation = Quaternion.Euler(0f, bodyRight.angle, 0f);
    }
}
