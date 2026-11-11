using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// The structures scene's mesh builder (scene 4, RULES 5b): boxes and quads merged per material and per shell group, with
/// UVs in metres from world (0, 0) on the face's own plane (as the map's planar projection, so the pool tile's joints and
/// every world-projected game surface land on the cell lattice: district roots sit on 192 m multiples), and one collision
/// mesh for everything marked solid. No GameObject per box: Finish makes one renderer per group and one MeshCollider.
/// A group carries the shell shader's _CeilingHeight (its ceiling in world metres) and, off world y 0, _FloorGrime 0
/// (VISUAL_STANDARD; 70 section 2). Used by the hub now and by the Spaces stage's kit.
/// </summary>
public sealed class FrontRoomsStructuresMesh
{
    sealed class Part
    {
        public readonly List<Vector3> v = new List<Vector3>();
        public readonly List<Vector3> n = new List<Vector3>();
        public readonly List<Vector2> uv = new List<Vector2>();
        public readonly List<int> t = new List<int>();
        // NEW only: UV1, a wall's own value for the paper at the wrong scale (Red's ask 4: x its lot 0..1, 0 = not a wall; y the
        // wall's foot, metres): read by the Surface's scale terms (structures_scale_terms), zeros elsewhere
        public readonly List<Vector2> wall = new List<Vector2>();
    }

    struct GroupKey
    {
        public Material material;
        public float ceiling;
        public bool floorGrimeOff, shadows;
        // K2 (structures G2-a, FR-1): the hall a piece lies in (Zones; 0 = none, TODAY's key)
        public int zone;
    }

    readonly Transform root;
    readonly Dictionary<GroupKey, Part> groups = new Dictionary<GroupKey, Part>(new KeyCompare());
    readonly Part collision = new Part();
    readonly Vector3 worldOffset;

    /// <summary>The shell's ceiling (world metres) given to groups made from now on; 2.9 by default.</summary>
    public float Ceiling = 2.9f;
    /// <summary>Groups made from now on have their floor grime turned off (a floor that is not at world y 0).</summary>
    public bool FloorGrimeOff;
    /// <summary>Groups made from now on cast shadows (default true).</summary>
    public bool CastShadows = true;
    /// <summary>
    /// K2 (structures G2-a; the K1 review's FR-1): the halls of a district (root-local boxes). A piece whose centre lies in one goes
    /// into that hall's own renderers, so its metals, glass and trims sit inside the hall's reflection probe instead of one
    /// district-wide renderer taking another room's; the bevels still solve over the whole district in one Finish. The pool tile
    /// (every Prop_PoolTile material: the field, the courses, the nosing, the trims) is never split: Red's tile and glaze (GZ-C)
    /// keep the probes they were picked under (Red 2026-11-07 21:54: image 1's tile kept), and a hall's tile reflecting its own
    /// probe drew moving streaks on SP6's B2 cheek (flash_check 3.5 at 76 deg/s). Null (TODAY, and every district that gives
    /// none): one group per material, as before.
    /// </summary>
    public List<Bounds> Zones;
    /// <summary>Triangles made so far (render), for the district's cost line.</summary>
    public int Triangles { get; private set; }
    public int Renderers { get; private set; }
    /// <summary>
    /// NEW (scene 4's TODAY / NEW, FrontRoomsStructuresAssets; contract K1 of 游戏视觉's structures_assets): boxes are recorded and
    /// written at Finish with rounds by material on the edges nothing touches (FrontRoomsStructureBevels, which writes them with the
    /// map's FrontRoomsBevel), and the lit stand-in materials are swapped for the game's surfaces where one fits
    /// (FrontRoomsStructuresAssets.Real). Render only: the collision mesh is written as before. False (TODAY): every box is written
    /// as it comes, bit for bit as before.
    /// </summary>
    public bool Bevels;
    /// <summary>With Bevels: a round for the boxes made from now on instead of their material's (a cap's bigger round; the material's
    /// rules still apply, FrontRoomsStructureBevels.Shape); null = by material.</summary>
    public FrontRoomsStructureBevels.Spec? EdgeOverride;
    readonly FrontRoomsStructureBevels.Set bevelSet = new FrontRoomsStructureBevels.Set();

    sealed class KeyCompare : IEqualityComparer<GroupKey>
    {
        public bool Equals(GroupKey a, GroupKey b) => a.material == b.material && Mathf.Abs(a.ceiling - b.ceiling) < 1e-3f && a.floorGrimeOff == b.floorGrimeOff && a.shadows == b.shadows && a.zone == b.zone;
        public int GetHashCode(GroupKey k) => (k.material != null ? k.material.GetHashCode() : 0) ^ Mathf.RoundToInt(k.ceiling * 100f) * 31 ^ (k.floorGrimeOff ? 7 : 0) ^ (k.shadows ? 13 : 0) ^ k.zone * 977;
    }

    public FrontRoomsStructuresMesh(Transform root)
    {
        this.root = root;
        worldOffset = root != null ? root.position : Vector3.zero;
    }

    // K2: the hall a root-local point lies in (1-based; 0 outside every hall, with no halls given, or for the pool tile).
    int ZoneOf(Vector3 local, Material m)
    {
        if (Zones == null || (m != null && m.name.StartsWith("Prop_PoolTile", System.StringComparison.Ordinal))) return 0;
        for (var i = 0; i < Zones.Count; i++) if (Zones[i].Contains(local)) return i + 1;
        return 0;
    }

    Part Get(Material m, Vector3 at = default(Vector3))
    {
        if (Bevels) m = FrontRoomsStructuresAssets.Real(m);
        var key = new GroupKey { material = m, ceiling = Ceiling, floorGrimeOff = FloorGrimeOff, shadows = CastShadows, zone = ZoneOf(at, m) };
        if (!groups.TryGetValue(key, out var p)) groups[key] = p = new Part();
        return p;
    }

    /// <summary>An axis-aligned box in the root's space; solid adds it to the collision mesh.</summary>
    public void Box(Vector3 center, Vector3 size, Material m, bool solid = true)
    {
        if (size.x <= 1e-4f || size.y <= 1e-4f || size.z <= 1e-4f) return;
        if (m != null)
        {
            if (Bevels) Record(center, size, Quaternion.identity, m, FrontRoomsStructureBevels.UvRule.Planar);
            else BoxInto(Get(m, center), center, size, Quaternion.identity, true);
        }
        if (solid) BoxInto(collision, center, size, Quaternion.identity, false);
    }

    /// <summary>A box turned about its centre (rotation in the root's space).</summary>
    public void Box(Vector3 center, Vector3 size, Quaternion rotation, Material m, bool solid = true)
    {
        if (size.x <= 1e-4f || size.y <= 1e-4f || size.z <= 1e-4f) return;
        if (m != null)
        {
            if (Bevels) Record(center, size, rotation, m, FrontRoomsStructureBevels.UvRule.Planar);
            else BoxInto(Get(m, center), center, size, rotation, true);
        }
        if (solid) BoxInto(collision, center, size, rotation, false);
    }

    /// <summary>NEW only: a prism of FrontRoomsStructuresKit (an oriented box whose UVs run along its own line, as Kit.Prism's
    /// quads), recorded for Finish; its collider is the caller's, as before.</summary>
    public void PrismBox(Vector3 center, Vector3 size, Quaternion rotation, Material m, float wallLot = 0f, float wallFoot = 0f)
    {
        if (m == null || size.x <= 1e-4f || size.y <= 1e-4f || size.z <= 1e-4f) return;
        var b = Record(center, size, rotation, m, FrontRoomsStructureBevels.UvRule.Prism);
        b.wallValue = new Vector2(wallLot, wallFoot);
    }

    /// <summary>NEW only: a volume that is not drawn but counts as touching for the bevels (an arch's spandrel, built of quads,
    /// keeps the pier's edge beside it square).</summary>
    public void Occluder(Vector3 center, Vector3 size, Quaternion rotation)
    {
        if (!Bevels || size.x <= 1e-4f || size.y <= 1e-4f || size.z <= 1e-4f) return;
        var b = bevelSet.Add(center, size, rotation, FrontRoomsStructureBevels.None, FrontRoomsStructureBevels.UvRule.Planar, null);
        b.occluderOnly = true;
    }

    FrontRoomsStructureBevels.Box Record(Vector3 center, Vector3 size, Quaternion rotation, Material m, FrontRoomsStructureBevels.UvRule uv)
    {
        m = FrontRoomsStructuresAssets.Real(m);
        var key = new GroupKey { material = m, ceiling = Ceiling, floorGrimeOff = FloorGrimeOff, shadows = CastShadows, zone = ZoneOf(center, m) };
        var spec = EdgeOverride.HasValue ? FrontRoomsStructureBevels.Shape(EdgeOverride.Value, m) : FrontRoomsStructureBevels.For(m);
        return bevelSet.Add(center, size, rotation, spec, uv, key);
    }

    // NEW: the recorded boxes solved together, then written into their groups.
    void FlushBevels()
    {
        if (bevelSet.Boxes.Count == 0) return;
        bevelSet.Solve();
        foreach (var b in bevelSet.Boxes)
        {
            if (b.occluderOnly || !(b.tag is GroupKey)) continue;
            var key = (GroupKey)b.tag;
            if (!groups.TryGetValue(key, out var p)) groups[key] = p = new Part();
            while (p.wall.Count < p.v.Count) p.wall.Add(Vector2.zero);
            Triangles += FrontRoomsStructureBevels.Emit(b, p.v, p.n, p.uv, p.t, worldOffset);
            while (p.wall.Count < p.v.Count) p.wall.Add(b.wallValue);
        }
        bevelSet.Boxes.Clear();
    }

    /// <summary>A box from its min and max corners (axis-aligned).</summary>
    public void Span(Vector3 min, Vector3 max, Material m, bool solid = true) => Box((min + max) * .5f, max - min, m, solid);

    /// <summary>Collision only (no render).</summary>
    public void Collider(Vector3 center, Vector3 size) => BoxInto(collision, center, size, Quaternion.identity, false);
    public void Collider(Vector3 center, Vector3 size, Quaternion rotation) => BoxInto(collision, center, size, rotation, false);

    /// <summary>
    /// A quad a, b, c, d whose front normal is Cross(b - a, d - a) (clockwise seen from the front; a floor facing up:
    /// a (x0, z0), b (x0, z1), c (x1, z1), d (x1, z0)), planar metre UVs; solid adds it to the collision mesh.
    /// </summary>
    public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Material m, bool solid = false)
    {
        var nrm = Vector3.Cross(b - a, d - a);
        if (nrm.sqrMagnitude < 1e-10f) nrm = Vector3.Cross(c - b, a - b);
        nrm.Normalize();
        if (m != null) QuadInto(Get(m, (a + b + c + d) * .25f), a, b, c, d, nrm, true);
        if (solid) QuadInto(collision, a, b, c, d, nrm, false);
    }

    /// <summary>A quad with given UVs (metres along the surface, for arcs and copings whose courses run on).</summary>
    public void QuadUV(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud, Material m, bool solid = false)
    {
        var nrm = Vector3.Cross(b - a, d - a);
        if (nrm.sqrMagnitude < 1e-10f) nrm = Vector3.Cross(c - b, a - b);
        nrm.Normalize();
        if (m != null)
        {
            var p = Get(m, (a + b + c + d) * .25f);
            var i = p.v.Count;
            p.v.Add(a); p.v.Add(b); p.v.Add(c); p.v.Add(d);
            for (var k = 0; k < 4; k++) p.n.Add(nrm);
            p.uv.Add(ua); p.uv.Add(ub); p.uv.Add(uc); p.uv.Add(ud);
            Tris(p, i);
            Triangles += 2;
        }
        if (solid) QuadInto(collision, a, b, c, d, nrm, false);
    }

    /// <summary>Append a prepared triangle list (root space) to a material, with UVs as given.</summary>
    public void Append(List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> t, Material m, bool solid)
    {
        if (v == null || t == null) return;
        if (m != null)
        {
            var p = Get(m, Centre(v));
            var o = p.v.Count;
            p.v.AddRange(v);
            p.n.AddRange(n);
            p.uv.AddRange(uv);
            foreach (var i in t) p.t.Add(o + i);
            Triangles += t.Count / 3;
        }
        if (solid)
        {
            var o = collision.v.Count;
            collision.v.AddRange(v);
            collision.n.AddRange(n);
            collision.uv.AddRange(uv);
            foreach (var i in t) collision.t.Add(o + i);
        }
    }

    // K2: where a prepared triangle list lies (its bounds' centre), for its hall.
    Vector3 Centre(List<Vector3> v)
    {
        if (Zones == null || v.Count == 0) return Vector3.zero;
        Vector3 lo = v[0], hi = v[0];
        foreach (var x in v) { lo = Vector3.Min(lo, x); hi = Vector3.Max(hi, x); }
        return (lo + hi) * .5f;
    }

    void BoxInto(Part p, Vector3 c, Vector3 size, Quaternion r, bool count)
    {
        var h = size * .5f;
        Vector3 X = r * Vector3.right, Y = r * Vector3.up, Z = r * Vector3.forward;
        // Every face has u x v along its outward normal (Unity's Cross), so Tris winds it front-out.
        Face(p, c + X * h.x, X, -Z, Y, h.z, h.y, count);
        Face(p, c - X * h.x, -X, Z, Y, h.z, h.y, count);
        Face(p, c + Y * h.y, Y, X, -Z, h.x, h.z, count);
        Face(p, c - Y * h.y, -Y, X, Z, h.x, h.z, count);
        Face(p, c + Z * h.z, Z, X, Y, h.x, h.y, count);
        Face(p, c - Z * h.z, -Z, -X, Y, h.x, h.y, count);
    }

    // A face at centre c with outward normal nrm; u, v span it (Cross(u, v) = nrm).
    void Face(Part p, Vector3 c, Vector3 nrm, Vector3 u, Vector3 v, float hu, float hv, bool count)
    {
        var a = c - u * hu - v * hv;
        var b = c + u * hu - v * hv;
        var cc = c + u * hu + v * hv;
        var d = c - u * hu + v * hv;
        QuadInto(p, a, b, cc, d, nrm, count);
    }

    void QuadInto(Part p, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 nrm, bool count)
    {
        var i = p.v.Count;
        p.v.Add(a); p.v.Add(b); p.v.Add(c); p.v.Add(d);
        for (var k = 0; k < 4; k++) p.n.Add(nrm);
        p.uv.Add(Planar(a, nrm)); p.uv.Add(Planar(b, nrm)); p.uv.Add(Planar(c, nrm)); p.uv.Add(Planar(d, nrm));
        Tris(p, i);
        if (count) Triangles += 2;
    }

    static void Tris(Part p, int i)
    {
        // With Cross(b - a, d - a) along the front normal, a, b, c, d run clockwise seen from the front (Unity's front
        // face, left-handed), as the map's MeshBuilder.Face: (0, 1, 2) (0, 2, 3).
        p.t.Add(i); p.t.Add(i + 1); p.t.Add(i + 2);
        p.t.Add(i); p.t.Add(i + 2); p.t.Add(i + 3);
    }

    // Metres from world (0, 0) on the face's dominant plane, read the way the map's MeshBuilder reads them.
    Vector2 Planar(Vector3 local, Vector3 nrm)
    {
        var w = local + worldOffset;
        var ax = Mathf.Abs(nrm.x); var ay = Mathf.Abs(nrm.y); var az = Mathf.Abs(nrm.z);
        if (ax >= ay && ax >= az) return new Vector2(w.z, w.y);
        if (ay >= ax && ay >= az) return new Vector2(w.x, w.z);
        return new Vector2(w.x, w.y);
    }

    static readonly int CeilingHeightId = Shader.PropertyToID("_CeilingHeight");
    static readonly int FloorGrimeId = Shader.PropertyToID("_FloorGrime");

    /// <summary>Make the renderers (one per group) and the collider under the root; meshes go into <paramref name="owned"/> to be freed.</summary>
    public void Finish(string name, List<Mesh> owned)
    {
        if (root == null) return;
        if (Bevels) FlushBevels();
        var holder = new GameObject(name + " shell");
        holder.transform.SetParent(root, false);
        foreach (var pair in groups)
        {
            var p = pair.Value;
            if (p.v.Count == 0 || pair.Key.material == null) continue;
            var go = new GameObject(pair.Key.material.name);
            go.transform.SetParent(holder.transform, false);
            if (Bevels) while (p.wall.Count < p.v.Count) p.wall.Add(Vector2.zero);
            var mesh = ToMesh(name + " " + pair.Key.material.name, p, true);
            owned?.Add(mesh);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = pair.Key.material;
            r.shadowCastingMode = pair.Key.shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            var m = pair.Key.material;
            if (m.HasProperty(CeilingHeightId) || m.HasProperty(FloorGrimeId))
            {
                var block = new MaterialPropertyBlock();
                if (m.HasProperty(CeilingHeightId)) block.SetFloat(CeilingHeightId, pair.Key.ceiling);
                if (pair.Key.floorGrimeOff && m.HasProperty(FloorGrimeId)) block.SetFloat(FloorGrimeId, 0f);
                r.SetPropertyBlock(block);
            }
            Renderers++;
        }
        if (collision.v.Count > 0)
        {
            var col = new GameObject(name + " collision");
            col.transform.SetParent(root, false);
            var mesh = ToMesh(name + " collision", collision, false);
            owned?.Add(mesh);
            col.AddComponent<MeshCollider>().sharedMesh = mesh;
        }
        groups.Clear();
    }

    static Mesh ToMesh(string name, Part p, bool tangents)
    {
        var mesh = new Mesh { name = name, hideFlags = HideFlags.DontSave };
        if (p.v.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
        mesh.SetVertices(p.v);
        mesh.SetNormals(p.n);
        mesh.SetUVs(0, p.uv);
        if (p.wall.Count == p.v.Count && p.wall.Count > 0) mesh.SetUVs(1, p.wall);
        mesh.SetTriangles(p.t, 0);
        if (tangents) mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        return mesh;
    }
}
