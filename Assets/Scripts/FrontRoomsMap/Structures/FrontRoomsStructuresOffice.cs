using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// SP4 (key 4) of scene 4 (RULES 5b; research/level_space/70 section 6; 20 V2, V3; 30 sections 2.3-2.4; 31 F1b, E5b; 40 sections
/// 4, 6.2, 11.4; 60 section 3): the Office's stack and pit. A plate of 10 x 8 cells (x 0..30, z 0..24), storeys 3.9 (F0 +-0,
/// F1 +3.9, F2 +7.8), Standard 2.9 lids, the game's Office_Wall, Office_Carpet, Office_Ceiling and office troffers; guards
/// the Office stair's glass on a putty steel band. The atrium's floor is the sunken bullpen (V2, an A3 court at -1.20, two
/// step runs at opposite corners, four pinwheel bays of 1.35, 1.52, 1.65 and 2.16 m panels, a Relay stand-in at their shared
/// crossing); over it the stack (V3) rises 11.9 m to a laylight, each floor opening a different set of faces: F0 three
/// walkways and the raised floor's ledge; F1 the west gallery, the north band and the boardroom's glass; F2 the north band
/// and the blade field. Round it: the raised computer floor (a step, a 1:12 ramp and the B9 creeping slope), the tiered
/// boardroom, the E5b blade field, the slot stair hall (F1b: two straight flights crossing between three floors) and a B3
/// core as the second way. No spiral of any size: the Relay could take every route. Greybox where no kit exists, listed on
/// the signs.
/// </summary>
public sealed class FrontRoomsStructuresOffice : FrontRoomsStructuresDistrict
{
    const float S1 = 3.9f, S2 = 7.8f, L0 = 2.9f, L1 = 6.8f, L2 = 10.7f, Pit = -1.2f, Raised = .3f;
    const int RoomW = 1, RoomSlot = 2, RoomG1 = 3, RoomB = 4, RoomN2 = 5;
    static readonly Vector3 Arrival = new Vector3(4.5f, 0f, 2.2f);

    FrontRoomsStructuresKit kit;
    Material wall, carpet, ceiling, fabric, metal, concrete, band, glass;

    public FrontRoomsStructuresOffice(Vector3 site)
        : base(4, "SP4", "SP4 · 办公层：三层中庭，底下是下沉工位区", "三层办公中庭和下沉工位区。", site) { }

    // ---------------------------------------------------------------- build

    public override void Build(FrontRoomsStructures host)
    {
        kit = new FrontRoomsStructuresKit(host, this);
        wall = FrontRoomsPoolMaterials.Surface("Office_Wall", new Color(.82f, .80f, .74f));
        carpet = FrontRoomsPoolMaterials.Surface("Office_Carpet", new Color(.35f, .37f, .40f));
        ceiling = FrontRoomsPoolMaterials.Surface("Office_Ceiling", new Color(.85f, .85f, .82f));
        fabric = FrontRoomsPoolMaterials.Surface("Prop_FabricCubicle", new Color(.45f, .47f, .50f));
        metal = FrontRoomsPoolMaterials.Surface("Painted_Metal", new Color(.42f, .41f, .38f));
        concrete = FrontRoomsPoolMaterials.Surface("Stair_Slab", new Color(.6f, .6f, .58f));
        band = FrontRoomsStructuresKit.BandMaterial();
        glass = FrontRoomsStructuresKit.GlassMaterial();

        kit.Safe("F0 atrium floor", BuildF0);
        kit.Safe("F1", BuildF1);
        kit.Safe("F2", BuildF2);
        kit.Safe("pit and bays", BuildPit);
        kit.Safe("raised floor and creeping slope", BuildRaised);
        kit.Safe("boardroom", BuildBoardroom);
        kit.Safe("blade field", BuildBlades);
        kit.Safe("stack faces and laylight", BuildStack);
        kit.Safe("slot stair hall", BuildSlot);
        kit.Safe("core", BuildCore);
        kit.Finish();
        kit.Safe("zone volume", () => FrontRoomsPostStack.EnsureZoneVolume(Root, "Office", new Bounds(new Vector3(15f, 4.8f, 12f), new Vector3(30f, 12.4f, 24f))));
        kit.Safe("signs", BuildSigns);

        Box = new Bounds(Site + new Vector3(15f, 5f, 12f), new Vector3(34f, 16f, 28f));
        LowestFloor = Site.y + Pit;
        Far = 80f;
        Stops.Clear();
        kit.Stop("到达点：南边走道的栏板", Arrival, new Vector3(15f, 5f, 13.5f));
        kit.Stop("坑底：隔着隔板看交叉口", new Vector3(4.6f, Pit, 8.15f), new Vector3(10.05f, Pit + 1.5f, 8.15f));
        kit.Stop("坑底：隔着 1.35 米隔板看替身", new Vector3(5.0f, Pit, 3.6f), new Vector3(10.05f, Pit + 1.5f, 8.15f));
        kit.Stop("坑底：隔着 1.52 米隔板看替身", new Vector3(15.1f, Pit, 3.6f), new Vector3(10.05f, Pit + 1.5f, 8.15f));
        kit.Stop("坑底：隔着 1.65 米隔板看替身", new Vector3(15.1f, Pit, 12.7f), new Vector3(10.05f, Pit + 1.5f, 8.15f));
        kit.Stop("坑底：隔着 2.16 米隔板看替身", new Vector3(5.8f, Pit, 12.3f), new Vector3(10.05f, Pit + 1.5f, 8.15f));
        kit.Stop("抬高地板", new Vector3(23.6f, Raised, 9.0f), new Vector3(10.05f, Pit + 1.2f, 8.15f));
        kit.Stop("缓坡起点", new Vector3(18.6f, 0f, 1.5f), new Vector3(29f, 1.62f, 1.5f));
        kit.Stop("二楼：玻璃会议室最高一级", new Vector3(25.6f, S1 + .6f, 10.5f), new Vector3(14f, S1 + 1.2f, 10.5f));
        kit.Stop("二楼：西边画廊", new Vector3(1.5f, S1, 9f), new Vector3(15f, S1 + .8f, 9f));
        kit.Stop("三楼：北边走廊看隔板阵", new Vector3(21f, S2, 17.2f), new Vector3(21f, S2 + 1f, 9f));
        kit.Stop("窄长楼梯厅", new Vector3(1.5f, 0f, 20.9f), new Vector3(15f, 6f, 20.9f));
        // Review SP4-F17: the F2 look-down, the B7 ramp, the B1 step and the B3 core (walk tests 70 l.409-411, R6).
        kit.Stop("三楼：往下看坑底的替身", new Vector3(10.5f, S2, 16.2f), new Vector3(10.05f, Pit, 8.15f), 70f);
        kit.Stop("B7 缓坡脚下往上看", new Vector3(29.3f, 0f, 14.6f), new Vector3(29.3f, 1.9f, 9.5f));
        kit.Stop("B1 拱门里的两级台阶", new Vector3(19.5f, 0f, 16.8f), new Vector3(19.5f, 1.4f, 12f));
        kit.Stop("B3 楼梯间，往上看第一段", new Vector3(18.6f, 0f, 18.7f), new Vector3(22.8f, 2.9f, 18.73f), 45f);
    }

    // ---------------------------------------------------------------- F0: walkways, band, pit, raised floor, the slot's floor

    void BuildF0()
    {
        var p = Plate(0f);
        var floor = FrontRoomsStructuresKit.Plate.Kind.Floor;
        var held = FrontRoomsStructuresKit.Plate.Kind.Held;
        // One room: the walkways, the band, the pit and the raised floor (their edges are ledges with guards).
        p.Fill(0, 0, 5, 0, floor, 0f, L0, RoomW);           // the south walkway (the arrival)
        p.Fill(0, 1, 0, 4, floor, 0f, L0, RoomW);           // the west walkway
        p.Fill(0, 5, 9, 5, floor, 0f, L0, RoomW);           // the north band
        p.Fill(1, 1, 5, 4, floor, Pit, L0, RoomW);          // the pit (the atrium's floor; open above)
        p.Fill(6, 1, 9, 4, floor, Raised, L0, RoomW);       // T_OF_RaisedFloor
        p.Fill(9, 3, 9, 4, held, Raised, L0, RoomW);        //   its ramp's cells, built by hand
        p.Fill(6, 0, 9, 0, held, 0f, L0, RoomW);            // B9, built by hand
        p.Fill(0, 6, 5, 7, floor, 0f, L2, RoomSlot);        // the slot stair hall's floor, its lid at +10.7
        p.NoLid(1, 1, 5, 4);
        p.Dark(0, 6, 5, 7);                                 // the slot hall's lid troffers are hung below (review SP4-F3)
        // The raised floor's walls to the band and the slope (Office_Wall), with their ways on.
        for (var i = 6; i <= 9; i++) { p.ForceWall(i, 4, 'N'); p.ForceWall(i, 0, 'N'); }
        p.Open(6, 4, 'N', .6f, 2.4f, 0f, Raised + 2.2f);    // B1: a STEP through a 1.8 m arch (x 18.6..20.4)
        p.Run(6, 4, 'N', .6f, 2.4f);
        p.Open(9, 4, 'N', 1.7f, 2.9f, 0f, 2.2f);            // B7's foot (x 28.7..29.9)
        p.Open(9, 0, 'N', .6f, 2.4f, Raised, Raised + 2.2f); // B9's end (x 27.6..29.4)
        // The pit's two step runs (8 risers, 2.4 wide) at opposite corners; glass guards on the rest of its edges.
        p.Run(0, 4, 'E', .6f, 3f);                          // W run, x 3 eastward, z 12.6..15.0
        p.Run(5, 0, 'N', .6f, 3f);                          // S run, z 3 northward, x 15.6..18.0
        // The slot opens to the band at x 0..3 and x 12..18 on F0; flight 1's wall stands on z 18 (built by hand).
        for (var i = 0; i <= 5; i++) p.NoWall(i, 6, 'S');
        // The core (x 18..24, z 18..21) is built by hand.
        p.NoWall(6, 5, 'N');
        p.NoWall(7, 5, 'N');
        p.NoWall(5, 6, 'E');
        p.Build(kit);
        // The slot hall's own troffers in its lid at +10.7, strong enough to reach its floor (10 m down), view lamps.
        for (var i = 0; i <= 5; i++)
            for (var j = 6; j <= 7; j++)
                kit.Troffer(new Vector3(3f * i + 1.5f, L2, 3f * j + 1.5f + FrontRooms.Map.ModuleUnits.TrofferOffsetZ), true, true, 3f, 20f);
        // B9's lid and lamps (its cells are the slope's).
        kit.Shell(L0, 0f);
        kit.Lid(18f, 0f, 30f, 3f, L0, ceiling);
        for (var i = 6; i <= 9; i++) kit.Troffer(new Vector3(3f * i + 1.5f, L0, 1.5f + FrontRooms.Map.ModuleUnits.TrofferOffsetZ), false, true);
        kit.Lid(27f, 9f, 30f, 15f, L0, ceiling);
        kit.Troffer(new Vector3(28.5f, L0, 10.5f + FrontRooms.Map.ModuleUnits.TrofferOffsetZ), false, true);
        kit.Troffer(new Vector3(28.5f, L0, 13.5f + FrontRooms.Map.ModuleUnits.TrofferOffsetZ), false, true);
    }

    FrontRoomsStructuresKit.Plate Plate(float y0) => new FrontRoomsStructuresKit.Plate(0f, 0f, y0, 10, 8)
    {
        WallMat = wall, FloorMat = carpet, FloorThick = .05f, CeilingMat = ceiling, LedgeMat = wall, TreadMat = carpet,
        NosingMat = FrontRoomsPoolMaterials.Surface("Stair_Band", new Color(.3f, .3f, .3f)),
        Troffers = true, Office = true, ViewLamps = true,
        VoidGuard = FrontRoomsStructuresKit.GuardStyle.Glass, LedgeGuard = FrontRoomsStructuresKit.GuardStyle.Glass, SectionMat = null,
    };

    // ---------------------------------------------------------------- F1: west gallery, north band, boardroom, the slot's F1 landing

    void BuildF1()
    {
        var p = Plate(S1);
        var floor = FrontRoomsStructuresKit.Plate.Kind.Floor;
        var voidKind = FrontRoomsStructuresKit.Plate.Kind.Void;
        p.Fill(0, 1, 0, 4, floor, 0f, L0, RoomG1);          // the west gallery
        p.Fill(0, 5, 7, 5, floor, 0f, L0, RoomG1);          // the north band (x 0..24 on F1)
        p.Fill(4, 6, 5, 6, floor, 0f, L2 - S1, RoomG1);     // the slot's F1 landing, open to the hall's lid
        p.Fill(5, 7, 5, 7, floor, 0f, L2 - S1, RoomG1);
        p.Fill(1, 1, 5, 4, voidKind, 0f, 0f, -1);           // the stack
        p.Fill(0, 6, 3, 6, voidKind, 0f, 0f, -1);           // the slot hall's air
        p.Fill(0, 7, 4, 7, voidKind, 0f, 0f, -1);
        // T_OF_Boardroom: tiers rising east, each edge a STEP its full 9 m; a glass front on the stack (built below).
        p.Fill(6, 2, 6, 4, floor, 0f, L0, RoomB);
        p.Fill(7, 2, 7, 4, floor, .3f, L0, RoomB);
        p.Fill(8, 2, 8, 4, floor, .6f, L0, RoomB);
        for (var j = 2; j <= 4; j++) { p.Run(6, j, 'E', 0f, 3f); p.Run(7, j, 'E', 0f, 3f); }
        for (var j = 2; j <= 4; j++) { p.ForceWall(6, j, 'W'); p.Open(6, j, 'W', .04f, 2.96f, 1.07f, L0 - .02f); }
        p.Open(6, 4, 'N', .8f, 2.2f, 0f, 2.2f);             // in from the band, x 18.8..20.2
        p.NoLid(4, 6, 5, 6);
        p.NoLid(5, 7, 5, 7);
        // Hand-built neighbours: the core, the slot hall's outer walls (the F0 plate's run to +10.7).
        p.NoWall(6, 5, 'N');
        p.NoWall(7, 5, 'N');
        p.NoWall(5, 6, 'E');
        p.NoWall(5, 7, 'E');
        p.NoWall(5, 7, 'N');
        p.Crossed(4, 6, 'W', .05f, 1.9f);                   // flight 1 arrives
        p.Crossed(5, 7, 'W', 1.1f, 2.95f);                  // flight 2 leaves
        p.Build(kit);
        // The boardroom's glass above its 1.07 sill (the sill is the guard), mullions on the cell lines.
        kit.Shell(L1, S1);
        kit.Prism(new Vector3(18f, 0f, 6f), new Vector3(18f, 0f, 15f), S1 + 1.07f, L1 - .02f, .012f, glass, true);
        foreach (var z in new[] { 9f, 12f }) kit.Prism(new Vector3(18f, 0f, z - .03f), new Vector3(18f, 0f, z + .03f), S1 + 1.07f, L1, .1f, band, false);
        // The F1 landing's structure under its carpet, seen from the slot's floor.
        kit.Shell(L0, 0f);
        kit.Box(12f, S1 - .3f, 18f, 18f, S1 - .05f, 21f, concrete);
        kit.Box(15f, S1 - .3f, 21f, 18f, S1 - .05f, 24f, concrete);
        // A run of 1.52 panels along the west gallery's back wall.
        kit.Shell(L1, S1);
        for (var k = 0; k < 7; k++) kit.Kit("Kit_CubiclePanel", new Vector3(.45f, S1, 4.0f + k * 1.524f + .762f), 90f, new Vector3(.06f, 1.52f, 1.524f), fabric);
    }

    // ---------------------------------------------------------------- F2: north band, east field, the slot's F2 landing

    void BuildF2()
    {
        var p = Plate(S2);
        var floor = FrontRoomsStructuresKit.Plate.Kind.Floor;
        var voidKind = FrontRoomsStructuresKit.Plate.Kind.Void;
        p.Fill(0, 5, 7, 5, floor, 0f, L0, RoomN2);          // the north band
        p.Fill(6, 1, 7, 4, floor, 0f, L0, RoomN2);          // the east field (E5b blades, built below)
        p.Fill(0, 6, 0, 6, floor, 0f, L2 - S2, RoomN2);     // the slot's F2 landing
        p.Fill(0, 7, 1, 7, floor, 0f, L2 - S2, RoomN2);
        p.Fill(1, 1, 5, 4, voidKind, 0f, 0f, -1);
        p.Fill(1, 6, 5, 6, voidKind, 0f, 0f, -1);
        p.Fill(2, 7, 5, 7, voidKind, 0f, 0f, -1);
        p.NoLid(0, 6, 0, 6);
        p.NoLid(0, 7, 1, 7);
        p.NoWall(6, 5, 'N');
        p.NoWall(7, 5, 'N');
        p.NoWall(0, 6, 'W');
        p.NoWall(0, 7, 'W');
        p.NoWall(0, 7, 'N');
        p.NoWall(1, 7, 'N');
        p.Crossed(1, 7, 'E', 1.1f, 2.95f);                  // flight 2 arrives
        p.Build(kit);
        kit.Shell(L0, 0f);
        kit.Box(0f, S2 - .3f, 21f, 6f, S2 - .05f, 24f, concrete);
        kit.Box(0f, S2 - .3f, 18f, 3f, S2 - .05f, 21f, concrete);
    }

    // ---------------------------------------------------------------- the pit (V2, an A3 sunken court) and its four bays

    void BuildPit()
    {
        // Glass cheeks on the runs' open sides.
        kit.Shell(L2, Pit);
        SlopedGlass(new Vector3(5.4f, Pit, 12.55f), new Vector3(3.0f, 0f, 12.55f));
        SlopedGlass(new Vector3(15.55f, Pit, 5.4f), new Vector3(15.55f, 0f, 3.0f));
        // Four pinwheel bays, 3.05 m square, 1.6 m aisles: SW 1.35 and NW 2.16 greybox, SE 1.52 and NE 1.65 the game's kit.
        Bay(new Vector2(6.2f, 4.3f), 1.35f, null, null);
        Bay(new Vector2(10.85f, 4.3f), 1.52f, "Kit_CubiclePanel", "Kit_PanelPost");
        Bay(new Vector2(10.85f, 8.95f), 1.65f, "Kit_CubiclePanelTall", "Kit_PanelPostTall");
        Bay(new Vector2(6.2f, 8.95f), 2.16f, null, null);
        // The Relay stand-in at the crossing the four bays share, off the pit's centre lines.
        kit.Relay(new Vector3(10.05f, Pit, 8.15f), Arrival);
    }

    // A pinwheel cluster (round 1's Cluster): a cross of four 1.524 panels round a centre post, one outer panel per station.
    void Bay(Vector2 min, float height, string asset, string postAsset)
    {
        var c = new Vector3(min.x + 1.525f, Pit, min.y + 1.525f);
        Vector3[] arms = { Vector3.right, Vector3.forward, Vector3.left, Vector3.back };
        Post(c, height, postAsset);
        for (var k = 0; k < 4; k++)
        {
            var end = c + arms[k] * 1.525f;
            Panel(c, end, height, asset);
            var turn = arms[(k + 1) % 4];
            Panel(end, end + turn * 1.524f, height, asset);
            Post(end, height, postAsset);
            Post(end + turn * 1.524f, height, postAsset);
        }
    }

    void Panel(Vector3 a, Vector3 b, float height, string asset)
    {
        var mid = (a + b) * .5f;
        var d = b - a;
        if (asset != null)
        {
            // The kit's panel: pivot at its foot's centre, its length on local x, front +Z.
            var yaw = Mathf.Abs(d.x) > Mathf.Abs(d.z) ? 0f : 90f;
            kit.Kit(asset, mid, yaw, Mathf.Abs(d.x) > Mathf.Abs(d.z) ? new Vector3(1.524f, height, .06f) : new Vector3(.06f, height, 1.524f), fabric);
            return;
        }
        kit.Prism(a, b, mid.y, mid.y + height, .06f, fabric, true);
    }

    void Post(Vector3 at, float height, string asset = null)
    {
        // The kit's post (Kit_PanelPost*) where the bay uses the kit's panels, else a greybox post.
        if (asset != null && kit.Kit(asset, at, 0f, new Vector3(.064f, height, .064f), metal, false) != null) return;
        kit.Prism(at - new Vector3(.032f, 0f, 0f), at + new Vector3(.032f, 0f, 0f), at.y, at.y + height + .01f, .064f, metal, false);
    }

    // A sloped glass guard on a steel band from a to b (nosing-line points): band, pane, cap, one collider.
    void SlopedGlass(Vector3 a, Vector3 b)
    {
        var d = b - a;
        var len = d.magnitude;
        if (len < .05f) return;
        var rot = Quaternion.LookRotation(d / len, Vector3.up);
        var mid = (a + b) * .5f;
        const float h = FrontRoomsStructuresKit.GuardHeight;
        kit.Mesh.Box(mid + Vector3.up * .05f, new Vector3(.06f, .1f, len), rot, band, false);
        kit.Mesh.Box(mid + Vector3.up * ((.1f + h - .05f) * .5f), new Vector3(.012f, h - .15f, len), rot, glass, false);
        kit.Mesh.Box(mid + Vector3.up * (h - .025f), new Vector3(.06f, .05f, len + .02f), rot, band, false);
        kit.Mesh.Collider(mid + Vector3.up * (h * .5f), new Vector3(.06f, h, len), rot);
    }

    // ---------------------------------------------------------------- T_OF_RaisedFloor's ways on, and B9 the creeping slope

    void BuildRaised()
    {
        // B7: a 1:12 ramp 1.2 wide inside along the east wall, its foot at the north wall's opening: landing 1.52 at +-0,
        // 3.6 m of ramp, landing 1.52 at +0.30; a kerb with a steel handrail between it and the raised floor.
        kit.Shell(L0, Raised);
        kit.Box(27f, 0f, 9f, 28.7f, Raised, 15f, carpet);
        kit.Box(28.7f, 0f, 9f, 29.92f, Raised, 9.88f, carpet);
        kit.Slab(28.7f, 13.48f, 29.92f, 15f, 0f, carpet, .05f);
        kit.Ramp(new Vector3(29.31f, 0f, 13.48f), Vector3.back, 1.22f, 3.6f, Raised, carpet, .15f);
        kit.Box(28.62f, Raised, 9.88f, 28.7f, Raised + .1f, 15f, carpet);
        kit.Prism(new Vector3(28.66f, 0f, 9.9f), new Vector3(28.66f, 0f, 15f), Raised + .86f, Raised + .9f, .04f, FrontRoomsPoolMaterials.Steel, false);
        foreach (var z in new[] { 10.2f, 12.0f, 13.8f, 14.8f })
            kit.Prism(new Vector3(28.66f, 0f, z - .02f), new Vector3(28.66f, 0f, z + .02f), Raised, Raised + .9f, .04f, FrontRoomsPoolMaterials.Steel, false);
        // B9: the south walkway carried on east, rising 1:30 from +-0 at x 18 to +0.30 at x 27, flat to x 30.
        kit.Shell(L0, 0f);
        kit.Ramp(new Vector3(18f, 0f, 1.5f), Vector3.right, 3f, 9f, Raised, carpet, .15f);
        kit.Box(27f, 0f, 0f, 30f, Raised, 3f, carpet);
        // A row of 1.52 panels along its south wall shows the slope: 1.38 m to the grid at x 18, 1.08 at x 27.
        for (var k = 0; k < 6; k++)
        {
            var x = 19.8f + k * 1.524f + .762f;
            kit.Kit("Kit_CubiclePanel", new Vector3(x, Mathf.Clamp01((x - 18f) / 9f) * Raised, .45f), 0f, new Vector3(1.524f, 1.52f, .06f), fabric);
        }
        // Low panels (1.35, greybox) on the raised floor: the prospect field over the pit.
        kit.Shell(L0, Raised);
        RaisedBay(new Vector2(20.0f, 5.0f));
        RaisedBay(new Vector2(24.4f, 9.5f));
    }

    void RaisedBay(Vector2 min)
    {
        var c = new Vector3(min.x + 1.525f, Raised, min.y + 1.525f);
        Vector3[] arms = { Vector3.right, Vector3.forward, Vector3.left, Vector3.back };
        Post(c, 1.35f);
        for (var k = 0; k < 4; k++)
        {
            var end = c + arms[k] * 1.525f;
            kit.Prism(c, end, Raised, Raised + 1.35f, .06f, fabric, true);
            var turn = arms[(k + 1) % 4];
            kit.Prism(end, end + turn * 1.524f, Raised, Raised + 1.35f, .06f, fabric, true);
            Post(end, 1.35f);
        }
    }

    // ---------------------------------------------------------------- the boardroom (F1): the plate's tiers; it stays empty

    void BuildBoardroom()
    {
        // Lamps over each tier are the plate's troffers; the room is otherwise empty (no boardroom furniture in the kit).
        kit.Shell(L1, S1);
        kit.Mark(new Vector3(26.9f, S1 + .6f + 1.3f, 10.5f), Vector3.left, "净高 2.30 · 2.60 · 2.90");
    }

    // ---------------------------------------------------------------- E5b blade field (F2's east field)

    void BuildBlades()
    {
        // Freestanding panels 1.52 wide on the 1.5 m sub-grid, heights 1.35 / 1.65 / 2.16 (greybox) mixed on both axes; two
        // rows in three run along x (they face the north band: a wall from there, aisles from across the atrium), each missing
        // one panel that moves along from row to row; the third row runs along z (review SP4-F9). Column 0 clears the guard.
        float[] heights = { 1.35f, 1.65f, 2.16f };
        var n = 0;
        for (var gx = 0; gx < 4; gx++)
            for (var gz = 0; gz < 8; gz++)
            {
                var x = 18.92f + gx * 1.5f;
                var z = 3.75f + gz * 1.5f;
                var alongX = gz % 3 != 2;
                if (alongX && gx == gz % 4) continue;
                var h = heights[(gx + 2 * gz) % 3];
                var half = .76f;
                var a = alongX ? new Vector3(x - half, 0f, z) : new Vector3(x, 0f, z - half);
                var b = alongX ? new Vector3(x + half, 0f, z) : new Vector3(x, 0f, z + half);
                kit.Prism(a, b, S2, S2 + h, .06f, fabric, true);
                Post(new Vector3(a.x, S2, a.z), h);
                Post(new Vector3(b.x, S2, b.z), h);
                n++;
            }
    }

    // ---------------------------------------------------------------- the stack's faces, fascias and the laylight

    void BuildStack()
    {
        // Faces that are walls: south (z 3) on F1 and F2, west (x 3) on F2, east (x 18) on F1 at z 3..6.
        kit.Shell(L2, S1);
        kit.WallX(3f, 3f, 18f, L0, L2, wall);
        kit.WallZ(3f, 3f, 15f, L1, L2, wall);
        kit.WallZ(18f, 3f, 6f, L0, L1, wall);
        // 1.0 m Office_Wall fascias from each lid line to the floor above, on every open edge (a finished atrium edge).
        Fascia(3f, 3f, 3f, 15f, L0, S1, Vector3.left);          // west, under F1's gallery
        Fascia(3f, 15f, 18f, 15f, L0, S1, Vector3.forward);     // north, under F1's band
        Fascia(3f, 15f, 18f, 15f, L1, S2, Vector3.forward);     // north, under F2's band
        Fascia(18f, 6f, 18f, 15f, L0, S1, Vector3.right);       // east, under the boardroom
        Fascia(18f, 3f, 18f, 15f, L1, S2, Vector3.right);       // east, under F2's field
        // The slot's south side (z 18) where the band meets it on F1 and F2.
        Fascia(0f, 18f, 18f, 18f, L0, S1, Vector3.back);
        Fascia(0f, 18f, 18f, 18f, L1, S2, Vector3.back);
        // The laylight over the whole void at +10.7: lens panels in the 0.6 grid, cool white, steady, the brightest surface;
        // its cells carry their own light (60 section 4), tuned so the pit reads lit (20 section 7: about ten times the lamp).
        kit.Shell(L2, S2);
        var lens = FrontRoomsPoolMaterials.Glow("SP4 laylight lens", new Color(.95f, .98f, 1f) * 1.7f);
        kit.QuadPlanar(new Vector3(3f, L2, 3f), new Vector3(18f, L2, 3f), new Vector3(18f, L2, 15f), new Vector3(3f, L2, 15f), Vector3.down, lens);
        var tbar = FrontRoomsPoolMaterials.Lit("ceiling t-bar white", new Color(.9f, .9f, .88f), .3f);
        for (var x = 3.6f; x < 17.95f; x += .6f) kit.Box(x - .012f, L2 - .03f, 3f, x + .012f, L2, 15f, tbar, false);
        for (var z = 3.6f; z < 14.95f; z += .6f) kit.Box(3f, L2 - .03f, z - .012f, 18f, L2, z + .012f, tbar, false);
        kit.Box(2.9f, L2 + .05f, 2.9f, 18.1f, L2 + .25f, 15.1f, wall, false);
        // The laylight's light: 12 view lamps, range 22 and 28 (by eye) so the pit, 11.8 m down, reads lit (70 section 6; review SP4-F2).
        for (var i = 0; i < 4; i++)
            for (var j = 0; j < 3; j++)
                kit.Lamp("laylight", LightType.Spot, new Vector3(4.875f + i * 3.75f, L2 - .08f, 5f + j * 4f), Vector3.down, 140f, 100f, 28f, 22f, new Color(.95f, .98f, 1f), true, Vector3.zero, null, Color.black);
    }

    // A fascia box along a..b (plan, on the void edge) from y0 to y1, 0.2 deep on the floor side (away from the void).
    void Fascia(float x0, float z0, float x1, float z1, float y0, float y1, Vector3 awayFromVoid)
    {
        var a = new Vector3(Mathf.Min(x0, x1), y0, Mathf.Min(z0, z1));
        var b = new Vector3(Mathf.Max(x0, x1), y1, Mathf.Max(z0, z1));
        if (Mathf.Abs(awayFromVoid.x) > .5f) { if (awayFromVoid.x > 0f) b.x += .2f; else a.x -= .2f; }
        else { if (awayFromVoid.z > 0f) b.z += .2f; else a.z -= .2f; }
        kit.Box(a, b, wall, true);
    }

    // ---------------------------------------------------------------- the slot stair hall (F1b)

    void BuildSlot()
    {
        var stair = concrete;
        var nose = FrontRoomsPoolMaterials.Surface("Stair_Band", new Color(.3f, .3f, .3f));
        // Flight 1, F0 -> F1, row j 6 eastward from x 3 against the wall on z 18 (Office_Wall up to F1's slab):
        // 12 risers, a 1.8 m landing at +1.80, 14 risers; 0.15 / 0.30; closed underneath.
        kit.Shell(L2, 0f);
        kit.WallX(18f, 3f, 12f, 0f, S1 - .05f, wall);
        kit.Flight(new Vector3(3.0f, 0f, 18.98f), Vector3.right, 1.8f, 12, .15f, .3f, stair, nose, true);
        kit.Box(6.3f, 0f, 18.08f, 8.1f, 1.8f, 19.88f, stair);
        kit.Flight(new Vector3(8.1f, 1.8f, 18.98f), Vector3.right, 1.8f, 14, .15f, .3f, stair, nose, true);
        kit.Box(8.1f, 0f, 18.08f, 12f, 1.8f, 19.88f, stair);
        SlopedGlass(new Vector3(3.0f, .15f, 19.92f), new Vector3(6.3f, 1.8f, 19.92f));
        kit.Guard(new Vector3(6.3f, 0f, 19.92f), new Vector3(8.1f, 0f, 19.92f), 1.8f, FrontRoomsStructuresKit.GuardStyle.Glass);
        SlopedGlass(new Vector3(8.1f, 1.95f, 19.92f), new Vector3(12.0f, S1, 19.92f));
        // Flight 2, F1 -> F2, row j 7 westward from x 15 against the north wall: 12 risers, landing +5.70, 14 risers.
        kit.Shell(L2, S1);
        kit.Flight(new Vector3(15.0f, S1, 23.02f), Vector3.left, 1.8f, 12, .15f, .3f, stair, nose, false);
        kit.Slab(9.9f, 22.12f, 11.7f, 23.92f, 5.7f, stair, .2f);
        kit.Flight(new Vector3(9.9f, 5.7f, 23.02f), Vector3.left, 1.8f, 14, .15f, .3f, stair, nose, false);
        SlopedGlass(new Vector3(15.0f, S1 + .15f, 22.08f), new Vector3(11.7f, 5.7f, 22.08f));
        kit.Guard(new Vector3(9.9f, 0f, 22.08f), new Vector3(11.7f, 0f, 22.08f), 5.7f, FrontRoomsStructuresKit.GuardStyle.Glass);
        SlopedGlass(new Vector3(9.9f, 5.85f, 22.08f), new Vector3(6.0f, S2, 22.08f));
        // A spot over each landing and mid landing (R8); the hall's own troffers are the F0 plate's at +10.7.
        foreach (var l in new[] { new Vector3(1.5f, 2.6f, 19.5f), new Vector3(7.2f, 4.4f, 19.0f), new Vector3(15f, 6.6f, 21f), new Vector3(10.8f, 8.3f, 23f), new Vector3(1.5f, 10.4f, 22f) })
            kit.Lamp("slot landing", LightType.Spot, l, Vector3.down, 120f, 80f, 2.4f, 7f, new Color(.96f, .97f, 1f), true, new Vector3(.3f, .3f, .08f), metal, new Color(1.5f, 1.5f, 1.5f));
    }

    // ---------------------------------------------------------------- the B3 core (x 18..24, z 18..21): the second way

    void BuildCore()
    {
        // Per storey two flights of 13 risers (3.6 m going), 1.3 m wide, landings at both ends, the half landing at +1.95;
        // an opening 1.0 x 2.1 at x 18.1..19.1 onto the north band on every floor. Office finish, steel band and glass none
        // needed: the scissor wall closes the flights.
        kit.Shell(L2, 0f);
        var doors = new List<FrontRoomsStructuresKit.Opening>();
        foreach (var l in new[] { 0f, S1, S2 }) doors.Add(new FrontRoomsStructuresKit.Opening(18.1f, 19.1f, l, l + 2.1f));
        kit.WallX(18f, 18f, 24f, 0f, L2 + .16f, wall, doors);
        kit.WallX(21f, 18f, 24f, 0f, L2 + .16f, wall);
        kit.WallZ(18f, 18f, 21f, 0f, L2 + .16f, wall);
        kit.WallZ(24f, 18f, 21f, 0f, L2 + .16f, wall);
        kit.Slab(18f, 18f, 24f, 21f, 0f, carpet, .05f);
        kit.Lid(18f, 18f, 24f, 21f, L2, ceiling);
        var tread = concrete;
        var nose = FrontRoomsPoolMaterials.Surface("Stair_Band", new Color(.3f, .3f, .3f));
        foreach (var lo in new[] { 0f, S1 })
        {
            var hi = lo + S1;
            kit.Flight(new Vector3(19.2f, lo, 18.73f), Vector3.right, 1.3f, 13, .15f, .3f, tread, nose, false);
            kit.Slab(22.8f, 18.08f, 23.92f, 20.92f, lo + 1.95f, tread, .2f);
            kit.Flight(new Vector3(22.8f, lo + 1.95f, 20.27f), Vector3.left, 1.3f, 13, .15f, .3f, tread, nose, false);
            // The top storey (lo = S1) has nothing above flight 1: the wall rises a guard past the landing and a rail closes its open edge.
            var topStorey = lo == S1;
            kit.Prism(new Vector3(19.2f, 0f, 19.5f), new Vector3(22.8f, 0f, 19.5f), lo, topStorey ? hi + FrontRoomsStructuresKit.GuardHeight : hi - .2f, .1f, wall, true);
            kit.Slab(18.08f, 18.08f, 19.2f, 20.92f, hi, tread, .2f);
            if (topStorey) kit.Guard(new Vector3(19.2f, 0f, 18.08f), new Vector3(19.2f, 0f, 19.45f), hi, FrontRoomsStructuresKit.GuardStyle.Rail);
        }
        foreach (var l in new[] { 0f, S1, S2 })
        {
            kit.Lamp("core landing", LightType.Spot, new Vector3(18.6f, l + 2.6f, 19.5f), Vector3.down, 140f, 100f, 1.6f, 5f, new Color(.96f, .97f, 1f), false, new Vector3(.4f, .4f, .08f), metal, new Color(1.5f, 1.5f, 1.5f));
            if (l < S2) kit.Lamp("core half landing", LightType.Spot, new Vector3(23.4f, l + 1.95f + 2.4f, 19.5f), Vector3.down, 140f, 100f, 1.4f, 5f, new Color(.96f, .97f, 1f), false, new Vector3(.4f, .4f, .08f), metal, new Color(1.5f, 1.5f, 1.5f));
        }
    }

    // ---------------------------------------------------------------- signs (70 section 6, verbatim) and marks

    void BuildSigns()
    {
        kit.Sign(new Vector3(7.2f, 1.5f, 2.0f), Arrival + Vector3.up * 1.5f, "SP4 · 办公层：三层中庭，底下是下沉工位区",
            "办公层，每层 3.9 米。中庭穿过三层，玻璃栏板，顶上一整块发光的采光天花；中庭的地面就是下沉 1.2 米的工位区，过道交叉口站着一个 Relay 替身（2.05 米，不会动）。每层朝中庭的开口都不一样：一楼三边是走道，东边是抬高 0.30 米的机房地板；二楼西、北两边是走廊，东边是玻璃会议室；三楼只有北、东两边开着，东边是一片独立隔板。北边一条 6 米宽的窄长楼梯厅，两段直跑楼梯来回交叉，连通三层；楼梯厅东头还有一部普通楼梯。全区没有螺旋楼梯，每条路 Relay 都能走。游戏里一次只加载一层，这里三层都是真的。\n"
            + "灰盒占位：只有隔板，没有桌椅；1.35 和 2.16 米的隔板和三楼的独立隔板是同色的灰盒板；玻璃、采光天花、楼梯都是灰盒。黑色人形是 Relay 的静态替身（2.05 米），只用来检查视线，不会动。\n"
            + "要你决定：1 中庭放进办公层吗？ 2 中庭底下直接做下沉工位区，这样组合可以吗？还是另做一个顶棚不动的下沉区？ 3 窄长楼梯厅放进办公层吗？", 0f);
        kit.Sign(new Vector3(.14f, 1.5f, 10.5f), new Vector3(1.5f, 1.5f, 10.5f), "下沉工位区（低 1.2 米）",
            "四片隔板，西南 1.35、东南 1.52、东北 1.65、西北 2.16 米。在走道上看得进每一片；下去绕一圈，隔着每一片看交叉口的替身。要你决定：四档隔板各成一片（1.35 看得远、1.65 能藏、2.16 像迷宫、1.52 夹在中间）：哪几档放进办公层？（10 OF-1）", 0f);
        kit.Sign(new Vector3(18.6f, 1.5f, .14f), new Vector3(18.6f, 1.5f, 1.5f), "抬高地板（+0.30）和缓坡",
            "机房地板，矮隔板 1.35 米，从这里看过整个工位区。三条路上来：北边一级台阶、东墙边 1:12 坡道、南边 9 米长 1:30 的缓坡；走缓坡感觉不到坡，只有天花慢慢压低（2.9 → 2.6 米）。要你决定：抬高地板放进办公层吗？这种缓坡放进办公层吗？", 0f);
        kit.Sign(new Vector3(21.2f, S1 + 1.5f, 17.86f), new Vector3(21.2f, S1 + 1.5f, 16.5f), "玻璃会议室（二楼）",
            "玻璃正面朝中庭（窗台 1.07 米，从三楼看得见里面的台阶），里面两级（+0.30、+0.60），天花不动，最里面一级净高只剩 2.3 米；家具还没有。要你决定：放进办公层吗？", S1);
        kit.Sign(new Vector3(23.86f, S2 + 1.5f, 16.5f), new Vector3(22.5f, S2 + 1.5f, 16.5f), "独立隔板阵（三楼东边）",
            "一片片独立的隔板，三分之二朝同一个方向：从北边走廊看是一堵墙，从中庭对面看是一排排通道。要你决定：放进办公层吗？", S2);
        kit.Sign(new Vector3(.14f, 1.5f, 21.6f), new Vector3(1.5f, 1.5f, 21.6f), "窄长楼梯厅",
            "6 × 18 米、三层高；一段往东上二楼，一段往西上三楼，平台在两头轮流，两段在头顶交叉；Relay 每段都能走，第二条路是楼梯厅东头的普通楼梯。要你决定：放进办公层吗？", 0f);
        kit.Mark(new Vector3(4.5f, 1.25f, 3.1f), Vector3.back, "坑底 −1.20 · 到采光天花 11.9 米");
        kit.Mark(new Vector3(19.0f, 1.2f, 2.9f), Vector3.left, "缓坡 1:30 · 9 米 · +0.30");
        kit.Mark(new Vector3(17.9f, 1.4f, 9f), Vector3.left, "抬高 +0.30 · 净高 2.60");
    }

    // ---------------------------------------------------------------- the frame

    public override FrontRoomsStructuresLook.Preset PresetAt(Vector3 world) => FrontRoomsStructuresLook.Preset.Office;

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
        if (host == null || host.Walker == null) return "";
        var y = host.Walker.transform.position.y - Site.y;
        return y > S2 - .3f ? "现在在三楼" : y > S1 - .3f ? "现在在二楼" : y < -.3f ? "现在在坑底" : "现在在一楼";
    }

    public override void Guide(FrontRoomsStructures host, StringBuilder sb)
    {
        sb.AppendLine(Title);
        sb.AppendLine("三层的办公中庭（每层 3.9 米），中庭地面是下沉 1.2 米的工位区；周围是抬高地板和缓坡、二楼的玻璃会议室、三楼的独立隔板阵、北边的窄长楼梯厅和东头的普通楼梯。每处有自己的牌子。");
        sb.AppendLine();
        sb.AppendLine("按键：[ ] 换站点（到达点、坑底、抬高地板、缓坡、会议室、二楼画廊、三楼北走廊、楼梯厅）；R 回到当前站点；C Relay 体型（这里每条路 Relay 都能走）；M 尺寸。");
        sb.AppendLine();
        sb.AppendLine("要你决定（在对话里按编号告诉总控）：");
        sb.AppendLine("SP4-1 中庭放进办公层吗？");
        sb.AppendLine("SP4-2 中庭底下直接做下沉工位区，这样组合可以吗？还是另做一个顶棚不动的下沉区？");
        sb.AppendLine("SP4-3 窄长楼梯厅放进办公层吗？");
        sb.AppendLine("SP4-4 四档隔板（1.35、1.52、1.65、2.16）哪几档放进办公层？（10 OF-1）");
        sb.AppendLine("SP4-5 抬高地板放进办公层吗？缓坡放进办公层吗？");
        sb.AppendLine("SP4-6 玻璃会议室放进办公层吗？");
        sb.AppendLine("SP4-7 独立隔板阵放进办公层吗？");
        sb.AppendLine();
        sb.AppendLine("灰盒占位：只有隔板，没有桌椅；1.35 和 2.16 米的隔板和三楼的独立隔板是同色的灰盒板；玻璃、采光天花、楼梯都是灰盒。黑色人形是 Relay 的静态替身（2.05 米），只用来检查视线，不会动。");
    }
}
