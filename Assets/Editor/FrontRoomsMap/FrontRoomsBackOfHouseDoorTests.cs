using System;
using System.Collections.Generic;
using System.IO;
using FrontRooms.Map;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Back-of-house map doors (zone doors D-2 option 2; MAP_GENERATION.md "Back-of-house doors"): the census and the guarantees of
/// FrontRoomsMapWorld.BackOfHouseDoor, checked without playing the game, in FrontRoomsMapInteractionTests' idiom:
/// - the census, cache only (Cache set by reflection), every edge of the 9 x 9 chunks round chunk (0, 0) on 20 seeds: the map
///   doors by today's member (the kit's rule: Office, else Lobby) and by the rule; the share of today's Lobby doors that turn
///   back-of-house is BackOfHouseShare +- 0.05, the share of service zones among the Low zones with a Lobby door +- 0.06;
/// - never true for an Office door, a door whose sides are not Low | Standard, or an edge that is not a door; the same from
///   either cell; the kit's member today is Lobby for every back-of-house door;
/// - both chunk copies of every border door are a door, a second cache that generates the chunks in the reverse order gives
///   every door the same answer, and after a revisit shift of all 81 chunks (a new revision) every border door and every
///   interior door that is still a door keeps its answer;
/// - never true for the EXIT door or a door in its 2-cell approach, a door by the start stairwell on a later floor, a door of
///   the floor-1 station's group, a Run! door, or a door with a pool side (scene 4's pool plan); each exclusion is also shown
///   to bite on a door the rule otherwise takes.
/// With the environment variable FR_BOH_PLANS naming a folder, it also writes the plan pictures of seeds 516574485 and
/// 20261001 (TODAY and RULE, 17 px a cell) and boh_census.txt there, for the plan sheet.
/// Writes Verification/map-boh-door-tests.json.
/// Headless: -executeMethod FrontRoomsBackOfHouseDoorTests.RunBatch -quit (throws on FAIL).
/// </summary>
public static class FrontRoomsBackOfHouseDoorTests
{
    [Serializable]
    sealed class Report
    {
        public string verdict;
        public int passed, failed;
        public List<string> checks = new List<string>();
        public List<string> census = new List<string>();
    }

    static Report report;

    static void Check(bool ok, string what)
    {
        if (ok) report.passed++; else report.failed++;
        report.checks.Add((ok ? "ok   " : "FAIL ") + what);
    }

    static void Note(string what) => report.census.Add(what);

    // The census seeds (FrontRoomsMapInteractionTests' transitions share) and the two floors of the plan pictures.
    static readonly int[] Seeds = { 516574485, 2554, 20388, 20261001, 1, 7, 42, 99, 123, 777, 1001, 1990, 2026, 3141, 4242, 5555, 8080, 9001, 12345, 31337 };
    static readonly int[] PlanSeeds = { 516574485, 20261001 };
    const int Radius = 4;
    const float ShareTolerance = .05f, ZoneShareTolerance = .06f;

    [MenuItem("FrontRooms/Map/Test back-of-house doors")]
    public static void Run() => Execute(false);

    public static void RunBatch() => Execute(true);

    static void Execute(bool throwOnFail)
    {
        report = new Report();
        var ambientMode = RenderSettings.ambientMode;
        var sky = RenderSettings.ambientSkyColor;
        var equator = RenderSettings.ambientEquatorColor;
        var ground = RenderSettings.ambientGroundColor;
        var reflection = RenderSettings.reflectionIntensity;
        var fog = RenderSettings.fog;
        var fogMode = RenderSettings.fogMode;
        var fogColor = RenderSettings.fogColor;
        var fogDensity = RenderSettings.fogDensity;
        var skybox = RenderSettings.skybox;
        var keepHook = FrontRoomsInteractableKit.DoorMemberFromMap;
        var roots = new List<GameObject>();
        var profiles = new List<FrontRoomsLevelProfile>();
        var plans = Environment.GetEnvironmentVariable("FR_BOH_PLANS");
        try
        {
            // The kit's theme rule alone in the census (a Run! world below sets the map hook; it is put back after).
            FrontRoomsInteractableKit.DoorMemberFromMap = null;
            var census = new CensusResult();
            Section("census", () => Census(roots, profiles, plans, census));
            Section("exit", () => ExitDoors(roots, profiles, census));
            Section("stairwell", () => StairwellDoors(roots, profiles, census));
            Section("station", () => StationDoors(roots, profiles));
            Section("run", () => RunDoors(roots, profiles));
            Section("pool", () => PoolDoors(roots, profiles));
        }
        finally
        {
            FrontRoomsInteractableKit.DoorMemberFromMap = keepHook;
            foreach (var r in roots)
            {
                if (r == null) continue;
                var w = r.GetComponent<FrontRoomsMapWorld>();
                if (w != null) w.Release();
                UnityEngine.Object.DestroyImmediate(r);
            }
            foreach (var p in profiles) if (p != null) UnityEngine.Object.DestroyImmediate(p);
            RenderSettings.ambientMode = ambientMode;
            RenderSettings.ambientSkyColor = sky;
            RenderSettings.ambientEquatorColor = equator;
            RenderSettings.ambientGroundColor = ground;
            RenderSettings.reflectionIntensity = reflection;
            RenderSettings.fog = fog;
            RenderSettings.fogMode = fogMode;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = fogDensity;
            RenderSettings.skybox = skybox;
            DynamicGI.UpdateEnvironment();
        }

        var pass = report.failed == 0 && report.passed > 0;
        report.verdict = (pass ? "PASS" : "FAIL") + ": " + report.passed + " passed, " + report.failed + " failed";
        var path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Verification", "map-boh-door-tests.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonUtility.ToJson(report, true));
        var text = "[BackOfHouseDoorTests] " + report.verdict + "\n" + string.Join("\n", report.checks) + "\n" + string.Join("\n", report.census);
        if (pass) Debug.Log(text); else Debug.LogError(text);
        if (throwOnFail && !pass) throw new Exception(report.verdict);
    }

    static void Section(string name, Action body)
    {
        try { body(); }
        catch (Exception e) { Check(false, name + ": exception " + e.GetType().Name + " " + e.Message + "\n" + e.StackTrace); }
    }

    // ---------- Worlds ----------

    static FrontRoomsLevelProfile Profile(List<FrontRoomsLevelProfile> profiles, int seed)
    {
        var p = UnityEngine.Object.Instantiate(FrontRoomsLevelProfiles.Resolve());
        p.hideFlags = HideFlags.DontSave;
        p.generation = p.Generation(seed);
        profiles.Add(p);
        return p;
    }

    static FrontRoomsMapWorld World(List<GameObject> roots, FrontRoomsLevelProfile profile, float period, string name)
    {
        var root = new GameObject(name) { hideFlags = HideFlags.DontSave };
        roots.Add(root);
        // Far from the open scene, on whole world periods (FrontRoomsMapInteractionTests.World).
        root.transform.position = new Vector3(period * ModuleUnits.WorldPeriod, 0f, -29f * ModuleUnits.WorldPeriod);
        var world = root.AddComponent<FrontRoomsMapWorld>();
        world.Profile = profile;
        return world;
    }

    static void SetCache(FrontRoomsMapWorld w, FrontRoomsMapCache cache) =>
        typeof(FrontRoomsMapWorld).GetProperty("Cache").GetSetMethod(true).Invoke(w, new object[] { cache });

    // A world on a fresh cache of the seed, nothing built (the interaction tests' census idiom).
    static FrontRoomsMapWorld CacheWorld(List<GameObject> roots, List<FrontRoomsLevelProfile> profiles, int seed, float period, string name, out FrontRoomsMapCache cache)
    {
        var w = World(roots, Profile(profiles, seed), period, name);
        cache = new FrontRoomsMapCache(w.Profile.Generation(seed), w.Profile.ModuleData());
        SetCache(w, cache);
        return w;
    }

    // A door object for the edge a-b, as BuildEdge makes it (only the cells and the edge key matter to the rule).
    static FrontRoomsMapWorld.Door Probe(GridCoord a, GridCoord b) => new FrontRoomsMapWorld.Door { a = a, b = b, edge = MapGrid.EdgeId(a, b) };

    static GridCoord Step(GridCoord a, int d) => d == 0 ? new GridCoord(a.x + 1, a.y) : new GridCoord(a.x, a.y + 1);

    static int Chebyshev(GridCoord a, GridCoord b) => Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));

    // Today's member by the kit's theme rule (FrontRoomsInteractableKit.DoorMemberFor with no map hook), written out so the
    // census still reads today once the visual line adds its back-of-house member: Office when a non-Low side is an Office
    // zone (or a Low | Low door with an Office side), else Lobby.
    static bool OfficeToday(ZoneInfo za, ZoneInfo zb)
    {
        var office = (za.height != ZoneHeight.Low && za.theme == ZoneTheme.Office) || (zb.height != ZoneHeight.Low && zb.theme == ZoneTheme.Office);
        if (!office && za.height == ZoneHeight.Low && zb.height == ZoneHeight.Low) office = za.theme == ZoneTheme.Office || zb.theme == ZoneTheme.Office;
        return office;
    }

    static bool LowStandard(ZoneInfo za, ZoneInfo zb) =>
        (za.height == ZoneHeight.Low && zb.height == ZoneHeight.Standard) || (zb.height == ZoneHeight.Low && za.height == ZoneHeight.Standard);

    // The rule's zone half on its own (no exclusion): a Level 0 Low | Standard pair whose Low zone is a service zone.
    static bool ByZone(FrontRoomsMapWorld w, GridCoord a, GridCoord b)
    {
        var za = w.Cache.Generator.ZoneOf(a);
        var zb = w.Cache.Generator.ZoneOf(b);
        if (za.theme != ZoneTheme.Level0 || zb.theme != ZoneTheme.Level0 || !LowStandard(za, zb)) return false;
        return w.BackOfHouseZone(za.height == ZoneHeight.Low ? za.id : zb.id);
    }

    // ---------- The census ----------

    sealed class DoorRecord
    {
        public GridCoord a, b;
        public bool boh, office, border;
    }

    sealed class CensusResult
    {
        public int seed;
        public FrontRoomsMapWorld.Door sample;
    }

    static void Census(List<GameObject> roots, List<FrontRoomsLevelProfile> profiles, string plans, CensusResult result)
    {
        int doors = 0, office = 0, lobby = 0, boh = 0, nonDoor = 0, nonDoorTrue = 0, officeTrue = 0, otherPair = 0, otherTrue = 0;
        int asym = 0, kitBad = 0, border = 0, copyBad = 0, orderDiff = 0, kept = 0, shiftDiff = 0, borderLost = 0, interiorKept = 0, interiorLost = 0;
        int lowZones = 0, serviceZones = 0, minFloor = int.MaxValue, maxFloor = 0, minLobby = int.MaxValue, maxLobby = 0;
        var lines = new List<string>();
        var planDir = string.IsNullOrEmpty(plans) ? null : plans;
        if (planDir != null) Directory.CreateDirectory(planDir);
        for (var k = 0; k < Seeds.Length; k++)
        {
            var seed = Seeds[k];
            var w = CacheWorld(roots, profiles, seed, -80f - 2f * k, "BOH DOOR TEST / census " + seed, out var cache);
            var records = new List<DoorRecord>();
            var lowSeen = new HashSet<GridCoord>();
            var serviceSeen = new HashSet<GridCoord>();
            int floorDoors = 0, floorLobby = 0, floorOffice = 0, floorBoh = 0;
            for (var cy = -Radius; cy <= Radius; cy++)
            for (var cx = -Radius; cx <= Radius; cx++)
            {
                var o = MapGrid.ChunkOrigin(new GridCoord(cx, cy));
                for (var j = 0; j < MapGrid.ChunkCells; j++)
                for (var i = 0; i < MapGrid.ChunkCells; i++)
                {
                    var a = new GridCoord(o.x + i, o.y + j);
                    for (var d = 0; d < 2; d++)
                    {
                        var b = Step(a, d);
                        var kind = cache.Edge(a, b);
                        var probe = Probe(a, b);
                        var yes = w.BackOfHouseDoor(probe);
                        if (yes != w.BackOfHouseDoor(Probe(b, a))) asym++;
                        if (kind != EdgeKind.Door) { nonDoor++; if (yes) nonDoorTrue++; continue; }
                        doors++;
                        floorDoors++;
                        var za = cache.Generator.ZoneOf(a);
                        var zb = cache.Generator.ZoneOf(b);
                        var lowStd = LowStandard(za, zb);
                        if (!lowStd) { otherPair++; if (yes) otherTrue++; }
                        var isOffice = OfficeToday(za, zb);
                        if (isOffice) { office++; floorOffice++; if (yes) officeTrue++; }
                        else
                        {
                            lobby++;
                            floorLobby++;
                            if (lowStd && za.theme == ZoneTheme.Level0 && zb.theme == ZoneTheme.Level0)
                            {
                                var low = za.height == ZoneHeight.Low ? za.id : zb.id;
                                lowSeen.Add(low);
                                if (w.BackOfHouseZone(low)) serviceSeen.Add(low);
                            }
                        }
                        if (yes)
                        {
                            boh++;
                            floorBoh++;
                            var kit = FrontRoomsInteractableKit.DoorMemberFor(w, probe).ToString();
                            if (kit != "Lobby" && kit != "BackOfHouse") kitBad++;
                        }
                        var isBorder = MapGrid.ChunkOf(a) != MapGrid.ChunkOf(b);
                        if (isBorder)
                        {
                            border++;
                            var next = cache.Get(MapGrid.ChunkOf(b));
                            var copy = d == 0 ? next.west[a.y - next.Origin.y] : next.south[a.x - next.Origin.x];
                            if (copy != EdgeKind.Door) copyBad++;
                        }
                        records.Add(new DoorRecord { a = a, b = b, boh = yes, office = isOffice, border = isBorder });
                    }
                }
            }
            lowZones += lowSeen.Count;
            serviceZones += serviceSeen.Count;
            minFloor = Mathf.Min(minFloor, floorBoh);
            maxFloor = Mathf.Max(maxFloor, floorBoh);
            minLobby = Mathf.Min(minLobby, floorLobby);
            maxLobby = Mathf.Max(maxLobby, floorLobby);
            lines.Add("seed " + seed + ": " + floorDoors + " doors, " + floorOffice + " Office, " + floorLobby + " Lobby, " + floorBoh + " back-of-house ("
                + (floorLobby > 0 ? (100f * floorBoh / floorLobby).ToString("0.0") : "0") + " % of Lobby); " + lowSeen.Count + " Low zones with a Lobby door, " + serviceSeen.Count + " service zones");
            if (result.sample == null)
                foreach (var r in records)
                {
                    if (!r.boh) continue;
                    // A door the EXIT and stairwell checks can move X or the start next to: well inside its chunk (X at local 1..6).
                    var lo = MapGrid.ChunkOrigin(MapGrid.ChunkOf(r.a));
                    int li = r.a.x - lo.x, lj = r.a.y - lo.y;
                    if (li < 1 || li > 5 || lj < 1 || lj > 4 || r.b.x != r.a.x) continue;
                    result.seed = seed;
                    result.sample = Probe(r.a, r.b);
                    break;
                }

            if (planDir != null && Array.IndexOf(PlanSeeds, seed) >= 0)
            {
                WritePlan(w, cache, records, seed, Path.Combine(planDir, "boh_plan_" + seed + "_today.png"), false);
                WritePlan(w, cache, records, seed, Path.Combine(planDir, "boh_plan_" + seed + "_rule.png"), true);
                var service = new HashSet<GridCoord>();
                foreach (var z in lowSeen) if (w.BackOfHouseZone(z)) service.Add(z);
                File.WriteAllText(Path.Combine(planDir, "boh_plan_" + seed + ".txt"), "seed " + seed + "\ndoors " + floorDoors + "\noffice " + floorOffice + "\nlobby " + floorLobby
                    + "\nboh " + floorBoh + "\nlowzones " + lowSeen.Count + "\nservicezones " + service.Count + "\n");
            }

            // Build order: a second cache of the seed that generates the 81 chunks in the reverse order.
            var w2 = CacheWorld(roots, profiles, seed, -81f - 2f * k, "BOH DOOR TEST / census order " + seed, out var cache2);
            for (var cy = Radius; cy >= -Radius; cy--)
            for (var cx = Radius; cx >= -Radius; cx--)
                cache2.Get(new GridCoord(cx, cy));
            foreach (var r in records)
                if (w2.BackOfHouseDoor(Probe(r.b, r.a)) != r.boh) orderDiff++;

            // A revisit shift of every chunk (a new revision): border doors stay doors; every door that still is one keeps its answer.
            for (var cy = -Radius; cy <= Radius; cy++)
            for (var cx = -Radius; cx <= Radius; cx++)
                cache.Shift(new GridCoord(cx, cy));
            foreach (var r in records)
            {
                if (cache.Edge(r.a, r.b) != EdgeKind.Door)
                {
                    if (r.border) borderLost++; else interiorLost++;
                    continue;
                }
                kept++;
                if (!r.border) interiorKept++;
                if (w.BackOfHouseDoor(Probe(r.a, r.b)) != r.boh) shiftDiff++;
            }
        }

        var floors = (float)Seeds.Length;
        var chunks = floors * (2 * Radius + 1) * (2 * Radius + 1);
        var share = lobby > 0 ? boh / (float)lobby : 0f;
        var zoneShare = lowZones > 0 ? serviceZones / (float)lowZones : 0f;
        Check(doors > 2000 && lobby > 1000 && Mathf.Abs(share - FrontRoomsMapWorld.BackOfHouseShare) <= ShareTolerance,
            "census: " + boh + " of " + lobby + " Lobby doors are back-of-house, " + (share * 100f).ToString("0.0") + " % (share " + FrontRoomsMapWorld.BackOfHouseShare.ToString("0.000")
            + " +- " + ShareTolerance.ToString("0.00") + "), over 20 seeds x 81 chunks; " + doors + " map doors, " + office + " Office");
        Check(lowZones > 200 && Mathf.Abs(zoneShare - FrontRoomsMapWorld.BackOfHouseShare) <= ZoneShareTolerance,
            "census: " + serviceZones + " of " + lowZones + " Low zones with a Lobby door are service zones, " + (zoneShare * 100f).ToString("0.0") + " % (+- " + ZoneShareTolerance.ToString("0.00") + ")");
        Check(office > 0 && officeTrue == 0, "never an Office door (" + office + " Office doors, " + officeTrue + " true)");
        Check(nonDoor > 0 && nonDoorTrue == 0 && otherTrue == 0, "never an edge that is not a door (" + nonDoor + " edges, " + nonDoorTrue + " true) or a door that is not Low | Standard ("
            + otherPair + " doors, " + otherTrue + " true)");
        Check(asym == 0, "the same answer from either cell (" + (doors + nonDoor) + " edges, " + asym + " differ)");
        Check(boh > 0 && kitBad == 0, "the kit's member today is Lobby for every back-of-house door (" + boh + " doors, " + kitBad + " not)");
        Check(border > 0 && copyBad == 0, "both chunk copies of every border door are a door (" + border + " border doors, " + copyBad + " differ)");
        Check(orderDiff == 0, "a second cache generating the chunks in the reverse order gives every door the same answer (" + doors + " doors, " + orderDiff + " differ)");
        Check(borderLost == 0 && kept > 0 && interiorKept > 0 && shiftDiff == 0, "after a revisit shift of all 81 chunks every border door is still a door (" + borderLost + " lost) and every door that still is one keeps its answer ("
            + kept + " kept, " + interiorKept + " of them interior, " + interiorLost + " interior doors gone with the new revision, " + shiftDiff + " changed)");
        Check(result.sample != null, "census: a back-of-house door well inside its chunk for the EXIT and stairwell checks" + (result.sample != null ? " (seed " + result.seed + ", " + result.sample.a + "|" + result.sample.b + ")" : ""));

        Note("CENSUS 20 seeds x 81 chunks (the 9 x 9 chunks round chunk (0, 0)), cache only:");
        Note("map doors " + doors + " (" + (doors / floors).ToString("0.0") + " a floor, " + (doors / chunks).ToString("0.00") + " a chunk): Office " + office + ", Lobby " + lobby
            + " (" + (lobby / floors).ToString("0.0") + " a floor, " + minLobby + "-" + maxLobby + ")");
        Note("back-of-house " + boh + " = " + (share * 100f).ToString("0.0") + " % of Lobby, " + (100f * boh / Mathf.Max(1, doors)).ToString("0.0") + " % of all map doors; "
            + (boh / floors).ToString("0.0") + " a floor (" + minFloor + "-" + maxFloor + "), " + (boh / chunks).ToString("0.00") + " a chunk");
        Note("Low zones with a Lobby door " + lowZones + " (" + (lowZones / floors).ToString("0.0") + " a floor), service zones " + serviceZones + " (" + (serviceZones / floors).ToString("0.0") + " a floor, "
            + (zoneShare * 100f).ToString("0.0") + " %)");
        Note("border doors " + border + "; after the shift " + kept + " kept (" + interiorKept + " interior), " + interiorLost + " interior gone");
        foreach (var l in lines) Note(l);
        if (planDir != null) File.WriteAllText(Path.Combine(planDir, "boh_census.txt"), string.Join("\n", report.census) + "\n");
    }

    // ---------- The exclusions ----------

    static void ExitDoors(List<GameObject> roots, List<FrontRoomsLevelProfile> profiles, CensusResult census)
    {
        // 1. The floor's own EXIT as the planner picks it (floor 1's band, a start door at the spawn rows' cell (4, 4)).
        const int seed = 516574485;
        var w = CacheWorld(roots, profiles, seed, -130f, "BOH DOOR TEST / exit", out var cache);
        var plan = FrontRoomsExitPlanner.ChooseExit(cache, new CellRect(0, 0, 0, 0), new GridCoord(4, 4), seed, FrontRoomsExitPlanner.Floor1BandMin, FrontRoomsExitPlanner.Floor1BandMax);
        if (!plan.found) { Check(false, "exit: the planner found no EXIT on seed " + seed); return; }
        var bare = CacheWorld(roots, profiles, seed, -131f, "BOH DOOR TEST / exit bare", out _);
        w.SetExitArea(plan.exitCell);
        var x = plan.exitCell;
        var n = new GridCoord(x.x, x.y - 1);
        int examined = 0, yes = 0, bitten = 0;
        for (var y = n.y - 3; y <= n.y + 3; y++)
        for (var xx = n.x - 3; xx <= n.x + 3; xx++)
        {
            var a = new GridCoord(xx, y);
            for (var d = 0; d < 2; d++)
            {
                var b = Step(a, d);
                if (Chebyshev(a, n) > FrontRoomsMapWorld.ExitLampHoldCells && Chebyshev(b, n) > FrontRoomsMapWorld.ExitLampHoldCells && a != x && b != x) continue;
                if (cache.Edge(a, b) != EdgeKind.Door && !((a == n && b == x) || (a == x && b == n))) continue;
                examined++;
                if (w.BackOfHouseDoor(Probe(a, b)) || w.BackOfHouseDoor(Probe(b, a))) yes++;
                if (bare.BackOfHouseDoor(Probe(a, b))) bitten++;
            }
        }
        Check(examined > 0 && yes == 0, "exit: never the EXIT door " + n + "|" + x + " or a door in its 2-cell approach (seed " + seed + ": " + examined + " doors, " + yes + " true; "
            + bitten + " of them back-of-house on the same floor with no EXIT)");

        // 2. The exclusion bites: X placed so a census back-of-house door lies in the approach.
        if (census.sample == null) { Check(false, "exit: no back-of-house sample door from the census"); return; }
        var s = census.sample;
        var w2 = CacheWorld(roots, profiles, census.seed, -132f, "BOH DOOR TEST / exit forced", out _);
        var before = w2.BackOfHouseDoor(Probe(s.a, s.b));
        w2.SetExitArea(new GridCoord(s.a.x, s.a.y + 2));
        var after = w2.BackOfHouseDoor(Probe(s.a, s.b));
        Check(before && !after, "exit: the back-of-house door " + s.a + "|" + s.b + " (seed " + census.seed + ") turns Lobby once X stands at " + new GridCoord(s.a.x, s.a.y + 2)
            + " (N " + new GridCoord(s.a.x, s.a.y + 1) + "; before " + before + ", after " + after + ")");
    }

    static void StairwellDoors(List<GameObject> roots, List<FrontRoomsLevelProfile> profiles, CensusResult census)
    {
        if (census.sample == null) { Check(false, "stairwell: no back-of-house sample door from the census"); return; }
        var s = census.sample;
        var w = CacheWorld(roots, profiles, census.seed, -134f, "BOH DOOR TEST / stairwell", out var cache);
        var before = w.BackOfHouseDoor(Probe(s.a, s.b));
        // A later floor begins in the last floor's X: a one-cell start area whose north door opens onto the sample door's cell.
        var start = new GridCoord(s.a.x, s.a.y - 1);
        w.SetStartArea(new RectInt(start.x, start.y, 1, 1), s.a);
        var after = w.BackOfHouseDoor(Probe(s.a, s.b));
        int examined = 0, yes = 0;
        for (var y = s.a.y - 3; y <= s.a.y + 3; y++)
        for (var xx = s.a.x - 3; xx <= s.a.x + 3; xx++)
        {
            var a = new GridCoord(xx, y);
            for (var d = 0; d < 2; d++)
            {
                var b = Step(a, d);
                if (Chebyshev(a, s.a) > FrontRoomsMapWorld.ExitLampHoldCells && Chebyshev(b, s.a) > FrontRoomsMapWorld.ExitLampHoldCells) continue;
                if (cache.Edge(a, b) != EdgeKind.Door) continue;
                examined++;
                if (w.BackOfHouseDoor(Probe(a, b))) yes++;
            }
        }
        Check(before && !after && yes == 0, "stairwell: with a later floor's one-cell start at " + start + " (door cell " + s.a + ") no door within 2 cells of the door is back-of-house ("
            + examined + " doors, " + yes + " true; the sample " + s.a + "|" + s.b + " was " + before + ")");
    }

    static void StationDoors(List<GameObject> roots, List<FrontRoomsLevelProfile> profiles)
    {
        // Floor 1 as the game makes it (FrontRoomsMapInteractionTests.TransitionStationWorld): the first seed and door row that plans.
        var resolved = FrontRoomsLevelProfiles.Resolve();
        var modules = resolved.ModuleData();
        var template = StartGroupTemplate.From(FrontRoomsMapVerification.StandInStation(4), null, out var why);
        if (template == null) { Check(false, "station: the stand-in station is refused: " + why); return; }
        var doorX = Mathf.FloorToInt(FrontRooms3DGame.TitleCenterX / MapGrid.CellSize);
        var seed = 0;
        var door = default(GridCoord);
        for (var s = 20261001; s < 20261061 && seed == 0; s++)
        for (var t = 0; t < 2 && seed == 0; t++)
        {
            var dc = new GridCoord(doorX, 2 + 4 * t);
            var probe = FrontRoomsExitPlanner.CacheFor(resolved.generation, modules, s, 1);
            if (FrontRoomsStartGroup.Plan(probe, template, dc).Ready) { seed = s; door = dc; }
        }
        if (seed == 0) { Check(false, "station: no start planned on 60 seeds"); return; }
        var world = World(roots, Profile(profiles, seed), -136f, "BOH DOOR TEST / station");
        world.BuildForCapture();
        world.NextFloor(seed, 1, new RectInt(0, 0, 0, 0), default(GridCoord));
        var plan = FrontRoomsStartGroup.Plan(world.Cache, template, door);
        if (!plan.Ready) { Check(false, "station: seed " + seed + " door " + door + " did not plan on the map's cache"); return; }
        var l = plan.layout;
        world.SetStartArea(new RectInt(l.strip.x, l.strip.y, l.strip.w, l.strip.h), l.door);
        if (!world.SetStation(plan, FrontRoomsMapVerification.StandInStation(4))) { Check(false, "station: SetStation refused the plan"); return; }
        world.SetExitArea(plan.exit.exitCell);
        var bare = CacheWorld(roots, profiles, seed, -137f, "BOH DOOR TEST / station bare", out _);
        int examined = 0, yes = 0, bitten = 0;
        for (var y = l.strip.y - 1; y <= l.exit.y + 1; y++)
        for (var x = l.strip.x - 3; x <= l.strip.x + l.strip.w + 2; x++)
        {
            var a = new GridCoord(x, y);
            for (var d = 0; d < 2; d++)
            {
                var b = Step(a, d);
                if (!(l.Protected(a) || l.Protected(b) || l.InStrip(a) || l.InStrip(b))) continue;
                if (world.Cache.Edge(a, b) != EdgeKind.Door) continue;
                examined++;
                if (world.BackOfHouseDoor(Probe(a, b))) yes++;
                if (ByZone(bare, a, b)) bitten++;
            }
        }
        Check(yes == 0, "station: no door of the floor-1 station's group (station, corridor, X, strip) is back-of-house (seed " + seed + ", door " + door + ": " + examined + " doors, "
            + yes + " true, " + bitten + " on a service zone's border)");
    }

    static void RunDoors(List<GameObject> roots, List<FrontRoomsLevelProfile> profiles)
    {
        // A Run door room forced as the interaction tests force it (RunCardWorld, card Door 9, seed 20263113).
        const int seed = 20263113;
        var profile = Profile(profiles, seed);
        profile.runRooms = true;
        profile.foil = false;
        profile.alarmDoor = false;
        var world = World(roots, profile, -138f, "BOH DOOR TEST / run");
        var keep = FrontRoomsMapWorld.RunRoomsForTools;
        var keepCard = FrontRoomsMapWorld.RunCardForTools;
        FrontRoomsMapWorld.RunRoomsForTools = 2;
        FrontRoomsMapWorld.RunCardForTools = RunCard.Door(9);
        try { world.BuildForCapture(); }
        finally
        {
            FrontRoomsMapWorld.RunRoomsForTools = keep;
            FrontRoomsMapWorld.RunCardForTools = keepCard;
        }
        var centre = world.CellOf(world.SpawnWorldPosition);
        int doors = 0, runDoors = 0, runTrue = 0, runByZone = 0;
        for (var y = centre.y - 24; y <= centre.y + 24; y++)
        for (var x = centre.x - 24; x <= centre.x + 24; x++)
        {
            var a = new GridCoord(x, y);
            for (var d = 0; d < 2; d++)
            {
                var b = Step(a, d);
                var door = world.DoorBetween(a, b);
                if (door == null) continue;
                doors++;
                if (!world.IsRunDoor(door)) continue;
                runDoors++;
                if (world.BackOfHouseDoor(door)) runTrue++;
                if (ByZone(world, door.a, door.b)) runByZone++;
            }
        }
        Check(runDoors > 0 && runTrue == 0, "run: no Run! door is back-of-house (seed " + seed + ", card Door 9: " + doors + " built doors, " + runDoors + " Run doors, " + runTrue + " true, "
            + runByZone + " on a service zone's border)");
    }

    static void PoolDoors(List<GameObject> roots, List<FrontRoomsLevelProfile> profiles)
    {
        // Scene 4's pool route on a map cache (FrontRoomsStructuresPoolRoute: arrival cell (4, 4), door (4, 5), variant 0).
        var library = FrontRoomsPoolLibrary.FromCode();
        if (!library.Complete) { Check(false, "pool: pool modules missing: " + library.Missing); return; }
        var resolved = FrontRoomsLevelProfiles.Resolve();
        var modules = resolved.ModuleData();
        FrontRoomsMapCache cache = null;
        var seed = 0;
        var error = "";
        for (var k = 0; k < 24 && cache == null; k++)
        {
            var s = 20261001 + k;
            var c = FrontRoomsExitPlanner.CacheFor(resolved.generation, modules, s, FrontRoomsPoolPlanner.PoolFloor);
            var plan = FrontRoomsPoolPlanner.Plan(c, library, new CellRect(4, 4, 1, 1), new GridCoord(4, 5), s, FrontRoomsPoolPlanOptions.Variant(0));
            if (plan.Ready) { cache = c; seed = s; }
            else error = plan.error;
        }
        if (cache == null) { Check(false, "pool: no pool route in 24 seeds: " + error); return; }
        var w = World(roots, Profile(profiles, seed), -140f, "BOH DOOR TEST / pool");
        SetCache(w, cache);
        int poolDoors = 0, poolTrue = 0, besideLow = 0;
        for (var y = -16; y <= 80; y++)
        for (var x = -56; x <= 56; x++)
        {
            var a = new GridCoord(x, y);
            for (var d = 0; d < 2; d++)
            {
                var b = Step(a, d);
                var ta = cache.Generator.ZoneOf(a).theme;
                var tb = cache.Generator.ZoneOf(b).theme;
                if (!PoolRules.IsPool(ta) && !PoolRules.IsPool(tb)) continue;
                if (cache.Edge(a, b) != EdgeKind.Door) continue;
                poolDoors++;
                if (w.BackOfHouseDoor(Probe(a, b))) poolTrue++;
                if (cache.Generator.ZoneOf(a).height == ZoneHeight.Low || cache.Generator.ZoneOf(b).height == ZoneHeight.Low) besideLow++;
            }
        }
        Check(poolDoors > 0 && poolTrue == 0, "pool: no door with a pool side is back-of-house (seed " + seed + ": " + poolDoors + " pool doors, " + poolTrue + " true, " + besideLow + " with a Low side)");
    }

    // ---------- Plan pictures (FR_BOH_PLANS) ----------

    const int Px = 17, Margin = 25;
    static readonly Color32 Frame = new Color32(18, 18, 18, 255);
    static readonly Color32 L0Low = new Color32(201, 178, 118, 255), L0Standard = new Color32(238, 228, 194, 255), L0Tall = new Color32(252, 250, 244, 255);
    static readonly Color32 OfficeCell = new Color32(150, 176, 206, 255), PoolCell = new Color32(128, 206, 200, 255);
    static readonly Color32 Hatch = new Color32(150, 40, 110, 255);
    static readonly Color32 LobbyDoor = new Color32(104, 56, 24, 255), OfficeDoor = new Color32(22, 66, 150, 255), BohDoor = new Color32(232, 0, 128, 255), Outline = new Color32(12, 12, 12, 255);

    static Color32 Shade(Color32 c, float f) => new Color32((byte)(c.r * f), (byte)(c.g * f), (byte)(c.b * f), 255);

    static Color32 Mix(Color32 a, Color32 b, float t) =>
        new Color32((byte)(a.r + (b.r - a.r) * t), (byte)(a.g + (b.g - a.g) * t), (byte)(a.b + (b.b - a.b) * t), 255);

    static Color32 CellColour(ZoneInfo z)
    {
        if (PoolRules.IsPool(z.theme)) return PoolCell;
        if (z.theme == ZoneTheme.Office) return OfficeCell;
        return z.height == ZoneHeight.Low ? L0Low : z.height == ZoneHeight.Tall ? L0Tall : L0Standard;
    }

    // One panel: the 72 x 72 cells of the 9 x 9 chunks round chunk (0, 0), north up, cells by height and theme, zone borders
    // dark, chunk lines faint, every door a bar across its edge in its member's colour; RULE hatches the service zones and
    // paints the back-of-house doors.
    static void WritePlan(FrontRoomsMapWorld w, FrontRoomsMapCache cache, List<DoorRecord> records, int seed, string path, bool rule)
    {
        var n = (2 * Radius + 1) * MapGrid.ChunkCells;
        var size = n * Px + 2 * Margin;
        var px = new Color32[size * size];
        for (var i = 0; i < px.Length; i++) px[i] = Frame;
        var x0 = -Radius * MapGrid.ChunkCells;
        var y0 = -Radius * MapGrid.ChunkCells;
        var zones = new GridCoord[n * n];
        for (var cy = 0; cy < n; cy++)
        for (var cx = 0; cx < n; cx++)
        {
            var z = cache.Generator.ZoneOf(new GridCoord(x0 + cx, y0 + cy));
            zones[cx + cy * n] = z.id;
            var c = CellColour(z);
            var service = rule && w.BackOfHouseZone(z.id);
            for (var y = 0; y < Px; y++)
            for (var x = 0; x < Px; x++)
            {
                var gx = Margin + cx * Px + x;
                var gy = Margin + cy * Px + y;
                var col = c;
                // Service zones: diagonal hatch, 2 px lines every 6 px across the panel (continuous over cells).
                if (service && ((gx + gy) % 6) < 2) col = Mix(c, Hatch, .55f);
                px[gx + gy * size] = col;
            }
        }
        // Chunk lines (faint), then zone borders (dark, 2 px).
        for (var k = 0; k <= 2 * Radius + 1; k++)
        {
            var p = Margin + k * MapGrid.ChunkCells * Px;
            if (p >= size - Margin) p = size - Margin - 1;
            for (var t = Margin; t < size - Margin; t++)
            {
                px[p + t * size] = Shade(px[p + t * size], .86f);
                px[t + p * size] = Shade(px[t + p * size], .86f);
            }
        }
        for (var cy = 0; cy < n; cy++)
        for (var cx = 0; cx < n; cx++)
        {
            var id = zones[cx + cy * n];
            if (cx + 1 < n && zones[cx + 1 + cy * n] != id)
            {
                var gx = Margin + (cx + 1) * Px;
                for (var y = 0; y < Px; y++)
                for (var t = -1; t <= 0; t++)
                {
                    var i = gx + t + (Margin + cy * Px + y) * size;
                    px[i] = Shade(px[i], .55f);
                }
            }
            if (cy + 1 < n && zones[cx + (cy + 1) * n] != id)
            {
                var gy = Margin + (cy + 1) * Px;
                for (var x = 0; x < Px; x++)
                for (var t = -1; t <= 0; t++)
                {
                    var i = Margin + cx * Px + x + (gy + t) * size;
                    px[i] = Shade(px[i], .55f);
                }
            }
        }
        // Doors: a 6 x 13 px bar across the edge with a 1 px outline, Office and Lobby first, back-of-house last (on top).
        for (var pass = 0; pass < 2; pass++)
            foreach (var r in records)
            {
                var boh = rule && r.boh;
                if ((pass == 1) != boh) continue;
                var col = boh ? BohDoor : r.office ? OfficeDoor : LobbyDoor;
                var east = r.b.x == r.a.x + 1;
                var cx = r.a.x - x0;
                var cy = r.a.y - y0;
                if (cx < 0 || cy < 0 || cx >= n || cy >= n) continue;
                int xa, xb, ya, yb;
                if (east)
                {
                    var ex = Margin + (cx + 1) * Px;
                    xa = ex - 3; xb = ex + 2;
                    ya = Margin + cy * Px + 2; yb = ya + Px - 5;
                }
                else
                {
                    var ey = Margin + (cy + 1) * Px;
                    ya = ey - 3; yb = ey + 2;
                    xa = Margin + cx * Px + 2; xb = xa + Px - 5;
                }
                for (var y = ya - 1; y <= yb + 1; y++)
                for (var x = xa - 1; x <= xb + 1; x++)
                {
                    if (x < 0 || y < 0 || x >= size || y >= size) continue;
                    var edge = x < xa || x > xb || y < ya || y > yb;
                    px[x + y * size] = edge ? Outline : col;
                }
            }
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        try
        {
            tex.SetPixels32(px);
            tex.Apply(false);
            File.WriteAllBytes(path, tex.EncodeToPNG());
        }
        finally { UnityEngine.Object.DestroyImmediate(tex); }
    }
}
