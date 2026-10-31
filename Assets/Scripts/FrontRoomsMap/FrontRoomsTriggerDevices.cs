using System;
using System.Collections.Generic;

namespace FrontRooms.Map
{
    /// <summary>What holds a zone's trigger-budget slot (RELAY 6, 12). Append only.</summary>
    public enum TriggerDeviceKind : byte { None, Run, Foil }

    /// <summary>
    /// The trigger budget (RELAY 6, 12; Relay v2 step V7a): one device per 3 / 2.5 / 2 / 1.75 / 1.5 zones, by the zone's stored
    /// tier. ONE budget for every device the map plans, not one per kind:
    /// - each zone rolls once, pure in the seed and the zone (Roll; the salt Run! rooms rolled with since CR-5, so no Run! plan
    ///   moves), and the roll under 1 / zonesPerDevice at the zone's stored tier (ChanceAt) is the zone's one slot;
    /// - the slot holds at most one device of one kind (the ledger: Take, HolderOf): a Run! room (a Standard zone met at T2+
    ///   after the run's first send, FrontRoomsRunRooms) or a foiled window (a Tall zone, FrontRoomsFoil). Neither planner
    ///   places a device in a zone whose slot the other holds.
    /// Held by the cache (FrontRoomsMapCache.Budget), made by the first planner that needs it (UseBudget); a new cache (a new
    /// run or floor) starts a new one. Plain C#.
    /// </summary>
    public sealed class FrontRoomsTriggerBudget
    {
        /// <summary>The roll's salt: FrontRoomsRunRooms' since CR-5 ("RUN1"), kept so the Run! plan is unchanged by the shared budget.</summary>
        public const int RollSalt = 0x52554e31;

        readonly int seed;
        readonly float[] zonesPerDevice;
        readonly Dictionary<GridCoord, TriggerDeviceKind> holders = new Dictionary<GridCoord, TriggerDeviceKind>();
        readonly int[] counts = new int[3];

        public FrontRoomsTriggerBudget(int seed, float[] zonesPerDevice)
        {
            this.seed = seed;
            // As FrontRoomsRunRules: no table, no device.
            this.zonesPerDevice = zonesPerDevice != null ? (float[])zonesPerDevice.Clone() : new float[0];
        }

        public int Seed => seed;

        /// <summary>One device per this many zones at <paramref name="tier"/> (1-based, clamped to the table; 0 with no table).</summary>
        public float ZonesPerDeviceAt(int tier) => zonesPerDevice.Length == 0 ? 0f : zonesPerDevice[Math.Max(0, Math.Min(zonesPerDevice.Length - 1, tier - 1))];

        /// <summary>The zone's roll in [0, 1): pure in the seed and the zone (the same number FrontRoomsRunRooms.Roll always gave).</summary>
        public float Roll(GridCoord zoneId) => MapHash.Unit(MapHash.Hash(seed, zoneId.x, zoneId.y, RollSalt));

        /// <summary>The chance a zone at <paramref name="tier"/> holds a slot: 1 / zonesPerDevice (FrontRoomsRunRules.ChanceAt's arithmetic).</summary>
        public float ChanceAt(int tier)
        {
            var z = ZonesPerDeviceAt(tier);
            return z > 0f ? Math.Min(1f, 1f / z) : 0f;
        }

        /// <summary>The highest chance at any tier.</summary>
        public float MaxChance
        {
            get
            {
                var best = 0f;
                for (var t = 1; t <= zonesPerDevice.Length; t++) best = Math.Max(best, ChanceAt(t));
                return best;
            }
        }

        /// <summary>The zone's roll passes at its stored tier: it has its one slot (whether or not a device took it yet).</summary>
        public bool HasSlot(GridCoord zoneId, int tier) => Roll(zoneId) < ChanceAt(tier);

        /// <summary>What holds the zone's slot (None: nothing yet).</summary>
        public TriggerDeviceKind HolderOf(GridCoord zoneId) => holders.TryGetValue(zoneId, out var kind) ? kind : TriggerDeviceKind.None;

        /// <summary>Record that a device of <paramref name="kind"/> took the zone's slot. False (nothing recorded) when the slot is held already.</summary>
        public bool Take(GridCoord zoneId, TriggerDeviceKind kind)
        {
            if (kind == TriggerDeviceKind.None || holders.ContainsKey(zoneId)) return false;
            holders[zoneId] = kind;
            counts[(int)kind]++;
            return true;
        }

        /// <summary>Slots taken by devices of one kind.</summary>
        public int Count(TriggerDeviceKind kind) => (int)kind < counts.Length ? counts[(int)kind] : 0;

        /// <summary>Every slot taken, all kinds together: the map's device count.</summary>
        public int Devices => holders.Count;

        /// <summary>The ledger: every zone whose slot a device holds, and its kind.</summary>
        public IEnumerable<KeyValuePair<GridCoord, TriggerDeviceKind>> Ledger => holders;
    }

    /// <summary>C13's rules for one map (FrontRoomsLevelProfile.FoilRules), plain data. With the profile's foil off there are none and no FrontRoomsFoil exists.</summary>
    [Serializable]
    public sealed class FrontRoomsFoilRules
    {
        /// <summary>Tools only (the foil check, the interaction tests, -autopilotFoil): every Tall zone's budget roll passes.</summary>
        public bool force;
        /// <summary>RELAY 6: no device within this many cells (Manhattan, a lower bound on walking) of the start door.</summary>
        public int minStartCells = 15;
        /// <summary>RELAY 10: every Tall zone keeps at least min(this, its window count) plain windows.</summary>
        public int minPlainPerZone = 2;
        /// <summary>The trigger budget (FrontRoomsTierV2.zonesPerDevice per tier, tier 1 first).</summary>
        public float[] zonesPerDevice = { 3f, 2.5f, 2f, 1.75f, 1.5f };

        public FrontRoomsFoilRules Clone()
        {
            var c = (FrontRoomsFoilRules)MemberwiseClone();
            c.zonesPerDevice = (float[])zonesPerDevice?.Clone();
            return c;
        }
    }

    /// <summary>Why a Tall zone has no foiled window (None: it has one). Append only; tests and logs read the names.</summary>
    public enum FoilMiss : byte { None, NotTall, Arrival, NoSlot, Taken, NoWindow }

    /// <summary>One of a Tall zone's candidate windows (a chunk-border window of the zone), in the zone's rank order.</summary>
    public struct FoilWindow
    {
        /// <summary>The window's cell in the Tall zone and its cell on the other side.</summary>
        public GridCoord tall, other;
        /// <summary>The other side's zone: the window's border is (the Tall zone, this zone).</summary>
        public GridCoord neighbour;
        /// <summary>MapGrid.EdgeId of the edge.</summary>
        public long edge;
        /// <summary>The edge hash the zone ranks its windows by (lowest first).</summary>
        public uint rank;
        /// <summary>Kept plain by the rule (the lowest-ranked of its border, then the lowest-ranked to min(2, count) in the zone).</summary>
        public bool plain;
        /// <summary>RELAY 6's places a device never goes (the first zone, 15 cells of the start door, the station and its walk, X and N).</summary>
        public bool excluded;
    }

    /// <summary>A Tall zone's foil decision (FrontRoomsFoil.Plan): its candidate windows, which are plain, and the one foiled (if any).</summary>
    public sealed class FrontRoomsFoilZone
    {
        public GridCoord Zone;
        /// <summary>The zone's stored tier (FrontRoomsMapCache.ZoneTier) the budget read.</summary>
        public int Tier;
        public FoilMiss Miss;
        /// <summary>The zone's chunk-border windows, lowest rank first (interior windows are never candidates: always plain).</summary>
        public FoilWindow[] Windows = new FoilWindow[0];
        /// <summary>Index of the foiled window in Windows, or -1.</summary>
        public int Foiled = -1;
        public int Plain;

        public bool HasFoil => Foiled >= 0;
        public FoilWindow FoiledWindow => Windows[Foiled];
    }

    /// <summary>
    /// C13, foiled windows (RELAY 6, 10, 12; Relay v2 step V7a), as map data. Held by the cache (FrontRoomsMapCache.Foil) only
    /// while the level profile's foil is on; with it off nothing here exists and every map and game is today's, byte for byte.
    ///
    /// The rule, per Tall zone (a window border always has exactly one Tall side, so a window is its Tall zone's):
    /// - Candidates are the zone's chunk-border windows: an edge between a cell of the zone and a Low or Standard cell in the
    ///   next chunk that the seed's border rule makes a Window (FrontRoomsMapGenerator.BorderEdge, the same edge both chunks
    ///   build). They are pure in the seed, so no rebuild or revisit shift moves them. Windows inside a chunk change with its
    ///   revision: they are never foiled (always plain). A window on a cell the map walls (the start area, X) is not in play
    ///   and not a candidate.
    /// - Each candidate's rank is its edge hash. The lowest-ranked window of each border (Tall zone, neighbour zone) is kept
    ///   plain, then the lowest-ranked others until the zone keeps min(2, count) plain (RELAY 10: never a forced call).
    /// - The zone's trigger-budget slot (FrontRoomsTriggerBudget: its roll under 1 / zonesPerDevice at the zone's stored tier,
    ///   RELAY 12) foils ONE window: the highest-ranked one that is neither plain nor excluded (RELAY 6: the first zone, 15
    ///   cells of the start door, Blocked: the station, its strip, its walk, X and the EXIT door). The ledger then holds the
    ///   slot for Foil; a slot a Run! room holds is never foiled again (Taken).
    /// So foil is a pure function of the edge hash and the zone's stored tier (and the floor's fixed start and station): the
    /// plan is made once per zone, at the first question after Ready (the floor set up: Begin), and kept for the floor.
    /// </summary>
    public sealed class FrontRoomsFoil
    {
        /// <summary>The device kind's tag (the director's device id starts with it).</summary>
        public const string Tag = "foil";
        /// <summary>The window rank's salt ("FOI1").</summary>
        public const int EdgeSalt = 0x464f4931;

        const int N = MapGrid.ChunkCells;

        readonly FrontRoomsFoilRules rules;
        FrontRoomsMapCache cache;
        FrontRoomsMapGenerator gen;
        FrontRoomsTriggerBudget budget;
        readonly Dictionary<GridCoord, FrontRoomsFoilZone> plans = new Dictionary<GridCoord, FrontRoomsFoilZone>();
        readonly Dictionary<long, GridCoord> foiledEdges = new Dictionary<long, GridCoord>();
        readonly List<FrontRoomsFoilZone> decided = new List<FrontRoomsFoilZone>();

        public FrontRoomsFoil(FrontRoomsFoilRules rules) { this.rules = (rules ?? new FrontRoomsFoilRules()).Clone(); }

        public FrontRoomsFoilRules Rules => rules;
        public FrontRoomsTriggerBudget Budget => budget;

        // ---------- What the world sets (FrontRoomsMapWorld, before Begin; the validator's FoilCache) ----------

        /// <summary>The floor is set up (start area, station, EXIT): zones are decided from now on. Before it nothing is foiled and nothing is kept.</summary>
        public bool Ready { get; set; }
        /// <summary>The floor's start door cell (RELAY 6's 15 cells) and its zone, the first zone (no device there, P2).</summary>
        public bool HasStart { get; private set; }
        public GridCoord StartDoor { get; private set; }
        public GridCoord ArrivalZone { get; private set; }
        /// <summary>Cells the map walls for the floor (the start area, X): their windows are not in play. Null: none.</summary>
        public Func<GridCoord, bool> Walled { get; set; }
        /// <summary>RELAY 6's other no-device cells (the station, its strip, its walk, X, N and a ring round the start area and X). Null: none.</summary>
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
            budget = owner.UseBudget(rules.zonesPerDevice);
            if (HasStart) ArrivalZone = gen.ZoneOf(StartDoor).id;
        }

        // ---------- Counts (the validator and the logs) ----------

        public int ZonesDecided => decided.Count;
        public int FoiledCount => foiledEdges.Count;
        public IReadOnlyList<FrontRoomsFoilZone> Decided => decided;

        public int Missed(FoilMiss miss)
        {
            var n = 0;
            foreach (var z in decided) if (z.Miss == miss) n++;
            return n;
        }

        // ---------- Queries ----------

        /// <summary>The edge a-b (side by side, either order) is a foiled window. Decides its Tall zone when not done yet; false before Ready.</summary>
        public bool IsFoiled(GridCoord a, GridCoord b)
        {
            if (gen == null || Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y) != 1) return false;
            var ha = gen.HeightOf(a);
            var hb = gen.HeightOf(b);
            if (ha == hb || (ha != ZoneHeight.Tall && hb != ZoneHeight.Tall)) return false;
            var plan = Plan(gen.ZoneOf(ha == ZoneHeight.Tall ? a : b).id);
            return plan != null && plan.HasFoil && plan.FoiledWindow.edge == MapGrid.EdgeId(a, b);
        }

        /// <summary>A foiled edge's Tall zone, for an edge already decided (no decision is made here).</summary>
        public bool FoiledZone(long edge, out GridCoord zoneId) => foiledEdges.TryGetValue(edge, out zoneId);

        /// <summary>The device id a foiled window's trip carries (DeviceTripped, the director's device id): "foil:(x, y)-(x, y)", the lower cell first.</summary>
        public static string DeviceId(GridCoord a, GridCoord b)
        {
            if (b.x < a.x || b.y < a.y) { var t = a; a = b; b = t; }
            return Tag + ":" + a + "-" + b;
        }

        /// <summary>
        /// The Tall zone's decision: made once, at the first question after Ready, and kept for the floor. Null before Ready, for
        /// a zone that is not Tall, and with no cache.
        /// </summary>
        public FrontRoomsFoilZone Plan(GridCoord zoneId)
        {
            if (gen == null) return null;
            if (plans.TryGetValue(zoneId, out var memo)) return memo;
            if (!Ready || gen.Zone(zoneId).height != ZoneHeight.Tall) return null;
            var plan = new FrontRoomsFoilZone { Zone = zoneId, Tier = cache.ZoneTier(zoneId) };
            plan.Windows = Candidates(zoneId);
            MarkPlain(plan);
            Decide(plan);
            plans[zoneId] = plan;
            decided.Add(plan);
            return plan;
        }

        /// <summary>
        /// The zone's candidate windows (pure in the seed, the zone, Walled and Blocked and the start): every chunk-border edge
        /// between a cell of the zone and a lower cell that the border rule makes a Window, lowest rank first.
        /// </summary>
        public FoilWindow[] Candidates(GridCoord zoneId)
        {
            var list = new List<FoilWindow>();
            // The 5 x 5 chunks round the site hold every cell of the zone (PlaceKey's window).
            for (var y = (zoneId.y - 2) * N; y < (zoneId.y + 3) * N; y++)
            for (var x = (zoneId.x - 2) * N; x < (zoneId.x + 3) * N; x++)
            {
                var cell = new GridCoord(x, y);
                if (gen.ZoneOf(cell).id != zoneId) continue;
                for (var k = 0; k < 4; k++)
                {
                    var next = k == 0 ? new GridCoord(x + 1, y) : k == 1 ? new GridCoord(x, y + 1) : k == 2 ? new GridCoord(x - 1, y) : new GridCoord(x, y - 1);
                    if (MapGrid.ChunkOf(cell) == MapGrid.ChunkOf(next)) continue;
                    var other = gen.ZoneOf(next);
                    if (other.height == ZoneHeight.Tall) continue;
                    var lo = k < 2 ? cell : next;
                    var east = k == 0 || k == 2;
                    if (gen.BorderEdge(lo, east) != EdgeKind.Window) continue;
                    if (Walled != null && (Walled(cell) || Walled(next))) continue;
                    list.Add(new FoilWindow
                    {
                        tall = cell, other = next, neighbour = other.id, edge = MapGrid.EdgeId(cell, next),
                        rank = MapHash.Hash(gen.Seed, lo.x, lo.y, EdgeSalt, east ? 1 : 2),
                        excluded = Excluded(cell, next, other.id),
                    });
                }
            }
            list.Sort((p, q) => p.rank != q.rank ? p.rank.CompareTo(q.rank) : p.edge.CompareTo(q.edge));
            return list.ToArray();
        }

        // RELAY 6: the first zone (either side), 15 cells of the start door (either cell), Blocked (either cell).
        bool Excluded(GridCoord tall, GridCoord other, GridCoord otherZone)
        {
            if (HasStart && (otherZone == ArrivalZone || Manhattan(tall, StartDoor) < rules.minStartCells || Manhattan(other, StartDoor) < rules.minStartCells)) return true;
            return Blocked != null && (Blocked(tall) || Blocked(other));
        }

        static int Manhattan(GridCoord a, GridCoord b) => Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y);

        // RELAY 10: the lowest-ranked window of each border, then the lowest-ranked to min(2, count) in the zone.
        void MarkPlain(FrontRoomsFoilZone plan)
        {
            var w = plan.Windows;
            var borders = new HashSet<GridCoord>();
            var plain = 0;
            for (var i = 0; i < w.Length; i++)
                if (borders.Add(w[i].neighbour)) { w[i].plain = true; plain++; }
            var need = Math.Min(Math.Max(0, rules.minPlainPerZone), w.Length);
            for (var i = 0; i < w.Length && plain < need; i++)
                if (!w[i].plain) { w[i].plain = true; plain++; }
            plan.Plain = plain;
        }

        void Decide(FrontRoomsFoilZone plan)
        {
            if (HasStart && plan.Zone == ArrivalZone) { plan.Miss = FoilMiss.Arrival; return; }
            if (!rules.force && !budget.HasSlot(plan.Zone, plan.Tier)) { plan.Miss = FoilMiss.NoSlot; return; }
            if (budget.HolderOf(plan.Zone) != TriggerDeviceKind.None) { plan.Miss = FoilMiss.Taken; return; }
            for (var i = plan.Windows.Length - 1; i >= 0; i--)
            {
                if (plan.Windows[i].plain || plan.Windows[i].excluded) continue;
                plan.Foiled = i;
                budget.Take(plan.Zone, TriggerDeviceKind.Foil);
                foiledEdges[plan.Windows[i].edge] = plan.Zone;
                plan.Miss = FoilMiss.None;
                return;
            }
            plan.Miss = FoilMiss.NoWindow;
        }
    }

    /// <summary>
    /// Auto Relay entries (RELAY 6 placement rules; 20_level_design 4, L4; Relay v2 step V7a), as map data: every zone gets at
    /// least 2 Relay entries at least 10 cells apart (Manhattan) wherever its cells allow it, so the Relay always has a natural
    /// place to appear and to leave from. Read only with the profile's foil on (FrontRoomsMapWorld.RelayEntries, the
    /// validator); off, the Relay's entries are the module markers alone, as today.
    /// Per zone, over the chunks given (the built ones), cells the map walls (the start area, X) and Blocked cells left out:
    /// 1. every dead-end tip (a cell with exactly one walkable edge: anything but a wall and glass) is an entry;
    /// 2. while no two entries are 10 cells apart: the corridor ends (a straight corridor cell, two walkable edges in line,
    ///    whose neighbour along it opens three or more ways), then any other walkable cell, adding the one or two that make
    ///    the farthest pair (ties: scan order).
    /// Tag "vent" in Low and Standard zones, "doorway" in Tall ones (the sound layer reads it with Arrived). Pure in the chunks.
    /// </summary>
    public static class FrontRoomsAutoEntries
    {
        /// <summary>RELAY 6: the two entries a zone needs lie at least this many cells apart (Manhattan).</summary>
        public const int MinApart = 10;
        public const string VentTag = "vent", DoorwayTag = "doorway";
        /// <summary>How an entry was found: a dead end, a corridor end, or any other cell (the fallback).</summary>
        public const byte DeadEnd = 0, CorridorEnd = 1, AnyCell = 2;

        public struct Entry
        {
            public GridCoord cell, zone;
            public byte rank;
            public string tag;
        }

        const int N = MapGrid.ChunkCells;

        static bool Walkable(EdgeKind kind) => kind != EdgeKind.Wall && kind != EdgeKind.Window;

        /// <summary>A chunk cell's edge in direction k (0 +x, 1 +y, 2 -x, 3 -y), from the chunk alone (its border copies included).</summary>
        public static EdgeKind EdgeOf(MapChunk chunk, int i, int j, int k)
        {
            switch (k)
            {
                case 0: return chunk.east[MapGrid.LocalIndex(i, j)];
                case 1: return chunk.north[MapGrid.LocalIndex(i, j)];
                case 2: return i > 0 ? chunk.east[MapGrid.LocalIndex(i - 1, j)] : chunk.west[j];
                default: return j > 0 ? chunk.north[MapGrid.LocalIndex(i, j - 1)] : chunk.south[i];
            }
        }

        static GridCoord Step(GridCoord c, int k) =>
            k == 0 ? new GridCoord(c.x + 1, c.y) : k == 1 ? new GridCoord(c.x, c.y + 1) : k == 2 ? new GridCoord(c.x - 1, c.y) : new GridCoord(c.x, c.y - 1);

        static int Manhattan(GridCoord a, GridCoord b) => Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y);

        /// <summary>The entries of every zone with cells in <paramref name="chunks"/>, zone by zone (zone ids in order), into <paramref name="into"/> (cleared).</summary>
        public static void Collect(FrontRoomsMapGenerator gen, IList<MapChunk> chunks, Func<GridCoord, bool> walled, Func<GridCoord, bool> blocked, List<Entry> into)
        {
            into.Clear();
            if (gen == null || chunks == null || chunks.Count == 0) return;
            var ordered = new List<MapChunk>(chunks);
            ordered.Sort((p, q) => p.coord.x != q.coord.x ? p.coord.x.CompareTo(q.coord.x) : p.coord.y.CompareTo(q.coord.y));
            // Each usable cell's walkable directions (bits 0-3), in scan order per chunk; the zone of each.
            var dirs = new Dictionary<GridCoord, int>();
            var order = new List<GridCoord>();
            var zoneOf = new Dictionary<GridCoord, GridCoord>();
            foreach (var chunk in ordered)
            {
                var o = chunk.Origin;
                for (var j = 0; j < N; j++)
                for (var i = 0; i < N; i++)
                {
                    var cell = new GridCoord(o.x + i, o.y + j);
                    if (walled != null && walled(cell)) continue;
                    var bits = 0;
                    for (var k = 0; k < 4; k++)
                        if (Walkable(EdgeOf(chunk, i, j, k)) && (walled == null || !walled(Step(cell, k)))) bits |= 1 << k;
                    if (bits == 0 || dirs.ContainsKey(cell)) continue;
                    dirs[cell] = bits;
                    order.Add(cell);
                    zoneOf[cell] = chunk.zone[MapGrid.LocalIndex(i, j)];
                }
            }
            // Candidates per zone, by rank, in scan order.
            var zones = new List<GridCoord>();
            var ranks = new Dictionary<GridCoord, List<GridCoord>[]>();
            foreach (var cell in order)
            {
                if (blocked != null && blocked(cell)) continue;
                var z = zoneOf[cell];
                if (!ranks.TryGetValue(z, out var lists))
                {
                    ranks[z] = lists = new[] { new List<GridCoord>(), new List<GridCoord>(), new List<GridCoord>() };
                    zones.Add(z);
                }
                lists[RankOf(cell, dirs)].Add(cell);
            }
            zones.Sort((p, q) => p.x != q.x ? p.x.CompareTo(q.x) : p.y.CompareTo(q.y));
            var picked = new List<GridCoord>();
            var pickedRank = new List<byte>();
            var pool = new List<GridCoord>();
            var poolRank = new List<byte>();
            foreach (var z in zones)
            {
                var lists = ranks[z];
                picked.Clear();
                pickedRank.Clear();
                pool.Clear();
                poolRank.Clear();
                foreach (var c in lists[DeadEnd]) { picked.Add(c); pickedRank.Add(DeadEnd); }
                // The corridor ends first; then every cell left (corridor ends not taken included), so the last round reaches
                // the zone's own farthest pair of usable cells.
                for (var r = CorridorEnd; r <= AnyCell && Farthest(picked) < MinApart; r++)
                {
                    foreach (var c in lists[r]) { pool.Add(c); poolRank.Add(r); }
                    AddFarthest(picked, pickedRank, pool, poolRank);
                }
                var tag = gen.Zone(z).height == ZoneHeight.Tall ? DoorwayTag : VentTag;
                for (var k = 0; k < picked.Count; k++) into.Add(new Entry { cell = picked[k], zone = z, rank = pickedRank[k], tag = tag });
            }
        }

        // 0 a dead end, 1 a corridor end, 2 any other cell.
        static int RankOf(GridCoord cell, Dictionary<GridCoord, int> dirs)
        {
            var bits = dirs[cell];
            var count = 0;
            for (var k = 0; k < 4; k++) if ((bits & (1 << k)) != 0) count++;
            if (count == 1) return DeadEnd;
            if (count == 2 && (bits == 5 || bits == 10))
            {
                var k0 = bits == 5 ? 0 : 1;
                if (Ways(Step(cell, k0), dirs) >= 3 || Ways(Step(cell, k0 + 2), dirs) >= 3) return CorridorEnd;
            }
            return AnyCell;
        }

        static int Ways(GridCoord cell, Dictionary<GridCoord, int> dirs)
        {
            if (!dirs.TryGetValue(cell, out var bits)) return 0;
            var count = 0;
            for (var k = 0; k < 4; k++) if ((bits & (1 << k)) != 0) count++;
            return count;
        }

        /// <summary>The largest Manhattan distance between two of <paramref name="cells"/> (0 for fewer than two).</summary>
        public static int Farthest(IList<GridCoord> cells)
        {
            var best = 0;
            for (var a = 0; a < cells.Count; a++)
            for (var b = a + 1; b < cells.Count; b++)
                best = Math.Max(best, Manhattan(cells[a], cells[b]));
            return best;
        }

        // Add the one or two candidates (not taken yet) that make the farthest pair with the entries so far or between
        // themselves, when that pair is farther than the best pair already there. Ties: the pair found first (entries before
        // candidates, scan order). A zone with no entry yet and a single candidate takes it.
        static void AddFarthest(List<GridCoord> picked, List<byte> pickedRank, List<GridCoord> candidates, List<byte> ranks)
        {
            var best = Farthest(picked);
            int p = -1, q = -1;
            for (var a = 0; a < picked.Count; a++)
            for (var b = 0; b < candidates.Count; b++)
            {
                if (picked.Contains(candidates[b])) continue;
                var d = Manhattan(picked[a], candidates[b]);
                if (d > best) { best = d; p = -1; q = b; }
            }
            for (var a = 0; a < candidates.Count; a++)
            {
                if (picked.Contains(candidates[a])) continue;
                for (var b = a + 1; b < candidates.Count; b++)
                {
                    if (picked.Contains(candidates[b])) continue;
                    var d = Manhattan(candidates[a], candidates[b]);
                    if (d > best) { best = d; p = a; q = b; }
                }
            }
            if (picked.Count == 0 && q < 0)
                for (var a = 0; a < candidates.Count && q < 0; a++) q = a;
            if (p >= 0) { picked.Add(candidates[p]); pickedRank.Add(ranks[p]); }
            if (q >= 0) { picked.Add(candidates[q]); pickedRank.Add(ranks[q]); }
        }
    }
}
