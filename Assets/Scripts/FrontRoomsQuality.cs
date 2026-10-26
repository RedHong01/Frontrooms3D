using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Render-quality presets and details (Settings › GRAPHICS), on every platform (Red, 2026-10-24: "我希望在游戏build里
/// （mac/iphone/ipad全平台）都有调整不同渲染质量预设/细节参数的选择"). Research, numbers and the preset ladder:
/// Documentation/research/interaction/settings/ (README §1, IA §3 and §7).
///
/// The shipped look is the reference: a value equal to the pipeline asset's own is never written, so the desktop at
/// ULTRA (its default) draws byte for byte as before. Writes go to the active URP asset at runtime and are undone when
/// play stops (the Editor keeps the asset file as it was). RESOLUTION AUTO gives every handheld screen the same
/// rendered-pixel budget, recomputed on every fold, unfold and rotation: the iPhone Duo's inner display (5.0 Mpx)
/// renders at 80 %, which brings it back to about the outer display's cost (probe v3, 2026-10-24).
/// </summary>
public static class FrontRoomsQuality
{
    public enum Preset { Low, Medium, High, Ultra }

    const string Prefix = "FrontRooms.Quality.";
    /// <summary>Rendered megapixels a handheld screen gets under RESOLUTION AUTO (LOW: 2.0, else 3.2).</summary>
    const float AutoBudget = 3.2e6f, AutoBudgetLow = 2.0e6f;

    public static readonly int[] HandheldFrameRates = { 30, 60 };
    /// <summary>ProMotion displays (Red, 2026-10-24, D4 · B): 40 is a steady step on a 120 Hz panel (120 ÷ 3); 120 costs
    /// the most battery and heat, and iOS drops it in Low Power Mode or when hot. Needs Enable ProMotion Support.</summary>
    public static readonly int[] ProMotionFrameRates = { 30, 40, 60, 120 };
    /// <summary>Desktop FRAME RATE: 0 follows the display (vsync, as shipped), then caps; -1 is MAX (no cap, no vsync).</summary>
    public static readonly int[] DesktopFrameRates = { 0, 30, 60, 120, -1 };

    public static Preset CurrentPreset { get; private set; } = Preset.Ultra;
    /// <summary>0 AUTO, 1 FIXED.</summary>
    public static int Resolution { get; private set; }
    /// <summary>RENDER SCALE when FIXED: 50–100 in steps of 5.</summary>
    public static int RenderScalePercent { get; private set; } = 100;
    /// <summary>0 SHARP (FSR 1), 1 SOFT (bilinear).</summary>
    public static int Upscaling { get; private set; }
    /// <summary>Index into HandheldFrameRates or DesktopFrameRates.</summary>
    public static int FrameRate { get; private set; }
    /// <summary>0 OFF, 1 LOW (MSAA 2×), 2 HIGH (MSAA 4×).</summary>
    public static int AntiAliasing { get; private set; } = 2;
    /// <summary>0 LOW (SSAO at half resolution), 1 HIGH (full resolution, as shipped).</summary>
    public static int AmbientOcclusion { get; private set; } = 1;
    /// <summary>0 LOW, 1 MEDIUM, 2 HIGH (as shipped).</summary>
    public static int Shadows { get; private set; } = 2;
    /// <summary>0 LOW (no high-quality filtering), 1 HIGH (as shipped).</summary>
    public static int Bloom { get; private set; } = 1;
    /// <summary>0 FOLLOW iOS Low Power Mode (30 FPS and LOW while it is on), 1 IGNORE.</summary>
    public static int LowPower { get; private set; }

    /// <summary>The details differ from the last preset picked: QUALITY reads CUSTOM.</summary>
    public static bool IsCustom => !Matches(CurrentPreset);
    /// <summary>The render scale the game draws at now (AUTO resolved for this screen).</summary>
    public static float EffectiveRenderScale { get; private set; } = 1f;
    /// <summary>The frame rate the game is drawing at, smoothed (frames per unscaled second).</summary>
    public static float MeasuredFps { get; private set; } = 60f;
    /// <summary>The frame rate this platform aims for now (the display's on desktop vsync).</summary>
    public static int TargetFps { get; private set; } = 60;
    /// <summary>iOS Low Power Mode is on and FOLLOW holds the game at 30 FPS and LOW.</summary>
    public static bool LowPowerActive { get; private set; }
    /// <summary>SSAO's half-resolution switch could be reached (else AMBIENT OCCLUSION is hidden).</summary>
    public static bool AmbientOcclusionAvailable => SsaoDownsampleField() != null;

    public static event Action Changed;

    static bool loaded;
    static bool Handheld => FrontRoomsHandheld.Active;

    /// <summary>iOS's Low Power Mode can be read here (iPhone and iPad; not Android).</summary>
    public static bool LowPowerSupported => Application.platform == RuntimePlatform.IPhonePlayer || (Application.isEditor && Handheld);

    public static Preset DefaultPreset => Handheld ? Preset.High : Preset.Ultra;

    // ------------------------------------------------------------- state

    public static void Load()
    {
        loaded = true;
        if (!PlayerPrefs.HasKey(Prefix + "Preset"))
        {
            SetDetailsFrom(DefaultPreset);
            CurrentPreset = DefaultPreset;
        }
        else
        {
            CurrentPreset = (Preset)Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "Preset"), 0, 3);
            SetDetailsFrom(CurrentPreset);
            Resolution = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "Resolution", Resolution), 0, 1);
            RenderScalePercent = SnapScale(PlayerPrefs.GetInt(Prefix + "RenderScale", RenderScalePercent));
            Upscaling = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "Upscaling", Upscaling), 0, 1);
            // Stored as frames per second, so a 40 or 120 saved on a ProMotion display lands on the nearest step elsewhere.
            if (PlayerPrefs.HasKey(Prefix + "FrameRateFps")) FrameRate = RateIndex(PlayerPrefs.GetInt(Prefix + "FrameRateFps"));
            AntiAliasing = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "AntiAliasing", AntiAliasing), 0, 2);
            AmbientOcclusion = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "AmbientOcclusion", AmbientOcclusion), 0, 1);
            Shadows = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "Shadows", Shadows), 0, 2);
            Bloom = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "Bloom", Bloom), 0, 1);
        }
        LowPower = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "LowPower", 0), 0, 1);
        FrontRoomsQualityRunner.Ensure();
    }

    public static int[] FrameRates => Handheld ? (ProMotion ? ProMotionFrameRates : HandheldFrameRates) : DesktopFrameRates;

    /// <summary>A 120 Hz handheld display (iPhone Pro, iPhone Duo, iPad Pro); the Editor's handheld preview counts as one.</summary>
    public static bool ProMotion => Handheld && (Application.isEditor
        || (Application.platform == RuntimePlatform.IPhonePlayer && Screen.currentResolution.refreshRateRatio.value >= 119.0));

    static int RateIndex(int fps)
    {
        var rates = FrameRates;
        var best = 0;
        for (var i = 1; i < rates.Length; i++) if (Mathf.Abs(rates[i] - fps) < Mathf.Abs(rates[best] - fps)) best = i;
        return best;
    }

    /// <summary>Picks a preset: every detail takes its value.</summary>
    public static void PickPreset(Preset preset)
    {
        CurrentPreset = preset;
        SetDetailsFrom(preset);
        SaveAll();
    }

    /// <summary>Back to this device's default (RESET GRAPHICS).</summary>
    public static void ResetToDefault() => PickPreset(DefaultPreset);

    public static void SetResolution(int value) { Resolution = Mathf.Clamp(value, 0, 1); SaveAll(); }
    public static void SetRenderScale(int percent) { RenderScalePercent = SnapScale(percent); Resolution = 1; SaveAll(); }
    public static void SetUpscaling(int value) { Upscaling = Mathf.Clamp(value, 0, 1); SaveAll(); }
    public static void SetFrameRate(int index) { FrameRate = Mathf.Clamp(index, 0, FrameRates.Length - 1); SaveAll(); }
    public static void SetAntiAliasing(int value) { AntiAliasing = Mathf.Clamp(value, 0, 2); SaveAll(); }
    public static void SetAmbientOcclusion(int value) { AmbientOcclusion = Mathf.Clamp(value, 0, 1); SaveAll(); }
    public static void SetShadows(int value) { Shadows = Mathf.Clamp(value, 0, 2); SaveAll(); }
    public static void SetBloom(int value) { Bloom = Mathf.Clamp(value, 0, 1); SaveAll(); }
    public static void SetLowPower(int value) { LowPower = Mathf.Clamp(value, 0, 1); SaveAll(); }

    static void SetDetailsFrom(Preset p)
    {
        // ULTRA is the shipped look: full resolution everywhere. Below it a handheld renders AUTO; a desktop uses a
        // fixed share of its window (it has no pixel budget problem at ULTRA or HIGH).
        if (p == Preset.Ultra) { Resolution = Handheld ? 1 : 0; RenderScalePercent = 100; }
        else if (Handheld) { Resolution = 0; RenderScalePercent = 100; }
        else { Resolution = p == Preset.High ? 0 : 1; RenderScalePercent = p == Preset.Low ? 70 : p == Preset.Medium ? 85 : 100; }
        Upscaling = 0;
        FrameRate = Handheld ? RateIndex(p == Preset.Low ? 30 : 60) : 0;
        AntiAliasing = p == Preset.Low ? 0 : p == Preset.Medium ? 1 : 2;
        AmbientOcclusion = p == Preset.Low || p == Preset.Medium ? 0 : 1;
        Shadows = p == Preset.Low ? 0 : p == Preset.Medium ? 1 : 2;
        Bloom = p == Preset.Low || p == Preset.Medium ? 0 : 1;
    }

    static bool Matches(Preset p)
    {
        var resolution = Resolution; var scale = RenderScalePercent; var up = Upscaling; var fr = FrameRate;
        var aa = AntiAliasing; var ao = AmbientOcclusion; var sh = Shadows; var bl = Bloom;
        SetDetailsFrom(p);
        var same = resolution == Resolution && (Resolution == 0 || scale == RenderScalePercent) && up == Upscaling && fr == FrameRate
            && aa == AntiAliasing && ao == AmbientOcclusion && sh == Shadows && bl == Bloom;
        Resolution = resolution; RenderScalePercent = scale; Upscaling = up; FrameRate = fr;
        AntiAliasing = aa; AmbientOcclusion = ao; Shadows = sh; Bloom = bl;
        return same;
    }

    static int SnapScale(int percent) => Mathf.Clamp(Mathf.RoundToInt(percent / 5f) * 5, 50, 100);

    static void SaveAll()
    {
        PlayerPrefs.SetInt(Prefix + "Preset", (int)CurrentPreset);
        PlayerPrefs.SetInt(Prefix + "Resolution", Resolution);
        PlayerPrefs.SetInt(Prefix + "RenderScale", RenderScalePercent);
        PlayerPrefs.SetInt(Prefix + "Upscaling", Upscaling);
        PlayerPrefs.SetInt(Prefix + "FrameRateFps", FrameRates[Mathf.Clamp(FrameRate, 0, FrameRates.Length - 1)]);
        PlayerPrefs.SetInt(Prefix + "AntiAliasing", AntiAliasing);
        PlayerPrefs.SetInt(Prefix + "AmbientOcclusion", AmbientOcclusion);
        PlayerPrefs.SetInt(Prefix + "Shadows", Shadows);
        PlayerPrefs.SetInt(Prefix + "Bloom", Bloom);
        PlayerPrefs.SetInt(Prefix + "LowPower", LowPower);
        PlayerPrefs.Save();
        Apply();
        Changed?.Invoke();
    }

    // ------------------------------------------------------------- apply

    sealed class Originals
    {
        public UniversalRenderPipelineAsset Asset;
        public float RenderScale;
        public UpscalingFilterSelection Upscaling;
        public int Msaa, MainShadowRes, AddShadowRes, Cascades;
        public float ShadowDistance;
        public int VSync;
        public ScriptableRendererFeature Ssao;
        public bool SsaoDownsample;
    }

    static Originals orig;
    static GameObject bloomVolumeObject;
    static VolumeProfile bloomProfile;

    static UniversalRenderPipelineAsset Asset => GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;

    /// <summary>Applies the effective settings (call after Load, and whenever the screen or Low Power Mode changes).</summary>
    public static void Apply()
    {
        if (!Application.isPlaying) return;
        if (!loaded) Load();
        var urp = Asset;
        if (urp == null) return;
        if (orig == null || orig.Asset != urp) Capture(urp);
        // FOLLOW + Low Power Mode on: 30 FPS and LOW, without touching what the player picked.
        var useLow = LowPowerActive;
        var aa = useLow ? 0 : AntiAliasing;
        var ao = useLow ? 0 : AmbientOcclusion;
        var sh = useLow ? 0 : Shadows;
        var bl = useLow ? 0 : Bloom;

        EffectiveRenderScale = ResolveScale(useLow);
        Write(urp.renderScale, EffectiveRenderScale, v => urp.renderScale = v);
        var filter = EffectiveRenderScale < .999f ? (Upscaling == 0 ? UpscalingFilterSelection.FSR : UpscalingFilterSelection.Linear) : orig.Upscaling;
        if (urp.upscalingFilter != filter) urp.upscalingFilter = filter;

        var msaa = aa == 0 ? 1 : aa == 1 ? 2 : orig.Msaa;
        if (urp.msaaSampleCount != msaa) urp.msaaSampleCount = msaa;

        var distance = sh == 0 ? 25f : orig.ShadowDistance;
        var cascades = sh == 0 ? 1 : orig.Cascades;
        var mainRes = sh == 0 ? Mathf.Min(1024, orig.MainShadowRes) : orig.MainShadowRes;
        var addRes = sh == 0 ? Mathf.Min(1024, orig.AddShadowRes) : sh == 1 ? Mathf.Min(2048, orig.AddShadowRes) : orig.AddShadowRes;
        Write(urp.shadowDistance, distance, v => urp.shadowDistance = v);
        if (urp.shadowCascadeCount != cascades) urp.shadowCascadeCount = cascades;
        if (urp.mainLightShadowmapResolution != mainRes) urp.mainLightShadowmapResolution = mainRes;
        if (urp.additionalLightsShadowmapResolution != addRes) urp.additionalLightsShadowmapResolution = addRes;

        SetSsaoDownsample(ao == 0 ? true : orig.SsaoDownsample);
        SetBloomLow(bl == 0);
        ApplyFrameRate(useLow);
    }

    static float ResolveScale(bool useLow)
    {
        // The drawable in pixels (on a device the screen; under a harness the profile it forces).
        var pixels = FrontRoomsHandheld.ScreenPixels;
        var px = Mathf.Max(1f, pixels.x * pixels.y);
        if (Handheld)
        {
            if (Resolution == 1 && !useLow) return RenderScalePercent / 100f;
            var budget = useLow || CurrentPreset == Preset.Low && !IsCustom ? AutoBudgetLow : AutoBudget;
            // Quantised to 5 % so a small resize does not reallocate every target: the Duo's inner panel (5.0 Mpx)
            // renders at 80 %, an iPad 11" at 90 %, the outer display and phones at 100 %.
            return Mathf.Clamp(Mathf.Round(Mathf.Sqrt(budget / px) * 20f) / 20f, .5f, 1f);
        }
        return Resolution == 1 ? RenderScalePercent / 100f : orig != null ? orig.RenderScale : 1f;
    }

    static void ApplyFrameRate(bool useLow)
    {
        if (Handheld)
        {
            var rates = FrameRates;
            var fps = useLow ? 30 : rates[Mathf.Clamp(FrameRate, 0, rates.Length - 1)];
            if (Application.targetFrameRate != fps) Application.targetFrameRate = fps;
            TargetFps = fps;
            return;
        }
        var cap = DesktopFrameRates[Mathf.Clamp(FrameRate, 0, DesktopFrameRates.Length - 1)];
        if (cap == 0)
        {
            // As shipped: the quality level's vsync and the game's 60.
            if (QualitySettings.vSyncCount != orig.VSync) QualitySettings.vSyncCount = orig.VSync;
            if (Application.targetFrameRate != 60) Application.targetFrameRate = 60;
            TargetFps = orig.VSync > 0 ? Mathf.Max(30, Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value)) / orig.VSync : 60;
        }
        else
        {
            if (QualitySettings.vSyncCount != 0) QualitySettings.vSyncCount = 0;
            if (Application.targetFrameRate != cap) Application.targetFrameRate = cap;
            TargetFps = cap > 0 ? cap : Mathf.Max(60, Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value));
        }
    }

    static void Write(float current, float value, Action<float> set)
    {
        if (Mathf.Abs(current - value) > .0005f) set(value);
    }

    static void Capture(UniversalRenderPipelineAsset urp)
    {
        orig = new Originals
        {
            Asset = urp,
            RenderScale = urp.renderScale,
            Upscaling = urp.upscalingFilter,
            Msaa = urp.msaaSampleCount,
            ShadowDistance = urp.shadowDistance,
            Cascades = urp.shadowCascadeCount,
            MainShadowRes = urp.mainLightShadowmapResolution,
            AddShadowRes = urp.additionalLightsShadowmapResolution,
            VSync = QualitySettings.vSyncCount,
        };
        foreach (var data in urp.rendererDataList)
        {
            if (data == null) continue;
            foreach (var f in data.rendererFeatures) if (f is ScreenSpaceAmbientOcclusion) { orig.Ssao = f; break; }
            if (orig.Ssao != null) break;
        }
        orig.SsaoDownsample = GetSsaoDownsample();
        Application.quitting -= Restore;
        Application.quitting += Restore;
    }

    /// <summary>Puts the asset back as it shipped (play stops in the Editor; harmless in a player).</summary>
    static void Restore()
    {
        if (orig == null || orig.Asset == null) return;
        var urp = orig.Asset;
        urp.renderScale = orig.RenderScale;
        urp.upscalingFilter = orig.Upscaling;
        urp.msaaSampleCount = orig.Msaa;
        urp.shadowDistance = orig.ShadowDistance;
        urp.shadowCascadeCount = orig.Cascades;
        urp.mainLightShadowmapResolution = orig.MainShadowRes;
        urp.additionalLightsShadowmapResolution = orig.AddShadowRes;
        SetSsaoDownsample(orig.SsaoDownsample);
        QualitySettings.vSyncCount = orig.VSync;
        orig = null;
    }

    // SSAO's settings class is internal (URP 17.3): its half-resolution switch is reached by reflection. Only the
    // render-target size changes, so no shader variant is involved (turning SSAO off would ask for a stripped one).
    static FieldInfo settingsField, downsampleField;

    static FieldInfo SsaoDownsampleField()
    {
        if (downsampleField != null) return downsampleField;
        try
        {
            settingsField = typeof(ScreenSpaceAmbientOcclusion).GetField("m_Settings", BindingFlags.Instance | BindingFlags.NonPublic);
            downsampleField = settingsField?.FieldType.GetField("Downsample", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        }
        catch (Exception) { downsampleField = null; }
        return downsampleField;
    }

    static bool GetSsaoDownsample()
    {
        if (orig?.Ssao == null || SsaoDownsampleField() == null) return false;
        var s = settingsField.GetValue(orig.Ssao);
        return s != null && (bool)downsampleField.GetValue(s);
    }

    static void SetSsaoDownsample(bool on)
    {
        if (orig?.Ssao == null || SsaoDownsampleField() == null) return;
        var s = settingsField.GetValue(orig.Ssao);
        if (s == null || (bool)downsampleField.GetValue(s) == on) return;
        downsampleField.SetValue(s, on);
    }

    // BLOOM LOW: a global override volume above the film look and the zone grades, overriding one parameter only.
    static void SetBloomLow(bool low)
    {
        if (!low)
        {
            if (bloomVolumeObject != null) bloomVolumeObject.SetActive(false);
            return;
        }
        if (bloomVolumeObject == null)
        {
            bloomVolumeObject = new GameObject("Post / quality override");
            UnityEngine.Object.DontDestroyOnLoad(bloomVolumeObject);
            var volume = bloomVolumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 100f;
            bloomProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            bloomProfile.name = "Quality override (runtime)";
            var bloom = bloomProfile.Add<UnityEngine.Rendering.Universal.Bloom>(false);
            bloom.highQualityFiltering.Override(false);
            volume.sharedProfile = bloomProfile;
        }
        bloomVolumeObject.SetActive(true);
    }

    // ------------------------------------------------------------- runner hooks

    internal static void Tick(float unscaledDelta)
    {
        if (unscaledDelta > 0f)
        {
            var fps = 1f / unscaledDelta;
            MeasuredFps = Mathf.Lerp(MeasuredFps, fps, 1f - Mathf.Exp(-unscaledDelta / .5f));
        }
    }

    // Only a handheld's AUTO resolution depends on the screen (a fold, an unfold, a rotation); the desktop keeps its writes.
    internal static void ScreenChanged() { if (Handheld) Apply(); }

    internal static void LowPowerChanged(bool on)
    {
        var active = on && LowPower == 0;
        if (active == LowPowerActive) return;
        LowPowerActive = active;
        Apply();
        Changed?.Invoke();
    }
}

/// <summary>Keeps FrontRoomsQuality current: the screen size (fold, unfold, rotation), Low Power Mode and the measured frame rate.</summary>
public sealed class FrontRoomsQualityRunner : MonoBehaviour
{
    static FrontRoomsQualityRunner instance;
    int lastW, lastH;
    float nextPowerPoll;

    public static void Ensure()
    {
        if (instance != null || !Application.isPlaying) return;
        var go = new GameObject("FrontRooms quality");
        go.hideFlags = HideFlags.HideInHierarchy;
        DontDestroyOnLoad(go);
        instance = go.AddComponent<FrontRoomsQualityRunner>();
    }

    void Update()
    {
        FrontRoomsQuality.Tick(Time.unscaledDeltaTime);
        var pixels = FrontRoomsHandheld.ScreenPixels;
        var w = Mathf.RoundToInt(pixels.x);
        var h = Mathf.RoundToInt(pixels.y);
        if (w != lastW || h != lastH)
        {
            var first = lastW == 0;
            lastW = w; lastH = h;
            if (!first) FrontRoomsQuality.ScreenChanged();
        }
        if (Time.unscaledTime >= nextPowerPoll)
        {
            nextPowerPoll = Time.unscaledTime + 2f;
#if UNITY_IOS && !UNITY_EDITOR
            FrontRoomsQuality.LowPowerChanged(UnityEngine.iOS.Device.lowPowerModeEnabled);
#endif
        }
    }
}
