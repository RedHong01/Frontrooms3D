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
/// [ ] step through the plan's 17 focus points (M0's kept out of its basin: StopFoci). Not built here: the player's water
/// moves (P2) and the Relay (P3).
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

        // Stops: the plan's 17 focus points, in world space (M0's on walkable floor out of its basin: StopFoci).
        Stops.Clear();
        var foci = StopFoci(plan, out var m0Note);
        for (var k = 0; k < foci.Count; k++)
        {
            var f = foci[k];
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
        Debug.Log(FrontRoomsStructures.Log + "P built: " + plan.Describe().Replace("\n", " | ") + " | " + m0Note);
    }

    // ---------------------------------------------------------------- where the stops stand

    const int M0Stop = 7;
    static readonly GridCoord[] Steps4 = { new GridCoord(1, 0), new GridCoord(-1, 0), new GridCoord(0, 1), new GridCoord(0, -1) };

    // The stops (p_m0, 11-05; research/level_space/70 section 1): the plan's 17 focus points (FrontRoomsPoolPlan.MakeFocus),
    // M0's (stop 7) by one rule of this scene's. Until pool_open_clear (11-05 07:00) the plan stood M0's stop 1 m in from the
    // edge by which its walk from the entrance first entered the room; where that walk first entered through the basin
    // (variant A's PL_Leisure by its north arch on 4 seeds of 39, a drop into 0.75 m of water), that point stood in the
    // water; since then MakeFocus takes the rule below itself, so this one stays as a guard. M0's stop stands 1 m in
    // from the middle of the edge by which the same walk (the plan's own: breadth first from the entrance, a step up of at
    // most PoolRules.StepUp, any step down) first reaches a cell of the room that is walkable floor out of the basin (not
    // Wade, not a stair, not Void: the ankle rows, a deck or an entry's threshold), looking at the room's centre (A's basin
    // water, B's hall), or 6 m on into the room when the centre stands nearer than 2.5 m, as the plan's stops look; its name
    // stays. Where the plan's own point stands on such floor (39 seeds of 39 in A and in B) it is that point. With
    // no such cell the plan's point stays (logged). Static and plan-only, so SignSpot and a check run it on any seed's plan.
    public static List<PoolFocus> StopFoci(FrontRoomsPoolPlan plan, out string m0Note)
    {
        var list = new List<PoolFocus>();
        m0Note = "M0 stop: no plan";
        if (plan == null) return list;
        for (var k = 0; k < plan.Focus.Count; k++) list.Add(plan.Focus[k]);
        if (list.Count > M0Stop) list[M0Stop] = M0Focus(plan, list[M0Stop], out m0Note);
        return list;
    }

    static PoolFocus M0Focus(FrontRoomsPoolPlan plan, PoolFocus f, out string note)
    {
        var cs = MapGrid.CellSize;
        var o = plan.OriginCell;
        var at = new GridCoord(Mathf.FloorToInt(f.x / cs), Mathf.FloorToInt(f.z / cs));
        var room = plan.RoomAt(at);
        var block = "block (" + (at.x - o.x) + ", " + (at.y - o.y) + ")";
        if (room == null) { note = "M0 stop: the plan's, " + block + " (no room under it)"; return f; }
        var under = plan.CellOf(at);
        if (OutOfBasin(under)) { note = "M0 stop: the plan's, " + block + " (" + under.water + ")"; return f; }
        var from = new Dictionary<GridCoord, GridCoord>();
        var queue = new Queue<GridCoord>();
        from[plan.EntranceIn] = plan.EntranceIn;
        queue.Enqueue(plan.EntranceIn);
        while (queue.Count > 0)
        {
            var cell = queue.Dequeue();
            foreach (var s in Steps4)
            {
                var next = cell + s;
                if (from.ContainsKey(next) || !CanStep(plan, cell, next)) continue;
                from[next] = cell;
                queue.Enqueue(next);
                if (plan.RoomAt(next) != room || !OutOfBasin(plan.CellOf(next))) continue;
                // The first cell of the room on walkable floor out of the basin, in the walk's order: 1 m in from the
                // middle of the edge it is walked onto by.
                int dx = next.x - cell.x, dy = next.y - cell.y;
                var g = new PoolFocus { name = f.name, cell = next };
                g.x = (next.x + cell.x + 1) * cs * .5f + dx * 1.0f;
                g.z = (next.y + cell.y + 1) * cs * .5f + dy * 1.0f;
                g.y = plan.FloorAt(g.x, g.z);
                float ex = room.CentreX - g.x, ez = room.CentreZ - g.z;
                if (ex * ex + ez * ez < 2.5f * 2.5f) { g.lookX = g.x + dx * 6f; g.lookZ = g.z + dy * 6f; }
                else { g.lookX = room.CentreX; g.lookZ = room.CentreZ; }
                g.lookY = plan.FloorAt(g.lookX, g.lookZ) + 1.5f;
                var by = plan.RoomAt(cell);
                note = "M0 stop: moved off the basin (the plan's stood in " + under.water + " at " + f.y.ToString("0.00") + ", " + block + "), to 1 m in from block ("
                    + (next.x - o.x) + ", " + (next.y - o.y) + ")'s " + (dx > 0 ? "west" : dx < 0 ? "east" : dy > 0 ? "south" : "north") + " edge, walked onto from "
                    + (by == null ? "-" : by == room ? "the room" : by.label) + ", floor " + g.y.ToString("0.00");
                return g;
            }
        }
        note = "M0 stop: the plan's, " + block + " (" + under.water + "; the walk reaches no cell of the room out of the basin)";
        return f;
    }

    // Walkable floor out of the basin: not Void, not Wade, not a flight of steps.
    static bool OutOfBasin(ModuleCell c) =>
        c.connector != CellConnector.Void && c.connector != CellConnector.Stair && c.water != CellWater.Wade;

    // The plan's step rule (FrontRoomsPoolPlan.CanStep): a walkable edge to a room cell, at most a capsule's step up where the
    // two floors meet.
    static bool CanStep(FrontRoomsPoolPlan plan, GridCoord from, GridCoord to)
    {
        if (plan.RoomAt(to) == null) return false;
        if (!FrontRoomsExitPlanner.Walkable(plan.EdgeOf(from, to))) return false;
        int dx = to.x - from.x, dy = to.y - from.y;
        var up = PoolRules.EdgeFloor(plan.CellOf(to), -dx, -dy) - PoolRules.EdgeFloor(plan.CellOf(from), dx, dy);
        return up <= PoolRules.StepUp + 1e-3f;
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

    // A sign named by a word has no code chip and opens its title with the name and ： (20_placard_type.md v2.8, E: chips carry
    // codes only, Latin letters and figures): 远处的门 and 防火楼梯 had stood in the chip, in the mono face, which has no CJK.
    void MakeSigns(FrontRoomsStructures host)
    {
        var log = new StringBuilder("P signs (70 section 1):");
        log.Append(Sign(0, "P · 泳池路线（游戏自己的地图代码）",
            "这是什么：B2 这一层的一段路。从普通走廊进门，经过前台、更衣室，到大泳池厅；穿过远处的圆拱是泳池层（Z0 到 Z3 四个区）；"
            + "最后从防火楼梯往下走进暗区。全部由游戏自己的地图代码生成，和游戏里一样走。\n"
            + "放在哪：游戏的 B2（第 3 层）。你说可以，才会在游戏里打开开关。\n"
            + "要你决定：P-1 到 P-6，按 G 看。\n"
            + "灰盒占位：前台、储物柜、淋浴、梯子、救生椅、标牌、滑梯管等都是灰色替身；水面和天花板上的光斑是临时效果；"
            + "水里还不能游（P2），这里也没有 Relay（P3）。"));
        log.Append(Sign(4, "远处的门：进入泳池层",
            "这是什么：泳池大厅北墙的圆拱（M5），后面就是泳池层。泳池层由一个个房间模组拼成：L0、L1 两个大厅（办公室吊顶 M1、柱厅 M2），"
            + "M0（休闲池或后现代厅），画廊（M4），球场或怪房间（M6），池厅。\n"
            + "要你决定：P-1，这一层整体的感觉；P-6，配色。\n"
            + "灰盒占位：灯具和水面效果是临时的。"));
        log.Append(Sign(7, "M0 · 两种变体（按 V）",
            "按 V 切换：A = 休闲池（PL_Leisure）+ 球场（PL_Court）；B = 后现代厅（PL_PoMo）+ 怪房间（PL_Weird）。两种加起来，15 个房间模组都能走到。\n"
            + "要你决定：P-3 选哪一个；P-4 球场和怪房间多常出现。\n"
            + "灰盒占位：滑梯管、玻璃砖墙、圆窗是替身。"));
        log.Append(Sign(11, "EXIT · 这一层的出口",
            "这是什么：这一层的 EXIT 门和楼梯间，和游戏里一样由地图放在这里。在游戏里，进门就是去下一层。\n"
            + "要你决定：P-2，B2 要钥匙、楼层站，还是两个都要（现在都没有）。\n"
            + "灰盒占位：楼梯间里面是游戏现在的样子。"));
        log.Append(Sign(12, "防火楼梯：往下走进暗区",
            "这是什么：7 + 7 级台阶（每级 0.18 × 0.28 米）一直走进水里，铁门开着，下面是暗区：比泳池层低 2.52 米，水深约 1 米，"
            + "只有暖色的壁灯。你说过：“第一版就做成真正往下一层”。\n"
            + "要你决定：P-5，暗区这样做对吗。\n"
            + "灰盒占位：铁门、扶手、壁灯是替身。"));
        Debug.Log(FrontRoomsStructures.Log + log);
    }

    // Where a sign hangs (p_signs, 11-05; research/level_space/70 section 1), from the room's own geometry in the plan, the same
    // on every seed and variant. Each sign hangs flat on a wall of its stop's room, its back on the wall, without posts (on the
    // pool level they would stand in the water):
    //  1. Walls: the room's plain wall faces (a Wall edge without a record) and the solid stubs beside an opening whose width the
    //     plan gives (a record's arch or gate, centred as PoolArch builds it; a door 1.0 m and a window 1.4 m, centred; an
    //     observation window 1.0 m), merged into straight runs. A run's end keeps 0.10 m off a corner and 0.35 m off an opening
    //     on its own line; a stub keeps 0.25 m off its opening.
    //  2. Spots: on each run, the panel's centre where its face's centre stands 2.5 m from the stop's eye (20_placard_type.md,
    //     E's FAR), both ways along the run, else the nearest the run allows, then in 0.2 m steps either side along the whole
    //     run, nearest first. A spot holds when the panel (1.2 m) has walkable floor under its foot and at the reading spot
    //     0.9 m out (a cell of the same room, not a basin, not on a flight of steps: the pool level's ankle water is its floor),
    //     no stand-in or wall lamp stands within 0.05 m of the panel or of a reader there (a kit the plan leaves out,
    //     FrontRoomsPoolPlan.KitLeftOut, is not built and counts as none; a valance stands where the pool pass hangs it, slid,
    //     shortened or left out clear of its wall's openings, FrontRoomsPoolPlan.OpeningSpanAt), it keeps out of every opening's keep-clear strip
    //     (a door's 1.2 m, an arch's or an open edge's 1.0 m, the opening and 0.3 m either side), stands
    //     0.6 m or more from every stop's floor point, and the eye sees its face (in front of it, no wall or stand-in between).
    //  3. Choice: the least |d - 2.5| (d farther than 2.5 counts double); spots within 0.15 m of that count as equal, and of
    //     those the nearest the stop's view axis (within 5 degrees: equal), then the one on the view's right.
    //  4. Blade (E-blade, 20_placard_type.md v2.5): when the eye reads the face more than 35 degrees off its normal, a blade on
    //     the same wall over the panel, its front face's centre 2.5 m from the eye (within the panel's span), its bottom 2.10 m
    //     over the highest floor under it (walkable), the ceiling 0.1 m or more over its top, clear of stand-ins and lamps and
    //     0.5 m from every stop's floor point, read 35 degrees or less off its face. A spot that needs a blade and cannot hang
    //     one gives way to the next; when none can, the best spot keeps its flat sign alone (logged).
    // Returns the placement for the build's log line.
    string Sign(int stop, string title, string body)
    {
        if (stop < 0 || stop >= Stops.Count || map == null || plan == null) return "";
        var s = Stops[stop];
        var eye = s.position + Vector3.up * FrontRoomsStructuresWalker.Eye;
        // The panel is built first, to learn its height, then hung where the rule puts it.
        var go = FrontRoomsStructuresSigns.Placard(Root, s.position + Vector3.up * 1.5f, eye, title, body, s.position.y, 1, false);
        if (go == null) return "";
        var panel = go.transform.Find("Panel");
        var spot = SignSpot(plan, stop, panel != null ? panel.localScale.y : .8f, FrontRoomsStructuresSigns.BladeHeight(title));
        if (!spot.found)
        {
            UnityEngine.Object.Destroy(go);
            FrontRoomsPoolMaterials.LogOnce("sign " + stop, "no wall for the sign at stop " + stop + " (" + spot.note + "): its text is on the G panel only.");
            return " | s" + stop.ToString("00") + " none";
        }
        var t = map.transform;
        var into = t.TransformDirection(spot.into);
        go.transform.SetPositionAndRotation(t.TransformPoint(spot.root), Quaternion.LookRotation(-into, Vector3.up));
        made.Add(go);
        if (spot.blade)
        {
            var wall = t.TransformPoint(spot.bladeWall);
            var b = FrontRoomsStructuresSigns.Blade(Root, wall, into, eye, title, t.TransformPoint(new Vector3(spot.bladeWall.x, spot.bladeFloorY, spot.bladeWall.z)).y);
            if (b != null) made.Add(b);
        }
        return " | s" + stop.ToString("00") + " " + s.name + ": " + spot.note;
    }

    /// <summary>Where a stop's sign hangs, in the plan's metres (SignSpot).</summary>
    public struct SignPlace
    {
        public bool found, blade;
        /// <summary>The panel's root (its top edge's middle, as Placard places it), the wall's normal into the room, the face's
        /// centre, the floor under it.</summary>
        public Vector3 root, into, face;
        public float floorY;
        /// <summary>The eye to the face's centre (m), how far off its normal the eye reads it and how far it stands off the
        /// stop's view axis (degrees, horizontal), on the view's right.</summary>
        public float distance, offFace, viewDeg;
        public bool right;
        /// <summary>The blade: a point on the wall's face at its plane, the floor under it, its front face's centre to the eye,
        /// how far off its face the eye reads it, its top and the ceiling over it.</summary>
        public Vector3 bladeWall, bladeFace;
        public float bladeFloorY, bladeDistance, bladeOffFace, bladeTop, bladeCeiling;
        public string note;
    }

    struct WallRun
    {
        public bool alongX;
        public float line, a, b;
        public Vector3 n;
        public Vector3 At(float t, float off) => (alongX ? new Vector3(t, 0f, line) : new Vector3(line, 0f, t)) + n * off;
        public float Along(Vector3 p) => alongX ? p.x : p.z;
    }

    struct KeepOut
    {
        public float x0, x1, z0, z1;
        public bool Hits(float ax, float az, float bx, float bz, float pad) =>
            Mathf.Max(ax, bx) > x0 - pad && Mathf.Min(ax, bx) < x1 + pad && Mathf.Max(az, bz) > z0 - pad && Mathf.Min(az, bz) < z1 + pad;
    }

    // The panel's root stands 40 mm off the wall's face, so its back (the panel 30 mm thick, 5-35 mm behind the root's face
    // plane) is 5 mm off the wall and its face 35 mm.
    const float SignTarget = 2.5f, PanelHalf = .6f, HangOff = .04f, FaceOff = .035f, ReadOut = .9f, BladeRule = 35f;

    /// <summary>
    /// The rule above for one stop of a plan (map metres; the stop's floor point and look as the route stands them,
    /// StopFoci), the panel's and the blade's heights given. Static and plan-only, so a check can run it on any seed's plan.
    /// </summary>
    public static SignPlace SignSpot(FrontRoomsPoolPlan plan, int stop, float panelHeight, float bladeHeight)
    {
        var none = new SignPlace { note = "" };
        if (plan == null || stop < 0 || stop >= plan.Focus.Count) { none.note = "no such stop"; return none; }
        var stops = StopFoci(plan, out _);
        var f = stops[stop];
        var cs = MapGrid.CellSize;
        var room = plan.RoomAt(new GridCoord(Mathf.FloorToInt(f.x / cs), Mathf.FloorToInt(f.z / cs)));
        if (room == null) { none.note = "the stop stands in no room"; return none; }
        var eye = new Vector3(f.x, f.y + FrontRoomsStructuresWalker.Eye, f.z);
        var view = new Vector3(f.lookX - f.x, 0f, f.lookZ - f.z);
        view = view.sqrMagnitude > 1e-4f ? view.normalized : Vector3.forward;
        var boxes = Obstacles(plan, room);
        var keep = KeepClear(plan, room);
        var found = new List<SignPlace>();
        var tried = 0;
        string last = "no wall run long enough";
        foreach (var run in WallRuns(plan, room))
        {
            float lo = run.a + PanelHalf, hi = run.b - PanelHalf;
            if (hi < lo) continue;
            var s0 = run.Along(eye);
            var p = Vector3.Dot(eye - run.At(s0, 0f), run.n) - FaceOff;
            if (p < .5f) continue;
            var solved = false;
            foreach (var way in new[] { -1f, 1f })
            {
                // The face's centre 2.5 m from the eye: solved on the stop's floor, then again on the floor under the spot (one
                // way only when the run stands farther than that).
                var t0 = s0;
                var floor = f.y;
                var reach = false;
                for (var pass = 0; pass < 2; pass++)
                {
                    var dy = floor + 2.05f - panelHeight * .5f - eye.y;
                    var rad = SignTarget * SignTarget - p * p - dy * dy;
                    reach = rad > 0f;
                    t0 = Mathf.Clamp(reach ? s0 + way * Mathf.Sqrt(rad) : s0, lo, hi);
                    if (Walkable(plan, room, run.At(t0, .15f), out var under)) floor = under;
                }
                if (!reach && solved) continue;
                solved = true;
                // From there along the whole run in 0.2 m steps, nearest first; a spot that needs a blade it cannot hang is
                // kept only when the run has none that can.
                var steps = Mathf.CeilToInt((hi - lo) / .2f) + 1;
                var flatOnly = new SignPlace();
                for (var k = 0; k <= 2 * steps; k++)
                {
                    var t = t0 + (k == 0 ? 0f : (k % 2 == 1 ? 1f : -1f) * .2f * ((k + 1) / 2));
                    if (t < lo - 1e-4f || t > hi + 1e-4f) continue;
                    tried++;
                    var place = TrySpot(plan, room, run, t, eye, view, panelHeight, bladeHeight, boxes, keep, stops, out var why);
                    if (!place.found) { last = why; continue; }
                    if (place.offFace > BladeRule && !place.blade)
                    {
                        if (!flatOnly.found) flatOnly = place;
                        last = why;
                        continue;
                    }
                    found.Add(place);
                    flatOnly.found = false;
                    break;
                }
                if (flatOnly.found) found.Add(flatOnly);
            }
        }
        if (found.Count == 0) { none.note = tried + " spots tried, none holds (last: " + last + ")"; return none; }
        // The choice: the distance first, then the view axis, then the right; a spot that needs a blade it cannot hang gives way.
        var withBlade = found.FindAll(q => q.offFace <= BladeRule || q.blade);
        var pool = withBlade.Count > 0 ? withBlade : found;
        float Err(SignPlace q) => q.distance > SignTarget ? 2f * (q.distance - SignTarget) : SignTarget - q.distance;
        var minErr = float.MaxValue;
        foreach (var q in pool) minErr = Mathf.Min(minErr, Err(q));
        var near = pool.FindAll(q => Err(q) <= minErr + .15f);
        var minView = float.MaxValue;
        foreach (var q in near) minView = Mathf.Min(minView, q.viewDeg);
        var front = near.FindAll(q => q.viewDeg <= minView + 5f);
        var best = front[0];
        foreach (var q in front)
            if ((q.right && !best.right) || (q.right == best.right && Err(q) < Err(best) - 1e-4f)) best = q;
        best.note = "wall " + WallName(best.into) + ", " + best.distance.ToString("0.000") + " m from the eye, " + best.offFace.ToString("0.0") + " deg off its face, "
            + best.viewDeg.ToString("0.0") + " deg " + (best.right ? "right" : "left") + " of the view; "
            + (best.blade ? "blade " + best.bladeDistance.ToString("0.000") + " m, " + best.bladeOffFace.ToString("0.0") + " deg off its face, top " + best.bladeTop.ToString("0.00") + " (ceiling " + best.bladeCeiling.ToString("0.00") + ")"
                : best.offFace > BladeRule ? "NO BLADE (none could hang)" : "no blade (35 deg rule)")
            + "; " + found.Count + " spots hold of " + tried + " tried";
        return best;
    }

    static string WallName(Vector3 into) =>
        Mathf.Abs(into.x) > .5f ? (into.x > 0f ? "west wall (faces east)" : "east wall (faces west)") : (into.z > 0f ? "south wall (faces north)" : "north wall (faces south)");

    static SignPlace TrySpot(FrontRoomsPoolPlan plan, PoolRoom room, WallRun run, float t, Vector3 eye, Vector3 view, float h, float bladeH,
        List<Bounds> boxes, List<KeepOut> keep, List<PoolFocus> stops, out string why)
    {
        var q = new SignPlace();
        // Walkable floor under the panel's foot, level within a step.
        if (!Walkable(plan, room, run.At(t, .15f), out var floorY)) { why = "no floor under the panel"; return q; }
        foreach (var k in new[] { -.55f, .55f })
            if (!Walkable(plan, room, run.At(t + k, .15f), out var fk) || Mathf.Abs(fk - floorY) > .31f) { why = "no level floor under the panel's ends"; return q; }
        var read = run.At(t, FaceOff + ReadOut);
        if (!Walkable(plan, room, read, out var readY) || Mathf.Abs(readY - floorY) > .35f) { why = "no floor at the reading spot"; return q; }
        var face = run.At(t, FaceOff);
        face.y = floorY + 2.05f - h * .5f;
        var toEye = eye - face;
        if (Vector3.Dot(toEye, run.n) < .5f) { why = "the eye is not in front of it"; return q; }
        var d = toEye.magnitude;
        if (d > 16f) { why = "farther than 16 m"; return q; }
        // The panel, and a reader standing at the reading spot, clear of every stand-in and wall lamp.
        var a = run.At(t - PanelHalf, 0f);
        var b = run.At(t + PanelHalf, .08f);
        var panelBox = new Bounds();
        panelBox.SetMinMax(new Vector3(Mathf.Min(a.x, b.x), floorY + 2.05f - h, Mathf.Min(a.z, b.z)), new Vector3(Mathf.Max(a.x, b.x), floorY + 2.05f, Mathf.Max(a.z, b.z)));
        panelBox.Expand(.1f);
        var reader = new Bounds(new Vector3(read.x, readY + .9f, read.z), new Vector3(.6f, 1.8f, .6f));
        foreach (var box in boxes)
            if (box.Intersects(panelBox) || box.Intersects(reader)) { why = "a stand-in or lamp is in the way"; return q; }
        foreach (var k in keep)
            if (k.Hits(a.x, a.z, b.x, b.z, 0f)) { why = "in an opening's keep-clear strip"; return q; }
        foreach (var other in stops)
            if (SegmentDistance(new Vector2(other.x, other.z), new Vector2(a.x, a.z), new Vector2(b.x, b.z)) < .6f) { why = "on a stop's floor point"; return q; }
        if (!Sees(plan, eye, face, boxes)) { why = "the eye does not see it"; return q; }
        var flat = new Vector3(toEye.x, 0f, toEye.z);
        var toFace = new Vector3(-flat.x, 0f, -flat.z);
        q.found = true;
        q.into = run.n;
        q.face = face;
        q.floorY = floorY;
        q.root = run.At(t, HangOff);
        q.root.y = floorY + 2.05f;
        q.distance = d;
        q.offFace = Vector3.Angle(run.n, flat);
        q.viewDeg = Vector3.Angle(view, toFace);
        q.right = Vector3.Dot(toFace, Vector3.Cross(Vector3.up, view)) > 0f;
        why = "";
        if (q.offFace <= BladeRule) return q;
        // The blade over the panel: its plane where its front face's centre is 2.5 m from the eye, within the panel's span.
        var s0 = run.Along(eye);
        var off = Vector3.Dot(eye - run.At(s0, 0f), run.n) - .65f;
        var side = Mathf.Abs(t - s0) > 1e-3f ? Mathf.Sign(t - s0) : 1f;
        var tb = t;
        var fb = floorY;
        for (var pass = 0; pass < 3; pass++)
        {
            // Its front face (15 mm toward the eye from its plane) 2.5 m from the eye.
            var dyb = fb + 2.10f + bladeH * .5f - eye.y;
            var rad = SignTarget * SignTarget - off * off - dyb * dyb;
            tb = Mathf.Clamp(rad > 0f ? s0 + side * (Mathf.Sqrt(rad) + .015f) : t, t - (PanelHalf - .1f), t + (PanelHalf - .1f));
            fb = -999f;
            foreach (var o in new[] { .1f, .65f, 1.25f })
            {
                if (!Walkable(plan, room, run.At(tb, o), out var fo)) { fb = float.NaN; break; }
                fb = Mathf.Max(fb, fo);
            }
            if (float.IsNaN(fb)) { why = "the blade would hang over no floor"; return Flat(q); }
        }
        var top = fb + 2.10f + bladeH;
        var ceiling = room.CeilingY;
        if (ceiling - top < .1f) { why = "no room for the blade under the ceiling"; return Flat(q); }
        var w0 = run.At(tb - .05f, .03f);
        var w1 = run.At(tb + .05f, 1.27f);
        var bladeBox = new Bounds();
        bladeBox.SetMinMax(new Vector3(Mathf.Min(w0.x, w1.x), fb + 2.10f, Mathf.Min(w0.z, w1.z)), new Vector3(Mathf.Max(w0.x, w1.x), top, Mathf.Max(w0.z, w1.z)));
        bladeBox.Expand(.1f);
        foreach (var box in boxes)
            if (box.Intersects(bladeBox)) { why = "a stand-in or lamp is in the blade's way"; return Flat(q); }
        foreach (var other in stops)
            if (SegmentDistance(new Vector2(other.x, other.z), new Vector2(w0.x, w0.z), new Vector2(w1.x, w1.z)) < .5f) { why = "the blade over a stop's floor point"; return Flat(q); }
        var along = run.alongX ? new Vector3(1f, 0f, 0f) : new Vector3(0f, 0f, 1f);
        var centre = run.At(tb, .65f);
        centre.y = fb + 2.10f + bladeH * .5f;
        var frontN = Vector3.Dot(along, eye - centre) >= 0f ? along : -along;
        var bladeFace = centre + frontN * .015f;
        var toEyeB = eye - bladeFace;
        var offB = Vector3.Angle(frontN, new Vector3(toEyeB.x, 0f, toEyeB.z));
        if (offB > BladeRule) { why = "the blade would be read " + offB.ToString("0") + " deg off its face"; return Flat(q); }
        q.blade = true;
        q.bladeWall = run.At(tb, 0f);
        q.bladeWall.y = fb;
        q.bladeFloorY = fb;
        q.bladeFace = bladeFace;
        q.bladeDistance = toEyeB.magnitude;
        q.bladeOffFace = offB;
        q.bladeTop = top;
        q.bladeCeiling = ceiling;
        return q;
    }

    // A spot whose blade cannot hang: kept, flat sign only (the choice takes a spot with a blade first).
    static SignPlace Flat(SignPlace q)
    {
        q.blade = false;
        return q;
    }

    // A point (map metres) over a cell of the room that a reader stands on: not Void, not a basin, not on a flight's slope;
    // its floor.
    static bool Walkable(FrontRoomsPoolPlan plan, PoolRoom room, Vector3 p, out float floorY)
    {
        floorY = 0f;
        var cs = MapGrid.CellSize;
        var cell = new GridCoord(Mathf.FloorToInt(p.x / cs), Mathf.FloorToInt(p.z / cs));
        if (plan.RoomAt(cell) != room) return false;
        var c = plan.CellOf(cell);
        if (c.connector == CellConnector.Void || c.water == CellWater.Wade) return false;
        float u = p.x - cell.x * cs, v = p.z - cell.y * cs;
        if (c.connector == CellConnector.Stair && c.StairDistance(u, v) > c.stairFrom - .05f) return false;
        floorY = c.connector == CellConnector.Stair ? c.floorY : c.FloorAt(u, v);
        return true;
    }

    // The room's wall faces: per cell side, the solid stretches of the edge (cell-local 0..3 along it), merged along each line.
    static List<WallRun> WallRuns(FrontRoomsPoolPlan plan, PoolRoom room)
    {
        var cs = MapGrid.CellSize;
        var half = ModuleUnits.WallHalf;
        var pieces = new List<WallRun>();
        for (var j = 0; j < room.depth; j++)
        for (var i = 0; i < room.width; i++)
        {
            var c = new GridCoord(room.origin.x + i, room.origin.y + j);
            for (var dir = 0; dir < 4; dir++)
            {
                int dx = dir == 0 ? 1 : dir == 2 ? -1 : 0, dz = dir == 1 ? 1 : dir == 3 ? -1 : 0;
                var nb = new GridCoord(c.x + dx, c.y + dz);
                var alongX = dz != 0;
                var line = (dx > 0 ? (c.x + 1) * cs : dx < 0 ? c.x * cs : dz > 0 ? (c.y + 1) * cs : c.y * cs) - (dx + dz) * half;
                var start = alongX ? c.x * cs : c.y * cs;
                foreach (var seg in Solid(plan, c, nb))
                    pieces.Add(new WallRun { alongX = alongX, line = line, a = start + seg.x, b = start + seg.y, n = new Vector3(-dx, 0f, -dz) });
            }
        }
        // Merge touching pieces on the same line facing the same way.
        pieces.Sort((p, q) => p.alongX != q.alongX ? p.alongX.CompareTo(q.alongX) : Mathf.Abs(p.line - q.line) > 1e-3f ? p.line.CompareTo(q.line)
            : Mathf.Abs(p.n.x + p.n.z - q.n.x - q.n.z) > 1e-3f ? (p.n.x + p.n.z).CompareTo(q.n.x + q.n.z) : p.a.CompareTo(q.a));
        var runs = new List<WallRun>();
        foreach (var p in pieces)
        {
            if (runs.Count > 0)
            {
                var last = runs[runs.Count - 1];
                if (last.alongX == p.alongX && Mathf.Abs(last.line - p.line) < 1e-3f && Vector3.Dot(last.n, p.n) > .5f && p.a <= last.b + 1e-3f)
                {
                    last.b = Mathf.Max(last.b, p.b);
                    runs[runs.Count - 1] = last;
                    continue;
                }
            }
            runs.Add(p);
        }
        // A run's end at a cell corner: 0.35 m off when the room goes on along that line (an opening follows), else 0.10 m (a
        // corner, the side wall's half thickness and 2 cm).
        for (var k = 0; k < runs.Count; k++)
        {
            var r = runs[k];
            r.a += EndKeep(plan, room, r, r.a, -1);
            r.b -= EndKeep(plan, room, r, r.b, 1);
            runs[k] = r;
        }
        return runs;
    }

    static float EndKeep(FrontRoomsPoolPlan plan, PoolRoom room, WallRun r, float end, int way)
    {
        var cs = MapGrid.CellSize;
        var k = end / cs;
        if (Mathf.Abs(k - Mathf.Round(k)) > 1e-3f) return 0f;
        var beyond = r.At(end + way * .5f, .5f);
        var cell = new GridCoord(Mathf.FloorToInt(beyond.x / cs), Mathf.FloorToInt(beyond.z / cs));
        return plan.RoomAt(cell) == room ? .35f : .10f;
    }

    // The solid stretches of the edge from cell c to nb, cell-local metres along it (west to east, south to north).
    static List<Vector2> Solid(FrontRoomsPoolPlan plan, GridCoord c, GridCoord nb)
    {
        var list = new List<Vector2>();
        var cs = MapGrid.CellSize;
        var exit = (c == plan.FrontCell && nb == plan.ExitCell) || (c == plan.ExitCell && nb == plan.FrontCell);
        var kind = exit ? EdgeKind.Door : plan.EdgeOf(c, nb);
        var has = plan.TryRecord(c, nb, out var rec);
        float width;
        if (kind == EdgeKind.Wall)
        {
            if (!has || rec.style == PoolEdgeStyle.None) { list.Add(new Vector2(0f, cs)); return list; }
            if (rec.style != PoolEdgeStyle.ObsWindow) return list;
            width = 1.0f;
        }
        else if (kind == EdgeKind.Arch && has && rec.archWidth > 0f) width = rec.archWidth + ((rec.trim & 2) != 0 ? .3f : 0f);
        else if (kind == EdgeKind.Door) width = ModuleUnits.DoorWidth + 2f * (ModuleUnits.TrimFace + ModuleUnits.TrimProud);
        else if (kind == EdgeKind.Window) width = ModuleUnits.WindowWidth + 2f * (ModuleUnits.TrimFace + ModuleUnits.TrimProud);
        else return list;
        var stub = (cs - width) * .5f - .25f;
        if (stub > .05f)
        {
            list.Add(new Vector2(0f, stub));
            list.Add(new Vector2(cs - stub, cs));
        }
        return list;
    }

    // The keep-clear strips inside the room's openings to other cells: the opening and 0.3 m either side, a door's 1.2 m deep
    // (its leaf), an arch's, window's or open edge's 1.0 m (ModuleUnits.EntryClearDepth).
    static List<KeepOut> KeepClear(FrontRoomsPoolPlan plan, PoolRoom room)
    {
        var list = new List<KeepOut>();
        var cs = MapGrid.CellSize;
        for (var j = 0; j < room.depth; j++)
        for (var i = 0; i < room.width; i++)
        {
            var c = new GridCoord(room.origin.x + i, room.origin.y + j);
            for (var dir = 0; dir < 4; dir++)
            {
                int dx = dir == 0 ? 1 : dir == 2 ? -1 : 0, dz = dir == 1 ? 1 : dir == 3 ? -1 : 0;
                var nb = new GridCoord(c.x + dx, c.y + dz);
                if (plan.RoomAt(nb) == room) continue;
                var exit = c == plan.FrontCell && nb == plan.ExitCell;
                var kind = exit ? EdgeKind.Door : plan.EdgeOf(c, nb);
                if (kind == EdgeKind.Wall) continue;
                var has = plan.TryRecord(c, nb, out var rec);
                var width = kind == EdgeKind.Door ? ModuleUnits.DoorWidth : has && rec.archWidth > 0f ? rec.archWidth : kind == EdgeKind.Window ? ModuleUnits.WindowWidth : cs;
                var depth = kind == EdgeKind.Door ? ModuleUnits.DoorClearDepth : ModuleUnits.EntryClearDepth;
                float a0 = (cs - width) * .5f - .3f, a1 = (cs + width) * .5f + .3f;
                var k = new KeepOut();
                if (dz != 0)
                {
                    k.x0 = c.x * cs + a0; k.x1 = c.x * cs + a1;
                    var e = dz > 0 ? (c.y + 1) * cs : c.y * cs;
                    k.z0 = dz > 0 ? e - depth : e; k.z1 = dz > 0 ? e : e + depth;
                }
                else
                {
                    k.z0 = c.y * cs + a0; k.z1 = c.y * cs + a1;
                    var e = dx > 0 ? (c.x + 1) * cs : c.x * cs;
                    k.x0 = dx > 0 ? e - depth : e; k.x1 = dx > 0 ? e : e + depth;
                }
                list.Add(k);
            }
        }
        return list;
    }

    // The room's stand-ins as the pool pass builds them (FrontRoomsMapWorld.Pool's PoolStandIns: the skin's kits but those
    // the plan leaves out, FrontRoomsPoolPlan.KitLeftOut, where an opening of the plan crosses one; FrontRoomsPoolStandIns
    // builds each kit, its box turned by its yaw) and wall lamps (as FrontRoomsMapWorld.Pool's PoolRoomLamps hangs a valance,
    // a halide bracket, a stair light or a globe; a valance where pool_valance_clear puts it, ValanceSpot below), map metres.
    static List<Bounds> Obstacles(FrontRoomsPoolPlan plan, PoolRoom room)
    {
        var list = new List<Bounds>();
        if (room.module == null) return list;
        var cs = MapGrid.CellSize;
        var ox = room.origin.x * cs;
        var oz = room.origin.y * cs;
        var skin = PoolRules.SkinOf(room.module);
        var kits = skin.standIns;
        if (kits != null)
            for (var i = 0; i < kits.Length; i++)
            {
                // A kit the plan leaves out is not built: no obstacle, a sign may hang where it would have stood.
                if (plan.KitLeftOut(room, i)) continue;
                var s = kits[i];
                if (!KitBox(s.kit, s.length, out var centre, out var size)) continue;
                var x = ox + s.x;
                var z = oz + s.z;
                var y = s.fromBase ? room.BaseY + s.y : plan.FloorAt(x, z) + s.y;
                var rot = Quaternion.Euler(0f, s.yaw, 0f);
                var e = size * .5f;
                var r = rot * Vector3.right;
                var fw = rot * Vector3.forward;
                var ext = new Vector3(Mathf.Abs(r.x) * e.x + Mathf.Abs(fw.x) * e.z, e.y, Mathf.Abs(r.z) * e.x + Mathf.Abs(fw.z) * e.z);
                list.Add(new Bounds(new Vector3(x, y, z) + rot * centre, ext * 2f));
            }
        float w = room.width * cs, d = room.depth * cs;
        var ceilingY = room.CeilingY;
        var baseY = room.BaseY;
        switch (skin.lampKit)
        {
            case PoolLampKit.Valance:
            {
                // Along the two longest walls, one meant per 6 m from 3 m, each kept clear of its wall's openings at its foot
                // (0.78 m under the ceiling; the plan's OpeningSpanAt), as the pool pass hangs it since pool_valance_clear
                // (11-05): slid within its own 6 m, shortened to 1.8 or 1.2 m, else left out. The box covers the fixture (0.12 m
                // square, 0.66-0.78 under the ceiling), its glow strip on top and its up light; a valance whose cell lamp is Off
                // is not built but still counts here.
                var alongX = w >= d;
                var run = alongX ? w : d;
                var cells = alongX ? room.width : room.depth;
                var foot = ceilingY - .78f;
                var openings = new List<Vector2>();
                for (var wall = 0; wall < 2; wall++)
                {
                    openings.Clear();
                    for (var i = 0; i < cells; i++)
                    {
                        var inside = alongX ? new GridCoord(room.origin.x + i, wall == 0 ? room.origin.y : room.origin.y + room.depth - 1)
                            : new GridCoord(wall == 0 ? room.origin.x : room.origin.x + room.width - 1, room.origin.y + i);
                        var outside = alongX ? new GridCoord(inside.x, inside.y + (wall == 0 ? -1 : 1)) : new GridCoord(inside.x + (wall == 0 ? -1 : 1), inside.y);
                        if (plan.OpeningSpanAt(inside, outside, foot, out var f0, out var f1)) openings.Add(new Vector2(i * cs + f0, i * cs + f1));
                    }
                    for (var p = 3f; p < run; p += 6f)
                    {
                        if (!ValanceSpot(openings, p, run, out var spot, out var length)) continue;
                        var x = alongX ? spot : (wall == 0 ? .3f : w - .3f);
                        var z = alongX ? (wall == 0 ? .3f : d - .3f) : spot;
                        list.Add(new Bounds(new Vector3(ox + x, ceilingY - .69f, oz + z), alongX ? new Vector3(length, .22f, .16f) : new Vector3(.16f, .22f, length)));
                    }
                }
                break;
            }
            case PoolLampKit.Rig:
                for (var side = 0; side < 2; side++)
                for (var k = 0; k < 3; k++)
                    list.Add(new Bounds(new Vector3(ox + (side == 0 ? .35f : w - .35f), baseY + 3.45f, oz + d * (k + 1) / 4f), new Vector3(.4f, .3f, .5f)));
                break;
            case PoolLampKit.StairLights:
                list.Add(new Bounds(new Vector3(ox + Mathf.Min(2.8f, w - .15f), baseY + 2.4f, oz + .8f), new Vector3(.2f, .3f, .4f)));
                list.Add(new Bounds(new Vector3(ox + .2f, baseY - .3f, oz + Mathf.Min(6.24f, d - .2f)), new Vector3(.2f, .3f, .4f)));
                break;
        }
        if (room.id == "PC_Lockers")
            for (var k = 0; k < 2; k++)
                list.Add(new Bounds(new Vector3(ox + w - 1.5f + (k == 0 ? -.6f : .6f), ceilingY - .35f, oz + d - 1.5f), new Vector3(.35f, .35f, .35f)));
        return list;
    }

    // FrontRoomsMapWorld.Pool's ValanceSpot (pool_valance_clear, 11-05), the same rule: where a valance meant for p (metres along
    // its wall of length run) stands 0.1 m clear of the wall's openings (along-wall spans at its foot): p itself, else the
    // nearest place just past an opening, inside p's own 6 m and 0.3 m off the corners; at 2.4 m, else 1.8, else 1.2. False:
    // none fits (the pool pass leaves it out). The pool pass keeps it private; this copy follows it.
    const float ValanceClear = .1f;

    static bool ValanceSpot(List<Vector2> openings, float p, float run, out float spot, out float length)
    {
        foreach (var l in new[] { 2.4f, 1.8f, 1.2f })
        {
            var half = l * .5f;
            float lo = Mathf.Max(p - 3f + half, half + .3f), hi = Mathf.Min(p + 3f - half, run - half - .3f);
            bool Free(float c)
            {
                if (c < lo - 1e-4f || c > hi + 1e-4f) return false;
                foreach (var o in openings)
                    if (c + half + ValanceClear > o.x && c - half - ValanceClear < o.y) return false;
                return true;
            }
            length = l;
            spot = p;
            if (Free(p)) return true;
            var best = float.MaxValue;
            foreach (var o in openings)
            {
                var left = o.x - ValanceClear - half;
                var right = o.y + ValanceClear + half;
                if (Free(left) && Mathf.Abs(left - p) < Mathf.Abs(best - p)) best = left;
                if (Free(right) && Mathf.Abs(right - p) < Mathf.Abs(best - p)) best = right;
            }
            if (best < float.MaxValue) { spot = best; return true; }
        }
        spot = p;
        length = 0f;
        return false;
    }

    // A kit's box in its own frame (x its length, y up from its point, z its front), as FrontRoomsPoolStandIns.Build draws it;
    // false for the marks laid flat on the floor (drains, depth marks, court lines), which nothing stands clear of.
    static bool KitBox(string kit, float length, out Vector3 centre, out Vector3 size)
    {
        centre = Vector3.zero;
        size = new Vector3(.5f, .5f, .5f);
        float l;
        switch (kit)
        {
            case "Kit_FloorDrain": case "Kit_MainDrainGrate": case "Kit_DepthMark": case "Kit_CourtLines": return false;
            case "Kit_TowelDesk": l = length > 0f ? length : 3f; centre = new Vector3(0f, .65f, .03f); size = new Vector3(l + .1f, 1.3f, .66f); return true;
            case "Kit_SignBlank": size = new Vector3(length > 0f ? length : 1.2f, .4f, .05f); return true;
            case "Kit_LockerBank": l = length > 0f ? length : 3.4f; centre = new Vector3(0f, .965f, 0f); size = new Vector3(l, 1.93f, .42f); return true;
            case "Kit_LockerBench": l = length > 0f ? length : 3.4f; centre = new Vector3(0f, .24f, 0f); size = new Vector3(l, .48f, .35f); return true;
            case "Kit_ShowerSet": centre = new Vector3(0f, 1.6f, .04f); size = new Vector3(.2f, 1.2f, .5f); return true;
            case "Kit_GlassBlockHalfWall": l = length > 0f ? length : 2.9f; centre = new Vector3(0f, .77f, 0f); size = new Vector3(l + .02f, 1.54f, .14f); return true;
            case "Kit_GlassBlockWall": l = length > 0f ? length : 3.4f; centre = new Vector3(0f, 1.3f, 0f); size = new Vector3(l, 2.6f, .05f); return true;
            case "Kit_PoolLadder": centre = new Vector3(0f, -.15f, -.12f); size = new Vector3(.6f, 2.2f, .55f); return true;
            case "Kit_GuardChair": centre = new Vector3(0f, 1.2f, 0f); size = new Vector3(.7f, 2.4f, .7f); return true;
            case "Kit_RingBuoy": centre = new Vector3(0f, 0f, .06f); size = new Vector3(.7f, .7f, .12f); return true;
            case "Kit_RulesBoard": centre = new Vector3(0f, 0f, .01f); size = new Vector3(1.2f, .9f, .05f); return true;
            case "Kit_PaceClock": centre = new Vector3(0f, 0f, .03f); size = new Vector3(.85f, .85f, .06f); return true;
            case "Kit_ObsWindow": centre = new Vector3(0f, .175f, .02f); size = new Vector3(1.0f, .75f, .06f); return true;
            case "Kit_Soffit": l = length > 0f ? length : 15f; centre = new Vector3(0f, .12f, 0f); size = new Vector3(l, .26f, 3f); return true;
            case "Kit_Beam": l = length > 0f ? length : 15f; centre = new Vector3(0f, .25f, 0f); size = new Vector3(l, .5f, .5f); return true;
            case "Kit_PipeRail": l = length > 0f ? length : 6f; centre = new Vector3(-l * .5f, -.43f, 0f); size = new Vector3(l, .96f, .08f); return true;
            case "Kit_GateBars": centre = new Vector3(-.95f, 1.04f, -.05f); size = new Vector3(.75f, 2.1f, .08f); return true;
            case "Kit_PipeRun": l = length > 0f ? length : 6f; centre = new Vector3(0f, .05f, 0f); size = new Vector3(l, .3f, .2f); return true;
            case "Kit_Oculus": centre = new Vector3(0f, 0f, .02f); size = new Vector3(1.45f, 1.45f, .1f); return true;
            case "Kit_SlideTube": centre = new Vector3(0f, -.8f, 1.6f); size = new Vector3(1.1f, 2.7f, 4.6f); return true;
            default: centre = new Vector3(0f, .25f, 0f); return true;
        }
    }

    // The eye sees the face: no wall edge crossed on the way (a step of 0.2 m), no stand-in box on the line.
    static bool Sees(FrontRoomsPoolPlan plan, Vector3 eye, Vector3 face, List<Bounds> boxes)
    {
        var cs = MapGrid.CellSize;
        var steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(new Vector2(eye.x, eye.z), new Vector2(face.x, face.z)) / .2f));
        var prev = new GridCoord(Mathf.FloorToInt(eye.x / cs), Mathf.FloorToInt(eye.z / cs));
        for (var k = 1; k <= steps; k++)
        {
            var p = Vector3.Lerp(eye, face, k / (float)steps);
            var c = new GridCoord(Mathf.FloorToInt(p.x / cs), Mathf.FloorToInt(p.z / cs));
            if (c != prev)
            {
                var ok = c.x == prev.x || c.y == prev.y
                    ? plan.EdgeOf(prev, c) != EdgeKind.Wall
                    : (plan.EdgeOf(prev, new GridCoord(c.x, prev.y)) != EdgeKind.Wall && plan.EdgeOf(new GridCoord(c.x, prev.y), c) != EdgeKind.Wall)
                      || (plan.EdgeOf(prev, new GridCoord(prev.x, c.y)) != EdgeKind.Wall && plan.EdgeOf(new GridCoord(prev.x, c.y), c) != EdgeKind.Wall);
                if (!ok) return false;
                prev = c;
            }
        }
        var ray = new Ray(eye, face - eye);
        var length = Vector3.Distance(eye, face) - .05f;
        foreach (var box in boxes)
            if (box.IntersectRay(ray, out var at) && at < length) return false;
        return true;
    }

    static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        var t = ab.sqrMagnitude > 1e-8f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
        return Vector2.Distance(p, a + ab * t);
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
