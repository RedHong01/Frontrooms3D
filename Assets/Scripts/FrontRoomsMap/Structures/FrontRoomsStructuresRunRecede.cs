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
    int look;                   // today's Run! B on arrival (70 section 9); A, B (agreed), C by L
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
        lower = FrontRoomsPoolMaterials.Surface("RunB_BlockLower", new Color(.55f, .56f, .55f));
        upper = FrontRoomsPoolMaterials.Surface("RunB_BlockUpper", new Color(.78f, .78f, .74f));
        floorMat = FrontRoomsPoolMaterials.Surface("RunB_Floor", new Color(.62f, .61f, .58f));
        soffitMat = FrontRoomsPoolMaterials.Surface("RunB_Soffit", new Color(.7f, .7f, .68f));
        cove = FrontRoomsPoolMaterials.Surface("RunB_Cove", new Color(.3f, .3f, .3f));
        frameMat = FrontRoomsPoolMaterials.Surface("Door_Frame", new Color(.18f, .15f, .12f));
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
        ResetRoom(false);
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
        var origin = Root.position;
        foreach (var r in dress.GetComponentsInChildren<Renderer>(true))
        {
            if (r == null) continue;
            var b = r.bounds;
            if (b.size.z > 4.5f) continue;
            var z0 = b.min.z - origin.z;
            cellRenderers[CellOf(z0 + .05f)].Add(r);
        }
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

    // A door frame (dark bronze casing) on a 1.0 x 2.1 opening centred at x, on the wall plane z (district metres).
    void Frame(float x, float z)
    {
        kit.Box(x - .56f, 0f, z - .1f, x - .5f, 2.16f, z + .1f, frameMat, false);
        kit.Box(x + .5f, 0f, z - .1f, x + .56f, 2.16f, z + .1f, frameMat, false);
        kit.Box(x - .56f, 2.1f, z - .1f, x + .56f, 2.16f, z + .1f, frameMat, false);
    }

    // An RN-F leaf hinged at <paramref name="hingeLocal"/> (parent space), swinging toward +z: a 15 x 15 in F5 lite 0.34 m
    // from the hinge edge at 1.194-1.575 m, a panic bar on the -z face; lit (the goal's C42 box behind it) or glazed.
    Transform Leaf(Transform parent, Vector3 hingeLocal, string name, bool lit, out Collider leafCollider)
    {
        var hinge = new GameObject(name);
        hinge.transform.SetParent(parent, false);
        hinge.transform.localPosition = hingeLocal;
        var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        slab.name = "leaf";
        slab.transform.SetParent(hinge.transform, false);
        slab.transform.localPosition = new Vector3(.5f, 1.05f, 0f);
        slab.transform.localScale = new Vector3(.98f, 2.08f, .045f);
        slab.GetComponent<Renderer>().sharedMaterial = leafMat;
        leafCollider = slab.GetComponent<Collider>();
        var lite = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lite.name = "F5 lite";
        UnityEngine.Object.Destroy(lite.GetComponent<Collider>());
        lite.transform.SetParent(hinge.transform, false);
        lite.transform.localPosition = new Vector3(.34f + .1905f, 1.3843f, 0f);
        lite.transform.localScale = new Vector3(.381f, .381f, .05f);
        var lr = lite.GetComponent<Renderer>();
        lr.sharedMaterial = lit ? FrontRoomsPoolMaterials.Glow("SP7 goal lite (C42)", C42 * 1.4f) : FrontRoomsStructuresKit.GlassMaterial();
        lr.shadowCastingMode = ShadowCastingMode.Off;
        var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bar.name = "panic bar";
        UnityEngine.Object.Destroy(bar.GetComponent<Collider>());
        bar.transform.SetParent(hinge.transform, false);
        bar.transform.localPosition = new Vector3(.5f, 1.0f, lit ? -.06f : .06f);
        bar.transform.localScale = new Vector3(.8f, .05f, .05f);
        bar.GetComponent<Renderer>().sharedMaterial = FrontRoomsPoolMaterials.Steel;
        if (lit)
        {
            // C42's light in a small box behind the lite (greybox): a steel shroud on the leaf's far face round the lite.
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "STAND-IN C42 light box";
            UnityEngine.Object.Destroy(box.GetComponent<Collider>());
            box.transform.SetParent(hinge.transform, false);
            box.transform.localPosition = new Vector3(.34f + .1905f, 1.3843f, .1f);
            box.transform.localScale = new Vector3(.5f, .5f, .14f);
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

    // ---------------------------------------------------------------- compartment frames (10 RN-2): FD1 z 9 (the trigger), FD2 z 18

    void BuildFireFrames()
    {
        kit.Shell(Ceil, 0f);
        foreach (var z in new[] { Fd1, Fd2 })
        {
            // 0.16 block on the cell line, a 2.0 x 2.1 frame centred with dark-bronze casing, the pair of 1.0 m leaves held
            // open at 90 degrees on magnets against the walls on the entrance side (2.0 x 2.1 m clear: the Relay's 2.05 passes).
            kit.WallX(z, -HalfW, HalfW, 0f, Ceil, upper, new List<FrontRoomsStructuresKit.Opening> { new FrontRoomsStructuresKit.Opening(-1f, 1f, 0f, 2.1f) }, .16f);
            kit.Box(-HalfW, 0f, z - .081f, -1.06f, 1.2f, z - .083f, lower, false);
            kit.Box(1.06f, 0f, z - .081f, HalfW, 1.2f, z - .083f, lower, false);
            kit.Box(-HalfW, 0f, z + .081f, -1.06f, 1.2f, z + .083f, lower, false);
            kit.Box(1.06f, 0f, z + .081f, HalfW, 1.2f, z + .083f, lower, false);
            kit.Box(-1.06f, 0f, z - .1f, -1f, 2.16f, z + .1f, frameMat, false);
            kit.Box(1f, 0f, z - .1f, 1.06f, 2.16f, z + .1f, frameMat, false);
            kit.Box(-1.06f, 2.1f, z - .1f, 1.06f, 2.16f, z + .1f, frameMat, false);
            kit.Box(-1.045f, 0f, z - 1.08f, -1.0f, 2.08f, z - .1f, leafMat);
            kit.Box(1.0f, 0f, z - 1.08f, 1.045f, 2.08f, z - .1f, leafMat);
            foreach (var s in new[] { -1f, 1f })
            {
                // A wall magnet with its extension arm to the leaf (greybox).
                kit.StandIn("hold-open magnet", new Vector3(s * 1.385f, 1.9f, z - .95f), new Vector3(.07f, .08f, .08f), FrontRoomsPoolMaterials.Steel, 0f, false);
                kit.Box(s * 1.35f - .015f, 1.885f, z - .965f, s * 1.35f + .015f, 1.915f, z - .935f, FrontRoomsPoolMaterials.Steel, false);
                kit.Box(Mathf.Min(s * 1.35f, s * 1.045f), 1.885f, z - .965f, Mathf.Max(s * 1.35f, s * 1.045f), 1.915f, z - .935f, FrontRoomsPoolMaterials.Steel, false);
            }
        }
    }

    // ---------------------------------------------------------------- the head's machine: track, guide slot, magnets (receding)

    void BuildTrack()
    {
        trackPart = kit.BeginPart("SP7 recede track (receding variants)");
        kit.Shell(Ceil, 0f);
        var steel = FrontRoomsPoolMaterials.Steel;
        // A surface-mounted 100 mm steel track in both soffit corners from the FD1 frame to the end wall, hangers every 1.5 m;
        // the 20 mm guide slot in the floor from z 27 to 42 (71 section 5.1; in view from the arch: the honest tell).
        foreach (var s in new[] { -1f, 1f })
        {
            kit.Box(s * 1.30f - .05f, 2.845f, Fd1 + .1f, s * 1.30f + .05f, 2.895f, Length, steel, false);
            for (var z = Fd1 + .75f; z < Length; z += 1.5f)
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
        const float t = .08f;
        // Block-faced end wall (the two tones), the door opening 1.0 x 2.1 with its frame; solid for the walker. Over 2.25 m the
        // wall is slotted where the fixtures of the cells it uncovers cross its outline (70 section 9: the side heads and the
        // remote EMT on both walls, the tray, the strip, the main), brush seals lining each slot (greybox).
        foreach (var r in WallPieces())
        {
            if (r.yMin < 1.2f - .001f) kit.Box(r.xMin, r.yMin, -t, r.xMax, Mathf.Min(1.2f, r.yMax), t, lower);
            if (r.yMax > 1.2f + .001f) kit.Box(r.xMin, Mathf.Max(1.2f, r.yMin), -t, r.xMax, r.yMax, t, upper);
        }
        foreach (var sl in Slots)
        {
            kit.Box(sl.x - .012f, sl.y, -t - .004f, sl.x + .003f, HeadTop, t + .004f, rubber, false);
            kit.Box(sl.x + sl.width - .003f, sl.y, -t - .004f, sl.x + sl.width + .012f, HeadTop, t + .004f, rubber, false);
            if (sl.y > Band + .01f) kit.Box(sl.x, sl.y - .012f, -t - .004f, sl.x + sl.width, sl.y + .003f, t + .004f, rubber, false);
        }
        kit.Box(-HeadHalf, 0f, -t - .0035f, -.56f, .102f, -t, cove, false);
        kit.Box(.56f, 0f, -t - .0035f, HeadHalf, .102f, -t, cove, false);
        kit.Box(-.56f, 0f, -.1f, -.5f, 2.16f, .1f, frameMat, false);
        kit.Box(.5f, 0f, -.1f, .56f, 2.16f, .1f, frameMat, false);
        kit.Box(-.56f, 2.1f, -.1f, .56f, 2.16f, .1f, frameMat, false);
        // The guide pin under the wall (in the slot at x 0.95) and the two trolleys in the soffit corners.
        kit.Box(.93f, 0f, .02f, .97f, .03f, .06f, FrontRoomsPoolMaterials.Steel, false);
        foreach (var s in new[] { -1f, 1f })
        {
            kit.Box(s * 1.30f - .07f, 2.78f, -.16f, s * 1.30f + .07f, 2.845f, .16f, FrontRoomsPoolMaterials.Steel, false);
            // The trolley's hanger: an arm over the side slot to a plate on the slot's inner edge, down to the band.
            kit.Box(Mathf.Min(s * 1.105f, s * 1.30f), 2.76f, -.03f, Mathf.Max(s * 1.105f, s * 1.30f), 2.78f, .03f, FrontRoomsPoolMaterials.Steel, false);
            kit.Box(Mathf.Min(s * 1.085f, s * 1.105f), Band, -.05f, Mathf.Max(s * 1.085f, s * 1.105f), 2.78f, .05f, FrontRoomsPoolMaterials.Steel, false);
        }
        // The goal head's bracket: an arm from the wall over the door to a plate 1.2 m out, the twin-head unit under it.
        kit.Box(-.025f, 2.6f, -1.18f, .025f, 2.65f, -t, FrontRoomsPoolMaterials.Steel, false);
        kit.Box(-.2f, 2.30f, -1.2f, .2f, 2.65f, -1.18f, FrontRoomsPoolMaterials.Steel, false);
        // A knee brace from the wall over the door frame to the arm.
        var braceA = new Vector3(0f, 2.26f, -t);
        var braceB = new Vector3(0f, 2.61f, -.82f);
        kit.Mesh.Box((braceA + braceB) * .5f, new Vector3(.04f, .04f, (braceB - braceA).magnitude), Quaternion.LookRotation((braceB - braceA).normalized, Vector3.up), FrontRoomsPoolMaterials.Steel, false);
        kit.EndPart();
        foreach (var r in head.GetComponentsInChildren<Renderer>(true))
            if (r.sharedMaterial == lower || r.sharedMaterial == upper || r.sharedMaterial == cove) headWall.Add(r);
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
                BuildUnitHeads(unit.transform, false, Vector3.zero, Vector3.zero, d.remote, d.trimMinus, d.trimPlus, cell, d);
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
        l.type = LightType.Spot;
        l.spotAngle = rig.wrapOuter;
        l.innerSpotAngle = rig.wrapInner;
        l.color = rig.wrapColor;
        l.intensity = rig.wrapIntensity * scale;
        l.range = range;
        l.shadows = LightShadows.None;
        l.shadowStrength = .9f;
        l.shadowNearPlane = .1f;
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
    HeadLamp[] BuildUnitHeads(Transform unit, bool goal, Vector3 aimA, Vector3 aimB, bool remote, float trimMinus, float trimPlus, int cell, RunUnitDef def = default(RunUnitDef), bool explicitAim = false)
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
        // 70 section 9's sign, flat on the vestibule's east wall facing west, centre (1.42, 1.5, -4.0), out of the main view;
        // its text verbatim with the plan's key letters (70: L length, C light, G entrance, V Reduce Flashing -> V, L, O, F here,
        // as 70 allows). Two changes for what happened after 70's 06:22 edit, so the sign never asks what Red has answered or
        // lists a stand-in that is gone: Red said yes to the red tube on 11-04 (the brighter level, then the brightest column;
        // 70 section 10 already marks it answered, on on arrival), so it left the greybox list (the strip kit's own tube now)
        // and its open item (the measurements renumbered 1).
        kit.Sign(new Vector3(HalfW - .03f, 1.5f, -4.0f), new Vector3(-1.2f, 1.5f, -4.0f), "SP7 · Run!：会往后退的门",
            "游戏里的 Run!（Run! B 的设备走廊），平地，最长 14 格、42 米。你 11 月 3 日对 71 的九个决定回了\"全部按推荐\"，这里照推荐搭好，让你亲眼走一遍；走过以后你说可以，会退的门、Run 门入口和红色灯光才进游戏；觉得哪里不对，直接告诉我们。走法和游戏一样：Ctrl 冲刺（体力 5 秒），Shift 慢走。进来时是\"会退的 9→14 格\"：跨过 9 米那道防火门框，警报触发，尽头的墙和门往后退：先猛缩两格，再一格一格退，最后重重停住，门就是真门，推开是一间亮着的大厅。走路时门只在第一下猛缩时变远，按住 Shift 慢走才会一直越退越远；冲到离门 12 米以内，剩下的一段一口气滑到底。\n"
            + "按键：V 长度（普通 7 / 9 / 12 / 14 格，会退 9→12 / 9→14 格）；B 后退方式（T2 砖块拆开再砌回，电脑和 iPad 用；T1 整面墙滑走，网页版用）；L 触发后的灯光（今天的 / A 警报回路 / B 警报脉冲，你同意的推荐 / C 红潮）；H 红灯管；O 入口（拱门 / 带小方窗的 Run 门，只在普通长度）；F 减少闪烁；T 手动触发，再按一次恢复：白光从入口一格一格亮回来（你喜欢的那段）；Y 把门放回原位再试。旁边的小牌子显示现在的设置。\n"
            + "灰盒占位：砖块重组的路径（简单程序动画）、轨道、门吸、密封条、墙上的\"印子\"；灯光由场景 4 专用的脚本模拟，正式效果等游戏视觉做；没有声音，没有会动的 Relay。不到 14 格时尽头的门在这里打不开，因为门后是还没亮出来的走廊（游戏里这扇门永远不锁）。黑色人形是 Relay 的静态替身（2.05 米），只用来检查视线，不会动。\n"
            + "还没定的：1 要在游戏里实测、这里测不出来的：机器人测被追着领先 8 米冲进会退的房间能不能逃掉、冲到门口关上门的人有多少真能甩掉它、12 格和 14 格在 100 个种子里放得下多少；B 的光敏安全用 Harding FPA 或 PEAT 测。", 0f);
        // The plate beside the sign: the current settings, in the placard's colours.
        var plate = new GameObject("SP7 settings plate");
        plate.transform.SetParent(Root, false);
        plate.transform.localPosition = new Vector3(HalfW - .02f, 1.5f, -5.2f);
        plate.transform.localRotation = Quaternion.LookRotation(Vector3.right);
        var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        panel.name = "Panel";
        UnityEngine.Object.Destroy(panel.GetComponent<Collider>());
        panel.transform.SetParent(plate.transform, false);
        panel.transform.localPosition = new Vector3(0f, 0f, .01f);
        panel.transform.localScale = new Vector3(1.0f, .45f, .015f);
        panel.GetComponent<Renderer>().sharedMaterial = FrontRoomsPoolMaterials.Lit("placard panel E", new Color(.086f, .082f, .075f), .1f);
        var textGo = new GameObject("Text");
        textGo.transform.SetParent(plate.transform, false);
        textGo.transform.localPosition = new Vector3(-.46f, .195f, -.002f);
        plateText = textGo.AddComponent<TextMesh>();
        var font = FrontRoomsStructuresSigns.BodyFontAsset;
        if (font != null) plateText.font = font;
        plateText.fontSize = 48;
        plateText.characterSize = .028f * 10f / 48f;
        plateText.lineSpacing = 1.15f;
        plateText.anchor = TextAnchor.UpperLeft;
        plateText.alignment = TextAlignment.Left;
        plateText.color = new Color(.957f, .945f, .91f);
        plateText.richText = true;
        var tr = textGo.GetComponent<MeshRenderer>();
        if (tr != null)
        {
            if (font != null && font.material != null) tr.sharedMaterial = font.material;
            tr.shadowCastingMode = ShadowCastingMode.Off;
        }
        // The Relay stand-in (71 section 10): x -0.6, z 6, facing north.
        kit.Relay(new Vector3(-.6f, 0f, 6f), new Vector3(-.6f, 0f, 20f));
        // Metric marks (M).
        kit.Mark(new Vector3(1.3f, 1.95f, 1.5f), Vector3.left, "1.5 米 · 拱门入口在这里触发");
        kit.Mark(new Vector3(1.3f, 2.25f, 2.5f), Vector3.left, "2.5 米 · Run 门入口在这里触发");
        kit.Mark(new Vector3(1.3f, 2.3f, Fd1 - .3f), Vector3.left, "FD1 · 9 米 · 会退的房间在这里触发");
        kit.Mark(new Vector3(1.3f, 2.3f, Fd2 - .3f), Vector3.left, "FD2 · 18 米 · 没看向尽头时最晚在这里开始退");
        kit.Mark(new Vector3(1.3f, 1.7f, 20.6f), Vector3.left, "7 格 · 21 米");
        kit.Mark(new Vector3(1.3f, 1.7f, 26.6f), Vector3.left, "9 格 · 27 米（今天）");
        kit.Mark(new Vector3(1.3f, 1.7f, 35.6f), Vector3.left, "12 格 · 36 米");
        kit.Mark(new Vector3(1.3f, 1.7f, 41.6f), Vector3.left, "14 格 · 42 米（上限）");
    }

    // ---------------------------------------------------------------- the room: reset, trip, the reset re-strike

    // Y, V, entering and leaving: the head back on its rest line, the hidden cells off, the record cleared, armed.
    void ResetRoom(bool moveWalker, FrontRoomsStructures host = null)
    {
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
            else lastWalkerZ = p.z;
        }
        plateDirty = true;
        Evaluate();
    }

    bool MagnetFor(float line)
    {
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
        if (Receding && !receded && Mathf.Abs(headZ - Rest) < .01f)
        {
            recedePending = true;
            BuildSchedule();
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
        if (phase == Phase.Resetting && clock >= ResetEnd)
        {
            phase = Phase.Armed;
            clock = 0f;
            SetAlarmLatch(false);
            ApplyPending();
            ApplyTube();
            plateDirty = true;
        }
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
    }

    void Finished()
    {
        recedeRunning = recedePending = false;
        receded = true;
        beatPhase = Beat.Done;
        finalSeatTime = phase == Phase.Tripped ? clock : -1f;
        if (!seatedLines.Contains(headZ)) seatedLines.Add(headZ);
        plateDirty = true;
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
        if (recedePending || recedeRunning) return 1f;
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
                    if (s.cell >= 0 && !float.IsNegativeInfinity(revealAt[s.cell])) continue;   // revealed strips unshadowed (71 5.2)
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
            + "后退方式（B）：<color=#F4DF3B>" + (bodyT2 ? "T2 重组" : "T1 后退") + "</color>" + Later(pendingBody, pendingBody == 1 ? "T2 重组" : "T1 后退") + "\n"
            + "触发后的灯光（L）：<color=#F4DF3B>" + PlateLook[look] + "</color>" + Later(pendingLook, pendingLook >= 0 ? PlateLook[pendingLook] : "") + "\n"
            + "红灯管（H）：<color=#F4DF3B>" + (tube ? "开" : "关") + "</color>" + Later(pendingTube, pendingTube == 1 ? "开" : "关") + "\n"
            + "入口（O）：<color=#F4DF3B>" + (doorEntrance ? "Run 门" : "拱门") + "</color>" + (Receding ? "（会退的只用拱门）" : "") + Later(pendingDoor, pendingDoor == 1 ? "Run 门" : "拱门") + "\n"
            + "减少闪烁（F）：<color=#F4DF3B>" + (reduce ? "开" : "关") + "</color>\n"
            + "<color=#96938B>" + state + "</color>";
    }

    // ---------------------------------------------------------------- keys, doors, the hall's plate

    public override FrontRoomsStructuresLook.Preset PresetAt(Vector3 world)
    {
        var p = world - Site;
        if (p.z < 0f) return FrontRoomsStructuresLook.Preset.Level0;
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
                return true;
            case UnityEngine.InputSystem.Key.B:
            {
                var next = !(pendingBody >= 0 ? pendingBody == 1 : bodyT2);
                if (armed) { bodyT2 = next; pendingBody = -1; } else pendingBody = next ? 1 : 0;
                plateDirty = true;
                message = "后退方式：" + (next ? "T2 砖块拆开再砌回去" : "T1 整面墙滑走") + later;
                return true;
            }
            case UnityEngine.InputSystem.Key.L:
            {
                var next = ((pendingLook >= 0 ? pendingLook : look) + 1) % LookNames.Length;
                if (armed) { look = next; pendingLook = -1; } else pendingLook = next;
                plateDirty = true;
                message = "触发后的灯光：" + LookNames[next] + (!tube && next != 0 ? "（红灯管关着，按 H 打开才有红光）" : "") + later;
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

    bool GoalCanOpen => Mathf.Abs(headZ - Length) < .01f && !recedeRunning;

    public override string Describe(Collider c, out bool hold)
    {
        hold = false;
        if (c == null) return null;
        if (c == entranceLeafCollider) return entranceOpen ? "E  ·  关门" : "E  ·  开门";
        if (c == goalLeafCollider)
        {
            if (!GoalCanOpen) return "这扇门在场景 4 里打不开（长度不到 14 格；游戏里门永远不锁）";
            return goalOpen ? "E  ·  关门" : "E  ·  开门（推杠）";
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
        return PlateVariant[variant] + " · " + (bodyT2 ? "T2" : "T1") + " · 灯光 " + PlateLook[Look] + " · 红灯管" + (tube ? "开" : "关") + " · " + (doorEntrance ? "Run 门" : "拱门") + " · 减少闪烁" + (reduce ? "开" : "关") + " · " + state;
    }

    public override void Guide(FrontRoomsStructures host, StringBuilder sb)
    {
        sb.AppendLine(Title);
        sb.AppendLine("Run! B 的设备走廊，平地，最长 14 格（42 米），尽头是一整块会动的端头：墙、带 15×15 英寸小方窗的 Run 门、门上方架子上的应急灯。会退的版本在 9 米那道防火门框触发警报，端头按 71 号文档的节奏往后退；普通的版本就是 7、9、12、14 格的固定长度。");
        sb.AppendLine();
        sb.AppendLine("按键：V 长度；B 后退方式 T2 / T1；L 触发后的灯光（今天的、A、B、C）；H 红灯管；O 入口（拱门、Run 门，只在普通长度）；F 减少闪烁；T 手动触发 / 恢复；Y 把门放回原位；E 开门；[ ] 换站点；R 回到站点；M 尺寸；C Relay 体型。B、L、H、O 在警报中按下，等下次待触发时生效。");
        sb.AppendLine();
        sb.AppendLine("要你决定（在对话里按编号告诉总控）：");
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
        foreach (var m in owned) if (m != null) UnityEngine.Object.Destroy(m);
        owned.Clear();
        base.Release();
    }
}
