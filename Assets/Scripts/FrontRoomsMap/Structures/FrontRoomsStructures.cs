using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Scene 4, Assets/Scenes/FrontRoomsStructures.unity (RULES 5b, Red's words of 2026-11-03 quoted there): every new
/// structure, walkable at the game's size, for Red to try and then say what goes into the game. The scene's one bootstrap. Nothing in the game scene, the Level
/// Designer or the shipped build refers to it, and the scene is in no build list.
///
/// Play builds the hub (a Level 0 hall with one pad per district) and the walker; each district builds on its first
/// visit, in its own try/catch (a failure logs once and shows on its hub placard; the rest run). Only the hub and the
/// current district are awake, except the pool route's map world, which keeps streaming round its parked anchor.
/// The level profile is a scene-local copy (never saved, never the asset): poolFamily on, no revisit shift while Red looks,
/// doors need no keys, build radius 2. Site: every root on a 192 m multiple (surfaces are projected from world 0), districts
/// 384 m apart on the row z = -768 (SP1 and SP5 carry 70's 1.5 m x offset), the pool route at the origin as in the game.
/// Keys (Keyboard; the walker's game bindings are untouched): 1-6 SP1-SP6, 7 the pool route P, 8 the kit row, 9 SP7 (71
/// section 10), 0 the hub (E on a pad does the same); R back to the current stop; [ ] previous and next stop; L the
/// district's light key (alias N, T); F flume water (SP7: Reduce Flashing); V variant; B, Y, O, H SP7's body, head-back,
/// entrance and red-tube keys; C the Relay's 2.05 m body; M metric overlays; G the guide panel.
/// Red picks by telling the coordinator the question's code (for example "SP1-2"); there is no keep or cut switch in the scene.
/// </summary>
[DisallowMultipleComponent]
public sealed class FrontRoomsStructures : MonoBehaviour
{
    [SerializeField, Tooltip("The level's numbers (FrontRoomsLevel0.asset). A scene-local copy is made at Play; the asset is never changed.")]
    FrontRoomsLevelProfile levelProfile;
    [SerializeField, Tooltip("LS_PoolTile150_A: 150 mm porcelain tile, 2048 px = 1.5 m (sRGB).")] Texture2D tileBase;
    [SerializeField, Tooltip("LS_PoolTile150_N: its normal map.")] Texture2D tileNormal;
    [SerializeField, Tooltip("LS_PoolTile150_S: its mask (R smoothness, G cavity; linear).")] Texture2D tileMask;

    public const string Log = "[FrontRooms] STRUCTURES · ";
    public const float RowZ = -768f;

    readonly List<FrontRoomsStructuresDistrict> districts = new List<FrontRoomsStructuresDistrict>();
    FrontRoomsStructuresDistrict current, hub;
    FrontRoomsStructuresWalker walker;
    FrontRoomsStructuresLook look;
    FrontRoomsLevelProfile profileCopy;
    bool started, guide = true, metrics;
    string flash;
    float flashUntil;
    GUIStyle titleStyle, bodyStyle, smallStyle, centreStyle;
    static readonly Color Accent = new Color(.957f, .875f, .231f);
    static readonly Color Paper = new Color(.957f, .945f, .91f);
    static readonly Color Muted = new Color(.74f, .73f, .69f);

    public FrontRoomsStructuresWalker Walker => walker;
    public FrontRoomsStructuresLook Look => look;
    /// <summary>The scene-local profile copy (poolFamily on). Never the asset.</summary>
    public FrontRoomsLevelProfile Profile => profileCopy;
    public FrontRoomsStructuresDistrict Current => current;
    public IReadOnlyList<FrontRoomsStructuresDistrict> Districts => districts;
    public bool Metrics => metrics;

    void Start()
    {
        if (!Application.isPlaying) return;
        try { Begin(); }
        catch (Exception e)
        {
            Debug.LogError(Log + "the scene could not start: " + e);
        }
    }

    void Begin()
    {
        FrontRoomsStructuresSigns.Pads.Clear();
        var source = levelProfile != null ? levelProfile : FrontRoomsLevelProfile.Default;
        if (levelProfile == null) FrontRoomsPoolMaterials.LogOnce("profile", "no level profile on the scene object: the code defaults are copied.");
        profileCopy = source != null ? Instantiate(source) : ScriptableObject.CreateInstance<FrontRoomsLevelProfile>();
        profileCopy.name = "Structures scene profile (copy, never saved)";
        profileCopy.hideFlags = HideFlags.DontSave;
        profileCopy.poolFamily = true;
        profileCopy.shiftAfterSeconds = 1e6f;
        profileCopy.doorsNeedKeys = false;
        profileCopy.buildRadius = 2;

        FrontRoomsPoolMaterials.SetTileSet(tileBase, tileNormal, tileMask);
        look = FrontRoomsStructuresLook.Create(transform);
        CreateDistricts();
        hub = districts[0];
        walker = FrontRoomsStructuresWalker.Create(this, hub.Site + new Vector3(0f, 0f, 1.5f), 0f);
        started = true;
        Jump(hub, 0);
        Debug.Log(Log + "ready: hub built; districts build on their first visit. Keys: 1-6 SP1-SP6, 7 pool route, 8 kit row, 9 SP7, 0 hub, G guide.");
    }

    /// <summary>
    /// The districts, in key order. The pool route (7) is the map's own code with the pool switched on in this scene's
    /// profile; SP1-SP6 and the kit row are the Spaces stage's (research/level_space/70): until they land, their pads say so.
    /// </summary>
    void CreateDistricts()
    {
        districts.Clear();
        districts.Add(new FrontRoomsStructuresHub(new Vector3(0f, 0f, RowZ)));
        // Each district by its class name (the Spaces stage's files); a pending pad where a class is not in this build.
        districts.Add(Made("FrontRoomsStructuresSlide", new FrontRoomsStructuresPending(1, "SP1", "SP1 · 滑梯：瓷砖墙在下面托着（你选的 A）", "泳池骨架里的滑梯 A，可以滑一次。", new Vector3(-1152f + 1.5f, 0f, RowZ))));
        districts.Add(Made("FrontRoomsStructuresWell", new FrontRoomsStructuresPending(2, "SP2", "SP2 · 井：穿过整栋楼的一口竖井", "四层叠起来的竖井，从 Level 0 往下看到泳池。", new Vector3(-768f, 0f, RowZ))));
        districts.Add(Made("FrontRoomsStructuresLevel0", new FrontRoomsStructuresPending(3, "SP3", "SP3 · Level 0：原样的场地和几处断点", "Level 0 的走廊，和几处可以放进去的变化。", new Vector3(-384f, 0f, RowZ))));
        districts.Add(Made("FrontRoomsStructuresOffice", new FrontRoomsStructuresPending(4, "SP4", "SP4 · 办公层：三层中庭，底下是下沉工位区", "三层办公中庭和下沉工位区。", new Vector3(384f, 0f, RowZ))));
        districts.Add(Made("FrontRoomsStructuresRun", new FrontRoomsStructuresPending(5, "SP5", "SP5 · Run!：一路下坡到卸货间", "Run! 的坡道走廊和卸货间。", new Vector3(768f + 1.5f, 0f, RowZ))));
        districts.Add(Made("FrontRoomsStructuresPoolTerrain", new FrontRoomsStructuresPending(6, "SP6", "SP6 · 泳池层：深处的地形和大螺旋楼梯", "泳池层深处的地形和大螺旋楼梯。", new Vector3(1152f, 0f, RowZ))));
        districts.Add(MakePoolRoute());
        districts.Add(Made("FrontRoomsStructuresKitRow", new FrontRoomsStructuresPending(8, "KIT", "8 · 模组件一字排开", "滑梯、楼梯、拱墙这些模组件，一件一件看。", new Vector3(1536f, 0f, RowZ))));
        // SP7 (71 section 10, agreed 11-03): beside SP5 in 70's order, on the row after the kit row (SP5's 1.5 m x offset).
        districts.Add(Made("FrontRoomsStructuresRunRecede", new FrontRoomsStructuresPending(9, "SP7", "SP7 · Run!：会往后退的门", "Run! B 的走廊做到 14 格，尽头的墙和门会往后退。", new Vector3(1920f + 1.5f, 0f, RowZ))));
    }

    static FrontRoomsStructuresDistrict Made(string typeName, FrontRoomsStructuresPending fallback)
    {
        var made = FrontRoomsStructuresDistrict.Make(typeName, fallback.Site);
        return made ?? fallback;
    }

    // The pool route (key 7): the map's own code (FrontRoomsStructuresPoolRoute) once it is staged; a pending pad until then.
    FrontRoomsStructuresDistrict MakePoolRoute()
    {
        var made = FrontRoomsStructuresDistrict.Make("FrontRoomsStructuresPoolRoute", Vector3.zero);
        return made ?? new FrontRoomsStructuresPending(7, "P", "P · 泳池俱乐部和泳池层（游戏自己的地图代码）", "从 B2 的走廊走进泳池俱乐部，再走到更深处。", Vector3.zero);
    }

    // ---------------------------------------------------------------- jumps

    /// <summary>The district with this key, or null.</summary>
    public FrontRoomsStructuresDistrict District(int key)
    {
        foreach (var d in districts) if (d.Key == key) return d;
        return null;
    }

    /// <summary>Jump to a district's stop (its arrival point unless a stop index is given): build it first if it is not built yet.</summary>
    public void Jump(FrontRoomsStructuresDistrict d, int stop = -1)
    {
        if (d == null || walker == null) return;
        if (!d.Available)
        {
            Flash(d.Code + " 还没有搭建（下一阶段）。");
            return;
        }
        if (!d.Built && !d.Failed) BuildDistrict(d);
        if (d.Failed)
        {
            Flash(d.Code + " 构建失败：" + d.Error);
            return;
        }
        Activate(d);
        if (d.Stops.Count == 0) d.Stops.Add(new FrontRoomsStructuresStop("到达点", d.Site, 0f, 0f));
        d.StopIndex = stop >= 0 ? Mathf.Clamp(stop, 0, d.Stops.Count - 1) : Mathf.Clamp(d.StopIndex, 0, d.Stops.Count - 1);
        var s = d.Stops[d.StopIndex];
        walker.Teleport(s.position, s.yaw, s.pitch);
        walker.SetFar(d.Far);
        d.Arrived(this, d.StopIndex);
        look.Want(d.PresetAt(s.position));
    }

    void BuildDistrict(FrontRoomsStructuresDistrict d)
    {
        var t = Time.realtimeSinceStartup;
        try
        {
            var root = new GameObject("District " + d.Code);
            root.transform.SetParent(transform, false);
            root.transform.position = d.Site;
            d.Root = root.transform;
            d.Build(this);
            d.Built = true;
            d.Cost = FrontRoomsStructuresDistrict.CostLine(d, look, Time.realtimeSinceStartup - t);
            Debug.Log(Log + "built " + d.Code + ": " + d.Cost);
        }
        catch (Exception e)
        {
            d.Failed = true;
            d.Error = e.GetType().Name + ": " + e.Message;
            Debug.LogError(Log + "district " + d.Code + " failed to build and is left out: " + e);
            if (d.Root != null) d.Root.gameObject.SetActive(false);
        }
    }

    void Activate(FrontRoomsStructuresDistrict d)
    {
        if (current == d) return;
        var previous = current;
        current = d;
        if (previous != null)
        {
            try { previous.Leave(this); } catch (Exception e) { Debug.LogError(Log + previous.Code + " leave: " + e); }
            if (previous != hub && !previous.KeepActive && previous.Root != null) previous.Root.gameObject.SetActive(false);
        }
        if (d.Root != null) d.Root.gameObject.SetActive(true);
        try { d.Enter(this); } catch (Exception e) { Debug.LogError(Log + d.Code + " enter: " + e); }
    }

    public void Flash(string message)
    {
        flash = message;
        flashUntil = Time.time + 3f;
    }

    // ---------------------------------------------------------------- the walker's use (pads, then the district)

    public string Describe(Collider c, out bool hold)
    {
        hold = false;
        var pad = FrontRoomsStructuresSigns.PadOf(c);
        if (pad != null) return "E  ·  " + pad.label;
        if (current == null) return null;
        try { return current.Describe(c, out hold); } catch (Exception e) { FrontRoomsPoolMaterials.LogOnce("describe " + current.Code, current.Code + " describe: " + e.Message); return null; }
    }

    public string PadPrompt(Vector3 feet)
    {
        var pad = FrontRoomsStructuresSigns.PadUnder(feet);
        if (pad != null) return "E  ·  " + pad.label;
        if (current == null) return null;
        try { return current.FeetPrompt(this, feet); }
        catch (Exception e) { FrontRoomsPoolMaterials.LogOnce("feet " + current.Code, current.Code + " feet prompt: " + e.Message); return null; }
    }

    public void UsePadUnder(Vector3 feet)
    {
        var pad = FrontRoomsStructuresSigns.PadUnder(feet);
        if (pad != null) { UsePad(pad); return; }
        if (current == null) return;
        try { current.UseAtFeet(this, feet); }
        catch (Exception e) { FrontRoomsPoolMaterials.LogOnce("feet use " + current.Code, current.Code + " use at feet: " + e.Message); }
    }

    void UsePad(FrontRoomsStructuresSigns.Pad pad)
    {
        if (pad == null) return;
        Jump(District(pad.key));
    }

    public void Use(Collider c)
    {
        var pad = FrontRoomsStructuresSigns.PadOf(c);
        if (pad != null) { UsePad(pad); return; }
        try { current?.Use(c); } catch (Exception e) { FrontRoomsPoolMaterials.LogOnce("use " + current.Code, current.Code + " use: " + e.Message); }
    }

    public bool Hold(Collider c, Vector3 point, float dt, out float progress)
    {
        progress = 0f;
        try { return current != null && current.Hold(c, point, dt, out progress); }
        catch (Exception e) { FrontRoomsPoolMaterials.LogOnce("hold " + current.Code, current.Code + " hold: " + e.Message); return false; }
    }

    public void ReleaseHold(Collider c)
    {
        try { current?.ReleaseHold(c); } catch (Exception e) { FrontRoomsPoolMaterials.LogOnce("release " + current.Code, current.Code + " release: " + e.Message); }
    }

    // ---------------------------------------------------------------- the frame

    void Update()
    {
        if (!started || walker == null || current == null) return;
        var dt = Time.deltaTime;
        Keys();
        var feet = walker.transform.position;
        // Walking from one district into another (the loop, later) makes it current; a jump already did.
        if (!current.Contains(feet))
        {
            foreach (var d in districts)
                if (d != current && d.Built && !d.Failed && d.Contains(feet)) { Activate(d); break; }
        }
        // A fall: back on the arrival point at once.
        if (feet.y < current.LowestFloor - 3f) Jump(current, 0);
        foreach (var d in districts)
        {
            if (!d.Built || d.Failed) continue;
            if (d != current && d != hub && !d.KeepActive) continue;
            try { d.Tick(this, dt, d == current); }
            catch (Exception e) { FrontRoomsPoolMaterials.LogOnce("tick " + d.Code, d.Code + " tick: " + e.Message); }
        }
        look.Want(current.PresetAt(feet));
        look.Tick(walker.EyePosition, current.Key, dt);
    }

    void Keys()
    {
        var k = Keyboard.current;
        if (k == null) return;
        for (var n = 0; n <= 9; n++)
        {
            if (!DigitDown(k, n)) continue;
            var d = District(n);
            if (d != null) Jump(d, 0);
            return;
        }
        if (k.rKey.wasPressedThisFrame) { Jump(current, current.StopIndex); return; }
        if (k.leftBracketKey.wasPressedThisFrame) { Step(-1); return; }
        if (k.rightBracketKey.wasPressedThisFrame) { Step(1); return; }
        if (k.gKey.wasPressedThisFrame) guide = !guide;
        if (k.cKey.wasPressedThisFrame)
        {
            walker.SetRelayBody(!walker.RelayBody);
            Flash(walker.RelayBody ? "Relay 体型（2.05 米，半径 0.3）：只走 Relay 能走的路。" : "回到玩家体型（1.75 米）。");
            foreach (var d in districts) if (d.Built && !d.Failed) d.RelayBody(this, walker.RelayBody);
        }
        if (k.mKey.wasPressedThisFrame)
        {
            metrics = !metrics;
            foreach (var d in districts) if (d.Built && !d.Failed) d.ShowMetrics(this, metrics);
            Flash(metrics ? "尺寸标记：开" : "尺寸标记：关");
        }
        Key[] districtKeys = { Key.L, Key.N, Key.T, Key.F, Key.V, Key.B, Key.Y, Key.O, Key.H };
        foreach (var key in districtKeys)
        {
            if (!k[key].wasPressedThisFrame) continue;
            string message = null;
            var handled = false;
            try { handled = current.KeyPressed(this, key, out message); }
            catch (Exception e) { FrontRoomsPoolMaterials.LogOnce("key " + current.Code + key, current.Code + " key " + key + ": " + e.Message); }
            if (handled && !string.IsNullOrEmpty(message)) Flash(message);
            else if (!handled) Flash(current.Code + " 没有这个按键。");
        }
    }

    static bool DigitDown(Keyboard k, int n)
    {
        switch (n)
        {
            case 0: return k.digit0Key.wasPressedThisFrame || k.numpad0Key.wasPressedThisFrame;
            case 1: return k.digit1Key.wasPressedThisFrame || k.numpad1Key.wasPressedThisFrame;
            case 2: return k.digit2Key.wasPressedThisFrame || k.numpad2Key.wasPressedThisFrame;
            case 3: return k.digit3Key.wasPressedThisFrame || k.numpad3Key.wasPressedThisFrame;
            case 4: return k.digit4Key.wasPressedThisFrame || k.numpad4Key.wasPressedThisFrame;
            case 5: return k.digit5Key.wasPressedThisFrame || k.numpad5Key.wasPressedThisFrame;
            case 6: return k.digit6Key.wasPressedThisFrame || k.numpad6Key.wasPressedThisFrame;
            case 7: return k.digit7Key.wasPressedThisFrame || k.numpad7Key.wasPressedThisFrame;
            case 8: return k.digit8Key.wasPressedThisFrame || k.numpad8Key.wasPressedThisFrame;
            case 9: return k.digit9Key.wasPressedThisFrame || k.numpad9Key.wasPressedThisFrame;
            default: return false;
        }
    }

    void Step(int by)
    {
        var d = current;
        if (d == null || d.Stops.Count == 0) return;
        var i = ((d.StopIndex + by) % d.Stops.Count + d.Stops.Count) % d.Stops.Count;
        Jump(d, i);
        Flash(d.Code + " · " + (i + 1) + "/" + d.Stops.Count + " · " + d.Stops[i].name);
    }

    void OnDestroy()
    {
        foreach (var d in districts)
        {
            try { d.Release(); } catch (Exception e) { Debug.LogError(Log + d.Code + " release: " + e); }
        }
        FrontRoomsStructuresSigns.Pads.Clear();
        FrontRoomsPoolMaterials.Release();
        if (profileCopy != null) Destroy(profileCopy);
        if (Cursor.lockState == CursorLockMode.Locked) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    }

    // ---------------------------------------------------------------- HUD and the guide (G)

    void OnGUI()
    {
        if (!started || walker == null || current == null) return;
        if (titleStyle == null)
        {
            var body = FrontRoomsStructuresSigns.BodyFontAsset;
            var title = FrontRoomsStructuresSigns.TitleFontAsset;
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Normal, wordWrap = false };
            bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
            smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, wordWrap = false };
            centreStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            if (title != null) titleStyle.font = title;
            if (body != null) { bodyStyle.font = body; smallStyle.font = body; centreStyle.font = body; }
        }
        var d = current;
        Box(new Rect(24f, 22f, 4f, 50f), Accent);
        Text(new Rect(38f, 16f, 1400f, 34f), d.Title, titleStyle, Paper);
        var stop = d.Stops.Count > 0 ? d.Stops[Mathf.Clamp(d.StopIndex, 0, d.Stops.Count - 1)].name : "";
        Text(new Rect(38f, 52f, 1400f, 20f), "场景 4 · 新结构  ·  " + d.Code + (stop.Length > 0 ? "  ·  站点 " + (d.StopIndex + 1) + "/" + d.Stops.Count + " " + stop : "") + (walker.RelayBody ? "  ·  Relay 体型" : "") + "  ·  " + d.Status(this), smallStyle, Muted);

        var cx = Screen.width * .5f;
        var cy = Screen.height * .5f;
        Box(new Rect(cx - 2f, cy - 2f, 4f, 4f), Accent);
        if (walker.Prompt != null) Text(new Rect(cx - 320f, cy + 22f, 640f, 44f), walker.Prompt, centreStyle, Paper);
        if (walker.HoldProgress > 0f)
        {
            Box(new Rect(cx - 60f, cy + 66f, 120f, 4f), new Color(1f, 1f, 1f, .25f));
            Box(new Rect(cx - 60f, cy + 66f, 120f * walker.HoldProgress, 4f), Accent);
        }
        if (walker.Stamina01 < .999f)
            for (var i = 0; i < 5; i++)
                Box(new Rect(cx - 72f + i * 30f, cy + 92f, 24f, 6f), walker.Stamina01 >= (i + .5f) / 5f ? Accent : new Color(1f, 1f, 1f, .25f));
        if (flash != null && Time.time < flashUntil) Text(new Rect(cx - 380f, cy + 112f, 760f, 48f), flash, centreStyle, Accent);

        Text(new Rect(24f, Screen.height - 30f, 1800f, 20f),
            "点击：看  ·  WASD 走  ·  左 Ctrl 跑  ·  Shift 慢走  ·  E 使用  ·  1-6 区域  ·  7 泳池路线  ·  8 模组件  ·  9 SP7  ·  0 大厅  ·  R 回到站点  ·  [ ] 换站点  ·  L 灯  ·  V 变体  ·  C Relay 体型  ·  M 尺寸  ·  G 说明  ·  Esc 鼠标",
            smallStyle, Muted);

        if (!guide) return;
        var sb = new StringBuilder();
        try { d.Guide(this, sb); } catch (Exception e) { sb.AppendLine("（说明出错：" + e.Message + "）"); }
        if (d.Cost.Length > 0) sb.AppendLine().Append("成本（灰盒）：").Append(d.Cost);
        var text = sb.ToString();
        var w = Mathf.Min(520f, Screen.width * .36f);
        var h = Mathf.Min(Screen.height - 140f, bodyStyle.CalcHeight(new GUIContent(text), w - 24f) + 24f);
        var r = new Rect(Screen.width - w - 20f, 20f, w, h);
        Box(r, new Color(0f, 0f, 0f, .55f));
        Text(new Rect(r.x + 12f, r.y + 12f, w - 24f, h - 24f), text, bodyStyle, Paper);
    }

    static void Box(Rect r, Color c)
    {
        var old = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = old;
    }

    static void Text(Rect r, string text, GUIStyle style, Color c)
    {
        var old = GUI.color;
        GUI.color = c;
        GUI.Label(r, text, style);
        GUI.color = old;
    }
}

/// <summary>A stop in a district: a floor point (world), a heading and a pitch; [ ] step through them, R comes back to the current one.</summary>
public struct FrontRoomsStructuresStop
{
    public string name;
    public Vector3 position;
    public float yaw, pitch;

    public FrontRoomsStructuresStop(string name, Vector3 position, float yaw, float pitch)
    {
        this.name = name;
        this.position = position;
        this.yaw = yaw;
        this.pitch = pitch;
    }

    /// <summary>A stop at a floor point facing a point (level heading; pitch toward it, clamped to 30 degrees).</summary>
    public static FrontRoomsStructuresStop Facing(string name, Vector3 floor, Vector3 lookAt)
    {
        var d = lookAt - (floor + Vector3.up * FrontRoomsStructuresWalker.Eye);
        var flat = new Vector2(d.x, d.z);
        var yaw = flat.sqrMagnitude > 1e-6f ? Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg : 0f;
        var pitch = flat.magnitude > 1e-3f ? Mathf.Clamp(-Mathf.Atan2(d.y, flat.magnitude) * Mathf.Rad2Deg, -30f, 30f) : 0f;
        return new FrontRoomsStructuresStop(name, floor, yaw, pitch);
    }
}

/// <summary>
/// One district of scene 4: a table row with its key, code, Chinese title and one line for its pad, its site (world, on the
/// 192 m lattice), stops, look and walk rules. Build runs once, in the bootstrap's try/catch; everything it makes goes under
/// Root. Districts are the Spaces stage's table: SP1-SP6 and the kit row replace their pending rows by their own classes.
/// </summary>
public abstract class FrontRoomsStructuresDistrict
{
    public readonly int Key;
    public readonly string Code, Title, Line;
    public readonly Vector3 Site;
    public Transform Root;
    public bool Built, Failed;
    public string Error = "", Cost = "";
    /// <summary>The district's box (world): its bounds plus 2 m. Inside it, its look holds.</summary>
    public Bounds Box;
    /// <summary>The district's lowest floor (world y): a fall 3 m under it puts the walker back on its arrival point.</summary>
    public float LowestFloor;
    /// <summary>The camera's far plane here (the game's 80 m unless the main view needs more).</summary>
    public float Far = 80f;
    public readonly List<FrontRoomsStructuresStop> Stops = new List<FrontRoomsStructuresStop>();
    public int StopIndex;
    /// <summary>Meshes and materials the district made: freed by Release.</summary>
    public readonly List<Mesh> Meshes = new List<Mesh>();

    protected FrontRoomsStructuresDistrict(int key, string code, string title, string line, Vector3 site)
    {
        Key = key;
        Code = code;
        Title = title;
        Line = line;
        Site = site;
        Box = new Bounds(site, Vector3.zero);
        LowestFloor = site.y;
    }

    /// <summary>A district class by name with a (Vector3 site) constructor, or null when it is not in this build (a later stage's).</summary>
    public static FrontRoomsStructuresDistrict Make(string typeName, Vector3 site)
    {
        try
        {
            var type = Type.GetType(typeName);
            if (type == null || !typeof(FrontRoomsStructuresDistrict).IsAssignableFrom(type)) return null;
            return Activator.CreateInstance(type, site) as FrontRoomsStructuresDistrict;
        }
        catch (Exception e)
        {
            FrontRoomsPoolMaterials.LogOnce("make " + typeName, typeName + " could not be made: " + e.Message);
            return null;
        }
    }

    /// <summary>False for a district whose builder is not staged yet: its pad says so and a jump is refused.</summary>
    public virtual bool Available => true;
    /// <summary>True keeps the district's root active while another district is current (the pool route's map world).</summary>
    public virtual bool KeepActive => false;

    public abstract void Build(FrontRoomsStructures host);
    public virtual void Enter(FrontRoomsStructures host) { }
    public virtual void Leave(FrontRoomsStructures host) { }
    /// <summary>The walker landed on stop i (a jump, R, [ ]).</summary>
    public virtual void Arrived(FrontRoomsStructures host, int stop) { }
    public virtual void Tick(FrontRoomsStructures host, float dt, bool isCurrent) { }
    public virtual bool Contains(Vector3 world) => Box.size.sqrMagnitude > 0f && Box.Contains(world);
    public virtual FrontRoomsStructuresLook.Preset PresetAt(Vector3 world) => FrontRoomsStructuresLook.Preset.Level0;
    /// <summary>L (N, T), F, V, B, Y, O, H: true when the district has the key; message is flashed.</summary>
    public virtual bool KeyPressed(FrontRoomsStructures host, Key key, out string message) { message = null; return false; }
    public virtual void RelayBody(FrontRoomsStructures host, bool on) { }
    public virtual void ShowMetrics(FrontRoomsStructures host, bool on) { }
    public virtual string Describe(Collider c, out bool hold) { hold = false; return null; }
    /// <summary>A prompt for where the walker stands (SP1's tub), when nothing in the crosshair has one.</summary>
    public virtual string FeetPrompt(FrontRoomsStructures host, Vector3 feet) => null;
    /// <summary>E with the feet prompt showing.</summary>
    public virtual void UseAtFeet(FrontRoomsStructures host, Vector3 feet) { }
    public virtual void Use(Collider c) { }
    public virtual bool Hold(Collider c, Vector3 point, float dt, out float progress) { progress = 0f; return false; }
    public virtual void ReleaseHold(Collider c) { }
    /// <summary>One line for the HUD (the current stop's state, a variant, a light key).</summary>
    public virtual string Status(FrontRoomsStructures host) => "";
    /// <summary>The G panel: what it is, keys, the numbered questions (Red answers by code).</summary>
    public virtual void Guide(FrontRoomsStructures host, StringBuilder sb) { sb.AppendLine(Title).AppendLine(Line); }

    public virtual void Release()
    {
        foreach (var m in Meshes) if (m != null) UnityEngine.Object.Destroy(m);
        Meshes.Clear();
    }

    /// <summary>The district's cost line (VISUAL_STANDARD 1.4, F36): measured on what it built; desktop Editor only.</summary>
    public static string CostLine(FrontRoomsStructuresDistrict d, FrontRoomsStructuresLook look, float seconds)
    {
        if (d.Root == null) return "";
        long tris = 0;
        var renderers = 0;
        var materials = new HashSet<Material>();
        var textures = new HashSet<Texture>();
        var tile = FrontRoomsPoolMaterials.HasTileSet ? FrontRoomsPoolMaterials.Tile : null;
        var tileArea = 0f;
        foreach (var r in d.Root.GetComponentsInChildren<MeshRenderer>(true))
        {
            renderers++;
            var mf = r.GetComponent<MeshFilter>();
            var mesh = mf != null ? mf.sharedMesh : null;
            if (mesh != null && mesh.isReadable) tris += mesh.triangles.LongLength / 3;
            foreach (var m in r.sharedMaterials)
            {
                if (m == null || !materials.Add(m)) continue;
                foreach (var id in m.GetTexturePropertyNameIDs())
                {
                    var t = m.GetTexture(id);
                    if (t != null) textures.Add(t);
                }
            }
            if (tile != null && r.sharedMaterial == tile && mesh != null && mesh.isReadable) tileArea += Area(mesh, r.transform);
        }
        long bytes = 0;
        foreach (var t in textures) bytes += UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(t);
        var lights = 0;
        var shadowed = 0;
        foreach (var l in d.Root.GetComponentsInChildren<Light>(true))
        {
            lights++;
            if (l.shadows != LightShadows.None) shadowed++;
        }
        if (look != null)
        {
            look.CountLamps(d.Key, out var own, out var mayShadow);
            if (own > 0) shadowed = Mathf.Max(shadowed, mayShadow);
        }
        return tris + " tris (greybox; no LOD), " + renderers + " renderers, " + materials.Count + " materials, " + lights + " realtime lights (" + shadowed
            + " may shadow), textures " + (bytes / 1048576f).ToString("0.0") + " MB, pool tile " + tileArea.ToString("0") + " m2, built in " + (seconds * 1000f).ToString("0")
            + " ms; desktop Editor only, phone tier not built, frame time not measured";
    }

    static float Area(Mesh mesh, Transform t)
    {
        var v = mesh.vertices;
        var tr = mesh.triangles;
        var s = t.lossyScale;
        var a = 0f;
        for (var i = 0; i + 2 < tr.Length; i += 3)
        {
            var p0 = Vector3.Scale(v[tr[i]], s);
            var p1 = Vector3.Scale(v[tr[i + 1]], s);
            var p2 = Vector3.Scale(v[tr[i + 2]], s);
            a += Vector3.Cross(p1 - p0, p2 - p0).magnitude * .5f;
        }
        return a;
    }
}

/// <summary>A district whose builder is a later stage's (the Spaces stage, research/level_space/70): its pad says so.</summary>
public sealed class FrontRoomsStructuresPending : FrontRoomsStructuresDistrict
{
    public FrontRoomsStructuresPending(int key, string code, string title, string line, Vector3 site) : base(key, code, title, line, site) { }
    public override bool Available => false;
    public override void Build(FrontRoomsStructures host) { }
}
