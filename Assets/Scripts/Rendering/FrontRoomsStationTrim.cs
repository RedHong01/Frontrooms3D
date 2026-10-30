using System.Collections.Generic;
using FrontRooms.Map;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// A26 station-look-v1 (research/station/70_station_look_v1_plan.md §5): the station's procedural, render-only trim in
/// walls 2 · Post Room (DEC 15:4x; 61 §5, §6; Red's pick image, PICK station_walls.py S2):
/// - room side (Station mask): the 6 in vinyl crash rail 0.78-0.93 m, 28 mm proud, 10 mm bevels; the 4 in black base,
///   6 mm proud; one 0.305 m dark VCT border course from every wall face, round the riser chase, stopping at D;
/// - at E1 and E2 (both masks): pressed-steel cased frames on both faces (face 0.05, proud 0.012) with 12 mm throat liners,
///   stainless corner guards on the station face (0.10-1.32 m), VCT through the throat, a rubber reducer at the opening's
///   outer (Level 0) face, and the 0.30 m gypsum band on the ceiling over the opening's cell edge.
/// No header panel over D (plan check C1-5) and no frame at D (C1-6): D is the stream's flush double door.
/// Built in the station set's frame (the map's metres), one mesh per material and mask (C2-10). No colliders, so the
/// map's clear widths (E1 1.70 m, E2 1.20 m) are kept. Inert without a station.
/// </summary>
public static class FrontRoomsStationTrim
{
    public const float RailBottom = .78f, RailTop = .93f, RailProud = .028f, RailBevel = .010f;
    public const float BaseHeight = .10f, BaseProud = .006f;
    public const float BorderWidth = .305f, BorderLift = .004f;
    public const float CasingFace = .05f, CasingProud = .012f, ThroatLiner = .012f;
    public const float GuardWing = .05f, GuardThick = .003f, GuardBottom = .10f, GuardTop = 1.32f;
    public const float ReducerWidth = .05f, ReducerHeight = .006f;
    public const float BandWidth = .30f, BandDepth = .06f;
    /// <summary>The rail and the base stop this far short of D's opening edges (the stream leaves open to 90 deg).</summary>
    public const float DoorStop = .05f;

    /// <summary>Which rendering-layer class a trim renderer takes once the layers are on (61 §7).</summary>
    public enum MaskClass { StationOnly, BothLayers }

    public struct Part
    {
        public Renderer renderer;
        public MaskClass mask;
    }

    /// <summary>
    /// The station's geometry in the set's frame (map metres): its inner faces, the openings' spans, the riser chase and
    /// D. Computed from the map's layout and the set's keep-clear rects, so the trim follows the authored spans.
    /// </summary>
    public struct Room
    {
        public float west, east, south, north;        // cell lines
        public float e1x0, e1x1, e2z0, e2z1;          // the arch openings' clear spans
        public float riserX, riserZ;                  // the riser chase's west and south faces
        public float doorX0, doorX1;                  // the stream door's opening
        public float ceiling;

        public static Room From(FrontRoomsMapWorld map)
        {
            var cs = MapGrid.CellSize;
            var l = map.StationLayout;
            var set = map.Station;
            var r = new Room
            {
                west = l.station.x * cs,
                east = (l.station.x + l.station.w) * cs,
                south = l.station.y * cs,
                north = (l.station.y + l.station.h) * cs,
                ceiling = ModuleUnits.StandardCeiling,
            };
            r.e1x0 = set.E1Clear.xMin;
            r.e1x1 = set.E1Clear.xMax;
            r.e2z0 = set.E2Clear.yMin;
            r.e2z1 = set.E2Clear.yMax;
            r.riserX = r.east - FrontRoomsStationSet.RiserWidth;
            r.riserZ = r.north - FrontRoomsStationSet.RiserDepth;
            var axis = (l.door.x + .5f) * cs;
            r.doorX0 = axis - FrontRoomsStationSet.StreamDoorWidth * .5f;
            r.doorX1 = axis + FrontRoomsStationSet.StreamDoorWidth * .5f;
            return r;
        }
    }

    // ------------------------------------------------------------------ mesh builder
    sealed class Builder
    {
        readonly List<Vector3> v = new List<Vector3>();
        readonly List<Vector3> n = new List<Vector3>();
        readonly List<Vector2> uv = new List<Vector2>();
        readonly List<int> t = new List<int>();
        public int Triangles => t.Count / 3;

        static Vector2 Planar(Vector3 p, Vector3 normal)
        {
            var a = new Vector3(Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z));
            if (a.y >= a.x && a.y >= a.z) return new Vector2(p.x, p.z);
            if (a.x >= a.z) return new Vector2(p.z * Mathf.Sign(normal.x), p.y);
            return new Vector2(-p.x * Mathf.Sign(normal.z), p.y);
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            var normal = Vector3.Cross(b - a, c - a).normalized;
            var i = v.Count;
            foreach (var p in new[] { a, b, c, d })
            {
                v.Add(p);
                n.Add(normal);
                uv.Add(Planar(p, normal));
            }
            t.Add(i); t.Add(i + 1); t.Add(i + 2);
            t.Add(i); t.Add(i + 2); t.Add(i + 3);
        }

        /// <summary>An axis box between two corners; faces listed in <paramref name="skip"/> (0 -x, 1 +x, 2 -y, 3 +y, 4 -z, 5 +z) are left out.</summary>
        public void Box(Vector3 lo, Vector3 hi, int skipMask = 0)
        {
            var p000 = new Vector3(lo.x, lo.y, lo.z); var p100 = new Vector3(hi.x, lo.y, lo.z);
            var p010 = new Vector3(lo.x, hi.y, lo.z); var p110 = new Vector3(hi.x, hi.y, lo.z);
            var p001 = new Vector3(lo.x, lo.y, hi.z); var p101 = new Vector3(hi.x, lo.y, hi.z);
            var p011 = new Vector3(lo.x, hi.y, hi.z); var p111 = new Vector3(hi.x, hi.y, hi.z);
            // Unity is left-handed: a quad wound clockwise as seen from its normal's side faces that side.
            if ((skipMask & 1) == 0) Quad(p001, p011, p010, p000);   // -x
            if ((skipMask & 2) == 0) Quad(p100, p110, p111, p101);   // +x
            if ((skipMask & 4) == 0) Quad(p000, p100, p101, p001);   // -y
            if ((skipMask & 8) == 0) Quad(p010, p011, p111, p110);   // +y
            if ((skipMask & 16) == 0) Quad(p000, p010, p110, p100);  // -z
            if ((skipMask & 32) == 0) Quad(p101, p111, p011, p001);  // +z
        }

        /// <summary>
        /// A profile (out from the wall, up from y0) swept along a wall run from <paramref name="start"/> by
        /// <paramref name="length"/> along <paramref name="along"/>, standing out along <paramref name="outward"/>; capped.
        /// </summary>
        public void Run(Vector3 start, Vector3 along, Vector3 outward, float length, Vector2[] profile)
        {
            var end = start + along * length;
            for (var k = 0; k < profile.Length - 1; k++)
            {
                var p0 = profile[k]; var p1 = profile[k + 1];
                var a0 = start + outward * p0.x + Vector3.up * p0.y;
                var a1 = start + outward * p1.x + Vector3.up * p1.y;
                var b0 = end + outward * p0.x + Vector3.up * p0.y;
                var b1 = end + outward * p1.x + Vector3.up * p1.y;
                // The profile runs counter-clockwise in (out, up), so (dy, -dx) is the segment's outward normal.
                var d2 = p1 - p0;
                QuadFacing(a0, a1, b1, b0, outward * d2.y - Vector3.up * d2.x);
            }
            // End caps (convex profiles: a fan from the wall-bottom point).
            for (var k = 1; k < profile.Length - 1; k++)
            {
                Tri(start, profile[0], profile[k], profile[k + 1], outward, -along);
                Tri(end, profile[0], profile[k], profile[k + 1], outward, along);
            }
        }

        void Tri(Vector3 at, Vector2 a, Vector2 b, Vector2 c, Vector3 outward, Vector3 facing)
        {
            var pa = at + outward * a.x + Vector3.up * a.y;
            var pb = at + outward * b.x + Vector3.up * b.y;
            var pc = at + outward * c.x + Vector3.up * c.y;
            var normal = Vector3.Cross(pb - pa, pc - pa);
            if (Vector3.Dot(normal, facing) < 0f) { var tmp = pb; pb = pc; pc = tmp; }
            var i = v.Count;
            foreach (var p in new[] { pa, pb, pc })
            {
                v.Add(p);
                n.Add(facing.normalized);
                uv.Add(Planar(p, facing));
            }
            t.Add(i); t.Add(i + 1); t.Add(i + 2);
        }

        // A quad wound to face <paramref name="want"/>, whatever the run's direction.
        void QuadFacing(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 want)
        {
            var normal = Vector3.Cross(b - a, c - a);
            if (Vector3.Dot(normal, want) < 0f) { Quad(d, c, b, a); return; }
            Quad(a, b, c, d);
        }

        public Mesh Build(string name)
        {
            var m = new Mesh { name = name };
            if (v.Count > 65000) m.indexFormat = IndexFormat.UInt32;
            m.SetVertices(v);
            m.SetNormals(n);
            m.SetUVs(0, uv);
            m.SetTriangles(t, 0);
            m.RecalculateBounds();
            m.RecalculateTangents();
            return m;
        }
    }

    // ------------------------------------------------------------------ buckets
    sealed class Bucket
    {
        public string material;
        public MaskClass mask;
        public bool shadows;
        public readonly Builder b = new Builder();
    }

    static Bucket Get(Dictionary<string, Bucket> buckets, string material, MaskClass mask, bool shadows)
    {
        var key = material + "/" + mask;
        if (!buckets.TryGetValue(key, out var bk)) buckets[key] = bk = new Bucket { material = material, mask = mask, shadows = shadows };
        return bk;
    }

    static readonly Vector2[] RailProfile =
    {
        new Vector2(0f, RailBottom), new Vector2(RailProud - RailBevel, RailBottom), new Vector2(RailProud, RailBottom + RailBevel),
        new Vector2(RailProud, RailTop - RailBevel), new Vector2(RailProud - RailBevel, RailTop), new Vector2(0f, RailTop),
    };
    static readonly Vector2[] BaseProfile =
    {
        new Vector2(0f, 0f), new Vector2(BaseProud, 0f), new Vector2(BaseProud, BaseHeight - .003f), new Vector2(BaseProud - .003f, BaseHeight), new Vector2(0f, BaseHeight),
    };

    // One wall run: the wall face line from (x0, z0) to (x1, z1), the room on the side of `outward`.
    struct WallRun
    {
        public Vector3 a, b, outward;
        public WallRun(float x0, float z0, float x1, float z1, Vector3 outward) { a = new Vector3(x0, 0f, z0); b = new Vector3(x1, 0f, z1); this.outward = outward; }
    }

    /// <summary>
    /// The room-side runs for one phase: <paramref name="southFace"/> is the south liner's face (start-area phase) or the
    /// data wall's face (after ClearStartArea). Inside corners: one run goes to the corner, the other stops at its front.
    /// </summary>
    static List<WallRun> Runs(Room r, float southFace, float proud)
    {
        var h = ModuleUnits.WallHalf;
        float w = r.west + h, e = r.east - h, n = r.north - h, s = southFace;
        var runs = new List<WallRun>
        {
            // South (liner or data wall), two spans either side of D, 0.05 m short of the opening.
            new WallRun(w, s, r.doorX0 - DoorStop, s, Vector3.forward),
            new WallRun(r.doorX1 + DoorStop, s, e, s, Vector3.forward),
            // West: from the south run's front to E2's casing, from E2's casing to the north wall.
            new WallRun(w, s + proud, w, r.e2z0 - CasingFace, Vector3.right),
            new WallRun(w, r.e2z1 + CasingFace, w, n, Vector3.right),
            // North: from the west run's front to E1's casing, from E1's casing to the riser's west run front.
            new WallRun(w + proud, n, r.e1x0 - CasingFace, n, Vector3.back),
            new WallRun(r.e1x1 + CasingFace, n, r.riserX - proud, n, Vector3.back),
            // Riser west face: from just past its outside corner (covering the corner) up to the north wall.
            new WallRun(r.riserX, r.riserZ - proud, r.riserX, n, Vector3.left),
            // Riser south face: from the outside corner to the east wall.
            new WallRun(r.riserX, r.riserZ, e, r.riserZ, Vector3.back),
            // East: from the south run's front to the riser south run's front.
            new WallRun(e, s + proud, e, r.riserZ - proud, Vector3.left),
        };
        return runs;
    }

    static void AddRun(Builder b, WallRun run, Vector2[] profile)
    {
        var d = run.b - run.a;
        var len = d.magnitude;
        if (len < .01f) return;
        b.Run(run.a, d / len, run.outward, len, profile);
    }

    // The 0.305 m border course as non-overlapping floor rectangles (x0, z0, x1, z1), round the riser, stopping at D.
    static List<Vector4> Border(Room r, float southFace)
    {
        var h = ModuleUnits.WallHalf;
        float a = BorderWidth, w = r.west + h, e = r.east - h, n = r.north - h, s = southFace, rx = r.riserX, rz = r.riserZ;
        return new List<Vector4>
        {
            new Vector4(w, s, r.doorX0, s + a),
            new Vector4(r.doorX1, s, e, s + a),
            new Vector4(w, s + a, w + a, n),
            new Vector4(w + a, n - a, rx, n),
            new Vector4(e - a, s + a, e, rz),
            new Vector4(rx - a, rz - a, e - a, rz),
            new Vector4(rx - a, rz, rx, n - a),
        };
    }

    static void Floor(Builder b, Vector4 rect, float y)
    {
        if (rect.z - rect.x < .001f || rect.w - rect.y < .001f) return;
        b.Quad(new Vector3(rect.x, y, rect.y), new Vector3(rect.x, y, rect.w), new Vector3(rect.z, y, rect.w), new Vector3(rect.z, y, rect.y));
    }

    /// <summary>The room-side trim of one phase (crash rail, base, border course) under <paramref name="parent"/>.</summary>
    public static void BuildRoomSide(Transform parent, Room r, float southFace, List<Part> parts, List<Mesh> meshes)
    {
        var buckets = new Dictionary<string, Bucket>();
        var rail = Get(buckets, "Station_CrashRail", MaskClass.StationOnly, true);
        foreach (var run in Runs(r, southFace, RailProud)) AddRun(rail.b, run, RailProfile);
        var bse = Get(buckets, "Station_Base", MaskClass.StationOnly, false);
        foreach (var run in Runs(r, southFace, BaseProud)) AddRun(bse.b, run, BaseProfile);
        var border = Get(buckets, "Station_FloorBorder", MaskClass.StationOnly, false);
        foreach (var rect in Border(r, southFace)) Floor(border.b, rect, BorderLift);
        Emit(parent, "room side", buckets, parts, meshes);
    }

    /// <summary>The E1 / E2 trim (frames, liners, guards, throat floors, reducers, ceiling bands), both masks.</summary>
    public static void BuildOpenings(Transform parent, Room r, List<Part> parts, List<Mesh> meshes)
    {
        var buckets = new Dictionary<string, Bucket>();
        var frame = Get(buckets, "Prop_SteelBrown", MaskClass.BothLayers, true);
        var guard = Get(buckets, "Station_StainlessGuard", MaskClass.BothLayers, false);
        var throat = Get(buckets, "Station_Floor", MaskClass.BothLayers, false);
        var reducer = Get(buckets, "Station_Base", MaskClass.BothLayers, false);
        var band = Get(buckets, "Station_CeilingBand", MaskClass.BothLayers, false);
        var h = ModuleUnits.WallHalf;
        var top = ModuleUnits.ArchTop;
        var c = CasingFace; var p = CasingProud; var l = ThroatLiner;

        // E1 in the north wall: the station face at north - h (room toward -z), the outer face at north + h.
        float zi = r.north - h, zo = r.north + h, x0 = r.e1x0, x1 = r.e1x1;
        foreach (var face in new[] { -1, 1 })
        {
            float za = face < 0 ? zi - p : zo, zb = face < 0 ? zi : zo + p;
            frame.b.Box(new Vector3(x0 - c, 0f, za), new Vector3(x0, top + c, zb));
            frame.b.Box(new Vector3(x1, 0f, za), new Vector3(x1 + c, top + c, zb));
            frame.b.Box(new Vector3(x0 - c, top, za), new Vector3(x1 + c, top + c, zb));
        }
        frame.b.Box(new Vector3(x0, 0f, zi), new Vector3(x0 + l, top, zo));
        frame.b.Box(new Vector3(x1 - l, 0f, zi), new Vector3(x1, top, zo));
        frame.b.Box(new Vector3(x0, top - l, zi), new Vector3(x1, top, zo));
        // Stainless guards over the casing's arris on the station face: a wing on the casing face, a wing on the liner.
        guard.b.Box(new Vector3(x0 - GuardWing, GuardBottom, zi - p - GuardThick), new Vector3(x0 + l, GuardTop, zi - p));
        guard.b.Box(new Vector3(x0 + l, GuardBottom, zi - p), new Vector3(x0 + l + GuardThick, GuardTop, zi - p + GuardWing));
        guard.b.Box(new Vector3(x1 - l, GuardBottom, zi - p - GuardThick), new Vector3(x1 + GuardWing, GuardTop, zi - p));
        guard.b.Box(new Vector3(x1 - l - GuardThick, GuardBottom, zi - p), new Vector3(x1 - l, GuardTop, zi - p + GuardWing));
        Floor(throat.b, new Vector4(x0 + l, zi - .002f, x1 - l, zo + .002f), BorderLift);
        reducer.b.Box(new Vector3(x0 + l, 0f, zo), new Vector3(x1 - l, ReducerHeight, zo + ReducerWidth));
        var cellX0 = Mathf.Floor((x0 + x1) * .5f / MapGrid.CellSize) * MapGrid.CellSize;
        band.b.Box(new Vector3(cellX0, r.ceiling - BandDepth, r.north - BandWidth * .5f), new Vector3(cellX0 + MapGrid.CellSize, r.ceiling - .001f, r.north + BandWidth * .5f));

        // E2 in the west wall: the station face at west + h (room toward +x), the outer face at west - h.
        float xi = r.west + h, xo = r.west - h, z0 = r.e2z0, z1 = r.e2z1;
        foreach (var face in new[] { -1, 1 })
        {
            float xa = face < 0 ? xi : xo - p, xb = face < 0 ? xi + p : xo;
            frame.b.Box(new Vector3(xa, 0f, z0 - c), new Vector3(xb, top + c, z0));
            frame.b.Box(new Vector3(xa, 0f, z1), new Vector3(xb, top + c, z1 + c));
            frame.b.Box(new Vector3(xa, top, z0 - c), new Vector3(xb, top + c, z1 + c));
        }
        frame.b.Box(new Vector3(xo, 0f, z0), new Vector3(xi, top, z0 + l));
        frame.b.Box(new Vector3(xo, 0f, z1 - l), new Vector3(xi, top, z1));
        frame.b.Box(new Vector3(xo, top - l, z0), new Vector3(xi, top, z1));
        guard.b.Box(new Vector3(xi + p, GuardBottom, z0 - GuardWing), new Vector3(xi + p + GuardThick, GuardTop, z0 + l));
        guard.b.Box(new Vector3(xi + p - GuardWing, GuardBottom, z0 + l), new Vector3(xi + p, GuardTop, z0 + l + GuardThick));
        guard.b.Box(new Vector3(xi + p, GuardBottom, z1 - l), new Vector3(xi + p + GuardThick, GuardTop, z1 + GuardWing));
        guard.b.Box(new Vector3(xi + p - GuardWing, GuardBottom, z1 - l - GuardThick), new Vector3(xi + p, GuardTop, z1 - l));
        Floor(throat.b, new Vector4(xo - .002f, z0 + l, xi + .002f, z1 - l), BorderLift);
        reducer.b.Box(new Vector3(xo - ReducerWidth, 0f, z0 + l), new Vector3(xo, ReducerHeight, z1 - l));
        var cellZ0 = Mathf.Floor((z0 + z1) * .5f / MapGrid.CellSize) * MapGrid.CellSize;
        band.b.Box(new Vector3(r.west - BandWidth * .5f, r.ceiling - BandDepth, cellZ0), new Vector3(r.west + BandWidth * .5f, r.ceiling - .001f, cellZ0 + MapGrid.CellSize));
        Emit(parent, "openings", buckets, parts, meshes);
    }

    static void Emit(Transform parent, string group, Dictionary<string, Bucket> buckets, List<Part> parts, List<Mesh> meshes)
    {
        foreach (var bk in buckets.Values)
        {
            if (bk.b.Triangles == 0) continue;
            var go = new GameObject("Trim / " + group + " / " + bk.material + (bk.mask == MaskClass.BothLayers ? " (both layers)" : ""));
            go.transform.SetParent(parent, false);
            var mesh = bk.b.Build(go.name);
            meshes.Add(mesh);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = FrontRoomsSurfaces.TryGet(bk.material) ?? FrontRoomsSurfaces.Get(bk.material);
            mr.shadowCastingMode = bk.shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            mr.receiveShadows = true;
            parts.Add(new Part { renderer = mr, mask = bk.mask });
        }
    }
}
