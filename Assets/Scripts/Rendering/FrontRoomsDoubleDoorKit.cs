using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The start double door (stream_door_frame, 2026-10-29; Red: the leaves hung in the air with a slit at both jambs),
/// built in one place for both of its callers:
/// - FrontRoomsRoomStream: every stream room's end door, both faces, swinging both ways (BuildDoor, SetHingeSide);
/// - FrontRoomsStationDress: the static entrance, the same door shut, its +Z face on the station side of D's data
///   wall once ClearStartArea has given the strip back (Faces.Plus: what lies behind the wall is left out, and the
///   gaps show a dark seal instead of the wall behind them).
/// A 1990 commercial double-acting pair in a cased walnut frame (Kit_DoorFrame_Wood's walnut, oak and lining stock):
/// 1-3/4" flush leaves on three double-acting spring hinges each (Bommer's layout: one within 7" of the top, a pair
/// within 3" of the bottom), 3 mm from the jamb linings, the head and each other, 6 mm over a 12 mm oak saddle. A DA
/// spring hinge has a barrel on each face; the leaf turns on the barrel on the face it swings toward, so its pivot
/// sits on that face's knuckle axis (PlaceLeaf) and the leaf clears the lining both ways. Black brush strips close
/// the 3 mm gaps.
/// Door space: x across the opening (0 its centre, the left leaf hinged at -x), y up from the floor, z through the
/// wall (0 the leaves' centre plane; +Z the face the stream door swings toward, the station's side). The frame is
/// cased on a WallDepth wall centred on z 0. Render-only except the leaves and their pulls and kick plates when the
/// caller asks for colliders (the stream's doorway colliders).
/// </summary>
public static class FrontRoomsDoubleDoorKit
{
    public const float Width = 2.4f;
    /// <summary>The head lining's soffit (FrontRoomsRoomStream: RoomHeight 2.9 - its 0.24 m header).</summary>
    public const float HeadBottom = 2.9f - .24f;
    /// <summary>The wall the casing sits on, centred on the door's z 0 (the stream's end wall).</summary>
    public const float WallDepth = .04f;
    public const float Gap = .003f;
    public const float LeafThickness = .0445f;
    public const float LeafWidth = (Width - 3f * Gap) * .5f;
    public const float SaddleHeight = .012f, SaddleHalf = .076f, SaddleFlatHalf = .052f;
    public const float LeafBottom = SaddleHeight + .006f;
    public const float LeafHeight = HeadBottom - Gap - LeafBottom;
    /// <summary>Jamb stock behind the opening edge, as deep as the leaf (a DA spring hinge needs the jamb flush with the door faces).</summary>
    public const float LiningThickness = .019f;
    /// <summary>
    /// Streamline casing, set back 12 mm from the lining face so the hinge barrels clear it; its outer edge 50 mm from
    /// the opening, where the station's crash rail and base stop (FrontRoomsStationTrim.DoorStop) and 25 mm short of
    /// the BUSINESS HOURS plate.
    /// </summary>
    public const float CasingReveal = .012f, CasingFace = .038f, CasingProud = .014f;
    public const float HingeBarrelRadius = .011f, HingeBarrelLength = .203f, HingeFlangeLength = .178f;
    /// <summary>Knuckle axis: just off the leaf face (the barrel clears the leaf and the lining by 1 mm), centred on the 3 mm hinge gap.</summary>
    public const float HingeAxisOffset = LeafThickness * .5f + HingeBarrelRadius + .001f;
    public const float BrushThickness = .0015f;

    /// <summary>Both: a door seen from both sides (the stream). Plus: only its +Z face stands (the static entrance on a solid wall).</summary>
    public enum Faces { Both, Plus }

    public struct Materials
    {
        public Material walnut, oak, brush, hardware, leaf;
    }

    // ------------------------------------------------------------------ build
    /// <summary>The frame under <paramref name="frame"/> (its origin the door space's): linings, casing, saddle, and with
    /// both faces the brush strips and the hinges' jamb halves; with Plus the dark seal behind the gaps.</summary>
    public static void BuildFrame(Transform frame, Materials m, Faces faces)
    {
        var both = faces == Faces.Both;
        Part(frame, "door frame lining and casing", FrameMesh(both), m.walnut);
        Part(frame, "door saddle", SaddleMesh(), m.oak);
        if (both)
        {
            Part(frame, "door brush strips", BrushMesh(), m.brush);
            Part(frame, "door hinges / jamb halves", HingeJambMesh(), m.hardware);
        }
        else Part(frame, "door gap seal", GapSealMesh(), m.brush);
    }

    /// <summary>
    /// One leaf (hand -1: the left leaf, hinged at -x; +1: the right) under a new pivot, a child of
    /// <paramref name="pivotParent"/> (the frame's parent: PlaceLeaf puts the pivot at the frame's position plus the
    /// knuckle axis). The leaf sits in its own carrier, origin on its hinge edge and centre plane, so the pivot can move
    /// to the other face's knuckle axis while the leaf stays put. Call PlaceLeaf before use.
    /// </summary>
    public static Transform BuildLeaf(Transform pivotParent, Transform frame, int hand, Materials m, Faces faces, bool colliders,
        out Transform carrier, out Transform hingeBody, out GameObject slab)
    {
        var both = faces == Faces.Both;
        var side = hand < 0 ? "left" : "right";
        var pivot = new GameObject("double door " + side + " hinge").transform;
        pivot.SetParent(pivotParent, false);
        carrier = new GameObject("double door " + side + " leaf carrier").transform;
        carrier.SetParent(pivot, false);
        var along = -hand;
        slab = Box(carrier, "double door " + side, new Vector3(along * LeafWidth * .5f, LeafBottom + LeafHeight * .5f, 0f),
            new Vector3(LeafWidth, LeafHeight, LeafThickness), m.leaf, colliders);
        // 1.5 mm arrises on the slab: the practicals catch its edges.
        slab.GetComponent<MeshFilter>().sharedMesh = LeafMesh();
        // A pull bar on two posts on each face, 0.10 m from the centre line, and brushed kick plates on the face,
        // 3 mm over the leaf's bottom.
        const float handleDepth = .045f;
        var handleOffset = LeafThickness * .5f + handleDepth * .5f + .006f;
        var handleX = along * (Width * .5f - Gap - .10f);
        if (both) Box(carrier, side + " door handle / near", new Vector3(handleX, 1.22f, -handleOffset), new Vector3(.055f, .18f, handleDepth), m.hardware, colliders);
        Box(carrier, side + " door handle / far", new Vector3(handleX, 1.22f, handleOffset), new Vector3(.055f, .18f, handleDepth), m.hardware, colliders);
        var plateOffset = LeafThickness * .5f + .002f;
        foreach (var face in new[] { -1f, 1f })
        {
            if (face < 0f && !both) continue;
            Box(carrier, side + " door kick plate " + (face < 0 ? "/ near" : "/ far"), new Vector3(along * LeafWidth * .5f, LeafBottom + .003f + .125f, face * plateOffset),
                new Vector3(LeafWidth - .05f, .25f, .004f), m.hardware, colliders);
        }
        Part(carrier, side + " door hinge leaves and pull posts", LeafHardwareMesh(hand, both), m.hardware);
        if (hand < 0 && both) Part(carrier, "door meeting brush", MeetingBrushMesh(), m.brush);
        // The swing-side barrels (+Z face) and the centre channels: on the frame while the leaf turns on them, with the
        // leaf when it turns on the -Z ones.
        hingeBody = new GameObject(side + " door hinge barrels and channels").transform;
        hingeBody.SetParent(frame, false);
        Part(hingeBody, side + " door hinge barrels / swing side", HingeBodyMesh(hand), m.hardware);
        return pivot;
    }

    /// <summary>
    /// The leaf turns on the knuckle axis of the face it swings toward: side +1 the +Z barrels, -1 the -Z barrels.
    /// The pivot and the frame share a parent and the frame is not turned in it. Call only while the leaf is shut
    /// (its pivot is reset to the shut pose), so nothing visibly moves.
    /// </summary>
    public static void PlaceLeaf(Transform frame, Transform pivot, Transform carrier, Transform hingeBody, int hand, int side)
    {
        pivot.localRotation = Quaternion.identity;
        hingeBody.SetParent(frame, false);
        hingeBody.localPosition = Vector3.zero;
        hingeBody.localRotation = Quaternion.identity;
        var f = frame.localPosition;
        pivot.localPosition = new Vector3(f.x + hand * (Width * .5f - Gap * .5f), f.y, f.z + side * HingeAxisOffset);
        carrier.localPosition = new Vector3(-hand * Gap * .5f, 0f, -side * HingeAxisOffset);
        carrier.localRotation = Quaternion.identity;
        if (side < 0) hingeBody.SetParent(pivot, true);
    }

    static GameObject Part(Transform parent, string name, Mesh mesh, Material material)
    {
        var part = new GameObject(name);
        part.transform.SetParent(parent, false);
        part.AddComponent<MeshFilter>().sharedMesh = mesh;
        part.AddComponent<MeshRenderer>().sharedMaterial = material;
        return part;
    }

    // As FrontRoomsRoomStream.Box: a planar box, with a matching BoxCollider when asked.
    static GameObject Box(Transform parent, string name, Vector3 localPosition, Vector3 scale, Material material, bool collider)
    {
        var box = new GameObject(name);
        box.transform.SetParent(parent, false);
        box.transform.localPosition = localPosition;
        box.transform.localScale = Vector3.one;
        box.AddComponent<MeshFilter>().sharedMesh = FrontRoomsFilmMesh.GetPlanarBox(scale);
        box.AddComponent<MeshRenderer>().sharedMaterial = material;
        if (collider) box.AddComponent<BoxCollider>().size = scale;
        return box;
    }

    // ------------------------------------------------------------------ meshes (door space)
    // Built once and shared by every door. UVs in metres, along the grain for the wood.
    static readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();

    static bool Cached(string key, out Mesh mesh) => meshes.TryGetValue(key, out mesh) && mesh != null;

    /// <summary>The hinge centres (Bommer's DA spring hinge layout): a pair low, 60 mm over the leaf's bottom and 60 mm apart, one 150 mm under its top.</summary>
    public static float[] HingeCentres()
    {
        var c1 = LeafBottom + .06f + HingeFlangeLength * .5f;
        var c2 = c1 + HingeFlangeLength + .06f;
        var c3 = LeafBottom + LeafHeight - .15f - HingeFlangeLength * .5f;
        return new[] { c1, c2, c3 };
    }

    static Mesh LeafMesh()
    {
        Mesh mesh;
        if (Cached("leaf", out mesh)) return mesh;
        var b = new MeshBuilder();
        b.BevelBox(Vector3.zero, new Vector3(LeafWidth, LeafHeight, LeafThickness), .0015f);
        return meshes["leaf"] = b.ToMesh("Stream door leaf");
    }

    static Mesh FrameMesh(bool both)
    {
        var key = both ? "frame" : "framePlus";
        Mesh mesh;
        if (Cached(key, out mesh)) return mesh;
        var b = new MeshBuilder();
        var half = Width * .5f;
        var t = LeafThickness * .5f;
        // linings: the jambs under the head, the head across both
        b.Box(new Vector3(-half - LiningThickness, 0f, -t), new Vector3(-half, HeadBottom, t), 1);
        b.Box(new Vector3(half, 0f, -t), new Vector3(half + LiningThickness, HeadBottom, t), 1);
        b.Box(new Vector3(-half - LiningThickness, HeadBottom, -t), new Vector3(half + LiningThickness, HeadBottom + LiningThickness, t), 0);
        // casing on the wall face, mitred at the head
        var profile = CasingProfile();
        var inner = half + CasingReveal;
        var top = HeadBottom + CasingReveal;
        foreach (var face in new[] { -1f, 1f })
        {
            if (face < 0f && !both) continue;
            var z = face * WallDepth * .5f;
            var proud = new Vector3(0f, 0f, face);
            b.Sweep(new Vector3(-inner, 0f, z), Vector3.up, Vector3.left, proud, profile, 0f, 0f, top, 1f);
            b.Sweep(new Vector3(inner, 0f, z), Vector3.up, Vector3.right, proud, profile, 0f, 0f, top, 1f);
            b.Sweep(new Vector3(0f, top, z), Vector3.right, Vector3.up, proud, profile, -inner, -1f, inner, 1f);
        }
        return meshes[key] = b.ToMesh(both ? "Stream door frame" : "Stream door frame, +Z face");
    }

    /// <summary>
    /// The casing's section: u across the face from its opening edge, w proud of the wall. A 2.5 mm round-over on the
    /// opening edge, 14 mm proud there, an elliptical streamline falling to a 4 mm outer edge 38 mm out.
    /// </summary>
    static Vector2[] CasingProfile()
    {
        var p = new List<Vector2> { new Vector2(0f, 0f), new Vector2(0f, CasingProud - .0025f) };
        for (var k = 1; k <= 3; k++)
        {
            var a = Mathf.PI - k * Mathf.PI * .5f / 3f;
            p.Add(new Vector2(.0025f + .0025f * Mathf.Cos(a), CasingProud - .0025f + .0025f * Mathf.Sin(a)));
        }
        const float edge = .004f;
        var au = CasingFace - .0025f;
        var aw = CasingProud - edge;
        for (var k = 1; k <= 10; k++)
        {
            var a = Mathf.PI * .5f - k * Mathf.PI * .5f / 10f;
            p.Add(new Vector2(.0025f + au * Mathf.Cos(a), edge + aw * Mathf.Sin(a)));
        }
        p.Add(new Vector2(CasingFace, 0f));
        return p.ToArray();
    }

    static Mesh SaddleMesh()
    {
        Mesh mesh;
        if (Cached("saddle", out mesh)) return mesh;
        var b = new MeshBuilder();
        var half = Width * .5f;
        float z0 = -SaddleHalf, z1 = -SaddleFlatHalf, z2 = SaddleFlatHalf, z3 = SaddleHalf, h = SaddleHeight;
        var bevelN = new Vector3(0f, SaddleHalf - SaddleFlatHalf, -h).normalized;
        b.QuadUV(new Vector3(-half, 0f, z0), new Vector3(half, 0f, z0), new Vector3(half, h, z1), new Vector3(-half, h, z1), bevelN, 0);
        b.QuadUV(new Vector3(-half, h, z1), new Vector3(half, h, z1), new Vector3(half, h, z2), new Vector3(-half, h, z2), Vector3.up, 0);
        b.QuadUV(new Vector3(-half, h, z2), new Vector3(half, h, z2), new Vector3(half, 0f, z3), new Vector3(-half, 0f, z3), new Vector3(0f, bevelN.y, -bevelN.z), 0);
        foreach (var x in new[] { -half, half })
        {
            var n = new Vector3(Mathf.Sign(x), 0f, 0f);
            b.QuadUV(new Vector3(x, 0f, z0), new Vector3(x, h, z1), new Vector3(x, h, z2), new Vector3(x, 0f, z3), n, 2);
        }
        return meshes["saddle"] = b.ToMesh("Stream door saddle");
    }

    /// <summary>Static brushes: each jamb gap between the hinges, from the saddle to the head brush, and the head gap across the opening; 0.2 mm into the leaf.</summary>
    static Mesh BrushMesh()
    {
        Mesh mesh;
        if (Cached("brush", out mesh)) return mesh;
        var b = new MeshBuilder();
        var half = Width * .5f;
        var bt = BrushThickness * .5f;
        var reach = Gap + .0002f;
        var headBrushBottom = HeadBottom - reach;
        var c = HingeCentres();
        var hf = HingeFlangeLength * .5f;
        var spans = new[] { SaddleHeight, c[0] - hf, c[0] + hf, c[1] - hf, c[1] + hf, c[2] - hf, c[2] + hf, headBrushBottom };
        foreach (var hand in new[] { -1f, 1f })
            for (var i = 0; i < spans.Length; i += 2)
            {
                var x0 = hand * half;
                var x1 = hand * (half - reach);
                b.Box(new Vector3(Mathf.Min(x0, x1), spans[i], -bt), new Vector3(Mathf.Max(x0, x1), spans[i + 1], bt), 1);
            }
        b.Box(new Vector3(-half, headBrushBottom, -bt), new Vector3(half, HeadBottom, bt), 0);
        return meshes["brush"] = b.ToMesh("Stream door brush strips");
    }

    /// <summary>
    /// Faces.Plus: the door stands on a solid wall (z below WallDepth / 2 is wall), so its gaps would show the wall's
    /// paper 2 mm down. A dark seal 1 mm proud of the wall face fills the opening behind the leaves and the lining.
    /// </summary>
    static Mesh GapSealMesh()
    {
        Mesh mesh;
        if (Cached("gapSeal", out mesh)) return mesh;
        var b = new MeshBuilder();
        var half = Width * .5f;
        var z = WallDepth * .5f;
        b.Box(new Vector3(-half, 0f, z - .002f), new Vector3(half, HeadBottom, z + .001f), 1);
        return meshes["gapSeal"] = b.ToMesh("Stream door gap seal");
    }

    /// <summary>The meeting stiles' brush, on the left leaf's edge (its carrier space).</summary>
    static Mesh MeetingBrushMesh()
    {
        Mesh mesh;
        if (Cached("meeting", out mesh)) return mesh;
        var b = new MeshBuilder();
        var bt = BrushThickness * .5f;
        b.Box(new Vector3(LeafWidth, LeafBottom + .002f, -bt), new Vector3(LeafWidth + Gap + .0002f, LeafBottom + LeafHeight - .002f, bt), 1);
        return meshes["meeting"] = b.ToMesh("Stream door meeting brush");
    }

    static void HingeBarrel(MeshBuilder b, Vector3 centre)
    {
        b.Cylinder(centre, HingeBarrelRadius, HingeBarrelLength, 1, 32);
        var tip = new Vector3(0f, HingeBarrelLength * .5f + .0025f, 0f);
        b.Cylinder(centre + tip, HingeBarrelRadius * .78f, .005f, 1, 32);
        b.Cylinder(centre - tip, HingeBarrelRadius * .78f, .005f, 1, 32);
    }

    /// <summary>The hinges' jamb halves: the -Z barrels and the flanges mortised into the jamb linings, which never move.</summary>
    static Mesh HingeJambMesh()
    {
        Mesh mesh;
        if (Cached("hingeJamb", out mesh)) return mesh;
        var b = new MeshBuilder();
        var half = Width * .5f;
        var hf = HingeFlangeLength * .5f;
        foreach (var hand in new[] { -1f, 1f })
            foreach (var y in HingeCentres())
            {
                var axisX = hand * (half - Gap * .5f);
                HingeBarrel(b, new Vector3(axisX, y, -HingeAxisOffset));
                var x0 = hand * half;
                var x1 = hand * (half - .0006f);
                b.Box(new Vector3(Mathf.Min(x0, x1), y - hf, -HingeAxisOffset), new Vector3(Mathf.Max(x0, x1), y + hf, .010f), 1);
            }
        return meshes["hingeJamb"] = b.ToMesh("Stream door hinges, jamb halves");
    }

    /// <summary>One leaf's +Z barrels and centre channels.</summary>
    static Mesh HingeBodyMesh(int hand)
    {
        var key = "hingeBody" + hand;
        Mesh mesh;
        if (Cached(key, out mesh)) return mesh;
        var b = new MeshBuilder();
        var axisX = hand * (Width * .5f - Gap * .5f);
        var hf = HingeFlangeLength * .5f;
        foreach (var y in HingeCentres())
        {
            HingeBarrel(b, new Vector3(axisX, y, HingeAxisOffset));
            b.Box(new Vector3(axisX - .0008f, y - hf, -HingeAxisOffset), new Vector3(axisX + .0008f, y + hf, HingeAxisOffset), 1);
        }
        return meshes[key] = b.ToMesh("Stream door hinge barrels " + (hand < 0 ? "west" : "east"));
    }

    /// <summary>One leaf's flanges mortised into its hinge edge (reaching its +Z barrels) and its pulls' posts (carrier space: hinge edge x 0, centre plane z 0).</summary>
    static Mesh LeafHardwareMesh(int hand, bool both)
    {
        var key = "leafHardware" + hand + (both ? "" : "+");
        Mesh mesh;
        if (Cached(key, out mesh)) return mesh;
        var b = new MeshBuilder();
        var hf = HingeFlangeLength * .5f;
        foreach (var y in HingeCentres())
            b.Box(new Vector3(Mathf.Min(0f, hand * .0006f), y - hf, -.010f), new Vector3(Mathf.Max(0f, hand * .0006f), y + hf, HingeAxisOffset), 1);
        var handleX = -hand * (Width * .5f - Gap - .10f);
        foreach (var face in new[] { -1f, 1f })
        {
            if (face < 0f && !both) continue;
            foreach (var dy in new[] { -.06f, .06f })
                b.Cylinder(new Vector3(handleX, 1.22f + dy, face * (LeafThickness * .5f + .003f)), .007f, .010f, 2, 20);
        }
        return meshes[key] = b.ToMesh("Stream door leaf hardware " + (hand < 0 ? "west" : "east"));
    }

    sealed class MeshBuilder
    {
        readonly List<Vector3> v = new List<Vector3>();
        readonly List<Vector3> n = new List<Vector3>();
        readonly List<Vector2> uv = new List<Vector2>();
        readonly List<int> t = new List<int>();

        int Vert(Vector3 p, Vector3 normal, Vector2 tex)
        {
            v.Add(p); n.Add(normal); uv.Add(tex);
            return v.Count - 1;
        }

        // A triangle wound to face 'facing' (Unity: clockwise seen from the front).
        void Tri(int a, int b, int c, Vector3 facing)
        {
            if (Vector3.Dot(Vector3.Cross(v[b] - v[a], v[c] - v[a]), facing) < 0f) { var s = b; b = c; c = s; }
            t.Add(a); t.Add(b); t.Add(c);
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 na, Vector3 nb, Vector3 nc, Vector3 nd, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud, Vector3 facing)
        {
            var i0 = Vert(a, na, ua); var i1 = Vert(b, nb, ub); var i2 = Vert(c, nc, uc); var i3 = Vert(d, nd, ud);
            Tri(i0, i1, i2, facing);
            Tri(i0, i2, i3, facing);
        }

        // A flat quad, UV projected in metres (uAxis: the axis U runs along; V the other in-plane axis), measured as in
        // a stream room's own space (its door line at z 12), so the stream door's grain lies where it did before the kit.
        public void QuadUV(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, int uAxis)
        {
            QuadUV(a, b, c, d, normal, uAxis, StreamUvOrigin);
        }

        static readonly Vector3 StreamUvOrigin = new Vector3(0f, 0f, -12f);

        // As above, the UVs measured from uvOrigin (a box's min corner, as GetPlanarBox).
        public void QuadUV(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, int uAxis, Vector3 uvOrigin)
        {
            Quad(a, b, c, d, normal, normal, normal, normal, Project(a - uvOrigin, normal, uAxis), Project(b - uvOrigin, normal, uAxis),
                Project(c - uvOrigin, normal, uAxis), Project(d - uvOrigin, normal, uAxis), normal);
        }

        static Vector2 Project(Vector3 p, Vector3 normal, int uAxis)
        {
            var drop = Mathf.Abs(normal.x) >= Mathf.Abs(normal.y) && Mathf.Abs(normal.x) >= Mathf.Abs(normal.z) ? 0 : (Mathf.Abs(normal.y) >= Mathf.Abs(normal.z) ? 1 : 2);
            if (uAxis == drop) uAxis = drop == 0 ? 2 : 0;
            var vAxis = 3 - drop - uAxis;
            return new Vector2(p[uAxis], p[vAxis]);
        }

        // An axis-aligned box from min to max, U along grainAxis where the face holds it.
        public void Box(Vector3 min, Vector3 max, int grainAxis)
        {
            for (var k = 0; k < 3; k++)
                for (var s = -1; s <= 1; s += 2)
                {
                    var normal = Vector3.zero; normal[k] = s;
                    var i = (k + 1) % 3; var j = (k + 2) % 3;
                    var p = new Vector3[4];
                    for (var q = 0; q < 4; q++)
                    {
                        var c = Vector3.zero;
                        c[k] = s < 0 ? min[k] : max[k];
                        c[i] = (q == 1 || q == 2) ? max[i] : min[i];
                        c[j] = (q >= 2) ? max[j] : min[j];
                        p[q] = c;
                    }
                    QuadUV(p[0], p[1], p[2], p[3], normal, grainAxis);
                }
        }

        // A box with chamfered (bevel) arrises: six faces, twelve chamfers, eight corners, all flat.
        // UVs as FrontRoomsFilmMesh.GetPlanarBox (front/back x,y; sides z,y; top/bottom x,z), from the min corner.
        public void BevelBox(Vector3 centre, Vector3 size, float bev)
        {
            var h = size * .5f;
            var o = centre - h;
            for (var k = 0; k < 3; k++)
                for (var s = -1; s <= 1; s += 2)
                {
                    var i = (k + 1) % 3; var j = (k + 2) % 3;
                    var normal = Vector3.zero; normal[k] = s;
                    var p = new Vector3[4];
                    for (var q = 0; q < 4; q++)
                    {
                        var si = (q == 1 || q == 2) ? 1 : -1;
                        var sj = q >= 2 ? 1 : -1;
                        p[q] = centre + BevelPoint(h, bev, k, k == 0 ? s : (i == 0 ? si : sj), k == 1 ? s : (i == 1 ? si : sj), k == 2 ? s : (i == 2 ? si : sj));
                    }
                    QuadUV(p[0], p[1], p[2], p[3], normal, k == 2 ? 0 : (k == 0 ? 2 : 0), o);
                }
            // chamfers between faces a and b along the third axis c
            for (var a = 0; a < 3; a++)
                for (var bb = a + 1; bb < 3; bb++)
                {
                    var c = 3 - a - bb;
                    for (var sa = -1; sa <= 1; sa += 2)
                        for (var sb = -1; sb <= 1; sb += 2)
                        {
                            var s = new int[3];
                            s[a] = sa; s[bb] = sb;
                            s[c] = -1;
                            var p0 = centre + BevelPoint(h, bev, a, s[0], s[1], s[2]);
                            var p3 = centre + BevelPoint(h, bev, bb, s[0], s[1], s[2]);
                            s[c] = 1;
                            var p1 = centre + BevelPoint(h, bev, a, s[0], s[1], s[2]);
                            var p2 = centre + BevelPoint(h, bev, bb, s[0], s[1], s[2]);
                            var normal = Vector3.zero; normal[a] = sa; normal[bb] = sb;
                            normal.Normalize();
                            QuadUV(p0, p1, p2, p3, normal, c == 1 ? 0 : c, o);
                        }
                }
            for (var sx = -1; sx <= 1; sx += 2)
                for (var sy = -1; sy <= 1; sy += 2)
                    for (var sz = -1; sz <= 1; sz += 2)
                    {
                        var normal = new Vector3(sx, sy, sz).normalized;
                        var pa = centre + BevelPoint(h, bev, 0, sx, sy, sz);
                        var pb = centre + BevelPoint(h, bev, 1, sx, sy, sz);
                        var pc = centre + BevelPoint(h, bev, 2, sx, sy, sz);
                        var ia = Vert(pa, normal, Project(pa - o, normal, 0));
                        var ib = Vert(pb, normal, Project(pb - o, normal, 0));
                        var ic = Vert(pc, normal, Project(pc - o, normal, 0));
                        Tri(ia, ib, ic, normal);
                    }
        }

        // The corner point (signs sx, sy, sz) as it lies on the face of axis 'face'.
        static Vector3 BevelPoint(Vector3 h, float bev, int face, int sx, int sy, int sz)
        {
            return new Vector3(sx * (face == 0 ? h.x : h.x - bev), sy * (face == 1 ? h.y : h.y - bev), sz * (face == 2 ? h.z : h.z - bev));
        }

        // A capped cylinder along an axis (0 x, 1 y, 2 z), smooth sides, U round the girth in metres.
        public void Cylinder(Vector3 centre, float radius, float length, int axis, int segments)
        {
            var ax = Vector3.zero; ax[axis] = 1f;
            var e1 = Vector3.zero; e1[(axis + 1) % 3] = 1f;
            var e2 = Vector3.zero; e2[(axis + 2) % 3] = 1f;
            var top = centre + ax * length * .5f;
            var bottom = centre - ax * length * .5f;
            for (var k = 0; k < segments; k++)
            {
                var a0 = k * Mathf.PI * 2f / segments;
                var a1 = (k + 1) * Mathf.PI * 2f / segments;
                var r0 = e1 * Mathf.Cos(a0) + e2 * Mathf.Sin(a0);
                var r1 = e1 * Mathf.Cos(a1) + e2 * Mathf.Sin(a1);
                var u0 = a0 * radius; var u1 = a1 * radius;
                Quad(bottom + r0 * radius, bottom + r1 * radius, top + r1 * radius, top + r0 * radius, r0, r1, r1, r0,
                    new Vector2(u0, 0f), new Vector2(u1, 0f), new Vector2(u1, length), new Vector2(u0, length), (r0 + r1).normalized);
                foreach (var end in new[] { -1f, 1f })
                {
                    var o = end < 0f ? bottom : top;
                    var cn = ax * end;
                    var ic = Vert(o, cn, Vector2.zero);
                    var i0 = Vert(o + r0 * radius, cn, new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * radius);
                    var i1 = Vert(o + r1 * radius, cn, new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * radius);
                    Tri(ic, i0, i1, cn);
                }
            }
        }

        // A profile (u across, w proud) swept along 'along' from a0 + m0 u to a1 + m1 u (45 deg mitres: m +-1).
        // Smooth across profile joints that turn less than 40 degrees; U along the sweep (the grain), V round the profile.
        public void Sweep(Vector3 origin, Vector3 along, Vector3 across, Vector3 proud, Vector2[] profile, float a0, float m0, float a1, float m1)
        {
            var count = profile.Length - 1;
            var seg = new Vector3[count];
            for (var k = 0; k < count; k++)
            {
                var d = profile[k + 1] - profile[k];
                var n2 = new Vector2(-d.y, d.x).normalized;
                seg[k] = (across * n2.x + proud * n2.y).normalized;
            }
            var vlen = 0f;
            for (var k = 0; k < count; k++)
            {
                var pa = profile[k]; var pb = profile[k + 1];
                var na = k > 0 && Vector3.Angle(seg[k - 1], seg[k]) < 40f ? (seg[k - 1] + seg[k]).normalized : seg[k];
                var nb = k < count - 1 && Vector3.Angle(seg[k], seg[k + 1]) < 40f ? (seg[k] + seg[k + 1]).normalized : seg[k];
                var la = (pb - pa).magnitude;
                float sa0 = a0 + m0 * pa.x, sa1 = a1 + m1 * pa.x, sb0 = a0 + m0 * pb.x, sb1 = a1 + m1 * pb.x;
                var baseA = origin + across * pa.x + proud * pa.y;
                var baseB = origin + across * pb.x + proud * pb.y;
                Quad(baseA + along * sa0, baseA + along * sa1, baseB + along * sb1, baseB + along * sb0, na, na, nb, nb,
                    new Vector2(sa0, vlen), new Vector2(sa1, vlen), new Vector2(sb1, vlen + la), new Vector2(sb0, vlen + la), seg[k]);
                vlen += la;
            }
        }

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name, hideFlags = HideFlags.DontSave };
            mesh.SetVertices(v);
            mesh.SetNormals(n);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(t, 0);
            mesh.RecalculateBounds();
            try { mesh.RecalculateTangents(); } catch (Exception) { }
            return mesh;
        }
    }
}
