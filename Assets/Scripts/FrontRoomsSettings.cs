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
    const string TouchScaleKey = "FrontRooms.Touch.Scale";
    const string TouchOpacityKey = "FrontRooms.Touch.Opacity";
    const string TouchLeftHandedKey = "FrontRooms.Touch.LeftHanded";
    const string TouchLookSpeedKey = "FrontRooms.Touch.LookSpeed";
    const string TouchInvertLookKey = "FrontRooms.Touch.InvertLook";
    const string TouchFloatingStickKey = "FrontRooms.Touch.FloatingStick";
    const string TouchSprintSocketKey = "FrontRooms.Touch.SprintSocket";
    const string TouchHapticsKey = "FrontRooms.Touch.Haptics";
    const string TouchHapticsControlsKey = "FrontRooms.Touch.HapticsControls";
    const string TouchGyroKey = "FrontRooms.Touch.Gyro";

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

    /// <summary>Mobile touch preferences, kept here so scene reloads preserve the player's layout.</summary>
    public static int TouchControlsScalePercent { get; private set; } = 100;
    public static int TouchOpacityPercent { get; private set; } = 34;
    public static bool TouchLeftHanded { get; private set; }
    public static int TouchLookSpeedPercent { get; private set; } = 100;
    public static bool TouchInvertLook { get; private set; }
    public static bool TouchFloatingStick { get; private set; } = true;
    public static bool TouchSprintSocket { get; private set; } = true;
    /// <summary>Gameplay haptics (doors, glass, the Relay's lock-on, caught).</summary>
    public static bool TouchHaptics { get; private set; } = true;
    /// <summary>Control haptics (the sprint socket latching, USE and button presses); a separate switch, like Alien: Isolation's.</summary>
    public static bool TouchHapticsControls { get; private set; } = true;
    /// <summary>0 off, 1 while a look/use finger is down, 2 always.</summary>
    public static int TouchGyroMode { get; private set; }

    /// <summary>Raised after any setting changes.</summary>
    public static event Action Changed;

    public static void Load()
    {
        // First launch on a handheld with the OS Reduce Motion switch on starts CAMERA MOTION at off (desktop always starts at 100).
        CameraMotionPercent = Snap(PlayerPrefs.GetInt(CameraMotionKey, FrontRoomsHandheld.Active && FrontRoomsHandheld.OsReduceMotion ? 0 : 100));
        ReduceFlashing = PlayerPrefs.GetInt(ReduceFlashingKey, 0) != 0;
        TapToBreak = PlayerPrefs.GetInt(TapToBreakKey, 0) != 0;
        Captions = PlayerPrefs.GetInt(CaptionsKey, 0) != 0;
        RelayReadout = PlayerPrefs.GetInt(RelayReadoutKey, 0) != 0;
        TouchControlsScalePercent = SnapTouchScale(PlayerPrefs.GetInt(TouchScaleKey, 100));
        TouchOpacityPercent = SnapTouchOpacity(PlayerPrefs.GetInt(TouchOpacityKey, 34));
        TouchLeftHanded = PlayerPrefs.GetInt(TouchLeftHandedKey, 0) != 0;
        TouchLookSpeedPercent = SnapTouchLookSpeed(PlayerPrefs.GetInt(TouchLookSpeedKey, 100));
        TouchInvertLook = PlayerPrefs.GetInt(TouchInvertLookKey, 0) != 0;
        TouchFloatingStick = PlayerPrefs.GetInt(TouchFloatingStickKey, 1) != 0;
        TouchSprintSocket = PlayerPrefs.GetInt(TouchSprintSocketKey, 1) != 0;
        TouchHaptics = PlayerPrefs.GetInt(TouchHapticsKey, 1) != 0;
        TouchHapticsControls = PlayerPrefs.GetInt(TouchHapticsControlsKey, 1) != 0;
        TouchGyroMode = Mathf.Clamp(PlayerPrefs.GetInt(TouchGyroKey, 0), 0, 2);
        FrontRoomsMobileHaptics.GameplayEnabled = TouchHaptics;
        FrontRoomsMobileHaptics.ControlsEnabled = TouchHapticsControls;
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

    public static void StepTouchControlsScale(int direction)
    {
        TouchControlsScalePercent = Step(TouchControlsScalePercent, new[] { 80, 100, 120, 140 }, direction);
        Save(TouchScaleKey, TouchControlsScalePercent);
    }

    public static void StepTouchOpacity(int direction)
    {
        TouchOpacityPercent = Step(TouchOpacityPercent, new[] { 20, 34, 50, 65 }, direction);
        Save(TouchOpacityKey, TouchOpacityPercent);
    }

    public static void SetTouchLeftHanded(bool on) { TouchLeftHanded = on; Save(TouchLeftHandedKey, on ? 1 : 0); }

    public static void StepTouchLookSpeed(int direction)
    {
        TouchLookSpeedPercent = Step(TouchLookSpeedPercent, new[] { 60, 80, 100, 120, 150 }, direction);
        Save(TouchLookSpeedKey, TouchLookSpeedPercent);
    }

    public static void SetTouchInvertLook(bool on) { TouchInvertLook = on; Save(TouchInvertLookKey, on ? 1 : 0); }
    public static void SetTouchFloatingStick(bool on) { TouchFloatingStick = on; Save(TouchFloatingStickKey, on ? 1 : 0); }
    public static void SetTouchSprintSocket(bool on) { TouchSprintSocket = on; Save(TouchSprintSocketKey, on ? 1 : 0); }

    public static void SetTouchHaptics(bool on)
    {
        TouchHaptics = on;
        FrontRoomsMobileHaptics.GameplayEnabled = on;
        Save(TouchHapticsKey, on ? 1 : 0);
    }

    public static void SetTouchHapticsControls(bool on)
    {
        TouchHapticsControls = on;
        FrontRoomsMobileHaptics.ControlsEnabled = on;
        Save(TouchHapticsControlsKey, on ? 1 : 0);
    }

    public static void StepTouchGyro(int direction)
    {
        TouchGyroMode = (TouchGyroMode + (direction > 1 ? 1 : direction >= 0 ? 1 : -1) + 3) % 3;
        Save(TouchGyroKey, TouchGyroMode);
    }

    static int Snap(int percent) => percent <= 25 ? 0 : percent < 75 ? 50 : 100;
    static int SnapTouchScale(int percent) => SnapTo(percent, 80, 100, 120, 140);
    static int SnapTouchOpacity(int percent) => SnapTo(percent, 20, 34, 50, 65);
    static int SnapTouchLookSpeed(int percent) => SnapTo(percent, 60, 80, 100, 120, 150);

    static int SnapTo(int value, params int[] steps)
    {
        var closest = steps[0];
        var distance = Mathf.Abs(value - closest);
        for (var i = 1; i < steps.Length; i++)
        {
            var nextDistance = Mathf.Abs(value - steps[i]);
            if (nextDistance < distance)
            {
                closest = steps[i];
                distance = nextDistance;
            }
        }
        return closest;
    }

    static int Step(int current, int[] steps, int direction)
    {
        var i = Array.IndexOf(steps, current);
        if (i < 0) i = 0;
        if (direction > 1) return steps[(i + 1) % steps.Length];
        return steps[Mathf.Clamp(i + (direction >= 0 ? 1 : -1), 0, steps.Length - 1)];
    }

    static void Save(string key, int value)
    {
        PlayerPrefs.SetInt(key, value);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
