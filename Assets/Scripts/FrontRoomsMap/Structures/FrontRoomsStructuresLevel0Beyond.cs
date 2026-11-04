using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SP3's events beyond the field (research/level_space/70 section 5 (f)-(k); 40 section 6.2; 30 section 8; 31 sections 3.2, 5;
/// 62 Q2-Q3): one per chunk east of x 48 (chunks C-I, F7 taking two), along one Level 0 corridor (z 3..6) that leaves the
/// field through the south-east plain room's east wall. Each event is a whole module on the cell grid (40 11.9) on one kit
/// plate: (f) the training room, (g) F9 the parallel bowl, (h) F7 the island field (with its patch below), (i) the sunken
/// lounge, (j) the file mezzanine (Tall 5.4, a gallery, a B4-S and the Relay's straight flight), (k) F8 a flat lid over a
/// stepped floor. The plate's chunk-wide fields are not built (only the corridor and the module of each chunk), so the walk
/// keeps 70's spacing without 400 empty rooms. Composed placements are this file's (70 left the rectangles to the Plan).
/// </summary>
public sealed partial class FrontRoomsStructuresLevel0
{
    const float BeyondX0 = 48f;
    const int Corr2 = 20, Training = 21, TrainingSide = 22, Bowl = 23, Islands = 24, Lounge = 25, Mezz = 26, FlatLid = 27;

    // Plate cell (i, j) of the beyond plate in district metres: x = 48 + 3i, z = 3j.
    static float BX(int i) => BeyondX0 + 3f * i;

    void BuildBeyond()
    {
        var p = new FrontRoomsStructuresKit.Plate(BeyondX0, 0f, 0f, 54, 10)
        {
            WallMat = wall, FloorMat = carpet, FloorThick = .05f, CeilingMat = ceiling, LedgeMat = wall, TreadMat = carpet,
            Troffers = true, VoidGuard = FrontRoomsStructuresKit.GuardStyle.HalfWall, GuardWall = wall,
            GuardCap = FrontRoomsPoolMaterials.Surface("Prop_WoodOak", new Color(.55f, .40f, .25f)),
            LedgeGuard = FrontRoomsStructuresKit.GuardStyle.HalfWall, SectionMat = concrete, SectionDepth = 1.0f,
        };
        var floor = FrontRoomsStructuresKit.Plate.Kind.Floor;
        var voidKind = FrontRoomsStructuresKit.Plate.Kind.Void;
        var held = FrontRoomsStructuresKit.Plate.Kind.Held;
        // The corridor east (z 3..6, x 48..210); its west end is the field's arch.
        p.Fill(0, 1, 53, 1, floor, 0f, 2.9f, Corr2);
        p.NoWall(0, 1, 'W');
        // (f) T_L0_TrainingRoom, 4 x 4 (x 54..66, z 6..18): rows 0, -0.30, -0.60, -0.90 falling north to the screen wall;
        // a side passage (x 66..69) for the bottom arch.
        for (var j = 2; j <= 5; j++) p.Fill(2, j, 5, j, floor, -.3f * (j - 2), 2.9f, Training);
        p.Fill(6, 2, 6, 5, floor, 0f, 2.9f, TrainingSide);
        p.Open(3, 1, 'N', .6f, 2.1f, 0f, 2.2f);                       // the top arch, off-centre in the back wall
        p.Open(6, 1, 'N', .7f, 2.2f, 0f, 2.2f);                       // the side passage back to the corridor
        p.Open(5, 5, 'E', .9f, 2.1f, 0f, 2.2f);                       // the bottom arch in the front wall's corner cell
        for (var j = 3; j <= 5; j++)
        {
            p.Run(2, j, 'S', 0f, 1.2f);                               // STEPs in the two 1.2 m side aisles; a KERB elsewhere
            p.Run(5, j, 'S', 1.8f, 3f);
        }
        // (g) F9 the parallel bowl, 6 x 8 (x 75..93, z 6..30): rim 0, terrace -0.60, pit -1.20 (3), terrace -0.60 (2), rim 0;
        // the lid steps with the floor (+2.90, +2.30, +1.70), 2.90 clear everywhere.
        float[] bowl = { 0f, -.6f, -1.2f, -1.2f, -1.2f, -.6f, -.6f, 0f };
        for (var k = 0; k < 8; k++) p.Fill(9, 2 + k, 14, 2 + k, floor, bowl[k], bowl[k] + 2.9f, Bowl);
        p.Open(10, 1, 'N', .7f, 2.2f, 0f, 2.2f);
        p.Run(9, 3, 'S', .6f, 1.8f); p.Run(13, 3, 'S', .3f, 1.5f);    // two 4-riser runs a line, never opposite (R4, R6)
        p.Run(11, 4, 'S', 1.2f, 2.4f); p.Run(14, 4, 'S', .6f, 1.8f);
        p.Run(10, 7, 'S', .9f, 2.1f); p.Run(12, 7, 'S', 1.5f, 2.7f);
        p.Run(9, 9, 'S', 1.5f, 2.7f); p.Run(13, 9, 'S', .6f, 1.8f);
        // (h) F7 the island field, 11 x 7 (x 99..132, z 6..27) over two chunks: a one-cell floor ring, void lanes on columns
        // 0, 4, 8 and rows 0, 4 of the inside; island A +0.30, island B +0.60; the carpet and grid run on.
        p.Fill(17, 2, 27, 8, floor, 0f, 2.9f, Islands);
        p.Fill(18, 3, 26, 7, voidKind, 0f, 0f, -1);
        p.Fill(19, 4, 21, 6, floor, .3f, 2.9f, Islands);
        p.Fill(23, 4, 25, 6, floor, .6f, 2.9f, Islands);
        p.Open(18, 1, 'N', .8f, 2.3f, 0f, 2.2f);
        p.Crossed(17, 5, 'E', .6f, 2.4f); p.Crossed(19, 5, 'W', .6f, 2.4f);      // A west
        p.Crossed(21, 5, 'E', .6f, 2.4f); p.Crossed(23, 5, 'W', .6f, 2.4f);      // A-B
        p.Crossed(24, 6, 'N', .6f, 2.4f); p.Crossed(24, 8, 'S', .6f, 2.4f);      // B north
        p.Crossed(24, 2, 'N', .6f, 2.4f); p.Crossed(24, 4, 'S', .6f, 2.4f);      // B south
        // (i) T_L0_SunkenLounge, 3 x 3 (x 147..156, z 6..15): the pit is cut by hand (Held).
        p.Fill(33, 2, 35, 4, held, 0f, 2.9f, Lounge);
        p.Open(34, 1, 'N', .7f, 2.2f, 0f, 2.2f);
        // (j) T_OF_FileMezzanine in Level 0 (62 Q2), 4 x 3, Tall 5.4 (x 171..183, z 6..15).
        p.Fill(41, 2, 44, 4, floor, 0f, 5.4f, Mezz);
        p.Open(42, 1, 'N', .9f, 2.4f, 0f, 2.2f);
        // (k) F8 a flat Standard lid over a stepped floor, 4 x 4 (x 195..207, z 6..18): columns -1.20, -0.60, 0, +0.60;
        // two step runs on every level line (two ways onto every level, R6); clear height 4.10 down to 2.30.
        float[] f8 = { -1.2f, -.6f, 0f, .6f };
        for (var k = 0; k < 4; k++) p.Fill(49 + k, 2, 49 + k, 5, floor, f8[k], 2.9f, FlatLid);
        p.Open(51, 1, 'N', .8f, 2.3f, 0f, 2.2f);
        p.Run(50, 3, 'W', .6f, 1.8f); p.Run(50, 5, 'W', .9f, 2.1f);
        p.Run(51, 2, 'W', 1.2f, 2.4f); p.Run(51, 4, 'W', .6f, 1.8f);
        p.Run(52, 3, 'W', .3f, 1.5f); p.Run(52, 5, 'W', 1.2f, 2.4f);
        p.Build(kit);

        kit.Safe("(f) training room", BuildTraining);
        kit.Safe("(g) F9 bowl", BuildBowl);
        kit.Safe("(h) F7 islands", BuildIslands);
        kit.Safe("(i) sunken lounge", BuildLounge);
        kit.Safe("(j) file mezzanine", BuildMezzanine);
        kit.Safe("beyond signs", BuildBeyondSigns);
    }

    // ---------------------------------------------------------------- (f) the training room's set pieces

    void BuildTraining()
    {
        // The bottom arch's 6-riser step run in the corner cell (-0.90 -> +-0), table rows on each tier, the screen wall.
        kit.Shell(2.9f, -.9f);
        kit.StepRun(new Vector3(66f, 0f, 16.5f), Vector3.right, 1.2f, -.9f, 0f, carpet, null);
        var laminate = FrontRoomsPoolMaterials.Surface("Prop_LaminateBeige", new Color(.8f, .75f, .62f));
        for (var j = 2; j <= 5; j++)
        {
            var y = -.3f * (j - 2);
            kit.StandIn("training table row", new Vector3(60f, y + .37f, 3f * j + .6f), new Vector3(8.4f, .74f, .6f), laminate);
        }
        kit.StandIn("projection screen", new Vector3(60f, -.9f + 1.75f, 17.9f), new Vector3(5.4f, 1.7f, .04f), FrontRoomsPoolMaterials.Lit("screen white", new Color(.9f, .9f, .88f), .2f), 0f, false);
    }

    // ---------------------------------------------------------------- (g) F9: the Relay stand-in on the far rim

    void BuildBowl()
    {
        // 2 m back on the far rim, facing the near rim: from the near rim the pit's lid cuts the view at about 1.75 m.
        kit.Relay(new Vector3(84f, 0f, 29f), new Vector3(84f, 0f, 7f));
    }

    // ---------------------------------------------------------------- (h) F7: bridges, the grid over the lanes, the patch below

    void BuildIslands()
    {
        var hw = FrontRoomsStructuresKit.GuardStyle.HalfWall;
        var oak = FrontRoomsPoolMaterials.Surface("Prop_WoodOak", new Color(.55f, .40f, .25f));
        // A west: a deck at +-0 over the lane, a STEP up onto A.
        kit.Shell(2.9f, 0f);
        kit.Box(102f, -.25f, 15.6f, 105f, 0f, 17.4f, carpet);
        kit.Guard(new Vector3(102f, 0f, 15.65f), new Vector3(105f, 0f, 15.65f), 0f, hw, wall, oak);
        kit.Guard(new Vector3(102f, 0f, 17.35f), new Vector3(105f, 0f, 17.35f), 0f, hw, wall, oak);
        kit.StepRun(new Vector3(105f, 0f, 16.5f), Vector3.right, 1.7f, 0f, .3f, carpet, null);
        // A-B: a deck at +0.30, a STEP up onto B.
        kit.Shell(2.9f, .3f);
        kit.Box(114f, .05f, 15.6f, 117f, .3f, 17.4f, carpet);
        kit.Guard(new Vector3(114f, 0f, 15.65f), new Vector3(117f, 0f, 15.65f), .3f, hw, wall, oak);
        kit.Guard(new Vector3(114f, 0f, 17.35f), new Vector3(117f, 0f, 17.35f), .3f, hw, wall, oak);
        kit.StepRun(new Vector3(117f, 0f, 16.5f), Vector3.right, 1.7f, .3f, .6f, carpet, null);
        // B north and B south: decks at +-0 with a 4-riser STEP RUN up to B (two arms and three, never four).
        kit.Shell(2.9f, 0f);
        kit.Box(120.6f, -.25f, 21f, 122.4f, 0f, 24f, carpet);
        kit.Guard(new Vector3(120.65f, 0f, 21f), new Vector3(120.65f, 0f, 24f), 0f, hw, wall, oak);
        kit.Guard(new Vector3(122.35f, 0f, 21f), new Vector3(122.35f, 0f, 24f), 0f, hw, wall, oak);
        kit.StepRun(new Vector3(121.5f, 0f, 21f), Vector3.back, 1.7f, 0f, .6f, carpet, null);
        kit.Box(120.6f, -.25f, 9f, 122.4f, 0f, 12f, carpet);
        kit.Guard(new Vector3(120.65f, 0f, 9f), new Vector3(120.65f, 0f, 12f), 0f, hw, wall, oak);
        kit.Guard(new Vector3(122.35f, 0f, 9f), new Vector3(122.35f, 0f, 12f), 0f, hw, wall, oak);
        kit.StepRun(new Vector3(121.5f, 0f, 12f), Vector3.forward, 1.7f, 0f, .6f, carpet, null);
        // The carpet's grid runs on: lids and troffers over the void lanes.
        kit.Shell(2.9f, 0f);
        for (var i = 18; i <= 26; i++)
            for (var j = 3; j <= 7; j++)
            {
                var lane = i == 18 || i == 22 || i == 26 || j == 3 || j == 7;
                if (!lane) continue;
                kit.Lid(BX(i), 3f * j, BX(i) + 3f, 3f * j + 3f, 2.9f, ceiling);
                kit.Troffer(new Vector3(BX(i) + 1.5f, 2.9f, 3f * j + 1.5f + FrontRooms.Map.ModuleUnits.TrofferOffsetZ), false);
            }
        // The patch at -3.90 under the lanes: its floor, walls, a lid under the islands and the ring, troffers under them.
        kit.Shell(Patch + 2.9f, Patch);
        kit.Slab(102f, 9f, 129f, 24f, Patch, carpet, .2f);
        kit.WallX(9f, 101.92f, 129.08f, Patch, -.3f, wall);
        kit.WallX(24f, 101.92f, 129.08f, Patch, -.3f, wall);
        kit.WallZ(102f, 9f, 24f, Patch, -.3f, wall);
        kit.WallZ(129f, 9f, 24f, Patch, -.3f, wall);
        foreach (var r in new[] { new Vector4(105f, 12f, 114f, 21f), new Vector4(117f, 12f, 126f, 21f) })
        {
            kit.Lid(r.x, r.y, r.z, r.w, PatchCeil, ceiling);
            for (var x = r.x + 1.5f; x < r.z; x += 3f)
                for (var z = r.y + 1.5f; z < r.w; z += 3f)
                    kit.Troffer(new Vector3(x, PatchCeil, z + FrontRooms.Map.ModuleUnits.TrofferOffsetZ), false);
        }
    }

    // ---------------------------------------------------------------- (i) the sunken lounge's hand-built floor

    void BuildLounge()
    {
        // The room x 147..156, z 6..15 (3 x 3); the pit 6 x 6, off both centre lines (x 148.8..154.8, z 7.8..13.8), sunk 0.60
        // under the kept 2.9 lid (3.50 clear); 4-riser step runs along its south and west sides, E2 half-walls 1.1 on the other two.
        kit.Shell(2.9f, -.6f);
        kit.Box(147f, -.25f, 6f, 156f, 0f, 7.8f, carpet);
        kit.Box(147f, -.25f, 13.8f, 156f, 0f, 15f, carpet);
        kit.Box(147f, -.25f, 7.8f, 148.8f, 0f, 13.8f, carpet);
        kit.Box(154.8f, -.25f, 7.8f, 156f, 0f, 13.8f, carpet);
        kit.Slab(148.8f, 7.8f, 154.8f, 13.8f, -.6f, carpet, .2f);
        kit.StepRun(new Vector3(151.8f, 0f, 7.8f), Vector3.back, 6f, -.6f, 0f, carpet, null);
        kit.StepRun(new Vector3(148.8f, 0f, 10.8f + .45f), Vector3.left, 5.1f, -.6f, 0f, carpet, null);
        kit.Guard(new Vector3(148.8f, 0f, 13.88f), new Vector3(154.88f, 0f, 13.88f), 0f, FrontRoomsStructuresKit.GuardStyle.HalfWall, wall, null, 1.1f);
        kit.Guard(new Vector3(154.88f, 0f, 7.8f), new Vector3(154.88f, 0f, 13.88f), 0f, FrontRoomsStructuresKit.GuardStyle.HalfWall, wall, null, 1.1f);
        kit.Box(148.8f, -.6f, 13.8f, 154.8f, 0f, 13.84f, wall, false);
        kit.Box(154.76f, -.6f, 7.8f, 154.8f, 0f, 13.8f, wall, false);
        kit.Lid(147f, 6f, 156f, 15f, 2.9f, ceiling);
        for (var i = 0; i < 3; i++)
            for (var j = 0; j < 3; j++)
                kit.Troffer(new Vector3(148.5f + 3f * i, 2.9f, 7.5f + 3f * j + FrontRooms.Map.ModuleUnits.TrofferOffsetZ), false);
    }

    // ---------------------------------------------------------------- (j) the file mezzanine's gallery, B4-S and B2

    void BuildMezzanine()
    {
        // An A5 gallery at +2.70 (slab 0.20, 2.50 clear under it) over the north half, greybox file rows under it; a B4-S in the
        // south-west corner (15 risers of 0.18) and at the other end a straight B2 (18 risers of 0.15, 5.1 m going), the
        // Relay's way up; glass guards on the gallery's open edge.
        const float g = 2.7f;
        kit.Shell(5.4f, g);
        kit.Slab(171f, 10.5f, 183f, 15f, g, carpet, .2f);
        kit.Shell(5.4f, 0f);
        var steel = FrontRoomsPoolMaterials.Surface("Prop_SteelBeige", new Color(.75f, .72f, .65f));
        foreach (var z in new[] { 11.6f, 13.0f, 14.4f }) kit.StandIn("file shelving row", new Vector3(177.6f, 1.05f, z), new Vector3(9.6f, 2.1f, .45f), steel);
        for (var i = 0; i < 4; i++) kit.Troffer(new Vector3(172.5f + 3f * i, g - .2f, 12.75f), false);
        // B4-S: 2.5 across round a post, 15 x 0.18, 14 treads of 23 degrees (322), the last riser on the north radius, a landing
        // west of it against the west wall (the split suite's B4-S, resized after the walk test of 11-04: 1.7 across was too
        // steep for the player's controller). An unseen collision post r 0.32 keeps the walking line 0.62 m out.
        var c = new Vector3(172.5f, 0f, 8.6f);
        kit.Ring(c, 0f, .12f, 0f, g + 1.0f, -180f, 180f, metal, true, null, 30f);
        kit.Ring(c, 0f, .32f, 0f, g, -180f, 180f, null, true, null, 30f);
        kit.Spiral(c, .12f, 1.25f, 128f, 23f, 15, .18f, 0f, carpet, null, .2f, -1, 0f, 0f, .025f, metal, FrontRoomsStructuresKit.GuardHeight);
        // The landing over the entry gap and the first tread only, so the player's capsule clears it from the treads below.
        kit.Shell(5.4f, g);
        kit.Slab(171.6f, 9.0f, 172.5f, 10.5f, g, carpet, .2f);
        kit.Guard(new Vector3(171.68f, 0f, 9.0f), new Vector3(171.68f, 0f, 10.45f), g, FrontRoomsStructuresKit.GuardStyle.Glass);
        kit.Guard(new Vector3(171.6f, 0f, 9.08f), new Vector3(172.42f, 0f, 9.08f), g, FrontRoomsStructuresKit.GuardStyle.Glass);
        kit.RelayBlocker("B4-S foot", new Vector3(171.6f, 0f, 9.0f), new Vector3(172.6f, 2.4f, 10.5f));
        kit.RelayBlocker("B4-S top", new Vector3(171.6f, g, 9.0f), new Vector3(172.5f, g + 2.4f, 10.5f));
        // B2: along the gallery's south edge, rising east from x 176.1 to 181.2, a landing to the east wall.
        kit.Shell(5.4f, 0f);
        kit.Flight(new Vector3(176.1f, 0f, 9.9f), Vector3.right, 1.2f, 18, .15f, .3f, carpet, null, true);
        kit.Box(181.2f, 0f, 9.3f, 182.92f, g, 10.5f, carpet);
        SlopedRail(new Vector3(176.1f, .15f, 9.26f), new Vector3(181.2f, g, 9.26f));
        kit.Guard(new Vector3(181.2f, 0f, 9.26f), new Vector3(182.92f, 0f, 9.26f), g, FrontRoomsStructuresKit.GuardStyle.Glass);
        // Glass guards on the gallery's open south edge, but where the two landings join it.
        kit.Shell(5.4f, g);
        kit.Guard(new Vector3(172.5f, 0f, 10.58f), new Vector3(181.2f, 0f, 10.58f), g, FrontRoomsStructuresKit.GuardStyle.Glass);
    }

    // ---------------------------------------------------------------- the beyond signs (70 section 5's bullets, verbatim) and stops

    void BuildBeyondSigns()
    {
        Sign(55.2f, "培训室", "地面一排比一排低 0.3 米，降向前面的投影墙，上下各有一个门。要你决定：这个放进 Level 0 吗？台阶 0.30 米一层、一格深，还是原来的 0.45 米座位台阶（62 Q3）？");
        Sign(82.5f, "平行碗", "地面分两级降到 1.2 米深，天花板跟着一起降，所以哪里都是 2.9 米高。对面边上站着 Relay 的替身。要你决定：这个放进 Level 0 吗？远处那个人影头被天花板切掉、身子看得见，这样算不算“看得见 Relay”？");
        Sign(105.6f, "岛群", "几块 3×3 格的“岛”，四周是一格宽、能看到楼下的空槽，只能从桥上过；两块岛高度不同。要你决定：这个放进 Level 0 吗？");
        Sign(148.4f, "下沉休息区", "3×3 格的房间里，一块 2×2 格的地面沉下去 0.6 米（两边各有四级台阶），天花板不动，坑里有 3.5 米高；另外两边是 1.1 米的矮墙。沙发和茶几还没做。要你决定：这个放进 Level 0 吗？");
        Sign(177.6f, "档案夹层", "5.4 米高的房间，半边是 +2.7 米的夹层，下面是一排排档案架。一角是小螺旋楼梯，另一头是直跑楼梯，Relay 走直梯。要你决定：这个放进 Level 0 吗？档案夹层放在 Level 0 的高房间里，还是给办公层开高房间（62 Q2）？");
        Sign(198.6f, "平顶阶梯地面", "一整块平的天花板下面，地面从 −1.2 米一级级升到 +0.6 米，所以越往上走头顶越低（4.1 → 2.3 米），天花板本身一级也没有。要你决定：这个放进 Level 0 吗？");
    }

    // A sign on the corridor's north wall beside a module's arch, facing south into the corridor.
    void Sign(float x, string title, string body) => kit.Sign(new Vector3(x, 1.5f, 5.86f), new Vector3(x, 1.5f, 4.5f), title, body, 0f);

    void BeyondStops()
    {
        kit.Stop("培训室（东边第一处）", new Vector3(56f, 0f, 7.6f), new Vector3(60f, -.9f, 17f));
        kit.Stop("平行碗", new Vector3(84f, 0f, 7.6f), new Vector3(84f, .8f, 29f));
        kit.Stop("岛群", new Vector3(103.5f, 0f, 7.6f), new Vector3(115f, .8f, 16.5f));
        kit.Stop("下沉休息区", new Vector3(147.8f, 0f, 6.8f), new Vector3(152f, -.6f, 11f));
        kit.Stop("档案夹层", new Vector3(178.5f, 0f, 7.4f), new Vector3(176f, 3.2f, 13f));
        kit.Stop("平顶阶梯地面", new Vector3(202.5f, 0f, 7.5f), new Vector3(196.5f, -1.2f, 16.5f));
    }
}
