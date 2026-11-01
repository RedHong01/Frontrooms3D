using FrontRooms.Map;
using UnityEngine;

public sealed partial class FrontRoomsMapHunter
{
    FrontRoomsMapWorld.Door pendingBodyBreak;
    FrontRoomsMapWorld.Door bodyBraceBreak;

    bool BodyDoorPlayerSafe => world.InStairwell(world.CellOf(
        playerCollider != null ? playerCollider.transform.position : sightFeet));

    // An OPEN leaf can still be braced short of passable. Its break must not take
    // TickBreak's ordinary "the player opened it" early exit during the blows.
    bool BodyHeldBreakActive(FrontRoomsMapWorld.Door door) => world.BodyDoors
        && door != null && door == bodyBraceBreak && !BodyDoorPlayerSafe;

    void EndBodyDoorBreak() => bodyBraceBreak = null;

    // A player holding a leaf is a perceptible obstruction, never an infinite OPEN wait.
    public void BodyDoorHeld(FrontRoomsMapWorld.Door door)
    {
        if (!world.BodyDoors || door == null || door.broken || !Released || caught
            || world.IsExitDoor(door) || BodyDoorPlayerSafe) return;
        if (State == HunterState.BreakDoor) return;
        pendingBodyBreak = door;
    }

    void TickBodyDoorHold()
    {
        var door = pendingBodyBreak;
        pendingBodyBreak = null;
        if (door == null || door.broken || door.hinge == null || caught || !Released || !world.BodyDoors
            || world.IsExitDoor(door) || BodyDoorPlayerSafe || !world.RelayDoorHeldByPlayer(door)) return;
        StopBreaking();
#if FRONTROOMS_RELAY_V2
        EndDoorActV2();
        breakLicenceV2.Add(door.edge);
#endif
        var direction = door.position - position;
        direction.y = 0f;
        breakingDoor = door;
        bodyBraceBreak = door;
        breakFacing = direction.sqrMagnitude > 1e-4f ? direction.normalized : Heading;
        world.SwingAwayFrom(door, position);
        world.MarkBeingBroken(door, true);
        StartBlows(door);
        resumeState = State;
        SetState(HunterState.BreakDoor);
    }

    public void BodyDoorBrokeAgainstPlayer(FrontRoomsMapWorld.Door door)
    {
        if (door == null || !Released || caught || world.IsExitDoor(door) || BodyDoorPlayerSafe) return;
        // Fired only when this Relay's break throw actually meets the player's capsule.
        // The stopped-throw safety rule applies to legacy doors as well as body doors.
        caught = true;
        Caught?.Invoke();
    }
}
