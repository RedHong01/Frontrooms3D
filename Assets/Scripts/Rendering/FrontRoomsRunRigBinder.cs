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
/// outward normal) and DoorBetween(the last cell, Beyond) (the door, its hinge for the card's latch side).
/// The length comes from the room's record (71 s.9, card 1: CellsOf, the cells from the entrance to the goal; 9 today, 7 .. 14
/// built, anything else refused at 9): the rig and the dress are RunRigLayout.Corridor(cells). The map builds only 9-cell rooms
/// today, so nothing changes until it sends another length (contract: W/apply/run_rethink_contract/README.md).
/// - RunRoomBuilt (both chunks standing; again after each rebuild or return): a root at RunRoomPoint(room, 0) with
///   RunRoomRotation(room), the corridor-local frame (z 0 on the entrance cell line, +z to the goal wall at 3 m a cell, 27
///   today, +x right, y 0 on the floor). Under it: the dress (skinShell: the map built the shell, CR-3), the rig (OwnLamps: the cells are
///   ModuleLamp.Off, CR-4), the shared ALARM ZONE tell at both entrances (RELAY 6: the card outside each opening, the
///   red-banded detector inside, clear of the wraparounds), and the goal window light's far-room lamp (C42). The goal
///   door is the map's (RN-F + F5, never locked: C38 / M8); the rig only aims at it (RunEntrance). A room rebuilt while its
///   alarm stands comes back tripped (TripImmediate, no events).
/// - RunRoomDropped (a chunk dropped or rebuilt, the floor changed): the root is destroyed.
/// - RoomTriggered (Alarm): Trip (the rig raises MainsDropped / TransferRelay for the sound line, CR-S3).
/// - RoomReset (the zone reset, the Relay's WITHDRAW): the animated re-strike when the viewer is within
///   AnimatedResetDistance of the room's centre (plus half of any length past 27 m), else back to armed at once.
/// - RunRoomWarned (CR-4): Warn(stage) on the rig's own wraps.
/// - LateUpdate: every rig's Tick (after the map's own lamp tick).
/// Office hosts (C39): the same B look whatever the host zone; only the goal window light takes the far zone's lamp colour.
/// The rig's light gate keeps the host's lamps (Level 0 or Office) out of the corridor except through the arch and the goal
/// opening, whose state the binder passes each frame (SetGoalOpen: the leaf off the latch or broken = the whole door, else
/// its F5 lite); the Run_B grade overrides everything the Office grade sets (stage C).
/// WebGL: the rig's own tier (no goal light: no lite there); the dress and the tell as everywhere.
/// Card 4 (71 s.7.2 and s.9, decision 8c: D1, an honest Run door): when the map builds a door on the entrance edge
/// (DoorBetween(Outside, Cells[0]); the level-design line's card 5, from T3 after one plain Run!) the entrance is a Run door: the layout takes it
/// (RunRigLayout.UseEntranceDoor, hinged where the map hangs it), the ALARM ZONE card goes on its latch side outside and the
/// red-banded detector inside at the sight rule's depth for a door head (DressEntranceTell), the gate's entrance portal follows
/// its leaf (SetEntranceOpen), and the corridor's light shows under it on the approach side (FrontRoomsRunDoorGap); its lite shows
/// the lit corridor through the map's own wired glass. Today the map builds arches only: no door is found, and every room is
/// built exactly as before (contract: W/apply/run_rethink_contract/README.md, card 4).
/// The receding room (71 s.4-s.5, decision 4; the level-design line's card 4, the map side landed 11-04 12:46): when the map's
/// record says the room recedes (room.Receding: its Variant Recede and SeatCells, dealt only with the rules' recede switch, which
/// no level profile serializes until Red has walked SP7, RULES 5b), the layout is RunRigLayout.Receding(SeatCells[0], Cells.Length),
/// the end is built after the dress and before the rig (FrontRoomsRunRecede.Build, then Bind) and ticked after the rig, and the
/// map's trip (at FD1 for this variant) trips it too (Order T: the alarm and the end's flight are one event). On each SeatReached
/// the map seats the room (RunSeated(room, line / 3 m): its cells open, its goal point moves); at the final seat B's pulses may
/// begin (FrontRoomsRunRig.ReleasePulses; held from the build, 71 s.5.7). The recede's 8-byte record lives with the map's room
/// (RecedeRecord, PackRecord), written at each seat and when the room is dropped; a room rebuilt after its recede began comes back
/// seated at its deepest line with no event (Restore), and the map is seated too (RunSeated(room, LengthCells)). The goal end's
/// tell stands at the deepest line, the release side's frame. Every other room is built and ticked exactly as before.
/// ASCII, no tuples, no per-frame allocation once built.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("")]
public sealed class FrontRoomsRunRigBinder : MonoBehaviour
{
    /// <summary>The reset re-strike plays only when the viewer is this close to the room's centre (m; a 9-cell room's: a longer room
    /// adds half its extra length, so the reach past its ends stays 16.5 m); farther, it is armed at once.</summary>
    public static float AnimatedResetDistance = 30f;
    /// <summary>Optional (the system-design line's call, open): the tell's LEDs burn steady while the room is tripped (the period latch).</summary>
    public static bool LatchAlarmLed;
    /// <summary>The detector disc's clearance from a ceiling lamp's end (m): the tell's 0.25 m clear ceiling disc.</summary>
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
    /// <summary>A receding room's recede is built and bound, raised after RigBuilt (the sound line subscribes to its Cue here, as to
    /// the rig's events on RigBuilt). A room rebuilt after its recede began is already seated when this is raised: Restore raises
    /// nothing.</summary>
    public static event Action<FrontRoomsRunRoom, FrontRoomsRunRecede> RecedeBuilt;
    /// <summary>A receding room's recede is about to be destroyed (before RigDropped).</summary>
    public static event Action<FrontRoomsRunRoom, FrontRoomsRunRecede> RecedeDropped;

    sealed class Entry
    {
        public FrontRoomsRunRoom room;
        public GameObject root;
        public FrontRoomsRunRig rig;
        public GameObject archDetector, goalDetector;
        public GridCoord[] farCells;
        public Vector3[] farLamps;
        public FrontRoomsMapWorld.Door goalDoor;
        /// <summary>Card 4: the map's door on the entrance edge (a Run door entrance), or null for the arch (every room today).</summary>
        public FrontRoomsMapWorld.Door entranceDoor;
        public FrontRoomsRunDoorGap gap;
        /// <summary>The level-design line's card 4: the receding end (null for every other room) and its seat hook (kept to unsubscribe).</summary>
        public FrontRoomsRunRecede recede;
        public Action<int, float, bool> onSeat;
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

    /// <summary>
    /// The room's length in cells (71 s.9, card 1), from its record: CR-1's cells from the entrance (Cells[0]) to the goal (the
    /// last), so Cells.Length; 9 (RunRigLayout.TodayCells) when the record has none. The map builds 9 today. A length outside
    /// 7 .. 14 is refused by RunRigLayout.Corridor (a warning, built at 9). A receding room's record holds its reserved L_max
    /// cells (12 or 14) and its seats from the end's first line (SeatCells, 9 first): RunRigLayout.Receding(SeatCells[0], CellsOf).
    /// </summary>
    public static int CellsOf(FrontRoomsRunRoom room) => room != null && room.Cells != null && room.Cells.Length > 0 ? room.Cells.Length : RunRigLayout.TodayCells;

    /// <summary>The goal cell (the last of the room's cells; CR-6's goal door is on its far edge, onto Beyond).</summary>
    static GridCoord GoalCell(FrontRoomsRunRoom room) => room.Cells[room.Cells.Length - 1];

    /// <summary>Tools: the rig of a built room, or null.</summary>
    public FrontRoomsRunRig RigOf(FrontRoomsRunRoom room) => room != null && rooms.TryGetValue(room.Id, out var e) ? e.rig : null;

    /// <summary>Tools: the recede of a built receding room, or null (every other room, or not built).</summary>
    public FrontRoomsRunRecede RecedeOf(FrontRoomsRunRoom room) => room != null && rooms.TryGetValue(room.Id, out var e) ? e.recede : null;

    /// <summary>
    /// The level-design line's card 4: the recede's 8-byte record (RunRecedeRecord) as the map's room keeps it
    /// (FrontRoomsRunRoom.RecedeRecord, which the map never reads): version, flags, steps and glideFrom in the low four bytes, then
    /// gateTime and glideTime (halves). 0 is no record (version 0: nothing to restore).
    /// </summary>
    public static ulong PackRecord(RunRecedeRecord r)
    {
        return r.version | (ulong)r.flags << 8 | (ulong)r.steps << 16 | (ulong)r.glideFrom << 24 | (ulong)r.gateTime << 32 | (ulong)r.glideTime << 48;
    }

    /// <summary>The record PackRecord wrote.</summary>
    public static RunRecedeRecord UnpackRecord(ulong bits)
    {
        return new RunRecedeRecord
        {
            version = (byte)bits, flags = (byte)(bits >> 8), steps = (byte)(bits >> 16), glideFrom = (byte)(bits >> 24),
            gateTime = (ushort)(bits >> 32), glideTime = (ushort)(bits >> 48),
        };
    }

    /// <summary>A seat line in rig z (m) as the map's whole cells from the entrance (71 s.3.6: 27, 33, 36, 39, 42 m are 9, 11, 12, 13, 14).</summary>
    public static int LineCells(float line) => Mathf.RoundToInt(line / RunRigLayout.CellLength);

    /// <summary>Tools: how many rooms are built.</summary>
    public int Count => rooms.Count;

    void Attach(FrontRoomsMapWorld m, Transform v)
    {
        viewer = v;
        foreach (var e in rooms.Values)
        {
            if (e.rig != null) e.rig.SetViewer(Viewer);
            if (e.recede != null) e.recede.SetViewer(GazeOf(Viewer));
        }
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

    /// <summary>The gaze the recede's gate reads (71 s.4.3: the camera faces down the corridor; contract: SetViewer(the player's
    /// camera)): the camera riding the viewer (the game's camera rides the player's eye, FrontRoomsCameraRig), else the viewer.</summary>
    static Transform GazeOf(Transform v)
    {
        if (v == null) return null;
        var cam = v.GetComponentInChildren<Camera>(true);
        return cam != null ? cam.transform : v;
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
        // Card 4: a door on the entrance edge makes it a Run door entrance (none today: the map builds the arch).
        e.entranceDoor = room.Cells != null && room.Cells.Length > 0 ? map.DoorBetween(room.Outside, room.Cells[0]) : null;
        var layout = Layout(room, e.root.transform, e.entranceDoor);
        var dress = FrontRoomsRunDress.Build(e.root.transform, layout);
        DressTell(e, layout, dress);
        // The receding room: the end after the dress and the tell (the goal end's detector waits in its hidden cell), before the
        // rig, so the rig takes the end's parts into its ambient and light gate.
        if (layout.recedeLine >= 0f) e.recede = FrontRoomsRunRecede.Build(dress, layout);
        e.rig = FrontRoomsRunRig.Create(e.root.transform, layout, RunRigHost.OwnLamps, null, dress);
        e.rig.SetViewer(Viewer);
        e.goalDoor = map.DoorBetween(GoalCell(room), room.Beyond);
        e.rig.SetGoalOpen(GoalOpen(e.goalDoor));
        if (e.entranceDoor != null)
        {
            e.rig.SetEntranceOpen(GoalOpen(e.entranceDoor));
            e.gap = FrontRoomsRunDoorGap.Build(e.rig, e.root.transform);
        }
        FarLamps(e);
        var entry = e;
        e.rig.FarRoomLamp = () => FarLamp(entry);
        var restored = e.recede != null && BindRecede(e);
        if (room.State == RunRoomState.Tripped)
        {
            e.rig.TripImmediate();
            // A receding room tripped before its end moved: the end waits for the gaze gate again (Trip raises nothing).
            if (e.recede != null && !restored) e.recede.Trip();
            Latch(e, true);
        }
        rooms[room.Id] = e;
        ticking.Add(e);
        // The map never reads the record: a room rebuilt seated is seated on the map too (false, nothing raised, if it already is).
        if (restored) map.RunSeated(room, room.LengthCells);
        RigBuilt?.Invoke(room, e.rig);
        if (e.recede != null) RecedeBuilt?.Invoke(room, e.recede);
    }

    /// <summary>
    /// The receding room's end, after the rig: bound to it (the goal mount rides the end, the light gate stands on its line), the
    /// player's gaze, the seat hook, then the map's record. A record whose recede had begun (RunRecedeRecord.Started) is restored:
    /// the end seated at the deepest line, its cells and strips on, no event; true. Otherwise the recede is still to come, so B's
    /// pulses wait for its final seat (71 s.5.7); false.
    /// </summary>
    bool BindRecede(Entry e)
    {
        var recede = e.recede;
        recede.Bind(e.rig);
        recede.SetViewer(GazeOf(Viewer));
        var entry = e;
        e.onSeat = (step, line, final) => OnSeat(entry, line, final);
        recede.SeatReached += e.onSeat;
        var record = UnpackRecord(e.room.RecedeRecord);
        if (record.version == RunRecedeRecord.Version1 && record.Started)
        {
            recede.Restore(record);
            return true;
        }
        e.rig.PulseHeld = true;
        return false;
    }

    /// <summary>A seat of the receding end: the record to the map's room, B's pulses free at the final seat, and the map seated on
    /// the seat's line in whole cells (its cells up to it open, its goal point moves; the last seat makes the goal door real).</summary>
    void OnSeat(Entry e, float line, bool final)
    {
        if (e == null || e.recede == null) return;
        e.room.RecedeRecord = PackRecord(e.recede.Record);
        if (final && e.rig != null) e.rig.ReleasePulses(1f);
        if (map != null) map.RunSeated(e.room, LineCells(line));
    }

    void OnDropped(FrontRoomsRunRoom room)
    {
        if (room != null && rooms.TryGetValue(room.Id, out var e)) Drop(e);
    }

    void OnTriggered(ModuleTrigger kind, FrontRoomsRunRoom room, string tag)
    {
        if (kind != ModuleTrigger.Alarm || room == null || !rooms.TryGetValue(room.Id, out var e) || e.rig == null) return;
        e.rig.Trip();
        // A receding room trips at FD1 (the map's EntranceTrip until its last seat): the end's clock starts with the alarm; the gaze
        // gate decides its first step (once: a seated or already tripped end ignores it).
        if (e.recede != null) e.recede.Trip();
        Latch(e, true);
    }

    void OnReset(FrontRoomsRunRoom room)
    {
        if (room == null || !rooms.TryGetValue(room.Id, out var e) || e.rig == null) return;
        var v = Viewer;
        var length = e.rig.Layout.length;
        var reach = AnimatedResetDistance + (length - RunRigLayout.CellLength * RunRigLayout.TodayCells) * .5f;
        var near = v != null && (v.position - e.root.transform.TransformPoint(new Vector3(0f, 0f, length * .5f))).sqrMagnitude
            <= reach * reach;
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
        if (e.recede != null)
        {
            // 71 s.4.6, the record stays: the map's room keeps it, and the rebuild comes back from it.
            e.room.RecedeRecord = PackRecord(e.recede.Record);
            if (e.onSeat != null) e.recede.SeatReached -= e.onSeat;
            RecedeDropped?.Invoke(e.room, e.recede);
        }
        if (e.rig != null) RigDropped?.Invoke(e.room, e.rig);
        rooms.Remove(e.room.Id);
        ticking.Remove(e);
        if (e.root != null)
        {
            if (Application.isPlaying) Destroy(e.root); else DestroyImmediate(e.root);
        }
        e.root = null;
        e.rig = null;
        e.recede = null;
        e.onSeat = null;
    }

    void LateUpdate() => Tick(Time.deltaTime);

    /// <summary>Tools and tests in edit mode, where LateUpdate does not run: one tick of every built room, as LateUpdate does.</summary>
    public void TickForTools(float dt) => Tick(dt);

    void Tick(float dt)
    {
        for (var i = ticking.Count - 1; i >= 0; i--)
        {
            var e = ticking[i];
            if (e.rig == null) { ticking.RemoveAt(i); continue; }
            e.rig.SetGoalOpen(GoalOpen(e.goalDoor));
            if (e.entranceDoor != null) e.rig.SetEntranceOpen(GoalOpen(e.entranceDoor));
            e.rig.Tick(dt);
            // The receding end after the rig (as R20 and the recede's harnesses tick it): its seats call the map (OnSeat).
            if (e.recede != null) e.recede.Tick(dt);
            // Card 4: the light under the shut Run door, from the rig's Lights as this Tick left them.
            if (e.gap != null) e.gap.Tick(ShutWeight(e.entranceDoor));
        }
    }

    /// <summary>Card 4: 1 while the entrance leaf is on its latch (0.5 deg), 0 once it is GoalOpenDegrees + 1 off it or broken: the
    /// light under the door fades as the open door lets the corridor's real light through.</summary>
    public static float ShutWeight(FrontRoomsMapWorld.Door door)
    {
        if (door == null || door.broken) return 0f;
        var t = Mathf.Clamp01((door.angle - .5f) / (GoalOpenDegrees + .5f));
        return 1f - t * t * (3f - 2f * t);
    }

    void OnDestroy() => Detach();

    /// <summary>Step 10: the goal opening is a whole-door portal for the map's lamps once its leaf is off the latch or
    /// broken, else only its lite (no door at all: the whole opening).</summary>
    static bool GoalOpen(FrontRoomsMapWorld.Door door) => door == null || door.broken || door.angle > GoalOpenDegrees;

    // ------------------------------------------------------------------ the room's layout
    /// <summary>The map module's layout (Corridor(cells) with the map's arch and door; the cells from the room's record, CellsOf;
    /// a receding room's Receding(its first seat, cells), anything decision 4 does not build refused there with a warning as a
    /// plain 9), the goal from the map's door, the far zone's lamp for the goal window light (C42).</summary>
    RunRigLayout Layout(FrontRoomsRunRoom room, Transform root, FrontRoomsMapWorld.Door entranceDoor)
    {
        var cells = CellsOf(room);
        var l = room.Receding ? RunRigLayout.Receding(room.SeatCells[0], cells, FrontRoomsRunRooms.ArchWidth, ModuleUnits.DoorWidth)
            : RunRigLayout.Corridor(cells, FrontRoomsRunRooms.ArchWidth, ModuleUnits.DoorWidth);
        var receding = l.recedeLine >= 0f;
        // Probes by segment (71 s.5.8): the room keeps the bake it reuses under its own name (no cube is shipped today).
        if (string.IsNullOrEmpty(l.probeBake)) l.probeBake = l.name;
        l.name = "Run! room " + room.Id;
        l.wallThickness = ModuleUnits.WallThickness;
        l.buildShell = false;
        l.skinShell = true;
        // Card 4: the Run door entrance, hinged on the jamb the map hangs it from (its latch side carries the card).
        if (entranceDoor != null) l.UseEntranceDoor(ModuleUnits.DoorWidth, HingeSide(root, entranceDoor));
        // The goal door's opening centre (the map's RunEntrance, goal side): the goal head aims at it.
        map.RunEntrance(room, true, out var goalWorld, out var outward);
        var g = root.InverseTransformPoint(goalWorld);
        // The map's goal door and the record's cells must agree (contract); if not, the rig still aims at the map's door. A receding
        // room's goal door is on its L_max edge (the release side) while its goal point is the end's line, so the rig keeps the end's
        // first line: the end carries the goal mount seat by seat (and Restore seats a rebuilt room).
        var mapGoalZ = room.Receding ? room.LengthMetres : g.z;
        if (Mathf.Abs(mapGoalZ - l.length) > .05f)
            Debug.LogWarning("[RunRig] Run! room " + room.Id + ": its record holds " + cells + " cells, the rig " + l.Cells + " (" + l.length.ToString("0.0") + " m), but the map's goal door stands at z " + mapGoalZ.ToString("0.00") + " (contract: W/apply/run_rethink_contract/README.md)");
        l.goalPoint = new Vector3(g.x, l.goalPoint.y, receding ? l.goalPoint.z : g.z);
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
        var goalDoor = map.DoorBetween(GoalCell(room), room.Beyond);
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
    /// the cards under the root (the host's light). A receding room's goal tell stands at its deepest line, where the map's goal
    /// door is (the release side's frame): the card outside it, the detector inside, in the last hidden cell (FrontRoomsRunRecede
    /// keeps it off with that cell until the end uncovers it); the end's own line moves.
    /// </summary>
    void DressTell(Entry e, RunRigLayout l, Transform dress)
    {
        var root = e.root.transform;
        var t = l.wallThickness;
        var depth = l.length - t;
        // The entrance (south): the arch, or card 4's Run door.
        e.archDetector = DressEntranceTell(root, dress, l);
        // The goal door (north): its entrance frame is turned half round at the goal point (a receding room: on the deepest line).
        var gx = l.goalPoint.x;
        var gz = l.recedeLine >= 0f ? l.length : l.goalPoint.z;
        var goalDepth = FrontRoomsAlarmTell.DetectorDepth(t, l.height, ModuleUnits.DoorHeight, depth);
        var goalZ = ClearOfWraps(l, gz - goalDepth, -1f);
        var latch = GoalLatchSide(e.room, root, gx);
        var p = FrontRoomsAlarmTell.CardPoint(ModuleUnits.DoorWidth, t, -latch);   // the entrance frame's +X is the root's -X
        FrontRoomsAlarmTell.DressCard(root, new Vector3(gx - p.x, p.y, gz - p.z), 0f);
        e.goalDetector = FrontRoomsAlarmTell.DressCeiling(dress, new Vector3(gx, l.height, goalZ), 0f);
    }

    /// <summary>
    /// The entrance's tell (the entrance frame is the root's: +Z into the room); returns its detector. The arch (every room today):
    /// the card on the approacher's left (B's NOTICE plate is on the right), the detector at the sight rule's depth for the arch's
    /// 2.2 m head, pushed deeper clear of the strips. Card 4's Run door (RunRigLayout.entranceDoor): the card on the door's latch side,
    /// clear of the leaf's swing and its frame (the goal door's rule), the NOTICE plate already on the hinge side (UseEntranceDoor),
    /// the detector at the sight rule's depth for the door's 2.1 m head (seen from 2 m outside once the leaf is open), pushed deeper
    /// clear of the strips. With the leaf shut the card, the lit F5 lite and the light under the door carry the tell outside.
    /// Public for the tests and harnesses.
    /// </summary>
    public static GameObject DressEntranceTell(Transform root, Transform dress, RunRigLayout l)
    {
        if (root == null || l == null) return null;
        var t = l.wallThickness;
        var depth = l.length - t;
        if (l.entranceDoor)
        {
            var doorDepth = FrontRoomsAlarmTell.DetectorDepth(t, l.height, ModuleUnits.DoorHeight, depth);
            var doorZ = ClearOfWraps(l, doorDepth, 1f);
            FrontRoomsAlarmTell.DressCard(root, FrontRoomsAlarmTell.CardPoint(ModuleUnits.DoorWidth, t, -l.entranceHinge), 180f);
            return FrontRoomsAlarmTell.DressCeiling(dress, new Vector3(0f, l.height, doorZ), 180f);
        }
        var archDepth = FrontRoomsAlarmTell.DetectorDepth(t, l.height, ModuleUnits.ArchTop, depth);
        var archZ = ClearOfWraps(l, archDepth, 1f);
        FrontRoomsAlarmTell.DressCard(root, FrontRoomsAlarmTell.CardPoint(FrontRoomsRunRooms.ArchWidth, t, -1), 180f);
        return FrontRoomsAlarmTell.DressCeiling(dress, new Vector3(0f, l.height, archZ), 180f);
    }

    /// <summary>Card 4: the side the map hung the entrance door's hinge on, in the root's frame (+1: +X), -1 without a hinge.</summary>
    static int HingeSide(Transform root, FrontRoomsMapWorld.Door door)
    {
        if (door == null || door.hinge == null || root == null) return -1;
        return root.InverseTransformPoint(door.hinge.position).x > 0f ? 1 : -1;
    }

    /// <summary>The goal door's latch side in the root's frame (+1: +X): opposite its hinge (the map's door), +1 without one.</summary>
    int GoalLatchSide(FrontRoomsRunRoom room, Transform root, float goalX)
    {
        var door = map.DoorBetween(GoalCell(room), room.Beyond);
        if (door == null || door.hinge == null) return 1;
        var h = root.InverseTransformPoint(door.hinge.position);
        return h.x > goalX ? -1 : 1;
    }

    /// <summary>A ceiling point on the centreline moved along <paramref name="deeper"/> (+1 or -1) until the detector's disc
    /// clears every ceiling lamp of the rig (the R-B strip's 1.28 m, or a wraparound's 1.235 m, plus DetectorClear).</summary>
    static float ClearOfWraps(RunRigLayout l, float z, float deeper)
    {
        var wrapHalf = FrontRoomsRunRig.OwnLampHalfLength(l);
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
