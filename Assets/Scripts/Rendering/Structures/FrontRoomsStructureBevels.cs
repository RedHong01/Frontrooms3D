// Scene 4's edge option: rounds by material on the builder's boxes, only where an edge is really exposed, written by the map's
// shared rounds (FrontRoomsBevel: surface detail package 8, FrontRoomsMapWorld.Bevel.cs, surface_bevels and surface_bevels_fix)
// (game visual line, structures_assets step 2, package structures_profiles; research/structures_assets/20_research.md Part B
// F1, B.2, B.6; 30_plan.md row X-01).
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The structures scene's bevels (scene 4 only, NEW), on top of the map's surface_bevels contract. The rounds themselves are
/// FrontRoomsBevel's: its quarter round (Strip), its mitre (Mitre), its held normals (each half of a round lit as its own face,
/// never darker than the darker face: the two-lamp valley fix), its held UV0 (HoldKeep), its segment rule and its radii. So
/// scene 4 and the map round an arris the same way, with one implementation. What scene 4 adds is only what its builder needs.
/// The builder (FrontRoomsStructuresMesh, through the staged contract K1) records its boxes instead of writing them; at Finish
/// the set is solved: an edge of a box is rounded only when nothing else of the set touches it (probes just outside its two
/// faces and their diagonal, along its whole length), so two floor slabs side by side, a lintel on its piers or a ledge on the
/// next cell's ledge keep their joint flush and only the true arrises turn. A partly covered edge keeps today's knife edge
/// (never a groove). Boxes may be turned (Strip takes any two face normals). Three rounds may meet at a corner (a free-standing
/// pier's top, a plinth, a cap's end): there the rounds stop a radius short and Octant closes the corner, each third of it held
/// to its own face's normal and UV0 as Strip holds each half of a round.
/// Radii by material: the map's corner bead (FrontRoomsMapWorld.BevelWallRadius, 3 mm) on board, paper and paint; the map's
/// tile bullnose (BevelTileRadius, 3/8 in) on tile; a 1/2 in edger's round on cast concrete; a bullnose block's 1 in; 3 mm on
/// wood; none on metal (a deburred steel arris is under a millimetre, and a held round shows only its outline: below a pixel
/// at any walking distance, so it would be triangles for nothing; the sweeps' sections carry a tube's or an angle's real
/// radii), on glass, on a _FR_DETAIL surface (as the map: its per-wall id reads the normal) and on the scene's own signs and
/// marks. FrontRooms/Surface's plain path takes the map's faceted round (Shape). Held normals keep the
/// plain path's frame on each half of a round exactly its face's, a turned face included, so no _FR_PlanarSnap is set (it would
/// snap a turned wall's paper to the nearest axis). The faces keep today's UVs (world planar, or a prism's own frame; a
/// prism's rounds take the prism's frame at the same held points). Colliders are never touched: the builder's collision mesh is
/// written as today, so the walk test sees the same scene.
/// Cost: a rounded edge is 2 x its facets in triangles (12 for the 3 mm bead's 6), a corner of three rounds 3 x its facets.
/// </summary>
public static class FrontRoomsStructureBevels
{
    /// <summary>A round: radius (m), segments (FrontRoomsBevel's: made even, 2 to 12) and flat (one facet per band, the plain
    /// path's geometry; Shape sets it from the material).</summary>
    public struct Spec
    {
        public float radius;
        public int segments;
        public bool flat;

        public Spec(float radius, int segments, bool flat)
        {
            this.radius = radius;
            this.segments = Mathf.Clamp(segments, 2, 12);
            this.flat = flat;
        }

        public bool On { get { return radius > .0008f; } }
    }

    public static readonly Spec None = new Spec(0f, 2, false);
    /// <summary>Corner bead on gypsum board under paper or paint: the map's (20_design.md 5.1 row 8: r 1-3 mm).</summary>
    public static readonly Spec Board = new Spec(FrontRoomsMapWorld.BevelWallRadius, FrontRoomsMapWorld.BevelWallSegments, false);
    /// <summary>A glazed tile's surface bullnose on an outside corner: the map's (P6: r 6-10 mm; 3/8 in).</summary>
    public static readonly Spec Tile = new Spec(FrontRoomsMapWorld.BevelTileRadius, FrontRoomsMapWorld.BevelTileSegments, false);
    /// <summary>Cast concrete: a 1/2 in edger's round on slab and stair edges (the shared rounds have no chamfer).</summary>
    public static readonly Spec Concrete = new Spec(.0127f, 4, false);
    /// <summary>A bullnose block corner (1 in).</summary>
    public static readonly Spec Block = new Spec(.0254f, 4, false);
    /// <summary>A sanded wood arris.</summary>
    public static readonly Spec Wood = new Spec(.003f, 4, false);
    /// <summary>A rolled or cut steel edge, deburred: none drawn (under a millimetre; see the class note).</summary>
    public static readonly Spec Metal = None;
    /// <summary>Carpet wrapping a slab edge.</summary>
    public static readonly Spec Carpet = new Spec(.004f, 4, false);

    static readonly Dictionary<Material, Spec> specCache = new Dictionary<Material, Spec>();

    /// <summary>False in WebGL players: they keep box edges, as the map's do (FrontRoomsMapWorld.BevelsOn).</summary>
    public static bool RoundsOn
    {
        get
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return false;
#else
            return true;
#endif
        }
    }

    /// <summary>The round a material's arrises get (by the game's surface names; the scene's stand-ins and glass none), shaped by
    /// the map's material rules (Shape).</summary>
    public static Spec For(Material m)
    {
        if (m == null) return None;
        Spec s;
        if (specCache.TryGetValue(m, out s)) return s;
        s = Shape(Classify(m), m);
        specCache[m] = s;
        return s;
    }

    /// <summary>
    /// The map's material rules for a round (FrontRoomsMapWorld.Bevel.cs: BevelMaterial, BevelFaceted, BevelFacets; private there,
    /// so restated here rule for rule until the map makes them public, K1's ask A-1): no round on a _FR_DETAIL surface (its
    /// per-wall id, FRDetailWallId, reads the unsnapped normal: package B2); FrontRooms/Surface's plain path takes a faceted
    /// round, about 0.8 mm of arc a facet, 4 to 12, even; the mesh-UV tile and plain URP materials keep a smooth round of the
    /// spec's segments.
    /// </summary>
    public static Spec Shape(Spec s, Material m)
    {
        if (!RoundsOn) return None;
        if (m == null || !s.On) return s;
        if (m.IsKeywordEnabled("_FR_DETAIL")) return None;
        s.flat = m.shader != null && m.shader.name == "FrontRooms/Surface" && !m.IsKeywordEnabled("_FR_MESH_UV");
        if (s.flat)
        {
            var n = Mathf.CeilToInt(s.radius * Mathf.PI * .5f / .0008f);
            n = Mathf.Clamp(Mathf.Max(n, s.segments), 4, 12);
            s.segments = (n & 1) == 1 ? n + 1 : n;
        }
        return s;
    }

    static Spec Classify(Material m)
    {
        var n = m.name ?? "";
        var shader = m.shader != null ? m.shader.name : "";
        if (shader == "FrontRooms/Glass" || n.IndexOf("Glass", System.StringComparison.Ordinal) >= 0) return None;
        if (shader != "FrontRooms/Surface")
        {
            // runtime stand-ins: only the tile fallbacks are real enough to round; signs, pads, marks, glows stay square
            if (n.StartsWith("Structures / tile", System.StringComparison.Ordinal) || n.StartsWith("Structures / band", System.StringComparison.Ordinal)) return Tile;
            return None;
        }
        if (m.IsKeywordEnabled("_FR_TILE") || n.StartsWith("Prop_PoolTile_", System.StringComparison.Ordinal)) return Tile;
        if (n.StartsWith("Stair_Slab", System.StringComparison.Ordinal) || n.StartsWith("Stair_Fill", System.StringComparison.Ordinal) || n.StartsWith("Slab_", System.StringComparison.Ordinal)) return Concrete;
        if (n.StartsWith("RunB_Block", System.StringComparison.Ordinal) || n.StartsWith("Stair_Block", System.StringComparison.Ordinal)) return Block;
        if (n.StartsWith("Prop_Wood", System.StringComparison.Ordinal) || n.StartsWith("Door_Veneer", System.StringComparison.Ordinal) || n.StartsWith("Prop_Plywood", System.StringComparison.Ordinal)) return Wood;
        if (n.StartsWith("Prop_Steel", System.StringComparison.Ordinal) || n.StartsWith("Prop_Stainless", System.StringComparison.Ordinal) || n.StartsWith("Prop_Alumin", System.StringComparison.Ordinal)
            || n.StartsWith("Painted_Metal", System.StringComparison.Ordinal) || n.StartsWith("RunB_GalvSteel", System.StringComparison.Ordinal) || n.StartsWith("Station_Stainless", System.StringComparison.Ordinal)
            || n.StartsWith("Prop_Chrome", System.StringComparison.Ordinal) || n.StartsWith("Prop_Brass", System.StringComparison.Ordinal) || n.StartsWith("Trans_Alumin", System.StringComparison.Ordinal)
            // G11 (K1 review, structures G3-b): P3's painted handrail steel is a metal (as Board it gave the 1/2 in pickets wall rounds)
            || n.StartsWith("Prop_RailEnamel", System.StringComparison.Ordinal)) return Metal;
        if (n.IndexOf("Carpet", System.StringComparison.Ordinal) >= 0 || n.StartsWith("RunB_Floor", System.StringComparison.Ordinal) || n.StartsWith("Run_Floor", System.StringComparison.Ordinal)) return Carpet;
        if (n.StartsWith("Prop_Plastic", System.StringComparison.Ordinal) || n.StartsWith("Prop_Laminate", System.StringComparison.Ordinal) || n.StartsWith("Prop_Vinyl", System.StringComparison.Ordinal)) return Wood;
        // paper, paint, gypsum, acoustic ceilings, plenum, every other surface of the game: corner bead
        return Board;
    }

    /// <summary>How a box's faces take their UVs: world planar on the face's dominant axis (FrontRoomsStructuresMesh.Box) or a
    /// prism's own frame (FrontRoomsStructuresKit.Prism: u along its line from world 0, v = y; ends across; top and bottom
    /// world planar), so the faces keep today's UVs exactly.</summary>
    public enum UvRule { Planar = 0, Prism = 1 }

    /// <summary>One recorded box: centre and half size in the builder's root space, its rotation, the bevel and UV rule, and the
    /// owner's tag (its render group). Mask is filled by Solve (bit = 1 &lt;&lt; edge id).</summary>
    public sealed class Box
    {
        public Vector3 centre, half;
        public Quaternion rotation = Quaternion.identity;
        public Spec spec;
        public UvRule uv;
        public object tag;
        public int mask;
        /// <summary>Not drawn: a volume that only counts as touching (an arch's spandrel built of quads).</summary>
        public bool occluderOnly;
        /// <summary>The owner's per-box vertex value (scene 4: a wall's lot and foot for the paper's scale terms, UV1).</summary>
        public Vector2 wallValue;
        internal Vector3 ax, ay, az;
        internal Bounds bounds;

        internal Vector3 Axis(int a) { return a == 0 ? ax : a == 1 ? ay : az; }
        internal float Half(int a) { return a == 0 ? half.x : a == 1 ? half.y : half.z; }
    }

    /// <summary>A set of boxes solved together (one builder mesh).</summary>
    public sealed class Set
    {
        public readonly List<Box> Boxes = new List<Box>();
        readonly Dictionary<long, List<int>> grid = new Dictionary<long, List<int>>();
        const float CellSize = 1f;
        /// <summary>Probe offsets: just outside a face (eps) and a little in along the face from the edge (delta).</summary>
        public const float Eps = .0015f, Delta = .004f, Tolerance = .0002f;

        public Box Add(Vector3 centre, Vector3 size, Quaternion rotation, Spec spec, UvRule uv, object tag)
        {
            var b = new Box { centre = centre, half = size * .5f, rotation = rotation, spec = spec, uv = uv, tag = tag };
            b.ax = rotation * Vector3.right;
            b.ay = rotation * Vector3.up;
            b.az = rotation * Vector3.forward;
            var ext = new Vector3(
                Mathf.Abs(b.ax.x) * b.half.x + Mathf.Abs(b.ay.x) * b.half.y + Mathf.Abs(b.az.x) * b.half.z,
                Mathf.Abs(b.ax.y) * b.half.x + Mathf.Abs(b.ay.y) * b.half.y + Mathf.Abs(b.az.y) * b.half.z,
                Mathf.Abs(b.ax.z) * b.half.x + Mathf.Abs(b.ay.z) * b.half.y + Mathf.Abs(b.az.z) * b.half.z);
            b.bounds = new Bounds(centre, ext * 2f + Vector3.one * .01f);
            Boxes.Add(b);
            return b;
        }

        static long Key(int x, int y, int z)
        {
            return ((long)(x + 1048576) << 42) | ((long)(y + 1048576) << 21) | (long)(z + 1048576);
        }

        void Index()
        {
            grid.Clear();
            for (var i = 0; i < Boxes.Count; i++)
            {
                var b = Boxes[i].bounds;
                int x0 = Mathf.FloorToInt(b.min.x / CellSize), x1 = Mathf.FloorToInt(b.max.x / CellSize);
                int y0 = Mathf.FloorToInt(b.min.y / CellSize), y1 = Mathf.FloorToInt(b.max.y / CellSize);
                int z0 = Mathf.FloorToInt(b.min.z / CellSize), z1 = Mathf.FloorToInt(b.max.z / CellSize);
                for (var x = x0; x <= x1; x++)
                for (var y = y0; y <= y1; y++)
                for (var z = z0; z <= z1; z++)
                {
                    var k = Key(x, y, z);
                    List<int> list;
                    if (!grid.TryGetValue(k, out list)) grid[k] = list = new List<int>(4);
                    list.Add(i);
                }
            }
        }

        bool InsideOther(int self, Vector3 q)
        {
            List<int> list;
            if (!grid.TryGetValue(Key(Mathf.FloorToInt(q.x / CellSize), Mathf.FloorToInt(q.y / CellSize), Mathf.FloorToInt(q.z / CellSize)), out list)) return false;
            for (var i = 0; i < list.Count; i++)
            {
                var j = list[i];
                if (j == self) continue;
                var o = Boxes[j];
                var d = q - o.centre;
                if (Mathf.Abs(Vector3.Dot(d, o.ax)) < o.half.x - Tolerance && Mathf.Abs(Vector3.Dot(d, o.ay)) < o.half.y - Tolerance
                    && Mathf.Abs(Vector3.Dot(d, o.az)) < o.half.z - Tolerance) return true;
            }
            return false;
        }

        /// <summary>Fills every box's mask: an edge is bevelled when its spec is on, the box is thick enough and nothing touches it.</summary>
        public void Solve()
        {
            Index();
            for (var i = 0; i < Boxes.Count; i++)
            {
                var b = Boxes[i];
                b.mask = 0;
                if (b.occluderOnly || !b.spec.On) continue;
                var r = Radius(b);
                if (r <= .0008f) continue;
                for (var e = 0; e < 12; e++)
                    if (Exposed(i, b, e)) b.mask |= 1 << e;
            }
        }

        bool Exposed(int self, Box b, int e)
        {
            int a1, a2, a3;
            float s1, s2;
            EdgeAxes(e, out a1, out a2, out a3, out s1, out s2);
            var na = b.Axis(a1) * s1;
            var nb = b.Axis(a2) * s2;
            var along = b.Axis(a3);
            var h3 = b.Half(a3);
            var p0 = b.centre + na * b.Half(a1) + nb * b.Half(a2);
            var delta = Mathf.Min(Delta, Mathf.Min(b.Half(a1), b.Half(a2)));
            var n = Mathf.Clamp(Mathf.CeilToInt(2f * h3 / .25f) + 1, 3, 64);
            for (var k = 0; k < n; k++)
            {
                var s = Mathf.Lerp(-h3 + .002f, h3 - .002f, n == 1 ? .5f : k / (float)(n - 1));
                var p = p0 + along * s;
                if (InsideOther(self, p + na * Eps - nb * delta)) return false;
                if (InsideOther(self, p + nb * Eps - na * delta)) return false;
                if (InsideOther(self, p + (na + nb) * Eps)) return false;
            }
            return true;
        }
    }

    // ---------------------------------------------------------------- edges and corners of a box

    // Edge ids: pair 0 = faces X and Y (along Z), pair 1 = X and Z (along Y), pair 2 = Y and Z (along X); id = pair * 4 +
    // (first face's sign + ? 0 : 2) + (second face's sign + ? 0 : 1).
    static void EdgeAxes(int e, out int a1, out int a2, out int a3, out float s1, out float s2)
    {
        var pair = e / 4;
        s1 = (e & 2) == 0 ? 1f : -1f;
        s2 = (e & 1) == 0 ? 1f : -1f;
        if (pair == 0) { a1 = 0; a2 = 1; a3 = 2; }
        else if (pair == 1) { a1 = 0; a2 = 2; a3 = 1; }
        else { a1 = 1; a2 = 2; a3 = 0; }
    }

    /// <summary>The edge between face axis a (sign sa) and face axis b (sign sb).</summary>
    static int EdgeId(int a, float sa, int b, float sb)
    {
        if (a > b) { var t = a; a = b; b = t; var ts = sa; sa = sb; sb = ts; }
        var pair = a == 0 && b == 1 ? 0 : a == 0 && b == 2 ? 1 : 2;
        return pair * 4 + (sa > 0f ? 0 : 2) + (sb > 0f ? 0 : 1);
    }

    static bool Bev(int mask, int a, float sa, int b, float sb)
    {
        return (mask & (1 << EdgeId(a, sa, b, sb))) != 0;
    }

    static float Radius(Box b)
    {
        var m = Mathf.Min(b.half.x, Mathf.Min(b.half.y, b.half.z));
        return Mathf.Min(b.spec.radius, .3f * m);
    }

    static float Comp(Vector3 v, int a) { return a == 0 ? v.x : a == 1 ? v.y : v.z; }

    static Vector3 Local(Box b, float x, float y, float z)
    {
        return b.centre + b.ax * x + b.ay * y + b.az * z;
    }

    static Vector3 LocalByAxes(Box b, int a, float va, int c, float vc, int d, float vd)
    {
        var l = Vector3.zero;
        Put(ref l, a, va);
        Put(ref l, c, vc);
        Put(ref l, d, vd);
        return Local(b, l.x, l.y, l.z);
    }

    static void Put(ref Vector3 v, int a, float x)
    {
        if (a == 0) v.x = x;
        else if (a == 1) v.y = x;
        else v.z = x;
    }

    // ---------------------------------------------------------------- writing a box

    /// <summary>
    /// Writes a solved box (its mask) into the lists, in the builder's root space; worldOffset is the root's world position
    /// (the UVs are world metres: FrontRoomsBevel's uvOffset). Returns the triangles written. A box with mask 0 is today's box
    /// (24 vertices, 12 triangles), with the same UVs as the builder's own. Otherwise its faces are narrowed where they meet a
    /// round, and a face across a round's square end takes the round's quarter circle at FrontRoomsBevel's angles; the rounds are
    /// FrontRoomsBevel.Strip (square ends; FrontRoomsBevel.Mitre where two rounds share a face at a corner, as its Box; a radius
    /// short where three rounds share a corner, which Octant closes).
    /// </summary>
    public static int Emit(Box b, List<Vector3> verts, List<Vector3> normals, List<Vector2> uvs, List<int> tris, Vector3 worldOffset)
    {
        var r = b.mask != 0 ? Radius(b) : 0f;
        var mask = r > 0f ? b.mask : 0;
        var k = Even(b.spec.segments);
        var start = tris.Count;
        var h = new float[] { b.half.x, b.half.y, b.half.z };
        // the faces: convex outlines in their planes, fan-triangulated
        for (var a = 0; a < 3; a++)
        for (var si = 0; si < 2; si++)
        {
            var s = si == 0 ? 1f : -1f;
            var nrm = b.Axis(a) * s;
            var u = (a + 1) % 3;
            var w = (a + 2) % 3;
            var outline = new List<Vector3>(mask != 0 ? 4 * (k + 1) : 4);
            for (var cu = -1; cu <= 1; cu += 2)
            for (var cw = -1; cw <= 1; cw += 2)
            {
                var bu = Bev(mask, a, s, u, cu);
                var bw = Bev(mask, a, s, w, cw);
                var bp = Bev(mask, u, cu, w, cw);
                if (!bu && !bw && bp)
                {
                    // the square end of the perpendicular edge's round lies in this face: its quarter circle
                    for (var j = 0; j <= k; j++)
                    {
                        var phi = Mathf.PI * .5f * j / k;
                        var pu = cu * (h[u] - r + r * Mathf.Cos(phi));
                        var pw = cw * (h[w] - r + r * Mathf.Sin(phi));
                        outline.Add(LocalByAxes(b, a, s * h[a], u, pu, w, pw));
                    }
                }
                else
                    outline.Add(LocalByAxes(b, a, s * h[a], u, cu * (h[u] - (bu ? r : 0f)), w, cw * (h[w] - (bw ? r : 0f))));
            }
            Fan(b, outline, nrm, a, verts, normals, uvs, tris, worldOffset);
        }
        if (mask == 0) return (tris.Count - start) / 3;
        // the rounds along the exposed edges: FrontRoomsBevel's
        for (var e = 0; e < 12; e++)
        {
            if ((mask & (1 << e)) == 0) continue;
            int a1, a2, a3;
            float s1, s2;
            EdgeAxes(e, out a1, out a2, out a3, out s1, out s2);
            var nA = b.Axis(a1) * s1;
            var nB = b.Axis(a2) * s2;
            var axis = b.Axis(a3);
            Vector3 k0 = Vector3.zero, k1 = Vector3.zero, cut0 = Vector3.zero, cut1 = Vector3.zero;
            for (var end = 0; end < 2; end++)
            {
                var s3 = end == 0 ? -1f : 1f;
                var onA = Bev(mask, a1, s1, a3, s3);   // the round at this end that shares face A with ours
                var onB = Bev(mask, a2, s2, a3, s3);   // ... that shares face B
                var p = LocalByAxes(b, a1, s1 * h[a1], a2, s2 * h[a2], a3, s3 * (onA && onB ? h[a3] - r : h[a3]));
                var cut = Vector3.zero;
                if (onA && !onB) cut = end == 0 ? FrontRoomsBevel.Mitre(nB, axis) : FrontRoomsBevel.Mitre(axis, -nB);
                else if (onB && !onA) cut = end == 0 ? FrontRoomsBevel.Mitre(nA, axis) : FrontRoomsBevel.Mitre(axis, -nA);
                if (end == 0) { k0 = p; cut0 = cut; }
                else { k1 = p; cut1 = cut; }
            }
            var first = verts.Count;
            FrontRoomsBevel.Strip(verts, normals, uvs, tris, k0, k1, nA, nB, r, k, cut0, cut1, worldOffset, 1f, b.spec.flat);
            if (b.uv == UvRule.Prism) PrismHeld(b, verts, normals, uvs, first, k0, nA, nB, a1, a2, r, worldOffset);
        }
        // the corners three rounds share
        for (var cx = -1; cx <= 1; cx += 2)
        for (var cy = -1; cy <= 1; cy += 2)
        for (var cz = -1; cz <= 1; cz += 2)
        {
            if (!Bev(mask, 0, cx, 1, cy) || !Bev(mask, 0, cx, 2, cz) || !Bev(mask, 1, cy, 2, cz)) continue;
            Octant(b, Local(b, cx * (h[0] - r), cy * (h[1] - r), cz * (h[2] - r)), b.ax * cx, b.ay * cy, b.az * cz, r, k,
                verts, normals, uvs, tris, worldOffset);
        }
        return (tris.Count - start) / 3;
    }

    // FrontRoomsBevel's segment rule (even, so a round's 45 degree row is a vertex row): the faces' quarter circles and the
    // corners take the same count as its rounds.
    static int Even(int segments)
    {
        if (segments < 2) segments = 2;
        return (segments & 1) == 1 ? segments + 1 : segments;
    }

    // A corner three rounds share: the octant of the round's sphere (centre c, radius r) between the three faces' outward
    // normals, as three fans from its middle point, one third to each face. Each third carries its own face's normal and that
    // face's UV0 at its point held back towards the face's tangent lines (all but FrontRoomsBevel.HoldKeep of the way), as
    // FrontRoomsBevel.Strip holds each half of a round; its rims are the three rounds' square ends, at Strip's angles, and the
    // two lines from the middle to a round's 45 degree point are shared by the thirds beside it. 3 x segments triangles.
    static void Octant(Box b, Vector3 c, Vector3 nx, Vector3 ny, Vector3 nz, float r, int k, List<Vector3> verts, List<Vector3> normals,
        List<Vector2> uvs, List<int> tris, Vector3 worldOffset)
    {
        var mid = c + (nx + ny + nz).normalized * r;
        var outward = nx + ny + nz;
        var half = k / 2;
        for (var f = 0; f < 3; f++)
        {
            var nf = f == 0 ? nx : f == 1 ? ny : nz;
            var n2 = f == 0 ? ny : f == 1 ? nz : nx;
            var n3 = f == 0 ? nz : f == 1 ? nx : ny;
            var centre = verts.Count;
            HeldVertex(b, mid, c, nf, n2, n3, f, verts, normals, uvs, worldOffset);
            var corner = verts.Count;
            HeldVertex(b, c + nf * r, c, nf, n2, n3, f, verts, normals, uvs, worldOffset);
            for (var side = 0; side < 2; side++)
            {
                var other = side == 0 ? n2 : n3;
                var prev = corner;
                for (var j = 1; j <= half; j++)
                {
                    var phi = Mathf.PI * .5f * j / k;
                    var at = verts.Count;
                    HeldVertex(b, c + nf * (Mathf.Cos(phi) * r) + other * (Mathf.Sin(phi) * r), c, nf, n2, n3, f, verts, normals, uvs, worldOffset);
                    Tri(verts, tris, centre, prev, at, outward);
                    prev = at;
                }
            }
        }
    }

    // A corner patch's vertex: its third's face normal, UV0 by that face's rule at the point held towards its tangent lines.
    static void HeldVertex(Box b, Vector3 p, Vector3 c, Vector3 nf, Vector3 n2, Vector3 n3, int axis, List<Vector3> verts,
        List<Vector3> normals, List<Vector2> uvs, Vector3 worldOffset)
    {
        var keep = 1f - FrontRoomsBevel.HoldKeep;
        var d = p - c;
        var held = p - n2 * (keep * Vector3.Dot(d, n2)) - n3 * (keep * Vector3.Dot(d, n3));
        verts.Add(p);
        normals.Add(nf);
        uvs.Add(FaceUV(b, held, nf, axis, worldOffset));
    }

    // FrontRoomsBevel.Strip writes the map's planar UV0; a prism's faces take theirs along its own line, so its rounds take the
    // prism's rule at the same held points (each half's point moved back towards its own face's tangent line).
    static void PrismHeld(Box b, List<Vector3> verts, List<Vector3> normals, List<Vector2> uvs, int first, Vector3 k0, Vector3 nA,
        Vector3 nB, int a1, int a2, float r, Vector3 worldOffset)
    {
        var keep = 1f - FrontRoomsBevel.HoldKeep;
        for (var i = first; i < verts.Count; i++)
        {
            var onA = Vector3.Dot(normals[i], nA) > .5f;
            var other = onA ? nB : nA;
            var p = verts[i];
            var held = p - other * (keep * (Vector3.Dot(p - k0, other) + r));
            uvs[i] = FaceUV(b, held, onA ? nA : nB, onA ? a1 : a2, worldOffset);
        }
    }

    static void Fan(Box b, List<Vector3> outline, Vector3 nrm, int axis, List<Vector3> verts, List<Vector3> normals, List<Vector2> uvs, List<int> tris, Vector3 worldOffset)
    {
        if (outline.Count < 3) return;
        // order the convex outline round its centre (the corners were gathered in grid order)
        var centre = Vector3.zero;
        foreach (var p in outline) centre += p;
        centre /= outline.Count;
        var u = b.Axis((axis + 1) % 3);
        var w = b.Axis((axis + 2) % 3);
        outline.Sort((p, q) =>
        {
            var ap = Mathf.Atan2(Vector3.Dot(p - centre, w), Vector3.Dot(p - centre, u));
            var aq = Mathf.Atan2(Vector3.Dot(q - centre, w), Vector3.Dot(q - centre, u));
            return ap.CompareTo(aq);
        });
        // drop coincident neighbours (a corner arc's end meets the next corner's point when a side has no length)
        for (var i = outline.Count - 1; i > 0 && outline.Count > 3; i--)
            if ((outline[i] - outline[i - 1]).sqrMagnitude < 1e-12f) outline.RemoveAt(i);
        if (outline.Count > 3 && (outline[0] - outline[outline.Count - 1]).sqrMagnitude < 1e-12f) outline.RemoveAt(outline.Count - 1);
        var first = verts.Count;
        foreach (var p in outline)
        {
            verts.Add(p);
            normals.Add(nrm);
            uvs.Add(FaceUV(b, p, nrm, axis, worldOffset));
        }
        for (var i = 1; i + 1 < outline.Count; i++) Tri(verts, tris, first, first + i, first + i + 1, nrm);
    }

    // A face's UV (today's rule for the box's kind).
    static Vector2 FaceUV(Box b, Vector3 local, Vector3 nrm, int axis, Vector3 worldOffset)
    {
        var wp = local + worldOffset;
        if (b.uv == UvRule.Prism)
        {
            // FrontRoomsStructuresKit.Prism: local z = t (along), local x = s (across), y up; v = the root-space y
            if (axis == 0) return new Vector2(Vector3.Dot(wp, b.az), local.y);
            if (axis == 2) return new Vector2(Vector3.Dot(wp, b.ax), local.y);
            return new Vector2(wp.x, wp.z);
        }
        return Planar(wp, nrm);
    }

    /// <summary>FrontRoomsStructuresMesh's planar UV: metres from world 0 on the face's dominant plane.</summary>
    public static Vector2 Planar(Vector3 world, Vector3 nrm)
    {
        var ax = Mathf.Abs(nrm.x);
        var ay = Mathf.Abs(nrm.y);
        var az = Mathf.Abs(nrm.z);
        if (ax >= ay && ax >= az) return new Vector2(world.z, world.y);
        if (ay >= ax && ay >= az) return new Vector2(world.x, world.z);
        return new Vector2(world.x, world.y);
    }

    // A triangle wound to face along 'outward' (Unity's front face: Cross(p1 - p0, p2 - p0) along the normal).
    static void Tri(List<Vector3> verts, List<int> tris, int i0, int i1, int i2, Vector3 outward)
    {
        var c = Vector3.Cross(verts[i1] - verts[i0], verts[i2] - verts[i0]);
        if (c.sqrMagnitude < 1e-16f) return;
        if (Vector3.Dot(c, outward) >= 0f) { tris.Add(i0); tris.Add(i1); tris.Add(i2); }
        else { tris.Add(i0); tris.Add(i2); tris.Add(i1); }
    }
}
