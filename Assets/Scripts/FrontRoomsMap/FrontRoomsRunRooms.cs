using System;
using System.Collections.Generic;

namespace FrontRooms.Map
{
    /// <summary>A Run! room's alarm (CR-2): armed until the player steps 1.5 m in, tripped until the zone resets (the Relay's WITHDRAW).</summary>
    public enum RunRoomState : byte { Armed, Tripped }

    /// <summary>
    /// The Run! rules for one map, from the level profile (FrontRoomsLevelProfile.RunRules). Plain data. Every switch is
    /// off by default, and with runRooms off no FrontRoomsRunRooms exists at all (today's map, byte for byte).
    /// </summary>
    [Serializable]
    public sealed class FrontRoomsRunRules
    {
        /// <summary>Call 2 (CR-6a): the goal door is a forced Door edge wherever the far cell is (in the room's own zone, or a Low
        /// zone). Off (CR-6b): the corridor must end on a Standard-to-Low border, where the map's rule makes a door anyway.</summary>
        public bool goalDoorForced;
        /// <summary>Call 5: Run! rooms in Office zones too (the same B look). Off: Level 0 zones only.</summary>
        public bool office;
        /// <summary>Tools only (the 100-seed check, -autopilotRunRooms): every zone met once the map is ready is treated as met
        /// at tier minTier or later and after the run's first send, and the budget roll always passes.</summary>
        public bool force;
        /// <summary>RELAY 6 (系统设计 2026-10-31): Run! rooms are the tier-2 trigger kind.</summary>
        public int minTier = 2;
        /// <summary>RELAY 6: no trigger within 15 cells of the start door (walking cells, bounded below by the Manhattan distance used here).</summary>
        public int minStartCells = 15;
        /// <summary>CR-5: a Relay entry (a module marker) within this many cells of a Run! room is not offered to the Relay.</summary>
        public int minEntryCells = 9;
        /// <summary>The trigger budget, one device per this many zones generated at tier t (index t - 1; FrontRoomsTierV2.zonesPerDevice).</summary>
        public float[] zonesPerDevice = { 3f, 2.5f, 2f, 1.75f, 1.5f };

        /// <summary>The chance a zone met at <paramref name="tier"/> holds a Run! room (1 / zonesPerDevice; 1 when forced).</summary>
        public float ChanceAt(int tier)
        {
            if (force) return 1f;
            if (zonesPerDevice == null || zonesPerDevice.Length == 0) return 0f;
            var z = zonesPerDevice[Math.Max(0, Math.Min(zonesPerDevice.Length - 1, tier - 1))];
            return z > 0f ? Math.Min(1f, 1f / z) : 0f;
        }

        /// <summary>The highest chance at any tier: a zone whose roll is over it never competes for a chunk (FrontRoomsRunRooms.Priority).</summary>
        public float MaxChance
        {
            get
            {
                if (force) return 1f;
                var best = 0f;
                for (var t = 1; zonesPerDevice != null && t <= zonesPerDevice.Length; t++) best = Math.Max(best, ChanceAt(t));
                return best;
            }
        }

        public FrontRoomsRunRules Clone()
        {
            var c = (FrontRoomsRunRules)MemberwiseClone();
            c.zonesPerDevice = (float[])zonesPerDevice?.Clone();
            return c;
        }
    }

    /// <summary>
    /// One Run! room (L7, CR-1): a 1 x 9 straight of Standard cells of its zone's theme (it may straddle zones of that height
    /// and theme, as a carved room may), owned by the zone of the arch's outside cell <see cref="Outside"/> (the host zone),
    /// ending in the goal door onto <see cref="Beyond"/>. Corridor-local frame (the visual chat's
    /// FrontRoomsRunRig): z = 0 on the entrance cell line, +z along <see cref="AxisX"/>/<see cref="AxisY"/> to the goal wall at
    /// z = 27; x = 0 on the axis. Map-space metres (the map root's local space), plain C#.
    /// </summary>
    public sealed class FrontRoomsRunRoom
    {
        /// <summary>A stable id within the floor (the order rooms were planned in).</summary>
        public int Id;
        /// <summary>The host zone: the arch's outside cell's (CR-1: "on its own zone's side"); its tier and theme are the room's.</summary>
        public GridCoord Zone;
        /// <summary>Every zone its cells lie in (the host zone first).</summary>
        public GridCoord[] Zones;
        /// <summary>The 9 cells, from the entrance (Cells[0]) to the goal (Cells[8]).</summary>
        public GridCoord[] Cells;
        /// <summary>The unit step from the entrance to the goal.</summary>
        public int AxisX, AxisY;
        /// <summary>The arch's outside cell (Cells[0] - axis) and the goal door's far cell (Cells[8] + axis).</summary>
        public GridCoord Outside, Beyond;
        /// <summary>The two chunks it lies in (the entrance's, the goal's).</summary>
        public GridCoord ChunkA, ChunkB;
        /// <summary>The zone's tier when it was met (RELAY 12), and the far cell's height (Standard: CR-6a's forced door; Low: a border door).</summary>
        public int Tier;
        public ZoneHeight BeyondHeight;
        public ZoneTheme Theme;

        // ---------- The alarm (CR-2), kept by the room so drops and rebuilds keep it ----------
        public RunRoomState State;
        /// <summary>Re-armed with the player inside (a reset): it trips again only after the player has left it.</summary>
        public bool WaitForExit;
        /// <summary>How many times it tripped this floor, and the map's clock at the last trip (the world's; -1 never).</summary>
        public int Trips;
        public float TrippedAt = -1f;

        public const int Length = FrontRoomsRunRooms.Length;
        public float LengthMetres => Length * MapGrid.CellSize;

        /// <summary>The corridor's yaw in degrees (0 = +Z, 90 = +X): the rig's local +z in map space.</summary>
        public float Yaw => AxisY == 1 ? 0f : AxisX == 1 ? 90f : AxisY == -1 ? 180f : 270f;

        /// <summary>The entrance edge's centre on the cell line, at floor level (corridor-local origin), map space x and z.</summary>
        public float OriginX => (Cells[0].x + .5f - AxisX * .5f) * MapGrid.CellSize;
        public float OriginZ => (Cells[0].y + .5f - AxisY * .5f) * MapGrid.CellSize;

        /// <summary>Corridor-local (x, z) to map space (x, z).</summary>
        public void ToMap(float localX, float localZ, out float mapX, out float mapZ)
        {
            // Right of the axis (local +x) is the axis turned clockwise: (AxisY, -AxisX).
            mapX = OriginX + AxisX * localZ + AxisY * localX;
            mapZ = OriginZ + AxisY * localZ - AxisX * localX;
        }

        /// <summary>Map space (x, z) to corridor-local z (along the axis from the entrance line) and x.</summary>
        public void ToLocal(float mapX, float mapZ, out float localX, out float localZ)
        {
            var dx = mapX - OriginX;
            var dz = mapZ - OriginZ;
            localZ = dx * AxisX + dz * AxisY;
            localX = dx * AxisY - dz * AxisX;
        }

        public bool Contains(GridCoord cell)
        {
            for (var k = 0; k < Cells.Length; k++) if (Cells[k] == cell) return true;
            return false;
        }

        /// <summary>The trip test (CR-2): local z at least ArmDepth in from either opening (the arch, the goal door).</summary>
        public bool InArmedZone(float mapX, float mapZ)
        {
            ToLocal(mapX, mapZ, out var x, out var z);
            return z >= FrontRoomsRunRooms.ArmDepth && z <= LengthMetres - FrontRoomsRunRooms.ArmDepth && Math.Abs(x) < MapGrid.CellSize * .5f;
        }

        public override string ToString() => "run " + Id + " zone " + Zone + " " + Cells[0] + "->" + Cells[Length - 1] + " (arch from " + Outside + ", goal door to " + Beyond + " " + BeyondHeight + ")";
    }

    /// <summary>
    /// Run! rooms (L7) as map data: the map side of CR-1 (the 27 m straight), CR-3 (finish, data), CR-4 (lamps Off), CR-5
    /// (placement) and CR-6 (the goal door's edge). Held by the cache (FrontRoomsMapCache.SetRunRooms) like the A26 carve,
    /// only while the level profile's runRooms is on; with it off nothing here exists and every map is today's.
    ///
    /// How a room comes to be (CR-1, CR-5):
    /// - A zone is decided once, when the cache first meets it (the moment its tier is recorded, RELAY 12): a room only if the
    ///   map is Ready (Begin: the start area, station and EXIT are set), the run's first send has gone out (Open, A40), the
    ///   zone's tier is at least minTier (T2), it is not the arrival zone (P2), it is Standard Level 0 (Office too with call
    ///   5), and its budget roll passes (1 / the tier's zonesPerDevice). Zones met earlier never get one.
    /// - The candidate is a pure function of the seed, the zone and the rules (Candidate): a straight of 9 cells, all Standard
    ///   and of the host zone's theme (it may straddle zones, as a carved room may), entered through an arch from a cell of the
    ///   host zone. Both long sides lie inside a chunk (perpendicular chunk-local 1..6) and both ends off a chunk border (the
    ///   entrance cell's chunk-local 1..6 along the axis), so it crosses exactly one chunk border, between two of its own cells,
    ///   and every edge it writes lies in one of its two chunks; both chunks hold a host cell, so neither is generated before
    ///   the host zone is met and decided. The goal's far cell is any Standard or Low cell with call 2 (CR-6a), a Low cell
    ///   without it (CR-6b: a Standard-to-Low border). No cell is reserved (start area, X, the station) or next to one, none is
    ///   any zone's key cell (RELAY 6: the key never only inside a trigger room), no long side faces a Tall zone (no glass and
    ///   no C33 arch is walled), every cell and the arch's outside cell are 15+ cells (Manhattan) from the start door.
    /// - Placed only if every other zone it touches is not the arrival zone and was either not met yet or met at minTier+
    ///   after the first send (ZonesStillOk).
    /// - At most one zone's corridor per chunk: of two candidates sharing a chunk the lower priority hash wins (Priority),
    ///   whether or not the winner is placed, so no plan depends on the order chunks are generated in.
    /// Apply (each Get, after the carve): rooms over the corridor are dropped (columns and key spot again), the corridor's
    /// inside edges Open, its long sides Wall, the entrance Arch (1.4 m, centred: TryArch), the goal edge Door; its lamps Off
    /// (CR-4); then the rest of the chunk is joined again without the corridor (the corridor is never the only way round).
    /// </summary>
    public sealed class FrontRoomsRunRooms
    {
        /// <summary>CR-1: 1 x 9 cells, 27 m.</summary>
        public const int Length = 9;
        /// <summary>CR-1 (10_run_directions 1.2): the entrance arch, 1.4 m wide, centred.</summary>
        public const float ArchWidth = 1.4f;
        /// <summary>CR-2: the room arms 1.5 m inside.</summary>
        public const float ArmDepth = 1.5f;
        /// <summary>CR-2: the tag RoomTriggered carries.</summary>
        public const string Tag = "run";

        const int BudgetSalt = 0x52554e31, PickSalt = 0x52554e32, PrioritySalt = 0x52554e33;
        const int N = MapGrid.ChunkCells;

        readonly FrontRoomsRunRules rules;
        FrontRoomsMapCache cache;
        FrontRoomsMapGenerator gen;

        // Per zone: the decision (null: none) and the memoised candidate (null: none fits or it does not compete).
        readonly Dictionary<GridCoord, FrontRoomsRunRoom> decided = new Dictionary<GridCoord, FrontRoomsRunRoom>();
        readonly Dictionary<GridCoord, FrontRoomsRunRoom> candidates = new Dictionary<GridCoord, FrontRoomsRunRoom>();
        readonly Dictionary<GridCoord, FrontRoomsRunRoom> byCell = new Dictionary<GridCoord, FrontRoomsRunRoom>();
        readonly Dictionary<GridCoord, List<FrontRoomsRunRoom>> byChunk = new Dictionary<GridCoord, List<FrontRoomsRunRoom>>();
        readonly Dictionary<long, EdgeKind> borderOverride = new Dictionary<long, EdgeKind>();
        readonly Dictionary<long, FrontRoomsRunRoom> archEdges = new Dictionary<long, FrontRoomsRunRoom>();
        readonly Dictionary<long, FrontRoomsRunRoom> goalEdges = new Dictionary<long, FrontRoomsRunRoom>();
        readonly List<FrontRoomsRunRoom> rooms = new List<FrontRoomsRunRoom>();
        // Every zone the cache has met: its tier then, and whether the run's first send had gone out (A40).
        readonly Dictionary<GridCoord, int> metTier = new Dictionary<GridCoord, int>();
        readonly HashSet<GridCoord> metOpen = new HashSet<GridCoord>();
        // Each zone's key cell (PlaceKey's search, or the carve's move), memoised.
        readonly Dictionary<GridCoord, GridCoord> keyCells = new Dictionary<GridCoord, GridCoord>();

        public FrontRoomsRunRooms(FrontRoomsRunRules rules) { this.rules = (rules ?? new FrontRoomsRunRules()).Clone(); }

        public FrontRoomsRunRules Rules => rules;

        // ---------- What the world sets (FrontRoomsMapWorld) ----------

        /// <summary>The map is set up (Begin): zones met before it (the EXIT and start-group planning) never get a room.</summary>
        public bool Ready { get; set; }
        /// <summary>The run's first send has gone out (A40). Kept by the world for the run; the game raises it.</summary>
        public bool Open { get; set; }
        /// <summary>The floor's start door cell (RELAY 6's 15 cells) and its zone, the arrival zone (P2).</summary>
        public bool HasStart { get; private set; }
        public GridCoord StartDoor { get; private set; }
        public GridCoord ArrivalZone { get; private set; }
        /// <summary>Cells no corridor, arch cell or goal cell may use: the start area, X, the station and a ring round each.</summary>
        public Func<GridCoord, bool> Blocked { get; set; }

        public void SetStart(GridCoord doorCell)
        {
            HasStart = true;
            StartDoor = doorCell;
            ArrivalZone = gen != null ? gen.ZoneOf(doorCell).id : doorCell;
        }

        internal void Attach(FrontRoomsMapCache owner)
        {
            cache = owner;
            gen = owner.Generator;
            if (HasStart) ArrivalZone = gen.ZoneOf(StartDoor).id;
        }

        // ---------- Counts (the validator and the logs) ----------

        public int ZonesDecided => decided.Count;
        public int NotReady { get; private set; }
        public int Ineligible { get; private set; }
        public int NoFit { get; private set; }
        public int Suppressed { get; private set; }
        public int Placed => rooms.Count;

        /// <summary>Raised when a zone's room is planned (before any of its chunks is generated): the world logs it.</summary>
        public event Action<FrontRoomsRunRoom> Planned;

        // ---------- Queries ----------

        public IReadOnlyList<FrontRoomsRunRoom> Rooms => rooms;
        public IReadOnlyDictionary<long, EdgeKind> BorderOverride => borderOverride;
        public bool IsRunCell(GridCoord cell) => byCell.ContainsKey(cell);
        public FrontRoomsRunRoom RoomAt(GridCoord cell) => byCell.TryGetValue(cell, out var r) ? r : null;
        public bool IsGoalEdge(GridCoord a, GridCoord b) => goalEdges.ContainsKey(MapGrid.EdgeId(a, b));
        public FrontRoomsRunRoom GoalRoom(GridCoord a, GridCoord b) => goalEdges.TryGetValue(MapGrid.EdgeId(a, b), out var r) ? r : null;
        public bool IsArchEdge(GridCoord a, GridCoord b) => archEdges.ContainsKey(MapGrid.EdgeId(a, b));
        public IReadOnlyList<FrontRoomsRunRoom> RoomsIn(GridCoord chunk) => byChunk.TryGetValue(chunk, out var list) ? list : (IReadOnlyList<FrontRoomsRunRoom>)Array.Empty<FrontRoomsRunRoom>();
        public bool Decided(GridCoord zoneId, out FrontRoomsRunRoom room) => decided.TryGetValue(zoneId, out room);

        /// <summary>The entrance arch's span (CR-1): 1.4 m, centred on its edge.</summary>
        public bool TryArch(GridCoord a, GridCoord b, out float width, out float center)
        {
            if (archEdges.ContainsKey(MapGrid.EdgeId(a, b))) { width = ArchWidth; center = MapGrid.CellSize * .5f; return true; }
            width = center = 0f;
            return false;
        }

        /// <summary>The zone's budget roll in [0, 1): pure in the seed and the zone.</summary>
        public float Roll(GridCoord zoneId) => MapHash.Unit(MapHash.Hash(gen.Seed, zoneId.x, zoneId.y, BudgetSalt));

        /// <summary>The zone's priority for a shared chunk: the lower wins (ties: the lower zone id).</summary>
        public uint Priority(GridCoord zoneId) => MapHash.Hash(gen.Seed, zoneId.x, zoneId.y, PrioritySalt);

        // ---------- The decision (CR-5) ----------

        /// <summary>The cache met a zone for the first time, at <paramref name="tier"/>: decide its Run! room once.</summary>
        public void Meet(GridCoord zoneId, int tier)
        {
            if (gen == null || decided.ContainsKey(zoneId)) return;
            metTier[zoneId] = tier;
            if (Ready && (Open || rules.force)) metOpen.Add(zoneId);
            FrontRoomsRunRoom room = null;
            if (!Ready) NotReady++;
            else if (!Eligible(zoneId, tier)) Ineligible++;
            else
            {
                var c = Candidate(zoneId);
                if (c == null) NoFit++;
                else if (SuppressedBy(c) != null) Suppressed++;
                else if (!ZonesStillOk(c)) Ineligible++;
                else room = Place(c, tier);
            }
            decided[zoneId] = room;
        }

        /// <summary>
        /// CR-5 for every zone the candidate's cells touch, as the run stands now: none is the arrival zone, and each was either
        /// not met yet (it will be met at today's tier or later, after the first send) or met at minTier+ after the first send.
        /// </summary>
        bool ZonesStillOk(FrontRoomsRunRoom c)
        {
            foreach (var z in c.Zones)
            {
                if (HasStart && z == ArrivalZone) return false;
                if (z == c.Zone || !metTier.TryGetValue(z, out var t)) continue;
                if (!metOpen.Contains(z)) return false;
                if (!rules.force && t < rules.minTier) return false;
            }
            return true;
        }

        /// <summary>The tier a zone was met at and whether the first send had gone out then (false: not met yet).</summary>
        public bool MetAt(GridCoord zoneId, out int tier, out bool afterFirstSend)
        {
            afterFirstSend = metOpen.Contains(zoneId);
            return metTier.TryGetValue(zoneId, out tier);
        }

        bool Eligible(GridCoord zoneId, int tier)
        {
            if (!rules.force && (!Open || tier < rules.minTier)) return false;
            if (HasStart && zoneId == ArrivalZone) return false;
            if (!Competes(zoneId)) return false;
            return Roll(zoneId) < rules.ChanceAt(rules.force ? rules.minTier : tier);
        }

        // The pure half of eligibility: Standard, Level 0 (or Office with call 5), and a roll under the best chance at any tier.
        bool Competes(GridCoord zoneId)
        {
            var zone = gen.Zone(zoneId);
            if (zone.height != ZoneHeight.Standard) return false;
            if (zone.theme == ZoneTheme.Office && !rules.office) return false;
            return Roll(zoneId) < rules.MaxChance;
        }

        /// <summary>A higher-priority zone whose candidate shares a chunk with <paramref name="c"/>, or null.</summary>
        FrontRoomsRunRoom SuppressedBy(FrontRoomsRunRoom c)
        {
            var mine = Priority(c.Zone);
            var seen = new HashSet<GridCoord>();
            foreach (var chunk in new[] { c.ChunkA, c.ChunkB })
            {
                var o = MapGrid.ChunkOrigin(chunk);
                for (var j = 0; j < N; j++)
                for (var i = 0; i < N; i++)
                {
                    var other = gen.ZoneOf(new GridCoord(o.x + i, o.y + j)).id;
                    if (other == c.Zone || !seen.Add(other)) continue;
                    var oc = Candidate(other);
                    if (oc == null || (oc.ChunkA != c.ChunkA && oc.ChunkA != c.ChunkB && oc.ChunkB != c.ChunkA && oc.ChunkB != c.ChunkB)) continue;
                    var theirs = Priority(other);
                    if (theirs < mine || (theirs == mine && (other.x < c.Zone.x || (other.x == c.Zone.x && other.y < c.Zone.y)))) return oc;
                }
            }
            return null;
        }

        FrontRoomsRunRoom Place(FrontRoomsRunRoom c, int tier)
        {
            var room = new FrontRoomsRunRoom
            {
                Id = rooms.Count + 1, Zone = c.Zone, Zones = c.Zones, Cells = c.Cells, AxisX = c.AxisX, AxisY = c.AxisY, Outside = c.Outside, Beyond = c.Beyond,
                ChunkA = c.ChunkA, ChunkB = c.ChunkB, Tier = tier, BeyondHeight = c.BeyondHeight, Theme = c.Theme,
            };
            rooms.Add(room);
            foreach (var cell in room.Cells) byCell[cell] = room;
            Index(room.ChunkA, room);
            if (room.ChunkB != room.ChunkA) Index(room.ChunkB, room);
            for (var k = 0; k + 1 < Length; k++)
            {
                var a = room.Cells[k];
                var b = room.Cells[k + 1];
                if (MapGrid.ChunkOf(a) != MapGrid.ChunkOf(b)) borderOverride[MapGrid.EdgeId(a, b)] = EdgeKind.Open;
            }
            archEdges[MapGrid.EdgeId(room.Outside, room.Cells[0])] = room;
            goalEdges[MapGrid.EdgeId(room.Cells[Length - 1], room.Beyond)] = room;
            Planned?.Invoke(room);
            return room;
        }

        void Index(GridCoord chunk, FrontRoomsRunRoom room)
        {
            if (!byChunk.TryGetValue(chunk, out var list)) byChunk[chunk] = list = new List<FrontRoomsRunRoom>();
            list.Add(room);
        }

        // ---------- The candidate (CR-1; pure in the seed, the zone, the rules and Blocked) ----------

        /// <summary>The zone's one candidate straight (memoised), or null: none fits, or the zone does not compete.</summary>
        public FrontRoomsRunRoom Candidate(GridCoord zoneId)
        {
            if (candidates.TryGetValue(zoneId, out var memo)) return memo;
            FrontRoomsRunRoom result = null;
            if (Competes(zoneId))
            {
                var zoneCells = new List<GridCoord>();
                KeyCellOf(zoneId, zoneCells);
                var found = new List<FrontRoomsRunRoom>();
                // The arch's outside cell is in the host zone: every straight starts one step from one of its cells.
                foreach (var o in zoneCells)
                {
                    Consider(zoneId, new GridCoord(o.x + 1, o.y), 1, 0, false, found);
                    Consider(zoneId, new GridCoord(o.x, o.y + 1), 0, 1, false, found);
                    Consider(zoneId, new GridCoord(o.x - Length, o.y), 1, 0, true, found);
                    Consider(zoneId, new GridCoord(o.x, o.y - Length), 0, 1, true, found);
                }
                if (found.Count > 0)
                    result = found[(int)(MapHash.Hash(gen.Seed, zoneId.x, zoneId.y, PickSalt) % (uint)found.Count)];
            }
            candidates[zoneId] = result;
            return result;
        }

        static int Mod(int v) => ((v % N) + N) % N;

        bool IsBlocked(GridCoord cell) => Blocked != null && Blocked(cell);

        int StartDistance(GridCoord cell) => Math.Abs(cell.x - StartDoor.x) + Math.Abs(cell.y - StartDoor.y);

        // The straight whose low cell is l along (ax, ay); entered at its low end (the arch from l - axis) or, reversed, at its
        // high end (the arch from l + 9 axis). Adds it when it passes.
        void Consider(GridCoord zoneId, GridCoord l, int ax, int ay, bool reversed, List<FrontRoomsRunRoom> found)
        {
            // Long sides inside a chunk; both ends off a chunk border (the low end's along index 1..6: the high end, 8 on, has the same).
            var perp = ax == 1 ? Mod(l.y) : Mod(l.x);
            var along = ax == 1 ? Mod(l.x) : Mod(l.y);
            if (perp < 1 || perp > N - 2 || along < 1 || along > N - 2) return;
            var host = gen.Zone(zoneId);
            var cells = new GridCoord[Length];
            var zones = new List<GridCoord> { zoneId };
            for (var k = 0; k < Length; k++)
            {
                var c = new GridCoord(l.x + ax * k, l.y + ay * k);
                var z = gen.ZoneOf(c);
                // One space (as a carved room across zones): every cell Standard and of the host's theme.
                if (z.height != ZoneHeight.Standard || z.theme != host.theme || IsBlocked(c)) return;
                // RELAY 6: no zone's key lies in it.
                if (KeyCell(z.id) == c) return;
                if (HasStart && StartDistance(c) < rules.minStartCells) return;
                // No glass and no C33 arch on a long side: a Tall neighbour there would be walled.
                var s1 = new GridCoord(c.x + ay, c.y + ax);
                var s2 = new GridCoord(c.x - ay, c.y - ax);
                if (gen.HeightOf(s1) == ZoneHeight.Tall || gen.HeightOf(s2) == ZoneHeight.Tall) return;
                if (!zones.Contains(z.id)) zones.Add(z.id);
                cells[k] = c;
            }
            var low = new GridCoord(l.x - ax, l.y - ay);
            var high = new GridCoord(l.x + ax * Length, l.y + ay * Length);
            var outside = reversed ? high : low;
            var beyond = reversed ? low : high;
            if (gen.ZoneOf(outside).id != zoneId || IsBlocked(outside)) return;
            if (HasStart && StartDistance(outside) < rules.minStartCells) return;
            if (IsBlocked(beyond)) return;
            var far = gen.ZoneOf(beyond);
            if (far.height == ZoneHeight.Tall) return;
            // Call 2 on (6a): the forced door opens onto any Standard or Low cell. Off (6b): a Standard-to-Low border only.
            if (!rules.goalDoorForced && far.height != ZoneHeight.Low) return;
            var ordered = new GridCoord[Length];
            for (var k = 0; k < Length; k++) ordered[k] = reversed ? cells[Length - 1 - k] : cells[k];
            var chunkA = MapGrid.ChunkOf(ordered[0]);
            var chunkB = MapGrid.ChunkOf(ordered[Length - 1]);
            // Both chunks hold a host cell, so neither is generated before the host zone is met (and decided): chunk A holds the
            // arch's outside cell; chunk B is checked.
            if (!HoldsZone(chunkB, zoneId)) return;
            found.Add(new FrontRoomsRunRoom
            {
                Zone = zoneId, Zones = zones.ToArray(), Cells = ordered, AxisX = reversed ? -ax : ax, AxisY = reversed ? -ay : ay, Outside = outside, Beyond = beyond,
                ChunkA = chunkA, ChunkB = chunkB, BeyondHeight = far.height, Theme = host.theme,
            });
        }

        bool HoldsZone(GridCoord chunk, GridCoord zoneId)
        {
            var o = MapGrid.ChunkOrigin(chunk);
            for (var j = 0; j < N; j++)
            for (var i = 0; i < N; i++)
                if (gen.ZoneOf(new GridCoord(o.x + i, o.y + j)).id == zoneId) return true;
            return false;
        }

        /// <summary>A zone's key cell (memoised; a Tall zone, which has no key, reads a cell no corridor can hold).</summary>
        public GridCoord KeyCell(GridCoord zoneId)
        {
            if (keyCells.TryGetValue(zoneId, out var key)) return key;
            key = gen.Zone(zoneId).height == ZoneHeight.Tall ? new GridCoord(int.MinValue, int.MinValue) : KeyCellOf(zoneId, null);
            keyCells[zoneId] = key;
            return key;
        }

        /// <summary>
        /// The zone's key cell (PlaceKey's search: the zone cell nearest its site over the 5 x 5 chunks round it, the first in
        /// scan order on a tie), or the carve's move for it (A26); fills <paramref name="zoneCells"/> (when given) with every cell of the zone
        /// in that window, in scan order.
        /// </summary>
        GridCoord KeyCellOf(GridCoord zoneId, List<GridCoord> zoneCells)
        {
            var zone = gen.Zone(zoneId);
            var best = float.MaxValue;
            var key = new GridCoord(int.MinValue, int.MinValue);
            for (var y = (zoneId.y - 2) * N; y < (zoneId.y + 3) * N; y++)
            for (var x = (zoneId.x - 2) * N; x < (zoneId.x + 3) * N; x++)
            {
                var cell = new GridCoord(x, y);
                if (gen.ZoneOf(cell).id != zoneId) continue;
                zoneCells?.Add(cell);
                var ex = (x + .5f) * MapGrid.CellSize - zone.siteX;
                var ez = (y + .5f) * MapGrid.CellSize - zone.siteZ;
                var d = ex * ex + ez * ez;
                if (d >= best) continue;
                best = d;
                key = cell;
            }
            if (cache != null && cache.CarveKeyMove(zoneId, out var moved)) key = moved;
            return key;
        }

        // ---------- Apply (FrontRoomsMapCache.Get, after the carve) ----------

        /// <summary>Write every room planned through this freshly generated chunk into it (see the class summary).</summary>
        public void Apply(MapChunk chunk)
        {
            if (chunk == null || !byChunk.TryGetValue(chunk.coord, out var list) || list.Count == 0) return;
            var origin = chunk.Origin;
            var mine = new bool[N * N];
            foreach (var room in list)
                foreach (var c in room.Cells)
                {
                    int i = c.x - origin.x, j = c.y - origin.y;
                    if (i >= 0 && i < N && j >= 0 && j < N) mine[MapGrid.LocalIndex(i, j)] = true;
                }
            DropRooms(chunk, mine);
            foreach (var room in list)
            {
                for (var k = 0; k < Length; k++)
                {
                    var c = room.Cells[k];
                    // The long sides.
                    var px = room.AxisY != 0 ? 1 : 0;
                    var py = room.AxisX != 0 ? 1 : 0;
                    SetEdge(chunk, c, new GridCoord(c.x + px, c.y + py), EdgeKind.Wall);
                    SetEdge(chunk, c, new GridCoord(c.x - px, c.y - py), EdgeKind.Wall);
                    if (k + 1 < Length) SetEdge(chunk, c, room.Cells[k + 1], EdgeKind.Open);
                    int i = c.x - origin.x, j = c.y - origin.y;
                    if (i >= 0 && i < N && j >= 0 && j < N) chunk.lamp[MapGrid.LocalIndex(i, j)] = ModuleLamp.Off;
                }
                SetEdge(chunk, room.Outside, room.Cells[0], EdgeKind.Arch);
                SetEdge(chunk, room.Cells[Length - 1], room.Beyond, EdgeKind.Door);
            }
            Reconnect(chunk, mine);
        }

        // Write an edge's kind into this chunk's copy of it (its east/north arrays, or west/south for a border it shares).
        static void SetEdge(MapChunk chunk, GridCoord a, GridCoord b, EdgeKind kind)
        {
            if (b.x < a.x || b.y < a.y) { var t = a; a = b; b = t; }
            var east = b.x == a.x + 1;
            var o = chunk.Origin;
            int ai = a.x - o.x, aj = a.y - o.y, bi = b.x - o.x, bj = b.y - o.y;
            if (ai >= 0 && ai < N && aj >= 0 && aj < N)
            {
                if (east) chunk.east[MapGrid.LocalIndex(ai, aj)] = kind;
                else chunk.north[MapGrid.LocalIndex(ai, aj)] = kind;
            }
            else if (bi >= 0 && bi < N && bj >= 0 && bj < N)
            {
                if (east && bi == 0) chunk.west[bj] = kind;
                else if (!east && bj == 0) chunk.south[bi] = kind;
            }
        }

        // Every room over a corridor cell leaves rooms[] (as the carve's roomDrop): columns and the key spot again, and a dropped
        // module room's lamps go back to the last module room left over the cell, else Auto (the corridor's own go Off after).
        void DropRooms(MapChunk chunk, bool[] mine)
        {
            List<CellRect> kept = null;
            List<RoomModuleData> keptModules = null;
            List<CellRect> droppedModules = null;
            for (var r = 0; r < chunk.rooms.Length; r++)
            {
                var rect = chunk.rooms[r];
                var hit = false;
                for (var j = rect.y; j < rect.y + rect.h && !hit; j++)
                for (var i = rect.x; i < rect.x + rect.w && !hit; i++)
                    hit = mine[MapGrid.LocalIndex(i, j)];
                if (hit && kept == null)
                {
                    kept = new List<CellRect>();
                    keptModules = new List<RoomModuleData>();
                    droppedModules = new List<CellRect>();
                    for (var k = 0; k < r; k++) { kept.Add(chunk.rooms[k]); keptModules.Add(chunk.ModuleOf(k)); }
                }
                if (hit) { if (chunk.ModuleOf(r) != null) droppedModules.Add(rect); }
                else if (kept != null) { kept.Add(rect); keptModules.Add(chunk.ModuleOf(r)); }
            }
            if (kept == null) return;
            chunk.rooms = kept.ToArray();
            chunk.roomModules = keptModules.ToArray();
            gen.PlaceColumns(chunk, chunk.revision);
            gen.PlaceKeySpot(chunk);
            foreach (var rect in droppedModules)
                for (var j = rect.y; j < rect.y + rect.h; j++)
                for (var i = rect.x; i < rect.x + rect.w; i++)
                    chunk.lamp[MapGrid.LocalIndex(i, j)] = KeptLamp(chunk, i, j);
        }

        static ModuleLamp KeptLamp(MapChunk chunk, int i, int j)
        {
            for (var r = chunk.rooms.Length - 1; r >= 0; r--)
            {
                var module = chunk.ModuleOf(r);
                var rect = chunk.rooms[r];
                if (module != null && rect.Contains(i, j)) return module.LampAt(i - rect.x, j - rect.y);
            }
            return ModuleLamp.Auto;
        }

        /// <summary>
        /// Join the chunk's other cells into one piece without the corridor (RELAY 6: a trigger room is never the only way
        /// round): each round opens the first generated wall (scan order) between two pieces, glass last; between two heights it
        /// opens as the map's exit (a door, or a window next to a tall zone). The corridor's own edges are never opened. Always
        /// possible: the corridor stays a chunk-local 1..6 off its long sides and leaves a free row past each end in its chunk.
        /// </summary>
        void Reconnect(MapChunk chunk, bool[] mine)
        {
            var parent = new int[N * N];
            for (var round = 0; round < N * N; round++)
            {
                for (var k = 0; k < parent.Length; k++) parent[k] = k;
                int Find(int k) { while (parent[k] != k) k = parent[k] = parent[parent[k]]; return k; }
                for (var j = 0; j < N; j++)
                for (var i = 0; i < N; i++)
                {
                    var k = MapGrid.LocalIndex(i, j);
                    if (mine[k]) continue;
                    if (i < N - 1 && !mine[k + 1] && MapGrid.Passable(chunk.east[k])) parent[Find(k)] = Find(k + 1);
                    if (j < N - 1 && !mine[k + N] && MapGrid.Passable(chunk.north[k])) parent[Find(k)] = Find(k + N);
                }
                var root = -1;
                var joined = true;
                for (var k = 0; k < parent.Length && joined; k++)
                {
                    if (mine[k]) continue;
                    if (root < 0) root = Find(k);
                    else joined = Find(k) == root;
                }
                if (joined) return;
                var best = -1;
                var bestGlass = true;
                for (var j = 0; j < N && (best < 0 || bestGlass); j++)
                for (var i = 0; i < N && (best < 0 || bestGlass); i++)
                {
                    var k = MapGrid.LocalIndex(i, j);
                    if (mine[k]) continue;
                    for (var e = 0; e < 2; e++)
                    {
                        var other = e == 0 ? (i < N - 1 ? k + 1 : -1) : (j < N - 1 ? k + N : -1);
                        if (other < 0 || mine[other] || Find(k) == Find(other)) continue;
                        if ((e == 0 ? chunk.east[k] : chunk.north[k]) != EdgeKind.Wall) continue;
                        var glass = Kind(chunk, k, e == 0) == EdgeKind.Window;
                        if (best < 0 || (bestGlass && !glass)) { best = e == 0 ? k : N * N + k; bestGlass = glass; }
                    }
                }
                if (best < 0) return;
                var isEast = best < N * N;
                var a = best % (N * N);
                if (isEast) chunk.east[a] = Kind(chunk, a, true); else chunk.north[a] = Kind(chunk, a, false);
            }
        }

        // The map's exit between two side-by-side cells of the chunk: an arch at one height, else a door, or a window next to a tall zone.
        EdgeKind Kind(MapChunk chunk, int index, bool east)
        {
            var a = chunk.Cell(index % N, index / N);
            var b = east ? new GridCoord(a.x + 1, a.y) : new GridCoord(a.x, a.y + 1);
            var ha = gen.HeightOf(a);
            var hb = gen.HeightOf(b);
            return ha == hb ? EdgeKind.Arch : ha == ZoneHeight.Tall || hb == ZoneHeight.Tall ? EdgeKind.Window : EdgeKind.Door;
        }

        // ---------- The module, as data (CR-1, CR-2, CR-3, CR-4, CR-6) ----------

        /// <summary>
        /// Run_AlarmCorridor_1x9 as module data: width 1, depth 9, Standard, the host zone's theme; a Corridor (planned by the
        /// map, never placed into a carved room), trigger Alarm, finish Run, no columns, no fill, every lamp Off (the rig's),
        /// the south edge an Arch (the entrance), the north edge a Door (the goal), the long sides Wall. The planner builds
        /// exactly this; it is not in any profile's pool.
        /// </summary>
        public static RoomModuleData CorridorModule(ZoneTheme theme = ZoneTheme.Level0)
        {
            var m = new RoomModuleData
            {
                width = 1, depth = Length, height = ZoneHeight.Standard, theme = theme, placement = ModulePlacement.Corridor,
                trigger = ModuleTrigger.Alarm, finish = ModuleFinish.Run, columns = ModuleColumns.None, fill = ModuleFill.None,
                allowRotate = true, weight = 0f,
            };
            m.Normalize();
            m.south[0] = ModuleEdge.Arch;
            m.north[0] = ModuleEdge.Door;
            for (var j = 0; j < Length; j++) m.lamps[j] = ModuleLamp.Off;
            return m;
        }
    }
}
