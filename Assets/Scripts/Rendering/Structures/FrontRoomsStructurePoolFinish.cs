using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The rest of image 1's pool construction for scene 4 (structures_assets G2-d, 2026-11-10; line visual (game visual);
/// research/structures_assets/60_build.md '## G2-d'; W/structures_assets/g2_review_assets.md sections 5.2, 6, 8 and S-9; 30_plan
/// K-02, S1-08, S2-01, S6-06): Red's image 1 (SP1's slide hall, SP2's F0 pool hall, SP6's deep pool terrain; 2026-11-07 21:54 and
/// 21:58) with the pieces a 1988-91 natatorium is built with that G2-a / G2-b / G2-c left open. NEW only (the districts call it in
/// their NEW branch), render only: nothing here makes or moves a collider, a light or a water. Inert until the owners' contract
/// (W/apply/structures_pool_finish_contract) calls it.
///
/// - Cove base (C7; A.3.1 PC9 "sanitary cove base"; B F2): the feet of the district's tiled walls, piers, terraces and guards on
///   every dry tiled deck of the halls given (found in the district's own finished tile meshes: a vertical field-tile face that
///   stands at least 0.45 m over a dry field-tile floor; under water, at a step (a riser is lower) or where no floor is in front
///   there is none), chained round outside arrises and curved walls, swept as K1's FrontRoomsStructuresKit.CoveBase
///   (Prop_PoolTile_Cove: r 50 with its 50 mm flats, 2 mm proud), its open ends closed with the section's outline. Inside corners
///   are each wall's run carried into the other (the union of the two coves is the mitred corner piece).
/// - Movement joints (C7; B F14, TCNA EJ171 "12 ft or less where wet"): K1's SealantJoint (10 mm grey polyurethane, tooled) on the
///   same dry decks along the district's 3 m cell lines, wherever that deck runs under the line; they pass under the cove at a wall.
/// - Deck drains (C9; A.5.1, TM 5-662 3-5 and fig 3-3 "SLOPE TO FLOOR DRAIN"): Kit_FloorDrain at the points given, each on the
///   centre of its 150 mm tile, kept only on a dry deck clear of walls (the fall itself is not modelled: the deck is one slab).
/// - Block basins' fittings (C9; review section 6): for a basin built of blocks (SP6), the main drain in a 0.45 m square of the dark
///   Lane tile at the point given and return inlets 0.30 m under the water at the wall points given, clear of the basin's niches.
/// - Depth marks (C11; TM 5-662 2-5; S43; 30_plan rule 7): a framed BLANK inlay on the deck at the water's edge, ten whole 150 mm
///   tiles (the unglazed deck tile in the lane colour) round a 2 x 1 field of the deck's own tile, 1 mm proud, on the deck's grid: the numerals are the narrative line's
///   words and the graphic visual line's art, so the field stays blank.
/// - The ring buoy, the CCTV camera (G4's Kit_CCTVCamera, when its kit is in the build) and the dispatcher post (C11; S-9).
/// - KIT's three pool edges side by side (C13; ask 1; 30_plan K-02): an exhibit basin a bay long for each system.
/// District-local metres throughout (the kit's root has no rotation). ASCII only, but for the kit's niche lamp name it matches.
/// </summary>
public static class FrontRoomsStructurePoolFinish
{
    // ---------------------------------------------------------------- numbers

    /// <summary>A face is a wall foot when it stands this high over the deck (a step riser, a curb or a kerb is lower).</summary>
    public const float WallMin = .45f;
    /// <summary>A deck level of a hall needs this much dry field tile (m2): the court's terraces and SP6's levels pass, a kerb's top
    /// or a coping does not.</summary>
    public const float DeckMinArea = 2f;
    /// <summary>The cove's reach on the floor (Prop_PoolTile_Cove: r 50 + its 50 mm flat): the floor is tested this far out.</summary>
    public const float CoveReach = .10f;
    /// <summary>Cove rings every 1.2 m (eight tiles), as KitRow's K-02 cove: a long strip interpolated the cove's u with a visible
    /// error at the 0.35 m hero.</summary>
    public const float CoveStep = 1.2f;
    /// <summary>Joint path points every 0.6 m (KitRow's K-02 joint).</summary>
    public const float JointStep = .6f;
    /// <summary>The deck's tile (world-aligned from 0) and a drain's clearance from any wall face.</summary>
    public const float Tile = .15f, DrainClear = .35f;
    /// <summary>The district's cell (the joints' lattice).</summary>
    public const float Cell = 3f;

    // ---------------------------------------------------------------- the district's own finished geometry

    internal sealed class Tri
    {
        public Vector3 a, b, c, n;
        public float y0, y1;
        public Vector2 lo, hi;
    }

    /// <summary>What the district's finished tile and water meshes are (district-local), read once per finish pass.</summary>
    public sealed class Scan
    {
        public readonly FrontRoomsStructuresKit Kit;
        internal readonly List<Tri> floors = new List<Tri>();
        internal readonly List<Tri> walls = new List<Tri>();
        internal readonly List<Tri> waters = new List<Tri>();
        readonly Dictionary<long, List<int>> floorGrid = new Dictionary<long, List<int>>();
        readonly Dictionary<long, List<int>> wallGrid = new Dictionary<long, List<int>>();
        readonly Dictionary<long, List<int>> waterGrid = new Dictionary<long, List<int>>();
        const float G = .5f;
        public int TileTriangles { get; private set; }

        public Scan(FrontRoomsStructuresKit kit)
        {
            Kit = kit;
            var root = kit != null ? kit.Root : null;
            if (root == null) return;
            var toLocal = root.worldToLocalMatrix;
            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var idx = new List<int>();
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                var m = r.sharedMaterial;
                if (m == null) continue;
                var field = IsField(m);
                var water = !field && IsWater(m);
                if (!field && !water) continue;
                var mf = r.GetComponent<MeshFilter>();
                var mesh = mf != null ? mf.sharedMesh : null;
                if (mesh == null || !mesh.isReadable) continue;
                var mat = toLocal * r.transform.localToWorldMatrix;
                verts.Clear(); norms.Clear();
                mesh.GetVertices(verts);
                mesh.GetNormals(norms);
                for (var sm = 0; sm < mesh.subMeshCount; sm++)
                {
                    idx.Clear();
                    mesh.GetTriangles(idx, sm);
                    for (var i = 0; i + 2 < idx.Count; i += 3)
                    {
                        var t = new Tri { a = mat.MultiplyPoint3x4(verts[idx[i]]), b = mat.MultiplyPoint3x4(verts[idx[i + 1]]), c = mat.MultiplyPoint3x4(verts[idx[i + 2]]) };
                        var nn = norms.Count == verts.Count ? norms[idx[i]] + norms[idx[i + 1]] + norms[idx[i + 2]] : Vector3.Cross(t.b - t.a, t.c - t.a);
                        nn = mat.MultiplyVector(nn);
                        if (nn.sqrMagnitude < 1e-12f) continue;
                        t.n = nn.normalized;
                        t.y0 = Mathf.Min(t.a.y, Mathf.Min(t.b.y, t.c.y));
                        t.y1 = Mathf.Max(t.a.y, Mathf.Max(t.b.y, t.c.y));
                        t.lo = new Vector2(Mathf.Min(t.a.x, Mathf.Min(t.b.x, t.c.x)), Mathf.Min(t.a.z, Mathf.Min(t.b.z, t.c.z)));
                        t.hi = new Vector2(Mathf.Max(t.a.x, Mathf.Max(t.b.x, t.c.x)), Mathf.Max(t.a.z, Mathf.Max(t.b.z, t.c.z)));
                        if (water)
                        {
                            if (t.n.y > .99f && t.y1 - t.y0 < .002f) Add(waters, waterGrid, t);
                            continue;
                        }
                        TileTriangles++;
                        if (t.n.y > .985f && t.y1 - t.y0 < .002f) Add(floors, floorGrid, t);
                        else if (Mathf.Abs(t.n.y) < .08f) Add(walls, wallGrid, t);
                    }
                }
            }
        }

        static long Key(int i, int j) => ((long)(i + 100000) << 32) | (uint)(j + 100000);

        static void Add(List<Tri> list, Dictionary<long, List<int>> grid, Tri t)
        {
            var id = list.Count;
            list.Add(t);
            for (var i = Mathf.FloorToInt(t.lo.x / G); i <= Mathf.FloorToInt(t.hi.x / G); i++)
                for (var j = Mathf.FloorToInt(t.lo.y / G); j <= Mathf.FloorToInt(t.hi.y / G); j++)
                {
                    var k = Key(i, j);
                    if (!grid.TryGetValue(k, out var l)) grid[k] = l = new List<int>();
                    l.Add(id);
                }
        }

        static bool Inside(Tri t, float x, float z)
        {
            if (x < t.lo.x - 1e-4f || x > t.hi.x + 1e-4f || z < t.lo.y - 1e-4f || z > t.hi.y + 1e-4f) return false;
            float d1 = Side(x, z, t.a, t.b), d2 = Side(x, z, t.b, t.c), d3 = Side(x, z, t.c, t.a);
            var neg = d1 < -1e-7f || d2 < -1e-7f || d3 < -1e-7f;
            var pos = d1 > 1e-7f || d2 > 1e-7f || d3 > 1e-7f;
            return !(neg && pos);
        }

        static float Side(float x, float z, Vector3 p, Vector3 q) => (x - q.x) * (p.z - q.z) - (p.x - q.x) * (z - q.z);

        /// <summary>A field-tile floor at height y (within 4 mm) under the plan point.</summary>
        public bool FloorAt(float x, float y, float z)
        {
            if (!floorGrid.TryGetValue(Key(Mathf.FloorToInt(x / G), Mathf.FloorToInt(z / G)), out var l)) return false;
            foreach (var id in l)
            {
                var t = floors[id];
                if (Mathf.Abs(t.y0 - y) > .004f) continue;
                if (Inside(t, x, z)) return true;
            }
            return false;
        }

        /// <summary>A water surface at or over height y (within 1.5 m) over the plan point: the floor there is wet.</summary>
        public bool WetAt(float x, float y, float z)
        {
            if (!waterGrid.TryGetValue(Key(Mathf.FloorToInt(x / G), Mathf.FloorToInt(z / G)), out var l)) return false;
            foreach (var id in l)
            {
                var t = waters[id];
                if (t.y0 < y - .004f || t.y0 > y + 1.5f) continue;
                if (Inside(t, x, z)) return true;
            }
            return false;
        }

        /// <summary>A dry field-tile floor at y under the plan point.</summary>
        public bool DryDeck(float x, float y, float z) => FloorAt(x, y, z) && !WetAt(x, y, z);

        /// <summary>Whether a field-tile wall face standing over height y (from y - 2 cm to y + 0.3) comes within 'd' of the plan point.</summary>
        public bool WallNear(float x, float y, float z, float d)
        {
            var p = new Vector2(x, z);
            for (var i = Mathf.FloorToInt((x - d) / G); i <= Mathf.FloorToInt((x + d) / G); i++)
                for (var j = Mathf.FloorToInt((z - d) / G); j <= Mathf.FloorToInt((z + d) / G); j++)
                {
                    if (!wallGrid.TryGetValue(Key(i, j), out var l)) continue;
                    foreach (var id in l)
                    {
                        var t = walls[id];
                        if (t.y1 < y + .02f || t.y0 > y + .3f) continue;
                        if (p.x < t.lo.x - d || p.x > t.hi.x + d || p.y < t.lo.y - d || p.y > t.hi.y + d) continue;
                        if (SegDist(p, new Vector2(t.a.x, t.a.z), new Vector2(t.b.x, t.b.z)) < d || SegDist(p, new Vector2(t.b.x, t.b.z), new Vector2(t.c.x, t.c.z)) < d
                            || SegDist(p, new Vector2(t.c.x, t.c.z), new Vector2(t.a.x, t.a.z)) < d) return true;
                    }
                }
            return false;
        }

        /// <summary>A field-tile wall face near the point (within 'd' of its plane, its normal within 25 degrees of 'facing').</summary>
        public bool FaceAt(Vector3 p, Vector3 facing, float d)
        {
            for (var i = Mathf.FloorToInt((p.x - .3f) / G); i <= Mathf.FloorToInt((p.x + .3f) / G); i++)
                for (var j = Mathf.FloorToInt((p.z - .3f) / G); j <= Mathf.FloorToInt((p.z + .3f) / G); j++)
                {
                    if (!wallGrid.TryGetValue(Key(i, j), out var l)) continue;
                    foreach (var id in l)
                    {
                        var t = walls[id];
                        if (Vector3.Dot(t.n, facing) < .9f || p.y < t.y0 - .01f || p.y > t.y1 + .01f) continue;
                        var off = Vector3.Dot(p - t.a, t.n);
                        if (Mathf.Abs(off) > d) continue;
                        var q = p - t.n * off;
                        if (InTri3(q, t)) return true;
                    }
                }
            return false;
        }

        static bool InTri3(Vector3 q, Tri t)
        {
            var v0 = t.c - t.a;
            var v1 = t.b - t.a;
            var v2 = q - t.a;
            float d00 = Vector3.Dot(v0, v0), d01 = Vector3.Dot(v0, v1), d11 = Vector3.Dot(v1, v1), d20 = Vector3.Dot(v2, v0), d21 = Vector3.Dot(v2, v1);
            var den = d00 * d11 - d01 * d01;
            if (Mathf.Abs(den) < 1e-12f) return false;
            var u = (d11 * d20 - d01 * d21) / den;
            var v = (d00 * d21 - d01 * d20) / den;
            return u >= -1e-3f && v >= -1e-3f && u + v <= 1.001f;
        }

        static float SegDist(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var l2 = ab.sqrMagnitude;
            var f = l2 > 1e-12f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / l2) : 0f;
            return Vector2.Distance(p, a + ab * f);
        }

        /// <summary>The dry deck levels of a hall (district-local box): field-tile floor heights with DeckMinArea of dry floor.</summary>
        public List<float> DeckLevels(Bounds hall)
        {
            var area = new Dictionary<int, float>();
            foreach (var t in floors)
            {
                var cx = (t.a.x + t.b.x + t.c.x) / 3f;
                var cz = (t.a.z + t.b.z + t.c.z) / 3f;
                if (!InPlan(hall, cx, cz) || t.y0 < hall.min.y || t.y0 > hall.max.y) continue;
                if (WetAt(cx, t.y0, cz)) continue;
                var k = Mathf.RoundToInt(t.y0 * 1000f);
                var a = .5f * Mathf.Abs((t.b.x - t.a.x) * (t.c.z - t.a.z) - (t.c.x - t.a.x) * (t.b.z - t.a.z));
                area.TryGetValue(k, out var s);
                area[k] = s + a;
            }
            var levels = new List<float>();
            foreach (var kv in area) if (kv.Value >= DeckMinArea) levels.Add(kv.Key * .001f);
            levels.Sort();
            return levels;
        }

        /// <summary>The height up to which a wall face's plane is tiled over the span of a foot segment (its plane's group).</summary>
        internal float FaceTop(Tri w, Vector3 p0, Vector3 p1)
        {
            var top = w.y1;
            var lo = new Vector2(Mathf.Min(p0.x, p1.x) - .01f, Mathf.Min(p0.z, p1.z) - .01f);
            var hi = new Vector2(Mathf.Max(p0.x, p1.x) + .01f, Mathf.Max(p0.z, p1.z) + .01f);
            var d = Vector3.Dot(w.n, w.a);
            for (var i = Mathf.FloorToInt(lo.x / G); i <= Mathf.FloorToInt(hi.x / G); i++)
                for (var j = Mathf.FloorToInt(lo.y / G); j <= Mathf.FloorToInt(hi.y / G); j++)
                {
                    if (!wallGrid.TryGetValue(Key(i, j), out var l)) continue;
                    foreach (var id in l)
                    {
                        var t = walls[id];
                        if (t.y1 <= top || Vector3.Dot(t.n, w.n) < .999f || Mathf.Abs(Vector3.Dot(t.n, t.a) - d) > .002f) continue;
                        if (t.hi.x < lo.x || t.lo.x > hi.x || t.hi.y < lo.y || t.lo.y > hi.y) continue;
                        if (t.y0 > w.y1 + .01f && t.y0 > top + .01f) continue;
                        top = t.y1;
                    }
                }
            return top;
        }
    }

    static bool InPlan(Bounds b, float x, float z) => x >= b.min.x && x <= b.max.x && z >= b.min.z && z <= b.max.z;

    /// <summary>The pool's field tile (walls and decks): FieldMesh at any water line and its tile kinds by radius, the mosaic, the
    /// LS stand-in. Not the trims (gutter, coping, cove, nosing, tread, riser, cap, bullnose, weir lip, lane).</summary>
    public static bool IsField(Material m)
    {
        var n = m != null && m.name != null ? m.name : "";
        if (n.StartsWith("Structures / pool tile", StringComparison.Ordinal)) return true;
        if (!n.StartsWith("Prop_PoolTile_", StringComparison.Ordinal)) return false;
        return n.IndexOf("Field", StringComparison.Ordinal) >= 0 || n.IndexOf("Mosaic", StringComparison.Ordinal) >= 0;
    }

    /// <summary>A water surface (the district's water quads; the falls' flow sheets stand vertical and are left out by the caller).</summary>
    public static bool IsWater(Material m)
    {
        if (m == null) return false;
        var n = m.name ?? "";
        if (n.IndexOf("water", StringComparison.OrdinalIgnoreCase) >= 0 && n.IndexOf("flow", StringComparison.OrdinalIgnoreCase) < 0) return true;
        var sh = m.shader != null ? m.shader.name : "";
        return sh.IndexOf("Water", StringComparison.OrdinalIgnoreCase) >= 0 && sh.IndexOf("flow", StringComparison.OrdinalIgnoreCase) < 0;
    }

    // ---------------------------------------------------------------- the plan a district hands over

    /// <summary>One district's finish pass: what the owners' code knows (halls, points), built at once after its last Finish.</summary>
    public sealed class Plan
    {
        readonly FrontRoomsStructuresKit kit;
        readonly float gridX, gridZ;
        readonly List<Bounds> halls = new List<Bounds>();
        readonly List<Vector3> drains = new List<Vector3>();
        readonly List<Vector3> marks = new List<Vector3>();
        readonly List<Vector3> markDirs = new List<Vector3>();
        readonly List<BasinSpec> basins = new List<BasinSpec>();
        readonly List<Vector3> buoys = new List<Vector3>(), buoyOut = new List<Vector3>();
        readonly List<Vector3> cams = new List<Vector3>(), camOut = new List<Vector3>();
        public string Report { get; private set; }

        /// <summary>gridX, gridZ: where the district's 3 m cell lines fall on each axis (SP1: x 1.5, z 0; SP2, SP6: 0, 0).</summary>
        public Plan(FrontRoomsStructuresKit kit, float gridX, float gridZ)
        {
            this.kit = kit;
            this.gridX = gridX;
            this.gridZ = gridZ;
        }

        /// <summary>A pool hall (district-local box: its plan on the walls' centre lines, its height round its decks).</summary>
        public Plan Hall(Bounds b) { halls.Add(b); return this; }
        public Plan Drain(Vector3 at) { drains.Add(at); return this; }
        /// <summary>A depth mark behind the water's edge: 'edge' a point on the edge line at the deck's height, 'toDeck' away from the water.</summary>
        public Plan Mark(Vector3 edge, Vector3 toDeck) { marks.Add(edge); markDirs.Add(toDeck); return this; }
        public Plan Basin(BasinSpec b) { basins.Add(b); return this; }
        public Plan RingBuoy(Vector3 wallPoint, Vector3 outward) { buoys.Add(wallPoint); buoyOut.Add(outward); return this; }
        public Plan Cctv(Vector3 wallPoint, Vector3 outward) { cams.Add(wallPoint); camOut.Add(outward); return this; }

        /// <summary>Builds everything (NEW only): the caller finishes the kit right after, so the coves and joints get renderers.</summary>
        public void Build()
        {
            if (kit == null || !kit.New || kit.Root == null) return;
            var s = new Scan(kit);
            var cove = Coves(s, halls);
            var joints = Joints(s, halls, gridX, gridZ);
            var nd = 0;
            foreach (var d in drains) if (DeckDrain(s, d) != null) nd++;
            var nm = 0;
            for (var i = 0; i < marks.Count; i++) if (DepthMark(s, marks[i], markDirs[i])) nm++;
            var nf = 0;
            foreach (var b in basins) nf += BlockBasin(s, b);
            var nb = 0;
            for (var i = 0; i < buoys.Count; i++) if (Place(kit, "Kit_RingBuoy", buoys[i], buoyOut[i]) != null) nb++;
            var nc = 0;
            for (var i = 0; i < cams.Count; i++) if (Place(kit, "Kit_CCTVCamera", cams[i], camOut[i]) != null) nc++;
            Report = kit.District.Code + " pool finish (G2-d): cove " + cove.ToString("0.0") + " m, joints " + joints.ToString("0.0") + " m, deck drains " + nd + "/" + drains.Count
                + ", depth marks " + nm + "/" + marks.Count + ", basin fittings " + nf + ", ring buoys " + nb + "/" + buoys.Count + ", cameras " + nc + "/" + cams.Count
                + (cams.Count > nc ? " (Kit_CCTVCamera not in this build)" : "") + "; tile triangles read " + s.TileTriangles + ".";
            Debug.Log(FrontRoomsStructures.Log + Report);
        }
    }

    /// <summary>A basin built of blocks (SP6): its floor and water heights, the main drain's point, the return inlets' wall points
    /// (at any height: each goes 0.30 m under the water) and the direction each faces into the water.</summary>
    public sealed class BasinSpec
    {
        public float floorY, waterY;
        public Vector3 drain;
        public bool hasDrain;
        public readonly List<Vector3> inlets = new List<Vector3>();
        public readonly List<Vector3> inletInto = new List<Vector3>();

        public BasinSpec(float floorY, float waterY) { this.floorY = floorY; this.waterY = waterY; }
        public BasinSpec Drain(float x, float z) { drain = new Vector3(x, floorY, z); hasDrain = true; return this; }
        public BasinSpec Inlet(float x, float z, Vector3 into) { inlets.Add(new Vector3(x, waterY - .3f, z)); inletInto.Add(into); return this; }
    }

    // ---------------------------------------------------------------- C7: the cove base

    sealed class Run
    {
        public Vector3 a, b, n;
        public bool used;
    }

    /// <summary>The cove base round every dry deck of the halls; returns its length (m).</summary>
    public static float Coves(Scan s, IList<Bounds> halls)
    {
        var kit = s.Kit;
        var coveMat = FrontRoomsPoolTile.Get("Cove");
        if (coveMat == null || halls == null) return 0f;
        var runs = new List<Run>();
        var seen = new HashSet<long>();
        foreach (var hall in halls)
        {
            var levels = s.DeckLevels(hall);
            foreach (var y in levels)
            {
                foreach (var w in s.walls)
                {
                    if (w.y0 > y + .002f || w.y1 < y + .02f) continue;
                    var cx = (w.a.x + w.b.x + w.c.x) / 3f;
                    var cz = (w.a.z + w.b.z + w.c.z) / 3f;
                    if (cx < hall.min.x - 1f || cx > hall.max.x + 1f || cz < hall.min.z - 1f || cz > hall.max.z + 1f) continue;
                    Vector3 p0, p1;
                    if (!Slice(w, y, out p0, out p1)) continue;
                    if (s.FaceTop(w, p0, p1) < y + WallMin) continue;
                    var nh = new Vector3(w.n.x, 0f, w.n.z).normalized;
                    // orient: the room (the face's normal) on the plumb frame's +x
                    var t = p1 - p0;
                    if (Vector3.Dot(new Vector3(t.z, 0f, -t.x), nh) < 0f) { var sw = p0; p0 = p1; p1 = sw; }
                    Passing(s, hall, y, p0, p1, nh, runs, seen);
                }
            }
        }
        // chain: a run's end on the next run's start (2 mm), then sweep each chain
        var total = 0f;
        var byStart = new Dictionary<long, List<int>>();
        var byEnd = new Dictionary<long, List<int>>();
        for (var i = 0; i < runs.Count; i++)
        {
            Index(byStart, PKey(runs[i].a), i);
            Index(byEnd, PKey(runs[i].b), i);
        }
        for (var i = 0; i < runs.Count; i++)
        {
            if (runs[i].used) continue;
            // walk back to the chain's head (a run whose start no other free run ends on), at most once round a loop
            var head = i;
            for (var guard = 0; guard < runs.Count; guard++)
            {
                var prev = Find(runs, byEnd, runs[head].a, false);
                if (prev < 0 || prev == i) break;
                head = prev;
            }
            var path = new List<Vector3> { runs[head].a };
            var cur = head;
            var closed = false;
            for (var guard = 0; guard <= runs.Count; guard++)
            {
                runs[cur].used = true;
                path.Add(runs[cur].b);
                var next = Find(runs, byStart, runs[cur].b, true);
                if (next < 0)
                {
                    closed = Vector3.Distance(runs[cur].b, runs[head].a) < .0025f && cur != head;
                    break;
                }
                cur = next;
            }
            var simple = Simplify(path, closed);
            if (simple.Count < 2) continue;
            if (closed && simple.Count > 3)
            {
                // a closed loop starts and ends at its first side's middle (one seamless butt there, every corner mitred)
                var m0 = (simple[0] + simple[1]) * .5f;
                var loop = new List<Vector3> { m0 };
                for (var k = 1; k < simple.Count; k++) loop.Add(simple[k]);
                loop.Add(simple[0]);
                loop.Add(m0);
                simple = loop;
            }
            var dense = Dense(simple, CoveStep);
            kit.CoveBase(dense, coveMat);
            if (!closed)
            {
                CoveEnd(kit, dense[0], dense[1] - dense[0], coveMat, -1f);
                CoveEnd(kit, dense[dense.Count - 1], dense[dense.Count - 1] - dense[dense.Count - 2], coveMat, 1f);
            }
            for (var k = 1; k < dense.Count; k++) total += Vector3.Distance(dense[k - 1], dense[k]);
        }
        return total;
    }

    static void Index(Dictionary<long, List<int>> map, long key, int i)
    {
        if (!map.TryGetValue(key, out var l)) map[key] = l = new List<int>();
        l.Add(i);
    }

    // a free run whose start (or end) lies within 2.5 mm of p: the key's cell and its neighbours
    static int Find(List<Run> runs, Dictionary<long, List<int>> map, Vector3 p, bool start)
    {
        for (var dx = -1; dx <= 1; dx++)
            for (var dz = -1; dz <= 1; dz++)
                for (var dy = -1; dy <= 1; dy++)
                {
                    var q = p + new Vector3(dx * .002f, dy * .002f, dz * .002f);
                    if (!map.TryGetValue(PKey(q), out var l)) continue;
                    foreach (var i in l)
                    {
                        if (runs[i].used) continue;
                        if (Vector3.Distance(start ? runs[i].a : runs[i].b, p) < .0025f) return i;
                    }
                }
        return -1;
    }

    static long PKey(Vector3 p) => ((long)Mathf.RoundToInt(p.x * 500f) << 40) ^ ((long)Mathf.RoundToInt(p.z * 500f) << 20) ^ Mathf.RoundToInt(p.y * 500f);

    // a triangle's segment on the plane y (null when it does not cross it)
    static bool Slice(Tri w, float y, out Vector3 p0, out Vector3 p1)
    {
        p0 = p1 = Vector3.zero;
        var pts = new List<Vector3>(3);
        var v = new[] { w.a, w.b, w.c };
        for (var i = 0; i < 3; i++)
        {
            var a = v[i];
            var b = v[(i + 1) % 3];
            var da = a.y - y;
            var db = b.y - y;
            if (Mathf.Abs(da) < 1e-4f) { AddUnique(pts, new Vector3(a.x, y, a.z)); continue; }
            if (da * db < 0f && Mathf.Abs(db) >= 1e-4f)
            {
                var f = da / (da - db);
                AddUnique(pts, new Vector3(Mathf.Lerp(a.x, b.x, f), y, Mathf.Lerp(a.z, b.z, f)));
            }
        }
        if (pts.Count < 2) return false;
        p0 = pts[0];
        p1 = pts[1];
        return (p1 - p0).sqrMagnitude > 1e-8f;
    }

    static void AddUnique(List<Vector3> l, Vector3 p)
    {
        foreach (var q in l) if ((q - p).sqrMagnitude < 1e-10f) return;
        l.Add(p);
    }

    // the stretches of a foot segment with a dry deck in front of it inside the hall, ends found to 1 mm
    static void Passing(Scan s, Bounds hall, float y, Vector3 p0, Vector3 p1, Vector3 n, List<Run> runs, HashSet<long> seen)
    {
        var len = Vector3.Distance(p0, p1);
        var steps = Mathf.Max(1, Mathf.CeilToInt(len / .05f));
        bool Ok(float f)
        {
            var p = Vector3.Lerp(p0, p1, f);
            var q1 = p + n * .03f;
            var q2 = p + n * (CoveReach - .01f);
            return InPlan(hall, q1.x, q1.z) && s.DryDeck(q1.x, y, q1.z) && s.DryDeck(q2.x, y, q2.z);
        }
        var fs = new float[steps + 1];
        var ok = new bool[steps + 1];
        for (var i = 0; i <= steps; i++)
        {
            // the ends tested 1 mm in, so a segment that meets its neighbour at a corner still counts there
            fs[i] = Mathf.Clamp(i / (float)steps, .001f / Mathf.Max(len, .001f), 1f - .001f / Mathf.Max(len, .001f));
            ok[i] = Ok(fs[i]);
        }
        var start = -1f;
        for (var i = 0; i <= steps; i++)
        {
            if (ok[i] && start < 0f) start = i == 0 ? 0f : Edge(Ok, fs[i - 1], fs[i]);
            var endHere = start >= 0f && (!ok[i] || i == steps);
            if (!endHere) continue;
            var end = ok[i] ? 1f : Edge(Ok, fs[i], fs[i - 1]);
            if (end - start > 1e-4f)
            {
                var a = Vector3.Lerp(p0, p1, start);
                var b = Vector3.Lerp(p0, p1, end);
                // the same stretch from two coplanar faces (overlapping walls) counts once
                var key = PKey(a) * 31 ^ PKey(b);
                if (seen.Add(key)) runs.Add(new Run { a = a, b = b, n = n });
            }
            start = -1f;
        }
    }

    // bisect between a passing fraction and a failing one
    static float Edge(Func<float, bool> ok, float pass, float fail)
    {
        for (var k = 0; k < 10; k++)
        {
            var mid = (pass + fail) * .5f;
            if (ok(mid)) pass = mid;
            else fail = mid;
        }
        return pass;
    }

    // merge collinear points (a straight run in one piece; the facets of a curve and the rounds of an arris stay)
    static List<Vector3> Simplify(List<Vector3> path, bool closed)
    {
        var o = new List<Vector3>();
        foreach (var p in path)
        {
            if (o.Count > 0 && Vector3.Distance(o[o.Count - 1], p) < 1e-4f) continue;
            if (o.Count >= 2)
            {
                var a = o[o.Count - 2];
                var b = o[o.Count - 1];
                var d0 = (b - a).normalized;
                var d1 = (p - b).normalized;
                if (Vector3.Dot(d0, d1) > .99999f) { o[o.Count - 1] = p; continue; }
            }
            o.Add(p);
        }
        if (closed && o.Count > 2 && Vector3.Distance(o[0], o[o.Count - 1]) < .003f) o.RemoveAt(o.Count - 1);
        return o;
    }

    static List<Vector3> Dense(IList<Vector3> path, float step)
    {
        var o = new List<Vector3>();
        for (var i = 0; i < path.Count; i++)
        {
            if (i > 0)
            {
                var n = Mathf.CeilToInt(Vector3.Distance(path[i - 1], path[i]) / Mathf.Max(step, .05f));
                for (var k = 1; k < n; k++) o.Add(Vector3.Lerp(path[i - 1], path[i], k / (float)n));
            }
            o.Add(path[i]);
        }
        return o;
    }

    // the cove's open end closed with its own outline (the section CoveBase sweeps: Cove(.05, .05, 6) laid 2 mm proud, skirts 4 mm)
    static void CoveEnd(FrontRoomsStructuresKit kit, Vector3 at, Vector3 along, Material m, float sign)
    {
        along.y = 0f;
        if (along.sqrMagnitude < 1e-8f) return;
        along.Normalize();
        var r = new Vector3(along.z, 0f, -along.x);
        const float Lift = .002f, Skirt = .004f;
        var c = FrontRoomsStructureProfiles.Cove(.05f, .05f, 6);
        var poly = new List<Vector2> { new Vector2(c.P[0].x + Lift, Lift - Skirt) };
        foreach (var q in c.P) poly.Add(q + new Vector2(Lift, Lift));
        poly.Add(new Vector2(Lift - Skirt, c.P[c.P.Length - 1].y + Lift));
        poly.Add(new Vector2(Lift - Skirt, Lift - Skirt));
        var arr = poly.ToArray();
        var order = FrontRoomsStructureProfiles.EarClip(arr);
        var v = new List<Vector3>();
        var nn = new List<Vector3>();
        var uv = new List<Vector2>();
        var tris = new List<int>();
        var nrm = along * sign;
        foreach (var q in arr)
        {
            v.Add(at + r * q.x + Vector3.up * q.y);
            nn.Add(nrm);
            uv.Add(new Vector2(q.x, q.y));
        }
        for (var i = 0; i + 2 < order.Count; i += 3)
        {
            int a = order[i], b = order[i + 1], cc = order[i + 2];
            if (Vector3.Dot(Vector3.Cross(v[b] - v[a], v[cc] - v[a]), nrm) < 0f) { var t = b; b = cc; cc = t; }
            tris.Add(a); tris.Add(b); tris.Add(cc);
        }
        kit.Mesh.Append(v, nn, uv, tris, m, false);
    }

    // ---------------------------------------------------------------- C7: the movement joints

    /// <summary>EJ171 joints on every dry deck of the halls along the cell lines (x = gridX + 3k, z = gridZ + 3k); returns their
    /// length (m).</summary>
    public static float Joints(Scan s, IList<Bounds> halls, float gridX, float gridZ)
    {
        var kit = s.Kit;
        var total = 0f;
        if (halls == null) return 0f;
        var drawn = new HashSet<long>();
        foreach (var hall in halls)
        {
            var levels = s.DeckLevels(hall);
            foreach (var y in levels)
                for (var axis = 0; axis < 2; axis++)
                {
                    float a0 = axis == 0 ? hall.min.x : hall.min.z, a1 = axis == 0 ? hall.max.x : hall.max.z;
                    float b0 = axis == 0 ? hall.min.z : hall.min.x, b1 = axis == 0 ? hall.max.z : hall.max.x;
                    var g0 = axis == 0 ? gridX : gridZ;
                    for (var k = Mathf.CeilToInt((a0 - g0) / Cell - 1e-4f); g0 + k * Cell <= a1 + 1e-4f; k++)
                    {
                        var at = g0 + k * Cell;
                        // a line on a hall's own wall line runs under that wall: skip the hall's boundary lines
                        if (Mathf.Abs(at - a0) < .01f || Mathf.Abs(at - a1) < .01f) continue;
                        var line = ((long)axis << 58) ^ ((long)Mathf.RoundToInt(at * 100f) << 38) ^ ((long)Mathf.RoundToInt(y * 1000f) << 20);
                        var steps = Mathf.CeilToInt((b1 - b0) / .05f);
                        var from = float.NaN;
                        for (var i = 0; i <= steps; i++)
                        {
                            var b = Mathf.Min(b1, b0 + i * .05f);
                            var x = axis == 0 ? at : b;
                            var z = axis == 0 ? b : at;
                            // a sample another hall drew already (halls that overlap: C9 in the island room) is not drawn twice
                            var ok = s.DryDeck(x, y, z) && !drawn.Contains(line ^ Mathf.RoundToInt(b * 20f));
                            if (ok && float.IsNaN(from)) from = b;
                            if ((!ok || i == steps) && !float.IsNaN(from))
                            {
                                var to = ok ? b : b - .05f;
                                if (to - from > .3f)
                                {
                                    var pa = axis == 0 ? new Vector3(at, y + .0006f, from) : new Vector3(from, y + .0006f, at);
                                    var pb = axis == 0 ? new Vector3(at, y + .0006f, to) : new Vector3(to, y + .0006f, at);
                                    kit.SealantJoint(Dense(new[] { pa, pb }, JointStep), Vector3.up);
                                    total += to - from;
                                    for (var bb = from; bb <= to + 1e-3f; bb += .05f) drawn.Add(line ^ Mathf.RoundToInt(bb * 20f));
                                }
                                from = float.NaN;
                            }
                        }
                    }
                }
        }
        return total;
    }

    // ---------------------------------------------------------------- C9: deck drains, block basins' fittings

    static Vector3 W(FrontRoomsStructuresKit kit, Vector3 local) => kit.Root != null ? local + kit.Root.position : local;

    // the centre of the 150 mm tile (world-aligned from 0) a district point lies on
    static Vector3 TileCentre(FrontRoomsStructuresKit kit, Vector3 local)
    {
        var w = W(kit, local);
        var cx = (Mathf.Floor(w.x / Tile) + .5f) * Tile;
        var cz = (Mathf.Floor(w.z / Tile) + .5f) * Tile;
        return new Vector3(local.x + (cx - w.x), local.y, local.z + (cz - w.z));
    }

    /// <summary>Kit_FloorDrain on the tile centre nearest the point (searched to 0.6 m) on a dry deck at the point's height, clear of walls.</summary>
    public static GameObject DeckDrain(Scan s, Vector3 at)
    {
        var kit = s.Kit;
        if (!FrontRoomsKitLibrary.Has("Kit_FloorDrain")) return null;
        for (var ring = 0; ring <= 4; ring++)
            for (var dx = -ring; dx <= ring; dx++)
                for (var dz = -ring; dz <= ring; dz++)
                {
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz)) != ring) continue;
                    var p = TileCentre(kit, at + new Vector3(dx * Tile, 0f, dz * Tile));
                    if (!s.DryDeck(p.x, at.y, p.z) || s.WallNear(p.x, at.y, p.z, DrainClear)) continue;
                    if (!s.DryDeck(p.x + .1f, at.y, p.z) || !s.DryDeck(p.x - .1f, at.y, p.z) || !s.DryDeck(p.x, at.y, p.z + .1f) || !s.DryDeck(p.x, at.y, p.z - .1f)) continue;
                    return Spawn(kit, "Kit_FloorDrain", new Vector3(p.x, at.y, p.z), Quaternion.identity);
                }
        FrontRoomsPoolMaterials.LogOnce("G2-d drain " + kit.District.Code + at, "pool finish: no dry deck clear of walls near " + at + " for a deck drain.");
        return null;
    }

    /// <summary>The main drain (in its Lane square) and the return inlets of a basin built of blocks; returns the fittings placed.</summary>
    public static int BlockBasin(Scan s, BasinSpec b)
    {
        var kit = s.Kit;
        var n = 0;
        if (b.hasDrain && FrontRoomsKitLibrary.Has("Kit_MainDrainGrate"))
        {
            var c = TileCentre(kit, b.drain);
            if (s.FloorAt(c.x, b.floorY, c.z) && !s.WallNear(c.x, b.floorY, c.z, .3f))
            {
                var lane = FrontRoomsPoolTile.ForWater("Lane", W(kit, new Vector3(0f, b.waterY, 0f)).y);
                const float F = .225f;
                var y = b.floorY + .001f;
                if (lane != null)
                    kit.QuadPlanar(new Vector3(c.x - F, y, c.z - F), new Vector3(c.x - F, y, c.z + F), new Vector3(c.x + F, y, c.z + F), new Vector3(c.x + F, y, c.z - F), Vector3.up, lane);
                if (Spawn(kit, "Kit_MainDrainGrate", new Vector3(c.x, y, c.z), Quaternion.identity) != null) n++;
            }
            else FrontRoomsPoolMaterials.LogOnce("G2-d main drain " + kit.District.Code + b.drain, "pool finish: no clear basin floor at " + b.drain + " for a main drain.");
        }
        if (!FrontRoomsKitLibrary.Has("Kit_ReturnInlet")) return n;
        for (var i = 0; i < b.inlets.Count; i++)
        {
            var p = b.inlets[i];
            var into = b.inletInto[i];
            into.y = 0f;
            into.Normalize();
            if (p.y < b.floorY + .15f) continue;
            var along = new Vector3(into.z, 0f, -into.x);
            // clear of a niche: slide along the wall away from any underwater lamp within 0.7 m (as the kit's FlushBasins)
            for (var tries = 0; tries < 3 && NicheNear(kit, p, .7f); tries++) p += along * .75f;
            if (NicheNear(kit, p, .7f) || !s.FaceAt(p, into, .01f)) continue;
            if (Spawn(kit, "Kit_ReturnInlet", p, Quaternion.LookRotation(into, Vector3.up)) != null) n++;
        }
        return n;
    }

    static bool NicheNear(FrontRoomsStructuresKit kit, Vector3 local, float d)
    {
        var wp = W(kit, local);
        foreach (var l in kit.Lights)
        {
            if (l == null || l.transform.parent == null || l.transform.parent.name != "Lamp · underwater") continue;
            var p = l.transform.position;
            if (new Vector2(p.x - wp.x, p.z - wp.z).magnitude < d && Mathf.Abs(p.y - wp.y) < 1f) return true;
        }
        return false;
    }

    // ---------------------------------------------------------------- C11: depth marks, the ring buoy, the camera, the post

    /// <summary>
    /// A framed blank depth mark behind a water's edge (TM 5-662 2-5: "on the edge of the deck next to the pool"): 4 x 3 whole tiles
    /// on the deck's grid, their near side 0.25 m behind the edge line (clear of the edge's grate and curb), the outer ten in the
    /// lane colour (InlayTile: the unglazed deck tile), the middle 2 x 1 the deck's own (blank: the numerals are not ours). False when the deck is not dry and clear there.
    /// </summary>
    public static bool DepthMark(Scan s, Vector3 edge, Vector3 toDeck)
    {
        // (the frame's tile: InlayTile, below)
        var kit = s.Kit;
        toDeck.y = 0f;
        if (toDeck.sqrMagnitude < 1e-6f) return false;
        toDeck.Normalize();
        var lane = InlayTile();
        if (lane == null) return false;
        var alongX = Mathf.Abs(toDeck.z) > Mathf.Abs(toDeck.x);
        var sd = alongX ? Mathf.Sign(toDeck.z) : Mathf.Sign(toDeck.x);
        var w = W(kit, edge);
        // the deck axis: the first whole tile line 0.25 m or more behind the edge line; the along axis: 4 tiles centred on the point
        var e = (alongX ? w.z : w.x) + sd * .25f;
        var d0 = sd > 0f ? Mathf.Ceil(e / Tile - 1e-3f) * Tile : Mathf.Floor(e / Tile + 1e-3f) * Tile;
        var a0 = Mathf.Round((alongX ? w.x : w.z) / Tile) * Tile - 2f * Tile;
        var off = alongX ? new Vector2(w.x - edge.x, w.z - edge.z) : new Vector2(w.z - edge.z, w.x - edge.x);
        var y = edge.y;
        var cells = new List<Vector4>();
        for (var i = 0; i < 4; i++)
            for (var j = 0; j < 3; j++)
            {
                if (i >= 1 && i <= 2 && j == 1) continue;
                float u0 = a0 + i * Tile, u1 = u0 + Tile;
                float v0 = d0 + sd * j * Tile, v1 = v0 + sd * Tile;
                cells.Add(new Vector4(u0 - off.x, Mathf.Min(v0, v1) - off.y, u1 - off.x, Mathf.Max(v0, v1) - off.y));
            }
        // the whole inlay on a dry deck, no wall within it
        foreach (var c in cells)
        {
            float cx = (c.x + c.z) * .5f, cv = (c.y + c.w) * .5f;
            var px = alongX ? cx : cv;
            var pz = alongX ? cv : cx;
            if (!s.DryDeck(px, y, pz) || s.WallNear(px, y, pz, .1f)) return false;
        }
        var top = y + .001f;
        foreach (var c in cells)
        {
            Vector3 p00, p01, p11, p10;
            if (alongX)
            {
                p00 = new Vector3(c.x, top, c.y); p01 = new Vector3(c.x, top, c.w); p11 = new Vector3(c.z, top, c.w); p10 = new Vector3(c.z, top, c.y);
            }
            else
            {
                p00 = new Vector3(c.y, top, c.x); p01 = new Vector3(c.y, top, c.z); p11 = new Vector3(c.w, top, c.z); p10 = new Vector3(c.w, top, c.x);
            }
            kit.QuadPlanar(p00, p01, p11, p10, Vector3.up, lane);
        }
        return true;
    }

    static Material inlayTile, inlaySource;

    /// <summary>
    /// The depth mark's frame tile (G2-d render, 2026-11-10): the pool family's unglazed abrasive 150 mm deck tile (Prop_PoolTile_Deck,
    /// world-planar on the same 150 mm grid as the deck it is set in: ten whole tiles, as a slip-resistant deck inlay is laid) in the
    /// lane colour (Prop_PoolTile_Lane's base colour), its body (_EdgeColor, where the tile data thins the glaze) the same colour and
    /// no wear (_TileWear 0: the deck tile is the club's, worn, and its dirt is brown): the pool level is pristine. Why not the Lane
    /// mosaic itself: its 25 mm grid carries a quarter of light grout and a little brown wear, which the texture's far mips average
    /// to a beige mat (SP1's inlays read beige from 3 m in the first render); the deck tile with only its own body showed the same
    /// cream at 3 m (its white body and dirt in the far mips). Joints stay the deck's own grout (4 mm, one joint every 150 mm).
    /// One instance per source material (the phone twin gets its own); the Lane tile when the deck tile is not in the build.
    /// </summary>
    static Material InlayTile()
    {
        var lane = FrontRoomsPoolTile.Get("Lane");
        var deck = FrontRoomsPoolTile.Get("Deck");
        if (deck == null) return lane;
        if (inlayTile != null && inlaySource == deck) return inlayTile;
        inlayTile = new Material(deck) { name = deck.name + " (depth mark)" };
        var c = lane != null && lane.HasProperty("_BaseColor") ? lane.GetColor("_BaseColor") : new Color(.1f, .12f, .16f, 1f);
        if (inlayTile.HasProperty("_BaseColor")) inlayTile.SetColor("_BaseColor", c);
        if (inlayTile.HasProperty("_EdgeColor")) inlayTile.SetColor("_EdgeColor", c);
        if (inlayTile.HasProperty("_TileWear")) inlayTile.SetFloat("_TileWear", 0f);
        // matte as an abrasive deck tile is (smoothness about 0.14 from the deck's 0.31): SP1's lay-in grid is cream and bright right
        // over the deck, and a dark face at a walker's angle shows mostly what it reflects
        if (inlayTile.HasProperty("_Smoothness")) inlayTile.SetFloat("_Smoothness", .45f);
        inlaySource = deck;
        return inlayTile;
    }

    /// <summary>One of game visual's wall-frame kits (origin on the wall plane, +Z out of the wall, +Y up), render only; null when it is
    /// not in this build (nothing is drawn in its place).</summary>
    public static GameObject Place(FrontRoomsStructuresKit kit, string name, Vector3 wallPoint, Vector3 outward)
    {
        if (kit == null || !FrontRoomsKitLibrary.Has(name)) return null;
        outward.y = 0f;
        if (outward.sqrMagnitude < 1e-6f) outward = Vector3.forward;
        return Spawn(kit, name, wallPoint, Quaternion.LookRotation(outward.normalized, Vector3.up));
    }

    /// <summary>A floor kit (origin on the floor) on the centre of the 150 mm tile under a district point (KIT's exhibit deck: no scan,
    /// the deck there is laid in the same build); render only; null when it is not in this build.</summary>
    public static GameObject FloorKit(FrontRoomsStructuresKit kit, string name, Vector3 at)
    {
        if (kit == null || !kit.New || !FrontRoomsKitLibrary.Has(name)) return null;
        return Spawn(kit, name, TileCentre(kit, at), Quaternion.identity);
    }

    static GameObject Spawn(FrontRoomsStructuresKit kit, string name, Vector3 at, Quaternion rot)
    {
        try { return FrontRoomsKitLibrary.Spawn(name, kit.PartRoot, at, rot, null, false, null, false); }
        catch (Exception e) { FrontRoomsPoolMaterials.LogOnce("G2-d kit " + name, "pool finish: " + name + " failed: " + e.Message); return null; }
    }

    /// <summary>
    /// The slide's dispatcher post (Kit_DispatcherPost) standing on the floor against a wall at 'floorAtWall' (its back on the wall's
    /// face), facing 'outward', its monitor showing the pool's still (Prop_ScreenCRT_On, as the start room's CRT did). Null when the
    /// kit is not in this build: the caller then builds the shelf and CRT as before. Render only.
    /// </summary>
    public static GameObject DispatcherPost(FrontRoomsStructuresKit kit, Vector3 floorAtWall, Vector3 outward)
    {
        var go = Place(kit, "Kit_DispatcherPost", floorAtWall, outward);
        if (go == null) return null;
        var on = FrontRoomsSurfaces.TryGet("Prop_ScreenCRT_On");
        if (on != null)
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var ms = r.sharedMaterials;
                var changed = false;
                for (var i = 0; i < ms.Length; i++)
                    if (ms[i] != null && ms[i].name.StartsWith("Prop_ScreenCRT", StringComparison.Ordinal) && ms[i] != on) { ms[i] = on; changed = true; }
                if (changed) r.sharedMaterials = ms;
            }
        return go;
    }

    // ---------------------------------------------------------------- C13: KIT's edge systems side by side

    /// <summary>
    /// An exhibit basin a bay long for one pool edge system (KIT's tile hall, ask 1: all three side by side): a basin x0..x1 by
    /// zEdge..zBack (the edge along zEdge, the water to +z), its floor at floorY, its water at waterY, the deck at 0; the edge swept
    /// along its open side with closed ends (FrontRoomsStructureCurveTile.EdgeSection), the pool's cove r 50 at the floor, a main
    /// drain in its Lane square and a return inlet in its back wall. Render only: the caller keeps the floor's collider as it was.
    /// </summary>
    public static void EdgeExhibit(FrontRoomsStructuresKit kit, float x0, float x1, float zEdge, float zBack, float floorY, float waterY, FrontRoomsStructureCurveTile.Edge kind)
    {
        if (kit == null || !kit.New) return;
        var tile = FrontRoomsPoolMaterials.Tile;
        var wy = W(kit, new Vector3(0f, waterY, 0f)).y;
        var wet = FrontRoomsPoolTile.ForWater("FieldMesh", wy);
        if (wet == null) wet = tile;
        // the basin: its floor, its front wall under the edge, its back wall 1 mm proud of the hall's wall, its sides on the plinths'
        // faces; render only
        kit.Box(new Vector3(x0, floorY - .25f, zEdge), new Vector3(x1, floorY, zBack), wet, false);
        kit.Box(new Vector3(x0, floorY - .25f, zEdge - .2f), new Vector3(x1, -.2f, zEdge), wet, false);
        kit.Box(new Vector3(x0, floorY - .25f, zBack - .001f), new Vector3(x1, -.001f, zBack + .1f), wet, false);
        kit.Box(new Vector3(x0 - .1f, floorY - .25f, zEdge - .2f), new Vector3(x0, -.001f, zBack + .1f), wet, false);
        kit.Box(new Vector3(x1, floorY - .25f, zEdge - .2f), new Vector3(x1 + .1f, -.001f, zBack + .1f), wet, false);
        kit.Water(x0, zEdge, x1, zBack, waterY);
        var cove = FrontRoomsPoolTile.ForWater("Cove", wy);
        if (cove != null)
        {
            var zm = (zEdge + zBack) * .5f;
            kit.CoveBase(new[] { new Vector3(x0, floorY, zm), new Vector3(x0, floorY, zBack), new Vector3(x1, floorY, zBack), new Vector3(x1, floorY, zEdge),
                new Vector3(x0, floorY, zEdge), new Vector3(x0, floorY, zm) }, cove);
        }
        // the edge: the water on the path's left (the plumb frame's +x over the deck, toward -z): x0 to x1 along +x
        var path = new List<Vector3> { new Vector3(x0, 0f, zEdge), new Vector3(x1, 0f, zEdge) };
        var section = FrontRoomsStructureCurveTile.EdgeSection(kind, .32f, out var tileKind);
        var mat = FrontRoomsPoolTile.ForWater(tileKind, wy);
        if (mat == null) mat = tile;
        var o = FrontRoomsStructureProfiles.SweepOptions.Default;
        o.plumb = true;
        o.creaseDeg = 10f;
        o.u0 = FrontRoomsStructureProfiles.WorldU(W(kit, path[0]), path[1] - path[0]);
        var v = new List<Vector3>();
        var n = new List<Vector3>();
        var uv = new List<Vector2>();
        var t = new List<int>();
        FrontRoomsStructureProfiles.Sweep(v, n, uv, t, path, Vector3.up, section, o);
        kit.Mesh.Append(v, n, uv, t, mat, false);
        EdgeEnd(kit, path[0], path[1] - path[0], section, mat, -1f);
        EdgeEnd(kit, path[1], path[1] - path[0], section, mat, 1f);
        // one of each fitting: the main drain in its Lane square at the floor's centre, a return inlet in the back wall
        var centre = TileCentre(kit, new Vector3((x0 + x1) * .5f, floorY, (zEdge + zBack) * .5f));
        var lane = FrontRoomsPoolTile.ForWater("Lane", wy);
        const float F = .225f;
        var ly = floorY + .001f;
        if (lane != null)
            kit.QuadPlanar(new Vector3(centre.x - F, ly, centre.z - F), new Vector3(centre.x - F, ly, centre.z + F), new Vector3(centre.x + F, ly, centre.z + F), new Vector3(centre.x + F, ly, centre.z - F), Vector3.up, lane);
        if (FrontRoomsKitLibrary.Has("Kit_MainDrainGrate")) Spawn(kit, "Kit_MainDrainGrate", new Vector3(centre.x, ly, centre.z), Quaternion.identity);
        if (FrontRoomsKitLibrary.Has("Kit_ReturnInlet") && waterY - .3f > floorY + .15f)
            Spawn(kit, "Kit_ReturnInlet", new Vector3((x0 + x1) * .5f + .75f, waterY - .3f, zBack - .001f), Quaternion.LookRotation(Vector3.back, Vector3.up));
    }

    // an open edge sweep's end: the section's outline closed down its body (as the kit's EdgeEnd)
    static void EdgeEnd(FrontRoomsStructuresKit kit, Vector3 at, Vector3 along, FrontRoomsStructureProfiles.Section s, Material m, float sign)
    {
        along.y = 0f;
        if (along.sqrMagnitude < 1e-8f || m == null) return;
        along.Normalize();
        var r = Vector3.Cross(Vector3.up, along).normalized;
        var count = s.P.Length;
        var poly = new Vector2[count + 1];
        for (var i = 0; i < count; i++) poly[i] = s.P[i];
        poly[count] = new Vector2(s.P[0].x, s.P[count - 1].y);
        var order = FrontRoomsStructureProfiles.EarClip(poly);
        var v = new List<Vector3>();
        var nn = new List<Vector3>();
        var uv = new List<Vector2>();
        var tris = new List<int>();
        var nrm = along * sign;
        foreach (var q in poly)
        {
            v.Add(at + r * q.x + Vector3.up * q.y);
            nn.Add(nrm);
            uv.Add(new Vector2(q.x, q.y));
        }
        for (var i = 0; i + 2 < order.Count; i += 3)
        {
            int a = order[i], b = order[i + 1], c = order[i + 2];
            if (Vector3.Dot(Vector3.Cross(v[b] - v[a], v[c] - v[a]), nrm) < 0f) { var tmp = b; b = c; c = tmp; }
            tris.Add(a); tris.Add(b); tris.Add(c);
        }
        kit.Mesh.Append(v, nn, uv, tris, m, false);
    }
}
