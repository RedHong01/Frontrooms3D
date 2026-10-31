using System;
using System.Collections.Generic;
using FrontRooms.Map;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Level transitions (production, 2026-10-22): the one entry point the map calls (CONTRACT_map.diff, T2-T6).
///
/// Per chunk the map makes one <see cref="ChunkBuild"/> with <see cref="BeginChunk"/> and calls, in BuildInto:
///  - <see cref="ChunkBuild.TakesCell"/> for each cell's floor and ceiling slabs (a Level 0 cell of a renovated
///    crossing is built in pieces here; the map keeps both full slabs in collision);
///  - <see cref="ChunkBuild.Open"/> for an open edge, <see cref="ChunkBuild.Edge"/> for every other edge
///    (before its wall pieces, with the opening exactly as BuildEdge computed it), and
///    <see cref="ChunkBuild.Piece"/> for each wall piece: true means the kit builds that piece's render
///    geometry (insets, band skins, post reach at band posts) and the map adds only its own collision boxes,
///    unchanged and in the same order;
///  - <see cref="ChunkBuild.Corner"/> once per lattice point (each cell's south-west corner);
///  - <see cref="ChunkBuild.Finish"/> after the cell loop: Office-wall pieces, paper and carpet go into the
///    map's own block meshes (0 extra draws, the block's grime band); everything else is one renderer per
///    chunk x material x shadow flag under "Transitions", with no MaterialPropertyBlock (SRP batching), and
///    the props. Returns the free-post collider boxes (the only collision the kits add).
/// The view is read only: the kits never write map state, lamps, doors or zones.
/// </summary>
public static partial class FrontRoomsTransitionKit
{
    /// <summary>The map's read-only view, one per map cache (FrontRoomsMapWorld.TransitionView).</summary>
    public sealed class View
    {
        public Func<GridCoord, ZoneTheme> theme;
        public Func<GridCoord, float> ceiling;
        /// <summary>The edge kind the map builds (today StartAreaEdge over the cache; EffectiveEdge after C22).</summary>
        public Func<GridCoord, GridCoord, EdgeKind> edge;
        /// <summary>Cells the kits leave exactly as today: the start area (later the exit area, X1/X2, station rects).</summary>
        public Func<GridCoord, bool> excluded;
        /// <summary>FrontRoomsMapWorld.BorderRenovated.</summary>
        public Func<GridCoord, GridCoord, bool> renovated;
        public ArchFn arch;
        public int seed;
        public Material officeWall, level0Wall, officeFloor, level0Floor, officeCeiling, level0Ceiling, trim;
        public float wallpaperRepeat = 1.2f;

        public Material WallOf(GridCoord cell) => theme(cell) == ZoneTheme.Office ? officeWall : level0Wall;
    }

    public delegate void BoxFn(int block, Material material, Vector3 centre, Vector3 size, float repeat);
    public delegate void AppendFn(int block, Material material, List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> t);

    /// <summary>Tools: chunks built with the kits, kit renderers and their triangles since the last reset.</summary>
    public static int StatChunks, StatRenderers, StatTris, StatProps, StatPosts, StatPiecesTaken, StatCellsTaken;

    public static void ResetStats()
    {
        StatChunks = StatRenderers = StatTris = StatProps = StatPosts = StatPiecesTaken = StatCellsTaken = 0;
        FrameKit.Count.Reset();
        FrameKit.Built.Clear();
        RenoKit.Stats.Clear();
    }

    /// <summary>
    /// Start one chunk. Null (the map builds exactly as today) when the kits are off.
    /// blockOf(i, j, ceiling) and blockCeiling(block) are the map's own block rule; collide adds one box to the
    /// chunk's collision exactly as the map's Solid does; furnished(cell) says a cell is in a room Furnish
    /// dresses (or a module room, or another chunk), where no floor prop may stand.
    /// </summary>
    public static ChunkBuild BeginChunk(View view, GridCoord coord, Transform root, Func<int, int, float, int> blockOf, Func<int, float> blockCeiling,
        Action<Vector3, Vector3> collide, Func<GridCoord, bool> furnished)
    {
        if (!Kits || view == null || root == null) return null;
        var b = new ChunkBuild(view, coord, root, blockOf, blockCeiling, collide, furnished);
        StatChunks++;
        return b;
    }

    // ---------- keys (structs, no tuples) ----------

    struct BlockMatKey : IEquatable<BlockMatKey>
    {
        public int block;
        public Material mat;
        public BlockMatKey(int b, Material m) { block = b; mat = m; }
        public bool Equals(BlockMatKey o) => block == o.block && mat == o.mat;
        public override bool Equals(object o) => o is BlockMatKey k && Equals(k);
        public override int GetHashCode() => unchecked(block * 397 ^ (mat != null ? FrontRoomsObjectId.Of(mat) : 0));
    }

    struct LooseKey : IEquatable<LooseKey>
    {
        public Material mat;
        public bool shadows;
        public LooseKey(Material m, bool s) { mat = m; shadows = s; }
        public bool Equals(LooseKey o) => mat == o.mat && shadows == o.shadows;
        public override bool Equals(object o) => o is LooseKey k && Equals(k);
        public override int GetHashCode() => unchecked((mat != null ? FrontRoomsObjectId.Of(mat) : 0) * 2 + (shadows ? 1 : 0));
    }

    struct VariantKey : IEquatable<VariantKey>
    {
        public int id, cm;
        public bool Equals(VariantKey o) => id == o.id && cm == o.cm;
        public override bool Equals(object o) => o is VariantKey k && Equals(k);
        public override int GetHashCode() => unchecked(id * 7919 ^ cm);
    }

    static readonly Dictionary<VariantKey, Material> Variants = new Dictionary<VariantKey, Material>();
    static readonly int CeilingHeightId = Shader.PropertyToID("_CeilingHeight");

    /// <summary>
    /// A grimed stage material for one ceiling height (its _CeilingHeight set on a cached instance), so kit
    /// renderers keep the grime band without a property block. Shared by every chunk and map.
    /// </summary>
    static Material Variant(Material m, float worldCeilingY)
    {
        if (m == null || !m.HasProperty(CeilingHeightId)) return m;
        if (Mathf.Abs(m.GetFloat(CeilingHeightId) - worldCeilingY) < .005f) return m;
        var key = new VariantKey { id = FrontRoomsObjectId.Of(m), cm = Mathf.RoundToInt(worldCeilingY * 100f) };
        if (Variants.TryGetValue(key, out var v) && v != null) return v;
        v = new Material(m) { name = m.name + " @" + (key.cm / 100f).ToString("0.0") + " m", hideFlags = HideFlags.DontSave };
        v.SetFloat(CeilingHeightId, worldCeilingY);
        Variants[key] = v;
        return v;
    }

    /// <summary>One chunk's transition build. Implements the frame kit's sink and the renovation kit's output.</summary>
    public sealed class ChunkBuild : FrameKit.ISink, RenoKit.IOut
    {
        readonly View view;
        readonly GridCoord coord, cell0;
        readonly Transform root;
        readonly Func<int, int, float, int> blockOf;
        readonly Func<int, float> blockCeiling;
        readonly Action<Vector3, Vector3> collide;
        readonly bool reduced;
        readonly RenoKit.Context reno;
        readonly Dictionary<GridCoord, FrameKit.TransitionCorner> corners = new Dictionary<GridCoord, FrameKit.TransitionCorner>();
        readonly FrontRoomsTransitionKit.PostPiece[] posts = new FrontRoomsTransitionKit.PostPiece[4];

        // Outputs.
        struct BoxCmd { public int block; public Material mat; public Vector3 centre, size; public float repeat; }
        readonly List<BoxCmd> blockBoxes = new List<BoxCmd>();
        readonly Dictionary<BlockMatKey, RenoKit.RawCmd> blockRaws = new Dictionary<BlockMatKey, RenoKit.RawCmd>();
        readonly Dictionary<int, TransitionMesh> officeBlocks = new Dictionary<int, TransitionMesh>();
        readonly Dictionary<int, TransitionMesh> trimBlocks = new Dictionary<int, TransitionMesh>();
        readonly Dictionary<LooseKey, TransitionMesh> loose = new Dictionary<LooseKey, TransitionMesh>();
        readonly Dictionary<LooseKey, RenoKit.RawCmd> looseRaws = new Dictionary<LooseKey, RenoKit.RawCmd>();
        readonly List<Bounds> colliders = new List<Bounds>();
        struct PropCmd { public string asset; public Vector3 position; public Quaternion rotation; }
        readonly List<PropCmd> props = new List<PropCmd>();
        readonly List<FrontRoomsWallFixtures.KeepOut> keepOuts = new List<FrontRoomsWallFixtures.KeepOut>();

        // The cell being built (chunk-local), for the block of Office-wall pieces.
        int ci, cj;

        // The edge being built (Edge, then its Piece calls).
        bool edgeActive, alongX, beadArch, capStart, capEnd, bandA, bandB, haloA, haloB, bandStart, bandEnd;
        float hwA, hwB, lo, hi, top;
        int rsA, rsB, reA, reB;
        RenoKit.Edge e;
        RenoKit.Face faceA, faceB;

        internal ChunkBuild(View v, GridCoord chunk, Transform chunkRoot, Func<int, int, float, int> blockOfFn, Func<int, float> blockCeilingFn,
            Action<Vector3, Vector3> collideFn, Func<GridCoord, bool> furnished)
        {
            view = v;
            coord = chunk;
            cell0 = new GridCoord(chunk.x * MapGrid.ChunkCells, chunk.y * MapGrid.ChunkCells);
            root = chunkRoot;
            blockOf = blockOfFn;
            blockCeiling = blockCeilingFn;
            collide = collideFn;
            reduced = Reduced;
            reno = new RenoKit.Context
            {
                theme = v.theme,
                ceiling = v.ceiling,
                edge = v.edge,
                arch = v.arch,
                excluded = v.excluded,
                renovated = v.renovated,
                furnished = furnished,
                seed = v.seed,
                mats = RenoKit.Mats.Load(v.level0Ceiling),
                reduced = reduced,
                Out = this,
            };
            reno.Begin(cell0, chunkRoot.position);
            FrameKit.R = reduced;
            // A keep-out left over from a chunk whose Install never ran (Suppressed) must not reach this one.
            FrontRoomsWallFixtures.PendingKeepOuts.Clear();
        }

        Vector3 LocalOf(GridCoord p) => new Vector3((p.x - cell0.x) * MapGrid.CellSize, 0f, (p.y - cell0.y) * MapGrid.CellSize);

        void SetCell(GridCoord cell)
        {
            ci = Mathf.Clamp(cell.x - cell0.x, 0, MapGrid.ChunkCells - 1);
            cj = Mathf.Clamp(cell.y - cell0.y, 0, MapGrid.ChunkCells - 1);
        }

        bool Excluded(GridCoord c) => view.excluded != null && view.excluded(c);

        // ---------- corners ----------

        FrameKit.TransitionCorner CornerAt(GridCoord p)
        {
            if (corners.TryGetValue(p, out var known)) return known;
            var c = new FrameKit.TransitionCorner { point = p, at = LocalOf(p) };
            var cells = new[] { new GridCoord(p.x - 1, p.y - 1), new GridCoord(p.x, p.y - 1), new GridCoord(p.x - 1, p.y), p };
            for (var q = 0; q < 4; q++)
            {
                c.theme[q] = view.theme(cells[q]);
                c.ceiling[q] = view.ceiling(cells[q]);
                c.wall[q] = view.WallOf(cells[q]);
                if (Excluded(cells[q])) c.special = true;
            }
            // Arms: W, E, S, N.
            c.arm[0] = view.edge(cells[0], cells[2]);
            c.arm[1] = view.edge(cells[1], cells[3]);
            c.arm[2] = view.edge(cells[0], cells[1]);
            c.arm[3] = view.edge(cells[2], cells[3]);
            c.armHeight[0] = Mathf.Max(c.ceiling[0], c.ceiling[2]);
            c.armHeight[1] = Mathf.Max(c.ceiling[1], c.ceiling[3]);
            c.armHeight[2] = Mathf.Max(c.ceiling[0], c.ceiling[1]);
            c.armHeight[3] = Mathf.Max(c.ceiling[2], c.ceiling[3]);
            corners[p] = c;
            return c;
        }

        // ---------- band posts: the B0.1 rule with the band's stage in each face's finish ----------

        static int HeightClass(float ceiling) => ceiling < 2.65f ? 0 : ceiling < 4f ? 1 : 2;

        RenoKit.Stage StageAt(GridCoord a, GridCoord b, bool ax, bool sideB, int drop)
        {
            var f = RenoKit.FaceOf(reno, a, b, ax, sideB);
            return f.level0 ? f.At(drop) : RenoKit.Stage.Paper;
        }

        /// <summary>The piece between a and b at a post, with the band's stage at the drop next to the post folded into each finish.</summary>
        FrontRoomsTransitionKit.PostPiece BandPiece(GridCoord a, GridCoord b, bool ax, int drop, ref bool band)
        {
            if (view.edge(a, b) == EdgeKind.Open) return default;
            var sa = StageAt(a, b, ax, false, drop);
            var sb = StageAt(a, b, ax, true, drop);
            if (sa != RenoKit.Stage.Paper || sb != RenoKit.Stage.Paper) band = true;
            Material wa = view.WallOf(a), wb = view.WallOf(b);
            return new FrontRoomsTransitionKit.PostPiece
            {
                present = true,
                finishA = ((long)FrontRoomsObjectId.Of(wa) << 8) | ((long)HeightClass(view.ceiling(a)) << 4) | (long)sa,
                finishB = ((long)FrontRoomsObjectId.Of(wb) << 8) | ((long)HeightClass(view.ceiling(b)) << 4) | (long)sb,
                officeA = wa == view.officeWall,
                officeB = wb == view.officeWall,
            };
        }

        /// <summary>At post k: is any face round it in a band at the drop next to the post? If so, the reach of the piece in role.</summary>
        bool BandPost(GridCoord k, int role, out int reachA, out int reachB)
        {
            GridCoord sw = new GridCoord(k.x - 1, k.y - 1), se = new GridCoord(k.x, k.y - 1), nw = new GridCoord(k.x - 1, k.y), ne = k;
            var band = false;
            // South / West pieces end at the post (their drop 3); North / East pieces start there (drop 0).
            posts[FrontRoomsTransitionKit.South] = BandPiece(sw, se, false, 3, ref band);
            posts[FrontRoomsTransitionKit.North] = BandPiece(nw, ne, false, 0, ref band);
            posts[FrontRoomsTransitionKit.West] = BandPiece(sw, nw, true, 3, ref band);
            posts[FrontRoomsTransitionKit.East] = BandPiece(se, ne, true, 0, ref band);
            reachA = reachB = 1;
            if (!band || !B0) return false;
            FrontRoomsTransitionKit.CornerReach(posts, role, out reachA, out reachB);
            return true;
        }

        // ---------- the map's hooks ----------

        /// <summary>A Level 0 cell of a renovated crossing: floor and ceiling in pieces (render only). False: the map builds its slabs.</summary>
        public bool TakesCell(GridCoord cell, Vector3 local, float height, int block, Material floor, Material ceiling)
        {
            if (Excluded(cell)) return false;
            SetCell(cell);
            var took = RenoKit.CrossingCell(reno, cell, local, height, block, floor, ceiling);
            if (took) StatCellsTaken++;
            return took;
        }

        /// <summary>An open edge (BuildEdge returns right after): a border portal, finished or unfinished.</summary>
        public void Open(GridCoord a, GridCoord b, Vector3 start, Vector3 along, float height)
        {
            edgeActive = false;
            if (Excluded(a) || Excluded(b)) return;
            if (view.theme(a) == view.theme(b)) return;
            SetCell(a);
            var te = Describe(EdgeKind.Open, a, b, start, along, height, MapGrid.CellSize * .5f, MapGrid.CellSize, height, 0f);
            FrameKit.Edge(te, this);
        }

        FrameKit.TransitionEdge Describe(EdgeKind kind, GridCoord a, GridCoord b, Vector3 start, Vector3 along, float height, float centre, float width, float openingTop, float sill)
        {
            var ax = along.x > .5f;
            var across = new Vector3(along.z, 0f, along.x);
            var c0 = CornerAt(ax ? new GridCoord(a.x, a.y + 1) : new GridCoord(a.x + 1, a.y));
            var c1 = CornerAt(new GridCoord(a.x + 1, a.y + 1));
            var ta = view.theme(a);
            var tb = view.theme(b);
            return new FrameKit.TransitionEdge
            {
                kind = kind, a = a, b = b, themeA = ta, themeB = tb,
                heightA = view.ceiling(a), heightB = view.ceiling(b), height = height,
                width = width, centre = centre, top = openingTop, sill = sill,
                start = start, along = along, across = across, officeWall = view.officeWall,
                c0 = c0, c1 = c1,
                renovated = ta != tb && view.renovated != null && view.renovated(a, b),
            };
        }

        /// <summary>
        /// Every non-open edge, before its wall pieces, with the opening exactly as BuildEdge built it
        /// (width 0 for a wall): the frame's pieces and base, the renovation's reveal, halo and props, and
        /// what the Piece calls of this edge must do.
        /// </summary>
        public void Edge(EdgeKind kind, GridCoord a, GridCoord b, Vector3 start, Vector3 along, float height,
            float centre, float width, float openingTop, float sill, int sideA, int sideB, Material wallA, Material wallB,
            bool mayExtendStart, bool mayExtendEnd)
        {
            edgeActive = false;
            if (kind == EdgeKind.Open || Excluded(a) || Excluded(b)) return;
            SetCell(a);
            var te = Describe(kind, a, b, start, along, height, centre, width, openingTop, sill);
            if (te.Border) FrameKit.Edge(te, this);
            FrameKit.Base(te, this);

            alongX = along.x > .5f;
            lo = centre - width * .5f;
            hi = centre + width * .5f;
            top = openingTop;
            e = new RenoKit.Edge
            {
                kind = kind, a = a, b = b, alongX = alongX, start = start, along = along, across = te.across, height = height,
                lo = lo, hi = hi, top = openingTop, sill = sill, wallA = wallA, wallB = wallB, blockA = sideA, blockB = sideB,
                mayExtendStart = mayExtendStart, mayExtendEnd = mayExtendEnd,
            };
            faceA = RenoKit.FaceOf(reno, a, b, alongX, false);
            faceB = RenoKit.FaceOf(reno, a, b, alongX, true);
            haloA = kind != EdgeKind.Wall && RenoKit.Halo(reno, e, faceA, false, out hwA);
            haloB = kind != EdgeKind.Wall && RenoKit.Halo(reno, e, faceB, true, out hwB);
            bandA = faceA.level0 && (faceA.band || haloA);
            bandB = faceB.level0 && (faceB.band || haloB);
            if (kind == EdgeKind.Arch) RenoKit.ArchReveal(reno, e);
            RenoKit.EdgeExtras(reno, e, te.heightA, te.heightB);

            beadArch = te.Border && kind == EdgeKind.Arch && !te.renovated;
            capStart = FrameKit.EndCapAt(te.c0);
            capEnd = FrameKit.EndCapAt(te.c1);
            bandStart = BandPost(alongX ? new GridCoord(a.x, a.y + 1) : new GridCoord(a.x + 1, a.y), alongX ? FrontRoomsTransitionKit.East : FrontRoomsTransitionKit.North, out rsA, out rsB);
            bandEnd = BandPost(new GridCoord(a.x + 1, a.y + 1), alongX ? FrontRoomsTransitionKit.West : FrontRoomsTransitionKit.South, out reA, out reB);
            edgeActive = true;
        }

        /// <summary>
        /// One wall piece of the current edge. fA..tB are the skins' extents as the map's B0 made them.
        /// True: the kit has built the render geometry (the map adds only its collision boxes, as today).
        /// </summary>
        public bool Piece(float from, float to, float bottom, float top_, float fA, float tA, float fB, float tB, int blockIndex)
        {
            if (!edgeActive) return false;
            const float L = MapGrid.CellSize, H = ModuleUnits.WallHalf;
            float iF = 0f, iT = 0f, raise = 0f;
            var opening = e.kind != EdgeKind.Wall;
            var atOpeningEnd = opening && bottom == 0f && to == lo;
            var atOpeningStart = opening && bottom == 0f && from == hi;
            if (beadArch)
            {
                if (atOpeningEnd) iT = FrameKit.BeadInset;
                else if (atOpeningStart) iF = FrameKit.BeadInset;
                else if (from == lo && to == hi && bottom == top) { iF = -FrameKit.BeadInset; iT = -FrameKit.BeadInset; raise = FrameKit.BeadInset; }
            }
            var startPost = from <= 0f && e.mayExtendStart;
            var endPost = to >= L && e.mayExtendEnd;
            if (startPost && capStart) iF += FrameKit.BeadInset;
            if (endPost && capEnd) iT += FrameKit.BeadInset;
            var reachStart = startPost && bandStart;
            var reachEnd = endPost && bandEnd;
            if (iF == 0f && iT == 0f && raise == 0f && !bandA && !bandB && !reachStart && !reachEnd) return false;

            // Render extents: the band posts' reach replaces B0's, then the render-only insets.
            if (reachStart) { fA = from - H * rsA; fB = from - H * rsB; }
            if (reachEnd) { tA = to + H * reA; tB = to + H * reB; }
            fA += iF; fB += iF; tA -= iT; tB -= iT;
            var y0 = bottom + raise;
            if (top_ - y0 < .02f) return true;
            StatPiecesTaken++;
            var single = e.wallA == e.wallB && e.blockA == e.blockB && fA == fB && tA == tB && !bandA && !bandB;
            if (single)
            {
                if (tA - fA < .02f) return true;
                var centre = e.start + e.along * ((fA + tA) * .5f) + Vector3.up * ((y0 + top_) * .5f);
                Box(blockIndex, e.wallA, centre, Abs(e.along * (tA - fA) + e.across * ModuleUnits.WallThickness + Vector3.up * (top_ - y0)), view.wallpaperRepeat);
                return true;
            }
            Skin(false, fA, tA, y0, top_, atOpeningStart, atOpeningEnd, from <= 1e-4f, to >= L - 1e-4f);
            Skin(true, fB, tB, y0, top_, atOpeningStart, atOpeningEnd, from <= 1e-4f, to >= L - 1e-4f);
            return true;
        }

        void Skin(bool sideB, float f, float t, float bottom, float top_, bool atOpeningStart, bool atOpeningEnd, bool startAtPost, bool endAtPost)
        {
            if (t - f < .02f) return;
            var face = sideB ? faceB : faceA;
            var band = sideB ? bandB : bandA;
            var paper = sideB ? e.wallB : e.wallA;
            var block = sideB ? e.blockB : e.blockA;
            if (band)
            {
                RenoKit.Skin(reno, e, face, sideB, paper, block, f, t, bottom, top_, sideB ? haloB : haloA, sideB ? hwB : hwA, atOpeningStart, atOpeningEnd, startAtPost, endAtPost);
                return;
            }
            var side = sideB ? 1f : -1f;
            var centre = e.start + e.along * ((f + t) * .5f) + Vector3.up * ((bottom + top_) * .5f) + e.across * (ModuleUnits.WallThickness * .25f * side);
            Box(block, paper, centre, Abs(e.along * (t - f) + e.across * (ModuleUnits.WallThickness * .5f) + Vector3.up * (top_ - bottom)), view.wallpaperRepeat);
        }

        /// <summary>The map's collision box for a piece the kit renders (exactly what the map's Solid adds).</summary>
        public void Collide(Vector3 centre, Vector3 size) => collide?.Invoke(centre, size);

        /// <summary>Once per lattice point (the cell's south-west corner): free posts, end caps, corner guards.</summary>
        public void Corner(GridCoord cell)
        {
            SetCell(cell);
            var c = CornerAt(cell);
            if (c.special) return;
            FrameKit.Corner(c, this, view.officeWall);
        }

        // ---------- FrameKit.ISink ----------

        public TransitionMesh Mesh(Material material, float ceiling, bool castShadows)
        {
            if (material == view.officeWall && castShadows)
            {
                var b = blockOf(ci, cj, ceiling);
                if (!officeBlocks.TryGetValue(b, out var om)) officeBlocks[b] = om = new TransitionMesh();
                return om;
            }
            return Loose(Variant(material, root.position.y + ceiling), castShadows);
        }

        public TransitionMesh BaseMesh(float ceiling)
        {
            if (reduced && view.trim != null)
            {
                var b = blockOf(ci, cj, ceiling);
                if (!trimBlocks.TryGetValue(b, out var tm)) trimBlocks[b] = tm = new TransitionMesh();
                return tm;
            }
            return Loose(FrontRoomsSurfaces.CoveBase ?? view.trim, false);
        }

        public void Collider(Vector3 centre, Vector3 size)
        {
            colliders.Add(new Bounds(centre, size));
            StatPosts++;
        }

        TransitionMesh Loose(Material m, bool shadows)
        {
            var k = new LooseKey(m, shadows);
            if (!loose.TryGetValue(k, out var mesh)) loose[k] = mesh = new TransitionMesh();
            return mesh;
        }

        // ---------- RenoKit.IOut ----------

        bool BlockMaterial(Material m) => m != null && (m == view.level0Wall || m == view.officeWall || m == view.level0Floor || m == view.officeFloor || m == view.level0Ceiling || m == view.officeCeiling);

        float WorldCeiling(int block) => root.position.y + (block >= 0 && blockCeiling != null ? blockCeiling(block) : ModuleUnits.StandardCeiling);

        public void Box(int block, Material mat, Vector3 centre, Vector3 size, float repeat = 1.2f)
        {
            if (size.x < 1e-4f || size.y < 1e-4f || size.z < 1e-4f || mat == null) return;
            if (block >= 0 && BlockMaterial(mat))
            {
                blockBoxes.Add(new BoxCmd { block = block, mat = mat, centre = centre, size = size, repeat = repeat });
                return;
            }
            var m = Loose(reno.mats.Grimed(mat) ? Variant(mat, WorldCeiling(block)) : mat, true);
            var h = size * .5f;
            void F(Vector3 n, Vector3 u, Vector3 v, float hu, float hv)
            {
                var c = centre + Vector3.Scale(n, h);
                m.Quad(c - u * hu - v * hv, c + u * hu - v * hv, c + u * hu + v * hv, c - u * hu + v * hv, n);
            }
            F(Vector3.right, Vector3.forward, Vector3.up, h.z, h.y);
            F(Vector3.left, Vector3.forward, Vector3.up, h.z, h.y);
            F(Vector3.up, Vector3.right, Vector3.forward, h.x, h.z);
            F(Vector3.down, Vector3.right, Vector3.forward, h.x, h.z);
            F(Vector3.forward, Vector3.right, Vector3.up, h.x, h.y);
            F(Vector3.back, Vector3.right, Vector3.up, h.x, h.y);
        }

        public RenoKit.RawCmd Raw(int block, Material mat)
        {
            if (block >= 0 && BlockMaterial(mat))
            {
                var bk = new BlockMatKey(block, mat);
                if (!blockRaws.TryGetValue(bk, out var r)) blockRaws[bk] = r = new RenoKit.RawCmd { block = block, mat = mat };
                return r;
            }
            var vm = reno.mats.Grimed(mat) ? Variant(mat, WorldCeiling(block)) : mat;
            var lk = new LooseKey(vm, true);
            if (!looseRaws.TryGetValue(lk, out var lr)) looseRaws[lk] = lr = new RenoKit.RawCmd { block = -1, mat = vm };
            return lr;
        }

        public void Prop(string asset, Vector3 position, Quaternion rotation, Rect rect, float height)
        {
            props.Add(new PropCmd { asset = asset, position = position, rotation = rotation });
            keepOuts.Add(new FrontRoomsWallFixtures.KeepOut { xz = rect, y0 = 0f, y1 = height + .02f });
        }

        // ---------- finish ----------

        /// <summary>
        /// After the cell loop, before the map's block renderers: merge the block pieces into the map's builders,
        /// make the kit renderers and props, hand the wall fixtures their keep-outs. Returns the free-post
        /// collider boxes (chunk-local centre and size) for the map's collision.
        /// </summary>
        public List<Bounds> Finish(BoxFn box, AppendFn append, List<Mesh> ownedMeshes)
        {
            foreach (var b in blockBoxes) box(b.block, b.mat, b.centre, b.size, b.repeat);
            foreach (var r in blockRaws.Values) if (r.v.Count > 0) append(r.block, r.mat, r.v, r.n, r.uv, r.t);
            foreach (var pair in officeBlocks) if (pair.Value.vertices.Count > 0) append(pair.Key, view.officeWall, pair.Value.vertices, pair.Value.normals, pair.Value.uvs, pair.Value.triangles);
            if (view.trim != null)
                foreach (var pair in trimBlocks) if (pair.Value.vertices.Count > 0) append(pair.Key, view.trim, pair.Value.vertices, pair.Value.normals, pair.Value.uvs, pair.Value.triangles);
            // Loose raw meshes join the loose renderer of the same material.
            foreach (var pair in looseRaws)
            {
                var r = pair.Value;
                if (r.v.Count == 0) continue;
                var m = Loose(pair.Key.mat, true);
                var o = m.vertices.Count;
                m.vertices.AddRange(r.v);
                m.normals.AddRange(r.n);
                m.uvs.AddRange(r.uv);
                foreach (var t in r.t) m.triangles.Add(o + t);
            }
            GameObject kitRoot = null;
            foreach (var pair in loose)
            {
                var mesh = pair.Value;
                var mat = pair.Key.mat;
                if (mesh.vertices.Count == 0 || mat == null) continue;
                if (kitRoot == null)
                {
                    kitRoot = new GameObject("Transitions");
                    kitRoot.transform.SetParent(root, false);
                }
                var go = new GameObject("Transition " + mat.name + (pair.Key.shadows ? "" : " (no shadow)"));
                go.transform.SetParent(kitRoot.transform, false);
                var um = mesh.ToMesh("Chunk " + coord + " transition " + mat.name, mat.IsKeywordEnabled("_FR_MESH_UV"));
                go.AddComponent<MeshFilter>().sharedMesh = um;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = pair.Key.shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
                ownedMeshes?.Add(um);
                StatRenderers++;
                StatTris += mesh.TriangleCount;
            }
            if (props.Count > 0)
            {
                if (kitRoot == null)
                {
                    kitRoot = new GameObject("Transitions");
                    kitRoot.transform.SetParent(root, false);
                }
                foreach (var p in props)
                    if (FrontRoomsKitLibrary.Spawn(p.asset, kitRoot.transform, p.position, p.rotation, null, false, "Renovation prop " + p.asset, false) != null) StatProps++;
            }
            FrontRoomsWallFixtures.PendingKeepOuts.Clear();
            FrontRoomsWallFixtures.PendingKeepOuts.AddRange(keepOuts);
            return colliders;
        }

        static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
    }
}
