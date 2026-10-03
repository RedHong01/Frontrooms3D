using System;
using UnityEngine;

/// <summary>
/// The player's comfort, assist and input settings (the pause screen's
/// settings panel), remembered across runs in PlayerPrefs. Static, so the
/// camera rig, the post stack, the lamps and the captions can read them
/// without a game reference. Load() runs once in FrontRooms3DGame.Awake.
/// </summary>
public static class FrontRoomsSettings
{
    const string CameraMotionKey = "FrontRooms.Comfort.CameraMotion";
    const string ReduceFlashingKey = "FrontRooms.Comfort.ReduceFlashing";
    const string TapToBreakKey = "FrontRooms.Input.TapToBreak";
    const string CaptionsKey = "FrontRooms.Assist.Captions";
    const string RelayReadoutKey = "FrontRooms.Assist.RelayReadout";

    /// <summary>Camera motion in shots and shakes: 0 (off: takeovers play in place, look stays free), 50 or 100.</summary>
    public static int CameraMotionPercent { get; private set; } = 100;
    public static float CameraMotionScale => CameraMotionPercent / 100f;
    /// <summary>No exposure flashes or colour pulses; slower lamp stutter where the lamps read it.</summary>
    public static bool ReduceFlashing { get; private set; }
    /// <summary>Glass breaks with taps of E instead of a hold (never faster than the hold).</summary>
    public static bool TapToBreak { get; private set; }
    /// <summary>Captions for sounds that matter (read by the captions when they land).</summary>
    public static bool Captions { get; private set; }
    /// <summary>The Relay's state and distance on the HUD: an assist, off by default (Red, 2026-10-03).</summary>
    public static bool RelayReadout { get; private set; }

    /// <summary>Raised after any setting changes.</summary>
    public static event Action Changed;

    public static void Load()
    {
        CameraMotionPercent = Snap(PlayerPrefs.GetInt(CameraMotionKey, 100));
        ReduceFlashing = PlayerPrefs.GetInt(ReduceFlashingKey, 0) != 0;
        TapToBreak = PlayerPrefs.GetInt(TapToBreakKey, 0) != 0;
        Captions = PlayerPrefs.GetInt(CaptionsKey, 0) != 0;
        RelayReadout = PlayerPrefs.GetInt(RelayReadoutKey, 0) != 0;
    }

    /// <summary>The next camera motion step (off, 50 %, 100 %): −1 / +1 stop at the ends, 2 cycles (100 % → off).</summary>
    public static void StepCameraMotion(int direction)
    {
        var steps = new[] { 0, 50, 100 };
        var i = Array.IndexOf(steps, CameraMotionPercent);
        i = (i < 0 ? 2 : i) + (direction >= 0 ? 1 : -1);
        i = direction > 1 ? i % steps.Length : Mathf.Clamp(i, 0, steps.Length - 1);
        CameraMotionPercent = steps[i];
        Save(CameraMotionKey, CameraMotionPercent);
    }

    public static void SetReduceFlashing(bool on) { ReduceFlashing = on; Save(ReduceFlashingKey, on ? 1 : 0); }
    public static void SetTapToBreak(bool on) { TapToBreak = on; Save(TapToBreakKey, on ? 1 : 0); }
    public static void SetCaptions(bool on) { Captions = on; Save(CaptionsKey, on ? 1 : 0); }
    public static void SetRelayReadout(bool on) { RelayReadout = on; Save(RelayReadoutKey, on ? 1 : 0); }

    static int Snap(int percent) => percent <= 25 ? 0 : percent < 75 ? 50 : 100;

    static void Save(string key, int value)
    {
        PlayerPrefs.SetInt(key, value);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
