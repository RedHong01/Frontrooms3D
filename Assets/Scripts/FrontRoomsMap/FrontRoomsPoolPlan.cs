using System;
using System.Collections.Generic;
using System.Text;

namespace FrontRooms.Map
{
    // Pool family (structures scene, scene 4, RULES 5b; s67_pool DESIGN section 7, structures PLAN 2.7): the route from the
    // club on B2 to the dark zone as one plan over eight whole chunks, held by the cache (FrontRoomsMapCache.SetPool) and
    // applied last in Cache.Get. Plain C#, like FrontRoomsMap.cs. Nothing here runs unless something asks for a plan: today
    // that is the structures scene's pool route alone (the game's B2 hook is a seam, DESIGN 12).

    /// <summary>
    /// The pool family's room modules by id: the fifteen of FrontRoomsPoolModules (built in code in the structures scene),
    /// cloned and normalised. Complete when every id of FrontRoomsPoolModules.Ids is present. Get returns a fresh clone.
    /// </summary>
    public sealed class FrontRoomsPoolLibrary
    {
        readonly Dictionary<string, RoomModuleData> byId = new Dictionary<string, RoomModuleData>();

        public FrontRoomsPoolLibrary(IDictionary<string, RoomModuleData> modules)
        {
            if (modules == null) return;
            foreach (var pair in modules)
            {
                if (string.IsNullOrEmpty(pair.Key) || pair.Value == null) continue;
                var m = pair.Value.Clone();
                m.Normalize();
                if (m.pool != null) m.pool.Normalize(m.width, m.depth);
                byId[pair.Key] = m;
            }
        }

        /// <summary>The fifteen modules of the code table (FrontRoomsPoolModules.All).</summary>
        public static FrontRoomsPoolLibrary FromCode() => new FrontRoomsPoolLibrary(FrontRoomsPoolModules.All());

        public int Count => byId.Count;

        public bool Complete
        {
            get
            {
                foreach (var id in FrontRoomsPoolModules.Ids) if (!byId.ContainsKey(id)) return false;
                return true;
            }
        }

        /// <summary>The ids of FrontRoomsPoolModules.Ids this library lacks, comma separated ("" when complete).</summary>
        public string Missing
        {
            get
            {
                var sb = new StringBuilder();
                foreach (var id in FrontRoomsPoolModules.Ids)
                {
                    if (byId.ContainsKey(id)) continue;
                    if (sb.Length > 0) sb.Append(", ");
                    sb.Append(id);
                }
                return sb.ToString();
            }
        }

        public bool Has(string id) => id != null && byId.ContainsKey(id);

        /// <summary>A fresh clone of the module, or null.</summary>
        public RoomModuleData Get(string id) => id != null && byId.TryGetValue(id, out var m) ? m.Clone() : null;
    }

    /// <summary>
    /// Planner options (structures scene only, PLAN 2.7 e). Null fields leave the game's hash picks. Variant A forces
    /// PL_Leisure into M0 and PL_Court into slot m, variant B PL_PoMo and PL_Weird, so all fifteen modules are walked
    /// between the two.
    /// </summary>
    public sealed class FrontRoomsPoolPlanOptions
    {
        /// <summary>M0's module ("PL_Leisure" or "PL_PoMo"), or null for the hash pick.</summary>
        public string m0;
        /// <summary>Slot m's module ("PL_Court", "PL_Weird" or "PL_Chain"), or null for the hash pick.</summary>
        public string slotM;
        /// <summary>L0's module ("PL_OfficeCeiling" or "PL_PillarHall"; L1 takes the other), or null for the hash pick.</summary>
        public string l0;

        /// <summary>Variant 0 (A): PL_Leisure and PL_Court. Any other (B): PL_PoMo and PL_Weird.</summary>
        public static FrontRoomsPoolPlanOptions Variant(int variant) =>
            variant == 0 ? new FrontRoomsPoolPlanOptions { m0 = "PL_Leisure", slotM = "PL_Court" } : new FrontRoomsPoolPlanOptions { m0 = "PL_PoMo", slotM = "PL_Weird" };

        public override string ToString() =>
            "M0 " + (m0 ?? "hash") + ", slot m " + (slotM ?? "hash") + ", L0 " + (l0 ?? "hash");
    }

    /// <summary>One named viewpoint of the route: a floor point and a point to look at, in map metres (DESIGN 7 "Focus points").</summary>
    [Serializable]
    public struct PoolFocus
    {
        public string name;
        public GridCoord cell;
        /// <summary>The floor point (on the walking floor).</summary>
        public float x, y, z;
        /// <summary>A point the eye looks at.</summary>
        public float lookX, lookY, lookZ;
    }

    /// <summary>One room of a pool plan: the approach (no module) or a placed pool module, with its synthetic zone.</summary>
    public sealed class PoolRoom
    {
        public int index;
        /// <summary>The module's id ("PC_Hall", "PL_Chain", "PL_Dark_OfficeCeiling" ...), or "Approach".</summary>
        public string id;
        /// <summary>For the status line: the id and, for a slot, its letter.</summary>
        public string label;
        /// <summary>"approach", "club", "level" or "dark".</summary>
        public string part;
        /// <summary>The map cell of its south-west cell.</summary>
        public GridCoord origin;
        public int width, depth;
        /// <summary>The placed module (the plan's own clone), or null for the approach.</summary>
        public RoomModuleData module;
        public ZoneInfo zone;

        public bool IsDark => part == "dark";
        public bool Contains(GridCoord c) => c.x >= origin.x && c.y >= origin.y && c.x < origin.x + width && c.y < origin.y + depth;
        public float CentreX => (origin.x + width * .5f) * MapGrid.CellSize;
        public float CentreZ => (origin.y + depth * .5f) * MapGrid.CellSize;
        /// <summary>The module's base plane (0, or -2.52 in the dark zone).</summary>
        public float BaseY => module != null && module.pool != null ? module.pool.baseY : 0f;
        /// <summary>Its ceiling in map metres.</summary>
        public float CeilingY => BaseY + MapGrid.CeilingHeight(zone.height);
    }

    /// <summary>
    /// The pool route (s67_pool DESIGN 7): approach, club (PC_Entry, PC_Lockers, PC_Hall), the pool level in four zones
    /// (Z0-Z3 with their slots), the fire stair and the dark zone, over eight whole chunks from an origin chunk. Built by
    /// FrontRoomsPoolPlanner; the cache holds a Built plan (SetPool), the world only a Ready one (accepted: walk band and
    /// Validate clean). Every chunk build of a plan chunk gives the same arrays, so revisit shifts change nothing.
    /// Plan records hold a round arch's crown in map metres (its author module's base added, clamped under both ceilings).
    /// </summary>
    public sealed class FrontRoomsPoolPlan
    {
        /// <summary>The plan's eight chunks, in chunk steps from its origin chunk; (1, 0) and (1, 1) stay the map's.</summary>
        public static readonly GridCoord[] ChunkSteps =
        {
            new GridCoord(0, 0), new GridCoord(0, 1), new GridCoord(0, 2), new GridCoord(1, 2),
            new GridCoord(0, 3), new GridCoord(1, 3), new GridCoord(0, 4), new GridCoord(1, 4),
        };

        /// <summary>The route's named viewpoints, in order.</summary>
        public static readonly string[] FocusNames =
        {
            "Entrance", "Entry", "Lockers", "Hall from the SW deck", "Far door", "Turn", "L0", "M0", "Gallery", "L1",
            "Basin room", "EXIT", "Stair head", "Stair foot", "Dark chain", "Dark office", "Dark basin",
        };

        // A slot of DESIGN 7's block table: block cell (x, y), its size, and the zone (0-3) it serves.
        struct Slot
        {
            public string name;
            public int x, y, w, h, zone;
            public Slot(string name, int x, int y, int w, int h, int zone) { this.name = name; this.x = x; this.y = y; this.w = w; this.h = h; this.zone = zone; }
        }

        static readonly Slot[] Slots =
        {
            new Slot("a", 4, 16, 2, 2, 0), new Slot("b", 6, 16, 2, 2, 0), new Slot("c", 0, 16, 2, 2, 0), new Slot("d", 0, 18, 2, 3, 0), new Slot("e", 0, 21, 2, 3, 0),
            new Slot("f", 8, 16, 2, 3, 1), new Slot("g", 10, 16, 2, 3, 1), new Slot("l", 12, 16, 2, 3, 1), new Slot("h", 12, 19, 2, 3, 1), new Slot("k", 12, 22, 2, 2, 1),
            new Slot("m", 6, 24, 2, 4, 2), new Slot("n", 6, 28, 2, 2, 2), new Slot("o", 0, 30, 2, 2, 2), new Slot("p", 2, 30, 2, 2, 2), new Slot("q", 4, 30, 2, 2, 2),
            new Slot("r", 12, 24, 2, 2, 3), new Slot("t", 14, 24, 2, 2, 3),
        };

        const int VoidRoom = -1, Outside = -2;
        /// <summary>The Void zone's id offset from FrontRoomsMapGenerator.PoolZoneBase (rooms count up from 0).</summary>
        const int VoidZoneIndex = 4095;

        sealed class ChunkData
        {
            public readonly GridCoord[] zone = new GridCoord[MapGrid.CellsPerChunk];
            public readonly ZoneHeight[] height = new ZoneHeight[MapGrid.CellsPerChunk];
            public readonly EdgeKind[] east = new EdgeKind[MapGrid.CellsPerChunk];
            public readonly EdgeKind[] north = new EdgeKind[MapGrid.CellsPerChunk];
            public readonly EdgeKind[] west = new EdgeKind[MapGrid.ChunkCells];
            public readonly EdgeKind[] south = new EdgeKind[MapGrid.ChunkCells];
            public readonly ModuleLamp[] lamp = new ModuleLamp[MapGrid.CellsPerChunk];
            public readonly ModuleCell[] pool = new ModuleCell[MapGrid.CellsPerChunk];
            public CellRect[] rooms = new CellRect[0];
            public RoomModuleData[] roomModules = new RoomModuleData[0];
            public readonly Dictionary<long, ModuleEdgeRecord> poolEdges = new Dictionary<long, ModuleEdgeRecord>();
        }

        readonly List<PoolRoom> rooms = new List<PoolRoom>();
        readonly Dictionary<GridCoord, int> roomOfCell = new Dictionary<GridCoord, int>();
        readonly HashSet<GridCoord> planChunks = new HashSet<GridCoord>();
        readonly List<GridCoord> touched = new List<GridCoord>();
        readonly Dictionary<long, EdgeKind> edges = new Dictionary<long, EdgeKind>();
        readonly Dictionary<long, ModuleEdgeRecord> records = new Dictionary<long, ModuleEdgeRecord>();
        readonly Dictionary<long, EdgeKind> borders = new Dictionary<long, EdgeKind>();
        readonly Dictionary<GridCoord, ZoneInfo> zoneCells = new Dictionary<GridCoord, ZoneInfo>();
        readonly List<ZoneInfo> zoneList = new List<ZoneInfo>();
        readonly Dictionary<GridCoord, ChunkData> chunkData = new Dictionary<GridCoord, ChunkData>();
        readonly HashSet<long> fixedLinks = new HashSet<long>();
        readonly List<PoolFocus> focus = new List<PoolFocus>();
        readonly List<string> notes = new List<string>();
        readonly Dictionary<string, int> used = new Dictionary<string, int>();
        ZoneInfo voidZone;

        internal FrontRoomsPoolPlan(GridCoord originChunk, int seed, int floorSeed)
        {
            OriginChunk = originChunk;
            Seed = seed;
            FloorSeed = floorSeed;
        }

        // ---------- What the plan is ----------

        /// <summary>The block's south-west chunk.</summary>
        public GridCoord OriginChunk { get; }
        /// <summary>Block cell (0, 0) as a map cell.</summary>
        public GridCoord OriginCell => MapGrid.ChunkOrigin(OriginChunk);
        /// <summary>The map seed the plan was made on (its hash picks), and the floor seed the caller passed.</summary>
        public int Seed { get; }
        public int FloorSeed { get; }
        /// <summary>Every array is made: the cache may hold it (SetPool).</summary>
        public bool Built { get; private set; }
        /// <summary>The planner accepted it (walk band, Validate clean): the world may hold it.</summary>
        public bool Ready { get; private set; }
        /// <summary>Why no plan (not Ready), or null.</summary>
        public string error;
        /// <summary>The floor's EXIT (X and N of the plan; distance = walking cells from the start door to N, plus the door).</summary>
        public ExitPlan exit;
        /// <summary>Planning time in milliseconds (every candidate).</summary>
        public double planMs;
        /// <summary>Walking cells from the start door to the entrance's outside cell, and from the entrance to N inside the plan.</summary>
        public int walkToEntrance, walkInsideToFront;
        /// <summary>The options it was planned with ("hash" where none).</summary>
        public string optionsText = "";
        /// <summary>Walls the repair opened.</summary>
        public int Repairs { get; private set; }
        /// <summary>A stable hash of every decision (origin, picks).</summary>
        public string Hash { get; private set; } = "";

        /// <summary>The map cell south of the approach (outside the plan) and the approach's first cell.</summary>
        public GridCoord EntranceOutside => B(1, -1);
        public GridCoord EntranceIn => B(1, 0);
        /// <summary>N (the EXIT chain's north-east cell, dry at 0) and X (the stairwell's cell, Void in the plan).</summary>
        public GridCoord FrontCell => B(14, 27);
        public GridCoord ExitCell => B(14, 28);

        public IReadOnlyList<PoolRoom> Rooms => rooms;
        public IReadOnlyList<PoolFocus> Focus => focus;
        public IReadOnlyList<string> Notes => notes;

        // What the generator and the cache hold while the plan is set (FrontRoomsMapPool.cs).
        public Dictionary<GridCoord, ZoneInfo> ZoneCells => zoneCells;
        public IList<ZoneInfo> ZoneList => zoneList;
        public IReadOnlyDictionary<long, EdgeKind> Borders => borders;
        /// <summary>Every chunk a SetPool or ClearPool drops: the plan's chunks and their ring.</summary>
        public IReadOnlyList<GridCoord> Touched => touched;
        public IEnumerable<GridCoord> PlanChunks => planChunks;

        GridCoord B(int bx, int by) => new GridCoord(OriginCell.x + bx, OriginCell.y + by);

        // ---------- Queries ----------

        /// <summary>A cell of one of the plan's eight chunks.</summary>
        public bool IsPlanCell(GridCoord cell) => planChunks.Contains(MapGrid.ChunkOf(cell));
        public bool IsPlanChunk(GridCoord chunk) => planChunks.Contains(chunk);

        /// <summary>A plan cell no room holds (no floor, no ceiling, unseen).</summary>
        public bool IsVoid(GridCoord cell) => RoomIndex(cell) == VoidRoom;

        /// <summary>The room holding the cell, or null (Void or outside).</summary>
        public PoolRoom RoomAt(GridCoord cell)
        {
            var r = RoomIndex(cell);
            return r >= 0 ? rooms[r] : null;
        }

        /// <summary>The room by module id (the first), or null.</summary>
        public PoolRoom RoomById(string id)
        {
            foreach (var r in rooms) if (r.id == id) return r;
            return null;
        }

        int RoomIndex(GridCoord cell)
        {
            if (!IsPlanCell(cell)) return Outside;
            return roomOfCell.TryGetValue(cell, out var r) ? r : VoidRoom;
        }

        /// <summary>The plan's cell (water, floor, connector); a dry cell at 0 outside the plan and in the approach.</summary>
        public ModuleCell CellOf(GridCoord cell)
        {
            var r = RoomIndex(cell);
            if (r == Outside) return default;
            if (r == VoidRoom) return new ModuleCell { connector = CellConnector.Void };
            var room = rooms[r];
            if (room.module == null) return default;
            return PoolRules.SkinOf(room.module).CellAt(room.module, cell.x - room.origin.x, cell.y - room.origin.y);
        }

        /// <summary>The walking floor at a point in map metres (0 outside the plan).</summary>
        public float FloorAt(float x, float z)
        {
            var cs = MapGrid.CellSize;
            var cell = new GridCoord((int)Math.Floor(x / cs), (int)Math.Floor(z / cs));
            return CellOf(cell).FloorAt(x - cell.x * cs, z - cell.y * cs);
        }

        /// <summary>The plan's edge between two side-by-side cells (Wall for a pair the plan does not touch).</summary>
        public EdgeKind EdgeOf(GridCoord a, GridCoord b) => edges.TryGetValue(MapGrid.EdgeId(a, b), out var k) ? k : EdgeKind.Wall;

        /// <summary>The plan's record on the edge a-b (arch width, style, crown in map metres, trim), from either side.</summary>
        public bool TryRecord(GridCoord a, GridCoord b, out ModuleEdgeRecord record) => records.TryGetValue(MapGrid.EdgeId(a, b), out record);

        // ---------- Apply (FrontRoomsMapCache.Get, last) ----------

        /// <summary>
        /// A plan chunk takes the plan's zones, heights, edges (borders too), lamps, rooms, modules, pool cells and records;
        /// no key; columns placed again for its rooms. A ring chunk is left as generated (its border edges came through
        /// FrontRoomsMapGenerator.BorderEdge, which reads the plan's Borders first).
        /// </summary>
        public void Apply(FrontRoomsMapGenerator generator, MapChunk chunk)
        {
            if (generator == null || chunk == null || !Built) return;
            if (!chunkData.TryGetValue(chunk.coord, out var d)) return;
            Copy(d.zone, chunk.zone);
            Copy(d.height, chunk.height);
            Copy(d.east, chunk.east);
            Copy(d.north, chunk.north);
            Copy(d.west, chunk.west);
            Copy(d.south, chunk.south);
            Copy(d.lamp, chunk.lamp);
            chunk.rooms = (CellRect[])d.rooms.Clone();
            chunk.roomModules = (RoomModuleData[])d.roomModules.Clone();
            chunk.pool = (ModuleCell[])d.pool.Clone();
            chunk.poolEdges = new Dictionary<long, ModuleEdgeRecord>(d.poolEdges);
            chunk.hasKey = false;
            chunk.keySpot = false;
            chunk.keyHost = null;
            generator.PlaceColumns(chunk, chunk.revision);
        }

        static void Copy<T>(T[] from, T[] to)
        {
            if (from == null || to == null) return;
            Array.Copy(from, to, Math.Min(from.Length, to.Length));
        }

        // ---------- Build (FrontRoomsPoolPlanner only) ----------

        internal bool Build(FrontRoomsPoolLibrary library, FrontRoomsPoolPlanOptions options, out string failure)
        {
            failure = null;
            optionsText = options != null ? options.ToString() : "hash picks";
            foreach (var s in ChunkSteps) planChunks.Add(new GridCoord(OriginChunk.x + s.x, OriginChunk.y + s.y));

            // Rooms, in table order (DESIGN 7). The approach first: three normal Level 0 cells.
            var approach = new PoolRoom { id = "Approach", label = "approach", part = "approach", origin = B(1, 0), width = 1, depth = 3 };
            AddRoom(approach, ZoneHeight.Standard, ZoneTheme.Level0);
            if (!Place(library, "PC_Entry", "PC_Entry", "club", 1, 3, out failure)) return false;
            if (!Place(library, "PC_Lockers", "PC_Lockers", "club", 1, 5, out failure)) return false;
            var hall = PlaceRoom(library, "PC_Hall", "PC_Hall", "club", 1, 8, out failure);
            if (hall == null) return false;
            var turn = PlaceRoom(library, "PL_Turn", "PL_Turn", "level", 2, 16, out failure);
            if (turn == null) return false;

            // L0 and L1: PL_OfficeCeiling and PL_PillarHall, one each.
            var l0Id = options != null && (options.l0 == "PL_OfficeCeiling" || options.l0 == "PL_PillarHall") ? options.l0
                : Unit(SaltL0Pick, 0) < .5f ? "PL_OfficeCeiling" : "PL_PillarHall";
            var l1Id = l0Id == "PL_OfficeCeiling" ? "PL_PillarHall" : "PL_OfficeCeiling";
            if (!Place(library, l0Id, l0Id + " (L0)", "level", 2, 18, out failure)) return false;

            // M0: PL_Leisure (4 x 5 at y 19) or PL_PoMo (4 x 4 at y 20).
            var m0Id = options != null && (options.m0 == "PL_Leisure" || options.m0 == "PL_PoMo") ? options.m0
                : PickWeighted(library, new[] { "PL_Leisure", "PL_PoMo" }, 99, 99, Unit(SaltM0Pick, 0));
            if (m0Id == null) { failure = "no M0 module"; return false; }
            var m0 = library.Get(m0Id);
            if (!Place(library, m0Id, m0Id + " (M0)", "level", 8, m0 != null && m0.depth >= 5 ? 19 : 20, out failure)) return false;

            // The fixed chain serving the gallery's south arch, and the gallery.
            if (!Place(library, "PL_Chain", "PL_Chain (gallery)", "level", 14, 17, out failure)) return false;
            if (!Place(library, "PL_Gallery", "PL_Gallery", "level", 15, 19, out failure)) return false;

            if (!Place(library, l1Id, l1Id + " (L1)", "level", 0, 24, out failure)) return false;
            if (!Place(library, "PL_BasinRoom", "PL_BasinRoom (M1)", "level", 8, 24, out failure)) return false;

            // The EXIT chain: a PL_Chain whose north-east cell (N) is dry at 0 with a steady lamp.
            var exitChain = library.Get("PL_Chain");
            if (exitChain == null || exitChain.pool == null || exitChain.width != 2 || exitChain.depth != 2) { failure = "PL_Chain is not a 2 x 2 pool module"; return false; }
            exitChain.pool.Normalize(2, 2);
            if (exitChain.pool.cells.Length != 4) exitChain.pool.cells = new[] { PoolRules.DefaultCell(ZoneTheme.PoolLevel), PoolRules.DefaultCell(ZoneTheme.PoolLevel), PoolRules.DefaultCell(ZoneTheme.PoolLevel), PoolRules.DefaultCell(ZoneTheme.PoolLevel) };
            exitChain.pool.cells[3] = new ModuleCell { water = CellWater.Dry };
            exitChain.lamps[3] = ModuleLamp.Steady;
            if (!PlaceModule(exitChain, "PL_Chain", "PL_Chain (EXIT)", "level", 13, 26, out failure)) return false;

            var stair = PlaceRoom(library, "PL_FireStair", "PL_FireStair", "level", 11, 28, out failure);
            if (stair == null) return false;

            // The dark zone: the chain behind the gate, the dark office and basin, two chains (5 rooms).
            var darkChain = PlaceRoom(library, "PL_Dark", "PL_Dark (gate)", "dark", 10, 32, out failure);
            if (darkChain == null) return false;
            var darkOffice = PoolRules.DarkOf(library.Get("PL_OfficeCeiling"));
            if (!PlaceModule(darkOffice, "PL_Dark_OfficeCeiling", "dark office", "dark", 8, 34, out failure)) return false;
            var darkBasin = PoolRules.DarkOf(library.Get("PL_BasinRoom"));
            if (!PlaceModule(darkBasin, "PL_Dark_BasinRoom", "dark basin", "dark", 4, 34, out failure)) return false;
            if (!Place(library, "PL_Dark", "PL_Dark (west 1)", "dark", 2, 34, out failure)) return false;
            if (!Place(library, "PL_Dark", "PL_Dark (west 2)", "dark", 2, 36, out failure)) return false;

            // The slots: a forced slot m first (so its count holds), then every slot in table order by weight among what fits.
            string forcedM = null;
            if (options != null && !string.IsNullOrEmpty(options.slotM) && library.Has(options.slotM)) forcedM = options.slotM;
            if (forcedM != null) Count(forcedM);
            for (var k = 0; k < Slots.Length; k++)
            {
                var slot = Slots[k];
                string id;
                if (slot.name == "m" && forcedM != null) id = forcedM;
                else
                {
                    var choices = slot.name == "m" ? new[] { "PL_Court", "PL_Chain", "PL_Weird" } : new[] { "PL_Chain", "PL_Weird" };
                    id = PickWeighted(library, choices, slot.w, slot.h, Unit(SaltSlotPick, k));
                    if (id == null) { failure = "slot " + slot.name + ": nothing fits"; return false; }
                    Count(id);
                }
                var m = library.Get(id);
                if (m == null || m.width > slot.w || m.depth > slot.h) { failure = "slot " + slot.name + ": " + id + " does not fit " + slot.w + " x " + slot.h; return false; }
                if (!PlaceModule(m, id, id + " (slot " + slot.name + ")", "level", slot.x, slot.y, out failure)) return false;
            }

            // Fixed links across parts: the approach into the club, the far door into the level, the gate into the dark zone.
            fixedLinks.Add(LinkKey(approach.index, RoomById("PC_Entry").index));
            fixedLinks.Add(LinkKey(hall.index, turn.index));
            fixedLinks.Add(LinkKey(stair.index, darkChain.index));

            // Zones: every plan cell its room's synthetic zone, Void cells one Standard PoolLevel zone.
            voidZone = MakeZone(VoidZoneIndex, ZoneHeight.Standard, ZoneTheme.PoolLevel, B(8, 20));
            zoneList.Add(voidZone);
            foreach (var chunk in planChunks)
            {
                var o = MapGrid.ChunkOrigin(chunk);
                for (var j = 0; j < MapGrid.ChunkCells; j++)
                for (var i = 0; i < MapGrid.ChunkCells; i++)
                {
                    var cell = new GridCoord(o.x + i, o.y + j);
                    var r = RoomIndex(cell);
                    zoneCells[cell] = r >= 0 ? rooms[r].zone : voidZone;
                }
            }

            // Edges: every edge with a plan cell on a side, then the repair, then the borders.
            foreach (var chunk in planChunks)
            {
                var o = MapGrid.ChunkOrigin(chunk);
                for (var j = 0; j < MapGrid.ChunkCells; j++)
                for (var i = 0; i < MapGrid.ChunkCells; i++)
                {
                    var cell = new GridCoord(o.x + i, o.y + j);
                    SetEdge(cell, new GridCoord(cell.x + 1, cell.y));
                    SetEdge(cell, new GridCoord(cell.x, cell.y + 1));
                    SetEdge(new GridCoord(cell.x - 1, cell.y), cell);
                    SetEdge(new GridCoord(cell.x, cell.y - 1), cell);
                }
            }
            Repair();

            // Per chunk arrays.
            foreach (var chunk in planChunks) chunkData[chunk] = MakeChunk(chunk);

            // Touched: the plan's chunks and their ring.
            foreach (var chunk in planChunks)
                for (var dy = -1; dy <= 1; dy++)
                for (var dx = -1; dx <= 1; dx++)
                {
                    var c = new GridCoord(chunk.x + dx, chunk.y + dy);
                    if (!touched.Contains(c)) touched.Add(c);
                }

            Hash = MakeHash();
            Built = true;
            return true;
        }

        // Hash salts of the pool planner (the generator's are 11-73, the exit planner's 79 and 83, the arch's 97, the station's 233).
        internal const int SaltOrigin = 241, SaltL0Pick = 242, SaltM0Pick = 243, SaltSlotPick = 244;

        float Unit(int salt, int k) => MapHash.Unit(MapHash.Hash(Seed, OriginChunk.x, OriginChunk.y, salt, k));

        void Count(string id)
        {
            used.TryGetValue(id, out var n);
            used[id] = n + 1;
        }

        // A weighted pick among the modules that fit w x h and are under their maxPerRun; null when none.
        string PickWeighted(FrontRoomsPoolLibrary library, string[] ids, int w, int h, float roll)
        {
            var total = 0f;
            var fit = new List<string>();
            var weights = new List<float>();
            foreach (var id in ids)
            {
                var m = library.Get(id);
                if (m == null || m.weight <= 0f || m.width > w || m.depth > h) continue;
                var most = m.pool != null ? m.pool.maxPerRun : 0;
                used.TryGetValue(id, out var n);
                if (most > 0 && n >= most) continue;
                fit.Add(id);
                weights.Add(m.weight);
                total += m.weight;
            }
            if (fit.Count == 0) return null;
            var t = roll * total;
            for (var k = 0; k < fit.Count; k++)
            {
                if (t < weights[k]) return fit[k];
                t -= weights[k];
            }
            return fit[fit.Count - 1];
        }

        bool Place(FrontRoomsPoolLibrary library, string id, string label, string part, int bx, int by, out string failure) =>
            PlaceRoom(library, id, label, part, bx, by, out failure) != null;

        PoolRoom PlaceRoom(FrontRoomsPoolLibrary library, string id, string label, string part, int bx, int by, out string failure)
        {
            var m = library.Get(id);
            if (m == null) { failure = id + " is not in the library"; return null; }
            return PlaceModule(m, id, label, part, bx, by, out failure) ? rooms[rooms.Count - 1] : null;
        }

        bool PlaceModule(RoomModuleData m, string id, string label, string part, int bx, int by, out string failure)
        {
            failure = null;
            if (m == null) { failure = id + ": no module"; return false; }
            m.Normalize();
            if (m.pool == null) m.pool = PoolRules.Default(m.theme).Clone();
            m.pool.Normalize(m.width, m.depth);
            var room = new PoolRoom { id = id, label = label, part = part, origin = B(bx, by), width = m.width, depth = m.depth, module = m };
            // Every room lies inside one plan chunk (DESIGN 7's table does), so a chunk's rooms[] holds it whole.
            var c0 = MapGrid.ChunkOf(room.origin);
            var c1 = MapGrid.ChunkOf(new GridCoord(room.origin.x + room.width - 1, room.origin.y + room.depth - 1));
            if (c0 != c1 || !planChunks.Contains(c0)) { failure = label + " at block (" + bx + ", " + by + ") leaves its chunk"; return false; }
            for (var j = 0; j < room.depth; j++)
            for (var i = 0; i < room.width; i++)
            {
                var cell = new GridCoord(room.origin.x + i, room.origin.y + j);
                if (roomOfCell.ContainsKey(cell)) { failure = label + " overlaps " + rooms[roomOfCell[cell]].label + " at " + cell; return false; }
            }
            AddRoom(room, m.height, m.theme);
            return true;
        }

        void AddRoom(PoolRoom room, ZoneHeight height, ZoneTheme theme)
        {
            room.index = rooms.Count;
            room.zone = MakeZone(room.index, height, theme, new GridCoord(0, 0));
            room.zone.siteX = room.CentreX;
            room.zone.siteZ = room.CentreZ;
            rooms.Add(room);
            zoneList.Add(room.zone);
            for (var j = 0; j < room.depth; j++)
            for (var i = 0; i < room.width; i++)
                roomOfCell[new GridCoord(room.origin.x + i, room.origin.y + j)] = room.index;
        }

        ZoneInfo MakeZone(int index, ZoneHeight height, ZoneTheme theme, GridCoord site) =>
            new ZoneInfo
            {
                id = new GridCoord(FrontRoomsMapGenerator.PoolZoneBase + index, FrontRoomsMapGenerator.PoolZoneBase),
                height = height,
                theme = theme,
                siteX = (site.x + .5f) * MapGrid.CellSize,
                siteZ = (site.y + .5f) * MapGrid.CellSize,
            };

        static long LinkKey(int a, int b) => a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;

        static bool IsGlass(PoolEdgeStyle s) => s == PoolEdgeStyle.ObsWindow || s == PoolEdgeStyle.ClearGlass;

        static int Rank(ModuleEdgeRecord r) =>
            IsGlass(r.style) ? 4 : r.style == PoolEdgeStyle.Gate ? 3 : r.style == PoolEdgeStyle.RoundArch ? 2 : r.archWidth > 0f ? 1 : 0;

        // The module's perimeter edge of room's cell toward (dx, dy), with its record if it has one.
        static ModuleEdge Perimeter(PoolRoom room, GridCoord cell, int dx, int dy, out ModuleEdgeRecord record, out bool hasRecord)
        {
            record = default;
            hasRecord = false;
            if (room.module == null) return ModuleEdge.Wall;
            int i = cell.x - room.origin.x, j = cell.y - room.origin.y;
            ModuleSide side;
            int index;
            if (dx == 1) { side = ModuleSide.East; index = j; }
            else if (dx == -1) { side = ModuleSide.West; index = j; }
            else if (dy == 1) { side = ModuleSide.North; index = i; }
            else { side = ModuleSide.South; index = i; }
            var m = room.module;
            var along = side == ModuleSide.South || side == ModuleSide.North ? m.width : m.depth;
            if (index < 0 || index >= along) return ModuleEdge.Wall;
            foreach (var r in m.edgeRecords ?? new ModuleEdgeRecord[0])
                if (r.side == side && r.index == index) { record = r; hasRecord = true; break; }
            return m.PerimeterAt(side, index);
        }

        // A round arch's crown in map metres: the author's base added, under both ceilings less the clearance.
        ModuleEdgeRecord Settle(ModuleEdgeRecord r, PoolRoom author, PoolRoom a, PoolRoom b)
        {
            if (r.style != PoolEdgeStyle.RoundArch) return r;
            var top = Math.Min(a.CeilingY, b.CeilingY) - PoolRules.CrownClearance;
            var crown = author.BaseY + r.crown;
            r.crown = crown > top ? top : crown;
            return r;
        }

        void SetEdge(GridCoord a, GridCoord b)
        {
            var id = MapGrid.EdgeId(a, b);
            if (edges.ContainsKey(id)) return;
            edges[id] = Rule(a, b, out var record, out var hasRecord);
            if (hasRecord) records[id] = record;
        }

        // DESIGN 7 "Edges", for a the west or south cell and b its east or north neighbour.
        EdgeKind Rule(GridCoord a, GridCoord b, out ModuleEdgeRecord record, out bool hasRecord)
        {
            record = default;
            hasRecord = false;
            if ((a == EntranceOutside && b == EntranceIn) || (a == EntranceIn && b == EntranceOutside)) return EdgeKind.Arch;
            int dx = b.x - a.x, dy = b.y - a.y;
            int ra = RoomIndex(a), rb = RoomIndex(b);
            if (ra == Outside || rb == Outside) return EdgeKind.Wall;
            if (ra == VoidRoom && rb == VoidRoom) return EdgeKind.Wall;
            if (ra == VoidRoom || rb == VoidRoom)
            {
                // A room's glass facing nothing keeps its record (the window is built); any other opening into Void is a wall.
                var room = ra == VoidRoom ? rooms[rb] : rooms[ra];
                var cell = ra == VoidRoom ? b : a;
                int sx = ra == VoidRoom ? -dx : dx, sy = ra == VoidRoom ? -dy : dy;
                Perimeter(room, cell, sx, sy, out var r, out var has);
                if (has && IsGlass(r.style)) { record = r; hasRecord = true; }
                return EdgeKind.Wall;
            }
            if (ra == rb)
            {
                var room = rooms[ra];
                if (room.module == null) return EdgeKind.Open;
                return RoomModuleData.ToEdge(room.module.EdgeAt(a.x - room.origin.x, a.y - room.origin.y, dx, dy));
            }
            var A = rooms[ra];
            var Bm = rooms[rb];
            if (A.part != Bm.part && !fixedLinks.Contains(LinkKey(A.index, Bm.index))) return EdgeKind.Wall;
            if (A.part == "approach" || Bm.part == "approach")
                if (!fixedLinks.Contains(LinkKey(A.index, Bm.index))) return EdgeKind.Wall;
            var kA = Perimeter(A, a, dx, dy, out var recA, out var hasA);
            var kB = Perimeter(Bm, b, -dx, -dy, out var recB, out var hasB);
            if (hasA && IsGlass(recA.style)) { record = recA; hasRecord = true; return EdgeKind.Wall; }
            if (hasB && IsGlass(recB.style)) { record = recB; hasRecord = true; return EdgeKind.Wall; }
            if (kA == ModuleEdge.Door || kB == ModuleEdge.Door) return EdgeKind.Door;
            var openA = kA == ModuleEdge.Arch || kA == ModuleEdge.Open;
            var openB = kB == ModuleEdge.Arch || kB == ModuleEdge.Open;
            if (!openA && !openB) return EdgeKind.Wall;
            var useA = openA && hasA;
            var useB = openB && hasB;
            if (useA && useB)
            {
                var pa = Rank(recA);
                var pb = Rank(recB);
                if (pb > pa || (pb == pa && recB.archWidth > recA.archWidth)) useA = false;
                else useB = false;
            }
            if (useA) { record = Settle(recA, A, A, Bm); hasRecord = true; }
            else if (useB) { record = Settle(recB, Bm, A, Bm); hasRecord = true; }
            return EdgeKind.Arch;
        }

        // ---------- Repair (DESIGN 7, by walking) ----------

        // DESIGN 7's union-find over rooms misses rooms that a height splits: PL_Leisure's weir drops one way into its basin,
        // and a basin room's arch can lead down into another's, so rooms can be joined on paper and still leave a pit. The
        // repair therefore grows "home", the cells that can be walked to from the entrance and walk back to it, one opening
        // at a time: a wall between a home cell and a room cell outside home, where the two floors meet within a step (so
        // the opening walks both ways), inside one part, never the approach or the fire stair; of those, the middle of the
        // longest run toward one room (ties: the lower room index). A Python port of this plan over 1,440 plans (60 seeds,
        // 8 blocks, 3 option sets) left none unreached and none with a pit, with at most 4 openings a plan (2.8 on average).
        void Repair()
        {
            for (var guard = 0; guard < 200; guard++)
            {
                var fwd = Walk(EdgeOf, null, null);
                var back = WalkBack(EdgeOf);
                var missing = 0;
                foreach (var pair in roomOfCell)
                    if (!fwd.ContainsKey(pair.Key) || !back.Contains(pair.Key)) missing++;
                if (missing == 0) return;
                var runs = new Dictionary<int, List<long>>();
                foreach (var pair in edges)
                {
                    if (pair.Value != EdgeKind.Wall || records.ContainsKey(pair.Key)) continue;
                    DecodeEdge(pair.Key, out var a, out var b);
                    int ra = RoomIndex(a), rb = RoomIndex(b);
                    if (ra < 0 || rb < 0 || ra == rb) continue;
                    var ha = fwd.ContainsKey(a) && back.Contains(a);
                    var hb = fwd.ContainsKey(b) && back.Contains(b);
                    if (ha == hb) continue;
                    if (!MayRepair(rooms[ra], rooms[rb], a, b)) continue;
                    var other = ha ? rb : ra;
                    if (!runs.TryGetValue(other, out var list)) runs[other] = list = new List<long>();
                    list.Add(pair.Key);
                }
                if (runs.Count == 0)
                {
                    notes.Add("Repair: " + missing + " room cell(s) neither reach the entrance and back nor have a wall that may open.");
                    return;
                }
                var best = -1;
                foreach (var pair in runs)
                    if (best < 0 || pair.Value.Count > runs[best].Count || (pair.Value.Count == runs[best].Count && pair.Key < best)) best = pair.Key;
                var run = runs[best];
                run.Sort();
                var pick = ClearOf(run);
                DecodeEdge(pick, out var pa, out var pb);
                var A = RoomAt(pa);
                var Bm = RoomAt(pb);
                edges[pick] = EdgeKind.Arch;
                records[pick] = RepairRecord(A, Bm);
                Repairs++;
                notes.Add("Repair: opened " + A.label + " to " + Bm.label + " at " + pa + "-" + pb + ".");
            }
        }

        const float RepairArch = 1.4f;

        // The arch Repair opens between two rooms: 1.4 m, round-headed with its crown 2.3 m over the base between two
        // PoolLevel rooms, else the map's rect arch.
        ModuleEdgeRecord RepairRecord(PoolRoom A, PoolRoom Bm)
        {
            var rec = new ModuleEdgeRecord { archWidth = RepairArch };
            if (A.zone.theme == ZoneTheme.PoolLevel && Bm.zone.theme == ZoneTheme.PoolLevel)
            {
                rec.style = PoolEdgeStyle.RoundArch;
                rec.crown = 2.3f;
                rec = Settle(rec, A, A, Bm);
            }
            return rec;
        }

        // The middle of the run, or the edge nearest it (one either side in turn, the lower first) that no kit of either room
        // stands across (KitsAcross). The level-space line's scene 4 route P, 11-05: on every variant B seed the arch into
        // PL_PoMo opened in its west wall behind the room's own glass block wall (1.10 of its 1.40 m), and the player walked
        // through the panel. The room the opening leads to is chosen as before; only its place along that run moves. Every
        // edge covered: the middle all the same (its kits are then left out, MarkKitsAcross).
        long ClearOf(List<long> run)
        {
            var mid = run.Count / 2;
            var cs = MapGrid.CellSize;
            for (var step = 0; step < 2 * run.Count; step++)
            {
                var i = mid + (step + 1) / 2 * (step % 2 == 1 ? -1 : 1);
                if (i < 0 || i >= run.Count) continue;
                DecodeEdge(run[i], out var a, out var b);
                var rec = RepairRecord(RoomAt(a), RoomAt(b));
                if (KitsAcross(run[i], cs * .5f, RepairArch * .5f, OpeningTop(EdgeKind.Arch, rec, a, b, RepairArch), rec.style == PoolEdgeStyle.RoundArch, KitSpanMargin, null) == 0)
                    return run[i];
            }
            return run[mid];
        }

        // The opening's top in map metres, as the pool pass builds it (FrontRoomsMapWorld.Pool.cs, the arch branch; keep the
        // two in step): a round arch's crown; else the rect or gate head, ModuleUnits.ArchTop over the lower base under both
        // ceilings by the header and 2 m over the higher floor at least; a door the map's DoorHeight over 0.
        float OpeningTop(EdgeKind kind, ModuleEdgeRecord rec, GridCoord a, GridCoord b, float width)
        {
            if (kind == EdgeKind.Door) return ModuleUnits.DoorHeight;
            PoolRoom A = RoomAt(a), B = RoomAt(b);
            var ceilMin = Math.Min(A != null ? A.CeilingY : 99f, B != null ? B.CeilingY : 99f);
            var baseMin = Math.Min(A != null ? A.BaseY : 0f, B != null ? B.BaseY : 0f);
            var floorMax = Math.Max(CellOf(a).floorY, CellOf(b).floorY);
            if (rec.style == PoolEdgeStyle.RoundArch)
            {
                var crown = Math.Min(rec.crown > 0f || baseMin < -.01f ? rec.crown : baseMin + 2.3f, ceilMin - PoolRules.CrownClearance);
                crown = Math.Max(crown, floorMax + width * .5f + 1.2f);
                return Math.Min(crown, ceilMin - .05f);
            }
            var top = baseMin + Math.Min(ModuleUnits.ArchTop, ceilMin - baseMin - ModuleUnits.ArchHeaderMin);
            return top < floorMax + 2f ? Math.Min(floorMax + 2f, ceilMin - .05f) : top;
        }

        // A kit stands across an opening when its box (ModuleStandIn.Footprint) comes within KitReach of the wall line, starts
        // under the opening's top, and overlaps its span (plus a margin either side) at the kit's lowest point (a round head
        // narrows the span above its springing, so a pipe or a clock beside the curve is clear). Repair keeps KitSpanMargin
        // clear; a kit is left out only where it truly overlaps (margin 0).
        const float KitReach = .35f, KitSpanMargin = .1f, KitTopMargin = .02f;

        /// <summary>
        /// The kits of either room standing across an opening of the edge between two cells: centred <paramref name="centre"/>
        /// metres along the edge from its west or south end, <paramref name="half"/> its half-width, <paramref name="top"/> its
        /// top in map metres, <paramref name="round"/> for a round head, <paramref name="margin"/> metres either side. Each is
        /// added to <paramref name="into"/> (room index times 65536 plus the kit's index); with null the count stops at the first.
        /// </summary>
        int KitsAcross(long edge, float centre, float half, float top, bool round, float margin, HashSet<long> into)
        {
            DecodeEdge(edge, out var a, out var b);
            var cs = MapGrid.CellSize;
            var alongX = a.y != b.y;
            var line = (alongX ? b.y : b.x) * cs;
            var start = (alongX ? a.x : a.y) * cs;
            var found = 0;
            for (var side = 0; side < 2; side++)
            {
                var cell = side == 0 ? a : b;
                var room = RoomAt(cell);
                if (room == null || room.module == null) continue;
                var kits = PoolRules.SkinOf(room.module).standIns;
                if (kits == null) continue;
                var floor = CellOf(cell).floorY;
                for (var k = 0; k < kits.Length; k++)
                {
                    var s = kits[k];
                    if (!s.Footprint(out var hx, out var hz, out var bottom)) continue;
                    var low = (s.fromBase ? room.BaseY : floor) + s.y + bottom;
                    if (low >= top - KitTopMargin) continue;
                    var yaw = s.yaw * (Math.PI / 180.0);
                    float cos = (float)Math.Abs(Math.Cos(yaw)), sin = (float)Math.Abs(Math.Sin(yaw));
                    float px = room.origin.x * cs + s.x, pz = room.origin.y * cs + s.z;
                    // The kit's X axis runs along (cos, -sin) in plan x, z and its Z axis along (sin, cos) (Unity's yaw).
                    float off, along, offExtent, alongExtent;
                    if (alongX) { off = Math.Abs(pz - line); along = px - start; offExtent = sin * hx + cos * hz; alongExtent = cos * hx + sin * hz; }
                    else { off = Math.Abs(px - line); along = pz - start; offExtent = cos * hx + sin * hz; alongExtent = sin * hx + cos * hz; }
                    if (off - offExtent > KitReach) continue;
                    var span = half;
                    var spring = top - half;
                    if (round && low > spring) span = (float)Math.Sqrt(Math.Max(0f, half * half - (low - spring) * (low - spring)));
                    if (along + alongExtent <= centre - span - margin || along - alongExtent >= centre + span + margin) continue;
                    found++;
                    if (into == null) return found;
                    into.Add(((long)room.index << 16) | (uint)k);
                }
            }
            return found;
        }

        readonly HashSet<long> kitsLeftOut = new HashSet<long>();

        /// <summary>A kit of the room's module (its index in the skin's standIns) that an opening of the plan crosses: the pool pass leaves it out.</summary>
        public bool KitLeftOut(PoolRoom room, int kit) => room != null && kitsLeftOut.Contains(((long)room.index << 16) | (uint)kit);

        // Every opening between two rooms that a kit stands across (a module's own arch meeting its neighbour's wall kit, or
        // a Repair arch with no clear edge): the kit is left out (a fixture cannot hang where the wall is open), with a note.
        // An arch without an authored width is where the map's hash puts it (MapArch.Opening, the plan's Seed is the map's);
        // a door's whole possible span counts.
        void MarkKitsAcross()
        {
            kitsLeftOut.Clear();
            var cs = MapGrid.CellSize;
            var hits = new HashSet<long>();
            foreach (var pair in edges)
            {
                if (pair.Value != EdgeKind.Arch && pair.Value != EdgeKind.Door) continue;
                DecodeEdge(pair.Key, out var a, out var b);
                int ra = RoomIndex(a), rb = RoomIndex(b);
                if (ra < 0 || rb < 0 || ra == rb) continue;
                OpeningOf(pair.Key, pair.Value, a, b, out var centre, out var half, out var top, out var round);
                hits.Clear();
                if (KitsAcross(pair.Key, centre, half, top, round, 0f, hits) == 0) continue;
                foreach (var key in hits)
                {
                    if (!kitsLeftOut.Add(key)) continue;
                    var room = rooms[(int)(key >> 16)];
                    notes.Add("Left out: " + PoolRules.SkinOf(room.module).standIns[(int)(key & 0xffff)].kit + " of " + room.label + ", across the opening at " + a + "-" + b + ".");
                }
            }
        }

        // An Arch or Door edge's opening: its centre and half-width along the edge from the west or south end, its top in map
        // metres, a round head. An arch without an authored width is where the map's hash puts it (MapArch.Opening, the plan's
        // Seed is the map's); a door counts its whole possible span.
        void OpeningOf(long id, EdgeKind kind, GridCoord a, GridCoord b, out float centre, out float half, out float top, out bool round)
        {
            var cs = MapGrid.CellSize;
            records.TryGetValue(id, out var rec);
            float width;
            if (kind == EdgeKind.Door) { width = cs - 2f * ModuleUnits.ArchCornerMargin; centre = cs * .5f; }
            else if (rec.archWidth > 0f) { width = rec.archWidth; centre = cs * .5f; }
            else MapArch.Opening(Seed, a, a.y != b.y, out width, out centre);
            half = width * .5f;
            round = kind == EdgeKind.Arch && rec.style == PoolEdgeStyle.RoundArch;
            top = OpeningTop(kind, rec, a, b, width);
        }

        // A clear glass wall's pane: between 0.2 m jambs at either end of the edge, up to its head 2.7 m over the higher base.
        // The pool pass builds the glass wall from these (FrontRoomsMapWorld.Pool.cs, BuildPoolEdge's ClearGlass branch), so
        // the pane OpeningSpanAt keeps wall lamps clear of is the pane that is built.
        internal const float GlassJamb = .2f, GlassHead = 2.7f;

        /// <summary>
        /// What a wall lamp on the edge between two neighbouring cells keeps clear of at height <paramref name="y"/> (map
        /// metres), as metres along the edge from its west or south end; false where the wall is solid there. An Arch or Door
        /// edge's opening under its top (a round head narrows above its springing); a clear glass wall's pane under its head
        /// (nothing to hang a lamp on); the EXIT door, N to X (not an edge of the plan: X is its Void and the map's exit rule
        /// builds the door), the map's centred door with its frame as scene 4's sign rule counts a door (ModuleUnits.TrimFace
        /// and TrimProud a side, 1.18 m), at every height: its head trim (to 2.151) and the EXIT sign over it (2.18 to 2.392)
        /// stand over a Standard valance's foot, and N is Standard. The pool pass hangs its valances clear of it
        /// (FrontRoomsMapWorld.PoolValances; the level-space line's p_recheck4, 11-05: PL_Chain (slot n)'s south valance ran in
        /// front of PL_Court's glass on every variant A plan, PL_Chain (EXIT)'s north one over the EXIT door's head trim on all).
        /// </summary>
        public bool OpeningSpanAt(GridCoord a, GridCoord b, float y, out float from, out float to)
        {
            from = to = 0f;
            if (b.x < a.x || b.y < a.y) { var t = a; a = b; b = t; }
            var cs = MapGrid.CellSize;
            if (a == FrontCell && b == ExitCell)
            {
                var door = ModuleUnits.DoorWidth * .5f + ModuleUnits.TrimFace + ModuleUnits.TrimProud;
                from = cs * .5f - door;
                to = cs * .5f + door;
                return true;
            }
            var id = MapGrid.EdgeId(a, b);
            if (!edges.TryGetValue(id, out var kind)) return false;
            if (kind == EdgeKind.Wall)
            {
                if (!records.TryGetValue(id, out var glass) || glass.style != PoolEdgeStyle.ClearGlass) return false;
                PoolRoom A = RoomAt(a), B = RoomAt(b);
                if (y >= Math.Max(A != null ? A.BaseY : 0f, B != null ? B.BaseY : 0f) + GlassHead) return false;
                from = GlassJamb;
                to = cs - GlassJamb;
                return true;
            }
            if (kind != EdgeKind.Arch && kind != EdgeKind.Door) return false;
            OpeningOf(id, kind, a, b, out var centre, out var half, out var top, out var round);
            if (y >= top) return false;
            var span = half;
            var spring = top - half;
            if (round && y > spring) span = (float)Math.Sqrt(Math.Max(0f, half * half - (y - spring) * (y - spring)));
            from = centre - span;
            to = centre + span;
            return true;
        }

        // Only inside one part, never the approach, never the fire stair's sides, only where the floors meet within a step.
        bool MayRepair(PoolRoom a, PoolRoom b, GridCoord ca, GridCoord cb)
        {
            if (a.part != b.part || a.part == "approach") return false;
            if (a.id == "PL_FireStair" || b.id == "PL_FireStair") return false;
            int dx = cb.x - ca.x, dy = cb.y - ca.y;
            var fa = PoolRules.EdgeFloor(CellOf(ca), dx, dy);
            var fb = PoolRules.EdgeFloor(CellOf(cb), -dx, -dy);
            return Math.Abs(fa - fb) <= PoolRules.StepUp + 1e-3f;
        }

        // MapGrid.EdgeId's inverse: a the west or south cell, b its east or north neighbour.
        static void DecodeEdge(long id, out GridCoord a, out GridCoord b)
        {
            var east = (id & 1L) != 0;
            var rest = id ^ (east ? 1L : 0L);
            var x = (int)(rest >> 33);
            var yBits = (int)((rest >> 1) & 0xffffffffL);
            // EdgeId keeps y's low 31 bits (a.y & 0x7fffffff) and xors x into the high bits; undo both.
            var y = yBits & 0x7fffffff;
            if ((y & 0x40000000) != 0) y |= unchecked((int)0x80000000);
            a = new GridCoord(x, y);
            b = east ? new GridCoord(x + 1, y) : new GridCoord(x, y + 1);
        }

        // ---------- Per chunk ----------

        ChunkData MakeChunk(GridCoord chunk)
        {
            var d = new ChunkData();
            var o = MapGrid.ChunkOrigin(chunk);
            const int n = MapGrid.ChunkCells;
            for (var j = 0; j < n; j++)
            for (var i = 0; i < n; i++)
            {
                var cell = new GridCoord(o.x + i, o.y + j);
                var k = MapGrid.LocalIndex(i, j);
                var zone = zoneCells[cell];
                d.zone[k] = zone.id;
                d.height[k] = zone.height;
                d.east[k] = EdgeOf(cell, new GridCoord(cell.x + 1, cell.y));
                d.north[k] = EdgeOf(cell, new GridCoord(cell.x, cell.y + 1));
                var r = RoomIndex(cell);
                d.pool[k] = CellOf(cell);
                if (r == VoidRoom) d.lamp[k] = ModuleLamp.Off;
                else if (rooms[r].module == null) d.lamp[k] = ModuleLamp.Auto;
                else d.lamp[k] = rooms[r].module.LampAt(cell.x - rooms[r].origin.x, cell.y - rooms[r].origin.y);
            }
            for (var k = 0; k < n; k++)
            {
                d.west[k] = EdgeOf(new GridCoord(o.x - 1, o.y + k), new GridCoord(o.x, o.y + k));
                d.south[k] = EdgeOf(new GridCoord(o.x + k, o.y - 1), new GridCoord(o.x + k, o.y));
                // Every border edge of a plan chunk goes to the generator's override, so both chunks' copies agree.
                Border(new GridCoord(o.x - 1, o.y + k), new GridCoord(o.x, o.y + k));
                Border(new GridCoord(o.x + k, o.y - 1), new GridCoord(o.x + k, o.y));
                Border(new GridCoord(o.x + n - 1, o.y + k), new GridCoord(o.x + n, o.y + k));
                Border(new GridCoord(o.x + k, o.y + n - 1), new GridCoord(o.x + k, o.y + n));
            }
            var list = new List<CellRect>();
            var modules = new List<RoomModuleData>();
            foreach (var room in rooms)
            {
                if (room.module == null || MapGrid.ChunkOf(room.origin) != chunk) continue;
                list.Add(new CellRect(room.origin.x - o.x, room.origin.y - o.y, room.width, room.depth));
                modules.Add(room.module);
            }
            d.rooms = list.ToArray();
            d.roomModules = modules.ToArray();
            foreach (var pair in records)
            {
                DecodeEdge(pair.Key, out var a, out _);
                if (MapGrid.ChunkOf(a) == chunk) d.poolEdges[pair.Key] = pair.Value;
            }
            return d;
        }

        void Border(GridCoord a, GridCoord b) => borders[MapGrid.EdgeId(a, b)] = EdgeOf(a, b);

        string MakeHash()
        {
            unchecked
            {
                var h = 2166136261u;
                void Mix(int v) { h = (h ^ (uint)v) * 16777619u; }
                Mix(Seed);
                Mix(OriginChunk.x);
                Mix(OriginChunk.y);
                foreach (var r in rooms)
                {
                    foreach (var ch in r.id) Mix(ch);
                    Mix(r.origin.x);
                    Mix(r.origin.y);
                }
                foreach (var pair in edges)
                {
                    if (pair.Value == EdgeKind.Wall) continue;
                    Mix((int)(pair.Key & 0x7fffffff));
                    Mix((int)(pair.Key >> 31));
                    Mix((int)pair.Value);
                }
                return h.ToString("x8");
            }
        }

        // ---------- Walking (the step rule) ----------

        static readonly GridCoord[] Steps4 = { new GridCoord(1, 0), new GridCoord(-1, 0), new GridCoord(0, 1), new GridCoord(0, -1) };

        // A step from one room cell to its neighbour: a walkable edge, and at most a capsule's step up where the two floors meet.
        bool CanStep(GridCoord from, GridCoord to, Func<GridCoord, GridCoord, EdgeKind> edgeOf)
        {
            if (RoomIndex(to) < 0) return false;
            if (!FrontRoomsExitPlanner.Walkable(edgeOf(from, to))) return false;
            int dx = to.x - from.x, dy = to.y - from.y;
            var up = PoolRules.EdgeFloor(CellOf(to), -dx, -dy) - PoolRules.EdgeFloor(CellOf(from), dx, dy);
            return up <= PoolRules.StepUp + 1e-3f;
        }

        // Breadth first from the entrance over room cells; skip says which cells it never enters.
        Dictionary<GridCoord, GridCoord> Walk(Func<GridCoord, GridCoord, EdgeKind> edgeOf, Func<GridCoord, bool> skip, List<GridCoord> order)
        {
            var from = new Dictionary<GridCoord, GridCoord>();
            var queue = new Queue<GridCoord>();
            from[EntranceIn] = EntranceIn;
            queue.Enqueue(EntranceIn);
            order?.Add(EntranceIn);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                foreach (var s in Steps4)
                {
                    var next = cell + s;
                    if (from.ContainsKey(next) || (skip != null && skip(next)) || !CanStep(cell, next, edgeOf)) continue;
                    from[next] = cell;
                    order?.Add(next);
                    queue.Enqueue(next);
                }
            }
            return from;
        }

        // The cells from which the entrance can be walked to (reverse breadth first).
        HashSet<GridCoord> WalkBack(Func<GridCoord, GridCoord, EdgeKind> edgeOf)
        {
            var back = new HashSet<GridCoord> { EntranceIn };
            var queue = new Queue<GridCoord>();
            queue.Enqueue(EntranceIn);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                foreach (var s in Steps4)
                {
                    var prev = cell + s;
                    if (back.Contains(prev) || RoomIndex(prev) < 0 || !CanStep(prev, cell, edgeOf)) continue;
                    back.Add(prev);
                    queue.Enqueue(prev);
                }
            }
            return back;
        }

        // ---------- Validate (C12) ----------

        /// <summary>
        /// C12 (DESIGN 7): every module's own checks; from the entrance over walkable edges and the step rule (up at most
        /// 0.3 where two floors meet, down any): every room reached, N reached without dark cells, every reached cell can
        /// walk back (no pit), the fire stair and its gate the only way into the dark zone; every door's leaf dry; N dry at
        /// 0 and Standard; X chunk-local 1..6; columns 2.4 m apart and off inner walls. With a cache, edges are read as the
        /// cache builds them (the Apply path); without, from the plan.
        /// </summary>
        public void Validate(FrontRoomsMapCache cache, List<string> errors, List<string> warnings)
        {
            if (errors == null) errors = new List<string>();
            if (warnings == null) warnings = new List<string>();
            if (!Built) { errors.Add("The plan is not built."); return; }
            Func<GridCoord, GridCoord, EdgeKind> edgeOf = cache != null ? (Func<GridCoord, GridCoord, EdgeKind>)cache.Edge : EdgeOf;

            foreach (var room in rooms)
            {
                if (room.module == null) continue;
                var e = new List<string>();
                var w = new List<string>();
                PoolRules.SkinOf(room.module).Validate(room.module, e, w);
                foreach (var s in e) errors.Add(room.label + ": " + s);
                foreach (var s in w) warnings.Add(room.label + ": " + s);
            }

            var reached = Walk(edgeOf, null, null);
            foreach (var room in rooms)
            {
                var any = false;
                for (var j = 0; j < room.depth && !any; j++)
                for (var i = 0; i < room.width && !any; i++)
                    any = reached.ContainsKey(new GridCoord(room.origin.x + i, room.origin.y + j));
                if (!any) errors.Add(room.label + " is not reached from the entrance.");
            }

            var dry = Walk(edgeOf, c => { var r = RoomAt(c); return r != null && r.IsDark; }, null);
            if (!dry.ContainsKey(FrontCell)) errors.Add("N " + FrontCell + " is not reached without the dark zone.");

            var back = WalkBack(edgeOf);
            foreach (var cell in reached.Keys)
                if (!back.Contains(cell)) { errors.Add("No way back to the entrance from " + cell + " (" + RoomAt(cell)?.label + ")."); break; }

            var noStair = Walk(edgeOf, c => { var r = RoomAt(c); return r != null && r.id == "PL_FireStair"; }, null);
            foreach (var cell in noStair.Keys)
            {
                var r = RoomAt(cell);
                if (r != null && r.IsDark) { errors.Add("The dark zone is reached without the fire stair (at " + cell + ")."); break; }
            }
            var stair = RoomById("PL_FireStair");
            if (stair != null)
            {
                var top = new GridCoord(stair.origin.x, stair.origin.y + stair.depth - 1);
                if (!TryRecord(top, new GridCoord(top.x, top.y + 1), out var gate) || gate.style != PoolEdgeStyle.Gate)
                    errors.Add("The fire stair's north edge carries no gate record.");
            }

            foreach (var pair in edges)
            {
                if (pair.Value != EdgeKind.Door) continue;
                DecodeEdge(pair.Key, out var a, out var b);
                var ca = CellOf(a);
                var cb = CellOf(b);
                if ((ca.HasWater && ca.waterY > PoolRules.AnkleWater + 1e-3f) || (cb.HasWater && cb.waterY > PoolRules.AnkleWater + 1e-3f))
                    errors.Add("The door at " + a + "-" + b + " stands in water over " + PoolRules.AnkleWater.ToString("0.00") + ".");
            }

            var n = CellOf(FrontCell);
            if (n.water != CellWater.Dry || Math.Abs(n.floorY) > 1e-3f || n.connector != CellConnector.None) errors.Add("N " + FrontCell + " is not dry at 0.");
            if (zoneCells.TryGetValue(FrontCell, out var nz) && nz.height != ZoneHeight.Standard) errors.Add("N " + FrontCell + " is not Standard height.");
            var xo = MapGrid.ChunkOrigin(MapGrid.ChunkOf(ExitCell));
            int xi = ExitCell.x - xo.x, xj = ExitCell.y - xo.y;
            if (xi < 1 || xi > MapGrid.ChunkCells - 2 || xj < 1 || xj > MapGrid.ChunkCells - 2) errors.Add("X " + ExitCell + " is at chunk-local (" + xi + ", " + xj + "), outside 1..6.");

            foreach (var room in rooms)
            {
                var m = room.module;
                if (m == null || m.columns != ModuleColumns.Custom) continue;
                var cols = m.customColumns ?? new ModuleColumn[0];
                for (var p = 0; p < cols.Length; p++)
                {
                    var c = cols[p];
                    if (c.x <= 0 || c.y <= 0 || c.x >= m.width || c.y >= m.depth) continue;
                    // The four inner edges meeting at the corner stay open, so no column stands in a doorway or a wall.
                    if (m.EdgeAt(c.x - 1, c.y - 1, 1, 0) != ModuleEdge.Open || m.EdgeAt(c.x - 1, c.y, 1, 0) != ModuleEdge.Open
                        || m.EdgeAt(c.x - 1, c.y - 1, 0, 1) != ModuleEdge.Open || m.EdgeAt(c.x, c.y - 1, 0, 1) != ModuleEdge.Open)
                        errors.Add(room.label + ": the column at (" + c.x + ", " + c.y + ") meets an inner wall or doorway.");
                    for (var q = p + 1; q < cols.Length; q++)
                    {
                        var dx = (cols[q].x - c.x) * MapGrid.CellSize;
                        var dz = (cols[q].y - c.y) * MapGrid.CellSize;
                        if (dx * dx + dz * dz < 2.4f * 2.4f - 1e-3f) errors.Add(room.label + ": columns closer than 2.4 m.");
                    }
                }
            }
        }

        // ---------- Accept (FrontRoomsPoolPlanner) ----------

        internal void Accept(int stepsToEntrance, List<string> warnings, double ms)
        {
            walkToEntrance = stepsToEntrance;
            var order = new List<GridCoord>();
            var from = Walk(EdgeOf, null, order);
            var inside = 0;
            if (from.ContainsKey(FrontCell))
                for (var c = FrontCell; c != EntranceIn; c = from[c]) inside++;
            walkInsideToFront = inside;
            var distance = stepsToEntrance + 1 + inside + 1;
            exit = new ExitPlan { found = true, exitCell = ExitCell, frontCell = FrontCell, distance = distance, bandMin = distance, bandMax = distance, candidates = 1 };
            if (warnings != null) foreach (var w in warnings) notes.Add(w);
            MakeFocus(from, order);
            MarkKitsAcross();
            planMs = ms;
            Ready = true;
        }

        // ---------- Focus points ----------

        void MakeFocus(Dictionary<GridCoord, GridCoord> from, List<GridCoord> order)
        {
            focus.Clear();
            var cs = MapGrid.CellSize;
            // A room's stop stands 1 m in from the edge by which the walk first reaches a cell of it out of the basin (no Wade,
            // no stair, no void). The walk takes any drop, so a room's first cell can be its basin: the level-space line's route P, 11-05,
            // M0 (PL_Leisure) entered from M1 by its north arch straight into 0.75 m of water on 4 of 39 seeds. A room with no
            // such cell keeps its first.
            var entry = new Dictionary<int, GridCoord>();
            var standing = new HashSet<int>();
            foreach (var cell in order)
            {
                var r = RoomIndex(cell);
                if (r < 0 || standing.Contains(r)) continue;
                var c = CellOf(cell);
                if (c.water != CellWater.Wade && c.connector == CellConnector.None) { entry[r] = cell; standing.Add(r); }
                else if (!entry.ContainsKey(r)) entry[r] = cell;
            }

            PoolFocus RoomFocus(string name, PoolRoom room)
            {
                if (room == null) return Point(name, EntranceIn, cs * .5f, cs * .5f, 0f, 6f);
                if (!entry.TryGetValue(room.index, out var c) || !from.TryGetValue(c, out var p) || p == c)
                    return Point(name, room.origin, cs * .5f, cs * .5f, 0f, 6f);
                int dx = c.x - p.x, dy = c.y - p.y;
                var mx = (c.x + p.x + 1) * cs * .5f;
                var mz = (c.y + p.y + 1) * cs * .5f;
                var f = new PoolFocus { name = name, cell = c, x = mx + dx * 1.0f, z = mz + dy * 1.0f };
                f.y = FloorAt(f.x, f.z);
                var ex = room.CentreX - f.x;
                var ez = room.CentreZ - f.z;
                if (ex * ex + ez * ez < 2.5f * 2.5f) { f.lookX = f.x + dx * 6f; f.lookZ = f.z + dy * 6f; }
                else { f.lookX = room.CentreX; f.lookZ = room.CentreZ; }
                f.lookY = FloorAt(f.lookX, f.lookZ) + 1.5f;
                return f;
            }

            PoolFocus Point(string name, GridCoord cell, float u, float v, float lookDx, float lookDz)
            {
                var f = new PoolFocus { name = name, cell = cell, x = cell.x * cs + u, z = cell.y * cs + v };
                f.y = FloorAt(f.x, f.z);
                f.lookX = f.x + lookDx;
                f.lookZ = f.z + lookDz;
                f.lookY = f.y + 1.5f;
                return f;
            }

            var hall = RoomById("PC_Hall");
            var stair = RoomById("PL_FireStair");
            focus.Add(Point(FocusNames[0], EntranceIn, cs * .5f, .9f, 0f, 6f));
            focus.Add(RoomFocus(FocusNames[1], RoomById("PC_Entry")));
            focus.Add(RoomFocus(FocusNames[2], RoomById("PC_Lockers")));
            focus.Add(RoomFocus(FocusNames[3], hall));
            if (hall != null)
            {
                // In front of the far door (the hall's north round arch at block x 3), looking north through it.
                var c = B(3, 15);
                var f = Point(FocusNames[4], c, cs * .5f, cs * .4f, 0f, 7f);
                f.lookY = f.y + 1.6f;
                focus.Add(f);
            }
            else focus.Add(Point(FocusNames[4], EntranceIn, cs * .5f, cs * .5f, 0f, 6f));
            focus.Add(RoomFocus(FocusNames[5], RoomById("PL_Turn")));
            focus.Add(RoomFocus(FocusNames[6], RoomAt(B(2, 18))));
            focus.Add(RoomFocus(FocusNames[7], RoomAt(B(8, 20))));
            focus.Add(RoomFocus(FocusNames[8], RoomById("PL_Gallery")));
            focus.Add(RoomFocus(FocusNames[9], RoomAt(B(0, 24))));
            focus.Add(RoomFocus(FocusNames[10], RoomAt(B(8, 24))));
            {
                // N's south cell, facing the EXIT door (N to X).
                var c = new GridCoord(FrontCell.x, FrontCell.y - 1);
                var f = Point(FocusNames[11], c, cs * .5f, cs * .5f, 0f, cs * 2f - cs * .5f);
                f.lookY = 1.2f;
                focus.Add(f);
            }
            if (stair != null)
            {
                var head = Point(FocusNames[12], stair.origin, cs * .5f, .6f, 0f, cs * 2.2f);
                head.lookY = head.y - 1.0f;
                focus.Add(head);
                var foot = Point(FocusNames[13], new GridCoord(stair.origin.x, stair.origin.y + stair.depth - 1), cs * .5f, .9f, 0f, cs * 1.4f);
                foot.lookY = foot.y + 1.4f;
                focus.Add(foot);
            }
            else
            {
                focus.Add(Point(FocusNames[12], EntranceIn, cs * .5f, cs * .5f, 0f, 6f));
                focus.Add(Point(FocusNames[13], EntranceIn, cs * .5f, cs * .5f, 0f, 6f));
            }
            focus.Add(RoomFocus(FocusNames[14], RoomAt(B(10, 32))));
            focus.Add(RoomFocus(FocusNames[15], RoomAt(B(8, 34))));
            focus.Add(RoomFocus(FocusNames[16], RoomAt(B(4, 34))));
        }

        // ---------- Status ----------

        /// <summary>The route in a few lines for the guide panel and the log (DESIGN 8's Status).</summary>
        public string Describe()
        {
            var sb = new StringBuilder();
            if (!Ready && !Built) return "Pool route: no plan (" + (error ?? "not built") + ").";
            sb.Append("Pool route: map seed ").Append(Seed).Append(", block chunk ").Append(OriginChunk)
              .Append(" (cells ").Append(OriginCell).Append(" to ").Append(B(15, 39)).Append("), ").Append(optionsText).Append(", hash ").Append(Hash).AppendLine(".");
            sb.Append("Rooms: ");
            for (var k = 0; k < rooms.Count; k++)
            {
                if (k > 0) sb.Append(", ");
                sb.Append(rooms[k].label);
            }
            sb.AppendLine(".");
            sb.Append("X ").Append(ExitCell).Append(", N ").Append(FrontCell).Append("; walking cells: start door to the entrance ").Append(walkToEntrance)
              .Append(", entrance to N ").Append(walkInsideToFront).Append("; repairs ").Append(Repairs).Append("; plan ").Append(planMs.ToString("0")).AppendLine(" ms.");
            var dark = 0;
            foreach (var r in rooms) if (r.IsDark) dark++;
            sb.Append("Dark zone: ").Append(dark).AppendLine(" rooms behind the fire stair and its gate. Relay kept out until P3.");
            foreach (var note in notes) sb.AppendLine(note);
            return sb.ToString();
        }
    }

    /// <summary>
    /// The pool route's planner (DESIGN 7): candidate blocks above the floor's start door, sorted by the seed; the first
    /// that builds, walks from the start door to its entrance in 10 to 40 cells and validates clean is held by the cache
    /// and returned Ready. None fits: a plan that is not Ready, with its error, and the cache holds no plan.
    /// </summary>
    public static class FrontRoomsPoolPlanner
    {
        /// <summary>LEVEL B2 (FloorLabel: LEVEL 0 on floor 1, B1 on 2, B2 on 3).</summary>
        public const int PoolFloor = 3;
        /// <summary>Chunks per pool-level zone (DESIGN 7; the 3 to 5 asked).</summary>
        public const int ZoneChunks = 4;
        /// <summary>Walking cells from the start door to the entrance's outside cell.</summary>
        public const int BandMin = 10, BandMax = 40;

        /// <summary>
        /// Plan the route for a floor whose start area and door are given (map cells). The cache is left holding the plan
        /// when it is Ready; chunks the cache made before it are dropped (they regenerate the same under the plan).
        /// </summary>
        public static FrontRoomsPoolPlan Plan(FrontRoomsMapCache cache, FrontRoomsPoolLibrary library, CellRect startArea, GridCoord door, int floorSeed, FrontRoomsPoolPlanOptions options = null)
        {
            var started = DateTime.UtcNow;
            var seed = cache != null ? cache.Generator.Seed : floorSeed;
            var none = new FrontRoomsPoolPlan(default(GridCoord), seed, floorSeed);
            if (cache == null) { none.error = "no map cache"; return none; }
            if (library == null || !library.Complete) { none.error = "pool modules missing: " + (library == null ? "all" : library.Missing); return none; }
            cache.ClearPool();
            var dc = MapGrid.ChunkOf(door);
            var candidates = new List<GridCoord>();
            for (var cy = dc.y + 1; cy <= dc.y + 2; cy++)
            for (var cx = dc.x - 2; cx <= dc.x + 1; cx++)
                candidates.Add(new GridCoord(cx, cy));
            candidates.Sort((p, q) =>
            {
                var hp = MapHash.Hash(seed, p.x, p.y, FrontRoomsPoolPlan.SaltOrigin);
                var hq = MapHash.Hash(seed, q.x, q.y, FrontRoomsPoolPlan.SaltOrigin);
                if (hp != hq) return hp.CompareTo(hq);
                return p.x != q.x ? p.x.CompareTo(q.x) : p.y.CompareTo(q.y);
            });
            var reasons = new StringBuilder();
            foreach (var origin in candidates)
            {
                if (BlockHolds(origin, dc, startArea)) { Reason(reasons, origin, "holds the start door or area"); continue; }
                var before = new List<GridCoord>();
                foreach (var c in cache.Built) before.Add(c.coord);
                var plan = new FrontRoomsPoolPlan(origin, seed, floorSeed);
                if (!plan.Build(library, options, out var why)) { Reason(reasons, origin, why); continue; }
                cache.SetPool(plan);
                var steps = FrontRoomsExitPlanner.WalkStepsTo(cache, startArea, door, plan.EntranceOutside, plan.ExitCell, BandMax);
                if (steps < BandMin || steps > BandMax)
                {
                    cache.ClearPool();
                    Reason(reasons, origin, steps < 0 ? "entrance not reached within " + BandMax + " cells" : "entrance " + steps + " cells from the door");
                    continue;
                }
                var errors = new List<string>();
                var warnings = new List<string>();
                plan.Validate(cache, errors, warnings);
                if (errors.Count > 0)
                {
                    cache.ClearPool();
                    Reason(reasons, origin, errors.Count + " check(s) failed, first: " + errors[0]);
                    continue;
                }
                foreach (var coord in before) cache.Drop(coord);
                plan.Accept(steps, warnings, (DateTime.UtcNow - started).TotalMilliseconds);
                return plan;
            }
            cache.ClearPool();
            none.error = "no block fits:" + reasons;
            none.planMs = (DateTime.UtcNow - started).TotalMilliseconds;
            return none;
        }

        static void Reason(StringBuilder sb, GridCoord origin, string why) => sb.Append(" chunk ").Append(origin).Append(": ").Append(why).Append(';');

        // A block chunk holds the door's chunk, or a cell of the start area or its ring.
        static bool BlockHolds(GridCoord origin, GridCoord doorChunk, CellRect startArea)
        {
            foreach (var s in FrontRoomsPoolPlan.ChunkSteps)
            {
                var chunk = new GridCoord(origin.x + s.x, origin.y + s.y);
                if (chunk == doorChunk) return true;
                if (startArea.w <= 0 || startArea.h <= 0) continue;
                for (var y = startArea.y - 1; y <= startArea.y + startArea.h; y++)
                for (var x = startArea.x - 1; x <= startArea.x + startArea.w; x++)
                    if (MapGrid.ChunkOf(new GridCoord(x, y)) == chunk) return true;
            }
            return false;
        }
    }
}
