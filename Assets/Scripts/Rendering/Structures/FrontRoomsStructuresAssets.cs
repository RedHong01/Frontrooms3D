// Scene 4's TODAY / NEW switch, Red's five open asks and the stand-in surface table (game visual line, structures_assets
// step 2, package structures_profiles + structures_surfaces; research/structures_assets/30_plan.md sections 1, 5 and appendix A).
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The structures scene's asset switch (scene 4, Assets/Scenes/FrontRoomsStructures.unity; RULES 5b). Red's ask, 2026-11-05
/// 17:42: every asset of the structures scene to the main game's standard, research first (30_plan.md). NEW is read by every new
/// branch of the scene's builder (FrontRoomsStructuresMesh, FrontRoomsStructuresKit and the districts, through the staged
/// contracts K1-K4): with it false the scene builds exactly today's greybox, byte for byte. It is false everywhere else: the game,
/// the map's pool pass and the title stream never set it, so nothing here changes the game.
/// Scene 4's bootstrap sets it at its start (NEW by default once a group's contract has landed, 30_plan rule 1) and clears it when
/// the scene ends; a key flips it and rebuilds the current district.
/// The five asks (30_plan section 5) are Red's: each is built as the recommendation, with the alternatives a key away, so nothing
/// waits for the answer. Their state lives here so every builder reads one value.
/// </summary>
public static class FrontRoomsStructuresAssets
{
    /// <summary>District keys that stay TODAY in NEW: SP7 (Red's #3 combination pending; its files are hands off) and the pool
    /// route P (the map's own code: its NEW comes with the map contract K2m, group G2).</summary>
    public const int KeyPoolRoute = 7, KeySP7 = 9;

    static bool isNew;

    /// <summary>NEW: the production assets (bevels by material, real guards, stairs, edges, trims, the real surfaces); false:
    /// today's greybox, bit for bit. False unless scene 4 sets it.</summary>
    public static bool New
    {
        get { return isNew; }
        set
        {
            if (isNew == value) return;
            isNew = value;
            Serial++;
        }
    }

    /// <summary>What scene 4's bootstrap sets New to when it starts: true (NEW is the default once a group's contract has landed),
    /// unless the process carries FR_STRUCTURES_NEW=0 (a clone harness's TODAY run, so its builds and lamp order are the survey's
    /// own). Red's Editor and every player never set the variable.</summary>
    public static bool StartNew
    {
        get { return System.Environment.GetEnvironmentVariable("FR_STRUCTURES_NEW") != "0"; }
    }

    /// <summary>Counts every change of New or an ask (a builder that caches can compare it).</summary>
    public static int Serial { get; private set; }

    /// <summary>NEW for one district: SP7 and the pool route stay TODAY (see KeySP7, KeyPoolRoute).</summary>
    public static bool NewFor(int districtKey)
    {
        return isNew && districtKey != KeySP7 && districtKey != KeyPoolRoute;
    }

    // ---------------------------------------------------------------- Red's five asks (30_plan section 5)

    /// <summary>Ask 1, the pool edge: REC by district (SP6 rim-flow, SP1 and SP2 roll-out, P recessed, KIT all three side by
    /// side); or one system everywhere.</summary>
    public enum PoolEdge { ByDistrict = 0, RimFlow = 1, RollOut = 2, Recessed = 3 }

    /// <summary>Ask 2, tile on tight curves: REC by radius (150 mm at R 1.2 m and more, 108 / 75 mm to 0.6, 50 mm to 0.3,
    /// 25 mm below), or 150 mm everywhere with one flat facet per tile column.</summary>
    public enum CurveTile { ByRadius = 0, Uniform150 = 1 }

    /// <summary>Ask 3, rails on public floors: REC pickets (the period 6 in rule) on public floors, the two-line rail in
    /// service zones; or the two-line rail made real everywhere. Glass stays glass.</summary>
    public enum PublicRail { Pickets = 0, TwoLine = 1 }

    /// <summary>Ask 4, Red's paper at the wrong scale (V4, the spine, SP4's 11.9 m walls): today (the default: it is Red's
    /// pick), lot steps per wall, or lot steps and butt joins above a roll's length. Built by structures_scale_terms (G3).</summary>
    public enum PaperScale { Today = 0, LotSteps = 1, LotStepsAndJoins = 2 }

    /// <summary>Ask 5, the game's lamp modes in scene 4: REC the game's tier-1 modes (pool fluorescents steady until the 0d
    /// photosafety fix), or every lamp steady as today.</summary>
    public enum LampModes { Game = 0, Steady = 1 }

    public static PoolEdge Edge = PoolEdge.ByDistrict;
    public static CurveTile Curves = CurveTile.ByRadius;
    public static PublicRail Rails = PublicRail.Pickets;
    public static PaperScale Paper = PaperScale.Today;
    public static LampModes Lamps = LampModes.Game;

    /// <summary>Every ask back to its default (the recommendation; ask 4 off).</summary>
    public static void ResetAsks()
    {
        Edge = PoolEdge.ByDistrict;
        Curves = CurveTile.ByRadius;
        Rails = PublicRail.Pickets;
        Paper = PaperScale.Today;
        Lamps = LampModes.Game;
        Serial++;
    }

    /// <summary>What a district's build depends on (the switch and the asks that change geometry: 1-3): a district built under
    /// another signature is built again before it is shown.</summary>
    public static int Signature(int districtKey)
    {
        if (!NewFor(districtKey)) return 0;
        return 1 + ((int)Edge << 1) + ((int)Curves << 4) + ((int)Rails << 5);
    }

    /// <summary>The next value of an ask (1-5) on its key; returns the Chinese line for the flash.</summary>
    public static string Cycle(int ask)
    {
        switch (ask)
        {
            case 1: Edge = (PoolEdge)(((int)Edge + 1) % 4); break;
            case 2: Curves = Curves == CurveTile.ByRadius ? CurveTile.Uniform150 : CurveTile.ByRadius; break;
            case 3: Rails = Rails == PublicRail.Pickets ? PublicRail.TwoLine : PublicRail.Pickets; break;
            case 4: Paper = (PaperScale)(((int)Paper + 1) % 3); break;
            case 5: Lamps = Lamps == LampModes.Game ? LampModes.Steady : LampModes.Game; break;
            default: return "";
        }
        Serial++;
        return AskLine(ask);
    }

    /// <summary>The pool edge system a district builds (ask 1): its recommended one unless Red's key picked one for all.</summary>
    public static PoolEdge EdgeFor(int districtKey)
    {
        if (Edge != PoolEdge.ByDistrict) return Edge;
        switch (districtKey)
        {
            case 6: return PoolEdge.RimFlow;
            case 1:
            case 2: return PoolEdge.RollOut;
            case KeyPoolRoute: return PoolEdge.Recessed;
            default: return PoolEdge.RollOut;
        }
    }

    /// <summary>Tile pitch (m) for a tiled curve of radius r (ask 2): 0.15 at R 1.2 m and over, 0.10909 (4 1/4 in, 11 to 1.2 m)
    /// to R 0.85, 0.075 to R 0.6, 0.05 to R 0.3, 0.025 below; Uniform150 keeps 0.15 everywhere.</summary>
    public static float CurvePitch(float radius)
    {
        if (Curves == CurveTile.Uniform150 || radius >= 1.2f) return .15f;
        if (radius >= .85f) return 1.2f / 11f;
        if (radius >= .6f) return .075f;
        if (radius >= .3f) return .05f;
        return .025f;
    }

    /// <summary>The pool tile kind that carries a pitch from CurvePitch (FrontRoomsPoolTile kinds, mesh UVs in metres).</summary>
    public static string CurveTileKind(float pitch)
    {
        if (pitch > .14f) return "FieldMesh";
        if (pitch > .1f) return "Field108Mesh";
        if (pitch > .07f) return "Field75Mesh";
        if (pitch > .04f) return "Field50Mesh";
        return "MosaicMesh";
    }

    /// <summary>The flash line for an ask's state (Chinese, as Red reads the scene).</summary>
    public static string AskLine(int ask)
    {
        switch (ask)
        {
            case 1:
                switch (Edge)
                {
                    case PoolEdge.RimFlow: return "问题 1 · 池边：全部用齐平溢流（A）";
                    case PoolEdge.RollOut: return "问题 1 · 池边：全部用翻出式（B）";
                    case PoolEdge.Recessed: return "问题 1 · 池边：全部用嵌入式溢水槽（C）";
                    default: return "问题 1 · 池边：按推荐，每个区一种（SP6 齐平、SP1 和 SP2 翻出式、P 嵌入式；KIT 三种并排）";
                }
            case 2:
                return Curves == CurveTile.ByRadius
                    ? "问题 2 · 弯墙的瓷砖：按推荐，越弯砖越小（150 / 108 / 75 / 50 / 25 毫米）"
                    : "问题 2 · 弯墙的瓷砖：全部 150 毫米，一列砖一个平面（B）";
            case 3:
                return Rails == PublicRail.Pickets
                    ? "问题 3 · 公共楼层的护栏：按推荐，加竖条（B）；机房和服务区仍是两道横杆"
                    : "问题 3 · 公共楼层的护栏：两道横杆，只把它做真（A）";
            case 4:
                switch (Paper)
                {
                    case PaperScale.LotSteps: return "问题 4 · 高墙上的墙纸：每面墙一个批次色差（B）";
                    case PaperScale.LotStepsAndJoins: return "问题 4 · 高墙上的墙纸：批次色差加横向接缝（C，推荐）";
                    default: return "问题 4 · 高墙上的墙纸：和现在一样（默认关：这是你选过的墙纸）";
                }
            case 5:
                return Lamps == LampModes.Game
                    ? "问题 5 · 灯：按推荐，像游戏里一样会闪、会暗、会坏（泳池的荧光灯先保持常亮）"
                    : "问题 5 · 灯：全部常亮（和现在一样）";
        }
        return "";
    }

    // ---------------------------------------------------------------- the stand-in surfaces (structures_surfaces, appendix A)

    /// <summary>
    /// The runtime stand-in materials (FrontRoomsPoolMaterials.Lit / Glow keys, named "Structures / key") that a real surface of
    /// the game replaces in NEW as a straight swap (same geometry). Stand-ins that a kit replaces (props, lamps, the flume, the
    /// water) are their packages' (P7, P10-P20) and stay here unmapped; the scene's own signs, pads and marks stay as they are.
    /// </summary>
    static readonly string[,] SurfaceTable =
    {
        // construction greys
        { "SP3 plenum lining", "Plenum_Dark" },
        { "SP5 sealed concrete (greybox)", "Stair_Fill" },
        { "access floor panel (greybox)", "Prop_AccessFloor" },
        { "beam paint", "Prop_Fireproofing" },
        { "chain link", "Prop_WireMesh" },
        { "guard wall", "Wall_Compound" },
        { "steel putty", "Prop_SteelPutty" },
        { "painted rail", "Painted_Metal" },
        { "oak cap", "Prop_WoodOak" },
        { "steel", "Prop_StainlessSatin" },
        // prop greys whose shape stays until their kit lands (a real finish on the stand-in's box)
        { "cctv beige", "Prop_PlasticBeige" },
        { "crt beige", "Prop_PlasticBeige" },
        { "head beige", "Prop_PlasticBeige" },
        { "computer cabinet grey", "Prop_SteelPutty" },
        { "desk", "Prop_LaminateBeige" },
        { "chair", "Prop_FabricChair" },
        { "locker", "Prop_SteelBeige" },
        { "rubber black", "Prop_Rubber" },
        { "rubber wedge", "Prop_Rubber" },
        { "rules board", "Prop_LaminateGrey" },
        { "stand-in dark", "Prop_PlasticBlack" },
        { "start light head", "Prop_SteelBlack" },
        { "globe glass", "Prop_FrostedGlass" },
    };

    static Dictionary<string, string> surfaceMap;
    static readonly Dictionary<string, Material> resolved = new Dictionary<string, Material>();

    /// <summary>The real surface that replaces a stand-in key in NEW (null: none, or the surface is not in this build).</summary>
    public static Material Surface(string standInKey)
    {
        if (string.IsNullOrEmpty(standInKey)) return null;
        if (surfaceMap == null)
        {
            surfaceMap = new Dictionary<string, string>();
            for (var i = 0; i < SurfaceTable.GetLength(0); i++) surfaceMap[SurfaceTable[i, 0]] = SurfaceTable[i, 1];
        }
        string name;
        if (!surfaceMap.TryGetValue(standInKey, out name)) return null;
        return Load(name);
    }

    // A surface by name, its _Phone twin on the phone tier where there is one (the structures surfaces have them).
    static Material Load(string name)
    {
        var phone = FrontRoomsTrofferKit.PhoneTier;
        var key = phone ? name + "_Phone" : name;
        Material m;
        if (resolved.TryGetValue(key, out m) && m != null) return m;
        m = phone ? FrontRoomsSurfaces.TryGet(name + "_Phone") : null;
        if (m == null) m = FrontRoomsSurfaces.TryGet(name);
        resolved[key] = m;
        return m;
    }

    /// <summary>The structures trim sheet (Prop_StructTrim, its phone twin on the phone tier; null before it lands).</summary>
    public static Material TrimSheet()
    {
        return Load("Prop_StructTrim");
    }

    /// <summary>A stand-in material's real replacement in NEW (by its runtime name "Structures / key"), or the material itself.
    /// Only lit stand-ins are swapped: glows, water, the scene's UI and every asset material pass through unchanged, but for
    /// SceneLit (a stairwell surface's scene-lit copy).</summary>
    public static Material Real(Material m)
    {
        if (m == null) return null;
        var n = m.name;
        const string Prefix = "Structures / ";
        if (n != null && n.StartsWith(Prefix, System.StringComparison.Ordinal))
        {
            var real = Surface(n.Substring(Prefix.Length));
            if (real != null) m = real;
        }
        return SceneLit(m);
    }

    const string OwnLightsKeyword = "_FR_OWN_LIGHTS";
    static readonly Dictionary<Material, Material> sceneLit = new Dictionary<Material, Material>();

    /// <summary>
    /// The stairwell's own-lights surfaces (the Stair_* family: _FR_OWN_LIGHTS skips every light that reaches over 6 m, so the
    /// map's lamps do not shine through the stairwell's walls) take only lamps of 6 m or less. Scene 4's lamps reach 10-16 m, so
    /// a slab section, a waist soffit, a stair band or the dock's floor in them read black under a lit ceiling (G1 render stage,
    /// 2026-11-06: SP5's dock floor, KIT's slab sections). In NEW they take a copy without the keyword, made once per material and
    /// used by scene 4's builder only; TODAY and the game keep the shared material.
    /// </summary>
    public static Material SceneLit(Material m)
    {
        if (m == null || !m.IsKeywordEnabled(OwnLightsKeyword)) return m;
        Material c;
        if (sceneLit.TryGetValue(m, out c) && c != null) return c;
        c = new Material(m) { name = m.name + " (scene 4, scene lights)" };
        c.DisableKeyword(OwnLightsKeyword);
        if (c.HasProperty("_OwnLights")) c.SetFloat("_OwnLights", 0f);
        sceneLit[m] = c;
        return c;
    }

    static readonly int TileLotId = Shader.PropertyToID("_TileLot");
    static readonly Dictionary<Material, Material> farAA = new Dictionary<Material, Material>();

    /// <summary>
    /// A pool tile material's copy whose tile specular AA may reach 'ceiling' (roughness) where the mesh normal turns within a
    /// pixel: FrontRooms/Surface reads _TileLot.z as that ceiling (0, every material of the game, keeps the look's own 0.18), so
    /// a small round far away spreads its highlight over the pixel it lies in instead of aliasing into a dotted line (G1 review
    /// FIX 5: the nosing L's r 8 nose at 8-15 m). Up close the round spans many pixels, its normal turns little per pixel and
    /// nothing changes. Made once per material, used by scene 4's builder only (NEW); TODAY and the game keep the shared one.
    /// </summary>
    public static Material FarAA(Material m, float ceiling)
    {
        if (m == null || !m.HasProperty(TileLotId)) return m;
        Material c;
        if (farAA.TryGetValue(m, out c) && c != null) return c;
        c = new Material(m) { name = m.name + " (scene 4, far AA)" };
        var lot = c.GetVector(TileLotId);
        lot.z = Mathf.Clamp01(ceiling);
        c.SetVector(TileLotId, lot);
        farAA[m] = c;
        return c;
    }

    /// <summary>Every stand-in key the table maps and its surface (docs and tests).</summary>
    public static IEnumerable<KeyValuePair<string, string>> SurfacePairs()
    {
        for (var i = 0; i < SurfaceTable.GetLength(0); i++) yield return new KeyValuePair<string, string>(SurfaceTable[i, 0], SurfaceTable[i, 1]);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        isNew = false;
        Serial = 0;
        Edge = PoolEdge.ByDistrict;
        Curves = CurveTile.ByRadius;
        Rails = PublicRail.Pickets;
        Paper = PaperScale.Today;
        Lamps = LampModes.Game;
        resolved.Clear();
        sceneLit.Clear();
        farAA.Clear();
    }
}
