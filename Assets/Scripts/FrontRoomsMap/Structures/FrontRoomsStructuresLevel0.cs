using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// SP3 (key 3) of scene 4 (RULES 5b; research/level_space/70 section 5; 20 section 4.1; 40 sections 6.2, 7.1, 11.4; 60 section 6;
/// 62 sections 2.3, 2.6): Level 0 as it is, with its checks set into it along one straight spine. A field of 16 x 8 cells
/// (x 0..48, z 0..24) in the game's own finish (L0_Wallpaper, L0_Carpet, L0_Ceiling, one L03 troffer per cell, walls 0.16
/// on cell lines, LMS doorless rectangular arches 2.2 high, no doors, no windows, no round arch): the spine along row 3,
/// a cross corridor at column 12. Along it: V1 the cut (a 2 x 2 hole in a suite's floor, rails on its four sides, its
/// section, and an unreachable plain room 3.9 m below), V2 the drop (a room sunk 2.4 m behind a stage ledge, a 6 m wide
/// flight, a dais and a step run back up: a loop), V4 the tall grid (14.6 m of the same paper and grid behind a 2.2 m
/// slot), the computer room (a raised floor, a step and a 1:12 ramp) and the split suite (Tall 5.4: a platform, a gallery,
/// a flight, a step run and a one-cell B4-S that the Relay never takes). Built by hand from LMS numbers on the kit's plate,
/// never through the map (which streams chunks round the player). Greybox where no kit exists, listed on the signs.
/// </summary>
public sealed partial class FrontRoomsStructuresLevel0 : FrontRoomsStructuresDistrict
{
    const int Corr = 1, SuiteC = 2, RoomF1 = 3, RoomF2 = 4, Comp = 5, Drop = 6, Split = 7, Tall = 8, RoomSE = 9, RoomNE = 10;
    const float TallCeil = 14.6f, SplitCeil = 5.4f, Gallery = 2.7f, Patch = -3.9f, PatchCeil = -1.0f;
    static readonly Vector3 Arrival = new Vector3(1.5f, 0f, 10.5f);

    FrontRoomsStructuresKit kit;
    Material wall, carpet, ceiling, plenum, concrete, metal, glass;

    public FrontRoomsStructuresLevel0(Vector3 site)
        : base(3, "SP3", "SP3 · Level 0：原样的场地和几处断点", "Level 0 的走廊，和几处可以放进去的变化。", site) { }

    // ---------------------------------------------------------------- build

    public override void Build(FrontRoomsStructures host)
    {
        kit = new FrontRoomsStructuresKit(host, this);
        wall = FrontRoomsPoolMaterials.Surface("L0_Wallpaper", new Color(.80f, .74f, .48f));
        carpet = FrontRoomsPoolMaterials.Surface("L0_Carpet", new Color(.62f, .55f, .36f));
        ceiling = FrontRoomsPoolMaterials.Surface("L0_Ceiling", new Color(.86f, .83f, .70f));
        plenum = FrontRoomsPoolMaterials.Surface("Plenum_Dark", new Color(.03f, .03f, .03f));
        concrete = FrontRoomsPoolMaterials.Surface("Stair_Slab", new Color(.6f, .6f, .58f));
        metal = FrontRoomsPoolMaterials.Surface("Painted_Metal", new Color(.42f, .41f, .38f));
        glass = FrontRoomsStructuresKit.GlassMaterial();

        kit.Safe("field", BuildField);
        kit.Safe("V1 cut", BuildCut);
        kit.Safe("V1 patch below", BuildPatch);
        kit.Safe("V2 drop", BuildDrop);
        kit.Safe("V4 tall grid", BuildTall);
        kit.Safe("computer room", BuildComputerRoom);
        kit.Safe("split suite", BuildSplitSuite);
        kit.Safe("beyond the field", BuildBeyond);
        kit.Finish();
        kit.Safe("signs", BuildSigns);

        Box = new Bounds(Site + new Vector3(105f, 5f, 15f), new Vector3(214f, 20f, 34f));
        LowestFloor = Site.y - 2.4f;
        Far = 80f;
        Stops.Clear();
        kit.Stop("到达点：主走廊西头", Arrival, new Vector3(39f, 1.62f, 10.5f));
        kit.Stop("V1 · 楼板开口的栏杆边", new Vector3(9f, 0f, 10.7f), new Vector3(9f, Patch, 17f));
        kit.Stop("V2 · 看台边", new Vector3(32f, 0f, 10.7f), new Vector3(30f, -2.4f, 19f));
        kit.Stop("V2 · 坑底", new Vector3(33f, -2.4f, 15.5f), new Vector3(28f, .6f, 11f));
        kit.Stop("V4 · 高房间里", new Vector3(45f, 0f, 9f), new Vector3(43.5f, 9f, 13.5f));
        kit.Stop("V4 · 地面的缝，墙一直往上", new Vector3(46.5f, 0f, 12f), new Vector3(39f, 4.4f, 10.7f), 30f);   // review SP3-F16
        kit.Stop("机房门口", new Vector3(11.6f, 0f, 6.6f), new Vector3(6.0f, .3f, 2.2f));
        kit.Stop("分层套间：地面", new Vector3(27.6f, 0f, 4.6f), new Vector3(34.5f, 2.2f, 4.5f));
        kit.Stop("分层套间：夹层", new Vector3(29f, Gallery, 7.8f), new Vector3(30f, 1.2f, 1.5f));
        BeyondStops();
    }

    // ---------------------------------------------------------------- the field: one plate in Level 0's own finish

    void BuildField()
    {
        var p = new FrontRoomsStructuresKit.Plate(0f, 0f, 0f, 16, 8)
        {
            WallMat = wall, FloorMat = carpet, FloorThick = .05f, CeilingMat = ceiling, LedgeMat = wall, TreadMat = carpet,
            Troffers = true, VoidGuard = FrontRoomsStructuresKit.GuardStyle.Rail, LedgeGuard = FrontRoomsStructuresKit.GuardStyle.Rail, SectionMat = null,
        };
        var floor = FrontRoomsStructuresKit.Plate.Kind.Floor;
        // The spine (row 3, x 0..36) and the cross corridor (column 12): one room.
        p.Fill(0, 3, 11, 3, floor, 0f, 2.9f, Corr);
        p.Fill(12, 0, 12, 7, floor, 0f, 2.9f, Corr);
        // V1: the suite and its cut (2 x 2 void cells, off both its centre lines).
        p.Fill(0, 4, 4, 7, floor, 0f, 2.9f, SuiteC);
        p.Fill(2, 4, 3, 5, FrontRoomsStructuresKit.Plate.Kind.Void, 0f, 0f, -1);
        // Plain rooms.
        p.Fill(5, 4, 7, 7, floor, 0f, 2.9f, RoomF1);
        p.Fill(4, 0, 7, 2, floor, 0f, 2.9f, RoomF2);
        p.Fill(13, 0, 15, 1, floor, 0f, 2.9f, RoomSE);
        p.Fill(13, 6, 15, 7, floor, 0f, 2.9f, RoomNE);
        // The computer room: the entry column at 0, the rest +0.30; the ramp's cells are built by hand.
        p.Fill(0, 0, 2, 2, floor, .3f, 2.9f, Comp);
        p.Fill(1, 0, 2, 0, FrontRoomsStructuresKit.Plate.Kind.Held, .3f, 2.9f, Comp);
        p.Fill(3, 0, 3, 2, floor, 0f, 2.9f, Comp);
        // V2: the flight's cells and the pit at -2.40, the dais at -1.50, the ceiling kept at +2.90.
        p.Fill(8, 4, 11, 6, floor, -2.4f, 2.9f, Drop);
        p.Fill(8, 7, 11, 7, floor, -1.5f, 2.9f, Drop);
        // The split suite, Tall 5.4: platform +1.20, floor and ground 0 (the gallery over the ground is built by hand).
        p.Fill(8, 0, 11, 0, floor, 1.2f, SplitCeil, Split);
        p.Fill(8, 1, 11, 2, floor, 0f, SplitCeil, Split);
        // V4: the tall grid.
        p.Fill(13, 2, 15, 5, floor, 0f, TallCeil, Tall);
        // Their own lamps (x1.6, x6.4) are hung below; the cell (0, 7) has two missing tiles.
        p.Dark(8, 4, 11, 7);
        p.Dark(8, 0, 11, 2);
        p.Dark(13, 2, 15, 5);
        p.Dark(7, 3, 12, 3);                               // hung below as view lamps (the arrival's main view: review SP3-F2)
        p.NoLid(0, 7, 0, 7);

        // V2 opens on the spine: no wall; the flight starts at the edge, a rail on the stage ledge (built below).
        for (var i = 8; i <= 11; i++) p.NoWall(i, 4, 'S');
        p.Run(9, 7, 'S', 0f, 3f);                          // dais <- pit: 6 risers at x 27..30
        // The split suite's edges: glass guards on the platform; L's foot and the step run cross it.
        for (var i = 8; i <= 11; i++) p.GuardEdge(i, 0, 'N', FrontRoomsStructuresKit.GuardStyle.Glass);
        p.Crossed(8, 0, 'N', 0f, 1.25f);
        p.Run(10, 0, 'N', 1.0f, 2.2f);                     // floor -> platform: 8 risers, 1.2 wide
        // The computer room: a STEP (two risers) facing its arch, the ramp's landing, a kerb elsewhere.
        p.Run(2, 1, 'E', 0f, 3f);
        p.Crossed(2, 0, 'E', 0f, 1.2f);
        // LMS doorless arches (rectangular, top 2.2, off-centre).
        p.Open(0, 3, 'N', .6f, 2.1f, 0f, 2.2f);            // spine -> V1 suite
        p.Open(4, 6, 'E', .5f, 2.0f, 0f, 2.2f);            // V1 suite -> room f
        p.Open(6, 3, 'N', .9f, 2.4f, 0f, 2.2f);            // spine -> room f (north)
        p.Open(5, 2, 'N', .7f, 2.2f, 0f, 2.2f);            // room f (south) -> spine
        p.Open(3, 1, 'E', .8f, 2.3f, 0f, 2.2f);            // computer room <- room f
        p.Open(7, 2, 'E', .6f, 2.1f, 0f, 2.2f);            // room f -> split suite ground
        p.Open(11, 2, 'E', .9f, 2.4f, 0f, 2.2f);           // split suite ground -> cross corridor
        p.Open(11, 7, 'E', 1.1f, 2.7f, 0f, 2.2f);          // V2's second way -> cross corridor
        p.Open(12, 0, 'E', .8f, 2.2f, 0f, 2.2f);           // cross corridor -> room f (south-east)
        p.Open(12, 7, 'E', .6f, 2.0f, 0f, 2.2f);           // cross corridor -> room f (north-east)
        p.Open(12, 3, 'E', .9f, 2.5f, 0f, 2.2f);           // V4's slot, end-on to the spine (z 9.9..11.5)
        p.Open(15, 1, 'E', .8f, 2.3f, 0f, 2.2f);           // on east, out of the field to (f)-(k)
        p.Build(kit);
        // 0.6 m columns on the 6 m grid inside the two plain rooms of 3 x 3 or more.
        kit.Shell(2.9f, 0f);
        kit.Box(17.7f, 0f, 17.7f, 18.3f, 2.9f, 18.3f, wall);
        kit.Box(17.7f, 0f, 5.7f, 18.3f, 2.9f, 6.3f, wall);
        // The cell (0, 7): two missing tiles opening into black (r1 a3).
        MissingTiles(0f, 21f, new[] { new Vector2(.6f, 21.0f), new Vector2(2.4f, 23.4f) });
    }

    // A cell's lid at 2.9 with 0.6 m tiles left out (black plenum above them) and its troffer.
    void MissingTiles(float x, float z, Vector2[] holes)
    {
        kit.Shell(2.9f, 0f);
        const float y = 2.9f;
        // Strips round the holes: rows of 0.6 m.
        for (var r = 0; r < 5; r++)
        {
            var z0 = z + r * .6f;
            var cuts = new List<float>();
            foreach (var h in holes) if (Mathf.Abs(h.y - z0) < .01f) cuts.Add(h.x);
            cuts.Sort();
            var cur = x;
            foreach (var c in cuts)
            {
                if (c > cur + .01f) kit.Lid(cur, z0, c, z0 + .6f, y, ceiling);
                cur = c + .6f;
            }
            if (cur < x + 3f - .01f) kit.Lid(cur, z0, x + 3f, z0 + .6f, y, ceiling);
        }
        foreach (var h in holes)
        {
            kit.Box(h.x - .05f, y + .16f, h.y - .05f, h.x + .65f, y + .7f, h.y + .65f, plenum, false);
        }
        kit.Troffer(new Vector3(x + 1.5f, y, z + 1.5f + FrontRooms.Map.ModuleUnits.TrofferOffsetZ), false);
    }

    // ---------------------------------------------------------------- V1: the cut (must)

    void BuildCut()
    {
        // The suite's troffers run on over the cut: the grid is cut from the floor only.
        kit.Shell(2.9f, 0f);
        kit.Lid(6f, 12f, 12f, 18f, 2.9f, ceiling);
        foreach (var c in new[] { new Vector2(7.5f, 13.5f), new Vector2(10.5f, 13.5f), new Vector2(7.5f, 16.5f), new Vector2(10.5f, 16.5f) })
            kit.Troffer(new Vector3(c.x, 2.9f, c.y + FrontRooms.Map.ModuleUnits.TrofferOffsetZ), false);
        // The section on all four sides: carpet (the plate's), a 0.20 slab, the open plenum down to the lower grid at -1.00.
        kit.Shell(0f, PatchCeil);
        kit.Box(5f, -.25f, 11f, 13f, -.05f, 12f, concrete);       // south side
        kit.Box(5f, -.25f, 18f, 13f, -.05f, 19f, concrete);       // north
        kit.Box(5f, -.25f, 12f, 6f, -.05f, 18f, concrete);        // west
        kit.Box(12f, -.25f, 12f, 13f, -.05f, 18f, concrete);      // east
        // The plenum's dark round the cut (under the slab, over the lower grid), so the gap reads as a gap: a dark grey lining
        // (0.10), not black, so slab, duct and plenum read as bands of their own (review SP3-F15).
        var lining = FrontRoomsPoolMaterials.Lit("SP3 plenum lining", new Color(.10f, .10f, .10f), .2f);
        foreach (var y in new[] { -.27f, PatchCeil + .16f })
        {
            kit.Box(2f, y, 8f, 16f, y + .02f, 12f, lining, false);
            kit.Box(2f, y, 18f, 16f, y + .02f, 22f, lining, false);
            kit.Box(2f, y, 12f, 6f, y + .02f, 18f, lining, false);
            kit.Box(12f, y, 12f, 16f, y + .02f, 18f, lining, false);
        }
        // A duct and a sprinkler branch crossing the plenum, seen past the slab edge: the duct's face 0.25 behind the edge,
        // all of it under the slab and facing the cut's troffers (review SP3-F15).
        var galv = FrontRoomsPoolMaterials.Surface("RunB_GalvSteel", new Color(.6f, .62f, .63f));
        kit.Box(2.5f, -.75f, 18.25f, 15.5f, -.35f, 18.85f, galv, false);
        kit.Box(9.6f, -.5f, 8.5f, 9.66f, -.44f, 11.95f, metal, false);
        kit.Box(9.6f, -.5f, 18.05f, 9.66f, -.44f, 21.5f, metal, false);
    }

    // The plain room 3.9 m below (unreachable): x 0..18, z 6..24, its ceiling at -1.00 open under the cut, one troffer per
    // lid cell, the r1 still's desk, chair and two files by the cut.
    void BuildPatch()
    {
        kit.Shell(PatchCeil, Patch);
        kit.Slab(0f, 6f, 18f, 24f, Patch, carpet, .2f);
        kit.WallX(6f, -.08f, 18.08f, Patch, PatchCeil, wall);
        kit.WallX(24f, -.08f, 18.08f, Patch, PatchCeil, wall);
        kit.WallZ(0f, 6f, 24f, Patch, PatchCeil, wall);
        kit.WallZ(18f, 6f, 24f, Patch, PatchCeil, wall);
        kit.Lid(0f, 6f, 18f, 12f, PatchCeil, ceiling);
        kit.Lid(0f, 18f, 18f, 24f, PatchCeil, ceiling);
        kit.Lid(0f, 12f, 6f, 18f, PatchCeil, ceiling);
        kit.Lid(12f, 12f, 18f, 18f, PatchCeil, ceiling);
        for (var i = 0; i < 6; i++)
            for (var j = 2; j < 8; j++)
            {
                if (i >= 2 && i <= 3 && j >= 4 && j <= 5) continue;
                kit.Troffer(new Vector3(3f * i + 1.5f, PatchCeil, 3f * j + 1.5f + FrontRooms.Map.ModuleUnits.TrofferOffsetZ), false);
            }
        kit.Kit("Kit_OfficeDesk", new Vector3(8.2f, Patch, 15.4f), 20f, new Vector3(1.52f, .74f, .76f));
        kit.Kit("Kit_TaskChair", new Vector3(8.4f, Patch, 14.5f), 200f, new Vector3(.6f, .9f, .6f));
        kit.Kit("Kit_FilingCabinet", new Vector3(10.8f, Patch, 17.3f), 180f, new Vector3(.46f, 1.32f, .7f));
        kit.Kit("Kit_FilingCabinet", new Vector3(11.4f, Patch, 17.3f), 180f, new Vector3(.46f, 1.32f, .7f));
    }

    // ---------------------------------------------------------------- V2: the drop (must)

    void BuildDrop()
    {
        // The flight (B2, its own cells): 16 risers of 0.15 on 0.30 treads, 6 m wide, from the spine down to -2.40 at z 16.5.
        kit.Shell(2.9f, -2.4f);
        kit.Flight(new Vector3(27f, -2.4f, 16.5f), Vector3.back, 6f, 16, .15f, .3f, carpet, null, true);
        // The flight's base is drawn solid but collides as its ramp only: close the 0.7 m under its top end, where the ramp's
        // soffit clears the player's 1.75 m capsule, so nobody walks into the stair and off the pit's edge under the spine
        // (walk test 11-04, SP3 BUG 1: out of the world). Collision only; the top stays under the soffit.
        kit.Mesh.Collider(new Vector3(27f, -1.575f, 12.35f), new Vector3(6f, 1.65f, .7f));
        // The stage ledge (x 30..36): the pit's south face and a rail on the spine side.
        kit.Box(30f, -2.4f, 12f, 36f, 0f, 12.1f, wall);
        kit.Shell(2.9f, 0f);
        kit.Guard(new Vector3(30f, 0f, 11.92f), new Vector3(36f, 0f, 11.92f), 0f, FrontRoomsStructuresKit.GuardStyle.Rail);
        // The flight's east cheek: a rail stepping down with it.
        kit.Shell(2.9f, -2.4f);
        SlopedRail(new Vector3(29.92f, 0f, 12.0f), new Vector3(29.92f, -2.25f, 16.2f));
        // The second way (R6): a step run of 10 risers rising east from the dais to the arch at x 36, a rail on its south side.
        kit.StepRun(new Vector3(36f, 0f, 22.92f), Vector3.right, 1.84f, -1.5f, 0f, carpet, null);
        SlopedRail(new Vector3(33.0f, -1.5f, 21.98f), new Vector3(36f, 0f, 21.98f));
        // Lamps x1.6 (the room's own troffers; ceiling flat at +2.90: 5.30 clear over the pit, 4.40 over the dais).
        for (var i = 8; i <= 11; i++)
            for (var j = 4; j <= 7; j++)
                kit.Troffer(new Vector3(3f * i + 1.5f, 2.9f, 3f * j + 1.5f + FrontRooms.Map.ModuleUnits.TrofferOffsetZ), true, false, 1.6f, 12f);
        // The spine's cells from x 21 to the junction: view lamps (its far third is the arrival's main view; review SP3-F2).
        for (var i = 7; i <= 12; i++) kit.Troffer(new Vector3(3f * i + 1.5f, 2.9f, 10.5f + FrontRooms.Map.ModuleUnits.TrofferOffsetZ), true);
        // The Relay stand-in on the dais, seen from the stage (COMMON rule 12).
        kit.Relay(new Vector3(25.5f, -1.5f, 22.6f), new Vector3(30f, 0f, 11f));
    }

    // A rail on a slope from a to b (nosing-line points, district metres): posts every 1.2, top and mid rails, one collider.
    void SlopedRail(Vector3 a, Vector3 b)
    {
        var d = b - a;
        var len = d.magnitude;
        if (len < .05f) return;
        var flat = new Vector3(d.x, 0f, d.z).normalized;
        var count = Mathf.Max(1, Mathf.CeilToInt(new Vector3(d.x, 0f, d.z).magnitude / 1.2f));
        for (var k = 0; k <= count; k++)
        {
            var q = Vector3.Lerp(a, b, k / (float)count);
            kit.Prism(new Vector3(q.x, 0f, q.z) - flat * .02f, new Vector3(q.x, 0f, q.z) + flat * .02f, q.y, q.y + FrontRoomsStructuresKit.GuardHeight, .04f, metal, false);
        }
        var rot = Quaternion.LookRotation(d / len, Vector3.up);
        var mid = (a + b) * .5f;
        kit.Mesh.Box(mid + Vector3.up * (FrontRoomsStructuresKit.GuardHeight - .025f), new Vector3(.05f, .05f, len + .04f), rot, metal, false);
        kit.Mesh.Box(mid + Vector3.up * .53f, new Vector3(.03f, .03f, len), rot, metal, false);
        kit.Mesh.Collider(mid + Vector3.up * (FrontRoomsStructuresKit.GuardHeight * .5f), new Vector3(.05f, FrontRoomsStructuresKit.GuardHeight, len), rot);
    }

    // ---------------------------------------------------------------- V4: the tall grid (must)

    void BuildTall()
    {
        // The same paper, grid and troffers to 14.6 (the plate's walls and lid); lamps 20 at range 22 in a 120 / 80 degree cone
        // (32 in the game's 162 / 96 cone grazed the top 2 m of wall to white: review SP3-F4, by eye), kept on while in SP3.
        var scale = 20f / FrontRoomsTransitionLightLead.Level0Intensity;
        for (var i = 13; i <= 15; i++)
            for (var j = 2; j <= 5; j++)
            {
                var l = kit.Troffer(new Vector3(3f * i + 1.5f, TallCeil, 3f * j + 1.5f + FrontRooms.Map.ModuleUnits.TrofferOffsetZ), true, false, scale, 22f);
                if (l != null) { l.spotAngle = 120f; l.innerSpotAngle = 80f; }
            }
        // A water cooler and a chair for scale.
        kit.Kit("Kit_WaterCooler", new Vector3(46.9f, 0f, 16.9f), 225f, new Vector3(.32f, 1.1f, .32f));
        kit.Kit("Kit_TaskChair", new Vector3(44.6f, 0f, 15.8f), 200f, new Vector3(.6f, .9f, .6f));
    }

    // ---------------------------------------------------------------- the computer room (T_L0_ComputerRoom, must)

    void BuildComputerRoom()
    {
        // The raised floor's hand-built cells (1..2, 0): +0.30 round a 1:12 ramp, 1.2 wide on the south wall, x 9 -> 5.4.
        kit.Shell(2.9f, .3f);
        kit.Box(3f, 0f, 1.2f, 9f, .3f, 3f, carpet);
        kit.Box(3f, 0f, 0f, 5.4f, .3f, 1.2f, carpet);
        kit.Ramp(new Vector3(9f, 0f, .6f), Vector3.left, 1.2f, 3.6f, .3f, carpet, .15f);
        kit.Lid(3f, 0f, 9f, 3f, 2.9f, ceiling);
        kit.Troffer(new Vector3(4.5f, 2.9f, 1.5f + FrontRooms.Map.ModuleUnits.TrofferOffsetZ), false);
        kit.Troffer(new Vector3(7.5f, 2.9f, 1.5f + FrontRooms.Map.ModuleUnits.TrofferOffsetZ), false);
        // Grey cabinet rows (62 2.3, r3 still S1), 1.8 tall, with aisles between them.
        var grey = FrontRoomsPoolMaterials.Lit("computer cabinet grey", new Color(.62f, .62f, .60f), .35f);
        kit.StandIn("computer cabinet row", new Vector3(3.9f, .3f + .9f, 4.35f), new Vector3(6.6f, 1.8f, .6f), grey);
        kit.StandIn("computer cabinet row", new Vector3(3.9f, .3f + .9f, 7.05f), new Vector3(6.6f, 1.8f, .6f), grey);
        kit.StandIn("computer console", new Vector3(1.4f, .3f + .4f, 1.9f), new Vector3(1.6f, .8f, .7f), grey);
    }

    // ---------------------------------------------------------------- the split suite (T_L0_SplitSuite, must)

    void BuildSplitSuite()
    {
        // The gallery at +2.70 over the ground row (slab 0.20, 2.50 clear under it), a landing out to L's top.
        kit.Shell(SplitCeil, Gallery);
        kit.Slab(24f, 6f, 36f, 9f, Gallery, carpet, .2f);
        kit.Slab(24f, 5.7f, 25.25f, 6f, Gallery, carpet, .2f);
        // L: the flight platform -> gallery on the west wall, 10 risers of 0.15, 1.2 wide, on a solid base.
        kit.Shell(SplitCeil, 1.2f);
        kit.Box(24.08f, 0f, 3f, 25.25f, 1.2f, 5.7f, wall);
        kit.Flight(new Vector3(24.66f, 1.2f, 3.0f), Vector3.forward, 1.17f, 10, .15f, .3f, carpet, null, true);
        SlopedRail(new Vector3(25.3f, 1.2f, 3.0f), new Vector3(25.3f, Gallery - .15f, 5.7f));
        // The step run floor -> platform (the plate's): rails on its open sides.
        kit.Shell(SplitCeil, 0f);
        SlopedRail(new Vector3(30.95f, 0f, 5.1f), new Vector3(30.95f, 1.05f, 3.0f));
        SlopedRail(new Vector3(32.25f, 0f, 5.1f), new Vector3(32.25f, 1.05f, 3.0f));
        // @: the B4-S, 2.5 across round a post, 15 risers of 0.18 in 14 treads of 23 degrees (322: never under itself), the
        // last riser on the north radius; balusters as a thin screen on the outer edge; out of the Relay's graph. It was 1.7
        // across with 12 x 0.225 in 30 degree treads: about 45 degrees on the player's walking line, which the game's
        // controller (r 0.3, slope 45) cannot climb (walk test 11-04). Still one cell (40 11.9). An unseen collision post
        // r 0.32 keeps the player's walking line 0.62 m out, where the ramp is 36 degrees at most.
        var c = new Vector3(34.5f, 0f, 4.5f);
        kit.Ring(c, 0f, .12f, 0f, Gallery + 1.0f, -180f, 180f, metal, true, null, 30f);
        kit.Ring(c, 0f, .32f, 0f, Gallery, -180f, 180f, null, true, null, 30f);
        kit.Spiral(c, .12f, 1.25f, 128f, 23f, 15, .18f, 0f, carpet, null, .2f, -1, 0f, 0f, .025f, metal, FrontRoomsStructuresKit.GuardHeight);
        // The top landing west of the last riser, over the entry gap and the first tread only (2.5 m clear under it there, and
        // the player's 1.75 m capsule clears it from the next treads), joining the gallery; glass on its west and south edges.
        kit.Shell(SplitCeil, Gallery);
        kit.Slab(33.6f, 4.9f, 34.5f, 6f, Gallery, carpet, .2f);
        kit.Guard(new Vector3(33.68f, 0f, 4.9f), new Vector3(33.68f, 0f, 5.95f), Gallery, FrontRoomsStructuresKit.GuardStyle.Glass);
        kit.Guard(new Vector3(33.6f, 0f, 4.98f), new Vector3(34.42f, 0f, 4.98f), Gallery, FrontRoomsStructuresKit.GuardStyle.Glass);
        kit.RelayBlocker("B4-S foot", new Vector3(33.6f, 0f, 4.9f), new Vector3(34.6f, 2.4f, 6.0f));
        kit.RelayBlocker("B4-S top", new Vector3(33.6f, Gallery, 4.9f), new Vector3(34.5f, Gallery + 2.4f, 6.0f));
        // Glass guards on the gallery's open edge except at L's top and the B4-S's landing.
        kit.Guard(new Vector3(25.25f, 0f, 6.08f), new Vector3(33.6f, 0f, 6.08f), Gallery, FrontRoomsStructuresKit.GuardStyle.Glass);
        kit.Guard(new Vector3(34.5f, 0f, 6.08f), new Vector3(35.92f, 0f, 6.08f), Gallery, FrontRoomsStructuresKit.GuardStyle.Glass);
        // Lamps x1.6 at the Tall lid, and troffers in the gallery's soffit over the ground route.
        kit.Shell(SplitCeil, 0f);
        for (var i = 8; i <= 11; i++)
        {
            for (var j = 0; j <= 2; j++)
                kit.Troffer(new Vector3(3f * i + 1.5f, SplitCeil, 3f * j + 1.5f + FrontRooms.Map.ModuleUnits.TrofferOffsetZ), false, false, 1.6f, 12f);
            kit.Troffer(new Vector3(3f * i + 1.5f, Gallery - .2f, 7.5f + FrontRooms.Map.ModuleUnits.TrofferOffsetZ), false);
        }
    }

    // ---------------------------------------------------------------- signs (70 section 5, verbatim): the arrival and each event

    void BuildSigns()
    {
        // The arrival point's sign spot is inside the corridor wall: the sign hangs on that wall, to the right, out of the main view.
        kit.Sign(new Vector3(3.6f, 1.5f, 9.14f), new Vector3(3.6f, 1.5f, 10.5f), "SP3 · Level 0：原样的场地和几处断点",
            "这里是 Level 0 本来的走廊和空房间，每格一盏灯。沿这条主走廊往东：左手先是楼板上的一个口子，能看到楼下；再往前地面整片沉下去 2.4 米；走廊尽头的门洞里是一间 14.6 米高的房间。南边还有一间架空地板的机房和一间分层的套间。东墙外每个区块各放一处：阶梯培训室、平行碗、岛群、下沉休息区、档案夹层、平顶阶梯地面。每处都有自己的牌子。\n"
            + "灰盒占位：楼下那一层、柜子、档案架和家具、栏杆和玻璃栏板都是灰盒，正式版换成游戏里的模型和材质。黑色人形是 Relay 的静态替身（2.05 米），只用来检查视线，不会动。", 0f);
        kit.Sign(new Vector3(7.2f, 1.5f, 9.14f), new Vector3(7.2f, 1.5f, 10.5f), "V1 · 楼板开口",
            "楼板上开了一个 6×6 米的口，往下 3.9 米是下一层；口子边上露出地毯、楼板和吊顶里的风管。只能看，下不去。要你决定：这个放进 Level 0 吗？", 0f);
        kit.Sign(new Vector3(27.6f, 1.5f, 9.14f), new Vector3(27.6f, 1.5f, 10.5f), "V2 · 下沉套间",
            "整间房的地面沉下去 2.4 米（16 级台阶），天花板不动，所以里面有 5.3 米高，走廊边成了看台。最里面有一个比坑底高 0.9 米的平台，从平台另有一段台阶回到走廊。要你决定：这个放进 Level 0 吗？", 0f);
        kit.Sign(new Vector3(38.86f, 1.5f, 13.2f), new Vector3(37.5f, 1.5f, 13.2f), "V4 · 高房间",
            "2.9 米高的走廊尽头有一个 2.2 米高的门洞，里面的房间高 14.6 米，同样的墙纸和吊顶格子一直到顶。要你决定：这个放进 Level 0 吗？", 0f);
        kit.Sign(new Vector3(12.14f, 1.5f, 7.6f), new Vector3(13.5f, 1.5f, 7.6f), "机房",
            "地面架高 0.3 米，天花板不动，屋里只剩 2.6 米高。门口一级台阶，南墙边一条坡道，两条路都能上去。要你决定：这个放进 Level 0 吗？", 0f);
        kit.Sign(new Vector3(23.86f, 1.5f, 5.3f), new Vector3(22.5f, 1.5f, 5.3f), "分层套间",
            "5.4 米高的房间里有 +1.2 米的平台和 +2.7 米的夹层。夹层一头是小螺旋楼梯，另一头从平台走直梯上去。Relay 不走螺旋楼梯，只走直梯。要你决定：这个放进 Level 0 吗？这种一格大小的小螺旋楼梯可以留在 Level 0 和办公层吗？不要的话改成直跑楼梯。", 0f);
        // Metric marks (M).
        kit.Mark(new Vector3(9f, 1.25f, 11.85f), Vector3.back, "开口 6 × 6 · 下一层 −3.90 · 吊顶 −1.00");
        kit.Mark(new Vector3(33f, 1.25f, 11.85f), Vector3.back, "坑底 −2.40 · 平台 −1.50 · 天花 +2.90");
        kit.Mark(new Vector3(39.2f, 2.5f, 10.7f), Vector3.right, "高 14.60 · 门洞 1.6 × 2.2");
        kit.Mark(new Vector3(8.8f, 1.2f, 4.5f), Vector3.right, "架空 +0.30 · 净高 2.60 · 坡道 1:12");
        kit.Mark(new Vector3(28.5f, 3.9f, 6.3f), Vector3.back, "夹层 +2.70 · 平台 +1.20 · 房高 5.40");
    }

    // ---------------------------------------------------------------- the frame

    public override FrontRoomsStructuresLook.Preset PresetAt(Vector3 world)
    {
        var l = world - Site;
        if (l.x > 39f && l.x < 48f && l.z > 6f && l.z < 18f) return FrontRoomsStructuresLook.Preset.Level0Tall;
        return FrontRoomsStructuresLook.Preset.Level0;
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
        sb.AppendLine("Level 0 本来的走廊和房间（16 × 8 格），沿主走廊放了五处：V1 楼板开口、V2 下沉套间、V4 高房间、机房、分层套间。每处门口有自己的牌子。");
        sb.AppendLine();
        sb.AppendLine("按键：[ ] 换站点（到达点、V1、V2 看台、V2 坑底、V4、机房、分层套间地面、夹层，再往东六处）；R 回到当前站点；C Relay 体型（小螺旋楼梯对它关闭）；M 尺寸。");
        sb.AppendLine();
        sb.AppendLine("要你决定（在对话里按编号告诉总控）：");
        sb.AppendLine("SP3-1 V1 楼板开口放进 Level 0 吗？");
        sb.AppendLine("SP3-2 V2 下沉套间放进 Level 0 吗？");
        sb.AppendLine("SP3-3 V4 高房间放进 Level 0 吗？");
        sb.AppendLine("SP3-4 机房放进 Level 0 吗？");
        sb.AppendLine("SP3-5 分层套间放进 Level 0 吗？一格大小的小螺旋楼梯可以留在 Level 0 和办公层吗？不要的话改成直跑楼梯。");
        sb.AppendLine("SP3-6 培训室放进 Level 0 吗？台阶 0.30 米一层、一格深，还是原来的 0.45 米座位台阶（62 Q3）？");
        sb.AppendLine("SP3-7 平行碗放进 Level 0 吗？远处那个人影头被天花板切掉、身子看得见，这样算不算“看得见 Relay”？");
        sb.AppendLine("SP3-8 岛群放进 Level 0 吗？");
        sb.AppendLine("SP3-9 下沉休息区放进 Level 0 吗？");
        sb.AppendLine("SP3-10 档案夹层放进 Level 0 吗？档案夹层放在 Level 0 的高房间里，还是给办公层开高房间（62 Q2）？");
        sb.AppendLine("SP3-11 平顶阶梯地面放进 Level 0 吗？");
        sb.AppendLine();
        sb.AppendLine("灰盒占位：楼下那一层、柜子、档案架和家具、栏杆和玻璃栏板都是灰盒，正式版换成游戏里的模型和材质。黑色人形是 Relay 的静态替身（2.05 米），只用来检查视线，不会动。");
        sb.AppendLine();
        sb.AppendLine("技术记录：东墙外的六处沿一条往东的走廊排开，每处占一个区块（24 米），岛群占两个；从东南角房间的门洞出去。");
    }
}
