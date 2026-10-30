using FrontRooms.Map;
using UnityEngine;

/// <summary>
/// D1.6 (10_design §5.2): the lock beats the map's existing events drive on a dressed door's rig, with no map change:
/// the lever / knob on an open (DoorHandleTurned), the rattle's knob (DoorRattled), the latch click as a shut leaf
/// lands (DoorLatched), the strike coming off on a break (DoorBrokenFrom), the hanging leaf's wobble when a broken door
/// is tried (DoorBrokenTried), and the deadbolt left drawn once a door is unlocked (DoorUnlocked: for the run, from both
/// faces). One hidden component per map, added by the facade's first DressDoor in Play Mode; it unbinds on destroy.
/// Render-only: it reads nothing back into gameplay, plays no sound (the beats are the map's and the door shot's).
/// </summary>
public sealed class FrontRoomsDoorDressEvents : MonoBehaviour
{
    FrontRoomsMapWorld map;

    internal static void Bind(FrontRoomsMapWorld map)
    {
        if (map == null || !Application.isPlaying) return;
        if (map.GetComponent<FrontRoomsDoorDressEvents>() != null) return;
        var host = map.gameObject.AddComponent<FrontRoomsDoorDressEvents>();
        host.hideFlags = HideFlags.HideInInspector | HideFlags.DontSave;
        host.Attach(map);
    }

    void Attach(FrontRoomsMapWorld world)
    {
        map = world;
        map.DoorHandleTurned += OnHandleTurned;
        map.DoorRattled += OnRattled;
        map.DoorLatched += OnLatched;
        map.DoorBrokenFrom += OnBrokenFrom;
        map.DoorBrokenTried += OnBrokenTried;
        map.DoorUnlocked += OnUnlocked;
    }

    void OnDestroy()
    {
        if (map == null) return;
        map.DoorHandleTurned -= OnHandleTurned;
        map.DoorRattled -= OnRattled;
        map.DoorLatched -= OnLatched;
        map.DoorBrokenFrom -= OnBrokenFrom;
        map.DoorBrokenTried -= OnBrokenTried;
        map.DoorUnlocked -= OnUnlocked;
        map = null;
    }

    static FrontRoomsDoorRig RigOf(FrontRoomsMapWorld.Door door) => door != null ? door.rig as FrontRoomsDoorRig : null;

    static void OnHandleTurned(FrontRoomsMapWorld.Door door, Vector3 point) { var r = RigOf(door); if (r != null) r.BeatOpen(); }
    static void OnRattled(FrontRoomsMapWorld.Door door, Vector3 point) { var r = RigOf(door); if (r != null) r.BeatRattle(); }
    static void OnLatched(FrontRoomsMapWorld.Door door, Vector3 point) { var r = RigOf(door); if (r != null) r.BeatLatchClick(); }
    static void OnBrokenFrom(FrontRoomsMapWorld.Door door, Vector3 from, bool swingSide) { var r = RigOf(door); if (r != null) r.DetachStrike(from); }
    static void OnBrokenTried(FrontRoomsMapWorld.Door door, Vector3 point) { var r = RigOf(door); if (r != null) r.BeatWobble(); }
    static void OnUnlocked(FrontRoomsMapWorld.Door door, GridCoord zone, Vector3 keyhole) { var r = RigOf(door); if (r != null && r.LockedModel) r.SetDeadboltRest(1f); }
}
