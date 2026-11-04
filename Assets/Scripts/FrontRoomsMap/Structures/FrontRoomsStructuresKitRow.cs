using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// The kit row (key 8) of scene 4 (RULES 5b; PLAN 4 step 13, "may"; research/level_space/40 sections 11.4 and 11.8 item 1,
/// the kit sheet, walkable): 40 section 11.4's edge pieces as the kit's own Plate resolves them from two cells and their
/// height difference, never placed by hand. Two closed Tall halls (5.4 m), one in Level 0's own finish (wallpaper, carpet,
/// the 0.6 m ceiling grid, L03 troffers at x1.6) and one in the pool tile (porcelain white LS_PoolTile150, a dark nosing on
/// every step, metal-halide wall boxes and down-fills): along each hall's north side a row of plinths one to six level
/// steps high (0.30 to 1.80 m), each crossed on part of its front edge (a STEP or STEP RUN the plate builds) and closed on
/// the rest (KERB, LEDGE, and the 1.07 m guard where the drop passes 0.76 m); at six steps nothing crosses at the edge (a
/// connector module in its own cells, R2); at the east end a void cell through the storey, its edges guarded with the
/// floor's section showing and a patch of the storey below. The arrival cell carries the units: a 0.6 m fine-module grid on
/// its floor and a 0.30 m step gauge to one storey. Each plinth has a small label; the other pieces stand in the districts
/// that use them (the sign says where). No Relay stand-in; no spiral here.
/// </summary>
public sealed class FrontRoomsStructuresKitRow : FrontRoomsStructuresDistrict
{
    const float Tall = 5.4f, Step = .30f, Hall2Z = 15f;
    const int Width = 14, Rows = 3;

    FrontRoomsStructuresKit kit;

    public FrontRoomsStructuresKitRow(Vector3 site)
        : base(8, "KIT", "KIT · 模组件：每种高差的边怎么接", "高 1 到 6 级的台子一字排开，办公 / Level 0 版和瓷砖版各一套。", site) { }

    public override void Build(FrontRoomsStructures host)
    {
        kit = new FrontRoomsStructuresKit(host, this);
        var paper = FrontRoomsPoolMaterials.Surface("L0_Wallpaper", new Color(.80f, .74f, .48f));
        var carpet = FrontRoomsPoolMaterials.Surface("L0_Carpet", new Color(.55f, .49f, .30f));
        var tiles = FrontRoomsPoolMaterials.Surface("L0_Ceiling", new Color(.86f, .83f, .70f));
        var oak = FrontRoomsPoolMaterials.Surface("Prop_WoodOak", new Color(.55f, .40f, .25f));
        var concrete = FrontRoomsPoolMaterials.Surface("Stair_Slab", new Color(.6f, .6f, .58f));
        var tile = FrontRoomsPoolMaterials.Tile;
        var nosing = FrontRoomsPoolMaterials.Nosing;

        kit.Safe("Level 0 hall", () =>
        {
            var a = new FrontRoomsStructuresKit.Plate(0f, 0f, 0f, Width, Rows)
            {
                WallMat = paper, FloorMat = carpet, FloorThick = .05f, CeilingMat = tiles, LedgeMat = paper, TreadMat = carpet,
                Troffers = true, LampScale = 1.6f, LampRange = 12f, ViewLamps = true, RunCheeks = true,
                VoidGuard = FrontRoomsStructuresKit.GuardStyle.Rail, LedgeGuard = FrontRoomsStructuresKit.GuardStyle.HalfWall,
                GuardWall = paper, GuardCap = oak, SectionMat = concrete, SectionDepth = 1.0f,
            };
            Ladder(a);
            a.Build(kit);
            Below(0f, 3.9f, a.SectionDepth, paper, carpet, tiles);
            Units(0f, carpet);
        });
        kit.Safe("tile hall", () =>
        {
            var b = new FrontRoomsStructuresKit.Plate(0f, Hall2Z, 0f, Width, Rows)
            {
                WallMat = tile, FloorMat = tile, FloorThick = .2f, CeilingMat = tile, LedgeMat = tile, TreadMat = tile, NosingMat = nosing,
                Troffers = false, RunCheeks = true,
                VoidGuard = FrontRoomsStructuresKit.GuardStyle.Glass, LedgeGuard = FrontRoomsStructuresKit.GuardStyle.TileWall,
                GuardWall = tile, GuardCap = tile, SectionMat = concrete, SectionDepth = 1.2f,
            };
            Ladder(b);
            b.Build(kit);
            Below(Hall2Z, FrontRoomsStructuresKit.PoolStorey, b.SectionDepth, tile, tile, tile);
            // Pool hall light (70 section 2): metal-halide wall boxes throwing up on both long walls every 6 m, about 3.4 m up
            // (70 section 2), a down-fill grid; view lamps, so the hall's far half is lit from the arrival (review KIT-F6).
            for (var x = 3f; x < Width * 3f; x += 6f)
            {
                kit.WallBox(new Vector3(x, 3.4f, Hall2Z + .08f), Vector3.forward, 6f, 10f, true);
                kit.WallBox(new Vector3(x + 3f, 3.4f, Hall2Z + 9f - .08f), Vector3.back, 6f, 10f, true);
            }
            for (var x = 6f; x < Width * 3f; x += 9f) kit.DownFill(new Vector3(x, Tall - .07f, Hall2Z + 3.5f), 2.2f, 11f, true);
            Units(Hall2Z, tile);
            // The tile hall's own box probe, as SP1's and SP2's halls (the Pool preset expects one; review KIT-F15); its
            // lamps are view lamps, so they are on when it renders.
            try { host.Look.Probe(Root, new Bounds(Site + new Vector3(Width * 1.5f, Tall * .5f, Hall2Z + 4.5f), new Vector3(Width * 3f, Tall, 9f)), 1024); }
            catch (System.Exception e) { FrontRoomsPoolMaterials.LogOnce("KIT probe", "KIT probe: " + e.Message); }
        });
        kit.Finish();
        kit.Safe("labels", Labels);
        // 70 section 1's spot: 1.5 m right of the main view's axis, 2 m ahead of the arrival, facing it.
        kit.Safe("sign", () => kit.Sign(new Vector3(3.74f, 1.5f, 1.1f), new Vector3(1.5f, 1.5f, 2.2f), "KIT · 模组件：每种高差的边怎么接",
            "40 号文档 §11.4 的边件：两格之间的高差按 0.30 米一级算，边由两边的格子自动决定，不用手放。北边一排是高 1 到 6 级的台子，每个台子前沿左边一段做成台阶（跨过），其余不跨（路缘、台沿，高差超过 0.76 米加 1.07 米护栏）；6 级以上边上不放台阶，要用独立的楼梯模块。最东边是一个穿透楼层的空洞。办公 / Level 0 一套，瓷砖（泳池层）一套，按 ] 换过去。其他件在各区里看：拱墙、穿墙口、滑道段和塔在 SP1；B4-L 和竖井在 SP2；B4-S 和楼板开口在 SP3；浅滩、溢流台、下水台阶、压顶和 B4-M 在 SP6。\n"
            + "灰盒占位：护栏、压顶、截面、地面网格线和刻度杆是场景 4 的简单几何，不是正式模型；墙、地、灯和瓷砖都是游戏自己的。\n"
            + "要你决定：1 高差按 0.30 米一级、一格里最多五级台阶，作为关卡生成器的规则可以吗？ 2 高差超过 0.76 米才加护栏，低于这个只做台沿，可以吗？ 3 6 级（1.8 米）以上必须用独立的楼梯模块，可以吗？", 0f));

        Box = new Bounds(Site + new Vector3(Width * 1.5f, 1f, 12f), new Vector3(Width * 3f + 4f, 16f, 28f));
        LowestFloor = Site.y - FrontRoomsStructuresKit.PoolStorey;
        Far = 80f;
        Stops.Clear();
        kit.Stop("到达点：Level 0 版，从西头看过去", new Vector3(1.5f, 0f, 2.2f), new Vector3(30f, 1.2f, 7.5f));
        kit.Stop("Level 0 版：台子正面", new Vector3(16.5f, 0f, 1.2f), new Vector3(16.5f, 1.2f, 7.5f));
        kit.Stop("Level 0 版：空洞边", new Vector3(37.5f, 0f, 2.35f), new Vector3(37.5f, -3.9f, 4.5f), 60f);
        kit.Stop("瓷砖版，从西头看过去", new Vector3(1.5f, 0f, Hall2Z + 2.2f), new Vector3(30f, 1.2f, Hall2Z + 7.5f));
        kit.Stop("瓷砖版：台子正面", new Vector3(16.5f, 0f, Hall2Z + 1.2f), new Vector3(16.5f, 1.2f, Hall2Z + 7.5f));
        kit.Stop("瓷砖版：空洞边", new Vector3(37.5f, 0f, Hall2Z + 2.35f), new Vector3(37.5f, -4.5f, Hall2Z + 4.5f), 60f);
    }

    // One hall's cells: rows 0-1 the walk at +-0, row 2 the plinths at odd columns one to six steps high (the even columns at
    // +-0 between them), each front edge crossed from 0.3 to 1.5 m (no crossing at six steps); the void at column 12, row 1.
    // Plinth 6's way on (R2, KIT-3; review KIT-F4, Red 11-04 "所有结构都能够正常让玩家通行"): nothing crosses its own edge; a
    // connector of its own in column 10 (a straight flight of 12 risers up the south edge of cell (10, 2), its foot in rows 0-1)
    // reaches a landing at +1.80 beside it, and a STEP from plinth 5 is the raised area's second way on (R6).
    static void Ladder(FrontRoomsStructuresKit.Plate p)
    {
        var floor = FrontRoomsStructuresKit.Plate.Kind.Floor;
        p.Fill(0, 0, Width - 1, Rows - 1, floor, 0f, Tall, 1);
        for (var k = 1; k <= 6; k++)
        {
            var i = 2 * k - 1;
            p.Fill(i, 2, i, 2, floor, Step * k, Tall, 1);
            if (k <= 5) p.Run(i, 2, 'S', .3f, 1.5f);
        }
        p.Fill(10, 2, 10, 2, floor, Step * 6, Tall, 1);
        p.Run(10, 2, 'S', .3f, 1.5f);
        p.Run(10, 2, 'W', 1.5f, 2.7f);
        p.Fill(12, 1, 12, 1, FrontRoomsStructuresKit.Plate.Kind.Void, 0f, Tall, 1);
    }

    // Under the void (column 12, row 1): the storey below as a room of 3 x 3 cells round the void's column (review KIT-F5, as
    // SP3's V1: its floor runs on past the cut's edges), its lid at the section's foot (open under the void), its own lamps;
    // and the hall's ceiling carried on over the void cell.
    void Below(float z0, float storey, float sectionDepth, Material wall, Material floorMat, Material ceiling)
    {
        float x0 = 33f, x1 = 42f, za = z0, zb = z0 + 9f, vx0 = 36f, vx1 = 39f, vz0 = z0 + 3f, vz1 = z0 + 6f;
        var lid = -sectionDepth - .002f;
        kit.Shell(lid, -storey);
        kit.Slab(x0, za, x1, zb, -storey, floorMat, .2f);
        kit.WallX(za, x0 - .08f, x1 + .08f, -storey, lid + .16f, wall);
        kit.WallX(zb, x0 - .08f, x1 + .08f, -storey, lid + .16f, wall);
        kit.WallZ(x0, za, zb, -storey, lid + .16f, wall);
        kit.WallZ(x1, za, zb, -storey, lid + .16f, wall);
        // The lid round the void, its opening 2 mm wider than the void so the slab's section stays in front at the cut.
        kit.Lid(x0, za, x1, vz0 - .002f, lid, ceiling);
        kit.Lid(x0, vz1 + .002f, x1, zb, lid, ceiling);
        kit.Lid(x0, vz0 - .002f, vx0 - .002f, vz1 + .002f, lid, ceiling);
        kit.Lid(vx1 + .002f, vz0 - .002f, x1, vz1 + .002f, lid, ceiling);
        var tileHall = z0 > 1f;
        for (var i = 0; i < 3; i++)
            for (var j = 0; j < 3; j++)
            {
                if (i == 1 && j == 1) continue;
                var c = new Vector3(x0 + 3f * i + 1.5f, lid, za + 3f * j + 1.5f);
                if (tileHall) { if ((i + j) % 2 == 0) kit.DownFill(c + Vector3.down * .07f, 2.2f, 8f, false); }
                else kit.Troffer(c + new Vector3(0f, 0f, FrontRooms.Map.ModuleUnits.TrofferOffsetZ), false);
            }
        // The hall's own ceiling over the void cell (a void through the storey below, not a hole in the roof).
        kit.Shell(Tall, 0f);
        kit.Lid(vx0, vz0, vx1, vz1, Tall, ceiling);
        if (tileHall) kit.DownFill(new Vector3(37.5f, Tall - .07f, z0 + 4.5f), 2.2f, 11f, true);
        else kit.Troffer(new Vector3(37.5f, Tall, z0 + 4.5f + FrontRooms.Map.ModuleUnits.TrofferOffsetZ), true, false, 1.6f, 12f);
    }

    // The units on the arrival cell (0, 0): the 0.6 m fine module drawn on the floor, a 0.30 m step gauge to one storey in the
    // cell's north-east corner (alternate steps dark, every storey's top marked).
    void Units(float z0, Material floorMat)
    {
        var ink = FrontRoomsPoolMaterials.Lit("kit row grid ink", new Color(.12f, .12f, .12f), .2f);
        var light = FrontRoomsPoolMaterials.Lit("kit row gauge light", new Color(.86f, .85f, .82f), .3f);
        var accent = FrontRoomsPoolMaterials.Glow("kit row gauge accent", new Color(.957f, .875f, .231f));
        for (var k = 1; k < 5; k++)
        {
            var f = .6f * k;
            kit.Box(f - .006f, .0005f, z0, f + .006f, .0035f, z0 + 3f, ink, false);
            kit.Box(0f, .0005f, z0 + f - .006f, 3f, .0035f, z0 + f + .006f, ink, false);
        }
        var storeys = Mathf.RoundToInt((z0 > 1f ? FrontRoomsStructuresKit.PoolStorey : FrontRoomsStructuresKit.OfficeStorey) / Step);
        // The gauge in the cell's north-west corner, behind-left of the arrival, out of the main view (review KIT-F2).
        for (var s = 0; s < storeys; s++)
            kit.Box(.10f, s * Step, z0 + 2.82f, .18f, (s + 1) * Step, z0 + 2.9f, s % 2 == 0 ? ink : light, false);
        kit.Box(.08f, storeys * Step - .01f, z0 + 2.80f, .20f, storeys * Step + .01f, z0 + 2.92f, accent, false);
    }

    // A small label (outside the fiction) on a post in front of each plinth, in the placard's colours.
    void Labels()
    {
        string[] lines =
        {
            "1 级 · +0.30\n跨过：STEP，两级 0.15，一个 0.6 米细模数\n不跨：KERB 路缘",
            "2 级 · +0.60\n跨过：STEP RUN 四级，开口一侧有侧板\n不跨：LEDGE 台沿和压顶",
            "3 级 · +0.90\n跨过：STEP RUN 六级\n不跨：LEDGE，超过 0.76 米加 1.07 米护栏",
            "4 级 · +1.20\n跨过：STEP RUN 八级\n不跨：LEDGE 加护栏",
            "5 级 · +1.50\n跨过：STEP RUN 十级（一格最多五个细模数）\n不跨：LEDGE 加护栏",
            "6 级 · +1.80\n边上不放台阶：用独立的楼梯模块（R2）\n边：LEDGE 加 1.07 米护栏（夹层、走廊边）",
        };
        foreach (var z0 in new[] { 0f, Hall2Z })
        {
            for (var k = 0; k < lines.Length; k++)
            {
                var x = 3f * (2 * k + 1) + 2.4f;
                Label(new Vector3(x, 0f, z0 + 5.2f), lines[k]);
            }
            Label(new Vector3(38.4f, 0f, z0 + 2.2f), "空洞 · VOID EDGE\n楼板边露出截面，1.07 米护栏\n视线和声音能过，Relay 不能");
        }
    }

    void Label(Vector3 floor, string text)
    {
        if (Root == null) return;
        var go = new GameObject("Kit label");
        go.transform.SetParent(Root, false);
        go.transform.localPosition = floor + new Vector3(0f, 1.0f, 0f);
        go.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);
        var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        panel.name = "Panel";
        Object.Destroy(panel.GetComponent<Collider>());
        panel.transform.SetParent(go.transform, false);
        panel.transform.localPosition = new Vector3(0f, 0f, .01f);
        panel.transform.localScale = new Vector3(.6f, .34f, .015f);
        panel.GetComponent<Renderer>().sharedMaterial = FrontRoomsPoolMaterials.Lit("placard panel E", FrontRoomsStructuresSigns.PanelColor, .1f);
        var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
        post.name = "Post";
        Object.Destroy(post.GetComponent<Collider>());
        post.transform.SetParent(Root, false);
        post.transform.localPosition = floor + new Vector3(0f, .42f, .03f);
        post.transform.localScale = new Vector3(.04f, .84f, .04f);
        post.GetComponent<Renderer>().sharedMaterial = FrontRoomsPoolMaterials.Lit("placard post", new Color(.2f, .2f, .2f), .3f, .6f);
        var t = new GameObject("Text");
        t.transform.SetParent(go.transform, false);
        t.transform.localPosition = new Vector3(-.27f, .135f, -.002f);
        var tm = t.AddComponent<TextMesh>();
        var font = FrontRoomsStructuresSigns.BodyFontAsset;
        if (font != null) tm.font = font;
        tm.fontSize = 64;
        tm.characterSize = .0185f * 10f / 64f;
        tm.lineSpacing = 1.25f;
        tm.anchor = TextAnchor.UpperLeft;
        tm.alignment = TextAlignment.Left;
        tm.color = FrontRoomsStructuresSigns.TextColor;
        tm.text = FrontRoomsStructuresSigns.Wrap(text, 30);
        var r = t.GetComponent<MeshRenderer>();
        if (r != null)
        {
            var depth = FrontRoomsStructuresSigns.DepthText(font);
            if (depth != null) r.sharedMaterial = depth;
            r.shadowCastingMode = ShadowCastingMode.Off;
        }
    }

    public override FrontRoomsStructuresLook.Preset PresetAt(Vector3 world)
    {
        var z = world.z - Site.z;
        return z < (Hall2Z + 9f) * .5f ? FrontRoomsStructuresLook.Preset.Level0Tall : FrontRoomsStructuresLook.Preset.Pool;
    }

    public override void ShowMetrics(FrontRoomsStructures host, bool on)
    {
        if (kit != null) kit.ShowMarks(on);
    }

    public override string Status(FrontRoomsStructures host) => "[ ] 在 Level 0 版和瓷砖版之间换";

    public override void Guide(FrontRoomsStructures host, StringBuilder sb)
    {
        sb.AppendLine(Title);
        sb.AppendLine("40 号文档 §11.4 的边件，由两格的高差自动生成：北边一排是高 1 到 6 级（0.30 到 1.80 米）的台子，前沿左边一段跨过（台阶），其余不跨（路缘、台沿、护栏）。最东边是穿透楼层的空洞。到达格的地面画着 0.6 米细模数，角上的刻度杆每 0.30 米一段，到一层楼高。");
        sb.AppendLine();
        sb.AppendLine("按键：[ ] 换站点（Level 0 版、瓷砖版、空洞边）；R 回到站点；M 尺寸；C Relay 体型。");
        sb.AppendLine();
        sb.AppendLine("要你决定（在对话里按编号告诉总控）：");
        sb.AppendLine("KIT-1 高差按 0.30 米一级、一格里最多五级台阶，作为关卡生成器的规则可以吗？");
        sb.AppendLine("KIT-2 高差超过 0.76 米才加护栏，低于这个只做台沿，可以吗？");
        sb.AppendLine("KIT-3 6 级（1.8 米）以上必须用独立的楼梯模块，可以吗？");
        sb.AppendLine();
        sb.AppendLine("其他件在各区里看：拱墙、穿墙口、滑道段和塔在 SP1；B4-L 和竖井在 SP2；B4-S 和楼板开口在 SP3；浅滩、溢流台、下水台阶、压顶和 B4-M 在 SP6。");
        sb.AppendLine();
        sb.AppendLine("灰盒占位：护栏、压顶、截面、地面网格线和刻度杆是场景 4 的简单几何，不是正式模型；墙、地、灯和瓷砖都是游戏自己的。");
    }
}
