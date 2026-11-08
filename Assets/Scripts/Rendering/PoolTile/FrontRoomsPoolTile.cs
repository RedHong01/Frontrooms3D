// The pool family's tile (research/bathroom_rooms/70_look_build.md, "Tile"). Game visual line.
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The pool family's tile materials: Resources/Surfaces/Pool/Prop_PoolTile_&lt;kind&gt; (FrontRooms/Surface with _FR_TILE;
/// maps written by Tools/lookdev/pool_tile/gen_pool_tile.py), their phone twins Prop_PoolTile_&lt;kind&gt;_Phone (maps at
/// half size) where FrontRoomsTrofferKit.PhoneTier says so (handheld, WebGL players), and one instance per water height
/// for the surfaces at a water's edge: the wet band, the meniscus and a club's scale line sit on _WaterY.
/// Kinds (porcelain white, Red 10-31 17:19; the club worn, the pool level pristine):
///   Field, FieldClub    glazed 150 mm wall tile, 3 mm grey-green joint (world-planar)
///   Course, CourseClub  the aqua waterline course, the same tile (world-planar)
///   Cap                 the club wainscot's top course with its bullnose (world-planar, one 150 mm row)
///   Mosaic, MosaicClub  1 in unglazed porcelain mosaic for basins and flooded floors (world-planar)
///   Lane                the same mosaic, near-black: lane lines and end-wall targets (world-planar)
///   Deck                unglazed abrasive deck tile, 150 mm, the club's (world-planar)
///   Tread, Riser        the pool stairs' white tread (abrasive) and riser (glazed), mesh UV in metres
///   Nosing              the stairs' dark nosing L (50 mm on the tread, 25 mm down the riser; Red 10-31 17:24), mesh UV
///   Coping              full bullnose coping, 300 mm units, mesh UV across the unrolled profile
///   FieldMesh, CourseMesh  the field and the course on mesh UVs in metres (the structures scene's tile and band)
///   Field108Mesh, Field75Mesh, Field50Mesh, MosaicMesh  the tile for tiled curves by radius (scene 4; 109.1, 75, 50, 25 mm), mesh UV
///   Bullnose, Cove, Gutter, WeirLip  scene 4's trim pieces: the outside-corner bullnose, the pool cove r 50, the roll-out gutter
///                       with its flush finger grate, the square weir lip with its drip groove; mesh UV across the unrolled profile
///                       (FrontRoomsStructureProfiles has the matching sections)
/// The tile's look (70_look_build.md section 13; Red 2026-11-04, the Pools tile and the dish, verbatim there): SetLook picks
/// one for every pool tile material at once (FrontRooms/Surface reads the globals Publish writes). Today = the tile as landed
/// 11-04 12:52, bit for bit; GZC = the glaze line's GZ-C as given (research/level_space/73_pool_glaze.md, Red's pick 16:4x);
/// DishShallow, DishMedium (REC, the default), DishDeep = GZ-C's glaze with Red's dish (the centre sunk, the four corners up)
/// and the Pools levers (hand-set joints, lippage, gloss and tone per tile, each tile its own cushion and waviness).
/// The dish also goes into the tone (section 13.15; Red's rule from the walls, VISUAL_STANDARD 1.2: relief in the tone as well as
/// the normal): the sunk centre a little darker, the raised corners a little lighter, a narrow cavity at the joints, from each
/// tile's own shape, fading to the tile's mean tone with the view distance. DishTone scales it (1 = the recommended amount, 0 =
/// the dish in the normal only, as landed 11-05 04:22).
/// Nothing here runs until a caller asks for a material or a look; no other material changes.
/// </summary>
public static class FrontRoomsPoolTile
{
    public const string Folder = "Surfaces/Pool/";
    public const string Prefix = "Prop_PoolTile_";
    public const float NoWater = -1000f;

    public static readonly string[] Kinds =
    {
        "Field", "FieldClub", "Course", "CourseClub", "Cap", "Mosaic", "MosaicClub", "Lane", "Deck", "Tread", "Riser", "Nosing", "Coping",
        "FieldMesh", "CourseMesh",
        // structures assets (scene 4, structures_tile_trims): tiled curves by radius (Red's ask 2) and the four trim pieces
        "Field108Mesh", "Field75Mesh", "Field50Mesh", "MosaicMesh", "Bullnose", "Cove", "Gutter", "WeirLip"
    };

    /// <summary>The tile's look. Today = the landed tile, bit for bit; the others: see the class summary.</summary>
    public enum PoolTileLook { Today = 0, GZC = 1, DishShallow = 2, DishMedium = 3, DishDeep = 4 }

    /// <summary>The look every pool tile shows until SetLook says otherwise: Red's pick after trying the looks in scene 4
    /// (2026-11-05, in the level_space line's chat: DishDeep, for now).</summary>
    public const PoolTileLook DefaultLook = PoolTileLook.DishDeep;

    /// <summary>The look the shader shows now.</summary>
    public static PoolTileLook Look { get; private set; } = DefaultLook;

    /// <summary>The glaze's slope variance in the face map (rad^2; gen_pool_tile.py make_face, "true variance"): what its mips
    /// average away goes into the roughness, so far tiles keep their sheen instead of turning to mirrors.</summary>
    public const float FaceSlopeVariance = 3.7e-5f;

    static readonly int LookId = Shader.PropertyToID("_FR_TileLook");
    static readonly int Look2Id = Shader.PropertyToID("_FR_TileLook2");
    static readonly int GlazeId = Shader.PropertyToID("_FR_TileGlaze");
    static readonly int AAId = Shader.PropertyToID("_FR_TileAA");
    static readonly int ToneId = Shader.PropertyToID("_FR_TileTone");
    static int publishedTier = -1;

    /// <summary>How strongly the dish also shows in the tile's tone (section 13.15): a multiplier on each look's own amount.
    /// 1 = the recommended amount; 0 = the dish in the normal only (the tile as landed 11-05 04:22, bit for bit); 1.5 = half again.
    /// Setting it shows at once on every pool tile (a global: no material is edited); scene 4 compares with it.</summary>
    public static float DishTone
    {
        get { return dishTone; }
        set { dishTone = Mathf.Max(0f, value); Publish(); }
    }

    /// <summary>DishTone at load: Red's pick with DishDeep (2026-11-05, the tone sheet's 1.5 column); 1 was the recommendation.</summary>
    public const float DefaultDishTone = 1.5f;
    static float dishTone = DefaultDishTone;

    /// <summary>The dish's tone is set for the hero's 0.35 m and falls with the view distance: half at 0.35 m + ToneNear (a
    /// gradient over a whole tile is what the eye sees least well up close and best at 5-15 m, where a tile wall must not turn
    /// into a quilt of pillows). Past ToneFadeEnd (from 0.35 x of it) every texel takes the tile's mean tone, the joint's cavity
    /// too. By distance, not by pixels: the same on every screen. The phone tier's are shorter (its screen shows a tile at about
    /// a third of the angle; the texels past the fade also skip the tone's arithmetic).</summary>
    public const float ToneNear = .8f, ToneNearPhone = .5f, ToneFadeEnd = 8f, ToneFadeEndPhone = 4.5f;

    /// <summary>A look's own tone at DishTone 1: x the tone per mm of the dish's height at 0.35 m (the sunk centre darker, the
    /// raised corners lighter; it follows each tile's own depth), y the darkening at the cushion's foot (the joint's cavity, a
    /// narrow band over the cushion and the grout's sides). Today: none.</summary>
    public static Vector2 ToneOf(PoolTileLook look)
    {
        switch (look)
        {
            case PoolTileLook.GZC: return new Vector2(.21f, .12f);
            case PoolTileLook.DishShallow: return new Vector2(.35f, .12f);
            case PoolTileLook.DishMedium: return new Vector2(.35f, .14f);
            case PoolTileLook.DishDeep: return new Vector2(.28f, .16f);
            default: return Vector2.zero;
        }
    }

    static readonly Dictionary<string, Material> shared = new Dictionary<string, Material>();
    static readonly Dictionary<string, Material> atWater = new Dictionary<string, Material>();
    static readonly int WaterYId = Shader.PropertyToID("_WaterY");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        shared.Clear();
        atWater.Clear();
        Look = DefaultLook;
        dishTone = DefaultDishTone;
        publishedTier = -1;
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        lastDescribedLook = DefaultLook;
#endif
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void PublishAtStart() { Publish(); }

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
    static void PublishInEditor() { Publish(); }
#endif

    /// <summary>Show a look on every pool tile material (a global switch: no material is edited). Scene 4 and the tools call it.</summary>
    public static void SetLook(PoolTileLook look)
    {
        Look = look;
        Publish();
    }

    /// <summary>The next look in the order Today, GZC, DishShallow, DishMedium, DishDeep (for a key that cycles them).</summary>
    public static PoolTileLook NextLook(PoolTileLook look)
    {
        return (PoolTileLook)(((int)look + 1) % 5);
    }

    /// <summary>Writes the current look's globals (FrontRooms/Surface's _FR_TileLook, _FR_TileLook2, _FR_TileGlaze, _FR_TileAA,
    /// _FR_TileTone). Today writes zeros: the shader's today path. Also called when the tier changes (the phone maps are half
    /// size) and when DishTone changes.</summary>
    public static void Publish()
    {
        var phone = PhoneTier;
        publishedTier = phone ? 1 : 0;
        var look = Vector4.zero;
        var look2 = Vector4.zero;
        // GZ-C's glaze (73 sections 4.1, 5.1): core 0.97, halo 0.62 at 0.2 (their tuning note: 0.4 hid the core at 0.35 m),
        // box projection on; specular AA 0.10 / 0.18
        var glaze = new Vector4(.97f, .62f, .2f, 1f);
        var aa = new Vector4(.10f, .18f, FaceSlopeVariance, phone ? 1f : 0f);
        switch (Look)
        {
            case PoolTileLook.GZC:
                // 73 section 7: tilt sd 0.20 deg, saddle sd 0.05 mm, bow 0.15-0.40 mm (80 % domed); no Pools levers
                look = new Vector4(1f, 0f, .2f, 0f);
                look2 = new Vector4(.20f, .05f, .15f, .40f);
                break;
            case PoolTileLook.DishShallow:
                look = new Vector4(2f, .25f, .85f, 1f);
                look2 = new Vector4(.20f, .08f, 0f, 0f);
                break;
            case PoolTileLook.DishMedium:
                look = new Vector4(3f, .45f, .85f, 1f);
                look2 = new Vector4(.20f, .12f, 0f, 0f);
                break;
            case PoolTileLook.DishDeep:
                look = new Vector4(4f, .75f, .85f, 1f);
                look2 = new Vector4(.22f, .18f, 0f, 0f);
                break;
            default:
                glaze = Vector4.zero;
                aa = Vector4.zero;
                break;
        }
        Shader.SetGlobalVector(LookId, look);
        Shader.SetGlobalVector(Look2Id, look2);
        Shader.SetGlobalVector(GlazeId, glaze);
        Shader.SetGlobalVector(AAId, aa);
        // the dish in tone (section 13.15): the look's own amounts times DishTone, the dish's falloff and the fade's end
        var tone = ToneOf(Look) * dishTone;
        Shader.SetGlobalVector(ToneId, new Vector4(tone.x, tone.y, phone ? ToneNearPhone : ToneNear, phone ? ToneFadeEndPhone : ToneFadeEnd));
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        // GlassRT describes a tile by the look (Today: as before; else the glaze's colour): describe the loaded ones again
        if (Look != lastDescribedLook)
        {
            lastDescribedLook = Look;
            foreach (var kv in shared) if (kv.Value != null) FrontRoomsGlassRTSystem.RefreshMaterial(kv.Value);
            foreach (var kv in atWater) if (kv.Value != null) FrontRoomsGlassRTSystem.RefreshMaterial(kv.Value);
        }
#endif
    }

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
    static PoolTileLook lastDescribedLook = DefaultLook;
#endif

    /// <summary>The phone tier, as the lamp kits take it.</summary>
    public static bool PhoneTier { get { return FrontRoomsTrofferKit.PhoneTier; } }

    /// <summary>The kind's shared material for the current tier (null, with one warning, when it is missing).</summary>
    public static Material Get(string kind)
    {
        return Get(kind, PhoneTier);
    }

    /// <summary>The kind's shared material for a given tier; a missing phone twin falls back to the desktop one.</summary>
    public static Material Get(string kind, bool phone)
    {
        if (publishedTier != (PhoneTier ? 1 : 0)) Publish();
        var key = kind + (phone ? "_Phone" : "");
        Material m;
        if (shared.TryGetValue(key, out m)) return m;
        m = Resources.Load<Material>(Folder + Prefix + key);
        if (m == null && phone) m = Resources.Load<Material>(Folder + Prefix + kind);
        if (m == null) Debug.LogWarning("[FrontRoomsPoolTile] missing Resources/" + Folder + Prefix + key);
        shared[key] = m;
        return m;
    }

    /// <summary>The kind for surfaces that meet a water at waterY (world metres): one instance per kind, tier and water
    /// height to the millimetre, with _WaterY set, so every renderer at that water shares it (the SRP batcher keeps them
    /// together). NaN or NoWater: the shared material.</summary>
    public static Material ForWater(string kind, float waterY)
    {
        return ForWater(kind, waterY, PhoneTier);
    }

    public static Material ForWater(string kind, float waterY, bool phone)
    {
        var m0 = Get(kind, phone);
        if (m0 == null || float.IsNaN(waterY) || waterY <= NoWater + 1f) return m0;
        var mm = Mathf.RoundToInt(waterY * 1000f);
        var key = kind + (phone ? "_Phone" : "") + "@" + mm;
        Material m;
        if (atWater.TryGetValue(key, out m) && m != null) return m;
        m = new Material(m0) { name = m0.name + " (water " + mm + " mm)", hideFlags = HideFlags.DontSave };
        m.SetFloat(WaterYId, mm * .001f);
        atWater[key] = m;
        return m;
    }

    /// <summary>Moves one water's line: every instance made for fromY (to the millimetre) now sits at toY. For a water that
    /// rises or falls (the water stages); the instances keep their keys, so later ForWater(fromY) calls still find them.</summary>
    public static int MoveWater(float fromY, float toY)
    {
        var tag = "@" + Mathf.RoundToInt(fromY * 1000f);
        var n = 0;
        foreach (var kv in atWater)
        {
            if (kv.Value == null || !kv.Key.EndsWith(tag)) continue;
            kv.Value.SetFloat(WaterYId, toY);
            n++;
        }
        return n;
    }

    /// <summary>Destroys the water instances (tools that rebuild a scene); shared materials stay loaded.</summary>
    public static void ReleaseInstances()
    {
        foreach (var kv in atWater)
        {
            if (kv.Value == null) continue;
            if (Application.isPlaying) Object.Destroy(kv.Value);
            else Object.DestroyImmediate(kv.Value);
        }
        atWater.Clear();
    }
}
