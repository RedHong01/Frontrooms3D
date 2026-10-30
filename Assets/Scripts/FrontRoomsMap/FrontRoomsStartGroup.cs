using System;
using System.Collections.Generic;

namespace FrontRooms.Map
{
    /// <summary>
    /// The floor-1 start group's shape (A26, FD-b; notes/floor1_start_group_plan.md 4.1): a station W x H cells with the
    /// start door D in its south row (column 2 from the west), E1 (an arch) in its north wall on D's axis, E2 (an arch)
    /// in its west wall, and a straight corridor of k cells north of E1 whose last cell N fronts the EXIT X = N + (0, 1).
    /// Never rotated or mirrored. Step 0 builds it in code (StandIn) until the station module asset exists; step 2 reads
    /// it from the module (StartGroupTemplate.From).
    /// </summary>
    [Serializable]
    public sealed class StartGroupTemplate
    {
        /// <summary>The station is always 4 cells wide (12 m).</summary>
        public const int Width = 4;
        /// <summary>k for a station module (Red 2026-10-29, plan 10: the EXIT 4 cells straight north of E1, 12 m).</summary>
        public const int CorridorCells = 4;
        /// <summary>H: 4 (12 x 12 m) or 3 (12 x 9 m).</summary>
        public int depth = 4;
        /// <summary>k: corridor cells C1..Ck north of E1, Ck = N. H + k must be 7 or 8 (X chunk-local 1..6 on both parities, 4.1).</summary>
        public int corridor = 4;
        /// <summary>The start door's column in the station (0-based from the west): D's west neighbour stays inside (G1, 4.8).</summary>
        public int entrance = 2;
        /// <summary>E2's row in the west wall (0-based from the south; FD-b and 9: row 1).</summary>
        public int e2Row = 1;
        /// <summary>The authored arch spans, centred on their edges (E1 1.6-1.8 m, E2 1.1-1.4 m; 4.1).</summary>
        public float e1Width = 1.7f, e2Width = 1.2f;
        public const float E1MinWidth = 1.6f, E1MaxWidth = 1.8f, E2MinWidth = 1.1f, E2MaxWidth = 1.4f;
        /// <summary>The station's lamps, i + j * Width (j = 0 the south row), never Auto (4.6, 4.9); null: the stand-in table (StandInLamp).</summary>
        public ModuleLamp[] lamps;
        /// <summary>The station's inner edges as the module authors them (RoomModuleData's innerEast / innerNorth order); null: all Open (the stand-in).</summary>
        public ModuleEdge[] innerEast, innerNorth;

        /// <summary>The lamp of station cell (i, j), written into the map by the carve (FrontRoomsMapCarve).</summary>
        public ModuleLamp LampAt(int i, int j) => lamps != null ? lamps[i + j * Width] : StandInLamp(i, j);

        /// <summary>The station's inner edge between cell (i, j) and (i + 1, j), as the carve forces it.</summary>
        public EdgeKind InnerEast(int i, int j) => innerEast != null ? RoomModuleData.ToEdge(innerEast[i + j * (Width - 1)]) : EdgeKind.Open;

        /// <summary>The station's inner edge between cell (i, j) and (i, j + 1), as the carve forces it.</summary>
        public EdgeKind InnerNorth(int i, int j) => innerNorth != null ? RoomModuleData.ToEdge(innerNorth[i + j * Width]) : EdgeKind.Open;

        /// <summary>
        /// The stand-in lamp table (4.6, 61 4.4) until the station module asset gives its own: the two cells beside D
        /// Steady, the placard cell (north row, column 1: x 5.5 m) Steady, the TV cell (north row, column 3: x 9.35 m, also
        /// the riser block's cell, 9) and the axis cell under E1 Off, every other cell Dim.
        /// </summary>
        public ModuleLamp StandInLamp(int i, int j)
        {
            if (j == 0 && (i == entrance - 1 || i == entrance + 1)) return ModuleLamp.Steady;
            if (j == depth - 1 && i == 1) return ModuleLamp.Steady;
            if (j == depth - 1 && (i == Width - 1 || i == entrance)) return ModuleLamp.Off;
            return ModuleLamp.Dim;
        }

        /// <summary>The stand-in station of step 0 (5.1): inner edges all Open, E1 on the door axis, E2 west at <paramref name="row"/>.</summary>
        public static StartGroupTemplate StandIn(int depth, int row, int corridor) =>
            new StartGroupTemplate { depth = depth, e2Row = row, corridor = corridor };

        /// <summary>The shape rules the planner refuses a template for (4.3).</summary>
        public bool Valid(out string why)
        {
            why = null;
            if (depth < 3 || depth > 4) why = "depth " + depth + " is not 3 or 4";
            else if (corridor < 3) why = "corridor " + corridor + " is under 3 cells";
            else if (depth + corridor != 7 && depth + corridor != 8) why = "H + k = " + (depth + corridor) + ": X would leave chunk-local 1..6 on one parity";
            else if (entrance != 2) why = "the start door must be column 2";
            else if (e2Row < 1 || e2Row >= depth) why = "E2 row " + e2Row + " is off the west wall's rows 1.." + (depth - 1);
            else if (e1Width < E1MinWidth || e1Width > E1MaxWidth || e2Width < E2MinWidth || e2Width > E2MaxWidth)
                why = "the arch spans " + e1Width + " / " + e2Width + " m are off E1's " + E1MinWidth + "-" + E1MaxWidth + " or E2's " + E2MinWidth + "-" + E2MaxWidth + " m";
            else if (lamps != null && lamps.Length != Width * depth) why = "the lamp table has " + lamps.Length + " cells, not " + Width * depth;
            else if (lamps != null && Array.IndexOf(lamps, ModuleLamp.Auto) >= 0) why = "a station lamp is Auto (4.6: never)";
            else if (innerEast != null && innerEast.Length != (Width - 1) * depth) why = "the inner east edges are " + innerEast.Length + ", not " + (Width - 1) * depth;
            else if (innerNorth != null && innerNorth.Length != Width * (depth - 1)) why = "the inner north edges are " + innerNorth.Length + ", not " + Width * (depth - 1);
            else if (InnerEast(entrance - 1, 0) != EdgeKind.Open) why = "the start door's west inner edge is not Open (G1)";
            else
                for (var j = 0; j < depth - 1 && why == null; j++)
                    if (InnerNorth(entrance, j) != EdgeKind.Open) why = "the entrance column is not Open north of row " + j + " (the walk D to E1)";
            return why == null;
        }

        public string Name => "H" + depth + " E2r" + e2Row + " k" + corridor;

        /// <summary>
        /// The template of a station module (4.3, 4.9): the module checked by RoomModuleData.Validate, the station contract
        /// included (ValidateStation; <paramref name="footprint"/> as Validate takes it, null when the kit is not known),
        /// then read: H its depth, k = CorridorCells, the start door's column from its StationEntrance marker, E2's row from
        /// its west arch, its lamps and inner edges. E1 and E2 keep the stand-in spans (1.7 and 1.2 m, inside 4.1's 1.6-1.8 and
        /// 1.1-1.4): a module has no per-opening span yet. Null, with the first reason, for a module the planner refuses.
        /// </summary>
        public static StartGroupTemplate From(RoomModuleData module, Func<string, float[]> footprint, out string why)
        {
            why = null;
            if (module == null) { why = "no station module"; return null; }
            var m = module.Clone();
            m.Normalize();
            if (!m.IsStation) { why = "the module has no StationEntrance marker, so it is not a station"; return null; }
            var errors = new List<string>();
            m.Validate(errors, new List<string>(), footprint);
            if (errors.Count > 0) { why = errors[0] + (errors.Count > 1 ? " (and " + (errors.Count - 1) + " more)" : ""); return null; }
            var t = new StartGroupTemplate { depth = m.depth, corridor = CorridorCells, lamps = (ModuleLamp[])m.lamps.Clone() };
            t.innerEast = (ModuleEdge[])m.innerEast.Clone();
            t.innerNorth = (ModuleEdge[])m.innerNorth.Clone();
            foreach (var mk in m.markers)
                if (mk.kind == ModuleMarkerKind.StationEntrance) t.entrance = (int)Math.Floor(mk.x / MapGrid.CellSize);
            for (var j = 0; j < m.depth; j++)
                if (m.west[j] == ModuleEdge.Arch) t.e2Row = j;
            return t.Valid(out why) ? t : null;
        }
    }

    /// <summary>The start group laid out at one start door, in map cells (the root at the map's origin).</summary>
    [Serializable]
    public struct StartGroupLayout
    {
        /// <summary>D, the cell the start door opens onto.</summary>
        public GridCoord door;
        /// <summary>The station's footprint: x D.x-2..D.x+1, y D.y..D.y+H-1.</summary>
        public CellRect station;
        /// <summary>E1: its inside cell (the station's north row on D's axis) and its outside cell C1.</summary>
        public GridCoord e1In, e1Out;
        /// <summary>E2: its inside cell (the station's west column) and its outside cell E2o.</summary>
        public GridCoord e2In, e2Out;
        /// <summary>k.</summary>
        public int corridor;
        /// <summary>N (= C_k) and X.</summary>
        public GridCoord front, exit;
        /// <summary>The start strip (stream rooms) behind the door: 2 * 2 + 1 columns, as many rows as asked (canonical R* = 20).</summary>
        public CellRect strip;

        public static StartGroupLayout For(StartGroupTemplate t, GridCoord door, int corridor, int stripRows)
        {
            var x0 = door.x - t.entrance;
            var top = door.y + t.depth - 1;
            return new StartGroupLayout
            {
                door = door,
                station = new CellRect(x0, door.y, StartGroupTemplate.Width, t.depth),
                e1In = new GridCoord(door.x, top),
                e1Out = new GridCoord(door.x, top + 1),
                e2In = new GridCoord(x0, door.y + t.e2Row),
                e2Out = new GridCoord(x0 - 1, door.y + t.e2Row),
                corridor = corridor,
                front = new GridCoord(door.x, top + corridor),
                exit = new GridCoord(door.x, top + corridor + 1),
                // The game's start area (V:397, G StartAreaHalfCells = 2 at G:230).
                strip = new CellRect(door.x - FrontRoomsStartGroup.StripHalfCells, door.y - stripRows, FrontRoomsStartGroup.StripHalfCells * 2 + 1, stripRows),
            };
        }

        /// <summary>C_i, i = 1..k (C_k is N).</summary>
        public GridCoord Corridor(int i) => new GridCoord(door.x, station.y + station.h - 1 + i);
        public bool InStation(GridCoord cell) => FrontRoomsExitPlanner.InArea(station, cell);
        public bool InStrip(GridCoord cell) => FrontRoomsExitPlanner.InArea(strip, cell);
        public bool InCorridor(GridCoord cell) => cell.x == door.x && cell.y > station.y + station.h - 1 && cell.y <= front.y;
        /// <summary>The cells the carve protects: the station, C1..N and X (4.2).</summary>
        public bool Protected(GridCoord cell) => InStation(cell) || InCorridor(cell) || cell == exit;
        /// <summary>c, the start door's chunk.</summary>
        public GridCoord DoorChunk => MapGrid.ChunkOf(door);
        /// <summary>c', X's chunk (c + (0, 1) for every valid template, 4.1).</summary>
        public GridCoord ExitChunk => MapGrid.ChunkOf(exit);
    }

    /// <summary>A zone key the carve moves off the station or X (seed-only: the PlaceKey search, FM:649-670, minus those cells).</summary>
    [Serializable]
    public struct StartGroupKeyMove
    {
        public GridCoord zone, from, site, to;
    }

    /// <summary>
    /// The carve as a virtual overlay on a cache (A26 step 0, 5.1): forced edges keyed by edge id, read before the cache's
    /// own edges, plus the room drops and re-placed columns (on clones of c and c') and the key moves. Never written into
    /// the cache. Step 1 turns the same data into FrontRoomsMapCarve, applied last in FrontRoomsMapCache.Get (4.2).
    /// </summary>
    public sealed class StartGroupCarve
    {
        /// <summary>Why an edge is forced: the template or the glass rule round N (both protected, 4.2), or a repair phase.</summary>
        public enum Source : byte { Template, SightlineGlass, RepairA, RepairB, RepairC }

        public struct Forced
        {
            public GridCoord a, b;
            public EdgeKind kind;
            public Source source;
        }

        /// <summary>Forced edges by edge id (FrontRoomsStartGroup.EdgeId, as MW:2462-2467).</summary>
        public readonly Dictionary<long, Forced> forced = new Dictionary<long, Forced>();
        /// <summary>The chunks the carve never lets shift (c, c').</summary>
        public readonly List<GridCoord> pinned = new List<GridCoord>();
        /// <summary>Clones of c and c' with the dropped rooms gone, columns placed again and key spots re-read; empty in a SEARCH overlay.</summary>
        public readonly Dictionary<GridCoord, MapChunk> chunks = new Dictionary<GridCoord, MapChunk>();
        public readonly List<StartGroupKeyMove> keyMoves = new List<StartGroupKeyMove>();
        /// <summary>Dropped room rects (chunk-local), with their chunk, in order.</summary>
        public readonly List<GridCoord> droppedChunk = new List<GridCoord>();
        public readonly List<CellRect> droppedRoom = new List<CellRect>();

        public int roomDrops, roomDropsOutside, columnsChanged, keySpotResets;
        /// <summary>Repair openings per phase; window joins (glass the only way a piece with a non-Tall cell joins); new windows into Tall-only pieces (their own kind, FM:653); pieces nothing could join.</summary>
        public int repairA, repairB, repairC, windowJoins, tallWindowJoins, repairFailures;
        /// <summary>Window joins whose piece led on (LeadsOnCells, glass blocking, the strip reserved) in the raw map: the carve cut it off. The rest were glass pockets before the carve.</summary>
        public int windowJoinsCarve;
        /// <summary>A cell of each window-joined piece (diagnostics).</summary>
        public readonly List<GridCoord> windowJoinCells = new List<GridCoord>();
        /// <summary>Forced edges on a chunk border, and those whose kind differs from Generator.BorderEdge (the overrides step 1's BorderOverride must carry).</summary>
        public int borderEdges, borderOverrides;
        /// <summary>Glass edges round N, N-1, N-2 forced to Wall (EP:372-389).</summary>
        public int glassWalled;

        public int Repairs => repairA + repairB + repairC;
        /// <summary>5.3's outside edits: repair openings + room drops reaching outside the template + key moves.</summary>
        public int OutsideEdits => Repairs + roomDropsOutside + keyMoves.Count;

        public bool IsProtected(GridCoord a, GridCoord b) =>
            forced.TryGetValue(FrontRoomsStartGroup.EdgeId(a, b), out var f) && (f.source == Source.Template || f.source == Source.SightlineGlass);

        public bool TryForced(GridCoord a, GridCoord b, out EdgeKind kind)
        {
            if (forced.TryGetValue(FrontRoomsStartGroup.EdgeId(a, b), out var f)) { kind = f.kind; return true; }
            kind = EdgeKind.Wall;
            return false;
        }

        /// <summary>The edge between two side-by-side cells: forced first, else the cache's (FM:796-805; generates as it must).</summary>
        public EdgeKind Edge(FrontRoomsMapCache cache, GridCoord a, GridCoord b) => TryForced(a, b, out var kind) ? kind : cache.Edge(a, b);

        /// <summary>A chunk-border edge as step 1's Generator.BorderEdge (FM:349) will return it: the override first, then the seed. Never generates a chunk.</summary>
        public EdgeKind BorderEdge(FrontRoomsMapGenerator generator, GridCoord cell, bool east)
        {
            var other = east ? new GridCoord(cell.x + 1, cell.y) : new GridCoord(cell.x, cell.y + 1);
            return TryForced(cell, other, out var kind) ? kind : generator.BorderEdge(cell, east);
        }

        /// <summary>A chunk as the carve leaves it: the clone for c and c', else the cache's.</summary>
        public MapChunk ChunkFor(FrontRoomsMapCache cache, GridCoord coord) => chunks.TryGetValue(coord, out var chunk) ? chunk : cache.Get(coord);

        public bool IsPinned(GridCoord chunk) => pinned.Contains(chunk);

        public void Force(GridCoord a, GridCoord b, EdgeKind kind, Source source)
        {
            if (b.x < a.x || b.y < a.y) { var t = a; a = b; b = t; }
            forced[FrontRoomsStartGroup.EdgeId(a, b)] = new Forced { a = a, b = b, kind = kind, source = source };
        }

        /// <summary>The key cell and site cell of a zone after the carve (key moves, then the clones' re-read key spots); false for a zone without a key.</summary>
        public bool KeyOf(FrontRoomsMapCache cache, GridCoord zone, out GridCoord keyCell, out GridCoord siteCell)
        {
            foreach (var m in keyMoves)
                if (m.zone == zone) { keyCell = m.to; siteCell = m.to; return true; }
            var home = ChunkFor(cache, zone);
            keyCell = home.keyCell;
            siteCell = home.keySiteCell;
            return home.hasKey;
        }

        /// <summary>A 64-bit FNV-1a hash of everything the carve decides, with the root, D and X: equal plans, equal hashes.</summary>
        public string Hash(GridCoord root, GridCoord door, GridCoord exit)
        {
            var h = 14695981039346656037UL;
            void Add(long v)
            {
                unchecked
                {
                    for (var i = 0; i < 8; i++)
                    {
                        h ^= (byte)(v >> (8 * i));
                        h *= 1099511628211UL;
                    }
                }
            }
            Add(root.x); Add(root.y); Add(door.x); Add(door.y); Add(exit.x); Add(exit.y);
            var ids = new List<long>(forced.Keys);
            ids.Sort();
            foreach (var id in ids) { Add(id); Add((long)forced[id].kind); }
            foreach (var m in keyMoves) { Add(m.zone.x); Add(m.zone.y); Add(m.to.x); Add(m.to.y); }
            for (var i = 0; i < droppedRoom.Count; i++)
            {
                Add(droppedChunk[i].x); Add(droppedChunk[i].y);
                var r = droppedRoom[i];
                Add(r.x); Add(r.y); Add(r.w); Add(r.h);
            }
            return h.ToString("x16");
        }
    }

    /// <summary>Why a SEARCH candidate (a root and a k) was turned down, in the order the tests run (5.3).</summary>
    public enum StartGroupReject { ZoneCorridor, LineWall, LineArchRay, BladeSign, XRoom, XKey, Sightline, ChunkWhole, Count }

    /// <summary>One CARVE plan as the game would make it at Space (A26 step 0, virtual; 4.3 Plan).</summary>
    public sealed class StartGroupPlan
    {
        /// <summary>A zone root was found (false: ZoneNoRoot).</summary>
        public bool found;
        /// <summary>The accepted root (linear index in ring, b, a order), its ring, the zone roots tried, and leads-on retries.</summary>
        public int rootIndex = -1, ring, zoneRoots, leadsOnRetries;
        public GridCoord root;
        public StartGroupLayout layout;
        public StartGroupCarve carve;
        public bool leadsOnE2, leadsOnC1;
        /// <summary>The C22 rules on the carved data (FromStartGroup's asserts, 4.3) and G1.</summary>
        public bool exitFits, xRoom, xKey, chunkWhole, deadEnd, corridor, frontStandard, g1;
        public FrontRoomsExitPlanner.SightlineView sightline;
        /// <summary>Walking steps D into X (ExitPlan.distance = WalkStepsTo + 1, EP:501): must be H + k.</summary>
        public int distance;
        public bool Asserts => exitFits && chunkWhole && frontStandard && sightline == FrontRoomsExitPlanner.SightlineView.Clear && !corridor;

        /// <summary>
        /// FrontRoomsStartGroup.Plan only (4.3, 4.11): the carve it set on the cache (null: no plan, so today's start), the
        /// EXIT the carve makes (FrontRoomsExitPlanner.FromStartGroup), why there is no plan (null when there is one), and
        /// why it was accepted with a warning (null when it was not).
        /// </summary>
        public FrontRoomsMapCarve mapCarve;
        public ExitPlan exit;
        public string error, warning;
        /// <summary>Plan's own time in ms, from the call to the carve set and asserted (the START line's "carve ms", 4.5).</summary>
        public float planMs;
        /// <summary>The plan is made: its carve is on the cache and its EXIT found.</summary>
        public bool Ready => mapCarve != null && exit.found;
    }

    /// <summary>One SEARCH run (natural line, 5.3): the first root and k whose line, X and sign are already right.</summary>
    public sealed class StartGroupSearch
    {
        public bool found;
        public int rootIndex = -1, ring, zoneRoots, k;
        public GridCoord root;
        public StartGroupLayout layout;
        public StartGroupCarve overlay;
        public readonly int[] rejects = new int[(int)StartGroupReject.Count];
    }

    /// <summary>M7 first-send candidates per rung (4.10), before visibility and body fit.</summary>
    public struct StartGroupRungs
    {
        /// <summary>Rung 1 (8-10 cells, far side by the exit field), the same with the dot test instead, rung 2 (6-11 any side), rung 3 (14-20).</summary>
        public int rung1, rung1Dot, rung2, rung3;
    }

    /// <summary>
    /// A26 step 0 core (notes/floor1_start_group_plan.md 4.1-4.3, 4.10, 5): the zone-only root search, the CARVE plan and
    /// the SEARCH (natural line) evaluation, both on a virtual overlay over a FrontRoomsMapCache that they never change
    /// (the cache only generates chunks, as any reader does), and the checks the feasibility batch reports (leads-on, the
    /// E1-E2 loop, the sight-line ray, the M7 rungs). Plain C#, no UnityEngine, shared later by the game, the validator
    /// and the batch. Nothing in the game calls it yet. Every rule mirrored from the real map code cites its file:line.
    /// </summary>
    public static class FrontRoomsStartGroup
    {
        /// <summary>R*: the strip's rows for leads-on and exclusions, whatever the real strip (HYBRID 6; 5 rooms x 12 m / 3 m).</summary>
        public const int CanonicalStripRows = 20;
        /// <summary>Cells either side of the door column in the strip (FrontRooms3DGame StartAreaHalfCells, G:230).</summary>
        public const int StripHalfCells = 2;
        /// <summary>Zone roots tried after the first when leads-on fails, before the plan is accepted with a warning (4.3 step 6).</summary>
        public const int MaxLeadsOnRetries = 8;
        /// <summary>The E2o to E1o walk with the station removed must be at most this many cells (5.3).</summary>
        public const int LoopCap = 60;
        /// <summary>M7: the gate fires on entering C_N, N = 2 path cells outside the footprint (4.10, 9).</summary>
        public const int GateCells = 2;
        /// <summary>M7 rungs in walking cells (4.10): 1 far side 8-10, 2 any side 6-11, 3 14-20.</summary>
        public const int Rung1Min = 8, Rung1Max = 10, Rung2Min = 6, Rung2Max = 11, Rung3Min = 14, Rung3Max = 20;
        /// <summary>The far-side margin (4.10): exitField.DistanceTo(c) >= exitField.DistanceTo(player) + 3, in metres (FrontRoomsMapPathField.cs:7).</summary>
        public const float FarSideMetres = 3f;
        /// <summary>The exit field's reach in cells (FrontRoomsMapHunter SourceFieldCap 90 m); beyond it DistanceTo is infinity (PF:44-48), so far side.</summary>
        public const int ExitFieldCells = 30;
        /// <summary>Chunks built round the player (FrontRoomsLevel0.asset buildRadius 2; MW:796-797).</summary>
        public const int BuildRadius = 2;

        // The sight-line ray (HYBRID 1.6): eye 1.62 m (MU PlayerEye) on the door axis, Va 2 m behind the door line, Vb 1 m
        // past it; the flat sign on N's face of the EXIT wall, 0.08 m off the wall line (MW ExitSignBack), 2.18-2.392 m; an
        // arch passes when the ray is under its top (MU ArchTop 2.2 m) less 0.02 m and the axis keeps 0.2 m off its sides.
        public const float ViewBehindDoor = 2f, ViewPastDoor = 1f, SignBack = .08f, SignBottom = 2.18f, SignTop = 2.392f;
        public const float ArchRayTop = ModuleUnits.ArchTop - .02f, ArchSideMargin = .2f;
        public static float SignCentre => (SignBottom + SignTop) * .5f;

        static readonly GridCoord[] Steps4 = { new GridCoord(1, 0), new GridCoord(-1, 0), new GridCoord(0, 1), new GridCoord(0, -1) };

        // ---------- Edges and roots ----------

        /// <summary>FrontRoomsMapWorld's edge id (MW:2481-2486): the lower cell and the direction (MapGrid.EdgeId since step 1).</summary>
        public static long EdgeId(GridCoord a, GridCoord b) => MapGrid.EdgeId(a, b);

        static GridCoord[] roots;
        static int[] rootRings;

        /// <summary>Every root of RootRings (EP:170) rings in the order MapRootFor and RootFor try them (EP:668-672: ring, then b, then a), in cells.</summary>
        public static int RootCount { get { Roots(); return roots.Length; } }

        public static GridCoord RootAt(int index, out int ring)
        {
            Roots();
            ring = rootRings[index];
            return roots[index];
        }

        static void Roots()
        {
            if (roots != null) return;
            var period = FrontRoomsExitPlanner.PeriodCells;
            var list = new List<GridCoord>();
            var rings = new List<int>();
            for (var ring = 0; ring <= FrontRoomsExitPlanner.RootRings; ring++)
            for (var b = -ring; b <= ring; b++)
            for (var a = -ring; a <= ring; a++)
            {
                if (Math.Max(Math.Abs(a), Math.Abs(b)) != ring) continue;
                list.Add(new GridCoord(a * period, b * period));
                rings.Add(ring);
            }
            rootRings = rings.ToArray();
            roots = list.ToArray();
        }

        static bool StandardLevel0(FrontRoomsMapCache cache, GridCoord cell)
        {
            var zone = cache.ZoneOf(cell);
            return zone.height == ZoneHeight.Standard && zone.theme == ZoneTheme.Level0;
        }

        /// <summary>FindRoot's zone tests without the corridor (4.3): the 5 door-row cells (StartRowStandard, EP:583-591) and the station Standard Level 0, E2o Standard.</summary>
        public static bool ZoneFitsStation(FrontRoomsMapCache cache, StartGroupLayout l)
        {
            if (!FrontRoomsExitPlanner.StartRowStandard(cache, l.door, StripHalfCells)) return false;
            for (var y = l.station.y; y < l.station.y + l.station.h; y++)
            for (var x = l.station.x; x < l.station.x + l.station.w; x++)
                if (!StandardLevel0(cache, new GridCoord(x, y))) return false;
            return cache.ZoneOf(l.e2Out).height == ZoneHeight.Standard;
        }

        /// <summary>The corridor's zone test (4.3): C1..N Standard Level 0, so N is Standard (FrontStandard, EP:364).</summary>
        public static bool ZoneFitsCorridor(FrontRoomsMapCache cache, StartGroupLayout l)
        {
            for (var i = 1; i <= l.corridor; i++)
                if (!StandardLevel0(cache, l.Corridor(i))) return false;
            return true;
        }

        /// <summary>
        /// FindRoot (4.3): the first root from linear index <paramref name="from"/> on (ring, b, a order, RootRings rings)
        /// whose start group passes the zone tests. Reads zones only (cache.ZoneOf, FM:807) and generates nothing. -1 when none.
        /// </summary>
        public static int FindRoot(FrontRoomsMapCache cache, StartGroupTemplate t, GridCoord doorWorldCell, int from, int stripRows, out StartGroupLayout layout)
        {
            layout = default(StartGroupLayout);
            for (var i = Math.Max(0, from); i < RootCount; i++)
            {
                var root = RootAt(i, out _);
                // As RootFor (EP:674): the door's map cell is its world cell minus the root.
                var door = new GridCoord(doorWorldCell.x - root.x, doorWorldCell.y - root.y);
                var l = StartGroupLayout.For(t, door, t.corridor, stripRows);
                if (!ZoneFitsStation(cache, l) || !ZoneFitsCorridor(cache, l)) continue;
                layout = l;
                return i;
            }
            return -1;
        }

        // ---------- CARVE ----------

        /// <summary>
        /// The CARVE plan (4.2-4.3) on a virtual overlay: the zone root, the carve (forced edges with N-X Open, the glass
        /// rule, room drops with columns placed again on clones, key moves, repair A/B/C), leads-on from E2o and C1 with up
        /// to MaxLeadsOnRetries more zone roots (none passing: the first is accepted with a warning), then the C22 rules
        /// (FromStartGroup's asserts) and G1 on the carved data. The cache only generates chunks.
        /// <paramref name="retryGlass"/> (step 0 amendment A1; Red 2026-10-29: on by default, plan 10): a root whose repair
        /// needs a window join (or leaves a piece nothing joins) is retried like a leads-on failure, from the same budget.
        /// The feasibility batch passes false for the plan as written.
        /// </summary>
        public static StartGroupPlan EvaluateCarve(FrontRoomsMapCache cache, StartGroupTemplate t, GridCoord doorWorldCell, int stripRows, bool retryGlass = true)
        {
            var plan = new StartGroupPlan();
            if (cache == null || t == null || !t.Valid(out _)) return plan;
            var from = 0;
            var tried = 0;
            StartGroupPlan first = null, chosen = null;
            while (tried <= MaxLeadsOnRetries)
            {
                var index = FindRoot(cache, t, doorWorldCell, from, stripRows, out var layout);
                if (index < 0) break;
                tried++;
                var p = new StartGroupPlan { found = true, rootIndex = index, layout = layout, zoneRoots = tried, leadsOnRetries = tried - 1 };
                p.root = RootAt(index, out p.ring);
                p.carve = BuildCarve(cache, t, layout);
                p.leadsOnE2 = LeadsOn(cache, p.carve, layout, layout.e2Out);
                p.leadsOnC1 = LeadsOn(cache, p.carve, layout, layout.e1Out);
                if (first == null) first = p;
                var glassFree = !retryGlass || (p.carve.windowJoins == 0 && p.carve.repairFailures == 0);
                if (p.leadsOnE2 && p.leadsOnC1 && glassFree) { chosen = p; break; }
                from = index + 1;
            }
            if (first == null) return plan;
            if (chosen == null)
            {
                // 4.3 step 6: no zone root's leads-on passed in 1 + MaxLeadsOnRetries: accept with a warning (the EXIT is
                // reachable through the station anyway), on the nearest root.
                chosen = first;
                chosen.zoneRoots = tried;
                chosen.leadsOnRetries = tried - 1;
            }
            plan = chosen;
            var l = plan.layout;
            var c = plan.carve;
            plan.exitFits = ExitFits(cache, c, l, out plan.xRoom, out plan.xKey);
            plan.chunkWhole = KeepsChunkWhole(cache, c, l);
            plan.deadEnd = IsDeadEnd(cache, c, l);
            plan.sightline = Sightline(cache, c, l);
            plan.corridor = CorridorPastDoor(cache, c, l);
            plan.frontStandard = FrontRoomsExitPlanner.FrontStandard(cache, l.front);
            plan.g1 = G1(cache, c, l);
            // ExitPlan.distance = WalkStepsTo(D, N, avoiding X) + 1 (EP:501, 4.3), cap 40 (EP WidestBandMax).
            var steps = WalkSteps(cache, c, l, l.door, l.front, FrontRoomsExitPlanner.WidestBandMax);
            plan.distance = steps < 0 ? -1 : steps + 1;
            return plan;
        }

        static readonly StartGroupCarve NoOverlay = new StartGroupCarve();

        /// <summary>
        /// The planner the game and the validator call (4.3). Clears any carve the cache holds; makes the CARVE plan on the
        /// virtual overlay (EvaluateCarve, A1 on: a window join is retried like a leads-on failure, from the same budget of
        /// MaxLeadsOnRetries); turns it into map data (FrontRoomsMapCarve.From) and sets it on the cache (SetCarve: c and
        /// c' carved and pinned, the border overrides on the generator); then asserts on the carved cache what the overlay
        /// promised: leads-on from E2o and C1 as the overlay found it, G1, and the EXIT (FrontRoomsExitPlanner.FromStartGroup:
        /// ExitCellFits, KeepsChunkWhole, FrontStandard, Sightline Clear, the flat sign). Failure handling (4.11): a template
        /// the rules refuse or no zone root in RootRings rings gives no plan (error set, the cache left raw); a failed assert
        /// is a bug: the carve is cleared and the error set, so the caller falls back to today's start; when no root tried
        /// leads on (and, under A1, joins without glass) the nearest is accepted with a warning, since the EXIT is reachable
        /// through the station anyway. The seed is the cache's (Generator.Seed); the plan is a function of it, the tier-1
        /// generation, the door's world cell and the template, whatever the cache generated before.
        /// </summary>
        public static StartGroupPlan Plan(FrontRoomsMapCache cache, StartGroupTemplate t, GridCoord doorWorldCell, int stripRows = CanonicalStripRows)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var plan = PlanUntimed(cache, t, doorWorldCell, stripRows);
            plan.planMs = (float)watch.Elapsed.TotalMilliseconds;
            return plan;
        }

        static StartGroupPlan PlanUntimed(FrontRoomsMapCache cache, StartGroupTemplate t, GridCoord doorWorldCell, int stripRows)
        {
            if (cache == null) return new StartGroupPlan { error = "no map cache" };
            cache.ClearCarve();
            if (t == null) return new StartGroupPlan { error = "no station template" };
            if (!t.Valid(out var why)) return new StartGroupPlan { error = "the station template is refused: " + why };
            var plan = EvaluateCarve(cache, t, doorWorldCell, stripRows);
            if (!plan.found)
            {
                plan.error = "no zone root in " + FrontRoomsExitPlanner.RootRings + " rings fits the station, its corridor and E2o";
                return plan;
            }
            var l = plan.layout;
            var c = plan.carve;
            if (!plan.leadsOnE2 || !plan.leadsOnC1)
                plan.warning = "no root of " + plan.zoneRoots + " leads on from both E2o and C1 (E2o " + plan.leadsOnE2 + ", C1 " + plan.leadsOnC1 + "): the nearest accepted";
            else if (c.windowJoins > 0 || c.repairFailures > 0)
                plan.warning = "glass join: no root of " + plan.zoneRoots + " joins c and c' without glass (" + c.windowJoins + " window joins, " + c.repairFailures + " unjoined): the nearest accepted";

            var carve = FrontRoomsMapCarve.From(c, t, l, plan.root);
            cache.SetCarve(carve);
            var problems = new List<string>();
            if (LeadsOn(cache, NoOverlay, l, l.e2Out) != plan.leadsOnE2 || LeadsOn(cache, NoOverlay, l, l.e1Out) != plan.leadsOnC1) problems.Add("leads-on differs from the overlay");
            if (!FrontRoomsExitPlanner.StartDoorWestOpen(cache, l.door)) problems.Add("G1 (the start door's west edge is not Open)");
            var exit = FrontRoomsExitPlanner.FromStartGroup(cache, l, out var failed);
            if (!exit.found) problems.Add("FromStartGroup: " + failed);
            if (problems.Count > 0)
            {
                cache.ClearCarve();
                plan.error = "assert failed on the carved cache (a bug; today's start instead): " + string.Join("; ", problems);
                return plan;
            }
            plan.mapCarve = carve;
            plan.exit = exit;
            return plan;
        }

        /// <summary>G1 (StartDoorWestOpen, EP:597-598) on the overlay: D's west edge is Open.</summary>
        public static bool G1(FrontRoomsMapCache cache, StartGroupCarve carve, StartGroupLayout l) =>
            carve.Edge(cache, l.door, new GridCoord(l.door.x - 1, l.door.y)) == EdgeKind.Open;

        /// <summary>The station's own edges (4.2): the template's inner edges (the stand-in's all Open; D-west Open = G1), perimeter Wall but E1 and E2 (Arch); protected.</summary>
        static void ForceStation(StartGroupCarve carve, StartGroupTemplate t, StartGroupLayout l)
        {
            const StartGroupCarve.Source T = StartGroupCarve.Source.Template;
            var s = l.station;
            int x0 = s.x, x1 = s.x + s.w - 1, y0 = s.y, y1 = s.y + s.h - 1;
            for (var y = y0; y <= y1; y++)
            for (var x = x0; x <= x1; x++)
            {
                var cell = new GridCoord(x, y);
                if (x < x1) carve.Force(cell, new GridCoord(x + 1, y), t.InnerEast(x - x0, y - y0), T);
                if (y < y1) carve.Force(cell, new GridCoord(x, y + 1), t.InnerNorth(x - x0, y - y0), T);
            }
            for (var x = x0; x <= x1; x++)
            {
                // The south row is Wall in data, D's south edge too (4.7); the north wall has E1 on D's axis.
                carve.Force(new GridCoord(x, y0 - 1), new GridCoord(x, y0), EdgeKind.Wall, T);
                carve.Force(new GridCoord(x, y1), new GridCoord(x, y1 + 1), x == l.e1In.x ? EdgeKind.Arch : EdgeKind.Wall, T);
            }
            for (var y = y0; y <= y1; y++)
            {
                carve.Force(new GridCoord(x0 - 1, y), new GridCoord(x0, y), y == l.e2In.y ? EdgeKind.Arch : EdgeKind.Wall, T);
                carve.Force(new GridCoord(x1, y), new GridCoord(x1 + 1, y), EdgeKind.Wall, T);
            }
        }

        /// <summary>
        /// The overlay SEARCH evaluates under (5.3: "the station still overlaid"): the station's edges, and N-X Open, the
        /// EXIT door's own edge (correction V5: KeepsChunkWhole walks N to X, EP:433-434; the door is built whatever the data
        /// says, MW:2032-2036, so it edits nothing a player sees). Nothing else is forced. c and c' pinned as in any station mode.
        /// </summary>
        public static StartGroupCarve SearchOverlay(StartGroupTemplate t, StartGroupLayout l)
        {
            var carve = new StartGroupCarve();
            ForceStation(carve, t, l);
            carve.Force(l.front, l.exit, EdgeKind.Open, StartGroupCarve.Source.Template);
            carve.pinned.Add(l.DoorChunk);
            if (l.ExitChunk != l.DoorChunk) carve.pinned.Add(l.ExitChunk);
            return carve;
        }

        /// <summary>The carve on revision-0 data of c and c' (4.2), as a virtual overlay.</summary>
        public static StartGroupCarve BuildCarve(FrontRoomsMapCache cache, StartGroupTemplate t, StartGroupLayout l)
        {
            var carve = new StartGroupCarve();
            var gen = cache.Generator;
            const StartGroupCarve.Source T = StartGroupCarve.Source.Template;
            const int n = MapGrid.ChunkCells;
            ForceStation(carve, t, l);
            // The corridor's north edges C1 -> ... -> N, Open.
            for (var i = 1; i < l.corridor; i++) carve.Force(l.Corridor(i), l.Corridor(i + 1), EdgeKind.Open, T);
            // N: west and east Wall, so the flat sign faces the room (ExitSignIsEnd false, MW:1056-1058; CorridorPastDoor, EP:410-416).
            carve.Force(new GridCoord(l.front.x - 1, l.front.y), l.front, EdgeKind.Wall, T);
            carve.Force(l.front, new GridCoord(l.front.x + 1, l.front.y), EdgeKind.Wall, T);
            // X: west, east and north Wall (IsDeadEnd, EP:350), and N-X Open in data (V5: KeepsChunkWhole needs N to reach X, EP:433-434).
            carve.Force(new GridCoord(l.exit.x - 1, l.exit.y), l.exit, EdgeKind.Wall, T);
            carve.Force(l.exit, new GridCoord(l.exit.x + 1, l.exit.y), EdgeKind.Wall, T);
            carve.Force(l.exit, new GridCoord(l.exit.x, l.exit.y + 1), EdgeKind.Wall, T);
            carve.Force(l.front, l.exit, EdgeKind.Open, T);
            // The glass rule (EP:372-389): every window round N, N-1, N-2 but N-X becomes a wall (protected).
            for (var k = 0; k < FrontRoomsExitPlanner.SightlineCells; k++)
            {
                var cell = new GridCoord(l.front.x, l.front.y - k);
                foreach (var step in Steps4)
                {
                    var other = cell + step;
                    if (other == l.exit || carve.TryForced(cell, other, out _)) continue;
                    if (cache.Edge(cell, other) != EdgeKind.Window) continue;
                    carve.Force(cell, other, EdgeKind.Wall, StartGroupCarve.Source.SightlineGlass);
                    carve.glassWalled++;
                }
            }

            // The chunks the protected cells lie in (c and c'), pinned; their revision-0 data, cloned for the room drops.
            var region = new List<GridCoord>();
            void AddChunk(GridCoord cell)
            {
                var coord = MapGrid.ChunkOf(cell);
                if (!region.Contains(coord)) region.Add(coord);
            }
            for (var y = l.station.y; y < l.station.y + l.station.h; y++)
            for (var x = l.station.x; x < l.station.x + l.station.w; x++) AddChunk(new GridCoord(x, y));
            for (var i = 1; i <= l.corridor; i++) AddChunk(l.Corridor(i));
            AddChunk(l.exit);
            region.Sort((p, q) => p.y != q.y ? p.y.CompareTo(q.y) : p.x.CompareTo(q.x));
            carve.pinned.AddRange(region);

            // Rooms (4.2 roomDrop): every room rect over a protected cell goes (no furniture on the station or the line, X
            // outside rooms); PlaceColumns (FM:507-552) runs again on what is left; PlaceKeySpot (FM:679-716) reads the
            // key spot again, since a dropped module room no longer holds one.
            foreach (var coord in region)
            {
                var raw = cache.Get(coord);
                var clone = CloneChunk(raw);
                var rooms = new List<CellRect>();
                var mods = new List<RoomModuleData>();
                for (var r = 0; r < raw.rooms.Length; r++)
                {
                    var rect = raw.rooms[r];
                    bool hit = false, outside = false;
                    for (var j = rect.y; j < rect.y + rect.h; j++)
                    for (var i = rect.x; i < rect.x + rect.w; i++)
                    {
                        if (l.Protected(raw.Cell(i, j))) hit = true;
                        else outside = true;
                    }
                    if (!hit)
                    {
                        rooms.Add(rect);
                        mods.Add(raw.ModuleOf(r));
                        continue;
                    }
                    carve.roomDrops++;
                    if (outside) carve.roomDropsOutside++;
                    carve.droppedChunk.Add(coord);
                    carve.droppedRoom.Add(rect);
                }
                if (rooms.Count != raw.rooms.Length)
                {
                    clone.rooms = rooms.ToArray();
                    clone.roomModules = mods.ToArray();
                    gen.PlaceColumns(clone, clone.revision);
                    for (var j = 0; j <= n; j++)
                    for (var i = 0; i <= n; i++)
                    {
                        var k = i + j * (n + 1);
                        if (raw.pillar[k] == clone.pillar[k] && raw.pillarStyle[k] == clone.pillarStyle[k]) continue;
                        if (l.Protected(raw.Cell(i - 1, j - 1)) || l.Protected(raw.Cell(i, j - 1)) || l.Protected(raw.Cell(i - 1, j)) || l.Protected(raw.Cell(i, j))) continue;
                        carve.columnsChanged++;
                    }
                    gen.PlaceKeySpot(clone);
                    if (clone.keyCell != raw.keyCell) carve.keySpotResets++;
                }
                carve.chunks[coord] = clone;
            }

            // Zone keys off the station and X (4.2 keyMoves): a key lies in its own zone (V:284), so only zones owning a
            // station cell or X can have one there; it moves to the zone cell nearest the site outside them (PlaceKey's
            // search, FM:649-670, minus those cells; seed-only), keySpot false.
            var keyZones = new List<GridCoord>();
            void KeyZone(GridCoord cell)
            {
                var id = cache.ZoneOf(cell).id;
                if (!keyZones.Contains(id)) keyZones.Add(id);
            }
            for (var y = l.station.y; y < l.station.y + l.station.h; y++)
            for (var x = l.station.x; x < l.station.x + l.station.w; x++) KeyZone(new GridCoord(x, y));
            KeyZone(l.exit);
            bool Off(GridCoord cell) => l.InStation(cell) || cell == l.exit;
            foreach (var id in keyZones)
            {
                var zone = gen.Zone(id);
                if (zone.height == ZoneHeight.Tall) continue;
                var home = carve.ChunkFor(cache, id);
                if (!home.hasKey || (!Off(home.keyCell) && !Off(home.keySiteCell))) continue;
                var best = float.MaxValue;
                var to = home.keySiteCell;
                for (var y = (id.y - 2) * n; y < (id.y + 3) * n; y++)
                for (var x = (id.x - 2) * n; x < (id.x + 3) * n; x++)
                {
                    var cell = new GridCoord(x, y);
                    if (Off(cell) || gen.ZoneOf(cell).id != id) continue;
                    var ex = (x + .5f) * MapGrid.CellSize - zone.siteX;
                    var ez = (y + .5f) * MapGrid.CellSize - zone.siteZ;
                    var d = ex * ex + ez * ez;
                    if (d >= best) continue;
                    best = d;
                    to = cell;
                }
                carve.keyMoves.Add(new StartGroupKeyMove { zone = id, from = home.keyCell, site = home.keySiteCell, to = to });
            }

            // Repair (CARVE 1.7): A geometric pockets through seed-only borders, B the walkable join while the strip stands,
            // C the walkable join once ClearStartArea gives the strip back. Ties to the lowest edge id.
            var cells = new List<GridCoord>();
            foreach (var coord in region)
            {
                var o = MapGrid.ChunkOrigin(coord);
                for (var j = 0; j < n; j++)
                for (var i = 0; i < n; i++) cells.Add(new GridCoord(o.x + i, o.y + j));
            }
            bool ExcludedAB(GridCoord cell) => l.InStation(cell) || cell == l.exit || l.InStrip(cell);
            bool ExcludedC(GridCoord cell) => l.InStation(cell) || cell == l.exit;
            RepairGeometric(cache, carve, region, cells, ExcludedAB, StartGroupCarve.Source.RepairA);
            RepairJoin(cache, carve, region, cells, ExcludedAB, l.InStrip, StartGroupCarve.Source.RepairB);
            RepairJoin(cache, carve, region, cells, ExcludedC, cell => false, StartGroupCarve.Source.RepairC);

            // Border edges the carve forces: how many differ from the seed (step 1's Generator.BorderOverride entries, FM:349).
            foreach (var f in carve.forced.Values)
            {
                if (MapGrid.ChunkOf(f.a) == MapGrid.ChunkOf(f.b)) continue;
                carve.borderEdges++;
                if (f.kind != gen.BorderEdge(f.a, f.b.x > f.a.x)) carve.borderOverrides++;
            }
            return carve;
        }

        /// <summary>A deep copy of a chunk's data.</summary>
        public static MapChunk CloneChunk(MapChunk s) => new MapChunk
        {
            coord = s.coord, revision = s.revision, tier = s.tier,
            zone = (GridCoord[])s.zone.Clone(), height = (ZoneHeight[])s.height.Clone(),
            east = (EdgeKind[])s.east.Clone(), north = (EdgeKind[])s.north.Clone(), west = (EdgeKind[])s.west.Clone(), south = (EdgeKind[])s.south.Clone(),
            pillar = (bool[])s.pillar.Clone(), pillarStyle = (byte[])s.pillarStyle.Clone(),
            hasKey = s.hasKey, keyCell = s.keyCell, keySiteCell = s.keySiteCell, keySpot = s.keySpot,
            keyX = s.keyX, keyZ = s.keyZ, keyY = s.keyY, keyYaw = s.keyYaw, keyHost = s.keyHost, ownZone = s.ownZone,
            rooms = (CellRect[])s.rooms.Clone(), roomModules = (RoomModuleData[])s.roomModules.Clone(), lamp = (ModuleLamp[])s.lamp.Clone(),
        };

        // ---------- Repair ----------

        /// <summary>Union-find whose roots are the lowest index (deterministic), tracking whether a piece holds a cell that is not Tall.</summary>
        sealed class Joiner
        {
            readonly int[] parent;
            readonly bool[] lower;
            public Joiner(int count)
            {
                parent = new int[count];
                lower = new bool[count];
                for (var i = 0; i < count; i++) parent[i] = i;
            }
            public void MarkLower(int i) => lower[Find(i)] = true;
            public bool HasLower(int i) => lower[Find(i)];
            public int Find(int i)
            {
                while (parent[i] != i) { parent[i] = parent[parent[i]]; i = parent[i]; }
                return i;
            }
            public bool Union(int a, int b)
            {
                a = Find(a);
                b = Find(b);
                if (a == b) return false;
                var lo = Math.Min(a, b);
                var hi = Math.Max(a, b);
                parent[hi] = lo;
                lower[lo] |= lower[hi];
                return true;
            }
        }

        /// <summary>A repair opening between two cells, by RoomModuleStamp.Reconnect's kinds (ST:150-156): Arch at one height (1), Door Low-Standard (2), Window by a Tall zone (9, last resort).</summary>
        static EdgeKind OpeningKind(FrontRoomsMapGenerator gen, GridCoord a, GridCoord b, out int cost)
        {
            var ha = gen.HeightOf(a);
            var hb = gen.HeightOf(b);
            if (ha == hb) { cost = 1; return EdgeKind.Arch; }
            if (ha == ZoneHeight.Tall || hb == ZoneHeight.Tall) { cost = 9; return EdgeKind.Window; }
            cost = 2;
            return EdgeKind.Door;
        }

        static void Count(StartGroupCarve carve, StartGroupCarve.Source source, EdgeKind kind, bool lowerPiece)
        {
            if (source == StartGroupCarve.Source.RepairA) carve.repairA++;
            else if (source == StartGroupCarve.Source.RepairB) carve.repairB++;
            else carve.repairC++;
            if (kind != EdgeKind.Window) return;
            if (lowerPiece) carve.windowJoins++;
            else carve.tallWindowJoins++;
        }

        /// <summary>An edge out of c and c' (always a chunk-border edge): the overlay first, else the seed (BorderEdge, FM:349), never generating the neighbour.</summary>
        static EdgeKind OuterEdge(FrontRoomsMapCache cache, StartGroupCarve carve, GridCoord a, GridCoord b)
        {
            if (b.x < a.x || b.y < a.y) { var t = a; a = b; b = t; }
            return carve.BorderEdge(cache.Generator, a, b.x > a.x);
        }

        static Dictionary<GridCoord, int> Index(List<GridCoord> cells, Func<GridCoord, bool> excluded)
        {
            var index = new Dictionary<GridCoord, int>();
            for (var i = 0; i < cells.Count; i++) if (!excluded(cells[i])) index[cells[i]] = i;
            return index;
        }

        /// <summary>
        /// Phase A (CARVE 1.7): the cells of c and c' that are not excluded, joined by every edge the carve does not protect
        /// (and the protected ones that are walkable), are geometric pieces; each piece with no walkable seed-only border
        /// edge out of c and c' (into a cell that is not excluded) gets one unprotected border wall opened: the cheapest kind,
        /// then the lowest edge id. A piece of Tall cells only is left through windows by design (FM:653, 366), so a border
        /// window counts as its way out. Border edges are seed-only and never shift (FM:349-358; V:299-305).
        /// </summary>
        static void RepairGeometric(FrontRoomsMapCache cache, StartGroupCarve carve, List<GridCoord> region, List<GridCoord> cells, Func<GridCoord, bool> excluded, StartGroupCarve.Source source)
        {
            var gen = cache.Generator;
            var index = Index(cells, excluded);
            var joiner = new Joiner(cells.Count);
            foreach (var pair in index) if (gen.HeightOf(pair.Key) != ZoneHeight.Tall) joiner.MarkLower(pair.Value);
            bool InRegion(GridCoord cell) => region.Contains(MapGrid.ChunkOf(cell));
            foreach (var pair in index)
            {
                var a = pair.Key;
                for (var s = 0; s < 2; s++)
                {
                    var b = s == 0 ? new GridCoord(a.x + 1, a.y) : new GridCoord(a.x, a.y + 1);
                    if (!index.TryGetValue(b, out var bi)) continue;
                    if (carve.IsProtected(a, b) && !FrontRoomsExitPlanner.Walkable(carve.Edge(cache, a, b))) continue;
                    joiner.Union(pair.Value, bi);
                }
            }
            var hasOut = new HashSet<int>();
            var bestEdge = new Dictionary<int, long>();
            var bestCost = new Dictionary<int, int>();
            var bestA = new Dictionary<int, GridCoord>();
            var bestB = new Dictionary<int, GridCoord>();
            foreach (var pair in index)
            {
                var a = pair.Key;
                var piece = joiner.Find(pair.Value);
                foreach (var step in Steps4)
                {
                    var b = a + step;
                    if (InRegion(b) || excluded(b)) continue;
                    var kind = OuterEdge(cache, carve, a, b);
                    if (FrontRoomsExitPlanner.Walkable(kind) || (kind == EdgeKind.Window && !joiner.HasLower(piece))) { hasOut.Add(piece); continue; }
                    if (kind != EdgeKind.Wall || carve.IsProtected(a, b)) continue;
                    var id = EdgeId(a, b);
                    OpeningKind(gen, a, b, out var cost);
                    if (bestEdge.TryGetValue(piece, out var have) && (bestCost[piece] < cost || (bestCost[piece] == cost && have <= id))) continue;
                    bestEdge[piece] = id;
                    bestCost[piece] = cost;
                    bestA[piece] = a;
                    bestB[piece] = b;
                }
            }
            var pieces = new SortedSet<int>();
            foreach (var pair in index) pieces.Add(joiner.Find(pair.Value));
            foreach (var piece in pieces)
            {
                if (hasOut.Contains(piece)) continue;
                if (!bestEdge.ContainsKey(piece)) { carve.repairFailures++; continue; }
                var kind = OpeningKind(gen, bestA[piece], bestB[piece], out _);
                carve.Force(bestA[piece], bestB[piece], kind, source);
                Count(carve, source, kind, joiner.HasLower(piece));
            }
        }

        /// <summary>
        /// Phases B and C (CARVE 1.7, "inner join"; 3.3: E1o and E2o join without the station): a union-find over the
        /// walkable edges (walls and glass block) of the cells of c and c' that are not excluded. Walkable pieces are all
        /// Tall or all lower (a Tall edge to another height is a window or a wall, FM:364-368). While two lower pieces are
        /// split, the cheapest unprotected wall between two pieces opens, Kruskal-wise (same height Arch 1, Low-Standard
        /// Door 2, by RoomModuleStamp.Reconnect's kinds, ST:155; ties to the lowest edge id). A Tall-only piece is left
        /// through windows by design (FM:653), so it only needs glass to a lower piece or out of c and c'; one with neither
        /// gets the cheapest opening (an Arch to Tall, else a window: tallWindowJoins). Then every piece that still has no
        /// way out of c and c' (walkable for a lower piece, glass for a Tall one) gets the cheapest unprotected border wall
        /// (seed-only, FM:349-358); a lower piece only glass can free is a window join (5.4 bar 0), one nothing frees a
        /// repair failure.
        /// </summary>
        static void RepairJoin(FrontRoomsMapCache cache, StartGroupCarve carve, List<GridCoord> region, List<GridCoord> cells, Func<GridCoord, bool> excluded, Func<GridCoord, bool> rawReserved, StartGroupCarve.Source source)
        {
            var gen = cache.Generator;
            var index = Index(cells, excluded);
            var outNode = cells.Count;
            var walk = new Joiner(cells.Count);
            var glass = new Joiner(cells.Count + 1);
            foreach (var pair in index)
                if (gen.HeightOf(pair.Key) != ZoneHeight.Tall) { walk.MarkLower(pair.Value); glass.MarkLower(pair.Value); }
            bool InRegion(GridCoord cell) => region.Contains(MapGrid.ChunkOf(cell));
            var candidates = new List<long>();
            var candA = new Dictionary<long, GridCoord>();
            var candB = new Dictionary<long, GridCoord>();
            var candCost = new Dictionary<long, int>();
            // Per cell: a walkable / a glass way out of c and c' (into a cell not excluded), and its unprotected border walls.
            var walkOut = new HashSet<int>();
            var border = new List<long>();
            var borderA = new Dictionary<long, GridCoord>();
            var borderB = new Dictionary<long, GridCoord>();
            foreach (var pair in index)
            {
                var a = pair.Key;
                foreach (var step in Steps4)
                {
                    var b = a + step;
                    if (!InRegion(b))
                    {
                        if (excluded(b)) continue;
                        var outer = OuterEdge(cache, carve, a, b);
                        if (FrontRoomsExitPlanner.Walkable(outer)) walkOut.Add(pair.Value);
                        if (MapGrid.Passable(outer)) glass.Union(pair.Value, outNode);
                        else if (!carve.IsProtected(a, b))
                        {
                            var bid = EdgeId(a, b);
                            border.Add(bid);
                            borderA[bid] = a;
                            borderB[bid] = b;
                        }
                        continue;
                    }
                    if (step.x < 0 || step.y < 0 || !index.TryGetValue(b, out var bi)) continue;
                    var kind = carve.Edge(cache, a, b);
                    if (FrontRoomsExitPlanner.Walkable(kind)) walk.Union(pair.Value, bi);
                    if (MapGrid.Passable(kind)) glass.Union(pair.Value, bi);
                    else if (!carve.IsProtected(a, b))
                    {
                        var id = EdgeId(a, b);
                        OpeningKind(gen, a, b, out var cost);
                        candidates.Add(id);
                        candA[id] = a;
                        candB[id] = b;
                        candCost[id] = cost;
                    }
                }
            }
            // A Tall-only piece needs glass to a lower piece or out of c and c' only.
            bool TallFree(int i) => glass.HasLower(i) || glass.Find(i) == glass.Find(outNode);
            candidates.Sort((p, q) => candCost[p] != candCost[q] ? candCost[p].CompareTo(candCost[q]) : p.CompareTo(q));
            foreach (var id in candidates)
            {
                int ia = index[candA[id]], ib = index[candB[id]];
                int ra = walk.Find(ia), rb = walk.Find(ib);
                if (ra == rb) continue;
                bool la = walk.HasLower(ra), lb = walk.HasLower(rb);
                var need = la && lb || (!la && !TallFree(ia)) || (!lb && !TallFree(ib));
                if (!need) continue;
                var kind = OpeningKind(gen, candA[id], candB[id], out _);
                carve.Force(candA[id], candB[id], kind, source);
                Count(carve, source, kind, false);
                if (FrontRoomsExitPlanner.Walkable(kind)) walk.Union(ia, ib);
                glass.Union(ia, ib);
            }
            // Pieces with no way out of c and c': the cheapest border wall of the piece, else a window join or a failure.
            var pieceOut = new HashSet<int>();
            foreach (var i in walkOut) pieceOut.Add(walk.Find(i));
            var bestEdge = new Dictionary<int, long>();
            var bestCost = new Dictionary<int, int>();
            border.Sort();
            foreach (var bid in border)
            {
                var r = walk.Find(index[borderA[bid]]);
                OpeningKind(gen, borderA[bid], borderB[bid], out var cost);
                if (bestEdge.TryGetValue(r, out var have) && bestCost[r] <= cost) continue;
                bestEdge[r] = bid;
                bestCost[r] = cost;
            }
            var pieces = new SortedSet<int>();
            foreach (var pair in index) pieces.Add(walk.Find(pair.Value));
            foreach (var r in pieces)
            {
                var lower = walk.HasLower(r);
                if (lower ? pieceOut.Contains(r) : TallFree(r)) continue;
                if (bestEdge.TryGetValue(r, out var bid) && (!lower || bestCost[r] < 9))
                {
                    var kind = OpeningKind(gen, borderA[bid], borderB[bid], out _);
                    carve.Force(borderA[bid], borderB[bid], kind, source);
                    Count(carve, source, kind, false);
                    continue;
                }
                // A lower piece only glass frees (a window now, or a border wall that could only open as one): a window join.
                if (lower && (glass.Find(r) == glass.Find(outNode) || bestEdge.ContainsKey(r)))
                {
                    carve.windowJoins++;
                    carve.windowJoinCells.Add(cells[r]);
                    if (RawLeadsOn(cache, cells[r], rawReserved)) carve.windowJoinsCarve++;
                }
                else carve.repairFailures++;
            }
        }

        /// <summary>
        /// Diagnostics: from <paramref name="start"/> on the raw map (the cache's own edges, nothing forced), glass blocking,
        /// never into a reserved cell, the walk reaches LeadsOnCells (EP:168) cells.
        /// </summary>
        static bool RawLeadsOn(FrontRoomsMapCache cache, GridCoord start, Func<GridCoord, bool> reserved)
        {
            var seen = new HashSet<GridCoord> { start };
            var queue = new Queue<GridCoord>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                foreach (var step in Steps4)
                {
                    var next = cell + step;
                    if (seen.Contains(next) || reserved(next)) continue;
                    if (!FrontRoomsExitPlanner.Walkable(cache.Edge(cell, next))) continue;
                    seen.Add(next);
                    if (seen.Count >= FrontRoomsExitPlanner.LeadsOnCells) return true;
                    queue.Enqueue(next);
                }
            }
            return false;
        }

        // ---------- Walks on the overlay ----------

        /// <summary>
        /// Leads-on (LeadsIntoMaze, EP:630-650, glass blocking; 4.3 step 5): from <paramref name="start"/>, never into the
        /// strip, the station or X (nor back into D), the walk reaches LeadsOnCells (EP:168) cells, D and the start counted.
        /// </summary>
        public static bool LeadsOn(FrontRoomsMapCache cache, StartGroupCarve carve, StartGroupLayout l, GridCoord start)
        {
            var seen = new HashSet<GridCoord> { start, l.door };
            var queue = new Queue<GridCoord>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                foreach (var step in Steps4)
                {
                    var next = cell + step;
                    if (seen.Contains(next) || l.InStrip(next) || l.InStation(next) || next == l.exit) continue;
                    if (!FrontRoomsExitPlanner.Walkable(carve.Edge(cache, cell, next))) continue;
                    seen.Add(next);
                    if (seen.Count >= FrontRoomsExitPlanner.LeadsOnCells) return true;
                    queue.Enqueue(next);
                }
            }
            return false;
        }

        /// <summary>The loop (5.3): walking cells from E2o to E1o (C1) with the station, the strip and X removed (glass blocks); -1 past <paramref name="cap"/>.</summary>
        public static int Loop(FrontRoomsMapCache cache, StartGroupCarve carve, StartGroupLayout l, int cap)
        {
            var dist = new Dictionary<GridCoord, int> { { l.e2Out, 0 } };
            var queue = new Queue<GridCoord>();
            queue.Enqueue(l.e2Out);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                var d = dist[cell];
                if (cell == l.e1Out) return d;
                if (d >= cap) continue;
                foreach (var step in Steps4)
                {
                    var next = cell + step;
                    if (dist.ContainsKey(next) || l.InStrip(next) || l.InStation(next) || next == l.exit) continue;
                    if (!FrontRoomsExitPlanner.Walkable(carve.Edge(cache, cell, next))) continue;
                    dist[next] = d + 1;
                    queue.Enqueue(next);
                }
            }
            return -1;
        }

        /// <summary>Walking steps from <paramref name="from"/> to <paramref name="to"/> never into the strip or X, no wall, no glass (WalkDistances/WalkStepsTo, EP:475-505); -1 past the cap.</summary>
        public static int WalkSteps(FrontRoomsMapCache cache, StartGroupCarve carve, StartGroupLayout l, GridCoord from, GridCoord to, int cap)
        {
            var dist = new Dictionary<GridCoord, int> { { from, 0 } };
            var queue = new Queue<GridCoord>();
            queue.Enqueue(from);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                var d = dist[cell];
                if (cell == to) return d;
                if (d >= cap) continue;
                foreach (var step in Steps4)
                {
                    var next = cell + step;
                    if (dist.ContainsKey(next) || l.InStrip(next) || next == l.exit) continue;
                    if (!FrontRoomsExitPlanner.Walkable(carve.Edge(cache, cell, next))) continue;
                    dist[next] = d + 1;
                    queue.Enqueue(next);
                }
            }
            return -1;
        }

        // ---------- The C22 rules on the overlay ----------

        /// <summary>
        /// ExitCellFits (EP:331-347) on the carved data: X off the strip and its ring, chunk-local 1..6, outside every room
        /// rect left in its chunk, no column on its corners (room problems: <paramref name="room"/>), not its zone's key or
        /// key site cell after the carve (<paramref name="key"/>).
        /// </summary>
        public static bool ExitFits(FrontRoomsMapCache cache, StartGroupCarve carve, StartGroupLayout l, out bool room, out bool key)
        {
            room = false;
            key = false;
            var x = l.exit;
            if (FrontRoomsExitPlanner.InAreaOrRing(l.strip, x)) { room = true; return false; }
            const int n = MapGrid.ChunkCells;
            var chunk = carve.ChunkFor(cache, MapGrid.ChunkOf(x));
            int i = x.x - chunk.Origin.x, j = x.y - chunk.Origin.y;
            if (i < 1 || i > n - 2 || j < 1 || j > n - 2) { room = true; return false; }
            foreach (var r in chunk.rooms) if (r.Contains(i, j)) room = true;
            const int c = n + 1;
            if (chunk.pillar[i + j * c] || chunk.pillar[i + 1 + j * c] || chunk.pillar[i + (j + 1) * c] || chunk.pillar[i + 1 + (j + 1) * c]) room = true;
            if (carve.KeyOf(cache, cache.ZoneOf(x).id, out var keyCell, out var siteCell) && (keyCell == x || siteCell == x)) key = true;
            return !room && !key;
        }

        /// <summary>KeepsChunkWhole (EP:424-436, ReachInChunk EP:440-468) on the carved data: inside X's chunk, never through the strip, every edge but a wall passable.</summary>
        public static bool KeepsChunkWhole(FrontRoomsMapCache cache, StartGroupCarve carve, StartGroupLayout l)
        {
            var coord = MapGrid.ChunkOf(l.exit);
            if (MapGrid.ChunkOf(l.front) != coord) return false;
            var withX = new HashSet<GridCoord>();
            var reached = Reach(cache, carve, l, coord, l.front, l.exit, false, withX);
            if (!withX.Contains(l.exit)) return false;
            return Reach(cache, carve, l, coord, l.front, l.exit, true, new HashSet<GridCoord>()) == reached - 1;
        }

        static int Reach(FrontRoomsMapCache cache, StartGroupCarve carve, StartGroupLayout l, GridCoord coord, GridCoord from, GridCoord avoid, bool avoiding, HashSet<GridCoord> seen)
        {
            var stack = new Stack<GridCoord>();
            seen.Add(from);
            stack.Push(from);
            while (stack.Count > 0)
            {
                var cell = stack.Pop();
                foreach (var step in Steps4)
                {
                    var next = cell + step;
                    if (MapGrid.ChunkOf(next) != coord || seen.Contains(next) || (avoiding && next == avoid) || l.InStrip(next)) continue;
                    if (!MapGrid.Passable(carve.Edge(cache, cell, next))) continue;
                    seen.Add(next);
                    stack.Push(next);
                }
            }
            return seen.Count;
        }

        /// <summary>IsDeadEnd (EP:350-353): X's west, east and north edges are walls.</summary>
        public static bool IsDeadEnd(FrontRoomsMapCache cache, StartGroupCarve carve, StartGroupLayout l) =>
            carve.Edge(cache, l.exit, new GridCoord(l.exit.x - 1, l.exit.y)) == EdgeKind.Wall
            && carve.Edge(cache, l.exit, new GridCoord(l.exit.x + 1, l.exit.y)) == EdgeKind.Wall
            && carve.Edge(cache, l.exit, new GridCoord(l.exit.x, l.exit.y + 1)) == EdgeKind.Wall;

        /// <summary>Sightline (EP:372-389) on the carved data; MayBecomeWindow (EP:397-402) also says no for a pinned chunk (step 1's change, 4.3).</summary>
        public static FrontRoomsExitPlanner.SightlineView Sightline(FrontRoomsMapCache cache, StartGroupCarve carve, StartGroupLayout l)
        {
            var view = FrontRoomsExitPlanner.SightlineView.Clear;
            var exitChunk = MapGrid.ChunkOf(l.exit);
            for (var k = 0; k < FrontRoomsExitPlanner.SightlineCells; k++)
            {
                var cell = new GridCoord(l.front.x, l.front.y - k);
                foreach (var step in Steps4)
                {
                    var other = cell + step;
                    if (other == l.exit) continue;
                    if (carve.Edge(cache, cell, other) == EdgeKind.Window) return FrontRoomsExitPlanner.SightlineView.Window;
                    if (view != FrontRoomsExitPlanner.SightlineView.Clear || carve.IsPinned(MapGrid.ChunkOf(cell))) continue;
                    if (FrontRoomsExitPlanner.MayBecomeWindow(cache, cell, other, exitChunk)) view = FrontRoomsExitPlanner.SightlineView.MayGetWindow;
                }
            }
            return view;
        }

        /// <summary>CorridorPastDoor (EP:410-416) on the carved data (true means the end-on blade, not the flat sign).</summary>
        public static bool CorridorPastDoor(FrontRoomsMapCache cache, StartGroupCarve carve, StartGroupLayout l)
        {
            var west = new GridCoord(l.front.x - 1, l.front.y);
            var east = new GridCoord(l.front.x + 1, l.front.y);
            return !l.InStrip(west) && !l.InStrip(east)
                && FrontRoomsExitPlanner.Walkable(carve.Edge(cache, l.front, west)) && FrontRoomsExitPlanner.Walkable(carve.Edge(cache, l.front, east));
        }

        // ---------- The sight line ----------

        /// <summary>The line from the station to the EXIT sign: clear, a wall (or door or glass) on it, or an arch the ray misses.</summary>
        public enum LineView { Clear, Wall, ArchRay }

        /// <summary>
        /// The ray test (HYBRID 1.6; 5.3) along D's axis: E1 (its authored span, centred) and every line edge C_i to C_i+1
        /// Open, or an Arch (MapArch.Opening, the world's hash) the rays pass: the sign's centre from Va (2 m behind the door
        /// line) and its top from Vb (1 m past it) both under each arch's top less 0.02 m, the axis 0.2 m inside its sides.
        /// </summary>
        public static LineView Line(FrontRoomsMapCache cache, StartGroupCarve carve, StartGroupTemplate t, StartGroupLayout l, out int arches)
        {
            arches = 0;
            var seed = cache.Generator.Seed;
            if (!ArchPasses(l, t.e1Width, MapGrid.CellSize * .5f, l.e1Out.y * MapGrid.CellSize)) return LineView.ArchRay;
            for (var i = 1; i < l.corridor; i++)
            {
                var a = l.Corridor(i);
                var kind = carve.Edge(cache, a, l.Corridor(i + 1));
                if (kind == EdgeKind.Open) continue;
                if (kind != EdgeKind.Arch) return LineView.Wall;
                arches++;
                MapArch.Opening(seed, a, true, out var width, out var centre);
                if (!ArchPasses(l, width, centre, (a.y + 1) * MapGrid.CellSize)) return LineView.ArchRay;
            }
            return LineView.Clear;
        }

        static bool ArchPasses(StartGroupLayout l, float width, float centre, float z)
        {
            if (Math.Abs(centre - MapGrid.CellSize * .5f) > width * .5f - ArchSideMargin) return false;
            var doorLine = l.door.y * MapGrid.CellSize;
            var sign = (l.front.y + 1) * MapGrid.CellSize - SignBack;
            return RayHeight(doorLine - ViewBehindDoor, sign, SignCentre, z) <= ArchRayTop
                && RayHeight(doorLine + ViewPastDoor, sign, SignTop, z) <= ArchRayTop;
        }

        static float RayHeight(float eyeZ, float targetZ, float targetY, float z) =>
            ModuleUnits.PlayerEye + (targetY - ModuleUnits.PlayerEye) * (z - eyeZ) / (targetZ - eyeZ);

        // ---------- SEARCH ----------

        /// <summary>
        /// SEARCH, the natural line (5.3): in the same root order as FindRoot, the station overlaid (SearchOverlay), the first
        /// root and k (in the order given) whose corridor is already Open or ray-passing arches, whose flat sign is natural
        /// (N's sides as generated), whose X passes the rules as generated (no room drop, no key move), with no glass round N
        /// and X's chunk whole. Counts the reason of each turned-down candidate.
        /// </summary>
        public static StartGroupSearch EvaluateSearch(FrontRoomsMapCache cache, StartGroupTemplate t, GridCoord doorWorldCell, int stripRows, int[] corridors)
        {
            var result = new StartGroupSearch();
            if (cache == null || t == null || corridors == null || corridors.Length == 0) return result;
            for (var index = 0; index < RootCount; index++)
            {
                var root = RootAt(index, out var ring);
                var door = new GridCoord(doorWorldCell.x - root.x, doorWorldCell.y - root.y);
                if (!ZoneFitsStation(cache, StartGroupLayout.For(t, door, corridors[0], stripRows))) continue;
                var any = false;
                foreach (var k in corridors)
                    if (ZoneFitsCorridor(cache, StartGroupLayout.For(t, door, k, stripRows))) { any = true; break; }
                if (!any) continue;
                result.zoneRoots++;
                foreach (var k in corridors)
                {
                    var l = StartGroupLayout.For(t, door, k, stripRows);
                    if (!ZoneFitsCorridor(cache, l)) { result.rejects[(int)StartGroupReject.ZoneCorridor]++; continue; }
                    var overlay = SearchOverlay(t, l);
                    var reason = NaturalReject(cache, overlay, t, l);
                    if (reason != StartGroupReject.Count) { result.rejects[(int)reason]++; continue; }
                    result.found = true;
                    result.rootIndex = index;
                    result.ring = ring;
                    result.root = root;
                    result.k = k;
                    result.layout = l;
                    result.overlay = overlay;
                    return result;
                }
            }
            return result;
        }

        static StartGroupReject NaturalReject(FrontRoomsMapCache cache, StartGroupCarve overlay, StartGroupTemplate t, StartGroupLayout l)
        {
            var line = Line(cache, overlay, t, l, out _);
            if (line == LineView.Wall) return StartGroupReject.LineWall;
            if (line == LineView.ArchRay) return StartGroupReject.LineArchRay;
            if (CorridorPastDoor(cache, overlay, l)) return StartGroupReject.BladeSign;
            ExitFits(cache, overlay, l, out var room, out var key);
            if (room) return StartGroupReject.XRoom;
            if (key) return StartGroupReject.XKey;
            if (Sightline(cache, overlay, l) != FrontRoomsExitPlanner.SightlineView.Clear) return StartGroupReject.Sightline;
            if (!KeepsChunkWhole(cache, overlay, l)) return StartGroupReject.ChunkWhole;
            return StartGroupReject.Count;
        }

        // ---------- M7 ----------

        /// <summary>
        /// M7 first-send candidates per rung from the gate cell C2 (4.10), before visibility and body fit: the Relay's
        /// Search (H:750-773: built cells, Open, Arch and Door pass, walls and unbroken glass do not; PassageBetween walls off
        /// the strip and X, MW:2478) out to 20 cells, without the station and its ring, the cells within Chebyshev 2 of the
        /// exit walk D..N and X, and the strip. Built = the 5 x 5 chunks round the player (buildRadius 2). Far side: the
        /// exit field from N (orthogonal walk, metres) at least FarSideMetres beyond the player's; past the field's reach the
        /// distance is infinity (PF:44-48), so far. rung1Dot uses the dot test dot(c - p, N - p) &lt; 0 instead.
        /// </summary>
        public static StartGroupRungs RungCounts(FrontRoomsMapCache cache, StartGroupCarve carve, StartGroupLayout l)
        {
            var rungs = new StartGroupRungs();
            var player = l.Corridor(Math.Min(GateCells, l.corridor));
            var home = MapGrid.ChunkOf(player);
            bool Built(GridCoord cell)
            {
                var c = MapGrid.ChunkOf(cell);
                return Math.Abs(c.x - home.x) <= BuildRadius && Math.Abs(c.y - home.y) <= BuildRadius && !l.InStrip(cell) && cell != l.exit;
            }
            var depth = Walk(cache, carve, player, Rung3Max, Built);
            var field = Walk(cache, carve, l.front, ExitFieldCells, Built);
            var playerField = field.TryGetValue(player, out var pf) ? pf * MapGrid.CellSize : float.PositiveInfinity;
            foreach (var pair in depth)
            {
                var cell = pair.Key;
                var d = pair.Value;
                if (d < Rung2Min) continue;
                if (FrontRoomsExitPlanner.InAreaOrRing(l.station, cell)) continue;
                if (Math.Abs(cell.x - l.door.x) <= 2 && cell.y >= l.door.y - 2 && cell.y <= l.exit.y + 2) continue;
                if (d <= Rung2Max) rungs.rung2++;
                if (d >= Rung3Min && d <= Rung3Max) rungs.rung3++;
                if (d < Rung1Min || d > Rung1Max) continue;
                var cellField = field.TryGetValue(cell, out var f) ? f * MapGrid.CellSize : float.PositiveInfinity;
                if (cellField >= playerField + FarSideMetres) rungs.rung1++;
                if ((cell.x - player.x) * (l.front.x - player.x) + (cell.y - player.y) * (l.front.y - player.y) < 0) rungs.rung1Dot++;
            }
            return rungs;
        }

        static Dictionary<GridCoord, int> Walk(FrontRoomsMapCache cache, StartGroupCarve carve, GridCoord start, int maxDepth, Func<GridCoord, bool> built)
        {
            var depth = new Dictionary<GridCoord, int> { { start, 0 } };
            var queue = new Queue<GridCoord>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                var d = depth[cell];
                if (d >= maxDepth) continue;
                foreach (var step in Steps4)
                {
                    var next = cell + step;
                    if (depth.ContainsKey(next) || !built(next)) continue;
                    var kind = carve.Edge(cache, cell, next);
                    if (kind == EdgeKind.Wall || kind == EdgeKind.Window) continue;
                    depth[next] = d + 1;
                    queue.Enqueue(next);
                }
            }
            return depth;
        }
    }
}
