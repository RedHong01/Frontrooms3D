using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using FrontRooms.Map;
using UnityEditor;
using UnityEngine;

/// <summary>
/// The wall-cue route (FrontRoomsWayfinding, MAP_ROUTE_SPEC.md v1.0.2 section 8) and its writer
/// (FrontRoomsWayCueDriver, CUE_DRIVER_INTERFACE.md v0.2.4; RELAY_PURSUIT_REDESIGN.md 9.6), checked without playing.
/// - Hand-made grids: the pre-review's two minimal cases (F1: a built half the target pass never reaches gets the
///   frontier rule; F2: no two-cell loop at the cap's edge), F1 again through an unbuilt column, glass and a locked
///   door, a random sweep (walls, doors, glass, an unbuilt ring; caps 24/36/48 m), BeaconPath's metres (F7), and the
///   pressure route of 9.6 (the Relay between the player and the only door: no pressure dirs, no stepping row; a door
///   away from it: chosen, and a calm row that leads past the Relay does not step).
/// - 100 seeds x floors 1-3 as the game plans them (floor 1 from the title's door, later floors from the last EXIT;
///   built = the chunks within buildRadius 2 of the floor's start door): R1, R2, R3, R4 (the built map moved 3 chunks
///   off N), R7 per zone height, R8 (fresh instances in the same order, a fresh cache, rebuild ms), and with a
///   SIMULATED station (M4 is not built: floor 1 the first room, A26; floors 2+ a cell 10-20 cells from the arrival,
///   off the EXIT route, 6+ from N) R5 and R6.
/// - The driver on a built map: stage clicks, turn-back, the wave's arrival, turn and step times, the retract, the
///   texel and _FR_WayOrigin, toroidal slots, the Relay exclusion at turn start, precedence, Caught, resets, Reduce
///   Motion, 9.6's no-door case, and a re-point within one rebuild.
/// Writes Verification/wayfinding-tests.json. Headless: -executeMethod FrontRoomsWayfindingTests.RunBatch -quit
/// (throws on FAIL).
/// </summary>
public static class FrontRoomsWayfindingTests
{
    const int FirstSeed = 20261001;
    const int SeedCount = 100;
    const int Floors = 3;
    const int BuildRadius = 2;
    const float Dt = 1f / 60f;
    const float Tie = 1e-3f;
    // Floor 1's start as FrontRoomsMapVerification makes it (the title's door, its strip of stream rooms behind it).
    const int TitleHalfCells = 2;
    static GridCoord TitleDoorWorldCell => new GridCoord(
        Mathf.FloorToInt(FrontRooms3DGame.TitleCenterX / MapGrid.CellSize),
        Mathf.FloorToInt((FrontRoomsRoomStream.RoomLength * 1.5f + MapGrid.CellSize * .5f) / MapGrid.CellSize));
    static int TitleRows => Mathf.RoundToInt(FrontRoomsRoomStream.RoomLength * 2f / MapGrid.CellSize);

    static readonly int[] DirX = FrontRoomsWayfinding.DirX, DirY = FrontRoomsWayfinding.DirY;

    [Serializable]
    sealed class Report
    {
        public string verdict;
        public int passed, failed;
        public List<string> checks = new List<string>();
        public List<string> r7 = new List<string>();
        public List<string> costs = new List<string>();
        public List<string> simulated = new List<string>();
        public List<string> notes = new List<string>();
    }

    static Report report;

    static void Check(bool ok, string what)
    {
        if (ok) report.passed++; else report.failed++;
        report.checks.Add((ok ? "ok   " : "FAIL ") + what);
    }

    [MenuItem("FrontRooms/Map/Test wall-cue route")]
    public static void Run() => Execute(false);

    public static void RunBatch() => Execute(true);

    static void Execute(bool throwOnFail)
    {
        report = new Report();
        var roots = new List<GameObject>();
        var watch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            Synthetic();
            Seeds();
            Driver(roots);
        }
        catch (Exception e)
        {
            Check(false, "exception: " + e.GetType().Name + " " + e.Message + "\n" + e.StackTrace);
        }
        finally
        {
            foreach (var r in roots)
            {
                if (r == null) continue;
                var d = r.GetComponent<FrontRoomsWayCueDriver>();
                if (d != null) d.Unbind();
                var w = r.GetComponent<FrontRoomsMapWorld>();
                if (w != null) w.Release();
                UnityEngine.Object.DestroyImmediate(r);
            }
        }
        report.notes.Add("ran " + watch.Elapsed.TotalSeconds.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " s");
        var pass = report.failed == 0 && report.passed > 0;
        report.verdict = (pass ? "PASS" : "FAIL") + ": " + report.passed + " passed, " + report.failed + " failed";
        var path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Verification", "wayfinding-tests.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonUtility.ToJson(report, true));
        var text = "[WayfindingTests] " + report.verdict + "\n" + string.Join("\n", report.checks) + "\nR7: " + string.Join("\n", report.r7)
            + "\nR8: " + string.Join("\n", report.costs);
        if (pass) Debug.Log(text); else Debug.LogError(text);
        if (throwOnFail && !pass) throw new Exception(report.verdict);
    }

    static string F(float v) => v.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
    static string P(int part, int whole) => whole > 0 ? (100.0 * part / whole).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " %" : "n/a";

    // ---------- Maps ----------

    /// <summary>A hand-made grid: built cells, reserved cells, and an edge kind per edge ('O' open, 'A' arch, 'D' shut door, 'J' ajar door, 'P' open door, 'L' door locked to the player, 'G' glass, 'W' wall).</summary>
    sealed class Grid : FrontRoomsWayfinding.IMap
    {
        public readonly HashSet<GridCoord> built = new HashSet<GridCoord>();
        public readonly HashSet<GridCoord> reserved = new HashSet<GridCoord>();
        readonly Dictionary<long, char> edges = new Dictionary<long, char>();
        public char defaultEdge = 'W';
        public GridCoord centre;
        public int revision = 1;

        static long Key(GridCoord a, GridCoord b)
        {
            if (b.x < a.x || b.y < a.y) { var t = a; a = b; b = t; }
            return ((long)a.x << 33) ^ ((long)(a.y & 0x7fffffff) << 1) ^ (b.x > a.x ? 1L : 0L);
        }

        public void Set(GridCoord a, GridCoord b, char kind) => edges[Key(a, b)] = kind;
        public char KindOf(GridCoord a, GridCoord b) => edges.TryGetValue(Key(a, b), out var k) ? k : defaultEdge;

        public void Fill(int x0, int y0, int x1, int y1)
        {
            for (var y = y0; y <= y1; y++)
            for (var x = x0; x <= x1; x++) built.Add(new GridCoord(x, y));
        }

        static EdgeKind ToEdge(char k) => k == 'O' ? EdgeKind.Open : k == 'A' ? EdgeKind.Arch : k == 'D' || k == 'J' || k == 'P' || k == 'L' ? EdgeKind.Door : k == 'G' ? EdgeKind.Window : EdgeKind.Wall;

        public int Revision => revision;
        public GridCoord Centre => centre;
        public bool IsBuilt(GridCoord c) => built.Contains(c) && !reserved.Contains(c);
        public bool IsReserved(GridCoord c) => reserved.Contains(c);
        public EdgeKind Edge(GridCoord a, GridCoord b) => ToEdge(KindOf(a, b));
        public bool TryGetChunk(GridCoord chunk, out MapChunk data) { data = null; return false; }

        public EdgeKind FrontierEdge(GridCoord b, GridCoord u)
        {
            var k = KindOf(b, u);
            return k == 'L' ? EdgeKind.Wall : ToEdge(k);
        }

        public float Crossing(GridCoord from, GridCoord to, out EdgeKind edge)
        {
            var k = KindOf(from, to);
            edge = ToEdge(k);
            switch (k)
            {
                case 'O':
                case 'A':
                case 'P': return 0f;
                case 'J': return FrontRoomsMapPathField.AjarOpening;
                case 'D': return FrontRoomsMapPathField.ShutOpening;
                default: return float.PositiveInfinity;
            }
        }

        public bool Shuttable(GridCoord a, GridCoord b)
        {
            var k = KindOf(a, b);
            return k == 'D' || k == 'J' || k == 'P';
        }

        public Vector3 CellCentre(GridCoord c) => new Vector3((c.x + .5f) * MapGrid.CellSize, 0f, (c.y + .5f) * MapGrid.CellSize);
        public Vector3 CrossingPoint(GridCoord a, GridCoord b) => (CellCentre(a) + CellCentre(b)) * .5f;
    }

    /// <summary>A floor as the planner leaves it, read from its cache: the chunks within the radius of a centre built, the start area and X reserved, every door shut (as a floor starts).</summary>
    sealed class CacheMap : FrontRoomsWayfinding.IMap
    {
        public readonly FrontRoomsMapCache cache;
        readonly CellRect area;
        readonly GridCoord exitCell, centre;
        readonly HashSet<GridCoord> chunks = new HashSet<GridCoord>();

        public CacheMap(FrontRoomsMapCache cache, CellRect area, GridCoord exitCell, GridCoord centreCell, int radius)
        {
            this.cache = cache;
            this.area = area;
            this.exitCell = exitCell;
            centre = centreCell;
            var cc = MapGrid.ChunkOf(centreCell);
            for (var dy = -radius; dy <= radius; dy++)
            for (var dx = -radius; dx <= radius; dx++)
            {
                var chunk = new GridCoord(cc.x + dx, cc.y + dy);
                chunks.Add(chunk);
                cache.Get(chunk);
            }
        }

        public int Revision => 1;
        public GridCoord Centre => centre;
        public bool IsReserved(GridCoord c) => FrontRoomsExitPlanner.InArea(area, c) || c == exitCell;
        public bool IsBuilt(GridCoord c) => !IsReserved(c) && chunks.Contains(MapGrid.ChunkOf(c));
        public EdgeKind Edge(GridCoord a, GridCoord b) => cache.Edge(a, b);
        public bool TryGetChunk(GridCoord chunk, out MapChunk data) => cache.TryGetGenerated(chunk, out data);
        public EdgeKind FrontierEdge(GridCoord b, GridCoord u) => FrontRoomsWayfinding.FrontierEdgeOf(cache, b, u);

        public float Crossing(GridCoord from, GridCoord to, out EdgeKind edge)
        {
            edge = cache.Edge(from, to);
            switch (edge)
            {
                case EdgeKind.Open:
                case EdgeKind.Arch: return 0f;
                case EdgeKind.Door: return FrontRoomsMapPathField.ShutOpening;
                default: return float.PositiveInfinity;
            }
        }

        public bool Shuttable(GridCoord a, GridCoord b) => cache.Edge(a, b) == EdgeKind.Door && a != exitCell && b != exitCell;
        public Vector3 CellCentre(GridCoord c) => new Vector3((c.x + .5f) * MapGrid.CellSize, 0f, (c.y + .5f) * MapGrid.CellSize);
        public Vector3 CrossingPoint(GridCoord a, GridCoord b) => (CellCentre(a) + CellCentre(b)) * .5f;
    }

    // ---------- R1 and R2: the walk along the dirs ----------

    sealed class WalkStats
    {
        public int walks, loops, nonDecreasing, deadEnds, overBound, windowSteps, xSteps, nDir, badExit, toTarget, toFrontier, closer, closerSeed;
        public readonly List<string> examples = new List<string>();
        public void Fail(string what) { if (examples.Count < 6) examples.Add(what); }
        public int Failures => loops + nonDecreasing + deadEnds + overBound + windowSteps + xSteps + nDir + badExit;

        public void Add(WalkStats o)
        {
            walks += o.walks; loops += o.loops; nonDecreasing += o.nonDecreasing; deadEnds += o.deadEnds; overBound += o.overBound;
            windowSteps += o.windowSteps; xSteps += o.xSteps; nDir += o.nDir; badExit += o.badExit; toTarget += o.toTarget;
            toFrontier += o.toFrontier; closer += o.closer; closerSeed += o.closerSeed;
            foreach (var e in o.examples) Fail(e);
        }

        public string Line() => walks + " walks (" + toTarget + " to the target, " + toFrontier + " to a frontier seed): loops " + loops
            + ", non-decreasing steps " + nonDecreasing + ", dead ends " + deadEnds + ", over the step bound " + overBound + ", window steps " + windowSteps
            + ", steps into X " + xSteps + ", N not into X " + nDir + ", bad exits " + badExit + (examples.Count > 0 ? " [" + string.Join(" | ", examples) + "]" : "");
    }

    /// <summary>
    /// R1 from every cell with message 1 (and the target): it reaches the target or steps out of the built map from a
    /// frontier seed over a walkable edge that is not glass, within ceil(sqrt2 x field / 3) + 2 steps; never a window
    /// step, never into X but from N, never a loop. R2 on every step inside the routed map: field(next) + the step's door
    /// cost is below field(cell). Memoised, so each cell is walked once.
    /// </summary>
    static void Walks(FrontRoomsWayfinding way, FrontRoomsWayfinding.IMap map, Func<GridCoord, GridCoord, EdgeKind> trueEdge, GridCoord? exitCell, WalkStats st,
        byte[] endOut = null)
    {
        const int Slots = FrontRoomsWayfinding.Slots;
        var mark = new byte[Slots];
        var steps = new int[Slots];
        var end = new byte[Slots];
        var endAt = new GridCoord[Slots];
        var seedAt = new GridCoord[Slots];
        var path = new List<int>(256);
        var target = way.Target;
        for (var s0 = 0; s0 < Slots; s0++)
        {
            var c = way.CellAtSlot(s0);
            var isTarget = way.HasTarget && c == target && way.RegionAt(c) != 0;
            if (!way.Route(c, out _) && !isTarget) continue;
            st.walks++;
            if (mark[s0] != 2)
            {
                path.Clear();
                var x = c;
                byte e;
                var tail = 0;
                var ea = default(GridCoord);
                var sa = default(GridCoord);
                var memo = false;
                while (true)
                {
                    var xs = FrontRoomsWayfinding.Slot(x);
                    if (mark[xs] == 2) { e = end[xs]; tail = steps[xs] + 1; ea = endAt[xs]; sa = seedAt[xs]; memo = true; break; }
                    if (mark[xs] == 1) { st.loops++; st.Fail("loop at " + x); e = 3; break; }
                    mark[xs] = 1;
                    path.Add(xs);
                    if (way.HasTarget && x == target)
                    {
                        if (!way.TargetIsStation && (!way.Route(x, out var nd) || nd != 1)) { st.nDir++; st.Fail("N " + x + " does not point into X"); }
                        e = 1;
                        ea = x;
                        break;
                    }
                    if (!way.Route(x, out var d)) { st.deadEnds++; st.Fail("dead end at " + x); e = 3; break; }
                    var n = new GridCoord(x.x + DirX[d], x.y + DirY[d]);
                    var edge = trueEdge(x, n);
                    if (edge == EdgeKind.Window) { st.windowSteps++; st.Fail("window step " + x + " to " + n); e = 3; break; }
                    if (exitCell.HasValue && n == exitCell.Value) { st.xSteps++; st.Fail("step into X from " + x); e = 3; break; }
                    if (way.RegionAt(n) == 0)
                    {
                        if (way.IsFrontierSeed(x) && !map.IsBuilt(n) && !map.IsReserved(n) && edge != EdgeKind.Wall) { e = 2; ea = n; sa = x; tail = 1; break; }
                        st.badExit++;
                        st.Fail("leaves the routed map at " + x + " to " + n + " (" + edge + ", seed " + way.IsFrontierSeed(x) + ")");
                        e = 3;
                        break;
                    }
                    var cost = map.Crossing(x, n, out _);
                    if (!(way.FieldAt(n) + cost < way.FieldAt(x) - Tie))
                    {
                        st.nonDecreasing++;
                        st.Fail("field " + F(way.FieldAt(x)) + " at " + x + " to " + F(way.FieldAt(n)) + " (+" + F(cost) + ") at " + n);
                    }
                    x = n;
                }
                // Unwind: the last cell of the path is tail steps from the end (one more when it stopped on a memo).
                var last = path.Count - 1;
                for (var i = last; i >= 0; i--)
                {
                    var ps = path[i];
                    mark[ps] = 2;
                    end[ps] = e;
                    endAt[ps] = ea;
                    seedAt[ps] = sa;
                    steps[ps] = (memo ? tail : (e == 2 ? 1 : 0)) + (last - i);
                }
            }
            var es = end[s0];
            if (es == 3) continue;
            var bound = Mathf.CeilToInt(Mathf.Sqrt(2f) * way.FieldAt(c) / MapGrid.CellSize) + 2;
            if (steps[s0] > bound) { st.overBound++; st.Fail(steps[s0] + " steps from " + c + " (field " + F(way.FieldAt(c)) + ", bound " + bound + ")"); }
            if (es == 1) st.toTarget++;
            else
            {
                st.toFrontier++;
                // The spec's measure is the seed cell; the remainder is measured from the unbuilt cell it opens onto (both logged).
                if (way.HasTarget && FrontRoomsWayfinding.Straight(endAt[s0], target) < FrontRoomsWayfinding.Straight(c, target)) st.closer++;
                if (way.HasTarget && FrontRoomsWayfinding.Straight(seedAt[s0], target) < FrontRoomsWayfinding.Straight(c, target)) st.closerSeed++;
            }
        }
        if (endOut != null) Array.Copy(end, endOut, FrontRoomsWayfinding.Slots);
    }

    static int Steps(FrontRoomsWayfinding way, GridCoord from, GridCoord to, Func<GridCoord, GridCoord, EdgeKind> trueEdge, out bool window)
    {
        window = false;
        var c = from;
        for (var i = 0; i <= 200; i++)
        {
            if (c == to) return i;
            if (!way.Route(c, out var d)) return -1;
            var n = new GridCoord(c.x + DirX[d], c.y + DirY[d]);
            if (trueEdge(c, n) == EdgeKind.Window) window = true;
            c = n;
        }
        return -1;
    }

    // ---------- Hand-made grids ----------

    static void Synthetic()
    {
        // F1, the pre-review's minimal case: x, y in 1..10, all open but a wall line between x 5 and 6; T (9, 5).
        {
            var g = new Grid { defaultEdge = 'O', centre = new GridCoord(12, 12) };
            g.Fill(1, 1, 10, 10);
            for (var y = 0; y <= 11; y++) g.Set(new GridCoord(5, y), new GridCoord(6, y), 'W');
            var way = new FrontRoomsWayfinding(g);
            way.SetStationTarget(new GridCoord(9, 5), -1);
            way.Rebuild();
            var routed = 0;
            for (var y = 1; y <= 10; y++)
            for (var x = 1; x <= 5; x++)
                if (way.Route(new GridCoord(x, y), out _) && way.RegionAt(new GridCoord(x, y)) == 2) routed++;
            var st = new WalkStats();
            Walks(way, g, g.Edge, null, st);
            Check(routed == 50 && st.Failures == 0 && st.toFrontier >= 50,
                "F1 minimal: the west half the target never reaches is frontier-routed, " + routed + "/50 cells; " + st.Line());
        }
        // F1 through an unbuilt column, glass and a locked door: each joins the west part to the target only that way.
        {
            var names = new[] { "an unbuilt column", "glass", "a locked door" };
            for (var v = 0; v < 3; v++)
            {
                var g = new Grid { defaultEdge = 'O', centre = new GridCoord(12, 12) };
                g.Fill(1, 1, 11, 10);
                if (v == 0) for (var y = 1; y <= 10; y++) g.built.Remove(new GridCoord(6, y));
                else
                {
                    for (var y = 0; y <= 11; y++) g.Set(new GridCoord(5, y), new GridCoord(6, y), 'W');
                    g.Set(new GridCoord(5, 5), new GridCoord(6, 5), v == 1 ? 'G' : 'L');
                }
                var way = new FrontRoomsWayfinding(g);
                way.SetStationTarget(new GridCoord(9, 5), -1);
                way.Rebuild();
                var routed = 0;
                for (var y = 1; y <= 10; y++)
                for (var x = 1; x <= 5; x++)
                    if (way.Route(new GridCoord(x, y), out _) && way.RegionAt(new GridCoord(x, y)) == 2) routed++;
                var st = new WalkStats();
                Walks(way, g, g.Edge, null, st);
                Check(routed == 50 && st.Failures == 0, "F1 regression: a built part joined to the target only through " + names[v] + " takes the frontier rule (" + routed + "/50 routed); " + st.Line());
            }
        }
        // F2, the pre-review's minimal case: a 2 x 2 block at the cap's edge, the corner reached only diagonally in the frontier pass.
        {
            var g = new Grid { defaultEdge = 'W', centre = new GridCoord(12, 12) };
            g.Fill(1, 1, 10, 10);
            void O(int ax, int ay, int bx, int by) => g.Set(new GridCoord(ax, ay), new GridCoord(bx, by), 'O');
            O(1, 1, 2, 1); O(1, 1, 1, 2); O(2, 1, 2, 2); O(1, 2, 2, 2); O(2, 2, 3, 2); O(1, 1, 0, 1);
            for (var y = 10; y > 1; y--) O(10, y, 10, y - 1);
            for (var x = 10; x > 2; x--) O(x, 1, x - 1, 1);
            for (var x = 10; x > 1; x--) O(x, 10, x - 1, 10);
            for (var y = 10; y > 2; y--) O(1, y, 1, y - 1);
            var way = new FrontRoomsWayfinding(g, 52f);
            way.SetStationTarget(new GridCoord(10, 10), -1);
            way.Rebuild();
            var st = new WalkStats();
            Walks(way, g, g.Edge, null, st);
            var corner = way.RegionAt(new GridCoord(2, 2));
            var near = way.RegionAt(new GridCoord(1, 1));
            Check(st.Failures == 0 && corner == 1 && near == 1,
                "F2 minimal (cap 52 m): no two-cell loop at the cap's edge; with no cap on the target pass (S1) the corner and its neighbour past 52 m keep the target's route (regions " + corner + ", " + near + "); " + st.Line());
        }
        // A random sweep: 28 x 28 grids, walls, shut doors and glass, an unbuilt ring round them, caps 24/36/48 m.
        {
            var total = new WalkStats();
            var grids = 0;
            var frontierRouted = 0;
            for (var seed = 0; seed < 200; seed++)
            {
                var caps = new[] { 24f, 36f, 48f };
                foreach (var capM in caps)
                {
                    var rnd = new System.Random(seed * 7 + 1);
                    var g = new Grid { defaultEdge = 'O', centre = new GridCoord(14, 14) };
                    g.Fill(0, 0, 27, 27);
                    for (var x = -1; x <= 28; x++)
                    for (var y = -1; y <= 28; y++)
                    {
                        for (var k = 0; k < 2; k++)
                        {
                            var r = rnd.NextDouble();
                            var kind = r < .12 ? 'W' : r < .17 ? 'G' : r < .25 ? 'D' : 'O';
                            g.Set(new GridCoord(x, y), k == 0 ? new GridCoord(x + 1, y) : new GridCoord(x, y + 1), kind);
                        }
                    }
                    var t = new GridCoord(rnd.Next(3, 25), rnd.Next(3, 25));
                    var way = new FrontRoomsWayfinding(g, capM);
                    way.SetStationTarget(t, -1);
                    way.Rebuild();
                    Walks(way, g, g.Edge, null, total);
                    frontierRouted += way.FrontierCells;
                    grids++;
                }
            }
            Check(total.Failures == 0, "F1/F2 sweep: " + grids + " grids (28 x 28, walls, doors, glass, unbuilt ring; caps 24/36/48 m), " + frontierRouted + " frontier-routed cells: " + total.Line());
        }
        Beacon();
        Hysteresis();
        Pressure();
    }

    // Review m8: ResetHysteresis gives a fresh build's dirs (a floor never leans on the last floor's ties).
    static void Hysteresis()
    {
        var g = new Grid { defaultEdge = 'O', centre = new GridCoord(12, 12) };
        g.Fill(0, 0, 11, 11);
        // Target (8, 0) leaves -z on the diagonal (9, 2), (8, 3), ...; target (11, 0) ties +x and -z there.
        var a = new FrontRoomsWayfinding(g);
        a.SetStationTarget(new GridCoord(8, 0), -1);
        a.Rebuild();
        a.SetStationTarget(new GridCoord(11, 0), -1);
        a.Rebuild();
        var carried = a.Hash();
        var tieDir = a.Route(new GridCoord(8, 3), out var td) ? td : -1;
        var fresh = new FrontRoomsWayfinding(g);
        fresh.SetStationTarget(new GridCoord(11, 0), -1);
        fresh.Rebuild();
        a.ResetHysteresis();
        a.Rebuild();
        var resetDir = a.Route(new GridCoord(8, 3), out var rd) ? rd : -1;
        Check(carried != fresh.Hash() && a.Hash() == fresh.Hash() && tieDir == 3 && resetDir == 0,
            "m8 hysteresis: after another target a tie keeps the old dir ((8, 3): " + tieDir + ", the fresh build differs); after ResetHysteresis the build is the fresh one's (" + resetDir + ", hash equal)");
    }

    // F7: BeaconPath's metres are the route's octile metres with no door costs, plus the straight remainder.
    static void Beacon()
    {
        var g = new Grid { defaultEdge = 'O', centre = new GridCoord(12, 12) };
        g.Fill(0, 0, 11, 11);
        var way = new FrontRoomsWayfinding(g);
        way.SetStationTarget(new GridCoord(0, 0), 2);
        way.Rebuild();
        var ok = way.BeaconPath(new GridCoord(5, 3), out var metres, out var shut, out _);
        var octile = MapGrid.CellSize * (3f * Mathf.Sqrt(2f) + 2f);
        var tvDir = way.Route(new GridCoord(0, 0), out var tdir) ? tdir : -1;
        Check(ok && Mathf.Abs(metres - octile) < .01f && shut == 0 && tvDir == 2,
            "F7 beacon: an open room, (5, 3) to the TV at (0, 0): " + F(metres) + " m = octile " + F(octile) + " m (3 m a step along the dirs would say " + F(8 * MapGrid.CellSize) + "), " + shut
            + " shut openings; the station cell points at the TV's wall (dir " + tvDir + ", m10a)");

        var corridor = new Grid { defaultEdge = 'W', centre = new GridCoord(12, 12) };
        corridor.Fill(0, 0, 6, 0);
        for (var x = 0; x < 6; x++) corridor.Set(new GridCoord(x, 0), new GridCoord(x + 1, 0), x == 3 ? 'D' : 'O');
        var way2 = new FrontRoomsWayfinding(corridor);
        way2.SetStationTarget(new GridCoord(0, 0), -1);
        way2.Rebuild();
        var ok2 = way2.BeaconPath(new GridCoord(6, 0), out var m2, out var shut2, out _);
        var tvNone = !way2.Route(new GridCoord(0, 0), out _) && way2.RegionAt(new GridCoord(0, 0)) == 1;
        Check(ok2 && Mathf.Abs(m2 - 18f) < .01f && shut2 == 1 && Mathf.Abs(way2.FieldAt(new GridCoord(6, 0)) - 27f) < .01f && tvNone,
            "F7 beacon: through one shut door, 6 cells: " + F(m2) + " m with no door cost (field " + F(way2.FieldAt(new GridCoord(6, 0))) + " m), " + shut2
            + " shut opening (counted on the metres' own route, m4); the station cell with the TV on no edge is message 0 (" + tvNone + ", m10a)");

        var open = new Grid { defaultEdge = 'O', centre = new GridCoord(12, 12) };
        open.Fill(0, 0, 6, 0);
        for (var x = 0; x < 7; x++)
        {
            open.Set(new GridCoord(x, 0), new GridCoord(x, 1), 'W');
            open.Set(new GridCoord(x, 0), new GridCoord(x, -1), 'W');
        }
        open.Set(new GridCoord(-1, 0), new GridCoord(0, 0), 'W');
        var way3 = new FrontRoomsWayfinding(open);
        way3.SetStationTarget(new GridCoord(10, 0), -1);
        way3.Rebuild();
        var ok3 = way3.BeaconPath(new GridCoord(2, 0), out var m3, out _, out _);
        Check(ok3 && Mathf.Abs(m3 - 21f) < .01f, "F7 beacon: the TV beyond the built map: 4 cells to the edge + 9 m straight remainder = " + F(m3) + " m");

        way.SetStationKeyTaken(true);
        way.Rebuild();
        Check(!way.BeaconPath(new GridCoord(5, 3), out _, out _, out _), "beacon off once the key is taken (A20)");

        // Review m5: the station sent again after the take (another dir) leaves the key taken, the field on N; a new
        // floor (ClearStation) starts untaken.
        way.SetExit(new GridCoord(6, 12));
        way.SetStationTarget(new GridCoord(0, 0), 1);
        way.Rebuild();
        var kept = way.StationKeyTaken && !way.TargetIsStation && way.Target == new GridCoord(6, 11) && way.Route(new GridCoord(6, 11), out var nd) && nd == 1;
        way.ClearStation();
        way.SetStationTarget(new GridCoord(0, 0), 2);
        way.Rebuild();
        var fresh = !way.StationKeyTaken && way.TargetIsStation;
        Check(kept && fresh, "m5 key switch: the station re-sent after the take with another dir keeps the key taken and the field on N (" + kept + "); ClearStation starts a new floor's station, untaken (" + fresh + ")");
    }

    // RELAY 9.6 (the system-design ruling, 2026-10-22): the pressure route never leads toward the Relay.
    static void Pressure()
    {
        Grid Corridor(bool doorAway)
        {
            var g = new Grid { defaultEdge = 'W', centre = new GridCoord(12, 12) };
            g.Fill(0, 0, 20, 0);
            for (var x = 0; x < 20; x++) g.Set(new GridCoord(x, 0), new GridCoord(x + 1, 0), x == 16 || (doorAway && x == 1) ? 'D' : 'O');
            return g;
        }
        var relay = new GridCoord(10, 0);
        var player = new GridCoord(4, 0);
        Func<GridCoord, float> distance = c => MapGrid.CellSize * Mathf.Abs(c.x - relay.x);

        var g1 = Corridor(false);
        var w1 = new FrontRoomsWayfinding(g1) { RelayDistance = distance };
        w1.SetStationTarget(new GridCoord(20, 0), -1);
        w1.Rebuild();
        var calmHash = w1.Hash();
        w1.BuildPressure(relay, player);
        var dirs = 0;
        var steps = 0;
        for (var x = 0; x <= 20; x++)
        {
            var c = new GridCoord(x, 0);
            if (w1.PressureRoute(c, out _)) dirs++;
            for (var k = 0; k < 4; k++) if (w1.StepAllowed(c, k)) steps++;
        }
        Check(!w1.PressureReachesPlayer && dirs == 0 && steps == 0 && w1.Hash() == calmHash,
            "9.6: the Relay at x 10 between the player at x 4 and the only shuttable door (16|17): no pressure dirs (" + dirs + "), no stepping row (" + steps
            + "), the calm field untouched");

        var g2 = Corridor(true);
        var w2 = new FrontRoomsWayfinding(g2) { RelayDistance = distance };
        w2.SetStationTarget(new GridCoord(20, 0), -1);
        w2.Rebuild();
        w2.BuildPressure(relay, player);
        var playerDir = w2.PressureRoute(player, out var pd) ? pd : -1;
        var pastRelay = 0;
        // x 6..14: nearer the Relay than the player's distance less one cell (x 5 is within the slack and leads away).
        for (var x = 6; x <= 14; x++) if (w2.PressureRoute(new GridCoord(x, 0), out _)) pastRelay++;
        var calmPlayerSteps = w2.StepAllowed(player, 0);
        var farSteps = w2.StepAllowed(new GridCoord(18, 0), 0);
        var chaseDir = w2.ChaseRoute(player, out var cdir) ? cdir : -1;
        Check(w2.PressureReachesPlayer && playerDir == 2 && chaseDir == 2 && w2.StepAllowed(player, 2) && pastRelay == 0 && !calmPlayerSteps && farSteps,
            "9.6: a door away from the Relay (1|2) is chosen: the player's dir " + playerDir + " (-x; ChaseRoute " + chaseDir + "), it steps; no pressure cell nearer the Relay than a cell of slack ("
            + pastRelay + "); the calm +x row at the player, which leads past the Relay, does not step (" + calmPlayerSteps + "); a calm row beyond it, leading away, does (" + farSteps + ")");

        // Review M1: a shut door off the corridor at (5, 0)|(5, 1) seeds a pressure dir (+z) at (5, 0), a room-set cell,
        // but the player's cell (18 m from the Relay; (5, 0) is 15 m, the door's reach) is never reached: no door
        // qualifies, so every room-set cell keeps its calm turn and none steps.
        {
            var g3 = Corridor(false);
            g3.built.Add(new GridCoord(5, 1));
            g3.Set(new GridCoord(5, 0), new GridCoord(5, 1), 'D');
            Func<GridCoord, float> manhattan = c => MapGrid.CellSize * (Mathf.Abs(c.x - relay.x) + Mathf.Abs(c.y - relay.y));
            var w3 = new FrontRoomsWayfinding(g3) { RelayDistance = manhattan };
            w3.SetStationTarget(new GridCoord(20, 0), -1);
            w3.Rebuild();
            w3.BuildPressure(relay, player);
            var room3 = new List<GridCoord>();
            FrontRoomsWayfinding.RoomSet(g3, player, room3);
            var seededOff = w3.PressureRoute(new GridCoord(5, 0), out var sd) && sd == 1;
            var turnedOff = 0;
            var steps3 = 0;
            foreach (var c in room3)
            {
                var calmOk = w3.Route(c, out var cd);
                var chaseOk = w3.ChaseRoute(c, out var chd);
                if (calmOk != chaseOk || cd != chd) turnedOff++;
                for (var k = 0; k < 4; k++) if (w3.StepAllowed(c, k)) steps3++;
            }
            Check(!w3.PressureReachesPlayer && seededOff && room3.Contains(new GridCoord(5, 0)) && turnedOff == 0 && steps3 == 0,
                "9.6 / M1: a door off the room set seeds a pressure dir at (5, 0) (" + seededOff + ") the player cannot use (no door qualifies: " + !w3.PressureReachesPlayer + "): all "
                + room3.Count + " room-set cells keep their calm turn (" + turnedOff + " differ), none steps (" + steps3 + ")");
        }

        // An open door just passed (3|4, the player at 3, the Relay at x 10) and a shut door ahead (0|1): the door
        // behind is no target and its far cell joins no route, so the player's arrows lead on to the door ahead and no
        // two arrows point at each other (the draft looped 3 <-> 4 here, toward the Relay).
        {
            var g4 = Corridor(false);
            g4.Set(new GridCoord(3, 0), new GridCoord(4, 0), 'P');
            g4.Set(new GridCoord(0, 0), new GridCoord(1, 0), 'D');
            var p4 = new GridCoord(3, 0);
            var w4 = new FrontRoomsWayfinding(g4) { RelayDistance = distance };
            w4.SetStationTarget(new GridCoord(20, 0), -1);
            w4.Rebuild();
            w4.BuildPressure(relay, p4);
            var dir4 = w4.ChaseRoute(p4, out var d4) ? d4 : -1;
            var seen = new HashSet<GridCoord>();
            var c4 = p4;
            var loop = false;
            while (w4.PressureRoute(c4, out var pd4))
            {
                if (!seen.Add(c4)) { loop = true; break; }
                c4 = new GridCoord(c4.x + DirX[pd4], c4.y + DirY[pd4]);
            }
            var loops = 0;
            for (var x = 0; x <= 20; x++)
            {
                var c = new GridCoord(x, 0);
                if (!w4.PressureRoute(c, out var a)) continue;
                var n = new GridCoord(x + DirX[a], 0);
                if (w4.PressureRoute(n, out var b) && b == ((a + 2) & 3)) loops++;
            }
            Check(w4.PressureReachesPlayer && dir4 == 2 && !w4.StepAllowed(p4, 0) && !loop && c4 == new GridCoord(0, 0) && loops == 0,
                "9.6: an open door just passed (3|4) is no target: the player's dir " + dir4 + " (-x, on to the door 0|1), the walk ends through it at " + c4 + " (loop " + loop
                + "); 0 facing pairs (" + loops + "); the +x row back toward the Relay does not step");
        }
    }

    // ---------- 100 seeds x 3 floors ----------

    sealed class Totals
    {
        public readonly WalkStats calm = new WalkStats(), frontier = new WalkStats(), station = new WalkStats(), take = new WalkStats();
        public int floors, noExit, targetUnbuilt, r3Bad, r8Bad, r8FreshBad, r5Bad, r5Ties, r6Floor1Rooms, r6Floor1Bad, r6Later, r6LaterBad, noStation, takeLines, takeBad, noSightLine;
        public int r4Floors, r4NotAllFrontier, r6Floor1Cells, r6Floor1Cut;
        public readonly int[] built = new int[3], message0 = new int[3], frontierCells = new int[3], targetCells = new int[3];
        // R8: every single build's ms (the first, cold, build of each floor and its two warm rebuilds): the pass column's
        // max. Logged beside it: per floor the faster of the two warm rebuilds, and the cold builds on their own.
        public readonly List<float> all = new List<float>(), ms = new List<float>(), firstMs = new List<float>();
        public readonly List<string> examples = new List<string>();
        public void Fail(string what) { if (examples.Count < 8) examples.Add(what); }
    }

    static void Seeds()
    {
        var profile = FrontRoomsLevelProfiles.Resolve();
        var modules = profile.ModuleData();
        var t = new Totals();
        // Warm the code once (JIT), so R8's times are the rebuild's own.
        {
            var g = new Grid { defaultEdge = 'O', centre = new GridCoord(12, 12) };
            g.Fill(0, 0, 30, 30);
            var w = new FrontRoomsWayfinding(g);
            w.SetStationTarget(new GridCoord(3, 3), -1);
            for (var i = 0; i < 3; i++) w.Rebuild();
        }
        for (var i = 0; i < SeedCount; i++)
        {
            var settings = profile.generation.Clone();
            settings.seed = unchecked(FirstSeed + i * 7919);
            var previous = new ExitPlan();
            for (var f = 1; f <= Floors; f++)
            {
                FrontRoomsMapCache cache;
                CellRect area;
                GridCoord door;
                int floorSeed;
                if (f == 1)
                {
                    cache = FrontRoomsExitPlanner.CacheFor(settings, modules, settings.seed, f);
                    var root = FrontRoomsExitPlanner.RootFor(cache, TitleDoorWorldCell, TitleHalfCells, TitleRows);
                    door = root.doorCell;
                    area = new CellRect(door.x - TitleHalfCells, door.y - TitleRows, TitleHalfCells * 2 + 1, TitleRows);
                    floorSeed = settings.seed;
                }
                else
                {
                    var seedPlan = FrontRoomsExitPlanner.SeedFor(settings.seed, f, previous.exitCell, settings, modules, f, out cache);
                    floorSeed = seedPlan.seed;
                    door = previous.NextDoorCell;
                    area = previous.NextStartArea;
                }
                FrontRoomsExitPlanner.BandFor(f, out var lo, out var hi);
                var plan = FrontRoomsExitPlanner.ChooseExit(cache, area, door, floorSeed, lo, hi);
                if (!plan.found) { t.noExit++; break; }
                Floor(t, settings, modules, cache, area, door, plan, f, floorSeed);
                previous = plan;
            }
        }

        Check(t.noExit == 0 && t.floors == SeedCount * Floors, "seeds: " + SeedCount + " seeds x " + Floors + " floors planned, " + t.floors + " floors with an EXIT (" + t.noExit + " without)");
        Check(t.calm.Failures == 0, "R1/R2 (target N, key taken or no station; built = buildRadius 2 round the start door): " + t.calm.Line());
        Check(t.r3Bad == 0, "R3: the calm field is the same with the Relay at 5 places and states (fresh instances, " + t.floors * 5 + " builds): " + t.r3Bad + " differ");
        Check(t.r4Floors == t.floors && t.frontier.Failures == 0 && t.r4NotAllFrontier == 0 && t.frontier.toTarget == 0,
            "R4 (the built map moved 3 chunks off N, its chunk unbuilt; " + t.r4Floors + " floors): every walk ends on a frontier seed over a walkable non-glass edge: " + t.frontier.Line());
        var low = t.frontier.toFrontier > 0 && t.frontier.closerSeed < .9 * t.frontier.toFrontier;
        report.notes.Add("R4 share of walks whose seed cell is straight-closer to N than the start (the spec's measure): " + P(t.frontier.closerSeed, t.frontier.toFrontier)
            + (low ? " (below 90 %: REPORTED)" : "") + "; measured from the unbuilt cell the seed opens onto: " + P(t.frontier.closer, t.frontier.toFrontier));
        Check(t.r8Bad == 0 && t.r8FreshBad == 0, "R8 determinism: fresh instances in the same order hash alike (" + t.r8Bad + " differ); a fresh cache generated in another order too (" + t.r8FreshBad + " differ)");
        Stats(t.all, out var mean, out var p95, out var max);
        Stats(t.ms, out var warmMean, out var warmP95, out var warmMax);
        Stats(t.firstMs, out var firstMean, out _, out var firstMax);
        report.costs.Add("calm rebuild, every build (buildRadius 2, editor batch, CacheMap; " + t.all.Count + " builds = " + t.firstMs.Count + " floors x a cold first build + 2 warm rebuilds): mean "
            + F(mean) + " ms, p95 " + F(p95) + " ms, max " + F(max) + " ms; cold first builds mean " + F(firstMean) + ", max " + F(firstMax) + " ms; per floor the faster warm rebuild mean "
            + F(warmMean) + ", p95 " + F(warmP95) + ", max " + F(warmMax) + " ms");
        Check(t.all.Count > 0 && max <= 4f, "R8 cost: every rebuild <= 4 ms on the batch maps: max " + F(max) + " ms over " + t.all.Count + " builds (" + t.firstMs.Count
            + " floors, cold first builds included; mean " + F(mean) + ", p95 " + F(p95) + ")");
        var names = new[] { "Low", "Standard", "Tall" };
        for (var h = 0; h < 3; h++)
        {
            var share0 = t.built[h] > 0 ? (float)t.message0[h] / t.built[h] : 0f;
            report.r7.Add(names[h] + ": " + t.built[h] + " built cells, message 0 " + P(t.message0[h], t.built[h]) + ", frontier-routed " + P(t.frontierCells[h], t.built[h])
                + ", target-routed " + P(t.targetCells[h], t.built[h]) + (h == 2 && share0 > .25f ? " (Tall message-0 share above 25 %: REPORTED, a map rule, not the cue)" : ""));
        }
        Check(t.station.Failures == 0, "SIMULATED R1/R2 with a station target (key not taken): " + t.station.Line());
        Check(t.r6Floor1Bad == 0 && t.r6Floor1Rooms > 0, "SIMULATED R6 floor 1 (A26): R1 from every first-room cell the player walks to ends at the TV: " + t.r6Floor1Rooms + " rooms, "
            + t.r6Floor1Cells + " cells, " + t.r6Floor1Bad + " not (" + t.r6Floor1Cut + " cells of the rooms' rects cut off from the door by walls or glass, left out)");
        Check(t.r6LaterBad == 0 && t.r6Later > 0, "SIMULATED R6 floors 2+: R1 from the arrival cell ends at the station TV, not N: " + (t.r6Later - t.r6LaterBad) + "/" + t.r6Later + " (" + t.noStation + " floors had no M4-like cell)");
        Check(t.r5Bad == 0, "SIMULATED R5: after StationKeyTaken one rebuild leads every cell to N (no dir follows the station field; " + t.r5Ties + " dirs differ from a fresh N build only by a kept tie): " + t.r5Bad + " bad");
        Check(t.takeBad == 0 && t.takeLines > 0, "SIMULATED R6 after the take (floor 1): from the doorway on the EXIT sight line to N in <= 8 steps, no window: " + (t.takeLines - t.takeBad) + "/" + t.takeLines
            + " (" + t.noSightLine + " floors with no 3-cell clear line south of N)");
        report.simulated.Add("M4 is not built: the station is simulated. Floor 1 (A26): the room set of the start door cell, the TV in its cell furthest from the door, on a wall edge if it has one. Floors 2+: the built cell with the least hash among those 10-20 walking cells from the arrival, 6+ from N, on no shortest arrival-N route.");
        report.simulated.Add("R6 after the take: the doorway is the furthest cell, up to 6, straight south of N over Open or Arch edges (a clear sight line), needing 3; M4 will place the station so.");
        if (t.examples.Count > 0) report.notes.Add("examples: " + string.Join(" | ", t.examples));
    }

    static void Stats(List<float> values, out float mean, out float p95, out float max)
    {
        values.Sort();
        max = values.Count > 0 ? values[values.Count - 1] : 0f;
        p95 = values.Count > 0 ? values[Mathf.Min(values.Count - 1, (int)(values.Count * .95f))] : 0f;
        mean = 0f;
        foreach (var m in values) mean += m;
        mean = values.Count > 0 ? mean / values.Count : 0f;
    }

    static void Floor(Totals t, MapSettings settings, IReadOnlyList<RoomModuleData> modules, FrontRoomsMapCache cache, CellRect area, GridCoord door, ExitPlan plan, int f, int floorSeed)
    {
        t.floors++;
        var x = plan.exitCell;
        var n = plan.frontCell;
        var map = new CacheMap(cache, area, x, door, BuildRadius);
        Func<GridCoord, GridCoord, EdgeKind> edge = (a, b) => cache.Edge(a, b);
        var at = "seed " + settings.seed + " floor " + f + ": ";

        // Target N (key taken, or no station).
        var way = new FrontRoomsWayfinding(map);
        way.SetExit(x);
        way.Rebuild();
        // R8: every build counts toward the max (spec: rebuild <= 4 ms, log max), the cold first build of a fresh
        // field included; the faster warm rebuild per floor is logged as well.
        var first = way.LastBuildMs;
        way.Rebuild();
        var warm1 = way.LastBuildMs;
        way.Rebuild();
        var warm2 = way.LastBuildMs;
        t.all.Add(first);
        t.all.Add(warm1);
        t.all.Add(warm2);
        t.ms.Add(Mathf.Min(warm1, warm2));
        t.firstMs.Add(first);
        if (!map.IsBuilt(n)) t.targetUnbuilt++;
        var st = new WalkStats();
        Walks(way, map, edge, x, st);
        t.calm.Add(st);
        // R7: per zone height, the built cells with message 0, frontier-routed, target-routed.
        for (var s = 0; s < FrontRoomsWayfinding.Slots; s++)
        {
            var c = way.CellAtSlot(s);
            if (!map.IsBuilt(c)) continue;
            var h = (int)cache.ZoneOf(c).height;
            if (h < 0 || h > 2) continue;
            t.built[h]++;
            if (!way.Route(c, out _)) t.message0[h]++;
            else if (way.RegionAt(c) == 2) t.frontierCells[h]++;
            else t.targetCells[h]++;
        }
        var hash = way.Hash();
        // R8: a fresh instance, and a fresh cache that generated other chunks first.
        var again = new FrontRoomsWayfinding(map);
        again.SetExit(x);
        again.Rebuild();
        if (again.Hash() != hash) { t.r8Bad++; t.Fail(at + "R8 fresh instance differs"); }
        var fresh = FrontRoomsExitPlanner.CacheFor(settings, modules, floorSeed, f);
        var far = MapGrid.ChunkOf(n);
        fresh.Get(new GridCoord(far.x + 2, far.y - 3));
        fresh.Get(new GridCoord(far.x - 1, far.y + 2));
        var freshMap = new CacheMap(fresh, area, x, door, BuildRadius);
        var freshWay = new FrontRoomsWayfinding(freshMap);
        freshWay.SetExit(x);
        freshWay.Rebuild();
        if (freshWay.Hash() != hash) { t.r8FreshBad++; t.Fail(at + "R8 fresh cache differs"); }
        // R3: the Relay at 5 places and states (chasing: the pressure route built too): the calm field never moves.
        var rnd = new System.Random(settings.seed * 31 + f);
        for (var j = 0; j < 5; j++)
        {
            var relay = new GridCoord(door.x + rnd.Next(-16, 17), door.y + rnd.Next(-16, 17));
            var scale = 1f + j;
            var w = new FrontRoomsWayfinding(map) { RelayDistance = c => scale * FrontRoomsWayfinding.Straight(c, relay) };
            w.SetExit(x);
            w.Rebuild();
            if ((j & 1) == 1) w.BuildPressure(relay, door);
            if (w.Hash() != hash) { t.r3Bad++; t.Fail(at + "R3 differs with the Relay at " + relay); }
        }

        // R4: the built map 3 chunks off N toward the start, N's chunk unbuilt.
        {
            var cn = MapGrid.ChunkOf(n);
            var cd = MapGrid.ChunkOf(door);
            var sx = Math.Sign(cd.x - cn.x);
            var sy = Math.Sign(cd.y - cn.y);
            if (sx == 0 && sy == 0) sy = -1;
            var centreChunk = new GridCoord(cn.x + 3 * sx, cn.y + 3 * sy);
            var o = MapGrid.ChunkOrigin(centreChunk);
            var map4 = new CacheMap(cache, area, x, new GridCoord(o.x + 4, o.y + 4), BuildRadius);
            if (!map4.IsBuilt(n))
            {
                t.r4Floors++;
                var w4 = new FrontRoomsWayfinding(map4);
                w4.SetExit(x);
                w4.Rebuild();
                var s4 = new WalkStats();
                Walks(w4, map4, edge, x, s4);
                if (s4.toTarget > 0 || s4.toFrontier == 0) { t.r4NotAllFrontier++; t.Fail(at + "R4 " + s4.toTarget + " walks to the target, " + s4.toFrontier + " to seeds"); }
                t.frontier.Add(s4);
            }
        }

        // SIMULATED station (M4 is not built).
        GridCoord tv;
        int tvDir;
        List<GridCoord> firstRoom = null;
        if (f == 1)
        {
            firstRoom = new List<GridCoord>();
            FrontRoomsWayfinding.RoomSet(map, door, firstRoom);
            if (firstRoom.Count == 0) { t.noStation++; return; }
            var inRoom = new HashSet<GridCoord>(firstRoom);
            var d = Bfs(map, door, inRoom);
            tv = door;
            var best = -1;
            foreach (var c in firstRoom)
                if (d.TryGetValue(c, out var v) && v > best) { best = v; tv = c; }
        }
        else if (!LaterStation(map, door, n, settings.seed, out tv))
        {
            t.noStation++;
            return;
        }
        tvDir = WallDir(map, tv);
        var ws = new FrontRoomsWayfinding(map);
        ws.SetExit(x);
        ws.SetStationTarget(tv, tvDir);
        ws.Rebuild();
        var sst = new WalkStats();
        var ends = new byte[FrontRoomsWayfinding.Slots];
        Walks(ws, map, edge, x, sst, ends);
        t.station.Add(sst);
        if (f == 1)
        {
            t.r6Floor1Rooms++;
            // The room's cells the player walks to (a module's rect can hold a cell walled or glazed off from the rest).
            var walkable = Bfs(map, door);
            foreach (var c in firstRoom)
            {
                if (!walkable.ContainsKey(c)) { t.r6Floor1Cut++; continue; }
                t.r6Floor1Cells++;
                if (ends[FrontRoomsWayfinding.Slot(c)] != 1 || !(ws.Route(c, out _) || c == tv))
                {
                    t.r6Floor1Bad++;
                    t.Fail(at + "R6 first-room cell " + c + " does not end at the TV " + tv + " (end " + ends[FrontRoomsWayfinding.Slot(c)] + ", region " + ws.RegionAt(c)
                        + ", field " + F(ws.FieldAt(c)) + ", edge to the TV side " + (Mathf.Abs(c.x - tv.x) + Mathf.Abs(c.y - tv.y) == 1 ? cache.Edge(c, tv).ToString() : "-") + ")");
                }
            }
        }
        else
        {
            t.r6Later++;
            if (ends[FrontRoomsWayfinding.Slot(door)] != 1) { t.r6LaterBad++; t.Fail(at + "R6 the arrival " + door + " does not end at the TV " + tv); }
        }

        // R5: the take, one rebuild: every dir is a best step of the N field, and the messages are a fresh N build's.
        ws.SetStationKeyTaken(true);
        var rebuilt = ws.Refresh();
        var bad = !rebuilt || ws.TargetIsStation || ws.Target != n || ws.Refresh();
        for (var s = 0; s < FrontRoomsWayfinding.Slots && !bad; s++)
        {
            var c = ws.CellAtSlot(s);
            var r1 = ws.Route(c, out var d1);
            var r2 = way.Route(c, out var d2);
            if (r1 != r2) { bad = true; t.Fail(at + "R5 message differs at " + c); break; }
            if (!r1 || d1 == d2) continue;
            var bestV = float.PositiveInfinity;
            for (var k = 0; k < 4; k++) bestV = Mathf.Min(bestV, ws.StepValue(c, k));
            if (ws.StepValue(c, d1) > bestV + Tie) { bad = true; t.Fail(at + "R5 " + c + " keeps a station dir"); }
            else t.r5Ties++;
        }
        if (bad) t.r5Bad++;

        // R6 after the take (floor 1): from the doorway on the EXIT sight line to N.
        if (f == 1)
        {
            var k = 0;
            var line = n;
            while (k < 6)
            {
                var next = new GridCoord(line.x, line.y - 1);
                if (!map.IsBuilt(next)) break;
                var e = cache.Edge(line, next);
                if (e != EdgeKind.Open && e != EdgeKind.Arch) break;
                line = next;
                k++;
            }
            if (k < 3) t.noSightLine++;
            else
            {
                t.takeLines++;
                var steps = Steps(ws, line, n, edge, out var window);
                if (steps < 0 || steps > 8 || window) { t.takeBad++; t.Fail(at + "R6 take: " + steps + " steps from " + line + " to N" + (window ? ", through glass" : "")); }
            }
        }
    }

    // Walking cells from a cell over built cells (any crossing but walls, glass and locks), optionally inside a set.
    static Dictionary<GridCoord, int> Bfs(FrontRoomsWayfinding.IMap map, GridCoord from, HashSet<GridCoord> inside = null)
    {
        var dist = new Dictionary<GridCoord, int>();
        if (!map.IsBuilt(from)) return dist;
        var q = new Queue<GridCoord>();
        dist[from] = 0;
        q.Enqueue(from);
        while (q.Count > 0)
        {
            var c = q.Dequeue();
            var d = dist[c];
            for (var k = 0; k < 4; k++)
            {
                var n = new GridCoord(c.x + DirX[k], c.y + DirY[k]);
                if (dist.ContainsKey(n) || !map.IsBuilt(n) || (inside != null && !inside.Contains(n))) continue;
                if (float.IsPositiveInfinity(map.Crossing(c, n, out _))) continue;
                dist[n] = d + 1;
                q.Enqueue(n);
            }
        }
        return dist;
    }

    // M4 on floors 2+ (simulated): 10-20 walking cells from the arrival, 6+ from N, off every shortest arrival-N route.
    static bool LaterStation(FrontRoomsWayfinding.IMap map, GridCoord arrival, GridCoord n, int seed, out GridCoord tv)
    {
        tv = default;
        var fromA = Bfs(map, arrival);
        var fromN = Bfs(map, n);
        var route = fromA.TryGetValue(n, out var an) ? an : -1;
        var found = false;
        var bestHash = uint.MaxValue;
        foreach (var pair in fromA)
        {
            var c = pair.Key;
            if (pair.Value < 10 || pair.Value > 20) continue;
            var dn = fromN.TryGetValue(c, out var v) ? v : int.MaxValue;
            if (dn < 6) continue;
            if (route >= 0 && dn != int.MaxValue && pair.Value + dn == route) continue;
            uint h;
            unchecked { h = (uint)(c.x * 73856093 ^ c.y * 19349663 ^ seed) * 2654435761u; }
            if (h < bestHash) { bestHash = h; tv = c; found = true; }
        }
        return found;
    }

    // The TV hangs on a wall of its cell when it has one (the first in +x, +z, -x, -z), else on none (-1).
    static int WallDir(FrontRoomsWayfinding.IMap map, GridCoord c)
    {
        for (var k = 0; k < 4; k++)
        {
            var n = new GridCoord(c.x + DirX[k], c.y + DirY[k]);
            var e = map.IsBuilt(n) ? map.Edge(c, n) : map.FrontierEdge(c, n);
            if (e == EdgeKind.Wall) return k;
        }
        return -1;
    }

    // ---------- The driver ----------

    static void Driver(List<GameObject> roots)
    {
        var profile = FrontRoomsLevelProfiles.Resolve();
        var root = new GameObject("WAYFINDING TEST / driver") { hideFlags = HideFlags.DontSave };
        roots.Add(root);
        root.transform.position = new Vector3(-57f * ModuleUnits.WorldPeriod, 0f, -27f * ModuleUnits.WorldPeriod);
        var world = root.AddComponent<FrontRoomsMapWorld>();
        world.Profile = profile;
        world.BuildForCapture();
        var hunter = new FrontRoomsMapHunter(world, new FrontRoomsHunterTuning { sightRange = 0f }, null, null, 7);
        var driver = root.AddComponent<FrontRoomsWayCueDriver>();
        var reduce = FrontRoomsWayCueDriver.ReduceMotionForTools;
        try
        {
            FrontRoomsWayCueDriver.ReduceMotionForTools = false;
            driver.Bind(world, hunter);
            DriverChecks(world, hunter, driver);
        }
        finally
        {
            FrontRoomsWayCueDriver.ReduceMotionForTools = reduce;
            driver.Unbind();
        }
    }

    static void DriverChecks(FrontRoomsMapWorld world, FrontRoomsMapHunter hunter, FrontRoomsWayCueDriver driver)
    {
        var eye = world.Player;
        var mid = world.CellOf(world.SpawnWorldPosition);
        var room = new List<GridCoord>();
        var player = default(GridCoord);
        var tv = default(GridCoord);
        var tvBack = default(GridCoord);
        var found = false;
        for (var y = mid.y - 8; y <= mid.y + 8 && !found; y++)
        for (var x = mid.x - 8; x <= mid.x + 8 && !found; x++)
        {
            var p = new GridCoord(x, y);
            if (!world.IsBuilt(p)) continue;
            FrontRoomsWayfinding.RoomSet(world, p, room);
            if (room.Count < 4) continue;
            for (var k = 0; k < 4 && !found; k++)
            {
                var a = new GridCoord(p.x + 9 * DirX[k], p.y + 9 * DirY[k]);
                var b = new GridCoord(p.x - 9 * DirX[k], p.y - 9 * DirY[k]);
                if (!world.IsBuilt(a) || !world.IsBuilt(b) || room.Contains(a) || room.Contains(b)) continue;
                eye.position = world.CellCenter(p);
                driver.ResetForTools();
                driver.SetStationTarget(a, -1);
                driver.TickForTools(Dt);
                var routed = 0;
                foreach (var c in room) if (driver.Way.Route(c, out _)) routed++;
                if (routed < 4) continue;
                player = p;
                tv = a;
                tvBack = b;
                found = true;
            }
        }
        if (!found) { Check(false, "driver: no room of 4+ routed cells near the spawn"); return; }
        var way = driver.Way;
        FrontRoomsWayfinding.RoomSet(world, player, room);
        var routedRoom = new List<GridCoord>();
        foreach (var c in room) if (way.Route(c, out _)) routedRoom.Add(c);
        string Where() => " (player " + player + ", room " + room.Count + " cells, " + routedRoom.Count + " routed)";

        void Until(float time)
        {
            for (var guard = 0; guard < 100000 && driver.Clock < time - 1e-5f; guard++) driver.TickForTools(Mathf.Min(.01f, time - driver.Clock));
        }
        bool All(List<GridCoord> cells, int state)
        {
            foreach (var c in cells) if (driver.StateOf(c) != state) return false;
            return true;
        }
        int MaxState()
        {
            var m = 0;
            for (var s = 0; s < FrontRoomsWayfinding.Slots; s++) m = Mathf.Max(m, driver.StateOf(way.CellAtSlot(s)));
            return m;
        }
        int OffZero()
        {
            var m = 0;
            for (var s = 0; s < FrontRoomsWayfinding.Slots; s++) if (driver.StateOf(way.CellAtSlot(s)) != 0) m++;
            return m;
        }

        // Texel, origin, toroidal slots.
        {
            var c = routedRoom[0];
            way.Route(c, out var d);
            var texel = driver.TexelOf(c);
            var px = (Color32)driver.CellsTexture.GetPixel(c.x & 63, c.y & 63);
            var unrouted = -1;
            for (var s = 0; s < FrontRoomsWayfinding.Slots && unrouted < 0; s++)
            {
                var u = way.CellAtSlot(s);
                if (!way.Route(u, out _)) unrouted = s;
            }
            var unroutedA = unrouted >= 0 ? driver.TexelOf(way.CellAtSlot(unrouted)).a : 0;
            var origin = Shader.GetGlobalVector("_FR_WayOrigin");
            var rootPos = world.transform.position;
            var wrap = FrontRoomsWayfinding.Slot(new GridCoord(-1, -1)) == FrontRoomsWayfinding.Slots - 1
                && FrontRoomsWayfinding.Slot(c) == FrontRoomsWayfinding.Slot(new GridCoord(c.x + 64, c.y - 128))
                && way.CellAtSlot(FrontRoomsWayfinding.Slot(c)) == c;
            Check(texel.r == 0 && texel.g == 0 && texel.b == 0 && texel.a == d + 4 && px.a == texel.a && unroutedA == 0
                && Mathf.Abs(origin.x - rootPos.x) < .01f && Mathf.Abs(origin.y - rootPos.z) < .01f && Mathf.Abs(origin.z - MapGrid.CellSize) < .001f && origin.w == 1f && wrap,
                "driver texel: an idle routed cell is (0, 0, 0, dir + 4 = " + texel.a + "), the texture's (x & 63, y & 63) pixel matches (" + px.a + "), message 0 is A 0; _FR_WayOrigin "
                + origin + " = the map root's x/z, 3 m, on; toroidal slots (-1, -1) = 4095, 64 cells apart share one" + Where());
        }

        // Stage 1: the room set's cells turn at +0.20 / +0.50 / +0.80 s, never step; the texel carries the fade.
        var t0 = driver.Clock;
        driver.WarnStageForTools(1);
        Until(t0 + .19f); var at19 = All(routedRoom, 0);
        Until(t0 + .21f); var at21 = All(routedRoom, 1);
        Until(t0 + .51f); var at51 = All(routedRoom, 2);
        Until(t0 + .81f); var at81 = All(routedRoom, 3);
        Until(t0 + 2f);
        var held = All(routedRoom, 3);
        var dirsOk = true;
        foreach (var c in routedRoom) if (!way.Route(c, out var d) || driver.TurnedDir(c) != d) dirsOk = false;
        var tx = driver.TexelOf(routedRoom[0]);
        Check(at19 && at21 && at51 && at81 && held && dirsOk && MaxState() == 3 && tx.r == 2 && tx.g == 3 && tx.b == 255 && tx.a == driver.TurnedDir(routedRoom[0]) + 4,
            "driver stage 1: 0 at +0.19 s, clicks at +0.20/+0.50/+0.80 s (" + at21 + ", " + at51 + ", " + at81 + "), held fitted on the route at +2 s, no stepping; texel (2, 3, 255, dir + 4)" + Where());

        // Stage 0: back at 0.15 s clicks; G7: every cell at 0.
        var t1 = driver.Clock;
        driver.WarnStageForTools(0);
        Until(t1 + .01f); var b2 = All(routedRoom, 2);
        Until(t1 + .14f); var b2b = All(routedRoom, 2);
        Until(t1 + .16f); var b1 = All(routedRoom, 1);
        Until(t1 + .31f); var b0 = All(routedRoom, 0);
        Until(t1 + .5f);
        Check(b2 && b2b && b1 && b0 && OffZero() == 0 && driver.ActiveCells == 0,
            "driver stage 0: turn-back clicks at +0, +0.15, +0.30 s (" + b2 + ", " + b1 + ", " + b0 + "); G7: every cell at 0, none active");

        // The Relay exclusion is tested when a cell starts to turn; a turned cell holds when the Relay comes near.
        {
            // A built cell well away from the room, for the Relay when it must not matter.
            var away = player;
            for (var r = 14; r <= 22 && away == player; r++)
            for (var k = 0; k < 4 && away == player; k++)
            {
                var a = new GridCoord(player.x + r * DirX[k], player.y + r * DirY[k]);
                if (world.IsBuilt(a)) away = a;
            }
            GridCoord? relayAt = null;
            for (var y = player.y - 6; y <= player.y + 6 && !relayAt.HasValue; y++)
            for (var x = player.x - 6; x <= player.x + 6 && !relayAt.HasValue; x++)
            {
                var r = new GridCoord(x, y);
                if (!world.IsBuilt(r) || room.Contains(r)) continue;
                int near = 0, farCount = 0;
                foreach (var c in routedRoom)
                {
                    if (Mathf.Max(Mathf.Abs(c.x - r.x), Mathf.Abs(c.y - r.y)) <= FrontRoomsWayCueDriver.RelayClearCells) near++; else farCount++;
                }
                if (near > 0 && farCount > 0) relayAt = r;
            }
            if (!relayAt.HasValue) Check(false, "driver Relay exclusion: no built cell beside the room with room cells both near and far" + Where());
            else
            {
                hunter.DebugPlace(world.CellCenter(relayAt.Value));
                var t2 = driver.Clock;
                driver.WarnStageForTools(1);
                Until(t2 + 1f);
                var nearCells = new List<GridCoord>();
                var farCells = new List<GridCoord>();
                foreach (var c in routedRoom)
                {
                    if (driver.NearRelayForTools(c)) nearCells.Add(c); else farCells.Add(c);
                }
                var excluded = All(nearCells, 0) && All(farCells, 3) && nearCells.Count > 0 && farCells.Count > 0;
                // The Relay comes to a turned cell: it holds.
                var q = farCells.Count > 0 ? farCells[0] : player;
                hunter.DebugPlace(world.CellCenter(q));
                Until(driver.Clock + 1f);
                var holds = All(farCells, 3);
                // Review m7: the Relay gone and the set recomputed (a field rebuild): the cells it held back stay at 0
                // (one test per turn start, so their turning never reads out its leaving).
                hunter.DebugPlace(world.CellCenter(away));
                way.Invalidate();
                Until(driver.Clock + 1f);
                var stay = All(nearCells, 0) && All(farCells, 3);
                driver.WarnStageForTools(0);
                Until(driver.Clock + .5f);
                Check(excluded && holds && stay && OffZero() == 0,
                    "driver Relay exclusion: with the Relay at " + relayAt.Value + " the " + nearCells.Count + " room cells within 2 cells stay at 0 and the other " + farCells.Count
                    + " turn; the Relay then standing at a turned cell " + q + ": they hold (" + holds + "); the Relay gone and the set recomputed, the held-back cells stay at 0 (" + stay + ", m7)");
            }
            // The Relay well away for the rest.
            hunter.DebugPlace(world.CellCenter(away));
        }

        // Review m10c: at stage 1 the room set follows the player: cells left behind turn back, cells gained turn.
        {
            GridCoord? p2 = null;
            var room2 = new List<GridCoord>();
            var routed2 = new List<GridCoord>();
            for (var r = 3; r <= 10 && !p2.HasValue; r++)
            for (var k = 0; k < 4 && !p2.HasValue; k++)
            {
                var q = new GridCoord(player.x + r * DirX[k], player.y + r * DirY[k]);
                if (!world.IsBuilt(q)) continue;
                FrontRoomsWayfinding.RoomSet(world, q, room2);
                var overlap = false;
                foreach (var c in room2) if (room.Contains(c)) overlap = true;
                if (overlap) continue;
                routed2.Clear();
                foreach (var c in room2) if (way.Route(c, out _) && !driver.NearRelayForTools(c)) routed2.Add(c);
                if (routed2.Count >= 2) p2 = q;
            }
            if (!p2.HasValue) Check(false, "driver room set follows: no second room of 2+ routed cells within 10 cells" + Where());
            else
            {
                driver.WarnStageForTools(1);
                Until(driver.Clock + 1f);
                var before = All(routedRoom, 3);
                eye.position = world.CellCenter(p2.Value);
                Until(driver.Clock + 1f);
                var left = All(routedRoom, 0);
                var gained = All(routed2, 3);
                eye.position = world.CellCenter(player);
                Until(driver.Clock + 1f);
                var backAgain = All(routedRoom, 3) && All(routed2, 0);
                driver.WarnStageForTools(0);
                Until(driver.Clock + .5f);
                Check(before && left && gained && backAgain && OffZero() == 0,
                    "driver room set follows the player (m10c): to " + p2.Value + " the " + routedRoom.Count + " cells left behind turn back (" + left + ") and its " + routed2.Count
                    + " routed cells turn (" + gained + "); back again, the reverse (" + backAgain + ")");
            }
        }

        // A stand-in for the Relay's path distance that rises away from a point 42 m off, one where a door qualifies.
        var real = way.RelayDistance;
        Func<GridCoord, float> standIn = null;
        for (var k = 0; k < 8 && standIn == null; k++)
        {
            var ox = k == 0 ? 1 : k == 1 ? -1 : k == 2 ? 0 : k == 3 ? 0 : k == 4 ? 1 : k == 5 ? -1 : k == 6 ? 1 : -1;
            var oy = k == 0 ? 0 : k == 1 ? 0 : k == 2 ? 1 : k == 3 ? -1 : k == 4 ? 1 : k == 5 ? 1 : k == 6 ? -1 : -1;
            var o = new GridCoord(player.x + 14 * ox, player.y + 14 * oy);
            Func<GridCoord, float> fn = c => FrontRoomsWayfinding.Straight(c, o);
            way.RelayDistance = fn;
            way.BuildPressure(o, player);
            if (way.PressureReachesPlayer) standIn = fn;
        }
        if (standIn == null) { report.notes.Add("driver: no stand-in Relay field let a door qualify near the spawn; the wave's step timing is checked against the hunter's own field"); standIn = real; }
        way.RelayDistance = standIn;

        // The chase wave: arrival at 8 m/s by steps from the room set, turn clicks 0.12 s, steps 4 a second; 9.6 holds.
        {
            var t3 = driver.Clock;
            driver.HunterStateForTools(HunterState.Chase);
            Until(t3 + .01f); var w1 = All(routedRoom, 1);
            Until(t3 + .11f); var w1b = All(routedRoom, 1);
            Until(t3 + .13f); var w2 = All(routedRoom, 2);
            Until(t3 + .25f); var w3 = All(routedRoom, 3);
            // A cell 4-6 steps out turns as the wave arrives (reached well before the retract below).
            GridCoord? outer = null;
            var outerSteps = 0;
            for (var s = 0; s < FrontRoomsWayfinding.Slots && !outer.HasValue; s++)
            {
                var c = way.CellAtSlot(s);
                var st = driver.WaveStepsForTools(c);
                if (st >= 4 && st <= 6 && driver.StateOf(c) == 0) { outer = c; outerSteps = st; }
            }
            var arriveOk = false;
            var arrival = t3 + outerSteps * MapGrid.CellSize / FrontRoomsCueTiming.WaveSpeed;
            if (outer.HasValue)
            {
                Until(arrival - .02f);
                var before = driver.StateOf(outer.Value);
                Until(arrival + .02f);
                arriveOk = before == 0 && driver.StateOf(outer.Value) == 1;
            }
            // Step cadence of a stepping cell, and 9.6 at every sample.
            Until(t3 + 1f);
            GridCoord? stepper = null;
            foreach (var c in routedRoom) if (!stepper.HasValue && driver.SteppingForTools(c)) stepper = c;
            var intervals = new List<float>();
            var allowedOk = true;
            var steppingSeen = 0;
            if (stepper.HasValue)
            {
                var last = driver.StateOf(stepper.Value);
                var lastAt = -1f;
                var end = driver.Clock + 1.6f;
                while (driver.Clock < end - 1e-5f)
                {
                    driver.TickForTools(.01f);
                    var now = driver.StateOf(stepper.Value);
                    if (now != last)
                    {
                        if (lastAt >= 0f) intervals.Add(driver.Clock - lastAt);
                        lastAt = driver.Clock;
                        last = now;
                    }
                }
            }
            for (var s = 0; s < FrontRoomsWayfinding.Slots; s++)
            {
                var c = way.CellAtSlot(s);
                if (!driver.SteppingForTools(c)) continue;
                steppingSeen++;
                if (!way.StepAllowed(c, driver.TurnedDir(c))) allowedOk = false;
            }
            var cadence = intervals.Count >= 3;
            foreach (var iv in intervals) if (Mathf.Abs(iv - FrontRoomsCueTiming.FlowClick) > .015f) cadence = false;
            Check(w1 && w1b && w2 && w3 && (!outer.HasValue || arriveOk) && outer.HasValue,
                "driver wave: the room set turns at +0 / +0.12 / +0.24 s (" + w1 + ", " + w2 + ", " + w3 + "); a cell " + outerSteps + " steps out turns at "
                + F(arrival - t3) + " s (" + arriveOk + ")");
            Check(stepper.HasValue && cadence && allowedOk && steppingSeen > 0,
                "driver wave: rows step 4 clicks a second (" + intervals.Count + " intervals, " + (intervals.Count > 0 ? F(intervals[0]) : "-") + " s), and every one of "
                + steppingSeen + " stepping cells is on a dir 9.6 allows (" + allowedOk + "; a door qualifies: " + way.PressureReachesPlayer + ")");

            // Retract: outside-in over 2 s; each cell ends its loop and turns back; with stage 0 everything ends at 0.
            var t4 = driver.Clock;
            var dmax = 1;
            for (var s = 0; s < FrontRoomsWayfinding.Slots; s++)
            {
                var c = way.CellAtSlot(s);
                var st = driver.WaveStepsForTools(c);
                if (st > dmax && driver.TurnedDir(c) >= 0) dmax = st;
            }
            driver.HunterStateForTools(HunterState.Search);
            var roomAt = driver.RetractAtForTools(routedRoom[0]);
            var outerAt = outer.HasValue ? driver.RetractAtForTools(outer.Value) : float.PositiveInfinity;
            var expectOuter = t4 + (1f - outerSteps / (float)dmax) * FrontRoomsCueTiming.RetractWindow;
            Until(t4 + FrontRoomsCueTiming.RetractWindow + FrontRoomsCueTiming.FlowLoop * FrontRoomsCueTiming.FlowClick + 3f * FrontRoomsCueTiming.TurnBackStep + .2f);
            Check(Mathf.Abs(roomAt - (t4 + FrontRoomsCueTiming.RetractWindow)) < .001f && (!outer.HasValue || Mathf.Abs(outerAt - expectOuter) < .001f) && OffZero() == 0 && driver.ActiveCells == 0,
                "driver retract: the room set's retract at +2 s (" + F(roomAt - t4) + "), a cell " + outerSteps + " of " + dmax + " steps out at +" + F(outerAt - t4)
                + " s (outside-in); 3.95 s later every cell is at 0 (G7)");
        }

        // Precedence and the landing: stage 1, a chase, stage 0 inside it changes nothing; stage 1 again; the retract lands on the stage.
        {
            driver.WarnStageForTools(1);
            Until(driver.Clock + 1f);
            driver.HunterStateForTools(HunterState.Chase);
            Until(driver.Clock + 1f);
            var stepping = new List<GridCoord>();
            foreach (var c in routedRoom) if (driver.SteppingForTools(c)) stepping.Add(c);
            driver.WarnStageForTools(0);
            Until(driver.Clock + .8f);
            var keep = true;
            foreach (var c in stepping) if (!driver.SteppingForTools(c)) keep = false;
            driver.WarnStageForTools(1);
            var t5 = driver.Clock;
            driver.HunterStateForTools(HunterState.Search);
            Until(t5 + 4.5f);
            var landed = true;
            foreach (var c in routedRoom) if (!way.Route(c, out var d) || driver.StateOf(c) != 3 || driver.TurnedDir(c) != d) landed = false;
            var outside = 0;
            for (var s = 0; s < FrontRoomsWayfinding.Slots; s++)
            {
                var c = way.CellAtSlot(s);
                if (!room.Contains(c) && driver.StateOf(c) != 0) outside++;
            }
            Check(keep && stepping.Count > 0 && landed && outside == 0,
                "driver precedence: stage 0 during the wave leaves its " + stepping.Count + " stepping room cells stepping (" + keep + "); the retract with stage 1 lands the room set turned on the calm route ("
                + landed + "), every other cell at 0 (" + outside + " not)");
            driver.WarnStageForTools(0);
            Until(driver.Clock + .5f);
        }

        // Caught: a freeze (no new clicks); then a floor change's reset.
        {
            driver.HunterStateForTools(HunterState.Chase);
            Until(driver.Clock + .6f);
            driver.CaughtForTools();
            var before = new int[FrontRoomsWayfinding.Slots];
            for (var s = 0; s < FrontRoomsWayfinding.Slots; s++) before[s] = driver.StateOf(way.CellAtSlot(s));
            var active = driver.ActiveCells;
            Until(driver.Clock + 2f);
            var same = true;
            for (var s = 0; s < FrontRoomsWayfinding.Slots; s++) if (driver.StateOf(way.CellAtSlot(s)) != before[s]) same = false;
            var off = OffZero();
            driver.ResetForTools();
            var reset = OffZero() == 0 && driver.ActiveCells == 0 && !driver.Frozen && !driver.WaveActive && !way.Station.HasValue;
            Check(same && off > 0 && driver.ActiveCells == 0 && reset,
                "driver Caught: " + off + " cells off 0 hold for 2 s with no new click (" + same + ", " + active + " active); a floor change's reset: every cell 0, unfrozen, no wave, no station (" + reset + ")");
            driver.SetStationTarget(tv, -1);
            driver.TickForTools(Dt);
        }

        // The Relay parked (Dormant: its warning metrics reset): every cell back to 0 at once.
        {
            driver.WarnStageForTools(1);
            Until(driver.Clock + 1f);
            var turned = OffZero();
            driver.HunterStateForTools(HunterState.Dormant);
            Check(turned > 0 && OffZero() == 0 && driver.ActiveCells == 0 && driver.Stage == 0,
                "driver reset on Dormant: " + turned + " turned cells to 0 at once, stage 0");
            var away = player;
            for (var r = 14; r <= 22 && away == player; r++)
            for (var k = 0; k < 4 && away == player; k++)
            {
                var a = new GridCoord(player.x + r * DirX[k], player.y + r * DirY[k]);
                if (world.IsBuilt(a)) away = a;
            }
            hunter.DebugPlace(world.CellCenter(away));
        }

        // Reduce Motion: turn only; the wave a one-time turn.
        {
            FrontRoomsWayCueDriver.ReduceMotionForTools = true;
            driver.HunterStateForTools(HunterState.Chase);
            var maxState = 0;
            var end = driver.Clock + 2.5f;
            while (driver.Clock < end - 1e-5f)
            {
                Until(driver.Clock + .05f);
                maxState = Mathf.Max(maxState, MaxState());
            }
            var turned = OffZero();
            driver.HunterStateForTools(HunterState.Search);
            Until(driver.Clock + 4.5f);
            FrontRoomsWayCueDriver.ReduceMotionForTools = false;
            Check(maxState == 3 && turned > 0 && OffZero() == 0, "driver Reduce Motion: the wave turns " + turned + " cells and never steps (max state " + maxState + "); all back at 0 after");
        }

        // Review m10b: BreakDoor inside a chase changes nothing and the Chase after it does not restart the wave; any
        // exit from the chase retracts (Listen and Hunt as well as Search), and at stage 0 every cell ends at 0.
        {
            var probe = routedRoom[0];
            driver.HunterStateForTools(HunterState.Chase);
            Until(driver.Clock + .6f);
            var id = driver.WaveIdForTools;
            var steps0 = driver.WaveStepsForTools(probe);
            var outerProbe = default(GridCoord);
            var outerArrive = -1;
            for (var s = 0; s < FrontRoomsWayfinding.Slots && outerArrive < 0; s++)
            {
                var c = way.CellAtSlot(s);
                var st = driver.WaveStepsForTools(c);
                if (st >= 12 && driver.StateOf(c) == 0) { outerProbe = c; outerArrive = st; }
            }
            driver.HunterStateForTools(HunterState.BreakDoor);
            Until(driver.Clock + .3f);
            driver.HunterStateForTools(HunterState.Chase);
            Until(driver.Clock + .05f);
            var idem = driver.WaveIdForTools == id && driver.WaveActive && driver.WaveStepsForTools(probe) == steps0 && driver.StateOf(probe) >= 3
                && (outerArrive < 0 || driver.WaveStepsForTools(outerProbe) == outerArrive);
            var exits = new[] { HunterState.Listen, HunterState.Hunt };
            var retracts = true;
            var detail = "";
            foreach (var e in exits)
            {
                if (!driver.WaveActive)
                {
                    driver.HunterStateForTools(HunterState.Chase);
                    Until(driver.Clock + .6f);
                }
                var te = driver.Clock;
                driver.HunterStateForTools(e);
                var at = driver.RetractAtForTools(probe);
                var ended = !driver.WaveActive;
                Until(te + 4.5f);
                var allZero = OffZero() == 0 && driver.ActiveCells == 0;
                if (!ended || Mathf.Abs(at - (te + FrontRoomsCueTiming.RetractWindow)) > .001f || !allZero) retracts = false;
                detail += e + ": the room set's retract at +" + F(at - te) + " s, all 0 after (" + allZero + "); ";
            }
            Check(idem && retracts, "driver chase exits (m10b): BreakDoor and the Chase after it keep wave " + id + " (" + idem + ", its arrivals unchanged); " + detail);
        }

        // 9.6: no door qualifies: the room set keeps its calm turn, no row steps, the wave still runs.
        {
            way.RelayDistance = c => float.PositiveInfinity;
            driver.WarnStageForTools(1);
            Until(driver.Clock + 1f);
            driver.HunterStateForTools(HunterState.Chase);
            var maxState = 0;
            var end = driver.Clock + 2f;
            while (driver.Clock < end - 1e-5f)
            {
                Until(driver.Clock + .05f);
                maxState = Mathf.Max(maxState, MaxState());
            }
            var calmTurn = true;
            foreach (var c in routedRoom) if (!way.Route(c, out var d) || driver.StateOf(c) != 3 || driver.TurnedDir(c) != d) calmTurn = false;
            var turned = OffZero();
            var qualifies = way.PressureReachesPlayer;
            driver.HunterStateForTools(HunterState.Search);
            Until(driver.Clock + 4.5f);
            driver.WarnStageForTools(0);
            Until(driver.Clock + .5f);
            way.RelayDistance = standIn;
            Check(!qualifies && maxState == 3 && calmTurn && turned > routedRoom.Count,
                "driver 9.6, no door qualifies: the wave turns " + turned + " cells, none steps (max state " + maxState + "), the room set keeps its calm turn (" + calmTurn + ")");
        }

        // Review m10e: the pressure route on the hunter's own field (PathDistanceFromRelay, its 45 m reach), the Relay
        // 5-8 cells from the player: along every chain of pressure dirs the Relay's path metres never fall more than a
        // cell below the chain's first cell, and no chain loops; 10 builds timed.
        {
            GridCoord? near = null;
            for (var r = 5; r <= 8 && !near.HasValue; r++)
            for (var k = 0; k < 4 && !near.HasValue; k++)
            {
                var a = new GridCoord(player.x + r * DirX[k], player.y + r * DirY[k]);
                if (world.IsBuilt(a)) near = a;
            }
            if (!near.HasValue) Check(false, "driver 9.6 on the hunter's field: no built cell 5-8 cells from the player" + Where());
            else
            {
                way.RelayDistance = real;
                hunter.DebugPlace(world.CellCenter(near.Value));
                var rc = world.CellOf(hunter.Position);
                var times = new List<float>();
                for (var i = 0; i < 10; i++)
                {
                    way.BuildPressure(rc, player);
                    times.Add(way.LastPressureMs);
                }
                int cells = 0, bad = 0, loops = 0;
                for (var s = 0; s < FrontRoomsWayfinding.Slots; s++)
                {
                    var c = way.CellAtSlot(s);
                    if (!way.PressureRoute(c, out _)) continue;
                    cells++;
                    var start = real(c);
                    var x = c;
                    var guard = 0;
                    while (way.PressureRoute(x, out var pd) && guard++ < 256)
                    {
                        var nx = new GridCoord(x.x + DirX[pd], x.y + DirY[pd]);
                        if (real(nx) < start - FrontRoomsWayfinding.PressureSlackMetres - Tie) { bad++; break; }
                        x = nx;
                    }
                    if (guard >= 256) loops++;
                }
                Stats(times, out var pmean, out _, out var pmax);
                var reaches = way.PressureReachesPlayer;
                report.costs.Add("pressure route on the hunter's own field (the Relay at " + rc + ", the player at " + player + "): " + cells + " cells, a door qualifies " + reaches
                    + ", 10 builds mean " + F(pmean) + " ms, max " + F(pmax) + " ms");
                Check(bad == 0 && loops == 0, "driver 9.6 on the hunter's own field (m10e): the Relay at " + rc + ", " + cells + " pressure cells (a door qualifies: " + reaches
                    + "); chains that bring it more than a cell nearer " + bad + ", loops " + loops + "; 10 builds max " + F(pmax) + " ms");
                hunter.DebugPlace(world.CellCenter(tvBack));
                way.RelayDistance = standIn;
                way.Invalidate();
                driver.TickForTools(Dt);
            }
        }

        // R5 in the driver: a target switch re-points the room set within one rebuild.
        {
            driver.WarnStageForTools(1);
            Until(driver.Clock + 1f);
            var before = new Dictionary<GridCoord, int>();
            foreach (var c in routedRoom) before[c] = driver.TurnedDir(c);
            var builds = way.Builds;
            driver.SetStationTarget(tvBack, -1);
            driver.TickForTools(Dt);
            var oneBuild = way.Builds == builds + 1;
            Until(driver.Clock + 1.5f);
            var repointed = true;
            var changed = 0;
            foreach (var c in routedRoom)
            {
                if (!way.Route(c, out var d)) continue;
                if (driver.TurnedDir(c) != d) repointed = false;
                if (before[c] != d) changed++;
            }
            // The key taken with no EXIT on this map: no target, the room set turns back. Review m1: through the last
            // fade to the print a cell keeps its turned dir in A (the field's A there is now 0, message 0).
            var probe = routedRoom[0];
            var probeDir = driver.TurnedDir(probe);
            var tk = driver.Clock;
            driver.StationKeyTaken();
            Until(tk + .34f);
            var fade = driver.TexelOf(probe);
            var fadeKeeps = fade.r == 1 && fade.g == 0 && fade.b > 0 && fade.b < 255 && probeDir >= 0 && fade.a == probeDir + 4;
            Until(tk + .6f);
            var rest = driver.TexelOf(probe);
            var restOk = rest.r == 0 && rest.g == 0 && rest.b == 0 && rest.a == 0;
            var noTarget = !way.HasTarget && OffZero() == 0;
            Check(oneBuild && repointed && changed > 0 && noTarget && fadeKeeps && restOk,
                "driver re-point: a target switch rebuilds once (" + oneBuild + ") and the room set re-points (" + changed + " cells changed dir, all on the new route: " + repointed
                + "); the key taken with no EXIT leaves no target and every cell at 0 (" + noTarget + "); the last fade keeps the turned dir (texel " + fade + ", m1), then (0, 0, 0, 0) (" + restOk + ")");
            driver.WarnStageForTools(0);
            Until(driver.Clock + .5f);
        }
        way.RelayDistance = real;
        report.costs.Add("driver: field " + way.Builds + " rebuilds, max " + F(way.MaxBuildMs) + " ms on the built test map; pressure " + way.PressureBuilds + " builds, max " + F(way.MaxPressureMs)
            + " ms; driver step max " + F(driver.MaxStepMs) + " ms");

        // R8 on the live map (review M2): the WorldMap path, which reads OpeningCostBetween, DoorBetween and the lock
        // rule on every crossing: 40 station targets spread over the built window, each built twice on the
        // long-lived field; every build counts toward the max.
        {
            way.ClearStation();
            var builds = new List<float>();
            var targets = 0;
            for (var s = 0; s < FrontRoomsWayfinding.Slots * 8 && targets < 40; s += 97)
            {
                var c = way.CellAtSlot(s % FrontRoomsWayfinding.Slots);
                if (!world.IsBuilt(c)) continue;
                targets++;
                way.SetStationTarget(c, -1);
                way.Rebuild();
                builds.Add(way.LastBuildMs);
                way.Rebuild();
                builds.Add(way.LastBuildMs);
            }
            Stats(builds, out var lmean, out var lp95, out var lmax);
            way.ClearStation();
            report.costs.Add("calm rebuild on the live map (WorldMap, " + targets + " station targets x 2 builds, every build): mean " + F(lmean) + " ms, p95 " + F(lp95) + " ms, max " + F(lmax) + " ms");
            Check(targets >= 20 && lmax <= 4f, "R8 cost on the live map (WorldMap: OpeningCostBetween, DoorBetween, the lock rule): every rebuild <= 4 ms: max " + F(lmax) + " ms over "
                + builds.Count + " builds (" + targets + " station targets; mean " + F(lmean) + ", p95 " + F(lp95) + ")");
        }

        // The live hooks (review m10d, m6): Boot's one MapRunStarted subscription binds the map's own driver; the
        // game's FloorChanged resets every cell; a station M4 sets from a FloorChanged handler that runs before the
        // driver's (subscribed first) stays, as it was set under the new floor's EXIT, with its key untaken; one left
        // from the floor before goes.
        {
            const BindingFlags Hidden = BindingFlags.NonPublic | BindingFlags.Static;
            var gameType = typeof(FrontRooms3DGame);
            var runEvent = gameType.GetEvent("MapRunStarted");
            var floorEvent = gameType.GetEvent("FloorChanged");
            var runField = gameType.GetField("MapRunStarted", Hidden);
            var floorField = gameType.GetField("FloorChanged", Hidden);
            var boot = typeof(FrontRoomsWayCueDriver).GetMethod("Boot", Hidden);
            var exitCells = new List<GridCoord>();
            for (var r = 6; r <= 20 && exitCells.Count < 3; r++)
            for (var k = 0; k < 4 && exitCells.Count < 3; k++)
            {
                var c = new GridCoord(player.x + r * DirX[k], player.y + r * DirY[k]);
                var o = MapGrid.ChunkOrigin(MapGrid.ChunkOf(c));
                int li = c.x - o.x, lj = c.y - o.y;
                if (!world.IsBuilt(c) || c == tv || c == tvBack || room.Contains(c) || li < 1 || li > MapGrid.ChunkCells - 2 || lj < 1 || lj > MapGrid.ChunkCells - 2) continue;
                exitCells.Add(c);
            }
            if (runEvent == null || floorEvent == null || runField == null || floorField == null || boot == null || exitCells.Count < 3)
            {
                Check(false, "driver live hooks: reflection found no MapRunStarted / FloorChanged backing field or no Boot, or fewer than 3 EXIT cells (" + exitCells.Count + ")");
                return;
            }
            driver.Unbind();
            var station2 = tvBack;
            Action<FrontRoomsMapWorld, FrontRoomsMapHunter, int> m4 = (mw, mh, fl) =>
            {
                var cur = FrontRoomsWayCueDriver.Current;
                if (cur != null && mw == cur.World) cur.SetStationTarget(station2, -1);
            };
            floorEvent.AddEventHandler(null, m4);
            Delegate hook = null;
            try
            {
                boot.Invoke(null, null);
                boot.Invoke(null, null);
                var hooks = 0;
                var list = runField.GetValue(null) as Delegate;
                if (list != null)
                    foreach (var h in list.GetInvocationList())
                        if (h.Method.DeclaringType == typeof(FrontRoomsWayCueDriver) && h.Method.Name == "OnMapRunStarted") { hooks++; hook = h; }
                world.SetExitArea(exitCells[0]);
                if (hook != null) ((Action<FrontRoomsMapWorld, FrontRoomsMapHunter>)hook)(world, hunter);
                var boundOk = FrontRoomsWayCueDriver.Current == driver && driver.Way != null && driver.World == world && world.GetComponents<FrontRoomsWayCueDriver>().Length == 1;
                way = driver.Way;
                driver.SetStationTarget(tv, -1);
                driver.TickForTools(Dt);
                driver.WarnStageForTools(1);
                Until(driver.Clock + 1f);
                var turnedBefore = OffZero();
                driver.StationKeyTaken();
                driver.TickForTools(Dt);
                // Floor 2: M4's handler (subscribed first) sets the new floor's station, then the driver's resets.
                world.SetExitArea(exitCells[1]);
                ((Action<FrontRoomsMapWorld, FrontRoomsMapHunter, int>)floorField.GetValue(null))(world, hunter, 2);
                var reset = OffZero() == 0 && driver.ActiveCells == 0;
                driver.TickForTools(Dt);
                var kept = way.Station == station2 && !way.StationKeyTaken && way.TargetIsStation && way.Target == station2;
                // Floor 3: no M4 now: the floor-2 station goes.
                floorEvent.RemoveEventHandler(null, m4);
                world.SetExitArea(exitCells[2]);
                ((Action<FrontRoomsMapWorld, FrontRoomsMapHunter, int>)floorField.GetValue(null))(world, hunter, 3);
                driver.TickForTools(Dt);
                var cleared = !way.Station.HasValue && !way.TargetIsStation && way.HasTarget;
                Check(hooks == 1 && boundOk && turnedBefore > 0 && reset && kept && cleared,
                    "driver live hooks (m10d, m6): Boot twice leaves one MapRunStarted subscription (" + hooks + ") that binds the map's own driver (" + boundOk + "); FloorChanged resets "
                    + turnedBefore + " turned cells to 0 (" + reset + "); a station M4 sets from a FloorChanged handler ahead of the driver's stays, key untaken (" + kept
                    + "); one left from the floor before goes, the field on N (" + cleared + ")");
            }
            finally
            {
                floorEvent.RemoveEventHandler(null, m4);
                if (hook != null) runEvent.RemoveEventHandler(null, hook);
                world.ClearExitArea();
            }
        }
    }
}
