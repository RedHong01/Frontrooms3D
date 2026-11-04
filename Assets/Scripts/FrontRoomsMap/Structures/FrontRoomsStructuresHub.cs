using System.Text;
using FrontRooms.Map;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Scene 4's hub (key 0; PLAN 2.4, showcase_scenes/10 section 4): a Level 0 hall in the game's own finish (L0_Wallpaper,
/// L0_Carpet, L0_Ceiling on its 0.6 m grid, the L03 troffer at the game's lens drive, one per cell), 8 x 5 cells at Standard
/// 2.9 m. The walker spawns on its south side facing an arc of nine pads, one per district in key order (SP1-SP6, the
/// pool route, the kit row, SP7), each under a placard with its key, name and one line on what to try. Beside the spawn: the
/// keys, and what is not in this scene. No picture: no render of the new districts exists yet.
/// </summary>
public sealed class FrontRoomsStructuresHub : FrontRoomsStructuresDistrict
{
    const float Width = 24f, Depth = 15f, Ceiling = ModuleUnits.StandardCeiling;
    static readonly Vector3 Spawn = new Vector3(0f, 0f, 1.5f);

    public FrontRoomsStructuresHub(Vector3 site) : base(0, "HUB", "大厅 · 场景 4：所有新结构", "每个编号的踏板通往一个区域；走上去按 E，或者直接按数字键。", site) { }

    public override void Build(FrontRoomsStructures host)
    {
        var root = Root;
        var mesh = new FrontRoomsStructuresMesh(root) { Ceiling = Site.y + Ceiling };
        var wall = FrontRoomsPoolMaterials.Surface("L0_Wallpaper", new Color(.80f, .74f, .48f));
        var floor = FrontRoomsPoolMaterials.Surface("L0_Carpet", new Color(.55f, .49f, .30f));
        var ceiling = FrontRoomsPoolMaterials.Surface("L0_Ceiling", new Color(.86f, .83f, .70f));
        var hx = Width * .5f;
        var t = ModuleUnits.WallThickness;
        mesh.Box(new Vector3(0f, -ModuleUnits.FloorSlab * .5f, Depth * .5f), new Vector3(Width + t, ModuleUnits.FloorSlab, Depth + t), floor);
        mesh.Box(new Vector3(0f, Ceiling + ModuleUnits.CeilingSlab * .5f, Depth * .5f), new Vector3(Width + t, ModuleUnits.CeilingSlab, Depth + t), ceiling);
        mesh.Box(new Vector3(-hx, Ceiling * .5f, Depth * .5f), new Vector3(t, Ceiling, Depth + t), wall);
        mesh.Box(new Vector3(hx, Ceiling * .5f, Depth * .5f), new Vector3(t, Ceiling, Depth + t), wall);
        mesh.Box(new Vector3(0f, Ceiling * .5f, 0f), new Vector3(Width + t, Ceiling, t), wall);
        mesh.Box(new Vector3(0f, Ceiling * .5f, Depth), new Vector3(Width + t, Ceiling, t), wall);
        mesh.Finish("Hub", Meshes);

        // One L03 troffer per cell, as the map hangs them (its lens 0.3 m north of the cell centre, the spot 0.06 m under it).
        for (var j = 0; j < 5; j++)
        for (var i = 0; i < 8; i++)
        {
            var c = new Vector3(-hx + (i + .5f) * MapGrid.CellSize, Ceiling, (j + .5f) * MapGrid.CellSize + ModuleUnits.TrofferOffsetZ);
            Troffer(host, root, c, Key, false);
        }

        // The pads: an arc of radius 7 round the spawn, from 70 degrees west to 70 degrees east, in key order.
        var keys = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        for (var k = 0; k < keys.Length; k++)
        {
            var a = Mathf.Deg2Rad * (-70f + 140f * k / (keys.Length - 1));
            var dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
            var padLocal = Spawn + dir * 7f;
            var d = host.District(keys[k]);
            if (d == null) continue;
            var ready = d.Available;
            var label = (d.Key == 8 ? "8" : d.Key.ToString()) + "  ·  " + d.Title + (ready ? "" : "（尚未搭建）");
            FrontRoomsStructuresSigns.MakePad(root, root.TransformPoint(padLocal), d.Key, label, ready ? new Color(.957f, .875f, .231f) * 1.6f : new Color(.35f, .35f, .35f));
            var body = d.Line + (ready ? "\n走上踏板按 E，或按 " + d.Key + "。" : "\n这一区还没有搭建（下一阶段：Spaces）。按 " + d.Key + " 会提示。");
            FrontRoomsStructuresSigns.Placard(root, root.TransformPoint(padLocal + dir * .9f + Vector3.up * 1.5f), root.TransformPoint(Spawn), d.Key + " · " + d.Code, d.Title + "\n" + body, Site.y);
        }

        FrontRoomsStructuresSigns.Placard(root, root.TransformPoint(new Vector3(3.6f, 1.5f, 1.6f)), root.TransformPoint(Spawn), "按键",
            "数字键 1-6：六个区域（SP1-SP6）。7：泳池路线（游戏自己的地图代码）。8：模组件。9：SP7 会往后退的门（Run!）。0：回到大厅。\n"
            + "R：回到这一区的当前站点。[ 和 ]：上一个、下一个站点。L：这一区的灯光（夜景、停电）。V：变体。F：滑道里的水。\n"
            + "SP7 里另有 B、H、O、Y 四个键，见那里的牌子。C：换成 Relay 的体型（2.05 米），检查 Relay 能走的路。M：尺寸标记。G：说明面板。\n"
            + "走路、跑、慢走、E 都和游戏一样：WASD，左 Ctrl 跑，Shift 慢走。点击屏幕控制视角，Esc 放开鼠标。\n"
            + "要你决定的问题都有编号（比如 SP1-2），在对话里按编号告诉总控就行。", Site.y);
        FrontRoomsStructuresSigns.Placard(root, root.TransformPoint(new Vector3(-3.6f, 1.5f, 1.6f)), root.TransformPoint(Spawn), "这里没有的东西",
            "V7（需要地图的回访变化，只在深层）；Run! 的猫道 G1、G2（你已否决）；r4t 和 LS12（10-31 已撤回）；滑梯的 B、C 两种支撑（你选了 A）；"
            + "61 §5 的其他滑梯样式（只有文字）；62、63 的家具方案（不是结构）；泳池的玩家水中动作（P2）和 Relay 进泳池（P3）还没做，"
            + "所以泳池路线里没有 Relay。", Site.y);

        Box = new Bounds(Site + new Vector3(0f, 1.5f, Depth * .5f), new Vector3(Width + 4f, 7f, Depth + 4f));
        LowestFloor = Site.y;
        Stops.Clear();
        Stops.Add(FrontRoomsStructuresStop.Facing("出生点", Site + Spawn, Site + Spawn + new Vector3(0f, 1.62f, 7f)));
    }

    /// <summary>
    /// The game's L03 troffer at a ceiling point (local), its lens at the map's Level 0 drive and the map's spot under it
    /// (162 / 96 degrees, Level 0's colour and intensity, range 10, shadows by the budget). Without the kit: a flat lens.
    /// </summary>
    public static void Troffer(FrontRoomsStructures host, Transform parent, Vector3 ceilingLocal, int district, bool view)
    {
        var root = new GameObject("Fixture (L03)");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = ceilingLocal;
        var lensMaterial = FrontRoomsSurfaces.TryGet("Troffer_Lens");
        Renderer lens = null;
        var drive = Color.white * 2f;
        try
        {
            lens = FrontRoomsTrofferKit.Build(root.transform);
            if (lens != null) drive = FrontRoomsTrofferKit.LensDrive(lensMaterial);
        }
        catch (System.Exception e) { FrontRoomsPoolMaterials.LogOnce("troffer", "the troffer kit failed: " + e.Message); lens = null; }
        if (lens == null)
        {
            FrontRoomsPoolMaterials.LogOnce("troffer missing", "the L03 troffer kit is missing: a flat lens stands in.");
            var flat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flat.name = "Lens (flat stand-in)";
            Object.Destroy(flat.GetComponent<Collider>());
            flat.transform.SetParent(root.transform, false);
            flat.transform.localPosition = new Vector3(0f, -ModuleUnits.TrofferDrop, 0f);
            flat.transform.localScale = new Vector3(ModuleUnits.TrofferShort, ModuleUnits.TrofferLens, ModuleUnits.TrofferLong);
            lens = flat.GetComponent<MeshRenderer>();
            if (lensMaterial != null) lens.sharedMaterial = lensMaterial;
            lens.shadowCastingMode = ShadowCastingMode.Off;
            drive = lensMaterial != null && lensMaterial.HasProperty("_EmissionColor") ? lensMaterial.GetColor("_EmissionColor") : new Color(2.2f, 2.1f, 1.8f);
        }
        var lightGo = new GameObject("Light");
        lightGo.transform.SetParent(root.transform, false);
        lightGo.transform.localPosition = new Vector3(0f, -ModuleUnits.LampDrop, 0f);
        lightGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Spot;
        light.spotAngle = 162f;
        light.innerSpotAngle = 96f;
        light.color = FrontRoomsTransitionLightLead.Level0Color;
        light.intensity = FrontRoomsTransitionLightLead.Level0Intensity;
        light.range = 10f;
        light.shadows = LightShadows.None;
        light.shadowStrength = .92f;
        light.shadowNearPlane = .1f;
        host.Look.AddLamp(district, light, lens, drive, view);
    }

    public override FrontRoomsStructuresLook.Preset PresetAt(Vector3 world) => FrontRoomsStructuresLook.Preset.Level0;

    public override void Guide(FrontRoomsStructures host, StringBuilder sb)
    {
        sb.AppendLine("大厅 · 场景 4：所有新结构");
        sb.AppendLine("这里的每一块地方都是能走进去的空间，不是图片。走一遍，然后在对话里告诉总控哪些放进游戏。");
        sb.AppendLine();
        foreach (var d in host.Districts)
        {
            if (d == this) continue;
            sb.Append(d.Key).Append("  ").Append(d.Title);
            if (!d.Available) sb.Append("（尚未搭建）");
            else if (d.Failed) sb.Append("（构建失败：").Append(d.Error).Append("）");
            sb.AppendLine();
        }
        sb.AppendLine();
        sb.AppendLine("每个区域的问题都有编号（比如 P-3、SP1-2），按编号回答即可。G 关掉这个面板。");
    }
}
