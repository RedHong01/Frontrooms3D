using System;
using System.Collections.Generic;

namespace FrontRooms.Map
{
    [Serializable]
    public sealed class MapSeedReport
    {
        public int seed;
        public bool passed;
        public int cells;
        public int reachable;
        public int zones;
        public int lowZones;
        public int standardZones;
        public int tallZones;
        public int keys;
        public int open;
        public int arch;
        public int wall;
        public int door;
        public int window;
        public int pillars;
        public int modulesPlaced;
        public List<string> errors = new List<string>();
    }

    [Serializable]
    public sealed class MapVerificationReport
    {
        public string suite = "FrontRooms map data";
        public string scope = "Generated cells, edges, zones, pillars and keys for a square of chunks per seed. Geometry, rendering and human playability are separate.";
        public string timestampUtc;
        public int radiusChunks;
        public int seedCount;
        public int passed;
        public int failed;
        public List<MapSeedReport> seeds = new List<MapSeedReport>();
        // The EXIT planner over floors (RunExits), filled by the verification when it runs it.
        public MapExitReport exits;
    }

    /// <summary>One floor of one run in the EXIT check (RunExits).</summary>
    [Serializable]
    public sealed class MapExitFloorReport
    {
        public int floor;
        // The floor's map seed (the run seed on floor 1) and, after floor 1, how many seeds SeedFor probed.
        public int floorSeed;
        public int seedTries;
        // The tier the floor's chunks were generated at (the check's own spread: floor f at tier f).
        public int tier;
        public GridCoord startDoor;
        public ExitPlan exit;
        // N's zone height and the N-X edge as generated (the EXIT door replaces it): data for the map's EXIT door.
        public string frontHeight;
        public string generatedEdge;
        // G1 on floors 2+: SeedFor found a seed whose start door fits the placard at P1 west (StairDoorPlacardWest).
        public bool placardWest;
    }

    /// <summary>One run (a seed) through its floors in the EXIT check.</summary>
    [Serializable]
    public sealed class MapExitSeedReport
    {
        public int seed;
        public bool passed;
        // Floor 1's map root in cells (RootFor, the game's MapRootFor) and whether its door row is Standard Level 0.
        public GridCoord root;
        public bool rootStandard;
        // G1: the door cell's west edge is Open (the placard's P1 west).
        public bool rootPlacardWest;
        public List<MapExitFloorReport> floors = new List<MapExitFloorReport>();
        public List<string> errors = new List<string>();
    }

    /// <summary>Totals of one floor index over every seed.</summary>
    [Serializable]
    public sealed class MapExitFloorStats
    {
        public int floor;
        public int exits;
        public int widened;
        // EXITs only reached through glass (ExitPlan.throughGlass): floor 1 starts that are glass-bound pockets.
        public int throughGlass;
        public int deadEnds;
        public int minDistance = int.MaxValue;
        public int maxDistance;
        public float meanDistance;
        public float meanCandidates;
        public int maxSeedTries;
        public int seedFallbacks;
        // G1 on floors 2+: starts where no seed in MaxSeedTries fit the placard at P1 west (SeedFor fell back to a Standard
        // start; the placard mount skips the sheet there).
        public int placardWestMissing;
        public int lowFront, standardFront, tallFront;
        // How the N-X edge was generated, by EdgeKind (Open, Arch, Wall, Door, Window).
        public int[] generatedEdges = new int[5];
        // The front rules (C23 step 1), summed over the floor's EXITs, in the band each was found in: cells that passed
        // every other rule, and those turned down for N's height, for the sightline's glass, and of the latter only
        // for an edge a revisit shift could make glass; how many EXITs had at least one cell turned down by each.
        public int beforeFrontRules, rejectedHeight, rejectedSightline, rejectedShiftOnly;
        public int exitsWithHeightRejects, exitsWithSightlineRejects;
        // Widened EXITs whose narrower bands had cells lost to the front rules alone (they made the floor widen).
        public int widenedByFrontRules;
        // EXITs on a corridor (the end-on blade over the door), and with the flat sign.
        public int corridorSigns, flatSigns;
    }

    [Serializable]
    public sealed class MapExitReport
    {
        public string suite = "FrontRooms EXIT planner (C22)";
        public string scope = "Per seed, floors 1..n: floor 1 from the title's start area where the game puts it (RootFor), later floors from the previous EXIT cell (SeedFor, a 1-cell start area). Checks the planner's EXIT against the rules again with the validator's own walks. Data only: no geometry, no MapWorld.";
        public int seedCount;
        public int floors;
        public int passed;
        public int failed;
        public List<MapExitFloorStats> stats = new List<MapExitFloorStats>();
        public List<MapExitSeedReport> seeds = new List<MapExitSeedReport>();
    }

    /// <summary>One start of the start-group check (RunStartGroups): a run seed at one door row, through its floors.</summary>
    [Serializable]
    public sealed class MapStartGroupRow
    {
        public int seed;
        // t: the door's world cell is (column, 2 + 4t); chunk-local row 2 for an even t, 6 for an odd t.
        public int doorRow;
        public bool passed;
        // The root (cells), its ring, the zone roots tried (more than 1: leads-on or A1 took a later one), D and X in map cells.
        public GridCoord root;
        public int ring, zoneRoots;
        public GridCoord door, exit;
        public int distance;
        public string hash;
        public int repairs, roomDrops, keyMoves, borderOverrides, glassWalled;
        // Plan's warning (4.11: accepted on the nearest root) or empty; the plan's time in ms (Plan alone).
        public string warning;
        public float planMs;
        public List<string> errors = new List<string>();
    }

    /// <summary>The floor-1 start group (A26) over every door row of a world period, per seed (RunStartGroups).</summary>
    [Serializable]
    public sealed class MapStartGroupReport
    {
        public string suite = "FrontRooms floor-1 start group (A26)";
        public string scope = "Per seed and door row (both parities): FrontRoomsStartGroup.Plan from the door's world cell, its carve re-derived with the validator's own loops (CheckStartGroup), its EXIT checked by CheckExit (band H + k), the plan made again on a fresh cache, a revisit shift round X; floors 2+ from that X as always. Data only.";
        public string timestampUtc;
        public string template;
        public int seedCount, doorRows, floors, starts, passed, failed;
        // Starts on a later zone root, accepted with a warning, the widest ring and the most zone roots tried.
        public int retried, warned, maxRing, maxZoneRoots;
        // Starts at chunk-local door row 2 (even t) and 6 (odd t) that passed.
        public int evenPassed, oddPassed;
        public float planMsMean, planMsP95, planMsMax;
        public List<MapExitFloorStats> stats = new List<MapExitFloorStats>();
        public List<MapStartGroupRow> rows = new List<MapStartGroupRow>();
    }

    /// <summary>
    /// Checks the rules the rest of the game relies on. For each seed it builds
    /// the chunks in [-radius, radius) on both axes and verifies:
    /// neighbours agree on every shared edge; every cell is reachable when
    /// walls are the only blockers; doors and windows appear only where the
    /// ceiling height changes; every key zone with cells in the square has a
    /// key inside it; a rebuilt chunk is identical; and a shifted chunk keeps
    /// its borders. RunExits checks the EXIT planner (FrontRoomsExitPlanner,
    /// C22) over several floors per seed.
    /// </summary>
    public static class FrontRoomsMapValidator
    {
        public static MapVerificationReport Run(MapSettings settings, int firstSeed, int seedCount, int radius, IReadOnlyList<RoomModuleData> modules = null)
        {
            var report = new MapVerificationReport { radiusChunks = radius, seedCount = seedCount, timestampUtc = DateTime.UtcNow.ToString("o") };
            for (var i = 0; i < seedCount; i++)
            {
                var s = (settings ?? new MapSettings()).Clone();
                s.seed = unchecked(firstSeed + i * 7919);
                var result = ValidateSeed(s, radius, modules);
                report.seeds.Add(result);
                if (result.passed) report.passed++; else report.failed++;
            }
            return report;
        }

        public static MapSeedReport ValidateSeed(MapSettings settings, int radius, IReadOnlyList<RoomModuleData> modules = null)
        {
            var report = new MapSeedReport { seed = settings.seed };
            var cache = new FrontRoomsMapCache(settings, modules);
            var gen = cache.Generator;
            const int n = MapGrid.ChunkCells;
            int min = -radius, max = radius;
            for (var cy = min; cy < max; cy++)
            for (var cx = min; cx < max; cx++)
            {
                var built = cache.Get(new GridCoord(cx, cy));
                for (var r = 0; r < built.rooms.Length; r++) if (built.ModuleOf(r) != null) report.modulesPlaced++;
            }

            void Fail(string message) { if (report.errors.Count < 12) report.errors.Add(message); }

            // 1. Neighbours agree on shared border edges and border pillars.
            for (var cy = min; cy < max; cy++)
            for (var cx = min; cx < max; cx++)
            {
                var c = cache.Get(new GridCoord(cx, cy));
                if (cx + 1 < max)
                {
                    var e = cache.Get(new GridCoord(cx + 1, cy));
                    for (var k = 0; k < n; k++)
                        if (c.east[MapGrid.LocalIndex(n - 1, k)] != e.west[k]) Fail("border east of chunk " + c.coord + " row " + k);
                    for (var k = 0; k <= n; k++)
                        if (c.pillar[n + k * (n + 1)] != e.pillar[k * (n + 1)]) Fail("pillar on east border of chunk " + c.coord);
                }
                if (cy + 1 < max)
                {
                    var u = cache.Get(new GridCoord(cx, cy + 1));
                    for (var k = 0; k < n; k++)
                        if (c.north[MapGrid.LocalIndex(k, n - 1)] != u.south[k]) Fail("border north of chunk " + c.coord + " column " + k);
                    for (var k = 0; k <= n; k++)
                        if (c.pillar[k + n * (n + 1)] != u.pillar[k]) Fail("pillar on north border of chunk " + c.coord);
                }
            }

            // 2. Reachability and 3. zone rules, over every cell and edge in the square.
            int x0 = min * n, x1 = max * n, y0 = min * n, y1 = max * n;
            int w = x1 - x0, h = y1 - y0;
            report.cells = w * h;
            var seen = new bool[w * h];
            var queue = new Queue<GridCoord>();
            var start = new GridCoord(x0, y0);
            seen[0] = true;
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                report.reachable++;
                Visit(cell, new GridCoord(cell.x + 1, cell.y));
                Visit(cell, new GridCoord(cell.x - 1, cell.y));
                Visit(cell, new GridCoord(cell.x, cell.y + 1));
                Visit(cell, new GridCoord(cell.x, cell.y - 1));
            }
            void Visit(GridCoord from, GridCoord to)
            {
                if (to.x < x0 || to.x >= x1 || to.y < y0 || to.y >= y1) return;
                var index = (to.x - x0) + (to.y - y0) * w;
                if (seen[index] || !MapGrid.Passable(cache.Edge(from, to))) return;
                seen[index] = true;
                queue.Enqueue(to);
            }
            if (report.reachable != report.cells) Fail("only " + report.reachable + " of " + report.cells + " cells reachable");

            var zonesWithCells = new HashSet<GridCoord>();
            for (var y = y0; y < y1; y++)
            for (var x = x0; x < x1; x++)
            {
                var cell = new GridCoord(x, y);
                zonesWithCells.Add(gen.ZoneOf(cell).id);
                if (x + 1 < x1) CheckEdge(cell, new GridCoord(x + 1, y));
                if (y + 1 < y1) CheckEdge(cell, new GridCoord(x, y + 1));
                if (x > x0 && y > y0)
                {
                    var c = cache.Get(MapGrid.ChunkOf(cell));
                    var o = c.Origin;
                    if (c.pillar[(x - o.x) + (y - o.y) * (n + 1)])
                    {
                        report.pillars++;
                        // 7. Columns stand strictly inside an intact room: on the 6 m grid and
                        // never in a Low zone, unless a module placed its own.
                        var owner = -1;
                        for (var r = 0; r < c.rooms.Length && owner < 0; r++)
                        {
                            var room = c.rooms[r];
                            if (c.RoomIntact(r) && x - o.x > room.x && x - o.x < room.x + room.w && y - o.y > room.y && y - o.y < room.y + room.h) owner = r;
                        }
                        var custom = owner >= 0 && c.ModuleOf(owner) != null && c.ModuleOf(owner).columns == ModuleColumns.Custom;
                        if (owner < 0 || (!custom && !gen.Uniform(c, c.rooms[owner]))) Fail("column outside an intact room at " + cell);
                        if (!custom && !FrontRoomsMapGenerator.OnColumnGrid(cell)) Fail("column off the 6 m grid at " + cell);
                        if (!custom && gen.HeightOf(cell) == ZoneHeight.Low) Fail("column in a Low zone at " + cell);
                    }
                }
            }
            void CheckEdge(GridCoord a, GridCoord b)
            {
                var kind = cache.Edge(a, b);
                switch (kind)
                {
                    case EdgeKind.Open: report.open++; break;
                    case EdgeKind.Arch: report.arch++; break;
                    case EdgeKind.Wall: report.wall++; break;
                    case EdgeKind.Door: report.door++; break;
                    case EdgeKind.Window: report.window++; break;
                }
                var ha = gen.HeightOf(a);
                var hb = gen.HeightOf(b);
                if (ha == hb && (kind == EdgeKind.Door || kind == EdgeKind.Window)) Fail(kind + " between two " + ha + " cells at " + a + "-" + b);
                if (ha != hb && (kind == EdgeKind.Open || kind == EdgeKind.Arch)) Fail(kind + " across a height change at " + a + "-" + b);
                var tall = ha == ZoneHeight.Tall || hb == ZoneHeight.Tall;
                if (kind == EdgeKind.Door && tall) Fail("door into a tall zone at " + a + "-" + b);
                if (kind == EdgeKind.Window && !tall) Fail("window without a tall zone at " + a + "-" + b);
            }

            // 4. Keys: every low or standard zone whose site is in the square
            // carries a key that sits inside the zone itself.
            foreach (var id in zonesWithCells)
            {
                if (id.x < min || id.x >= max || id.y < min || id.y >= max) continue;
                var zone = gen.Zone(id);
                report.zones++;
                if (zone.height == ZoneHeight.Low) report.lowZones++;
                else if (zone.height == ZoneHeight.Standard) report.standardZones++;
                else report.tallZones++;
                var home = cache.Get(id);
                if (zone.height == ZoneHeight.Tall)
                {
                    if (home.hasKey) Fail("tall zone " + id + " has a key");
                    continue;
                }
                if (!home.hasKey) { Fail("zone " + id + " has no key"); continue; }
                report.keys++;
                if (gen.ZoneOf(home.keyCell).id != id) Fail("key of zone " + id + " lies outside it at " + home.keyCell);
            }

            // 5. Determinism: a fresh generator building in a different order
            // produces identical chunks. 6. A shifted chunk keeps its borders.
            var fresh = new FrontRoomsMapGenerator(settings, modules);
            for (var cy = max - 1; cy >= min; cy -= 3)
            for (var cx = max - 1; cx >= min; cx -= 3)
            {
                var coord = new GridCoord(cx, cy);
                var a = cache.Get(coord);
                var b = fresh.Generate(coord);
                if (!Same(a.east, b.east) || !Same(a.north, b.north) || !Same(a.west, b.west) || !Same(a.south, b.south) || !Same(a.pillar, b.pillar) || !Same(a.pillarStyle, b.pillarStyle) || a.keyCell != b.keyCell || a.keySpot != b.keySpot || a.keyX != b.keyX || a.keyZ != b.keyZ || a.keyY != b.keyY || a.tier != b.tier
                    || !Same(a.rooms, b.rooms) || !Same(a.lamp, b.lamp) || !SameModules(a, b))
                    Fail("chunk " + coord + " differs when rebuilt");
                var shifted = fresh.Generate(coord, 1);
                for (var k = 0; k < n; k++)
                {
                    if (shifted.east[MapGrid.LocalIndex(n - 1, k)] != a.east[MapGrid.LocalIndex(n - 1, k)] || shifted.west[k] != a.west[k]
                        || shifted.north[MapGrid.LocalIndex(k, n - 1)] != a.north[MapGrid.LocalIndex(k, n - 1)] || shifted.south[k] != a.south[k])
                        Fail("shifted chunk " + coord + " changed a border");
                }
            }

            report.passed = report.errors.Count == 0;
            return report;
        }

        // ---------- 8. The EXIT planner (C22) ----------

        /// <summary>
        /// The EXIT planner over <paramref name="floors"/> floors for each seed (the same seeds as Run). Floor 1
        /// starts where the game starts: the title's door world cell (the root at the origin) with its strip of
        /// <paramref name="titleRows"/> x (2 * <paramref name="titleHalfCells"/> + 1) cells behind it, placed by
        /// RootFor (the game's MapRootFor). Each later floor takes SeedFor's seed for the previous EXIT cell X and
        /// starts with X as a 1-cell start area and X + (0, 1) as its door. Floor f generates at tier f: a run
        /// that reaches a floor has often risen a tier, and the rules must hold at any tier.
        /// </summary>
        public static MapExitReport RunExits(MapSettings settings, int firstSeed, int seedCount, int floors, GridCoord titleDoorWorldCell, int titleHalfCells, int titleRows, IReadOnlyList<RoomModuleData> modules = null)
        {
            var report = new MapExitReport { seedCount = seedCount, floors = floors };
            for (var f = 1; f <= floors; f++) report.stats.Add(new MapExitFloorStats { floor = f });
            for (var i = 0; i < seedCount; i++)
            {
                var s = (settings ?? new MapSettings()).Clone();
                s.seed = unchecked(firstSeed + i * 7919);
                var result = ValidateExits(s, floors, titleDoorWorldCell, titleHalfCells, titleRows, modules);
                report.seeds.Add(result);
                if (result.passed) report.passed++; else report.failed++;
                AddFloorStats(report.stats, result);
            }
            FinishFloorStats(report.stats);
            return report;
        }

        /// <summary>
        /// The floor-1 start group (A26, notes/floor1_start_group_plan.md 6 step 2) for the same seeds as Run, at every
        /// door row of a world period: the door's world cell (<paramref name="doorColumn"/>, 2 + 4t) for t = 0 ..
        /// <paramref name="doorRows"/> - 1, so both chunk-local parities (rows 2 and 6). Each start is a run through
        /// <paramref name="floors"/> floors (ValidateExits with <paramref name="station"/>): floor 1 from the plan, the
        /// later floors from its X. The strip is the planner's (R* rows), whatever the title made.
        /// </summary>
        public static MapStartGroupReport RunStartGroups(MapSettings settings, int firstSeed, int seedCount, int floors, int doorColumn, int doorRows, StartGroupTemplate station,
            IReadOnlyList<RoomModuleData> modules = null)
        {
            var report = new MapStartGroupReport { timestampUtc = DateTime.UtcNow.ToString("o"), template = station != null ? station.Name : "none", seedCount = seedCount, doorRows = doorRows, floors = floors };
            for (var f = 1; f <= floors; f++) report.stats.Add(new MapExitFloorStats { floor = f });
            var times = new List<float>();
            for (var i = 0; i < seedCount; i++)
            {
                var s = (settings ?? new MapSettings()).Clone();
                s.seed = unchecked(firstSeed + i * 7919);
                for (var t = 0; t < doorRows; t++)
                {
                    var result = ValidateExits(s, floors, new GridCoord(doorColumn, 2 + 4 * t), StartHalfCells, FrontRoomsStartGroup.CanonicalStripRows, modules, station, out var group);
                    var row = new MapStartGroupRow { seed = s.seed, doorRow = t, passed = result.passed && station != null };
                    if (station == null) row.errors.Add("no station template");
                    if (group != null)
                    {
                        row.root = group.root;
                        row.ring = group.ring;
                        row.zoneRoots = group.zoneRoots;
                        row.warning = group.warning ?? "";
                        row.planMs = group.planMs;
                        row.door = group.layout.door;
                        row.exit = group.exit.exitCell;
                        row.distance = group.exit.distance;
                        if (group.mapCarve != null)
                        {
                            var c = group.mapCarve;
                            row.hash = c.Hash;
                            row.repairs = c.RepairA + c.RepairB + c.RepairC;
                            row.roomDrops = c.RoomDrops;
                            row.keyMoves = c.KeyMoves;
                            row.borderOverrides = c.BorderOverrides;
                            row.glassWalled = c.GlassWalled;
                        }
                        times.Add(group.planMs);
                        if (group.zoneRoots > 1) report.retried++;
                        if (!string.IsNullOrEmpty(group.warning)) report.warned++;
                        report.maxRing = Math.Max(report.maxRing, group.ring);
                        report.maxZoneRoots = Math.Max(report.maxZoneRoots, group.zoneRoots);
                    }
                    if (!result.passed) row.errors.AddRange(result.errors);
                    report.rows.Add(row);
                    report.starts++;
                    if (row.passed)
                    {
                        report.passed++;
                        if (t % 2 == 0) report.evenPassed++; else report.oddPassed++;
                    }
                    else report.failed++;
                    AddFloorStats(report.stats, result);
                }
            }
            FinishFloorStats(report.stats);
            if (times.Count > 0)
            {
                times.Sort();
                var sum = 0f;
                foreach (var ms in times) sum += ms;
                report.planMsMean = sum / times.Count;
                report.planMsP95 = times[Math.Max(0, (int)Math.Ceiling(times.Count * .95) - 1)];
                report.planMsMax = times[times.Count - 1];
            }
            return report;
        }

        // The title strip's half width (FrontRooms3DGame StartAreaHalfCells, G:230; FrontRoomsStartGroup.StripHalfCells).
        const int StartHalfCells = FrontRoomsStartGroup.StripHalfCells;

        // One run's floors into the per-floor totals (RunExits, RunStartGroups).
        static void AddFloorStats(List<MapExitFloorStats> stats, MapExitSeedReport result)
        {
            foreach (var floor in result.floors)
            {
                var st = stats[floor.floor - 1];
                if (floor.seedTries > st.maxSeedTries) st.maxSeedTries = floor.seedTries;
                if (!floor.exit.found) continue;
                st.exits++;
                if (floor.exit.widened) st.widened++;
                if (floor.exit.throughGlass) st.throughGlass++;
                if (floor.exit.deadEnd) st.deadEnds++;
                st.minDistance = Math.Min(st.minDistance, floor.exit.distance);
                st.maxDistance = Math.Max(st.maxDistance, floor.exit.distance);
                st.meanDistance += floor.exit.distance;
                st.meanCandidates += floor.exit.candidates;
                if (floor.frontHeight == nameof(ZoneHeight.Low)) st.lowFront++;
                else if (floor.frontHeight == nameof(ZoneHeight.Tall)) st.tallFront++;
                else st.standardFront++;
                if (Enum.TryParse(floor.generatedEdge, out EdgeKind kind)) st.generatedEdges[(int)kind]++;
                st.beforeFrontRules += floor.exit.beforeFrontRules;
                st.rejectedHeight += floor.exit.rejectedHeight;
                st.rejectedSightline += floor.exit.rejectedSightline;
                st.rejectedShiftOnly += floor.exit.rejectedShiftOnly;
                if (floor.exit.rejectedHeight > 0) st.exitsWithHeightRejects++;
                if (floor.exit.rejectedSightline > 0) st.exitsWithSightlineRejects++;
                if (floor.exit.widened && floor.exit.lostToFrontRules > 0) st.widenedByFrontRules++;
                if (floor.exit.corridor) st.corridorSigns++; else st.flatSigns++;
            }
            foreach (var floor in result.floors)
            {
                if (floor.floor > 1 && floor.seedTries >= FrontRoomsExitPlanner.MaxSeedTries) stats[floor.floor - 1].seedFallbacks++;
                if (floor.floor > 1 && !floor.placardWest) stats[floor.floor - 1].placardWestMissing++;
            }
        }

        static void FinishFloorStats(List<MapExitFloorStats> stats)
        {
            foreach (var st in stats)
            {
                if (st.exits > 0) { st.meanDistance /= st.exits; st.meanCandidates /= st.exits; }
                else st.minDistance = 0;
            }
        }

        /// <summary>One run through its floors: plan each EXIT, then check it with the validator's own walks.</summary>
        public static MapExitSeedReport ValidateExits(MapSettings settings, int floors, GridCoord titleDoorWorldCell, int titleHalfCells, int titleRows, IReadOnlyList<RoomModuleData> modules = null) =>
            ValidateExits(settings, floors, titleDoorWorldCell, titleHalfCells, titleRows, modules, null, out _);

        /// <summary>
        /// One run through its floors, floor 1 from the start group (A26) when <paramref name="station"/> is given: the
        /// planner's FrontRoomsStartGroup.Plan instead of RootFor and ChooseExit, so the root comes from FindRoot and the
        /// EXIT is the layout's X (FromStartGroup), checked by CheckExit with the band H + k, by CheckStartGroup (every carve
        /// invariant again, with the validator's own loops) and planned again on a fresh cache; the start area is the plan's
        /// strip (R* rows). Floors 2+ as always, from that X. <paramref name="stationPlan"/> is floor 1's plan (null without).
        /// </summary>
        public static MapExitSeedReport ValidateExits(MapSettings settings, int floors, GridCoord titleDoorWorldCell, int titleHalfCells, int titleRows, IReadOnlyList<RoomModuleData> modules,
            StartGroupTemplate station, out StartGroupPlan stationPlan)
        {
            var report = new MapExitSeedReport { seed = settings.seed };
            void Fail(string message) { if (report.errors.Count < 12) report.errors.Add(message); }
            stationPlan = null;

            var previous = new ExitPlan();
            for (var f = 1; f <= floors; f++)
            {
                var floor = new MapExitFloorReport { floor = f, tier = f };
                report.floors.Add(floor);
                FrontRoomsMapCache cache;
                CellRect area;
                GridCoord door;
                StartGroupPlan group = null;
                if (f == 1 && station != null)
                {
                    cache = FrontRoomsExitPlanner.CacheFor(settings, modules, settings.seed, floor.tier);
                    group = FrontRoomsStartGroup.Plan(cache, station, titleDoorWorldCell);
                    stationPlan = group;
                    report.root = group.root;
                    if (!group.Ready) { Fail("floor 1: no start group (" + station.Name + ") from the door " + titleDoorWorldCell + ": " + group.error); break; }
                    door = group.layout.door;
                    area = group.layout.strip;
                    report.rootStandard = FrontRoomsExitPlanner.StartRowStandard(cache, door, titleHalfCells);
                    report.rootPlacardWest = FrontRoomsExitPlanner.StartDoorWestOpen(cache, door);
                    floor.floorSeed = settings.seed;
                }
                else if (f == 1)
                {
                    cache = FrontRoomsExitPlanner.CacheFor(settings, modules, settings.seed, floor.tier);
                    var root = FrontRoomsExitPlanner.RootFor(cache, titleDoorWorldCell, titleHalfCells, titleRows);
                    report.root = root.root;
                    report.rootStandard = root.standard;
                    report.rootPlacardWest = root.placardWest;
                    if (!root.leadsOn) { Fail("floor 1: no root in " + FrontRoomsExitPlanner.RootRings + " rings leads into the maze"); break; }
                    if (!root.placardWest) Fail("floor 1: G1: no Standard root in " + FrontRoomsExitPlanner.RootRings + " rings keeps the start door " + root.doorCell + "'s west edge Open (the placard's P1 west)");
                    door = root.doorCell;
                    area = new CellRect(door.x - titleHalfCells, door.y - titleRows, titleHalfCells * 2 + 1, titleRows);
                    floor.floorSeed = settings.seed;
                }
                else
                {
                    var seedPlan = FrontRoomsExitPlanner.SeedFor(settings.seed, f, previous.exitCell, settings, modules, floor.tier, out cache);
                    floor.floorSeed = seedPlan.seed;
                    floor.seedTries = seedPlan.tries;
                    door = previous.NextDoorCell;
                    area = previous.NextStartArea;
                    if (!seedPlan.standard) Fail("floor " + f + ": SeedFor found no Standard Level 0 start door at " + door + " in " + seedPlan.tries + " seeds");
                    floor.placardWest = seedPlan.placardWest;
                    // SeedFor's tests again, by hand: the door cell, the way on, and where it leads (glass blocks it).
                    var zone = cache.ZoneOf(door);
                    if (zone.height != ZoneHeight.Standard || zone.theme != ZoneTheme.Level0) Fail("floor " + f + ": start door cell " + door + " is " + zone.height + " " + zone.theme);
                    var ahead = new GridCoord(door.x, door.y + 1);
                    if (!FrontRoomsExitPlanner.Walkable(cache.Edge(door, ahead))) Fail("floor " + f + ": the way on from " + door + " is " + cache.Edge(door, ahead));
                    // LeadsIntoMaze counts the door cell with the cells walked from the one ahead.
                    else if (Walk(cache, area, ahead, door, true, int.MaxValue, LeadsOnCap - 1).Count < LeadsOnCap - 1) Fail("floor " + f + ": the way on from the start door " + door + " leads into a pocket");
                }
                floor.startDoor = door;
                int lo, hi;
                ExitPlan plan;
                if (group != null)
                {
                    // The start group's EXIT: no band to search, the distance is H + k by construction.
                    lo = hi = station.depth + station.corridor;
                    plan = group.exit;
                }
                else
                {
                    FrontRoomsExitPlanner.BandFor(f, out lo, out hi);
                    plan = FrontRoomsExitPlanner.ChooseExit(cache, area, door, floor.floorSeed, lo, hi);
                }
                floor.exit = plan;
                if (!plan.found) { Fail("floor " + f + ": no EXIT from the door at " + door + " in any band (6-40)"); break; }
                floor.frontHeight = cache.ZoneOf(plan.frontCell).height.ToString();
                floor.generatedEdge = cache.Edge(plan.frontCell, plan.exitCell).ToString();
                if (plan.throughGlass && f > 1) Fail("floor " + f + ": the EXIT needs glass, but SeedFor's start leads on without it");
                CheckExit(cache, area, door, plan, lo, hi, "floor " + f + ": ", Fail);

                if (group != null)
                {
                    CheckStartGroup(cache, station, group, titleDoorWorldCell, titleHalfCells, "floor 1: ", Fail);
                    // The same plan from a fresh cache that generated other chunks first: the carve is a function of the
                    // seed, the tier, the door and the template, whatever the cache built before.
                    var freshGroup = FrontRoomsExitPlanner.CacheFor(settings, modules, settings.seed, floor.tier);
                    var farGroup = MapGrid.ChunkOf(plan.exitCell);
                    freshGroup.Get(new GridCoord(farGroup.x + 2, farGroup.y - 3));
                    freshGroup.Get(new GridCoord(farGroup.x - 1, farGroup.y + 2));
                    var againGroup = FrontRoomsStartGroup.Plan(freshGroup, station, titleDoorWorldCell);
                    if (!againGroup.Ready || againGroup.root != group.root || againGroup.mapCarve.Hash != group.mapCarve.Hash || againGroup.exit.exitCell != plan.exitCell
                        || againGroup.exit.distance != plan.distance || againGroup.zoneRoots != group.zoneRoots)
                        Fail("floor 1: the start group differs on a fresh cache (root " + againGroup.root + " vs " + group.root + ", hash "
                            + (againGroup.mapCarve != null ? againGroup.mapCarve.Hash : "none") + " vs " + group.mapCarve.Hash + ")");
                }
                else
                {
                    // The same EXIT from a fresh cache that generated other chunks first: the choice is a function of
                    // the seed, the tier and the start, whichever map (or tool) asks and in whatever order.
                    var fresh = FrontRoomsExitPlanner.CacheFor(settings, modules, floor.floorSeed, floor.tier);
                    var far = MapGrid.ChunkOf(plan.exitCell);
                    fresh.Get(new GridCoord(far.x + 2, far.y - 3));
                    fresh.Get(new GridCoord(far.x - 1, far.y + 2));
                    var again = FrontRoomsExitPlanner.ChooseExit(fresh, area, door, floor.floorSeed, lo, hi);
                    if (again.exitCell != plan.exitCell || again.distance != plan.distance || again.candidates != plan.candidates || again.throughGlass != plan.throughGlass
                        || again.corridor != plan.corridor || again.beforeFrontRules != plan.beforeFrontRules || again.nearestGap != plan.nearestGap || again.nearest != plan.nearest)
                        Fail("floor " + f + ": the EXIT differs on a fresh cache (" + again.exitCell + " vs " + plan.exitCell + ")");
                }

                // Last, as it changes the cache: a revisit shift of every chunk round X's (X's own never shifts, D5; nor
                // does a chunk the start group pins).
                CheckAfterShift(cache, area, plan, "floor " + f + ": ", Fail);
                previous = plan;
            }
            report.passed = report.errors.Count == 0;
            return report;
        }

        // A start door leads into the maze when the walk from it reaches this many cells (the planner's LeadsOnCells).
        const int LeadsOnCap = FrontRoomsExitPlanner.LeadsOnCells;

        /// <summary>The planned EXIT against every rule, with the validator's own walks (not the planner's).</summary>
        static void CheckExit(FrontRoomsMapCache cache, CellRect area, GridCoord door, ExitPlan plan, int lo, int hi, string at, Action<string> fail)
        {
            const int n = MapGrid.ChunkCells;
            var x = plan.exitCell;
            var front = plan.frontCell;
            if (front.x != x.x || front.y != x.y - 1) fail(at + "N " + front + " is not X " + x + " + (0, -1)");
            var coord = MapGrid.ChunkOf(x);
            var origin = MapGrid.ChunkOrigin(coord);
            int i = x.x - origin.x, j = x.y - origin.y;
            if (i < 1 || i > n - 2 || j < 1 || j > n - 2) fail(at + "X " + x + " is chunk-local (" + i + ", " + j + "), off 1..6");
            if (RingDistance(area, x) <= 1) fail(at + "X " + x + " is in the start area or its ring");
            var chunk = cache.Get(coord);
            for (var r = 0; r < chunk.rooms.Length; r++)
                if (chunk.rooms[r].Contains(i, j)) fail(at + "X " + x + " lies in room " + r + " of chunk " + coord + (chunk.ModuleOf(r) != null ? " (a module)" : ""));
            if (chunk.pillar[i + j * (n + 1)] || chunk.pillar[i + 1 + j * (n + 1)] || chunk.pillar[i + (j + 1) * (n + 1)] || chunk.pillar[i + 1 + (j + 1) * (n + 1)])
                fail(at + "a column stands on a corner of X " + x);
            CheckKeys(cache, x, at, fail);

            // The walk: from the start door, never through the start area, X, a wall or glass. Glass counts only for
            // the planner's last resort, and only where the start leaves its pocket through glass alone (floor 1,
            // whose title root search lets glass lead on): the walk on foot from the door must stay a pocket.
            if (plan.throughGlass)
            {
                var pocket = Walk(cache, area, door, x, false, int.MaxValue, LeadsOnCap, false).Count;
                if (pocket >= LeadsOnCap) fail(at + "the EXIT needs glass, but on foot the door " + door + " leads on (" + pocket + "+ cells)");
            }
            var walk = Walk(cache, area, door, x, true, WidestBand, int.MaxValue, plan.throughGlass);
            if (!walk.TryGetValue(front, out var steps)) fail(at + "N " + front + " cannot be reached " + (plan.throughGlass ? "(glass passable)" : "on foot") + " from the door " + door + " without X");
            else if (steps + 1 != plan.distance) fail(at + "distance " + plan.distance + " but the walk into X is " + (steps + 1));
            if (plan.distance < plan.bandMin || plan.distance > plan.bandMax) fail(at + "distance " + plan.distance + " outside its band " + plan.bandMin + "-" + plan.bandMax);
            var asked = plan.bandMin == lo && plan.bandMax == hi;
            var wide = plan.bandMin == Math.Min(lo, FrontRoomsExitPlanner.WideBandMin) && plan.bandMax == Math.Max(hi, FrontRoomsExitPlanner.WideBandMax);
            var widest = plan.bandMin == Math.Min(lo, FrontRoomsExitPlanner.WidestBandMin) && plan.bandMax == Math.Max(hi, FrontRoomsExitPlanner.WidestBandMax);
            if (plan.widened ? !(wide || widest) : !asked) fail(at + "band " + plan.bandMin + "-" + plan.bandMax + (plan.widened ? " is not a widening of " : " is not the band asked, ") + lo + "-" + hi);

            // No disconnect: inside X's chunk (outside the start area, every edge but a wall passable), N reaches
            // X, and without X it still reaches every cell it reached with X there.
            var withX = ChunkReach(cache, area, coord, front, null);
            var withoutX = ChunkReach(cache, area, coord, front, x);
            if (!withX.Contains(x)) fail(at + "N " + front + " does not reach X " + x + " inside its chunk");
            else
                foreach (var cell in withX)
                    if (cell != x && !withoutX.Contains(cell)) { fail(at + "removing X " + x + " cuts " + cell + " off from N inside the chunk"); break; }

            // The preference: a dead end (generated W, E and N edges all walls) whenever the band had one.
            var deadEnd = cache.Edge(x, new GridCoord(x.x - 1, x.y)) == EdgeKind.Wall && cache.Edge(x, new GridCoord(x.x + 1, x.y)) == EdgeKind.Wall
                && cache.Edge(x, new GridCoord(x.x, x.y + 1)) == EdgeKind.Wall;
            if (deadEnd != plan.deadEnd) fail(at + "X " + x + " is " + (deadEnd ? "" : "not ") + "a dead end, the plan says otherwise");
            // C23 round 2: the preference is asked among the nearest candidates (all of the band's, unless widened).
            if (plan.nearestDeadEnds > 0 && !deadEnd) fail(at + "the nearest candidates had " + plan.nearestDeadEnds + " dead ends but X " + x + " is not one");
            CheckNearest(cache, area, door, plan, lo, hi, at, fail);

            // The front rules (C23 step 1), with the validator's own loops: N is Standard height (the exit side of the
            // door needs a Standard ceiling, 10_directions 1.2); no glass on the EXIT door's sightline, nor an edge a
            // revisit shift could glaze; the corridor flag (which sign the map mounts) as the edges say.
            var frontHeight = cache.ZoneOf(front).height;
            if (frontHeight != ZoneHeight.Standard) fail(at + "N " + front + " is " + frontHeight + ", not Standard: the EXIT sign does not fit over the door");
            var glass = SightlineProblem(cache, x, false);
            if (glass != null) fail(at + "a window on the EXIT door's sightline: " + glass);
            var mayGlaze = SightlineProblem(cache, x, true);
            if (mayGlaze != null) fail(at + "an edge of the EXIT door's sightline a revisit shift could glaze: " + mayGlaze);
            var west = new GridCoord(front.x - 1, front.y);
            var east = new GridCoord(front.x + 1, front.y);
            var corridor = WalksThrough(cache, area, front, west) && WalksThrough(cache, area, front, east);
            if (corridor != plan.corridor) fail(at + "N " + front + (corridor ? " has" : " has no") + " corridor running past the door (" + cache.Edge(front, west) + " / "
                + cache.Edge(front, east) + "), the plan says otherwise");
            if (plan.candidates > plan.beforeFrontRules || plan.beforeFrontRules - plan.candidates > plan.rejectedHeight + plan.rejectedSightline
                || plan.rejectedShiftOnly > plan.rejectedSightline)
                fail(at + "the front-rule counts do not add up: " + plan.beforeFrontRules + " before, " + plan.candidates + " after, " + plan.rejectedHeight + " height, "
                    + plan.rejectedSightline + " sightline (" + plan.rejectedShiftOnly + " shift only)");
        }

        /// <summary>
        /// The floor-1 start group (A26) against every carve invariant, re-derived with the validator's own loops from the
        /// door, the root and the template, never from the carve's own lists (notes/floor1_start_group_plan.md 4.1, 4.2,
        /// 4.6, 4.8): D is the door's world cell less the root, chunk-local x 5 and y 2 or 6; the station x D-2..D+1, y
        /// D..D+H-1, C1..N straight north of E1, X = N + (0, 1) in c' = c + (0, 1); c and c' pinned; the door row, the
        /// station and C1..N Standard Level 0, E2o Standard; every edge the template fixes (the inside as authored, the
        /// walls, D's south edge, E1 and E2 with their spans, the corridor Open, N's sides, X's three walls, N-X Open, so
        /// G1 too); both copies of every border round c and c' agree with BorderEdge; no room over the station, C1..N or X;
        /// no zone key there; the station's lamps the template's (never Auto), C1 Steady; leads-on from E2o and C1, and c
        /// and c' joined to the map with the strip standing and given back (only a plan accepted with a warning may miss
        /// those); the distance H + k.
        /// </summary>
        static void CheckStartGroup(FrontRoomsMapCache cache, StartGroupTemplate t, StartGroupPlan group, GridCoord titleDoorWorldCell, int halfCells, string at, Action<string> fail)
        {
            const int n = MapGrid.ChunkCells;
            at += "start group: ";
            var l = group.layout;
            var d = new GridCoord(titleDoorWorldCell.x - group.root.x, titleDoorWorldCell.y - group.root.y);
            var h = t.depth;
            int x0 = d.x - t.entrance, x1 = x0 + StartGroupTemplate.Width - 1, y0 = d.y, y1 = d.y + h - 1;
            var front = new GridCoord(d.x, y1 + t.corridor);
            var x = new GridCoord(d.x, front.y + 1);
            var c = MapGrid.ChunkOf(d);
            var cx = MapGrid.ChunkOf(x);
            int di = d.x - c.x * n, dj = d.y - c.y * n;
            bool InStation(GridCoord cell) => cell.x >= x0 && cell.x <= x1 && cell.y >= y0 && cell.y <= y1;
            bool Protected(GridCoord cell) => InStation(cell) || (cell.x == d.x && cell.y > y1 && cell.y <= front.y) || cell == x;
            if (l.door != d || l.station.x != x0 || l.station.y != y0 || l.station.w != StartGroupTemplate.Width || l.station.h != h || l.front != front || l.exit != x)
                fail(at + "the plan's layout (D " + l.door + ", N " + l.front + ", X " + l.exit + ") is not the template's at D " + d + " (N " + front + ", X " + x + ")");
            if (di != 5 || (dj != 2 && dj != 6)) fail(at + "D " + d + " is chunk-local (" + di + ", " + dj + "), not (5, 2) or (5, 6)");
            if (cx != new GridCoord(c.x, c.y + 1)) fail(at + "X " + x + " lies in chunk " + cx + ", not c + (0, 1) = " + new GridCoord(c.x, c.y + 1));
            if (!cache.IsPinned(c) || !cache.IsPinned(cx)) fail(at + "c " + c + " and c' " + cx + " are not both pinned");
            if (group.exit.distance != h + t.corridor) fail(at + "the EXIT is " + group.exit.distance + " walking cells from D, not H + k = " + (h + t.corridor));

            // Zones: the door row, the station and C1..N Standard Level 0; E2o Standard.
            bool StandardLevel0(GridCoord cell)
            {
                var zone = cache.ZoneOf(cell);
                return zone.height == ZoneHeight.Standard && zone.theme == ZoneTheme.Level0;
            }
            var zoneProblem = "";
            for (var i = -halfCells; i <= halfCells && zoneProblem.Length == 0; i++)
                if (!StandardLevel0(new GridCoord(d.x + i, d.y))) zoneProblem = "door row cell " + new GridCoord(d.x + i, d.y);
            for (var y = y0; y <= front.y && zoneProblem.Length == 0; y++)
            for (var xx = x0; xx <= x1 && zoneProblem.Length == 0; xx++)
                if ((y <= y1 || xx == d.x) && !StandardLevel0(new GridCoord(xx, y))) zoneProblem = (y <= y1 ? "station cell " : "corridor cell ") + new GridCoord(xx, y);
            if (zoneProblem.Length > 0) fail(at + "the " + zoneProblem + " is not Standard Level 0");
            var e2Out = new GridCoord(x0 - 1, y0 + t.e2Row);
            if (cache.ZoneOf(e2Out).height != ZoneHeight.Standard) fail(at + "E2o " + e2Out + " is " + cache.ZoneOf(e2Out).height + ", not Standard");

            // Every edge the template fixes.
            void Want(GridCoord a, GridCoord b, EdgeKind kind, string what)
            {
                var got = cache.Edge(a, b);
                if (got != kind) fail(at + what + " " + a + "-" + b + " is " + got + ", not " + kind);
            }
            for (var y = y0; y <= y1; y++)
            for (var xx = x0; xx <= x1; xx++)
            {
                var cell = new GridCoord(xx, y);
                if (xx < x1) Want(cell, new GridCoord(xx + 1, y), t.InnerEast(xx - x0, y - y0), "the station's inside");
                if (y < y1) Want(cell, new GridCoord(xx, y + 1), t.InnerNorth(xx - x0, y - y0), "the station's inside");
            }
            for (var xx = x0; xx <= x1; xx++)
            {
                Want(new GridCoord(xx, y0 - 1), new GridCoord(xx, y0), EdgeKind.Wall, xx == d.x ? "D's south edge" : "the station's south wall");
                Want(new GridCoord(xx, y1), new GridCoord(xx, y1 + 1), xx == d.x ? EdgeKind.Arch : EdgeKind.Wall, xx == d.x ? "E1" : "the station's north wall");
            }
            for (var y = y0; y <= y1; y++)
            {
                Want(new GridCoord(x0 - 1, y), new GridCoord(x0, y), y == e2Out.y ? EdgeKind.Arch : EdgeKind.Wall, y == e2Out.y ? "E2" : "the station's west wall");
                Want(new GridCoord(x1, y), new GridCoord(x1 + 1, y), EdgeKind.Wall, "the station's east wall");
            }
            for (var y = y1 + 1; y < front.y; y++) Want(new GridCoord(d.x, y), new GridCoord(d.x, y + 1), EdgeKind.Open, "the corridor");
            Want(new GridCoord(front.x - 1, front.y), front, EdgeKind.Wall, "N's west side");
            Want(front, new GridCoord(front.x + 1, front.y), EdgeKind.Wall, "N's east side");
            Want(new GridCoord(x.x - 1, x.y), x, EdgeKind.Wall, "X's west side");
            Want(x, new GridCoord(x.x + 1, x.y), EdgeKind.Wall, "X's east side");
            Want(x, new GridCoord(x.x, x.y + 1), EdgeKind.Wall, "X's north side");
            Want(front, x, EdgeKind.Open, "N-X");
            if (!FrontRoomsExitPlanner.StartDoorWestOpen(cache, d)) fail(at + "G1: the start door " + d + "'s west edge is not Open");
            const float spanSlack = .0001f;
            if (!cache.ArchOverride(new GridCoord(d.x, y1), new GridCoord(d.x, y1 + 1), out var w1, out var c1) || w1 < StartGroupTemplate.E1MinWidth - spanSlack
                || w1 > StartGroupTemplate.E1MaxWidth + spanSlack || Math.Abs(c1 - MapGrid.CellSize * .5f) > spanSlack)
                fail(at + "E1 has no authored span of " + StartGroupTemplate.E1MinWidth + "-" + StartGroupTemplate.E1MaxWidth + " m, centred (" + w1 + " m at " + c1 + ")");
            if (!cache.ArchOverride(e2Out, new GridCoord(x0, e2Out.y), out var w2, out var c2) || w2 < StartGroupTemplate.E2MinWidth - spanSlack
                || w2 > StartGroupTemplate.E2MaxWidth + spanSlack || Math.Abs(c2 - MapGrid.CellSize * .5f) > spanSlack)
                fail(at + "E2 has no authored span of " + StartGroupTemplate.E2MinWidth + "-" + StartGroupTemplate.E2MaxWidth + " m, centred (" + w2 + " m at " + c2 + ")");

            // Both copies of every border round c and c' agree, and BorderEdge (the generator's answer, with the carve's
            // overrides) is what they hold.
            var borderProblem = "";
            for (var cy = c.y - 1; cy <= c.y + 2 && borderProblem.Length == 0; cy++)
            for (var cxx = c.x - 1; cxx <= c.x + 1 && borderProblem.Length == 0; cxx++)
            {
                var chunk = cache.Get(new GridCoord(cxx, cy));
                var east = cache.Get(new GridCoord(cxx + 1, cy));
                var north = cache.Get(new GridCoord(cxx, cy + 1));
                var o = chunk.Origin;
                for (var k = 0; k < n && borderProblem.Length == 0; k++)
                {
                    var e = chunk.east[MapGrid.LocalIndex(n - 1, k)];
                    if (e != east.west[k] || e != cache.Generator.BorderEdge(new GridCoord(o.x + n - 1, o.y + k), true))
                        borderProblem = "east of chunk " + chunk.coord + " row " + k + ": " + e + " / " + east.west[k];
                    var u = chunk.north[MapGrid.LocalIndex(k, n - 1)];
                    if (u != north.south[k] || u != cache.Generator.BorderEdge(new GridCoord(o.x + k, o.y + n - 1), false))
                        borderProblem = "north of chunk " + chunk.coord + " column " + k + ": " + u + " / " + north.south[k];
                }
            }
            if (borderProblem.Length > 0) fail(at + "the copies of a border disagree with each other or with BorderEdge, " + borderProblem);

            // No room over a protected cell; no zone key in the station or X (each key in its own zone).
            foreach (var coord in new[] { c, cx })
            {
                var chunk = cache.Get(coord);
                foreach (var rect in chunk.rooms)
                {
                    var hit = false;
                    for (var j = rect.y; j < rect.y + rect.h && !hit; j++)
                    for (var i = rect.x; i < rect.x + rect.w && !hit; i++)
                        hit = Protected(chunk.Cell(i, j));
                    if (hit) fail(at + "room " + rect.x + "," + rect.y + " " + rect.w + "x" + rect.h + " of chunk " + coord + " covers the station, the corridor or X");
                }
            }
            var zones = new List<GridCoord> { cache.ZoneOf(x).id };
            for (var y = y0; y <= y1; y++)
            for (var xx = x0; xx <= x1; xx++)
            {
                var id = cache.ZoneOf(new GridCoord(xx, y)).id;
                if (!zones.Contains(id)) zones.Add(id);
            }
            foreach (var zone in zones)
            {
                var home = cache.Get(zone);
                if (!home.hasKey) continue;
                if (InStation(home.keyCell) || InStation(home.keySiteCell) || home.keyCell == x || home.keySiteCell == x)
                    fail(at + "zone " + zone + "'s key lies in the station or X (key " + home.keyCell + ", site " + home.keySiteCell + ")");
                else if (cache.ZoneOf(home.keyCell).id != zone) fail(at + "zone " + zone + "'s key " + home.keyCell + " lies outside it");
            }

            // Lamps: the station's as the template's, never Auto; C1 Steady.
            ModuleLamp LampOf(GridCoord cell)
            {
                var chunk = cache.Get(MapGrid.ChunkOf(cell));
                return chunk.lamp[MapGrid.LocalIndex(cell.x - chunk.Origin.x, cell.y - chunk.Origin.y)];
            }
            var lampProblem = "";
            for (var j = 0; j < h && lampProblem.Length == 0; j++)
            for (var i = 0; i < StartGroupTemplate.Width && lampProblem.Length == 0; i++)
            {
                var lamp = LampOf(new GridCoord(x0 + i, y0 + j));
                if (lamp == ModuleLamp.Auto || lamp != t.LampAt(i, j)) lampProblem = "station cell " + new GridCoord(x0 + i, y0 + j) + " is " + lamp + ", the template says " + t.LampAt(i, j);
            }
            if (lampProblem.Length == 0 && LampOf(new GridCoord(d.x, y1 + 1)) != ModuleLamp.Steady) lampProblem = "C1 is " + LampOf(new GridCoord(d.x, y1 + 1)) + ", not Steady";
            if (lampProblem.Length > 0) fail(at + "lamps: " + lampProblem);

            // Leads-on and the joins (4.3 step 5; the repair, CARVE 1.7): a plan accepted with a warning (4.11) may miss them.
            if (!string.IsNullOrEmpty(group.warning)) return;
            bool Off(GridCoord cell) => InStation(cell) || cell == x || cell == d || FrontRoomsExitPlanner.InArea(l.strip, cell);
            foreach (var start in new[] { e2Out, new GridCoord(d.x, y1 + 1) })
            {
                var seen = new HashSet<GridCoord> { start, d };
                var queue = new Queue<GridCoord>();
                queue.Enqueue(start);
                while (queue.Count > 0 && seen.Count < LeadsOnCap)
                {
                    var cell = queue.Dequeue();
                    for (var k = 0; k < 4; k++)
                    {
                        var next = k == 0 ? new GridCoord(cell.x + 1, cell.y) : k == 1 ? new GridCoord(cell.x - 1, cell.y) : k == 2 ? new GridCoord(cell.x, cell.y + 1) : new GridCoord(cell.x, cell.y - 1);
                        if (seen.Contains(next) || Off(next)) continue;
                        var kind = cache.Edge(cell, next);
                        if (kind == EdgeKind.Wall || kind == EdgeKind.Window) continue;
                        seen.Add(next);
                        queue.Enqueue(next);
                    }
                }
                if (seen.Count < LeadsOnCap) fail(at + "the way on from " + start + " (glass blocking, never into the strip, the station or X) reaches " + seen.Count + " cells, under " + LeadsOnCap);
            }
            var standing = UnjoinedPieces(cache, c, cx, InStation, x, l.strip, true);
            if (standing.Length > 0) fail(at + "with the strip standing, a piece of c and c' is cut off from the map at " + standing);
            var back = UnjoinedPieces(cache, c, cx, InStation, x, l.strip, false);
            if (back.Length > 0) fail(at + "with the strip given back, a piece of c and c' is cut off from the map at " + back);
        }

        /// <summary>
        /// The pieces of chunks c and c' (all their cells but the station, X and, while it stands, the strip) not joined to
        /// the map, read with the validator's own walk: a piece over walkable edges with a cell that is not Tall must walk to
        /// a cell beyond c and c'; a Tall-only one (left through windows by design) needs glass to such a cell or to a cell
        /// that is not Tall. One cell of each such piece, or "".
        /// </summary>
        static string UnjoinedPieces(FrontRoomsMapCache cache, GridCoord c, GridCoord cx, Func<GridCoord, bool> inStation, GridCoord x, CellRect strip, bool stripStands)
        {
            bool Region(GridCoord cell)
            {
                var k = MapGrid.ChunkOf(cell);
                return k == c || k == cx;
            }
            bool Inside(GridCoord cell) => Region(cell) && !inStation(cell) && cell != x && !(stripStands && FrontRoomsExitPlanner.InArea(strip, cell));
            bool Beyond(GridCoord cell) => !Region(cell) && !(stripStands && FrontRoomsExitPlanner.InArea(strip, cell));
            var cells = new List<GridCoord>();
            foreach (var coord in new[] { c, cx })
            {
                var o = MapGrid.ChunkOrigin(coord);
                for (var j = 0; j < MapGrid.ChunkCells; j++)
                for (var i = 0; i < MapGrid.ChunkCells; i++)
                {
                    var cell = new GridCoord(o.x + i, o.y + j);
                    if (Inside(cell)) cells.Add(cell);
                }
            }
            // Pieces over the edges `joins` lets through: per piece, whether it holds a cell that is not Tall and whether it
            // reaches a cell beyond c and c'.
            Dictionary<GridCoord, int> Label(Func<EdgeKind, bool> joins, List<bool> lower, List<bool> reaches)
            {
                var label = new Dictionary<GridCoord, int>();
                foreach (var start in cells)
                {
                    if (label.ContainsKey(start)) continue;
                    var id = lower.Count;
                    lower.Add(false);
                    reaches.Add(false);
                    label[start] = id;
                    var queue = new Queue<GridCoord>();
                    queue.Enqueue(start);
                    while (queue.Count > 0)
                    {
                        var cell = queue.Dequeue();
                        if (cache.ZoneOf(cell).height != ZoneHeight.Tall) lower[id] = true;
                        for (var k = 0; k < 4; k++)
                        {
                            var next = k == 0 ? new GridCoord(cell.x + 1, cell.y) : k == 1 ? new GridCoord(cell.x - 1, cell.y) : k == 2 ? new GridCoord(cell.x, cell.y + 1) : new GridCoord(cell.x, cell.y - 1);
                            if (Inside(next))
                            {
                                if (label.ContainsKey(next) || !joins(cache.Edge(cell, next))) continue;
                                label[next] = id;
                                queue.Enqueue(next);
                            }
                            else if (Beyond(next) && joins(cache.Edge(cell, next))) reaches[id] = true;
                        }
                    }
                }
                return label;
            }
            var walkLower = new List<bool>();
            var walkOut = new List<bool>();
            var walk = Label(FrontRoomsExitPlanner.Walkable, walkLower, walkOut);
            var glassLower = new List<bool>();
            var glassOut = new List<bool>();
            var glass = Label(MapGrid.Passable, glassLower, glassOut);
            var seen = new HashSet<int>();
            var problems = new List<string>();
            foreach (var cell in cells)
            {
                var w = walk[cell];
                if (!seen.Add(w)) continue;
                var g = glass[cell];
                if (walkLower[w] ? !walkOut[w] : !(glassLower[g] || glassOut[g])) problems.Add(cell.ToString());
            }
            return string.Join(" ", problems);
        }

        /// <summary>
        /// The nearest-first rule of a widened band (C23 round 2), with the validator's own walks: X's walk lies
        /// plan.nearestGap cells outside the band asked (lo-hi), 0 unless widened. On a widened plan every candidate
        /// of its band is found again by the validator's own rules (CandidateDistance): as many as the plan counted,
        /// none nearer the band asked than X, and as many as near (and as many dead ends among them) as the plan says.
        /// </summary>
        static void CheckNearest(FrontRoomsMapCache cache, CellRect area, GridCoord door, ExitPlan plan, int lo, int hi, string at, Action<string> fail)
        {
            var gapX = plan.distance < lo ? lo - plan.distance : plan.distance > hi ? plan.distance - hi : 0;
            if (gapX != plan.nearestGap) fail(at + "X lies " + gapX + " walking cells outside the band asked " + lo + "-" + hi + ", the plan says " + plan.nearestGap);
            if (plan.nearest < 1 || plan.nearest > plan.candidates || plan.nearestDeadEnds > plan.nearest || plan.nearestDeadEnds > plan.deadEnds)
                fail(at + "the nearest counts do not add up: " + plan.nearest + " nearest (" + plan.nearestDeadEnds + " dead ends) of " + plan.candidates + " (" + plan.deadEnds + ")");
            if (!plan.widened)
            {
                if (plan.nearestGap != 0 || plan.nearest != plan.candidates || plan.nearestDeadEnds != plan.deadEnds)
                    fail(at + "the band asked was not widened, yet the plan narrowed its pick: gap " + plan.nearestGap + ", " + plan.nearest + " of " + plan.candidates);
                return;
            }
            int count = 0, minGap = int.MaxValue, atMin = 0, deadAtMin = 0;
            var reach = Walk(cache, area, door, default(GridCoord), false, plan.bandMax - 1, int.MaxValue, plan.throughGlass);
            foreach (var pair in reach)
            {
                var candidate = new GridCoord(pair.Key.x, pair.Key.y + 1);
                var distance = CandidateDistance(cache, area, door, candidate, plan.throughGlass);
                if (distance < plan.bandMin || distance > plan.bandMax) continue;
                count++;
                var gap = distance < lo ? lo - distance : distance > hi ? distance - hi : 0;
                var deadEnd = cache.Edge(candidate, new GridCoord(candidate.x - 1, candidate.y)) == EdgeKind.Wall && cache.Edge(candidate, new GridCoord(candidate.x + 1, candidate.y)) == EdgeKind.Wall
                    && cache.Edge(candidate, new GridCoord(candidate.x, candidate.y + 1)) == EdgeKind.Wall;
                if (gap < minGap) { minGap = gap; atMin = 0; deadAtMin = 0; }
                if (gap == minGap) { atMin++; if (deadEnd) deadAtMin++; }
            }
            if (count != plan.candidates || minGap != gapX || atMin != plan.nearest || deadAtMin != plan.nearestDeadEnds)
                fail(at + "widened to " + plan.bandMin + "-" + plan.bandMax + ": the validator finds " + count + " candidates (plan " + plan.candidates + "), the nearest "
                    + (minGap == int.MaxValue ? "-" : minGap.ToString()) + " cells outside " + lo + "-" + hi + " (X " + gapX + "), " + atMin + " that near (plan " + plan.nearest + "), "
                    + deadAtMin + " dead ends among them (plan " + plan.nearestDeadEnds + ")");
        }

        /// <summary>
        /// A cell X' against every rule CheckExit asks of a planned X, read the validator's way: chunk-local 1..6, off
        /// the start area and its ring, in no room, no column on a corner, no key's cell, N' Standard, nothing on the
        /// sightline (glass now or after a shift), its chunk whole without it. Returns its walking distance (the walk
        /// to N' without X', glass as asked, plus the EXIT door), or -1 when a rule turns it down.
        /// </summary>
        static int CandidateDistance(FrontRoomsMapCache cache, CellRect area, GridCoord door, GridCoord x, bool glass)
        {
            const int n = MapGrid.ChunkCells;
            var front = new GridCoord(x.x, x.y - 1);
            var coord = MapGrid.ChunkOf(x);
            var origin = MapGrid.ChunkOrigin(coord);
            int i = x.x - origin.x, j = x.y - origin.y;
            if (i < 1 || i > n - 2 || j < 1 || j > n - 2 || RingDistance(area, x) <= 1) return -1;
            var chunk = cache.Get(coord);
            for (var r = 0; r < chunk.rooms.Length; r++)
                if (chunk.rooms[r].Contains(i, j)) return -1;
            if (chunk.pillar[i + j * (n + 1)] || chunk.pillar[i + 1 + j * (n + 1)] || chunk.pillar[i + (j + 1) * (n + 1)] || chunk.pillar[i + 1 + (j + 1) * (n + 1)]) return -1;
            var keyed = false;
            CheckKeys(cache, x, string.Empty, message => keyed = true);
            if (keyed) return -1;
            if (cache.ZoneOf(front).height != ZoneHeight.Standard || SightlineProblem(cache, x, false) != null || SightlineProblem(cache, x, true) != null) return -1;
            var withX = ChunkReach(cache, area, coord, front, null);
            if (!withX.Contains(x)) return -1;
            var withoutX = ChunkReach(cache, area, coord, front, x);
            foreach (var cell in withX)
                if (cell != x && !withoutX.Contains(cell)) return -1;
            var walk = Walk(cache, area, door, x, true, WidestBand, int.MaxValue, glass);
            return walk.TryGetValue(front, out var steps) ? steps + 1 : -1;
        }

        // From N to a side neighbour: neither a wall nor glass, and the neighbour not in the start area.
        static bool WalksThrough(FrontRoomsMapCache cache, CellRect area, GridCoord front, GridCoord side)
        {
            var kind = cache.Edge(front, side);
            return kind != EdgeKind.Wall && kind != EdgeKind.Window && RingDistance(area, side) != 0;
        }

        /// <summary>
        /// The EXIT door's sightline, read by the validator: every edge round N = X + (0, -1), N + (0, -1) and
        /// N + (0, -2) except N-X (the EXIT door). Without <paramref name="shiftable"/>: the first edge that is glass
        /// now. With it: the first edge that runs between a Tall cell and a lower one inside one chunk other than
        /// X's, where a revisit shift could put glass. Null when there is none.
        /// </summary>
        static string SightlineProblem(FrontRoomsMapCache cache, GridCoord x, bool shiftable)
        {
            var exitChunk = MapGrid.ChunkOf(x);
            for (var k = 1; k <= FrontRoomsExitPlanner.SightlineCells; k++)
            {
                var cell = new GridCoord(x.x, x.y - k);
                for (var d = 0; d < 4; d++)
                {
                    var other = d == 0 ? new GridCoord(cell.x + 1, cell.y) : d == 1 ? new GridCoord(cell.x - 1, cell.y) : d == 2 ? new GridCoord(cell.x, cell.y + 1) : new GridCoord(cell.x, cell.y - 1);
                    if (other == x) continue;
                    if (!shiftable)
                    {
                        if (cache.Edge(cell, other) == EdgeKind.Window) return cell + "-" + other + " is glass";
                        continue;
                    }
                    var tallCell = cache.ZoneOf(cell).height == ZoneHeight.Tall;
                    var tallOther = cache.ZoneOf(other).height == ZoneHeight.Tall;
                    var chunk = MapGrid.ChunkOf(cell);
                    // A chunk the floor-1 start group pins (A26) never shifts either.
                    if (tallCell != tallOther && chunk == MapGrid.ChunkOf(other) && chunk != exitChunk && !cache.IsPinned(chunk))
                        return cell + "-" + other + " (a Tall border inside chunk " + chunk + ", now " + cache.Edge(cell, other) + ")";
                }
            }
            return null;
        }

        // The widest band the planner can use: no walk needs to go further.
        const int WidestBand = FrontRoomsExitPlanner.WidestBandMax;

        /// <summary>X is no zone's key cell, key site cell or key spot cell, for every zone homed within two chunks of X (every zone X can belong to).</summary>
        static void CheckKeys(FrontRoomsMapCache cache, GridCoord x, string at, Action<string> fail)
        {
            var coord = MapGrid.ChunkOf(x);
            for (var dy = -2; dy <= 2; dy++)
            for (var dx = -2; dx <= 2; dx++)
            {
                var home = cache.Get(new GridCoord(coord.x + dx, coord.y + dy));
                if (home.hasKey && (home.keyCell == x || home.keySiteCell == x))
                    fail(at + "X " + x + " is the key " + (home.keySpot ? "spot" : "site") + " cell of zone " + home.coord);
            }
        }

        /// <summary>
        /// Revisit-shift every chunk round X's (the map never shifts X's own, D5, nor one the floor-1 start group pins,
        /// A26), then: X is still no key's cell,
        /// and from N (never through X or the start area, glass counting as passable, inside the 5 x 5 chunks
        /// round X) every passable neighbour of X and every one of the 8 chunks round X's are still reached.
        /// </summary>
        static void CheckAfterShift(FrontRoomsMapCache cache, CellRect area, ExitPlan plan, string at, Action<string> fail)
        {
            var x = plan.exitCell;
            var coord = MapGrid.ChunkOf(x);
            for (var dy = -1; dy <= 1; dy++)
            for (var dx = -1; dx <= 1; dx++)
            {
                var near = new GridCoord(coord.x + dx, coord.y + dy);
                if ((dx != 0 || dy != 0) && !cache.IsPinned(near)) cache.Shift(near);
            }
            CheckKeys(cache, x, at + "after a revisit shift, ", fail);
            int x0 = (coord.x - 2) * MapGrid.ChunkCells, y0 = (coord.y - 2) * MapGrid.ChunkCells, x1 = x0 + 5 * MapGrid.ChunkCells, y1 = y0 + 5 * MapGrid.ChunkCells;
            var seen = new HashSet<GridCoord> { plan.frontCell };
            var queue = new Queue<GridCoord>();
            queue.Enqueue(plan.frontCell);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                for (var d = 0; d < 4; d++)
                {
                    var next = d == 0 ? new GridCoord(cell.x + 1, cell.y) : d == 1 ? new GridCoord(cell.x - 1, cell.y) : d == 2 ? new GridCoord(cell.x, cell.y + 1) : new GridCoord(cell.x, cell.y - 1);
                    if (next.x < x0 || next.x >= x1 || next.y < y0 || next.y >= y1 || next == x || seen.Contains(next) || FrontRoomsExitPlanner.InArea(area, next)) continue;
                    if (!MapGrid.Passable(cache.Edge(cell, next))) continue;
                    seen.Add(next);
                    queue.Enqueue(next);
                }
            }
            for (var d = 0; d < 4; d++)
            {
                var u = d == 0 ? new GridCoord(x.x + 1, x.y) : d == 1 ? new GridCoord(x.x - 1, x.y) : d == 2 ? new GridCoord(x.x, x.y + 1) : new GridCoord(x.x, x.y - 1);
                if (MapGrid.Passable(cache.Edge(x, u)) && !FrontRoomsExitPlanner.InArea(area, u) && !seen.Contains(u))
                    fail(at + "after a revisit shift round X " + x + ", its neighbour " + u + " is cut off from N");
            }
            var chunks = new HashSet<GridCoord>();
            foreach (var cell in seen) chunks.Add(MapGrid.ChunkOf(cell));
            for (var dy = -1; dy <= 1; dy++)
            for (var dx = -1; dx <= 1; dx++)
                if (!chunks.Contains(new GridCoord(coord.x + dx, coord.y + dy)))
                    fail(at + "after a revisit shift round X " + x + ", N no longer reaches chunk " + new GridCoord(coord.x + dx, coord.y + dy));
            // The front rules hold after the shift too: N's zone never changes, and no window came onto the sightline.
            var glass = SightlineProblem(cache, x, false);
            if (glass != null) fail(at + "after a revisit shift round X " + x + ", a window on the EXIT door's sightline: " + glass);
            if (cache.ZoneOf(plan.frontCell).height != ZoneHeight.Standard) fail(at + "after a revisit shift round X " + x + ", N is no longer Standard");
        }

        /// <summary>Chebyshev distance from a cell to a rect of cells (0 inside; the ring is 1).</summary>
        static int RingDistance(CellRect area, GridCoord cell)
        {
            if (area.w <= 0 || area.h <= 0) return int.MaxValue;
            var dx = Math.Max(Math.Max(area.x - cell.x, cell.x - (area.x + area.w - 1)), 0);
            var dy = Math.Max(Math.Max(area.y - cell.y, cell.y - (area.y + area.h - 1)), 0);
            return Math.Max(dx, dy);
        }

        /// <summary>
        /// The validator's own walk: BFS from <paramref name="from"/> over edges that are neither walls nor glass (glass
        /// passable when <paramref name="glass"/>),
        /// never into the start area or <paramref name="avoid"/> (when <paramref name="hasAvoid"/>), out to
        /// <paramref name="maxSteps"/> steps, stopping once <paramref name="cap"/> cells are reached.
        /// </summary>
        static Dictionary<GridCoord, int> Walk(FrontRoomsMapCache cache, CellRect area, GridCoord from, GridCoord avoid, bool hasAvoid, int maxSteps, int cap, bool glass = false)
        {
            var dist = new Dictionary<GridCoord, int> { [from] = 0 };
            var queue = new Queue<GridCoord>();
            queue.Enqueue(from);
            while (queue.Count > 0 && dist.Count < cap)
            {
                var cell = queue.Dequeue();
                var steps = dist[cell];
                if (steps >= maxSteps) continue;
                for (var d = 0; d < 4 && dist.Count < cap; d++)
                {
                    var next = d == 0 ? new GridCoord(cell.x + 1, cell.y) : d == 1 ? new GridCoord(cell.x - 1, cell.y) : d == 2 ? new GridCoord(cell.x, cell.y + 1) : new GridCoord(cell.x, cell.y - 1);
                    if (dist.ContainsKey(next) || (hasAvoid && next == avoid) || RingDistance(area, next) == 0) continue;
                    var kind = cache.Edge(cell, next);
                    if (kind == EdgeKind.Wall || (!glass && kind == EdgeKind.Window)) continue;
                    dist[next] = steps + 1;
                    queue.Enqueue(next);
                }
            }
            return dist;
        }

        /// <summary>Cells of one chunk reached from a cell over the chunk's own edges (anything but a wall), outside the start area and never through <paramref name="avoid"/>.</summary>
        static HashSet<GridCoord> ChunkReach(FrontRoomsMapCache cache, CellRect area, GridCoord chunk, GridCoord from, GridCoord? avoid)
        {
            var origin = MapGrid.ChunkOrigin(chunk);
            var seen = new HashSet<GridCoord> { from };
            var queue = new Queue<GridCoord>();
            queue.Enqueue(from);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                for (var d = 0; d < 4; d++)
                {
                    var next = d == 0 ? new GridCoord(cell.x + 1, cell.y) : d == 1 ? new GridCoord(cell.x - 1, cell.y) : d == 2 ? new GridCoord(cell.x, cell.y + 1) : new GridCoord(cell.x, cell.y - 1);
                    if (next.x < origin.x || next.x >= origin.x + MapGrid.ChunkCells || next.y < origin.y || next.y >= origin.y + MapGrid.ChunkCells) continue;
                    if (seen.Contains(next) || (avoid.HasValue && next == avoid.Value) || RingDistance(area, next) == 0) continue;
                    if (!MapGrid.Passable(cache.Edge(cell, next))) continue;
                    seen.Add(next);
                    queue.Enqueue(next);
                }
            }
            return seen;
        }

        /// <summary>The same modules in the same rooms, by value (every generator clones its library).</summary>
        static bool SameModules(MapChunk a, MapChunk b)
        {
            if (a.rooms.Length != b.rooms.Length) return false;
            for (var r = 0; r < a.rooms.Length; r++)
            {
                RoomModuleData x = a.ModuleOf(r), y = b.ModuleOf(r);
                if ((x == null) != (y == null)) return false;
                if (x != null && (x.width != y.width || x.depth != y.depth || !Same(x.lamps, y.lamps) || !Same(x.props, y.props) || !Same(x.markers, y.markers)
                    || !Same(x.innerEast, y.innerEast) || !Same(x.innerNorth, y.innerNorth))) return false;
            }
            return true;
        }

        static bool Same<T>(T[] a, T[] b)
        {
            if (a.Length != b.Length) return false;
            var comparer = EqualityComparer<T>.Default;
            for (var i = 0; i < a.Length; i++) if (!comparer.Equals(a[i], b[i])) return false;
            return true;
        }
    }
}
