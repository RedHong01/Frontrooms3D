using System;
using System.Collections.Generic;

namespace FrontRooms.Map
{
    /// <summary>
    /// One floor's EXIT, as the planner chose it (C22, plan v2 D4). X is the cell the stairwell stands in;
    /// N = X + (0, -1) is the map cell in front of the EXIT door, so the EXIT door is X's south edge (N to X)
    /// and X's north edge is the next floor's start door (NextDoorCell). All in map cells (the map's own
    /// space, root at the origin), which the next floor keeps: the root never moves under a sealed player (D2).
    /// </summary>
    [Serializable]
    public struct ExitPlan
    {
        /// <summary>False when no cell in any band passed the rules (the floor has no EXIT).</summary>
        public bool found;
        /// <summary>X: the stairwell's cell. Reserved by the map like a start area (D3), never shifted (D5).</summary>
        public GridCoord exitCell;
        /// <summary>N = X + (0, -1): where the player stands to open the EXIT door.</summary>
        public GridCoord frontCell;
        /// <summary>
        /// Walking steps from the start door cell into X: the shortest walk to N that never enters X (or the
        /// start area) and never needs glass (unless throughGlass), plus the step through the EXIT door.
        /// </summary>
        public int distance;
        /// <summary>The band the EXIT was found in, after any widening.</summary>
        public int bandMin, bandMax;
        /// <summary>True when the asked band had no candidate and a wider one was used.</summary>
        public bool widened;
        /// <summary>
        /// The last resort: no band had an EXIT the player could walk to without breaking glass, so the walk
        /// counted windows as passable. Only a start that leaves its pocket through glass alone comes to this
        /// (floor 1, whose root search lets glass lead on, as it always has); later floors start where SeedFor
        /// made sure the way on needs no glass.
        /// </summary>
        public bool throughGlass;
        /// <summary>Cells that passed every rule in that band, and how many of them were dead ends.</summary>
        public int candidates, deadEnds;
        /// <summary>
        /// The nearest-first rule of a widened band (C23 round 2): how many walking cells the chosen X lies outside
        /// the band asked (0 whenever the band asked had a candidate), how many candidates lie that near, and how
        /// many of those are dead ends (the pool the dead-end preference and the hash pick from).
        /// </summary>
        public int nearestGap, nearest, nearestDeadEnds;
        /// <summary>The chosen X's generated west, east and north edges were already walls (the preferred kind).</summary>
        public bool deadEnd;
        /// <summary>
        /// C23 step 1's corridor rule: N's west and east neighbours are both map cells the player walks to from N
        /// (neither edge a wall or glass, neither cell in the start area), so the corridor runs past the EXIT door
        /// in the side of its wall. The map then mounts the end-on blade (Kit_ExitSign_End) over the door instead of
        /// the flat sign (FrontRoomsMapWorld.ExitSignIsEnd reads the same rule from the edges as built).
        /// </summary>
        public bool corridor;
        /// <summary>
        /// The front rules, in the band the EXIT was found in (C23, 2026-10-22): cells that passed every other rule
        /// (beforeFrontRules), and of those, how many the planner turned down because N is not Standard height
        /// (rejectedHeight) or because a window lies, or may come to lie after a revisit shift, on the EXIT door's
        /// sightline (rejectedSightline; rejectedShiftOnly of them have no window now). One cell can fail both.
        /// candidates is what is left.
        /// </summary>
        public int beforeFrontRules, rejectedHeight, rejectedSightline, rejectedShiftOnly;
        /// <summary>
        /// The cells of the narrower bands tried before this one (0 unless widened) that passed every other rule and
        /// were lost to the front rules alone: above 0, the front rules are what made the floor widen its band.
        /// </summary>
        public int lostToFrontRules;

        /// <summary>The next floor's start door cell: X's north neighbour.</summary>
        public GridCoord NextDoorCell => new GridCoord(exitCell.x, exitCell.y + 1);

        /// <summary>The next floor's start area: X alone.</summary>
        public CellRect NextStartArea => new CellRect(exitCell.x, exitCell.y, 1, 1);
    }

    /// <summary>What SeedFor found for a floor after the first (D2).</summary>
    [Serializable]
    public struct FloorSeedPlan
    {
        /// <summary>The floor's map seed: FloorSeed(runSeed, floor, k) for the k it took.</summary>
        public int seed;
        /// <summary>k + 1: how many seeds were probed.</summary>
        public int tries;
        /// <summary>True when the seed passed every test (its start row is Standard Level 0 and leads on). False: a fallback.</summary>
        public bool standard;
        /// <summary>The start row leads into the maze (true for a standard seed and for the "only leads on" fallback).</summary>
        public bool leadsOn;
        /// <summary>G1 on floors 2+: the placard fits at P1 west beside the north door (StairDoorPlacardWest). False: a fallback.</summary>
        public bool placardWest;
    }

    /// <summary>Where a start door puts the map's root (RootFor, the game's MapRootFor in cells).</summary>
    [Serializable]
    public struct RootPlan
    {
        /// <summary>The map root in cells: a multiple of the world period (64 cells, 192 m) on both axes.</summary>
        public GridCoord root;
        /// <summary>The cell the door opens onto, in map cells (the door's world cell minus the root).</summary>
        public GridCoord doorCell;
        /// <summary>The door row is Standard-height Level 0 (false only for the fallback).</summary>
        public bool standard;
        /// <summary>The cell ahead is open and leads into the maze (false only when nothing in 24 rings did).</summary>
        public bool leadsOn;
        /// <summary>G1: the door cell's west edge is Open, so the placard hangs at P1 west (false only for a fallback).</summary>
        public bool placardWest;
    }

    /// <summary>
    /// The floor's EXIT and the next floor's seed, chosen from data alone (C22, Red 2026-10-22; plan v2
    /// c22_stairwell_plan_v2.md, decisions D2, D4 and D5). Plain C#: it reads a FrontRoomsMapCache and nothing
    /// else, so the map (SetExitArea before Begin), the game (the seal and the next floor) and the validator
    /// (100 seeds x 3 floors) all run the same code and agree on the same cell.
    ///
    /// Why from data and before Begin: the EXIT cell X is reserved by the map (no floor, ceiling, furniture or
    /// Relay there; the stairwell object fills it), so it has to be known before X's chunk is first built.
    /// Choosing it later would mean rebuilding that chunk in the player's sight, and RebuildChunk furnishes the
    /// whole dress queue in one frame. The walk the choice needs generates the chunks round the start at the
    /// floor's start tier; that is accepted (MAP_GENERATION.md: a chunk keeps the tier it was generated at).
    ///
    /// The rules for X (each one is there for a reason the review found in the map code):
    /// - It lies 14-18 walking cells from the start door on floor 1 and 14-22 on later floors (widened to 10-30,
    ///   then 6-40, when a band has no candidate). The walk is a BFS from the start door cell over every edge but
    ///   walls and windows (the EXIT never needs glass), never into the start area; the cells beside the start
    ///   area are walked like any other (the player and PassageBetween see them as map). The distance into X is
    ///   the walk to N with X removed (X is no map once reserved) plus the EXIT door. Only when no band has a
    ///   candidate does the walk count glass (ExitPlan.throughGlass): the title's root search lets glass lead on,
    ///   so now and then a floor-1 start is a pocket only windows leave (8 of the 1,900 floor-1 starts checked on
    ///   2026-10-22; seed 21029144: 14 Standard cells, four windows into a Tall zone).
    /// - Chunk-local 1..6 on both axes: its four edges, its four corners and N are then in X's own chunk, X is
    ///   never a chunk border's required gate, and the map only has to keep one chunk from shifting (D5).
    /// - Outside every room rect of its chunk: a reserved cell inside a carved room would un-furnish the room and
    ///   drop its Relay entries (the start-area rules), and module markers and key spots only lie inside rooms.
    /// - Not its zone's key site cell or key cell (a key lies in its own zone, so no other zone's key can be
    ///   there), and no column on its four corners (columns stand inside rooms, so this only guards the rule).
    /// - Not in the start area or the ring of cells round it: two reserved areas that touch would meet on an
    ///   edge both sides call reserved, which the start-area rule builds as open.
    /// - Removing X keeps its chunk whole: every cell of the chunk (outside the start area) that N reaches with X
    ///   there, N still reaches inside the chunk without X. That keeps X's other neighbours connected, and with
    ///   X's chunk never shifting it stays true for the whole floor: the chunk's border gates are its own cells,
    ///   still joined to N, and every neighbour chunk is whole by its own maze tree whatever its revision.
    /// - N, the cell in front of the EXIT door, is Standard height (2.9 m, never Low or Tall; FrontStandard): the
    ///   exit side of the door needs a Standard ceiling (C23 step 1, Red 2026-10-22; the visual chat,
    ///   research/stairwell/10_directions.md 1.2 "Ceiling" and 7f). The floor's LIT-locked EXIT sign hangs over the
    ///   door there on the steel frame's anchor (bottom 2.18 m, top 2.392 m since C23 round 2).
    /// - No window on the EXIT door's sightline (Sightline): none of the edges round N, N + (0, -1) and N + (0, -2)
    ///   (the corridor the player sees the door down) is glass, so a lit EXIT is never first seen through a window
    ///   (10_directions.md 7e). N's north edge is not asked: it is the EXIT door itself, whatever was generated
    ///   there. Windows only ever stand on an edge between a Tall zone and a lower one (the generator's and the
    ///   module stamp's rule), and a revisit shift regenerates every inner edge of the chunk it shifts; so an edge
    ///   of that kind inside a chunk other than X's (which never shifts), now a wall, could turn into a window
    ///   later, and turns the cell down too. Edges on a chunk border and zones never change.
    /// - Preferred: X's generated west, east and north edges are already walls. X is then a dead end off N (its
    ///   maze tree edge is the south one), so reserving it removes nothing anybody could walk through.
    /// Among the cells that pass, sorted by (x, y): in a widened band only those whose walk lies nearest the band
    /// asked (the fewest walking cells outside it; C23 round 2, so a widened floor 1 stays as near 14-18 as the map
    /// allows), then the dead ends among them if there are any; one is picked by a hash of the floor seed. In the
    /// band asked every candidate is 0 cells outside it, so the nearest rule changes nothing there. For floor 1 the
    /// start area depends on when Space was pressed, so the EXIT is a function of (seed, start), not of the seed alone.
    /// </summary>
    public static class FrontRoomsExitPlanner
    {
        /// <summary>Walking cells from the start door into X: floor 1, then every later floor.</summary>
        public const int Floor1BandMin = 14, Floor1BandMax = 18, LaterBandMin = 14, LaterBandMax = 22;
        /// <summary>The two widenings tried, in order, when a band has no candidate.</summary>
        public const int WideBandMin = 10, WideBandMax = 30, WidestBandMin = 6, WidestBandMax = 40;
        /// <summary>Seeds SeedFor probes before it falls back (the most any check needed on 2026-10-22: 73 of 3,800 later floors).</summary>
        public const int MaxSeedTries = 256;
        /// <summary>The walk from the cell past a start door must reach this many cells to count as leading into the maze (MapRootFor).</summary>
        public const int LeadsOnCells = 150;
        /// <summary>Rings of world periods RootFor searches round the unmoved root (MapRootFor).</summary>
        public const int RootRings = 24;

        // Hash salts of the planner, apart from the generator's (MapHash: 11-73).
        const int SaltFloorSeed = 79, SaltExitPick = 83;
        const int N = MapGrid.ChunkCells;

        static readonly GridCoord[] Steps4 = { new GridCoord(1, 0), new GridCoord(-1, 0), new GridCoord(0, 1), new GridCoord(0, -1) };

        /// <summary>The world period (ModuleUnits.WorldPeriod, 192 m) in cells: map roots move by whole periods so the printed ceiling grid stays on the troffers.</summary>
        public static int PeriodCells => (int)Math.Round(ModuleUnits.WorldPeriod / MapGrid.CellSize);

        /// <summary>N for an X: the cell in front of the EXIT door.</summary>
        public static GridCoord FrontOf(GridCoord exitCell) => new GridCoord(exitCell.x, exitCell.y - 1);

        /// <summary>The walking band for a floor (1 = the title's floor): 14-18 on floor 1, 14-22 after.</summary>
        public static void BandFor(int floor, out int bandMin, out int bandMax)
        {
            bandMin = floor <= 1 ? Floor1BandMin : LaterBandMin;
            bandMax = floor <= 1 ? Floor1BandMax : LaterBandMax;
        }

        /// <summary>A cell inside a rect of map cells (x, y, w, h).</summary>
        public static bool InArea(CellRect area, GridCoord cell) =>
            area.w > 0 && area.h > 0 && cell.x >= area.x && cell.x < area.x + area.w && cell.y >= area.y && cell.y < area.y + area.h;

        /// <summary>A cell inside the rect or in the ring of cells round it (Chebyshev distance 1, corners too).</summary>
        public static bool InAreaOrRing(CellRect area, GridCoord cell) =>
            area.w > 0 && area.h > 0 && cell.x >= area.x - 1 && cell.x <= area.x + area.w && cell.y >= area.y - 1 && cell.y <= area.y + area.h;

        /// <summary>An edge the player walks through: anything but a wall and unbroken glass.</summary>
        public static bool Walkable(EdgeKind kind) => kind != EdgeKind.Wall && kind != EdgeKind.Window;

        // ---------- The EXIT ----------

        /// <summary>
        /// Choose one floor's EXIT. <paramref name="startArea"/> is the floor's start area in map cells (the title's
        /// stream rooms on floor 1, the previous X on later floors) and <paramref name="startDoor"/> the cell its door
        /// opens onto; <paramref name="floorSeed"/> is the floor's map seed (the run seed on floor 1). The band is
        /// tried first, then 10-30, then 6-40 (never narrower than the band asked); a widened band picks among its
        /// candidates nearest the band asked (NearestGap). Generates the chunks the walk touches at the cache's
        /// current Tier: set the floor's tier on the cache first.
        /// </summary>
        public static ExitPlan ChooseExit(FrontRoomsMapCache cache, CellRect startArea, GridCoord startDoor, int floorSeed, int bandMin, int bandMax)
        {
            var plan = new ExitPlan();
            if (cache == null || InArea(startArea, startDoor)) return plan;
            bandMin = Math.Max(1, bandMin);
            bandMax = Math.Max(bandMin, bandMax);
            // Cells the front rules alone turned down in the bands that found nothing (reported on a widened plan).
            var lost = 0;
            // The three bands on foot, then (the last resort) the same three with glass counted as a way through.
            for (var attempt = 0; attempt < 6; attempt++)
            {
                var widening = attempt % 3;
                var lo = widening == 0 ? bandMin : Math.Min(bandMin, widening == 1 ? WideBandMin : WidestBandMin);
                var hi = widening == 0 ? bandMax : Math.Max(bandMax, widening == 1 ? WideBandMax : WidestBandMax);
                // A widening that changes nothing (a band asked wider than it) is not tried twice.
                if (widening > 0)
                {
                    var prevLo = widening == 1 ? bandMin : Math.Min(bandMin, WideBandMin);
                    var prevHi = widening == 1 ? bandMax : Math.Max(bandMax, WideBandMax);
                    if (lo == prevLo && hi == prevHi) continue;
                }
                var glass = attempt >= 3;
                if (!TryBand(cache, startArea, startDoor, floorSeed, lo, hi, bandMin, bandMax, glass, ref plan, out var bandLost))
                {
                    lost += bandLost;
                    continue;
                }
                plan.widened = widening > 0;
                plan.throughGlass = glass;
                plan.lostToFrontRules = lost;
                plan.corridor = CorridorPastDoor(cache, startArea, plan.exitCell);
                return plan;
            }
            return plan;
        }

        struct Candidate
        {
            public GridCoord exit;
            public int distance;
            public bool deadEnd;
        }

        /// <summary>
        /// How many walking cells a distance lies outside the band asked (0 inside it): the nearest-first rule of a
        /// widened band (C23 round 2). The validator reads it too.
        /// </summary>
        public static int NearestGap(int distance, int askedMin, int askedMax) =>
            distance < askedMin ? askedMin - distance : distance > askedMax ? distance - askedMax : 0;

        static bool TryBand(FrontRoomsMapCache cache, CellRect area, GridCoord door, int floorSeed, int lo, int hi, int askedMin, int askedMax, bool glass, ref ExitPlan plan,
            out int lostToFrontRules)
        {
            // Every N lies at most hi - 1 steps out (removing X never shortens a walk), so the walk stops there.
            var dist = WalkDistances(cache, area, door, default(GridCoord), false, hi - 1, glass);
            var found = new List<Candidate>();
            int before = 0, height = 0, sightline = 0, shiftOnly = 0;
            foreach (var pair in dist)
            {
                var front = pair.Key;
                var exit = new GridCoord(front.x, front.y + 1);
                if (!ExitCellFits(cache, area, exit)) continue;
                // The walk to N without X. If X is no nearer the door than N, no shortest walk to N passes
                // through X, so the plain distance stands; otherwise walk again round X.
                var steps = pair.Value;
                if (dist.TryGetValue(exit, out var toExit) && toExit < steps)
                    steps = WalkStepsTo(cache, area, door, front, exit, hi - 1, glass);
                if (steps < 0 || steps + 1 < lo || steps + 1 > hi) continue;
                if (!KeepsChunkWhole(cache, area, exit, front)) continue;
                // The front rules last, so the cells they turn down are counted among those every other rule let through.
                before++;
                var standard = FrontStandard(cache, front);
                var view = Sightline(cache, exit);
                if (!standard) height++;
                if (view != SightlineView.Clear)
                {
                    sightline++;
                    if (view == SightlineView.MayGetWindow) shiftOnly++;
                }
                if (!standard || view != SightlineView.Clear) continue;
                found.Add(new Candidate { exit = exit, distance = steps + 1, deadEnd = IsDeadEnd(cache, exit) });
            }
            lostToFrontRules = found.Count == 0 ? before : 0;
            if (found.Count == 0) return false;
            plan.beforeFrontRules = before;
            plan.rejectedHeight = height;
            plan.rejectedSightline = sightline;
            plan.rejectedShiftOnly = shiftOnly;
            found.Sort((a, b) => a.exit.x != b.exit.x ? a.exit.x.CompareTo(b.exit.x) : a.exit.y.CompareTo(b.exit.y));
            var deadEnds = 0;
            foreach (var c in found) if (c.deadEnd) deadEnds++;
            // Nearest first (C23 round 2): only the candidates whose walk lies fewest cells outside the band asked
            // (all of them in the band asked, where every gap is 0), then the dead ends among those, then the hash.
            var gap = int.MaxValue;
            foreach (var c in found) gap = Math.Min(gap, NearestGap(c.distance, askedMin, askedMax));
            var nearest = found.FindAll(c => NearestGap(c.distance, askedMin, askedMax) == gap);
            var nearestDeadEnds = 0;
            foreach (var c in nearest) if (c.deadEnd) nearestDeadEnds++;
            var pool = nearestDeadEnds > 0 ? nearest.FindAll(c => c.deadEnd) : nearest;
            var pick = pool[(int)(MapHash.Hash(floorSeed, 0, 0, SaltExitPick) % (uint)pool.Count)];
            plan.found = true;
            plan.exitCell = pick.exit;
            plan.frontCell = FrontOf(pick.exit);
            plan.distance = pick.distance;
            plan.bandMin = lo;
            plan.bandMax = hi;
            plan.candidates = found.Count;
            plan.deadEnds = deadEnds;
            plan.deadEnd = pick.deadEnd;
            plan.nearestGap = gap;
            plan.nearest = nearest.Count;
            plan.nearestDeadEnds = nearestDeadEnds;
            return true;
        }

        /// <summary>
        /// The cell rules for X that need no walk: off the start area and its ring, chunk-local 1..6 on both axes,
        /// outside every room rect of its chunk, no column on its corners, not its zone's key site or key cell.
        /// </summary>
        public static bool ExitCellFits(FrontRoomsMapCache cache, CellRect area, GridCoord exit)
        {
            if (cache == null || InAreaOrRing(area, exit)) return false;
            var coord = MapGrid.ChunkOf(exit);
            var origin = MapGrid.ChunkOrigin(coord);
            int i = exit.x - origin.x, j = exit.y - origin.y;
            if (i < 1 || i > N - 2 || j < 1 || j > N - 2) return false;
            var chunk = cache.Get(coord);
            foreach (var room in chunk.rooms)
                if (room.Contains(i, j)) return false;
            const int c = N + 1;
            if (chunk.pillar[i + j * c] || chunk.pillar[i + 1 + j * c] || chunk.pillar[i + (j + 1) * c] || chunk.pillar[i + 1 + (j + 1) * c]) return false;
            // A zone is named by the chunk its site lies in, and that chunk carries its key (PlaceKey). A key
            // (site cell, or a module's key spot) always lies in its own zone, so X's zone is the only one to ask.
            var home = cache.Get(cache.ZoneOf(exit).id);
            return !(home.hasKey && (home.keyCell == exit || home.keySiteCell == exit));
        }

        /// <summary>X's generated west, east and north edges are walls: a dead end off N.</summary>
        public static bool IsDeadEnd(FrontRoomsMapCache cache, GridCoord exit) =>
            cache.Edge(exit, new GridCoord(exit.x - 1, exit.y)) == EdgeKind.Wall
            && cache.Edge(exit, new GridCoord(exit.x + 1, exit.y)) == EdgeKind.Wall
            && cache.Edge(exit, new GridCoord(exit.x, exit.y + 1)) == EdgeKind.Wall;

        // ---------- The front rules (C23 step 1, 2026-10-22) ----------

        /// <summary>How many cells of the EXIT door's sightline the window rule asks: N and the two cells south of it.</summary>
        public const int SightlineCells = 3;

        /// <summary>The EXIT door's sightline: clear; a window on it now; or none now, but an edge that a revisit shift could make a window.</summary>
        public enum SightlineView { Clear, Window, MayGetWindow }

        /// <summary>The front rule on N's height: N is Standard (2.9 m), never Low or Tall, so the EXIT sign fits over the door.</summary>
        public static bool FrontStandard(FrontRoomsMapCache cache, GridCoord front) => cache.ZoneOf(front).height == ZoneHeight.Standard;

        /// <summary>
        /// The front rule on glass: the edges round N, N + (0, -1) and N + (0, -2), the corridor the player sees the
        /// EXIT door down, but N's north edge (the EXIT door itself). Window when one of them is glass as generated;
        /// MayGetWindow when none is, but one runs between a Tall zone and a lower one inside a chunk that may shift
        /// (any but X's own; MayBecomeWindow); Clear otherwise, for as long as the floor lasts.
        /// </summary>
        public static SightlineView Sightline(FrontRoomsMapCache cache, GridCoord exit)
        {
            var front = FrontOf(exit);
            var exitChunk = MapGrid.ChunkOf(exit);
            var view = SightlineView.Clear;
            for (var k = 0; k < SightlineCells; k++)
            {
                var cell = new GridCoord(front.x, front.y - k);
                foreach (var step in Steps4)
                {
                    var other = cell + step;
                    if (other == exit) continue;
                    if (cache.Edge(cell, other) == EdgeKind.Window) return SightlineView.Window;
                    if (view == SightlineView.Clear && MayBecomeWindow(cache, cell, other, exitChunk)) view = SightlineView.MayGetWindow;
                }
            }
            return view;
        }

        /// <summary>
        /// An edge a revisit shift could make glass: it runs between a Tall zone and a lower one (where alone the
        /// generator and the module stamp put windows; zones never change), and inside one chunk that is not
        /// <paramref name="exitChunk"/> (a chunk border edge is a function of the seed alone, and X's chunk never
        /// shifts).
        /// </summary>
        public static bool MayBecomeWindow(FrontRoomsMapCache cache, GridCoord a, GridCoord b, GridCoord exitChunk)
        {
            if ((cache.ZoneOf(a).height == ZoneHeight.Tall) == (cache.ZoneOf(b).height == ZoneHeight.Tall)) return false;
            var chunk = MapGrid.ChunkOf(a);
            return chunk == MapGrid.ChunkOf(b) && chunk != exitChunk;
        }

        /// <summary>
        /// The corridor rule for the EXIT sign: N's west and east neighbours are both outside the start area and N's
        /// edges to them are walkable (no wall, no glass; the glass rule keeps windows off them anyway). The corridor
        /// then runs past the door in the side of its wall, and the sign is the end-on blade. Both edges are inner
        /// edges of X's chunk (X is chunk-local 1..6), which never shifts, so the answer holds for the floor.
        /// </summary>
        public static bool CorridorPastDoor(FrontRoomsMapCache cache, CellRect area, GridCoord exit)
        {
            var front = FrontOf(exit);
            var west = new GridCoord(front.x - 1, front.y);
            var east = new GridCoord(front.x + 1, front.y);
            return !InArea(area, west) && !InArea(area, east) && Walkable(cache.Edge(front, west)) && Walkable(cache.Edge(front, east));
        }

        /// <summary>
        /// Removing X keeps X's chunk whole: inside the chunk, never through the start area, with every edge but a
        /// wall passable (the map's own guarantee counts glass), N reaches X, and without X N still reaches every
        /// other cell it reached with it. X must be chunk-local 1..6 (its neighbours and N are then in the chunk).
        /// Reads only X's chunk, which never shifts (D5), so the answer holds for the floor.
        /// </summary>
        public static bool KeepsChunkWhole(FrontRoomsMapCache cache, CellRect area, GridCoord exit, GridCoord front)
        {
            var chunk = cache.Get(MapGrid.ChunkOf(exit));
            var origin = chunk.Origin;
            if (MapGrid.ChunkOf(front) != chunk.coord) return false;
            var x = MapGrid.LocalIndex(exit.x - origin.x, exit.y - origin.y);
            var n = MapGrid.LocalIndex(front.x - origin.x, front.y - origin.y);
            var withX = new bool[MapGrid.CellsPerChunk];
            var withoutX = new bool[MapGrid.CellsPerChunk];
            var reached = ReachInChunk(chunk, area, n, -1, withX);
            if (!withX[x]) return false;
            return ReachInChunk(chunk, area, n, x, withoutX) == reached - 1;
        }

        // Cells of one chunk reached from local index `from` over its own inner edges (not walls), never through
        // `avoid` or a start-area cell. Fills `seen`; returns the count.
        static int ReachInChunk(MapChunk chunk, CellRect area, int from, int avoid, bool[] seen)
        {
            var origin = chunk.Origin;
            var stack = new Stack<int>();
            seen[from] = true;
            stack.Push(from);
            var count = 1;
            while (stack.Count > 0)
            {
                var k = stack.Pop();
                int i = k % N, j = k / N;
                for (var d = 0; d < 4; d++)
                {
                    int ni = i, nj = j;
                    EdgeKind kind;
                    if (d == 0) { if (i + 1 >= N) continue; ni++; kind = chunk.east[k]; }
                    else if (d == 1) { if (i == 0) continue; ni--; kind = chunk.east[k - 1]; }
                    else if (d == 2) { if (j + 1 >= N) continue; nj++; kind = chunk.north[k]; }
                    else { if (j == 0) continue; nj--; kind = chunk.north[k - N]; }
                    var next = MapGrid.LocalIndex(ni, nj);
                    if (seen[next] || next == avoid || !MapGrid.Passable(kind)) continue;
                    if (InArea(area, new GridCoord(origin.x + ni, origin.y + nj))) continue;
                    seen[next] = true;
                    count++;
                    stack.Push(next);
                }
            }
            return count;
        }

        /// <summary>
        /// Walking distances from <paramref name="door"/>: BFS over walkable edges (and windows when
        /// <paramref name="glass"/>), never into the start area (nor into <paramref name="avoid"/> when
        /// <paramref name="hasAvoid"/>), out to <paramref name="maxSteps"/>.
        /// </summary>
        public static Dictionary<GridCoord, int> WalkDistances(FrontRoomsMapCache cache, CellRect area, GridCoord door, GridCoord avoid, bool hasAvoid, int maxSteps, bool glass = false)
        {
            var dist = new Dictionary<GridCoord, int>();
            if (InArea(area, door) || (hasAvoid && door == avoid)) return dist;
            var queue = new Queue<GridCoord>();
            dist[door] = 0;
            queue.Enqueue(door);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                var d = dist[cell];
                if (d >= maxSteps) continue;
                foreach (var step in Steps4)
                {
                    var next = cell + step;
                    if (dist.ContainsKey(next) || InArea(area, next) || (hasAvoid && next == avoid)) continue;
                    var kind = cache.Edge(cell, next);
                    if (kind == EdgeKind.Wall || (!glass && kind == EdgeKind.Window)) continue;
                    dist[next] = d + 1;
                    queue.Enqueue(next);
                }
            }
            return dist;
        }

        /// <summary>Walking steps from the door to <paramref name="target"/> without entering <paramref name="avoid"/>; -1 beyond <paramref name="maxSteps"/>.</summary>
        public static int WalkStepsTo(FrontRoomsMapCache cache, CellRect area, GridCoord door, GridCoord target, GridCoord avoid, int maxSteps, bool glass = false)
        {
            var dist = WalkDistances(cache, area, door, avoid, true, maxSteps, glass);
            return dist.TryGetValue(target, out var steps) ? steps : -1;
        }

        // ---------- The next floor's seed (D2) ----------

        /// <summary>The k-th candidate seed of a floor after the first: a hash of the run seed, the floor and k.</summary>
        public static int FloorSeed(int runSeed, int floor, int k) => unchecked((int)MapHash.Hash(runSeed, floor, k, SaltFloorSeed));

        /// <summary>
        /// The seed of floor <paramref name="floor"/> (2 and up) whose start door, X's north edge, fits: the cell
        /// X + (0, 1) is Standard-height Level 0 and the way on from it is open and leads into the maze. These are
        /// MapRootFor's tests for a 1-cell start area (X), with one change: glass blocks the way on, so a new floor
        /// never starts behind a window, and one more: G1's placard fits at P1 west beside the door
        /// (StairDoorPlacardWest). The root cannot move under a sealed player, so the seed is searched instead:
        /// k = 0, 1, ... up to MaxSeedTries; failing that, the first Standard seed without the placard wall; then the
        /// first seed whose way on only leads on (any height), and failing that k = 0. <paramref name="cacheForSeed"/> makes the cache the floor's map
        /// would make for a seed (its tier already set); the one that passed is returned in <paramref name="cache"/>,
        /// with the probed chunks already generated, so the EXIT of that floor can be chosen on it.
        /// </summary>
        public static FloorSeedPlan SeedFor(int runSeed, int floor, GridCoord exitCell, Func<int, FrontRoomsMapCache> cacheForSeed, out FrontRoomsMapCache cache)
        {
            cache = null;
            if (cacheForSeed == null) return new FloorSeedPlan { seed = FloorSeed(runSeed, floor, 0) };
            var door = new GridCoord(exitCell.x, exitCell.y + 1);
            FrontRoomsMapCache fallback = null, standardOnly = null;
            var fallbackPlan = new FloorSeedPlan();
            var standardOnlyPlan = new FloorSeedPlan();
            for (var k = 0; k < MaxSeedTries; k++)
            {
                var seed = FloorSeed(runSeed, floor, k);
                var probe = cacheForSeed(seed);
                var standard = StartRowStandard(probe, door, 0);
                if (!standard && (fallback != null || standardOnly != null)) continue;
                if (!StartRowLeadsOn(probe, door, 0, 1, true)) continue;
                var west = standard && StairDoorPlacardWest(probe, door);
                var plan = new FloorSeedPlan { seed = seed, tries = k + 1, standard = standard, leadsOn = true, placardWest = west };
                if (west)
                {
                    cache = probe;
                    return plan;
                }
                if (standard)
                {
                    if (standardOnly == null) { standardOnly = probe; standardOnlyPlan = plan; }
                    continue;
                }
                fallback = probe;
                fallbackPlan = plan;
            }
            if (standardOnly != null)
            {
                cache = standardOnly;
                return standardOnlyPlan;
            }
            if (fallback != null)
            {
                cache = fallback;
                return fallbackPlan;
            }
            var first = FloorSeed(runSeed, floor, 0);
            cache = cacheForSeed(first);
            return new FloorSeedPlan { seed = first, tries = MaxSeedTries };
        }

        /// <summary>SeedFor with the cache a map makes from generation numbers and a module library (FrontRoomsMapWorld.CreateCache), at <paramref name="tier"/>.</summary>
        public static FloorSeedPlan SeedFor(int runSeed, int floor, GridCoord exitCell, MapSettings settings, IReadOnlyList<RoomModuleData> modules, int tier, out FrontRoomsMapCache cache) =>
            SeedFor(runSeed, floor, exitCell, seed => CacheFor(settings, modules, seed, tier), out cache);

        /// <summary>A cache for one seed of these generation numbers, generating at <paramref name="tier"/>.</summary>
        public static FrontRoomsMapCache CacheFor(MapSettings settings, IReadOnlyList<RoomModuleData> modules, int seed, int tier)
        {
            var copy = (settings ?? new MapSettings()).Clone();
            copy.seed = seed;
            return new FrontRoomsMapCache(copy, modules) { Tier = Math.Max(1, tier) };
        }

        // ---------- The start row (MapRootFor's tests, shared) ----------

        /// <summary>The 2 * halfCells + 1 cells across the door row are all Standard-height Level 0.</summary>
        public static bool StartRowStandard(FrontRoomsMapCache cache, GridCoord door, int halfCells)
        {
            for (var dx = -halfCells; dx <= halfCells; dx++)
            {
                var zone = cache.ZoneOf(new GridCoord(door.x + dx, door.y));
                if (zone.height != ZoneHeight.Standard || zone.theme != ZoneTheme.Level0) return false;
            }
            return true;
        }

        /// <summary>
        /// G1 (tutorial phase 0, 平面视觉's sheet): the door cell's west edge is Open, so the evacuation plan hangs on
        /// the start wall west of the door (P1 west, FrontRoomsPlacard) on every seed and its YOU ARE HERE is true.
        /// </summary>
        public static bool StartDoorWestOpen(FrontRoomsMapCache cache, GridCoord door) =>
            cache.Edge(door, new GridCoord(door.x - 1, door.y)) == EdgeKind.Open;

        /// <summary>
        /// G1 on floors 2+ (游戏视觉: the B1+ sheets hang on the map side beside the stairwell's north door, west first,
        /// as on floor 1): the door cell's west edge is Open, and the cell west of it has a wall on its south edge to
        /// hang the frame on (floor 1's facade strip gives the title door that wall; a 1-cell start area does not).
        /// </summary>
        public static bool StairDoorPlacardWest(FrontRoomsMapCache cache, GridCoord door)
        {
            var west = new GridCoord(door.x - 1, door.y);
            return StartDoorWestOpen(cache, door) && cache.Edge(west, new GridCoord(west.x, west.y - 1)) == EdgeKind.Wall;
        }

        /// <summary>
        /// The door cell's far edge is neither wall nor window, and from the cell beyond it the walk reaches the
        /// maze (LeadsIntoMaze) without coming back into the door cell or the start area: the
        /// (2 * halfCells + 1) x rows strip right behind the door.
        /// </summary>
        public static bool StartRowLeadsOn(FrontRoomsMapCache cache, GridCoord door, int halfCells, int rows, bool glassBlocks)
        {
            var area = new CellRect(door.x - halfCells, door.y - rows, halfCells * 2 + 1, rows);
            var ahead = new GridCoord(door.x, door.y + 1);
            var onward = cache.Edge(door, ahead);
            if (onward == EdgeKind.Wall || onward == EdgeKind.Window) return false;
            return LeadsIntoMaze(cache, ahead, area, door, glassBlocks);
        }

        /// <summary>
        /// From <paramref name="start"/>, through anything but a wall (and glass when <paramref name="glassBlocks"/>)
        /// and never back into the door cell or the start area, the walk reaches a good part of the maze
        /// (LeadsOnCells; a pocket cut off by the strip is far smaller). FrontRooms3DGame.LeadsIntoMaze, cell for cell.
        /// </summary>
        public static bool LeadsIntoMaze(FrontRoomsMapCache cache, GridCoord start, CellRect area, GridCoord door, bool glassBlocks)
        {
            var seen = new HashSet<GridCoord> { start, door };
            var queue = new Queue<GridCoord>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                foreach (var step in Steps4)
                {
                    var next = cell + step;
                    if (seen.Contains(next) || InArea(area, next)) continue;
                    var kind = cache.Edge(cell, next);
                    if (kind == EdgeKind.Wall || (glassBlocks && kind == EdgeKind.Window)) continue;
                    seen.Add(next);
                    if (seen.Count >= LeadsOnCells) return true;
                    queue.Enqueue(next);
                }
            }
            return false;
        }

        /// <summary>
        /// Where a start door puts the map root: FrontRooms3DGame.MapRootFor in cells. <paramref name="doorWorldCell"/>
        /// is the cell the door opens onto with the root at the origin; the rings of whole world periods round it
        /// are searched for the first root whose door row passes StartRowStandard, StartRowLeadsOn (glass passable,
        /// as the title's search has it) and, with <paramref name="placardWest"/> (G1, the game's rule), StartDoorWestOpen;
        /// else the first Standard row that leads on; else the first that only leads on; else the origin. The game
        /// still runs its own copy; this one is for data (the validator's floor 1) until the game calls it. Tests pass
        /// placardWest false to pin a root from before G1.
        /// </summary>
        public static RootPlan RootFor(FrontRoomsMapCache cache, GridCoord doorWorldCell, int halfCells, int rows, bool glassBlocks = false, bool placardWest = true)
        {
            var period = PeriodCells;
            var reachable = new RootPlan();
            var standardOnly = new RootPlan();
            var haveReachable = false;
            var haveStandard = false;
            for (var ring = 0; ring <= RootRings; ring++)
            for (var b = -ring; b <= ring; b++)
            for (var a = -ring; a <= ring; a++)
            {
                if (Math.Max(Math.Abs(a), Math.Abs(b)) != ring) continue;
                var root = new GridCoord(a * period, b * period);
                var door = new GridCoord(doorWorldCell.x - root.x, doorWorldCell.y - root.y);
                var standard = StartRowStandard(cache, door, halfCells);
                if (!standard && (haveReachable || haveStandard)) continue;
                var west = StartDoorWestOpen(cache, door);
                if (standard && haveStandard && !west) continue;
                if (!StartRowLeadsOn(cache, door, halfCells, rows, glassBlocks)) continue;
                var plan = new RootPlan { root = root, doorCell = door, standard = standard, leadsOn = true, placardWest = west };
                if (standard && (west || !placardWest)) return plan;
                if (standard) { standardOnly = plan; haveStandard = true; }
                else { reachable = plan; haveReachable = true; }
            }
            if (haveStandard) return standardOnly;
            return haveReachable ? reachable : new RootPlan { doorCell = doorWorldCell };
        }
    }
}
