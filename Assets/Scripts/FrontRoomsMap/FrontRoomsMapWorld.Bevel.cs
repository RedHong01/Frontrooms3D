using System;
using System.Collections.Generic;
using FrontRooms.Map;
using UnityEngine;

// Surface detail package 8, the map's half (research/surface_detail/20_design.md section 5.1 row 8 and P6; map line,
// surface_bevels B1, 2026-11-05): the knife edges the map builds are rounded, render only. Each rounded arris moves its
// render box back by the round's radius (a piece shortened, a header raised and widened, a sill lowered and widened) and a
// lining takes its place: a quarter round, and the reveal, soffit, sill or end narrowed by the radius.
// Rounds that meet at a corner of an opening (or at a corner of one box) are mitred on that corner's 45 degree plane.
// Shading (surface_bevels_fix, 2026-11-06): each half of a round carries its own face's normal, so a round is lit exactly as
// its two faces and is never darker than the darker one. A normal between the faces' (B1's rounds) faced neither of the lamps
// that light an outside corner's two faces one each (a jamb's reveal by the next room's lamps, its wall face by its own
// room's): a dark core on every rounded corner, at every distance (the two-lamp valley; integration/surface_bevels_fix/
// diagnosis.txt). The round keeps its silhouette and the step in shading moves from the arris to its 45 degree row.
// Collision keeps every box as it was, so colliders, nav, plan hashes, valances, openings, signs and every seed's layout
// cannot move (no hash or random number is read here).
// Rounded here: the shell's arch jambs and soffits, free wall ends, the outside corner of an L and its columns, on every
// face whose material is not a _FR_DETAIL surface (the papered and painted walls); in the pool pass every tiled outside
// corner (skin ends at jambs, free ends, Ls, the clear glass wall's opening), the round arch's curved arrises over a smooth
// intrados, pillars, step noses under a swept nosing, the basin's edge, the weir's lip, the course's top and ends (the
// bullnose takes the course's colour where the course meets it) and the coping's nose.
// Not here: _FR_DETAIL surfaces (the Office's and the stations' painted board, the cove base, the casings: package B2, once
// FrontRooms/Surface's FRDetailWallId takes the snapped normal); door and window jambs and heads (the casings and the window
// kit cover them); the transition kit's border work, the title stream's rooms, the stairwell (its frames and the dresser)
// and scene 4's own kits, which are theirs (scene 4's kits can call FrontRoomsBevel below).
// UVs: the plain path (paper, paint) frames its UVs from the normal, so every renderer that carries a round takes
// _FR_PlanarSnap 1 in the block AddRenderer sets (each half of a round continues its own face's print and wear); the
// mesh-UV path (the pool tile) takes UV0 per half by its own face's projection, the 45 degree row written twice, held near
// its face's tangent line across the round (FrontRoomsBevel.HoldKeep): the round shows its face's glaze, not the edge zone
// of a tile that ends at the arris (a pillar's corner on the tile grid: a dark joint along the arris before).
// Switch: FrontRoomsSurfaceDetail.Today = today's box edges, byte for byte (the old Box calls, the old order, no snap); read
// when a chunk builds; a change rebuilds every built chunk (PollSurfaceDetail). WebGL players keep box edges.
public sealed partial class FrontRoomsMapWorld
{
    /// <summary>Papered and painted drywall: the corner bead's round (20_design.md 5.1 row 8: r 1-3 mm).</summary>
    public const float BevelWallRadius = .003f;
    public const int BevelWallSegments = 4;
    /// <summary>The pool tile's surface bullnose (P6: r 6-10 mm; 3/8 in) on tiled outside corners, pillars, steps, the basin's edge and the lip.</summary>
    public const float BevelTileRadius = .0095f;
    public const int BevelTileSegments = 6;
    /// <summary>The aqua course's top and ends: its whole 4 mm proud face.</summary>
    public const float BevelCourseRadius = .004f;
    public const int BevelCourseSegments = 4;
    /// <summary>The coping plate's nose: a half round over its 4 mm.</summary>
    public const float BevelCopingRadius = .002f;
    public const int BevelCopingSegments = 2;

    /// <summary>
    /// True: chunks built now round their knife edges. False under FrontRoomsSurfaceDetail.Today (today's box edges, byte for
    /// byte) and in WebGL players (their own track keeps box edges).
    /// </summary>
    public static bool BevelsOn
    {
        get
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return false;
#else
            return !FrontRoomsSurfaceDetail.Today;
#endif
        }
    }

    /// <summary>
    /// Raised after a change of FrontRoomsSurfaceDetail.Today rebuilt every built chunk (bevels and the pool's wet band follow
    /// it). Anything that holds on to a chunk's renderers (scene 4's water preview: its Submerged bodies) rebuilds from them.
    /// </summary>
    public event Action SurfaceDetailRebuilt;

    static readonly int PlanarSnapId = Shader.PropertyToID("_FR_PlanarSnap");

    // The chunk BuildInto is building: it rounds its edges (BevelsOn when it began), and its collision builder, where a rounded
    // piece writes its box exactly as Solid would.
    bool bevelChunk;
    MeshBuilder bevelCollision;
    // FrontRoomsSurfaceDetail.Today as the built chunks were made (bevels, the wet band); known after the first build.
    bool bevelsBuiltToday, bevelsBuiltKnown;

    void BeginBevelChunk(MeshBuilder collision)
    {
        bevelChunk = BevelsOn;
        bevelCollision = collision;
        bevelsBuiltToday = FrontRoomsSurfaceDetail.Today;
        bevelsBuiltKnown = true;
    }

    // Tools only (scene 4's \ key, a test): Today changed since the built chunks were made, so all of them are built again
    // (a hitch of their build times), and SurfaceDetailRebuilt tells those who hold their renderers.
    void PollSurfaceDetail()
    {
        if (!bevelsBuiltKnown || built.Count == 0 || FrontRoomsSurfaceDetail.Today == bevelsBuiltToday) return;
        var coords = new List<GridCoord>(built.Keys);
        foreach (var c in coords) RebuildChunk(c);
        bevelsBuiltToday = FrontRoomsSurfaceDetail.Today;
        var handlers = SurfaceDetailRebuilt;
        if (handlers == null) return;
        foreach (Action h in handlers.GetInvocationList())
        {
            try { h(); }
            catch (Exception e) { Debug.LogException(e); }
        }
    }

    // A face the rounds may touch in this package: anything but a _FR_DETAIL surface (its per-wall hash, FRDetailWallId, reads
    // the unsnapped normal, so on a round its id would change across the round: package B2, after the shader's snap).
    public static bool BevelMaterial(Material m) => m != null && !m.IsKeywordEnabled("_FR_DETAIL");

    // A face whose material frames its UVs from the normal (FrontRooms/Surface's plain path: the paper's print, paint, block)
    // takes a faceted round: one flat facet per band. A smooth round's normal, extrapolated by the 2 x 2 quad's helper lanes
    // past 45 degrees (on a sub-pixel round, far past), flips the snapped frame and the quad takes the print's coarsest mip: a
    // dark seam on the round and a crawling sawtooth on far corners (surface_bevels build log, LOOK 1). A facet's normal is the
    // same over its triangle, so its frame and its mip hold. The mesh-UV tile and plain URP materials keep smooth rounds.
    public static bool BevelFaceted(Material m) => m != null && m.shader != null && m.shader.name == "FrontRooms/Surface" && !m.IsKeywordEnabled("_FR_MESH_UV");

    // A faceted round's facet count: about 0.8 mm of arc each (3 mm: 6, 9.5 mm: 12 at most), even.
    public static int BevelFacets(float r, int segments)
    {
        var n = Mathf.CeilToInt(r * Mathf.PI * .5f / .0008f);
        n = Mathf.Clamp(Mathf.Max(n, segments), 4, 12);
        return (n & 1) == 1 ? n + 1 : n;
    }

    // ---------- Into a MeshBuilder ----------

    static void BevelStripInto(MeshBuilder m, Material material, Vector3 k0, Vector3 k1, Vector3 nA, Vector3 nB, float r, int segments, Vector3 cut0, Vector3 cut1, Vector3 origin, float repeat)
    {
        if (m == null || r <= 0f) return;
        var flat = BevelFaceted(material);
        if (flat) segments = BevelFacets(r, segments);
        if (FrontRoomsBevel.Strip(m.vertices, m.normals, m.uvs, m.triangles, k0, k1, nA, nB, r, segments, cut0, cut1, origin, repeat, flat) > 0) m.planarSnap = true;
    }

    static void BevelQuadInto(MeshBuilder m, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 normal, Vector3 origin, float repeat)
    {
        if (m == null) return;
        FrontRoomsBevel.Quad(m.vertices, m.normals, m.uvs, m.triangles, p0, p1, p2, p3, normal, origin, repeat);
    }

    // A box with the given edges rounded (FrontRoomsBevel.Edge bits); no edge or no radius: MeshBuilder.Box exactly.
    static void BevelBoxInto(MeshBuilder m, Material material, Vector3 center, Vector3 size, int edges, float r, int segments, Vector3 origin, float repeat)
    {
        if (m == null) return;
        if (edges == 0 || r <= 0f) { m.Box(center, size, origin, repeat); return; }
        var flat = BevelFaceted(material);
        if (flat) segments = BevelFacets(r, segments);
        FrontRoomsBevel.Box(m.vertices, m.normals, m.uvs, m.triangles, center, size, edges, r, segments, origin, repeat, flat);
        m.planarSnap = true;
    }

    static readonly int BevelColumnEdges =
        FrontRoomsBevel.Edge(Vector3.right, Vector3.forward) | FrontRoomsBevel.Edge(Vector3.right, Vector3.back) |
        FrontRoomsBevel.Edge(Vector3.left, Vector3.forward) | FrontRoomsBevel.Edge(Vector3.left, Vector3.back);

    // ---------- Lattice points ----------

    const int BevelEndNone = 0, BevelEndFree = 1, BevelEndL = 2;

    /// <summary>
    /// How the piece in <paramref name="role"/> ends at lattice point <paramref name="k"/>, read from postPieces right after
    /// CornerReach filled them for it: a free end (both faces' arrises round), the outside corner of an L (one arris, on the
    /// face <paramref name="outside"/>: -1 the a face, +1 the b face; <paramref name="owner"/>: this piece draws the round,
    /// the east-west arm as the kit's tie rule, while both arms stop short of it), or nothing (a straight run, a T or X, a
    /// post of mixed finishes, which is the kit's, or a point that touches the start area or X).
    /// </summary>
    int BevelEndAt(GridCoord k, int role, bool alongX, out float outside, out bool owner)
    {
        outside = 0f;
        owner = false;
        var p = postPieces;
        if (p == null || p.Length != 4 || !p[role].present) return BevelEndNone;
        long first = 0;
        var any = false;
        for (var d = 0; d < 4; d++)
        {
            if (!p[d].present) continue;
            if (!any) { first = p[d].finishA; any = true; }
            if (p[d].finishA != first || p[d].finishB != first) return BevelEndNone;
        }
        if (BevelOffLattice(new GridCoord(k.x - 1, k.y - 1)) || BevelOffLattice(new GridCoord(k.x, k.y - 1)) || BevelOffLattice(new GridCoord(k.x - 1, k.y)) || BevelOffLattice(k))
            return BevelEndNone;
        var others = 0;
        for (var d = 0; d < 4; d++) if (d != role && p[d].present) others++;
        if (others == 0) return BevelEndFree;
        if (others != 1 || p[role ^ 1].present) return BevelEndNone;
        // the L's other arm lies on the inside: +across is north for an east-west piece, east for a north-south one
        var plus = alongX ? p[FrontRoomsTransitionKit.North].present : p[FrontRoomsTransitionKit.East].present;
        outside = plus ? -1f : 1f;
        owner = alongX;
        return BevelEndL;
    }

    // The start area and X (the title stream's rooms and the stairwell stand there): no round at a post that touches them.
    bool BevelOffLattice(GridCoord c) => InStartArea(c) || InExitArea(c);

    // ---------- The shell's edges (BuildEdge) ----------

    // One render box BuildEdge's Piece made while its edge rounds (the collision box went in at once): side 0 the one box of
    // the whole wall, -1 / +1 the a / b skin; role 1 left of the opening (its end is a jamb), 2 right of it, 3 over it.
    sealed class BevelPiece
    {
        public int block;
        public Material material;
        public float from, to, bottom, top, side;
        public int role;
        public bool atStart, atEnd;
    }

    /// <summary>
    /// The rounds of one shell edge, after its pieces: every render box BuildEdge held back is drawn, trimmed for its rounds
    /// with their linings when the transition kit took none of the edge's pieces, else exactly as built. Lattice ends: a free
    /// end (both faces) or an L (the east-west arm draws it); an arch: jambs and soffit on each face, mitred at the corners.
    /// </summary>
    void FinishBevelEdge(List<BevelPiece> pieces, bool kitTook, EdgeKind kind, GridCoord a, GridCoord b, Vector3 start, Vector3 along, Vector3 across, float height,
        float c, float width, float openingTop, Material wallA, Material wallB, int sideA, int sideB, BuilderFn get, Vector3 origin,
        int endStart, float outStart, bool ownStart, int endEnd, float outEnd, bool ownEnd)
    {
        if (pieces == null) return;
        // a pool cell's face (a door's or a window's wall the map builds in the pool family) takes the pool pass's bullnose
        var rA = BevelMaterial(wallA) ? (IsPoolCell(a) ? BevelTileRadius : BevelWallRadius) : 0f;
        var rB = BevelMaterial(wallB) ? (IsPoolCell(b) ? BevelTileRadius : BevelWallRadius) : 0f;
        var segA = IsPoolCell(a) ? BevelTileSegments : BevelWallSegments;
        var segB = IsPoolCell(b) ? BevelTileSegments : BevelWallSegments;
        var trim = !kitTook && (rA > 0f || rB > 0f);
        var half = WallThickness * .5f;
        var length = MapGrid.CellSize;
        var arch = kind == EdgeKind.Arch;
        var J0 = c - width * .5f;
        var J1 = c + width * .5f;
        // an L rounds only when its outside face may
        var rLs = endStart == BevelEndL ? (outStart < 0f ? rA : rB) : 0f;
        var rLe = endEnd == BevelEndL ? (outEnd < 0f ? rA : rB) : 0f;
        bool startA = false, startB = false, endA = false, endB = false, leftA = false, leftB = false, rightA = false, rightB = false, headA = false, headB = false;
        foreach (var p in pieces)
        {
            var f = p.from;
            var t = p.to;
            var y0 = p.bottom;
            var r = p.side > 0f ? rB : rA;
            if (trim && r > 0f)
            {
                if (p.atStart && endStart == BevelEndFree) f += r;
                if (p.atEnd && endEnd == BevelEndFree) t -= r;
                if (arch && p.role == 1) t -= r;
                if (arch && p.role == 2) f += r;
                if (arch && p.role == 3) { f -= r; t += r; y0 += r; }
            }
            if (trim)
            {
                // an L: every box of both arms stops short of the outside round
                if (p.atStart && endStart == BevelEndL) f += rLs;
                if (p.atEnd && endEnd == BevelEndL) t -= rLe;
            }
            var center = start + along * ((f + t) * .5f) + Vector3.up * ((y0 + p.top) * .5f);
            Vector3 size;
            if (p.side == 0f) size = Abs(along * (t - f) + across * WallThickness + Vector3.up * (p.top - y0));
            else
            {
                center += across * (WallThickness * .25f * p.side);
                size = Abs(along * (t - f) + across * (WallThickness * .5f) + Vector3.up * (p.top - y0));
            }
            if (t - f > 1e-4f && p.top - y0 > 1e-4f) get(p.block, p.material).Box(center, size, origin, WallpaperRepeat);
            var onA = p.side <= 0f;
            var onB = p.side >= 0f;
            if (p.atStart) { startA |= onA; startB |= onB; }
            if (p.atEnd) { endA |= onA; endB |= onB; }
            if (p.role == 1) { leftA |= onA; leftB |= onB; }
            if (p.role == 2) { rightA |= onA; rightB |= onB; }
            if (p.role == 3) { headA |= onA; headB |= onB; }
        }
        if (!trim) return;

        for (var s = -1; s <= 1; s += 2)
        {
            var sign = (float)s;
            var r = s < 0 ? rA : rB;
            var material = s < 0 ? wallA : wallB;
            var block = s < 0 ? sideA : sideB;
            var seg = s < 0 ? segA : segB;
            if (r <= 0f) continue;   // this face keeps its arrises (an L's outside round is this face's own)
            var m = get(block, material);
            var face = across * sign;
            var faceOff = across * (half * sign);
            var inset = across * ((half - r) * sign);
            // lattice ends
            for (var e = 0; e < 2; e++)
            {
                var atEnd = e == 1;
                var kindAt = atEnd ? endEnd : endStart;
                if (kindAt == BevelEndNone || !(atEnd ? (s < 0 ? endA : endB) : (s < 0 ? startA : startB))) continue;
                var outDir = atEnd ? along : -along;
                var plane = start + along * (atEnd ? length + half : -half);
                if (kindAt == BevelEndFree && r > 0f)
                {
                    BevelStripInto(m, material, plane + faceOff, plane + faceOff + Vector3.up * height, face, outDir, r, seg, Vector3.zero, Vector3.zero, origin, WallpaperRepeat);
                    BevelQuadInto(m, plane + inset, plane + inset + Vector3.up * height, plane + Vector3.up * height, plane, outDir, origin, WallpaperRepeat);
                }
                else if (kindAt == BevelEndL && (atEnd ? ownEnd : ownStart) && (atEnd ? outEnd : outStart) == sign)
                {
                    var rl = atEnd ? rLe : rLs;
                    if (rl > 0f) BevelStripInto(m, material, plane + faceOff, plane + faceOff + Vector3.up * height, face, outDir, rl, seg, Vector3.zero, Vector3.zero, origin, WallpaperRepeat);
                }
            }
            if (!arch || r <= 0f) continue;
            // the arch: jambs from the floor to the soffit and the soffit between them, mitred where they meet
            var H = openingTop;
            var left = s < 0 ? leftA : leftB;
            var right = s < 0 ? rightA : rightB;
            var head = s < 0 ? headA : headB;
            var kL = start + along * J0 + faceOff;
            var kR = start + along * J1 + faceOff;
            var up = Vector3.up;
            var mL = head ? FrontRoomsBevel.Mitre(up, along) : Vector3.zero;
            var mR = head ? FrontRoomsBevel.Mitre(along, -up) : Vector3.zero;
            if (left)
            {
                BevelStripInto(m, material, kL, kL + up * H, face, along, r, seg, Vector3.zero, mL, origin, WallpaperRepeat);
                var p0 = start + along * J0;
                BevelQuadInto(m, p0 + inset, p0 + inset + up * H, p0 + up * H, p0, along, origin, WallpaperRepeat);
            }
            if (right)
            {
                BevelStripInto(m, material, kR, kR + up * H, face, -along, r, seg, Vector3.zero, mR, origin, WallpaperRepeat);
                var p1 = start + along * J1;
                BevelQuadInto(m, p1 + inset, p1 + inset + up * H, p1 + up * H, p1, -along, origin, WallpaperRepeat);
            }
            if (head)
            {
                BevelStripInto(m, material, kL + up * H, kR + up * H, face, -up, r, seg, left ? mL : Vector3.zero, right ? mR : Vector3.zero, origin, WallpaperRepeat);
                var q0 = start + along * J0 + up * H;
                var q1 = start + along * J1 + up * H;
                BevelQuadInto(m, q0 + inset, q1 + inset, q1, q0, -up, origin, WallpaperRepeat);
            }
        }
    }
}

/// <summary>
/// Rounded edges for render meshes made of boxes (surface detail package 8; the map's own use is FrontRoomsMapWorld.Bevel.cs):
/// quarter rounds, rounded boxes and swept rounds, written into plain vertex / normal / UV0 / index lists with the map's UV
/// rule (world metres, uvOffset added, projected on each face's main axis, divided by repeat). Radii in metres; segments are
/// made even, so the 45 degree row is a vertex row and each half of a round takes its own face's projection. Each half of a
/// round also takes its own face's normal (lit as that face: never darker than the darker face) and its UV0 held near that
/// face's tangent line across the round (a mesh-UV material shows the face's field there). A material that frames its UVs
/// from the normal (FrontRooms/Surface's plain path) needs _FR_PlanarSnap 1 on the renderer.
/// Scene 4's kits (their half of package 8) may call these for their own geometry.
/// </summary>
public static class FrontRoomsBevel
{
    /// <summary>
    /// How much of a round's own UV0 travel across the round is kept (surface_bevels_fix, F2): each half's UV0 is taken at its
    /// point moved back towards its face's tangent line, all but this fraction of the way, so a mesh-UV tile shows the glaze a
    /// radius in from the arris over the round instead of the edge zone (joint and cushion) of a tile that ends at the arris.
    /// Not zero, so a round's triangles keep a UV area and RecalculateTangents gives them their face's own tangent frame (the
    /// tile reads its normal map in the mesh's tangent space). The plain path reads neither UV0 nor the tangents.
    /// </summary>
    public const float HoldKeep = 1f / 16f;

    /// <summary>The map's UV for a point: world metres on the normal's main axis (MeshBuilder.Box, PoolQuad).</summary>
    public static Vector2 PlanarUV(Vector3 p, Vector3 normal, Vector3 uvOffset, float repeat)
    {
        var w = p + uvOffset;
        float ax = Mathf.Abs(normal.x), ay = Mathf.Abs(normal.y), az = Mathf.Abs(normal.z);
        var uv = ax >= ay && ax >= az ? new Vector2(w.z, w.y) : ay >= az ? new Vector2(w.x, w.z) : new Vector2(w.x, w.y);
        return uv / (repeat > 0f ? repeat : 1f);
    }

    /// <summary>The plane where two rounds meet at a corner: <paramref name="dIn"/> the first one's direction into the corner, <paramref name="dOut"/> the next one's out of it.</summary>
    public static Vector3 Mitre(Vector3 dIn, Vector3 dOut) => (dIn + dOut).normalized;

    /// <summary>A flat quad p0..p3 facing <paramref name="normal"/> (turned round if wound the other way).</summary>
    public static void Quad(List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3,
        Vector3 normal, Vector3 uvOffset, float repeat)
    {
        if (Vector3.Dot(Vector3.Cross(p1 - p0, p2 - p0), normal) < 0f) { var s = p1; p1 = p3; p3 = s; }
        var i = v.Count;
        Add(v, n, uv, p0, normal, normal, uvOffset, repeat);
        Add(v, n, uv, p1, normal, normal, uvOffset, repeat);
        Add(v, n, uv, p2, normal, normal, uvOffset, repeat);
        Add(v, n, uv, p3, normal, normal, uvOffset, repeat);
        t.Add(i); t.Add(i + 1); t.Add(i + 2);
        t.Add(i); t.Add(i + 2); t.Add(i + 3);
    }

    /// <summary>A quad p0..p3 with a normal per corner (a smooth surface in steps), wound to face them; UV0 by <paramref name="uvNormal"/>'s projection.</summary>
    public static void QuadNormals(List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3,
        Vector3 n0, Vector3 n1, Vector3 n2, Vector3 n3, Vector3 uvNormal, Vector3 uvOffset, float repeat)
    {
        var i = v.Count;
        Add(v, n, uv, p0, n0, uvNormal, uvOffset, repeat);
        Add(v, n, uv, p1, n1, uvNormal, uvOffset, repeat);
        Add(v, n, uv, p2, n2, uvNormal, uvOffset, repeat);
        Add(v, n, uv, p3, n3, uvNormal, uvOffset, repeat);
        Tri(v, n, t, i, i + 1, i + 2);
        Tri(v, n, t, i, i + 2, i + 3);
    }

    /// <summary>
    /// A ribbon between two matching polylines <paramref name="a"/> and <paramref name="b"/> (a profile swept straight), a
    /// normal per profile point (<paramref name="normals"/>), each step's UV0 by its mean normal's projection. Returns the
    /// triangles added.
    /// </summary>
    public static int Ribbon(List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> t, IList<Vector3> a, IList<Vector3> b, IList<Vector3> normals,
        Vector3 uvOffset, float repeat)
    {
        if (a == null || b == null || normals == null || a.Count < 2 || b.Count != a.Count || normals.Count != a.Count) return 0;
        var tris = 0;
        for (var k = 0; k + 1 < a.Count; k++)
        {
            var uvNormal = (normals[k] + normals[k + 1]).normalized;
            var i = v.Count;
            Add(v, n, uv, a[k], normals[k], uvNormal, uvOffset, repeat);
            Add(v, n, uv, a[k + 1], normals[k + 1], uvNormal, uvOffset, repeat);
            Add(v, n, uv, b[k + 1], normals[k + 1], uvNormal, uvOffset, repeat);
            Add(v, n, uv, b[k], normals[k], uvNormal, uvOffset, repeat);
            tris += Tri(v, n, t, i, i + 1, i + 2) + Tri(v, n, t, i, i + 2, i + 3);
        }
        return tris;
    }

    /// <summary>
    /// A quarter round of <paramref name="radius"/> replacing the sharp arris from <paramref name="k0"/> to <paramref name="k1"/>
    /// between the faces with outward normals <paramref name="nA"/> and <paramref name="nB"/>: it leaves face A a radius in
    /// from the arris and meets face B a radius in. Each end is cut by a plane through its corner point with normal
    /// <paramref name="cut0"/> / <paramref name="cut1"/> (zero: square to the arris; a mitre: <see cref="Mitre"/>). The half
    /// next to face A carries nA as its normal and face A's projection (UV0 held, <see cref="HoldKeep"/>), the other half nB.
    /// <paramref name="flat"/>: one facet per band (the plain path's geometry). Returns the triangles added.
    /// </summary>
    public static int Strip(List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> t, Vector3 k0, Vector3 k1, Vector3 nA, Vector3 nB,
        float radius, int segments, Vector3 cut0, Vector3 cut1, Vector3 uvOffset, float repeat, bool flat = false)
    {
        var span = k1 - k0;
        var length = span.magnitude;
        if (radius <= 0f || length < 1e-5f) return 0;
        var axis = span / length;
        segments = Even(segments);
        var half = segments / 2;
        var tris = 0;
        if (flat)
        {
            // one flat facet per band, on its own half's face: that face's normal and projection
            for (var i = 0; i < segments; i++)
            {
                var a0 = Mathf.PI * .5f * i / segments;
                var a1 = Mathf.PI * .5f * (i + 1) / segments;
                var onA = i < half;
                var face = onA ? nA : nB;
                var o0 = nA * ((Mathf.Cos(a0) - 1f) * radius) + nB * ((Mathf.Sin(a0) - 1f) * radius);
                var o1 = nA * ((Mathf.Cos(a1) - 1f) * radius) + nB * ((Mathf.Sin(a1) - 1f) * radius);
                var h0 = Hold(onA, nA, nB, radius, a0);
                var h1 = Hold(onA, nA, nB, radius, a1);
                float s0 = Cut(o0, axis, cut0, 0f), e0 = Cut(o0, axis, cut1, length);
                float s1 = Cut(o1, axis, cut0, 0f), e1 = Cut(o1, axis, cut1, length);
                if (e0 < s0) e0 = s0;
                if (e1 < s1) e1 = s1;
                var b = v.Count;
                AddHeld(v, n, uv, k0 + o0 + axis * s0, h0, face, uvOffset, repeat);
                AddHeld(v, n, uv, k0 + o1 + axis * s1, h1, face, uvOffset, repeat);
                AddHeld(v, n, uv, k0 + o1 + axis * e1, h1, face, uvOffset, repeat);
                AddHeld(v, n, uv, k0 + o0 + axis * e0, h0, face, uvOffset, repeat);
                tris += Tri(v, n, t, b, b + 1, b + 2) + Tri(v, n, t, b, b + 2, b + 3);
            }
            return tris;
        }
        for (var h = 0; h < 2; h++)
        {
            var face = h == 0 ? nA : nB;
            var first = v.Count;
            for (var i = 0; i <= half; i++)
            {
                var phi = Mathf.PI * .5f * (h * half + i) / segments;
                float c = Mathf.Cos(phi), s = Mathf.Sin(phi);
                var off = nA * ((c - 1f) * radius) + nB * ((s - 1f) * radius);
                var held = Hold(h == 0, nA, nB, radius, phi);
                var t0 = Cut(off, axis, cut0, 0f);
                var t1 = Cut(off, axis, cut1, length);
                if (t1 < t0) t1 = t0;
                AddHeld(v, n, uv, k0 + off + axis * t0, held, face, uvOffset, repeat);
                AddHeld(v, n, uv, k0 + off + axis * t1, held, face, uvOffset, repeat);
            }
            for (var i = 0; i < half; i++)
            {
                var a = first + i * 2;
                tris += Tri(v, n, t, a, a + 2, a + 3) + Tri(v, n, t, a, a + 3, a + 1);
            }
        }
        return tris;
    }

    /// <summary>The bit of a box's edge between its faces with outward normals <paramref name="f1"/> and <paramref name="f2"/> (two different world axes; 0 otherwise).</summary>
    public static int Edge(Vector3 f1, Vector3 f2)
    {
        int i1 = AxisOf(f1), i2 = AxisOf(f2);
        if (i1 == i2) return 0;
        var a = 3 - i1 - i2;
        int b = (a + 1) % 3, c = (a + 2) % 3;
        var sb = i1 == b ? SignOf(f1) : SignOf(f2);
        var sc = i1 == c ? SignOf(f1) : SignOf(f2);
        return Bit(a, sb, sc);
    }

    /// <summary>
    /// An axis-aligned box with the edges in <paramref name="edges"/> (<see cref="Edge"/> bits) rounded: its faces narrowed
    /// where they meet a round, a face across a round's end given the round's quarter circle at that corner, and two rounds
    /// that share a corner of the box (and a face) mitred there. Three rounds at one corner are not supported. The radius
    /// shrinks to fit the box. Returns the triangles added.
    /// </summary>
    public static int Box(List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> t, Vector3 center, Vector3 size, int edges,
        float radius, int segments, Vector3 uvOffset, float repeat, bool flat = false)
    {
        var lo = center - size * .5f;
        var hi = center + size * .5f;
        segments = Even(segments);
        // the radius fits every face it narrows (a face narrowed on both sides keeps its middle)
        for (var j = 0; j < 3; j++)
        {
            var most = 0;
            for (var k = 0; k < 3; k++)
            {
                if (k == j) continue;
                for (var s = -1; s <= 1; s += 2)
                {
                    var count = 0;
                    for (var sg = -1; sg <= 1; sg += 2) if ((edges & Edge(Axis(k) * s, Axis(j) * sg)) != 0) count++;
                    most = Mathf.Max(most, count);
                }
            }
            if (most > 0) radius = Mathf.Min(radius, (hi[j] - lo[j]) / most);
        }
        if (radius <= 1e-5f) edges = 0;
        var tris = 0;
        var pts = new List<Vector3>();
        for (var k = 0; k < 3; k++)
        for (var s = -1; s <= 1; s += 2)
        {
            var f = Axis(k) * s;
            int u = (k + 1) % 3, w = (k + 2) % 3;
            var U = Axis(u);
            var W = Axis(w);
            bool uLo = (edges & Edge(f, -U)) != 0, uHi = (edges & Edge(f, U)) != 0, wLo = (edges & Edge(f, -W)) != 0, wHi = (edges & Edge(f, W)) != 0;
            float u0 = lo[u] + (uLo ? radius : 0f), u1 = hi[u] - (uHi ? radius : 0f), w0 = lo[w] + (wLo ? radius : 0f), w1 = hi[w] - (wHi ? radius : 0f);
            if (u1 - u0 < 1e-6f || w1 - w0 < 1e-6f) continue;   // the round took the whole face
            var plane = s > 0 ? hi[k] : lo[k];
            pts.Clear();
            var arcs = false;
            // counter-clockwise in (u, w): corners (-,-), (+,-), (+,+), (-,+); a corner across a round's end becomes its arc
            for (var corner = 0; corner < 4; corner++)
            {
                var su = corner == 1 || corner == 2 ? 1 : -1;
                var sw = corner >= 2 ? 1 : -1;
                var cu = su > 0 ? u1 : u0;
                var cw = sw > 0 ? w1 : w0;
                var keptU = su > 0 ? !uHi : !uLo;
                var keptW = sw > 0 ? !wHi : !wLo;
                var round = keptU && keptW && (edges & Edge(U * su, W * sw)) != 0;
                if (!round) { pts.Add(Point(k, plane, u, cu, w, cw)); continue; }
                arcs = true;
                var start = corner == 0 ? 180f : corner == 1 ? 270f : corner == 2 ? 0f : 90f;
                float ccu = cu - su * radius, ccw = cw - sw * radius;
                for (var i = 0; i <= segments; i++)
                {
                    var a = (start + 90f * i / segments) * Mathf.Deg2Rad;
                    pts.Add(Point(k, plane, u, ccu + Mathf.Cos(a) * radius, w, ccw + Mathf.Sin(a) * radius));
                }
            }
            if (!arcs)
            {
                Quad(v, n, uv, t, pts[0], pts[1], pts[2], pts[3], f, uvOffset, repeat);
                tris += 2;
                continue;
            }
            var mid = Vector3.zero;
            foreach (var p in pts) mid += p;
            mid /= pts.Count;
            var centreIndex = v.Count;
            Add(v, n, uv, mid, f, f, uvOffset, repeat);
            var firstRing = v.Count;
            foreach (var p in pts) Add(v, n, uv, p, f, f, uvOffset, repeat);
            for (var i = 0; i < pts.Count; i++) tris += Tri(v, n, t, centreIndex, firstRing + i, firstRing + (i + 1) % pts.Count);
        }
        // the rounds
        for (var a = 0; a < 3; a++)
        for (var bit = 0; bit < 4; bit++)
        {
            if ((edges & (1 << (a * 4 + bit))) == 0) continue;
            var sb = (bit & 1) != 0 ? 1f : -1f;
            var sc = (bit & 2) != 0 ? 1f : -1f;
            int b = (a + 1) % 3, c = (a + 2) % 3;
            var A = Axis(a);
            var F1 = Axis(b) * sb;
            var F2 = Axis(c) * sc;
            var k0 = Vector3.zero;
            k0[a] = lo[a];
            k0[b] = sb > 0f ? hi[b] : lo[b];
            k0[c] = sc > 0f ? hi[c] : lo[c];
            var k1 = k0;
            k1[a] = hi[a];
            Vector3 m0 = Vector3.zero, m1 = Vector3.zero;
            bool n1lo = (edges & Edge(F1, -A)) != 0, n2lo = (edges & Edge(F2, -A)) != 0;
            if (n1lo && !n2lo) m0 = Mitre(Axis(c) * sc, A);
            else if (n2lo && !n1lo) m0 = Mitre(Axis(b) * sb, A);
            bool n1hi = (edges & Edge(F1, A)) != 0, n2hi = (edges & Edge(F2, A)) != 0;
            if (n1hi && !n2hi) m1 = Mitre(A, -Axis(c) * sc);
            else if (n2hi && !n1hi) m1 = Mitre(A, -Axis(b) * sb);
            tris += Strip(v, n, uv, t, k0, k1, F1, F2, radius, segments, m0, m1, uvOffset, repeat, flat);
        }
        return tris;
    }

    /// <summary>
    /// A quarter round swept along a curved arris: <paramref name="path"/> the sharp arris's points, <paramref name="nA"/> the
    /// first face's outward normal (one plane), <paramref name="nB"/> the second face's outward normal at each point (square to
    /// nA). Square ends. Each step keeps its own vertices, its second half projected by the step's mean nB (one projection
    /// per step, as the faces beside it). The first half carries nA as its normal; the second half nB (smooth: each point's,
    /// as the smooth intrados beside it; <paramref name="flat"/>: the step's mean, as the flat strips beside it). UV0 held as
    /// in <see cref="Strip"/>. Returns the triangles added.
    /// </summary>
    public static int Sweep(List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> t, IList<Vector3> path, Vector3 nA, IList<Vector3> nB,
        float radius, int segments, Vector3 uvOffset, float repeat, bool flat = false)
    {
        if (path == null || nB == null || path.Count < 2 || nB.Count != path.Count || radius <= 0f) return 0;
        segments = Even(segments);
        var half = segments / 2;
        var tris = 0;
        for (var p = 0; p + 1 < path.Count; p++)
        {
            var nbMid = (nB[p] + nB[p + 1]).normalized;
            if (flat)
            {
                // one flat facet per band and step, on its own half's face (the second half: the step's mean nB)
                for (var i = 0; i < segments; i++)
                {
                    var a0 = Mathf.PI * .5f * i / segments;
                    var a1 = Mathf.PI * .5f * (i + 1) / segments;
                    var onA = i < half;
                    var face = onA ? nA : nbMid;
                    Vector3 Pt(int e, float a) => path[p + e] + nA * ((Mathf.Cos(a) - 1f) * radius) + nB[p + e] * ((Mathf.Sin(a) - 1f) * radius);
                    var b = v.Count;
                    AddHeld(v, n, uv, Pt(0, a0), Hold(onA, nA, nB[p], radius, a0), face, uvOffset, repeat);
                    AddHeld(v, n, uv, Pt(0, a1), Hold(onA, nA, nB[p], radius, a1), face, uvOffset, repeat);
                    AddHeld(v, n, uv, Pt(1, a1), Hold(onA, nA, nB[p + 1], radius, a1), face, uvOffset, repeat);
                    AddHeld(v, n, uv, Pt(1, a0), Hold(onA, nA, nB[p + 1], radius, a0), face, uvOffset, repeat);
                    tris += Tri(v, n, t, b, b + 1, b + 2) + Tri(v, n, t, b, b + 2, b + 3);
                }
                continue;
            }
            for (var h = 0; h < 2; h++)
            {
                var uvNormal = h == 0 ? nA : nbMid;
                var first = v.Count;
                for (var i = 0; i <= half; i++)
                {
                    var phi = Mathf.PI * .5f * (h * half + i) / segments;
                    float c = Mathf.Cos(phi), s = Mathf.Sin(phi);
                    for (var e = 0; e < 2; e++)
                    {
                        var nb = nB[p + e];
                        var off = nA * ((c - 1f) * radius) + nb * ((s - 1f) * radius);
                        var at = path[p + e] + off;
                        v.Add(at);
                        n.Add(h == 0 ? nA : nb);
                        uv.Add(PlanarUV(at + Hold(h == 0, nA, nb, radius, phi), uvNormal, uvOffset, repeat));
                    }
                }
                for (var i = 0; i < half; i++)
                {
                    var a = first + i * 2;
                    tris += Tri(v, n, t, a, a + 2, a + 3) + Tri(v, n, t, a, a + 3, a + 1);
                }
            }
        }
        return tris;
    }

    static int Even(int segments)
    {
        if (segments < 2) segments = 2;
        return (segments & 1) == 1 ? segments + 1 : segments;
    }

    // Where the line k0 + off + axis * x meets the plane through k0 + axis * at with normal cut (none: at).
    static float Cut(Vector3 off, Vector3 axis, Vector3 cut, float at)
    {
        if (cut.sqrMagnitude < 1e-8f) return at;
        var d = Vector3.Dot(axis, cut);
        if (Mathf.Abs(d) < 1e-3f) return at;
        return at - Vector3.Dot(off, cut) / d;
    }

    static void Add(List<Vector3> v, List<Vector3> n, List<Vector2> uv, Vector3 p, Vector3 normal, Vector3 uvNormal, Vector3 uvOffset, float repeat)
    {
        v.Add(p);
        n.Add(normal);
        uv.Add(PlanarUV(p, uvNormal, uvOffset, repeat));
    }

    // A round's point at angle a (0: on face A's tangent line, 90 degrees: on face B's) moved back towards its own half's
    // tangent line, all but HoldKeep of the way: where its UV0 is taken.
    static Vector3 Hold(bool onA, Vector3 nA, Vector3 nB, float radius, float a)
    {
        var keep = 1f - HoldKeep;
        return onA ? nB * (-keep * radius * Mathf.Sin(a)) : nA * (-keep * radius * Mathf.Cos(a));
    }

    // A round's vertex at p: its half's face normal, UV0 by that face's projection at p + held (Hold).
    static void AddHeld(List<Vector3> v, List<Vector3> n, List<Vector2> uv, Vector3 p, Vector3 held, Vector3 face, Vector3 uvOffset, float repeat)
    {
        v.Add(p);
        n.Add(face);
        uv.Add(PlanarUV(p + held, face, uvOffset, repeat));
    }

    // One triangle, wound to face its vertices' normals; a degenerate one (a mitre's zero-length row) is left out.
    static int Tri(List<Vector3> v, List<Vector3> n, List<int> t, int a, int b, int c)
    {
        var cross = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
        if (cross.sqrMagnitude < 1e-18f) return 0;
        if (Vector3.Dot(cross, n[a] + n[b] + n[c]) >= 0f) { t.Add(a); t.Add(b); t.Add(c); }
        else { t.Add(a); t.Add(c); t.Add(b); }
        return 1;
    }

    static Vector3 Point(int k, float plane, int u, float pu, int w, float pw)
    {
        var p = Vector3.zero;
        p[k] = plane;
        p[u] = pu;
        p[w] = pw;
        return p;
    }

    static int AxisOf(Vector3 d) => Mathf.Abs(d.x) > .5f ? 0 : Mathf.Abs(d.y) > .5f ? 1 : 2;
    static float SignOf(Vector3 d) => d.x + d.y + d.z > 0f ? 1f : -1f;
    static Vector3 Axis(int i) => i == 0 ? Vector3.right : i == 1 ? Vector3.up : Vector3.forward;
    static int Bit(int a, float sb, float sc) => 1 << (a * 4 + (sb > 0f ? 1 : 0) + (sc > 0f ? 2 : 0));
}
