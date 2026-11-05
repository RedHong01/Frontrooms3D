using System;
using System.Collections.Generic;
using FrontRooms.Map;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// The pool family's STAND-IN kits (s67_pool DESIGN section 6, structures PLAN 2.7): greybox props built from boxes and
/// prisms, one mesh per material, no Unity colliders (a kit that blocks hands one box to the map's collision mesh). Named
/// "STAND-IN ..." in the hierarchy so the rendering line's real kit replaces this one table. Kit-local axes: X the long
/// axis, Y up, +Z the front (a kit's back stands against a wall); yaw turns it as Unity does (yaw 90: X runs along -Z,
/// the front faces +X). The reference point is the kit's centre on its floor, except Kit_PipeRail, which starts there and
/// runs along -X (with yaw 90: along +Z), following the floor. Sizes in metres; length 0 is the kit's default. A kit's box
/// changed here changes ModuleStandIn.Footprint too (the pool plan keeps its openings clear of kits by it).
/// </summary>
public static class FrontRoomsPoolStandIns
{
    sealed class Part
    {
        public readonly List<Vector3> v = new List<Vector3>();
        public readonly List<Vector3> n = new List<Vector3>();
        public readonly List<Vector2> uv = new List<Vector2>();
        public readonly List<int> t = new List<int>();
    }

    sealed class Kit
    {
        public readonly Dictionary<Material, Part> parts = new Dictionary<Material, Part>();
        public Quaternion rotation;
        public Vector3 position;
        public bool hasBounds;
        public Bounds bounds;
        public readonly HashSet<Material> glows = new HashSet<Material>();

        Part PartOf(Material m)
        {
            if (!parts.TryGetValue(m, out var p)) parts[m] = p = new Part();
            return p;
        }

        // A box in kit-local space (centre, size), turned and placed into the parent's space.
        public void Box(Vector3 c, Vector3 s, Material m, bool solid = true)
        {
            if (m == null) return;
            var h = s * .5f;
            Face(m, c + Vector3.right * h.x, Vector3.right, Vector3.up, Vector3.forward, h.y, h.z);
            Face(m, c - Vector3.right * h.x, Vector3.left, Vector3.forward, Vector3.up, h.z, h.y);
            Face(m, c + Vector3.up * h.y, Vector3.up, Vector3.forward, Vector3.right, h.z, h.x);
            Face(m, c - Vector3.up * h.y, Vector3.down, Vector3.right, Vector3.forward, h.x, h.z);
            Face(m, c + Vector3.forward * h.z, Vector3.forward, Vector3.right, Vector3.up, h.x, h.y);
            Face(m, c - Vector3.forward * h.z, Vector3.back, Vector3.up, Vector3.right, h.y, h.x);
            if (!solid) return;
            for (var k = 0; k < 8; k++)
            {
                var corner = c + new Vector3((k & 1) == 0 ? -h.x : h.x, (k & 2) == 0 ? -h.y : h.y, (k & 4) == 0 ? -h.z : h.z);
                var p = position + rotation * corner;
                if (!hasBounds) { bounds = new Bounds(p, Vector3.zero); hasBounds = true; }
                else bounds.Encapsulate(p);
            }
        }

        // u x v points along the normal, so (0,1,2)(0,2,3) is clockwise seen from the front (as the map's MeshBuilder).
        void Face(Material m, Vector3 c, Vector3 normal, Vector3 u, Vector3 v, float uh, float vh)
        {
            var p = PartOf(m);
            var i = p.v.Count;
            var nw = rotation * normal;
            Corner(p, c - u * uh - v * vh, nw);
            Corner(p, c + u * uh - v * vh, nw);
            Corner(p, c + u * uh + v * vh, nw);
            Corner(p, c - u * uh + v * vh, nw);
            p.t.Add(i); p.t.Add(i + 1); p.t.Add(i + 2);
            p.t.Add(i); p.t.Add(i + 2); p.t.Add(i + 3);
        }

        void Corner(Part p, Vector3 local, Vector3 normal)
        {
            var w = position + rotation * local;
            p.v.Add(w);
            p.n.Add(normal);
            var ax = Mathf.Abs(normal.x);
            var ay = Mathf.Abs(normal.y);
            var az = Mathf.Abs(normal.z);
            p.uv.Add(ax >= ay && ax >= az ? new Vector2(w.z, w.y) : ay >= az ? new Vector2(w.x, w.z) : new Vector2(w.x, w.y));
        }

        // A square-section bar between two kit-local points (rails, pipes, ladder rails, sloped segments).
        public void Bar(Vector3 a, Vector3 b, float thick, Material m, bool solid = false)
        {
            if (m == null) return;
            var d = b - a;
            var len = d.magnitude;
            if (len < 1e-4f) return;
            var dir = d / len;
            var side = Mathf.Abs(Vector3.Dot(dir, Vector3.up)) > .9f ? Vector3.right : Vector3.up;
            var u = Vector3.Cross(dir, side).normalized * (thick * .5f);
            var v = Vector3.Cross(dir, u).normalized * (thick * .5f);
            var p = PartOf(m);
            // Four long faces as quads (start ring to end ring).
            var ring = new[] { u + v, u - v, -u - v, -u + v };
            for (var k = 0; k < 4; k++)
            {
                var r0 = ring[k];
                var r1 = ring[(k + 1) & 3];
                var nrm = rotation * (r0 + r1).normalized;
                var i = p.v.Count;
                Corner(p, a + r0, nrm);
                Corner(p, b + r0, nrm);
                Corner(p, b + r1, nrm);
                Corner(p, a + r1, nrm);
                // Winding: check against the outward normal.
                var w0 = p.v[i]; var w1 = p.v[i + 1]; var w2 = p.v[i + 2];
                if (Vector3.Dot(Vector3.Cross(w1 - w0, w2 - w0), nrm) >= 0f) { p.t.Add(i); p.t.Add(i + 1); p.t.Add(i + 2); p.t.Add(i); p.t.Add(i + 2); p.t.Add(i + 3); }
                else { p.t.Add(i); p.t.Add(i + 2); p.t.Add(i + 1); p.t.Add(i); p.t.Add(i + 3); p.t.Add(i + 2); }
            }
            if (solid)
            {
                foreach (var q in new[] { a, b })
                {
                    var w = position + rotation * q;
                    if (!hasBounds) { bounds = new Bounds(w, Vector3.one * thick); hasBounds = true; }
                    else bounds.Encapsulate(new Bounds(w, Vector3.one * thick));
                }
            }
        }

        // A flat disc facing kit-local `facing` (unit), centre c, radius r.
        public void Disc(Vector3 c, Vector3 facing, float r, Material m, int segments = 20)
        {
            if (m == null) return;
            var p = PartOf(m);
            var side = Mathf.Abs(Vector3.Dot(facing, Vector3.up)) > .9f ? Vector3.right : Vector3.up;
            var u = Vector3.Cross(facing, side).normalized;
            var v = Vector3.Cross(facing, u).normalized;
            var nrm = rotation * facing;
            var i0 = p.v.Count;
            Corner(p, c, nrm);
            for (var k = 0; k <= segments; k++)
            {
                var a = k * Mathf.PI * 2f / segments;
                Corner(p, c + (u * Mathf.Cos(a) + v * Mathf.Sin(a)) * r, nrm);
            }
            for (var k = 1; k <= segments; k++)
            {
                var w0 = p.v[i0]; var w1 = p.v[i0 + k]; var w2 = p.v[i0 + k + 1];
                if (Vector3.Dot(Vector3.Cross(w1 - w0, w2 - w0), nrm) >= 0f) { p.t.Add(i0); p.t.Add(i0 + k); p.t.Add(i0 + k + 1); }
                else { p.t.Add(i0); p.t.Add(i0 + k + 1); p.t.Add(i0 + k); }
            }
        }

        // A ring of boxes (round frames, the ring buoy) in the kit-local plane facing `facing`.
        public void Ring(Vector3 c, Vector3 facing, float r, float thick, float depth, Material m, int segments = 12)
        {
            var side = Mathf.Abs(Vector3.Dot(facing, Vector3.up)) > .9f ? Vector3.right : Vector3.up;
            var u = Vector3.Cross(facing, side).normalized;
            var v = Vector3.Cross(facing, u).normalized;
            for (var k = 0; k < segments; k++)
            {
                var a0 = k * Mathf.PI * 2f / segments;
                var a1 = (k + 1) * Mathf.PI * 2f / segments;
                var p0 = c + (u * Mathf.Cos(a0) + v * Mathf.Sin(a0)) * r;
                var p1 = c + (u * Mathf.Cos(a1) + v * Mathf.Sin(a1)) * r;
                Bar(p0, p1, thick, m);
            }
            _ = depth;
        }
    }

    static Material Grey => FrontRoomsPoolMaterials.Lit("stand-in grey", new Color(.62f, .64f, .65f), .3f);
    static Material Dark => FrontRoomsPoolMaterials.Lit("stand-in dark", new Color(.08f, .09f, .10f), .3f);
    static Material Sign => FrontRoomsPoolMaterials.Lit("sign", new Color(.16f, .17f, .18f), .3f);
    static Material Locker => FrontRoomsPoolMaterials.Lit("locker", new Color(.72f, .75f, .76f), .45f);
    static Material Chair => FrontRoomsPoolMaterials.Lit("chair", new Color(.92f, .92f, .90f), .5f);
    static Material Teal => FrontRoomsPoolMaterials.Lit("teal", new Color(.16f, .44f, .46f), .45f);
    static Material Rose => FrontRoomsPoolMaterials.Lit("rose", new Color(.74f, .48f, .54f), .45f);
    static Material CourtRed => FrontRoomsPoolMaterials.Lit("court red", new Color(.6f, .12f, .1f), .3f);
    static Material Wood => FrontRoomsPoolMaterials.Lit("desk", new Color(.55f, .52f, .48f), .3f);
    static Material Towel => FrontRoomsPoolMaterials.Lit("towel", new Color(.95f, .95f, .93f), .1f);
    static Material Board => FrontRoomsPoolMaterials.Lit("board face", new Color(.88f, .88f, .84f), .2f);
    static Material Paint => FrontRoomsPoolMaterials.Lit("beam paint", new Color(.90f, .91f, .89f), .2f);
    static Material Tube => FrontRoomsPoolMaterials.Lit("slide tube", new Color(.20f, .55f, .60f), .6f);

    /// <summary>
    /// Build one kit under <paramref name="parent"/> at <paramref name="localPosition"/> (the parent's space), turned by
    /// <paramref name="yaw"/>. <paramref name="collide"/> (centre, size in the parent's space; null: no collision) gets the
    /// kit's bounding box; <paramref name="floorAt"/> gives the floor under a parent-space point (Kit_PipeRail follows it).
    /// Unknown kits log once and build a grey box. Meshes made go into <paramref name="meshes"/> (freed with the chunk).
    /// </summary>
    public static GameObject Build(string kit, Transform parent, Vector3 localPosition, float yaw, float length,
        Action<Vector3, Vector3> collide, Func<Vector3, float> floorAt, List<Mesh> meshes)
    {
        if (parent == null) return null;
        var k = new Kit { rotation = Quaternion.Euler(0f, yaw, 0f), position = localPosition };
        var glowBlock = FrontRoomsPoolMaterials.Glow("glass block", new Color(1.35f, 1.5f, 1.55f));
        var glowWindow = FrontRoomsPoolMaterials.Glow("window", new Color(.30f, 1.35f, 1.45f));
        k.glows.Add(glowBlock);
        k.glows.Add(glowWindow);
        var steel = FrontRoomsPoolMaterials.Steel;
        switch (kit)
        {
            case "Kit_TowelDesk":
            {
                var l = length > 0f ? length : 3f;
                k.Box(new Vector3(0f, .5f, 0f), new Vector3(l, 1f, .6f), Wood);
                k.Box(new Vector3(0f, 1.02f, .03f), new Vector3(l + .1f, .04f, .66f), Board);
                k.Box(new Vector3(-l * .27f, 1.17f, 0f), new Vector3(.4f, .26f, .32f), Towel, false);
                k.Box(new Vector3(l * .27f, 1.17f, 0f), new Vector3(.4f, .26f, .32f), Towel, false);
                break;
            }
            case "Kit_SignBlank":
                k.Box(Vector3.zero, new Vector3(length > 0f ? length : 1.2f, .4f, .05f), Sign, false);
                break;
            case "Kit_LockerBank":
            {
                var l = length > 0f ? length : 3.4f;
                k.Box(new Vector3(0f, .05f, -.02f), new Vector3(l, .1f, .36f), Dark);
                k.Box(new Vector3(0f, .1f + .915f, 0f), new Vector3(l, 1.83f, .4f), Locker);
                // Door seams every 0.3 m and a vent row, 5 mm proud.
                for (var x = -l * .5f + .3f; x < l * .5f - .01f; x += .3f)
                    k.Box(new Vector3(x, 1.0f, .2025f), new Vector3(.01f, 1.7f, .005f), Dark, false);
                k.Box(new Vector3(0f, 1.0f, .2025f), new Vector3(l - .04f, .008f, .005f), Dark, false);
                break;
            }
            case "Kit_LockerBench":
            {
                var l = length > 0f ? length : 3.4f;
                k.Box(new Vector3(0f, .45f, 0f), new Vector3(l, .05f, .35f), Wood);
                k.Box(new Vector3(-l * .5f + .3f, .21f, 0f), new Vector3(.06f, .42f, .3f), steel);
                k.Box(new Vector3(l * .5f - .3f, .21f, 0f), new Vector3(.06f, .42f, .3f), steel);
                break;
            }
            case "Kit_ShowerSet":
                k.Bar(new Vector3(0f, 1.0f, -.12f), new Vector3(0f, 2.08f, -.12f), .03f, steel);
                k.Bar(new Vector3(0f, 2.08f, -.12f), new Vector3(0f, 2.08f, .2f), .03f, steel);
                k.Disc(new Vector3(0f, 2.04f, .22f), Vector3.down, .08f, steel, 14);
                k.Box(new Vector3(0f, 1.1f, -.1f), new Vector3(.12f, .12f, .06f), steel, false);
                break;
            case "Kit_GlassBlockHalfWall":
            {
                var l = length > 0f ? length : 2.9f;
                k.Box(new Vector3(0f, .75f, 0f), new Vector3(l, 1.5f, .1f), glowBlock);
                k.Box(new Vector3(0f, 1.52f, 0f), new Vector3(l + .02f, .04f, .14f), FrontRoomsPoolMaterials.Tile, false);
                Grout(k, l, 1.5f, .05f, .2f);
                break;
            }
            case "Kit_GlassBlockWall":
            {
                var l = length > 0f ? length : 3.4f;
                k.Box(new Vector3(0f, 1.3f, 0f), new Vector3(l, 2.6f, .05f), glowBlock);
                Grout(k, l, 2.6f, .025f, .2f);
                break;
            }
            case "Kit_FloorDrain":
                k.Box(new Vector3(0f, .002f, 0f), new Vector3(.2f, .004f, .2f), Dark, false);
                break;
            case "Kit_MainDrainGrate":
                k.Box(new Vector3(0f, .002f, 0f), new Vector3(.5f, .004f, .5f), Dark, false);
                for (var x = -.2f; x <= .21f; x += .1f) k.Box(new Vector3(x, .005f, 0f), new Vector3(.02f, .004f, .48f), steel, false);
                break;
            case "Kit_PoolLadder":
                // Two rails over the coping into the basin (front +Z), four rungs under the water.
                foreach (var x in new[] { -.25f, .25f })
                {
                    k.Bar(new Vector3(x, 0f, -.35f), new Vector3(x, .9f, -.35f), .04f, steel, true);
                    k.Bar(new Vector3(x, .9f, -.35f), new Vector3(x, .9f, .12f), .04f, steel);
                    k.Bar(new Vector3(x, .9f, .12f), new Vector3(x, -1.2f, .12f), .04f, steel);
                }
                for (var r = 0; r < 4; r++) k.Box(new Vector3(0f, -.25f - r * .27f, .12f), new Vector3(.5f, .03f, .08f), steel, false);
                break;
            case "Kit_GuardChair":
            {
                foreach (var x in new[] { -.3f, .3f })
                foreach (var z in new[] { -.3f, .3f })
                    k.Box(new Vector3(x, .875f, z), new Vector3(.05f, 1.75f, .05f), Chair);
                k.Box(new Vector3(0f, 1.775f, 0f), new Vector3(.68f, .05f, .66f), Chair);
                k.Box(new Vector3(0f, 2.1f, -.3f), new Vector3(.68f, .6f, .05f), Chair);
                for (var r = 1; r <= 3; r++) k.Box(new Vector3(0f, r * .44f, .3f), new Vector3(.6f, .04f, .08f), Chair, false);
                break;
            }
            case "Kit_RingBuoy":
                k.Ring(new Vector3(0f, 0f, .06f), Vector3.forward, .29f, .1f, .1f, Rose, 8);
                break;
            case "Kit_RulesBoard":
                k.Box(Vector3.zero, new Vector3(1.2f, .9f, .04f), Sign, false);
                k.Box(new Vector3(0f, 0f, .025f), new Vector3(.95f, .7f, .01f), Board, false);
                break;
            case "Kit_PaceClock":
                k.Disc(new Vector3(0f, 0f, .03f), Vector3.forward, .4f, Board, 28);
                k.Ring(new Vector3(0f, 0f, .03f), Vector3.forward, .4f, .04f, .04f, Dark, 20);
                k.Box(new Vector3(0f, .12f, .04f), new Vector3(.02f, .26f, .01f), Dark, false);
                k.Box(new Vector3(.1f, 0f, .045f), new Vector3(.2f, .015f, .01f), CourtRed, false);
                break;
            case "Kit_DepthMark":
                k.Box(new Vector3(0f, .002f, 0f), new Vector3(.3f, .004f, .2f), Dark, false);
                break;
            case "Kit_ObsWindow":
                k.Box(new Vector3(0f, .5f, .02f), new Vector3(1.0f, .08f, .04f), steel, false);
                k.Box(new Vector3(0f, -.15f, .02f), new Vector3(1.0f, .08f, .04f), steel, false);
                k.Box(new Vector3(-.46f, .175f, .02f), new Vector3(.08f, .7f, .04f), steel, false);
                k.Box(new Vector3(.46f, .175f, .02f), new Vector3(.08f, .7f, .04f), steel, false);
                k.Box(new Vector3(0f, .175f, .01f), new Vector3(.84f, .56f, .01f), glowWindow, false);
                break;
            case "Kit_Soffit":
            {
                var l = length > 0f ? length : 15f;
                // The soffit's underside is the map's Level 0 ceiling with two troffer lenses (render only).
                k.Box(new Vector3(0f, .125f, 0f), new Vector3(l, .25f, 3f), Paint, false);
                var ceiling = FrontRoomsSurfaces.Room(RoomRule.Lobby, FrontRoomsSurfaces.Slot.Ceiling);
                if (ceiling != null) k.Box(new Vector3(0f, -.002f, 0f), new Vector3(l - .02f, .004f, 2.98f), ceiling, false);
                var lens = FrontRoomsPoolMaterials.Glow("soffit lens", new Color(1.5f, 1.55f, 1.55f));
                k.glows.Add(lens);
                k.Box(new Vector3(-3f, -.012f, 0f), new Vector3(.6f, .02f, 1.2f), lens, false);
                k.Box(new Vector3(3f, -.012f, 0f), new Vector3(.6f, .02f, 1.2f), lens, false);
                break;
            }
            case "Kit_Beam":
            {
                var l = length > 0f ? length : 15f;
                k.Box(new Vector3(0f, .25f, 0f), new Vector3(l, .5f, .5f), Paint, false);
                break;
            }
            case "Kit_PipeRail":
                PipeRail(k, length > 0f ? length : 6f, steel, floorAt, parent);
                break;
            case "Kit_GateBars":
                // The gate pushed aside, left of the 1.2 m opening (open: Red's v1 dark zone walk).
                for (var b = 0; b < 7; b++) k.Bar(new Vector3(-1.25f + b * .1f, 0f, -.05f), new Vector3(-1.25f + b * .1f, 2.08f, -.05f), .028f, steel);
                foreach (var y in new[] { .1f, 1.04f, 2.0f }) k.Box(new Vector3(-.95f, y, -.05f), new Vector3(.66f, .04f, .03f), steel, false);
                break;
            case "Kit_PipeRun":
            {
                var l = length > 0f ? length : 6f;
                k.Bar(new Vector3(-l * .5f, 0f, 0f), new Vector3(l * .5f, 0f, 0f), .14f, steel);
                for (var x = -l * .5f + .5f; x < l * .5f; x += 2.5f) k.Box(new Vector3(x, .1f, 0f), new Vector3(.04f, .2f, .16f), Dark, false);
                break;
            }
            case "Kit_Oculus":
                k.Ring(new Vector3(0f, 0f, .02f), Vector3.forward, .66f, .12f, .06f, Teal, 24);
                k.Disc(new Vector3(0f, 0f, .01f), Vector3.forward, .6f, glowBlock, 28);
                break;
            case "Kit_SlideTube":
                k.Bar(new Vector3(0f, 0f, -.2f), new Vector3(0f, 0f, 1.4f), 1.0f, Tube);
                k.Bar(new Vector3(0f, 0f, 1.4f), new Vector3(0f, -1.6f, 3.2f), 1.0f, Tube);
                k.Disc(new Vector3(0f, -1.62f, 3.72f), new Vector3(0f, -.5f, .85f).normalized, .45f, Dark, 20);
                break;
            case "Kit_CourtLines":
            {
                const float w = 5f, d = 11f, line = .05f;
                k.Box(new Vector3(0f, .002f, -d * .5f), new Vector3(w, .003f, line), CourtRed, false);
                k.Box(new Vector3(0f, .002f, d * .5f), new Vector3(w, .003f, line), CourtRed, false);
                k.Box(new Vector3(-w * .5f, .002f, 0f), new Vector3(line, .003f, d), CourtRed, false);
                k.Box(new Vector3(w * .5f, .002f, 0f), new Vector3(line, .003f, d), CourtRed, false);
                k.Box(new Vector3(0f, .002f, 0f), new Vector3(w, .003f, line), CourtRed, false);
                break;
            }
            default:
                FrontRoomsPoolMaterials.LogOnce("kit " + kit, "unknown stand-in kit '" + kit + "': a grey box stands in.");
                k.Box(new Vector3(0f, .25f, 0f), new Vector3(.5f, .5f, .5f), Grey);
                break;
        }

        var root = new GameObject("STAND-IN " + kit);
        root.transform.SetParent(parent, false);
        foreach (var pair in k.parts)
        {
            if (pair.Key == null || pair.Value.v.Count == 0) continue;
            var go = new GameObject(pair.Key.name);
            go.transform.SetParent(root.transform, false);
            var mesh = new Mesh { name = "STAND-IN " + kit + " " + pair.Key.name };
            if (pair.Value.v.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(pair.Value.v);
            mesh.SetNormals(pair.Value.n);
            mesh.SetUVs(0, pair.Value.uv);
            mesh.SetTriangles(pair.Value.t, 0);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            meshes?.Add(mesh);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = pair.Key;
            var glow = k.glows.Contains(pair.Key);
            r.shadowCastingMode = glow ? ShadowCastingMode.Off : ShadowCastingMode.On;
            r.receiveShadows = !glow;
        }
        if (collide != null && k.hasBounds) collide(k.bounds.center, k.bounds.size);
        return root;
    }

    // Grout lines over a glass-block face (both faces), every `step` metres.
    static void Grout(Kit k, float l, float h, float halfDepth, float step)
    {
        var dark = FrontRoomsPoolMaterials.Lit("glass block grout", new Color(.70f, .74f, .75f), .3f);
        for (var x = -l * .5f + step; x < l * .5f - .01f; x += step)
        foreach (var z in new[] { halfDepth + .002f, -halfDepth - .002f })
            k.Box(new Vector3(x, h * .5f, z), new Vector3(.012f, h, .003f), dark, false);
        for (var y = step; y < h - .01f; y += step)
        foreach (var z in new[] { halfDepth + .002f, -halfDepth - .002f })
            k.Box(new Vector3(0f, y, z), new Vector3(l, .012f, .003f), dark, false);
    }

    // A 22 mm pipe rail from the reference point along kit-local -X, 0.9 m over the floor wherever it is, a post every 0.84 m
    // (three risers of 0.28). The reference point's y is the floor there plus 0.9 (the module's stand-in y).
    static void PipeRail(Kit k, float length, Material steel, Func<Vector3, float> floorAt, Transform parent)
    {
        if (steel == null) return;
        var step = .84f;
        var count = Mathf.Max(1, Mathf.CeilToInt(length / step));
        var baseFloor = floorAt != null ? floorAt(k.position) : k.position.y - .9f;
        Vector3 Top(float s)
        {
            var local = new Vector3(-s, 0f, 0f);
            var world = k.position + k.rotation * local;
            var f = floorAt != null ? floorAt(world) : baseFloor;
            // Kit-local y is relative to the reference point (y = floor there + 0.9).
            return new Vector3(-s, f + .9f - k.position.y, 0f);
        }
        var prev = Top(0f);
        for (var i = 0; i <= count; i++)
        {
            var s = Mathf.Min(length, i * step);
            var top = Top(s);
            k.Bar(new Vector3(top.x, top.y - .9f, 0f), new Vector3(top.x, top.y, 0f), .03f, steel);
            if (i > 0) k.Bar(prev, top, .022f, steel);
            prev = top;
            if (s >= length) break;
        }
        _ = parent;
    }
}
