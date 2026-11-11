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
/// entrance and red-tube keys; C the Relay's 2.05 m body; M metric overlays; G the guide panel; Q 游戏视觉's pool zone look in
/// the pool rooms of SP1, SP2's F0 and SP6 (on by default; off = the scene's own fixed trilight; FrontRoomsStructuresLook).
/// Red picks by telling the coordinator the question's code (for example "SP1-2"); there is no keep or cut switch in the scene.
/// ; 游戏视觉's structure assets (FrontRoomsStructuresAssets.New, research/structures_assets/30_plan.md: bevels by material,
/// guards, stairs and edges as the period built them, the real surfaces) or today's greybox, bit for bit; the current district is
/// rebuilt, the others on their next visit. NEW is the default. With NEW on, Red's open asks (30_plan section 5) are a key away:
/// ' the pool edge, / the tile on tight curves, , the rails' pickets, . the lamp modes (each built as recommended); none of them
/// with Shift held (Shift + . is 光影水反's lens A/B, research/light_design/10_research.md LW3).
/// The pool tile carries the glaze Red picked (GZ-C on 2026-11-04, landed A first; research/level_space/73_pool_glaze.md
/// section 3): the LS set's v2 maps, smoothness 0.87 and cavity 0.7 on the shared tile and band (Glaze), and the lamps'
/// reflection proxies with the probes captured on arrival and on each change, every lamp on (FrontRoomsStructuresLook).
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
    /// <summary>GZ-A's pool tile and band (73 section 3.2): smoothness x mask R (0.85 before; no specular AA yet, so a small
    /// step) and cavity (0.8 before). Base colour, bump and the rest stay as FrontRoomsPoolMaterials makes them.</summary>
    public const float GlazeSmoothness = .87f, GlazeCavity = .7f;

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
        // 游戏视觉's structure assets by default (structures_assets contract K1; ; flips back to today's greybox)
        FrontRoomsStructuresAssets.New = FrontRoomsStructuresAssets.StartNew;
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
        Glaze();
        // The pool tile opens on the game's default (FrontRoomsPoolTile.DefaultLook / DefaultDishTone: Red's DishDeep at 1.5, 11-05).
        look = FrontRoomsStructuresLook.Create(transform);
        CreateDistricts();
        hub = districts[0];
        walker = FrontRoomsStructuresWalker.Create(this, hub.Site + new Vector3(0f, 0f, 1.5f), 0f);
        started = true;
        Jump(hub, 0);
        Debug.Log(Log + "ready: hub built; districts build on their first visit. Keys: 1-6 SP1-SP6, 7 pool route, 8 kit row, 9 SP7, 0 hub, G guide, Q pool zone look, ; structure assets (NEW / TODAY).");
    }

    /// <summary>
    /// GZ-A's material (73 section 3.2) on the LS set's tile and aqua band (the structures scene's own runtime copies), in place,
    /// right after SetTileSet and before any district builds. Since pool_tile_real (11-05 04:23) FrontRoomsPoolMaterials.Tile and
    /// .Band hand out 游戏视觉's Prop_PoolTile_FieldMesh / CourseMesh, which are Resources assets carrying Red's GZ-C glaze:
    /// they are never written here (a value set on an asset material in the Editor is saved into its .mat), so GZ-A's values
    /// reach only the LS fallback, LsTile / LsBand.
    /// </summary>
    /// <summary>DishTone's steps on - / =: 游戏视觉's 0 / 0.5 / 1 / 1.5, then 2 and 3 beyond their tuned range, to try.</summary>
    static readonly float[] ToneSteps = { 0f, .5f, 1f, 1.5f, 2f, 3f };

    /// <summary>The dish tone's step on the flash.</summary>
    static string ToneName(float tone)
    {
        if (tone < .25f) return "0（只靠灯影，没有明暗）";
        if (tone < .75f) return "0.5（很淡）";
        if (tone < 1.25f) return "1（默认，近看轻轻的凹）";
        if (tone < 1.75f) return "1.5（近看明显的凹）";
        if (tone < 2.5f) return "2（超出游戏视觉调好的范围，只供尝试）";
        return "3（超出游戏视觉调好的范围，只供尝试）";
    }

    /// <summary>The pool tile look's name on the flash (Chinese, as Red reads it).</summary>
    static string TileLookName(FrontRoomsPoolTile.PoolTileLook look)
    {
        switch (look)
        {
            case FrontRoomsPoolTile.PoolTileLook.Today: return "今天的砖（换釉面之前）";
            case FrontRoomsPoolTile.PoolTileLook.GZC: return "GZ-C：你选的釉面，砖面按选项图";
            case FrontRoomsPoolTile.PoolTileLook.DishShallow: return "浅凹";
            case FrontRoomsPoolTile.PoolTileLook.DishMedium: return "中凹（默认：中间凹四角突）";
            case FrontRoomsPoolTile.PoolTileLook.DishDeep: return "深凹";
        }
        return look.ToString();
    }

    static void Glaze()
    {
        foreach (var m in new[] { FrontRoomsPoolMaterials.LsTile, FrontRoomsPoolMaterials.LsBand })
        {
            if (m == null) continue;
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", GlazeSmoothness);
            if (m.HasProperty("_OcclusionStrength")) m.SetFloat("_OcclusionStrength", GlazeCavity);
        }
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
        // built under the other assets switch or another answer to Red's asks: rebuilt from a fresh district first
        if (d.Built && !d.Failed && d.AssetsSignature != FrontRoomsStructuresAssets.Signature(d.Key)) d = Rebuild(d);
        if (!d.Built && !d.Failed) BuildDistrict(d);
        if (d.Failed)
        {
            Flash(d.Code + " 构建失败：" + d.Error);
            // a failed rebuild of the current district leaves no current one (no keys, no fall reset): back to the hub
            if (current == null && hub != null && d != hub) Jump(hub, 0);
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
            look.BeginDistrictBuild(d.Key);
            try { d.Build(this); }
            finally { look.EndDistrictBuild(); }
            d.Built = true;
            d.AssetsSignature = FrontRoomsStructuresAssets.Signature(d.Key);
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
        // the scene-wide C (Relay body) and M (metric marks) hold for a district built while they are on (a first build after C,
        // or a fresh one from the ; key and the asks)
        if (d.Built && !d.Failed && walker != null)
        {
            try
            {
                if (walker.RelayBody) d.RelayBody(this, true);
                if (metrics) d.ShowMetrics(this, true);
            }
            catch (Exception e) { Debug.LogError(Log + d.Code + " relay body / metrics: " + e); }
        }
    }

    /// <summary>
    /// A district built again from a fresh instance of its class (the assets switch or an ask changed what it builds): the old one
    /// leaves, is released and its root destroyed (its lamps drop out of the budget, its pool rooms out of the zone looks), the
    /// new one takes its place in the table and is built. The pool route (the map's world) is never rebuilt here.
    /// </summary>
    FrontRoomsStructuresDistrict Rebuild(FrontRoomsStructuresDistrict d)
    {
        if (d == null || d.KeepActive) return d;
        var fresh = d == hub ? new FrontRoomsStructuresHub(d.Site) : FrontRoomsStructuresDistrict.Make(d.GetType().FullName, d.Site);
        if (fresh == null) return d;
        if (current == d)
        {
            try { d.Leave(this); } catch (Exception e) { Debug.LogError(Log + d.Code + " leave: " + e); }
            current = null;
        }
        try { d.Release(); } catch (Exception e) { Debug.LogError(Log + d.Code + " release: " + e); }
        if (d.Root != null)
        {
            look.HoldProbes(d.Key, d.Root, transform);
            d.Root.gameObject.SetActive(false);
            Destroy(d.Root.gameObject);
        }
        if (d == hub) FrontRoomsStructuresSigns.Pads.Clear();
        look.Forget(d);
        var i = districts.IndexOf(d);
        if (i >= 0) districts[i] = fresh;
        if (hub == d) hub = fresh;
        BuildDistrict(fresh);
        return fresh;
    }

    /// <summary>The structure assets on or off (the ; key; the review harness sets it the same way): the current district is
    /// rebuilt where the walker stands, every other one on its next visit.</summary>
    public void SetAssetsNew(bool on)
    {
        FrontRoomsStructuresAssets.New = on;
        RefreshCurrent();
        Flash(on ? "结构资产（;）：新的 · 游戏视觉按游戏标准做的倒角、护栏、楼梯、台沿和表面（默认）"
                 : "结构资产（;）：旧的灰盒 · 和改之前一模一样，用来对比");
    }

    // The current district again under the switch and the asks, the walker kept where it stands. Nothing happens where the key
    // changes nothing (SP7 and the pool route keep today's build in both modes; an ask its district does not use): a Jump there
    // re-armed SP7's alarm and streamed P round its stop. A ride ends in the rebuild's Leave, so it is read before.
    void RefreshCurrent()
    {
        if (current == null || walker == null) return;
        if (current.KeepActive || current.AssetsSignature == FrontRoomsStructuresAssets.Signature(current.Key)) return;
        var key = current.Key;
        var feet = walker.transform.position;
        float yaw = walker.Yaw, pitch = walker.Pitch;
        var riding = walker.Riding;
        Jump(current, current.StopIndex);
        // back where the walker stood (Teleport sets the feet 2 cm up), unless a ride ended or the fresh build failed (the hub
        // took over)
        if (!riding && current != null && current.Key == key) walker.Teleport(feet - Vector3.up * .02f, yaw, pitch);
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
        // GZ-A (73 section 3.4): the district's probes are captured two frames after the arrival, with every lamp of it on
        look.Entered(d);
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
        // 游戏视觉's pool zone look and the scene's own share the frame (the zone weighed at the camera, as URP weighs its grade)
        look.ZoneTick(current, feet, walker.View != null ? walker.View.transform.position : walker.EyePosition);
        look.Tick(walker.EyePosition, current.Key, dt);
    }

    // K2: the Home dip (the game's W1 dip: under HidDropOut 0.80, so the arcs go out)
    const float PoolDipLine = .6f, PoolDipSeconds = 1.2f;

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
        if (k.qKey.wasPressedThisFrame)
        {
            look.SetZoneLook(!look.ZoneLookOn);
            Flash(look.ZoneFlash(current));
        }
        // ` (Shift+` back): 游戏视觉's pool tile looks (pool_tile_real, 11-05 04:22; Red picked GZ-C on 11-04 and asked them for
        // "中间凹四角突"): Today, GZ-C as given, dish shallow / medium (their default) / deep. The current district's probes are
        // rendered again so the glaze reflects the room in the new look.
        if (k.backquoteKey.wasPressedThisFrame)
        {
            var tile = FrontRoomsPoolTile.Look;
            var back = k.leftShiftKey.isPressed || k.rightShiftKey.isPressed;
            FrontRoomsPoolTile.SetLook(back ? (FrontRoomsPoolTile.PoolTileLook)(((int)tile + 4) % 5) : FrontRoomsPoolTile.NextLook(tile));
            if (current != null && current.Root != null) look.RerenderProbes(current.Root);
            Flash("泳池瓷砖（`）：" + TileLookName(FrontRoomsPoolTile.Look) + "  ·  凹凸明暗 " + ToneName(FrontRoomsPoolTile.DishTone));
        }
        // - / =: how strongly the dish also shows in the tile's own shading (游戏视觉's DishTone, pool_tile_tone 11-05 08:04; a
        // global, no material is edited): 0 (the dish in the normal only), 0.5, 1 (their recommended default), 1.5, and 2 / 3,
        // beyond their tuned range, only for Red to try (Red 11-05: "把每一种都做到那个structure scene里我自己尝试").
        if (k.minusKey.wasPressedThisFrame || k.equalsKey.wasPressedThisFrame)
        {
            var steps = ToneSteps;
            var at = 0;
            for (var i = 0; i < steps.Length; i++) if (Mathf.Abs(steps[i] - FrontRoomsPoolTile.DishTone) < Mathf.Abs(steps[at] - FrontRoomsPoolTile.DishTone)) at = i;
            at = Mathf.Clamp(at + (k.equalsKey.wasPressedThisFrame ? 1 : -1), 0, steps.Length - 1);
            FrontRoomsPoolTile.DishTone = steps[at];
            if (current != null && current.Root != null) look.RerenderProbes(current.Root);
            Flash("瓷砖凹凸明暗（- =）：" + ToneName(FrontRoomsPoolTile.DishTone) + "  ·  " + TileLookName(FrontRoomsPoolTile.Look));
        }
        // \ : 游戏视觉's surface detail (office_surface_detail and the Level 0, Run!, station and stair packages, 11-05 04:22, the
        // default since) or today's surfaces, bit for bit (FrontRoomsSurfaceDetail.Today, a global: every _FR_DETAIL material at
        // once). Red 11-05 ~13:3x asked for a key to compare the office's new carpet tiles, walls and ceiling with the old ("加一个").
        if (k.backslashKey.wasPressedThisFrame)
        {
            FrontRoomsSurfaceDetail.Today = !FrontRoomsSurfaceDetail.Today;
            Flash(FrontRoomsSurfaceDetail.Today
                ? "表面（\\）：旧的（11 月 5 日以前）  ·  办公室、Level 0、Run!、候车区、楼梯一起换"
                : "表面（\\）：新的（游戏视觉 11 月 5 日做的，游戏里的默认）");
        }
        // ; : 游戏视觉's structure assets or today's greybox, bit for bit (structures_assets, contract K1)
        if (k.semicolonKey.wasPressedThisFrame) { SetAssetsNew(!FrontRoomsStructuresAssets.New); return; }
        // Red's open asks (30_plan section 5), built as recommended with the alternatives a key away: ' pool edge (1), / tile on
        // tight curves (2), , rails' pickets (3), . lamp modes (5); 1-3 change what is built (the district is rebuilt). Not with
        // Shift held: Shift + . is the lens A/B of 光影水反's LW3 (FrontRoomsStructuresWater.cs)
        if (FrontRoomsStructuresAssets.New && !(k.leftShiftKey.isPressed || k.rightShiftKey.isPressed))
        {
            var ask = k.quoteKey.wasPressedThisFrame ? 1 : k.slashKey.wasPressedThisFrame ? 2 : k.commaKey.wasPressedThisFrame ? 3 : k.backquoteKey.wasPressedThisFrame ? 4 : k.periodKey.wasPressedThisFrame ? 5 : 0;
            if (ask > 0)
            {
                var line = FrontRoomsStructuresAssets.Cycle(ask);
                if (ask == 5) FrontRoomsStructuresLampModes.SetGameModes(FrontRoomsStructuresAssets.Lamps == FrontRoomsStructuresAssets.LampModes.Game);
                // ask 4 (` : Red's paper at the wrong scale) is FrontRooms/Surface's global (structures_scale_terms): nothing to rebuild
                else if (ask != 4) RefreshCurrent();
                Flash(line);
                return;
            }
        }
        // Home (K2, 游戏视觉's structures G2-a): a dip of the pool district's line, as the game's W1 dip, so the pool rig's
        // metal-halide cycle is seen in place: every wall box drops out at once (one step) and its quartz lamp holds 15 % in warm
        // white, the arcs restrike after 8 s and warm up over 24 s, blue-green first; the niches dim with the line and come back.
        // SP1, SP2 and SP6 in NEW; not with Shift held (Shift + . is 光影水反's lens A/B)
        if (k.homeKey.wasPressedThisFrame && !(k.leftShiftKey.isPressed || k.rightShiftKey.isPressed))
        {
            if ((current.Key == 1 || current.Key == 2 || current.Key == 6) && FrontRoomsStructuresAssets.NewFor(current.Key))
            {
                look.Dip(current.Key, PoolDipLine, PoolDipSeconds);
                Flash("灯线下陷（Home）：池厅的金卤壁灯一起熄灭，壁灯里的石英灯留 15% 暖光；8 秒后重新点火，24 秒里从暗青慢慢亮到白；水下灯跟着暗一下再回来。");
            }
            else Flash("灯线下陷（Home）只在 SP1、SP2、SP6 的新资产里有（灯具适配的金卤壁灯和水下灯）。");
            return;
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
            // GZ-A: a district key changes what its probes see (SP1's night key and flume, SP6's dome light): captured again
            if (handled) look.Changed(current);
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
        // The look keys write globals that would stay in the Editor after Play: back to the game's defaults.
        if (FrontRoomsSurfaceDetail.Today) FrontRoomsSurfaceDetail.Today = false;
        FrontRoomsStructuresAssets.New = false;
        FrontRoomsStructuresAssets.ResetAsks();
        FrontRoomsStructuresLampModes.SetGameModes(true);
        FrontRoomsPoolTile.SetLook(FrontRoomsPoolTile.DefaultLook);
        FrontRoomsPoolTile.DishTone = FrontRoomsPoolTile.DefaultDishTone;
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
        var zone = look.ZoneStatus(d);
        Text(new Rect(38f, 52f, 1400f, 20f), "场景 4 · 新结构  ·  " + d.Code + (stop.Length > 0 ? "  ·  站点 " + (d.StopIndex + 1) + "/" + d.Stops.Count + " " + stop : "") + (walker.RelayBody ? "  ·  Relay 体型" : "") + "  ·  " + d.Status(this) + (zone.Length > 0 ? "  ·  " + zone : ""), smallStyle, Muted);

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
            "点击：看  ·  WASD 走  ·  左 Ctrl 跑  ·  Shift 慢走  ·  E 使用  ·  1-6 区域  ·  7 泳池路线  ·  8 模组件  ·  9 SP7  ·  0 大厅  ·  R 回到站点  ·  [ ] 换站点  ·  L 灯  ·  Home 灯线下陷（泳池）  ·  Q 泳池外观  ·  ` 瓷砖  ·  - = 凹凸明暗  ·  \\ 表面新旧  ·  V 变体  ·  C Relay 体型  ·  M 尺寸  ·  G 说明  ·  Esc 鼠标",
            smallStyle, Muted);

        if (!guide) return;
        var sb = new StringBuilder();
        try { d.Guide(this, sb); } catch (Exception e) { sb.AppendLine("（说明出错：" + e.Message + "）"); }
        var zoneGuide = look.ZoneGuide(d);
        if (zoneGuide.Length > 0) sb.AppendLine().AppendLine(zoneGuide);
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

    /// <summary>A stop at a floor point facing a point (level heading; pitch toward it, clamped to <paramref name="maxPitch"/>,
    /// 30 degrees unless a stop looks down into a void or up a shaft: scene 4 review SP2-F1).</summary>
    public static FrontRoomsStructuresStop Facing(string name, Vector3 floor, Vector3 lookAt, float maxPitch = 30f)
    {
        var d = lookAt - (floor + Vector3.up * FrontRoomsStructuresWalker.Eye);
        var flat = new Vector2(d.x, d.z);
        var yaw = flat.sqrMagnitude > 1e-6f ? Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg : 0f;
        var pitch = flat.magnitude > 1e-3f ? Mathf.Clamp(-Mathf.Atan2(d.y, flat.magnitude) * Mathf.Rad2Deg, -maxPitch, maxPitch) : 0f;
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
    /// <summary>FrontRoomsStructuresAssets.Signature at the build (the assets switch and Red's asks it was built under).</summary>
    public int AssetsSignature;
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
