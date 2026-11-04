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
/// Nothing here runs until a caller asks for a material; no other material changes.
/// </summary>
public static class FrontRoomsPoolTile
{
    public const string Folder = "Surfaces/Pool/";
    public const string Prefix = "Prop_PoolTile_";
    public const float NoWater = -1000f;

    public static readonly string[] Kinds =
    {
        "Field", "FieldClub", "Course", "CourseClub", "Cap", "Mosaic", "MosaicClub", "Lane", "Deck", "Tread", "Riser", "Nosing", "Coping"
    };

    static readonly Dictionary<string, Material> shared = new Dictionary<string, Material>();
    static readonly Dictionary<string, Material> atWater = new Dictionary<string, Material>();
    static readonly int WaterYId = Shader.PropertyToID("_WaterY");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        shared.Clear();
        atWater.Clear();
    }

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
