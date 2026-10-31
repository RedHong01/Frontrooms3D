// FrontRoomsDoorBreakVfx.cs -- door break dust and grit, production P1 (2026-10-30). Replaces the DB2 prototype's round white
// dust cards (moved to W/_partial/door_break_proto_dust_20261030/). Spec: Documentation/research/door_break/54_dust_grit.md
// sec. 3-4 (Red 2026-10-30: "碎沙 ... 更多颗粒 ... 白色的原型雾气看起来不是特别的realistic").
//
// Five layers, one instance per damaged door, render-only, silent:
//   grit    0.1-2 mm grains: CPU-simulated, drawn by FrontRooms/DoorGrit in ONE procedural draw (Graphics.RenderPrimitives,
//           6 vertices per grain, no vertex buffer) from a GraphicsBuffer of 32-byte grains written once per frame in world
//           space; streaked along their screen motion over a 1/120 s exposure, at least 1 px wide with the opacity carrying
//           the true size (phone-wire AA), coloured per grain from palettes sampled from our own door textures;
//   solids  crumbs (2-6 mm, 8 irregular polyhedra of 8-14 tris) and chips (6-50 mm tapered slivers along the grain): three
//           dynamic meshes, one per material (core, stile, the face veneer), on FrontRoomsSurface with a random metric UV
//           offset per piece (no vertex colour); at the settle they go into the door's floor mesh (the visual merges them);
//   haze    <= 64 thin wisps that are never round (an 8-frame filament atlas, eroding into filaments), forward-scattering
//           (HG g 0.6, back gain <= 6), soft against geometry, faded near the camera, and lit by the slot / notch shaft:
//           FrontRooms/DoorHaze, one procedural draw;
//   skin    the floor's dust skin (FrontRooms/DoorDustSkin): a 128 x 100 R8 density at 16 mm texels that the landing grit
//           builds up (carpet absorbs grit under 1 mm; hard floors keep a scatter that is splatted after 2 s at rest);
//   trickle the crumbled core pours sand out of the slot for 1.0 s after D2 and out of the notch for 1.2 s after FINAL.
// Every random number is FrontRoomsGlassFractureMeshes.Rand01(seed, emitter, beat, index) (the GD3c keyed hash): the phone
// tier draws indices 0..N-1 of the same lists, so it is a strict prefix of desktop, and a 3-blow take reshuffles nothing.
// Fixed 1/120 s substeps from the emission time (blow times snapped to the grid), analytic collisions in the door root (the
// floor, the wall slab, the shut leaf until FINAL, then the leaf on the throw curve as a kinematic plane).
// The door root is canonical (+X = the burst side); this root is mirrored by the break's burst sign.
// P3 (2026-10-30, steel; 54 4.3 "Steel leaf"): a hollow-metal leaf makes no wood. Its beats shed enamel flakes (M, D1: 1-6 mm
// flat flakes that flutter, the door's paint palette with the primer entry), kraft honeycomb paper bits out of the opened
// seam (D2) and gypsum crumbs from the frame anchors behind the strike (FINAL); haze at 40 % of the wood counts. The leaf
// has no notch (the debris and grit never pass through it). P4: the frame's joints shed gypsum and settled dust on every blow
// from M (the head casing and, from D2, the latch jamb by the strike): the "head dust".
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class FrontRoomsDoorBreakVfx : IDisposable
{
    public enum Tier { Desktop, Phone }
    public const float Substep = 1f / 120f;
    const float Gravity = -9.81f, WallHalf = .08f, OpenZ0 = 0f, OpenZ1 = .998f, LeafX = .022f, LeafZ = .995f, LeafTop = 2.098f;
    const float PivotZ = .036f;

    /// <summary>Red's open choice (54 4.12 a): the drawn grain size x this beyond 3 m (1.0 = purely physical); 1.0 within 1.2 m.</summary>
    public static float GritVisualScaleFar = 1.5f;
    public static float GritVisualScaleFarPhone = 2.0f;
    public static float GritVisualScaleNearPhone = 1.3f;
    /// <summary>P5 phone tier: the haze's camera fade starts here (desktop 0.25 m) and the shader collapses every wisp wholly
    /// nearer, so a wisp at the lens costs no fill on a phone (54 4.8: phone haze fill cap).</summary>
    public const float PhoneHazeFadeStart = .80f;

    // ---- caps (54 4.3)
    static int Cap(Tier t, int desk, int phone) => t == Tier.Desktop ? desk : phone;
    readonly int gritCap, solidCap, hazeCap;

    // ---- emitter ids (the keyed hash's salt)
    public enum Emitter { HeadSift = 1, SplitBreath = 2, SplitThrough = 3, SlotOpen = 4, SlotTrickle = 5, NotchBurst = 6, NotchTrickle = 7, PivotTear = 8, EdgeShake = 9, FrameJoint = 10, SteelFlakes = 11, SteelSeam = 12, SteelAnchors = 13 }
    // palette ids
    public const int PalCore = 0, PalStile = 1, PalVeneer = 2, PalFrame = 3, PalSteel = 4, PalGypsum = 5, PalSettled = 6, PalGlue = 7, PalCount = 8;
    // solid materials (draws)
    public const int MatCore = 0, MatStile = 1, MatFace = 2, MatCount = 3;

    // ======================================================================================== state
    [StructLayout(LayoutKind.Sequential)]
    public struct GritGpu { public Vector3 pos; public float diam; public Vector3 vel; public uint packed; }   // 32 B
    [StructLayout(LayoutKind.Sequential)]
    public struct HazeGpu { public Vector3 pos; public float size; public Vector3 vel; public uint packed; public Vector4 misc; }   // 48 B

    struct Grain
    {
        public Vector3 p, v;
        public float d, vt, age, life, restClock, fade, flutterA, flutterF, flutterPh;
        public uint col;          // sRGB8 rgb + 8-bit seed
        public byte state;        // 0 flying, 1 resting, 2 absorbing (carpet), 3 dead
    }

    public struct Solid
    {
        public Vector3 p, v, w;
        public Quaternion q;
        public float size, age, restClock;
        public Vector3 stretch;
        public Vector2 uvOff;
        public byte shape, mat, kind;   // kind 0 crumb, 1 chip
        public bool rest;
    }

    struct Wisp
    {
        public Vector3 p, v0, drift;
        public float s0, s1, age, life, tau0, aspect;
        public Vector3 gustV;
        public byte frame, albedo;
        public float rot;
    }

    readonly Grain[] grains;
    int grainCount;
    readonly Solid[] solids;
    int solidCount;
    readonly Wisp[] wisps;
    int wispCount;
    readonly GritGpu[] gritGpu;
    readonly HazeGpu[] hazeGpu;
    GraphicsBuffer gritBuffer, hazeBuffer;
    int gritDrawn, hazeDrawn;

    readonly Transform root;
    readonly float burstSign;
    readonly uint seed;
    public readonly Tier tier;
    readonly bool hardFloor;
    readonly Vector3[] palettes;          // [PalCount * 8], linear
    float clock;                          // seconds since the first emission (the substep clock)
    float acc;
    float finalAt = -1f, breakAt = -1f;
    readonly FrontRoomsDoorBreakVisual.ThrowData throwData;
    // P3/P4: the leaf's material (a steel leaf has no notch) and the frame's (the joints' palette and count)
    readonly bool steel, woodFrame;
    /// <summary>Steel: the Relay's dish on the push face (door root, canonical), from the motif; the seam's peel span (Y).</summary>
    public Vector3 SteelContact = new Vector3(-.022f, 1.08f, .745f);
    public Vector2 SteelSeam = new Vector2(.60f, .855f);
    public bool Steel => steel;

    // trickles: (emitter, start, total desktop, total phone, tau, cut, beat)
    struct Trickle { public Emitter e; public int beat; public float start, tau, cut; public int total, done; public float y0, y1; }
    readonly List<Trickle> trickles = new List<Trickle>();
    struct Pending { public float at; public int beat; public Emitter e; }
    readonly List<Pending> pending = new List<Pending>();

    // the split motif (door root): ys (every 10 mm from 0.50 to 1.70) and zs
    readonly float[] motifYs, motifZs;

    // meshes
    readonly Mesh[] solidMesh = new Mesh[MatCount];
    readonly GameObject[] solidGo = new GameObject[MatCount];
    readonly Material[] solidMat = new Material[MatCount];
    bool solidsDirty;
    readonly List<Vector3> v3 = new List<Vector3>();
    readonly List<Vector3> n3 = new List<Vector3>();
    readonly List<Vector4> t4 = new List<Vector4>();
    readonly List<Vector2> uv = new List<Vector2>();
    readonly List<int> ix = new List<int>();

    // skin
    public const int SkinW = 128, SkinH = 100;
    public const float SkinTexel = .016f, SkinX0 = -.648f, SkinZ0 = -.30f;
    readonly float[] skin = new float[SkinW * SkinH];
    readonly byte[] skinBytes = new byte[SkinW * SkinH];
    Texture2D skinTex;
    GameObject skinGo;
    Material skinMat;
    bool skinDirty;
    float skinUploadClock;
    public bool SkinUsed { get; private set; }

    // grit / haze draws
    Material gritMat, hazeMat;
    readonly MaterialPropertyBlock gritProps = new MaterialPropertyBlock(), hazeProps = new MaterialPropertyBlock();
    static Texture2D grainTex, wispTex;

    // the shaft (54 4.6), set by the visual each frame (world space)
    Vector4 leakC, leakU, leakV, leakN, leakLampPos, leakLampCol;

    // ---- stats (tools)
    public readonly int[] EmittedGrit = new int[8], EmittedSolids = new int[8], EmittedHaze = new int[8];
    public int PeakGrit { get; private set; }
    public int PeakSolids { get; private set; }
    public int PeakHaze { get; private set; }
    public float PeakMainMs { get; private set; }
    public float LastMainMs { get; private set; }
    public float PeakMainAt { get; private set; }
    public string PeakMainNote { get; private set; } = "";
    public int Dropped { get; private set; }
    public int GritLive => grainCount;
    public int SolidLive => solidCount;
    public int HazeLive => wispCount;
    public int Live => grainCount + solidCount + wispCount;
    public float Clock => clock;
    public int SolidsResting { get { var n = 0; for (var i = 0; i < solidCount; i++) if (solids[i].rest) n++; return n; } }
    public bool AllSolidsResting { get { for (var i = 0; i < solidCount; i++) if (!solids[i].rest) return false; return true; } }
    public int Draws { get { var d = 0; if (gritDrawn > 0) d++; if (hazeDrawn > 0) d++; for (var m = 0; m < MatCount; m++) if (solidMesh[m] != null && solidMesh[m].vertexCount > 0) d++; if (SkinUsed) d++; return d; } }
    readonly Stopwatch watch = new Stopwatch();

    // ======================================================================================== set-up

    public FrontRoomsDoorBreakVfx(Transform doorRoot, float burstSign, uint seed, Tier tier, bool hardFloor, Material faceMaterial,
        float[] motifYs, float[] motifZs, float[] paletteLinear, FrontRoomsDoorBreakVisual.ThrowData throwData, bool steel = false, bool woodFrame = true)
    {
        this.steel = steel;
        this.woodFrame = woodFrame;
        this.burstSign = burstSign;
        this.seed = seed;
        this.tier = tier;
        this.hardFloor = hardFloor;
        this.motifYs = motifYs;
        this.motifZs = motifZs;
        this.throwData = throwData;
        gritCap = Cap(tier, 6144, 1536);
        solidCap = Cap(tier, 1200, 400);
        hazeCap = Cap(tier, 64, 24);
        grains = new Grain[gritCap];
        solids = new Solid[solidCap];
        wisps = new Wisp[hazeCap];
        gritGpu = new GritGpu[gritCap];
        hazeGpu = new HazeGpu[hazeCap];
        palettes = new Vector3[PalCount * 8];
        for (var i = 0; i < palettes.Length; i++)
        {
            var k = i * 3;
            palettes[i] = paletteLinear != null && k + 2 < paletteLinear.Length ? new Vector3(paletteLinear[k], paletteLinear[k + 1], paletteLinear[k + 2]) : new Vector3(.4f, .3f, .2f);
        }
        var go = new GameObject("Break VFX");
        root = go.transform;
        root.SetParent(doorRoot, false);
        root.localScale = new Vector3(burstSign, 1f, 1f);
        // steel (P3): the core slot holds the kraft paper bits, the stile slot the frame's gypsum crumbs, the face the enamel flakes
        solidMat[MatCore] = steel ? FrontRoomsSurfaces.TryGet("Door_CoreSteel") ?? FrontRoomsSurfaces.TryGet("Prop_Chipboard") : FrontRoomsSurfaces.TryGet("Door_CoreWood") ?? FrontRoomsSurfaces.TryGet("Prop_Chipboard");
        solidMat[MatStile] = steel ? FrontRoomsSurfaces.TryGet("Door_CoreGypsum") ?? FrontRoomsSurfaces.TryGet("Prop_PlasticWhite") : FrontRoomsSurfaces.TryGet("Door_CoreStile") ?? FrontRoomsSurfaces.TryGet("Prop_Studs");
        solidMat[MatFace] = faceMaterial != null ? faceMaterial : FrontRoomsSurfaces.TryGet(steel ? "Door_Enamel" : "Door_Veneer");
        string[] names = steel ? new[] { "kraft bits", "gypsum crumbs", "enamel flakes" } : new[] { "crumbs core", "crumbs stile", "crumbs face" };
        for (var m = 0; m < MatCount; m++)
        {
            solidMesh[m] = new Mesh { name = "Door break " + names[m], indexFormat = IndexFormat.UInt32 };
            solidMesh[m].MarkDynamic();
            solidGo[m] = Part(names[m], solidMesh[m], solidMat[m]);
        }
        var gs = Shader.Find("FrontRooms/DoorGrit");
        var hs = Shader.Find("FrontRooms/DoorHaze");
        if (grainTex == null) grainTex = Resources.Load<Texture2D>("Surfaces/Textures/Door_Grain_D");
        if (wispTex == null) wispTex = Resources.Load<Texture2D>("Surfaces/Textures/Door_DustWisp_D");
        if (gs != null && gs.isSupported && SystemInfo.supportsComputeShaders)
        {
            gritMat = new Material(gs) { name = "Door break grit" };
            if (grainTex != null) gritMat.SetTexture("_GrainTex", grainTex);
            gritBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, gritCap, 32) { name = "Door break grit" };
        }
        if (hs != null && hs.isSupported && SystemInfo.supportsComputeShaders)
        {
            hazeMat = new Material(hs) { name = "Door break haze" };
            if (wispTex != null) hazeMat.SetTexture("_WispTex", wispTex);
            hazeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, hazeCap, 48) { name = "Door break haze" };
        }
        RenderPipelineManager.beginCameraRendering += OnCamera;
    }

    GameObject Part(string name, Mesh mesh, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.rayTracingMode = UnityEngine.Experimental.Rendering.RayTracingMode.Off;
        return go;
    }

    // ======================================================================================== keyed random
    float R(Emitter e, int beat, int i, int j) => (float)FrontRoomsGlassFractureMeshes.Rand01(seed, (int)e * 128 + j, beat, i);
    float R(Emitter e, int beat, int i, int j, float a, float b) => a + (b - a) * R(e, beat, i, j);

    float MotifZ(float y)
    {
        if (motifYs == null || motifYs.Length < 2) return .919f;
        var k = (y - motifYs[0]) / (motifYs[1] - motifYs[0]);
        var i = Mathf.Clamp(Mathf.FloorToInt(k), 0, motifYs.Length - 2);
        var t = Mathf.Clamp01(k - i);
        return Mathf.Lerp(motifZs[i], motifZs[i + 1], t);
    }

    // count for this tier: the desktop count, or the phone prefix
    int N(int desk, int phone) => tier == Tier.Desktop ? desk : phone;

    // the wood terminal speed (54 2.3, m/s) for a diameter in metres; gypsum falls 1.6x faster, settled lint 0.8x
    static readonly float[] VtD = { .00005f, .0001f, .0002f, .0003f, .0005f, .001f, .002f, .005f };
    static readonly float[] VtV = { .03f, .11f, .34f, .60f, 1.10f, 2.2f, 3.0f, 4.0f };
    static float TerminalSpeed(float d, int pal)
    {
        var v = VtV[VtV.Length - 1];
        for (var i = 1; i < VtD.Length; i++)
            if (d <= VtD[i]) { v = Mathf.Lerp(VtV[i - 1], VtV[i], Mathf.InverseLerp(VtD[i - 1], VtD[i], d)); break; }
        if (d < VtD[0]) v = VtV[0];
        return pal == PalGypsum ? v * 1.6f : pal == PalSettled ? v * .8f : v;
    }

    // ======================================================================================== the leaf on the throw curve
    // degrees toward +X at t seconds after FINAL (the map's throw: 0 -> 105 deg in 0.12 s from 0.1 s, then 0.18 s to 80)
    float LeafDeg(float tAfterFinal)
    {
        float brk = .10f, ts = .12f, bs = .18f, deg = 105f, rest = 80f;
        if (throwData != null) { brk = throwData.breakAt; if (throwData.@throw != null) { ts = throwData.@throw.seconds; bs = throwData.@throw.bounceSeconds; deg = throwData.@throw.deg; rest = throwData.@throw.restDeg; } }
        if (tAfterFinal <= brk) return 0f;
        var u = tAfterFinal - brk;
        if (u <= ts) return deg * u / ts;
        var v = Mathf.Min(1f, (u - ts) / bs);
        return deg + (rest - deg) * (1f - (1f - v) * (1f - v));
    }

    float LeafDegNow => finalAt < 0f ? 0f : LeafDeg(clock - finalAt);

    // a point of the shut leaf (door root, canonical) at the leaf's throw angle now
    Vector3 OnLeaf(Vector3 p) => OnLeaf(p, LeafDegNow);
    static Vector3 OnLeaf(Vector3 p, float deg)
    {
        if (deg <= 0f) return p;
        var a = deg * Mathf.Deg2Rad;
        var along = p.z - PivotZ;
        var across = p.x;
        // leaf direction from the pivot (sin a, cos a) in (x, z); its normal toward +X (cos a, -sin a)
        return new Vector3(along * Mathf.Sin(a) + across * Mathf.Cos(a), p.y, PivotZ + along * Mathf.Cos(a) - across * Mathf.Sin(a));
    }
    static Vector3 LeafDir(Vector3 d, float deg)
    {
        if (deg <= 0f) return d;
        var a = deg * Mathf.Deg2Rad;
        return new Vector3(d.z * Mathf.Sin(a) + d.x * Mathf.Cos(a), d.y, d.z * Mathf.Cos(a) - d.x * Mathf.Sin(a));
    }

    // ======================================================================================== emitters (54 4.3)

    /// <summary>A blow: the head sift (every blow; the ledge empties: 0.6^k) and the stage's own emitters. Stage 0 E,
    /// 1 M, 2 D1, 3 D2, 4 FINAL. struckBefore = blows the door had taken before this one (kept damage counts too).
    /// newStage: this blow showed a stage the door had not shown before (a kept stage never re-emits its split dust).</summary>
    public void Blow(int stage, int struckBefore, bool newStage)
    {
        var beat = Mathf.Clamp(stage, 0, 4);
        HeadSift(beat, struckBefore, stage);
        if (beat >= 1) FrameJoint(beat, struckBefore);
        if (!newStage) return;
        if (steel) { SteelBeat(beat); return; }
        switch (beat)
        {
            case 1: Split(Emitter.SplitBreath, beat, .85f, 1.15f, N(300, 75), N(24, 8), N(10, 3), N(4, 2), .5f, 1.5f, 0f, .08f); break;
            case 2: Split(Emitter.SplitThrough, beat, .78f, 1.24f, N(700, 175), N(60, 20), N(30, 10), N(6, 3), .8f, 2.0f, .15f, .08f); break;
            case 3:
                Split(Emitter.SlotOpen, beat, .62f, 1.42f, N(600, 150), N(120, 40), N(30, 10), N(8, 3), 1.0f, 2.5f, .1f, .08f);
                trickles.Add(new Trickle { e = Emitter.SlotTrickle, beat = beat, start = clock, tau = .35f, cut = 1.0f, total = N(300, 75), y0 = .64f, y1 = 1.0f });
                break;
            case 4:
                finalAt = clock;
                Notch(beat);
                trickles.Add(new Trickle { e = Emitter.NotchTrickle, beat = beat, start = clock, tau = .35f, cut = 1.2f, total = N(500, 125), y0 = .74f, y1 = 1.20f });
                break;
        }
    }

    /// <summary>The map's BreakDoor (0.1 s after FINAL): the top pivot tears out of the head (pose a) and the leaf's gust
    /// starts; the edges shake when the leaf hits its stop (ThrowSeconds later).</summary>
    public void Break()
    {
        if (breakAt >= 0f) return;
        breakAt = clock;
        if (finalAt < 0f) finalAt = clock - .10f;
        var beat = 5;
        // top pivot tear: screws and the pivot arm rip out of the head, gypsum and frame wood, a downward curtain
        var nG = N(500, 125);
        for (var i = 0; i < nG; i++)
        {
            var p = new Vector3(R(Emitter.PivotTear, beat, i, 0, -.03f, .06f), R(Emitter.PivotTear, beat, i, 1, 2.08f, 2.12f), R(Emitter.PivotTear, beat, i, 2, 0f, .15f));
            var v = new Vector3(R(Emitter.PivotTear, beat, i, 3, -.1f, .25f), -R(Emitter.PivotTear, beat, i, 4, .2f, 1.0f), R(Emitter.PivotTear, beat, i, 5, -.1f, .15f));
            var r = R(Emitter.PivotTear, beat, i, 6);
            AddGrain(Emitter.PivotTear, beat, i, p, v, r < .45f ? PalGypsum : r < .70f ? PalFrame : PalSettled);
        }
        var nS = N(40, 13);
        for (var i = 0; i < nS; i++)
        {
            var p = new Vector3(R(Emitter.PivotTear, beat, i, 10, -.02f, .05f), R(Emitter.PivotTear, beat, i, 11, 2.06f, 2.11f), R(Emitter.PivotTear, beat, i, 12, .0f, .12f));
            var v = new Vector3(R(Emitter.PivotTear, beat, i, 13, -.2f, .5f), -R(Emitter.PivotTear, beat, i, 14, .1f, .6f), R(Emitter.PivotTear, beat, i, 15, -.2f, .3f));
            AddSolid(Emitter.PivotTear, beat, i, p, v, 0, steel || R(Emitter.PivotTear, beat, i, 16) < .5f ? MatStile : MatCore);
        }
        var nH = N(6, 2);
        for (var i = 0; i < nH; i++)
            AddWisp(Emitter.PivotTear, beat, i, new Vector3(R(Emitter.PivotTear, beat, i, 20, -.04f, .1f), R(Emitter.PivotTear, beat, i, 21, 1.95f, 2.10f), R(Emitter.PivotTear, beat, i, 22, .0f, .2f)),
                new Vector3(R(Emitter.PivotTear, beat, i, 23, -.05f, .15f), -R(Emitter.PivotTear, beat, i, 24, .1f, .4f), 0f), .10f, .40f, .08f, 3f, 1.6f, 2.5f, 2);
        EmittedGrit[5] += nG; EmittedSolids[5] += nS; EmittedHaze[5] += nH;
        pending.Add(new Pending { at = clock + (throwData != null && throwData.@throw != null ? throwData.@throw.seconds : .12f), beat = 6, e = Emitter.EdgeShake });
    }

    void HeadSift(int beat, int struckBefore, int stage)
    {
        var desk = Mathf.RoundToInt(260f * Mathf.Pow(.6f, Mathf.Max(0, struckBefore)));
        var n = N(desk, Mathf.RoundToInt(desk * .25f));
        for (var i = 0; i < n; i++)
        {
            var burstSide = R(Emitter.HeadSift, struckBefore, i, 0) < .65f;
            var sx = burstSide ? 1f : -1f;
            var p = new Vector3(sx * R(Emitter.HeadSift, struckBefore, i, 1, LeafX, LeafX + .01f), R(Emitter.HeadSift, struckBefore, i, 2, LeafTop - .002f, LeafTop + .004f), R(Emitter.HeadSift, struckBefore, i, 3, .05f, .95f));
            var v = new Vector3(sx * R(Emitter.HeadSift, struckBefore, i, 4, .05f, .3f), R(Emitter.HeadSift, struckBefore, i, 5, -.05f, .05f), R(Emitter.HeadSift, struckBefore, i, 6, -.04f, .04f));
            var r = R(Emitter.HeadSift, struckBefore, i, 7);
            var pal = r < .70f ? PalSettled : r < .85f ? (steel ? PalSettled : PalCore) : (stage >= 2 ? PalGypsum : PalSettled);
            AddGrain(Emitter.HeadSift, struckBefore, i, OnLeaf(p), v, pal);
        }
        var nh = struckBefore == 0 ? N(3, 1) : N(2, 1);
        for (var i = 0; i < nh; i++)
        {
            var sx = R(Emitter.HeadSift, struckBefore, i, 30) < .65f ? 1f : -1f;
            var p = new Vector3(sx * R(Emitter.HeadSift, struckBefore, i, 31, .03f, .08f), R(Emitter.HeadSift, struckBefore, i, 32, 1.92f, 2.06f), R(Emitter.HeadSift, struckBefore, i, 33, .1f, .9f));
            AddWisp(Emitter.HeadSift, struckBefore, i, OnLeaf(p), new Vector3(sx * .06f, -.18f, 0f), .06f, .25f, .05f, 3f, 1.2f, 2.2f, 1);
        }
        if (beat >= 0 && beat < EmittedGrit.Length) { EmittedGrit[beat] += n; EmittedHaze[beat] += nh; }
    }

    // split emitters: grains, crumbs and chips along the active segment of the motif polyline, on the burst face (10 % on
    // the push face: veneer crushed at the Relay's contact)
    void Split(Emitter e, int beat, float y0, float y1, int nGrit, int nCrumb, int nChip, int nHaze, float vMin, float vMax, float dropShare, float tau0)
    {
        for (var i = 0; i < nGrit; i++)
        {
            var y = R(e, beat, i, 0, y0, y1);
            var push = R(e, beat, i, 1) < .10f;
            var sx = push ? -1f : 1f;
            var p = new Vector3(sx * (LeafX + .001f), y, MotifZ(y) + R(e, beat, i, 2, -.004f, .004f));
            Vector3 v;
            if (R(e, beat, i, 3) < dropShare) v = new Vector3(sx * R(e, beat, i, 4, 0f, .2f), R(e, beat, i, 5, -.05f, .05f), R(e, beat, i, 6, -.05f, .05f));
            else v = Fan(e, beat, i, sx, vMin, vMax, 35f);
            var r = R(e, beat, i, 9);
            var pal = r < .005f ? PalGlue : r < .705f ? PalCore : r < .905f ? PalStile : PalVeneer;
            if (e == Emitter.SlotOpen && R(e, beat, i, 10) < .10f) pal = PalCore;   // the petal's underside (chipboard)
            AddGrain(e, beat, i, p, v, pal);
        }
        for (var i = 0; i < nCrumb; i++)
        {
            var y = R(e, beat, i, 20, y0, y1);
            var sx = R(e, beat, i, 21) < .10f ? -1f : 1f;
            var p = new Vector3(sx * (LeafX + .002f), y, MotifZ(y) + R(e, beat, i, 22, -.004f, .006f));
            var v = Fan(e, beat, i + 7919, sx, vMin * .6f, vMax * .7f, 40f);
            var r = R(e, beat, i, 23);
            AddSolid(e, beat, i, p, v, 0, r < .70f ? MatCore : r < .90f ? MatStile : MatFace);
        }
        for (var i = 0; i < nChip; i++)
        {
            var y = R(e, beat, i, 30, y0, y1);
            var p = new Vector3(LeafX + .002f, y, MotifZ(y) + R(e, beat, i, 31, -.002f, .004f));
            var v = Fan(e, beat, i + 15013, 1f, vMin, vMax * 1.1f, 30f);
            AddSolid(e, beat, i + 1000, p, v, 1, R(e, beat, i, 32) < 1f / 6f ? MatFace : MatStile);
        }
        for (var i = 0; i < nHaze; i++)
        {
            var y = Mathf.Lerp(y0, y1, (i + R(e, beat, i, 40)) / Mathf.Max(1, nHaze));
            var p = new Vector3(LeafX + .02f, y, MotifZ(y));
            var v = new Vector3(R(e, beat, i, 41, .4f, 1.0f), R(e, beat, i, 42, -.05f, .1f), R(e, beat, i, 43, -.15f, .15f));
            AddWisp(e, beat, i, p, v, .03f, .15f, tau0, 2f, 1.0f, 1.8f, 0);
        }
        if (beat < EmittedGrit.Length) { EmittedGrit[beat] += nGrit; EmittedSolids[beat] += nCrumb + nChip; EmittedHaze[beat] += nHaze; }
    }

    Vector3 Fan(Emitter e, int beat, int i, float sx, float vMin, float vMax, float coneDeg)
    {
        var s = R(e, beat, i, 50, vMin, vMax);
        var a = R(e, beat, i, 51, 0f, Mathf.PI * 2f);
        var c = Mathf.Tan(coneDeg * Mathf.Deg2Rad) * Mathf.Sqrt(R(e, beat, i, 52));
        var dir = new Vector3(sx, c * Mathf.Sin(a), c * Mathf.Cos(a)).normalized;
        return dir * s;
    }

    void Notch(int beat)
    {
        var e = Emitter.NotchBurst;
        float za = .905f, zb = LeafZ, ya = .72f, yb = 1.38f;
        var per = 2f * ((zb - za) + (yb - ya));
        int nG = N(1800, 450), nS = N(300, 100), nC = N(120, 40), nH = N(14, 6);
        for (var i = 0; i < nG; i++)
        {
            var p = OnOutline(R(e, beat, i, 0) * per, za, zb, ya, yb);
            p.x = R(e, beat, i, 1, -LeafX, LeafX + .004f);
            var v = Fan(e, beat, i, 1f, 1.5f, 4f, 40f);
            v.z += R(e, beat, i, 2, -.3f, .6f);
            var r = R(e, beat, i, 3);
            AddGrain(e, beat, i, p, v, r < .55f ? PalCore : r < .85f ? PalStile : r < .95f ? PalVeneer : PalFrame);
        }
        for (var i = 0; i < nS; i++)
        {
            var p = OnOutline(R(e, beat, i, 10) * per, za, zb, ya, yb);
            p.x = R(e, beat, i, 11, -LeafX, LeafX);
            var v = Fan(e, beat, i + 7919, 1f, .8f, 2.6f, 45f);
            var r = R(e, beat, i, 12);
            AddSolid(e, beat, i, p, v, 0, r < .60f ? MatCore : r < .90f ? MatStile : MatFace);
        }
        for (var i = 0; i < nC; i++)
        {
            var p = OnOutline(R(e, beat, i, 20) * per, za, zb, ya, yb);
            p.x = R(e, beat, i, 21, 0f, LeafX + .002f);
            var v = Fan(e, beat, i + 15013, 1f, 1.5f, 4.2f, 35f);
            AddSolid(e, beat, i + 1000, p, v, 1, R(e, beat, i, 22) < 1f / 6f ? MatFace : MatStile);
        }
        for (var i = 0; i < nH; i++)
        {
            var y = Mathf.Lerp(ya, yb, (i + R(e, beat, i, 30)) / nH);
            var p = new Vector3(R(e, beat, i, 31, .02f, .07f), y, R(e, beat, i, 32, .90f, 1.0f));
            var v = new Vector3(R(e, beat, i, 33, .6f, 1.4f), R(e, beat, i, 34, -.05f, .15f), R(e, beat, i, 35, -.1f, .3f));
            AddWisp(e, beat, i, p, v, .08f, .35f, .12f, 1.6f, 1.6f, 2.6f, 0);
        }
        EmittedGrit[beat] += nG; EmittedSolids[beat] += nS + nC; EmittedHaze[beat] += nH;
    }

    static Vector3 OnOutline(float s, float za, float zb, float ya, float yb)
    {
        var h = yb - ya;
        var w = zb - za;
        if (s < h) return new Vector3(0f, ya + s, za);
        s -= h;
        if (s < w) return new Vector3(0f, yb, za + s);
        s -= w;
        if (s < h) return new Vector3(0f, yb - s, zb);
        s -= h;
        return new Vector3(0f, ya, zb - Mathf.Min(s, w));
    }

    void EdgeShake(int beat)
    {
        var e = Emitter.EdgeShake;
        var deg = LeafDegNow;
        var n = N(300, 75);
        for (var i = 0; i < n; i++)
        {
            var top = R(e, beat, i, 0) < .5f;
            var p = top ? new Vector3(R(e, beat, i, 1, -LeafX, LeafX), LeafTop, R(e, beat, i, 2, .05f, .95f)) : new Vector3(R(e, beat, i, 1, -LeafX, LeafX), R(e, beat, i, 3, .2f, 2.0f), LeafZ);
            var v = LeafDir(new Vector3(R(e, beat, i, 4, -.5f, .5f), R(e, beat, i, 5, -.1f, .2f), top ? 0f : R(e, beat, i, 6, .1f, .5f)), deg);
            var r = R(e, beat, i, 7);
            AddGrain(e, beat, i, OnLeaf(p, deg), v, r < .70f ? PalSettled : r < .85f ? PalCore : PalGypsum);
        }
        var nh = N(2, 1);
        for (var i = 0; i < nh; i++)
            AddWisp(e, beat, i, OnLeaf(new Vector3(.04f, R(e, beat, i, 10, 1.4f, 2.0f), .9f), deg), Vector3.zero, .1f, .3f, .05f, 2f, 1.2f, 2.0f, 1);
        EmittedGrit[5] += n; EmittedHaze[5] += nh;
    }

    // ---- P4: the frame's joints (the "head dust"): every blow from M shakes gypsum and settled dust out of the head casing's
    // joint with the wall and, from D2, out of the latch jamb's joint by the strike. Keyed by the blow index (a kept stage that
    // blows again sheds new grains). A steel sleeve sheds 60 % of a wood casing's.
    static readonly int[] FrameJointDesk = { 0, 40, 70, 110, 180 };
    void FrameJoint(int beat, int blow)
    {
        var e = Emitter.FrameJoint;
        var k = woodFrame ? 1f : .6f;
        var desk = Mathf.RoundToInt(FrameJointDesk[Mathf.Clamp(beat, 0, 4)] * k);
        var n = N(desk, Mathf.RoundToInt(desk * .25f));
        for (var i = 0; i < n; i++)
        {
            var jamb = beat >= 3 && R(e, blow, i, 0) < .30f;
            Vector3 p, v;
            if (jamb)
            {
                p = new Vector3(R(e, blow, i, 1, .095f, .112f), R(e, blow, i, 2, .85f, 1.15f), R(e, blow, i, 3, 1.0f, 1.07f));
                v = new Vector3(R(e, blow, i, 4, 0f, .15f), R(e, blow, i, 5, -.05f, .02f), R(e, blow, i, 6, -.05f, .03f));
            }
            else
            {
                var sx = R(e, blow, i, 7) < .70f ? 1f : -1f;
                p = new Vector3(sx * R(e, blow, i, 1, .095f, .112f), R(e, blow, i, 2, 2.10f, 2.17f), R(e, blow, i, 3, .02f, .98f));
                v = new Vector3(sx * R(e, blow, i, 4, 0f, .15f), R(e, blow, i, 5, -.05f, .02f), R(e, blow, i, 6, -.03f, .03f));
            }
            var r = R(e, blow, i, 8);
            var pal = r < .55f ? PalGypsum : r < (woodFrame ? .85f : 1f) ? PalSettled : PalFrame;
            AddGrain(e, blow, i, p, v, pal);
        }
        var nh = beat >= 3 ? N(1, 1) : 0;
        for (var i = 0; i < nh; i++)
            AddWisp(e, blow, i, new Vector3(R(e, blow, i, 20, .10f, .14f), R(e, blow, i, 21, 2.02f, 2.12f), R(e, blow, i, 22, .1f, .9f)), new Vector3(.04f, -.12f, 0f), .06f, .22f, .04f, 3f, 1.2f, 2.0f, 2);
        if (beat < EmittedGrit.Length) { EmittedGrit[beat] += n; EmittedHaze[beat] += nh; }
    }

    // ---- P3: the steel leaf's beats (54 4.3 "Steel leaf"); haze at 40 % of the wood counts
    void SteelBeat(int beat)
    {
        switch (beat)
        {
            case 1: Flakes(beat, N(40, 13), N(200, 50), .60f, N(2, 1)); break;            // M: the dish crazes
            case 2: Flakes(beat, N(80, 27), N(300, 75), .45f, N(2, 1)); break;            // D1: the bow crazes the lock edge
            case 3:                                                                       // D2: the edge seam opens
                Flakes(beat, N(40, 13), N(120, 30), .30f, 0);
                Seam(beat, N(60, 20), N(200, 50), N(3, 1));
                break;
            case 4:                                                                       // FINAL: the latch tears through
                finalAt = clock;
                Flakes(beat, N(40, 13), N(100, 25), .20f, 0);
                Anchors(beat, N(60, 20), N(800, 200), N(6, 3));
                break;
        }
    }

    // enamel flakes off the push face's dish (pushShare) or the burst face's lock edge, with fine paint and primer grit
    void Flakes(int beat, int nFlake, int nGrit, float pushShare, int nHaze)
    {
        var e = Emitter.SteelFlakes;
        for (var i = 0; i < nFlake + nGrit; i++)
        {
            var push = R(e, beat, i, 0) < pushShare;
            Vector3 p, v;
            if (push)
            {
                p = new Vector3(-(LeafX + .0015f), SteelContact.y + R(e, beat, i, 1, -.07f, .07f), SteelContact.z + R(e, beat, i, 2, -.07f, .07f));
                v = new Vector3(-R(e, beat, i, 3, .08f, .45f), R(e, beat, i, 4, -.10f, .20f), R(e, beat, i, 5, -.15f, .15f));
            }
            else
            {
                p = new Vector3(LeafX + .0015f, R(e, beat, i, 1, .85f, 1.15f), R(e, beat, i, 2, .93f, .99f));
                v = new Vector3(R(e, beat, i, 3, .10f, .60f), R(e, beat, i, 4, -.10f, .30f), R(e, beat, i, 5, -.20f, .20f));
            }
            if (i < nFlake)
            {
                AddSolid(e, beat, i, p, v, 0, MatFace);
                if (solidCount > 0)
                {
                    // a flat flake of paint, 1-6 mm, a few tenths of a millimetre thick: it flutters (stretch.y < 0.3)
                    var s = solids[solidCount - 1];
                    s.size = R(e, beat, i, 6, .001f, .006f);
                    s.stretch = new Vector3(R(e, beat, i, 7, 1.0f, 1.4f), .07f, R(e, beat, i, 8, .6f, 1.0f));
                    solids[solidCount - 1] = s;
                }
            }
            else
            {
                var r = R(e, beat, i, 9);
                AddGrain(e, beat, i, p, v, r < .70f ? PalVeneer : PalSettled);
            }
        }
        for (var i = 0; i < nHaze; i++)
        {
            var push = i % 2 == 0 && pushShare > .4f;
            var p = push ? new Vector3(-(LeafX + .03f), SteelContact.y, SteelContact.z) : new Vector3(LeafX + .03f, R(e, beat, i, 40, .9f, 1.1f), .96f);
            AddWisp(e, beat, i, p, new Vector3(push ? -.25f : .25f, -.05f, 0f), .04f, .14f, .05f, 2f, .9f, 1.5f, 1);
        }
        if (beat < EmittedGrit.Length) { EmittedGrit[beat] += nGrit; EmittedSolids[beat] += nFlake; EmittedHaze[beat] += nHaze; }
    }

    // the opened edge seam: kraft honeycomb paper bits and paper grit out of the latch end, under the armor front
    void Seam(int beat, int nBits, int nGrit, int nHaze)
    {
        var e = Emitter.SteelSeam;
        for (var i = 0; i < nBits + nGrit; i++)
        {
            var p = new Vector3(R(e, beat, i, 0, .0f, .02f), R(e, beat, i, 1, SteelSeam.x, SteelSeam.y), R(e, beat, i, 2, .985f, .996f));
            var v = new Vector3(R(e, beat, i, 3, .10f, .55f), R(e, beat, i, 4, -.20f, .20f), R(e, beat, i, 5, .0f, .30f));
            if (i < nBits)
            {
                AddSolid(e, beat, i, p, v, 0, MatCore);
                if (solidCount > 0)
                {
                    var s = solids[solidCount - 1];
                    s.size = R(e, beat, i, 6, .001f, .004f);
                    s.stretch = new Vector3(R(e, beat, i, 7, 1.0f, 1.6f), .10f, R(e, beat, i, 8, .4f, .8f));
                    solids[solidCount - 1] = s;
                }
            }
            else AddGrain(e, beat, i, p, v, R(e, beat, i, 9) < .70f ? PalCore : PalVeneer);
        }
        for (var i = 0; i < nHaze; i++)
        {
            var y = Mathf.Lerp(SteelSeam.x, SteelSeam.y, (i + R(e, beat, i, 40)) / Mathf.Max(1, nHaze));
            AddWisp(e, beat, i, new Vector3(.03f, y, .99f), new Vector3(R(e, beat, i, 41, .3f, .7f), -.02f, R(e, beat, i, 42, .0f, .2f)), .03f, .12f, .06f, 2f, 1.0f, 1.6f, 0);
        }
        if (beat < EmittedGrit.Length) { EmittedGrit[beat] += nGrit; EmittedSolids[beat] += nBits; EmittedHaze[beat] += nHaze; }
    }

    // FINAL: the latch tears through the strike; the frame's anchors behind it give up gypsum crumbs and a burst of grit
    void Anchors(int beat, int nCrumb, int nGrit, int nHaze)
    {
        var e = Emitter.SteelAnchors;
        for (var i = 0; i < nCrumb + nGrit; i++)
        {
            var p = new Vector3(R(e, beat, i, 0, .0f, .06f), R(e, beat, i, 1, .85f, 1.10f), R(e, beat, i, 2, .995f, 1.0f));
            var v = Fan(e, beat, i, 1f, .5f, 2.0f, 40f);
            v.z -= R(e, beat, i, 3, .0f, .6f);
            if (i < nCrumb) AddSolid(e, beat, i, p, v, 0, MatStile);
            else
            {
                var r = R(e, beat, i, 4);
                AddGrain(e, beat, i, p, v, r < .60f ? PalGypsum : r < .85f ? PalVeneer : PalSettled);
            }
        }
        for (var i = 0; i < nHaze; i++)
        {
            var y = Mathf.Lerp(.82f, 1.14f, (i + R(e, beat, i, 30)) / Mathf.Max(1, nHaze));
            AddWisp(e, beat, i, new Vector3(R(e, beat, i, 31, .02f, .06f), y, R(e, beat, i, 32, .95f, 1.0f)), new Vector3(R(e, beat, i, 33, .5f, 1.1f), R(e, beat, i, 34, -.05f, .1f), R(e, beat, i, 35, -.2f, .1f)), .06f, .28f, .10f, 1.6f, 1.4f, 2.4f, 2);
        }
        if (beat < EmittedGrit.Length) { EmittedGrit[beat] += nGrit; EmittedSolids[beat] += nCrumb; EmittedHaze[beat] += nHaze; }
    }

    void TrickleStep(ref Trickle tr)
    {
        var t = Mathf.Min(clock - tr.start, tr.cut);
        var norm = 1f - Mathf.Exp(-tr.cut / tr.tau);
        var want = Mathf.Min(tr.total, Mathf.FloorToInt(tr.total * (1f - Mathf.Exp(-t / tr.tau)) / norm + 1e-4f));
        var deg = LeafDegNow;
        var notch = tr.e == Emitter.NotchTrickle;
        for (var i = tr.done; i < want; i++)
        {
            // the slot's lower half / the notch's torn core face, a stream 15-40 mm wide that leaves at under 0.15 m/s
            var y = R(tr.e, tr.beat, i, 0, tr.y0, tr.y1);
            var z = notch ? R(tr.e, tr.beat, i, 1, .885f, .905f) : MotifZ(y) + R(tr.e, tr.beat, i, 1, -.004f, .004f);
            var x = notch ? R(tr.e, tr.beat, i, 2, -.015f, .02f) : R(tr.e, tr.beat, i, 2, .008f, LeafX + .002f);
            var p = OnLeaf(new Vector3(x, y, z), deg);
            var v = LeafDir(new Vector3(R(tr.e, tr.beat, i, 3, 0f, .12f), R(tr.e, tr.beat, i, 4, -.08f, 0f), R(tr.e, tr.beat, i, 5, -.03f, .03f)), deg);
            var pal = R(tr.e, tr.beat, i, 6) < (notch ? .85f : .80f) ? PalCore : PalStile;
            AddGrain(tr.e, tr.beat, i, p, v, pal);
        }
        if (want > tr.done) EmittedGrit[tr.beat] += want - tr.done;
        tr.done = want;
    }

    // ======================================================================================== adders

    void AddGrain(Emitter e, int beat, int i, Vector3 p, Vector3 v, int pal)
    {
        // 45 % fine sift (0.1-0.3 mm), 55 % grit (0.3-2 mm), log-uniform
        var fine = R(e, beat, i, 60) < .45f;
        var u = R(e, beat, i, 61);
        var d = fine ? .0001f * Mathf.Pow(3f, u) : .0003f * Mathf.Pow(2f / .3f, u);
        int slot;
        if (grainCount < gritCap) slot = grainCount++;
        else
        {
            // overflow: the oldest grain of the finest class goes first (deterministic: the scan order is fixed)
            slot = 0;
            var best = float.MaxValue;
            for (var k = 0; k < grainCount; k++)
            {
                var score = grains[k].d * 1e4f - grains[k].age * .1f;
                if (score < best) { best = score; slot = k; }
            }
            Dropped++;
        }
        var c = palettes[pal * 8 + Mathf.Clamp((int)(R(e, beat, i, 62) * 8f), 0, 7)];
        var jitter = 1f + (R(e, beat, i, 63) - .5f) * .24f;
        if (d <= .0005f) jitter *= 1.15f;       // fines are lighter than the solid
        c *= jitter;
        var vt = TerminalSpeed(d, pal);
        grains[slot] = new Grain
        {
            p = p, v = v, d = d, vt = vt, age = 0f, life = 7f, state = 0,
            col = Pack(c, (byte)(R(e, beat, i, 64) * 255f)),
            flutterA = d >= .001f ? R(e, beat, i, 65, .05f, .15f) : 0f,
            flutterF = R(e, beat, i, 66, 3f, 6f),
            flutterPh = R(e, beat, i, 67, 0f, 6.283f),
        };
    }

    static uint Pack(Vector3 lin, byte s)
    {
        uint r = (uint)Mathf.Clamp(Mathf.RoundToInt(Mathf.LinearToGammaSpace(Mathf.Clamp01(lin.x)) * 255f), 0, 255);
        uint g = (uint)Mathf.Clamp(Mathf.RoundToInt(Mathf.LinearToGammaSpace(Mathf.Clamp01(lin.y)) * 255f), 0, 255);
        uint b = (uint)Mathf.Clamp(Mathf.RoundToInt(Mathf.LinearToGammaSpace(Mathf.Clamp01(lin.z)) * 255f), 0, 255);
        return r | (g << 8) | (b << 16) | ((uint)s << 24);
    }

    void AddSolid(Emitter e, int beat, int i, Vector3 p, Vector3 v, byte kind, int mat)
    {
        if (solidCount >= solidCap) { Dropped++; return; }
        var s = new Solid
        {
            p = p, v = v, kind = kind, mat = (byte)mat,
            w = new Vector3(R(e, beat, i, 70, -25f, 25f), R(e, beat, i, 71, -25f, 25f), R(e, beat, i, 72, -25f, 25f)),
            q = Quaternion.Euler(R(e, beat, i, 73, 0f, 360f), R(e, beat, i, 74, 0f, 360f), R(e, beat, i, 75, 0f, 360f)),
            shape = (byte)Mathf.Clamp((int)(R(e, beat, i, 76) * 8f), 0, 7),
            uvOff = new Vector2(R(e, beat, i, 77, 0f, 1f), R(e, beat, i, 78, 0f, 1f)),
        };
        if (kind == 0)
        {
            s.size = R(e, beat, i, 79, .002f, .006f);
            var flake = R(e, beat, i, 80) < .4f;
            s.stretch = flake ? new Vector3(1.25f, .2f, .5f) : new Vector3(R(e, beat, i, 81, .7f, 1.1f), R(e, beat, i, 82, .6f, 1f), R(e, beat, i, 83, .7f, 1.1f));
        }
        else
        {
            var len = R(e, beat, i, 79, .006f, .05f);
            var wid = R(e, beat, i, 80, .0025f, .006f);
            s.size = len;
            s.stretch = new Vector3(wid / len, Mathf.Max(.0008f, wid * .3f) / len, 1f);   // along local Z
            // slivers leave along the grain: start with their long axis near +Y
            s.q = Quaternion.LookRotation(new Vector3(R(e, beat, i, 84, -.3f, .3f), 1f, R(e, beat, i, 85, -.3f, .3f)).normalized, Vector3.right);
            s.w *= .6f;
        }
        solids[solidCount++] = s;
        solidsDirty = true;
    }

    void AddWisp(Emitter e, int beat, int i, Vector3 p, Vector3 v0, float s0, float s1, float tau0, float aspect, float lifeMin, float lifeMax, byte albedo)
    {
        if (wispCount >= hazeCap) { Dropped++; return; }
        var life = R(e, beat, i, 90, lifeMin, lifeMax);
        if (breakAt >= 0f) life = Mathf.Min(life, breakAt + 3.0f - clock - .01f);
        else if (finalAt >= 0f) life = Mathf.Min(life, finalAt + .10f + 3.0f - clock - .01f);
        if (life <= .05f) return;
        wisps[wispCount++] = new Wisp
        {
            p = p, v0 = v0, s0 = s0, s1 = s1, tau0 = tau0, aspect = aspect, life = life, albedo = albedo,
            frame = (byte)Mathf.Clamp((int)(R(e, beat, i, 91) * 8f), 0, 7),
            rot = R(e, beat, i, 92, -.6f, .6f),
            drift = new Vector3(R(e, beat, i, 93, -1f, 1f), R(e, beat, i, 94, -.5f, .5f), R(e, beat, i, 95, -1f, 1f)) * .05f,
        };
    }

    // ======================================================================================== simulation

    public void Tick(float dt)
    {
        watch.Restart();
        acc += dt;
        var steps = 0;
        while (acc >= Substep - 1e-6f && steps < 24) { Step(Substep); acc -= Substep; steps++; }
        if (steps == 24) acc = 0f;
        if (solidsDirty) { BuildSolids(); solidsDirty = false; }
        UploadGpu();
        skinUploadClock += dt;
        if (skinDirty && skinUploadClock >= .1f) { UploadSkin(); skinUploadClock = 0f; }
        watch.Stop();
        LastMainMs = (float)watch.Elapsed.TotalMilliseconds;
        if (LastMainMs > PeakMainMs) { PeakMainMs = LastMainMs; PeakMainAt = clock; PeakMainNote = "grit " + grainCount + " solids " + solidCount + " haze " + wispCount + " steps " + steps; }
        PeakGrit = Mathf.Max(PeakGrit, grainCount);
        PeakSolids = Mathf.Max(PeakSolids, solidCount);
        PeakHaze = Mathf.Max(PeakHaze, wispCount);
    }

    void Step(float h)
    {
        clock += h;
        for (var k = pending.Count - 1; k >= 0; k--)
            if (clock >= pending[k].at)
            {
                var pd = pending[k];
                pending.RemoveAt(k);
                if (pd.e == Emitter.EdgeShake) EdgeShake(pd.beat);
            }
        for (var k = 0; k < trickles.Count; k++)
        {
            var tr = trickles[k];
            if (tr.done < tr.total) { TrickleStep(ref tr); trickles[k] = tr; }
        }
        var deg = LeafDegNow;
        var omega = 0f;
        if (finalAt >= 0f) omega = (LeafDeg(clock - finalAt + h) - deg) / h * Mathf.Deg2Rad;
        var gust = breakAt >= 0f && clock - breakAt < .25f + .6f ? Mathf.Exp(-Mathf.Max(0f, clock - breakAt - .25f) / .2f) : 0f;
        StepGrains(h, deg, omega, gust);
        StepSolids(h, deg, omega);
        StepWisps(h, deg, omega, gust);
    }

    bool InWall(Vector3 p) => Mathf.Abs(p.x) < WallHalf && (p.z < OpenZ0 || p.z > OpenZ1) && p.y < 2.2f;

    // the leaf: shut (|x| < 0.022 in its footprint) until FINAL; then a plane on the throw curve. Returns the pushed point.
    bool LeafHit(ref Vector3 p, ref Vector3 v, Vector3 prev, float deg, float omega, float radius)
    {
        if (finalAt < 0f || deg <= .01f)
        {
            if (p.y < 0f || p.y > 2.1f || p.z < 0f || p.z > LeafZ) return false;
            if (Mathf.Abs(p.x) >= LeafX + radius) return false;
            if (!steel && finalAt >= 0f && p.z > .90f && p.y > .70f && p.y < 1.40f) return false;      // the notch is open from FINAL (wood)
            // the notch is open after FINAL (handled below); before it the leaf is whole except the slot's few mm
            var side = Mathf.Sign(prev.x == 0f ? 1f : prev.x);
            p.x = side * (LeafX + radius);
            if (v.x * side < 0f) v.x = -v.x * .2f;
            return true;
        }
        var a = deg * Mathf.Deg2Rad;
        var n = new Vector3(Mathf.Cos(a), 0f, -Mathf.Sin(a));
        var along = (p.x * Mathf.Sin(a) + (p.z - PivotZ) * Mathf.Cos(a));
        if (along < 0f || along > LeafZ - PivotZ || p.y < 0f || p.y > 2.1f) return false;
        var across = Vector3.Dot(new Vector3(p.x, 0f, p.z - PivotZ), n);
        var acrossPrev = Vector3.Dot(new Vector3(prev.x, 0f, prev.z - PivotZ), n);
        if (!steel && along > .905f - PivotZ && p.y > .72f && p.y < 1.38f) return false;     // the notch lets it through (wood)
        if (Mathf.Abs(across) >= LeafX + radius && Mathf.Sign(across) == Mathf.Sign(acrossPrev)) return false;
        var s = Mathf.Sign(acrossPrev == 0f ? 1f : acrossPrev);
        p += n * (s * (LeafX + radius) - across);
        // the leaf's surface velocity at the contact (Amnesia's rule): grains take it
        var lv = n * (omega * along);
        var rel = Vector3.Dot(v - lv, n) * s;
        if (rel < 0f) v -= n * (Vector3.Dot(v - lv, n)) * 1.0f;
        return true;
    }

    void StepGrains(float h, float deg, float omega, float gust)
    {
        for (var i = grainCount - 1; i >= 0; i--)
        {
            var g = grains[i];
            g.age += h;
            if (g.state == 2)
            {
                g.fade += h;
                if (g.fade >= .15f) g.state = 3;
            }
            else if (g.state == 1)
            {
                g.restClock += h;
                if (g.restClock >= 2f) { Splat(g.p, g.d, 1f); g.state = 3; }
            }
            else
            {
                var prev = g.p;
                // drag set by the terminal speed (quadratic), gravity, flutter for flakes
                var sp = g.v.magnitude;
                var k = 9.81f / (g.vt * g.vt);
                g.v += new Vector3(0f, Gravity, 0f) * h - g.v * (k * sp * h);
                if (g.flutterA > 0f)
                {
                    var f = g.flutterA * Mathf.Sin(g.age * g.flutterF * 6.283f + g.flutterPh);
                    g.p.x += f * h * .7f;
                    g.p.z += f * h * .7f * Mathf.Cos(g.flutterPh);
                }
                if (gust > 0f && g.d < .0005f) Gust(ref g.v, g.p, deg, omega, gust, h);
                g.p += g.v * h;
                if (InWall(g.p)) { g.p.x = Mathf.Sign(prev.x == 0f ? 1f : prev.x) * WallHalf; g.v.x = -g.v.x * .25f; }
                LeafHit(ref g.p, ref g.v, prev, deg, omega, g.d * .5f);
                if (g.p.y <= 0f)
                {
                    g.p.y = 0f;
                    if (!hardFloor && g.d < .001f) { g.state = 2; g.fade = 0f; Splat(g.p, g.d, 1f); }
                    else if (g.v.y < -.3f)
                    {
                        g.v = new Vector3(g.v.x * .4f, -g.v.y * .25f, g.v.z * .4f);
                    }
                    else { g.state = 1; g.v = Vector3.zero; g.restClock = 0f; }
                }
                if (g.age > g.life) { Splat(g.p, g.d, .5f); g.state = 3; }
            }
            if (g.state == 3)
            {
                grains[i] = grains[--grainCount];
                continue;
            }
            grains[i] = g;
        }
    }

    void Gust(ref Vector3 v, Vector3 p, float deg, float omega, float gust, float h)
    {
        // inside the swept wedge (the burst side between the shut plane and the leaf now, radius < 1 m): pushed along the
        // leaf normal at 0.25 x the leaf's surface speed at that radius, decaying with tau 0.2 s
        if (p.x <= 0f || deg <= 0f) return;
        var r = new Vector2(p.x, p.z - PivotZ).magnitude;
        if (r > 1.0f) return;
        var ang = Mathf.Atan2(p.x, p.z - PivotZ) * Mathf.Rad2Deg;
        if (ang < 0f || ang > deg + 8f) return;
        var a = deg * Mathf.Deg2Rad;
        var n = new Vector3(Mathf.Cos(a), 0f, -Mathf.Sin(a));
        var target = n * (.25f * Mathf.Abs(omega) * r * gust);
        var along = Vector3.Dot(v, n);
        var want = target.magnitude;
        if (along < want) v += n * ((want - along) * Mathf.Min(1f, h / .03f));
    }

    void StepSolids(float h, float deg, float omega)
    {
        var moved = false;
        for (var i = 0; i < solidCount; i++)
        {
            var s = solids[i];
            if (s.rest) continue;
            s.age += h;
            var prev = s.p;
            var vt = s.kind == 1 ? 3.0f : (s.stretch.y < .3f ? 2.5f : 4.0f);
            var sp = s.v.magnitude;
            s.v += new Vector3(0f, Gravity, 0f) * h - s.v * (9.81f / (vt * vt) * sp * h);
            if (s.stretch.y < .3f || s.kind == 1)
            {
                var f = .1f * Mathf.Sin(s.age * 4.5f * 6.283f + s.uvOff.x * 6.283f);
                s.v.x += f * h * 3f;
            }
            s.p += s.v * h;
            if (InWall(s.p)) { s.p.x = Mathf.Sign(prev.x == 0f ? 1f : prev.x) * WallHalf; s.v.x = -s.v.x * .3f; }
            LeafHit(ref s.p, ref s.v, prev, deg, omega, s.size * .3f);
            var wl = s.w.magnitude;
            if (wl > 1e-4f) s.q = Quaternion.AngleAxis(wl * h * Mathf.Rad2Deg, s.w / wl) * s.q;
            var lift = s.size * (s.kind == 1 ? Mathf.Max(s.stretch.x, s.stretch.y) * .5f : .3f);
            if (s.p.y < lift)
            {
                s.p.y = lift;
                var e = hardFloor ? .25f : .15f;
                var fr = hardFloor ? .4f : .25f;
                s.v = new Vector3(s.v.x * fr, -s.v.y * e, s.v.z * fr);
                s.w *= .35f;
                if (s.v.sqrMagnitude < .0025f)
                {
                    s.rest = true;
                    s.v = Vector3.zero;
                    s.w = Vector3.zero;
                    // lie on the largest face (the thinnest local axis up), keeping the heading
                    var fwd = s.q * Vector3.forward;
                    fwd.y = 0f;
                    if (fwd.sqrMagnitude < 1e-4f) fwd = Vector3.forward;
                    s.q = s.kind == 1 ? Quaternion.LookRotation(fwd.normalized, Vector3.up) : Quaternion.LookRotation(fwd.normalized, Vector3.up);
                    s.p.y = s.size * (s.kind == 1 ? s.stretch.y * .5f : s.stretch.y * .5f);
                }
            }
            solids[i] = s;
            moved = true;
        }
        if (moved) solidsDirty = true;
    }

    void StepWisps(float h, float deg, float omega, float gust)
    {
        for (var i = wispCount - 1; i >= 0; i--)
        {
            var w = wisps[i];
            w.age += h;
            if (w.age >= w.life) { wisps[i] = wisps[--wispCount]; continue; }
            var v = w.v0 * Mathf.Exp(-w.age / .15f);
            var slump = w.age > .4f ? -.08f * Mathf.Min(1f, (w.age - .4f) / .4f) : 0f;
            var curl = new Vector3(Mathf.Sin(w.age * 1.7f + w.rot * 5f), 0f, Mathf.Cos(w.age * 1.3f + w.rot * 7f)) * .05f;
            // the leaf's gust: a velocity of its own that decays with tau 0.2 s
            var gv = w.gustV;
            if (gust > 0f) Gust(ref gv, w.p, deg, omega, gust, h);
            w.gustV = gv * Mathf.Exp(-h / .2f);
            v += new Vector3(0f, slump, 0f) + curl + w.drift + w.gustV;
            w.p += v * h;
            if (w.p.y < .05f) w.p.y = .05f;
            wisps[i] = w;
        }
    }

    // ======================================================================================== the dust skin

    void Splat(Vector3 p, float d, float weight)
    {
        var fx = (p.x - SkinX0) / SkinTexel - .5f;
        var fz = (p.z - SkinZ0) / SkinTexel - .5f;
        var x0 = Mathf.FloorToInt(fx);
        var z0 = Mathf.FloorToInt(fz);
        if (x0 < 0 || z0 < 0 || x0 >= SkinW - 1 || z0 >= SkinH - 1) return;
        var tx = fx - x0;
        var tz = fz - z0;
        // a grain's share of the texel: its area over the texel's, x a pile-up gain (fines pack into the loops)
        var m = weight * Mathf.Min(.08f, (d * d) / (SkinTexel * SkinTexel) * 900f);
        skin[z0 * SkinW + x0] += m * (1 - tx) * (1 - tz);
        skin[z0 * SkinW + x0 + 1] += m * tx * (1 - tz);
        skin[(z0 + 1) * SkinW + x0] += m * (1 - tx) * tz;
        skin[(z0 + 1) * SkinW + x0 + 1] += m * tx * tz;
        skinDirty = true;
        if (!SkinUsed) EnsureSkin();
    }

    void EnsureSkin()
    {
        SkinUsed = true;
        if (skinGo != null) return;
        var sh = Shader.Find("FrontRooms/DoorDustSkin");
        if (sh == null) return;
        skinTex = new Texture2D(SkinW, SkinH, TextureFormat.R8, false, true) { name = "Door break dust skin", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        skinMat = new Material(sh) { name = "Door break dust skin" };
        skinMat.SetTexture("_SkinTex", skinTex);
        var floorMat = FrontRoomsSurfaces.TryGet(hardFloor ? "Run_Floor" : "L0_Carpet");
        if (floorMat != null && floorMat.HasProperty("_BaseMap")) skinMat.SetTexture("_PileTex", floorMat.GetTexture("_BaseMap"));
        skinMat.SetFloat("_Hard", hardFloor ? 1f : 0f);
        var fines = steel ? palettes[PalGypsum * 8 + 3] * .95f : palettes[PalCore * 8 + 5] * 1.15f;
        skinMat.SetColor("_FinesColor", new Color(fines.x, fines.y, fines.z, 1f));
        var mesh = new Mesh { name = "Door break dust skin" };
        float x1 = SkinX0 + SkinW * SkinTexel, z1 = SkinZ0 + SkinH * SkinTexel, y = .0015f;
        mesh.vertices = new[] { new Vector3(SkinX0, y, SkinZ0), new Vector3(x1, y, SkinZ0), new Vector3(x1, y, z1), new Vector3(SkinX0, y, z1) };
        mesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
        mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
        mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        mesh.RecalculateBounds();
        skinGo = Part("dust skin", mesh, skinMat);
        skinGo.GetComponent<MeshRenderer>().receiveShadows = false;
    }

    void UploadSkin()
    {
        if (skinTex == null) return;
        for (var i = 0; i < skin.Length; i++) skinBytes[i] = (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Min(1f, skin[i]) * 255f), 0, 255);
        skinTex.SetPixelData(skinBytes, 0);
        skinTex.Apply(false, false);
        skinDirty = false;
    }

    /// <summary>The skin's density bytes (for the door's floor pack, restore).</summary>
    public byte[] SkinSnapshot()
    {
        for (var i = 0; i < skin.Length; i++) skinBytes[i] = (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Min(1f, skin[i]) * 255f), 0, 255);
        return (byte[])skinBytes.Clone();
    }

    /// <summary>Restore: the skin from a floor pack.</summary>
    public void RestoreSkin(byte[] bytes)
    {
        if (bytes == null || bytes.Length != skin.Length) return;
        for (var i = 0; i < skin.Length; i++) skin[i] = bytes[i] / 255f;
        EnsureSkin();
        skinDirty = true;
        UploadSkin();
    }

    // ======================================================================================== solids: meshes, pack, merge

    // 8 crumb shapes (irregular polyhedra, 8-14 tris) built once from a fixed seed
    static Vector3[][] crumbVerts;
    static int[][] crumbTris;
    static void EnsureShapes()
    {
        if (crumbVerts != null) return;
        crumbVerts = new Vector3[8][];
        crumbTris = new int[8][];
        for (var s = 0; s < 8; s++)
        {
            // a jittered octahedron (8 tris) or a jittered capped prism (12 tris)
            var r = new System.Random(4210 + s);
            Func<float> J = () => .55f + .6f * (float)r.NextDouble();
            if (s % 2 == 0)
            {
                crumbVerts[s] = new[] { new Vector3(J(), 0, 0), new Vector3(-J(), 0, 0), new Vector3(0, J(), 0), new Vector3(0, -J(), 0), new Vector3(0, 0, J()), new Vector3(0, 0, -J()) };
                crumbTris[s] = new[] { 0, 2, 4, 2, 1, 4, 1, 3, 4, 3, 0, 4, 2, 0, 5, 1, 2, 5, 3, 1, 5, 0, 3, 5 };
            }
            else
            {
                var vs = new List<Vector3>();
                for (var k = 0; k < 4; k++)
                {
                    var a = k * Mathf.PI * .5f + (float)r.NextDouble() * .5f;
                    var rr = J();
                    vs.Add(new Vector3(Mathf.Cos(a) * rr, -.5f * J(), Mathf.Sin(a) * rr));
                }
                for (var k = 0; k < 4; k++)
                {
                    var a = k * Mathf.PI * .5f + .4f + (float)r.NextDouble() * .5f;
                    var rr = J() * .8f;
                    vs.Add(new Vector3(Mathf.Cos(a) * rr, .5f * J(), Mathf.Sin(a) * rr));
                }
                crumbVerts[s] = vs.ToArray();
                crumbTris[s] = new[] { 0, 1, 2, 0, 2, 3, 4, 6, 5, 4, 7, 6, 0, 4, 5, 0, 5, 1, 1, 5, 6, 1, 6, 2, 2, 6, 7, 2, 7, 3, 3, 7, 4, 3, 4, 0 };
            }
        }
    }

    // a sliver: a flat tapered strip along local +Z, 8 tris
    static readonly Vector3[] ChipVerts =
    {
        new Vector3(-.5f, -.5f, -.5f), new Vector3(.5f, -.5f, -.5f), new Vector3(.5f, .5f, -.5f), new Vector3(-.5f, .5f, -.5f),
        new Vector3(-.08f, 0f, .5f), new Vector3(.08f, 0f, .5f),
    };
    static readonly int[] ChipTris = { 0, 2, 1, 0, 3, 2, 3, 4, 5, 3, 5, 2, 0, 1, 5, 0, 5, 4, 1, 2, 5, 0, 4, 3 };

    // vertex normals per shape (the average of the faces at each corner): a crumb of 2-6 mm reads as a lit pebble; the
    // vertices are shared, so a crumb costs its 6-8 vertices, not 3 per triangle
    static Vector3[][] crumbNormals;
    static Vector3[] chipNormals;
    static Vector3[] VertexNormals(Vector3[] v, int[] t)
    {
        var n = new Vector3[v.Length];
        for (var k = 0; k < t.Length; k += 3)
        {
            var f = Vector3.Cross(v[t[k + 1]] - v[t[k]], v[t[k + 2]] - v[t[k]]);
            n[t[k]] += f; n[t[k + 1]] += f; n[t[k + 2]] += f;
        }
        for (var k = 0; k < n.Length; k++) n[k] = n[k].sqrMagnitude > 1e-12f ? n[k].normalized : Vector3.up;
        return n;
    }

    void AppendSolid(in Solid s, Matrix4x4 toLocal, List<Vector3> pv, List<Vector3> pn, List<Vector4> pt, List<Vector2> pu, List<int> pi)
    {
        EnsureShapes();
        if (crumbNormals == null)
        {
            crumbNormals = new Vector3[8][];
            for (var k = 0; k < 8; k++) crumbNormals[k] = VertexNormals(crumbVerts[k], crumbTris[k]);
            chipNormals = VertexNormals(ChipVerts, ChipTris);
        }
        var verts = s.kind == 1 ? ChipVerts : crumbVerts[s.shape];
        var norms = s.kind == 1 ? chipNormals : crumbNormals[s.shape];
        var tris = s.kind == 1 ? ChipTris : crumbTris[s.shape];
        var scale = s.kind == 1 ? new Vector3(s.stretch.x * s.size, s.stretch.y * s.size, s.size) : s.stretch * s.size;
        var inv = new Vector3(1f / Mathf.Max(scale.x, 1e-6f), 1f / Mathf.Max(scale.y, 1e-6f), 1f / Mathf.Max(scale.z, 1e-6f));
        var bi = pv.Count;
        for (var k = 0; k < verts.Length; k++)
        {
            var lp = Vector3.Scale(verts[k], scale);
            var p = toLocal.MultiplyPoint3x4(s.p + s.q * lp);
            var n = toLocal.MultiplyVector(s.q * Vector3.Scale(norms[k], inv)).normalized;
            var tu = Vector3.Cross(n, Mathf.Abs(n.y) < .9f ? Vector3.up : Vector3.right).normalized;
            pv.Add(p);
            pn.Add(n);
            pt.Add(new Vector4(tu.x, tu.y, tu.z, 1f));
            // metric UVs in the piece's own frame (the grain along its long local Z for slivers), at its random offset:
            // every crumb shows a different patch of the real material
            pu.Add(new Vector2(lp.x + lp.y * .5f, lp.z) + s.uvOff);
        }
        for (var t = 0; t < tris.Length; t++) pi.Add(bi + tris[t]);
    }

    void BuildSolids()
    {
        for (var m = 0; m < MatCount; m++)
        {
            v3.Clear(); n3.Clear(); t4.Clear(); uv.Clear(); ix.Clear();
            for (var i = 0; i < solidCount; i++) if (solids[i].mat == m) AppendSolid(solids[i], Matrix4x4.identity, v3, n3, t4, uv, ix);
            var mesh = solidMesh[m];
            mesh.Clear();
            if (v3.Count == 0) continue;
            mesh.SetVertices(v3);
            mesh.SetNormals(n3);
            mesh.SetTangents(t4);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(ix, 0, true);
        }
    }

    /// <summary>The floor merge: the resting solids (door root, canonical) as triangles per material, then they leave the
    /// live meshes (the visual's floor mesh holds them). Returns how many were taken.</summary>
    public int TakeRestingSolids(Matrix4x4 toLocal, List<Vector3>[] pv, List<Vector3>[] pn, List<Vector4>[] pt, List<Vector2>[] pu, List<int>[] pi, List<Solid> taken, int max)
    {
        var n = 0;
        for (var i = solidCount - 1; i >= 0 && n < max; i--)
        {
            if (!solids[i].rest) continue;
            var m = solids[i].mat;
            AppendSolid(solids[i], toLocal, pv[m], pn[m], pt[m], pu[m], pi[m]);
            taken?.Add(solids[i]);
            solids[i] = solids[--solidCount];
            n++;
        }
        if (n > 0) solidsDirty = true;
        return n;
    }

    /// <summary>Restore: put resting solids back (from the door's floor pack), so the visual's merge takes them.</summary>
    public void RestoreSolids(List<Solid> list)
    {
        if (list == null) return;
        foreach (var s in list)
        {
            if (solidCount >= solidCap) break;
            var r = s;
            r.rest = true;
            solids[solidCount++] = r;
        }
        solidsDirty = true;
    }

    public Material SolidMaterial(int m) => solidMat[m];

    // ======================================================================================== GPU

    void UploadGpu()
    {
        var l2w = root.localToWorldMatrix;
        gritDrawn = 0;
        for (var i = 0; i < grainCount; i++)
        {
            var g = grains[i];
            var fade = g.state == 2 ? 1f - g.fade / .15f : 1f;
            gritGpu[gritDrawn++] = new GritGpu { pos = l2w.MultiplyPoint3x4(g.p), diam = g.d * fade, vel = l2w.MultiplyVector(g.v), packed = g.col };
        }
        if (gritBuffer != null && gritDrawn > 0) gritBuffer.SetData(gritGpu, 0, 0, gritDrawn);
        hazeDrawn = 0;
        for (var i = 0; i < wispCount; i++)
        {
            var w = wisps[i];
            var u = w.age / w.life;
            var size = w.s0 + (w.s1 - w.s0) * (1f - Mathf.Exp(-w.age / .5f));
            var vel = w.v0 * Mathf.Exp(-w.age / .15f) + new Vector3(0f, w.age > .4f ? -.08f : 0f, 0f);
            // misc: x tau0, y erosion (0..1 over its life), z aspect, w in-fade
            var fadeIn = Mathf.Clamp01(w.age / .06f);
            hazeGpu[hazeDrawn++] = new HazeGpu
            {
                pos = l2w.MultiplyPoint3x4(w.p), size = size, vel = l2w.MultiplyVector(vel),
                packed = (uint)w.frame | ((uint)w.albedo << 4) | ((uint)Mathf.Clamp(Mathf.RoundToInt((w.rot + 1f) * 127f), 0, 255) << 8),
                misc = new Vector4(w.tau0, Mathf.Clamp01(u * u * 1.15f), w.aspect, fadeIn),
            };
        }
        if (hazeBuffer != null && hazeDrawn > 0) hazeBuffer.SetData(hazeGpu, 0, 0, hazeDrawn);
    }

    /// <summary>The slot / notch shaft (54 4.6) in world space: centre (w = on), U and V half axes, the normal toward the
    /// burst side, the far lamp's position and colour x level x the stage curve or the blow pulse.</summary>
    public void SetShaft(bool on, Vector3 c, Vector3 u, Vector3 v, Vector3 n, Vector3 lamp, Color col)
    {
        leakC = new Vector4(c.x, c.y, c.z, on ? 1f : 0f);
        leakU = new Vector4(u.x, u.y, u.z, .004f);
        leakV = new Vector4(v.x, v.y, v.z, 0f);
        leakN = new Vector4(n.x, n.y, n.z, 0f);
        leakLampPos = new Vector4(lamp.x, lamp.y, lamp.z, 1f);
        leakLampCol = new Vector4(col.r, col.g, col.b, 1f);
    }

    static readonly int IdGrit = Shader.PropertyToID("_FR_Grit"), IdHaze = Shader.PropertyToID("_FR_Haze");
    static readonly int IdGritParams = Shader.PropertyToID("_FR_GritParams"), IdGritScale = Shader.PropertyToID("_FR_GritScale");
    static readonly int IdLeakC = Shader.PropertyToID("_FR_LeakC"), IdLeakU = Shader.PropertyToID("_FR_LeakU"), IdLeakV = Shader.PropertyToID("_FR_LeakV"),
        IdLeakN = Shader.PropertyToID("_FR_LeakN"), IdLampPos = Shader.PropertyToID("_FR_LeakLampPos"), IdLampCol = Shader.PropertyToID("_FR_LeakLampCol");
    static readonly int IdHazeParams = Shader.PropertyToID("_FR_HazeParams"), IdAlbedo = Shader.PropertyToID("_FR_HazeAlbedo");

    void OnCamera(ScriptableRenderContext ctx, Camera cam)
    {
        if (cam == null || root == null) return;
        if (cam.cameraType != CameraType.Game && cam.cameraType != CameraType.SceneView) return;
        var bounds = new Bounds(root.position + Vector3.up * 1.1f, new Vector3(5f, 3f, 5f));
        var msaa = 1;
        if (cam.targetTexture != null) msaa = cam.targetTexture.antiAliasing;
        else if (cam.allowMSAA && GraphicsSettings.currentRenderPipeline is UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset urp) msaa = urp.msaaSampleCount;
        var phone = tier == Tier.Phone;
        if (gritMat != null && gritBuffer != null && gritDrawn > 0)
        {
            gritProps.SetBuffer(IdGrit, gritBuffer);
            gritProps.SetVector(IdGritParams, new Vector4(Substep, 24f, msaa <= 1 ? 1f : 0f, 0f));
            gritProps.SetVector(IdGritScale, new Vector4(phone ? GritVisualScaleNearPhone : 1f, phone ? GritVisualScaleFarPhone : GritVisualScaleFar, Time.time, gritDrawn));
            SetLeak(gritProps);
            var rp = new RenderParams(gritMat) { camera = cam, worldBounds = bounds, matProps = gritProps, shadowCastingMode = ShadowCastingMode.Off, receiveShadows = false, layer = root.gameObject.layer };
            Graphics.RenderPrimitives(rp, MeshTopology.Triangles, gritDrawn * 6, 1);
        }
        if (hazeMat != null && hazeBuffer != null && hazeDrawn > 0)
        {
            hazeProps.SetBuffer(IdHaze, hazeBuffer);
            hazeProps.SetVector(IdHazeParams, new Vector4(phone ? 1f : 0f, .30f, .15f, phone ? PhoneHazeFadeStart : 0f));
            var core = palettes[PalCore * 8 + 4];
            var settled = palettes[PalSettled * 8 + 4];
            hazeProps.SetVectorArray(IdAlbedo, new[] { new Vector4(.45f, .33f, .19f, 0f), new Vector4(settled.x * 1.2f, settled.y * 1.2f, settled.z * 1.2f, 0f), new Vector4(.70f, .68f, .63f, 0f), new Vector4(core.x, core.y, core.z, 0f) });
            SetLeak(hazeProps);
            var rp = new RenderParams(hazeMat) { camera = cam, worldBounds = bounds, matProps = hazeProps, shadowCastingMode = ShadowCastingMode.Off, receiveShadows = false, layer = root.gameObject.layer };
            Graphics.RenderPrimitives(rp, MeshTopology.Triangles, hazeDrawn * 6, 1);
        }
    }

    void SetLeak(MaterialPropertyBlock b)
    {
        b.SetVector(IdLeakC, leakC);
        b.SetVector(IdLeakU, leakU);
        b.SetVector(IdLeakV, leakV);
        b.SetVector(IdLeakN, leakN);
        b.SetVector(IdLampPos, leakLampPos);
        b.SetVector(IdLampCol, leakLampCol);
    }

    /// <summary>Tools: the haze's projected area (sum of wisp quads, clipped to the screen) as a fraction of the screen.</summary>
    public float HazeFill(Camera cam)
    {
        if (cam == null) return 0f;
        var l2w = root.localToWorldMatrix;
        var sum = 0f;
        for (var i = 0; i < wispCount; i++)
        {
            var w = wisps[i];
            var size = w.s0 + (w.s1 - w.s0) * (1f - Mathf.Exp(-w.age / .5f));
            var p = l2w.MultiplyPoint3x4(w.p);
            var d = Vector3.Dot(p - cam.transform.position, cam.transform.forward);
            if (d < cam.nearClipPlane) continue;
            var hpx = size / (2f * d * Mathf.Tan(cam.fieldOfView * .5f * Mathf.Deg2Rad));   // fraction of the screen height
            sum += hpx * hpx * w.aspect / cam.aspect;
        }
        return sum;
    }

    public void Dispose()
    {
        RenderPipelineManager.beginCameraRendering -= OnCamera;
        gritBuffer?.Release();
        hazeBuffer?.Release();
        gritBuffer = hazeBuffer = null;
        if (root != null) UnityEngine.Object.Destroy(root.gameObject);
        for (var m = 0; m < MatCount; m++) if (solidMesh[m] != null) UnityEngine.Object.Destroy(solidMesh[m]);
        if (gritMat != null) UnityEngine.Object.Destroy(gritMat);
        if (hazeMat != null) UnityEngine.Object.Destroy(hazeMat);
        if (skinMat != null) UnityEngine.Object.Destroy(skinMat);
        if (skinTex != null) UnityEngine.Object.Destroy(skinTex);
    }

    /// <summary>The skin's GameObject (the visual keeps it after the VFX's life ends: the skin persists).</summary>
    public GameObject SkinObject => skinGo;
    public Transform Root => root;
}
