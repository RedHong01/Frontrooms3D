using System;
using System.Collections.Generic;

namespace FrontRooms.Map
{
    /// <summary>Why a floor's station went nowhere, or why one candidate was turned down, in the order the planner asks (FrontRoomsFloorStation.Plan). Tests read the names.</summary>
    public enum FloorStationMiss : byte
    {
        /// <summary>A station stands on the floor.</summary>
        None,
        /// <summary>No template: both profile fields empty, or no map cache.</summary>
        NoModule,
        /// <summary>The floor has no EXIT, or none on foot.</summary>
        NoExit,
        /// <summary>No cell 10-20 cells of walking out could be E1's outside cell.</summary>
        NoWalk,
        /// <summary>The footprint and E1's outside cell are not one zone of the module's kind (Level 0 Standard for Waiting, Office for Desk, never Tall).</summary>
        Zone,
        /// <summary>On the floor's start area, its door or the ring round them.</summary>
        StartArea,
        /// <summary>Closer than 6 cells to the EXIT.</summary>
        NearExit,
        /// <summary>In X's own chunk, which the carve must never touch (C22: its rules hold for the floor because nothing changes it).</summary>
        ExitChunk,
        /// <summary>A cell of the footprint lies on a shortest walk from the arrival door to N.</summary>
        OnRoute,
        /// <summary>E2 cannot open: its outside cell is Tall, or at another height on a module with no map door.</summary>
        SecondOpening,
        /// <summary>The footprint's wall would stand on a Tall zone's one arch (C33).</summary>
        TallArch,
        /// <summary>A candidate left over when the budget of carves (MaxCarves) ran out.</summary>
        Budget,
        /// <summary>The carve's repair could only join a pocket through glass, or not at all (A1's rule).</summary>
        Glass,
        /// <summary>E1's or E2's outside cell does not lead on without the station.</summary>
        LeadsOn,
        /// <summary>With the carve in place the station is not 10-20 cells of walking from the arrival door.</summary>
        WalkBand,
        /// <summary>The carve moved the EXIT's walk, or put the station on it.</summary>
        RouteMoved,
        /// <summary>Set on the cache, the carve failed a read-back (X's rules, the stairwell's placard wall): a bug, never a played floor.</summary>
        Carved,
        Count,
    }

    /// <summary>
    /// A floor station module as the planner reads it (BS1): Station · Waiting or Station · Desk, its footprint, E1 (its side,
    /// its cell along that side, the StationArch marker's point, its authored width) and E2. Read through module.Rotated, so
    /// the tools' turned fit runs reach the same code; the game plays turn 0 only (no rotation in the game: BS1).
    /// </summary>
    public sealed class FloorStationTemplate
    {
        /// <summary>The module data the plan stands on, turned by <see cref="turns"/> quarter turns.</summary>
        public RoomModuleData module;
        public int turns;
        public int width, depth;
        /// <summary>Which zones it goes in: Level 0 (Waiting) or Office (Desk).</summary>
        public ZoneTheme theme;
        public ModuleSide e1Side, e2Side;
        public int e1Index, e2Index;
        /// <summary>E1's centre in metres along its edge (the marker's point); the authored widths, 0 = the map's own.</summary>
        public float e1Center, e1Width, e2Width;

        /// <summary>The name in logs and read-outs.</summary>
        public string Name => theme == ZoneTheme.Office ? "Station · Desk" : "Station · Waiting";

        public ModuleLamp LampAt(int i, int j) => module.lamps[i + j * width];
        public EdgeKind InnerEast(int i, int j) => RoomModuleData.ToEdge(module.innerEast[i + j * (width - 1)]);
        public EdgeKind InnerNorth(int i, int j) => RoomModuleData.ToEdge(module.innerNorth[i + j * width]);

        /// <summary>The module cell (i, j) along a side at an index.</summary>
        public GridCoord InCell(ModuleSide side, int index) =>
            side == ModuleSide.South ? new GridCoord(index, 0) : side == ModuleSide.North ? new GridCoord(index, depth - 1)
                : side == ModuleSide.West ? new GridCoord(0, index) : new GridCoord(width - 1, index);

        /// <summary>The step out through a side.</summary>
        public static GridCoord OutStep(ModuleSide side) =>
            side == ModuleSide.South ? new GridCoord(0, -1) : side == ModuleSide.North ? new GridCoord(0, 1)
                : side == ModuleSide.West ? new GridCoord(-1, 0) : new GridCoord(1, 0);

        /// <summary>
        /// The template of a floor station module: the module checked by RoomModuleData.Validate, the floor station contract
        /// included (<paramref name="footprint"/> as Validate takes it, null when the kit is not known), then read. Null, with
        /// the first reason, for a module the planner refuses.
        /// </summary>
        public static FloorStationTemplate From(RoomModuleData module, Func<string, float[]> footprint, out string why)
        {
            why = null;
            if (module == null) { why = "no floor station module"; return null; }
            var m = module.Clone();
            m.Normalize();
            if (!m.IsFloorStation) { why = "the module has no StationArch marker, so it is not a floor station"; return null; }
            var errors = new List<string>();
            m.Validate(errors, new List<string>(), footprint);
            if (errors.Count > 0) { why = errors[0] + (errors.Count > 1 ? " (and " + (errors.Count - 1) + " more)" : ""); return null; }
            return Read(m, 0);
        }

        /// <summary>Tools only (the fit runs, BS1): this template turned clockwise. Turn 0 is itself.</summary>
        public FloorStationTemplate Turned(int quarterTurns) => ((quarterTurns % 4) + 4) % 4 == 0 ? this : Read(module.Rotated(quarterTurns), quarterTurns);

        // E1 is the arch nearest the StationArch marker, E2 the other (the contract says there are two).
        static FloorStationTemplate Read(RoomModuleData m, int turns)
        {
            var t = new FloorStationTemplate { module = m, turns = turns, width = m.width, depth = m.depth, theme = m.theme };
            var marker = default(ModuleMarker);
            foreach (var mk in m.markers)
                if (mk.kind == ModuleMarkerKind.StationArch) marker = mk;
            var cs = MapGrid.CellSize;
            var best = float.MaxValue;
            var found = false;
            for (var s = ModuleSide.South; s <= ModuleSide.East; s++)
            {
                var alongX = s == ModuleSide.South || s == ModuleSide.North;
                for (var k = 0; k < (alongX ? m.width : m.depth); k++)
                {
                    if (m.PerimeterAt(s, k) != ModuleEdge.Arch) continue;
                    var mx = alongX ? (k + .5f) * cs : s == ModuleSide.West ? 0f : m.WidthMetres;
                    var mz = !alongX ? (k + .5f) * cs : s == ModuleSide.South ? 0f : m.DepthMetres;
                    var d = (mx - marker.x) * (mx - marker.x) + (mz - marker.z) * (mz - marker.z);
                    if (found && d >= best)
                    {
                        t.e2Side = s;
                        t.e2Index = k;
                        continue;
                    }
                    if (found)
                    {
                        t.e2Side = t.e1Side;
                        t.e2Index = t.e1Index;
                    }
                    found = true;
                    best = d;
                    t.e1Side = s;
                    t.e1Index = k;
                }
            }
            t.e1Center = (t.e1Side == ModuleSide.South || t.e1Side == ModuleSide.North ? marker.x : marker.z) - t.e1Index * cs;
            t.e1Width = m.ArchWidthAt(t.e1Side, t.e1Index);
            t.e2Width = m.ArchWidthAt(t.e2Side, t.e2Index);
            return t;
        }
    }

    /// <summary>What FrontRoomsFloorStation.Plan found on one floor, fit or miss: the fit table's raw numbers (BS1) and the game's STATION line.</summary>
    public sealed class FloorStationReport
    {
        /// <summary>A station stands (the plan is Ready).</summary>
        public bool fit;
        /// <summary>No station: the deepest reason any candidate got to (None on a fit).</summary>
        public FloorStationMiss miss;
        /// <summary>Candidates turned down, by reason; candidates = cells in the band x templates.</summary>
        public readonly int[] rejects = new int[(int)FloorStationMiss.Count];
        public int candidates;
        /// <summary>Candidates that passed every test on zones and walks alone, and the carves tried on them.</summary>
        public int passed, tries;
        /// <summary>On a fit: the module (the turned data under the tools' turned runs), its kind and turn.</summary>
        public RoomModuleData module;
        public ZoneTheme theme;
        public int turns;
        /// <summary>On a fit: walking cells from the arrival door into the station, and from it into X (ExitPlan.distance, unmoved).</summary>
        public int walkCells, exitCells;
    }

    /// <summary>
    /// One floor below the first in a run, for the logs and 关卡设计调研's read-outs (BS3: FrontRooms3DGame.FloorStationRows;
    /// outcomes on B1-B2 are theirs to measure once the modules exist). Times are the run's elapsed seconds, -1 for not yet.
    /// A floor with no module asked (both profile fields empty) has no row.
    /// </summary>
    [Serializable]
    public sealed class FloorStationRow
    {
        public int floor, seed;
        public bool fit;
        public FloorStationMiss miss;
        public ZoneTheme theme;
        public GridCoord e1In;
        public int walkCells, exitCells, tries;
        public float planMs;
        /// <summary>The player first stood in the station; the floor key was taken; the EXIT's key turn committed; the player sealed into X.</summary>
        public float reachedAt = -1f, takeAt = -1f, unlockAt = -1f, exitAt = -1f;
        /// <summary>E on the locked EXIT before the take.</summary>
        public int exitTries;
    }

    /// <summary>
    /// BS1: a floor's station, floors 2 and deeper (M4's rule; research/tutorial/20_requests.md BS1, 10_proposal.md 4.3; the
    /// modules are BS4's). One station, a found room: 10-20 cells of walking from the arrival door into it, off every shortest
    /// walk to the EXIT, at least 6 cells from X in both axes taken together (Chebyshev), in one zone of its kind (never
    /// Tall), reached on foot with no glass and no key. Plain C# on a FrontRoomsMapCache, after the EXIT is planned and before
    /// Begin: a function of the cache's seed and tier, the door, the EXIT and the modules, whatever the cache generated before.
    /// How it goes (the A26 carve's steps; plan notes/floor1_start_group_plan.md 4.2, A1's glass rule):
    /// - every cell the BFS from the door reaches 9-19 steps out, tried as E1's outside cell, for each template; zones and
    ///   walks only, nothing changed (Prefilter);
    /// - those left sorted by a hash of the seed and the cell, carved on a virtual overlay (FrontRoomsStartGroup.BuildFloorCarve:
    ///   walls, E1 and E2, room drops, pins, repair), checked on it (CheckOverlay), then set on the cache and read back
    ///   (ReadBack). At most MaxCarves; a failure tries the next;
    /// - no fit: the cache is left raw and the floor plays as A35 (no station, EXIT unlocked): never a locked EXIT with no
    ///   reachable key.
    /// </summary>
    public static class FrontRoomsFloorStation
    {
        /// <summary>Walking cells from the arrival door into the station (M4).</summary>
        public const int WalkMin = 10, WalkMax = 20;
        /// <summary>No station cell closer to X than this in either axis (M4, R10).</summary>
        public const int ExitGapCells = 6;
        /// <summary>Carves tried on a floor, as floor 1's root budget (FrontRoomsStartGroup.MaxLeadsOnRetries + 1).</summary>
        public const int MaxCarves = FrontRoomsStartGroup.MaxLeadsOnRetries + 1;

        // The pick's salt, apart from the generator's (MapHash: 11-73), the planner's (79, 83) and the map's (89, 97, 211, 223, 307).
        const int SaltPick = 233;

        static readonly GridCoord[] Steps4 = { new GridCoord(1, 0), new GridCoord(-1, 0), new GridCoord(0, 1), new GridCoord(0, -1) };
        static readonly StartGroupCarve NoOverlay = new StartGroupCarve();

        struct Candidate
        {
            public FloorStationTemplate template;
            public int order;
            public StartGroupLayout layout;
            public EdgeKind e2;
            public uint hash;
        }

        /// <summary>
        /// The floor's station on <paramref name="cache"/>, right after its EXIT is chosen (ChooseExit, on this cache) and before
        /// the map builds. <paramref name="waiting"/> and <paramref name="desk"/> are FloorStationTemplate.From's, null for none;
        /// <paramref name="startArea"/> and <paramref name="door"/> the floor's start area and the cell its door opens onto.
        /// Ready: the carve is on the cache, plan.walk the walk from the door, plan.floor what was found; hand it to
        /// FrontRoomsMapWorld.SetStation with plan.floor.module. Not Ready: the cache is raw, plan.error and plan.floor say why.
        /// <paramref name="turnsForTools"/> is for the fit runs alone: 4 also tries the three turned footprints. The game never passes it.
        /// </summary>
        public static StartGroupPlan Plan(FrontRoomsMapCache cache, FloorStationTemplate waiting, FloorStationTemplate desk, CellRect startArea, GridCoord door, ExitPlan exit, int turnsForTools = 1)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var plan = PlanUntimed(cache, waiting, desk, startArea, door, exit, turnsForTools);
            plan.planMs = (float)watch.Elapsed.TotalMilliseconds;
            return plan;
        }

        static StartGroupPlan PlanUntimed(FrontRoomsMapCache cache, FloorStationTemplate waiting, FloorStationTemplate desk, CellRect startArea, GridCoord door, ExitPlan exit, int turns)
        {
            var plan = new StartGroupPlan { floor = new FloorStationReport() };
            var report = plan.floor;
            if (cache == null) return Miss(plan, FloorStationMiss.NoModule, "no map cache");
            cache.ClearCarve();
            if (waiting == null && desk == null) return Miss(plan, FloorStationMiss.NoModule, "no floor station module");
            if (!exit.found || exit.throughGlass) return Miss(plan, FloorStationMiss.NoExit, exit.found ? "the EXIT is only reached through glass" : "the floor has no EXIT");
            var x = exit.exitCell;
            var n = exit.frontCell;
            // The cells the EXIT leaves out: X, or X1-X2 with the walkable stair (ExitPlan.stair); every walk keeps out of them.
            var xr = exit.ExitArea;
            var reach = Math.Max(WalkMax, exit.distance);
            var fromDoor = Walk(cache, NoOverlay, startArea, xr, door, reach);
            if (!fromDoor.TryGetValue(n, out var route)) return Miss(plan, FloorStationMiss.NoExit, "N is not on foot from the arrival door");
            var fromN = Walk(cache, NoOverlay, startArea, xr, n, route);

            var passed = new List<Candidate>();
            var seed = cache.Generator.Seed;
            var order = 0;
            for (var pick = 0; pick < 2; pick++)
            {
                var template = pick == 0 ? waiting : desk;
                if (template == null) continue;
                for (var turn = 0; turn < Math.Max(1, turns); turn++, order++)
                {
                    var t = template.Turned(turn);
                    foreach (var pair in fromDoor)
                    {
                        if (pair.Value + 1 < WalkMin || pair.Value + 1 > WalkMax) continue;
                        report.candidates++;
                        var l = Layout(t, pair.Key, startArea, n, x);
                        var reject = Prefilter(cache, t, l, startArea, door, x, xr, fromDoor, fromN, route, out var e2);
                        if (reject != FloorStationMiss.None) { report.rejects[(int)reject]++; continue; }
                        passed.Add(new Candidate { template = t, order = order, layout = l, e2 = e2, hash = MapHash.Hash(seed, pair.Key.x, pair.Key.y, SaltPick + order) });
                    }
                }
            }
            report.passed = passed.Count;
            passed.Sort((a, b) => a.hash != b.hash ? a.hash.CompareTo(b.hash) : a.order != b.order ? a.order.CompareTo(b.order)
                : a.layout.e1Out.x != b.layout.e1Out.x ? a.layout.e1Out.x.CompareTo(b.layout.e1Out.x) : a.layout.e1Out.y.CompareTo(b.layout.e1Out.y));

            // G1's answer for the arrival door, kept by the carve: C22's (west cell's south wall) or, after a walkable stair (a
            // 1 x 2 start area with its door south of it), the far door's mirror (FarDoorPlacardWest).
            var stairStart = FrontRoomsExitPlanner.IsStairStart(startArea, door);
            var placard = StartPlacard(cache, door, stairStart);
            foreach (var c in passed)
            {
                if (report.tries >= MaxCarves) { report.rejects[(int)FloorStationMiss.Budget] = passed.Count - report.tries; break; }
                report.tries++;
                var l = c.layout;
                var overlay = FrontRoomsStartGroup.BuildFloorCarve(cache, c.template, l, c.e2);
                var miss = CheckOverlay(cache, overlay, l, startArea, door, xr, n, route, reach, out var walk);
                if (miss != FloorStationMiss.None) { report.rejects[(int)miss]++; continue; }
                var carve = FrontRoomsMapCarve.FromFloorStation(overlay, c.template, l, seed);
                cache.SetCarve(carve);
                if (!ReadBack(cache, l, c.e2, startArea, door, x, n, placard, exit.stair, stairStart))
                {
                    cache.ClearCarve();
                    report.rejects[(int)FloorStationMiss.Carved]++;
                    continue;
                }
                plan.found = true;
                plan.layout = l;
                plan.carve = overlay;
                plan.mapCarve = carve;
                plan.exit = exit;
                plan.walk = walk;
                report.fit = true;
                report.module = c.template.module;
                report.theme = c.template.theme;
                report.turns = c.template.turns;
                report.walkCells = walk.Count - 1;
                report.exitCells = exit.distance;
                return plan;
            }
            var deepest = FloorStationMiss.NoWalk;
            for (var r = (int)FloorStationMiss.Count - 1; r > (int)FloorStationMiss.NoWalk; r--)
                if (report.rejects[r] > 0) { deepest = (FloorStationMiss)r; break; }
            return Miss(plan, deepest, "no room fits: " + report.candidates + " candidates, " + report.passed + " past zones and walks, " + report.tries + " carved");
        }

        static StartGroupPlan Miss(StartGroupPlan plan, FloorStationMiss miss, string why)
        {
            plan.floor.miss = miss;
            plan.error = why + " (" + miss + ")";
            return plan;
        }

        /// <summary>A template laid out with E1's outside cell at <paramref name="e1Out"/>, on a floor whose start area, N and X are given.</summary>
        public static StartGroupLayout Layout(FloorStationTemplate t, GridCoord e1Out, CellRect startArea, GridCoord front, GridCoord exit)
        {
            var step1 = FloorStationTemplate.OutStep(t.e1Side);
            var e1In = new GridCoord(e1Out.x - step1.x, e1Out.y - step1.y);
            var cell1 = t.InCell(t.e1Side, t.e1Index);
            var cell2 = t.InCell(t.e2Side, t.e2Index);
            var x0 = e1In.x - cell1.x;
            var y0 = e1In.y - cell1.y;
            var e2In = new GridCoord(x0 + cell2.x, y0 + cell2.y);
            return new StartGroupLayout
            {
                floorStation = true,
                door = e1In,
                station = new CellRect(x0, y0, t.width, t.depth),
                e1In = e1In,
                e1Out = e1Out,
                e2In = e2In,
                e2Out = e2In + FloorStationTemplate.OutStep(t.e2Side),
                corridor = 0,
                front = front,
                exit = exit,
                strip = startArea,
            };
        }

        // Zones and walks alone, nothing carved. Reads the generator's zones (pure, no ZoneTier record) and the walks already made.
        static FloorStationMiss Prefilter(FrontRoomsMapCache cache, FloorStationTemplate t, StartGroupLayout l, CellRect startArea, GridCoord door, GridCoord x, CellRect xr,
            Dictionary<GridCoord, int> fromDoor, Dictionary<GridCoord, int> fromN, int route, out EdgeKind e2)
        {
            e2 = EdgeKind.Arch;
            var gen = cache.Generator;
            var zone = gen.ZoneOf(l.e1In);
            // Waiting: Level 0, Standard only. Desk: Office, never Tall (an Office zone is Standard: the generator gives Low zones Level 0).
            if (zone.theme != t.theme || (t.theme == ZoneTheme.Level0 ? zone.height != ZoneHeight.Standard : zone.height == ZoneHeight.Tall)) return FloorStationMiss.Zone;
            var s = l.station;
            for (var j = s.y; j < s.y + s.h; j++)
            for (var i = s.x; i < s.x + s.w; i++)
                if (gen.ZoneOf(new GridCoord(i, j)).id != zone.id) return FloorStationMiss.Zone;
            if (gen.ZoneOf(l.e1Out).id != zone.id) return FloorStationMiss.Zone;

            var doorCell = new CellRect(door.x, door.y, 1, 1);
            for (var j = s.y; j < s.y + s.h; j++)
            for (var i = s.x; i < s.x + s.w; i++)
            {
                var cell = new GridCoord(i, j);
                if (FrontRoomsExitPlanner.InAreaOrRing(startArea, cell) || FrontRoomsExitPlanner.InAreaOrRing(doorCell, cell)) return FloorStationMiss.StartArea;
            }
            if (FrontRoomsExitPlanner.InArea(startArea, l.e2Out) || l.e2Out == door || l.e1Out == door) return FloorStationMiss.StartArea;

            // Chebyshev cells to the exit area's nearest cell (X; with the walkable stair X1 or X2).
            for (var j = s.y; j < s.y + s.h; j++)
            for (var i = s.x; i < s.x + s.w; i++)
                if (Math.Max(Math.Max(Math.Max(xr.x - i, i - (xr.x + xr.w - 1)), 0), Math.Max(Math.Max(xr.y - j, j - (xr.y + xr.h - 1)), 0)) < ExitGapCells) return FloorStationMiss.NearExit;

            var exitChunk = MapGrid.ChunkOf(x);
            if (MapGrid.ChunkOf(l.e1Out) == exitChunk || MapGrid.ChunkOf(l.e2Out) == exitChunk) return FloorStationMiss.ExitChunk;
            for (var j = s.y; j < s.y + s.h; j++)
            for (var i = s.x; i < s.x + s.w; i++)
                if (MapGrid.ChunkOf(new GridCoord(i, j)) == exitChunk) return FloorStationMiss.ExitChunk;

            for (var j = s.y; j < s.y + s.h; j++)
            for (var i = s.x; i < s.x + s.w; i++)
            {
                var cell = new GridCoord(i, j);
                if (fromDoor.TryGetValue(cell, out var a) && fromN.TryGetValue(cell, out var b) && a + b <= route) return FloorStationMiss.OnRoute;
            }

            // E2: an arch at one height; over a Low-Standard border the map's door, which only the Desk's contract takes.
            var kind = FrontRoomsStartGroup.OpeningKind(gen, l.e2In, l.e2Out, out _);
            if (kind == EdgeKind.Window || (kind == EdgeKind.Door && t.theme != ZoneTheme.Office)) return FloorStationMiss.SecondOpening;
            e2 = kind;

            // C33: a station never stands on a Tall zone's one arch: its wall would take the hall off the walking route.
            for (var i = s.x; i < s.x + s.w; i++)
                if (gen.IsTallExit(new GridCoord(i, s.y), new GridCoord(i, s.y - 1)) || gen.IsTallExit(new GridCoord(i, s.y + s.h - 1), new GridCoord(i, s.y + s.h))) return FloorStationMiss.TallArch;
            for (var j = s.y; j < s.y + s.h; j++)
                if (gen.IsTallExit(new GridCoord(s.x, j), new GridCoord(s.x - 1, j)) || gen.IsTallExit(new GridCoord(s.x + s.w - 1, j), new GridCoord(s.x + s.w, j))) return FloorStationMiss.TallArch;
            return FloorStationMiss.None;
        }

        // The rules on the virtual overlay: no glass join, both openings lead on, 10-20 cells from the door, the EXIT's walk as it was.
        static FloorStationMiss CheckOverlay(FrontRoomsMapCache cache, StartGroupCarve overlay, StartGroupLayout l, CellRect area, GridCoord door, CellRect x, GridCoord n, int route, int reach, out List<GridCoord> walk)
        {
            walk = null;
            if (overlay.windowJoins > 0 || overlay.repairFailures > 0) return FloorStationMiss.Glass;
            if (!FrontRoomsStartGroup.LeadsOn(cache, overlay, l, l.e1Out) || !FrontRoomsStartGroup.LeadsOn(cache, overlay, l, l.e2Out)) return FloorStationMiss.LeadsOn;
            var fromDoor = Walk(cache, overlay, area, x, door, reach);
            var s = l.station;
            var best = -1;
            var entry = default(GridCoord);
            for (var j = s.y; j < s.y + s.h; j++)
            for (var i = s.x; i < s.x + s.w; i++)
            {
                var cell = new GridCoord(i, j);
                if (!fromDoor.TryGetValue(cell, out var d) || (best >= 0 && d >= best)) continue;
                best = d;
                entry = cell;
            }
            if (best < WalkMin || best > WalkMax) return FloorStationMiss.WalkBand;
            if (!fromDoor.TryGetValue(n, out var carved) || carved != route) return FloorStationMiss.RouteMoved;
            var fromN = Walk(cache, overlay, area, x, n, route);
            for (var j = s.y; j < s.y + s.h; j++)
            for (var i = s.x; i < s.x + s.w; i++)
            {
                var cell = new GridCoord(i, j);
                if (fromDoor.TryGetValue(cell, out var a) && fromN.TryGetValue(cell, out var b) && a + b <= route) return FloorStationMiss.RouteMoved;
            }
            walk = RouteTo(cache, overlay, fromDoor, entry);
            return FloorStationMiss.None;
        }

        // Set on the cache: what the overlay promised reads back from real chunks, and X and the stairwell's door kept theirs.
        // The walkable stair (stairExit): the pair's rules on X1-X2; after a walkable stair (stairStart): the far door's G1.
        static bool ReadBack(FrontRoomsMapCache cache, StartGroupLayout l, EdgeKind e2, CellRect area, GridCoord door, GridCoord x, GridCoord n, bool placard, bool stairExit, bool stairStart) =>
            cache.Edge(l.e1In, l.e1Out) == EdgeKind.Arch && cache.Edge(l.e2In, l.e2Out) == e2
            && FrontRoomsStartGroup.LeadsOn(cache, NoOverlay, l, l.e1Out) && FrontRoomsStartGroup.LeadsOn(cache, NoOverlay, l, l.e2Out)
            && (stairExit
                ? FrontRoomsExitPlanner.StairCellsFit(cache, area, x) && FrontRoomsExitPlanner.StairKeepsChunkWhole(cache, area, x, n)
                : FrontRoomsExitPlanner.ExitCellFits(cache, area, x) && FrontRoomsExitPlanner.KeepsChunkWhole(cache, area, x, n))
            && FrontRoomsExitPlanner.FrontStandard(cache, n) && FrontRoomsExitPlanner.Sightline(cache, x) == FrontRoomsExitPlanner.SightlineView.Clear
            && StartPlacard(cache, door, stairStart) == placard;

        // G1 at the floor's arrival door: StairDoorPlacardWest after C22's vestibule, FarDoorPlacardWest after a walkable stair.
        static bool StartPlacard(FrontRoomsMapCache cache, GridCoord door, bool stairStart) =>
            stairStart ? FrontRoomsExitPlanner.FarDoorPlacardWest(cache, door) : FrontRoomsExitPlanner.StairDoorPlacardWest(cache, door);

        // Walking steps from a cell over the overlay (no wall, no glass), never into the start area or X (X1-X2 with the
        // walkable stair: the rect x), out to cap.
        static Dictionary<GridCoord, int> Walk(FrontRoomsMapCache cache, StartGroupCarve overlay, CellRect area, CellRect x, GridCoord from, int cap)
        {
            var dist = new Dictionary<GridCoord, int> { { from, 0 } };
            var queue = new Queue<GridCoord>();
            queue.Enqueue(from);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                var d = dist[cell];
                if (d >= cap) continue;
                foreach (var step in Steps4)
                {
                    var next = cell + step;
                    if (dist.ContainsKey(next) || FrontRoomsExitPlanner.InArea(x, next) || FrontRoomsExitPlanner.InArea(area, next)) continue;
                    if (!FrontRoomsExitPlanner.Walkable(overlay.Edge(cache, cell, next))) continue;
                    dist[next] = d + 1;
                    queue.Enqueue(next);
                }
            }
            return dist;
        }

        // One shortest walk from the BFS's start to a cell it reached, start first: back from the cell, the first neighbour one step nearer.
        static List<GridCoord> RouteTo(FrontRoomsMapCache cache, StartGroupCarve overlay, Dictionary<GridCoord, int> dist, GridCoord to)
        {
            var walk = new List<GridCoord> { to };
            var cell = to;
            var d = dist[to];
            while (d > 0)
            {
                var stepped = false;
                foreach (var step in Steps4)
                {
                    var previous = cell + step;
                    if (!dist.TryGetValue(previous, out var p) || p != d - 1 || !FrontRoomsExitPlanner.Walkable(overlay.Edge(cache, cell, previous))) continue;
                    cell = previous;
                    d = p;
                    walk.Add(cell);
                    stepped = true;
                    break;
                }
                // The BFS reached every cell by such a step, so this never happens; a guard, never a hang in a floor change.
                if (!stepped) break;
            }
            walk.Reverse();
            return walk;
        }
    }
}
