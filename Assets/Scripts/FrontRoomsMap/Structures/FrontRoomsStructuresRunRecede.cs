using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

/// <summary>
/// SP7 (key 9) of scene 4 (RULES 5b; research/level_space/71_run_room.md section 10, agreed by Red on 2026-11-03 at 23:18
/// in the level-space chat, decision 9 of 71 section 8; built from 71 until the level-space line moves it into 70 as SP7):
/// Run!, the door that backs away. Run! B's service corridor at 14 cells (42 m) in the game's own production dress
/// (FrontRoomsRunDress over a 1 x 14 RunRigLayout: VCT lane and accents, two-tone block, cove, bumper rail, sprinkler main,
/// tray, EMT, the remote circuit, the pull station, bell and extinguisher at Run! B's z), entered from one Level 0 cell
/// through Run! B's arch or a Run door (O), with the game's ALARM ZONE card and detector (FrontRoomsAlarmTell).
/// The end is a movable head (block-faced end wall, RN-F goal door with its F5 lite and panic bar, a twin-head unit on a
/// short bracket aimed back at the leaf, C42's light in a box behind the lite, trolleys on a surface track in the soffit
/// corners from FD1 to the end, a guide slot, sweeps) standing at the variant's line: plain 7, 9, 12 or 14 cells, or
/// receding 9 -> 12 or 9 -> 14 (V). Receding rooms trip at the FD1 plane (Order T) and run 71 section 4.5's schedule from
/// the trip (gaze gate 35 degrees, fallback at 18 m; a two-cell flinch, one-cell steps, a heavy final seat; the keep-away
/// glide at 6 m/s when the walker is within 12 m of the head between steps); T1 rolls the whole end, T2 takes the wall
/// apart course by course and re-stacks it round the door (B). The cells behind the head are built, their dressing and
/// lamps off until a seat reveals them; ghost bands mark every line the head stood on; the record stays until Y. Light
/// (a structures-only driver, scene 4 only): Run! B's armed and tripped states with the rig's own timing (read from a fresh
/// RunRigLayout), looks A, B and C with 71 section 6.3-6.4's numbers on a third red tube per strip (a greybox emissive
/// cylinder), section 5.7's grammar in receding rooms, the reset re-strike from the entrance, Reduce Flashing (F, local
/// to this district: the game's setting is never read or written). Beyond the 14th cell a Tall release hall lit like SP5's
/// dock; its frame holds a blind panel until the head seats there. A static Relay stand-in at x -0.6, z 6. No sound, no
/// moving Relay. Greybox where no kit exists, and the sign says so.
/// Treatments (X; research/level_space/72_unreachable_end.md section 3, Red 2026-11-04: make each of them in scene 4): X cycles
/// 72's five ends (Shift+X back) and resets the room; 1 is today's SP7 and runs the code above unchanged (no hook is called).
/// Treatments 2-5 (FrontRoomsStructuresRunEnd*, one file each) act in the receding variants: the frame trips them at FD1 and
/// hands them FrontRoomsStructuresRunEndRoom (the head, the cells and their reveal, the bands, the lamps through holds and
/// hooks, the walker, the fog, the hall); today's schedule does not run under them. A second placard on the vestibule's west
/// wall lists the five; the plate names the current one. N goes to the current treatment's own option.
/// </summary>
public sealed class FrontRoomsStructuresRunRecede : FrontRoomsStructuresDistrict
{
    const float HalfW = 1.42f, Ceil = 2.9f, CellLen = 3f, Length = 42f, HeadTop = 2.88f, HeadHalf = 1.40f;
    const float HallZ0 = 42.1f, HallZ1 = 51f, HallCeil = 5.4f, HallX0 = -1.5f, HallX1 = 7.5f, HallWallZ = 42.18f, VestZ0 = -6f, Band = 2.25f;
    const float Fd1 = 9f, Fd2 = 18f, GazeDegrees = 35f, KeepAway = 12f, GlideSpeed = 6f, Ramp = .175f;
    const int Cells = 14;
    static readonly Vector3 Arrival = new Vector3(0f, 0f, -2f);
    /// <summary>71 section 6.3: alarm red in linear (1.00, 0.02, 0.01); its sRGB is (1.000, 0.152, 0.100).</summary>
    static readonly Color AlarmRedLinear = new Color(1f, .02f, .01f);
    static readonly Color C42 = new Color(1f, .96f, .88f);
    /// <summary>The red soffit halo's peak at the level Red picked on the HDR still (11-04, card zone_lamps 1, verified in the
    /// lamps chat: "add, the brighter level", then the brightest column: tube linear 3, strip Light 3.5, halo .40); the game's
    /// rig has the tube, the room's red light and halos are the game visual line's card 3, imitated here.</summary>
    static readonly Color RedHaloPeak = new Color(.40f, .004f, .003f);
    static readonly float[] RestLine = { 21f, 27f, 36f, 42f, 27f, 27f };
    static readonly float[] FinalLine = { 21f, 27f, 36f, 42f, 36f, 42f };
    static readonly string[] VariantNames = { "普通 7 格（21 米）", "普通 9 格（27 米，今天的长度）", "普通 12 格（36 米）", "普通 14 格（42 米）", "会退 9→12 格", "会退 9→14 格" };
    static readonly string[] LookNames = { "今天的样子", "A 警报回路", "B 警报脉冲", "C 红潮" };
    static readonly float[] BandLines = { 21f, 27f, 33f, 36f, 39f };
    static readonly float[] MagnetLines = { 21f, 27f, 33f, 36f, 39f, 42f };

    // ---------------------------------------------------------------- settings (keys)
    int variant = 5;            // 9 -> 14 (the agreed version at T5; 70 section 9 opens on it)
    int look = 2;               // B on arrival, Red's pick (70 section 9, 71 decision 6); A its fallback; today's and C by L
    bool bodyT2 = true;         // T1 head carrying T2 on desktop (decision 5)
    bool doorEntrance;          // arch (today) or the Run door (decision 8c), plain variants only
    bool reduce;                // Reduce Flashing, local to this district
    bool tube = true;           // the red tube (H): on, Red's yes of 11-04 (70 section 10); off shows the 11-03 strip
    // B, L, H and O act when the room is next armed (at once if it is): no key changes the light of a tripped room (70 section 9).
    int pendingLook = -1, pendingBody = -1, pendingTube = -1, pendingDoor = -1;
    bool tellShown;

    // ---------------------------------------------------------------- state
    enum Phase { Armed, Tripped, Resetting }
    enum Beat { Tell, Travel, Seat, Dwell, Glide, Done }
    Phase phase = Phase.Armed;
    float clock;
    bool recedePending, recedeRunning, receded;
    float headZ;
    float lastWalkerZ = -10f;
    float finalSeatTime = -1f;
    float stutterClock, shadowClock;
    Beat beatPhase = Beat.Done;
    int beatIndex;
    float beatClock, glideFrom, glideTime;
    bool travelT2;
    readonly List<Step> steps = new List<Step>();
    readonly List<float> seatedLines = new List<float>();
    readonly bool[] cellOpen = new bool[Cells];      // lit and solid (from a step's seat)
    readonly bool[] cellShown = new bool[Cells];     // drawn (from the tell of the step that uncovers it)
    readonly float[] revealAt = new float[Cells];
    bool plateDirty = true;

    // ---------------------------------------------------------------- treatments (X): 72 section 3's five ends
    // 0..4 = #1..#5. #1 (0) is today's SP7: activeEnd stays null and every line of today's code runs as before.
    readonly FrontRoomsStructuresRunEnd[] ends =
    {
        new FrontRoomsStructuresRunEndToday(),
        new FrontRoomsStructuresRunEndFarShore(),
        new FrontRoomsStructuresRunEndLightLeaves(),
        new FrontRoomsStructuresRunEndLookBack(),
        new FrontRoomsStructuresRunEndDreamRun(),
    };
    readonly bool[] endBuilt = new bool[5], endFailed = new bool[5];
    int treatment;                          // X: the treatment picked (0 = #1, today's)
    FrontRoomsStructuresRunEnd activeEnd;   // the treatment driving the room now, or null (today's code)
    RoomLink room;
    bool endMoving, endWaiting;             // the treatment's end moves / waits to start (B's pulses wait, the door stays shut)
    bool endTouched, endFault, partOpen, hallTouched;
    float endTime;
    readonly List<float> endLines = new List<float>();
    // The walker as a treatment reads it (frame.velocity, frame.own, the gait) and the pushes it gave (room.Push).
    Vector3 lastFeet, pushed;
    bool feetKnown;
    float lastStamina = 1f;
    CharacterController walkerBody;
    // Lamp holds (room.HoldStrip / HoldHead) and what each lamp showed last frame.
    StripHold[] stripHolds;
    HeadHold[] headHolds;
    FrontRoomsStructuresRunEndStrip[] stripShown;
    FrontRoomsStructuresRunEndHead[] headShown;
    // The fog override (room.Fog), applied after SP7's preset.
    bool fogOn;
    Color fogColour, savedFogColour;
    float fogDensity, savedFogDensity;
    // The frame's shared objects as they stood before any treatment ran (put back before every reset once one has run).
    bool snapped;
    readonly List<TransformSnap> transformSnaps = new List<TransformSnap>();
    readonly List<RendererSnap> rendererSnaps = new List<RendererSnap>();
    readonly List<LightSnap> lightSnaps = new List<LightSnap>();
    readonly List<ColliderSnap> colliderSnaps = new List<ColliderSnap>();
    // The treatments' requests (integration of 11-04): a walker where restored solids come back is jumped home; the goal
    // unit, the panic bar and the head's machine as handles; the head wall's pieces; the marks' words; the stand-in room behind
    // a short-opening door; the ambient override; B's pulse eased under a treatment; the textures a treatment made.
    bool walkerBlocked;
    readonly List<Collider> restoredColliders = new List<Collider>();
    readonly Collider[] overlapBuffer = new Collider[32];
    Transform goalUnit, goalBar, headMachine, shortRoom;
    readonly List<Rect> wallPieces = new List<Rect>();
    readonly List<Rect> slotRects = new List<Rect>();
    readonly List<TextMesh> markTexts = new List<TextMesh>();
    bool marksTouched;
    StripLamp shortLamp;
    Light shortFill;
    bool shortFailed;
    float shortLevel;
    readonly List<Texture> ownedTextures = new List<Texture>();
    bool ambientOn;
    Color ambientSky, ambientEquator, ambientGround, savedSky, savedEquator, savedGround, savedAmbientLight;
    float pulseGateFrom, pulseGateTarget, pulseGateT0 = float.NegativeInfinity;
    float pulseFirstNow = 3f, pulseFirstOld = 3f, pulseFirstT0 = float.NegativeInfinity;
    HeadLamp probeHead;

    struct Step
    {
        public float from, to, tell, travel, seat, dwell, amp;
        public bool final;
    }

    // ---------------------------------------------------------------- built things
    RunRigLayout rig;
    FrontRoomsStructuresKit kit;
    Material lower, upper, floorMat, soffitMat, cove, frameMat, leafMat, rubber, redTube, lensStandIn;
    readonly List<Material> owned = new List<Material>();
    readonly List<Renderer>[] cellRenderers = new List<Renderer>[Cells];
    readonly List<GameObject>[] cellObjects = new List<GameObject>[Cells];
    readonly List<StripLamp> strips = new List<StripLamp>();
    readonly List<StripLamp> hallStrips = new List<StripLamp>();
    readonly List<HeadLamp> heads = new List<HeadLamp>();
    readonly List<Emissive> pilots = new List<Emissive>();
    HeadLamp[] goalHeads = new HeadLamp[0];
    Transform head, archPart, doorPart, trackPart, blindPart, blocksRoot;
    readonly Dictionary<float, GameObject> bandParts = new Dictionary<float, GameObject>();
    readonly Dictionary<float, GameObject> magnets = new Dictionary<float, GameObject>();
    readonly List<Renderer> headWall = new List<Renderer>();
    readonly List<Block> blocks = new List<Block>();
    readonly List<Transform> sweeps = new List<Transform>();
    Transform entranceLeaf, goalLeaf;
    Collider entranceLeafCollider, goalLeafCollider;
    bool entranceOpen, goalOpen;
    GameObject archDetector, doorDetector;
    TextMesh plateText;
    // For the treatments: the dress's root and pieces, the fire-door frames as parts, C42's glow, the treatments' placard, the
    // vestibule's Level 0 ambient (for the placard rebuilt on X).
    Transform dressRoot;
    readonly List<Renderer> dressRenderers = new List<Renderer>();
    readonly List<Renderer> longShell = new List<Renderer>();
    readonly List<Collider> dressColliders = new List<Collider>();
    readonly Transform[] fireFrames = new Transform[2];
    Renderer goalLite;
    GameObject endSign;
    SphericalHarmonicsL2 vestibuleSH;
    bool vestibuleSHSet;
    MaterialPropertyBlock block;
    Material haloWhite, haloRed;
    static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
    static readonly int DensityId = Shader.PropertyToID("_Density");
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    sealed class StripLamp
    {
        public float z;
        public int cell = -1;
        public GameObject root;
        public Light light;
        public Renderer lens, red, halo;
        public bool stutter;
        public float scale = 1f;
    }

    sealed class HeadLamp
    {
        public float z;
        public int cell = -1;
        public Light light;
        public Renderer[] lens;
        public int[] lensIndex;
        public float baseIntensity;
        public bool goal;
    }

    sealed class Emissive
    {
        public Renderer renderer;
        public int index;
        public int cell = -1;
    }

    sealed class Block
    {
        public Transform t;
        public Vector3 seat;       // head-local centre (x, y; z 0 = the wall's centre plane)
        public float row01;        // 0 bottom course .. 1 top course
        public float side;         // -1 the west stream, +1 the east stream
        public float laneX, laneY;
        public float wobble;
    }

    public FrontRoomsStructuresRunRecede(Vector3 site)
        : base(9, "SP7", "SP7 · Run!：会往后退的门", "Run! B 的走廊做到 14 格，尽头的墙和门会往后退。", site)
    {
        for (var c = 0; c < Cells; c++)
        {
            cellRenderers[c] = new List<Renderer>();
            cellObjects[c] = new List<GameObject>();
        }
    }

    bool Receding => variant >= 4;
    float Rest => RestLine[variant];
    float Final => FinalLine[variant];
    float ArmZ => Receding && !receded ? Fd1 : doorEntrance ? 2.5f : 1.5f;
    // The look in force: with the red tube off the light is today's (70 section 9: the tube is the red looks' source).
    int Look => tube ? look : 0;
    static int CellOf(float z) => Mathf.Clamp(Mathf.FloorToInt(z / CellLen), 0, Cells - 1);

    // ---------------------------------------------------------------- build

    public override void Build(FrontRoomsStructures host)
    {
        try { rig = new RunRigLayout(); }
        catch (Exception e) { FrontRoomsPoolMaterials.LogOnce("SP7 rig", "SP7: the Run! rig's numbers could not be read: " + e.Message); rig = null; }
        if (rig == null) rig = new RunRigLayout();
        block = new MaterialPropertyBlock();
        kit = new FrontRoomsStructuresKit(host, this);
        room = new RoomLink(this);
        lower = FrontRoomsPoolMaterials.Surface("RunB_BlockLower", new Color(.55f, .56f, .55f));
        upper = FrontRoomsPoolMaterials.Surface("RunB_BlockUpper", new Color(.78f, .78f, .74f));
        floorMat = FrontRoomsPoolMaterials.Surface("RunB_Floor", new Color(.62f, .61f, .58f));
        soffitMat = FrontRoomsPoolMaterials.Surface("RunB_Soffit", new Color(.7f, .7f, .68f));
        cove = FrontRoomsPoolMaterials.Surface("RunB_Cove", new Color(.3f, .3f, .3f));
        frameMat = FrontRoomsPoolMaterials.Surface("Prop_SteelBrown", new Color(.16f, .12f, .09f));   // Run! B's dark-bronze casing (review SP7-space-F5)
        leafMat = FrontRoomsPoolMaterials.Surface("Door_Enamel", new Color(.55f, .52f, .45f));
        rubber = FrontRoomsPoolMaterials.Lit("SP7 rubber sweep", new Color(.07f, .07f, .07f), .25f);
        redTube = FrontRoomsPoolMaterials.Emissive("SP7 red tube (greybox)", new Color(.20f, .025f, .02f), .75f);
        lensStandIn = FrontRoomsPoolMaterials.Emissive("SP7 strip lens (stand-in)", new Color(.85f, .86f, .88f), .6f);
        for (var c = 0; c < Cells; c++) revealAt[c] = float.NegativeInfinity;

        kit.Safe("vestibule", BuildVestibule);
        if (!kit.Safe("Run! B dress", BuildDress)) kit.Safe("corridor shell (fallback)", BuildShellFallback);
        kit.Safe("entrance: arch", BuildArch);
        kit.Safe("entrance: Run door", BuildRunDoor);
        kit.Safe("fire-door frames", BuildFireFrames);
        kit.Safe("recede track", BuildTrack);
        kit.Safe("ghost bands", BuildBands);
        kit.Safe("release hall", BuildHall);
        kit.Safe("head", BuildHead);
        kit.Safe("T2 blocks", BuildBlocks);
        kit.Safe("lamps", BuildLamps);
        kit.Finish();
        kit.Safe("zone volume", () => FrontRoomsPostStack.EnsureZoneVolume(Root, "Run_B", new Bounds(new Vector3(3f, 2.7f, 25.65f), new Vector3(9f, 5.4f, 50.7f)), 1f));
        kit.Safe("signs", BuildSigns);
        kit.Safe("vestibule ambient", VestibuleAmbient);

        Box = new Bounds(Site + new Vector3(3f, 2.5f, 22.5f), new Vector3(16f, 10f, 62f));
        LowestFloor = Site.y;
        // The game's far clip for a Run! room (71 section 3.2; 70 section 9): the 14-cell end, 44 m off, shows as in the game.
        Far = 46f;
        Stops.Clear();
        kit.Stop("到达点：入口外 2 米", Arrival, new Vector3(0f, 1.62f, 27f));
        kit.Stop("FD1 门框前（会退的房间在门框处触发）", new Vector3(0f, 0f, 6.9f), new Vector3(0f, 1.62f, 27f));
        kit.Stop("尽头前回头看", new Vector3(.5f, 0f, Rest - 2.5f), new Vector3(-.6f, 1.4f, 6f));
        kit.Stop("释放大厅（门的另一边）", new Vector3(3f, 0f, 47.5f), new Vector3(0f, 1.3f, 42f));
        kit.Stop("入口外 5 米（看 Run 门）", new Vector3(0f, 0f, -5f), new Vector3(0f, 1.4f, 0f));
        // Review SP7-light-F9: the dark red tube read about 3 px from the arrival; review SP7-space-F11: no stop before the head.
        kit.Stop("红灯管（H）：抬头看", new Vector3(0f, 0f, 3.6f), new Vector3(0f, 2.9f, 4.8f));
        kit.Stop("端头前 4 米", new Vector3(0f, 0f, Rest - 4f), new Vector3(0f, 2.2f, Rest));
        ResetRoom(false);
    }

    // Level 0's trilight on the vestibule's own renderers (everything wholly south of z 0.01: its carpet, wallpaper, lid,
    // troffers, the sign and the plate), as per-renderer SH, so its colours stay Level 0's while the scene's ambient is the
    // corridor's (review SP7-light-F1).
    void VestibuleAmbient()
    {
        var sh = FrontRoomsStructuresLook.TrilightSH(FrontRoomsLook.AmbientSky, FrontRoomsLook.AmbientEquator, FrontRoomsLook.AmbientGround);
        vestibuleSH = sh;
        vestibuleSHSet = true;
        var block = new MaterialPropertyBlock();
        var count = 0;
        foreach (var r in Root.GetComponentsInChildren<Renderer>(true))
        {
            if (r == null || r.bounds.max.z > Site.z + .01f) continue;
            r.lightProbeUsage = LightProbeUsage.CustomProvided;
            r.GetPropertyBlock(block);
            block.CopySHCoefficientArraysFrom(new[] { sh });
            r.SetPropertyBlock(block);
            block.Clear();
            count++;
        }
        Debug.Log(FrontRoomsStructures.Log + "SP7 vestibule: Level 0 ambient on " + count + " renderers.");
    }

    // The vestibule (70 section 9): two Standard Level 0 cells (the zone that hosts the room; two deep so the Run door reads
    // from 5 m, as a runner must), L0 surfaces, one L03 troffer per cell.
    void BuildVestibule()
    {
        var paper = FrontRoomsPoolMaterials.Surface("L0_Wallpaper", new Color(.80f, .74f, .48f));
        var carpet = FrontRoomsPoolMaterials.Surface("L0_Carpet", new Color(.55f, .49f, .30f));
        var tiles = FrontRoomsPoolMaterials.Surface("L0_Ceiling", new Color(.86f, .83f, .70f));
        kit.Shell(Ceil, 0f);
        kit.Slab(-1.5f, VestZ0, 1.5f, 0f, 0f, carpet, .2f);
        kit.WallZ(-1.5f, VestZ0 - .08f, 0f, 0f, Ceil, paper);
        kit.WallZ(1.5f, VestZ0 - .08f, 0f, 0f, Ceil, paper);
        kit.WallX(VestZ0, -1.58f, 1.58f, 0f, Ceil, paper);
        kit.Lid(-1.5f, VestZ0, 1.5f, 0f, Ceil, tiles);
        kit.Troffer(new Vector3(0f, Ceil, -4.5f + FrontRooms.Map.ModuleUnits.TrofferOffsetZ), true);
        kit.Troffer(new Vector3(0f, Ceil, -1.5f + FrontRooms.Map.ModuleUnits.TrofferOffsetZ), true);
    }

    // The corridor in the game's own Run! B production dress, 1 x 14 cells; the end walls are this district's (the entrance
    // parts and the movable head). Small pieces are sorted into the cell they start in, so a hidden cell's dressing stays off
    // until a seat reveals it; the long shell pieces (floor, walls, soffit deck) are always on.
    void BuildDress()
    {
        var l = new RunRigLayout
        {
            name = "SP7 corridor 1x14",
            halfWidth = HalfW, length = Length, height = Ceil,
            units = Units(),
            buildShell = true, skinShell = false,
            floorX0 = -HalfW, floorX1 = HalfW, floorZ0 = 0f, floorZ1 = Length,
            laneHalf = .45f, accentRowsX = new[] { -.6f, .6f },
            mainX = .55f, trayX = -.60f, emtWallX = -HalfW, runZ0 = 0f, runZ1 = Length,
            sideDoors = new RunSideDoor[0],
            obstacles = new RunObstacle[0],
        };
        l.walls = new[]
        {
            new RunWallFace { start = new Vector3(-HalfW, 0f, 0f), along = Vector3.forward, length = Length, normal = Vector3.right, gaps = new Vector2[0] },
            new RunWallFace { start = new Vector3(HalfW, 0f, 0f), along = Vector3.forward, length = Length, normal = Vector3.left, gaps = new Vector2[0] },
        };
        // Run! B's fire gear at its own z (Corridor1x9's wall items); the NOTICE plate goes with each entrance part.
        l.wallItems = new[]
        {
            new RunWallItem { kit = "Kit_PullStation", point = new Vector3(HalfW, 1.2f - .09f, 1f), normal = Vector3.left },
            new RunWallItem { kit = "Kit_FireBell", point = new Vector3(HalfW, 2.3f, 1f), normal = Vector3.left },
            new RunWallItem { kit = "Kit_ExtinguisherCabinet", point = new Vector3(-HalfW, 1.1f - .305f, 10.2f), normal = Vector3.right },
        };
        var dress = FrontRoomsRunDress.Build(Root, l);
        if (dress == null) throw new InvalidOperationException("the Run! B dress returned nothing");
        dressRoot = dress;
        var origin = Root.position;
        foreach (var r in dress.GetComponentsInChildren<Renderer>(true))
        {
            if (r == null) continue;
            dressRenderers.Add(r);
            var b = r.bounds;
            if (b.size.z > 4.5f) { longShell.Add(r); continue; }
            var z0 = b.min.z - origin.z;
            cellRenderers[CellOf(z0 + .05f)].Add(r);
        }
        dressColliders.AddRange(dress.GetComponentsInChildren<Collider>(true));
    }

    // The tripped heads (70 section 9; Red's "TA and TB combined"): a T-A unit at z 3 on the east wall; T-B remote heads at z 9
    // (on FD1's north face, over the frame's west half), 15 east, 22.5 west, 28.5 east, 34.5 west and 40.5 east; the T-A goal
    // unit rides the head. Run! B's own pattern holds up to FD2; past it the heads sit mid-cell, so none stands on a line the
    // end stops at. Unit 1 is listed on the west wall at z 9.2 for the dress's remote circuit (it reaches FD1's corner there).
    static readonly float[] UnitZ = { 3f, 9.2f, 15f, 22.5f, 28.5f, 34.5f, 40.5f };

    static RunUnitDef[] Units()
    {
        var u = new RunUnitDef[UnitZ.Length];
        for (var k = 0; k < u.Length; k++)
            u[k] = new RunUnitDef { z = UnitZ[k], side = k % 2 == 0 ? 1f : -1f, autoAim = true, remote = k > 0 };
        u[0].trimPlus = -.23f;
        return u;
    }

    void BuildShellFallback()
    {
        FrontRoomsPoolMaterials.LogOnce("SP7 dress", "SP7: the Run! B dress failed, a plain shell in its surfaces stands in.");
        kit.Shell(Ceil, 0f);
        kit.Slab(-1.5f, 0f, 1.5f, Length, 0f, floorMat, .2f);
        kit.WallZ(-1.5f, 0f, Length, 0f, Ceil, upper);
        kit.WallZ(1.5f, 0f, Length, 0f, Ceil, upper);
        kit.Lid(-1.5f, 0f, 1.5f, Length, Ceil, soffitMat);
        kit.Box(-1.42f, 0f, 0f, -1.418f, 1.2f, Length, lower, false);
        kit.Box(1.418f, 0f, 0f, 1.42f, 1.2f, Length, lower, false);
    }

    // ---------------------------------------------------------------- the entrance (O): Run! B's arch, or the Run door

    void BuildArch()
    {
        archPart = kit.BeginPart("SP7 entrance: Run! B arch");
        EntranceWall(.7f, 2.2f);
        try
        {
            FrontRoomsAlarmTell.DressCard(archPart, FrontRoomsAlarmTell.CardPoint(1.4f, .16f, -1), 180f);
            archDetector = FrontRoomsAlarmTell.DressCeiling(archPart, new Vector3(0f, Ceil, FrontRoomsAlarmTell.DetectorDepth(.16f, Ceil, 2.2f, Length)), 180f);
        }
        catch (Exception e) { FrontRoomsPoolMaterials.LogOnce("SP7 tell", "SP7: the ALARM ZONE tell failed: " + e.Message); }
        Notice(archPart, new Vector3(Mathf.Min(.7f + .4f, HalfW - .15f), 1.5f, -.08f));
        kit.EndPart();
    }

    void BuildRunDoor()
    {
        doorPart = kit.BeginPart("SP7 entrance: Run door (RN-F + F5)");
        EntranceWall(.5f, 2.1f);
        Frame(0f, 0f);
        var hinge = Leaf(doorPart, new Vector3(-.5f, 0f, 0f), "Run door leaf (E opens)", false, out entranceLeafCollider);
        entranceLeaf = hinge;
        try
        {
            FrontRoomsAlarmTell.DressCard(doorPart, FrontRoomsAlarmTell.CardPoint(1f, .16f, 1), 180f);
            doorDetector = FrontRoomsAlarmTell.DressCeiling(doorPart, new Vector3(0f, Ceil, FrontRoomsAlarmTell.DetectorDepth(.16f, Ceil, 2.1f, Length)), 180f);
        }
        catch (Exception e) { FrontRoomsPoolMaterials.LogOnce("SP7 tell door", "SP7: the ALARM ZONE tell at the door failed: " + e.Message); }
        Notice(doorPart, new Vector3(-1.0f, 1.5f, -.08f));
        kit.EndPart();
        doorPart.gameObject.SetActive(false);
    }

    // The entrance wall on z 0: wallpaper on the vestibule's half, Run! B's upper tone on the corridor's half, an opening of
    // half width w and top h, the lower tone and cove on the corridor face.
    void EntranceWall(float w, float h)
    {
        var paper = FrontRoomsPoolMaterials.Surface("L0_Wallpaper", new Color(.80f, .74f, .48f));
        var open = new List<FrontRoomsStructuresKit.Opening> { new FrontRoomsStructuresKit.Opening(-w, w, 0f, h) };
        kit.Shell(Ceil, 0f);
        kit.WallX(-.04f, -1.5f, 1.5f, 0f, Ceil, paper, open, .08f);
        kit.WallX(.04f, -1.5f, 1.5f, 0f, Ceil, upper, open, .08f);
        var edge = w + (h < 2.15f ? .06f : 0f);
        kit.Box(-HalfW, 0f, .081f, -edge, 1.2f, .083f, lower, false);
        kit.Box(edge, 0f, .081f, HalfW, 1.2f, .083f, lower, false);
        kit.Box(-HalfW, 0f, .083f, -edge, .102f, .086f, cove, false);
        kit.Box(edge, 0f, .083f, HalfW, .102f, .086f, cove, false);
    }

    void Notice(Transform parent, Vector3 at)
    {
        try { FrontRoomsKitLibrary.Spawn("Kit_SafetyPlate_10x7", parent, at, Quaternion.LookRotation(Vector3.back), null, false, "run b NOTICE plate", false); }
        catch (Exception e) { FrontRoomsPoolMaterials.LogOnce("SP7 notice", "SP7: the NOTICE plate failed: " + e.Message); }
    }

    // A door frame (dark bronze casing) on a 1.0 x 2.1 opening centred at x, on the wall plane z (district metres). Its inner
    // faces stand CasingStop proud of the opening's jambs and head (render stage 11-04: flush, they shared the wall opening's
    // faces and z-fought in stair steps whenever a jamb was seen at a slant; render-only, the opening's collider is the wall's).
    const float CasingStop = .004f;

    void Frame(float x, float z)
    {
        kit.Box(x - .56f, 0f, z - .1f, x - .5f + CasingStop, 2.16f, z + .1f, frameMat, false);
        kit.Box(x + .5f - CasingStop, 0f, z - .1f, x + .56f, 2.16f, z + .1f, frameMat, false);
        kit.Box(x - .56f, 2.1f - CasingStop, z - .1f, x + .56f, 2.16f, z + .1f, frameMat, false);
    }

    // An RN-F leaf hinged at <paramref name="hingeLocal"/> (parent space), swinging toward +z: the kit's F5 lite (15 x 15 in,
    // 0.1495-0.5305 from the hinge edge, 1.194-1.575 m up) cut through the slab and glazed, a panic bar on the -z face. Lit
    // (the goal): a one-sided C42 glow on the lite's corridor face, so from the corridor it shines and from the hall the lite
    // is a window back onto the corridor (review SP7-space-F2/F3, SP7-light-F3: the slab had no hole and the box covered it).
    Transform Leaf(Transform parent, Vector3 hingeLocal, string name, bool lit, out Collider leafCollider)
    {
        var hinge = new GameObject(name);
        hinge.transform.SetParent(parent, false);
        hinge.transform.localPosition = hingeLocal;
        var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        slab.name = "leaf (collider)";
        slab.transform.SetParent(hinge.transform, false);
        slab.transform.localPosition = new Vector3(.5f, 1.05f, 0f);
        slab.transform.localScale = new Vector3(.98f, 2.08f, .045f);
        slab.GetComponent<Renderer>().enabled = false;
        leafCollider = slab.GetComponent<Collider>();
        const float lx0 = .1495f, lx1 = .5305f, ly0 = 1.1938f, ly1 = 1.5748f;
        LeafPiece(hinge.transform, .01f, lx0, .01f, 2.09f);
        LeafPiece(hinge.transform, lx1, .99f, .01f, 2.09f);
        LeafPiece(hinge.transform, lx0, lx1, .01f, ly0);
        LeafPiece(hinge.transform, lx0, lx1, ly1, 2.09f);
        var lite = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lite.name = "F5 lite (glass)";
        UnityEngine.Object.Destroy(lite.GetComponent<Collider>());
        lite.transform.SetParent(hinge.transform, false);
        lite.transform.localPosition = new Vector3((lx0 + lx1) * .5f, (ly0 + ly1) * .5f, 0f);
        lite.transform.localScale = new Vector3(lx1 - lx0, ly1 - ly0, .012f);
        var lr = lite.GetComponent<Renderer>();
        lr.sharedMaterial = FrontRoomsStructuresKit.GlassMaterial();
        lr.shadowCastingMode = ShadowCastingMode.Off;
        if (lit)
        {
            // Bright enough to lead the end wall from the arrival (review SP7-space-F6: at x1.4 it tied with the walls).
            var glow = GameObject.CreatePrimitive(PrimitiveType.Quad);
            glow.name = "C42 glow (corridor face, one-sided)";
            UnityEngine.Object.Destroy(glow.GetComponent<Collider>());
            glow.transform.SetParent(hinge.transform, false);
            glow.transform.localPosition = new Vector3((lx0 + lx1) * .5f, (ly0 + ly1) * .5f, -.026f);
            glow.transform.localScale = new Vector3(lx1 - lx0, ly1 - ly0, 1f);
            var gr = glow.GetComponent<Renderer>();
            gr.sharedMaterial = FrontRoomsPoolMaterials.Glow("SP7 goal lite (C42)", C42 * 2.4f);
            gr.shadowCastingMode = ShadowCastingMode.Off;
            goalLite = gr;
        }
        var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bar.name = "panic bar";
        UnityEngine.Object.Destroy(bar.GetComponent<Collider>());
        bar.transform.SetParent(hinge.transform, false);
        bar.transform.localPosition = new Vector3(.5f, 1.0f, lit ? -.06f : .06f);
        bar.transform.localScale = new Vector3(.8f, .05f, .05f);
        bar.GetComponent<Renderer>().sharedMaterial = FrontRoomsPoolMaterials.Steel;
        if (lit) goalBar = bar.transform;
        if (lit)
        {
            // C42's driver (greybox): a small steel box on the leaf's hall face above the lite, clear of its view.
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "STAND-IN C42 driver box";
            UnityEngine.Object.Destroy(box.GetComponent<Collider>());
            box.transform.SetParent(hinge.transform, false);
            box.transform.localPosition = new Vector3((lx0 + lx1) * .5f, 1.75f, .0725f);
            box.transform.localScale = new Vector3(.38f, .18f, .10f);
            box.GetComponent<Renderer>().sharedMaterial = FrontRoomsPoolMaterials.Lit("SP7 C42 box", new Color(.25f, .25f, .24f), .3f, .4f);
        }
        else
        {
            var lever = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lever.name = "lever handle";
            UnityEngine.Object.Destroy(lever.GetComponent<Collider>());
            lever.transform.SetParent(hinge.transform, false);
            lever.transform.localPosition = new Vector3(.86f, 1.0f, -.05f);
            lever.transform.localScale = new Vector3(.12f, .02f, .02f);
            lever.GetComponent<Renderer>().sharedMaterial = FrontRoomsPoolMaterials.Steel;
        }
        return hinge.transform;
    }

    // One piece of a leaf round its lite (leaf-local x and y, 45 mm thick), render only.
    void LeafPiece(Transform hinge, float x0, float x1, float y0, float y1)
    {
        if (x1 - x0 < 1e-3f || y1 - y0 < 1e-3f) return;
        var piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
        piece.name = "leaf";
        UnityEngine.Object.Destroy(piece.GetComponent<Collider>());
        piece.transform.SetParent(hinge, false);
        piece.transform.localPosition = new Vector3((x0 + x1) * .5f, (y0 + y1) * .5f, 0f);
        piece.transform.localScale = new Vector3(x1 - x0, y1 - y0, .045f);
        piece.GetComponent<Renderer>().sharedMaterial = leafMat;
    }

    // ---------------------------------------------------------------- compartment frames (10 RN-2): FD1 z 9 (the trigger), FD2 z 18

    void BuildFireFrames()
    {
        kit.Shell(Ceil, 0f);
        var n = 0;
        foreach (var z in new[] { Fd1, Fd2 })
        {
            // Each frame is its own part (the same pieces, its own collider), so a treatment can move or hide it (72 section 3.2:
            // FD2 rides the far shore's island).
            var framePart = kit.BeginPart(n == 0 ? "SP7 FD1 frame (z 9, the trip plane)" : "SP7 FD2 frame (z 18)");
            if (n < fireFrames.Length) fireFrames[n] = framePart;
            n++;
            kit.Shell(Ceil, 0f);
            // 0.16 block on the cell line, a 2.0 x 2.1 frame centred with dark-bronze casing, the pair of 1.0 m leaves held
            // open at 90 degrees on magnets against the walls on the entrance side (2.0 x 2.1 m clear: the Relay's 2.05 passes).
            kit.WallX(z, -HalfW, HalfW, 0f, Ceil, upper, new List<FrontRoomsStructuresKit.Opening> { new FrontRoomsStructuresKit.Opening(-1f, 1f, 0f, 2.1f) }, .16f);
            kit.Box(-HalfW, 0f, z - .081f, -1.06f, 1.2f, z - .083f, lower, false);
            kit.Box(1.06f, 0f, z - .081f, HalfW, 1.2f, z - .083f, lower, false);
            kit.Box(-HalfW, 0f, z + .081f, -1.06f, 1.2f, z + .083f, lower, false);
            kit.Box(1.06f, 0f, z + .081f, HalfW, 1.2f, z + .083f, lower, false);
            // The casing CasingStop proud of the opening's jambs and head (see Frame: flush, they z-fought).
            kit.Box(-1.06f, 0f, z - .1f, -1f + CasingStop, 2.16f, z + .1f, frameMat, false);
            kit.Box(1f - CasingStop, 0f, z - .1f, 1.06f, 2.16f, z + .1f, frameMat, false);
            kit.Box(-1.06f, 2.1f - CasingStop, z - .1f, 1.06f, 2.16f, z + .1f, frameMat, false);
            var castKeep = kit.Mesh.CastShadows;
            kit.Mesh.CastShadows = false;
            kit.Box(-1.045f, 0f, z - 1.08f, -1.0f, 2.08f, z - .1f, leafMat);
            kit.Box(1.0f, 0f, z - 1.08f, 1.045f, 2.08f, z - .1f, leafMat);
            kit.Mesh.CastShadows = castKeep;
            foreach (var s in new[] { -1f, 1f })
            {
                // A wall magnet with its extension arm to the leaf (greybox).
                kit.StandIn("hold-open magnet", new Vector3(s * 1.385f, 1.9f, z - .95f), new Vector3(.07f, .08f, .08f), FrontRoomsPoolMaterials.Steel, 0f, false);
                kit.Box(s * 1.35f - .015f, 1.885f, z - .965f, s * 1.35f + .015f, 1.915f, z - .935f, FrontRoomsPoolMaterials.Steel, false);
                kit.Box(Mathf.Min(s * 1.35f, s * 1.045f), 1.885f, z - .965f, Mathf.Max(s * 1.35f, s * 1.045f), 1.915f, z - .935f, FrontRoomsPoolMaterials.Steel, false);
            }
            kit.EndPart();
        }
    }

    // ---------------------------------------------------------------- the head's machine: track, guide slot, magnets (receding)

    void BuildTrack()
    {
        trackPart = kit.BeginPart("SP7 recede track (receding variants)");
        kit.Shell(Ceil, 0f);
        var steel = FrontRoomsPoolMaterials.Steel;
        // A surface-mounted 100 mm steel track in both soffit corners from the entrance wall to the end wall, hangers every 1.5 m
        // (none in a fire-door frame); the 20 mm guide slot in the floor from z 27 to 42 (71 section 5.1; in view from the arch:
        // the honest tell; from the FD1 frame it hid behind FD1's header from the arrival: review SP7-space-F1).
        foreach (var s in new[] { -1f, 1f })
        {
            kit.Box(s * 1.30f - .05f, 2.845f, .16f, s * 1.30f + .05f, 2.895f, Length, steel, false);
            for (var z = .75f; z < Length; z += 1.5f)
                kit.Box(s * 1.30f - .02f, 2.895f, z - .02f, s * 1.30f + .02f, Ceil, z + .02f, steel, false);
        }
        kit.Box(.94f, .0006f, 27f, .96f, .0014f, Length, FrontRoomsPoolMaterials.Lit("SP7 guide slot", new Color(.03f, .03f, .03f), .2f), false);
        kit.EndPart();
        // Hold-open magnets on both walls in front of every line the head can seat on (shown per variant and cell).
        foreach (var line in MagnetLines)
        {
            var go = new GameObject("SP7 head magnets z " + line);
            go.transform.SetParent(Root, false);
            foreach (var s in new[] { -1f, 1f })
            {
                var m = GameObject.CreatePrimitive(PrimitiveType.Cube);
                m.name = "STAND-IN hold-open magnet";
                UnityEngine.Object.Destroy(m.GetComponent<Collider>());
                m.transform.SetParent(go.transform, false);
                m.transform.localPosition = new Vector3(s * (HalfW - .025f), 2.02f, line - .16f);
                m.transform.localScale = new Vector3(.05f, .12f, .08f);
                m.GetComponent<Renderer>().sharedMaterial = steel;
            }
            magnets[line] = go;
        }
    }

    // Ghost bands (greybox): where the head stood, 0.2 m of cleaner paint and unworn wax on walls, soffit and floor.
    void BuildBands()
    {
        var gl = Lighter(lower, 1.16f, "SP7 ghost band lower");
        var gu = Lighter(upper, 1.12f, "SP7 ghost band upper");
        var gs = Lighter(soffitMat, 1.12f, "SP7 ghost band soffit");
        var gf = Lighter(FrontRoomsPoolMaterials.Surface("RunB_FloorWax", new Color(.66f, .65f, .62f)), 1.12f, "SP7 ghost band wax");
        foreach (var line in BandLines)
        {
            var part = kit.BeginPart("SP7 ghost band z " + line);
            kit.Shell(Ceil, 0f);
            float z0 = line - .1f, z1 = line + .1f;
            foreach (var s in new[] { -1f, 1f })
            {
                var x = s * (HalfW - .003f);
                kit.Box(x - .002f, 0f, z0, x + .002f, 1.2f, z1, gl, false);
                kit.Box(x - .002f, 1.2f, z0, x + .002f, Ceil - .003f, z1, gu, false);
            }
            kit.Box(-HalfW, Ceil - .006f, z0, HalfW, Ceil - .002f, z1, gs, false);
            kit.Box(-HalfW + .003f, .0005f, z0, HalfW - .003f, .0025f, z1, gf, false);
            kit.EndPart();
            part.gameObject.SetActive(false);
            bandParts[line] = part.gameObject;
        }
    }

    Material Lighter(Material src, float k, string name)
    {
        if (src == null) return FrontRoomsPoolMaterials.Lit(name, new Color(.8f, .8f, .78f));
        var m = new Material(src) { name = "Structures / " + name, hideFlags = HideFlags.DontSave };
        if (m.HasProperty(BaseColorId))
        {
            var c = m.GetColor(BaseColorId);
            m.SetColor(BaseColorId, new Color(Mathf.Min(1f, c.r * k), Mathf.Min(1f, c.g * k), Mathf.Min(1f, c.b * k), c.a));
        }
        else if (m.HasProperty("_Color"))
        {
            var c = m.GetColor("_Color");
            m.SetColor("_Color", new Color(Mathf.Min(1f, c.r * k), Mathf.Min(1f, c.g * k), Mathf.Min(1f, c.b * k), c.a));
        }
        owned.Add(m);
        return m;
    }

    // ---------------------------------------------------------------- the release hall (z 42..51, Tall 5.4) and the blind doorway

    void BuildHall()
    {
        kit.Shell(HallCeil, 0f);
        // 70 section 9: three cells (x -1.5..7.5, z 42..51), off the corridor's axis and entered in its west cell, as SP5's dock.
        kit.Slab(HallX0, Length, HallX1, HallZ1, 0f, floorMat, .25f);
        var door = new List<FrontRoomsStructuresKit.Opening> { new FrontRoomsStructuresKit.Opening(-.5f, .5f, 0f, 2.1f) };
        kit.WallX(HallWallZ, HallX0 - .08f, HallX1 + .08f, 0f, HallCeil, upper, door, .16f);
        kit.WallZ(HallX0, HallZ0, HallZ1 + .08f, 0f, HallCeil, upper);
        kit.WallZ(HallX1, HallZ0, HallZ1 + .08f, 0f, HallCeil, upper);
        kit.WallX(HallZ1, HallX0 - .08f, HallX1 + .08f, 0f, HallCeil, upper);
        kit.Lid(HallX0, HallZ0, HallX1, HallZ1, HallCeil, soffitMat);
        // Run! B's lower tone on the hall's inner faces.
        var f = HallWallZ + .081f;
        kit.Box(HallX0 + .08f, 0f, f, -.56f, 1.2f, f + .002f, lower, false);
        kit.Box(.56f, 0f, f, HallX1 - .08f, 1.2f, f + .002f, lower, false);
        kit.Box(HallX0 + .08f, 0f, HallZ1 - .082f, HallX1 - .08f, 1.2f, HallZ1 - .08f, lower, false);
        kit.Box(HallX0 + .08f, 0f, HallZ0, HallX0 + .082f, 1.2f, HallZ1 - .08f, lower, false);
        kit.Box(HallX1 - .082f, 0f, HallZ0, HallX1 - .08f, 1.2f, HallZ1 - .08f, lower, false);
        // The real goal frame on the hall's south wall, on the axis.
        kit.Box(-.56f, 0f, HallWallZ + .08f, -.5f, 2.16f, HallWallZ + .12f, frameMat, false);
        kit.Box(.5f, 0f, HallWallZ + .08f, .56f, 2.16f, HallWallZ + .12f, frameMat, false);
        kit.Box(-.56f, 2.1f, HallWallZ + .08f, .56f, 2.16f, HallWallZ + .12f, frameMat, false);
        // The way back: a plate on the floor (E), the same as the hub's pads.
        kit.StandIn("return plate (E: back to the entrance)", new Vector3(3f, .01f, 49.5f), new Vector3(.9f, .02f, .9f), FrontRoomsPoolMaterials.Lit("pad plate", new Color(.32f, .33f, .34f), .4f, .5f), 0f, false);
        kit.StandIn("return plate rim", new Vector3(3f, .012f, 49.5f), new Vector3(.96f, .004f, .96f), FrontRoomsPoolMaterials.Glow("pad rim SP7", new Color(.957f, .875f, .231f) * 1.6f), 0f, false);
        // The blind doorway (71 section 4.6): a block panel in the frame while the head stands short of z 42.
        blindPart = kit.BeginPart("SP7 blind doorway (the frame before the head seats)");
        kit.Shell(HallCeil, 0f);
        kit.Box(-.5f, 0f, HallWallZ - .06f, .5f, 1.2f, HallWallZ + .06f, lower);
        kit.Box(-.5f, 1.2f, HallWallZ - .06f, .5f, 2.1f, HallWallZ + .06f, upper);
        kit.EndPart();
    }

    // ---------------------------------------------------------------- the head (built at z 0, moved to its line)

    void BuildHead()
    {
        head = kit.BeginPart("SP7 head (moving end: wall, RN-F goal door, goal head, C42)");
        kit.Shell(Ceil, 0f);
        const float t = FrontRoomsStructuresRunEndRoom.HeadWallHalf;
        // Block-faced end wall (the two tones), the door opening 1.0 x 2.1 with its frame; solid for the walker. Over 2.25 m the
        // wall is slotted where the fixtures of the cells it uncovers cross its outline (70 section 9: the side heads and the
        // remote EMT on both walls, the tray, the strip, the main), brush seals lining each slot (greybox).
        foreach (var r in WallPieces())
        {
            wallPieces.Add(r);
            if (r.yMin < 1.2f - .001f) kit.Box(r.xMin, r.yMin, -t, r.xMax, Mathf.Min(1.2f, r.yMax), t, lower);
            if (r.yMax > 1.2f + .001f) kit.Box(r.xMin, Mathf.Max(1.2f, r.yMin), -t, r.xMax, r.yMax, t, upper);
        }
        foreach (var sl in Slots) slotRects.Add(Rect.MinMaxRect(sl.x, sl.y, sl.x + sl.width, HeadTop));
        const float coveDepth = FrontRoomsStructuresRunEndRoom.HeadCoveDepth, coveTop = FrontRoomsStructuresRunEndRoom.HeadCoveTop, casing = FrontRoomsStructuresRunEndRoom.HeadCasingHalf;
        kit.Box(-HeadHalf, 0f, -t - coveDepth, -casing, coveTop, -t, cove, false);
        kit.Box(casing, 0f, -t - coveDepth, HeadHalf, coveTop, -t, cove, false);
        kit.Box(-.56f, 0f, -.1f, -.5f, 2.16f, .1f, frameMat, false);
        kit.Box(.5f, 0f, -.1f, .56f, 2.16f, .1f, frameMat, false);
        kit.Box(-.56f, 2.1f, -.1f, .56f, 2.16f, .1f, frameMat, false);
        // The goal head's bracket: an arm from the wall over the door to a plate 1.2 m out, the twin-head unit under it.
        kit.Box(-.025f, 2.6f, -1.18f, .025f, 2.65f, -t, FrontRoomsPoolMaterials.Steel, false);
        kit.Box(-.05f, 2.30f, -1.2f, .05f, 2.65f, -1.18f, FrontRoomsPoolMaterials.Steel, false);
        // A knee brace from the wall over the door frame to the arm.
        var braceA = new Vector3(0f, 2.26f, -t);
        var braceB = new Vector3(0f, 2.61f, -.82f);
        kit.Mesh.Box((braceA + braceB) * .5f, new Vector3(.04f, .04f, (braceB - braceA).magnitude), Quaternion.LookRotation((braceB - braceA).normalized, Vector3.up), FrontRoomsPoolMaterials.Steel, false);
        kit.EndPart();
        foreach (var r in head.GetComponentsInChildren<Renderer>(true))
            if (r.sharedMaterial == lower || r.sharedMaterial == upper || r.sharedMaterial == cove) headWall.Add(r);
        // The head's machine (the slots' brush seals, the guide pin, the trolleys and their hangers): the same pieces, render
        // only, in their own part under the head (built at z 0 as the head is), so an end that never moves can hide it (#5).
        headMachine = kit.BeginPart("SP7 head machine (brush seals, guide pin, trolleys, hangers)");
        kit.Shell(Ceil, 0f);
        foreach (var sl in Slots)
        {
            kit.Box(sl.x - .012f, sl.y, -t - .004f, sl.x + .003f, HeadTop, t + .004f, rubber, false);
            kit.Box(sl.x + sl.width - .003f, sl.y, -t - .004f, sl.x + sl.width + .012f, HeadTop, t + .004f, rubber, false);
            if (sl.y > Band + .01f) kit.Box(sl.x, sl.y - .012f, -t - .004f, sl.x + sl.width, sl.y + .003f, t + .004f, rubber, false);
        }
        // The guide pin under the wall (in the slot at x 0.95) and the two trolleys in the soffit corners.
        kit.Box(.93f, 0f, .02f, .97f, .03f, .06f, FrontRoomsPoolMaterials.Steel, false);
        foreach (var s in new[] { -1f, 1f })
        {
            kit.Box(s * 1.30f - .07f, 2.78f, -.16f, s * 1.30f + .07f, 2.845f, .16f, FrontRoomsPoolMaterials.Steel, false);
            // The trolley's hanger: an arm over the side slot to a plate on the slot's inner edge, down to the band.
            kit.Box(Mathf.Min(s * 1.105f, s * 1.30f), 2.76f, -.03f, Mathf.Max(s * 1.105f, s * 1.30f), 2.78f, .03f, FrontRoomsPoolMaterials.Steel, false);
            kit.Box(Mathf.Min(s * 1.085f, s * 1.105f), Band, -.05f, Mathf.Max(s * 1.085f, s * 1.105f), 2.78f, .05f, FrontRoomsPoolMaterials.Steel, false);
        }
        kit.EndPart();
        headMachine.SetParent(head, false);
        // Rubber sweeps on the side edges (pressed when seated, relaxed while moving).
        foreach (var s in new[] { -1f, 1f })
        {
            var sw = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sw.name = "rubber sweep";
            UnityEngine.Object.Destroy(sw.GetComponent<Collider>());
            sw.transform.SetParent(head, false);
            sw.transform.localPosition = new Vector3(s * (HeadHalf + .01f), HeadTop * .5f, 0f);
            sw.transform.localScale = new Vector3(.02f, HeadTop, .06f);
            sw.GetComponent<Renderer>().sharedMaterial = rubber;
            sweeps.Add(sw.transform);
        }
        var top = GameObject.CreatePrimitive(PrimitiveType.Cube);
        top.name = "rubber sweep (top)";
        UnityEngine.Object.Destroy(top.GetComponent<Collider>());
        top.transform.SetParent(head, false);
        top.transform.localPosition = new Vector3(0f, HeadTop + .01f, 0f);
        top.transform.localScale = new Vector3(2f * HeadHalf, .02f, .06f);
        top.GetComponent<Renderer>().sharedMaterial = rubber;
        // The goal door: RN-F with set A's F5 lite lit by C42's box behind it, the panic bar on the corridor face.
        goalLeaf = Leaf(head, new Vector3(-.5f, 0f, 0f), "Goal door leaf (E opens at 14 cells)", true, out goalLeafCollider);
        // The goal head: one twin-head unit on the bracket, both heads aimed back at the leaf and the floor before it.
        var unit = new GameObject("Goal head (twin-head unit on a bracket)");
        unit.transform.SetParent(head, false);
        unit.transform.localPosition = new Vector3(0f, rig.unitBoxCentreY - .0955f, -1.2f);
        unit.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        goalUnit = unit.transform;
        goalHeads = BuildUnitHeads(unit.transform, true, new Vector3(0f, 1.15f, -.05f), new Vector3(0f, 0f, -.7f), false, 0f, 0f, -1);
    }

    struct Slot
    {
        public float x, width, y;
        public Slot(float x0, float x1, float y) { x = x0; width = x1 - x0; this.y = y; }
    }

    // The head's slots (x from, to, and the slot's bottom): both walls' side heads and remote EMT, the tray, the strip, the main.
    static readonly Slot[] Slots =
    {
        new Slot(-HeadHalf, -1.10f, Band), new Slot(-.84f, -.36f, 2.62f), new Slot(-.14f, .14f, 2.64f), new Slot(.47f, .63f, 2.54f), new Slot(1.10f, HeadHalf, Band),
    };

    // The head wall's solid pieces (head space): the two panels beside the door and the piece over it up to the band, and the
    // band's pieces between and under the slots.
    static List<Rect> WallPieces()
    {
        var list = new List<Rect>
        {
            Rect.MinMaxRect(-HeadHalf, 0f, -.56f, Band),
            Rect.MinMaxRect(.56f, 0f, HeadHalf, Band),
            Rect.MinMaxRect(-.56f, 2.16f, .56f, Band),
        };
        var x = -HeadHalf;
        foreach (var sl in Slots)
        {
            if (sl.x > x + .001f) list.Add(Rect.MinMaxRect(x, Band, sl.x, HeadTop));
            if (sl.y > Band + .001f) list.Add(Rect.MinMaxRect(sl.x, Band, sl.x + sl.width, sl.y));
            x = sl.x + sl.width;
        }
        if (HeadHalf > x + .001f) list.Add(Rect.MinMaxRect(x, Band, HeadHalf, HeadTop));
        return list;
    }

    // T2's blocks (greybox): the head wall's pieces cut into 0.40 x 0.20 m units in running bond (about 90), parked under the
    // district and shown only while a T2 step is in the air; the solid wall is the merged mesh above.
    void BuildBlocks()
    {
        blocksRoot = new GameObject("SP7 T2 blocks (in the air only)").transform;
        blocksRoot.SetParent(Root, false);
        const float h = .2f;
        var rows = Mathf.FloorToInt(HeadTop / h);
        var pieces = WallPieces();
        for (var r = 0; r < rows; r++)
        {
            var ya = r * h;
            var yb = r == rows - 1 ? HeadTop : ya + h;
            var offset = r % 2 == 0 ? 0f : .2f;
            foreach (var pc in pieces)
            {
                var y0 = Mathf.Max(ya, pc.yMin);
                var y1 = Mathf.Min(yb, pc.yMax);
                if (y1 - y0 < .03f) continue;
                // Cut the piece's span on the 0.4 m bond (offset every other course), from the wall's west edge.
                var cuts = new List<float> { pc.xMin };
                for (var c = -HeadHalf + offset; c < pc.xMax - .05f; c += .4f) if (c > pc.xMin + .05f) cuts.Add(c);
                cuts.Add(pc.xMax);
                for (var k = 0; k + 1 < cuts.Count; k++)
                {
                    var a = cuts[k];
                    var b = cuts[k + 1];
                    if (b - a < .03f) continue;
                    var cx = (a + b) * .5f;
                    var cy = (y0 + y1) * .5f;
                    AddBlock(new Vector3(cx, cy, 0f), new Vector3(b - a - .004f, y1 - y0 - .004f, .156f), Mathf.Clamp01(cy / HeadTop), cx < 0f ? -1f : 1f, cy < 1.2f ? lower : upper);
                }
            }
        }
        blocksRoot.gameObject.SetActive(false);
    }

    void AddBlock(Vector3 seat, Vector3 size, float row01, float side, Material m)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "block";
        UnityEngine.Object.Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(blocksRoot, false);
        go.transform.localScale = size;
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = ShadowCastingMode.Off;
        var hash = Mathf.Abs(Mathf.Sin(seat.x * 12.9898f + seat.y * 78.233f) * 43758.547f) % 1f;
        blocks.Add(new Block
        {
            t = go.transform,
            seat = seat,
            row01 = row01,
            side = side,
            // Courses under 2.15 stream along their wall at their own height, 0.18 m off it; the top courses ride at 2.6 m just
            // inside the side heads' reach (x 0.95), so no block passes through a head of the cells being uncovered.
            laneX = seat.y >= 2.15f ? side * .95f : side * 1.24f,
            laneY = seat.y >= 2.15f ? 2.6f : seat.y,
            wobble = hash * 2f - 1f,
        });
    }

    // ---------------------------------------------------------------- lamps: R-B strips with the red tube, twin-head units, the hall

    void BuildLamps()
    {
        try
        {
            haloWhite = FrontRoomsRunRig.CreateHaloMaterial(rig.haloColor);
            haloRed = FrontRoomsRunRig.CreateHaloMaterial(RedHaloPeak);
            if (haloWhite != null) owned.Add(haloWhite);
            if (haloRed != null) owned.Add(haloRed);
        }
        catch (Exception e) { FrontRoomsPoolMaterials.LogOnce("SP7 halo material", "SP7: the soffit halo material failed: " + e.Message); }
        var lampRoot = new GameObject("SP7 lamps").transform;
        lampRoot.SetParent(Root, false);
        // 14 R-B strips on the soffit at z 1.8 + 3k, the stutter tube at 13.8 (Run! B's cell 5); each with a third, red tube.
        for (var k = 0; k < Cells; k++)
        {
            var z = 1.8f + 3f * k;
            var s = Strip(lampRoot, new Vector3(0f, Ceil, z), 1f, rig.wrapRange, true, "R-B strip " + k);
            s.cell = k;
            s.stutter = k == 4;
            strips.Add(s);
            cellObjects[k].Add(s.root);
        }
        // The twin-head units (T-A at z 3 and 39, T-B between), Run! B's aims and trims.
        var defs = Units();
        for (var u = 0; u < defs.Length; u++)
        {
            var d = defs[u];
            var onFd1 = u == 1;
            var unit = new GameObject("Twin-head unit " + u + (onFd1 ? " on FD1" : d.side > 0 ? " R" : " L") + " z " + d.z.ToString("0.0"));
            unit.transform.SetParent(lampRoot, false);
            var cell = CellOf(d.z + .01f);
            if (onFd1)
            {
                // On FD1's north face over the frame's west half, facing north; its heads aimed at the floor ahead across the axis.
                unit.transform.localPosition = new Vector3(-.5f, rig.unitBoxCentreY - .0955f, Fd1 + .08f);
                unit.transform.localRotation = Quaternion.identity;
                BuildUnitHeads(unit.transform, false, new Vector3(-.55f, 0f, Fd1 + 4.6f), new Vector3(.55f, 0f, Fd1 + 4.6f), true, 0f, 0f, cell, d, true);
                // The remote circuit's last metre: along FD1's north face from the west wall's corner into the remote box.
                var emt = FrontRoomsPoolMaterials.Surface("RunB_GalvSteel", new Color(.6f, .62f, .63f));
                var run = GameObject.CreatePrimitive(PrimitiveType.Cube);
                run.name = "remote EMT on FD1";
                UnityEngine.Object.Destroy(run.GetComponent<Collider>());
                run.transform.SetParent(unit.transform.parent, false);
                run.transform.localPosition = new Vector3((-HalfW + .01f - .55f) * .5f, rig.unitBoxCentreY - .0955f + .095f, Fd1 + .08f + .0115f);
                run.transform.localScale = new Vector3(HalfW - .01f - .55f, .018f, .018f);
                run.GetComponent<Renderer>().sharedMaterial = emt;
                run.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            else
            {
                unit.transform.localPosition = new Vector3(d.side * HalfW, rig.unitBoxCentreY - .0955f, d.z);
                unit.transform.localRotation = Quaternion.Euler(0f, d.side > 0 ? -90f : 90f, 0f);
                // The last two units' +z lamps land 1.1 m ahead, short of the 36 and 42 seats: aimed 4.2 m ahead they pooled on the
                // end wall beside the door and tied with it (review SP7-light-F6).
                BuildUnitHeads(unit.transform, false, Vector3.zero, Vector3.zero, d.remote, d.trimMinus, d.trimPlus, cell, d, false, u >= UnitZ.Length - 2 ? 1.1f : 0f);
            }
            cellObjects[cell].Add(unit);
        }
        // The release hall: one R-B strip per cell at x1.6 (LMS 4, Tall), always on: its light shows in the goal lite.
        for (var i = 0; i < 3; i++)
            for (var j = 0; j < 3; j++)
            {
                var s = Strip(lampRoot, new Vector3(3f * i, HallCeil, HallZ0 - .1f + 1.5f + 3f * j), 1.6f, 14f, false, "hall strip");
                // The hall's strips never carry the red tube (70 section 11).
                if (s.red != null) { s.red.enabled = false; s.red = null; }
                hallStrips.Add(s);
            }
    }

    // An R-B strip: the game's strip kit (bare two tubes in a wire guard), Run! B's wrap light under it, and (corridor) the
    // red tube beside the white ones, a greybox emissive cylinder 1.22 m x 38 mm, dark oxblood when unlit.
    StripLamp Strip(Transform parent, Vector3 at, float scale, float range, bool withRed, string name)
    {
        var s = new StripLamp { z = at.z, scale = scale };
        var frame = new GameObject(name);
        frame.transform.SetParent(parent, false);
        frame.transform.localPosition = at;
        s.root = frame;
        MeshRenderer lens = null, kitRed = null;
        try
        {
            // The game's R-B strip; since 11-04 (card zone_lamps 1, Red's yes) a three-lamp strip whose middle tube is red-coated,
            // its own renderer (the kit's second), driven apart, as FrontRoomsRunRig drives it.
            if (FrontRoomsLampKit.Available(FrontRoomsLampKit.Family.Strip))
                lens = FrontRoomsLampKit.Build(FrontRoomsLampKit.Family.Strip, frame.transform, Vector3.zero, Quaternion.identity, "SP7 strip tubes", false, out kitRed);
        }
        catch (Exception e) { FrontRoomsPoolMaterials.LogOnce("SP7 strip", "SP7: the R-B strip kit failed: " + e.Message); lens = null; kitRed = null; }
        if (kitRed != null) s.red = kitRed;
        if (lens == null)
        {
            FrontRoomsPoolMaterials.LogOnce("SP7 strip missing", "SP7: the R-B strip kit is missing: a flat stand-in lens is used.");
            var flat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flat.name = "STAND-IN strip lens";
            UnityEngine.Object.Destroy(flat.GetComponent<Collider>());
            flat.transform.SetParent(frame.transform, false);
            flat.transform.localPosition = new Vector3(0f, -.06f, 0f);
            flat.transform.localScale = new Vector3(.1f, .04f, 1.22f);
            lens = flat.GetComponent<MeshRenderer>();
            lens.sharedMaterial = lensStandIn;
            lens.shadowCastingMode = ShadowCastingMode.Off;
        }
        s.lens = lens;
        if (withRed && s.red == null)
        {
            // An older strip kit without its red tube: a greybox cylinder beside the white tubes stands in (logged once).
            FrontRoomsPoolMaterials.LogOnce("SP7 red tube", "SP7: the strip kit has no red tube: a greybox cylinder stands in.");
            var c = new Vector3(0f, -.075f, 0f);
            var e = new Vector3(.04f, .02f, .61f);
            try
            {
                var lb = lens.localBounds;
                if (lb.size.sqrMagnitude > 1e-6f) { c = lb.center; e = lb.extents; }
            }
            catch (Exception) { }
            var tube = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tube.name = "STAND-IN red tube (alarm circuit)";
            UnityEngine.Object.Destroy(tube.GetComponent<Collider>());
            tube.transform.SetParent(frame.transform, false);
            tube.transform.localPosition = new Vector3(Mathf.Clamp(c.x + e.x + .024f, .045f, .13f), Mathf.Clamp(c.y, -.16f, -.03f), c.z);
            tube.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            tube.transform.localScale = new Vector3(.038f, .61f, .038f);
            var tr = tube.GetComponent<Renderer>();
            tr.sharedMaterial = redTube;
            tr.shadowCastingMode = ShadowCastingMode.Off;
            s.red = tr;
        }
        var lightGo = new GameObject("light");
        lightGo.transform.SetParent(frame.transform, false);
        lightGo.transform.localPosition = new Vector3(0f, -.092f, 0f);
        lightGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var l = lightGo.AddComponent<Light>();
        FrontRoomsMapWorld.ShippedShadowFilter(l);
        l.type = LightType.Spot;
        l.spotAngle = rig.wrapOuter;
        l.innerSpotAngle = rig.wrapInner;
        l.color = rig.wrapColor;
        l.intensity = rig.wrapIntensity * scale;
        l.range = range;
        l.shadows = LightShadows.None;
        l.shadowStrength = .9f;
        l.shadowNearPlane = .1f;
        if (name.EndsWith(" 0", StringComparison.Ordinal))
        {
            // Strip 0 (beside the entrance): its own bias, so the vestibule's side walls at the corners stop taking the
            // corridor's light through the 0.08 entrance wall (review SP7-light-F5).
            try
            {
                var ad = lightGo.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>();
                if (ad == null) ad = lightGo.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>();
                ad.usePipelineSettings = false;
                l.shadowBias = .15f;
                l.shadowNormalBias = .25f;
            }
            catch (Exception e) { FrontRoomsPoolMaterials.LogOnce("SP7 strip bias", "SP7: strip 0's bias: " + e.Message); }
        }
        s.light = l;
        // The rig's soffit halo (3 mm under the soffit), white; the red halo material swaps in under the alarm.
        try { if (haloWhite != null) s.halo = FrontRoomsRunRig.BuildHalo(frame.transform, new Vector3(0f, -.003f, 0f), haloWhite); }
        catch (Exception e) { FrontRoomsPoolMaterials.LogOnce("SP7 halo", "SP7: the soffit halo failed: " + e.Message); }
        SetStrip(s, 1f, 0f, 1f, 0f);
        return s;
    }

    // The two heads of one twin-head unit under <paramref name="unit"/> (the cabinet's frame: wall at local z 0, +z out of the
    // wall), as FrontRoomsRunRig.BuildUnits: the cabinet (T-A) or the remote mount (T-B), yokes on mount_l / mount_r, lamps
    // on the trunnion, aimed by Run! B's rule (pitch 30 degrees, at the floor across the axis at 0.55 m) or, for the goal
    // head, at the two given points (unit-parent space). Lights off until the trip; the cabinet's AC ON pilot lit when armed.
    HeadLamp[] BuildUnitHeads(Transform unit, bool goal, Vector3 aimA, Vector3 aimB, bool remote, float trimMinus, float trimPlus, int cell, RunUnitDef def = default(RunUnitDef), bool explicitAim = false, float plusAhead = 0f)
    {
        var result = new List<HeadLamp>();
        var mountL = Anchor("Kit_EmergencyTwinHead", "mount_l", new Vector3(.112f, .201f, .059f));
        var mountR = Anchor("Kit_EmergencyTwinHead", "mount_r", new Vector3(-.112f, .201f, .059f));
        var trunnion = Anchor("Kit_EmergencyTwinHead_Yoke", "trunnion", new Vector3(0f, .085f, 0f));
        var lensAnchor = Anchor("Kit_EmergencyTwinHead_Lamp", "lens", new Vector3(0f, 0f, .049f));
        Texture2D cookie = null;
        try { cookie = Resources.Load<Texture2D>("Surfaces/Textures/Cookie_SealedBeam_S"); } catch (Exception) { cookie = null; }
        GameObject housing = null;
        try
        {
            if (remote) housing = FrontRoomsLampKit.BuildHousing(FrontRoomsLampKit.Family.RemotePlate, unit, Vector3.zero, Quaternion.identity);
            if (housing == null) housing = FrontRoomsKitLibrary.Spawn("Kit_EmergencyTwinHead", unit, Vector3.zero, Quaternion.identity, null, false, "cabinet", false);
        }
        catch (Exception e) { FrontRoomsPoolMaterials.LogOnce("SP7 unit", "SP7: a twin-head unit failed: " + e.Message); housing = null; }
        if (housing == null)
        {
            FrontRoomsPoolMaterials.LogOnce("SP7 unit missing", "SP7: Kit_EmergencyTwinHead is missing: stand-in boxes are used.");
            housing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            housing.name = "STAND-IN twin-head cabinet";
            UnityEngine.Object.Destroy(housing.GetComponent<Collider>());
            housing.transform.SetParent(unit, false);
            housing.transform.localPosition = new Vector3(0f, .0955f, .064f);
            housing.transform.localScale = new Vector3(.345f, .191f, .128f);
            housing.GetComponent<Renderer>().sharedMaterial = FrontRoomsPoolMaterials.Lit("head beige", new Color(.85f, .82f, .74f));
        }
        else if (!remote)
        {
            foreach (var r in housing.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (var i = 0; i < mats.Length; i++)
                    if (mats[i] != null && mats[i].name.StartsWith("Run_NeonPilot", StringComparison.Ordinal))
                        pilots.Add(new Emissive { renderer = r, index = i, cell = cell });
            }
        }
        foreach (var r in housing.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = ShadowCastingMode.Off;
        var rigSpace = Root;
        for (var m = 0; m < 2; m++)
        {
            var mount = m == 0 ? mountL : mountR;
            GameObject yoke = null, lamp = null;
            try
            {
                yoke = FrontRoomsKitLibrary.Spawn("Kit_EmergencyTwinHead_Yoke", unit, mount, Quaternion.identity, null, false, "yoke " + m, false);
                if (yoke != null) lamp = FrontRoomsKitLibrary.Spawn("Kit_EmergencyTwinHead_Lamp", yoke.transform, trunnion, Quaternion.identity, null, false, "lamp head " + m, false);
            }
            catch (Exception e) { FrontRoomsPoolMaterials.LogOnce("SP7 head kit", "SP7: a head kit failed: " + e.Message); }
            if (yoke == null)
            {
                yoke = new GameObject("yoke " + m + " (stand-in)");
                yoke.transform.SetParent(unit, false);
                yoke.transform.localPosition = mount;
            }
            if (lamp == null)
            {
                lamp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                lamp.name = "STAND-IN lamp head " + m;
                UnityEngine.Object.Destroy(lamp.GetComponent<Collider>());
                lamp.transform.SetParent(yoke.transform, false);
                lamp.transform.localPosition = trunnion;
                lamp.transform.localScale = new Vector3(.1f, .04f, .1f);
                lamp.GetComponent<Renderer>().sharedMaterial = FrontRoomsPoolMaterials.Lit("head beige", new Color(.85f, .82f, .74f));
            }
            // The lamp's pivot in district space, and its target.
            var pivot = rigSpace.InverseTransformPoint(lamp.transform.position);
            Vector3 target;
            var unitZ = rigSpace.InverseTransformPoint(unit.position).z;
            var plus = pivot.z > unitZ;
            if (goal || explicitAim) target = rigSpace.InverseTransformPoint(unit.parent.TransformPoint(m == 0 ? aimA : aimB));
            else
            {
                var dh = pivot.y / Mathf.Tan(rig.floodPitch * Mathf.Deg2Rad);
                var tx = -def.side * rig.aimCross;
                var along = Mathf.Sqrt(Mathf.Max(.01f, dh * dh - (tx - pivot.x) * (tx - pivot.x)));
                if (plus && plusAhead > 0f) along = plusAhead;
                target = new Vector3(tx, 0f, pivot.z + (plus ? along : -along));
            }
            var dir = rigSpace.TransformDirection(target - pivot).normalized;
            var flat = Vector3.ProjectOnPlane(dir, rigSpace.up);
            if (flat.sqrMagnitude > 1e-6f) yoke.transform.rotation = Quaternion.LookRotation(flat.normalized, rigSpace.up);
            lamp.transform.rotation = Quaternion.LookRotation(dir, rigSpace.up);
            var h = new HeadLamp { z = pivot.z, goal = goal, cell = cell };
            var lensList = new List<Renderer>();
            var idx = new List<int>();
            foreach (var r in lamp.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
                var mats = r.sharedMaterials;
                for (var i = 0; i < mats.Length; i++)
                    if (mats[i] != null && mats[i].name.StartsWith("Run_SealedBeamLens", StringComparison.Ordinal)) { lensList.Add(r); idx.Add(i); }
            }
            h.lens = lensList.ToArray();
            h.lensIndex = idx.ToArray();
            var lightGo = new GameObject(goal ? "head light (goal)" : "head light");
            lightGo.transform.SetParent(lamp.transform, false);
            lightGo.transform.localPosition = lensAnchor + new Vector3(0f, 0f, .017f);
            var light = lightGo.AddComponent<Light>();
            FrontRoomsMapWorld.ShippedShadowFilter(light);
            light.type = LightType.Spot;
            light.color = rig.floodColor;
            light.shadows = LightShadows.None;
            light.shadowStrength = rig.floodShadowStrength;
            light.shadowNearPlane = .05f;
            if (goal)
            {
                // Inference (71 section 5.1, to be checked here): the goal head 1.2 m out lights the leaf at about the level
                // the rig's goal head gives it from 6 m (gain 5); the second head the floor before the door. Tuned by eye.
                light.spotAngle = m == 0 ? 70f : 90f;
                light.innerSpotAngle = m == 0 ? 44f : 60f;
                light.range = m == 0 ? 7f : 6f;
                h.baseIntensity = m == 0 ? 7f : 3.5f;
            }
            else
            {
                light.spotAngle = rig.floodOuter;
                light.innerSpotAngle = rig.floodInner;
                light.range = rig.floodRange;
                light.cookie = cookie;
                h.baseIntensity = rig.floodIntensity * Mathf.Max(0f, 1f + (plus ? trimPlus : trimMinus));
            }
            light.intensity = 0f;
            light.enabled = false;
            h.light = light;
            heads.Add(h);
            result.Add(h);
        }
        return result.ToArray();
    }

    static Vector3 Anchor(string asset, string anchor, Vector3 fallback)
    {
        try
        {
            var info = FrontRoomsKitLibrary.GetInfo(asset);
            return info != null && info.TryAnchor(anchor, out var p) ? p : fallback;
        }
        catch (Exception) { return fallback; }
    }

    // ---------------------------------------------------------------- the sign (71 section 10, verbatim; key letters per the plan), plate, marks

    void BuildSigns()
    {
        // 70 section 9's sign, flat on the vestibule's east wall facing west, centre (1.42, 1.5, -4.044), out of the main view;
        // its text verbatim with the plan's key letters (70: L length, C light, G entrance, V Reduce Flashing -> V, L, O, F here,
        // as 70 allows). Two changes for what happened after 70's 06:22 edit, so the sign never asks what Red has answered or
        // lists a stand-in that is gone: Red said yes to the red tube on 11-04 (the brighter level, then the brightest column;
        // 70 section 10 already marks it answered, on on arrival), so it left the greybox list (the strip kit's own tube now)
        // and its open item (the measurements renumbered 1).
        // z -4.044 (was -4.0): the placard spec v2.4 puts every arrival 2.5 m from its district sign; the arrival eye
        // (0, 1.62, -2) to this panel's face centre (panel 1.575 m tall) is 2.500 m here (2.464 at -4.0). The settings plate
        // beside it (z -5.7..-4.7) stays 56 mm clear of the panel's end.
        kit.Sign(new Vector3(HalfW - .03f, 1.5f, -4.044f), new Vector3(-1.2f, 1.5f, -4.044f), "SP7 · Run!：会往后退的门",
            "游戏里的 Run!（Run! B 的设备走廊），平地，最长 14 格、42 米。你 11 月 3 日对 71 的九个决定回了\"全部按推荐\"，这里照推荐搭好，让你亲眼走一遍；走过以后你说可以，会退的门、Run 门入口和红色灯光才进游戏；觉得哪里不对，直接告诉我们。走法和游戏一样：Ctrl 冲刺（体力 5 秒），Shift 慢走。进来时是\"会退的 9→14 格\"：跨过 9 米那道防火门框，警报触发，尽头的墙和门往后退：先猛缩两格，再一格一格退，最后重重停住，门就是真门，推开是一间亮着的大厅。走路时门只在第一下猛缩时变远，按住 Shift 慢走才会一直越退越远；冲到离门 12 米以内，剩下的一段一口气滑到底。\n"
            + "按键：V 长度（普通 7 / 9 / 12 / 14 格，会退 9→12 / 9→14 格）；B 后退方式（T2 砖块拆开再砌回，电脑和 iPad 用；T1 整面墙滑走，网页版用）；L 触发后的灯光（今天的 / A 警报回路 / B 警报脉冲，你同意的推荐 / C 红潮）；H 红灯管；O 入口（拱门 / 带小方窗的 Run 门，只在普通长度）；F 减少闪烁；T 手动触发，再按一次恢复：白光从入口一格一格亮回来（你喜欢的那段）；Y 把门放回原位再试；X 换后退的做法（72 号文档的五种，Shift+X 往回，对面墙上的牌子写了每一种怎么试）；N 当前做法自己的选项（有的话）。旁边的小牌子显示现在的设置。\n"
            + "灰盒占位：砖块重组的路径（简单程序动画）、轨道、门吸、密封条、墙上的\"印子\"；灯光由场景 4 专用的脚本模拟，正式效果等游戏视觉做；没有声音，没有会动的 Relay。不到 14 格时尽头的门在这里打不开，因为门后是还没亮出来的走廊（游戏里这扇门永远不锁）。黑色人形是 Relay 的静态替身（2.05 米），只用来检查视线，不会动。\n"
            + "还没定的：1 要在游戏里实测、这里测不出来的：机器人测被追着领先 8 米冲进会退的房间能不能逃掉、冲到门口关上门的人有多少真能甩掉它、12 格和 14 格在 100 个种子里放得下多少；B 的光敏安全用 Harding FPA 或 PEAT 测。", 0f);
        // The treatments' placard (X) on the west wall, across from this one.
        BuildEndSign();
        // Its blade (20_placard_type.md v2.5, E-blade), built once (its title never changes, so X leaves it): the arrival reads
        // the treatments' placard 56 degrees off its face, so its code and title also hang on a blade square to the west wall
        // (face x -1.42), inner edge 50 mm off it, bottom 2.10, its title on two lines before the bracket (the blade's 12-em
        // measure), top 2.36 (0.54 under the lid; 0.52 under the troffer's west edge); front face z -4.299, 2.500 m from the
        // arrival eye, behind the arrival as the placard (turn round), read 18.5 degrees to the side and 14 up. The vestibule's
        // Level 0 ambient reaches it (VestibuleAmbient runs after this).
        FrontRoomsStructuresSigns.Blade(Root, Root.TransformPoint(new Vector3(-HalfW, 0f, -4.314f)), Root.TransformDirection(Vector3.right),
            Root.TransformPoint(Arrival + Vector3.up * FrontRoomsStructuresWalker.Eye), "SP7 · 后退的五种做法（按 X 换）", Root.position.y);
        // The plate beside the sign: the current settings, in the placard's colours (taller since the treatments: X's lines).
        var plate = new GameObject("SP7 settings plate");
        plate.transform.SetParent(Root, false);
        plate.transform.localPosition = new Vector3(HalfW - .02f, 1.5f, -5.2f);
        plate.transform.localRotation = Quaternion.LookRotation(Vector3.right);
        var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        panel.name = "Panel";
        UnityEngine.Object.Destroy(panel.GetComponent<Collider>());
        panel.transform.SetParent(plate.transform, false);
        panel.transform.localPosition = new Vector3(0f, 0f, .01f);
        panel.transform.localScale = new Vector3(1.0f, .74f, .015f);
        panel.GetComponent<Renderer>().sharedMaterial = FrontRoomsPoolMaterials.Lit("placard panel E", new Color(.086f, .082f, .075f), .1f);
        var textGo = new GameObject("Text");
        textGo.transform.SetParent(plate.transform, false);
        textGo.transform.localPosition = new Vector3(-.46f, .34f, -.002f);
        plateText = textGo.AddComponent<TextMesh>();
        var font = FrontRoomsStructuresSigns.BodyFontAsset;
        if (font != null) plateText.font = font;
        plateText.fontSize = 48;
        plateText.characterSize = .032f * 10f / 48f;   // the sign's body type (70 section 9: the same panel and type; review SP7-space-F9)
        plateText.lineSpacing = 1.15f;
        plateText.anchor = TextAnchor.UpperLeft;
        plateText.alignment = TextAlignment.Left;
        plateText.color = new Color(.957f, .945f, .91f);
        plateText.richText = true;
        var tr = textGo.GetComponent<MeshRenderer>();
        if (tr != null)
        {
            var depth = FrontRoomsStructuresSigns.DepthText(font);
            if (depth != null) tr.sharedMaterial = depth;
            tr.shadowCastingMode = ShadowCastingMode.Off;
        }
        // The Relay stand-in (71 section 10): x -0.6, z 6, facing north.
        kit.Relay(new Vector3(-.6f, 0f, 6f), new Vector3(-.6f, 0f, 20f));
        // Metric marks (M); their labels are kept for a treatment's own words (its Mark hook).
        for (var i = 0; i < MarkText.Length; i++)
        {
            kit.Mark(MarkAt[i], Vector3.left, MarkText[i]);
            var mark = Root.Find("Mark · " + MarkText[i]);
            markTexts.Add(mark != null ? mark.GetComponent<TextMesh>() : null);
        }
    }

    static readonly Vector3[] MarkAt =
    {
        new Vector3(1.3f, 1.95f, 1.5f), new Vector3(1.3f, 2.25f, 2.5f), new Vector3(1.3f, 2.3f, Fd1 - .3f), new Vector3(1.3f, 2.3f, Fd2 - .3f),
        new Vector3(1.3f, 1.7f, 20.6f), new Vector3(1.3f, 1.7f, 26.6f), new Vector3(1.3f, 1.7f, 35.6f), new Vector3(1.3f, 1.7f, 41.6f),
    };

    static readonly string[] MarkText =
    {
        "1.5 米 · 拱门入口在这里触发", "2.5 米 · Run 门入口在这里触发", "FD1 · 9 米 · 会退的房间在这里触发", "FD2 · 18 米 · 没看向尽头时最晚在这里开始退",
        "7 格 · 21 米", "9 格 · 27 米（今天）", "12 格 · 36 米", "14 格 · 42 米（上限）",
    };

    // ---------------------------------------------------------------- the room: reset, trip, the reset re-strike

    // Y, V, entering and leaving: the head back on its rest line, the hidden cells off, the record cleared, armed.
    void ResetRoom(bool moveWalker, FrontRoomsStructures host = null)
    {
        // A treatment that no longer applies leaves; once one has run, the shared objects go back as built first (X, below).
        EndBeforeReset();
        recedePending = recedeRunning = receded = false;
        beatPhase = Beat.Done;
        steps.Clear();
        seatedLines.Clear();
        finalSeatTime = -1f;
        phase = Phase.Armed;
        clock = 0f;
        headZ = Rest;
        for (var c = 0; c < Cells; c++)
        {
            revealAt[c] = float.NegativeInfinity;
            SetCell(c, c < Mathf.RoundToInt(Rest / CellLen));
        }
        foreach (var b in bandParts.Values) if (b != null) b.SetActive(false);
        PlaceHead(Rest, false);
        ShowBlocks(false);
        CloseDoors();
        if (trackPart != null) trackPart.gameObject.SetActive(Receding);
        foreach (var pair in magnets)
        {
            if (pair.Value == null) continue;
            pair.Value.SetActive(MagnetFor(pair.Key) && cellShown[CellOf(pair.Key - .2f)]);
        }
        if (blindPart != null) blindPart.gameObject.SetActive(Rest < Length - .01f);
        SetAlarmLatch(false);
        tellShown = false;
        ApplyPending();
        ApplyTube();
        if (Stops.Count > 2) Stops[2] = FrontRoomsStructuresStop.Facing("尽头前回头看", Site + new Vector3(.5f, 0f, Rest - 2.5f), Site + new Vector3(-.6f, 1.4f, 6f));
        if (moveWalker && host != null && host.Walker != null)
        {
            var p = host.Walker.transform.position - Site;
            if (p.z > Rest - .45f && p.z < HallZ0) host.Jump(this, 0);
            // A treatment's moved solids came back round the walker (EndBeforeReset's restore): home too (no one is put inside).
            else if (walkerBlocked) host.Jump(this, 0);
            else lastWalkerZ = p.z;
        }
        plateDirty = true;
        Evaluate();
        // The treatment that applies (built the first time) enters and re-arms (X, below).
        EndAfterReset();
        walkerBlocked = false;
    }

    bool MagnetFor(float line)
    {
        if (activeEnd != null)
        {
            // A treatment's own lines (its Lines hook).
            foreach (var l in endLines) if (Mathf.Abs(l - line) < .01f) return true;
            return false;
        }
        if (Mathf.Abs(line - Rest) < .01f) return true;
        if (!Receding) return false;
        if (Mathf.Abs(line - 33f) < .01f || Mathf.Abs(line - Final) < .01f) return true;
        return variant == 5 && (Mathf.Abs(line - 36f) < .01f || Mathf.Abs(line - 39f) < .01f);
    }

    // A cell drawn (its dressing, strips and heads on, dark) and lit (its lamps live, revealed): a hidden cell shows from the
    // tell of the step that uncovers it, so the head rolls away from finished corridor, and lights at that step's seat.
    void SetShown(int c, bool on)
    {
        if (c < 0 || c >= Cells) return;
        cellShown[c] = on;
        foreach (var r in cellRenderers[c]) if (r != null) r.enabled = on;
        foreach (var go in cellObjects[c]) if (go != null) go.SetActive(on);
        foreach (var pair in magnets)
            if (pair.Value != null && CellOf(pair.Key - .2f) == c) pair.Value.SetActive(on && MagnetFor(pair.Key));
    }

    void SetCell(int c, bool on)
    {
        if (c < 0 || c >= Cells) return;
        SetShown(c, on);
        cellOpen[c] = on;
    }

    void ShowCells(float from, float to)
    {
        var c0 = Mathf.RoundToInt(from / CellLen);
        var c1 = Mathf.RoundToInt(to / CellLen);
        for (var c = c0; c < c1 && c < Cells; c++) if (!cellShown[c]) SetShown(c, true);
    }

    void PlaceHead(float z, bool moving)
    {
        headZ = z;
        if (head != null) head.localPosition = new Vector3(0f, 0f, z);
        foreach (var s in sweeps) if (s != null) s.localScale = new Vector3(moving ? .012f : .02f, HeadTop, moving ? .05f : .06f);
    }

    void ShowBlocks(bool on)
    {
        if (blocksRoot != null) blocksRoot.gameObject.SetActive(on);
        foreach (var r in headWall) if (r != null) r.enabled = !on;
    }

    void CloseDoors()
    {
        entranceOpen = goalOpen = false;
        if (entranceLeaf != null) entranceLeaf.localRotation = Quaternion.identity;
        if (goalLeaf != null) goalLeaf.localRotation = Quaternion.identity;
    }

    void SetAlarmLatch(bool on)
    {
        try
        {
            if (archDetector != null) FrontRoomsAlarmTell.SetAlarm(archDetector, on);
            if (doorDetector != null) FrontRoomsAlarmTell.SetAlarm(doorDetector, on);
        }
        catch (Exception e) { FrontRoomsPoolMaterials.LogOnce("SP7 latch", "SP7: the detector latch failed: " + e.Message); }
    }

    void Trip()
    {
        if (phase != Phase.Armed) return;
        phase = Phase.Tripped;
        clock = 0f;
        SetAlarmLatch(true);
        // Today's schedule (under a treatment only when it asks for it: UsesTodayRecede).
        if (Receding && !receded && Mathf.Abs(headZ - Rest) < .01f && (activeEnd == null || activeEnd.UsesTodayRecede))
        {
            recedePending = true;
            BuildSchedule();
        }
        if (activeEnd != null)
        {
            var e = activeEnd;
            try { e.Trip(room); }
            catch (Exception ex) { EndFault(e, "trip", ex); }
            PulseEaseReset();
        }
        plateDirty = true;
    }

    void StartReset()
    {
        if (phase != Phase.Tripped) return;
        phase = Phase.Resetting;
        clock = 0f;
        // A recede still waiting for its look is called off with the alarm; one already moving finishes (never stops midway).
        if (!recedeRunning) recedePending = false;
        if (activeEnd != null)
        {
            // Today's re-strike runs on every lamp: the treatment's holds go, its wait is called off; then it hears of it.
            ReleaseHoldsNow();
            endWaiting = false;
            var e = activeEnd;
            try { e.ResetStarted(room); }
            catch (Exception ex) { EndFault(e, "reset started", ex); }
            PulseEaseReset();
        }
        plateDirty = true;
    }

    float ResetEnd => rig.resetRiseStart + rig.resetCellSeconds * Mathf.Ceil(headZ / Mathf.Max(.1f, rig.resetCellLength)) + (reduce ? rig.reducedEnvelope : rig.resetRise) + .2f;

    // 71 section 4.5: R 9 = the flinch and the final step; R 15 = the flinch, two one-cell steps, the final step. T2's beats, or
    // T1's quicker ones (seats at 1.15, 2.15, 3.15, 4.85 s for R 15).
    void BuildSchedule()
    {
        steps.Clear();
        var t2 = bodyT2;
        var line = Rest;
        steps.Add(new Step { from = line, to = line + 6f, tell = .25f, travel = t2 ? 1.1f : .75f, seat = t2 ? .2f : .15f, dwell = .2f, amp = .015f });
        line += 6f;
        while (line + 3f < Final - .01f)
        {
            steps.Add(new Step { from = line, to = line + 3f, tell = .15f, travel = t2 ? .8f : .5f, seat = t2 ? .2f : .15f, dwell = .2f, amp = .015f });
            line += 3f;
        }
        steps.Add(new Step { from = line, to = Final, tell = .25f, travel = t2 ? 1f : .9f, seat = .35f, dwell = 0f, amp = .03f, final = true });
        beatIndex = 0;
        beatPhase = Beat.Tell;
        beatClock = 0f;
    }

    // ---------------------------------------------------------------- the frame

    public override void Tick(FrontRoomsStructures host, float dt, bool isCurrent)
    {
        if (!isCurrent || host == null || host.Walker == null) return;
        // X / Shift+X (the treatment) and a treatment's fault both reset the room, so they come before the walker is read.
        TreatmentKey(host);
        if (endFault) EndRecover(host);
        var p = host.Walker.transform.position - Site;
        // The trip: crossing the arm line inside the corridor (never on a jump: Arrived sets the last z).
        if (phase == Phase.Armed && lastWalkerZ < ArmZ && p.z >= ArmZ && Mathf.Abs(p.x) < 1.5f && p.z < Length) Trip();
        lastWalkerZ = p.z;
        if (phase != Phase.Armed) clock += dt;
        // The gaze gate (71 section 4.3): the first step waits for a look down the corridor, or for 18 m.
        if (recedePending && !recedeRunning)
        {
            var fwd = host.Walker.View != null ? host.Walker.View.transform.forward : host.Walker.transform.forward;
            // In today's look the clock starts when the heads are up (about 0.6 s), so no step moves in the rig's dark (70 section 9).
            var headsUp = reduce ? rig.reducedHeadDelay + rig.reducedEnvelope : rig.headDelay + rig.headStagger * Rest + rig.headRise;
            var ready = Look != 0 || clock >= headsUp;
            if (ready && (Vector3.Angle(fwd, Root.forward) <= GazeDegrees || p.z >= Fd2)) { recedeRunning = true; beatPhase = Beat.Tell; beatClock = 0f; tellShown = false; }
        }
        if (recedeRunning) Advance(dt, p.z);
        if (activeEnd != null) EndTick(host, dt, p);
        if (phase == Phase.Resetting && clock >= ResetEnd)
        {
            phase = Phase.Armed;
            clock = 0f;
            SetAlarmLatch(false);
            ApplyPending();
            ApplyTube();
            plateDirty = true;
            if (activeEnd != null)
            {
                var e = activeEnd;
                try { e.Rearmed(room); }
                catch (Exception ex) { EndFault(e, "rearmed", ex); }
                PulseEaseReset();
            }
        }
        if (activeEnd != null || shortRoom != null) ShortRoomTick(dt);
        stutterClock += dt;
        Evaluate();
        shadowClock -= dt;
        if (shadowClock <= 0f)
        {
            shadowClock = .2f;
            Shadows(host.Walker.EyePosition);
        }
        if (plateDirty) UpdatePlate();
    }

    void Advance(float dt, float walkerZ)
    {
        if (steps.Count == 0) { recedeRunning = false; return; }
        beatClock += dt;
        var s = steps[Mathf.Clamp(beatIndex, 0, steps.Count - 1)];
        switch (beatPhase)
        {
            case Beat.Tell:
                if (!tellShown) { ShowCells(s.from, s.to); tellShown = true; }
                // The tell: the magnets let go, a 15 mm jolt toward the walker and back.
                PlaceHead(s.from - .015f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(beatClock / Mathf.Max(.01f, s.tell))), false);
                if (beatClock >= s.tell)
                {
                    beatClock -= s.tell;
                    beatPhase = Beat.Travel;
                    tellShown = false;
                    Leaving(s.from);
                    travelT2 = bodyT2;
                    if (travelT2)
                    {
                        FlyBlocks(0f, s.travel, s.from, s.to);
                        ShowBlocks(true);
                    }
                }
                break;
            case Beat.Travel:
            {
                var u = Mathf.Clamp01(beatClock / Mathf.Max(.01f, s.travel));
                PlaceHead(s.from + (s.to - s.from) * Trapezoid(u, Ramp / Mathf.Max(.01f, s.travel)), true);
                if (travelT2) FlyBlocks(beatClock, s.travel, s.from, s.to);
                if (beatClock >= s.travel)
                {
                    beatClock -= s.travel;
                    beatPhase = Beat.Seat;
                    ShowBlocks(false);
                    Seated(s.from, s.to);
                }
                break;
            }
            case Beat.Seat:
            {
                var u = Mathf.Clamp01(beatClock / Mathf.Max(.01f, s.seat));
                PlaceHead(s.to + s.amp * Mathf.Sin(Mathf.PI * u) * Mathf.Exp(-3f * u), false);
                if (beatClock >= s.seat)
                {
                    beatClock -= s.seat;
                    PlaceHead(s.to, false);
                    if (s.final) Finished();
                    else beatPhase = Beat.Dwell;
                }
                break;
            }
            case Beat.Dwell:
                PlaceHead(s.to, false);
                // The keep-away (71 section 4.5): a walker within 12 m of the head between steps: the rest as one glide.
                if (s.to - walkerZ < KeepAway && Final - s.to > .01f)
                {
                    glideFrom = s.to;
                    ShowCells(glideFrom, Final);
                    Leaving(glideFrom);
                    glideTime = (Final - s.to) / GlideSpeed + Ramp;
                    beatClock = 0f;
                    beatPhase = Beat.Glide;
                    break;
                }
                if (beatClock >= s.dwell)
                {
                    beatClock -= s.dwell;
                    beatIndex = Mathf.Min(beatIndex + 1, steps.Count - 1);
                    beatPhase = Beat.Tell;
                }
                break;
            case Beat.Glide:
            {
                var u = Mathf.Clamp01(beatClock / Mathf.Max(.01f, glideTime));
                PlaceHead(glideFrom + (Final - glideFrom) * Trapezoid(u, Ramp / Mathf.Max(.01f, glideTime)), true);
                if (beatClock >= glideTime)
                {
                    // The heavy final seat, as the last step's.
                    steps[steps.Count - 1] = new Step { from = glideFrom, to = Final, tell = 0f, travel = 0f, seat = .35f, dwell = 0f, amp = .03f, final = true };
                    beatIndex = steps.Count - 1;
                    beatClock = 0f;
                    beatPhase = Beat.Seat;
                    Seated(glideFrom, Final);
                }
                break;
            }
        }
    }

    // The head leaves a line it stood on: that line's ghost band is uncovered (the record).
    void Leaving(float from)
    {
        if (!seatedLines.Contains(from)) seatedLines.Add(from);
        if (bandParts.TryGetValue(from, out var band) && band != null) band.SetActive(true);
    }

    // A seat: the cells between the old line and the new one are revealed (their dressing on, their lamps strike once).
    void Seated(float from, float to)
    {
        var c0 = Mathf.RoundToInt(from / CellLen);
        var c1 = Mathf.RoundToInt(to / CellLen);
        for (var c = c0; c < c1 && c < Cells; c++)
        {
            if (cellOpen[c]) continue;
            SetCell(c, true);
            revealAt[c] = phase == Phase.Armed ? float.NegativeInfinity : clock;
        }
        foreach (var pair in magnets)
            if (pair.Value != null) pair.Value.SetActive(MagnetFor(pair.Key) && cellShown[CellOf(pair.Key - .2f)]);
        if (blindPart != null) blindPart.gameObject.SetActive(to < Length - .01f);
        if (activeEnd != null) EndSeated(from, to);
    }

    void Finished()
    {
        recedeRunning = recedePending = false;
        receded = true;
        beatPhase = Beat.Done;
        finalSeatTime = phase == Phase.Tripped ? clock : -1f;
        if (!seatedLines.Contains(headZ)) seatedLines.Add(headZ);
        if (Stops.Count > 2) Stops[2] = FrontRoomsStructuresStop.Facing("尽头前回头看", Site + new Vector3(.5f, 0f, headZ - 2.5f), Site + new Vector3(-.6f, 1.4f, 6f));
        plateDirty = true;
        if (activeEnd != null)
        {
            var e = activeEnd;
            try { e.Done(room); }
            catch (Exception ex) { EndFault(e, "done", ex); }
        }
    }

    // The trapezoidal travel (71 section 4.5: peak = length / (travel - 0.175 s)): ramps of fraction r at both ends.
    static float Trapezoid(float u, float r)
    {
        r = Mathf.Clamp(r, .001f, .5f);
        var v = 1f / (1f - r);
        if (u < r) return .5f * v * u * u / r;
        if (u > 1f - r) return 1f - .5f * v * (1f - u) * (1f - u) / r;
        return .5f * v * r + v * (u - r);
    }

    static float Smooth(float x)
    {
        x = Mathf.Clamp01(x);
        return x * x * (3f - 2f * x);
    }

    // T2 (71 section 5.3, programmed paths): top courses leave first, each block lifts 30 mm, turns into its stream along its
    // side wall (within 0.35 m of it; the top courses along the soffit above 2.45 m), rides back, and drops into its course at
    // the new line bottom first; nothing crosses the centre line or the lite.
    void FlyBlocks(float t, float travel, float from, float to)
    {
        foreach (var b in blocks)
        {
            if (b.t == null) continue;
            var start = (1f - b.row01) * .3f * travel;
            var end = .7f * travel + b.row01 * .3f * travel;
            var s = Mathf.Clamp01((t - start) / Mathf.Max(.01f, end - start));
            var seat0 = new Vector3(b.seat.x, b.seat.y, from);
            var seat1 = new Vector3(b.seat.x, b.seat.y, to);
            var laneX = b.laneX;
            var lane0 = new Vector3(laneX, b.laneY + .03f, from + .3f);
            var lane1 = new Vector3(laneX, b.laneY + .03f, to - .3f);
            Vector3 pos;
            float turn;
            if (s < .2f)
            {
                var k = Smooth(s / .2f);
                pos = Vector3.Lerp(seat0 + Vector3.up * .03f * k, lane0, k);
                turn = k;
            }
            else if (s < .8f)
            {
                pos = Vector3.Lerp(lane0, lane1, Smooth((s - .2f) / .6f));
                turn = 1f;
            }
            else
            {
                var k = Smooth((s - .8f) / .2f);
                pos = Vector3.Lerp(lane1, seat1 + Vector3.up * .03f * (1f - k), k);
                turn = 1f - k;
            }
            var env = Mathf.Sin(Mathf.PI * s);
            b.t.localPosition = pos;
            b.t.localRotation = Quaternion.Euler(2f * b.wobble * env, 90f * turn * b.side + 4f * b.wobble * env, 1.5f * b.wobble * env);
        }
    }

    // ---------------------------------------------------------------- light: the structures-only driver (71 sections 5.7, 6.3-6.7)

    void Evaluate()
    {
        // A treatment's lamps: today's drive, its holds and its hooks (X, below). #1 never takes this branch.
        if (activeEnd != null) { EvaluateDriven(); return; }
        var t = clock;
        foreach (var s in strips)
        {
            if (s == null || s.light == null) continue;
            if (s.cell >= 0 && !cellOpen[s.cell]) { SetStrip(s, 0f, 0f, 0f, 0f); continue; }
            StripLevels(s, t, out var w, out var r, out var lens, out var rt);
            SetStrip(s, w, r, lens, rt);
        }
        foreach (var h in heads)
        {
            if (h == null || h.light == null) continue;
            if (h.cell >= 0 && !cellOpen[h.cell]) { h.light.enabled = false; SetLens(h.lens, h.lensIndex, Color.black); continue; }
            var f = HeadLevel(h, t);
            var v = h.baseIntensity * f;
            h.light.intensity = v;
            h.light.enabled = v > .0005f;
            SetLens(h.lens, h.lensIndex, rig.headLensEmission * f);
        }
        var pilot = phase == Phase.Tripped ? 0f : 1f;
        foreach (var e in pilots)
        {
            if (e == null || e.renderer == null) continue;
            if (e.cell >= 0 && !cellOpen[e.cell]) continue;
            block.Clear();
            e.renderer.GetPropertyBlock(block, e.index);
            block.SetColor(EmissionId, rig.pilotEmission * pilot);
            e.renderer.SetPropertyBlock(block, e.index);
        }
    }

    float WhiteDrop(float t) => reduce ? Mathf.Clamp01(1f - t / rig.reducedEnvelope) : Mathf.Clamp01(1f - t / rig.mainsDrop);

    float Afterglow(float t)
    {
        if (t < 0f) return 0f;
        var end = reduce ? rig.reducedEnvelope : rig.mainsDrop;
        return t <= end ? rig.afterglow : rig.afterglow * Mathf.Exp(-(t - end) / Mathf.Max(.01f, rig.afterglowTau));
    }

    // The red strike: 0 -> 0.35 at once, then a smoothstep to 1 over 0.40 s (the reset's own curve); Reduce Flashing: a 0.5 s rise.
    float RedRise(float t)
    {
        if (t < 0f) return 0f;
        if (reduce) return Smooth(t / rig.reducedEnvelope);
        return .35f + .65f * Smooth(t / rig.resetRise);
    }

    // A revealed cell's strip strikes once: a rise, never a flash.
    float Strike(float t) => t < 0f ? 0f : Smooth(t / (reduce ? rig.reducedEnvelope : rig.resetRise));

    // Per strip: w the white tubes and Light, r the room's red (the Light recoloured, the halo), lens the white tubes' glow, rt
    // the red tube's own burn on the battery circuit. Today's Run! B (the rig as landed 11-04) strikes the red tube with the
    // heads and turns no Light red; looks A, B and C turn the room red at Red's picked level and burn the tube with it.
    void StripLevels(StripLamp s, float t, out float w, out float r, out float lens, out float rt)
    {
        w = 1f;
        r = 0f;
        lens = 1f;
        rt = 0f;
        var c = s.cell;
        var late = c >= 0 && !float.IsNegativeInfinity(revealAt[c]);
        var tr = late ? t - revealAt[c] : 0f;
        if (phase == Phase.Armed)
        {
            w = s.stutter ? Stutter() : 1f;
            lens = w;
            return;
        }
        if (phase == Phase.Resetting)
        {
            // Red tubes out with the heads in one step; the flick ripple from the entrance, dark, the white re-strike.
            w = Restrike(t, s.z);
            if (late) w *= Smooth(tr / .4f);
            lens = w;
            return;
        }
        switch (Look)
        {
            case 0:
            {
                // Today's Run! B: the mains drop at the trip (0.06 s), the lens afterglow; the red tube strikes with the heads
                // (their relay at 0.35 + 0.004 z, an 0.18 s ease-in rise; Reduce Flashing a 0.5 s rise from 0.25); a revealed
                // strip's tube strikes at its seat.
                w = late ? 0f : WhiteDrop(t);
                lens = late ? 0f : Mathf.Lerp(Afterglow(t), 1f, w);
                rt = late ? Strike(t - Mathf.Max(revealAt[c], rig.headDelay + rig.headStagger * s.z)) : RigHeadRise(t, s.z);
                return;
            }
            case 1:
            case 2:
            {
                // A, B: a dark band runs from the entrance at 20 m/s (strip c white 1 -> 0 at 0.15 c), its red strikes at 0.35 + 0.15 c.
                var t0 = .15f * c;
                if (late)
                {
                    w = 0f;
                    r = Strike(t - Mathf.Max(revealAt[c], .35f + t0));
                    lens = 0f;
                }
                else if (reduce)
                {
                    var k = Smooth((t - t0) / rig.reducedEnvelope);
                    w = 1f - k;
                    r = k;
                    lens = w;
                }
                else
                {
                    w = t < t0 ? 1f : WhiteDrop(t - t0);
                    r = RedRise(t - .35f - t0);
                    lens = t < t0 ? 1f : Mathf.Lerp(Afterglow(t - t0), 1f, w);
                }
                if (Look == 2 && !reduce) r *= Pulse(t, c);
                rt = r;
                return;
            }
            default:
            {
                // C: the red tide from the opening at walking pace (strip c at 0.25 + (1.8 + 3c) / 3.2 s), a one-way cross-fade.
                var tc = .25f + s.z / 3.2f;
                var k = Smooth((t - tc) / (reduce ? rig.reducedEnvelope : rig.resetRise));
                w = 1f - k;
                r = k;
                if (late) { var st = Strike(tr); w *= st; r *= st; }
                lens = w;
                rt = r;
                return;
            }
        }
    }

    // The rig's head rise at z (its relay delay and ease-in; Reduce Flashing: the 0.5 s envelope from 0.25 s).
    float RigHeadRise(float t, float z)
    {
        if (reduce) return Smooth((t - rig.reducedHeadDelay) / rig.reducedEnvelope);
        var f = Mathf.Clamp01((t - (rig.headDelay + rig.headStagger * z)) / rig.headRise);
        return f * f;
    }

    // B's pulse (71 section 6.4): every 2.5 s from 3.0 s after the trip (in a receding room from the later of 3.0 s and 1.0 s
    // after the final seat), each strip from the entrance dips 1.00 -> 0.35 in 0.06 s, holds 0.10 s, rises over 0.40 s;
    // 12 pulses at most (counted from the trip), none while the head moves.
    float Pulse(float t, int c)
    {
        // Under a treatment the gate and the schedule's start ease over 0.4 s when they change (PulseEased, X below; the
        // treatments' request); #1 runs the lines after this one as before.
        if (activeEnd != null) return PulseEased(t, .15f * c);
        // (endMoving and endWaiting are a treatment's; both stay false under #1.)
        if (recedePending || recedeRunning || endMoving || endWaiting) return 1f;
        var first = 3f;
        if (finalSeatTime >= 0f) first = Mathf.Max(3f, finalSeatTime + 1f);
        var level = 1f;
        for (var k = 0; k < 12; k++)
        {
            var start = first + 2.5f * k + .15f * c;
            var s = t - start;
            if (s < 0f) break;
            if (s < .06f) level = Mathf.Min(level, 1f - .65f * (s / .06f));
            else if (s < .16f) level = Mathf.Min(level, .35f);
            else if (s < .56f) level = Mathf.Min(level, .35f + .65f * Smooth((s - .16f) / .4f));
        }
        return level;
    }

    // The rig's re-strike (RunRigLayout): one flick a lamp rippling from the entrance 0.025 s a cell, dark, then a rise from
    // 0.35 swept from 0.45 s at 0.15 s a cell; Reduce Flashing: a 0.5 s rise, no flick.
    float Restrike(float tr, float z)
    {
        var cell = Mathf.Floor(Mathf.Max(0f, z) / Mathf.Max(.1f, rig.resetCellLength));
        if (reduce)
        {
            var sr = tr - cell * rig.resetCellSeconds;
            return sr < 0f ? 0f : Smooth(sr / rig.reducedEnvelope);
        }
        var flick = cell * rig.resetRippleSeconds;
        if (tr >= flick && tr < flick + rig.resetFlickOn) return rig.resetFlickLevel;
        var s = tr - (rig.resetRiseStart + cell * rig.resetCellSeconds);
        if (s < 0f) return 0f;
        return Mathf.Lerp(.35f, 1f, Smooth(s / rig.resetRise));
    }

    // The stutter tube (cell 5): the rig's Unstable model round 0.78; steady 0.78 with Reduce Flashing.
    float Stutter()
    {
        if (reduce) return rig.stutterLevel;
        var t = stutterClock + 3.1f;
        var wave = .5f + .5f * Mathf.Sin(t * 3.7f) * (.68f + .32f * Mathf.Sin(t * 1.13f));
        var dropout = Mathf.PerlinNoise(t * .92f, 7.3f) > .77f ? .38f : 1f;
        return rig.stutterLevel * (.86f + .14f * Mathf.Clamp01(wave)) * dropout;
    }

    float HeadLevel(HeadLamp h, float t)
    {
        if (phase != Phase.Tripped) return 0f;
        float start;
        if (h.goal) start = reduce ? rig.reducedHeadDelay : rig.headDelay + rig.headStagger * headZ;
        else
        {
            switch (Look)
            {
                case 0: start = reduce ? rig.reducedHeadDelay : rig.headDelay + rig.headStagger * h.z; break;
                case 1:
                case 2: start = .35f + h.z / 20f; break;
                default: start = .25f + h.z / 3.2f; break;
            }
            if (h.cell >= 0 && !float.IsNegativeInfinity(revealAt[h.cell])) start = Mathf.Max(start, revealAt[h.cell]);
        }
        if (reduce) return Smooth((t - start) / rig.reducedEnvelope);
        var f = Mathf.Clamp01((t - start) / rig.headRise);
        return f * f;
    }

    // The strip Light's intensity at full red: Red's pick on the HDR still follows the rig's tube level (tube linear 3 ->
    // Light 3.5, 2 -> 2.3, else 71 6.3's 1.15).
    float RedLight => rig.redTubeLinear.r >= 2.5f ? 3.5f : rig.redTubeLinear.r >= 1.5f ? 2.3f : 1.15f;

    // One strip's light, lenses, red tube and halo: white level w (Run! B's wrap light), room red r (the same Light recoloured,
    // 71 6.3, at Red's picked level), the white tubes' lens level, the red tube as the rig's RedTube (its coating shows the
    // white tubes, redTubeBounce x their level, or it burns at redTubeLinear x rt, whichever is greater), the soffit halo.
    void SetStrip(StripLamp s, float w, float r, float lensLevel, float rt)
    {
        if (s == null || s.light == null) return;
        var white = rig.wrapIntensity * s.scale * Mathf.Max(0f, w);
        var red = RedLight * Mathf.Max(0f, r);
        var sum = white + red;
        s.light.enabled = sum > .0005f;
        if (sum > .0005f)
        {
            // The red Light takes the rig's tube hue (a near-pure deep red that ACES keeps red; 71's alarm red leaned orange).
            var hue = rig.redTubeLinear.maxColorComponent > 1e-4f ? rig.redTubeLinear / rig.redTubeLinear.maxColorComponent : AlarmRedLinear;
            var c = (rig.wrapColor.linear * white + hue * red) / sum;
            s.light.color = c.gamma;
            s.light.intensity = sum;
        }
        if (s.lens != null)
        {
            block.Clear();
            s.lens.GetPropertyBlock(block);
            block.SetColor(EmissionId, rig.stripLensEmission * Mathf.Max(0f, lensLevel));
            s.lens.SetPropertyBlock(block);
        }
        if (s.red != null)
        {
            var bounce = rig.redTubeBounce * Mathf.Max(0f, lensLevel);
            var burn = rig.redTubeLinear.gamma * Mathf.Clamp01(rt);
            block.Clear();
            s.red.GetPropertyBlock(block);
            block.SetColor(EmissionId, new Color(Mathf.Max(bounce.r, burn.r), Mathf.Max(bounce.g, burn.g), Mathf.Max(bounce.b, burn.b), 1f));
            s.red.SetPropertyBlock(block);
        }
        if (s.halo != null)
        {
            var redHalo = r > lensLevel && haloRed != null;
            var level = redHalo ? Mathf.Clamp01(r) : Mathf.Max(0f, lensLevel);
            if (redHalo && s.halo.sharedMaterial != haloRed) s.halo.sharedMaterial = haloRed;
            else if (!redHalo && haloWhite != null && s.halo.sharedMaterial != haloWhite) s.halo.sharedMaterial = haloWhite;
            s.halo.enabled = level > .0005f;
            block.Clear();
            block.SetFloat(DensityId, level);
            s.halo.SetPropertyBlock(block);
        }
    }

    void SetLens(Renderer[] renderers, int[] indices, Color c)
    {
        if (renderers == null || indices == null) return;
        for (var i = 0; i < renderers.Length && i < indices.Length; i++)
        {
            var r = renderers[i];
            if (r == null) continue;
            block.Clear();
            r.GetPropertyBlock(block, indices[i]);
            block.SetColor(EmissionId, c);
            r.SetPropertyBlock(block, indices[i]);
        }
    }

    // Shadows (71 5.2: inside 10_plan 3's 3 armed / 8 tripped): the three nearest lit strips, the three nearest lit heads and
    // the goal heads soft-shadowed; the rest unshadowed.
    readonly List<Light> shadowPick = new List<Light>();

    void Shadows(Vector3 eye)
    {
        shadowPick.Clear();
        Nearest(eye, 3, true);
        Nearest(eye, 3, false);
        foreach (var g in goalHeads) if (g != null && g.light != null && g.light.enabled) shadowPick.Add(g.light);
        foreach (var s in strips) Shadow(s?.light);
        foreach (var h in heads) Shadow(h?.light);
    }

    void Shadow(Light l)
    {
        if (l == null) return;
        var want = shadowPick.Contains(l) ? LightShadows.Soft : LightShadows.None;
        if (l.shadows != want) l.shadows = want;
    }

    void Nearest(Vector3 eye, int count, bool stripsNotHeads)
    {
        for (var n = 0; n < count; n++)
        {
            Light best = null;
            var bestD = float.MaxValue;
            if (stripsNotHeads)
            {
                foreach (var s in strips)
                {
                    if (s == null || s.light == null || !s.light.enabled || shadowPick.Contains(s.light)) continue;
                    // Revealed strips unshadowed (71 5.2), except with the walker at the door or in the hall (eye past the rest line
                    // less 3 m): there the nearest strips take the shadows, so no strip lights the hall through its wall.
                    if (s.cell >= 0 && !float.IsNegativeInfinity(revealAt[s.cell]) && (eye - Site).z <= Rest - 3f) continue;
                    var d = (s.light.transform.position - eye).sqrMagnitude;
                    if (d < bestD) { bestD = d; best = s.light; }
                }
            }
            else
            {
                foreach (var h in heads)
                {
                    if (h == null || h.goal || h.light == null || !h.light.enabled || shadowPick.Contains(h.light)) continue;
                    var d = (h.light.transform.position - eye).sqrMagnitude;
                    if (d < bestD) { bestD = d; best = h.light; }
                }
            }
            if (best == null) return;
            shadowPick.Add(best);
        }
    }

    static readonly string[] PlateVariant = { "普通 7 格", "普通 9 格", "普通 12 格", "普通 14 格", "会退 9→12 格", "会退 9→14 格" };
    static readonly string[] PlateLook = { "今天的", "A 警报回路", "B 警报脉冲", "C 红潮" };

    // The plate beside the sign (70 section 9's lines, each with its current value; the keys per the plan).
    void UpdatePlate()
    {
        plateDirty = false;
        if (plateText == null) return;
        string Later(int pending, string now) => pending < 0 ? "" : "  <color=#96938B>（下次：" + now + "）</color>";
        var state = phase == Phase.Armed ? (receded ? "待触发（已退过，按 Y 放回原位）" : "待触发（走过 " + ArmZ.ToString("0.#") + " 米）") : phase == Phase.Tripped ? "警报中（按 T 恢复）" : "恢复中";
        plateText.text = "长度（V）：<color=#F4DF3B>" + PlateVariant[variant] + "</color>\n"
            + "后退方式（B）：<color=#F4DF3B>" + (bodyT2 ? "T2 重组" : "T1 后退") + "</color>" + Later(pendingBody, pendingBody == 1 ? "T2 重组" : "T1 后退") + (activeEnd != null && !activeEnd.UsesBody ? "  <color=#96938B>（这种做法不用）</color>" : "") + "\n"
            + "触发后的灯光（L）：<color=#F4DF3B>" + PlateLook[look] + "</color>" + Later(pendingLook, pendingLook >= 0 ? PlateLook[pendingLook] : "") + "\n"
            + "红灯管（H）：<color=#F4DF3B>" + (tube ? "开" : "关") + "</color>" + Later(pendingTube, pendingTube == 1 ? "开" : "关") + "\n"
            + "入口（O）：<color=#F4DF3B>" + (doorEntrance ? "Run 门" : "拱门") + "</color>" + (Receding ? "（会退的只用拱门）" : "") + Later(pendingDoor, pendingDoor == 1 ? "Run 门" : "拱门") + "\n"
            + "减少闪烁（F）：<color=#F4DF3B>" + (reduce ? "开" : "关") + "</color>\n"
            + PlateTreatment()
            + "<color=#96938B>" + state + EndStatus(" · ") + "</color>";
    }

    // The plate's treatment lines (X): the current one, one line on what to try (or what it does in this length), N's line.
    string PlateTreatment()
    {
        var e = CurrentEnd;
        var sb = new StringBuilder();
        sb.Append("后退的做法（X）：<color=#F4DF3B>").Append(e.Number).Append(' ').Append(e.Name).Append("</color>\n");
        string note;
        if (endFailed[treatment]) note = "这一种出错了，现在跑的是今天的（日志里有原因）";
        else if (!Receding && !e.RunsInPlain) note = e.PlainLine;
        else note = e.TryLine.Length > 0 ? "试：" + e.TryLine : "";
        PlateLines(sb, note);
        if (activeEnd != null)
        {
            var option = "";
            try { option = activeEnd.OptionLine ?? ""; }
            catch (Exception ex) { EndFault(activeEnd, "option line", ex); }
            PlateLines(sb, option);
        }
        return sb.ToString();
    }

    // A muted note on the plate, wrapped to the plate's width, each line in its own colour tag.
    static void PlateLines(StringBuilder sb, string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        foreach (var line in FrontRoomsStructuresSigns.Wrap(text, 27).Split('\n'))
            if (line.Length > 0) sb.Append("<color=#96938B>").Append(line).Append("</color>\n");
    }

    // The active treatment's own words of state, after a separator, or nothing.
    string EndStatus(string separator)
    {
        if (activeEnd == null) return "";
        var s = "";
        try { s = activeEnd.Status(room) ?? ""; }
        catch (Exception ex) { EndFault(activeEnd, "status", ex); }
        return s.Length > 0 ? separator + s : "";
    }

    // ---------------------------------------------------------------- keys, doors, the hall's plate

    public override FrontRoomsStructuresLook.Preset PresetAt(Vector3 world)
    {
        var today = TodayPresetAt(world);
        if (activeEnd == null && !fogOn && !ambientOn) return today;
        // A treatment may pick another preset; a fog or ambient override goes on after the preset (X, below). Inside the stand-in
        // room behind a short-opening door the look is the armed corridor's (a plain lit room, as the hall's).
        if (activeEnd != null && InShortRoom(world - Site)) today = FrontRoomsStructuresLook.Preset.RunB;
        var preset = today;
        if (activeEnd != null)
        {
            var e = activeEnd;
            try { preset = e.PresetAt(room, world - Site, today); }
            catch (Exception ex) { EndFault(e, "preset", ex); preset = today; }
        }
        FogAfterPreset(preset);
        return preset;
    }

    FrontRoomsStructuresLook.Preset TodayPresetAt(Vector3 world)
    {
        var p = world - Site;
        // No Level 0 look in the vestibule any more: from the arrival the corridor showed in Level 0's trilight and reflection,
        // and crossing z 0 snapped every colour (review SP7-light-F1); the vestibule's own renderers carry Level 0's ambient.
        if (p.z > HallZ0) return FrontRoomsStructuresLook.Preset.RunB;
        if (phase == Phase.Resetting) return clock < rig.resetRiseStart ? FrontRoomsStructuresLook.Preset.RunBTripped : FrontRoomsStructuresLook.Preset.RunB;
        if (phase != Phase.Tripped) return FrontRoomsStructuresLook.Preset.RunB;
        if (Look == 0) return FrontRoomsStructuresLook.Preset.RunBTripped;
        if (Look == 3) return p.z <= 3.2f * (clock - .25f) ? FrontRoomsStructuresLook.Preset.RunBRed : FrontRoomsStructuresLook.Preset.RunB;
        return clock >= .35f ? FrontRoomsStructuresLook.Preset.RunBRed : FrontRoomsStructuresLook.Preset.RunBTripped;
    }

    public override bool KeyPressed(FrontRoomsStructures host, Key key, out string message)
    {
        message = null;
        var armed = phase == Phase.Armed;
        var later = armed ? "" : "（下次待触发时生效）";
        switch (key)
        {
            case UnityEngine.InputSystem.Key.V:
                variant = (variant + 1) % RestLine.Length;
                ResetRoom(true, host);
                message = "长度：" + VariantNames[variant] + (Final < Length - .01f ? "（尽头的门在场景 4 里打不开）" : "") + (Receding ? "；会退的只用拱门" : "");
                // The treatment picked by X, when it is not today's: running here or not.
                if (treatment != 0) message += "；后退的做法 " + CurrentEnd.Number + (activeEnd != null ? "" : endFailed[treatment] ? "：出错了，用的是今天的" : "：" + CurrentEnd.PlainLine);
                return true;
            case UnityEngine.InputSystem.Key.B:
            {
                var next = !(pendingBody >= 0 ? pendingBody == 1 : bodyT2);
                if (armed) { bodyT2 = next; pendingBody = -1; } else pendingBody = next ? 1 : 0;
                plateDirty = true;
                message = "后退方式：" + (next ? "T2 砖块拆开再砌回去" : "T1 整面墙滑走") + later + (activeEnd != null && !activeEnd.UsesBody ? "（现在的做法不用这一项）" : "");
                return true;
            }
            case UnityEngine.InputSystem.Key.N:
            {
                // The current treatment's own option (none under #1: the host says the key is not here, as before).
                if (activeEnd == null) return false;
                var e = activeEnd;
                try
                {
                    if (!e.Option(room, out message)) return false;
                    plateDirty = true;
                    return true;
                }
                catch (Exception ex)
                {
                    EndFault(e, "option", ex);
                    message = null;
                    return false;
                }
            }
            case UnityEngine.InputSystem.Key.L:
            {
                var next = ((pendingLook >= 0 ? pendingLook : look) + 1) % LookNames.Length;
                if (armed) { look = next; pendingLook = -1; } else pendingLook = next;
                var tubeOff = !(pendingTube >= 0 ? pendingTube == 1 : tube);
                var tubeOn = next != 0 && tubeOff;
                if (tubeOn)
                {
                    // A, B and C need the red tube, so choosing one turns it on (70 section 9; review SP7-light-F8).
                    if (armed) { tube = true; pendingTube = -1; ApplyTube(); } else pendingTube = 1;
                }
                plateDirty = true;
                message = "触发后的灯光：" + LookNames[next] + (tubeOn ? "（红灯管随之打开）" : "") + later;
                return true;
            }
            case UnityEngine.InputSystem.Key.H:
            {
                var next = !(pendingTube >= 0 ? pendingTube == 1 : tube);
                if (armed) { tube = next; pendingTube = -1; ApplyTube(); } else pendingTube = next ? 1 : 0;
                plateDirty = true;
                message = "红灯管：" + (next ? "开" : "关（灯架同 11 月 3 日的 R-B 灯管，灯光同今天的）") + later;
                return true;
            }
            case UnityEngine.InputSystem.Key.O:
            {
                if (Receding) { message = "会退的只用拱门（一个房间只有一种变化）。"; return true; }
                var next = !(pendingDoor >= 0 ? pendingDoor == 1 : doorEntrance);
                if (armed) { SetEntrance(next); pendingDoor = -1; } else pendingDoor = next ? 1 : 0;
                plateDirty = true;
                message = "入口：" + (next ? "Run 门（在 2.5 米触发）" : "拱门（在 1.5 米触发）") + later;
                return true;
            }
            case UnityEngine.InputSystem.Key.F:
                reduce = !reduce;
                plateDirty = true;
                message = "减少闪烁：" + (reduce ? "开" : "关") + "（只在这一区）";
                return true;
            case UnityEngine.InputSystem.Key.T:
                if (phase == Phase.Armed) { Trip(); message = "警报触发（手动）。"; }
                else if (phase == Phase.Tripped) { StartReset(); message = "恢复：白光从入口一格一格亮回来。"; }
                else message = "正在恢复。";
                return true;
            case UnityEngine.InputSystem.Key.Y:
                ResetRoom(true, host);
                message = "门放回原位，重新待触发。";
                return true;
        }
        return false;
    }

    // B, L, H and O pressed while the room was tripped take effect here, when it is armed again; a receding room keeps the arch.
    void ApplyPending()
    {
        if (pendingLook >= 0) { look = pendingLook; pendingLook = -1; }
        if (pendingBody >= 0) { bodyT2 = pendingBody == 1; pendingBody = -1; }
        if (pendingTube >= 0) { tube = pendingTube == 1; pendingTube = -1; }
        if (pendingDoor >= 0) { SetEntrance(pendingDoor == 1); pendingDoor = -1; }
        if (Receding && doorEntrance) SetEntrance(false);
        plateDirty = true;
    }

    void SetEntrance(bool door)
    {
        doorEntrance = door;
        if (archPart != null) archPart.gameObject.SetActive(!door);
        if (doorPart != null) doorPart.gameObject.SetActive(door);
        CloseDoors();
    }

    // H: the corridor strips' red tube shown or hidden (off shows the two-tube strip Red approved on 11-03).
    void ApplyTube()
    {
        foreach (var st in strips) if (st != null && st.red != null) st.red.enabled = tube;
    }

    // Scene 4: the hall is only behind z 42. Under a treatment its DoorReachable hook says whether the door can be pushed now, and
    // one that OpensShort has its door open wherever the head stands (the stand-in room behind it).
    bool GoalCanOpen => activeEnd == null ? Mathf.Abs(headZ - Length) < .01f && !recedeRunning
        : (Mathf.Abs(headZ - Length) < .01f || EndOpensShort()) && EndDoorReachable();

    public override string Describe(Collider c, out bool hold)
    {
        hold = false;
        if (c == null) return null;
        if (c == entranceLeafCollider) return entranceOpen ? "E  ·  关门" : "E  ·  开门";
        if (c == goalLeafCollider)
        {
            if (!GoalCanOpen) return EndDoorPrompt("这扇门在场景 4 里打不开（长度不到 14 格；游戏里门永远不锁）", false);
            return EndDoorPrompt(goalOpen ? "E  ·  关门" : "E  ·  开门（推杠）", true);
        }
        return null;
    }

    public override void Use(Collider c)
    {
        if (c == null) return;
        if (c == entranceLeafCollider && entranceLeaf != null)
        {
            entranceOpen = !entranceOpen;
            entranceLeaf.localRotation = Quaternion.Euler(0f, entranceOpen ? -95f : 0f, 0f);
        }
        else if (c == goalLeafCollider && goalLeaf != null && GoalCanOpen)
        {
            goalOpen = !goalOpen;
            goalLeaf.localRotation = Quaternion.Euler(0f, goalOpen ? -95f : 0f, 0f);
        }
    }

    public override string FeetPrompt(FrontRoomsStructures host, Vector3 feet)
    {
        var p = feet - Site;
        return new Vector2(p.x - 3f, p.z - 49.5f).sqrMagnitude < .45f * .45f && Mathf.Abs(p.y) < .6f ? "E  ·  回到入口（SP7 到达点）" : null;
    }

    public override void UseAtFeet(FrontRoomsStructures host, Vector3 feet)
    {
        if (FeetPrompt(host, feet) != null && host != null) host.Jump(this, 0);
    }

    public override void Arrived(FrontRoomsStructures host, int stop)
    {
        if (host != null && host.Walker != null) lastWalkerZ = (host.Walker.transform.position - Site).z;
    }

    public override void Enter(FrontRoomsStructures host)
    {
        ResetRoom(false);
        SetEntrance(doorEntrance && !Receding);
    }

    public override void Leave(FrontRoomsStructures host)
    {
        ResetRoom(false);
    }

    public override void RelayBody(FrontRoomsStructures host, bool on)
    {
        if (kit != null) kit.SetRelay(on);
    }

    public override void ShowMetrics(FrontRoomsStructures host, bool on)
    {
        if (kit != null) kit.ShowMarks(on);
    }

    public override string Status(FrontRoomsStructures host)
    {
        var state = phase == Phase.Armed ? (receded ? "已退过（Y 放回）" : "待触发") : phase == Phase.Tripped ? (recedePending && !recedeRunning ? "警报中 · 等你看向尽头" : recedeRunning ? "警报中 · 尽头在退" : "警报中") : "恢复中";
        return "做法 " + CurrentEnd.Number + " " + CurrentEnd.Name + (activeEnd == null && treatment != 0 ? "（这里不起作用）" : "") + " · "
            + PlateVariant[variant] + " · " + (bodyT2 ? "T2" : "T1") + " · 灯光 " + PlateLook[Look] + " · 红灯管" + (tube ? "开" : "关") + " · " + (doorEntrance ? "Run 门" : "拱门") + " · 减少闪烁" + (reduce ? "开" : "关") + " · " + state + EndStatus(" · ");
    }

    public override void Guide(FrontRoomsStructures host, StringBuilder sb)
    {
        sb.AppendLine(Title);
        sb.AppendLine("Run! B 的设备走廊，平地，最长 14 格（42 米），尽头是一整块会动的端头：墙、带 15×15 英寸小方窗的 Run 门、门上方架子上的应急灯。会退的版本在 9 米那道防火门框触发警报，端头按 71 号文档的节奏往后退；普通的版本就是 7、9、12、14 格的固定长度。");
        sb.AppendLine();
        sb.AppendLine("按键：V 长度；B 后退方式 T2 / T1；L 触发后的灯光（今天的、A、B、C）；H 红灯管；O 入口（拱门、Run 门，只在普通长度）；F 减少闪烁；T 手动触发 / 恢复；Y 把门放回原位；E 开门；[ ] 换站点；R 回到站点；M 尺寸；C Relay 体型。B、L、H、O 在警报中按下，等下次待触发时生效。");
        sb.AppendLine();
        // The treatments (X): the five, the current one, its own lines.
        sb.Append("后退的五种做法（72 号文档；X 换，Shift+X 往回，换了就复位；N 当前做法的附加选项）：");
        for (var i = 0; i < ends.Length; i++) sb.Append(i > 0 ? "；" : "").Append(ends[i].Number).Append(' ').Append(ends[i].Name);
        sb.AppendLine("。都只在会退的长度里起作用；每一种怎么试，写在入口西墙的牌子上。");
        sb.AppendLine("现在是 " + CurrentEnd.Number + " " + CurrentEnd.Name + (endFailed[treatment] ? "（出错了，跑的是今天的）" : activeEnd == null && treatment != 0 ? "（" + CurrentEnd.PlainLine + "）" : "") + "。");
        if (activeEnd != null)
        {
            var e = activeEnd;
            try { e.Guide(room, sb); }
            catch (Exception ex) { EndFault(e, "guide", ex); }
        }
        sb.AppendLine();
        sb.AppendLine("要你决定（在对话里按编号告诉总控）：");
        sb.AppendLine("SP7-2 五种做法都走过以后，选哪一种？回一个编号，比如\"2\"；也可以组合，比如\"2，加上 3 的灯光\"。");
        sb.AppendLine("SP7-1 走过以后：会退的门、Run 门入口和红色灯光，放进游戏吗？（71 的九个决定你已回了\"全部按推荐\"，这里照做；走过觉得哪里不对，直接说。）");
        sb.AppendLine("红灯管：你 11 月 4 日回了\"加，用更亮档\"，又选了最亮那一档；这里默认开，按 H 关掉对比。");
        sb.AppendLine("还没定的（游戏里测，这里测不出来）：被追着领先 8 米冲进会退的房间能不能逃掉；冲到门口关上门的人有多少真能甩掉它；12 格和 14 格在 100 个种子里放得下多少；B 的光敏安全用 Harding FPA 或 PEAT 测。");
        sb.AppendLine();
        sb.AppendLine("灰盒占位：砖块重组的路径（简单程序动画）、轨道、门吸、密封条、端头的开槽和毛刷、墙上的\"印子\"；灯光由场景 4 专用的脚本模拟，正式效果等游戏视觉做；没有声音，没有会动的 Relay。不到 14 格时尽头的门在这里打不开（游戏里这扇门永远不锁）。黑色人形是 Relay 的静态替身（2.05 米），只用来检查视线，不会动。");
        sb.AppendLine();
        sb.AppendLine("技术记录：走廊的地面、墙、管线、消防设备和应急灯是游戏自己的 Run! B 布置（按 14 格摆），灯架是游戏现在的三管 R-B 灯（中间那根是红管）；Run! B 走廊里那三件障碍物没有放，冲刺不受阻挡。红光的亮度按你在静帧图上选的那一档（红管 3、灯 3.5、光晕 0.40）；后退、砖块和红色灯光的状态由场景 4 专用的脚本按 70、71 号文档的数值模拟，游戏的 Run! B 灯光装置只有红管跟着应急灯亮，其余等游戏视觉做。远处裁切 46 米，和游戏一样。减少闪烁只在这一区切换，不改游戏的设置。离开这一区会复位。");
    }

    public override void Release()
    {
        // The treatments free what they made; the fog goes back.
        for (var i = 0; i < ends.Length; i++)
        {
            if (!endBuilt[i] || ends[i] == null) continue;
            try { ends[i].Release(room); }
            catch (Exception e) { FrontRoomsPoolMaterials.LogOnce("SP7 end release " + i, "SP7: treatment " + ends[i].Number + " release: " + e.Message); }
        }
        ClearFogNow();
        ClearAmbientNow();
        foreach (var m in owned) if (m != null) UnityEngine.Object.Destroy(m);
        owned.Clear();
        foreach (var tx in ownedTextures) if (tx != null) UnityEngine.Object.Destroy(tx);
        ownedTextures.Clear();
        base.Release();
    }

    // ================================================================ treatments (X): the frame's side (72 section 3)
    //
    // X picks a treatment and resets the room. ResetRoom calls EndBeforeReset first (a treatment that no longer applies exits;
    // once one has run, its lamp holds, the fog and every shared object go back as they stood before any treatment ran) and
    // EndAfterReset last (the treatment that applies is built the first time, enters, re-arms in its Reset and names its
    // lines). While one is active: Trip, Tick, the seats, T's re-strike, the lamps, the preset, the door and N go through its
    // hooks, each in a try/catch; a fault logs once, the treatment is left out and the next frame resets the room without it.

    bool liteTouched;

    FrontRoomsStructuresRunEnd CurrentEnd => ends[Mathf.Clamp(treatment, 0, ends.Length - 1)];

    FrontRoomsStructuresRunEndPhase EndPhase => phase == Phase.Armed ? FrontRoomsStructuresRunEndPhase.Armed
        : phase == Phase.Tripped ? FrontRoomsStructuresRunEndPhase.Tripped : FrontRoomsStructuresRunEndPhase.Resetting;

    // The treatment that drives the room for the pick and the variant, or null (today's code: #1, a plain variant, a fault).
    FrontRoomsStructuresRunEnd WantedEnd()
    {
        var e = CurrentEnd;
        if (e == null || e.IsToday || endFailed[treatment]) return null;
        if (!Receding && !e.RunsInPlain) return null;
        return e;
    }

    // X / Shift+X while SP7 is current (the host routes no X to a district): the next or previous treatment, the room reset.
    void TreatmentKey(FrontRoomsStructures host)
    {
        var k = Keyboard.current;
        if (k == null || !k.xKey.wasPressedThisFrame) return;
        var back = k.leftShiftKey.isPressed || k.rightShiftKey.isPressed;
        treatment = ((treatment + (back ? -1 : 1)) % ends.Length + ends.Length) % ends.Length;
        ResetRoom(true, host);
        BuildEndSign();
        plateDirty = true;
        host.Flash(TreatmentMessage());
    }

    string TreatmentMessage()
    {
        var e = CurrentEnd;
        var s = "后退的做法：" + e.Number + " " + e.Name;
        if (endFailed[treatment]) return s + "（出错了，现在跑的是今天的，日志里有原因）";
        if (!Receding && !e.RunsInPlain) return s + "（" + e.PlainLine + "）";
        return e.TryLine.Length > 0 ? s + "。" + e.TryLine : s;
    }

    void EndRecover(FrontRoomsStructures host)
    {
        endFault = false;
        ResetRoom(true, host);
        BuildEndSign();
        plateDirty = true;
        if (host != null) host.Flash("后退的做法 " + CurrentEnd.Number + " 出错了，现在跑的是今天的（日志里有原因）。");
    }

    void EndFault(FrontRoomsStructuresRunEnd e, string where, Exception ex)
    {
        var i = Array.IndexOf(ends, e);
        var n = e != null ? e.Number : 0;
        FrontRoomsPoolMaterials.LogOnce("SP7 end " + n + " " + where, "SP7: treatment " + n + " failed in " + where + " and is left out (the room runs today's SP7): " + ex);
        if (i > 0) endFailed[i] = true;
        endFault = true;
    }

    bool EndSafe(FrontRoomsStructuresRunEnd e, string where, Action a)
    {
        try
        {
            a();
            return true;
        }
        catch (Exception ex)
        {
            EndFault(e, where, ex);
            return false;
        }
    }

    void EndBeforeReset()
    {
        var want = WantedEnd();
        if (activeEnd != null && activeEnd != want)
        {
            var old = activeEnd;
            activeEnd = null;
            EndSafe(old, "exit", () => old.Exit(room));
            if (old.Own != null) old.Own.gameObject.SetActive(false);
        }
        if (activeEnd == null && !endTouched) return;
        ReleaseHoldsNow();
        ClearFogNow();
        ClearAmbientNow();
        endMoving = endWaiting = false;
        CloseRoomPart();
        if (snapped) RestoreSnapshot();
        RestoreMarks();
        ShortRoomOff();
        endTouched = activeEnd != null;
    }

    void EndAfterReset()
    {
        var want = WantedEnd();
        if (want != null && want != activeEnd)
        {
            // Today's room is kept as it stands before any treatment touches it (the first time only, before any build).
            if (!snapped) TakeSnapshot();
            if (!EnsureBuilt(want)) return;
            if (want.Own != null) want.Own.gameObject.SetActive(true);
            activeEnd = want;
            endTouched = true;
            endTime = 0f;
            feetKnown = false;
            pushed = Vector3.zero;
            SeedShown();
            if (!EndSafe(want, "enter", () => want.Enter(room))) return;
        }
        if (activeEnd == null) return;
        endTouched = true;
        var e = activeEnd;
        if (!EndSafe(e, "reset", () => e.Reset(room))) return;
        endLines.Clear();
        if (!EndSafe(e, "lines", () => e.Lines(room, endLines))) return;
        RefreshMagnetsNow();
        ApplyMarks(e);
        PulseEaseReset();
        ShortRoomTick(0f);
        plateDirty = true;
        // The lamps as the re-armed treatment drives them, this frame already.
        Evaluate();
    }

    // Built once, the first time it becomes active: its own root under the district's, its Build in a try/catch.
    bool EnsureBuilt(FrontRoomsStructuresRunEnd e)
    {
        var i = Array.IndexOf(ends, e);
        if (i <= 0 || endFailed[i]) return false;
        if (endBuilt[i]) return true;
        endBuilt[i] = true;
        var own = new GameObject("SP7 end " + e.Number + " (" + e.GetType().Name + ")").transform;
        own.SetParent(Root, false);
        e.Bind(own);
        var t0 = Time.realtimeSinceStartup;
        var ok = EndSafe(e, "build", () => e.Build(room));
        CloseRoomPart();
        if (ok)
        {
            Debug.Log(FrontRoomsStructures.Log + "SP7 treatment " + e.Number + " built in " + ((Time.realtimeSinceStartup - t0) * 1000f).ToString("0") + " ms.");
            return true;
        }
        own.gameObject.SetActive(false);
        if (snapped) RestoreSnapshot();
        return false;
    }

    void CloseRoomPart()
    {
        if (!partOpen) return;
        partOpen = false;
        if (kit != null) kit.EndPart();
    }

    Transform BeginEndPart(FrontRoomsStructuresRunEnd end, string name)
    {
        CloseRoomPart();
        var part = kit.BeginPart(name);
        if (end != null && end.Own != null) part.SetParent(end.Own, false);
        partOpen = true;
        return part;
    }

    void EndTick(FrontRoomsStructures host, float dt, Vector3 p)
    {
        endTime += dt;
        var f = EndFrame(host, dt, p);
        var e = activeEnd;
        try { e.Tick(room, f); }
        catch (Exception ex) { EndFault(e, "tick", ex); }
    }

    // The walker as a treatment reads it: feet, eye and view in district metres, its velocity with and without the pushes,
    // its gait (stamina falls only while it sprints).
    FrontRoomsStructuresRunEndFrame EndFrame(FrontRoomsStructures host, float dt, Vector3 p)
    {
        var w = host.Walker;
        var f = new FrontRoomsStructuresRunEndFrame { dt = dt, clock = clock, time = endTime, phase = EndPhase, feet = p };
        f.eye = w.EyePosition - Site;
        var fwd = w.View != null ? w.View.transform.forward : w.transform.forward;
        f.view = (Root != null ? Root.InverseTransformDirection(fwd) : fwd).normalized;
        var push = pushed;
        pushed = Vector3.zero;
        if (feetKnown && dt > 1e-5f)
        {
            var v = (p - lastFeet) / dt;
            // A jump (R, [ ], a reset) is not a speed.
            if (v.sqrMagnitude < 400f)
            {
                f.velocity = v;
                f.own = (p - lastFeet - push) / dt;
            }
        }
        lastFeet = p;
        feetKnown = true;
        f.speed = new Vector2(f.own.x, f.own.z).magnitude;
        f.stamina01 = w.Stamina01;
        f.sprinting = f.stamina01 < lastStamina - 1e-5f || f.speed > 4.6f;
        lastStamina = f.stamina01;
        f.gait = f.sprinting ? FrontRoomsStructuresRunEndGait.Sprint
            : f.speed > 2.2f ? FrontRoomsStructuresRunEndGait.Walk
            : f.speed > .25f ? FrontRoomsStructuresRunEndGait.Slow : FrontRoomsStructuresRunEndGait.Still;
        if (walkerBody == null) walkerBody = w.GetComponent<CharacterController>();
        f.grounded = walkerBody != null && walkerBody.isGrounded;
        f.axisAngle = Vector3.Angle(f.view, Vector3.forward);
        f.yawOff = new Vector2(f.view.x, f.view.z).sqrMagnitude > 1e-8f ? Mathf.Atan2(f.view.x, f.view.z) * Mathf.Rad2Deg : 0f;
        return f;
    }

    // room.Push: the walker moved through its own CharacterController (it collides and steps); a hair down keeps it grounded.
    bool PushWalker(Vector3 delta)
    {
        var w = kit != null && kit.Host != null ? kit.Host.Walker : null;
        if (w == null || w.Frozen || w.Riding) return false;
        if (walkerBody == null) walkerBody = w.GetComponent<CharacterController>();
        if (walkerBody == null || !walkerBody.enabled) return false;
        var world = Root != null ? Root.TransformVector(delta) : delta;
        walkerBody.Move(world + Vector3.down * .002f);
        pushed += delta;
        return true;
    }

    void EndSeated(float from, float to)
    {
        var e = activeEnd;
        try { e.Seated(room, from, to); }
        catch (Exception ex) { EndFault(e, "seated", ex); }
    }

    bool EndDoorReachable()
    {
        var e = activeEnd;
        try { return e.DoorReachable(room); }
        catch (Exception ex)
        {
            EndFault(e, "door", ex);
            return false;
        }
    }

    bool EndOpensShort()
    {
        var e = activeEnd;
        if (e == null) return false;
        try { return e.OpensShort; }
        catch (Exception ex)
        {
            EndFault(e, "opens short", ex);
            return false;
        }
    }

    // The goal door's prompt under a treatment (its DoorPrompt hook last); #1's words come back as they came in.
    string EndDoorPrompt(string today, bool canOpen)
    {
        var e = activeEnd;
        if (e == null) return today;
        var atHall = Mathf.Abs(headZ - Length) < .01f;
        if (!atHall && EndOpensShort())
            today = !canOpen ? "门还没停稳（等它停下再推）" : goalOpen ? "E  ·  关门" : "E  ·  开门（推杠；场景 4 里门后是占位的小房间）";
        else if (!canOpen && atHall) today = "门还没停稳（等它停下再推）";
        try
        {
            var s = e.DoorPrompt(room, today);
            return string.IsNullOrEmpty(s) ? today : s;
        }
        catch (Exception ex)
        {
            EndFault(e, "door prompt", ex);
            return today;
        }
    }

    // room.Seat without the strike: the cells open already in the room's state (a step nobody saw).
    void SeatQuiet(float from, float to)
    {
        var c0 = Mathf.RoundToInt(from / CellLen);
        var c1 = Mathf.RoundToInt(to / CellLen);
        for (var c = Mathf.Max(0, c0); c < c1 && c < Cells; c++)
        {
            if (cellOpen[c]) continue;
            SetCell(c, true);
            revealAt[c] = float.NegativeInfinity;
        }
        RefreshMagnetsNow();
        if (blindPart != null) blindPart.gameObject.SetActive(to < Length - .01f);
        if (activeEnd != null) EndSeated(from, to);
    }

    void OpenCellFor(int c, bool strike)
    {
        if (c < 0 || c >= Cells) return;
        var was = cellOpen[c];
        SetCell(c, true);
        if (strike) revealAt[c] = phase == Phase.Armed ? float.NegativeInfinity : clock;
        else if (!was) revealAt[c] = float.NegativeInfinity;
    }

    void CloseCellFor(int c)
    {
        if (c < 0 || c >= Cells) return;
        SetCell(c, false);
        revealAt[c] = float.NegativeInfinity;
    }

    void RefreshMagnetsNow()
    {
        foreach (var pair in magnets)
            if (pair.Value != null) pair.Value.SetActive(MagnetFor(pair.Key) && cellShown[CellOf(pair.Key - .2f)]);
    }

    // ---------------------------------------------------------------- treatments: the lamps (today's drive, holds, hooks)

    sealed class StripHold
    {
        public bool on, releasing;
        public float t0, fade;
        public FrontRoomsStructuresRunEndStrip from, to;
    }

    sealed class HeadHold
    {
        public bool on, releasing;
        public float t0, fade;
        public FrontRoomsStructuresRunEndHead from, to;
    }

    void EnsureEndArrays()
    {
        if (stripHolds == null || stripHolds.Length != strips.Count)
        {
            stripHolds = new StripHold[strips.Count];
            for (var i = 0; i < stripHolds.Length; i++) stripHolds[i] = new StripHold();
            stripShown = new FrontRoomsStructuresRunEndStrip[strips.Count];
        }
        if (headHolds == null || headHolds.Length != heads.Count)
        {
            headHolds = new HeadHold[heads.Count];
            for (var i = 0; i < headHolds.Length; i++) headHolds[i] = new HeadHold();
            headShown = new FrontRoomsStructuresRunEndHead[heads.Count];
        }
    }

    void SeedShown()
    {
        EnsureEndArrays();
        for (var i = 0; i < strips.Count; i++) if (strips[i] != null) stripShown[i] = TodayStripDrive(strips[i], clock);
        for (var i = 0; i < heads.Count; i++) if (heads[i] != null) headShown[i] = TodayHeadDrive(heads[i], clock);
    }

    void ReleaseHoldsNow()
    {
        if (stripHolds != null) foreach (var h in stripHolds) h.on = h.releasing = false;
        if (headHolds != null) foreach (var h in headHolds) h.on = h.releasing = false;
    }

    void HoldStripNow(int i, FrontRoomsStructuresRunEndStrip target, float fade)
    {
        EnsureEndArrays();
        var h = stripHolds[i];
        h.from = stripShown[i];
        h.to = Normalise(strips[i], target);
        h.t0 = endTime;
        h.fade = Mathf.Max(0f, fade);
        h.on = true;
        h.releasing = false;
    }

    void ReleaseStripNow(int i, float fade)
    {
        EnsureEndArrays();
        var h = stripHolds[i];
        if (!h.on && !h.releasing) return;
        h.from = stripShown[i];
        h.t0 = endTime;
        h.fade = Mathf.Max(0f, fade);
        h.on = false;
        h.releasing = h.fade > 0f;
    }

    void HoldHeadNow(int i, FrontRoomsStructuresRunEndHead target, float fade)
    {
        EnsureEndArrays();
        var h = headHolds[i];
        h.from = headShown[i];
        h.to = Normalise(heads[i], target);
        h.t0 = endTime;
        h.fade = Mathf.Max(0f, fade);
        h.on = true;
        h.releasing = false;
    }

    void ReleaseHeadNow(int i, float fade)
    {
        EnsureEndArrays();
        var h = headHolds[i];
        if (!h.on && !h.releasing) return;
        h.from = headShown[i];
        h.t0 = endTime;
        h.fade = Mathf.Max(0f, fade);
        h.on = false;
        h.releasing = h.fade > 0f;
    }

    FrontRoomsStructuresRunEndStrip StripHoldApply(int i, FrontRoomsStructuresRunEndStrip today)
    {
        var h = stripHolds[i];
        if (!h.on && !h.releasing) return today;
        var k = h.fade <= 0f ? 1f : FrontRoomsStructuresRunEnd.Smooth((endTime - h.t0) / h.fade);
        if (h.on)
        {
            var held = FrontRoomsStructuresRunEndStrip.Lerp(h.from, h.to, k);
            held.open = today.open;
            return held;
        }
        if (k >= 1f)
        {
            h.releasing = false;
            return today;
        }
        var back = FrontRoomsStructuresRunEndStrip.Lerp(h.from, today, k);
        back.open = today.open;
        return back;
    }

    FrontRoomsStructuresRunEndHead HeadHoldApply(int i, FrontRoomsStructuresRunEndHead today)
    {
        var h = headHolds[i];
        if (!h.on && !h.releasing) return today;
        var k = h.fade <= 0f ? 1f : FrontRoomsStructuresRunEnd.Smooth((endTime - h.t0) / h.fade);
        FrontRoomsStructuresRunEndHead r;
        if (h.on) r = FrontRoomsStructuresRunEndHead.Lerp(h.from, h.to, k);
        else if (k >= 1f)
        {
            h.releasing = false;
            return today;
        }
        else r = FrontRoomsStructuresRunEndHead.Lerp(h.from, today, k);
        r.open = today.open;
        r.goal = today.goal;
        return r;
    }

    Color RedHue => rig.redTubeLinear.maxColorComponent > 1e-4f ? rig.redTubeLinear / rig.redTubeLinear.maxColorComponent : AlarmRedLinear;

    // A colour left at zero is the rig's own; open is the cell's.
    FrontRoomsStructuresRunEndStrip Normalise(StripLamp s, FrontRoomsStructuresRunEndStrip d)
    {
        if (d.whiteColour.maxColorComponent <= 0f) d.whiteColour = rig.wrapColor.linear;
        if (d.redColour.maxColorComponent <= 0f) d.redColour = RedHue;
        if (d.lensColour.maxColorComponent <= 0f) d.lensColour = rig.stripLensEmission;
        if (d.tubeColour.maxColorComponent <= 0f) d.tubeColour = rig.redTubeLinear.gamma;
        d.open = s == null || s.cell < 0 || cellOpen[s.cell];
        return d;
    }

    FrontRoomsStructuresRunEndHead Normalise(HeadLamp h, FrontRoomsStructuresRunEndHead d)
    {
        if (d.colour.maxColorComponent <= 0f) d.colour = rig.floodColor;
        if (d.lensColour.maxColorComponent <= 0f) d.lensColour = rig.headLensEmission;
        d.open = h == null || h.cell < 0 || cellOpen[h.cell];
        d.goal = h != null && h.goal;
        return d;
    }

    // Today's drive of one strip at t, as Evaluate computes it (a closed cell's strip at 0), in the struct.
    FrontRoomsStructuresRunEndStrip TodayStripDrive(StripLamp s, float t)
    {
        var d = Normalise(s, new FrontRoomsStructuresRunEndStrip());
        if (!d.open) return d;
        StripLevels(s, t, out d.white, out d.red, out d.lens, out d.tube);
        return d;
    }

    FrontRoomsStructuresRunEndHead TodayHeadDrive(HeadLamp h, float t)
    {
        var d = Normalise(h, new FrontRoomsStructuresRunEndHead());
        if (d.open) d.level = HeadLevel(h, t);
        return d;
    }

    // Evaluate under a treatment: per lamp today's drive, its hold, its hook, then the lamp lit as SetStrip lights it (with the
    // struct's colours); the pilots as today.
    void EvaluateDriven()
    {
        EnsureEndArrays();
        var t = clock;
        var e = activeEnd;
        try
        {
            for (var i = 0; i < strips.Count; i++)
            {
                var s = strips[i];
                if (s == null || s.light == null) continue;
                var d = StripHoldApply(i, TodayStripDrive(s, t));
                e.Strip(room, i, ref d);
                d = Normalise(s, d);
                stripShown[i] = d;
                SetStripDrive(s, d);
            }
            for (var i = 0; i < heads.Count; i++)
            {
                var h = heads[i];
                if (h == null || h.light == null) continue;
                var d = HeadHoldApply(i, TodayHeadDrive(h, t));
                e.Head(room, i, ref d);
                d = Normalise(h, d);
                headShown[i] = d;
                SetHeadDrive(h, d);
            }
        }
        catch (Exception ex) { EndFault(e, "lamps", ex); }
        var pilot = phase == Phase.Tripped ? 0f : 1f;
        foreach (var p in pilots)
        {
            if (p == null || p.renderer == null) continue;
            if (p.cell >= 0 && !cellOpen[p.cell]) continue;
            block.Clear();
            p.renderer.GetPropertyBlock(block, p.index);
            block.SetColor(EmissionId, rig.pilotEmission * pilot);
            p.renderer.SetPropertyBlock(block, p.index);
        }
    }

    // SetStrip with the struct's colours (the same sums: the Light white + red, the lens, the red tube's bounce or burn, the halo).
    void SetStripDrive(StripLamp s, FrontRoomsStructuresRunEndStrip d)
    {
        if (s == null || s.light == null) return;
        var white = rig.wrapIntensity * s.scale * Mathf.Max(0f, d.white);
        var red = RedLight * Mathf.Max(0f, d.red);
        var sum = white + red;
        s.light.enabled = sum > .0005f;
        if (sum > .0005f)
        {
            var c = (d.whiteColour * white + d.redColour * red) / sum;
            s.light.color = c.gamma;
            s.light.intensity = sum;
        }
        if (s.lens != null)
        {
            block.Clear();
            s.lens.GetPropertyBlock(block);
            block.SetColor(EmissionId, d.lensColour * Mathf.Max(0f, d.lens));
            s.lens.SetPropertyBlock(block);
        }
        if (s.red != null)
        {
            var bounce = rig.redTubeBounce * Mathf.Max(0f, d.lens);
            var burn = d.tubeColour * Mathf.Clamp01(d.tube);
            block.Clear();
            s.red.GetPropertyBlock(block);
            block.SetColor(EmissionId, new Color(Mathf.Max(bounce.r, burn.r), Mathf.Max(bounce.g, burn.g), Mathf.Max(bounce.b, burn.b), 1f));
            s.red.SetPropertyBlock(block);
        }
        if (s.halo != null)
        {
            var redHalo = d.red > d.lens && haloRed != null;
            var level = redHalo ? Mathf.Clamp01(d.red) : Mathf.Max(0f, d.lens);
            if (redHalo && s.halo.sharedMaterial != haloRed) s.halo.sharedMaterial = haloRed;
            else if (!redHalo && haloWhite != null && s.halo.sharedMaterial != haloWhite) s.halo.sharedMaterial = haloWhite;
            s.halo.enabled = level > .0005f;
            block.Clear();
            block.SetFloat(DensityId, level);
            s.halo.SetPropertyBlock(block);
        }
    }

    void SetHeadDrive(HeadLamp h, FrontRoomsStructuresRunEndHead d)
    {
        if (h == null || h.light == null) return;
        var v = h.baseIntensity * Mathf.Max(0f, d.level);
        h.light.intensity = v;
        h.light.enabled = v > .0005f;
        h.light.color = d.colour;
        SetLens(h.lens, h.lensIndex, d.lensColour * Mathf.Max(0f, d.level));
    }

    // ---------------------------------------------------------------- treatments: the fog override, the placard

    void SetFogOverride(Color colour, float density)
    {
        if (!fogOn)
        {
            savedFogColour = RenderSettings.fogColor;
            savedFogDensity = RenderSettings.fogDensity;
        }
        fogOn = true;
        fogColour = colour;
        fogDensity = Mathf.Max(0f, density);
        RenderSettings.fogColor = fogColour;
        RenderSettings.fogDensity = fogDensity;
    }

    void ClearFogNow()
    {
        if (!fogOn) return;
        fogOn = false;
        RenderSettings.fogColor = savedFogColour;
        RenderSettings.fogDensity = savedFogDensity;
    }

    // After the preset: the preset applied now (the host's Want after this is then a no-op) and its fog kept for the clear,
    // then the override's fog on top.
    void FogAfterPreset(FrontRoomsStructuresLook.Preset preset)
    {
        if (!fogOn && !ambientOn) return;
        var look = kit != null && kit.Host != null ? kit.Host.Look : null;
        if (look != null && look.Current != preset)
        {
            look.Want(preset);
            savedFogColour = RenderSettings.fogColor;
            savedFogDensity = RenderSettings.fogDensity;
            SaveAmbient();
        }
        if (fogOn)
        {
            RenderSettings.fogColor = fogColour;
            RenderSettings.fogDensity = fogDensity;
        }
        if (ambientOn) PutAmbient(ambientSky, ambientEquator, ambientGround);
    }

    // room.Ambient (#2's warm gap): the trilight while set, over the preset's; the preset's kept for the clear.
    void SetAmbientOverride(Color sky, Color equator, Color ground)
    {
        if (!ambientOn) SaveAmbient();
        ambientOn = true;
        ambientSky = sky;
        ambientEquator = equator;
        ambientGround = ground;
        PutAmbient(sky, equator, ground);
    }

    void ClearAmbientNow()
    {
        if (!ambientOn) return;
        ambientOn = false;
        PutAmbient(savedSky, savedEquator, savedGround);
        RenderSettings.ambientLight = savedAmbientLight;
    }

    void SaveAmbient()
    {
        savedSky = RenderSettings.ambientSkyColor;
        savedEquator = RenderSettings.ambientEquatorColor;
        savedGround = RenderSettings.ambientGroundColor;
        savedAmbientLight = RenderSettings.ambientLight;
    }

    static void PutAmbient(Color sky, Color equator, Color ground)
    {
        if (RenderSettings.ambientSkyColor != sky) RenderSettings.ambientSkyColor = sky;
        if (RenderSettings.ambientEquatorColor != equator) RenderSettings.ambientEquatorColor = equator;
        if (RenderSettings.ambientGroundColor != ground) RenderSettings.ambientGroundColor = ground;
        if (RenderSettings.ambientLight != sky) RenderSettings.ambientLight = sky;
    }

    // ---------------------------------------------------------------- treatments: the requests (the curves, the drive at a place)

    // room.WhiteDrop / Afterglow / RedRise / PulseAt / Restrike / RestrikeAt are the frame's own (above); a lamp's place along
    // the corridor as a continuous cell index from strip 0 (strip c at 1.8 + 3c has index c).
    static float CellIndexAt(float z) => (z - 1.8f) / CellLen;

    float RestrikeStart(float z)
    {
        var cell = Mathf.Floor(Mathf.Max(0f, z) / Mathf.Max(.1f, rig.resetCellLength));
        return (reduce ? 0f : rig.resetRiseStart) + cell * rig.resetCellSeconds;
    }

    // StripLevels for a strip hung at z (room.TodayStripAt): its own cell's state (the reveal), every place-dependent time from
    // z, continuously; at the strip's own z the same numbers as StripLevels (which #1 runs, unchanged).
    void StripLevelsAt(StripLamp s, float t, float z, out float w, out float r, out float lens, out float rt)
    {
        w = 1f;
        r = 0f;
        lens = 1f;
        rt = 0f;
        var c = s.cell;
        var ci = CellIndexAt(z);
        var late = c >= 0 && !float.IsNegativeInfinity(revealAt[c]);
        var tr = late ? t - revealAt[c] : 0f;
        if (phase == Phase.Armed)
        {
            w = s.stutter ? Stutter() : 1f;
            lens = w;
            return;
        }
        if (phase == Phase.Resetting)
        {
            w = Restrike(t, z);
            if (late) w *= Smooth(tr / .4f);
            lens = w;
            return;
        }
        switch (Look)
        {
            case 0:
                w = late ? 0f : WhiteDrop(t);
                lens = late ? 0f : Mathf.Lerp(Afterglow(t), 1f, w);
                rt = late ? Strike(t - Mathf.Max(revealAt[c], rig.headDelay + rig.headStagger * z)) : RigHeadRise(t, z);
                return;
            case 1:
            case 2:
            {
                var t0 = .15f * ci;
                if (late)
                {
                    w = 0f;
                    r = Strike(t - Mathf.Max(revealAt[c], .35f + t0));
                    lens = 0f;
                }
                else if (reduce)
                {
                    var k = Smooth((t - t0) / rig.reducedEnvelope);
                    w = 1f - k;
                    r = k;
                    lens = w;
                }
                else
                {
                    w = t < t0 ? 1f : WhiteDrop(t - t0);
                    r = RedRise(t - .35f - t0);
                    lens = t < t0 ? 1f : Mathf.Lerp(Afterglow(t - t0), 1f, w);
                }
                if (Look == 2 && !reduce) r *= PulseEased(t, t0);
                rt = r;
                return;
            }
            default:
            {
                var tc = .25f + z / 3.2f;
                var k = Smooth((t - tc) / (reduce ? rig.reducedEnvelope : rig.resetRise));
                w = 1f - k;
                r = k;
                if (late) { var st = Strike(tr); w *= st; r *= st; }
                lens = w;
                rt = r;
                return;
            }
        }
    }

    FrontRoomsStructuresRunEndStrip TodayStripDriveAt(StripLamp s, float t, float z)
    {
        if (Mathf.Abs(z - s.z) < 1e-4f) return TodayStripDrive(s, t);
        var d = Normalise(s, new FrontRoomsStructuresRunEndStrip());
        if (!d.open) return d;
        StripLevelsAt(s, t, z, out d.white, out d.red, out d.lens, out d.tube);
        return d;
    }

    FrontRoomsStructuresRunEndHead TodayHeadDriveAt(HeadLamp h, float t, float z)
    {
        var d = Normalise(h, new FrontRoomsStructuresRunEndHead());
        if (!d.open) return d;
        if (probeHead == null) probeHead = new HeadLamp();
        probeHead.z = z;
        probeHead.cell = h.cell;
        probeHead.goal = h.goal;
        d.level = HeadLevel(probeHead, t);
        return d;
    }

    // room.TrippedStrip: the look's own tripped strip once its front has passed (the cell open or not).
    FrontRoomsStructuresRunEndStrip TrippedStripDrive(StripLamp s, float t)
    {
        var d = Normalise(s, new FrontRoomsStructuresRunEndStrip());
        switch (Look)
        {
            case 0:
                d.tube = 1f;
                break;
            case 1:
            case 2:
                d.red = Look == 2 && !reduce ? Pulse(t, Mathf.Max(0, s.cell)) : 1f;
                d.tube = d.red;
                break;
            default:
                d.red = d.tube = 1f;
                break;
        }
        return d;
    }

    // room.TrippedSince: when today's drive of the strip stands at its tripped state (StripLevels' times, per look).
    float TrippedSinceFor(StripLamp s)
    {
        if (phase != Phase.Tripped) return float.PositiveInfinity;
        var c = s.cell;
        var late = c >= 0 && !float.IsNegativeInfinity(revealAt[c]);
        var env = reduce ? rig.reducedEnvelope : rig.resetRise;
        float done;
        switch (Look)
        {
            case 0:
                done = reduce ? rig.reducedHeadDelay + rig.reducedEnvelope : Mathf.Max(rig.mainsDrop, rig.headDelay + rig.headStagger * s.z + rig.headRise);
                if (late) done = Mathf.Max(done, Mathf.Max(revealAt[c], rig.headDelay + rig.headStagger * s.z) + env);
                return done;
            case 1:
            case 2:
            {
                var t0 = .15f * Mathf.Max(0, c);
                done = reduce ? t0 + rig.reducedEnvelope : .35f + t0 + rig.resetRise;
                if (late) done = Mathf.Max(done, Mathf.Max(revealAt[c], .35f + t0) + env);
                return done;
            }
            default:
                done = .25f + s.z / 3.2f + env;
                if (late) done = Mathf.Max(done, revealAt[c] + env);
                return done;
        }
    }

    // ---------------------------------------------------------------- treatments: B's pulse eased (Pulse under a treatment)

    // When the gate (the end moving or waiting) or the schedule's start (the final seat) changes, the pulse level eases from
    // where it stood over B's own 0.4 s rise instead of snapping (a dipping strip never jumps back to full).
    const float PulseEase = .4f;

    bool PulseGated => recedePending || recedeRunning || endMoving || endWaiting;
    float PulseFirst => finalSeatTime >= 0f ? Mathf.Max(3f, finalSeatTime + 1f) : 3f;

    // At every trip, re-strike, re-arm and reset (the clock starts again): the ease state as the room stands, nothing easing.
    void PulseEaseReset()
    {
        pulseGateFrom = pulseGateTarget = PulseGated ? 1f : 0f;
        pulseGateT0 = float.NegativeInfinity;
        pulseFirstNow = pulseFirstOld = PulseFirst;
        pulseFirstT0 = float.NegativeInfinity;
    }

    float PulseGateLevel(float t)
    {
        var u = (t - pulseGateT0) / PulseEase;
        return u >= 1f ? pulseGateTarget : Mathf.Lerp(pulseGateFrom, pulseGateTarget, Smooth(u));
    }

    // B's schedule alone (Pulse's loop): 12 pulses from first, a lamp delay seconds after the entrance's.
    static float PulseLevel(float t, float delay, float first)
    {
        var level = 1f;
        for (var k = 0; k < 12; k++)
        {
            var s = t - (first + 2.5f * k + delay);
            if (s < 0f) break;
            if (s < .06f) level = Mathf.Min(level, 1f - .65f * (s / .06f));
            else if (s < .16f) level = Mathf.Min(level, .35f);
            else if (s < .56f) level = Mathf.Min(level, .35f + .65f * Smooth((s - .16f) / .4f));
        }
        return level;
    }

    float PulseEased(float t, float delay)
    {
        var gate = PulseGated ? 1f : 0f;
        if (gate != pulseGateTarget)
        {
            pulseGateFrom = PulseGateLevel(t);
            pulseGateTarget = gate;
            pulseGateT0 = t;
        }
        var first = PulseFirst;
        if (first != pulseFirstNow)
        {
            pulseFirstOld = pulseFirstNow;
            pulseFirstNow = first;
            pulseFirstT0 = t;
        }
        var level = PulseLevel(t, delay, pulseFirstNow);
        var u = (t - pulseFirstT0) / PulseEase;
        if (u < 1f) level = Mathf.Lerp(PulseLevel(t, delay, pulseFirstOld), level, Smooth(u));
        return Mathf.Lerp(level, 1f, PulseGateLevel(t));
    }

    // ---------------------------------------------------------------- treatments: the marks' words

    void ApplyMarks(FrontRoomsStructuresRunEnd e)
    {
        for (var i = 0; i < markTexts.Count && i < MarkText.Length; i++)
        {
            var tm = markTexts[i];
            if (tm == null) continue;
            var s = MarkText[i];
            try { s = e.Mark(room, i, MarkText[i]); }
            catch (Exception ex)
            {
                EndFault(e, "mark", ex);
                s = MarkText[i];
            }
            if (string.IsNullOrEmpty(s)) s = MarkText[i];
            if (tm.text != s)
            {
                tm.text = s;
                marksTouched = true;
            }
        }
    }

    void RestoreMarks()
    {
        if (!marksTouched) return;
        marksTouched = false;
        for (var i = 0; i < markTexts.Count && i < MarkText.Length; i++)
            if (markTexts[i] != null && markTexts[i].text != MarkText[i]) markTexts[i].text = MarkText[i];
    }

    // ---------------------------------------------------------------- treatments: the stand-in room behind a short-opening door

    // 72 3.4: "a player who never looks back meets a plain room"; C38: the door is never locked. Scene 4 has its hall only behind
    // 42 m, so under a treatment that OpensShort a lit STAND-IN room waits behind the goal door while the head stands short of
    // it: 2.94 m deep in the corridor's section behind the head wall (Run! B's block in its two tones and cove, the corridor
    // floor's own surface on a plain slab, a lid at 2.45 m under the services that run on through the hidden cells), lit by one
    // R-B strip while the door is open (up over 0.25 s; F: 0.5 s); it rides the head, its back wall solid.
    const float ShortDepth = 3.02f, ShortHalf = 1.38f, ShortLid = 2.45f;
    // Render stage 11-04 (renders/scene4/run_ends1, #5 film F1 after E): from the doorway of a tripped room the strip alone lit
    // the floor and the walls' feet, the back wall's upper half and the lid stayed dark (the tripped ambient is the alarm's dim
    // red), so the stand-in read as a dark closet. A soft fill in the room's middle stands in for the strip's bounce (in the
    // armed room the RunB ambient does it): no shadows, its 2.4 m range ends in the room (the corridor's floor and walls
    // beyond the head wall are 2.1-2.2 m from it, where the range's own falloff leaves under 3 % of its light).
    const float ShortFillIntensity = .9f, ShortFillRange = 2.4f;

    void ShortRoomTick(float dt)
    {
        var want = activeEnd != null && headZ < Length - .01f && EndOpensShort();
        if (want && shortRoom == null && !shortFailed) BuildShortRoom();
        if (shortRoom == null) return;
        if (shortRoom.gameObject.activeSelf != want) shortRoom.gameObject.SetActive(want);
        var target = want && goalOpen ? 1f : 0f;
        var rate = dt / (reduce ? rig.reducedEnvelope : .25f);
        var level = dt <= 0f ? target : Mathf.MoveTowards(shortLevel, target, rate);
        if (shortLamp != null && (level != shortLevel || dt <= 0f)) SetStrip(shortLamp, level, 0f, level, 0f);
        SetShortFill(level);
        shortLevel = level;
    }

    void SetShortFill(float level)
    {
        if (shortFill == null) return;
        var v = ShortFillIntensity * level;
        if (shortFill.intensity != v) shortFill.intensity = v;
        var on = v > .001f;
        if (shortFill.enabled != on) shortFill.enabled = on;
    }

    void ShortRoomOff()
    {
        if (shortRoom == null) return;
        shortRoom.gameObject.SetActive(false);
        shortLevel = 0f;
        if (shortLamp != null) SetStrip(shortLamp, 0f, 0f, 0f, 0f);
        SetShortFill(0f);
    }

    // A point (district metres) inside the stand-in room while it shows.
    bool InShortRoom(Vector3 p)
    {
        if (shortRoom == null || !shortRoom.gameObject.activeInHierarchy) return false;
        var z = p.z - headZ;
        return z > FrontRoomsStructuresRunEndRoom.HeadWallHalf && z < ShortDepth && Mathf.Abs(p.x) < HalfW && p.y > -.5f && p.y < Ceil;
    }

    void BuildShortRoom()
    {
        if (kit == null || head == null) { shortFailed = true; return; }
        try
        {
            CloseRoomPart();
            var part = kit.BeginPart("STAND-IN SP7 room behind the goal door (a treatment that opens short of 42 m)");
            partOpen = true;
            kit.Shell(ShortLid, 0f);
            const float z0 = FrontRoomsStructuresRunEndRoom.HeadWallHalf + .002f, z1 = ShortDepth, x = ShortHalf, lid = ShortLid;
            foreach (var s in new[] { -1f, 1f })
            {
                // The side walls 4 cm inside the corridor's own (they cover its EMT and the remote heads' mounts), the cove on them.
                var xa = Mathf.Min(s * x, s * (x + .03f));
                var xb = Mathf.Max(s * x, s * (x + .03f));
                kit.Box(xa, 0f, z0, xb, 1.2f, z1, lower, false);
                kit.Box(xa, 1.2f, z0, xb, lid, z1, upper, false);
                var xc = Mathf.Min(s * (x - .0035f), s * x);
                var xd = Mathf.Max(s * (x - .0035f), s * x);
                kit.Box(xc, 0f, z0, xd, .102f, z1, cove, false);
            }
            // The back wall (solid: the walker stops here), its cove.
            kit.Box(-x - .03f, 0f, z1, x + .03f, 1.2f, z1 + .1f, lower);
            kit.Box(-x - .03f, 1.2f, z1, x + .03f, lid, z1 + .1f, upper);
            kit.Box(-x, 0f, z1 - .0035f, x, .102f, z1, cove, false);
            // The floor (a plain slab 2 mm proud, render only: the corridor's floor carries the walker) and the lid.
            kit.Box(-x, 0f, z0, x, .002f, z1, floorMat, false);
            kit.Box(-x - .03f, lid, z0, x + .03f, lid + .04f, z1 + .1f, soffitMat, false);
            kit.EndPart();
            partOpen = false;
            part.SetParent(head, false);
            part.localPosition = Vector3.zero;
            shortRoom = part;
            // One R-B strip on the lid's centre line, the corridor's white (off the alarm circuit: no red tube), its cone kept short.
            shortLamp = Strip(part, new Vector3(0f, lid, 1.6f), 1f, 3.2f, false, "STAND-IN room strip");
            if (shortLamp != null && shortLamp.red != null) { shortLamp.red.enabled = false; shortLamp.red = null; }
            if (shortLamp != null && shortLamp.light != null)
            {
                shortLamp.light.spotAngle = Mathf.Min(shortLamp.light.spotAngle, 120f);
                shortLamp.light.innerSpotAngle = Mathf.Min(shortLamp.light.innerSpotAngle, 80f);
            }
            if (shortLamp != null) SetStrip(shortLamp, 0f, 0f, 0f, 0f);
            // The fill (see ShortFillIntensity): a point light at the room's middle, the strip's colour, off until the door opens.
            var fillGo = new GameObject("STAND-IN room fill (its strip's bounce)");
            fillGo.transform.SetParent(part, false);
            fillGo.transform.localPosition = new Vector3(0f, 1.3f, 1.62f);
            shortFill = fillGo.AddComponent<Light>();
            FrontRoomsMapWorld.ShippedShadowFilter(shortFill);
            shortFill.type = LightType.Point;
            shortFill.color = rig.wrapColor;
            shortFill.range = ShortFillRange;
            shortFill.shadows = LightShadows.None;
            shortFill.intensity = 0f;
            shortFill.enabled = false;
            shortLevel = 0f;
            part.gameObject.SetActive(false);
            Debug.Log(FrontRoomsStructures.Log + "SP7: the stand-in room behind the goal door built (a treatment that opens short).");
        }
        catch (Exception e)
        {
            CloseRoomPart();
            shortFailed = true;
            shortRoom = null;
            shortLamp = null;
            shortFill = null;
            FrontRoomsPoolMaterials.LogOnce("SP7 short room", "SP7: the stand-in room behind the goal door failed (the door stays shut short of 42 m): " + e.Message);
        }
    }

    // ---------------------------------------------------------------- treatments: a piece of the dress cut to a box (room.CutPiece)

    struct CutVert
    {
        public Vector3 p, d, n;
        public Vector2 uv;
    }

    readonly List<CutVert> cutPoly = new List<CutVert>(), cutTmp = new List<CutVert>();

    static CutVert CutLerp(CutVert a, CutVert b, float k) => new CutVert
    {
        p = Vector3.Lerp(a.p, b.p, k), d = Vector3.Lerp(a.d, b.d, k), n = Vector3.Lerp(a.n, b.n, k), uv = Vector2.Lerp(a.uv, b.uv, k),
    };

    // One plane of the box (Sutherland-Hodgman): keep d[axis] >= v (low) or <= v (high).
    void CutPlane(int axis, float v, bool low)
    {
        cutTmp.Clear();
        var n = cutPoly.Count;
        for (var i = 0; i < n; i++)
        {
            var a = cutPoly[i];
            var b = cutPoly[(i + 1) % n];
            var da = low ? a.d[axis] - v : v - a.d[axis];
            var db = low ? b.d[axis] - v : v - b.d[axis];
            if (da >= 0f) cutTmp.Add(a);
            if ((da >= 0f) != (db >= 0f)) cutTmp.Add(CutLerp(a, b, da / (da - db)));
        }
        cutPoly.Clear();
        cutPoly.AddRange(cutTmp);
    }

    // FrontRooms/Surface's world planar frame for a face of normal n (district = world axes: the root has no rotation), as #2's
    // boxes make it: +x (-z, y), -x (z, y), +y (x, -z), -y (x, z), +z (x, y), -z (-x, y).
    static Vector2 PlanarUv(Vector3 w, Vector3 n)
    {
        var ax = Mathf.Abs(n.x);
        var ay = Mathf.Abs(n.y);
        var az = Mathf.Abs(n.z);
        if (ax >= ay && ax >= az) return n.x >= 0f ? new Vector2(-w.z, w.y) : new Vector2(w.z, w.y);
        if (ay >= az) return n.y >= 0f ? new Vector2(w.x, -w.z) : new Vector2(w.x, w.z);
        return n.z >= 0f ? new Vector2(w.x, w.y) : new Vector2(-w.x, w.y);
    }

    Renderer CutPieceNow(FrontRoomsStructuresRunEnd end, Renderer piece, Bounds box, Transform parent, Material material, bool planarUv)
    {
        if (piece == null) return null;
        var mf = piece.GetComponent<MeshFilter>();
        var src = mf != null ? mf.sharedMesh : null;
        if (src == null || !src.isReadable) return null;
        if (parent == null) parent = end != null && end.Own != null ? end.Own : Root;
        var toDistrict = Root.worldToLocalMatrix * piece.transform.localToWorldMatrix;
        var toParent = parent.worldToLocalMatrix * piece.transform.localToWorldMatrix;
        var vs = src.vertices;
        var ns = src.normals;
        var uvs = src.uv;
        var hasN = ns != null && ns.Length == vs.Length;
        var hasUv = uvs != null && uvs.Length == vs.Length;
        var outV = new List<Vector3>();
        var outN = new List<Vector3>();
        var outUv = new List<Vector2>();
        var subs = new List<List<int>>();
        var lo = box.min;
        var hi = box.max;
        for (var sm = 0; sm < src.subMeshCount; sm++)
        {
            var list = new List<int>();
            subs.Add(list);
            if (src.GetTopology(sm) != MeshTopology.Triangles) continue;
            var tri = src.GetTriangles(sm);
            for (var k = 0; k + 2 < tri.Length; k += 3)
            {
                cutPoly.Clear();
                for (var j = 0; j < 3; j++)
                {
                    var vi = tri[k + j];
                    cutPoly.Add(new CutVert { p = vs[vi], d = toDistrict.MultiplyPoint3x4(vs[vi]), n = hasN ? ns[vi] : Vector3.zero, uv = hasUv ? uvs[vi] : Vector2.zero });
                }
                var face = Vector3.Cross(cutPoly[1].d - cutPoly[0].d, cutPoly[2].d - cutPoly[0].d);
                for (var axis = 0; axis < 3 && cutPoly.Count >= 3; axis++)
                {
                    CutPlane(axis, lo[axis], true);
                    if (cutPoly.Count >= 3) CutPlane(axis, hi[axis], false);
                }
                if (cutPoly.Count < 3) continue;
                var b0 = outV.Count;
                foreach (var cv in cutPoly)
                {
                    outV.Add(toParent.MultiplyPoint3x4(cv.p));
                    var nn = hasN ? toParent.MultiplyVector(cv.n) : toParent.MultiplyVector(Vector3.zero);
                    outN.Add(nn.sqrMagnitude > 1e-12f ? nn.normalized : Vector3.up);
                    outUv.Add(planarUv ? PlanarUv(cv.d + Site, face) : cv.uv);
                }
                for (var j = 1; j + 1 < cutPoly.Count; j++)
                {
                    list.Add(b0);
                    list.Add(b0 + j);
                    list.Add(b0 + j + 1);
                }
            }
        }
        if (outV.Count == 0) return null;
        var mesh = new Mesh { name = "SP7 cut " + piece.name, hideFlags = HideFlags.DontSave };
        if (outV.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
        mesh.SetVertices(outV);
        mesh.SetNormals(outN);
        mesh.SetUVs(0, outUv);
        mesh.subMeshCount = subs.Count;
        for (var sm = 0; sm < subs.Count; sm++) mesh.SetTriangles(subs[sm], sm);
        if (!hasN) mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        Meshes.Add(mesh);
        var go = new GameObject("cut " + piece.name);
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>();
        if (material != null)
        {
            var mats = new Material[subs.Count];
            for (var i = 0; i < mats.Length; i++) mats[i] = material;
            r.sharedMaterials = mats;
        }
        else r.sharedMaterials = piece.sharedMaterials;
        r.shadowCastingMode = piece.shadowCastingMode;
        r.receiveShadows = piece.receiveShadows;
        return r;
    }

    // ---------------------------------------------------------------- treatments: a walker inside what the restore put back

    // The walker's capsule (2 cm thinner, so a walker merely touching a face is not inside) against the given colliders.
    bool WalkerInside(List<Collider> list)
    {
        var w = kit != null && kit.Host != null ? kit.Host.Walker : null;
        if (w == null || list.Count == 0) return false;
        if (walkerBody == null) walkerBody = w.GetComponent<CharacterController>();
        var cc = walkerBody;
        if (cc == null || !cc.enabled) return false;
        Physics.SyncTransforms();
        var tr = cc.transform;
        var scale = tr.lossyScale;
        var radius = Mathf.Max(.05f, cc.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z)) - .02f);
        var half = Mathf.Max(0f, cc.height * Mathf.Abs(scale.y) * .5f - radius);
        var centre = tr.TransformPoint(cc.center);
        var up = tr.up;
        var n = Physics.OverlapCapsuleNonAlloc(centre + up * half, centre - up * half, radius, overlapBuffer, ~0, QueryTriggerInteraction.Ignore);
        for (var i = 0; i < n; i++)
            if (overlapBuffer[i] != null && overlapBuffer[i] != cc && list.Contains(overlapBuffer[i])) return true;
        return false;
    }

    // The treatments' placard (X): on the vestibule's west wall facing east, across from the room's sign; rebuilt on X so it
    // names the current one (one line each on what to try, from each treatment's TryLine). At z -4.062 (was -4.0) the arrival
    // eye (0, 1.62, -2) is 2.500 m from its face centre (placard spec v2.4): the body is laid out to a panel 1.310 m tall with
    // any of the five named (2.449 m at -4.0); only the "error" note of #2 or #5 adds a line (1.355 m: 2.502 m).
    void BuildEndSign()
    {
        if (kit == null) return;
        if (endSign != null)
        {
            endSign.SetActive(false);
            UnityEngine.Object.Destroy(endSign);
            endSign = null;
        }
        try
        {
            endSign = kit.Sign(new Vector3(-HalfW + .03f, 1.5f, -4.062f), new Vector3(1.2f, 1.5f, -4.062f), "SP7 · 后退的五种做法（按 X 换）", EndSignBody(), 0f);
            if (endSign != null && vestibuleSHSet) ApplyVestibuleSH(endSign);
        }
        catch (Exception e) { FrontRoomsPoolMaterials.LogOnce("SP7 end sign", "SP7: the treatments' placard failed: " + e.Message); }
    }

    string EndSignBody()
    {
        var cur = CurrentEnd;
        var sb = new StringBuilder();
        sb.Append("72 号文档给你挑的五种做法，都在这里做出来了，请每一种都走一遍。按 X 换下一种，Shift+X 换回上一种；一换房间就复位，门回到原位。五种都只在会退的长度里起作用（V：会退 9→12 格、9→14 格），普通长度和今天一样。现在是：")
            .Append(cur.Number).Append(' ').Append(cur.Name).Append(endFailed[treatment] ? "（出错了，跑的是今天的）" : "").Append("。\n");
        sb.Append("走过后告诉我们（每一种怎么试）：");
        for (var i = 0; i < ends.Length; i++)
        {
            var e = ends[i];
            if (i > 0) sb.Append(' ');
            sb.Append(e.Number).Append(' ').Append(e.Name);
            if (e.TryLine.Length > 0) sb.Append('：').Append(e.TryLine);
        }
        sb.Append('\n');
        sb.Append("灰盒占位：第 2 到 5 种新加的模型、动画和灯光（毛坯、工作灯、传送带这些）是简单程序做的，正式效果等游戏视觉做；第 4、5 种的门不到 42 米也推得开，门后是一间亮着的占位小房间（场景 4 的大厅只在 42 米）；没有声音，没有会动的 Relay。\n");
        sb.Append("要你决定：SP7-2 五种都走过以后选哪一种？回一个编号，比如\"2\"；也可以组合，比如\"2，加上 3 的灯光\"。");
        return sb.ToString();
    }

    // Level 0's trilight on a vestibule thing made after the build (the placard rebuilt on X), as VestibuleAmbient gives it.
    void ApplyVestibuleSH(GameObject go)
    {
        if (go == null || !vestibuleSHSet) return;
        var mpb = new MaterialPropertyBlock();
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            if (r == null) continue;
            r.lightProbeUsage = LightProbeUsage.CustomProvided;
            r.GetPropertyBlock(mpb);
            mpb.CopySHCoefficientArraysFrom(new[] { vestibuleSH });
            r.SetPropertyBlock(mpb);
            mpb.Clear();
        }
    }

    // ---------------------------------------------------------------- treatments: the room as it stood before any ran

    sealed class TransformSnap
    {
        readonly Transform t, parent;
        readonly Vector3 position, scale;
        readonly Quaternion rotation;
        readonly bool active;

        public TransformSnap(Transform t)
        {
            this.t = t;
            parent = t.parent;
            position = t.localPosition;
            rotation = t.localRotation;
            scale = t.localScale;
            active = t.gameObject.activeSelf;
        }

        public void Parent()
        {
            if (t != null && parent != null && t.parent != parent) t.SetParent(parent, false);
        }

        public void Pose()
        {
            if (t == null) return;
            t.localPosition = position;
            t.localRotation = rotation;
            t.localScale = scale;
            if (t.gameObject.activeSelf != active) t.gameObject.SetActive(active);
        }

        /// <summary>It stands elsewhere than as built (parent, pose or active state): its colliders come back somewhere else.</summary>
        public bool Moved => t != null && (t.parent != parent || t.localPosition != position || t.localRotation != rotation || t.localScale != scale || t.gameObject.activeSelf != active);

        public void Colliders(List<Collider> into)
        {
            if (t == null) return;
            foreach (var c in t.GetComponentsInChildren<Collider>(true)) if (c != null && !into.Contains(c)) into.Add(c);
        }
    }

    sealed class ColliderSnap
    {
        readonly Collider c;
        readonly bool enabled;

        public ColliderSnap(Collider c)
        {
            this.c = c;
            enabled = c.enabled;
        }

        public Collider Collider => c;
        public bool Changed => c != null && c.enabled != enabled;

        public void Restore()
        {
            if (c != null && c.enabled != enabled) c.enabled = enabled;
        }
    }

    sealed class RendererSnap
    {
        readonly Renderer r;
        readonly bool enabled;
        readonly Material[] materials;

        public RendererSnap(Renderer r)
        {
            this.r = r;
            enabled = r.enabled;
            materials = r.sharedMaterials;
        }

        public void Restore()
        {
            if (r == null) return;
            if (r.enabled != enabled) r.enabled = enabled;
            var now = r.sharedMaterials;
            var same = now.Length == materials.Length;
            for (var i = 0; same && i < now.Length; i++) same = now[i] == materials[i];
            if (!same) r.sharedMaterials = materials;
        }
    }

    sealed class LightSnap
    {
        readonly Light l;
        readonly LightType type;
        readonly Color colour;
        readonly float intensity, range, spot, inner;
        readonly Texture cookie;
        readonly bool enabled;

        public LightSnap(Light l)
        {
            this.l = l;
            type = l.type;
            colour = l.color;
            intensity = l.intensity;
            range = l.range;
            spot = l.spotAngle;
            inner = l.innerSpotAngle;
            cookie = l.cookie;
            enabled = l.enabled;
        }

        public void Restore()
        {
            if (l == null) return;
            if (l.type != type) l.type = type;
            l.color = colour;
            l.intensity = intensity;
            l.range = range;
            if (type == LightType.Spot)
            {
                l.spotAngle = spot;
                l.innerSpotAngle = inner;
            }
            if (l.cookie != cookie) l.cookie = cookie;
            l.enabled = enabled;
        }
    }

    // The frame's shared objects (the head, the dress, the fire-door frames, the track, the blind panel, bands, magnets, the
    // lamps and units, the T2 blocks' root) with every child: transforms and parents, active states, renderers' enabled
    // states and materials, lights' settings.
    void TakeSnapshot()
    {
        snapped = true;
        transformSnaps.Clear();
        rendererSnaps.Clear();
        lightSnaps.Clear();
        colliderSnaps.Clear();
        var roots = new List<Transform>();
        if (head != null) roots.Add(head);
        if (dressRoot != null) roots.Add(dressRoot);
        foreach (var f in fireFrames) if (f != null) roots.Add(f);
        if (trackPart != null) roots.Add(trackPart);
        if (blindPart != null) roots.Add(blindPart);
        foreach (var b in bandParts.Values) if (b != null) roots.Add(b.transform);
        foreach (var m in magnets.Values) if (m != null) roots.Add(m.transform);
        if (strips.Count > 0 && strips[0] != null && strips[0].root != null && strips[0].root.transform.parent != null) roots.Add(strips[0].root.transform.parent);
        for (var c = 0; c < Cells; c++) foreach (var go in cellObjects[c]) if (go != null) roots.Add(go.transform);
        foreach (var r in dressRenderers) if (r != null) roots.Add(r.transform);
        var seenT = new HashSet<Transform>();
        var seenR = new HashSet<Renderer>();
        var seenL = new HashSet<Light>();
        var seenC = new HashSet<Collider>();
        if (blocksRoot != null && seenT.Add(blocksRoot)) transformSnaps.Add(new TransformSnap(blocksRoot));
        foreach (var root in roots)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t != null && seenT.Add(t)) transformSnaps.Add(new TransformSnap(t));
            foreach (var r in root.GetComponentsInChildren<Renderer>(true)) if (r != null && seenR.Add(r)) rendererSnaps.Add(new RendererSnap(r));
            foreach (var l in root.GetComponentsInChildren<Light>(true)) if (l != null && seenL.Add(l)) lightSnaps.Add(new LightSnap(l));
            foreach (var c in root.GetComponentsInChildren<Collider>(true)) if (c != null && seenC.Add(c)) colliderSnaps.Add(new ColliderSnap(c));
        }
    }

    void RestoreSnapshot()
    {
        // What comes back elsewhere (a moved or re-shown part, a collider switched on again): tested against the walker once it
        // stands, so a reset never puts a solid round the walker (ResetRoom then jumps it home).
        restoredColliders.Clear();
        foreach (var s in transformSnaps) if (s.Moved) s.Colliders(restoredColliders);
        foreach (var s in colliderSnaps) if (s.Changed && !restoredColliders.Contains(s.Collider)) restoredColliders.Add(s.Collider);
        foreach (var s in transformSnaps) s.Parent();
        foreach (var s in transformSnaps) s.Pose();
        foreach (var s in rendererSnaps) s.Restore();
        foreach (var s in lightSnaps) s.Restore();
        foreach (var s in colliderSnaps) s.Restore();
        if (restoredColliders.Count > 0 && WalkerInside(restoredColliders)) walkerBlocked = true;
        if (liteTouched && goalLite != null)
        {
            liteTouched = false;
            block.Clear();
            goalLite.SetPropertyBlock(block);
        }
        if (hallTouched)
        {
            hallTouched = false;
            foreach (var s in hallStrips) SetStrip(s, 1f, 0f, 1f, 0f);
        }
    }

    // ---------------------------------------------------------------- the room API (FrontRoomsStructuresRunEndRoom) over SP7

    sealed class RoomLink : FrontRoomsStructuresRunEndRoom
    {
        readonly FrontRoomsStructuresRunRecede d;

        public RoomLink(FrontRoomsStructuresRunRecede district) { d = district; }

        public override FrontRoomsStructures Host => d.kit != null ? d.kit.Host : null;
        public override FrontRoomsStructuresDistrict District => d;
        public override Transform Root => d.Root;
        public override Vector3 Site => d.Site;
        public override FrontRoomsStructuresKit Kit => d.kit;
        public override RunRigLayout Rig => d.rig;

        public override Material Lower => d.lower;
        public override Material Upper => d.upper;
        public override Material FloorMat => d.floorMat;
        public override Material Soffit => d.soffitMat;
        public override Material Cove => d.cove;
        public override Material FrameSteel => d.frameMat;
        public override Material LeafMat => d.leafMat;
        public override Material Rubber => d.rubber;
        public override Material RedTubeStandIn => d.redTube;
        public override Material HaloWhite => d.haloWhite;
        public override Material HaloRed => d.haloRed;

        public override void Own(Material m)
        {
            if (m != null && !d.owned.Contains(m)) d.owned.Add(m);
        }

        public override void Own(Mesh m)
        {
            if (m != null) d.Meshes.Add(m);
        }

        public override void Own(Texture t)
        {
            if (t != null && !d.ownedTextures.Contains(t)) d.ownedTextures.Add(t);
        }

        public override Vector3 Feet => Walker != null ? Local(Walker.transform.position) : Vector3.zero;

        public override int Variant => d.variant;
        public override bool Receding => d.Receding;
        public override float RestLine => d.Rest;
        public override float FinalLine => d.Final;
        public override int Look => d.Look;
        public override bool Tube => d.tube;
        public override bool Reduce => d.reduce;
        public override bool BodyT2 => d.bodyT2;
        public override float RedLight => d.RedLight;

        public override FrontRoomsStructuresRunEndPhase Phase => d.EndPhase;
        public override float Clock => d.clock;
        public override bool Receded => d.receded;
        public override float FinalSeatTime => d.finalSeatTime;

        public override bool HeadMoving
        {
            get => d.recedeRunning || d.endMoving;
            set
            {
                d.endMoving = value;
                d.plateDirty = true;
            }
        }

        public override bool Waiting
        {
            get => d.recedePending || d.endWaiting;
            set
            {
                d.endWaiting = value;
                d.plateDirty = true;
            }
        }

        public override void Trip() => d.Trip();
        public override void StartReset() => d.StartReset();

        public override void Done()
        {
            d.endMoving = d.endWaiting = false;
            d.Finished();
        }

        public override void Flash(string message)
        {
            var h = Host;
            if (h != null && !string.IsNullOrEmpty(message)) h.Flash(message);
        }

        public override void Dirty() => d.plateDirty = true;

        public override FrontRoomsStructuresWalker Walker => Host != null ? Host.Walker : null;
        public override Camera View => Walker != null ? Walker.View : null;
        public override bool Push(Vector3 delta) => d.PushWalker(delta);

        public override Transform Head => d.head;
        public override float HeadZ => d.headZ;
        public override void PlaceHead(float z, bool moving) => d.PlaceHead(z, moving);
        public override IReadOnlyList<Renderer> HeadWall => d.headWall;
        public override IReadOnlyList<Transform> Sweeps => d.sweeps;
        public override Transform GoalLeaf => d.goalLeaf;
        public override Collider GoalLeafCollider => d.goalLeafCollider;
        public override Renderer GoalLite => d.goalLite;

        public override void SetLite(float scale)
        {
            if (d.goalLite == null) return;
            d.liteTouched = true;
            d.block.Clear();
            d.goalLite.GetPropertyBlock(d.block);
            d.block.SetColor(BaseColorId, C42 * 2.4f * Mathf.Max(0f, scale));
            d.goalLite.SetPropertyBlock(d.block);
        }

        public override bool GoalOpen => d.goalOpen;

        public override void CloseGoal()
        {
            d.goalOpen = false;
            if (d.goalLeaf != null) d.goalLeaf.localRotation = Quaternion.identity;
        }

        public override Transform Blocks => d.blocksRoot;
        public override void ShowBlocks(bool on) => d.ShowBlocks(on);
        public override void FlyBlocks(float t, float travel, float from, float to) => d.FlyBlocks(t, travel, from, to);

        // The head's box (the wall, the bracket and the goal head 1.2 m out, the trolleys), in world space.
        public override Bounds HeadBounds() =>
            new Bounds(World(new Vector3(0f, Ceiling * .5f, d.headZ - .55f)), new Vector3(2f * HalfWidth + .04f, Ceiling + .04f, 1.45f));

        public override Transform GoalUnit => d.goalUnit;
        public override Transform PanicBar => d.goalBar;
        public override Transform HeadMachine => d.headMachine;
        public override IReadOnlyList<Rect> HeadWallPieces => d.wallPieces;
        public override IReadOnlyList<Rect> HeadSlots => d.slotRects;
        public override Transform ShortRoom => d.shortRoom;

        public override Renderer CutPiece(FrontRoomsStructuresRunEnd end, Renderer piece, Bounds box, Transform parent, Material material, bool planarUv) =>
            d.CutPieceNow(end, piece, box, parent, material, planarUv);

        static bool Valid(int c) => c >= 0 && c < Cells;
        public override bool CellShown(int c) => Valid(c) && d.cellShown[c];
        public override bool CellOpen(int c) => Valid(c) && d.cellOpen[c];
        public override float RevealTime(int c) => Valid(c) ? d.revealAt[c] : float.NegativeInfinity;
        public override void ShowCell(int c, bool on) => d.SetShown(c, on);
        public override void OpenCell(int c, bool strike) => d.OpenCellFor(c, strike);
        public override void CloseCell(int c) => d.CloseCellFor(c);
        public override void ShowBetween(float from, float to) => d.ShowCells(Mathf.Max(0f, from), Mathf.Min(Length, to));

        public override void Seat(float from, float to, bool strike)
        {
            from = Mathf.Max(0f, from);
            to = Mathf.Min(Length, to);
            if (strike) d.Seated(from, to);
            else d.SeatQuiet(from, to);
        }

        public override IReadOnlyList<Renderer> CellRenderers(int c) => d.cellRenderers[Mathf.Clamp(c, 0, Cells - 1)];
        public override IReadOnlyList<GameObject> CellObjects(int c) => d.cellObjects[Mathf.Clamp(c, 0, Cells - 1)];

        public override void Leaving(float line)
        {
            foreach (var pair in d.bandParts)
                if (Mathf.Abs(pair.Key - line) < .01f) { d.Leaving(pair.Key); return; }
            d.Leaving(line);
        }

        public override GameObject Band(float line)
        {
            foreach (var pair in d.bandParts) if (Mathf.Abs(pair.Key - line) < .01f) return pair.Value;
            return null;
        }

        public override GameObject Magnets(float line)
        {
            foreach (var pair in d.magnets) if (Mathf.Abs(pair.Key - line) < .01f) return pair.Value;
            return null;
        }

        public override void RefreshMagnets() => d.RefreshMagnetsNow();
        public override IReadOnlyList<float> SeatedLines => d.seatedLines;
        public override Transform Track => d.trackPart;
        public override Transform FireFrame(int i) => i >= 0 && i < d.fireFrames.Length ? d.fireFrames[i] : null;

        public override Transform Dress => d.dressRoot;
        public override IReadOnlyList<Renderer> DressRenderers => d.dressRenderers;
        public override IReadOnlyList<Renderer> LongShell => d.longShell;
        public override IReadOnlyList<Collider> DressColliders => d.dressColliders;

        public override GameObject Blind => d.blindPart != null ? d.blindPart.gameObject : null;

        public override void SetBlind(bool on)
        {
            if (d.blindPart != null) d.blindPart.gameObject.SetActive(on);
        }

        public override void SetHall(float level)
        {
            d.hallTouched = true;
            foreach (var s in d.hallStrips) d.SetStrip(s, level, 0f, level, 0f);
        }

        public override int StripCount => d.strips.Count;
        public override float StripZ(int i) => d.strips[i].z;
        public override int StripCell(int i) => d.strips[i].cell;
        public override Light StripLight(int i) => d.strips[i].light;
        public override Transform StripRoot(int i) => d.strips[i].root != null ? d.strips[i].root.transform : null;
        public override int HeadCount => d.heads.Count;
        public override float HeadLampZ(int i) => d.heads[i].z;
        public override int HeadCell(int i) => d.heads[i].cell;
        public override bool IsGoalHead(int i) => d.heads[i].goal;
        public override Light HeadLight(int i) => d.heads[i].light;
        public override FrontRoomsStructuresRunEndStrip TodayStrip(int i) => d.TodayStripDrive(d.strips[i], d.clock);
        public override FrontRoomsStructuresRunEndHead TodayHead(int i) => d.TodayHeadDrive(d.heads[i], d.clock);

        public override FrontRoomsStructuresRunEndStrip ShownStrip(int i)
        {
            d.EnsureEndArrays();
            return d.stripShown[i];
        }

        public override FrontRoomsStructuresRunEndHead ShownHead(int i)
        {
            d.EnsureEndArrays();
            return d.headShown[i];
        }

        public override void HoldStrip(int i, FrontRoomsStructuresRunEndStrip target, float fade) => d.HoldStripNow(i, target, fade);
        public override void ReleaseStrip(int i, float fade) => d.ReleaseStripNow(i, fade);

        public override bool StripHeld(int i)
        {
            d.EnsureEndArrays();
            return d.stripHolds[i].on;
        }

        public override void HoldHead(int i, FrontRoomsStructuresRunEndHead target, float fade) => d.HoldHeadNow(i, target, fade);
        public override void ReleaseHead(int i, float fade) => d.ReleaseHeadNow(i, fade);

        public override bool HeadHeld(int i)
        {
            d.EnsureEndArrays();
            return d.headHolds[i].on;
        }

        public override void ReleaseLamps(float fade)
        {
            for (var i = 0; i < d.strips.Count; i++) d.ReleaseStripNow(i, fade);
            for (var i = 0; i < d.heads.Count; i++) d.ReleaseHeadNow(i, fade);
        }

        public override FrontRoomsStructuresRunEndStrip TodayStripAt(int i, float z) => d.TodayStripDriveAt(d.strips[i], d.clock, z);
        public override FrontRoomsStructuresRunEndHead TodayHeadAt(int i, float z) => d.TodayHeadDriveAt(d.heads[i], d.clock, z);
        public override FrontRoomsStructuresRunEndStrip TrippedStrip(int i) => d.TrippedStripDrive(d.strips[i], d.clock);
        public override float TrippedSince(int i) => d.TrippedSinceFor(d.strips[i]);
        public override float WhiteDrop(float t) => d.WhiteDrop(t);
        public override float Afterglow(float t) => d.Afterglow(t);
        public override float RedRise(float t) => d.RedRise(t);
        public override float PulseAt(float t, float z) => d.PulseEased(t, .15f * CellIndexAt(z));
        public override float Restrike(float tr, float z) => d.Restrike(tr, z);
        public override float RestrikeAt(float z) => d.RestrikeStart(z);

        public override void Fog(Color colour, float density) => d.SetFogOverride(colour, density);
        public override void ClearFog() => d.ClearFogNow();
        public override Color PresetFogColour => d.fogOn ? d.savedFogColour : RenderSettings.fogColor;
        public override float PresetFogDensity => d.fogOn ? d.savedFogDensity : RenderSettings.fogDensity;
        public override void Ambient(Color sky, Color equator, Color ground) => d.SetAmbientOverride(sky, equator, ground);
        public override void ClearAmbient() => d.ClearAmbientNow();
        public override Transform BeginPart(FrontRoomsStructuresRunEnd end, string name) => d.BeginEndPart(end, name);
        public override void EndPart() => d.CloseRoomPart();

        public override Transform NewObject(FrontRoomsStructuresRunEnd end, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(end != null && end.Own != null ? end.Own : d.Root, false);
            return go.transform;
        }
    }
}
