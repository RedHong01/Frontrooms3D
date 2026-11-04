using System.Globalization;
using FrontRooms.Map;
using UnityEngine;

// Run! rooms (L7; CR-2 and the map hooks' game side), only while the map holds Run! rooms (map.RunRoomsOn: the level
// profile's runRooms, default off, or the tools' -autopilotRunRooms). With it off nothing here is wired or ticked, and
// the game is today's.
// - Open (A40): the map plans no Run! room in a zone met before the run's first send. The game raises map.RunRoomsOpen
//   once the Relay has been sent (its first send, or its first time on the map); the map keeps it through the floors.
// - The trip (CR-2, RELAY 6): RoomTriggered(Alarm, room, "run") is a device. Under FRONTROOMS_RELAY_V2 it reaches the
//   director as a device trigger at the device (error 0; RELAX included; on the map a re-dispatch while the budget lasts,
//   else heard and queued) and the on-map ears hear it at the device loudness (45). On v1 it is v1's device bridge (the
//   foil's): one noise of the device loudness at the device.
// - The reset (CR-2): v2, the director's Away (the Relay's WITHDRAW ends off the map); v1 has no WITHDRAW, so the zone
//   resets once the Relay has been calm (Dormant, Listen or Wander) RunResetCalmSecondsV1 since the last trip (SP).
// - CR-4: each warning burst on a Run! room is forwarded to the rig (map.RunRoomWarned) with the Relay's stage.
// - Logs: RUN lines for every room planned, built, tripped and reset (the tester's report fields).
public sealed partial class FrontRooms3DGame
{
    /// <summary>v1's stand-in for the zone reset (v2's WITHDRAW): seconds of a calm Relay since the last trip (SP).</summary>
    const float RunResetCalmSecondsV1 = 20f;
#if !FRONTROOMS_RELAY_V2
    float runCalmFor;
#endif
    float runLastTripAt = -1f;

    /// <summary>Once per map run, after the Relay (and v2's director) exist: wire the map's Run! events; nothing with Run! rooms off.</summary>
    void WireRunRooms()
    {
        if (map == null || !map.RunRoomsOn) return;
#if !FRONTROOMS_RELAY_V2
        runCalmFor = 0f;
#endif
        runLastTripAt = -1f;
        var rules = map.RunRooms.Rules;
        Log("RUN · Run! rooms ON · call 2 (forced goal door) " + (rules.goalDoorForced ? "on" : "off") + ", call 5 (Office zones) " + (rules.office ? "on" : "off")
            + (rules.force ? ", placement FORCED (tools)" : "") + " · tier " + rules.minTier + "+, after the run's first send, " + rules.minStartCells + "+ cells from the start door, not the arrival zone");
        map.RunRoomPlanned += room => Log("RUN · planned " + RunRoomText(room) + " · floor " + floor + ", zone tier " + room.Tier);
        map.RunRoomBuilt += OnRunRoomBuilt;
        map.RunRoomDropped += room => Event("run", "dropped " + room.Id);
        map.RoomTriggered += OnRunRoomTriggered;
        map.RoomReset += room =>
        {
            Event("run", "reset " + room.Id);
            Log("RUN · reset room " + room.Id + " at " + elapsed.ToString("0.0", CultureInfo.InvariantCulture) + " s (armed again" + (room.WaitForExit ? ", trips again once the player has left it" : "") + ")");
        };
        // A burst plays on the player's room set (its centre may lie on lamps outside the corridor, which has none of its own).
        if (warnLamps != null) warnLamps.WarnBurst += (centre, lamps) => { if (playerRoot != null) map.ForwardRunWarn(playerRoot.position, relay != null ? relay.WarnStage : 0); };
#if FRONTROOMS_RELAY_V2
        // CR-2: the zone resets when the Relay's WITHDRAW ends off the map (the director's Away).
        if (director != null) director.Away += reason =>
        {
            var n = map.ResetRunRooms();
            if (n > 0) Log("RUN · the Relay went away (" + reason + "): " + n + " Run! room" + (n == 1 ? "" : "s") + " reset");
        };
#endif
    }

    /// <summary>Each frame of map play (after the Relay's tick): Open once the first send is out; v1's reset bridge.</summary>
    void UpdateRunRooms(float dt)
    {
        if (relay == null) return;
        if (!map.RunRoomsOpen && (relay.FirstSendDone || relay.OnMap))
        {
            map.RunRoomsOpen = true;
            Log("RUN · open at " + elapsed.ToString("0.0", CultureInfo.InvariantCulture) + " s on floor " + floor + ": the run's first send has gone out (A40); zones met from now on at tier "
                + map.RunRooms.Rules.minTier + "+ may hold a Run! room");
        }
#if !FRONTROOMS_RELAY_V2
        if (!map.AnyRunRoomTripped) { runCalmFor = 0f; return; }
        var s = relay.State;
        runCalmFor = s == HunterState.Dormant || s == HunterState.Listen || s == HunterState.Wander ? runCalmFor + dt : 0f;
        if (runCalmFor < RunResetCalmSecondsV1 || elapsed - runLastTripAt < RunResetCalmSecondsV1) return;
        runCalmFor = 0f;
        var n = map.ResetRunRooms();
        if (n > 0) Log("RUN · v1 reset: the Relay calm " + RunResetCalmSecondsV1.ToString("0", CultureInfo.InvariantCulture) + " s after the last trip: " + n + " Run! room" + (n == 1 ? "" : "s") + " armed again");
#endif
    }

    void OnRunRoomBuilt(FrontRoomsRunRoom room)
    {
        Event("run", "built " + room.Id);
        // The tester's fields: the finish (CR-3), the lamps the map built in it (CR-4: none), the goal door (M8, CR-6).
        var lamps = 0;
        foreach (var c in room.Cells) if (map.LampLevel(c) != FrontRoomsMapWorld.NoLamp) lamps++;
        var door = map.DoorBetween(room.GoalCell, room.Beyond);
        Log("RUN · built " + RunRoomText(room) + " · " + room.State + " · map lamps in it " + lamps + " · goal door " + (door == null ? "NOT BUILT"
            : "member " + (map.DoorMemberFor(door)?.ToString() ?? "theme") + ", locked by rule " + map.LockedByRule(door) + (door.rig != null ? ", dressed" : ", cube"))
            + " · Run finish on its cells, arch " + FrontRoomsRunRooms.ArchWidth.ToString("0.0", CultureInfo.InvariantCulture) + " m centred");
    }

    void OnRunRoomTriggered(ModuleTrigger trigger, FrontRoomsRunRoom room, string tag)
    {
        var p = map.RunDevicePoint(room);
        runLastTripAt = elapsed;
#if !FRONTROOMS_RELAY_V2
        runCalmFor = 0f;
#endif
        Event("run", "tripped " + room.Id);
        Log("RUN · tripped " + RunRoomText(room) + " · " + trigger + " \"" + tag + "\" at " + elapsed.ToString("0.0", CultureInfo.InvariantCulture) + " s (trip " + room.Trips + "), device "
            + Flat(p) + " · the Relay " + (relay != null ? relay.State.ToString() : "none"));
#if FRONTROOMS_RELAY_V2
        // RELAY 6 (ModuleTrigger.Alarm -> DeviceTrigger): a device call to the device, sharing the trigger budget; the on-map ears hear it.
        RelayNoise(RelayNoiseKind.Device, p);
        director?.DeviceTrigger(p, FrontRoomsRunRooms.Tag);
#elif UNITY_EDITOR
        // v1 (the foil's bridge): one noise of the device loudness at the device.
        AutopilotNoise(AutoCauseOther, p, hunterTuning.deviceLoudness);
#else
        relay?.Noise(p, hunterTuning.deviceLoudness);
#endif
    }

    static string RunRoomText(FrontRoomsRunRoom room) =>
        "Run! room " + room.Id + " (zone " + room.Zone + " " + room.Theme + ", cells " + room.Cells[0] + " -> " + room.GoalCell + ", arch from " + room.Outside
        + ", goal door to " + room.Beyond + " " + room.BeyondHeight + ", yaw " + room.Yaw.ToString("0", CultureInfo.InvariantCulture) + ")";
}
