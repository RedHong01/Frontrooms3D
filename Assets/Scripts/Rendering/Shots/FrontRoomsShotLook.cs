using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// V4 + V5 (D1.6, 10_design §8; door_shots_plan §4): one driver for the door and glass shots' look, self-installed in
/// Play Mode (a hidden host, as FrontRoomsInspectLook's), no Game.cs line.
/// - "Post / shot Unlock" (priority 2), weight = FrontRoomsDoorShot.Weight: vignette +0.10, postExposure +0.15, and a
///   Gaussian far blur (start = lock depth + 0.12 m, end = start + 2.5 m, max radius 1.0) where
///   FrontRoomsInspectLook.DofSupported (desktop; off on WebGL, iOS, Android). No motion blur anywhere.
/// - "Post / shot Glass" (priority 2), weight = FrontRoomsGlassShot.Weight: vignette +GlassBreak.Vignette (0.08) and a
///   chromatic-aberration pulse of +GlassBreak.CaPulsePeak (0.16) over GlassBreak.CaPulse (0.25 s) on the shatter beat
///   (GlassShot.Beat 3); no pulse with Reduce flashing.
/// - LOD0 forced on the unlock shot's door from FrontRoomsCameraRig.ShotStarted to ShotEnded (all platforms).
/// - V5: one unshadowed point light on the view camera, Unlock.ShotLightIntensity (0.6) x DoorShot.Weight, range 1.2 m,
///   neutral warm white, only while an unlock shot lives. Compiled out on WebGL (its 32-light cap).
/// Profiles are runtime instances (DontSave); values are base + delta, the base read from the resolved stack when a shot
/// starts with these volumes at weight 0. No per-frame allocation.
/// </summary>
public static class FrontRoomsShotLook
{
    public const float UnlockVignette = .10f, UnlockExposure = .15f, UnlockBlurBeyondLock = .12f, UnlockBlurDepth = 2.5f, UnlockBlurRadius = 1f;
    public const float LockDepthDefault = .58f;
    public const float ShotLightRange = 1.2f;
    public static readonly Color ShotLightColor = new Color(1f, .94f, .86f);
    /// <summary>Within this of the shot's focus a dressed door is the shot's door (LOD0 force).</summary>
    public const float ShotDoorReach = 1.6f;

    static GameObject root;
    static Volume unlockVolume, glassVolume;
    static VolumeProfile unlockProfile, glassProfile;
    static Vignette unlockVignette, glassVignette;
    static ColorAdjustments unlockColor;
    static DepthOfField unlockDof;
    static ChromaticAberration glassChroma;
    static float baseVignette, baseExposure, baseChroma;
    static float caClock = -1f;
    static FrontRoomsDoorRig forcedRig;
    static bool unlockLive;
#if !(UNITY_WEBGL && !UNITY_EDITOR)
    static Light shotLight;
#endif

    public static float UnlockWeight => unlockVolume != null ? unlockVolume.weight : 0f;
    public static float GlassWeight => glassVolume != null ? glassVolume.weight : 0f;
    public static float ShotLightIntensityNow
    {
        get
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return 0f;
#else
            return shotLight != null && shotLight.enabled ? shotLight.intensity : 0f;
#endif
        }
    }
    public static FrontRoomsDoorRig ForcedRig => forcedRig;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (root != null) return;
        root = new GameObject("Post / shot look") { hideFlags = HideFlags.HideAndDontSave };
        Object.DontDestroyOnLoad(root);
        root.AddComponent<FrontRoomsShotLookHost>();
        unlockProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        unlockProfile.hideFlags = HideFlags.DontSave;
        unlockVignette = unlockProfile.Add<Vignette>(false);
        unlockColor = unlockProfile.Add<ColorAdjustments>(false);
        if (FrontRoomsInspectLook.DofSupported)
        {
            unlockDof = unlockProfile.Add<DepthOfField>(false);
            unlockDof.mode.Override(DepthOfFieldMode.Gaussian);
            unlockDof.gaussianMaxRadius.Override(UnlockBlurRadius);
            unlockDof.highQualitySampling.Override(true);
        }
        glassProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        glassProfile.hideFlags = HideFlags.DontSave;
        glassVignette = glassProfile.Add<Vignette>(false);
        glassChroma = glassProfile.Add<ChromaticAberration>(false);
        unlockVolume = MakeVolume("Post / shot Unlock", unlockProfile);
        glassVolume = MakeVolume("Post / shot Glass", glassProfile);
        FrontRoomsCameraRig.ShotStarted += OnShotStarted;
        FrontRoomsCameraRig.ShotEnded += OnShotEnded;
        FrontRoomsGlassShot.Beat += OnGlassBeat;
        // Leaving Play Mode in the Editor (and quitting a player): DontSave objects outlive Play Mode, so take the volumes,
        // the profiles and the light down here; a volume left at a shot's weight would otherwise tint the next session.
        Application.quitting -= Shutdown;
        Application.quitting += Shutdown;
    }

    static void Shutdown()
    {
        Application.quitting -= Shutdown;
        if (unlockVolume != null) unlockVolume.weight = 0f;
        if (glassVolume != null) glassVolume.weight = 0f;
#if !(UNITY_WEBGL && !UNITY_EDITOR)
        if (shotLight != null) Object.Destroy(shotLight.gameObject);
        shotLight = null;
#endif
        var r = root;
        Uninstall();
        if (r != null) Object.Destroy(r);
        if (unlockProfile != null) Object.Destroy(unlockProfile);
        if (glassProfile != null) Object.Destroy(glassProfile);
        unlockVolume = glassVolume = null;
        unlockProfile = glassProfile = null;
    }

    internal static void Uninstall()
    {
        FrontRoomsCameraRig.ShotStarted -= OnShotStarted;
        FrontRoomsCameraRig.ShotEnded -= OnShotEnded;
        FrontRoomsGlassShot.Beat -= OnGlassBeat;
        ReleaseLod();
        root = null;
    }

    static Volume MakeVolume(string name, VolumeProfile profile)
    {
        var go = new GameObject(name) { hideFlags = HideFlags.DontSave };
        go.transform.SetParent(root.transform, false);
        var v = go.AddComponent<Volume>();
        v.isGlobal = true;
        v.priority = 2f;
        v.weight = 0f;
        v.profile = profile;
        return v;
    }

    static void Snapshot()
    {
        var stack = VolumeManager.instance.stack;
        if (stack == null) return;
        var v = stack.GetComponent<Vignette>();
        var a = stack.GetComponent<ColorAdjustments>();
        var c = stack.GetComponent<ChromaticAberration>();
        baseVignette = v != null ? v.intensity.value : 0f;
        baseExposure = a != null ? a.postExposure.value : 0f;
        baseChroma = c != null ? c.intensity.value : 0f;
    }

    static void OnShotStarted(ShotKind kind, Vector3 focus)
    {
        if (kind != ShotKind.Unlock && kind != ShotKind.Glass) return;
        if (UnlockWeight <= 0f && GlassWeight <= 0f) Snapshot();
        if (kind == ShotKind.Glass)
        {
            glassVignette.intensity.Override(Mathf.Clamp01(baseVignette + FrontRoomsShotTimings.GlassBreak.Vignette));
            glassChroma.intensity.Override(baseChroma);
            return;
        }
        unlockLive = true;
        unlockVignette.intensity.Override(Mathf.Clamp01(baseVignette + UnlockVignette));
        unlockColor.postExposure.Override(baseExposure + UnlockExposure);
        if (unlockDof != null)
        {
            var cam = FrontRoomsHeldItem.ViewCamera;
            var lockDepth = cam != null ? Mathf.Max(.2f, Vector3.Distance(cam.transform.position, focus)) : LockDepthDefault;
            // The camera dips to pose P (0.58 m from the lock); at ShotStarted it is still at the eye, so use the pose's distance.
            lockDepth = Mathf.Min(lockDepth, LockDepthDefault);
            unlockDof.gaussianStart.Override(lockDepth + UnlockBlurBeyondLock);
            unlockDof.gaussianEnd.Override(lockDepth + UnlockBlurBeyondLock + UnlockBlurDepth);
        }
        ReleaseLod();
        forcedRig = FrontRoomsDoorRig.Nearest(focus, ShotDoorReach);
        if (forcedRig != null) forcedRig.ForceLod0(true);
    }

    static void OnShotEnded(ShotKind kind, Vector3 focus)
    {
        if (kind != ShotKind.Unlock) return;
        unlockLive = false;
        ReleaseLod();
    }

    static void ReleaseLod()
    {
        if (forcedRig != null) forcedRig.ForceLod0(false);
        forcedRig = null;
    }

    static void OnGlassBeat(int beat, float strength)
    {
        if (beat != 3 || FrontRoomsSettings.ReduceFlashing) return;
        caClock = 0f;
    }

    /// <summary>The host's LateUpdate: weights, the CA pulse and the shot light.</summary>
    internal static void Tick(float dt)
    {
        if (unlockVolume == null) return;
        var w = Mathf.Clamp01(FrontRoomsDoorShot.Weight);
        unlockVolume.weight = w;
        var g = Mathf.Clamp01(FrontRoomsGlassShot.Weight);
        glassVolume.weight = g;
        if (caClock >= 0f)
        {
            caClock += dt;
            var u = Mathf.Clamp01(caClock / FrontRoomsShotTimings.GlassBreak.CaPulse);
            var k = u < .25f ? u / .25f : 1f - (u - .25f) / .75f;
            glassChroma.intensity.Override(Mathf.Clamp01(baseChroma + FrontRoomsShotTimings.GlassBreak.CaPulsePeak * k * k));
            if (u >= 1f) { caClock = -1f; glassChroma.intensity.Override(baseChroma); }
        }
#if !(UNITY_WEBGL && !UNITY_EDITOR)
        var on = unlockLive && w > 0f;
        if (on)
        {
            var cam = FrontRoomsHeldItem.ViewCamera;
            if (cam != null)
            {
                if (shotLight == null)
                {
                    var go = new GameObject("Shot light (unlock)") { hideFlags = HideFlags.DontSave };
                    shotLight = go.AddComponent<Light>();
                    shotLight.type = LightType.Point;
                    shotLight.shadows = LightShadows.None;
                    shotLight.range = ShotLightRange;
                    shotLight.color = ShotLightColor;
                    shotLight.renderMode = LightRenderMode.ForcePixel;
                }
                if (shotLight.transform.parent != cam.transform) shotLight.transform.SetParent(cam.transform, false);
                shotLight.transform.localPosition = Vector3.zero;
                shotLight.intensity = FrontRoomsShotTimings.Unlock.ShotLightIntensity * w;
                if (!shotLight.enabled) shotLight.enabled = true;
            }
        }
        else if (shotLight != null && shotLight.enabled) shotLight.enabled = false;
#endif
    }
}
