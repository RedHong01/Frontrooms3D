using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using FrontRooms.Map;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// A26 step 0 (notes/floor1_start_group_plan.md 5): the headless feasibility check that decides CARVE against SEARCH
/// before any feature code. Data only, on the level profile's generation numbers and module library at tier 1 (as
/// FrontRoomsMapVerification, MV:47-49): seeds 20261001..20261100 x the 16 title door rows (85, 2 + 4t) x H {4, 3} x
/// E2 row {1, 2}: CARVE at k = 4 (plus k = 3 for H = 4 as variant rows) and SEARCH (natural line, k {4, 3} for H = 4,
/// {4, 5} for H = 3), the strip as the canonical R* = 20 rows (and the real 4 and 8 rows for t = 0 and 1 as a
/// cross-check). Every CARVE start is also re-planned on a fresh cache that generated two far chunks first
/// (determinism, the V:427-436 pattern) and re-checked with the 24 chunks of the 5 x 5 round c (all but c and c')
/// shifted (shift robustness). Then a 1,000-seed sweep at t = 0 and 1 for the footprints that pass. The core
/// (FrontRoomsStartGroup) works on a virtual overlay and never changes a cache; nothing in the game calls it.
/// Writes Verification/start-group-feasibility.json, logs the FEAS lines and the decision by 5.4's rule.
/// Run with -executeMethod FrontRoomsStartGroupFeasibility.RunBatch (or the menu item).
/// Options for a short run: -feasSeeds N, -feasSweep N (0 skips), -feasSearchSeeds N (0 skips SEARCH).
/// -feasAmendA1 1: CARVE only (matrix, variants, sweep) with amendment A1 (a root whose repair needs a window join is
/// retried like a leads-on failure; EvaluateCarve retryGlass), written to start-group-feasibility-amendA1.json. Supplementary
/// evidence for the decision; the plan's own run is the one without it.
/// Editor script: ASCII only, no tuples.
/// </summary>
public static class FrontRoomsStartGroupFeasibility
{
    const int FirstSeed = 20261001;
    const int SeedCount = 100;
    const int SweepFirstSeed = FirstSeed + SeedCount;
    const int SweepCount = 1000;
    const int DoorRows = 16;
    // 5.4's bars.
    const double MsP95Bar = 25.0, MsMaxBar = 60.0, RetryBar = 1.0, Rung2Bar = 99.0, SearchHitBar = 99.5;
    const int OutsideMedianBar = 2, OutsideP95Bar = 6, PortRootsExpected = 78;
    const string Dot = " \u00b7 ";

    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    static readonly string[] CarveReasons =
    {
        "ZoneNoRoot", "LeadsOnE2", "LeadsOnC1", "RepairWindow", "RepairSplit", "XRoom", "XKey", "ChunkWhole", "Sightline", "BladeSign",
        "LineWall", "LineArchRay", "LoopLong", "Determinism", "ShiftBroke", "G1", "Distance",
    };

    // ---------- Report ----------

    [Serializable]
    public sealed class FeasCount
    {
        public string name;
        public int count;
    }

    [Serializable]
    public sealed class FeasBar
    {
        public string name;
        public string value;
        public bool pass;
    }

    [Serializable]
    public sealed class FeasSummary
    {
        public string mode, set, k;
        public int h, e2Row, stripRows;
        public int n, hit, nEven, hitEven, nOdd, hitOdd, zoneNoRoot;
        public double hitPct, hitEvenPct, hitOddPct;
        public int ringMed, ringP95, ringMax, rootsMed, rootsP95, rootsMax;
        public double msMean, msP95, msMax, msRepeatP95, msRepeatMax;
        public int msOver60, msOver60Repeat, windowJoinsCarve;
        public int plansWithGc;
        public double chunksMean;
        public int chunksMax;
        public int repairA, repairB, repairC, startsRepairA, startsRepairB, startsRepairC, windowJoins, tallWindowJoins, repairFailures;
        public int leadsOnRetryStarts, leadsOnRetries;
        public double leadsOnRetryPct;
        public int loopMed, loopP90, loopMax, loopFail;
        public double rung1EmptyPct, rung1DotEmptyPct, rung2EmptyPct, rung3EmptyPct;
        public int outsideMed, outsideP95, outsideMax;
        public int roomDrops, roomDropsOutside, keyMoves, keySpotResets, columnsChanged, glassWalled, borderOverridesMax;
        public double determinismPct, shiftPct, g1Pct, assertsPct;
        public int searchLeadsOnFail;
        public List<FeasCount> kHistogram = new List<FeasCount>();
        public List<FeasCount> failReasons = new List<FeasCount>();
        public List<FeasCount> shiftBreaks = new List<FeasCount>();
        public List<FeasBar> bars = new List<FeasBar>();
        public bool barsPass;
        /// <summary>The same bars read by cause: window joins the carve cut (not glass pockets the raw map already has) and plan ms on the faster of two identical plans.</summary>
        public List<FeasBar> barsAttributed = new List<FeasBar>();
        public bool barsPassAttributed;
        /// <summary>Starts with a carve-cut window join, and those whose next zone root (up to MaxLeadsOnRetries more) plans with none and leads on.</summary>
        public int glassStarts, glassRetryClean;
        public string line;
    }

    [Serializable]
    public sealed class FeasRow
    {
        public string mode, set, fails, hash;
        public int seed, t, parity, h, e2Row, k, rows;
        public bool hit, found;
        public int rootIndex, ring, zoneRoots;
        public GridCoord root, door, exit;
        public float ms;
        public bool gc;
        public int chunks;
        public int repA, repB, repC, windows, windowsCarve, tallWindows, repairFail, retries;
        public string windowCells;
        public float msAgain;
        public bool leadsE2, leadsC1;
        public int loop, loopShift, rung1, rung1Dot, rung2, rung3;
        public string shiftBroke;
        public int drops, dropsOutside, columns, keys, keySpotResets, glassWalled, borderOverrides, outside;
        public bool deterministic, shiftOk, g1, asserts;
        public int distance, lineArches;
        /// <summary>Diagnostics for a start with a carve-cut window join: the next zone root whose carve has none and leads on (-1: none in MaxLeadsOnRetries more).</summary>
        public int glassRetryRoot = -1;
    }

    [Serializable]
    public sealed class FeasCross
    {
        public int seed, t, h, e2Row, realRows;
        public bool hitCanonical, hitReal, sameRoot, sameHash;
        public string failsReal;
    }

    [Serializable]
    public sealed class FeasReport
    {
        public string suite = "FrontRooms A26 step 0: floor-1 start group feasibility (CARVE vs SEARCH)";
        public string timestampUtc;
        public string plan = "notes/floor1_start_group_plan.md 5 (FINAL PLAN 2026-10-22, amendments 9)";
        public string profile;
        public int firstSeed, seedCount, sweepFirstSeed, sweepCount, doorRows, canonicalStripRows, rootRings, rootCount;
        public string archPortCheck;
        public string decision;
        public string decisionWhy;
        /// <summary>5.4's rule with the window-join and ms bars read by cause (barsAttributed).</summary>
        public string decisionAttributed;
        public string decisionWhyAttributed;
        public List<string> decisionSteps = new List<string>();
        public List<int> footprintsPassing = new List<int>();
        public List<FeasSummary> summaries = new List<FeasSummary>();
        public List<FeasCross> realRowsCrossCheck = new List<FeasCross>();
        public string realRowsSummary;
        public List<string> notes = new List<string>();
        public List<FeasRow> rows = new List<FeasRow>();
        public List<FeasRow> sweepFailRows = new List<FeasRow>();
    }

    // ---------- Entry ----------

    [MenuItem("FrontRooms/Map/Start group feasibility (A26 step 0)")]
    public static void Menu() => Run(false);

    public static void RunBatch() => Run(true);

    /// <summary>Amendment A1 for this run (-feasAmendA1 1): EvaluateCarve's retryGlass.</summary>
    static bool glassRetry;

    static int ArgInt(string name, int fallback)
    {
        var args = Environment.GetCommandLineArgs();
        for (var i = 0; i + 1 < args.Length; i++)
            if (args[i] == name && int.TryParse(args[i + 1], NumberStyles.Integer, Inv, out var v)) return v;
        return fallback;
    }

    public static string Run(bool batch)
    {
        var started = DateTime.UtcNow;
        var profile = FrontRoomsLevelProfiles.Resolve();
        var modules = profile.ModuleData();
        var settings = profile.generation;
        var seeds = ArgInt("-feasSeeds", SeedCount);
        var sweep = ArgInt("-feasSweep", SweepCount);
        var searchSeeds = ArgInt("-feasSearchSeeds", seeds);
        glassRetry = ArgInt("-feasAmendA1", 0) == 1;
        if (glassRetry) searchSeeds = 0;
        var profilePath = AssetDatabase.GetAssetPath(profile);
        var report = new FeasReport
        {
            timestampUtc = started.ToString("o"),
            profile = string.IsNullOrEmpty(profilePath) ? "code defaults" : profilePath,
            firstSeed = FirstSeed, seedCount = seeds, sweepFirstSeed = SweepFirstSeed, sweepCount = sweep, doorRows = DoorRows,
            canonicalStripRows = FrontRoomsStartGroup.CanonicalStripRows, rootRings = FrontRoomsExitPlanner.RootRings, rootCount = FrontRoomsStartGroup.RootCount,
        };
        if (glassRetry)
        {
            report.suite += " - AMENDMENT A1 (window join -> next zone root, as a leads-on failure; CARVE only)";
            report.notes.Add("Amendment A1 run: EvaluateCarve(retryGlass: true). A root whose repair needs a window join, or leaves a piece nothing joins, is retried like a leads-on failure (4.3 step 6, the same 1 + 8 roots); leads-on retries therefore count both causes. SEARCH and the real-rows cross-check are not run (see start-group-feasibility.json).");
        }
        report.archPortCheck = ArchPortCheck(settings.seed);
        Debug.Log("[FrontRoomsMap] FEAS" + Dot + report.archPortCheck);

        // Warm the JIT before anything is timed.
        {
            var w = Fresh(settings, modules, FirstSeed);
            var wt = StartGroupTemplate.StandIn(4, 1, 4);
            var wp = FrontRoomsStartGroup.EvaluateCarve(w, wt, DoorCell(1), FrontRoomsStartGroup.CanonicalStripRows);
            if (wp.found)
            {
                FrontRoomsStartGroup.Loop(w, wp.carve, wp.layout, FrontRoomsStartGroup.LoopCap);
                FrontRoomsStartGroup.RungCounts(w, wp.carve, wp.layout);
                FrontRoomsStartGroup.Line(w, wp.carve, wt, wp.layout, out _);
            }
            FrontRoomsStartGroup.EvaluateCarve(Fresh(settings, modules, FirstSeed + 1), wt, DoorCell(0), FrontRoomsStartGroup.CanonicalStripRows);
            FrontRoomsStartGroup.EvaluateSearch(Fresh(settings, modules, FirstSeed), wt, DoorCell(1), FrontRoomsStartGroup.CanonicalStripRows, new[] { 4, 3 });
        }

        var canonical = new Dictionary<string, FeasRow>();
        // CARVE: the matrix (k = 4), then the k = 3 variant rows for H = 4.
        foreach (var h in new[] { 4, 3 })
        foreach (var r in new[] { 1, 2 })
            report.summaries.Add(RunCarve(report, settings, modules, "matrix", h, r, 4, FirstSeed, seeds, AllRows(), false, canonical, null));
        foreach (var r in new[] { 1, 2 })
            report.summaries.Add(RunCarve(report, settings, modules, "variant", 4, r, 3, FirstSeed, seeds, AllRows(), false, null, null));

        // The real strip rows for t = 0 and 1 against R*.
        if (!glassRetry)
            foreach (var h in new[] { 4, 3 })
            foreach (var r in new[] { 1, 2 })
                report.summaries.Add(RunCarve(report, settings, modules, "realRows", h, r, 4, FirstSeed, seeds, new[] { 0, 1 }, true, null, canonical));
        int crossHitDiff = 0, crossRoot = 0, crossHash = 0;
        foreach (var c in report.realRowsCrossCheck)
        {
            if (c.hitCanonical != c.hitReal) crossHitDiff++;
            if (!c.sameRoot) crossRoot++;
            if (!c.sameHash) crossHash++;
        }
        report.realRowsSummary = report.realRowsCrossCheck.Count + " starts (t = 0, 1 x 4 configs, strip 4 | 8 rows): hit differs on " + crossHitDiff
            + ", root differs on " + crossRoot + ", carve hash differs on " + crossHash;
        Debug.Log("[FrontRoomsMap] FEAS" + Dot + "real rows vs R* = 20" + Dot + report.realRowsSummary);

        // SEARCH, the natural line.
        if (searchSeeds > 0)
            foreach (var h in new[] { 4, 3 })
            foreach (var r in new[] { 1, 2 })
                report.summaries.Add(RunSearch(report, settings, modules, "matrix", h, r, FirstSeed, searchSeeds, AllRows()));

        // 5.4's rule on the literal bars, then on the bars read by cause; the sweep covers what passes either way (labelled).
        var sweepConfigs = Decide(report, false);
        foreach (var cfg in Decide(report, true))
            if (!sweepConfigs.Any(x => x[0] == cfg[0] && x[1] == cfg[1] && x[2] == cfg[2])) sweepConfigs.Add(cfg);

        // The sweep: 1,000 more seeds at t = 0 and 1 for each footprint that passes (E2 row 1, k = 4). When no footprint
        // passes under either reading, the two E2-row-1 k-4 footprints are swept anyway as "sweepInfo" (rates only: it
        // does not enter the decision, which 5.4 step 3 has already taken).
        var sweepSet = "sweep";
        if (sweepConfigs.Count == 0)
        {
            sweepSet = "sweepInfo";
            sweepConfigs.Add(new[] { 4, 1, 4 });
            sweepConfigs.Add(new[] { 3, 1, 4 });
        }
        if (sweep > 0)
        {
            var sweepNotes = new List<string>();
            foreach (var cfg in sweepConfigs)
            {
                var s = RunCarve(report, settings, modules, sweepSet, cfg[0], cfg[1], cfg[2], SweepFirstSeed, sweep, new[] { 0, 1 }, false, null, null);
                report.summaries.Add(s);
                sweepNotes.Add("CARVE H" + s.h + " E2r" + s.e2Row + " k" + s.k + ": hit " + s.hit + "/" + s.n + ", bars " + (s.barsPass ? "PASS" : "FAIL [" + FailedBars(s) + "]")
                    + ", by cause " + (s.barsPassAttributed ? "PASS" : "FAIL [" + FailedBars(s, true) + "]"));
            }
            report.notes.Add((sweepSet == "sweep" ? "Sweep" : "Sweep for information only (no footprint passed under either reading)") + " (seeds " + SweepFirstSeed + ".." + (SweepFirstSeed + sweep - 1) + ", t = 0 and 1): " + string.Join("; ", sweepNotes));
        }

        report.notes.Add("Plan ms: Stopwatch round EvaluateCarve on a fresh cache (FindRoot, raw c and c', the carve with repair A-C, leads-on with retries, the C22 asserts, G1 and the distance walk) = 4.3 Plan's work at Space; cache creation, the loop, M7, the ray test, determinism and the shift check are outside it. plansWithGc counts timed plans a gen-0 GC ran inside (editor, Mono).");
        report.notes.Add("Repair: phase B and C open inner walls and border walls into OUT alike, cheapest kind then lowest edge id; a piece of Tall cells only that reaches OUT through its windows is joined by design (FM:653: Tall zones are left through windows), so tallWindowJoins (a new window into a Tall-only piece) is reported but is not a window join. windowJoins counts glass as the only join of a piece holding a non-Tall cell (5.4 bar 0); repairFailures counts pieces nothing could join.");
        report.notes.Add("Bars by cause: (1) a window join counts against the carve only when the piece led on (LeadsOnCells, glass blocking) in the raw map with the same cells reserved (the strip in phase B, nothing in phase C), i.e. the carve cut it off; the rest are glass pockets today's map already has at that root. (2) plan ms is the faster of the two identical plans each start makes (the timed plan and the determinism re-plan on a fresh cache), so a one-off scheduler or editor stall does not decide. Every other bar is the literal one. glassRetryRoot: for a start with any window join, the next zone root (up to 8 more) whose carve has none and leads on - a measure of a 'glass join -> next root' rule that the plan does not have.");
        report.notes.Add("M7 counts (4.10): the Relay's Search from C2 over the 5 x 5 built chunks (Open/Arch/Door pass; never the strip or X), before visibility and body fit, without the station and its ring, the exit-walk band (Chebyshev 2 of D..N and X) and the strip. Far side: an orthogonal walk from N in metres (cells x 3), reach 30 cells, unreached = infinity (FrontRoomsMapPathField.DistanceTo), not the field's diagonals and opening costs; rung1Dot is the dot test.");
        report.notes.Add("SEARCH: hit = the natural-line rules of 5.3 only (line Open or ray-passing arches, flat sign as generated, X rules as generated, no glass round N, X's chunk whole) on the station overlay plus N-X Open (the EXIT door's own edge, V5); leads-on of a hit is reported, not required. k order: H 4 tries 4 then 3, H 3 tries 4 then 5.");
        report.notes.Add("Shift robustness: the carve planned on revision 0, then every chunk of the 5 x 5 round c but c and c' shifted once (Cache.Shift): leads-on from E2o and C1, the loop, the ray line and the C22 asserts re-checked on the same carve.");
        report.notes.Add("Elapsed " + (DateTime.UtcNow - started).TotalSeconds.ToString("0", Inv) + " s.");

        var path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Verification", glassRetry ? "start-group-feasibility-amendA1.json" : "start-group-feasibility.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonUtility.ToJson(report, false));
        foreach (var s in report.summaries) Debug.Log("[FrontRoomsMap] " + s.line);
        foreach (var step in report.decisionSteps) Debug.Log("[FrontRoomsMap] FEAS" + Dot + "rule " + step);
        foreach (var note in report.notes) Debug.Log("[FrontRoomsMap] FEAS" + Dot + "note" + Dot + note);
        var tagA1 = glassRetry ? " [AMENDMENT A1, supplementary]" : "";
        Debug.Log("[FrontRoomsMap] FEAS" + Dot + "DECISION (literal bars)" + tagA1 + Dot + report.decision + Dot + report.decisionWhy + Dot + path);
        Debug.Log("[FrontRoomsMap] FEAS" + Dot + "DECISION (bars by cause)" + tagA1 + Dot + report.decisionAttributed + Dot + report.decisionWhyAttributed);
        return report.decision;
    }

    static int[] AllRows()
    {
        var rows = new int[DoorRows];
        for (var t = 0; t < DoorRows; t++) rows[t] = t;
        return rows;
    }

    /// <summary>The title door's world cell for terminal room t (MV:38-40): x 85 (TitleCenterX, G:279), y 2 + 4t (the cell 1.5 m past the door line at z 6 + 12t).</summary>
    static GridCoord DoorCell(int t) => new GridCoord(
        Mathf.FloorToInt(FrontRooms3DGame.TitleCenterX / MapGrid.CellSize),
        Mathf.FloorToInt((FrontRoomsRoomStream.RoomLength * (t + .5f) + MapGrid.CellSize * .5f) / MapGrid.CellSize));

    /// <summary>The real strip's rows when Space is pressed in room t with room 0 still streamed (MV:41 for t = 1): 4 (t + 1).</summary>
    static int RealRows(int t) => Mathf.RoundToInt(FrontRoomsRoomStream.RoomLength * (t + 1) / MapGrid.CellSize);

    static FrontRoomsMapCache Fresh(MapSettings settings, IReadOnlyList<RoomModuleData> modules, int seed) => FrontRoomsExitPlanner.CacheFor(settings, modules, seed, 1);

    static string Key(int seed, int t, int h, int r) => seed + "/" + t + "/" + h + "/" + r;

    /// <summary>Chunk-local row of the door cell: 2 (t even) or 6 (t odd).</summary>
    static int Parity(GridCoord door) => door.y - MapGrid.FloorDiv(door.y, MapGrid.ChunkCells) * MapGrid.ChunkCells;

    // ---------- CARVE ----------

    static FeasSummary RunCarve(FeasReport report, MapSettings settings, IReadOnlyList<RoomModuleData> modules, string set, int h, int r, int k,
        int firstSeed, int seeds, int[] doorRows, bool realRows, Dictionary<string, FeasRow> store, Dictionary<string, FeasRow> compare)
    {
        var template = StartGroupTemplate.StandIn(h, r, k);
        var rows = new List<FeasRow>();
        var watch = System.Diagnostics.Stopwatch.StartNew();
        for (var i = 0; i < seeds; i++)
        {
            var seed = firstSeed + i;
            foreach (var t in doorRows)
            {
                var stripRows = realRows ? RealRows(t) : FrontRoomsStartGroup.CanonicalStripRows;
                var row = CarveRow(settings, modules, seed, t, template, stripRows, set);
                rows.Add(row);
                if (store != null) store[Key(seed, t, h, r)] = row;
                if (compare != null && compare.TryGetValue(Key(seed, t, h, r), out var canon))
                    report.realRowsCrossCheck.Add(new FeasCross
                    {
                        seed = seed, t = t, h = h, e2Row = r, realRows = stripRows, hitCanonical = canon.hit, hitReal = row.hit,
                        sameRoot = canon.root == row.root, sameHash = canon.hash == row.hash, failsReal = row.fails,
                    });
                if (set.StartsWith("sweep", StringComparison.Ordinal)) { if (!row.hit) report.sweepFailRows.Add(row); }
                else report.rows.Add(row);
            }
        }
        var s = SummariseCarve(set, h, r, k, realRows ? -1 : FrontRoomsStartGroup.CanonicalStripRows, rows);
        Debug.Log("[FrontRoomsMap] FEAS progress" + Dot + "CARVE " + set + " " + template.Name + " done in " + watch.Elapsed.TotalSeconds.ToString("0.0", Inv) + " s");
        return s;
    }

    static FeasRow CarveRow(MapSettings settings, IReadOnlyList<RoomModuleData> modules, int seed, int t, StartGroupTemplate template, int stripRows, string set)
    {
        var door = DoorCell(t);
        var row = new FeasRow { mode = "CARVE", set = set, seed = seed, t = t, h = template.depth, e2Row = template.e2Row, k = template.corridor, rows = stripRows, loop = -1, rootIndex = -1 };
        row.parity = Parity(door);
        var cache = Fresh(settings, modules, seed);
        var gc = GC.CollectionCount(0);
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var plan = FrontRoomsStartGroup.EvaluateCarve(cache, template, door, stripRows, glassRetry);
        watch.Stop();
        row.ms = (float)watch.Elapsed.TotalMilliseconds;
        row.gc = GC.CollectionCount(0) != gc;
        row.chunks = cache.Count;
        row.found = plan.found;
        var fails = new List<string>();
        if (!plan.found)
        {
            row.fails = "ZoneNoRoot";
            return row;
        }
        var l = plan.layout;
        var c = plan.carve;
        row.rootIndex = plan.rootIndex;
        row.ring = plan.ring;
        row.zoneRoots = plan.zoneRoots;
        row.root = plan.root;
        row.door = l.door;
        row.exit = l.exit;
        row.repA = c.repairA;
        row.repB = c.repairB;
        row.repC = c.repairC;
        row.windows = c.windowJoins;
        row.windowsCarve = c.windowJoinsCarve;
        row.windowCells = string.Join(" ", c.windowJoinCells.Select(x => x.ToString()));
        row.tallWindows = c.tallWindowJoins;
        row.repairFail = c.repairFailures;
        row.retries = plan.leadsOnRetries;
        row.leadsE2 = plan.leadsOnE2;
        row.leadsC1 = plan.leadsOnC1;
        row.drops = c.roomDrops;
        row.dropsOutside = c.roomDropsOutside;
        row.columns = c.columnsChanged;
        row.keys = c.keyMoves.Count;
        row.keySpotResets = c.keySpotResets;
        row.glassWalled = c.glassWalled;
        row.borderOverrides = c.borderOverrides;
        row.outside = c.OutsideEdits;
        row.g1 = plan.g1;
        row.asserts = plan.Asserts;
        row.distance = plan.distance;
        row.hash = c.Hash(plan.root, l.door, l.exit);
        row.loop = FrontRoomsStartGroup.Loop(cache, c, l, FrontRoomsStartGroup.LoopCap);
        var rungs = FrontRoomsStartGroup.RungCounts(cache, c, l);
        row.rung1 = rungs.rung1;
        row.rung1Dot = rungs.rung1Dot;
        row.rung2 = rungs.rung2;
        row.rung3 = rungs.rung3;
        var line = FrontRoomsStartGroup.Line(cache, c, template, l, out row.lineArches);

        // Diagnostics (outside the timed plan): a start with a window join, planned again from the next zone roots
        // (up to MaxLeadsOnRetries more, as 4.3 step 6 does for leads-on): the first whose carve has no window join and
        // no unjoinable piece and leads on from E2o and C1. Measures a "glass join -> next root" rule; the plan does not use it.
        if (c.windowJoins > 0)
        {
            var from = plan.rootIndex + 1;
            for (var tries = 0; tries < FrontRoomsStartGroup.MaxLeadsOnRetries; tries++)
            {
                var index = FrontRoomsStartGroup.FindRoot(cache, template, door, from, stripRows, out var next);
                if (index < 0) break;
                var carve = FrontRoomsStartGroup.BuildCarve(cache, template, next);
                if (carve.windowJoins == 0 && carve.repairFailures == 0
                    && FrontRoomsStartGroup.LeadsOn(cache, carve, next, next.e2Out) && FrontRoomsStartGroup.LeadsOn(cache, carve, next, next.e1Out))
                {
                    row.glassRetryRoot = index;
                    break;
                }
                from = index + 1;
            }
        }

        // Determinism: a fresh cache that generated two other chunks first plans the same root, X and carve (V:427-436).
        var fresh = Fresh(settings, modules, seed);
        var far = l.ExitChunk;
        fresh.Get(new GridCoord(far.x + 2, far.y - 3));
        fresh.Get(new GridCoord(far.x - 1, far.y + 2));
        var watchAgain = System.Diagnostics.Stopwatch.StartNew();
        var again = FrontRoomsStartGroup.EvaluateCarve(fresh, template, door, stripRows, glassRetry);
        watchAgain.Stop();
        row.msAgain = (float)watchAgain.Elapsed.TotalMilliseconds;
        row.deterministic = again.found && again.root == plan.root && again.layout.exit == l.exit
            && again.carve.Hash(again.root, again.layout.door, again.layout.exit) == row.hash && again.leadsOnRetries == plan.leadsOnRetries;

        // Shift robustness: the same carve, with every chunk of the 5 x 5 round c but c and c' at revision 1.
        var shifted = Fresh(settings, modules, seed);
        var home = l.DoorChunk;
        for (var dy = -2; dy <= 2; dy++)
        for (var dx = -2; dx <= 2; dx++)
        {
            var coord = new GridCoord(home.x + dx, home.y + dy);
            if (!c.IsPinned(coord)) shifted.Shift(coord);
        }
        var shiftLine = FrontRoomsStartGroup.Line(shifted, c, template, l, out _);
        row.loopShift = FrontRoomsStartGroup.Loop(shifted, c, l, FrontRoomsStartGroup.LoopCap);
        // Broken = a check that passed on revision 0 fails after the shift (a check that already failed is counted by its own reason).
        var broke = new List<string>();
        if (plan.leadsOnE2 && !FrontRoomsStartGroup.LeadsOn(shifted, c, l, l.e2Out)) broke.Add("leadsE2");
        if (plan.leadsOnC1 && !FrontRoomsStartGroup.LeadsOn(shifted, c, l, l.e1Out)) broke.Add("leadsC1");
        if (row.loop >= 0 && row.loopShift < 0) broke.Add("loop");
        if (line == FrontRoomsStartGroup.LineView.Clear && shiftLine != line) broke.Add("line");
        if (plan.exitFits && !FrontRoomsStartGroup.ExitFits(shifted, c, l, out _, out _)) broke.Add("exitFits");
        if (plan.chunkWhole && !FrontRoomsStartGroup.KeepsChunkWhole(shifted, c, l)) broke.Add("chunkWhole");
        if (plan.sightline == FrontRoomsExitPlanner.SightlineView.Clear && FrontRoomsStartGroup.Sightline(shifted, c, l) != plan.sightline) broke.Add("sightline");
        if (!plan.corridor && FrontRoomsStartGroup.CorridorPastDoor(shifted, c, l)) broke.Add("corridor");
        if (plan.g1 && !FrontRoomsStartGroup.G1(shifted, c, l)) broke.Add("G1");
        row.shiftOk = broke.Count == 0;
        row.shiftBroke = string.Join("+", broke);

        if (!plan.leadsOnE2) fails.Add("LeadsOnE2");
        if (!plan.leadsOnC1) fails.Add("LeadsOnC1");
        if (c.windowJoins > 0) fails.Add("RepairWindow");
        if (c.repairFailures > 0) fails.Add("RepairSplit");
        if (plan.xRoom) fails.Add("XRoom");
        if (plan.xKey) fails.Add("XKey");
        if (!plan.chunkWhole) fails.Add("ChunkWhole");
        if (plan.sightline != FrontRoomsExitPlanner.SightlineView.Clear || !plan.frontStandard) fails.Add("Sightline");
        if (plan.corridor) fails.Add("BladeSign");
        if (line == FrontRoomsStartGroup.LineView.Wall) fails.Add("LineWall");
        if (line == FrontRoomsStartGroup.LineView.ArchRay) fails.Add("LineArchRay");
        if (row.loop < 0) fails.Add("LoopLong");
        if (!row.deterministic) fails.Add("Determinism");
        if (!row.shiftOk) fails.Add("ShiftBroke");
        if (!plan.g1) fails.Add("G1");
        if (plan.distance != template.depth + template.corridor) fails.Add("Distance");
        row.hit = fails.Count == 0;
        row.fails = string.Join(",", fails);
        return row;
    }

    static FeasSummary SummariseCarve(string set, int h, int r, int k, int stripRows, List<FeasRow> rows)
    {
        var s = new FeasSummary { mode = "CARVE", set = set, h = h, e2Row = r, k = k.ToString(Inv), stripRows = stripRows, n = rows.Count };
        var found = rows.Where(x => x.found).ToList();
        Common(s, rows, found);
        s.repairA = found.Sum(x => x.repA);
        s.repairB = found.Sum(x => x.repB);
        s.repairC = found.Sum(x => x.repC);
        s.startsRepairA = found.Count(x => x.repA > 0);
        s.startsRepairB = found.Count(x => x.repB > 0);
        s.startsRepairC = found.Count(x => x.repC > 0);
        s.windowJoins = found.Sum(x => x.windows);
        s.windowJoinsCarve = found.Sum(x => x.windowsCarve);
        // Timing noise: each plan runs twice (the determinism re-plan, on a fresh cache, is the same work); the faster of the two.
        var repeat = found.Select(x => Math.Min((double)x.ms, (double)x.msAgain)).ToList();
        s.msRepeatP95 = RankD(repeat, .95);
        s.msRepeatMax = repeat.Count == 0 ? 0 : repeat.Max();
        s.msOver60 = rows.Count(x => x.ms > MsMaxBar);
        s.msOver60Repeat = repeat.Count(x => x > MsMaxBar);
        s.tallWindowJoins = found.Sum(x => x.tallWindows);
        s.repairFailures = found.Sum(x => x.repairFail);
        s.leadsOnRetryStarts = found.Count(x => x.retries > 0);
        s.leadsOnRetries = found.Sum(x => x.retries);
        s.leadsOnRetryPct = Pct(s.leadsOnRetryStarts, s.n);
        s.rung1EmptyPct = Pct(found.Count(x => x.rung1 == 0), found.Count);
        s.rung1DotEmptyPct = Pct(found.Count(x => x.rung1Dot == 0), found.Count);
        s.rung2EmptyPct = Pct(found.Count(x => x.rung2 == 0), found.Count);
        s.rung3EmptyPct = Pct(found.Count(x => x.rung3 == 0), found.Count);
        s.outsideMed = Rank(found.Select(x => x.outside), .5);
        s.outsideP95 = Rank(found.Select(x => x.outside), .95);
        s.outsideMax = found.Count == 0 ? 0 : found.Max(x => x.outside);
        s.roomDrops = found.Sum(x => x.drops);
        s.roomDropsOutside = found.Sum(x => x.dropsOutside);
        s.keyMoves = found.Sum(x => x.keys);
        s.keySpotResets = found.Sum(x => x.keySpotResets);
        s.columnsChanged = found.Sum(x => x.columns);
        s.glassWalled = found.Sum(x => x.glassWalled);
        s.borderOverridesMax = found.Count == 0 ? 0 : found.Max(x => x.borderOverrides);
        s.determinismPct = Pct(found.Count(x => x.deterministic), found.Count);
        s.shiftPct = Pct(found.Count(x => x.shiftOk), found.Count);
        s.g1Pct = Pct(found.Count(x => x.g1), found.Count);
        s.assertsPct = Pct(found.Count(x => x.asserts), found.Count);
        var tally = new Dictionary<string, int>();
        foreach (var row in rows)
            if (!string.IsNullOrEmpty(row.fails))
                foreach (var f in row.fails.Split(','))
                    tally[f] = tally.TryGetValue(f, out var v) ? v + 1 : 1;
        foreach (var name in CarveReasons) if (tally.TryGetValue(name, out var v)) s.failReasons.Add(new FeasCount { name = name, count = v });
        var breaks = new SortedDictionary<string, int>();
        foreach (var row in found)
            if (!string.IsNullOrEmpty(row.shiftBroke))
                foreach (var b in row.shiftBroke.Split('+'))
                    breaks[b] = breaks.TryGetValue(b, out var v) ? v + 1 : 1;
        foreach (var pair in breaks) s.shiftBreaks.Add(new FeasCount { name = pair.Key, count = pair.Value });

        // 5.4's CARVE hard bars (each footprint).
        var all = found.Count == s.n;
        Bar(s, "0 ZoneNoRoot", s.zoneNoRoot.ToString(Inv), s.zoneNoRoot == 0);
        Bar(s, "0 window joins", s.windowJoins + " (+" + s.repairFailures + " unjoinable pockets)", s.windowJoins == 0 && s.repairFailures == 0);
        Bar(s, "leads-on retries <= 1 %", F(s.leadsOnRetryPct) + " %", s.leadsOnRetryPct <= RetryBar);
        Bar(s, "determinism 100 %", F(s.determinismPct) + " %", all && s.determinismPct >= 100.0);
        Bar(s, "shift robustness 100 %", F(s.shiftPct) + " %", all && s.shiftPct >= 100.0);
        Bar(s, "G1 100 %", F(s.g1Pct) + " %", all && s.g1Pct >= 100.0);
        Bar(s, "plan ms p95 <= 25 and max <= 60", F(s.msP95) + " / " + F(s.msMax), s.msP95 <= MsP95Bar && s.msMax <= MsMaxBar);
        Bar(s, "M7 rung 2 non-empty >= 99 %", F(100.0 - s.rung2EmptyPct) + " %", 100.0 - s.rung2EmptyPct >= Rung2Bar);
        s.barsPass = s.bars.All(b => b.pass);
        // The same bars read by cause: a window join the carve cut (the piece led on in the raw map with the same cells
        // reserved) against a glass pocket the raw map already has; plan ms on the faster of two identical plans.
        s.glassStarts = found.Count(x => x.windows > 0);
        s.glassRetryClean = found.Count(x => x.windows > 0 && x.glassRetryRoot >= 0);
        foreach (var b in s.bars)
        {
            if (b.name == "0 window joins")
                s.barsAttributed.Add(new FeasBar { name = "0 window joins cut by the carve", value = s.windowJoinsCarve + " (+" + s.repairFailures + " unjoinable; " + (s.windowJoins - s.windowJoinsCarve) + " glass pockets the raw map already has)", pass = s.windowJoinsCarve == 0 && s.repairFailures == 0 });
            else if (b.name.StartsWith("plan ms", StringComparison.Ordinal))
                s.barsAttributed.Add(new FeasBar { name = "plan ms p95 <= 25 and max <= 60, faster of two identical plans", value = F(s.msRepeatP95) + " / " + F(s.msRepeatMax), pass = s.msRepeatP95 <= MsP95Bar && s.msRepeatMax <= MsMaxBar });
            else s.barsAttributed.Add(new FeasBar { name = b.name, value = b.value, pass = b.pass });
        }
        s.barsPassAttributed = s.barsAttributed.All(b => b.pass);
        s.line = "FEAS" + Dot + "CARVE H" + h + " E2r" + r + " k" + k + (set == "matrix" ? "" : " [" + set + (stripRows < 0 ? " rows 4|8" : "") + "]")
            + Dot + "hit " + s.hit + "/" + s.n + " (t even " + s.hitEven + "/" + s.nEven + ", odd " + s.hitOdd + "/" + s.nOdd + ")"
            + Dot + "ring " + s.ringMed + "/" + s.ringP95 + "/" + s.ringMax + " (roots " + s.rootsMed + "/" + s.rootsP95 + "/" + s.rootsMax + (s.rootsMax <= PortRootsExpected ? ", <= 78 as the port" : ", > 78 (port expected <= 78)") + ")"
            + Dot + "ms " + F(s.msMean) + "/" + F(s.msP95) + "/" + F(s.msMax) + " (" + s.plansWithGc + " with a GC; " + s.msOver60 + " over 60; faster of two runs p95/max " + F(s.msRepeatP95) + "/" + F(s.msRepeatMax) + ", " + s.msOver60Repeat + " over 60)"
            + Dot + "chunks " + F(s.chunksMean) + "/" + s.chunksMax
            + Dot + "repairs A/B/C " + s.repairA + "/" + s.repairB + "/" + s.repairC + " (starts " + s.startsRepairA + "/" + s.startsRepairB + "/" + s.startsRepairC + ")"
            + Dot + "window joins " + s.windowJoins + " (" + s.windowJoinsCarve + " cut off by the carve, " + (s.windowJoins - s.windowJoinsCarve) + " glass pockets before it; Tall-only windows " + s.tallWindowJoins + ", unjoinable " + s.repairFailures + ")"
            + Dot + "leads-on retries " + s.leadsOnRetries + " (" + F(s.leadsOnRetryPct) + " % of starts)"
            + Dot + "loop " + s.loopMed + "/" + s.loopP90 + "/" + s.loopMax + (s.loopFail > 0 ? " (" + s.loopFail + " over " + FrontRoomsStartGroup.LoopCap + ")" : "")
            + Dot + "rung1/2/3 empty " + F(s.rung1EmptyPct) + "/" + F(s.rung2EmptyPct) + "/" + F(s.rung3EmptyPct) + " % (rung1 by dot " + F(s.rung1DotEmptyPct) + " %)"
            + Dot + "outside edits " + s.outsideMed + "/" + s.outsideP95 + " (max " + s.outsideMax + "; room drops " + s.roomDropsOutside + " of " + s.roomDrops + ", key moves " + s.keyMoves + ")"
            + Dot + "det " + F(s.determinismPct) + " % shift " + F(s.shiftPct) + " %" + (s.shiftBreaks.Count > 0 ? " (broke: " + string.Join(", ", s.shiftBreaks.Select(x => x.name + " " + x.count)) + ")" : "") + " G1 " + F(s.g1Pct) + " % asserts " + F(s.assertsPct) + " %"
            + Dot + "fail reasons {" + string.Join(", ", s.failReasons.Select(x => x.name + ": " + x.count)) + "}"
            + Dot + "bars " + (s.barsPass ? "PASS" : "FAIL [" + FailedBars(s) + "]")
            + Dot + "by cause " + (s.barsPassAttributed ? "PASS" : "FAIL [" + FailedBars(s, true) + "]")
            + (s.glassStarts > 0 ? Dot + "glass starts " + s.glassStarts + ", next root clean " + s.glassRetryClean : "");
        return s;
    }

    /// <summary>The fields CARVE and SEARCH summaries share: hits by parity, rings and roots, ms, chunks, loops.</summary>
    static void Common(FeasSummary s, List<FeasRow> rows, List<FeasRow> found)
    {
        s.hit = rows.Count(x => x.hit);
        s.nEven = rows.Count(x => x.parity == 2);
        s.nOdd = rows.Count(x => x.parity == 6);
        s.hitEven = rows.Count(x => x.hit && x.parity == 2);
        s.hitOdd = rows.Count(x => x.hit && x.parity == 6);
        s.hitPct = Pct(s.hit, s.n);
        s.hitEvenPct = Pct(s.hitEven, s.nEven);
        s.hitOddPct = Pct(s.hitOdd, s.nOdd);
        s.zoneNoRoot = rows.Count(x => x.zoneRoots == 0);
        s.ringMed = Rank(found.Select(x => x.ring), .5);
        s.ringP95 = Rank(found.Select(x => x.ring), .95);
        s.ringMax = found.Count == 0 ? 0 : found.Max(x => x.ring);
        s.rootsMed = Rank(found.Select(x => x.rootIndex + 1), .5);
        s.rootsP95 = Rank(found.Select(x => x.rootIndex + 1), .95);
        s.rootsMax = found.Count == 0 ? 0 : found.Max(x => x.rootIndex + 1);
        s.msMean = rows.Count == 0 ? 0 : rows.Average(x => (double)x.ms);
        s.msP95 = RankD(rows.Select(x => (double)x.ms), .95);
        s.msMax = rows.Count == 0 ? 0 : rows.Max(x => (double)x.ms);
        s.plansWithGc = rows.Count(x => x.gc);
        s.chunksMean = rows.Count == 0 ? 0 : rows.Average(x => (double)x.chunks);
        s.chunksMax = rows.Count == 0 ? 0 : rows.Max(x => x.chunks);
        var loops = found.Where(x => x.loop >= 0).Select(x => x.loop).ToList();
        s.loopMed = Rank(loops, .5);
        s.loopP90 = Rank(loops, .9);
        s.loopMax = loops.Count == 0 ? 0 : loops.Max();
        s.loopFail = found.Count(x => x.loop < 0);
    }

    // ---------- SEARCH ----------

    static FeasSummary RunSearch(FeasReport report, MapSettings settings, IReadOnlyList<RoomModuleData> modules, string set, int h, int r, int firstSeed, int seeds, int[] doorRows)
    {
        var ks = h == 4 ? new[] { 4, 3 } : new[] { 4, 5 };
        var template = StartGroupTemplate.StandIn(h, r, ks[0]);
        var rows = new List<FeasRow>();
        var funnel = new int[(int)StartGroupReject.Count];
        var watch = System.Diagnostics.Stopwatch.StartNew();
        for (var i = 0; i < seeds; i++)
        {
            var seed = firstSeed + i;
            foreach (var t in doorRows)
            {
                var door = DoorCell(t);
                var row = new FeasRow { mode = "SEARCH", set = set, seed = seed, t = t, h = h, e2Row = r, rows = FrontRoomsStartGroup.CanonicalStripRows, loop = -1, rootIndex = -1 };
                row.parity = Parity(door);
                var cache = Fresh(settings, modules, seed);
                var gc = GC.CollectionCount(0);
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var res = FrontRoomsStartGroup.EvaluateSearch(cache, template, door, FrontRoomsStartGroup.CanonicalStripRows, ks);
                sw.Stop();
                row.ms = (float)sw.Elapsed.TotalMilliseconds;
                row.gc = GC.CollectionCount(0) != gc;
                row.chunks = cache.Count;
                row.found = res.found;
                row.hit = res.found;
                row.zoneRoots = res.zoneRoots;
                for (var q = 0; q < funnel.Length; q++) funnel[q] += res.rejects[q];
                var parts = new List<string>();
                if (res.found)
                {
                    row.rootIndex = res.rootIndex;
                    row.ring = res.ring;
                    row.k = res.k;
                    row.root = res.root;
                    row.door = res.layout.door;
                    row.exit = res.layout.exit;
                    row.leadsE2 = FrontRoomsStartGroup.LeadsOn(cache, res.overlay, res.layout, res.layout.e2Out);
                    row.leadsC1 = FrontRoomsStartGroup.LeadsOn(cache, res.overlay, res.layout, res.layout.e1Out);
                    row.loop = FrontRoomsStartGroup.Loop(cache, res.overlay, res.layout, FrontRoomsStartGroup.LoopCap);
                    FrontRoomsStartGroup.Line(cache, res.overlay, template, res.layout, out row.lineArches);
                }
                else
                {
                    parts.Add(res.zoneRoots == 0 ? "ZoneNoRoot" : "NoHit");
                    for (var q = 0; q < res.rejects.Length; q++) if (res.rejects[q] > 0) parts.Add((StartGroupReject)q + ":" + res.rejects[q]);
                }
                row.fails = string.Join(",", parts);
                if (set.StartsWith("sweep", StringComparison.Ordinal)) { if (!row.hit) report.sweepFailRows.Add(row); }
                else report.rows.Add(row);
                rows.Add(row);
            }
        }
        var s = new FeasSummary { mode = "SEARCH", set = set, h = h, e2Row = r, k = string.Join("|", ks.Select(x => x.ToString(Inv))), stripRows = FrontRoomsStartGroup.CanonicalStripRows, n = rows.Count };
        var found = rows.Where(x => x.found).ToList();
        Common(s, rows, found);
        s.searchLeadsOnFail = found.Count(x => !x.leadsE2 || !x.leadsC1);
        foreach (var kv in found.GroupBy(x => x.k).OrderBy(g => g.Key)) s.kHistogram.Add(new FeasCount { name = "k" + kv.Key, count = kv.Count() });
        s.failReasons.Add(new FeasCount { name = "ZoneNoRoot", count = s.zoneNoRoot });
        s.failReasons.Add(new FeasCount { name = "NoHit (24 rings)", count = s.n - s.hit });
        for (var q = 0; q < funnel.Length; q++) s.failReasons.Add(new FeasCount { name = ((StartGroupReject)q).ToString(), count = funnel[q] });
        Bar(s, "(a) hit >= 99.5 % at t even", F(s.hitEvenPct) + " %", s.hitEvenPct >= SearchHitBar);
        Bar(s, "(a) hit >= 99.5 % at t odd", F(s.hitOddPct) + " %", s.hitOddPct >= SearchHitBar);
        Bar(s, "(a) ms p95 <= 25", F(s.msP95), s.msP95 <= MsP95Bar);
        s.barsPass = s.bars.All(b => b.pass);
        s.line = "FEAS" + Dot + "SEARCH H" + h + " E2r" + r + " k" + s.k + (set == "matrix" ? "" : " [" + set + "]")
            + Dot + "hit " + s.hit + "/" + s.n + " (t even " + s.hitEven + "/" + s.nEven + ", odd " + s.hitOdd + "/" + s.nOdd + ")"
            + Dot + "ring " + s.ringMed + "/" + s.ringP95 + "/" + s.ringMax + " (roots " + s.rootsMed + "/" + s.rootsP95 + "/" + s.rootsMax + ")"
            + Dot + "ms " + F(s.msMean) + "/" + F(s.msP95) + "/" + F(s.msMax) + " (" + s.plansWithGc + " with a GC)"
            + Dot + "chunks " + F(s.chunksMean) + "/" + s.chunksMax
            + Dot + "repairs A/B/C -" + Dot + "window joins -" + Dot + "leads-on of hits failing " + s.searchLeadsOnFail + "/" + s.hit
            + Dot + "loop " + s.loopMed + "/" + s.loopP90 + "/" + s.loopMax + (s.loopFail > 0 ? " (" + s.loopFail + " over " + FrontRoomsStartGroup.LoopCap + ")" : "")
            + Dot + "rung1/2/3 empty -" + Dot + "outside edits 0/0"
            + Dot + "k " + string.Join(" ", s.kHistogram.Select(x => x.name + ":" + x.count))
            + Dot + "fail reasons {" + string.Join(", ", s.failReasons.Where(x => x.count > 0).Select(x => x.name + ": " + x.count)) + "}"
            + Dot + "(a) " + (s.barsPass ? "PASS" : "FAIL [" + FailedBars(s) + "]");
        Debug.Log("[FrontRoomsMap] FEAS progress" + Dot + "SEARCH " + set + " H" + h + " E2r" + r + " done in " + watch.Elapsed.TotalSeconds.ToString("0.0", Inv) + " s");
        return s;
    }

    // ---------- 5.4: the decision ----------

    static FeasSummary Find(FeasReport report, string mode, string set, int h, int r, string k)
    {
        foreach (var s in report.summaries)
            if (s.mode == mode && s.set == set && s.h == h && s.e2Row == r && (k == null || s.k == k)) return s;
        return null;
    }

    /// <summary>
    /// 5.4's rule, on the literal bars (<paramref name="attributed"/> false: report.decision) or on the bars read by cause
    /// (true: report.decisionAttributed). Returns the CARVE configs that pass (H, E2 row, k) for the sweep.
    /// </summary>
    static List<int[]> Decide(FeasReport report, bool attributed)
    {
        var steps = report.decisionSteps;
        var tag = attributed ? "by cause" : "literal";
        Func<FeasSummary, bool> pass = x => attributed ? x.barsPassAttributed : x.barsPass;
        var sweep = new List<int[]>();
        var carvePasses = new List<int>();
        var searchReplaces = new List<int>();
        string decision, why;
        foreach (var h in new[] { 4, 3 })
        {
            var carve = Find(report, "CARVE", "matrix", h, 1, "4");
            var search = Find(report, "SEARCH", "matrix", h, 1, null);
            if (carve == null) continue;
            steps.Add(tag + Dot + "1" + Dot + "CARVE H" + h + " (E2 row 1, k 4): " + (pass(carve) ? "meets every hard bar" : "FAILS " + FailedBars(carve, attributed)));
            var a = search != null && search.barsPass;
            var b = carve.outsideMed > OutsideMedianBar || carve.outsideP95 > OutsideP95Bar;
            steps.Add(tag + Dot + "2" + Dot + "H" + h + ": (a) SEARCH " + (search == null ? "not run" : (a ? "meets (a): " : "misses (a): ") + F(search.hitEvenPct) + " % t even / " + F(search.hitOddPct) + " % t odd, ms p95 " + F(search.msP95))
                + "; (b) CARVE outside edits median " + carve.outsideMed + " / p95 " + carve.outsideP95 + (b ? " exceed 2 / 6" : " within 2 / 6")
                + " -> " + (a && b ? "SEARCH replaces CARVE" : "SEARCH does not replace CARVE"));
            if (a && b) searchReplaces.Add(h);
            else if (pass(carve)) carvePasses.Add(h);
        }
        if (carvePasses.Count == 0 && searchReplaces.Count == 0)
        {
            // 5.4 step 3: CARVE fails a hard bar: report the reasons and the variants (k 3, E2 row 2, H 3) before any feature code.
            var variants = report.summaries.Where(x => x.mode == "CARVE" && (x.set == "matrix" || x.set == "variant") && pass(x)).ToList();
            steps.Add(tag + Dot + "3" + Dot + "CARVE fails a hard bar on every E2-row-1 footprint; variants passing every bar: "
                + (variants.Count == 0 ? "none" : string.Join(", ", variants.Select(v => "H" + v.h + " E2r" + v.e2Row + " k" + v.k))));
            decision = "CARVE failed: re-run variants";
            why = "5.4 step 3 (stop before feature code). Failing bars: " + string.Join(" | ", report.summaries.Where(x => x.mode == "CARVE" && x.set == "matrix" && x.e2Row == 1 && !pass(x))
                .Select(x => "H" + x.h + ": " + FailedBars(x, attributed)))
                + ". Variants passing: " + (variants.Count == 0 ? "none" : string.Join(", ", variants.Select(v => "H" + v.h + " E2r" + v.e2Row + " k" + v.k)));
            foreach (var v in variants) sweep.Add(new[] { v.h, v.e2Row, int.Parse(v.k, Inv) });
        }
        else
        {
            foreach (var h in carvePasses) sweep.Add(new[] { h, 1, 4 });
            if (searchReplaces.Count > 0 && carvePasses.Count == 0)
            {
                decision = "SEARCH";
                why = "5.4 step 2: SEARCH meets (a) and CARVE's outside edits meet (b) for H " + string.Join(", ", searchReplaces);
            }
            else
            {
                decision = "CARVE";
                why = "5.4 step 1: CARVE meets every hard bar for H " + string.Join(" and ", carvePasses)
                    + (searchReplaces.Count > 0 ? "; SEARCH would replace it for H " + string.Join(", ", searchReplaces) : "; step 2 does not apply (SEARCH misses (a), or CARVE's outside edits are within (b))");
            }
            steps.Add(tag + Dot + "4" + Dot + "footprints: " + (carvePasses.Count == 2 ? "both H 4 and H 3 pass, the map supports both (the depth is the visual line's pick; 4 x 4 preferred, 9)" : "H " + string.Join(", ", carvePasses) + " passes"));
        }
        if (attributed)
        {
            report.decisionAttributed = decision;
            report.decisionWhyAttributed = why;
        }
        else
        {
            report.decision = decision;
            report.decisionWhy = why;
            report.footprintsPassing.AddRange(carvePasses);
        }
        return sweep;
    }

    // ---------- Helpers ----------

    static void Bar(FeasSummary s, string name, string value, bool pass) => s.bars.Add(new FeasBar { name = name, value = value, pass = pass });

    static string FailedBars(FeasSummary s, bool attributed = false) => string.Join("; ", (attributed ? s.barsAttributed : s.bars).Where(b => !b.pass).Select(b => b.name + " = " + b.value));

    static double Pct(int part, int whole) => whole == 0 ? 0.0 : 100.0 * part / whole;

    static string F(double v) => v.ToString(v >= 100 ? "0.#" : "0.0#", Inv);

    /// <summary>Nearest-rank percentile of integers (0 when empty).</summary>
    static int Rank(IEnumerable<int> values, double p)
    {
        var list = values.ToList();
        if (list.Count == 0) return 0;
        list.Sort();
        var i = (int)Math.Ceiling(p * list.Count) - 1;
        return list[Math.Max(0, Math.Min(list.Count - 1, i))];
    }

    static double RankD(IEnumerable<double> values, double p)
    {
        var list = values.ToList();
        if (list.Count == 0) return 0;
        list.Sort();
        var i = (int)Math.Ceiling(p * list.Count) - 1;
        return list[Math.Max(0, Math.Min(list.Count - 1, i))];
    }

    /// <summary>
    /// MapArch.Opening against the world's own form (FrontRoomsMapWorld.ArchOpening, MW:2411-2417: MapHash and Mathf.Lerp),
    /// over 20,000 edges of the seed: they must agree bit for bit. MapHash is internal to the runtime assembly, so it is
    /// reached by reflection.
    /// </summary>
    static string ArchPortCheck(int seed)
    {
        var type = typeof(FrontRoomsMapGenerator).Assembly.GetType("FrontRooms.Map.MapHash");
        var hash = type == null ? null : type.GetMethod("Hash", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        var unit = type == null ? null : type.GetMethod("Unit", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        if (hash == null || unit == null) return "MapArch port check skipped: MapHash not found";
        var mismatches = 0;
        var checks = 0;
        for (var x = -50; x < 50; x++)
        for (var y = -50; y < 50; y++)
        for (var s = 0; s < 2; s++)
        {
            var a = new GridCoord(x * 7, y * 5);
            var alongX = s == 1;
            var edgeHash = (uint)hash.Invoke(null, new object[] { seed, a.x * 2 + (alongX ? 1 : 0), a.y, 97, 0 });
            var w = ModuleUnits.ArchMinWidth + ModuleUnits.ArchWidthSpread * (float)unit.Invoke(null, new object[] { edgeHash });
            var margin = ModuleUnits.ArchCornerMargin + w * .5f;
            var c = Mathf.Lerp(margin, MapGrid.CellSize - margin, (float)unit.Invoke(null, new object[] { (uint)hash.Invoke(null, new object[] { (int)edgeHash, 3, 7, 11, 0 }) }));
            MapArch.Opening(seed, a, alongX, out var w2, out var c2);
            checks++;
            if (w != w2 || c != c2) mismatches++;
        }
        return "MapArch.Opening vs the world's ArchOpening form: " + (checks - mismatches) + "/" + checks + " edges identical (seed " + seed + ")";
    }
}
