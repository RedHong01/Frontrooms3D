using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

/// <summary>
/// The facts the handheld (iOS / Android) layer reads about the device: whether
/// the touch layer is on at all, how many pixels make one point, the safe area,
/// tablet or phone, Reduce Motion and haptics.
///
/// <see cref="Active"/> is true only in iOS / Android players. Mac, Windows and
/// WebGL players compile it to false (a WebGL page opened on a phone stays on
/// the desktop path: mobile is its own track). In the Editor it is true only
/// when a touch harness forces a device profile, or when the interactive
/// preview is switched on outside batch mode and outside an autopilot run, so
/// the map chat's autopilot, Relay and capture runs always get the desktop HUD.
/// </summary>
public static class FrontRoomsHandheld
{
    /// <summary>A device the Editor can stand in for: its screen in pixels, points per pixel and safe insets in points.</summary>
    public readonly struct Profile
    {
        public readonly string Name;
        public readonly Vector2Int Pixels;
        public readonly float PointScale;
        public readonly float InsetLeft, InsetRight, InsetTop, InsetBottom;
        public readonly bool Tablet;

        public Profile(string name, int width, int height, float pointScale, float left, float right, float top, float bottom, bool tablet)
        {
            Name = name;
            Pixels = new Vector2Int(width, height);
            PointScale = pointScale;
            InsetLeft = left;
            InsetRight = right;
            InsetTop = top;
            InsetBottom = bottom;
            Tablet = tablet;
        }

        public Vector2 Points => new Vector2(Pixels.x / PointScale, Pixels.y / PointScale);
    }

    /// <summary>iPhone 16 / 17 Pro landscape: 874 × 402 pt at @3x, the Figma phone master (insets L 62, R 62, B 21).</summary>
    public static readonly Profile IPhonePro = new Profile("iPhone 16 Pro", 2622, 1206, 3f, 62f, 62f, 0f, 21f, false);
    /// <summary>A 20:9 Android phone: 914 × 411 dp at 2.625 (punch-hole side and gesture bar insets).</summary>
    public static readonly Profile Android20x9 = new Profile("Android 20:9", 2400, 1080, 2.625f, 32f, 32f, 0f, 20f, false);
    /// <summary>iPad Air 11" landscape: 1180 × 820 pt at @2x (home indicator only).</summary>
    public static readonly Profile IPad11 = new Profile("iPad 11", 2360, 1640, 2f, 0f, 0f, 0f, 20f, true);

    /// <summary>Whether the handheld layer (touch controls, handheld HUD) runs.</summary>
    public static bool Active
    {
        get
        {
#if UNITY_EDITOR
            if (forced) return true;
            return EditorPreviewRequested && !Application.isBatchMode && !EditorAutopilotRunning();
#elif UNITY_IOS || UNITY_ANDROID
            return true;
#else
            return false;
#endif
        }
    }

    /// <summary>Pixels per point (iOS) or per dp (Android). 1 canvas unit of the touch layer is one of these.</summary>
    public static float PointScale
    {
        get
        {
#if UNITY_EDITOR
            if (forced) return forcedProfile.PointScale * forcedPixelRatio;
            var preview = EditorPreviewProfile;
            return preview.PointScale * Mathf.Max(.1f, Screen.height / (float)preview.Pixels.y);
#elif UNITY_IOS
            if (cachedPointScale <= 0f)
            {
                cachedPointScale = FRNativeScale();
                if (cachedPointScale < 1f) cachedPointScale = Mathf.Max(1f, (Screen.dpi > 0f ? Screen.dpi : 326f) / 163f);
            }
            return cachedPointScale;
#elif UNITY_ANDROID
            if (cachedPointScale <= 0f) cachedPointScale = AndroidDensity();
            return cachedPointScale;
#else
            return 1f;
#endif
        }
    }

    /// <summary>The screen in pixels (the capture target when a harness renders off screen).</summary>
    public static Vector2 ScreenPixels
    {
        get
        {
#if UNITY_EDITOR
            if (forced) return forcedPixels;
#endif
            return new Vector2(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height));
        }
    }

    /// <summary>The safe area in pixels, origin bottom-left, as <see cref="Screen.safeArea"/> reports it.</summary>
    public static Rect SafeAreaPixels
    {
        get
        {
#if UNITY_EDITOR
            if (forced) return InsetRect(forcedProfile, forcedPixels, PointScale);
            if (Active) return InsetRect(EditorPreviewProfile, ScreenPixels, PointScale);
#endif
            var area = Screen.safeArea;
            if (area.width <= 0f || area.height <= 0f) area = new Rect(0f, 0f, Screen.width, Screen.height);
            return area;
        }
    }

    /// <summary>The screen in points.</summary>
    public static Vector2 ScreenPoints => ScreenPixels / Mathf.Max(.01f, PointScale);

    /// <summary>Tablet grip: the touch controls start larger (Xbox Accessibility Guidelines 107: 24 mm on tablets).</summary>
    public static bool IsTablet
    {
        get
        {
#if UNITY_EDITOR
            if (forced) return forcedProfile.Tablet;
            return EditorPreviewProfile.Tablet;
#elif UNITY_IOS
            return FRIsPad();
#elif UNITY_ANDROID
            // Android's tablet line: smallest width of 600 dp.
            var dp = Mathf.Min(Screen.width, Screen.height) / Mathf.Max(.01f, PointScale);
            return dp >= 600f;
#else
            return false;
#endif
        }
    }

    /// <summary>The controls' starting size: 1.4× on tablets, 1× on phones (the player's CONTROLS SIZE multiplies it).</summary>
    public static float DeviceControlsScale => IsTablet ? 1.4f : 1f;

    /// <summary>
    /// Motion should be reduced: the OS asks for it (iOS Reduce Motion, Android
    /// "remove animations"), or CAMERA MOTION is OFF. Transitions then become
    /// short opacity changes with no slide, scale or overshoot.
    /// </summary>
    public static bool ReduceMotion => OsReduceMotion || FrontRoomsSettings.CameraMotionPercent <= 0;

    /// <summary>The operating system's own reduce-motion switch.</summary>
    public static bool OsReduceMotion
    {
        get
        {
#if UNITY_EDITOR
            return EditorReduceMotion;
#elif UNITY_IOS
            return FRReduceMotion();
#elif UNITY_ANDROID
            return AndroidAnimationsOff();
#else
            return false;
#endif
        }
    }

    /// <summary>Whether the device can play haptics at all (no iPad can).</summary>
    public static bool HapticsSupported
    {
        get
        {
#if UNITY_EDITOR
            return !IsTablet;
#elif UNITY_IOS
            return !FRIsPad();
#elif UNITY_ANDROID
            return true;
#else
            return false;
#endif
        }
    }

    static Rect InsetRect(Profile profile, Vector2 pixels, float pointScale)
    {
        var left = profile.InsetLeft * pointScale;
        var right = profile.InsetRight * pointScale;
        var top = profile.InsetTop * pointScale;
        var bottom = profile.InsetBottom * pointScale;
        return new Rect(left, bottom, Mathf.Max(1f, pixels.x - left - right), Mathf.Max(1f, pixels.y - top - bottom));
    }

#if UNITY_IOS && !UNITY_EDITOR
    static float cachedPointScale;
    [DllImport("__Internal")] static extern float FRNativeScale();
    // ints, not bools: a C bool return marshals unreliably.
    [DllImport("__Internal")] static extern int FRReduceMotionNative();
    [DllImport("__Internal")] static extern int FRIsPadNative();
    static bool FRReduceMotion() => FRReduceMotionNative() != 0;
    static bool FRIsPad() => FRIsPadNative() != 0;
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
    static float cachedPointScale;
    static float lastAnimationCheck = -10f;
    static bool animationsOff;

    static float AndroidDensity()
    {
        try
        {
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var resources = activity.Call<AndroidJavaObject>("getResources"))
            using (var metrics = resources.Call<AndroidJavaObject>("getDisplayMetrics"))
            {
                var density = metrics.Get<float>("density");
                if (density > .5f) return density;
            }
        }
        catch (System.Exception) { }
        return Mathf.Max(1f, (Screen.dpi > 0f ? Screen.dpi : 160f) / 160f);
    }

    static bool AndroidAnimationsOff()
    {
        // Settings.Global.ANIMATOR_DURATION_SCALE == 0 is Android's "remove animations". Checked at most once a second.
        if (Time.unscaledTime - lastAnimationCheck < 1f) return animationsOff;
        lastAnimationCheck = Time.unscaledTime;
        try
        {
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var resolver = activity.Call<AndroidJavaObject>("getContentResolver"))
            using (var global = new AndroidJavaClass("android.provider.Settings$Global"))
                animationsOff = global.CallStatic<float>("getFloat", resolver, "animator_duration_scale", 1f) <= 0f;
        }
        catch (System.Exception) { animationsOff = false; }
        return animationsOff;
    }
#endif

#if UNITY_EDITOR
    static bool forced;
    static Profile forcedProfile;
    static Vector2 forcedPixels;
    static float forcedPixelRatio = 1f;

    /// <summary>Set by the Editor menu (FrontRooms 3D / Mobile / Touch preview): the interactive Play Mode preview.</summary>
    public static bool EditorPreviewRequested;
    /// <summary>The device the interactive preview stands in for.</summary>
    public static Profile EditorPreviewProfile = IPhonePro;
    /// <summary>Stand-in for the OS Reduce Motion switch in the Editor.</summary>
    public static bool EditorReduceMotion;

    /// <summary>A harness takes the handheld path for its own run: the profile's screen, safe area and point scale, rendered at <paramref name="renderPixels"/> (the profile's pixels when null).</summary>
    public static void Force(Profile profile, Vector2Int? renderPixels = null)
    {
        forced = true;
        forcedProfile = profile;
        var pixels = renderPixels ?? profile.Pixels;
        forcedPixels = new Vector2(pixels.x, pixels.y);
        forcedPixelRatio = pixels.y / (float)profile.Pixels.y;
    }

    public static void ClearForce()
    {
        forced = false;
        forcedPixelRatio = 1f;
    }

    public static bool Forced => forced;
    public static Profile ForcedProfile => forcedProfile;

    // The map chat's autopilot (FrontRoomsMainScenePlaytest) keeps the desktop path, even with the preview on.
    static bool EditorAutopilotRunning()
    {
        if (UnityEditor.SessionState.GetBool(FrontRooms3DGame.AutopilotKey, false)) return true;
        foreach (var arg in System.Environment.GetCommandLineArgs())
            if (arg.StartsWith("-autopilot", System.StringComparison.Ordinal)) return true;
        return false;
    }
#endif
}
