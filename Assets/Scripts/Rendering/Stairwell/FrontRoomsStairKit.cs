using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Stairwell A (game visual line, C22 look A; research/stairwell/10_directions.md 2.2 and 1.2): the look-A materials
/// and the small procedural meshes of the vestibule kit. The materials are project assets
/// (Resources/Surfaces/Stair_*.mat, FrontRooms/Surface), so their shader variants ship in player builds; the
/// "Inside" ones carry _FR_OWN_LIGHTS: the stairwell's inside takes only its own lights, never the map's 10-12 m
/// lamps that reach through its walls (the safe zone: they dip with W1's warnings). Meshes are made once per session
/// and shared by every stairwell and every EXIT build, so a rebuild or NextFloor leaks nothing; the per-stairwell
/// band mesh and the plate materials are the dresser's and go with their stairwell (FrontRoomsStairDressOwner).
/// A13's chain, padlock and eye bolt are FrontRoomsExitChain's (Rendering/Exit), not this kit's.
/// </summary>
public static class FrontRoomsStairKit
{
    // ---------------------------------------------------------------- look A values (10_directions.md 2.2)
    public static readonly Color BlockPutty = Hex("DACFBC"), BandTan = Hex("A39A86"), CutIn = Hex("4A4238");
    public static readonly Color TreadFill = Hex("BDBEB8"), SlabPaint = Hex("D2CEC4");
    public static readonly Color LeafPutty = Hex("B7B09E"), FrameSteel = Hex("5F5A50");

    /// <summary>The band to 1.1 m and its 12 mm cut-in line (BSS 120 Fig. 2.7.5; 10_directions 2.1).</summary>
    public const float BandTop = 1.1f, CutInHeight = .012f;

    /// <summary>Sign 7, AREA OF RESCUE ASSISTANCE (stair SPEC v1.3 row 7): the face 18.5 x 6.5 in, the white steel box 19.5 x 7.5 x 2.25 in.</summary>
    public static readonly Vector2 RescueFace = new Vector2(.4699f, .1651f);
    public static readonly Vector3 RescueBox = new Vector3(.4953f, .1905f, .05715f);

    public const string BlockName = "Stair_Block", BandName = "Stair_Band", CutInName = "Stair_CutIn", FillName = "Stair_Fill", SlabName = "Stair_Slab";
    public const string LeafName = "Stair_SteelLeaf", LeafInsideName = "Stair_SteelLeafInside", FrameName = "Stair_Frame", FrameInsideName = "Stair_FrameInside";
    public const string KickName = "Stair_Kick", HingeName = "Stair_Hinge", EmtName = "Stair_Emt", PlateName = "Stair_Plate", PlateInsideName = "Stair_PlateInside";
    public const string SignBoxName = "Stair_SignBox", RescueFaceName = "Stair_RescueFace";

    /// <summary>The keyword of the stairwell's inside (FrontRoomsSurface.shader: own lights only).</summary>
    public const string OwnLightsKeyword = "_FR_OWN_LIGHTS";

    static readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
    static readonly Dictionary<string, Material> fallbacks = new Dictionary<string, Material>();

    public static Color Hex(string h)
    {
        ColorUtility.TryParseHtmlString("#" + h, out var c);
        return c;
    }

    /// <summary>The project material Resources/Surfaces/name; a plain lit stand-in (warned once) if it is missing.</summary>
    public static Material Get(string name)
    {
        var m = FrontRoomsSurfaces.TryGet(name);
        if (m != null) return m;
        if (fallbacks.TryGetValue(name, out var f) && f != null) return f;
        Debug.LogWarning("[StairwellA] material Surfaces/" + name + " is missing; a plain stand-in is used");
        f = FrontRoomsSurfaces.Lit("Stair A missing / " + name, new Color(.6f, .58f, .54f), .3f);
        fallbacks[name] = f;
        return f;
    }

    public static Material Block => Get(BlockName);
    public static Material Band => Get(BandName);
    public static Material CutInLine => Get(CutInName);
    public static Material Fill => Get(FillName);
    public static Material Slab => Get(SlabName);
    /// <summary>The EXIT leaf (seen from the maze and from the stair): lit by every light.</summary>
    public static Material Leaf => Get(LeafName);
    /// <summary>The stairwell's own leaves seen only from inside (the sealed south leaf).</summary>
    public static Material LeafInside => Get(LeafInsideName);
    public static Material Frame => Get(FrameName);
    public static Material FrameInside => Get(FrameInsideName);
    public static Material Kick => Get(KickName);
    public static Material HingeSteel => Get(HingeName);
    public static Material Conduit => Get(EmtName);
    public static Material SignBox => Get(SignBoxName);
    public static Material RescueFaceMaterial => Get(RescueFaceName);

    /// <summary>A plate material for one baked plate set (the set owns it and destroys it with its textures).</summary>
    public static Material PlateMaterial(FrontRoomsStairSigns.PlateSet set, bool inside)
    {
        var m = new Material(Get(inside ? PlateInsideName : PlateName)) { name = "Stair A / plate " + (set != null ? set.text : "missing"), hideFlags = HideFlags.DontSave };
        if (set != null)
        {
            m.SetTexture("_BaseMap", set.albedo);
            m.SetTexture("_BumpMap", set.normal);
            m.SetTexture("_MaskMap", set.mask);
            m.SetVector("_TileSize", new Vector4(set.size.x, set.size.y, 0f, 0f));
        }
        m.SetColor("_BaseColor", Color.white);
        m.SetFloat("_BumpScale", 1f);
        m.SetFloat("_Smoothness", FrontRoomsStairSigns.SmoothFace);
        return m;
    }

    public static int SharedMeshes => meshes.Count;

    // ---------------------------------------------------------------- mesh building
    public sealed class MB
    {
        public readonly List<Vector3> v = new List<Vector3>();
        public readonly List<Vector3> n = new List<Vector3>();
        public readonly List<Vector4> t = new List<Vector4>();
        public readonly List<Vector2> uv = new List<Vector2>();
        public readonly List<int> tri = new List<int>();

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud)
        {
            var i = v.Count;
            v.Add(a); v.Add(b); v.Add(c); v.Add(d);
            for (var k = 0; k < 4; k++) n.Add(normal);
            var tan = (b - a).normalized;
            var w = Vector3.Dot(Vector3.Cross(normal, tan), (d - a).normalized) >= 0f ? 1f : -1f;
            for (var k = 0; k < 4; k++) t.Add(new Vector4(tan.x, tan.y, tan.z, w));
            uv.Add(ua); uv.Add(ub); uv.Add(uc); uv.Add(ud);
            Tri(i, i + 1, i + 2, normal);
            Tri(i, i + 2, i + 3, normal);
        }

        /// <summary>A triangle wound so that its front (Unity: Cross(v1 - v0, v2 - v0)) faces <paramref name="normal"/>.</summary>
        public void Tri(int a, int b, int c, Vector3 normal)
        {
            if (Vector3.Dot(Vector3.Cross(v[b] - v[a], v[c] - v[a]), normal) >= 0f) { tri.Add(a); tri.Add(b); tri.Add(c); }
            else { tri.Add(a); tri.Add(c); tri.Add(b); }
        }

        /// <summary>A box (centre, size) turned by rot, metre UVs per face.</summary>
        public void Box(Vector3 c, Vector3 size, Quaternion rot)
        {
            var h = size * .5f;
            Vector3 P(float x, float y, float z) => c + rot * new Vector3(x * h.x, y * h.y, z * h.z);
            var R = rot * Vector3.right; var U = rot * Vector3.up; var F = rot * Vector3.forward;
            Quad(P(1, -1, 1), P(-1, -1, 1), P(-1, 1, 1), P(1, 1, 1), F, new Vector2(0, 0), new Vector2(size.x, 0), new Vector2(size.x, size.y), new Vector2(0, size.y));
            Quad(P(-1, -1, -1), P(1, -1, -1), P(1, 1, -1), P(-1, 1, -1), -F, new Vector2(0, 0), new Vector2(size.x, 0), new Vector2(size.x, size.y), new Vector2(0, size.y));
            Quad(P(1, -1, -1), P(1, -1, 1), P(1, 1, 1), P(1, 1, -1), R, new Vector2(0, 0), new Vector2(size.z, 0), new Vector2(size.z, size.y), new Vector2(0, size.y));
            Quad(P(-1, -1, 1), P(-1, -1, -1), P(-1, 1, -1), P(-1, 1, 1), -R, new Vector2(0, 0), new Vector2(size.z, 0), new Vector2(size.z, size.y), new Vector2(0, size.y));
            Quad(P(-1, 1, -1), P(1, 1, -1), P(1, 1, 1), P(-1, 1, 1), U, new Vector2(0, 0), new Vector2(size.x, 0), new Vector2(size.x, size.z), new Vector2(0, size.z));
            Quad(P(-1, -1, 1), P(1, -1, 1), P(1, -1, -1), P(-1, -1, -1), -U, new Vector2(0, 0), new Vector2(size.x, 0), new Vector2(size.x, size.z), new Vector2(0, size.z));
        }

        /// <summary>A tube along a polyline (circular section, radius r, sides around).</summary>
        public void Tube(IList<Vector3> path, float r, int sides, bool closed)
        {
            var n0 = path.Count;
            if (n0 < 2) return;
            var count = closed ? n0 + 1 : n0;
            var baseIndex = v.Count;
            var prevNormal = Vector3.zero;
            var length = 0f;
            for (var i = 0; i < count; i++)
            {
                var p = path[i % n0];
                Vector3 tangent;
                if (closed) tangent = (path[(i + 1) % n0] - path[(i - 1 + n0) % n0]).normalized;
                else tangent = (path[Mathf.Min(i + 1, n0 - 1)] - path[Mathf.Max(i - 1, 0)]).normalized;
                if (i > 0) length += (p - path[(i - 1) % n0]).magnitude;
                var nrm = i == 0 ? Vector3.Cross(tangent, Mathf.Abs(Vector3.Dot(tangent, Vector3.up)) > .9f ? Vector3.forward : Vector3.up).normalized
                    : Vector3.ProjectOnPlane(prevNormal, tangent).normalized;
                if (nrm.sqrMagnitude < 1e-6f) nrm = Vector3.Cross(tangent, Vector3.right).normalized;
                prevNormal = nrm;
                var bin = Vector3.Cross(tangent, nrm);
                for (var s = 0; s <= sides; s++)
                {
                    var a = s * Mathf.PI * 2f / sides;
                    var dir = nrm * Mathf.Cos(a) + bin * Mathf.Sin(a);
                    v.Add(p + dir * r);
                    n.Add(dir);
                    t.Add(new Vector4(tangent.x, tangent.y, tangent.z, 1f));
                    uv.Add(new Vector2(length, s * Mathf.PI * 2f * r / sides));
                }
            }
            var ring = sides + 1;
            for (var i = 0; i < count - 1; i++)
            for (var s = 0; s < sides; s++)
            {
                var a = baseIndex + i * ring + s;
                var b = a + ring;
                tri.Add(a); tri.Add(a + 1); tri.Add(b);
                tri.Add(b); tri.Add(a + 1); tri.Add(b + 1);
            }
        }

        /// <summary>A capped cylinder from a to b.</summary>
        public void Cylinder(Vector3 a, Vector3 b, float r, int sides)
        {
            Tube(new List<Vector3> { a, b }, r, sides, false);
            var axis = (b - a).normalized;
            Disc(a, -axis, r, sides);
            Disc(b, axis, r, sides);
        }

        public void Disc(Vector3 c, Vector3 normal, float r, int sides)
        {
            var u = Vector3.Cross(normal, Mathf.Abs(Vector3.Dot(normal, Vector3.up)) > .9f ? Vector3.forward : Vector3.up).normalized;
            var w = Vector3.Cross(normal, u);
            var i0 = v.Count;
            v.Add(c); n.Add(normal); t.Add(new Vector4(u.x, u.y, u.z, 1f)); uv.Add(Vector2.zero);
            for (var s = 0; s <= sides; s++)
            {
                var a = s * Mathf.PI * 2f / sides;
                v.Add(c + (u * Mathf.Cos(a) + w * Mathf.Sin(a)) * r);
                n.Add(normal); t.Add(new Vector4(u.x, u.y, u.z, 1f));
                uv.Add(new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
            }
            for (var s = 0; s < sides; s++) Tri(i0, i0 + 1 + s, i0 + 2 + s, normal);
        }

        public Mesh ToMesh(string name)
        {
            var m = new Mesh { name = name, hideFlags = HideFlags.DontSave };
            if (v.Count > 65000) m.indexFormat = IndexFormat.UInt32;
            m.SetVertices(v);
            m.SetNormals(n);
            m.SetTangents(t);
            m.SetUVs(0, uv);
            m.SetTriangles(tri, 0);
            m.RecalculateBounds();
            return m;
        }
    }

    static Mesh Cached(string key, System.Func<MB, bool> build)
    {
        if (meshes.TryGetValue(key, out var m) && m != null) return m;
        var mb = new MB();
        if (!build(mb)) return null;
        m = mb.ToMesh("Stair A / " + key);
        meshes[key] = m;
        return m;
    }

    /// <summary>A 10 in kick plate, 1/16 in brushed stainless (its face along +Z).</summary>
    public static Mesh KickPlate(float width) => Cached("kick plate " + width.ToString("0.000"), mb =>
    {
        const float h = .254f, th = .0016f;
        mb.Box(new Vector3(0f, h * .5f, th * .5f), new Vector3(width, h, th), Quaternion.identity);
        return true;
    });

    /// <summary>
    /// A two-ply plate: a 1.6 mm slab with the art's corner radius (3.2 mm). The face looks along -Z (as a Unity
    /// quad's) and carries the plate's UVs in metres from its bottom-left corner, so _TileSize = the plate's size maps
    /// the art 0..1; the back lies on z = 0. Place it with LookRotation(-surfaceNormal, up).
    /// </summary>
    public static Mesh PlateSlab(Vector2 size) => Cached("plate " + size.x.ToString("0.0000") + "x" + size.y.ToString("0.0000"), mb =>
    {
        const float th = .0016f, rad = .0032f;
        const int arc = 4;
        var outline = new List<Vector2>();
        var corners = new[] { new Vector2(size.x - rad, rad), new Vector2(size.x - rad, size.y - rad), new Vector2(rad, size.y - rad), new Vector2(rad, rad) };
        for (var c = 0; c < 4; c++)
            for (var i = 0; i <= arc; i++)
            {
                var a = (c - 1) * Mathf.PI * .5f + i * Mathf.PI * .5f / arc;
                outline.Add(corners[c] + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rad);
            }
        var o = new Vector3(-size.x * .5f, -size.y * .5f, 0f);
        var centre = mb.v.Count;
        // the face, seen from -Z looking along +Z: +X is the viewer's right, so u = x reads unmirrored
        mb.v.Add(o + new Vector3(size.x * .5f, size.y * .5f, -th)); mb.n.Add(Vector3.back); mb.t.Add(new Vector4(1, 0, 0, -1)); mb.uv.Add(size * .5f);
        foreach (var p in outline) { mb.v.Add(o + new Vector3(p.x, p.y, -th)); mb.n.Add(Vector3.back); mb.t.Add(new Vector4(1, 0, 0, -1)); mb.uv.Add(p); }
        for (var i = 0; i < outline.Count; i++) mb.Tri(centre, centre + 1 + i, centre + 1 + (i + 1) % outline.Count, Vector3.back);
        for (var i = 0; i < outline.Count; i++)
        {
            var p0 = outline[i];
            var p1 = outline[(i + 1) % outline.Count];
            var e = p1 - p0;
            var nrm = new Vector3(e.y, -e.x, 0f).normalized;
            mb.Quad(o + new Vector3(p0.x, p0.y, 0f), o + new Vector3(p1.x, p1.y, 0f), o + new Vector3(p1.x, p1.y, -th), o + new Vector3(p0.x, p0.y, -th), nrm,
                p0, p1, p1 + Vector2.up * th, p0 + Vector2.up * th);
        }
        return true;
    });

    /// <summary>A butt hinge knuckle (4 1/2 in, ball bearing): a cylinder along +Y, 11 mm radius.</summary>
    public static Mesh Knuckle => Cached("hinge knuckle", mb =>
    {
        mb.Cylinder(new Vector3(0f, -.057f, 0f), new Vector3(0f, .057f, 0f), .0105f, 12);
        mb.Cylinder(new Vector3(0f, -.002f, 0f), new Vector3(0f, .002f, 0f), .0112f, 12);
        return true;
    });

    /// <summary>A length of 1/2 in EMT along +Y (21 mm OD), with a set-screw coupling at the top.</summary>
    public static Mesh EmtRun(float length) => Cached("EMT " + length.ToString("0.000"), mb =>
    {
        mb.Cylinder(Vector3.zero, new Vector3(0f, length, 0f), .0089f, 10);
        mb.Cylinder(new Vector3(0f, length - .045f, 0f), new Vector3(0f, length - .005f, 0f), .011f, 10);
        return true;
    });

    /// <summary>Sign 7's white steel box, its back on z = 0 and its front at +Z (the face quad is separate).</summary>
    public static Mesh RescueBoxMesh => Cached("rescue sign box", mb =>
    {
        var s = RescueBox;
        // the frame round the face (0.5 in) and the box's sides: a hollow front, so the face sits 1 mm inside the rim
        const float rim = .0127f, lip = .002f;
        mb.Box(new Vector3(0f, 0f, (s.z - lip) * .5f), new Vector3(s.x, s.y, s.z - lip), Quaternion.identity);
        mb.Box(new Vector3(0f, (s.y - rim) * .5f, s.z - lip * .5f), new Vector3(s.x, rim, lip), Quaternion.identity);
        mb.Box(new Vector3(0f, -(s.y - rim) * .5f, s.z - lip * .5f), new Vector3(s.x, rim, lip), Quaternion.identity);
        mb.Box(new Vector3((s.x - rim) * .5f, 0f, s.z - lip * .5f), new Vector3(rim, s.y - 2f * rim, lip), Quaternion.identity);
        mb.Box(new Vector3(-(s.x - rim) * .5f, 0f, s.z - lip * .5f), new Vector3(rim, s.y - 2f * rim, lip), Quaternion.identity);
        return true;
    });

    /// <summary>Sign 7's lit face: a quad at +Z facing +Z, UVs 0..1. Seen from the front (looking along -Z) the
    /// viewer's right is -X, so u grows toward -X and the art reads unmirrored.</summary>
    public static Mesh RescueFaceMesh => Cached("rescue sign face", mb =>
    {
        var f = RescueFace * .5f;
        var z = RescueBox.z - .0012f;
        mb.Quad(new Vector3(f.x, -f.y, z), new Vector3(-f.x, -f.y, z), new Vector3(-f.x, f.y, z), new Vector3(f.x, f.y, z), Vector3.forward,
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1));
        return true;
    });

    /// <summary>A leaf face skin: a width x height quad in the YZ plane, facing +X (side 1) or -X (side -1), metre UVs.</summary>
    public static Mesh SkinQuad(float width, float height, float side) => Cached("skin " + width.ToString("0.000") + "x" + height.ToString("0.000") + (side > 0f ? "+" : "-"), mb =>
    {
        var w = width * .5f;
        var h = height * .5f;
        var n = side > 0f ? Vector3.right : Vector3.left;
        mb.Quad(new Vector3(0f, -h, -w), new Vector3(0f, -h, w), new Vector3(0f, h, w), new Vector3(0f, h, -w), n,
            new Vector2(0f, 0f), new Vector2(width, 0f), new Vector2(width, height), new Vector2(0f, height));
        return true;
    });

    /// <summary>Tools: every shared mesh made here (the leak check counts these before and after ten floors).</summary>
    public static int MeshCount => meshes.Count;
}
