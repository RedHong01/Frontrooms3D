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
/// probes are rendered once after a district's build. It also keeps the light budget of the scene's own lamps (lamps on
/// within 16 m of the walker with a 3 m fade, one in three shadowed within 9 m; view lamps stay on), the map's lamps in P
/// keep the map's own. No shared asset is ever changed: the URP asset, the film look profile and every game material stay
/// as they are. Numbers are starting values, tuned by eye on the walk.
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
            poolProfile = Grade(-12f, -2f, .45f, -4f, 4f);
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
                Trilight(new Color(.30f, .36f, .38f), new Color(.32f, .38f, .40f), new Color(.36f, .44f, .44f), new Color(.11f, .16f, .17f), .010f);
                Grade(poolProfile, 1f);
                // The halls' own probes (rendered once) carry the tile's reflections; elsewhere the game's default cube.
                FrontRoomsLook.SetZoneReflection(FrontRoomsLook.ReflectionZone.Level0, 0f);
                break;
            case Preset.PoolNight:
                // SP1's night key (70 section 3): the three trilight colours halved, the same grade.
                Trilight(new Color(.15f, .18f, .19f), new Color(.16f, .19f, .20f), new Color(.18f, .22f, .22f), new Color(.06f, .08f, .085f), .010f);
                Grade(poolProfile, 1f);
                FrontRoomsLook.SetZoneReflection(FrontRoomsLook.ReflectionZone.Level0, 0f);
                break;
            case Preset.PoolDark:
                Trilight(new Color(.02f, .045f, .055f), new Color(.025f, .055f, .065f), new Color(.03f, .07f, .075f), new Color(.02f, .045f, .05f), .012f);
                Grade(darkProfile, 1f);
                FrontRoomsLook.SetZoneReflection(FrontRoomsLook.ReflectionZone.DeadLamp, 0f);
                break;
            case Preset.RunB:
                // Run! B's armed bounce (the rig's SH: floor .06, walls .25, ceiling .40, tint (.93, .97, 1)), for SP2's F1,
                // SP5 and SP7. The rig's numbers are linear SH values; RenderSettings colours are gamma-space
                // (FrontRoomsLook), so they go in through .gamma (2026-11-04: the first build passed them raw, about 5 x dark).
                var tint = new Color(.93f, .97f, 1f);
                Trilight((tint * .06f).gamma, (tint * .25f).gamma, (tint * .40f).gamma, FrontRoomsLook.FogColor, FrontRoomsLook.FogDensity);
                Grade(null, 0f);
                FrontRoomsLook.SetZoneReflection(FrontRoomsLook.ReflectionZone.Level0, 0f);
                break;
            case Preset.RunBTripped:
                // Run! B's tripped bounce (floor .025, walls .050, ceiling .070, white; linear, so through .gamma).
                Trilight((Color.white * .025f).gamma, (Color.white * .05f).gamma, (Color.white * .07f).gamma, FrontRoomsLook.FogColor, FrontRoomsLook.FogDensity);
                Grade(null, 0f);
                FrontRoomsLook.SetZoneReflection(FrontRoomsLook.ReflectionZone.DeadLamp, 0f);
                break;
            case Preset.RunBRed:
                // SP7's red looks (71 section 6.3): the tripped ambient tinted linear (1.00, 0.30, 0.24) at floor .05, walls
                // .06, ceiling .08 (a wall lit by it alone reads maroon-black, never a black hole); no grade change.
                var red = new Color(1f, .30f, .24f);
                Trilight((red * .05f).gamma, (red * .06f).gamma, (red * .08f).gamma, FrontRoomsLook.FogColor, FrontRoomsLook.FogDensity);
                Grade(null, 0f);
                FrontRoomsLook.SetZoneReflection(FrontRoomsLook.ReflectionZone.DeadLamp, 0f);
                break;
            case Preset.Level0Tall:
                // SP3's V4 room (70 section 1: Level 0 with the Tall reflection in V4).
                FrontRoomsLook.ApplyAmbient();
                Grade(null, 0f);
                FrontRoomsLook.SetZoneReflection(FrontRoomsLook.ReflectionZone.Tall, 0f);
                break;
            case Preset.Office:
                FrontRoomsLook.ApplyAmbient();
                Grade(null, 0f);
                FrontRoomsLook.SetZoneReflection(FrontRoomsLook.ReflectionZone.Office, 0f);
                break;
            default:
                FrontRoomsLook.ApplyAmbient();
                Grade(null, 0f);
                FrontRoomsLook.SetZoneReflection(FrontRoomsLook.ReflectionZone.Level0, 0f);
                break;
        }
        environmentDirty = 0f;
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
        poolVolume.weight = weight;
    }

    /// <summary>A realtime box probe over a hall (world bounds), rendered once a frame or two after it is made (after the build).</summary>
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
            return p;
        }
        catch (System.Exception e)
        {
            FrontRoomsPoolMaterials.LogOnce("probe", "a reflection probe failed: " + e.Message);
            return null;
        }
    }

    // ---------------------------------------------------------------- the scene's own lamps

    sealed class Lamp
    {
        public Light light;
        public Renderer lens;
        public Color lensEmission;
        public float intensity;
        public bool mayShadow, view;
        public int district;
        public float scale = 1f;
    }

    readonly List<Lamp> lamps = new List<Lamp>();
    float budgetClock;
    MaterialPropertyBlock lensBlock;
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    /// <summary>A lamp of the scene's own (district = its key): on within 16 m of the walker (3 m fade), every third may shadow within 9 m; a view lamp stays on while its district is current.</summary>
    public void AddLamp(int district, Light light, Renderer lens, Color lensEmission, bool view = false)
    {
        if (light == null) return;
        lamps.Add(new Lamp { light = light, lens = lens, lensEmission = lensEmission, intensity = light.intensity, mayShadow = lamps.Count % 3 == 0, view = view, district = district });
        if (lens != null)
        {
            if (lensBlock == null) lensBlock = new MaterialPropertyBlock();
            lens.GetPropertyBlock(lensBlock);
            lensBlock.SetColor(EmissionColorId, lensEmission);
            lens.SetPropertyBlock(lensBlock);
        }
    }

    /// <summary>Scale one lamp (0 keeps it off; SP1's night lines and dimmed hall lamps). Unknown lights are ignored.</summary>
    public void Scale(Light light, float scale)
    {
        if (light == null) return;
        foreach (var l in lamps)
            if (l.light == light) { l.scale = Mathf.Max(0f, scale); break; }
        budgetClock = 0f;
    }

    /// <summary>Lamps of one district, for its cost line.</summary>
    public void CountLamps(int district, out int lights, out int shadowed)
    {
        lights = shadowed = 0;
        foreach (var l in lamps)
        {
            if (l.district != district || l.light == null) continue;
            lights++;
            if (l.mayShadow) shadowed++;
        }
    }

    /// <summary>The budget, ten times a second: called by the bootstrap with the walker's eye and the current district.</summary>
    public void Tick(Vector3 eye, int district, float dt)
    {
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
                try { p.RenderProbe(); }
                catch (System.Exception e) { FrontRoomsPoolMaterials.LogOnce("probe render", "a reflection probe did not render: " + e.Message); }
            }
            pendingProbes.Clear();
        }
        FrontRoomsPoolMaterials.Animate(Time.time);
        budgetClock -= dt;
        if (budgetClock > 0f) return;
        budgetClock = .1f;
        foreach (var l in lamps)
        {
            if (l.light == null) continue;
            var mine = l.district == district;
            var d = Vector3.Distance(eye, l.light.transform.position);
            var level = (!mine && l.district != 0 ? 0f : l.view && mine ? 1f : Mathf.Clamp01((19f - d) / 3f)) * l.scale;
            var on = level > .001f;
            if (l.light.enabled != on) l.light.enabled = on;
            if (on) l.light.intensity = l.intensity * level;
            var shadow = on && l.mayShadow && d < 9f ? LightShadows.Soft : LightShadows.None;
            if (l.light.shadows != shadow) l.light.shadows = shadow;
        }
    }

    void OnDestroy()
    {
        if (poolProfile != null) Destroy(poolProfile);
        if (darkProfile != null) Destroy(darkProfile);
    }
}
