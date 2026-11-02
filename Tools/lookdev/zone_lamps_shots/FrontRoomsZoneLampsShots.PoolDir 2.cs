// Clone-only (line bathroom = the pool level, K session, 2026-10-31). Direction stills for Red, per
// Frontrooms3D/Documentation/research/bathroom_rooms/40_direction_images.md and 15_poolrooms.md: the club rooms in
// front of game visual's pool STAND-IN ROOM and the pool-level rooms behind its far door. Greybox only: no shared
// rendering code is touched, signs are blank panels, nothing is taken from any creator's images.
// Every room has its own rendering layer, so lights never bleed through the zero-thickness walls.
// Run: -executeMethod FrontRoomsZoneLampsShots.RunBatch -zlZone pooldir -zlOption all -zlPhone 0 -frGlassRT off
using System.Collections;
using System.Collections.Generic;
using FrontRooms.Map;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static partial class FrontRoomsZoneLampsShots
{
    // read by FloorY (edited in this clone); the stand-in default is a swimming pool, D6 makes it a wading pool
    static float poolShallowY = -1.1f, poolDeepY = -2.4f;

    const float T3 = .9144f, LvlFloor = -.15f, LvlWater = -.03f, ClubCeil = 2.9f, LvlCeil = 2.85f, BasinCeil = 4.05f, PillarCeil = 2.25f;
    const uint LHall = 1u, LEntry = 2u, LLock = 4u, LOdd = 8u, LEven = 16u, LBasin = 32u, LPillar = 64u, LStair = 2u;
    static readonly Color CoolWhite = new Color(1f, .96f, .90f);
    static readonly Color Warm = new Color(1f, .86f, .70f);
    // the pool level reads as bright white tile in the canon (15 section 2); the stand-in's ambient is the map's warm one
    const float LvlBoost = 1.7f;
    static readonly Color LevelWhite = new Color(.94f, .98f, 1f);

    sealed class DirDoor
    {
        public char side; public float a, b, h; public bool arch;
        public DirDoor(char s, float a0, float b0, float h0, bool ar) { side = s; a = a0; b = b0; h = h0; arch = ar; }
    }

    static readonly Dictionary<string, Transform> dirGroups = new Dictionary<string, Transform>();
    static readonly List<ReflectionProbe> dirProbes = new List<ReflectionProbe>();
    static readonly List<Renderer> basinNE = new List<Renderer>();
    static readonly List<Light> basinNELights = new List<Light>();
    static readonly List<Material> stairShades = new List<Material>();
    static Material mHallWall, mHallBand, mHallDeck, mClubTile, mClubBand, mClubFloor, mLvlWall, mLvlBand, mLvlFloor;
    static Material mPaint, mBlock, mSteel, mLocker, mWood, mSign, mDark, mChair, mLensOn, mLensOff, mCausticB, mCausticBC;
    // fusion pass (16_pool_palette_fusion.md): clone-only overrides of ambient, fog, grade, lamp colours, water and caustics
    static Material mClubPaint, mWater, mWaterline, mCausticLvl;
    static Volume dirVol;
    static WhiteBalance dirWB;
    static ColorAdjustments dirCA;
    static readonly Dictionary<Light, Color> lightBase = new Dictionary<Light, Color>();
    static readonly Dictionary<Light, float> lightBaseI = new Dictionary<Light, float>();

    // ------------------------------------------------------------------------------------------- helpers
    static Transform DG(string name)
    {
        Transform g;
        if (dirGroups.TryGetValue(name, out g)) return g;
        g = new GameObject("dir " + name).transform;
        g.SetParent(standRoot, false);
        dirGroups[name] = g;
        return g;
    }

    static void DShow(string name, bool on) { DG(name).gameObject.SetActive(on); }

    static Renderer DQ(string grp, string name, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Material m, float uv, uint lay)
    {
        var r = Quad(name, a, b, c, d, m, uv, lay);
        r.transform.SetParent(DG(grp), false);
        return r;
    }

    static void DBox(string grp, string name, Vector3 a, Vector3 b, Material m, float uv, uint lay, string skip)
    {
        if (!skip.Contains("-y")) DQ(grp, name, new Vector3(a.x, a.y, a.z), new Vector3(b.x, a.y, a.z), new Vector3(b.x, a.y, b.z), new Vector3(a.x, a.y, b.z), m, uv, lay);
        if (!skip.Contains("+y")) DQ(grp, name, new Vector3(a.x, b.y, a.z), new Vector3(a.x, b.y, b.z), new Vector3(b.x, b.y, b.z), new Vector3(b.x, b.y, a.z), m, uv, lay);
        if (!skip.Contains("-x")) DQ(grp, name, new Vector3(a.x, a.y, a.z), new Vector3(a.x, a.y, b.z), new Vector3(a.x, b.y, b.z), new Vector3(a.x, b.y, a.z), m, uv, lay);
        if (!skip.Contains("+x")) DQ(grp, name, new Vector3(b.x, a.y, a.z), new Vector3(b.x, b.y, a.z), new Vector3(b.x, b.y, b.z), new Vector3(b.x, a.y, b.z), m, uv, lay);
        if (!skip.Contains("-z")) DQ(grp, name, new Vector3(a.x, a.y, a.z), new Vector3(a.x, b.y, a.z), new Vector3(b.x, b.y, a.z), new Vector3(b.x, a.y, a.z), m, uv, lay);
        if (!skip.Contains("+z")) DQ(grp, name, new Vector3(a.x, a.y, b.z), new Vector3(b.x, a.y, b.z), new Vector3(b.x, b.y, b.z), new Vector3(a.x, b.y, b.z), m, uv, lay);
    }

    static void DCyl(string grp, string name, Vector3 a, Vector3 b, float radius, Material m, uint lay)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        go.name = name;
        go.transform.SetParent(DG(grp), false);
        go.transform.localPosition = (a + b) * .5f;
        go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, (b - a).normalized);
        go.transform.localScale = new Vector3(radius * 2f, (b - a).magnitude * .5f, radius * 2f);
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = m;
        r.renderingLayerMask = lay;
    }

    static Light DSpot(string grp, string name, Vector3 pos, Vector3 dir, float outer, float inner, float intensity, float range, Color col, bool shadows, uint lay)
    {
        var l = AddSpot(name, pos, dir, outer, inner, intensity, range, col, shadows, lay);
        l.transform.SetParent(DG(grp), false);
        return l;
    }

    static Light DPoint(string grp, string name, Vector3 pos, float intensity, float range, Color col, uint lay)
    {
        var go = new GameObject(name);
        go.transform.SetParent(DG(grp), false);
        go.transform.localPosition = pos;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.intensity = intensity;
        l.range = range;
        l.color = col;
        l.shadows = LightShadows.None;
        l.renderingLayerMask = (int)lay;
        return l;
    }

    // a wall face: S at z0 faces +z, N at z1 faces -z, W at x0 faces +x, E at x1 faces -x; s runs along the wall
    static Renderer DFace(string grp, string name, char side, float x0, float x1, float z0, float z1, float s0, float s1, float y0, float y1, Material m, float uv, uint lay)
    {
        return DSlant(grp, name, side, x0, x1, z0, z1, s0, s1, y0, y0, y1, m, uv, lay);
    }

    static Renderer DSlant(string grp, string name, char side, float x0, float x1, float z0, float z1, float sA, float sB, float yA, float yB, float top, Material m, float uv, uint lay)
    {
        if (side == 'S') return DQ(grp, name, new Vector3(sA, yA, z0), new Vector3(sB, yB, z0), new Vector3(sB, top, z0), new Vector3(sA, top, z0), m, uv, lay);
        if (side == 'N') return DQ(grp, name, new Vector3(sA, yA, z1), new Vector3(sA, top, z1), new Vector3(sB, top, z1), new Vector3(sB, yB, z1), m, uv, lay);
        if (side == 'W') return DQ(grp, name, new Vector3(x0, yA, sA), new Vector3(x0, top, sA), new Vector3(x0, top, sB), new Vector3(x0, yB, sB), m, uv, lay);
        return DQ(grp, name, new Vector3(x1, yA, sA), new Vector3(x1, yB, sB), new Vector3(x1, top, sB), new Vector3(x1, top, sA), m, uv, lay);
    }

    static void DPiece(string grp, string name, char side, float x0, float x1, float z0, float z1, float s0, float s1, float y0, float y1, float tileY, Material tile, Material upper, float uvUp, uint lay)
    {
        var t = Mathf.Clamp(tileY, y0, y1);
        if (t > y0 + .001f) DFace(grp, name + " tile", side, x0, x1, z0, z1, s0, s1, y0, t, tile, T3, lay);
        if (y1 > t + .001f) DFace(grp, name + " upper", side, x0, x1, z0, z1, s0, s1, t, y1, upper, uvUp, lay);
    }

    // one wall split at the tile line, with door openings (rectangular, or round-headed in full-tile walls)
    static void DWall(string grp, string name, char side, float x0, float x1, float z0, float z1, float y0, float y1, float tileY, Material tile, Material upper, float uvUp, List<DirDoor> doors, uint lay)
    {
        var horizontal = side == 'S' || side == 'N';
        float s0 = horizontal ? x0 : z0, s1 = horizontal ? x1 : z1;
        var cuts = new List<DirDoor>();
        foreach (var d in doors) if (d.side == side) cuts.Add(d);
        cuts.Sort((p, q) => p.a.CompareTo(q.a));
        var cur = s0;
        foreach (var d in cuts)
        {
            if (d.a > cur + .001f) DPiece(grp, name, side, x0, x1, z0, z1, cur, d.a, y0, y1, tileY, tile, upper, uvUp, lay);
            if (!d.arch) DPiece(grp, name + " lintel", side, x0, x1, z0, z1, d.a, d.b, y0 + d.h, y1, tileY, tile, upper, uvUp, lay);
            else
            {
                float r = (d.b - d.a) * .5f, cx = (d.a + d.b) * .5f, spring = y0 + d.h - r;
                const int n = 16;
                for (var i = 0; i < n; i++)
                {
                    float sa = d.a + (d.b - d.a) * i / n, sb = d.a + (d.b - d.a) * (i + 1) / n;
                    float ya = spring + Mathf.Sqrt(Mathf.Max(0f, r * r - (sa - cx) * (sa - cx)));
                    float yb = spring + Mathf.Sqrt(Mathf.Max(0f, r * r - (sb - cx) * (sb - cx)));
                    DSlant(grp, name + " arch", side, x0, x1, z0, z1, sa, sb, ya, yb, y1, tile, T3, lay);
                }
            }
            cur = d.b;
        }
        if (s1 > cur + .001f) DPiece(grp, name, side, x0, x1, z0, z1, cur, s1, y0, y1, tileY, tile, upper, uvUp, lay);
    }

    // a colour band 4 mm proud of the wall, broken where a door reaches it
    static void DBand(string grp, char side, float x0, float x1, float z0, float z1, float floorY, float b0, float b1, Material m, List<DirDoor> doors, uint lay)
    {
        const float e = .004f;
        var horizontal = side == 'S' || side == 'N';
        float s0 = horizontal ? x0 : z0, s1 = horizontal ? x1 : z1;
        var cuts = new List<DirDoor>();
        foreach (var d in doors) if (d.side == side && floorY + d.h > b0) cuts.Add(d);
        cuts.Sort((p, q) => p.a.CompareTo(q.a));
        var cur = s0;
        foreach (var d in cuts)
        {
            if (d.a > cur + .001f) DFace(grp, "band", side, x0 + e, x1 - e, z0 + e, z1 - e, cur, d.a, b0, b1, m, T3, lay);
            cur = d.b;
        }
        if (s1 > cur + .001f) DFace(grp, "band", side, x0 + e, x1 - e, z0 + e, z1 - e, cur, s1, b0, b1, m, T3, lay);
    }

    static void DRoom(string grp, string name, float x0, float x1, float z0, float z1, float fy, float cy, float tileH, Material tile, Material upper, float uvUp, Material floor, Material ceil, float uvCeil, List<DirDoor> doors, bool floorOn, string sides, uint lay)
    {
        foreach (var s in sides) DWall(grp, name + " " + s, s, x0, x1, z0, z1, fy, cy, fy + tileH, tile, upper, uvUp, doors, lay);
        if (floorOn) DQ(grp, name + " floor", new Vector3(x0, fy, z0), new Vector3(x0, fy, z1), new Vector3(x1, fy, z1), new Vector3(x1, fy, z0), floor, T3, lay);
        if (ceil != null) DQ(grp, name + " ceiling", new Vector3(x0, cy, z0), new Vector3(x1, cy, z0), new Vector3(x1, cy, z1), new Vector3(x0, cy, z1), ceil, uvCeil, lay);
    }

    static void DWater(string grp, float x0, float x1, float z0, float z1, float y, uint lay)
    {
        var go = new GameObject("water");
        go.transform.SetParent(DG(grp), false);
        var m = QuadMesh(new Vector3(x0, y, z0), new Vector3(x0, y, z1), new Vector3(x1, y, z1), new Vector3(x1, y, z0), 1f, false);
        float u = (x1 - x0) / 9f, v = (z1 - z0) / 18f;
        m.uv = new[] { new Vector2(0, 0), new Vector2(0, v), new Vector2(u, v), new Vector2(u, 0) };
        go.AddComponent<MeshFilter>().sharedMesh = m;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = ZLMat("ZL_Water");
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.renderingLayerMask = lay;
    }

    static void DTroffer(string grp, float x, float y, float z, uint lay)
    {
        DQ(grp, "troffer lens", new Vector3(x - .3f, y, z - .3f), new Vector3(x + .3f, y, z - .3f), new Vector3(x + .3f, y, z + .3f), new Vector3(x - .3f, y, z + .3f), mLensOn, 1f, lay);
        DSpot(grp, "troffer", new Vector3(x, y - .05f, z), Vector3.down, 150f, 90f, 2.2f, 5.5f, CoolWhite, false, lay);
    }

    // sealed 4 ft fluorescent, long axis along z (P3-B)
    static void DStrip(string grp, float x, float y, float z, uint lay)
    {
        DQ(grp, "strip lens", new Vector3(x - .08f, y, z - .61f), new Vector3(x + .08f, y, z - .61f), new Vector3(x + .08f, y, z + .61f), new Vector3(x - .08f, y, z + .61f), mLensOn, 1f, lay);
        DSpot(grp, "strip", new Vector3(x, y - .05f, z), Vector3.down, 150f, 100f, 2.0f, 5f, CoolWhite, false, lay);
    }

    // flush sealed lens in the ceiling (P6-A)
    static Renderer DLens(string grp, float x, float cy, float z, uint lay, List<Light> lightsOut)
    {
        var y = cy - .004f;
        var r = DQ(grp, "flush lens", new Vector3(x - .3f, y, z - .3f), new Vector3(x + .3f, y, z - .3f), new Vector3(x + .3f, y, z + .3f), new Vector3(x - .3f, y, z + .3f), mLensOn, 1f, lay);
        var l = DSpot(grp, "flush", new Vector3(x, cy - .06f, z), Vector3.down, 140f, 80f, 3.0f * LvlBoost, 7.5f, LevelWhite, false, lay);
        if (lightsOut != null) lightsOut.Add(l);
        return r;
    }

    // continuous wall band on one long wall of a 6 m wide room (P6-B): a valance with its lamps hidden on top
    static void DValance(string grp, float side, float z0, float z1, float cy, uint lay)
    {
        float xw = side * 3f, xi = side * 2.82f;
        DBox(grp, "valance", new Vector3(Mathf.Min(xw, xi), cy - .78f, z0 + .3f), new Vector3(Mathf.Max(xw, xi), cy - .66f, z1 - .3f), mPaint, 1f, lay, "");
        if (side < 0f) DFace(grp, "valance glow", 'W', xi + .003f, 0f, z0, z1, z0 + .3f, z1 - .3f, cy - .69f, cy - .66f, mLensOn, 1f, lay);
        else DFace(grp, "valance glow", 'E', 0f, xi - .003f, z0, z1, z0 + .3f, z1 - .3f, cy - .69f, cy - .66f, mLensOn, 1f, lay);
        var zc = (z0 + z1) * .5f;
        foreach (var k in new[] { -1.4f, 1.4f })
            DSpot(grp, "band up", new Vector3(side * 2.9f, cy - .6f, zc + k), new Vector3(-side * .35f, 1f, 0f), 150f, 100f, 1.5f * LvlBoost, 3.6f, LevelWhite, false, lay);
        DSpot(grp, "band bounce", new Vector3(side * 1.2f, cy - .05f, zc), Vector3.down, 170f, 130f, .8f * LvlBoost, 4.2f, LevelWhite, false, lay);
    }

    // a wet-niche lamp as game visual builds it (P2-A), its light on the basin layer only
    static void DNiche(string grp, Vector3 p, Vector3 n, uint lay)
    {
        var go = AddKit(ZL + "Prefabs/ZL_NICHE", p + n * .006f, Quaternion.FromToRotation(Vector3.down, n), Incand * 1.4f);
        go.transform.SetParent(DG(grp), false);
        go.transform.Find("Lens").GetComponent<Renderer>().renderingLayerMask = lay;
        var h = go.transform.Find("Housing");
        if (h != null) foreach (var r in h.GetComponentsInChildren<Renderer>()) r.renderingLayerMask = lay | BasinLayer;
        DSpot(grp, "underwater", p + n * .12f, (n + Vector3.down * .22f).normalized, 110f, 40f, 2.4f, 10f, Incand, false, BasinLayer);
    }

    // a tall guard chair, seat at 1.75 m, facing +x (facing = 1) or -x (facing = -1)
    static void DChair(string grp, float x, float y, float z, float facing, uint lay)
    {
        foreach (var dx in new[] { -.35f, .35f })
            foreach (var dz in new[] { -.35f, .35f })
                DCyl(grp, "chair leg", new Vector3(x + dx, y, z + dz), new Vector3(x + dx * .7f, y + 1.75f, z + dz * .7f), .025f, mSteel, lay);
        DBox(grp, "chair seat", new Vector3(x - .3f, y + 1.75f, z - .3f), new Vector3(x + .3f, y + 1.8f, z + .3f), mChair, 1f, lay, "");
        var bx = x - facing * .3f;
        DBox(grp, "chair back", new Vector3(bx - .03f, y + 1.8f, z - .3f), new Vector3(bx + .03f, y + 2.25f, z + .3f), mChair, 1f, lay, "");
        foreach (var ry in new[] { .45f, .9f, 1.35f })
            DBox(grp, "chair rung", new Vector3(x + facing * .22f - .02f, y + ry, z - .25f), new Vector3(x + facing * .22f + .02f, y + ry + .03f, z + .25f), mSteel, 1f, lay, "");
    }

    static void DMark(string grp, float x, float y, float z, uint lay)
    {
        DQ(grp, "depth mark blank", new Vector3(x - .15f, y, z - .1f), new Vector3(x - .15f, y, z + .1f), new Vector3(x + .15f, y, z + .1f), new Vector3(x + .15f, y, z - .1f), mDark, 1f, lay);
    }

    static void DProbe(float x0, float x1, float z0, float z1, float fy, float cy)
    {
        var go = new GameObject("dir probe");
        go.transform.SetParent(standRoot, false);
        var py = fy + 1.4f;
        go.transform.localPosition = new Vector3((x0 + x1) * .5f, py, (z0 + z1) * .5f);
        var p = go.AddComponent<ReflectionProbe>();
        p.mode = ReflectionProbeMode.Realtime;
        p.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
        p.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
        p.boxProjection = true;
        var bottom = fy - 1.5f;
        p.size = new Vector3(x1 - x0, cy - bottom, z1 - z0);
        p.center = new Vector3(0f, (cy + bottom) * .5f - py, 0f);
        p.resolution = 256;
        p.importance = 10;
        p.hdr = true;
        p.nearClipPlane = .05f;
        p.farClipPlane = 40f;
        dirProbes.Add(p);
    }

    static IEnumerator DirProbes()
    {
        foreach (var r in standRoot.GetComponentsInChildren<Renderer>(true))
            r.reflectionProbeUsage = r.name == "water" ? ReflectionProbeUsage.BlendProbes : ReflectionProbeUsage.Off;
        probe.RenderProbe();
        foreach (var p in dirProbes) p.RenderProbe();
        yield return Frames(8);
    }

    static IEnumerator DirSettle() { yield return Frames(4); yield return DirProbes(); }

    static Material DInst(Material src, string name) { var m = new Material(src); m.name = name; return m; }

    static Material DLit(Color c, float smooth)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", smooth);
        return m;
    }

    static Material DUnlit(Color c)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        m.SetColor("_BaseColor", c);
        return m;
    }

    static void DirMaterials()
    {
        mHallWall = DInst(ZLMat("ZL_TileWall"), "dir hall wall");
        mHallBand = DInst(ZLMat("ZL_TileWall"), "dir hall band");
        mHallDeck = DInst(ZLMat("ZL_TileDeck"), "dir hall deck");
        mClubTile = DInst(ZLMat("ZL_TileWall"), "dir club tile");
        mClubBand = DInst(ZLMat("ZL_TileWall"), "dir club band");
        mClubFloor = DInst(ZLMat("ZL_TileDeck"), "dir club floor");
        mLvlWall = DInst(ZLMat("ZL_TileWall"), "dir level wall");
        mLvlBand = DInst(ZLMat("ZL_TileWall"), "dir level band");
        mLvlFloor = DInst(ZLMat("ZL_TileBasin"), "dir level floor");
        mPaint = SurfMat("Office_Wall_Fresh");
        mClubPaint = DInst(mPaint, "dir club paint");
        mBlock = SurfMat("RunB_BlockUpper");
        mSteel = SurfMat("Prop_StainlessSatin");
        mLocker = DLit(new Color(.78f, .76f, .70f), .45f);
        mWood = DLit(new Color(.50f, .34f, .20f), .35f);
        mSign = DLit(new Color(.16f, .17f, .18f), .3f);
        mDark = DLit(new Color(.04f, .04f, .045f), .2f);
        mChair = DLit(new Color(.92f, .92f, .90f), .5f);
        mLensOn = DUnlit(new Color(4.2f, 4.1f, 3.9f));
        mLensOff = DUnlit(new Color(.035f, .035f, .035f));
        var deckAsset = ZLMat("ZL_TileDeck");
        foreach (var r in standRoot.GetComponentsInChildren<Renderer>(true)) if (r.sharedMaterial == deckAsset) r.sharedMaterial = mHallDeck;
        for (var i = 0; i < 8; i++)
        {
            var m = DInst(ZLMat("ZL_TileBasin"), "dir stair shade " + i);
            var k = Mathf.Lerp(.85f, .06f, i / 7f);
            m.SetColor("_BaseColor", new Color(k, k * 1.02f, k * 1.05f));
            stairShades.Add(m);
        }
    }

    // ------------------------------------------------------------------------------------------- the rooms
    static void DirHall()
    {
        var kill = new List<GameObject>();
        foreach (Transform c in standRoot) if (c.name.StartsWith("wall ")) kill.Add(c.gameObject);
        foreach (var g in kill) UnityEngine.Object.DestroyImmediate(g);
        var doors = new List<DirDoor> { new DirDoor('S', -6.2f, -5.0f, 2.1f, false), new DirDoor('N', -.7f, .7f, 2.1f, false) };
        foreach (var v in new[] { "24", "12", "full" })
        {
            var tileY = v == "24" ? 2.4f : v == "12" ? 1.2f : HallH;
            var b1 = v == "12" ? 1.05f : 2.25f;
            foreach (var s in "SNWE")
            {
                DWall("hall " + v, "hall wall", s, -HallX, HallX, 0f, HallZ, 0f, HallH, tileY, mHallWall, mBlock, 1f, doors, LHall);
                DBand("hall " + v, s, -HallX, HallX, 0f, HallZ, 0f, b1 - .15f, b1, mHallBand, doors, LHall);
            }
        }
        DChair("hall", -6.1f, 0f, 11.9f, 1f, LHall);
        foreach (var z in new[] { PoolZ0 + .6f, 9f, 15f, PoolZ1 - .6f })
        {
            DMark("hall", -PoolX - .55f, .002f, z, LHall);
            DMark("hall", PoolX + .55f, .002f, z, LHall);
        }
    }

    // entry and towel desk x -7.42..-1.0, z -15..-9; lockers and gang shower z -9..0, door into the hall at x -6.2..-5.0
    static void DirClub()
    {
        var eDoors = new List<DirDoor> { new DirDoor('S', -4.8f, -3.2f, 2.1f, false), new DirDoor('N', -4.6f, -3.4f, 2.1f, false) };
        DRoom("entry", "entry", -7.42f, -1.0f, -15f, -9f, 0f, ClubCeil, 0f, mClubTile, mClubPaint, 1f, mClubFloor, mPaint, 1f, eDoors, true, "SNWE", LEntry);
        DBox("entry", "desk", new Vector3(-6.6f, 0f, -11.9f), new Vector3(-3.6f, 1.02f, -11.3f), mWood, 1f, LEntry, "-y");
        DBox("entry", "desk top", new Vector3(-6.65f, 1.02f, -11.95f), new Vector3(-3.55f, 1.06f, -11.25f), mChair, 1f, LEntry, "");
        DBox("entry", "towels", new Vector3(-6.3f, 1.06f, -11.8f), new Vector3(-5.9f, 1.30f, -11.45f), mChair, 1f, LEntry, "-y");
        DBox("entry", "towels", new Vector3(-5.8f, 1.06f, -11.8f), new Vector3(-5.4f, 1.22f, -11.45f), mChair, 1f, LEntry, "-y");
        DBox("entry", "sign blank", new Vector3(-6.3f, 2.12f, -9.06f), new Vector3(-4.9f, 2.52f, -9.0f), mSign, 1f, LEntry, "");
        DBox("entry", "sign blank", new Vector3(-4.4f, 2.18f, -9.05f), new Vector3(-3.6f, 2.40f, -9.0f), mSign, 1f, LEntry, "");
        foreach (var x in new[] { -5.6f, -2.8f }) foreach (var z in new[] { -13.5f, -10.5f }) DTroffer("entry", x, ClubCeil - .005f, z, LEntry);

        var lDoors = new List<DirDoor> { new DirDoor('S', -4.6f, -3.4f, 2.1f, false), new DirDoor('N', -6.2f, -5.0f, 2.1f, false) };
        DRoom("lockers", "lockers", -7.42f, -1.0f, -9f, 0f, 0f, ClubCeil, 1.5f, mClubTile, mClubPaint, 1f, mClubFloor, mPaint, 1f, lDoors, true, "SNWE", LLock);
        foreach (var s in "SNWE") DBand("lockers", s, -7.42f, -1.0f, -9f, 0f, 0f, 1.25f, 1.4f, mClubBand, lDoors, LLock);
        DBox("lockers", "plinth", new Vector3(-7.42f, 0f, -8.6f), new Vector3(-7.0f, .1f, -3.65f), mDark, 1f, LLock, "-y-x");
        for (var i = 0; i < 13; i++)
        {
            var z = -8.6f + i * .38f;
            DBox("lockers", "locker", new Vector3(-7.42f, .1f, z), new Vector3(-7.04f, 1.93f, z + .37f), mLocker, 1f, LLock, "-x-y");
        }
        DBox("lockers", "locker row", new Vector3(-5.3f, .1f, -7.8f), new Vector3(-4.54f, 1.93f, -4.4f), mLocker, 1f, LLock, "-y");
        for (var z = -7.8f + .38f; z < -4.45f; z += .38f)
        {
            DQ("lockers", "locker seam", new Vector3(-4.536f, .12f, z), new Vector3(-4.536f, 1.91f, z), new Vector3(-4.536f, 1.91f, z + .012f), new Vector3(-4.536f, .12f, z + .012f), mDark, 1f, LLock);
            DQ("lockers", "locker seam", new Vector3(-5.304f, .12f, z), new Vector3(-5.304f, .12f, z + .012f), new Vector3(-5.304f, 1.91f, z + .012f), new Vector3(-5.304f, 1.91f, z), mDark, 1f, LLock);
        }
        DBox("lockers", "bench", new Vector3(-6.6f, .42f, -7.8f), new Vector3(-6.25f, .47f, -4.4f), mWood, 1f, LLock, "");
        foreach (var z in new[] { -7.6f, -4.6f }) DBox("lockers", "bench leg", new Vector3(-6.48f, 0f, z - .03f), new Vector3(-6.37f, .42f, z + .03f), mSteel, 1f, LLock, "-y");
        DBox("lockers", "shower half wall", new Vector3(-4.0f, 0f, -3.1f), new Vector3(-1.0f, 1.5f, -3.0f), mClubTile, T3, LLock, "-y+x");
        foreach (var z in new[] { -2.4f, -1.5f, -.6f })
        {
            DCyl("lockers", "shower pipe", new Vector3(-1.03f, 1.15f, z), new Vector3(-1.03f, 2.0f, z), .012f, mSteel, LLock);
            DCyl("lockers", "shower arm", new Vector3(-1.03f, 2.0f, z), new Vector3(-1.22f, 1.95f, z), .012f, mSteel, LLock);
            DCyl("lockers", "shower head", new Vector3(-1.22f, 1.95f, z), new Vector3(-1.27f, 1.90f, z), .04f, mSteel, LLock);
            DBox("lockers", "shower valve", new Vector3(-1.06f, 1.12f, z - .05f), new Vector3(-1.0f, 1.2f, z + .05f), mSteel, 1f, LLock, "+x");
        }
        DQ("lockers", "drain", new Vector3(-2.6f, .003f, -1.6f), new Vector3(-2.6f, .003f, -1.4f), new Vector3(-2.4f, .003f, -1.4f), new Vector3(-2.4f, .003f, -1.6f), mDark, 1f, LLock);
        foreach (var x in new[] { -5.9f, -2.6f }) foreach (var z in new[] { -7.5f, -5.0f }) DStrip("lockers", x, ClubCeil - .005f, z, LLock);
        foreach (var x in new[] { -3.2f, -2.0f })
        {
            DBox("lockers", "vapour-tight globe", new Vector3(x - .08f, ClubCeil - .2f, -1.68f), new Vector3(x + .08f, ClubCeil - .04f, -1.52f), mLensOn, 1f, LLock, "+y");
            DPoint("lockers", "globe", new Vector3(x, ClubCeil - .3f, -1.6f), 1.0f, 3.6f, Warm, LLock);
        }
    }

    // T1..T4 from the far door, x -3..3, 6 m each, floor -0.15 under ankle water; B the basin room; P the pillar hall;
    // D the stair room east of B
    static void DirLevel()
    {
        for (var k = 0; k < 4; k++)
        {
            float a = HallZ + 6f * k, b = a + 6f;
            var lay = k % 2 == 0 ? LOdd : LEven;
            var g = "t" + (k + 1);
            var rect = new List<DirDoor> { new DirDoor('S', -.7f, .7f, 2.25f, false), new DirDoor('N', -.7f, .7f, 2.25f, false) };
            var arch = new List<DirDoor> { new DirDoor('S', -.7f, .7f, 2.45f, true), new DirDoor('N', -.7f, .7f, 2.45f, true) };
            DQ(g + " base", "floor", new Vector3(-3f, LvlFloor, a), new Vector3(-3f, LvlFloor, b), new Vector3(3f, LvlFloor, b), new Vector3(3f, LvlFloor, a), mLvlFloor, T3, lay);
            DWater(g + " water", -3f, 3f, a, b, LvlWater, lay);
            if (k == 0)
            {
                DFace("t1 base", "sill riser", 'S', -3f, 3f, a, b, -.7f, .7f, LvlFloor, 0f, mLvlFloor, T3, lay);
                DRoom("t1 club", "t1", -3f, 3f, a, b, LvlFloor, LvlCeil, 1.35f, mClubTile, mClubPaint, 1f, null, mPaint, 1f, rect, false, "SNWE", lay);
                foreach (var s in "SNWE") DBand("t1 club", s, -3f, 3f, a, b, LvlFloor, .9f, 1.05f, mClubBand, rect, lay);
                DBox("t1 club", "sign blank", new Vector3(2.94f, 1.85f, a + 2.5f), new Vector3(3f, 2.2f, a + 3.5f), mSign, 1f, lay, "+x");
                DQ("t1 dry", "dry floor", new Vector3(-3f, 0f, a), new Vector3(-3f, 0f, b), new Vector3(3f, 0f, b), new Vector3(3f, 0f, a), mClubFloor, T3, lay);
                DFace("t1 dry", "dry riser", 'S', -3f, 3f, b, b, -.7f, .7f, LvlFloor, 0f, mClubFloor, T3, lay);
                foreach (var dz in new[] { 1.5f, 4.5f }) DTroffer("t1 clublight", 0f, LvlCeil - .005f, a + dz, lay);
                DRoom("t1 level", "t1", -3f, 3f, a, b, LvlFloor, LvlCeil, 9f, mLvlWall, mLvlWall, T3, null, mLvlWall, T3, rect, false, "SNWE", lay);
                foreach (var s in "SNWE") DBand("t1 level", s, -3f, 3f, a, b, LvlFloor, 1.2f, 1.5f, mLvlBand, rect, lay);
                foreach (var sd in new[] { -1f, 1f }) DValance("t1 levellight", sd, a, b, LvlCeil, lay);
            }
            else
            {
                DRoom(g + " sides", g, -3f, 3f, a, b, LvlFloor, LvlCeil, 9f, mLvlWall, mLvlWall, T3, null, mLvlWall, T3, rect, false, "WE", lay);
                DRoom(g + " rect", g, -3f, 3f, a, b, LvlFloor, LvlCeil, 9f, mLvlWall, mLvlWall, T3, null, null, T3, rect, false, "SN", lay);
                DRoom(g + " arch", g, -3f, 3f, a, b, LvlFloor, LvlCeil, 9f, mLvlWall, mLvlWall, T3, null, null, T3, arch, false, "SN", lay);
                foreach (var s in "SNWE")
                {
                    DBand(g + " rect", s, -3f, 3f, a, b, LvlFloor, 1.2f, 1.5f, mLvlBand, rect, lay);
                    DBand(g + " arch", s, -3f, 3f, a, b, LvlFloor, 1.2f, 1.5f, mLvlBand, arch, lay);
                }
                foreach (var sd in new[] { -1f, 1f }) DValance(g + " sides", sd, a, b, LvlCeil, lay);
            }
            DProbe(-3f, 3f, a, b, LvlFloor, LvlCeil);
        }

        // B: x -6..6, z bz0..bz0+12, ceiling 4.05; basin x -3.5..3.5, z pz0..pz1, 1.07 m sloping to 1.37 m; doors S, N (pillar hall), E (stair room)
        float bz0 = HallZ + 24f, bz1 = bz0 + 12f, px0 = -3.5f, px1 = 3.5f, pz0 = bz0 + 3f, pz1 = bz0 + 9f, ys = -1.07f, yd = -1.37f;
        var bDoors = new List<DirDoor> { new DirDoor('S', -.7f, .7f, 2.25f, false), new DirDoor('N', -.7f, .7f, 2.25f, false), new DirDoor('E', pz0 + 2.3f, pz0 + 3.7f, 2.25f, false) };
        DRoom("basin", "basin room", -6f, 6f, bz0, bz1, LvlFloor, BasinCeil, 9f, mLvlWall, mLvlWall, T3, null, mLvlWall, T3, bDoors, false, "SNWE", LBasin);
        foreach (var s in "SNWE") DBand("basin", s, -6f, 6f, bz0, bz1, LvlFloor, 1.2f, 1.5f, mLvlBand, bDoors, LBasin);
        DQ("basin", "deck s", new Vector3(-6f, LvlFloor, bz0), new Vector3(-6f, LvlFloor, pz0), new Vector3(6f, LvlFloor, pz0), new Vector3(6f, LvlFloor, bz0), mLvlFloor, T3, LBasin);
        DQ("basin", "deck n", new Vector3(-6f, LvlFloor, pz1), new Vector3(-6f, LvlFloor, bz1), new Vector3(6f, LvlFloor, bz1), new Vector3(6f, LvlFloor, pz1), mLvlFloor, T3, LBasin);
        DQ("basin", "deck w", new Vector3(-6f, LvlFloor, pz0), new Vector3(-6f, LvlFloor, pz1), new Vector3(px0, LvlFloor, pz1), new Vector3(px0, LvlFloor, pz0), mLvlFloor, T3, LBasin);
        DQ("basin", "deck e", new Vector3(px1, LvlFloor, pz0), new Vector3(px1, LvlFloor, pz1), new Vector3(6f, LvlFloor, pz1), new Vector3(6f, LvlFloor, pz0), mLvlFloor, T3, LBasin);
        var basinMat = ZLMat("ZL_TileBasin");
        var LB = LBasin | BasinLayer;
        DQ("basin", "basin floor", new Vector3(px0, ys, pz0), new Vector3(px0, yd, pz1), new Vector3(px1, yd, pz1), new Vector3(px1, ys, pz0), basinMat, T3, LB);
        DQ("basin", "basin wall w", new Vector3(px0, ys, pz0), new Vector3(px0, LvlFloor, pz0), new Vector3(px0, LvlFloor, pz1), new Vector3(px0, yd, pz1), basinMat, T3, LB);
        DQ("basin", "basin wall e", new Vector3(px1, ys, pz0), new Vector3(px1, yd, pz1), new Vector3(px1, LvlFloor, pz1), new Vector3(px1, LvlFloor, pz0), basinMat, T3, LB);
        DQ("basin", "basin wall s", new Vector3(px0, ys, pz0), new Vector3(px1, ys, pz0), new Vector3(px1, LvlFloor, pz0), new Vector3(px0, LvlFloor, pz0), basinMat, T3, LB);
        DQ("basin", "basin wall n", new Vector3(px0, yd, pz1), new Vector3(px0, LvlFloor, pz1), new Vector3(px1, LvlFloor, pz1), new Vector3(px1, yd, pz1), basinMat, T3, LB);
        DWater("basin water", -6f, 6f, bz0, bz1, LvlWater, LBasin);
        foreach (var rx in new[] { -.25f, .25f })
        {
            var pts = new[] { new Vector3(rx, LvlFloor, pz1 + .45f), new Vector3(rx, .63f, pz1 + .45f), new Vector3(rx, .75f, pz1 + .25f), new Vector3(rx, .63f, pz1 + .02f), new Vector3(rx, .1f, pz1 - .12f), new Vector3(rx, -1.15f, pz1 - .12f) };
            for (var i = 0; i + 1 < pts.Length; i++) DCyl("basin", "ladder rail", pts[i], pts[i + 1], .021f, mSteel, LB);
        }
        foreach (var ty in new[] { -.45f, -.75f }) DBox("basin", "ladder tread", new Vector3(-.24f, ty - .02f, pz1 - .16f), new Vector3(.24f, ty, pz1 - .06f), mSteel, 1f, LB, "");
        DChair("basin", 4.75f, LvlFloor, pz0 + 1.5f, -1f, LBasin);
        foreach (var z in new[] { pz0 + .5f, pz1 - .5f })
        {
            DMark("basin", px0 - .45f, LvlFloor + .002f, z, LBasin);
            DMark("basin", px1 + .45f, LvlFloor + .002f, z, LBasin);
        }
        foreach (var x in new[] { -4.5f, -1.5f, 1.5f, 4.5f })
            foreach (var z in new[] { bz0 + 1.5f, bz0 + 4.5f, bz0 + 7.5f, bz0 + 10.5f })
            {
                var ne = x > 0f && z > bz0 + 3f;
                var r = DLens("basin", x, BasinCeil, z, LBasin, ne ? basinNELights : null);
                if (ne) basinNE.Add(r);
            }
        foreach (var zz in new[] { pz0 + 1.5f, pz0 + 4.5f })
        {
            DNiche("basin", new Vector3(px0, -.72f, zz), Vector3.right, LBasin);
            DNiche("basin", new Vector3(px1, -.72f, zz), Vector3.left, LBasin);
        }
        mCausticB = new Material(ZLMat("ZL_Caustic"));
        mCausticB.SetVector("_Rect", new Vector4(P0.x + px0, P0.z + pz0, P0.x + px1, P0.z + pz1));
        var cf = DQ("basin", "caustic floor", new Vector3(px0, ys + .01f, pz0), new Vector3(px0, yd + .01f, pz1), new Vector3(px1, yd + .01f, pz1), new Vector3(px1, ys + .01f, pz0), mCausticB, 1f, LBasin);
        cf.shadowCastingMode = ShadowCastingMode.Off;
        mCausticBC = new Material(ZLMat("ZL_Caustic"));
        mCausticBC.SetVector("_Rect", new Vector4(P0.x + px0 - 1.5f, P0.z + pz0 - 1.5f, P0.x + px1 + 1.5f, P0.z + pz1 + 1.5f));
        mCausticBC.SetFloat("_Scale", 2.4f);
        mCausticBC.SetFloat("_Fade", 1.6f);
        var cy = BasinCeil - .006f;
        var cc = DQ("basin", "caustic ceiling", new Vector3(px0 - 1.5f, cy, pz0 - 1.5f), new Vector3(px1 + 1.5f, cy, pz0 - 1.5f), new Vector3(px1 + 1.5f, cy, pz1 + 1.5f), new Vector3(px0 - 1.5f, cy, pz1 + 1.5f), mCausticBC, 1f, LBasin);
        cc.shadowCastingMode = ShadowCastingMode.Off;
        DProbe(-6f, 6f, bz0, bz1, LvlFloor, BasinCeil);

        // P: the flooded pillar hall north of B, 18 x 18 m under a 2.4 m ceiling; columns on the 3 m grid; the east third unlit
        float qz0 = bz1, qz1 = bz1 + 18f;
        var qDoors = new List<DirDoor> { new DirDoor('S', -.7f, .7f, 2.25f, false) };
        DRoom("pillars", "pillar hall", -9f, 9f, qz0, qz1, LvlFloor, PillarCeil, 9f, mLvlWall, mLvlWall, T3, mLvlFloor, mLvlWall, T3, qDoors, true, "SNWE", LPillar);
        DWater("pillars water", -9f, 9f, qz0, qz1, LvlWater, LPillar);
        for (var ix = -2; ix <= 2; ix++)
            for (var iz = 1; iz <= 5; iz++)
            {
                float x = ix * 3f, z = qz0 + iz * 3f;
                DBox("pillars", "pillar", new Vector3(x - .3f, LvlFloor, z - .3f), new Vector3(x + .3f, PillarCeil, z + .3f), mLvlWall, T3, LPillar, "-y+y");
            }
        var lit = new[] { new Vector2(-7.5f, 1.5f), new Vector2(-4.5f, 4.5f), new Vector2(-1.5f, 1.5f), new Vector2(-7.5f, 7.5f), new Vector2(1.5f, 7.5f), new Vector2(-4.5f, 10.5f), new Vector2(-1.5f, 13.5f), new Vector2(-7.5f, 16.5f), new Vector2(1.5f, 4.5f), new Vector2(-4.5f, 16.5f) };
        foreach (var c in lit) DLens("pillars", c.x, PillarCeil, qz0 + c.y, LPillar, null);
        DProbe(-9f, 9f, qz0, qz1, LvlFloor, PillarCeil);

        // D: the stair room east of B (x 6..12): a stair down into dark water to a gate below the water line, an intake grate
        float cz0 = pz0 + 2.2f, cz1 = pz0 + 3.8f, sx0 = 8f, sx1 = 10.4f, sby = -1.9f;
        var dDoors = new List<DirDoor> { new DirDoor('W', pz0 + 2.3f, pz0 + 3.7f, 2.25f, false) };
        DRoom("stair", "stair room", 6f, 12f, pz0, pz1, LvlFloor, LvlCeil, 9f, mLvlWall, mLvlWall, T3, null, mLvlWall, T3, dDoors, false, "SNWE", LStair);
        foreach (var s in "SNWE") DBand("stair", s, 6f, 12f, pz0, pz1, LvlFloor, 1.2f, 1.5f, mLvlBand, dDoors, LStair);
        DQ("stair", "floor s", new Vector3(6f, LvlFloor, pz0), new Vector3(6f, LvlFloor, cz0), new Vector3(12f, LvlFloor, cz0), new Vector3(12f, LvlFloor, pz0), mLvlFloor, T3, LStair);
        DQ("stair", "floor n", new Vector3(6f, LvlFloor, cz1), new Vector3(6f, LvlFloor, pz1), new Vector3(12f, LvlFloor, pz1), new Vector3(12f, LvlFloor, cz1), mLvlFloor, T3, LStair);
        DQ("stair", "floor w", new Vector3(6f, LvlFloor, cz0), new Vector3(6f, LvlFloor, cz1), new Vector3(sx0, LvlFloor, cz1), new Vector3(sx0, LvlFloor, cz0), mLvlFloor, T3, LStair);
        var LSB = LStair | BasinLayer;
        const int steps = 7;
        float run = (sx1 - sx0) / steps, rise = (LvlFloor - sby) / steps;
        for (var i = 0; i < steps; i++)
        {
            float xa = sx0 + i * run, xb = xa + run, yt = LvlFloor - i * rise, yb = yt - rise;
            var m = stairShades[i];
            DFace("stair", "riser", 'E', 0f, xa, cz0, cz1, cz0, cz1, yb, yt, m, T3, LSB);
            DQ("stair", "tread", new Vector3(xa, yb, cz0), new Vector3(xa, yb, cz1), new Vector3(xb, yb, cz1), new Vector3(xb, yb, cz0), m, T3, LSB);
            DFace("stair", "side n", 'N', 0f, 0f, 0f, cz1, xa, xb, sby, LvlFloor, m, T3, LSB);
            DFace("stair", "side s", 'S', 0f, 0f, cz0, 0f, xa, xb, sby, LvlFloor, m, T3, LSB);
        }
        var deep = stairShades[steps];
        DQ("stair", "sump floor", new Vector3(sx1, sby, cz0), new Vector3(sx1, sby, cz1), new Vector3(12f, sby, cz1), new Vector3(12f, sby, cz0), deep, T3, LSB);
        DFace("stair", "side n", 'N', 0f, 0f, 0f, cz1, sx1, 12f, sby, LvlFloor, deep, T3, LSB);
        DFace("stair", "side s", 'S', 0f, 0f, cz0, 0f, sx1, 12f, sby, LvlFloor, deep, T3, LSB);
        DFace("stair", "sump end", 'E', 0f, 12f, cz0, cz1, cz0, cz1, sby, LvlFloor, deep, T3, LSB);
        for (var z = cz0 + .1f; z < cz1 - .05f; z += .12f) DCyl("stair", "gate bar", new Vector3(11f, sby, z), new Vector3(11f, -.45f, z), .014f, mSteel, LSB);
        foreach (var y in new[] { -1.6f, -.5f }) DCyl("stair", "gate rail", new Vector3(11f, y, cz0 + .02f), new Vector3(11f, y, cz1 - .02f), .018f, mSteel, LSB);
        DFace("stair", "intake dark", 'N', 0f, 0f, 0f, pz1 - .004f, 7f, 9.4f, LvlFloor, .75f, mDark, 1f, LStair);
        for (var y = LvlFloor + .06f; y < .74f; y += .1f) DBox("stair", "intake bar", new Vector3(7f, y, pz1 - .05f), new Vector3(9.4f, y + .025f, pz1 - .02f), mSteel, 1f, LStair, "");
        DBox("stair", "intake frame", new Vector3(6.94f, LvlFloor, pz1 - .06f), new Vector3(7.0f, .81f, pz1 - .004f), mSteel, 1f, LStair, "");
        DBox("stair", "intake frame", new Vector3(9.4f, LvlFloor, pz1 - .06f), new Vector3(9.46f, .81f, pz1 - .004f), mSteel, 1f, LStair, "");
        DBox("stair", "intake frame", new Vector3(6.94f, .75f, pz1 - .06f), new Vector3(9.46f, .81f, pz1 - .004f), mSteel, 1f, LStair, "");
        DWater("stair water", 6f, 12f, pz0, pz1, LvlWater, LStair);
        DLens("stair", 7.0f, LvlCeil, pz0 + 1.2f, LStair, null);
        DNiche("stair", new Vector3(8.6f, -.65f, cz1), Vector3.back, LStair);
        DProbe(6f, 12f, pz0, pz1, LvlFloor, LvlCeil);
    }

    // ------------------------------------------------------------------------------------------- variations
    static void DPalette(string id)
    {
        Color wall = Color.white, band = Color.white, deck = Color.white, locker = new Color(.78f, .76f, .70f), paint = Color.white;
        if (id == "B") { band = new Color(.52f, .80f, .66f); deck = new Color(.66f, .34f, .25f); locker = new Color(.82f, .74f, .56f); }
        if (id == "C") { band = new Color(.20f, .56f, .58f); deck = new Color(.94f, .92f, .86f); locker = new Color(.55f, .57f, .59f); }
        // F1: genre-forward, aqua accents on off-white all the way in
        if (id == "F1") { band = new Color(.40f, .76f, .77f); deck = new Color(.93f, .93f, .91f); locker = new Color(.72f, .75f, .76f); }
        var hallBand = band;
        // E90: the 1990 commercial palette in the club (rose band, teal lockers, grey-mauve walls), teal band in the hall
        if (id == "E90") { band = new Color(.74f, .48f, .54f); hallBand = new Color(.18f, .50f, .50f); deck = new Color(.66f, .67f, .68f); locker = new Color(.16f, .42f, .44f); paint = new Color(.86f, .80f, .83f); }
        var floor = new Color(deck.r * .9f, deck.g * .9f, deck.b * .9f, 1f);
        mHallWall.SetColor("_BaseColor", wall);
        mClubTile.SetColor("_BaseColor", wall);
        mHallBand.SetColor("_BaseColor", hallBand);
        mClubBand.SetColor("_BaseColor", band);
        mClubPaint.SetColor("_BaseColor", paint);
        mHallDeck.SetColor("_BaseColor", deck);
        mClubFloor.SetColor("_BaseColor", floor);
        mLocker.SetColor("_BaseColor", locker);
    }

    static void DLevelPalette(string id)
    {
        Color wall = Color.white, floor = new Color(.95f, .97f, 1f), band = Color.white;
        if (id == "blue") { wall = new Color(.80f, .90f, 1f); floor = new Color(.78f, .88f, 1f); band = wall; }
        if (id == "mint") band = new Color(.52f, .80f, .66f);
        mLvlWall.SetColor("_BaseColor", wall);
        mLvlBand.SetColor("_BaseColor", band);
        mLvlFloor.SetColor("_BaseColor", floor);
    }

    static void DHallTile(string v) { DShow("hall 24", v == "24"); DShow("hall 12", v == "12"); DShow("hall full", v == "full"); }

    static void DT1(bool levelWalls, bool water, bool levelLight)
    {
        DShow("t1 club", !levelWalls); DShow("t1 level", levelWalls);
        DShow("t1 water", water); DShow("t1 dry", !water);
        DShow("t1 clublight", !levelLight); DShow("t1 levellight", levelLight);
    }

    static void DArch(bool on) { foreach (var g in new[] { "t2", "t3", "t4" }) { DShow(g + " rect", !on); DShow(g + " arch", on); } }

    static void DBasinDark(bool dark)
    {
        foreach (var r in basinNE) r.sharedMaterial = dark ? mLensOff : mLensOn;
        foreach (var l in basinNELights) l.enabled = !dark;
        mCausticB.SetFloat("_Level", dark ? .5f : .9f);
        mCausticBC.SetFloat("_Level", dark ? .9f : .3f);
    }

    // ------------------------------------------------------------------------------------------- the fusion pass
    static void DirFusionSetup()
    {
        mWater = DInst(ZLMat("ZL_Water"), "dir water");
        foreach (var r in standRoot.GetComponentsInChildren<Renderer>(true)) if (r.name == "water") r.sharedMaterial = mWater;
        mWaterline = DInst(ZLMat("ZL_TileWall"), "dir waterline");
        mCausticLvl = new Material(ZLMat("ZL_Caustic"));
        mCausticLvl.SetVector("_Rect", new Vector4(P0.x - 9f, P0.z + HallZ, P0.x + 12f, P0.z + HallZ + 54f));
        mCausticLvl.SetFloat("_Scale", 2.4f);
        mCausticLvl.SetFloat("_Fade", .6f);
        mCausticLvl.SetFloat("_Level", 0f);
        // the aqua waterline course: 0.3 m just above the ankle water in the level rooms, the top course inside both basins
        var rect = new List<DirDoor> { new DirDoor('S', -.7f, .7f, 2.25f, false), new DirDoor('N', -.7f, .7f, 2.25f, false) };
        for (var k = 0; k < 4; k++)
        {
            float a = HallZ + 6f * k, b = a + 6f;
            var lay = k % 2 == 0 ? LOdd : LEven;
            var g = k == 0 ? "t1 level" : "wl t" + (k + 1);
            foreach (var s in "SNWE") DBand(g, s, -3f, 3f, a, b, LvlFloor, LvlFloor + .02f, LvlFloor + .32f, mWaterline, rect, lay);
            var c = DQ(g, "caustic ceiling", new Vector3(-3f, LvlCeil - .006f, a), new Vector3(3f, LvlCeil - .006f, a), new Vector3(3f, LvlCeil - .006f, b), new Vector3(-3f, LvlCeil - .006f, b), mCausticLvl, 1f, lay);
            c.shadowCastingMode = ShadowCastingMode.Off;
        }
        float bz0 = HallZ + 24f, bz1 = bz0 + 12f, pz0 = bz0 + 3f, pz1 = bz0 + 9f, qz1 = bz1 + 18f;
        var bDoors = new List<DirDoor> { new DirDoor('S', -.7f, .7f, 2.25f, false), new DirDoor('N', -.7f, .7f, 2.25f, false), new DirDoor('E', pz0 + 2.3f, pz0 + 3.7f, 2.25f, false) };
        var qDoors = new List<DirDoor> { new DirDoor('S', -.7f, .7f, 2.25f, false) };
        var dDoors = new List<DirDoor> { new DirDoor('W', pz0 + 2.3f, pz0 + 3.7f, 2.25f, false) };
        foreach (var s in "SNWE")
        {
            DBand("wl basin", s, -6f, 6f, bz0, bz1, LvlFloor, LvlFloor + .02f, LvlFloor + .32f, mWaterline, bDoors, LBasin);
            DBand("wl pillars", s, -9f, 9f, bz1, qz1, LvlFloor, LvlFloor + .02f, LvlFloor + .32f, mWaterline, qDoors, LPillar);
            DBand("wl stair", s, 6f, 12f, pz0, pz1, LvlFloor, LvlFloor + .02f, LvlFloor + .32f, mWaterline, dDoors, LStair);
        }
        var hb = LHall | BasinLayer;
        DFace("wl hall", "waterline", 'W', -PoolX + .004f, 0f, 0f, 0f, PoolZ0, PoolZ1, -.30f, -.005f, mWaterline, T3, hb);
        DFace("wl hall", "waterline", 'E', 0f, PoolX - .004f, 0f, 0f, PoolZ0, PoolZ1, -.30f, -.005f, mWaterline, T3, hb);
        DFace("wl hall", "waterline", 'S', 0f, 0f, PoolZ0 + .004f, 0f, -PoolX, PoolX, -.30f, -.005f, mWaterline, T3, hb);
        DFace("wl hall", "waterline", 'N', 0f, 0f, 0f, PoolZ1 - .004f, -PoolX, PoolX, -.30f, -.005f, mWaterline, T3, hb);
        var bb = LBasin | BasinLayer;
        DFace("wl basin", "waterline", 'W', -3.5f + .004f, 0f, 0f, 0f, pz0, pz1, -.45f, -.155f, mWaterline, T3, bb);
        DFace("wl basin", "waterline", 'E', 0f, 3.5f - .004f, 0f, 0f, pz0, pz1, -.45f, -.155f, mWaterline, T3, bb);
        DFace("wl basin", "waterline", 'S', 0f, 0f, pz0 + .004f, 0f, -3.5f, 3.5f, -.45f, -.155f, mWaterline, T3, bb);
        DFace("wl basin", "waterline", 'N', 0f, 0f, 0f, pz1 - .004f, -3.5f, 3.5f, -.45f, -.155f, mWaterline, T3, bb);
        foreach (var q in new[] { new Vector4(-6f, 6f, bz0, bz1), new Vector4(-9f, 9f, bz1, qz1), new Vector4(6f, 12f, pz0, pz1) })
        {
            var y = (q.z == bz1 ? PillarCeil : q.z == bz0 ? BasinCeil : LvlCeil) - .008f;
            var c = DQ("wl ceilings", "caustic ceiling", new Vector3(q.x, y, q.z), new Vector3(q.y, y, q.z), new Vector3(q.y, y, q.w), new Vector3(q.x, y, q.w), mCausticLvl, 1f, LBasin);
            c.shadowCastingMode = ShadowCastingMode.Off;
        }
        var go = new GameObject("dir grade volume");
        go.transform.SetParent(standRoot, false);
        dirVol = go.AddComponent<Volume>();
        dirVol.isGlobal = true;
        dirVol.priority = 100f;
        dirVol.weight = 0f;
        var p = ScriptableObject.CreateInstance<VolumeProfile>();
        dirWB = p.Add<WhiteBalance>(false);
        dirCA = p.Add<ColorAdjustments>(false);
        dirVol.sharedProfile = p;
    }

    static string LightKind(Light l)
    {
        if (overLights.Contains(l)) return "mh";
        if (waterLights.Contains(l) || l.name == "underwater") return "under";
        for (var t = l.transform.parent; t != null && t != standRoot; t = t.parent)
            if (t.name == "dir entry" || t.name == "dir lockers" || t.name == "dir t1 clublight") return "club";
        return "level";
    }

    // warm = the game's own look (first pass); turq / era = the pool family's look; night = wet-niche lamps only
    static void DLook(string id)
    {
        bool night = id == "night", cool = id != "warm";
        if (!cool) FrontRoomsLook.ApplyAmbient();
        else
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = night ? new Color(.02f, .045f, .055f) : new Color(.15f, .20f, .22f);
            RenderSettings.ambientEquatorColor = night ? new Color(.025f, .055f, .065f) : new Color(.17f, .23f, .24f);
            RenderSettings.ambientGroundColor = night ? new Color(.03f, .07f, .075f) : new Color(.21f, .28f, .28f);
            RenderSettings.ambientLight = RenderSettings.ambientSkyColor;
            RenderSettings.fogColor = night ? new Color(.02f, .045f, .05f) : new Color(.11f, .16f, .17f);
            RenderSettings.fogDensity = .010f;
        }
        dirVol.weight = cool ? 1f : 0f;
        dirWB.temperature.Override(night ? -16f : -12f);
        dirWB.tint.Override(-2f);
        dirCA.postExposure.Override(night ? .6f : .45f);
        dirCA.contrast.Override(-4f);
        dirCA.saturation.Override(night ? 6f : 4f);
        foreach (var l in standRoot.GetComponentsInChildren<Light>(true))
        {
            if (!lightBase.ContainsKey(l)) { lightBase[l] = l.color; lightBaseI[l] = l.intensity; }
            var k = LightKind(l);
            if (!cool) l.color = lightBase[l];
            else if (k == "mh") l.color = new Color(.90f, .97f, 1f);
            else if (k == "under") l.color = new Color(.55f, .92f, 1f);
            else if (k == "club") l.color = new Color(.93f, .97f, 1f);
            else l.color = new Color(.86f, .96f, 1f);
            l.intensity = lightBaseI[l] * (cool && k == "level" ? 1.2f : 1f);
            if (k == "level") l.enabled = !night;
        }
        SetPoolState(!night, true);
        mLensOn.SetColor("_BaseColor", night ? new Color(.04f, .04f, .04f) : cool ? new Color(3.9f, 4.3f, 4.6f) : new Color(4.2f, 4.1f, 3.9f));
        mWater.SetColor("_BaseColor", !cool ? new Color(.05f, .2f, .22f, .3f) : night ? new Color(.10f, .45f, .50f, .36f) : new Color(.16f, .60f, .62f, .36f));
        mWaterline.SetColor("_BaseColor", cool ? new Color(.40f, .76f, .77f) : Color.white);
        mLvlWall.SetColor("_BaseColor", cool ? new Color(.97f, .99f, 1f) : Color.white);
        var cc = cool ? new Color(.32f, .50f, .50f) : new Color(.3f, .42f, .42f);
        foreach (var m in new[] { causticFloor, causticCeil, mCausticB, mCausticBC, mCausticLvl }) m.SetColor("_Color", cc);
        causticFloor.SetFloat("_Level", night ? 0f : cool ? 1.0f : .9f);
        causticCeil.SetFloat("_Level", night ? 1.0f : cool ? .6f : .3f);
        mCausticB.SetFloat("_Level", night ? .3f : cool ? 1.0f : .9f);
        mCausticBC.SetFloat("_Level", night ? 1.0f : cool ? .65f : .3f);
        mCausticLvl.SetFloat("_Level", night ? .5f : cool ? .55f : 0f);
        if (mCausticE != null) { mCausticE.SetColor("_Color", cc); mCausticE.SetFloat("_Level", night ? .8f : cool ? .55f : 0f); }
    }

    static void DS(string name, string note, Vector3 eye, Vector3 at) { Aim(P0 + eye, P0 + at, -1f); Shot(name, note); }

    // ------------------------------------------------------------------------------------------- structure set pieces (18_structure_fusion.md)
    static Material mWin, mGlassBlock, mExit, mSlide, mCausticE, mTeal, mRose, mConcrete, mGrout;
    static float e2Lz1;

    // a disc facing cross(right, up)
    static void DDisc(string grp, string name, Vector3 c, Vector3 right, Vector3 up, float r, Material m, uint lay)
    {
        const int n = 20;
        for (var i = 0; i < n; i++)
        {
            float a0 = Mathf.PI * 2f * i / n, a1 = Mathf.PI * 2f * (i + 1) / n;
            var p0 = c + (right * Mathf.Cos(a0) + up * Mathf.Sin(a0)) * r;
            var p1 = c + (right * Mathf.Cos(a1) + up * Mathf.Sin(a1)) * r;
            DQ(grp, name, c, p0, p1, p1, m, 1f, lay);
        }
    }

    static void DirStructure()
    {
        mWin = DUnlit(new Color(.30f, 1.35f, 1.45f));
        mGlassBlock = DUnlit(new Color(1.35f, 1.5f, 1.55f));
        mExit = DUnlit(new Color(2.6f, .25f, .18f));
        mSlide = DLit(new Color(.20f, .55f, .60f), .75f);
        mTeal = DLit(new Color(.16f, .44f, .46f), .45f);
        mRose = DLit(new Color(.74f, .48f, .54f), .45f);
        mConcrete = DLit(new Color(.55f, .55f, .53f), .2f);
        mGrout = DLit(new Color(.55f, .57f, .58f), .3f);
        mCausticE = new Material(ZLMat("ZL_Caustic"));
        mCausticE.SetVector("_Rect", new Vector4(P0.x - 60f, P0.z - 2f, P0.x - 30f, P0.z + 64f));
        mCausticE.SetFloat("_Scale", 2.4f);
        mCausticE.SetFloat("_Fade", .6f);
        mCausticE.SetFloat("_Level", 0f);
        var ceil = SurfMat("L0_Ceiling");
        var block = SurfMat("Stair_Block");
        var none = new List<DirDoor>();

        // E1 (M1 M2 M7): the office ceiling over flooded tile, columns that multiply, a beach entry. x -58..-40, z 0..18
        const uint L1 = 2u;
        float ax0 = -58f, ax1 = -40f, az0 = 0f, az1 = 18f, ac = 2.6f;
        DRoom("e1", "e1", ax0, ax1, az0, az1, LvlFloor, ac, 9f, mLvlWall, mLvlWall, T3, null, null, T3, none, false, "SNWE", L1);
        DQ("e1", "beach", new Vector3(ax0, .05f, az0), new Vector3(ax0, LvlFloor, az0 + 4f), new Vector3(ax1, LvlFloor, az0 + 4f), new Vector3(ax1, .05f, az0), mLvlFloor, T3, L1);
        DQ("e1", "floor", new Vector3(ax0, LvlFloor, az0 + 4f), new Vector3(ax0, LvlFloor, az1), new Vector3(ax1, LvlFloor, az1), new Vector3(ax1, LvlFloor, az0 + 4f), mLvlFloor, T3, L1);
        DWater("e1 water", ax0, ax1, az0, az1, LvlWater, L1);
        DQ("e1", "ceiling", new Vector3(ax0, ac, az0), new Vector3(ax1, ac, az0), new Vector3(ax1, ac, az1), new Vector3(ax0, ac, az1), ceil, 1f, L1);
        foreach (var v in new[] { new Vector2(-52.4f, 9.1f), new Vector2(-44.5f, 12.8f), new Vector2(-50.0f, 15.2f), new Vector2(-46.3f, 6.7f), new Vector2(-55.1f, 13.9f) })
            DQ("e1", "missing tile", new Vector3(v.x, ac - .003f, v.y), new Vector3(v.x + .61f, ac - .003f, v.y), new Vector3(v.x + .61f, ac - .003f, v.y + .61f), new Vector3(v.x, ac - .003f, v.y + .61f), mDark, 1f, L1);
        var ti = 0;
        for (var x = ax0 + 2.4f; x < ax1 - 1f; x += 3.6f)
            for (var z = az0 + 2.4f; z < az1 - 1f; z += 3.6f)
            {
                var dead = ti++ % 3 == 2;
                DQ("e1", "troffer lens", new Vector3(x - .3f, ac - .004f, z - .6f), new Vector3(x + .3f, ac - .004f, z - .6f), new Vector3(x + .3f, ac - .004f, z + .6f), new Vector3(x - .3f, ac - .004f, z + .6f), dead ? mLensOff : mLensOn, 1f, L1);
                if (!dead) DSpot("e1", "troffer", new Vector3(x, ac - .06f, z), Vector3.down, 150f, 90f, 2.6f, 6f, CoolWhite, false, L1);
            }
        foreach (var row in new[] { new Vector3(9f, 6f, 6.01f), new Vector3(4.5f, 10.5f, 10.51f), new Vector3(3f, 13.5f, 16.6f) })
            for (var z = row.y; z <= row.z; z += 3f)
                for (var x = ax0 + row.x; x < ax1 - .5f; x += row.x)
                    DBox("e1", "column", new Vector3(x - .3f, LvlFloor, z - .3f), new Vector3(x + .3f, ac, z + .3f), mLvlWall, T3, L1, "-y+y");
        DProbe(ax0, ax1, az0, az1, LvlFloor, ac);

        // E2 (M3): the tower's fire stair runs down into dark water to a half-drowned gate under an EXIT sign. x -56.2..-53.8, z 24..
        const uint L2 = 4u;
        var L2B = L2 | BasinLayer;
        float sx0 = -56.2f, sx1 = -53.8f, sz0 = 24f, top = 0f, wl = -1.5f, bot = -2.52f;
        const int nr = 14;
        float rz = .28f, ry = .18f, fz0 = sz0 + 1.5f, fz1 = fz0 + nr * rz, lz1 = fz1 + 1.6f;
        e2Lz1 = lz1;
        var gate = new List<DirDoor> { new DirDoor('N', -55.4f, -54.6f, 2.1f, false) };
        foreach (var s in "WE") DWall("e2", "stair wall", s, sx0, sx1, sz0, lz1, bot, top + 3f, wl + .3f, mLvlWall, block, 1f, none, L2B);
        DWall("e2", "stair wall n", 'N', sx0, sx1, sz0, lz1, bot, top + 3f, wl + .3f, mLvlWall, block, 1f, gate, L2B);
        DWall("e2", "stair wall s", 'S', sx0, sx1, sz0, lz1, top, top + 3f, top, mLvlWall, block, 1f, new List<DirDoor> { new DirDoor('S', -55.6f, -54.4f, 2.1f, false) }, L2);
        DQ("e2", "ceiling", new Vector3(sx0, top + 3f, sz0), new Vector3(sx1, top + 3f, sz0), new Vector3(sx1, top + 3f, lz1), new Vector3(sx0, top + 3f, lz1), mPaint, 1f, L2);
        DQ("e2", "top landing", new Vector3(sx0, top, sz0), new Vector3(sx0, top, fz0), new Vector3(sx1, top, fz0), new Vector3(sx1, top, sz0), mConcrete, 1f, L2);
        for (var i = 0; i < nr; i++)
        {
            float za = fz0 + i * rz, zb = za + rz, yt = top - i * ry, yb = yt - ry;
            DFace("e2", "riser", 'S', sx0, sx1, za, za, sx0, sx1, yb, yt, mConcrete, 1f, L2B);
            DQ("e2", "tread", new Vector3(sx0, yb, za), new Vector3(sx0, yb, zb), new Vector3(sx1, yb, zb), new Vector3(sx1, yb, za), mConcrete, 1f, L2B);
        }
        DQ("e2", "bottom landing", new Vector3(sx0, bot, fz1), new Vector3(sx0, bot, lz1), new Vector3(sx1, bot, lz1), new Vector3(sx1, bot, fz1), mConcrete, 1f, L2B);
        var hx = sx1 - .08f;
        DCyl("e2", "handrail", new Vector3(hx, .9f, sz0 + .3f), new Vector3(hx, .9f, fz0), .022f, mSteel, L2);
        DCyl("e2", "handrail", new Vector3(hx, .9f, fz0), new Vector3(hx, bot + .9f, fz1), .022f, mSteel, L2B);
        for (var i = 0; i <= nr; i += 3) { var pz = fz0 + i * rz; var py = top - i * ry; DCyl("e2", "rail post", new Vector3(hx, py, pz), new Vector3(hx, py + .9f, pz), .018f, mSteel, L2B); }
        for (var x = -55.35f; x < -54.62f; x += .11f) DCyl("e2", "gate bar", new Vector3(x, bot, lz1 - .03f), new Vector3(x, bot + 2.08f, lz1 - .03f), .014f, mSteel, L2B);
        foreach (var y in new[] { bot + .4f, bot + 1.2f, bot + 2.0f }) DCyl("e2", "gate rail", new Vector3(-55.4f, y, lz1 - .03f), new Vector3(-54.6f, y, lz1 - .03f), .018f, mSteel, L2B);
        DBox("e2", "exit sign", new Vector3(-55.18f, bot + 2.28f, lz1 - .07f), new Vector3(-54.82f, bot + 2.42f, lz1 - .02f), mExit, 1f, L2, "+z");
        DPoint("e2", "stair light", new Vector3(sx1 - .2f, 2.4f, sz0 + .8f), 1.3f, 5f, Warm, L2);
        DPoint("e2", "stair light", new Vector3(sx0 + .2f, -.3f, fz0 + 2.2f), .7f, 4f, Warm, L2);
        DWater("e2 water", sx0, sx1, fz0, lz1, wl, L2);
        DProbe(sx0, sx1, sz0, lz1, bot, top + 3f);

        // E6: the dark zone beyond the gate: the flooded lower basement, dead troffers, wet-niche lamps, a glowing window
        float ex0 = -57.2f, ex1 = -52.8f, ez1 = lz1 + 9f, ec = bot + 2.7f;
        DRoom("e6", "e6", ex0, ex1, lz1, ez1, bot, ec, 9f, mLvlWall, mLvlWall, T3, mLvlFloor, ceil, 1f, new List<DirDoor> { new DirDoor('S', -55.4f, -54.6f, 2.1f, false) }, true, "SNWE", L2B);
        foreach (var dz in new[] { 2.2f, 5.6f }) DQ("e6", "dead troffer", new Vector3(-55.3f, ec - .004f, lz1 + dz - .6f), new Vector3(-54.7f, ec - .004f, lz1 + dz - .6f), new Vector3(-54.7f, ec - .004f, lz1 + dz + .6f), new Vector3(-55.3f, ec - .004f, lz1 + dz + .6f), mLensOff, 1f, L2);
        foreach (var dz in new[] { 2.5f, 6.5f })
        {
            DNiche("e6", new Vector3(ex0, wl - .45f, lz1 + dz), Vector3.right, L2);
            DNiche("e6", new Vector3(ex1, wl - .45f, lz1 + dz), Vector3.left, L2);
        }
        DFace("e6", "window glow", 'N', 0f, 0f, 0f, ez1 - .004f, -55.6f, -54.4f, wl + .2f, wl + .9f, mWin, 1f, L2);
        DSpot("e6", "underwater", new Vector3(-55f, wl + .55f, ez1 - .2f), Vector3.back, 120f, 60f, 2.0f, 6f, new Color(.4f, .9f, .95f), false, L2B);
        DWater("e6 water", ex0, ex1, lz1, ez1, wl, L2);
        var c6 = DQ("e6", "caustic ceiling", new Vector3(ex0, ec - .008f, lz1), new Vector3(ex1, ec - .008f, lz1), new Vector3(ex1, ec - .008f, ez1), new Vector3(ex0, ec - .008f, ez1), mCausticE, 1f, L2);
        c6.shadowCastingMode = ShadowCastingMode.Off;
        DProbe(ex0, ex1, lz1, ez1, bot, ec);

        // E3 (M4): the service gallery beside a basin, lit only by its observation windows. x -45.5..-43.7, z 24..39
        const uint L3 = 8u;
        float gx0 = -45.5f, gx1 = -43.7f, gz0 = 24f, gz1 = 39f, gc = 2.4f;
        DRoom("e3", "e3", gx0, gx1, gz0, gz1, 0f, gc, 9f, mLvlWall, mLvlWall, T3, mLvlFloor, mLvlWall, T3, none, true, "SNWE", L3);
        var wi = 0;
        for (var z = gz0 + 2f; z < gz1 - 1f; z += 3f)
        {
            var round = wi++ % 2 == 1;
            if (round)
            {
                DDisc("e3", "porthole frame", new Vector3(gx0 + .003f, 1.5f, z), new Vector3(0f, 0f, -1f), Vector3.up, .38f, mSteel, L3);
                DDisc("e3", "porthole glow", new Vector3(gx0 + .006f, 1.5f, z), new Vector3(0f, 0f, -1f), Vector3.up, .3f, mWin, L3);
            }
            else
            {
                DFace("e3", "window frame", 'W', gx0 + .003f, 0f, 0f, 0f, z - .5f, z + .5f, 1.15f, 1.85f, mSteel, 1f, L3);
                DFace("e3", "window glow", 'W', gx0 + .006f, 0f, 0f, 0f, z - .42f, z + .42f, 1.22f, 1.78f, mWin, 1f, L3);
            }
            DSpot("e3", "underwater", new Vector3(gx0 + .15f, 1.5f, z), Vector3.right, 130f, 70f, 1.8f, 4.5f, new Color(.35f, .85f, .9f), false, L3);
        }
        foreach (var p in new[] { new Vector2(gx1 - .2f, .09f), new Vector2(gx1 - .45f, .06f), new Vector2(gx1 - .65f, .05f) })
            DCyl("e3", "pipe", new Vector3(p.x, gc - .22f, gz0 + .1f), new Vector3(p.x, gc - .22f, gz1 - .1f), p.y, mSteel, L3);

        // E4 (M5): postmodern arch with keystone and pilasters, an oculus, a glass-block wall; the flooded room beyond
        const uint L4 = 16u;
        float px0 = -58f, px1 = -46f, pz0 = 44f, pz1 = 56f, pc = 4.2f;
        var arch = new List<DirDoor> { new DirDoor('N', -53.5f, -50.5f, 3.3f, true) };
        DRoom("e4", "e4", px0, px1, pz0, pz1, LvlFloor, pc, 9f, mLvlWall, mLvlWall, T3, mLvlFloor, mLvlWall, T3, arch, true, "SNWE", L4);
        DRoom("e4", "e4b", -56f, -48f, pz1, pz1 + 7f, LvlFloor, 3.6f, 9f, mLvlWall, mLvlWall, T3, mLvlFloor, mLvlWall, T3, new List<DirDoor> { new DirDoor('S', -53.5f, -50.5f, 3.3f, true) }, true, "SNWE", L4);
        DWater("e4 water", px0, px1, pz0, pz1 + 7f, LvlWater, L4);
        DBox("e4", "keystone", new Vector3(-52.25f, LvlFloor + 3.32f, pz1 - .08f), new Vector3(-51.75f, LvlFloor + 3.9f, pz1), mRose, 1f, L4, "+z");
        foreach (var xp in new[] { -53.85f, -50.15f })
        {
            DBox("e4", "pilaster", new Vector3(xp - .25f, LvlFloor, pz1 - .1f), new Vector3(xp + .25f, LvlFloor + 1.8f, pz1), mTeal, 1f, L4, "+z-y");
            DBox("e4", "capital", new Vector3(xp - .32f, LvlFloor + 1.8f, pz1 - .14f), new Vector3(xp + .32f, LvlFloor + 2.0f, pz1), mRose, 1f, L4, "+z");
        }
        DDisc("e4", "oculus frame", new Vector3(px1 - .003f, 2.9f, 52.5f), new Vector3(0f, 0f, 1f), Vector3.up, .72f, mTeal, L4);
        DDisc("e4", "oculus glow", new Vector3(px1 - .006f, 2.9f, 52.5f), new Vector3(0f, 0f, 1f), Vector3.up, .6f, mGlassBlock, L4);
        DFace("e4", "glass block grout", 'W', px0 + .004f, 0f, 0f, 0f, 51f, 54.4f, LvlFloor + .3f, LvlFloor + 2.9f, mGrout, 1f, L4);
        for (var bz = 51.01f; bz < 54.39f; bz += .2f)
            for (var by = LvlFloor + .31f; by < LvlFloor + 2.89f; by += .2f)
                DFace("e4", "glass block", 'W', px0 + .008f, 0f, 0f, 0f, bz, bz + .18f, by, by + .18f, mGlassBlock, 1f, L4);
        DSpot("e4", "glass block light", new Vector3(px0 + .3f, 1.5f, 52.7f), Vector3.right, 150f, 90f, 2.4f, 7f, LevelWhite, false, L4);
        foreach (var dz in new[] { 1.8f, 3.9f, 6.0f }) DLens("e4", -52f, 3.6f, pz1 + dz, L4, null);
        DLens("e4", -55f, pc, 50f, L4, null);
        DLens("e4", -49f, pc, 50f, L4, null);
        DProbe(px0, px1, pz0, pz1 + 6f, LvlFloor, pc);

        // E5 (M7): leisure-pool geography: an ankle-water deck spills over a weir into a lower basin; a slide tube enters through the wall
        const uint L5 = 32u;
        float qx0 = -46f, qx1 = -34f, qz0 = 44f, qw = 50f, qz1 = 58f, qc = 4.2f, lowFloor = -1.2f, lowWater = -.45f;
        DRoom("e5", "e5", qx0, qx1, qz0, qz1, lowFloor, qc, 9f, mLvlWall, mLvlWall, T3, null, mLvlWall, T3, none, false, "SNWE", L5);
        DQ("e5", "upper deck", new Vector3(qx0, LvlFloor, qz0), new Vector3(qx0, LvlFloor, qw), new Vector3(qx1, LvlFloor, qw), new Vector3(qx1, LvlFloor, qz0), mLvlFloor, T3, L5);
        DWater("e5 water", qx0, qx1, qz0, qw, LvlWater, L5);
        DFace("e5", "weir face", 'S', qx0, qx1, qw, qw, qx0, qx1, lowFloor, LvlFloor, mLvlWall, T3, L5);
        DBox("e5", "weir lip", new Vector3(qx0, LvlFloor - .02f, qw - .25f), new Vector3(qx1, LvlFloor + .02f, qw), mChair, 1f, L5, "-y");
        var sheet = DFace("e5", "water", 'S', qx0, qx1, qw + .02f, qw + .02f, qx0, qx1, lowWater, LvlWater, mWater != null ? mWater : ZLMat("ZL_Water"), 1f, L5);
        sheet.shadowCastingMode = ShadowCastingMode.Off;
        DQ("e5", "lower floor", new Vector3(qx0, lowFloor, qw), new Vector3(qx0, lowFloor, qz1), new Vector3(qx1, lowFloor, qz1), new Vector3(qx1, lowFloor, qw), mLvlFloor, T3, L5);
        DWater("e5 water low", qx0, qx1, qw, qz1, lowWater, L5);
        DCyl("e5", "slide", new Vector3(-37f, 3.2f, qz1 + .3f), new Vector3(-37f, 2.0f, qz1 - 2.2f), .5f, mSlide, L5);
        DCyl("e5", "slide", new Vector3(-37f, 2.0f, qz1 - 2.2f), new Vector3(-38.4f, lowWater + .3f, qz1 - 4.8f), .5f, mSlide, L5);
        DDisc("e5", "slide mouth", new Vector3(-37f, 3.2f, qz1 - .004f), new Vector3(-1f, 0f, 0f), Vector3.up, .6f, mDark, L5);
        foreach (var lp in new[] { new Vector2(-43f, 47f), new Vector2(-37f, 47f), new Vector2(-43f, 54f), new Vector2(-37f, 54f) }) DLens("e5", lp.x, qc, lp.y, L5, null);
        var c5 = DQ("e5", "caustic ceiling", new Vector3(qx0, qc - .008f, qz0), new Vector3(qx1, qc - .008f, qz0), new Vector3(qx1, qc - .008f, qz1), new Vector3(qx0, qc - .008f, qz1), mCausticE, 1f, L5);
        c5.shadowCastingMode = ShadowCastingMode.Off;
        DProbe(qx0, qx1, qz0, qz1, lowFloor, qc);
    }

    static IEnumerator StructureShots()
    {
        var eye = ModuleUnits.PlayerEye;
        var tag = "STRUCTURE greybox (line bathroom, 18_structure_fusion.md) in the fusion look (16)";
        DPalette("E90"); DLevelPalette("white"); DLook("era");
        yield return DirSettle();
        DS("E1_office_ceiling_over_water", tag + " | M1 M2 M7: office ceiling, columns multiplying, beach entry", new Vector3(-49f, .05f + eye, .8f), new Vector3(-49f, 1.1f, 18f));
        DS("E2_fire_stair_into_water", tag + " | M3: the fire stair into dark water, a drowned gate, EXIT", new Vector3(-55f, eye, 24.5f), new Vector3(-55f, -2.0f, 31.5f));
        DS("E3_service_gallery", tag + " | M4: the service gallery lit by observation windows", new Vector3(-44.3f, eye, 24.6f), new Vector3(-44.9f, 1.35f, 39f));
        DS("E4_pomo_arch_glass_block", tag + " | M5: postmodern arch, keystone, oculus, glass block", new Vector3(-52f, LvlFloor + eye, 48.2f), new Vector3(-52f, 1.9f, 56f));
        DS("E5_leisure_weir_slide", tag + " | M7: beach deck, weir into a lower basin, slide mouth", new Vector3(-44.6f, LvlFloor + eye, 48.6f), new Vector3(-38.5f, -.7f, 54.5f));
        DLook("night");
        yield return DirSettle();
        DS("E6_dark_zone", tag + " | the dark zone beyond the gate (F3): wet-niche lamps, a window", new Vector3(-55f, -2.52f + eye, e2Lz1 - .5f), new Vector3(-55f, -1.6f, e2Lz1 + 9f));
        DLook("warm");
    }

    static IEnumerator FusionShots()
    {
        var eye = ModuleUnits.PlayerEye;
        var wet = LvlFloor + eye;
        var bz0 = HallZ + 24f;
        var tag = "FUSION greybox (line bathroom, 16_pool_palette_fusion.md); clone-only ambient, fog, grade and lamp overrides";
        DPalette("W"); DLevelPalette("white"); DLook("warm");
        yield return DirSettle();
        var pr = PlayerRoot;
        Aim(pr.position + Vector3.up * eye, pr.position + pr.forward * 6f + Vector3.up * 1.3f, -1f);
        Shot("L0_reference", tag + " | Level 0 from the same run, the game's own look");

        foreach (var look in new[] { "turq", "era" })
        {
            var pre = look == "turq" ? "F1_" : "F2_";
            DPalette(look == "turq" ? "F1" : "E90"); DLevelPalette("white"); DLook(look); DT1(true, true, true);
            yield return DirSettle();
            DS(pre + "S1_entry", tag + " | " + look + " | club entry", new Vector3(-4.0f, eye, -14.4f), new Vector3(-5.0f, 1.5f, -9.5f));
            DS(pre + "S2_lockers", tag + " | " + look + " | lockers and gang shower", new Vector3(-1.6f, eye, -8.4f), new Vector3(-4.6f, 1.2f, -1.6f));
            DS(pre + "S3_hall", tag + " | " + look + " | pool hall", new Vector3(-6.4f, eye, 1.2f), new Vector3(1.0f, 1.9f, 16f));
            DS(pre + "S4_fardoor", tag + " | " + look + " | far door, the level beyond", new Vector3(0f, eye, 1.6f), new Vector3(0f, 1.3f, HallZ));
            DS(pre + "S5_turn", tag + " | " + look + " | T1 fast", new Vector3(-.3f, wet, HallZ + .7f), new Vector3(1.6f, .9f, HallZ + 5.6f));
            if (look != "turq") continue;
            DS(pre + "S6_chain", tag + " | turq | room chain", new Vector3(.6f, wet, HallZ + 6.7f), new Vector3(0f, 1.0f, bz0));
            DS(pre + "S7_basin", tag + " | turq | basin room", new Vector3(-5.2f, wet, bz0 + .8f), new Vector3(1.0f, -.4f, bz0 + 6.7f));
            DS(pre + "S9_pillars", tag + " | turq | pillar hall", new Vector3(1.2f, wet, bz0 + 13.2f), new Vector3(3.5f, .9f, bz0 + 26f));
            DS(pre + "S10_stair", tag + " | turq | stair into dark water", new Vector3(6.6f, wet, bz0 + 6f), new Vector3(10.6f, -1.3f, bz0 + 6f));
        }

        DPalette("F1"); DLevelPalette("white"); DLook("night");
        yield return DirSettle();
        DS("F3_S3_hall", tag + " | night | pool hall, wet-niche lamps only", new Vector3(-6.4f, eye, 1.2f), new Vector3(1.0f, 1.9f, 16f));
        DS("F3_S7_basin", tag + " | night | basin room, wet-niche lamps only", new Vector3(-5.2f, wet, bz0 + .8f), new Vector3(1.0f, -.4f, bz0 + 6.7f));
        DS("F3_S10_stair", tag + " | night | stair into dark water", new Vector3(6.6f, wet, bz0 + 6f), new Vector3(10.6f, -1.3f, bz0 + 6f));
        DLook("warm");
    }

    // ------------------------------------------------------------------------------------------- the run
    static IEnumerator PoolDirMain()
    {
        poolShallowY = -1.07f; poolDeepY = -1.37f;
        UrpBoxProjection(true);
        BuildPoolRoom();
        BuildOverhead("P1A", false);
        BuildWater("P2A", false);
        SetPoolState(true, true);
        DirMaterials();
        DirHall();
        DirClub();
        DirLevel();
        DPalette("W"); DLevelPalette("white"); DHallTile("24"); DT1(true, true, true); DArch(false); DBasinDark(false);
        if (Opt == "structure")
        {
            DirStructure();
            DirFusionSetup();
            yield return Frames(10);
            yield return StructureShots();
            Stat();
            yield break;
        }
        if (Opt == "fusion")
        {
            DirFusionSetup();
            yield return Frames(10);
            yield return FusionShots();
            Stat();
            yield break;
        }
        yield return Frames(10);
        var eye = ModuleUnits.PlayerEye;
        var wet = LvlFloor + eye;
        var bz0 = HallZ + 24f;
        var tag = "GREYBOX direction still (line bathroom, 40_direction_images.md); pool hall = game visual's STAND-IN";

        foreach (var pal in new[] { "W", "B", "C" })
        {
            DPalette(pal);
            yield return DirSettle();
            Aim(P0 + new Vector3(-4.0f, eye, -14.4f), P0 + new Vector3(-5.0f, 1.5f, -9.5f), -1f);
            Shot("S1_entry_" + pal, tag + " | a: club entry and towel desk | palette " + pal);
            Aim(P0 + new Vector3(-1.6f, eye, -8.4f), P0 + new Vector3(-4.6f, 1.2f, -1.6f), -1f);
            Shot("S2_lockers_" + pal, tag + " | a: lockers and gang shower | palette " + pal);
            Aim(P0 + new Vector3(-6.4f, eye, 1.2f), P0 + new Vector3(1.0f, 1.9f, 16f), -1f);
            Shot("S3_hall_" + pal, tag + " | a: pool hall, tile to 2.4 m | palette " + pal);
        }
        DPalette("B");
        foreach (var tv in new[] { "12", "full" })
        {
            DHallTile(tv);
            yield return DirSettle();
            Aim(P0 + new Vector3(-6.4f, eye, 1.2f), P0 + new Vector3(1.0f, 1.9f, 16f), -1f);
            Shot("S3_hall_B_tile" + tv, tag + " | a: pool hall, tile line " + tv + " | palette B");
        }
        DHallTile("24");

        DT1(true, true, true);
        yield return DirSettle();
        Aim(P0 + new Vector3(0f, eye, 1.6f), P0 + new Vector3(0f, 1.3f, HallZ), -1f);
        Shot("S4_fardoor_changes_at_door", tag + " | c: the far door, the level starts at the door | club palette B");
        DT1(false, false, false);
        yield return DirSettle();
        Shot("S4_fardoor_changes_one_room_later", tag + " | c: the far door, T1 still the club, the level one room later | club palette B");
        DT1(false, true, false);
        yield return DirSettle();
        Aim(P0 + new Vector3(-.3f, wet, HallZ + .7f), P0 + new Vector3(1.6f, .9f, HallZ + 5.6f), -1f);
        Shot("S5_turn_slow", tag + " | c: T1 slow, club walls and light, water over the sill");
        DT1(true, true, true);
        yield return DirSettle();
        Shot("S5_turn_fast", tag + " | c: T1 fast, white tile to the ceiling, the band light, water");

        DPalette("W");
        foreach (var lp in new[] { "white", "blue", "mint" })
        {
            DLevelPalette(lp);
            yield return DirSettle();
            Aim(P0 + new Vector3(.6f, wet, HallZ + 6.7f), P0 + new Vector3(0f, 1.0f, bz0), -1f);
            Shot("S6_chain_" + lp, tag + " | b: room chain, rectangular doors | " + lp);
        }
        DLevelPalette("white");
        DArch(true);
        yield return DirSettle();
        Shot("S6_chain_white_arched", tag + " | b: room chain, round-headed doors (Red's call, 15 section 5)");
        DArch(false);
        DBasinDark(false);
        yield return DirSettle();
        Aim(P0 + new Vector3(-5.2f, wet, bz0 + .8f), P0 + new Vector3(1.0f, -.4f, bz0 + 6.7f), -1f);
        Shot("S7_basin_lit", tag + " | b: basin room, lit");
        DBasinDark(true);
        yield return DirSettle();
        Shot("S7_basin_half_dark", tag + " | b: basin room, the north-east half dark");
        DBasinDark(false);
        yield return DirSettle();
        Aim(P0 + new Vector3(1.8f, wet, bz0 + 11.2f), P0 + new Vector3(0f, 1.1f, HallZ + 6f), -1f);
        Shot("S8_look_back", tag + " | b: looking back toward the club");
        Aim(P0 + new Vector3(1.2f, wet, bz0 + 13.2f), P0 + new Vector3(3.5f, .9f, bz0 + 26f), -1f);
        Shot("S9_pillar_hall", tag + " | b: flooded pillar hall, irregular light, the east third dark");
        Aim(P0 + new Vector3(6.6f, wet, bz0 + 6f), P0 + new Vector3(10.6f, -1.3f, bz0 + 6f), -1f);
        Shot("S10_stair_gate", tag + " | b: stair into dark water, gate below the water line, intake grate");
        Stat();
    }
}
