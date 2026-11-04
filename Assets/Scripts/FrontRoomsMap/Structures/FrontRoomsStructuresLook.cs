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
                FrontRoomsLook.SetZoneReflection(FrontRoomsLook.ReflectionZone.Level0, 0f);
                break;
            case Preset.PoolNight:
                // SP1's night key (70 section 3): the three trilight colours halved, the same grade.
                Trilight(new Color(.22f, .245f, .255f), new Color(.25f, .28f, .29f), new Color(.33f, .36f, .36f), new Color(.06f, .08f, .085f), .010f);
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
        public bool mayShadow, view, mustShadow;
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
            if (l.mayShadow || l.mustShadow) shadowed++;
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
            var shadow = on && (l.mustShadow || (l.mayShadow && d < 9f)) ? LightShadows.Soft : LightShadows.None;
            if (l.light.shadows != shadow) l.light.shadows = shadow;
        }
    }

    void OnDestroy()
    {
        if (poolProfile != null) Destroy(poolProfile);
        if (darkProfile != null) Destroy(darkProfile);
    }
}
