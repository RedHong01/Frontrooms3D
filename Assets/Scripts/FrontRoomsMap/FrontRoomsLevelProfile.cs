using FrontRooms.Map;
using UnityEngine;

/// <summary>
/// One level's tunable numbers as an asset (Assets/Levels/FrontRoomsLevel0.asset):
/// how the maze is generated, how it streams, and what it costs. The main
/// game, the map test scene, the debug window and the 100-seed check all read
/// the same asset, so a change here is what the next run plays.
/// The geometry it is built from is fixed by <see cref="ModuleUnits"/>.
/// Edits made during Play are kept (it is an asset). The running map takes the
/// light, shadow, streaming, shift, key and dressing numbers at once
/// (FrontRoomsMapWorld.ApplyLive) and the tier table on the next frame; the
/// generation numbers and the module list take effect on the next run.
/// </summary>
[CreateAssetMenu(menuName = "FrontRooms/Level profile", fileName = "FrontRoomsLevel")]
public sealed class FrontRoomsLevelProfile : ScriptableObject
{
    public const string DefaultPath = "Assets/Levels/FrontRoomsLevel0.asset";

    [Tooltip("Generation numbers: zone heights, maze, rooms, exits, Office share. Its seed is used by the test scene, the debug window and the 100-seed check; the game uses Run Seed.")]
    public MapSettings generation = new MapSettings();

    [Header("Run")]
    [Tooltip("0 picks a new maze every run. Any other value replays that maze.")]
    public int runSeed = 0;
    [Range(1, 3), Tooltip("Chunks kept built around the player in each direction. 2 = 5 x 5 chunks, 120 m across. Also sets the camera's far plane (radius x 24 m - 2 m).")]
    public int buildRadius = 2;
    [Range(1, 4), Tooltip("New chunks built per frame while streaming. The first build around the spawn is always complete.")]
    public int chunksPerFrame = 1;
    [Min(5f), Tooltip("A chunk the player has been away from for at least this long comes back with a shifted interior.")]
    public float shiftAfterSeconds = 30f;
    [Tooltip("Off: doors open without the zone key. Keys are still collected and shown.")]
    public bool doorsNeedKeys = false;

    [Header("Light budget")]
    [Min(4f), Tooltip("Fixture lights beyond this distance are switched off (they fade over the last 3 m).")]
    public float lightRadius = 16f;
    [Min(0f), Tooltip("Only lamps this close cast shadows (one lamp in three).")]
    public float shadowRadius = 9f;

    [Header("Room modules")]
    [Tooltip("Designer rooms (Assets/Levels/Modules) the generator may place into carved rooms they fit. The chance and tier are in Generation (Module Chance, Module Tier).")]
    public FrontRoomsRoomModule[] modules = new FrontRoomsRoomModule[0];

    [Header("Floor-1 start group (A26)")]
    [Tooltip("The station module (the Front Desk) floor 1's start group is built from, behind the start door. Never a random pick: it is not in Modules. Empty: today's start (the title's root search, the EXIT 14-18 cells away).")]
    public FrontRoomsRoomModule stationModule;

    [Header("Difficulty tiers (DP08 proposal)")]
    [Tooltip("The run's tier rises every Zones Per Tier new zones, or after Stall Seconds without one. Each row scales the Relay's tuning (on the FrontRooms 3D object) and sets the lamp odds and module tier of chunks generated at that tier. Edits apply at once, also during Play.")]
    public FrontRoomsTierRules tiers = new FrontRoomsTierRules();

    [Header("Dressing")]
    [Tooltip("Furnish Office-zone rooms with FrontRoomsOfficeKit when it exists.")]
    public bool dressOffices = true;
    [Range(0f, 1f), Tooltip("Chance that a hall of at least 4 x 4 cells gets a FrontRoomsFurniturePile (at least 0.6 in tall zones).")]
    public float pileChance = .35f;

    static FrontRoomsLevelProfile fallback;

    /// <summary>The code defaults, for when no asset is assigned. Never saved.</summary>
    public static FrontRoomsLevelProfile Default
    {
        get
        {
            if (fallback == null)
            {
                fallback = CreateInstance<FrontRoomsLevelProfile>();
                fallback.name = "FrontRoomsLevel (defaults)";
                fallback.hideFlags = HideFlags.DontSave;
            }
            return fallback;
        }
    }

    /// <summary>A copy of the generation numbers with the given seed; the asset itself is never changed.</summary>
    public MapSettings Generation(int seed)
    {
        var copy = (generation ?? new MapSettings()).Clone();
        copy.seed = seed;
        return copy;
    }

    /// <summary>The modules' data, in a stable order (by asset name), for the generator. Never the assets' own data objects.</summary>
    public System.Collections.Generic.List<RoomModuleData> ModuleData()
    {
        // Each asset once (a duplicate would double its weight); ties in name keep the list's order.
        var seen = new System.Collections.Generic.HashSet<FrontRoomsRoomModule>();
        var list = new System.Collections.Generic.List<(string, RoomModuleData)>();
        if (modules != null)
            foreach (var m in modules)
                if (m != null && m.data != null && seen.Add(m)) list.Add((m.name, m.data.Clone()));
        var order = new System.Collections.Generic.List<(string, RoomModuleData)>(list);
        list.Sort((a, b) => { var c = string.CompareOrdinal(a.Item1, b.Item1); return c != 0 ? c : order.IndexOf(a).CompareTo(order.IndexOf(b)); });
        var result = new System.Collections.Generic.List<RoomModuleData>();
        foreach (var (_, d) in list) result.Add(d);
        return result;
    }

    /// <summary>A copy of the station module's data (A26), or null when the profile has none. Never the asset's own data object.</summary>
    public RoomModuleData StationData() => stationModule != null && stationModule.data != null ? stationModule.data.Clone() : null;

    /// <summary>Raised when the asset is edited (also during Play): a running map applies the numbers it can take live (FrontRoomsMapWorld.ApplyLive).</summary>
    public static event System.Action<FrontRoomsLevelProfile> Changed;

    void OnValidate()
    {
        if (generation == null) generation = new MapSettings();
        shadowRadius = Mathf.Min(shadowRadius, lightRadius);
        if (tiers == null) tiers = new FrontRoomsTierRules();
        tiers.Normalize();
        Changed?.Invoke(this);
    }
}

/// <summary>One difficulty tier: multipliers on the Relay's base tuning, and the Auto lamp odds of chunks generated at this tier.</summary>
[System.Serializable]
public sealed class FrontRoomsTier
{
    [Min(.1f), Tooltip("Chase speed, times the base (4.2 m/s).")]
    public float chaseSpeed = 1f;
    [Min(.1f), Tooltip("Hearing, times the base (1.4).")]
    public float hearing = 1f;
    [Min(.05f), Tooltip("Door break time, times the base (2.5 s). Blows land every 0.5 s, so 2.5 s is 5 blows, 1.3 s is 3.")]
    public float breakDoor = 1f;
    [Min(.1f), Tooltip("Search: the listening pause at each spot and the give-up time, times the base.")]
    public float search = 1f;
    [Header("Auto lamps (the rest are dim)")]
    [Range(0f, 1f)] public float lampSteady = .62f;
    [Range(0f, 1f)] public float lampStutter = .20f;
    [Range(0f, 1f)] public float lampFailing = .10f;
    [Range(0f, 1f)] public float lampDead = .05f;

    public FrontRoomsTier() { }

    public FrontRoomsTier(float chase, float hear, float breakDoor, float search, float steady, float stutter, float failing, float dead)
    {
        chaseSpeed = chase; hearing = hear; this.breakDoor = breakDoor; this.search = search;
        lampSteady = steady; lampStutter = stutter; lampFailing = failing; lampDead = dead;
    }

    /// <summary>Scale a copy of the base tuning (never the base itself).</summary>
    public void ApplyTo(FrontRoomsHunterTuning t)
    {
        t.chaseSpeed *= chaseSpeed;
        t.hearing *= hearing;
        t.breakDoorSeconds *= breakDoor;
        t.searchLookSeconds *= search;
        t.searchMaxSeconds *= search;
    }

    /// <summary>An Auto lamp's mode from a roll in [0, 1): 0 steady, 1 stutter, 2 failing, 3 dead, 4 dim (FrontRoomsMapWorld fixtures).</summary>
    public int LampMode(float roll)
    {
        float a = lampSteady, b = a + lampStutter, c = b + lampFailing, d = c + lampDead;
        return roll < a ? 0 : roll < b ? 1 : roll < c ? 2 : roll < d ? 3 : 4;
    }
}

/// <summary>
/// One tier's Relay v2 numbers (RELAY_PURSUIT_REDESIGN sections 4-8, 11, 12), as absolute values, not multipliers.
/// Data only: read only under FRONTROOMS_RELAY_V2. v1 reads FrontRoomsTier (ApplyTo) alone, and ApplyTo reads
/// none of these. Every number is a starting point for playtest (SP); the fixed ones (lock-on hold, lose
/// threshold, sight ranges, W bands) are not per tier and are not here.
/// </summary>
[System.Serializable]
public sealed class FrontRoomsTierV2
{
    [Header("Director (RELAY 4, 6)")]
    [Tooltip("Attention that calls the Relay (0-100).")]
    public float callThreshold = 60f;
    [Tooltip("Attention lost per second, 3 s after the last gain.")]
    public float attentionDecay = 3f;
    [Tooltip("Pressure that ends an encounter (SUSTAIN, then FADE).")]
    public float peakThreshold = 100f;
    [Tooltip("Seconds on the map before FADE; the hard end is 15 s later.")]
    public float onMapCap = 75f;
    [Tooltip("Re-dispatches (handoffs, on-map device calls) per encounter.")]
    public int redispatchBudget = 1;
    [Tooltip("Lock-ons per encounter (chases of 5 s or more) before FADE.")]
    public int lockOnCap = 2;
    [Tooltip("Seconds one chase may last before FADE (a logged backstop).")]
    public float chaseCap = 25f;
    [Tooltip("RELAX after an Away (half after an unbuilt-cell Away).")]
    public float relaxSeconds = 100f;
    [Tooltip("BUILD-UP seconds with no call before the quiet-player sweep.")]
    public float drySpellSeconds = 240f;
    [Tooltip("How long a sweep patrols.")]
    public float sweepSeconds = 40f;
    [Header("Hunter (RELAY 5, 7, 8, 11)")]
    [Tooltip("Arrive: seconds from placement to the step-in.")]
    public float arriveDelay = 3f;
    [Tooltip("Spotting time multiplier (the meter fills in (0.30 + 0.04 d) x this).")]
    public float spotMul = 1f;
    [Tooltip("Hearing multiplier on the v2 loudness (base 1.0).")]
    public float hearing = 1f;
    [Tooltip("Chase speed, m/s (v2.0 caps it at 4.4).")]
    public float chaseSpeed = 4.2f;
    [Tooltip("Seconds to break a door (blows every 0.5 s).")]
    public float breakDoorSeconds = 2.5f;
    [Tooltip("The full search's give-up time, seconds.")]
    public float searchSeconds = 15f;
    [Tooltip("Spots in the full search.")]
    public int searchSpots = 3;
    [Tooltip("Patrol after the give-up beat (0: none), seconds.")]
    public float patrolSeconds = 0f;
    [Header("Level (RELAY 6, 12)")]
    [Tooltip("Trigger budget: one device per this many zones generated at the tier.")]
    public float zonesPerDevice = 3f;
}

/// <summary>When the run's tier rises, and what each tier does (DP08: 5 tiers, every 4 zones or 2 minutes).</summary>
[System.Serializable]
public sealed class FrontRoomsTierRules
{
    [Min(1), Tooltip("New zones per tier: zones 1-4 are tier 1, 5-8 tier 2, and so on.")]
    public int zonesPerTier = 4;
    [Min(10f), Tooltip("Seconds without a new zone (out of the start rooms) before the tier rises anyway.")]
    public float stallSeconds = 120f;
    [Tooltip("Tier 1 first. Tier 1 is today's game: the base tuning and lamp odds.")]
    public FrontRoomsTier[] tiers = Defaults();
    [Tooltip("Relay v2's rows (RELAY_PURSUIT_REDESIGN sections 4-12), tier 1 first. Inert until FRONTROOMS_RELAY_V2: today's game reads only the rows above.")]
    public FrontRoomsTierV2[] v2 = DefaultsV2();

    // The v2 columns as the spec's tables give them, tier 1 to 5 (SP).
    static readonly float[] V2Call = { 60f, 56f, 52f, 48f, 45f };
    static readonly float[] V2Decay = { 3f, 2.75f, 2.5f, 2.25f, 2f };
    static readonly float[] V2Peak = { 100f, 105f, 110f, 115f, 120f };
    static readonly float[] V2OnMapCap = { 75f, 80f, 90f, 100f, 110f };
    static readonly int[] V2Redispatch = { 1, 1, 2, 2, 3 };
    static readonly int[] V2LockOnCap = { 2, 2, 2, 3, 3 };
    static readonly float[] V2ChaseCap = { 25f, 27f, 30f, 32f, 35f };
    static readonly float[] V2Relax = { 100f, 90f, 80f, 70f, 60f };
    static readonly float[] V2DrySpell = { 240f, 210f, 180f, 160f, 140f };
    static readonly float[] V2Sweep = { 40f, 45f, 50f, 55f, 60f };
    static readonly float[] V2ArriveDelay = { 3f, 2.75f, 2.5f, 2.25f, 2f };
    static readonly float[] V2SpotMul = { 1f, .95f, .9f, .85f, .8f };
    static readonly float[] V2Hearing = { 1f, 1.07f, 1.14f, 1.21f, 1.3f };
    static readonly float[] V2Chase = { 4.2f, 4.3f, 4.4f, 4.4f, 4.4f };
    static readonly float[] V2Break = { 2.5f, 2.3f, 2.1f, 1.9f, 1.9f };
    static readonly float[] V2Search = { 15f, 15f, 16f, 17f, 18f };
    static readonly int[] V2SearchSpots = { 3, 3, 3, 4, 4 };
    static readonly float[] V2Patrol = { 0f, 0f, 15f, 25f, 35f };
    static readonly float[] V2ZonesPerDevice = { 3f, 2.5f, 2f, 1.75f, 1.5f };

    /// <summary>Relay v2's five rows (T1-T5) with the spec's numbers.</summary>
    public static FrontRoomsTierV2[] DefaultsV2()
    {
        var rows = new FrontRoomsTierV2[V2Call.Length];
        for (var i = 0; i < rows.Length; i++)
            rows[i] = new FrontRoomsTierV2
            {
                callThreshold = V2Call[i], attentionDecay = V2Decay[i], peakThreshold = V2Peak[i], onMapCap = V2OnMapCap[i],
                redispatchBudget = V2Redispatch[i], lockOnCap = V2LockOnCap[i], chaseCap = V2ChaseCap[i], relaxSeconds = V2Relax[i],
                drySpellSeconds = V2DrySpell[i], sweepSeconds = V2Sweep[i], arriveDelay = V2ArriveDelay[i], spotMul = V2SpotMul[i],
                hearing = V2Hearing[i], chaseSpeed = V2Chase[i], breakDoorSeconds = V2Break[i], searchSeconds = V2Search[i],
                searchSpots = V2SearchSpots[i], patrolSeconds = V2Patrol[i], zonesPerDevice = V2ZonesPerDevice[i],
            };
        return rows;
    }

    /// <summary>Relay v2's row for tier n (1-based), clamped to its table. Read only under FRONTROOMS_RELAY_V2.</summary>
    public FrontRoomsTierV2 V2At(int tier)
    {
        Normalize();
        return v2[Mathf.Clamp(tier, 1, v2.Length) - 1];
    }

    /// <summary>
    /// The director's per-tier rows (RELAY 4, 6) from the v2 table, in the policy's layout (index 0 unused, tiers
    /// from 1), so the tier table stays the one place a tier's number lives. The other director numbers are
    /// FrontRoomsRelayDirectorRules' own defaults.
    /// </summary>
    public FrontRoomsRelayDirectorRules DirectorRules()
    {
        Normalize();
        var n = v2.Length + 1;
        var r = new FrontRoomsRelayDirectorRules
        {
            callThreshold = new float[n], attentionDecay = new float[n], peakThreshold = new float[n], onMapCap = new float[n],
            redispatchBudget = new int[n], lockOnCap = new int[n], chaseCap = new float[n], relaxSeconds = new float[n],
            drySpellSeconds = new float[n],
        };
        for (var t = 1; t < n; t++)
        {
            var row = v2[t - 1];
            r.callThreshold[t] = row.callThreshold;
            r.attentionDecay[t] = row.attentionDecay;
            r.peakThreshold[t] = row.peakThreshold;
            r.onMapCap[t] = row.onMapCap;
            r.redispatchBudget[t] = row.redispatchBudget;
            r.lockOnCap[t] = row.lockOnCap;
            r.chaseCap[t] = row.chaseCap;
            r.relaxSeconds[t] = row.relaxSeconds;
            r.drySpellSeconds[t] = row.drySpellSeconds;
        }
        return r;
    }

    public static FrontRoomsTier[] Defaults() => new[]
    {
        new FrontRoomsTier(1f, 1f, 1f, 1f, .62f, .20f, .10f, .05f),
        new FrontRoomsTier(1.07f, 1.15f, .88f, 1.15f, .57f, .21f, .12f, .06f),
        new FrontRoomsTier(1.14f, 1.3f, .76f, 1.3f, .52f, .22f, .14f, .08f),
        new FrontRoomsTier(1.21f, 1.45f, .64f, 1.45f, .46f, .24f, .16f, .10f),
        new FrontRoomsTier(1.29f, 1.6f, .52f, 1.6f, .40f, .25f, .18f, .12f),
    };

    public int MaxTier => tiers == null || tiers.Length == 0 ? 1 : tiers.Length;

    /// <summary>Tier n (1-based), clamped to the table.</summary>
    public FrontRoomsTier At(int tier)
    {
        Normalize();
        return tiers[Mathf.Clamp(tier, 1, tiers.Length) - 1];
    }

    /// <summary>The tier the zone count alone gives (1 for the first Zones Per Tier zones).</summary>
    public int TierForZones(int zones) => Mathf.Clamp(1 + Mathf.Max(0, zones - 1) / Mathf.Max(1, zonesPerTier), 1, MaxTier);

    public void Normalize()
    {
        if (tiers == null || tiers.Length == 0) tiers = Defaults();
        for (var i = 0; i < tiers.Length; i++) if (tiers[i] == null) tiers[i] = new FrontRoomsTier();
        zonesPerTier = Mathf.Max(1, zonesPerTier);
        stallSeconds = Mathf.Max(10f, stallSeconds);
        // v2's rows (inert until FRONTROOMS_RELAY_V2): a profile saved before they existed gets the spec's.
        if (v2 == null || v2.Length == 0) v2 = DefaultsV2();
        for (var i = 0; i < v2.Length; i++) if (v2[i] == null) v2[i] = new FrontRoomsTierV2();
    }
}
