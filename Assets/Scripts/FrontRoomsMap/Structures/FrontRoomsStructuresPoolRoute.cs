using System;
using System.Collections.Generic;
using System.Text;
using FrontRooms.Map;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// District P (key 7) of scene 4 (RULES 5b; structures PLAN 2.7, 3): the pool family's route built by the game's own map
/// code, with the pool switched on in this scene's profile copy alone. As the Level Designer's preview builds a station
/// floor: a seed search on scratch caches (start cell (4, 4), door (4, 5), LEVEL B2 = floor 3), then an embedded map world
/// at the origin on that seed, NextFloor, zone keys off, the pool plan (FrontRoomsPoolPlanner) on the world's own cache,
/// SetPool, SetExitArea, the EXIT stairwell, Begin round an anchor. The anchor follows the walker while P is current and
/// stays where it was when another district is, so the map never streams into the hub or a district. V rebuilds with the
/// other variant (A: PL_Leisure + PL_Court; B: PL_PoMo + PL_Weird), so all fifteen modules are walked between the two.
/// [ ] step through the plan's 17 focus points. Not built here: the player's water moves (P2) and the Relay (P3).
/// </summary>
public sealed class FrontRoomsStructuresPoolRoute : FrontRoomsStructuresDistrict
{
    const int SeedTries = 24;
    static readonly GridCoord ArrivalCell = new GridCoord(4, 4);
    static GridCoord ArrivalDoor => new GridCoord(ArrivalCell.x, ArrivalCell.y + 1);

    static readonly string[] StopNames =
    {
        "入口走廊", "前台", "更衣室", "泳池大厅（西南角）", "远处的圆拱门", "转角", "L0 大厅", "M0", "画廊", "L1 大厅",
        "池厅 M1", "EXIT 门前", "防火楼梯顶", "防火楼梯底", "暗区：铁门后", "暗区：办公室", "暗区：池厅",
    };

    FrontRoomsMapWorld map;
    Transform anchor, gameRoot;
    FrontRoomsStairwell stairwell;
    FrontRoomsPoolPlan plan;
    int variant;
    int floorSeed = int.MinValue, searchedFrom;
    string error = "";
    readonly List<GameObject> made = new List<GameObject>();
    readonly List<PoolRoom> probeRooms = new List<PoolRoom>();
    readonly HashSet<PoolRoom> probed = new HashSet<PoolRoom>();
    FrontRoomsStructures hostRef;

    public FrontRoomsStructuresPoolRoute(Vector3 site)
        : base(7, "P", "P · 泳池俱乐部和泳池层（游戏自己的地图代码）", "从 B2 的走廊走进泳池俱乐部，再一路走到下面一层的暗区。", site) { }

    public override bool KeepActive => true;

    public override void Build(FrontRoomsStructures host)
    {
        hostRef = host;
        anchor = new GameObject("P · map anchor (follows the walker here, parks when you leave)").transform;
        anchor.SetParent(Root, false);
        Box = new Bounds(Site + new Vector3(0f, 0f, 60f), new Vector3(420f, 40f, 420f));
        LowestFloor = Site.y + PoolRules.DarkBasinFloor;
        BuildRoute(host, 0);
    }

    // ---------------------------------------------------------------- the route

    void BuildRoute(FrontRoomsStructures host, int which)
    {
        TearDown();
        variant = which;
        var profile = host.Profile;
        if (profile == null) throw new InvalidOperationException("no scene profile copy");
        var library = FrontRoomsPoolLibrary.FromCode();
        if (!library.Complete) throw new InvalidOperationException("pool modules missing: " + library.Missing);
        var modules = profile.ModuleData();
        var area = new CellRect(ArrivalCell.x, ArrivalCell.y, 1, 1);

        // The seed: the first from the profile's own whose block takes both variants (so V changes M0 and slot m only).
        if (floorSeed == int.MinValue)
        {
            searchedFrom = profile.generation != null ? profile.generation.seed : 20261001;
            for (var k = 0; k < SeedTries && floorSeed == int.MinValue; k++)
            {
                var s = unchecked(searchedFrom + k);
                var a = FrontRoomsPoolPlanner.Plan(FrontRoomsExitPlanner.CacheFor(profile.generation, modules, s, FrontRoomsPoolPlanner.PoolFloor), library, area, ArrivalDoor, s, FrontRoomsPoolPlanOptions.Variant(0));
                if (!a.Ready) { error = a.error; continue; }
                var b = FrontRoomsPoolPlanner.Plan(FrontRoomsExitPlanner.CacheFor(profile.generation, modules, s, FrontRoomsPoolPlanner.PoolFloor), library, area, ArrivalDoor, s, FrontRoomsPoolPlanOptions.Variant(1));
                if (!b.Ready) { error = b.error; continue; }
                floorSeed = s;
            }
            if (floorSeed == int.MinValue) throw new InvalidOperationException("no pool route in " + SeedTries + " seeds from " + searchedFrom + ": " + error);
        }

        map = FrontRoomsMapWorld.CreateEmbedded(Root, profile, floorSeed);
        map.gameObject.name = "P · map world (game code, pool on in this scene only)";
        map.NextFloor(floorSeed, FrontRoomsPoolPlanner.PoolFloor, new RectInt(ArrivalCell.x, ArrivalCell.y, 1, 1), ArrivalDoor);
        map.ZoneKeysOff = true;
        plan = FrontRoomsPoolPlanner.Plan(map.Cache, library, area, ArrivalDoor, floorSeed, FrontRoomsPoolPlanOptions.Variant(variant));
        if (!plan.Ready) throw new InvalidOperationException("the pool plan failed on the map's own cache: " + plan.error);
        if (!map.SetPool(plan)) throw new InvalidOperationException("the map refused the pool plan (poolFamily off in the profile copy?)");
        map.SetExitArea(plan.exit.exitCell);
        gameRoot = new GameObject("P · game objects (EXIT stairwell)").transform;
        gameRoot.SetParent(Root, false);
        try { stairwell = FrontRoomsStairwell.Create(map, plan.exit.exitCell, gameRoot); }
        catch (Exception e) { FrontRoomsPoolMaterials.LogOnce("stairwell", "the EXIT stairwell could not be made: " + e.Message); }

        // Stops: the plan's 17 focus points, in world space.
        Stops.Clear();
        for (var k = 0; k < plan.Focus.Count; k++)
        {
            var f = plan.Focus[k];
            var floor = map.transform.TransformPoint(new Vector3(f.x, f.y, f.z));
            var look = map.transform.TransformPoint(new Vector3(f.lookX, f.lookY, f.lookZ));
            var name = k < StopNames.Length ? StopNames[k] : f.name;
            Stops.Add(FrontRoomsStructuresStop.Facing(name, floor, look));
        }
        if (Stops.Count == 0) Stops.Add(new FrontRoomsStructuresStop("到达点", Site, 0f, 0f));
        StopIndex = Mathf.Clamp(StopIndex, 0, Stops.Count - 1);

        anchor.position = Stops[StopIndex].position;
        map.Begin(anchor, true);
        StartAreaWall();
        if (stairwell != null)
        {
            try { stairwell.SetVisible(map.IsChunkBuiltAt(plan.exit.exitCell)); }
            catch (Exception e) { FrontRoomsPoolMaterials.LogOnce("stairwell visible", "stairwell visibility: " + e.Message); }
        }

        // The halls get a box probe each, rendered once when their chunk first stands (FrontRoomsStructuresLook).
        probeRooms.Clear();
        probed.Clear();
        foreach (var r in plan.Rooms)
            if (r.module != null && r.width * r.depth >= 8) probeRooms.Add(r);

        MakeSigns(host);
        Debug.Log(FrontRoomsStructures.Log + "P built: " + plan.Describe().Replace("\n", " | "));
    }

    // The floor's start area (4, 4) is the last floor's stairwell in the game; the map leaves it empty and open to its door
    // cell. Here no stairwell stands there, so a plain wall in the map's Level 0 paper closes that side (collider included).
    void StartAreaWall()
    {
        if (map == null) return;
        var cs = MapGrid.CellSize;
        var size = new Vector3(cs + ModuleUnits.WallThickness, ModuleUnits.StandardCeiling, ModuleUnits.WallThickness);
        var go = new GameObject("STAND-IN start stairwell end wall (the game's stairwell stands here)");
        go.transform.SetParent(map.transform, false);
        go.transform.localPosition = new Vector3((ArrivalCell.x + .5f) * cs, ModuleUnits.StandardCeiling * .5f, ArrivalDoor.y * cs - ModuleUnits.WallThickness * .5f);
        // Metre UVs (the map's paper is drawn at 1.2 m a repeat: near enough for a stand-in).
        go.AddComponent<MeshFilter>().sharedMesh = FrontRoomsFilmMesh.GetPlanarBox(size);
        var r = go.AddComponent<MeshRenderer>();
        var paper = FrontRoomsSurfaces.Room(RoomRule.Lobby, FrontRoomsSurfaces.Slot.Wall);
        r.sharedMaterial = paper != null ? paper : FrontRoomsPoolMaterials.Lit("stand-in wall", new Color(.80f, .74f, .48f), .06f);
        go.AddComponent<BoxCollider>().size = size;
        made.Add(go);
    }

    void TearDown()
    {
        foreach (var go in made) if (go != null) UnityEngine.Object.Destroy(go);
        made.Clear();
        if (map != null)
        {
            // Colliders and lamps go at once; the objects at the end of the frame (the map frees its meshes and materials).
            map.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(map.gameObject);
            map = null;
        }
        if (gameRoot != null)
        {
            gameRoot.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(gameRoot.gameObject);
            gameRoot = null;
        }
        stairwell = null;
        plan = null;
    }

    // ---------------------------------------------------------------- signs (outside the fiction; RULES 9: type is a placeholder)

    void MakeSigns(FrontRoomsStructures host)
    {
        Sign(0, "P · 泳池路线（游戏自己的地图代码）",
            "这是什么：B2 这一层的一段路。从普通走廊进门，经过前台、更衣室，到大泳池厅；穿过远处的圆拱是泳池层（Z0 到 Z3 四个区）；"
            + "最后从防火楼梯往下走进暗区。全部由游戏自己的地图代码生成，和游戏里一样走。\n"
            + "放在哪：游戏的 B2（第 3 层）。你说可以，才会在游戏里打开开关。\n"
            + "要你决定：P-1 到 P-6，按 G 看。\n"
            + "灰盒占位：前台、储物柜、淋浴、梯子、救生椅、标牌、滑梯管等都是灰色替身；水面和天花板上的光斑是临时效果；"
            + "水里还不能游（P2），这里也没有 Relay（P3）。");
        Sign(4, "远处的门 · 进入泳池层",
            "这是什么：泳池大厅北墙的圆拱（M5），后面就是泳池层。泳池层由一个个房间模组拼成：L0、L1 两个大厅（办公室吊顶 M1、柱厅 M2），"
            + "M0（休闲池或后现代厅），画廊（M4），球场或怪房间（M6），池厅。\n"
            + "要你决定：P-1，这一层整体的感觉；P-6，配色。\n"
            + "灰盒占位：灯具和水面效果是临时的。");
        Sign(7, "M0 · 两种变体（按 V）",
            "按 V 切换：A = 休闲池（PL_Leisure）+ 球场（PL_Court）；B = 后现代厅（PL_PoMo）+ 怪房间（PL_Weird）。两种加起来，15 个房间模组都能走到。\n"
            + "要你决定：P-3 选哪一个；P-4 球场和怪房间多常出现。\n"
            + "灰盒占位：滑梯管、玻璃砖墙、圆窗是替身。");
        Sign(11, "EXIT · 这一层的出口",
            "这是什么：这一层的 EXIT 门和楼梯间，和游戏里一样由地图放在这里。在游戏里，进门就是去下一层。\n"
            + "要你决定：P-2，B2 要钥匙、楼层站，还是两个都要（现在都没有）。\n"
            + "灰盒占位：楼梯间里面是游戏现在的样子。");
        Sign(12, "防火楼梯 · 往下走进暗区",
            "这是什么：7 + 7 级台阶（每级 0.18 × 0.28 米）一直走进水里，铁门开着，下面是暗区：比泳池层低 2.52 米，水深约 1 米，"
            + "只有暖色的壁灯。你说过：“第一版就做成真正往下一层”。\n"
            + "要你决定：P-5，暗区这样做对吗。\n"
            + "灰盒占位：铁门、扶手、壁灯是替身。");
    }

    // A placard by a stop: ahead and to one side of the view, on a dry floor of the plan (else the other side, else nearer).
    void Sign(int stop, string title, string body)
    {
        if (stop < 0 || stop >= Stops.Count || map == null || plan == null) return;
        var s = Stops[stop];
        var fwd = Quaternion.Euler(0f, s.yaw, 0f) * Vector3.forward;
        var right = Vector3.Cross(Vector3.up, fwd);
        var tries = new[] { new Vector2(1.0f, 2.2f), new Vector2(-1.0f, 2.2f), new Vector2(.9f, 1.4f), new Vector2(-.9f, 1.4f), new Vector2(0f, 2.6f) };
        foreach (var t in tries)
        {
            var at = s.position + right * t.x + fwd * t.y;
            if (!DryFloor(at, out var floorY) || !DryFloor(at + right * .55f, out _) || !DryFloor(at - right * .55f, out _)) continue;
            var centre = new Vector3(at.x, floorY + 1.5f, at.z);
            var go = FrontRoomsStructuresSigns.Placard(Root, centre, s.position + Vector3.up * 1.5f, title, body, floorY);
            if (go != null) made.Add(go);
            return;
        }
        FrontRoomsPoolMaterials.LogOnce("sign " + stop, "no dry floor for the sign at stop " + stop + ": its text is on the G panel only.");
    }

    // A point over a room cell of the plan (not Void, not deep water), with its floor (world y).
    bool DryFloor(Vector3 world, out float floorY)
    {
        floorY = 0f;
        if (map == null || plan == null) return false;
        var local = map.transform.InverseTransformPoint(world);
        var cs = MapGrid.CellSize;
        var cell = new GridCoord(Mathf.FloorToInt(local.x / cs), Mathf.FloorToInt(local.z / cs));
        if (!plan.IsPlanCell(cell) || plan.RoomAt(cell) == null) return false;
        var c = plan.CellOf(cell);
        if (c.connector != CellConnector.None || c.water == CellWater.Wade) return false;
        floorY = map.transform.TransformPoint(new Vector3(local.x, plan.FloorAt(local.x, local.z), local.z)).y;
        // Not too near a cell edge (a wall may stand there).
        var u = local.x - cell.x * cs;
        var v = local.z - cell.y * cs;
        return u > .35f && u < cs - .35f && v > .35f && v < cs - .35f;
    }

    // ---------------------------------------------------------------- the frame, stops and keys

    public override void Enter(FrontRoomsStructures host) { }

    public override void Arrived(FrontRoomsStructures host, int stop)
    {
        if (map == null || anchor == null || stop < 0 || stop >= Stops.Count) return;
        // Build what stands round the stop now, before the walker's first step (a far stop's chunks are not built yet).
        anchor.position = Stops[stop].position;
        map.StreamForTools(anchor.position, 0f);
        if (stairwell != null && plan != null) stairwell.SetVisible(map.IsChunkBuiltAt(plan.exit.exitCell));
    }

    public override void Tick(FrontRoomsStructures host, float dt, bool isCurrent)
    {
        if (map == null) return;
        if (isCurrent && host.Walker != null && anchor != null) anchor.position = host.Walker.transform.position;
        FrontRoomsPoolMaterials.Animate(Time.time);
        if (stairwell != null && plan != null) stairwell.SetVisible(map.IsChunkBuiltAt(plan.exit.exitCell));
        // A hall's probe once its chunk stands.
        for (var k = 0; k < probeRooms.Count; k++)
        {
            var r = probeRooms[k];
            if (probed.Contains(r) || !map.IsChunkBuiltAt(r.origin)) continue;
            probed.Add(r);
            var cs = MapGrid.CellSize;
            var rel = MapGrid.CeilingHeight(r.zone.height);
            var centre = map.transform.TransformPoint(new Vector3(r.CentreX, r.BaseY + rel * .5f, r.CentreZ));
            var probe = host.Look.Probe(Root, new Bounds(centre, new Vector3(r.width * cs, rel, r.depth * cs)), 512);
            if (probe != null) made.Add(probe.gameObject);
        }
    }

    public override FrontRoomsStructuresLook.Preset PresetAt(Vector3 world)
    {
        if (map == null || map.Cache == null) return FrontRoomsStructuresLook.Preset.Level0;
        var theme = map.ZoneOf(map.CellOf(world)).theme;
        return theme == ZoneTheme.PoolClub ? FrontRoomsStructuresLook.Preset.PoolClub
            : theme == ZoneTheme.PoolDark ? FrontRoomsStructuresLook.Preset.PoolDark
            : theme == ZoneTheme.PoolLevel ? FrontRoomsStructuresLook.Preset.Pool
            : theme == ZoneTheme.Office ? FrontRoomsStructuresLook.Preset.Office
            : FrontRoomsStructuresLook.Preset.Level0;
    }

    public override bool KeyPressed(FrontRoomsStructures host, Key key, out string message)
    {
        message = null;
        // UnityEngine.InputSystem.Key in full: Key alone is the district's own number here.
        if (key != UnityEngine.InputSystem.Key.V) return false;
        var next = variant == 0 ? 1 : 0;
        var stop = StopIndex;
        try
        {
            BuildRoute(host, next);
            StopIndex = Mathf.Clamp(stop, 0, Stops.Count - 1);
            host.Jump(this, StopIndex);
            message = next == 0 ? "变体 A：M0 = 休闲池（PL_Leisure），m 槽 = 球场（PL_Court）。" : "变体 B：M0 = 后现代厅（PL_PoMo），m 槽 = 怪房间（PL_Weird）。";
        }
        catch (Exception e)
        {
            message = "切换变体失败：" + e.Message;
            Debug.LogError(FrontRoomsStructures.Log + "P variant: " + e);
        }
        return true;
    }

    // The walker aims at the map's doors and keys as the game's player does.
    public override string Describe(Collider c, out bool hold)
    {
        hold = false;
        return map != null ? map.Describe(c, out hold) : null;
    }

    public override void Use(Collider c)
    {
        if (map != null) map.Use(c);
    }

    public override bool Hold(Collider c, Vector3 point, float dt, out float progress)
    {
        progress = 0f;
        return map != null && map.Hold(c, point, dt, out progress);
    }

    public override void ReleaseHold(Collider c)
    {
        if (map != null) map.ReleaseHold(c);
    }

    public override string Status(FrontRoomsStructures host)
    {
        if (plan == null) return "未构建";
        return "种子 " + floorSeed + " · 变体 " + (variant == 0 ? "A" : "B") + " · " + plan.Rooms.Count + " 个房间";
    }

    public override void Guide(FrontRoomsStructures host, StringBuilder sb)
    {
        sb.AppendLine(Title);
        sb.AppendLine("从 B2 普通的走廊进门：前台 → 更衣室 → 泳池大厅 → 远处的圆拱 → 泳池层四个区 → EXIT；再从池厅旁的防火楼梯往下走进暗区。全部由游戏自己的地图代码生成。");
        sb.AppendLine();
        sb.AppendLine("按键：[ ] 换站点（17 个），R 回到站点，V 切换变体（A 休闲池 + 球场 / B 后现代厅 + 怪房间），E 开门。");
        sb.AppendLine();
        sb.AppendLine("要你决定（在对话里按编号告诉总控）：");
        sb.AppendLine("P-1 泳池俱乐部放在 B2、后面接泳池层：照这样放进游戏吗？");
        sb.AppendLine("P-2 B2 这一层：要钥匙、楼层站，还是两个都要？（现在都没有）");
        sb.AppendLine("P-3 M0 选哪个：休闲池（A）还是后现代厅（B）？按 V 对比。");
        sb.AppendLine("P-4 球场和怪房间多常出现？（现在：球场大约每 10 个 B2 出现一次，怪房间每层最多 2 个）");
        sb.AppendLine("P-5 暗区：照现在这样（往下一层、水深约 1 米、只有暖色壁灯）吗？");
        sb.AppendLine("P-6 F1 配色：瓷白的砖、青色的腰线、偏青绿的“不对劲”的水，看着对吗？");
        sb.AppendLine();
        sb.AppendLine("灰盒占位：所有道具（前台、储物柜、淋浴、梯子、救生椅、标牌、滑梯管、铁门、扶手）、水面、天花板光斑。水里还不能游（P2），没有 Relay（P3）。");
        if (plan != null)
        {
            sb.AppendLine();
            sb.AppendLine("技术记录（给总控）：");
            sb.Append(plan.Describe());
            sb.AppendLine("Seed searched from " + searchedFrom + "; both variants fit seed " + floorSeed + ".");
        }
        else if (!string.IsNullOrEmpty(error)) sb.AppendLine("（路线没有建成：" + error + "）");
    }

    public override void Release()
    {
        TearDown();
        base.Release();
    }
}
