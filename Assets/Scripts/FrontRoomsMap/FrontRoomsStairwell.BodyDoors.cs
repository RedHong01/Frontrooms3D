using UnityEngine;

public sealed partial class FrontRoomsStairwell
{
    /// <summary>True body contact replaces the scripted north-door open. Off is the old stairwell.</summary>
    public bool BodyDoors { get; set; }
    /// <summary>The next floor is ready; contact may now release the latch. Readiness itself never opens it.</summary>
    public bool NorthDoorReady { get; set; }
    readonly FrontRoomsBodyDoorContact.Leaf bodyNorth = new FrontRoomsBodyDoorContact.Leaf();
    bool bodyNorthReady;

    public bool TickBodyDoor(Vector3 feet, Vector3 inputMove, Vector3 viewForward, float dt,
        bool sprinting, bool slow, float sinceSprint)
    {
        if (!BodyDoors || northHinge == null || map != null && map.DoorsPaused) return false;
        if (phase == DoorPhase.Locked || phase == DoorPhase.Shutting)
            return FrontRoomsBodyDoorContact.BlocksMove(northHinge.position, transform.right, -angle * swing,
                NorthOpeningWidth, feet, inputMove);
        if (!bodyNorthReady)
        {
            bodyNorth.angle = -angle * swing;
            bodyNorthReady = true;
        }
        var oldAngle = bodyNorth.angle;
        var verb = FrontRoomsBodyDoorContact.Tick(bodyNorth, northHinge.position, transform.right, NorthOpeningWidth,
            OpenDegrees, feet, inputMove, viewForward, dt, sprinting, slow, sinceSprint, NorthDoorReady && Joined);
        if (verb == FrontRoomsBodyDoorContact.Verb.Ease || verb == FrontRoomsBodyDoorContact.Verb.Barge ||
            verb == FrontRoomsBodyDoorContact.Verb.Shut || verb == FrontRoomsBodyDoorContact.Verb.Slam)
            NorthDoorMoved?.Invoke(NorthDoorPoint);
        angle = Mathf.Abs(bodyNorth.angle);
        if (angle > .001f) swing = -Mathf.Sign(bodyNorth.angle);
        phase = bodyNorth.shutting ? DoorPhase.BodyShutting : bodyNorth.moving ? DoorPhase.Swing : angle > 0f ? DoorPhase.Open : DoorPhase.Shut;
        if (verb == FrontRoomsBodyDoorContact.Verb.Latched)
        {
            // A player's pull latches, but only the floor transition's Close locks it for good.
            phase = DoorPhase.Shut;
            NorthDoorLatched?.Invoke(NorthDoorPoint);
        }
        PoseNorth();
        if (Mathf.Abs(oldAngle - bodyNorth.angle) > .0001f) Physics.SyncTransforms();
        return FrontRoomsBodyDoorContact.BlocksMove(northHinge.position, transform.right, bodyNorth.angle,
            NorthOpeningWidth, feet, inputMove);
    }
}
