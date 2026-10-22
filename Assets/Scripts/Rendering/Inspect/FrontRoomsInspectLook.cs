using FrontRooms.Map;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// The close-up's look (research/inspect/10_design.md §3.7): no added lamp; three runtime volumes.
/// - "Post / inspect Shot" (priority 2), weight = the rig's eased position weight: vignette base +0.04
///   (object +0.08); lens distortion -0.001 and CA max(0.001, base - 0.04) for flat and panel (the
///   uber keywords stay on: no variant change, no hitch).
/// - "Post / inspect Adapt" (priority 3), weight = adapt a on its own tau (0.6 s; 1.2 s with Reduce
///   flashing): postExposure base + a × AdaptMaxEv, contrast base + a × AdaptContrast, only in dark cells.
///   The light W is read where the face looks (its own lamp dominates: W = L + 0.02 ΣL_n), and the lift
///   is the eye's: log2(0.45 / W) EV, up to AdaptMaxEv (fix 2026-10-14; v1 read the cell behind a wall
///   face and weighted the neighbours 0.2, so a dead own lamp under lit neighbours got no lift).
///   Calibrated on the P2 captures (seed 516574485): the paper reads 0.425 linear lit, 0.037 with the own
///   lamp off and 3 lit neighbours, 0.035 with all 5 off, so a neighbour adds about 0.002 (W share 0.02
///   keeps them a tie-break); 2.25 EV brings the dark paper to about sRGB 113 (verifier: >= 110).
/// - "Post / inspect DOF" (priority 2, desktop only): Bokeh for the object kind, from 1 mm f/32 (zero
///   CoC) to the camcorder target by the eased weight; focus = the object's distance.
/// Profiles are built at runtime (CreateInstance, DontSave): no .asset is written, no sharedProfile is
/// touched. Values are base + delta: the base is read from the resolved volume stack at Open (with
/// these volumes at weight 0), so Office's own profile carries through.
/// </summary>
public static class FrontRoomsInspectLook
{
#if (UNITY_WEBGL && !UNITY_EDITOR) || FRONTROOMS_WEBGL_PREVIEW || UNITY_IOS || UNITY_ANDROID
    /// <summary>WebGL and mobile: no DOF volume and no prewarm (§6). Desktop is byte-identical without this gate.</summary>
    public static readonly bool DofSupported = false;
#else
    public static readonly bool DofSupported = true;
#endif

    public const float VignetteFlat = .04f, VignetteObject = .08f, LensDistortionFlat = -.001f, CaDrop = .04f, CaFloor = .001f;
    /// <summary>W at and above which there is no lift; each neighbour's share of W (render-calibrated: a neighbour lamp barely lights the face, see above).</summary>
    public const float AdaptLitBreak = .45f, NeighbourShare = .02f;
    /// <summary>The light is read this far in front of the face (the cell it looks into, never the one behind its wall).</summary>
    public const float LightProbeAhead = .3f;
    /// <summary>The lift at a = 1 (EV) and the contrast added with it (look values, tuned on captures; v1 kept them in FrontRoomsShotTimings.Inspect).</summary>
    public const float AdaptMaxEv = 2.25f, AdaptContrast = 12f;
    // DOF (§3.7): background at 3 m blurs ~5 px at 1080p with the object at 0.2 m. URP's Bokeh CoC in pixels is
    // (1 - P/z) × maxCoC × 14 (BokehMaxRadius 14 px), maxCoC = (f/N)(f/1000)/(P - f/1000): f 16.8 mm at f/4 gives 5 px.
    public const float DofStartFocal = 1f, DofStartAperture = 32f, DofAperture = 4f, DofBackgroundPx = 5f, DofBackgroundAt = 3f;

    static GameObject root;
    static Volume shotVolume, adaptVolume, dofVolume;
    static VolumeProfile shotProfile, adaptProfile, dofProfile;
    static Vignette vignette;
    static LensDistortion lens;
    static ChromaticAberration chroma;
    static ColorAdjustments adaptColor;
    static DepthOfField dof;
    static float baseVignette, baseLens, baseChroma, baseExposure, baseContrast;
    static bool snapshotValid, prewarmPending;
    static int prewarmFrame = -1;

    /// <summary>The adapt weight a (0..1) and its current target.</summary>
    public static float Adapt { get; private set; }
    public static float AdaptTarget { get; private set; }
    /// <summary>The light W of the target's cell (0..1) used for the adapt target.</summary>
    public static float CellLight { get; private set; } = 1f;
    /// <summary>The lift now, in EV (Adapt × AdaptMaxEv).</summary>
    public static float LiftEv => Adapt * AdaptMaxEv;
    /// <summary>The adapt target for a light W: log2(AdaptLitBreak / W) / AdaptMaxEv, clamped to 0..1 (0 at W ≥ 0.45).</summary>
    public static float AdaptFor(float w) => w >= AdaptLitBreak ? 0f : Mathf.Clamp01(Mathf.Log(AdaptLitBreak / Mathf.Max(w, 1e-3f), 2f) / AdaptMaxEv);
    public static float ShotWeight => shotVolume != null ? shotVolume.weight : 0f;
    public static float DofWeight => dofVolume != null ? dofVolume.weight : 0f;
    public static bool Holding { get; private set; }
    static InspectKind kind;
    static float dofTargetFocal = 16.8f;

    /// <summary>Base values read at the last Open (for tests: Office resolves postExposure 0, CA 0.03).</summary>
    public static Vector4 BaseSnapshot => new Vector4(baseExposure, baseContrast, baseVignette, baseChroma);

    static void Ensure(Transform parent)
    {
        if (root != null) return;
        root = new GameObject("Post / inspect") { hideFlags = HideFlags.DontSave };
        if (parent != null) root.transform.SetParent(parent, false);
        shotProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        shotProfile.hideFlags = HideFlags.DontSave;
        vignette = shotProfile.Add<Vignette>(false);
        lens = shotProfile.Add<LensDistortion>(false);
        chroma = shotProfile.Add<ChromaticAberration>(false);
        adaptProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        adaptProfile.hideFlags = HideFlags.DontSave;
        adaptColor = adaptProfile.Add<ColorAdjustments>(false);
        shotVolume = MakeVolume("Post / inspect Shot", 2f, shotProfile);
        adaptVolume = MakeVolume("Post / inspect Adapt", 3f, adaptProfile);
        if (DofSupported)
        {
            dofProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            dofProfile.hideFlags = HideFlags.DontSave;
            dof = dofProfile.Add<DepthOfField>(false);
            dof.mode.Override(DepthOfFieldMode.Bokeh);
            dof.focalLength.Override(DofStartFocal);
            dof.aperture.Override(DofStartAperture);
            dof.focusDistance.Override(1f);
            dofVolume = MakeVolume("Post / inspect DOF", 2f, dofProfile);
        }
    }

    static Volume MakeVolume(string name, float priority, VolumeProfile profile)
    {
        var go = new GameObject(name) { hideFlags = HideFlags.DontSave };
        go.transform.SetParent(root.transform, false);
        var v = go.AddComponent<Volume>();
        v.isGlobal = true;
        v.priority = priority;
        v.weight = 0f;
        v.profile = profile;   // the instance, never sharedProfile on an asset
        return v;
    }

    /// <summary>Read base values from the resolved stack. Only while every inspect volume is at weight 0.</summary>
    static void Snapshot()
    {
        if (snapshotValid && Adapt > 1e-3f) return;   // the adapt volume is still decaying: keep the last snapshot
        var stack = VolumeManager.instance.stack;
        if (stack == null) return;
        var v = stack.GetComponent<Vignette>();
        var l = stack.GetComponent<LensDistortion>();
        var c = stack.GetComponent<ChromaticAberration>();
        var a = stack.GetComponent<ColorAdjustments>();
        baseVignette = v != null ? v.intensity.value : 0f;
        baseLens = l != null ? l.intensity.value : 0f;
        baseChroma = c != null ? c.intensity.value : 0f;
        baseExposure = a != null ? a.postExposure.value : 0f;
        baseContrast = a != null ? a.contrast.value : 0f;
        snapshotValid = true;
    }

    /// <summary>Open: snapshot the base, write base + delta into the runtime profiles.</summary>
    public static void Begin(Transform parent, InspectKind k)
    {
        Ensure(parent);
        kind = k;
        Holding = true;
        if (shotVolume.weight <= 0f && adaptVolume.weight <= 0f) Snapshot();
        var obj = k == InspectKind.Object;
        vignette.intensity.Override(Mathf.Clamp01(baseVignette + (obj ? VignetteObject : VignetteFlat)));
        lens.active = !obj;
        chroma.active = !obj;
        // Keep LD and CA non-zero so the uber keywords never change (no variant, no hitch).
        lens.intensity.Override(baseLens != 0f ? LensDistortionFlat : 0f);
        chroma.intensity.Override(Mathf.Max(CaFloor, baseChroma - CaDrop));
        if (baseChroma <= 0f) chroma.intensity.Override(0f);
        adaptColor.postExposure.Override(baseExposure + AdaptMaxEv);
        adaptColor.contrast.Override(Mathf.Clamp(baseContrast + AdaptContrast, -100f, 100f));
        dofTargetFocal = SolveDofFocal(.2f);
    }

    /// <summary>The close-up gave control back: the adapt target falls to 0 (its own tau), DOF goes with the shot weight.</summary>
    public static void Release() => Holding = false;

    /// <summary>
    /// Every frame while EXAMINE is active (and after, until the adapt has decayed): the shot volume
    /// follows the rig's eased weight, the adapt follows its own tau, DOF follows the object.
    /// </summary>
    public static void Tick(float dt, float easedWeight, float cellLight, float objectDistance)
    {
        if (root == null) return;
        if (prewarmPending && Time.frameCount != prewarmFrame) EndPrewarm();
        shotVolume.weight = Mathf.Clamp01(easedWeight);
        CellLight = cellLight;
        AdaptTarget = Holding ? AdaptFor(cellLight) : 0f;
        var tau = FrontRoomsSettings.ReduceFlashing ? FrontRoomsShotTimings.Inspect.AdaptTauCalm : FrontRoomsShotTimings.Inspect.AdaptTau;
        Adapt += (AdaptTarget - Adapt) * (1f - Mathf.Exp(-Mathf.Max(0f, dt) / tau));
        if (Mathf.Abs(Adapt - AdaptTarget) < 1e-4f) Adapt = AdaptTarget;
        adaptVolume.weight = Adapt;
        if (dofVolume != null && !prewarmPending)
        {
            var on = kind == InspectKind.Object && easedWeight > 0f;
            dofVolume.weight = on ? 1f : 0f;
            if (on)
            {
                var w = Mathf.Clamp01(easedWeight);
                dof.focalLength.Override(Mathf.Lerp(DofStartFocal, SolveDofFocal(Mathf.Max(.05f, objectDistance)), w));
                dof.aperture.Override(Mathf.Lerp(DofStartAperture, DofAperture, w));
                dof.focusDistance.Override(Mathf.Max(.05f, objectDistance));
            }
        }
    }

    /// <summary>The focal length (mm) at DofAperture that blurs DofBackgroundAt metres by DofBackgroundPx with focus at p (URP's Bokeh model).</summary>
    public static float SolveDofFocal(float p)
    {
        var coc = DofBackgroundPx / 14f / Mathf.Max(1e-3f, 1f - p / DofBackgroundAt);   // maxCoC needed
        // (f/N)(f/1000) = coc (p - f/1000)  →  f² / (1000 N) + coc f / 1000 - coc p = 0
        var a = 1f / (1000f * DofAperture);
        var b = coc / 1000f;
        var c = -coc * p;
        return (-b + Mathf.Sqrt(b * b - 4f * a * c)) / (2f * a);
    }

    /// <summary>The CoC in pixels at depth z for a Bokeh setting (for the tests).</summary>
    public static float CocPixels(float focalMm, float aperture, float focus, float z)
    {
        var f = focalMm / 1000f;
        var maxCoC = (focalMm / aperture) * f / Mathf.Max(1e-6f, focus - f);
        return Mathf.Clamp((1f - focus / z) * maxCoC, -1f, 1f) * 14f;
    }

    /// <summary>
    /// At MapRunStarted, desktop only: one frame with the DOF volume at weight 1 and zero-CoC values, so
    /// the Bokeh pass's shaders are compiled before the first object close-up (§3.7). Visually identical.
    /// </summary>
    public static void Prewarm(Transform parent)
    {
        if (!DofSupported) return;
        Ensure(parent);
        dof.focalLength.Override(DofStartFocal);
        dof.aperture.Override(DofStartAperture);
        dof.focusDistance.Override(10f);
        dofVolume.weight = 1f;
        prewarmPending = true;
        prewarmFrame = Time.frameCount;
    }

    static void EndPrewarm()
    {
        prewarmPending = false;
        if (dofVolume != null) dofVolume.weight = 0f;
    }

    public static bool PrewarmPending => prewarmPending;

    /// <summary>The own lamp's level at the last CellLightAt (0..1; a lamp-less start-area cell reads 1).</summary>
    public static float OwnLight { get; private set; } = 1f;

    /// <summary>
    /// W = min(1, L + 0.02 ΣL_n) over the cell at <paramref name="world"/> and its 4 neighbours, from map.LampLevel (the
    /// logical level, overrides included; read-only). Pass the point LightProbeAhead in front of the face. The own cell
    /// counts as lit when it is a lamp-less start-area cell (a target inside the stream rooms, which carry their own
    /// lights); a lamp-less neighbour counts as dark (the start rooms sit behind the facade, and their lights go out
    /// once the start door shuts); a map cell with no lamp counts as dark.
    /// </summary>
    public static float CellLightAt(FrontRoomsMapWorld map, Vector3 world)
    {
        OwnLight = 1f;
        if (map == null) return 1f;
        var cell = map.CellOf(world);
        float Level(GridCoord c)
        {
            var l = map.LampLevel(c);
            return l >= 0f ? l : 0f;
        }
        var own = map.LampLevel(cell);
        var w = own >= 0f ? own : map.InStartArea(cell) ? 1f : 0f;
        OwnLight = w;
        w += NeighbourShare * (Level(new GridCoord(cell.x + 1, cell.y)) + Level(new GridCoord(cell.x - 1, cell.y))
                             + Level(new GridCoord(cell.x, cell.y + 1)) + Level(new GridCoord(cell.x, cell.y - 1)));
        return Mathf.Min(1f, w);
    }

    /// <summary>True when every inspect volume is back at weight 0 (the runner may stop ticking).</summary>
    public static bool Idle => root == null || (shotVolume.weight <= 0f && adaptVolume.weight <= 0f && Adapt <= 0f && (dofVolume == null || dofVolume.weight <= 0f) && !prewarmPending);

    /// <summary>Teardown: volumes to 0 and destroyed with their profiles.</summary>
    public static void Abort()
    {
        Holding = false;
        Adapt = AdaptTarget = 0f;
        prewarmPending = false;
        snapshotValid = false;
        if (root != null)
        {
            if (Application.isPlaying) Object.Destroy(root); else Object.DestroyImmediate(root);
        }
        if (shotProfile != null) Object.DestroyImmediate(shotProfile);
        if (adaptProfile != null) Object.DestroyImmediate(adaptProfile);
        if (dofProfile != null) Object.DestroyImmediate(dofProfile);
        root = null;
        shotVolume = adaptVolume = dofVolume = null;
        shotProfile = adaptProfile = dofProfile = null;
    }
}
