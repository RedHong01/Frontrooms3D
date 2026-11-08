using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

/// <summary>
/// Scene 4's water preview (RULES 5b; line 水体仿真调研, research/bathroom_rooms/17_water.md section 10). Red, 2026-11-04,
/// on where the water work is shown: "不需要写在克隆里，坐在结构scene里这样我就能预览了"; and on choosing: "我需要你把每一版的水都做进去，
/// 然后让我在structure scene试了之后我选择对应的". Three keys while walking:
/// K cycles every version of the water (Shift K back): today's stand-in, two variants of it on the same runtime materials
/// (FrontRoomsPoolMaterials.WaterOf: SP1, SP2, SP6 and the pool route P share them), then 游戏视觉's water surface
/// (pool_water: FrontRooms/Water, FrontRoomsWaterSurface, the Submerged colour) at every absorption scale of 17 Q1
/// (x1, x3, x4, x5, x9), two ankle-water variants (17 section 10.7: ankle water vanishes in white rooms) and two
/// waterline variants (游戏视觉's FrontRoomsWaterSubmerged.Waterline: wet band, darkening, meniscus, tide mark);
/// P cycles the water's platform tier (auto, desktop Ultra, desktop High, phone), so each platform's water is seen
/// (L belongs to the districts: the lamps); U cycles how the water answers the walker (Shift U back; Red, 13:2x, on the
/// rings: "目前水的波纹我认为太规范了": every stride drew one perfect ring; 5-12 are 游戏视觉's less regular rings); I cycles
/// 游戏视觉's water light on the room (Shift I back; Red, 13:2x: "目前水面的光没有给整个房间晕染上一层反射光", "如果水是青色，
/// 这个颜色是否会影响房间？"), and every change of look re-renders the halls'
/// reflection probes, so the tile and ceiling reflect the water now on screen; Z puts 游戏视觉's pool zone look over each
/// body's room (FrontRoomsZoneLookVolume: the zone's ambient, haze, height fog and grade, by the stand-in's water class:
/// club water PoolClub, level water PoolLevel, dark water PoolDark) in place of the district's one fixed trilight;
/// J cycles the desktop ray tracing (GlassRT Off, High, Ultra), so what it does to the water is seen live, with
/// GlassRT's and the water's own numbers on a small panel at the bottom left of the water districts.
/// Made at Play by the structures scene's bootstrap only, in the Editor only: the game scene, the Level Designer and every
/// build never create it. Today's look is the stand-in as built; leaving the scene restores GlassRT's quality and the
/// stand-in meshes (the scene's OnDestroy releases the materials and the ripple textures this makes).
/// </summary>
[DisallowMultipleComponent]
public sealed class FrontRoomsStructuresWater : MonoBehaviour
{
    enum Look { Today, Calm, DarkGlass, Clear, Absorb3, Turquoise, Absorb5, Absorb9, AnkleDeep, AnkleFilm, Waterline, TideMark }

    /// <summary>The names Red picks by (the sheet's codes: research/bathroom_rooms/sheets/water_looks.jpg, 71_water_q1.jpg).</summary>
    static readonly string[] LookNames =
    {
        "1 现在：替身水面（静止的大波纹，坡度平均 13°）",
        "2 W-A 平静：会动的细波纹（坡度约 2°），其余同替身",
        "3 W-B 暗色镜面：POOLS 方向，水体发暗，倒影为主",
        "4 W-C 清水 ×1：游戏视觉的水面，真实吸收（会所推荐）",
        "5 吸收 ×3：淡青（暗区推荐）",
        "6 W-D 亮青绿 ×4：一眼看出是水（关卡推荐）",
        "7 吸收 ×5：再青一点",
        "8 吸收 ×9：浅水也像深池（“一两个房间”那一档）",
        "9 浅水加深：深水 ×4，脚踝深的水 ×12",
        "10 浅水水膜：同 9，脚踝深的水面再毛一点，倒影碎开",
        "11 水线：同 6，墙脚和柱脚的湿痕更高更深，水边那道弯月形的亮边更亮",
        "12 水线加水渍：同 11，湿痕上缘多一道茶褐色的水渍线",
    };
    const int LookCount = 12;

    /// <summary>The absorption scale per look on 游戏视觉's water (17 Q1: the club x1, the level x3-5, one or two rooms x9;
    /// 70 section 7.3: club x1, dark x3, level x4), for deep water (Basin) and for flooded floors (Ankle, under AnkleDepth).</summary>
    static readonly float[] AbsorbDeep = { 0f, 0f, 0f, 1f, 3f, 4f, 5f, 9f, 4f, 4f, 4f, 4f };
    static readonly float[] AbsorbAnkle = { 0f, 0f, 0f, 1f, 3f, 4f, 5f, 9f, 12f, 12f, 4f, 4f };
    /// <summary>The ankle water's calm roughness (FrontRooms/Water _FR_WaterAlpha0, default 0.012); below 0 the default.
    /// 0.04 blurs the mirror into a film: the white ceiling no longer reads as a clean floor.</summary>
    static readonly float[] AnkleAlpha = { -1f, -1f, -1f, -1f, -1f, -1f, -1f, -1f, -1f, .04f, -1f, -1f };
    /// <summary>游戏视觉's waterline (FrontRoomsWaterSubmerged.Waterline, their values of 14:28): x raises the wet band (m),
    /// y adds darkening, z the meniscus gain, w the tea-brown tide mark at the band's dry edge; zero = their default.</summary>
    static readonly Vector4 WaterlineSet = new Vector4(.06f, .25f, 1.5f, 0f), TideSet = new Vector4(.06f, .25f, 1.5f, .6f);

    /// <summary>P: the water's platform tier (FrontRoomsWaterPlanar.ForceTier; -1 follows the game's rule).</summary>
    static readonly int[] Tiers = { -1, 2, 1, 0 };
    static readonly string[] TierNames =
    {
        "自动（跟画质设置走）",
        "桌面超高：镜面 1/2 分辨率，每帧",
        "桌面高：镜面 1/3 分辨率，隔帧",
        "手机：没有镜面，只有反射探针，手机波纹（WebGL、桌面中低画质同此档）",
    };

    /// <summary>U: how the water answers the walker (游戏视觉's rings, FrontRoomsWaterRings, and constant ripple,
    /// FrontRoomsWaterRipples). 1 as tuned: in shallow water every stride drops one ring at the feet (AutoSteps). 2 no stride
    /// rings: stepping in, the deep-water wake and the standing stir remain. 3 two feet: this file drops the strides itself,
    /// left and right of the walking line, each at its own stride length, place and strength, so their rings cross and
    /// break each other instead of drawing one chain of circles. 4 as 3 over a rougher, patchier constant ripple
    /// (Gain 1.5 = 3° rms, the top of calm water's 1-3°, 17 section 3.1; Patchiness 0.6, slicks over a larger share).</summary>
    static readonly string[] RippleNames =
    {
        "1 现在：浅水里每走一步，脚下落一圈完整的圆环",
        "2 少圈：只在踏进水、深水走动、站着不动时起波纹，浅水走路不再一步一圈",
        "3 两只脚：左右脚交替落水，落点、步长、力度都随机，圆环互相交错打乱",
        "4 再乱：同 3，底下的细波纹更强（约 3°），平静和起皱的地方更不均匀",
        "5 推荐（游戏视觉）：不再是画出来的圆线，脚下一片柔和的波纹，前强后弱、每步不同，很快归于平静",
        "6 推荐 + 两只脚：同 5，落脚用 3 的左右脚交替（落点、步长、力度随机）",
        "7 软边：圈不再是一道细黑线，变成宽而柔的波峰，脚下一小片搅动",
        "8 软边 + 脚形：同 7，每圈沿走的方向前强后弱、断成几瓣，每一步都不一样",
        "9 软边 + 水流：同 7，水面很慢地流动（约 1.2 厘米/秒），大圈被带歪成蛋形，小圈仍圆",
        "10 软边 + 回声减半：同 7，墙反射回来的圈只剩一半，晚一点到，彼此不齐",
        "11 软边 + 消得快：同 7，每圈早五分之一消失，身后拖的圈更少",
        "12 软边 + 步态：同 7，步子随速度变长（3.2 米/秒时一步 1.18 米），每步长短、位置、轻重都不一样",
    };
    /// <summary>U 3, 4 and 6 drop the strides in this file (Strides); 1 and 5-12 leave them to the rings' AutoSteps, 2 has none.
    /// 5-12 are 游戏视觉's less regular rings (FrontRoomsWaterRings, research/bathroom_rooms/70_look_build.md section 11):
    /// each starts from TodayLook(), 5 and 6 are NaturalLook(), 7 RimSoftness alone, 8-12 RimSoftness with one more static.</summary>
    bool OwnStrides => ripple == 2 || ripple == 3 || ripple == 5;
    const float StrideMin = .55f, StrideMax = .85f, FootSide = .11f, FootJitter = .07f, DeepWade = .45f;

    /// <summary>I: 游戏视觉's water light (FrontRoomsWaterSubmerged.SetWaterLight, research/bathroom_rooms/70_look_build.md
    /// section 12): the bounce (the light that went into the water comes back out in its colour, by depth, the zone's
    /// absorption and the white basin, in place of the zone ambient's light from the water's directions), the overhead lamps
    /// the water mirrors onto its ceiling (desktop only), the wet niches' glow spread over the basin. Every state but the
    /// first also makes the district's underwater lamps wet niches, so their light comes up out of the water. It replaced
    /// this file's upward spot lights of 13:26 (never both: they counted the water's light twice).</summary>
    static readonly string[] GlowNames =
    {
        "1 关：房间只有原来的灯和固定的泳池环境光（现在）",
        "2 水的颜色：天花板和墙的上部染上水透回来的青色光，正对水面最浓，墙的下段比墙顶多；水下灯的光也从灯上方的水面透出来",
        "3 水的颜色减半：同 2，弱一档",
        "4 灯在水里：头顶的灯被水面映到天花板上，一层淡淡的、随波纹漂动的光纹（只在电脑上有，P 切到手机档会自己关掉）",
        "5 水下灯散开：水下灯的光从整片池底泛上来，不再只在每盏灯上方一团（总亮度不变）",
        "6 推荐：2、4、5 一起",
        "7 水的颜色 ×2：同 2 亮一倍，只为看清颜色往哪里走，不是物理值",
    };
    static readonly FrontRoomsWaterSubmerged.WaterLightLook[] GlowLooks =
    {
        FrontRoomsWaterSubmerged.WaterLightLook.Today, FrontRoomsWaterSubmerged.WaterLightLook.Bounce, FrontRoomsWaterSubmerged.WaterLightLook.Bounce,
        FrontRoomsWaterSubmerged.WaterLightLook.Mirror, FrontRoomsWaterSubmerged.WaterLightLook.Spread, FrontRoomsWaterSubmerged.WaterLightLook.Recommended,
        FrontRoomsWaterSubmerged.WaterLightLook.Bounce,
    };
    static readonly float[] GlowBounce = { 0f, 1f, .5f, 0f, 0f, 1f, 2f };   // BounceGain after the look (3 and 7 move it)
    const float CausticRadius = .05f;
    const string NicheLamp = "Lamp · underwater";                          // FrontRoomsStructuresKit.Underwater's lamp

    /// <summary>Z: 游戏视觉's pool zone look (pool_zone_look, 70 section 3) over each body's room, or the scene's own trilight.</summary>
    static readonly string[] ZoneNames =
    {
        "1 场景原来的：整个区一组固定的泳池环境光（现在）",
        "2 游戏视觉的泳池区域外观：按水的类别（会所 / 关卡 / 暗区）换环境光、雾和调色，进出房间时渐变",
    };
    const float ZoneBlend = 2.5f, ZoneBelow = 1.5f;

    /// <summary>The calm ripple (17 section 3.1, section 7 item 3): isotropic, slopes about 2° RMS for the two layers
    /// together, never a visible repeat. Layer A: a 1.6 m tile of 0.12-0.8 m waves drifting 6 cm/s; layer B: a 0.7 m
    /// tile of 2.5-12 cm waves drifting 4 cm/s across it (the water's UVs are in metres). URP Lit reads the detail layer at
    /// baseUV * _DetailAlbedoMap_ST.xy + .zw, on top of _BaseMap_ST, so B's scale is TileA / TileB and its offset takes A's
    /// drift back out (LitInput.hlsl, InitializeStandardLitSurfaceData).</summary>
    const float TileA = 1.6f, TileB = .7f, SlopeA = 1.4f, SlopeB = 1.3f, SpeedA = .06f, SpeedB = .04f;
    static readonly Vector2 DriftA = new Vector2(.8f, .6f), DriftB = new Vector2(-.5f, .866f);
    static readonly Color DarkBase = new Color(.016f, .046f, .044f, .84f);
    static readonly Color Accent = new Color(.957f, .875f, .231f);
    static readonly Color Paper = new Color(.957f, .945f, .91f);
    static readonly Color Muted = new Color(.74f, .73f, .69f);

    /// <summary>Water shallower than this at a body's centre is a flooded floor (FrontRoomsWaterSurface.Kind.Ankle).</summary>
    const float AnkleDepth = .3f;
    /// <summary>With more flat rectangles than FRSubFind holds, the nearest get bodies; a walk this far rebuilds them.</summary>
    const float RebuildWalk = 8f;

    FrontRoomsStructures host;
    // Red, 13:1x, on W-D: "我目前认为这个ok": the scene opens on it (K still reaches today's stand-in and every other version)
    Look look = Look.Turquoise;
    Texture2D rippleA, rippleB;
    readonly Material[] water = new Material[3];
    readonly Snapshot[] saved = new Snapshot[3];
    FrontRoomsGlassRTQuality rtAtStart;
    bool rtChanged, hinted, tierChanged, rippleChanged;
    int tier, ripple;
    // the walker's own strides (ripple 3 and 4)
    Vector3 lastFeet;
    bool haveFeet, leftFoot;
    float walked, nextStride = .7f;
    readonly System.Random strideRandom = new System.Random(4113);
    // the water's light (I), the wet niches it registered, and the halls' probes
    int glow;
    readonly List<Light> niches = new List<Light>();
    readonly Dictionary<Light, float> nicheBase = new Dictionary<Light, float>();
    readonly List<Light> roomLights = new List<Light>();
    int probeFrames = -1;
    int zoneLook;
    float hintAt = -1f;
    int lastDistrict = -1;
    // the district object itself: a ';' or an ask key rebuilds a district as a fresh instance under the same key, and the
    // bodies parented under its old root die with it (夸张结构调研与设计's K1 review, finding B)
    FrontRoomsStructuresDistrict lastInstance;
    bool districtHasWater;
    float nextStats, nextRealCheck;
    string statsLine = "";
    GUIStyle style;

    // 游戏视觉's water (looks Clear and after): one body per flat rectangle of the stand-in, under its source renderer;
    // flooded floors (Ankle) draw with a material of their own, so the ankle looks can change their surface alone
    Material poolWater, poolWaterAnkle;
    float defaultAlpha = .012f;
    string realProblem;
    bool realChecked;
    readonly List<GameObject> bodies = new List<GameObject>();
    readonly List<Mesh> bodyMeshes = new List<Mesh>();
    readonly List<Source> sources = new List<Source>();
    Vector3 builtAt;
    bool builtCapped;
    int candidates;
    readonly RaycastHit[] hits = new RaycastHit[16];

    struct Snapshot
    {
        public bool valid;
        public Color baseColor;
        public float smoothness, bumpScale;
        public Texture bump;
        public Vector2 scale, offset;
    }

    /// <summary>A stand-in renderer whose flat rectangles became bodies: its own mesh, and the one drawn meanwhile
    /// (what is left: the slide's sloped film, rectangles past the cap).</summary>
    sealed class Source
    {
        public MeshFilter filter;
        public Renderer renderer;
        public Mesh original, kept;
        public bool wasEnabled;
    }

    /// <summary>One flat rectangle of a stand-in mesh (the kit's Water: one quad each), in its renderer's local space.</summary>
    sealed class Flat
    {
        public Renderer renderer;
        public MeshFilter filter;
        public float x0 = float.MaxValue, x1 = float.MinValue, z0 = float.MaxValue, z1 = float.MinValue, y;
        public readonly List<int> triangles = new List<int>();
        public float distance;

        public void Grow(Vector3 p)
        {
            x0 = Mathf.Min(x0, p.x); x1 = Mathf.Max(x1, p.x);
            z0 = Mathf.Min(z0, p.z); z1 = Mathf.Max(z1, p.z);
        }
    }

#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Attach()
    {
        var bootstrap = FindAnyObjectByType<FrontRoomsStructures>();
        if (bootstrap != null && bootstrap.GetComponent<FrontRoomsStructuresWater>() == null) bootstrap.gameObject.AddComponent<FrontRoomsStructuresWater>();
    }
#endif

    void Start()
    {
        host = GetComponent<FrontRoomsStructures>();
        rtAtStart = FrontRoomsGlassRT.Quality;
    }

    void Update()
    {
        if (host == null || host.Current == null) return;
        var d = host.Current;
        if (d.Key != lastDistrict || !ReferenceEquals(d, lastInstance))
        {
            lastDistrict = d.Key;
            lastInstance = d;
            districtHasWater = HasWater(d);
            // a second after arriving, so the jump's own message has been read
            if (districtHasWater && !hinted) { hinted = true; hintAt = Time.time + 1.2f; }
            if (IsReal(look)) { BuildBodies(d); ScheduleProbes(); }
        }
        else if (d.Key == 7 && !districtHasWater && Time.frameCount % 120 == 0) districtHasWater = HasWater(d);   // P streams
        if (hintAt > 0f && Time.time >= hintAt)
        {
            hintAt = -1f;
            host.Flash("水的预览：K 换水面（" + LookCount + " 版），U 换波纹，I 水光，Z 区域外观，P 换平台档，J 切换光追（左下角看数据）。");
        }

        var k = Keyboard.current;
        if (k != null && k.kKey.wasPressedThisFrame)
        {
            var back = k.leftShiftKey.isPressed || k.rightShiftKey.isPressed;
            var next = look;
            for (var i = 0; i < LookCount; i++)
            {
                next = (Look)(((int)next + (back ? LookCount - 1 : 1)) % LookCount);
                if (!IsReal(next) || RealAvailable()) break;
            }
            look = next;
            Apply();
            var note = look == Look.Calm || look == Look.DarkGlass ? "（只换替身材质）" : look == Look.Today && !RealAvailable() ? "（真水面还不能用：" + realProblem + "）" : "";
            host.Flash("水面 · " + LookNames[(int)look] + note);
        }
        if (k != null && k.jKey.wasPressedThisFrame) CycleRT();
        if (k != null && k.zKey.wasPressedThisFrame)
        {
            zoneLook = (zoneLook + 1) % ZoneNames.Length;
            foreach (var go in bodies) ZoneFor(go);
            ScheduleProbes();
            var placed = SceneZoneLook(d);
            host.Flash(placed ? "区域外观：这个区由场景自己放游戏视觉的区域外观（默认开），Z 不再另放；按 Q 开关"
                : "区域外观 · " + ZoneNames[zoneLook] + (IsReal(look) ? "" : "（只对游戏视觉的水面有效：按 K 换到第 4 版以后）"));
        }
        if (k != null && k.iKey.wasPressedThisFrame)
        {
            var back = k.leftShiftKey.isPressed || k.rightShiftKey.isPressed;
            glow = (glow + (back ? GlowNames.Length - 1 : 1)) % GlowNames.Length;
            ApplyGlow();
            ScheduleProbes();
            host.Flash("水光 · " + GlowNames[glow] + (IsReal(look) ? "" : "（只对游戏视觉的水面有效：按 K 换到第 4 版以后）"));
        }
        if (probeFrames > 0 && --probeFrames == 0) RenderProbes();
        if (k != null && k.uKey.wasPressedThisFrame)
        {
            var back = k.leftShiftKey.isPressed || k.rightShiftKey.isPressed;
            ripple = (ripple + (back ? RippleNames.Length - 1 : 1)) % RippleNames.Length;
            ApplyRipple();
            host.Flash("波纹 · " + RippleNames[ripple] + (IsReal(look) ? "" : "（只对游戏视觉的水面有效：按 K 换到第 4 版以后）"));
        }
        if (k != null && k.pKey.wasPressedThisFrame)
        {
            tier = (tier + 1) % Tiers.Length;
            FrontRoomsWaterPlanar.ForceTier = Tiers[tier];
            FrontRoomsWaterRipples.ForcePhoneTier = Tiers[tier] == 0;
            if (IsReal(look)) FrontRoomsWaterRipples.Ensure();            // the phone's ripple loops load on the switch
            tierChanged = true;
            nextStats = 0f;
            host.Flash("平台档 · " + TierNames[tier] + (IsReal(look) ? "" : "（只对游戏视觉的水面有效：按 K 换到第 4 版以后）"));
        }

        if (look == Look.Calm || look == Look.DarkGlass)
        {
            var t = Time.time;
            for (var i = 0; i < water.Length; i++)
            {
                var m = water[i];
                if (m == null) continue;
                m.mainTextureOffset = DriftA * (SpeedA * t / TileA);
                m.SetTextureOffset("_DetailAlbedoMap", (DriftB * SpeedB - DriftA * SpeedA) * (t / TileB));
            }
        }
        if (IsReal(look))
        {
            var walker = host.Walker;
            if (walker != null && bodies.Count > 0)
            {
                FrontRoomsWaterRings.ReportBody(FrontRoomsWaterRings.Body.Player, walker.transform.position, .3f);
                if (OwnStrides) Strides(walker.transform.position);
            }
            // P streams its chunks, and a district with more rectangles than FRSubFind holds keeps the nearest
            if (Time.unscaledTime >= nextRealCheck)
            {
                nextRealCheck = Time.unscaledTime + 2f;
                var feet = walker != null ? walker.transform.position : builtAt;
                if (districtHasWater && ((bodies.Count == 0 && d.Key == 7) || (builtCapped && (feet - builtAt).sqrMagnitude > RebuildWalk * RebuildWalk))) BuildBodies(d);
            }
        }
        if (Time.unscaledTime >= nextStats)
        {
            nextStats = Time.unscaledTime + .25f;
            statsLine = Stats();
        }
    }

    void OnDestroy()
    {
        if (rtChanged) FrontRoomsGlassRT.Quality = rtAtStart;
        if (tierChanged)
        {
            FrontRoomsWaterPlanar.ForceTier = -1;
            FrontRoomsWaterRipples.ForcePhoneTier = false;              // the next Ensure reloads the desktop loops
        }
        FrontRoomsWaterSubmerged.Waterline = Vector4.zero;
        if (rippleChanged)
        {
            FrontRoomsWaterRings.AutoSteps = true;
            FrontRoomsWaterRings.TodayLook();
            FrontRoomsWaterRipples.Gain = 1f;
            FrontRoomsWaterRipples.Patchiness = .25f;
        }
        TearDownBodies();
        if (glow != 0) FrontRoomsWaterSubmerged.SetWaterLight(FrontRoomsWaterSubmerged.WaterLightLook.Today);
        if (poolWater != null) Destroy(poolWater);
        if (poolWaterAnkle != null) Destroy(poolWaterAnkle);
        if (rippleA != null) Destroy(rippleA);
        if (rippleB != null) Destroy(rippleB);
    }

    // ---------------------------------------------------------------- the looks

    static bool IsReal(Look l) => l >= Look.Clear;

    static bool IsStandIn(Material m) => m != null && m.name.StartsWith("Structures / water", System.StringComparison.Ordinal);

    static bool HasWater(FrontRoomsStructuresDistrict d)
    {
        if (d == null || d.Root == null) return false;
        foreach (var r in d.Root.GetComponentsInChildren<Renderer>(true))
            if (IsStandIn(r.sharedMaterial)) return true;
        return false;
    }

    void Apply()
    {
        // the stand-in materials: the real looks keep them as built (the slide's film and rectangles past the cap stay on them)
        var standIn = look == Look.Calm || look == Look.DarkGlass ? look : Look.Today;
        for (var i = 0; i < water.Length; i++)
        {
            var m = FrontRoomsPoolMaterials.WaterOf((FrontRoomsPoolMaterials.Water)i);
            // a material the scene made anew is today's stand-in as built; one we already changed is never saved again
            if (m != null && (m != water[i] || !saved[i].valid)) saved[i] = Save(m);
            water[i] = m;
            if (m == null || !m.HasProperty("_BaseColor")) continue;
            var s = saved[i];
            if (standIn == Look.Today)
            {
                m.SetColor("_BaseColor", s.baseColor);
                m.SetFloat("_Smoothness", s.smoothness);
                m.SetTexture("_BumpMap", s.bump);
                m.SetFloat("_BumpScale", s.bumpScale);
                m.mainTextureScale = s.scale;
                m.mainTextureOffset = s.offset;
                m.SetTexture("_DetailNormalMap", null);
                m.SetTextureScale("_DetailAlbedoMap", Vector2.one);
                m.SetTextureOffset("_DetailAlbedoMap", Vector2.zero);
                m.DisableKeyword("_DETAIL_MULX2");
                continue;
            }
            if (rippleA == null) rippleA = Ripple("Structures / water preview ripple A", 2, 13, SlopeA, 11);
            if (rippleB == null) rippleB = Ripple("Structures / water preview ripple B", 6, 28, SlopeB, 23);
            m.SetTexture("_BumpMap", rippleA);
            m.SetFloat("_BumpScale", 1f);
            m.mainTextureScale = Vector2.one / TileA;
            m.SetTexture("_DetailNormalMap", rippleB);
            m.SetFloat("_DetailNormalMapScale", 1f);
            m.SetTextureScale("_DetailAlbedoMap", Vector2.one * (TileA / TileB));
            m.EnableKeyword("_DETAIL_MULX2");
            m.SetColor("_BaseColor", standIn == Look.DarkGlass ? DarkBase : s.baseColor);
            m.SetFloat("_Smoothness", standIn == Look.DarkGlass ? .985f : s.smoothness);
        }

        ScheduleProbes();
        FrontRoomsWaterSubmerged.Waterline = look == Look.Waterline ? WaterlineSet : look == Look.TideMark ? TideSet : Vector4.zero;
        if (!IsReal(look)) { TearDownBodies(); return; }
        if (poolWaterAnkle != null)
        {
            var a = AnkleAlpha[(int)look];
            poolWaterAnkle.SetFloat("_FR_WaterAlpha0", a >= 0f ? a : defaultAlpha);
        }
        if (bodies.Count == 0) BuildBodies(host != null ? host.Current : null);
        else
            foreach (var go in bodies)
            {
                var ws = go != null ? go.GetComponent<FrontRoomsWaterSurface>() : null;
                if (ws == null) continue;
                ws.absorbScale = AbsorbOf(ws.kind);
                ws.RefreshSubmerged();
            }
    }

    float AbsorbOf(FrontRoomsWaterSurface.Kind kind)
    {
        return kind == FrontRoomsWaterSurface.Kind.Ankle ? AbsorbAnkle[(int)look] : AbsorbDeep[(int)look];
    }

    // ---------------------------------------------------------------- the water's light on the room (I) and the probes

    /// <summary>The district's lamps (point and spot).</summary>
    void RoomLights(FrontRoomsStructuresDistrict d)
    {
        roomLights.Clear();
        if (d == null || d.Root == null) return;
        foreach (var l in d.Root.GetComponentsInChildren<Light>(false))
            if (l != null && (l.type == LightType.Point || l.type == LightType.Spot))
                roomLights.Add(l);
    }

    /// <summary>The two lamps that light the water at c most (inside its room box), for its floor caustics.</summary>
    void CausticLamps(Vector3 c, float halfX, float halfZ, float room, out Light a, out Light b)
    {
        a = b = null;
        float sa = 0f, sb = 0f;
        foreach (var l in roomLights)
        {
            if (l == null || !l.isActiveAndEnabled || l.intensity <= 0f) continue;
            var v = l.transform.position - c;
            if (v.y <= .1f || v.y > room + .5f || Mathf.Abs(v.x) > halfX || Mathf.Abs(v.z) > halfZ) continue;
            var r2 = v.sqrMagnitude;
            if (r2 >= l.range * l.range) continue;
            var score = l.intensity * Mathf.Max(l.color.maxColorComponent, .01f) * (v.y / Mathf.Sqrt(r2)) / Mathf.Max(r2, .25f);
            if (score > sa) { b = a; sb = sa; a = l; sa = score; }
            else if (score > sb) { b = l; sb = score; }
        }
    }

    /// <summary>I's state on 游戏视觉's statics, and the district's underwater lamps as wet niches while it is on.</summary>
    void ApplyGlow()
    {
        FrontRoomsWaterSubmerged.SetWaterLight(GlowLooks[glow]);
        if (GlowBounce[glow] != FrontRoomsWaterSubmerged.BounceGain)
        {
            FrontRoomsWaterSubmerged.BounceGain = GlowBounce[glow];
            FrontRoomsWaterSubmerged.Publish(true);
        }
        Niches();
    }

    /// <summary>While I is on, the district's underwater lamps (FrontRoomsStructuresKit.Underwater) are wet niches of the water
    /// over them (FrontRoomsWaterSubmerged: a niche belongs to the lowest surface above it; one under no body throws nothing),
    /// at the intensity first seen (as built), so a lamp that dims or dies takes its glow with it.</summary>
    void Niches()
    {
        foreach (var l in niches) FrontRoomsWaterSubmerged.UnregisterNiche(l);
        niches.Clear();
        var d = host != null ? host.Current : null;
        if (glow == 0 || bodies.Count == 0 || d == null || d.Root == null) return;
        foreach (var l in d.Root.GetComponentsInChildren<Light>(true))
        {
            if (l == null || l.transform.parent == null || l.transform.parent.name != NicheLamp) continue;
            if (!nicheBase.TryGetValue(l, out var b)) nicheBase[l] = b = l.intensity;
            FrontRoomsWaterSubmerged.RegisterNiche(l, b);
            niches.Add(l);
        }
    }

    void ScheduleProbes() { probeFrames = 3; }

    /// <summary>The halls' box probes (FrontRoomsStructuresLook.Probe: realtime, rendered by script) see the water now on
    /// screen and its light, so the tile, the ceiling and the probe tiers reflect them.</summary>
    void RenderProbes()
    {
        var d = host != null ? host.Current : null;
        if (d == null || d.Root == null) return;
        foreach (var p in d.Root.GetComponentsInChildren<ReflectionProbe>(false))
        {
            if (p == null || p.mode != ReflectionProbeMode.Realtime || p.refreshMode != ReflectionProbeRefreshMode.ViaScripting) continue;
            try { p.RenderProbe(); }
            catch (System.Exception e) { FrontRoomsPoolMaterials.LogOnce("water probe", "a reflection probe did not render: " + e.Message); }
        }
    }

    // ---------------------------------------------------------------- the ripple styles (U)

    void ApplyRipple()
    {
        rippleChanged = true;
        FrontRoomsWaterRings.TodayLook();
        FrontRoomsWaterRings.AutoSteps = ripple != 1 && !OwnStrides;
        FrontRoomsWaterRipples.Gain = ripple == 3 ? 1.5f : 1f;
        FrontRoomsWaterRipples.Patchiness = ripple == 3 ? .6f : .25f;
        switch (ripple)
        {
            case 4: case 5: FrontRoomsWaterRings.NaturalLook(); break;
            case 6: FrontRoomsWaterRings.RimSoftness = 1f; break;
            case 7: FrontRoomsWaterRings.RimSoftness = 1f; FrontRoomsWaterRings.SourceShape = 1f; break;
            case 8: FrontRoomsWaterRings.RimSoftness = 1f; FrontRoomsWaterRings.Currents = .05f; break;
            case 9: FrontRoomsWaterRings.RimSoftness = 1f; FrontRoomsWaterRings.EchoGain = .5f; break;
            case 10: FrontRoomsWaterRings.RimSoftness = 1f; FrontRoomsWaterRings.LifeScale = .8f; break;
            case 11: FrontRoomsWaterRings.RimSoftness = 1f; FrontRoomsWaterRings.Gait = 1f; break;
        }
        haveFeet = false;
        nextStats = 0f;
    }

    /// <summary>Ripple 3, 4 and 6: the walker's strides in shallow water, one foot after the other. Each lands beside the
    /// walking line (FootSide), a few centimetres off (FootJitter), after its own stride length and with its own strength,
    /// so the rings cross. Deeper than DeepWade the rings' own wake takes over (ReportBody).</summary>
    void Strides(Vector3 feet)
    {
        if (!haveFeet) { lastFeet = feet; haveFeet = true; walked = 0f; return; }
        var d = new Vector2(feet.x - lastFeet.x, feet.z - lastFeet.z);
        var dist = d.magnitude;
        lastFeet = feet;
        if (dist > 2f) { walked = 0f; return; }                       // a jump
        if (dist < 1e-4f) return;
        var depth = ShallowDepth(feet);
        if (depth <= .02f || depth >= DeepWade) { walked = 0f; return; }
        walked += dist;
        if (walked < nextStride) return;
        walked = 0f;
        nextStride = Mathf.Lerp(StrideMin, StrideMax, (float)strideRandom.NextDouble());
        var dir = d / dist;
        var side = new Vector3(dir.y, 0f, -dir.x) * (leftFoot ? -FootSide : FootSide);
        leftFoot = !leftFoot;
        var jitter = new Vector3((float)strideRandom.NextDouble() - .5f, 0f, (float)strideRandom.NextDouble() - .5f) * (2f * FootJitter);
        FrontRoomsWaterRings.Footstep(feet + side + jitter, Mathf.Lerp(.45f, 1f, (float)strideRandom.NextDouble()), dir);   // U 6's shape follows the walk
    }

    /// <summary>The water's depth over the feet when they stand in one of the bodies, else -1.</summary>
    float ShallowDepth(Vector3 feet)
    {
        foreach (var go in bodies)
        {
            var r = go != null ? go.GetComponent<Renderer>() : null;
            if (r == null) continue;
            var b = r.bounds;
            if (feet.x < b.min.x || feet.x > b.max.x || feet.z < b.min.z || feet.z > b.max.z) continue;
            var depth = b.max.y - feet.y;
            if (depth > -.05f) return depth;
        }
        return -1f;
    }

    static Snapshot Save(Material m)
    {
        return new Snapshot
        {
            valid = true,
            baseColor = m.GetColor("_BaseColor"),
            smoothness = m.HasProperty("_Smoothness") ? m.GetFloat("_Smoothness") : .95f,
            bumpScale = m.HasProperty("_BumpScale") ? m.GetFloat("_BumpScale") : 1f,
            bump = m.GetTexture("_BumpMap"),
            scale = m.mainTextureScale,
            offset = m.mainTextureOffset,
        };
    }

    /// <summary>A tileable isotropic ripple normal (256 px, half floats so slopes of 1-3° do not band): 40 waves with whole
    /// wave numbers between kMin and kMax per tile, random directions and phases, flat slope spectrum scaled to the RMS
    /// slope in degrees. Packed as FrontRoomsPoolMaterials' ripple (RGB, alpha 1), so both of URP's unpacks read it.</summary>
    static Texture2D Ripple(string name, int kMin, int kMax, float rmsDegrees, int seed)
    {
        const int n = 256, waves = 40;
        var rng = new System.Random(seed);
        var kx = new int[waves];
        var ky = new int[waves];
        var phase = new float[waves];
        var count = 0;
        for (var tries = 0; count < waves && tries < 4000; tries++)
        {
            var angle = rng.NextDouble() * System.Math.PI * 2.0;
            var mag = kMin + rng.NextDouble() * (kMax - kMin);
            var x = (int)System.Math.Round(System.Math.Cos(angle) * mag);
            var y = (int)System.Math.Round(System.Math.Sin(angle) * mag);
            if (x * x + y * y < kMin * kMin) continue;
            var dup = false;
            for (var j = 0; j < count; j++) if ((kx[j] == x && ky[j] == y) || (kx[j] == -x && ky[j] == -y)) dup = true;
            if (dup) continue;
            kx[count] = x;
            ky[count] = y;
            phase[count] = (float)(rng.NextDouble() * System.Math.PI * 2.0);
            count++;
        }
        // RMS of sum of `count` cosines with slope amplitude a each = a * sqrt(count / 2)
        var amp = Mathf.Tan(rmsDegrees * Mathf.Deg2Rad) / Mathf.Sqrt(count * .5f);
        var px = new Color[n * n];
        var tau = Mathf.PI * 2f;
        for (var v = 0; v < n; v++)
        for (var u = 0; u < n; u++)
        {
            float sx = 0f, sy = 0f;
            for (var j = 0; j < count; j++)
            {
                var len = Mathf.Sqrt(kx[j] * kx[j] + ky[j] * ky[j]);
                var c = Mathf.Cos(tau * (kx[j] * u + ky[j] * v) / n + phase[j]) * amp;
                sx += c * kx[j] / len;
                sy += c * ky[j] / len;
            }
            var nrm = new Vector3(-sx, -sy, 1f).normalized;
            px[u + v * n] = new Color(nrm.x * .5f + .5f, nrm.y * .5f + .5f, nrm.z * .5f + .5f, 1f);
        }
        var t = new Texture2D(n, n, TextureFormat.RGBAHalf, true, true) { name = name, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 8, hideFlags = HideFlags.DontSave };
        t.SetPixels(px);
        t.Apply(true, false);
        return t;
    }

    // ---------------------------------------------------------------- 游戏视觉's water (pool_water)

    /// <summary>FrontRooms/Water is in the project and runs here, and the ripple loops load (70 section 5).</summary>
    bool RealAvailable()
    {
        if (realChecked) return poolWater != null;
        realChecked = true;
        var sh = Shader.Find("FrontRooms/Water");
        if (sh == null) { realProblem = "FrontRooms/Water 不在工程里"; return false; }
        if (!sh.isSupported) { realProblem = "FrontRooms/Water 在这台机器上不受支持"; return false; }
        if (!FrontRoomsWaterRipples.Ensure()) { realProblem = "水的波纹没有加载：" + FrontRoomsWaterRipples.Problem; return false; }
        poolWater = new Material(sh) { name = "Structures / pool_water (FrontRooms/Water)", hideFlags = HideFlags.DontSave };
        poolWater.renderQueue = (int)RenderQueue.Transparent;
        if (poolWater.HasProperty("_FR_WaterAlpha0")) defaultAlpha = poolWater.GetFloat("_FR_WaterAlpha0");
        poolWaterAnkle = new Material(poolWater) { name = "Structures / pool_water ankle (FrontRooms/Water)", hideFlags = HideFlags.DontSave };
        var a = AnkleAlpha[(int)look];
        poolWaterAnkle.SetFloat("_FR_WaterAlpha0", a >= 0f ? a : defaultAlpha);
        return true;
    }

    /// <summary>
    /// The current district's stand-in water becomes 游戏视觉's: every flat rectangle of a stand-in mesh (the kit's Water
    /// quads; sloped films stay) gets a body of its own under the same renderer, so it follows the part it belongs to (the
    /// SP1 flume's wet part). The nearest FrontRoomsWaterSubmerged.MaxBodies rectangles get bodies; the stand-in mesh keeps
    /// the rest. Depth below the centre picks Basin or Ankle; the ceiling above and the walls around give the room's box.
    /// </summary>
    void BuildBodies(FrontRoomsStructuresDistrict d)
    {
        TearDownBodies();
        if (d == null || d.Root == null || !RealAvailable()) return;
        var walker = host != null ? host.Walker : null;
        var feet = walker != null ? walker.transform.position : d.Root.position;
        builtAt = feet;
        RoomLights(d);

        var flats = new List<Flat>();
        foreach (var r in d.Root.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (!IsStandIn(r.sharedMaterial)) continue;
            var f = r.GetComponent<MeshFilter>();
            if (f == null || f.sharedMesh == null || !f.sharedMesh.isReadable || f.sharedMesh.subMeshCount != 1) continue;
            // bodies are axis-aligned rectangles: a turned or scaled holder keeps its stand-in
            var tr = r.transform;
            if (Quaternion.Angle(tr.rotation, Quaternion.identity) > .01f || (tr.lossyScale - Vector3.one).sqrMagnitude > 1e-6f) continue;
            FlatRects(r, f, flats);
        }
        candidates = flats.Count;
        foreach (var fl in flats)
        {
            var tr = fl.renderer.transform;
            var local = tr.InverseTransformPoint(feet);
            var near = tr.TransformPoint(new Vector3(Mathf.Clamp(local.x, fl.x0, fl.x1), fl.y, Mathf.Clamp(local.z, fl.z0, fl.z1)));
            fl.distance = (near - feet).sqrMagnitude;
        }
        flats.Sort((a, b) => a.distance.CompareTo(b.distance));
        var cap = FrontRoomsWaterSubmerged.MaxBodies;
        builtCapped = flats.Count > cap;
        if (flats.Count > cap) flats.RemoveRange(cap, flats.Count - cap);
        if (flats.Count == 0) return;

        if (walker != null && walker.View != null) FrontRoomsWaterPlanar.Viewer = walker.View;
        var districtTop = float.NegativeInfinity;
        var taken = new Dictionary<MeshFilter, HashSet<int>>();
        for (var i = 0; i < flats.Count; i++)
        {
            var fl = flats[i];
            if (!taken.TryGetValue(fl.filter, out var set)) taken[fl.filter] = set = new HashSet<int>();
            foreach (var t in fl.triangles) set.Add(t);
            Body(fl, i, d, ref districtTop);
        }
        // the stand-in draws what is left
        foreach (var pair in taken)
        {
            var f = pair.Key;
            var src = new Source { filter = f, renderer = f.GetComponent<Renderer>(), original = f.sharedMesh };
            src.wasEnabled = src.renderer.enabled;
            var tris = src.original.triangles;
            var keep = new List<int>(tris.Length);
            for (var t = 0; t < tris.Length / 3; t++)
                if (!pair.Value.Contains(t)) { keep.Add(tris[t * 3]); keep.Add(tris[t * 3 + 1]); keep.Add(tris[t * 3 + 2]); }
            if (keep.Count == 0) src.renderer.enabled = false;
            else
            {
                src.kept = Object.Instantiate(src.original);
                src.kept.name = src.original.name + " (pool_water preview: rest)";
                src.kept.hideFlags = HideFlags.DontSave;
                src.kept.SetTriangles(keep, 0);
                f.sharedMesh = src.kept;
            }
            sources.Add(src);
        }
        Niches();
    }

    /// <summary>The flat rectangles of one stand-in mesh: triangles whose three corners share a height, grouped by shared
    /// corners, kept when the group fills its bounds (a rectangle, not an L).</summary>
    static void FlatRects(Renderer r, MeshFilter f, List<Flat> into)
    {
        var mesh = f.sharedMesh;
        var v = mesh.vertices;
        var tris = mesh.triangles;
        var parent = new int[v.Length];
        for (var i = 0; i < parent.Length; i++) parent[i] = i;
        var flat = new bool[tris.Length / 3];
        for (var t = 0; t < flat.Length; t++)
        {
            int a = tris[t * 3], b = tris[t * 3 + 1], c = tris[t * 3 + 2];
            if (Mathf.Abs(v[a].y - v[b].y) > 1e-4f || Mathf.Abs(v[a].y - v[c].y) > 1e-4f) continue;
            if (Vector3.Cross(v[b] - v[a], v[c] - v[a]).sqrMagnitude < 1e-10f) continue;
            flat[t] = true;
            Union(parent, a, b);
            Union(parent, a, c);
        }
        var groups = new Dictionary<int, Flat>();
        var area = new Dictionary<int, float>();
        for (var t = 0; t < flat.Length; t++)
        {
            if (!flat[t]) continue;
            int a = tris[t * 3], b = tris[t * 3 + 1], c = tris[t * 3 + 2];
            var root = Find(parent, a);
            if (!groups.TryGetValue(root, out var g))
            {
                groups[root] = g = new Flat { renderer = r, filter = f, y = v[a].y };
                area[root] = 0f;
            }
            g.triangles.Add(t);
            area[root] += Vector3.Cross(v[b] - v[a], v[c] - v[a]).magnitude * .5f;
            g.Grow(v[a]);
            g.Grow(v[b]);
            g.Grow(v[c]);
        }
        foreach (var pair in groups)
        {
            var g = pair.Value;
            var box = (g.x1 - g.x0) * (g.z1 - g.z0);
            if (box > .01f && area[pair.Key] >= box * .98f) into.Add(g);
        }
    }

    static int Find(int[] parent, int i)
    {
        while (parent[i] != i) { parent[i] = parent[parent[i]]; i = parent[i]; }
        return i;
    }

    static void Union(int[] parent, int a, int b)
    {
        a = Find(parent, a);
        b = Find(parent, b);
        if (a != b) parent[b] = a;
    }

    void Body(Flat fl, int index, FrontRoomsStructuresDistrict d, ref float districtTop)
    {
        var src = fl.renderer;
        var tr = src.transform;
        var c = tr.TransformPoint(new Vector3((fl.x0 + fl.x1) * .5f, fl.y, (fl.z0 + fl.z1) * .5f));
        float hx = (fl.x1 - fl.x0) * .5f, hz = (fl.z1 - fl.z0) * .5f;

        var depth = Ray(c + Vector3.up * .02f, Vector3.down, 8f);
        var kind = depth >= 0f && depth - .02f < AnkleDepth ? FrontRoomsWaterSurface.Kind.Ankle : FrontRoomsWaterSurface.Kind.Basin;
        // the ceiling: the highest of five rays (centre and four inset points), else the district's top
        var room = -1f;
        for (var s = 0; s < 5; s++)
        {
            var o = c + new Vector3(s == 1 ? -hx * .5f : s == 2 ? hx * .5f : 0f, .05f, s == 3 ? -hz * .5f : s == 4 ? hz * .5f : 0f);
            var up = Ray(o, Vector3.up, 40f);
            if (up >= 0f) room = Mathf.Max(room, up + .05f);
        }
        if (room < 0f)
        {
            if (float.IsNegativeInfinity(districtTop))
            {
                districtTop = c.y + 2.9f;
                foreach (var r in d.Root.GetComponentsInChildren<Renderer>(false)) districtTop = Mathf.Max(districtTop, r.bounds.max.y);
            }
            room = Mathf.Clamp(districtTop - c.y, 2.4f, 40f);
        }
        // the walls: the box grows past the water by the mean of the two sides' gaps (FrontRoomsWaterSurface.roomMargin is symmetric)
        var eye = c + Vector3.up * 1.2f;
        var margin = new Vector2(Gap(eye, Vector3.right, Vector3.left, hx), Gap(eye, Vector3.forward, Vector3.back, hz));

        var go = new GameObject("Water body · " + index + " (" + (hx * 2f).ToString("0.0") + " x " + (hz * 2f).ToString("0.0") + " m, " + (kind == FrontRoomsWaterSurface.Kind.Ankle ? "ankle" : "basin") + ")");
        go.SetActive(false);
        go.transform.SetParent(tr, false);
        var mesh = new Mesh { name = "Structures / pool_water body " + index, hideFlags = HideFlags.DontSave };
        mesh.vertices = new[] { new Vector3(fl.x0, fl.y, fl.z0), new Vector3(fl.x0, fl.y, fl.z1), new Vector3(fl.x1, fl.y, fl.z1), new Vector3(fl.x1, fl.y, fl.z0) };
        mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
        mesh.uv = new[] { new Vector2(c.x - hx, c.z - hz), new Vector2(c.x - hx, c.z + hz), new Vector2(c.x + hx, c.z + hz), new Vector2(c.x + hx, c.z - hz) };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateBounds();
        bodyMeshes.Add(mesh);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = kind == FrontRoomsWaterSurface.Kind.Ankle ? poolWaterAnkle : poolWater;
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.renderingLayerMask = src.renderingLayerMask;
        mr.reflectionProbeUsage = src.reflectionProbeUsage;
        var ws = go.AddComponent<FrontRoomsWaterSurface>();
        ws.kind = kind;
        ws.roomHeight = room;
        ws.roomMargin = margin;
        ws.absorbScale = AbsorbOf(kind);
        // the two lamps that light this water most carry the ripple's caustics onto its floor (17 section 4.4)
        CausticLamps(c, hx + margin.x, hz + margin.y, room, out ws.causticSourceA, out ws.causticSourceB);
        ws.causticSourceRadius = CausticRadius;
        ws.depth = depth > .02f ? depth - .02f : 1f;      // the bounce's colour: deeper water sends back a bluer light
        go.SetActive(true);                               // OnEnable (when its part is active): Water layer, planar, rings, submerged
        bodies.Add(go);
        go.name += " · " + ZoneOf(src.sharedMaterial);
        ZoneFor(go);
    }

    /// <summary>The pool zone a stand-in material's water belongs to (FrontRoomsPoolMaterials.WaterOf: level, club, dark).</summary>
    static string ZoneOf(Material m)
    {
        if (m != null && m == FrontRoomsPoolMaterials.WaterOf(FrontRoomsPoolMaterials.Water.Club)) return "PoolClub";
        if (m != null && m == FrontRoomsPoolMaterials.WaterOf(FrontRoomsPoolMaterials.Water.Dark)) return "PoolDark";
        return "PoolLevel";
    }

    /// <summary>Z on: a look volume over the body's room (its box: the water widened by the room margin, from ZoneBelow under
    /// the water to the room's top), made under the body so it goes with it. Z off: none.</summary>
    void ZoneFor(GameObject go)
    {
        if (go == null) return;
        var ws = go.GetComponent<FrontRoomsWaterSurface>();
        var mf = go.GetComponent<MeshFilter>();
        string zone = "PoolLevel";
        var at = go.name.LastIndexOf(" · ", System.StringComparison.Ordinal);
        if (at >= 0) zone = go.name.Substring(at + 3);
        var old = go.transform.Find("Zone look " + zone);
        // a district whose scene already places 游戏视觉's volumes (夸张结构调研与设计's scene-4 package) keeps its own
        if (zoneLook == 0 || ws == null || mf == null || mf.sharedMesh == null || SceneZoneLook(host != null ? host.Current : null))
        {
            if (old != null) Destroy(old.gameObject);
            return;
        }
        var b = mf.sharedMesh.bounds;                          // the body's local rectangle at the water's local y
        var y = b.center.y;
        var box = new Bounds(new Vector3(b.center.x, y + (ws.roomHeight - ZoneBelow) * .5f, b.center.z),
            new Vector3(b.size.x + 2f * ws.roomMargin.x, ws.roomHeight + ZoneBelow, b.size.z + 2f * ws.roomMargin.y));
        FrontRoomsZoneLookVolume.Ensure(go.transform, zone, box, ZoneBlend, y);
    }

    /// <summary>The panel's zone line: where the scene places 游戏视觉's pool room volumes itself (FrontRoomsStructuresLook,
    /// its Q key, on from the start since 11-04 21:35), the scene's switch; elsewhere Z's state.</summary>
    string ZoneLine(FrontRoomsStructuresDistrict d)
    {
        var scene = host != null ? host.Look : null;
        if (scene == null || !scene.HasPoolRooms(d)) return ZoneNames[zoneLook];
        return scene.ZoneLookOn ? "场景的 Q 键：开，游戏视觉的泳池区域外观（Z 在这个区不另放）"
            : "场景的 Q 键：关，整个区一组固定的泳池环境光（Z 在这个区不另放）";
    }

    /// <summary>True when the district holds a zone look volume this file did not make (one not under a water body).</summary>
    bool SceneZoneLook(FrontRoomsStructuresDistrict d)
    {
        if (d == null || d.Root == null) return false;
        foreach (var v in d.Root.GetComponentsInChildren<FrontRoomsZoneLookVolume>(true))
        {
            var mine = false;
            foreach (var go in bodies) if (go != null && v.transform.IsChildOf(go.transform)) { mine = true; break; }
            if (!mine) return true;
        }
        return false;
    }

    /// <summary>Distance to the first solid surface along dir, the walker's own capsule and the water skipped; -1 if none.</summary>
    float Ray(Vector3 origin, Vector3 dir, float max)
    {
        var n = Physics.RaycastNonAlloc(origin, dir, hits, max, ~(1 << FrontRoomsWaterPlanar.WaterLayer), QueryTriggerInteraction.Ignore);
        var walker = host != null ? host.Walker : null;
        var best = -1f;
        for (var i = 0; i < n; i++)
        {
            if (walker != null && hits[i].transform.IsChildOf(walker.transform)) continue;
            if (best < 0f || hits[i].distance < best) best = hits[i].distance;
        }
        return best;
    }

    float Gap(Vector3 eye, Vector3 a, Vector3 b, float half)
    {
        float sum = 0f, n = 0f;
        var da = Ray(eye, a, 40f);
        var db = Ray(eye, b, 40f);
        if (da >= 0f) { sum += Mathf.Max(0f, da - half); n++; }
        if (db >= 0f) { sum += Mathf.Max(0f, db - half); n++; }
        return n > 0f ? Mathf.Min(sum / n, 30f) : 0f;
    }

    void TearDownBodies()
    {
        foreach (var l in niches) FrontRoomsWaterSubmerged.UnregisterNiche(l);
        niches.Clear();
        foreach (var go in bodies)
        {
            if (go == null) continue;
            go.SetActive(false);                               // unregisters now, before a rebuild in the same frame
            Destroy(go);
        }
        bodies.Clear();
        foreach (var m in bodyMeshes) if (m != null) Destroy(m);
        bodyMeshes.Clear();
        foreach (var s in sources)
        {
            if (s.filter != null && s.original != null) s.filter.sharedMesh = s.original;
            if (s.renderer != null) s.renderer.enabled = s.wasEnabled;
            if (s.kept != null) Destroy(s.kept);
        }
        sources.Clear();
        builtCapped = false;
        candidates = 0;
        if (host != null && host.Walker != null && FrontRoomsWaterPlanar.Viewer == host.Walker.View) FrontRoomsWaterPlanar.Viewer = null;
    }

    // ---------------------------------------------------------------- GlassRT

    void CycleRT()
    {
        var before = FrontRoomsGlassRT.Quality;
        var next = before == FrontRoomsGlassRTQuality.Off ? FrontRoomsGlassRTQuality.High
            : before == FrontRoomsGlassRTQuality.High ? FrontRoomsGlassRTQuality.Ultra : FrontRoomsGlassRTQuality.Off;
        FrontRoomsGlassRT.Quality = next;
        rtChanged = true;
        var now = FrontRoomsGlassRT.Quality;
        nextStats = 0f;
        if (next != FrontRoomsGlassRTQuality.Off && now == FrontRoomsGlassRTQuality.Off) host.Flash("光追开不了：" + FrontRoomsGlassRT.Status);
        else host.Flash("光追 · " + RTName(now) + (now == FrontRoomsGlassRTQuality.Off ? "（玻璃用区域反射图）" : "（只有玻璃会变；水不接收光追反射，也不在光追场景里）"));
    }

    static string RTName(FrontRoomsGlassRTQuality q)
    {
        return q == FrontRoomsGlassRTQuality.Ultra ? "超高" : q == FrontRoomsGlassRTQuality.High ? "高" : "关";
    }

    string Stats()
    {
        var sb = new StringBuilder();
        if (IsReal(look))
        {
            var tier = FrontRoomsWaterPlanar.LastTier;
            sb.Append("水面反射：").Append(tier == FrontRoomsWaterPlanar.Tier.Ultra ? "超高（镜面 1/2 分辨率，每帧）" : tier == FrontRoomsWaterPlanar.Tier.High ? "高（镜面 1/3 分辨率，隔帧）" : "探针（没有镜面）");
            if (tier != FrontRoomsWaterPlanar.Tier.Probe)
            {
                var size = FrontRoomsWaterPlanar.LastSize;
                sb.Append("  ·  镜面 ").Append(FrontRoomsWaterPlanar.LastPlanarCount).Append(" 个 ").Append(size.x).Append("×").Append(size.y);
            }
            sb.Append("  ·  水体 ").Append(bodies.Count);
            if (builtCapped) sb.Append("（共 ").Append(candidates).Append(" 块，只放最近的 ").Append(bodies.Count).Append(" 块）");
            sb.Append('\n');
        }
        var q = FrontRoomsGlassRT.Quality;
        if (q == FrontRoomsGlassRTQuality.Off) return sb.Append("光追关着：玻璃只用反射探针。").ToString();
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        var cam = host != null && host.Walker != null ? host.Walker.View : null;
        var state = cam != null ? cam.GetComponent<FrontRoomsGlassRTCamera>() : null;
        if (state != null)
        {
            sb.Append("这一帧：可见的光追接收体 ").Append(state.lastVisibleReceivers);
            if (state.lastVisibleReceivers == 0) sb.Append("（画面里没有被光追的玻璃，开关光追这一帧不会变）");
            else if (state.lastWeight < .999f) sb.Append("（光追正在淡入）");
        }
        else sb.Append("相机没有光追状态");
#endif
        FrontRoomsGlassRTStats s;
        if (FrontRoomsGlassRT.TryGetStats(out s))
            sb.Append("\n最近一次光追：玻璃像素 ").Append(s.glassPixels).Append("  ·  场景实例 ").Append(s.tlasInstances)
                .Append("  ·  光追 GPU ").Append(s.gpuMsTotal.ToString("0.00")).Append(" ms");
        return sb.ToString();
    }

    // ---------------------------------------------------------------- the panel

    void OnGUI()
    {
        if (host == null || host.Current == null || !districtHasWater) return;
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
            var body = FrontRoomsStructuresSigns.BodyFontAsset;
            if (body != null) style.font = body;
        }
        var text = "水面：" + LookNames[(int)look] + "\n波纹：" + RippleNames[ripple] + "\n水光：" + GlowNames[glow] + "\n区域外观：" + ZoneLine(host.Current) + "\n平台档：" + TierNames[tier] + "\n光追：" + RTName(FrontRoomsGlassRT.Quality) + "  ·  " + FrontRoomsGlassRT.Status + "\n" + statsLine;
        const float w = 560f;
        const string head = "水 · K 换水面  ·  U 波纹  ·  I 水光  ·  Z 区域外观  ·  P 平台档  ·  J 光追  ·  K、U、I 按住 Shift 往回";
        var hh = style.CalcHeight(new GUIContent(head), w - 24f);
        var h = style.CalcHeight(new GUIContent(text), w - 24f) + 24f + hh;
        var r = new Rect(24f, Screen.height - 44f - h, w, h);
        var old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, .55f);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = Accent;
        GUI.Label(new Rect(r.x + 12f, r.y + 8f, w - 24f, hh), head, style);
        GUI.color = Paper;
        GUI.Label(new Rect(r.x + 12f, r.y + 10f + hh, w - 24f, h - 16f - hh), text, style);
        GUI.color = old;
    }
}
