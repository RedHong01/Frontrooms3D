using UnityEngine;

/// <summary>
/// Small platform boundary for mobile tactile feedback.
///
/// The first implementation intentionally uses Unity's built-in
/// <see cref="Handheld.Vibrate"/> primitive.  It gives iOS and Android a safe
/// baseline without adding a native plugin, while keeping the call sites
/// independent from the eventual native haptics implementation.
/// </summary>
public static class FrontRoomsMobileHaptics
{
    public enum Pulse
    {
        UsePressed,
        UseReleased,
        SprintLatched,
        SprintReleased,
        GlassBeat,
        GlassShattered,
        Caught,
        MenuConfirm,
        Invalid
    }

    // Haptics are a player preference.  The Settings layer can bind this to
    // its HAPTICS toggle later; keeping the preference here also lets a
    // platform-specific settings bootstrap disable it before gameplay starts.
    public static bool Enabled { get; set; } = true;

    // Avoid repeatedly waking the device when a held glass interaction emits
    // several beat callbacks in one frame or when a touch is noisy at release.
    public static float MinimumIntervalSeconds { get; set; } = 0.055f;

    static float lastPulseTime = float.NegativeInfinity;

    /// <summary>True on the two supported mobile player targets.</summary>
    public static bool IsMobileTarget
    {
        get
        {
#if UNITY_IOS || UNITY_ANDROID
            return true;
#else
            return false;
#endif
        }
    }

    /// <summary>Whether this adapter is able to issue a device pulse.</summary>
    public static bool IsAvailable => IsMobileTarget && Enabled;

    /// <summary>
    /// Sends one coarse device pulse. Unity's built-in API has no intensity or
    /// duration control; the enum remains semantic so a richer native adapter
    /// can be introduced later without changing gameplay call sites.
    /// </summary>
    public static void Play(Pulse pulse)
    {
        if (!Enabled || !IsMobileTarget || pulse == Pulse.Invalid)
            return;

        var now = Time.unscaledTime;
        if (now - lastPulseTime < Mathf.Max(0f, MinimumIntervalSeconds))
            return;

        lastPulseTime = now;
#if UNITY_IOS || UNITY_ANDROID
        Handheld.Vibrate();
#endif
    }

    public static void UsePressed() => Play(Pulse.UsePressed);
    public static void UseReleased() => Play(Pulse.UseReleased);
    public static void SprintLatched() => Play(Pulse.SprintLatched);
    public static void SprintReleased() => Play(Pulse.SprintReleased);
    public static void GlassBeat() => Play(Pulse.GlassBeat);
    public static void GlassShattered() => Play(Pulse.GlassShattered);
    public static void Caught() => Play(Pulse.Caught);
    public static void MenuConfirm() => Play(Pulse.MenuConfirm);

    /// <summary>Reset cooldown state when a new play session is entered.</summary>
    public static void Reset()
    {
        lastPulseTime = float.NegativeInfinity;
    }
}
