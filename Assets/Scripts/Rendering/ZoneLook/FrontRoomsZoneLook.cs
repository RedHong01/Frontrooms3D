using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Per-zone ambient, haze and height fog (research/bathroom_rooms/70_look_build.md, "Zone look"; 16 section 6 item 1,
/// 17 section 7 item 10). FrontRoomsLook.ApplyAmbient sets one warm trilight and one warm fog for the whole game; this
/// gives a zone its own, blended at the zone's border by the camera's position exactly as URP blends the zone's grade
/// volume (FrontRoomsPost_&lt;zone&gt;, made by FrontRoomsPostStack.EnsureZoneVolume beside each look volume): URP's weight
/// 1 - (d / blend)^2 from the box, volumes in ascending priority, each lerping what is below it.
///
/// Inert unless a FrontRoomsZoneLookVolume is enabled: with none, nothing is hooked and nothing is written. With one, the
/// look is applied for each render context (RenderPipelineManager.beginContextRendering, before URP reads RenderSettings
/// and culls) at that context's camera, and the values found there are written back unchanged when the context ends: so
/// outside rendering RenderSettings always hold the game's own values (a scene saved in the editor never keeps a zone's
/// look), and a camera outside every zone renders Level 0's values to the bit. What is written while a zone weighs in:
///  - RenderSettings.ambientProbe, in ambient mode Custom (the only mode that keeps a probe set from code): the captured
///    probe lerped toward each zone's trilight probe (TrilightProbe: Unity's own projection of a trilight, measured, so the
///    blend starts exactly at the game's probe). The trilight colours are not written (URP lights with the probe).
///  - RenderSettings.fogColor and fogDensity (exp2; a scene without fog takes the zone's colour at density 0 at its edge).
///  - The globals _FR_HeightFog (density per metre at the water, water height, 1 / scale height, 0) and
///    _FR_HeightFogColor (linear): FrontRooms/Surface's height fog, off (density 0) everywhere else.
///  - The intensity of the scene's directional lights (the game's only one is the fill "Soft ambient direction", the
///    ambient's direction) times the zone's fillScale, when that differs from 1 (the dark zone keeps a fifth).
/// Reflection cubes stay FrontRoomsZoneReflection's (Refl_Pool is the water stage's).
///
/// Tiers: the same values on desktop, phones and WebGL; no pass is added (the grade is the film look's own LUT pass, the
/// height fog a few instructions in Surface's existing pass behind a uniform branch).
///
/// The map: Attach(map) and Bind(map, viewer) are the agreed tool entries (bathroom-005) for the game and the Level
/// Designer. The map has no pool zones yet (contract C1, research/bathroom_rooms/50_modules_and_contracts.md): Attach finds
/// none and does nothing. The contract it expects is in W/apply/pool_zone_look_contract/README.md: ZoneTheme gains the pool
/// values, and the chunk build places one look volume per rectangle of pool cells (AddMapZone, like the Office grade's
/// AddZoneGrades). ASCII only.
/// </summary>
public static class FrontRoomsZoneLook
{
    public const string ProfileFolder = "Rendering/ZoneLook/";
    public const string ProfilePrefix = "FrontRoomsZoneLook_";
    public const string GradePrefix = "FrontRoomsPost_";
    /// <summary>The pool family's look zones (16 section 3): the club, the hall, the level past the far door, the dark zone.</summary>
    public static readonly string[] PoolZones = { "PoolClub", "PoolHall", "PoolLevel", "PoolDark" };

    public static readonly int HeightFogId = Shader.PropertyToID("_FR_HeightFog");
    public static readonly int HeightFogColorId = Shader.PropertyToID("_FR_HeightFogColor");
    /// <summary>The blended lamp share at the render (1 = as tuned): FrontRooms/Surface's water bounce replaces only the tuned ambient.</summary>
    public static readonly int AmbientShareId = Shader.PropertyToID("_FR_ZoneAmbientShare");

    // ------------------------------------------------------------------------------------------- registry
    static readonly List<FrontRoomsZoneLookVolume> volumes = new List<FrontRoomsZoneLookVolume>();
    static readonly List<FrontRoomsZoneLookVolume> sorted = new List<FrontRoomsZoneLookVolume>();
    static readonly Dictionary<string, FrontRoomsZoneLookProfile> profiles = new Dictionary<string, FrontRoomsZoneLookProfile>();
    static bool hooked, dirty = true;

    /// <summary>True while at least one look volume is enabled (the hooks are on).</summary>
    public static bool Active => volumes.Count > 0;
    public static int VolumeCount => volumes.Count;
    public static IReadOnlyList<FrontRoomsZoneLookVolume> Volumes => volumes;

    /// <summary>What the last applied context found: the strongest zone and its weight (logs, tests); "" and 0 outside.</summary>
    public static string LastZone { get; private set; } = "";
    public static float LastWeight { get; private set; }
    /// <summary>Contexts that had a zone weighing in / that were written back (logs, tests).</summary>
    public static int AppliedContexts { get; private set; }
    public static int RestoredContexts { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Unhook();
        volumes.Clear();
        sorted.Clear();
        profiles.Clear();
        depth = 0;
        overflow = 0;
        map = null;
        viewer = null;
        LastZone = "";
        LastWeight = 0f;
        lampShares.Clear();
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    static void OnSceneUnloaded(Scene scene)
    {
        volumes.RemoveAll(v => v == null);
        dirty = true;
        if (volumes.Count == 0) Unhook();
    }

    internal static void Register(FrontRoomsZoneLookVolume v)
    {
        if (v == null || volumes.Contains(v)) return;
        volumes.Add(v);
        dirty = true;
        fillsFoundAt = -10f;
        Hook();
    }

    internal static void Unregister(FrontRoomsZoneLookVolume v)
    {
        if (!volumes.Remove(v)) return;
        dirty = true;
        if (volumes.Count == 0) Unhook();
    }

    internal static void MarkDirty() { dirty = true; }

    static void Hook()
    {
        if (hooked) return;
        RenderPipelineManager.beginContextRendering += OnBeginContext;
        RenderPipelineManager.endContextRendering += OnEndContext;
        hooked = true;
    }

    static void Unhook()
    {
        if (!hooked) return;
        RenderPipelineManager.beginContextRendering -= OnBeginContext;
        RenderPipelineManager.endContextRendering -= OnEndContext;
        hooked = false;
        // a volume went away mid-render: give back what every open context found
        while (depth > 0) { depth--; if (stack[depth].applied) Restore(stack[depth]); }
    }

    // ------------------------------------------------------------------------------------------- profiles
    /// <summary>The zone's look profile: Resources/Rendering/ZoneLook/FrontRoomsZoneLook_&lt;zone&gt;, else the built-in
    /// values (FrontRoomsZoneLookProfile.Defaults), else null (an unknown zone weighs nothing).</summary>
    public static FrontRoomsZoneLookProfile Profile(string zone)
    {
        if (string.IsNullOrEmpty(zone)) return null;
        FrontRoomsZoneLookProfile p;
        if (profiles.TryGetValue(zone, out p) && p != null) return p;
        p = Resources.Load<FrontRoomsZoneLookProfile>(ProfileFolder + ProfilePrefix + zone);
        if (p == null) p = FrontRoomsZoneLookProfile.Defaults(zone);
        profiles[zone] = p;
        return p;
    }

    /// <summary>Drop cached profiles (an asset was edited or rebuilt).</summary>
    public static void ReloadProfiles()
    {
        foreach (var kv in profiles) if (kv.Value != null) kv.Value.Invalidate();
        profiles.Clear();
    }

    // ------------------------------------------------------------------------------------------- the ambient probe of a trilight
    // Unity's projection of the Lighting window's trilight (sky above, equator at the horizon, ground below) onto the
    // ambient probe is linear in the three LINEAR colours: probe = sky * B0 + equator * B1 + ground * B2, the same 9
    // coefficients for each channel (Basis[k][i]: colour k 0 sky, 1 equator, 2 ground; coefficient i). Measured from
    // Unity 6000.6.3f1's own rebuild (W/proj_unlock harness FrontRoomsPoolLook -plCalibrate, run zl_v1, 2026-11-03: each
    // colour alone at 1.0); it predicts Unity's probe of Level 0's trilight to 7.5e-9 and of PoolHall's to 3.7e-9, and every
    // run of the harness checks it again (zl_basis.txt).
    static readonly float[][] MeasuredBasis =
    {
        new float[] { 0.166666657f, 0.272165477f, 0f, 0f, 0f, 0f, -0.02604167f, 0f, -0.07812499f },
        new float[] { 0.6666666f, 0f, 0f, 0f, 0f, 0f, 0.05208331f, 0f, 0.156249985f },
        new float[] { 0.166666657f, -0.272165477f, 0f, 0f, 0f, 0f, -0.02604167f, 0f, -0.07812499f },
    };
    static float[][] basis;

    /// <summary>The basis in use (MeasuredBasis unless a calibration replaced it).</summary>
    public static float[][] Basis
    {
        get
        {
            if (basis == null) basis = MeasuredBasis;
            return basis;
        }
    }

    /// <summary>Use a measured basis (the harness's calibration; 3 x 9).</summary>
    public static void SetBasis(float[][] b)
    {
        if (b == null || b.Length != 3) return;
        basis = b;
        foreach (var kv in profiles) if (kv.Value != null) kv.Value.Invalidate();
    }

    /// <summary>The probe Unity builds from this trilight (gamma colours as the Lighting window takes them).</summary>
    public static SphericalHarmonicsL2 TrilightProbe(Color skyGamma, Color equatorGamma, Color groundGamma)
    {
        var b = Basis;
        var cols = new[] { skyGamma.linear, equatorGamma.linear, groundGamma.linear };
        var sh = new SphericalHarmonicsL2();
        for (var k = 0; k < 3; k++)
            for (var i = 0; i < 9; i++)
            {
                sh[0, i] += cols[k].r * b[k][i];
                sh[1, i] += cols[k].g * b[k][i];
                sh[2, i] += cols[k].b * b[k][i];
            }
        return sh;
    }

    // ------------------------------------------------------------------------------------------- the map (tool entries)
    static Object map;
    static Transform viewer;

    /// <summary>The map the look is attached to (null when none).</summary>
    public static Object AttachedMap => map;
    /// <summary>The bound viewer (the player's or the preview's camera): ZoneAtViewer reads it.</summary>
    public static Transform Viewer => viewer;

    /// <summary>
    /// Tool entry (bathroom-005): attach the zone look to a map, for the game's run and the Level Designer's preview. The map
    /// exposes no pool zone yet (C1), so this finds none and does nothing; it returns the pool look volumes already under
    /// the map (0 today). Under the contract the chunk build calls AddMapZone for each rectangle of pool cells (they live
    /// and die with their chunk); this entry stays the one place a tool calls.
    /// </summary>
    public static int Attach(FrontRoomsMapWorld world)
    {
        if (world == null) return 0;
        map = world;
        var n = 0;
        foreach (var v in world.GetComponentsInChildren<FrontRoomsZoneLookVolume>(true)) if (v != null) n++;
        if (n > 0) Debug.Log("[ZoneLook] attached to " + world.name + ": " + n + " look volumes");
        return n;
    }

    /// <summary>
    /// Tool entry (bathroom-005): bind the map and its viewer. Rendering never needs the viewer (each render context is
    /// weighed at its own camera, as URP weighs the grade); the viewer serves the queries that run outside rendering
    /// (ZoneAtViewer: lamp rigs, water) in the later stages.
    /// </summary>
    public static void Bind(FrontRoomsMapWorld world, Transform view)
    {
        Attach(world);
        viewer = view;
    }

    /// <summary>
    /// The contract's call (W/apply/pool_zone_look_contract): one look volume over a rectangle of pool cells, under the
    /// chunk's holder, in its local space; waterY is the module's water height (local), NaN for a dry zone.
    /// </summary>
    public static FrontRoomsZoneLookVolume AddMapZone(Transform holder, string zone, Bounds localBounds, float blendDistance, float waterY)
    {
        var v = FrontRoomsZoneLookVolume.Ensure(holder, zone, localBounds, blendDistance, waterY);
        if (v != null && v.priority != DefaultPriority(zone)) { v.priority = DefaultPriority(zone); v.Refresh(); }
        return v;
    }

    /// <summary>
    /// The deeper zone wins where two pool zones meet (club 1.0, hall 1.05, level 1.1, dark 1.15; all above the Office grade's
    /// 1, which never meets them): its look reaches blendDistance into the shallower zone, so the change starts before the
    /// player steps through, the same for the grade (URP sorts by the same priority) and for the light.
    /// </summary>
    public static float DefaultPriority(string zone)
    {
        switch (zone)
        {
            case "PoolClub": return 1f;
            case "PoolHall": return 1.05f;
            case "PoolLevel": return 1.1f;
            case "PoolDark": return 1.15f;
        }
        return 1f;
    }

    /// <summary>The strongest zone at a world position and its weight ("" and 0 outside every zone).</summary>
    public static string ZoneAt(Vector3 position, out float weight)
    {
        weight = 0f;
        var best = "";
        foreach (var v in volumes)
        {
            if (v == null || !v.isActiveAndEnabled) continue;
            var w = v.InfluenceAt(position);
            if (w > weight) { weight = w; best = v.zone; }
        }
        return best;
    }

    /// <summary>The strongest zone at the bound viewer ("" without one).</summary>
    public static string ZoneAtViewer(out float weight)
    {
        weight = 0f;
        return viewer != null ? ZoneAt(viewer.position, out weight) : "";
    }

    // ------------------------------------------------------------------------------------------- lamp presets
    /// <summary>The pool hall's overhead rig (zone_lamps/30_for_red.md 2.2; Red's P1 is open, P1-A the working pick).</summary>
    public enum OverheadRig { MapTroffer, WallBoxP1A, LowBayP1B, VaporTightP1C, Dead }
    /// <summary>The basins' underwater rig (30_for_red.md 2.3; Red's P2 is open, P2-A the working pick).</summary>
    public enum UnderwaterRig { None, NicheP2A, NicheP2B, PortP2C }

    /// <summary>Switchable until Red picks P1 and P2.</summary>
    public static OverheadRig PoolHallOverhead = OverheadRig.WallBoxP1A;
    public static UnderwaterRig PoolUnderwater = UnderwaterRig.NicheP2A;

    /// <summary>A zone's lamps for the lamp kit path (16 section 6 item 2): which rigs, and their colours (gamma).</summary>
    public struct LampPreset
    {
        public string zone;
        public OverheadRig overhead;
        public UnderwaterRig underwater;
        public Color light, lens, nicheLens, nicheLight;
        public float intensity, nicheIntensity;
    }

    /// <summary>
    /// The preset per zone: the club's troffers in cool white; the hall's overheads by PoolHallOverhead in metal halide (the
    /// map's own troffers off inside the rig, C7); the level's lamps in daylight at 1.2x (its valance and flush-lens
    /// fixtures are a later kit; until then the map's lamp recoloured); the dark zone's overheads dead. Basins (hall, dark)
    /// take PoolUnderwater: warm at the lens, the through-water colour as light.
    /// </summary>
    public static LampPreset Lamps(string zone)
    {
        var p = Profile(zone);
        var preset = new LampPreset { zone = zone, overhead = OverheadRig.MapTroffer, underwater = UnderwaterRig.None, light = Color.white, lens = Color.white, nicheLens = Color.white, nicheLight = Color.white, intensity = 1f, nicheIntensity = 0f };
        if (p != null)
        {
            preset.light = p.lampLight;
            preset.lens = p.lampLens;
            preset.intensity = p.lampIntensity;
            preset.nicheLens = p.nicheLens;
            preset.nicheLight = p.nicheLight;
            preset.nicheIntensity = p.nicheIntensity;
        }
        switch (zone)
        {
            case "PoolHall": preset.overhead = PoolHallOverhead; preset.underwater = PoolUnderwater; break;
            case "PoolDark": preset.overhead = OverheadRig.Dead; preset.underwater = PoolUnderwater; break;
        }
        return preset;
    }

    // ------------------------------------------------------------------------------------------- per render context
    struct Saved
    {
        public bool applied, fog, scaledFill;
        public int level, frame;
        public AmbientMode ambientMode;
        public FogMode fogMode;
        public Color fogColor;
        public float fogDensity;
        public SphericalHarmonicsL2 probe;
        public Vector4 heightFog, heightFogColor;
        public float ambientShare;
    }

    static readonly Saved[] stack = new Saved[16];
    static int depth, overflow;

    static void OnBeginContext(ScriptableRenderContext context, List<Camera> cameras)
    {
        // a context of an earlier frame that never ended (an exception inside a render) is unwound first, giving back what
        // it found, so a lost end can never leave a zone's values behind; nested contexts (a render request inside a
        // render) belong to the same frame
        var frame = Time.frameCount;
        while (depth > 0 && stack[depth - 1].frame >= 0 && stack[depth - 1].frame != frame)
        {
            depth--;
            if (stack[depth].applied) { Restore(stack[depth]); RestoredContexts++; }
        }
        if (depth >= stack.Length) { overflow++; return; }
        var saved = Capture();
        saved.applied = false;
        saved.level = depth;
        saved.frame = frame;
        var cam = PickCamera(cameras);
        if (cam != null) saved.applied = ApplyAt(TriggerPosition(cam), ref saved);
        stack[depth++] = saved;
    }

    static void OnEndContext(ScriptableRenderContext context, List<Camera> cameras)
    {
        if (overflow > 0) { overflow--; return; }
        if (depth <= 0) return;
        depth--;
        if (stack[depth].applied) { Restore(stack[depth]); RestoredContexts++; }
    }

    /// <summary>
    /// The camera a context is weighed at: the view the player sees. URP renders a frame's cameras in one context, sorted
    /// by depth, so an offscreen feed (the exit cut's cross camera renders to a texture one depth below the view) comes
    /// first: cameras drawing to the screen win, then Game and Scene view cameras, then the highest depth. Preview
    /// cameras never count. A context of one camera (a reflection probe, a planar camera, a capture) takes that camera.
    /// </summary>
    static Camera PickCamera(List<Camera> cameras)
    {
        if (cameras == null) return null;
        Camera best = null;
        var bestScore = -1;
        for (var i = 0; i < cameras.Count; i++)
        {
            var c = cameras[i];
            if (c == null || c.cameraType == CameraType.Preview) continue;
            var score = (c.targetTexture == null ? 2 : 0) + (c.cameraType == CameraType.Game || c.cameraType == CameraType.SceneView ? 1 : 0);
            if (best == null || score > bestScore || (score == bestScore && c.depth > best.depth)) { best = c; bestScore = score; }
        }
        return best;
    }

    /// <summary>Where URP weighs the camera's volumes: its volume trigger, else the camera itself.</summary>
    public static Vector3 TriggerPosition(Camera cam)
    {
        UniversalAdditionalCameraData data;
        if (cam.TryGetComponent(out data) && data.volumeTrigger != null) return data.volumeTrigger.position;
        return cam.transform.position;
    }

    static Saved Capture()
    {
        return new Saved
        {
            fog = RenderSettings.fog,
            ambientMode = RenderSettings.ambientMode,
            fogMode = RenderSettings.fogMode,
            fogColor = RenderSettings.fogColor,
            fogDensity = RenderSettings.fogDensity,
            probe = RenderSettings.ambientProbe,
            heightFog = Shader.GetGlobalVector(HeightFogId),
            heightFogColor = Shader.GetGlobalVector(HeightFogColorId),
            ambientShare = Shader.GetGlobalFloat(AmbientShareId),
        };
    }

    static void Restore(Saved s)
    {
        if (s.scaledFill) RestoreFill(s.level);
        if (RenderSettings.ambientMode != s.ambientMode) RenderSettings.ambientMode = s.ambientMode;
        RenderSettings.ambientProbe = s.probe;
        RenderSettings.fog = s.fog;
        RenderSettings.fogMode = s.fogMode;
        RenderSettings.fogColor = s.fogColor;
        RenderSettings.fogDensity = s.fogDensity;
        Shader.SetGlobalVector(HeightFogId, s.heightFog);
        Shader.SetGlobalVector(HeightFogColorId, s.heightFogColor);
        Shader.SetGlobalFloat(AmbientShareId, s.ambientShare);
    }

    /// <summary>The blended look at a position, from base values (what the context found). False when no zone weighs in.</summary>
    public struct Blend
    {
        public SphericalHarmonicsL2 probe;
        public Color fogLinear;
        public float fogDensity;
        public float heightDensity, heightWaterY, heightScale;
        public Color heightLinear;
        public float fillScale;
        /// <summary>The lamp share, weighted like the probe (1 = as tuned).</summary>
        public float ambientShare;
        public string strongest;
        public float strongestWeight;
    }

    static void SortVolumes()
    {
        if (!dirty) return;
        volumes.RemoveAll(v => v == null);
        sorted.Clear();
        // ascending priority, registration order within a priority (URP's stable insertion sort does the same)
        for (var i = 0; i < volumes.Count; i++)
        {
            var v = volumes[i];
            var at = sorted.Count;
            while (at > 0 && sorted[at - 1].priority > v.priority) at--;
            sorted.Insert(at, v);
        }
        dirty = false;
    }

    /// <summary>Weigh every look volume at a position against the given base values; false (and blend untouched) when no
    /// zone weighs in there.</summary>
    public static bool Evaluate(Vector3 position, SphericalHarmonicsL2 baseProbe, bool baseFog, FogMode baseMode, Color baseFogGamma, float baseDensity, Vector4 baseHeight, Vector4 baseHeightColor, out Blend blend)
    {
        SortVolumes();
        blend = new Blend();
        var any = false;
        for (var i = 0; i < sorted.Count; i++)
        {
            var v = sorted[i];
            if (v == null || !v.isActiveAndEnabled) continue;
            var w = v.InfluenceAt(position);
            if (w <= 0f) continue;
            var p = v.Profile;
            if (p == null) continue;
            if (!any)
            {
                any = true;
                blend.probe = baseProbe;
                var exp2 = baseFog && baseMode == FogMode.ExponentialSquared;
                // a scene without exp2 fog starts from the zone's own colour at density 0
                blend.fogLinear = exp2 ? baseFogGamma.linear : p.fogColor.linear;
                blend.fogDensity = exp2 ? baseDensity : 0f;
                blend.heightDensity = Mathf.Max(0f, baseHeight.x);
                blend.heightWaterY = baseHeight.y;
                blend.heightScale = baseHeight.z > 0f ? 1f / baseHeight.z : p.heightFogScale;
                blend.heightLinear = new Color(baseHeightColor.x, baseHeightColor.y, baseHeightColor.z, 1f);
                blend.fillScale = 1f;
                blend.ambientShare = 1f;
                blend.strongest = "";
            }
            var sh = p.AmbientProbe;
            // the zone's lamps' share (SetLampShare; 1 = as tuned, the same floats as before)
            var share = LampShareOf(v.zone);
            var lit = share != 1f;
            if (lit) sh = sh * share;
            blend.probe = blend.probe * (1f - w) + sh * w;
            blend.fogLinear = Color.Lerp(blend.fogLinear, lit ? p.fogColor.linear * share : p.fogColor.linear, w);
            blend.fogDensity = Mathf.Lerp(blend.fogDensity, p.fogDensity, w);
            var waterY = v.WaterLevelWorld;
            var hd = p.heightFog && (v.HasWater || LowestWaterIn(v, out waterY)) ? p.heightFogDensity : 0f;
            if (hd > 0f)
            {
                var hl = lit ? p.heightFogColor.linear * share : p.heightFogColor.linear;
                if (blend.heightDensity <= 0f)
                {
                    // the first layer: its own water, scale and colour, faded in by its weight
                    blend.heightWaterY = waterY;
                    blend.heightScale = p.heightFogScale;
                    blend.heightLinear = hl;
                }
                else
                {
                    blend.heightWaterY = Mathf.Lerp(blend.heightWaterY, waterY, w);
                    blend.heightScale = Mathf.Lerp(blend.heightScale, p.heightFogScale, w);
                    blend.heightLinear = Color.Lerp(blend.heightLinear, hl, w);
                }
            }
            blend.heightDensity = Mathf.Lerp(blend.heightDensity, hd, w);
            blend.fillScale = Mathf.Lerp(blend.fillScale, lit ? p.fillScale * share : p.fillScale, w);
            blend.ambientShare = Mathf.Lerp(blend.ambientShare, share, w);
            if (w > blend.strongestWeight) { blend.strongestWeight = w; blend.strongest = v.zone; }
        }
        return any;
    }

    static bool ApplyAt(Vector3 position, ref Saved s)
    {
        Blend b;
        if (!Evaluate(position, s.probe, s.fog, s.fogMode, s.fogColor, s.fogDensity, s.heightFog, s.heightFogColor, out b))
        {
            LastZone = "";
            LastWeight = 0f;
            return false;
        }
        Write(b);
        s.scaledFill = ScaleFill(b.fillScale, s.level);
        LastZone = b.strongest;
        LastWeight = b.strongestWeight;
        AppliedContexts++;
        return true;
    }

    static void Write(Blend b)
    {
        // a trilight (or skybox, or flat) ambient keeps the probe Unity derived from it: the setter only holds in Custom
        // mode (measured: in Trilight mode the probe read back unchanged and the frames did not move)
        if (RenderSettings.ambientMode != AmbientMode.Custom) RenderSettings.ambientMode = AmbientMode.Custom;
        RenderSettings.ambientProbe = b.probe;
        Shader.SetGlobalFloat(AmbientShareId, b.ambientShare);
        RenderSettings.fog = true;
        if (RenderSettings.fogMode != FogMode.ExponentialSquared) RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = b.fogLinear.gamma;
        RenderSettings.fogDensity = b.fogDensity;
        if (b.heightDensity > 1e-6f)
        {
            Shader.SetGlobalVector(HeightFogId, new Vector4(b.heightDensity, b.heightWaterY, 1f / Mathf.Max(.05f, b.heightScale), 0f));
            Shader.SetGlobalVector(HeightFogColorId, new Vector4(b.heightLinear.r, b.heightLinear.g, b.heightLinear.b, 1f));
        }
        else
        {
            Shader.SetGlobalVector(HeightFogId, Vector4.zero);
        }
    }

    // ------------------------------------------------------------------------------------------- the zone's lamps and waters
    // 70_look_build.md section 12.10 (scene 4 with its pool zone looks on, 2026-11-04): the zone's ambient, its haze colours
    // and its share of the scene's fill are the bounce of its lamps, so they follow the lamps; a look volume over terraced
    // water (AddPoolLooks gives no single height) lays its height fog on the lowest water in its box.
    static readonly Dictionary<string, float> lampShares = new Dictionary<string, float>();

    /// <summary>The share of a zone's lamp light that is on (linear), by which its ambient probe, its haze colours (exp2 and
    /// height fog) and its fill are scaled: 1 as the profile is tuned (the default: nothing changes), 0.5 a night key that
    /// halves the lamps, 0 dead; above 1 a scene without the zone's fixtures (scene 4 until P1-A / P2-A are built). zone
    /// "" or null sets every zone without its own value. Whoever dims the zone's lamps sets it with them.</summary>
    public static void SetLampShare(string zone, float share)
    {
        lampShares[zone ?? ""] = Mathf.Max(share, 0f);
    }

    /// <summary>A zone's lamp share (its own, else the one set for every zone, else 1).</summary>
    public static float LampShareOf(string zone)
    {
        float s;
        if (zone != null && lampShares.TryGetValue(zone, out s)) return s;
        return lampShares.TryGetValue("", out s) ? s : 1f;
    }

    /// <summary>The lowest registered water (FrontRoomsWaterSubmerged) whose rectangle's centre lies in the volume's box.</summary>
    static bool LowestWaterIn(FrontRoomsZoneLookVolume v, out float y)
    {
        y = 0f;
        var found = false;
        var n = FrontRoomsWaterSubmerged.BodyCount;
        if (n == 0) return false;
        var t = v.transform;
        var half = v.size * .5f;
        for (var i = 0; i < n; i++)
        {
            Rect r;
            float wy;
            if (!FrontRoomsWaterSubmerged.GetBody(i, out r, out wy)) continue;
            var d = t.InverseTransformPoint(new Vector3(r.center.x, wy, r.center.y)) - v.center;
            if (Mathf.Abs(d.x) > half.x || Mathf.Abs(d.y) > half.y || Mathf.Abs(d.z) > half.z) continue;
            if (!found || wy < y) { y = wy; found = true; }
        }
        return found;
    }

    // the scene's directional lights (found when a volume registers, and again at most once a second while a zone applies)
    static readonly List<Light> fills = new List<Light>();
    // what each open context found (one row per stack level, so nested contexts give back their own)
    static readonly float[][] fillSaved = new float[16][];
    static readonly int[] fillSavedCount = new int[16];
    static float fillsFoundAt = -10f;

    static void FindFills()
    {
        fills.Clear();
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (l != null && l.type == LightType.Directional) fills.Add(l);
        fillsFoundAt = Time.realtimeSinceStartup;
    }

    static bool ScaleFill(float scale, int level)
    {
        if (Mathf.Abs(scale - 1f) < 1e-4f || level < 0 || level >= fillSaved.Length) return false;
        // look again for the scene's fill at most once a second (never inside a nested context: its row must match)
        if (level == 0 && (Time.realtimeSinceStartup - fillsFoundAt > 1f || Time.realtimeSinceStartup < fillsFoundAt)) FindFills();
        if (fillSaved[level] == null || fillSaved[level].Length < fills.Count) fillSaved[level] = new float[Mathf.Max(4, fills.Count)];
        for (var i = 0; i < fills.Count; i++)
        {
            var l = fills[i];
            fillSaved[level][i] = l != null ? l.intensity : 0f;
            if (l != null) l.intensity = l.intensity * scale;
        }
        fillSavedCount[level] = fills.Count;
        return fills.Count > 0;
    }

    static void RestoreFill(int level)
    {
        if (level < 0 || level >= fillSaved.Length || fillSaved[level] == null) return;
        for (var i = 0; i < fills.Count && i < fillSavedCount[level]; i++)
            if (fills[i] != null) fills[i].intensity = fillSaved[level][i];
    }

    /// <summary>
    /// Apply the look for a position now, outside rendering (edit-mode captures that render with Camera.Render() go through
    /// the context hooks by themselves; this is for tools that read RenderSettings). Returns a token for RestoreNow.
    /// </summary>
    public static bool ApplyNow(Vector3 position)
    {
        if (depth >= stack.Length) return false;
        var saved = Capture();
        saved.level = depth;
        saved.frame = -1;   // a tool's level: held until RestoreNow, never unwound as stale
        saved.applied = ApplyAt(position, ref saved);
        stack[depth++] = saved;
        return saved.applied;
    }

    /// <summary>Undo the last ApplyNow.</summary>
    public static void RestoreNow()
    {
        if (depth <= 0) return;
        depth--;
        if (stack[depth].applied) Restore(stack[depth]);
    }
}
