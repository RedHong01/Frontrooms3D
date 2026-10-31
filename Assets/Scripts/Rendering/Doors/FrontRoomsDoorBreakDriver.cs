// FrontRoomsDoorBreakDriver.cs -- the door break's hooks into the running game, production P1 (2026-10-30). Render-only and
// with NO map change: it listens to events that already exist (Documentation/research/door_break/50_rethink.md 3.9, 3.10).
//   FrontRooms3DGame.MapRunStarted  a new run: forget every record, bind the Relay and the map;
//   FrontRooms3DGame.FloorChanged   a new floor (NextFloor reseeds the same map): forget the last floor's records;
//   FrontRoomsMapHunter.DoorBlow    the Relay's blow on a door (TickBreak raises it on the frame of JoltForBlow); the door
//                                   is the dressed rig at that position that the map marks as being broken, and the blow's
//                                   index and count are the hunter's public BlowIndex / BlowCount;
//   FrontRoomsMapWorld.DoorBrokenFrom  the break (after the map's throw starts).
// Map contract line M1 (W/apply/door_break_contract: FrontRoomsMapHunter.DoorBlowStruck(door, index, count, from), wired to
// BlowStruck below in FrontRooms3DGame) makes the door lookup exact; until 关卡设计 lands it, the position match is used (two
// dressed doors never share a position). The package runs correctly either way, and neither way feeds back into gameplay.
using UnityEngine;

public static class FrontRoomsDoorBreakDriver
{
    /// <summary>Tools: false while a harness drives the blows itself (the Relay is dormant there anyway).</summary>
    public static bool Enabled = true;
    static FrontRoomsMapWorld map;
    static FrontRoomsMapHunter hunter;
    // M1: the exact event ran for this blow; the position path (raised right after it) stands down.
    static bool exactBlow;
    /// <summary>Tools: the blows that reached a visual by the position path and by the exact event (M1).</summary>
    public static int BlowsByPosition { get; private set; }
    public static int BlowsExact { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { map = null; hunter = null; Enabled = true; exactBlow = false; BlowsByPosition = BlowsExact = 0; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Hook()
    {
        FrontRooms3DGame.MapRunStarted -= OnRun;
        FrontRooms3DGame.MapRunStarted += OnRun;
        FrontRooms3DGame.FloorChanged -= OnFloor;
        FrontRooms3DGame.FloorChanged += OnFloor;
        FrontRooms3DGame.MapRunEnded -= OnEnded;
        FrontRooms3DGame.MapRunEnded += OnEnded;
    }

    static void OnRun(FrontRoomsMapWorld world, FrontRoomsMapHunter relay)
    {
        Unbind();
        FrontRoomsDoorBreakRecords.Clear();
        Bind(world, relay);
    }

    static void OnFloor(FrontRoomsMapWorld world, FrontRoomsMapHunter relay, int floor)
    {
        // records are keyed by the floor's seed, so a new floor's doors never pick up the last floor's; free them
        FrontRoomsDoorBreakRecords.Clear();
        if (world != map || relay != hunter) { Unbind(); Bind(world, relay); }
    }

    static void OnEnded() => Unbind();

    static void Bind(FrontRoomsMapWorld world, FrontRoomsMapHunter relay)
    {
        map = world;
        hunter = relay;
        if (hunter != null) hunter.DoorBlow += OnBlow;
        if (map != null) map.DoorBrokenFrom += OnBroken;
    }

    static void Unbind()
    {
        if (hunter != null) hunter.DoorBlow -= OnBlow;
        if (map != null) map.DoorBrokenFrom -= OnBroken;
        hunter = null;
        map = null;
        exactBlow = false;
    }

    /// <summary>The dressed rig of the door at this position that the map marks as being broken (null when undressed).</summary>
    public static FrontRoomsDoorRig RigAt(FrontRoomsMapWorld world, Vector3 doorPosition)
    {
        var rigs = FrontRoomsDoorRig.All;
        FrontRoomsDoorRig best = null;
        var bestD = .0025f;
        for (var i = 0; i < rigs.Count; i++)
        {
            var r = rigs[i];
            if (r == null || r.Door == null || r.Map != world) continue;
            var d = (r.Door.position - doorPosition).sqrMagnitude;
            if (d < bestD && world.IsBeingBroken(r.Door)) { bestD = d; best = r; }
        }
        return best;
    }

    /// <summary>Map contract M1: FrontRoomsMapHunter.DoorBlowStruck, raised just before DoorBlow on the same frame, carrying
    /// the door. Subscribed by the contract's one line in FrontRooms3DGame (relay.DoorBlowStruck += ...BlowStruck).</summary>
    public static void BlowStruck(FrontRoomsMapWorld.Door door, int index, int count, Vector3 from)
    {
        exactBlow = true;
        if (!Enabled || door == null) return;
        var rig = door.rig as FrontRoomsDoorRig;
        if (rig == null) return;
        var v = FrontRoomsDoorBreakVisual.Ensure(rig);
        if (v == null) return;
        v.Blow(index, count, from + Vector3.up);
        BlowsExact++;
    }

    static void OnBlow(Vector3 doorPosition)
    {
        if (exactBlow) { exactBlow = false; return; }
        if (!Enabled || map == null || hunter == null) return;
        var rig = RigAt(map, doorPosition);
        if (rig == null) return;
        var v = FrontRoomsDoorBreakVisual.Ensure(rig);
        if (v == null) return;
        v.Blow(hunter.BlowIndex, hunter.BlowCount, hunter.Position + Vector3.up);
        BlowsByPosition++;
    }

    static void OnBroken(FrontRoomsMapWorld.Door door, Vector3 from, bool swingSide)
    {
        if (door == null) return;
        var rig = door.rig as FrontRoomsDoorRig;
        if (rig == null) return;
        var v = FrontRoomsDoorBreakVisual.Ensure(rig);
        if (v != null) v.Broken(from);
    }
}
