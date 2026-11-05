using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// SP2 (key 2) of scene 4 (RULES 5b; research/level_space/70 section 4, 20 section 3, 40 sections 7.5 and 11.4, 60 section 2):
/// the well, one 3 x 3 cell void through four storeys stacked for real, so Red judges the depth on the spot. Plates of 8 x 7
/// cells (x 0..24, z 0..21), the void in cells i 2..4, j 3..5 (x 6..15, z 9..18), off the plate's centre on both axes (R4),
/// the same column on every floor. F0 the pool hall (deck +-0, tiled lid 8.7, a gallery at +4.5 on the void's east side),
/// F1 Run!'s service floor (+9.0), F2 the Office (+12.9), F3 Level 0 (+16.8). Every rim is a different outline (40 7.5):
/// F3 galleries south and east with a wallpaper half-wall and oak cap, clear glass in the arrival bay, a 1 x 1 notch onto
/// the office below, windows from two west rooms, a door onto the drop guarded across its frame, a glass-block floor light;
/// F2 glass guards west and north with one balcony bay two risers down, the one bridge (A6) on a 0.9 m beam, glass fronts
/// east and south; F1 a mesh guard on a block upstand east, block walls elsewhere, a slot at the south wall's foot whose
/// water runs 9 m down F0's tiled wall into a splash trough and the basin (C5); F0 the basin 1.07 deep, a round-arch arcade
/// on the west only, the cascade wall south, the B4-L (3 x 3 cells, 1.45 m, 30 x 0.15, about 185 degrees) under a square
/// laylight. One B3 core at the east edge joins all five levels and is the Relay's way. Each floor has its own finish and
/// lamps; the look follows the floor the walker stands on. Greybox where no kit exists, listed on the sign.
/// </summary>
public sealed class FrontRoomsStructuresWell : FrontRoomsStructuresDistrict
{
    const float Gal = 4.5f, F1 = 9.0f, F2 = 12.9f, F3 = 16.8f;
    const float Lid0 = 8.7f, F1Ceil = 11.9f, F2Ceil = 15.8f, F3Ceil = 19.7f;
    const float B4LStep = 5.818f, B4LLanding = 22.6f;
    static readonly Vector3 Arrival = new Vector3(10.5f, F3, 8.0f);
    static readonly Vector3 B4L = new Vector3(19.5f, 0f, 4.5f);

    FrontRoomsStructuresKit kit;
    Material tile, nosing, runLower, runUpper, runFloor, runSoffit, offWall, offCarpet, offCeil, l0Wall, l0Carpet, l0Ceil, concrete, stairBand, plenum, glass, oak, glassBlock, sheet;

    public FrontRoomsStructuresWell(Vector3 site)
        : base(2, "SP2", "SP2 · 井：穿过整栋楼的一口竖井", "四层叠起来的竖井：从 Level 0 的玻璃栏板往下看到泳池。", site) { }

    // ---------------------------------------------------------------- build

    public override void Build(FrontRoomsStructures host)
    {
        kit = new FrontRoomsStructuresKit(host, this);
        tile = FrontRoomsPoolMaterials.Tile;
        nosing = FrontRoomsPoolMaterials.Nosing;
        runLower = FrontRoomsPoolMaterials.Surface("RunB_BlockLower", new Color(.55f, .56f, .55f));
        runUpper = FrontRoomsPoolMaterials.Surface("RunB_BlockUpper", new Color(.78f, .78f, .74f));
        runFloor = FrontRoomsPoolMaterials.Surface("RunB_FloorWax", new Color(.62f, .61f, .58f));
        runSoffit = FrontRoomsPoolMaterials.Surface("RunB_Soffit", new Color(.7f, .7f, .68f));
        offWall = FrontRoomsPoolMaterials.Surface("Office_Wall", new Color(.82f, .80f, .74f));
        offCarpet = FrontRoomsPoolMaterials.Surface("Office_Carpet", new Color(.35f, .37f, .40f));
        offCeil = FrontRoomsPoolMaterials.Surface("Office_Ceiling", new Color(.85f, .85f, .82f));
        l0Wall = FrontRoomsPoolMaterials.Surface("L0_Wallpaper", new Color(.80f, .74f, .48f));
        l0Carpet = FrontRoomsPoolMaterials.Surface("L0_Carpet", new Color(.62f, .55f, .36f));
        l0Ceil = FrontRoomsPoolMaterials.Surface("L0_Ceiling", new Color(.86f, .83f, .70f));
        concrete = FrontRoomsPoolMaterials.Surface("Stair_Slab", new Color(.6f, .6f, .58f));
        stairBand = FrontRoomsPoolMaterials.Surface("Stair_Band", new Color(.3f, .3f, .3f));
        plenum = FrontRoomsPoolMaterials.Surface("Plenum_Dark", new Color(.03f, .03f, .03f));
        oak = FrontRoomsPoolMaterials.Surface("Prop_WoodOak", new Color(.55f, .40f, .25f));
        glass = FrontRoomsStructuresKit.GlassMaterial();
        glassBlock = FrontRoomsPoolMaterials.Emissive("SP2 glass-block floor light (E7)", new Color(.80f, .86f, .84f), .7f);
        if (glassBlock != null && glassBlock.HasProperty("_EmissionColor")) glassBlock.SetColor("_EmissionColor", new Color(.95f, .97f, .9f) * 1.3f);
        sheet = FrontRoomsPoolMaterials.Transparent("water sheet", new Color(.55f, .85f, .86f, .32f), .95f);

        kit.Safe("F0 pool hall", BuildF0);
        kit.Safe("B4-L", BuildB4L);
        kit.Safe("gallery", BuildGallery);
        kit.Safe("core", BuildCore);
        kit.Safe("F1 Run!", BuildF1);
        kit.Safe("F2 Office", BuildF2);
        kit.Safe("bridge", BuildBridge);
        kit.Safe("F3 Level 0", BuildF3);
        kit.Safe("rims", BuildRims);
        kit.Safe("cascade", BuildCascade);
        kit.Safe("lamps", BuildLamps);
        kit.Finish();
        kit.Safe("office dressing", DressOffices);
        kit.Safe("zone volumes", BuildVolumes);
        kit.Safe("signs", BuildSigns);

        Box = new Bounds(Site + new Vector3(12f, 9.6f, 10.5f), new Vector3(28f, 24f, 25f));
        LowestFloor = Site.y - 1.12f;
        Far = 80f;
        Stops.Clear();
        kit.Stop("F3 · Level 0 玻璃栏板后（到达点）", Arrival, new Vector3(10.5f, F3 + 1.62f, 21f));
        kit.Stop("F3 · 玻璃栏板前，往下看", new Vector3(9.75f, F3, 8.55f), new Vector3(9.75f, 0f, 16.0f), 70f);
        kit.Stop("F2 · 办公层西边栏杆（对面是 Relay 替身）", new Vector3(4.6f, F2, 10.5f), new Vector3(16.2f, F1 + 1.6f, 10.5f));
        kit.Stop("F2 · 桥上", new Vector3(7.5f, F2, 13.5f), new Vector3(15f, F2 + 1.2f, 13.5f));
        kit.Stop("F1 · Run! 设备层东边", new Vector3(19.5f, F1, 13.5f), new Vector3(10.5f, F1 + .3f, 9.2f));
        kit.Stop("F0 · 画廊 +4.5", new Vector3(15.6f, Gal, 13.5f), new Vector3(8.0f, 0f, 13.5f), 70f);
        kit.Stop("F0 · 大圆楼梯顶", new Vector3(19.2f, Gal, 8.3f), new Vector3(22.6f, 2.0f, 4.5f), 45f);
        kit.Stop("F0 · 大圆楼梯底，抬头看天窗", new Vector3(18.6f, 0f, .9f), new Vector3(19.5f, Lid0, 4.5f), 70f);
        kit.Stop("F0 · 池边，抬头看井", new Vector3(10.5f, 0f, 19.6f), new Vector3(10.5f, F3Ceil, 12.0f), 70f);
        kit.Stop("F1 · 楼梯核（Relay 的路），往上看", new Vector3(21.8f, F1, 10.4f), new Vector3(21.8f, F1 + 2.4f, 16.5f), 45f);

        try { host.Look.Probe(Root, new Bounds(Site + new Vector3(12f, Lid0 * .5f, 10.5f), new Vector3(24f, Lid0, 21f)), 1024); }
        catch (Exception e) { FrontRoomsPoolMaterials.LogOnce("SP2 probe", "SP2 probe: " + e.Message); }
    }

    // ---------------------------------------------------------------- F0: the pool hall, the basin, the arcade, the cascade wall

    void BuildF0()
    {
        kit.Shell(Lid0, 0f);
        // The deck (porcelain, 0.3 slab) round the basin under the void.
        kit.Slab(0f, 0f, 6f, 21f, 0f, tile, .3f);
        kit.Slab(6f, 0f, 15f, 9f, 0f, tile, .3f);
        kit.Slab(6f, 18f, 15f, 21f, 0f, tile, .3f);
        kit.Slab(15f, 0f, 24f, 21f, 0f, tile, .3f);
        // Outer walls outside the plate's lines, inner faces on them (the B4-L's ribbon touches x 24 and z 0; 70 F29).
        const float t = .3f, top = Lid0 + .16f;
        kit.WallZ(-t * .5f, -t, 21f + t, 0f, top, tile, null, t);
        kit.WallX(21f + t * .5f, -t, 24f + t, 0f, top, tile, null, t);
        kit.WallX(-t * .5f, -t, 24f + t, 0f, top, tile, null, t);
        kit.WallZ(24f + t * .5f, -t, 9f, 0f, top, tile, null, t);
        kit.WallZ(24f + t * .5f, 18f, 21f + t, 0f, top, tile, null, t);
        // The tiled lid (8.7 soffit) over every cell but the void, the core's shaft and the B4-L's laylight.
        kit.Lid(0f, 0f, 6f, 21f, Lid0, tile);
        kit.Lid(6f, 0f, 15f, 9f, Lid0, tile);
        kit.Lid(6f, 18f, 15f, 21f, Lid0, tile);
        kit.Lid(15f, 7.5f, 21f, 21f, Lid0, tile);
        kit.Lid(21f, 18f, 24f, 21f, Lid0, tile);
        kit.Lid(21f, 7.5f, 24f, 9f, Lid0, tile);
        kit.Lid(15f, 0f, 24f, 1.5f, Lid0, tile);
        kit.Lid(15f, 1.5f, 16.5f, 7.5f, Lid0, tile);
        kit.Lid(22.5f, 1.5f, 24f, 7.5f, Lid0, tile);

        // The basin under the void: 1.07 m of level water ("wrong" turquoise), its surface 50 mm under the deck, floor -1.12.
        kit.Shell(Lid0, -1.12f);
        kit.Basin(6f, 9f, 15f, 18f, -1.12f, -.3f, tile);
        kit.Water(6f, 9f, 15f, 18f, -.05f);
        // Steps into the water at the north-east corner (C7): the way out for anyone in it, 7 risers of 0.16, a nosing on each.
        kit.Flight(new Vector3(13.5f, -1.12f, 16.2f), Vector3.forward, 2.0f, 7, .16f, .3f, tile, nosing, true);
        // A second way out (R6; review SP2-F12): steps rising west into the arcade's middle arch (z 12.6..14.4), their last
        // riser on the basin edge at x 6, clear of the underwater lamps at z 11.5 and 15.5; the arch's coping is cut there.
        kit.Flight(new Vector3(7.8f, -1.12f, 13.5f), Vector3.left, 1.8f, 7, .16f, .3f, tile, nosing, true);
        kit.Shell(Lid0, 0f);
        kit.Coping(new Vector3(6f, 0f, 18f), new Vector3(12.5f, 0f, 18f), 0f, tile);
        kit.Coping(new Vector3(14.5f, 0f, 18f), new Vector3(15f, 0f, 18f), 0f, tile);
        kit.Coping(new Vector3(15f, 0f, 9.0f), new Vector3(15f, 0f, 18f), 0f, tile);

        // The arcade (D2) on the west side only, under the void's edge: three round arches 2.4 wide on 0.6 piers, springing
        // 4.5 (crown 5.7), the wall above carrying F1's west block wall. The arches open from the aisle onto the coping.
        var arches = new List<FrontRoomsStructuresKit.Opening>
        {
            new FrontRoomsStructuresKit.Opening(9.3f, 11.7f, 0f, 4.5f, true),
            new FrontRoomsStructuresKit.Opening(12.3f, 14.7f, 0f, 4.5f, true),
            new FrontRoomsStructuresKit.Opening(15.3f, 17.7f, 0f, 4.5f, true),
        };
        // The arcade's east face on the basin edge (x 5.4..6.0): its piers stood 0.3 m in the water (review SP2-F13).
        kit.WallZ(5.7f, 8.85f, 18.3f, 0f, top, tile, arches, .6f);
        foreach (var a in arches)
        {
            if (a.from < 13.5f && a.to > 13.5f)
            {
                kit.Coping(new Vector3(6f, 0f, a.from), new Vector3(6f, 0f, 12.6f), 0f, tile);
                kit.Coping(new Vector3(6f, 0f, 14.4f), new Vector3(6f, 0f, a.to), 0f, tile);
            }
            else kit.Coping(new Vector3(6f, 0f, a.from), new Vector3(6f, 0f, a.to), 0f, tile);
        }
        // The cascade wall on the void's south edge, deck to the lid: F1's water runs down its north face (70 F26).
        kit.WallX(8.85f, 6f, 15f, 0f, top, tile, null, .3f);   // z 8.7..9.0: its north face on the basin edge (review SP2-F13)
    }

    // ---------------------------------------------------------------- the B4-L and its laylight (60 section 2)

    void BuildB4L()
    {
        kit.Shell(Lid0, 0f);
        // 30 risers of 0.15 from the deck to the gallery (+4.5), 1.45 wide on a 2.95 inner radius (9.0 m outside with the
        // 0.10 ribbons, exactly 3 x 3 cells), 5.82 degrees a tread on the walk line, one mid landing of 22.6 degrees after
        // riser 15: 185.5 degrees. The last riser on the north radius, its landing a bump-out of the gallery (60 2.5).
        var sweep = 28f * B4LStep + B4LLanding;
        var start = 90f - sweep;
        kit.Spiral(B4L, 2.95f, 4.40f, start, B4LStep, 30, .15f, 0f, tile, nosing, .3f, 14, B4LLanding, .10f, .10f, tile, FrontRoomsStructuresKit.GuardHeight);
        // The top landing at +4.5 (4.5 x 1.55 m along the module's north edge) and its parapet, the ribbon run on straight.
        kit.Shell(Lid0, Gal);
        kit.Slab(15f, 7.45f, 19.5f, 9.0f, Gal, tile, .3f);
        kit.Guard(new Vector3(15.1f, 0f, 7.5f), new Vector3(19.45f, 0f, 7.5f), Gal, FrontRoomsStructuresKit.GuardStyle.TileWall);
        kit.Shell(Lid0, 0f);
        // Step lights in the inner ribbon on every third tread (0.22 x 0.07 lens, 0.26 over the tread, aimed down onto it).
        var a = start;
        for (var k = 0; k < 29; k++)
        {
            var span = k == 14 ? B4LLanding : B4LStep;
            if (k % 3 == 2 && k != 14)
            {
                var mid = (a + span * .5f) * Mathf.Deg2Rad;
                var radial = new Vector3(Mathf.Cos(mid), 0f, Mathf.Sin(mid));
                var p = B4L + radial * 3.0f;
                p.y = (k + 1) * .15f + .26f;
                kit.Lamp("B4-L step light", LightType.Spot, p, (radial * .6f + Vector3.down).normalized, 110f, 70f, .55f, 1.8f, new Color(.90f, .96f, 1f), false,
                    new Vector3(.22f, .07f, .03f), FrontRoomsPoolMaterials.Steel, new Color(1.6f, 1.7f, 1.75f));
            }
            a += span;
        }
        // Six floor uplights in the well grazing the inner ribbon (60 section 4).
        foreach (var deg in new[] { -80f, -45f, -10f, 25f, 55f, 80f })
        {
            var r = new Vector3(Mathf.Cos(deg * Mathf.Deg2Rad), 0f, Mathf.Sin(deg * Mathf.Deg2Rad));
            kit.Lamp("B4-L uplight", LightType.Spot, B4L + r * 2.55f + Vector3.up * .05f, (r * .45f + Vector3.up).normalized, 50f, 30f, 1.4f, 6f, new Color(.90f, .96f, 1f), false,
                new Vector3(.16f, .16f, .06f), FrontRoomsPoolMaterials.Steel, new Color(1.4f, 1.5f, 1.55f));
        }
        // The square laylight on the grid over the well (6 x 6 m; 60 Q2 asks laylight or dome): a tiled coffer through the
        // lid, a lit diffuser on a steel grid of 1.5 m, one wide lamp above it.
        const float ly = Lid0, lt = 9.45f;
        kit.Box(16.35f, ly, 1.35f, 16.5f, lt, 7.65f, tile, false);
        kit.Box(22.5f, ly, 1.35f, 22.65f, lt, 7.65f, tile, false);
        kit.Box(16.5f, ly, 1.35f, 22.5f, lt, 1.5f, tile, false);
        kit.Box(16.5f, ly, 7.5f, 22.5f, lt, 7.65f, tile, false);
        var diffuser = FrontRoomsPoolMaterials.Glow("SP2 laylight diffuser", new Color(1.35f, 1.42f, 1.45f));
        kit.QuadPlanar(new Vector3(16.5f, 9.3f, 1.5f), new Vector3(22.5f, 9.3f, 1.5f), new Vector3(22.5f, 9.3f, 7.5f), new Vector3(16.5f, 9.3f, 7.5f), Vector3.down, diffuser);
        for (var g = 1; g < 4; g++)
        {
            kit.Box(16.5f + g * 1.5f - .025f, 9.22f, 1.5f, 16.5f + g * 1.5f + .025f, 9.3f, 7.5f, FrontRoomsPoolMaterials.Steel, false);
            kit.Box(16.5f, 9.22f, 1.5f + g * 1.5f - .025f, 22.5f, 9.3f, 1.5f + g * 1.5f + .025f, FrontRoomsPoolMaterials.Steel, false);
        }
        kit.Lamp("B4-L laylight", LightType.Spot, new Vector3(19.5f, 9.2f, 4.5f), Vector3.down, 130f, 90f, 7f, 14f, new Color(.92f, .97f, 1f), true, Vector3.zero, null, Color.black);
        // Never the Relay's way (R6; Red 10-31 ~16:15, ~16:38): closed at both ends in the Relay-body test.
        kit.RelayBlocker("B4-L foot", new Vector3(17.9f, 0f, 0f), new Vector3(19.15f, 2.6f, 1.8f));
        kit.RelayBlocker("B4-L top", new Vector3(18.2f, Gal, 7.45f), new Vector3(19.5f, Gal + 2.6f, 9f));
    }

    // ---------------------------------------------------------------- the gallery at +4.5 (the first pool storey)

    void BuildGallery()
    {
        kit.Shell(Lid0, Gal);
        kit.Slab(15f, 9f, 21f, 18f, Gal, tile, .3f);
        // Two tiled columns under its void edge, just behind the coping.
        kit.Shell(Gal - .3f, 0f);
        kit.Box(15.4f, 0f, 11.75f, 15.9f, Gal - .3f, 12.25f, tile);
        kit.Box(15.4f, 0f, 14.75f, 15.9f, Gal - .3f, 15.25f, tile);
        // Tiled guards 1.07: the well's foot (west, run on from the landing's parapet), the north edge, the south edge east
        // of the B4-L's landing.
        kit.Shell(Lid0, Gal);
        var g = FrontRoomsStructuresKit.GuardStyle.TileWall;
        kit.Guard(new Vector3(15.1f, 0f, 7.45f), new Vector3(15.1f, 0f, 18f), Gal, g);
        kit.Guard(new Vector3(15f, 0f, 17.9f), new Vector3(20.92f, 0f, 17.9f), Gal, g);
        kit.Guard(new Vector3(19.5f, 0f, 9.1f), new Vector3(20.92f, 0f, 9.1f), Gal, g);
    }

    // ---------------------------------------------------------------- the stair core: one B3 for all five levels

    void BuildCore()
    {
        // 1 x 3 cells at the plate's east edge (x 21..24, z 9..18); walls 0.2 on the lines, each storey in its own finish;
        // a door on its west wall at every level onto the floor's start landing (z 9.1..11.9).
        float[] levels = { 0f, Gal, F1, F2, F3 };
        float[] bandFloor = { 0f, F1, F2, F3 };
        float[] bandTop = { F1, F2, F3, F3Ceil + .16f };
        float[] bandCeiling = { Lid0, F1Ceil, F2Ceil, F3Ceil };
        Material[] bandMat = { tile, runUpper, offWall, l0Wall };
        for (var b = 0; b < 4; b++)
        {
            kit.Shell(bandCeiling[b], bandFloor[b]);
            var doors = new List<FrontRoomsStructuresKit.Opening>();
            foreach (var l in levels)
                if (l >= bandFloor[b] - 1e-3f && l < bandTop[b] - .5f) doors.Add(new FrontRoomsStructuresKit.Opening(9.6f, 11.4f, l, l + 2.2f));
            kit.WallZ(21f, 8.9f, 18.1f, bandFloor[b], bandTop[b], bandMat[b], doors, .2f);
            kit.WallZ(24f, 8.9f, 18.1f, bandFloor[b], bandTop[b], bandMat[b], null, .2f);
            kit.WallX(9f, 21.1f, 23.9f, bandFloor[b], bandTop[b], bandMat[b], null, .2f);
            kit.WallX(18f, 21.1f, 23.9f, bandFloor[b], bandTop[b], bandMat[b], null, .2f);
        }
        // Flights: 15 risers of 0.15 a flight in a pool storey, 13 in an office storey; start landings 2.8 deep.
        kit.Shell(Lid0, 0f);
        kit.CoreB3(21.1f, 23.9f, 9.1f, 17.9f, 2.8f, new[] { 0f, Gal }, tile, nosing, tile, tile);
        kit.CoreB3(21.1f, 23.9f, 9.1f, 17.9f, 2.8f, new[] { Gal, F1 }, tile, nosing, tile, tile);
        kit.Shell(F2 - .2f, F1);
        kit.CoreB3(21.1f, 23.9f, 9.1f, 17.9f, 2.8f, new[] { F1, F2 }, concrete, stairBand, concrete, runUpper);
        kit.Shell(F3 - .2f, F2);
        kit.CoreB3(21.1f, 23.9f, 9.1f, 17.9f, 2.8f, new[] { F2, F3 }, concrete, stairBand, concrete, offWall, true);
        kit.Shell(F3Ceil, F3);
        kit.Lid(21f, 9f, 24f, 18f, F3Ceil, l0Ceil);
        // A lamp over every landing (R8), each storey's colour: pool cool, Run! fluorescent, office cool, Level 0 warm.
        Color[] tint = { new Color(.90f, .97f, 1f), new Color(.90f, .97f, 1f), new Color(.93f, .97f, 1f), new Color(.96f, .97f, 1f), new Color(1f, .93f, .80f) };
        float[] halves = { 2.25f, 2.25f, 1.95f, 1.95f };
        // One wall lamp a flight at mid-run on the scissor wall (review SP2-F20: mid-flight treads sat 4.7-5.3 m from both
        // landing lamps, past the stair finishes' own-light range), range 5.
        for (var k = 0; k < halves.Length; k++)
        {
            var run = halves[k] / .15f * .3f - .3f;
            var midZ = 11.9f + run * .5f;
            kit.Lamp("core flight", LightType.Spot, new Vector3(22.42f, levels[k] + halves[k] * .5f + 2.0f, midZ), new Vector3(-1f, -1.6f, 0f), 120f, 80f, 1.2f, 5f, tint[k], false,
                new Vector3(.22f, .1f, .06f), FrontRoomsPoolMaterials.Steel, new Color(1.4f, 1.45f, 1.45f));
            kit.Lamp("core flight", LightType.Spot, new Vector3(22.58f, levels[k] + halves[k] * 1.5f + 2.0f, midZ), new Vector3(1f, -1.6f, 0f), 120f, 80f, 1.2f, 5f, tint[k], false,
                new Vector3(.22f, .1f, .06f), FrontRoomsPoolMaterials.Steel, new Color(1.4f, 1.45f, 1.45f));
        }
        for (var k = 0; k < levels.Length; k++)
        {
            kit.Lamp("core landing", LightType.Spot, new Vector3(22.5f, levels[k] + 2.45f, 9.25f), Vector3.down, 150f, 110f, 1.6f, 5f, tint[k], false,
                new Vector3(.6f, .12f, .08f), FrontRoomsPoolMaterials.Steel, new Color(1.5f, 1.55f, 1.55f));
            if (k < halves.Length)
                kit.Lamp("core far landing", LightType.Spot, new Vector3(22.5f, levels[k] + halves[k] + 2.35f, 17.75f), Vector3.down, 150f, 110f, 1.4f, 5f, tint[k], false,
                    new Vector3(.6f, .12f, .08f), FrontRoomsPoolMaterials.Steel, new Color(1.5f, 1.55f, 1.55f));
        }
    }

    // ---------------------------------------------------------------- F1: Run!'s service floor

    void BuildF1()
    {
        var p = new FrontRoomsStructuresKit.Plate(0f, 0f, F1, 8, 7)
        {
            WallMat = runUpper, FloorMat = runFloor, FloorThick = .14f, CeilingMat = runSoffit, LedgeMat = runUpper,
            Troffers = false, VoidGuard = FrontRoomsStructuresKit.GuardStyle.None, SectionMat = null,
        };
        var floor = FrontRoomsStructuresKit.Plate.Kind.Floor;
        // The service bay beside the void stops at the B4-L's module row (j 2 is the B4-L's, open to its laylight: 60 section 2;
        // scene 4 walk and look review SP2-X1, SP2-F2); the narrow room runs on south to j 2 and gives the water room its door.
        p.Fill(5, 3, 6, 5, floor, 0f, 2.9f, 1);        // the service bay on the void's east side
        p.Fill(0, 6, 7, 6, floor, 0f, 2.9f, 1);        // the service corridor along the north
        p.Fill(1, 2, 1, 5, floor, 0f, 2.9f, 2);        // a narrow room behind the west block wall
        p.Fill(2, 2, 4, 2, floor, 0f, 2.9f, 3);        // the water room behind the south wall
        p.Fill(2, 3, 4, 5, FrontRoomsStructuresKit.Plate.Kind.Void, 0f, 0f, -1);
        for (var j = 3; j <= 5; j++) p.NoWall(6, j, 'E');
        p.NoWall(7, 6, 'S');
        // Block walls on the void's west, south and north edges (70 F26); a mesh guard on the east, built below.
        for (var j = 3; j <= 5; j++) p.ForceWall(1, j, 'E');
        for (var i = 2; i <= 4; i++) { p.ForceWall(i, 2, 'N'); p.ForceWall(i, 6, 'S'); }
        p.Open(3, 2, 'N', 0f, 3f, 0f, .3f);                                   // the slot at the south wall's foot, 3 m x 0.3
        p.Open(2, 6, 'S', 1.5f, 3f, 1.0f, 2.1f);                              // one long window in the north wall (x 7.5..13.5)
        p.Open(3, 6, 'S', 0f, 3f, 1.0f, 2.1f);
        p.Open(4, 6, 'S', 0f, 1.5f, 1.0f, 2.1f);
        p.Open(1, 5, 'N', 1f, 2f, 0f, 2.1f);
        p.Open(2, 2, 'W', 1f, 2f, 0f, 2.1f);                                   // the water room's door, from the narrow room
        p.Build(kit);
        kit.Shell(F1Ceil, F1);
        kit.Prism(new Vector3(7.5f, 0f, 18f), new Vector3(13.5f, 0f, 18f), F1 + 1.0f, F1 + 2.1f, .012f, glass, true);
        // Run! B's two tones: the lower tone to 1.2 m along the corridor's long wall and the bay's east wall.
        kit.Prism(new Vector3(0f, 0f, 20.91f), new Vector3(24f, 0f, 20.91f), F1, F1 + 1.2f, .012f, runLower, false);
        // ... and on the block walls' void faces (west x 6, south z 9 but its slot, north z 18 under the long window) and the
        // service bay's south wall, as Run! B lays it on every face (review SP2-F15).
        kit.Prism(new Vector3(6.09f, 0f, 9.08f), new Vector3(6.09f, 0f, 17.92f), F1, F1 + 1.2f, .012f, runLower, false);
        kit.Prism(new Vector3(6.08f, 0f, 9.09f), new Vector3(9f, 0f, 9.09f), F1, F1 + 1.2f, .012f, runLower, false);
        kit.Prism(new Vector3(9f, 0f, 9.09f), new Vector3(12f, 0f, 9.09f), F1 + .3f, F1 + 1.2f, .012f, runLower, false);
        kit.Prism(new Vector3(12f, 0f, 9.09f), new Vector3(15f, 0f, 9.09f), F1, F1 + 1.2f, .012f, runLower, false);
        kit.Prism(new Vector3(6.08f, 0f, 17.91f), new Vector3(7.5f, 0f, 17.91f), F1, F1 + 1.2f, .012f, runLower, false);
        kit.Prism(new Vector3(7.5f, 0f, 17.91f), new Vector3(13.5f, 0f, 17.91f), F1, F1 + 1.0f, .012f, runLower, false);
        kit.Prism(new Vector3(13.5f, 0f, 17.91f), new Vector3(15f, 0f, 17.91f), F1, F1 + 1.2f, .012f, runLower, false);
        kit.Prism(new Vector3(15.3f, 0f, 9.09f), new Vector3(20.92f, 0f, 9.09f), F1, F1 + 1.2f, .012f, runLower, false);
        // The east rim: a 0.6 m block upstand and a galvanised chain-link guard on it (E8).
        kit.Box(15f, F1, 9f, 15.3f, F1 + .6f, 18f, runLower);
        kit.Guard(new Vector3(15.15f, 0f, 9f), new Vector3(15.15f, 0f, 18f), F1 + .6f, FrontRoomsStructuresKit.GuardStyle.Mesh, null, null, 1.2f);
        // The Relay stand-in behind the mesh, seen across the void from F2's west guard (COMMON rule 12).
        kit.Relay(new Vector3(16.3f, F1, 10.5f), new Vector3(6f, F1, 10.5f));
    }

    // ---------------------------------------------------------------- F2: the Office

    void BuildF2()
    {
        var p = new FrontRoomsStructuresKit.Plate(0f, 0f, F2, 8, 7)
        {
            WallMat = offWall, FloorMat = offCarpet, FloorThick = .05f, CeilingMat = offCeil, LedgeMat = offWall, TreadMat = offCarpet,
            Troffers = true, Office = true, VoidGuard = FrontRoomsStructuresKit.GuardStyle.Glass, SectionMat = null,
        };
        var floor = FrontRoomsStructuresKit.Plate.Kind.Floor;
        p.Fill(1, 3, 1, 6, floor, 0f, 2.9f, 1);        // the west gallery
        p.Fill(2, 6, 4, 6, floor, 0f, 2.9f, 1);        // the north gallery
        p.Fill(3, 6, 3, 6, floor, -.3f, 2.9f, 1);      // its balcony bay, 0.30 down (A4 at the rim)
        p.Fill(5, 1, 6, 6, floor, 0f, 2.9f, 2);        // the east office
        p.Fill(2, 1, 4, 2, floor, 0f, 2.9f, 3);        // the south office
        p.Fill(2, 3, 4, 5, FrontRoomsStructuresKit.Plate.Kind.Void, 0f, 0f, -1);
        for (var j = 3; j <= 5; j++) p.NoWall(6, j, 'E');
        // Glass fronts (E3) on the void's east and south edges; on the east, the bridge's door.
        for (var j = 3; j <= 5; j++) p.ForceWall(5, j, 'W');
        for (var i = 2; i <= 4; i++) p.ForceWall(i, 2, 'N');
        p.Open(5, 3, 'W', .15f, 2.85f, .1f, 2.6f);
        p.Open(5, 4, 'W', .15f, .6f, .1f, 2.6f);
        p.Open(5, 4, 'W', .75f, 2.25f, 0f, 2.2f);
        p.Open(5, 4, 'W', 2.4f, 2.85f, .1f, 2.6f);
        p.Open(5, 5, 'W', .15f, 2.85f, .1f, 2.6f);
        for (var i = 2; i <= 4; i++) p.Open(i, 2, 'N', .15f, 2.85f, .1f, 2.6f);
        // The balcony bay: two risers at each side, the glass guard run on at its lower floor.
        p.Run(3, 6, 'W', 0f, 3f);
        p.Run(3, 6, 'E', 0f, 3f);
        p.Crossed(1, 4, 'E', .75f, 2.25f);              // the bridge leaves the west gallery here
        p.Open(4, 1, 'E', 1f, 2f, 0f, 2.1f);             // south office to east office
        p.Open(4, 6, 'E', 1f, 2f, 0f, 2.1f);             // north gallery to the east office
        p.NoLid(4, 2, 4, 2);                             // F3's notch looks down here
        p.NoLid(5, 4, 5, 5);                             // under F3's glass-block floor light
        p.Build(kit);
        // The glass in the fronts.
        kit.Shell(F2Ceil, F2);
        for (var j = 3; j <= 5; j++)
        {
            if (j == 4)
            {
                kit.Prism(new Vector3(15f, 0f, 12.15f), new Vector3(15f, 0f, 12.6f), F2 + .1f, F2 + 2.6f, .012f, glass, true);
                kit.Prism(new Vector3(15f, 0f, 14.4f), new Vector3(15f, 0f, 14.85f), F2 + .1f, F2 + 2.6f, .012f, glass, true);
            }
            else kit.Prism(new Vector3(15f, 0f, 3f * j + .15f), new Vector3(15f, 0f, 3f * j + 2.85f), F2 + .1f, F2 + 2.6f, .012f, glass, true);
        }
        for (var i = 2; i <= 4; i++) kit.Prism(new Vector3(3f * i + .15f, 0f, 9f), new Vector3(3f * i + 2.85f, 0f, 9f), F2 + .1f, F2 + 2.6f, .012f, glass, true);
        // The glass-block light well under F3's floor light: lined in the office wall, a lamp under the blocks.
        kit.Shell(F3, F2Ceil);
        kit.Box(15.2f, F2Ceil, 12f, 18f, F3 - .08f, 12.1f, offWall, false);
        kit.Box(15.2f, F2Ceil, 17.9f, 18f, F3 - .08f, 18f, offWall, false);
        kit.Box(17.9f, F2Ceil, 12.1f, 18f, F3 - .08f, 17.9f, offWall, false);
        kit.Lamp("glass-block light (from beneath)", LightType.Spot, new Vector3(16.6f, F2Ceil + .1f, 15f), Vector3.up, 120f, 80f, 2.2f, 2.5f, new Color(.96f, .97f, 1f), false, Vector3.zero, null, Color.black);
    }

    // The bridge (A6), on F2 only: across the void on row j 4 from the west gallery to the east office's door, 1.5 m wide,
    // glass guards 1.07, carried from below on a 0.9 m deep beam in the office wall finish, bearing on both rims (40 11.3).
    void BuildBridge()
    {
        kit.Shell(F2Ceil, F2);
        kit.Box(5.8f, F2 - .2f, 12.75f, 15.2f, F2, 14.25f, offCarpet);
        kit.Box(5.7f, F2 - 1.1f, 12.9f, 15.3f, F2 - .2f, 14.1f, offWall);
        kit.Guard(new Vector3(6f, 0f, 12.8f), new Vector3(15f, 0f, 12.8f), F2, FrontRoomsStructuresKit.GuardStyle.Glass);
        kit.Guard(new Vector3(6f, 0f, 14.2f), new Vector3(15f, 0f, 14.2f), F2, FrontRoomsStructuresKit.GuardStyle.Glass);
    }

    // ---------------------------------------------------------------- F3: Level 0 (the cut, V1)

    void BuildF3()
    {
        var p = new FrontRoomsStructuresKit.Plate(0f, 0f, F3, 8, 7)
        {
            WallMat = l0Wall, FloorMat = l0Carpet, FloorThick = .05f, CeilingMat = l0Ceil, LedgeMat = l0Wall,
            Troffers = true, Office = false, VoidGuard = FrontRoomsStructuresKit.GuardStyle.HalfWall, GuardWall = l0Wall, GuardCap = oak, SectionMat = null,
        };
        var floor = FrontRoomsStructuresKit.Plate.Kind.Floor;
        var voidKind = FrontRoomsStructuresKit.Plate.Kind.Void;
        p.Fill(2, 3, 4, 5, voidKind, 0f, 0f, -1);
        p.Fill(4, 2, 4, 2, voidKind, 0f, 0f, -1);      // the notch: one bay of the south gallery jogs back a cell (70 F28)
        // The gallery: south (round the notch) and east, and the core's lobby.
        int[,] gallery = { { 2, 2 }, { 3, 2 }, { 3, 1 }, { 4, 1 }, { 5, 1 }, { 5, 2 }, { 5, 3 }, { 6, 3 } };
        for (var k = 0; k < gallery.GetLength(0); k++) p.Fill(gallery[k, 0], gallery[k, 1], gallery[k, 0], gallery[k, 1], floor, 0f, 2.9f, 1);
        p.Fill(5, 4, 5, 5, FrontRoomsStructuresKit.Plate.Kind.Held, 0f, 2.9f, 1);   // the glass-block floor light, built below
        p.Fill(0, 3, 1, 4, floor, 0f, 2.9f, 2);
        p.Fill(0, 5, 1, 5, floor, 0f, 2.9f, 3);
        p.Fill(0, 6, 1, 6, floor, 0f, 2.9f, 4);
        p.Fill(2, 6, 4, 6, floor, 0f, 2.9f, 5);
        p.Fill(5, 6, 7, 6, floor, 0f, 2.9f, 6);
        p.Fill(0, 0, 1, 2, floor, 0f, 2.9f, 7);
        p.Fill(2, 0, 2, 1, floor, 0f, 2.9f, 8);
        p.Fill(3, 0, 5, 0, floor, 0f, 2.9f, 9);
        p.Fill(6, 0, 7, 2, floor, 0f, 2.9f, 10);
        p.Fill(6, 4, 6, 5, floor, 0f, 2.9f, 11);
        for (var j = 3; j <= 5; j++) p.NoWall(6, j, 'E');
        p.NoWall(7, 2, 'N');
        p.NoWall(7, 6, 'S');
        // The arrival bay: clear glass under the same oak cap (70 F03).
        p.GuardEdge(3, 2, 'N', FrontRoomsStructuresKit.GuardStyle.Glass);
        // West: two rooms with windows to the void (E3, sill 1.0).
        for (var j = 3; j <= 5; j++) { p.ForceWall(1, j, 'E'); p.Open(1, j, 'E', .6f, 2.4f, 1.0f, 2.1f); }
        // North: a solid wall with one door onto the drop.
        for (var i = 2; i <= 4; i++) p.ForceWall(i, 6, 'S');
        p.Open(3, 6, 'S', 1f, 2f, 0f, 2.1f);
        // Doors: the gallery to its rooms, and the rooms round the void in a loop.
        p.Open(2, 2, 'W', 1f, 2f, 0f, 2.1f);
        p.Open(2, 2, 'S', 1f, 2f, 0f, 2.1f);
        p.Open(3, 1, 'S', 1f, 2f, 0f, 2.1f);
        p.Open(5, 1, 'E', 1f, 2f, 0f, 2.1f);
        p.Open(6, 3, 'N', 1f, 2f, 0f, 2.1f);
        p.Open(5, 5, 'N', 1f, 2f, 0f, 2.1f);
        p.Open(0, 3, 'S', 1f, 2f, 0f, 2.1f);
        p.Open(0, 4, 'N', 1f, 2f, 0f, 2.1f);
        p.Open(0, 5, 'N', 1f, 2f, 0f, 2.1f);
        p.Open(1, 6, 'E', 1f, 2f, 0f, 2.1f);
        p.Open(4, 6, 'E', 1f, 2f, 0f, 2.1f);
        p.Build(kit);
        kit.Shell(F3Ceil, F3);
        // Glass in the west windows; the guard across the north door's frame on the void side (a door onto the drop, never a way).
        for (var j = 3; j <= 5; j++) kit.Prism(new Vector3(6f, 0f, 3f * j + .6f), new Vector3(6f, 0f, 3f * j + 2.4f), F3 + 1.0f, F3 + 2.1f, .012f, glass, true);
        kit.Guard(new Vector3(9.95f, 0f, 18f), new Vector3(11.05f, 0f, 18f), F3, FrontRoomsStructuresKit.GuardStyle.Rail);
        // The glass-block floor light (E7), 1 x 2 cells in the east gallery, lit from the office below; a steel grid on it.
        kit.Box(15.2f, F3 - .08f, 12f, 18f, F3, 18f, glassBlock);
        kit.Box(15f, F3 - .08f, 12f, 15.2f, F3, 18f, l0Carpet);
        for (var g = 1; g < 5; g++) kit.Box(15f + g * .6f - .01f, F3, 12f, 15f + g * .6f + .01f, F3 + .004f, 18f, FrontRoomsPoolMaterials.Steel, false);
        for (var g = 1; g < 10; g++) kit.Box(15.2f, F3, 12f + g * .6f - .01f, 18f, F3 + .004f, 12f + g * .6f + .01f, FrontRoomsPoolMaterials.Steel, false);
        kit.Lid(15f, 12f, 18f, 18f, F3Ceil, l0Ceil);
        kit.Troffer(new Vector3(16.5f, F3Ceil, 13.5f + FrontRooms.Map.ModuleUnits.TrofferOffsetZ), true);
        kit.Troffer(new Vector3(16.5f, F3Ceil, 16.5f + FrontRooms.Map.ModuleUnits.TrofferOffsetZ), true);
        // Level 0's ceiling grid runs on over the well, lamps and all: the void is cut out of the floors only.
        kit.Lid(6f, 9f, 15f, 18f, F3Ceil, l0Ceil);
        kit.Lid(12f, 6f, 15f, 9f, F3Ceil, l0Ceil);
        for (var i = 2; i <= 4; i++)
            for (var j = 3; j <= 5; j++)
                kit.Troffer(new Vector3(3f * i + 1.5f, F3Ceil, 3f * j + 1.5f + FrontRooms.Map.ModuleUnits.TrofferOffsetZ), true);
        kit.Troffer(new Vector3(13.5f, F3Ceil, 7.5f + FrontRooms.Map.ModuleUnits.TrofferOffsetZ), true);
    }

    // ---------------------------------------------------------------- the rims' sections (V1: the cut shows the floor)

    void BuildRims()
    {
        // Under every void edge of F2 and F3: the floor's own carpet (the plate's slab), the structural slab, the plenum's
        // dark gap, and the ceiling of the floor below as a thin line at the bottom.
        var f2Void = VoidSet(false);
        var f3Void = VoidSet(true);
        // F2's balcony bay (3, 6) stands 0.30 lower: its band tops out under its own floor.
        Rim(f2Void, F1Ceil, (i, j) => i == 3 && j == 6 ? F2 - .35f : F2 - .05f, runSoffit);
        Rim(f3Void, F2Ceil, (i, j) => F3 - .05f, offCeil);
    }

    static bool[,] VoidSet(bool notch)
    {
        var v = new bool[8, 7];
        for (var i = 2; i <= 4; i++) for (var j = 3; j <= 5; j++) v[i, j] = true;
        if (notch) v[4, 2] = true;
        return v;
    }

    // topAt(i, j): the top of the band under the floor cell (i, j) beside the void (its floor less its carpet).
    void Rim(bool[,] v, float bottom, Func<int, int, float> topAt, Material ceilingBelow)
    {
        for (var i = 0; i < 8; i++)
            for (var j = 0; j < 7; j++)
            {
                if (!v[i, j]) continue;
                float x0 = 3f * i, x1 = x0 + 3f, z0 = 3f * j, z1 = z0 + 3f;
                if (i == 0 || !v[i - 1, j]) RimEdge(new Vector3(x0 - .2f, 0f, z0), new Vector3(x0, 0f, z1), Vector3.right, bottom, topAt(i - 1, j), ceilingBelow);
                if (i == 7 || !v[i + 1, j]) RimEdge(new Vector3(x1, 0f, z0), new Vector3(x1 + .2f, 0f, z1), Vector3.left, bottom, topAt(i + 1, j), ceilingBelow);
                if (j == 0 || !v[i, j - 1]) RimEdge(new Vector3(x0, 0f, z0 - .2f), new Vector3(x1, 0f, z0), Vector3.forward, bottom, topAt(i, j - 1), ceilingBelow);
                if (j == 6 || !v[i, j + 1]) RimEdge(new Vector3(x0, 0f, z1), new Vector3(x1, 0f, z1 + .2f), Vector3.back, bottom, topAt(i, j + 1), ceilingBelow);
            }
    }

    // One edge's section (lo..hi = its 0.2 m band on the floor side; toVoid points into the void): the slab 0.2 under the
    // carpet, the plenum's dark gap recessed 0.08 from the void face, the ceiling of the floor below as a 0.04 line.
    void RimEdge(Vector3 lo, Vector3 hi, Vector3 toVoid, float bottom, float top, Material ceilingBelow)
    {
        kit.Shell(top, bottom);
        kit.Box(new Vector3(lo.x, top - .2f, lo.z), new Vector3(hi.x, top, hi.z), SectionSlab(), true);
        var pl = lo;
        var ph = hi;
        if (toVoid.x > .5f) ph.x -= .08f; else if (toVoid.x < -.5f) pl.x += .08f;
        if (toVoid.z > .5f) ph.z -= .08f; else if (toVoid.z < -.5f) pl.z += .08f;
        kit.Box(new Vector3(pl.x, bottom + .04f, pl.z), new Vector3(ph.x, top - .2f, ph.z), plenum, false);
        kit.Box(new Vector3(lo.x, bottom, lo.z), new Vector3(hi.x, bottom + .04f, hi.z), ceilingBelow, false);
    }

    Material sectionSlab;

    // Stair_Slab carries _FR_OWN_LIGHTS (lamps over 6 m away are skipped: right in a core, black on a rim lit from 8-10 m), so
    // the rims take a runtime copy without it (review SP2-F3); the asset stays as it is.
    Material SectionSlab()
    {
        if (sectionSlab != null) return sectionSlab;
        if (concrete == null) return null;
        sectionSlab = new Material(concrete) { name = "SP2 rim section slab (Stair_Slab, all lamps)" };
        sectionSlab.DisableKeyword("_FR_OWN_LIGHTS");
        return sectionSlab;
    }

    // ---------------------------------------------------------------- the cascade (C5): F1's slot, 9 m down F0's wall

    void BuildCascade()
    {
        // F1's water room: a tiled channel from an overflow weir to the slot, out over a tiled lip on the void's edge.
        kit.Shell(F1Ceil, F1);
        kit.Box(9f, F1, 6.3f, 12f, F1 + .01f, 8.92f, tile, false);
        kit.Box(8.85f, F1, 6.15f, 9f, F1 + .15f, 8.92f, tile);
        kit.Box(12f, F1, 6.15f, 12.15f, F1 + .15f, 8.92f, tile);
        kit.Box(8.85f, F1, 6.0f, 12.15f, F1 + .45f, 6.3f, tile);
        kit.Box(9f, F1 - .1f, 8.93f, 12f, F1 + .02f, 9.15f, tile);
        kit.Water(9f, 6.3f, 12f, 9.15f, F1 + .07f);
        Sheet(new Vector3(9.05f, 0f, 6.31f), new Vector3(11.95f, 0f, 6.31f), F1 + .45f, F1 + .07f);
        // The fall: from the lip down F0's tiled cascade wall to the splash trough at its foot.
        Sheet(new Vector3(9.05f, 0f, 9.07f), new Vector3(11.95f, 0f, 9.07f), F1 - .05f, .08f);
        // The splash trough inside the basin, its front lip spilling into the basin.
        kit.Shell(Lid0, -1.12f);
        kit.Box(8.7f, -1.12f, 9.0f, 8.85f, .1f, 9.85f, tile);
        kit.Box(12.15f, -1.12f, 9.0f, 12.3f, .1f, 9.85f, tile);
        kit.Box(8.85f, -1.12f, 9.7f, 12.15f, .1f, 9.85f, tile);
        kit.Box(8.85f, -1.12f, 9.0f, 12.15f, -.2f, 9.7f, tile);
        kit.Water(8.85f, 9.0f, 12.15f, 9.7f, .07f);
        Sheet(new Vector3(8.85f, 0f, 9.86f), new Vector3(12.15f, 0f, 9.86f), .1f, -.05f);
        kit.Shell(Lid0, 0f);
    }

    // A falling water sheet (greybox: a transparent quad, both faces).
    void Sheet(Vector3 a, Vector3 b, float top, float bottom)
    {
        if (sheet == null) return;
        var keep = kit.Mesh.CastShadows;
        kit.Mesh.CastShadows = false;
        var n = Vector3.Cross(Vector3.up, b - a).normalized;
        kit.QuadPlanar(a + Vector3.up * bottom, b + Vector3.up * bottom, b + Vector3.up * top, a + Vector3.up * top, n, sheet);
        kit.QuadPlanar(a + Vector3.up * bottom, b + Vector3.up * bottom, b + Vector3.up * top, a + Vector3.up * top, -n, sheet);
        kit.Mesh.CastShadows = keep;
    }

    // ---------------------------------------------------------------- lamps (each floor its own; F0 and the basin are view lamps)

    void BuildLamps()
    {
        // F0: metal-halide wall boxes throwing up so the tiled lid is the brightest surface, 5.4 m up in the 8.7 m hall.
        // 10 / range 14 (was 7 / 11): thrown 30 degrees into the hall, they light the lid 3.3 m over them (review SP2-F17).
        foreach (var z in new[] { 4.5f, 10.5f, 16.5f }) kit.WallBox(new Vector3(0f, 5.4f, z), Vector3.right, 10f, 14f);
        foreach (var x in new[] { 4.5f, 10.5f, 16.5f, 22.5f }) kit.WallBox(new Vector3(x, 5.4f, 21f), Vector3.back, 10f, 14f);
        foreach (var x in new[] { 4.5f, 10.5f }) kit.WallBox(new Vector3(x, 5.4f, 0f), Vector3.forward, 10f, 14f);
        foreach (var x in new[] { 7.5f, 13.5f }) kit.WallBox(new Vector3(x, 5.4f, 8.7f), Vector3.back, 10f, 14f);
        kit.WallBox(new Vector3(20.9f, 6.8f, 13.5f), Vector3.left, 5f, 9f);
        // A soft down-fill grid at about 6 m.
        foreach (var d in new[] { new Vector2(3f, 4.5f), new Vector2(3f, 16.5f), new Vector2(10.5f, 4.5f), new Vector2(10.5f, 19.5f), new Vector2(18f, 13.5f), new Vector2(18f, 19.5f), new Vector2(17.2f, 8.3f) })
            kit.DownFill(new Vector3(d.x, Lid0 - .07f, d.y), 2.2f, 11f);
        // The basin lit from below: the glow that rises through the well (view lamps, F44).
        // Two per wall at the thirds, clear of the NE steps and the splash trough, each aimed 30 degrees down onto the porcelain
        // floor so the whole basin reads lit from F3 (review SP2-F5; by eye: 3.5, range 9).
        BasinLamp(new Vector3(6.02f, -.45f, 12f), Vector3.right);
        BasinLamp(new Vector3(6.02f, -.45f, 15f), Vector3.right);
        BasinLamp(new Vector3(14.98f, -.45f, 12f), Vector3.left);
        BasinLamp(new Vector3(14.98f, -.45f, 15f), Vector3.left);
        BasinLamp(new Vector3(8.5f, -.45f, 17.98f), Vector3.back);
        BasinLamp(new Vector3(11f, -.45f, 17.98f), Vector3.back);
        BasinLamp(new Vector3(7.5f, -.45f, 9.02f), Vector3.forward);
        BasinLamp(new Vector3(13.5f, -.45f, 9.02f), Vector3.forward);
        foreach (var z in new[] { 11.5f, 15.5f }) kit.DownFill(new Vector3(18f, Gal - .37f, z), 1.8f, 7f);
        // F1: Run!'s wrap fluorescents (1.22 m, in the soffit).
        foreach (var x in new[] { 1.5f, 7.5f, 13.5f, 19.5f }) Wrap(new Vector3(x, F1Ceil - .05f, 19.5f), true);
        Wrap(new Vector3(16.5f, F1Ceil - .05f, 10.5f), false);
        Wrap(new Vector3(16.5f, F1Ceil - .05f, 16.5f), false);
        Wrap(new Vector3(4.5f, F1Ceil - .05f, 7.5f), false);
        Wrap(new Vector3(10.5f, F1Ceil - .05f, 7.5f), true);
        Wrap(new Vector3(4.5f, F1Ceil - .05f, 13.5f), false);
    }

    void BasinLamp(Vector3 at, Vector3 into)
    {
        var l = kit.Underwater(at, into, 3.5f, 9f);
        if (l != null) l.transform.rotation = (Root != null ? Root.rotation : Quaternion.identity) * Quaternion.LookRotation((into.normalized * Mathf.Cos(30f * Mathf.Deg2Rad) + Vector3.down * Mathf.Sin(30f * Mathf.Deg2Rad)).normalized, Vector3.up);
    }

    // F1's lamps: the game's Run! B strip (FrontRoomsLampKit, as SP5's) with the rig's wrap light (5.5, range 10, 172 / 124;
    // FrontRoomsRunRig), its red-coated middle tube at the armed bounce (review SP2-F15, missed 3: a box-and-lens stand-in
    // at 2.4 / 8 / 150 / 110). The old box lamp stands in when the kit is missing.
    void Wrap(Vector3 at, bool alongX)
    {
        var tint = new Color(.93f, .97f, 1f);
        MeshRenderer lens = null, redTube = null;
        var frame = new GameObject("F1 Run! B strip");
        frame.transform.SetParent(Root, false);
        frame.transform.localPosition = at;
        frame.transform.localRotation = alongX ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;
        try
        {
            if (FrontRoomsLampKit.Available(FrontRoomsLampKit.Family.Strip))
                lens = FrontRoomsLampKit.Build(FrontRoomsLampKit.Family.Strip, frame.transform, Vector3.zero, Quaternion.identity, "SP2 F1 strip tubes", false, out redTube);
        }
        catch (System.Exception e) { FrontRoomsPoolMaterials.LogOnce("SP2 strip", "SP2: the R-B strip kit failed: " + e.Message); lens = null; redTube = null; }
        if (lens == null)
        {
            UnityEngine.Object.Destroy(frame);
            kit.Lamp("Run! wrap fluorescent", LightType.Spot, at, Vector3.down, 172f, 124f, 5.5f, 10f, tint, false,
                alongX ? new Vector3(1.22f, .18f, .1f) : new Vector3(.18f, 1.22f, .1f), FrontRoomsPoolMaterials.Surface("Painted_Metal", new Color(.85f, .85f, .82f)), new Color(1.5f, 1.6f, 1.6f));
            return;
        }
        if (redTube != null)
        {
            var mpb = new MaterialPropertyBlock();
            redTube.GetPropertyBlock(mpb);
            mpb.SetColor("_EmissionColor", new RunRigLayout().redTubeBounce);
            redTube.SetPropertyBlock(mpb);
        }
        var lightGo = new GameObject("light");
        lightGo.transform.SetParent(frame.transform, false);
        lightGo.transform.localPosition = new Vector3(0f, -.092f, 0f);
        lightGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var l = lightGo.AddComponent<Light>();
        l.type = LightType.Spot;
        l.spotAngle = 172f;
        l.innerSpotAngle = 124f;
        l.color = tint;
        l.intensity = 5.5f;
        l.range = 10f;
        l.shadows = LightShadows.None;
        if (kit.Host != null && kit.Host.Look != null) kit.Host.Look.AddLamp(Key, l, lens, tint * 2.6f, false);
    }

    // ---------------------------------------------------------------- the offices' furniture (the game's own dresser)

    void DressOffices()
    {
        var go = new GameObject("F2 office dressing (the game's FrontRoomsOfficeKit)");
        go.transform.SetParent(Root, false);
        go.transform.localPosition = new Vector3(0f, F2, 0f);
        var none = new Rect[0];
        // The east office: keep the bridge's lane, the core's door and the doors to the south office and the north gallery clear.
        FrontRoomsOfficeKit.Dress(go.transform, new Rect(15.08f, 3.08f, 5.84f, 17.84f), 2.9f, 2041, new[]
        {
            new Rect(15.08f, 12.4f, 5.84f, 2.2f), new Rect(18.9f, 9.2f, 2.0f, 2.6f), new Rect(15.08f, 3.8f, 1.4f, 1.4f), new Rect(15.08f, 18.8f, 1.6f, 1.6f),
        }, none);
        FrontRoomsOfficeKit.Dress(go.transform, new Rect(6.08f, 3.08f, 8.84f, 5.84f), 2.9f, 2042, new[] { new Rect(13.4f, 3.8f, 1.5f, 1.4f) }, none);
    }

    // ---------------------------------------------------------------- the game's zone grades for the Office and Run! floors,
    // and 游戏视觉's pool zone look over F0

    void BuildVolumes()
    {
        FrontRoomsPostStack.EnsureZoneVolume(Root, "Office", new Bounds(new Vector3(12f, F2 + 1.6f, 10.5f), new Vector3(24f, 3.4f, 21f)));
        FrontRoomsPostStack.EnsureZoneVolume(Root, "Run_B", new Bounds(new Vector3(12f, F1 + 1.45f, 10.5f), new Vector3(24f, 3.1f, 21f)));
        // F0 is one room (the hall, its basin, the gallery, the B4-L and the core's pool storey) in PoolLevel (level water, 70
        // section 2), as the map's AddPoolLooks: 3 m under the deck to 0.2 m over the lid, the basin's water (-0.05) carries the
        // height fog (the splash trough is no pool water). FrontRoomsStructuresLook switches it off above F0 (F1-F3 keep their
        // own looks; its box and blend would reach F1's eye).
        kit.Host.Look.PoolRoom(this, "F0 pool hall", "PoolLevel", 0f, 0f, 24f, 21f, 0f, Lid0, -.05f);
        kit.Host.Look.PoolRoomNote(this, "只在 F0 泳池层：F1-F3 用各层自己的外观。");
    }

    // ---------------------------------------------------------------- the sign (70 section 4, verbatim) and the metric marks

    void BuildSigns()
    {
        // The arrival stands 1 m behind the glass over the void, with the notch's drop on its right: the sign stands on clear
        // floor 2.5 m straight behind it (turn round), its face's centre 2.50 m from the arrival eye (20_placard_type.md v2.4,
        // E's FAR; was 1.27 m behind-right), in gallery cell (3, 1), leaving 0.8 m to the x 9 wall and 0.8 m to the notch's
        // guard corner, so the walker and the Relay pass it either side.
        kit.Sign(new Vector3(10.5f, F3 + 1.5f, 5.506f), new Vector3(10.5f, F3 + 1.5f, 8.0f), "SP2 · 井：穿过整栋楼的一口竖井",
            "从 Level 0 的玻璃栏板往下看，经过办公层、Run! 设备层，约 17 米下面是泳池的青绿色水面。每层井口的形状都不一样，只有办公层有桥，Run! 设备层有水顺着井的南墙流进泳池。东边的楼梯连通四层，也是 Relay 的路；泳池层另有一部大圆楼梯。游戏里一次只加载一层，往下看到的会是替身竖井；这里把四层都真的叠起来，让你判断深度。\n"
            + "灰盒占位：四层的栏杆、桥、楼梯细部、玻璃砖地灯、大圆楼梯顶上的天窗、流水和泳池水；泳池层的瓷砖贴图只在这个场景里，其他三层的墙面、地毯、吊顶和灯是游戏自己的。黑色人形是 Relay 的静态替身（2.05 米），只用来检查视线，不会动。\n"
            + "要你决定：1 用这口井做全楼的地标吗？ 2 井口用 3×3 格的方井，还是 2×5 格的窄槽（31 §3.5，这里没有做）？ 3 Level 0 这一格换成玻璃栏板才看得到水面（实心半墙会挡住），可以吗？ 4 大圆楼梯顶上用方形天窗，还是换成穹顶（60 Q2）？", F3);
        kit.Mark(new Vector3(10.5f, F3 + 1.25f, 8.85f), Vector3.back, "玻璃栏板 1.07 · 往下约 17 米到水面");
        kit.Mark(new Vector3(7.5f, F3 + 1.25f, 8.85f), Vector3.back, "F3 Level 0 楼面 +16.80");
        kit.Mark(new Vector3(15.2f, F3 + 1.25f, 13.5f), Vector3.right, "井 9 × 9 米（3 × 3 格）");
        kit.Mark(new Vector3(5.85f, F2 + 1.25f, 16.5f), Vector3.left, "F2 办公层 +12.90 · 桥宽 1.5 · 梁高 0.9");
        kit.Mark(new Vector3(15.5f, F1 + 1.95f, 13.5f), Vector3.right, "F1 Run! 设备层 +9.00 · 网栏在 0.6 墩上");
        kit.Mark(new Vector3(15.2f, Gal + 1.25f, 15f), Vector3.right, "画廊 +4.50");
        kit.Mark(new Vector3(6.4f, .45f, 13.5f), Vector3.right, "水深 1.07 · 水面在楼面下 0.05");
        kit.Mark(new Vector3(16.4f, 1.2f, 4.5f), Vector3.left, "B4-L：宽 1.45 · 30 级 × 0.15 · 约 185° · 外径 9.0");
    }

    // ---------------------------------------------------------------- the frame

    public override FrontRoomsStructuresLook.Preset PresetAt(Vector3 world)
    {
        // The floor the walker stands on sets the look (70 section 1: SP2 switches at each landing of its core).
        var y = world.y - Site.y;
        if (y > F3 - .3f) return FrontRoomsStructuresLook.Preset.Level0;
        if (y > F2 - .3f) return FrontRoomsStructuresLook.Preset.Office;
        if (y > F1 - .3f) return FrontRoomsStructuresLook.Preset.RunB;
        return FrontRoomsStructuresLook.Preset.Pool;
    }

    static string FloorName(float y)
    {
        if (y > F3 - .3f) return "F3 Level 0";
        if (y > F2 - .3f) return "F2 办公层";
        if (y > F1 - .3f) return "F1 Run! 设备层";
        if (y > Gal - .3f) return "F0 画廊 +4.5";
        return "F0 泳池大厅";
    }

    public override string Status(FrontRoomsStructures host)
    {
        if (host == null || host.Walker == null) return "";
        return "现在在 " + FloorName(host.Walker.transform.position.y - Site.y);
    }

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
        sb.AppendLine("一口 9 × 9 米的竖井穿过四层：F3 Level 0（+16.8）、F2 办公层（+12.9）、F1 Run! 设备层（+9.0）、F0 泳池大厅（甲板 ±0，+4.5 有一圈画廊）。每层井口的形状、栏杆和灯都是那一层自己的。");
        sb.AppendLine();
        sb.AppendLine("按键：[ ] 换站点（F3 玻璃栏板、F2 西边栏杆、F2 桥上、F1 东边、F0 画廊、F0 大圆楼梯顶、F0 池边）；R 回到当前站点；C Relay 体型（大圆楼梯对它关闭）；M 尺寸。");
        sb.AppendLine("走下去的路：东边的楼梯间连通五个楼面（每层两段），也是 Relay 的路；泳池层的画廊另有大圆楼梯（B4-L）下到甲板。");
        sb.AppendLine();
        sb.AppendLine("要你决定（在对话里按编号告诉总控）：");
        sb.AppendLine("SP2-1 用这口井做全楼的地标吗？");
        sb.AppendLine("SP2-2 井口用 3×3 格的方井，还是 2×5 格的窄槽（31 §3.5，这里没有做）？");
        sb.AppendLine("SP2-3 Level 0 这一格换成玻璃栏板才看得到水面（实心半墙会挡住），可以吗？");
        sb.AppendLine("SP2-4 大圆楼梯顶上用方形天窗，还是换成穹顶（60 Q2）？");
        sb.AppendLine();
        sb.AppendLine("灰盒占位：四层的栏杆、桥、楼梯细部、玻璃砖地灯、大圆楼梯顶上的天窗、流水和泳池水；泳池层的瓷砖贴图只在这个场景里，其他三层的墙面、地毯、吊顶和灯是游戏自己的。黑色人形是 Relay 的静态替身（2.05 米），只用来检查视线，不会动。");
        sb.AppendLine();
        sb.AppendLine("技术记录：游戏里一次只加载一层，往下看到的会是替身竖井；这里四层是真的叠起来的。B4-L 宽 1.45、内半径 2.95、30 级 × 0.15、中间一个平台，约 185 度；楼梯间每层两段（泳池层 15 级，其他 13 级）。");
    }
}
