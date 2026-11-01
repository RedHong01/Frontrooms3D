// LEVEL SPACE SHOTS, round 2: well variants, split levels, the stair hall, a moving floor, Run! verticality that stays a corridor.
// CLONE ONLY (W/proj_levelspace); never lands in main. ASCII only, no C# tuples, no GetInstanceID.
// Run with -zlZone levelspace -zlOption r2 (or letters J..N). Output: W/level_space/renders/levelspace/<option>/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public static partial class FrontRoomsZoneLampsShots
{
    static bool LsWant2(string k) { return Opt == "r2" || Opt.Contains(k); }

    // A straight flight from its foot (x0, y0) along x by run (signed), rising by rise, between z0 and z1.
    static void FlightX(float x0, float y0, float run, float rise, float z0, float z1, int n, Material m)
    {
        float t = run / n, r = rise / n;
        for (var i = 0; i < n; i++)
        {
            float xa = x0 + i * t, xb = x0 + (i + 1) * t, top = y0 + (i + 1) * r;
            Box("step", new Vector3(Mathf.Min(xa, xb), top - .35f, z0), new Vector3(Mathf.Max(xa, xb), top, z1), m, 1f, 1u, "");
        }
        Cyl("handrail", new Vector3(x0, y0 + r + .9f, z0 + .05f), new Vector3(x0 + run, y0 + rise + .9f, z0 + .05f), .025f, mMetal);
        Cyl("handrail", new Vector3(x0, y0 + r + .9f, z1 - .05f), new Vector3(x0 + run, y0 + rise + .9f, z1 - .05f), .025f, mMetal);
        for (var k = 0; k <= 4; k++)
        {
            float f = k / 4f, px = x0 + run * f, py = y0 + rise * f + r;
            Cyl("baluster", new Vector3(px, py, z0 + .05f), new Vector3(px, py + .9f, z0 + .05f), .015f, mMetal);
            Cyl("baluster", new Vector3(px, py, z1 - .05f), new Vector3(px, py + .9f, z1 - .05f), .015f, mMetal);
        }
    }

    // The same along z.
    static void FlightZ(float z0, float y0, float run, float rise, float x0, float x1, int n, Material m)
    {
        float t = run / n, r = rise / n;
        for (var i = 0; i < n; i++)
        {
            float za = z0 + i * t, zb = z0 + (i + 1) * t, top = y0 + (i + 1) * r;
            Box("step", new Vector3(x0, top - .35f, Mathf.Min(za, zb)), new Vector3(x1, top, Mathf.Max(za, zb)), m, 1f, 1u, "");
        }
        Cyl("handrail", new Vector3(x0 + .05f, y0 + r + .9f, z0), new Vector3(x0 + .05f, y0 + rise + .9f, z0 + run), .025f, mMetal);
        Cyl("handrail", new Vector3(x1 - .05f, y0 + r + .9f, z0), new Vector3(x1 - .05f, y0 + rise + .9f, z0 + run), .025f, mMetal);
    }

    static void LsPoolBottom(float yb, float hx, float bx)
    {
        Room(-bx, -bx, bx, bx, yb, 5.4f, mTile, mTile, mTile, "c");
        CeilHoles(Rect.MinMaxRect(-bx, -bx, bx, bx), new List<Rect> { Rect.MinMaxRect(-hx, -hx, hx, hx) }, yb + 5.4f, mTile);
        var w = Fq("water", new Vector3(-bx + .6f, yb + .06f, -bx + .6f), new Vector3(2f * bx - 1.2f, 0f, 0f), new Vector3(0f, 0f, 2f * bx - 1.2f), lsWater, 1f);
        w.shadowCastingMode = ShadowCastingMode.Off;
        AddSpot("pool glow up", new Vector3(0f, yb + .4f, 0f), Vector3.up, 120f, 80f, 160f, 60f, new Color(.35f, .95f, .92f), false, 1u);
    }

    static void FireWall(float z, float yf)
    {
        WallZ(z - .1f, -1.5f, -1f, yf, yf + LsCH, -1, mBlockUp); WallZ(z - .1f, 1f, 1.5f, yf, yf + LsCH, -1, mBlockUp); WallZ(z - .1f, -1f, 1f, yf + 2.1f, yf + LsCH, -1, mBlockUp);
        WallZ(z + .1f, -1.5f, -1f, yf, yf + LsCH, 1, mBlockUp); WallZ(z + .1f, 1f, 1.5f, yf, yf + LsCH, 1, mBlockUp); WallZ(z + .1f, -1f, 1f, yf + 2.1f, yf + LsCH, 1, mBlockUp);
        Box("fire frame L", new Vector3(-1.06f, yf, z - .1f), new Vector3(-1f, yf + 2.12f, z + .1f), mDoorFrame, 1f, 1u, "-y");
        Box("fire frame R", new Vector3(1f, yf, z - .1f), new Vector3(1.06f, yf + 2.12f, z + .1f), mDoorFrame, 1f, 1u, "-y");
        Box("fire frame H", new Vector3(-1.06f, yf + 2.1f, z - .1f), new Vector3(1.06f, yf + 2.16f, z + .1f), mDoorFrame, 1f, 1u, "");
    }

    // ================================================================================ J - the well, three variants
    static IEnumerator LsSceneJ()
    {
        LsBegin("J1 shaft", 1800f);
        for (var k = 0; k <= 10; k++) Ring(-LsFF * k, 3f, 3f, k == 0 ? 6f : 3f, k % 3 == 1, k % 3 == 1, k == 0 ? 2 : 0);
        LsPoolBottom(-LsFF * 11f - 2f, 3f, 6f);
        yield return 20;
        LsAim(0f, 1.62f, -2.8f, .05f, -46f, .2f, 74f);
        Shot("j1_shaft_down", "The well as a shaft: 6 x 6 m, ten floors, 46 m down to the pool");
        LsEnd();
        yield return 2;

        LsBegin("J2 funnel", 2000f);
        for (var k = 0; k <= 6; k++)
        {
            var h = 7.5f - .75f * k;
            Ring(-LsFF * k, h, h, 12f - h, k % 2 == 1, k % 2 == 1, k == 0 ? 2 : 0, true, k == 0 ? 0f : 7.5f - .75f * (k - 1));
        }
        LsPoolBottom(-LsFF * 7f - 2f, 3f, 7f);
        yield return 20;
        LsAim(0f, 1.62f, -7.1f, 0f, -26f, -.3f, 76f);
        Shot("j2_funnel_down", "The well as a funnel: 15 x 15 m at Level 0, each floor 0.75 m further in, 6 x 6 m over the pool");
        LsAim(-7f, 1.62f, -10.5f, 3f, -11f, 3f, -1f);
        Shot("j2_funnel_across", "The funnel from the Level 0 floor: every floor below is a terrace in view");
        LsEnd();
        yield return 2;

        LsBegin("J3 bridge", 2200f);
        Ring(0f, 4.5f, 4.5f, 7.5f, false, false, 2, false);
        Rail(-4.5f, -4.5f, -.9f, -4.5f, 0f); Rail(.9f, -4.5f, 4.5f, -4.5f, 0f); Rail(-4.5f, 4.5f, -.9f, 4.5f, 0f); Rail(.9f, 4.5f, 4.5f, 4.5f, 0f);
        Rail(-4.5f, -4.5f, -4.5f, 4.5f, 0f); Rail(4.5f, -4.5f, 4.5f, 4.5f, 0f);
        FloorQ(-.9f, -4.5f, .9f, 4.5f, 0f, mCarpet);
        CeilQ(-.9f, -4.5f, .9f, 4.5f, -.3f, mSlab);
        WallX(-.9f, -4.5f, 4.5f, -.3f, 0f, -1, mFascia); WallX(.9f, -4.5f, 4.5f, -.3f, 0f, 1, mFascia);
        GlassGuard(-.9f, -4.5f, -.9f, 4.5f, 0f); GlassGuard(.9f, -4.5f, .9f, 4.5f, 0f);
        for (var k = 1; k <= 5; k++) Ring(-LsFF * k, 4.5f, 4.5f, 3f, k % 2 == 0, k % 2 == 0, 0);
        FlightX(-4.5f, -12f, 9f, 4f, -.6f, .6f, 23, SurfMat("Stair_Slab"));
        LsPoolBottom(-LsFF * 6f - 2f, 4.5f, 9f);
        yield return 20;
        LsAim(-4.3f, 1.62f, -4.8f, .6f, -11f, .6f, 74f);
        Shot("j3_bridge_down", "The well with a bridge: the Level 0 route crosses the void; two floors down a stair flies across it");
        LsAim(-3.8f, 1.62f, -6.2f, .3f, .2f, 4.5f, -1f);
        Shot("j3_bridge_along", "The bridge from the Level 0 floor: 1.8 m wide, glass guards, the void open on both sides");
        LsEnd();
        yield return 2;
    }

    // ================================================================================ K - split levels in one office hall
    static void Plat(float x0, float z0, float x1, float z1, float top)
    {
        Box("platform", new Vector3(x0, -1.45f, z0), new Vector3(x1, top, z1), mOWall, 1.2f, 1u, "-y");
        FloorQ(x0, z0, x1, z1, top + .004f, mOCarpet);
    }

    static IEnumerator LsSceneK()
    {
        LsBegin("K split levels", 2400f);
        Room(-12f, -12f, 12f, 12f, -1.45f, 8.65f, mOWall, mOCarpet, mOCeil, "");
        Plat(-12f, -12f, 0f, 0f, 0f);
        Plat(-12f, 0f, 0f, 12f, 1.45f);
        Plat(0f, 0f, 12f, 12f, 2.9f);
        GlassGuard(0f, -12f, 0f, 0f, 0f);
        GlassGuard(-12f, 0f, -6.2f, 0f, 1.45f); GlassGuard(-4.6f, 0f, 0f, 0f, 1.45f);
        GlassGuard(0f, 0f, 0f, 5.8f, 2.9f); GlassGuard(0f, 7.4f, 0f, 12f, 2.9f);
        GlassGuard(0f, 0f, 12f, 0f, 2.9f);
        var st = SurfMat("Office_Carpet");
        FlightZ(-2.6f, 0f, 2.6f, 1.45f, -6.0f, -4.8f, 8, st);
        FlightX(-2.6f, 1.45f, 2.6f, 1.45f, 6f, 7.2f, 8, st);
        FlightX(2.6f, -1.45f, -2.6f, 1.45f, -6f, -4.8f, 8, st);
        Cluster(-10.5f, -10.5f, 0f); Cluster(-6f, -10.5f, 0f);
        Cluster(-10.5f, 3.5f, 1.45f); Cluster(-6.2f, 8f, 1.45f);
        Cluster(3f, 4f, 2.9f); Cluster(7.5f, 7.5f, 2.9f);
        Cluster(3.5f, -10.5f, -1.45f); Cluster(8f, -6f, -1.45f);
        Lamps(-12f, -12f, 12f, 12f, 7.2f, mLensCool, LampOf, 16f, 14f, 4, null);
        yield return 20;
        LsAim(-2.6f, 1.62f, -8.8f, 6f, 2.4f, 6f, -1f);
        Shot("k1_split_levels", "Office split levels: four platforms half a storey apart (-1.45, 0, +1.45, +2.9 m) under one 7.2 m ceiling, joined by short flights");
        LsAim(11.3f, 2.9f + 1.62f, 2f, -6f, -1f, -6f, -1f);
        Shot("k2_split_from_top", "From the top platform you look down over every other level");
        LsEnd();
        yield return 2;
    }

    // ================================================================================ L - the stair hall (deep floors)
    static IEnumerator LsSceneL()
    {
        LsBegin("L stair hall", 2600f);
        const float hx = 7.5f, hz = 6f;
        var sm = SurfMat("Stair_Slab");
        for (var k = -1; k <= 1; k++) Ring(k * LsFF, hx, hz, 3f, false, false, k == 0 ? 2 : 0);
        FloorQ(-hx, -hz, hx, hz, -8f, mCarpet);
        VoidBand(0f, 0f, hx, hz, -8f, -LsFF - LsPlen, mWall);
        VoidBand(0f, 0f, hx, hz, LsFF + LsCH, 8.2f, mFascia);
        CeilQ(-hx, -hz, hx, hz, 8.2f, mCeil);
        Lamps(-hx, -hz, hx, hz, 8.2f, mLens, LampL0, 26f, 18f, 4, null);
        FlightX(-7.5f, -4f, 7f, 4f, -3.6f, -2.4f, 23, sm);
        Box("landing", new Vector3(-.5f, -.3f, -3.6f), new Vector3(1.9f, 0f, -2.4f), sm, 1f, 1u, "");
        Box("bridge", new Vector3(.7f, -.3f, -2.4f), new Vector3(1.9f, 0f, 6f), sm, 1f, 1u, "");
        Rail(.7f, -2.4f, .7f, 6f, 0f); Rail(1.9f, 1.2f, 1.9f, 6f, 0f); Rail(1.9f, -2.4f, 1.9f, 1.2f, 0f);
        FlightX(1.9f, 0f, 5.6f, 4f, 2.4f, 3.6f, 23, sm);
        FlightZ(5.5f, -8f, -7f, 4f, -6.9f, -5.7f, 23, sm);
        Box("landing west", new Vector3(-7.5f, -4.3f, -2.7f), new Vector3(-5.7f, -4f, -1.5f), sm, 1f, 1u, "");
        Box("high bridge", new Vector3(-7.5f, 3.7f, -1.6f), new Vector3(7.5f, 4f, -.4f), sm, 1f, 1u, "");
        Rail(-7.5f, -1.6f, 7.5f, -1.6f, 4f); Rail(-7.5f, -.4f, 7.5f, -.4f, 4f);
        yield return 20;
        LsAim(-3f, 1.62f, 7.4f, 1f, -3f, -2f, 72f);
        Shot("l1_stair_hall", "Deep floors: a stair hall three storeys tall; flights rise to landings in mid-air, bridges tie them back to the galleries");
        LsAim(3f, -8f + 1.62f, 4f, -1f, 4f, -2f, 76f);
        Shot("l2_stair_hall_up", "The stair hall from its floor: flights and bridges crossing overhead at three heights");
        LsEnd();
        yield return 2;
    }

    // ================================================================================ M - a floor that moves between visits
    static IEnumerator LsSceneM()
    {
        LsBegin("M moving floor", 2800f);
        float x0 = -9f, x1 = 9f, z0 = -9f, z1 = 9f;
        var hole = Rect.MinMaxRect(-3f, -3f, 3f, 3f);
        FloorQ(x0, z0, x1, hole.yMin, 0f, mCarpet); FloorQ(x0, hole.yMax, x1, z1, 0f, mCarpet);
        FloorQ(x0, hole.yMin, hole.xMin, hole.yMax, 0f, mCarpet); FloorQ(hole.xMax, hole.yMin, x1, hole.yMax, 0f, mCarpet);
        Room(x0, z0, x1, z1, 0f, LsCH, mWall, mCarpet, mCeil, "f");
        Lamps(x0, z0, x1, z1, LsCH, mLens, LampL0, 5f, 8f, 3, null);
        Room(x0, z0, x1, z1, -LsFF, LsCH, mWall, mCarpet, mCeil, "c");
        CeilHoles(Rect.MinMaxRect(x0, z0, x1, z1), new List<Rect> { hole }, -LsFF + LsCH, mCeil);
        Lamps(x0, z0, x1, z1, -LsFF + LsCH, mLens, LampL0, 7f, 8.5f, 3, new[] { Grow(hole, .7f) });
        var keep = standRoot;
        var panel = new GameObject("floor panel").transform;
        panel.SetParent(lsRoot, false);
        standRoot = panel;
        Box("panel slab", new Vector3(hole.xMin, -.25f, hole.yMin), new Vector3(hole.xMax, 0f, hole.yMax), mSlab, 1f, 1u, "-y");
        FloorQ(hole.xMin, hole.yMin, hole.xMax, hole.yMax, .004f, mCarpet);
        FrontRoomsKitLibrary.Spawn("Kit_OfficeDesk", panel, new Vector3(.4f, 0f, .6f), 200f, null, false);
        FrontRoomsKitLibrary.Spawn("Kit_TaskChair", panel, new Vector3(.1f, 0f, -.4f), 20f, null, false);
        FrontRoomsKitLibrary.Spawn("Kit_FilingCabinet", panel, new Vector3(-2.4f, 0f, 2.3f), 0f, null, false);
        var cut = new GameObject("cut").transform;
        cut.SetParent(lsRoot, false);
        standRoot = cut;
        VoidBand(0f, 0f, 3f, 3f, -.03f, 0f, mCarpet);
        VoidBand(0f, 0f, 3f, 3f, -.23f, -.03f, mSlab);
        VoidBand(0f, 0f, 3f, 3f, -LsPlen, -.23f, mPlenum);
        var red = SurfMat("Prop_PlasticRed");
        Box("barricade S", new Vector3(-3.6f, .9f, -3.65f), new Vector3(3.6f, 1.05f, -3.55f), red, 1f, 1u, "");
        Box("barricade N", new Vector3(-3.6f, .9f, 3.55f), new Vector3(3.6f, 1.05f, 3.65f), red, 1f, 1u, "");
        Box("barricade W", new Vector3(-3.65f, .9f, -3.6f), new Vector3(-3.55f, 1.05f, 3.6f), red, 1f, 1u, "");
        Box("barricade E", new Vector3(3.55f, .9f, -3.6f), new Vector3(3.65f, 1.05f, 3.6f), red, 1f, 1u, "");
        for (var i = 0; i < 4; i++)
        {
            var sx = i % 2 == 0 ? -3.6f : 3.6f;
            var sz = i < 2 ? -3.6f : 3.6f;
            Box("barricade leg", new Vector3(sx - .04f, 0f, sz - .04f), new Vector3(sx + .04f, .95f, sz + .04f), red, 1f, 1u, "-y");
        }
        standRoot = keep;
        cut.gameObject.SetActive(false);
        yield return 20;
        LsAim(-4.4f, 1.62f, -4.6f, .4f, -3.6f, .9f, -1f);
        Shot("m1_visit_one", "Moving structure, first visit: an ordinary Level 0 suite");
        panel.localPosition = new Vector3(0f, -LsFF + .25f, 0f);
        cut.gameObject.SetActive(true);
        yield return 6;
        LsAim(-4.4f, 1.62f, -4.6f, .4f, -3.6f, .9f, -1f);
        Shot("m2_visit_two", "Moving structure, second visit: the 6 x 6 m floor panel has gone down a storey, desk and all; the cut is barricaded");
        LsEnd();
        yield return 2;
    }

    // ================================================================================ N - Run! verticality that stays a corridor
    static IEnumerator LsSceneN()
    {
        LsBegin("N1 split spine", 3000f);
        const float wx = 1.5f, d = 1.53f;
        var rise = d / 9f;
        var foot = 9f + 8f * .28f;
        Room(-wx, -6f, wx, 9f, 0f, LsCH, mBlockUp, mRunFloor, mSoffit, "N");
        WallX(-wx + .005f, -6f, 9f, 0f, 1.2f, 1, mBlockLo); WallX(wx - .005f, -6f, 9f, 0f, 1.2f, -1, mBlockLo);
        for (var i = 0; i < 8; i++)
            Box("step", new Vector3(-wx, -d, 9f + i * .28f), new Vector3(wx, -(i + 1) * rise, 9f + (i + 1) * .28f), mRunFloor, 1.5f, 1u, "-y-z-x+x");
        WallX(-wx, 9f, foot, -d, LsCH, 1, mBlockUp); WallX(wx, 9f, foot, -d, LsCH, -1, mBlockUp);
        CeilQ(-wx, 9f, wx, foot, LsCH, mSoffit);
        WallZ(foot, -wx, wx, -d + LsCH, LsCH, -1, mSoffit);
        Room(-wx, foot, wx, 27f, -d, LsCH, mBlockUp, mRunFloor, mSoffit, "S");
        WallX(-wx + .005f, foot, 27f, -d, -d + 1.2f, 1, mBlockLo); WallX(wx - .005f, foot, 27f, -d, -d + 1.2f, -1, mBlockLo);
        FireWall(9f, 0f);
        FireWall(19.5f, -d);
        Cyl("pipe", new Vector3(-1.02f, 2.5f, -6f), new Vector3(-1.02f, 2.5f, foot), .07f, mGalv);
        Cyl("pipe", new Vector3(-1.02f, 2.5f, foot), new Vector3(-1.02f, -d + 2.5f, foot + .4f), .07f, mGalv);
        Cyl("pipe", new Vector3(-1.02f, -d + 2.5f, foot + .4f), new Vector3(-1.02f, -d + 2.5f, 27f), .07f, mGalv);
        Cyl("sprinkler", new Vector3(-.74f, 2.62f, -6f), new Vector3(-.74f, 2.62f, 27f), .05f, mSprink);
        WrapLamp(0f, LsCH - .02f, -3f); WrapLamp(0f, LsCH - .02f, 1.5f); WrapLamp(0f, LsCH - .02f, 6f); WrapLamp(0f, LsCH - .02f, 10.2f);
        for (var z = foot + 2f; z < 27f; z += 3.5f) WrapLamp(0f, -d + LsCH - .02f, z);
        Battery(-wx + .02f, 2.2f, 3f, 90f); Battery(wx - .02f, 2.2f, 8.4f, -90f); Battery(-wx + .02f, -d + 2.2f, 15f, 90f); Battery(wx - .02f, -d + 2.2f, 23f, -90f);
        Mains(true);
        yield return 20;
        LsAim(0f, 1.62f, 4.2f, 0f, -1.1f, 17f, -1f);
        Shot("n1_split_spine_armed", "Run! split-level spine: half a storey down a full-width flight at a fire door; the soffit drops with it, so the corridor stays a corridor");
        Mains(false);
        yield return 6;
        LsAim(0f, 1.62f, 4.2f, 0f, -1.1f, 17f, -1f);
        Shot("n1_split_spine_tripped", "The same spine after the trip");
        LsEnd();
        yield return 2;

        LsBegin("N2 bridge corridor", 3200f);
        const float vx = 10f, vz = 4.5f;
        for (var k = 1; k <= 4; k++) Ring(-LsFF * k, vx, vz, 3f, k % 2 == 0, false, 0);
        FloorQ(-vx, -vz, vx, vz, -LsFF * 5f, mOCarpet);
        Lamps(-vx, -vz, vx, vz, -LsFF * 4f - LsPlen - .01f, mLensCool, LampOf, 8f, 10f, 0, null);
        FloorQ(-wx, -15f, wx, 15f, 0f, mRunFloor);
        CeilQ(-wx, -15f, wx, 15f, LsCH, mSoffit);
        CeilQ(-wx, -vz, wx, vz, -.3f, mSlab);
        WallX(-wx, -vz, vz, -.3f, 0f, -1, mFascia); WallX(wx, -vz, vz, -.3f, 0f, 1, mFascia);
        var wired = SurfMat("Glass_Wired");
        for (var s = -1; s <= 1; s += 2)
        {
            var x = s * wx;
            var face = s < 0 ? 1 : -1;
            WallX(x, -15f, -vz, 0f, LsCH, face, mBlockUp); WallX(x, vz, 15f, 0f, LsCH, face, mBlockUp);
            WallX(x, -vz, vz, 0f, .9f, face, mBlockUp); WallX(x, -vz, vz, 2.2f, LsCH, face, mBlockUp);
            WallX(x - s * .005f, -15f, -vz, 0f, 1.2f, face, mBlockLo); WallX(x - s * .005f, vz, 15f, 0f, 1.2f, face, mBlockLo);
            WallX(x - s * .005f, -vz, vz, 0f, .9f, face, mBlockLo);
            WallX(x, -vz, vz, .9f, 2.2f, 1, wired); WallX(x, -vz, vz, .9f, 2.2f, -1, wired);
            for (var z = -vz; z <= vz + .01f; z += 1.5f) Box("mullion", new Vector3(x - .04f, .9f, z - .03f), new Vector3(x + .04f, 2.2f, z + .03f), mMetal, 1f, 1u, "");
            Box("sill", new Vector3(x - .06f, .86f, -vz), new Vector3(x + .06f, .9f, vz), mMetal, 1f, 1u, "");
            Box("head", new Vector3(x - .06f, 2.2f, -vz), new Vector3(x + .06f, 2.24f, vz), mMetal, 1f, 1u, "");
        }
        WallZ(-15f, -wx, wx, 0f, LsCH, 1, mBlockUp);
        WallZ(15f, -wx, -.5f, 0f, LsCH, -1, mBlockUp); WallZ(15f, .5f, wx, 0f, LsCH, -1, mBlockUp); WallZ(15f, -.5f, .5f, 2.1f, LsCH, -1, mBlockUp);
        WallZ(14.99f, -.5f, .5f, 0f, 2.1f, -1, mPlenum);
        for (var z = -13.5f; z < 15f; z += 3f) WrapLamp(0f, LsCH - .02f, z);
        Battery(-wx + .02f, 2.2f, -9f, 90f); Battery(wx - .02f, 2.2f, 9f, -90f);
        Mains(true);
        yield return 20;
        LsAim(0f, 1.62f, -7.2f, 0f, 1.2f, 6f, -1f);
        Shot("n2_bridge_corridor", "Run! bridge corridor: the spine crosses a four-storey light well; wired-glass windows on both sides, still a corridor");
        LsAim(1.15f, 1.62f, .3f, 7f, -14f, 1.2f, 76f);
        Shot("n2_window_down", "At the window: the well drops four storeys below the corridor");
        LsEnd();
        yield return 2;
    }
}
