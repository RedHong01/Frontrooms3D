using System;
using System.Collections.Generic;

namespace FrontRooms.Map
{
    /// <summary>The skin a cell is drawn in (FrontRoomsMapCache.SkinOf): its zone's own, or the floor-1 station's.</summary>
    public enum MapSkin : byte { Zone, Station }

    /// <summary>
    /// The floor-1 start group's carve as map data (A26 step 1; notes/floor1_start_group_plan.md 4.2): the station, its
    /// two arches E1 and E2, the straight corridor C1..N and X, imposed on the generated map once per floor. Made from the
    /// planner's virtual overlay (StartGroupCarve, measured by step 0) and held by the cache (FrontRoomsMapCache.SetCarve):
    /// every Get applies it last, and the generator's BorderOverride carries its border edges, so every reader (Cache.Edge,
    /// BorderEdge, the world's EdgeNoGenerate, the Relay, the validator) sees one truth.
    /// What it holds:
    /// - forced edges by MapGrid.EdgeId, written into both chunks' copies of a border edge: the station's inside Open and
    ///   its walls but E1 and E2 (Arch), the corridor Open, N's and X's sides Wall, N-X Open (V5), glass round N, N-1, N-2
    ///   Wall, and the repair's openings (phases A-C); the template's and the glass rule's edges are protected;
    /// - the authored arch spans of E1 and E2 (ArchOverride), the station footprint (SkinOf) and its lamps, C1 Steady;
    /// - the room drop: every room over a protected cell (station, C1..N, X) leaves rooms[], the columns are placed again
    ///   and the key spot read again; a dropped module room's lamps go back to what the rooms left say (Auto outside them);
    /// - the zone keys moved off the station and X (seed-only, on the zone's home chunk), and the pins c and c'.
    /// Plain C#, like FrontRoomsMap.cs. A pinned chunk keeps revision 0, which the carve was planned on.
    /// </summary>
    public sealed class FrontRoomsMapCarve
    {
        /// <summary>An authored arch span: width and centre in metres from the edge's start (MapArch.Opening's terms).</summary>
        public struct ArchSpan
        {
            public float width, center;
        }

        /// <summary>One forced edge: <see cref="a"/> the west or south cell, <see cref="b"/> its neighbour.</summary>
        public struct Edge
        {
            public GridCoord a, b;
            public EdgeKind kind;
            /// <summary>The template's and the glass rule's edges (the repair never opens them).</summary>
            public bool isProtected;
        }

        // One write into a chunk's arrays: 0 east, 1 north, 2 west, 3 south.
        struct Write
        {
            public byte array;
            public int index;
            public EdgeKind kind;
        }

        struct LampWrite
        {
            public int index;
            public ModuleLamp lamp;
        }

        readonly List<Edge> edges = new List<Edge>();
        readonly Dictionary<long, int> edgeIndex = new Dictionary<long, int>();
        readonly Dictionary<long, EdgeKind> borderOverride = new Dictionary<long, EdgeKind>();
        readonly Dictionary<long, ArchSpan> arches = new Dictionary<long, ArchSpan>();
        readonly Dictionary<GridCoord, List<Write>> writes = new Dictionary<GridCoord, List<Write>>();
        readonly Dictionary<GridCoord, List<LampWrite>> lampWrites = new Dictionary<GridCoord, List<LampWrite>>();
        readonly Dictionary<GridCoord, StartGroupKeyMove> keyMoves = new Dictionary<GridCoord, StartGroupKeyMove>();
        readonly List<GridCoord> pins = new List<GridCoord>();
        readonly List<GridCoord> touched = new List<GridCoord>();

        /// <summary>The start group's cells (map cells, the root at the origin).</summary>
        public StartGroupLayout Layout { get; private set; }
        /// <summary>The root it was planned at (cells) and the planner's hash of everything it decides (StartGroupCarve.Hash).</summary>
        public GridCoord Root { get; private set; }
        public string Hash { get; private set; }

        /// <summary>The planner's counts: repair openings per phase, window joins (0 under A1), pieces nothing joined, room drops (those reaching outside the template), key moves, border overrides, glass walled.</summary>
        public int RepairA { get; private set; }
        public int RepairB { get; private set; }
        public int RepairC { get; private set; }
        public int WindowJoins { get; private set; }
        public int RepairFailures { get; private set; }
        public int RoomDrops { get; private set; }
        public int RoomDropsOutside { get; private set; }
        public int KeyMoves => keyMoves.Count;
        public int BorderOverrides { get; private set; }
        public int GlassWalled { get; private set; }

        /// <summary>Every forced edge, in edge id order.</summary>
        public IReadOnlyList<Edge> Edges => edges;
        /// <summary>The forced edges on a chunk border, by MapGrid.EdgeId: the generator's BorderOverride while the cache holds the carve.</summary>
        public IReadOnlyDictionary<long, EdgeKind> BorderOverride => borderOverride;
        /// <summary>c and c' (never shift).</summary>
        public IReadOnlyList<GridCoord> Pins => pins;
        /// <summary>Every chunk Apply changes: the pins, both chunks of each forced edge, the home chunk of each moved key.</summary>
        public IReadOnlyList<GridCoord> Touched => touched;
        /// <summary>The zone keys moved off the station and X, by zone.</summary>
        public IEnumerable<StartGroupKeyMove> Moves => keyMoves.Values;

        FrontRoomsMapCarve() { }

        /// <summary>
        /// The carve of a plan: the overlay the planner built on raw revision-0 data (FrontRoomsStartGroup.BuildCarve, or a
        /// StartGroupPlan's carve), the template it was built for (arch spans, lamps) and its layout and root.
        /// </summary>
        public static FrontRoomsMapCarve From(StartGroupCarve overlay, StartGroupTemplate template, StartGroupLayout layout, GridCoord root)
        {
            if (overlay == null) throw new ArgumentNullException(nameof(overlay));
            if (template == null) throw new ArgumentNullException(nameof(template));
            var carve = new FrontRoomsMapCarve { Layout = layout, Root = root, Hash = overlay.Hash(root, layout.door, layout.exit) };
            carve.RepairA = overlay.repairA;
            carve.RepairB = overlay.repairB;
            carve.RepairC = overlay.repairC;
            carve.WindowJoins = overlay.windowJoins;
            carve.RepairFailures = overlay.repairFailures;
            carve.RoomDrops = overlay.roomDrops;
            carve.RoomDropsOutside = overlay.roomDropsOutside;
            carve.BorderOverrides = overlay.borderOverrides;
            carve.GlassWalled = overlay.glassWalled;
            foreach (var coord in overlay.pinned) carve.Pin(coord);

            var ids = new List<long>(overlay.forced.Keys);
            ids.Sort();
            foreach (var id in ids)
            {
                var f = overlay.forced[id];
                var isProtected = f.source == StartGroupCarve.Source.Template || f.source == StartGroupCarve.Source.SightlineGlass;
                carve.edgeIndex[id] = carve.edges.Count;
                carve.edges.Add(new Edge { a = f.a, b = f.b, kind = f.kind, isProtected = isProtected });
                var east = f.b.x > f.a.x;
                var chunkA = MapGrid.ChunkOf(f.a);
                var chunkB = MapGrid.ChunkOf(f.b);
                var oa = MapGrid.ChunkOrigin(chunkA);
                carve.AddWrite(chunkA, (byte)(east ? 0 : 1), MapGrid.LocalIndex(f.a.x - oa.x, f.a.y - oa.y), f.kind);
                if (chunkB == chunkA) continue;
                // A border edge: the neighbour's west or south copy too, and the generator answers it (BorderOverride).
                var ob = MapGrid.ChunkOrigin(chunkB);
                carve.AddWrite(chunkB, (byte)(east ? 2 : 3), east ? f.b.y - ob.y : f.b.x - ob.x, f.kind);
                carve.borderOverride[id] = f.kind;
            }

            // E1 and E2: the template's spans, centred on their edges (ArchOpening reads them before the hash, 4.2).
            var half = MapGrid.CellSize * .5f;
            carve.arches[MapGrid.EdgeId(layout.e1In, layout.e1Out)] = new ArchSpan { width = template.e1Width, center = half };
            carve.arches[MapGrid.EdgeId(layout.e2Out, layout.e2In)] = new ArchSpan { width = template.e2Width, center = half };

            // Lamps (4.6): the station's table, never Auto, and C1 Steady.
            var s = layout.station;
            for (var j = 0; j < s.h; j++)
            for (var i = 0; i < s.w; i++)
                carve.AddLamp(new GridCoord(s.x + i, s.y + j), template.LampAt(i, j));
            carve.AddLamp(layout.e1Out, ModuleLamp.Steady);

            // Zone keys off the station and X, on their zones' home chunks (a zone is named by its site's chunk).
            foreach (var m in overlay.keyMoves)
            {
                carve.keyMoves[m.zone] = m;
                carve.Touch(m.zone);
            }
            return carve;
        }

        void Pin(GridCoord coord)
        {
            if (!pins.Contains(coord)) pins.Add(coord);
            Touch(coord);
        }

        void Touch(GridCoord coord)
        {
            if (!touched.Contains(coord)) touched.Add(coord);
        }

        void AddWrite(GridCoord chunk, byte array, int index, EdgeKind kind)
        {
            if (!writes.TryGetValue(chunk, out var list)) writes[chunk] = list = new List<Write>();
            list.Add(new Write { array = array, index = index, kind = kind });
            Touch(chunk);
        }

        void AddLamp(GridCoord cell, ModuleLamp lamp)
        {
            var chunk = MapGrid.ChunkOf(cell);
            var o = MapGrid.ChunkOrigin(chunk);
            if (!lampWrites.TryGetValue(chunk, out var list)) lampWrites[chunk] = list = new List<LampWrite>();
            list.Add(new LampWrite { index = MapGrid.LocalIndex(cell.x - o.x, cell.y - o.y), lamp = lamp });
            Touch(chunk);
        }

        // ---------- Queries ----------

        public bool IsPinned(GridCoord chunk) => pins.Contains(chunk);
        public bool InStation(GridCoord cell) => FrontRoomsExitPlanner.InArea(Layout.station, cell);
        public MapSkin SkinOf(GridCoord cell) => InStation(cell) ? MapSkin.Station : MapSkin.Zone;

        /// <summary>The kind the carve forces on the edge a-b, if it forces one.</summary>
        public bool TryForced(GridCoord a, GridCoord b, out EdgeKind kind)
        {
            if (edgeIndex.TryGetValue(MapGrid.EdgeId(a, b), out var i)) { kind = edges[i].kind; return true; }
            kind = EdgeKind.Wall;
            return false;
        }

        /// <summary>True for an edge of the template or the glass rule (4.2 protectedEdges).</summary>
        public bool IsProtected(GridCoord a, GridCoord b) => edgeIndex.TryGetValue(MapGrid.EdgeId(a, b), out var i) && edges[i].isProtected;

        /// <summary>E1's or E2's authored span; false for every other edge.</summary>
        public bool TryArch(GridCoord a, GridCoord b, out float width, out float center)
        {
            if (arches.TryGetValue(MapGrid.EdgeId(a, b), out var span)) { width = span.width; center = span.center; return true; }
            width = center = 0f;
            return false;
        }

        // ---------- Apply (FrontRoomsMapCache.Get, last) ----------

        /// <summary>
        /// Carve one freshly generated chunk: the room drop (pinned chunks), then the forced edges in this chunk's arrays,
        /// the lamps and the key move. A chunk the carve does not touch is left as it is.
        /// </summary>
        public void Apply(FrontRoomsMapGenerator generator, MapChunk chunk)
        {
            if (generator == null || chunk == null || !touched.Contains(chunk.coord)) return;
            if (pins.Contains(chunk.coord)) DropRooms(generator, chunk);
            if (writes.TryGetValue(chunk.coord, out var list))
                foreach (var w in list)
                {
                    switch (w.array)
                    {
                        case 0: chunk.east[w.index] = w.kind; break;
                        case 1: chunk.north[w.index] = w.kind; break;
                        case 2: chunk.west[w.index] = w.kind; break;
                        default: chunk.south[w.index] = w.kind; break;
                    }
                }
            if (lampWrites.TryGetValue(chunk.coord, out var lamps))
                foreach (var l in lamps) chunk.lamp[l.index] = l.lamp;
            if (keyMoves.TryGetValue(chunk.coord, out var move) && chunk.hasKey)
            {
                // Seed-only (PlaceKey's search minus the station and X), so the same on every revision of the home chunk.
                chunk.keyCell = move.to;
                chunk.keySiteCell = move.to;
                chunk.keySpot = false;
                chunk.keyHost = null;
            }
        }

        /// <summary>
        /// 4.2 roomDrop: every room rect over a protected cell leaves rooms[] and roomModules[] (its edges stay as they
        /// are), PlaceColumns runs again (FM:507) and PlaceKeySpot reads the key spot again (a dropped module room holds
        /// none). A dropped module's lamps go back to the last module room left over the cell, else Auto.
        /// </summary>
        void DropRooms(FrontRoomsMapGenerator generator, MapChunk chunk)
        {
            var l = Layout;
            List<CellRect> kept = null, dropped = null;
            List<RoomModuleData> keptModules = null;
            List<bool> droppedModule = null;
            for (var r = 0; r < chunk.rooms.Length; r++)
            {
                var rect = chunk.rooms[r];
                var hit = false;
                for (var j = rect.y; j < rect.y + rect.h && !hit; j++)
                for (var i = rect.x; i < rect.x + rect.w && !hit; i++)
                    hit = l.Protected(chunk.Cell(i, j));
                if (hit && dropped == null)
                {
                    dropped = new List<CellRect>();
                    droppedModule = new List<bool>();
                    kept = new List<CellRect>();
                    keptModules = new List<RoomModuleData>();
                    for (var k = 0; k < r; k++) { kept.Add(chunk.rooms[k]); keptModules.Add(chunk.ModuleOf(k)); }
                }
                if (hit) { dropped.Add(rect); droppedModule.Add(chunk.ModuleOf(r) != null); }
                else if (kept != null) { kept.Add(rect); keptModules.Add(chunk.ModuleOf(r)); }
            }
            if (dropped == null) return;
            chunk.rooms = kept.ToArray();
            chunk.roomModules = keptModules.ToArray();
            generator.PlaceColumns(chunk, chunk.revision);
            generator.PlaceKeySpot(chunk);
            for (var d = 0; d < dropped.Count; d++)
            {
                if (!droppedModule[d]) continue;
                var rect = dropped[d];
                for (var j = rect.y; j < rect.y + rect.h; j++)
                for (var i = rect.x; i < rect.x + rect.w; i++)
                    chunk.lamp[MapGrid.LocalIndex(i, j)] = KeptLamp(chunk, i, j);
            }
        }

        static ModuleLamp KeptLamp(MapChunk chunk, int i, int j)
        {
            // Later rooms are on top (RoomModuleStamp writes each module's lamps as it is stamped).
            for (var r = chunk.rooms.Length - 1; r >= 0; r--)
            {
                var module = chunk.ModuleOf(r);
                var rect = chunk.rooms[r];
                if (module != null && rect.Contains(i, j)) return module.LampAt(i - rect.x, j - rect.y);
            }
            return ModuleLamp.Auto;
        }
    }
}
