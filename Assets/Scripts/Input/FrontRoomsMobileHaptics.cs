using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

/// <summary>
/// Haptics for the handheld build (Documentation/TOUCH_CONTROLS.md §7, Figma TC12).
///
/// Each moment plays a short, specific pattern instead of one long buzz:
/// a light tick when the sprint socket latches, two firm ticks on a locked
/// door's rattle jolts, a buzz that rises while glass is held, a heavy hit when
/// it breaks. Two switches, like Alien: Isolation's: gameplay (the world) and
/// controls (the sprint socket, USE and menu presses).
///
/// iOS uses UIImpactFeedbackGenerator through FrontRoomsHandheldNative.mm;
/// Android uses VibrationEffect's predefined clicks (API 29+) or a short one-shot
/// (API 26+). iPads have no haptic engine, so nothing plays there: every haptic
/// also exists as sound or picture. Desktop and WebGL never play.
/// </summary>
public static class FrontRoomsMobileHaptics
{
    public enum Channel { Gameplay, Controls }

    public enum Style { Selection, Light, Medium, Heavy, Rigid, Soft }

    public struct Beat
    {
        public float Delay;
        public Style Style;
        public float Intensity;

        public Beat(float delay, Style style, float intensity)
        {
            Delay = delay;
            Style = style;
            Intensity = intensity;
        }
    }

    public static bool GameplayEnabled { get; set; } = true;
    public static bool ControlsEnabled { get; set; } = true;

    /// <summary>Raised for every pulse that would play (also in the Editor harness, where nothing vibrates).</summary>
    public static event Action<Channel, Style, float> Played;

    /// <summary>Pulses closer than this on one channel merge (a noisy release, several glass beats in a frame).</summary>
    public const float MinimumInterval = .03f;

    static readonly float[] lastPulse = { -10f, -10f };

    /// <summary>True when a pulse can be felt: a handheld with a haptic engine.</summary>
    public static bool IsAvailable => FrontRoomsHandheld.Active && FrontRoomsHandheld.HapticsSupported;

    public static void Play(Channel channel, Style style, float intensity = 1f)
    {
        if (!FrontRoomsHandheld.Active) return;
        if (channel == Channel.Gameplay ? !GameplayEnabled : !ControlsEnabled) return;
        var now = Time.unscaledTime;
        var i = (int)channel;
        if (now - lastPulse[i] < MinimumInterval) return;
        lastPulse[i] = now;
        intensity = Mathf.Clamp01(intensity);
        Played?.Invoke(channel, style, intensity);
        if (!FrontRoomsHandheld.HapticsSupported) return;
        Native(style, intensity);
    }

    /// <summary>Plays beats at their delays (unscaled time: a pause does not stretch a pattern already started).</summary>
    public static void Pattern(Channel channel, params Beat[] beats)
    {
        if (!FrontRoomsHandheld.Active || beats == null || beats.Length == 0) return;
        Runner.Queue(channel, beats);
    }

    // ------------------------------------------------------------- moments
    // The touch layer and FrontRooms3DGame.Mobile.cs call these from the game's own events.

    public static void UsePressed() => Play(Channel.Controls, Style.Rigid, .55f);
    public static void SprintLatched() => Play(Channel.Controls, Style.Light, .6f);
    public static void Winded() => Pattern(Channel.Controls, new Beat(0f, Style.Soft, .5f), new Beat(.12f, Style.Soft, .35f));
    public static void MenuConfirm() => Play(Channel.Controls, Style.Selection);
    public static void DoorLatched() => Play(Channel.Gameplay, Style.Light, .5f);
    /// <summary>Two firm ticks on the camera's two rattle jolts.</summary>
    public static void DoorRattle(float jolt1, float jolt2) =>
        Pattern(Channel.Gameplay, new Beat(jolt1, Style.Rigid, .8f), new Beat(jolt2, Style.Rigid, .8f));
    /// <summary>The held-glass buzz: light ticks that rise from 0.2 to 0.6 with the hold.</summary>
    public static void GlassBeat(float progress) => Play(Channel.Gameplay, Style.Light, Mathf.Lerp(.2f, .6f, Mathf.Clamp01(progress)));
    public static void GlassCracked(int stage) => Play(Channel.Gameplay, Style.Medium, stage >= 2 ? .8f : .7f);
    public static void GlassShattered() => Play(Channel.Gameplay, Style.Heavy, 1f);
    /// <summary>The Relay sees you: one firm double pulse with the lock-on cue.</summary>
    public static void LockOn() => Pattern(Channel.Gameplay, new Beat(0f, Style.Medium, .75f), new Beat(.15f, Style.Medium, .75f));
    /// <summary>Caught: one long heavy fade.</summary>
    public static void Caught() => Pattern(Channel.Gameplay,
        new Beat(0f, Style.Heavy, 1f), new Beat(.18f, Style.Medium, .65f), new Beat(.4f, Style.Soft, .4f), new Beat(.65f, Style.Soft, .2f));

    public static void Reset()
    {
        lastPulse[0] = lastPulse[1] = -10f;
        Runner.Clear();
    }

    // -------------------------------------------------------------- native

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")] static extern void FRHapticImpact(int style, float intensity);
    [DllImport("__Internal")] static extern void FRHapticSelection();

    static void Native(Style style, float intensity)
    {
        switch (style)
        {
            case Style.Selection: FRHapticSelection(); break;
            case Style.Light: FRHapticImpact(0, intensity); break;
            case Style.Medium: FRHapticImpact(1, intensity); break;
            case Style.Heavy: FRHapticImpact(2, intensity); break;
            case Style.Soft: FRHapticImpact(3, intensity); break;
            case Style.Rigid: FRHapticImpact(4, intensity); break;
        }
    }
#elif UNITY_ANDROID && !UNITY_EDITOR
    static AndroidJavaObject vibrator;
    static AndroidJavaClass vibrationEffect;
    static int sdk = -1;
    static bool amplitudeControl;

    // Never called: the reference alone makes Unity's Android build add the VIBRATE permission.
    // Handheld.Vibrate itself is a long buzz, which this design never plays (TOUCH_CONTROLS §7).
    [UnityEngine.Scripting.Preserve] static void VibratePermissionAnchor() => Handheld.Vibrate();

    static void Native(Style style, float intensity)
    {
        try
        {
            if (sdk < 0)
            {
                using (var version = new AndroidJavaClass("android.os.Build$VERSION")) sdk = version.GetStatic<int>("SDK_INT");
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                    vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                if (sdk >= 26) vibrationEffect = new AndroidJavaClass("android.os.VibrationEffect");
                if (sdk >= 26 && vibrator != null) amplitudeControl = vibrator.Call<bool>("hasAmplitudeControl");
            }
            if (vibrator == null) return;
            if (sdk >= 29)
            {
                // EFFECT_CLICK 0, EFFECT_TICK 2, EFFECT_HEAVY_CLICK 5.
                var effect = style == Style.Heavy ? 5 : style == Style.Medium || style == Style.Rigid ? 0 : 2;
                using (var e = vibrationEffect.CallStatic<AndroidJavaObject>("createPredefined", effect)) vibrator.Call("vibrate", e);
            }
            else if (sdk >= 26 && amplitudeControl)
            {
                var ms = style == Style.Heavy ? 40L : style == Style.Medium || style == Style.Rigid ? 22L : 12L;
                var amplitude = Mathf.Clamp(Mathf.RoundToInt(intensity * 255f), 1, 255);
                using (var e = vibrationEffect.CallStatic<AndroidJavaObject>("createOneShot", ms, amplitude)) vibrator.Call("vibrate", e);
            }
            // Older or on/off-only motors: no haptic rather than a buzz; the ring, bar and sound still say it.
        }
        catch (Exception)
        {
            vibrator = null;
        }
    }
#else
    static void Native(Style style, float intensity) { }
#endif

    /// <summary>Plays patterns on unscaled time; lives only while a pattern is queued.</summary>
    sealed class Runner : MonoBehaviour
    {
        static Runner instance;
        readonly List<(float at, Channel channel, Beat beat)> queue = new List<(float, Channel, Beat)>();

        public static void Queue(Channel channel, Beat[] beats)
        {
            if (instance == null)
            {
                var go = new GameObject("FrontRooms haptics") { hideFlags = HideFlags.HideAndDontSave };
                DontDestroyOnLoad(go);
                instance = go.AddComponent<Runner>();
            }
            var now = Time.unscaledTime;
            foreach (var b in beats) instance.queue.Add((now + Mathf.Max(0f, b.Delay), channel, b));
        }

        public static void Clear()
        {
            if (instance != null) instance.queue.Clear();
        }

        void Update()
        {
            var now = Time.unscaledTime;
            for (var i = queue.Count - 1; i >= 0; i--)
            {
                if (queue[i].at > now) continue;
                var (_, channel, beat) = queue[i];
                queue.RemoveAt(i);
                Play(channel, beat.Style, beat.Intensity);
            }
        }
    }
}
