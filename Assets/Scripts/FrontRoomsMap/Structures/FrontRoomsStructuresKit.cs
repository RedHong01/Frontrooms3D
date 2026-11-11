using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// The structures scene's module kit (scene 4, RULES 5b; research/level_space/40 section 11 with 11.9; structures PLAN 2.8).
/// Units: cell 3.0, half-grid 1.5 (cores and landings only), fine 0.6, tile course 0.15, level step 0.30, storey 3.9
/// (Level 0, Office, Run!) or 4.5 (pool). Pieces at 40 section 11.4 / 11.6's sizes: walls 0.16 on cell lines, oriented
/// prisms with metre UVs (u along the piece from world 0, v = world y, so tile joints run on round corners and arches),
/// walls with rectangular and round openings, rings with holes (a full circle holds whole 0.15 m tiles), sectors, swept
/// profiles (the flume), straight flights with a dark nosing and one ramp collider, step runs, ramps, switchback cores
/// (B3), spiral stairs (B4-S, B4-M, B4-L, the tower), guards (half-wall, tiled, glass on a steel band, rail, mesh),
/// water and basins, lamps that join the scene's light budget, Relay-only blockers (C) and metric marks (M).
/// A district is data: the Plate below takes cells with a kind, a floor and a ceiling (metres, whole 0.30 steps where the
/// level is a terrain level) and a room, and resolves every wall, ledge, guard, void edge and soffit from them (40 11.4's
/// table); a district only adds its set pieces. Everything merges into few renderers per material and one collider per
/// part (FrontRoomsStructuresMesh). Greybox parts a production asset will replace are named "STAND-IN ...".
/// Runtime only; nothing here reads or writes an asset.
/// </summary>
public sealed class FrontRoomsStructuresKit
{
    public const float Cell = 3f, Half = 1.5f, Fine = .6f, Course = .15f, Step = .3f;
    public const float PoolStorey = 4.5f, OfficeStorey = 3.9f;
    public const float Wall = .16f, GuardHeight = 1.07f, GuardDrop = .76f, SlabThick = .2f, LidSlab = .16f;
    public const float NosingTread = .05f, NosingRiser = .025f, Headroom = 2.1f;

    public enum GuardStyle { None, HalfWall, TileWall, Glass, Rail, Mesh }

    /// <summary>An opening in a wall run: from and to are metres along the run from its start; sill and top are world-local y
    /// (top is the flat head, or the springing when round: a semicircle of radius (to - from) / 2 sits on it).</summary>
    public struct Opening
    {
        public float from, to, sill, top;
        public bool round;

        public Opening(float from, float to, float sill, float top, bool round = false)
        {
            this.from = from;
            this.to = to;
            this.sill = sill;
            this.top = top;
            this.round = round;
        }
    }

    /// <summary>A hole in a ring: angles in degrees (from +x toward +z), y from y0 to y1 (round: a head of the hole's width on y1).</summary>
    public struct RingHole
    {
        public float a0, a1, y0, y1;
        public bool round;

        public RingHole(float a0, float a1, float y0, float y1, bool round = false)
        {
            this.a0 = a0;
            this.a1 = a1;
            this.y0 = y0;
            this.y1 = y1;
            this.round = round;
        }
    }

    public readonly FrontRoomsStructures Host;
    public readonly FrontRoomsStructuresDistrict District;
    public readonly Transform Root;
    readonly Vector3 world;
    string partName;
    Transform partRoot;
    readonly List<GameObject> blockers = new List<GameObject>();
    readonly List<GameObject> marks = new List<GameObject>();
    readonly List<Light> lights = new List<Light>();

    /// <summary>The mesh the pieces go into now (a district's main shell, or a part's).</summary>
    public FrontRoomsStructuresMesh Mesh { get; private set; }
    public IReadOnlyList<Light> Lights => lights;
    public Transform PartRoot => partRoot != null ? partRoot : Root;

    public FrontRoomsStructuresKit(FrontRoomsStructures host, FrontRoomsStructuresDistrict district)
    {
        Host = host;
        District = district;
        Root = district.Root;
        world = Root != null ? Root.position : Vector3.zero;
        isNew = FrontRoomsStructuresAssets.NewFor(district.Key);
        Mesh = new FrontRoomsStructuresMesh(Root) { Bevels = isNew };
        partName = district.Code;
    }

    /// <summary>Finish what is pending and start a part under its own child (switched on and off as a whole: the flume's water, the night lines).</summary>
    public Transform BeginPart(string name)
    {
        Finish();
        var go = new GameObject(name);
        go.transform.SetParent(Root, false);
        partRoot = go.transform;
        Mesh = new FrontRoomsStructuresMesh(partRoot) { Ceiling = 2.9f, Bevels = isNew, Zones = halls };
        partName = name;
        return partRoot;
    }

    /// <summary>Finish the part and go back to the district's own shell.</summary>
    public void EndPart()
    {
        Finish();
        partRoot = null;
        Mesh = new FrontRoomsStructuresMesh(Root) { Bevels = isNew, Zones = halls };
        partName = District.Code;
    }

    /// <summary>Make the renderers and the collider of what is pending (call once at the end of a district's build).</summary>
    public void Finish()
    {
        // K2 (structures G2-c): the pool edges and tile caps of this part, chained corner to corner, before its renderers are made
        if (isNew) FlushRuns();
        var ceiling = Mesh.Ceiling;
        var grime = Mesh.FloorGrimeOff;
        var shadows = Mesh.CastShadows;
        Mesh.Finish(partName, District.Meshes);
        Mesh = new FrontRoomsStructuresMesh(partRoot != null ? partRoot : Root) { Ceiling = ceiling, FloorGrimeOff = grime, CastShadows = shadows, Bevels = isNew, Zones = halls };
    }

    /// <summary>
    /// K2 (structures G2-a; the K1 review's FR-1), NEW only: the district's halls (district-local boxes): from here on the shell's
    /// renderers are split by hall (FrontRoomsStructuresMesh.Zones; the bevels still solve over the whole district), so each
    /// hall's metals, glass and trims take its own reflection probe. TODAY ignores it (one group per material, bit for bit).
    /// </summary>
    public void Halls(params Bounds[] boxes)
    {
        if (!isNew || boxes == null || boxes.Length == 0) return;
        halls = new List<Bounds>(boxes);
        Mesh.Zones = halls;
    }

    List<Bounds> halls;

    /// <summary>K2: a hall's box for Halls, from its footprint (district metres) and the heights its pieces span.</summary>
    public static Bounds Hall(float x0, float z0, float x1, float z1, float y0, float y1) =>
        new Bounds(new Vector3((x0 + x1) * .5f, (y0 + y1) * .5f, (z0 + z1) * .5f), new Vector3(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0), Mathf.Abs(z1 - z0)));

    /// <summary>Run one piece of a build; a failure logs once and the rest of the district goes on.</summary>
    public bool Safe(string what, Action build)
    {
        try
        {
            build();
            return true;
        }
        catch (Exception e)
        {
            FrontRoomsPoolMaterials.LogOnce(District.Code + " " + what, District.Code + " " + what + " was left out: " + e);
            return false;
        }
    }

    /// <summary>Shell properties for what follows: the ceiling in district metres (world y, every district's +-0 is world 0)
    /// and whether the floor under it is off world y 0 (its walls' floor grime is turned off).</summary>
    public void Shell(float ceiling, float floor)
    {
        Mesh.Ceiling = ceiling;
        Mesh.FloorGrimeOff = Mathf.Abs(floor) > .01f;
    }

    Vector3 W(Vector3 local) => local + world;

    // ---------------------------------------------------------------- quads and boxes

    /// <summary>A quad facing <paramref name="front"/> (either winding; it is turned to face it), UVs as given.</summary>
    public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 front, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud, Material m, bool solid = false)
    {
        if (m == null && !solid) return;
        if (Vector3.Dot(Vector3.Cross(b - a, d - a), front) < 0f) Mesh.QuadUV(a, d, c, b, ua, ud, uc, ub, m, solid);
        else Mesh.QuadUV(a, b, c, d, ua, ub, uc, ud, m, solid);
    }

    /// <summary>A quad on a plane with world-planar metre UVs (as the mesh's boxes).</summary>
    public void QuadPlanar(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 front, Material m, bool solid = false)
    {
        Quad(a, b, c, d, front, Planar(a, front), Planar(b, front), Planar(c, front), Planar(d, front), m, solid);
    }

    Vector2 Planar(Vector3 local, Vector3 n)
    {
        var w = W(local);
        float ax = Mathf.Abs(n.x), ay = Mathf.Abs(n.y), az = Mathf.Abs(n.z);
        if (ax >= ay && ax >= az) return new Vector2(w.z, w.y);
        if (ay >= ax && ay >= az) return new Vector2(w.x, w.z);
        return new Vector2(w.x, w.y);
    }

    /// <summary>An axis-aligned box from its min and max corners (district metres).</summary>
    public void Box(Vector3 min, Vector3 max, Material m, bool solid = true)
    {
        var lo = Vector3.Min(min, max);
        var hi = Vector3.Max(min, max);
        Mesh.Span(lo, hi, m, solid);
    }

    public void Box(float x0, float y0, float z0, float x1, float y1, float z1, Material m, bool solid = true) =>
        Box(new Vector3(x0, y0, z0), new Vector3(x1, y1, z1), m, solid);

    /// <summary>A floor slab: top at <paramref name="top"/>, <paramref name="thick"/> under it.</summary>
    public void Slab(float x0, float z0, float x1, float z1, float top, Material m, float thick = SlabThick, bool solid = true) =>
        Box(new Vector3(Mathf.Min(x0, x1), top - thick, Mathf.Min(z0, z1)), new Vector3(Mathf.Max(x0, x1), top, Mathf.Max(z0, z1)), m, solid);

    /// <summary>A lid: its soffit at <paramref name="bottom"/>.</summary>
    public void Lid(float x0, float z0, float x1, float z1, float bottom, Material m, float thick = LidSlab, bool solid = true) =>
        Box(new Vector3(Mathf.Min(x0, x1), bottom, Mathf.Min(z0, z1)), new Vector3(Mathf.Max(x0, x1), bottom + thick, Mathf.Max(z0, z1)), m, solid);

    // ---------------------------------------------------------------- oriented prisms and walls

    /// <summary>
    /// A vertical prism from plan point a to b (y ignored), bottom y0, top y1, thickness centred on the line. UVs: u along
    /// the line (from world 0 projected on it), v = world y; top and bottom planar. Solid adds an oriented box collider.
    /// </summary>
    public void Prism(Vector3 a, Vector3 b, float y0, float y1, float thick, Material m, bool solid = true)
    {
        a.y = 0f;
        b.y = 0f;
        var d = b - a;
        var len = d.magnitude;
        if (len < 1e-4f || y1 - y0 < 1e-4f || thick < 1e-4f) return;
        var t = d / len;
        var s = new Vector3(t.z, 0f, -t.x);
        var h = s * (thick * .5f);
        var u0 = Vector3.Dot(W(a), t);
        var up = Vector3.up;
        if (isNew)
        {
            // NEW: one recorded box, bevelled at Finish where nothing touches it (its faces keep these UVs); the collider as before
            if (m != null) Mesh.PrismBox((a + b) * .5f + up * ((y0 + y1) * .5f), new Vector3(thick, y1 - y0, len), Quaternion.LookRotation(t, up), m, WallLot(t, Vector3.Dot(W(a), s)), y0);
            if (solid) Mesh.Collider((a + b) * .5f + up * ((y0 + y1) * .5f), new Vector3(thick, y1 - y0, len), Quaternion.LookRotation(t, up));
            return;
        }
        if (m != null)
        {
            // Two long faces.
            Quad(a + h + up * y0, b + h + up * y0, b + h + up * y1, a + h + up * y1, s,
                new Vector2(u0, y0), new Vector2(u0 + len, y0), new Vector2(u0 + len, y1), new Vector2(u0, y1), m);
            Quad(a - h + up * y0, b - h + up * y0, b - h + up * y1, a - h + up * y1, -s,
                new Vector2(u0, y0), new Vector2(u0 + len, y0), new Vector2(u0 + len, y1), new Vector2(u0, y1), m);
            // Ends.
            var v0 = Vector3.Dot(W(a), s);
            Quad(a + h + up * y0, a - h + up * y0, a - h + up * y1, a + h + up * y1, -t,
                new Vector2(v0 + thick * .5f, y0), new Vector2(v0 - thick * .5f, y0), new Vector2(v0 - thick * .5f, y1), new Vector2(v0 + thick * .5f, y1), m);
            Quad(b + h + up * y0, b - h + up * y0, b - h + up * y1, b + h + up * y1, t,
                new Vector2(v0 + thick * .5f, y0), new Vector2(v0 - thick * .5f, y0), new Vector2(v0 - thick * .5f, y1), new Vector2(v0 + thick * .5f, y1), m);
            // Top and bottom.
            QuadPlanar(a + h + up * y1, b + h + up * y1, b - h + up * y1, a - h + up * y1, up, m);
            QuadPlanar(a + h + up * y0, b + h + up * y0, b - h + up * y0, a - h + up * y0, -up, m);
        }
        if (solid) Mesh.Collider((a + b) * .5f + up * ((y0 + y1) * .5f), new Vector3(thick, y1 - y0, len), Quaternion.LookRotation(t, up));
    }

    /// <summary>
    /// A wall run from a to b (plan), y0 to y1, thickness centred, with openings (metres along the run). Rectangular openings
    /// get a sill and a flat head; round ones a semicircular head with its intrados (UVs in metres round the arc).
    /// </summary>
    public void WallRun(Vector3 a, Vector3 b, float y0, float y1, float thick, IList<Opening> openings, Material m, bool solid = true)
    {
        a.y = 0f;
        b.y = 0f;
        var len = Vector3.Distance(a, b);
        if (len < 1e-4f) return;
        var t = (b - a) / len;
        var ops = new List<Opening>();
        if (openings != null)
            foreach (var o in openings)
            {
                var f = Mathf.Max(0f, Mathf.Min(o.from, o.to));
                var e = Mathf.Min(len, Mathf.Max(o.from, o.to));
                if (e - f > 1e-3f) ops.Add(new Opening(f, e, o.sill, o.top, o.round));
            }
        ops.Sort((p, q) => p.from.CompareTo(q.from));
        // Per-height cut: the run is split at every opening edge; each piece keeps the y-intervals no opening covers
        // (stacked doors on one wall all open; an opening never hides a later one). A round opening cuts from its sill
        // to the next opening's sill above it (or the wall top); its spandrel is the round head's, with a collider.
        var headTop = new float[ops.Count];
        for (var k = 0; k < ops.Count; k++)
        {
            var o = ops[k];
            var ht = y1;
            if (o.round)
                for (var q = 0; q < ops.Count; q++)
                {
                    if (q == k) continue;
                    var p2 = ops[q];
                    if (p2.to <= o.from + 1e-3f || p2.from >= o.to - 1e-3f) continue;
                    if (p2.sill > o.top - 1e-3f && p2.sill < ht) ht = Mathf.Max(o.top, p2.sill);
                }
            headTop[k] = ht;
        }
        var cuts = new List<float>();
        cuts.Add(0f);
        cuts.Add(len);
        foreach (var o in ops) { cuts.Add(o.from); cuts.Add(o.to); }
        cuts.Sort();
        var runFrom = -1f;
        var runTo = 0f;
        var ivs = new List<Vector2>();
        for (var c = 0; c + 1 < cuts.Count; c++)
        {
            var xa = cuts[c];
            var xb = cuts[c + 1];
            if (xb - xa < 1e-4f) continue;
            var mid = (xa + xb) * .5f;
            ivs.Clear();
            for (var k = 0; k < ops.Count; k++)
            {
                var o = ops[k];
                if (mid < o.from || mid > o.to) continue;
                var lo = Mathf.Max(y0, o.sill);
                var hi = Mathf.Min(y1, o.round ? headTop[k] : o.top);
                if (hi > lo + 1e-4f) ivs.Add(new Vector2(lo, hi));
            }
            if (ivs.Count == 0)
            {
                if (runFrom < 0f) runFrom = xa;
                runTo = xb;
                continue;
            }
            if (runFrom >= 0f) { RunPiece(a, t, runFrom, runTo, y0, y1, thick, m, solid, ops, headTop); runFrom = -1f; }
            ivs.Sort((p, q) => p.x.CompareTo(q.x));
            var at = y0;
            foreach (var iv in ivs)
            {
                if (iv.x > at + 1e-3f) Prism(a + t * xa, a + t * xb, at, iv.x, thick, m, solid);
                if (iv.y > at) at = iv.y;
            }
            if (at < y1 - 1e-3f) Prism(a + t * xa, a + t * xb, at, y1, thick, m, solid);
        }
        if (runFrom >= 0f) RunPiece(a, t, runFrom, runTo, y0, y1, thick, m, solid, ops, headTop);
        for (var k = 0; k < ops.Count; k++)
            if (ops[k].round) RoundHead(a, t, thick, ops[k].from, ops[k].to, ops[k].top, headTop[k], m, solid);
        // NEW: a round head's spandrel is quads: it counts as touching, so the pier's edge beside it stays square
        if (isNew)
            for (var k = 0; k < ops.Count; k++)
            {
                if (!ops[k].round || headTop[k] <= ops[k].top + 1e-3f) continue;
                var mid = a + t * ((ops[k].from + ops[k].to) * .5f);
                Mesh.Occluder(mid + Vector3.up * ((ops[k].top + headTop[k]) * .5f), new Vector3(thick, headTop[k] - ops[k].top, ops[k].to - ops[k].from), Quaternion.LookRotation(t, Vector3.up));
            }
    }

    // A full-height run of a wall (no opening over it). TODAY: one prism. NEW: the same, split at the sill and head heights of the
    // openings it borders, so each jamb is one whole exposed edge for the bevels (a lintel or a sill against the rest keeps the
    // joint flush); one collider for the whole run, as before.
    void RunPiece(Vector3 a, Vector3 t, float from, float to, float y0, float y1, float thick, Material m, bool solid, List<Opening> ops, float[] headTop)
    {
        if (!isNew) { Prism(a + t * from, a + t * to, y0, y1, thick, m, solid); return; }
        var cuts = new List<float> { y0, y1 };
        for (var k = 0; k < ops.Count; k++)
        {
            var o = ops[k];
            var touches = Mathf.Abs(o.from - to) < 1e-3f || Mathf.Abs(o.to - from) < 1e-3f;
            if (!touches) continue;
            if (o.sill > y0 + 1e-3f && o.sill < y1 - 1e-3f) cuts.Add(o.sill);
            var head = o.round ? o.top : o.top;
            if (head > y0 + 1e-3f && head < y1 - 1e-3f) cuts.Add(head);
        }
        cuts.Sort();
        for (var i = 0; i + 1 < cuts.Count; i++)
            if (cuts[i + 1] - cuts[i] > 1e-3f) Prism(a + t * from, a + t * to, cuts[i], cuts[i + 1], thick, m, false);
        if (solid)
        {
            var pa = a + t * from;
            var pb = a + t * to;
            pa.y = pb.y = 0f;
            var len = Vector3.Distance(pa, pb);
            if (len >= 1e-4f && y1 - y0 >= 1e-4f && thick >= 1e-4f)
                Mesh.Collider((pa + pb) * .5f + Vector3.up * ((y0 + y1) * .5f), new Vector3(thick, y1 - y0, len), Quaternion.LookRotation((pb - pa) / len, Vector3.up));
        }
    }

    void RoundHead(Vector3 a, Vector3 t, float thick, float from, float to, float spring, float top, Material m, bool solid)
    {
        if (spring >= top) return;
        // K2 (structures G2-c, C2), NEW only: the intrados in courses by its radius with the bullnose on both arrises, the spandrels
        // stopping at the round (FrontRoomsStructureCurveTile.Arch); the colliders below are today's (m = null)
        if (isNew && m != null && CurveTile(m, out var own, out var ownPitch))
        {
            var o = new FrontRoomsStructureCurveTile.Out();
            FrontRoomsStructureCurveTile.Arch(o, a, t, thick, from, to, spring, top, Vector3.Dot(W(new Vector3(a.x, 0f, a.z)), t));
            AppendCurve(o, m);
            m = null;
        }
        var r = (to - from) * .5f;
        var mid = from + r;
        var s = new Vector3(t.z, 0f, -t.x);
        var h = thick * .5f;
        var up = Vector3.up;
        var n = Mathf.Clamp(Mathf.CeilToInt(Mathf.PI * r / .08f), 8, 64);
        var u0 = Vector3.Dot(W(a), t);
        var v0 = Vector3.Dot(W(a), s);
        var arc = 0f;
        var centre = a + t * mid + up * spring;
        for (var k = 0; k < n; k++)
        {
            var fa = from + (to - from) * k / n;
            var fb = from + (to - from) * (k + 1) / n;
            var ha = Mathf.Min(top, spring + Mathf.Sqrt(Mathf.Max(0f, r * r - (fa - mid) * (fa - mid))));
            var hb = Mathf.Min(top, spring + Mathf.Sqrt(Mathf.Max(0f, r * r - (fb - mid) * (fb - mid))));
            var seg = Mathf.Sqrt((fb - fa) * (fb - fa) + (hb - ha) * (hb - ha));
            if (ha >= top - 1e-4f && hb >= top - 1e-4f) { arc += seg; continue; }
            var pa = a + t * fa;
            var pb = a + t * fb;
            if (solid)
            {
                var low = Mathf.Min(top, Mathf.Max(ha, hb));
                if (top - low > 1e-3f)
                    Mesh.Collider((pa + pb) * .5f + up * ((low + top) * .5f), new Vector3(thick, top - low, (fb - fa)), Quaternion.LookRotation(t, up));
            }
            if (m == null) { arc += seg; continue; }
            for (var side = -1; side <= 1; side += 2)
            {
                var off = s * (h * side);
                Quad(pa + off + up * ha, pb + off + up * hb, pb + off + up * top, pa + off + up * top, s * side,
                    new Vector2(u0 + fa, ha), new Vector2(u0 + fb, hb), new Vector2(u0 + fb, top), new Vector2(u0 + fa, top), m);
            }
            var cm = (pa + pb) * .5f + up * ((ha + hb) * .5f);
            var inward = centre - cm;
            if (inward.sqrMagnitude < 1e-8f) inward = -up;
            Quad(pa + s * h + up * ha, pb + s * h + up * hb, pb - s * h + up * hb, pa - s * h + up * ha, inward.normalized,
                new Vector2(arc, v0 + h), new Vector2(arc + seg, v0 + h), new Vector2(arc + seg, v0 - h), new Vector2(arc, v0 - h), m);
            arc += seg;
        }
    }

    /// <summary>A wall in the plane z = <paramref name="z"/> from x0 to x1; opening from / to are absolute x here.</summary>
    public void WallX(float z, float x0, float x1, float y0, float y1, Material m, IList<Opening> openings = null, float thick = Wall, bool solid = true)
    {
        var lo = Mathf.Min(x0, x1);
        var hi = Mathf.Max(x0, x1);
        WallRun(new Vector3(lo, 0f, z), new Vector3(hi, 0f, z), y0, y1, thick, Shift(openings, lo), m, solid);
    }

    /// <summary>A wall in the plane x = <paramref name="x"/> from z0 to z1; opening from / to are absolute z here.</summary>
    public void WallZ(float x, float z0, float z1, float y0, float y1, Material m, IList<Opening> openings = null, float thick = Wall, bool solid = true)
    {
        var lo = Mathf.Min(z0, z1);
        var hi = Mathf.Max(z0, z1);
        WallRun(new Vector3(x, 0f, lo), new Vector3(x, 0f, hi), y0, y1, thick, Shift(openings, lo), m, solid);
    }

    static List<Opening> Shift(IList<Opening> openings, float by)
    {
        if (openings == null) return null;
        var list = new List<Opening>(openings.Count);
        foreach (var o in openings) list.Add(new Opening(o.from - by, o.to - by, o.sill, o.top, o.round));
        return list;
    }

    // ---------------------------------------------------------------- rings, sectors, round slabs

    static Vector3 Polar(Vector3 c, float r, float deg)
    {
        var a = deg * Mathf.Deg2Rad;
        return new Vector3(c.x + r * Mathf.Cos(a), 0f, c.z + r * Mathf.Sin(a));
    }

    /// <summary>
    /// A curved wall (annular prism) round c from angle a0 to a1 (degrees from +x toward +z), radii rIn..rOut, y0..y1, with
    /// holes. UVs scaled so a full circle holds whole 0.15 m tiles on each face. Solid adds a box per segment and interval.
    /// </summary>
    public void Ring(Vector3 c, float rIn, float rOut, float y0, float y1, float a0, float a1, Material m, bool solid = true, IList<RingHole> holes = null, float segDeg = 7.5f,
        bool capRound = false)
    {
        if (a1 < a0) { var sw = a0; a0 = a1; a1 = sw; }
        // K2 (structures G2-c, C1; Red's ask 2 A), NEW only: tiled rings as a setter lays them (FrontRoomsStructureCurveTile.Ring:
        // one flat facet a tile column in the tile its radius takes, holes on the true circle with bullnose reveals, solid caps
        // with their normal, capRound: a free top's bullnose); the colliders below are today's, call for call (m = null)
        if (isNew && m != null && CurveTile(m, out var own, out var ownPitch))
        {
            var o = new FrontRoomsStructureCurveTile.Out();
            List<FrontRoomsStructureCurveTile.Hole> hs = null;
            if (holes != null)
            {
                hs = new List<FrontRoomsStructureCurveTile.Hole>(holes.Count);
                foreach (var h in holes) hs.Add(new FrontRoomsStructureCurveTile.Hole(h.a0, h.a1, h.y0, h.y1, h.round));
            }
            FrontRoomsStructureCurveTile.Ring(o, c, rIn, rOut, y0, y1, a0, a1, hs, capRound, true, own, ownPitch);
            AppendCurve(o, m);
            m = null;
        }
        var n = Mathf.Max(1, Mathf.CeilToInt((a1 - a0) / segDeg));
        var kOut = TileFit(rOut);
        var kIn = TileFit(rIn);
        var up = Vector3.up;
        List<Vector2> previous = null;
        for (var k = 0; k <= n; k++)
        {
            List<Vector2> here = null;
            float ta = 0f, tb = 0f;
            if (k < n)
            {
                ta = a0 + (a1 - a0) * k / n;
                tb = a0 + (a1 - a0) * (k + 1) / n;
                here = Intervals(y0, y1, (ta + tb) * .5f, rOut, holes);
            }
            // The radial face between segment k-1 and k: where exactly one of them is solid.
            var edgeAngle = k < n ? ta : a1;
            var pIn = c + (Polar(Vector3.zero, rIn, edgeAngle));
            var pOut = c + (Polar(Vector3.zero, rOut, edgeAngle));
            pIn.y = pOut.y = 0f;
            var tangent = new Vector3(-Mathf.Sin(edgeAngle * Mathf.Deg2Rad), 0f, Mathf.Cos(edgeAngle * Mathf.Deg2Rad));
            if (m != null)
            {
                foreach (var iv in Minus(here, previous)) RadialFace(pIn, pOut, iv.x, iv.y, -tangent, m);
                foreach (var iv in Minus(previous, here)) RadialFace(pIn, pOut, iv.x, iv.y, tangent, m);
            }
            if (k == n) break;
            var oa = c + Polar(Vector3.zero, rOut, ta);
            var ob = c + Polar(Vector3.zero, rOut, tb);
            var ia = c + Polar(Vector3.zero, rIn, ta);
            var ib = c + Polar(Vector3.zero, rIn, tb);
            oa.y = ob.y = ia.y = ib.y = 0f;
            var mid = (ta + tb) * .5f * Mathf.Deg2Rad;
            var radial = new Vector3(Mathf.Cos(mid), 0f, Mathf.Sin(mid));
            float uoa = ta * Mathf.Deg2Rad * rOut * kOut, uob = tb * Mathf.Deg2Rad * rOut * kOut;
            float uia = ta * Mathf.Deg2Rad * rIn * kIn, uib = tb * Mathf.Deg2Rad * rIn * kIn;
            foreach (var iv in here)
            {
                if (m != null)
                {
                    Quad(oa + up * iv.x, ob + up * iv.x, ob + up * iv.y, oa + up * iv.y, radial,
                        new Vector2(uoa, iv.x), new Vector2(uob, iv.x), new Vector2(uob, iv.y), new Vector2(uoa, iv.y), m);
                    if (rIn > 1e-3f)
                        Quad(ia + up * iv.x, ib + up * iv.x, ib + up * iv.y, ia + up * iv.y, -radial,
                            new Vector2(uia, iv.x), new Vector2(uib, iv.x), new Vector2(uib, iv.y), new Vector2(uia, iv.y), m);
                    QuadPlanar(ia + up * iv.y, ib + up * iv.y, ob + up * iv.y, oa + up * iv.y, up, m);
                    QuadPlanar(ia + up * iv.x, ib + up * iv.x, ob + up * iv.x, oa + up * iv.x, -up, m);
                }
                if (solid)
                {
                    var rm = (rIn + rOut) * .5f;
                    var centre = c + Polar(Vector3.zero, rm, (ta + tb) * .5f);
                    centre.y = (iv.x + iv.y) * .5f;
                    var chord = 2f * rOut * Mathf.Sin((tb - ta) * .5f * Mathf.Deg2Rad) + .02f;
                    var tangentMid = new Vector3(-Mathf.Sin(mid), 0f, Mathf.Cos(mid));
                    Mesh.Collider(centre, new Vector3(rOut - rIn, iv.y - iv.x, chord), Quaternion.LookRotation(tangentMid, up));
                }
            }
            previous = here;
        }
    }

    void RadialFace(Vector3 pIn, Vector3 pOut, float y0, float y1, Vector3 front, Material m)
    {
        var up = Vector3.up;
        Quad(pIn + up * y0, pOut + up * y0, pOut + up * y1, pIn + up * y1, front,
            new Vector2(0f, y0), new Vector2(Vector3.Distance(pIn, pOut), y0), new Vector2(Vector3.Distance(pIn, pOut), y1), new Vector2(0f, y1), m);
    }

    // How much to stretch a circle's metre UVs so its circumference holds a whole number of 0.15 m tiles.
    static float TileFit(float r)
    {
        if (r < 1e-3f) return 1f;
        var circ = 2f * Mathf.PI * r;
        var tiles = Mathf.Max(1f, Mathf.Round(circ / Course));
        return tiles * Course / circ;
    }

    // Solid y-intervals of [y0, y1] at angle deg once the holes there are taken out.
    static List<Vector2> Intervals(float y0, float y1, float deg, float rOut, IList<RingHole> holes)
    {
        var list = new List<Vector2> { new Vector2(y0, y1) };
        if (holes == null) return list;
        foreach (var hole in holes)
        {
            if (deg < hole.a0 || deg > hole.a1) continue;
            var top = hole.y1;
            if (hole.round)
            {
                var r = (hole.a1 - hole.a0) * .5f * Mathf.Deg2Rad * rOut;
                var d = (deg - (hole.a0 + hole.a1) * .5f) * Mathf.Deg2Rad * rOut;
                top = hole.y1 + Mathf.Sqrt(Mathf.Max(0f, r * r - d * d));
            }
            list = Cut(list, hole.y0, top);
        }
        return list;
    }

    static List<Vector2> Cut(List<Vector2> list, float lo, float hi)
    {
        var o = new List<Vector2>();
        foreach (var iv in list)
        {
            if (hi <= iv.x || lo >= iv.y) { o.Add(iv); continue; }
            if (lo > iv.x + 1e-4f) o.Add(new Vector2(iv.x, lo));
            if (hi < iv.y - 1e-4f) o.Add(new Vector2(hi, iv.y));
        }
        return o;
    }

    static List<Vector2> Minus(List<Vector2> a, List<Vector2> b)
    {
        var o = new List<Vector2>();
        if (a == null) return o;
        if (b == null) { o.AddRange(a); return o; }
        foreach (var iv in a)
        {
            var parts = new List<Vector2> { iv };
            foreach (var cut in b) parts = Cut(parts, cut.x, cut.y);
            o.AddRange(parts);
        }
        return o;
    }

    /// <summary>A flat annular sector (a tread, a landing, a ring floor): top at yTop, thick under it, angles in degrees.</summary>
    public void Sector(Vector3 c, float rIn, float rOut, float a0, float a1, float yTop, float thick, Material m, bool solid = true, float segDeg = 7.5f)
    {
        if (a1 < a0) { var sw = a0; a0 = a1; a1 = sw; }
        var n = Mathf.Max(1, Mathf.CeilToInt((a1 - a0) / segDeg));
        var up = Vector3.up;
        var yb = yTop - thick;
        var kOut = TileFit(rOut);
        for (var k = 0; k < n; k++)
        {
            var ta = a0 + (a1 - a0) * k / n;
            var tb = a0 + (a1 - a0) * (k + 1) / n;
            var oa = c + Polar(Vector3.zero, rOut, ta);
            var ob = c + Polar(Vector3.zero, rOut, tb);
            var ia = c + Polar(Vector3.zero, rIn, ta);
            var ib = c + Polar(Vector3.zero, rIn, tb);
            oa.y = ob.y = ia.y = ib.y = 0f;
            var mid = (ta + tb) * .5f * Mathf.Deg2Rad;
            var radial = new Vector3(Mathf.Cos(mid), 0f, Mathf.Sin(mid));
            if (m != null)
            {
                QuadPlanar(ia + up * yTop, ib + up * yTop, ob + up * yTop, oa + up * yTop, up, m);
                QuadPlanar(ia + up * yb, ib + up * yb, ob + up * yb, oa + up * yb, -up, m);
                float ua = ta * Mathf.Deg2Rad * rOut * kOut, ub = tb * Mathf.Deg2Rad * rOut * kOut;
                Quad(oa + up * yb, ob + up * yb, ob + up * yTop, oa + up * yTop, radial, new Vector2(ua, yb), new Vector2(ub, yb), new Vector2(ub, yTop), new Vector2(ua, yTop), m);
                if (rIn > 1e-3f) QuadPlanar(ia + up * yb, ib + up * yb, ib + up * yTop, ia + up * yTop, -radial, m);
                if (k == 0) RadialFace(ia, oa, yb, yTop, -new Vector3(-Mathf.Sin(ta * Mathf.Deg2Rad), 0f, Mathf.Cos(ta * Mathf.Deg2Rad)), m);
                if (k == n - 1) RadialFace(ib, ob, yb, yTop, new Vector3(-Mathf.Sin(tb * Mathf.Deg2Rad), 0f, Mathf.Cos(tb * Mathf.Deg2Rad)), m);
            }
            if (solid)
            {
                var rm = (rIn + rOut) * .5f;
                var centre = c + Polar(Vector3.zero, rm, (ta + tb) * .5f);
                centre.y = yTop - thick * .5f;
                var chord = 2f * rOut * Mathf.Sin((tb - ta) * .5f * Mathf.Deg2Rad) + .02f;
                Mesh.Collider(centre, new Vector3(rOut - rIn, thick, chord), Quaternion.LookRotation(new Vector3(-Mathf.Sin(mid), 0f, Mathf.Cos(mid)), up));
            }
        }
    }

    /// <summary>A square slab (half size <paramref name="half"/>, top yTop) round a circular hole of radius r at c: the floor round a shaft.</summary>
    public void SlabRoundHole(Vector3 c, float half, float r, float yTop, float thick, Material m, bool solid = true, int segments = 48)
    {
        var up = Vector3.up;
        var yb = yTop - thick;
        for (var k = 0; k < segments; k++)
        {
            var ta = 360f * k / segments;
            var tb = 360f * (k + 1) / segments;
            var ia = c + Polar(Vector3.zero, r, ta);
            var ib = c + Polar(Vector3.zero, r, tb);
            var oa = c + SquarePoint(half, ta);
            var ob = c + SquarePoint(half, tb);
            ia.y = ib.y = oa.y = ob.y = 0f;
            if (m != null)
            {
                QuadPlanar(ia + up * yTop, ib + up * yTop, ob + up * yTop, oa + up * yTop, up, m);
                QuadPlanar(ia + up * yb, ib + up * yb, ob + up * yb, oa + up * yb, -up, m);
            }
            if (solid) SolidQuad(ia + up * yTop, ib + up * yTop, ob + up * yTop, oa + up * yTop);
        }
    }

    // A walkable quad in the collision mesh (two-sided so the controller stands on it from above).
    void SolidQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        var up = Vector3.up;
        if (Vector3.Dot(Vector3.Cross(b - a, d - a), up) < 0f) { var t = b; b = d; d = t; }
        Mesh.QuadUV(a, b, c, d, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, null, true);
    }

    static Vector3 SquarePoint(float half, float deg)
    {
        var a = deg * Mathf.Deg2Rad;
        var x = Mathf.Cos(a);
        var z = Mathf.Sin(a);
        var k = half / Mathf.Max(Mathf.Abs(x), Mathf.Abs(z));
        return new Vector3(x * k, 0f, z * k);
    }

    // ---------------------------------------------------------------- sweeps (the flume)

    /// <summary>
    /// A profile (x across, y up) swept along points p[i0..i1] with frames right[i] and up[i]; u = metres along, v = metres
    /// round the profile. Two-sided adds the back faces (an open channel seen from inside and out). No collider.
    /// </summary>
    public void Sweep(IList<Vector3> p, IList<Vector3> right, IList<Vector3> upv, int i0, int i1, IList<Vector2> prof, bool closed, bool twoSided, Material m, float uStart = 0f, Material backMat = null)
    {
        if (m == null || p == null || prof == null || prof.Count < 2) return;
        i0 = Mathf.Clamp(i0, 0, p.Count - 1);
        i1 = Mathf.Clamp(i1, 0, p.Count - 1);
        if (i1 <= i0) return;
        var np = prof.Count;
        var vs = new List<Vector3>();
        var ns = new List<Vector3>();
        var uvs = new List<Vector2>();
        var ts = new List<int>();
        var along = uStart;
        var vAcc = new float[np];
        for (var k = 1; k < np; k++) vAcc[k] = vAcc[k - 1] + Vector2.Distance(prof[k - 1], prof[k]);
        for (var i = i0; i <= i1; i++)
        {
            if (i > i0) along += Vector3.Distance(p[i - 1], p[i]);
            for (var k = 0; k < np; k++)
            {
                vs.Add(p[i] + right[i] * prof[k].x + upv[i] * prof[k].y);
                // Normal from the profile's tangent (outward for a counter-clockwise profile, fixed below by winding).
                var ka = closed ? (k - 1 + np) % np : Mathf.Max(0, k - 1);
                var kb = closed ? (k + 1) % np : Mathf.Min(np - 1, k + 1);
                var tg = prof[kb] - prof[ka];
                var n2 = new Vector2(tg.y, -tg.x).normalized;
                ns.Add((right[i] * n2.x + upv[i] * n2.y).normalized);
                uvs.Add(new Vector2(along, vAcc[k]));
            }
        }
        var segs = closed ? np : np - 1;
        for (var i = 0; i < i1 - i0; i++)
            for (var k = 0; k < segs; k++)
            {
                int a = i * np + k, b = i * np + (k + 1) % np, c = a + np, e = b + np;
                ts.Add(a); ts.Add(b); ts.Add(c);
                ts.Add(b); ts.Add(e); ts.Add(c);
            }
        // Make the winding agree with the stored normals (front = along the normal).
        if (ts.Count >= 3)
        {
            var fn = Vector3.Cross(vs[ts[1]] - vs[ts[0]], vs[ts[2]] - vs[ts[0]]);
            if (Vector3.Dot(fn, ns[ts[0]]) < 0f)
                for (var q = 0; q < ts.Count; q += 3) { var sw = ts[q + 1]; ts[q + 1] = ts[q + 2]; ts[q + 2] = sw; }
        }
        Mesh.Append(vs, ns, uvs, ts, m, false);
        if (!twoSided) return;
        var back = new List<Vector3>(ns.Count);
        foreach (var n in ns) back.Add(-n);
        var tb = new List<int>(ts.Count);
        for (var q = 0; q < ts.Count; q += 3) { tb.Add(ts[q]); tb.Add(ts[q + 2]); tb.Add(ts[q + 1]); }
        Mesh.Append(vs, back, uvs, tb, backMat != null ? backMat : m, false);
    }

    // ---------------------------------------------------------------- stairs, step runs, ramps

    /// <summary>
    /// A straight flight: foot = the centre of the first riser's bottom edge, dir = the way up (horizontal), n risers of
    /// riser over n - 1 treads of tread (the top floor starts at the last riser). Treads in <paramref name="treadMat"/>, a dark
    /// nosing (50 mm on the tread, 25 mm down the riser) on every step and on the top edge when nosing is given; solid
    /// steps down to the foot's floor when <paramref name="solidBase"/>, else a 0.2 m waist. One ramp collider along the nosing line.
    /// </summary>
    public void Flight(Vector3 foot, Vector3 dir, float width, int n, float riser, float tread, Material treadMat, Material nosing, bool solidBase = true, bool collide = true)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude < 1e-6f || n < 1) return;
        dir.Normalize();
        var side = new Vector3(dir.z, 0f, -dir.x);
        var up = Vector3.up;
        var hw = width * .5f;
        if (isNew) FlightNew(foot, dir, side, width, n, riser, tread, treadMat, nosing, solidBase);
        else
        {
        for (var k = 1; k <= n - 1; k++)
        {
            var top = foot.y + k * riser;
            var bottom = solidBase ? foot.y : top - riser - .2f;
            var a = foot + dir * ((k - 1) * tread);
            var b = foot + dir * (k * tread);
            a.y = b.y = 0f;
            Prism(a, b, bottom, top, width, treadMat, false);
            if (nosing != null) Prism(a, a + dir * NosingTread, top - NosingRiser, top + .003f, width + .002f, nosing, false);
        }
        if (nosing != null)
        {
            var e = foot + dir * ((n - 1) * tread);
            e.y = 0f;
            var top = foot.y + n * riser;
            Prism(e, e + dir * NosingTread, top - NosingRiser, top + .003f, width + .002f, nosing, false);
        }
        // The top riser, 1 mm behind the edge line: a thin upper slab (0.05) left a dark slot under its edge (scene 4 review
        // SP3-F10, KIT-F7); where the upper floor has a face of its own on the edge, that face stands in front of it.
        {
            var e = foot + dir * ((n - 1) * tread);
            e.y = 0f;
            Prism(e + dir * .001f, e + dir * .021f, foot.y + (n - 1) * riser, foot.y + n * riser - .002f, width, treadMat, false);
        }
        }
        if (!collide) return;
        // The ramp: its top runs through the nosings, from one tread before the first riser (on the foot's floor).
        var start = foot - dir * tread;
        start.y = foot.y;
        var end = foot + dir * ((n - 1) * tread);
        end.y = foot.y + n * riser;
        var along = end - start;
        var slope = along.normalized;
        var normal = Vector3.Cross(slope, side);
        if (normal.y < 0f) normal = -normal;
        var centre = (start + end) * .5f - normal * .15f;
        Mesh.Collider(centre, new Vector3(width, .3f, along.magnitude), Quaternion.LookRotation(slope, normal));
    }

    /// <summary>A step run or a STEP on a level edge: n risers of 0.15 (the height difference / 0.15) from the lower floor up to the edge, solid to the floor.</summary>
    public void StepRun(Vector3 edgeCentre, Vector3 upDir, float width, float lowY, float highY, Material treadMat, Material nosing)
    {
        var n = Mathf.Max(1, Mathf.RoundToInt((highY - lowY) / Course));
        var riser = (highY - lowY) / n;
        upDir.y = 0f;
        upDir.Normalize();
        var foot = edgeCentre - upDir * ((n - 1) * Step);
        foot.y = lowY;
        Flight(foot, upDir, width, n, riser, Step, treadMat, nosing, true, true);
    }

    /// <summary>
    /// A raking wall beside a flight (a cheek, or a guard on its open side): plan line a..b, a.y and b.y the nosing line's height
    /// at each end; its top <paramref name="above"/> over that line, its bottom at <paramref name="bottomA"/> / <paramref name="bottomB"/>
    /// (the floor under a solid flight, a waist under a flight-bridge). Faces and ends vertical, so it never leans past the piece
    /// it meets (a tilted box did: scene 4 review SP6-F1); u along the line from world 0 and v = world y, as Prism, so tile
    /// courses stay level. Solid adds one collider along the slope from the nosing line to its top.
    /// </summary>
    public void RakeWall(Vector3 a, Vector3 b, float above, float bottomA, float bottomB, float thick, Material m, bool solid = true)
    {
        var flat = new Vector3(b.x - a.x, 0f, b.z - a.z);
        var len = flat.magnitude;
        if (len < .02f || thick < 1e-3f) return;
        var t = flat / len;
        var s = new Vector3(t.z, 0f, -t.x);
        var h = s * (thick * .5f);
        var pa = new Vector3(a.x, 0f, a.z);
        var pb = new Vector3(b.x, 0f, b.z);
        float ta = a.y + above, tb = b.y + above;
        var up = Vector3.up;
        if (m != null)
        {
            var u0 = Vector3.Dot(W(pa), t);
            Quad(pa + h + up * bottomA, pb + h + up * bottomB, pb + h + up * tb, pa + h + up * ta, s,
                new Vector2(u0, bottomA), new Vector2(u0 + len, bottomB), new Vector2(u0 + len, tb), new Vector2(u0, ta), m);
            Quad(pa - h + up * bottomA, pb - h + up * bottomB, pb - h + up * tb, pa - h + up * ta, -s,
                new Vector2(u0, bottomA), new Vector2(u0 + len, bottomB), new Vector2(u0 + len, tb), new Vector2(u0, ta), m);
            var v0 = Vector3.Dot(W(pa), s);
            Quad(pa + h + up * bottomA, pa - h + up * bottomA, pa - h + up * ta, pa + h + up * ta, -t,
                new Vector2(v0 + thick * .5f, bottomA), new Vector2(v0 - thick * .5f, bottomA), new Vector2(v0 - thick * .5f, ta), new Vector2(v0 + thick * .5f, ta), m);
            Quad(pb + h + up * bottomB, pb - h + up * bottomB, pb - h + up * tb, pb + h + up * tb, t,
                new Vector2(v0 + thick * .5f, bottomB), new Vector2(v0 - thick * .5f, bottomB), new Vector2(v0 - thick * .5f, tb), new Vector2(v0 + thick * .5f, tb), m);
            QuadPlanar(pa + h + up * ta, pb + h + up * tb, pb - h + up * tb, pa - h + up * ta, up, m);
            QuadPlanar(pa + h + up * bottomA, pb + h + up * bottomB, pb - h + up * bottomB, pa - h + up * bottomA, -up, m);
        }
        if (!solid) return;
        var slope = (b - a).normalized;
        var normal = Vector3.Cross(slope, s);
        if (normal.y < 0f) normal = -normal;
        Mesh.Collider((a + b) * .5f + up * (above * .5f), new Vector3(thick, above * Mathf.Max(.2f, normal.y), Vector3.Distance(a, b)), Quaternion.LookRotation(slope, normal));
    }

    /// <summary>
    /// A guard on a flight's open side in a guard style: a..b on the nosing line (b the upper end), its top
    /// <paramref name="height"/> over that line; a half-wall (in <paramref name="wall"/>, under an oak cap) or a tiled wall down
    /// to bottomA / bottomB; a rail, glass or mesh on posts with raking rails over the nosing line. One collider.
    /// </summary>
    public void RakingGuard(Vector3 a, Vector3 b, float bottomA, float bottomB, GuardStyle style, Material wall = null, Material cap = null, float height = GuardHeight)
    {
        if (style == GuardStyle.None) return;
        if (isNew && RakingGuardNew(a, b, bottomA, bottomB, style, wall, cap, height)) return;
        switch (style)
        {
            case GuardStyle.HalfWall:
                RakeWall(a, b, height - .04f, bottomA, bottomB, .16f, wall != null ? wall : FrontRoomsPoolMaterials.Lit("guard wall", new Color(.7f, .68f, .6f)), true);
                RakeWall(a, b, height, a.y + height - .04f, b.y + height - .04f, .2f, cap != null ? cap : CapMaterial(), false);
                return;
            case GuardStyle.TileWall:
                RakeWall(a, b, height, bottomA, bottomB, .2f, wall != null ? wall : FrontRoomsPoolMaterials.Tile, true);
                return;
        }
        var post = style == GuardStyle.Glass ? BandMaterial() : PostMaterial();
        var flat = new Vector3(b.x - a.x, 0f, b.z - a.z);
        var count = Mathf.Max(1, Mathf.CeilToInt(flat.magnitude / (style == GuardStyle.Rail ? 1.2f : 1.5f)));
        var size = style == GuardStyle.Rail ? .04f : .05f;
        var t = flat.normalized;
        for (var k = 0; k <= count; k++)
        {
            var q = Vector3.Lerp(a, b, k / (float)count);
            var pq = new Vector3(q.x, 0f, q.z);
            Prism(pq - t * (size * .5f), pq + t * (size * .5f), q.y, q.y + height, size, post, false);
        }
        if (style == GuardStyle.Glass)
        {
            RakeWall(a, b, .10f, a.y, b.y, .06f, post, false);
            RakeWall(a, b, height - .05f, a.y + .10f, b.y + .10f, .012f, GlassMaterial(), false);
            RakeWall(a, b, height, a.y + height - .05f, b.y + height - .05f, .06f, cap != null ? cap : post, false);
        }
        else
        {
            if (style == GuardStyle.Mesh) RakeWall(a, b, height - .04f, a.y, b.y, .01f, MeshMaterial(), false);
            else RakeWall(a, b, .55f, a.y + .51f, b.y + .51f, .03f, post, false);
            RakeWall(a, b, height, a.y + height - .05f, b.y + height - .05f, .05f, post, false);
        }
        RakeWall(a, b, height, a.y, b.y, style == GuardStyle.Glass ? .06f : .05f, null, true);
    }

    /// <summary>A sloped slab: the top runs from lowCentre (its y) along dir for <paramref name="length"/> m (plan) rising <paramref name="rise"/>.</summary>
    public void Ramp(Vector3 lowCentre, Vector3 dir, float width, float length, float rise, Material m, float thick = .2f)
    {
        dir.y = 0f;
        dir.Normalize();
        var side = new Vector3(dir.z, 0f, -dir.x);
        var start = lowCentre;
        var end = lowCentre + dir * length + Vector3.up * rise;
        var slope = (end - start).normalized;
        var normal = Vector3.Cross(slope, side);
        if (normal.y < 0f) normal = -normal;
        var centre = (start + end) * .5f - normal * (thick * .5f);
        Mesh.Box(centre, new Vector3(width, thick, Vector3.Distance(start, end)), Quaternion.LookRotation(slope, normal), m, true);
    }

    /// <summary>
    /// A switchback core (B3): plan x0..x1 holds two flights side by side (the first half x0..xm going +z, the second
    /// xm..x1 going -z), z0..z1 its length with the start landing (depth <paramref name="startDepth"/>) at z0 and the far
    /// landing at z1. floors are the levels it joins (y, bottom to top); each storey takes two flights of risers 0.15-ish
    /// (whole risers, half the storey each) on 0.30 treads, a scissor wall between them, landings as slabs.
    /// </summary>
    public void CoreB3(float x0, float x1, float z0, float z1, float startDepth, IList<float> floors, Material tread, Material nosing, Material landing, Material scissor, bool topOpen = false)
    {
        if (floors == null || floors.Count < 2) return;
        var xm = (x0 + x1) * .5f;
        var w = (x1 - x0) * .5f - .05f;
        for (var k = 0; k + 1 < floors.Count; k++)
        {
            var lo = floors[k];
            var hi = floors[k + 1];
            var half = (hi - lo) * .5f;
            var n = Mathf.Max(2, Mathf.RoundToInt(half / Course));
            var riser = half / n;
            var footZ = z0 + startDepth;
            var farZ = footZ + (n - 1) * Step;
            // Flight 1, west half, up +z; the far landing; flight 2, east half, up -z.
            Flight(new Vector3((x0 + xm) * .5f, lo, footZ), Vector3.forward, w, n, riser, Step, tread, nosing, false);
            Slab(x0, farZ, x1, z1, lo + half, landing);
            Flight(new Vector3((xm + x1) * .5f, lo + half, farZ), Vector3.back, w, n, riser, Step, tread, nosing, false);
            // The scissor wall between the flights (from the lower flight's foot to the upper landing's soffit).
            // topOpen (the core's last pair, nothing stands above it): the wall rises a guard past the top landing and a rail
            // closes the landing's open edge over flight 1.
            var topStorey = topOpen && k + 2 == floors.Count;
            if (scissor != null) Prism(new Vector3(xm, 0f, footZ), new Vector3(xm, 0f, farZ), lo, topStorey ? hi + GuardHeight : hi - SlabThick, .1f, scissor, true);
            // The start landing at the upper level (the floor's own landing at k = 0 is the caller's floor).
            Slab(x0, z0, x1, footZ, hi, landing);
            if (topStorey) Guard(new Vector3(x0, 0f, footZ), new Vector3(xm, 0f, footZ), hi, GuardStyle.Rail);
        }
    }

    /// <summary>
    /// A spiral or circular stair round c: treads from rIn to rOut, the first riser at startDeg, each tread stepDeg (signed:
    /// positive turns from +x toward +z), n risers of riser from baseY (n - 1 treads; the top floor starts at the last riser),
    /// optionally one landing (the tread after riser landingAfter spans landingDeg). Porcelain or given treads with a dark
    /// nosing, ribbons on the given radii (from 0.3 under the nosing line to guard height above it; 0 skips one), one box
    /// collider per ribbon segment. The walker stands on a helical ramp through every nosing (as a straight flight's ramp),
    /// not on the treads: per-tread boxes caught the player's capsule (r 0.3, step 0.3) on the floating treads, so the tower
    /// could not be climbed from its foot (the level_space walk test, after Red's 11-04 report of spirals that cannot be
    /// walked). Returns the angle of the last riser.
    /// </summary>
    public float Spiral(Vector3 c, float rIn, float rOut, float startDeg, float stepDeg, int n, float riser, float baseY, Material tread, Material nosing,
        float thick = .12f, int landingAfter = -1, float landingDeg = 0f, float innerRibbon = 0f, float outerRibbon = 0f, Material ribbon = null, float ribbonHeight = GuardHeight)
    {
        var sign = stepDeg >= 0f ? 1f : -1f;
        var a = startDeg;
        var samples = new List<Vector2>();   // (angle, nosing-line height) for the ribbons
        samples.Add(new Vector2(a, baseY));
        // The ramp's (angle, height): from half a tread before the first riser on the floor, through each tread's middle at
        // its top (so the feet stand at most half a riser over a tread and the headroom under a landing a turn up is kept),
        // flat across a landing, to the top floor half a tread past the last riser.
        var ramp = new List<Vector2> { new Vector2(a - stepDeg * .5f, baseY) };
        // K2 (structures G2-c, C3; review D9), NEW only, the pool's tiled spirals: each tread a block on a helicoid waist (a smooth
        // soffit under the whole stair, not a sawtooth of floating boxes), the pool's nosing L on each step, the ribbons faceted by
        // radius under a Cap course, and between walls an inside helical handrail on the newel; the colliders are today's
        var newTreads = isNew && tread != null && CurveTile(tread, out var tOwn, out var tPitch);
        var poolNosing = newTreads && nosing != null && NosingKindOf(nosing, tread) == NosingKind.Pool;
        for (var k = 0; k < n - 1; k++)
        {
            var span = k == landingAfter ? landingDeg * sign : stepDeg;
            var top = baseY + (k + 1) * riser;
            var b = a + span;
            if (newTreads) SpiralTread(c, rIn, rOut, a, b, top, riser, tread, k == 0 ? baseY : top - riser);
            else Sector(c, rIn, rOut, Mathf.Min(a, b), Mathf.Max(a, b), top, thick, tread, false, 6f);
            if (poolNosing) SpiralNosing(c, rIn, rOut, a, top, sign);
            else if (nosing != null)
            {
                // The nosing on the leading (lower) edge: a radial strip 50 mm into the tread.
                var w = NosingTread / Mathf.Max(.3f, (rIn + rOut) * .5f) * Mathf.Rad2Deg * sign;
                Sector(c, rIn, rOut, Mathf.Min(a, a + w), Mathf.Max(a, a + w), top + .003f, NosingRiser + .003f, nosing, false, 30f);
            }
            if (k > 0) samples.Add(new Vector2(a, top));
            if (k == landingAfter && Mathf.Abs(span) > Mathf.Abs(stepDeg) * 1.5f)
            {
                ramp.Add(new Vector2(a + stepDeg * .5f, top));
                ramp.Add(new Vector2(b - stepDeg * .5f, top));
            }
            else ramp.Add(new Vector2(a + span * .5f, top));
            a = b;
        }
        samples.Add(new Vector2(a, baseY + n * riser));
        ramp.Add(new Vector2(a + stepDeg * .5f, baseY + n * riser));
        HelixRamp(c, rIn + .01f, rOut - .01f, ramp);
        if (ribbon != null)
        {
            if (isNew && CurveTile(ribbon, out var rOwn, out var rPitch))
            {
                if (outerRibbon > 0f) RibbonNew(c, rOut, rOut + outerRibbon, samples, ribbonHeight, ribbon, rOwn, rPitch);
                if (innerRibbon > 0f && rIn - innerRibbon > .01f) RibbonNew(c, rIn - innerRibbon, rIn, samples, ribbonHeight, ribbon, rOwn, rPitch);
            }
            else
            {
                if (outerRibbon > 0f) Ribbon(c, rOut, rOut + outerRibbon, samples, ribbonHeight, ribbon);
                if (innerRibbon > 0f && rIn - innerRibbon > .01f) Ribbon(c, rIn - innerRibbon, rIn, samples, ribbonHeight, ribbon);
            }
        }
        if (newTreads && innerRibbon <= 0f && outerRibbon <= 0f && rIn > .05f) SpiralRail(c, rIn, samples);
        return a;
    }

    // K2 (structures G2-c, C3): one tread of a pool spiral in NEW: its top (a..b at 'top'), its riser at a (facing the step below,
    // from riserFoot), and under it the helicoid waist: the soffit runs from 'top - riser - waist' at a to 'top - waist' at b, so the
    // treads' soffits make one smooth helicoid. Render only.
    void SpiralTread(Vector3 c, float rIn, float rOut, float a, float b, float top, float riser, Material m, float riserFoot)
    {
        const float Waist = .12f;
        var up = Vector3.up;
        var steps = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(b - a) / 6f));
        var acc = 0f;
        var rm = (rIn + rOut) * .5f;
        for (var q = 0; q < steps; q++)
        {
            var ta = Mathf.Lerp(a, b, q / (float)steps);
            var tb = Mathf.Lerp(a, b, (q + 1) / (float)steps);
            var ia = c + Polar(Vector3.zero, rIn, ta);
            var ib = c + Polar(Vector3.zero, rIn, tb);
            var oa = c + Polar(Vector3.zero, rOut, ta);
            var ob = c + Polar(Vector3.zero, rOut, tb);
            ia.y = ib.y = oa.y = ob.y = 0f;
            QuadPlanar(ia + up * top, ib + up * top, ob + up * top, oa + up * top, up, m);
            float sa = top - riser - Waist + riser * q / steps, sb = top - riser - Waist + riser * (q + 1) / steps;
            var seg = Mathf.Abs(tb - ta) * Mathf.Deg2Rad * rm;
            Quad(ia + up * sa, ib + up * sb, ob + up * sb, oa + up * sa, Vector3.down,
                new Vector2(acc, 0f), new Vector2(acc + seg, 0f), new Vector2(acc + seg, rOut - rIn), new Vector2(acc, rOut - rIn), m);
            acc += seg;
        }
        var pIn = c + Polar(Vector3.zero, rIn, a);
        var pOut = c + Polar(Vector3.zero, rOut, a);
        pIn.y = pOut.y = 0f;
        var tan = new Vector3(-Mathf.Sin(a * Mathf.Deg2Rad), 0f, Mathf.Cos(a * Mathf.Deg2Rad));
        RadialFace(pIn, pOut, Mathf.Min(riserFoot, top - riser - Waist), top, -tan * (b >= a ? 1f : -1f), m);
    }

    // K2 (structures G2-c, C3): the pool's nosing L (Red's dark nosing, 50 mm on the tread, 25 mm down the riser) along a spiral
    // step's leading edge, its nose out over the step below.
    void SpiralNosing(Vector3 c, float rIn, float rOut, float a, float top, float sign)
    {
        var p0 = c + Polar(Vector3.zero, rIn + .001f, a);
        var p1 = c + Polar(Vector3.zero, rOut - .001f, a);
        p0.y = p1.y = top;
        if (sign < 0f) { var sw = p0; p0 = p1; p1 = sw; }
        var m = FrontRoomsStructuresAssets.FarAA(FrontRoomsPoolTile.Get("Nosing"), NosingFarAA);
        if (m != null) Lay(p0, p1, FrontRoomsStructureProfiles.PoolNosing(), m, Vector2.zero, false);
    }

    // K2 (structures G2-c, C3, C8): a ribbon in NEW: its faces faceted by radius (FrontRoomsStructureCurveTile.HelixBand), a Cap
    // course along its helical top (the top on the guard's height), today's colliders (Ribbon with no material).
    void RibbonNew(Vector3 c, float r0, float r1, List<Vector2> samples, float guard, Material m, string own, float ownPitch)
    {
        var o = new FrontRoomsStructureCurveTile.Out();
        FrontRoomsStructureCurveTile.HelixBand(o, new Vector3(c.x, 0f, c.z), r0, r1, samples, .3f, guard - TileCapThick, own, ownPitch);
        AppendCurve(o, m);
        var sorted = new List<Vector2>(samples);
        sorted.Sort((p, q) => p.x.CompareTo(q.x));
        float lo = sorted[0].x, hi = sorted[sorted.Count - 1].x;
        if (hi - lo > 1e-3f)
        {
            var steps = Mathf.Max(2, Mathf.CeilToInt((hi - lo) / 3f));
            var rm = (r0 + r1) * .5f;
            var path = new List<Vector3>();
            for (var i = 0; i <= steps; i++)
            {
                var th = Mathf.Lerp(lo, hi, i / (float)steps);
                var p = c + Polar(Vector3.zero, rm, th);
                p.y = FrontRoomsStructureCurveTile.HelixH(sorted, th) + guard - TileCapThick;
                path.Add(p);
            }
            var opts = FrontRoomsStructureProfiles.SweepOptions.Default;
            opts.plumb = true;
            opts.capStart = opts.capEnd = true;
            opts.u0 = FrontRoomsStructureProfiles.WorldU(W(path[0]), path[1] - path[0]);
            SweepTo(m, path, FrontRoomsStructureCurveTile.CapSection(r1 - r0, TileCapThick), opts);
        }
        Ribbon(c, r0, r1, samples, guard, null);
    }

    // K2 (structures G2-c, C3; 30_plan S1-03): a spiral between walls (the tower) takes an inside helical handrail on the newel:
    // the 42.2 mm stainless tube 1 1/2 in off the newel, 0.90 m over the nosing line, on G1-b's cast wall brackets about every 1.5 m.
    void SpiralRail(Vector3 c, float rNewel, List<Vector2> samples)
    {
        const float Off = .0592f, H = .9f;
        var sorted = new List<Vector2>(samples);
        sorted.Sort((p, q) => p.x.CompareTo(q.x));
        float lo = sorted[0].x, hi = sorted[sorted.Count - 1].x;
        if (hi - lo < 10f) return;
        var rr = rNewel + Off;
        // 12 degrees a segment on r 0.31 (a sagitta of 1.7 mm, the 42 mm tube's 24 sides): about 5.8 k triangles for four turns
        var steps = Mathf.Max(2, Mathf.CeilToInt((hi - lo) / 12f));
        var path = new List<Vector3>();
        for (var i = 0; i <= steps; i++)
        {
            var th = Mathf.Lerp(lo, hi, i / (float)steps);
            var p = c + Polar(Vector3.zero, rr, th);
            p.y = FrontRoomsStructureCurveTile.HelixH(sorted, th) + H;
            path.Add(p);
        }
        var metal = RailMetal(Family.Pool);
        var o = FrontRoomsStructureProfiles.SweepOptions.Default;
        o.capStart = o.capEnd = true;
        o.creaseDeg = 25f;
        SweepTo(metal, path, RailTube, o, Vector3.up);
        var arcLen = (hi - lo) * Mathf.Deg2Rad * rr;
        var nb = Mathf.Max(2, Mathf.RoundToInt(arcLen / 1.5f));
        for (var j = 0; j < nb; j++)
        {
            var th = lo + (hi - lo) * (j + .5f) / nb;
            var at = c + Polar(Vector3.zero, rr, th);
            at.y = FrontRoomsStructureCurveTile.HelixH(sorted, th) + H;
            var tan = new Vector3(-Mathf.Sin(th * Mathf.Deg2Rad), 0f, Mathf.Cos(th * Mathf.Deg2Rad));
            Fitting("Kit_RailBracket_Wall", at, -tan);
        }
    }

    /// <summary>A flat walking surface (collision only) on an annular sector a0..a1 at y: a landing whose own boxes would
    /// overhang the stair under it.</summary>
    public void WalkSurface(Vector3 c, float rIn, float rOut, float a0, float a1, float y) =>
        HelixRamp(c, rIn + .01f, rOut - .01f, new List<Vector2> { new Vector2(a0, y), new Vector2(a1, y) });

    // A helical walking surface between radii r0..r1 through (angle, height) points: collision only, a quad every 6 degrees.
    void HelixRamp(Vector3 c, float r0, float r1, List<Vector2> pts)
    {
        for (var k = 0; k + 1 < pts.Count; k++)
        {
            var s0 = pts[k];
            var s1 = pts[k + 1];
            var steps = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(s1.x - s0.x) / 6f));
            for (var q = 0; q < steps; q++)
            {
                var ta = Mathf.Lerp(s0.x, s1.x, q / (float)steps);
                var tb = Mathf.Lerp(s0.x, s1.x, (q + 1) / (float)steps);
                var ha = Mathf.Lerp(s0.y, s1.y, q / (float)steps);
                var hb = Mathf.Lerp(s0.y, s1.y, (q + 1) / (float)steps);
                var ia = c + Polar(Vector3.zero, r0, ta);
                var ib = c + Polar(Vector3.zero, r0, tb);
                var oa = c + Polar(Vector3.zero, r1, ta);
                var ob = c + Polar(Vector3.zero, r1, tb);
                ia.y = oa.y = ha;
                ib.y = ob.y = hb;
                SolidQuad(ia, ib, ob, oa);
            }
        }
    }

    // A helical band between radii r0..r1 following (angle, height) samples: from height - 0.3 to height + guard.
    void Ribbon(Vector3 c, float r0, float r1, List<Vector2> samples, float guard, Material m)
    {
        var up = Vector3.up;
        for (var k = 0; k + 1 < samples.Count; k++)
        {
            var s0 = samples[k];
            var s1 = samples[k + 1];
            if (Mathf.Abs(s1.x - s0.x) < 1e-3f) continue;
            var steps = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(s1.x - s0.x) / 6f));
            for (var q = 0; q < steps; q++)
            {
                var fa = q / (float)steps;
                var fb = (q + 1) / (float)steps;
                var ta = Mathf.Lerp(s0.x, s1.x, fa);
                var tb = Mathf.Lerp(s0.x, s1.x, fb);
                var ha = Mathf.Lerp(s0.y, s1.y, fa);
                var hb = Mathf.Lerp(s0.y, s1.y, fb);
                var oa = c + Polar(Vector3.zero, r1, ta);
                var ob = c + Polar(Vector3.zero, r1, tb);
                var ia = c + Polar(Vector3.zero, r0, ta);
                var ib = c + Polar(Vector3.zero, r0, tb);
                oa.y = ob.y = ia.y = ib.y = 0f;
                var mid = (ta + tb) * .5f * Mathf.Deg2Rad;
                var radial = new Vector3(Mathf.Cos(mid), 0f, Mathf.Sin(mid));
                float ua = ta * Mathf.Deg2Rad * r1, ub = tb * Mathf.Deg2Rad * r1;
                Quad(oa + up * (ha - .3f), ob + up * (hb - .3f), ob + up * (hb + guard), oa + up * (ha + guard), radial,
                    new Vector2(ua, ha - .3f), new Vector2(ub, hb - .3f), new Vector2(ub, hb + guard), new Vector2(ua, ha + guard), m);
                Quad(ia + up * (ha - .3f), ib + up * (hb - .3f), ib + up * (hb + guard), ia + up * (ha + guard), -radial,
                    new Vector2(ua, ha - .3f), new Vector2(ub, hb - .3f), new Vector2(ub, hb + guard), new Vector2(ua, ha + guard), m);
                QuadPlanar(ia + up * (ha + guard), ib + up * (hb + guard), ob + up * (hb + guard), oa + up * (ha + guard), up, m);
                QuadPlanar(ia + up * (ha - .3f), ib + up * (hb - .3f), ob + up * (hb - .3f), oa + up * (ha - .3f), -up, m);
                var centre = c + Polar(Vector3.zero, (r0 + r1) * .5f, (ta + tb) * .5f);
                centre.y = (ha + hb) * .5f + (guard - .3f) * .5f;
                var chord = Vector3.Distance(oa, ob) + .02f;
                Mesh.Collider(centre, new Vector3(Mathf.Max(.05f, r1 - r0), guard + .3f + Mathf.Abs(hb - ha), chord), Quaternion.LookRotation(new Vector3(-Mathf.Sin(mid), 0f, Mathf.Cos(mid)), up));
            }
        }
    }

    // ---------------------------------------------------------------- guards

    static Material guardCap, guardBand, guardPost, guardMesh;

    /// <summary>
    /// A guard 1.07 m high along a..b (plan) standing on floorY, in a style: a half-wall (in <paramref name="wall"/>, with a
    /// cap), a tiled half-wall 0.2 thick, clear glass on a 0.10 steel band with a steel cap, a rail (posts every 1.2, top and
    /// mid rail), or chain-link mesh on posts. One collider the guard's full height.
    /// </summary>
    public void Guard(Vector3 a, Vector3 b, float floorY, GuardStyle style, Material wall = null, Material cap = null, float height = GuardHeight)
    {
        if (style == GuardStyle.None) return;
        a.y = b.y = 0f;
        var len = Vector3.Distance(a, b);
        if (len < .02f) return;
        var t = (b - a) / len;
        var top = floorY + height;
        if (isNew && GuardNew(a, b, t, len, floorY, top, height, style, wall, cap)) return;
        switch (style)
        {
            case GuardStyle.HalfWall:
                Prism(a, b, floorY, top - .04f, .16f, wall != null ? wall : FrontRoomsPoolMaterials.Lit("guard wall", new Color(.7f, .68f, .6f)), true);
                Prism(a - t * .02f, b + t * .02f, top - .04f, top, .2f, cap != null ? cap : CapMaterial(), false);
                break;
            case GuardStyle.TileWall:
                Prism(a, b, floorY, top, .2f, wall != null ? wall : FrontRoomsPoolMaterials.Tile, true);
                break;
            case GuardStyle.Glass:
                Prism(a, b, floorY, floorY + .10f, .06f, BandMaterial(), false);
                Prism(a, b, floorY + .10f, top - .05f, .012f, GlassMaterial(), false);
                // A timber cap is the half-wall's own (0.2), so a glass bay in a half-wall run carries the same cap (70 F03;
                // review SP2 missed 2); steel and tile caps stay the band's 0.06.
                var capW = cap != null && cap != FrontRoomsPoolMaterials.Tile ? .2f : .06f;
                Prism(a - t * .01f, b + t * .01f, top - .05f, top, capW, cap != null ? cap : BandMaterial(), false);
                Posts(a, b, floorY, top, 1.5f, BandMaterial(), .05f, true);
                Mesh.Collider((a + b) * .5f + Vector3.up * (floorY + top) * .5f, new Vector3(.06f, height, len), Quaternion.LookRotation(t));
                break;
            case GuardStyle.Rail:
                Posts(a, b, floorY, top, 1.2f, PostMaterial(), .04f);
                Prism(a - t * .02f, b + t * .02f, top - .05f, top, .05f, PostMaterial(), false);
                Prism(a, b, floorY + .51f, floorY + .55f, .03f, PostMaterial(), false);
                Mesh.Collider((a + b) * .5f + Vector3.up * (floorY + top) * .5f, new Vector3(.05f, height, len), Quaternion.LookRotation(t));
                break;
            case GuardStyle.Mesh:
                Posts(a, b, floorY, top, 1.5f, PostMaterial(), .05f);
                Prism(a, b, floorY, top, .01f, MeshMaterial(), false);
                Prism(a - t * .02f, b + t * .02f, top - .04f, top, .04f, PostMaterial(), false);
                Mesh.Collider((a + b) * .5f + Vector3.up * (floorY + top) * .5f, new Vector3(.05f, height, len), Quaternion.LookRotation(t));
                break;
        }
    }

    void Posts(Vector3 a, Vector3 b, float floorY, float top, float pitch, Material m, float size, bool offCentre = false)
    {
        var len = Vector3.Distance(a, b);
        var t = (b - a) / Mathf.Max(1e-4f, len);
        var count = Mathf.Max(1, Mathf.CeilToInt(len / pitch));
        // Glass: an odd number of bays, so no post stands on a span's middle, where views are aimed (review SP2 missed 2:
        // the bay's middle post split the arrival's view down).
        if (offCentre && count % 2 == 0) count++;
        for (var k = 0; k <= count; k++)
        {
            var p = a + t * (len * k / count);
            Prism(p - t * (size * .5f), p + t * (size * .5f), floorY, top, size, m, false);
        }
    }

    /// <summary>Steel for caps, bands and rails: the game's Prop_SteelPutty, else a plain putty steel.</summary>
    public static Material BandMaterial()
    {
        if (guardBand != null) return guardBand;
        guardBand = FrontRoomsSurfaces.TryGet("Prop_SteelPutty");
        if (guardBand == null) guardBand = FrontRoomsPoolMaterials.Lit("steel putty", new Color(.62f, .6f, .55f), .45f, .6f);
        return guardBand;
    }

    static Material PostMaterial()
    {
        if (guardPost != null) return guardPost;
        guardPost = FrontRoomsSurfaces.TryGet("Painted_Metal");
        if (guardPost == null) guardPost = FrontRoomsPoolMaterials.Lit("painted rail", new Color(.42f, .41f, .38f), .4f, .3f);
        return guardPost;
    }

    static Material CapMaterial()
    {
        if (guardCap != null) return guardCap;
        guardCap = FrontRoomsSurfaces.TryGet("Prop_WoodOak");
        if (guardCap == null) guardCap = FrontRoomsPoolMaterials.Lit("oak cap", new Color(.55f, .40f, .25f), .45f);
        return guardCap;
    }

    static Material glassPane;

    /// <summary>
    /// Clear glass: a runtime copy of the game's Glass_Window (else a plain transparent pane). The structures mesh merges every
    /// pane into unit-scale renderers, so the glass shader's pane frame (the thinnest scaled axis is the pane's normal) always
    /// took x: every pane facing along z drew as the green float-glass edge at alpha 0.92, an opaque mint sheet (scene 4 review
    /// SP2-F4). The copy turns the auto edge and the grime (laid out in the same frame) off; the asset is never edited.
    /// </summary>
    public static Material GlassMaterial()
    {
        if (glassPane != null) return glassPane;
        var g = FrontRoomsSurfaces.TryGet("Glass_Window");
        if (g == null) return glassPane = FrontRoomsPoolMaterials.Transparent("glass", new Color(.8f, .9f, .9f, .15f), .95f);
        glassPane = new Material(g) { name = "Glass_Window (scene 4, merged panes)" };
        if (glassPane.HasProperty("_AutoEdge")) glassPane.SetFloat("_AutoEdge", 0f);
        if (glassPane.HasProperty("_Grime")) glassPane.SetFloat("_Grime", 0f);
        glassPane.DisableKeyword("_FR_GLASS_GRIME");
        return glassPane;
    }

    static Material MeshMaterial()
    {
        if (guardMesh != null) return guardMesh;
        guardMesh = FrontRoomsPoolMaterials.ChainLink();
        return guardMesh;
    }

    // ---------------------------------------------------------------- water, basins, copings

    /// <summary>A water surface (no collider, no shadow): level water is the "wrong" turquoise.</summary>
    public void Water(float x0, float z0, float x1, float z1, float y, FrontRoomsPoolMaterials.Water kind = FrontRoomsPoolMaterials.Water.Level)
    {
        // K2: the pool rig's niches read the district's own water (PlaceLampKits)
        if (poolLamps && Root != null)
        {
            var w0 = Root.TransformPoint(new Vector3(Mathf.Min(x0, x1), y, Mathf.Min(z0, z1)));
            var w1 = Root.TransformPoint(new Vector3(Mathf.Max(x0, x1), y, Mathf.Max(z0, z1)));
            waterRects.Add(new FrontRoomsStructuresPoolLamps.WaterRect(Rect.MinMaxRect(w0.x, w0.z, w1.x, w1.z), w0.y));
        }
        // K2 (structures G2-c): the district's waters (district-local) for the pool edges' water side
        if (isNew)
        {
            waterLocal.Add(new Vector4(Mathf.Min(x0, x1), Mathf.Min(z0, z1), Mathf.Max(x0, x1), Mathf.Max(z0, z1)));
            waterLocalY.Add(y);
        }
        var m = FrontRoomsPoolMaterials.WaterOf(kind);
        if (m == null) return;
        var keep = Mesh.CastShadows;
        Mesh.CastShadows = false;
        var a = new Vector3(Mathf.Min(x0, x1), y, Mathf.Min(z0, z1));
        var c = new Vector3(Mathf.Max(x0, x1), y, Mathf.Max(z0, z1));
        QuadPlanar(a, new Vector3(a.x, y, c.z), c, new Vector3(c.x, y, a.z), Vector3.up, m);
        Mesh.CastShadows = keep;
    }

    /// <summary>A tiled basin: floor at floorY, walls up to rimY (inside faces on the rectangle's edges, 0.2 thick outward).</summary>
    public void Basin(float x0, float z0, float x1, float z1, float floorY, float rimY, Material m, bool walls = true)
    {
        float lx = Mathf.Min(x0, x1), hx = Mathf.Max(x0, x1), lz = Mathf.Min(z0, z1), hz = Mathf.Max(z0, z1);
        Slab(lx, lz, hx, hz, floorY, m, .25f);
        if (!walls) return;
        const float t = .2f;
        Box(new Vector3(lx - t, floorY - .25f, lz - t), new Vector3(lx, rimY, hz + t), m);
        Box(new Vector3(hx, floorY - .25f, lz - t), new Vector3(hx + t, rimY, hz + t), m);
        Box(new Vector3(lx, floorY - .25f, lz - t), new Vector3(hx, rimY, lz), m);
        Box(new Vector3(lx, floorY - .25f, hz), new Vector3(hx, rimY, hz + t), m);
        // K2 (structures G2-c, C6), NEW only: the pool's sanitary cove (P2's Prop_PoolTile_Cove, r 50 with its flats) round the
        // basin's floor at every wall's foot, mitred at the corners (A.3.1; PC10-PC12); render only
        if (isNew && CurveTile(m, out var own, out var op))
        {
            var cove = FrontRoomsPoolTile.Get("Cove");
            var zm = (lz + hz) * .5f;
            if (cove != null)
                CoveBase(new[] { new Vector3(lx, floorY, zm), new Vector3(lx, floorY, hz), new Vector3(hx, floorY, hz), new Vector3(hx, floorY, lz), new Vector3(lx, floorY, lz), new Vector3(lx, floorY, zm) }, cove);
            // C9: its main drain and return inlets, placed at Finish (when its water is known)
            pendingBasins.Add(new Vector4(lx, lz, hx, hz));
            pendingBasinFloor.Add(floorY);
        }
    }

    /// <summary>A tiled coping along a..b on a rim at y: 0.30 wide (half over the edge line toward <paramref name="outward"/>), 0.05 proud.</summary>
    public void Coping(Vector3 a, Vector3 b, float y, Material m)
    {
        a.y = b.y = 0f;
        // K2 (structures G2-c, C6; Red's ask 1, decided), NEW only: the basin's edge as built (roll-out, rim-flow, or a tank's
        // bullnose coping), decided and chained corner to corner at Finish; the collider here is today's coping's, exactly
        if (isNew && m != null && CurveTile(m, out var own, out var op) && (b - a).sqrMagnitude > 4e-4f)
        {
            PrismCollider(a, b, y - .08f, y + .03f, .32f);
            pendingEdges.Add(new PendingEdge { a = a, b = b, y = y, m = m });
            return;
        }
        Prism(a, b, y - .08f, y + .03f, .32f, m, true);
    }

    // ---------------------------------------------------------------- lamps (the scene's light budget)

    /// <summary>
    /// A lamp of the district's own: a fixture box (render only) with a lens, and a light (spot or point) at pos aimed along
    /// dir; joins the scene's budget (on within 16 m, a view lamp while the district is current). Returns the light.
    /// </summary>
    public Light Lamp(string name, LightType type, Vector3 pos, Vector3 dir, float spot, float inner, float intensity, float range, Color color, bool view,
        Vector3 fixture, Material body, Color lensEmission)
    {
        var go = new GameObject("Lamp · " + name);
        go.transform.SetParent(PartRoot, false);
        go.transform.localPosition = pos;
        if (dir.sqrMagnitude > 1e-6f) go.transform.localRotation = Quaternion.LookRotation(dir.normalized, Mathf.Abs(dir.normalized.y) > .95f ? Vector3.forward : Vector3.up);
        Renderer lens = null;
        if (fixture.sqrMagnitude > 1e-6f)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "Fixture";
            UnityEngine.Object.Destroy(box.GetComponent<Collider>());
            box.transform.SetParent(go.transform, false);
            box.transform.localPosition = new Vector3(0f, 0f, -fixture.z * .5f);
            box.transform.localScale = fixture;
            var r = box.GetComponent<MeshRenderer>();
            r.sharedMaterial = body != null ? body : FrontRoomsPoolMaterials.Steel;
            r.shadowCastingMode = ShadowCastingMode.Off;
            var face = GameObject.CreatePrimitive(PrimitiveType.Quad);
            face.name = "Lens";
            UnityEngine.Object.Destroy(face.GetComponent<Collider>());
            face.transform.SetParent(go.transform, false);
            face.transform.localPosition = new Vector3(0f, 0f, .002f);
            face.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            face.transform.localScale = new Vector3(fixture.x * .86f, fixture.y * .8f, 1f);
            lens = face.GetComponent<MeshRenderer>();
            lens.sharedMaterial = FrontRoomsPoolMaterials.Glow("lamp lens " + ColorKey(lensEmission), lensEmission);
            lens.shadowCastingMode = ShadowCastingMode.Off;
        }
        var lightGo = new GameObject("Light");
        lightGo.transform.SetParent(go.transform, false);
        lightGo.transform.localPosition = new Vector3(0f, 0f, .05f);
        var light = lightGo.AddComponent<Light>();
        FrontRoomsMapWorld.ShippedShadowFilter(light);
        light.type = type;
        if (type == LightType.Spot)
        {
            light.spotAngle = spot;
            light.innerSpotAngle = inner;
        }
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.None;
        light.shadowStrength = .9f;
        light.shadowNearPlane = .1f;
        if (Host != null && Host.Look != null) Host.Look.AddLamp(District.Key, light, null, Color.black, view);
        lights.Add(light);
        return light;
    }

    static string ColorKey(Color c) => c.r.ToString("0.00") + "," + c.g.ToString("0.00") + "," + c.b.ToString("0.00");

    /// <summary>The game's troffer (L03 kit, the map's lens drive) on a ceiling point, with Level 0's or the Office's light.
    /// <paramref name="lensLevel"/> scales the lens drive only (the light keeps its level): SP1's pool grid passes
    /// FrontRoomsTrofferKit.PoolGridLensLevel (zone lamps line, plan A of 2026-11-10).</summary>
    public Light Troffer(Vector3 ceiling, bool view, bool office = false, float scale = 1f, float range = 10f, bool dead = false, float lensLevel = 1f)
    {
        var root = new GameObject(dead ? "Fixture (L03, dead)" : "Fixture (L03)");
        root.transform.SetParent(PartRoot, false);
        root.transform.localPosition = ceiling;
        var lensMaterial = FrontRoomsSurfaces.TryGet(office ? "Troffer_Lens_Cool" : "Troffer_Lens");
        if (lensMaterial == null) lensMaterial = FrontRoomsSurfaces.TryGet("Troffer_Lens");
        Renderer lens = null;
        var drive = Color.white * 2f;
        try
        {
            lens = FrontRoomsTrofferKit.Build(root.transform);
            if (lens != null) drive = FrontRoomsTrofferKit.LensDrive(lensMaterial);
        }
        catch (Exception e) { FrontRoomsPoolMaterials.LogOnce("troffer", "the troffer kit failed: " + e.Message); lens = null; }
        if (lens == null)
        {
            FrontRoomsPoolMaterials.LogOnce("troffer missing", "the L03 troffer kit is missing: a flat lens stands in.");
            var flat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flat.name = "Lens (flat stand-in)";
            UnityEngine.Object.Destroy(flat.GetComponent<Collider>());
            flat.transform.SetParent(root.transform, false);
            flat.transform.localPosition = new Vector3(0f, -FrontRooms.Map.ModuleUnits.TrofferDrop, 0f);
            flat.transform.localScale = new Vector3(FrontRooms.Map.ModuleUnits.TrofferShort, FrontRooms.Map.ModuleUnits.TrofferLens, FrontRooms.Map.ModuleUnits.TrofferLong);
            lens = flat.GetComponent<MeshRenderer>();
            if (lensMaterial != null) lens.sharedMaterial = lensMaterial;
            lens.shadowCastingMode = ShadowCastingMode.Off;
            drive = lensMaterial != null && lensMaterial.HasProperty("_EmissionColor") ? lensMaterial.GetColor("_EmissionColor") : new Color(2.2f, 2.1f, 1.8f);
        }
        if (dead)
        {
            var block = new MaterialPropertyBlock();
            lens.GetPropertyBlock(block);
            block.SetColor("_EmissionColor", Color.black);
            lens.SetPropertyBlock(block);
            return null;
        }
        if (!Mathf.Approximately(lensLevel, 1f))
        {
            var k = Mathf.Max(0f, lensLevel);
            drive = new Color(drive.r * k, drive.g * k, drive.b * k, drive.a);
        }
        var lightGo = new GameObject("Light");
        lightGo.transform.SetParent(root.transform, false);
        lightGo.transform.localPosition = new Vector3(0f, -FrontRooms.Map.ModuleUnits.LampDrop, 0f);
        lightGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var light = lightGo.AddComponent<Light>();
        FrontRoomsMapWorld.ShippedShadowFilter(light);
        light.type = LightType.Spot;
        light.spotAngle = 162f;
        light.innerSpotAngle = 96f;
        light.color = office ? FrontRoomsTransitionLightLead.OfficeColor : FrontRoomsTransitionLightLead.Level0Color;
        light.intensity = (office ? FrontRoomsTransitionLightLead.OfficeIntensity : FrontRoomsTransitionLightLead.Level0Intensity) * scale;
        light.range = range;
        light.shadows = LightShadows.None;
        light.shadowStrength = .92f;
        light.shadowNearPlane = .1f;
        if (Host != null && Host.Look != null) Host.Look.AddLamp(District.Key, light, lens, drive, view);
        lights.Add(light);
        return light;
    }

    /// <summary>A metal-halide wall box (0.90, 0.97, 1.0) throwing up so the lid is the brightest surface (pool halls; 70 section 2).
    /// An aim (district direction) tilts the light off the wall into the room with the given cone, the box still on the wall:
    /// straight up, the first metre of wall over the box takes grazing light at point-blank range (review SP6-F6).</summary>
    public Light WallBox(Vector3 pos, Vector3 outward, float intensity = 7f, float range = 11f, bool view = true, Vector3 aim = default(Vector3), float spot = 110f, float inner = 70f)
    {
        outward.y = 0f;
        outward.Normalize();
        // K2 (NEW in a pool district): the same light in the same place in the budget, no stand-in body; P1-A goes on the wall
        // in PlaceLampKits, the light moves to its lamp (thrown 30 degrees, or along the aim given)
        if (poolLamps && FrontRoomsLampKit.Available(FrontRoomsLampKit.Family.WallBox))
        {
            var k = Lamp("metal-halide wall box", LightType.Spot, pos + outward * .14f, Vector3.up, spot, inner, intensity, range, new Color(.90f, .97f, 1f), view,
                Vector3.zero, null, Color.black);
            if (k != null) lampRequests.Add(new LampRequest { kind = LampKind.Box, light = k, pos = pos, dir = outward, aim = aim });
            return k;
        }
        // Thrown up 30 degrees off the wall into the room with a 110-degree cone, so the lid, not the first metre of wall over
        // the box, takes the light (70 section 2: the ceiling the brightest surface; review SP1-F8, SP2-F17, KIT-F1: the
        // straight-up 150-degree boxes bloomed the wall and left the lid darker than the walls). SP6's dome boxes aim their own.
        if (aim.sqrMagnitude < 1e-6f) aim = Vector3.up * Mathf.Cos(30f * Mathf.Deg2Rad) + outward * Mathf.Sin(30f * Mathf.Deg2Rad);
        var l = Lamp("metal-halide wall box", LightType.Spot, pos + outward * .14f, Vector3.up, spot, inner, intensity, range, new Color(.90f, .97f, 1f), view,
            new Vector3(.5f, .28f, .22f), FrontRoomsPoolMaterials.Steel, new Color(1.6f, 1.7f, 1.75f));
        // The box sits on the wall: turn the fixture so its back is on the wall (the light still aims up, or along aim).
        if (l != null)
        {
            l.transform.parent.localRotation = Quaternion.LookRotation(Vector3.up, -outward);
            if (aim.sqrMagnitude > 1e-6f)
            {
                l.transform.localPosition = new Vector3(0f, -.16f, .05f);   // 0.3 m off the wall (the box's local y is into the wall)
                l.transform.rotation = (Root != null ? Root.rotation : Quaternion.identity) * Quaternion.LookRotation(aim.normalized, Vector3.up);
            }
        }
        return l;
    }

    /// <summary>A soft down-fill under a lid (pool halls), aimed down.</summary>
    public Light DownFill(Vector3 pos, float intensity = 3f, float range = 12f, bool view = true)
    {
        // K2 (NEW in a pool district): the bounce of the lit lid keeps its light and loses its body; PlaceLampKits binds it to its
        // box (it follows the box's lamp), or puts ZL_FLUSH on the lid where no box shares its lid
        if (poolLamps && FrontRoomsLampKit.Available(FrontRoomsLampKit.Family.WallBox))
        {
            var k = Lamp("down-fill", LightType.Spot, pos, Vector3.down, 150f, 110f, intensity, range, new Color(.92f, .98f, 1f), view, Vector3.zero, null, Color.black);
            if (k != null) lampRequests.Add(new LampRequest { kind = LampKind.Fill, light = k, pos = pos });
            return k;
        }
        return Lamp("down-fill", LightType.Spot, pos, Vector3.down, 150f, 110f, intensity, range, new Color(.92f, .98f, 1f), view, new Vector3(.45f, .45f, .12f), FrontRoomsPoolMaterials.Steel, new Color(1.4f, 1.5f, 1.55f));
    }

    /// <summary>An underwater lamp on a basin wall, facing <paramref name="into"/>.</summary>
    public Light Underwater(Vector3 pos, Vector3 into, float intensity = 2.2f, float range = 6f, bool view = true)
    {
        // K2 (NEW in a pool district): the same light (its object keeps the name the water preview looks for), no stand-in body;
        // P2-A or ZL_NICHE_S goes on the wall in PlaceLampKits, by the water at the wall's foot
        if (poolLamps && FrontRoomsLampKit.Available(FrontRoomsLampKit.Family.Niche))
        {
            var k = Lamp("underwater", LightType.Spot, pos, into, 120f, 60f, intensity, range, new Color(.75f, .95f, 1f), view, Vector3.zero, null, Color.black);
            if (k != null) lampRequests.Add(new LampRequest { kind = LampKind.Niche, light = k, pos = pos, dir = into });
            return k;
        }
        // A round lens in its square niche plate, cyan-shifted and below clipping, so it reads as a lamp under the water rather
        // than a white card (review SP6-F11).
        var l = Lamp("underwater", LightType.Spot, pos, into, 120f, 60f, intensity, range, new Color(.75f, .95f, 1f), view, new Vector3(.3f, .3f, .06f), FrontRoomsPoolMaterials.Steel, new Color(.9f, 1.6f, 1.7f));
        var lens = l != null ? l.transform.parent.Find("Lens") : null;
        if (lens != null)
        {
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "Lens (round)";
            UnityEngine.Object.Destroy(disc.GetComponent<Collider>());
            disc.transform.SetParent(lens.parent, false);
            disc.transform.localPosition = new Vector3(0f, 0f, .004f);
            disc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            disc.transform.localScale = new Vector3(.24f, .003f, .24f);
            var dr = disc.GetComponent<MeshRenderer>();
            dr.sharedMaterial = lens.GetComponent<MeshRenderer>().sharedMaterial;
            dr.shadowCastingMode = ShadowCastingMode.Off;
            UnityEngine.Object.Destroy(lens.gameObject);
        }
        return l;
    }

    // ---------------------------------------------------------------- K2: the zone lamps line's pool kits (structures G2-a)
    //
    // 游戏视觉's structures_assets G2-a (Red 2026-11-07 21:54 and 21:58: image 1's pools lit by image 2's lamps; research/
    // structures_assets/60_build.md "G2-a"). NEW in SP1, SP2 and SP6 (PoolLamps): WallBox, DownFill and Underwater make the
    // lights they always made, in the same order (the budget's pattern and the TODAY / NEW comparison unchanged), without the
    // stand-in bodies, and queue them; after the district's last Finish, when its shell has colliders, PlaceLampKits puts the
    // production kits on them (FrontRoomsStructuresPoolLamps: P1-A on the wall face, P2-A or ZL_NICHE_S by the water at the
    // wall's foot, each bounce bound to its box or ZL_FLUSH on its lid) and hands them to the look (lens, glow, the
    // metal-halide drive, never a shadow). A kit missing: that call builds today's stand-in. TODAY: nothing here runs.

    enum LampKind { Box, Fill, Niche }

    sealed class LampRequest
    {
        public LampKind kind;
        public Light light;
        public Vector3 pos, dir, aim;
        public bool small;
    }

    readonly List<LampRequest> lampRequests = new List<LampRequest>();
    readonly List<FrontRoomsStructuresPoolLamps.WaterRect> waterRects = new List<FrontRoomsStructuresPoolLamps.WaterRect>();
    bool poolLamps;

    /// <summary>K2: set by SP1, SP2 and SP6 at the start of their build; true only in NEW.</summary>
    public bool PoolLamps
    {
        get { return poolLamps; }
        set { poolLamps = value && isNew; }
    }

    /// <summary>
    /// K2 (NEW in a pool district): an extra ZL_NICHE_S where a stair or a weir goes into deep enough water (the zone lamps
    /// line's rule: one on a stair's cheek where the water there is 0.60 m or more; one in a weir's receiving basin under the
    /// fall, aimed up at the curtain along <paramref name="aim"/>). A lamp outside the budget's shadow pattern (later lamps
    /// keep their turns) that never shadows; its object carries the underwater lamps' name, so the water preview takes it as a
    /// wet niche. Null in TODAY or when the kit is missing.
    /// </summary>
    public Light ExtraNiche(Vector3 pos, Vector3 into, float intensity, float range, Vector3 aim = default(Vector3))
    {
        if (!poolLamps || !FrontRoomsLampKit.Available(FrontRoomsLampKit.Family.NicheSmall)) return null;
        var look = Host != null ? Host.Look : null;
        if (look != null) look.AddingOutside = true;
        Light k;
        try { k = Lamp("underwater", LightType.Spot, pos, into, 120f, 60f, intensity, range, new Color(.75f, .95f, 1f), false, Vector3.zero, null, Color.black); }
        finally { if (look != null) look.AddingOutside = false; }
        if (k != null) lampRequests.Add(new LampRequest { kind = LampKind.Niche, light = k, pos = pos, dir = into, aim = aim, small = true });
        return k;
    }

    // a lamp belongs to the pool room it hangs in, never to the one over it (rooms reach 3 m under their floor for the water)
    const float RoomBelow = .25f;

    int Seed(Vector3 p, int salt) => District.Key * 7919 ^ Mathf.RoundToInt(p.x * 10f) * 73856093 ^ Mathf.RoundToInt(p.y * 10f) * 19349663
        ^ Mathf.RoundToInt(p.z * 10f) * 83492791 ^ salt * 1013904223;

    static string V(Vector3 v) => v.x.ToString("0.00") + "," + v.y.ToString("0.00") + "," + v.z.ToString("0.00");

    /// <summary>
    /// K2: the pool kits on the queued lights, once, after the district's last Finish (its colliders exist): the wall boxes
    /// first (each bounce looks for its box), then the niches, then the bounces. Logs what it placed and what it left out.
    /// </summary>
    public void PlaceLampKits()
    {
        if (!poolLamps || lampRequests.Count == 0 || Root == null) return;
        Physics.SyncTransforms();
        FrontRoomsStructuresPoolLamps.Scope = Root;
        try { PlaceQueued(); }
        finally { FrontRoomsStructuresPoolLamps.Scope = null; }
    }

    void PlaceQueued()
    {
        var look = Host != null ? Host.Look : null;
        var phone = FrontRoomsLampKit.PhoneTier;
        var boxes = new List<FrontRoomsStructuresPoolLamps.Box>();
        int patches = 0, deep = 0, small = 0, follow = 0, flush = 0, bare = 0, left = 0;
        var notes = new System.Text.StringBuilder();
        string note;
        var boxRequests = new List<LampRequest>();
        foreach (var r in lampRequests)
        {
            if (r.kind != LampKind.Box || r.light == null) continue;
            var aim = r.aim.sqrMagnitude > 1e-6f ? Root.TransformDirection(r.aim) : Vector3.zero;
            var box = FrontRoomsStructuresPoolLamps.PlaceWallBox(r.light.transform.parent, r.light, Root.TransformPoint(r.pos), Root.TransformDirection(r.dir), aim, phone, out note);
            if (box == null) { left++; notes.Append(" | box ").Append(V(r.pos)).Append(": ").Append(note); continue; }
            boxes.Add(box);
            boxRequests.Add(r);
            var patch = box.glow != null && box.glow.gameObject.activeSelf ? box.glow : null;
            if (patch != null) patches++;
            if (look != null) look.AttachLens(r.light, box.lens, FrontRoomsStructuresPoolLamps.BoxLensDrive, patch);
        }
        // the temperaments (ask 5 B) where a pool room has three boxes or more; a room's only one or two stay steady, so no hall
        // is left with a failing or dead box for its light (SP6's dome hall: a failing south box made its B2 deck flash, LW0)
        if (look != null)
            for (var i = 0; i < boxes.Count; i++)
            {
                Rect room, other;
                float y0, y1;
                var mates = 0;
                if (look.PoolRoomAt(District, boxes[i].face + boxes[i].outward * .5f, out room, out y0, out y1, RoomBelow))
                    foreach (var b in boxes)
                        if (look.PoolRoomAt(District, b.face + b.outward * .5f, out other, out y0, out y1, RoomBelow) && other == room) mates++;
                var r = boxRequests[i];
                look.SetHid(r.light, Seed(r.pos, 1), FrontRoomsStructuresPoolLamps.Drift(Root.TransformPoint(r.pos)), mates > 0 && mates < 3);
            }
        foreach (var r in lampRequests)
        {
            if (r.kind != LampKind.Niche || r.light == null) continue;
            var n = FrontRoomsStructuresPoolLamps.PlaceNiche(r.light.transform.parent, r.light, Root.TransformPoint(r.pos), Root.TransformDirection(r.dir), waterRects, phone, r.small, out note);
            if (n == null)
            {
                // no wall, no water or too shallow for either niche: a light under the water with no lamp is put out
                left++;
                notes.Append(" | niche ").Append(V(r.pos)).Append(": ").Append(note);
                if (look != null) look.Scale(r.light, 0f);
                continue;
            }
            if (n.Small) small++; else deep++;
            if (r.aim.sqrMagnitude > 1e-6f) r.light.transform.rotation = Quaternion.LookRotation(Root.TransformDirection(r.aim).normalized, Root.TransformDirection(r.dir));
            if (look != null)
            {
                look.Retune(r.light);
                look.AttachLens(r.light, n.lens, FrontRoomsStructuresPoolLamps.NicheLensDrive(r.light.color));
                look.SetNiche(r.light, Seed(r.pos, 2));
            }
        }
        foreach (var r in lampRequests)
        {
            if (r.kind != LampKind.Fill || r.light == null) continue;
            var at = Root.TransformPoint(r.pos);
            FrontRoomsStructuresPoolLamps.Box best = null;
            var bestD = float.MaxValue;
            // the same pool room as the district gave it (a stair core's landing is not the hall's lid, though its door looks in)
            var fillRoom = default(Rect);
            float f0, f1;
            var fillHas = look != null && look.PoolRoomAt(District, at, out fillRoom, out f0, out f1, RoomBelow);
            foreach (var b in boxes)
            {
                float d;
                if (!FrontRoomsStructuresPoolLamps.Bind(at, b, out d) || d >= bestD) continue;
                Rect boxRoom;
                if (fillHas && look.PoolRoomAt(District, b.face + b.outward * .5f, out boxRoom, out f0, out f1, RoomBelow) && boxRoom != fillRoom) continue;
                best = b;
                bestD = d;
            }
            if (best != null)
            {
                follow++;
                if (look != null) look.Follow(r.light, best.light);
                continue;
            }
            var lens = FrontRoomsStructuresPoolLamps.PlaceFlush(r.light.transform.parent, at, phone, out note);
            if (lens != null)
            {
                flush++;
                if (look != null) look.AttachLens(r.light, lens, FrontRoomsStructuresPoolLamps.FlushLensDrive(r.light.color));
            }
            else
            {
                bare++;
                notes.Append(" | bounce ").Append(V(r.pos)).Append(": ").Append(note);
                if (look != null) look.NoShadow(r.light);
            }
        }
        Debug.Log(FrontRoomsStructures.Log + District.Code + " pool lamps (K2, structures G2-a): " + boxes.Count + " P1-A wall boxes (" + patches + " lit-ceiling patches under a flat lid), "
            + deep + " P2-A niches, " + small + " ZL_NICHE_S, " + follow + " bounces following their box, " + flush + " ZL_FLUSH, " + bare + " bounces with neither"
            + (left > 0 ? "; left out " + left : "") + (look != null ? "; " + look.RigLine(District.Key) : "") + notes);
        lampRequests.Clear();
    }

    // ---------------------------------------------------------------- props, signs, stops, blockers, marks

    /// <summary>A plain stand-in box (render and collider), named for the hierarchy ("STAND-IN ...").</summary>
    public GameObject StandIn(string name, Vector3 centre, Vector3 size, Material m, float yaw = 0f, bool collide = true)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "STAND-IN " + name;
        go.transform.SetParent(PartRoot, false);
        go.transform.localPosition = centre;
        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        go.transform.localScale = size;
        var r = go.GetComponent<MeshRenderer>();
        r.sharedMaterial = m != null ? m : FrontRoomsPoolMaterials.Lit("stand-in grey", new Color(.5f, .5f, .5f));
        if (isNew) r.sharedMaterial = FrontRoomsStructuresAssets.Real(r.sharedMaterial);
        if (!collide) UnityEngine.Object.Destroy(go.GetComponent<Collider>());
        return go;
    }

    /// <summary>One of the game's kits by name (FrontRoomsKitLibrary), else a stand-in box of the given size (logged once).</summary>
    public GameObject Kit(string asset, Vector3 floor, float yaw, Vector3 standInSize, Material standIn = null, bool colliders = true)
    {
        GameObject go = null;
        try { go = FrontRoomsKitLibrary.Spawn(asset, PartRoot, floor, yaw, null, colliders, null, false); }
        catch (Exception e) { FrontRoomsPoolMaterials.LogOnce("kit " + asset, "kit " + asset + " failed: " + e.Message); }
        if (go != null) return go;
        FrontRoomsPoolMaterials.LogOnce("kit missing " + asset, "kit " + asset + " is missing: a stand-in box is used.");
        return StandIn(asset + " (kit missing)", floor + Vector3.up * standInSize.y * .5f, standInSize, standIn, yaw, colliders);
    }

    /// <summary>A placard for Red (outside the fiction) at a centre point (district metres) facing a viewer point; floor under it.</summary>
    public GameObject Sign(Vector3 centre, Vector3 viewer, string title, string body, float floorY)
    {
        if (Root == null) return null;
        return FrontRoomsStructuresSigns.Placard(Root, Root.TransformPoint(centre), Root.TransformPoint(viewer), title, body, Root.position.y + floorY);
    }

    /// <summary>A stop at a floor point (district metres) looking at a point; its pitch is clamped to <paramref name="maxPitch"/>
    /// (30 degrees, or more for a look down into a void or up a shaft).</summary>
    public void Stop(string name, Vector3 floor, Vector3 lookAt, float maxPitch = 30f)
    {
        District.Stops.Add(FrontRoomsStructuresStop.Facing(name, world + floor, world + lookAt, maxPitch));
    }

    /// <summary>The static Relay stand-in at a floor point facing a point (district metres).</summary>
    public GameObject Relay(Vector3 floor, Vector3 facing)
    {
        if (Root == null) return null;
        return FrontRoomsStructuresSigns.RelayStandIn(PartRoot, Root.TransformPoint(floor), Root.TransformPoint(facing));
    }

    /// <summary>A blocker that exists only in the Relay-body test (C): spirals, the tower, the flume and the tub are closed to it.</summary>
    public void RelayBlocker(string name, Vector3 min, Vector3 max)
    {
        var go = new GameObject("Relay-only blocker · " + name);
        go.transform.SetParent(Root, false);
        go.transform.localPosition = (min + max) * .5f;
        var box = go.AddComponent<BoxCollider>();
        box.size = Vector3.Max(max - min, Vector3.one * .02f);
        go.SetActive(false);
        blockers.Add(go);
    }

    public void SetRelay(bool on)
    {
        foreach (var b in blockers) if (b != null) b.SetActive(on);
    }

    /// <summary>A metric mark (M): a short label in the body font at a point, facing a direction (district metres).</summary>
    public void Mark(Vector3 at, Vector3 facing, string text, float size = .05f)
    {
        var go = new GameObject("Mark · " + text);
        go.transform.SetParent(Root, false);
        go.transform.localPosition = at;
        facing.y = 0f;
        go.transform.localRotation = Quaternion.LookRotation(facing.sqrMagnitude > 1e-6f ? -facing.normalized : Vector3.forward);
        var tm = go.AddComponent<TextMesh>();
        var font = FrontRoomsStructuresSigns.BodyFontAsset;
        if (font != null) tm.font = font;
        tm.fontSize = 48;
        tm.characterSize = size * 10f / 48f;
        tm.anchor = TextAnchor.LowerCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = new Color(.957f, .875f, .231f);
        tm.text = text;
        var r = go.GetComponent<MeshRenderer>();
        if (r != null)
        {
            var depth = FrontRoomsStructuresSigns.DepthText(font);
            if (depth != null) r.sharedMaterial = depth;
            r.shadowCastingMode = ShadowCastingMode.Off;
        }
        // A thin plumb line under the label.
        var line = GameObject.CreatePrimitive(PrimitiveType.Cube);
        line.name = "Line";
        UnityEngine.Object.Destroy(line.GetComponent<Collider>());
        line.transform.SetParent(go.transform, false);
        line.transform.localPosition = new Vector3(0f, -.25f, 0f);
        line.transform.localScale = new Vector3(.01f, .5f, .01f);
        line.GetComponent<MeshRenderer>().sharedMaterial = FrontRoomsPoolMaterials.Glow("mark line", new Color(.957f, .875f, .231f));
        go.SetActive(false);
        marks.Add(go);
    }

    public void ShowMarks(bool on)
    {
        foreach (var m in marks) if (m != null) m.SetActive(on);
    }

    // ================================================================ NEW: production pieces (contract K1)
    //
    // 游戏视觉's structures_assets (research/structures_assets/30_plan.md rows X-01..X-03, K-01, K-02; 20_research.md Part B F1-F8):
    // with FrontRoomsStructuresAssets.NewFor(this district) every box and prism is bevelled by material where nothing touches it
    // (FrontRoomsStructuresMesh.Bevels), and the guards, stairs and level edges are built as the period built them, from the
    // profiles in FrontRoomsStructureProfiles and 游戏视觉's kits where they are in the build. Render only: every collider below
    // is today's, call for call, so the walk test sees the same scene. TODAY (NEW off) never reaches this region.

    readonly bool isNew;

    /// <summary>NEW for this district's build (scene 4's TODAY / NEW key; SP7 and the pool route stay TODAY).</summary>
    public bool New => isNew;

    readonly List<Vector3> sweepV = new List<Vector3>(), sweepN = new List<Vector3>();
    readonly List<Vector2> sweepUV = new List<Vector2>();
    readonly List<int> sweepT = new List<int>();
    // 24 and 20 segments: a facet's sagitta 0.18 and 0.21 mm, under a pixel at the 0.35 m hero (12 and 10 showed facets on the
    // silhouettes; G1 review FIX 4). Builder meshes have no LOD: a few tens of triangles a run.
    static readonly FrontRoomsStructureProfiles.Section RailTube = FrontRoomsStructureProfiles.Tube(.0211f, 24);
    static readonly FrontRoomsStructureProfiles.Section PipeTube = FrontRoomsStructureProfiles.Tube(.0167f, 20);
    static readonly FrontRoomsStructureProfiles.Section Shoe = FrontRoomsStructureProfiles.GlassShoe();
    static readonly FrontRoomsStructureProfiles.Section ShoeGasketL = FrontRoomsStructureProfiles.ShoeGasket(-1f);
    static readonly FrontRoomsStructureProfiles.Section ShoeGasketR = FrontRoomsStructureProfiles.ShoeGasket(1f);
    static readonly FrontRoomsStructureProfiles.Section CapRailSection = FrontRoomsStructureProfiles.CapRail();
    static readonly FrontRoomsStructureProfiles.Section ChannelSection = FrontRoomsStructureProfiles.Channel();
    /// <summary>Guard and handrail tube (1 1/4 in NPS, 42.2 mm OD) and the 1 in pipe (33.4 mm): G1-b's rail kits take these.</summary>
    public const float RailR = .0211f, PipeR = .0167f;

    enum Family { Level0, Office, Pool, Run }

    // A piece's family from its finish, else from the district (pool halls SP1, SP2, SP6; the Office SP4; Run! SP5).
    Family FamilyOf(Material m)
    {
        var n = m != null && m.name != null ? m.name : "";
        if (n.StartsWith("Prop_PoolTile_", StringComparison.Ordinal) || n.StartsWith("Structures / tile", StringComparison.Ordinal) || (m != null && m.IsKeywordEnabled("_FR_TILE"))) return Family.Pool;
        if (n.StartsWith("Office_", StringComparison.Ordinal)) return Family.Office;
        if (n.StartsWith("RunB_", StringComparison.Ordinal) || n.StartsWith("Run_", StringComparison.Ordinal) || n.StartsWith("Stair_Block", StringComparison.Ordinal)) return Family.Run;
        if (n.StartsWith("L0_", StringComparison.Ordinal) || n.StartsWith("Exit_", StringComparison.Ordinal)) return Family.Level0;
        switch (District.Key)
        {
            case 1: case 2: case 6: return Family.Pool;
            case 4: return Family.Office;
            case 5: return Family.Run;
            default: return Family.Level0;
        }
    }

    static Material Surf(string name, Material fallback)
    {
        var m = FrontRoomsSurfaces.TryGet(name);
        return m != null ? m : fallback;
    }

    // K2 (structures G2-c): whether a material is the pool's tile for FrontRoomsStructureCurveTile, and which: the white field
    // (Prop_PoolTile_FieldMesh, at any water line, or the LS stand-in) takes the tile by radius (own null); another pool tile (the
    // aqua course, a mosaic) keeps its own material and pitch (own ""); anything else is not tile (today's geometry).
    static bool CurveTile(Material m, out string own, out float ownPitch)
    {
        own = null;
        ownPitch = .15f;
        if (m == null) return false;
        var n = m.name ?? "";
        if (n.StartsWith("Prop_PoolTile_FieldMesh", StringComparison.Ordinal) || n.StartsWith("Structures / pool tile", StringComparison.Ordinal)) return true;
        if (!n.StartsWith("Prop_PoolTile_", StringComparison.Ordinal) && !m.IsKeywordEnabled("_FR_TILE")) return false;
        own = "";
        if (m.HasProperty("_TileSize") && m.HasProperty("_TileGrid"))
        {
            var size = m.GetVector("_TileSize");
            var grid = m.GetVector("_TileGrid");
            if (size.x > 1e-3f && grid.x >= 1f) ownPitch = size.x / grid.x;
        }
        return true;
    }

    // K2 (structures G2-c): a curve build's lists into the shell, each tile kind in its material (at the caller's water line when
    // the caller's tile has one); key "" is the caller's own material.
    public void AppendCurve(FrontRoomsStructureCurveTile.Out o, Material like)
    {
        foreach (var kv in o.Kinds)
        {
            var l = kv.Value;
            if (l.t.Count == 0) continue;
            var mat = kv.Key.Length == 0 ? like : CurveMat(kv.Key, like);
            if (mat == null) continue;
            Mesh.Append(l.v, l.n, l.uv, l.t, mat, false);
        }
    }

    static readonly int WaterYId = Shader.PropertyToID("_WaterY");

    static Material CurveMat(string kind, Material like)
    {
        if (like != null && like.name != null && like.name.IndexOf("(water ", StringComparison.Ordinal) >= 0 && like.HasProperty(WaterYId))
        {
            var w = FrontRoomsPoolTile.ForWater(kind, like.GetFloat(WaterYId));
            if (w != null) return w;
        }
        var m = FrontRoomsPoolTile.Get(kind);
        return m != null ? m : like;
    }

    // Rail metal by family (G1-b's kit variants): stainless in the pool, putty steel in the Office, painted steel elsewhere.
    Material RailMetal(Family f)
    {
        switch (f)
        {
            case Family.Pool: return FrontRoomsPoolMaterials.Steel;
            case Family.Office: return BandMaterial();
            default: return RailPaint();
        }
    }

    static Material railPaint;

    // Painted steel at the hand's scale (G1 review FIX 4): Prop_RailEnamel (P3: Painted_Metal's own maps at 0.30 m a tile, an
    // enamel gloss; the painted rail kits wear it too) where it is in the build, else the scene's Painted_Metal as before.
    static Material RailPaint()
    {
        if (railPaint != null) return railPaint;
        railPaint = FrontRoomsSurfaces.TryGet("Prop_RailEnamel");
        return railPaint != null ? railPaint : PostMaterial();
    }

    static string KitName(string baseName, Family f)
    {
        return f == Family.Pool ? baseName : f == Family.Office ? baseName + "_Putty" : baseName + "_Painted";
    }

    // The structures trim sheet (P3): one material, metal per texel; until it lands the band steel stands in.
    static Material Trim()
    {
        var m = FrontRoomsStructuresAssets.TrimSheet();
        return m != null ? m : BandMaterial();
    }

    // A cap's own edge: a 6 mm round on timber, the tile's bullnose on tile, else by material.
    static FrontRoomsStructureBevels.Spec? CapEdge(Material cap)
    {
        var n = cap != null && cap.name != null ? cap.name : "";
        if (n.StartsWith("Prop_Wood", StringComparison.Ordinal)) return new FrontRoomsStructureBevels.Spec(.006f, 2, false);
        if (cap != null && (cap.IsKeywordEnabled("_FR_TILE") || n.StartsWith("Prop_PoolTile_", StringComparison.Ordinal))) return FrontRoomsStructureBevels.Tile;
        return null;
    }

    // Red's ask 4: a wall's lot, from its line (its direction and its offset), the same for every piece of one wall.
    static float WallLot(Vector3 t, float offset)
    {
        var h = Mathf.RoundToInt(Mathf.Abs(t.x) * 64f) * 73856093 ^ Mathf.RoundToInt(Mathf.Abs(t.z) * 64f) * 19349663 ^ Mathf.RoundToInt(offset * 20f) * 83492791;
        h = (h ^ (h >> 13)) * 1274126177;
        return .001f + ((h >> 8) & 0xFFFF) / 65536f * .998f;
    }

    void SweepTo(Material m, IList<Vector3> path, FrontRoomsStructureProfiles.Section s, FrontRoomsStructureProfiles.SweepOptions o)
    {
        SweepTo(m, path, s, o, Vector3.up);
    }

    // up: the frame hint; a path in a vertical plane passes its plane's normal, so every joint stays a true mitre
    void SweepTo(Material m, IList<Vector3> path, FrontRoomsStructureProfiles.Section s, FrontRoomsStructureProfiles.SweepOptions o, Vector3 up)
    {
        if (m == null || path == null || path.Count < 2) return;
        sweepV.Clear(); sweepN.Clear(); sweepUV.Clear(); sweepT.Clear();
        FrontRoomsStructureProfiles.Sweep(sweepV, sweepN, sweepUV, sweepT, path, up, s, o);
        Mesh.Append(sweepV, sweepN, sweepUV, sweepT, m, false);
    }

    void Tube(Vector3 from, Vector3 to, FrontRoomsStructureProfiles.Section tube, Material m, bool caps)
    {
        if ((to - from).sqrMagnitude < 1e-6f) return;
        var o = FrontRoomsStructureProfiles.SweepOptions.Default;
        o.capStart = o.capEnd = caps;
        o.u0 = FrontRoomsStructureProfiles.WorldU(W(from), to - from);
        SweepTo(m, new[] { from, to }, tube, o);
    }

    // A straight run of a plumb section (shoes, cap rails, nosings) from a to b, u along from world 0.
    void Lay(Vector3 a, Vector3 b, FrontRoomsStructureProfiles.Section s, Material m, Vector2 strip, bool caps = true)
    {
        if ((b - a).sqrMagnitude < 1e-6f) return;
        var o = FrontRoomsStructureProfiles.SweepOptions.Default;
        o.plumb = true;
        o.capStart = o.capEnd = caps;
        o.vRange = strip;
        o.u0 = FrontRoomsStructureProfiles.WorldU(W(a), b - a) * (strip.sqrMagnitude > 0f ? 1f / FrontRoomsStructureProfiles.TrimMetres : 1f);
        if (strip.sqrMagnitude > 0f) o.uScale = 1f / FrontRoomsStructureProfiles.TrimMetres;
        SweepTo(m, new[] { a, b }, s, o);
    }

    // One of 游戏视觉's kits (render only) with its local x along xAxis and y up; nothing when it is not in this build.
    GameObject Fitting(string name, Vector3 at, Vector3 xAxis)
    {
        if (string.IsNullOrEmpty(name) || !FrontRoomsKitLibrary.Has(name)) return null;
        xAxis.y = 0f;
        if (xAxis.sqrMagnitude < 1e-8f) xAxis = Vector3.right;
        xAxis.Normalize();
        var rot = Quaternion.LookRotation(Vector3.Cross(xAxis, Vector3.up), Vector3.up);
        try { return FrontRoomsKitLibrary.Spawn(name, PartRoot, at, rot, null, false, null, false); }
        catch (Exception e) { FrontRoomsPoolMaterials.LogOnce("fitting " + name, "fitting " + name + " failed: " + e.Message); return null; }
    }

    // Kit.Box's collider alone, exactly as Box makes it.
    void BoxCollider(Vector3 min, Vector3 max)
    {
        var lo = Vector3.Min(min, max);
        var hi = Vector3.Max(min, max);
        var size = hi - lo;
        if (size.x <= 1e-4f || size.y <= 1e-4f || size.z <= 1e-4f) return;
        Mesh.Collider((lo + hi) * .5f, hi - lo);
    }

    // ---------------------------------------------------------------- NEW guards (Part B F6-F8; 1988 UBC; ask 3)

    bool GuardNew(Vector3 a, Vector3 b, Vector3 t, float len, float floorY, float top, float height, GuardStyle style, Material wall, Material cap)
    {
        var fam = FamilyOf(wall);
        switch (style)
        {
            case GuardStyle.HalfWall:
            {
                // today's wall and cap, bevelled; the cap's own round (6 mm on oak) on its top, underside and returned ends
                Prism(a, b, floorY, top - .04f, .16f, wall != null ? wall : FrontRoomsPoolMaterials.Lit("guard wall", new Color(.7f, .68f, .6f)), true);
                var keep = Mesh.EdgeOverride;
                var c = cap != null ? cap : CapMaterial();
                Mesh.EdgeOverride = CapEdge(c);
                Prism(a - t * .02f, b + t * .02f, top - .04f, top, .2f, c, false);
                Mesh.EdgeOverride = keep;
                return true;
            }
            case GuardStyle.TileWall:
            {
                // K2 (structures G2-c, C8): in the pool a Cap course on the wall (A.2.4, A.3.3); elsewhere today's prism, whose
                // bevels give its top and free ends the tile's bullnose
                var w = wall != null ? wall : FrontRoomsPoolMaterials.Tile;
                if (!CurveTile(w, out var own, out var op)) return false;
                TileGuard(a, b, t, len, floorY, top, w);
                return true;
            }
            case GuardStyle.Glass:
                GlassGuard(a, b, t, len, floorY, top, height, cap, fam);
                return true;
            case GuardStyle.Rail:
                RailGuard(a, b, t, len, floorY, top, height, fam);
                return true;
            case GuardStyle.Mesh:
                MeshGuard(a, b, t, len, floorY, top, height, fam);
                return true;
        }
        return false;
    }

    // Pipe rail on flanged posts at today's places (Red, 19:5x via the owners: rail positions as built): a 42.2 mm top rail on
    // post-top saddles, and on public floors (ask 3 REC B) a 33.4 mm bottom rail with 1/2 in square pickets at 140 mm (a 6 in
    // sphere does not pass, the period rule); in service zones and on ask 3 A the two-line rail made real (a 33.4 mm mid rail).
    void RailGuard(Vector3 a, Vector3 b, Vector3 t, float len, float floorY, float top, float height, Family fam)
    {
        var metal = RailMetal(fam);
        var railY = top - RailR;
        var count = Mathf.Max(1, Mathf.CeilToInt(len / 1.2f));
        var saddle = KitName("Kit_RailBracket_PostTop", fam);
        var hasSaddle = FrontRoomsKitLibrary.Has(saddle);
        var postTop = hasSaddle ? railY - .040f : railY;
        for (var k = 0; k <= count; k++)
        {
            var q = a + t * (len * k / count);
            q.y = 0f;
            // K2 (structures G2-c, C12): where guards share a corner, one post (and its flange and saddle) stands there
            if (SharedPosts && !postsAt.Add(PostKey(q))) continue;
            Tube(new Vector3(q.x, floorY, q.z), new Vector3(q.x, postTop, q.z), RailTube, metal, false);
            Fitting(KitName("Kit_RailPost_BaseFlange", fam), new Vector3(q.x, floorY, q.z), t);
            if (hasSaddle) Fitting(saddle, new Vector3(q.x, postTop, q.z), t);
        }
        Tube(a + Vector3.up * railY, b + Vector3.up * railY, RailTube, metal, true);
        if (!SharedPosts)
        {
            Fitting(KitName("Kit_RailEnd_Cap", fam), a + Vector3.up * railY, -t);
            Fitting(KitName("Kit_RailEnd_Cap", fam), b + Vector3.up * railY, t);
        }
        var pickets = FrontRoomsStructuresAssets.Rails == FrontRoomsStructuresAssets.PublicRail.Pickets && fam != Family.Run && District.Key != 5 && !ServiceRails;
        if (pickets)
        {
            var by = floorY + .10f;
            BayRail(a + Vector3.up * by, b + Vector3.up * by, count, metal);
            // the rails are sweeps: they count as touching, so a picket's ends inside them stay square (28 triangles, not 44)
            Mesh.Occluder((a + b) * .5f + Vector3.up * railY, new Vector3(RailR * 1.4f, RailR * 1.4f, len), Quaternion.LookRotation(t));
            Mesh.Occluder((a + b) * .5f + Vector3.up * by, new Vector3(PipeR * 1.4f, PipeR * 1.4f, len), Quaternion.LookRotation(t));
            Pickets(a, b, t, len, by, railY, count, metal);
        }
        else BayRail(a + Vector3.up * (floorY + .53f), b + Vector3.up * (floorY + .53f), count, metal);
        Mesh.Collider((a + b) * .5f + Vector3.up * (floorY + top) * .5f, new Vector3(.05f, height, len), Quaternion.LookRotation(t));
    }

    readonly List<Vector3> weldV = new List<Vector3>(), weldN = new List<Vector3>();
    readonly List<Vector2> weldUV = new List<Vector2>();
    readonly List<int> weldT = new List<int>();

    // A 33.4 mm rail (the bottom rail, or the two-line rail's mid rail) bay by bay between its posts, as a fabricator builds it
    // (G1 review FIX 4: it used to run through every post): a..b on the posts' axes at the rail's height (level or raked), 'bays'
    // the bays between count + 1 posts. Each bay's pipe is coped to its posts: it ends just inside the post where its whole end
    // lies within the post, so the joint seen is the true saddle curve of the two tubes; a fillet weld runs round that curve.
    void BayRail(Vector3 a, Vector3 b, int bays, Material metal)
    {
        if (bays < 1 || (b - a).sqrMagnitude < 1e-6f) return;
        for (var k = 0; k < bays; k++)
        {
            var p0 = Vector3.Lerp(a, b, k / (float)bays);
            var p1 = Vector3.Lerp(a, b, (k + 1) / (float)bays);
            var dir = (p1 - p0).normalized;
            var inset = CopeDepth(dir) - .0005f;
            if (Vector3.Distance(p0, p1) <= 2f * inset + .01f) continue;
            Tube(p0 + dir * inset, p1 - dir * inset, PipeTube, metal, false);
            Weld(p0, dir, metal);
            Weld(p1, -dir, metal);
        }
    }

    // The pipe's frame on a vertical post: dir along the pipe (away from the post), u its upper side square to dir in the
    // vertical plane, n horizontal across; c the cosine of its slope.
    static void PipeFrame(Vector3 dir, out Vector3 u, out Vector3 n, out float c)
    {
        var h = new Vector3(dir.x, 0f, dir.z);
        c = Mathf.Max(h.magnitude, .2f);
        n = Vector3.Cross(Vector3.up, h.normalized).normalized;
        u = Vector3.Cross(dir, n).normalized;
        if (u.y < 0f) u = -u;
    }

    // How far along the pipe from the post's axis the pipe's surface at angle th meets the post's face (radius RailR).
    static float CopeAt(Vector3 dir, Vector3 u, float c, float th)
    {
        var uh = Vector3.Dot(new Vector3(u.x, 0f, u.z), new Vector3(dir.x, 0f, dir.z).normalized);
        var s = Mathf.Sin(th);
        return (Mathf.Sqrt(Mathf.Max(RailR * RailR - PipeR * PipeR * s * s, 0f)) - PipeR * Mathf.Cos(th) * uh) / c;
    }

    // The deepest the pipe's end may sit and still lie wholly inside the post (the least of CopeAt round the pipe).
    static float CopeDepth(Vector3 dir)
    {
        PipeFrame(dir, out var u, out var n, out var c);
        var m = float.MaxValue;
        for (var i = 0; i < 32; i++) m = Mathf.Min(m, CopeAt(dir, u, c, Mathf.PI * 2f * i / 32f));
        return m;
    }

    // A fillet weld round the saddle curve where the pipe (axis through 'post', the post's axis point at the pipe's height, running
    // along dir away from it) meets the post's face: a bead of 3 mm legs, one on each tube, crowned between them; 96 triangles.
    void Weld(Vector3 post, Vector3 dir, Material metal)
    {
        const int N = 24;
        const float Leg = .003f, Lift = .0003f;
        PipeFrame(dir, out var u, out var n, out var c);
        weldV.Clear(); weldN.Clear(); weldUV.Clear(); weldT.Clear();
        var perim = 2f * Mathf.PI * PipeR;
        for (var i = 0; i <= N; i++)
        {
            var th = Mathf.PI * 2f * i / N;
            var np = Mathf.Cos(th) * u + Mathf.Sin(th) * n;
            var j = post + dir * CopeAt(dir, u, c, th) + np * PipeR;
            var nq = new Vector3(j.x - post.x, 0f, j.z - post.z).normalized;
            var wt = (np - Vector3.Dot(np, nq) * nq).normalized;
            var crown = (np + nq).normalized;
            var uu = perim * i / N;
            weldV.Add(j + dir * Leg + np * Lift); weldN.Add(np); weldUV.Add(new Vector2(uu, 0f));
            weldV.Add(j + crown * (Leg * .42f)); weldN.Add(crown); weldUV.Add(new Vector2(uu, Leg * .6f));
            weldV.Add(j + wt * Leg + nq * Lift); weldN.Add(nq); weldUV.Add(new Vector2(uu, Leg * 1.2f));
        }
        for (var i = 0; i < N; i++)
            for (var r = 0; r < 2; r++)
            {
                int qa = i * 3 + r, qb = (i + 1) * 3 + r, qc = (i + 1) * 3 + r + 1, qd = i * 3 + r + 1;
                var face = weldN[qa] + weldN[qb] + weldN[qc] + weldN[qd];
                // Unity's front face: Cross(b - a, d - a) along the outward normal
                if (Vector3.Dot(Vector3.Cross(weldV[qb] - weldV[qa], weldV[qd] - weldV[qa]), face) < 0f) { var sw = qb; qb = qd; qd = sw; }
                weldT.Add(qa); weldT.Add(qb); weldT.Add(qc);
                weldT.Add(qa); weldT.Add(qc); weldT.Add(qd);
            }
        Mesh.Append(weldV, weldN, weldUV, weldT, metal, false);
    }

    // 1/2 in square pickets every ~140 mm between two rails (y0 bottom rail axis, y1 top rail axis), none on a post.
    void Pickets(Vector3 a, Vector3 b, Vector3 t, float len, float y0, float y1, int posts, Material metal)
    {
        // G12 (K1 review, structures G3-b): bay by bay between the posts' faces, as many 1/2 in bars as keep every clear gap at
        // 6 in or less (the period's sphere) beside the posts too, evenly spread (it skipped a picket near a post: 157 mm gaps)
        const float Bar = .0127f, Gap = .1524f;
        var rot = Quaternion.LookRotation(t);
        var bays = Mathf.Max(1, posts);
        var bay = len / bays;
        var clear = bay - 2f * RailR;
        var n = Mathf.Max(1, Mathf.CeilToInt((clear - Gap) / (Gap + Bar)));
        for (var j = 0; j < bays; j++)
            for (var k = 0; k < n; k++)
            {
                var q = a + t * (j * bay + RailR + clear * (k + 1f) / (n + 1f));
                Mesh.Box(new Vector3(q.x, (y0 + y1) * .5f, q.z), new Vector3(Bar, y1 - y0, Bar), rot, metal, false);
            }
    }

    // 1988 UBC structural glass: 12 mm panes (<= 1.52 m, 6 mm gaps, polished edges) in a continuous aluminium base shoe with its
    // gaskets, an aluminium cap rail over them (or the half-wall's own timber cap where one is given), no posts.
    void GlassGuard(Vector3 a, Vector3 b, Vector3 t, float len, float floorY, float top, float height, Material cap, Family fam)
    {
        // K2 (structures G2-c, C14; FR-2): an end on a lower run's end steps back onto this run's own floor (render only: the
        // collider at the end of this method keeps the run's own line)
        var ca = a;
        var cb = b;
        foreach (var g in glassEnds)
        {
            if (g.w >= floorY - 1e-3f) continue;
            if (Flat(new Vector3(g.x, 0f, g.z) - a) < 1e-3f) a += t * GlassEndInset;
            else if (Flat(new Vector3(g.x, 0f, g.z) - b) < 1e-3f) b -= t * GlassEndInset;
        }
        glassEnds.Add(new Vector4(ca.x, 0f, ca.z, floorY));
        glassEnds.Add(new Vector4(cb.x, 0f, cb.z, floorY));
        len = Vector3.Distance(a, b);
        var trim = Trim();
        var strip = FrontRoomsStructureProfiles.StripV(fam == Family.Pool ? FrontRoomsStructureProfiles.Strip.StainlessBrushed : FrontRoomsStructureProfiles.Strip.ShoeAluminium);
        var fa = new Vector3(a.x, floorY, a.z);
        var fb = new Vector3(b.x, floorY, b.z);
        Lay(fa, fb, Shoe, trim, strip);
        var rubber = Surf("Prop_Rubber", null);
        if (rubber != null) { Lay(fa, fb, ShoeGasketL, rubber, Vector2.zero); Lay(fa, fb, ShoeGasketR, rubber, Vector2.zero); }
        var timber = cap != null && cap != FrontRoomsPoolMaterials.Tile;
        var glassTop = timber ? top - .05f : top - .038f + .018f;
        var panes = Mathf.Max(1, Mathf.CeilToInt(len / 1.52f));
        var pane = (len - (panes - 1) * .006f) / panes;
        for (var i = 0; i < panes; i++)
        {
            var s0 = i * (pane + .006f);
            var p0 = a + t * s0;
            var p1 = a + t * (s0 + pane);
            // the game's own pane (one scaled cube on Glass_Window: its edges and grime read); the old merged prism only without it
            if (GlassPane(p0, p1, floorY + .025f, floorY + .025f, glassTop - floorY - .025f, floorY))
                Mesh.Occluder((p0 + p1) * .5f + Vector3.up * ((floorY + .025f + glassTop) * .5f), new Vector3(.012f, glassTop - floorY - .025f, pane), Quaternion.LookRotation(t, Vector3.up));
            else Prism(p0, p1, floorY + .025f, glassTop, .012f, GlassMaterial(), false);
        }
        if (timber)
        {
            var keep = Mesh.EdgeOverride;
            Mesh.EdgeOverride = CapEdge(cap);
            Prism(a - t * .01f, b + t * .01f, top - .05f, top, .2f, cap, false);
            Mesh.EdgeOverride = keep;
        }
        else Lay(a + Vector3.up * (top - .038f), b + Vector3.up * (top - .038f), CapRailSection, trim, strip);
        Fitting(fam == Family.Pool ? "Kit_GlassShoe_EndCap_Stainless" : "Kit_GlassShoe_EndCap", fa, -t);
        Fitting(fam == Family.Pool ? "Kit_GlassShoe_EndCap_Stainless" : "Kit_GlassShoe_EndCap", fb, t);
        Mesh.Collider((ca + cb) * .5f + Vector3.up * (floorY + top) * .5f, new Vector3(.06f, height, Vector3.Distance(ca, cb)), Quaternion.LookRotation(t));
    }

    static Mesh paneCube;
    MaterialPropertyBlock paneBlock;
    static readonly int PaneFloorYId = Shader.PropertyToID("_FloorY");
    static readonly int PaneAlphaId = Shader.PropertyToID("_AlphaFace");
    static readonly int PaneDustId = Shader.PropertyToID("_DustFilm");
    /// <summary>A 12 mm guard pane against Glass_Window's 6 mm window: face-on alpha 0.15 (T 0.85, a 12 mm clear float pane's
    /// visible transmittance; the window's 0.11 is a 6 mm pane's T 0.89), and the even dust film a public guard carries (the
    /// window's is 0). Up to about 8 m the pane reads by its edges, its grime and this haze. Further out, face on against an evenly
    /// lit bright wall, clear glass shows only by its cap rail, shoe and joints, as real glass does without a reflection (the G1
    /// fix measured these two values at SP1 s02's upper guard, 15 m: 1.4 levels in 255); the traced reflection (GlassRT) carries
    /// it there. A pane dark enough to read there unaided would be tinted glass.</summary>
    const float GuardPaneAlpha = .15f, GuardPaneDust = .08f;

    // One guard pane built as the game builds its window panes (FrontRoomsGlassPane): a unit cube scaled to the pane on the shared
    // Glass_Window, so FrontRooms/Glass takes the pane frame from the object (the thinnest scaled axis is the normal, v up): the
    // four 12 mm polished edges draw as the green float-glass edge, and the grime lays out on each pane (dust above the shoe,
    // smears and prints at hand height under the cap rail, the lit haze that makes clear glass read in an evenly lit room). A
    // merged shell has no pane frame (TODAY's GlassMaterial copy turns edge and grime off), so its frameless pane was invisible
    // without RT (G1 review FIX 1). a, b: the pane's ends on plan; yA, yB: its bottom at a and at b (a raked pane rises between
    // them: a parallelogram, made by a parent's scale and turn and the cube's own turn, A = R(phi) S R(theta)); height: plumb.
    // Render only: no collider (the guard keeps its one), no shadow. False when Glass_Window is not in this build.
    bool GlassPane(Vector3 a, Vector3 b, float yA, float yB, float height, float floorY)
    {
        var window = FrontRoomsSurfaces.TryGet("Glass_Window");
        if (window == null) return false;
        a.y = 0f;
        b.y = 0f;
        var d = b - a;
        var len = d.magnitude;
        if (len < 1e-3f || height < 1e-3f) return true;
        var t = d / len;
        const float Thick = .012f;
        if (paneCube == null)
        {
            var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            paneCube = tmp.GetComponent<MeshFilter>().sharedMesh;
            UnityEngine.Object.DestroyImmediate(tmp);
        }
        var go = new GameObject("Glass pane");
        go.transform.SetParent(PartRoot, false);
        go.transform.localPosition = (a + b) * .5f + Vector3.up * ((yA + yB) * .5f + height * .5f);
        // the pane's own frame: x along the run, y up, z across (LookRotation's right = Cross(up, forward) = t)
        var frame = Quaternion.LookRotation(new Vector3(-t.z, 0f, t.x), Vector3.up);
        var cube = go;
        var rise = yB - yA;
        if (Mathf.Abs(rise) < 1e-4f)
        {
            go.transform.localRotation = frame;
            go.transform.localScale = new Vector3(len, height, Thick);
        }
        else
        {
            // the shear [[len, 0], [rise, height]] as R(phi) diag(sx, sy) R(theta) (closed-form 2 x 2 SVD; Rz(a) turns x toward y)
            float e = (len + height) * .5f, f = (len - height) * .5f, g = rise * .5f, h = rise * .5f;
            float q = Mathf.Sqrt(e * e + h * h), r = Mathf.Sqrt(f * f + g * g);
            float a1 = Mathf.Atan2(g, f), a2 = Mathf.Atan2(h, e);
            go.transform.localRotation = frame * Quaternion.Euler(0f, 0f, (a2 + a1) * .5f * Mathf.Rad2Deg);
            go.transform.localScale = new Vector3(q + r, Mathf.Max(q - r, 1e-4f), Thick);
            cube = new GameObject("Glass pane (sheared)");
            cube.transform.SetParent(go.transform, false);
            cube.transform.localRotation = Quaternion.Euler(0f, 0f, (a2 - a1) * .5f * Mathf.Rad2Deg);
        }
        cube.AddComponent<MeshFilter>().sharedMesh = paneCube;
        var rend = cube.AddComponent<MeshRenderer>();
        rend.sharedMaterial = window;
        rend.shadowCastingMode = ShadowCastingMode.Off;
        // the smear and print bands sit at hand height over this guard's floor (the material's _FloorY is world 0)
        if (paneBlock == null) paneBlock = new MaterialPropertyBlock();
        paneBlock.Clear();
        paneBlock.SetFloat(PaneFloorYId, W(Vector3.up * floorY).y);
        paneBlock.SetFloat(PaneAlphaId, GuardPaneAlpha);
        paneBlock.SetFloat(PaneDustId, GuardPaneDust);
        rend.SetPropertyBlock(paneBlock);
        return true;
    }

    // A raking glass guard's panes along a..b (b the upper end, both on the nosing line): <= 1.52 m on plan with 6 mm joints, each
    // a parallelogram from 25 mm over the line (in the shoe) to 'height' above that. False when Glass_Window is not in this build.
    bool RakedPanes(Vector3 a, Vector3 b, float height)
    {
        if (FrontRoomsSurfaces.TryGet("Glass_Window") == null) return false;
        var flat = new Vector3(b.x - a.x, 0f, b.z - a.z);
        var run = flat.magnitude;
        if (run < .02f) return true;
        var panes = Mathf.Max(1, Mathf.CeilToInt(run / 1.52f));
        var pane = (run - (panes - 1) * .006f) / panes;
        for (var i = 0; i < panes; i++)
        {
            var f0 = i * (pane + .006f) / run;
            var f1 = (i * (pane + .006f) + pane) / run;
            var p0 = Vector3.Lerp(a, b, f0);
            var p1 = Vector3.Lerp(a, b, f1);
            GlassPane(p0, p1, p0.y + .025f, p1.y + .025f, height, (p0.y + p1.y) * .5f);
        }
        return true;
    }

    // Framed woven wire (Part B F8 (d)): 2 in square posts where they stand today, a channel frame round each bay 75 mm clear of
    // the floor, the woven wire in it (Prop_WireMesh, cut out), a 42.2 mm top rail.
    void MeshGuard(Vector3 a, Vector3 b, Vector3 t, float len, float floorY, float top, float height, Family fam)
    {
        var metal = RailMetal(fam);
        var wire = Surf("Prop_WireMesh", MeshMaterial());
        var count = Mathf.Max(1, Mathf.CeilToInt(len / 1.5f));
        var rot = Quaternion.LookRotation(t);
        const float Post = .0508f;
        for (var k = 0; k <= count; k++)
        {
            var q = a + t * (len * k / count);
            Mesh.Box(new Vector3(q.x, (floorY + top - .04f) * .5f, q.z), new Vector3(Post, top - .04f - floorY, Post), rot, metal, false);
        }
        Tube(a + Vector3.up * (top - RailR), b + Vector3.up * (top - RailR), RailTube, metal, true);
        var y0 = floorY + .075f;
        var y1 = top - .06f;
        for (var k = 0; k < count; k++)
        {
            var p0 = a + t * (len * k / count + Post * .5f + .012f);
            var p1 = a + t * (len * (k + 1) / count - Post * .5f - .012f);
            if ((p1 - p0).sqrMagnitude < .01f) continue;
            // the frame starts mid-bottom (its two ends meet there) and sweeps with the bay's plane normal as up, so all four
            // members keep the 25.4 x 12.7 mm channel, open toward the wire (with Vector3.up the posts' members turned or pinched)
            var pm = (p0 + p1) * .5f;
            var path = new[] { pm + Vector3.up * y0, p1 + Vector3.up * y0, p1 + Vector3.up * y1, p0 + Vector3.up * y1, p0 + Vector3.up * y0, pm + Vector3.up * y0 };
            var o = FrontRoomsStructureProfiles.SweepOptions.Default;
            o.creaseDeg = 10f;
            SweepTo(metal, path, ChannelSection, o, new Vector3(-t.z, 0f, t.x));
            var n = new Vector3(t.z, 0f, -t.x);
            var u0 = Vector3.Dot(W(p0), t);
            var u1 = u0 + Vector3.Distance(p0, p1);
            // FR-1 (K1 review; structures G4-b's shared ask): the woven wire as two faces 3.4 mm apart (10 ga), each lit as a front
            // face from its own side with the same metre UVs, so the alpha-cut diamonds coincide (one face read near-black from behind)
            var wo = n * .0017f;
            Quad(p0 + wo + Vector3.up * y0, p1 + wo + Vector3.up * y0, p1 + wo + Vector3.up * y1, p0 + wo + Vector3.up * y1, n,
                new Vector2(u0, y0), new Vector2(u1, y0), new Vector2(u1, y1), new Vector2(u0, y1), wire);
            Quad(p1 - wo + Vector3.up * y0, p0 - wo + Vector3.up * y0, p0 - wo + Vector3.up * y1, p1 - wo + Vector3.up * y1, -n,
                new Vector2(u1, y0), new Vector2(u0, y0), new Vector2(u0, y1), new Vector2(u1, y1), wire);
        }
        Mesh.Collider((a + b) * .5f + Vector3.up * (floorY + top) * .5f, new Vector3(.05f, height, len), Quaternion.LookRotation(t));
    }

    bool RakingGuardNew(Vector3 a, Vector3 b, float bottomA, float bottomB, GuardStyle style, Material wall, Material cap, float height)
    {
        var fam = FamilyOf(wall);
        switch (style)
        {
            case GuardStyle.HalfWall:
                RakeWall(a, b, height - .04f, bottomA, bottomB, .16f, wall != null ? wall : FrontRoomsPoolMaterials.Lit("guard wall", new Color(.7f, .68f, .6f)), true);
                RakingCap(a, b, height, .2f, .04f, cap != null ? cap : CapMaterial());
                return true;
            case GuardStyle.TileWall:
            {
                var w = wall != null ? wall : FrontRoomsPoolMaterials.Tile;
                if (CurveTile(w, out var own, out var op))
                {
                    // K2 (structures G2-c, C8; G3, G14): today's collider, the wall to the cap's foot, the Cap course on the rake
                    RakeWall(a, b, height, bottomA, bottomB, .2f, null, true);
                    RakeWall(a, b, height - TileCapThick, bottomA, bottomB, .2f, w, false);
                    RakingTileCap(a, b, height - TileCapThick, .2f, w);
                    return true;
                }
                RakeWall(a, b, height, bottomA, bottomB, .2f, w, true);
                // the rake's top in a course of bullnose cap tile
                RakingCap(a, b, height + .012f, .2f, .012f, w);
                return true;
            }
            case GuardStyle.Rail:
            {
                var metal = RailMetal(fam);
                var flat = new Vector3(b.x - a.x, 0f, b.z - a.z);
                var count = Mathf.Max(1, Mathf.CeilToInt(flat.magnitude / 1.2f));
                var t = flat.normalized;
                var saddle = KitName("Kit_RailBracket_PostTop", fam);
                var hasSaddle = FrontRoomsKitLibrary.Has(saddle);
                for (var k = 0; k <= count; k++)
                {
                    var q = Vector3.Lerp(a, b, k / (float)count);
                    var postTop = q.y + height - RailR - (hasSaddle ? .040f : 0f);
                    Tube(q, new Vector3(q.x, postTop, q.z), RailTube, metal, false);
                    Fitting(KitName("Kit_RailPost_BaseFlange", fam), q, t);
                    if (hasSaddle) Fitting(saddle, new Vector3(q.x, postTop, q.z), t);
                }
                Tube(a + Vector3.up * (height - RailR), b + Vector3.up * (height - RailR), RailTube, metal, true);
                var pickets = FrontRoomsStructuresAssets.Rails == FrontRoomsStructuresAssets.PublicRail.Pickets && fam != Family.Run && District.Key != 5;
                if (pickets)
                {
                    BayRail(a + Vector3.up * .10f, b + Vector3.up * .10f, count, metal);
                    var slope = (b - a).normalized;
                    var nrm = Vector3.Cross(slope, new Vector3(t.z, 0f, -t.x));
                    if (nrm.y < 0f) nrm = -nrm;
                    Mesh.Occluder((a + b) * .5f + Vector3.up * (height - RailR), new Vector3(RailR * 1.4f, RailR * 1.4f, Vector3.Distance(a, b)), Quaternion.LookRotation(slope, nrm));
                    Mesh.Occluder((a + b) * .5f + Vector3.up * .10f, new Vector3(PipeR * 1.4f, PipeR * 1.4f, Vector3.Distance(a, b)), Quaternion.LookRotation(slope, nrm));
                    var n = Mathf.Max(1, Mathf.RoundToInt(flat.magnitude / .14f));
                    for (var k = 0; k < n; k++)
                    {
                        var f = (k + .5f) / n;
                        var nearest = Mathf.Round(f * count) / count;
                        if (Mathf.Abs(f - nearest) * flat.magnitude < .045f) continue;
                        var q = Vector3.Lerp(a, b, f);
                        var y0 = q.y + .10f;
                        var y1 = q.y + height - RailR;
                        Mesh.Box(new Vector3(q.x, (y0 + y1) * .5f, q.z), new Vector3(.0127f, y1 - y0, .0127f), Quaternion.LookRotation(t), metal, false);
                    }
                }
                else BayRail(a + Vector3.up * .53f, b + Vector3.up * .53f, count, metal);
                RakeWall(a, b, height, a.y, b.y, .05f, null, true);
                return true;
            }
            case GuardStyle.Glass:
            {
                var trim = Trim();
                var strip = FrontRoomsStructureProfiles.StripV(fam == Family.Pool ? FrontRoomsStructureProfiles.Strip.StainlessBrushed : FrontRoomsStructureProfiles.Strip.ShoeAluminium);
                Lay(a, b, Shoe, trim, strip);
                // raked panes (parallelograms, <= 1.52 m on plan, 6 mm joints) as the game's panes; the merged rake only without them
                if (!RakedPanes(a, b, height - .045f))
                    RakeWall(a, b, height - .02f, a.y + .025f, b.y + .025f, .012f, GlassMaterial(), false);
                if (cap != null && cap != FrontRoomsPoolMaterials.Tile) RakingCap(a, b, height, .2f, .05f, cap);
                else Lay(a + Vector3.up * (height - .038f), b + Vector3.up * (height - .038f), CapRailSection, trim, strip);
                RakeWall(a, b, height, a.y, b.y, .06f, null, true);
                return true;
            }
        }
        return false;
    }

    // A cap along a rake (an oriented box, its top 'above' over the nosing line a..b), with the cap's own edge.
    void RakingCap(Vector3 a, Vector3 b, float above, float width, float thick, Material m)
    {
        var d = b - a;
        if (d.sqrMagnitude < 1e-6f || m == null) return;
        var slope = d.normalized;
        var side = new Vector3(slope.z, 0f, -slope.x).normalized;
        var normal = Vector3.Cross(slope, side);
        if (normal.y < 0f) normal = -normal;
        var keep = Mesh.EdgeOverride;
        Mesh.EdgeOverride = CapEdge(m);
        Mesh.Box((a + b) * .5f + Vector3.up * above - normal * (thick * .5f), new Vector3(width, thick, d.magnitude + .02f), Quaternion.LookRotation(slope, normal), m, false);
        Mesh.EdgeOverride = keep;
    }

    // ---------------------------------------------------------------- NEW stairs (Part B F3-F5)

    enum NosingKind { None, Pool, Carpet, Abrasive, Given }

    static NosingKind NosingKindOf(Material nosing, Material tread)
    {
        if (nosing != null)
        {
            var n = nosing.name ?? "";
            if (n == "Structures / nosing" || n.StartsWith("Prop_PoolTile_Nosing", StringComparison.Ordinal)) return NosingKind.Pool;
            if (n.StartsWith("RunB_", StringComparison.Ordinal)) return NosingKind.Given;
            return NosingKind.Abrasive;
        }
        if (tread != null && tread.name != null && tread.name.IndexOf("Carpet", StringComparison.Ordinal) >= 0) return NosingKind.Carpet;
        return NosingKind.None;
    }

    // A flight as the period built it: on a solid base, today's steps; on a waist (a flight in the air), one riser-high block per
    // step on a 150 mm sloped waist slab (a soffit and a straight string line instead of the sawtooth), and a real nosing per
    // step: the pool's nosing L (Red's dark nosing, Prop_PoolTile_Nosing), an aluminium carpet bar in carpeted rooms, a cast
    // abrasive nosing elsewhere (Run!'s vinyl in its own material). The ramp collider is today's (the caller's).
    void FlightNew(Vector3 foot, Vector3 dir, Vector3 side, float width, int n, float riser, float tread, Material treadMat, Material nosing, bool solidBase)
    {
        var hw = width * .5f;
        var kind = NosingKindOf(nosing, treadMat);
        for (var k = 1; k <= n - 1; k++)
        {
            var top = foot.y + k * riser;
            var bottom = solidBase ? foot.y : top - riser;
            var a = foot + dir * ((k - 1) * tread);
            var b = foot + dir * (k * tread);
            a.y = b.y = 0f;
            Prism(a, b, bottom, top, width, treadMat, false);
            NosingPiece(a, dir, side, hw, top, kind, nosing);
        }
        var e = foot + dir * ((n - 1) * tread);
        e.y = 0f;
        NosingPiece(e, dir, side, hw, foot.y + n * riser, kind, nosing);
        Prism(e + dir * .001f, e + dir * .021f, foot.y + (n - 1) * riser, foot.y + n * riser - .002f, width, treadMat, false);
        if (solidBase || n < 2) return;
        // the waist: its top on the line through every step's front foot, 150 mm deep, under the whole flight
        const float Waist = .15f;
        var s0 = foot;
        var s1 = foot + dir * ((n - 1) * tread) + Vector3.up * ((n - 1) * riser);
        var slope = (s1 - s0).normalized;
        var normal = Vector3.Cross(slope, side);
        if (normal.y < 0f) normal = -normal;
        var soffit = FamilyOf(treadMat) == Family.Pool ? treadMat : Surf("Stair_Slab", treadMat);
        // G7 (K1 review, structures G3-b): the waist 2 mm inside the steps' sides each side (their coplanar sides z-fought)
        Mesh.Box((s0 + s1) * .5f - normal * (Waist * .5f), new Vector3(width - .004f, Waist, Vector3.Distance(s0, s1)), Quaternion.LookRotation(slope, normal), soffit, false);
    }

    // One nosing along a step's front edge (edge: the riser line at the step's centre, y 0; top: the tread's height).
    void NosingPiece(Vector3 edge, Vector3 dir, Vector3 side, float hw, float top, NosingKind kind, Material given)
    {
        if (kind == NosingKind.None) return;
        var p0 = edge - side * (hw + .001f) + Vector3.up * top;
        var p1 = edge + side * (hw + .001f) + Vector3.up * top;
        // the plumb frame's +x (cross(up, along)) must point out over the edge (-dir)
        if (Vector3.Dot(Vector3.Cross(Vector3.up, (p1 - p0).normalized), -dir) < 0f) { var sw = p0; p0 = p1; p1 = sw; }
        switch (kind)
        {
            case NosingKind.Pool:
            {
                // the nosing's own copy whose tile specular AA may spread a far round's highlight (G1 review FIX 5: the r 8 nose's
                // 1 px highlight rasterised into a string of bright dots at 8-15 m); up close nothing changes
                var m = FrontRoomsStructuresAssets.FarAA(FrontRoomsPoolTile.Get("Nosing"), NosingFarAA);
                Lay(p0, p1, FrontRoomsStructureProfiles.PoolNosing(), m != null ? m : given, Vector2.zero, false);
                break;
            }
            case NosingKind.Carpet:
                Lay(p0, p1, FrontRoomsStructureProfiles.CarpetBar(), Trim(), FrontRoomsStructureProfiles.StripV(FrontRoomsStructureProfiles.Strip.CarpetBar), false);
                break;
            case NosingKind.Given:
                Lay(p0, p1, FrontRoomsStructureProfiles.AbrasiveNosing(), given, Vector2.zero, false);
                break;
            default:
                Lay(p0, p1, FrontRoomsStructureProfiles.AbrasiveNosing(), Trim(), FrontRoomsStructureProfiles.StripV(FrontRoomsStructureProfiles.Strip.AbrasiveNosing), false);
                break;
        }
    }

    // ---------------------------------------------------------------- NEW level edges (Part B F2, F9)

    // A ledge face (a level edge inside a room): today's box, its collider as before; where the floor is not the face's finish,
    // the floor runs to the arris (a strip of it on the face's top) and, on carpet, an aluminium carpet bar finishes the edge.
    void LedgeNew(Vector3 min, Vector3 max, Material ledge, Material floor, float floorThick, Vector3 q0, Vector3 q1, Vector3 intoLow)
    {
        BoxCollider(min, max);
        var lo = Vector3.Min(min, max);
        var hi = Vector3.Max(min, max);
        // under a carpet bar the face is the ledge's own finish: carpet is only its pile and pad (12 mm), behind the bar's 19 mm
        // face leg (G1 review FIX 7: the 50 mm floor strip showed 31 mm of carpet down the face under the bar)
        var carpet = floor != null && floor.name != null && floor.name.IndexOf("Carpet", StringComparison.Ordinal) >= 0;
        var strip = carpet ? Mathf.Min(floorThick, CarpetPile) : floorThick;
        if (floor != null && floor != ledge && hi.y - lo.y > strip + .02f)
        {
            Box(lo, new Vector3(hi.x, hi.y - strip, hi.z), ledge, false);
            Box(new Vector3(lo.x, hi.y - strip, lo.z), hi, floor, false);
        }
        else Box(lo, hi, ledge, false);
        if (carpet) EdgeBar(q0 + intoLow * .1f, q1 + intoLow * .1f, hi.y, intoLow);
    }

    /// <summary>Carpet with its pad at a nosing or an edge bar (m): what the bar's face leg covers.</summary>
    const float CarpetPile = .012f;
    /// <summary>The pool nosing's far specular AA ceiling (roughness; FrontRoomsStructuresAssets.FarAA).</summary>
    const float NosingFarAA = .55f;

    // A slab's void face under a carpet bar shows the slab: a skin of the section's material 1.5 mm proud of the floor's own box
    // face (a..b on the edge line, 'out' toward the void), from the floor box's foot up to the pile (G1 review FIX 7: ~30 mm of
    // carpet ran down the void face under the bar before the section began).
    void SectionFace(Vector3 a, Vector3 b, Vector3 outward, float y0, float y1, Material m)
    {
        if (m == null || y1 - y0 < .002f) return;
        a.y = b.y = 0f;
        var o = outward.normalized * .0015f;
        QuadPlanar(a + o + Vector3.up * y0, b + o + Vector3.up * y0, b + o + Vector3.up * y1, a + o + Vector3.up * y1, outward, m);
    }

    /// <summary>
    /// NEW: a tile cove base along walls' feet (K-02; G1 review FIX 9): Prop_PoolTile_Cove's r 50 cove with its 50 mm flats on
    /// the floor and the wall (the map's V), laid 2 mm proud of both with 4 mm skirts that close its edges (the cove tile's own
    /// edge), along a path at the walls' faces on the floor whose plumb frame's +x (Cross(up, along)) points into the room;
    /// mitred where the path turns (internal corners). Render only; nothing in TODAY.
    /// </summary>
    public void CoveBase(IList<Vector3> path, Material m)
    {
        if (!isNew || m == null || path == null || path.Count < 2) return;
        const float Lift = .002f, Skirt = .004f;
        var c = FrontRoomsStructureProfiles.Cove(.05f, .05f, 6);
        var n = c.P.Length;
        var p = new Vector2[n + 2];
        var v = new float[n + 2];
        var hard = new bool[n + 2];
        p[0] = new Vector2(c.P[0].x + Lift, Lift - Skirt);
        v[0] = c.V[0];
        for (var i = 0; i < n; i++) { p[i + 1] = c.P[i] + new Vector2(Lift, Lift); v[i + 1] = c.V[i]; }
        p[n + 1] = new Vector2(Lift - Skirt, c.P[n - 1].y + Lift);
        v[n + 1] = c.V[n - 1];
        hard[0] = hard[1] = hard[n] = hard[n + 1] = true;
        var o = FrontRoomsStructureProfiles.SweepOptions.Default;
        o.plumb = true;
        o.creaseDeg = 10f;
        o.u0 = FrontRoomsStructureProfiles.WorldU(W(path[0]), path[1] - path[0]);
        SweepTo(m, path, new FrontRoomsStructureProfiles.Section("cove base r 50", p, hard, false, v), o);
    }

    /// <summary>
    /// NEW: a movement joint (TCNA EJ171; G1 review FIX 9): a 10 mm grey polyurethane bead, tooled 0.4 mm concave, on the
    /// structures trim sheet's sealant strip, along a path laid on finished surfaces (0.6 mm over them); 'normalHint' points
    /// out of the surfaces (the sweep's frame: the bead's width stays square to the path). Render only; nothing in TODAY.
    /// </summary>
    public void SealantJoint(IList<Vector3> path, Vector3 normalHint)
    {
        if (!isNew || path == null || path.Count < 2) return;
        var o = FrontRoomsStructureProfiles.SweepOptions.Default;
        o.vRange = FrontRoomsStructureProfiles.StripV(FrontRoomsStructureProfiles.Strip.Sealant);
        o.uScale = 1f / FrontRoomsStructureProfiles.TrimMetres;
        o.u0 = FrontRoomsStructureProfiles.WorldU(W(path[0]), path[1] - path[0]) / FrontRoomsStructureProfiles.TrimMetres;
        o.creaseDeg = 40f;
        sweepV.Clear(); sweepN.Clear(); sweepUV.Clear(); sweepT.Clear();
        FrontRoomsStructureProfiles.Sweep(sweepV, sweepN, sweepUV, sweepT, path, normalHint, FrontRoomsStructureProfiles.Sealant(.010f, .0004f), o);
        Mesh.Append(sweepV, sweepN, sweepUV, sweepT, Trim(), false);
    }

    // ---------------------------------------------------------------- K2 (structures G2-c): pool edges and tile caps

    struct PendingEdge
    {
        public Vector3 a, b;
        public float y;
        public Material m;
    }

    struct PendingCap
    {
        public Vector3 a, b;
        public float y, width, thick;
        public Material m;
    }

    readonly List<PendingEdge> pendingEdges = new List<PendingEdge>();
    readonly List<PendingCap> pendingCaps = new List<PendingCap>();
    readonly List<Vector4> waterLocal = new List<Vector4>();
    readonly List<float> waterLocalY = new List<float>();

    /// <summary>The cap course's height on a tiled guard (a double-bullnose cap tile 45 mm over the wall's top course).</summary>
    const float TileCapThick = .045f;

    // Kit.Prism's collider alone, exactly as Prism makes it (a, b on plan).
    void PrismCollider(Vector3 a, Vector3 b, float y0, float y1, float thick)
    {
        a.y = 0f;
        b.y = 0f;
        var d = b - a;
        var len = d.magnitude;
        if (len < 1e-4f || y1 - y0 < 1e-4f || thick < 1e-4f) return;
        Mesh.Collider((a + b) * .5f + Vector3.up * ((y0 + y1) * .5f), new Vector3(thick, y1 - y0, len), Quaternion.LookRotation(d / len, Vector3.up));
    }

    // The water (district-local) under plan point p below height y: its surface, or NaN.
    float WaterUnder(Vector3 p, float y)
    {
        var best = float.NaN;
        for (var i = 0; i < waterLocal.Count; i++)
        {
            var r = waterLocal[i];
            var wy = waterLocalY[i];
            if (p.x < r.x || p.x > r.z || p.z < r.y || p.z > r.w) continue;
            if (wy > y - .005f || wy < y - 1.2f) continue;
            if (float.IsNaN(best) || wy > best) best = wy;
        }
        return best;
    }

    void FlushRuns()
    {
        if (pendingEdges.Count > 0) FlushEdges();
        if (pendingCaps.Count > 0) FlushCaps();
        if (pendingBasins.Count > 0) FlushBasins();
    }

    readonly List<Vector4> pendingBasins = new List<Vector4>();
    readonly List<float> pendingBasinFloor = new List<float>();

    // K2 (structures G2-c, C9; 30_plan S1-08, S2-01; A.5.1, BR:S40 2-5): a tiled basin's fittings as built: the main drain at its
    // deepest point (a flat slotted grate in a square of the dark Lane tile, "marked plainly in an appropriate contrasting color"),
    // return inlets 0.3 m under the water on its two long walls (one a 3 m), clear of its niches. Ladders none: image 1's ways out
    // are steps. Kits render only (no collider); the Lane square 1 mm over the floor.
    void FlushBasins()
    {
        var lane = FrontRoomsPoolTile.Get("Lane");
        for (var i = 0; i < pendingBasins.Count; i++)
        {
            var r = pendingBasins[i];
            var floorY = pendingBasinFloor[i];
            var centre = new Vector3((r.x + r.z) * .5f, floorY, (r.y + r.w) * .5f);
            var waterY = float.NaN;
            for (var k = 0; k < waterLocal.Count; k++)
            {
                var w = waterLocal[k];
                if (centre.x < w.x || centre.x > w.z || centre.z < w.y || centre.z > w.w || waterLocalY[k] < floorY) continue;
                waterY = waterLocalY[k];
            }
            if (float.IsNaN(waterY)) continue;
            const float Frame = .225f;
            if (lane != null)
                QuadPlanar(centre + new Vector3(-Frame, .001f, -Frame), centre + new Vector3(-Frame, .001f, Frame), centre + new Vector3(Frame, .001f, Frame), centre + new Vector3(Frame, .001f, -Frame), Vector3.up, lane);
            Fitting("Kit_MainDrainGrate", centre + Vector3.up * .001f, Vector3.right);
            // the two long walls (x or z), their inside faces facing into the basin
            var alongX = r.z - r.x >= r.w - r.y;
            var len = alongX ? r.z - r.x : r.w - r.y;
            var n = Mathf.Max(1, Mathf.RoundToInt(len / 3f));
            var y = waterY - .3f;
            if (y < floorY + .15f) continue;
            for (var side = 0; side < 2; side++)
            {
                Vector3 inward;
                if (alongX) inward = side == 0 ? Vector3.forward : Vector3.back;
                else inward = side == 0 ? Vector3.right : Vector3.left;
                for (var k = 0; k < n; k++)
                {
                    var f = (k + .5f) / n;
                    var at = alongX ? new Vector3(Mathf.Lerp(r.x, r.z, f), y, side == 0 ? r.y : r.w) : new Vector3(side == 0 ? r.x : r.z, y, Mathf.Lerp(r.y, r.w, f));
                    // clear of a niche: slide along the wall away from any underwater lamp within 0.7 m
                    var tAlong = alongX ? Vector3.right : Vector3.forward;
                    for (var tries = 0; tries < 3 && NicheNear(at, .7f); tries++) at += tAlong * .75f;
                    if (NicheNear(at, .7f)) continue;
                    Fitting("Kit_ReturnInlet", at, Vector3.Cross(Vector3.up, inward));
                }
            }
        }
        pendingBasins.Clear();
        pendingBasinFloor.Clear();
    }

    // whether one of this district's underwater lamps lies within d (plan) of a district-local point
    bool NicheNear(Vector3 local, float d)
    {
        var wp = W(local);
        foreach (var l in lights)
        {
            if (l == null || l.transform.parent == null || l.transform.parent.name != "Lamp · underwater") continue;
            var p = l.transform.position;
            if (new Vector2(p.x - wp.x, p.z - wp.z).magnitude < d && Mathf.Abs(p.y - wp.y) < 1f) return true;
        }
        return false;
    }

    // Each pending coping: its water side (the side over a water lower than its deck), turned so the water lies on its path's left
    // (the plumb frame's +x over the deck), its system (ask 1 by district; a tank's edge 0.6 m over its water: the bullnose coping);
    // then chained end to end (same deck, system, water and material) and swept: mitred corners, closed ends where steps, a
    // flume's bed or a weir cut it. A coping with no water beside it keeps today's prism (render; its collider is already made).
    void FlushEdges()
    {
        var segs = new List<PendingEdge>();
        var kinds = new List<int>();
        var waters = new List<float>();
        foreach (var e in pendingEdges)
        {
            var d = e.b - e.a;
            var len = d.magnitude;
            var t = d / len;
            var side = Vector3.Cross(Vector3.up, t);
            var mid = (e.a + e.b) * .5f;
            var wr = WaterUnder(mid + side * .35f, e.y);
            var wl = WaterUnder(mid - side * .35f, e.y);
            if (float.IsNaN(wr) == float.IsNaN(wl))
            {
                var keep = Mesh.EdgeOverride;
                Prism(e.a, e.b, e.y - .08f, e.y + .03f, .32f, e.m, false);
                Mesh.EdgeOverride = keep;
                continue;
            }
            var s = e;
            var wy = wl;
            if (!float.IsNaN(wr)) { s.a = e.b; s.b = e.a; wy = wr; }
            var depth = e.y - wy;
            FrontRoomsStructureCurveTile.Edge kind;
            if (depth > .2f) kind = FrontRoomsStructureCurveTile.Edge.Tank;
            else
                switch (FrontRoomsStructuresAssets.EdgeFor(District.Key))
                {
                    case FrontRoomsStructuresAssets.PoolEdge.RimFlow: kind = FrontRoomsStructureCurveTile.Edge.RimFlow; break;
                    case FrontRoomsStructuresAssets.PoolEdge.Recessed: kind = FrontRoomsStructureCurveTile.Edge.Recessed; break;
                    default: kind = FrontRoomsStructureCurveTile.Edge.RollOut; break;
                }
            segs.Add(s);
            kinds.Add((int)kind);
            waters.Add(wy);
        }
        pendingEdges.Clear();
        var used = new bool[segs.Count];
        for (var i = 0; i < segs.Count; i++)
        {
            if (used[i]) continue;
            used[i] = true;
            var chain = new List<Vector3> { segs[i].a, segs[i].b };
            for (var grew = true; grew;)
            {
                grew = false;
                for (var j = 0; j < segs.Count; j++)
                {
                    if (used[j] || kinds[j] != kinds[i] || segs[j].m != segs[i].m || Mathf.Abs(segs[j].y - segs[i].y) > 1e-3f) continue;
                    if (Flat(segs[j].a - chain[chain.Count - 1]) < 1e-3f) { chain.Add(segs[j].b); used[j] = grew = true; }
                    else if (Flat(segs[j].b - chain[0]) < 1e-3f) { chain.Insert(0, segs[j].a); used[j] = grew = true; }
                }
            }
            EdgeSweep(chain, segs[i].y, (FrontRoomsStructureCurveTile.Edge)kinds[i], segs[i].m, waters[i]);
        }
    }

    static float Flat(Vector3 d) { return Mathf.Sqrt(d.x * d.x + d.z * d.z); }

    void EdgeSweep(List<Vector3> chain, float y, FrontRoomsStructureCurveTile.Edge kind, Material like, float waterY)
    {
        var closed = chain.Count > 3 && Flat(chain[0] - chain[chain.Count - 1]) < 1e-3f;
        var path = new List<Vector3>();
        if (closed)
        {
            // a closed loop starts and ends at its first side's middle (one seamless butt there, every corner mitred)
            var m0 = (chain[0] + chain[1]) * .5f;
            path.Add(m0);
            for (var k = 1; k < chain.Count; k++) path.Add(chain[k]);
            path[path.Count - 1] = chain[0];
            path.Add(m0);
        }
        else path.AddRange(chain);
        for (var k = 0; k < path.Count; k++) path[k] = new Vector3(path[k].x, y, path[k].z);
        var section = FrontRoomsStructureCurveTile.EdgeSection(kind, .32f, out var tileKind);
        var mat = FrontRoomsPoolTile.ForWater(tileKind, W(new Vector3(0f, waterY, 0f)).y);
        if (mat == null) mat = like;
        var o = FrontRoomsStructureProfiles.SweepOptions.Default;
        o.plumb = true;
        o.creaseDeg = 10f;
        o.u0 = FrontRoomsStructureProfiles.WorldU(W(path[0]), path[1] - path[0]);
        SweepTo(mat, path, section, o);
        if (closed) return;
        // the open ends: the section's own outline closed down its body (the edge's returned end)
        EdgeEnd(path[0], path[1] - path[0], section, mat, -1f);
        EdgeEnd(path[path.Count - 1], path[path.Count - 1] - path[path.Count - 2], section, mat, 1f);
    }

    void EdgeEnd(Vector3 at, Vector3 along, FrontRoomsStructureProfiles.Section s, Material m, float sign)
    {
        along.y = 0f;
        if (along.sqrMagnitude < 1e-8f || m == null) return;
        along.Normalize();
        var r = Vector3.Cross(Vector3.up, along).normalized;
        var n = s.P.Length;
        var poly = new Vector2[n + 1];
        for (var i = 0; i < n; i++) poly[i] = s.P[i];
        poly[n] = new Vector2(s.P[0].x, s.P[n - 1].y);
        var order = FrontRoomsStructureProfiles.EarClip(poly);
        sweepV.Clear(); sweepN.Clear(); sweepUV.Clear(); sweepT.Clear();
        var nrm = along * sign;
        foreach (var q in poly)
        {
            sweepV.Add(at + r * q.x + Vector3.up * q.y);
            sweepN.Add(nrm);
            sweepUV.Add(new Vector2(q.x, q.y));
        }
        for (var i = 0; i + 2 < order.Count; i += 3)
        {
            int a = order[i], b = order[i + 1], c = order[i + 2];
            if (Vector3.Dot(Vector3.Cross(sweepV[b] - sweepV[a], sweepV[c] - sweepV[a]), nrm) < 0f) { var t = b; b = c; c = t; }
            sweepT.Add(a); sweepT.Add(b); sweepT.Add(c);
        }
        Mesh.Append(sweepV, sweepN, sweepUV, sweepT, m, false);
    }

    // The tiled guards' and parapets' Cap courses, chained end to end (either way round) and swept with mitred corners and returned
    // ends, in the field tile (each 150 mm along one cap tile); the cap's volume keeps the wall's top arrises square (no round under it).
    void FlushCaps()
    {
        var segs = new List<PendingCap>(pendingCaps);
        pendingCaps.Clear();
        var used = new bool[segs.Count];
        for (var i = 0; i < segs.Count; i++)
        {
            if (used[i]) continue;
            used[i] = true;
            var chain = new List<Vector3> { segs[i].a, segs[i].b };
            for (var grew = true; grew;)
            {
                grew = false;
                for (var j = 0; j < segs.Count; j++)
                {
                    if (used[j] || segs[j].m != segs[i].m || Mathf.Abs(segs[j].y - segs[i].y) > 1e-3f || Mathf.Abs(segs[j].width - segs[i].width) > 1e-3f) continue;
                    Vector3 sa = segs[j].a, sb = segs[j].b;
                    if (Flat(sa - chain[chain.Count - 1]) < 1e-3f) { chain.Add(sb); used[j] = grew = true; }
                    else if (Flat(sb - chain[chain.Count - 1]) < 1e-3f) { chain.Add(sa); used[j] = grew = true; }
                    else if (Flat(sb - chain[0]) < 1e-3f) { chain.Insert(0, sa); used[j] = grew = true; }
                    else if (Flat(sa - chain[0]) < 1e-3f) { chain.Insert(0, sb); used[j] = grew = true; }
                }
            }
            var c = segs[i];
            var closed = chain.Count > 3 && Flat(chain[0] - chain[chain.Count - 1]) < 1e-3f;
            var path = new List<Vector3>();
            if (closed)
            {
                var m0 = (chain[0] + chain[1]) * .5f;
                path.Add(m0);
                for (var k = 1; k < chain.Count; k++) path.Add(chain[k]);
                path[path.Count - 1] = chain[0];
                path.Add(m0);
            }
            else path.AddRange(chain);
            for (var k = 0; k < path.Count; k++) path[k] = new Vector3(path[k].x, c.y, path[k].z);
            var o = FrontRoomsStructureProfiles.SweepOptions.Default;
            o.plumb = true;
            o.creaseDeg = 10f;
            o.capStart = o.capEnd = !closed;
            o.u0 = FrontRoomsStructureProfiles.WorldU(W(path[0]), path[1] - path[0]);
            SweepTo(c.m, path, FrontRoomsStructureCurveTile.CapSection(c.width, c.thick), o);
        }
    }

    // A tiled guard in the pool (NEW): the wall to the cap's foot (bevelled, its top arrises kept square by the cap's volume), the
    // Cap course on it (chained at Finish), today's collider for the guard's full height.
    void TileGuard(Vector3 a, Vector3 b, Vector3 t, float len, float floorY, float top, Material wall)
    {
        const float Thick = .2f;
        var capFoot = top - TileCapThick;
        Prism(a, b, floorY, capFoot, Thick, wall, false);
        PrismCollider(a, b, floorY, top, Thick);
        Mesh.Occluder((a + b) * .5f + Vector3.up * (capFoot + TileCapThick * .5f), new Vector3(Thick, TileCapThick, len), Quaternion.LookRotation(t, Vector3.up));
        pendingCaps.Add(new PendingCap { a = a, b = b, y = capFoot, width = Thick, thick = TileCapThick, m = wall });
    }

    // A raking tiled guard's cap (NEW; the K1 review's G3 and G14): the Cap course along the rake, its top on the guard's height and
    // its ends plumb, flush with the wall's plumb ends (it neither stands 12 mm proud nor leans past them).
    void RakingTileCap(Vector3 a, Vector3 b, float capFoot, float width, Material m)
    {
        if (m == null || (b - a).sqrMagnitude < 1e-6f) return;
        var o = FrontRoomsStructureProfiles.SweepOptions.Default;
        o.plumb = true;
        o.capStart = o.capEnd = true;
        o.u0 = FrontRoomsStructureProfiles.WorldU(W(a), b - a);
        SweepTo(m, new[] { a + Vector3.up * capFoot, b + Vector3.up * capFoot }, FrontRoomsStructureCurveTile.CapSection(width, TileCapThick), o);
    }

    /// <summary>K2 (structures G2-c, C12): rails in a service zone (Red's ask 3: the two-line rail, pickets only on public floors) for
    /// the guards made while it is set (SP6's C8 balance tank).</summary>
    public bool ServiceRails;
    /// <summary>K2 (structures G2-c, C12; the K1 review's FR-5 / G8): guards made while it is set share one post where their ends
    /// meet (a hole's square guard: one post a corner, not two 0.11 m apart).</summary>
    public bool SharedPosts;
    readonly HashSet<long> postsAt = new HashSet<long>();

    static long PostKey(Vector3 p) => ((long)Mathf.RoundToInt(p.x * 200f) << 32) ^ (long)(Mathf.RoundToInt(p.z * 200f) & 0xffffffff);

    // K2 (structures G2-c, C14; the K1 review's FR-2): glass guard ends, so a run that starts or ends where a lower run ends steps its
    // shoe, panes and end fitting back onto its own floor (the court's +1.8 run over the +1.2 run's end at x 3.9).
    readonly List<Vector4> glassEnds = new List<Vector4>();
    const float GlassEndInset = .045f;

    // The carpet bar along an edge at height y, its nose out toward 'outward' (horizontal).
    void EdgeBar(Vector3 a, Vector3 b, float y, Vector3 outward)
    {
        a.y = b.y = y;
        if ((b - a).sqrMagnitude < 1e-4f) return;
        if (Vector3.Dot(Vector3.Cross(Vector3.up, (b - a).normalized), outward) < 0f) { var sw = a; a = b; b = sw; }
        Lay(a, b, FrontRoomsStructureProfiles.CarpetBar(), Trim(), FrontRoomsStructureProfiles.StripV(FrontRoomsStructureProfiles.Strip.CarpetBar), false);
    }

    // ---------------------------------------------------------------- the cell plate

    /// <summary>
    /// One storey of a district as cells (40 section 11.4): each cell has a kind (Floor, Void, Held: a connector or set
    /// piece owns it), a floor and a ceiling (metres above the plate's base; terrain levels in whole 0.30 steps) and a room.
    /// Build resolves every edge from its two cells: walls between rooms and round the outside (with the openings given),
    /// level faces inside a room with a guard over 0.76 m unless the edge is crossed (a STEP, STEP RUN or flight the plate
    /// builds), VOID EDGE guards with the floor's section under them, soffits where ceilings differ. Floors, lids and one
    /// troffer per lit cell come with it. Districts only add their set pieces.
    /// </summary>
    public sealed class Plate
    {
        public enum Kind : byte { Outside, Floor, Void, Held }

        public readonly int W, D;
        public readonly float X0, Z0, Y0;
        readonly Kind[] kinds;
        readonly float[] floors, ceils;
        readonly int[] rooms;
        readonly bool[] dark, deadLamp, noLid;
        readonly Dictionary<long, List<Opening>> openings = new Dictionary<long, List<Opening>>();
        readonly Dictionary<long, List<Vector2>> crossed = new Dictionary<long, List<Vector2>>();
        readonly Dictionary<long, GuardStyle> guards = new Dictionary<long, GuardStyle>();
        readonly HashSet<long> noWall = new HashSet<long>();
        readonly HashSet<long> forceWall = new HashSet<long>();
        readonly List<RunSpec> runs = new List<RunSpec>();

        public Material WallMat, FloorMat, CeilingMat, LedgeMat, SectionMat, GuardWall, GuardCap, TreadMat, NosingMat;
        public GuardStyle VoidGuard = GuardStyle.Rail, LedgeGuard = GuardStyle.Rail;
        /// <summary>Under a void edge, the floor's section is drawn down this far (slab and plenum to the ceiling below).</summary>
        public float SectionDepth = 1.0f;
        public float WallThick = Wall, FloorThick = SlabThick;
        /// <summary>One troffer per lit cell (0.3 m north of its centre, as the map hangs them).</summary>
        public bool Troffers = true, Office;
        public float LampScale = 1f, LampRange = 10f;
        public bool ViewLamps;
        /// <summary>Step runs get a cheek on each open side (a side short of its edge's ends): 0.12 over the nosings, solid to the
        /// lower floor, and from the first tread over 0.76 m the ledge's guard style 1.07 over the nosing line to the edge, where it
        /// meets the ledge guard's cut end (40 section 11.4, STEP RUN + cheek; 70 section 1, guard; scene 4 review KIT-F3).</summary>
        public bool RunCheeks;

        struct RunSpec
        {
            public int axis, i, j;
            public float from, to;
        }

        public Plate(float x0, float z0, float y0, int w, int d)
        {
            X0 = x0;
            Z0 = z0;
            Y0 = y0;
            W = Mathf.Max(1, w);
            D = Mathf.Max(1, d);
            var n = W * D;
            kinds = new Kind[n];
            floors = new float[n];
            ceils = new float[n];
            rooms = new int[n];
            dark = new bool[n];
            deadLamp = new bool[n];
            noLid = new bool[n];
        }

        bool In(int i, int j) => i >= 0 && j >= 0 && i < W && j < D;
        int Idx(int i, int j) => i + j * W;

        public Kind KindAt(int i, int j) => In(i, j) ? kinds[Idx(i, j)] : Kind.Outside;
        public float FloorAt(int i, int j) => In(i, j) ? floors[Idx(i, j)] : 0f;
        public float CeilAt(int i, int j) => In(i, j) ? ceils[Idx(i, j)] : 0f;
        public int RoomAt(int i, int j) => In(i, j) ? rooms[Idx(i, j)] : -1;

        /// <summary>Cells i0..i1, j0..j1 (inclusive): kind, floor and ceiling (metres above the plate's base), room.</summary>
        public void Fill(int i0, int j0, int i1, int j1, Kind kind, float floor, float ceil, int room)
        {
            for (var j = Mathf.Min(j0, j1); j <= Mathf.Max(j0, j1); j++)
            for (var i = Mathf.Min(i0, i1); i <= Mathf.Max(i0, i1); i++)
            {
                if (!In(i, j)) continue;
                var k = Idx(i, j);
                kinds[k] = kind;
                floors[k] = floor;
                ceils[k] = ceil;
                rooms[k] = room;
            }
        }

        /// <summary>No troffer in these cells (dark), or a dead one (lens unlit), or no lid at all (open above).</summary>
        public void Dark(int i0, int j0, int i1, int j1) => Flag(dark, i0, j0, i1, j1);
        public void DeadLamp(int i, int j) => Flag(deadLamp, i, j, i, j);
        public void NoLid(int i0, int j0, int i1, int j1) => Flag(noLid, i0, j0, i1, j1);

        void Flag(bool[] a, int i0, int j0, int i1, int j1)
        {
            for (var j = Mathf.Min(j0, j1); j <= Mathf.Max(j0, j1); j++)
            for (var i = Mathf.Min(i0, i1); i <= Mathf.Max(i0, i1); i++)
                if (In(i, j)) a[Idx(i, j)] = true;
        }

        // An edge of cell (i, j) on a side (N, S, E, W) as (axis, ei, ej): axis 0 = the line x = X0 + 3 ei between
        // (ei - 1, ej) and (ei, ej); axis 1 = the line z = Z0 + 3 ej between (ei, ej - 1) and (ei, ej).
        static long Key(int axis, int i, int j) => ((long)axis << 40) | ((long)(i + 1000) << 20) | (long)(j + 1000);

        static void EdgeOf(int i, int j, char side, out int axis, out int ei, out int ej)
        {
            switch (side)
            {
                case 'W': axis = 0; ei = i; ej = j; return;
                case 'E': axis = 0; ei = i + 1; ej = j; return;
                case 'S': axis = 1; ei = i; ej = j; return;
                default: axis = 1; ei = i; ej = j + 1; return;
            }
        }

        /// <summary>An opening on the edge of cell (i, j) on a side: from / to metres along the edge from its west or south end; sill and top above the plate's base.</summary>
        public void Open(int i, int j, char side, float from, float to, float sill, float top, bool round = false)
        {
            EdgeOf(i, j, side, out var axis, out var ei, out var ej);
            var key = Key(axis, ei, ej);
            if (!openings.TryGetValue(key, out var list)) openings[key] = list = new List<Opening>();
            list.Add(new Opening(from, to, sill, top, round));
        }

        /// <summary>An LMS doorless arch (top 2.2 above the lower floor) of the given width, starting <paramref name="from"/> m along the edge.</summary>
        public void Arch(int i, int j, char side, float from, float width, float top = 2.2f)
        {
            EdgeOf(i, j, side, out var axis, out var ei, out var ej);
            var low = Mathf.Min(SideFloor(axis, ei, ej, true), SideFloor(axis, ei, ej, false));
            Open(i, j, side, from, from + width, low, low + top);
        }

        float SideFloor(int axis, int ei, int ej, bool first)
        {
            int i = ei, j = ej;
            if (first) { if (axis == 0) i -= 1; else j -= 1; }
            return In(i, j) && kinds[Idx(i, j)] != Kind.Outside ? floors[Idx(i, j)] : 0f;
        }

        /// <summary>The whole edge open: no wall even between rooms or to the outside (a set piece or another plate continues there).</summary>
        public void NoWall(int i, int j, char side)
        {
            EdgeOf(i, j, side, out var axis, out var ei, out var ej);
            noWall.Add(Key(axis, ei, ej));
        }

        /// <summary>A wall even on a void edge or inside a room (with its openings).</summary>
        public void ForceWall(int i, int j, char side)
        {
            EdgeOf(i, j, side, out var axis, out var ei, out var ej);
            forceWall.Add(Key(axis, ei, ej));
        }

        /// <summary>A guard style for one edge (a void edge or a ledge); None leaves it unguarded.</summary>
        public void GuardEdge(int i, int j, char side, GuardStyle style)
        {
            EdgeOf(i, j, side, out var axis, out var ei, out var ej);
            guards[Key(axis, ei, ej)] = style;
        }

        /// <summary>
        /// A STEP or STEP RUN across a level edge of cell (i, j): from / to along the edge; the plate builds the risers
        /// (0.15 each) in the lower cell up to the edge, and no guard stands on that span.
        /// </summary>
        public void Run(int i, int j, char side, float from, float to)
        {
            EdgeOf(i, j, side, out var axis, out var ei, out var ej);
            var key = Key(axis, ei, ej);
            if (!crossed.TryGetValue(key, out var list)) crossed[key] = list = new List<Vector2>();
            list.Add(new Vector2(Mathf.Min(from, to), Mathf.Max(from, to)));
            runs.Add(new RunSpec { axis = axis, i = ei, j = ej, from = Mathf.Min(from, to), to = Mathf.Max(from, to) });
        }

        /// <summary>Mark a span of a level or void edge as crossed by a piece the district builds (a flight, a bridge): no guard there.</summary>
        public void Crossed(int i, int j, char side, float from, float to)
        {
            EdgeOf(i, j, side, out var axis, out var ei, out var ej);
            var key = Key(axis, ei, ej);
            if (!crossed.TryGetValue(key, out var list)) crossed[key] = list = new List<Vector2>();
            list.Add(new Vector2(Mathf.Min(from, to), Mathf.Max(from, to)));
        }

        struct Seg
        {
            public float from, to, y0, y1, ceiling;
            public bool grimeOff;
            public List<Opening> ops;
        }

        public void Build(FrontRoomsStructuresKit kit)
        {
            var cs = Cell;
            var keepCeiling = kit.Mesh.Ceiling;
            var keepGrime = kit.Mesh.FloorGrimeOff;
            // Floors, lids and lamps.
            for (var j = 0; j < D; j++)
            for (var i = 0; i < W; i++)
            {
                var k = Idx(i, j);
                if (kinds[k] != Kind.Floor) continue;
                float x = X0 + i * cs, z = Z0 + j * cs;
                var fy = Y0 + floors[k];
                kit.Shell(Y0 + ceils[k], fy);
                if (FloorMat != null) kit.Slab(x, z, x + cs, z + cs, fy, FloorMat, FloorThick);
                if (!noLid[k] && CeilingMat != null && ceils[k] > floors[k] + .5f)
                {
                    kit.Lid(x, z, x + cs, z + cs, Y0 + ceils[k], CeilingMat);
                    if (Troffers && !dark[k])
                        kit.Troffer(new Vector3(x + cs * .5f, Y0 + ceils[k], z + cs * .5f + FrontRooms.Map.ModuleUnits.TrofferOffsetZ), ViewLamps, Office, LampScale, LampRange, deadLamp[k]);
                }
            }
            // Edges: vertical lines (axis 0) and horizontal lines (axis 1).
            var segs = new Dictionary<long, List<Seg>>();
            for (var axis = 0; axis < 2; axis++)
            {
                var lines = axis == 0 ? W + 1 : D + 1;
                var cells = axis == 0 ? D : W;
                for (var line = 0; line < lines; line++)
                for (var c = 0; c < cells; c++)
                {
                    int ai, aj, bi, bj;
                    if (axis == 0) { ai = line - 1; aj = c; bi = line; bj = c; }
                    else { ai = c; aj = line - 1; bi = c; bj = line; }
                    var key = axis == 0 ? Key(0, line, c) : Key(1, c, line);
                    Edge(kit, axis, line, c, KindAt(ai, aj), KindAt(bi, bj), ai, aj, bi, bj, key, segs);
                }
            }
            // Walls, merged per line into runs.
            foreach (var pair in segs)
            {
                var list = pair.Value;
                list.Sort((p, q) => p.from.CompareTo(q.from));
                var axis = (int)(pair.Key >> 40);
                var line = (int)((pair.Key >> 20) & 0xFFFFF) - 1000;
                var at = axis == 0 ? X0 + line * cs : Z0 + line * cs;
                var k = 0;
                while (k < list.Count)
                {
                    var first = list[k];
                    var run = new List<Opening>();
                    var from = first.from;
                    var to = first.to;
                    if (first.ops != null) run.AddRange(first.ops);
                    var e = k + 1;
                    while (e < list.Count && Mathf.Abs(list[e].from - to) < 1e-3f && Mathf.Abs(list[e].y0 - first.y0) < 1e-3f && Mathf.Abs(list[e].y1 - first.y1) < 1e-3f && list[e].grimeOff == first.grimeOff)
                    {
                        to = list[e].to;
                        if (list[e].ops != null) run.AddRange(list[e].ops);
                        e++;
                    }
                    // Extend a free end by half a wall (corners close); never into a collinear neighbour.
                    var before = k > 0 && Mathf.Abs(list[k - 1].to - from) < 1e-3f;
                    var after = e < list.Count && Mathf.Abs(list[e].from - to) < 1e-3f;
                    var f = before ? from : from - WallThick * .5f;
                    var t = after ? to : to + WallThick * .5f;
                    kit.Mesh.Ceiling = first.ceiling;
                    kit.Mesh.FloorGrimeOff = first.grimeOff;
                    var ops = new List<Opening>();
                    foreach (var o in run) ops.Add(new Opening(o.from - f, o.to - f, o.sill, o.top, o.round));
                    if (axis == 0) kit.WallRun(new Vector3(at, 0f, f), new Vector3(at, 0f, t), first.y0, first.y1, WallThick, ops, WallMat, true);
                    else kit.WallRun(new Vector3(f, 0f, at), new Vector3(t, 0f, at), first.y0, first.y1, WallThick, ops, WallMat, true);
                    k = e;
                }
            }
            // Step runs.
            foreach (var r in runs) BuildRun(kit, r);
            kit.Mesh.Ceiling = keepCeiling;
            kit.Mesh.FloorGrimeOff = keepGrime;
        }

        void Edge(FrontRoomsStructuresKit kit, int axis, int line, int c, Kind ka, Kind kb, int ai, int aj, int bi, int bj, long key, Dictionary<long, List<Seg>> segs)
        {
            var cs = Cell;
            var occA = ka == Kind.Floor || ka == Kind.Held;
            var occB = kb == Kind.Floor || kb == Kind.Held;
            if (!occA && !occB) return;
            // The edge's two ends (plan) and its own span along the line.
            var at = axis == 0 ? X0 + line * cs : Z0 + line * cs;
            var s0 = (axis == 0 ? Z0 : X0) + c * cs;
            var s1 = s0 + cs;
            Vector3 p0 = axis == 0 ? new Vector3(at, 0f, s0) : new Vector3(s0, 0f, at);
            Vector3 p1 = axis == 0 ? new Vector3(at, 0f, s1) : new Vector3(s1, 0f, at);
            var toB = axis == 0 ? Vector3.right : Vector3.forward;
            openings.TryGetValue(key, out var ops);
            crossed.TryGetValue(key, out var cross);
            var lineKey = Key(axis, line, 0);
            if (noWall.Contains(key) && !forceWall.Contains(key)) return;

            float fa = occA ? floors[Idx(ai, aj)] : 0f, fb = occB ? floors[Idx(bi, bj)] : 0f;
            float ca = occA ? ceils[Idx(ai, aj)] : 0f, cb = occB ? ceils[Idx(bi, bj)] : 0f;
            var wall = forceWall.Contains(key);
            if (occA && occB) wall |= rooms[Idx(ai, aj)] != rooms[Idx(bi, bj)];
            else if (occA ^ occB)
            {
                var other = occA ? kb : ka;
                if (other == Kind.Outside) wall = true;
            }
            if (wall)
            {
                float y0, y1;
                if (occA && occB) { y0 = Mathf.Min(fa, fb); y1 = Mathf.Max(ca, cb); }
                else if (occA) { y0 = fa; y1 = ca; }
                else { y0 = fb; y1 = cb; }
                var seg = new Seg { from = s0, to = s1, y0 = Y0 + y0, y1 = Y0 + y1, ceiling = Y0 + y1, grimeOff = Mathf.Abs(Y0 + y0) > .01f };
                if (ops != null)
                {
                    seg.ops = new List<Opening>();
                    foreach (var o in ops) seg.ops.Add(new Opening(s0 + o.from, s0 + o.to, Y0 + o.sill, Y0 + o.top, o.round));
                }
                if (!segs.TryGetValue(lineKey, out var list)) segs[lineKey] = list = new List<Seg>();
                list.Add(seg);
                return;
            }
            // Void edges: a guard on the floor side and the floor's section under it.
            if ((occA && kb == Kind.Void) || (occB && ka == Kind.Void))
            {
                var floorSideA = occA;
                var fy = Y0 + (floorSideA ? fa : fb);
                var inward = floorSideA ? -toB : toB;
                var style = guards.TryGetValue(key, out var gs) ? gs : VoidGuard;
                GuardSpans(kit, p0, p1, inward, fy, style, cross, s0);
                if (kit.isNew && FloorMat != null && FloorMat.name != null && FloorMat.name.IndexOf("Carpet", StringComparison.Ordinal) >= 0)
                {
                    kit.EdgeBar(p0, p1, fy, -inward);
                    // under the bar the slab, not carpet (the floor's box is its finish only on top)
                    kit.SectionFace(p0, p1, -inward, fy - FloorThick, fy - CarpetPile, SectionMat != null ? SectionMat : Surf("Stair_Slab", null));
                }
                if (SectionMat != null && SectionDepth > FloorThick + .01f)
                {
                    var a0 = p0 + inward * .1f;
                    kit.Box(Vector3.Min(a0, p1) + Vector3.up * (fy - SectionDepth), Vector3.Max(a0, p1) + Vector3.up * (fy - FloorThick), SectionMat, false);
                }
                return;
            }
            if (!(occA && occB)) return;
            // Same room: level faces, guards, soffits.
            if (Mathf.Abs(fa - fb) > 1e-3f)
            {
                var lowIsA = fa < fb;
                var lo = Y0 + Mathf.Min(fa, fb);
                var hi = Y0 + Mathf.Max(fa, fb);
                var intoLow = lowIsA ? -toB : toB;
                    kit.Mesh.Ceiling = Y0 + Mathf.Max(ca, cb);
                kit.Mesh.FloorGrimeOff = Mathf.Abs(lo) > .01f;
                if (LedgeMat != null)
                {
                    var len = Vector3.Distance(p0, p1);
                    var dirEdge = (p1 - p0) / len;
                    foreach (var sp in Spans(len, cross))
                    {
                        var q0 = p0 + dirEdge * sp.x;
                        var q1 = p0 + dirEdge * sp.y;
                        var f0 = q0 + intoLow * .1f;
                        if (kit.isNew) kit.LedgeNew(Vector3.Min(f0, q1) + Vector3.up * lo, Vector3.Max(f0, q1) + Vector3.up * hi, LedgeMat, FloorMat, FloorThick, q0, q1, intoLow);
                        else kit.Box(Vector3.Min(f0, q1) + Vector3.up * lo, Vector3.Max(f0, q1) + Vector3.up * hi, LedgeMat, true);
                    }
                }
                var drop = hi - lo;
                var style = guards.TryGetValue(key, out var gs) ? gs : (drop > GuardDrop ? LedgeGuard : GuardStyle.None);
                if (style != GuardStyle.None) GuardSpans(kit, p0, p1, -intoLow, hi, style, cross, s0);
            }
            if (Mathf.Abs(ca - cb) > 1e-3f && CeilingMat != null)
            {
                var lo = Y0 + Mathf.Min(ca, cb);
                var hi = Y0 + Mathf.Max(ca, cb);
                kit.Box(Vector3.Min(p0, p1) + Vector3.up * lo - (axis == 0 ? Vector3.right : Vector3.forward) * .04f,
                    Vector3.Max(p0, p1) + Vector3.up * (hi + LidSlab) + (axis == 0 ? Vector3.right : Vector3.forward) * .04f, WallMat != null ? WallMat : CeilingMat, false);
            }
        }

        // A guard along the edge, inset into the floor side, leaving out the crossed spans.
        void GuardSpans(FrontRoomsStructuresKit kit, Vector3 p0, Vector3 p1, Vector3 intoFloor, float floorY, GuardStyle style, List<Vector2> cross, float s0)
        {
            if (style == GuardStyle.None) return;
            var len = Vector3.Distance(p0, p1);
            var t = (p1 - p0) / len;
            var inset = intoFloor * (style == GuardStyle.TileWall ? .1f : .08f);
            foreach (var sp in Spans(len, cross))
            {
                if (sp.y - sp.x < .05f) continue;
                kit.Guard(p0 + t * sp.x + inset, p0 + t * sp.y + inset, floorY, style, GuardWall, GuardCap);
            }
        }

        static List<Vector2> Spans(float len, List<Vector2> cross)
        {
            var spans = new List<Vector2> { new Vector2(0f, len) };
            if (cross != null)
                foreach (var cr in cross) spans = Cut(spans, cr.x, cr.y);
            return spans;
        }

        void BuildRun(FrontRoomsStructuresKit kit, RunSpec r)
        {
            int ai, aj, bi, bj;
            if (r.axis == 0) { ai = r.i - 1; aj = r.j; bi = r.i; bj = r.j; }
            else { ai = r.i; aj = r.j - 1; bi = r.i; bj = r.j; }
            if (!In(ai, aj) || !In(bi, bj)) return;
            var fa = floors[Idx(ai, aj)];
            var fb = floors[Idx(bi, bj)];
            if (Mathf.Abs(fa - fb) < 1e-3f) return;
            var cs = Cell;
            var at = r.axis == 0 ? X0 + r.i * cs : Z0 + r.j * cs;
            var s0 = (r.axis == 0 ? Z0 + r.j * cs : X0 + r.i * cs);
            var mid = s0 + (r.from + r.to) * .5f;
            var edge = r.axis == 0 ? new Vector3(at, 0f, mid) : new Vector3(mid, 0f, at);
            var toB = r.axis == 0 ? Vector3.right : Vector3.forward;
            var upDir = fb > fa ? toB : -toB;
            // The top riser stands on the edge, inside the lower cell.
            kit.StepRun(edge, upDir, r.to - r.from, Y0 + Mathf.Min(fa, fb), Y0 + Mathf.Max(fa, fb), TreadMat != null ? TreadMat : FloorMat, NosingMat);
            if (!RunCheeks) return;
            var lo = Y0 + Mathf.Min(fa, fb);
            var hi = Y0 + Mathf.Max(fa, fb);
            var n = Mathf.Max(1, Mathf.RoundToInt((hi - lo) / Course));
            var riser = (hi - lo) / n;
            var along = r.axis == 0 ? Vector3.forward : Vector3.right;
            var lineStart = r.axis == 0 ? new Vector3(at, 0f, s0) : new Vector3(s0, 0f, at);
            var thick = LedgeGuard == GuardStyle.HalfWall ? .16f : .2f;
            var cheek = LedgeMat != null ? LedgeMat : (GuardWall != null ? GuardWall : FloorMat);
            var firstHigh = Mathf.FloorToInt(GuardDrop / riser + 1e-4f) + 1;
            for (var k = 0; k < 2; k++)
            {
                var open = k == 0 ? r.from > .05f : r.to < cs - .05f;
                if (!open) continue;
                // Outside the run, 2 mm clear of its side (the ledge face's cut end stays in front there).
                var sAt = k == 0 ? r.from - thick * .5f - .002f : r.to + thick * .5f + .002f;
                var top = lineStart + along * sAt;
                var foot = top - upDir * ((n - 1) * Step);
                foot.y = lo + riser;
                top.y = hi;
                kit.RakeWall(foot, top, .12f, lo, lo, thick, cheek, false);
                if (firstHigh > n - 1 || LedgeGuard == GuardStyle.None) continue;
                var g = top - upDir * ((n - firstHigh) * Step);
                g.y = lo + firstHigh * riser;
                kit.RakingGuard(g, top, g.y + .12f, hi + .12f, LedgeGuard, GuardWall, GuardCap);
            }
        }
    }
}
