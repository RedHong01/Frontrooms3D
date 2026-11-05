using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// SP7 treatment #2, THE FAR SHORE (research/level_space/72_unreachable_end.md section 3.2, the recommendation; Red
/// 2026-11-04 on the five: make each of them in scene 4 to look at). The far third of the corridor (FD2's frame with its
/// open leaves, cells 6-8 with their strips, red tubes and the T-B unit at 22.5, the head with the RN-F goal door, its goal
/// head and C42) is one island behind a steel expansion-joint collar on FD2 (the tell: a 200 mm steel band round walls and
/// soffit, a steel threshold plate, a fat grooved coupling on the sprinkler main, two latches). At the trip (FD1, 9 m) the
/// latches let go, the coupling parts in a puff of rust, fire-stop wool tears out of the joint in strands, the island jolts
/// 15 mm away and sails off as one lit compartment, faster than a sprint (0.5 s up to 8 m/s, 0.5 s braking, a heavy 0.35 s
/// seat), uncovering a raw shell: unpainted block 50 mm wider each side, a bare slab with two guide rails, the exposed deck
/// with the main and tray carried on, and caged work lamps on a yellow cord, already burning, swinging in the air the island
/// pushed. The walker's own floor never moves; the gap keeps the corridor's width and height; the scar stays until Y.
/// R 15 seats the head at 42 (the door opens to the hall), R 9 at 36 (the hall's blind panel stays).
/// Light: the near shore and the island are today's look A/B/C (the island's lamps timed at their moving place: the band
/// catches the island and turns it crimson as it goes); B's pulses run on both shores and skip the gap, whose work lamps
/// are not on the alarm circuit; T's white re-strike walks the near shore, crosses the warm gap and walks the island.
/// Scene 4 only (RULES 5b): the game's dress and lamps are the frame's; the raw shell, the lamps' cages, the collar, the
/// wool and the grit are greybox where no kit exists (named STAND-IN). No sound, no moving Relay.
/// </summary>
public sealed class FrontRoomsStructuresRunEndFarShore : FrontRoomsStructuresRunEnd
{
    // ================================================================ identity (the plate, the sign, the G panel)

    public override int Number => 2;
    public override string Name => "彼岸漂走（推荐）";
    // The armed room's one change is the collar on FD2 (FRAME rule 6: said in the sign's words).
    public override string TryLine => "FD2 门框围了一圈钢伸缩缝。冲过 FD1，看最后三格连灯带门开走，跑过毛坯去推门。";
    public override string OptionLine => gateOn
        ? "N：车厢开走前等你看向尽头（默认，最晚走到 12 米）；按 N 改成一触发就走"
        : "N：一触发车厢就开走，不等你看；按 N 改回等你看向尽头";
    // FD2's mark (M): the joint is here, the gaze gate's fallback is at 12 m (72 3.2), not at FD2 as today's.
    public override string Mark(FrontRoomsStructuresRunEndRoom room, int i, string today) =>
        i == 3 ? "FD2 · 18 米 · 车厢从这条缝开走（没看向尽头时你走到 12 米它就走）" : today;

    // ================================================================ numbers (district metres; sources in the comments)

    // The joint: just short of FD2's wall (its near face at 17.92). FD2's open leaves (16.92-17.90), their arms and magnets
    // are in FD2's part and ride the island with it, as 72 3.2 puts "FD2's frame with its open leaves" on the island.
    const float JointZ = 17.80f;
    // The collar (72 3.2: "a steel expansion-joint collar runs round walls, soffit and floor (a steel threshold plate about
    // 200 mm wide across the floor)"): the fixed half a 200 mm band on the near shore's lining, the moving half to FD2's wall.
    const float CollarZ0 = 17.60f, CollarZ1 = 17.92f, CollarProud = .006f;
    // FD2's far face (the frame's 0.16 wall on z 18): the island's own services emerge from it.
    const float Fd2Far = 18.08f;
    // The island's far end: the head on the rest line (27, both receding variants); its block wall's far face (head t 0.08).
    const float IslandEnd = 27f, HeadFar = 27.08f;
    // The raw shell (72 3.2, player proposal: "its section 50 mm wider each side and its soffit higher than the finished
    // lining"): 17 block courses over a bare slab 25 mm under the finished floor, to the hall's wall (42.1).
    const float RawHalf = 1.47f, RawBack = 1.67f, RawFloor = -.025f, RawSoffit = 3.375f, RawEnd = 42.1f;
    // The island's lining: inner faces as the corridor's (x 1.42, soffit 2.9), outside 10 mm clear of the raw block, its
    // roof at the dress's deck top (3.0), under the work lamps' cages (3.03).
    const float Inner = 1.42f, LiningOut = 1.46f, RoofTop = 3.0f, Soffit = 2.9f;
    // Block: 390 x 190 units on 10 mm joints (400 x 200 modules) in running bond; the mortar 8 mm behind the faces.
    const float BlockLength = .4f, BlockCourse = .2f, BlockJoint = .01f, MortarBack = .008f;

    // The move (72 3.2, from the player proposal: "a 0.25 s tell, 0.5 s up to 8 m/s, cruise, 0.5 s braking, a 0.35 s heavy
    // seat"; 72 section 5 lists them as proposed, not agreed). R 15: the seat begins at 2.63 s and settles at 2.98 s; R 9:
    // 1.88 / 2.23 s (from the gate's opening, which is the trip itself for a runner facing the door).
    const float TellTime = .25f;
    // "the island jolts 15 mm away" (72 3.2): today's tell amplitude (71 4.5, RunRecede's 0.015), away from the walker.
    const float Jolt = .015f;
    const float Cruise = 8f, Ramp = .5f;
    // Today's heavy final seat (RunRecede BuildSchedule: seat 0.35 s, amp 0.03, z = to + amp sin(pi u) exp(-3 u)).
    const float SeatTime = .35f, SeatAmp = .03f;
    // The gaze gate (72 3.2): SP7's 35 degrees kept, its fallback moved from 18 m to 12 m "so the seam never opens at the
    // runner's feet" (meaning proposal); N drops it (player proposal: "a runner crossing FD1 almost always faces the door").
    const float GateFallback = 12f;
    // Walkable at every moment (71 4.8 item 8: everything that moves stays ahead of the walker): a sprinter (5.5 m/s) at z when
    // the tell begins must stay the capsule's 0.3 plus 0.5 m short of FD2's leaves (their near end 16.92 plus the island's
    // travel) until the island has seated. R 15 comes closest 0.59 s in, as the island passes 5.5 m/s (14.6 - z); R 9 at its
    // seat's end (13.7 - z): the move waits for the walker at 13.8 m (R 15) or 12.9 m (R 9) or nearer the entrance (SafeFor,
    // worked out in Reset). The gate's own fallback (12 m) is inside both, so a run from FD1 never waits.
    const float LeafNear = 16.92f, Clearance = .8f;
    // Look A/B's band (RunRecede StripLevels: strip c darkens at 0.15 c, its red at 0.35 + 0.15 c: a front from z 1.8 at 20
    // m/s) and the heads' rise (start 0.35 + z / 20); look C's tide (0.25 + z / 3.2: walking pace from the opening).
    const float BandSpeed = 20f, BandOrigin = 1.8f, RedLag = .35f, TideSpeed = 3.2f, TideLag = .25f;

    // The gap's work lamps (72 3.2: "one caged work lamp per cell on its own always-on circuit"; "caged work lights on a
    // yellow cord already burning warm (uncovered, not switched on, so no flash) and swinging in the air the island pushed").
    // One per gap cell, the first 1.5 m into the gap: three at R 9, five at R 15.
    const int MaxLamps = 5;
    // The bulb's centre; the cage from 3.16 down to 3.03, 30 mm over the island's roof, so the island passes under them.
    const float LampBulbY = 3.10f;
    // Hook to the lamp's centre of mass: a swing of 2 pi sqrt(L / g) = 1.03 s.
    const float LampLength = .265f;
    // A 2700 K incandescent (a 150 W work lamp) under a shallow reflector, a 140 degree spot with the cage's wires in a cookie;
    // no shadows (they swing). Render stage 11-04 (renders/scene4/run_ends_diag2): at 3.2 the docked gap read near black from
    // inside it (slab luma 6-9, walls 12-14 of 255; 72 3.2 wants "15 m of raw corridor under warm bulbs", G1/G2 never a void);
    // 8.5 with the raw shell's albedo raised (RawMaterials) gives warm pools on the slab and lit block between them.
    const float LampIntensity = 8.5f, LampRange = 5.5f, LampSpot = 140f, LampInner = 100f;
    static readonly Color LampColour = new Color(1f, .70f, .40f);
    // The push of the passing island: up to 12 degrees at 8 m/s; with Reduce Flashing a third of it (no swing faster than
    // slow: the period stays the cord's 1 s, the amplitude falls to 4 degrees). zeta 0.05: the swing dies over about 8 s.
    const float SwingMax = 12f, SwingReduced = 4f, SwingDamping = .05f;
    // The pools' bounce on the deck and the walls' tops (in the gap the scene's ambient is the tripped room's dim one, and the
    // lamps' reflectors throw down): one up-facing spot on the slab per 6 m of gap, as the rig's look-dev floor-bounce spots,
    // rising with the lamps it stands among; SP.
    const int MaxBounce = 2;
    // Render stage 11-04: .55 left the deck black from under a lamp (luma .3-.5 looking up); 1.8 lets "the deck and services
    // exposed" read dimly over the pools.
    const float BounceIntensity = 1.8f, BounceRange = 6f;
    static readonly Color BounceColour = new Color(1f, .78f, .55f);
    // The open gap's ambient (room.Ambient): the warm bounce of the pools, off the alarm's red. Unity's trilight: sky lights the
    // up-facing floor, ground the down-facing deck (RunBTripped: floor .025, walls .05, ceiling .07). Render stage 11-04: the
    // integrator's first pass had them the other way round (sky .07, ground .025) and too dim to read; now floor .03, walls
    // .07, deck .11 (linear) in the bounce colour: the darks between the pools stay dim and warm, never a black pocket.
    static readonly Color GapSky = (BounceColour * .03f).gamma, GapEquator = (BounceColour * .07f).gamma, GapGround = (BounceColour * .11f).gamma;

    // ================================================================ state

    enum Beat { Armed, Gate, Tell, Travel, Seat, Docked }

    Beat beat = Beat.Armed;
    float beatClock;
    float travel;            // R less the jolt
    float rangeR;            // R (9 or 15)
    float safeZ = 12.9f;     // the walker's z at which the move may begin (SafeFor)
    float d, speed;          // the island's displacement along +z and its speed
    bool gateOn = true;      // N
    bool blocked, blockFlashed;
    float blockedTime;
    bool built;
    bool gapLook;            // PresetAt's hysteresis
    Vector3 site;

    // The island and what rides it (the frame puts every shared object back before each reset; Reset carries them again).
    Transform island;
    readonly List<Transform> carried = new List<Transform>();
    readonly List<Renderer> hiddenDress = new List<Renderer>();

    // The island's lamps (strips 6-8, the T-B unit at 22.5): their rest z and the moment the band / tide caught them.
    bool[] islandStrip = new bool[0], islandHead = new bool[0];
    float[] stripZ0 = new float[0], headZ0 = new float[0];
    float[] stripBand = new float[0], stripTide = new float[0], headBand = new float[0], headTide = new float[0];

    // The island's mesh-UV copies of the corridor's world-projected surfaces, and their macro wear (world space: it would slide
    // over the moving island, so it eases while the island runs and is back in full once it stands).
    readonly Dictionary<Material, Material> meshUv = new Dictionary<Material, Material>();
    readonly List<Material> macroMats = new List<Material>();
    readonly List<float> macroTone = new List<float>(), macroDirt = new List<float>();
    float macroShown = 1f;
    static readonly int MacroToneId = Shader.PropertyToID("_MacroTone");
    static readonly int MacroDirtId = Shader.PropertyToID("_MacroDirt");
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

    Texture2D cookie;
    MaterialPropertyBlock mpb;

    sealed class WorkLamp
    {
        public float z;
        public Transform pivot;
        public Light light;
        public GameObject root;
        public float swingZ, swingX, rateZ, rateX;   // degrees and degrees a second: toward +z (the island's way) and +x
        public bool kicked;
        public float uncoverAt = -1f;
        public float seed, level;
    }

    readonly WorkLamp[] lamps = new WorkLamp[MaxLamps];
    int lampCount;
    readonly Light[] bounce = new Light[MaxBounce];
    readonly float[] bounceZ = new float[MaxBounce];
    int bounceCount;

    sealed class Strand
    {
        public Transform t;
        public Vector3 anchor;      // on the near shore's collar
        public Vector3 hang;        // the direction it hangs in once torn
        public float tear, width, thick, hangLength;
        public bool torn;
        public float tornAt, seed;
    }

    readonly List<Strand> strands = new List<Strand>();
    readonly List<Transform> latches = new List<Transform>();
    float latchClock = -1f;

    // Bits: the rust puff from the parted coupling (translucent billboards) and grit (small cubes) from the joint and the deck.
    sealed class Bit
    {
        public Transform t;
        public Renderer r;
        public bool rust, live, landed;
        public float t0, life, size0, size1, spin;
        public Vector3 p, v, axis;
    }

    readonly List<Bit> bits = new List<Bit>();
    Material rustMat, gritMat;
    Vector3 couplingEnd;     // where the near shore's half of the coupling opens (the puff's origin)
    float puffAt = -1f;

    // ================================================================ build (once)

    public override void Build(FrontRoomsStructuresRunEndRoom room)
    {
        site = room.Site;
        mpb = new MaterialPropertyBlock();
        island = room.NewObject(this, "SP7 #2 island (FD2, cells 6-8 and the head ride it)");
        IndexLamps(room);
        BuildLining(room);
        Safe("straddling services", () => BuildStraddlers(room));
        BuildCollar(room);
        Safe("latches", () => BuildLatches(room));
        Safe("coupling", () => BuildCoupling(room));
        BuildRawShell(room);
        Safe("raw services", () => BuildRawServices(room));
        BuildWorkLamps(room);
        Safe("fire-stop wool", () => BuildStrands(room));
        Safe("rust and grit", () => BuildBits(room));
        CarryList(room);
        built = true;
    }

    static void Safe(string what, Action a)
    {
        try { a(); }
        catch (Exception e) { FrontRoomsPoolMaterials.LogOnce("SP7 #2 " + what, "SP7: treatment 2 left out its " + what + ": " + e.Message); }
    }

    // The island's lamps: strips in cells 6-8 and the heads of the T-B unit at 22.5 (cell 7); the goal heads ride the head.
    void IndexLamps(FrontRoomsStructuresRunEndRoom room)
    {
        var ns = room.StripCount;
        islandStrip = new bool[ns];
        stripZ0 = new float[ns];
        stripBand = new float[ns];
        stripTide = new float[ns];
        for (var i = 0; i < ns; i++)
        {
            var c = room.StripCell(i);
            islandStrip[i] = c >= 6 && c <= 8;
            stripZ0[i] = room.StripZ(i);
        }
        var nh = room.HeadCount;
        islandHead = new bool[nh];
        headZ0 = new float[nh];
        headBand = new float[nh];
        headTide = new float[nh];
        for (var i = 0; i < nh; i++)
        {
            var c = room.HeadCell(i);
            islandHead[i] = !room.IsGoalHead(i) && c >= 6 && c <= 8;
            headZ0[i] = room.HeadLampZ(i);
        }
        ClearCatches();
    }

    void ClearCatches()
    {
        for (var i = 0; i < stripBand.Length; i++) stripBand[i] = stripTide[i] = -1f;
        for (var i = 0; i < headBand.Length; i++) headBand[i] = headTide[i] = -1f;
    }

    // ---------------------------------------------------------------- the near shore and the island's lining

    // The dress's long shell (VCT lanes, wax, walls' block faces, lower tone, cove, soffit deck: single renderers 0-42) is not
    // cut at the joint, so Reset hides it (its colliders stay: the floor and walls the walker meets) and these copies stand in:
    // the near shore 0-17.80 in the same materials, the island 17.80-27 in mesh-UV copies (world projection would slide the
    // VCT and the block coursing over the moving island), walls thinned to 1.46 and the deck to 1.46 wide so it slides inside
    // the raw block. A box piece (the dress's slabs) is copied as a closed box; any other piece is cut by the frame
    // (room.CutPiece: its own triangles, the cut faces under the collar). UVs are the shader's own planar frame at the rest
    // pose, so the armed room is today's to the texel.
    void BuildLining(FrontRoomsStructuresRunEndRoom room)
    {
        var shell = room.LongShell;
        if (room.Dress == null || shell == null || shell.Count == 0)
        {
            FrontRoomsPoolMaterials.LogOnce("SP7 #2 no dress", "SP7: treatment 2 found no Run! B dress (the fallback shell): the gap shows the plain corridor.");
            return;
        }
        var near = new Batch(site);
        var moving = new Batch(site);
        var nearRoot = room.NewObject(this, "SP7 #2 near shore (the dress's long shell, 0-17.80)");
        var liningRoot = room.NewObject(this, "SP7 #2 island lining (the dress's long shell, 17.80-27, mesh UV)");
        liningRoot.SetParent(island, false);
        foreach (var r in shell)
        {
            if (r == null || r.sharedMaterial == null) continue;
            if (!DistrictBounds(r, site, out var lo, out var hi)) continue;
            var shadows = r.shadowCastingMode != ShadowCastingMode.Off;
            var box = IsBoxPiece(r);
            if (lo.z < JointZ - .001f)
            {
                if (box) near.Box(lo, new Vector3(hi.x, hi.y, Mathf.Min(hi.z, JointZ)), r.sharedMaterial, shadows);
                else room.CutPiece(this, r, CutBox(lo, new Vector3(hi.x, hi.y, Mathf.Min(hi.z, JointZ))), nearRoot, null, false);
            }
            if (hi.z > JointZ + .001f)
            {
                var a = new Vector3(Mathf.Max(lo.x, -LiningOut), lo.y, JointZ);
                var b = new Vector3(Mathf.Min(hi.x, LiningOut), Mathf.Min(hi.y, RoofTop), Mathf.Min(hi.z, IslandEnd));
                if (b.x - a.x > .0005f && b.y - a.y > .0005f && b.z - a.z > .0005f)
                {
                    if (box) moving.Box(a, b, MeshUvCopy(room, r.sharedMaterial), shadows);
                    else room.CutPiece(this, r, CutBox(a, b), liningRoot, MeshUvCopy(room, r.sharedMaterial), true);
                }
            }
            hiddenDress.Add(r);
        }
        near.Finish(nearRoot, "near shore", room);
        moving.Finish(liningRoot, "island lining", room);
        // The island's floor plate: 5 mm of steel under its VCT, on the guide rails (72 3.2: "a 10 mm steel floor plate carrying
        // its VCT on the guide rails"; 5 mm here so the bare slab sits 25 mm under the finished floor).
        var steel = new Batch(site);
        steel.Box(new Vector3(-LiningOut, RawFloor, JointZ), new Vector3(LiningOut, -.02f, IslandEnd), Galv, false);
        steel.Finish(liningRoot, "island floor plate", room);
    }

    // A copy of a world-projected corridor surface that takes its mesh's metre UVs instead (FrontRooms/Surface's
    // _FR_MESH_UV), so its texture rides the island; already mesh-UV surfaces (the cove, props) are used as they are.
    Material MeshUvCopy(FrontRoomsStructuresRunEndRoom room, Material src)
    {
        if (src == null) return null;
        if (src.IsKeywordEnabled("_FR_MESH_UV") || (src.HasProperty("_MeshUV") && src.GetFloat("_MeshUV") > .5f)) return src;
        if (meshUv.TryGetValue(src, out var have) && have != null) return have;
        var m = new Material(src) { name = "SP7 #2 island " + src.name + " (mesh UV)", hideFlags = HideFlags.DontSave };
        m.EnableKeyword("_FR_MESH_UV");
        if (m.HasProperty("_MeshUV")) m.SetFloat("_MeshUV", 1f);
        room.Own(m);
        meshUv[src] = m;
        if (m.HasProperty(MacroToneId) && m.HasProperty(MacroDirtId))
        {
            macroMats.Add(m);
            macroTone.Add(m.GetFloat(MacroToneId));
            macroDirt.Add(m.GetFloat(MacroDirtId));
        }
        return m;
    }

    // ---------------------------------------------------------------- the dress's small pieces across the joint

    // The dress's small pieces that cross the joint (CellRenderers, cell 5): the bumper rail tile hides (the rail ends short
    // of the collar, as rails stop at joints); the EMT, tray and main tiles hide and scaled clones of them stand in, the near
    // ones ending at the collar (the main in its coupling), the island's from FD2's far face (or the coupling); anything else
    // goes to the side its centre is on.
    readonly List<Renderer> straddleHidden = new List<Renderer>();
    readonly HashSet<Renderer> straddleSet = new HashSet<Renderer>();
    readonly List<Transform> straddleCarry = new List<Transform>();
    float mainX = .55f, mainY = 2.62f, mainRadius = .055f;
    bool mainFound;

    void BuildStraddlers(FrontRoomsStructuresRunEndRoom room)
    {
        var dress = room.Dress;
        if (dress == null) return;
        var roots = new List<Transform>();
        for (var c = 5; c <= 6; c++)
            foreach (var r in room.CellRenderers(c))
            {
                if (r == null || !DistrictBounds(r, site, out var lo, out var hi)) continue;
                if (lo.z >= JointZ - .001f || hi.z <= JointZ + .001f) continue;
                var root = KitRoot(r.transform, dress);
                if (root != null && !roots.Contains(root)) roots.Add(root);
            }
        foreach (var root in roots)
        {
            var name = root.name;
            var rs = root.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) continue;
            KitBounds(rs, site, out var lo, out var hi);
            // The dress's labels (FrontRoomsRunDress Overhead, RemoteCircuit, Faces): the tiles that run along the corridor.
            var main = name == "run b sprinkler main";
            var linear = main || name == "run b cable tray" || name == "run b emt" || name == "run b remote emt";
            var rail = name == "run b bumper rail";
            if (!linear && !rail)
            {
                if ((lo.z + hi.z) * .5f >= JointZ) straddleCarry.Add(root);
                continue;
            }
            foreach (var r in rs)
            {
                straddleHidden.Add(r);
                straddleSet.Add(r);
            }
            if (rail) continue;
            if (main && !mainFound)
            {
                mainFound = true;
                mainX = (lo.x + hi.x) * .5f;
                mainY = (lo.y + hi.y) * .5f;
                mainRadius = Mathf.Clamp((hi.y - lo.y) * .5f, .02f, .09f);
            }
            var nearEnd = main ? JointZ : CollarZ0;
            if (nearEnd - lo.z > .02f)
            {
                var clone = Clone(root, lo.z, nearEnd, hi.z - lo.z);
                if (clone != null) clone.SetParent(Own, true);
            }
            // The island's part: from FD2's far face (the main's is the coupling's stub, BuildCoupling).
            var islandStart = Fd2Far;
            if (!main && hi.z - islandStart > .02f)
            {
                var clone = Clone(root, islandStart, hi.z, hi.z - lo.z);
                if (clone != null) clone.SetParent(island, true);
            }
        }
        // The island's far end: a tile of cell 8 that runs on past the head's wall (the west remote EMT, 26.55-27.55) would ride
        // out through the hall's wall at R 15; it ends in the head's wall instead.
        var far = new List<Transform>();
        foreach (var r in room.CellRenderers(8))
        {
            if (r == null || !DistrictBounds(r, site, out var lo, out var hi)) continue;
            if (lo.z >= HeadFar - .001f || hi.z <= HeadFar + .02f) continue;
            var root = KitRoot(r.transform, dress);
            if (root != null && !far.Contains(root)) far.Add(root);
        }
        foreach (var root in far)
        {
            var name = root.name;
            if (name != "run b cable tray" && name != "run b emt" && name != "run b remote emt" && name != "run b sprinkler main") continue;
            var rs = root.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) continue;
            KitBounds(rs, site, out var lo, out var hi);
            foreach (var r in rs)
            {
                straddleHidden.Add(r);
                straddleSet.Add(r);
            }
            var clone = Clone(root, lo.z, IslandEnd, hi.z - lo.z);
            if (clone != null) clone.SetParent(island, true);
        }
    }

    // The kit instance a dress renderer belongs to: its nearest ancestor (or itself) with the dress's own label ("run b ...":
    // every FrontRoomsRunDress spawn and slab is named so), short of the overhead group that holds the main, tray and EMT;
    // a renderer with no such ancestor stands for itself.
    static Transform KitRoot(Transform t, Transform dress)
    {
        for (var p = t; p != null && p != dress; p = p.parent)
            if (p.name.StartsWith("run b ", StringComparison.Ordinal) && !p.name.StartsWith("run b overhead", StringComparison.Ordinal)) return p;
        return t;
    }

    static void KitBounds(Renderer[] rs, Vector3 site, out Vector3 lo, out Vector3 hi)
    {
        lo = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        hi = -lo;
        foreach (var r in rs)
        {
            if (!DistrictBounds(r, site, out var a, out var b)) continue;
            lo = Vector3.Min(lo, a);
            hi = Vector3.Max(hi, b);
        }
    }

    // A scaled clone of a linear kit tile running from z0 to z1 (district), scaled along whichever of its axes runs along the
    // corridor; the dress scales its own EMT tiles the same way (FrontRoomsRunDress Tiles).
    Transform Clone(Transform root, float z0, float z1, float length)
    {
        if (length < .01f || z1 - z0 < .01f) return null;
        var go = UnityEngine.Object.Instantiate(root.gameObject, root.parent);
        go.name = root.name + " (SP7 #2, z " + z0.ToString("0.00") + "-" + z1.ToString("0.00") + ")";
        var t = go.transform;
        var axis = 0;
        var best = -1f;
        for (var a = 0; a < 3; a++)
        {
            var e = a == 0 ? Vector3.right : a == 1 ? Vector3.up : Vector3.forward;
            var w = t.TransformVector(e);
            if (Mathf.Abs(w.z) > best)
            {
                best = Mathf.Abs(w.z);
                axis = a;
            }
        }
        var s = t.localScale;
        var f = (z1 - z0) / length;
        if (axis == 0) s.x *= f;
        else if (axis == 1) s.y *= f;
        else s.z *= f;
        t.localScale = s;
        var rs = go.GetComponentsInChildren<Renderer>(true);
        var srcs = root.GetComponentsInChildren<Renderer>(true);
        for (var i = 0; i < rs.Length && i < srcs.Length; i++)
        {
            rs[i].enabled = true;
            if (!srcs[i].HasPropertyBlock()) continue;
            srcs[i].GetPropertyBlock(mpb);
            rs[i].SetPropertyBlock(mpb);
            mpb.Clear();
        }
        KitBounds(rs, site, out var lo, out _);
        t.position += new Vector3(0f, 0f, z0 - lo.z);
        return t;
    }

    // ---------------------------------------------------------------- the collar (the tell) and its latches

    Material galv, steelDark;
    Material Galv => galv != null ? galv : galv = FrontRoomsPoolMaterials.Surface("RunB_GalvSteel", new Color(.6f, .62f, .63f));
    Material SteelDark => steelDark != null ? steelDark : steelDark = FrontRoomsPoolMaterials.Surface("Prop_SteelBlack", new Color(.12f, .12f, .12f));

    // The fixed half on the near shore: a galvanised band 200 mm wide on both walls and the soffit, its return over the
    // lining's end, the floor's nosing angle, a grey line of fire-stop wool in the seam, bolts, the latches' keepers. The
    // moving half on the island: its bands to FD2's wall, the return over the island's lining end, the threshold plate (its
    // lip on the near shore's nosing), bolts, and the flat bars FD2's hold-open magnets hang on, so they ride with FD2.
    void BuildCollar(FrontRoomsStructuresRunEndRoom room)
    {
        var kit = room.Kit;
        var g = Galv;
        var wool = Wool();
        room.BeginPart(this, "SP7 #2 collar, fixed half (near shore)");
        kit.Shell(Soffit, 0f);
        var castKeep = kit.Mesh.CastShadows;
        kit.Mesh.CastShadows = false;
        foreach (var s in new[] { -1f, 1f })
        {
            Span(kit, s * Inner, s * (Inner - CollarProud), 0f, Soffit, CollarZ0, JointZ, g);
            Span(kit, s * (Inner - CollarProud), s * RawHalf, RawFloor, RoofTop, JointZ - .006f, JointZ, g);
            for (var y = .25f; y < Soffit - .1f; y += .45f) Bolt(kit, new Vector3(s * (Inner - CollarProud - .004f), y, CollarZ0 + .1f), new Vector3(.008f, .022f, .022f));
            Span(kit, s * (Inner - CollarProud), s * (Inner - .028f), 1.515f, 1.545f, 17.665f, 17.715f, SteelDark);
            Span(kit, s * (Inner - .012f), s * (Inner - .002f), .01f, Soffit - .01f, JointZ - .004f, JointZ + .004f, wool);
        }
        kit.Box(-Inner, Soffit - CollarProud, CollarZ0, Inner, Soffit, JointZ, g, false);
        kit.Box(-RawHalf, Soffit - CollarProud, JointZ - .006f, RawHalf, RoofTop + .005f, JointZ, g, false);
        for (var x = -1.2f; x < 1.25f; x += .45f) Bolt(kit, new Vector3(x, Soffit - CollarProud - .004f, CollarZ0 + .1f), new Vector3(.022f, .008f, .022f));
        kit.Box(-Inner, RawFloor, JointZ - .04f, Inner, .002f, JointZ, g, false);
        kit.Box(-Inner + .004f, Soffit - .012f, JointZ - .004f, Inner - .004f, Soffit - .002f, JointZ + .004f, wool, false);
        kit.Mesh.CastShadows = castKeep;
        room.EndPart();

        var part = room.BeginPart(this, "SP7 #2 collar, moving half (island)");
        kit.Shell(Soffit, 0f);
        castKeep = kit.Mesh.CastShadows;
        kit.Mesh.CastShadows = false;
        foreach (var s in new[] { -1f, 1f })
        {
            Span(kit, s * Inner, s * (Inner - CollarProud), 0f, Soffit, JointZ, CollarZ1, g);
            Span(kit, s * (Inner - CollarProud), s * (LiningOut + .005f), RawFloor, RoofTop + .005f, JointZ, JointZ + .006f, g);
            for (var y = .25f; y < Soffit - .1f; y += .45f) Bolt(kit, new Vector3(s * (Inner - CollarProud - .004f), y, (JointZ + CollarZ1) * .5f), new Vector3(.008f, .022f, .022f));
            Span(kit, s * (Inner - .004f), s * Inner, 1.865f, 1.935f, 17.04f, CollarZ1, SteelDark);
        }
        kit.Box(-Inner, Soffit - CollarProud, JointZ, Inner, Soffit, CollarZ1, g, false);
        kit.Box(-LiningOut - .005f, Soffit - CollarProud, JointZ, LiningOut + .005f, RoofTop + .005f, JointZ + .006f, g, false);
        kit.Box(-1.40f, 0f, JointZ - .10f, 1.40f, .006f, JointZ + .10f, g, false);
        for (var x = -1.25f; x < 1.3f; x += .5f) Bolt(kit, new Vector3(x, .007f, JointZ + .06f), new Vector3(.02f, .004f, .02f));
        kit.Mesh.CastShadows = castKeep;
        room.EndPart();
        part.SetParent(island, true);
    }

    // A render-only box between two x, two y and two z (in any order).
    static void Span(FrontRoomsStructuresKit kit, float xa, float xb, float ya, float yb, float za, float zb, Material m)
    {
        kit.Box(Mathf.Min(xa, xb), Mathf.Min(ya, yb), Mathf.Min(za, zb), Mathf.Max(xa, xb), Mathf.Max(ya, yb), Mathf.Max(za, zb), m, false);
    }

    static void Bolt(FrontRoomsStructuresKit kit, Vector3 c, Vector3 size)
    {
        kit.Box(c - size * .5f, c + size * .5f, FrontRoomsPoolMaterials.Steel, false);
    }

    // Two latches across the joint at 1.55 m on both walls (72 3.2: "two hard clanks as its latches let go"): a black steel
    // hook pivoted on the island's band, over a keeper on the near shore's band; at the trip they flip up.
    void BuildLatches(FrontRoomsStructuresRunEndRoom room)
    {
        foreach (var s in new[] { -1f, 1f })
        {
            var pivot = room.NewObject(this, "SP7 #2 collar latch " + (s < 0f ? "west" : "east"));
            pivot.SetParent(island, false);
            pivot.localPosition = new Vector3(s * (Inner - CollarProud - .013f), 1.555f, 17.875f);
            Piece(pivot, "latch hook", new Vector3(0f, 0f, -.095f), new Vector3(.02f, .032f, .19f), SteelDark);
            Piece(pivot, "latch hook tip", new Vector3(0f, -.022f, -.178f), new Vector3(.02f, .03f, .028f), SteelDark);
            Piece(pivot, "latch pivot boss", Vector3.zero, new Vector3(.024f, .045f, .045f), FrontRoomsPoolMaterials.Steel);
            latches.Add(pivot);
        }
    }

    // A render-only cube under a parent (local centre and size).
    static Transform Piece(Transform parent, string name, Vector3 centre, Vector3 size, Material m)
    {
        return Prim(parent, PrimitiveType.Cube, name, centre, size, Quaternion.identity, m);
    }

    static Transform Prim(Transform parent, PrimitiveType type, string name, Vector3 centre, Vector3 scale, Quaternion rotation, Material m)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        UnityEngine.Object.Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = centre;
        go.transform.localRotation = rotation;
        go.transform.localScale = scale;
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = ShadowCastingMode.Off;
        return go.transform;
    }

    // ---------------------------------------------------------------- the main's coupling

    // A fat grooved coupling (72 3.2: "the dry sprinkler main is joined at the collar by a fat grooved coupling"; "the main's
    // coupling parts with a hiss and a puff of rust"): its housing stays on the near shore's pipe end (17.76-17.84), the
    // island's pipe end (a stub to its first tile, in FD2's wall) pulls out of it.
    void BuildCoupling(FrontRoomsStructuresRunEndRoom room)
    {
        var red = FrontRoomsPoolMaterials.Surface("RunB_SprinklerRed", new Color(.64f, .13f, .10f));
        var along = Quaternion.Euler(90f, 0f, 0f);
        var housing = room.NewObject(this, "STAND-IN SP7 #2 grooved coupling (near shore)");
        housing.localPosition = new Vector3(mainX, mainY, JointZ);
        var hr = mainRadius + .022f;
        Prim(housing, PrimitiveType.Cylinder, "housing", Vector3.zero, new Vector3(hr * 2f, .04f, hr * 2f), along, red);
        Prim(housing, PrimitiveType.Cylinder, "housing split", Vector3.zero, new Vector3(hr * 2f + .006f, .008f, hr * 2f + .006f), along, SteelDark);
        foreach (var s in new[] { -1f, 1f })
        {
            Piece(housing, "bolt pad", new Vector3(s * (hr + .012f), 0f, 0f), new Vector3(.03f, .024f, .05f), red);
            Piece(housing, "bolt", new Vector3(s * (hr + .012f), 0f, 0f), new Vector3(.012f, .05f, .012f), FrontRoomsPoolMaterials.Steel);
        }
        couplingEnd = new Vector3(mainX, mainY, JointZ + .045f);
        var stubLength = 18.02f - (JointZ + .01f);
        var stub = room.NewObject(this, "STAND-IN SP7 #2 island main stub");
        stub.SetParent(island, false);
        stub.localPosition = new Vector3(mainX, mainY, JointZ + .01f + stubLength * .5f);
        Prim(stub, PrimitiveType.Cylinder, "pipe", Vector3.zero, new Vector3(mainRadius * 2f, stubLength * .5f, mainRadius * 2f), along, red);
        Prim(stub, PrimitiveType.Cylinder, "groove", new Vector3(0f, 0f, -stubLength * .5f + .015f), new Vector3(mainRadius * 2f - .004f, .004f, mainRadius * 2f - .004f), along, SteelDark);
    }

    // ---------------------------------------------------------------- the raw shell (static: what the island uncovers)

    Material[] blockMats;
    Material mortar, slab, deck, chalk;

    // Unpainted block, a bare slab with two guide rails and chalk lines, the exposed deck (72 3.2: "unpainted grey block, a
    // bare slab with two guide rails, the deck and services exposed"; "the gap must read as the same corridor unfinished, in
    // period (1977-1990 unpainted block, slab, caged incandescent lamps), never as missing textures or a void"). Every block
    // is its own face on a mortar plane 8 mm behind, so the joints catch the work lamps' light.
    void BuildRawShell(FrontRoomsStructuresRunEndRoom room)
    {
        RawMaterials(room);
        var kit = room.Kit;
        room.BeginPart(this, "STAND-IN SP7 #2 raw shell (unpainted block, bare slab, deck)");
        kit.Shell(RawSoffit, 0f);
        var courses = Mathf.RoundToInt((RawSoffit - RawFloor) / BlockCourse);
        foreach (var s in new[] { -1f, 1f })
        {
            var xf = s * RawHalf;
            var xm = s * (RawHalf + MortarBack);
            var n = new Vector3(-s, 0f, 0f);
            Quad(kit, new Vector3(xm, RawFloor, JointZ), new Vector3(xm, RawSoffit, JointZ), new Vector3(xm, RawSoffit, RawEnd), new Vector3(xm, RawFloor, RawEnd), n, mortar);
            for (var k = 0; k < courses; k++)
            {
                var y0 = RawFloor + k * BlockCourse;
                var y1 = Mathf.Min(RawSoffit - BlockJoint * .5f, y0 + BlockCourse - BlockJoint);
                var off = (k & 1) == 0 ? 0f : BlockLength * .5f;
                for (var j = 0; j < 200; j++)
                {
                    var z0 = JointZ - off + j * BlockLength;
                    if (z0 >= RawEnd) break;
                    var za = Mathf.Max(JointZ, z0);
                    var zb = Mathf.Min(RawEnd, z0 + BlockLength - BlockJoint);
                    if (zb - za < .03f) continue;
                    var m = blockMats[Hash(j, k, s > 0f ? 7 : 3) % blockMats.Length];
                    Quad(kit, new Vector3(xf, y0, za), new Vector3(xf, y1, za), new Vector3(xf, y1, zb), new Vector3(xf, y0, zb), n, m);
                }
            }
            // The block's back closes the shell (for the light and the shadows).
            Span(kit, s * (RawHalf + MortarBack + .002f), s * RawBack, RawFloor - .2f, RawSoffit + .2f, JointZ, RawEnd, mortar);
        }
        // The header over the near shore's lining (from its deck's top, 3.0, to the raw deck), facing the gap.
        Quad(kit, new Vector3(-RawHalf, RoofTop, JointZ - MortarBack), new Vector3(RawHalf, RoofTop, JointZ - MortarBack), new Vector3(RawHalf, RawSoffit, JointZ - MortarBack), new Vector3(-RawHalf, RawSoffit, JointZ - MortarBack), Vector3.forward, mortar);
        for (var k = 0; k < courses; k++)
        {
            var y0 = RawFloor + k * BlockCourse;
            var y1 = Mathf.Min(RawSoffit - BlockJoint * .5f, y0 + BlockCourse - BlockJoint);
            if (y1 <= RoofTop + .02f) continue;
            y0 = Mathf.Max(y0, RoofTop);
            var off = (k & 1) == 0 ? 0f : BlockLength * .5f;
            for (var j = 0; j < 20; j++)
            {
                var x0 = -RawHalf - off + j * BlockLength;
                if (x0 >= RawHalf) break;
                var xa = Mathf.Max(-RawHalf, x0);
                var xb = Mathf.Min(RawHalf, x0 + BlockLength - BlockJoint);
                if (xb - xa < .03f) continue;
                Quad(kit, new Vector3(xa, y0, JointZ), new Vector3(xa, y1, JointZ), new Vector3(xb, y1, JointZ), new Vector3(xb, y0, JointZ), Vector3.forward, blockMats[Hash(j, k, 11) % blockMats.Length]);
            }
        }
        kit.Box(-RawBack, RoofTop, JointZ - .1f, RawBack, RawSoffit + .2f, JointZ - MortarBack - .002f, mortar, false);
        // The bare slab (its top 25 mm under the finished floor) and the raw deck over it.
        kit.Box(-RawHalf, RawFloor - .2f, JointZ, RawHalf, RawFloor, RawEnd, slab, false);
        kit.Box(-RawBack, RawSoffit, JointZ - .1f, RawBack, RawSoffit + .2f, RawEnd, deck, false);
        // Two guide rails set flush in the slab, 1.5 mm proud (they glint), clips every 1.5 m (72 3.2: "a bare slab with two
        // guide rails"; at x 0.95, where SP7's guide slot runs).
        var steel = FrontRoomsPoolMaterials.Steel;
        foreach (var s in new[] { -1f, 1f })
        {
            kit.Box(s * .95f - .025f, RawFloor - .01f, JointZ, s * .95f + .025f, RawFloor + .0015f, RawEnd - .05f, steel, false);
            for (var z = JointZ + .75f; z < RawEnd - .2f; z += 1.5f)
                kit.Box(s * .95f - .07f, RawFloor, z - .03f, s * .95f + .07f, RawFloor + .004f, z + .03f, Galv, false);
        }
        // Chalk: the layout's snapped lines (the lower tone's top at 1.2 m on both walls, the axis and the cell lines on the
        // slab): the corridor drawn on the raw shell before it was built.
        foreach (var s in new[] { -1f, 1f })
            Span(kit, s * (RawHalf - .0006f), s * RawHalf, 1.197f, 1.203f, JointZ + .15f, RawEnd - .2f, chalk);
        kit.Box(-.004f, RawFloor, JointZ + .25f, .004f, RawFloor + .0006f, RawEnd - .2f, chalk, false);
        for (var z = 21f; z < RawEnd - .1f; z += 3f)
            kit.Box(-.25f, RawFloor, z - .004f, .25f, RawFloor + .0006f, z + .004f, chalk, false);
        room.EndPart();
    }

    void RawMaterials(FrontRoomsStructuresRunEndRoom room)
    {
        // The block's raw concrete (Run! B's broken-block core, RunB_BlockCore, mesh UV) in three tones on tiles of 0.71-0.97 m
        // (none a multiple of the 0.4 m block, so no two neighbours show the same speckle; the aggregate at a few mm), matte,
        // dusty at the foot.
        var core = FrontRoomsPoolMaterials.Surface("RunB_BlockCore", new Color(.55f, .55f, .53f));
        // Render stage 11-04: the core's texture is about 0.30 linear on its own and the tints multiply it, so the first pass
        // (.64-.75 gamma) gave a charcoal block (albedo ~0.1). Unpainted concrete block reads mid grey (albedo ~0.25-0.30):
        // tints near white, the three tones kept apart.
        var tones = new[] { new Color(.97f, .96f, .93f), new Color(.90f, .89f, .86f), new Color(1f, .99f, .95f) };
        blockMats = new Material[tones.Length];
        for (var i = 0; i < tones.Length; i++)
        {
            var m = new Material(core) { name = "STAND-IN SP7 #2 unpainted block " + i, hideFlags = HideFlags.DontSave };
            Set(m, "_TileSize", new Vector4(.71f + .13f * i, .71f + .13f * i, 0f, 0f));
            SetColour(m, "_BaseColor", tones[i]);
            SetFloat(m, "_Smoothness", .55f);
            SetFloat(m, "_MacroTone", .30f);
            SetFloat(m, "_MacroDirt", .28f);
            SetFloat(m, "_FloorGrime", .55f);
            SetFloat(m, "_FloorGrimeHeight", .45f);
            room.Own(m);
            blockMats[i] = m;
        }
        var mortarSrc = FrontRoomsPoolMaterials.Surface("RunB_BlockMortar", new Color(.62f, .6f, .57f));
        mortar = new Material(mortarSrc) { name = "STAND-IN SP7 #2 raw mortar", hideFlags = HideFlags.DontSave };
        SetColour(mortar, "_BaseColor", new Color(.86f, .84f, .80f));
        SetFloat(mortar, "_Smoothness", .4f);
        room.Own(mortar);
        // The bare slab: the same raw concrete on a 1.1 m tile, a little darker and dustier than the block (no finish, no wax).
        slab = new Material(core) { name = "STAND-IN SP7 #2 bare slab", hideFlags = HideFlags.DontSave };
        Set(slab, "_TileSize", new Vector4(1.1f, 1.1f, 0f, 0f));
        SetColour(slab, "_BaseColor", new Color(.85f, .83f, .79f));
        SetFloat(slab, "_Smoothness", .7f);
        SetFloat(slab, "_MacroTone", .35f);
        SetFloat(slab, "_MacroDirt", .40f);
        room.Own(slab);
        // The deck's underside: the stairwells' cast concrete (form-tie marks), without its own-lights cut (it skips lamps
        // reaching over 6 m, and the strips reach 10).
        var cast = FrontRoomsPoolMaterials.Surface("Stair_Slab", new Color(.6f, .6f, .58f));
        deck = new Material(cast) { name = "STAND-IN SP7 #2 raw deck", hideFlags = HideFlags.DontSave };
        deck.DisableKeyword("_FR_OWN_LIGHTS");
        SetFloat(deck, "_OwnLights", 0f);
        SetColour(deck, "_BaseColor", new Color(.82f, .81f, .78f));
        SetFloat(deck, "_MacroDirt", .3f);
        room.Own(deck);
        chalk = FrontRoomsPoolMaterials.Surface("Chalk_Blue", new Color(.4f, .5f, .64f));
    }

    static void Set(Material m, string name, Vector4 v)
    {
        if (m != null && m.HasProperty(name)) m.SetVector(name, v);
    }

    static void SetColour(Material m, string name, Color c)
    {
        if (m != null && m.HasProperty(name)) m.SetColor(name, c);
    }

    static void SetFloat(Material m, string name, float f)
    {
        if (m != null && m.HasProperty(name)) m.SetFloat(name, f);
    }

    // A quad a, b, c, d facing front (the kit turns its winding), the kit's planar metre UVs.
    static void Quad(FrontRoomsStructuresKit kit, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 front, Material m)
    {
        kit.QuadPlanar(a, b, c, d, front, m, false);
    }

    static int Hash(int a, int b, int c)
    {
        unchecked
        {
            var h = (uint)(a * 73856093) ^ (uint)(b * 19349663) ^ (uint)(c * 83492791);
            h ^= h >> 13;
            h *= 0x5bd1e995u;
            h ^= h >> 15;
            return (int)(h & 0x7fffffffu);
        }
    }

    static float Hash01(int a, int b) => (Hash(a, b, 5) % 10007) / 10007f;

    // The services carried on in the raw shell, tight to the deck over the island's roof (72's build note: "the main and tray
    // carried on"): the game's 3 m main and tray kits with their hangers, struts and rods, from the header to the hall's wall.
    void BuildRawServices(FrontRoomsStructuresRunEndRoom room)
    {
        var parent = room.NewObject(this, "SP7 #2 raw shell services (main, tray: the game's kits)");
        var along = Quaternion.Euler(0f, 90f, 0f);   // kit +X (Blender) = Unity -X -> along +Z (FrontRoomsRunDress Overhead)
        var mainInfo = FrontRoomsKitLibrary.GetInfo("Kit_SprinklerMain_3m");
        var trayInfo = FrontRoomsKitLibrary.GetInfo("Kit_CableTray_3m");
        var mainOriginY = RawSoffit - .07f - (mainInfo != null ? mainInfo.Max.y : .06f);
        var trayOriginY = RawSoffit - .06f - (trayInfo != null ? trayInfo.Max.y : .05f);
        var galvanised = Galv;
        for (var z = JointZ; z < RawEnd - .3f; z += 3f)
        {
            var len = Mathf.Min(3f, RawEnd - z);
            var scale = new Vector3(len / 3f, 1f, 1f);
            FrontRoomsKitLibrary.Spawn("Kit_SprinklerMain_3m", parent, new Vector3(.55f, mainOriginY, z), along, scale, false, "raw main", false);
            FrontRoomsKitLibrary.Spawn("Kit_CableTray_3m", parent, new Vector3(-.60f, trayOriginY, z), along, scale, false, "raw tray", false);
            FrontRoomsKitLibrary.Spawn("Kit_PipeHanger_Band", parent, new Vector3(.55f, mainOriginY, z + 1.1f), along, null, false, "raw main hanger", false);
            var tz = z + .4f;
            var strutY = trayOriginY - .0105f;
            Piece(parent, "raw tray strut", new Vector3(-.60f, strutY, tz), new Vector3(.42f, .021f, .041f), galvanised);
            foreach (var s in new[] { -1f, 1f })
                Piece(parent, "raw tray rod", new Vector3(-.60f + s * .19f, (RawSoffit + strutY - .01f) * .5f, tz), new Vector3(.0095f, RawSoffit - strutY + .01f, .0095f), galvanised);
        }
        foreach (var r in parent.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = ShadowCastingMode.Off;
    }

    // ---------------------------------------------------------------- the work lamps

    Material cordMat;

    // Caged work lamps on a yellow festoon cord along the deck, one per gap cell (the drop at the cell's middle): a hook, a
    // short drop, a black rubber socket, a shallow reflector, a six-wire cage with a bottom ring, spokes and hub, the bulb
    // (Prop_BulbLit, burning), and a spot whose cookie is the cage's wires. They hang over the island's roof, so the island
    // passes under them; their light reaches the gap only once the island's roof has slid off it.
    void BuildWorkLamps(FrontRoomsStructuresRunEndRoom room)
    {
        cookie = CageCookie();
        room.Own(cookie);
        cordMat = FrontRoomsPoolMaterials.Surface("RunB_BucketYellow", new Color(.85f, .68f, .08f));
        var rubber = FrontRoomsPoolMaterials.Surface("Prop_Rubber", new Color(.02f, .02f, .02f));
        var bulb = FrontRoomsPoolMaterials.Surface("Prop_BulbLit", new Color(1f, .95f, .85f));
        var shade = FrontRoomsPoolMaterials.Surface("Prop_StainlessSatin", new Color(.8f, .8f, .8f));
        var neck = FrontRoomsPoolMaterials.Surface("Prop_Aluminium", new Color(.7f, .7f, .7f));
        var wire = SteelDark;
        var kit = room.Kit;
        room.BeginPart(this, "SP7 #2 festoon cord and hooks (yellow, on the deck)");
        kit.Shell(RawSoffit, 0f);
        var keep = kit.Mesh.CastShadows;
        kit.Mesh.CastShadows = false;
        // The cord starts at a temporary outlet box on the header (the site's own circuit, not the alarm's).
        kit.Box(.78f, 3.13f, JointZ, .94f, 3.27f, JointZ + .07f, Galv, false);
        var prev = new Vector3(.86f, 3.2f, JointZ + .075f);
        var top = RawSoffit - .02f;
        for (var k = 0; k < MaxLamps; k++)
        {
            var z = JointZ + 1.5f + 3f * k;
            var x = (k & 1) == 0 ? .10f : -.06f;
            var hook = new Vector3(x, top, z);
            Cord(kit, prev, hook, k == 0 ? .05f : .11f);
            kit.Box(x - .004f, RawSoffit - .03f, z - .004f, x + .004f, RawSoffit, z + .004f, FrontRoomsPoolMaterials.Steel, false);
            prev = hook;
            var lamp = new WorkLamp { z = z, seed = Hash01(k, 31) * 100f };
            var root = room.NewObject(this, "STAND-IN SP7 #2 work lamp " + (k + 1) + " (z " + z.ToString("0.0") + ")");
            root.localPosition = hook;
            lamp.root = root.gameObject;
            var pivot = new GameObject("swing").transform;
            pivot.SetParent(root, false);
            lamp.pivot = pivot;
            // In the pivot's frame: down is -y; heights below as district y less the hook's.
            Piece(pivot, "drop cord", new Vector3(0f, (3.215f - top) * .5f, 0f), new Vector3(.008f, top - 3.215f, .008f), cordMat);
            Prim(pivot, PrimitiveType.Cylinder, "socket (rubber)", new Vector3(0f, 3.18f - top, 0f), new Vector3(.044f, .035f, .044f), Quaternion.identity, rubber);
            Prim(pivot, PrimitiveType.Cylinder, "reflector", new Vector3(0f, 3.155f - top, 0f), new Vector3(.15f, .005f, .15f), Quaternion.identity, shade);
            Prim(pivot, PrimitiveType.Cylinder, "bulb neck", new Vector3(0f, 3.14f - top, 0f), new Vector3(.026f, .012f, .026f), Quaternion.identity, neck);
            Prim(pivot, PrimitiveType.Sphere, "bulb (burning)", new Vector3(0f, LampBulbY - top, 0f), new Vector3(.06f, .085f, .06f), Quaternion.identity, bulb);
            const float cr = .062f;
            for (var w = 0; w < 6; w++)
            {
                var a = (30f + 60f * w) * Mathf.Deg2Rad;
                Piece(pivot, "cage wire", new Vector3(Mathf.Cos(a) * cr, 3.095f - top, Mathf.Sin(a) * cr), new Vector3(.0025f, .12f, .0025f), wire);
                var b = (60f * w) * Mathf.Deg2Rad;
                var apothem = cr * Mathf.Cos(30f * Mathf.Deg2Rad);
                var ring = Piece(pivot, "cage ring", new Vector3(Mathf.Cos(b) * apothem, 3.035f - top, Mathf.Sin(b) * apothem), new Vector3(.0025f, .0025f, cr), wire);
                ring.localRotation = Quaternion.Euler(0f, -60f * w, 0f);
            }
            for (var w = 0; w < 3; w++)
            {
                var spoke = Piece(pivot, "cage spoke", new Vector3(0f, 3.033f - top, 0f), new Vector3(.0022f, .0022f, cr * 2f), wire);
                spoke.localRotation = Quaternion.Euler(0f, 60f * w, 0f);
            }
            Prim(pivot, PrimitiveType.Cylinder, "cage hub", new Vector3(0f, 3.032f - top, 0f), new Vector3(.014f, .003f, .014f), Quaternion.identity, wire);
            lamp.light = room.NewLight(pivot, "work lamp light (2700 K, not on the alarm circuit)", LightType.Spot, new Vector3(0f, LampBulbY - top, 0f), Vector3.down, LampSpot, LampInner, 0f, LampRange, LampColour);
            lamp.light.cookie = cookie;
            lamp.light.enabled = false;
            lamps[k] = lamp;
        }
        // The cord's tail, tied off past the last hook.
        Cord(kit, prev, prev + new Vector3(.06f, -.1f, .35f), .04f);
        kit.Mesh.CastShadows = keep;
        room.EndPart();
        for (var k = 0; k < MaxBounce; k++)
        {
            bounceZ[k] = JointZ + 4.5f + 6f * k;
            var holder = room.NewObject(this, "SP7 #2 work lamps' bounce " + (k + 1) + " (their pools' light on the deck)");
            var l = room.NewLight(holder, "bounce light", LightType.Spot, new Vector3(0f, .3f, bounceZ[k]), Vector3.up, 165f, 120f, 0f, BounceRange, BounceColour);
            l.enabled = false;
            bounce[k] = l;
        }
    }

    // A sagging run of cord between two points (a parabola in six straight pieces).
    void Cord(FrontRoomsStructuresKit kit, Vector3 a, Vector3 b, float sag)
    {
        const int n = 6;
        var p0 = a;
        for (var i = 1; i <= n; i++)
        {
            var u = i / (float)n;
            var p1 = Vector3.Lerp(a, b, u) + Vector3.down * (4f * sag * u * (1f - u));
            var step = p1 - p0;
            var len = step.magnitude;
            if (len > .002f) kit.Mesh.Box((p0 + p1) * .5f, new Vector3(.008f, .008f, len + .004f), Quaternion.LookRotation(step / len, Vector3.up), cordMat, false);
            p0 = p1;
        }
    }

    // The cage as the bulb throws it (a 140 degree spot: the cookie's radius 0.5 is 70 degrees off the axis, r = tan(a) /
    // tan(70)): six vertical wires at 62 mm (radial shadows beyond 44 degrees, where the bottom ring falls), three spokes
    // along the wires and a hub under the bulb, a soft edge, brighter straight down (the reflector). Half-dark at most: a
    // regular pattern that swings stays low in contrast (FRAME rule 4), and a bulb this size throws soft shadows.
    static Texture2D CageCookie()
    {
        const int n = 256;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false, true)
        {
            name = "SP7 #2 work lamp cage (cookie)",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.DontSave,
        };
        var px = new Color32[n * n];
        var tanEdge = Mathf.Tan(70f * Mathf.Deg2Rad);
        var ringR = Mathf.Tan(43.6f * Mathf.Deg2Rad) / tanEdge;
        var hubR = Mathf.Tan(5.3f * Mathf.Deg2Rad) / tanEdge;
        for (var y = 0; y < n; y++)
            for (var x = 0; x < n; x++)
            {
                var u = (x + .5f) / n - .5f;
                var v = (y + .5f) / n - .5f;
                var r = Mathf.Sqrt(u * u + v * v) * 2f;
                var phi = Mathf.Atan2(v, u) * Mathf.Rad2Deg;
                // The reflector gathers the light downward (0.45 + 0.55 cos^2 of the angle off the axis): pools on the slab,
                // the walls still lit to about 2.6 m.
                var off = Mathf.Cos(Mathf.Atan(r * tanEdge));
                var level = r >= 1f ? 0f : Smooth((1f - r) / .14f) * (.45f + .55f * off * off);
                var wireShade = 0f;
                for (var w = 0; w < 6; w++)
                {
                    var dphi = Mathf.DeltaAngle(phi, 30f + 60f * w);
                    wireShade = Mathf.Max(wireShade, Mathf.Exp(-(dphi * dphi) / 2.25f));
                }
                wireShade *= Smooth((r - ringR + .02f) / .05f);
                var ringShade = Mathf.Exp(-((r - ringR) * (r - ringR)) / (.02f * .02f)) * .3f;
                var spokeShade = 0f;
                if (r < ringR + .01f)
                    for (var w = 0; w < 3; w++)
                    {
                        var a = (30f + 60f * w) * Mathf.Deg2Rad;
                        var perp = Mathf.Abs(-u * Mathf.Sin(a) + v * Mathf.Cos(a)) * 2f;
                        spokeShade = Mathf.Max(spokeShade, Mathf.Exp(-(perp * perp) / (.006f * .006f)) * .18f);
                    }
                var hubShade = r < hubR ? .12f : r < hubR * 1.6f ? .12f * (1f - (r - hubR) / (hubR * .6f)) : 0f;
                level *= 1f - .5f * wireShade;
                level *= 1f - Mathf.Max(ringShade, Mathf.Max(spokeShade, hubShade));
                var b = (byte)Mathf.Clamp(Mathf.RoundToInt(level * 255f), 0, 255);
                px[y * n + x] = new Color32(b, b, b, b);
            }
        tex.SetPixels32(px);
        tex.Apply(false, false);
        return tex;
    }

    // The rust puff's sprite: white, the alpha a soft round core (a gaussian) with a slightly lumpy outline that is zero at the
    // rim, so the billboards overlap into a small cloud of rust and scale with no hard edge (the colour is the material's).
    static Texture2D PuffSprite()
    {
        const int n = 64;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, true, false)
        {
            name = "SP7 #2 rust puff sprite",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.DontSave,
        };
        var px = new Color32[n * n];
        for (var y = 0; y < n; y++)
            for (var x = 0; x < n; x++)
            {
                var u = (x + .5f) / n * 2f - 1f;
                var v = (y + .5f) / n * 2f - 1f;
                var phi = Mathf.Atan2(v, u);
                var r = Mathf.Sqrt(u * u + v * v) * (1f + .10f * Mathf.Sin(3f * phi + .7f) + .06f * Mathf.Sin(5f * phi + 2.1f));
                var a = r >= 1f ? 0f : Mathf.Exp(-3.2f * r * r) * Smooth((1f - r) / .35f);
                px[y * n + x] = new Color32(255, 255, 255, (byte)Mathf.Clamp(Mathf.RoundToInt(a * 255f), 0, 255));
            }
        tex.SetPixels32(px);
        tex.Apply(true, false);
        return tex;
    }

    // ---------------------------------------------------------------- fire-stop wool (72 3.2: "grey fire-stop wool pulls out
    // of the joint in strands"; the build note's "about a dozen fire-stop wool strands"): fourteen tufts round the joint,
    // stretched between the halves as the joint opens, each tearing at its own length and dropping to hang from the near
    // shore's collar.

    Material woolMat;

    Material Wool()
    {
        if (woolMat == null) woolMat = FrontRoomsPoolMaterials.Lit("STAND-IN SP7 #2 fire-stop wool", new Color(.60f, .58f, .53f), .04f);
        return woolMat;
    }

    void BuildStrands(FrontRoomsStructuresRunEndRoom room)
    {
        var root = room.NewObject(this, "STAND-IN SP7 #2 fire-stop wool strands");
        var k = 0;
        foreach (var s in new[] { -1f, 1f })
            for (var i = 0; i < 5; i++)
            {
                var y = .32f + .55f * i + (Hash01(k, 3) - .5f) * .2f;
                AddStrand(root, new Vector3(s * (Inner - .02f), y, JointZ), new Vector3(0f, -1f, .18f), k++);
            }
        for (var i = 0; i < 4; i++)
        {
            var x = -.98f + .65f * i + (Hash01(k, 3) - .5f) * .18f;
            AddStrand(root, new Vector3(x, Soffit - .02f, JointZ), new Vector3(0f, -1f, .1f), k++);
        }
    }

    void AddStrand(Transform root, Vector3 anchor, Vector3 hang, int k)
    {
        var t = Piece(root, "wool strand " + k, anchor, Vector3.one * .01f, Wool());
        t.gameObject.SetActive(false);
        strands.Add(new Strand
        {
            t = t,
            anchor = anchor,
            hang = hang.normalized,
            tear = .22f + 1.1f * Hash01(k, 17),
            width = .035f + .035f * Hash01(k, 19),
            thick = .016f + .012f * Hash01(k, 23),
            seed = Hash01(k, 29) * 50f,
        });
    }

    // ---------------------------------------------------------------- rust and grit

    void BuildBits(FrontRoomsStructuresRunEndRoom room)
    {
        // Render stage 11-04 (renders/scene4/run_ends2, film FA f0108-f0125): plain quads read as hard-edged brown squares over
        // FD2's wall, a glitch rather than a puff; each billboard now carries a soft round sprite (PuffSprite) in the puff's own
        // copy of the transparent material.
        var puff = PuffSprite();
        room.Own(puff);
        rustMat = new Material(FrontRoomsPoolMaterials.Transparent("SP7 #2 rust puff", new Color(.42f, .22f, .12f, .5f), 0f)) { name = "SP7 #2 rust puff (soft sprite)" };
        if (rustMat.HasProperty(BaseMapId)) rustMat.SetTexture(BaseMapId, puff);
        room.Own(rustMat);
        gritMat = FrontRoomsPoolMaterials.Lit("STAND-IN SP7 #2 mortar grit", new Color(.62f, .60f, .55f), .1f);
        var root = room.NewObject(this, "SP7 #2 rust and grit");
        for (var i = 0; i < 16; i++) bits.Add(MakeBit(root, true, i));
        for (var i = 0; i < 40; i++) bits.Add(MakeBit(root, false, i));
    }

    Bit MakeBit(Transform root, bool rust, int i)
    {
        var go = GameObject.CreatePrimitive(rust ? PrimitiveType.Quad : PrimitiveType.Cube);
        go.name = rust ? "rust " + i : "grit " + i;
        UnityEngine.Object.Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(root, false);
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = rust ? rustMat : gritMat;
        r.shadowCastingMode = ShadowCastingMode.Off;
        go.SetActive(false);
        return new Bit { t = go.transform, r = r, rust = rust };
    }

    // ---------------------------------------------------------------- what rides the island (worked out once, carried in Reset)

    // FD2's part, the cells' strips and units (6-8), the dress's small pieces wholly past the joint (cells 5-8), the head's
    // magnets at the rest line; the dress's long shell and the joint's straddlers hide (copies stand in).
    void CarryList(FrontRoomsStructuresRunEndRoom room)
    {
        carried.Clear();
        var fd2 = room.FireFrame(1);
        if (fd2 != null) carried.Add(fd2);
        for (var c = 5; c <= 8; c++)
        {
            foreach (var r in room.CellRenderers(c))
            {
                if (r == null || straddleSet.Contains(r) || !DistrictBounds(r, site, out var lo, out _)) continue;
                if (lo.z >= JointZ - .001f && !carried.Contains(r.transform)) carried.Add(r.transform);
            }
            if (c >= 6)
                foreach (var go in room.CellObjects(c))
                    if (go != null && !carried.Contains(go.transform)) carried.Add(go.transform);
        }
        foreach (var t in straddleCarry) if (t != null && !carried.Contains(t)) carried.Add(t);
        var magnets = room.Magnets(IslandEnd);
        if (magnets != null) carried.Add(magnets.transform);
        foreach (var r in straddleHidden) if (r != null && !hiddenDress.Contains(r)) hiddenDress.Add(r);
    }

    // ================================================================ the room's cycle

    public override void Enter(FrontRoomsStructuresRunEndRoom room)
    {
        gapLook = false;
    }

    public override void Reset(FrontRoomsStructuresRunEndRoom room)
    {
        if (!built) return;
        beat = Beat.Armed;
        beatClock = 0f;
        d = speed = 0f;
        blocked = blockFlashed = false;
        blockedTime = 0f;
        latchClock = puffAt = -1f;
        gapLook = false;
        rangeR = Mathf.Max(0f, room.FinalLine - room.RestLine);
        travel = Mathf.Max(0f, rangeR - Jolt);
        safeZ = SafeFor(rangeR);
        island.localPosition = Vector3.zero;
        // Today's room as the island's: the long shell and the straddlers hide (copies stand in, colliders stay), the track
        // goes (the island runs on the slab's rails; the tell is the collar, 72 3.2), what rides is carried.
        foreach (var r in hiddenDress) if (r != null) r.enabled = false;
        if (room.Track != null) room.Track.gameObject.SetActive(false);
        foreach (var t in carried) if (t != null) t.SetParent(island, true);
        room.PlaceHead(room.RestLine, false);
        ClearCatches();
        // The gap's lamps: one per cell of R (three at R 9, five at R 15), covered by the island.
        lampCount = Mathf.Clamp(Mathf.RoundToInt(rangeR / FrontRoomsStructuresRunEndRoom.CellLength), 0, MaxLamps);
        for (var k = 0; k < MaxLamps; k++)
        {
            var l = lamps[k];
            if (l == null) continue;
            if (l.root != null) l.root.SetActive(k < lampCount);
            l.swingZ = l.swingX = l.rateZ = l.rateX = 0f;
            l.kicked = false;
            l.uncoverAt = -1f;
            if (l.pivot != null) l.pivot.localRotation = Quaternion.identity;
            l.level = 0f;
            if (l.light != null)
            {
                l.light.intensity = 0f;
                l.light.enabled = false;
            }
        }
        bounceCount = rangeR > 12f ? 2 : 1;
        foreach (var b in bounce)
        {
            if (b == null) continue;
            b.intensity = 0f;
            b.enabled = false;
        }
        foreach (var s in strands)
        {
            s.torn = false;
            s.tornAt = -1f;
            if (s.t != null) s.t.gameObject.SetActive(false);
        }
        foreach (var l in latches) if (l != null) l.localRotation = Quaternion.identity;
        foreach (var b in bits)
        {
            b.live = false;
            if (b.t != null) b.t.gameObject.SetActive(false);
        }
        SetMacro(1f);
        room.Waiting = false;
        room.HeadMoving = false;
        room.Dirty();
    }

    public override void Exit(FrontRoomsStructuresRunEndRoom room)
    {
        SetMacro(1f);
        beat = Beat.Armed;
        foreach (var b in bits) b.live = false;
    }

    public override void Lines(FrontRoomsStructuresRunEndRoom room, List<float> lines)
    {
        // The head's magnets ride the island from the rest line; no other line is ever stood on.
        lines.Add(room.RestLine);
    }

    public override void Trip(FrontRoomsStructuresRunEndRoom room)
    {
        if (!built) return;
        ClearCatches();
        // Docked (a trip at the entrance after the re-strike): the island stays where it seated, the scar stays on this floor
        // (72 3.2); the trip only plays the light.
        if (beat == Beat.Docked || room.Receded)
        {
            beat = Beat.Docked;
            return;
        }
        if (beat != Beat.Armed) return;
        beat = Beat.Gate;
        beatClock = 0f;
        room.Waiting = true;
        room.Dirty();
    }

    public override void ResetStarted(FrontRoomsStructuresRunEndRoom room)
    {
        // A move still waiting for its look is called off with the alarm (as today's); one already moving finishes.
        if (beat != Beat.Gate) return;
        beat = Beat.Armed;
        blocked = false;
        room.Dirty();
    }

    public override bool Option(FrontRoomsStructuresRunEndRoom room, out string message)
    {
        gateOn = !gateOn;
        room.Dirty();
        message = gateOn ? "做法 2：车厢等你看向尽头才开走（最晚走到 12 米）" : "做法 2：一触发车厢就开走，不等你看（玩家的提案）";
        return true;
    }

    public override string Status(FrontRoomsStructuresRunEndRoom room)
    {
        switch (beat)
        {
            case Beat.Gate: return blocked ? "等你退回 FD2 前面" : gateOn ? "等你看向尽头" : "马上开走";
            case Beat.Tell:
            case Beat.Travel:
            case Beat.Seat: return "车厢在走";
            case Beat.Docked: return "已靠岸，毛坯留着（Y 复原）";
            default: return "";
        }
    }

    public override void Guide(FrontRoomsStructuresRunEndRoom room, StringBuilder sb)
    {
        sb.AppendLine("做法 2 彼岸漂走：FD2 那道门框是一圈钢的伸缩缝（墙和顶上一圈钢带、地上一块钢踏板、消防水管上一个粗接头、两边各一个卡扣）。跨过 FD1 触发后，卡扣弹开，水管接头断开冒一口锈，防火棉被扯成一缕一缕；FD2 到门这最后三格连灯带门整段往后开走（半秒加速到每秒 8 米，比冲刺快，半秒刹住，重重一停），露出没装修的毛坯：灰砖墙、带两条导轨的水泥地、露出来的楼板和管线、挂在黄线上的工作灯，被风带得直晃。停稳以后门才推得开：9→14 格停在 42 米（门后是大厅），9→12 格停在 36 米（门在这里打不开）。");
        sb.AppendLine("N：车厢开走前等不等你看向尽头（默认等，最晚走到 12 米就走）。工作灯不在警报回路上：B 的脉冲在两岸跑、跳过毛坯；T 恢复时白光走过近岸、跨过毛坯、再走到车厢里。毛坯一直留到按 Y。灰盒占位：毛坯、工作灯的灯罩和铁丝笼、伸缩缝、防火棉、锈和碎屑。");
    }

    // ================================================================ every frame

    public override void Tick(FrontRoomsStructuresRunEndRoom room, FrontRoomsStructuresRunEndFrame f)
    {
        if (!built) return;
        var dt = Mathf.Max(0f, f.dt);
        var before = d;
        switch (beat)
        {
            case Beat.Gate:
                GateTick(room, f);
                break;
            case Beat.Tell:
            {
                beatClock += dt;
                // The latches let go at once; the island jolts 15 mm away over the tell's first 0.1 s and holds there.
                d = Jolt * Smooth(beatClock / .1f);
                if (beatClock >= TellTime)
                {
                    beatClock -= TellTime;
                    beat = Beat.Travel;
                    d = Jolt + Move(beatClock, travel, Cruise, Ramp, Ramp);
                }
                break;
            }
            case Beat.Travel:
            {
                beatClock += dt;
                var total = MoveTime(travel, Cruise, Ramp, Ramp);
                d = Jolt + Move(beatClock, travel, Cruise, Ramp, Ramp);
                if (beatClock >= total)
                {
                    beatClock -= total;
                    beat = Beat.Seat;
                    d = rangeR;
                    SeatStarted(room, f);
                }
                break;
            }
            case Beat.Seat:
            {
                beatClock += dt;
                var u = Mathf.Clamp01(beatClock / SeatTime);
                d = rangeR + SeatAmp * Mathf.Sin(Mathf.PI * u) * Mathf.Exp(-3f * u);
                if (beatClock >= SeatTime)
                {
                    d = rangeR;
                    beat = Beat.Docked;
                    island.localPosition = new Vector3(0f, 0f, d);
                    room.PlaceHead(room.RestLine + d, false);
                    // The final seat: the hall's blind panel stays unless the head stands at 42 (scene 4: the hall is only
                    // there); the record, the "look back" stop and B's pulses (1.0 s after this) are the frame's.
                    room.SetBlind(room.RestLine + d < FrontRoomsStructuresRunEndRoom.Length - .01f);
                    room.Done();
                    room.Dirty();
                }
                break;
            }
        }
        speed = dt > 1e-5f ? (d - before) / dt : 0f;
        if (beat == Beat.Tell || beat == Beat.Travel || beat == Beat.Seat)
        {
            island.localPosition = new Vector3(0f, 0f, d);
            room.PlaceHead(room.RestLine + d, beat == Beat.Travel);
        }
        Catches(f);
        LampsTick(room, f);
        StrandsTick(f);
        LatchesTick(f);
        BitsTick(room, f);
        SetMacro(1f - .7f * Mathf.Clamp01(Mathf.Abs(speed) / Cruise));
    }

    void GateTick(FrontRoomsStructuresRunEndRoom room, FrontRoomsStructuresRunEndFrame f)
    {
        if (f.phase != FrontRoomsStructuresRunEndPhase.Tripped) return;
        var rig = room.Rig;
        // In today's look the island waits for the heads (as today's first step: no move in the rig's dark).
        var headsUp = room.Reduce ? rig.reducedHeadDelay + rig.reducedEnvelope : rig.headDelay + rig.headStagger * room.RestLine + rig.headRise;
        var ready = room.Look != 0 || f.clock >= headsUp;
        var looked = !gateOn || f.axisAngle <= FrontRoomsStructuresRunEndRoom.GazeDegrees || f.feet.z >= GateFallback;
        var wasBlocked = blocked;
        blocked = f.feet.z > safeZ && f.feet.z < FrontRoomsStructuresRunEndRoom.HallZ0 - .05f && Mathf.Abs(f.feet.x) < 1.6f && f.feet.y > -1f && f.feet.y < 3f;
        if (blocked)
        {
            blockedTime += f.dt;
            if (blockedTime > .6f && !blockFlashed)
            {
                blockFlashed = true;
                room.Flash("做法 2：你站在车厢开走的路上；退回 FD2 前面（" + safeZ.ToString("0.#") + " 米以内），车厢才会开走。");
            }
        }
        else blockedTime = 0f;
        if (wasBlocked != blocked) room.Dirty();
        if (!ready || !looked || blocked) return;
        beat = Beat.Tell;
        beatClock = 0f;
        room.Waiting = false;
        room.HeadMoving = true;
        latchClock = 0f;
        puffAt = f.time + .03f;
        Grit(f.time, 10, new Vector3(-1.2f, Soffit - .01f, JointZ - .01f), new Vector3(1.2f, Soffit - .01f, JointZ + .02f), .3f, 0);
        room.Dirty();
    }

    // The heavy seat: grit from the deck through the work lamps' light (72 3.2: "the work lights swing and settle; grit falls
    // through their light"), the lamps nudged back by the air the island stops.
    void SeatStarted(FrontRoomsStructuresRunEndRoom room, FrontRoomsStructuresRunEndFrame f)
    {
        var mouth = JointZ + rangeR;
        Grit(f.time, 18, new Vector3(-1.25f, RawSoffit - .01f, mouth - 2.6f), new Vector3(1.25f, RawSoffit - .01f, mouth - .1f), .7f, 1);
        Grit(f.time + .05f, 10, new Vector3(-1.2f, RawSoffit - .01f, JointZ + .5f), new Vector3(1.2f, RawSoffit - .01f, mouth - 2.6f), .9f, 2);
        var amp = (room.Reduce ? SwingReduced : SwingMax) * .3f;
        var w0 = Mathf.Sqrt(9.81f / LampLength);
        for (var k = 0; k < lampCount; k++)
        {
            var l = lamps[k];
            if (l == null || !l.kicked) continue;
            l.rateZ -= amp * w0 * (.7f + .3f * Hash01(k, 41));
        }
    }

    // The island's displacement t seconds after the tell began, as Tick drives it (the tell's jolt, the move, the heavy seat).
    static float IslandAt(float t, float r)
    {
        if (t <= 0f) return 0f;
        if (t < TellTime) return Jolt * Smooth(t / .1f);
        t -= TellTime;
        var length = Mathf.Max(0f, r - Jolt);
        var move = MoveTime(length, Cruise, Ramp, Ramp);
        if (t < move) return Jolt + Move(t, length, Cruise, Ramp, Ramp);
        t -= move;
        if (t < SeatTime)
        {
            var u = t / SeatTime;
            return r + SeatAmp * Mathf.Sin(Mathf.PI * u) * Mathf.Exp(-3f * u);
        }
        return r;
    }

    // The farthest z a walker may stand at when the move begins: a sprint from there stays Clearance short of FD2's leaves
    // until the island has seated (scanned at 10 ms).
    static float SafeFor(float r)
    {
        var total = TellTime + MoveTime(Mathf.Max(0f, r - Jolt), Cruise, Ramp, Ramp) + SeatTime;
        var best = float.MaxValue;
        for (var t = 0f; t <= total + .005f; t += .01f)
            best = Mathf.Min(best, LeafNear + IslandAt(t, r) - FrontRoomsStructuresWalker.Run * t);
        return Mathf.Max(GateFallback, best - Clearance);
    }

    // The band (A/B), the red strike's front (the heads) and the tide (C) catch each of the island's lamps where it is now.
    void Catches(FrontRoomsStructuresRunEndFrame f)
    {
        if (f.phase != FrontRoomsStructuresRunEndPhase.Tripped) return;
        var t = f.clock;
        var relBand = Mathf.Max(1f, BandSpeed - speed);
        var relTide = Mathf.Max(.5f, TideSpeed - speed);
        for (var i = 0; i < islandStrip.Length; i++)
        {
            if (!islandStrip[i]) continue;
            var z = stripZ0[i] + d;
            if (stripBand[i] < 0f)
            {
                var front = BandOrigin + BandSpeed * t;
                if (front >= z) stripBand[i] = Mathf.Max(0f, t - (front - z) / relBand);
            }
            if (stripTide[i] < 0f)
            {
                var front = TideSpeed * (t - TideLag);
                if (front >= z) stripTide[i] = Mathf.Max(TideLag, t - (front - z) / relTide);
            }
        }
        for (var i = 0; i < islandHead.Length; i++)
        {
            if (!islandHead[i]) continue;
            var z = headZ0[i] + d;
            if (headBand[i] < 0f)
            {
                var front = BandSpeed * (t - RedLag);
                if (front >= z) headBand[i] = Mathf.Max(RedLag, t - (front - z) / relBand);
            }
            if (headTide[i] < 0f)
            {
                var front = TideSpeed * (t - TideLag);
                if (front >= z) headTide[i] = Mathf.Max(TideLag, t - (front - z) / relTide);
            }
        }
    }

    // The work lamps: each lights the gap as the island's roof slides off it (about 0.1 s at 8 m/s; Reduce Flashing: a 0.5 s
    // rise), takes the passing island's push once, swings on its cord (a damped pendulum) and drifts gently after.
    void LampsTick(FrontRoomsStructuresRunEndRoom room, FrontRoomsStructuresRunEndFrame f)
    {
        var dt = Mathf.Min(f.dt, .05f);
        var mouth = JointZ + d;
        var w0 = Mathf.Sqrt(9.81f / LampLength);
        var maxSwing = room.Reduce ? SwingReduced : SwingMax;
        var drift = (room.Reduce ? .12f : .5f) * w0 * w0;
        var steps = Mathf.Max(1, Mathf.CeilToInt(dt / .01f));
        var h = dt / steps;
        for (var k = 0; k < lampCount; k++)
        {
            var l = lamps[k];
            if (l == null || l.light == null) continue;
            var open = Smooth((mouth - (l.z - .2f)) / .8f);
            if (open > 0f && l.uncoverAt < 0f) l.uncoverAt = f.time;
            var level = room.Reduce ? (l.uncoverAt < 0f ? 0f : Smooth((f.time - l.uncoverAt) / Fade(room, .12f))) : open;
            if (!l.kicked && open > .5f)
            {
                l.kicked = true;
                var push = maxSwing * Mathf.Clamp01(Mathf.Abs(speed) / Cruise);
                l.rateZ += push * w0 * (.85f + .3f * Hash01(k, 43));
                l.rateX += push * w0 * .35f * (Hash01(k, 47) - .5f);
            }
            // The draught after: half a degree, slow (0.3 Hz noise); with F, about a tenth of a degree.
            var nz = (Mathf.PerlinNoise(f.time * .3f, l.seed) - .5f) * 2f * drift;
            var nx = (Mathf.PerlinNoise(l.seed, f.time * .27f) - .5f) * 2f * drift * .6f;
            for (var s = 0; s < steps; s++)
            {
                l.rateZ += (-w0 * w0 * l.swingZ - 2f * SwingDamping * w0 * l.rateZ + nz) * h;
                l.rateX += (-w0 * w0 * l.swingX - 2f * SwingDamping * w0 * l.rateX + nx) * h;
                l.swingZ += l.rateZ * h;
                l.swingX += l.rateX * h;
            }
            // Never past the push's own amplitude (F: four degrees), whatever the draught adds.
            l.swingZ = Mathf.Clamp(l.swingZ, -maxSwing * 1.15f, maxSwing * 1.15f);
            l.swingX = Mathf.Clamp(l.swingX, -maxSwing * .6f, maxSwing * .6f);
            if (l.pivot != null) l.pivot.localRotation = Quaternion.Euler(-l.swingZ, 0f, l.swingX);
            l.level = level;
            var v = LampIntensity * level;
            l.light.intensity = v;
            l.light.enabled = v > .001f;
        }
        // The bounce rises with the lamps round it (within a cell either side), so it never lights the gap before they do.
        for (var k = 0; k < MaxBounce; k++)
        {
            var b = bounce[k];
            if (b == null) continue;
            var sum = 0f;
            var count = 0;
            for (var j = 0; j < lampCount; j++)
            {
                var l = lamps[j];
                if (l == null || Mathf.Abs(l.z - bounceZ[k]) > 3.1f) continue;
                sum += l.level;
                count++;
            }
            var v = k < bounceCount && count > 0 ? BounceIntensity * sum / count : 0f;
            b.intensity = v;
            b.enabled = v > .001f;
        }
    }

    // The wool: stretched between the halves as the joint opens, each strand tears at its own length, recoils and drops to
    // hang from the near shore's collar, then hangs with a slow sway.
    void StrandsTick(FrontRoomsStructuresRunEndFrame f)
    {
        var gap = d;
        foreach (var s in strands)
        {
            if (s.t == null) continue;
            if (gap < .004f && !s.torn)
            {
                if (s.t.gameObject.activeSelf) s.t.gameObject.SetActive(false);
                continue;
            }
            if (!s.t.gameObject.activeSelf) s.t.gameObject.SetActive(true);
            if (!s.torn)
            {
                var thin = Mathf.Lerp(1f, .45f, Mathf.Clamp01(gap / s.tear));
                Place(s.t, s.anchor, Vector3.forward, gap, s.width * thin, s.thick * thin);
                if (gap >= s.tear)
                {
                    s.torn = true;
                    s.tornAt = f.time;
                    s.hangLength = Mathf.Clamp(.32f * s.tear + .05f, .08f, .40f);
                }
                continue;
            }
            var u = f.time - s.tornAt;
            // The recoil (0.12 s) and the drop to hanging: a spring with a little overshoot, settled by about 0.8 s.
            var drop = 1f - Mathf.Exp(-u * 6.5f) * Mathf.Cos(u * 9f);
            var dir = Vector3.SlerpUnclamped(Vector3.forward, s.hang, Mathf.Clamp(drop, 0f, 1.12f)).normalized;
            var sway = Mathf.Sin(f.time * 1.7f + s.seed) * .05f + (Mathf.PerlinNoise(f.time * .4f, s.seed) - .5f) * .06f;
            dir = (dir + new Vector3(0f, 0f, sway)).normalized;
            var len = Mathf.Lerp(Mathf.Min(s.tear, .6f), s.hangLength, Smooth(u / .12f));
            Place(s.t, s.anchor, dir, len, s.width * .7f, s.thick * .8f);
        }
    }

    static void Place(Transform t, Vector3 anchor, Vector3 dir, float len, float w, float th)
    {
        len = Mathf.Max(.004f, len);
        t.localPosition = anchor + dir * (len * .5f);
        t.localRotation = Quaternion.LookRotation(dir, Mathf.Abs(dir.y) > .95f ? Vector3.forward : Vector3.up);
        t.localScale = new Vector3(w, th, len);
    }

    // The latches flip up at the trip (75 degrees in about 0.08 s, a bounce, settled by 0.25 s), then ride with the island.
    void LatchesTick(FrontRoomsStructuresRunEndFrame f)
    {
        if (latchClock < 0f) return;
        latchClock += f.dt;
        var u = latchClock;
        var a = 75f * (1f - Mathf.Exp(-u * 18f) * Mathf.Cos(u * 38f));
        foreach (var l in latches)
            if (l != null) l.localRotation = Quaternion.Euler(a, 0f, 0f);
        if (u > 1f) latchClock = -1f;
    }

    // ---------------------------------------------------------------- bits

    void Grit(float at, int count, Vector3 lo, Vector3 hi, float spread, int salt)
    {
        var made = 0;
        foreach (var b in bits)
        {
            if (made >= count) break;
            if (b.rust || b.live) continue;
            var k = made + 64 * salt;
            b.live = true;
            b.landed = false;
            b.t0 = at + spread * Hash01(k, 61);
            b.life = 2.6f;   // 71 5.8's grit: "gone within 3 s"
            b.p = new Vector3(Mathf.Lerp(lo.x, hi.x, Hash01(k, 67)), Mathf.Lerp(lo.y, hi.y, Hash01(k, 71)), Mathf.Lerp(lo.z, hi.z, Hash01(k, 73)));
            b.v = new Vector3((Hash01(k, 79) - .5f) * .3f, -.2f * Hash01(k, 83), (Hash01(k, 89) - .3f) * .4f);
            b.size0 = b.size1 = .012f + .014f * Hash01(k, 97);
            b.spin = 200f + 400f * Hash01(k, 101);
            b.axis = new Vector3(Hash01(k, 103) - .5f, Hash01(k, 107) - .5f, Hash01(k, 109) - .5f + .01f).normalized;
            made++;
        }
    }

    void Puff(float at)
    {
        var made = 0;
        foreach (var b in bits)
        {
            if (!b.rust || b.live) continue;
            var k = made;
            b.live = true;
            b.landed = false;
            b.t0 = at + .15f * Hash01(k, 113);
            b.life = 1.2f + .6f * Hash01(k, 127);
            b.p = couplingEnd;
            // Out of the parted pipe's open end into the gap, spreading (a dry main: air and rust scale, no water).
            var spread = new Vector3(Hash01(k, 131) - .5f, Hash01(k, 137) - .65f, 0f) * 1.6f;
            b.v = (Vector3.forward + spread).normalized * (1.2f + 1.3f * Hash01(k, 139));
            b.size0 = .04f + .03f * Hash01(k, 149);
            b.size1 = .22f + .12f * Hash01(k, 151);
            // Each sprite rolled its own way (no two alike), turning slowly as it drifts.
            b.spin = 360f * Hash01(k, 157);
            made++;
        }
    }

    void BitsTick(FrontRoomsStructuresRunEndRoom room, FrontRoomsStructuresRunEndFrame f)
    {
        if (puffAt >= 0f && f.time >= puffAt)
        {
            Puff(puffAt);
            puffAt = -1f;
        }
        var cam = room.View != null ? room.View.transform.forward : Vector3.forward;
        var root = room.Root;
        var camLocal = root != null ? root.InverseTransformDirection(cam) : cam;
        if (camLocal.sqrMagnitude < 1e-6f) camLocal = Vector3.forward;
        var dt = Mathf.Min(f.dt, .05f);
        foreach (var b in bits)
        {
            if (!b.live || b.t == null) continue;
            var age = f.time - b.t0;
            if (age < 0f)
            {
                if (b.t.gameObject.activeSelf) b.t.gameObject.SetActive(false);
                continue;
            }
            if (age > b.life)
            {
                b.live = false;
                b.t.gameObject.SetActive(false);
                continue;
            }
            if (!b.t.gameObject.activeSelf) b.t.gameObject.SetActive(true);
            if (b.rust)
            {
                // Drag (the puff stops within half a metre), a slow settle, growing and fading.
                b.v *= Mathf.Exp(-3.2f * dt);
                b.v += Vector3.down * .5f * dt;
                b.p += b.v * dt;
                var u = age / b.life;
                var size = Mathf.Lerp(b.size0, b.size1, 1f - (1f - u) * (1f - u));
                b.t.localPosition = b.p;
                b.t.localRotation = Quaternion.LookRotation(camLocal, Vector3.up) * Quaternion.Euler(0f, 0f, b.spin + 40f * age);
                b.t.localScale = new Vector3(size, size, 1f);
                var alpha = .55f * Smooth(age / .06f) * (1f - Smooth((u - .35f) / .65f));
                mpb.Clear();
                mpb.SetColor(BaseColorId, new Color(.42f, .22f, .12f, alpha));
                b.r.SetPropertyBlock(mpb);
                continue;
            }
            if (!b.landed)
            {
                b.v += Vector3.down * 9.81f * dt;
                b.v.x *= 1f - .6f * dt;
                b.v.z *= 1f - .6f * dt;
                b.p += b.v * dt;
                var floor = b.p.z < JointZ ? 0f : RawFloor;
                if (b.p.y <= floor + b.size0 * .5f)
                {
                    b.p.y = floor + b.size0 * .5f;
                    b.landed = true;
                }
            }
            var shrink = 1f - Smooth((age - (b.life - .4f)) / .4f);
            b.t.localPosition = b.p;
            if (!b.landed) b.t.localRotation = Quaternion.AngleAxis(b.spin * age, b.axis);
            b.t.localScale = Vector3.one * Mathf.Max(.0005f, b.size0 * shrink);
        }
    }

    // ---------------------------------------------------------------- the macro wear on the moving island

    void SetMacro(float k)
    {
        k = Mathf.Clamp01(k);
        if (Mathf.Abs(k - macroShown) < .01f && !(k >= 1f && macroShown < 1f)) return;
        macroShown = k;
        for (var i = 0; i < macroMats.Count; i++)
        {
            var m = macroMats[i];
            if (m == null) continue;
            m.SetFloat(MacroToneId, macroTone[i] * k);
            m.SetFloat(MacroDirtId, macroDirt[i] * k);
        }
    }

    // ================================================================ the lamps (the island's: today's grammar where it is now)

    public override void Strip(FrontRoomsStructuresRunEndRoom room, int i, ref FrontRoomsStructuresRunEndStrip s)
    {
        if (!built || i < 0 || i >= islandStrip.Length || !islandStrip[i]) return;
        var phase = room.Phase;
        if (phase == FrontRoomsStructuresRunEndPhase.Armed) return;
        var rig = room.Rig;
        var t = room.Clock;
        var z = stripZ0[i] + d;
        if (phase == FrontRoomsStructuresRunEndPhase.Resetting)
        {
            // T's re-strike at the strip's own cell (it walks the near shore, crosses the gap, walks the island).
            var w = room.Restrike(t, z);
            s.white = w;
            s.lens = w;
            s.red = 0f;
            s.tube = 0f;
            return;
        }
        var look = room.Look;
        // Today's look: the mains drop at once everywhere and the red tube strikes with the heads (4 ms a metre): today's drive.
        if (look == 0) return;
        if (look == 3)
        {
            var tc = stripTide[i];
            if (tc < 0f)
            {
                s.white = s.lens = 1f;
                s.red = s.tube = 0f;
                return;
            }
            var k = Smooth((t - tc) / (room.Reduce ? rig.reducedEnvelope : rig.resetRise));
            s.white = 1f - k;
            s.red = k;
            s.lens = 1f - k;
            s.tube = k;
            return;
        }
        var t0 = stripBand[i];
        if (t0 < 0f)
        {
            // The band has not caught it yet: still white.
            s.white = s.lens = 1f;
            s.red = s.tube = 0f;
            return;
        }
        float white, red, lens;
        if (room.Reduce)
        {
            var k = Smooth((t - t0) / rig.reducedEnvelope);
            white = 1f - k;
            red = k;
            lens = white;
        }
        else
        {
            white = room.WhiteDrop(t - t0);
            red = room.RedRise(t - RedLag - t0);
            lens = Mathf.Lerp(room.Afterglow(t - t0), 1f, white);
            // B's pulse at the strip's place now (docked once it pulses: they wait while the island moves), so it skips the gap
            // (no strips there) and lands on the island on time.
            if (look == 2) red *= room.PulseAt(t, z);
        }
        s.white = white;
        s.red = red;
        s.lens = lens;
        s.tube = red;
    }

    public override void Head(FrontRoomsStructuresRunEndRoom room, int i, ref FrontRoomsStructuresRunEndHead h)
    {
        if (!built || i < 0 || i >= islandHead.Length || !islandHead[i]) return;
        if (room.Phase != FrontRoomsStructuresRunEndPhase.Tripped) return;
        var look = room.Look;
        if (look == 0) return;
        var start = look == 3 ? headTide[i] : headBand[i];
        if (start < 0f)
        {
            h.level = 0f;
            return;
        }
        var rig = room.Rig;
        var t = room.Clock;
        if (room.Reduce) h.level = Smooth((t - start) / rig.reducedEnvelope);
        else
        {
            var f = Mathf.Clamp01((t - start) / rig.headRise);
            h.level = f * f;
        }
    }

    // ================================================================ the look

    // In the open gap the ambient is the tripped room's neutral one, not the red looks' or the armed bounce (the gap is lit by
    // its work lamps alone, off the alarm circuit: its darks stay grey under the warm pools); 0.3 m of hysteresis each way, so
    // standing on a boundary never flickers.
    public override FrontRoomsStructuresLook.Preset PresetAt(FrontRoomsStructuresRunEndRoom room, Vector3 local, FrontRoomsStructuresLook.Preset today)
    {
        if (!built) return today;
        var mouth = JointZ + d;
        if (mouth - JointZ < 1.2f)
        {
            if (gapLook) room.ClearAmbient();
            gapLook = false;
            return today;
        }
        var across = Mathf.Abs(local.x) < 1.6f && local.y > -1f && local.y < 4f;
        var was = gapLook;
        gapLook = gapLook
            ? across && local.z > JointZ - .3f && local.z < mouth + .3f
            : across && local.z > JointZ + .3f && local.z < mouth - .3f;
        // In the gap: the tripped room's neutral preset (its reflections and grade) under a dim warm trilight (room.Ambient).
        if (gapLook && !was) room.Ambient(GapSky, GapEquator, GapGround);
        else if (!gapLook && was) room.ClearAmbient();
        return gapLook ? FrontRoomsStructuresLook.Preset.RunBTripped : today;
    }

    // ================================================================ helpers

    // A dress piece that is one box (FrontRoomsFilmMesh's planar box: 24 vertices, 12 triangles), copied as its bounds.
    static bool IsBoxPiece(Renderer r)
    {
        var mf = r != null ? r.GetComponent<MeshFilter>() : null;
        var m = mf != null ? mf.sharedMesh : null;
        if (m == null) return true;
        return m.subMeshCount == 1 && m.vertexCount <= 24 && m.GetIndexCount(0) == 36;
    }

    // A district box from two corners (a few mm out, so a face on the line is kept whole).
    static Bounds CutBox(Vector3 a, Vector3 b)
    {
        var bounds = new Bounds((a + b) * .5f, Vector3.zero);
        bounds.SetMinMax(Vector3.Min(a, b) - Vector3.one * .002f, Vector3.Max(a, b) + Vector3.one * .002f);
        return bounds;
    }

    // District bounds of a renderer from its mesh and transform (exact for the dress's axis-aligned slabs and tiles).
    static bool DistrictBounds(Renderer r, Vector3 site, out Vector3 lo, out Vector3 hi)
    {
        lo = hi = Vector3.zero;
        if (r == null) return false;
        var mf = r.GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            var b = mf.sharedMesh.bounds;
            var m = r.transform.localToWorldMatrix;
            var a = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var c = -a;
            for (var k = 0; k < 8; k++)
            {
                var corner = b.center + Vector3.Scale(b.extents, new Vector3((k & 1) == 0 ? -1f : 1f, (k & 2) == 0 ? -1f : 1f, (k & 4) == 0 ? -1f : 1f));
                var w = m.MultiplyPoint3x4(corner);
                a = Vector3.Min(a, w);
                c = Vector3.Max(c, w);
            }
            lo = a - site;
            hi = c - site;
            return true;
        }
        var rb = r.bounds;
        lo = rb.min - site;
        hi = rb.max - site;
        return true;
    }

    // Boxes batched per material into meshes whose UVs are FrontRooms/Surface's own world planar frame (per face t and b,
    // Cross(t, b) = n: floors x and -z, walls along the wall with v up) at the rest pose, so a mesh-UV copy of a world-
    // projected surface draws the same texels until the mesh moves, and then carries them.
    sealed class Batch
    {
        sealed class Group
        {
            public Material material;
            public bool shadows;
            public readonly List<Vector3> v = new List<Vector3>();
            public readonly List<Vector3> n = new List<Vector3>();
            public readonly List<Vector2> uv = new List<Vector2>();
            public readonly List<int> t = new List<int>();
        }

        readonly List<Group> groups = new List<Group>();
        readonly Vector3 world;

        public Batch(Vector3 world)
        {
            this.world = world;
        }

        Group Get(Material m, bool shadows)
        {
            foreach (var g in groups) if (g.material == m && g.shadows == shadows) return g;
            var ng = new Group { material = m, shadows = shadows };
            groups.Add(ng);
            return ng;
        }

        public void Box(Vector3 a, Vector3 b, Material m, bool shadows)
        {
            if (m == null) return;
            var lo = Vector3.Min(a, b);
            var hi = Vector3.Max(a, b);
            var s = hi - lo;
            if (s.x < 1e-4f || s.y < 1e-4f || s.z < 1e-4f) return;
            var g = Get(m, shadows);
            var c = (lo + hi) * .5f;
            var h = s * .5f;
            Face(g, c + Vector3.right * h.x, Vector3.right, Vector3.back, Vector3.up, h.z, h.y);
            Face(g, c + Vector3.left * h.x, Vector3.left, Vector3.forward, Vector3.up, h.z, h.y);
            Face(g, c + Vector3.up * h.y, Vector3.up, Vector3.right, Vector3.back, h.x, h.z);
            Face(g, c + Vector3.down * h.y, Vector3.down, Vector3.right, Vector3.forward, h.x, h.z);
            Face(g, c + Vector3.forward * h.z, Vector3.forward, Vector3.right, Vector3.up, h.x, h.y);
            Face(g, c + Vector3.back * h.z, Vector3.back, Vector3.left, Vector3.up, h.x, h.y);
        }

        // A face at c with normal n = Cross(t, b), front-facing for (0, 1, 2) (0, 2, 3) as the structures mesh winds them.
        void Face(Group g, Vector3 c, Vector3 n, Vector3 t, Vector3 b, float ht, float hb)
        {
            var i = g.v.Count;
            Add(g, c - t * ht - b * hb, n, t, b);
            Add(g, c + t * ht - b * hb, n, t, b);
            Add(g, c + t * ht + b * hb, n, t, b);
            Add(g, c - t * ht + b * hb, n, t, b);
            g.t.Add(i);
            g.t.Add(i + 1);
            g.t.Add(i + 2);
            g.t.Add(i);
            g.t.Add(i + 2);
            g.t.Add(i + 3);
        }

        void Add(Group g, Vector3 p, Vector3 n, Vector3 t, Vector3 b)
        {
            g.v.Add(p);
            g.n.Add(n);
            var w = p + world;
            g.uv.Add(new Vector2(Vector3.Dot(w, t), Vector3.Dot(w, b)));
        }

        public void Finish(Transform parent, string name, FrontRoomsStructuresRunEndRoom room)
        {
            foreach (var g in groups)
            {
                if (g.v.Count == 0) continue;
                var mesh = new Mesh { name = "SP7 #2 " + name + " " + g.material.name, hideFlags = HideFlags.DontSave };
                if (g.v.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
                mesh.SetVertices(g.v);
                mesh.SetNormals(g.n);
                mesh.SetUVs(0, g.uv);
                mesh.SetTriangles(g.t, 0);
                mesh.RecalculateTangents();
                mesh.RecalculateBounds();
                room.Own(mesh);
                var go = new GameObject(name + " - " + g.material.name);
                go.transform.SetParent(parent, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = g.material;
                r.shadowCastingMode = g.shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            }
            groups.Clear();
        }
    }
}
