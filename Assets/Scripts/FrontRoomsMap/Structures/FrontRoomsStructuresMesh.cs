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
    }

    struct GroupKey
    {
        public Material material;
        public float ceiling;
        public bool floorGrimeOff, shadows;
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
    /// <summary>Triangles made so far (render), for the district's cost line.</summary>
    public int Triangles { get; private set; }
    public int Renderers { get; private set; }

    sealed class KeyCompare : IEqualityComparer<GroupKey>
    {
        public bool Equals(GroupKey a, GroupKey b) => a.material == b.material && Mathf.Abs(a.ceiling - b.ceiling) < 1e-3f && a.floorGrimeOff == b.floorGrimeOff && a.shadows == b.shadows;
        public int GetHashCode(GroupKey k) => (k.material != null ? k.material.GetHashCode() : 0) ^ Mathf.RoundToInt(k.ceiling * 100f) * 31 ^ (k.floorGrimeOff ? 7 : 0) ^ (k.shadows ? 13 : 0);
    }

    public FrontRoomsStructuresMesh(Transform root)
    {
        this.root = root;
        worldOffset = root != null ? root.position : Vector3.zero;
    }

    Part Get(Material m)
    {
        var key = new GroupKey { material = m, ceiling = Ceiling, floorGrimeOff = FloorGrimeOff, shadows = CastShadows };
        if (!groups.TryGetValue(key, out var p)) groups[key] = p = new Part();
        return p;
    }

    /// <summary>An axis-aligned box in the root's space; solid adds it to the collision mesh.</summary>
    public void Box(Vector3 center, Vector3 size, Material m, bool solid = true)
    {
        if (size.x <= 1e-4f || size.y <= 1e-4f || size.z <= 1e-4f) return;
        if (m != null) BoxInto(Get(m), center, size, Quaternion.identity, true);
        if (solid) BoxInto(collision, center, size, Quaternion.identity, false);
    }

    /// <summary>A box turned about its centre (rotation in the root's space).</summary>
    public void Box(Vector3 center, Vector3 size, Quaternion rotation, Material m, bool solid = true)
    {
        if (size.x <= 1e-4f || size.y <= 1e-4f || size.z <= 1e-4f) return;
        if (m != null) BoxInto(Get(m), center, size, rotation, true);
        if (solid) BoxInto(collision, center, size, rotation, false);
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
        if (m != null) QuadInto(Get(m), a, b, c, d, nrm, true);
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
            var p = Get(m);
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
            var p = Get(m);
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
        var holder = new GameObject(name + " shell");
        holder.transform.SetParent(root, false);
        foreach (var pair in groups)
        {
            var p = pair.Value;
            if (p.v.Count == 0 || pair.Key.material == null) continue;
            var go = new GameObject(pair.Key.material.name);
            go.transform.SetParent(holder.transform, false);
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
        mesh.SetTriangles(p.t, 0);
        if (tangents) mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        return mesh;
    }
}
