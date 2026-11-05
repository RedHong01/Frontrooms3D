using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// SP6 (key 6) of scene 4 (RULES 5b; research/level_space/70 section 8 at its 21:39 redo; 40 sections 6.1-6.2, 7.4, 11.4, 11.9;
/// 60 sections 2, 4, 5; bathroom_rooms/50 section 1; 30 section 8, 31 section 5; zone_lamps/10_pool): the deep pool terrain, a walk
/// that keeps going down. A dry tiled corridor; through a tall narrow arch the BeachHall (an ankle-deep sheet, then a 1:12
/// beach into wading water and an apse; an arcaded terrace at +1.20); a guarded bridge over the L-shaped channel to a white
/// island (stepping stones, a lower deck, a cold-plunge wet block, a sight arch); the weir terraces (three 9 m weirs of 0.60
/// beside a dry stair walk, a basin at their foot); the dome hall 1.80 below the start (the B4-M helix rising 3.0 m round a
/// lit eye to an L gallery under a saucer dome whose off-centre oculus lays one disc of light on the water, a straight B2 as
/// the Relay's way, a stair into the water); the grotto (a low vaulted water passage ending 12 m in at a lit niche); off the
/// route the Casbah (tiled blocks at four levels in a flooded floor) and C8 (a covered basin under an access floor). All
/// water is the level's wrong turquoise, nothing deeper than 1.20, nothing flickers. Whole modules on the cell grid (40 11.9).
/// Greybox where no kit exists, listed on the sign. L switches the dome hall's light between A (today's) and B (S1 C: the vault
/// in shadow, small bright lamps thrown down; the section "the dome hall's light" below); everything else in SP6 keeps A.
/// </summary>
public sealed class FrontRoomsStructuresPoolTerrain : FrontRoomsStructuresDistrict
{
    const float Tall = 5.4f, Std = 2.9f, Terrace = 1.2f, DomeDeck = -1.8f, DomeLid = 3.6f;
    const float WadeSurf = -.03f, WadeFloor = -1.2f, DeepSurf = -1.83f, DeepFloor = -3.0f;
    static readonly Vector3 Arrival = new Vector3(4.5f, 0f, 1.5f);

    FrontRoomsStructuresKit kit;
    Material tile, nosing, sheet, fall;
    readonly List<Light> viewLamps = new List<Light>();

    // The dome hall's light B (L; the section "the dome hall's light" below).
    const float VaultShade = .20f, SoffitShade = .30f, WallShadeTop = .32f, WallShadeCurve = 1.25f, ShadeFrom = 1.8f, Liner = .006f;
    const int ShadeCourses = 12;
    static readonly Color MetalHalide = new Color(.90f, .97f, 1f);
    static readonly Color SpotLens = new Color(8.6f, 9.3f, 9.6f), SoffitLens = new Color(7.4f, 8.0f, 8.3f), CornerLens = new Color(6.2f, 6.7f, 6.9f);
    static readonly Color OculusDiffuser = new Color(2.4f, 2.55f, 2.65f);
    static readonly Vector3 DomeProbeCentre = new Vector3(3f, 1.5f, 42f);
    static readonly Vector2 Oculus = new Vector2(1.5f, 43.5f);
    bool domeB, domeReady, domeShaded;
    readonly List<Light> domeA = new List<Light>(), domeLampsB = new List<Light>();
    readonly List<Renderer> domeHideB = new List<Renderer>(), domeShowB = new List<Renderer>();
    readonly List<GameObject> domePartsB = new List<GameObject>();
    readonly List<DomeShade> domeShades = new List<DomeShade>();
    readonly List<Bounds> domeArms = new List<Bounds>();
    readonly UnityEngine.Rendering.SphericalHarmonicsL2[] domeSH = new UnityEngine.Rendering.SphericalHarmonicsL2[1];
    UnityEngine.Rendering.SphericalHarmonicsL2 domeBase;
    MaterialPropertyBlock domeBlock;
    ReflectionProbe domeProbe;
    int domeProbeFrames;

    // One renderer B shades: the room's own ambient probe times k. A shared one (the vault, which A draws too) gets back its
    // probe use and property block exactly as built when A comes back.
    sealed class DomeShade
    {
        public Renderer renderer;
        public float k;
        public bool shared;
        public UnityEngine.Rendering.LightProbeUsage usage;
        public MaterialPropertyBlock block;
    }

    public FrontRoomsStructuresPoolTerrain(Vector3 site)
        : base(6, "SP6", "SP6 · 泳池层：深处的地形和大螺旋楼梯", "泳池层深处的地形和大螺旋楼梯。", site) { }

    // ---------------------------------------------------------------- build

    public override void Build(FrontRoomsStructures host)
    {
        kit = new FrontRoomsStructuresKit(host, this);
        tile = FrontRoomsPoolMaterials.Tile;
        nosing = FrontRoomsPoolMaterials.Nosing;
        sheet = FrontRoomsPoolMaterials.Transparent("water sheet", new Color(.55f, .85f, .86f, .32f), .95f);

        kit.Safe("corridor", BuildCorridor);
        kit.Safe("BeachHall", BuildBeachHall);
        kit.Safe("IslandChannel", BuildIsland);
        kit.Safe("C9 wet block", BuildWetBlock);
        kit.Safe("WeirTerraces", BuildWeirs);
        kit.Safe("DomeGallery", BuildDomeHall);
        kit.Safe("B4-M", BuildB4M);
        kit.Safe("dome", BuildDome);
        kit.Safe("Grotto", BuildGrotto);
        kit.Safe("Casbah", BuildCasbah);
        kit.Safe("C8 covered basin", BuildCoveredBasin);
        kit.Finish();
        kit.Safe("signs", BuildSigns);

        Box = new Bounds(Site + new Vector3(1.5f, 2f, 28.5f), new Vector3(43f, 14f, 67f));
        LowestFloor = Site.y + DeepFloor;
        Far = 80f;
        Stops.Clear();
        kit.Stop("到达点：瓷砖走廊", Arrival, new Vector3(4.5f, 1.62f, 21f));
        kit.Stop("拱廊台地", new Vector3(2.35f, Terrace, 13.5f), new Vector3(7.5f, .5f, 19.5f));
        kit.Stop("岛上", new Vector3(1.5f, Terrace, 29.2f), new Vector3(1.5f, 1.6f, 40f));
        kit.Stop("溢水台地顶", new Vector3(10.5f, 0f, 25.4f), new Vector3(16.5f, -1.8f, 40f));
        kit.Stop("溢水台地底", new Vector3(10.5f, -1.8f, 40.5f), new Vector3(16.5f, -.3f, 27f));
        kit.Stop("穹顶回廊", new Vector3(5.5f, Terrace, 46.8f), new Vector3(1.5f, DeepSurf, 43.5f));
        kit.Stop("穹顶下：圆洞和水上的圆斑", new Vector3(1.5f, DomeDeck, 38.6f), new Vector3(1.5f, 1.2f, 43.5f));
        kit.Stop("穹顶大厅甲板", new Vector3(-1.4f, DomeDeck, 37.6f), new Vector3(0f, -1f, 48f));
        kit.Stop("水洞尽头的壁龛", new Vector3(0f, DeepFloor, 58.6f), new Vector3(0f, -1.2f, 48f));
        kit.Stop("瓷砖块群顶上", new Vector3(-9f, 2.4f, 15f), new Vector3(-15f, .6f, 6f));
        kit.Stop("检修地板水池", new Vector3(7.4f, 0f, .4f), new Vector3(13f, -.6f, 0f));

        foreach (var b in new[]
        {
            new Bounds(new Vector3(6f, Tall * .5f, 13.5f), new Vector3(12f, Tall, 15f)),
            new Bounds(new Vector3(1.5f, Tall * .5f, 28.5f), new Vector3(15f, Tall, 15f)),
            new Bounds(new Vector3(15f, 1.5f, 33f), new Vector3(12f, 8.4f, 18f)),
            new Bounds(new Vector3(3f, 1.5f, 42f), new Vector3(12f, 7.8f, 12f)),
            new Bounds(new Vector3(-9f, Tall * .5f, 12f), new Vector3(18f, Tall, 18f)),
        })
        {
            try { host.Look.Probe(Root, new Bounds(Site + b.center, b.size), 512); }
            catch (Exception e) { FrontRoomsPoolMaterials.LogOnce("SP6 probe", "SP6 probe: " + e.Message); }
        }
        kit.Safe("pool zone look", BuildZoneLook);
        // Last: its nine lamps join the scene's budget after every lamp of SP6, and nine keeps every later lamp's turn in the
        // budget's one-in-three shadow rule, so no other lamp in the scene changes.
        kit.Safe("dome hall light B", BuildDomeLightB);
    }

    // 游戏视觉's pool zone look (FrontRoomsStructuresLook.PoolRoom, as the map's AddPoolLooks): one PoolLevel volume per room
    // (the pool level; all water here is level water), 3 m under the room's floor to 0.2 m over its ceiling (the dome's crown,
    // the grotto's vault). The height fog sits on a room's water where all of it shares one height; the weir terraces' four
    // sheets (-0.03 to -1.83) do not, so that room has none, and the corridor is dry.
    void BuildZoneLook()
    {
        var look = kit.Host.Look;
        const string zone = "PoolLevel";
        look.PoolRoom(this, "corridor", zone, 3f, -3f, 6f, 6f, 0f, Std);
        look.PoolRoom(this, "BeachHall", zone, 0f, 6f, 12f, 21f, 0f, Tall, WadeSurf);
        look.PoolRoom(this, "IslandChannel", zone, -6f, 21f, 9f, 36f, 0f, Tall, WadeSurf);
        look.PoolRoom(this, "C9 wet block", zone, 3f, 30f, 6f, 36f, 0f, Std, WadeSurf);
        look.PoolRoom(this, "WeirTerraces", zone, 9f, 24f, 21f, 42f, 0f, Tall);
        look.PoolRoom(this, "DomeGallery", zone, -3f, 36f, 9f, 48f, DomeDeck, 6f, DeepSurf);
        look.PoolRoom(this, "Grotto", zone, -3f, 48f, 3f, 60.3f, DeepFloor, -.6f, DeepSurf);
        look.PoolRoom(this, "Casbah", zone, -18f, 3f, 0f, 21f, WadeFloor, Tall, WadeSurf);
        look.PoolRoom(this, "C8 covered basin", zone, 6f, -3f, 15f, 3f, 0f, Std, -.6f);
    }

    // ---------------------------------------------------------------- helpers

    List<FrontRoomsStructuresKit.Opening> Arches(params float[] spec)
    {
        // Groups of five: from, to, sill, top (springing when round), round (1 or 0).
        var list = new List<FrontRoomsStructuresKit.Opening>();
        for (var k = 0; k + 4 < spec.Length; k += 5) list.Add(new FrontRoomsStructuresKit.Opening(spec[k], spec[k + 1], spec[k + 2], spec[k + 3], spec[k + 4] > .5f));
        return list;
    }

    // A tiled solid block (a terrain level): its top at y, down to a base under every floor near it.
    void Block(float x0, float z0, float x1, float z1, float top, float bottom = -3.2f) => kit.Box(x0, bottom, z0, x1, top, z1, tile);

    // A tiled parapet 1.07 (0.2 thick) on a level edge.
    void Parapet(float x0, float z0, float x1, float z1, float floor) =>
        kit.Guard(new Vector3(x0, 0f, z0), new Vector3(x1, 0f, z1), floor, FrontRoomsStructuresKit.GuardStyle.TileWall);

    // A tiled parapet along a slope (nosing-line points a, b, b the upper end): a vertical-sided raking wall 0.2 thick, 1.07 over
    // the line, its ends vertical so it stops flush at the level parapet or floor it meets (the tilted box it replaces leaned
    // 0.24 m past both: scene 4 review SP6-F1); down to the floor beside a solid flight, or 0.35 under the nosing line beside a
    // flight-bridge over water (floor NaN). One collider along the slope.
    void SlopedParapet(Vector3 a, Vector3 b, float floor = float.NaN)
    {
        var waist = float.IsNaN(floor);
        kit.RakeWall(a, b, FrontRoomsStructuresKit.GuardHeight, waist ? a.y - .35f : floor, waist ? b.y - .35f : floor, .2f, tile, true);
    }

    // A falling water sheet (greybox: a transparent quad, both faces).
    void Sheet(Vector3 a, Vector3 b, float top, float bottom)
    {
        if (sheet == null || top - bottom < .02f) return;
        var keep = kit.Mesh.CastShadows;
        kit.Mesh.CastShadows = false;
        var n = Vector3.Cross(Vector3.up, b - a).normalized;
        kit.QuadPlanar(a + Vector3.up * bottom, b + Vector3.up * bottom, b + Vector3.up * top, a + Vector3.up * top, n, sheet);
        kit.QuadPlanar(a + Vector3.up * bottom, b + Vector3.up * bottom, b + Vector3.up * top, a + Vector3.up * top, -n, sheet);
        kit.Mesh.CastShadows = keep;
    }

    // A weir's fall: white water (brighter, alpha about 0.6), so the drop reads from upstream and below (review SP6-F8).
    void Fall(Vector3 a, Vector3 b, float top, float bottom)
    {
        if (fall == null) fall = FrontRoomsPoolMaterials.Transparent("weir fall (white water)", new Color(.86f, .95f, .96f, .58f), .7f);
        var keep = sheet;
        sheet = fall != null ? fall : keep;
        Sheet(a, b, top, bottom);
        sheet = keep;
    }

    // A wet-niche lamp (F6) set in a basin or channel wall, facing into the water: warm-free, steady.
    void Niche(Vector3 at, Vector3 into) => kit.Underwater(at, into, 2.0f, 6f, false);

    // A sealed flush lens (F5) in a lid.
    void Lens(Vector3 at, float intensity = 2.2f, float range = 7f) =>
        kit.Lamp("sealed flush lens (F5)", LightType.Spot, at, Vector3.down, 150f, 110f, intensity, range, new Color(.92f, .98f, 1f), false,
            new Vector3(.5f, .5f, .06f), FrontRoomsPoolMaterials.Steel, new Color(1.5f, 1.6f, 1.65f));

    // ---------------------------------------------------------------- the corridor (x 3..6, z -3..6), dry, Standard 2.9

    void BuildCorridor()
    {
        kit.Shell(Std, 0f);
        kit.Slab(3f, -3f, 6f, 6f, 0f, tile, .3f);
        kit.WallZ(3f, -3.08f, 6f, 0f, Std + .16f, tile);
        kit.WallZ(6f, -3.08f, 6f, 0f, Std + .16f, tile, Arches(-1.8f, -.6f, 0f, 2.1f, 1f));
        kit.WallX(-3f, 2.92f, 6.08f, 0f, Std + .16f, tile);
        kit.Lid(3f, -3f, 6f, 6f, Std, tile);
        foreach (var z in new[] { -1.5f, 1.5f, 4.5f }) Lens(new Vector3(4.5f, Std - .05f, z));
    }

    // ---------------------------------------------------------------- T_PL_BeachHall (x 0..12, z 6..21; Tall 5.4)

    void BuildBeachHall()
    {
        kit.Shell(Tall, 0f);
        // The south wall (0.6 thick, z 6.0..6.6) with the tall narrow arch: span 1.5, springing 2.10, crown 2.85, sill +-0.
        kit.WallX(6.3f, -.15f, 12.15f, -1.3f, Tall + .16f, tile, Arches(3.75f, 5.25f, 0f, 2.1f, 1f), .6f);
        // The dry lip (+-0, x 0..6, z 6.6..7.2), then a kerb of one course into the ankle sheet (C2), then the beach (C3)
        // falling north at 1:12 from -0.15 at z 9 to -1.15 at the north wall: 1.12 m of water there (Wade).
        kit.Box(0f, -1.3f, 6.6f, 6f, 0f, 7.2f, tile);
        kit.Box(0f, -1.3f, 7.2f, 3f, 0f, 9f, tile);
        kit.Box(3f, -1.3f, 7.2f, 12f, -.15f, 9f, tile);
        kit.Box(6f, -1.3f, 6.6f, 12f, -.15f, 7.2f, tile);
        kit.Shell(Tall, -1.15f);
        kit.Ramp(new Vector3(7.5f, -1.15f, 21f), Vector3.back, 9f, 12f, 1.0f, tile, .3f);
        kit.Water(3f, 6.6f, 12f, 21f, WadeSurf);
        // The terrace (A4, +1.20; x 0..3, z 9..21), its STEP RUN up from the lip (8 risers, z 6.9..9.0) with a cheek guarded
        // where it stands over 0.76 above the sheet.
        kit.Shell(Tall, Terrace);
        Block(0f, 9f, 3f, 21f, Terrace, -1.3f);
        kit.StepRun(new Vector3(1.5f, 0f, 9f), Vector3.forward, 3f, 0f, Terrace, tile, nosing);
        SlopedParapet(new Vector3(3.1f, .75f, 8.1f), new Vector3(3.1f, Terrace, 9f), -.15f);
        // The D2 arcade on the terrace's edge (x 3): piers 0.6 at z 9, 12, 15, 18, 21; round arches 2.4 (8 courses),
        // springing +3.30, crown +4.50, the three southern bays over a tiled half-wall 1.07, the fourth blind on the apse.
        kit.WallZ(3f, 8.7f, 21f, -1.3f, Tall + .16f, tile, Arches(9.3f, 11.7f, Terrace + 1.07f, 3.3f, 1f, 12.3f, 14.7f, Terrace + 1.07f, 3.3f, 1f, 15.3f, 17.7f, Terrace + 1.07f, 3.3f, 1f), .6f);
        // The E4 apse: r 3.0 on the chord x 3..9 of z 18, bulging to z 21; the cell x 9..12, z 18..21 solid tile.
        kit.Shell(Tall, -1.15f);
        kit.Ring(new Vector3(6f, 0f, 18f), 3.0f, 3.15f, -1.3f, Tall + .16f, 0f, 180f, tile, true, null, 6f);
        Block(9f, 18f, 12f, 21f, Tall, -1.3f);
        // The other walls: west (x 0) with the Casbah's walk arch from the terrace, east (x 12), the north wall of the
        // terrace with the bridge's arch, a straight back wall behind the apse.
        kit.Shell(Tall, 0f);
        kit.WallZ(0f, 6f, 21.15f, -1.3f, Tall + .16f, tile, Arches(15.0f, 16.8f, Terrace, 3.3f, 1f), .3f);
        kit.WallZ(12f, 6f, 21.15f, -1.3f, Tall + .16f, tile, null, .3f);
        kit.WallX(21f, 0f, 3.3f, -1.3f, Tall + .16f, tile, Arches(.6f, 2.4f, Terrace, 3.3f, 1f), .3f);
        kit.WallX(21f, 3.3f, 9f, -1.3f, Tall + .16f, tile, null, .3f);
        kit.Lid(0f, 6f, 12f, 21f, Tall, tile);
        // Lamps: metal-halide wall boxes on both long walls (about 3.4 over +-0) throwing up, a down-fill grid, wet niches
        // in the beach; the hall's lamps stay on in SP6 (view lamps).
        viewLamps.Add(kit.WallBox(new Vector3(12f, 3.4f, 10.5f), Vector3.left, 6f, 10f));
        viewLamps.Add(kit.WallBox(new Vector3(12f, 3.4f, 16.5f), Vector3.left, 6f, 10f));
        viewLamps.Add(kit.WallBox(new Vector3(0f, 3.4f + Terrace, 12f), Vector3.right, 6f, 10f));
        viewLamps.Add(kit.WallBox(new Vector3(0f, 3.4f + Terrace, 19.5f), Vector3.right, 6f, 10f));
        foreach (var d in new[] { new Vector2(7.5f, 10.5f), new Vector2(7.5f, 16.5f) }) viewLamps.Add(kit.DownFill(new Vector3(d.x, Tall - .07f, d.y), 2.4f, 11f));
        Niche(new Vector3(11.85f, -.6f, 15f), Vector3.left);
        Niche(new Vector3(6f, -.7f, 20.85f), Vector3.back);
    }

    // ---------------------------------------------------------------- T_PL_IslandChannel (x -6..9, z 21..36; Tall 5.4)

    void BuildIsland()
    {
        kit.Shell(Tall, WadeFloor);
        // The C4 channel, an L at Wade (floor -1.20, surface -0.03): the south arm z 21..24, the west arm x -6..-3.
        kit.Slab(-6f, 21f, 9f, 24f, WadeFloor, tile, .3f);
        kit.Slab(-6f, 24f, -3f, 36f, WadeFloor, tile, .3f);
        kit.Water(-6f, 21f, 9f, 24f, WadeSurf);
        kit.Water(-6f, 24f, -3f, 36f, WadeSurf);
        // The A7 island (+1.20, dry) and the lower deck (+-0) round it; C9's basin is cut from the deck (built with it).
        kit.Shell(Tall, Terrace);
        Block(-3f, 24f, 3f, 30f, Terrace, WadeFloor - .1f);
        kit.Shell(Tall, 0f);
        Block(6f, 24f, 9f, 36f, 0f, WadeFloor - .1f);
        Block(3f, 24f, 6f, 30f, 0f, WadeFloor - .1f);
        Block(3f, 33.6f, 6f, 36f, 0f, WadeFloor - .1f);
        Block(3f, 30f, 3.6f, 33.6f, 0f, WadeFloor - .1f);
        Block(5.4f, 30f, 6f, 33.6f, 0f, WadeFloor - .1f);
        Block(3.6f, 33f, 5.4f, 33.6f, 0f, WadeFloor - .1f);
        Block(-3f, 30f, 3f, 36f, 0f, WadeFloor - .1f);
        // Copings on the deck's water edges (deck-level: no guard).
        kit.Coping(new Vector3(3f, 0f, 24f), new Vector3(6.6f, 0f, 24f), 0f, tile);
        kit.Coping(new Vector3(7.8f, 0f, 24f), new Vector3(9f, 0f, 24f), 0f, tile);
        kit.Coping(new Vector3(-3f, 0f, 30f), new Vector3(-3f, 0f, 34.2f), 0f, tile);
        kit.Coping(new Vector3(-3f, 0f, 35.4f), new Vector3(-3f, 0f, 36f), 0f, tile);
        // The A6 bridge from the terrace's north end over the south arm (x 0.6..2.4, +1.20, 0.30 slab, tiled parapets).
        kit.Shell(Tall, Terrace);
        kit.Box(.6f, Terrace - .3f, 21f, 2.4f, Terrace, 24f, tile);
        Parapet(.7f, 21f, .7f, 24f, Terrace);
        Parapet(2.3f, 21f, 2.3f, 24f, Terrace);
        // The island's tiled half-walls on all four sides, open only where the bridge and the step run arrive.
        Parapet(-2.9f, 24.1f, .6f, 24.1f, Terrace);
        Parapet(2.4f, 24.1f, 2.9f, 24.1f, Terrace);
        Parapet(-2.9f, 24.1f, -2.9f, 29.9f, Terrace);
        Parapet(2.9f, 24.1f, 2.9f, 25.5f, Terrace);
        Parapet(2.9f, 27.3f, 2.9f, 29.9f, Terrace);
        Parapet(-2.9f, 29.9f, .6f, 29.9f, Terrace);
        Parapet(2.4f, 29.9f, 2.9f, 29.9f, Terrace);
        // The second way onto the island (R6): a step run of 8 risers rising south to its edge, its cheeks guarded.
        kit.Shell(Tall, 0f);
        kit.StepRun(new Vector3(1.5f, 0f, 30f), Vector3.back, 1.8f, 0f, Terrace, tile, nosing);
        SlopedParapet(new Vector3(.5f, .75f, 30.9f), new Vector3(.5f, Terrace, 30f), 0f);
        SlopedParapet(new Vector3(2.5f, .75f, 30.9f), new Vector3(2.5f, Terrace, 30f), 0f);
        // The way from the island down to the weirs (70 section 8: the weirs at the island's foot): a step run of 8 risers east
        // onto the lower deck by C9 (x 3..5.4, z 25.5..27.3), its cheeks guarded, toward the walk arch on x 9. Without it the
        // deck east of C9, the weirs, the dome hall and the grotto were reached only by wading between the stones' 0.6 m gaps,
        // which the player's capsule (0.66 with its skin) cannot (walk test 11-04, walk7).
        kit.Shell(Tall, 0f);
        kit.StepRun(new Vector3(3f, 0f, 26.4f), Vector3.left, 1.8f, 0f, Terrace, tile, nosing);
        SlopedParapet(new Vector3(3.9f, .75f, 25.4f), new Vector3(3f, Terrace, 25.4f), 0f);
        SlopedParapet(new Vector3(3.9f, .75f, 27.4f), new Vector3(3f, Terrace, 27.4f), 0f);
        // B8 stepping stones: ten tiled drums 0.9 across, tops 3 cm proud of the water, 1.2 m on centres along x -4.5, so 0.30
        // clear between them (0.6 across left 0.60 clear, the player's own width: no crossing without a jump; walk test 11-04).
        for (var k = 0; k < 9; k++) kit.Ring(new Vector3(-4.5f, 0f, 21.6f + 1.2f * k), 0f, .45f, WadeFloor, 0f, -180f, 180f, tile, true, null, 15f);
        kit.Ring(new Vector3(-3.6f, 0f, 32.0f), 0f, .45f, WadeFloor, 0f, -180f, 180f, tile, true, null, 15f);
        // Two STEPS INTO WATER of 8 risers from the deck's copings, west into the west arm and south into the south arm.
        kit.Shell(Tall, WadeFloor);
        kit.StepRun(new Vector3(-3f, 0f, 34.8f), Vector3.right, 1.2f, WadeFloor, 0f, tile, nosing);
        kit.StepRun(new Vector3(7.2f, 0f, 24f), Vector3.forward, 1.2f, WadeFloor, 0f, tile, nosing);
        // Walls: west x -6, north z 36 with the sight arch into the dome hall (span 2.4 on x 0..3, sill 1.07 over the deck,
        // springing 2.10, crown 3.30), east x 9 (built with the weir room), the Casbah's north wall carries the south arm's west end.
        kit.Shell(Tall, 0f);
        kit.WallZ(-6f, 21f, 36.15f, WadeFloor - .1f, Tall + .16f, tile, null, .3f);
        kit.WallX(36f, -6.15f, 9f, DeepFloor - .1f, Tall + .16f, tile, Arches(.3f, 2.7f, 1.07f, 2.1f, 1f), .3f);
        kit.Lid(-6f, 21f, 9f, 36f, Tall, tile);
        // Lamps: wall boxes on the long walls, a down-fill grid, wet niches in the channel.
        kit.WallBox(new Vector3(-6f, 3.4f, 27f), Vector3.right, 6f, 10f, false);
        kit.WallBox(new Vector3(-6f, 3.4f, 33f), Vector3.right, 6f, 10f, false);
        kit.WallBox(new Vector3(9f, 3.4f, 30f), Vector3.left, 6f, 10f, false);
        foreach (var d in new[] { new Vector2(0f, 27f), new Vector2(6f, 33f), new Vector2(-3f, 33f) }) kit.DownFill(new Vector3(d.x, Tall - .07f, d.y), 2.2f, 11f, false);
        Niche(new Vector3(-5.85f, -.6f, 28f), Vector3.right);
        Niche(new Vector3(-1f, -.6f, 21.2f), Vector3.forward);
        Niche(new Vector3(5f, -.6f, 23.85f), Vector3.back);
    }

    // ---------------------------------------------------------------- C9 wet block on the lower deck (x 3..6, z 30..36)

    void BuildWetBlock()
    {
        // A tiled solid 5.0 tall (0.4 under the lid, so it reads freestanding); inside Standard 2.9, a cold plunge only
        // (whirlpool and sauna stay deferred, pool D10): basin 1.8 x 2.4 at Wade with 8 tiled steps in; a round walk arch
        // (span 1.2, springing 2.10, crown 2.70) in its west face at z 33.0..34.2.
        kit.Shell(Std, 0f);
        kit.WallX(30f, 3f, 6f, 0f, 5f, tile, null, .3f);
        kit.WallX(36f - .3f, 3f, 6f, 0f, 5f, tile, null, .3f);
        kit.WallZ(6f - .15f, 30f, 35.85f, 0f, 5f, tile, null, .3f);
        kit.WallZ(3f + .15f, 30f, 35.85f, 0f, 5f, tile, Arches(33.0f, 34.2f, 0f, 2.1f, 1f), .3f);
        kit.Lid(3f, 30f, 6f, 35.85f, Std, tile);
        kit.Lid(3f, 30f, 6f, 35.85f, 5f - .16f, tile);
        kit.Shell(Std, WadeFloor);
        kit.Basin(3.6f, 30.6f, 5.4f, 33f, WadeFloor, 0f, tile, false);
        kit.Slab(3.6f, 30f, 5.4f, 30.6f, 0f, tile, 1.3f);
        kit.Water(3.6f, 30.6f, 5.4f, 33f, WadeSurf);
        kit.StepRun(new Vector3(4.5f, 0f, 33f), Vector3.forward, 1.2f, WadeFloor, 0f, tile, nosing);
        // One vapour-tight globe (F7) and one wet-niche lamp (F6).
        kit.Lamp("vapour-tight globe (F7)", LightType.Point, new Vector3(4.5f, Std - .25f, 34.5f), Vector3.down, 0f, 0f, 1.6f, 5f, new Color(.95f, .98f, 1f), false,
            new Vector3(.22f, .22f, .22f), FrontRoomsPoolMaterials.Lit("globe glass", new Color(.9f, .92f, .92f), .8f), new Color(1.6f, 1.65f, 1.7f));
        Niche(new Vector3(5.38f, -.6f, 31.8f), Vector3.left);
    }

    // ---------------------------------------------------------------- T_PL_WeirTerraces (x 9..21, z 24..42; Tall, lid +5.40)

    void BuildWeirs()
    {
        // Four A4 terraces falling north: T0 +-0 (z 24..27), T1 -0.60 (z 27..33), T2 -1.20 (z 33..36), T3 -1.80 (z 36..42).
        // A dry walk on x 9..12; over x 12..21 T0-T2 are ankle sheets (floor 0.15 under the terrace, surface 0.03 under)
        // spilling over three 9 m tiled weirs of 0.60 (W1-W3, z 27, 33, 36); a basin at Wade below W3.
        float[] z0 = { 24f, 27f, 33f, 36f };
        float[] z1 = { 27f, 33f, 36f, 42f };
        float[] t = { 0f, -.6f, -1.2f, -1.8f };
        for (var k = 0; k < 4; k++)
        {
            kit.Shell(Tall, t[k]);
            Block(9f, z0[k], 12f, z1[k], t[k], DeepFloor - .2f);
            if (k < 3)
            {
                Block(12f, z0[k], 21f, z1[k], t[k] - .15f, DeepFloor - .2f);
                kit.Water(12f, z0[k], 21f, z1[k], t[k] - .03f);
            }
        }
        // The weirs: tiled lips with their crest at the upper sheet's surface, a falling sheet to the next water; the east
        // wall's second step runs pass through W1 and W2 (the lip has a gap there).
        for (var k = 0; k < 3; k++)
        {
            var z = z1[k];
            // The lip 2 cm under the water's surface: a film runs over it (its top was coplanar with the water; review SP6-F8).
            var surface = t[k] - .03f;
            var crest = t[k] - .05f;
            var below = k < 2 ? t[k + 1] - .03f : DeepSurf;
            kit.Shell(Tall, below);
            if (k < 2)
            {
                kit.Box(12f, crest - .3f, z - .15f, 19.6f, crest, z, tile);
                kit.Box(20.8f, crest - .3f, z - .15f, 21f, crest, z, tile);
                kit.Box(12f, crest - .025f, z - .05f, 19.6f, crest + .003f, z + .002f, nosing, false);
                kit.Box(20.8f, crest - .025f, z - .05f, 21f, crest + .003f, z + .002f, nosing, false);
                Fall(new Vector3(12.05f, 0f, z + .01f), new Vector3(19.55f, 0f, z + .01f), surface, below);
                Fall(new Vector3(20.85f, 0f, z + .01f), new Vector3(20.95f, 0f, z + .01f), surface, below);
            }
            else
            {
                kit.Box(12f, crest - .3f, z - .15f, 21f, crest, z, tile);
                kit.Box(12f, crest - .025f, z - .05f, 21f, crest + .003f, z + .002f, nosing, false);
                Fall(new Vector3(12.05f, 0f, z + .01f), new Vector3(20.95f, 0f, z + .01f), surface, below);
            }
            // A light over each weir, 1 m downstream, aimed back at its face (R8: lit lips).
            kit.Lamp("weir light", LightType.Spot, new Vector3(16.5f, Tall - .07f, z + 1f), new Vector3(0f, -1f, -.35f), 150f, 110f, 2.0f, 12f, new Color(.92f, .98f, 1f), false,
                new Vector3(.45f, .45f, .12f), FrontRoomsPoolMaterials.Steel, new Color(1.4f, 1.5f, 1.55f));
        }
        // STEP RUNs of 4 risers in the dry walk at every weir, and through the sheet against the east wall at W1 and W2.
        for (var k = 0; k < 3; k++)
        {
            kit.Shell(Tall, t[k + 1]);
            kit.StepRun(new Vector3(10.5f, 0f, z1[k]), Vector3.back, 1.8f, t[k + 1], t[k], tile, nosing);
            if (k < 2) kit.StepRun(new Vector3(20.2f, 0f, z1[k]), Vector3.back, 1.2f, t[k + 1] - .15f, t[k] - .15f, tile, nosing);
        }
        // The top sheet fed from a slot under the south wall (composed).
        kit.Shell(Tall, 0f);
        kit.WallX(24f, 9f, 21.15f, DeepFloor - .2f, Tall + .16f, tile, Arches(13f, 20f, -.15f, .05f, 0f), .3f);
        kit.Box(13f, -.4f, 23.4f, 20f, -.15f, 23.85f, FrontRoomsPoolMaterials.Surface("Plenum_Dark", new Color(.03f, .03f, .03f)), false);
        // The basin below W3 (x 12..21, z 36..42): floor -3.00, surface -1.83, coping flush with T3, two STEPS INTO WATER.
        kit.Shell(Tall, DeepFloor);
        kit.Slab(12f, 36f, 21f, 42f, DeepFloor, tile, .25f);
        kit.Water(12f, 36f, 21f, 42f, DeepSurf);
        kit.StepRun(new Vector3(12f, 0f, 37.2f), Vector3.left, 1.2f, DeepFloor, -1.8f, tile, nosing);
        kit.StepRun(new Vector3(12f, 0f, 40.8f), Vector3.left, 1.2f, DeepFloor, -1.8f, tile, nosing);
        kit.Coping(new Vector3(12f, 0f, 36f), new Vector3(12f, 0f, 36.6f), -1.8f, tile);
        kit.Coping(new Vector3(12f, 0f, 37.8f), new Vector3(12f, 0f, 40.2f), -1.8f, tile);
        kit.Coping(new Vector3(12f, 0f, 41.4f), new Vector3(12f, 0f, 42f), -1.8f, tile);
        Niche(new Vector3(20.85f, -2.4f, 39f), Vector3.left);
        Niche(new Vector3(16.5f, -2.4f, 41.85f), Vector3.back);
        Niche(new Vector3(14f, -2.4f, 41.85f), Vector3.back);
        // Walls: x 9 (z 24..42) with the walk arch from the island's deck (z 24.3..26.7, springing 2.10) and the arch from
        // T3 into the dome hall (z 39.6..41.4, springing 2.10 over -1.80, crown 3.0 over it); x 21; z 42. Lid +5.40.
        kit.Shell(Tall, 0f);
        kit.WallZ(9f, 21f, 42.15f, DeepFloor - .2f, Tall + .16f, tile, Arches(24.3f, 26.7f, 0f, 2.1f, 1f, 39.6f, 41.4f, -1.8f, .3f, 1f), .3f);
        kit.WallZ(21f, 24f, 42.15f, DeepFloor - .2f, Tall + .16f, tile, null, .3f);
        kit.WallX(42f, 9f, 21.15f, DeepFloor - .2f, Tall + .16f, tile, null, .3f);
        kit.Lid(9f, 24f, 21f, 42f, Tall, tile);
        // Wall boxes on both long walls, about 3.4 m over the terrace they stand over, and a fill over the dry walk at each
        // step run (70 section 2; review SP6-F9).
        kit.WallBox(new Vector3(21f, 3.4f, 28.5f), Vector3.left, 6f, 10f, false);
        kit.WallBox(new Vector3(21f, 2.4f, 37.5f), Vector3.left, 6f, 10f, false);
        kit.WallBox(new Vector3(21f, 2.8f, 33f), Vector3.left, 6f, 10f, false);
        kit.WallBox(new Vector3(9f, 2.8f, 30f), Vector3.right, 6f, 10f, false);
        kit.WallBox(new Vector3(9f, 2.2f, 36f), Vector3.right, 6f, 10f, false);
        foreach (var z in new[] { 27f, 33f, 36f }) kit.DownFill(new Vector3(10.5f, Tall - .07f, z), 2.0f, 12f, false);
    }

    // ---------------------------------------------------------------- T_PL_DomeGallery (x -3..9, z 36..48): deck -1.80, lid +3.60

    void BuildDomeHall()
    {
        kit.Shell(DomeLid, DomeDeck);
        // Deck cells and basin cells (C1, Wade: surface -1.83, floor -3.00).
        Block(-3f, 36f, 0f, 45f, DomeDeck, DeepFloor - .1f);
        Block(0f, 36f, 3f, 39f, DomeDeck, DeepFloor - .1f);
        Block(3f, 36f, 9f, 42f, DomeDeck, DeepFloor - .1f);
        Block(6f, 42f, 9f, 48f, DomeDeck, DeepFloor - .1f);
        kit.Slab(0f, 39f, 3f, 48f, DeepFloor, tile, .25f);
        kit.Slab(3f, 42f, 6f, 48f, DeepFloor, tile, .25f);
        kit.Slab(-3f, 45f, 0f, 48f, DeepFloor, tile, .25f);
        kit.Water(0f, 39f, 3f, 48f, DeepSurf);
        kit.Water(3f, 42f, 6f, 48f, DeepSurf);
        kit.Water(-3f, 45f, 0f, 48f, DeepSurf);
        // C7: a stair into the water on the basin's south edge (x 0..3, 8 risers), from the deck at the B2's foot, pointing
        // north at the grotto's mouth; tiled cheeks, no rail, no gate. A second set of 8 from the walk beside the B2.
        kit.Shell(DomeLid, DeepFloor);
        kit.StepRun(new Vector3(1.5f, 0f, 39f), Vector3.back, 3f, DeepFloor, DomeDeck, tile, nosing);
        kit.StepRun(new Vector3(-.9f, 0f, 45f), Vector3.back, 1.2f, DeepFloor, DomeDeck, tile, nosing);
        kit.Shell(DomeLid, DomeDeck);
        kit.Coping(new Vector3(0f, 0f, 41.1f), new Vector3(0f, 0f, 45f), DomeDeck, tile);
        kit.Coping(new Vector3(3f, 0f, 42f), new Vector3(6f, 0f, 42f), DomeDeck, tile);
        kit.Coping(new Vector3(6f, 0f, 42f), new Vector3(6f, 0f, 48f), DomeDeck, tile);
        kit.Coping(new Vector3(-3f, 0f, 45f), new Vector3(-1.5f, 0f, 45f), DomeDeck, tile);
        // The A5 gallery (+1.20, an L; slab 0.20; 2.40 clear under the lid's edge), tiled parapets on every open edge.
        kit.Shell(DomeLid, Terrace);
        kit.Slab(3f, 42f, 9f, 45f, Terrace, tile, .2f);
        kit.Slab(-3f, 45f, 9f, 48f, Terrace, tile, .2f);
        Parapet(3.1f, 42.1f, 3.1f, 45.1f, Terrace);
        Parapet(-1.8f, 45.1f, 3.1f, 45.1f, Terrace);
        Parapet(3f, 42.1f, 6.8f, 42.1f, Terrace);
        Parapet(8.1f, 42.1f, 8.9f, 42.1f, Terrace);
        // The B2 (the second way, R3, R6; the Relay's): down the west wall from the gallery's far end, x -3.0..-1.8, 20 risers
        // of 0.15 with a lit landing at -0.30 after 10 (z 42.3..41.1), its foot at z 38.4; a cheek wall on the open side.
        kit.Shell(DomeLid, DomeDeck);
        kit.Flight(new Vector3(-2.4f, DomeDeck, 38.4f), Vector3.forward, 1.2f, 10, .15f, .3f, tile, nosing, true);
        Block(-3f, 41.1f, -1.8f, 42.3f, -.3f, DomeDeck);
        kit.Flight(new Vector3(-2.4f, -.3f, 42.3f), Vector3.forward, 1.2f, 10, .15f, .3f, tile, nosing, true);
        kit.Box(-3f, DomeDeck, 42.3f, -1.8f, -.3f, 45f, tile);
        SlopedParapet(new Vector3(-1.7f, DomeDeck + .15f, 38.4f), new Vector3(-1.7f, -.3f, 41.1f), DomeDeck);
        kit.Box(-1.8f, DomeDeck, 41.1f, -1.6f, -.3f + FrontRoomsStructuresKit.GuardHeight, 42.3f, tile);
        SlopedParapet(new Vector3(-1.7f, -.15f, 42.3f), new Vector3(-1.7f, Terrace, 45f), DomeDeck);
        // The Relay stand-in at the B2's head on the gallery's far end, facing south.
        kit.Relay(new Vector3(-2.4f, Terrace, 46.2f), new Vector3(-2.4f, Terrace, 40f));
        // Walls: south z 36 (the island room's, with the sight arch), west x -3, north z 48 with the grotto's mouth, east x 9
        // (z 42..48; z 36..42 is the weir room's). The flat lid at +3.60 outside the dome's circle is built with the dome.
        kit.WallZ(-3f, 36f, 48.15f, DeepFloor - .1f, DomeLid + .2f, tile, null, .3f);
        kit.WallX(48f, -3.15f, 9.15f, DeepFloor - .1f, DomeLid + .2f, tile, Arches(-1.5f, 1.5f, DeepFloor, DeepFloor + 2.4f, 0f), .3f);
        kit.WallZ(9f, 42f, 48f, DeepFloor - .1f, DomeLid + .2f, tile, null, .3f);
        // Lamps: wall boxes on the south and west walls at +2.00 under the springing (the dome the brightest surface after the
        // oculus), two downlights in the gallery's soffit over the water, three wet niches in the basin.
        // Tilted 35 degrees off the wall toward the dome's centre, so the shell, not the first metre of wall, takes the light
        // (review SP6-F6; by eye: intensity 9, range 11).
        kit.WallBox(new Vector3(4.5f, 2.0f, 36.15f), Vector3.forward, 9f, 11f, false, DomeAim(new Vector3(4.5f, 0f, 36.45f)), 100f, 60f);
        kit.WallBox(new Vector3(-2.85f, 2.0f, 40.5f), Vector3.right, 9f, 11f, false, DomeAim(new Vector3(-2.55f, 0f, 40.5f)), 100f, 60f);
        kit.Lamp("gallery soffit downlight", LightType.Spot, new Vector3(1.5f, Terrace - .25f, 46.5f), Vector3.down, 110f, 70f, 1.6f, 6f, new Color(.92f, .98f, 1f), false, new Vector3(.25f, .25f, .05f), FrontRoomsPoolMaterials.Steel, new Color(1.4f, 1.5f, 1.55f));
        kit.Lamp("gallery soffit downlight", LightType.Spot, new Vector3(4.5f, Terrace - .25f, 45f), Vector3.down, 110f, 70f, 1.6f, 6f, new Color(.92f, .98f, 1f), false, new Vector3(.25f, .25f, .05f), FrontRoomsPoolMaterials.Steel, new Color(1.4f, 1.5f, 1.55f));
        Niche(new Vector3(.15f, -2.4f, 43f), Vector3.right);
        Niche(new Vector3(2.85f, -2.4f, 46f), Vector3.left);
        Niche(new Vector3(4.5f, -2.4f, 47.85f), Vector3.back);
        // SP6-F7: the B2 is the Relay's way and lit as a way (rule 12): step lights in the west wall every third riser (as the
        // B4-M) and a low wall light over its landing.
        for (var f = 0; f < 2; f++)
            for (var k = 2; k <= 8; k += 3)
            {
                var y0 = f == 0 ? DomeDeck : -.3f;
                var z0 = f == 0 ? 38.4f : 42.3f;
                kit.Lamp("B2 step light", LightType.Spot, new Vector3(-2.83f, y0 + k * .15f + .26f, z0 + (k - .5f) * .3f), new Vector3(.6f, -1f, 0f), 110f, 70f, .45f, 1.6f, new Color(.90f, .96f, 1f), false,
                    new Vector3(.22f, .07f, .03f), FrontRoomsPoolMaterials.Steel, new Color(1.6f, 1.7f, 1.75f));
            }
        kit.Lamp("B2 landing light", LightType.Spot, new Vector3(-2.83f, .2f, 41.7f), new Vector3(.5f, -1f, 0f), 120f, 80f, .8f, 2.5f, new Color(.90f, .96f, 1f), false,
            new Vector3(.3f, .1f, .04f), FrontRoomsPoolMaterials.Steel, new Color(1.6f, 1.7f, 1.75f));
    }

    // The aim of a dome-hall wall box: 35 degrees off vertical toward the dome's centre (3, 42).
    static Vector3 DomeAim(Vector3 from)
    {
        var flat = new Vector3(3f - from.x, 0f, 42f - from.z).normalized;
        return flat * Mathf.Sin(35f * Mathf.Deg2Rad) + Vector3.up * Mathf.Cos(35f * Mathf.Deg2Rad);
    }

    // ---------------------------------------------------------------- the B4-M (60 2.2, 5): module x 3..9, z 36..42, axis (6, 39)

    void BuildB4M()
    {
        // r_in 0.80, width 1.30, tiled solid ribbons 0.08 (4.36 outside); 18 risers of 0.167 to +1.20 in 17 treads of 14.58
        // degrees (248), anticlockwise from the first riser at 112 degrees to the last on the east radius; no mid landing.
        var c = new Vector3(6f, 0f, 39f);
        kit.Shell(DomeLid, DomeDeck);
        kit.Spiral(c, .8f, 2.1f, 112f, 14.58f, 18, 3.0f / 18f, DomeDeck, tile, nosing, .2f, -1, 0f, .08f, .08f, tile, FrontRoomsStructuresKit.GuardHeight);
        // The top landing north along the tangent (x 6.8..8.1, z 39..42) onto the gallery; parapets on its open sides.
        kit.Shell(DomeLid, Terrace);
        kit.Slab(6.8f, 39f, 8.1f, 42f, Terrace, tile, .2f);
        Parapet(6.75f, 39.1f, 6.75f, 42.1f, Terrace);
        Parapet(8.15f, 39f, 8.15f, 42.1f, Terrace);
        // A cool step light in the inner ribbon every third tread; an uplight in the eye washing the soffit.
        var a = 112f;
        for (var k = 0; k < 17; k++)
        {
            if (k % 3 == 2)
            {
                var mid = (a + 14.58f * .5f) * Mathf.Deg2Rad;
                var radial = new Vector3(Mathf.Cos(mid), 0f, Mathf.Sin(mid));
                var p = c + radial * .86f;
                p.y = DomeDeck + (k + 1) * (3.0f / 18f) + .26f;
                kit.Lamp("B4-M step light", LightType.Spot, p, (radial * .6f + Vector3.down).normalized, 110f, 70f, .45f, 1.6f, new Color(.90f, .96f, 1f), false,
                    new Vector3(.22f, .07f, .03f), FrontRoomsPoolMaterials.Steel, new Color(1.6f, 1.7f, 1.75f));
            }
            a += 14.58f;
        }
        kit.Lamp("B4-M eye uplight", LightType.Spot, new Vector3(6f, DomeDeck + .04f, 39f), Vector3.up, 70f, 50f, 1.8f, 6f, new Color(.92f, .98f, 1f), false,
            new Vector3(.2f, .2f, .05f), FrontRoomsPoolMaterials.Steel, new Color(1.5f, 1.6f, 1.65f));
        // Never the Relay's way (Red 10-31 ~16:15).
        kit.RelayBlocker("B4-M foot", new Vector3(4.6f, DomeDeck, 39.4f), new Vector3(6.1f, DomeDeck + 2.6f, 41.2f));
        kit.RelayBlocker("B4-M top", new Vector3(6.8f, Terrace, 40.2f), new Vector3(8.1f, Terrace + 2.6f, 42f));
    }

    // ---------------------------------------------------------------- the D4 saucer dome and its oculus

    void BuildDome()
    {
        // A saucer over the whole hall: a circle of 12.0 centred (3, 42), springing at the lid +3.60, crown +6.00 (sphere r 8.7),
        // tile courses round it; the flat lid outside the circle; an oculus 1.2 across at (1.5, 43.5) in a tiled shaft to a
        // lamp housing at about +7.0 whose lamp no reachable eye point sees: one steady metal-halide downlight.
        const float R = 8.7f, rim = 6f;
        var centre = new Vector3(3f, 6.0f - R, 42f);
        var oculus = new Vector2(1.5f, 43.5f);
        kit.Shell(6f, DomeDeck);
        kit.SlabRoundHole(new Vector3(3f, 0f, 42f), 6f, rim, DomeLid + .16f, .16f, tile);
        const int rings = 12, segs = 64;
        for (var i = 0; i < rings; i++)
        {
            // The crown ring starts 2 mm off the apex (render9, 11-04): from the apex itself its quads had two corners on one
            // point, so their normal was zero and their winding undecided, and the crown showed a hole r 0.5 m in A and B.
            var r0 = i == 0 ? .002f : rim * i / rings;
            var r1 = rim * (i + 1) / rings;
            var y0 = centre.y + Mathf.Sqrt(R * R - r0 * r0);
            var y1 = centre.y + Mathf.Sqrt(R * R - r1 * r1);
            var v0 = R * Mathf.Asin(Mathf.Clamp(r0 / R, 0f, 1f));
            var v1 = R * Mathf.Asin(Mathf.Clamp(r1 / R, 0f, 1f));
            for (var s = 0; s < segs; s++)
            {
                var t0 = 2f * Mathf.PI * s / segs;
                var t1 = 2f * Mathf.PI * (s + 1) / segs;
                var a = new Vector3(3f + r0 * Mathf.Cos(t0), y0, 42f + r0 * Mathf.Sin(t0));
                var b = new Vector3(3f + r0 * Mathf.Cos(t1), y0, 42f + r0 * Mathf.Sin(t1));
                var cc = new Vector3(3f + r1 * Mathf.Cos(t1), y1, 42f + r1 * Mathf.Sin(t1));
                var d = new Vector3(3f + r1 * Mathf.Cos(t0), y1, 42f + r1 * Mathf.Sin(t0));
                var mid = (a + b + cc + d) * .25f;
                if (Vector2.Distance(new Vector2(mid.x, mid.z), oculus) < .62f) continue;
                var into = (centre - mid).normalized;
                kit.Quad(a, b, cc, d, into, new Vector2(t0 * r0, v0), new Vector2(t1 * r0, v0), new Vector2(t1 * r1, v1), new Vector2(t0 * r1, v1), tile);
            }
        }
        var rho = Vector2.Distance(oculus, new Vector2(3f, 42f));
        var yo = centre.y + Mathf.Sqrt(R * R - rho * rho);
        kit.Ring(new Vector3(oculus.x, 0f, oculus.y), .6f, .75f, yo - .2f, 7.2f, -180f, 180f, tile, false, null, 15f);
        kit.Lid(oculus.x - .8f, oculus.y - .8f, oculus.x + .8f, oculus.y + .8f, 7.2f, tile, .1f, false);
        var l = kit.Lamp("oculus (metal-halide downlight)", LightType.Spot, new Vector3(oculus.x, 7.1f, oculus.y), Vector3.down, 21f, 18f, 120f, 14f, new Color(.95f, .98f, 1f), true, Vector3.zero, null, Color.black);
        viewLamps.Add(l);
    }

    // ---------------------------------------------------------------- the dome hall's light: A today's, B (L; S1 C)

    // S1, the pool light (Red 11-04 ~14:1x, "每个都按推荐来做" on C: "先只在一个区试 B（比如 SP6 的穹顶厅），你走过、和 A 对比之后再定").
    // A is today's: the whole room porcelain white, two wall boxes throwing up, the dome the brightest surface after the oculus.
    // B, in Red's words: "地面和墙下部保持瓷白，天花和墙上部压暗（或者让上部的墙处在阴影里），灯做成更小、更亮的点，这样水会显得更深，
    // 闪光也更明显": from about 2 m out still water shows the colour of the ceiling and the upper walls, and the lamps as its
    // sparkle (bathroom_rooms/17 section 10.1, 10.6 item 2). B changes the dome hall only (x -3..9, z 36..48):
    //  - The vault in shadow, by light. Scene 4's pool trilight stands in for the halls' bounce and its ground term lights every
    //    surface that faces down, so the dome takes more of its light from the ambient than from its two boxes: putting the
    //    boxes out alone would leave it near white. With no lamp throwing up in B, the vault gets a share of the room's own
    //    ambient: per-renderer SH = the probe the room has now (the scene's trilight, or 游戏视觉's zone look's blend when Q has
    //    it on, each by its own code) times a shade. The dome, its oculus shaft and the flat lid's corners (BuildDome's own
    //    renderer group) VaultShade; the gallery's soffits (a liner 6 mm under them) SoffitShade; the walls' top third (twelve
    //    liners 6 mm proud of the four faces, one per 0.15 m course from +1.80 to the springing at +3.60, so every step sits on
    //    a joint) from .98 down to .34, so the shadow comes down the walls and meets the vault. Same tile, same porcelain white:
    //    no finish changes, only light.
    //  - Lamps thrown down only (every cone's upper edge at least 5 degrees under its lamp's horizon), small round lenses driven
    //    about 30 times the lit tile (17 section 10.1 cue 3: glints 10-40 times their floor). The two up-throwing wall boxes and
    //    the two soffit downlights are put out and hidden; in their place two compact wall spots at the boxes' places thrown
    //    down onto the deck (the C7's head, the walk by the B2), two compact soffit downlights, two compact wall spots under the
    //    gallery (the east wall over the deck, the north wall over the grotto's mouth), two recessed downlights in the flat lid
    //    over the gallery's far corners (range 4 m: the gallery only; the Relay's end lit) and a lamp in the oculus shaft under
    //    a diffuser, so the oculus is a bright disc in the dark vault. Nine lamps through kit.Lamp (the scene's budget and its
    //    shadow filter), switched by Look.Scale; steady, nothing flickers, nothing red. No lamp of B reaches another room but
    //    through an opening (checked on the plan; the south spot keeps its shadow on for that). In A the two up-throwing boxes
    //    leak a little through the south wall onto the island room's lid by that wall (no shadows, as every A lamp); in B they
    //    are out, so that leak goes with them.
    // The wet niches, the B2's and the B4-M's lights, the eye uplight and the oculus lamp are the same in A and B. The dome
    // hall's own probe is rendered again after each switch; the other halls keep theirs. Until L is pressed nothing is drawn or
    // lit that A did not have: every piece of B is hidden or at scale 0, and the vault renderer is not touched.

    void BuildDomeLightB()
    {
        try
        {
            BuildDomeB();
            domeReady = true;
        }
        finally
        {
            // A, whatever happened above: no piece of B may stay lit or drawn
            SetDomeB(false, true);
        }
    }

    void BuildDomeB()
    {
        var look = kit.Host != null ? kit.Host.Look : null;
        if (look == null || Root == null) throw new InvalidOperationException("no look or root");

        // A's four lamps that B puts out, found where BuildDomeHall made them (that code is unchanged)
        foreach (var l in kit.Lights)
        {
            var lamp = l != null ? l.transform.parent : null;
            if (lamp == null || !InDomeHall(lamp.localPosition)) continue;
            if (lamp.name != "Lamp · metal-halide wall box" && lamp.name != "Lamp · gallery soffit downlight") continue;
            domeA.Add(l);
            domeHideB.AddRange(lamp.GetComponentsInChildren<Renderer>(true));
        }
        if (domeA.Count != 4) FrontRoomsPoolMaterials.LogOnce("SP6 dome A lamps", "SP6 dome hall light B: " + domeA.Count + " of A's 4 lamps found (two wall boxes, two soffit downlights).");

        // The vault: the renderer group BuildDome's pieces went into (tile at ceiling +6.0; nothing else in SP6 starts over +3.55).
        foreach (var r in Root.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (r == null || r.sharedMaterial != tile) continue;
            var b = r.bounds;
            if (b.min.y > Site.y + DomeLid - .05f && b.Contains(Site + new Vector3(3f, 5f, 42f))) AddShade(r, VaultShade, true);
        }
        if (domeShades.Count != 1) FrontRoomsPoolMaterials.LogOnce("SP6 dome vault", "SP6 dome hall light B: " + domeShades.Count + " vault renderers found (1 expected).");
        foreach (var p in Root.GetComponentsInChildren<ReflectionProbe>(true))
            if (p != null && (p.transform.position - (Site + DomeProbeCentre)).sqrMagnitude < .01f) domeProbe = p;

        // B's lamps. Their GameObjects stay active (the water preview gathers a room's lights from active ones); the budget
        // switches each light by its scale. Order: the boxes' places (south, west), the soffit, under the gallery (east,
        // north), the flat lid's far corners, the oculus.
        var lamps = kit.BeginPart("SP6 dome hall light B · lamps");
        // The south spot's cone, heading west of north from 0.27 m off the island room's wall, dips behind that wall plane at
        // its west side (up to E .11 on the island's deck by the wall, measured on the plan): it always casts its shadow
        // while on, as SP5's sodium heads (Look.Shadowed), so the wall keeps it in the dome hall.
        look.Shadowed(WallSpotB(new Vector3(4.5f, 2f, 36.4f), new Vector3(1.8f, DomeDeck, 38.2f), Vector3.forward, 76f, 40f, 7f, 9f));
        WallSpotB(new Vector3(-2.6f, 2f, 40.5f), new Vector3(-.6f, DomeDeck, 41.5f), Vector3.right, 80f, 44f, 6f, 9f);
        DownB("compact soffit downlight (B)", new Vector3(1.5f, .99f, 46.5f), 120f, 64f, 1.8f, 6.5f, new Vector3(.12f, .12f, .06f), SoffitLens, .1f);
        DownB("compact soffit downlight (B)", new Vector3(4.5f, .99f, 45f), 120f, 64f, 1.8f, 6.5f, new Vector3(.12f, .12f, .06f), SoffitLens, .1f);
        WallSpotB(new Vector3(8.6f, .5f, 45f), new Vector3(6.3f, DomeDeck, 44.6f), Vector3.left, 78f, 42f, 3f, 7f);
        WallSpotB(new Vector3(0f, .5f, 47.6f), new Vector3(0f, DeepSurf, 45.2f), Vector3.back, 78f, 42f, 2.6f, 7f);
        DownB("recessed downlight (B)", new Vector3(7.8f, DomeLid - .016f, 46.8f), 90f, 50f, 1.3f, 4f, new Vector3(.15f, .15f, .04f), CornerLens, .12f);
        DownB("recessed downlight (B)", new Vector3(-1.8f, DomeLid - .016f, 46.8f), 90f, 50f, 1.3f, 4f, new Vector3(.15f, .15f, .04f), CornerLens, .12f);
        // The oculus: a lamp in the shaft lights its walls from the top (a lightwell fading into the vault), and a diffuser
        // under the lamp housing's lid closes it: looked up into, the oculus is a bright disc.
        AddLampB(kit.Lamp("oculus lightwell (B)", LightType.Point, new Vector3(Oculus.x, 6.9f, Oculus.y), Vector3.down, 0f, 0f, 1.1f, 2.1f, MetalHalide, true, Vector3.zero, null, Color.black), 0f);
        var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = "Oculus diffuser (B)";
        UnityEngine.Object.Destroy(disc.GetComponent<Collider>());
        disc.transform.SetParent(lamps, false);
        disc.transform.localPosition = new Vector3(Oculus.x, 7.192f, Oculus.y);
        disc.transform.localScale = new Vector3(1.2f, .002f, 1.2f);
        var dr = disc.GetComponent<MeshRenderer>();
        dr.sharedMaterial = FrontRoomsPoolMaterials.Glow("oculus diffuser (B)", OculusDiffuser);
        dr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        domeShowB.Add(dr);

        // The wall spots' arms, wall to housing (render only).
        var arms = kit.BeginPart("SP6 dome hall light B · lamp arms");
        kit.Mesh.CastShadows = false;
        foreach (var a in domeArms) kit.Box(a.min, a.max, FrontRoomsPoolMaterials.Steel, false);
        domePartsB.Add(arms.gameObject);

        // The walls' top third, one liner per course; then the gallery's soffits (its two slabs and the B4-M's top landing,
        // undersides at +1.00). Each part becomes its renderer when the next begins.
        var courses = new List<KeyValuePair<Transform, float>>();
        for (var i = 0; i < ShadeCourses; i++)
        {
            var part = kit.BeginPart("SP6 dome hall light B · wall course " + (i + 1));
            kit.Shell(DomeLid, DomeDeck);
            kit.Mesh.CastShadows = false;
            var y0 = ShadeFrom + i * FrontRoomsStructuresKit.Course;
            CourseLiner(y0, i == ShadeCourses - 1 ? DomeLid + .01f : y0 + FrontRoomsStructuresKit.Course);
            courses.Add(new KeyValuePair<Transform, float>(part, Mathf.Exp(Mathf.Log(WallShadeTop) * Mathf.Pow((i + .5f) / ShadeCourses, WallShadeCurve))));
        }
        var soffit = kit.BeginPart("SP6 dome hall light B · gallery soffit");
        kit.Shell(DomeLid, DomeDeck);
        kit.Mesh.CastShadows = false;
        var ys = Terrace - .2f - Liner;
        SoffitLiner(3f, 42f, 8.85f, 45f, ys);
        SoffitLiner(-2.85f, 45f, 8.85f, 47.85f, ys);
        SoffitLiner(6.8f, 39f, 8.1f, 42f, ys);
        kit.EndPart();
        foreach (var c in courses) LinerShade(c.Key, c.Value);
        LinerShade(soffit, SoffitShade);
        Debug.Log(FrontRoomsStructures.Log + "SP6 dome hall light B ready (L): " + domeA.Count + " A lamps to put out, " + domeLampsB.Count + " B lamps, "
            + domeShades.Count + " shaded renderers, probe " + (domeProbe != null ? "found" : "missing") + ".");
    }

    static bool InDomeHall(Vector3 p) => p.x >= -3f && p.x <= 9f && p.z >= 36f && p.z <= 48f;

    // A compact metal-halide wall spot of B: a small housing on a short steel arm off the wall, aimed at a point, its lens
    // round and driven bright. into is the wall's normal into the room (the faces: x -2.85, z 47.85, x 8.85, z 36.15).
    Light WallSpotB(Vector3 lens, Vector3 target, Vector3 into, float spot, float inner, float intensity, float range)
    {
        var fixture = new Vector3(.11f, .11f, .16f);
        var l = kit.Lamp("compact metal-halide wall spot (B)", LightType.Spot, lens, target - lens, spot, inner, intensity, range, MetalHalide, false,
            fixture, FrontRoomsPoolMaterials.Steel, SpotLens);
        AddLampB(l, .09f);
        var back = lens - (target - lens).normalized * fixture.z;
        const float t = .0175f;
        if (Mathf.Abs(into.z) > .5f)
        {
            var face = into.z > 0f ? 36.15f - .01f : 47.85f + .01f;
            domeArms.Add(Span(new Vector3(back.x - t, back.y - t, Mathf.Min(face, back.z)), new Vector3(back.x + t, back.y + t, Mathf.Max(face, back.z))));
        }
        else
        {
            var face = into.x > 0f ? -2.85f - .01f : 8.85f + .01f;
            domeArms.Add(Span(new Vector3(Mathf.Min(face, back.x), back.y - t, back.z - t), new Vector3(Mathf.Max(face, back.x), back.y + t, back.z + t)));
        }
        return l;
    }

    static Bounds Span(Vector3 min, Vector3 max) => new Bounds((min + max) * .5f, max - min);

    // A compact downlight of B under a soffit or recessed in the flat lid, aimed straight down.
    void DownB(string name, Vector3 lens, float spot, float inner, float intensity, float range, Vector3 fixture, Color drive, float lensDiameter) =>
        AddLampB(kit.Lamp(name, LightType.Spot, lens, Vector3.down, spot, inner, intensity, range, MetalHalide, false, fixture, FrontRoomsPoolMaterials.Steel, drive), lensDiameter);

    // One of B's nine lamps: off and hidden until L; its square lens becomes a round one (as the kit's underwater lamps) so
    // each lamp reads as a point.
    void AddLampB(Light l, float lensDiameter)
    {
        if (l == null) return;
        domeLampsB.Add(l);
        var lamp = l.transform.parent;
        var fixture = lamp.Find("Fixture");
        if (fixture != null) domeShowB.Add(fixture.GetComponent<Renderer>());
        var quad = lamp.Find("Lens");
        var square = quad != null ? quad.GetComponent<MeshRenderer>() : null;
        if (square == null) return;
        if (lensDiameter <= 0f) { domeShowB.Add(square); return; }
        var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = "Lens (round)";
        UnityEngine.Object.Destroy(disc.GetComponent<Collider>());
        disc.transform.SetParent(lamp, false);
        disc.transform.localPosition = new Vector3(0f, 0f, .004f);
        disc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        disc.transform.localScale = new Vector3(lensDiameter, .003f, lensDiameter);
        var dr = disc.GetComponent<MeshRenderer>();
        dr.sharedMaterial = square.sharedMaterial;
        dr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        domeShowB.Add(dr);
        UnityEngine.Object.Destroy(quad.gameObject);
    }

    // One course of the walls' liner: the west, east and north faces whole, the south face round the sight arch (x 0.3..2.7,
    // sill +1.07, springing +2.10, crown +3.30): rows of 15 mm through its head, each stopped at the opening's widest point in
    // the row, so no liner hangs into the opening (a few millimetres of A's wall show at its edge, by the lit intrados).
    void CourseLiner(float y0, float y1)
    {
        float w = -2.85f + Liner, e = 8.85f - Liner, s = 36.15f + Liner, n = 47.85f - Liner;
        LinerQuad(new Vector3(w, 0f, 36.15f), new Vector3(w, 0f, 47.85f), y0, y1, Vector3.right, false);
        LinerQuad(new Vector3(e, 0f, 36.15f), new Vector3(e, 0f, 47.85f), y0, y1, Vector3.left, false);
        LinerQuad(new Vector3(-2.85f, 0f, n), new Vector3(8.85f, 0f, n), y0, y1, Vector3.back, true);
        const float c = 1.5f, r = 1.2f, sill = 1.07f, spring = 2.1f, crown = 3.3f;
        var y = y0;
        while (y < y1 - 1e-4f)
        {
            float yb, half;
            if (y < sill - 1e-4f) { yb = Mathf.Min(y1, sill); half = 0f; }
            else if (y < spring - 1e-4f) { yb = Mathf.Min(y1, spring); half = r; }
            else if (y < crown - 1e-4f)
            {
                yb = Mathf.Min(Mathf.Min(y1, crown), y + .015f);
                half = Mathf.Sqrt(Mathf.Max(0f, r * r - (y - spring) * (y - spring)));
            }
            else { yb = y1; half = 0f; }
            if (half < 1e-3f) LinerQuad(new Vector3(-2.85f, 0f, s), new Vector3(8.85f, 0f, s), y, yb, Vector3.forward, true);
            else
            {
                LinerQuad(new Vector3(-2.85f, 0f, s), new Vector3(c - half, 0f, s), y, yb, Vector3.forward, true);
                LinerQuad(new Vector3(c + half, 0f, s), new Vector3(8.85f, 0f, s), y, yb, Vector3.forward, true);
            }
            y = yb;
        }
    }

    // A wall liner quad on a face's plane from plan point a to b, y0..y1, with the wall's own UVs (u the world coordinate along
    // the run, v = y, as Prism), so its joints are the wall's.
    void LinerQuad(Vector3 a, Vector3 b, float y0, float y1, Vector3 front, bool alongX)
    {
        if (y1 - y0 < 1e-4f || (b - a).sqrMagnitude < 1e-6f) return;
        float ua = alongX ? Site.x + a.x : Site.z + a.z, ub = alongX ? Site.x + b.x : Site.z + b.z;
        kit.Quad(new Vector3(a.x, y0, a.z), new Vector3(b.x, y0, b.z), new Vector3(b.x, y1, b.z), new Vector3(a.x, y1, a.z), front,
            new Vector2(ua, y0), new Vector2(ub, y0), new Vector2(ub, y1), new Vector2(ua, y1), tile);
    }

    // A soffit liner under a slab, facing down, with the slab's own planar UVs.
    void SoffitLiner(float x0, float z0, float x1, float z1, float y) =>
        kit.QuadPlanar(new Vector3(x0, y, z0), new Vector3(x1, y, z0), new Vector3(x1, y, z1), new Vector3(x0, y, z1), Vector3.down, tile);

    void LinerShade(Transform part, float k)
    {
        if (part == null) return;
        foreach (var r in part.GetComponentsInChildren<MeshRenderer>(true)) AddShade(r, k, false);
        domePartsB.Add(part.gameObject);
    }

    void AddShade(Renderer r, float k, bool shared)
    {
        var s = new DomeShade { renderer = r, k = k, shared = shared, usage = r.lightProbeUsage };
        if (shared && r.HasPropertyBlock())
        {
            s.block = new MaterialPropertyBlock();
            r.GetPropertyBlock(s.block);
        }
        domeShades.Add(s);
    }

    // A or B. The first call (the build) only makes sure B is off and hidden: A's lamps, renderers and the vault stay as built.
    void SetDomeB(bool on, bool initial = false)
    {
        domeB = on;
        var look = kit != null && kit.Host != null ? kit.Host.Look : null;
        if (!initial)
        {
            foreach (var l in domeA) if (l != null && look != null) look.Scale(l, on ? 0f : 1f);
            foreach (var r in domeHideB) if (r != null) r.enabled = !on;
        }
        foreach (var l in domeLampsB)
        {
            if (l == null) continue;
            if (look != null) look.Scale(l, on ? 1f : 0f);
            if (!on) l.enabled = false;
        }
        foreach (var r in domeShowB) if (r != null) r.enabled = on;
        foreach (var g in domePartsB) if (g != null) g.SetActive(on);
        ApplyDomeShade(on, true);
        domeProbeFrames = initial ? 0 : 3;
    }

    // B's shade on its renderers: the room's probe now times each one's k (again whenever that probe changes: Q, or the zone
    // look's blend at a room's edge). A gives the vault back its probe use and property block as built.
    void ApplyDomeShade(bool on, bool force)
    {
        if (!on)
        {
            if (!domeShaded) return;
            foreach (var s in domeShades)
            {
                if (s.renderer == null || !s.shared) continue;
                s.renderer.SetPropertyBlock(s.block);
                s.renderer.lightProbeUsage = s.usage;
            }
            domeShaded = false;
            return;
        }
        var sh = DomeAmbient();
        if (!force && domeShaded && sh == domeBase) return;
        domeBase = sh;
        domeShaded = true;
        if (domeBlock == null) domeBlock = new MaterialPropertyBlock();
        foreach (var s in domeShades)
        {
            if (s.renderer == null) continue;
            domeSH[0] = sh * s.k;
            s.renderer.GetPropertyBlock(domeBlock);
            domeBlock.CopySHCoefficientArraysFrom(domeSH);
            s.renderer.SetPropertyBlock(domeBlock);
            s.renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.CustomProvided;
        }
    }

    // The probe the dome hall has now, as URP gets it: the scene's trilight through Unity's own projection
    // (FrontRoomsZoneLook.TrilightProbe), and wherever 游戏视觉's zone look weighs in (Q; outside rendering RenderSettings
    // hold the scene's own values) that look's own blend at the camera (FrontRoomsZoneLook.Evaluate), so B's shade starts
    // from what the room around it shows.
    UnityEngine.Rendering.SphericalHarmonicsL2 DomeAmbient()
    {
        var sh = RenderSettings.ambientMode == UnityEngine.Rendering.AmbientMode.Trilight
            ? FrontRoomsZoneLook.TrilightProbe(RenderSettings.ambientSkyColor, RenderSettings.ambientEquatorColor, RenderSettings.ambientGroundColor)
            : RenderSettings.ambientProbe;
        var host = kit != null ? kit.Host : null;
        var walker = host != null ? host.Walker : null;
        if (FrontRoomsZoneLook.Active && walker != null)
        {
            var at = walker.View != null ? walker.View.transform.position : walker.EyePosition;
            FrontRoomsZoneLook.Blend blend;
            if (FrontRoomsZoneLook.Evaluate(at, sh, false, FogMode.ExponentialSquared, Color.black, 0f, Vector4.zero, Vector4.zero, out blend)) sh = blend.probe;
        }
        return sh;
    }

    // ---------------------------------------------------------------- T_PL_Grotto (x -3..3, z 48..60; Low)

    void BuildGrotto()
    {
        // A tiled water passage 3.0 wide in 1.5 m of tiled mass each side, Wade from the basin; a segmental vault springing 2.10
        // and crowned 2.40 over the floor (1.23 over the water): compression, and the Relay (2.05) still passes. It ends at
        // z 59.4 in a lit niche 1.5 wide: a sealed lens in its head; four wet niches at 3 m, alternate sides.
        kit.Shell(-.6f, DeepFloor);
        kit.Slab(-1.5f, 48f, 1.5f, 60f, DeepFloor, tile, .25f);
        kit.Water(-1.5f, 48f, 1.5f, 59.4f, DeepSurf);
        kit.Water(-.75f, 59.4f, .75f, 60f, DeepSurf);
        Block(-3f, 48f, -1.5f, 60.3f, .6f, DeepFloor - .25f);
        Block(1.5f, 48f, 3f, 60.3f, .6f, DeepFloor - .25f);
        // The segmental vault (C6): chord 3.0, rise 0.30 (r 3.90), springing -0.90, crown -0.60, in 12 facets with their colliders,
        // u = z and v = arc length from the springing so the courses run round the intrados. It starts at the mouth's face
        // (z 47.85) and a spandrel closes the mouth's corners up to the crown, so the mouth reads segmental, not as a
        // flat-headed culvert; the flat boxes that hid the haunches are gone (review SP6-F2 and its missed item).
        var spring = DeepFloor + 2.1f;
        var crown = DeepFloor + 2.4f;
        const float VaultR = 3.9f, Mouth = 47.85f;
        var halfAngle = Mathf.Asin(1.5f / VaultR);
        var arcY = crown - VaultR;
        const int facets = 12;
        for (var f = 0; f < facets; f++)
        {
            var t0 = -halfAngle + 2f * halfAngle * f / facets;
            var t1 = -halfAngle + 2f * halfAngle * (f + 1) / facets;
            var p0 = new Vector2(VaultR * Mathf.Sin(t0), arcY + VaultR * Mathf.Cos(t0));
            var p1 = new Vector2(VaultR * Mathf.Sin(t1), arcY + VaultR * Mathf.Cos(t1));
            float v0 = VaultR * (t0 + halfAngle), v1 = VaultR * (t1 + halfAngle);
            var mid = (p0 + p1) * .5f;
            var inward = new Vector3(-mid.x, arcY - mid.y, 0f).normalized;
            kit.Quad(new Vector3(p0.x, p0.y, Mouth), new Vector3(p1.x, p1.y, Mouth), new Vector3(p1.x, p1.y, 59.4f), new Vector3(p0.x, p0.y, 59.4f), inward,
                new Vector2(Mouth, v0), new Vector2(Mouth, v1), new Vector2(59.4f, v1), new Vector2(59.4f, v0), tile, true);
            if (crown + .01f - Mathf.Min(p0.y, p1.y) > .003f)
                kit.QuadPlanar(new Vector3(p0.x, p0.y, Mouth - .001f), new Vector3(p1.x, p1.y, Mouth - .001f), new Vector3(p1.x, crown + .01f, Mouth - .001f), new Vector3(p0.x, crown + .01f, Mouth - .001f), Vector3.back, tile);
        }
        kit.Box(-1.5f, crown + .002f, 48f, 1.5f, crown + .1f, 59.4f, tile, false);
        kit.Box(-1.5f, crown + .1f, 48f, 1.5f, .6f, 60.3f, tile);
        // The end: the passage closes at z 59.4 but for the niche (x -0.75..0.75, z 59.4..60.0), a sealed lens in its head.
        kit.Box(-1.5f, DeepFloor, 59.4f, -.75f, crown + .1f, 60f, tile);
        kit.Box(.75f, DeepFloor, 59.4f, 1.5f, crown + .1f, 60f, tile);
        kit.Box(-1.5f, DeepFloor - .25f, 60f, 1.5f, .6f, 60.3f, tile);
        kit.Box(-.75f, spring + .05f, 59.4f, .75f, crown + .1f, 60f, tile);
        kit.Lamp("niche lens (F5)", LightType.Spot, new Vector3(0f, spring + .02f, 59.7f), Vector3.down, 140f, 100f, 2.4f, 4f, new Color(.92f, .98f, 1f), true,
            new Vector3(.5f, .4f, .04f), FrontRoomsPoolMaterials.Steel, new Color(1.8f, 1.9f, 1.95f));
        Niche(new Vector3(-1.45f, -2.4f, 51f), Vector3.right);
        Niche(new Vector3(1.45f, -2.4f, 54f), Vector3.left);
        Niche(new Vector3(-1.45f, -2.4f, 57f), Vector3.right);
        Niche(new Vector3(1.45f, -2.4f, 49.5f), Vector3.left);
    }

    // ---------------------------------------------------------------- T_PL_Casbah (F6; x -18..0, z 3..21; Tall 5.4)

    void BuildCasbah()
    {
        // Tiled blocks in a flooded floor (Wade: floor -1.20, surface -0.03): A +1.20, B +-0, C +2.40 (the top), D +0.60, E +1.20;
        // unequal, no two on one line (R4). Every block of two cells or more has two ways (R6).
        kit.Shell(Tall, WadeFloor);
        kit.Slab(-18f, 3f, 0f, 21f, WadeFloor, tile, .3f);
        kit.Water(-18f, 3f, 0f, 21f, WadeSurf);
        kit.Shell(Tall, Terrace);
        Block(-3f, 9f, 0f, 21f, Terrace, WadeFloor);     // A, entered from the terrace through the BeachHall's west wall
        Block(-12f, 12f, -6f, 18f, 2.4f, WadeFloor);     // C
        Block(-18f, 9f, -12f, 15f, .6f, WadeFloor);      // D
        Block(-15f, 3f, -9f, 6f, Terrace, WadeFloor);    // E
        kit.Shell(Tall, 0f);
        Block(-6f, 18f, -3f, 21f, 0f, WadeFloor);        // B, with its arch onto the stepping stones
        // B: a step run of 8 east up onto A; 8 steps into water on its west edge.
        kit.StepRun(new Vector3(-3f, 0f, 19.5f), Vector3.right, 1.8f, 0f, Terrace, tile, nosing);
        // Its open sides from the tread at 0.75 up, flush with A's parapet at x -3.0 (70 section 1: guard over 0.76; review SP6-F13).
        SlopedParapet(new Vector3(-3.9f, .75f, 18.5f), new Vector3(-3f, Terrace, 18.5f), 0f);
        SlopedParapet(new Vector3(-3.9f, .75f, 20.5f), new Vector3(-3f, Terrace, 20.5f), 0f);
        kit.Shell(Tall, WadeFloor);
        kit.StepRun(new Vector3(-6f, 0f, 19.5f), Vector3.right, 1.2f, WadeFloor, 0f, tile, nosing);
        // A -> C: a flight-bridge over the water at z 15.0..16.2, 8 risers up west from x -3.0, a 0.9 landing to C's edge.
        kit.Shell(Tall, Terrace);
        kit.Flight(new Vector3(-3f, Terrace, 15.6f), Vector3.left, 1.2f, 8, .15f, .3f, tile, nosing, false);
        kit.Slab(-6f, 15f, -5.1f, 16.2f, 2.4f, tile, .3f);
        SlopedParapet(new Vector3(-3f, Terrace + .15f, 14.95f), new Vector3(-5.1f, 2.4f, 14.95f));
        SlopedParapet(new Vector3(-3f, Terrace + .15f, 16.25f), new Vector3(-5.1f, 2.4f, 16.25f));
        Parapet(-5.1f, 14.95f, -6f, 14.95f, 2.4f);
        Parapet(-5.1f, 16.25f, -6f, 16.25f, 2.4f);
        // C -> D: a B2 of 12 risers down west from C's edge at z 12.6..13.8 onto D.
        kit.Shell(Tall, .6f);
        kit.Flight(new Vector3(-15.3f, .6f, 13.2f), Vector3.right, 1.2f, 12, .15f, .3f, tile, nosing, true);
        SlopedParapet(new Vector3(-15.3f, .75f, 12.5f), new Vector3(-12f, 2.4f, 12.5f), .6f);
        SlopedParapet(new Vector3(-15.3f, .75f, 13.9f), new Vector3(-12f, 2.4f, 13.9f), .6f);
        // D -> E: a level bridge at +1.20 from E's north edge (x -13.8..-12.6, z 6..9) ending in 4 risers down onto D.
        kit.Shell(Tall, Terrace);
        kit.Box(-13.8f, Terrace - .3f, 6f, -12.6f, Terrace, 9f, tile);
        Parapet(-13.75f, 6f, -13.75f, 9f, Terrace);
        Parapet(-12.65f, 6f, -12.65f, 9f, Terrace);
        kit.StepRun(new Vector3(-13.2f, 0f, 9f), Vector3.back, 1.2f, .6f, Terrace, tile, nosing);
        // E: a C7 of 16 risers from its east edge into the floor water (z 3.6..4.8).
        kit.Shell(Tall, WadeFloor);
        kit.StepRun(new Vector3(-9f, 0f, 4.2f), Vector3.left, 1.2f, WadeFloor, Terrace, tile, nosing);
        // Its open sides where the treads stand over 0.76 above the water, flush with E's parapet at x -9.0 (review SP6-F13).
        SlopedParapet(new Vector3(-8.1f, .75f, 3.5f), new Vector3(-9f, Terrace, 3.5f), WadeFloor);
        SlopedParapet(new Vector3(-8.1f, .75f, 4.9f), new Vector3(-9f, Terrace, 4.9f), WadeFloor);
        // Parapets 1.07 on A, C and E (D's edges are plain copings at 0.63).
        Parapet(-2.9f, 9.1f, -.15f, 9.1f, Terrace);
        Parapet(-2.9f, 9.1f, -2.9f, 15f, Terrace);
        Parapet(-2.9f, 16.2f, -2.9f, 18.6f, Terrace);
        Parapet(-2.9f, 20.4f, -2.9f, 20.85f, Terrace);
        Parapet(-11.9f, 12.1f, -6.1f, 12.1f, 2.4f);
        Parapet(-11.9f, 17.9f, -6.1f, 17.9f, 2.4f);
        Parapet(-6.1f, 12.1f, -6.1f, 15f, 2.4f);
        Parapet(-6.1f, 16.2f, -6.1f, 17.9f, 2.4f);
        Parapet(-11.9f, 13.8f, -11.9f, 17.9f, 2.4f);
        Parapet(-11.9f, 12.1f, -11.9f, 12.6f, 2.4f);
        Parapet(-14.9f, 5.9f, -13.8f, 5.9f, Terrace);
        Parapet(-12.6f, 5.9f, -9.1f, 5.9f, Terrace);
        Parapet(-14.9f, 3.15f, -14.9f, 5.9f, Terrace);
        Parapet(-9.1f, 4.8f, -9.1f, 5.9f, Terrace);
        Parapet(-9.1f, 3.15f, -9.1f, 3.6f, Terrace);
        kit.Shell(Tall, .6f);
        kit.Coping(new Vector3(-18f, 0f, 15f), new Vector3(-12f, 0f, 15f), .6f, tile);
        kit.Coping(new Vector3(-12f, 0f, 9f), new Vector3(-12f, 0f, 12.6f), .6f, tile);
        kit.Coping(new Vector3(-12f, 0f, 13.8f), new Vector3(-12f, 0f, 15f), .6f, tile);
        kit.Coping(new Vector3(-18f, 0f, 9f), new Vector3(-13.8f, 0f, 9f), .6f, tile);
        kit.Coping(new Vector3(-12.6f, 0f, 9f), new Vector3(-12f, 0f, 9f), .6f, tile);
        // Walls: south z 3, west x -18, north z 21 with B's arch onto the stepping stones; the east wall is the BeachHall's.
        kit.Shell(Tall, 0f);
        kit.WallX(3f, -18.15f, 0f, WadeFloor - .1f, Tall + .16f, tile, null, .3f);
        kit.WallZ(-18f, 3f, 21.15f, WadeFloor - .1f, Tall + .16f, tile, null, .3f);
        kit.WallX(21f, -18.15f, 0f, WadeFloor - .1f, Tall + .16f, tile, Arches(-5.4f, -3.6f, 0f, 2.1f, 1f), .3f);
        kit.Lid(-18f, 3f, 0f, 21f, Tall, tile);
        kit.WallBox(new Vector3(-18f, 3.4f, 9f), Vector3.right, 6f, 11f, false);
        kit.WallBox(new Vector3(-18f, 3.4f, 16f), Vector3.right, 6f, 11f, false);
        kit.WallBox(new Vector3(-9f, 3.4f, 3f), Vector3.forward, 6f, 11f, false);
        kit.WallBox(new Vector3(-15f, 3.4f, 3f), Vector3.forward, 6f, 11f, false);
        kit.WallBox(new Vector3(-3f, 3.4f, 3f), Vector3.forward, 6f, 11f, false);
        kit.WallBox(new Vector3(-15f, 3.4f, 21f), Vector3.back, 6f, 11f, false);
        kit.WallBox(new Vector3(-9f, 3.4f, 21f), Vector3.back, 6f, 11f, false);
        foreach (var d in new[] { new Vector2(-15f, 18f), new Vector2(-4.5f, 6f), new Vector2(-9f, 9f), new Vector2(-15f, 6f), new Vector2(-15f, 12f), new Vector2(-9f, 15f), new Vector2(-3f, 15f) })
            kit.DownFill(new Vector3(d.x, Tall - .07f, d.y), 2.2f, 11f, false);
        // Six wet niches in its flooded floor.
        Niche(new Vector3(-17.85f, -.7f, 18f), Vector3.right);
        Niche(new Vector3(-17.85f, -.7f, 5f), Vector3.right);
        Niche(new Vector3(-6f, -.7f, 3.15f), Vector3.forward);
        Niche(new Vector3(-1f, -.7f, 3.15f), Vector3.forward);
        Niche(new Vector3(-9f, -.7f, 20.85f), Vector3.back);
        Niche(new Vector3(-14f, -.7f, 20.85f), Vector3.back);
    }

    // ---------------------------------------------------------------- C8 covered basin (x 6..15, z -3..3; Standard 2.9)

    void BuildCoveredBasin()
    {
        // An access floor at +-0 (0.6 panels on pedestals) over x 6..10.8, the water under it at Wade (surface -0.60, floor
        // -1.80); three panels missing, each ringed by a 1.07 tube rail; the open edge on x 10.8 steps down as a C7 of 12
        // risers east into the open water (x 10.8..15; 4 above the surface, 8 in it), its foot 0.75 m short of the end wall
        // so the water beside it is waded on the level (walk test 11-04: at x 11.4 its foot was the wall). Two sealed lenses
        // in the lid, three wet niches under the floor so the holes glow.
        kit.Shell(Std, -1.8f);
        kit.Slab(6f, -3f, 15f, 3f, -1.8f, tile, .25f);
        kit.Water(6f, -3f, 15f, 3f, -.6f);
        kit.WallX(-3f, 6f, 15.15f, -1.9f, Std + .16f, tile, null, .3f);
        kit.WallX(3f, 6f, 15.15f, -1.9f, Std + .16f, tile, null, .3f);
        kit.WallZ(15f, -3f, 3f, -1.9f, Std + .16f, tile, null, .3f);
        kit.WallZ(6f, -3f, 3f, -1.9f, -.3f, tile, null, .16f);
        kit.Lid(6f, -3f, 15f, 3f, Std, tile);
        kit.Shell(Std, 0f);
        var panel = FrontRoomsPoolMaterials.Lit("access floor panel (greybox)", new Color(.72f, .72f, .7f), .3f);
        var missing = new[] { new Vector2(7.2f, -1.8f), new Vector2(9.0f, .6f), new Vector2(9.6f, -2.4f) };
        for (var x = 6f; x < 10.79f; x += .6f)
            for (var z = -3f; z < 2.99f; z += .6f)
            {
                var gone = false;
                foreach (var m in missing) if (Mathf.Abs(m.x - x) < .01f && Mathf.Abs(m.y - z) < .01f) gone = true;
                if (!gone) kit.Box(x + .005f, -.04f, z + .005f, x + .595f, 0f, z + .595f, panel);
                kit.Box(x - .02f, -1.8f, z - .02f, x + .02f, -.04f, z + .02f, FrontRoomsPoolMaterials.Steel, false);
            }
        foreach (var m in missing)
        {
            // A tube rail round each hole, guarded whatever the drop (70 section 1).
            kit.Guard(new Vector3(m.x - .05f, 0f, m.y - .05f), new Vector3(m.x + .65f, 0f, m.y - .05f), 0f, FrontRoomsStructuresKit.GuardStyle.Rail);
            kit.Guard(new Vector3(m.x - .05f, 0f, m.y + .65f), new Vector3(m.x + .65f, 0f, m.y + .65f), 0f, FrontRoomsStructuresKit.GuardStyle.Rail);
            kit.Guard(new Vector3(m.x - .05f, 0f, m.y - .05f), new Vector3(m.x - .05f, 0f, m.y + .65f), 0f, FrontRoomsStructuresKit.GuardStyle.Rail);
            kit.Guard(new Vector3(m.x + .65f, 0f, m.y - .05f), new Vector3(m.x + .65f, 0f, m.y + .65f), 0f, FrontRoomsStructuresKit.GuardStyle.Rail);
        }
        kit.Shell(Std, -1.8f);
        kit.StepRun(new Vector3(10.8f, 0f, 0f), Vector3.left, 2.4f, -1.8f, 0f, tile, nosing);
        kit.Coping(new Vector3(10.8f, 0f, -3f), new Vector3(10.8f, 0f, -1.2f), 0f, tile);
        kit.Coping(new Vector3(10.8f, 0f, 1.2f), new Vector3(10.8f, 0f, 3f), 0f, tile);
        Lens(new Vector3(8.7f, Std - .05f, 0f));
        Lens(new Vector3(13.2f, Std - .05f, 0f));
        Niche(new Vector3(6.15f, -1.2f, -1.5f), Vector3.right);
        Niche(new Vector3(6.15f, -1.2f, 1.5f), Vector3.right);
        Niche(new Vector3(9f, -1.2f, -2.85f), Vector3.forward);
    }

    // ---------------------------------------------------------------- the sign (70 section 8, verbatim) and marks

    void BuildSigns()
    {
        // Flat on the corridor's east wall facing west, out of the main view (70 section 8), its face's centre 2.50 m from the
        // arrival eye (20_placard_type.md v2.4, E's FAR; was z 3.5, 2.42 m).
        kit.Sign(new Vector3(5.85f, 1.5f, 3.593f), new Vector3(4.5f, 1.5f, 3.593f), "SP6 · 泳池层：深处的地形和大螺旋楼梯",
            "泳池层更深处的一段路，一路往下走。瓷砖走廊尽头是一道窄高的拱门；门后大厅的地面先是一层没过脚踝的水，再以 1:12 的坡度斜进没过腰的水里，尽头是半圆形的端墙。左边台阶上到带拱廊的台地，过一座有栏板的桥到白色的岛上。岛脚下是三道宽溢水堰，每道落差 0.6 米，旁边有台阶一级级往下。堰底的拱门通向比入口低 1.8 米的穹顶大厅：白色大螺旋楼梯绕着亮灯的井口升高 3 米到回廊，穹顶偏心开一个圆洞，光落在水面上成一个圆斑。回廊另一头有直跑楼梯，Relay 走这一条。楼梯脚下另有一段台阶走进水里，正对一条低矮的拱顶水洞，往里蹚 12 米，尽头是一个亮着灯的瓷砖壁龛。旁边还有高低错落、最低一层泡在水里的瓷砖块群，一间冷水池小间，和一个铺了检修地板、缺了几块板的水池。水都是关卡里那种不对劲的青绿色，最深 1.2 米，都能蹚过去；这里没有任何灯会闪。\n"
            + "灰盒占位：全部地形块、拱、穹顶和圆洞上方的灯井、楼梯和栏板细部、检修地板、水（没有焦散）；瓷砖用的是 150 毫米瓷白砖贴图（只在这个场景里）。黑色人形是 Relay 的静态替身（2.05 米），只用来检查视线，不会动。\n"
            + "要你决定：1 浅滩大厅、岛和水渠、溢水台地、穹顶回廊、水洞、瓷砖块群，各自放进泳池层吗？ 2 大螺旋楼梯的栏板用实心瓷砖（现在这样）还是玻璃（60 Q4）？ 3 冷水池小间和铺了检修地板的水池，各自放进泳池层吗？", 0f);
        // Its blade (20_placard_type.md v2.5, E-blade): the arrival reads the flat sign 56 degrees off its face, so its code and
        // title also hang on a blade square to the east wall (face x 5.92), inner edge 50 mm off it, bottom 2.10, its title on two
        // lines at the colon (the blade's 12-em measure), top 2.36 (0.54 under the lid; the lenses at z 1.5 and 4.5 stay clear).
        // Front face z 3.242, 2.00 m from the arrival eye (read 24 degrees to the side and 18 up), not 2.5: placement first (70
        // section 1). At 2.5 m (z 3.82) its inner end covered the top right of the tall arch that frames the main view (70
        // section 8; review11 pass 1); with the walker's eye as rendered (1.64) its silhouette clears the arch's edge by 1.4 px
        // at z 3.35, 8 px at 3.28 and 12-13 px (about 1 degree) here, so the arch and the blade read apart in the arrival frame.
        FrontRoomsStructuresSigns.Blade(Root, Root.TransformPoint(new Vector3(5.92f, 0f, 3.257f)), Root.TransformDirection(Vector3.left),
            Root.TransformPoint(Arrival + Vector3.up * FrontRoomsStructuresWalker.Eye), "SP6 · 泳池层：深处的地形和大螺旋楼梯", Root.position.y);
        kit.Mark(new Vector3(4.5f, 3.2f, 6.0f), Vector3.back, "拱 1.5 · 起拱 2.10 · 拱顶 2.85");
        kit.Mark(new Vector3(10f, .9f, 20.7f), Vector3.back, "坡 1:12 · 水深 0 → 1.12");
        kit.Mark(new Vector3(10.5f, 1.4f, 26.9f), Vector3.back, "堰 0.60 × 3 · 9 米宽");
        kit.Mark(new Vector3(6f, 1.0f, 41.5f), Vector3.back, "B4-M：18 级 × 0.167 · 248° · 宽 1.30");
        kit.Mark(new Vector3(1.5f, -.3f, 43.5f), Vector3.back, "圆洞 Ø1.2 · 穹顶 +6.00");
        kit.Mark(new Vector3(0f, -1.0f, 49f), Vector3.back, "水洞拱顶离水面 1.23");
    }

    // ---------------------------------------------------------------- the frame

    public override FrontRoomsStructuresLook.Preset PresetAt(Vector3 world) => FrontRoomsStructuresLook.Preset.Pool;

    public override void RelayBody(FrontRoomsStructures host, bool on)
    {
        if (kit != null) kit.SetRelay(on);
    }

    public override void ShowMetrics(FrontRoomsStructures host, bool on)
    {
        if (kit != null) kit.ShowMarks(on);
    }

    // The dome hall's light B: its shade follows the room's probe (Q, a zone's edge) and its probe is rendered again after a switch.
    public override void Tick(FrontRoomsStructures host, float dt, bool isCurrent)
    {
        if (!isCurrent || !domeReady) return;
        if (domeB) ApplyDomeShade(true, false);
        if (domeProbeFrames > 0 && --domeProbeFrames == 0 && domeProbe != null && domeProbe.isActiveAndEnabled)
        {
            try { domeProbe.RenderProbe(); }
            catch (Exception e) { FrontRoomsPoolMaterials.LogOnce("SP6 dome probe", "SP6: the dome hall's probe did not render again: " + e.Message); }
        }
    }

    // L: the dome hall's light, A (today's) or B (S1 C).
    public override bool KeyPressed(FrontRoomsStructures host, UnityEngine.InputSystem.Key key, out string message)
    {
        message = null;
        if (key != UnityEngine.InputSystem.Key.L) return false;
        if (!domeReady)
        {
            message = "穹顶厅灯光：B 没有搭好（原因在日志里），现在只有 A。";
            return true;
        }
        try { SetDomeB(!domeB); }
        catch (Exception e) { FrontRoomsPoolMaterials.LogOnce("SP6 dome switch", "SP6: the dome hall's light switch failed: " + e.Message); }
        message = domeB
            ? "穹顶厅灯光 · B 天花压暗、灯更小更亮：穹顶、回廊底下和墙最上面一段落进阴影，往上打的大壁灯换成往下打的小亮灯，圆洞是暗穹顶里的亮圆（只改穹顶大厅）"
            : "穹顶厅灯光 · A 今天的：整间瓷白，两盏大壁灯往上打，穹顶最亮";
        return true;
    }

    public override string Status(FrontRoomsStructures host) =>
        !domeReady ? "" : domeB ? "穹顶厅灯光：B 天花压暗、灯更小更亮（L 换）" : "穹顶厅灯光：A 今天的（L 换）";

    public override void Guide(FrontRoomsStructures host, StringBuilder sb)
    {
        sb.AppendLine(Title);
        sb.AppendLine("一路往下走的泳池层深处：浅滩大厅 → 台地和桥 → 岛 → 溢水台地 → 穹顶大厅（大螺旋楼梯 B4-M、回廊、圆洞）→ 水洞；旁边是瓷砖块群、冷水池小间和检修地板水池。");
        sb.AppendLine();
        sb.AppendLine("按键：[ ] 换站点（走廊、台地、岛、溢水台地顶、穹顶回廊、穹顶甲板、水洞尽头、块群顶、检修地板水池）；R 回到当前站点；C Relay 体型（大螺旋楼梯对它关闭）；M 尺寸；L 穹顶厅灯光 A / B。");
        sb.AppendLine();
        if (!domeReady) sb.AppendLine("穹顶厅灯光（L）：B 没有搭好（原因在日志里），现在只有 A。");
        else
            sb.AppendLine("穹顶厅灯光（L）：现在是 " + (domeB ? "B" : "A") + "。A 是今天的：整间瓷白，两盏大壁灯往上打，穹顶是最亮的面。"
                + "B 是天花压暗、灯更小更亮：地面、甲板和墙的下面三分之二照旧瓷白、照亮；穹顶、回廊底下的天花和墙最上面 1.8 米落进阴影（瓷砖还是同一种瓷白，只是没有灯往上照）；"
                + "往上打的两盏大壁灯换成往下打的小亮灯：大壁灯原来的位置各一盏，回廊底下四盏，回廊两个远角的天花上各一盏，圆洞里一盏照亮井壁，圆洞成了暗穹顶里的一个亮圆。"
                + "离岸两米以外的水倒映的是上面的暗处和这些小灯，所以 B 的水应该显得更深、闪光更明显。只改穹顶大厅，SP6 其他地方不变。"
                + "B 的小灯、灯臂和圆洞的灯罩是灰盒占位。");
        sb.AppendLine();
        sb.AppendLine("要你决定（在对话里按编号告诉总控）：");
        sb.AppendLine("SP6-1 浅滩大厅、岛和水渠、溢水台地、穹顶回廊、水洞、瓷砖块群，各自放进泳池层吗？");
        sb.AppendLine("SP6-2 大螺旋楼梯的栏板用实心瓷砖（现在这样）还是玻璃（60 Q4）？");
        sb.AppendLine("SP6-3 冷水池小间和铺了检修地板的水池，各自放进泳池层吗？");
        sb.AppendLine("SP6-4 穹顶大厅的灯光用 A（今天的）还是 B（天花压暗、灯更小更亮）？按 L 对比；选 B 的话，再定别的泳池房间要不要也这样。");
        sb.AppendLine();
        sb.AppendLine("灰盒占位：全部地形块、拱、穹顶和圆洞上方的灯井、楼梯和栏板细部、检修地板、水（没有焦散）；瓷砖用的是 150 毫米瓷白砖贴图（只在这个场景里）。黑色人形是 Relay 的静态替身（2.05 米），只用来检查视线，不会动。");
    }
}
