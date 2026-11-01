// LEVEL SPACE SHOTS, round 4: larger stairs (research/level_space/60_large_stairs.md).
// CLONE ONLY (W/proj_levelspace); never lands in main. ASCII only, no C# tuples, no GetInstanceID.
// Scenes V..Y: the three stair sizes side by side (B4-S post spiral, B4-M helical round an open well, B4-L circular stair),
// B4-M in the split suite and the file mezzanine, B4-M from a pool deck to a gallery, B4-L as the pool hall's arrival.
// Run with -zlZone levelspace -zlOption r4s (or letters V..Y). Output: W/level_space/renders/levelspace/<option>/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public static partial class FrontRoomsZoneLampsShots
{
    static bool LsWant4(string k) { return Opt == "r4s" || Opt.Contains(k); }

    // B4-M: an open well of 1.6 m, clear width 1.3 m, ribbons 0.08 m (outside 4.36 m, a 2 x 2 cell module).
    // B4-L: inner radius 2.95 m (at least twice the width), clear width 1.45 m, ribbons 0.10 m (outside 9.0 m, 3 x 3 cells).
    // The walk line lies 0.30 m (12 in) in from the narrow end; the going on it is 0.28 m (M) and 0.33 m (L).
    const float HxMIn = .8f, HxMOut = 2.1f, HxMRib = .08f, HxLIn = 2.95f, HxLOut = 4.4f, HxLRib = .10f;
    const float HxWalk = .30f, HxGuard = 1.07f, HxRailH = .86f;
    static Material hxOak, hxSteel, hxStain, hxBlue, hxNose;
    static readonly Color HxWarm = new Color(1f, .94f, .84f);

    static void HxInit()
    {
        if (hxOak != null) return;
        hxOak = SurfMat("Prop_WoodOak");
        hxSteel = SurfMat("Prop_SteelPutty");
        hxStain = SurfMat("Prop_StainlessSatin");
        hxBlue = SurfMat("Prop_PlasticBlue");
        hxNose = SurfMat("Station_CrashRail");
    }

    // ------------------------------------------------------------------------------------------ helical stair helpers
    sealed class HxBuf
    {
        public readonly List<Vector3> V = new List<Vector3>();
        public readonly List<Vector2> U = new List<Vector2>();
        public readonly List<int> T = new List<int>();

        // Quads between polylines p and q of equal length; the front faces Cross(p[k+1] - p[k], q[k] - p[k]).
        public void Strip(List<Vector3> p, List<Vector3> q, List<Vector2> up, List<Vector2> uq)
        {
            var b = V.Count;
            var n = p.Count;
            V.AddRange(p); V.AddRange(q); U.AddRange(up); U.AddRange(uq);
            for (var k = 0; k + 1 < n; k++)
            {
                T.Add(b + k); T.Add(b + k + 1); T.Add(b + n + k + 1);
                T.Add(b + k); T.Add(b + n + k + 1); T.Add(b + n + k);
            }
        }

        // One quad a, b, c, d (in order round its edge), wound so that its front faces `want`.
        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 want)
        {
            var i = V.Count;
            V.Add(a); V.Add(b); V.Add(c); V.Add(d);
            U.Add(new Vector2(a.x + a.z, a.y) / 1.2f); U.Add(new Vector2(b.x + b.z, b.y) / 1.2f);
            U.Add(new Vector2(c.x + c.z, c.y) / 1.2f); U.Add(new Vector2(d.x + d.z, d.y) / 1.2f);
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), want) >= 0f) { T.Add(i); T.Add(i + 1); T.Add(i + 2); T.Add(i); T.Add(i + 2); T.Add(i + 3); }
            else { T.Add(i); T.Add(i + 2); T.Add(i + 1); T.Add(i); T.Add(i + 3); T.Add(i + 2); }
        }
    }

    static void HxEmit(string name, HxBuf b, Material m)
    {
        if (b.V.Count == 0 || m == null) return;
        var mesh = new Mesh { name = name };
        mesh.indexFormat = IndexFormat.UInt32;
        mesh.SetVertices(b.V);
        mesh.SetUVs(0, b.U);
        mesh.SetTriangles(b.T, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        var go = new GameObject(name);
        go.transform.SetParent(lsRoot, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = ShadowCastingMode.On;
    }

    static Vector3 HxP(float cx, float cz, float r, float deg, float y)
    {
        var a = deg * Mathf.Deg2Rad;
        return new Vector3(cx + r * Mathf.Cos(a), y, cz + r * Mathf.Sin(a));
    }

    static List<Vector2> HxXZ(List<Vector3> p)
    {
        var u = new List<Vector2>(p.Count);
        foreach (var v in p) u.Add(new Vector2(v.x, v.z) / 1.2f);
        return u;
    }

    // Degrees per tread for a going measured on the walk line; degrees for a landing measured on the centre line.
    static float HxDeg(float rIn, float going) { return Mathf.Rad2Deg * going / (rIn + HxWalk); }
    static float HxArcDeg(float rIn, float rOut, float len) { return Mathf.Rad2Deg * len / ((rIn + rOut) * .5f); }

    // Progress (degrees from the first riser) of every riser; the tread after riser landAfter is a landing of landDeg.
    static float[] HxRisers(int n, float dDeg, int landAfter, float landDeg)
    {
        var P = new float[n];
        for (var k = 1; k < n; k++) P[k] = P[k - 1] + (k == landAfter ? landDeg : dDeg);
        return P;
    }

    static float HxSweep(int n, float dDeg, int landAfter, float landDeg) { return HxRisers(n, dDeg, landAfter, landDeg)[n - 1]; }

    // The nosing line: through the front edge of every tread.
    static float HxNose(float[] P, float yBot, float rise, float phi)
    {
        var n = P.Length;
        if (phi <= P[0]) return yBot + rise;
        for (var k = 0; k + 1 < n; k++)
            if (phi <= P[k + 1]) return yBot + rise * (k + 1 + (phi - P[k]) / Mathf.Max(1e-4f, P[k + 1] - P[k]));
        return yBot + rise * n;
    }

    // A flat annular sector (facing up) between angles lo < hi.
    static void HxSector(HxBuf b, float cx, float cz, float r1, float r2, float lo, float hi, float y)
    {
        var m = Mathf.Max(1, Mathf.CeilToInt((hi - lo) / 4f));
        var p = new List<Vector3>();
        var q = new List<Vector3>();
        for (var j = 0; j <= m; j++)
        {
            var th = lo + (hi - lo) * j / m;
            p.Add(HxP(cx, cz, r1, th, y));
            q.Add(HxP(cx, cz, r2, th, y));
        }
        b.Strip(p, q, HxXZ(p), HxXZ(q));
    }

    // A closed band between radii r1 < r2 along ascending angles th, from yb[i] to yt[i].
    static void HxBand(HxBuf b, float cx, float cz, float r1, float r2, List<float> th, List<float> yb, List<float> yt)
    {
        var n = th.Count;
        var ib = new List<Vector3>(); var it = new List<Vector3>(); var ob = new List<Vector3>(); var ot = new List<Vector3>();
        var uib = new List<Vector2>(); var uit = new List<Vector2>(); var uob = new List<Vector2>(); var uot = new List<Vector2>();
        for (var i = 0; i < n; i++)
        {
            var a = th[i] * Mathf.Deg2Rad;
            ib.Add(HxP(cx, cz, r1, th[i], yb[i])); it.Add(HxP(cx, cz, r1, th[i], yt[i]));
            ob.Add(HxP(cx, cz, r2, th[i], yb[i])); ot.Add(HxP(cx, cz, r2, th[i], yt[i]));
            uib.Add(new Vector2(r1 * a, yb[i]) / 1.2f); uit.Add(new Vector2(r1 * a, yt[i]) / 1.2f);
            uob.Add(new Vector2(r2 * a, yb[i]) / 1.2f); uot.Add(new Vector2(r2 * a, yt[i]) / 1.2f);
        }
        b.Strip(ot, ob, uot, uob);
        b.Strip(ib, it, uib, uit);
        b.Strip(it, ot, HxXZ(it), HxXZ(ot));
        b.Strip(ob, ib, HxXZ(ob), HxXZ(ib));
        var e0t = new List<Vector3> { it[0], ot[0] }; var e0b = new List<Vector3> { ib[0], ob[0] };
        var e1t = new List<Vector3> { it[n - 1], ot[n - 1] }; var e1b = new List<Vector3> { ib[n - 1], ob[n - 1] };
        var u0t = new List<Vector2> { new Vector2(r1, yt[0]) / 1.2f, new Vector2(r2, yt[0]) / 1.2f };
        var u0b = new List<Vector2> { new Vector2(r1, yb[0]) / 1.2f, new Vector2(r2, yb[0]) / 1.2f };
        var u1t = new List<Vector2> { new Vector2(r1, yt[n - 1]) / 1.2f, new Vector2(r2, yt[n - 1]) / 1.2f };
        var u1b = new List<Vector2> { new Vector2(r1, yb[n - 1]) / 1.2f, new Vector2(r2, yb[n - 1]) / 1.2f };
        b.Strip(e0t, e0b, u0t, u0b);
        b.Strip(e1b, e1t, u1b, u1t);
    }

    static List<float> HxAdd(List<float> y, float d)
    {
        var o = new List<float>(y.Count);
        foreach (var v in y) o.Add(v + d);
        return o;
    }

    // A helical stair round an open well centred on (cx, cz): n risers from yBot to yTop, treads between rIn and rOut, dDeg per tread,
    // the first riser at a0 degrees (+x toward +z); hand +1 climbs counter-clockwise seen from above, -1 clockwise.
    // landAfter > 0 makes the tread after that riser a landing of landDeg. riser: the riser faces, or null for the tread material.
    // guard: a solid ribbon in that material, glass when it is mGlass, balusters when null. rails: handrails at 0.86 m in the cap
    // material. Returns the angle of the last riser.
    static float HxStair(float cx, float cz, float yBot, float yTop, float rIn, float rOut, float rib, int n, float dDeg, float a0, int hand,
        int landAfter, float landDeg, Material tread, Material riser, Material body, Material guard, Material cap, bool rails)
    {
        var rise = (yTop - yBot) / n;
        var P = HxRisers(n, dDeg, landAfter, landDeg);
        var end = P[n - 1];
        var d = rise + .15f;
        var tb = new HxBuf();
        var rsb = riser != null && riser != tread ? new HxBuf() : tb;
        for (var k = 0; k < n; k++)
        {
            var th = a0 + hand * P[k];
            float y0 = yBot + k * rise, y1 = y0 + rise;
            var bot = new List<Vector3> { HxP(cx, cz, rIn, th, y0), HxP(cx, cz, rOut, th, y0) };
            var top = new List<Vector3> { HxP(cx, cz, rIn, th, y1), HxP(cx, cz, rOut, th, y1) };
            var ub = new List<Vector2> { new Vector2(rIn, y0) / 1.2f, new Vector2(rOut, y0) / 1.2f };
            var ut = new List<Vector2> { new Vector2(rIn, y1) / 1.2f, new Vector2(rOut, y1) / 1.2f };
            if (hand > 0) rsb.Strip(top, bot, ut, ub); else rsb.Strip(bot, top, ub, ut);
            if (k == n - 1) break;
            float ta = a0 + hand * P[k], tz = a0 + hand * P[k + 1];
            HxSector(tb, cx, cz, rIn, rOut, Mathf.Min(ta, tz), Mathf.Max(ta, tz), y1);
        }
        HxEmit("helix treads", tb, tread);
        if (rsb != tb) HxEmit("helix risers", rsb, riser);

        var ph = new List<float>();
        for (var k = 0; k + 1 < n; k++)
        {
            var m = Mathf.Max(1, Mathf.CeilToInt((P[k + 1] - P[k]) / 3f));
            for (var j = 0; j < m; j++) ph.Add(P[k] + (P[k + 1] - P[k]) * j / m);
        }
        ph.Add(end);
        if (hand < 0) ph.Reverse();
        var ang = new List<float>(); var yN = new List<float>(); var yS = new List<float>(); var yR = new List<float>();
        foreach (var f in ph)
        {
            ang.Add(a0 + hand * f);
            var y = HxNose(P, yBot, rise, f);
            yN.Add(y);
            yS.Add(Mathf.Max(yBot + .003f, y - d));
            yR.Add(Mathf.Max(yBot, y - d - .06f));
        }

        var bb = new HxBuf();
        var sp = new List<Vector3>();
        var sq = new List<Vector3>();
        for (var i = 0; i < ang.Count; i++) { sp.Add(HxP(cx, cz, rOut, ang[i], yS[i])); sq.Add(HxP(cx, cz, rIn, ang[i], yS[i])); }
        bb.Strip(sp, sq, HxXZ(sp), HxXZ(sq));

        var gb = new HxBuf();
        var glb = new HxBuf();
        var cb = new HxBuf();
        for (var side = 0; side < 2; side++)
        {
            float r1 = side == 0 ? rIn - rib : rOut, r2 = side == 0 ? rIn : rOut + rib;
            if (guard != null && guard != mGlass)
            {
                HxBand(gb, cx, cz, r1, r2, ang, yR, HxAdd(yN, HxGuard - .02f));
                HxBand(cb, cx, cz, r1 - .012f, r2 + .012f, ang, HxAdd(yN, HxGuard - .05f), HxAdd(yN, HxGuard));
                continue;
            }
            HxBand(bb, cx, cz, r1, r2, ang, yR, HxAdd(yN, .10f));
            if (guard == mGlass)
            {
                var rm = (r1 + r2) * .5f;
                HxBand(glb, cx, cz, rm - .006f, rm + .006f, ang, HxAdd(yN, .10f), HxAdd(yN, HxGuard - .05f));
                HxBand(cb, cx, cz, r1, r2, ang, HxAdd(yN, HxGuard - .05f), HxAdd(yN, HxGuard));
                continue;
            }
            var rb = (r1 + r2) * .5f;
            HxBand(cb, cx, cz, rb - .025f, rb + .025f, ang, HxAdd(yN, HxGuard - .05f), HxAdd(yN, HxGuard));
            for (var k = 0; k + 1 < n; k++)
            {
                var f = (P[k] + P[k + 1]) * .5f;
                var y = HxNose(P, yBot, rise, f);
                var o = HxP(cx, cz, rb, a0 + hand * f, 0f);
                Cyl("helix baluster", new Vector3(o.x, y + .1f, o.z), new Vector3(o.x, y + HxGuard - .05f, o.z), .011f, mMetal);
            }
        }
        if (rails)
        {
            HxBand(cb, cx, cz, rIn + .048f, rIn + .092f, ang, HxAdd(yN, HxRailH - .022f), HxAdd(yN, HxRailH + .022f));
            HxBand(cb, cx, cz, rOut - .092f, rOut - .048f, ang, HxAdd(yN, HxRailH - .022f), HxAdd(yN, HxRailH + .022f));
        }
        HxEmit("helix soffit and stringers", bb, body);
        HxEmit("helix ribbons", gb, guard);
        HxEmit("helix glass", glb, mGlass);
        HxEmit("helix caps and rails", cb, cap);
        Log("helix at " + F3(cx) + "," + F3(cz) + " | r " + F3(rIn) + "-" + F3(rOut) + " | " + n + " risers of " + F3(rise)
            + " | " + F3(dDeg) + " deg a tread | sweep " + F3(end) + " deg | last riser at " + F3(a0 + hand * end));
        return a0 + hand * end;
    }

    // Step lights in the inner ribbon: a small lens facing the treads every `every` treads, each with a short spot onto its tread.
    static void HxStepLights(float cx, float cz, float yBot, float yTop, float rIn, int n, float dDeg, float a0, int hand, int landAfter, float landDeg,
        int every, float intensity, Color col)
    {
        var rise = (yTop - yBot) / n;
        var P = HxRisers(n, dDeg, landAfter, landDeg);
        var count = 0;
        for (var k = 1; k + 1 < n; k += every)
        {
            var mid = a0 + hand * (P[k] + P[k + 1]) * .5f;
            var y = yBot + (k + 1) * rise;
            var a = mid * Mathf.Deg2Rad;
            var er = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            var et = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a));
            var c = HxP(cx, cz, rIn + .004f, mid, y + .26f);
            Fq("step light", c - et * .11f - Vector3.up * .035f, et * .22f, Vector3.up * .07f, mGlow, 1f).shadowCastingMode = ShadowCastingMode.Off;
            AddSpot("step light", HxP(cx, cz, rIn + .1f, mid, y + .3f), (er - Vector3.up * .9f).normalized, 120f, 80f, intensity, 2.8f, col, false, 1u);
            count++;
        }
        Log("step lights " + count);
    }

    // A dark nosing strip at the front edge of every tread and landing of a helical stair: 50 mm on the tread, 25 mm down the riser.
    static void HxNosings(float cx, float cz, float yBot, float yTop, float rIn, float rOut, int n, float dDeg, float a0, int hand,
        int landAfter, float landDeg, Material m)
    {
        var rise = (yTop - yBot) / n;
        var P = HxRisers(n, dDeg, landAfter, landDeg);
        var b = new HxBuf();
        for (var k = 0; k < n; k++)
        {
            var th = a0 + hand * P[k];
            var a = th * Mathf.Deg2Rad;
            var into = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a)) * hand;
            var y = yBot + (k + 1) * rise;
            var top = Vector3.up * (y + .006f);
            var low = Vector3.up * (y - .025f);
            Vector3 i0 = HxP(cx, cz, rIn, th, 0f), o0 = HxP(cx, cz, rOut, th, 0f), off = -into * .003f;
            b.Quad(i0 + top, o0 + top, o0 + top + into * .05f, i0 + top + into * .05f, Vector3.up);
            b.Quad(i0 + top + off, o0 + top + off, o0 + low + off, i0 + low + off, -into);
        }
        HxEmit("helix nosings", b, m);
    }

    // The same nosing on every step of a straight flight built by FlightZ.
    static void HxFlightNosingZ(float z0, float y0, float run, float rise, float x0, float x1, int n, Material m)
    {
        float t = run / n, r = rise / n;
        var into = new Vector3(0f, 0f, Mathf.Sign(run));
        var b = new HxBuf();
        for (var i = 0; i < n; i++)
        {
            float za = z0 + i * t, y = y0 + (i + 1) * r;
            Vector3 p0 = new Vector3(x0, y + .006f, za), p1 = new Vector3(x1, y + .006f, za), off = -into * .003f;
            b.Quad(p0, p1, p1 + into * .05f, p0 + into * .05f, Vector3.up);
            b.Quad(p0 + off, p1 + off, new Vector3(x1, y - .025f, za) + off, new Vector3(x0, y - .025f, za) + off, -into);
        }
        HxEmit("flight nosings", b, m);
    }

    static void HxPoint(string name, Vector3 p, float intensity, float range, Color col)
    {
        var go = new GameObject(name);
        go.transform.SetParent(lsRoot, false);
        go.transform.localPosition = p;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.intensity = intensity;
        l.range = range;
        l.color = col;
        l.shadows = LightShadows.None;
        l.renderingLayerMask = 1;
    }

    // An opal drum pendant hung down the well's axis.
    static void HxPendant(float x, float z, float yCeil, float yLamp, float intensity, float range, Color col)
    {
        Cyl("pendant rod", new Vector3(x, yLamp + .2f, z), new Vector3(x, yCeil, z), .008f, mMetal);
        Cyl("pendant drum", new Vector3(x, yLamp - .18f, z), new Vector3(x, yLamp + .2f, z), .2f, mGlow);
        HxPoint("pendant light", new Vector3(x, yLamp - .32f, z), intensity, range, col);
    }

    static void HxUplight(float x, float y, float z, Vector3 dir, float intensity, float range, Color col)
    {
        Fq("uplight lens", new Vector3(x - .09f, y + .006f, z - .09f), new Vector3(.18f, 0f, 0f), new Vector3(0f, 0f, .18f), mGlow, 1f).shadowCastingMode = ShadowCastingMode.Off;
        AddSpot("uplight", new Vector3(x, y + .05f, z), dir, 46f, 26f, intensity, range, col, false, 1u);
    }

    // A straight solid parapet 1.07 m high standing on y, along x or along z, thickness t, with a cap.
    static void HxWall(float x0, float z0, float x1, float z1, float y, float t, Material m, Material cap)
    {
        float ax = Mathf.Min(x0, x1), bx = Mathf.Max(x0, x1), az = Mathf.Min(z0, z1), bz = Mathf.Max(z0, z1);
        if (bx - ax < bz - az) { ax -= t * .5f; bx += t * .5f; } else { az -= t * .5f; bz += t * .5f; }
        Box("parapet", new Vector3(ax, y, az), new Vector3(bx, y + HxGuard - .03f, bz), m, 1.2f, 1u, "-y");
        Box("parapet cap", new Vector3(ax - .012f, y + HxGuard - .05f, az - .012f), new Vector3(bx + .012f, y + HxGuard, bz + .012f), cap, 1f, 1u, "-y");
    }

    // A basin with its deck inside the hall rectangle (hx0, hz0, hx1, hz1); water 0.12 below the deck, two lane lines and a ladder.
    static void HxBasin(float x0, float z0, float x1, float z1, float depth, float hx0, float hz0, float hx1, float hz1)
    {
        FloorQ(hx0, hz0, hx1, z0, 0f, mTile); FloorQ(hx0, z1, hx1, hz1, 0f, mTile);
        FloorQ(hx0, z0, x0, z1, 0f, mTile); FloorQ(x1, z0, hx1, z1, 0f, mTile);
        FloorQ(x0, z0, x1, z1, -depth, mTile);
        VoidBand((x0 + x1) * .5f, (z0 + z1) * .5f, (x1 - x0) * .5f, (z1 - z0) * .5f, -depth, 0f, mTile);
        TsWaterQ(x0, z0, x1, z1, -.12f);
        TsDeepQ(x0, z0, x1, z1, -.6f);
        var w = (x1 - x0) / 3f;
        for (var i = 1; i < 3; i++)
            Cyl("lane line", new Vector3(x0 + w * i, -.1f, z0 + .1f), new Vector3(x0 + w * i, -.1f, z1 - .1f), .04f, hxBlue);
        var lx = x0 + 1.2f;
        for (var s = -1; s <= 1; s += 2)
        {
            var x = lx + s * .25f;
            Cyl("ladder rail", new Vector3(x, -1.05f, z1 - .12f), new Vector3(x, .8f, z1 - .12f), .022f, hxStain);
            Cyl("ladder rail", new Vector3(x, .8f, z1 - .12f), new Vector3(x, .8f, z1 + .25f), .022f, hxStain);
            Cyl("ladder rail", new Vector3(x, .8f, z1 + .25f), new Vector3(x, 0f, z1 + .25f), .022f, hxStain);
        }
        for (var r = 0; r < 3; r++)
            Box("ladder step", new Vector3(lx - .25f, -.32f - r * .3f, z1 - .2f), new Vector3(lx + .25f, -.29f - r * .3f, z1 - .04f), hxStain, 1f, 1u, "");
    }

    static void HxHallLamps(float x0, float z0, float x1, float z1, float y, float step, float intensity, float range, Rect skip)
    {
        var first = true;
        for (var x = x0 + step * .5f; x < x1; x += step)
        for (var z = z0 + step * .5f; z < z1; z += step)
        {
            if (skip.width > 0f && skip.Contains(new Vector2(x, z))) continue;
            TsSpot("hall lamp", new Vector3(x, y, z), Vector3.down, 130f, intensity, range, TsCool, first);
            first = false;
        }
    }

    // ================================================================================ V - the three sizes side by side
    static IEnumerator LsSceneV()
    {
        TsInit();
        HxInit();
        LsBegin("V three stair sizes", 6000f);
        const float H = 8f, Y1 = 4f;
        Room(-3f, -12f, 27f, 12f, 0f, H, mWall, mCarpet, mCeil, "");
        TsSolid(-3f, 9f, 27f, 12f, Y1 - .25f, Y1, mFascia, mCarpet);
        // B4-S, today's post spiral: one cell, 17 risers of 0.235 in one turn
        TsSpiral(1.5f, 8.1f, 0f, Y1, .85f, -240f, 17, 330f, mStair3);
        // B4-M: 24 risers of 0.167, the 12 ft landing at mid-height, then north onto a landing to the upper floor
        float dM = HxDeg(HxMIn, .28f), lM = HxArcDeg(HxMIn, HxMOut, 1.3f), sM = HxSweep(24, dM, 12, lM);
        HxStair(9f, 6f, 0f, Y1, HxMIn, HxMOut, HxMRib, 24, dM, -sM, 1, 12, lM, mCarpet, null, mFascia, mFascia, hxOak, true);
        TsSolid(9.8f, 6f, 11.1f, 9f, Y1 - .25f, Y1, mFascia, mCarpet);
        HxWall(9.76f, 6f, 9.76f, 9f, Y1, .08f, mFascia, hxOak);
        HxWall(11.14f, 6f, 11.14f, 9f, Y1, .08f, mFascia, hxOak);
        // B4-L: 27 risers of 0.148, landing after 13
        float dL = HxDeg(HxLIn, .33f), lL = HxArcDeg(HxLIn, HxLOut, 1.45f), sL = HxSweep(27, dL, 13, lL);
        HxStair(19.5f, 4.5f, 0f, Y1, HxLIn, HxLOut, HxLRib, 27, dL, -sL, 1, 13, lL, mCarpet, null, mFascia, mFascia, hxOak, true);
        TsSolid(22.45f, 4.5f, 23.9f, 9f, Y1 - .25f, Y1, mFascia, mCarpet);
        HxWall(22.4f, 4.5f, 22.4f, 9f, Y1, .1f, mFascia, hxOak);
        HxWall(23.95f, 4.5f, 23.95f, 9f, Y1, .1f, mFascia, hxOak);
        GlassGuard(-3f, 9f, 1.05f, 9f, Y1); GlassGuard(1.95f, 9f, 9.7f, 9f, Y1);
        GlassGuard(11.2f, 9f, 22.33f, 9f, Y1); GlassGuard(24.02f, 9f, 27f, 9f, Y1);
        Lamps(-3f, -12f, 27f, 12f, H, mLens, LampL0, 45f, 20f, 6, null);
        Lamps(-3f, 9f, 27f, 12f, Y1 - .26f, mLens, LampL0, 6f, 6f, 0, null);
        Kit("Kit_OfficeDesk", 4.6f, 0f, -1.2f, 0f);
        Kit("Kit_TaskChair", 4.6f, 0f, -.4f, 180f);
        Kit("Kit_WaterCooler", -2.75f, 0f, 2f, 90f);
        Kit("Kit_ClubChair", 6.4f, 0f, 1.4f, 200f);
        yield return 20;
        LsAim(12.5f, 1.62f, -11f, 12.5f, 2.6f, 5f, 56f);
        Shot("v1_three_sizes", "Three sizes, one 4.0 m storey each: B4-S post spiral (1.7 m, 1 cell), B4-M helical round a 1.6 m well (4.36 m, 2 x 2 cells), B4-L circular stair (9.0 m, 3 x 3 cells)");
        LsAim(26f, Y1 + 1.62f, 10.5f, 10f, 0f, 2f, 70f);
        Shot("v2_from_upper_floor", "From the shared upper floor at +4.0, beside the B4-L landing: B4-L below, B4-M and B4-S beyond, all three arriving on the same floor");
        LsEnd();
        yield return 2;
    }

    // ================================================================================ W - B4-M in the split suite and the file mezzanine
    static IEnumerator LsSceneW()
    {
        TsInit();
        HxInit();
        const float H = 5.4f, G = 2.7f;
        float dM = HxDeg(HxMIn, .28f), sM = HxSweep(16, dM, 0, 0f);

        LsBegin("W1 split suite B4-M", 6200f);
        Room(-6f, -4.5f, 9f, 4.5f, 0f, H, mWall, mCarpet, mCeil, "");
        TsSolid(-6f, -4.5f, 3f, -1.5f, 0f, 1.2f, mFascia, mCarpet);
        TsSolid(-6f, 1.5f, 9f, 4.5f, G - .2f, G, mFascia, mCarpet);
        FlightZ(.9f, 0f, -2.4f, 1.2f, -.6f, .6f, 8, mCarpet);
        FlightZ(-1.5f, 1.2f, 3f, 1.5f, -6f, -4.8f, 10, mCarpet);
        HxStair(6f, -1.5f, 0f, G, HxMIn, HxMOut, HxMRib, 16, dM, -sM, 1, 0, 0f, mCarpet, null, mFascia, mFascia, hxOak, true);
        TsSolid(6.8f, -1.5f, 9f, 1.5f, G - .2f, G, mFascia, mCarpet);
        HxWall(6.76f, -1.5f, 6.76f, 1.5f, G, .08f, mFascia, hxOak);
        HxWall(8.14f, -1.5f, 9f, -1.5f, G, .08f, mFascia, hxOak);
        HxWall(-4.8f, 1.5f, 6.72f, 1.5f, G, .08f, mFascia, hxOak);
        HxWall(-4.8f, -1.5f, -.6f, -1.5f, 1.2f, .08f, mFascia, hxOak);
        HxWall(.6f, -1.5f, 3f, -1.5f, 1.2f, .08f, mFascia, hxOak);
        HxWall(3f, -4.5f, 3f, -1.5f, 1.2f, .08f, mFascia, hxOak);
        HxStepLights(6f, -1.5f, 0f, G, HxMIn, 16, dM, -sM, 1, 0, 0f, 3, 2.5f, HxWarm);
        HxPendant(6f, -1.5f, H, 3.4f, 3f, 6f, LampL0);
        Lamps(-6f, -4.5f, 9f, 4.5f, H, mLens, LampL0, 22f, 15f, 3, null);
        Lamps(-6f, 1.5f, 9f, 4.5f, G - .21f, mLens, LampL0, 8f, 8f, 0, null);
        Kit("Kit_OfficeDesk", -2f, G, 3.4f, 180f);
        Kit("Kit_TaskChair", -2f, G, 4.1f, 180f);
        Kit("Kit_Bookcase", 2.5f, G, 4.25f, 180f);
        Kit("Kit_Credenza", -2.5f, 1.2f, -4.1f, 0f);
        Kit("Kit_ClubChair", -.9f, 1.2f, -3.1f, 30f);
        Kit("Kit_ClubChair", 1.4f, 1.2f, -3.2f, -30f);
        Kit("Kit_Sofa3", -3.4f, 0f, -1.05f, 0f);
        Kit("Kit_Torchiere", -5.6f, 0f, 1.2f, 0f);
        yield return 20;
        LsAim(.6f, 1.62f, 1.35f, 6.4f, 1.7f, -1.7f, 78f);
        Shot("w1_suite_b4m", "Level 0 split suite, 15 x 9 m, 5.4 m tall: B4-M from the floor to the +2.7 gallery (16 risers, 219 deg); the flight by the platform is the second way up");
        LsAim(7.75f, G + 1.62f, 1.05f, 5f, .3f, -2.4f, 74f);
        Shot("w2_suite_from_landing", "From the B4-M top landing: down the open well to the floor, the platform beyond");
        LsEnd();
        yield return 2;

        LsBegin("W2 file mezzanine B4-M", 6400f);
        Room(-6f, -4.5f, 9f, 4.5f, 0f, H, mOWall, mOCarpet, mOCeil, "");
        TsSolid(-6f, -4.5f, -1.5f, 4.5f, G - .2f, G, mFascia, mOCarpet);
        for (var z = -3.9f; z < 3.9f; z += .9f)
        {
            Kit("Kit_FilingCabinet", -5.5f, 0f, z, 90f);
            Kit("Kit_FilingCabinet", -3.2f, 0f, z, 270f);
            Kit("Kit_FilingCabinet", -5.5f, G, z, 90f);
            Kit("Kit_FilingCabinet", -3.2f, G, z, 270f);
        }
        HxStair(1.5f, -1.5f, 0f, G, HxMIn, HxMOut, HxMRib, 16, dM, 90f - sM, 1, 0, 0f, mOCarpet, null, hxSteel, mGlass, hxSteel, true);
        TsSolid(-1.5f, -.7f, 1.5f, .6f, G - .2f, G, mFascia, mOCarpet);
        GlassGuard(-1.5f, .64f, 1.5f, .64f, G); GlassGuard(-1.5f, -.74f, 1.5f, -.74f, G);
        GlassGuard(-1.5f, -4.5f, -1.5f, -.74f, G); GlassGuard(-1.5f, .64f, -1.5f, 3.3f, G);
        FlightX(4f, 0f, -5.4f, G, 3.3f, 4.5f, 18, mOCarpet);
        Cluster(5.6f, -4.2f, 0f);
        SetPanels(1.65f);
        Kit("Kit_Copier", 8.4f, 0f, 1.6f, 270f);
        Kit("Kit_StepStool", -2.4f, 0f, 1.9f, 20f);
        HxStepLights(1.5f, -1.5f, 0f, G, HxMIn, 16, dM, 90f - sM, 1, 0, 0f, 3, 2.5f, LampOf);
        TsSpot("well downlight", new Vector3(1.5f, H - .05f, -1.5f), Vector3.down, 40f, 18f, 9f, LampOf, true);
        Lamps(-6f, -4.5f, 9f, 4.5f, H, mLensCool, LampOf, 20f, 14f, 3, null);
        Lamps(-6f, -4.5f, -1.5f, 4.5f, G - .21f, mLensCool, LampOf, 7f, 7f, 0, null);
        yield return 20;
        LsAim(7.2f, 1.62f, 1.2f, 1f, 1.7f, -1.8f, -1f);
        Shot("w3_mezzanine_b4m", "Office file mezzanine, 15 x 9 m, 5.4 m tall: B4-M with glass ribbons to the +2.7 mezzanine; the straight flight along the wall is the Relay's way up");
        LsAim(.2f, G + 1.62f, -.05f, 3.4f, .7f, -3.3f, 74f);
        Shot("w4_mezzanine_from_landing", "From the B4-M top landing: through the glass down the well, the cubicle field beyond at 1.65 m panels");
        LsEnd();
        yield return 2;
    }

    // ================================================================================ X - B4-M from a pool deck to a gallery
    static IEnumerator LsSceneX()
    {
        TsInit();
        HxInit();
        LsBegin("X pool gallery B4-M", 6600f);
        TsPoolLook(true);
        const float H = 6f, G = 3f;
        WallX(-9f, 0f, 15f, 0f, H, 1, mTile); WallX(9f, 0f, 15f, 0f, H, -1, mTile);
        WallZ(0f, -9f, 9f, 0f, H, 1, mTile); WallZ(15f, -9f, 9f, 0f, H, -1, mTile);
        CeilQ(-9f, 0f, 9f, 15f, H, mTile);
        HxBasin(-6f, 1.5f, 1.5f, 10.5f, 1.35f, -9f, 0f, 9f, 15f);
        TsSolid(-9f, 12f, 9f, 15f, G - .25f, G, mTile, mTile);
        float dM = HxDeg(HxMIn, .28f), sM = HxSweep(18, dM, 0, 0f);
        HxStair(6f, 9f, 0f, G, HxMIn, HxMOut, HxMRib, 18, dM, -sM, 1, 0, 0f, mTile, null, mTile, mTile, hxStain, true);
        HxNosings(6f, 9f, 0f, G, HxMIn, HxMOut, 18, dM, -sM, 1, 0, 0f, hxNose);
        TsSolid(6.8f, 9f, 9f, 12f, G - .25f, G, mTile, mTile);
        HxWall(6.76f, 9f, 6.76f, 12f, G, .08f, mTile, hxStain);
        HxWall(8.14f, 9f, 9f, 9f, G, .08f, mTile, hxStain);
        GlassGuard(-7.8f, 12f, 6.72f, 12f, G);
        FlightZ(6.6f, 0f, 5.4f, G, -9f, -7.8f, 18, mTile);
        HxFlightNosingZ(6.6f, 0f, 5.4f, G, -9f, -7.8f, 18, hxNose);
        HxStepLights(6f, 9f, 0f, G, HxMIn, 18, dM, -sM, 1, 0, 0f, 3, 2.5f, TsCool);
        HxUplight(6f, 0f, 9f, Vector3.up, 10f, 8f, TsCool);
        for (var i = 0; i < 3; i++)
        {
            var a = (60f + 120f * i) * Mathf.Deg2Rad;
            var er = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            HxUplight(6f + .45f * er.x, 0f, 9f + .45f * er.z, (Vector3.up + er * .35f).normalized, 7f, 6f, TsCool);
        }
        HxHallLamps(-9f, 0f, 9f, 12f, H - .1f, 6f, 45f, 14f, new Rect(0f, 0f, 0f, 0f));
        foreach (var x in new[] { -6f, 0f, 6f }) TsSpot("gallery lamp", new Vector3(x, G - .3f, 13.5f), Vector3.down, 140f, 8f, 6f, TsCool, false);
        foreach (var p in new[] { new Vector3(-4f, -1.2f, 4f), new Vector3(-.5f, -1.2f, 8f), new Vector3(-3f, -1.2f, 7f) })
            TsSpot("underwater", p, Vector3.up, 120f, 12f, 7f, TsUnder, false);
        yield return 20;
        LsAim(2.4f, 1.62f, 3f, 6f, 1.8f, 9.2f, -1f);
        Shot("x1_pool_deck_b4m", "Pool hall, 6 m tall: B4-M in porcelain white tile from the deck to the +3.0 gallery (18 risers, 248 deg); the straight flight on the far wall is the second way up");
        LsAim(7.45f, G + 1.62f, 11f, 6.2f, .6f, 6.6f, 78f);
        Shot("x2_pool_from_landing", "From the B4-M top landing: dark nosing strips mark every step as the tiled ribbon winds down to the deck");
        LsAim(-2f, G + 1.62f, 13.2f, 6f, 1.4f, 9f, -1f);
        Shot("x3_from_the_gallery", "From the +3.0 gallery: the whole helix, its 1.6 m eye open to the deck and lit from the floor of the eye, the pool below");
        TsPoolLook(false);
        LsEnd();
        yield return 2;
    }

    // ================================================================================ Y - B4-L as the pool hall's arrival
    static IEnumerator LsSceneY()
    {
        TsInit();
        HxInit();
        LsBegin("Y pool hall arrival B4-L", 6800f);
        TsPoolLook(true);
        const float H = 8.4f, Y1 = 4f;
        WallX(-12f, 0f, 24f, 0f, H, 1, mTile); WallX(12f, 0f, 24f, 0f, H, -1, mTile); WallZ(24f, -12f, 12f, 0f, H, -1, mTile);
        WallZ(0f, -12f, -3f, 0f, H, 1, mTile); WallZ(0f, 0f, 12f, 0f, H, 1, mTile);
        WallZ(0f, -3f, 0f, 0f, Y1, 1, mTile); WallZ(0f, -3f, 0f, Y1 + LsCH, H, 1, mTile);
        Room(-3f, -9f, 0f, 0f, Y1, LsCH, mWall, mCarpet, mCeil, "N");
        Lamps(-3f, -9f, 0f, 0f, Y1 + LsCH, mLens, LampL0, 9f, 9f, 2, null);
        var hole = Rect.MinMaxRect(-7.5f, 4.5f, -1.5f, 10.5f);
        CeilHoles(Rect.MinMaxRect(-12f, 0f, 12f, 24f), new List<Rect> { hole }, H, mTile);
        VoidBand(-4.5f, 7.5f, 3f, 3f, H, H + .8f, mTile);
        Fq("laylight", new Vector3(-7.5f, H + .8f, 4.5f), new Vector3(0f, 0f, 6f), new Vector3(6f, 0f, 0f), mGlow, 1f).shadowCastingMode = ShadowCastingMode.Off;
        HxBasin(1.5f, 7.5f, 9f, 22.5f, 1.5f, -12f, 0f, 12f, 24f);
        TsSolid(-12f, 0f, 12f, 3f, Y1 - .25f, Y1, mTile, mTile);
        float dL = HxDeg(HxLIn, .33f), lL = HxArcDeg(HxLIn, HxLOut, 1.45f), sL = HxSweep(27, dL, 13, lL);
        HxStair(-4.5f, 7.5f, 0f, Y1, HxLIn, HxLOut, HxLRib, 27, dL, -90f - sL, 1, 13, lL, mTile, null, mTile, mTile, hxStain, true);
        HxNosings(-4.5f, 7.5f, 0f, Y1, HxLIn, HxLOut, 27, dL, -90f - sL, 1, 13, lL, hxNose);
        TsSolid(-4.5f, 3f, 0f, 4.55f, Y1 - .25f, Y1, mTile, mTile);
        HxWall(-4.5f, 4.6f, 0f, 4.6f, Y1, .1f, mTile, hxStain);
        HxWall(.05f, 3f, .05f, 4.65f, Y1, .1f, mTile, hxStain);
        HxWall(-12f, 3f, -4.5f, 3f, Y1, .1f, mTile, hxStain);
        HxWall(.1f, 3f, 10.8f, 3f, Y1, .1f, mTile, hxStain);
        FlightZ(12.9f, 0f, -4.2f, 2f, 10.8f, 12f, 14, mTile);
        TsSolid(10.8f, 7.2f, 12f, 8.7f, 1.75f, 2f, mTile, mTile);
        FlightZ(7.2f, 2f, -4.2f, 2f, 10.8f, 12f, 14, mTile);
        HxFlightNosingZ(12.9f, 0f, -4.2f, 2f, 10.8f, 12f, 14, hxNose);
        HxFlightNosingZ(7.2f, 2f, -4.2f, 2f, 10.8f, 12f, 14, hxNose);
        HxStepLights(-4.5f, 7.5f, 0f, Y1, HxLIn, 27, dL, -90f - sL, 1, 13, lL, 3, 3f, TsCool);
        for (var i = 0; i < 6; i++)
        {
            var a = (30f + 60f * i) * Mathf.Deg2Rad;
            var er = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            HxUplight(-4.5f + 2.45f * er.x, 0f, 7.5f + 2.45f * er.z, (Vector3.up + er * .3f).normalized, 8f, 7f, TsCool);
        }
        TsSpot("laylight", new Vector3(-4.5f, H + .7f, 7.5f), Vector3.down, 75f, 70f, 16f, new Color(1f, .98f, .94f), true);
        HxHallLamps(-12f, 0f, 12f, 24f, H - .1f, 6f, 60f, 18f, hole);
        foreach (var x in new[] { -9f, -3f, 3f, 9f }) TsSpot("balcony soffit lamp", new Vector3(x, Y1 - .32f, 1.5f), Vector3.down, 140f, 9f, 6f, TsCool, false);
        foreach (var p in new[] { new Vector3(3f, -1.35f, 11f), new Vector3(7f, -1.35f, 16f), new Vector3(4f, -1.35f, 20.5f), new Vector3(6.5f, -1.35f, 9f) })
            TsSpot("underwater", p, Vector3.up, 120f, 12f, 7f, TsUnder, false);
        yield return 20;
        LsAim(-4.2f, Y1 + 1.62f, 3.8f, -6f, .8f, 9f, 80f);
        Shot("y1_arrival_from_top", "Arriving from the corridor at +4.0: B4-L in tile winds down 168 deg to the deck (27 risers of 0.148, a landing after 13); the pool to the right");
        LsAim(-1f, 1.62f, 18.5f, -4.5f, 2f, 7.5f, 62f);
        Shot("y2_hall_from_deck", "From the deck: the pool, then B4-L in porcelain white tile with dark nosings under its laylight, and the arrival balcony; the flights by the east wall are the second way down");
        LsAim(-8.15f, 1.926f + 1.62f, 7.5f, 2f, 0f, 10.5f, -1f);
        Shot("y3_from_mid_landing", "On the mid landing at +1.93 (the 12 ft rule): across the 5.9 m well to the pool");
        LsAim(-2.6f, 1.62f, 9.4f, -6.5f, 3f, 5f, 80f);
        Shot("y4_inside_the_well", "Inside the well of B4-L: the ribbon rises round you toward the balcony; uplights graze the inner ribbon, the laylight is overhead");
        TsPoolLook(false);
        LsEnd();
        yield return 2;
    }
}
