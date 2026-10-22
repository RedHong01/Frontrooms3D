using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_IOS || UNITY_ANDROID || UNITY_EDITOR
using UnityEngine.InputSystem.EnhancedTouch;
using EnhancedTouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using InputTouchPhase = UnityEngine.InputSystem.TouchPhase;
#endif

/// <summary>
/// What the touch layer needs from the game: the desktop settings rows (shown
/// beside the touch rows), the pause card's meta line and the caught card.
/// FrontRooms3DGame implements it (FrontRooms3DGame.Mobile.cs).
/// </summary>
public interface IFrontRoomsTouchHost
{
    int GameSettingCount { get; }
    string GameSettingSection(int index);
    string GameSettingLabel(int index);
    string GameSettingValue(int index);
    bool GameSettingStepped(int index);
    void StepGameSetting(int index, int direction);
    string PauseMeta { get; }
    FrontRoomsTouchCaught CaughtStats { get; }
    /// <summary>The camera whose frame freezes under the pause frost (the world only, no HUD).</summary>
    Camera BackdropCamera { get; }
}

/// <summary>The caught card's numbers.</summary>
public struct FrontRoomsTouchCaught
{
    public int Seconds, Zones, Tier, Keys, DoorsBroken;
    public string Where;
}

/// <summary>
/// The touch input state machine (Documentation/TOUCH_CONTROLS.md §2, §9).
///
/// Every touch is converted to points (origin bottom-left) and tested against
/// <see cref="FrontRoomsTouchLayout"/>, the same layout the view draws from, so
/// a control is pressed exactly where it is drawn on every phone and tablet.
/// Each frame it publishes one <see cref="FrontRoomsInput.FrameSnapshot"/>
/// before the game updates (execution order −500).
///
/// It runs only while <see cref="FrontRoomsHandheld.Active"/>: iOS / Android
/// players, or the Editor touch preview and harness. Mac, Windows and WebGL
/// players compile the touch code out.
/// </summary>
[DefaultExecutionOrder(-500)]
public sealed class FrontRoomsTouchControls : MonoBehaviour
{
    public enum MenuState { Title, Playing, Paused, Settings, Caught }

    public enum UseKind { Hidden, Open, Shut, Take, Locked, Hold, Tap, Broken }

    /// <summary>Buttons the view draws a press state for.</summary>
    public enum Button { None, Pause, Use, SprintToggle, Resume, Settings, Restart, ReturnTitle, RestartCancel, RestartConfirm, TryAgain, SettingsClose, SettingsRow }

    /// <summary>The touch preference rows (the TOUCH column of the settings card).</summary>
    public enum TouchRow { LookSpeed, InvertLook, GyroLook, Stick, Sprint, ControlsSize, ControlsOpacity, LeftHanded, HapticsGameplay, HapticsControls }

    /// <summary>Settings rows for the touch column start here; the game's own rows use their index.</summary>
    public const int TouchRowBase = 100;

    /// <summary>What the contextual USE control represents (bound by the game from UpdateAim).</summary>
    [Serializable]
    public struct UsePrompt : IEquatable<UsePrompt>
    {
        public bool Visible;
        public bool Interactable;
        public bool Hold;
        public bool Locked;
        public bool TapMode;
        public string Label;
        [Range(0f, 1f)] public float Progress;
        public UseKind Kind;

        /// <summary>A locked door still takes the press: it rattles (desktop E does the same). So does a broken one: it won't shut.</summary>
        public bool AllowsPress => Visible && Interactable && Kind != UseKind.Hidden;

        public static UsePrompt Hidden => new UsePrompt { Label = string.Empty, Kind = UseKind.Hidden };

        public bool Equals(UsePrompt other)
        {
            return Visible == other.Visible && Interactable == other.Interactable && Hold == other.Hold && Locked == other.Locked
                && TapMode == other.TapMode && string.Equals(Label, other.Label, StringComparison.Ordinal)
                && Mathf.Approximately(Progress, other.Progress) && Kind == other.Kind;
        }

        public override bool Equals(object obj) => obj is UsePrompt other && Equals(other);
        public override int GetHashCode() => (Visible ? 1 : 0) ^ (Hold ? 4 : 0) ^ (Locked ? 8 : 0) ^ (Label ?? string.Empty).GetHashCode() ^ ((int)Kind << 5);
        public static bool operator ==(UsePrompt a, UsePrompt b) => a.Equals(b);
        public static bool operator !=(UsePrompt a, UsePrompt b) => !a.Equals(b);
    }

    /// <summary>One row (or section header) of the settings card, in points.</summary>
    public struct SettingsSlot
    {
        public int Id;
        public int Column;
        public bool Header;
        public string Label;
        public string Value;
        public bool Stepped;
        /// <summary>The row's rect in points, before the touch column's scroll.</summary>
        public Rect Rect;
    }

    /// <summary>The slow gait's speed as a share of the walk: 3.2 m/s × 0.45 = 1.44 m/s.</summary>
    public const float SlowSpeedShare = .45f;
    /// <summary>A run starts only within this many degrees of straight ahead, so a hard strafe never runs by accident.</summary>
    public const float RunHalfAngle = 70f;
    /// <summary>A broken door's try: USE swings out this long, then meets the frame (B·4's 90 ms), where the soft knock is felt.</summary>
    public const float BrokenKnockSeconds = .09f;

    // Timing (seconds) and slop (points).
    const float TapMaxSeconds = .22f;
    const float TapSlop = 8f;
    const float UseLookSlop = 12f;
    const float ButtonCancelSlop = 18f;
    const float ScrollStartSlop = 6f;
    const float BaseLookDegreesPerPoint = .20f;

    public IFrontRoomsTouchHost Host { get; set; }

    /// <summary>Raised for a settings row tap: the row id (game row index, or TouchRowBase + TouchRow) and a step (−1, +1, or 2 to cycle).</summary>
    public event Action<int, int> SettingsRowRequested;
    public event Action RestartCancelRequested;
    /// <summary>Raised by the pause card's QUIT TO TITLE chip; the game owns the scene/title reset.</summary>
    public event Action ReturnTitleRequested;
    /// <summary>A short, still tap on the look side, in viewport coordinates (0..1): the game uses what is under it (a door).</summary>
    public event Action<Vector2> LookTapped;
    public event Action<bool> RunChanged;

    // ------------------------------------------------------- state for the view
    public FrontRoomsTouchLayout Layout { get; private set; }
    public MenuState CurrentMenuState => menuState;
    /// <summary>The caught card's TRY AGAIN has waited its 0.6 s: Android Back may retry too.</summary>
    public bool TryAgainReady => menuState == MenuState.Caught && FrontRoomsTouchMotion.Now - menuStateSince >= TryAgainDelay;
    public bool RestartConfirmationOpen { get; private set; }
    public bool StickTouched => moveId >= 0;
    public Vector2 StickOrigin { get; private set; }
    /// <summary>Where the thumb is drawn: the finger, held inside the ring unless it reaches ahead for the run line (or runs).</summary>
    public Vector2 StickThumb { get; private set; }
    /// <summary>The stick's gait this frame: slow near the centre, walk out to the ring, run past the line (Red, 2026-10-08).</summary>
    public FrontRoomsGait CurrentGait { get; private set; }
    /// <summary>0..1 as the thumb goes from the ring to the run line, ahead; 1 while running.</summary>
    public float RunReach { get; private set; }
    /// <summary>Running: the stick is past the run line (or the RUN button is on). The game still applies stamina.</summary>
    public bool Running { get; private set; }
    public bool Winded { get; private set; }
    /// <summary>Settings › RUN: STICK (push past the ring) or BUTTON.</summary>
    public bool RunStickMode => runStickMode;
    public bool FloatingStick => floatingStick;
    public bool LeftHanded => leftHanded;
    public UsePrompt CurrentUsePrompt { get; private set; } = UsePrompt.Hidden;
    public bool UseHeld => useId >= 0;
    /// <summary>The button a finger is on right now (its press state), or None.</summary>
    public Button PressedButton { get; private set; }
    public int PressedSettingsRow { get; private set; } = -1;
    /// <summary>The last row changed, for the accent rule.</summary>
    public int ActiveSettingsRow { get; private set; } = -1;
    public float SettingsScroll { get; private set; }
    public float SettingsScrollMax { get; private set; }
    public IReadOnlyList<SettingsSlot> SettingsSlots => slots;
    public float LastTouchTime { get; private set; }

    MenuState menuState = MenuState.Title;
    float menuStateSince;
    /// <summary>TRY AGAIN takes taps only after this long on the caught card (a thumb still down from the chase must not restart).</summary>
    public const float TryAgainDelay = .6f;
    bool floatingStick = true;
    bool runStickMode = true;
    bool leftHanded;
    float controlsScale = 1f;
    float lookDegreesPerPoint = BaseLookDegreesPerPoint;
    bool invertLook;
    int gyroMode;

    // This frame's edges and values (published to FrontRoomsInput).
    Vector2 frameMove, frameLook;
    bool frameUseDown, framePauseDown, frameStartDown, frameSettingsDown, frameRestartDown, frameShotBack;

    readonly List<SettingsSlot> slots = new List<SettingsSlot>();
    float scrollVelocity;

    int moveId = -1, lookId = -1, useId = -1;
    bool shotBackArmed;
    Vector2 moveFinger;

#if UNITY_IOS || UNITY_ANDROID || UNITY_EDITOR
    enum Role { None, Move, Look, Use, Button, Scroll, Start }

    struct Finger
    {
        public Role Role;
        public Button Button;
        public int Row;
        public int RowDirection;
        public Vector2 Start;
        public Vector2 Last;
        public float StartTime;
        public bool Cancelled;
        public bool Moved;
        public float ScrollStart;
        /// <summary>A tap on the frost around the settings card: it goes back like the BACK chip.</summary>
        public bool Scrim;
    }

    readonly Dictionary<int, Finger> fingers = new Dictionary<int, Finger>();
    readonly List<int> stale = new List<int>();
    readonly HashSet<int> seen = new HashSet<int>();
    bool touchOwner, simulationOwner;
#endif

    void Awake()
    {
        ApplySavedSettings();
        RefreshLayout();
    }

    void OnEnable()
    {
#if UNITY_IOS || UNITY_ANDROID || UNITY_EDITOR
        if (!EnhancedTouchSupport.enabled)
        {
            EnhancedTouchSupport.Enable();
            touchOwner = true;
        }
#if UNITY_EDITOR
        // The interactive Editor preview drives touch with the mouse (one finger). A harness feeds its own touchscreen.
        if (FrontRoomsHandheld.EditorPreviewRequested && !FrontRoomsHandheld.Forced && !TouchSimulation.instance)
        {
            TouchSimulation.Enable();
            simulationOwner = true;
        }
#endif
        EnableGyroIfNeeded();
#endif
    }

    void OnDisable()
    {
        ClearTouchState();
        FrontRoomsInput.ClearVirtualSnapshot();
#if UNITY_IOS || UNITY_ANDROID || UNITY_EDITOR
        if (simulationOwner) { TouchSimulation.Disable(); simulationOwner = false; }
        if (touchOwner) { EnhancedTouchSupport.Disable(); touchOwner = false; }
#endif
    }

    void OnApplicationFocus(bool focused)
    {
        if (!focused) ClearTouchState();
    }

    void OnApplicationPause(bool paused)
    {
        if (paused) ClearTouchState();
    }

    void Update()
    {
        if (!FrontRoomsHandheld.Active)
        {
            FrontRoomsInput.ClearVirtualSnapshot();
            return;
        }
        RefreshLayout();
        BeginFrame();
#if UNITY_IOS || UNITY_ANDROID || UNITY_EDITOR
        ProcessTouches();
#endif
        UpdateStick();
        AddGyroLook();
        UpdateSettingsSlots();
        UpdateScrollInertia();
        FrontRoomsInput.SetVirtualSnapshot(new FrontRoomsInput.FrameSnapshot(
            frameMove,
            frameLook,
            Running && !Winded,
            frameUseDown,
            useId >= 0,
            framePauseDown,
            frameStartDown,
            frameSettingsDown,
            frameRestartDown,
            frameShotBack));
    }

    void BeginFrame()
    {
        frameMove = Vector2.zero;
        frameLook = Vector2.zero;
        frameUseDown = framePauseDown = frameStartDown = frameSettingsDown = frameRestartDown = frameShotBack = false;
    }

    void RefreshLayout()
    {
        var scale = Mathf.Max(.01f, FrontRoomsHandheld.PointScale);
        var safePx = FrontRoomsHandheld.SafeAreaPixels;
        var safe = new Rect(safePx.x / scale, safePx.y / scale, safePx.width / scale, safePx.height / scale);
        var edgeGuard = 0f;
#if UNITY_ANDROID
        edgeGuard = 24f;
#endif
        Layout = new FrontRoomsTouchLayout(FrontRoomsHandheld.ScreenPoints, safe, controlsScale * FrontRoomsHandheld.DeviceControlsScale, leftHanded, FrontRoomsHandheld.IsTablet, edgeGuard);
    }

    // ------------------------------------------------------------ public api

    public void SetMenuState(MenuState state)
    {
        if (menuState == state) return;
        menuState = state;
        menuStateSince = FrontRoomsTouchMotion.Now;
        if (state != MenuState.Paused) RestartConfirmationOpen = false;
        if (state == MenuState.Settings) { SettingsScroll = 0f; scrollVelocity = 0f; }
        ClearTouchState();
    }

    public void SetRestartConfirmation(bool open)
    {
        RestartConfirmationOpen = open && menuState == MenuState.Paused;
    }

    /// <summary>A camera takeover's HUD fade, 0..1 (the rig's HudFade): the desktop HUD fades with it, and so does USE unless a thumb holds it.</summary>
    public float Takeover { get; private set; }

    public void SetTakeover(float value) => Takeover = Mathf.Clamp01(value);

    public void SetWinded(bool value)
    {
        if (Winded == value) return;
        Winded = value;
        if (Winded) SetRunning(false);
    }

    /// <summary>Binds what the crosshair is on. Losing the target releases a held USE so a stale hold never leaks into the next one.</summary>
    public void SetUsePrompt(UsePrompt prompt)
    {
        prompt.Progress = Mathf.Clamp01(prompt.Progress);
        if (prompt == CurrentUsePrompt) return;
        var lost = !prompt.AllowsPress;
        CurrentUsePrompt = prompt;
        if (lost && useId >= 0) ReleaseUse();
    }

    public void ClearUsePrompt() => SetUsePrompt(UsePrompt.Hidden);

    /// <summary>When the broken door USE names last answered a try (FrontRoomsTouchMotion.Now): the view swings USE once.</summary>
    public float BrokenTriedAt { get; private set; } = float.NegativeInfinity;

    /// <summary>The broken door USE names answered a try (USE, or a tap on it) with the map's Broken: it won't shut.</summary>
    public void NoteBrokenTried() => BrokenTriedAt = FrontRoomsTouchMotion.Now;

    /// <summary>Reads the touch preferences (Settings › TOUCH).</summary>
    public void ApplySavedSettings()
    {
        controlsScale = Mathf.Clamp(FrontRoomsSettings.TouchControlsScalePercent / 100f, .8f, 1.4f);
        var hand = FrontRoomsSettings.TouchLeftHanded;
        if (hand != leftHanded) { leftHanded = hand; ClearTouchState(); }
        lookDegreesPerPoint = BaseLookDegreesPerPoint * FrontRoomsSettings.TouchLookSpeedPercent / 100f;
        invertLook = FrontRoomsSettings.TouchInvertLook;
        gyroMode = Mathf.Clamp(FrontRoomsSettings.TouchGyroMode, 0, 2);
        var floating = FrontRoomsSettings.TouchFloatingStick;
        if (floating != floatingStick) { floatingStick = floating; ClearTouchState(); }
        // The preference keeps its old name: TouchSprintSocket true = run with the stick.
        var stickRun = FrontRoomsSettings.TouchSprintSocket;
        if (stickRun != runStickMode) { runStickMode = stickRun; SetRunning(false); }
        FrontRoomsMobileHaptics.GameplayEnabled = FrontRoomsSettings.TouchHaptics;
        FrontRoomsMobileHaptics.ControlsEnabled = FrontRoomsSettings.TouchHapticsControls;
#if UNITY_IOS || UNITY_ANDROID || UNITY_EDITOR
        EnableGyroIfNeeded();
#endif
        RefreshLayout();
    }

    // ----------------------------------------------------------------- stick

    void UpdateStick()
    {
        if (moveId < 0)
        {
            StickOrigin = floatingStick ? StickOrigin : Layout.StickRest;
            StickThumb = StickOrigin;
            CurrentGait = FrontRoomsGait.Still;
            RunReach = 0f;
            return;
        }
        var L = Layout;
        var offset = moveFinger - StickOrigin;
        var r = L.StickRadius;
        var magnitude = offset.magnitude;
        var ahead = magnitude > .001f && Vector2.Angle(Vector2.up, offset) <= RunHalfAngle;
        var canRun = runStickMode && !Winded;

        // Run past the ring (Red, 2026-10-08): in at the line 24 pt beyond the ring, ahead, with a click you feel;
        // out once the thumb is back inside the ring (or pulled back). Between the two the gait holds, so a thumb
        // resting near the edge never flickers. Alien: Isolation's sprint fired at the edge itself and broke stealth.
        if (canRun)
        {
            if (!Running) { if (ahead && magnitude >= L.RunLineRadius) SetRunning(true); }
            else if (magnitude < r || offset.y < 0f) SetRunning(false);
        }
        else if (!runStickMode && Running && offset.y <= 0f && magnitude > L.StickDeadZone)
        {
            // A RUN-button run also ends on a pull back.
            SetRunning(false);
        }
        RunReach = Running ? 1f : canRun && ahead ? Mathf.Clamp01((magnitude - r) / Mathf.Max(1f, L.RunLineRadius - r)) : 0f;

        // A pull well below the centre is the desktop S: it cancels a shot that allows it.
        var pulledBack = offset.y < -L.ShotBackDepth;
        if (pulledBack && !shotBackArmed) { frameShotBack = true; shotBackArmed = true; }
        else if (!pulledBack) shotBackArmed = false;

        // Three gaits, one speed each, toward the thumb: slow inside 36 pt (the Relay can't hear it), walk beyond
        // (the desktop walk), run past the line (the desktop sprint).
        if (magnitude <= L.StickDeadZone) CurrentGait = FrontRoomsGait.Still;
        else if (Running) CurrentGait = FrontRoomsGait.Run;
        else
        {
            var walking = CurrentGait == FrontRoomsGait.Walk || CurrentGait == FrontRoomsGait.Run;
            var edge = L.SlowRadius + (walking ? -L.SlowHysteresis : L.SlowHysteresis);
            CurrentGait = magnitude >= edge ? FrontRoomsGait.Walk : FrontRoomsGait.Slow;
        }
        frameMove = CurrentGait == FrontRoomsGait.Still ? Vector2.zero
            : offset / magnitude * (CurrentGait == FrontRoomsGait.Slow ? SlowSpeedShare : 1f);

        // The drawn thumb stays inside the ring, except ahead, where it follows the finger to just over the line.
        var reach = canRun && (ahead || Running) ? L.RunThumbReach : r;
        StickThumb = StickOrigin + Vector2.ClampMagnitude(offset, reach);
    }

    void SetRunning(bool value)
    {
        if (Running == value) return;
        Running = value;
        FrontRoomsMobileInteractionEvents.RunChanged(value);
        RunChanged?.Invoke(value);
    }

    // -------------------------------------------------------------- settings

    void UpdateSettingsSlots()
    {
        slots.Clear();
        if (menuState != MenuState.Settings) return;
        var L = Layout;
        var left = L.SettingsColumn(0);
        var right = L.SettingsColumn(1);
        const float header = 18f;
        var rowH = FrontRoomsTouchLayout.SettingsRowHeight;

        // Left column: the game's own rows (display, comfort, input, assist), under two headers.
        var y = left.yMax;
        string group = null;
        var count = Host != null ? Host.GameSettingCount : 0;
        for (var i = 0; i < count; i++)
        {
            var section = Host.GameSettingSection(i);
            var g = section == "OUTPUT" || section == "COMFORT" ? "DISPLAY  +  COMFORT" : "INPUT  +  ASSIST";
            if (g != group)
            {
                group = g;
                slots.Add(new SettingsSlot { Id = -1, Column = 0, Header = true, Label = g, Rect = new Rect(left.x, y - header, left.width, header) });
                y -= header;
            }
            slots.Add(new SettingsSlot { Id = i, Column = 0, Label = Host.GameSettingLabel(i), Value = Host.GameSettingValue(i), Stepped = Host.GameSettingStepped(i), Rect = new Rect(left.x, y - rowH, left.width, rowH) });
            y -= rowH;
        }

        // Right column: TOUCH, scrolling under its header.
        slots.Add(new SettingsSlot { Id = -1, Column = 1, Header = true, Label = "TOUCH", Rect = new Rect(right.x, right.yMax - header, right.width, header) });
        var ry = right.yMax - header;
        foreach (TouchRow row in Enum.GetValues(typeof(TouchRow)))
        {
            if (!TouchRowVisible(row)) continue;
            slots.Add(new SettingsSlot { Id = TouchRowBase + (int)row, Column = 1, Label = TouchRowLabel(row), Value = TouchRowValue(row), Stepped = TouchRowStepped(row), Rect = new Rect(right.x, ry - rowH, right.width, rowH) });
            ry -= rowH;
        }
        var visible = right.height - header;
        SettingsScrollMax = Mathf.Max(0f, (right.yMax - header - ry) - visible);
        SettingsScroll = Mathf.Clamp(SettingsScroll, 0f, SettingsScrollMax);
    }

    void UpdateScrollInertia()
    {
#if UNITY_IOS || UNITY_ANDROID || UNITY_EDITOR
        foreach (var f in fingers.Values) if (f.Role == Role.Scroll) return;
#endif
        if (Mathf.Abs(scrollVelocity) < 1f) { scrollVelocity = 0f; return; }
        SettingsScroll = Mathf.Clamp(SettingsScroll + scrollVelocity * FrontRoomsTouchMotion.Delta, 0f, SettingsScrollMax);
        scrollVelocity *= Mathf.Exp(-FrontRoomsTouchMotion.Delta / .22f);
    }

    /// <summary>The touch column's area that scrolls (below its header), in points.</summary>
    public Rect SettingsScrollViewport
    {
        get
        {
            var right = Layout.SettingsColumn(1);
            return new Rect(right.x, right.y, right.width, right.height - 18f);
        }
    }

    public static bool TouchRowVisible(TouchRow row)
    {
        // No iPad plays haptics, so the haptic rows only appear where they can do something.
        if (row == TouchRow.HapticsGameplay || row == TouchRow.HapticsControls) return FrontRoomsHandheld.HapticsSupported;
        return true;
    }

    public static string TouchRowLabel(TouchRow row)
    {
        switch (row)
        {
            case TouchRow.LookSpeed: return "LOOK SPEED";
            case TouchRow.InvertLook: return "INVERT LOOK";
            case TouchRow.GyroLook: return "GYRO LOOK";
            case TouchRow.Stick: return "STICK";
            case TouchRow.Sprint: return "RUN";
            case TouchRow.ControlsSize: return "CONTROLS SIZE";
            case TouchRow.ControlsOpacity: return "CONTROLS OPACITY";
            case TouchRow.LeftHanded: return "LEFT-HANDED";
            case TouchRow.HapticsGameplay: return "HAPTICS  ·  GAMEPLAY";
            case TouchRow.HapticsControls: return "HAPTICS  ·  CONTROLS";
            default: return string.Empty;
        }
    }

    public static string TouchRowValue(TouchRow row)
    {
        switch (row)
        {
            case TouchRow.LookSpeed: return FrontRoomsSettings.TouchLookSpeedPercent + "%";
            case TouchRow.InvertLook: return FrontRoomsSettings.TouchInvertLook ? "ON" : "OFF";
            case TouchRow.GyroLook: return FrontRoomsSettings.TouchGyroMode == 0 ? "OFF" : FrontRoomsSettings.TouchGyroMode == 1 ? "WHILE TOUCHING" : "ALWAYS";
            case TouchRow.Stick: return FrontRoomsSettings.TouchFloatingStick ? "FLOATING" : "FIXED";
            case TouchRow.Sprint: return FrontRoomsSettings.TouchSprintSocket ? "STICK" : "BUTTON";
            case TouchRow.ControlsSize: return FrontRoomsSettings.TouchControlsScalePercent + "%";
            case TouchRow.ControlsOpacity: return FrontRoomsSettings.TouchOpacityPercent + "%";
            case TouchRow.LeftHanded: return FrontRoomsSettings.TouchLeftHanded ? "ON" : "OFF";
            case TouchRow.HapticsGameplay: return FrontRoomsSettings.TouchHaptics ? "ON" : "OFF";
            case TouchRow.HapticsControls: return FrontRoomsSettings.TouchHapticsControls ? "ON" : "OFF";
            default: return string.Empty;
        }
    }

    public static bool TouchRowStepped(TouchRow row) =>
        row == TouchRow.LookSpeed || row == TouchRow.ControlsSize || row == TouchRow.ControlsOpacity;

    /// <summary>Applies a touch row step (−1, +1, or 2 to cycle).</summary>
    public static void StepTouchRow(TouchRow row, int direction)
    {
        switch (row)
        {
            case TouchRow.LookSpeed: FrontRoomsSettings.StepTouchLookSpeed(direction); break;
            case TouchRow.InvertLook: FrontRoomsSettings.SetTouchInvertLook(!FrontRoomsSettings.TouchInvertLook); break;
            case TouchRow.GyroLook: FrontRoomsSettings.StepTouchGyro(direction); break;
            case TouchRow.Stick: FrontRoomsSettings.SetTouchFloatingStick(!FrontRoomsSettings.TouchFloatingStick); break;
            case TouchRow.Sprint: FrontRoomsSettings.SetTouchSprintSocket(!FrontRoomsSettings.TouchSprintSocket); break;
            case TouchRow.ControlsSize: FrontRoomsSettings.StepTouchControlsScale(direction); break;
            case TouchRow.ControlsOpacity: FrontRoomsSettings.StepTouchOpacity(direction); break;
            case TouchRow.LeftHanded: FrontRoomsSettings.SetTouchLeftHanded(!FrontRoomsSettings.TouchLeftHanded); break;
            case TouchRow.HapticsGameplay: FrontRoomsSettings.SetTouchHaptics(!FrontRoomsSettings.TouchHaptics); break;
            case TouchRow.HapticsControls: FrontRoomsSettings.SetTouchHapticsControls(!FrontRoomsSettings.TouchHapticsControls); break;
        }
    }

    // ------------------------------------------------------------- menu rects

    /// <summary>A menu chip's centre and size (points) for the current state, or a zero size when it is not shown.</summary>
    public bool TryGetChip(Button button, out Vector2 center, out Vector2 size)
    {
        var L = Layout;
        var c = L.Center;
        center = Vector2.zero;
        size = Vector2.zero;
        var h = FrontRoomsTouchLayout.ChipHeight;
        switch (menuState)
        {
            case MenuState.Paused when !RestartConfirmationOpen:
                if (button == Button.Resume) { center = c + new Vector2(-96.5f, -89f); size = new Vector2(75f, h); return true; }
                if (button == Button.Settings) { center = c + new Vector2(-1.5f, -89f); size = new Vector2(83f, h); return true; }
                if (button == Button.Restart) { center = c + new Vector2(95.5f, -89f); size = new Vector2(79f, h); return true; }
                // QUIT TO TITLE: centred 16 pt under the row, as wide as Figma's hug (平面视觉, 2026-10-08).
                if (button == Button.ReturnTitle) { center = c + new Vector2(0f, -145f); size = new Vector2(109f, h); return true; }
                return false;
            case MenuState.Paused when RestartConfirmationOpen:
                if (button == Button.RestartCancel) { center = c + new Vector2(-46f, -89f); size = new Vector2(74f, h); return true; }
                if (button == Button.RestartConfirm) { center = c + new Vector2(46f, -89f); size = new Vector2(79f, h); return true; }
                return false;
            case MenuState.Settings:
                if (button == Button.SettingsClose)
                {
                    // BACK, in the card's top-right corner (it was CLOSE, drawn under the card and invisible).
                    var card = L.SettingsCardSize;
                    center = L.SettingsCardCenter + new Vector2(card.x * .5f - 24f - 29f, card.y * .5f - 8f - 20f);
                    size = new Vector2(58f, h);
                    return true;
                }
                return false;
            case MenuState.Caught:
                // 10 pt lower than before: the caught card's subtitle pushed its block down (FIX_CARD_VL217 A7).
                if (button == Button.TryAgain) { center = c + new Vector2(0f, -99f); size = new Vector2(91f, h); return true; }
                return false;
        }
        return false;
    }

    static readonly Button[] MenuButtons = { Button.Resume, Button.Settings, Button.Restart, Button.ReturnTitle, Button.RestartCancel, Button.RestartConfirm, Button.SettingsClose, Button.TryAgain };

    Button MenuButtonAt(Vector2 p)
    {
        foreach (var b in MenuButtons)
        {
            if (b == Button.TryAgain && FrontRoomsTouchMotion.Now - menuStateSince < TryAgainDelay) continue;
            if (!TryGetChip(b, out var center, out var size)) continue;
            var hit = new Vector2(size.x + 12f, Mathf.Max(FrontRoomsTouchLayout.ChipHitHeight, size.y + 4f));
            if (FrontRoomsTouchLayout.Contains(center, hit, p)) return b;
        }
        return Button.None;
    }

    bool SettingsRowAt(Vector2 p, out int id, out int direction)
    {
        id = -1;
        direction = 2;
        var viewport = SettingsScrollViewport;
        foreach (var s in slots)
        {
            if (s.Header) continue;
            var rect = s.Rect;
            if (s.Column == 1)
            {
                if (!viewport.Contains(p)) continue;
                rect.y += SettingsScroll;
            }
            if (!rect.Contains(p)) continue;
            id = s.Id;
            // ‹ steps down, › (and the rest of the row) steps up; on/off rows cycle.
            if (s.Stepped) direction = p.x < rect.xMax - 44f && p.x > rect.xMax - 150f ? -1 : 1;
            return true;
        }
        return false;
    }

    // ---------------------------------------------------------------- touches

#if UNITY_IOS || UNITY_ANDROID || UNITY_EDITOR
    void ProcessTouches()
    {
        seen.Clear();
        var scale = Mathf.Max(.01f, FrontRoomsHandheld.PointScale);
        var active = EnhancedTouch.activeTouches;
        for (var i = 0; i < active.Count; i++)
        {
            var touch = active[i];
            if (!touch.valid) continue;
            var id = touch.touchId;
            seen.Add(id);
            var p = touch.screenPosition / scale;
            if (touch.phase == InputTouchPhase.Began)
            {
                Begin(id, p);
                continue;
            }
            if (!fingers.TryGetValue(id, out var f)) continue;
            var delta = p - f.Last;
            f.Last = p;
            fingers[id] = f;
            Move(id, f, p, delta);
            if (touch.phase == InputTouchPhase.Ended || touch.phase == InputTouchPhase.Canceled)
                End(id, fingers.TryGetValue(id, out var ended) ? ended : f, p, touch.phase == InputTouchPhase.Canceled);
        }

        // A focus change can swallow an Ended event: never leave a stick, USE or button stuck down.
        stale.Clear();
        foreach (var pair in fingers) if (!seen.Contains(pair.Key)) stale.Add(pair.Key);
        foreach (var id in stale)
            if (fingers.TryGetValue(id, out var f)) End(id, f, f.Last, true);
    }

    void Begin(int id, Vector2 p)
    {
        LastTouchTime = FrontRoomsTouchMotion.Now;
        var L = Layout;
        var f = new Finger { Start = p, Last = p, StartTime = FrontRoomsTouchMotion.Now, Row = -1 };
        switch (menuState)
        {
            case MenuState.Title:
                f.Role = Role.Start;
                break;
            case MenuState.Playing:
                if (FrontRoomsTouchLayout.Contains(L.PauseCenter, new Vector2(L.PauseHitSize, L.PauseHitSize), p))
                {
                    f.Role = Role.Button;
                    f.Button = Button.Pause;
                }
                else if (CurrentUsePrompt.AllowsPress && useId < 0 && FrontRoomsTouchLayout.Contains(L.UseCenter, L.UseHitRadius, p))
                {
                    f.Role = Role.Use;
                    useId = id;
                    frameUseDown = true;
                    FrontRoomsMobileInteractionEvents.UsePressed();
                }
                else if (!runStickMode && FrontRoomsTouchLayout.Contains(L.SprintButtonCenter, L.SprintButtonHitRadius, p))
                {
                    f.Role = Role.Button;
                    f.Button = Button.SprintToggle;
                }
                else if (moveId < 0 && L.InMoveZone(p))
                {
                    f.Role = Role.Move;
                    moveId = id;
                    moveFinger = p;
                    StickOrigin = floatingStick ? L.FloatingOrigin(p) : L.StickRest;
                    shotBackArmed = false;
                }
                else if (lookId < 0 && L.InLookZone(p))
                {
                    f.Role = Role.Look;
                    lookId = id;
                }
                break;
            case MenuState.Paused:
            case MenuState.Caught:
                f.Button = MenuButtonAt(p);
                if (f.Button != Button.None) f.Role = Role.Button;
                break;
            case MenuState.Settings:
                f.Button = MenuButtonAt(p);
                if (f.Button != Button.None) f.Role = Role.Button;
                else if (SettingsRowAt(p, out var row, out var dir))
                {
                    f.Role = Role.Button;
                    f.Button = Button.SettingsRow;
                    f.Row = row;
                    f.RowDirection = dir;
                    f.ScrollStart = SettingsScroll;
                    scrollVelocity = 0f;
                }
                else if (!SettingsCardRect.Contains(p))
                {
                    // The frost around the card is a way back too; BACK shows pressed while the tap is held.
                    f.Role = Role.Button;
                    f.Button = Button.SettingsClose;
                    f.Scrim = true;
                }
                break;
        }
        if (f.Role == Role.None) return;
        fingers[id] = f;
        if (f.Role == Role.Button)
        {
            PressedButton = f.Button;
            PressedSettingsRow = f.Row;
        }
    }

    void Move(int id, Finger f, Vector2 p, Vector2 delta)
    {
        switch (f.Role)
        {
            case Role.Move:
                moveFinger = p;
                break;
            case Role.Look:
                frameLook += LookDegrees(delta);
                if ((p - f.Start).sqrMagnitude > TapSlop * TapSlop) { f.Moved = true; fingers[id] = f; }
                break;
            case Role.Use:
                // A thumb that starts on USE may slide on to look (keeping the dot on a pane while it holds), after a little slop.
                if (!f.Moved && (p - f.Start).sqrMagnitude > UseLookSlop * UseLookSlop) { f.Moved = true; fingers[id] = f; }
                else if (f.Moved) frameLook += LookDegrees(delta);
                break;
            case Role.Button:
                if (f.Button == Button.SettingsRow && Layout.SettingsColumn(1).Contains(f.Start) && Mathf.Abs(p.y - f.Start.y) > ScrollStartSlop)
                {
                    // A drag in the touch column scrolls it instead of tapping the row.
                    f.Role = Role.Scroll;
                    fingers[id] = f;
                    PressedButton = Button.None;
                    PressedSettingsRow = -1;
                    goto case Role.Scroll;
                }
                var cancelled = !StillOnButton(f, p);
                if (cancelled != f.Cancelled)
                {
                    f.Cancelled = cancelled;
                    fingers[id] = f;
                    PressedButton = cancelled ? Button.None : f.Button;
                    PressedSettingsRow = cancelled ? -1 : f.Row;
                }
                break;
            case Role.Scroll:
                SettingsScroll = Mathf.Clamp(f.ScrollStart + (p.y - f.Start.y), 0f, SettingsScrollMax);
                scrollVelocity = FrontRoomsTouchMotion.Delta > 0f ? delta.y / FrontRoomsTouchMotion.Delta : 0f;
                break;
        }
    }

    Rect SettingsCardRect
    {
        get
        {
            var size = Layout.SettingsCardSize;
            return new Rect(Layout.SettingsCardCenter - size * .5f, size);
        }
    }

    bool StillOnButton(Finger f, Vector2 p)
    {
        var L = Layout;
        // A frost tap must stay a tap outside the card (a drag that wanders onto the card cancels it).
        if (f.Scrim) return !SettingsCardRect.Contains(p) && (p - f.Start).sqrMagnitude <= TapSlop * TapSlop * 4f;
        switch (f.Button)
        {
            case Button.Pause: return FrontRoomsTouchLayout.Contains(L.PauseCenter, new Vector2(L.PauseHitSize + ButtonCancelSlop, L.PauseHitSize + ButtonCancelSlop), p);
            case Button.SprintToggle: return FrontRoomsTouchLayout.Contains(L.SprintButtonCenter, L.SprintButtonHitRadius + ButtonCancelSlop, p);
            case Button.SettingsRow: return (p - f.Start).sqrMagnitude <= TapSlop * TapSlop * 4f;
            default:
                if (!TryGetChip(f.Button, out var c, out var size)) return false;
                return FrontRoomsTouchLayout.Contains(c, size + Vector2.one * (12f + ButtonCancelSlop), p);
        }
    }

    void End(int id, Finger f, Vector2 p, bool cancelled)
    {
        fingers.Remove(id);
        switch (f.Role)
        {
            case Role.Move:
                if (moveId == id)
                {
                    moveId = -1;
                    SetRunning(false);
                    shotBackArmed = false;
                }
                break;
            case Role.Look:
                if (lookId == id)
                {
                    lookId = -1;
                    var quick = FrontRoomsTouchMotion.Now - f.StartTime <= TapMaxSeconds;
                    if (!cancelled && quick && !f.Moved && (p - f.Start).sqrMagnitude <= TapSlop * TapSlop)
                    {
                        var frame = Layout.Frame;
                        LookTapped?.Invoke(new Vector2(p.x / Mathf.Max(1f, frame.x), p.y / Mathf.Max(1f, frame.y)));
                    }
                }
                break;
            case Role.Use:
                if (useId == id) ReleaseUse();
                break;
            case Role.Start:
                if (!cancelled) frameStartDown = true;
                break;
            case Role.Button:
                PressedButton = Button.None;
                PressedSettingsRow = -1;
                if (!cancelled && !f.Cancelled && StillOnButton(f, p)) Activate(f);
                break;
            case Role.Scroll:
                break;
        }
    }

    void Activate(Finger f)
    {
        switch (f.Button)
        {
            case Button.Pause:
            case Button.Resume:
                framePauseDown = true;
                break;
            case Button.Settings:
            case Button.SettingsClose:
                frameSettingsDown = true;
                break;
            case Button.Restart:
            case Button.RestartConfirm:
            case Button.TryAgain:
                frameRestartDown = true;
                break;
            case Button.ReturnTitle:
                ReturnTitleRequested?.Invoke();
                break;
            case Button.RestartCancel:
                RestartCancelRequested?.Invoke();
                break;
            case Button.SprintToggle:
                if (!Winded) SetRunning(!Running);
                break;
            case Button.SettingsRow:
                ActiveSettingsRow = f.Row;
                SettingsRowRequested?.Invoke(f.Row, f.RowDirection);
                break;
        }
        if (f.Button != Button.Pause && f.Button != Button.SprintToggle) FrontRoomsMobileInteractionEvents.MenuConfirmed();
    }

    Vector2 LookDegrees(Vector2 deltaPoints)
    {
        var d = deltaPoints * lookDegreesPerPoint;
        // Drag up looks up (the game takes +y as up, as with the mouse); invert flips it.
        return new Vector2(d.x, invertLook ? -d.y : d.y);
    }

    void AddGyroLook()
    {
        if (gyroMode <= 0 || menuState != MenuState.Playing) return;
        if (gyroMode == 1 && lookId < 0 && useId < 0 && moveId < 0) return;
        var gyro = UnityEngine.InputSystem.Gyroscope.current;
        if (gyro == null || !gyro.enabled) return;
        var w = gyro.angularVelocity.ReadValue();
        // rad/s about the device axes, landscape: turning the phone left/right is the y axis, tilting it is x.
        var degrees = new Vector2(-w.y, invertLook ? -w.x : w.x) * Mathf.Rad2Deg * FrontRoomsTouchMotion.Delta;
        frameLook += degrees;
    }

    void EnableGyroIfNeeded()
    {
        if (gyroMode <= 0) return;
        var gyro = UnityEngine.InputSystem.Gyroscope.current;
        if (gyro != null && !gyro.enabled) UnityEngine.InputSystem.InputSystem.EnableDevice(gyro);
    }
#else
    void AddGyroLook() { }
#endif

    void ReleaseUse()
    {
        if (useId < 0) return;
#if UNITY_IOS || UNITY_ANDROID || UNITY_EDITOR
        fingers.Remove(useId);
#endif
        useId = -1;
        FrontRoomsMobileInteractionEvents.UseReleased();
    }

    void ClearTouchState()
    {
        if (useId >= 0) ReleaseUse();
#if UNITY_IOS || UNITY_ANDROID || UNITY_EDITOR
        fingers.Clear();
#endif
        moveId = lookId = -1;
        shotBackArmed = false;
        SetRunning(false);
        CurrentGait = FrontRoomsGait.Still;
        RunReach = 0f;
        PressedButton = Button.None;
        PressedSettingsRow = -1;
        frameMove = frameLook = Vector2.zero;
    }
}
