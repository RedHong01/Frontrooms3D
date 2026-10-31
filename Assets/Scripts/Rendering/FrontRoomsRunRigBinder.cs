using System;
using System.Collections.Generic;
using FrontRooms.Map;
using UnityEngine;

/// <summary>
/// Run! B on the map (L7; the map side CR-1 to CR-6 + M8 landed by the level-design line 2026-10-31 01:07: FrontRoomsRunRooms
/// and FrontRoomsMapWorld's Run! events). One component on the map's GameObject builds, ticks and lets go of the visual Run!
/// rooms. It attaches itself, as FrontRoomsStationPaper does: on FrontRooms3DGame.MapRunStarted and FloorChanged (the same
/// map object), and only when map.RunRoomsOn (the level profile's runRooms, default off, or the tools' -autopilotRunRooms).
/// With Run! rooms off nothing is added, nothing subscribes and the game is today's. No map file is edited.
/// The goal door's place comes from the map: RunEntrance(room, true) (the opening centre on the goal wall line, and its
/// outward normal) and DoorBetween(Cells[8], Beyond) (the door, its hinge for the card's latch side).
/// - RunRoomBuilt (both chunks standing; again after each rebuild or return): a root at RunRoomPoint(room, 0) with
///   RunRoomRotation(room), the corridor-local frame (z 0 on the entrance cell line, +z to the goal wall at 27, +x right,
///   y 0 on the floor). Under it: the dress (skinShell: the map built the shell, CR-3), the rig (OwnLamps: the cells are
///   ModuleLamp.Off, CR-4), the shared ALARM ZONE tell at both entrances (RELAY 6: the card outside each opening, the
///   red-banded detector inside, clear of the wraparounds), and the goal window light's far-room lamp (C42). The goal
///   door is the map's (RN-F + F5, never locked: C38 / M8); the rig only aims at it (RunEntrance). A room rebuilt while its
///   alarm stands comes back tripped (TripImmediate, no events).
/// - RunRoomDropped (a chunk dropped or rebuilt, the floor changed): the root is destroyed.
/// - RoomTriggered (Alarm): Trip (the rig raises MainsDropped / TransferRelay for the sound line, CR-S3).
/// - RoomReset (the zone reset, the Relay's WITHDRAW): the animated re-strike when the viewer is within
///   AnimatedResetDistance, else back to armed at once.
/// - RunRoomWarned (CR-4): Warn(stage) on the rig's own wraps.
/// - LateUpdate: every rig's Tick (after the map's own lamp tick).
/// Office hosts (C39): the same B look whatever the host zone; only the goal window light takes the far zone's lamp colour.
/// The rig's light gate keeps the host's lamps (Level 0 or Office) out of the corridor except through the arch and the goal
/// opening, whose state the binder passes each frame (SetGoalOpen: the leaf off the latch or broken = the whole door, else
/// its F5 lite); the Run_B grade overrides everything the Office grade sets (stage C).
/// WebGL: the rig's own tier (no goal light: no lite there); the dress and the tell as everywhere.
/// ASCII, no tuples, no per-frame allocation once built.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("")]
public sealed class FrontRoomsRunRigBinder : MonoBehaviour
{
    /// <summary>The reset re-strike plays only when the viewer is this close to the room (m); farther, it is armed at once.</summary>
    public static float AnimatedResetDistance = 30f;
    /// <summary>Optional (the system-design line's call, open): the tell's LEDs burn steady while the room is tripped (the period latch).</summary>
    public static bool LatchAlarmLed;
    /// <summary>The detector disc's clearance from a wraparound's end (m): the tell's 0.25 m clear ceiling disc.</summary>
    public const float DetectorClear = .15f;
    /// <summary>The goal leaf counts as open for the light gate past this hinge angle (degrees); latched is 0.5.</summary>
    public const float GoalOpenDegrees = 2f;
    /// <summary>The goal leaf (RN-F, Kit_DoorLeaf_Ward_DA) whose F5 lite is the shut door's portal.</summary>
    public const string GoalLeafKit = "Kit_DoorLeaf_Ward_DA";

    /// <summary>A room's rig is built (sound line CR-S3 subscribes to its StateChanged / MainsDropped / TransferRelay /
    /// ResetStarted here; tools count rooms).</summary>
    public static event Action<FrontRoomsRunRoom, FrontRoomsRunRig> RigBuilt;
    /// <summary>A room's rig is about to be destroyed.</summary>
    public static event Action<FrontRoomsRunRoom, FrontRoomsRunRig> RigDropped;

    sealed class Entry
    {
        public FrontRoomsRunRoom room;
        public GameObject root;
        public FrontRoomsRunRig rig;
        public GameObject archDetector, goalDetector;
        public GridCoord[] farCells;
        public Vector3[] farLamps;
        public FrontRoomsMapWorld.Door goalDoor;
    }

    FrontRoomsMapWorld map;
    Transform viewer;
    readonly Dictionary<int, Entry> rooms = new Dictionary<int, Entry>();
    readonly List<Entry> ticking = new List<Entry>();
    bool subscribed;

    // ------------------------------------------------------------------ hook (the game's run events; no map edit)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Hook()
    {
        FrontRooms3DGame.MapRunStarted -= OnRunStarted;
        FrontRooms3DGame.MapRunStarted += OnRunStarted;
        FrontRooms3DGame.FloorChanged -= OnFloorChanged;
        FrontRooms3DGame.FloorChanged += OnFloorChanged;
    }

    // MapRunStarted is raised after the game's WireRunRooms, so RunRoomsOn is known; rooms already built are picked up by Bind.
    static void OnRunStarted(FrontRoomsMapWorld map, FrontRoomsMapHunter relay) => BindIfOn(map);
    static void OnFloorChanged(FrontRoomsMapWorld map, FrontRoomsMapHunter relay, int floor) => BindIfOn(map);

    static void BindIfOn(FrontRoomsMapWorld map)
    {
        if (map == null || !map.RunRoomsOn) return;
        Bind(map, map.Player);
    }

    /// <summary>The binder of a map (added on first call), bound to <paramref name="viewer"/> (the player; null: the main
    /// camera). Rooms the map already announced built are built now. The hook above calls it with Run! rooms on; public for
    /// harnesses. Idempotent.</summary>
    public static FrontRoomsRunRigBinder Bind(FrontRoomsMapWorld map, Transform viewer)
    {
        if (map == null) return null;
        var binder = map.GetComponent<FrontRoomsRunRigBinder>();
        if (binder == null) binder = map.gameObject.AddComponent<FrontRoomsRunRigBinder>();
        binder.Attach(map, viewer);
        return binder;
    }

    /// <summary>Tools: the rig of a built room, or null.</summary>
    public FrontRoomsRunRig RigOf(FrontRoomsRunRoom room) => room != null && rooms.TryGetValue(room.Id, out var e) ? e.rig : null;

    /// <summary>Tools: how many rooms are built.</summary>
    public int Count => rooms.Count;

    void Attach(FrontRoomsMapWorld m, Transform v)
    {
        viewer = v;
        foreach (var e in rooms.Values) if (e.rig != null) e.rig.SetViewer(Viewer);
        if (map == m && subscribed) return;
        Detach();
        map = m;
        map.RunRoomBuilt += OnBuilt;
        map.RunRoomDropped += OnDropped;
        map.RoomTriggered += OnTriggered;
        map.RoomReset += OnReset;
        map.RunRoomWarned += OnWarned;
        subscribed = true;
        var run = map.RunRooms;
        if (run == null) return;
        var list = run.Rooms;
        for (var i = 0; i < list.Count; i++) if (map.IsRunRoomBuilt(list[i])) OnBuilt(list[i]);
    }

    void Detach()
    {
        if (map != null && subscribed)
        {
            map.RunRoomBuilt -= OnBuilt;
            map.RunRoomDropped -= OnDropped;
            map.RoomTriggered -= OnTriggered;
            map.RoomReset -= OnReset;
            map.RunRoomWarned -= OnWarned;
        }
        subscribed = false;
        var ids = new List<int>(rooms.Keys);
        foreach (var id in ids) Drop(rooms[id]);
        rooms.Clear();
        ticking.Clear();
    }

    Transform Viewer
    {
        get
        {
            if (viewer != null) return viewer;
            var cam = Camera.main;
            return cam != null ? cam.transform : null;
        }
    }

    // ------------------------------------------------------------------ the map's events
    void OnBuilt(FrontRoomsRunRoom room)
    {
        if (room == null || map == null) return;
        if (rooms.TryGetValue(room.Id, out var old)) Drop(old);
        var e = new Entry { room = room };
        e.root = new GameObject("Run B / room " + room.Id);
        e.root.transform.SetParent(map.transform, false);
        e.root.transform.SetPositionAndRotation(map.RunRoomPoint(room, Vector3.zero), map.RunRoomRotation(room));
        var layout = Layout(room, e.root.transform);
        var dress = FrontRoomsRunDress.Build(e.root.transform, layout);
        DressTell(e, layout, dress);
        e.rig = FrontRoomsRunRig.Create(e.root.transform, layout, RunRigHost.OwnLamps, null, dress);
        e.rig.SetViewer(Viewer);
        e.goalDoor = map.DoorBetween(room.Cells[FrontRoomsRunRooms.Length - 1], room.Beyond);
        e.rig.SetGoalOpen(GoalOpen(e.goalDoor));
        FarLamps(e);
        var entry = e;
        e.rig.FarRoomLamp = () => FarLamp(entry);
        if (room.State == RunRoomState.Tripped)
        {
            e.rig.TripImmediate();
            Latch(e, true);
        }
        rooms[room.Id] = e;
        ticking.Add(e);
        RigBuilt?.Invoke(room, e.rig);
    }

    void OnDropped(FrontRoomsRunRoom room)
    {
        if (room != null && rooms.TryGetValue(room.Id, out var e)) Drop(e);
    }

    void OnTriggered(ModuleTrigger kind, FrontRoomsRunRoom room, string tag)
    {
        if (kind != ModuleTrigger.Alarm || room == null || !rooms.TryGetValue(room.Id, out var e) || e.rig == null) return;
        e.rig.Trip();
        Latch(e, true);
    }

    void OnReset(FrontRoomsRunRoom room)
    {
        if (room == null || !rooms.TryGetValue(room.Id, out var e) || e.rig == null) return;
        var v = Viewer;
        var near = v != null && (v.position - e.root.transform.TransformPoint(new Vector3(0f, 0f, room.LengthMetres * .5f))).sqrMagnitude
            <= AnimatedResetDistance * AnimatedResetDistance;
        if (near) e.rig.StartReset(); else e.rig.ResetImmediate();
        Latch(e, false);
    }

    void OnWarned(FrontRoomsRunRoom room, int stage)
    {
        if (room != null && rooms.TryGetValue(room.Id, out var e) && e.rig != null) e.rig.Warn(stage);
    }

    void Drop(Entry e)
    {
        if (e == null) return;
        if (e.rig != null) RigDropped?.Invoke(e.room, e.rig);
        rooms.Remove(e.room.Id);
        ticking.Remove(e);
        if (e.root != null)
        {
            if (Application.isPlaying) Destroy(e.root); else DestroyImmediate(e.root);
        }
        e.root = null;
        e.rig = null;
    }

    void LateUpdate()
    {
        var dt = Time.deltaTime;
        for (var i = ticking.Count - 1; i >= 0; i--)
        {
            var e = ticking[i];
            if (e.rig == null) { ticking.RemoveAt(i); continue; }
            e.rig.SetGoalOpen(GoalOpen(e.goalDoor));
            e.rig.Tick(dt);
        }
    }

    void OnDestroy() => Detach();

    /// <summary>Step 10: the goal opening is a whole-door portal for the map's lamps once its leaf is off the latch or
    /// broken, else only its lite (no door at all: the whole opening).</summary>
    static bool GoalOpen(FrontRoomsMapWorld.Door door) => door == null || door.broken || door.angle > GoalOpenDegrees;

    // ------------------------------------------------------------------ the room's layout
    /// <summary>The map module's layout (Corridor1x9 with the map's arch and door), the goal from the map's door, the far
    /// zone's lamp for the goal window light (C42).</summary>
    RunRigLayout Layout(FrontRoomsRunRoom room, Transform root)
    {
        var l = RunRigLayout.Corridor1x9(FrontRoomsRunRooms.ArchWidth, ModuleUnits.DoorWidth);
        l.name = "Run! room " + room.Id;
        l.wallThickness = ModuleUnits.WallThickness;
        l.buildShell = false;
        l.skinShell = true;
        // The goal door's opening centre (the map's RunEntrance, goal side): the goal head aims at it.
        map.RunEntrance(room, true, out var goalWorld, out var outward);
        var g = root.InverseTransformPoint(goalWorld);
        l.goalPoint = new Vector3(g.x, l.goalPoint.y, g.z);
        var back = root.InverseTransformDirection(-outward);
        if (back.sqrMagnitude > .5f) l.goalNormal = new Vector3(back.x, 0f, back.z).normalized;
        // Step 10: the shut goal door's portal is its F5 lite, measured from the map's hinge (the leaf kit's lite_centre
        // anchor and liteRect: 0.34 m from the hinge edge, 0.381 m square, 1.194-1.575 m).
        l.goalLiteX = l.goalPoint.x;
        var leaf = FrontRoomsKitLibrary.GetInfo(GoalLeafKit);
        var fromHinge = leaf != null && leaf.TryAnchor("lite_centre", out var lc) ? lc.z : .34f;
        if (leaf != null && leaf.HasLite)
        {
            var lr = leaf.LiteRect;
            l.goalLiteHalf = (lr.y - lr.x) * .5f;
            l.goalLiteBottom = lr.z;
            l.goalLiteTop = lr.w;
        }
        var goalDoor = map.DoorBetween(room.Cells[FrontRoomsRunRooms.Length - 1], room.Beyond);
        if (goalDoor != null && goalDoor.hinge != null)
        {
            var hx = root.InverseTransformPoint(goalDoor.hinge.position).x;
            l.goalLiteX = hx - Mathf.Sign(hx - l.goalPoint.x) * fromHinge;
        }
        // C42: the far zone's lamp (the map's ThemeMaterials: Level 0 or Office, the light lead when it is on).
        var far = map.ZoneOf(room.Beyond);
        var office = far.theme == ZoneTheme.Office;
        if (FrontRoomsTransitionLightLead.On)
        {
            l.goalLightColor = office ? FrontRoomsTransitionLightLead.OfficeColor : FrontRoomsTransitionLightLead.Level0Color;
            l.goalLightIntensity = office ? FrontRoomsTransitionLightLead.OfficeIntensity : FrontRoomsTransitionLightLead.Level0Intensity;
        }
        else
        {
            l.goalLightColor = new Color(1f, .96f, .88f);
            l.goalLightIntensity = office ? 5.5f : 5f;
        }
        l.goalLightHeight = Mathf.Min(l.goalLightHeight, MapGrid.CeilingHeight(room.BeyondHeight) - .25f);
        l.farLampRadius = map.Profile != null ? map.Profile.lightRadius : l.farLampRadius;
        return l;
    }

    /// <summary>
    /// RELAY 6 (the shared tell, FrontRoomsAlarmTell, the same at every trigger room's entrance): the ALARM ZONE card on the
    /// outside face beside each opening (the arch: on the approacher's left, B's NOTICE plate is on the right; the goal door:
    /// on its latch side, clear of the leaf's swing), and the red-banded detector on the ceiling centreline inside, at the
    /// sight rule's depth pushed clear of the wraparounds. The detectors hang under the dress (the rig's corridor ambient);
    /// the cards under the root (the host's light).
    /// </summary>
    void DressTell(Entry e, RunRigLayout l, Transform dress)
    {
        var root = e.root.transform;
        var t = l.wallThickness;
        var depth = l.length - t;
        // The arch (south): the entrance frame is the root's.
        var archDepth = FrontRoomsAlarmTell.DetectorDepth(t, l.height, ModuleUnits.ArchTop, depth);
        var archZ = ClearOfWraps(l, archDepth, 1f);
        FrontRoomsAlarmTell.DressCard(root, FrontRoomsAlarmTell.CardPoint(FrontRoomsRunRooms.ArchWidth, t, -1), 180f);
        e.archDetector = FrontRoomsAlarmTell.DressCeiling(dress, new Vector3(0f, l.height, archZ), 180f);
        // The goal door (north): its entrance frame is turned half round at the goal point.
        var gx = l.goalPoint.x;
        var gz = l.goalPoint.z;
        var goalDepth = FrontRoomsAlarmTell.DetectorDepth(t, l.height, ModuleUnits.DoorHeight, depth);
        var goalZ = ClearOfWraps(l, gz - goalDepth, -1f);
        var latch = GoalLatchSide(e.room, root, gx);
        var p = FrontRoomsAlarmTell.CardPoint(ModuleUnits.DoorWidth, t, -latch);   // the entrance frame's +X is the root's -X
        FrontRoomsAlarmTell.DressCard(root, new Vector3(gx - p.x, p.y, gz - p.z), 0f);
        e.goalDetector = FrontRoomsAlarmTell.DressCeiling(dress, new Vector3(gx, l.height, goalZ), 0f);
    }

    /// <summary>The goal door's latch side in the root's frame (+1: +X): opposite its hinge (the map's door), +1 without one.</summary>
    int GoalLatchSide(FrontRoomsRunRoom room, Transform root, float goalX)
    {
        var door = map.DoorBetween(room.Cells[FrontRoomsRunRooms.Length - 1], room.Beyond);
        if (door == null || door.hinge == null) return 1;
        var h = root.InverseTransformPoint(door.hinge.position);
        return h.x > goalX ? -1 : 1;
    }

    /// <summary>A ceiling point on the centreline moved along <paramref name="deeper"/> (+1 or -1) until the detector's disc
    /// clears every wraparound (its 1.235 m length plus DetectorClear).</summary>
    static float ClearOfWraps(RunRigLayout l, float z, float deeper)
    {
        const float wrapHalf = .6175f;
        for (var pass = 0; pass < 4; pass++)
        {
            var moved = false;
            for (var k = 0; k < l.ownLamps.Length; k++)
            {
                var c = l.ownLamps[k].z;
                if (Mathf.Abs(z - c) >= wrapHalf + DetectorClear) continue;
                z = c + deeper * (wrapHalf + DetectorClear);
                moved = true;
            }
            if (!moved) break;
        }
        return z;
    }

    void Latch(Entry e, bool on)
    {
        if (!LatchAlarmLed || e == null) return;
        FrontRoomsAlarmTell.SetAlarm(e.archDetector, on);
        FrontRoomsAlarmTell.SetAlarm(e.goalDetector, on);
    }

    // ------------------------------------------------------------------ C42: the far room's lamp
    /// <summary>The far room's lamp cells (the goal door's far cell and the next one along the axis) and their fixture
    /// points (the map's BuildFixture: the cell centre + TrofferOffsetZ along map +Z; chunk roots only translate).</summary>
    void FarLamps(Entry e)
    {
        var r = e.room;
        var next = new GridCoord(r.Beyond.x + r.AxisX, r.Beyond.y + r.AxisY);
        e.farCells = new[] { r.Beyond, next };
        e.farLamps = new Vector3[e.farCells.Length];
        var cs = MapGrid.CellSize;
        for (var i = 0; i < e.farCells.Length; i++)
        {
            var c = e.farCells[i];
            e.farLamps[i] = map.transform.TransformPoint(new Vector3((c.x + .5f) * cs, 0f, (c.y + .5f) * cs + ModuleUnits.TrofferOffsetZ));
        }
    }

    /// <summary>
    /// The far room's lamp as the map's lamp tick lights it (TickFixtures): the lamp's level (LampLevel, overrides included)
    /// times its distance fade (lightRadius, the last 3 m, XZ from the viewer); the brighter of the two far cells; below 0
    /// when neither has a lamp (the rig then fades its light near the door). Allocation-free.
    /// </summary>
    float FarLamp(Entry e)
    {
        var v = Viewer;
        if (map == null || v == null || e.farCells == null) return -1f;
        var radius = map.Profile != null ? map.Profile.lightRadius : 16f;
        var vp = v.position;
        var best = -1f;
        for (var i = 0; i < e.farCells.Length; i++)
        {
            var level = map.LampLevel(e.farCells[i]);
            if (level == FrontRoomsMapWorld.NoLamp) continue;
            var p = e.farLamps[i];
            var dx = p.x - vp.x;
            var dz = p.z - vp.z;
            var fade = Mathf.Clamp01((radius - Mathf.Sqrt(dx * dx + dz * dz)) / 3f);
            best = Mathf.Max(best, Mathf.Clamp01(level) * fade);
        }
        return best;
    }
}
