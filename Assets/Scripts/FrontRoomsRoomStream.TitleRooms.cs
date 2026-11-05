using System;
using UnityEngine;

/// <summary>
/// Title rooms (line game visual, title_stream Build, 2026-11-04). Red, 11-04 09:13:40 in this line's chat: "然后我需要你在最开始无限流
/// 房间里，去构思新的房间 Office/run/pool/等已经被设计好了之后我希望你在这个无限流房间的里增加上这些不同的房间作为最开始无限流房间的设计".
/// The title corridor passes through the room families the project has designed, each as a vignette at the stream's own
/// module (FrontRoomsTitleVignettes), placed by a programme:
///   Today    every room a Level 0 replica (the title as it ships; the default until Red picks, so nothing changes).
///   Tour     rooms 0-1 Level 0 (the logo and its relay-S, the T6 TV), then OFFICE, L0, RUN!, L0, WAITING, L0, POOL, L0, repeat:
///            8 rooms, 83.5 s (6 rooms, 62.6 s with PoolStopsReady off).
///   Drift    rooms 0-1 Level 0, then a seeded shuffle bag of the families with one or two Level 0 rooms between them; every
///            family once a bag, a family never within five rooms of itself (one copy in the bank is enough).
///   Descent  rooms 0-1 Level 0, then OFFICE, RUN!, LOW, NIGHT POST, (CLUB), POOL, DARK POOL, then WAKE (a Level 0 room lit
///            at once as its door opens), repeat.
/// What stays as it is in every programme: RoomRule (title rooms keep Lobby, so the Hunter, the map, the legacy playable
/// cycle and its tests see nothing new), the 12 m module and its door lines, the walnut pairs and their 0.9 s, the 1.15 m/s
/// crawl, rooms 0 and 1, the TV, the logo clock, the handoff in place. New with a programme (never with Today):
///   - one vignette per family, built once at Initialize (the bank) and moved into a slot while it is sealed (MaintainPool);
///     the legacy profile variants are not built (the shipped title stops paying for four hidden variants a slot);
///   - a room behind a shut title door turns its lights off (at most the current room and the next are lit: 24 lights, was up
///     to 48); at Space they come back (the player may walk back);
///   - the start door always opens from a Level 0 room (the "airlock"): Space in a family room hands the player that room and
///     the sealed Level 0 room past it, whose far door is the start door (Descent turns that sealed room to Level 0 unseen);
///   - the title camera takes each family's reflection zone as it crosses into the room (the game sets the start rooms' own
///     once play starts);
///   - the 192 m rebase waits until only a Level 0 room can be seen.
/// WebGL keeps Today (its own track). Nothing here allocates in Tick but the recycle's re-parenting (no room, no mesh).
/// </summary>
public sealed partial class FrontRoomsRoomStream
{
    /// <summary>The family a title room stands for. Level 0 is the slot's own room.</summary>
    public enum TitleFamily : byte { Level0, Office, Run, Waiting, Pool, Low, Club, DarkPool }

    /// <summary>The title flows of the concept sheet (TF-1 Tour, TF-2 Drift, TF-3 Descent) and today's.</summary>
    public enum TitleProgramme : byte { Today, Tour, Drift, Descent }

    /// <summary>The switch: which flow the title plays. Today until Red picks (the package is held); tools and tests set it
    /// before the title is built. A running stream keeps the programme it was built with.</summary>
    public static TitleProgramme Programme = TitleProgramme.Tour;   // Red's pick (title_stream_rooms, repack.sh)

    /// <summary>The pool stops (Pool, DarkPool) in the programmes. On since the pool look landed in main (2026-11-04 12:52-13:03:
    /// pool_zone_look, pool_tile, pool_water); off skips them (Tour's loop is then 6 rooms).</summary>
    public static bool PoolStopsReady = true;

    /// <summary>Drift's seed; 0 draws a new one every launch (the film and the tests fix one).</summary>
    public static int DriftSeed = 0;

    /// <summary>Row 4 of 技术美术调研 (research/tech_art/20_ranked_changes.md 3.4; Red 11-04 12:49 "全部按推荐" on that list):
    /// corridor spot shadows by room family. With a programme, Level 0 rooms and the pool rooms cast none (empty rooms: the
    /// measured look is unchanged and their shadow passes go); a dressed family casts only from the lamps over its furniture (at
    /// most <see cref="FamilyShadowLamps"/>), each as a tight cone (<see cref="TightShadowCone"/>, <see cref="TightShadowRange"/>:
    /// URP sizes a spot's shadow texel and bias from its cone and range, so they shrink from about 6 and 9 cm to about 1 and 2 cm
    /// and the contact shadows under desks, seats and shelves land); the other lamps light unshadowed at the family's wide cone.
    /// Run! keeps its rig's strips (F7: every strip shadowed at 172 deg): a tight cone would darken the top of the corridor's
    /// walls, so Run! changes in the rig, for the maze and the title together, or not at all.</summary>
    public static bool FamilyShadows = true;
    /// <summary>Row 4's empty-room rule in today's title as well (its Lobby rooms lose their four shadowed troffers). Off, so this
    /// package changes nothing in Today; the landing may turn it on (Red's 12:49 answer covers row 4).</summary>
    public static bool FamilyShadowsInToday = false;
    public const float TightShadowCone = 120f, TightShadowInner = 90f, TightShadowRange = 7f;
    public const int FamilyShadowLamps = 4;

    /// <summary>The first sequence a programme may change (rooms 0 and 1 stay Level 0: the logo's door, the TV).</summary>
    public const int FirstTitleFamilySequence = 2;

    static readonly TitleFamily[] TourWithPool = { TitleFamily.Office, TitleFamily.Level0, TitleFamily.Run, TitleFamily.Level0, TitleFamily.Waiting, TitleFamily.Level0, TitleFamily.Pool, TitleFamily.Level0 };
    static readonly TitleFamily[] TourDry = { TitleFamily.Office, TitleFamily.Level0, TitleFamily.Run, TitleFamily.Level0, TitleFamily.Waiting, TitleFamily.Level0 };
    static readonly TitleFamily[] DescentOrder = { TitleFamily.Office, TitleFamily.Run, TitleFamily.Low, TitleFamily.Waiting, TitleFamily.Club, TitleFamily.Pool, TitleFamily.DarkPool };
    static readonly TitleFamily[] DriftBag = { TitleFamily.Office, TitleFamily.Run, TitleFamily.Waiting, TitleFamily.Pool, TitleFamily.Low, TitleFamily.Club };
    const int DriftLength = 512;
    const int FamilyMinGap = 5;

    TitleProgramme programme;
    bool legacyVariants = true;
    Transform titleBank;
    FrontRoomsTitleVignette[] vignettes;
    TitleFamily[] descentCycle;
    TitleFamily[] driftFamilies;
    int titleLookSequence = int.MinValue;
    int driftSeedUsed;
    string lastBuildReport = "";

    /// <summary>The last bank build, family by family (tools: the start-up cost).</summary>
    public string TitleBuildReport => lastBuildReport;

    /// <summary>True while this stream plays a title programme other than Today.</summary>
    public bool TitleRoomsOn => programme != TitleProgramme.Today;
    /// <summary>The programme this stream was built with.</summary>
    public TitleProgramme ActiveProgramme => programme;
    /// <summary>Drift's seed in use (0 for other programmes).</summary>
    public int ActiveDriftSeed => programme == TitleProgramme.Drift ? driftSeedUsed : 0;

    /// <summary>The family of any sequence, loaded or not.</summary>
    public TitleFamily FamilyAt(int sequence)
    {
        var room = initialized ? FindSequence(sequence) : null;
        return room != null ? room.family : FamilyForSequence(sequence);
    }

    /// <summary>Raised when a title room's door has finished opening and the room is connected (its lamps cue), with its
    /// sequence, family and the door's centre: the sound line's hook for a room tone or a strike per family (the stream plays
    /// nothing, AUDIO_CONTRACT). Only with a programme other than Today.</summary>
    public event Action<int, TitleFamily, Vector3> TitleRoomConnected;

    /// <summary>The family of the stream room at world z (the start rooms after the handoff: HUD, reflection, footsteps;
    /// contracts W/title_stream/contracts/map.md and sound.md).</summary>
    public TitleFamily FamilyAtZ(float z) => initialized ? FamilyAt(SequenceAtZ(z)) : TitleFamily.Level0;

    /// <summary>The reflection zone the room at world z wants (Level 0 for a Level 0 room; the pool's cubes only with the pool
    /// look). For the game's start rooms once it reads per room (contract map.md); the title camera uses it already.</summary>
    public FrontRoomsLook.ReflectionZone ReflectionZoneAtZ(float z)
    {
        var room = initialized ? FindSequence(SequenceAtZ(z)) : null;
        return room == null ? FrontRoomsLook.ReflectionZone.Level0 : ReflectionZoneOf(room);
    }

    /// <summary>True where a title pool room's ankle water stands over world <paramref name="position"/> (between the dry sills,
    /// inside the side walls): footsteps and wading (contract sound.md; the map's WaterAt knows nothing of the stream).</summary>
    public bool InTitleWater(Vector3 position)
    {
        var room = initialized ? FindSequence(SequenceAtZ(position.z)) : null;
        if (room?.vignette == null || (room.family != TitleFamily.Pool && room.family != TitleFamily.DarkPool)) return false;
        var local = position.z - room.startZ;
        return local > FrontRoomsTitleVignettes.PoolSill && local < RoomLength - FrontRoomsTitleVignettes.PoolSill
            && Mathf.Abs(position.x - centerX) < FrontRoomsTitleVignettes.SideFace;
    }

    /// <summary>How many family vignettes this stream built (tools).</summary>
    public int VignetteCount
    {
        get
        {
            if (vignettes == null) return 0;
            var n = 0;
            for (var i = 0; i < vignettes.Length; i++) if (vignettes[i] != null) n++;
            return n;
        }
    }

    /// <summary>Lights enabled under the stream's rooms right now (tools: the light budget).</summary>
    public int EnabledLightCount() => CountLights(false);

    /// <summary>Of those, the ones casting shadows (tools: row 4's shadow maps).</summary>
    public int EnabledShadowedLightCount() => CountLights(true);

    int CountLights(bool shadowedOnly)
    {
        var n = 0;
        for (var i = 0; i < MaxRooms; i++)
        {
            var room = pool[i];
            if (room?.root == null || !room.root.activeInHierarchy || room.roomLights == null) continue;
            for (var k = 0; k < room.roomLights.Length; k++)
            {
                var light = room.roomLights[k];
                if (light == null || !light.enabled || !light.gameObject.activeInHierarchy) continue;
                if (shadowedOnly && light.shadows == LightShadows.None) continue;
                n++;
            }
        }
        return n;
    }

    // ------------------------------------------------------------------------------------------------- setup
    /// <summary>Fixes the programme and builds the bank (Initialize, before the rooms; BuildEditorPreview).</summary>
    void BeginTitleRooms(bool preview)
    {
        programme = Programme;
#if UNITY_WEBGL && !UNITY_EDITOR
        // WebGL is its own track: it keeps today's title unless the WebGL line asks.
        programme = TitleProgramme.Today;
#endif
        legacyVariants = preview || programme == TitleProgramme.Today;
        vignettes = null;
        descentCycle = null;
        driftFamilies = null;
        titleLookSequence = int.MinValue;
        driftSeedUsed = 0;
        if (titleBank != null)
        {
            if (Application.isPlaying) Destroy(titleBank.gameObject);
            else DestroyImmediate(titleBank.gameObject);
            titleBank = null;
        }
        if (programme == TitleProgramme.Today) return;
        var watch = System.Diagnostics.Stopwatch.StartNew();
        titleBank = new GameObject("Title rooms / bank").transform;
        titleBank.SetParent(transform, false);
        vignettes = new FrontRoomsTitleVignette[Enum.GetValues(typeof(TitleFamily)).Length];
        var wanted = WantedFamilies(preview);
        var each = new System.Text.StringBuilder();
        for (var i = 0; i < wanted.Length; i++)
        {
            var family = wanted[i];
            if (vignettes[(int)family] != null) continue;
            var t0 = watch.Elapsed.TotalMilliseconds;
            try { vignettes[(int)family] = FrontRoomsTitleVignettes.Build(family, titleBank, 7919 * (int)family + 104729); }
            catch (Exception e) { Debug.LogError("[FrontRooms3D] title rooms: the " + family + " vignette failed and stays out: " + e); }
            each.Append(each.Length > 0 ? ", " : "").Append(family).Append(' ').Append((watch.Elapsed.TotalMilliseconds - t0).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)).Append(" ms");
        }
        lastBuildReport = each.ToString();
        if (programme == TitleProgramme.Descent) descentCycle = BuildDescentCycle();
        if (programme == TitleProgramme.Drift) driftFamilies = BuildDrift();
        if (Application.isPlaying)
            Debug.Log("[FrontRooms3D] title rooms: programme " + programme + (programme == TitleProgramme.Drift ? " seed " + driftSeedUsed : "")
                + ", " + VignetteCount + " vignettes built in " + watch.ElapsedMilliseconds + " ms [" + lastBuildReport + "] (pool stops " + (PoolStopsReady ? "on" : "off")
                + ", pool look " + (FrontRoomsTitleVignettes.PoolLookPresent ? "present" : "absent: stand-in tile and water") + ")");
    }

    TitleFamily[] WantedFamilies(bool preview)
    {
        if (preview) return new[] { TitleFamily.Office, TitleFamily.Run, TitleFamily.Waiting, TitleFamily.Pool, TitleFamily.Low, TitleFamily.DarkPool };
        var list = new System.Collections.Generic.List<TitleFamily>(8);
        TitleFamily[] source;
        switch (programme)
        {
            case TitleProgramme.Tour: source = PoolStopsReady ? TourWithPool : TourDry; break;
            case TitleProgramme.Descent: source = DescentOrder; break;
            default: source = DriftBag; break;
        }
        for (var i = 0; i < source.Length; i++)
        {
            var f = source[i];
            if (f == TitleFamily.Level0 || list.Contains(f) || !FamilyAllowed(f)) continue;
            list.Add(f);
        }
        return list.ToArray();
    }

    static bool FamilyAllowed(TitleFamily family)
    {
        if ((family == TitleFamily.Pool || family == TitleFamily.DarkPool || family == TitleFamily.Club) && !PoolStopsReady) return false;
        return FrontRoomsTitleVignettes.Available(family);
    }

    bool Built(TitleFamily family) => family == TitleFamily.Level0 || (vignettes != null && vignettes[(int)family] != null);

    TitleFamily[] BuildDescentCycle()
    {
        var list = new System.Collections.Generic.List<TitleFamily>(8);
        for (var i = 0; i < DescentOrder.Length; i++) if (Built(DescentOrder[i])) list.Add(DescentOrder[i]);
        // WAKE: a Level 0 room, lit at once as its door opens (WakeAt).
        list.Add(TitleFamily.Level0);
        while (list.Count < FamilyMinGap) list.Insert(list.Count - 1, TitleFamily.Level0);
        return list.ToArray();
    }

    TitleFamily[] BuildDrift()
    {
        driftSeedUsed = DriftSeed != 0 ? DriftSeed : (Environment.TickCount & 0x7fffffff) | 1;
        var rng = new System.Random(driftSeedUsed);
        var bag = new System.Collections.Generic.List<TitleFamily>(DriftBag.Length);
        for (var i = 0; i < DriftBag.Length; i++) if (Built(DriftBag[i])) bag.Add(DriftBag[i]);
        var seq = new TitleFamily[DriftLength];
        var last = new int[Enum.GetValues(typeof(TitleFamily)).Length];
        for (var i = 0; i < last.Length; i++) last[i] = int.MinValue / 2;
        var at = FirstTitleFamilySequence;
        var first = true;
        while (at < DriftLength && bag.Count > 0)
        {
            for (var i = bag.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                var swap = bag[i]; bag[i] = bag[j]; bag[j] = swap;
            }
            for (var b = 0; b < bag.Count && at < DriftLength; b++)
            {
                var family = bag[b];
                // One or two Level 0 rooms between families (none before the first, revealed at 12.2 s), more while the family's
                // only copy could still stand in a room of the pool.
                if (!first) at += 1 + rng.Next(2);
                while (at - last[(int)family] < FamilyMinGap) at++;
                if (at >= DriftLength) break;
                seq[at] = family;
                last[(int)family] = at;
                at++;
                first = false;
            }
        }
        return seq;
    }

    /// <summary>The programme's family for a sequence (Level 0 where the family is not built).</summary>
    TitleFamily FamilyForSequence(int sequence)
    {
        if (programme == TitleProgramme.Today || sequence < FirstTitleFamilySequence || IsPlayable) return TitleFamily.Level0;
        var k = sequence - FirstTitleFamilySequence;
        TitleFamily family;
        switch (programme)
        {
            case TitleProgramme.Tour:
            {
                var cycle = PoolStopsReady && Built(TitleFamily.Pool) ? TourWithPool : TourDry;
                family = cycle[k % cycle.Length];
                break;
            }
            case TitleProgramme.Descent:
                family = descentCycle == null || descentCycle.Length == 0 ? TitleFamily.Level0 : descentCycle[k % descentCycle.Length];
                break;
            case TitleProgramme.Drift:
            {
                if (driftFamilies == null) return TitleFamily.Level0;
                // Past the precomputed run the bag repeats from a family-free stretch; FamilyMinGap is kept by the attach check.
                var s = sequence < DriftLength ? sequence : FirstTitleFamilySequence + (sequence - FirstTitleFamilySequence) % (DriftLength - FirstTitleFamilySequence);
                family = driftFamilies[s];
                break;
            }
            default: return TitleFamily.Level0;
        }
        return Built(family) ? family : TitleFamily.Level0;
    }

    /// <summary>Descent's WAKE room: the Level 0 room that closes each loop, lit at once as its door opens.</summary>
    bool WakeAt(int sequence)
    {
        if (programme != TitleProgramme.Descent || descentCycle == null || sequence < FirstTitleFamilySequence || IsPlayable) return false;
        return (sequence - FirstTitleFamilySequence) % descentCycle.Length == descentCycle.Length - 1;
    }

    // ------------------------------------------------------------------------------------------------- slots and vignettes
    /// <summary>The slot's own channel targets (its twelve L03), kept so a vignette with lamps of its own can hand them back.</summary>
    void CaptureOwnLamps(RoomSlot room)
    {
        if (room.roomLights == null) return;
        var n = room.roomLights.Length;
        room.ownLights = (Light[])room.roomLights.Clone();
        room.ownBaseIntensity = (float[])room.lightBaseIntensity.Clone();
        room.ownWasEnabled = (bool[])room.lightWasEnabled.Clone();
        room.ownDiffusers = (Renderer[])room.lightDiffusers.Clone();
        room.ownDiffuserKit = (bool[])room.lightDiffuserKit.Clone();
        room.lightSecond = new Renderer[n];
        room.lightHalo = new Renderer[n];
        room.lightSecondBlocks = new MaterialPropertyBlock[n];
        room.lightHaloBlocks = new MaterialPropertyBlock[n];
        for (var i = 0; i < n; i++)
        {
            room.lightSecondBlocks[i] = new MaterialPropertyBlock();
            room.lightHaloBlocks[i] = new MaterialPropertyBlock();
        }
        var root = room.root.transform;
        room.shellFloor = root.Find("carpet floor")?.gameObject;
        room.shellBaseboards = new[] { root.Find("left baseboard")?.gameObject, root.Find("right baseboard")?.gameObject };
    }

    /// <summary>Gives the slot (sealed, out of sight) the programme's family for <paramref name="sequence"/>. Call before
    /// ResetRoomLights and RefreshRoomMaterials.</summary>
    void AssignTitleFamily(RoomSlot room, int sequence)
    {
        if (!TitleRoomsOn || room == null) return;
        ReleaseVignette(room);
        room.wakeLit = WakeAt(sequence);
        var family = FamilyForSequence(sequence);
        if (family == TitleFamily.Level0) return;
        var v = vignettes[(int)family];
        // Its one copy is still in a room of the pool: this room stays Level 0 (FamilyMinGap keeps that from happening).
        if (v == null || v.inSequence >= 0) return;
        AttachVignette(room, v, sequence);
    }

    void AttachVignette(RoomSlot room, FrontRoomsTitleVignette v, int sequence)
    {
        room.family = v.family;
        room.vignette = v;
        v.inSequence = sequence;
        var t = v.root.transform;
        t.SetParent(room.root.transform, false);
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;
        v.root.SetActive(true);
        SetSeenOnly(v, false);
        if (v.ownLamps) SwapToVignetteLamps(room, v);
        ShowSlotShell(room, v);
        TitleVignetteMoved(room, v, true);
    }

    void ReleaseVignette(RoomSlot room)
    {
        var v = room?.vignette;
        if (v == null) return;
        if (v.ownLamps) RestoreSlotLamps(room);
        SetSeenOnly(v, false);
        if (v.root != null)
        {
            v.root.SetActive(false);
            v.root.transform.SetParent(titleBank != null ? titleBank : transform, false);
        }
        TitleVignetteMoved(room, v, false);
        v.inSequence = -1;
        room.vignette = null;
        room.family = TitleFamily.Level0;
        ShowSlotShell(room, null);
    }

    /// <summary>Called after a vignette moves into a slot (attached) or out of it (released). The door culling of 技术美术调研 row 1
    /// (W/apply/ta_door_culling, landing separately) implements it beside its portal code in FrontRoomsRoomStream.cs, so a
    /// vignette takes the drawn state of the slot it joins and is drawn again when it leaves (a vignette moves while its slot is
    /// sealed, out of sight). Without an implementation the compiler drops the calls.</summary>
    partial void TitleVignetteMoved(RoomSlot room, FrontRoomsTitleVignette vignette, bool attached);

    /// <summary>The start door is shut for good (CloseTerminalDoor). The game then fades every Light under the stream and destroys
    /// the Light components (FrontRooms3DGame.cs, about l. 2151), which URP's per-light data refuses while it stands
    /// (RequireComponent; 技术美术调研 row 16, contract G2 of ta_lamp_lightdata). The family lamps carry that data (row 16
    /// option D), so it goes first, here, behind the shut door; once G2 is in the game too this finds nothing to do.</summary>
    void ReleaseTitleLampData()
    {
        if (!TitleRoomsOn) return;
        for (var i = 0; i < MaxRooms; i++)
        {
            var v = pool[i]?.vignette;
            if (v?.root == null) continue;
            var data = v.root.GetComponentsInChildren<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>(true);
            for (var k = 0; k < data.Length; k++)
                if (data[k] != null) DestroyImmediate(data[k]);
        }
    }

    void SwapToVignetteLamps(RoomSlot room, FrontRoomsTitleVignette v)
    {
        if (room.roomLights == null || room.ownLights == null) return;
        // The slot's own troffers go dark before the channels move (their Lights off, their lenses at rest), through the L03
        // drive (the vignette is not theirs).
        var keep = room.vignette;
        room.vignette = null;
        for (var i = 0; i < room.roomLights.Length; i++) SetFixtureOutput(room, i, 0f, false);
        room.vignette = keep;
        for (var i = 0; i < room.roomLights.Length; i++)
        {
            var has = i < v.Count && v.lights[i] != null;
            room.roomLights[i] = has ? v.lights[i] : null;
            room.lightBaseIntensity[i] = has ? v.baseIntensity[i] : 0f;
            room.lightWasEnabled[i] = has;
            room.lightDiffusers[i] = i < v.lens.Length ? v.lens[i] : null;
            room.lightDiffuserKit[i] = false;
            room.lightSecond[i] = i < v.second.Length ? v.second[i] : null;
            room.lightHalo[i] = i < v.halo.Length ? v.halo[i] : null;
            if (has) v.lights[i].bounceIntensity = diffuseCoefficient;
        }
        if (!v.keepSlotFixtures && room.fixtures != null)
            for (var i = 0; i < room.fixtures.Length; i++)
                if (room.fixtures[i] != null) room.fixtures[i].gameObject.SetActive(false);
    }

    void RestoreSlotLamps(RoomSlot room)
    {
        if (room.roomLights == null || room.ownLights == null) return;
        for (var i = 0; i < room.roomLights.Length; i++) SetFixtureOutput(room, i, 0f, false);
        for (var i = 0; i < room.roomLights.Length; i++)
        {
            room.roomLights[i] = room.ownLights[i];
            room.lightBaseIntensity[i] = room.ownBaseIntensity[i];
            room.lightWasEnabled[i] = room.ownWasEnabled[i];
            room.lightDiffusers[i] = room.ownDiffusers[i];
            room.lightDiffuserKit[i] = room.ownDiffuserKit[i];
            room.lightSecond[i] = null;
            room.lightHalo[i] = null;
        }
        if (room.fixtures != null)
            for (var i = 0; i < room.fixtures.Length; i++)
                if (room.fixtures[i] != null) room.fixtures[i].gameObject.SetActive(true);
    }

    static void ShowSlotShell(RoomSlot room, FrontRoomsTitleVignette v)
    {
        if (room.shellFloor != null) room.shellFloor.SetActive(v == null || !v.hideShellFloor);
        if (room.shellBaseboards != null)
            for (var i = 0; i < room.shellBaseboards.Length; i++)
                if (room.shellBaseboards[i] != null) room.shellBaseboards[i].SetActive(v == null || !v.hideBaseboards);
    }

    /// <summary>The pool's water (and anything else that costs only while seen): on while the room is lit and connected.</summary>
    static void SetSeenOnly(FrontRoomsTitleVignette v, bool on)
    {
        if (v?.seenOnly == null) return;
        for (var i = 0; i < v.seenOnly.Length; i++)
            if (v.seenOnly[i] != null && v.seenOnly[i].enabled != on) v.seenOnly[i].enabled = on;
    }

    /// <summary>After a rebase the water re-registers at its new place (its box is taken in world space when it is enabled).</summary>
    void RefreshSeenOnly()
    {
        for (var i = 0; i < MaxRooms; i++)
        {
            var v = pool[i]?.vignette;
            if (v?.seenOnly == null) continue;
            for (var k = 0; k < v.seenOnly.Length; k++)
            {
                var b = v.seenOnly[k];
                if (b == null || !b.enabled) continue;
                b.enabled = false;
                b.enabled = true;
            }
        }
    }

    // ------------------------------------------------------------------------------------------------- what a family changes
    Material RoomWallMaterial(RoomSlot room) => room?.vignette?.wall != null ? room.vignette.wall : ProfileMaterial(profileWallMaterials, room == null ? RoomRule.Lobby : room.rule, wallMaterial);
    Material RoomFloorMaterial(RoomSlot room) => room?.vignette?.floor != null ? room.vignette.floor : ProfileMaterial(profileFloorMaterials, room.rule, floorMaterial);
    Material RoomCeilingMaterial(RoomSlot room) => room?.vignette?.ceiling != null ? room.vignette.ceiling : ProfileMaterial(profileCeilingMaterials, room.rule, ceilingMaterial);
    static Color RoomLightColor(RoomSlot room) => room.vignette != null ? room.vignette.lampColor : ProfileLightColor(room.rule);
    static float RoomLightIntensity(RoomSlot room) => room.vignette != null ? room.vignette.slotIntensity : ProfileLightIntensity(room.rule);
    static float RoomLightRange(RoomSlot room) => room.vignette != null ? room.vignette.slotRange : ProfileLightRange(room.rule);
    static bool OwnLampsIn(RoomSlot room) => room.vignette != null && room.vignette.ownLamps;

    /// <summary>Row 4 (FamilyShadows) on the slot's own twelve L03, after RefreshRoomMaterials has set the family's range: a Level 0
    /// room and the pool cast none; a family riding them (the waiting room) casts from the lamps over its furniture, tight. A
    /// family with lamps of its own chose them when it was built. Today: untouched unless FamilyShadowsInToday.</summary>
    void ApplyShadowRoles(RoomSlot room)
    {
        if (room?.roomLights == null || room.root == null || OwnLampsIn(room)) return;
        if (TitleRoomsOn ? !FamilyShadows : !FamilyShadowsInToday) return;
        var v = room.vignette;
        int[] picks = null;
        if (v?.occluders != null && v.occluders.Length > 0)
        {
            // Every slot's L03 sit at the same room-space points (12 m rooms on the 1.2 m grid, the 192 m rebase too), so the
            // pick is made once per vignette.
            if (v.slotShadowPicks == null)
            {
                var at = new Vector3[room.roomLights.Length];
                for (var i = 0; i < at.Length; i++)
                    at[i] = room.roomLights[i] != null ? room.root.transform.InverseTransformPoint(room.roomLights[i].transform.position) : new Vector3(0f, -100f, 0f);
                v.slotShadowPicks = FrontRoomsTitleVignettes.PickShadowLamps(at, v.occluders, FamilyShadowLamps);
            }
            picks = v.slotShadowPicks;
        }
        for (var i = 0; i < room.roomLights.Length; i++)
        {
            var light = room.roomLights[i];
            if (light == null || light.type != LightType.Spot) continue;
            var tight = picks != null && Array.IndexOf(picks, i) >= 0;
            FrontRoomsTitleVignettes.SetShadowRole(light, tight, 162f, 96f, light.range);
        }
    }

    /// <summary>A family lamp's lens, second renderer and halo at <paramref name="level"/> (the map's drive: the lens colour at the
    /// family's peak; the pendant's ceiling patch takes the lens's block; the red tube its coating; the halo the level).</summary>
    void ApplyVignetteLampVisual(RoomSlot room, int fixtureIndex, float level, bool enabled)
    {
        var v = room.vignette;
        var lit = enabled ? level : 0f;
        var emission = v.lensColor * Mathf.Lerp(.02f, v.lensPeak, lit);
        if (room.lightDiffusers != null && fixtureIndex < room.lightDiffusers.Length && room.lightDiffusers[fixtureIndex] != null)
        {
            var renderer = room.lightDiffusers[fixtureIndex];
            var block = room.lightDiffuserBlocks[fixtureIndex] ?? (room.lightDiffuserBlocks[fixtureIndex] = new MaterialPropertyBlock());
            renderer.GetPropertyBlock(block);
            block.SetColor("_EmissionColor", emission);
            renderer.SetPropertyBlock(block);
        }
        var second = room.lightSecond != null && fixtureIndex < room.lightSecond.Length ? room.lightSecond[fixtureIndex] : null;
        if (second != null && v.secondDrive != FrontRoomsTitleVignette.SecondDrive.None)
        {
            var block = room.lightSecondBlocks[fixtureIndex];
            second.GetPropertyBlock(block);
            block.SetColor("_EmissionColor", v.secondDrive == FrontRoomsTitleVignette.SecondDrive.RedTube ? v.secondColor * lit : emission);
            second.SetPropertyBlock(block);
        }
        var halo = room.lightHalo != null && fixtureIndex < room.lightHalo.Length ? room.lightHalo[fixtureIndex] : null;
        if (halo != null)
        {
            var block = room.lightHaloBlocks[fixtureIndex];
            halo.GetPropertyBlock(block);
            block.SetFloat("_Density", lit);
            halo.SetPropertyBlock(block);
            halo.enabled = lit > .0005f;
        }
    }

    /// <summary>The family's lamp odds and, for Run!, the RESET re-strike as the reveal: from the entrance toward the far door,
    /// one cell every 0.15 s from 0.45 s, one flick, then a rise from 0.35 over 0.4 s (FrontRoomsRunRig's resetRiseStart,
    /// resetCellSeconds, resetRise; Red: "一个一个灯光熄灭，一个一个灯光在亮").</summary>
    static void ConfigureVignetteLamps(RoomSlot room)
    {
        var v = room.vignette;
        if (v == null || !v.chase) return;
        var n = Mathf.Min(v.Count, room.lampState.Length);
        for (var lamp = 0; lamp < n; lamp++)
        {
            room.lampDelay[lamp] = .45f + .15f * lamp;
            room.lampFlickerCount[lamp] = 1;
            room.lampFlickerPeriod[lamp] = .16f;
            room.lampFlickerOnFraction[lamp] = .5f;
            room.lampFlickerPhase[lamp] = 0f;
            room.lampRevealSeconds[lamp] = .4f;
            room.lampDead[lamp] = false;
            room.lampUnstable[lamp] = false;
        }
    }

    // ------------------------------------------------------------------------------------------------- lights off behind shut doors
    /// <summary>A title door has snapped shut behind the camera: the room behind it goes dark (its lamps keep their state).</summary>
    void DimRoom(RoomSlot room)
    {
        if (!TitleRoomsOn || room == null || room.lightsDimmed) return;
        room.lightsDimmed = true;
        if (room.roomLights != null)
            for (var i = 0; i < room.roomLights.Length; i++)
                if (room.roomLights[i] != null) room.roomLights[i].enabled = false;
        SetSeenOnly(room.vignette, false);
    }

    void UndimRoom(RoomSlot room)
    {
        if (room == null || !room.lightsDimmed) return;
        room.lightsDimmed = false;
        if (room.roomLights == null) return;
        for (var i = 0; i < room.roomLights.Length; i++)
        {
            var light = room.roomLights[i];
            if (light == null || room.lightOutputEnabled == null || i >= room.lightOutputEnabled.Length) continue;
            light.enabled = room.lightOutputEnabled[i];
        }
    }

    /// <summary>A door finished opening and connected <paramref name="next"/>: its lamps cue (WAKE: lit at once) and what costs only
    /// while seen comes on.</summary>
    void ConnectTitleRoom(RoomSlot next)
    {
        if (next.wakeLit && TitleRoomsOn) ActivateRoomLightImmediately(next);
        else ScheduleRoomLight(next);
        if (!TitleRoomsOn) return;
        SetSeenOnly(next.vignette, true);
        TitleRoomConnected?.Invoke(next.sequence, next.family, new Vector3(centerX, RoomHeight * .42f, next.startZ));
    }

    // ------------------------------------------------------------------------------------------------- the handoff (airlock)
    /// <summary>The start door always opens from a Level 0 room: when the first shut door is a family room's, the sealed room past
    /// it carries the start door (Tour and Drift build it Level 0; Descent's is turned so by EndStreamAt, unseen).</summary>
    int AirlockTerminal(RoomSlot room)
    {
        if (!TitleRoomsOn || room.family == TitleFamily.Level0) return room.sequence;
        var next = FindSequence(room.sequence + 1);
        return next != null && !next.connected ? next.sequence : room.sequence;
    }

    /// <summary>EndStreamAt: the terminal room becomes Level 0 if it is a sealed family room (Descent), the rooms behind light up
    /// again (the player may walk back), and their water comes on.</summary>
    void PrepareTitleHandoff(RoomSlot terminal)
    {
        if (!TitleRoomsOn || terminal == null) return;
        if (terminal.family != TitleFamily.Level0 && !terminal.connected)
        {
            ReleaseVignette(terminal);
            terminal.wakeLit = false;
            ResetRoomLights(terminal);
            RefreshRoomMaterials(terminal);
        }
        for (var i = 0; i < MaxRooms; i++)
        {
            var room = pool[i];
            if (room == null || room.sequence > terminal.sequence) continue;
            UndimRoom(room);
            if (room.connected || room.sequence == currentSequence) SetSeenOnly(room.vignette, true);
        }
    }

    // ------------------------------------------------------------------------------------------------- title look and rebase
    /// <summary>The title camera takes the family's reflection zone as it crosses into a room (before the handoff only).</summary>
    void UpdateTitleLook()
    {
        if (!Application.isPlaying || frozen || startRequested) return;
        var room = FindSequence(currentSequence);
        if (room == null || room.sequence == titleLookSequence) return;
        var first = titleLookSequence == int.MinValue;
        titleLookSequence = room.sequence;
        if (first && room.family == TitleFamily.Level0) return;   // Initialize already set Level 0
        FrontRoomsLook.SetZoneReflection(ReflectionZoneOf(room), .5f);
    }

    static FrontRoomsLook.ReflectionZone ReflectionZoneOf(RoomSlot room)
    {
        var name = room.vignette != null ? room.vignette.reflectionZone : "Level0";
        FrontRoomsLook.ReflectionZone zone;
        if (!string.IsNullOrEmpty(name) && Enum.TryParse(name, out zone) && Enum.IsDefined(typeof(FrontRoomsLook.ReflectionZone), zone)) return zone;
        // The pool's cubes come with the pool look: until then the dead pool takes the dead-lamp cube, the lit one Level 0's.
        return name == "PoolDead" ? FrontRoomsLook.ReflectionZone.DeadLamp : FrontRoomsLook.ReflectionZone.Level0;
    }

    /// <summary>The rebase moves every world-projected print by a multiple of 192 m; a family's print may not divide it (a 12 in
    /// VCT), so it waits until only a Level 0 room can be seen: the camera in a Level 0 room whose far door is shut, the door
    /// behind shut or Level 0 behind it.</summary>
    bool TitleRebaseAllowed()
    {
        var room = FindSequence(currentSequence);
        if (room == null) return true;
        if (room.family != TitleFamily.Level0 || room.doorOpen || room.doorOpening) return false;
        var previous = FindSequence(room.sequence - 1);
        return previous == null || previous.family == TitleFamily.Level0 || (!previous.doorOpen && !previous.doorOpening);
    }

    // ------------------------------------------------------------------------------------------------- teardown
    /// <summary>DisposeOneRoom: one bank vignette (not standing in a room) a call, then the bank. False when nothing is left.</summary>
    bool DisposeOneTitleBankPiece()
    {
        if (titleBank == null) return false;
        if (vignettes != null)
            for (var i = 0; i < vignettes.Length; i++)
            {
                var v = vignettes[i];
                if (v == null || v.inSequence >= 0) continue;
                vignettes[i] = null;
                if (v.root != null) Destroy(v.root);
                return true;
            }
        Destroy(titleBank.gameObject);
        titleBank = null;
        return true;
    }

    // ------------------------------------------------------------------------------------------------- edit-mode preview
    /// <summary>The edit-mode reference (Red 10-31: every room viewable): after the five profiles, one room per family vignette,
    /// doors open, lamps on. Only while Programme is not Today, so today's preview is unchanged.</summary>
    void BuildTitleFamilyPreview(int firstIndex)
    {
        if (!TitleRoomsOn || vignettes == null) return;
        var index = firstIndex;
        for (var f = 1; f < vignettes.Length; f++)
        {
            var v = vignettes[f];
            if (v == null) continue;
            var room = new RoomSlot { sequence = index, rule = RoomRule.Lobby, startZ = index * RoomLength };
            room.endZ = room.startZ + RoomLength;
            room.root = new GameObject("Title room " + index + " / " + (TitleFamily)f);
            room.root.transform.SetParent(transform, false);
            room.root.transform.localPosition = new Vector3(0f, 0f, room.startZ);
            BuildRoom(room, index);
            AttachVignette(room, v, index);
            SetSeenOnly(v, true);
            ResetRoomLights(room);
            RefreshRoomMaterials(room);
            room.doorProgress = 1f;
            ApplyDoorPose(room);
            ActivateRoomLightImmediately(room);
            index++;
        }
    }
}
