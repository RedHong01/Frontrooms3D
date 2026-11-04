using System;
using System.Collections.Generic;
using System.Reflection;
using FrontRooms.Map;
using UnityEngine;
using UnityEngine.InputSystem;

// Scene 3, menu 6 ROOM TYPES (交互设计, 2026-11-03; scene3/rooms/90_plan.md §3 rows 2 and 4 and §6, 81_roomtypes.md I1-I3): a
// game tour on a DontSave copy of the game's own profile tilted toward variety (81 §5.1), where the guide finds every kind
// of room the game builds on the BUILT chunks round the player (read only: the chunk data, the generator's pure zones, the
// map's records) and jumps the player to each, R1-R13 with a station card each, behind a gate that never jumps during a
// shot, a climb, a pull, the seal or the floor change. K walks the B1 route (the stair key, then the EXIT); L lights a dark
// patch and puts it back as built. The four authored rooms (R2 C, R3 C, R4 B, R5 C) are placed by the tour (90_plan §3 row 4,
// 81 §5.2 and I2): at the run's start, before any chunk is built, the profile's four pool modules go into unbuilt chunks
// round the start door with the cache's own placement (Cache.Place), at a spot the game's own module placement would take
// (off the chunk border, Auto columns on the Level Designer's 6 m phase), their zones are met before the Run! force is
// turned on (no Run! corridor runs through one), and the stream's first build of each chunk stamps it. So floor 1 holds each
// for sure, as the designer drew it, and with them the bullpen's and pillar hall's columns (R6 B, C) and the pillar hall's
// pile (R7); the game's own are listed too. A placement that fails leaves its stop as the game made it, said once on the list.
public sealed partial class FrontRoomsInteractablesShowcase
{
    const int RoomRowCount = 14, RoomVariantMax = 5;
    const float RoomScanSeconds = 1.5f, RoomCardSeconds = 8f, RoomCardReach = 12f, LitLeaveDistance = 15f;
    const int LitMaxCells = 16;

    /// <summary>One stop of the list (R0-R13) and its station card: code, name, the variants, Try and touch (English, the
    /// game's verbs), what it is and what Red decides (Chinese, per variant), the small line (names and the shipped odds).</summary>
    sealed class RoomRow
    {
        public string code, name, tryLine, touch, what, small;
        public string[] variants, decide;
    }

    static readonly RoomRow[] RoomTable =
    {
        new RoomRow { code = "R0", name = "TITLE CORRIDOR", variants = new[] { "TITLE CORRIDOR" },
            tryLine = "Watch the crawl; SPACE starts the run at the next shut door.", touch = "TAP TO START",
            what = "每局开头的标题走廊：5 间 Lobby 房（11.5×12 m，2.9 m），镜头每秒 1.15 m 前行。",
            decide = new[] { "标题走廊的长度和节奏留着吗？Shift / Office / Run / Exit 四种走廊只在编辑器预览里（不在游戏里），要不要让它们进游戏？" },
            small = "FrontRoomsRoomStream, Lobby rooms only in a run; every tour starts here. No jump: the stream owns these cells." },
        new RoomRow { code = "R1", name = "STATION · FLOOR 1", variants = new[] { "FRONT DESK", "EXIT CORRIDOR" },
            tryLine = "A: walk in from the start door. B: walk north 4 cells to the chained EXIT.", touch = "walk in",
            what = "1 层保安岗：12×12 m，柜台、电视、钥匙板、疏散图；北面 4 格走廊通到上锁的 EXIT。",
            decide = new[] { "第一眼能认出是保安岗吗？柜台、电视、钥匙板的位置对吗？", "走廊尽头上锁的 EXIT 拉得住人吗？4 格长度合适吗？" },
            small = "Station_FrontDesk_4x4 (stationModule); every shipped run" },
        new RoomRow { code = "R2", name = "LEVEL 0", variants = new[] { "THE MAZE", "PLAIN ROOM", "WAITING ROOM" },
            tryLine = "Walk the corridor; J: a plain room, then the waiting room.", touch = "walk in",
            what = "Standard 2.9 m 的 Level 0 区：黄墙纸、暖灯，82% 的墙让走廊成迷宫。",
            decide = new[] { "82 % 墙的窄走廊迷宫，密度合适吗？", "空房间（2-4 格）要不要更常见或更少？", "等候室（隔墙、三把椅子、钟、落地灯）读得出“有人等过”吗？留着吗？" },
            small = "HUD LEVEL 0 / THE MAZE · zones 38.5 % shipped (tour 28.6 %) · C: L0_WaitingRoom_4x3, one placed by the tour on floor 1 for sure (game: moduleChance .3, natural 43 %); the game's own listed too" },
        new RoomRow { code = "R3", name = "LOW ROOMS", variants = new[] { "LOW ROOMS", "LOW ROOM", "STORE ROOM" },
            tryLine = "Walk the 2.4 m rooms; J: a plain low room, the dead-end store.", touch = "walk in",
            what = "2.4 m 低矮区：一半是墙、两成是拱，房间 2-3 格。",
            decide = new[] { "2.4 m 的压迫感和 Standard 区分得开吗？50 % 墙 / 20 % 拱合适吗？", "低矮空房间留着这个出现率吗？", "死胡同储物间（书柜、柜子、箱子、托盘）留着吗？" },
            small = "HUD LOW ROOMS · Low 35 % shipped (tour 30 %) · C: Low_Storage_2x3, one placed by the tour on floor 1 for sure (natural 83 %); the game's own listed too" },
        new RoomRow { code = "R4", name = "TALL HALLS", variants = new[] { "TALL HALLS", "PILLAR HALL" },
            tryLine = "Stand in the 5.4 m hall; J: the pillar hall and its pile.", touch = "walk in",
            what = "5.4 m 高厅：只有三成是墙，灯更亮、照得更远，不放钥匙。",
            decide = new[] { "5.4 m 高厅的尺度和灯光够“空旷”吗？", "柱厅（6 m 柱网 + 一堆家具）和普通高厅区别够吗？留着吗？" },
            small = "HUD TALL HALLS · Tall 10 % shipped (tour 18 %) · B: Tall_PillarHall_6x5, one placed by the tour on floor 1 for sure, within 3 chunks of the start door (natural 7.6 %)" },
        new RoomRow { code = "R5", name = "OFFICE", variants = new[] { "WALL ROWS", "PODS", "BULLPEN" },
            tryLine = "Walk the office; J: wall rows, pods, the bullpen.", touch = "walk in",
            what = "办公区（只在 Standard 高度）：冷色灯罩，每个房间都摆办公家具。",
            decide = new[] { "两排靠墙工位的窄办公室读得出办公室吗？", "中间成组的隔间工位密度合适吗？", "开放办公区（复印机、饮水机、文件柜）留着吗？三种办公室都要吗？" },
            small = "HUD LEVEL 4 / OFFICE · 16.5 % shipped (tour 23.4 %) · rows when the short side < 7 m · C: Office_Bullpen_4x4, one placed by the tour on floor 1 for sure (natural 3.7 %)" },
        new RoomRow { code = "R6", name = "COLUMNS", variants = new[] { "LEVEL 0", "OFFICE", "TALL" },
            tryLine = "J compares the three: 0.6 m, 0.9 m with bulkheads, 0.9 m in 5.4 m.", touch = "look",
            what = "房间里 6 m 柱网上的柱子：Level 0 细柱，办公区粗柱带梁，高厅粗柱。",
            decide = new[] { "0.6 m 柱子留着吗？出现率（游戏 25 %）？", "办公区 0.9 m 柱 + 梁头读得出“另一栋楼”吗？", "高厅柱子（游戏 70 %）够多吗？" },
            small = "PlaceColumns, rooms of 3 x 3 cells and more · game odds .25 / .75 / .70 (tour 1 / 1 / 1) · B and C on floor 1 for sure: the placed bullpen's and pillar hall's columns" },
        new RoomRow { code = "R7", name = "FURNITURE PILE", variants = new[] { "CENTRE SCULPTURE", "COPY-PASTE ROW", "CEILING STUCK", "OFFICE CLUSTER", "ZERO PILE" },
            tryLine = "J steps through the piles found; the name is the tableau.", touch = "look",
            what = "房间中央堆起来的家具地标，共五种堆法。",
            decide = new[] { "五种堆法都留吗？出现率（35 %，高厅 60 %）？" },
            small = "FrontRoomsFurniturePile · rooms of 4 x 4 cells and more · game .35 (Tall .6), tour 1 · floor 1: the placed pillar hall carries one (fill Pile); its tableau is the game's pick" },
        new RoomRow { code = "R8", name = "HEIGHT BORDER", variants = new[] { "DOOR", "WINDOW", "ARCH" },
            tryLine = "J: the door 2.4 to 2.9 m, a window into a hall, the hall's one arch.", touch = "walk into the door · hold USE at the window, then walk in · walk through the arch",
            what = "不同高度的区相接处：低和标准之间是门，进高厅是窗，每个高厅只有一个拱门。",
            decide = new[] { "高度变化处用门这条规则读得出来吗？", "窗户作为进出高厅的“第二条路”清楚吗？", "每个高厅只有一个拱门，够找吗？" },
            small = "borderOpening .15 · C33 TallExit · the only generated doors and windows" },
        new RoomRow { code = "R9", name = "LEVEL 0 / OFFICE", variants = new[] { "FRAME", "RENOVATED" },
            tryLine = "Cross into the Office; J: a renovated crossing.", touch = "walk through",
            what = "Level 0 和办公区的交界：每处都有框，约三成在“装修中”。",
            decide = new[] { "门框/拱门的“换楼”处理读得出吗？", "装修中的过渡（游戏 30 %）多一些、少一些还是不要？" },
            small = "FrontRoomsTransitionKit · renovationShare .3 (tour .5)" },
        new RoomRow { code = "R10", name = "DARK PATCH", variants = new[] { "AS BUILT", "LIT" },
            tryLine = "Stand under the dead lamps; L lights the same cells, L again restores them.", touch = "look (L has no touch switch yet)",
            what = "坏灯、暗灯的格子：按层级随机，越往后越多。",
            decide = new[] { "坏灯区多暗算合适？层级越高越多（T5 Dead .12）可以吗？", "同一块区域亮着 vs 坏着，差别够吗？" },
            small = "LampModeOf Dead / Dim · T1 .05 + .03, T5 .12 + .05 · DeadLamp reflection" },
        new RoomRow { code = "R11", name = "RUN! ROOM", variants = new[] { "OUTSIDE", "INSIDE" },
            tryLine = "Walk to it (how far and which way on this card); stand at the arch, walk in: it trips.", touch = "walk in: it trips",
            what = "9 格长的报警走廊：拱门进、尽头一扇门，进去 1.5 m 触发报警、主灯熄灭。",
            decide = new[] { "Run! 房间要在正式版打开吗（现在 runRooms 关）？" },
            small = "FrontRoomsRunRooms · OFF in the shipped game · tour: forced · 2-5 chunks out by design (CR-5): a 70-120 m walk" },
        new RoomRow { code = "R12", name = "STAIRWELL", variants = new[] { "EXIT", "DESCENT", "ARRIVAL" },
            tryLine = "K: the stair key, then the EXIT; E UNLOCK, walk in.", touch = "USE on the key, USE on the EXIT, walk in",
            what = "EXIT 后的一格楼梯间：封门、0.3 秒黑场、换层，下一层从同一个楼梯间出来。",
            decide = new[] { "上锁的楼梯门 + 链条 + EXIT 灯，信号够吗？", "封门 + 0.3 秒黑场这个转场留着吗？", "一格前室 + 北门自动开（没有可走的楼梯）够吗？" },
            small = "FrontRoomsStairwell · ExitSealDepth 1.4 m · blackout .3 s" },
        new RoomRow { code = "R13", name = "B1 STATION", variants = new[] { "WAITING", "DESK" },
            tryLine = "On B1, 10-20 cells from the stairwell; N on floor 1's title picks which one.", touch = "walk in",
            what = "B 层岗亭：在 Level 0 区是等候式，在办公区是办公桌式；它的钥匙锁着这一层的 EXIT。",
            decide = new[] { "等候式岗亭（五把椅子）留着吗？离到达门 10-20 格合适吗？", "办公式岗亭留着吗？两种都要吗？" },
            small = "Station_Waiting_3x2 / Station_Desk_3x3 · shipped B1 72.8 % / 27.2 % · tour 94 % / 84 % on B1" },
    };

    /// <summary>One example of a stop on the built cells round the player: where it is, where to stand, what to face.</summary>
    sealed class RoomStop
    {
        public int row, variant;
        public Vector3 at, stand, look;
        public float distance, ring;
        public bool jumpable;
        public string detail;
        public GridCoord cell;
    }

    // What the last scan found, per row and variant (index row * RoomVariantMax + variant), nearest first.
    readonly List<RoomStop>[] roomFound = NewRoomLists();
    readonly List<RoomStop> roomPool = new List<RoomStop>();
    int roomPoolUsed;
    readonly Comparison<RoomStop> byStopDistance = (x, y) => x.distance.CompareTo(y.distance);
    // While a chunk is scanned: the best example of each kind in it (one per chunk keeps the lists short).
    readonly RoomStop[] chunkBest = new RoomStop[RoomRowCount * RoomVariantMax];
    readonly float[] chunkScore = new float[RoomRowCount * RoomVariantMax];
    readonly List<Vector3> pileAt = new List<Vector3>();
    float roomsClock;
    // The list: its page, the row and variant last shown (J steps from there), the example index within it.
    int roomsPage, roomsRow = -1, roomsVariant, roomsIndex;
    // The station card: the stop it shows, until when, and where (it stays while the player is near).
    int cardRow = -1, cardVariant;
    float cardUntil;
    Vector3 cardAt;
    // The station the floors below the first take (N on the title): Station · Waiting (Level 0) or Station · Desk (Office).
    bool roomsDesk;
    FrontRoomsRoomModule roomsWaitingModule, roomsDeskModule;
    // R10 B: the cells L set Steady through the map's own lamp modes, put back as built (Auto) on L, on leaving, F2 and OnDestroy.
    readonly List<GridCoord> litCells = new List<GridCoord>();
    readonly List<GridCoord> litQueue = new List<GridCoord>();
    readonly HashSet<GridCoord> litSeen = new HashSet<GridCoord>();
    Vector3 litAt;
    int litDead, litDim;
    static bool gateWarned;

    // I2: the pool modules placed at the run's start (floor 1's cache only; NextFloor's new cache has none).
    const int PlaceRings = 3, PlaceModulesMax = 8;

    /// <summary>One pool module the tour placed at the run's start, or could not: the stop it shows as, the chunk, the footprint
    /// in chunk-local cells (as RoomModuleStamp.Apply appends it to the chunk's rooms), its middle in the world, and why not.</summary>
    sealed class RoomPlacement
    {
        public string name, why;
        public int row, variant, ring, turns;
        public GridCoord chunk;
        public CellRect rect;
        public RoomModuleData module;
        public Vector3 centre;
        // fits: a footprint was found (phase 1, pure); placed: the cache holds it (phase 2).
        public bool fits, placed;
        // 0 not generated yet, 1 the chunk's data holds the stamped room, -1 the chunk was generated without it (said once).
        public int stamped;
    }

    readonly List<RoomPlacement> roomPlacements = new List<RoomPlacement>();
    readonly List<FrontRoomsRoomModule> placeQueue = new List<FrontRoomsRoomModule>();
    readonly List<RoomModuleData> placeTurns = new List<RoomModuleData>();
    readonly HashSet<GridCoord> placeUsed = new HashSet<GridCoord>();
    readonly HashSet<GridCoord> placeTouched = new HashSet<GridCoord>();
    // This run's floor 1 had its placements tried (the floors below say "floor 1 only").
    bool placementsRan;

    // The jump gate reads the game's own private state, as its seal does (FrontRooms3DGame.ExitSealAllowed): by name and type,
    // read only. A field not found with that type turns its check off, said once on the list.
    const BindingFlags GameFields = BindingFlags.Instance | BindingFlags.NonPublic;
    static readonly FieldInfo PhaseField = GameField("phase", null);
    static readonly FieldInfo FloorSwapField = GameField("floorSwap", null);
    static readonly FieldInfo RigField = GameField("rig", typeof(FrontRoomsCameraRig));
    static readonly FieldInfo GlassShotField = GameField("glassShot", typeof(FrontRoomsGlassShot));
    static readonly FieldInfo DoorShotField = GameField("doorShot", typeof(FrontRoomsDoorShot));
    static readonly FieldInfo ClimbField = GameField("climbTime", typeof(float));
    static readonly FieldInfo PullField = GameField("pullClear", typeof(Vector3?));

    // A private field of the game by name, kept only with the type the gate reads it as (null type: an enum read by name).
    static FieldInfo GameField(string name, Type type)
    {
        var f = typeof(FrontRooms3DGame).GetField(name, GameFields);
        if (f == null) return null;
        return (type == null ? f.FieldType.IsEnum : f.FieldType == type) ? f : null;
    }

    static string GateMissing()
    {
        var missing = "";
        if (PhaseField == null) missing += " phase";
        if (FloorSwapField == null) missing += " floorSwap";
        if (RigField == null) missing += " rig";
        if (GlassShotField == null) missing += " glassShot";
        if (DoorShotField == null) missing += " doorShot";
        if (ClimbField == null) missing += " climbTime";
        if (PullField == null) missing += " pullClear";
        return missing.Length == 0 ? null : "Jump gate: FrontRooms3DGame has no" + missing + " (renamed?); those checks are off.";
    }

    static List<RoomStop>[] NewRoomLists()
    {
        var lists = new List<RoomStop>[RoomRowCount * RoomVariantMax];
        for (var i = 0; i < lists.Length; i++) lists[i] = new List<RoomStop>();
        return lists;
    }

    static RoomRow RoomRowAt(int row) => row >= 0 && row < RoomTable.Length ? RoomTable[row] : null;

    static int VariantCount(int row)
    {
        var r = RoomRowAt(row);
        return r == null || r.variants == null ? 0 : Mathf.Min(r.variants.Length, RoomVariantMax);
    }

    /// <summary>"R2 B  PLAIN ROOM"; a stop with one exhibit has no letter ("R0  TITLE CORRIDOR").</summary>
    static string RoomLabel(int row, int variant)
    {
        var r = RoomRowAt(row);
        if (r == null) return "";
        var n = VariantCount(row);
        var v = Mathf.Clamp(variant, 0, Mathf.Max(0, n - 1));
        return r.code + (n > 1 ? " " + (char)('A' + v) : "") + "  " + (n > 0 ? r.variants[v] : r.name);
    }

    static string DecideOf(RoomRow r, int variant)
    {
        if (r == null || r.decide == null || r.decide.Length == 0) return "";
        return r.decide[Mathf.Clamp(variant, 0, r.decide.Length - 1)];
    }

    List<RoomStop> RoomList(int row, int variant) =>
        roomFound[Mathf.Clamp(row, 0, RoomRowCount - 1) * RoomVariantMax + Mathf.Clamp(variant, 0, RoomVariantMax - 1)];

    /// <summary>The card's and the list's title for a stop: RoomLabel, but R7 under its own name ("R7  FURNITURE PILE") while
    /// no pile of that tableau is on the scan (no pile seen yet: key 7 opens variant A), since the placed pillar hall's
    /// tableau is the game's pick, not A's.</summary>
    string CardLabel(int row, int variant)
    {
        if (row != 7 || RoomList(7, variant).Count > 0) return RoomLabel(row, variant);
        var r = RoomRowAt(7);
        return r.code + "  " + r.name;
    }

    // Any furniture pile on the last scan (R7, any tableau).
    bool PileSeen()
    {
        for (var v = 0; v < VariantCount(7); v++) if (RoomList(7, v).Count > 0) return true;
        return false;
    }

    // ---------- the tour's copy and its run (called from the landed tour code)

    /// <summary>
    /// The ROOM TYPES copy (81 §5.1, variant A2): zones Low .30 / Standard .52 / Tall .18 with officeShare .45, columns in every
    /// room that may hold them, a pile in every room big enough, half the Level 0 | Office crossings renovated, no revisit
    /// shift (the rooms the list points at stay put), Run! rooms on; the devices off; the floor-1 station kept; the B1
    /// station of the N choice; body doors, keys, transitions and offices as shipped. Only this DontSave copy changes.
    /// </summary>
    void ApplyRoomsCopy(FrontRoomsLevelProfile source)
    {
        var g = tourProfile.generation;
        if (g != null)
        {
            g.lowShare = .30f;
            g.standardShare = .52f;
            g.tallShare = .18f;
            g.officeShare = .45f;
            g.level0ColumnRooms = g.officeColumnRooms = g.tallColumnHalls = 1f;
        }
        tourProfile.pileChance = 1f;
        tourProfile.renovationShare = .5f;
        tourProfile.shiftAfterSeconds = 1e6f;
        tourProfile.runRooms = true;
        tourProfile.sashPry = tourProfile.foil = tourProfile.alarmDoor = false;
        roomsWaitingModule = source.stationWaiting;
        roomsDeskModule = source.stationDesk;
        ApplyB1Choice();
        // As shipped: no test-bed close-ups on the kits (V still turns them on).
        devTargets = false;
        FrontRoomsInspect.DevTargets = false;
    }

    // The game reads the floor station modules from the map's profile (this copy) the first time a floor below asks.
    void ApplyB1Choice()
    {
        if (tourProfile == null) return;
        tourProfile.stationWaiting = roomsDesk ? null : roomsWaitingModule;
        tourProfile.stationDesk = roomsDesk ? roomsDeskModule : null;
    }

    void RoomsRunStarted()
    {
        litCells.Clear();
        ClearRoomFound();
        roomsRow = -1;
        roomsVariant = roomsIndex = 0;
        cardRow = -1;
        roomsClock = 0f;
        // I2 (81 §5.2): the pool modules into unbuilt chunks and their zones met while the Run! force is still off, so those
        // zones are decided "no Run! room" for good (FrontRoomsRunRooms.Meet, ZonesStillOk); only then the force. It never
        // throws (a failure is one line on the list), so the force below is always set: without placements the tour is as before.
        PlaceRoomModules();
        if (map.RunRooms != null) map.RunRooms.Rules.force = true;
        var missing = GateMissing();
        if (missing == null) return;
        problem = problem == null ? missing : problem + " " + missing;
        if (gateWarned) return;
        gateWarned = true;
        Debug.LogWarning("Interactables showcase: " + missing);
    }

    // NextFloor made a new Run! planner and cleared every lamp mode: force the planner again, forget the lit cells. Its new
    // cache holds no placement (81 §5.3: none on the floors below), so the records go too.
    void RoomsFloorChanged()
    {
        if (map.RunRooms != null) map.RunRooms.Rules.force = true;
        litCells.Clear();
        ClearRoomFound();
        cardRow = -1;
        roomsClock = 0f;
        roomPlacements.Clear();
    }

    void RoomsRunEnded()
    {
        litCells.Clear();
        ClearRoomFound();
        cardRow = -1;
        roomPlacements.Clear();
        placementsRan = false;
    }

    // ---------- I2: the four placements (81 §5.2 and §8 I2; 90_plan §3 row 4)

    /// <summary>
    /// MapRunStarted, before any chunk is built (StartRunInPlace calls Begin(playerRoot, false), which builds none, then raises
    /// MapRunStarted; the stream builds from MapWorld.Update on): each pool module of the map's profile goes into the nearest
    /// unbuilt chunk round the start door where one turn of it fits, at the kind of spot the game's own module placement takes
    /// (FitModule). Phase 1 only reads (the generator's pure zones, the carve, the planners), so a throw there leaves the map
    /// as the game made it. Phase 2 writes:
    /// the footprint's zones met (Cache.ZoneOf: with the Run! force still off they get no Run! room, ever), then the cache's
    /// own placement (Cache.Place: data only; the stream's first Build of that chunk stamps it with RoomModuleStamp, the
    /// generator's own path). Never throws: whatever fails is one line on the list and one console warning, and that stop
    /// shows only where the game made one.
    /// </summary>
    void PlaceRoomModules()
    {
        roomPlacements.Clear();
        placementsRan = true;
        string failed = null;
        Exception thrown = null;
        try
        {
            var cache = map.Cache;
            if (cache == null) failed = "the map has no cache";
            else
            {
                CollectPoolModules(map.Profile);
                if (placeQueue.Count == 0) failed = "the profile holds no pool module";
                var door = StartDoorCell();
                var centre = MapGrid.ChunkOf(door);
                placeUsed.Clear();
                placeTouched.Clear();
                if (cache.Carve != null)
                    foreach (var c in cache.Carve.Touched) placeTouched.Add(c);
                // Phase 1: every fit first, in the generator's module order (by name), one module per chunk.
                for (var k = 0; k < placeQueue.Count; k++)
                {
                    var asset = placeQueue[k];
                    // Our own copy: the stamp normalises the data it is given (RoomModuleStamp.Apply), never the asset's.
                    var data = asset.data.Clone();
                    data.Normalize();
                    var p = new RoomPlacement { name = asset.name };
                    ModuleStop(data, out p.row, out p.variant);
                    GridCoord chunk;
                    RoomModuleData turned;
                    int x0, y0, ring, turns;
                    if (FitModule(data, door, centre, out chunk, out turned, out x0, out y0, out ring, out turns))
                    {
                        p.fits = true;
                        p.chunk = chunk;
                        p.module = turned;
                        p.rect = new CellRect(x0, y0, turned.width, turned.depth);
                        p.ring = ring;
                        p.turns = turns;
                        var o = MapGrid.ChunkOrigin(chunk);
                        p.centre = (map.CellCenter(new GridCoord(o.x + x0, o.y + y0))
                            + map.CellCenter(new GridCoord(o.x + x0 + turned.width - 1, o.y + y0 + turned.depth - 1))) * .5f;
                        placeUsed.Add(chunk);
                    }
                    else p.why = "no unbuilt chunk within " + PlaceRings + " chunks of the start door has room for it";
                    roomPlacements.Add(p);
                }
                // Phase 2: the zones met, then the placement, one module at a time. The force is off while they are met (a
                // tools run, RunRoomsForTools >= 2, attaches the planner forced): the caller turns it on right after.
                if (map.RunRooms != null) map.RunRooms.Rules.force = false;
                for (var k = 0; k < roomPlacements.Count; k++)
                {
                    var p = roomPlacements[k];
                    if (!p.fits) continue;
                    MeetPlacedZones(p);
                    cache.Place(p.chunk, p.module, p.rect.x, p.rect.y);
                    p.placed = true;
                }
            }
        }
        catch (Exception e)
        {
            thrown = e;
            failed = "the placement threw (" + e.GetType().Name + ": " + e.Message + ")";
        }
        var placedLog = "";
        var placedLabels = "";
        var missed = "";
        for (var k = 0; k < roomPlacements.Count; k++)
        {
            var p = roomPlacements[k];
            if (p.placed)
            {
                placedLog += (placedLog.Length > 0 ? "; " : "") + p.name + " in chunk " + p.chunk + " (ring " + p.ring + ") at "
                    + p.rect.x + ", " + p.rect.y + ", " + p.rect.w + " x " + p.rect.h + " cells, turned " + p.turns + SpotNote(p);
                placedLabels += (placedLabels.Length > 0 ? ", " : "") + RoomLabel(p.row, p.variant);
                continue;
            }
            if (p.why == null) p.why = failed ?? "not placed";
            missed += (missed.Length > 0 ? ", " : "") + RoomLabel(p.row, p.variant);
        }
        if (placedLog.Length > 0) Debug.Log("Interactables showcase (rooms): placed " + placedLog + ".");
        if (failed == null && missed.Length == 0) return;
        // One line for the list and the console: what failed, what still stands, and that the rest is the game's own.
        var line = failed != null
            ? "PLACEMENTS: " + failed + (placedLabels.Length > 0 ? "; placed: " + placedLabels : "") + "; the authored rooms not placed show only where the game made one."
            : "PLACEMENTS: " + missed + " not placed (no unbuilt chunk within " + PlaceRings + " chunks of the start door has room): only where the game made one.";
        problem = problem == null ? line : problem + " " + line;
        Debug.LogWarning("Interactables showcase (rooms): " + line + (thrown != null ? "\n" + thrown : ""));
    }

    /// <summary>The profile's pool modules (each once, room modules only: no station, no corridor or trigger module, none the
    /// generator drops), in the generator's own order (by name, FrontRoomsLevelProfile.ModuleData), at most PlaceModulesMax.</summary>
    void CollectPoolModules(FrontRoomsLevelProfile profile)
    {
        placeQueue.Clear();
        if (profile == null || profile.modules == null) return;
        for (var k = 0; k < profile.modules.Length && placeQueue.Count < PlaceModulesMax; k++)
        {
            var m = profile.modules[k];
            if (m == null || m.data == null || placeQueue.Contains(m)) continue;
            if (m.data.IsStation || m.data.placement != ModulePlacement.Room || m.data.trigger != ModuleTrigger.None) continue;
            // Weight 0 (or NaN, or below) is the generator's "never" (its constructor drops it, FrontRoomsMap.cs:293): a room
            // retired that way is not placed either. Its tier range is not read: a module moved to later tiers still shows here.
            if (!(m.data.weight > 0f)) continue;
            var at = placeQueue.Count;
            while (at > 0 && string.CompareOrdinal(placeQueue[at - 1].name, m.name) > 0) at--;
            placeQueue.Insert(at, m);
        }
    }

    /// <summary>D, the start door's cell: the floor-1 station's (its layout), else the Run! planner's start, else the stream's focus.</summary>
    GridCoord StartDoorCell()
    {
        if (map.HasStation && !map.StationLayout.floorStation) return map.StationLayout.door;
        var run = map.RunRooms;
        if (run != null && run.HasStart) return run.StartDoor;
        return map.CellOf(map.StreamFocus ?? Feet);
    }

    /// <summary>The stop a module shows as, as the finder reads a module room: Low R3 C, Tall R4 B, Office R5 C, else R2 C.</summary>
    static void ModuleStop(RoomModuleData m, out int row, out int variant)
    {
        if (m.height == ZoneHeight.Low) { row = 3; variant = 2; }
        else if (m.height == ZoneHeight.Tall) { row = 4; variant = 1; }
        else if (m.theme == ZoneTheme.Office) { row = 5; variant = 2; }
        else { row = 2; variant = 2; }
    }

    /// <summary>
    /// 81's FindFit (the census's rule, scene3/rooms/sim_rooms/Program.cs:363-400) with the game's own spot rule first: the
    /// first chunk in rings 1, 2, then 3 round the start door's chunk (in a ring, south to north, west to east) that no earlier
    /// placement took, the carve neither pins nor touches, no Run! room uses and nothing has built, with one turn of the module
    /// whose every footprint cell has the module's height and theme (RoomModuleStamp.Apply refuses any other,
    /// FrontRoomsRoomModuleStamp.cs:32-43) and none is reserved (the start rooms, X), the station's (its room, C1..N, X) or
    /// beside the start door. Three passes over rings 1-3 (SpotAllowed), so a farther chunk with the game's kind of spot wins
    /// over a nearer one without: 0, the spot FrontRoomsMapGenerator.PlaceModules prefers (Spot, FrontRoomsMap.cs:552-580):
    /// off every chunk border (Apply writes only edges inside the chunk, so on a border side the map's own edges would stand
    /// for the designer's walls and doorways) and, for Auto columns, on the 6 m column phase the Level Designer preview shows;
    /// 1, Auto columns only: on that phase, a border allowed (the game's own choice when a room leaves no inner spot); 2, any
    /// spot (rare: the room then differs from its design on that side or in its columns, said by SpotNote). Reads only.
    /// </summary>
    bool FitModule(RoomModuleData module, GridCoord door, GridCoord centre, out GridCoord chunk, out RoomModuleData turned, out int x0, out int y0, out int ringOut, out int turnsOut)
    {
        chunk = default;
        turned = null;
        x0 = y0 = 0;
        ringOut = turnsOut = -1;
        const int n = MapGrid.ChunkCells;
        var cache = map.Cache;
        var gen = cache.Generator;
        var run = map.RunRooms;
        placeTurns.Clear();
        for (var t = 0; t < (module.allowRotate ? 4 : 1); t++)
        {
            var m = module.Rotated(t);
            m.Normalize();
            placeTurns.Add(m);
        }
        for (var pass = 0; pass < 3; pass++)
        {
            // Pass 1 differs from pass 2 only for Auto columns.
            if (pass == 1 && module.columns != ModuleColumns.Auto) continue;
            for (var ring = 1; ring <= PlaceRings; ring++)
            for (var dy = -ring; dy <= ring; dy++)
            for (var dx = -ring; dx <= ring; dx++)
            {
                if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != ring) continue;
                var c = new GridCoord(centre.x + dx, centre.y + dy);
                var o = MapGrid.ChunkOrigin(c);
                if (placeUsed.Contains(c) || placeTouched.Contains(c) || cache.IsPinned(c) || map.IsChunkBuiltAt(o)) continue;
                if (run != null && run.RoomsIn(c).Count > 0) continue;
                // Never X's (and N's) chunk: the EXIT was chosen on that chunk's data (a dead end, the chunk whole without X,
                // FrontRoomsExitPlanner), which a stamp there could undo. On floor 1 it is the carve's anyway; this keeps a start
                // without a station safe too.
                if (map.HasExitArea && (MapGrid.ChunkOf(map.ExitCell) == c || MapGrid.ChunkOf(map.ExitFrontCell) == c)) continue;
                for (var t = 0; t < placeTurns.Count; t++)
                {
                    var m = placeTurns[t];
                    for (var y = 0; y + m.depth <= n; y++)
                    for (var x = 0; x + m.width <= n; x++)
                    {
                        if (!SpotAllowed(pass, m, x, y) || !FootprintFits(gen, m, o, x, y, door)) continue;
                        chunk = c;
                        turned = m;
                        x0 = x;
                        y0 = y;
                        ringOut = ring;
                        turnsOut = t;
                        return true;
                    }
                }
            }
        }
        return false;
    }

    /// <summary>
    /// FitModule's passes as a test of one chunk-local start of a turned module, by the generator's own spot rule
    /// (FrontRoomsMapGenerator.Spot and Pick, FrontRoomsMap.cs:572-589): pass 0, off every chunk border (x, y >= 1 and the far
    /// side <= 7) and, for Auto columns, at an even distance from (8 - side) / 2 on both axes (the phase the Level Designer
    /// preview shows, RoomModuleData.AutoColumnAt); pass 1, that phase only; pass 2, any start.
    /// </summary>
    static bool SpotAllowed(int pass, RoomModuleData m, int x, int y)
    {
        const int n = MapGrid.ChunkCells;
        if (pass >= 2) return true;
        if (m.columns == ModuleColumns.Auto && (((x - (n - m.width) / 2) & 1) != 0 || ((y - (n - m.depth) / 2) & 1) != 0)) return false;
        return pass == 1 || (x >= 1 && y >= 1 && x + m.width <= n - 1 && y + m.depth <= n - 1);
    }

    /// <summary>"" for a placement at the game's own kind of spot; else what differs from the design this run (the console's
    /// placed line, the finder's detail): a side on a chunk border (the map's edges there), columns off the designer's grid.</summary>
    static string SpotNote(RoomPlacement p)
    {
        if (p == null || p.module == null) return "";
        const int n = MapGrid.ChunkCells;
        var r = p.rect;
        var border = r.x == 0 || r.y == 0 || r.x + r.w == n || r.y + r.h == n;
        var offGrid = !SpotAllowed(1, p.module, r.x, r.y);
        if (!border && !offGrid) return "";
        return " (this run " + (border ? "on a chunk border: that side is the map's" : "") + (border && offGrid ? "; " : "")
            + (offGrid ? "its columns off the designer's grid" : "") + ")";
    }

    bool FootprintFits(FrontRoomsMapGenerator gen, RoomModuleData m, GridCoord o, int x, int y, GridCoord door)
    {
        for (var j = 0; j < m.depth; j++)
        for (var i = 0; i < m.width; i++)
        {
            var cell = new GridCoord(o.x + x + i, o.y + y + j);
            var zone = gen.ZoneOf(cell);
            if (zone.height != m.height || zone.theme != m.theme) return false;
            if (map.IsReserved(cell) || InStationArea(cell)) return false;
            // The start door's cell and the ring round it stay as the map dresses them (inside the station on floor 1 anyway).
            if (Mathf.Abs(cell.x - door.x) <= 1 && Mathf.Abs(cell.y - door.y) <= 1) return false;
        }
        return true;
    }

    // Every zone a placed footprint's cells belong to, met now through the cache (it records the zone's tier and hands it to the
    // Run! planner's Meet): with the force off and the first send not out, the planner decides "none" for it and refuses every
    // later corridor touching it (ZonesStillOk). Nothing to meet with Run! rooms off.
    void MeetPlacedZones(RoomPlacement p)
    {
        if (map.RunRooms == null) return;
        var o = MapGrid.ChunkOrigin(p.chunk);
        for (var j = 0; j < p.rect.h; j++)
        for (var i = 0; i < p.rect.w; i++)
            map.Cache.ZoneOf(new GridCoord(o.x + p.rect.x + i, o.y + p.rect.y + j));
    }

    /// <summary>Each scan: a placed room whose chunk the cache has generated since either stands in its data or does not (said once).</summary>
    void CheckPlacements()
    {
        if (floor > 1 || map == null || map.Cache == null) return;
        for (var k = 0; k < roomPlacements.Count; k++)
        {
            var p = roomPlacements[k];
            if (!p.placed || p.stamped != 0) continue;
            MapChunk data;
            if (!map.Cache.TryGetGenerated(p.chunk, out data) || data == null) continue;
            p.stamped = HoldsRoom(data, p.rect) ? 1 : -1;
            if (p.stamped > 0) continue;
            var line = "PLACEMENT LEFT OUT: " + RoomLabel(p.row, p.variant) + " (" + p.name + ") in chunk " + p.chunk + ": the map's stamp refused it; it shows only where the game made one.";
            problem = problem == null ? line : problem + " " + line;
            Debug.LogWarning("Interactables showcase (rooms): " + line);
        }
    }

    static bool HoldsRoom(MapChunk data, CellRect rect)
    {
        if (data.rooms == null) return false;
        for (var r = 0; r < data.rooms.Length; r++)
            if (SameRect(data.rooms[r], rect) && data.ModuleOf(r) != null) return true;
        return false;
    }

    static bool SameRect(CellRect a, CellRect b) => a.x == b.x && a.y == b.y && a.w == b.w && a.h == b.h;

    /// <summary>The placement behind a module room of this floor (the scan's detail line), by chunk and footprint, or null.</summary>
    RoomPlacement PlacedRoomFor(GridCoord chunk, CellRect rect)
    {
        if (floor > 1) return null;
        for (var k = 0; k < roomPlacements.Count; k++)
        {
            var p = roomPlacements[k];
            if (p.placed && p.chunk == chunk && SameRect(p.rect, rect)) return p;
        }
        return null;
    }

    // The placed footprint holding a map cell, or null (a pile's detail line).
    RoomPlacement PlacedRoomAt(GridCoord cell)
    {
        if (floor > 1) return null;
        for (var k = 0; k < roomPlacements.Count; k++)
        {
            var p = roomPlacements[k];
            if (!p.placed || MapGrid.ChunkOf(cell) != p.chunk) continue;
            var o = MapGrid.ChunkOrigin(p.chunk);
            if (p.rect.Contains(cell.x - o.x, cell.y - o.y)) return p;
        }
        return null;
    }

    // The module a variant leans on: R2 C, R3 C, R4 B, R5 C are the modules themselves; R6 B stands in the bullpen (its columns),
    // R6 C in the pillar hall, R7 in the pillar hall (its fill Pile). False: a variant no placement guarantees.
    static bool PlacedStop(int row, int v, out int moduleRow, out int moduleVariant)
    {
        moduleRow = row;
        moduleVariant = v;
        if ((row == 2 && v == 2) || (row == 3 && v == 2) || (row == 4 && v == 1) || (row == 5 && v == 2)) return true;
        if (row == 6 && v == 1) { moduleRow = 5; moduleVariant = 2; return true; }
        if (row == 6 && v == 2) { moduleRow = 4; moduleVariant = 1; return true; }
        if (row == 7) { moduleRow = 4; moduleVariant = 1; return true; }
        return false;
    }

    static bool IsModuleStop(int row, int v) => (row == 2 && v == 2) || (row == 3 && v == 2) || (row == 4 && v == 1) || (row == 5 && v == 2);

    /// <summary>This floor's placement behind a variant: a placed one first, then the nearest; null on the floors below, with
    /// none tried, or for a variant no placement guarantees.</summary>
    RoomPlacement PlacementFor(int row, int v)
    {
        int mr, mv;
        if (floor > 1 || !PlacedStop(row, v, out mr, out mv)) return null;
        RoomPlacement best = null;
        var feet = Feet;
        for (var k = 0; k < roomPlacements.Count; k++)
        {
            var p = roomPlacements[k];
            if (p.row != mr || p.variant != mv) continue;
            if (best == null || (p.placed && !best.placed)
                || (p.placed == best.placed && Vector3.Distance(feet, p.centre) < Vector3.Distance(feet, best.centre))) best = p;
        }
        return best;
    }

    /// <summary>A placed room to point at while the scan has no example of the variant: placed, not refused by the stamp; for
    /// the columns and the pile only while that room is not built round the player yet (then the scan says what it holds).</summary>
    RoomPlacement PlacedHint(int row, int v)
    {
        var p = PlacementFor(row, v);
        if (p == null || !p.placed || p.stamped < 0) return null;
        if ((row == 6 || row == 7) && map != null && map.IsChunkBuiltAt(MapGrid.ChunkOrigin(p.chunk)) && InScanRange(p.chunk)) return null;
        return p;
    }

    /// <summary>The card's line for a variant a placement stands behind while the scan has no example of it: where the placed
    /// room is and which way, or why there is none (no fit, the stamp left it out, a floor below). Null: say what the scan says.</summary>
    string PlacedLine(int row, int v)
    {
        var own = IsModuleStop(row, v);
        if (floor > 1) return own && placementsRan ? "The tour places it on floor 1 only: on this floor only where the game made one; walk on." : null;
        var p = PlacementFor(row, v);
        if (p == null) return null;
        if (!p.placed) return own ? "Not placed this run (" + p.why + "): only where the game made one; walk on." : null;
        if (p.stamped < 0) return own ? "Placed in chunk " + p.chunk + ", but the map's stamp left it out: only where the game made one." : null;
        var hint = PlacedHint(row, v);
        if (hint == null) return null;
        // R7: the hall's pile is FrontRoomsFurniturePile's own pick (and stands where PileSpot finds room): no tableau named.
        var what = row == 6 ? "In the placed " + (v == 1 ? "bullpen" : "pillar hall") + ", its columns" : row == 7 ? "In the placed pillar hall, a pile (its tableau shows once seen)" : "Placed by the tour";
        var built = map != null && map.IsChunkBuiltAt(MapGrid.ChunkOrigin(hint.chunk));
        return what + ": " + Mathf.RoundToInt(Vector3.Distance(Feet, hint.centre)) + " m, " + Bearing(hint.centre)
            + (built ? ", built  ·  walk that way: once it is round you, the list jumps you in." : ", not built yet  ·  walk that way: it builds as you come near.");
    }

    int ScanRadius => map != null ? Mathf.Clamp(map.BuildRadius, 1, 3) : 1;

    // A chunk the scan reads (within ScanRadius chunks of the player's).
    bool InScanRange(GridCoord chunk)
    {
        if (map == null || player == null) return false;
        var here = MapGrid.ChunkOf(map.CellOf(Feet));
        var radius = ScanRadius;
        return Mathf.Abs(chunk.x - here.x) <= radius && Mathf.Abs(chunk.y - here.y) <= radius;
    }

    /// <summary>Every lamp L lit back to the map's own (Auto). On L, on leaving the patch, F2 and OnDestroy.</summary>
    void RestoreLamps()
    {
        if (litCells.Count == 0) return;
        if (map != null)
            for (var i = 0; i < litCells.Count; i++) map.SetLampMode(litCells[i], ModuleLamp.Auto);
        litCells.Clear();
    }

    // ---------- per frame (the tour's Update)

    /// <summary>The title, before SPACE: N picks the station of the floors below the first for this run.</summary>
    void UpdateRoomsTitle(Keyboard keys)
    {
        if (keys == null || paused || !keys.nKey.wasPressedThisFrame) return;
        roomsDesk = !roomsDesk;
        ApplyB1Choice();
    }

    void TickRooms()
    {
        roomsClock -= Time.unscaledDeltaTime;
        if (roomsClock <= 0f)
        {
            roomsClock = RoomScanSeconds;
            ScanRooms();
        }
        if (litCells.Count > 0 && (player == null || Vector3.Distance(Feet, litAt) > LitLeaveDistance))
        {
            RestoreLamps();
            Toast(RoomLabel(10, 0), "You left the patch: its lamps are back as the map built them.");
        }
    }

    /// <summary>The list's keys: 1-0 a row ([ ] the page), J the next variant or example, K the B1 route, L the lamps.</summary>
    void UpdateRoomsKeys(Keyboard keys)
    {
        if (keys.leftBracketKey.wasPressedThisFrame || keys.pageUpKey.wasPressedThisFrame
            || keys.rightBracketKey.wasPressedThisFrame || keys.pageDownKey.wasPressedThisFrame) roomsPage = 1 - roomsPage;
        if (keys.kKey.wasPressedThisFrame) StairRoute();
        if (keys.lKey.wasPressedThisFrame) ToggleLights();
        if (keys.jKey.wasPressedThisFrame) NextRoomVariant();
        var digit = DigitDown(keys);
        if (digit < 0) return;
        var row = (roomsPage == 0 ? 1 : 11) + digit;
        if (row <= (roomsPage == 0 ? 10 : 13)) ShowRoomRow(row);
    }

    // ---------- choosing and jumping

    void ShowRoomRow(int row)
    {
        roomsRow = row;
        roomsIndex = 0;
        for (var v = 0; v < VariantCount(row); v++)
            if (JumpTarget(row, v, 0) != null)
            {
                ShowRoomVariant(row, v);
                return;
            }
        // Nothing to stand at yet: a placed room's card (where it is, which way), else the first variant's.
        for (var v = 0; v < VariantCount(row); v++)
            if (row >= 2 && row <= 6 && PlacedHint(row, v) != null)
            {
                ShowRoomVariant(row, v);
                return;
            }
        ShowRoomVariant(row, row == 13 && roomsDesk ? 1 : 0);
    }

    void NextRoomVariant()
    {
        if (roomsRow < 1)
        {
            Toast(TourName(tour), "Pick a stop first: 1-0, [ ] for page 2.");
            return;
        }
        var n = VariantCount(roomsRow);
        for (var k = 1; k < n; k++)
        {
            var v = (roomsVariant + k) % n;
            if (!Showable(roomsRow, v)) continue;
            // A variant only a placement makes showable (its card, no example yet) waits until this one's examples are walked.
            if (roomsRow >= 2 && roomsRow <= 6 && JumpTarget(roomsRow, v, 0) == null && roomsIndex + 1 < JumpableCount(roomsRow, roomsVariant)) continue;
            roomsIndex = 0;
            ShowRoomVariant(roomsRow, v);
            return;
        }
        // One kind here only: its next example.
        roomsIndex++;
        ShowRoomVariant(roomsRow, roomsVariant);
    }

    bool Showable(int row, int v)
    {
        if (row == 10 && v == 1) return litCells.Count > 0 || JumpTarget(10, 0, 0) != null;
        if (row == 11 && v == 1) return RoomList(11, 0).Count > 0;
        if (row == 12 && v == 1) return map != null && map.HasExitArea;
        if (row == 12 && v == 2) return floor > 1;
        if (JumpTarget(row, v, 0) != null) return true;
        // A placed room not built (or not near) yet still has its card: J shows where it is (R7's tableau only once seen).
        return row != 7 && PlacedHint(row, v) != null;
    }

    /// <summary>The index-th example of a variant to stand at (wrapping), or null: none found, or one the guide never jumps into.</summary>
    RoomStop JumpTarget(int row, int v, int index)
    {
        if (row == 10 && v == 1) v = 0;
        var count = JumpableCount(row, v);
        if (count == 0) return null;
        var list = RoomList(row, v);
        var want = ((index % count) + count) % count;
        for (var i = 0; i < list.Count; i++)
        {
            if (!list[i].jumpable) continue;
            if (want == 0) return list[i];
            want--;
        }
        return null;
    }

    int JumpableCount(int row, int v)
    {
        if (row < 1 || v < 0 || v >= VariantCount(row)) return 0;
        var list = RoomList(row, v);
        var count = 0;
        for (var i = 0; i < list.Count; i++) if (list[i].jumpable) count++;
        return count;
    }

    void ShowRoomVariant(int row, int v)
    {
        roomsRow = row;
        roomsVariant = v;
        var label = CardLabel(row, v);
        var r = RoomRowAt(row);
        if (row == 12 && v == 1)
        {
            StairRoute();
            return;
        }
        if (row == 10 && v == 1)
        {
            LightVariant();
            return;
        }
        if (row == 11 && v == 1)
        {
            ShowCard(row, v, Feet);
            Toast(label, "Never a jump inside: walk in from the arch; 1.5 m in, it trips (the Relay is held: H).");
            return;
        }
        var stop = JumpTarget(row, v, roomsIndex);
        if (stop == null)
        {
            ShowCard(row, v, Feet);
            Toast(label, WhereElse(row, v));
            return;
        }
        var count = JumpableCount(row, v);
        if (JumpToStop(stop, label + (count > 1 ? "  " + (((roomsIndex % count) + count) % count + 1) + " / " + count : ""), r != null ? r.tryLine : ""))
            ShowCard(row, v, stop.at);
    }

    string WhereElse(int row, int v)
    {
        switch (row)
        {
            case 1: return floor > 1 ? "Floor 1 only: the station behind the start door." : "Behind the start door: walk through it.";
            case 11: return RunLine();
            case 12: return v == 2 ? "From B1 on, the room each floor begins in (no jump into the start rooms)." : "No EXIT on this floor yet.";
            case 13:
                if (floor <= 1) return "On B1: K takes you to the stair key, then the EXIT; N on the title picks which station.";
                return map != null && map.HasStation && map.StationLayout.floorStation ? "This floor holds the other kind; N on floor 1's title picks." : "No station on this floor (A35): its EXIT is open.";
        }
        // The authored rooms and what stands in them (R6 B, C, R7): the placed one's way, or why there is none.
        int mr, mv;
        if (PlacedStop(row, v, out mr, out mv))
        {
            var placedLine = PlacedLine(row, v);
            if (placedLine != null) return placedLine;
        }
        if (IsModuleStop(row, v)) return "None the game made round you: walk on.";
        return "None round you yet: walk on, the floor builds as you go.";
    }

    /// <summary>
    /// Every jump of this tour waits for a calm frame, as the game's own seal does: no jump during a close-up, a shot, the
    /// glass or key shot, a climb, a door pull, the seal or the floor change, nor out of the start rooms. Null: clear.
    /// </summary>
    string GateRefusal()
    {
        if (game == null || map == null || player == null) return "The run has not started.";
        if (map.InStartArea(map.CellOf(player.position))) return "Walk through the start door first; jumps work on the floor itself.";
        if (FrontRooms3DGame.InspectingNow || FrontRoomsInspect.Active) return "Not during a close-up.";
        if (map.ExitSealing) return "Not while the EXIT seals.";
        var phase = PhaseField != null ? PhaseField.GetValue(game) : null;
        if (phase != null && phase.ToString() != "Playing") return "Not now: the game is in " + phase.ToString().ToUpperInvariant() + ".";
        var swap = FloorSwapField != null ? FloorSwapField.GetValue(game) : null;
        if (swap != null && swap.ToString() != "None") return "Not during the floor change.";
        if (RigField != null && RigField.GetValue(game) is FrontRoomsCameraRig rig && rig.InShot) return "Not during a shot.";
        if (GlassShotField != null && GlassShotField.GetValue(game) is FrontRoomsGlassShot glass && (glass.Active || glass.Blending)) return "Not during the glass shot.";
        if (DoorShotField != null && DoorShotField.GetValue(game) is FrontRoomsDoorShot door && (door.UnlockActive || door.KeyClock >= 0f)) return "Not during the key shot.";
        if (ClimbField != null && ClimbField.GetValue(game) is float climb && climb >= 0f) return "Not while climbing.";
        if (PullField != null && PullField.GetValue(game) != null) return "Not during the door pull.";
        return null;
    }

    bool JumpToStop(RoomStop stop, string title, string line)
    {
        var why = GateRefusal();
        if (why != null)
        {
            Toast(title, why);
            return false;
        }
        if (!StandAt(stop, out var feet))
        {
            Toast(title, "No clear floor there: walk (" + Mathf.RoundToInt(stop.distance) + " m).");
            return false;
        }
        MoveAndFace(feet, stop.look);
        Toast(title, line);
        scanClock = .1f;
        roomsClock = .1f;
        return true;
    }

    /// <summary>A standing spot: round the example first (a pile, a column), else its stand point and a ring round that.</summary>
    bool StandAt(RoomStop e, out Vector3 feet)
    {
        feet = default;
        var toPlayer = Flat(Feet - e.at);
        if (toPlayer.sqrMagnitude < 1e-4f) toPlayer = Vector3.forward;
        toPlayer.Normalize();
        if (e.ring > 0f)
            for (var r = 0; r < 3; r++)
            {
                var radius = e.ring * (r == 0 ? 1f : r == 1 ? 1.25f : .8f);
                for (var a = 0; a < 8; a++)
                    if (TryFloor(e.at + Quaternion.Euler(0f, a * 45f, 0f) * toPlayer * radius, out feet)) return true;
            }
        if (TryFloor(e.stand, out feet)) return true;
        for (var ring = 0; ring < 2; ring++)
        for (var a = 0; a < 8; a++)
            if (TryFloor(e.stand + Quaternion.Euler(0f, a * 45f, 0f) * toPlayer * (ring == 0 ? .9f : 1.6f), out feet)) return true;
        return false;
    }

    // The landed jump's move: the body off for the move, then the game's own yaw and pitch toward the point.
    void MoveAndFace(Vector3 feet, Vector3 look)
    {
        var body = player.GetComponent<CharacterController>();
        var was = body != null && body.enabled;
        if (body != null) body.enabled = false;
        player.position = feet;
        if (body != null) body.enabled = was;
        Physics.SyncTransforms();
        var dir = look - (feet + Vector3.up * ModuleUnits.PlayerEye);
        var flat = Flat(dir);
        if (flat.sqrMagnitude > 1e-4f && YawField != null && YawField.FieldType == typeof(float))
            YawField.SetValue(game, Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg);
        if (PitchField != null && PitchField.FieldType == typeof(float))
            PitchField.SetValue(game, Mathf.Clamp(-Mathf.Atan2(dir.y, Mathf.Max(.1f, flat.magnitude)) * Mathf.Rad2Deg, -45f, 45f));
    }

    void ShowCard(int row, int v, Vector3 at)
    {
        cardRow = row;
        cardVariant = v;
        cardAt = at;
        cardUntil = Time.unscaledTime + RoomCardSeconds;
    }

    /// <summary>
    /// K, R12 B: the B1 route the player walks. No stair key in hand on a station floor: the landed jump to the key plate's take
    /// spot; with it (or on a floor whose EXIT is open): to N, facing the EXIT. E TAKE KEY, E UNLOCK and the walk in stay the game's.
    /// </summary>
    void StairRoute()
    {
        roomsRow = 12;
        roomsVariant = 1;
        var label = RoomLabel(12, 1);
        var why = GateRefusal();
        if (why != null)
        {
            Toast(label, why);
            return;
        }
        var keyFirst = map.FloorKeyRule && !map.StationKeyIsTaken;
        if (!Jump(keyFirst ? Kind.StairKey : Kind.Exit, 0)) return;
        toastTitle = label + (keyFirst ? "  ·  1 THE STAIR KEY" : "  ·  2 THE EXIT");
        toastLine = keyFirst ? "E · TAKE KEY, then K again: the EXIT." : "E · UNLOCK, then walk 1.4 m in: the seal, the blackout, the next floor.";
        ShowCard(12, 1, Feet);
    }

    // ---------- R10 B: the lamp compare

    void ToggleLights()
    {
        if (litCells.Count > 0)
        {
            var at = litAt;
            RestoreLamps();
            Toast(RoomLabel(10, 0), "The same cells as the map built them. L lights them again.");
            ShowCard(10, 0, at);
            return;
        }
        if (!DarkCellNear(out var seed))
        {
            var patch = NearestPatch(10f);
            if (patch == null)
            {
                Toast(RoomLabel(10, 1), "L works at a dark patch: 0 on page 1 jumps to one.");
                return;
            }
            seed = patch.cell;
        }
        LightPatch(seed);
        Toast(RoomLabel(10, 1), litCells.Count + " lamps lit Steady (" + litDead + " dead, " + litDim + " dim as built). L again, or walk away: as built.");
        ShowCard(10, 1, litAt);
    }

    // The dead or dim cell under the player, or one beside it (the scan lists one patch a chunk, maybe not this one).
    bool DarkCellNear(out GridCoord seed)
    {
        seed = default;
        if (map == null || player == null) return false;
        var here = map.CellOf(Feet);
        for (var k = 0; k < 10; k++)
        {
            // The player's own cell first, then the eight round it (k 1-9 walk the 3 x 3, its middle skipped).
            var c = k == 0 ? here : new GridCoord(here.x + (k - 1) % 3 - 1, here.y + (k - 1) / 3 - 1);
            if (k > 0 && c == here) continue;
            if (!map.IsBuilt(c) || InStationArea(c)) continue;
            var m = map.LampModeOf(c);
            if (m != ModuleLamp.Dead && m != ModuleLamp.Dim) continue;
            seed = c;
            return true;
        }
        return false;
    }

    // J or 0 then J on R10: stand in the nearest patch (a jump when it is not close), then light it.
    void LightVariant()
    {
        if (litCells.Count > 0)
        {
            ShowCard(10, 1, litAt);
            Toast(RoomLabel(10, 1), "Lit. L puts it back as built.");
            return;
        }
        if (!DarkCellNear(out var seed))
        {
            var patch = NearestPatch(6f);
            if (patch == null)
            {
                var target = JumpTarget(10, 0, roomsIndex);
                if (target == null)
                {
                    ShowCard(10, 1, Feet);
                    Toast(RoomLabel(10, 1), WhereElse(10, 1));
                    return;
                }
                if (!JumpToStop(target, RoomLabel(10, 1), "")) return;
                patch = target;
            }
            seed = patch.cell;
        }
        LightPatch(seed);
        Toast(RoomLabel(10, 1), litCells.Count + " lamps lit Steady (" + litDead + " dead, " + litDim + " dim as built). L: as built again.");
        ShowCard(10, 1, litAt);
    }

    RoomStop NearestPatch(float within)
    {
        var list = RoomList(10, 0);
        var feet = Feet;
        RoomStop best = null;
        var bestD = within;
        for (var i = 0; i < list.Count; i++)
        {
            var d = Vector3.Distance(feet, list[i].at);
            if (d > bestD) continue;
            bestD = d;
            best = list[i];
        }
        return best;
    }

    /// <summary>The dead and dim cells joined to the seed (four ways, built, outside the start area, X and the station), at most
    /// LitMaxCells, each set Steady through the map's own lamp modes; RestoreLamps sets them back to Auto.</summary>
    void LightPatch(GridCoord seed)
    {
        RestoreLamps();
        litQueue.Clear();
        litSeen.Clear();
        litDead = litDim = 0;
        litQueue.Add(seed);
        litSeen.Add(seed);
        for (var q = 0; q < litQueue.Count && litCells.Count < LitMaxCells; q++)
        {
            var c = litQueue[q];
            if (!map.IsBuilt(c) || InStationArea(c)) continue;
            var mode = map.LampModeOf(c);
            if (mode != ModuleLamp.Dead && mode != ModuleLamp.Dim) continue;
            if (mode == ModuleLamp.Dead) litDead++;
            else litDim++;
            litCells.Add(c);
            map.SetLampMode(c, ModuleLamp.Steady);
            for (var k = 0; k < 4; k++)
            {
                var n = new GridCoord(c.x + (k == 0 ? 1 : k == 1 ? -1 : 0), c.y + (k == 2 ? 1 : k == 3 ? -1 : 0));
                if (litSeen.Add(n)) litQueue.Add(n);
            }
        }
        litAt = map.CellCenter(seed);
    }

    bool InStationArea(GridCoord cell) => map.HasStation && map.StationLayout.Protected(cell);

    // ---------- the finders (each RoomScanSeconds; built chunks only; nothing here generates, builds or meets a zone)

    void ClearRoomFound()
    {
        for (var i = 0; i < roomFound.Length; i++) roomFound[i].Clear();
        roomPoolUsed = 0;
        for (var k = 0; k < chunkBest.Length; k++) chunkBest[k] = null;
    }

    RoomStop NewStop(int row, int v, Vector3 at, Vector3 stand, Vector3 look, Vector3 feet)
    {
        if (roomPoolUsed == roomPool.Count) roomPool.Add(new RoomStop());
        var e = roomPool[roomPoolUsed++];
        e.row = row;
        e.variant = v;
        e.at = at;
        e.stand = stand;
        e.look = look;
        e.distance = Vector3.Distance(feet, at);
        e.ring = 0f;
        e.jumpable = true;
        e.detail = null;
        e.cell = default;
        return e;
    }

    void AddStop(RoomStop e) => RoomList(e.row, e.variant).Add(e);

    // The chunk's best example of a kind: the higher score, then the nearer.
    void Offer(int row, int v, float score, Vector3 at, Vector3 stand, Vector3 look, Vector3 feet, GridCoord cell, float ring)
    {
        var k = row * RoomVariantMax + v;
        var d = Vector3.Distance(feet, at);
        var best = chunkBest[k];
        if (best != null && (score < chunkScore[k] || (Mathf.Approximately(score, chunkScore[k]) && d >= best.distance))) return;
        if (best == null) best = chunkBest[k] = NewStop(row, v, at, stand, look, feet);
        best.at = at;
        best.stand = stand;
        best.look = look;
        best.distance = d;
        best.cell = cell;
        best.ring = ring;
        chunkScore[k] = score;
    }

    void CommitChunk()
    {
        for (var k = 0; k < chunkBest.Length; k++)
        {
            if (chunkBest[k] != null) AddStop(chunkBest[k]);
            chunkBest[k] = null;
            chunkScore[k] = 0f;
        }
    }

    void ScanRooms()
    {
        ClearRoomFound();
        if (map == null || map.Cache == null || player == null) return;
        // The placed rooms whose chunks the cache generated since the last scan: stamped, or said once.
        CheckPlacements();
        var feet = Feet;
        var here = MapGrid.ChunkOf(map.CellOf(feet));
        var radius = ScanRadius;
        // The piles first: a plain room is one without a pile.
        ScanPiles(feet);
        for (var cy = here.y - radius; cy <= here.y + radius; cy++)
        for (var cx = here.x - radius; cx <= here.x + radius; cx++)
        {
            var coord = new GridCoord(cx, cy);
            if (!map.IsChunkBuiltAt(MapGrid.ChunkOrigin(coord)) || !map.Cache.TryGetGenerated(coord, out var data) || data == null) continue;
            ScanChunk(data, feet);
            CommitChunk();
        }
        ScanStations(feet);
        ScanRunRooms(feet);
        for (var i = 0; i < roomFound.Length; i++) roomFound[i].Sort(byStopDistance);
    }

    void ScanChunk(MapChunk data, Vector3 feet)
    {
        const int n = MapGrid.ChunkCells;
        var gen = map.Cache.Generator;
        var up = Vector3.up;
        for (var j = 0; j < n; j++)
        for (var i = 0; i < n; i++)
        {
            var cell = data.Cell(i, j);
            if (map.IsReserved(cell) || InStationArea(cell)) continue;
            var zone = gen.ZoneOf(cell);
            var centre = map.CellCenter(cell);
            // R2 A, R3 A: a corridor's longest straight walk inside the chunk; R4 A: the tall hall, rooms included.
            if ((zone.height == ZoneHeight.Tall || !InAnyRoom(data, i, j)) && zone.theme == ZoneTheme.Level0)
            {
                var east = Run(data, i, j, true);
                var north = Run(data, i, j, false);
                var run = Mathf.Max(east, north);
                if (run >= 2)
                {
                    var to = east >= north ? new GridCoord(cell.x + run, cell.y) : new GridCoord(cell.x, cell.y + run);
                    var row = zone.height == ZoneHeight.Low ? 3 : zone.height == ZoneHeight.Tall ? 4 : 2;
                    Offer(row, 0, run, centre, centre, map.CellCenter(to) + up * 1.5f, feet, cell, 0f);
                }
            }
            // R10 A: a dead or dim lamp with a dark neighbour.
            var mode = map.LampModeOf(cell);
            if (mode == ModuleLamp.Dead || mode == ModuleLamp.Dim)
            {
                var dark = 0;
                var toward = cell;
                for (var k = 0; k < 4; k++)
                {
                    var nb = new GridCoord(cell.x + (k == 0 ? 1 : k == 1 ? -1 : 0), cell.y + (k == 2 ? 1 : k == 3 ? -1 : 0));
                    if (!map.IsBuilt(nb) || InStationArea(nb)) continue;
                    var m = map.LampModeOf(nb);
                    if (m != ModuleLamp.Dead && m != ModuleLamp.Dim) continue;
                    dark++;
                    toward = nb;
                }
                if (dark > 0) Offer(10, 0, dark, centre, centre, map.CellCenter(toward) + up * 2.2f, feet, cell, 0f);
            }
            // R8, R9: the borders east and north of the cell.
            Border(cell, new GridCoord(cell.x + 1, cell.y), zone, feet);
            Border(cell, new GridCoord(cell.x, cell.y + 1), zone, feet);
        }
        ScanChunkRooms(data, feet);
        ScanChunkColumns(data, feet);
    }

    // The straight walk from a local cell along +x or +y inside its chunk: open or arched edges only, at most 5.
    static int Run(MapChunk data, int i, int j, bool east)
    {
        var steps = 0;
        while (steps < 5)
        {
            var ci = east ? i + steps : i;
            var cj = east ? j : j + steps;
            if ((east && ci >= MapGrid.ChunkCells - 1) || (!east && cj >= MapGrid.ChunkCells - 1)) break;
            var edge = east ? data.east[MapGrid.LocalIndex(ci, cj)] : data.north[MapGrid.LocalIndex(ci, cj)];
            if (edge != EdgeKind.Open && edge != EdgeKind.Arch) break;
            steps++;
        }
        return steps;
    }

    static bool InAnyRoom(MapChunk data, int i, int j)
    {
        if (data.rooms == null) return false;
        for (var r = 0; r < data.rooms.Length; r++) if (data.rooms[r].Contains(i, j)) return true;
        return false;
    }

    // A border between two built cells: a door where Low meets Standard, a window into a Tall zone, a Tall hall's one arch
    // (R8); a Level 0 | Office crossing, renovated or not (R9). Both chunks built, so no edge read generates anything.
    void Border(GridCoord a, GridCoord b, ZoneInfo za, Vector3 feet)
    {
        if (!map.IsBuilt(b) || InStationArea(b)) return;
        var zb = map.Cache.Generator.ZoneOf(b);
        if (za.height == zb.height && za.theme == zb.theme) return;
        var mid = (map.CellCenter(a) + map.CellCenter(b)) * .5f;
        // Stand 1.5 m off the edge on the player's side, facing across it.
        var near = Vector3.Distance(feet, map.CellCenter(a)) <= Vector3.Distance(feet, map.CellCenter(b)) ? a : b;
        var stand = map.CellCenter(near);
        var look = mid + Vector3.up * 1.4f;
        if (za.height != zb.height)
        {
            var door = map.DoorBetween(a, b);
            var lowStandard = (za.height == ZoneHeight.Low && zb.height == ZoneHeight.Standard) || (za.height == ZoneHeight.Standard && zb.height == ZoneHeight.Low);
            if (door != null && lowStandard && !map.IsExitDoor(door) && !map.IsRunDoor(door)) Offer(8, 0, 1f, mid, stand, look, feet, near, 0f);
            else if (map.Cache.Generator.IsTallExit(a, b)) Offer(8, 2, 1f, mid, stand, look, feet, near, 0f);
            else if ((za.height == ZoneHeight.Tall || zb.height == ZoneHeight.Tall) && map.EffectiveEdge(a, b) == EdgeKind.Window) Offer(8, 1, 1f, mid, stand, look, feet, near, 0f);
        }
        if (za.theme != zb.theme && map.EffectiveEdge(a, b) != EdgeKind.Wall)
            Offer(9, map.BorderRenovated(a, b) ? 1 : 0, 1f, mid, stand, look, feet, near, 0f);
    }

    // R2 B-C, R3 B-C, R4 B, R5 A-C: whole rooms (no later room cutting them), outside the start area, X and the station.
    void ScanChunkRooms(MapChunk data, Vector3 feet)
    {
        if (data.rooms == null) return;
        var gen = map.Cache.Generator;
        for (var r = 0; r < data.rooms.Length; r++)
        {
            if (!data.RoomIntact(r)) continue;
            var rect = data.rooms[r];
            if (rect.w <= 0 || rect.h <= 0 || RoomTouchesReserved(data, rect)) continue;
            var module = data.ModuleOf(r);
            var zone = gen.ZoneOf(data.Cell(rect.x, rect.y));
            int row, v;
            if (module != null)
            {
                if (module.height == ZoneHeight.Low) { row = 3; v = 2; }
                else if (module.height == ZoneHeight.Tall) { row = 4; v = 1; }
                else if (module.theme == ZoneTheme.Office) { row = 5; v = 2; }
                else { row = 2; v = 2; }
            }
            else
            {
                // The tall hall is R4 A's (the zone finder); a room of two kinds of zone is not one space.
                if (zone.height == ZoneHeight.Tall || !gen.Uniform(data, rect)) continue;
                if (zone.theme == ZoneTheme.Office)
                {
                    // The office kit's rule: wall rows when the clear floor's short side is under 7 m (two cells), else pods.
                    row = 5;
                    v = Mathf.Min(rect.w, rect.h) >= 3 ? 1 : 0;
                }
                else
                {
                    if (RoomHasColumn(data, rect) || RoomHasPile(data, rect)) continue;
                    row = zone.height == ZoneHeight.Low ? 3 : 2;
                    v = 1;
                }
            }
            var first = map.CellCenter(data.Cell(rect.x, rect.y));
            var centre = (first + map.CellCenter(data.Cell(rect.x + rect.w - 1, rect.y + rect.h - 1))) * .5f;
            // Stand in the room's cell nearest the player, facing its centre.
            var stand = first;
            var best = float.MaxValue;
            for (var y = rect.y; y < rect.y + rect.h; y++)
            for (var x = rect.x; x < rect.x + rect.w; x++)
            {
                var c = map.CellCenter(data.Cell(x, y));
                var d = (c - feet).sqrMagnitude;
                if (d >= best) continue;
                best = d;
                stand = c;
            }
            var e = NewStop(row, v, centre, stand, centre + Vector3.up * 1.2f, feet);
            var placedHere = module != null ? PlacedRoomFor(data.coord, rect) : null;
            e.detail = rect.w + " x " + rect.h + " cells"
                + (module == null ? "" : placedHere != null ? ", authored, placed by the tour" + SpotNote(placedHere) : ", authored, the game's own");
            AddStop(e);
        }
    }

    bool RoomTouchesReserved(MapChunk data, CellRect rect)
    {
        for (var y = rect.y; y < rect.y + rect.h; y++)
        for (var x = rect.x; x < rect.x + rect.w; x++)
        {
            var cell = data.Cell(x, y);
            if (map.IsReserved(cell) || InStationArea(cell)) return true;
        }
        return false;
    }

    static bool RoomHasColumn(MapChunk data, CellRect rect)
    {
        if (data.pillar == null) return false;
        for (var j = rect.y + 1; j < rect.y + rect.h; j++)
        for (var i = rect.x + 1; i < rect.x + rect.w; i++)
        {
            var k = i + j * (MapGrid.ChunkCells + 1);
            if (k >= 0 && k < data.pillar.Length && data.pillar[k]) return true;
        }
        return false;
    }

    bool RoomHasPile(MapChunk data, CellRect rect)
    {
        var o = data.Origin;
        for (var p = 0; p < pileAt.Count; p++)
        {
            var c = map.CellOf(pileAt[p]);
            if (rect.Contains(c.x - o.x, c.y - o.y)) return true;
        }
        return false;
    }

    // R6: a column on the 6 m grid, by its zone: Level 0 0.6 m, Office 0.9 m with bulkheads, Tall 0.9 m.
    void ScanChunkColumns(MapChunk data, Vector3 feet)
    {
        if (data.pillar == null || data.pillarStyle == null) return;
        var gen = map.Cache.Generator;
        var corner = map.transform.rotation * new Vector3(-MapGrid.CellSize * .5f, 0f, -MapGrid.CellSize * .5f);
        for (var j = 1; j < MapGrid.ChunkCells; j++)
        for (var i = 1; i < MapGrid.ChunkCells; i++)
        {
            var k = i + j * (MapGrid.ChunkCells + 1);
            if (k >= data.pillar.Length || !data.pillar[k]) continue;
            var cell = data.Cell(i, j);
            // The map builds no column touching the start area or X.
            if (map.IsReserved(cell) || map.IsReserved(data.Cell(i - 1, j)) || map.IsReserved(data.Cell(i, j - 1)) || map.IsReserved(data.Cell(i - 1, j - 1))) continue;
            var zone = gen.ZoneOf(cell);
            var v = zone.height == ZoneHeight.Tall ? 2 : zone.theme == ZoneTheme.Office ? 1 : 0;
            var at = map.CellCenter(cell) + corner;
            // Stand 2.4 m off it toward the room's middle (else toward the player), facing it.
            var away = Flat(RoomCentreAround(data, i, j) - at);
            if (away.sqrMagnitude < .25f) away = Flat(feet - at);
            if (away.sqrMagnitude < 1e-4f) away = Vector3.forward;
            var stand = at + away.normalized * 2.4f;
            Offer(6, v, 1f, at, stand, at + Vector3.up * 1.4f, feet, cell, 2.4f);
        }
    }

    // The centre of the room holding a column corner, or the corner itself.
    Vector3 RoomCentreAround(MapChunk data, int i, int j)
    {
        if (data.rooms != null)
            for (var r = 0; r < data.rooms.Length; r++)
            {
                var rect = data.rooms[r];
                if (i <= rect.x || i >= rect.x + rect.w || j <= rect.y || j >= rect.y + rect.h) continue;
                return (map.CellCenter(data.Cell(rect.x, rect.y)) + map.CellCenter(data.Cell(rect.x + rect.w - 1, rect.y + rect.h - 1))) * .5f;
            }
        return map.CellCenter(data.Cell(i, j));
    }

    // R7: the pile roots the furniture pile names "furniture pile (<tableau>)" under each built chunk's root ("Chunk (x, y) · ...").
    void ScanPiles(Vector3 feet)
    {
        pileAt.Clear();
        var root = map.transform;
        for (var c = 0; c < root.childCount; c++)
        {
            var chunk = root.GetChild(c);
            if (chunk == null || !chunk.name.StartsWith("Chunk (", StringComparison.Ordinal)) continue;
            for (var p = 0; p < chunk.childCount; p++)
            {
                var pile = chunk.GetChild(p);
                if (pile == null || !pile.gameObject.activeInHierarchy) continue;
                var pileName = pile.name;
                if (!pileName.StartsWith("furniture pile (", StringComparison.Ordinal)) continue;
                var at = pile.position;
                pileAt.Add(at);
                var e = NewStop(7, TableauOf(pileName), at, at, at + Vector3.up, feet);
                e.ring = 4f;
                var host = PlacedRoomAt(map.CellOf(at));
                e.detail = pileName + (host != null ? ", in the placed " + host.name : "");
                AddStop(e);
            }
        }
    }

    // FrontRoomsFurniturePile.Tableau, in its order: CentreSculpture, CopyPasteRow, CeilingStuck, OfficeCluster, ZeroPile.
    static int TableauOf(string name)
    {
        if (name.IndexOf("CopyPasteRow", StringComparison.Ordinal) >= 0) return 1;
        if (name.IndexOf("CeilingStuck", StringComparison.Ordinal) >= 0) return 2;
        if (name.IndexOf("OfficeCluster", StringComparison.Ordinal) >= 0) return 3;
        if (name.IndexOf("ZeroPile", StringComparison.Ordinal) >= 0) return 4;
        return 0;
    }

    // R1 (floor 1's station and its corridor), R13 (a lower floor's station), R12 A (the EXIT, from N).
    void ScanStations(Vector3 feet)
    {
        var up = Vector3.up;
        if (map.HasStation)
        {
            var l = map.StationLayout;
            var room = (map.CellCenter(new GridCoord(l.station.x, l.station.y))
                + map.CellCenter(new GridCoord(l.station.x + l.station.w - 1, l.station.y + l.station.h - 1))) * .5f;
            if (!l.floorStation)
            {
                // The player's first view: the cell behind the start door, facing E1.
                var door = map.CellCenter(l.door);
                AddStop(NewStop(1, 0, room, door, map.CellCenter(l.e1In) + up * 1.4f, feet));
                if (l.corridor > 0)
                {
                    var c1 = map.CellCenter(l.Corridor(1));
                    AddStop(NewStop(1, 1, c1, c1, map.CellCenter(l.front) + up * 1.5f, feet));
                }
            }
            else
            {
                var module = map.StationModule;
                var v = module != null && module.theme == ZoneTheme.Office ? 1 : 0;
                var stand = Vector3.Lerp(map.CellCenter(l.e1Out), map.CellCenter(l.e1In), .75f);
                AddStop(NewStop(13, v, room, stand, room + up * 1.2f, feet));
            }
        }
        var exit = map.ExitDoor;
        if (exit != null)
        {
            var mid = (map.CellCenter(exit.a) + map.CellCenter(exit.b)) * .5f;
            AddStop(NewStop(12, 0, mid, map.CellCenter(map.ExitFrontCell), mid + up * 1.4f, feet));
        }
    }

    // R11: every Run! room the planner holds; a built one is stood at from a cell outside its arch, never inside.
    void ScanRunRooms(Vector3 feet)
    {
        var run = map.RunRooms;
        if (run == null || run.Rooms == null) return;
        for (var i = 0; i < run.Rooms.Count; i++)
        {
            var room = run.Rooms[i];
            if (room == null) continue;
            map.RunEntrance(room, false, out var arch, out _);
            var back = new GridCoord(room.Outside.x - room.AxisX, room.Outside.y - room.AxisY);
            var e = NewStop(11, 0, arch, map.CellCenter(back), arch + Vector3.up * 1.6f, feet);
            e.jumpable = map.IsRunRoomBuilt(room);
            e.detail = e.jumpable ? "built" : "planned, not built yet";
            AddStop(e);
        }
    }

    // ---------- drawing

    /// <summary>Which way and how far, as the player faces now.</summary>
    string Bearing(Vector3 at)
    {
        var to = Flat(at - Feet);
        var forward = view != null ? Flat(view.transform.forward) : Vector3.forward;
        if (to.sqrMagnitude < 1f || forward.sqrMagnitude < 1e-4f) return "here";
        var a = Vector3.SignedAngle(forward, to, Vector3.up);
        var side = a < 0f ? "left" : "right";
        var abs = Mathf.Abs(a);
        return abs < 30f ? "ahead" : abs < 75f ? "ahead " + side : abs < 120f ? "to the " + side : abs < 160f ? "behind, " + side : "behind";
    }

    string RunLine()
    {
        if (map == null) return "";
        if (map.RunRooms == null) return "Run! rooms are off on this floor.";
        var list = RoomList(11, 0);
        if (list.Count == 0) return "None planned yet: walk on; they come 2-5 chunks out (70-120 m).";
        RoomStop built = null;
        for (var i = 0; i < list.Count && built == null; i++) if (list[i].jumpable) built = list[i];
        var near = list[0];
        if (built != null)
            return "Built: " + Mathf.RoundToInt(built.distance) + " m, " + Bearing(built.at) + "  ·  page 2, key 1 jumps outside its arch";
        return "Planned, not built yet: " + Mathf.RoundToInt(near.distance) + " m, " + Bearing(near.at) + "  ·  walk that way";
    }

    string ExitLine()
    {
        var list = RoomList(12, 0);
        if (list.Count == 0) return "No EXIT on this floor yet.";
        var e = list[0];
        var state = map.FloorKeyRule ? (map.StationKeyIsTaken ? "the key in hand" : "locked: the stair key first (K)") : "open";
        return "EXIT " + Mathf.RoundToInt(e.distance) + " m, " + Bearing(e.at) + "  ·  " + state;
    }

    /// <summary>The card's live line: what the scan holds for this stop now.</summary>
    string LiveLine(int row, int v)
    {
        if (map == null) return row == 13 ? "This run's B1 station: " + (roomsDesk ? "DESK" : "WAITING") + " (N)." : "";
        switch (row)
        {
            case 0: return "The start rooms: no jump here; walk through the start door.";
            case 10:
                if (litCells.Count > 0) return "LIT: " + litCells.Count + " lamps (" + litDead + " dead, " + litDim + " dim as built)  ·  L: as built";
                break;
            case 11: return RunLine();
            case 12:
                if (v == 2) return floor > 1 ? "This floor began in that stairwell: the room behind its north door." : "From B1 on: the room each floor begins in.";
                return ExitLine();
            case 13:
                if (floor <= 1) return "On B1. This run's choice: " + (roomsDesk ? "DESK" : "WAITING") + " (N on the title).";
                break;
        }
        var list = row == 10 && v == 1 ? RoomList(10, 0) : RoomList(row, v);
        if (list.Count == 0) return WhereElse(row, v);
        var near = list[0];
        return list.Count + " round you  ·  nearest " + Mathf.RoundToInt(near.distance) + " m, " + Bearing(near.at) + (near.detail != null ? "  ·  " + near.detail : "");
    }

    string RowStatus(int row)
    {
        if (map == null) return "";
        switch (row)
        {
            case 1:
                if (floor > 1) return "FLOOR 1 ONLY";
                break;
            case 11:
            {
                var list = RoomList(11, 0);
                if (list.Count == 0) return "NONE PLANNED YET";
                for (var i = 0; i < list.Count; i++) if (list[i].jumpable) return "BUILT " + Mathf.RoundToInt(list[i].distance) + " M";
                return "PLANNED " + Mathf.RoundToInt(list[0].distance) + " M";
            }
            case 12:
            {
                var list = RoomList(12, 0);
                return (list.Count > 0 ? "EXIT " + Mathf.RoundToInt(list[0].distance) + " M" : "NO EXIT YET") + "  ·  K";
            }
            case 13:
                if (floor <= 1) return "ON B1 (" + (roomsDesk ? "DESK" : "WAITING") + ")";
                if (!map.HasStation || !map.StationLayout.floorStation) return "NO STATION HERE";
                break;
            case 7:
            {
                // No pile seen yet, but the placed pillar hall holds one (its tableau shows once it is seen).
                var hall = PileSeen() ? null : PlacedHint(7, 0);
                if (hall != null) return "PILLAR HALL ~" + Mathf.RoundToInt(Vector3.Distance(Feet, hall.centre)) + " M";
                break;
            }
        }
        var s = "";
        var n = VariantCount(row);
        for (var v = 0; v < n; v++)
        {
            var list = RoomList(row, v);
            if (v > 0) s += "  ";
            if (row == 10 && v == 1) s += "B " + (litCells.Count > 0 ? "LIT" : "L");
            else if (list.Count > 0) s += (char)('A' + v) + " " + Mathf.RoundToInt(list[0].distance);
            else
            {
                // A placed room not on the scan yet: its distance, marked ~ (walk there; it is not built or not near).
                var placed = row != 7 ? PlacedHint(row, v) : null;
                s += (char)('A' + v) + " " + (placed != null ? "~" + Mathf.RoundToInt(Vector3.Distance(Feet, placed.centre)) : "-");
            }
        }
        return s;
    }

    bool RowFound(int row)
    {
        if (row == 12) return RoomList(12, 0).Count > 0;
        for (var v = 0; v < VariantCount(row); v++) if (RoomList(row, v).Count > 0 || PlacedHint(row, v) != null) return true;
        return false;
    }

    /// <summary>The list in this tour: R1-R10 on page 1 (keys 1-0), R11-R13 on page 2 (keys 1-3), the marked row's lines under it.</summary>
    void DrawRoomsList(float x, float y, float w, float s)
    {
        Text(new Rect(x, y, w, 40f * s), TourName(tour) + (floor > 1 ? "  ·  LEVEL B" + (floor - 1) : ""), titleStyle, Paper);
        y += 38f * s;
        var meta = "RELAY " + (holdRelay ? "HELD" : "LIVE") + " (H)  ·  PAGE " + (roomsPage + 1) + "/2 [ ]  ·  J NEXT  ·  K B1 ROUTE  ·  L LAMPS  ·  G HIDE  ·  F2 TOURS";
        var mh = smallStyle.CalcHeight(new GUIContent(meta), w);
        Text(new Rect(x, y, w, mh), meta, smallStyle, Paper, .8f);
        y += mh + 6f * s;
        if (problem != null)
        {
            var ph = smallStyle.CalcHeight(new GUIContent(problem), w);
            Text(new Rect(x, y, w, ph), problem, smallStyle, Accent);
            y += ph + 8f * s;
        }
        var first = roomsPage == 0 ? 1 : 11;
        var last = roomsPage == 0 ? 10 : 13;
        for (var row = first; row <= last; row++)
        {
            var r = RoomRowAt(row);
            if (r == null) continue;
            var key = row - first + 1;
            var any = RowFound(row);
            var alpha = any ? 1f : .5f;
            var marked = roomsRow == row;
            if (marked) Fill(new Rect(x - 14f * s, y + 6f * s, 4f * s, 14f * s), Accent);
            // The key and the R code in Plex Mono Medium, the name in Bayon (RowLabel, .Plan.cs).
            RowKey(x, y, key == 10 ? "0" : key.ToString(), any ? Accent : Paper, alpha);
            RowLabel(x + 34f * s, y, r.code, r.name, 52f * s, Paper, alpha);
            Text(new Rect(x + w - 262f * s, y + 5f * s, 262f * s, 22f * s), RowStatus(row), statusStyle, Paper, any ? .9f : .55f);
            y += 28f * s;
            if (!marked) continue;
            y = Para(x + 34f * s, y - 2f * s, w - 34f * s, CardLabel(row, roomsVariant) + "  ·  " + r.tryLine, smallStyle, Paper, .85f, 2f * s);
            y = RowDecide(x + 34f * s, y, w - 34f * s, DecideOf(r, roomsVariant), .9f, 6f * s);
        }
    }

    /// <summary>The station card on the left, below the game's room panel and the toast: the start rooms' own (R0, or R12 C
    /// below floor 1) while the player stands there; else the stop last shown, for 8 s and while the player stays near it.</summary>
    void DrawRoomCard(float margin, float s)
    {
        int row = -1, v = 0;
        if (map != null && player != null && map.InStartArea(map.CellOf(player.position)))
        {
            row = floor > 1 ? 12 : 0;
            v = floor > 1 ? 2 : 0;
        }
        else if (cardRow >= 0 && (Time.unscaledTime < cardUntil || Vector3.Distance(Feet, cardAt) < RoomCardReach))
        {
            row = cardRow;
            v = cardVariant;
        }
        if (row < 0) return;
        RoomCardBlock(row, v, margin, Mathf.Max(240f * s, Screen.height * .36f), Mathf.Min(600f * s, Screen.width * .42f), s);
    }

    /// <summary>The title, before SPACE: the B1 choice (N) and R0's card, under the tour's title lines.</summary>
    void DrawRoomsTitle(float x, float y, float w, float s)
    {
        Text(new Rect(x, y, w, 24f * s), "N · B1 STATION: " + (roomsDesk ? "DESK (Office zones)" : "WAITING (Level 0 zones)"), lineStyle, Accent);
        RoomCardBlock(0, 0, x, y + 40f * s, w, s);
    }

    // Placard C on screen in the placard type (DrawCard, .Plan.cs): the code chip ("R2 B", "R7" while no pile is seen) and the
    // name, KEYS, TOUCH, what it is, 要你决定, the small line in grey and the live line in accent at the foot.
    void RoomCardBlock(int row, int v, float x, float y, float w, float s)
    {
        var r = RoomRowAt(row);
        if (r == null) return;
        string code, name;
        SplitLabel(CardLabel(row, v), out code, out name);
        DrawCard(x, y + 8f * s, w, 1f, true, code, name, TryLabel, r.tryLine, TouchLabel, r.touch, r.what, DecideOf(r, v), r.small,
            LiveLine(row, v), null, null);
    }
}
