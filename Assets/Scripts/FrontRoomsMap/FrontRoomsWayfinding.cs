using System;
using System.Collections.Generic;
using FrontRooms.Map;
using UnityEngine;

/// <summary>
/// The wall-cue route (C24 = A and A20, Red 2026-10-22; Documentation/research/cue_driver/MAP_ROUTE_SPEC.md v1.0.2):
/// one path field over the built cells that gives every built cell a route dir and a message, for the wallpaper
/// arrows (FrontRoomsWayCueDriver writes them into _FR_WayCells) and for the station TV's beacon (BeaconPath,
/// tutorial M5). One field, two readers, so the arrows and the beacon never pull two ways.
/// - Target: the station TV's cell while the floor has a station and its key is not taken (SetStationTarget,
///   SetStationKeyTaken: FrontRoomsWayCueDriver sends the map's own station, A26's, and its take, M5), else N, the map cell in front of the
///   EXIT door (SetExit). No EXIT: no target, and every cell is message 0 (spec 7).
/// - Edges, walking toward the target: Open, Arch, an open or broken door 0; an ajar door +4 m, a shut one +9 m
///   (OpeningCostBetween, hearing's costs); a door locked to a player without its key, glass (whole or broken: the
///   cue never points at glass), walls and the reserved cells (X, the start area) never. Octile like
///   FrontRoomsMapPathField (same step lengths, a diagonal only between two plain Open sides). The field reads the
///   map alone: never the Relay (R3), and never a chunk that is not built (a frontier edge is read from the seed's
///   border rule, FrontRoomsMapGenerator.BorderEdge, so nothing is generated).
/// - Cap 150 m (SP) of path from the target. Every built cell the target pass does not hold within the cap (past it,
///   or never reached: a part of the built map joined to the target only through unbuilt chunks, glass or a locked
///   door), and every cell while the target's chunk is not built, takes the frontier rule (spec 3): the seeds are
///   those cells with a walkable edge (not glass) to an unbuilt, unreserved cell, each at the straight metres from
///   that cell's centre to the target. That pass keeps to its own cells, a diagonal included (both halves in it), so
///   no cell at the cap's edge can point uphill (spec 3, review F1 and F2). Cells neither pass reaches, sealed pockets
///   and glass-only exits included, are message 0.
/// - Per cell: the orthogonal step with the least field(next) + step (a diagonal comes out as one of its halves; a
///   frontier seed may step out of the built map at its remainder). On a tie the cell keeps its last dir if that is
///   still among the best, else +x, +z, -x, -z. N points through the EXIT door into X; the station cell at the TV's
///   wall, or message 0 when the TV hangs on no cell edge.
/// - Rebuilt only on a trigger (Refresh): the map's revision (PassageRevision: streaming, doors, glass; the start
///   area; keys), the window moving with the streaming centre, a target switch (the key, a new floor's EXIT), the
///   target's chunk becoming built. Never per frame. Every buffer is preallocated: a build allocates nothing.
/// The chase's pressure route (RELAY_PURSUIT_REDESIGN.md 9.6, LD R6) is a second field of the same machinery that
/// may read the Relay (BuildPressure); the calm field above never does.
/// </summary>
public sealed class FrontRoomsWayfinding
{
    /// <summary>The calm field's reach, in metres of path from the target (SP, spec 2).</summary>
    public const float CapMetres = 150f;
    /// <summary>The pressure route's reach from its door, and its price for every cell entered within PressureNearCells of the Relay (SP).</summary>
    public const float PressureCapMetres = 45f, PressureNearMetres = 6f;
    public const int PressureNearCells = 2;
    /// <summary>
    /// RELAY 9.6 (系统设计, 2026-10-22): along the pressure route the Relay's path distance may fall at most one cell
    /// below the distance at the cell the route starts from, so the route never leads toward it.
    /// </summary>
    public const float PressureSlackMetres = MapGrid.CellSize;
    /// <summary>The window the fields cover: 64 x 64 cells round the streaming centre, addressed toroidally (cell and 63), as _FR_WayCells is.</summary>
    public const int Side = 64, Mask = Side - 1, Slots = Side * Side;
    /// <summary>The widest buildRadius the window holds (every chunk within 3 of the centre chunk). Past it built cells 64 apart would share a texel, so the driver leaves the cue off.</summary>
    public const int MaxBuildRadius = 3;
    /// <summary>The room set (RELAY 9.2): the walk's depth and size when the player stands in no carved or module room.</summary>
    public const int RoomDepth = 2, RoomCells = 9;
    /// <summary>Dirs: 0 +x, 1 +z, 2 -x, 3 -z (the fixed tie order, and _FR_WayCells' A channel).</summary>
    public static readonly int[] DirX = { 1, 0, -1, 0 }, DirY = { 0, 1, 0, -1 };

    const float Tie = 1e-3f;
    // The window's south-west cell lies this far south-west of its centre chunk's origin, so it holds every chunk
    // within 3 of the centre chunk (buildRadius 2 needs 2).
    const int WindowLead = 28;
    const byte RegionNone = 0, RegionTarget = 1, RegionFrontier = 2, RegionPressure = 3;

    /// <summary>What the room set reads: built cells, generated edges, and a chunk's carved and module rooms.</summary>
    public interface IRoomCells
    {
        /// <summary>The cell's chunk is built and the cell is not reserved.</summary>
        bool IsBuilt(GridCoord cell);
        /// <summary>The generated edge between two side-by-side cells (both of a built chunk).</summary>
        EdgeKind Edge(GridCoord a, GridCoord b);
        /// <summary>A chunk already generated, without generating one.</summary>
        bool TryGetChunk(GridCoord chunk, out MapChunk data);
    }

    /// <summary>
    /// The map as the fields read it. FrontRoomsMapWorld through WorldMap in play; FrontRoomsWayfindingTests gives a
    /// cache with a built-chunk set (100 seeds x 3 floors without geometry) and hand-made grids (the F1/F2 cases).
    /// </summary>
    public interface IMap : IRoomCells
    {
        /// <summary>Changes whenever anything the field reads changes.</summary>
        int Revision { get; }
        /// <summary>The streaming centre: the built chunks lie round its chunk.</summary>
        GridCoord Centre { get; }
        /// <summary>X or the start area: never a route, never a frontier.</summary>
        bool IsReserved(GridCoord cell);
        /// <summary>The edge from a built cell to its unbuilt neighbour, read without generating the neighbour's chunk; a Wall when it is a door locked to the player.</summary>
        EdgeKind FrontierEdge(GridCoord built, GridCoord unbuilt);
        /// <summary>
        /// The extra metres to walk from <paramref name="from"/> into its built neighbour <paramref name="to"/>: 0 open,
        /// arch, open or broken door; 4 ajar; 9 shut; +inf for a wall, glass, or a door locked to a player without its key.
        /// </summary>
        float Crossing(GridCoord from, GridCoord to, out EdgeKind edge);
        /// <summary>A door the player can shut stands on this edge (whole, not being broken, not the EXIT).</summary>
        bool Shuttable(GridCoord a, GridCoord b);
        Vector3 CellCentre(GridCoord cell);
        Vector3 CrossingPoint(GridCoord a, GridCoord b);
    }

    /// <summary>The live map.</summary>
    public sealed class WorldMap : IMap
    {
        readonly FrontRoomsMapWorld world;
        int passage, keys, revision;
        bool startArea, needKeys, seen;

        public WorldMap(FrontRoomsMapWorld world) => this.world = world;

        public FrontRoomsMapWorld World => world;

        public int Revision
        {
            get
            {
                // Keys change what a locked door costs without moving PassageRevision; so does the start area given back.
                // M3: the inner doors' rule (no locked inner doors on Floor 1's station floor, A13), and the floor key held.
                var p = world.PassageRevision;
                var s = world.HasStartArea;
                var k = world.KeysHeld;
                var n = world.InteriorDoorsLocked;
                if (!seen || p != passage || s != startArea || k != keys || n != needKeys)
                {
                    seen = true;
                    passage = p;
                    startArea = s;
                    keys = k;
                    needKeys = n;
                    revision++;
                }
                return revision;
            }
        }

        public GridCoord Centre => world.CellOf(world.StreamFocus ?? (world.Player != null ? world.Player.position : world.transform.position));
        public bool IsBuilt(GridCoord cell) => world.IsBuilt(cell);
        public bool IsReserved(GridCoord cell) => world.IsReserved(cell);
        public EdgeKind Edge(GridCoord a, GridCoord b) => world.Cache.Edge(a, b);
        public bool TryGetChunk(GridCoord chunk, out MapChunk data) => world.Cache.TryGetGenerated(chunk, out data);

        public EdgeKind FrontierEdge(GridCoord built, GridCoord unbuilt)
        {
            var edge = FrontierEdgeOf(world.Cache, built, unbuilt);
            // The door's leaf is not built, so its lock is the rule's: locked with keys on, unless the player holds the key
            // that opens it there (the zone's, or M3's floor key).
            return edge == EdgeKind.Door && world.InteriorDoorsLocked && !world.HoldsKeyAt(built) ? EdgeKind.Wall : edge;
        }

        public float Crossing(GridCoord from, GridCoord to, out EdgeKind edge)
        {
            edge = world.Cache.Edge(from, to);
            switch (edge)
            {
                case EdgeKind.Open:
                case EdgeKind.Arch:
                    return 0f;
                case EdgeKind.Door:
                    var cost = world.OpeningCostBetween(from, to);
                    if (cost < FrontRoomsMapPathField.ShutOpening) return cost;
                    var door = world.DoorBetween(from, to);
                    return door != null && LockedAgainst(door, from) ? float.PositiveInfinity : cost;
                default:
                    return float.PositiveInfinity;
            }
        }

        // KeyFor's rule, read without a Vector3: a shut door locked by the level's rule, not unlocked yet, whose key the
        // player does not hold (the key of the zone the player stands in; on a floor-key floor, M3, the floor key).
        bool LockedAgainst(FrontRoomsMapWorld.Door door, GridCoord from) =>
            !door.open && !door.broken && world.LockedByRule(door) && !world.IsUnlockedForTools(door) && !world.HoldsKeyAt(from);

        public bool Shuttable(GridCoord a, GridCoord b)
        {
            var door = world.DoorBetween(a, b);
            return door != null && !door.broken && !world.IsBeingBroken(door) && !world.IsExitDoor(door);
        }

        public Vector3 CellCentre(GridCoord cell) => world.CellCenter(cell);
        public Vector3 CrossingPoint(GridCoord a, GridCoord b) => world.CrossingPoint(a, b);
    }

    /// <summary>
    /// The edge between a built cell and its unbuilt neighbour without generating the neighbour's chunk: on a chunk
    /// border the seed's border rule (FrontRoomsMapGenerator.BorderEdge, which every chunk builds its borders from),
    /// inside the built cell's own chunk (a reserved neighbour) that chunk's edge.
    /// </summary>
    public static EdgeKind FrontierEdgeOf(FrontRoomsMapCache cache, GridCoord built, GridCoord unbuilt)
    {
        var lo = built;
        var hi = unbuilt;
        if (hi.x < lo.x || hi.y < lo.y) { lo = unbuilt; hi = built; }
        return MapGrid.ChunkOf(lo) != MapGrid.ChunkOf(hi) ? cache.Generator.BorderEdge(lo, hi.x > lo.x) : cache.Edge(lo, hi);
    }

    // One field's buffers: the Dijkstra's stamps, keys and heap, and the per-slot result. Preallocated, reused.
    sealed class Field
    {
        public GridCoord min;
        public int build, pass;
        public readonly int[] reached = new int[Slots], settled = new int[Slots], eligible = new int[Slots], seeded = new int[Slots];
        // builtAt: the slot's cell is built in this build (read once per build, not per relax).
        public readonly int[] builtAt = new int[Slots];
        // dist: the field (door costs included); walk: the same from the pass's seed (the cap's measure); metres: the
        // route's octile metres with no door costs, plus a frontier seed's straight remainder (BeaconPath, F7);
        // openings: the ajar and shut doors on that same route (BeaconPath's shutOpenings, review m4).
        public readonly float[] dist = new float[Slots], walk = new float[Slots], metres = new float[Slots], limit = new float[Slots];
        public readonly int[] openings = new int[Slots];
        // The pressure field only: the Relay's path metres per cell, and the least of them along the route from it;
        // beyond: the far cell of a seed's door this pass (the route ends at the door; the calm route leads on there).
        public readonly float[] relay = new float[Slots], least = new float[Slots];
        public readonly int[] beyond = new int[Slots];
        public readonly sbyte[] seedDir = new sbyte[Slots];
        // The extra metres from a slot's cell into its neighbour k (Crossing), for this build: made once per directed
        // edge, so the frontier pass reads what the target pass already made (R8's cost, review M2).
        public readonly float[] cross = new float[Slots * 4];
        public readonly int[] crossBuild = new int[Slots * 4];
        // The generated edge from a slot's cell to its neighbour k, for this build (both directions filled at once).
        public readonly byte[] edgeKind = new byte[Slots * 4];
        public readonly int[] edgeBuild = new int[Slots * 4];
        public readonly byte[] region = new byte[Slots];
        public readonly sbyte[] dir = new sbyte[Slots], lastDir = new sbyte[Slots];
        public readonly int[] lastX = new int[Slots], lastY = new int[Slots];
        public readonly bool[] hasLast = new bool[Slots];
        public readonly int[] order = new int[Slots * 2], queue = new int[Slots];
        public int count;
        readonly int[] heapNode = new int[Slots * 9];
        readonly float[] heapKey = new float[Slots * 9];
        int heapCount;

        public Field()
        {
            // No route anywhere until the first build.
            for (var s = 0; s < Slots; s++) dir[s] = -1;
        }

        public bool Empty => heapCount == 0;

        public bool InWindow(GridCoord c) => (uint)(c.x - min.x) < Side && (uint)(c.y - min.y) < Side;
        public GridCoord CellAt(int s) => new GridCoord(min.x + (((s & Mask) - min.x) & Mask), min.y + (((s >> 6) - min.y) & Mask));
        public bool Built(int s) => builtAt[s] == build;

        public void Begin(GridCoord window)
        {
            build++;
            // Stamps wrap after about 2^31 passes: clear them, as FrontRoomsMapPathField does.
            if (pass > int.MaxValue - 8 || build > int.MaxValue - 8)
            {
                Array.Clear(reached, 0, Slots);
                Array.Clear(settled, 0, Slots);
                Array.Clear(eligible, 0, Slots);
                Array.Clear(seeded, 0, Slots);
                Array.Clear(beyond, 0, Slots);
                Array.Clear(builtAt, 0, Slots);
                Array.Clear(crossBuild, 0, crossBuild.Length);
                Array.Clear(edgeBuild, 0, edgeBuild.Length);
                pass = 0;
                build = 1;
            }
            min = window;
            count = 0;
        }

        public void NextPass()
        {
            pass++;
            heapCount = 0;
        }

        public void Seed(int s, float h, int sdir, float lim)
        {
            seeded[s] = pass;
            seedDir[s] = (sbyte)sdir;
            Reach(s, h, 0f, h, 0, float.PositiveInfinity, lim);
        }

        public bool Reach(int s, float d, float w, float m, int o, float cap, float lim)
        {
            if (w > cap) return false;
            if (reached[s] == pass && d >= dist[s] - 1e-4f) return false;
            reached[s] = pass;
            dist[s] = d;
            walk[s] = w;
            metres[s] = m;
            openings[s] = o;
            limit[s] = lim;
            var at = heapCount++;
            while (at > 0)
            {
                var parent = (at - 1) / 2;
                if (heapKey[parent] <= d) break;
                heapNode[at] = heapNode[parent];
                heapKey[at] = heapKey[parent];
                at = parent;
            }
            heapNode[at] = s;
            heapKey[at] = d;
            return true;
        }

        public int Pop(out float d)
        {
            var top = heapNode[0];
            d = heapKey[0];
            var lastNode = heapNode[--heapCount];
            var lastKey = heapKey[heapCount];
            var at = 0;
            while (true)
            {
                var child = at * 2 + 1;
                if (child >= heapCount) break;
                if (child + 1 < heapCount && heapKey[child + 1] < heapKey[child]) child++;
                if (heapKey[child] >= lastKey) break;
                heapNode[at] = heapNode[child];
                heapKey[at] = heapKey[child];
                at = child;
            }
            heapNode[at] = lastNode;
            heapKey[at] = lastKey;
            return top;
        }
    }

    readonly IMap map;
    readonly float cap;
    readonly Field calm = new Field(), pressure = new Field();
    readonly System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
    readonly List<GridCoord> beaconRoom = new List<GridCoord>(64);
    readonly float[] stepValue = new float[4];
    // The calm route during a chase: the least Relay distance along each cell's calm route, and whether a row there may step.
    readonly float[] calmLeast = new float[Slots];
    readonly bool[] calmStep = new bool[Slots];

    GridCoord? exit, station;
    int stationDir = -1;
    bool keyTaken, dirty = true;
    // What the last build was made for.
    int builtRevision;
    bool builtHasTarget, builtTargetBuilt;
    GridCoord builtTarget, builtWindow;
    // The target as the last build used it.
    GridCoord target;
    bool hasTarget, targetIsStation;
    int calmPass1 = -1, calmPass2 = -1;
    GridCoord pressureRelay;
    bool pressureValid, pressureReachesPlayer;

    /// <summary>A field over <paramref name="map"/>; <paramref name="capMetres"/> is the calm reach (CapMetres in play; tests use small caps for the cap-edge cases).</summary>
    public FrontRoomsWayfinding(IMap map, float capMetres = CapMetres)
    {
        this.map = map;
        cap = capMetres;
    }

    public IMap Map => map;
    public float Cap => cap;

    /// <summary>The pressure route's reading of the Relay: path metres from its cell (the hunter's PathDistanceFromRelay; +inf beyond its reach).</summary>
    public Func<GridCoord, float> RelayDistance { get; set; }

    // ---------- Stats (R8: cost per rebuild, logged by the driver) ----------

    /// <summary>Calm rebuilds so far; the ones that ran the frontier pass; the ones whose first pass met the cap.</summary>
    public int Builds { get; private set; }
    public int FrontierBuilds { get; private set; }
    public int CapBuilds { get; private set; }
    public int PressureBuilds { get; private set; }
    public float LastBuildMs { get; private set; }
    public float MaxBuildMs { get; private set; }
    public float TotalBuildMs { get; private set; }
    public float MaxPressureMs { get; private set; }
    public float LastPressureMs { get; private set; }
    /// <summary>The last build's cells with message 1, of them the frontier-routed ones, and the built cells in its window.</summary>
    public int RoutedCells { get; private set; }
    public int FrontierCells { get; private set; }
    public int BuiltCells { get; private set; }

    public void ResetStats()
    {
        Builds = FrontierBuilds = CapBuilds = PressureBuilds = 0;
        LastBuildMs = MaxBuildMs = TotalBuildMs = MaxPressureMs = 0f;
    }

    // ---------- Targets ----------

    /// <summary>X, the floor's EXIT cell (the map's ExitCell while HasExitArea), or null for no EXIT. The target is N = X + (0, -1).</summary>
    public void SetExit(GridCoord? exitCell)
    {
        if (exit == exitCell) return;
        exit = exitCell;
        dirty = true;
    }

    /// <summary>
    /// The floor's station (tutorial M4): the TV's cell and the dir of the cell edge it hangs on (0 +x, 1 +z, 2 -x, 3 -z;
    /// -1 when it hangs on none, and the station cell is then message 0: the TV is in view there). Null: no station.
    /// It leaves the key as it is: a station sent again on the same floor (a revisit shift, a TV edge fix) never
    /// takes the arrows back to the TV after the take (A20; review m5). A new floor starts from ClearStation.
    /// </summary>
    public void SetStationTarget(GridCoord? tvCell, int tvEdgeDir)
    {
        var edge = tvCell.HasValue && tvEdgeDir >= 0 && tvEdgeDir < 4 ? tvEdgeDir : -1;
        if (station == tvCell && stationDir == edge) return;
        station = tvCell;
        stationDir = edge;
        dirty = true;
    }

    /// <summary>A new floor: no station, and its key not taken.</summary>
    public void ClearStation()
    {
        station = null;
        stationDir = -1;
        keyTaken = false;
        dirty = true;
    }

    /// <summary>
    /// A new floor: forget the tie memory (both fields), so a floor's arrows are a fresh build's of the same map and
    /// never lean on the last floor's dirs (R8; review m8).
    /// </summary>
    public void ResetHysteresis()
    {
        Array.Clear(calm.hasLast, 0, Slots);
        Array.Clear(pressure.hasLast, 0, Slots);
        dirty = true;
    }

    /// <summary>A20's switch (M6's StationKeyTaken): once the floor key is taken the field leads to the EXIT, and the beacon is off.</summary>
    public void SetStationKeyTaken(bool taken)
    {
        if (keyTaken == taken) return;
        keyTaken = taken;
        dirty = true;
    }

    public bool StationKeyTaken => keyTaken;
    public GridCoord? Station => station;

    /// <summary>The target now: the station until its key is taken, else N; false with no EXIT and no station.</summary>
    public bool TryGetTarget(out GridCoord cell, out bool isStation)
    {
        if (station.HasValue && !keyTaken) { cell = station.Value; isStation = true; return true; }
        isStation = false;
        if (exit.HasValue) { cell = FrontRoomsExitPlanner.FrontOf(exit.Value); return true; }
        cell = default;
        return false;
    }

    /// <summary>The last build's target (what the field leads to).</summary>
    public bool HasTarget => hasTarget;
    public GridCoord Target => target;
    public bool TargetIsStation => hasTarget && targetIsStation;

    /// <summary>Rebuild now on the next Refresh, whatever changed (a new floor, tools).</summary>
    public void Invalidate() => dirty = true;

    /// <summary>
    /// Rebuilds the calm field when a trigger fired: the map's revision, the window, the target, or the target's chunk
    /// becoming built. Returns true when it rebuilt. Cheap otherwise: call it every frame.
    /// </summary>
    public bool Refresh()
    {
        var has = TryGetTarget(out var t, out _);
        var revision = map.Revision;
        var targetBuilt = has && map.IsBuilt(t);
        if (!dirty && revision == builtRevision && has == builtHasTarget && (!has || t == builtTarget) && targetBuilt == builtTargetBuilt
            && WindowFor(map.Centre) == builtWindow) return false;
        Rebuild();
        return true;
    }

    /// <summary>Rebuild the calm field now.</summary>
    public void Rebuild()
    {
        watch.Restart();
        dirty = false;
        builtRevision = map.Revision;
        hasTarget = TryGetTarget(out target, out targetIsStation);
        builtHasTarget = hasTarget;
        builtTarget = target;
        builtTargetBuilt = hasTarget && map.IsBuilt(target);
        builtWindow = WindowFor(map.Centre);
        BuildCalm(builtWindow);
        pressureValid = false;
        pressureReachesPlayer = false;
        Revision++;
        Builds++;
        LastBuildMs = (float)watch.Elapsed.TotalMilliseconds;
        TotalBuildMs += LastBuildMs;
        if (LastBuildMs > MaxBuildMs) MaxBuildMs = LastBuildMs;
    }

    /// <summary>Counts calm rebuilds, for readers that cache what the field said.</summary>
    public int Revision { get; private set; }

    // ---------- Reading the field ----------

    public static int Slot(GridCoord c) => (c.x & Mask) | ((c.y & Mask) << 6);

    /// <summary>The window cell a slot holds in the last build (each slot holds exactly one cell of the window).</summary>
    public GridCoord CellAtSlot(int slot) => calm.CellAt(slot);
    public bool InWindow(GridCoord cell) => calm.InWindow(cell);
    public GridCoord WindowMin => calm.min;

    /// <summary>The route dir of a cell (message 1), or false (message 0: no target, not built, not reached, or outside the window).</summary>
    public bool Route(GridCoord cell, out int dir)
    {
        if (!calm.InWindow(cell)) { dir = -1; return false; }
        dir = calm.dir[Slot(cell)];
        return dir >= 0;
    }

    public int Message(GridCoord cell) => Route(cell, out _) ? 1 : 0;

    /// <summary>_FR_WayCells' A channel for the cell a slot holds: dir + 4 x message (0 for message 0).</summary>
    public byte TexelA(int slot)
    {
        var d = calm.dir[slot];
        return d >= 0 ? (byte)(d + 4) : (byte)0;
    }

    /// <summary>The field at a cell: metres to the target (frontier cells: walk + straight remainder), +inf where not reached.</summary>
    public float FieldAt(GridCoord cell)
    {
        if (!calm.InWindow(cell)) return float.PositiveInfinity;
        var s = Slot(cell);
        return calm.region[s] != RegionNone ? calm.dist[s] : float.PositiveInfinity;
    }

    /// <summary>The route's octile metres from a cell to the target with no door costs (frontier cells: plus the straight remainder); +inf where not reached.</summary>
    public float MetresAt(GridCoord cell)
    {
        if (!calm.InWindow(cell)) return float.PositiveInfinity;
        var s = Slot(cell);
        return calm.region[s] != RegionNone ? calm.metres[s] : float.PositiveInfinity;
    }

    /// <summary>1 reached by the target, 2 by the frontier rule, 0 not reached.</summary>
    public int RegionAt(GridCoord cell) => calm.InWindow(cell) ? calm.region[Slot(cell)] : 0;

    /// <summary>The cell is one of the frontier pass's seeds (a built cell with a walkable edge to an unbuilt one).</summary>
    public bool IsFrontierSeed(GridCoord cell) => calmPass2 >= 0 && calm.InWindow(cell) && calm.region[Slot(cell)] == RegionFrontier && calm.seeded[Slot(cell)] == calmPass2;

    /// <summary>
    /// Tests (R2, R5, the hysteresis): what stepping from a cell in dir k is worth in the last build, field(next) + step,
    /// or the frontier remainder for a seed stepping out; +inf where the step is not allowed. The dir chosen is the least.
    /// </summary>
    public float StepValue(GridCoord cell, int k)
    {
        if (!calm.InWindow(cell)) return float.PositiveInfinity;
        var s = Slot(cell);
        var r = calm.region[s];
        return r == RegionNone ? float.PositiveInfinity : Value(calm, s, cell, k, r, false, default);
    }

    /// <summary>Straight metres between two cell centres.</summary>
    public static float Straight(GridCoord a, GridCoord b)
    {
        float dx = a.x - b.x, dy = a.y - b.y;
        return MapGrid.CellSize * Mathf.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>A stable hash of the last build (dirs, messages, regions, the field to the millimetre): R3 and R8.</summary>
    public uint Hash()
    {
        var h = 2166136261u;
        Mix(ref h, calm.min.x);
        Mix(ref h, calm.min.y);
        for (var s = 0; s < Slots; s++)
        {
            Mix(ref h, calm.dir[s]);
            Mix(ref h, calm.region[s]);
            Mix(ref h, calm.region[s] != RegionNone ? Mathf.RoundToInt(calm.dist[s] * 1000f) : -1);
        }
        return h;
    }

    static void Mix(ref uint h, int v)
    {
        unchecked { h = (h ^ (uint)v) * 16777619u; }
    }

    // ---------- The calm field ----------

    static GridCoord WindowFor(GridCoord centre)
    {
        var o = MapGrid.ChunkOrigin(MapGrid.ChunkOf(centre));
        return new GridCoord(o.x - WindowLead, o.y - WindowLead);
    }

    // Which window cells are built, read once per build (the relax loop reads the stamp, not the map).
    int MarkBuilt(Field f)
    {
        var n = 0;
        for (var s = 0; s < Slots; s++)
        {
            if (!map.IsBuilt(f.CellAt(s))) continue;
            f.builtAt[s] = f.build;
            n++;
        }
        return n;
    }

    void BuildCalm(GridCoord window)
    {
        var f = calm;
        f.Begin(window);
        BuiltCells = MarkBuilt(f);
        calmPass1 = -1;
        calmPass2 = -1;
        int routed = 0, frontier = 0;
        if (hasTarget)
        {
            if (f.InWindow(target) && f.Built(Slot(target)))
            {
                // Pass 1: the target, uncapped, so the cells past the cap are known.
                f.NextPass();
                calmPass1 = f.pass;
                f.Seed(Slot(target), 0f, -1, 0f);
                Run(f, float.PositiveInfinity, false, false, default);
                for (var i = 0; i < f.count; i++)
                    if (f.walk[f.order[i]] > cap) { CapBuilds++; break; }
            }
            // Pass 2, the frontier rule, over every built cell the target pass never reaches (joined to the target only
            // through unbuilt chunks, glass or a locked door; F1). The target pass has no cap (S1, MAP_ROUTE_SPEC v1.0.3):
            // a cell past 150 m that pass 1 reached keeps its route, so the arrows never flip as the player crosses the
            // cap. All of them while the target is not built. It runs whenever one of them has a walkable edge out of the
            // built map; the cap still bounds the frontier pass's own reach.
            f.NextPass();
            var seeds = 0;
            for (var s = 0; s < Slots; s++)
            {
                if (!f.Built(s) || (calmPass1 >= 0 && f.settled[s] == calmPass1)) continue;
                f.eligible[s] = f.pass;
                var c = f.CellAt(s);
                var h = float.PositiveInfinity;
                var hk = -1;
                for (var k = 0; k < 4; k++)
                {
                    var hu = FrontierRemainder(f, c, new GridCoord(c.x + DirX[k], c.y + DirY[k]));
                    if (hu < h - Tie) { h = hu; hk = k; }
                }
                if (hk < 0) continue;
                f.Seed(s, h, hk, 0f);
                seeds++;
            }
            if (seeds > 0)
            {
                calmPass2 = f.pass;
                Run(f, cap, true, false, default);
                FrontierBuilds++;
            }
        }

        // Regions first (the dirs compare neighbours), then the dirs.
        for (var s = 0; s < Slots; s++)
        {
            byte r = RegionNone;
            if (calmPass2 >= 0 && f.settled[s] == calmPass2) r = RegionFrontier;
            else if (calmPass1 >= 0 && f.settled[s] == calmPass1) r = RegionTarget;
            f.region[s] = r;
        }
        for (var s = 0; s < Slots; s++)
        {
            var r = f.region[s];
            int d;
            if (r == RegionNone) d = -1;
            else
            {
                var c = f.CellAt(s);
                if (r == RegionTarget && c == target) d = targetIsStation ? stationDir : 1;
                else d = BestDir(f, s, c, r, false, default);
                if (d >= 0)
                {
                    Remember(f, s, c, d);
                    routed++;
                    if (r == RegionFrontier) frontier++;
                }
            }
            f.dir[s] = (sbyte)d;
        }
        RoutedCells = routed;
        FrontierCells = frontier;
    }

    // The straight remainder from a frontier cell's neighbour u to the target, when u is an unbuilt, unreserved cell
    // behind a walkable edge (not a wall, not glass, not a door locked to the player); +inf otherwise. Read from the
    // seed's border rule, without generating u's chunk.
    float FrontierRemainder(Field f, GridCoord c, GridCoord u)
    {
        var built = f.InWindow(u) ? f.Built(Slot(u)) : map.IsBuilt(u);
        if (built || map.IsReserved(u)) return float.PositiveInfinity;
        var edge = map.FrontierEdge(c, u);
        if (edge == EdgeKind.Wall || edge == EdgeKind.Window) return float.PositiveInfinity;
        return Straight(u, target);
    }

    // The Dijkstra over the window's built cells, from the seeds already reached: the player walks from the cell
    // being relaxed into the settled one, so the crossing (and its key) is read from that side. A restricted pass
    // (onlyEligible) keeps to its own cells, a diagonal's two halves included (F2: else a corner reached diagonally
    // could have no lower neighbour of its own pass, and its dir would point uphill into a two-cell loop). The pressure
    // field (press) takes orthogonal steps only, each checked against the Relay's distance (RELAY 9.6).
    void Run(Field f, float reach, bool onlyEligible, bool press, GridCoord relayCell)
    {
        while (!f.Empty)
        {
            var s = f.Pop(out var d);
            if (f.settled[s] == f.pass || d > f.dist[s]) continue;
            f.settled[s] = f.pass;
            f.order[f.count++] = s;
            var c = f.CellAt(s);
            var enter = press ? NearCost(c, relayCell) : 0f;
            var plain = 0;
            for (var k = 0; k < 4; k++)
            {
                var n = new GridCoord(c.x + DirX[k], c.y + DirY[k]);
                if (!f.InWindow(n)) continue;
                var ns = Slot(n);
                if (!f.Built(ns)) continue;
                var ci = ns * 4 + ((k + 2) & 3);
                float cost;
                EdgeKind edge;
                if (f.crossBuild[ci] == f.build)
                {
                    // The frontier pass reads the crossing the target pass made (one Crossing per directed edge per build).
                    cost = f.cross[ci];
                    edge = (EdgeKind)f.edgeKind[ci];
                }
                else
                {
                    cost = map.Crossing(n, c, out edge);
                    f.cross[ci] = cost;
                    f.crossBuild[ci] = f.build;
                    f.edgeKind[ci] = (byte)edge;
                    f.edgeBuild[ci] = f.build;
                    f.edgeKind[s * 4 + k] = (byte)edge;
                    f.edgeBuild[s * 4 + k] = f.build;
                }
                if (edge == EdgeKind.Open) plain |= 1 << k;
                if (float.IsPositiveInfinity(cost) || f.settled[ns] == f.pass) continue;
                if (onlyEligible && f.eligible[ns] != f.pass) continue;
                // The pressure route: never past a seed's door, no further from the Relay than its door, and from n on
                // never more than one cell nearer the Relay than n itself.
                if (press && (f.beyond[ns] == f.pass || !(f.relay[ns] <= f.limit[s]) || f.least[s] < f.relay[ns] - PressureSlackMetres)) continue;
                var step = FrontRoomsMapPathField.Orthogonal + cost + enter;
                var opening = cost >= FrontRoomsMapPathField.AjarOpening - Tie ? 1 : 0;
                if (f.Reach(ns, d + step, f.walk[s] + step, f.metres[s] + FrontRoomsMapPathField.Orthogonal, f.openings[s] + opening, reach, f.limit[s]) && press)
                    f.least[ns] = Mathf.Min(f.relay[ns], f.least[s]);
            }
            if (press) continue;
            // A diagonal only when both orthogonal sides are plain open room edges (FrontRoomsMapPathField's rule).
            for (var k = 0; k < 4; k++)
            {
                var dx = k < 2 ? 1 : -1;
                var dy = (k & 1) == 0 ? 1 : -1;
                if ((plain & (1 << (dx > 0 ? 0 : 2))) == 0 || (plain & (1 << (dy > 0 ? 1 : 3))) == 0) continue;
                var corner = new GridCoord(c.x + dx, c.y + dy);
                if (!f.InWindow(corner)) continue;
                var cs = Slot(corner);
                if (!f.Built(cs) || f.settled[cs] == f.pass) continue;
                var halfX = new GridCoord(c.x + dx, c.y);
                var halfY = new GridCoord(c.x, c.y + dy);
                if (onlyEligible && (f.eligible[cs] != f.pass || f.eligible[Slot(halfX)] != f.pass || f.eligible[Slot(halfY)] != f.pass)) continue;
                if (EdgeAt(f, halfX, dy > 0 ? 1 : 3) != EdgeKind.Open || EdgeAt(f, halfY, dx > 0 ? 0 : 2) != EdgeKind.Open) continue;
                var step = FrontRoomsMapPathField.Diagonal;
                f.Reach(cs, d + step, f.walk[s] + step, f.metres[s] + step, f.openings[s], reach, f.limit[s]);
            }
        }
    }

    // The generated edge from a built window cell to its built neighbour k, read once per build (both passes and both
    // directions share it).
    EdgeKind EdgeAt(Field f, GridCoord a, int k)
    {
        var i = Slot(a) * 4 + k;
        if (f.edgeBuild[i] == f.build) return (EdgeKind)f.edgeKind[i];
        var n = new GridCoord(a.x + DirX[k], a.y + DirY[k]);
        var e = map.Edge(a, n);
        var j = Slot(n) * 4 + ((k + 2) & 3);
        f.edgeKind[i] = f.edgeKind[j] = (byte)e;
        f.edgeBuild[i] = f.edgeBuild[j] = f.build;
        return e;
    }

    // What stepping from c (slot s, region r) in dir k is worth: field(next) + step for a neighbour of the same
    // region, the remainder for a frontier seed stepping out of the built map; +inf when not allowed.
    float Value(Field f, int s, GridCoord c, int k, byte r, bool press, GridCoord relayCell)
    {
        var n = new GridCoord(c.x + DirX[k], c.y + DirY[k]);
        if (f.InWindow(n))
        {
            var ns = Slot(n);
            if (f.region[ns] == r)
            {
                var ci = s * 4 + k;
                if (f.crossBuild[ci] != f.build) return float.PositiveInfinity;
                var cost = f.cross[ci];
                if (float.IsPositiveInfinity(cost)) return float.PositiveInfinity;
                if (press && f.least[ns] < f.relay[s] - PressureSlackMetres) return float.PositiveInfinity;
                return f.dist[ns] + FrontRoomsMapPathField.Orthogonal + cost + (press ? NearCost(n, relayCell) : 0f);
            }
        }
        if (r == RegionFrontier && f.seeded[s] == calmPass2) return FrontierRemainder(f, c, n);
        return float.PositiveInfinity;
    }

    int BestDir(Field f, int s, GridCoord c, byte r, bool press, GridCoord relayCell)
    {
        var best = float.PositiveInfinity;
        var bestK = -1;
        for (var k = 0; k < 4; k++)
        {
            var v = Value(f, s, c, k, r, press, relayCell);
            stepValue[k] = v;
            if (v < best - Tie) { best = v; bestK = k; }
        }
        if (bestK < 0) return -1;
        // Hysteresis: the arrows never flip between equal routes.
        if (f.hasLast[s] && f.lastX[s] == c.x && f.lastY[s] == c.y)
        {
            var last = f.lastDir[s];
            if (last >= 0 && stepValue[last] <= best + Tie) return last;
        }
        return bestK;
    }

    static void Remember(Field f, int s, GridCoord c, int d)
    {
        f.hasLast[s] = true;
        f.lastX[s] = c.x;
        f.lastY[s] = c.y;
        f.lastDir[s] = (sbyte)d;
    }

    // ---------- The beacon (tutorial M5) ----------

    /// <summary>
    /// The station TV's beacon, read from the same field while the key is not taken (spec 5). pathMetres: the octile
    /// metres along the route the field found, with no door costs (S2 has those as shutOpenings), plus the straight
    /// remainder where the route leaves the built map (F7). shutOpenings: the ajar or shut doors on that same route
    /// (carried with the metres, so on a tie the two never describe two routes; review m4). portalPos: the first
    /// opening that leaves the player's room set along the dirs (the TV's cell centre when the route never leaves
    /// it). False when the target is not the station (no station, or the key taken: the beacon is off) or the
    /// player's cell has no route.
    /// </summary>
    public bool BeaconPath(GridCoord playerCell, out float pathMetres, out int shutOpenings, out Vector3 portalPos)
    {
        pathMetres = float.PositiveInfinity;
        shutOpenings = 0;
        portalPos = default;
        if (!TargetIsStation || !calm.InWindow(playerCell) || calm.region[Slot(playerCell)] == RegionNone) return false;
        var ps = Slot(playerCell);
        RoomSet(map, playerCell, beaconRoom);
        var c = playerCell;
        var portal = false;
        for (var guard = 0; guard < Slots; guard++)
        {
            if (c == target)
            {
                if (!portal) portalPos = map.CellCentre(c);
                pathMetres = calm.metres[ps];
                shutOpenings = calm.openings[ps];
                return true;
            }
            var s = Slot(c);
            var k = calm.dir[s];
            if (k < 0) return false;
            var n = new GridCoord(c.x + DirX[k], c.y + DirY[k]);
            if (!portal && beaconRoom.Contains(c) && !beaconRoom.Contains(n))
            {
                portalPos = map.CrossingPoint(c, n);
                portal = true;
            }
            if (!calm.InWindow(n) || calm.region[Slot(n)] == RegionNone)
            {
                // Out of the built map from a frontier seed: the field's metres already hold the straight remainder.
                pathMetres = calm.metres[ps];
                shutOpenings = calm.openings[ps];
                return !float.IsPositiveInfinity(pathMetres);
            }
            c = n;
        }
        return false;
    }

    // ---------- The pressure route (chase only; RELAY 9.6, LD R6) ----------

    /// <summary>The last pressure build is current (built after the last calm build).</summary>
    public bool PressureValid => pressureValid;
    public GridCoord PressureRelay => pressureRelay;
    /// <summary>
    /// The last pressure build reached the player's cell: a door qualifies (RELAY 9.6). False: no shuttable door can be
    /// reached without passing the Relay, so the rows do not step (the room set keeps its calm turn; the wave still runs).
    /// </summary>
    public bool PressureReachesPlayer => pressureValid && pressureReachesPlayer;

    /// <summary>
    /// The chase's pressure route: the doors the player can shut that lead away from the Relay (RELAY 9.6, LD R6, and
    /// 系统设计's ruling of 2026-10-22). It reads the Relay, which a chase allows (the player has seen it).
    /// - The cells it may use are those the player walks to without the Relay's path distance (RelayDistance) ever
    ///   falling more than one cell (PressureSlackMetres) below the player's own: a door that can only be reached by
    ///   passing the Relay is never a target (the +6 m near-Relay price alone allowed that).
    /// - A door counts when it is whole (open, ajar or shut; never broken, never the EXIT), not locked to the player,
    ///   its near cell among those cells, at least 2 cells from the Relay and within its reach, its far cell further
    ///   from the Relay by path and not the player's own (a door already passed). Each such door's near cell is a seed
    ///   whose dir points through it.
    /// - The field spreads from the seeds over the calm edge rules (never glass), orthogonal steps only, +6 m for every
    ///   cell entered within 2 cells of the Relay, up to 45 m (all SP), only into cells no further from the Relay than
    ///   the door's near cell, never into a seed door's far cell (beyond a door the calm route leads on; so the dirs
    ///   hold no loop: every other dir points at a cell settled before it), and only where, along the route ahead,
    ///   the Relay's distance never falls more than one cell below the cell's own (checked again along the dirs chosen).
    /// - Cells it does not reach keep the calm route. A calm cell may step during the chase only when its calm route
    ///   keeps the same rule (StepAllowed), and no row steps at all while no door qualifies (PressureReachesPlayer).
    /// </summary>
    public bool BuildPressure(GridCoord relayCell, GridCoord playerCell)
    {
        pressureValid = false;
        pressureReachesPlayer = false;
        if (RelayDistance == null) return false;
        watch.Restart();
        var f = pressure;
        f.Begin(calm.min);
        MarkBuilt(f);
        f.NextPass();
        pressureRelay = relayCell;
        for (var s = 0; s < Slots; s++) f.relay[s] = f.Built(s) ? RelayDistance(f.CellAt(s)) : float.PositiveInfinity;
        var seeds = 0;
        if (f.InWindow(playerCell) && f.Built(Slot(playerCell)))
        {
            // The cells the player reaches without coming more than one cell nearer the Relay (breadth-first).
            var ps = Slot(playerCell);
            var floor = f.relay[ps] - PressureSlackMetres;
            int head = 0, tail = 0;
            f.eligible[ps] = f.pass;
            f.queue[tail++] = ps;
            while (head < tail)
            {
                var s = f.queue[head++];
                var c = f.CellAt(s);
                for (var k = 0; k < 4; k++)
                {
                    var n = new GridCoord(c.x + DirX[k], c.y + DirY[k]);
                    if (!f.InWindow(n)) continue;
                    var ns = Slot(n);
                    if (!f.Built(ns) || f.eligible[ns] == f.pass || !(f.relay[ns] >= floor)) continue;
                    if (float.IsPositiveInfinity(map.Crossing(c, n, out _))) continue;
                    f.eligible[ns] = f.pass;
                    f.queue[tail++] = ns;
                }
            }
            // The doors among them that lead away from the Relay.
            for (var i = 0; i < tail; i++)
            {
                var s = f.queue[i];
                var a = f.CellAt(s);
                var da = f.relay[s];
                if (float.IsPositiveInfinity(da) || Mathf.Max(Mathf.Abs(a.x - relayCell.x), Mathf.Abs(a.y - relayCell.y)) < PressureNearCells) continue;
                for (var k = 0; k < 4; k++)
                {
                    var b = new GridCoord(a.x + DirX[k], a.y + DirY[k]);
                    if (!f.InWindow(b)) continue;
                    var bs = Slot(b);
                    // A door into the player's own cell is one already passed: behind them, not a way on.
                    if (bs == ps || !f.Built(bs) || map.Edge(a, b) != EdgeKind.Door || !map.Shuttable(a, b)) continue;
                    if (float.IsPositiveInfinity(map.Crossing(a, b, out _)) || !(f.relay[bs] > da + Tie)) continue;
                    f.Seed(s, 0f, k, da);
                    f.least[s] = da;
                    // The route ends at the door: its far cell never joins the route, so no arrow there points back
                    // through it at the seed, whose own arrow points through it (a two-cell loop at an open door).
                    f.beyond[bs] = f.pass;
                    seeds++;
                    break;
                }
            }
        }
        if (seeds > 0) Run(f, PressureCapMetres, true, true, relayCell);
        for (var s = 0; s < Slots; s++)
        {
            f.region[s] = seeds > 0 && f.settled[s] == f.pass ? RegionPressure : RegionNone;
            f.dir[s] = -1;
        }
        // The dirs in settled order (each points at a cell settled before it), each checked again along the dirs
        // themselves: a cell whose way on would bring the Relay more than a cell nearer is left to the calm route.
        for (var i = 0; i < f.count; i++)
        {
            var s = f.order[i];
            if (f.region[s] != RegionPressure) continue;
            var c = f.CellAt(s);
            if (f.seeded[s] == f.pass && f.dist[s] <= 0f)
            {
                f.dir[s] = f.seedDir[s];
                f.least[s] = f.relay[s];
                Remember(f, s, c, f.dir[s]);
                continue;
            }
            var d = BestDir(f, s, c, RegionPressure, true, relayCell);
            var ns = d >= 0 ? Slot(new GridCoord(c.x + DirX[d], c.y + DirY[d])) : -1;
            if (ns < 0 || f.dir[ns] < 0 || f.least[ns] < f.relay[s] - PressureSlackMetres)
            {
                f.region[s] = RegionNone;
                continue;
            }
            f.dir[s] = (sbyte)d;
            f.least[s] = Mathf.Min(f.relay[s], f.least[ns]);
            Remember(f, s, c, d);
        }
        pressureReachesPlayer = f.InWindow(playerCell) && f.dir[Slot(playerCell)] >= 0;
        CalmSteps(f);
        pressureValid = true;
        PressureBuilds++;
        var ms = (float)watch.Elapsed.TotalMilliseconds;
        LastPressureMs = ms;
        if (ms > MaxPressureMs) MaxPressureMs = ms;
        return true;
    }

    // Whether a row may step on the calm route during the chase: along the calm dirs from the cell the Relay's
    // distance never falls more than one cell below the cell's own (the calm field's settled order puts each cell's
    // way on before it; a cell past the cap that the frontier pass took again is listed twice, the second time right).
    void CalmSteps(Field p)
    {
        var f = calm;
        for (var s = 0; s < Slots; s++) calmStep[s] = false;
        if (!pressureReachesPlayer) return;
        for (var i = 0; i < f.count; i++)
        {
            var s = f.order[i];
            if (f.region[s] == RegionNone) continue;
            var least = p.relay[s];
            var d = f.dir[s];
            if (d >= 0)
            {
                var c = f.CellAt(s);
                var n = new GridCoord(c.x + DirX[d], c.y + DirY[d]);
                if (f.InWindow(n) && f.region[Slot(n)] != RegionNone) least = Mathf.Min(least, calmLeast[Slot(n)]);
            }
            calmLeast[s] = least;
            calmStep[s] = least >= p.relay[s] - PressureSlackMetres;
        }
    }

    /// <summary>
    /// The pressure field's own dir for a cell, while PressureValid; false where it does not reach. Raw: a cell can
    /// hold a dir here while no door qualifies for the player (a door seeded off the room set). What a cell turns to
    /// in a chase is ChaseRoute.
    /// </summary>
    public bool PressureRoute(GridCoord cell, out int dir)
    {
        dir = -1;
        if (!pressureValid || !pressure.InWindow(cell)) return false;
        dir = pressure.dir[Slot(cell)];
        return dir >= 0;
    }

    /// <summary>
    /// The dir a cell turns to in a chase (RELAY 9.6): the pressure route's where it reaches the cell, but only while a
    /// door qualifies (PressureReachesPlayer); else the calm route. With no door qualifying, the room set keeps its
    /// calm turn even where a door off the player's way seeded a pressure dir (review M1).
    /// </summary>
    public bool ChaseRoute(GridCoord cell, out int dir)
    {
        if (PressureReachesPlayer && PressureRoute(cell, out dir)) return true;
        return Route(cell, out dir);
    }

    /// <summary>
    /// RELAY 9.6: a row turned to <paramref name="dir"/> on this cell may step during the chase. Only while a door
    /// qualifies (PressureReachesPlayer), and only on the pressure route or on a calm route that never leads toward the
    /// Relay: a stepping row never urges the player toward it.
    /// </summary>
    public bool StepAllowed(GridCoord cell, int dir)
    {
        if (!PressureReachesPlayer || dir < 0 || !pressure.InWindow(cell)) return false;
        var s = Slot(cell);
        if (pressure.dir[s] == dir) return true;
        return calm.InWindow(cell) && calm.dir[s] == dir && calmStep[s];
    }

    static float NearCost(GridCoord c, GridCoord relayCell) =>
        Mathf.Max(Mathf.Abs(c.x - relayCell.x), Mathf.Abs(c.y - relayCell.y)) <= PressureNearCells ? PressureNearMetres : 0f;

    // ---------- The room set (RELAY 9.2), shared by the warning lamps and the cue ----------

    struct RoomStep
    {
        public GridCoord cell;
        public int depth;
    }

    // The live map as the room set reads it (a struct, so the shared function takes it without allocating).
    struct WorldCells : IRoomCells
    {
        readonly FrontRoomsMapWorld world;
        public WorldCells(FrontRoomsMapWorld world) => this.world = world;
        public bool IsBuilt(GridCoord cell) => world.IsBuilt(cell);
        public EdgeKind Edge(GridCoord a, GridCoord b) => world.Cache.Edge(a, b);
        public bool TryGetChunk(GridCoord chunk, out MapChunk data) => world.Cache.TryGetGenerated(chunk, out data);
    }

    static readonly Queue<RoomStep> roomWalk = new Queue<RoomStep>();
    static readonly HashSet<GridCoord> roomSeen = new HashSet<GridCoord>();

    /// <summary>
    /// "Your room" (RELAY_PURSUIT_REDESIGN.md 9.2; CUE_DRIVER_INTERFACE.md 5 item 6): the carved or module room the
    /// cell stands in (its built cells, row by row), else a breadth-first walk over Open edges from it (depth 2, at most
    /// 9 cells; arches, doors and glass are walls). Never the start area or X (they are not built). The one function
    /// the warning lamps' bursts (FrontRoomsWarnLamps) and the wallpaper cue's turns read, so lamps and walls agree.
    /// Main thread only (shared scratch); allocation-free once warm.
    /// </summary>
    public static void RoomSet(FrontRoomsMapWorld world, GridCoord p, List<GridCoord> into)
    {
        if (world == null || world.Cache == null) { into.Clear(); return; }
        RoomSet(new WorldCells(world), p, into);
    }

    /// <summary>The room set over any map the fields read (the same function: tests and the beacon).</summary>
    public static void RoomSet<T>(T cells, GridCoord p, List<GridCoord> into) where T : IRoomCells
    {
        into.Clear();
        if (!cells.IsBuilt(p)) return;
        var chunk = MapGrid.ChunkOf(p);
        if (cells.TryGetChunk(chunk, out var data))
        {
            var o = MapGrid.ChunkOrigin(chunk);
            for (var r = data.rooms.Length - 1; r >= 0; r--)
            {
                var rect = data.rooms[r];
                if (!rect.Contains(p.x - o.x, p.y - o.y)) continue;
                for (var y = rect.y; y < rect.y + rect.h; y++)
                for (var x = rect.x; x < rect.x + rect.w; x++)
                {
                    var c = new GridCoord(o.x + x, o.y + y);
                    if (cells.IsBuilt(c)) into.Add(c);
                }
                return;
            }
        }
        roomWalk.Clear();
        roomSeen.Clear();
        roomWalk.Enqueue(new RoomStep { cell = p, depth = 0 });
        roomSeen.Add(p);
        while (roomWalk.Count > 0 && into.Count < RoomCells)
        {
            var step = roomWalk.Dequeue();
            var cell = step.cell;
            into.Add(cell);
            if (step.depth >= RoomDepth) continue;
            for (var k = 0; k < 4; k++)
            {
                // The warning lamps' order: +x, -x, +z, -z.
                var n = k == 0 ? new GridCoord(cell.x + 1, cell.y) : k == 1 ? new GridCoord(cell.x - 1, cell.y)
                    : k == 2 ? new GridCoord(cell.x, cell.y + 1) : new GridCoord(cell.x, cell.y - 1);
                if (roomSeen.Contains(n) || !cells.IsBuilt(n)) continue;
                if (cells.Edge(cell, n) != EdgeKind.Open) continue;
                roomSeen.Add(n);
                roomWalk.Enqueue(new RoomStep { cell = n, depth = step.depth + 1 });
            }
        }
    }
}
