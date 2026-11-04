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
/// Greybox where no kit exists, listed on the sign.
/// </summary>
public sealed class FrontRoomsStructuresPoolTerrain : FrontRoomsStructuresDistrict
{
    const float Tall = 5.4f, Std = 2.9f, Terrace = 1.2f, DomeDeck = -1.8f, DomeLid = 3.6f;
    const float WadeSurf = -.03f, WadeFloor = -1.2f, DeepSurf = -1.83f, DeepFloor = -3.0f;
    static readonly Vector3 Arrival = new Vector3(4.5f, 0f, 1.5f);

    FrontRoomsStructuresKit kit;
    Material tile, nosing, sheet;
    readonly List<Light> viewLamps = new List<Light>();

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
        kit.Stop("拱廊台地", new Vector3(1.4f, Terrace, 11f), new Vector3(9f, 0f, 16f));
        kit.Stop("岛上", new Vector3(0f, Terrace, 27.6f), new Vector3(15f, 0f, 36f));
        kit.Stop("溢水台地顶", new Vector3(10.5f, 0f, 25.4f), new Vector3(16.5f, -1.8f, 40f));
        kit.Stop("穹顶回廊", new Vector3(5.5f, Terrace, 46.8f), new Vector3(1.5f, DeepSurf, 43.5f));
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

    // A tiled parapet along a slope (nosing-line points a, b): a tilted solid 0.2 thick, 1.07 over the line, one collider.
    void SlopedParapet(Vector3 a, Vector3 b)
    {
        var d = b - a;
        var len = d.magnitude;
        if (len < .05f) return;
        var rot = Quaternion.LookRotation(d / len, Vector3.up);
        var mid = (a + b) * .5f + Vector3.up * (FrontRoomsStructuresKit.GuardHeight * .5f);
        kit.Mesh.Box(mid, new Vector3(.2f, FrontRoomsStructuresKit.GuardHeight, len), rot, tile, true);
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
        SlopedParapet(new Vector3(3.1f, .75f, 8.1f), new Vector3(3.1f, Terrace, 9f));
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
        Parapet(2.9f, 24.1f, 2.9f, 29.9f, Terrace);
        Parapet(-2.9f, 29.9f, .6f, 29.9f, Terrace);
        Parapet(2.4f, 29.9f, 2.9f, 29.9f, Terrace);
        // The second way onto the island (R6): a step run of 8 risers rising south to its edge, its cheeks guarded.
        kit.Shell(Tall, 0f);
        kit.StepRun(new Vector3(1.5f, 0f, 30f), Vector3.back, 1.8f, 0f, Terrace, tile, nosing);
        SlopedParapet(new Vector3(.5f, .75f, 30.9f), new Vector3(.5f, Terrace, 30f));
        SlopedParapet(new Vector3(2.5f, .75f, 30.9f), new Vector3(2.5f, Terrace, 30f));
        // B8 stepping stones: ten tiled drums 0.6 across, tops 3 cm proud of the water, 1.2 m apart, along x -4.5.
        for (var k = 0; k < 9; k++) kit.Ring(new Vector3(-4.5f, 0f, 21.6f + 1.2f * k), 0f, .3f, WadeFloor, 0f, -180f, 180f, tile, true, null, 22.5f);
        kit.Ring(new Vector3(-3.6f, 0f, 32.0f), 0f, .3f, WadeFloor, 0f, -180f, 180f, tile, true, null, 22.5f);
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
            var crest = t[k] - .03f;
            var below = k < 2 ? t[k + 1] - .03f : DeepSurf;
            kit.Shell(Tall, below);
            if (k < 2)
            {
                kit.Box(12f, crest - .3f, z - .15f, 19.6f, crest, z, tile);
                kit.Box(20.8f, crest - .3f, z - .15f, 21f, crest, z, tile);
                Sheet(new Vector3(12.05f, 0f, z + .01f), new Vector3(19.55f, 0f, z + .01f), crest, below);
                Sheet(new Vector3(20.85f, 0f, z + .01f), new Vector3(20.95f, 0f, z + .01f), crest, below);
            }
            else
            {
                kit.Box(12f, crest - .3f, z - .15f, 21f, crest, z, tile);
                Sheet(new Vector3(12.05f, 0f, z + .01f), new Vector3(20.95f, 0f, z + .01f), crest, below);
            }
            // A down-fill over each weir lip (R8: lit lips).
            kit.DownFill(new Vector3(16.5f, Tall - .07f, z), 2.0f, 12f, false);
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
        kit.WallBox(new Vector3(21f, 3.4f, 28.5f), Vector3.left, 6f, 10f, false);
        kit.WallBox(new Vector3(21f, 2.4f, 37.5f), Vector3.left, 6f, 10f, false);
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
        SlopedParapet(new Vector3(-1.7f, DomeDeck + .15f, 38.4f), new Vector3(-1.7f, -.3f, 41.1f));
        Parapet(-1.7f, 41.1f, -1.7f, 42.3f, -.3f);
        SlopedParapet(new Vector3(-1.7f, -.15f, 42.3f), new Vector3(-1.7f, Terrace, 45f));
        // The Relay stand-in at the B2's head on the gallery's far end, facing south.
        kit.Relay(new Vector3(-2.4f, Terrace, 46.2f), new Vector3(-2.4f, Terrace, 40f));
        // Walls: south z 36 (the island room's, with the sight arch), west x -3, north z 48 with the grotto's mouth, east x 9
        // (z 42..48; z 36..42 is the weir room's). The flat lid at +3.60 outside the dome's circle is built with the dome.
        kit.WallZ(-3f, 36f, 48.15f, DeepFloor - .1f, DomeLid + .2f, tile, null, .3f);
        kit.WallX(48f, -3.15f, 9.15f, DeepFloor - .1f, DomeLid + .2f, tile, Arches(-1.5f, 1.5f, DeepFloor, -.75f, 0f), .3f);
        kit.WallZ(9f, 42f, 48f, DeepFloor - .1f, DomeLid + .2f, tile, null, .3f);
        // Lamps: wall boxes on the south and west walls at +2.00 under the springing (the dome the brightest surface after the
        // oculus), two downlights in the gallery's soffit over the water, three wet niches in the basin.
        kit.WallBox(new Vector3(4.5f, 2.0f, 36.15f), Vector3.forward, 5f, 9f, false);
        kit.WallBox(new Vector3(-2.85f, 2.0f, 40.5f), Vector3.right, 5f, 9f, false);
        kit.Lamp("gallery soffit downlight", LightType.Spot, new Vector3(1.5f, Terrace - .25f, 46.5f), Vector3.down, 110f, 70f, 1.6f, 6f, new Color(.92f, .98f, 1f), false, new Vector3(.25f, .25f, .05f), FrontRoomsPoolMaterials.Steel, new Color(1.4f, 1.5f, 1.55f));
        kit.Lamp("gallery soffit downlight", LightType.Spot, new Vector3(4.5f, Terrace - .25f, 45f), Vector3.down, 110f, 70f, 1.6f, 6f, new Color(.92f, .98f, 1f), false, new Vector3(.25f, .25f, .05f), FrontRoomsPoolMaterials.Steel, new Color(1.4f, 1.5f, 1.55f));
        Niche(new Vector3(.15f, -2.4f, 43f), Vector3.right);
        Niche(new Vector3(2.85f, -2.4f, 46f), Vector3.left);
        Niche(new Vector3(4.5f, -2.4f, 47.85f), Vector3.back);
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
            var r0 = rim * i / rings;
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
        var l = kit.Lamp("oculus (metal-halide downlight)", LightType.Spot, new Vector3(oculus.x, 7.1f, oculus.y), Vector3.down, 22f, 14f, 60f, 14f, new Color(.95f, .98f, 1f), true, Vector3.zero, null, Color.black);
        viewLamps.Add(l);
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
        // The segmental vault: two haunch panels and a crown panel, the mass above it.
        var spring = DeepFloor + 2.1f;
        var crown = DeepFloor + 2.4f;
        kit.Quad(new Vector3(-1.5f, spring, 48f), new Vector3(-1.5f, spring, 59.4f), new Vector3(-.6f, crown, 59.4f), new Vector3(-.6f, crown, 48f), Vector3.down,
            new Vector2(48f, 0f), new Vector2(59.4f, 0f), new Vector2(59.4f, .95f), new Vector2(48f, .95f), tile, true);
        kit.Quad(new Vector3(.6f, crown, 48f), new Vector3(.6f, crown, 59.4f), new Vector3(1.5f, spring, 59.4f), new Vector3(1.5f, spring, 48f), Vector3.down,
            new Vector2(48f, .95f), new Vector2(59.4f, .95f), new Vector2(59.4f, 0f), new Vector2(48f, 0f), tile, true);
        kit.Box(-.6f, crown, 48f, .6f, crown + .1f, 59.4f, tile);
        kit.Box(-1.5f, crown + .1f, 48f, 1.5f, .6f, 60.3f, tile);
        kit.Box(-1.5f, spring - .01f, 48f, -.6f, crown + .1f, 59.4f, tile, false);
        kit.Box(.6f, spring - .01f, 48f, 1.5f, crown + .1f, 59.4f, tile, false);
        // The end: the passage closes at z 59.4 but for the niche (x -0.75..0.75, z 59.4..60.0), a sealed lens in its head.
        kit.Box(-1.5f, DeepFloor, 59.4f, -.75f, crown + .1f, 60f, tile);
        kit.Box(.75f, DeepFloor, 59.4f, 1.5f, crown + .1f, 60f, tile);
        kit.Box(-1.5f, DeepFloor - .25f, 60f, 1.5f, .6f, 60.3f, tile);
        kit.Box(-.75f, spring + .05f, 59.4f, .75f, crown + .1f, 60f, tile);
        kit.Lamp("niche lens (F5)", LightType.Spot, new Vector3(0f, spring + .02f, 59.7f), Vector3.down, 140f, 100f, 2.4f, 4f, new Color(.92f, .98f, 1f), false,
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
        SlopedParapet(new Vector3(-15.3f, .75f, 12.55f), new Vector3(-12f, 2.4f, 12.55f));
        SlopedParapet(new Vector3(-15.3f, .75f, 13.85f), new Vector3(-12f, 2.4f, 13.85f));
        // D -> E: a level bridge at +1.20 from E's north edge (x -13.8..-12.6, z 6..9) ending in 4 risers down onto D.
        kit.Shell(Tall, Terrace);
        kit.Box(-13.8f, Terrace - .3f, 6f, -12.6f, Terrace, 9f, tile);
        Parapet(-13.75f, 6f, -13.75f, 9f, Terrace);
        Parapet(-12.65f, 6f, -12.65f, 9f, Terrace);
        kit.StepRun(new Vector3(-13.2f, 0f, 9f), Vector3.back, 1.2f, .6f, Terrace, tile, nosing);
        // E: a C7 of 16 risers from its east edge into the floor water (z 3.6..4.8).
        kit.Shell(Tall, WadeFloor);
        kit.StepRun(new Vector3(-9f, 0f, 4.2f), Vector3.left, 1.2f, WadeFloor, Terrace, tile, nosing);
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
        foreach (var d in new[] { new Vector2(-15f, 18f), new Vector2(-4.5f, 6f), new Vector2(-9f, 9f) }) kit.DownFill(new Vector3(d.x, Tall - .07f, d.y), 2.2f, 11f, false);
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
        // An access floor at +-0 (0.6 panels on pedestals) over x 6..11.4, the water under it at Wade (surface -0.60, floor
        // -1.80); three panels missing, each ringed by a 1.07 tube rail; the open edge on x 11.4 steps down as a C7 of 12
        // risers east into the open water (4 above the surface, 8 in it). Two sealed lenses in the lid, three wet niches
        // under the floor so the holes glow.
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
        var missing = new[] { new Vector2(7.2f, -1.8f), new Vector2(9.0f, .6f), new Vector2(10.2f, -2.4f) };
        for (var x = 6f; x < 11.39f; x += .6f)
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
        kit.StepRun(new Vector3(11.4f, 0f, 0f), Vector3.left, 2.4f, -1.8f, 0f, tile, nosing);
        kit.Coping(new Vector3(11.4f, 0f, -3f), new Vector3(11.4f, 0f, -1.2f), 0f, tile);
        kit.Coping(new Vector3(11.4f, 0f, 1.2f), new Vector3(11.4f, 0f, 3f), 0f, tile);
        Lens(new Vector3(8.7f, Std - .05f, 0f));
        Lens(new Vector3(13.2f, Std - .05f, 0f));
        Niche(new Vector3(6.15f, -1.2f, -1.5f), Vector3.right);
        Niche(new Vector3(6.15f, -1.2f, 1.5f), Vector3.right);
        Niche(new Vector3(9f, -1.2f, -2.85f), Vector3.forward);
    }

    // ---------------------------------------------------------------- the sign (70 section 8, verbatim) and marks

    void BuildSigns()
    {
        // Flat on the corridor's east wall facing west, out of the main view (70 section 8).
        kit.Sign(new Vector3(5.85f, 1.5f, 3.5f), new Vector3(4.5f, 1.5f, 3.5f), "SP6 · 泳池层：深处的地形和大螺旋楼梯",
            "泳池层更深处的一段路，一路往下走。瓷砖走廊尽头是一道窄高的拱门；门后大厅的地面先是一层没过脚踝的水，再以 1:12 的坡度斜进没过腰的水里，尽头是半圆形的端墙。左边台阶上到带拱廊的台地，过一座有栏板的桥到白色的岛上。岛脚下是三道宽溢水堰，每道落差 0.6 米，旁边有台阶一级级往下。堰底的拱门通向比入口低 1.8 米的穹顶大厅：白色大螺旋楼梯绕着亮灯的井口升高 3 米到回廊，穹顶偏心开一个圆洞，光落在水面上成一个圆斑。回廊另一头有直跑楼梯，Relay 走这一条。楼梯脚下另有一段台阶走进水里，正对一条低矮的拱顶水洞，往里蹚 12 米，尽头是一个亮着灯的瓷砖壁龛。旁边还有高低错落、最低一层泡在水里的瓷砖块群，一间冷水池小间，和一个铺了检修地板、缺了几块板的水池。水都是关卡里那种不对劲的青绿色，最深 1.2 米，都能蹚过去；这里没有任何灯会闪。\n"
            + "灰盒占位：全部地形块、拱、穹顶和圆洞上方的灯井、楼梯和栏板细部、检修地板、水（没有焦散）；瓷砖用的是 150 毫米瓷白砖贴图（只在这个场景里）。黑色人形是 Relay 的静态替身（2.05 米），只用来检查视线，不会动。\n"
            + "要你决定：1 浅滩大厅、岛和水渠、溢水台地、穹顶回廊、水洞、瓷砖块群，各自放进泳池层吗？ 2 大螺旋楼梯的栏板用实心瓷砖（现在这样）还是玻璃（60 Q4）？ 3 冷水池小间和铺了检修地板的水池，各自放进泳池层吗？", 0f);
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

    public override void Guide(FrontRoomsStructures host, StringBuilder sb)
    {
        sb.AppendLine(Title);
        sb.AppendLine("一路往下走的泳池层深处：浅滩大厅 → 台地和桥 → 岛 → 溢水台地 → 穹顶大厅（大螺旋楼梯 B4-M、回廊、圆洞）→ 水洞；旁边是瓷砖块群、冷水池小间和检修地板水池。");
        sb.AppendLine();
        sb.AppendLine("按键：[ ] 换站点（走廊、台地、岛、溢水台地顶、穹顶回廊、穹顶甲板、水洞尽头、块群顶、检修地板水池）；R 回到当前站点；C Relay 体型（大螺旋楼梯对它关闭）；M 尺寸。");
        sb.AppendLine();
        sb.AppendLine("要你决定（在对话里按编号告诉总控）：");
        sb.AppendLine("SP6-1 浅滩大厅、岛和水渠、溢水台地、穹顶回廊、水洞、瓷砖块群，各自放进泳池层吗？");
        sb.AppendLine("SP6-2 大螺旋楼梯的栏板用实心瓷砖（现在这样）还是玻璃（60 Q4）？");
        sb.AppendLine("SP6-3 冷水池小间和铺了检修地板的水池，各自放进泳池层吗？");
        sb.AppendLine();
        sb.AppendLine("灰盒占位：全部地形块、拱、穹顶和圆洞上方的灯井、楼梯和栏板细部、检修地板、水（没有焦散）；瓷砖用的是 150 毫米瓷白砖贴图（只在这个场景里）。黑色人形是 Relay 的静态替身（2.05 米），只用来检查视线，不会动。");
    }
}
