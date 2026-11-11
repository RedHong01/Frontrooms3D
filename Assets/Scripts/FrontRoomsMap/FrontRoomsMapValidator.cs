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
        public int tallExits;
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

    /// <summary>One seed of the Run! check (RunRunRooms): the counts and the rows' errors (also in the seed's own errors).</summary>
    [Serializable]
    public sealed class MapRunSeedRow
    {
        public int seed;
        public bool passed;
        // Zones decided, rooms placed, and why the rest got none (met before Ready, not eligible, no straight fits, a chunk lost to a higher-priority zone).
        public int zonesDecided, placed, notReady, ineligible, noFit, suppressed;
        // Placed rooms by host theme, by goal (a forced Door onto a Standard cell, or a Standard-to-Low border door), those straddling zones, and module Relay entries the world would hide (within minEntryCells).
        public int level0, office, ownZoneDoors, borderDoors, multiZone, entriesHidden;
        // The Run! rethink (71 s.9, card 1): rooms by length and twist; the competing zones of the square and how many hold a
        // straight of the checked length (the fit rate's two counts).
        public int cells7, cells9, cells12, cells14, doorEntrances, receding, competing, fit;
        public List<string> errors = new List<string>();
    }

    /// <summary>Run! rooms (L7, CR-1 to CR-6) over the 100 seeds (RunRunRooms): every map rule again, plus the Run! rows.</summary>
    [Serializable]
    public sealed class MapRunReport
    {
        public string suite = "FrontRooms Run! rooms (L7, CR-1 to CR-6)";
        public string scope = "Per seed: the start door at cell (0, 0) with its chunk met at tier 1, every other zone met at tier 2 after the first send (the profile's budget), then ValidateSeed's map rules with Run! rooms in (doors between two Standard cells only on a goal edge; determinism and revisit shifts on a fresh cache with the same rules) and the Run! rows per room: CR-1 (9 cells in a straight, Standard and of the host zone's theme (zones may be straddled, both chunks holding a host cell), inside Open, long sides Wall, arch, goal Door, lamps Off, no room over it, one chunk border crossed, the zone key outside), CR-5 (every zone touched met at tier 2+ after the first send, never the arrival zone, no zone key inside, 15 cells from the start door, one zone per chunk, the rest of each chunk joined without the corridor) and CR-6 (the goal's far cell). Data only.";
        public string timestampUtc;
        public bool goalDoorForced, office;
        public int radiusChunks, seedCount, passed, failed, placed, notReady, ineligible, noFit, suppressed, ownZoneDoors, borderDoors, multiZone, entriesHidden;
        // The Run! rethink (71 s.9, card 1): the card every room is (per zone: forceCard, else plain 9), rooms by length and twist,
        // and the fit rate: competing zones of the squares that hold a straight of the card's length (fit / competing).
        public string card;
        public int cells7, cells9, cells12, cells14, doorEntrances, receding, competing, fit;
        public float fitRate;
        public List<MapRunSeedRow> seeds = new List<MapRunSeedRow>();
    }

    /// <summary>One run of the deck check (RunRunDeck): every floor's deal, and the rows' errors.</summary>
    [Serializable]
    public sealed class MapRunDeckSeedRow
    {
        public int seed;
        public bool passed;
        // Per floor: "floor 3 T3: door 9 (deck door 9)" ... (RunDeal.ToString).
        public List<string> deals = new List<string>();
        // Rooms placed, floors dealt nothing, twists placed, devices the calm rule kept out (planner-excluded candidates touching a calm zone).
        public int rooms, nothing, twists, calmExcluded;
        // The devices-first pass (card 3's other order): rooms placed, zones refused (a goal door into a zone with a decided
        // device: CalmRefused), and devices decided before a room on its host zone's border (built later as plain).
        public int firstRooms, calmRefused, hostDevicesPlain;
        public List<string> errors = new List<string>();
    }

    /// <summary>
    /// The Run! deck and the calm rule (71 s.9, cards 2 and 3; RunRunDeck): a run of floors per seed, the game's way (one Run!
    /// a floor from the run's deck, foil and alarm doors forced on the one budget), every room placed counted as met.
    /// </summary>
    [Serializable]
    public sealed class MapRunDeckReport
    {
        public string suite = "FrontRooms Run! deck and calm (71 s.9, cards 2 and 3)";
        public string scope = "Per seed a run of floors 1..N at the tiers given (non-decreasing), each a fresh cache on its floor seed with the run's one FrontRoomsRunDeck, the start door at cell (0, 0) met at tier 1, every other zone met at the floor's tier after the first send, foil and alarm doors forced on the one budget, every placed room counted as met. Rows: at most one Run! a floor; the run's first Run! plain 7, built at 7 (never shortened), and plain until one is met; no twist on two floors running (the receding end never); the receding end only on B2+ (9 -> 14 only on B3+ at T5); lengths by tier (decision 3: T2-T3 9, T4 12, T5 14 or 12 where 14 does not fit; a twist that cannot be dealt plays as the tier's plain room; a 9-cell room at T4-T5 only as a Run door or ramp twist; FitOrder shortens only 14 to 12 and a receding 9 -> 14 to 9 -> 12); each room's structure (CR-1, CR-5, CR-6, the entrance arch or Run door, F5 never the only route, F7 no chain or dead end beyond the goal door); card 3: no foiled window or alarm door decided after a room touches its host zone or the zone beyond its goal door; a replay of the same run deals the same. Then a devices-first pass per seed (card 3's other order: every device of the square's west half decided at tier 1 before the floor's zones are met): no room placed whose goal door opens into a zone a decided device touches (they are refused, CalmRefused), and every device in a room's calm zones was decided before it and touches only its host zone (built later, it reads plain). Data only.";
        public string timestampUtc;
        public bool doorEntrance, recede, ramps;
        public string tiers;
        public int seedCount, floors, passed, failed, rooms, nothing, twists, calmExcluded, firstRooms, calmRefused, hostDevicesPlain;
        // Rooms by what was built (plain 7 / 9 / 12 / 14, door 9, recede 9->12 / 9->14) and by tier.
        public List<string> builtCounts = new List<string>();
        public List<MapRunDeckSeedRow> seeds = new List<MapRunDeckSeedRow>();
    }

    /// <summary>One seed of the foil check (RunFoil; C13, Relay v2 step V7a): counts, and the rows' errors.</summary>
    [Serializable]
    public sealed class MapFoilSeedRow
    {
        public int seed;
        public bool passed;
        // Tall zones wholly inside the square; their windows (every one, the chunk-border candidates, the interior ones), plain and foiled.
        public int tallZones, windows, candidates, interior, plain, foiled;
        // Tall zones whose budget roll passes at their stored tier (the unforced density), and those without a foil by reason (FoilMiss).
        public int slots, noSlot, noWindow, arrival, taken;
        // Candidate windows RELAY 6's places exclude (the first zone, 15 cells of the start door).
        public int excluded;
        // The shared budget's ledger (every zone met): slots held by foiled windows and by Run! rooms; Run! rooms placed.
        public int ledgerFoil, ledgerRun, runRooms;
        // Relay entries: zones wholly inside, their auto entries by how they were found, zones too small for two 10 cells apart.
        public int entryZones, entries, deadEnds, corridorEnds, otherCells, smallZones;
        // Cover (RELAY 12, reported only): metres of walking out of a 12 m sight line, worst and mean over the cells of Tall
        // zones and of the other zones, and the Tall zones whose worst cell is over 15 m.
        public float coverTallMax, coverTallMean, coverOtherMax, coverOtherMean;
        public int coverTallOver;
        public List<string> errors = new List<string>();
    }

    /// <summary>One Tall zone's cover (RELAY 12, reported): its cells, the worst and mean walk out of a 12 m sight line.</summary>
    [Serializable]
    public sealed class MapFoilZoneCover
    {
        public int seed;
        public GridCoord zone;
        public int cells;
        public float max, mean;
    }

    /// <summary>C13's foiled windows, the shared trigger budget, the auto Relay entries and the cover measure over the seeds (RunFoil).</summary>
    [Serializable]
    public sealed class MapFoilReport
    {
        public string suite = "FrontRooms foiled windows, trigger budget, Relay entries and cover (C13, Relay v2 V7a)";
        public string scope = "Per seed: the start door at cell (0, 0) with its chunk met at tier 1, every other zone met at the check's tier after the first send, foil on (and Run! rooms when given: one budget), the square of chunks built. Per Tall zone wholly inside: its chunk-border windows are the planner's candidates and are windows; every border with windows keeps a plain one; the zone keeps min(2, count) plain; at most one foiled window, only with the zone's budget slot (or forced), never plain, never excluded (the first zone, 15 cells of the start door), never walkable for the route; the ledger holds Foil for it; a fresh cache decides the same; a revisit shift of the foiled window's chunks keeps it a foiled window. The ledger: one kind per zone, counts equal to the planners'. Relay entries: every zone wholly inside has two 10 cells apart unless its cells are not that far apart. Cover: reported, never a failure. Data only.";
        public string timestampUtc;
        public bool force, runRooms;
        public int radiusChunks, seedCount, tier, passed, failed;
        public int tallZones, windows, candidates, interior, plain, foiled, slots, noSlot, noWindow, arrival, taken, excluded, ledgerFoil, ledgerRun, runRoomsPlaced;
        public int entryZones, entries, deadEnds, corridorEnds, otherCells, smallZones;
        public float coverTallMax, coverTallMean, coverOtherMax, coverOtherMean;
        public int coverTallOver;
        public List<MapFoilSeedRow> seeds = new List<MapFoilSeedRow>();
        // The Tall zones whose worst cell is over CoverTargetMetres (RELAY 12's target), worst first, at most 40.
        public List<MapFoilZoneCover> coverWorst = new List<MapFoilZoneCover>();
    }

    /// <summary>One seed of the alarm-door check (RunAlarmDoors; C16, Relay v2 step V7b): counts, and the rows' errors.</summary>
    [Serializable]
    public sealed class MapAlarmDoorSeedRow
    {
        public int seed;
        public bool passed;
        // Low and Standard zones wholly inside the square; the doors they own (FrontRoomsAlarmDoors.OwnerOf): every one, the
        // chunk-border candidates, those inside chunks, kept plain, alarmed.
        public int zones, doors, candidates, interior, plain, alarmed;
        // Zones whose budget roll passes at their stored tier (the unforced density), and those without an alarm door by reason (AlarmDoorMiss).
        public int slots, noSlot, noDoor, arrival, taken;
        // Owned candidates RELAY 6's places exclude, and borders with a single chunk-border door (kept plain: never the only crossing).
        public int excluded, singleBorders;
        // The one budget's ledger (every zone met): slots held by alarm doors, foiled windows and Run! rooms; Run! rooms placed.
        public int ledgerAlarm, ledgerFoil, ledgerRun, runRooms;
        public List<string> errors = new List<string>();
    }

    /// <summary>C16's alarm push-bar doors in the one trigger budget over the seeds (RunAlarmDoors).</summary>
    [Serializable]
    public sealed class MapAlarmDoorReport
    {
        public string suite = "FrontRooms alarm push-bar doors in the one trigger budget (C16, Relay v2 V7b)";
        public string scope = "Per seed: the start door at cell (0, 0) with its chunk met at tier 1, every other zone met at the check's tier after the first send, alarm doors on (and foil and Run! rooms when given: one budget), the square of chunks built. Per Low or Standard zone wholly inside: its chunk-border doors by the seed's rule are the planner's candidates and are doors; at most one alarm door, a chunk-border door the zone owns, only with the zone's budget slot (or forced), never kept plain, never excluded (the first zone, 15 cells of the start door, a Run! room), never the only crossing of its border (another walkable crossing between the two zones that is no alarm door); the ledger holds AlarmDoor for it; a fresh cache decides the same; a revisit shift of its chunks keeps it a door and alarmed. The ledger: one kind per zone, counts equal to the planners'. The Run! rooms of the zones inside and every inside Tall zone's foil are the same as on a cache without alarm doors. Data only.";
        public string timestampUtc;
        public bool force, foil, runRooms;
        public int radiusChunks, seedCount, tier, passed, failed;
        public int zones, doors, candidates, interior, plain, alarmed, slots, noSlot, noDoor, arrival, taken, excluded, singleBorders, ledgerAlarm, ledgerFoil, ledgerRun, runRoomsPlaced;
        public List<MapAlarmDoorSeedRow> seeds = new List<MapAlarmDoorSeedRow>();
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

        public static MapSeedReport ValidateSeed(MapSettings settings, int radius, IReadOnlyList<RoomModuleData> modules = null) =>
            ValidateSeed(settings, radius, modules, null, null);

        /// <summary>
        /// Run! (L7): every Run! room over the seeds with <paramref name="rules"/> (the profile's, its switches as set), on maps
        /// made as RunCache makes them. Each seed runs ValidateSeed's map rules with Run! rooms in, then the Run! rows.
        /// </summary>
        public static MapRunReport RunRunRooms(MapSettings settings, int firstSeed, int seedCount, int radius, IReadOnlyList<RoomModuleData> modules, FrontRoomsRunRules rules)
        {
            var report = new MapRunReport { radiusChunks = radius, seedCount = seedCount, timestampUtc = DateTime.UtcNow.ToString("o"), goalDoorForced = rules != null && rules.goalDoorForced, office = rules != null && rules.office };
            report.card = (rules == null || rules.forceCard.IsNothing ? RunCard.Plain(FrontRoomsRunRooms.Length) : rules.forceCard).ToString();
            for (var i = 0; i < seedCount; i++)
            {
                var s = (settings ?? new MapSettings()).Clone();
                s.seed = unchecked(firstSeed + i * 7919);
                var row = new MapRunSeedRow { seed = s.seed };
                var seedReport = ValidateSeed(s, radius, modules, rules ?? new FrontRoomsRunRules(), row);
                row.passed = seedReport.passed;
                if (!seedReport.passed) foreach (var e in seedReport.errors) if (row.errors.Count < 12) row.errors.Add(e);
                report.seeds.Add(row);
                if (row.passed) report.passed++; else report.failed++;
                report.placed += row.placed;
                report.notReady += row.notReady;
                report.ineligible += row.ineligible;
                report.noFit += row.noFit;
                report.suppressed += row.suppressed;
                report.ownZoneDoors += row.ownZoneDoors;
                report.borderDoors += row.borderDoors;
                report.multiZone += row.multiZone;
                report.entriesHidden += row.entriesHidden;
                report.cells7 += row.cells7;
                report.cells9 += row.cells9;
                report.cells12 += row.cells12;
                report.cells14 += row.cells14;
                report.doorEntrances += row.doorEntrances;
                report.receding += row.receding;
                report.competing += row.competing;
                report.fit += row.fit;
            }
            report.fitRate = report.competing > 0 ? (float)report.fit / report.competing : 0f;
            return report;
        }

        /// <summary>
        /// The Run! rules these check caches hold (RunCache, FoilCache, AlarmCache): the caller's, placed per zone (deck off) and
        /// without the calm rule (calm off), so every plan stays pure in the seed and the zone and the fresh-cache and revisit
        /// checks hold as before the Run! rethink. The deck and the calm rule have their own check (RunRunDeck).
        /// </summary>
        public static FrontRoomsRunRules ChecksRules(FrontRoomsRunRules rules)
        {
            if (rules == null) return null;
            var r = rules.Clone();
            r.deck = false;
            r.calm = false;
            return r;
        }

        /// <summary>
        /// A cache with Run! rooms as the check plays them: the start door at cell (0, 0), its chunk generated at tier 1 before
        /// the map is ready (the arrival zone), then the map ready, the first send gone out and every later zone met at tier 2.
        /// The same steps on any cache give the same rooms (the plan is pure in the seed, the zone and its tier): the rooms are
        /// placed per zone (ChecksRules: forceCard's room in every zone whose slot roll passes, else today's plain 9).
        /// </summary>
        public static FrontRoomsMapCache RunCache(MapSettings settings, IReadOnlyList<RoomModuleData> modules, FrontRoomsRunRules rules)
        {
            var cache = new FrontRoomsMapCache(settings, modules);
            var run = new FrontRoomsRunRooms(ChecksRules(rules) ?? new FrontRoomsRunRules { deck = false, calm = false });
            cache.SetRunRooms(run);
            var start = new GridCoord(0, 0);
            run.SetStart(start);
            cache.Tier = 1;
            cache.Get(MapGrid.ChunkOf(start));
            run.Ready = true;
            run.Open = true;
            cache.Tier = 2;
            return cache;
        }

        static MapSeedReport ValidateSeed(MapSettings settings, int radius, IReadOnlyList<RoomModuleData> modules, FrontRoomsRunRules runRules, MapRunSeedRow runRow)
        {
            var report = new MapSeedReport { seed = settings.seed };
            var cache = runRules != null ? RunCache(settings, modules, runRules) : new FrontRoomsMapCache(settings, modules);
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
                // Run! (CR-6a, card 5): a goal door, or a Run door room's entrance, may stand between two Standard cells (only with Run! rooms in).
                var runGoal = kind == EdgeKind.Door && cache.Run != null && cache.Run.IsRunDoorEdge(a, b);
                if (ha == hb && (kind == EdgeKind.Door || kind == EdgeKind.Window) && !runGoal) Fail(kind + " between two " + ha + " cells at " + a + "-" + b);
                // C33: a Tall zone's one arch to a lower zone is the only opening across a height change that is not a door or glass.
                if (ha != hb && (kind == EdgeKind.Open || (kind == EdgeKind.Arch && !gen.IsTallExit(a, b)))) Fail(kind + " across a height change at " + a + "-" + b);
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
                    // C33: a Tall zone with a lower neighbour has its arch to it, and the map built it as an arch
                    // (one walled in by other Tall zones has none: it opens into them at one height).
                    if (gen.TallExit(id, out var ta, out var tb))
                    {
                        if (cache.Edge(ta, tb) != EdgeKind.Arch) Fail("tall zone " + id + "'s exit " + ta + "-" + tb + " is " + cache.Edge(ta, tb) + ", not an arch (C33)");
                        else report.tallExits++;
                    }
                    continue;
                }
                if (!home.hasKey) { Fail("zone " + id + " has no key"); continue; }
                report.keys++;
                if (gen.ZoneOf(home.keyCell).id != id) Fail("key of zone " + id + " lies outside it at " + home.keyCell);
            }

            // 5. Determinism: a fresh generator building in a different order
            // produces identical chunks. 6. A shifted chunk keeps its borders.
            var fresh = new FrontRoomsMapGenerator(settings, modules);
            // Run! rooms: a fresh cache made the same way (RunCache), built in this other order, and its revisit shifts.
            var freshCache = runRules != null ? RunCache(settings, modules, runRules) : null;
            for (var cy = max - 1; cy >= min; cy -= 3)
            for (var cx = max - 1; cx >= min; cx -= 3)
            {
                var coord = new GridCoord(cx, cy);
                var a = cache.Get(coord);
                var b = freshCache != null ? freshCache.Get(coord) : fresh.Generate(coord);
                if (!Same(a.east, b.east) || !Same(a.north, b.north) || !Same(a.west, b.west) || !Same(a.south, b.south) || !Same(a.pillar, b.pillar) || !Same(a.pillarStyle, b.pillarStyle) || a.keyCell != b.keyCell || a.keySpot != b.keySpot || a.keyX != b.keyX || a.keyZ != b.keyZ || a.keyY != b.keyY || a.tier != b.tier
                    || !Same(a.rooms, b.rooms) || !Same(a.lamp, b.lamp) || !SameModules(a, b))
                    Fail("chunk " + coord + " differs when rebuilt");
                MapChunk shifted;
                if (freshCache != null)
                {
                    freshCache.Shift(coord);
                    shifted = freshCache.Get(coord);
                }
                else shifted = fresh.Generate(coord, 1);
                for (var k = 0; k < n; k++)
                {
                    if (shifted.east[MapGrid.LocalIndex(n - 1, k)] != a.east[MapGrid.LocalIndex(n - 1, k)] || shifted.west[k] != a.west[k]
                        || shifted.north[MapGrid.LocalIndex(k, n - 1)] != a.north[MapGrid.LocalIndex(k, n - 1)] || shifted.south[k] != a.south[k])
                        Fail("shifted chunk " + coord + " changed a border");
                }
            }

            // 9. Run! rooms (CR-1, CR-5, CR-6), with them in.
            if (runRules != null) CheckRunRooms(cache, freshCache, min, max, runRules, runRow, Fail);

            report.passed = report.errors.Count == 0;
            return report;
        }

        // ---------- 9. Run! rooms (L7; the Run! rethink, 71 s.9 cards 1, 3, 4, 5) ----------

        static void CheckRunRooms(FrontRoomsMapCache cache, FrontRoomsMapCache fresh, int min, int max, FrontRoomsRunRules rules, MapRunSeedRow row, Action<string> fail)
        {
            var run = cache.Run;
            var gen = cache.Generator;
            if (row != null)
            {
                row.zonesDecided = run.ZonesDecided;
                row.notReady = run.NotReady;
                row.ineligible = run.Ineligible;
                row.noFit = run.NoFit;
                row.suppressed = run.Suppressed;
            }
            // Per zone (the check caches): every room is the card, exactly (forceCard, else today's plain 9).
            var card = rules.forceCard.IsNothing ? RunCard.Plain(FrontRoomsRunRooms.Length) : rules.forceCard;
            // Card 1's fit rate: the zones whose site lies in the square (one site per chunk) two chunks or more from the start
            // door's (so the 15-cell rule round the start does not count), that compete for a room, and how many of them hold a
            // straight of the card's length (Candidate is pure in the seed: it generates nothing).
            if (row != null)
                for (var cy = min; cy < max; cy++)
                for (var cx = min; cx < max; cx++)
                {
                    var id = new GridCoord(cx, cy);
                    if (Math.Max(Math.Abs(cx), Math.Abs(cy)) < 2 || !run.CompetesForTools(id)) continue;
                    row.competing++;
                    if (run.Candidate(id, card.cells) != null) row.fit++;
                }
            var chunkOwner = new Dictionary<GridCoord, int>();
            // A snapshot: the checks generate chunks (Get, ZoneTier), and a zone met now may plan a room of its own.
            foreach (var room in new List<FrontRoomsRunRoom>(run.Rooms))
            {
                if (!RoomInside(room, min, max)) continue;
                CheckRunRoom(cache, room, rules, card, row, fail);
                var tag = RunTag(room);
                // One zone's room per chunk.
                foreach (var ch in new[] { room.ChunkA, room.ChunkB })
                {
                    if (chunkOwner.TryGetValue(ch, out var other) && other != room.Id) fail(tag + ": shares chunk " + ch + " with Run! room " + other);
                    chunkOwner[ch] = room.Id;
                }
                // A fresh cache made the same way planned the same room.
                if (fresh != null)
                {
                    // Meet the host zone there (the zone of the entrance's outside cell; a corridor cell may lie in another zone).
                    fresh.ZoneOf(room.Outside);
                    var again = fresh.Run.RoomAt(room.Cells[0]);
                    if (again == null || again.Outside != room.Outside || again.Beyond != room.Beyond || again.Cells[0] != room.Cells[0] || again.LengthCells != room.LengthCells || again.Variant != room.Variant)
                        fail(tag + ": a fresh cache planned it differently");
                }
                // Module Relay entries the world hides from the Relay (within minEntryCells of the room or the cell beyond its goal
                // door: CR-5 and F4), counted.
                if (row != null)
                    for (var cy = min; cy < max; cy++)
                    for (var cx = min; cx < max; cx++)
                    {
                        var data = cache.Get(new GridCoord(cx, cy));
                        for (var r = 0; r < data.rooms.Length; r++)
                        {
                            var module = data.ModuleOf(r);
                            if (module == null || module.markers == null || !data.RoomIntact(r)) continue;
                            foreach (var mk in module.markers)
                            {
                                if (mk.kind != ModuleMarkerKind.RelayEntry) continue;
                                var ec = new GridCoord(data.Origin.x + data.rooms[r].x + (int)Math.Floor(mk.x / MapGrid.CellSize), data.Origin.y + data.rooms[r].y + (int)Math.Floor(mk.z / MapGrid.CellSize));
                                var near = Math.Abs(room.Beyond.x - ec.x) + Math.Abs(room.Beyond.y - ec.y) < rules.minEntryCells;
                                foreach (var c in room.Cells)
                                    if (!near && Math.Abs(c.x - ec.x) + Math.Abs(c.y - ec.y) < rules.minEntryCells) near = true;
                                if (near) row.entriesHidden++;
                            }
                        }
                    }
            }
        }

        static string RunTag(FrontRoomsRunRoom room) => "Run! room " + room.Id + " (" + room.VariantText + ", zone " + room.Zone + ", " + room.Cells[0] + "->" + room.GoalCell + ")";

        // Only rooms wholly inside the square: a room met at its edge is checked by its own cells as far as they reach.
        static bool RoomInside(FrontRoomsRunRoom room, int min, int max)
        {
            foreach (var c in room.Cells)
            {
                var ch = MapGrid.ChunkOf(c);
                if (ch.x < min || ch.x >= max || ch.y < min || ch.y >= max) return false;
            }
            return true;
        }

        /// <summary>
        /// One Run! room's structure (CR-1, CR-5, CR-6; 71 s.9 cards 1, 4, 5; F5 and F7 of 71 s.7.6): its length and twist (the
        /// card when <paramref name="card"/> is given, else 7 .. 14 by its own record), the straight, its cells, the entrance (the
        /// arch, or card 5's Run door, registered), the goal door, the zones it touches, both chunks, never the only route
        /// (F5: the far cell reaches the entrance's outside cell without the room, within 2 chunks round it), and no chain (F7:
        /// the goal door never opens into another Run! room, nor onto a dead end).
        /// </summary>
        static void CheckRunRoom(FrontRoomsMapCache cache, FrontRoomsRunRoom room, FrontRoomsRunRules rules, RunCard? card, MapRunSeedRow row, Action<string> fail)
        {
            const int n = MapGrid.ChunkCells;
            var run = cache.Run;
            var gen = cache.Generator;
            var tag = RunTag(room);
            if (row != null)
            {
                row.placed++;
                if (room.Theme == ZoneTheme.Office) row.office++; else row.level0++;
                if (room.BeyondHeight == ZoneHeight.Low) row.borderDoors++; else row.ownZoneDoors++;
                if (room.Zones.Length > 1) row.multiZone++;
                switch (room.LengthCells)
                {
                    case 7: row.cells7++; break;
                    case 9: row.cells9++; break;
                    case 12: row.cells12++; break;
                    case 14: row.cells14++; break;
                }
                if (room.EntranceDoor) row.doorEntrances++;
                if (room.Receding) row.receding++;
            }
            var zone = gen.Zone(room.Zone);
            // CR-1 and card 1: 7 .. 14 cells in a straight (the card's length per zone), one Standard zone of an allowed theme.
            if (room.LengthCells < FrontRoomsRunRooms.MinCells || room.LengthCells > FrontRoomsRunRooms.CapCells) fail(tag + ": " + room.LengthCells + " cells, outside 7 .. 14");
            if (card.HasValue && (room.LengthCells != card.Value.cells || room.Variant != card.Value.variant)) fail(tag + ": built " + room.VariantText + ", not the card " + card.Value);
            // Cards 4-6: one twist; a Run door or ramp room 9 cells; a receding room's seats 9 .. its reserved length, the end on its first.
            if ((room.Variant == RunVariant.Door || room.Variant == RunVariant.Ramp) && room.LengthCells != FrontRoomsRunRooms.Length) fail(tag + ": a " + room.Variant + " room of " + room.LengthCells + " cells");
            if (room.Variant == RunVariant.Recede)
            {
                var seats = room.SeatCells;
                if (!room.Receding || seats[0] != FrontRoomsRunRooms.RecedeFromCells || seats[seats.Length - 1] != room.LengthCells || (room.LengthCells != FrontRoomsRunRooms.LateCells && room.LengthCells != FrontRoomsRunRooms.DeepestCells))
                    fail(tag + ": receding seats " + (seats == null ? "none" : string.Join(",", seats)) + " for " + room.LengthCells + " cells");
                if (room.SeatIndex != 0 || room.OpenCells != FrontRoomsRunRooms.RecedeFromCells || Math.Abs(room.EntranceTrip - FrontRoomsRunRooms.RecedeTripCells * MapGrid.CellSize) > 1e-4f)
                    fail(tag + ": a new receding room's end at " + room.OpenCells + " cells, arming " + room.EntranceTrip + " m in (not 9 cells, the FD1 frame at 9 m)");
                // Card 4: the hidden length is shut (the head's line, the goal door before the last seat), the open part walks.
                if (!run.HeldShut(room.Cells[room.OpenCells - 1], room.Cells[room.OpenCells]) || !run.HeldShut(room.GoalCell, room.Beyond) || run.HeldShut(room.Cells[0], room.Cells[1]) || !run.IsHiddenCell(room.GoalCell) || run.IsHiddenCell(room.Cells[0]))
                    fail(tag + ": its hidden length is not shut past the end's line");
            }
            else if (room.SeatCells != null) fail(tag + ": seats on a room that does not recede");
            if (zone.height != ZoneHeight.Standard) fail(tag + ": zone is " + zone.height + ", not Standard");
            if (zone.theme == ZoneTheme.Office && !rules.office) fail(tag + ": an Office zone without call 5");
            if (Math.Abs(room.AxisX) + Math.Abs(room.AxisY) != 1) fail(tag + ": no axis");
            var crossings = 0;
            for (var k = 0; k < room.Cells.Length; k++)
            {
                var c = room.Cells[k];
                var cz = gen.ZoneOf(c);
                if (cz.height != ZoneHeight.Standard || cz.theme != zone.theme) fail(tag + ": cell " + c + " is " + cz.height + " " + cz.theme + ", not one space with its host zone");
                if (System.Array.IndexOf(room.Zones, cz.id) < 0) fail(tag + ": cell " + c + "'s zone missing from its zones");
                if (k > 0 && (c.x - room.Cells[k - 1].x != room.AxisX || c.y - room.Cells[k - 1].y != room.AxisY)) fail(tag + ": not a straight at " + c);
                if (room.IndexOf(c) != k) fail(tag + ": IndexOf(" + c + ") is " + room.IndexOf(c) + ", not " + k);
                if (k + 1 < room.Cells.Length)
                {
                    if (cache.Edge(c, room.Cells[k + 1]) != EdgeKind.Open) fail(tag + ": inside edge " + c + "-" + room.Cells[k + 1] + " is " + cache.Edge(c, room.Cells[k + 1]));
                    if (MapGrid.ChunkOf(c) != MapGrid.ChunkOf(room.Cells[k + 1])) crossings++;
                }
                var px = room.AxisY != 0 ? 1 : 0;
                var py = room.AxisX != 0 ? 1 : 0;
                var s1 = new GridCoord(c.x + px, c.y + py);
                var s2 = new GridCoord(c.x - px, c.y - py);
                if (cache.Edge(c, s1) != EdgeKind.Wall || cache.Edge(c, s2) != EdgeKind.Wall) fail(tag + ": a long side of " + c + " is not a wall");
                if (MapGrid.ChunkOf(s1) != MapGrid.ChunkOf(c) || MapGrid.ChunkOf(s2) != MapGrid.ChunkOf(c)) fail(tag + ": a long side of " + c + " is on a chunk border");
                var data = cache.Get(MapGrid.ChunkOf(c));
                var o = data.Origin;
                if (data.lamp[MapGrid.LocalIndex(c.x - o.x, c.y - o.y)] != ModuleLamp.Off) fail(tag + ": cell " + c + " has a map lamp (CR-4)");
                foreach (var rect in data.rooms)
                    if (rect.Contains(c.x - o.x, c.y - o.y)) fail(tag + ": a carved or module room covers " + c);
                if (run.HasStart && Math.Abs(c.x - run.StartDoor.x) + Math.Abs(c.y - run.StartDoor.y) < rules.minStartCells) fail(tag + ": cell " + c + " within " + rules.minStartCells + " cells of the start door");
            }
            if (crossings != 1) fail(tag + ": crosses " + crossings + " chunk borders, not 1");
            // RELAY 6: no zone's key lies in it (a zone's key is held by its site's chunk), and CR-5 for every zone it touches:
            // met at minTier+ (the host at least), never the arrival zone.
            foreach (var z in room.Zones)
            {
                var home = cache.Get(z);
                if (home.hasKey && room.Contains(home.keyCell)) fail(tag + ": zone " + z + "'s key lies in it at " + home.keyCell);
                if (cache.ZoneTier(z) < rules.minTier && !rules.force) fail(tag + ": touches zone " + z + " met at tier " + cache.ZoneTier(z));
                if (run.HasStart && z == run.ArrivalZone) fail(tag + ": touches the arrival zone " + z);
                if (run.MetAt(z, out _, out var afterSend) && !afterSend) fail(tag + ": touches zone " + z + " met before the first send");
            }
            // Both chunks hold a host cell (neither could be generated before the host zone was decided).
            foreach (var ch in new[] { room.ChunkA, room.ChunkB })
            {
                var holds = false;
                var co = MapGrid.ChunkOrigin(ch);
                for (var k = 0; k < n * n && !holds; k++) holds = gen.ZoneOf(new GridCoord(co.x + k % n, co.y + k / n)).id == room.Zone;
                if (!holds) fail(tag + ": chunk " + ch + " holds no cell of its host zone");
            }
            // The entrance on the zone's own side (the arch, or card 5's Run door), the goal door (CR-6), both off a chunk border.
            if (gen.ZoneOf(room.Outside).id != room.Zone) fail(tag + ": the entrance's outside cell " + room.Outside + " is in another zone");
            var entrance = cache.Edge(room.Outside, room.Cells[0]);
            if (room.EntranceDoor)
            {
                if (entrance != EdgeKind.Door || !run.IsEntranceDoorEdge(room.Outside, room.Cells[0]) || run.IsArchEdge(room.Outside, room.Cells[0]) || !run.IsRunDoorEdge(room.Outside, room.Cells[0]))
                    fail(tag + ": the entrance " + room.Outside + "-" + room.Cells[0] + " is " + entrance + ", not a registered Run door");
                if (Math.Abs(room.EntranceTrip - room.DoorTrip) > 1e-4f || Math.Abs(room.GoalTrip - room.DoorTrip) > 1e-4f) fail(tag + ": a Run door room arming " + room.EntranceTrip + " / " + room.GoalTrip + " m in, not " + room.DoorTrip);
            }
            else if (entrance != EdgeKind.Arch || !run.IsArchEdge(room.Outside, room.Cells[0]) || run.IsEntranceDoorEdge(room.Outside, room.Cells[0]))
                fail(tag + ": the entrance " + room.Outside + "-" + room.Cells[0] + " is " + entrance + ", not its arch");
            if (cache.Edge(room.GoalCell, room.Beyond) != EdgeKind.Door) fail(tag + ": the goal edge is " + cache.Edge(room.GoalCell, room.Beyond) + ", not a door");
            if (!run.IsGoalEdge(room.GoalCell, room.Beyond) || !run.IsRunDoorEdge(room.GoalCell, room.Beyond) || run.RunDoorRoom(room.GoalCell, room.Beyond) != room) fail(tag + ": the goal edge is not registered (M8)");
            if (MapGrid.ChunkOf(room.Outside) != MapGrid.ChunkOf(room.Cells[0]) || MapGrid.ChunkOf(room.Beyond) != MapGrid.ChunkOf(room.GoalCell)) fail(tag + ": an end on a chunk border");
            var far = gen.ZoneOf(room.Beyond);
            if (far.height == ZoneHeight.Tall) fail(tag + ": the goal door opens into a Tall zone");
            if (!rules.goalDoorForced && far.height != ZoneHeight.Low) fail(tag + ": the goal's far cell " + room.Beyond + " (" + far.height + ") is not Low without call 2 (CR-6b)");
            // CR-5: the tier, the arrival zone, the 15 cells.
            if (run.HasStart && Math.Abs(room.Outside.x - run.StartDoor.x) + Math.Abs(room.Outside.y - run.StartDoor.y) < rules.minStartCells) fail(tag + ": the entrance within " + rules.minStartCells + " cells of the start door");
            // Never the only way round: each of its chunks is one piece without the corridor (cells of the chunk, its own edges).
            foreach (var ch in new[] { room.ChunkA, room.ChunkB })
            {
                var data = cache.Get(ch);
                var o = data.Origin;
                var seen = new bool[n * n];
                var queue = new Queue<int>();
                var first = -1;
                var free = 0;
                for (var k = 0; k < n * n; k++)
                {
                    if (room.Contains(new GridCoord(o.x + k % n, o.y + k / n))) continue;
                    free++;
                    if (first < 0) first = k;
                }
                if (first < 0) continue;
                seen[first] = true;
                queue.Enqueue(first);
                var reached = 0;
                while (queue.Count > 0)
                {
                    var k = queue.Dequeue();
                    reached++;
                    int i = k % n, j = k / n;
                    void Step(int to, EdgeKind kind)
                    {
                        if (seen[to] || !MapGrid.Passable(kind) || room.Contains(new GridCoord(o.x + to % n, o.y + to / n))) return;
                        seen[to] = true;
                        queue.Enqueue(to);
                    }
                    if (i < n - 1) Step(k + 1, data.east[k]);
                    if (i > 0) Step(k - 1, data.east[k - 1]);
                    if (j < n - 1) Step(k + n, data.north[k]);
                    if (j > 0) Step(k - n, data.north[k - n]);
                }
                if (reached != free) fail(tag + ": chunk " + ch + " falls apart without the corridor (" + reached + " of " + free + " cells)");
            }
            // F5 (71 s.7.6): never the only route: the goal door's far cell reaches the entrance's outside cell without the room.
            if (!ReachesWithout(cache, room, room.Beyond, room.Outside)) fail(tag + ": F5: the far cell " + room.Beyond + " reaches the entrance " + room.Outside + " only through the room");
            // F7: no chain: the goal door never opens into another Run! room, nor onto a dead end (the far cell has a way on).
            var beyondRoom = run.RoomAt(room.Beyond);
            if (beyondRoom != null) fail(tag + ": F7: its goal door opens into Run! room " + beyondRoom.Id);
            var ways = 0;
            foreach (var step in new[] { new GridCoord(1, 0), new GridCoord(-1, 0), new GridCoord(0, 1), new GridCoord(0, -1) })
            {
                var next = new GridCoord(room.Beyond.x + step.x, room.Beyond.y + step.y);
                if (next != room.GoalCell && MapGrid.Passable(cache.Edge(room.Beyond, next))) ways++;
            }
            if (ways == 0) fail(tag + ": F7: its goal door opens onto a dead end at " + room.Beyond);
        }

        // F5: a walk from one cell to another over passable edges, never through the room, within the 2 chunks round its two chunks.
        static bool ReachesWithout(FrontRoomsMapCache cache, FrontRoomsRunRoom room, GridCoord from, GridCoord to)
        {
            int x0 = Math.Min(room.ChunkA.x, room.ChunkB.x) - 2, x1 = Math.Max(room.ChunkA.x, room.ChunkB.x) + 2;
            int y0 = Math.Min(room.ChunkA.y, room.ChunkB.y) - 2, y1 = Math.Max(room.ChunkA.y, room.ChunkB.y) + 2;
            var seen = new HashSet<GridCoord> { from };
            var queue = new Queue<GridCoord>();
            queue.Enqueue(from);
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                if (c == to) return true;
                foreach (var step in new[] { new GridCoord(1, 0), new GridCoord(-1, 0), new GridCoord(0, 1), new GridCoord(0, -1) })
                {
                    var next = new GridCoord(c.x + step.x, c.y + step.y);
                    var ch = MapGrid.ChunkOf(next);
                    if (ch.x < x0 || ch.x > x1 || ch.y < y0 || ch.y > y1 || seen.Contains(next) || room.Contains(next)) continue;
                    if (!MapGrid.Passable(cache.Edge(c, next))) continue;
                    seen.Add(next);
                    queue.Enqueue(next);
                }
            }
            return false;
        }

        // ---------- 9b. The Run! deck and the calm rule (71 s.9, cards 2 and 3) ----------

        /// <summary>The floor tiers the deck check plays by default (non-decreasing, as the tier carries down): T2 on LEVEL 0 and B1, T3 on B2, T4 on B3, T5 on B4 and B5.</summary>
        public static readonly int[] DeckTiers = { 2, 2, 3, 4, 5, 5 };

        /// <summary>
        /// Cards 2 and 3 over <paramref name="seedCount"/> runs (MapRunDeckReport.scope): per run one FrontRoomsRunDeck through
        /// floors 1 .. tiers.Length, each floor a fresh cache on its floor seed played the game's way (the deck, the calm rule;
        /// foil and alarm doors forced on the one budget), every placed room met at once. <paramref name="rules"/> are the Run!
        /// rules (their twist switches as given: the check turns doorEntrance and recede on to see them dealt, ramps stay as given).
        /// </summary>
        public static MapRunDeckReport RunRunDeck(MapSettings settings, int firstSeed, int seedCount, int radius, IReadOnlyList<RoomModuleData> modules, FrontRoomsRunRules rules, int[] tiers = null)
        {
            tiers = tiers ?? DeckTiers;
            rules = (rules ?? new FrontRoomsRunRules()).Clone();
            rules.deck = true;
            rules.force = false;
            rules.forceCard = RunCard.Nothing;
            var report = new MapRunDeckReport
            {
                seedCount = seedCount, floors = tiers.Length, timestampUtc = DateTime.UtcNow.ToString("o"), doorEntrance = rules.doorEntrance, recede = rules.recede, ramps = rules.ramps,
                tiers = string.Join(" ", Array.ConvertAll(tiers, t => "T" + t)),
            };
            var built = new SortedDictionary<string, int>(StringComparer.Ordinal);
            for (var i = 0; i < seedCount; i++)
            {
                var runSeed = unchecked(firstSeed + i * 7919);
                var row = new MapRunDeckSeedRow { seed = runSeed };
                void Fail(string message) { if (row.errors.Count < 12) row.errors.Add(message); }
                var deck = PlayDeckRun(settings, runSeed, radius, modules, rules, tiers, row, Fail);
                // A replay of the same run deals the same (the deck is pure in the run's seed and the floors' order).
                var replay = PlayDeckRun(settings, runSeed, radius, modules, rules, tiers, null, null);
                if (deck.History.Count != replay.History.Count) Fail("a replay dealt " + replay.History.Count + " floors, not " + deck.History.Count);
                else
                    for (var k = 0; k < deck.History.Count; k++)
                        if (deck.History[k].ToString() != replay.History[k].ToString()) Fail("a replay dealt floor " + deck.History[k].Floor + " as " + replay.History[k] + ", not " + deck.History[k]);
                // Card 3's other order: the west half walked (its devices decided) before the floor's zones are met.
                PlayDeckRun(settings, runSeed, radius, modules, rules, tiers, row, m => Fail("devices first: " + m), true);
                foreach (var deal in deck.History)
                {
                    row.deals.Add(deal.ToString());
                    if (deal.Built.IsNothing) { if (deal.Deck.IsNothing) row.nothing++; continue; }
                    var key = deal.Built + " at T" + deal.Tier;
                    built.TryGetValue(key, out var count);
                    built[key] = count + 1;
                }
                row.passed = row.errors.Count == 0;
                report.seeds.Add(row);
                if (row.passed) report.passed++; else report.failed++;
                report.rooms += row.rooms;
                report.nothing += row.nothing;
                report.twists += row.twists;
                report.calmExcluded += row.calmExcluded;
                report.firstRooms += row.firstRooms;
                report.calmRefused += row.calmRefused;
                report.hostDevicesPlain += row.hostDevicesPlain;
            }
            foreach (var pair in built) report.builtCounts.Add(pair.Key + ": " + pair.Value);
            return report;
        }

        // Every device the planners have decided so far: its edge and its two cells (a foiled window's Tall cell and the other
        // side; an alarm door's near and far cells), read from their decided plans.
        static Dictionary<long, GridCoord[]> DecidedDevices(FrontRoomsFoil foil, FrontRoomsAlarmDoors doors)
        {
            var map = new Dictionary<long, GridCoord[]>();
            foreach (var z in foil.Decided) if (z.HasFoil) map[z.FoiledWindow.edge] = new[] { z.FoiledWindow.tall, z.FoiledWindow.other };
            foreach (var z in doors.Decided) if (z.HasAlarm) map[z.AlarmedDoor.edge] = new[] { z.AlarmedDoor.near, z.AlarmedDoor.far };
            return map;
        }

        // One run of floors for the deck check: its deck, every floor's rows into row (null: a quiet replay). devicesFirst: card
        // 3's other order (the zones of the square's west half met at tier 1 and every foil and alarm-door plan of theirs decided
        // before the floor's zones are met at its tier), counted in row's firstRooms, calmRefused and hostDevicesPlain.
        static FrontRoomsRunDeck PlayDeckRun(MapSettings settings, int runSeed, int radius, IReadOnlyList<RoomModuleData> modules, FrontRoomsRunRules rules, int[] tiers, MapRunDeckSeedRow row, Action<string> fail, bool devicesFirst = false)
        {
            var deck = new FrontRoomsRunDeck(runSeed);
            int min = -radius, max = radius;
            for (var f = 0; f < tiers.Length; f++)
            {
                var floor = f + 1;
                var s = (settings ?? new MapSettings()).Clone();
                s.seed = floor == 1 ? runSeed : FrontRoomsExitPlanner.FloorSeed(runSeed, floor, 0);
                var cache = new FrontRoomsMapCache(s, modules);
                var run = new FrontRoomsRunRooms(rules) { Floor = floor, Deck = deck };
                cache.SetRunRooms(run);
                var foil = new FrontRoomsFoil(new FrontRoomsFoilRules { force = true });
                cache.SetFoil(foil);
                var doors = new FrontRoomsAlarmDoors(new FrontRoomsAlarmDoorRules { force = true });
                cache.SetAlarmDoors(doors);
                var start = new GridCoord(0, 0);
                run.SetStart(start);
                foil.SetStart(start);
                doors.SetStart(start);
                cache.Tier = 1;
                cache.Get(MapGrid.ChunkOf(start));
                run.Ready = run.Open = foil.Ready = doors.Ready = true;
                // Card 3: the devices decided when the floor's room is planned (its refusal reads the planners; checked below).
                Dictionary<long, GridCoord[]> atPlan = null;
                run.Planned += planned =>
                {
                    atPlan = DecidedDevices(foil, doors);
                    if (fail == null) return;
                    var into = cache.Generator.ZoneOf(planned.Beyond).id;
                    foreach (var pair in atPlan)
                        if (cache.Generator.ZoneOf(pair.Value[0]).id == into || cache.Generator.ZoneOf(pair.Value[1]).id == into)
                        {
                            fail("floor " + floor + ": Run! room " + planned.Id + "'s goal door opens into zone " + into + ", where a device " + pair.Value[0] + "-" + pair.Value[1] + " was decided before it (not refused)");
                            break;
                        }
                };
                if (devicesFirst)
                {
                    // The square's west half walked first, at tier 1 (no Run! there): every plan of its zones decided now.
                    var walked = new HashSet<GridCoord>();
                    for (var y = min * MapGrid.ChunkCells; y < max * MapGrid.ChunkCells; y++)
                    for (var x = min * MapGrid.ChunkCells; x < 0; x++)
                    {
                        var id = cache.Generator.ZoneOf(new GridCoord(x, y)).id;
                        if (!walked.Add(id)) continue;
                        if (cache.Generator.Zone(id).height == ZoneHeight.Tall) foil.Plan(id); else doors.Plan(id);
                    }
                }
                cache.Tier = tiers[f];
                for (var cy = min; cy < max; cy++)
                for (var cx = min; cx < max; cx++)
                    cache.Get(new GridCoord(cx, cy));
                // The player meets every room placed (the run flag: plain first).
                foreach (var room in run.Rooms) run.MarkMet(room);
                if (row == null) continue;
                row.calmRefused += run.CalmRefused;
                if (!devicesFirst && run.CalmRefused > 0) fail("floor " + floor + ": " + run.CalmRefused + " zones refused for a device beyond the goal door, with no device decided before the rooms");
                var deal = deck.DealOn(floor);
                if (run.Placed > 1) fail("floor " + floor + ": " + run.Placed + " Run! rooms (one a floor)");
                if (run.Placed == 1 && (deal == null || deal.Built.IsNothing || deal.RoomId != run.Rooms[0].Id)) fail("floor " + floor + ": a room with no deal");
                if (deal == null) continue;
                if (deal.Floor != floor || deal.Tier < tiers[f]) fail("floor " + floor + ": dealt as floor " + deal.Floor + " at T" + deal.Tier);
                // The deal's rules (FrontRoomsRunDeck.Play): plain first, the gates, no repeats, the lengths by tier.
                var before = 0;
                foreach (var d in deck.History) if (d.Floor < floor && !d.Built.IsNothing) before++;
                var p = deal.Played;
                if (before == 0 && !p.IsNothing && p != RunCard.Plain(FrontRoomsRunRooms.FirstCells)) fail("floor " + floor + ": the run's first Run! played " + p + ", not plain 7");
                if (p.IsTwist && before == 0) fail("floor " + floor + ": a twist before a plain Run! was met");
                var above = deck.DealOn(floor - 1);
                if (p.IsTwist && above != null && !above.Built.IsNothing && above.Built.variant == p.variant) fail("floor " + floor + ": " + p + " after " + above.Built + " on the floor above");
                if (p.variant == RunVariant.Recede && !p.IsNothing && (floor < FrontRoomsRunDeck.RecedeMinFloor || (p.cells > FrontRoomsRunRooms.LateCells && floor < FrontRoomsRunDeck.RecedeLongMinFloor) || !rules.recede))
                    fail("floor " + floor + ": " + p + " on floor " + floor);
                if (p.variant == RunVariant.Door && !rules.doorEntrance) fail("floor " + floor + ": a Run door with the switch off");
                if (p.variant == RunVariant.Ramp && !rules.ramps) fail("floor " + floor + ": a ramp with the switch off");
                // The deck's card is one of its tier's; a plain card is the tier's length (card 1) and is played as it is once a Run! was met.
                var inDeck = false;
                foreach (var c in FrontRoomsRunDeck.DeckAt(deal.Tier)) inDeck |= c == deal.Deck;
                if (!inDeck) fail("floor " + floor + ": " + deal.Deck + " is not a card of T" + deal.Tier + "'s deck");
                if (!deal.Deck.IsNothing && deal.Deck.variant == RunVariant.Plain && deal.Deck.cells != FrontRoomsRunRooms.PlainCellsAt(deal.Tier))
                    fail("floor " + floor + ": plain " + deal.Deck.cells + " in T" + deal.Tier + "'s deck (the table: " + FrontRoomsRunRooms.PlainCellsAt(deal.Tier) + ")");
                if (before > 0 && !deal.Deck.IsNothing && deal.Deck.variant == RunVariant.Plain && p != deal.Deck) fail("floor " + floor + ": the deck's " + deal.Deck + " played as " + p);
                if (deal.Deck.IsNothing != p.IsNothing) fail("floor " + floor + ": the deck's " + deal.Deck + " played as " + p);
                // Decision 3: once a Run! was met, a plain card (the deck's, or a twist that cannot be dealt) is the tier's length.
                if (before > 0 && !p.IsNothing && p.variant == RunVariant.Plain && p.cells != FrontRoomsRunRooms.PlainCellsAt(deal.Tier))
                    fail("floor " + floor + ": played " + p + " at T" + deal.Tier + ", not the tier's plain " + FrontRoomsRunRooms.PlainCellsAt(deal.Tier) + (deal.Why != null ? " (" + deal.Why + ")" : ""));
                var fits = FrontRoomsRunRooms.FitOrder(p);
                if (!deal.Built.IsNothing && Array.IndexOf(fits, deal.Built) < 0) fail("floor " + floor + ": built " + deal.Built + " for " + p);
                if (deal.Built.IsNothing && !deal.Played.IsNothing) fail("floor " + floor + ": played " + p + " and built nothing");
                // The teaching room is built at 7 or not at all (it waits); no plain room under 12 cells at T4-T5.
                if (before == 0 && !deal.Built.IsNothing && deal.Built != RunCard.Plain(FrontRoomsRunRooms.FirstCells)) fail("floor " + floor + ": the run's first Run! built " + deal.Built + ", not plain 7");
                if (deal.Tier >= 4 && !deal.Built.IsNothing && deal.Built.variant == RunVariant.Plain && deal.Built.cells < FrontRoomsRunRooms.LateCells && before > 0)
                    fail("floor " + floor + ": a " + deal.Built + " at T" + deal.Tier + " (decision 3: 12 from T4, 14 at T5 where it fits)");
                if (run.Placed == 0) continue;
                var r = run.Rooms[0];
                if (devicesFirst) row.firstRooms++;
                else
                {
                    row.rooms++;
                    if (r.Variant != RunVariant.Plain) row.twists++;
                }
                if (r.Floor != floor || r.Card != p || r.Variant != deal.Built.variant || r.LengthCells != deal.Built.cells) fail("floor " + floor + ": the room's record (" + r.VariantText + ", floor " + r.Floor + ", card " + r.Card + ") is not its deal's");
                var shadowRow = new MapRunSeedRow();
                if (RoomInside(r, min, max)) CheckRunRoom(cache, r, rules, null, shadowRow, fail);
                // Card 3: the calm zones (the host's and the far cell's). Every plan of the square is decided now; a device in them
                // was either decided before the room (then only on the host zone's border: built later, the world reads it plain;
                // one into the zone beyond would have refused the room) or after it, which the planners' own exclusion forbids.
                var beyondZone = cache.Generator.ZoneOf(r.Beyond).id;
                var calm = new HashSet<GridCoord> { r.Zone, beyondZone };
                foreach (var z in calm) if (!run.IsCalmZone(z)) fail("floor " + floor + ": zone " + z + " is not calm");
                var zones = new HashSet<GridCoord>();
                for (var y = min * MapGrid.ChunkCells; y < max * MapGrid.ChunkCells; y++)
                for (var x = min * MapGrid.ChunkCells; x < max * MapGrid.ChunkCells; x++)
                    zones.Add(cache.Generator.ZoneOf(new GridCoord(x, y)).id);
                foreach (var z in zones)
                {
                    if (cache.Generator.Zone(z).height == ZoneHeight.Tall)
                    {
                        var plan = foil.Plan(z);
                        if (plan == null || devicesFirst) continue;
                        foreach (var w in plan.Windows) if (!w.plain && run.CalmTouches(w.tall, w.other)) row.calmExcluded++;
                    }
                    else
                    {
                        var plan = doors.Plan(z);
                        if (plan == null || devicesFirst) continue;
                        foreach (var d in plan.Doors) if (d.owned && !d.plain && run.CalmTouches(d.near, d.far)) row.calmExcluded++;
                    }
                }
                foreach (var pair in DecidedDevices(foil, doors))
                {
                    GridCoord a = pair.Value[0], b = pair.Value[1];
                    if (!run.CalmTouches(a, b)) continue;
                    var what = "a device " + a + "-" + b + " in Run! room " + r.Id + "'s calm zones";
                    if (atPlan == null || !atPlan.ContainsKey(pair.Key)) fail("floor " + floor + ": " + what + ", decided after the room");
                    else if (cache.Generator.ZoneOf(a).id == beyondZone || cache.Generator.ZoneOf(b).id == beyondZone) fail("floor " + floor + ": " + what + ", decided before it, beyond its goal door");
                    else row.hostDevicesPlain++;
                }
            }
            return deck;
        }

        // ---------- 10. C13's foiled windows, the trigger budget, the auto Relay entries and cover (Relay v2 step V7a) ----------

        /// <summary>RELAY 12's cover target: in tall and large rooms every cell lies within this many metres of walking from cover.</summary>
        public const float CoverTargetMetres = 15f;
        /// <summary>RELAY 8's calm sight range: a cell beyond it is out of sight.</summary>
        public const float CoverSightMetres = 12f;
        /// <summary>The cover walk's cap (a cell with no cover within it reads this).</summary>
        public const float CoverCapMetres = 60f;

        /// <summary>
        /// C13 (V7a) over the seeds: maps made as FoilCache makes them (foil on with <paramref name="rules"/>, Run! rooms too with
        /// <paramref name="runRules"/>: one trigger budget), every other zone met at <paramref name="tier"/>. Each seed's foil rows,
        /// the ledger, the Relay entries and the cover measure (MapFoilReport.scope). Data only; the map rules are ValidateSeed's
        /// (foil changes no edge).
        /// </summary>
        public static MapFoilReport RunFoil(MapSettings settings, int firstSeed, int seedCount, int radius, IReadOnlyList<RoomModuleData> modules, FrontRoomsFoilRules rules, FrontRoomsRunRules runRules, int tier)
        {
            rules = rules ?? new FrontRoomsFoilRules();
            var report = new MapFoilReport { radiusChunks = radius, seedCount = seedCount, tier = tier, force = rules.force, runRooms = runRules != null, timestampUtc = DateTime.UtcNow.ToString("o") };
            var worst = new List<MapFoilZoneCover>();
            float tallSum = 0f, otherSum = 0f;
            int tallCells = 0, otherCells = 0;
            for (var i = 0; i < seedCount; i++)
            {
                var s = (settings ?? new MapSettings()).Clone();
                s.seed = unchecked(firstSeed + i * 7919);
                var row = new MapFoilSeedRow { seed = s.seed };
                ValidateFoilSeed(s, radius, modules, rules, runRules, tier, row, worst, ref tallSum, ref tallCells, ref otherSum, ref otherCells);
                row.passed = row.errors.Count == 0;
                report.seeds.Add(row);
                if (row.passed) report.passed++; else report.failed++;
                report.tallZones += row.tallZones; report.windows += row.windows; report.candidates += row.candidates; report.interior += row.interior;
                report.plain += row.plain; report.foiled += row.foiled; report.slots += row.slots; report.noSlot += row.noSlot; report.noWindow += row.noWindow;
                report.arrival += row.arrival; report.taken += row.taken; report.excluded += row.excluded; report.ledgerFoil += row.ledgerFoil; report.ledgerRun += row.ledgerRun;
                report.runRoomsPlaced += row.runRooms; report.entryZones += row.entryZones; report.entries += row.entries; report.deadEnds += row.deadEnds;
                report.corridorEnds += row.corridorEnds; report.otherCells += row.otherCells; report.smallZones += row.smallZones; report.coverTallOver += row.coverTallOver;
                report.coverTallMax = Math.Max(report.coverTallMax, row.coverTallMax);
                report.coverOtherMax = Math.Max(report.coverOtherMax, row.coverOtherMax);
            }
            report.coverTallMean = tallCells > 0 ? tallSum / tallCells : 0f;
            report.coverOtherMean = otherCells > 0 ? otherSum / otherCells : 0f;
            worst.Sort((a, b) => b.max.CompareTo(a.max));
            for (var k = 0; k < worst.Count && k < 40; k++) report.coverWorst.Add(worst[k]);
            return report;
        }

        /// <summary>
        /// A cache with foil (and Run! rooms with <paramref name="runRules"/>) as the check plays them: the start door at cell
        /// (0, 0), its chunk generated at tier 1 before the map is ready (the first zone), then the map ready, the first send
        /// gone out, and every later zone met at <paramref name="tier"/>. The Run! planner attaches first, so the one budget
        /// takes its table (the profile's; foil's is the same). The same steps on any cache give the same foil.
        /// </summary>
        public static FrontRoomsMapCache FoilCache(MapSettings settings, IReadOnlyList<RoomModuleData> modules, FrontRoomsFoilRules rules, FrontRoomsRunRules runRules, int tier)
        {
            var cache = new FrontRoomsMapCache(settings, modules);
            FrontRoomsRunRooms run = null;
            if (runRules != null)
            {
                // Per zone and without the calm rule (ChecksRules): the foil plans stay pure for the fresh-cache check.
                run = new FrontRoomsRunRooms(ChecksRules(runRules));
                cache.SetRunRooms(run);
            }
            var foil = new FrontRoomsFoil(rules);
            cache.SetFoil(foil);
            var start = new GridCoord(0, 0);
            run?.SetStart(start);
            foil.SetStart(start);
            cache.Tier = 1;
            cache.Get(MapGrid.ChunkOf(start));
            if (run != null) { run.Ready = true; run.Open = true; }
            foil.Ready = true;
            cache.Tier = Math.Max(1, tier);
            return cache;
        }

        static void ValidateFoilSeed(MapSettings settings, int radius, IReadOnlyList<RoomModuleData> modules, FrontRoomsFoilRules rules, FrontRoomsRunRules runRules, int tier,
            MapFoilSeedRow row, List<MapFoilZoneCover> worst, ref float tallSum, ref int tallCells, ref float otherSum, ref int otherCells)
        {
            void Fail(string message) { if (row.errors.Count < 12) row.errors.Add(message); }
            const int n = MapGrid.ChunkCells;
            int min = -radius, max = radius;
            var cache = FoilCache(settings, modules, rules, runRules, tier);
            var gen = cache.Generator;
            var foil = cache.Foil;
            var budget = cache.Budget;
            for (var cy = min; cy < max; cy++)
            for (var cx = min; cx < max; cx++)
                cache.Get(new GridCoord(cx, cy));
            // Zones wholly inside the square: every cell of a zone lies in the 5 x 5 chunks round its site's chunk.
            var inside = new List<GridCoord>();
            var seenZones = new HashSet<GridCoord>();
            for (var y = min * n; y < max * n; y++)
            for (var x = min * n; x < max * n; x++)
            {
                var id = gen.ZoneOf(new GridCoord(x, y)).id;
                if (!seenZones.Add(id)) continue;
                if (id.x - 2 >= min && id.x + 2 < max && id.y - 2 >= min && id.y + 2 < max) inside.Add(id);
            }
            inside.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            var start = new GridCoord(0, 0);
            var arrival = gen.ZoneOf(start).id;

            // 10.1 Per Tall zone: its windows as built against the planner's candidates, the plain rule, one foil within the budget.
            var foiledEdges = new List<FoiledEdge>();
            foreach (var id in inside)
            {
                if (gen.Zone(id).height != ZoneHeight.Tall) continue;
                row.tallZones++;
                var plan = foil.Plan(id);
                var tag = "Tall zone " + id;
                if (plan == null) { Fail(tag + ": no foil plan after Ready"); continue; }
                if (plan.Tier != cache.ZoneTier(id)) Fail(tag + ": planned at tier " + plan.Tier + ", stored tier " + cache.ZoneTier(id));
                if (budget.HasSlot(id, plan.Tier)) row.slots++;
                var planEdges = new HashSet<long>();
                foreach (var w in plan.Windows) planEdges.Add(w.edge);
                var borderTotal = new Dictionary<GridCoord, int>();
                var borderPlain = new Dictionary<GridCoord, int>();
                int total = 0, border = 0, foiledHere = 0;
                for (var y = (id.y - 2) * n; y < (id.y + 3) * n; y++)
                for (var x = (id.x - 2) * n; x < (id.x + 3) * n; x++)
                {
                    var cell = new GridCoord(x, y);
                    if (gen.ZoneOf(cell).id != id) continue;
                    for (var k = 0; k < 4; k++)
                    {
                        var next = k == 0 ? new GridCoord(x + 1, y) : k == 1 ? new GridCoord(x, y + 1) : k == 2 ? new GridCoord(x - 1, y) : new GridCoord(x, y - 1);
                        var other = gen.ZoneOf(next);
                        if (other.height == ZoneHeight.Tall || cache.Edge(cell, next) != EdgeKind.Window) continue;
                        total++;
                        var onBorder = MapGrid.ChunkOf(cell) != MapGrid.ChunkOf(next);
                        if (onBorder)
                        {
                            border++;
                            if (!planEdges.Contains(MapGrid.EdgeId(cell, next))) Fail(tag + ": chunk-border window " + cell + "-" + next + " is not a candidate");
                        }
                        var foiled = foil.IsFoiled(cell, next);
                        if (foiled)
                        {
                            foiledHere++;
                            foiledEdges.Add(new FoiledEdge { a = cell, b = next, zone = id });
                            if (!onBorder) Fail(tag + ": an interior window " + cell + "-" + next + " is foiled");
                        }
                        borderTotal.TryGetValue(other.id, out var bt);
                        borderTotal[other.id] = bt + 1;
                        borderPlain.TryGetValue(other.id, out var bp);
                        borderPlain[other.id] = bp + (foiled ? 0 : 1);
                    }
                }
                if (border != plan.Windows.Length) Fail(tag + ": " + plan.Windows.Length + " candidates, " + border + " chunk-border windows built");
                foreach (var w in plan.Windows)
                    if (cache.Edge(w.tall, w.other) != EdgeKind.Window) Fail(tag + ": candidate " + w.tall + "-" + w.other + " is " + cache.Edge(w.tall, w.other) + ", not a window");
                // RELAY 10: a plain window on every border with windows; min(2, count) plain in the zone.
                foreach (var pair in borderTotal)
                    if (pair.Value > 0 && borderPlain[pair.Key] < 1) Fail(tag + ": its border with zone " + pair.Key + " has " + pair.Value + " windows and none plain");
                if (total - foiledHere < Math.Min(rules.minPlainPerZone, total)) Fail(tag + ": " + (total - foiledHere) + " plain of " + total + " windows, under min(" + rules.minPlainPerZone + ", count)");
                // One foil, only with the zone's slot (or forced), never a kept-plain or excluded window; the ledger holds it.
                if (foiledHere > 1) Fail(tag + ": " + foiledHere + " foiled windows");
                if (foiledHere != (plan.HasFoil ? 1 : 0)) Fail(tag + ": " + foiledHere + " foiled windows built, the plan says " + (plan.HasFoil ? 1 : 0));
                if (plan.HasFoil)
                {
                    var w = plan.FoiledWindow;
                    if (!rules.force && !budget.HasSlot(id, plan.Tier)) Fail(tag + ": foiled without its budget slot (roll " + budget.Roll(id) + ", chance " + budget.ChanceAt(plan.Tier) + ")");
                    if (budget.HolderOf(id) != TriggerDeviceKind.Foil) Fail(tag + ": foiled, but the ledger holds " + budget.HolderOf(id));
                    if (w.plain || w.excluded) Fail(tag + ": the foiled window " + w.tall + "-" + w.other + " is " + (w.plain ? "a kept-plain one" : "excluded"));
                    if (id == arrival || w.neighbour == arrival) Fail(tag + ": a device in the first zone");
                    if (Math.Abs(w.tall.x - start.x) + Math.Abs(w.tall.y - start.y) < rules.minStartCells || Math.Abs(w.other.x - start.x) + Math.Abs(w.other.y - start.y) < rules.minStartCells)
                        Fail(tag + ": the foiled window " + w.tall + "-" + w.other + " within " + rules.minStartCells + " cells of the start door");
                    // The route (MAP_ROUTE_SPEC: glass never) and the player's walk never through it: glass is not walkable.
                    if (FrontRoomsExitPlanner.Walkable(cache.Edge(w.tall, w.other))) Fail(tag + ": the foiled window is walkable");
                }
                else if (plan.Miss == FoilMiss.NoSlot && (rules.force || budget.HasSlot(id, plan.Tier))) Fail(tag + ": no slot, but its roll passes");
                row.windows += total;
                row.candidates += plan.Windows.Length;
                row.interior += total - border;
                row.plain += total - foiledHere;
                row.foiled += foiledHere;
                foreach (var w in plan.Windows) if (w.excluded) row.excluded++;
                switch (plan.Miss)
                {
                    case FoilMiss.NoSlot: row.noSlot++; break;
                    case FoilMiss.NoWindow: row.noWindow++; break;
                    case FoilMiss.Arrival: row.arrival++; break;
                    case FoilMiss.Taken: row.taken++; break;
                }
            }

            // 10.2 The ledger: one kind per zone; foil only in Tall zones it foiled, Run! only in zones holding a room; counts agree.
            foreach (var pair in budget.Ledger)
            {
                if (pair.Value == TriggerDeviceKind.Foil)
                {
                    var plan = foil.Plan(pair.Key);
                    if (plan == null || !plan.HasFoil) Fail("ledger: zone " + pair.Key + " held by Foil with no foiled window");
                }
                else if (pair.Value == TriggerDeviceKind.Run)
                {
                    if (cache.Run == null || !cache.Run.Decided(pair.Key, out var room) || room == null) Fail("ledger: zone " + pair.Key + " held by Run with no Run! room");
                }
            }
            row.ledgerFoil = budget.Count(TriggerDeviceKind.Foil);
            row.ledgerRun = budget.Count(TriggerDeviceKind.Run);
            row.runRooms = cache.Run != null ? cache.Run.Placed : 0;
            if (row.ledgerFoil != foil.FoiledCount) Fail("ledger: " + row.ledgerFoil + " Foil slots, " + foil.FoiledCount + " foiled windows");
            if (cache.Run != null && row.ledgerRun != cache.Run.Placed) Fail("ledger: " + row.ledgerRun + " Run slots, " + cache.Run.Placed + " Run! rooms");
            if (budget.Devices != row.ledgerFoil + row.ledgerRun) Fail("ledger: " + budget.Devices + " devices, " + (row.ledgerFoil + row.ledgerRun) + " by kind");
            if (cache.Run != null)
                foreach (var room in cache.Run.Rooms)
                    if (budget.HolderOf(room.Zone) != TriggerDeviceKind.Run) Fail("ledger: Run! room " + room.Id + "'s zone " + room.Zone + " is held by " + budget.HolderOf(room.Zone));

            // 10.3 Determinism: a fresh cache made the same way decides every Tall zone the same.
            var fresh = FoilCache(settings, modules, rules, runRules, tier);
            foreach (var id in inside)
            {
                if (gen.Zone(id).height != ZoneHeight.Tall) continue;
                var a = foil.Plan(id);
                var b = fresh.Foil.Plan(id);
                if (a == null || b == null || a.Miss != b.Miss || a.HasFoil != b.HasFoil || (a.HasFoil && a.FoiledWindow.edge != b.FoiledWindow.edge) || a.Windows.Length != b.Windows.Length)
                    Fail("Tall zone " + id + ": a fresh cache decided it differently");
            }

            // 10.4 Revisit shifts: the foiled window's chunks shifted (twice) keep it a window and foiled, and the candidates as they were.
            foreach (var fe in foiledEdges)
            {
                GridCoord a = fe.a, b = fe.b, zone = fe.zone;
                for (var shift = 0; shift < 2; shift++)
                {
                    cache.Shift(MapGrid.ChunkOf(a));
                    if (MapGrid.ChunkOf(b) != MapGrid.ChunkOf(a)) cache.Shift(MapGrid.ChunkOf(b));
                    cache.Get(MapGrid.ChunkOf(a));
                    cache.Get(MapGrid.ChunkOf(b));
                }
                if (cache.Edge(a, b) != EdgeKind.Window || !foil.IsFoiled(a, b)) Fail("Tall zone " + zone + ": after revisit shifts the foiled edge " + a + "-" + b + " is " + cache.Edge(a, b) + (foil.IsFoiled(a, b) ? "" : ", not foiled"));
                if (foil.Candidates(zone).Length != foil.Plan(zone).Windows.Length) Fail("Tall zone " + zone + ": its candidates changed with a revisit shift");
            }

            // 10.5 Relay entries: two 10 cells apart in every zone wholly inside whose cells allow it.
            var chunks = new List<MapChunk>();
            for (var cy = min; cy < max; cy++)
            for (var cx = min; cx < max; cx++)
                chunks.Add(cache.Get(new GridCoord(cx, cy)));
            var entries = new List<FrontRoomsAutoEntries.Entry>();
            FrontRoomsAutoEntries.Collect(gen, chunks, null, null, entries);
            var byZone = new Dictionary<GridCoord, List<GridCoord>>();
            foreach (var e in entries)
            {
                if (!byZone.TryGetValue(e.zone, out var list)) byZone[e.zone] = list = new List<GridCoord>();
                list.Add(e.cell);
                var tall = gen.Zone(e.zone).height == ZoneHeight.Tall;
                if (e.tag != (tall ? FrontRoomsAutoEntries.DoorwayTag : FrontRoomsAutoEntries.VentTag)) Fail("entry " + e.cell + ": tag " + e.tag + " in a " + gen.Zone(e.zone).height + " zone");
                if (gen.ZoneOf(e.cell).id != e.zone) Fail("entry " + e.cell + ": not in its zone " + e.zone);
            }
            var usable = new Dictionary<GridCoord, List<GridCoord>>();
            foreach (var chunk in chunks)
                for (var j = 0; j < n; j++)
                for (var i = 0; i < n; i++)
                {
                    var walk = false;
                    for (var k = 0; k < 4 && !walk; k++)
                    {
                        var kind = FrontRoomsAutoEntries.EdgeOf(chunk, i, j, k);
                        walk = kind != EdgeKind.Wall && kind != EdgeKind.Window;
                    }
                    if (!walk) continue;
                    var z = chunk.zone[MapGrid.LocalIndex(i, j)];
                    if (!usable.TryGetValue(z, out var cells)) usable[z] = cells = new List<GridCoord>();
                    cells.Add(chunk.Cell(i, j));
                }
            foreach (var id in inside)
            {
                row.entryZones++;
                byZone.TryGetValue(id, out var mine);
                var apart = mine != null ? FrontRoomsAutoEntries.Farthest(mine) : 0;
                if (apart >= FrontRoomsAutoEntries.MinApart) continue;
                usable.TryGetValue(id, out var cells);
                if (cells == null || FrontRoomsAutoEntries.Farthest(cells) < FrontRoomsAutoEntries.MinApart) { row.smallZones++; continue; }
                Fail("zone " + id + ": its Relay entries are at most " + apart + " cells apart (" + (mine != null ? mine.Count : 0) + " entries), its cells " + FrontRoomsAutoEntries.Farthest(cells));
            }
            foreach (var e in entries)
            {
                if (e.zone.x - 2 < min || e.zone.x + 2 >= max || e.zone.y - 2 < min || e.zone.y + 2 >= max) continue;
                row.entries++;
                if (e.rank == FrontRoomsAutoEntries.DeadEnd) row.deadEnds++;
                else if (e.rank == FrontRoomsAutoEntries.CorridorEnd) row.corridorEnds++;
                else row.otherCells++;
            }

            // 10.6 Cover (RELAY 12; reported, never a failure): every cell of each zone wholly inside.
            foreach (var id in inside)
            {
                var tallZone = gen.Zone(id).height == ZoneHeight.Tall;
                if (!usable.TryGetValue(id, out var cells) || cells.Count == 0) continue;
                float zoneMax = 0f, zoneSum = 0f;
                foreach (var cell in cells)
                {
                    var m = CoverMetres(cache, cell);
                    zoneMax = Math.Max(zoneMax, m);
                    zoneSum += m;
                }
                if (tallZone)
                {
                    tallSum += zoneSum;
                    tallCells += cells.Count;
                    row.coverTallMax = Math.Max(row.coverTallMax, zoneMax);
                    row.coverTallMean += zoneSum;
                    if (zoneMax > CoverTargetMetres)
                    {
                        row.coverTallOver++;
                        worst.Add(new MapFoilZoneCover { seed = settings.seed, zone = id, cells = cells.Count, max = zoneMax, mean = zoneSum / cells.Count });
                    }
                }
                else
                {
                    otherSum += zoneSum;
                    otherCells += cells.Count;
                    row.coverOtherMax = Math.Max(row.coverOtherMax, zoneMax);
                    row.coverOtherMean += zoneSum;
                }
            }
            var tallCount = 0;
            var otherCount = 0;
            foreach (var id in inside)
            {
                if (!usable.TryGetValue(id, out var cells)) continue;
                if (gen.Zone(id).height == ZoneHeight.Tall) tallCount += cells.Count; else otherCount += cells.Count;
            }
            row.coverTallMean = tallCount > 0 ? row.coverTallMean / tallCount : 0f;
            row.coverOtherMean = otherCount > 0 ? row.coverOtherMean / otherCount : 0f;
        }

        struct FoiledEdge { public GridCoord a, b, zone; }

        /// <summary>
        /// RELAY 12's cover measure for one cell (V7a; it uses a sight test, not a count of corners, as V4b's staged rows asked):
        /// the metres of walking (3 m a step, through anything but a wall or glass) from <paramref name="from"/> to the nearest
        /// cell out of a 12 m sight line from it: beyond CoverSightMetres, or with a wall across the line between the two cell
        /// centres (through a corner, either way round counts as seen). Glass, doors and arches are seen through (the worst case).
        /// CoverCapMetres when none is found within it.
        /// </summary>
        public static float CoverMetres(FrontRoomsMapCache cache, GridCoord from)
        {
            var cap = (int)(CoverCapMetres / MapGrid.CellSize);
            var depth = new Dictionary<GridCoord, int> { [from] = 0 };
            var queue = new Queue<GridCoord>();
            queue.Enqueue(from);
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                var d = depth[c];
                if (c != from && !InSight(cache, from, c)) return d * MapGrid.CellSize;
                if (d >= cap) continue;
                for (var k = 0; k < 4; k++)
                {
                    var next = k == 0 ? new GridCoord(c.x + 1, c.y) : k == 1 ? new GridCoord(c.x, c.y + 1) : k == 2 ? new GridCoord(c.x - 1, c.y) : new GridCoord(c.x, c.y - 1);
                    if (depth.ContainsKey(next)) continue;
                    var kind = cache.Edge(c, next);
                    if (kind == EdgeKind.Wall || kind == EdgeKind.Window) continue;
                    depth[next] = d + 1;
                    queue.Enqueue(next);
                }
            }
            return CoverCapMetres;
        }

        // In a 12 m sight line from a's centre: b's centre within CoverSightMetres and no wall across the line.
        static bool InSight(FrontRoomsMapCache cache, GridCoord a, GridCoord b)
        {
            float dx = (b.x - a.x) * MapGrid.CellSize, dy = (b.y - a.y) * MapGrid.CellSize;
            if (dx * dx + dy * dy > CoverSightMetres * CoverSightMetres) return false;
            return LineClear(cache, a, b);
        }

        // The segment between two cell centres crosses no wall: the cells it passes, in order (exact integer steps); through a
        // corner either way round will do.
        static bool LineClear(FrontRoomsMapCache cache, GridCoord a, GridCoord b)
        {
            int x = a.x, y = a.y;
            int sx = Math.Sign(b.x - a.x), sy = Math.Sign(b.y - a.y);
            int nx = Math.Abs(b.x - a.x), ny = Math.Abs(b.y - a.y);
            int ix = 0, iy = 0;
            while (ix < nx || iy < ny)
            {
                // The next x crossing lies at t = (ix + 0.5) / nx, the next y one at (iy + 0.5) / ny (both scaled by 2 nx ny).
                var tx = ix < nx ? (2L * ix + 1) * ny : long.MaxValue;
                var ty = iy < ny ? (2L * iy + 1) * nx : long.MaxValue;
                var here = new GridCoord(x, y);
                if (tx == ty)
                {
                    var side = new GridCoord(x + sx, y);
                    var up = new GridCoord(x, y + sy);
                    var corner = new GridCoord(x + sx, y + sy);
                    var viaX = cache.Edge(here, side) != EdgeKind.Wall && cache.Edge(side, corner) != EdgeKind.Wall;
                    var viaY = cache.Edge(here, up) != EdgeKind.Wall && cache.Edge(up, corner) != EdgeKind.Wall;
                    if (!viaX && !viaY) return false;
                    x += sx; y += sy; ix++; iy++;
                }
                else if (tx < ty)
                {
                    if (cache.Edge(here, new GridCoord(x + sx, y)) == EdgeKind.Wall) return false;
                    x += sx; ix++;
                }
                else
                {
                    if (cache.Edge(here, new GridCoord(x, y + sy)) == EdgeKind.Wall) return false;
                    y += sy; iy++;
                }
            }
            return true;
        }

        // ---------- 11. C16's alarm push-bar doors in the one trigger budget (Relay v2 step V7b) ----------

        /// <summary>
        /// C16 (V7b) over the seeds: maps made as AlarmCache makes them (alarm doors on with <paramref name="rules"/>, foil with
        /// <paramref name="foilRules"/> and Run! rooms with <paramref name="runRules"/> when given: one trigger budget), every
        /// other zone met at <paramref name="tier"/>. Each seed's alarm-door rows and the ledger (MapAlarmDoorReport.scope).
        /// Data only; alarm doors change no edge, so the map rules are ValidateSeed's.
        /// </summary>
        public static MapAlarmDoorReport RunAlarmDoors(MapSettings settings, int firstSeed, int seedCount, int radius, IReadOnlyList<RoomModuleData> modules,
            FrontRoomsAlarmDoorRules rules, FrontRoomsFoilRules foilRules, FrontRoomsRunRules runRules, int tier)
        {
            rules = rules ?? new FrontRoomsAlarmDoorRules();
            var report = new MapAlarmDoorReport
            {
                radiusChunks = radius, seedCount = seedCount, tier = tier, force = rules.force, foil = foilRules != null, runRooms = runRules != null,
                timestampUtc = DateTime.UtcNow.ToString("o"),
            };
            for (var i = 0; i < seedCount; i++)
            {
                var s = (settings ?? new MapSettings()).Clone();
                s.seed = unchecked(firstSeed + i * 7919);
                var row = new MapAlarmDoorSeedRow { seed = s.seed };
                ValidateAlarmSeed(s, radius, modules, rules, foilRules, runRules, tier, row);
                row.passed = row.errors.Count == 0;
                report.seeds.Add(row);
                if (row.passed) report.passed++; else report.failed++;
                report.zones += row.zones; report.doors += row.doors; report.candidates += row.candidates; report.interior += row.interior;
                report.plain += row.plain; report.alarmed += row.alarmed; report.slots += row.slots; report.noSlot += row.noSlot; report.noDoor += row.noDoor;
                report.arrival += row.arrival; report.taken += row.taken; report.excluded += row.excluded; report.singleBorders += row.singleBorders;
                report.ledgerAlarm += row.ledgerAlarm; report.ledgerFoil += row.ledgerFoil; report.ledgerRun += row.ledgerRun; report.runRoomsPlaced += row.runRooms;
            }
            return report;
        }

        /// <summary>
        /// A cache as the alarm-door check plays it: Run! rooms (with <paramref name="runRules"/>), foil (with
        /// <paramref name="foilRules"/>) and alarm doors (with <paramref name="rules"/>) attached in the world's order (the one
        /// budget takes the first planner's table: the profile's, the same for all), the start door at cell (0, 0), its chunk
        /// generated at tier 1 before the map is ready (the first zone), then the map ready, the first send gone out, and every
        /// later zone met at <paramref name="tier"/>. With <paramref name="rules"/> null it is the same map without alarm doors
        /// (FoilCache's steps, with foil), for the "unchanged" rows. The same steps on any cache give the same decisions.
        /// </summary>
        public static FrontRoomsMapCache AlarmCache(MapSettings settings, IReadOnlyList<RoomModuleData> modules, FrontRoomsAlarmDoorRules rules, FrontRoomsFoilRules foilRules,
            FrontRoomsRunRules runRules, int tier)
        {
            var cache = new FrontRoomsMapCache(settings, modules);
            FrontRoomsRunRooms run = null;
            if (runRules != null)
            {
                // Per zone and without the calm rule (ChecksRules): the plans stay pure for the fresh-cache checks.
                run = new FrontRoomsRunRooms(ChecksRules(runRules));
                cache.SetRunRooms(run);
            }
            FrontRoomsFoil foil = null;
            if (foilRules != null)
            {
                foil = new FrontRoomsFoil(foilRules);
                cache.SetFoil(foil);
            }
            FrontRoomsAlarmDoors doors = null;
            if (rules != null)
            {
                doors = new FrontRoomsAlarmDoors(rules);
                cache.SetAlarmDoors(doors);
            }
            var start = new GridCoord(0, 0);
            run?.SetStart(start);
            foil?.SetStart(start);
            doors?.SetStart(start);
            cache.Tier = 1;
            cache.Get(MapGrid.ChunkOf(start));
            if (run != null) { run.Ready = true; run.Open = true; }
            if (foil != null) foil.Ready = true;
            if (doors != null) doors.Ready = true;
            cache.Tier = Math.Max(1, tier);
            return cache;
        }

        static void ValidateAlarmSeed(MapSettings settings, int radius, IReadOnlyList<RoomModuleData> modules, FrontRoomsAlarmDoorRules rules, FrontRoomsFoilRules foilRules,
            FrontRoomsRunRules runRules, int tier, MapAlarmDoorSeedRow row)
        {
            void Fail(string message) { if (row.errors.Count < 12) row.errors.Add(message); }
            const int n = MapGrid.ChunkCells;
            int min = -radius, max = radius;
            var cache = AlarmCache(settings, modules, rules, foilRules, runRules, tier);
            var gen = cache.Generator;
            var doors = cache.AlarmDoors;
            var budget = cache.Budget;
            BuildSquare(cache, min, max);
            // Zones wholly inside the square: every cell of a zone lies in the 5 x 5 chunks round its site's chunk.
            var inside = new List<GridCoord>();
            var seenZones = new HashSet<GridCoord>();
            for (var y = min * n; y < max * n; y++)
            for (var x = min * n; x < max * n; x++)
            {
                var id = gen.ZoneOf(new GridCoord(x, y)).id;
                if (!seenZones.Add(id)) continue;
                if (id.x - 2 >= min && id.x + 2 < max && id.y - 2 >= min && id.y + 2 < max) inside.Add(id);
            }
            inside.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            var insideSet = new HashSet<GridCoord>(inside);
            var start = new GridCoord(0, 0);
            var arrival = gen.ZoneOf(start).id;

            // 11.1 Per Low or Standard zone: its chunk-border doors as built against the planner's candidates, one alarm door
            // within the budget, never kept plain or excluded, never the only crossing of its border.
            var alarmEdges = new List<AlarmEdge>();
            foreach (var id in inside)
            {
                if (gen.Zone(id).height == ZoneHeight.Tall) continue;
                row.zones++;
                var plan = doors.Plan(id);
                var tag = "zone " + id;
                if (plan == null) { Fail(tag + ": no alarm-door plan after Ready"); continue; }
                if (plan.Tier != cache.ZoneTier(id)) Fail(tag + ": planned at tier " + plan.Tier + ", stored tier " + cache.ZoneTier(id));
                if (budget.HasSlot(id, plan.Tier)) row.slots++;
                var planEdges = new HashSet<long>();
                var perBorder = new Dictionary<GridCoord, int>();
                foreach (var d in plan.Doors)
                {
                    planEdges.Add(d.edge);
                    perBorder.TryGetValue(d.neighbour, out var c);
                    perBorder[d.neighbour] = c + 1;
                    if (d.owned && d.excluded) row.excluded++;
                    if (d.owned && d.plain) row.plain++;
                }
                foreach (var pair in perBorder)
                    if (pair.Value == 1 && (id.x < pair.Key.x || (id.x == pair.Key.x && id.y < pair.Key.y))) row.singleBorders++;
                int border = 0, alarmedHere = 0;
                for (var y = (id.y - 2) * n; y < (id.y + 3) * n; y++)
                for (var x = (id.x - 2) * n; x < (id.x + 3) * n; x++)
                {
                    var cell = new GridCoord(x, y);
                    if (gen.ZoneOf(cell).id != id) continue;
                    for (var k = 0; k < 4; k++)
                    {
                        var next = k == 0 ? new GridCoord(x + 1, y) : k == 1 ? new GridCoord(x, y + 1) : k == 2 ? new GridCoord(x - 1, y) : new GridCoord(x, y - 1);
                        var other = gen.ZoneOf(next);
                        if (other.id == id || BuiltEdge(cache, cell, next) != EdgeKind.Door) continue;
                        var edge = MapGrid.EdgeId(cell, next);
                        var onBorder = MapGrid.ChunkOf(cell) != MapGrid.ChunkOf(next);
                        var forced = (gen.BorderOverride != null && gen.BorderOverride.ContainsKey(edge)) || (gen.RunBorderOverride != null && gen.RunBorderOverride.ContainsKey(edge));
                        if (onBorder && !forced && other.height != ZoneHeight.Tall && !doors.RunTouches(cell, next))
                        {
                            border++;
                            if (!planEdges.Contains(edge)) Fail(tag + ": chunk-border door " + cell + "-" + next + " is not a candidate");
                        }
                        if (doors.OwnerOf(cell, next) != id) continue;
                        row.doors++;
                        if (!onBorder) row.interior++;
                        if (!doors.IsAlarm(cell, next)) continue;
                        alarmedHere++;
                        alarmEdges.Add(new AlarmEdge { a = cell, b = next, zone = id });
                        if (!onBorder) Fail(tag + ": a door inside a chunk " + cell + "-" + next + " is an alarm door");
                    }
                }
                if (border != plan.Doors.Length) Fail(tag + ": " + plan.Doors.Length + " candidates, " + border + " chunk-border doors built");
                foreach (var d in plan.Doors)
                    if (BuiltEdge(cache, d.near, d.far) != EdgeKind.Door) Fail(tag + ": candidate " + d.near + "-" + d.far + " is " + BuiltEdge(cache, d.near, d.far) + ", not a door");
                if (alarmedHere > 1) Fail(tag + ": " + alarmedHere + " alarm doors");
                if (alarmedHere != (plan.HasAlarm ? 1 : 0)) Fail(tag + ": " + alarmedHere + " alarm doors built, the plan says " + (plan.HasAlarm ? 1 : 0));
                if (plan.HasAlarm)
                {
                    var d = plan.AlarmedDoor;
                    var where = d.near + "-" + d.far;
                    if (!rules.force && !budget.HasSlot(id, plan.Tier)) Fail(tag + ": an alarm door without its budget slot (roll " + budget.Roll(id) + ", chance " + budget.ChanceAt(plan.Tier) + ")");
                    if (budget.HolderOf(id) != TriggerDeviceKind.AlarmDoor) Fail(tag + ": an alarm door, but the ledger holds " + budget.HolderOf(id));
                    if (!d.owned || d.plain || d.excluded) Fail(tag + ": the alarm door " + where + " is " + (!d.owned ? "not its own" : d.plain ? "a kept-plain one" : "excluded"));
                    if (id == arrival || d.neighbour == arrival) Fail(tag + ": a device in the first zone");
                    if (Math.Abs(d.near.x - start.x) + Math.Abs(d.near.y - start.y) < rules.minStartCells || Math.Abs(d.far.x - start.x) + Math.Abs(d.far.y - start.y) < rules.minStartCells)
                        Fail(tag + ": the alarm door " + where + " within " + rules.minStartCells + " cells of the start door");
                    if (MapGrid.ChunkOf(d.near) == MapGrid.ChunkOf(d.far)) Fail(tag + ": the alarm door " + where + " is inside a chunk");
                    if (!OtherCrossing(cache, doors, id, d.neighbour, d.edge)) Fail(tag + ": the alarm door " + where + " is the only crossing of its border with zone " + d.neighbour);
                    // A Run! room planned before it (its zone inside the square, met while the square was built) never touches it.
                    var room = cache.Run != null ? cache.Run.RoomAt(d.near) ?? cache.Run.RoomAt(d.far) : null;
                    if (room != null && insideSet.Contains(room.Zone)) Fail(tag + ": the alarm door " + where + " is in Run! room " + room.Id);
                    if (cache.Run != null && cache.Run.IsGoalEdge(d.near, d.far))
                        foreach (var r in cache.Run.Rooms)
                            if (insideSet.Contains(r.Zone) && MapGrid.EdgeId(r.Cells[r.Cells.Length - 1], r.Beyond) == d.edge) Fail(tag + ": the alarm door " + where + " is Run! room " + r.Id + "'s goal door");
                }
                else if (plan.Miss == AlarmDoorMiss.NoSlot && (rules.force || budget.HasSlot(id, plan.Tier))) Fail(tag + ": no slot, but its roll passes");
                row.candidates += plan.Owned;
                row.alarmed += alarmedHere;
                switch (plan.Miss)
                {
                    case AlarmDoorMiss.NoSlot: row.noSlot++; break;
                    case AlarmDoorMiss.NoDoor: row.noDoor++; break;
                    case AlarmDoorMiss.Arrival: row.arrival++; break;
                    case AlarmDoorMiss.Taken: row.taken++; break;
                }
            }

            // 11.2 The ledger: one kind per zone; each kind only where its planner placed one; the counts agree.
            foreach (var pair in budget.Ledger)
            {
                if (pair.Value == TriggerDeviceKind.AlarmDoor)
                {
                    var plan = doors.Plan(pair.Key);
                    if (plan == null || !plan.HasAlarm) Fail("ledger: zone " + pair.Key + " held by AlarmDoor with no alarm door");
                }
                else if (pair.Value == TriggerDeviceKind.Foil)
                {
                    var plan = cache.Foil != null ? cache.Foil.Plan(pair.Key) : null;
                    if (plan == null || !plan.HasFoil) Fail("ledger: zone " + pair.Key + " held by Foil with no foiled window");
                }
                else if (pair.Value == TriggerDeviceKind.Run)
                {
                    if (cache.Run == null || !cache.Run.Decided(pair.Key, out var room) || room == null) Fail("ledger: zone " + pair.Key + " held by Run with no Run! room");
                }
            }
            row.ledgerAlarm = budget.Count(TriggerDeviceKind.AlarmDoor);
            row.ledgerFoil = budget.Count(TriggerDeviceKind.Foil);
            row.ledgerRun = budget.Count(TriggerDeviceKind.Run);
            row.runRooms = cache.Run != null ? cache.Run.Placed : 0;
            if (row.ledgerAlarm != doors.AlarmCount) Fail("ledger: " + row.ledgerAlarm + " AlarmDoor slots, " + doors.AlarmCount + " alarm doors");
            if (cache.Foil != null && row.ledgerFoil != cache.Foil.FoiledCount) Fail("ledger: " + row.ledgerFoil + " Foil slots, " + cache.Foil.FoiledCount + " foiled windows");
            if (cache.Run != null && row.ledgerRun != cache.Run.Placed) Fail("ledger: " + row.ledgerRun + " Run slots, " + cache.Run.Placed + " Run! rooms");
            if (budget.Devices != row.ledgerAlarm + row.ledgerFoil + row.ledgerRun) Fail("ledger: " + budget.Devices + " devices, " + (row.ledgerAlarm + row.ledgerFoil + row.ledgerRun) + " by kind");

            // 11.3 The other planners unchanged by alarm doors (the same cache without them): the Run! rooms of the zones inside
            // (decided when their zones were met, before any alarm door) and every inside Tall zone's foil.
            if (cache.Run != null || cache.Foil != null)
            {
                var without = AlarmCache(settings, modules, null, foilRules, runRules, tier);
                BuildSquare(without, min, max);
                if (cache.Run != null)
                {
                    var mine = new List<FrontRoomsRunRoom>();
                    var theirs = new List<FrontRoomsRunRoom>();
                    foreach (var r in cache.Run.Rooms) if (insideSet.Contains(r.Zone)) mine.Add(r);
                    foreach (var r in without.Run.Rooms) if (insideSet.Contains(r.Zone)) theirs.Add(r);
                    if (mine.Count != theirs.Count) Fail("Run!: " + mine.Count + " rooms in the zones inside with alarm doors, " + theirs.Count + " without");
                    for (var i = 0; i < mine.Count && i < theirs.Count; i++)
                        if (mine[i].Zone != theirs[i].Zone || mine[i].Cells[0] != theirs[i].Cells[0] || mine[i].Beyond != theirs[i].Beyond) Fail("Run!: room " + mine[i].Id + " of zone " + mine[i].Zone + " moved with alarm doors on");
                }
                if (cache.Foil != null)
                    foreach (var id in inside)
                    {
                        if (gen.Zone(id).height != ZoneHeight.Tall) continue;
                        var a = cache.Foil.Plan(id);
                        var b = without.Foil.Plan(id);
                        if (a == null || b == null || a.Miss != b.Miss || a.HasFoil != b.HasFoil || (a.HasFoil && a.FoiledWindow.edge != b.FoiledWindow.edge))
                            Fail("Tall zone " + id + ": foil decided differently with alarm doors on");
                    }
            }

            // 11.4 Determinism: a fresh cache made the same way decides every zone the same.
            var fresh = AlarmCache(settings, modules, rules, foilRules, runRules, tier);
            BuildSquare(fresh, min, max);
            foreach (var id in inside)
            {
                if (gen.Zone(id).height == ZoneHeight.Tall) continue;
                var a = doors.Plan(id);
                var b = fresh.AlarmDoors.Plan(id);
                if (a == null || b == null || a.Miss != b.Miss || a.HasAlarm != b.HasAlarm || (a.HasAlarm && a.AlarmedDoor.edge != b.AlarmedDoor.edge) || a.Doors.Length != b.Doors.Length)
                    Fail("zone " + id + ": a fresh cache decided it differently");
            }

            // 11.5 Revisit shifts: the alarm door's chunks (those inside the square) shifted twice keep it a door and alarmed,
            // and the candidates as they were.
            foreach (var ae in alarmEdges)
            {
                GridCoord a = ae.a, b = ae.b, zone = ae.zone;
                GridCoord ca = MapGrid.ChunkOf(a), cb = MapGrid.ChunkOf(b);
                var shiftB = cb != ca && cb.x >= min && cb.x < max && cb.y >= min && cb.y < max;
                for (var shift = 0; shift < 2; shift++)
                {
                    cache.Shift(ca);
                    if (shiftB) cache.Shift(cb);
                    cache.Get(ca);
                    if (shiftB) cache.Get(cb);
                }
                var kind = BuiltEdge(cache, a, b);
                if (kind != EdgeKind.Door || !doors.IsAlarm(a, b)) Fail("zone " + zone + ": after revisit shifts the alarm door " + a + "-" + b + " is " + kind + (doors.IsAlarm(a, b) ? "" : ", not alarmed"));
                if (doors.Candidates(zone).Length != doors.Plan(zone).Doors.Length) Fail("zone " + zone + ": its candidates changed with a revisit shift");
            }
        }

        struct AlarmEdge { public GridCoord a, b, zone; }

        // The square's chunks built, then every zone of a cell in it or one cell round it met (as the world meets the zones on
        // both sides of a door before it builds the door), so no later question meets a zone and plans a Run! room after the
        // alarm doors were decided.
        static void BuildSquare(FrontRoomsMapCache cache, int min, int max)
        {
            const int n = MapGrid.ChunkCells;
            for (var cy = min; cy < max; cy++)
            for (var cx = min; cx < max; cx++)
                cache.Get(new GridCoord(cx, cy));
            for (var y = min * n - 1; y <= max * n; y++)
            for (var x = min * n - 1; x <= max * n; x++)
                cache.ZoneOf(new GridCoord(x, y));
        }

        // The edge a-b as built, never generating a chunk: the chunk that holds it if built, else (a chunk-border edge) the seed's border rule.
        static EdgeKind BuiltEdge(FrontRoomsMapCache cache, GridCoord a, GridCoord b)
        {
            var lo = a;
            var hi = b;
            if (hi.x < lo.x || hi.y < lo.y) { lo = b; hi = a; }
            if (cache.IsBuilt(MapGrid.ChunkOf(lo))) return cache.Edge(lo, hi);
            return MapGrid.ChunkOf(lo) != MapGrid.ChunkOf(hi) ? cache.Generator.BorderEdge(lo, hi.x > lo.x) : EdgeKind.Wall;
        }

        // RELAY 6, "never the only crossing of a keyed border": another walkable edge between the two zones (a door, an arch or
        // an open edge; never glass) that is not an alarm door. Every cell of the zone lies in the 5 x 5 chunks round its site.
        static bool OtherCrossing(FrontRoomsMapCache cache, FrontRoomsAlarmDoors doors, GridCoord zone, GridCoord other, long edge)
        {
            var gen = cache.Generator;
            const int n = MapGrid.ChunkCells;
            for (var y = (zone.y - 2) * n; y < (zone.y + 3) * n; y++)
            for (var x = (zone.x - 2) * n; x < (zone.x + 3) * n; x++)
            {
                var cell = new GridCoord(x, y);
                if (gen.ZoneOf(cell).id != zone) continue;
                for (var k = 0; k < 4; k++)
                {
                    var next = k == 0 ? new GridCoord(x + 1, y) : k == 1 ? new GridCoord(x, y + 1) : k == 2 ? new GridCoord(x - 1, y) : new GridCoord(x, y - 1);
                    if (gen.ZoneOf(next).id != other || MapGrid.EdgeId(cell, next) == edge) continue;
                    var kind = BuiltEdge(cache, cell, next);
                    if (kind == EdgeKind.Wall || kind == EdgeKind.Window) continue;
                    if (kind == EdgeKind.Door && doors.IsAlarm(cell, next)) continue;
                    return true;
                }
            }
            return false;
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
        /// RunExits for the walkable stair (FrontRoomsLevelProfile.walkableStair; MAP_GENERATION.md "Walkable stair"): every
        /// floor's EXIT is ChooseStairExit's 1 x 2 (X1, X2), and each later floor takes SeedForStair's seed and starts in the
        /// previous X1-X2 with its door on N (ValidateExits with stair). The same seeds, floors and reports as RunExits.
        /// </summary>
        public static MapExitReport RunStairExits(MapSettings settings, int firstSeed, int seedCount, int floors, GridCoord titleDoorWorldCell, int titleHalfCells, int titleRows, IReadOnlyList<RoomModuleData> modules = null)
        {
            var report = new MapExitReport { seedCount = seedCount, floors = floors, scope = "Walkable stair: per seed, floors 1..n: floor 1 from the title's start area where the game puts it (RootFor), later floors from the previous X1-X2 (SeedForStair, a 1 x 2 start area, its door N south of X1); every EXIT 1 x 2 (ChooseStairExit)." };
            for (var f = 1; f <= floors; f++) report.stats.Add(new MapExitFloorStats { floor = f });
            for (var i = 0; i < seedCount; i++)
            {
                var s = (settings ?? new MapSettings()).Clone();
                s.seed = unchecked(firstSeed + i * 7919);
                var result = ValidateExits(s, floors, titleDoorWorldCell, titleHalfCells, titleRows, modules, null, true, out _);
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
            StartGroupTemplate station, out StartGroupPlan stationPlan) =>
            ValidateExits(settings, floors, titleDoorWorldCell, titleHalfCells, titleRows, modules, station, false, out stationPlan);

        /// <summary>
        /// ValidateExits, and with <paramref name="stair"/> the walkable stair's chain: ChooseStairExit (floor 1's start-group X
        /// as X1 when X2 fits, StairFromStartGroup), SeedForStair after a stair EXIT with its tests by hand (N' Standard Level 0,
        /// the way on south of it, no key in X1-X2), CheckStairExit on both cells, the fresh-cache replan and the revisit shift
        /// round the pair. Without it exactly as before.
        /// </summary>
        public static MapExitSeedReport ValidateExits(MapSettings settings, int floors, GridCoord titleDoorWorldCell, int titleHalfCells, int titleRows, IReadOnlyList<RoomModuleData> modules,
            StartGroupTemplate station, bool stair, out StartGroupPlan stationPlan)
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
                    // After a walkable-stair EXIT: SeedForStair (the start door N' south of X1-X2, the way on south of it).
                    var seedPlan = previous.stair
                        ? FrontRoomsExitPlanner.SeedForStair(settings.seed, f, previous.exitCell, settings, modules, floor.tier, out cache)
                        : FrontRoomsExitPlanner.SeedFor(settings.seed, f, previous.exitCell, settings, modules, floor.tier, out cache);
                    floor.floorSeed = seedPlan.seed;
                    floor.seedTries = seedPlan.tries;
                    door = previous.NextDoorCell;
                    area = previous.NextStartArea;
                    if (!seedPlan.standard) Fail("floor " + f + ": SeedFor found no Standard Level 0 start door at " + door + " in " + seedPlan.tries + " seeds");
                    floor.placardWest = seedPlan.placardWest;
                    // SeedFor's tests again, by hand: the door cell, the way on, and where it leads (glass blocks it).
                    var zone = cache.ZoneOf(door);
                    if (zone.height != ZoneHeight.Standard || zone.theme != ZoneTheme.Level0) Fail("floor " + f + ": start door cell " + door + " is " + zone.height + " " + zone.theme);
                    // The walkable stair: no zone key in X1-X2 (the map never spawns a reserved cell's key).
                    if (previous.stair)
                        for (var sy = area.y; sy < area.y + area.h; sy++)
                            CheckKeys(cache, new GridCoord(area.x, sy), "floor " + f + ": the start area X1-X2: ", Fail);
                    var ahead = new GridCoord(door.x, door.y + (previous.stair ? -1 : 1));
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
                    // The walkable stair: the station's X is X1 when X2 fits too (the game's rule); else that floor keeps C22's.
                    if (stair) plan = FrontRoomsExitPlanner.StairFromStartGroup(cache, area, plan, out _);
                }
                else
                {
                    FrontRoomsExitPlanner.BandFor(f, out lo, out hi);
                    plan = stair
                        ? FrontRoomsExitPlanner.ChooseStairExit(cache, area, door, floor.floorSeed, lo, hi)
                        : FrontRoomsExitPlanner.ChooseExit(cache, area, door, floor.floorSeed, lo, hi);
                }
                floor.exit = plan;
                if (!plan.found) { Fail("floor " + f + ": no EXIT from the door at " + door + " in any band (6-40)"); break; }
                floor.frontHeight = cache.ZoneOf(plan.frontCell).height.ToString();
                floor.generatedEdge = cache.Edge(plan.frontCell, plan.exitCell).ToString();
                if (plan.throughGlass && f > 1) Fail("floor " + f + ": the EXIT needs glass, but SeedFor's start leads on without it");
                if (plan.stair) CheckStairExit(cache, area, door, plan, lo, hi, "floor " + f + ": ", Fail);
                else CheckExit(cache, area, door, plan, lo, hi, "floor " + f + ": ", Fail);

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
                    var again = stair
                        ? FrontRoomsExitPlanner.ChooseStairExit(fresh, area, door, floor.floorSeed, lo, hi)
                        : FrontRoomsExitPlanner.ChooseExit(fresh, area, door, floor.floorSeed, lo, hi);
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

        /// <summary>
        /// A walkable-stair EXIT (plan.stair) against every rule, with the validator's own loops: CheckExit's on both cells (X1
        /// chunk-local i 1..6 and j 1..5, X1 and X2 off the start area and its ring, in no room, no column on the pair's six corners,
        /// no key's cell), the walk to N round both cells, the chunk whole without the pair, the pair's dead end, the nearest rule
        /// on stair candidates, and the front rules with exit = X1.
        /// </summary>
        static void CheckStairExit(FrontRoomsMapCache cache, CellRect area, GridCoord door, ExitPlan plan, int lo, int hi, string at, Action<string> fail)
        {
            const int n = MapGrid.ChunkCells;
            var x = plan.exitCell;
            var x2 = new GridCoord(x.x, x.y + 1);
            var pair = new CellRect(x.x, x.y, 1, 2);
            var front = plan.frontCell;
            if (front.x != x.x || front.y != x.y - 1) fail(at + "N " + front + " is not X1 " + x + " + (0, -1)");
            var coord = MapGrid.ChunkOf(x);
            var origin = MapGrid.ChunkOrigin(coord);
            int i = x.x - origin.x, j = x.y - origin.y;
            if (i < 1 || i > n - 2 || j < 1 || j > n - 3) fail(at + "X1 " + x + " is chunk-local (" + i + ", " + j + "), off i 1..6, j 1..5");
            if (RingDistance(area, x) <= 1 || RingDistance(area, x2) <= 1) fail(at + "X1-X2 " + x + " touches the start area or its ring");
            var chunk = cache.Get(coord);
            for (var r = 0; r < chunk.rooms.Length; r++)
                if (chunk.rooms[r].Contains(i, j) || chunk.rooms[r].Contains(i, j + 1)) fail(at + "X1-X2 " + x + " lies in room " + r + " of chunk " + coord);
            for (var dj = 0; dj <= 2; dj++)
                if (chunk.pillar[i + (j + dj) * (n + 1)] || chunk.pillar[i + 1 + (j + dj) * (n + 1)]) fail(at + "a column stands on a corner of X1-X2 " + x);
            CheckKeys(cache, x, at, fail);
            CheckKeys(cache, x2, at, fail);

            if (plan.throughGlass)
            {
                var pocket = WalkAround(cache, area, door, pair, int.MaxValue, LeadsOnCap, false).Count;
                if (pocket >= LeadsOnCap) fail(at + "the EXIT needs glass, but on foot the door " + door + " leads on (" + pocket + "+ cells)");
            }
            var walk = WalkAround(cache, area, door, pair, WidestBand, int.MaxValue, plan.throughGlass);
            if (!walk.TryGetValue(front, out var steps)) fail(at + "N " + front + " cannot be reached from the door " + door + " without X1-X2");
            else if (steps + 1 != plan.distance) fail(at + "distance " + plan.distance + " but the walk into X1 is " + (steps + 1));
            if (plan.distance < plan.bandMin || plan.distance > plan.bandMax) fail(at + "distance " + plan.distance + " outside its band " + plan.bandMin + "-" + plan.bandMax);

            // No disconnect: inside the chunk, N reaches X1, and without X1 and X2 it reaches every other cell it reached with them.
            var withPair = ChunkReach(cache, area, coord, front, null);
            var withoutPair = ChunkReachAround(cache, area, coord, front, pair);
            if (!withPair.Contains(x)) fail(at + "N " + front + " does not reach X1 " + x + " inside its chunk");
            else
                foreach (var cell in withPair)
                    if (cell != x && cell != x2 && !withoutPair.Contains(cell)) { fail(at + "removing X1-X2 " + x + " cuts " + cell + " off from N inside the chunk"); break; }

            var deadEnd = StairDeadEnd(cache, x);
            if (deadEnd != plan.deadEnd) fail(at + "X1-X2 " + x + " is " + (deadEnd ? "" : "not ") + "a dead end, the plan says otherwise");
            if (plan.nearestDeadEnds > 0 && !deadEnd) fail(at + "the nearest candidates had " + plan.nearestDeadEnds + " dead ends but X1-X2 " + x + " is not one");
            CheckNearest(cache, area, door, plan, lo, hi, at, fail, true);

            var frontHeight = cache.ZoneOf(front).height;
            if (frontHeight != ZoneHeight.Standard) fail(at + "N " + front + " is " + frontHeight + ", not Standard: the EXIT sign does not fit over the door");
            var glass = SightlineProblem(cache, x, false);
            if (glass != null) fail(at + "a window on the EXIT door's sightline: " + glass);
            var mayGlaze = SightlineProblem(cache, x, true);
            if (mayGlaze != null) fail(at + "an edge of the EXIT door's sightline a revisit shift could glaze: " + mayGlaze);
            var west = new GridCoord(front.x - 1, front.y);
            var east = new GridCoord(front.x + 1, front.y);
            var corridor = WalksThrough(cache, area, front, west) && WalksThrough(cache, area, front, east);
            if (corridor != plan.corridor) fail(at + "N " + front + (corridor ? " has" : " has no") + " corridor running past the door, the plan says otherwise");
        }

        // The pair's dead end, the validator's way: X1's west and east edges and X2's west, east and north edges all walls.
        static bool StairDeadEnd(FrontRoomsMapCache cache, GridCoord x)
        {
            var x2 = new GridCoord(x.x, x.y + 1);
            return cache.Edge(x, new GridCoord(x.x - 1, x.y)) == EdgeKind.Wall && cache.Edge(x, new GridCoord(x.x + 1, x.y)) == EdgeKind.Wall
                && cache.Edge(x2, new GridCoord(x2.x - 1, x2.y)) == EdgeKind.Wall && cache.Edge(x2, new GridCoord(x2.x + 1, x2.y)) == EdgeKind.Wall
                && cache.Edge(x2, new GridCoord(x2.x, x2.y + 1)) == EdgeKind.Wall;
        }

        /// <summary>CandidateDistance for a walkable-stair pair at X1 = <paramref name="x"/> (CheckStairExit's rules); -1 when a rule turns it down.</summary>
        static int StairCandidateDistance(FrontRoomsMapCache cache, CellRect area, GridCoord door, GridCoord x, bool glass)
        {
            const int n = MapGrid.ChunkCells;
            var x2 = new GridCoord(x.x, x.y + 1);
            var pair = new CellRect(x.x, x.y, 1, 2);
            var front = new GridCoord(x.x, x.y - 1);
            var coord = MapGrid.ChunkOf(x);
            var origin = MapGrid.ChunkOrigin(coord);
            int i = x.x - origin.x, j = x.y - origin.y;
            if (i < 1 || i > n - 2 || j < 1 || j > n - 3 || RingDistance(area, x) <= 1 || RingDistance(area, x2) <= 1) return -1;
            var chunk = cache.Get(coord);
            for (var r = 0; r < chunk.rooms.Length; r++)
                if (chunk.rooms[r].Contains(i, j) || chunk.rooms[r].Contains(i, j + 1)) return -1;
            for (var dj = 0; dj <= 2; dj++)
                if (chunk.pillar[i + (j + dj) * (n + 1)] || chunk.pillar[i + 1 + (j + dj) * (n + 1)]) return -1;
            var keyed = false;
            CheckKeys(cache, x, string.Empty, message => keyed = true);
            CheckKeys(cache, x2, string.Empty, message => keyed = true);
            if (keyed) return -1;
            if (cache.ZoneOf(front).height != ZoneHeight.Standard || SightlineProblem(cache, x, false) != null || SightlineProblem(cache, x, true) != null) return -1;
            var withPair = ChunkReach(cache, area, coord, front, null);
            if (!withPair.Contains(x)) return -1;
            var withoutPair = ChunkReachAround(cache, area, coord, front, pair);
            foreach (var cell in withPair)
                if (cell != x && cell != x2 && !withoutPair.Contains(cell)) return -1;
            var walk = WalkAround(cache, area, door, pair, WidestBand, int.MaxValue, glass);
            return walk.TryGetValue(front, out var steps) ? steps + 1 : -1;
        }

        // Walk, never into the rect avoid (the walkable stair's X1-X2) either.
        static Dictionary<GridCoord, int> WalkAround(FrontRoomsMapCache cache, CellRect area, GridCoord from, CellRect avoid, int maxSteps, int cap, bool glass = false)
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
                    if (dist.ContainsKey(next) || RingDistance(avoid, next) == 0 || RingDistance(area, next) == 0) continue;
                    var kind = cache.Edge(cell, next);
                    if (kind == EdgeKind.Wall || (!glass && kind == EdgeKind.Window)) continue;
                    dist[next] = steps + 1;
                    queue.Enqueue(next);
                }
            }
            return dist;
        }

        // ChunkReach, never through the rect avoid (the walkable stair's X1-X2).
        static HashSet<GridCoord> ChunkReachAround(FrontRoomsMapCache cache, CellRect area, GridCoord chunk, GridCoord from, CellRect avoid)
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
                    if (seen.Contains(next) || RingDistance(avoid, next) == 0 || RingDistance(area, next) == 0) continue;
                    if (!MapGrid.Passable(cache.Edge(cell, next))) continue;
                    seen.Add(next);
                    queue.Enqueue(next);
                }
            }
            return seen;
        }

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
        static void CheckNearest(FrontRoomsMapCache cache, CellRect area, GridCoord door, ExitPlan plan, int lo, int hi, string at, Action<string> fail, bool stair = false)
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
                // The walkable stair's candidates are pairs (StairCandidateDistance, StairDeadEnd).
                var distance = stair ? StairCandidateDistance(cache, area, door, candidate, plan.throughGlass) : CandidateDistance(cache, area, door, candidate, plan.throughGlass);
                if (distance < plan.bandMin || distance > plan.bandMax) continue;
                count++;
                var gap = distance < lo ? lo - distance : distance > hi ? distance - hi : 0;
                var deadEnd = stair ? StairDeadEnd(cache, candidate)
                    : cache.Edge(candidate, new GridCoord(candidate.x - 1, candidate.y)) == EdgeKind.Wall && cache.Edge(candidate, new GridCoord(candidate.x + 1, candidate.y)) == EdgeKind.Wall
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
            // The walkable stair: X2 = X1 + (0, 1) is left out too, and its outside neighbours must stay joined to N.
            var stairPair = plan.stair;
            var x2 = new GridCoord(x.x, x.y + 1);
            if (stairPair) CheckKeys(cache, x2, at + "after a revisit shift, ", fail);
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
                    if (next.x < x0 || next.x >= x1 || next.y < y0 || next.y >= y1 || next == x || (stairPair && next == x2) || seen.Contains(next) || FrontRoomsExitPlanner.InArea(area, next)) continue;
                    if (!MapGrid.Passable(cache.Edge(cell, next))) continue;
                    seen.Add(next);
                    queue.Enqueue(next);
                }
            }
            for (var d = 0; d < 4; d++)
            {
                var u = d == 0 ? new GridCoord(x.x + 1, x.y) : d == 1 ? new GridCoord(x.x - 1, x.y) : d == 2 ? new GridCoord(x.x, x.y + 1) : new GridCoord(x.x, x.y - 1);
                if (stairPair && u == x2) continue;
                if (MapGrid.Passable(cache.Edge(x, u)) && !FrontRoomsExitPlanner.InArea(area, u) && !seen.Contains(u))
                    fail(at + "after a revisit shift round X " + x + ", its neighbour " + u + " is cut off from N");
            }
            if (stairPair)
                for (var d = 0; d < 3; d++)
                {
                    var u = d == 0 ? new GridCoord(x2.x + 1, x2.y) : d == 1 ? new GridCoord(x2.x - 1, x2.y) : new GridCoord(x2.x, x2.y + 1);
                    if (MapGrid.Passable(cache.Edge(x2, u)) && !FrontRoomsExitPlanner.InArea(area, u) && !seen.Contains(u))
                        fail(at + "after a revisit shift round X1-X2 " + x + ", X2's neighbour " + u + " is cut off from N");
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
