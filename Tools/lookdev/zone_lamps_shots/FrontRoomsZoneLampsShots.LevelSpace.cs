// LEVEL SPACE SHOTS: direction stills for research/level_space (K research sub-chat, 2026-10-31).
// CLONE ONLY (W/proj_levelspace); never lands in main. ASCII only, no C# tuples, no GetInstanceID.
// Stand-in scenes far from the map (same ambient, fog and post as the game), built one at a time: build, shoot, destroy.
// Run: Unity -batchmode -projectPath W/proj_levelspace -buildTarget OSXUniversal -executeMethod FrontRoomsZoneLampsShots.RunBatch
//      -zlZone levelspace -zlOption all|<scene letters A..I> -frGlassRT off -logFile <f>
// Output: W/level_space/renders/levelspace/<option>/<pose>.jpg
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public static partial class FrontRoomsZoneLampsShots
{
    static readonly Vector3 LS0 = new Vector3(6000f, 0f, -6000f);
    const float LsFF = 4f, LsCH = 2.9f, LsPlen = 1.1f;
    static Transform lsRoot;
    static int lsLamps;
    static readonly List<Transform> lsPanels = new List<Transform>();
    static readonly List<Light> lsMains = new List<Light>();
    static readonly List<Light> lsEmerg = new List<Light>();
    static readonly List<Renderer> lsMainLens = new List<Renderer>();
    static Material mWall, mCarpet, mCeil, mCove, mPlenum, mSlab, mFascia, mLens, mLensCool, mMetal, mOWall, mOCarpet, mOCeil, mGlass, mTile;
    static Material mBlockLo, mBlockUp, mRunFloor, mSoffit, mGalv, mSprink, mDoorFrame, mRelay, lsWater, mGlow;
    static readonly Color LampL0 = new Color(1f, .96f, .88f), LampOf = new Color(.90f, .96f, 1f), LampWrap = new Color(.90f, .95f, 1f), LampEm = new Color(1f, .84f, .64f);

    static bool LsWant(string k) { return Opt == "all" || Opt.Contains(k); }

    static IEnumerator LevelSpaceMain()
    {
        mWall = SurfMat("L0_Wallpaper"); mCarpet = SurfMat("L0_Carpet"); mCeil = SurfMat("L0_Ceiling"); mCove = SurfMat("Cove_Base");
        mPlenum = SurfMat("Plenum_Dark"); mSlab = SurfMat("Wall_Compound"); mFascia = SurfMat("Wall_Primer");
        mLens = SurfMat("Troffer_Lens"); mLensCool = SurfMat("Troffer_Lens_Cool"); mMetal = SurfMat("Painted_Metal");
        mOWall = SurfMat("Office_Wall"); mOCarpet = SurfMat("Office_Carpet"); mOCeil = SurfMat("Office_Ceiling"); mGlass = SurfMat("Glass_Window");
        mTile = SurfMat("Prop_CeramicGlaze"); mBlockLo = SurfMat("RunB_BlockLower"); mBlockUp = SurfMat("RunB_BlockUpper");
        mRunFloor = SurfMat("RunB_FloorWax"); mSoffit = SurfMat("RunB_Soffit"); mGalv = SurfMat("RunB_GalvSteel"); mSprink = SurfMat("RunB_SprinklerRed");
        mDoorFrame = SurfMat("Door_Frame"); mRelay = SurfMat("Prop_PlasticBlack");
        lsWater = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "LS water glow" };
        lsWater.SetColor("_BaseColor", new Color(.22f, 1.25f, 1.2f));
        mGlow = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "LS laylight" };
        mGlow.SetColor("_BaseColor", new Color(1.05f, 1.08f, 1.1f));
        Log("level space | scenes " + Opt + " | far clip was " + F3(cam.farClipPlane));
        if (LsWant("A")) yield return LsSceneA();
        if (LsWant("B")) yield return LsSceneB();
        if (LsWant("C")) yield return LsSceneC();
        if (LsWant("D")) yield return LsSceneD();
        if (LsWant("E")) yield return LsSceneE();
        if (LsWant("F")) yield return LsSceneF();
        if (LsWant("G")) yield return LsSceneG();
        if (LsWant("H")) yield return LsSceneH();
        if (LsWant("I")) yield return LsSceneI();
        if (LsWant2("J")) yield return LsSceneJ();
        if (LsWant2("K")) yield return LsSceneK();
        if (LsWant2("L")) yield return LsSceneL();
        if (LsWant2("M")) yield return LsSceneM();
        if (LsWant2("N")) yield return LsSceneN();
        if (LsWant3("O")) yield return LsSceneO();
        if (LsWant3("P")) yield return LsSceneP();
        if (LsWant3("Q")) yield return LsSceneQ();
        if (LsWant3("R")) yield return LsSceneR();
        if (LsWant3("S")) yield return LsSceneS();
        if (LsWant3("T")) yield return LsSceneT();
        if (LsWant3("U")) yield return LsSceneU();
        if (LsWant4("V")) yield return LsSceneV();
        if (LsWant4("W")) yield return LsSceneW();
        if (LsWant4("X")) yield return LsSceneX();
        if (LsWant4("Y")) yield return LsSceneY();
        Stat();
    }

    // ------------------------------------------------------------------------------------------ scene plumbing
    static void LsBegin(string name, float dx)
    {
        lsRoot = new GameObject("LS " + name).transform;
        lsRoot.position = LS0 + new Vector3(dx, 0f, 0f);
        standRoot = lsRoot;
        lsLamps = 0; lsPanels.Clear(); lsMains.Clear(); lsEmerg.Clear(); lsMainLens.Clear();
        preRender = LsPre;
        Log("scene " + name + " at " + V3(lsRoot.position));
    }

    static void LsPre() { cam.farClipPlane = 160f; }

    static void LsEnd()
    {
        Log("scene end | lamps " + lsLamps + " | panels " + lsPanels.Count);
        if (lsRoot != null) Object.DestroyImmediate(lsRoot.gameObject);
        lsRoot = null;
        preRender = null;
    }

    static Vector3 LsW(float x, float y, float z) { return lsRoot.position + new Vector3(x, y, z); }
    static void LsAim(float ex, float ey, float ez, float tx, float ty, float tz, float fov) { Aim(LsW(ex, ey, ez), LsW(tx, ty, tz), fov); }
    static Rect Grow(Rect r, float m) { return Rect.MinMaxRect(r.xMin - m, r.yMin - m, r.xMax + m, r.yMax + m); }

    // A quad o, o+v, o+u+v, o+u; its front faces Cross(v, u). Stand-in walls are single quads, so they cast shadows from both sides.
    static Renderer Fq(string n, Vector3 o, Vector3 u, Vector3 v, Material m, float uv)
    {
        var r = Quad(n, o, o + v, o + u + v, o + u, m, uv, 1u);
        r.shadowCastingMode = ShadowCastingMode.TwoSided;
        return r;
    }

    static void FloorQ(float x0, float z0, float x1, float z1, float y, Material m)
    {
        if (x1 - x0 > .001f && z1 - z0 > .001f) Fq("floor", new Vector3(x0, y, z0), new Vector3(x1 - x0, 0f, 0f), new Vector3(0f, 0f, z1 - z0), m, 1.5f);
    }

    static void CeilQ(float x0, float z0, float x1, float z1, float y, Material m)
    {
        if (x1 - x0 > .001f && z1 - z0 > .001f) Fq("ceiling", new Vector3(x0, y, z0), new Vector3(0f, 0f, z1 - z0), new Vector3(x1 - x0, 0f, 0f), m, 1.2f);
    }

    // A wall in the plane x = const, seen from +x (side > 0) or from -x.
    static void WallX(float x, float z0, float z1, float y0, float y1, int side, Material m)
    {
        if (z1 - z0 < .001f || y1 - y0 < .001f) return;
        if (side > 0) Fq("wall", new Vector3(x, y0, z0), new Vector3(0f, 0f, z1 - z0), new Vector3(0f, y1 - y0, 0f), m, 1.2f);
        else Fq("wall", new Vector3(x, y0, z0), new Vector3(0f, y1 - y0, 0f), new Vector3(0f, 0f, z1 - z0), m, 1.2f);
    }

    // A wall in the plane z = const, seen from +z (side > 0) or from -z.
    static void WallZ(float z, float x0, float x1, float y0, float y1, int side, Material m)
    {
        if (x1 - x0 < .001f || y1 - y0 < .001f) return;
        if (side > 0) Fq("wall", new Vector3(x0, y0, z), new Vector3(0f, y1 - y0, 0f), new Vector3(x1 - x0, 0f, 0f), m, 1.2f);
        else Fq("wall", new Vector3(x0, y0, z), new Vector3(x1 - x0, 0f, 0f), new Vector3(0f, y1 - y0, 0f), m, 1.2f);
    }

    // A room seen from inside. skip letters: f c W E S N (floor, ceiling, walls at -x, +x, -z, +z).
    static void Room(float x0, float z0, float x1, float z1, float y0, float h, Material wall, Material floor, Material ceil, string skip)
    {
        if (!skip.Contains("f")) FloorQ(x0, z0, x1, z1, y0, floor);
        if (!skip.Contains("c")) CeilQ(x0, z0, x1, z1, y0 + h, ceil);
        if (!skip.Contains("W")) { WallX(x0, z0, z1, y0, y0 + h, 1, wall); WallX(x0 + .006f, z0, z1, y0, y0 + .1f, 1, mCove); }
        if (!skip.Contains("E")) { WallX(x1, z0, z1, y0, y0 + h, -1, wall); WallX(x1 - .006f, z0, z1, y0, y0 + .1f, -1, mCove); }
        if (!skip.Contains("S")) { WallZ(z0, x0, x1, y0, y0 + h, 1, wall); WallZ(z0 + .006f, x0, x1, y0, y0 + .1f, 1, mCove); }
        if (!skip.Contains("N")) { WallZ(z1, x0, x1, y0, y0 + h, -1, wall); WallZ(z1 - .006f, x0, x1, y0, y0 + .1f, -1, mCove); }
    }

    // Faces round a rectangular void centred on (cx, cz), looking into the void, from y0 to y1.
    static void VoidBand(float cx, float cz, float hx, float hz, float y0, float y1, Material m)
    {
        WallX(cx - hx, cz - hz, cz + hz, y0, y1, 1, m);
        WallX(cx + hx, cz - hz, cz + hz, y0, y1, -1, m);
        WallZ(cz - hz, cx - hx, cx + hx, y0, y1, 1, m);
        WallZ(cz + hz, cx - hx, cx + hx, y0, y1, -1, m);
    }

    static void CeilHoles(Rect r, List<Rect> holes, float y, Material m)
    {
        var mesh = FillMesh(r, holes, y);
        var uv = mesh.uv;
        for (var i = 0; i < uv.Length; i++) uv[i] = uv[i] / 1.2f;
        mesh.uv = uv;
        var go = new GameObject("ceiling with holes");
        go.transform.SetParent(lsRoot, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = m;
        mr.shadowCastingMode = ShadowCastingMode.TwoSided;
    }

    static Light LsLamp(float x, float z, float yc, Material lens, Color col, float intensity, float range, bool shadow)
    {
        var r = Fq("lens", new Vector3(x - .3f, yc - .012f, z - .6f), new Vector3(0f, 0f, 1.2f), new Vector3(.6f, 0f, 0f), lens, 1f);
        r.shadowCastingMode = ShadowCastingMode.Off;
        var b = new MaterialPropertyBlock();
        b.SetColor("_EmissionColor", col * 2.2f);
        r.SetPropertyBlock(b);
        lsMainLens.Add(r);
        var l = AddSpot("lamp", new Vector3(x, yc - .06f, z), Vector3.down, 150f, 96f, intensity, range, col, shadow, 1u);
        lsMains.Add(l);
        lsLamps++;
        return l;
    }

    static void Lamps(float x0, float z0, float x1, float z1, float yc, Material lens, Color col, float intensity, float range, int shadowEvery, Rect[] holes)
    {
        var k = 0;
        for (var x = x0 + 1.5f; x < x1 - .2f; x += 3f)
        for (var z = z0 + 1.5f; z < z1 - .2f; z += 3f)
        {
            var skip = false;
            if (holes != null) foreach (var h in holes) if (h.Contains(new Vector2(x, z))) { skip = true; break; }
            if (skip) continue;
            LsLamp(x, z, yc, lens, col, intensity, range, shadowEvery > 0 && k % shadowEvery == 0);
            k++;
        }
    }

    static void Rail(float x0, float z0, float x1, float z1, float y)
    {
        var a = new Vector3(x0, y, z0);
        var b = new Vector3(x1, y, z1);
        Cyl("rail top", a + Vector3.up * 1.07f, b + Vector3.up * 1.07f, .025f, mMetal);
        Cyl("rail mid", a + Vector3.up * .55f, b + Vector3.up * .55f, .012f, mMetal);
        var n = Mathf.Max(1, Mathf.RoundToInt(Vector3.Distance(a, b) / 1.2f));
        for (var i = 0; i <= n; i++)
        {
            var p = Vector3.Lerp(a, b, i / (float)n);
            Cyl("rail post", p, p + Vector3.up * 1.07f, .02f, mMetal);
        }
    }

    static void GlassGuard(float x0, float z0, float x1, float z1, float y)
    {
        var a = new Vector3(x0, y + .08f, z0);
        var u = new Vector3(x1 - x0, 0f, z1 - z0);
        var v = new Vector3(0f, .95f, 0f);
        Fq("guard glass", a, u, v, mGlass, 1f);
        Fq("guard glass", a, v, u, mGlass, 1f);
        Cyl("guard cap", new Vector3(x0, y + 1.07f, z0), new Vector3(x1, y + 1.07f, z1), .03f, mMetal);
        Cyl("guard shoe", new Vector3(x0, y + .05f, z0), new Vector3(x1, y + .05f, z1), .03f, mMetal);
    }

    static void RelayStandIn(float x, float y, float z)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        Object.DestroyImmediate(g.GetComponent<Collider>());
        g.name = "Relay stand-in 2.05 m";
        g.transform.SetParent(lsRoot, false);
        g.transform.localPosition = new Vector3(x, y + 1.025f, z);
        g.transform.localScale = new Vector3(.6f, 1.025f, .6f);
        g.GetComponent<Renderer>().sharedMaterial = mRelay;
    }

    static GameObject Kit(string name, float x, float y, float z, float yaw)
    {
        var g = FrontRoomsKitLibrary.Spawn(name, lsRoot, new Vector3(x, y, z), yaw, null, false);
        if (g == null) Log("WARN kit missing " + name);
        return g;
    }

    static void Panel(float x, float y, float z, float yaw, float sy)
    {
        var g = FrontRoomsKitLibrary.Spawn("Kit_CubiclePanel", lsRoot, new Vector3(x, y, z), yaw, new Vector3(1f, sy, 1f), false);
        if (g != null) lsPanels.Add(g.transform);
    }

    static void PanelRow(float xa, float za, float xb, float zb, float y, float sy)
    {
        var a = new Vector3(xa, y, za);
        var b = new Vector3(xb, y, zb);
        var n = Mathf.Max(1, Mathf.FloorToInt(Vector3.Distance(a, b) / 1.524f));
        var dir = (b - a).normalized;
        var yaw = Mathf.Abs(dir.x) > .5f ? 0f : 90f;
        for (var i = 0; i < n; i++)
        {
            var p = a + dir * (1.524f * (i + .5f));
            Panel(p.x, y, p.z, yaw, sy);
        }
    }

    static void SetPanels(float h)
    {
        var sy = h / 1.52f;
        foreach (var t in lsPanels) if (t != null) t.localScale = new Vector3(1f, sy, 1f);
    }

    // One office floor of four workstations round a spine (2 x 2 cubicles of 1.524 m), panels on the spine, the cross and both ends.
    static void Cluster(float x, float z, float y)
    {
        const float c = 1.524f;
        Panel(x + c * .5f, y, z + c, 0f, 1f); Panel(x + c * 1.5f, y, z + c, 0f, 1f);
        Panel(x + c, y, z + c * .5f, 90f, 1f); Panel(x + c, y, z + c * 1.5f, 90f, 1f);
        Panel(x, y, z + c * .5f, 90f, 1f); Panel(x, y, z + c * 1.5f, 90f, 1f);
        Panel(x + 2f * c, y, z + c * .5f, 90f, 1f); Panel(x + 2f * c, y, z + c * 1.5f, 90f, 1f);
        for (var col = 0; col < 2; col++)
        {
            var cx = x + c * (col + .5f);
            Kit("Kit_OfficeDesk", cx, y, z + c - .43f, 180f);
            Kit("Kit_TaskChair", cx, y, z + c - 1.1f, 0f);
            Kit("Kit_CRTMonitor", cx, y + .74f, z + c - .32f, 180f);
            Kit("Kit_OfficeDesk", cx, y, z + c + .43f, 0f);
            Kit("Kit_TaskChair", cx, y, z + c + 1.1f, 180f);
            Kit("Kit_CRTMonitor", cx, y + .74f, z + c + .32f, 0f);
        }
    }

    // One floor round a void: floor and ceiling rings, the slab edge band under it (facing the void), back walls with dark doors,
    // a guard on the edge and a row of lamps 1.5 m outside the edge.
    static void Ring(float y, float hx, float hz, float w, bool office, bool glass, int shadowEvery, bool guards = true, float ceilInner = 0f)
    {
        var wall = office ? mOWall : mWall;
        var floor = office ? mOCarpet : mCarpet;
        var ceil = office ? mOCeil : mCeil;
        var lens = office ? mLensCool : mLens;
        var col = office ? LampOf : LampL0;
        float ox = hx + w, oz = hz + w, yc = y + LsCH;
        float cx = ceilInner > hx ? ceilInner : hx, cz = ceilInner > hx ? hz + (ceilInner - hx) : hz;
        FloorQ(-ox, -oz, ox, -hz, y, floor); FloorQ(-ox, hz, ox, oz, y, floor);
        FloorQ(-ox, -hz, -hx, hz, y, floor); FloorQ(hx, -hz, ox, hz, y, floor);
        CeilQ(-ox, -oz, ox, -cz, yc, ceil); CeilQ(-ox, cz, ox, oz, yc, ceil);
        CeilQ(-ox, -cz, -cx, cz, yc, ceil); CeilQ(cx, -cz, ox, cz, yc, ceil);
        VoidBand(0f, 0f, hx, hz, y - LsPlen, y, mFascia);
        WallX(-ox, -oz, oz, y, yc, 1, wall); WallX(ox, -oz, oz, y, yc, -1, wall);
        WallZ(-oz, -ox, ox, y, yc, 1, wall); WallZ(oz, -ox, ox, y, yc, -1, wall);
        WallZ(oz - .01f, -.5f, .5f, y, y + 2.1f, -1, mPlenum); WallZ(-oz + .01f, 1.5f, 2.5f, y, y + 2.1f, 1, mPlenum);
        WallX(-ox + .01f, -1.5f, -.5f, y, y + 2.1f, 1, mPlenum); WallX(ox - .01f, .5f, 1.5f, y, y + 2.1f, -1, mPlenum);
        if (guards && glass)
        {
            GlassGuard(-hx, -hz, hx, -hz, y); GlassGuard(-hx, hz, hx, hz, y); GlassGuard(-hx, -hz, -hx, hz, y); GlassGuard(hx, -hz, hx, hz, y);
        }
        else if (guards)
        {
            Rail(-hx, -hz, hx, -hz, y); Rail(-hx, hz, hx, hz, y); Rail(-hx, -hz, -hx, hz, y); Rail(hx, -hz, hx, hz, y);
        }
        var k = 0;
        for (var x = -hx + 1.5f; x < hx; x += 3f)
        {
            LsLamp(x, -hz - 1.5f, yc, lens, col, 5f, 8f, shadowEvery > 0 && k++ % shadowEvery == 0);
            LsLamp(x, hz + 1.5f, yc, lens, col, 5f, 8f, false);
        }
        for (var z = -hz + 1.5f; z < hz; z += 3f)
        {
            LsLamp(-hx - 1.5f, z, yc, lens, col, 5f, 8f, false);
            LsLamp(hx + 1.5f, z, yc, lens, col, 5f, 8f, false);
        }
        if (w > 5f)
        {
            for (var x = -ox + 1.5f; x < ox; x += 3f) { LsLamp(x, -oz + 1.5f, yc, lens, col, 5f, 8f, false); LsLamp(x, oz - 1.5f, yc, lens, col, 5f, 8f, false); }
            for (var z = -hz + 1.5f; z < hz; z += 3f) { LsLamp(-ox + 1.5f, z, yc, lens, col, 5f, 8f, false); LsLamp(ox - 1.5f, z, yc, lens, col, 5f, 8f, false); }
        }
    }

    static void WrapLamp(float x, float y, float z)
    {
        Kit("Kit_WrapFixture_4ft", x, y, z, 90f);
        var r = Fq("wrap lens", new Vector3(x - .1f, y - .085f, z - .6f), new Vector3(0f, 0f, 1.2f), new Vector3(.2f, 0f, 0f), mLensCool, 1f);
        r.shadowCastingMode = ShadowCastingMode.Off;
        var b = new MaterialPropertyBlock();
        b.SetColor("_EmissionColor", LampWrap * 2f);
        r.SetPropertyBlock(b);
        lsMainLens.Add(r);
        lsMains.Add(AddSpot("wrap", new Vector3(x, y - .12f, z), Vector3.down, 160f, 110f, 6f, 9f, LampWrap, false, 1u));
        lsLamps++;
    }

    static void Battery(float x, float y, float z, float yaw)
    {
        Kit("Kit_EmergencyTwinHead", x, y, z, yaw);
        var dir = (Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, -.75f, 1f)).normalized;
        lsEmerg.Add(AddSpot("battery head", new Vector3(x, y + .1f, z) + dir * .2f, dir, 75f, 40f, 9f, 14f, LampEm, true, 1u));
    }

    static void Mains(bool on)
    {
        foreach (var l in lsMains) if (l != null) l.enabled = on;
        foreach (var l in lsEmerg) if (l != null) l.enabled = !on;
        foreach (var r in lsMainLens)
        {
            if (r == null) continue;
            var b = new MaterialPropertyBlock();
            r.GetPropertyBlock(b);
            b.SetColor("_EmissionColor", on ? LampWrap * 2f : Color.black);
            r.SetPropertyBlock(b);
        }
    }

    // ================================================================================ A - Level 0: open to below
    static IEnumerator LsSceneA()
    {
        LsBegin("A open to below", 0f);
        float x0 = -12f, x1 = 12f, z0 = -9f, z1 = 12f;
        var hole = Rect.MinMaxRect(-1.5f, 1.5f, 4.5f, 7.5f);
        FloorQ(x0, z0, x1, hole.yMin, 0f, mCarpet); FloorQ(x0, hole.yMax, x1, z1, 0f, mCarpet);
        FloorQ(x0, hole.yMin, hole.xMin, hole.yMax, 0f, mCarpet); FloorQ(hole.xMax, hole.yMin, x1, hole.yMax, 0f, mCarpet);
        Room(x0, z0, x1, z1, 0f, LsCH, mWall, mCarpet, mCeil, "fc");
        var miss = Rect.MinMaxRect(-7.8f, 2.4f, -6.6f, 3.6f);
        CeilHoles(Rect.MinMaxRect(x0, z0, x1, 10.8f), new List<Rect> { miss }, LsCH, mCeil);
        CeilQ(x0, 10.8f, x1, z1, 2.4f, mFascia);
        WallZ(10.8f, x0, x1, 2.4f, LsCH, -1, mFascia);
        float px0 = -9f, px1 = -5.4f, pz0 = 1.2f, pz1 = 4.8f, py1 = LsCH + .95f;
        CeilQ(px0, pz0, px1, pz1, py1, mPlenum);
        WallX(px0, pz0, pz1, LsCH, py1, 1, mPlenum); WallX(px1, pz0, pz1, LsCH, py1, -1, mPlenum);
        WallZ(pz0, px0, px1, LsCH, py1, 1, mPlenum); WallZ(pz1, px0, px1, LsCH, py1, -1, mPlenum);
        Box("duct", new Vector3(px0, 3.2f, 2.5f), new Vector3(px1, 3.6f, 3.3f), mGalv, 1f, 1u, "");
        Cyl("sprinkler branch", new Vector3(px0, 3.05f, 3.95f), new Vector3(px1, 3.05f, 3.95f), .03f, mSprink);
        Cyl("conduit", new Vector3(px0, 3.72f, 1.7f), new Vector3(px1, 3.72f, 1.95f), .014f, mGalv);
        var hc = hole.center;
        float hhx = hole.width * .5f, hhz = hole.height * .5f;
        VoidBand(hc.x, hc.y, hhx, hhz, -.03f, 0f, mCarpet);
        VoidBand(hc.x, hc.y, hhx, hhz, -.23f, -.03f, mSlab);
        VoidBand(hc.x, hc.y, hhx, hhz, -LsPlen, -.23f, mPlenum);
        Cyl("cut duct", new Vector3(hole.xMin, -.7f, 4.0f), new Vector3(hole.xMin + .02f, -.7f, 4.0f), .2f, mGalv);
        Rail(hole.xMin, hole.yMin, hole.xMax, hole.yMin, 0f); Rail(hole.xMin, hole.yMax, hole.xMax, hole.yMax, 0f);
        Rail(hole.xMin, hole.yMin, hole.xMin, hole.yMax, 0f); Rail(hole.xMax, hole.yMin, hole.xMax, hole.yMax, 0f);
        Lamps(x0, z0, x1, 10.8f, LsCH, mLens, LampL0, 5f, 8f, 3, new[] { Grow(hole, .7f), Grow(miss, .7f) });
        var yb = -LsFF;
        Room(x0, z0, x1, z1, yb, LsCH, mWall, mCarpet, mCeil, "c");
        CeilHoles(Rect.MinMaxRect(x0, z0, x1, z1), new List<Rect> { hole }, yb + LsCH, mCeil);
        Lamps(x0, z0, x1, z1, yb + LsCH, mLens, LampL0, 7f, 8.5f, 3, new[] { Grow(hole, .7f) });
        Kit("Kit_OfficeDesk", 2.6f, yb, 6.3f, 180f);
        Kit("Kit_TaskChair", 1.1f, yb, 4.1f, 200f);
        Kit("Kit_FilingCabinet", -1.1f, yb, 7.1f, 90f);
        Kit("Kit_FilingCabinet", -1.1f, yb, 6.6f, 90f);
        yield return 20;
        LsAim(-3.4f, 1.62f, -1.8f, 1.8f, -3f, 5.2f, 72f);
        Shot("a1_open_to_below", "Level 0: a 6 x 6 m opening in the floor shows the floor below; rail 1.07 m");
        LsAim(1.5f, 1.62f, .7f, 1.6f, -4f, 5.2f, 72f);
        Shot("a2_section_down", "Level 0: at the rail, the cut shows carpet, slab and plenum, then the floor below 4 m down");
        LsAim(-7.2f, 1.62f, .5f, -7.2f, 3.7f, 3.0f, 72f);
        Shot("a3_plenum_soffit", "Level 0: two missing tiles open the 1 m plenum (duct, sprinkler branch)");
        LsEnd();
        yield return 2;
    }

    // ================================================================================ B - Level 0: the tall grid
    static IEnumerator LsSceneB()
    {
        LsBegin("B tall grid", 200f);
        const float T = 14.5f;
        Room(-1.5f, -12f, 1.5f, -.08f, 0f, LsCH, mWall, mCarpet, mCeil, "N");
        WallZ(-.08f, -1.5f, -.8f, 0f, LsCH, -1, mWall); WallZ(-.08f, .8f, 1.5f, 0f, LsCH, -1, mWall); WallZ(-.08f, -.8f, .8f, 2.2f, LsCH, -1, mWall);
        WallX(-.8f, -.08f, .08f, 0f, 2.2f, 1, mWall); WallX(.8f, -.08f, .08f, 0f, 2.2f, -1, mWall);
        CeilQ(-.8f, -.08f, .8f, .08f, 2.2f, mWall);
        FloorQ(-.8f, -.08f, .8f, .08f, 0f, mCarpet);
        Lamps(-1.5f, -12f, 1.5f, -.08f, LsCH, mLens, LampL0, 5f, 8f, 2, null);
        Room(-6f, .08f, 6f, 12f, 0f, T, mWall, mCarpet, mCeil, "S");
        WallZ(.08f, -6f, -.8f, 0f, T, 1, mWall); WallZ(.08f, .8f, 6f, 0f, T, 1, mWall); WallZ(.08f, -.8f, .8f, 2.2f, T, 1, mWall);
        WallZ(.086f, -6f, -.8f, 0f, .1f, 1, mCove); WallZ(.086f, .8f, 6f, 0f, .1f, 1, mCove);
        Lamps(-6f, .08f, 6f, 12f, T, mLens, LampL0, 32f, 22f, 4, null);
        Kit("Kit_WaterCooler", -5.75f, 0f, 7f, 90f);
        Kit("Kit_TaskChair", 1.8f, 0f, 6.5f, 30f);
        yield return 20;
        LsAim(2.6f, 1.62f, 10.2f, -.2f, 5.2f, 0f, -1f);
        Shot("b1_corridor_to_tall", "Level 0: from inside, the 2.2 m doorway you came through sits at the foot of a wall 14.5 m high, the same paper all the way up");
        LsAim(-3.6f, 1.62f, 2.8f, 2.4f, 12f, 10.5f, 75f);
        Shot("b2_tall_looking_up", "Level 0: inside, the grid ceiling and its troffers sit five storeys up");
        LsEnd();
        yield return 2;
    }

    // ================================================================================ C - The well through every floor
    static IEnumerator LsSceneC()
    {
        LsBegin("C the well", 400f);
        const float hx = 4.5f;
        Ring(0f, hx, hx, 7.5f, false, false, 2);
        for (var k = 1; k <= 6; k++) Ring(-LsFF * k, hx, hx, 3f, k % 2 == 0, k % 2 == 0, 0);
        var yb = -LsFF * 7f - 2f;
        const float bx = 9f;
        Room(-bx, -bx, bx, bx, yb, 5.4f, mTile, mTile, mTile, "c");
        CeilHoles(Rect.MinMaxRect(-bx, -bx, bx, bx), new List<Rect> { Rect.MinMaxRect(-hx, -hx, hx, hx) }, yb + 5.4f, mTile);
        VoidBand(0f, 0f, hx, hx, yb + 5.4f, -LsFF * 6f - LsPlen, mFascia);
        var water = Fq("water", new Vector3(-bx + .6f, yb + .06f, -bx + .6f), new Vector3(2f * bx - 1.2f, 0f, 0f), new Vector3(0f, 0f, 2f * bx - 1.2f), lsWater, 1f);
        water.shadowCastingMode = ShadowCastingMode.Off;
        AddSpot("pool glow up", new Vector3(0f, yb + .4f, 0f), Vector3.up, 120f, 80f, 140f, 48f, new Color(.35f, .95f, .92f), false, 1u);
        AddSpot("pool hall", new Vector3(0f, yb + 5.2f, -6f), Vector3.down, 150f, 100f, 18f, 9f, new Color(.85f, .97f, 1f), false, 1u);
        yield return 20;
        LsAim(0f, 1.62f, -5.3f, 0f, -16f, 2f, -1f);
        Shot("c1_well_approach", "The well: a 9 x 9 m void through every floor; turquoise light from the pool at the bottom, 30 m down");
        LsAim(0f, 1.62f, -4.2f, .05f, yb, .3f, 74f);
        Shot("c2_well_down", "The well from the Level 0 rail: six floors below, then the pool");
        LsEnd();
        yield return 2;
    }

    // ================================================================================ D - Level 0: sunken suite
    static IEnumerator LsSceneD()
    {
        LsBegin("D sunken suite", 600f);
        float x0 = -9f, x1 = 9f;
        const float pit = -2.4f, rise = 2.4f / 14f, tread = .3f;
        FloorQ(x0, -6f, x1, -3f, 0f, mCarpet);
        WallZ(-6f, x0, x1, 0f, LsCH, 1, mWall); WallZ(-5.994f, x0, x1, 0f, .1f, 1, mCove);
        WallX(x0, -6f, 12f, pit, LsCH, 1, mWall); WallX(x1, -6f, 12f, pit, LsCH, -1, mWall); WallZ(12f, x0, x1, pit, LsCH, -1, mWall);
        CeilQ(x0, -6f, x1, 12f, LsCH, mCeil);
        for (var i = 0; i < 13; i++)
            Box("step", new Vector3(x0, pit, -3f + i * tread), new Vector3(x1, -(i + 1) * rise, -3f + (i + 1) * tread), mCarpet, 1.5f, 1u, "-y-z-x+x");
        FloorQ(x0, -3f + 13f * tread, x1, 6f, pit, mCarpet);
        for (var j = 0; j < 5; j++)
            Box("dais step", new Vector3(x0, pit, 6f + j * tread), new Vector3(x1, pit + (j + 1) * .18f, 7.5f), mCarpet, 1.5f, 1u, "-y+z-x+x");
        Box("dais", new Vector3(x0, pit, 7.5f), new Vector3(x1, pit + .9f, 12f), mCarpet, 1.5f, 1u, "-y+z-x+x");
        Lamps(x0, -6f, x1, 12f, LsCH, mLens, LampL0, 9f, 11f, 3, null);
        Kit("Kit_Sofa3", -2f, pit, 3.2f, 180f);
        Kit("Kit_ClubChair", 2.4f, pit, 2.4f, 230f);
        Kit("Kit_ClubChair", -5f, pit, 1.6f, 130f);
        Kit("Kit_Urn", 0f, pit + .9f, 10f, 0f);
        yield return 20;
        LsAim(-6f, 1.62f, -5.2f, 2f, -2.2f, 5f, -1f);
        Shot("d1_sunken_suite", "Level 0: the suite floor drops a full storey (2.4 m, fourteen risers); the ceiling stays, so the room is 5.3 m tall");
        LsAim(3.5f, pit + 1.62f, 5f, -3f, .9f, -5f, -1f);
        Shot("d2_from_the_pit", "Level 0: from the pit, the corridor edge is a stage above you");
        LsEnd();
        yield return 2;
    }

    // ================================================================================ E - Office: atrium through stacked floors
    static IEnumerator LsSceneE()
    {
        LsBegin("E office atrium", 800f);
        const float hx = 6f, hz = 4.5f, w = 4.5f;
        for (var k = -2; k <= 2; k++)
        {
            var y = k * LsFF;
            Ring(y, hx, hz, w, true, true, k == 0 ? 3 : 0);
            PanelRow(-hx + .3f, -hz - 1.8f, hx - .3f, -hz - 1.8f, y, 1f);
            PanelRow(-hx + .3f, hz + 1.8f, hx - .3f, hz + 1.8f, y, 1f);
            PanelRow(-hx - 1.8f, -hz + .3f, -hx - 1.8f, hz - .3f, y, 1f);
            PanelRow(hx + 1.8f, -hz + .3f, hx + 1.8f, hz - .3f, y, 1f);
        }
        var yf = -2f * LsFF;
        FloorQ(-hx, -hz, hx, hz, yf, mOCarpet);
        for (var i = -1; i <= 1; i += 2)
        {
            Box("planter", new Vector3(i * 3f - .7f, yf, -.7f), new Vector3(i * 3f + .7f, yf + .55f, .7f), mMetal, 1f, 1u, "-y");
            Cyl("trunk", new Vector3(i * 3f, yf + .55f, 0f), new Vector3(i * 3f, yf + 3.2f, 0f), .06f, SurfMat("Prop_WoodDark"));
        }
        var top = 2f * LsFF + LsCH;
        VoidBand(0f, 0f, hx, hz, top, top + 1.6f, mFascia);
        CeilQ(-hx, -hz, hx, hz, top + 1.6f, mGlow);
        AddSpot("laylight", new Vector3(0f, top + 1.5f, 0f), Vector3.down, 120f, 90f, 30f, 30f, new Color(.95f, .98f, 1f), false, 1u);
        yield return 20;
        LsAim(-4.2f, 1.62f, -5.3f, 4.5f, -1.2f, 3f, 72f);
        Shot("e1_office_atrium", "Office: an atrium through five office floors; the floors above and below are in view, panels at every edge");
        LsAim(-3.5f, yf + 1.62f, -3f, 1.5f, 9f, 2.5f, 75f);
        Shot("e2_office_atrium_up", "Office: from the atrium floor, four floors of office balconies and a laylight");
        LsEnd();
        yield return 2;
    }

    // ================================================================================ F - Office: sunken bullpen and panel heights
    static IEnumerator LsSceneF()
    {
        LsBegin("F sunken bullpen", 1000f);
        float x0 = -12f, x1 = 12f, z0 = -10.5f, z1 = 10.5f, px = 9f, pz = 7.5f;
        const float pit = -1.2f, c = 1.524f, aisle = 1.6f;
        FloorQ(x0, z0, x1, -pz, 0f, mOCarpet); FloorQ(x0, pz, x1, z1, 0f, mOCarpet);
        FloorQ(x0, -pz, -px, pz, 0f, mOCarpet); FloorQ(px, -pz, x1, pz, 0f, mOCarpet);
        Room(x0, z0, x1, z1, 0f, LsCH, mOWall, mOCarpet, mOCeil, "f");
        FloorQ(-px, -pz, px, pz, pit, mOCarpet);
        VoidBand(0f, 0f, px, pz, pit, 0f, mOWall);
        for (var i = 0; i < 6; i++)
            Box("pit step", new Vector3(-px + i * .3f, pit, -1.2f), new Vector3(-px + (i + 1) * .3f, -(i + 1) * (1.2f / 7f), 1.2f), mOCarpet, 1.5f, 1u, "-y-x-z+z");
        Rail(-px, -pz, px, -pz, 0f); Rail(-px, pz, px, pz, 0f); Rail(px, -pz, px, pz, 0f);
        Rail(-px, -pz, -px, -1.2f, 0f); Rail(-px, 1.2f, -px, pz, 0f);
        Lamps(x0, z0, x1, z1, LsCH, mLensCool, LampOf, 6.5f, 9.5f, 3, null);
        var span = 3f * (2f * c) + 2f * aisle;
        var start = -span * .5f;
        for (var i = 0; i < 3; i++)
        for (var j = 0; j < 3; j++)
            Cluster(start + i * (2f * c + aisle), start + j * (2f * c + aisle), pit);
        var aisleMid = start + 2f * c + aisle * .5f;
        RelayStandIn(aisleMid, pit, aisleMid);
        Log("bullpen: " + lsPanels.Count + " panels, Relay at aisle crossing " + F3(aisleMid));
        SetPanels(1.65f);
        yield return 20;
        LsAim(-10.6f, 1.62f, -9.1f, aisleMid, pit, aisleMid, -1f);
        Shot("f1_bullpen_from_edge_165", "Office: from the raised walkway you see over every 1.65 m panel, and the Relay among them");
        var heights = new[] { 1.35f, 1.52f, 1.65f, 2.16f };
        var tags = new[] { "135_low", "152_today", "165_refuge", "216_full" };
        for (var v = 0; v < 4; v++)
        {
            SetPanels(heights[v]);
            yield return 6;
            LsAim(-7.7f, pit + 1.62f, -6.9f, aisleMid, pit + 1.3f, aisleMid, -1f);
            Shot("f" + (v + 2) + "_in_pit_" + tags[v], "Office: inside the bullpen, panels " + F3(heights[v]) + " m; Relay stand-in 2.05 m at the far aisle crossing");
        }
        LsEnd();
        yield return 2;
    }

    // ================================================================================ G - Run!: catwalk over the plant room
    static IEnumerator LsSceneG()
    {
        LsBegin("G plant room", 1200f);
        float x0 = -9f, x1 = 9f, z0 = -15f, z1 = 15f, yf = -6f, yc = 6f;
        Room(x0, z0, x1, z1, yf, yc - yf, mBlockUp, SurfMat("RunB_Floor"), mSoffit, "");
        WallX(x0 + .005f, z0, z1, yf, yf + 1.2f, 1, mBlockLo); WallX(x1 - .005f, z0, z1, yf, yf + 1.2f, -1, mBlockLo);
        WallZ(z0 + .005f, x0, x1, yf, yf + 1.2f, 1, mBlockLo); WallZ(z1 - .005f, x0, x1, yf, yf + 1.2f, -1, mBlockLo);
        Box("catwalk", new Vector3(-.6f, -.08f, z0), new Vector3(.6f, 0f, z1), mGalv, 1f, 1u, "");
        Rail(-.6f, z0, -.6f, z1, 0f); Rail(.6f, z0, .6f, z1, 0f);
        for (var z = z0 + 1.5f; z < z1; z += 3f)
        {
            Cyl("hanger", new Vector3(-.6f, 1.07f, z), new Vector3(-.6f, yc, z), .012f, mGalv);
            Cyl("hanger", new Vector3(.6f, 1.07f, z), new Vector3(.6f, yc, z), .012f, mGalv);
        }
        Box("chiller", new Vector3(-7.5f, yf, -11f), new Vector3(-2.5f, yf + 2.6f, -6f), mMetal, 1f, 1u, "-y");
        Box("chiller", new Vector3(2.5f, yf, -3f), new Vector3(7.5f, yf + 2.6f, 2f), mMetal, 1f, 1u, "-y");
        Box("boiler", new Vector3(-7.5f, yf, 4f), new Vector3(-3f, yf + 3.4f, 10f), SurfMat("Prop_SteelPutty"), 1f, 1u, "-y");
        Cyl("riser", new Vector3(-5f, yf + 2.6f, -8.5f), new Vector3(-5f, yc, -8.5f), .25f, mGalv);
        Cyl("riser", new Vector3(5f, yf + 2.6f, -.5f), new Vector3(5f, yc, -.5f), .25f, mSprink);
        Cyl("riser", new Vector3(-5.2f, yf + 3.4f, 7f), new Vector3(-5.2f, yc, 7f), .18f, mGalv);
        Cyl("main", new Vector3(x0, 2.2f, -6f), new Vector3(x1, 2.2f, -6f), .3f, mGalv);
        Cyl("main", new Vector3(x0, 2.9f, 3f), new Vector3(x1, 2.9f, 3f), .22f, mSprink);
        Cyl("main", new Vector3(x0, 1.9f, 9f), new Vector3(x1, 1.9f, 9f), .18f, mGalv);
        Box("duct", new Vector3(2.2f, 3.4f, z0), new Vector3(3.6f, 4.4f, z1), mGalv, 1f, 1u, "");
        WrapLamp(0f, 2.6f, -12f); WrapLamp(0f, 2.6f, -6.5f); WrapLamp(0f, 2.6f, 5.5f); WrapLamp(0f, 2.6f, 11.5f);
        Battery(x0 + .02f, yf + 4.2f, -2f, 90f); Battery(x1 - .02f, yf + 4.2f, 7f, -90f); Battery(x0 + .02f, yf + 4.2f, 11f, 90f);
        Battery(x1 - .02f, yf + 4.2f, -10f, -90f);
        foreach (var l in lsEmerg) if (l != null) l.enabled = true;
        AddSpot("plant down", new Vector3(-5f, yc - .2f, -8.5f), Vector3.down, 120f, 80f, 26f, 15f, LampWrap, true, 1u);
        AddSpot("plant down", new Vector3(5f, yc - .2f, -.5f), Vector3.down, 120f, 80f, 26f, 15f, LampWrap, false, 1u);
        AddSpot("plant down", new Vector3(-5f, yc - .2f, 7f), Vector3.down, 120f, 80f, 26f, 15f, LampWrap, false, 1u);
        AddSpot("plant down", new Vector3(5f, yc - .2f, 9f), Vector3.down, 120f, 80f, 22f, 15f, LampEm, false, 1u);
        RelayStandIn(4.2f, yf, 7.5f);
        yield return 20;
        LsAim(0f, 1.62f, -13.6f, 0f, -4.2f, 7f, -1f);
        Shot("g1_catwalk", "Run!: the chase crosses a three-storey plant room on a grating catwalk; the Relay stands on the floor 6 m below");
        LsAim(5.8f, yf + 1.62f, -12.6f, -.2f, .8f, 1f, 72f);
        Shot("g2_catwalk_from_below", "Run!: the catwalk seen from the plant floor");
        LsEnd();
        yield return 2;
    }

    // ================================================================================ H - Run!: the descending ramp
    static IEnumerator LsSceneH()
    {
        LsBegin("H ramp", 1400f);
        const float wx = 1.5f, L = 24f, drop = 3f;
        Room(-wx, -6f, wx, 0f, 0f, LsCH, mBlockUp, mRunFloor, mSoffit, "N");
        Fq("ramp floor", new Vector3(-wx, 0f, 0f), new Vector3(2f * wx, 0f, 0f), new Vector3(0f, -drop, L), mRunFloor, 1.5f);
        Fq("ramp soffit", new Vector3(-wx, LsCH, 0f), new Vector3(0f, -drop, L), new Vector3(2f * wx, 0f, 0f), mSoffit, 1.2f);
        Fq("ramp wall W", new Vector3(-wx, 0f, 0f), new Vector3(0f, -drop, L), new Vector3(0f, LsCH, 0f), mBlockUp, 1.2f);
        Fq("ramp wall E", new Vector3(wx, 0f, 0f), new Vector3(0f, LsCH, 0f), new Vector3(0f, -drop, L), mBlockUp, 1.2f);
        Fq("ramp band W", new Vector3(-wx + .005f, 0f, 0f), new Vector3(0f, -drop, L), new Vector3(0f, 1.2f, 0f), mBlockLo, 1.2f);
        Fq("ramp band E", new Vector3(wx - .005f, 0f, 0f), new Vector3(0f, 1.2f, 0f), new Vector3(0f, -drop, L), mBlockLo, 1.2f);
        WallX(-wx + .005f, -6f, 0f, 0f, 1.2f, 1, mBlockLo); WallX(wx - .005f, -6f, 0f, 0f, 1.2f, -1, mBlockLo);
        var yb = -drop;
        Room(-wx, L, wx, L + 6f, yb, LsCH, mBlockUp, mRunFloor, mSoffit, "SN");
        WallX(-wx + .005f, L, L + 6f, yb, yb + 1.2f, 1, mBlockLo); WallX(wx - .005f, L, L + 6f, yb, yb + 1.2f, -1, mBlockLo);
        WallZ(L + 6f, -wx, -.5f, yb, yb + LsCH, -1, mBlockUp); WallZ(L + 6f, .5f, wx, yb, yb + LsCH, -1, mBlockUp);
        WallZ(L + 6f, -.5f, .5f, yb + 2.1f, yb + LsCH, -1, mBlockUp);
        Box("goal frame L", new Vector3(-.56f, yb, L + 5.92f), new Vector3(-.5f, yb + 2.12f, L + 6.08f), mDoorFrame, 1f, 1u, "-y");
        Box("goal frame R", new Vector3(.5f, yb, L + 5.92f), new Vector3(.56f, yb + 2.12f, L + 6.08f), mDoorFrame, 1f, 1u, "-y");
        Box("goal frame H", new Vector3(-.56f, yb + 2.1f, L + 5.92f), new Vector3(.56f, yb + 2.16f, L + 6.08f), mDoorFrame, 1f, 1u, "");
        Room(-6f, L + 6.08f, 6f, L + 18f, yb, 5.4f, mFascia, mRunFloor, mSoffit, "S");
        WallZ(L + 6.08f, -6f, -.5f, yb, yb + 5.4f, 1, mFascia); WallZ(L + 6.08f, .5f, 6f, yb, yb + 5.4f, 1, mFascia);
        WallZ(L + 6.08f, -.5f, .5f, yb + 2.1f, yb + 5.4f, 1, mFascia);
        for (var x = -4.5f; x <= 4.6f; x += 3f)
        for (var z = L + 7.6f; z < L + 18f; z += 3f)
            AddSpot("release", new Vector3(x, yb + 5.3f, z), Vector3.down, 150f, 100f, 30f, 12f, new Color(.95f, .98f, 1f), false, 1u);
        Cyl("pipe", new Vector3(-1.05f, 2.5f, -6f), new Vector3(-1.05f, 2.5f, 0f), .07f, mGalv);
        Cyl("pipe", new Vector3(-1.05f, 2.5f, 0f), new Vector3(-1.05f, 2.5f - drop, L), .07f, mGalv);
        Cyl("pipe", new Vector3(-1.05f, 2.5f - drop, L), new Vector3(-1.05f, 2.5f - drop, L + 6f), .07f, mGalv);
        Cyl("sprinkler", new Vector3(-.75f, 2.62f, 0f), new Vector3(-.75f, 2.62f - drop, L), .05f, mSprink);
        Box("tray", new Vector3(.55f, 2.62f, -6f), new Vector3(1.25f, 2.7f, 0f), mGalv, 1f, 1u, "");
        for (var z = 1.5f; z < L; z += 3f) WrapLamp(0f, LsCH - drop * (z / L) - .02f, z);
        WrapLamp(0f, LsCH - .02f, -3f);
        WrapLamp(0f, yb + LsCH - .02f, L + 3f);
        for (var z = 3f; z < L; z += 6f) Battery((z % 12f < 6f) ? -wx + .02f : wx - .02f, 2.2f - drop * (z / L), z, (z % 12f < 6f) ? 90f : -90f);
        Mains(true);
        yield return 20;
        LsAim(0f, 1.62f, -4.5f, 0f, -2.2f, 26f, -1f);
        Shot("h1_ramp_down_armed", "Run!: the service spine as a 1:8 ramp 24 m long, 3 m down to the goal door; mains on");
        Mains(false);
        yield return 6;
        LsAim(0f, 1.62f, -4.5f, 0f, -2.2f, 26f, -1f);
        Shot("h2_ramp_down_tripped", "Run!: the same ramp after the trip: battery heads only, the goal door is the brightest thing");
        LsEnd();
        yield return 2;
    }

    // ================================================================================ I - Run! B corridor: the section is the threat
    static IEnumerator LsSceneI()
    {
        LsBegin("I run spine", 1600f);
        var layout = RunRigLayout.Corridor1x9();
        layout.wrapLensEmission = new Color(.90f, .95f, 1f) * 1.1f;
        var dress = FrontRoomsRunDress.Build(lsRoot, layout);
        rig = FrontRoomsRunRig.Create(lsRoot, layout, RunRigHost.OwnLamps, null, dress);
        var viewer = new GameObject("LS run viewer").transform;
        viewer.SetParent(lsRoot, false);
        viewer.localPosition = new Vector3(0f, 1.6f, 6f);
        rig.SetViewer(viewer);
        rig.ResetImmediate();
        for (var i = 0; i < 90; i++) { rig.Tick(1f / 60f); yield return 1; }
        preRender = LsRunPre;
        LsAim(0f, 1.62f, .7f, 0f, 1.57f, 10.7f, -1f);
        Shot("i1_run_b_today_armed", "Run! B today (Emergency Power corridor, armed): Standard section, services at the soffit");
        Box("duct", new Vector3(.45f, 2.47f, 4.5f), new Vector3(1.15f, 2.86f, 25f), mGalv, 1f, 1u, "");
        Cyl("pipe", new Vector3(-1.02f, 2.52f, 1.5f), new Vector3(-1.02f, 2.52f, 25.5f), .075f, mGalv);
        Cyl("pipe", new Vector3(-.74f, 2.6f, 1.5f), new Vector3(-.74f, 2.6f, 25.5f), .05f, mSprink);
        for (var zi = 0; zi < 2; zi++)
        {
            var z = 9f + zi * 9f;
            WallZ(z - .1f, -1.5f, -1f, 0f, LsCH, -1, mBlockUp); WallZ(z - .1f, 1f, 1.5f, 0f, LsCH, -1, mBlockUp); WallZ(z - .1f, -1f, 1f, 2.1f, LsCH, -1, mBlockUp);
            WallZ(z + .1f, -1.5f, -1f, 0f, LsCH, 1, mBlockUp); WallZ(z + .1f, 1f, 1.5f, 0f, LsCH, 1, mBlockUp); WallZ(z + .1f, -1f, 1f, 2.1f, LsCH, 1, mBlockUp);
            Box("fire frame L", new Vector3(-1.06f, 0f, z - .1f), new Vector3(-1f, 2.12f, z + .1f), mDoorFrame, 1f, 1u, "-y");
            Box("fire frame R", new Vector3(1f, 0f, z - .1f), new Vector3(1.06f, 2.12f, z + .1f), mDoorFrame, 1f, 1u, "-y");
            Box("fire frame H", new Vector3(-1.06f, 2.1f, z - .1f), new Vector3(1.06f, 2.16f, z + .1f), mDoorFrame, 1f, 1u, "");
        }
        for (var i = 0; i < 30; i++) { rig.Tick(1f / 60f); yield return 1; }
        LsAim(0f, 1.62f, .7f, 0f, 1.57f, 10.7f, -1f);
        Shot("i2_spine_section_armed", "Run! spine proposal, armed: services hang to 2.45 m clear, fire-door walls every 9 m");
        rig.TripImmediate();
        for (var i = 0; i < 90; i++) { rig.Tick(1f / 60f); yield return 1; }
        LsAim(0f, 1.62f, .7f, 0f, 1.57f, 10.7f, -1f);
        Shot("i3_spine_section_tripped", "Run! spine proposal, tripped: battery pools through the compartment openings");
        LsEnd();
        yield return 2;
    }

    static void LsRunPre() { CopyLens(); cam.farClipPlane = 160f; }
}
