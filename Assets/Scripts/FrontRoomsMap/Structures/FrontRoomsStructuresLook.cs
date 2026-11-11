using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// The structures scene's one writer of RenderSettings and post (scene 4; research/level_space/70 section 1; PLAN 2.6).
/// Each district asks for a preset where the walker stands: Level 0 and the Office keep the game's own look
/// (FrontRoomsLook.ApplyAmbient, the global film look, the zone reflection); the pool family gets palette F1 (trilight
/// (.30, .36, .38) / (.32, .38, .40) / (.36, .44, .44), fog (.11, .16, .17) ExponentialSquared .010, and a structures-only
/// runtime VolumeProfile: white balance -12, tint -2, post-exposure +.45, contrast -4, saturation +4; bathroom_rooms/16 section 5)
/// and the dark zone its night (DESIGN section 6). DynamicGI.UpdateEnvironment after each switch. Realtime reflection
/// probes are captured two frames after the walker arrives in their district and again when it changes, with every lamp of
/// the district on for that frame (the pool glaze GZ-A, research/level_space/73_pool_glaze.md section 3.4); the lenses of the
/// pool family's lamps carry reflection proxies only those captures see (section 3.3). The section "the pool glaze's lamps
/// and probes" below has both. It also keeps the light budget of the scene's own lamps (lamps on
/// within 16 m of the walker with a 3 m fade, one in three shadowed within 9 m; view lamps stay on), the map's lamps in P
/// keep the map's own. No shared asset is ever changed: the URP asset, the film look profile and every game material stay
/// as they are. Numbers are starting values, tuned by eye on the walk.
/// The pool rooms of SP1, SP2's F0 and SP6 also carry 游戏视觉's pool zone look, on by default (Q: today's fixed trilight and
/// grade instead); the section "游戏视觉's pool zone look" below says how the two share the frame and never fight.
/// </summary>
public sealed class FrontRoomsStructuresLook : MonoBehaviour
{
    public enum Preset { Game, Level0, Office, Pool, PoolClub, PoolDark, RunB, PoolNight, Level0Tall, RunBTripped, RunBRed }

    Preset current = (Preset)(-1);
    Volume poolVolume;
    VolumeProfile poolProfile, darkProfile;
    float environmentDirty = -1f;
    readonly List<ReflectionProbe> pendingProbes = new List<ReflectionProbe>();
    int probeFrames;

    public Preset Current => current;

    /// <summary>Make the look on the bootstrap: the game's film look as the global volume, plus the pool grade (weight 0).</summary>
    public static FrontRoomsStructuresLook Create(Transform host)
    {
        var look = host.gameObject.AddComponent<FrontRoomsStructuresLook>();
        try { FrontRoomsPostStack.Ensure(host); }
        catch (System.Exception e) { FrontRoomsPoolMaterials.LogOnce("post", "the film look volume failed: " + e.Message); }
        look.MakePoolVolume(host);
        look.Want(Preset.Game);
        return look;
    }

    void MakePoolVolume(Transform host)
    {
        try
        {
            // Tuned by eye on the scene-4 review (70 section 1 says starting values): at -12 / +.45 the porcelain tile read
            // grey-teal (wall luma 84, Red 10-31 17:19 asks Dream Pools' porcelain white): -6, +.55, saturation +2.
            poolProfile = Grade(-6f, -2f, .6f, -4f, 2f);
            darkProfile = Grade(-16f, -2f, .6f, -4f, 6f);
            var go = new GameObject("Post / structures pool grade (runtime)");
            go.transform.SetParent(host, false);
            poolVolume = go.AddComponent<Volume>();
            poolVolume.isGlobal = true;
            poolVolume.priority = 100f;
            poolVolume.weight = 0f;
            poolVolume.sharedProfile = poolProfile;
        }
        catch (System.Exception e)
        {
            FrontRoomsPoolMaterials.LogOnce("poolgrade", "the pool grade could not be made: " + e.Message);
        }
    }

    static VolumeProfile Grade(float temperature, float tint, float exposure, float contrast, float saturation)
    {
        var p = ScriptableObject.CreateInstance<VolumeProfile>();
        p.name = "Structures pool grade";
        p.hideFlags = HideFlags.DontSave;
        var wb = p.Add<WhiteBalance>(true);
        wb.temperature.Override(temperature);
        wb.tint.Override(tint);
        var ca = p.Add<ColorAdjustments>(true);
        ca.postExposure.Override(exposure);
        ca.contrast.Override(contrast);
        ca.saturation.Override(saturation);
        return p;
    }

    /// <summary>Ask for a preset; applied at once when it changes (every frame with the same value is free).</summary>
    public void Want(Preset preset)
    {
        if (preset == current) return;
        current = preset;
        try { Apply(preset); }
        catch (System.Exception e) { FrontRoomsPoolMaterials.LogOnce("look " + preset, "the " + preset + " look failed: " + e.Message); }
    }

    void Apply(Preset preset)
    {
        switch (preset)
        {
            case Preset.Pool:
            case Preset.PoolClub:
                // 70 section 1's (.30, .36, .38) / (.32, .38, .40) / (.36, .44, .44) x about 1.4 and toward neutral (review
                // SP1-F8, KIT-F1, SP6-F9: the tile read grey-teal under the cyan trilight); fog as 70.
                // The equator and ground raised again (walls and lid stand in for the hall's bounce, which realtime lamps alone do
                // not give): walls toward 160+, the lid over them.
                Trilight(new Color(.44f, .49f, .51f), new Color(.50f, .555f, .575f), new Color(.66f, .72f, .72f), new Color(.11f, .16f, .17f), .010f);
                Grade(poolProfile, 1f);
                // The halls' own probes (rendered once) carry the tile's reflections; elsewhere the game's default cube.
                Reflection(FrontRoomsLook.ReflectionZone.Level0);
                break;
            case Preset.PoolNight:
                // SP1's night key (70 section 3): the three trilight colours halved, the same grade.
                Trilight(new Color(.22f, .245f, .255f), new Color(.25f, .28f, .29f), new Color(.33f, .36f, .36f), new Color(.06f, .08f, .085f), .010f);
                Grade(poolProfile, 1f);
                Reflection(FrontRoomsLook.ReflectionZone.Level0);
                break;
            case Preset.PoolDark:
                Trilight(new Color(.02f, .045f, .055f), new Color(.025f, .055f, .065f), new Color(.03f, .07f, .075f), new Color(.02f, .045f, .05f), .012f);
                Grade(darkProfile, 1f);
                Reflection(FrontRoomsLook.ReflectionZone.DeadLamp);
                break;
            case Preset.RunB:
                // Run! B's armed bounce (the rig's SH: floor .06, walls .25, ceiling .40, tint (.93, .97, 1)), for SP2's F1,
                // SP5 and SP7. The rig's numbers are linear SH values; RenderSettings colours are gamma-space
                // (FrontRoomsLook), so they go in through .gamma (2026-11-04: the first build passed them raw, about 5 x dark).
                var tint = new Color(.93f, .97f, 1f);
                Trilight((tint * .06f).gamma, (tint * .25f).gamma, (tint * .40f).gamma, FrontRoomsLook.FogColor, FrontRoomsLook.FogDensity);
                Grade(null, 0f);
                Reflection(FrontRoomsLook.ReflectionZone.Level0);
                break;
            case Preset.RunBTripped:
                // Run! B's tripped bounce (floor .025, walls .050, ceiling .070, white; linear, so through .gamma).
                Trilight((Color.white * .025f).gamma, (Color.white * .05f).gamma, (Color.white * .07f).gamma, FrontRoomsLook.FogColor, FrontRoomsLook.FogDensity);
                Grade(null, 0f);
                Reflection(FrontRoomsLook.ReflectionZone.DeadLamp);
                break;
            case Preset.RunBRed:
                // SP7's red looks (71 section 6.3): the tripped ambient tinted linear (1.00, 0.30, 0.24) at floor .05, walls
                // .06, ceiling .08 (a wall lit by it alone reads maroon-black, never a black hole); no grade change.
                var red = new Color(1f, .30f, .24f);
                Trilight((red * .05f).gamma, (red * .06f).gamma, (red * .08f).gamma, FrontRoomsLook.FogColor, FrontRoomsLook.FogDensity);
                Grade(null, 0f);
                Reflection(FrontRoomsLook.ReflectionZone.DeadLamp);
                break;
            case Preset.Level0Tall:
                // SP3's V4 room (70 section 1: Level 0 with the Tall reflection in V4).
                FrontRoomsLook.ApplyAmbient();
                Grade(null, 0f);
                Reflection(FrontRoomsLook.ReflectionZone.Tall);
                break;
            case Preset.Office:
                FrontRoomsLook.ApplyAmbient();
                Grade(null, 0f);
                Reflection(FrontRoomsLook.ReflectionZone.Office);
                break;
            default:
                FrontRoomsLook.ApplyAmbient();
                Grade(null, 0f);
                Reflection(FrontRoomsLook.ReflectionZone.Level0);
                break;
        }
        environmentDirty = 0f;
        // LW3 (游戏视觉's lens hook, FrontRoomsLampLens): where the camera is, for a fixed exposure per zone; it only keeps the
        // zone while nothing is set (the default), so the frame stays today's
        FrontRoomsLampLens.AtZone(LensZone(preset));
    }

    // LW3: a preset's grade zone (FrontRoomsLampLens.Zone), as the game grades it: the pool hall presets as the hall, night a lamp state.
    static FrontRoomsLampLens.Zone LensZone(Preset preset)
    {
        switch (preset)
        {
            case Preset.Office: return FrontRoomsLampLens.Zone.Office;
            case Preset.Level0Tall: return FrontRoomsLampLens.Zone.Tall;
            case Preset.Pool:
            case Preset.PoolNight: return FrontRoomsLampLens.Zone.PoolHall;
            case Preset.PoolClub: return FrontRoomsLampLens.Zone.PoolClub;
            case Preset.PoolDark: return FrontRoomsLampLens.Zone.PoolDark;
            case Preset.RunB:
            case Preset.RunBTripped:
            case Preset.RunBRed: return FrontRoomsLampLens.Zone.RunB;
            default: return FrontRoomsLampLens.Zone.Level0;
        }
    }

    // The preset's own reflection cube, set at once as before; inside a pool zone (zone look on) the zone's cube stays shown
    // (ZoneTick picks it, the game's rule), so a preset change there does not flash the preset's cube for a frame.
    void Reflection(FrontRoomsLook.ReflectionZone zone)
    {
        presetReflection = zone;
        ShowReflection(ZoneLookOn && zoneCubeHeld ? zoneCube : zone, 0f, true);
    }

    void ShowReflection(FrontRoomsLook.ReflectionZone zone, float blendSeconds, bool always = false)
    {
        if (!always && zone == shownReflection) return;
        shownReflection = zone;
        FrontRoomsLook.SetZoneReflection(zone, blendSeconds);
    }

    static void Trilight(Color sky, Color equator, Color ground, Color fog, float density)
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientLight = sky;
        RenderSettings.ambientSkyColor = sky;
        RenderSettings.ambientEquatorColor = equator;
        RenderSettings.ambientGroundColor = ground;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = fog;
        RenderSettings.fogDensity = density;
    }

    void Grade(VolumeProfile profile, float weight)
    {
        if (poolVolume == null) return;
        if (profile != null) poolVolume.sharedProfile = profile;
        // the preset's weight; with the zone look on it gives way where a zone weighs in (ZoneTick, GradeShare)
        gradeWeight = weight;
        poolVolume.weight = weight * GradeShare;
    }

    /// <summary>A realtime box probe over a hall (world bounds), rendered a frame or two after it is made (after the build): in
    /// its district's capture, with the district's lamps on, when the walker is in that district (GZ-A), else as it is.</summary>
    public ReflectionProbe Probe(Transform parent, Bounds world, int resolution = 1024)
    {
        try
        {
            var go = new GameObject("Reflection probe (rendered once)");
            go.transform.SetParent(parent, true);
            go.transform.position = world.center;
            var p = go.AddComponent<ReflectionProbe>();
            p.mode = ReflectionProbeMode.Realtime;
            p.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            p.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
            p.resolution = resolution;
            p.size = world.size;
            p.center = Vector3.zero;
            p.boxProjection = true;
            p.hdr = true;
            p.nearClipPlane = .1f;
            p.farClipPlane = Mathf.Max(20f, world.size.magnitude);
            p.importance = 2;
            pendingProbes.Add(p);
            probeFrames = 2;
            known.Add(p);
            return p;
        }
        catch (System.Exception e)
        {
            FrontRoomsPoolMaterials.LogOnce("probe", "a reflection probe failed: " + e.Message);
            return null;
        }
    }

    /// <summary>Render every realtime scripted probe under root again a few frames from now (after a change of look); the
    /// current district's go through its capture (GZ-A: its lamps on), so a re-render here is never a second one.</summary>
    public void RerenderProbes(Transform root)
    {
        if (root == null) return;
        if (here != null && here.Root == root) { RequestCapture(here); return; }
        foreach (var p in root.GetComponentsInChildren<ReflectionProbe>(false))
            if (p != null && p.mode == ReflectionProbeMode.Realtime && p.refreshMode == ReflectionProbeRefreshMode.ViaScripting && !pendingProbes.Contains(p))
                pendingProbes.Add(p);
        if (pendingProbes.Count > 0) probeFrames = Mathf.Max(probeFrames, 3);
    }

    // ---------------------------------------------------------------- 游戏视觉's pool zone look

    /// <summary>
    /// 游戏视觉's pool zone look (pool_zone_look: Rendering/ZoneLook; research/bathroom_rooms/70_look_build.md "Zone look") over
    /// every pool room of SP1, SP2's F0 and SP6, placed as the map's AddPoolLooks places it: one look volume per room through
    /// FrontRoomsZoneLook.AddMapZone, over the room's footprint from 3 m under its floor to 0.2 m over its ceiling, blend 2.5 m,
    /// the height fog on the room's water when all its pool water shares one height (none otherwise). Its ambient, haze,
    /// height fog and grade show here as in the game. On by default since 11-04 20:4x; Q switches to the scene's own fixed
    /// trilight and grade and back. (It was off at first: at a lamp share of 1, PoolLevel's ambient washed the porcelain slate
    /// grey in scene 4's lamps, tile luma 11-39 % under today's tuned trilight. 游戏视觉's pool_zone_look_tune gave the share
    /// that stands in for the fixtures scene 4 lacks, and with it the porcelain holds: see ZoneLookOn.)
    /// While it is on, the scene's own look gives way wherever a zone weighs in: the global pool grade by the zone's weight at
    /// the camera, as URP weighs the zone grade (inside a room the zone grade alone), the default reflection cube to the game's
    /// pool cube inside a pool zone (FrontRooms3DGame's rule: weight over 0.5 at the player), and a district's rooms are off
    /// wherever its own preset is not a pool one (SP2's F1-F3, which F0's box and blend would reach). The ambient and the fog
    /// need nothing: the zone look writes them per render context over the preset's trilight. Lamps are never touched. Each
    /// switch re-renders the district's probes, so the tile reflects the look on screen. 游戏视觉's assets and numbers are used
    /// as they are. With it off, every frame is today's.
    /// Default ON (W/water_light/for_water_line.md section 2.5: the bar is the porcelain's luma within 5 % of the scene's own
    /// trilight at the six zone stops). Measured 11-04 (render10, W/level_space/renders/scene4/zone2, zone_measure.py; Q on
    /// with the lamp share below against Q off, key I off): SP1 stops 0 / 2 -2.7 / -2.6 %, SP2 stops 5 / 8 -3.5 / -2.5 %,
    /// SP6 stops 0 / 1 -3.9 / -4.3 %; SP1 at night (share 0.7) -3.7 / -4.0 %; SP2's Level 0 arrival +0.1 % (no zone reaches it);
    /// with key I on -2.3 to -3.8 %. The zone look stays a touch cooler than the scene's own: tile B-G +3.5 to +7.2 with Q on
    /// against -0.7 to +5.9 off by day, +2.0 / +2.5 against -7.2 / -6.8 at night.
    /// </summary>
    public bool ZoneLookOn { get; private set; } = true;
    const float PoolLevelShare = 3f, PoolLevelNightShare = .7f;
    float lampShare = 1f;

    /// <summary>What the last frame found at the camera: the strongest zone ("" outside every zone) and its weight.</summary>
    public string ZoneAtCamera { get; private set; } = "";
    public float ZoneWeightAtCamera { get; private set; }
    /// <summary>The share of the scene's own pool grade in the last frame (1 = today's, 0 = the zone grade alone).</summary>
    public float GradeShare { get; private set; } = 1f;
    /// <summary>The reflection cube shown now (the preset's, or the game's pool cube inside a pool zone).</summary>
    public FrontRoomsLook.ReflectionZone ShownReflection => shownReflection;

    /// <summary>AddPoolLooks' sizes: the box from 3 m under the floor to 0.2 m over the ceiling, blended over 2.5 m.</summary>
    public const float PoolRoomBelow = 3f, PoolRoomAbove = .2f, PoolRoomBlend = 2.5f;

    sealed class PoolRoomEntry
    {
        public FrontRoomsZoneLookVolume volume;
        public FrontRoomsStructuresDistrict district;
        public string name;
        // K2 (structures G2-a): the room as the district gave it (district metres): footprint, floor, ceiling
        public Rect footprint;
        public float floor, ceiling;
    }

    readonly List<PoolRoomEntry> poolRooms = new List<PoolRoomEntry>();
    readonly Dictionary<FrontRoomsStructuresDistrict, string> poolNotes = new Dictionary<FrontRoomsStructuresDistrict, string>();
    float gradeWeight;
    FrontRoomsLook.ReflectionZone presetReflection = FrontRoomsLook.ReflectionZone.Level0, shownReflection = FrontRoomsLook.ReflectionZone.Level0;
    FrontRoomsLook.ReflectionZone zoneCube = FrontRoomsLook.ReflectionZone.PoolLevel;
    bool zoneCubeHeld;

    static bool IsPoolPreset(Preset p) => p == Preset.Pool || p == Preset.PoolClub || p == Preset.PoolNight || p == Preset.PoolDark;

    /// <summary>
    /// One pool room's look volume, in district metres: the footprint x0..x1, z0..z1, the room's floor and ceiling, and waterY,
    /// the height of all its pool water (NaN when it has none or more than one height: no height fog), as AddPoolLooks. Made
    /// under the district's root (it sleeps with it); a failure logs once and the district goes on without it.
    /// </summary>
    public FrontRoomsZoneLookVolume PoolRoom(FrontRoomsStructuresDistrict d, string room, string zone, float x0, float z0, float x1, float z1, float floor, float ceiling, float waterY = float.NaN)
    {
        if (d == null || d.Root == null || string.IsNullOrEmpty(zone)) return null;
        try
        {
            var holder = new GameObject("Pool look · " + d.Code + " " + room).transform;
            holder.SetParent(d.Root, false);
            float bottom = floor - PoolRoomBelow, top = ceiling + PoolRoomAbove;
            var box = new Bounds(new Vector3((x0 + x1) * .5f, (bottom + top) * .5f, (z0 + z1) * .5f), new Vector3(Mathf.Abs(x1 - x0), top - bottom, Mathf.Abs(z1 - z0)));
            var v = FrontRoomsZoneLook.AddMapZone(holder, zone, box, PoolRoomBlend, waterY);
            if (v == null)
            {
                FrontRoomsPoolMaterials.LogOnce("pool look " + zone, "the pool zone look " + zone + " could not be placed in " + d.Code + ".");
                Destroy(holder.gameObject);
                return null;
            }
            v.enabled = ZoneLookOn;
            poolRooms.Add(new PoolRoomEntry { volume = v, district = d, name = room, footprint = Rect.MinMaxRect(Mathf.Min(x0, x1), Mathf.Min(z0, z1), Mathf.Max(x0, x1), Mathf.Max(z0, z1)),
                floor = floor, ceiling = ceiling });
            return v;
        }
        catch (System.Exception e)
        {
            FrontRoomsPoolMaterials.LogOnce("pool look " + d.Code + " " + room, d.Code + " pool room " + room + ": the zone look was left out: " + e.Message);
            return null;
        }
    }

    /// <summary>A line of the district's own for the G panel's zone look paragraph (SP1's night, SP2's floors).</summary>
    public void PoolRoomNote(FrontRoomsStructuresDistrict d, string note)
    {
        if (d != null && !string.IsNullOrEmpty(note)) poolNotes[d] = note;
    }

    /// <summary>True when the district has pool rooms with 游戏视觉's look.</summary>
    public bool HasPoolRooms(FrontRoomsStructuresDistrict d)
    {
        if (d == null) return false;
        foreach (var r in poolRooms) if (r.district == d && r.volume != null) return true;
        return false;
    }

    /// <summary>
    /// K2 (structures G2-a, the water's light room, the pool rig's rooms): the smallest pool room of <paramref name="d"/> that
    /// holds the world point (its footprint, and between <paramref name="below"/> under its floor (default PoolRoomBelow: a basin
    /// under an access floor counts) and its ceiling), as the district gave it: footprint (world xz), floor and ceiling (world y;
    /// the dome hall's crown, the grotto's vault). False when none does (the map's pool route, a district without pool rooms).
    /// </summary>
    public bool PoolRoomAt(FrontRoomsStructuresDistrict d, Vector3 world, out Rect footprint, out float floor, out float ceiling, float below = PoolRoomBelow)
    {
        footprint = default(Rect);
        floor = ceiling = 0f;
        if (d == null || d.Root == null) return false;
        var p = d.Root.InverseTransformPoint(world);
        PoolRoomEntry best = null;
        var area = float.MaxValue;
        foreach (var r in poolRooms)
        {
            if (r.district != d || r.footprint.width <= 0f) continue;
            if (p.x < r.footprint.xMin || p.x > r.footprint.xMax || p.z < r.footprint.yMin || p.z > r.footprint.yMax) continue;
            if (p.y < r.floor - below || p.y > r.ceiling) continue;
            var a = r.footprint.width * r.footprint.height;
            if (a < area) { area = a; best = r; }
        }
        if (best == null) return false;
        var o = d.Root.position;
        footprint = new Rect(best.footprint.xMin + o.x, best.footprint.yMin + o.z, best.footprint.width, best.footprint.height);
        floor = best.floor + o.y;
        ceiling = best.ceiling + o.y;
        return true;
    }

    /// <summary>The district's pool rooms' look volumes (logs, tests).</summary>
    public List<FrontRoomsZoneLookVolume> PoolRoomVolumes(FrontRoomsStructuresDistrict d)
    {
        var list = new List<FrontRoomsZoneLookVolume>();
        foreach (var r in poolRooms) if (r.district == d && r.volume != null) list.Add(r.volume);
        return list;
    }

    /// <summary>Q: 游戏视觉's pool zone look on, or today's fixed trilight and grade; ZoneTick applies it this frame.</summary>
    public void SetZoneLook(bool on)
    {
        ZoneLookOn = on;
    }

    /// <summary>
    /// Every frame after Want (the bootstrap; the camera is the walker's view): the rooms' switch, the scene grade's share and
    /// the reflection cube.
    /// </summary>
    public void ZoneTick(FrontRoomsStructuresDistrict d, Vector3 feet, Vector3 camera)
    {
        if (d != null) here = d;
        // 游戏视觉's lamp share for PoolLevel (pool_zone_look_tune, main 11-04 16:48; W/water_light/for_water_line.md section 2):
        // scene 4's pool rooms lack the P1-A and down-fill fixtures the zone's ambient was tuned with, so with Q on the share
        // stands in for them, 3 by day and 0.7 under the night preset (3 x 0.235, the night trilight's linear share); only in
        // our own pool districts (the pool route P keeps the map's 1). Set before the switch below, so a flip's probe capture
        // already sees it.
        var share = ZoneLookOn && here != null && HasPoolRooms(here) ? (current == Preset.PoolNight ? PoolLevelNightShare : PoolLevelShare) : 1f;
        if (share != lampShare)
        {
            lampShare = share;
            FrontRoomsZoneLook.SetLampShare("PoolLevel", share);
        }
        // The current district's rooms hold only where its own preset is a pool one; the other districts' keep their switch
        // (their roots sleep), so coming back to a district does not flip them (its arrival capture renders its probes, GZ-A);
        // a flip here re-renders them through that capture, once.
        var poolHere = IsPoolPreset(current);
        var flipped = false;
        for (var i = poolRooms.Count - 1; i >= 0; i--)
        {
            var r = poolRooms[i];
            if (r.volume == null) { poolRooms.RemoveAt(i); continue; }
            var on = ZoneLookOn && (r.district != d || poolHere);
            if (r.volume.enabled == on) continue;
            r.volume.enabled = on;
            if (r.district == d) flipped = true;
        }
        if (flipped && d != null) RerenderProbes(d.Root);

        // URP weighs the zone grade at the camera, the zone look its ambient too: the scene's grade gives way by as much.
        float w;
        var zone = FrontRoomsZoneLook.ZoneAt(camera, out w);
        ZoneAtCamera = zone ?? "";
        ZoneWeightAtCamera = w;
        GradeShare = ZoneLookOn ? 1f - Mathf.Clamp01(w) : 1f;
        if (poolVolume != null)
        {
            var target = gradeWeight * GradeShare;
            if (Mathf.Abs(poolVolume.weight - target) > 1e-4f) poolVolume.weight = target;
        }

        // The game's reflection cube inside a pool zone (FrontRooms3DGame: weight over 0.5 at the player's feet).
        zoneCubeHeld = false;
        if (ZoneLookOn)
        {
            float wf;
            var at = FrontRoomsZoneLook.ZoneAt(feet, out wf);
            if (wf > .5f && !string.IsNullOrEmpty(at))
            {
                zoneCubeHeld = true;
                zoneCube = at == "PoolDark" ? FrontRoomsLook.ReflectionZone.PoolDead
                    : at == "PoolLevel" ? FrontRoomsLook.ReflectionZone.PoolLevel : FrontRoomsLook.ReflectionZone.Pool;
            }
        }
        ShowReflection(zoneCubeHeld ? zoneCube : presetReflection, .5f);
    }

    /// <summary>The status plate's words ("" in a district without pool rooms).</summary>
    public string ZoneStatus(FrontRoomsStructuresDistrict d)
    {
        if (!HasPoolRooms(d)) return "";
        if (!ZoneLookOn) return "泳池外观：场景原来的固定环境光（Q 换成游戏视觉的）";
        return IsPoolPreset(current) ? "泳池外观：游戏视觉的区域外观（Q 换回原来的）" : "泳池外观：游戏视觉的，只在泳池层（Q 换回原来的）";
    }

    /// <summary>The flash after Q.</summary>
    public string ZoneFlash(FrontRoomsStructuresDistrict d)
    {
        var s = ZoneLookOn
            ? "泳池外观 · 游戏视觉的区域外观：每个泳池房间用游戏里的环境光、雾、水面薄雾和调色，进出房间时渐变；灯不变"
            : "泳池外观 · 场景原来的：整个区一组固定的泳池环境光和调色（今天调好的）；灯不变";
        return HasPoolRooms(d) ? s : s + "（这个区没有泳池房间：SP1、SP2 的 F0 和 SP6 才有）";
    }

    /// <summary>The G panel's paragraph ("" in a district without pool rooms).</summary>
    public string ZoneGuide(FrontRoomsStructuresDistrict d)
    {
        if (!HasPoolRooms(d)) return "";
        string note;
        poolNotes.TryGetValue(d, out note);
        return "泳池外观（Q）：现在是" + (ZoneLookOn ? "游戏视觉的区域外观" : "场景原来的固定环境光") + "。默认开着：这个区每个泳池房间都用游戏视觉的区域外观，"
            + "和游戏里一样：环境光、雾、水面上的一层薄雾和调色，进出房间时在 2.5 米内渐变；瓷砖的亮度和之前调好的瓷白相差不到 5%，颜色稍微偏冷一点。"
            + "按 Q 换回这个场景原来的一组固定泳池环境光和调色，再按一次换回来。两种都不改灯。"
            + (string.IsNullOrEmpty(note) ? "" : note);
    }

    // ---------------------------------------------------------------- the scene's own lamps

    sealed class Lamp
    {
        public Light light;
        public Renderer lens;
        public Color lensEmission;
        public float intensity;
        public bool mayShadow, view, mustShadow;
        public int district;
        public float scale = 1f;
        // GZ-A: its reflection proxies (null: none) and whether its lenses were looked at for them
        public List<Renderer> proxies;
        public bool proxyChecked;
        // NEW (structures_assets, contract K1; null in TODAY): the game's lamp mode (ask 5), the budget's last level, the far
        // pool under a far lamp (FrontRoomsStructuresLamps) and whether it was looked for
        public FrontRoomsStructuresLampModes.State mode;
        public float budget = 1f, modeLevel = 1f;
        // LC-4 (K1 review, structures G3-b): the mode level the lens and pool were last written at (-1: never), and a lens left
        // behind while out of view
        public float lensLevel = -1f;
        public bool lensDirty;
        public Renderer pool;
        public bool poolChecked;
        // K2 (structures G2-a; null / false in TODAY and outside the pool districts' rig): the second renderer the lens drive also
        // writes (P1-A's lit ceiling), a wall box's metal-halide state or a niche's incandescent one (its temperament, colour as
        // built, level and tint now), the box a bounce follows, never a shadow (30_plan rule 6: P1-A's indirect boxes, the
        // bounce and the niches), and a lamp outside the shadow pattern (an extra niche: later lamps keep their turns)
        public Renderer glow;
        public bool hid, niche, noShadow, outside;
        public FrontRoomsLampKit.HidLamp hidLamp;
        public FrontRoomsStructuresLampModes.Mode hidMode;
        public Color hidColor = Color.white, hidTint = Color.white;
        public float hidLevel = 1f;
        public Lamp leader;
        // LW3 (FrontRoomsLampLens, read only while Tied): the lens's kit opening (m2; 0 = not looked up yet) and the lens-only
        // share its builder baked into lensEmission (SP1's pool grid 0.4)
        public float lensArea, lensShare = 1f;
    }

    readonly List<Lamp> lamps = new List<Lamp>();
    // K2: lamps added outside the shadow pattern (they take no place in it), and the pool rig's lamps and line (Dip)
    int outsideCount, rigCount;
    int dipDistrict = int.MinValue;
    float dipLine = 1f, dipUntil = -1f;
    readonly Dictionary<int, int> firstBuildIndex = new Dictionary<int, int>();
    int buildingDistrict = int.MinValue, buildingIndex = -1;
    bool rebuilding;
    float budgetClock;

    /// <summary>A district rebuilt in place leaves: its lamps drop out of the list (the cost line's 'may shadow' counted them
    /// twice, and every district built for the first time afterwards counted its shadow pattern on from too far) and its pool
    /// note goes (review F2, A, H).</summary>
    public void Forget(FrontRoomsStructuresDistrict d)
    {
        if (d == null) return;
        foreach (var l in lamps)
            if (l.district == d.Key)
            {
                if (l.outside) outsideCount--;
                if (l.hid || l.niche || l.leader != null) rigCount--;
            }
        lamps.RemoveAll(l => l.district == d.Key);
        poolNotes.Remove(d);
    }

    /// <summary>The bootstrap is about to build a district (its first build, or again after the structure assets key): its lamps'
    /// shadow pattern starts where its first build's did.</summary>
    public void BeginDistrictBuild(int district)
    {
        buildingDistrict = district;
        rebuilding = firstBuildIndex.TryGetValue(district, out buildingIndex);
        if (!rebuilding) firstBuildIndex[district] = buildingIndex = lamps.Count - outsideCount;
    }

    /// <summary>The district's build is done.</summary>
    public void EndDistrictBuild()
    {
        // a rebuild runs the budget in its own frame: no frame with every fresh lamp at full, the far pools made with the build
        if (rebuilding) budgetClock = 0f;
        buildingDistrict = int.MinValue;
        buildingIndex = -1;
        rebuilding = false;
    }
    MaterialPropertyBlock lensBlock;
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    /// <summary>A lamp of the scene's own (district = its key): on within 16 m of the walker (3 m fade), every third may shadow within 9 m; a view lamp stays on while its district is current.</summary>
    public void AddLamp(int district, Light light, Renderer lens, Color lensEmission, bool view = false)
    {
        if (light == null) return;
        // every third lamp may shadow, by its place in the build order: lamps.Count on a district's first build (as always); a
        // district built again (the structure assets key) counts on from where its first build started, so the same lamps cast
        // shadows in TODAY and in NEW and a comparison in place shows only the assets
        // (K2: a lamp added while AddingOutside takes no turn and never shadows; TODAY never sets it)
        var outside = AddingOutside;
        var index = lamps.Count - outsideCount;
        if (rebuilding && district == buildingDistrict && buildingIndex >= 0 && !outside) index = buildingIndex++;
        lamps.Add(new Lamp { light = light, lens = lens, lensEmission = lensEmission, intensity = light.intensity, mayShadow = !outside && index % 3 == 0, view = view, district = district,
            outside = outside, noShadow = outside });
        if (outside) outsideCount++;
        // NEW: the game's lamp modes (ask 5) on the lamps with a lens of the game's (the troffers): each rolls its temperament
        // from its place; the pool districts and the fixture boxes (their HID kits come with the pool package) stay steady
        if (FrontRoomsStructuresAssets.NewFor(district))
        {
            var p = light.transform.position;
            var seed = district * 7919 ^ Mathf.RoundToInt(p.x * 10f) * 73856093 ^ Mathf.RoundToInt(p.y * 10f) * 19349663 ^ Mathf.RoundToInt(p.z * 10f) * 83492791;
            var steady = lens == null || district == 1 || district == 2 || district == 6;
            lamps[lamps.Count - 1].mode = FrontRoomsStructuresLampModes.Roll(seed, steady);
        }
        if (lens != null)
        {
            if (lensBlock == null) lensBlock = new MaterialPropertyBlock();
            lens.GetPropertyBlock(lensBlock);
            // LW3: a lamp built while the hook is Tied takes its tied lens at once (Today: the emission given, as always)
            lensBlock.SetColor(EmissionColorId, FrontRoomsLampLens.Tied ? TiedLens(lamps[lamps.Count - 1]) : lensEmission);
            lens.SetPropertyBlock(lensBlock);
        }
    }

    /// <summary>
    /// A trilight ambient (sky, equator, ground as RenderSettings takes them, gamma) as an L2 probe, for renderers that keep
    /// another zone's ambient (SP7's vestibule): the gradient environment (up sky, horizon equator, down ground) projected
    /// from 512 directions, each a directional term of radiance x 4 / N, so a lit Lambert surface reads as under the trilight.
    /// </summary>
    public static SphericalHarmonicsL2 TrilightSH(Color sky, Color equator, Color ground)
    {
        var sh = new SphericalHarmonicsL2();
        sh.Clear();
        Color s = sky.linear, e = equator.linear, g = ground.linear;
        const int n = 512;
        var golden = Mathf.PI * (3f - Mathf.Sqrt(5f));
        for (var i = 0; i < n; i++)
        {
            var y = 1f - 2f * (i + .5f) / n;
            var r = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));
            var a = golden * i;
            var d = new Vector3(r * Mathf.Cos(a), y, r * Mathf.Sin(a));
            var c = y >= 0f ? Color.Lerp(e, s, y) : Color.Lerp(e, g, -y);
            sh.AddDirectionalLight(d, c, 4f / n);
        }
        return sh;
    }

    /// <summary>A lamp that always casts its shadow while on (a lamp whose light must not pass through walls: SP5's sodium heads;
    /// review SP5-F4). Unknown lights are ignored.</summary>
    public void Shadowed(Light light)
    {
        if (light == null) return;
        foreach (var l in lamps)
            if (l.light == light) { l.mustShadow = true; break; }
        budgetClock = 0f;
    }

    /// <summary>Scale one lamp (0 keeps it off; SP1's night lines and dimmed hall lamps). Unknown lights are ignored. Its
    /// reflection proxies follow at once, and a lamp of the district the walker is in has that district's probes captured
    /// again (GZ-A: a lamp state change). Scene 4's pool lamps are steady (nothing flickers), so that is one capture a key.</summary>
    public void Scale(Light light, float scale)
    {
        if (light == null) return;
        foreach (var l in lamps)
            if (l.light == light)
            {
                var s = Mathf.Max(0f, scale);
                var changed = Mathf.Abs(l.scale - s) > 1e-4f;
                l.scale = s;
                if (changed)
                {
                    Drive(l);
                    // NEW (contract K1): the lens follows the lamp, as the game's dead lamp has a dark lens (G1 review FIX 2: with
                    // every light of a district at 0 the lenses stayed lit); TODAY's lens is set once, as before
                    // (LW3: while the lens hook is Tied every lens follows its lamp)
                    if (l.mode != null || l.hid || l.niche || FrontRoomsLampLens.Tied) DriveLens(l);
                    if (here != null && l.district == here.Key) RequestCapture(here);
                }
                break;
            }
        budgetClock = 0f;
    }

    // NEW: a lamp's lens at its emission x its mode's level x its scale (0 = a dark lens). K2: a pool rig lamp's at its drive's
    // level and tint instead, and its glow (P1-A's lit ceiling) takes the lens's block, so a dead box leaves its bay dark.
    void DriveLens(Lamp l)
    {
        if (l.lens == null && l.glow == null) return;
        if (lensBlock == null) lensBlock = new MaterialPropertyBlock();
        var c = l.hid || l.niche ? l.lensEmission * l.hidTint * (l.hidLevel * l.scale) : l.lensEmission * (l.modeLevel * l.scale);
        if (l.lens != null)
        {
            l.lens.GetPropertyBlock(lensBlock);
            // LW3: Tied, the lens from its own light; the glow below keeps today's drive (it is the lit ceiling, not the lens)
            lensBlock.SetColor(LensEmissionId, FrontRoomsLampLens.Tied ? TiedLens(l) : c);
            l.lens.SetPropertyBlock(lensBlock);
        }
        if (l.glow != null)
        {
            l.glow.GetPropertyBlock(lensBlock);
            lensBlock.SetColor(LensEmissionId, c);
            l.glow.SetPropertyBlock(lensBlock);
        }
    }

    // ---------------------------------------------------------------- LW3: 游戏视觉's lens hook (FrontRoomsLampLens)
    //
    // research/light_design/10_research.md LW3 (Red 2026-11-06 "全部按推荐"; the A/B before anything lands): Today (the default)
    // writes every lens exactly as above, so every frame is today's. Tied: a lamp's lens is pi x I / A x its light's colour,
    // I = its full intensity x its state (mode, metal-halide or niche level, scale), never the walker's budget (a far lamp keeps
    // its lit lens), A its kit's opening, times the share its builder baked in (SP1's pool grid 0.4). A flip of the hook
    // re-drives every lens once and recaptures the current district's probes; nothing runs per frame at rest.

    int lensVersion;

    Color TiedLens(Lamp l)
    {
        if (l.lensArea <= 0f)
        {
            l.lensArea = FrontRoomsLampLens.AreaOf(l.lens);
            l.lensShare = FrontRoomsLampLens.ShareOf(l.lens, l.lensEmission);
        }
        var state = (l.hid || l.niche ? l.hidLevel : l.modeLevel) * l.scale;
        return FrontRoomsLampLens.Drive(l.light, l.intensity * state, l.lensArea, l.lensShare);
    }

    // The hook moved (Toggle, Set, ViewFactor while Tied): each lens once. Tied from its light; Today what today's code had
    // written (a moded or pool-rig lamp at its state, any other at the emission it was built with).
    void RelightLenses()
    {
        lensVersion = FrontRoomsLampLens.Version;
        if (lensBlock == null) lensBlock = new MaterialPropertyBlock();
        for (var i = 0; i < lamps.Count; i++)
        {
            var l = lamps[i];
            if (l.lens == null || l.light == null) continue;
            if (FrontRoomsLampLens.Tied || l.mode != null || l.hid || l.niche) { DriveLens(l); continue; }
            l.lens.GetPropertyBlock(lensBlock);
            lensBlock.SetColor(EmissionColorId, l.lensEmission);
            l.lens.SetPropertyBlock(lensBlock);
        }
        if (here != null) RequestCapture(here);
    }

    /// <summary>Lamps of one district, for its cost line.</summary>
    public void CountLamps(int district, out int lights, out int shadowed)
    {
        lights = shadowed = 0;
        foreach (var l in lamps)
        {
            if (l.district != district || l.light == null) continue;
            lights++;
            if (l.mayShadow || l.mustShadow) shadowed++;
        }
    }

    /// <summary>The budget, ten times a second: called by the bootstrap with the walker's eye and the current district.</summary>
    public void Tick(Vector3 eye, int district, float dt)
    {
        // LW3: the lens hook moved since the last frame (one int compare otherwise)
        if (lensVersion != FrontRoomsLampLens.Version) RelightLenses();
        if (environmentDirty >= 0f)
        {
            environmentDirty += dt;
            if (environmentDirty > 0f)
            {
                DynamicGI.UpdateEnvironment();
                environmentDirty = -1f;
            }
        }
        if (pendingProbes.Count > 0 && --probeFrames <= 0)
        {
            foreach (var p in pendingProbes)
            {
                if (p == null || !p.isActiveAndEnabled) continue;
                // the current district's go into its capture (its lamps on, GZ-A); any other renders as before
                if (here != null && here.Root != null && p.transform.IsChildOf(here.Root)) { RequestCapture(here, p); continue; }
                try { p.RenderProbe(); renderedProbes.Add(p); }
                catch (System.Exception e) { FrontRoomsPoolMaterials.LogOnce("probe render", "a reflection probe did not render: " + e.Message); }
            }
            pendingProbes.Clear();
        }
        // GZ-A: the capture's frame. A finished capture gives the budget back, a due one forces its district's lamps on; either
        // runs the budget in this frame, before the probes render (they render with this frame's cameras).
        var startCapture = CaptureTick(district);
        FrontRoomsPoolMaterials.Animate(Time.time);
        budgetClock -= dt;
        if (budgetClock <= 0f)
        {
            budgetClock = .1f;
            foreach (var l in lamps)
            {
                if (l.light == null) continue;
                var mine = l.district == district;
                var d = Vector3.Distance(eye, l.light.transform.position);
                // while its district's probes are captured every lamp of it is on: the view flag for all of them (GZ-A)
                var view = l.view || l.district == forcedDistrict;
                var level = (!mine && l.district != 0 ? 0f : view && mine ? 1f : Mathf.Clamp01((19f - d) / 3f)) * l.scale;
                if (l.mode != null)
                {
                    // NEW: the lamp's mode on top of the budget, and its far pool where the budget has taken its light
                    l.budget = level;
                    level *= l.modeLevel;
                    if (mine) FarPool(l, d);
                }
                else if (l.hid || l.niche || l.leader != null)
                {
                    // K2: the pool rig's drive on top of the budget (TickHid: a box's metal-halide lamp, a niche, a bounce)
                    l.budget = level;
                    level *= l.hidLevel;
                }
                var on = level > .001f;
                if (l.light.enabled != on) l.light.enabled = on;
                if (on) l.light.intensity = l.intensity * level;
                var shadow = on && !l.noShadow && (l.mustShadow || (l.mayShadow && d < 9f)) ? LightShadows.Soft : LightShadows.None;
                if (l.light.shadows != shadow) l.light.shadows = shadow;
            }
        }
        if (startCapture) RenderCapture();
        TickHid(district, dt);
        TickModes(district, dt);
    }

    // ---------------------------------------------------------------- NEW: the game's lamp modes and the far pools (contract K1)
    //
    // 游戏视觉's structures_assets (30_plan rows H02, S3-01, X-04; ask 5; FrontRoomsStructuresLampModes, FrontRoomsStructuresLamps):
    // each frame the current district's moded lamps advance (the game's shapes, photosafe, Reduce Flashing honoured) and their
    // light, lens and pool follow; a district's probes are captured with its lamps at their current level. TODAY has no moded
    // lamp, so nothing here runs.

    static readonly int LensEmissionId = Shader.PropertyToID("_EmissionColor");

    void TickModes(int district, float dt)
    {
        for (var i = 0; i < lamps.Count; i++)
        {
            var l = lamps[i];
            if (l.mode == null || l.light == null || l.district != district) continue;
            // a capture sees each lamp as the walker does: forcing the moded lamps to full for it struck every dead and dim troffer
            // of the district at once, past the per-lamp limiter and Reduce Flashing (review LC-1)
            var v = FrontRoomsStructuresLampModes.Advance(l.mode, dt);
            var seen = l.lens == null || l.lens.isVisible;
            if (Mathf.Abs(v - l.modeLevel) < .004f)
            {
                // LC-4: a lens left behind while out of view catches up once it is seen
                if (l.lensDirty && seen) { DriveLens(l); l.lensLevel = l.modeLevel; l.lensDirty = false; }
                continue;
            }
            l.modeLevel = v;
            var level = l.budget * v;   // the budget's level already carries the lamp's scale
            var on = level > .001f;
            if (l.light.enabled != on) l.light.enabled = on;
            if (on) l.light.intensity = l.intensity * level;
            // LC-4 (K1 review): the lens (at the mode's level x the lamp's scale) and its pool only on a step of .05 or more or at the
            // ends (dark, full), the lens only while it is in view; the light follows every step as before (SP3 wrote about 650
            // property blocks a frame)
            if (Mathf.Abs(v - l.lensLevel) >= .05f || ((v <= .001f || v >= .999f) && Mathf.Abs(v - l.lensLevel) > 1e-4f))
            {
                if (seen) { DriveLens(l); l.lensLevel = v; l.lensDirty = false; }
                else l.lensDirty = true;
                if (l.pool != null) FrontRoomsStructuresLamps.SetPool(l.pool, l.light.color, Mathf.Max(0f, l.scale - l.budget) * v);
            }
        }
    }

    // A far lamp's pool (made the first time it is wanted, after the build, on the floor found under the lamp; clipped short of
    // the walls round it), driven by the part of its light the budget has taken away.
    void FarPool(Lamp l, float distance)
    {
        if (l.lens == null) return;
        if (!l.poolChecked && l.budget < l.scale - .001f)
        {
            l.poolChecked = true;
            var origin = l.light.transform.position;
            RaycastHit hit;
            if (Physics.Raycast(origin + Vector3.down * .05f, Vector3.down, out hit, 20f, ~ProxyMask, QueryTriggerInteraction.Ignore))
            {
                var h = origin.y - hit.point.y;
                var radius = Mathf.Clamp(Mathf.Tan(l.light.innerSpotAngle * .5f * Mathf.Deg2Rad) * h, .8f, 3.2f);
                var c = hit.point + Vector3.up * .3f;
                float x0 = hit.point.x - radius, x1 = hit.point.x + radius, z0 = hit.point.z - radius, z1 = hit.point.z + radius;
                if (Physics.Raycast(c, Vector3.left, out var wl, radius, ~ProxyMask, QueryTriggerInteraction.Ignore)) x0 = wl.point.x;
                if (Physics.Raycast(c, Vector3.right, out var wr, radius, ~ProxyMask, QueryTriggerInteraction.Ignore)) x1 = wr.point.x;
                if (Physics.Raycast(c, Vector3.back, out var wb, radius, ~ProxyMask, QueryTriggerInteraction.Ignore)) z0 = wb.point.z;
                if (Physics.Raycast(c, Vector3.forward, out var wf, radius, ~ProxyMask, QueryTriggerInteraction.Ignore)) z1 = wf.point.z;
                var parent = l.light.transform.parent != null ? l.light.transform.parent.parent : null;
                if (parent == null) parent = l.light.transform;
                var local = parent.InverseTransformPoint(hit.point);
                var lo = parent.InverseTransformPoint(new Vector3(x0, hit.point.y, z0));
                var hi = parent.InverseTransformPoint(new Vector3(x1, hit.point.y, z1));
                l.pool = FrontRoomsStructuresLamps.BuildPool(parent, local, radius, Rect.MinMaxRect(Mathf.Min(lo.x, hi.x), Mathf.Min(lo.z, hi.z), Mathf.Max(lo.x, hi.x), Mathf.Max(lo.z, hi.z)), "far lamp pool");
            }
        }
        if (l.pool != null) FrontRoomsStructuresLamps.SetPool(l.pool, l.light.color, Mathf.Max(0f, l.scale - l.budget) * l.modeLevel);
    }

    // ---------------------------------------------------------------- K2: the pool rig's lamps (structures G2-a)
    //
    // 游戏视觉's structures_assets G2-a (Red 2026-11-07 21:54: image 1 lit by image 2's lamps; FrontRoomsStructuresPoolLamps): in
    // NEW, SP1, SP2 and SP6 put the zone lamps line's P1-A wall boxes, P2-A / ZL_NICHE_S niches and ZL_FLUSH on the lights they
    // always made (FrontRoomsStructuresKit.PlaceLampKits). Each box's light is a metal-halide lamp (its temperament from the
    // game's odds, its line, HidTick's drop-out, restrike and warm-up, the quartz lamp's 15 %); each niche an incandescent one
    // (Steady or Dead; a dip dims it); a bounce follows its box. Nothing here runs in TODAY or for any other lamp.

    /// <summary>K2: lamps added while this is set take no turn in the shadow pattern and never shadow (an extra niche).</summary>
    public bool AddingOutside;

    Lamp Find(Light light)
    {
        if (light == null) return null;
        for (var i = lamps.Count - 1; i >= 0; i--) if (lamps[i].light == light) return lamps[i];
        return null;
    }

    /// <summary>K2: a lamp's kit lens (and its glow) after the build, driven like a lens given to AddLamp.</summary>
    public void AttachLens(Light light, Renderer lens, Color lensEmission, Renderer glow = null)
    {
        var l = Find(light);
        if (l == null) return;
        l.lens = lens;
        l.glow = glow;
        l.lensEmission = lensEmission;
        l.lensArea = 0f;   // LW3: a new lens, its opening looked up again when Tied
        l.lensShare = 1f;
        DriveLens(l);
    }

    /// <summary>K2: the light's intensity as it is now is its full level (a small niche's x 0.4, set after the build).</summary>
    public void Retune(Light light)
    {
        var l = Find(light);
        if (l == null) return;
        l.intensity = light.intensity;
        if (FrontRoomsLampLens.Tied) DriveLens(l);   // LW3: a tied lens follows its light's new full level
    }

    /// <summary>K2: a wall box's light as a metal-halide lamp: its temperament rolled from <paramref name="seed"/> at the game's
    /// odds (Dead, Stutter / Dim at 60 %, Failing cycling), or Steady when <paramref name="steady"/> (a room's only boxes), its
    /// drift, never a shadow; its own moded state goes (no far pool).</summary>
    public void SetHid(Light light, int seed, float drift, bool steady = false)
    {
        var l = Find(light);
        if (l == null) return;
        if (!l.hid && !l.niche && l.leader == null) rigCount++;
        l.hid = true;
        l.niche = false;
        l.noShadow = true;
        l.mode = null;
        l.hidMode = steady ? FrontRoomsStructuresLampModes.Mode.Steady : FrontRoomsStructuresLampModes.Roll(seed, false).mode;
        l.hidColor = light.color;
        l.hidLamp = FrontRoomsLampKit.HidLamp.Lit(drift, l.hidMode == FrontRoomsStructuresLampModes.Mode.Failing);
        Color c;
        l.hidLevel = FrontRoomsStructuresPoolLamps.HidLevel(ref l.hidLamp, ModeOf(l), 1f, 0f, out c);
        l.hidTint = c;
        light.color = l.hidColor * c;
        DriveLens(l);
    }

    /// <summary>K2: a niche's light as an incandescent lamp (Steady or Dead from <paramref name="seed"/>), never a shadow.</summary>
    public void SetNiche(Light light, int seed)
    {
        var l = Find(light);
        if (l == null) return;
        if (!l.hid && !l.niche && l.leader == null) rigCount++;
        l.niche = true;
        l.hid = false;
        l.noShadow = true;
        l.mode = null;
        l.hidMode = FrontRoomsStructuresPoolLamps.Incandescent(FrontRoomsStructuresLampModes.Roll(seed, false).mode);
        l.hidColor = light.color;
        l.hidTint = Color.white;
        l.hidLevel = FrontRoomsStructuresPoolLamps.NicheLevel(ModeOf(l), 1f);
        DriveLens(l);
    }

    /// <summary>K2: a bounce (a down-fill without a body) follows its box's level and colour; never a shadow.</summary>
    public void Follow(Light light, Light leader)
    {
        var l = Find(light);
        var b = Find(leader);
        if (l == null || b == null || b == l) return;
        if (!l.hid && !l.niche && l.leader == null) rigCount++;
        l.leader = b;
        l.noShadow = true;
        l.mode = null;
        l.hidColor = light.color;
        l.hidLevel = b.hidLevel;
        l.hidTint = b.hidTint;
        light.color = l.hidColor * b.hidTint;
    }

    /// <summary>K2: a lamp that never shadows (30_plan rule 6), whatever its turn in the pattern.</summary>
    public void NoShadow(Light light)
    {
        var l = Find(light);
        if (l != null) l.noShadow = true;
    }

    /// <summary>K2: a dip of a district's line (the pool rig's key): to <paramref name="line"/> for <paramref name="seconds"/>. Under
    /// 0.80 every box's arc goes out at once (one step), the quartz lamps hold 15 %, the arcs restrike 8 s later and warm up over
    /// 24 s; the niches dim with the line and come back.</summary>
    public void Dip(int district, float line, float seconds)
    {
        dipDistrict = district;
        dipLine = Mathf.Clamp01(line);
        dipUntil = Time.time + Mathf.Max(0f, seconds);
    }

    /// <summary>K2: the district's pool rig in one line (boxes, niches, bounces; dead, aged, failing), for the G panel and logs.</summary>
    public string RigLine(int district)
    {
        int boxes = 0, niches = 0, bounces = 0, dead = 0, aged = 0, failing = 0;
        foreach (var l in lamps)
        {
            if (l.district != district || l.light == null) continue;
            if (l.leader != null) { bounces++; continue; }
            if (!l.hid && !l.niche) continue;
            if (l.hid) boxes++; else niches++;
            var m = l.hidMode;
            if (m == FrontRoomsStructuresLampModes.Mode.Dead) dead++;
            else if (m == FrontRoomsStructuresLampModes.Mode.Stutter || m == FrontRoomsStructuresLampModes.Mode.Dim) aged++;
            else if (m == FrontRoomsStructuresLampModes.Mode.Failing) failing++;
        }
        return boxes + " wall boxes, " + niches + " niches, " + bounces + " bounces; " + dead + " dead, " + aged + " aged, " + failing + " failing";
    }

    // Ask 5's A (every lamp steady) holds the rig steady too.
    static FrontRoomsStructuresLampModes.Mode ModeOf(Lamp l) =>
        FrontRoomsStructuresLampModes.GameModes ? l.hidMode : FrontRoomsStructuresLampModes.Mode.Steady;

    // Each frame for the current district: the boxes and niches advance, the bounces follow; a light, lens and glow are written
    // only when the level or colour moves (no property block written at rest: the K1 review's LC-4).
    void TickHid(int district, float dt)
    {
        if (rigCount <= 0) return;
        var mains = district == dipDistrict && Time.time < dipUntil ? dipLine : 1f;
        for (var i = 0; i < lamps.Count; i++)
        {
            var l = lamps[i];
            if (l.light == null || l.district != district || l.leader != null || !(l.hid || l.niche)) continue;
            Color c;
            float v;
            if (l.hid) v = FrontRoomsStructuresPoolLamps.HidLevel(ref l.hidLamp, ModeOf(l), mains, dt, out c);
            else { v = FrontRoomsStructuresPoolLamps.NicheLevel(ModeOf(l), mains); c = Color.white; }
            ApplyHid(l, v, c);
        }
        for (var i = 0; i < lamps.Count; i++)
        {
            var l = lamps[i];
            if (l.light != null && l.district == district && l.leader != null) ApplyHid(l, l.leader.hidLevel, l.leader.hidTint);
        }
    }

    void ApplyHid(Lamp l, float v, Color c)
    {
        if (Mathf.Abs(v - l.hidLevel) < .004f && Mathf.Abs(c.r - l.hidTint.r) + Mathf.Abs(c.g - l.hidTint.g) + Mathf.Abs(c.b - l.hidTint.b) < .006f) return;
        l.hidLevel = v;
        l.hidTint = c;
        l.light.color = l.hidColor * c;
        var level = l.budget * v;
        var on = level > .001f;
        if (l.light.enabled != on) l.light.enabled = on;
        if (on) l.light.intensity = l.intensity * level;
        DriveLens(l);
    }

    // ---------------------------------------------------------------- the pool glaze's lamps and probes (GZ-A)
    //
    // Red picked GZ-C on 2026-11-04 ("选这个", research/level_space/73_pool_glaze.md section 6), landed in the order A, B, C;
    // A is this scene's own (section 3), B and C are 游戏视觉's tile package. The glaze shows a lamp only when the lamp is
    // 50-200 times a lit wall; the lenses Red sees are 2-5 times (Red's levels, kept), and the probes used to be rendered once
    // after the build, with the walker's budget already putting far lamps out. Two parts here, as the option sheet's GZ-A
    // column rendered them (its clone rig, W/level_space/glaze/clone_src):
    // Reflection proxies (section 3.3). Every lens of a pool-family lamp (a lamp of the district whose preset where it hangs is
    // a pool one: SP1, SP2's F0, SP6, the kit row's tile hall) gets a copy of its lens mesh 2 mm in front of it along the
    // lamp's beam, on layer 27, unlit at the lamp's tint x ProxyGain; the oculus (no lens of its own) a 1.1 m disc in its
    // shaft at the light. Not the wall boxes: their lens faces the lid, and the lit lid is captured as it is. Nor SP2's
    // laylight: its 6 x 6 m diffuser is no lens (at a compact lens's gain it would flood SP2's probe). A proxy follows its
    // lamp's state (Scale: the night key, a lamp put out or dimmed) through its own property block, never the walker's
    // distance budget: a lamp that is on is on in every reflection. Made the first time the district's probes are captured,
    // from the lenses there are then (the kit's quad or round lens, the troffer kit's, a district's own round lenses).
    // Only reflection-probe captures render layer 27. Every other camera skips it: the walker's from its creation; each frame
    // before anything renders every enabled camera and the water mirrors' viewer (FrontRoomsWaterPlanar copies its viewer's
    // mask into each mirror camera, so the mirrors skip it too); and every camera as it begins rendering
    // (RenderPipelineManager.beginCameraRendering, so a camera made later, the review harness's, skips it from its first
    // frame; GlassRT takes its instances by the rendering camera's mask, so it leaves them out too). The water mirrors are
    // Reflection cameras like the probes' own, so a camera is told apart as a probe's by its eye on a probe of this look or a
    // 90-degree square view.
    // Probe capture (section 3.4). A district's probes are captured two frames after the walker arrives in it (Entered),
    // again after one of its keys (Changed: SP1's night key and flume, SP6's dome light), a state change of one of its lamps
    // (Scale) and a flip of the zone look (Q, through RerenderProbes, once), and whenever something renders one of its probes
    // while its lamps are not forced (the water preview re-renders them after its own keys and on arrival): each time with
    // every lamp of the district on for the frame the probes render, then the budget returns. Sizes and settings unchanged.

    /// <summary>The reflection proxies' layer: unused in the project (the game's are 4, 5, 28-31); only probe captures render it.</summary>
    public const int ProxyLayer = 27;
    public const int ProxyMask = 1 << ProxyLayer;
    /// <summary>
    /// A proxy's drive: its lamp's tint (linear) times this, passed as the option sheet's rig passed it, as a material colour,
    /// which Unity reads as sRGB and linearises once more (MaterialPropertyBlock.SetColor in a linear project): about 1200-1800
    /// on the GPU, not 30 times the tint (73 section 3.3 wrote linear). ProxyRadiance keeps that level, the sheet's GZ-A, at
    /// a lamp's full state and dims it linearly with the lamp. On this scale the design's 15-60 tuning range is steep: 15 is
    /// about a fifth of 30.
    /// </summary>
    public const float ProxyGain = 30f;
    /// <summary>How far the proxy sits in front of its lens, along its lamp's beam (m).</summary>
    public const float ProxyOffset = .002f;
    /// <summary>The oculus' proxy: a disc of this diameter at its light, inside its 1.2 m shaft.</summary>
    public const float OculusDisc = 1.1f;
    /// <summary>The most frames a capture keeps its district's lamps forced on while its probes have not been seen rendering.</summary>
    public const int CaptureFramesMax = 8;
    const string ProxyName = "Reflection proxy (layer 27, probes only)";

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    Material proxyMaterial;
    Mesh proxyDisc;
    MaterialPropertyBlock proxyBlock;
    bool proxyShaderMissing;
    // the district the walker is in (Entered, ZoneTick); the capture asked for (district, all its probes or some), its
    // count-down; the capture in flight (its district's lamps forced on, its probes, which were seen rendering, their ids)
    FrontRoomsStructuresDistrict here, captureFor;
    readonly List<ReflectionProbe> captureOnly = new List<ReflectionProbe>();
    bool captureAll;
    int captureFrames = -1, forcedDistrict = -1, forcedFrames, captureFrame;
    readonly List<ReflectionProbe> capturing = new List<ReflectionProbe>();
    readonly List<int> captureIds = new List<int>();
    readonly HashSet<ReflectionProbe> seen = new HashSet<ReflectionProbe>();
    // every probe this look made or captured (a camera at one of them is that probe's capture)
    readonly List<ReflectionProbe> known = new List<ReflectionProbe>();
    readonly List<Transform> lensBuffer = new List<Transform>();
    Camera[] cameraBuffer = new Camera[8];
    bool loggedProbeCamera, loggedUnseen;
    readonly HashSet<CameraType> loggedSkip = new HashSet<CameraType>();
    // An in-place rebuild's old probes (the ; key and the asks), kept lighting the fresh district until its own capture ends: a
    // realtime probe has no texture before it renders, and the two frames before the capture drew every tile matte and took
    // the lamps' glints out of the water, a flash at every press (the K1 water check, 2026-11-06)
    readonly List<ReflectionProbe> heldProbes = new List<ReflectionProbe>();
    readonly HashSet<ReflectionProbe> renderedProbes = new HashSet<ReflectionProbe>();
    int heldKey = int.MinValue, heldFrame, heldUntil;

    /// <summary>A district about to be rebuilt in place hands over its rendered probes, moved under keeper so they stay on while
    /// the old root goes: they light the fresh district until its first capture ends, then go.</summary>
    public void HoldProbes(int district, Transform oldRoot, Transform keeper)
    {
        if (oldRoot == null || keeper == null) return;
        renderedProbes.RemoveWhere(p => p == null);
        var rendered = new List<ReflectionProbe>();
        foreach (var p in oldRoot.GetComponentsInChildren<ReflectionProbe>(false))
            if (p != null && renderedProbes.Contains(p)) rendered.Add(p);
        if (rendered.Count > 0)
        {
            ReleaseHeldProbes();
            foreach (var p in rendered)
            {
                p.transform.SetParent(keeper, true);
                // above the fresh probe in the same box, so the engine's choice is a rule and not a tie-break
                p.importance += 1;
                heldProbes.Add(p);
            }
        }
        // a second press before the first one's capture: the old root's probes have not rendered, the held ones stay
        else if (heldKey != district) ReleaseHeldProbes();
        if (heldProbes.Count == 0) return;
        heldKey = district;
        heldFrame = Time.frameCount;
        heldUntil = Time.frameCount + CaptureFramesMax + 8;
    }

    void ReleaseHeldProbes()
    {
        foreach (var p in heldProbes)
        {
            renderedProbes.Remove(p);
            if (p != null) Destroy(p.gameObject);
        }
        heldProbes.Clear();
        heldKey = int.MinValue;
    }

    /// <summary>The walker arrived in a district (the bootstrap, when it becomes the current one): its probes are captured two
    /// frames from now, with its lamps on.</summary>
    public void Entered(FrontRoomsStructuresDistrict d)
    {
        if (d == null) return;
        here = d;
        RequestCapture(d);
    }

    /// <summary>One of the district's keys changed it (SP1's night key and flume, SP6's dome light): its probes again.</summary>
    public void Changed(FrontRoomsStructuresDistrict d)
    {
        if (d != null && d == here) RequestCapture(d);
    }

    /// <summary>A camera of a tool's own skips the reflection proxies (call it before the camera renders or is mirrored by
    /// FrontRoomsWaterPlanar.RenderFor; the scene does it for every camera it can reach). Reflection cameras are left alone
    /// here: the probes' captures must see the proxies (OnBeginCamera tells the water mirrors apart).</summary>
    public static void CullProxies(Camera c)
    {
        if (c != null && c.cameraType != CameraType.Reflection && (c.cullingMask & ProxyMask) != 0) c.cullingMask &= ~ProxyMask;
    }

    /// <summary>A proxy's radiance at its lamp's full state, as the GPU gets it (linear): the lamp's tint (linear) x ProxyGain,
    /// linearised once more, which is what the option sheet drew (ProxyGain).</summary>
    public static Color ProxyRadiance(Color lampColor) => (lampColor.linear * ProxyGain).linear;

    // An ask for a capture of d's probes (all, or one) two frames from now; asks for the same district coalesce, an ask for
    // another one replaces it (only the district the walker is in is captured).
    void RequestCapture(FrontRoomsStructuresDistrict d, ReflectionProbe only = null)
    {
        if (d == null || d.Root == null) return;
        if (captureFor != d)
        {
            captureFor = d;
            captureAll = false;
            captureOnly.Clear();
            captureFrames = -1;
        }
        if (only == null) captureAll = true;
        else if (!captureOnly.Contains(only)) captureOnly.Add(only);
        // Tick counts the frame of the ask (it runs after the bootstrap's jumps and keys): the capture is two frames after it
        if (captureFrames < 0) captureFrames = 3;
    }

    // Once a frame, in Tick before the budget: a capture in flight ends when its probes were seen rendering with the lamps on
    // (or say they are done, or the frames run out) and the budget comes back; a due capture then starts: its district's
    // proxies made and driven, its lamps forced on. True when the probes are to render this frame (RenderCapture).
    bool CaptureTick(int district)
    {
        if (heldProbes.Count > 0 && Time.frameCount > heldUntil) ReleaseHeldProbes();
        if (forcedDistrict >= 0)
        {
            forcedFrames--;
            var seenAll = SeenAll();
            var done = seenAll || (FinishedAll() && Time.frameCount - captureFrame >= 2);
            if (!done && forcedFrames > 0 && forcedDistrict == district) return false;
            if (!seenAll && forcedDistrict == district && !loggedUnseen)
            {
                loggedUnseen = true;
                Debug.LogWarning(FrontRoomsStructures.Log + "a probe capture (GZ-A) ended without its probes seen rendering ("
                    + (done ? "they say they are done" : "after " + CaptureFramesMax + " frames") + "): if the pool glaze shows no lamp images, "
                    + "the probes' cameras were not recognised and layer " + ProxyLayer + " was culled from them.");
            }
            // the fresh district's own probes have rendered: the old ones that lit it meanwhile go
            if (heldProbes.Count > 0 && forcedDistrict == heldKey && captureFrame > heldFrame) ReleaseHeldProbes();
            forcedDistrict = -1;
            capturing.Clear();
            captureIds.Clear();
            seen.Clear();
            budgetClock = 0f;
        }
        if (captureFrames < 0 || --captureFrames > 0) return false;
        captureFrames = -1;
        var d = captureFor;
        captureFor = null;
        var all = captureAll;
        captureAll = false;
        if (d == null || d != here || d.Root == null || !d.Root.gameObject.activeInHierarchy || d.Key != district) { captureOnly.Clear(); return false; }
        if (all)
        {
            foreach (var p in d.Root.GetComponentsInChildren<ReflectionProbe>(false))
                if (Capturable(p)) capturing.Add(p);
        }
        else
            foreach (var p in captureOnly)
                if (Capturable(p) && p.transform.IsChildOf(d.Root) && !capturing.Contains(p)) capturing.Add(p);
        captureOnly.Clear();
        if (capturing.Count == 0) return false;
        foreach (var p in capturing) if (!known.Contains(p)) known.Add(p);
        EnsureProxies(d);
        foreach (var l in lamps) if (l.district == d.Key) Drive(l);
        forcedDistrict = d.Key;
        forcedFrames = CaptureFramesMax;
        seen.Clear();
        budgetClock = 0f;
        return true;
    }

    static bool Capturable(ReflectionProbe p) =>
        p != null && p.isActiveAndEnabled && p.mode == ReflectionProbeMode.Realtime && p.refreshMode == ReflectionProbeRefreshMode.ViaScripting;

    void RenderCapture()
    {
        captureIds.Clear();
        captureFrame = Time.frameCount;
        foreach (var p in capturing)
        {
            var id = -1;
            try { id = p.RenderProbe(); renderedProbes.Add(p); }
            catch (System.Exception e) { FrontRoomsPoolMaterials.LogOnce("probe render", "a reflection probe did not render: " + e.Message); }
            captureIds.Add(id);
        }
    }

    // A capture is done when every probe was seen rendering while the lamps were on (OnBeginCamera), or, in case its cameras
    // were not recognised, when every one says it has finished and two frames have passed since the ask to render.
    bool SeenAll()
    {
        foreach (var p in capturing) if (p != null && !seen.Contains(p)) return false;
        return true;
    }

    bool FinishedAll()
    {
        for (var i = 0; i < capturing.Count; i++)
        {
            var p = capturing[i];
            if (p == null) continue;
            var id = i < captureIds.Count ? captureIds[i] : -1;
            if (id < 0 || !p.IsFinishedRendering(id)) return false;
        }
        return true;
    }

    // A probe of the current district rendered: while its lamps are forced it is the capture's (or as good as); otherwise
    // someone rendered it under the budget (the water preview's own re-render): capture it again, with the lamps on.
    void ProbeRendered(ReflectionProbe p)
    {
        if (here == null || here.Root == null || p == null) return;
        if (forcedDistrict >= 0 && forcedDistrict == here.Key)
        {
            if (capturing.Contains(p)) seen.Add(p);
            return;
        }
        if (Capturable(p) && p.transform.IsChildOf(here.Root)) RequestCapture(here, p);
    }

    // ---------------------------------------------------------------- the proxies

    // The district's pool-family lamps get their proxies, once per lamp (a lamp made later gets its own at the next capture).
    void EnsureProxies(FrontRoomsStructuresDistrict d)
    {
        int made = 0, boxes = 0, bare = 0;
        foreach (var l in lamps)
        {
            if (l.district != d.Key || l.proxyChecked || l.light == null) continue;
            l.proxyChecked = true;
            var lamp = l.light.transform.parent;
            if (lamp == null || !PoolLamp(d, l.light.transform.position)) continue;
            if (lamp.name.IndexOf("wall box", System.StringComparison.Ordinal) >= 0) { boxes++; continue; }
            var n = MakeProxies(l, lamp);
            if (n == 0) bare++;
            made += n;
        }
        if (made + boxes + bare > 0)
            Debug.Log(FrontRoomsStructures.Log + d.Code + " reflection proxies (GZ-A): " + made + " on layer " + ProxyLayer + " at the lamp's tint x " + ProxyGain
                + " (seen only by its probes), " + boxes + " wall boxes left as they are, " + bare + " lamps without a lens.");
    }

    static bool PoolLamp(FrontRoomsStructuresDistrict d, Vector3 world)
    {
        try { return IsPoolPreset(d.PresetAt(world)); }
        catch (System.Exception) { return false; }
    }

    int MakeProxies(Lamp l, Transform lamp)
    {
        if (!ProxyReady()) return 0;
        // the lamp's beam in the lamp's own space: the kit's lens faces its lamp's +z, the troffer kit's faces down
        var beam = lamp.InverseTransformDirection(l.light.transform.forward);
        beam = beam.sqrMagnitude > 1e-8f ? beam.normalized : Vector3.forward;
        lensBuffer.Clear();
        foreach (Transform c in lamp)
            if (c.name.StartsWith("Lens", System.StringComparison.Ordinal)) lensBuffer.Add(c);
        var made = 0;
        foreach (var c in lensBuffer)
        {
            var mf = c.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null || c.GetComponent<MeshRenderer>() == null) continue;
            AddProxy(l, lamp, mf.sharedMesh, c.localPosition + beam * ProxyOffset, c.localRotation, c.localScale);
            made++;
        }
        lensBuffer.Clear();
        // the oculus (SP6's dome) has no lens of its own: a disc of its shaft at the light, facing down its beam
        if (made == 0 && lamp.name.IndexOf("oculus", System.StringComparison.Ordinal) >= 0)
        {
            var t = l.light.transform;
            AddProxy(l, lamp, Disc(), t.localPosition + beam * ProxyOffset, t.localRotation, new Vector3(OculusDisc, OculusDisc, 1f));
            made++;
        }
        return made;
    }

    bool ProxyReady()
    {
        if (proxyMaterial != null) return true;
        if (proxyShaderMissing) return false;
        var sh = Shader.Find("Universal Render Pipeline/Unlit");
        if (sh == null)
        {
            proxyShaderMissing = true;
            FrontRoomsPoolMaterials.LogOnce("proxy shader", "URP Unlit is missing: no reflection proxies, so the pool glaze shows no lamps (GZ-A).");
            return false;
        }
        proxyMaterial = new Material(sh) { name = "Structures / reflection proxy (layer 27, probes only)", hideFlags = HideFlags.DontSave };
        if (proxyMaterial.HasProperty("_Cull")) proxyMaterial.SetFloat("_Cull", 0f);
        proxyMaterial.SetColor(BaseColorId, Color.white);
        return true;
    }

    void AddProxy(Lamp l, Transform lamp, Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        var go = new GameObject(ProxyName);
        go.layer = ProxyLayer;
        go.transform.SetParent(lamp, false);
        go.transform.localPosition = position;
        go.transform.localRotation = rotation;
        go.transform.localScale = scale;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = proxyMaterial;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        r.lightProbeUsage = LightProbeUsage.Off;
        r.reflectionProbeUsage = ReflectionProbeUsage.Off;
        if (l.proxies == null) l.proxies = new List<Renderer>();
        l.proxies.Add(r);
        Drive(l);
    }

    Mesh Disc()
    {
        if (proxyDisc != null) return proxyDisc;
        const int n = 32;
        var v = new Vector3[n + 1];
        var t = new int[n * 3];
        for (var i = 0; i < n; i++)
        {
            var a = i * Mathf.PI * 2f / n;
            v[i + 1] = new Vector3(Mathf.Cos(a) * .5f, Mathf.Sin(a) * .5f, 0f);
            t[i * 3] = 0;
            t[i * 3 + 1] = i + 1;
            t[i * 3 + 2] = (i + 1) % n + 1;
        }
        proxyDisc = new Mesh { name = "Structures / reflection proxy disc", hideFlags = HideFlags.DontSave };
        proxyDisc.vertices = v;
        proxyDisc.triangles = t;
        proxyDisc.RecalculateNormals();
        proxyDisc.RecalculateBounds();
        return proxyDisc;
    }

    // A lamp's proxies follow its state, never the budget: hidden while it is out (scale 0), else its radiance x its scale.
    // SetVector, not SetColor: the radiance is already what the GPU should get (ProxyRadiance), HDR and linear.
    void Drive(Lamp l)
    {
        if (l.proxies == null) return;
        var on = l.light != null && l.scale > .001f;
        var c = on ? ProxyRadiance(l.light.color) * l.scale : Color.black;
        if (proxyBlock == null) proxyBlock = new MaterialPropertyBlock();
        foreach (var r in l.proxies)
        {
            if (r == null) continue;
            if (r.gameObject.activeSelf != on) r.gameObject.SetActive(on);
            if (!on) continue;
            r.GetPropertyBlock(proxyBlock);
            proxyBlock.SetVector(BaseColorId, new Vector4(c.r, c.g, c.b, 1f));
            r.SetPropertyBlock(proxyBlock);
        }
    }

    // ---------------------------------------------------------------- which cameras see layer 27

    void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += OnBeginCamera;
        Application.onBeforeRender += OnBeforeRender;
    }

    void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
        Application.onBeforeRender -= OnBeforeRender;
        // The lamp share is the zone look's static state: hand the game back its own 1 when scene 4 stops (Play mode without
        // a domain reload would otherwise carry 3 into the game's pool level).
        if (lampShare != 1f)
        {
            lampShare = 1f;
            FrontRoomsZoneLook.SetLampShare("PoolLevel", 1f);
        }
    }

    // Before anything renders this frame (the water's mirrors render from this callback too, after it: they subscribe when
    // their first water appears): every enabled camera, and the camera the mirrors copy, skip the proxies.
    void OnBeforeRender()
    {
        var n = Camera.allCamerasCount;
        if (cameraBuffer.Length < n) cameraBuffer = new Camera[Mathf.NextPowerOfTwo(n)];
        n = Camera.GetAllCameras(cameraBuffer);
        for (var i = 0; i < n; i++) CullProxies(cameraBuffer[i]);
        CullProxies(FrontRoomsWaterPlanar.Viewer);
    }

    // Every camera as it begins rendering, before its culling: a reflection probe's capture keeps layer 27 (and tells a running
    // capture its probe rendered); every other camera loses it, the water mirrors (Reflection cameras too) included.
    void OnBeginCamera(ScriptableRenderContext context, Camera camera)
    {
        if (camera == null) return;
        if (camera.cameraType == CameraType.Reflection)
        {
            var probe = ProbeOf(camera);
            if (probe != null || CubeFace(camera))
            {
                if (!loggedProbeCamera)
                {
                    loggedProbeCamera = true;
                    Debug.Log(FrontRoomsStructures.Log + "reflection proxies (GZ-A): probe captures render layer " + ProxyLayer + " (first one: camera '" + camera.name
                        + "', " + (probe != null ? "at the probe " + probe.name : "a 90-degree square view") + "); every other camera skips it.");
                }
                if (probe != null) ProbeRendered(probe);
                return;
            }
        }
        if ((camera.cullingMask & ProxyMask) == 0) return;
        camera.cullingMask &= ~ProxyMask;
        if (loggedSkip.Add(camera.cameraType))
            Debug.Log(FrontRoomsStructures.Log + "reflection proxies (GZ-A): layer " + ProxyLayer + " taken out of camera '" + camera.name + "' (" + camera.cameraType + ").");
    }

    // The probe this camera captures: its eye (as it renders: the view matrix, not only the transform) on one of this look's.
    ReflectionProbe ProbeOf(Camera camera)
    {
        Vector3 eye = camera.cameraToWorldMatrix.GetColumn(3);
        for (var i = known.Count - 1; i >= 0; i--)
        {
            var p = known[i];
            if (p == null) { known.RemoveAt(i); continue; }
            if ((p.transform.position - eye).sqrMagnitude < 1e-4f) return p;
        }
        return null;
    }

    // A cube face: 90 degrees, square (the water mirrors keep their viewer's field of view and aspect).
    static bool CubeFace(Camera camera)
    {
        var m = camera.projectionMatrix;
        return Mathf.Abs(Mathf.Abs(m.m00) - 1f) < .01f && Mathf.Abs(Mathf.Abs(m.m11) - 1f) < .01f;
    }

    void OnDestroy()
    {
        if (poolProfile != null) Destroy(poolProfile);
        if (darkProfile != null) Destroy(darkProfile);
        if (proxyMaterial != null) Destroy(proxyMaterial);
        if (proxyDisc != null) Destroy(proxyDisc);
    }
}
