using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// LW3's lens hook, step 1 (line visual (game visual), 2026-11-10; research/light_design/10_research.md: LW3, "Checks per step"
/// row "LW3 lens A/B", decision item 1; Red 2026-11-06 in the light line's chat, "all as recommended" (verbatim there): LW3 =
/// (c) the lens tied to its light plus a fixed exposure per zone, the bloom threshold between the brightest wall and the lens,
/// and an A/B shown to Red before anything lands). INERT: nothing here changes a frame until a caller sets it.
///
/// Mode.Today (the default) is today's game: every lamp writes its lens exactly as before, so every frame is bit-identical.
/// That keeps Red's pick of 2026-10-31, L03 with the lens at 1.8 (FrontRoomsTrofferKit.LensPeak), until Red picks from the
/// A/B. Mode.Tied: a lamp's lens follows its own Light, E = pi x I / A x the Light's colour, where I is the lamp's FULL
/// intensity times its state (the game's lamp modes: flicker, stutter, dim, dead; a metal-halide box's level and tint; a
/// lamp's scale), never a distance budget (a far lamp keeps its lit lens), and A is the kit's lens opening (table below), so
/// lens and floor change by the same factor through every flicker. A lens-only share its builder gave rides on top (ShareOf:
/// SP1's pool grid at FrontRoomsTrofferKit.PoolGridLensLevel, 0.4, Red's plan A of 2026-11-10 for the pool flashes, stays a
/// dim). A lit ceiling patch (P1-A's glow) keeps today's drive: it is bounce, not lens.
///
/// The call: the light line's scene-4 key (Shift + .; FrontRoomsStructuresWater's keys) calls Toggle() (or Set(Mode.Tied) /
/// Set(Mode.Today)): one call, no allocation.
/// Readers bump on Version: scene 4's FrontRoomsStructuresLook re-drives each lamp's lens once when it moves (Today restores
/// exactly what today's code had written), recaptures the current district's probes, and drives every later lens write
/// through Drive while Tied. Not wired in step 1, so they stay on today's path in both modes: the map's lamps
/// (FrontRoomsMapWorld TickFixtures / WriteEmission, also scene 4's P district), the title stream (FrontRoomsRoomStream),
/// the Run! rig, the title vignettes; each adopts Drive(...) when LW3 lands after Red's pick (the map's part by contract).
///
/// Units. URP has no 1/pi in its direct diffuse, so one intensity unit stands for I_cd / pi, hence the pi (10_research.md
/// section 2). Drive returns the colour to PASS to SetColor or a property block (gamma, as LensPeak's 1.8 is passed); this
/// linear project linearises it, so the shader receives pi x I / A x the Light's linear colour (x its colour temperature
/// when the light uses one, as GlassRT reads lights). Level 0 (I 5, colour (1, .96, .88), L03's 0.634 m2): (24.8, 22.6,
/// 18.6) linear, passed as (4.30, 4.13, 3.78), against today's 1.8 passed (3.6-4.1 linear): the lens about 6 x brighter, the
/// floor unchanged. ViewFactor (default 1) scales the tied drive: 0.5-0.6 is the research's factor for a lens with no
/// view-angle falloff (12-15 linear at Level 0); it is clamped to 0.25..4.
///
/// Lens openings A (m2, the luminous opening the player sees; [EST] = from the kit's size, not a photometric file):
///   L03 troffer, 2' x 4' lay-in ....... 0.634  (the measured 2 x 4 lensed troffer of 10_research.md section 2: pi / A = 4.96)
///   Flush (ZL F5), 2' x 2' lay-in ..... 0.298  (10_research.md section 2's 2 x 2: pi / A = 10.5)
///   Wrap (Kit_WrapFixture_4ft) ........ 0.29   [EST] a two-lamp wraparound lens about 240 mm wide x 1.22 m, from below
///   Strip (ZL_STRIP, two white T12) ... 0.087  2 tubes x 38 mm x 1.15 m of glass, projected
///   Pendant (ZL_PEND) ................. 0.195  [EST] two side lens strips about 40 mm x 2.44 m
///   WallBox (ZL_WALLBOX, P1-A) ........ 0.13   [EST] the top glass over the middle chamber, about 600 x 220 mm (seen from above only)
///   Niche (ZL_NICHE, P2-A) ............ 0.0287 the lens inside the ring's 191 mm (zone_lamps 20_design.md "Niche")
///   NicheSmall (ZL_NICHE_S) ........... 0.0118 [EST] the 165 mm ring's inside, scaled from P2-A: about 123 mm
///   Globe (ZL_GLOBE, F7) .............. 0.0079 [EST] a 100 W globe end-on, about 100 mm across (10_pool.md 3.7)
///   any other lens (a stand-in) ....... the largest face of its mesh bounds in world metres, at least 0.005
/// The L03 drive is its emission colour (the band peak of its lens map); the map's mean over the opening is under 1, so the
/// lens's mean luminance sits that much under pi x I / A: ViewFactor covers it in the A/B.
///
/// The Relay rule (FrontRoomsTrofferKit's header; not changed here): the Relay's lens, FrontRoomsRelayLens.FullEmission
/// (0.80 / 0.76 / 0.65 as passed, about 0.61 linear at its peak), must stay under the troffers' 99th percentile. Tied puts a lit
/// Level 0 troffer at 24.8 linear x ViewFactor x share; at the lowest ViewFactor (0.25) the dimmest lit troffer, SP1's grid
/// at 0.4, is 2.5 linear (passed 1.5), still about 4 x the Relay, so the rule holds in every setting. Its kinship (the Relay as
/// a cut strip of the same lens, same tint) changes: under Tied the troffer is several times the Relay rather than 2.25 x.
///
/// The two post hooks LW3 names, default today's (Red 2026-11-10 15:49 "keep today's brightness" for image 1: nothing sets
/// them in step 1). SetZoneExposure(zone, ev): a fixed post exposure for one zone (absolute EV, URP's ColorAdjustments
/// postExposure, as LW3 asks: fixed per zone); AtZone(zone) tells the hook where the camera is (scene 4: the Look's preset).
/// SetBloomThreshold(t): URP's Bloom threshold (gamma, as URP takes it); LW3 puts it between the brightest wall (about 1.2 x
/// the floor) and the lens, about 2-5 x the floor in linear light (pass the gamma of that: linear ^ (1 / 2.2)). Both live on
/// ONE runtime global volume ("Post / LW3 lens hook (runtime)", priority 200: over the film look 0, the zone grades 1, the
/// shot looks 2, scene 4's pool grade 100 and the quality override 100), made only when a value is first set, with only the
/// set parameters overridden; with none set it is inactive. Never saved; a scene object (it goes with its scene and is made
/// again by the next call). A set exposure switches at the zone change (no blend; film one stop at a time or set neighbouring
/// zones alike). Today's values: exposure 0.15 (the film look: Level 0, Tall, Low, the title, the station; the PoolLevel and
/// Run! B grades leave it), Office 0, PoolClub 0.25, PoolDark 0.4, PoolHall 0.45; scene 4's pool presets 0.6 (the Look's
/// runtime pool grade); bloom threshold 1.05 everywhere (FrontRoomsPost; no zone grade overrides it).
///
/// Cost: Today one bool read per lens write. Tied one colour per lens write (a level change), nothing at rest, one re-drive
/// of each lamp on a flip; no allocation per frame. The same on every tier and platform (three-platform parity): the cost
/// asks for no reduced-tier path. ASCII only.
/// </summary>
public static class FrontRoomsLampLens
{
    /// <summary>How a lamp's lens is driven: Today (each lamp as before; the default) or Tied (from its own Light).</summary>
    public enum Mode : byte { Today, Tied }

    /// <summary>The game's grade zones (Resources/Rendering/FrontRoomsPost_&lt;zone&gt;; Level0 = the film look alone).</summary>
    public enum Zone : byte { Level0, Office, Tall, PoolClub, PoolHall, PoolLevel, PoolDark, RunB }

    const int ZoneCount = 8;

    /// <summary>Lens openings (m2): see the table in the class header.</summary>
    public const float AreaL03 = .634f, AreaFlush = .298f, AreaWrap = .29f, AreaStrip = .087f, AreaPendant = .195f,
        AreaWallBox = .13f, AreaNiche = .0287f, AreaNicheSmall = .0118f, AreaGlobe = .0079f, AreaMin = .005f;

    /// <summary>ViewFactor's range: at 0.25 the dimmest lit troffer still clears the Relay's lens (header).</summary>
    public const float ViewFactorMin = .25f, ViewFactorMax = 4f;

    /// <summary>Today's bloom threshold (FrontRoomsPost, gamma) and today's film-look exposure, for readers and HUD lines.</summary>
    public const float TodayBloomThreshold = 1.05f, TodayFilmExposure = .15f;

    /// <summary>The hook volume's priority (header).</summary>
    public const float PostPriority = 200f;

    const string PostName = "Post / LW3 lens hook (runtime)";

    /// <summary>Today (default) or Tied.</summary>
    public static Mode Current { get; private set; }

    /// <summary>True while lenses follow their lights.</summary>
    public static bool Tied => Current == Mode.Tied;

    /// <summary>Bumps whenever the tied drive changes (the mode, or ViewFactor while Tied): readers re-drive their lenses once.</summary>
    public static int Version { get; private set; }

    /// <summary>The tied drive's factor (1; 0.5-0.6 = a lens with no view-angle falloff). Set with SetViewFactor.</summary>
    public static float ViewFactor { get; private set; } = 1f;

    /// <summary>The zone the camera is in, as the last AtZone said (Level0 until then).</summary>
    public static Zone CurrentZone { get; private set; }

    static readonly float[] zoneExposure = Unset(ZoneCount);
    static float bloomThreshold = float.NaN;
    static GameObject postObject;
    static VolumeProfile postProfile;
    static ColorAdjustments exposureOverride;
    static Bloom bloomOverride;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Current = Mode.Today;
        Version = 0;
        ViewFactor = 1f;
        CurrentZone = Zone.Level0;
        for (var i = 0; i < ZoneCount; i++) zoneExposure[i] = float.NaN;
        bloomThreshold = float.NaN;
        postObject = null;
        postProfile = null;
        exposureOverride = null;
        bloomOverride = null;
    }

    static float[] Unset(int n)
    {
        var a = new float[n];
        for (var i = 0; i < n; i++) a[i] = float.NaN;
        return a;
    }

    // ---------------------------------------------------------------- the switch (the A/B key)

    /// <summary>Sets the mode; a change bumps Version (readers re-drive their lenses once).</summary>
    public static void Set(Mode mode)
    {
        if (Current == mode) return;
        Current = mode;
        Version++;
    }

    /// <summary>Today -> Tied -> Today (the scene-4 key, Shift + .); returns the mode now.</summary>
    public static Mode Toggle()
    {
        Set(Tied ? Mode.Today : Mode.Tied);
        return Current;
    }

    /// <summary>The tied drive's factor, clamped to ViewFactorMin..ViewFactorMax (NaN = 1); a change while Tied bumps Version.</summary>
    public static void SetViewFactor(float factor)
    {
        var f = float.IsNaN(factor) ? 1f : Mathf.Clamp(factor, ViewFactorMin, ViewFactorMax);
        if (Mathf.Abs(f - ViewFactor) < 1e-5f) return;
        ViewFactor = f;
        if (Tied) Version++;
    }

    /// <summary>Everything back to today's: Today, ViewFactor 1, no zone exposure, no bloom threshold.</summary>
    public static void ResetAll()
    {
        Set(Mode.Today);
        ViewFactor = 1f;
        for (var i = 0; i < ZoneCount; i++) zoneExposure[i] = float.NaN;
        bloomThreshold = float.NaN;
        ApplyPost();
    }

    // ---------------------------------------------------------------- the tied drive

    /// <summary>
    /// The tied lens colour to pass to SetColor (gamma): pi x <paramref name="intensity"/> / <paramref name="area"/> x the
    /// light's linear colour (x its colour temperature when it uses one) x ViewFactor x <paramref name="share"/>.
    /// <paramref name="intensity"/> is the lamp's full intensity times its state (mode level, scale), never a distance budget;
    /// <paramref name="area"/> its kit's opening (AreaOf); <paramref name="share"/> a lens-only share its builder gave (ShareOf).
    /// A null light counts as white. Pure: reads no mode (the caller asks Tied first).
    /// </summary>
    public static Color Drive(Light light, float intensity, float area, float share)
    {
        var c = Color.white;
        if (light != null)
        {
            c = light.color.linear;
            if (GraphicsSettings.lightsUseColorTemperature && light.useColorTemperature) c *= Mathf.CorrelatedColorTemperatureToRGB(light.colorTemperature);
        }
        var k = Mathf.PI * Mathf.Max(0f, intensity) / Mathf.Max(AreaMin, area) * ViewFactor * Mathf.Max(0f, share);
        return new Color(c.r * k, c.g * k, c.b * k, 1f).gamma;
    }

    /// <summary>
    /// The lens opening of the kit whose lens <paramref name="lens"/> is (the table in the header): L03 (FrontRoomsTrofferKit),
    /// a zone-lamp family (FrontRoomsLampKit.TryFamilyOf, which loads nothing), else the largest face of its mesh bounds in world
    /// metres (a stand-in), at least AreaMin. Call once per lamp and keep it (it reads components).
    /// </summary>
    public static float AreaOf(Renderer lens)
    {
        if (lens == null) return AreaL03;
        if (FrontRoomsTrofferKit.IsKitLens(lens)) return AreaL03;
        FrontRoomsLampKit.Family family;
        if (FrontRoomsLampKit.TryFamilyOf(lens, out family))
            switch (family)
            {
                case FrontRoomsLampKit.Family.Strip: return AreaStrip;
                case FrontRoomsLampKit.Family.Pendant: return AreaPendant;
                case FrontRoomsLampKit.Family.Wrap: return AreaWrap;
                case FrontRoomsLampKit.Family.WallBox: return AreaWallBox;
                case FrontRoomsLampKit.Family.Niche: return AreaNiche;
                case FrontRoomsLampKit.Family.NicheSmall: return AreaNicheSmall;
                case FrontRoomsLampKit.Family.Flush: return AreaFlush;
                case FrontRoomsLampKit.Family.Globe: return AreaGlobe;
            }
        var mf = lens.GetComponent<MeshFilter>();
        var mesh = mf != null ? mf.sharedMesh : null;
        if (mesh == null) return AreaL03;
        var s = Vector3.Scale(mesh.bounds.size, lens.transform.lossyScale);
        float x = Mathf.Abs(s.x), y = Mathf.Abs(s.y), z = Mathf.Abs(s.z);
        return Mathf.Max(AreaMin, Mathf.Max(x * y, Mathf.Max(y * z, x * z)));
    }

    /// <summary>
    /// The lens-only share a builder baked into today's drive <paramref name="todayDrive"/>: for an L03 lens, its peak over
    /// LensPeak (SP1's pool grid: 0.72 / 1.8 = FrontRoomsTrofferKit.PoolGridLensLevel 0.4), at most 1; 1 for every other lens
    /// (their drives are their kits' full drives). Call once per lamp with the drive it was built with.
    /// </summary>
    public static float ShareOf(Renderer lens, Color todayDrive)
    {
        if (lens == null || !FrontRoomsTrofferKit.IsKitLens(lens)) return 1f;
        return Mathf.Clamp01(todayDrive.maxColorComponent / FrontRoomsTrofferKit.LensPeak);
    }

    // ---------------------------------------------------------------- the post hooks (default today's)

    /// <summary>A fixed post exposure (EV, absolute) for <paramref name="zone"/>; NaN = today's (as ClearZoneExposure).</summary>
    public static void SetZoneExposure(Zone zone, float ev)
    {
        var i = (int)zone;
        if (i < 0 || i >= ZoneCount) return;
        zoneExposure[i] = ev;
        ApplyPost();
    }

    /// <summary>The zone back to today's exposure.</summary>
    public static void ClearZoneExposure(Zone zone) => SetZoneExposure(zone, float.NaN);

    /// <summary>The zone's fixed exposure, or NaN for today's.</summary>
    public static float ZoneExposure(Zone zone)
    {
        var i = (int)zone;
        return i >= 0 && i < ZoneCount ? zoneExposure[i] : float.NaN;
    }

    /// <summary>The bloom threshold set (gamma), or NaN for today's (TodayBloomThreshold).</summary>
    public static float BloomThreshold => bloomThreshold;

    /// <summary>URP's Bloom threshold (gamma) for every zone; NaN = today's (as ClearBloomThreshold).</summary>
    public static void SetBloomThreshold(float threshold)
    {
        bloomThreshold = float.IsNaN(threshold) ? float.NaN : Mathf.Max(0f, threshold);
        ApplyPost();
    }

    /// <summary>Today's bloom threshold again.</summary>
    public static void ClearBloomThreshold() => SetBloomThreshold(float.NaN);

    /// <summary>Where the camera is (scene 4: FrontRoomsStructuresLook from its preset). Cheap when nothing is set: it only
    /// keeps the zone.</summary>
    public static void AtZone(Zone zone)
    {
        var moved = zone != CurrentZone;
        CurrentZone = zone;
        // nothing set: the volume (if any) is already off; something set: apply on a move, or again where the volume went
        // with its scene
        if (!AnySet()) return;
        if (moved || postObject == null) ApplyPost();
    }

    static bool AnySet()
    {
        if (!float.IsNaN(bloomThreshold)) return true;
        for (var i = 0; i < ZoneCount; i++) if (!float.IsNaN(zoneExposure[i])) return true;
        return false;
    }

    // The one hook volume: made at the first value set, the set parameters overridden, inactive with none.
    static void ApplyPost()
    {
        var zi = (int)CurrentZone;
        var ev = zi >= 0 && zi < ZoneCount ? zoneExposure[zi] : float.NaN;
        var wantExposure = !float.IsNaN(ev);
        var wantBloom = !float.IsNaN(bloomThreshold);
        if (!wantExposure && !wantBloom)
        {
            if (postObject != null) postObject.SetActive(false);
            return;
        }
        if (postObject == null)
        {
            if (postProfile != null)
            {
                if (Application.isPlaying) Object.Destroy(postProfile);
                else Object.DestroyImmediate(postProfile);
            }
            // a plain scene object in Play (it goes with its scene); never saved by an edit-mode harness
            postObject = new GameObject(PostName);
            if (!Application.isPlaying) postObject.hideFlags = HideFlags.DontSave;
            var volume = postObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = PostPriority;
            postProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            postProfile.name = "LW3 lens hook (runtime)";
            postProfile.hideFlags = HideFlags.DontSave;
            exposureOverride = postProfile.Add<ColorAdjustments>(false);
            bloomOverride = postProfile.Add<Bloom>(false);
            volume.sharedProfile = postProfile;
        }
        exposureOverride.postExposure.Override(wantExposure ? ev : TodayFilmExposure);
        exposureOverride.postExposure.overrideState = wantExposure;
        bloomOverride.threshold.Override(wantBloom ? bloomThreshold : TodayBloomThreshold);
        bloomOverride.threshold.overrideState = wantBloom;
        postObject.SetActive(true);
    }
}
