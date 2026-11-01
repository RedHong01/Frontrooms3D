// LEVEL SPACE SHOTS, round 3: terrain splices (research/level_space/40_terrain_grammar.md).
// CLONE ONLY (W/proj_levelspace); never lands in main. ASCII only, no C# tuples, no GetInstanceID.
// Scenes O..U: the well without symmetry, the pool route, the slide tower, the casbah cluster, Level 0 terrain modules,
// Office terrain modules, Run! thresholds and the dock release. Run with -zlZone levelspace -zlOption r3 (or letters O..U).
// Output: W/level_space/renders/levelspace/<option>/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static partial class FrontRoomsZoneLampsShots
{
    static bool LsWant3(string k) { return Opt == "r3" || Opt.Contains(k); }

    static Material mWater3, mDeep3, mFall3, mRaised3, mCab3, mSlide3, mBump3, mStair3;
    static readonly Color TsCool = new Color(.92f, .97f, 1f), TsUnder = new Color(.35f, .95f, .92f);

    static void TsInit()
    {
        if (mWater3 != null) return;
        mWater3 = TsWater(new Color(.20f, .72f, .74f, .40f));
        mDeep3 = TsWater(new Color(.05f, .42f, .46f, .35f));
        mFall3 = TsWater(new Color(.75f, .95f, .95f, .45f));
        mRaised3 = SurfMat("Prop_LaminateGrey");
        mCab3 = SurfMat("Prop_SteelBeige");
        mSlide3 = SurfMat("Prop_PlasticBlue");
        mBump3 = SurfMat("RunB_BumperVinyl");
        mStair3 = SurfMat("Stair_Slab");
    }

    static Material TsWater(Color c)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "LS3 water" };
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)RenderQueue.Transparent;
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", .95f);
        m.SetFloat("_Metallic", 0f);
        return m;
    }

    // The pool family's light (the main K chat's F1 values): cool ambient and fog, a cooler white balance.
    static void TsPoolLook(bool on)
    {
        if (!on) { FrontRoomsLook.ApplyAmbient(); return; }
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.15f, .20f, .22f);
        RenderSettings.ambientEquatorColor = new Color(.17f, .23f, .24f);
        RenderSettings.ambientGroundColor = new Color(.21f, .28f, .28f);
        RenderSettings.ambientLight = RenderSettings.ambientSkyColor;
        RenderSettings.fogColor = new Color(.11f, .16f, .17f);
        RenderSettings.fogDensity = .010f;
        var go = new GameObject("LS3 pool grade");
        go.transform.SetParent(lsRoot, false);
        var vol = go.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.priority = 100f;
        vol.weight = 1f;
        var p = ScriptableObject.CreateInstance<VolumeProfile>();
        var wb = p.Add<WhiteBalance>(true);
        wb.temperature.Override(-14f);
        wb.tint.Override(-2f);
        vol.sharedProfile = p;
    }

    // ------------------------------------------------------------------------------------------ helpers
    static GameObject TsBox(string name, Vector3 c, Vector3 size, float yaw, Material m)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.DestroyImmediate(g.GetComponent<Collider>());
        g.name = name;
        g.transform.SetParent(lsRoot, false);
        g.transform.localPosition = c;
        g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        g.transform.localScale = size;
        g.GetComponent<Renderer>().sharedMaterial = m;
        return g;
    }

    static void TsBall(Vector3 c, float d, Material m)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Object.DestroyImmediate(g.GetComponent<Collider>());
        g.name = "ball";
        g.transform.SetParent(lsRoot, false);
        g.transform.localPosition = c;
        g.transform.localScale = Vector3.one * d;
        g.GetComponent<Renderer>().sharedMaterial = m;
    }

    // An axis-aligned solid with world-UV faces; a top material covers it with a floor.
    static void TsSolid(float x0, float z0, float x1, float z1, float y0, float y1, Material side, Material top)
    {
        Box("solid", new Vector3(x0, y0, z0), new Vector3(x1, y1, z1), side, 1.2f, 1u, top != null ? "+y" : "");
        if (top != null) FloorQ(x0, z0, x1, z1, y1 + .003f, top);
    }

    static void TsWaterQ(float x0, float z0, float x1, float z1, float y)
    {
        var r = Fq("water", new Vector3(x0, y, z0), new Vector3(x1 - x0, 0f, 0f), new Vector3(0f, 0f, z1 - z0), mWater3, 1f);
        r.shadowCastingMode = ShadowCastingMode.Off;
    }

    static void TsDeepQ(float x0, float z0, float x1, float z1, float y)
    {
        var r = Fq("deep water", new Vector3(x0, y, z0), new Vector3(x1 - x0, 0f, 0f), new Vector3(0f, 0f, z1 - z0), mDeep3, 1f);
        r.shadowCastingMode = ShadowCastingMode.Off;
    }

    // A vertical sheet seen from both sides: o, o+u, o+u+v, o+v.
    static void TsSheet(Vector3 o, Vector3 u, Vector3 v, Material m)
    {
        Fq("sheet", o, u, v, m, 1f).shadowCastingMode = ShadowCastingMode.Off;
        Fq("sheet", o, v, u, m, 1f).shadowCastingMode = ShadowCastingMode.Off;
    }

    static Renderer TsMesh(string name, List<Vector3> vs, List<int> ts, Material m, bool twoSided)
    {
        var v = new List<Vector3>(vs);
        var t = new List<int>(ts);
        if (twoSided)
        {
            var n = vs.Count;
            v.AddRange(vs);
            for (var i = 0; i + 2 < ts.Count; i += 3) { t.Add(ts[i] + n); t.Add(ts[i + 2] + n); t.Add(ts[i + 1] + n); }
        }
        var mesh = new Mesh { name = name };
        mesh.indexFormat = IndexFormat.UInt32;
        mesh.SetVertices(v);
        mesh.SetTriangles(t, 0);
        mesh.RecalculateNormals();
        var nr = mesh.normals;
        var uv = new Vector2[v.Count];
        for (var i = 0; i < v.Count; i++)
        {
            var a = nr[i];
            var p = v[i];
            float ax = Mathf.Abs(a.x), ay = Mathf.Abs(a.y), az = Mathf.Abs(a.z);
            uv[i] = (ay >= ax && ay >= az ? new Vector2(p.x, p.z) : ax >= az ? new Vector2(p.z, p.y) : new Vector2(p.x, p.y)) / 1.2f;
        }
        mesh.uv = uv;
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        var go = new GameObject(name);
        go.transform.SetParent(lsRoot, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = ShadowCastingMode.On;
        return r;
    }

    // A wall in the plane z with round-headed openings (centres cxs, width w, springing hs), thickness t.
    static void TsArchZ(float z, float x0, float x1, float y0, float y1, float[] cxs, float w, float hs, float t, Material m)
    {
        float r = w * .5f, prev = x0;
        for (var i = 0; i < cxs.Length; i++)
        {
            float a = cxs[i] - r, b = cxs[i] + r;
            if (a - prev > .01f) Box("pier", new Vector3(prev, y0, z - t * .5f), new Vector3(a, y1, z + t * .5f), m, 1.2f, 1u, "-y");
            TsSpandrel(z, cxs[i], r, hs, y1, t, m);
            prev = b;
        }
        if (x1 - prev > .01f) Box("pier", new Vector3(prev, y0, z - t * .5f), new Vector3(x1, y1, z + t * .5f), m, 1.2f, 1u, "-y");
    }

    static void TsSpandrel(float z, float cx, float r, float hs, float top, float t, Material m)
    {
        const int N = 18;
        var vs = new List<Vector3>();
        var ts = new List<int>();
        for (var face = 0; face < 2; face++)
        {
            var zz = face == 0 ? z - t * .5f : z + t * .5f;
            for (var i = 0; i < N; i++)
            {
                float a0 = Mathf.PI * i / N, a1 = Mathf.PI * (i + 1) / N;
                var corner = i < N / 2 ? new Vector3(cx + r, top, zz) : new Vector3(cx - r, top, zz);
                var b = vs.Count;
                vs.Add(corner);
                vs.Add(new Vector3(cx + r * Mathf.Cos(a0), hs + r * Mathf.Sin(a0), zz));
                vs.Add(new Vector3(cx + r * Mathf.Cos(a1), hs + r * Mathf.Sin(a1), zz));
                ts.Add(b); ts.Add(b + 1); ts.Add(b + 2);
            }
            var c0 = vs.Count;
            vs.Add(new Vector3(cx + r, top, zz)); vs.Add(new Vector3(cx - r, top, zz)); vs.Add(new Vector3(cx, hs + r, zz));
            ts.Add(c0); ts.Add(c0 + 1); ts.Add(c0 + 2);
        }
        for (var i = 0; i < N; i++)
        {
            float a0 = Mathf.PI * i / N, a1 = Mathf.PI * (i + 1) / N;
            var f0 = new Vector3(cx + r * Mathf.Cos(a0), hs + r * Mathf.Sin(a0), z - t * .5f);
            var f1 = new Vector3(cx + r * Mathf.Cos(a1), hs + r * Mathf.Sin(a1), z - t * .5f);
            var b = vs.Count;
            vs.Add(f0); vs.Add(f1); vs.Add(f1 + new Vector3(0f, 0f, t)); vs.Add(f0 + new Vector3(0f, 0f, t));
            ts.Add(b); ts.Add(b + 1); ts.Add(b + 2); ts.Add(b); ts.Add(b + 2); ts.Add(b + 3);
        }
        TsMesh("arch spandrel", vs, ts, m, true);
    }

    // A spiral stair round a post: n treads over sweepDeg degrees from yBot to yTop; the last tread points at startDeg + sweepDeg.
    static void TsSpiral(float cx, float cz, float yBot, float yTop, float r, float startDeg, int n, float sweepDeg, Material m)
    {
        Cyl("spiral post", new Vector3(cx, yBot, cz), new Vector3(cx, yTop + 1f, cz), .07f, mMetal);
        float rise = (yTop - yBot) / n, step = sweepDeg / n;
        var prev = Vector3.zero;
        for (var i = 0; i < n; i++)
        {
            var deg = startDeg + step * (i + 1);
            var a = deg * Mathf.Deg2Rad;
            var y = yBot + rise * (i + 1);
            var mid = new Vector3(cx + Mathf.Cos(a) * r * .5f, y - .025f, cz + Mathf.Sin(a) * r * .5f);
            TsBox("spiral tread", mid, new Vector3(r, .05f, Mathf.Max(.24f, r * Mathf.Abs(step) * Mathf.Deg2Rad * 1.05f)), -deg, m);
            var o = new Vector3(cx + Mathf.Cos(a) * (r - .04f), y, cz + Mathf.Sin(a) * (r - .04f));
            Cyl("spiral baluster", o, o + Vector3.up * .95f, .012f, mMetal);
            var topP = o + Vector3.up * .95f;
            if (i > 0) Cyl("spiral rail", prev, topP, .02f, mMetal);
            prev = topP;
        }
    }

    static void TsDome(float cx, float cy, float cz, float R, float oculusDeg, Material m)
    {
        const int seg = 40, rings = 12;
        var vs = new List<Vector3>();
        var ts = new List<int>();
        var maxLat = (90f - oculusDeg) * Mathf.Deg2Rad;
        for (var j = 0; j <= rings; j++)
        {
            var lat = maxLat * j / rings;
            for (var i = 0; i <= seg; i++)
            {
                var lon = 2f * Mathf.PI * i / seg;
                vs.Add(new Vector3(cx + R * Mathf.Cos(lat) * Mathf.Cos(lon), cy + R * Mathf.Sin(lat), cz + R * Mathf.Cos(lat) * Mathf.Sin(lon)));
            }
        }
        for (var j = 0; j < rings; j++)
        for (var i = 0; i < seg; i++)
        {
            int a = j * (seg + 1) + i, b = a + 1, c = a + seg + 1, d = c + 1;
            ts.Add(a); ts.Add(c); ts.Add(b); ts.Add(b); ts.Add(c); ts.Add(d);
        }
        TsMesh("dome", vs, ts, m, true);
    }

    static Vector3 TsSq(float cx, float y, float cz, float half, float a)
    {
        float c = Mathf.Cos(a), s = Mathf.Sin(a);
        var k = half / Mathf.Max(Mathf.Abs(c), Mathf.Abs(s));
        return new Vector3(cx + c * k, y, cz + s * k);
    }

    // The flat ring between a square opening and the circle a dome stands on.
    static void TsSquareRing(float cx, float y, float cz, float half, float R, Material m)
    {
        const int N = 64;
        var vs = new List<Vector3>();
        var ts = new List<int>();
        for (var i = 0; i < N; i++)
        {
            float a0 = 2f * Mathf.PI * i / N, a1 = 2f * Mathf.PI * (i + 1) / N;
            var b = vs.Count;
            vs.Add(new Vector3(cx + R * Mathf.Cos(a0), y, cz + R * Mathf.Sin(a0)));
            vs.Add(TsSq(cx, y, cz, half, a0));
            vs.Add(TsSq(cx, y, cz, half, a1));
            vs.Add(new Vector3(cx + R * Mathf.Cos(a1), y, cz + R * Mathf.Sin(a1)));
            ts.Add(b); ts.Add(b + 1); ts.Add(b + 2); ts.Add(b); ts.Add(b + 2); ts.Add(b + 3);
        }
        TsMesh("dome ring", vs, ts, m, true);
    }

    static void TsSpot(string name, Vector3 p, Vector3 dir, float outer, float intensity, float range, Color col, bool shadow)
    {
        AddSpot(name, p, dir, outer, outer * .6f, intensity, range, col, shadow, 1u);
    }

    static bool TsIn(Rect[] hs, float x, float z)
    {
        if (hs == null) return false;
        for (var i = 0; i < hs.Length; i++) if (hs[i].Contains(new Vector2(x, z))) return true;
        return false;
    }

    // One floor round an irregular opening (a union of rectangles on a 1.5 m raster); the ceiling opens where the floor above opens.
    static void TsWellFloor(int k, Rect[] hs, Rect[] above, bool office, Rect gap)
    {
        var y = -LsFF * k;
        var fl = office ? mOCarpet : mCarpet;
        var ce = office ? mOCeil : mCeil;
        var wa = office ? mOWall : mWall;
        const float E = 6f, c = 1.5f;
        for (var x = -E; x < E - .01f; x += c)
        for (var z = -E; z < E - .01f; z += c)
        {
            float mx = x + c * .5f, mz = z + c * .5f;
            var hole = TsIn(hs, mx, mz);
            if (!hole) FloorQ(x, z, x + c, z + c, y, fl);
            var open = k == 0 ? hole : TsIn(above, mx, mz);
            if (!open) CeilQ(x, z, x + c, z + c, y + LsCH, ce);
            else if (k == 0) Fq("laylight", new Vector3(x, y + LsCH + .3f, z), new Vector3(0f, 0f, c), new Vector3(c, 0f, 0f), mGlow, 1f).shadowCastingMode = ShadowCastingMode.Off;
            if (!hole) continue;
            if (!TsIn(hs, mx - c, mz)) TsEdge(true, x, z, z + c, y, 1, office, gap);
            if (!TsIn(hs, mx + c, mz)) TsEdge(true, x + c, z, z + c, y, -1, office, gap);
            if (!TsIn(hs, mx, mz - c)) TsEdge(false, z, x, x + c, y, 1, office, gap);
            if (!TsIn(hs, mx, mz + c)) TsEdge(false, z + c, x, x + c, y, -1, office, gap);
        }
        WallX(-E, -E, E, y, y + LsCH, 1, wa); WallX(E, -E, E, y, y + LsCH, -1, wa);
        WallZ(-E, -E, E, y, y + LsCH, 1, wa); WallZ(E, -E, E, y, y + LsCH, -1, wa);
        for (var x = -4.5f; x < E; x += 3f)
        for (var z = -4.5f; z < E; z += 3f)
        {
            var open = k == 0 ? TsIn(hs, x, z) : TsIn(above, x, z);
            if (!open && !TsIn(hs, x, z)) LsLamp(x, z, y + LsCH, office ? mLensCool : mLens, office ? LampOf : LampL0, 12f, 11f, k <= 1 && x < 0f && z < 0f);
        }
    }

    // alongZ: the edge lies on x = at from z = a to b; else on z = at from x = a to b. side: the void lies on the + side (1) or - side (-1).
    static void TsEdge(bool alongZ, float at, float a, float b, float y, int side, bool glass, Rect gap)
    {
        if (alongZ) WallX(at, a, b, y - LsPlen, y, side, mFascia);
        else WallZ(at, a, b, y - LsPlen, y, side, mFascia);
        var mid = alongZ ? new Vector2(at, (a + b) * .5f) : new Vector2((a + b) * .5f, at);
        if (gap.width > 0f && gap.Contains(mid)) return;
        if (alongZ) { if (glass) GlassGuard(at, a, at, b, y); else Rail(at, a, at, b, y); }
        else { if (glass) GlassGuard(a, at, b, at, y); else Rail(a, at, b, at, y); }
    }

    // ================================================================================ O - the well without symmetry
    static IEnumerator LsSceneO()
    {
        TsInit();
        LsBegin("O well spliced", 3000f);
        var hs = new List<Rect[]>
        {
            new[] { Rect.MinMaxRect(-4.5f, -3f, 1.5f, 3f), Rect.MinMaxRect(1.5f, -3f, 4.5f, 0f) },
            new[] { Rect.MinMaxRect(-3f, -1.5f, 1.5f, 4.5f), Rect.MinMaxRect(-3f, -4.5f, 0f, -1.5f) },
            new[] { Rect.MinMaxRect(-1.5f, -4.5f, 4.5f, 1.5f), Rect.MinMaxRect(-4.5f, -1.5f, -1.5f, 1.5f) },
            new[] { Rect.MinMaxRect(-4.5f, -1.5f, 1.5f, 3f), Rect.MinMaxRect(-1.5f, 3f, 1.5f, 4.5f) },
            new[] { Rect.MinMaxRect(-3f, -3f, 3f, 1.5f) },
            new[] { Rect.MinMaxRect(-1.5f, -3f, 3f, 3f) }
        };
        var noGap = new Rect(0f, 0f, 0f, 0f);
        for (var k = 0; k < hs.Count; k++)
            TsWellFloor(k, hs[k], k > 0 ? hs[k - 1] : null, k % 2 == 1, k == 0 ? Rect.MinMaxRect(4.4f, -3f, 4.6f, -1.5f) : noGap);
        var yb = -LsFF * hs.Count - 2f;
        LsPoolBottom(yb, 3f, 6f);
        TsSpiral(3f, -2.1f, -LsFF, 0f, .85f, 30f, 17, 330f, mStair3);
        TsSolid(3.85f, -2.6f, 4.5f, -1.6f, -.25f, 0f, mFascia, mCarpet);
        TsArchZ(3.3f, -4.5f, 1.5f, 0f, LsCH, new[] { -2.85f, -.15f }, 2.4f, 1.4f, .3f, mWall);
        TsSolid(3f, 3f, 6f, 6f, 0f, .6f, mFascia, mCarpet);
        FlightX(1.8f, 0f, 1.2f, .6f, 3.3f, 4.8f, 4, mCarpet);
        Rail(3f, 4.8f, 3f, 6f, .6f); Rail(3f, 3f, 6f, 3f, .6f);
        var y2 = -LsFF * 2f;
        TsSolid(-4.5f, -.9f, 4.5f, .9f, y2 - .25f, y2, mFascia, mCarpet);
        GlassGuard(-4.5f, -.9f, 4.5f, -.9f, y2); GlassGuard(-4.5f, .9f, 4.5f, .9f, y2);
        TsWaterQ(-2f, 1.5f, 1f, 2.4f, y2 + .02f);
        TsSheet(new Vector3(-1f, yb + .06f, 1.45f), new Vector3(1.5f, 0f, 0f), new Vector3(0f, y2 - yb, 0f), mFall3);
        TsSpot("cascade light", new Vector3(-.2f, y2 - 1f, 2.6f), new Vector3(0f, -.6f, -1f), 50f, 14f, 22f, TsUnder, false);
        yield return 20;
        LsAim(-2f, 1.62f, -3.55f, -.4f, -20f, -.2f, 74f);
        Shot("o1_well_down", "The well spliced: every floor's opening is cut differently, so floors overhang and step back; turquoise light at the bottom");
        LsAim(5.2f, .6f + 1.62f, 5.1f, 1.5f, -3.5f, -1.8f, -1f);
        Shot("o2_well_from_terrace", "From a raised terrace in one corner: an arcade on one side only, the spiral stair in the far corner");
        LsAim(-3.2f, y2 + 1.62f, 2.1f, .4f, y2 - 9f, -.4f, 74f);
        Shot("o3_well_bridge_cascade", "Two floors down: a bridge on this floor only, and water spilling down one face of the shaft");
        LsAim(.3f, yb + .06f + 1.62f, -.6f, -.2f, 0f, -.7f, 80f);
        Shot("o4_well_from_pool", "From the pool at the bottom: the openings above do not line up");
        LsEnd();
        yield return 2;
    }

    // ================================================================================ P - the pool route (Fig. 5)
    static IEnumerator LsSceneP()
    {
        TsInit();
        LsBegin("P pool route", 3400f);
        TsPoolLook(true);
        const float H = 5.4f;
        Room(-1.5f, -9f, 1.5f, 0f, 0f, LsCH, mWall, mCarpet, mCeil, "N");
        Lamps(-1.5f, -9f, 1.5f, 0f, LsCH, mLens, LampL0, 10f, 10f, 2, null);
        TsArchZ(0f, -6f, 12f, 0f, H, new[] { 0f }, 2.4f, 1.5f, .3f, mTile);
        WallX(-6f, 0f, 24f, -1.6f, H, 1, mTile); WallX(12f, 0f, 24f, -1.6f, H, -1, mTile); WallZ(24f, -6f, 12f, -1.6f, H, -1, mTile);
        var domeHole = Rect.MinMaxRect(3f, 10.5f, 11f, 18.5f);
        CeilHoles(Rect.MinMaxRect(-6f, 0f, 12f, 24f), new List<Rect> { domeHole }, H, mTile);
        VoidBand(7f, 14.5f, 4f, 4f, H, H + .6f, mTile);
        TsSquareRing(7f, H + .6f, 14.5f, 4f, 4f, mTile);
        TsDome(7f, H + .6f, 14.5f, 4f, 12f, mTile);
        Fq("oculus light", new Vector3(6.1f, H + .6f + 4.3f, 13.6f), new Vector3(0f, 0f, 1.8f), new Vector3(1.8f, 0f, 0f), mGlow, 1f).shadowCastingMode = ShadowCastingMode.Off;
        // dry landing, then the beach into ankle and wading water
        FloorQ(-6f, 0f, 4f, 1.5f, 0f, mTile);
        Fq("beach", new Vector3(-6f, 0f, 1.5f), new Vector3(10f, 0f, 0f), new Vector3(0f, -.85f, 6f), mTile, 1f);
        FloorQ(-6f, 7.5f, 4f, 10f, -.85f, mTile);
        TsWaterQ(-6f, 1.85f, 4f, 10f, -.05f);
        // the terrace (+0.5), reached from the water and from the landing
        TsSolid(4f, 0f, 12f, 10f, -.9f, .5f, mTile, mTile);
        FlightX(1.9f, -.85f, 2.1f, 1.35f, 7.3f, 9.1f, 9, mTile);
        FlightX(3.1f, 0f, .9f, .5f, .1f, 1.3f, 3, mTile);
        // channel and upper basin, the island and its bridge
        FloorQ(-6f, 10f, 12f, 18f, -.9f, mTile);
        TsWaterQ(-6f, 10f, 12f, 18f, -.05f);
        TsDeepQ(-6f, 10f, 12f, 18f, -.5f);
        TsSolid(4f, 12f, 10f, 17f, -.9f, .5f, mTile, mTile);
        TsSolid(6.2f, 10f, 7.8f, 12f, .3f, .5f, mTile, mTile);
        GlassGuard(6.2f, 10f, 6.2f, 12f, .5f); GlassGuard(7.8f, 10f, 7.8f, 12f, .5f);
        // the weir: the upper water spills 0.6 m into the lower basin
        WallZ(18f, -6f, 12f, -1.6f, -.08f, 1, mTile); WallZ(18f, -6f, 12f, -.9f, -.08f, -1, mTile);
        FloorQ(-6f, 17.85f, 12f, 18f, -.08f, mTile);
        FloorQ(-6f, 18f, 12f, 24f, -1.6f, mTile);
        TsWaterQ(-6f, 18.2f, 12f, 24f, -.65f);
        TsDeepQ(-6f, 18.2f, 12f, 24f, -1.15f);
        TsSheet(new Vector3(-6f, -.08f, 18f), new Vector3(18f, 0f, 0f), new Vector3(0f, -.57f, .22f), mFall3);
        // the deck, the spiral and the gallery on the north wall
        TsSolid(-6f, 21.6f, 0f, 24f, -1.6f, 0f, mTile, mTile);
        TsSpiral(-1f, 22.8f, 0f, 3f, .85f, 50f, 14, 320f, mTile);
        TsSolid(0f, 21.6f, 12f, 24f, 2.75f, 3f, mTile, mTile);
        GlassGuard(0f, 21.6f, 12f, 21.6f, 3f);
        for (var x = -3f; x < 12f; x += 6f)
        for (var z = 3f; z < 24f; z += 6f)
            if (!domeHole.Contains(new Vector2(x, z))) TsSpot("hall lamp", new Vector3(x, H - .1f, z), Vector3.down, 140f, 40f, 16f, TsCool, x < 0f && z < 4f);
        TsSpot("oculus", new Vector3(7f, 10.2f, 14.5f), Vector3.down, 46f, 140f, 22f, new Color(1f, .98f, .94f), true);
        TsSpot("gallery lamp", new Vector3(6f, 2.6f, 22.8f), Vector3.down, 140f, 8f, 7f, TsCool, false);
        foreach (var q in new[] { new Vector3(4f, 5.6f, 11.5f), new Vector3(10f, 5.6f, 11.5f), new Vector3(4f, 5.6f, 17.5f), new Vector3(10f, 5.6f, 17.5f) })
            TsSpot("dome uplight", q, (new Vector3(7f, 9f, 14.5f) - q).normalized, 70f, 18f, 9f, TsCool, false);
        foreach (var p in new[] { new Vector3(-3f, -.7f, 14f), new Vector3(2f, -.7f, 11f), new Vector3(-2f, -1.4f, 20f), new Vector3(6f, -1.4f, 20.5f), new Vector3(-3f, -.75f, 8.5f) })
            TsSpot("underwater", p, Vector3.up, 120f, 16f, 7f, TsUnder, false);
        yield return 20;
        LsAim(0f, 1.62f, -5.5f, 1f, .6f, 9f, -1f);
        Shot("p1_arch_to_beach", "From the Level 0 corridor: the tiled arch frames the beach, the terrace and the channel beyond");
        LsAim(-4.5f, 1.62f, 1f, 8f, 1.5f, 14f, -1f);
        Shot("p2_beach_across", "On the dry landing: the floor runs down into the water; the terrace, the bridge, the island and the dome are at other heights");
        LsAim(8.5f, .5f + 1.62f, 6f, 6f, .3f, 20f, -1f);
        Shot("p3_terrace_bridge", "From the terrace: the bridge to the island, the weir beyond, the gallery and the spiral against the far wall");
        LsAim(6f, 3f + 1.62f, 22.4f, 3f, -.5f, 8f, -1f);
        Shot("p4_gallery_down", "From the gallery: the whole route below, every part at its own level");
        LsAim(7.3f, .5f + 1.62f, 13f, 7f, 10f, 14.5f, 70f);
        Shot("p5_oculus", "On the island under the dome: the oculus is the only source you can see");
        TsPoolLook(false);
        LsEnd();
        yield return 2;
    }

    // ================================================================================ Q - the slide tower
    static IEnumerator LsSceneQ()
    {
        TsInit();
        LsBegin("Q slide tower", 3800f);
        TsPoolLook(true);
        const float H = 9f;
        FloorQ(-9f, 0f, 9f, 12f, 0f, mTile);
        FloorQ(-9f, 12f, 9f, 18f, -1.2f, mTile);
        CeilQ(-9f, 0f, 9f, 18f, H, mTile);
        WallX(-9f, 0f, 18f, -1.2f, H, 1, mTile); WallX(9f, 0f, 18f, -1.2f, H, -1, mTile);
        WallZ(0f, -9f, 9f, 0f, H, 1, mTile); WallZ(18f, -9f, 9f, -1.2f, H, -1, mTile);
        WallZ(12f, -9f, 2.4f, 0f, H, -1, mTile); WallZ(12f, 4.6f, 9f, 0f, H, -1, mTile);
        WallZ(12f, 2.4f, 4.6f, 0f, 1.2f, -1, mTile); WallZ(12f, 2.4f, 4.6f, 3.4f, H, -1, mTile);
        WallZ(12f, -9f, 2.4f, -1.2f, H, 1, mTile); WallZ(12f, 4.6f, 9f, -1.2f, H, 1, mTile);
        WallZ(12f, 2.4f, 4.6f, -1.2f, 1.2f, 1, mTile); WallZ(12f, 2.4f, 4.6f, 3.4f, H, 1, mTile);
        TsWaterQ(-9f, 12f, 9f, 18f, -.05f);
        TsDeepQ(-9f, 12f, 9f, 18f, -.6f);
        TsSpiral(-6f, 4f, 0f, 6f, 1.05f, 33f, 27, 700f, mTile);
        TsSolid(-4.8f, 2.4f, -1.6f, 5.6f, 5.75f, 6f, mTile, mTile);
        foreach (var p in new[] { new Vector2(-4.7f, 2.5f), new Vector2(-1.7f, 2.5f), new Vector2(-4.7f, 5.5f), new Vector2(-1.7f, 5.5f) })
            Cyl("tower leg", new Vector3(p.x, 0f, p.y), new Vector3(p.x, 5.75f, p.y), .09f, mMetal);
        Rail(-4.8f, 2.4f, -1.6f, 2.4f, 6f); Rail(-4.8f, 5.6f, -1.6f, 5.6f, 6f); Rail(-1.6f, 2.4f, -1.6f, 3.3f, 6f);
        var tube = new[]
        {
            new Vector3(-1.6f, 5.9f, 4f), new Vector3(.5f, 5.2f, 5.2f), new Vector3(2.6f, 4.2f, 7.4f), new Vector3(3.4f, 3f, 10f),
            new Vector3(3.5f, 2.3f, 12f), new Vector3(3f, 1.2f, 14f), new Vector3(1.8f, .2f, 15.5f), new Vector3(.6f, -.2f, 16.4f)
        };
        for (var i = 0; i + 1 < tube.Length; i++)
        {
            Cyl("slide", tube[i], tube[i + 1], .42f, mSlide3);
            TsBall(tube[i + 1], .84f, mSlide3);
        }
        for (var x = -6f; x < 9f; x += 6f)
        for (var z = 3f; z < 18f; z += 6f)
            TsSpot("hall lamp", new Vector3(x, H - .1f, z), Vector3.down, 130f, 34f, 18f, TsCool, x > 0f && z < 4f);
        TsSpot("platform lamp", new Vector3(-3.2f, 8.4f, 4f), Vector3.down, 90f, 14f, 6f, TsCool, false);
        foreach (var p in new[] { new Vector3(-5f, -.8f, 15f), new Vector3(1f, -.8f, 16f), new Vector3(6f, -.8f, 14f) })
            TsSpot("underwater", p, Vector3.up, 120f, 10f, 7f, TsUnder, false);
        yield return 20;
        LsAim(5.5f, 1.62f, 1.2f, -3f, 4.2f, 8f, -1f);
        Shot("q1_slide_tower", "The slide tower: two turns of spiral stair to a platform at +6 m, the tube crossing the hall and leaving through the wall");
        LsAim(-3f, 6f + 1.62f, 3.4f, 3f, 2.5f, 11f, -1f);
        Shot("q2_from_the_platform", "From the platform: the tube drops toward a hole in the far wall; the catch pool is behind it");
        LsAim(-4f, -1.2f + 1.62f, 16.5f, 2.6f, 1.6f, 13f, -1f);
        Shot("q3_catch_pool", "Wading in the catch pool: the tube arrives through the wall from a room you have not seen");
        TsPoolLook(false);
        LsEnd();
        yield return 2;
    }

    // ================================================================================ R - the casbah cluster
    static IEnumerator LsSceneR()
    {
        TsInit();
        LsBegin("R casbah", 4200f);
        TsPoolLook(true);
        const float H = 8f;
        Room(-12f, -12f, 12f, 12f, -1.2f, H + 1.2f, mTile, mTile, mTile, "");
        TsWaterQ(-12f, -12f, 12f, 12f, -.4f);
        TsDeepQ(-12f, -12f, 12f, 12f, -.8f);
        var blocks = new[]
        {
            new Vector4(-12f, -12f, -6f, -3f), new Vector4(-6f, -12f, -1.5f, -6f), new Vector4(-9f, 0f, -3f, 6f),
            new Vector4(0f, -3f, 6f, 3f), new Vector4(3f, 3f, 12f, 9f), new Vector4(6f, -12f, 12f, -6f), new Vector4(-3f, 6f, 3f, 12f)
        };
        var tops = new[] { 0f, 1.2f, 2.4f, .6f, 3f, 1.8f, .3f };
        for (var i = 0; i < blocks.Length; i++) TsSolid(blocks[i].x, blocks[i].y, blocks[i].z, blocks[i].w, -1.2f, tops[i], mTile, mTile);
        FlightX(-8.4f, 0f, 2.4f, 1.2f, -10.5f, -9.3f, 8, mTile);
        FlightZ(-6f, 1.2f, 2.4f, 1.2f, -5f, -3.8f, 8, mTile);
        TsSolid(-5f, -3.6f, -3.8f, 0f, 2.2f, 2.4f, mTile, mTile);
        Rail(-5f, -3.6f, -5f, 0f, 2.4f); Rail(-3.8f, -3.6f, -3.8f, 0f, 2.4f);
        FlightZ(-1.8f, .6f, 4.8f, 2.4f, 3.6f, 4.8f, 16, mTile);
        FlightX(-2.4f, -1.2f, 2.4f, 1.8f, -1.5f, 0f, 12, mTile);
        FlightZ(-.6f, -1.2f, -2.4f, 1.2f, -10.5f, -9f, 8, mTile);
        FlightZ(3.6f, -1.2f, 2.4f, 1.5f, -1.2f, .3f, 10, mTile);
        TsArchZ(-9f, 6.5f, 11.5f, 1.8f, 4.8f, new[] { 9f }, 1.6f, 3.2f, .3f, mTile);
        TsArchZ(4.5f, -9f, -3f, 2.4f, 5.6f, new[] { -6f }, 1.8f, 3.9f, .3f, mTile);
        Rail(3f, 3f, 3f, 9f, 3f); Rail(4.8f, 3f, 12f, 3f, 3f); Rail(3f, 3f, 3.6f, 3f, 3f);
        Rail(-3f, 0f, -3f, 6f, 2.4f); Rail(-9f, 6f, -3f, 6f, 2.4f);
        Rail(-6f, -6f, -5f, -6f, 1.2f); Rail(-3.8f, -6f, -1.5f, -6f, 1.2f); Rail(6f, -12f, 6f, -6f, 1.8f);
        for (var x = -9f; x < 12f; x += 6f)
        for (var z = -9f; z < 12f; z += 6f)
            TsSpot("hall lamp", new Vector3(x, H - .1f, z), Vector3.down, 130f, 30f, 16f, TsCool, x < -8f && z < -8f);
        TsSpot("block lamp", new Vector3(-6f, 5f, 3f), Vector3.down, 120f, 8f, 6f, TsCool, false);
        TsSpot("block lamp", new Vector3(7.5f, 5.6f, 6f), Vector3.down, 120f, 8f, 6f, TsCool, false);
        foreach (var p in new[] { new Vector3(-3f, -1f, -4f), new Vector3(-1f, -1f, 4.5f), new Vector3(-10f, -1f, 3f), new Vector3(8f, -1f, 0f) })
            TsSpot("underwater", p, Vector3.up, 120f, 9f, 7f, TsUnder, false);
        yield return 20;
        LsAim(-10.5f, 1.62f, -4.6f, 1f, 1.8f, 3f, -1f);
        Shot("r1_casbah_from_low_block", "The casbah cluster: blocks start from their own levels (0, +0.3, +0.6, +1.2, +1.8, +2.4, +3.0 m), joined by stairs and a stair-bridge; the lowest level is flooded");
        LsAim(4f, 3f + 1.62f, 3.8f, -5f, -.4f, -6f, -1f);
        Shot("r2_casbah_from_high_block", "From the highest block: arched gates on the blocks, stairs running into the water");
        LsAim(-1.2f, -1.2f + 1.62f, -4.8f, -4.4f, 2.2f, -2f, -1f);
        Shot("r3_casbah_wading", "Wading on the flooded level: the stair-bridge overhead");
        TsPoolLook(false);
        LsEnd();
        yield return 2;
    }

    // ================================================================================ S - Level 0 terrain modules
    static IEnumerator LsSceneS()
    {
        TsInit();
        LsBegin("S1 computer room", 4600f);
        Room(-6f, -4.5f, 6f, 4.5f, 0f, LsCH, mWall, mCarpet, mCeil, "");
        TsSolid(-.9f, -4.5f, 6f, -3.3f, 0f, .3f, mMetal, mRaised3);
        TsSolid(-4.5f, -3.3f, 6f, 4.5f, 0f, .3f, mMetal, mRaised3);
        Fq("ramp", new Vector3(-4.5f, 0f, -4.5f), new Vector3(3.6f, .3f, 0f), new Vector3(0f, 0f, 1.2f), mRaised3, 1f);
        FlightX(-5.1f, 0f, .6f, .3f, 0f, 1.5f, 2, mRaised3);
        for (var x = -3f; x < 5.5f; x += .8f)
        {
            TsSolid(x, 1.4f, x + .78f, 2.2f, .3f, 2.2f, mCab3, null);
            TsSolid(x, -1.2f, x + .78f, -.4f, .3f, 2.2f, mCab3, null);
        }
        Kit("Kit_OfficeDesk", 4.5f, .3f, 3.6f, 180f); Kit("Kit_CRTMonitor", 4.5f, 1.04f, 3.7f, 180f); Kit("Kit_TaskChair", 4.5f, .3f, 2.9f, 0f);
        Lamps(-6f, -4.5f, 6f, 4.5f, LsCH, mLens, LampL0, 13f, 11f, 3, null);
        yield return 20;
        LsAim(-5.4f, 1.62f, 3.7f, 3.5f, .7f, -2.4f, -1f);
        Shot("s1_computer_room", "Level 0, a 1980s computer room: raised floor +0.30 m with its 1:12 ramp and a two-riser step; the ceiling stays, so the room gets lower");
        LsEnd();
        yield return 2;

        LsBegin("S2 training room", 4700f);
        Room(-6f, -6f, 6f, 6f, -1.35f, 4.25f, mWall, mCarpet, mCeil, "");
        TsSolid(-6f, 0f, 6f, 1.8f, -1.35f, -.9f, mFascia, mCarpet);
        TsSolid(-6f, 1.8f, 6f, 3.6f, -1.35f, -.45f, mFascia, mCarpet);
        TsSolid(-6f, 3.6f, 6f, 6f, -1.35f, 0f, mFascia, mCarpet);
        FlightZ(3f, -.45f, .6f, .45f, 4.6f, 5.8f, 2, mCarpet);
        FlightZ(1.2f, -.9f, .6f, .45f, 4.6f, 5.8f, 2, mCarpet);
        FlightZ(-.6f, -1.35f, .6f, .45f, 4.6f, 5.8f, 2, mCarpet);
        var rows = new[] { new Vector2(-1.6f, -1.35f), new Vector2(.9f, -.9f), new Vector2(2.7f, -.45f), new Vector2(4.6f, 0f) };
        foreach (var r in rows)
            for (var x = -4.5f; x < 4f; x += 1.5f) Kit("Kit_TaskChair", x, r.y, r.x, 180f);
        TsSolid(-2.5f, -5.98f, 2.5f, -5.9f, -.2f, 1.2f, SurfMat("Prop_PlasticWhite"), null);
        Lamps(-6f, -6f, 6f, 6f, LsCH, mLens, LampL0, 16f, 12f, 3, null);
        yield return 20;
        LsAim(-3.6f, 1.62f, 5.3f, .5f, -.9f, -5.5f, -1f);
        Shot("s2_training_room", "Level 0, a tiered training room: you enter at the top and the floor steps down 1.35 m in tiers of 0.45; aisle steps at one side");
        LsEnd();
        yield return 2;

        LsBegin("S3 sunken lounge", 4800f);
        Room(-4.5f, -4.5f, 4.5f, 4.5f, 0f, LsCH, mWall, mCarpet, mCeil, "f");
        FloorQ(-4.5f, -4.5f, 4.5f, -2.5f, 0f, mCarpet); FloorQ(-4.5f, 2f, 4.5f, 4.5f, 0f, mCarpet);
        FloorQ(-4.5f, -2.5f, -2.5f, 2f, 0f, mCarpet); FloorQ(2.5f, -2.5f, 4.5f, 2f, 0f, mCarpet);
        FloorQ(-2.5f, -2.5f, 2.5f, 2f, -.6f, mCarpet);
        WallX(-2.5f, -2.5f, 2f, -.6f, 0f, 1, mFascia); WallX(2.5f, -2.5f, 2f, -.6f, 0f, -1, mFascia);
        WallZ(-2.5f, -2.5f, 2.5f, -.6f, 0f, 1, mFascia); WallZ(2f, -2.5f, 2.5f, -.6f, 0f, -1, mFascia);
        FlightZ(.8f, -.6f, 1.2f, .6f, -1f, .5f, 4, mCarpet);
        FlightX(-1.3f, -.6f, -1.2f, .6f, -2f, -.5f, 4, mCarpet);
        Kit("Kit_Sofa3", .4f, -.6f, -1.95f, 0f); Kit("Kit_Sofa3", 1.95f, -.6f, .2f, 270f);
        TsSolid(-.3f, -.9f, 1f, .2f, -.6f, -.2f, SurfMat("Prop_WoodWalnut"), null);
        Lamps(-4.5f, -4.5f, 4.5f, 4.5f, LsCH, mLens, LampL0, 12f, 10f, 2, null);
        yield return 20;
        LsAim(3.9f, 1.62f, 3.9f, -.8f, -.6f, -1.4f, -1f);
        Shot("s3_sunken_lounge", "Level 0, a sunken lounge: a pit 0.6 m down with steps on two sides, off the room's centre");
        LsEnd();
        yield return 2;

        LsBegin("S4 split suite", 4900f);
        Room(-6f, -4.5f, 6f, 4.5f, 0f, 5.4f, mWall, mCarpet, mCeil, "");
        TsSolid(-6f, -4.5f, 6f, -1.5f, 0f, 1.2f, mFascia, mCarpet);
        TsSolid(-6f, 1.5f, 6f, 4.5f, 2.5f, 2.7f, mFascia, mCarpet);
        FlightZ(.9f, 0f, -2.4f, 1.2f, 1f, 2.2f, 8, mCarpet);
        FlightZ(-1.5f, 1.2f, 3f, 1.5f, -6f, -4.8f, 10, mCarpet);
        TsSpiral(4.8f, .6f, 0f, 2.7f, .85f, 143f, 12, 307f, mStair3);
        GlassGuard(-4.8f, -1.5f, 1f, -1.5f, 1.2f); GlassGuard(2.2f, -1.5f, 6f, -1.5f, 1.2f);
        GlassGuard(-4.8f, 1.5f, 4.2f, 1.5f, 2.7f); GlassGuard(5.4f, 1.5f, 6f, 1.5f, 2.7f);
        Kit("Kit_OfficeDesk", -1.5f, 2.7f, 3.4f, 180f); Kit("Kit_OfficeDesk", 2f, 2.7f, 3.4f, 180f);
        Kit("Kit_Credenza", -2.5f, 1.2f, -4.1f, 0f); Kit("Kit_OfficeDesk", 3.5f, 1.2f, -3.2f, 0f);
        Lamps(-6f, -4.5f, 6f, 4.5f, 5.4f, mLens, LampL0, 22f, 15f, 3, null);
        Lamps(-6f, 1.5f, 6f, 4.5f, 2.5f, mLens, LampL0, 8f, 8f, 0, null);
        yield return 20;
        LsAim(3.2f, 1.62f, .1f, -3.2f, 1.9f, .4f, -1f);
        Shot("s4_split_suite", "Level 0, a split-level suite in a 5.4 m room: a platform at +1.2, a gallery at +2.7; the spiral is one way up, the flight along the wall the other");
        LsAim(-2f, 2.7f + 1.62f, 1.9f, 3f, .6f, -2.8f, -1f);
        Shot("s4_from_the_gallery", "From the gallery: the platform and the main floor below, the spiral in the corner");
        LsEnd();
        yield return 2;
    }

    // ================================================================================ T - Office terrain modules
    static IEnumerator LsSceneT()
    {
        TsInit();
        LsBegin("T1 raised trading floor", 5000f);
        Room(-9f, -6f, 9f, 6f, 0f, LsCH, mOWall, mOCarpet, mOCeil, "");
        TsSolid(-5f, -4f, 9f, 6f, 0f, .3f, mMetal, mOCarpet);
        Fq("ramp", new Vector3(-8.6f, 0f, -4f), new Vector3(3.6f, .3f, 0f), new Vector3(0f, 0f, 1.2f), mOCarpet, 1f);
        FlightX(-5.6f, 0f, .6f, .3f, 1f, 2.5f, 2, mOCarpet);
        Cluster(-3.5f, -2.5f, .3f); Cluster(1f, -2.5f, .3f); Cluster(-3.5f, 2f, .3f); Cluster(1f, 2f, .3f); Cluster(5.4f, -.4f, .3f);
        SetPanels(1.35f);
        RelayStandIn(-7.4f, 0f, 3.2f);
        Lamps(-9f, -6f, 9f, 6f, LsCH, mLensCool, LampOf, 13f, 11f, 3, null);
        yield return 20;
        LsAim(7.8f, .3f + 1.62f, -3.6f, -7.2f, 1.1f, 3.2f, -1f);
        Shot("t1_raised_trading_floor", "Office, a raised trading floor (+0.30 m, ramp and steps) with low 1.35 m panels: from it you see over the field to the Relay on the lower floor");
        LsEnd();
        yield return 2;

        LsBegin("T2 file mezzanine", 5100f);
        Room(-6f, -4.5f, 6f, 4.5f, 0f, 5.4f, mOWall, mOCarpet, mOCeil, "");
        TsSolid(-6f, -4.5f, -1.8f, 4.5f, 2.5f, 2.7f, mFascia, mOCarpet);
        for (var z = -3.9f; z < 3.9f; z += .9f)
        {
            Kit("Kit_FilingCabinet", -5.5f, 0f, z, 90f);
            Kit("Kit_FilingCabinet", -3.2f, 0f, z, 270f);
            Kit("Kit_FilingCabinet", -5.5f, 2.7f, z, 90f);
        }
        TsSpiral(-1f, -3.5f, 0f, 2.7f, .85f, 233f, 12, 307f, mStair3);
        FlightX(3.6f, 0f, -5.4f, 2.7f, 3.3f, 4.5f, 18, mOCarpet);
        GlassGuard(-1.8f, -2.7f, -1.8f, 3.3f, 2.7f);
        Cluster(.8f, -3.9f, 0f); Cluster(.8f, -.6f, 0f);
        SetPanels(1.65f);
        Lamps(-6f, -4.5f, 6f, 4.5f, 5.4f, mLensCool, LampOf, 20f, 14f, 3, null);
        Lamps(-6f, -4.5f, -1.8f, 4.5f, 2.5f, mLensCool, LampOf, 7f, 7f, 0, null);
        yield return 20;
        LsAim(5.3f, 1.62f, 3f, -3f, 2.2f, -1f, -1f);
        Shot("t2_file_mezzanine", "Office, a file mezzanine at +2.7 m in a 5.4 m room: a spiral at one end, a straight flight at the other (two ways up)");
        LsAim(-4.3f, 2.7f + 1.62f, 1.4f, 3f, .4f, -1.2f, -1f);
        Shot("t2_from_the_mezzanine", "From the mezzanine: the cubicle field below at 1.65 m panels");
        LsEnd();
        yield return 2;
    }

    // ================================================================================ U - Run!: thresholds and the dock release
    static IEnumerator LsSceneU()
    {
        TsInit();
        LsBegin("U dock release", 5300f);
        FloorQ(-1.5f, 0f, 1.5f, 4.1f, 0f, mRunFloor);
        FlightZ(4.1f, 0f, .9f, .45f, -1.5f, 1.5f, 3, mRunFloor);
        FloorQ(-1.5f, 5f, 1.5f, 8.1f, .45f, mRunFloor);
        Fq("ramp", new Vector3(-1.5f, .45f, 8.1f), new Vector3(3f, 0f, 0f), new Vector3(0f, -.45f, 3.6f), mRunFloor, 1f);
        FloorQ(-1.5f, 11.7f, 1.5f, 12f, 0f, mRunFloor);
        WallX(-1.5f, 0f, 12f, 0f, 1.2f, 1, mBlockLo); WallX(-1.5f, 0f, 12f, 1.2f, LsCH, 1, mBlockUp);
        WallX(1.5f, 0f, 12f, 0f, 1.2f, -1, mBlockLo); WallX(1.5f, 0f, 12f, 1.2f, LsCH, -1, mBlockUp);
        WallZ(0f, -1.5f, 1.5f, 0f, LsCH, 1, mBlockUp);
        CeilQ(-1.5f, 0f, 1.5f, 12f, LsCH, mSoffit);
        Cyl("sprinkler main", new Vector3(.6f, 2.62f, 0f), new Vector3(.6f, 2.62f, 12f), .06f, mSprink);
        Box("cable tray", new Vector3(-1.2f, 2.5f, 0f), new Vector3(-.6f, 2.58f, 12f), mGalv, 1f, 1u, "");
        FireWall(4f, 0f); FireWall(8f, .45f);
        for (var z = 1.5f; z < 12f; z += 3f) WrapLamp(0f, LsCH, z);
        const float yb = -1.22f, H = 8.2f;
        FloorQ(-6f, 15f, 6f, 21f, yb, mRunFloor);
        TsSolid(-6f, 12f, 6f, 15f, yb, 0f, mBlockLo, mRunFloor);
        WallX(-6f, 12f, 21f, yb, yb + H, 1, mBlockUp); WallX(6f, 12f, 21f, yb, yb + H, -1, mBlockUp);
        WallZ(21f, -6f, 1.2f, yb, yb + H, -1, mBlockUp); WallZ(21f, 4.8f, 6f, yb, yb + H, -1, mBlockUp); WallZ(21f, 1.2f, 4.8f, yb + 2.1f, yb + H, -1, mBlockUp);
        WallZ(12f, -6f, -1.5f, 0f, yb + H, 1, mBlockUp); WallZ(12f, 1.5f, 6f, 0f, yb + H, 1, mBlockUp); WallZ(12f, -1.5f, 1.5f, LsCH, yb + H, 1, mBlockUp);
        CeilQ(-6f, 12f, 6f, 21f, yb + H, mSoffit);
        FlightZ(17.4f, yb, -2.4f, 1.22f, 3.8f, 5.4f, 8, mRunFloor);
        foreach (var x in new[] { -4.5f, -1.5f, 1.5f }) TsSolid(x - .3f, 15f, x + .3f, 15.12f, -1f, -.3f, mBump3, null);
        Rail(-6f, 15f, 3.8f, 15f, 0f); Rail(5.4f, 15f, 6f, 15f, 0f);
        for (var r = 0; r < 14; r++) TsSolid(-4.8f, 20.88f, -1.2f, 20.98f, yb + .3f * r, yb + .3f * r + .26f, mGalv, null);
        for (var r = 7; r < 14; r++) TsSolid(1.2f, 20.88f, 4.8f, 20.98f, yb + .3f * r, yb + .3f * r + .26f, mGalv, null);
        Fq("outside light", new Vector3(1.2f, yb, 21.4f), new Vector3(3.6f, 0f, 0f), new Vector3(0f, 2.1f, 0f), mGlow, 1f).shadowCastingMode = ShadowCastingMode.Off;
        foreach (var p in new[] { new Vector3(-3f, yb + H - .2f, 15.5f), new Vector3(3f, yb + H - .2f, 15.5f), new Vector3(-3f, yb + H - .2f, 19f), new Vector3(3f, yb + H - .2f, 19f) })
            TsSpot("high bay", p, Vector3.down, 120f, 110f, 15f, new Color(.95f, .97f, 1f), p.x < 0f && p.z < 16f);
        TsSpot("door light", new Vector3(3f, yb + 1.2f, 22.4f), new Vector3(0f, 0f, -1f), 80f, 80f, 12f, new Color(1f, .97f, .9f), true);
        yield return 20;
        LsAim(0f, 1.62f, .8f, 0f, 1.2f, 14f, -1f);
        Shot("u1_corridor_to_dock", "Run!: three risers up through a fire-door frame, a ramp back down at the next, and the bright dock beyond: the release");
        LsAim(-3.6f, 1.62f, 12.9f, 2.2f, -.4f, 21f, -1f);
        Shot("u2_dock", "On the dock platform at corridor level: the bay drops 1.22 m to the truck floor; the half-open roll-up door is the brightest thing");
        LsAim(.9f, 1.62f, 2.2f, -.2f, .25f, 5.4f, -1f);
        Shot("u3_threshold", "A threshold: the floor steps up 0.45 m through the frame while the soffit stays, so the corridor gets lower");
        LsEnd();
        yield return 2;
    }
}
