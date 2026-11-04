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
    /// <summary>This run's Relay has left Dormant at least once (X2: SLOW · UNHEARD only retires after the release).</summary>
    bool RelayEverReleased { get; }
    /// <summary>This frame is a real creep: playing, the slow gait, not winded, the body moving ≥ 0.5 m/s (X2 §3.4).</summary>
    bool CreepCounts { get; }
    /// <summary>Adds the HUD's top band and captions in touch points (the zone block, the key panel's fixed place,
    /// the Relay readout, the captions strip): the stick's slot words never draw over them (X2 §3.3).</summary>
    void HudKeepOut(List<Rect> into);
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
    public enum MenuState { Title, Playing, Paused, Settings, Caught, Inspect }

    // Unlock and Examine are appended (HUD key B touch contract T-1 / T-2), so the earlier values keep their numbers.
    public enum UseKind { Hidden, Open, Shut, Take, Locked, Hold, Tap, Broken, Read, Back, Unlock, Examine, Watch, Peek }

    /// <summary>Buttons the view draws a press state for.</summary>
    public enum Button { None, Pause, Use, SprintToggle, Resume, Settings, Restart, ReturnTitle, RestartCancel, RestartConfirm, TryAgain, SettingsClose, SettingsRow, TextChip, SettingsTab, SettingsPreview, SettingsPeekBack, LanguageRow, PadMapTap }

    /// <summary>The touch preference rows (the TOUCH column of the settings card).</summary>
    public enum TouchRow { LookSpeed, InvertLook, GyroLook, Stick, Sprint, StickHints, ControlsSize, ControlsOpacity, LeftHanded, HapticsGameplay, HapticsControls }

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
        /// <summary>The settings row it draws (FrontRoomsSettingsMenu), or null for a header.</summary>
        public FrontRoomsSettingsMenu.Row Row;
        /// <summary>The row's first line in points (its name and its control); the rest of Rect is its second line while it is
        /// open (SETTINGS_MOTION.md).</summary>
        public float Rest;
        /// <summary>How far the row is open, 0..1 (its height's curve); whether it is opening or open; when it last opened and
        /// closed (FrontRoomsTouchMotion.Now), for its second line's fades.</summary>
        public float Open;
        public bool IsOpen;
        public float OpenAt, CloseAt;
        /// <summary>LANGUAGE only: how far its grid is unfolded under it, 0..1.</summary>
        public float Fold;
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
    // TT1′ (computer_ui 41 §5.4): a swipe on a terminal's picture. It starts in body rows 4–23 (3–22 from 0): a drag from the top
    // or bottom edge stays the system's, and a press on a soft key never reads as a swipe.
    const float TerminalSwipeMin = 40f, TerminalSwipeSeconds = .35f;
    const int TerminalSwipeFirstRow = 3, TerminalSwipeLastRow = 22;
    // EXAMINE (contract T1): a pinch this much wider (× Scale) is one full zoom step.
    const float PinchPointsPerStep = 160f;
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
    /// <summary>The TEXT chip in a close-up was tapped (EXAMINE T6): the game turns the transcript panel on or off.</summary>
    public event Action TranscriptToggled;

    // EXAMINE T6/T7, set by the view each frame: the TEXT chip shows (the close-up has a transcript), the panel shows, and how
    // far its text is scrolled.
    public bool TextChipShown { get; set; }
    public bool TranscriptShown { get; set; }
    public float TranscriptScroll { get; set; }
    public float TranscriptScrollMax { get; set; }
    /// <summary>The close-up on screen runs live (A43: only careful reading pauses). The game sets it as a close-up opens.</summary>
    public bool CloseUpRunsLive { get; set; }

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
    /// <summary>The last row touched or changed; with a pad or a keyboard, the row in focus (its yellow bar).</summary>
    public int ActiveSettingsRow { get; private set; } = -1;
    /// <summary>The open row (SETTINGS_MOTION.md; Red, 2026-11-01: "这个栏目应该直接展开变成两倍的大小"): the chosen row opens in
    /// place to twice its height and says what its choice means; only one is open; -1 none. A tap opens a row and works the
    /// control under it, a tap on the open row's name closes it; with a pad or a keyboard ↑ ↓ open the next, B or Esc close it.</summary>
    public int OpenSettingsRow { get; private set; } = -1;
    /// <summary>LANGUAGE unfolded into its grid (SETTINGS_MOTION.md; the desktop's E3 on touch): its ten charms five a line under
    /// the row, each name beside its charm, the key logic kept (a pick flies the charm onto the key). Only one row is open, so it
    /// closes the open row and an opening row folds it. A tap on the key, the current name or the row's word unfolds it and folds
    /// it again; on the rail a tap on another charm still picks at once; in the grid a tap on a cell picks and the grid stays. With
    /// a pad or a keyboard, as the desktop: folded, ← → go along the rail and A picks the charm chosen there or, on the current one,
    /// unfolds; in the grid ← → ↑ ↓ choose (↑ ↓ past its edge fold it and go on), A picks, B folds.</summary>
    public bool LanguageUnfolded { get; private set; }
    /// <summary>The grid's height under the row in points (the view's measure: two lines of cells and the gap under them).</summary>
    public float LanguageGridHeight { get; set; } = 112f;
    /// <summary>The whole LANGUAGE row as drawn, grid included, in frame points (the view's), for the grid's taps.</summary>
    public Rect LanguageRowRect { get; set; }
    /// <summary>LanguageTagAt's code for the row's head (its word, the key, the current name) while the grid is out.</summary>
    public const int LanguageHead = -2;
    // The grid unfolding: 0.26 s + 1 s per 1500 pt, at most 0.52 s, ease-out quart; folding, the cells fade out (0.12 s), then the
    // row folds in 0.22 s, ease-in-out cubic (as a map row and the desktop's LANGUAGE).
    public const float UnfoldMin = .26f, UnfoldMax = .52f, UnfoldPerUnit = 1f / 1500f, FoldFadeS = .12f, FoldS = .22f;
    float langUnfoldAt = -100f, langFoldAt = -100f, langUnfoldFrom, langFoldFrom;
    /// <summary>CONTROLLER unfolded into its map (SETTINGS_MOTION.md, E4c: "On the phone the controller fills the 710 pt list"; the
    /// desktop's E2): the desktop's controller drawing in the row, a callout chosen by a tap or ← →, the chosen one tapped again (or
    /// A) listening for a button. Only one row is open. A tap on the row's word unfolds it and folds it again; ↑ ↓ or B fold it.</summary>
    public bool MapUnfolded { get; private set; }
    /// <summary>The controller's map (the view makes it on the first unfold) and where it is in frame points: the unfolded part of
    /// its row on screen, the drawing's origin (top-left), its height with its pads, the chosen callout.</summary>
    public FrontRoomsKeyMapView PadMap { get; set; }
    public Rect PadMapRect { get; set; }
    public Vector2 PadMapOrigin { get; set; }
    public float PadMapHeight { get; set; } = 300f;
    public Rect PadMapChosenRect { get; set; }
    float mapUnfoldAt = -100f, mapFoldAt = -100f, mapUnfoldFrom, mapFoldFrom;
    // The list follows the map's chosen callout once the view has placed it (a frame after ← → or a tap).
    bool followChosen;
    /// <summary>The view's measure of a row's open height at a list width (its sentence wraps, never cut); null: twice its first line.</summary>
    public Func<FrontRoomsSettingsMenu.Row, float, float> SettingsOpenHeight { get; set; }
    // A row opening in place (SETTINGS_MOTION.md; the constants are W/settings_motion/mock.py's, as FrontRoomsDesktopSettingsView):
    // 1x -> 2x in 0.24 s, ease-out quart, led by its bar; its second line from 0.09 s, each element in 0.18 s, 0.035 s after the
    // one before; a closing row's second line fades in 0.09 s, then the row folds in 0.18 s, ease-in-out cubic; the list follows
    // in 0.32 s; the name grows 1.4x (17 -> 24 pt). Reduced motion: heights jump and the second line fades in 0.12 s.
    public const float OpenS = .24f, CloseFadeS = .09f, CloseS = .18f, RevealAt = .09f, RevealS = .18f, Stagger = .035f, ScrollS = .32f,
        TitleGrow = .4f, ReducedFadeS = .12f;
    public float SettingsScroll { get; private set; }
    public float SettingsScrollMax { get; private set; }
    /// <summary>The settings tab shown (Red, 2026-10-24: the list in groups): GRAPHICS · COMFORT · CONTROLS · ASSIST.</summary>
    public FrontRoomsSettingsMenu.Tab SettingsTab { get; private set; } = FrontRoomsSettingsMenu.Tab.Graphics;
    /// <summary>The sheet is set aside to show the game at the new settings; a tap brings it back (Red, 2026-10-24).</summary>
    public bool SettingsPeek { get; private set; }
    /// <summary>The LANGUAGE strip on ASSIST's first row (Red, 2026-10-30). The view draws it and hands it here, with where it
    /// sits in frame points: wider than the list, so its taps come before the list's. Empty while it isn't showing.</summary>
    public FrontRoomsLanguageStrip LanguageStrip { get; set; }
    /// <summary>A chip's word as drawn, from the view. English keeps the authored widths; a longer language widens a chip to
    /// its word and its padding.</summary>
    public Func<Button, float> ChipWordWidth { get; set; }
    public Rect LanguageStripRect { get; set; }
    /// <summary>Where the view drew each tab and the GRAPHICS preview (points), for the hit test.</summary>
    public readonly Rect[] SettingsTabRects = new Rect[4];
    public Rect SettingsPreviewRect;
    public IReadOnlyList<SettingsSlot> SettingsSlots => slots;
    public float LastTouchTime { get; private set; }

    /// <summary>
    /// What the player plays with (Red, 2026-10-29: "在手机/ipad上都可以用手柄/键盘游玩，如果玩家用手机玩的话虚拟摇杆就不会显示"):
    /// touch, or a pad or a keyboard, which hide the stick, USE, RUN and pause. A touch on the move side brings them back;
    /// a look drag works either way (a keyboard's move with a finger's look, on an iPad).
    /// </summary>
    public enum InputMode { Touch, Gamepad, Keyboard }
    public InputMode Mode { get; private set; } = InputMode.Touch;
    public bool ExternalInput => Mode != InputMode.Touch;
    /// <summary>The handheld's input mode for other HUD parts (the key HUD's prompt names the pad's or keyboard's button too).</summary>
    public static InputMode CurrentMode { get; private set; } = InputMode.Touch;
    /// <summary>With a pad or a keyboard, the menu chip in focus (RESUME, END RUN, the question's chips, TRY AGAIN), or None;
    /// a focused settings row is <see cref="ActiveSettingsRow"/> (its yellow rule).</summary>
    public Button FocusChip { get; private set; } = Button.None;
    Vector2Int navHeld;
    float navRepeatAt;

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
    float frameZoom;
    bool frameZoomToggle;

    readonly List<SettingsSlot> slots = new List<SettingsSlot>();
    float scrollVelocity;
    // Each row's open and close (SetOpenRow); the list following an open row (FollowRow); whether the first ↑ ↓ opens the
    // focused row where it is (a fresh sheet or tab, as the desktop).
    struct RowOpen { public int Id; public bool IsOpen; public float OpenAt, CloseAt, OpenFrom, CloseFrom; }
    readonly List<RowOpen> rowOpens = new List<RowOpen>();
    float followFrom, followTo, followAt = -100f;
    bool following, openArmed = true;

    int moveId = -1, lookId = -1, useId = -1;
    bool shotBackArmed;
    Vector2 moveFinger;
    // EXAMINE: the second finger of a pinch (with lookId) and its last spread.
    int pinchId = -1;
    float pinchSpread;

#if UNITY_IOS || UNITY_ANDROID || UNITY_EDITOR
    enum Role { None, Move, Look, Use, Button, Scroll, Start, Pinch, PanelScroll, SliderDrag, TerminalPicture }

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
        // A scene load (TRY AGAIN, END RUN) makes a new layer in touch mode: the mode the HUD and the prompts read starts from
        // it, never from the last run's pad (2026-10-31).
        CurrentMode = Mode;
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
        if (!paused) return;
        ClearTouchState();
        SaveTaughtIfPending();
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
        ApplyUnfoldPause();
        // The pad or the keyboard in use went (2026-11-01): the stick, USE and pause come back at once; the game pauses the run.
        if (FrontRoomsInput.ChangeThisFrame == FrontRoomsInput.DeviceChange.InUseLost && Mode != InputMode.Touch) SetMode(InputMode.Touch);
        // Not while a row listens for a control (FrontRoomsBindCapture): a key pressed while the pad's list listens must not hand
        // the sheet to the keyboard and move the focus.
        if (!FrontRoomsBindCapture.Busy)
        {
            var device = FrontRoomsInput.ExternalActivity();
            if (device == FrontRoomsInput.Device.Gamepad) SetMode(InputMode.Gamepad);
            else if (device == FrontRoomsInput.Device.Keyboard) SetMode(InputMode.Keyboard);
        }
#if UNITY_IOS || UNITY_ANDROID || UNITY_EDITOR
        ProcessTouches();
#endif
        UpdateStick();
        AddGyroLook();
        UpdateSettingsSlots();
        UpdateScrollInertia();
        NavigateMenus();
        var snapshot = new FrontRoomsInput.FrameSnapshot(
            frameMove,
            frameLook,
            Running && !Winded,
            frameUseDown,
            useId >= 0,
            framePauseDown,
            frameStartDown,
            frameSettingsDown,
            frameRestartDown,
            frameShotBack,
            frameZoom,
            frameZoomToggle);
        FrontRoomsInput.SetVirtualSnapshot(ExternalInput ? MergePhysical(snapshot) : snapshot);
    }

    // ------------------------------------------------------------ pads and keyboards (Red, 2026-10-29)

    // The frame the mode last changed (2026-10-31): the press that hands the sheet to a pad or a keyboard only shows the focus
    // (SetMode focuses the first row in that frame), so its X or Y never resets or previews a row the player has not seen.
    int modeChangedFrame = -1;

    void SetMode(InputMode mode)
    {
        if (Mode == mode) return;
        Mode = mode;
        CurrentMode = mode;
        modeChangedFrame = Time.frameCount;
        FocusChip = Button.None;
        navHeld = Vector2Int.zero;
        // A pad or a keyboard taking the sheet enters CONTROLS' list again: its own list first (Red, 2026-11-01), focused (the
        // first row's id is the same in either order, so the focus lands on the new first list).
        if (mode != InputMode.Touch && menuState == MenuState.Settings && !RestartConfirmationOpen) { FrontRoomsSettingsMenu.LatchControlsOrder(); FocusFirstRow(); }
    }

    /// <summary>
    /// The pad's or keyboard's frame over the touch frame: the larger move (a pad's tilt is the gait: under 0.7 the slow,
    /// unheard walk), both looks (the pad's at LOOK SPEED and INVERT LOOK), either button. In END RUN's question, B or Esc
    /// answers CANCEL instead of leaving the sheet; on Android, Esc is Back and FrontRoomsMobileBackBridge owns it.
    /// </summary>
    FrontRoomsInput.FrameSnapshot MergePhysical(FrontRoomsInput.FrameSnapshot t)
    {
        // A row listening for a control, and the frame its capture ends in (2026-11-01, keylang SPEC §13 "capture leaks"): the
        // press being bound, or the Esc or MENU that cancelled, must not also pause, use or go back.
        if (FrontRoomsInput.SuspendGameplay || captureFrame == Time.frameCount) return t;
        var p = FrontRoomsInput.PhysicalFrame();
        var look = p.Look * (lookDegreesPerPoint / BaseLookDegreesPerPoint);
        if (invertLook) look.y = -look.y;
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        var escape = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
        var pause = p.PauseDown;
#if UNITY_ANDROID && !UNITY_EDITOR
        if (escape) pause = false;
#endif
        if (RestartConfirmationOpen && pause) { RestartCancelRequested?.Invoke(); pause = false; }
        var text = t.TextToggleDown;
        if (p.TextToggleDown && menuState == MenuState.Inspect && TextChipShown) TranscriptToggled?.Invoke();
        return new FrontRoomsInput.FrameSnapshot(
            p.Move.sqrMagnitude > t.Move.sqrMagnitude ? p.Move : t.Move,
            t.Look + look,
            t.SprintHeld || (p.SprintHeld && !Winded),
            t.UseDown || p.UseDown,
            t.UseHeld || p.UseHeld,
            t.PauseDown || pause,
            t.StartDown || p.StartDown,
            t.SettingsDown || p.SettingsDown,
            t.RestartDown || p.RestartDown,
            t.ShotBackDown || (p.ShotBackDown && menuState != MenuState.Settings),
            t.Zoom + p.Zoom,
            t.ZoomToggleDown || p.ZoomToggleDown,
            text,
            // SLOW on a phone's keyboard (hold Shift): the game sizes the move to the slow gait, as on a Mac.
            p.SlowHeld);
    }

    struct MenuNav
    {
        public Vector2Int Dir;
        public bool Confirm, Back, Reset, Preview;
        public int Tab;
    }

    /// <summary>D-pad, left stick, arrows or W A S D (held: again after 0.35 s, then every 0.12 s); A, Enter or Space;
    /// B (the keyboard's Esc is the game's pause key); X or Backspace (the row back to its default) and Y (GRAPHICS'
    /// preview), as on the desktop sheet since 2026-10-31; LB / RB, Q / E or Tab for the tabs.</summary>
    MenuNav ReadMenuNav()
    {
        var nav = new MenuNav();
        var pad = UnityEngine.InputSystem.Gamepad.current;
        var keys = UnityEngine.InputSystem.Keyboard.current;
        var held = Vector2.zero;
        if (pad != null)
        {
            held += pad.dpad.ReadValue();
            var stick = pad.leftStick.ReadValue();
            if (stick.sqrMagnitude > .25f) held += stick;
        }
        if (keys != null)
        {
            held.x += (keys.rightArrowKey.isPressed || keys.dKey.isPressed ? 1f : 0f) - (keys.leftArrowKey.isPressed || keys.aKey.isPressed ? 1f : 0f);
            held.y += (keys.upArrowKey.isPressed || keys.wKey.isPressed ? 1f : 0f) - (keys.downArrowKey.isPressed || keys.sKey.isPressed ? 1f : 0f);
        }
        var dir = Mathf.Abs(held.x) > Mathf.Abs(held.y) + .01f ? new Vector2Int(held.x > 0f ? 1 : -1, 0)
            : Mathf.Abs(held.y) > .01f ? new Vector2Int(0, held.y > 0f ? 1 : -1) : Vector2Int.zero;
        var now = FrontRoomsTouchMotion.Now;
        if (dir == Vector2Int.zero) navHeld = Vector2Int.zero;
        else if (dir != navHeld) { navHeld = dir; navRepeatAt = now + .35f; nav.Dir = dir; }
        else if (now >= navRepeatAt) { navRepeatAt = now + .12f; nav.Dir = dir; }
        nav.Confirm = (pad != null && pad.buttonSouth.wasPressedThisFrame)
            || (keys != null && (keys.enterKey.wasPressedThisFrame || keys.numpadEnterKey.wasPressedThisFrame || keys.spaceKey.wasPressedThisFrame));
        nav.Back = pad != null && pad.buttonEast.wasPressedThisFrame;
        // X (or a keyboard's Backspace) puts the focused row back to its default and Y opens GRAPHICS' preview, as on the
        // desktop sheet (2026-10-31).
        nav.Reset = (pad != null && pad.buttonWest.wasPressedThisFrame) || (keys != null && keys.backspaceKey.wasPressedThisFrame);
        nav.Preview = pad != null && pad.buttonNorth.wasPressedThisFrame;
        var next = (pad != null && pad.rightShoulder.wasPressedThisFrame) || (keys != null && (keys.eKey.wasPressedThisFrame || keys.tabKey.wasPressedThisFrame));
        var previous = (pad != null && pad.leftShoulder.wasPressedThisFrame) || (keys != null && keys.qKey.wasPressedThisFrame);
        nav.Tab = (next ? 1 : 0) - (previous ? 1 : 0);
        return nav;
    }

    // The frame a bind row's capture last read the devices (MergePhysical keeps that frame's presses out of the game).
    int captureFrame = -1;

    /// <summary>The menus with a pad or a keyboard: the sheet's rows and its RESUME / END RUN, END RUN's question, TRY AGAIN.</summary>
    void NavigateMenus()
    {
        // A row listening for a control reads the devices itself (FrontRoomsBindCapture, 2026-11-01): no menu button acts
        // meanwhile. Before the touch check: a touch player taps a pad row too.
        if (menuState == MenuState.Settings && FrontRoomsBindCapture.Busy)
        {
            captureFrame = Time.frameCount;
            // A direction held meanwhile (W being bound, a d-pad press) waits for its release once the capture ends, as the
            // desktop's map pages do (FrontRoomsInput.HoldPadMenu): it never moves the focus off the row just bound.
            ReadMenuNav();
            navRepeatAt = float.PositiveInfinity;
            if (FrontRoomsBindCapture.Update()) SettingsRowRequested?.Invoke(ActiveSettingsRow, RowExplain);
            return;
        }
        // CONTROLLER's map listening for a button or asking to swap reads the devices itself (B or MENU cancels, A swaps), as the
        // desktop's map page: no menu button acts meanwhile, and a direction held waits for its release.
        if (menuState == MenuState.Settings && PadMap != null && PadMap.Capturing)
        {
            ReadMenuNav();
            navRepeatAt = float.PositiveInfinity;
            PadMap.UpdateCapture();
            FrontRoomsInput.HoldPadMenu();
            return;
        }
        if (!ExternalInput) { FocusChip = Button.None; return; }
        var nav = ReadMenuNav();
        switch (menuState)
        {
            case MenuState.Settings when SettingsPeek:
                if (nav.Confirm || nav.Back || nav.Preview) SettingsPeek = false;
                return;
            case MenuState.Settings when RestartConfirmationOpen:
            {
                // CANCEL · RESTART on a line, QUIT TO TITLE under them.
                if (FocusChip != Button.RestartCancel && FocusChip != Button.RestartConfirm && FocusChip != Button.ReturnTitle) FocusChip = Button.RestartCancel;
                if (nav.Dir.x != 0 && FocusChip != Button.ReturnTitle) FocusChip = FocusChip == Button.RestartCancel ? Button.RestartConfirm : Button.RestartCancel;
                else if (nav.Dir.y < 0) FocusChip = Button.ReturnTitle;
                else if (nav.Dir.y > 0 && FocusChip == Button.ReturnTitle) FocusChip = Button.RestartCancel;
                if (nav.Confirm) PressChip(FocusChip);
                else if (nav.Back) PressChip(Button.RestartCancel);
                return;
            }
            case MenuState.Settings:
                NavigateSheet(nav);
                return;
            case MenuState.Caught:
                FocusChip = Button.TryAgain;
                if (nav.Confirm && TryAgainReady) PressChip(Button.TryAgain);
                return;
            default:
                FocusChip = Button.None;
                return;
        }
    }

    void NavigateSheet(MenuNav nav)
    {
        if (nav.Tab != 0)
        {
            var n = FrontRoomsSettingsMenu.Tabs.Length;
            SettingsTab = FrontRoomsSettingsMenu.Tabs[(((int)SettingsTab + nav.Tab) % n + n) % n];
            FrontRoomsSettingsMenu.LatchControlsOrder();
            SettingsScroll = 0f;
            scrollVelocity = 0f;
            FocusChip = Button.None;
            ActiveSettingsRow = -1;
            CloseAllRows();
            UpdateSettingsSlots();
            FocusFirstRow();
            FrontRoomsMobileInteractionEvents.MenuConfirmed();
            return;
        }
        // B or Esc closes the open row; a second resumes (SETTINGS_MOTION.md).
        if (nav.Back) { if (MapUnfolded) SetMapUnfolded(false); else if (LanguageUnfolded) SetLanguageUnfolded(false); else if (OpenSettingsRow >= 0) SetOpenRow(-1); else PressChip(Button.SettingsClose); return; }
        if (FocusChip == Button.Restart || FocusChip == Button.SettingsClose)
        {
            // The header: END RUN, RESUME (left to right); down goes back to the rows.
            if (nav.Dir.x != 0) FocusChip = FocusChip == Button.Restart ? Button.SettingsClose : Button.Restart;
            else if (nav.Dir.y < 0) { FocusChip = Button.None; FocusFirstRow(); openArmed = true; }
            if (nav.Confirm) PressChip(FocusChip);
            return;
        }
        if (SlotRow(ActiveSettingsRow) == null) FocusFirstRow();
        // LANGUAGE's grid has the pad and the keys: ↑ ↓ a line up or down, past its edge they fold it and go on to the next row;
        // ← → the next charm; A picks (the grid stays).
        var gridRow = SlotRow(ActiveSettingsRow);
        if (LanguageUnfolded && gridRow != null && gridRow.Kind == FrontRoomsSettingsMenu.Kind.Language && LanguageStrip != null)
        {
            if (nav.Dir.y != 0 && LanguageStrip.StepRow(nav.Dir.y < 0 ? 1 : -1)) return;
            if (nav.Dir.y != 0) SetLanguageUnfolded(false);
            else if (nav.Dir.x != 0) { LanguageStrip.Step(nav.Dir.x); return; }
            else if (nav.Confirm && Time.frameCount != modeChangedFrame) { LanguageStrip.PickHighlighted(); return; }
        }
        // CONTROLLER's map has the pad and the keys, as the desktop's map page: ← → the next callout, ↑ ↓ fold it and go on to the
        // next row, A listens for a button for the chosen one, X puts it back, Y presses DEFAULTS (twice puts every button back).
        var mapRow = SlotRow(ActiveSettingsRow);
        if (MapUnfolded && mapRow != null && mapRow.Key == FrontRoomsSettingsMenu.Key.PadMap && PadMap != null)
        {
            var fresh = Time.frameCount == modeChangedFrame;
            if (nav.Dir.y != 0) SetMapUnfolded(false);
            else if (nav.Dir.x != 0) { PadMap.Step(nav.Dir.x); followChosen = true; return; }
            else if (nav.Confirm && !fresh) { PadMap.BeginCapture(); return; }
            else if (nav.Reset && !fresh) { PadMap.ResetChosen(); return; }
            else if (nav.Preview && !fresh) { PressMapDefaults(); return; }
            else return;
        }
        if (nav.Dir.y != 0)
        {
            // The first ↑ ↓ on a fresh sheet or tab opens the focused row where it is; then each opens the next and closes the
            // last, and the list follows (SETTINGS_MOTION.md, as the desktop).
            if (openArmed && OpenSettingsRow < 0 && Opens(SlotRow(ActiveSettingsRow))) { openArmed = false; SetOpenRow(ActiveSettingsRow); return; }
            openArmed = false;
            var next = NextRow(ActiveSettingsRow, nav.Dir.y < 0 ? 1 : -1);
            if (next >= 0) { ActiveSettingsRow = next; SetOpenRow(next); FollowRow(next); }
            else if (nav.Dir.y > 0) { FocusChip = Button.SettingsClose; ActiveSettingsRow = -1; SetOpenRow(-1); }
            return;
        }
        var row = SlotRow(ActiveSettingsRow);
        if (row == null) return;
        // X: the row back to its default, then the game redraws the sheet as after a tap (RowExplain changes nothing itself);
        // Y on GRAPHICS: its preview. The header chips returned above, so neither acts on RESUME or END RUN; nor on the press
        // that switched the sheet to this pad or keyboard, which only shows the focus (2026-10-31).
        var handedOver = Time.frameCount == modeChangedFrame;
        // CONTROLLER's map row, as the desktop's: A or → unfolds it.
        if (row.Key == FrontRoomsSettingsMenu.Key.PadMap && !handedOver && (nav.Confirm || nav.Dir.x > 0)) { SetMapUnfolded(true); FollowRow(ActiveSettingsRow); return; }
        if (nav.Reset && !handedOver) { FrontRoomsSettingsMenu.ResetRow(row); ApplySettingsRow(ActiveSettingsRow, RowExplain); return; }
        if (nav.Preview && !handedOver && SettingsTab == FrontRoomsSettingsMenu.Tab.Graphics) { ApplySettingsRow(ActiveSettingsRow, RowPreview); return; }
        if (nav.Dir.x != 0)
        {
            if (row.Kind == FrontRoomsSettingsMenu.Kind.Switch) ApplySettingsRow(ActiveSettingsRow, RowActivate);
            else if (row.Kind == FrontRoomsSettingsMenu.Kind.Language) LanguageStrip?.Step(nav.Dir.x);
            else if (row.Kind == FrontRoomsSettingsMenu.Kind.Segments || row.Kind == FrontRoomsSettingsMenu.Kind.Slider)
            {
                var to = Mathf.Clamp(row.Index + nav.Dir.x, 0, (row.Options != null ? row.Options.Length : 1) - 1);
                if (to != row.Index) ApplySettingsRow(ActiveSettingsRow, RowSet + to);
            }
            return;
        }
        // A row B closed opens again before A works its control (as Enter on the desktop).
        if (nav.Confirm && !handedOver && Opens(row) && OpenSettingsRow != ActiveSettingsRow) { openArmed = false; SetOpenRow(ActiveSettingsRow); FollowRow(ActiveSettingsRow); return; }
        // A too (2026-11-01): the A that hands the sheet to a pad only shows the focus, so it never starts a bind row's capture.
        // LANGUAGE folded, as the desktop: A picks the charm chosen along the rail, or on the current one unfolds the grid.
        if (nav.Confirm && !handedOver && row.Kind == FrontRoomsSettingsMenu.Kind.Language)
        {
            if (LanguageStrip != null && LanguageStrip.HighlightedLanguage != FrontRoomsLanguage.Current) LanguageStrip.PickHighlighted();
            else { SetLanguageUnfolded(true); FollowRow(ActiveSettingsRow); }
        }
        else if (nav.Confirm && !handedOver) ApplySettingsRow(ActiveSettingsRow, row.Visual && row.Kind != FrontRoomsSettingsMenu.Kind.Switch ? RowPreview : RowActivate);
    }

    void FocusFirstRow()
    {
        var first = NextRow(-1, 1);
        ActiveSettingsRow = first;
        if (first >= 0) KeepRowInView(first);
    }

    /// <summary>The next settings row (not a header) after <paramref name="from"/> in list order, or -1.</summary>
    int NextRow(int from, int step)
    {
        var at = -1;
        for (var i = 0; i < slots.Count; i++) if (slots[i].Id == from && !slots[i].Header) { at = i; break; }
        for (var i = at < 0 ? (step > 0 ? 0 : slots.Count - 1) : at + step; i >= 0 && i < slots.Count; i += step)
            if (!slots[i].Header && slots[i].Row != null) return slots[i].Id;
        return -1;
    }

    /// <summary>Scrolls the list so a focused row is in view, at once (a fresh sheet or tab).</summary>
    void KeepRowInView(int id)
    {
        var view = SettingsScrollViewport;
        following = false;
        foreach (var s in slots)
        {
            if (s.Id != id || s.Header) continue;
            var top = s.Rect.yMax + SettingsScroll;
            var bottom = s.Rect.yMin + SettingsScroll;
            if (top > view.yMax) SettingsScroll = Mathf.Clamp(view.yMax - s.Rect.yMax, 0f, SettingsScrollMax);
            else if (bottom < view.yMin) SettingsScroll = Mathf.Clamp(view.yMin - s.Rect.yMin, 0f, SettingsScrollMax);
            scrollVelocity = 0f;
            return;
        }
    }

    /// <summary>A pad's or a keyboard's press of a menu chip: what a tap on it does.</summary>
    void PressChip(Button button)
    {
        switch (button)
        {
            case Button.Restart:
            case Button.RestartConfirm:
            case Button.TryAgain:
                frameRestartDown = true;
                break;
            case Button.SettingsClose:
                frameSettingsDown = true;
                break;
            case Button.RestartCancel:
                RestartCancelRequested?.Invoke();
                break;
            case Button.ReturnTitle:
                ReturnTitleRequested?.Invoke();
                break;
            default:
                return;
        }
        FrontRoomsMobileInteractionEvents.MenuConfirmed();
    }

    void BeginFrame()
    {
        frameMove = Vector2.zero;
        frameLook = Vector2.zero;
        frameUseDown = framePauseDown = frameStartDown = frameSettingsDown = frameRestartDown = frameShotBack = false;
        frameZoom = 0f;
        frameZoomToggle = false;
    }

    Vector2 lastLayoutPixels;
    Rect lastLayoutSafe;
    float lastLayoutScale;
    int lastDuoDisplay;
    bool pauseOnUnfold;

    // R6 (Red, 2026-10-29): unfolding the Duo mid-run opens the settings page, the pause, as the pause button would. A live
    // close-up is still mid-run (A43): the press leaves it first, as Esc does (no pause inside a close-up), then the next pauses.
    void ApplyUnfoldPause()
    {
        if (!pauseOnUnfold) return;
        if (menuState == MenuState.Inspect) { framePauseDown = true; return; }
        pauseOnUnfold = false;
        if (menuState == MenuState.Playing) framePauseDown = true;
    }

    /// <summary>One line per change of the view's geometry (a fold, a rotation, a resize): what the layout is built from.</summary>
    static void LogLayout(Vector2 pixels, Rect safePx, float scale)
    {
        var shape = FrontRoomsHandheld.Shape;
        var pt = pixels / scale;
        var sb = new System.Text.StringBuilder("[HandheldLayout] frame ");
        sb.Append(F(pt.x)).Append(" x ").Append(F(pt.y)).Append(" pt @").Append(scale.ToString("0.###"))
          .Append(" (display ").Append(FrontRoomsHandheld.DisplayScale.ToString("0.###")).Append(")")
          .Append(" interface ").Append(FrontRoomsHandheld.InterfaceOrientation)
          .Append(" safe L ").Append(F(safePx.xMin / scale)).Append(" R ").Append(F((pixels.x - safePx.xMax) / scale))
          .Append(" T ").Append(F((pixels.y - safePx.yMax) / scale)).Append(" B ").Append(F(safePx.yMin / scale))
          .Append(" bleed B ").Append(F(shape.BleedSafe.yMin)).Append(" T ").Append(F(pt.y - shape.BleedSafe.yMax))
          .Append(shape.CameraBandRemoved ? " (camera band removed)" : string.Empty)
          .Append(" corners BL ").Append(F(shape.Corners.x)).Append(" BR ").Append(F(shape.Corners.y))
          .Append(" TL ").Append(F(shape.Corners.z)).Append(" TR ").Append(F(shape.Corners.w));
        foreach (var r in shape.Cutouts)
            sb.Append(" | cutout x ").Append(F(r.x)).Append(" y ").Append(F(r.y)).Append(" w ").Append(F(r.width)).Append(" h ").Append(F(r.height));
        foreach (var r in FrontRoomsHandheld.DivisionRegionsPoints)
            sb.Append(" | division x ").Append(F(r.x)).Append(" y ").Append(F(r.y)).Append(" w ").Append(F(r.width)).Append(" h ").Append(F(r.height));
        Debug.Log(sb.ToString());
    }

    static string F(float v) => v.ToString("0.#");

    void RefreshLayout()
    {
        var scale = Mathf.Max(.01f, FrontRoomsHandheld.PointScale);
        var safePx = FrontRoomsHandheld.SafeAreaPixels;
        // The bleed safe area: iOS's, less a camera band it folded in. Whatever touches that camera gives way on its own.
        var safe = FrontRoomsHandheld.Shape.BleedSafe;
        var pixels = FrontRoomsHandheld.ScreenPixels;
        if (lastLayoutPixels != Vector2.zero && (pixels != lastLayoutPixels || safePx != lastLayoutSafe || !Mathf.Approximately(scale, lastLayoutScale)))
        {
            // Fingers belong to the old view geometry. A fold/resize cancels them without changing the game/menu.
            ClearTouchState();
            scrollVelocity = 0f;
        }
        if (pixels != lastLayoutPixels || safePx != lastLayoutSafe || !Mathf.Approximately(scale, lastLayoutScale)) LogLayout(pixels, safePx, scale);
        var duoDisplay = FrontRoomsHandheld.DuoDisplay;
        var midRun = menuState == MenuState.Playing || (menuState == MenuState.Inspect && CloseUpRunsLive);
        if (lastDuoDisplay == FrontRoomsHandheld.DuoOuter && duoDisplay == FrontRoomsHandheld.DuoInner && midRun) pauseOnUnfold = true;
        // An in-between window during the move (fills neither display) keeps the last display, so outer → ? → inner still counts.
        if (duoDisplay != FrontRoomsHandheld.DuoNone) lastDuoDisplay = duoDisplay;
        lastLayoutPixels = pixels;
        lastLayoutSafe = safePx;
        lastLayoutScale = scale;
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
        // A43: leaving a live close-up keeps the left finger on the stick. Backing away is one move, and the thumb is already
        // there when a threat ends the close-up. Every other change of state starts clean.
        var keepStick = menuState == MenuState.Inspect && state == MenuState.Playing && CloseUpRunsLive;
        if (state != MenuState.Inspect) CloseUpRunsLive = false;
        menuState = state;
        menuStateSince = FrontRoomsTouchMotion.Now;
        if (state != MenuState.Paused && state != MenuState.Settings) RestartConfirmationOpen = false;
        // The sheet opening enters CONTROLS' list: the scheme in hand first (Red, 2026-11-01); leaving it ends a capture.
        if (state == MenuState.Settings) { SettingsScroll = 0f; scrollVelocity = 0f; ActiveSettingsRow = -1; CloseAllRows(); FrontRoomsSettingsMenu.LatchControlsOrder(); }
        else FrontRoomsBindCapture.Cancel();
        SettingsPeek = false;
        if (state != MenuState.Playing) SaveTaughtIfPending();
        TranscriptScroll = 0f;
        if (keepStick) ClearTouchStateButStick(); else ClearTouchState();
    }

    // ------------------------------------------------------------ teaching (X2)

    const string TaughtSlowKey = "FrontRooms.Touch.Taught.Slow";
    const string TaughtRunKey = "FrontRooms.Touch.Taught.Run";
    static bool taughtSavePending;

    /// <summary>SLOW · UNHEARD is learned (a counted creep after the Relay's release): the stick's slot stops teaching it.</summary>
    public static bool TaughtSlow => PlayerPrefs.GetInt(TaughtSlowKey, 0) != 0;
    /// <summary>The RUN preview is learned (the first stick crossing, or 4 s of preview shown).</summary>
    public static bool TaughtRun => PlayerPrefs.GetInt(TaughtRunKey, 0) != 0;
    /// <summary>Seconds the RUN preview has shown in all (it retires at 4 s); clearing its flag starts it again.</summary>
    public static float PreviewShownFor;

    /// <summary>
    /// Marks a teaching word learned or not. Straight to PlayerPrefs (no FrontRoomsSettings.Changed, no rebuild); the
    /// Save waits for the next menu or app pause, never the retire frame. A lost flag only shows a hint again.
    /// </summary>
    public static void SetTaught(bool slow, bool learned)
    {
        PlayerPrefs.SetInt(slow ? TaughtSlowKey : TaughtRunKey, learned ? 1 : 0);
        if (!slow && !learned) PreviewShownFor = 0f;
        taughtSavePending = true;
    }

    /// <summary>STICK HINTS reads ON while a word this run mode can show is unlearned (BUTTON has no preview).</summary>
    public static bool StickHintsOn => !TaughtSlow || (FrontRoomsSettings.TouchSprintSocket && !TaughtRun);

    static void SaveTaughtIfPending()
    {
        if (!taughtSavePending) return;
        taughtSavePending = false;
        PlayerPrefs.Save();
    }

    /// <summary>
    /// END RUN's 10-24 question (END THIS RUN? CANCEL · RESTART / QUIT TO TITLE). The game no longer opens it: since Red's
    /// 2026-10-30 "在回退标题前不用确认一下" END RUN goes back to the title at once. Kept, with its layout tests, for a question
    /// that may come back.
    /// </summary>
    public void SetRestartConfirmation(bool open)
    {
        RestartConfirmationOpen = open && (menuState == MenuState.Paused || menuState == MenuState.Settings);
    }

    /// <summary>USE's centre now: in a close-up USE is BACK, moved to the lower right, clear of the transcript panel and
    /// the framed plan (contract T2).</summary>
    public Vector2 UseCenterNow => menuState == MenuState.Inspect ? Layout.InspectBackCenter : Layout.UseCenter;
    /// <summary><see cref="UseCenterNow"/> before it gives way to a camera or a rounded corner.</summary>
    public Vector2 UseCenterNowBase => menuState == MenuState.Inspect ? Layout.InspectBackCenterBase : Layout.UseCenterBase;

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
        if (menuState == MenuState.Inspect)
        {
            // A close-up: the hidden stick's pull-back is the desktop S (it leaves); nothing walks or runs.
            var back = offset.y < -L.ShotBackDepth;
            if (back && !shotBackArmed) { frameShotBack = true; shotBackArmed = true; }
            else if (!back) shotBackArmed = false;
            StickThumb = StickOrigin;
            CurrentGait = FrontRoomsGait.Still;
            RunReach = 0f;
            return;
        }
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
        var list = L.SettingsListRect;
        const float header = 30f;
        var rows = FrontRoomsSettingsMenu.Rows(SettingsTab);
        var tabBase = 1000 + (int)SettingsTab * 100;
        var now = FrontRoomsTouchMotion.Now;
        var m = FrontRoomsSettingsMenu.Touch;
        var y = list.yMax;
        for (var i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            // A sheet with the GRAPHICS picture reads the frame rate in its corner, as the desktop does (Red, 2026-11-01); the
            // phone's list keeps the row, and its second line draws the frame times.
            if (r.Kind == FrontRoomsSettingsMenu.Kind.Meter && L.SettingsPictures) continue;
            if (r.Kind == FrontRoomsSettingsMenu.Kind.Header)
            {
                slots.Add(new SettingsSlot { Id = -1, Header = true, Label = r.Label, Rect = new Rect(list.x, y - header, list.width, header), Rest = header });
                y -= header;
                continue;
            }
            // Half padding was for the 418 pt list (Row.Compact): a list with room for the whole padding gives it back, so the
            // open row's notes fit their choices. Every frame after Rows set it, so the hit test and the view agree.
            if (r.Compact)
            {
                r.Compact = false;
                if (FrontRoomsSettingsMenu.TextWidth(r.Label, m.Label, false) + 22f + FrontRoomsSettingsMenu.ControlWidth(r, m) > list.width) r.Compact = true;
            }
            var id = tabBase + i;
            // LANGUAGE is as tall as its keychain strip, so the strip never hangs over the next row. An open row grows under its
            // first line to its open height (SETTINGS_MOTION.md): the rows below move down, nothing inside it moves.
            var rest = FrontRoomsTouchLayout.SettingsRowHeightOf(r);
            var motion = MotionOf(id);
            var open = OpenAmount(motion, now);
            var h = open > 0f ? Mathf.Lerp(rest, OpenHeightOf(r, list.width), open) : rest;
            // LANGUAGE grows under its head by its grid's height as the grid unfolds.
            var fold = r.Kind == FrontRoomsSettingsMenu.Kind.Language ? LanguageFoldAmount(now) : 0f;
            if (fold > 0f) h = rest + LanguageGridHeight * fold;
            // CONTROLLER's map row grows by its map's height as the map unfolds.
            var mapFold = r.Key == FrontRoomsSettingsMenu.Key.PadMap ? MapFoldAmount(now) : 0f;
            if (mapFold > 0f) h = rest + PadMapHeight * mapFold;
            slots.Add(new SettingsSlot { Id = id, Label = r.Label, Value = r.ValueText, Row = r, Rect = new Rect(list.x, y - h, list.width, h),
                Rest = rest, Open = open, IsOpen = motion.IsOpen, OpenAt = motion.OpenAt, CloseAt = motion.CloseAt, Fold = Mathf.Max(fold, mapFold) });
            y -= h;
        }
        // The list runs to the bottom edge (Red, 2026-10-24); its last row scrolls up clear of the home indicator.
        var visible = list.height - L.SettingsBottomInset;
        SettingsScrollMax = Mathf.Max(0f, (list.yMax - y) - visible);
        SettingsScroll = Mathf.Clamp(SettingsScroll, 0f, SettingsScrollMax);
        // A row that has folded leaves the bookkeeping.
        for (var i = rowOpens.Count - 1; i >= 0; i--)
            if (!rowOpens[i].IsOpen && now - rowOpens[i].CloseAt > CloseFadeS + CloseS) rowOpens.RemoveAt(i);
        if (followChosen && PadMapChosenRect.height > 0f) { followChosen = false; FollowFrame(PadMapChosenRect); }
    }

    // ------------------------------------------------------------ a row opens in place (SETTINGS_MOTION.md)

    /// <summary>A row that opens: a choice, a switch, a slider, a button, the frame rate or a gesture's row, with something to
    /// say (the explanation column is gone, so a read-only row opens to say what it does). LANGUAGE unfolds instead.</summary>
    public static bool Opens(FrontRoomsSettingsMenu.Row r) => r != null
        && (r.Kind == FrontRoomsSettingsMenu.Kind.Segments || r.Kind == FrontRoomsSettingsMenu.Kind.Switch || r.Kind == FrontRoomsSettingsMenu.Kind.Slider
            || r.Kind == FrontRoomsSettingsMenu.Kind.Action || r.Kind == FrontRoomsSettingsMenu.Kind.Meter || r.Kind == FrontRoomsSettingsMenu.Kind.Keys)
        && (!string.IsNullOrEmpty(r.Explain) || r.Notes != null);

    /// <summary>Opens row <paramref name="id"/> in place and closes the one open before it (only one is open); -1, or a row
    /// that does not open, closes it.</summary>
    void SetOpenRow(int id)
    {
        if (!Opens(SlotRow(id))) id = -1;
        if (id >= 0) { SetLanguageUnfolded(false); SetMapUnfolded(false); }
        if (id == OpenSettingsRow) return;
        var now = FrontRoomsTouchMotion.Now;
        var at = -1;
        for (var i = 0; i < rowOpens.Count; i++)
        {
            var o = rowOpens[i];
            if (o.Id == id) at = i;
            if (!o.IsOpen) continue;
            o.CloseFrom = OpenAmount(o, now);
            o.IsOpen = false;
            o.CloseAt = now;
            rowOpens[i] = o;
        }
        if (id >= 0)
        {
            var open = new RowOpen { Id = id, IsOpen = true, OpenAt = now, OpenFrom = at >= 0 ? OpenAmount(rowOpens[at], now) : 0f, CloseAt = -100f };
            if (at >= 0) rowOpens[at] = open; else rowOpens.Add(open);
        }
        OpenSettingsRow = id;
    }

    /// <summary>No row open and none moving, the next ↑ ↓ opening the focused row where it is: a fresh sheet or tab.</summary>
    void CloseAllRows()
    {
        rowOpens.Clear();
        OpenSettingsRow = -1;
        openArmed = true;
        following = false;
        // and LANGUAGE's grid, at once.
        LanguageUnfolded = false;
        langFoldFrom = 0f;
        langFoldAt = langUnfoldAt = -100f;
        LanguageStrip?.SetGrid(false, true);
        // and CONTROLLER's map.
        MapUnfolded = false;
        mapFoldFrom = 0f;
        mapFoldAt = mapUnfoldAt = -100f;
    }

    /// <summary>LANGUAGE's grid out or back, in motion, the strip with it; out, it closes the open row.</summary>
    void SetLanguageUnfolded(bool on)
    {
        if (on == LanguageUnfolded) return;
        var now = FrontRoomsTouchMotion.Now;
        var amount = LanguageFoldAmount(now);
        LanguageUnfolded = on;
        if (on) { SetOpenRow(-1); SetMapUnfolded(false); langUnfoldAt = now; langUnfoldFrom = amount; }
        else { langFoldAt = now; langFoldFrom = amount; }
        LanguageStrip?.SetGrid(on);
    }

    /// <summary>How far LANGUAGE's grid is out, 0..1. Reduced motion: it jumps.</summary>
    public float LanguageFoldAmount(float now)
    {
        if (FrontRoomsHandheld.ReduceMotion) return LanguageUnfolded ? 1f : 0f;
        if (LanguageUnfolded) return Mathf.Lerp(langUnfoldFrom, 1f, OutQuart((now - langUnfoldAt) / Mathf.Min(UnfoldMax, UnfoldMin + LanguageGridHeight * UnfoldPerUnit)));
        return langFoldFrom * (1f - FrontRoomsTouchMotion.Evaluate(FrontRoomsTouchMotion.Ease.InOutCubic, Mathf.Clamp01((now - langFoldAt - FoldFadeS) / FoldS)));
    }

    /// <summary>CONTROLLER's map out or back, in motion (the view plays its reveal and its fade); out, it closes the open row and
    /// folds LANGUAGE's grid.</summary>
    void SetMapUnfolded(bool on)
    {
        if (on == MapUnfolded) return;
        var now = FrontRoomsTouchMotion.Now;
        var amount = MapFoldAmount(now);
        MapUnfolded = on;
        if (on) { SetOpenRow(-1); SetLanguageUnfolded(false); mapUnfoldAt = now; mapUnfoldFrom = amount; }
        else { mapFoldAt = now; mapFoldFrom = amount; }
    }

    /// <summary>How far CONTROLLER's map is out, 0..1, on LANGUAGE's curves (the graph's own height). Reduced motion: it jumps.</summary>
    public float MapFoldAmount(float now)
    {
        if (FrontRoomsHandheld.ReduceMotion) return MapUnfolded ? 1f : 0f;
        if (MapUnfolded) return Mathf.Lerp(mapUnfoldFrom, 1f, OutQuart((now - mapUnfoldAt) / Mathf.Min(UnfoldMax, UnfoldMin + PadMapHeight * UnfoldPerUnit)));
        return mapFoldFrom * (1f - FrontRoomsTouchMotion.Evaluate(FrontRoomsTouchMotion.Ease.InOutCubic, Mathf.Clamp01((now - mapFoldAt - FoldFadeS) / FoldS)));
    }

    /// <summary>A frame point in the map's own units (its top-left origin, y down), as the desktop's MapPoint.</summary>
    Vector2 PadMapPoint(Vector2 p) => new Vector2(p.x - PadMapOrigin.x, PadMapOrigin.y - p.y) / Mathf.Max(.01f, PadMap != null ? PadMap.Scale : 1f);

    /// <summary>Y on the map: the CONTROLLER list's DEFAULTS row (a first press asks, a second within 3 s puts every button back).</summary>
    void PressMapDefaults()
    {
        foreach (var s in slots)
            if (!s.Header && s.Row != null && s.Row.Key == FrontRoomsSettingsMenu.Key.PadDefaults) { FrontRoomsSettingsMenu.Activate(s.Row); break; }
        PadMap?.Refresh(true);
    }

    /// <summary>The list follows to show a rect on screen whole (0.32 s, ease-in-out): the map's chosen callout.</summary>
    void FollowFrame(Rect r)
    {
        var view = SettingsScrollViewport;
        var bottom = view.yMin + Layout.SettingsBottomInset;
        var target = SettingsScroll;
        if (r.yMax > view.yMax) target -= r.yMax - view.yMax;
        else if (r.yMin < bottom) target += bottom - r.yMin;
        target = Mathf.Clamp(target, 0f, SettingsScrollMax);
        if (Mathf.Abs(target - SettingsScroll) < .5f) return;
        followFrom = SettingsScroll;
        followTo = target;
        followAt = FrontRoomsTouchMotion.Now;
        following = true;
        scrollVelocity = 0f;
    }

    /// <summary>The LANGUAGE row's slot id on this tab, or -1.</summary>
    int LanguageRowId()
    {
        foreach (var s in slots) if (!s.Header && s.Row != null && s.Row.Kind == FrontRoomsSettingsMenu.Kind.Language) return s.Id;
        return -1;
    }

    RowOpen MotionOf(int id)
    {
        foreach (var o in rowOpens) if (o.Id == id) return o;
        return new RowOpen { Id = id, OpenAt = -100f, CloseAt = -100f };
    }

    // How far a row is open, 0..1: it opens in 0.24 s, ease-out quart, from wherever it was; closing, it waits for its second line
    // to fade (0.09 s), then folds in 0.18 s, ease-in-out cubic. Reduced motion: it jumps.
    static float OpenAmount(RowOpen o, float now)
    {
        if (FrontRoomsHandheld.ReduceMotion) return o.IsOpen ? 1f : 0f;
        if (o.IsOpen) return Mathf.Lerp(o.OpenFrom, 1f, OutQuart((now - o.OpenAt) / OpenS));
        return o.CloseFrom * (1f - FrontRoomsTouchMotion.Evaluate(FrontRoomsTouchMotion.Ease.InOutCubic, Mathf.Clamp01((now - o.CloseAt - CloseFadeS) / CloseS)));
    }

    public static float OutQuart(float t) { t = Mathf.Clamp01(t); var u = 1f - t; return 1f - u * u * u * u; }

    float OpenHeightOf(FrontRoomsSettingsMenu.Row r, float width) =>
        SettingsOpenHeight != null ? SettingsOpenHeight(r, width) : 2f * FrontRoomsTouchLayout.SettingsRowHeightOf(r);

    /// <summary>The list follows to show the whole row <paramref name="id"/> at the height it is going to, with the section header
    /// that heads it (0.32 s, ease-in-out): it moves only as far as the row needs, and a finger on the list takes it over.</summary>
    void FollowRow(int id)
    {
        var view = SettingsScrollViewport;
        var shown = view.height - Layout.SettingsBottomInset;
        var y = view.yMax;
        var top = float.NaN;
        var bottom = 0f;
        for (var i = 0; i < slots.Count; i++)
        {
            var s = slots[i];
            var h = s.Header ? s.Rest : s.Id == OpenSettingsRow ? OpenHeightOf(s.Row, s.Rect.width)
                : s.Row != null && s.Row.Kind == FrontRoomsSettingsMenu.Kind.Language && LanguageUnfolded ? s.Rest + LanguageGridHeight
                : s.Row != null && s.Row.Key == FrontRoomsSettingsMenu.Key.PadMap && MapUnfolded ? s.Rest + PadMapHeight : s.Rest;
            if (!s.Header && s.Id == id)
            {
                top = y;
                bottom = y - h;
                // The header goes with its row when both fit.
                if (i > 0 && slots[i - 1].Header && h + slots[i - 1].Rest <= shown) top += slots[i - 1].Rest;
            }
            y -= h;
        }
        if (float.IsNaN(top)) return;
        var max = Mathf.Max(0f, view.yMax - y - shown);
        var target = SettingsScroll;
        if (bottom + target < view.yMax - shown) target = view.yMax - shown - bottom;
        if (top + target > view.yMax) target = view.yMax - top;
        target = Mathf.Clamp(target, 0f, max);
        if (Mathf.Abs(target - SettingsScroll) < .5f) return;
        followFrom = SettingsScroll;
        followTo = target;
        followAt = FrontRoomsTouchMotion.Now;
        following = true;
        scrollVelocity = 0f;
    }

    void UpdateScrollInertia()
    {
        // The list follows an open row (FollowRow) until a finger takes it.
        if (following)
        {
            var t = FrontRoomsHandheld.ReduceMotion ? 1f : (FrontRoomsTouchMotion.Now - followAt) / ScrollS;
            SettingsScroll = Mathf.Clamp(Mathf.Lerp(followFrom, followTo, FrontRoomsTouchMotion.Evaluate(FrontRoomsTouchMotion.Ease.InOutCubic, Mathf.Clamp01(t))), 0f, SettingsScrollMax);
            if (t >= 1f) following = false;
            return;
        }
#if UNITY_IOS || UNITY_ANDROID || UNITY_EDITOR
        foreach (var f in fingers.Values) if (f.Role == Role.Scroll) return;
#endif
        if (Mathf.Abs(scrollVelocity) < 1f) { scrollVelocity = 0f; return; }
        SettingsScroll = Mathf.Clamp(SettingsScroll + scrollVelocity * FrontRoomsTouchMotion.Delta, 0f, SettingsScrollMax);
        scrollVelocity *= Mathf.Exp(-FrontRoomsTouchMotion.Delta / .22f);
    }

    /// <summary>Which settings slots share the scrolling viewport on the current window.</summary>
    public bool SettingsSlotScrolls(SettingsSlot slot) => true;

    /// <summary>The scrolling settings area in points. Compact windows scroll both sections.</summary>
    public Rect SettingsScrollViewport
    {
        get
        {
            return Layout.SettingsListRect;
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
            case TouchRow.StickHints: return "STICK HINTS";
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
            case TouchRow.StickHints: return StickHintsOn ? "ON" : "OFF";
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
            case TouchRow.StickHints:
            {
                // ON clears both words, OFF retires both (X2 §3.5).
                var learned = StickHintsOn;
                SetTaught(true, learned);
                SetTaught(false, learned);
                break;
            }
            case TouchRow.ControlsSize: FrontRoomsSettings.StepTouchControlsScale(direction); break;
            case TouchRow.ControlsOpacity: FrontRoomsSettings.StepTouchOpacity(direction); break;
            case TouchRow.LeftHanded: FrontRoomsSettings.SetTouchLeftHanded(!FrontRoomsSettings.TouchLeftHanded); break;
            case TouchRow.HapticsGameplay: FrontRoomsSettings.SetTouchHaptics(!FrontRoomsSettings.TouchHaptics); break;
            case TouchRow.HapticsControls: FrontRoomsSettings.SetTouchHapticsControls(!FrontRoomsSettings.TouchHapticsControls); break;
        }
    }

    // ------------------------------------------------------------- menu rects

    /// <summary>A menu chip's centre and size (points) for the current state, or a zero size when it is not shown.</summary>
    /// <summary>A menu chip's place once it has given way to a camera or a rounded corner (hit-tested here, drawn by the view).</summary>
    public bool TryGetChip(Button button, out Vector2 center, out Vector2 size)
    {
        if (!TryGetChipBase(button, out center, out size)) return false;
        var rect = new Rect(center - size * .5f, size);
        // RESUME is anchored at a screen corner like the HUD (rounded-corner option 1): 16 pt from the curve. It steps in
        // sideways only, so it stays on the header line with END RUN and the tabs (Red, 2026-10-24: on the Duo it sat
        // lower than END RUN), and END RUN moves with it, 12 pt beside it. The other chips only move sideways.
        if (button == Button.SettingsClose) rect = ClearHeaderCorner(rect);
        else if (button == Button.Restart && menuState == MenuState.Settings && TryGetChipBase(Button.SettingsClose, out var resumeCenter, out var resumeSize))
        {
            var resume = new Rect(resumeCenter - resumeSize * .5f, resumeSize);
            var clearedResume = ClearHeaderCorner(resume);
            rect.x += clearedResume.x - resume.x;
            rect = FrontRoomsHandheld.ClearRect(rect);
            var rightLimit = clearedResume.xMin - 12f;
            if (rect.xMax > rightLimit)
            {
                // Camera clearance must not move END RUN into RESUME's touch target.
                rect.x = rightLimit - rect.width;
                var cutouts = FrontRoomsHandheld.Shape.Cutouts;
                for (var pass = 0; pass < cutouts.Length; pass++)
                {
                    var previousX = rect.x;
                    foreach (var cutout in cutouts)
                    {
                        var keep = new Rect(cutout.xMin - 12f, cutout.yMin - 12f,
                            cutout.width + 24f, cutout.height + 24f);
                        if (keep.Overlaps(rect)) rect.x = keep.xMin - rect.width;
                    }
                    if (rect.x == previousX) break;
                }
            }
        }
        else rect = FrontRoomsHandheld.ClearRect(rect);
        center = rect.center;
        return true;
    }

    static Rect ClearHeaderCorner(Rect rect) => FrontRoomsHandheld.ClearRect(rect, false, 12f, FrontRoomsHandheld.HudCornerGap);

    /// <summary>A menu chip's authored place, before it gives way.</summary>
    public bool TryGetChipBase(Button button, out Vector2 center, out Vector2 size)
    {
        var L = Layout;
        center = Vector2.zero;
        size = Vector2.zero;
        var h = FrontRoomsTouchLayout.ChipHeight;
        switch (menuState)
        {
            case MenuState.Paused when !RestartConfirmationOpen:
                if (button == Button.Resume) { center = L.PauseActionsCenter + Vector2.left * 96.5f; size = new Vector2(75f, h); return true; }
                if (button == Button.Settings) { center = L.PauseActionsCenter + Vector2.left * 1.5f; size = new Vector2(83f, h); return true; }
                if (button == Button.Restart) { center = L.PauseActionsCenter + Vector2.right * 95.5f; size = new Vector2(79f, h); return true; }
                // QUIT TO TITLE: centred 16 pt under the row, as wide as Figma's hug (平面视觉, 2026-10-08).
                if (button == Button.ReturnTitle) { center = L.PauseActionsCenter + Vector2.down * Mathf.Max(48f, 56f * L.MenuVerticalScale); size = new Vector2(109f, h); return true; }
                return false;
            case MenuState.Paused when RestartConfirmationOpen:
                if (button == Button.RestartCancel) { center = L.PauseActionsCenter + Vector2.left * 46f; size = new Vector2(74f, h); return true; }
                if (button == Button.RestartConfirm) { center = L.PauseActionsCenter + Vector2.right * 46f; size = new Vector2(79f, h); return true; }
                return false;
            case MenuState.Settings when RestartConfirmationOpen:
                // END RUN's question, where the pause card asked it: CANCEL · RESTART, and QUIT TO TITLE centred under them.
                if (button == Button.RestartCancel) { center = L.PauseActionsCenter + Vector2.left * 46f; size = new Vector2(74f, h); return true; }
                if (button == Button.RestartConfirm) { center = L.PauseActionsCenter + Vector2.right * 46f; size = new Vector2(79f, h); return true; }
                if (button == Button.ReturnTitle) { center = L.PauseActionsCenter + Vector2.down * Mathf.Max(48f, 56f * L.MenuVerticalScale); size = new Vector2(109f, h); return true; }
                return false;
            case MenuState.Settings:
                {
                    // The sheet is the pause (Red, 2026-10-24): RESUME (the old BACK) in the card's top-right corner, END RUN
                    // beside it; END RUN asks before a restart or a return to the title.
                    var card = L.SettingsCardSize;
                    var resume = L.SettingsCardCenter + new Vector2(card.x * .5f - 24f - 37.5f, card.y * .5f - 8f - 20f);
                    // Both grow towards the tabs: RESUME's right edge stays.
                    var resumeWidth = ChipWidth(Button.SettingsClose, 75f);
                    resume.x += 37.5f - resumeWidth * .5f;
                    if (button == Button.SettingsClose) { center = resume; size = new Vector2(resumeWidth, h); return true; }
                    // 12 pt apart: their 6 pt hit margins meet without overlapping.
                    var endWidth = ChipWidth(Button.Restart, 82f);
                    if (button == Button.Restart) { center = resume + Vector2.left * (resumeWidth * .5f + 12f + endWidth * .5f); size = new Vector2(endWidth, h); return true; }
                }
                return false;
            case MenuState.Caught:
                // 10 pt lower than before: the caught card's subtitle pushed its block down (FIX_CARD_VL217 A7).
                if (button == Button.TryAgain) { center = L.FromMaster(437f, L.CompactMenus ? 338f : 300f); size = new Vector2(ChipWidth(Button.TryAgain, 91f), h); return true; }
                return false;
        }
        return false;
    }

    float ChipWidth(Button button, float authored) =>
        FrontRoomsLanguage.IsEnglish || ChipWordWidth == null ? authored : Mathf.Max(authored, ChipWordWidth(button) + 26f);

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

    /// <summary>Direction codes for a settings row tap: 2 flips or runs it, 10 + n sets option or stop n, 0 only opens it (a
    /// tap on its first line off the control; on the open row it closes it), -3 a tap on an open row's second line off its
    /// choices (it stays open), -2 opens the full-screen preview (a picture row's second line on a sheet without the picture).</summary>
    public const int RowExplain = 0, RowActivate = 2, RowSet = 10, RowPreview = -2, RowLine = -3;

    bool SettingsRowAt(Vector2 p, out int id, out int direction)
    {
        id = -1;
        direction = RowExplain;
        var viewport = SettingsScrollViewport;
        if (!viewport.Contains(p)) return false;
        foreach (var s in slots)
        {
            if (s.Header) continue;
            var rect = s.Rect;
            rect.y += SettingsScroll;
            if (!rect.Contains(p)) continue;
            id = s.Id;
            var r = s.Row;
            if (r == null) return true;
            // The control sits against the row's right edge where it is drawn (narrowed for a camera or a corner), on its first
            // line. An open row's second line holds a note under each choice, so a tap there picks that choice too (as a click on
            // the desktop); a switch or a button works only on its first line.
            var drawn = FrontRoomsHandheld.ClearRect(rect, true);
            var fromRight = drawn.xMax - p.x;
            var second = p.y < rect.yMax - s.Rest;
            var off = second ? RowLine : RowExplain;
            var m = FrontRoomsSettingsMenu.Touch;
            switch (r.Kind)
            {
                case FrontRoomsSettingsMenu.Kind.Switch:
                case FrontRoomsSettingsMenu.Kind.Action:
                    direction = !second && fromRight <= FrontRoomsSettingsMenu.ControlWidth(r, m) + 12f ? RowActivate : off;
                    break;
                case FrontRoomsSettingsMenu.Kind.Segments:
                {
                    var seg = FrontRoomsSettingsMenu.SegmentAt(r, fromRight, m);
                    direction = seg >= 0 ? RowSet + seg : off;
                    break;
                }
                case FrontRoomsSettingsMenu.Kind.Slider:
                {
                    // The whole track is a target, and a tap on it jumps to the nearest stop (no drag needed, XAG 107).
                    var stop = FrontRoomsSettingsMenu.StopAt(r, fromRight, m, 16f);
                    direction = stop >= 0 ? RowSet + stop : off;
                    break;
                }
                default:
                    direction = off;
                    break;
            }
            // A sheet without the GRAPHICS picture (the phone): an open picture row's second line shows the game full screen.
            if (direction == RowLine && r.Visual && !Layout.SettingsPictures) direction = RowPreview;
            return true;
        }
        return false;
    }

    bool SettingsTabAt(Vector2 p, out int tab)
    {
        tab = -1;
        for (var i = 0; i < SettingsTabRects.Length; i++)
        {
            var r = SettingsTabRects[i];
            if (r.width <= 0f) continue;
            // 44 pt tall targets around each word (Apple HIG).
            var hit = new Rect(r.x - 8f, r.center.y - 22f, r.width + 16f, 44f);
            if (hit.Contains(p)) { tab = i; return true; }
        }
        return false;
    }

    FrontRoomsSettingsMenu.Row SlotRow(int id)
    {
        foreach (var s in slots) if (s.Id == id && !s.Header) return s.Row;
        return null;
    }

    /// <summary>Applies a settings row tap (or a slider's new stop); the view and the game read the result.</summary>
    void ApplySettingsRow(int id, int direction)
    {
        var r = SlotRow(id);
        ActiveSettingsRow = id;
        if (r != null)
        {
            if (direction == RowPreview) { SettingsPeek = true; return; }
            if (direction == RowActivate) FrontRoomsSettingsMenu.Activate(r);
            else if (direction >= RowSet && direction - RowSet != r.Index) FrontRoomsSettingsMenu.Set(r, direction - RowSet);
        }
        SettingsRowRequested?.Invoke(id, direction);
    }

    /// <summary>The LANGUAGE tag under a point in frame points, or -1.</summary>
    /// <summary>The LANGUAGE charm under a point in frame points: on the rail its tag (the key and the current name are the current
    /// language); with the grid out the cell under it, or LanguageHead on the row's head; -1 off it.</summary>
    int LanguageTagAt(Vector2 p)
    {
        if (LanguageStrip == null) return -1;
        if (!LanguageStrip.Grid) return LanguageStripRect.Contains(p) ? LanguageStrip.At(p.x - LanguageStripRect.x) : -1;
        if (!LanguageRowRect.Contains(p)) return -1;
        var local = LanguageLocal(p);
        return local.y < LanguageStripRect.height ? LanguageHead : LanguageStrip.AtCell(local);
    }

    /// <summary>A frame point in the strip's own units (its top-left origin, y down).</summary>
    Vector2 LanguageLocal(Vector2 p) => new Vector2(p.x - LanguageStripRect.x, LanguageStripRect.yMax - p.y);

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
                // A finger is no control to bind (2026-11-01): a touch anywhere ends a listening row; the tap still does what it does.
                if (FrontRoomsBindCapture.Busy) FrontRoomsBindCapture.Cancel();
                if (PadMap != null && PadMap.Capturing) PadMap.CancelCapture();
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
        // A touch takes the controls back, except a look drag while playing (a pad's or keyboard's move with a finger's look).
        if (ExternalInput && (!((menuState == MenuState.Playing || menuState == MenuState.Inspect) && L.InLookZone(p)) || OnTerminalPicture(p))) SetMode(InputMode.Touch);
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
                else if (CurrentUsePrompt.AllowsPress && useId < 0 && FrontRoomsTouchLayout.Contains(UseCenterNow, L.UseHitRadius, p))
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
            case MenuState.Inspect:
                // A close-up (contract T1): USE is BACK; the left zone is a hidden stick whose pull-back is the desktop S;
                // the right zone drags to look and pinches to zoom; a tap anywhere opens the full view and closes it. No pause button
                // (BACK, the pull-back and Android Back all leave) and no RUN button. The TEXT chip sits in the pause slot when the
                // close-up has a transcript (T6); a drag that starts on the panel scrolls it, anywhere else it looks (T7).
                if (TextChipShown && FrontRoomsTouchLayout.Contains(L.TextChipCenter, new Vector2(L.TextChipHitSize, L.TextChipHitSize), p))
                {
                    f.Role = Role.Button;
                    f.Button = Button.TextChip;
                }
                else if (CurrentUsePrompt.AllowsPress && useId < 0 && FrontRoomsTouchLayout.Contains(UseCenterNow, L.UseHitRadius, p))
                {
                    f.Role = Role.Use;
                    useId = id;
                    frameUseDown = true;
                    FrontRoomsMobileInteractionEvents.UsePressed();
                }
                else if (TranscriptShown && L.TranscriptPanel.Contains(p))
                {
                    f.Role = Role.PanelScroll;
                    f.ScrollStart = TranscriptScroll;
                }
                else if (OnTerminalPicture(p))
                {
                    // TT1′: a terminal's picture is the input surface; no look, no pinch, no hidden stick on it.
                    f.Role = Role.TerminalPicture;
                }
                else if (moveId < 0 && L.InMoveZone(p))
                {
                    f.Role = Role.Move;
                    moveId = id;
                    moveFinger = p;
                    StickOrigin = L.FloatingOrigin(p);
                    shotBackArmed = false;
                }
                else if (L.InLookZone(p))
                {
                    if (lookId < 0)
                    {
                        f.Role = Role.Look;
                        lookId = id;
                    }
                    else if (pinchId < 0 && fingers.TryGetValue(lookId, out var first))
                    {
                        // A second finger on the look side: a pinch. Look stops while it lasts.
                        f.Role = Role.Pinch;
                        pinchId = id;
                        pinchSpread = (p - first.Last).magnitude;
                        first.Moved = true;
                        fingers[lookId] = first;
                    }
                }
                break;
            case MenuState.Paused:
            case MenuState.Caught:
                f.Button = MenuButtonAt(p);
                if (f.Button != Button.None) f.Role = Role.Button;
                break;
            case MenuState.Settings:
                if (RestartConfirmationOpen)
                {
                    // END RUN's question covers the sheet: only its chips answer.
                    f.Button = MenuButtonAt(p);
                    if (f.Button != Button.None) f.Role = Role.Button;
                    break;
                }
                if (SettingsPeek)
                {
                    // The full-screen preview: a tap anywhere brings the sheet back.
                    f.Role = Role.Button;
                    f.Button = Button.SettingsPeekBack;
                    break;
                }
                f.Button = MenuButtonAt(p);
                if (f.Button != Button.None) f.Role = Role.Button;
                else if (MapUnfolded && PadMap != null && PadMapRect.Contains(p))
                {
                    // CONTROLLER's map: the lift chooses the callout under it, or listens for the chosen one (Activate).
                    f.Role = Role.Button;
                    f.Button = Button.PadMapTap;
                }
                else if (LanguageTagAt(p) != -1)
                {
                    f.Role = Role.Button;
                    f.Button = Button.LanguageRow;
                    f.Row = LanguageTagAt(p);
                    // The row has the focus while a finger is on it (the strip keeps the charm under it chosen); a cell swings once.
                    ActiveSettingsRow = LanguageRowId();
                    if (LanguageStrip.Grid && f.Row >= 0) LanguageStrip.HoverCell(LanguageLocal(p));
                }
                else if (SettingsTabAt(p, out var tabHit))
                {
                    f.Role = Role.Button;
                    f.Button = Button.SettingsTab;
                    f.Row = tabHit;
                }
                else if (SettingsTab == FrontRoomsSettingsMenu.Tab.Graphics && SettingsPreviewRect.width > 0f && SettingsPreviewRect.Contains(p))
                {
                    f.Role = Role.Button;
                    f.Button = Button.SettingsPreview;
                }
                else if (SettingsRowAt(p, out var row, out var dir))
                {
                    f.Role = Role.Button;
                    f.Button = Button.SettingsRow;
                    f.Row = row;
                    f.RowDirection = dir;
                    f.ScrollStart = SettingsScroll;
                    scrollVelocity = 0f;
                    following = false;
                }
                else if (SettingsScrollViewport.Contains(p))
                {
                    // Headers and blank parts of the viewport also allow a scroll gesture.
                    f.Role = Role.Scroll;
                    f.ScrollStart = SettingsScroll;
                    scrollVelocity = 0f;
                    following = false;
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
                if (pinchId >= 0) { Pinch(p, PinchOther(id)); break; }
                frameLook += LookDegrees(delta);
                if ((p - f.Start).sqrMagnitude > TapSlop * TapSlop) { f.Moved = true; fingers[id] = f; }
                break;
            case Role.Pinch:
                Pinch(p, PinchOther(id));
                break;
            case Role.Use:
                // A thumb that starts on USE may slide on to look (keeping the dot on a pane while it holds), after a little slop.
                if (!f.Moved && (p - f.Start).sqrMagnitude > UseLookSlop * UseLookSlop) { f.Moved = true; fingers[id] = f; }
                else if (f.Moved) frameLook += LookDegrees(delta);
                break;
            case Role.Button:
                if (f.Button == Button.SettingsRow && f.RowDirection >= RowSet && SlotRow(f.Row)?.Kind == FrontRoomsSettingsMenu.Kind.Slider
                    && Mathf.Abs(p.x - f.Start.x) > ScrollStartSlop && Mathf.Abs(p.x - f.Start.x) >= Mathf.Abs(p.y - f.Start.y))
                {
                    // A sideways drag that starts on a slider's track moves the slider; an upward one scrolls the list.
                    f.Role = Role.SliderDrag;
                    fingers[id] = f;
                    PressedButton = Button.None;
                    goto case Role.SliderDrag;
                }
                if ((f.Button == Button.SettingsRow || f.Button == Button.PadMapTap) && SettingsScrollViewport.Contains(f.Start) && Mathf.Abs(p.y - f.Start.y) > ScrollStartSlop)
                {
                    // A drag in the touch column scrolls it instead of tapping the row.
                    f.Role = Role.Scroll;
                    fingers[id] = f;
                    PressedButton = Button.None;
                    PressedSettingsRow = -1;
                    goto case Role.Scroll;
                }
                if (f.Button == Button.LanguageRow)
                {
                    // A drag up or down the sheet scrolls it, as on any row.
                    var dy = Mathf.Abs(p.y - f.Start.y);
                    if (SettingsScrollViewport.Contains(f.Start) && dy > ScrollStartSlop && dy > Mathf.Abs(p.x - f.Start.x))
                    {
                        f.Role = Role.Scroll;
                        fingers[id] = f;
                        PressedButton = Button.None;
                        PressedSettingsRow = -1;
                        goto case Role.Scroll;
                    }
                    // A slide along the strip takes the press to the charm under the finger, and the lift picks it; off the
                    // strip nothing is pressed. The finger sees what it will pick, so charms under 44 pt apart stay safe.
                    var at = LanguageTagAt(p);
                    // In the grid a press on the head stays on the head (a tap folds it), and one on the cells moves from cell to cell.
                    if (at == LanguageHead || f.Row == LanguageHead) at = at == LanguageHead && f.Row == LanguageHead ? LanguageHead : -1;
                    var off = at == -1;
                    if (off != f.Cancelled || (!off && at != f.Row))
                    {
                        f.Cancelled = off;
                        if (!off) f.Row = at;
                        if (!off && at >= 0 && LanguageStrip.Grid) LanguageStrip.HoverCell(LanguageLocal(p));
                        fingers[id] = f;
                        PressedButton = off ? Button.None : f.Button;
                        PressedSettingsRow = off ? -1 : f.Row;
                    }
                    break;
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
            case Role.PanelScroll:
                TranscriptScroll = Mathf.Clamp(f.ScrollStart + (p.y - f.Start.y), 0f, TranscriptScrollMax);
                break;
            case Role.Scroll:
                SettingsScroll = Mathf.Clamp(f.ScrollStart + (p.y - f.Start.y), 0f, SettingsScrollMax);
                scrollVelocity = FrontRoomsTouchMotion.Delta > 0f ? delta.y / FrontRoomsTouchMotion.Delta : 0f;
                break;
            case Role.SliderDrag:
            {
                var r = SlotRow(f.Row);
                if (r == null) break;
                foreach (var s in slots)
                {
                    if (s.Id != f.Row || s.Header) continue;
                    var rect = s.Rect;
                    rect.y += SettingsScroll;
                    var drawn = FrontRoomsHandheld.ClearRect(rect, true);
                    var stop = FrontRoomsSettingsMenu.StopAt(r, drawn.xMax - p.x, FrontRoomsSettingsMenu.Touch, 10000f);
                    if (stop >= 0 && stop != r.Index) ApplySettingsRow(f.Row, RowSet + stop);
                    break;
                }
                PressedSettingsRow = f.Row;
                break;
            }
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
            case Button.TextChip: return FrontRoomsTouchLayout.Contains(L.TextChipCenter, new Vector2(L.TextChipHitSize + ButtonCancelSlop, L.TextChipHitSize + ButtonCancelSlop), p);
            case Button.SprintToggle: return FrontRoomsTouchLayout.Contains(L.SprintButtonCenter, L.SprintButtonHitRadius + ButtonCancelSlop, p);
            case Button.SettingsRow:
            case Button.SettingsTab:
            case Button.SettingsPreview:
            case Button.SettingsPeekBack:
            case Button.PadMapTap:
                return (p - f.Start).sqrMagnitude <= TapSlop * TapSlop * 4f;
            case Button.LanguageRow:
                // Anywhere on the strip: a slide along it moves the press (Move), it is not a cancel; the grid's head stays the head.
                return f.Row == LanguageHead ? LanguageTagAt(p) == LanguageHead : LanguageTagAt(p) >= 0;
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
                    // A close-up's hidden stick is only a pull-back: a tap on it is a tap on the picture, often right under it.
                    if (!cancelled && menuState == MenuState.Inspect && FrontRoomsTouchMotion.Now - f.StartTime <= TapMaxSeconds && (p - f.Start).sqrMagnitude <= TapSlop * TapSlop)
                        frameZoomToggle = true;
                }
                break;
            case Role.Look:
                if (lookId == id)
                {
                    lookId = -1;
                    var quick = FrontRoomsTouchMotion.Now - f.StartTime <= TapMaxSeconds;
                    var tap = !cancelled && quick && !f.Moved && (p - f.Start).sqrMagnitude <= TapSlop * TapSlop;
                    if (pinchId >= 0)
                    {
                        // The pinch's other finger carries on looking.
                        if (fingers.TryGetValue(pinchId, out var other)) { other.Role = Role.Look; other.Moved = true; fingers[pinchId] = other; lookId = pinchId; }
                        pinchId = -1;
                    }
                    else if (tap && menuState == MenuState.Inspect) frameZoomToggle = true;   // the second tap: the full view, and back
                    else if (tap)
                    {
                        var frame = Layout.Frame;
                        LookTapped?.Invoke(new Vector2(p.x / Mathf.Max(1f, frame.x), p.y / Mathf.Max(1f, frame.y)));
                    }
                }
                break;
            case Role.Pinch:
                if (pinchId == id) pinchId = -1;
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
                // The lift picks the charm under the finger, wherever the slide took it.
                if (f.Button == Button.LanguageRow && f.Row != LanguageHead && LanguageTagAt(p) >= 0) f.Row = LanguageTagAt(p);
                if (!cancelled && !f.Cancelled && StillOnButton(f, p)) Activate(f);
                break;
            case Role.SliderDrag:
                PressedSettingsRow = -1;
                ActiveSettingsRow = f.Row;
                SetOpenRow(f.Row);
                FollowRow(f.Row);
                break;
            case Role.Scroll:
                break;
            case Role.TerminalPicture:
                if (!cancelled) ResolveTerminalTouch(f, p);
                break;
        }
    }

    Vector2 ViewportOf(Vector2 p) => new Vector2(p.x / Mathf.Max(1f, Layout.Frame.x), p.y / Mathf.Max(1f, Layout.Frame.y));

    /// <summary>TT1′: on a terminal's picture, or in the margin below it that row 25's soft keys reach into.</summary>
    bool OnTerminalPicture(Vector2 p)
    {
        var r = FrontRoomsInspectTerminal.PictureViewportRect;
        if (r.width <= 0f || r.height <= 0f) return false;
        var v = ViewportOf(p);
        return v.x >= r.xMin && v.x < r.xMax && v.y < r.yMax && v.y >= r.yMin - r.height * FrontRoomsInspectTerminal.SoftKeyMarginBelow;
    }

    /// <summary>TT1′ (41 §5.4): everything resolves on touch-up. A tap goes to the grid, a swipe pages on its main axis (up or left =
    /// next), and anything between does nothing.</summary>
    void ResolveTerminalTouch(Finger f, Vector2 p)
    {
        var held = FrontRoomsTouchMotion.Now - f.StartTime;
        var d = p - f.Start;
        if (held <= TapMaxSeconds && d.sqrMagnitude <= TapSlop * TapSlop)
        {
            FrontRoomsInspectTerminal.TapViewport(ViewportOf(p));
            return;
        }
        if (held > TerminalSwipeSeconds || d.magnitude < TerminalSwipeMin) return;
        var r = FrontRoomsInspectTerminal.PictureViewportRect;
        if (r.height <= 0f) return;
        var row = Mathf.FloorToInt((1f - (ViewportOf(f.Start).y - r.yMin) / r.height) * FrontRoomsTerminalGrid.Rows);
        if (row < TerminalSwipeFirstRow || row > TerminalSwipeLastRow) return;
        var next = Mathf.Abs(d.y) >= Mathf.Abs(d.x) ? d.y > 0f : d.x < 0f;
        FrontRoomsInspectTerminal.Swipe(next ? 1 : -1);
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
            case Button.TextChip:
                TranscriptToggled?.Invoke();
                break;
            case Button.SprintToggle:
                if (!Winded) SetRunning(!Running);
                break;
            case Button.SettingsRow:
            {
                // LANGUAGE's word unfolds its grid and folds it again; a tap between its cells does nothing.
                var langRow = SlotRow(f.Row);
                if (langRow != null && langRow.Kind == FrontRoomsSettingsMenu.Kind.Language)
                {
                    ActiveSettingsRow = f.Row;
                    if (f.RowDirection != RowLine) { SetLanguageUnfolded(!LanguageUnfolded); if (LanguageUnfolded) FollowRow(f.Row); }
                    break;
                }
                // CONTROLLER's word unfolds its map and folds it again; a tap beside the drawing does nothing.
                if (langRow != null && langRow.Key == FrontRoomsSettingsMenu.Key.PadMap)
                {
                    ActiveSettingsRow = f.Row;
                    if (f.RowDirection != RowLine) { SetMapUnfolded(!MapUnfolded); if (MapUnfolded) FollowRow(f.Row); }
                    break;
                }
            }
                // A tap opens its row in place (hover never does) and works the control under it; a tap on the open row's first line
                // off its control closes it (SETTINGS_MOTION.md, as a click on the desktop).
                openArmed = false;
                if (f.RowDirection == RowExplain && f.Row == OpenSettingsRow) { ActiveSettingsRow = f.Row; SetOpenRow(-1); }
                else
                {
                    SetOpenRow(f.Row);
                    ApplySettingsRow(f.Row, f.RowDirection == RowLine ? RowExplain : f.RowDirection);
                    FollowRow(f.Row);
                }
                break;
            case Button.PadMapTap:
            {
                // A callout is chosen by a tap; the chosen one tapped again listens for a button (as a click on the desktop's map).
                var mapAt = PadMapPoint(f.Start);
                foreach (var s in slots) if (!s.Header && s.Row != null && s.Row.Key == FrontRoomsSettingsMenu.Key.PadMap) ActiveSettingsRow = s.Id;
                if (PadMap == null) break;
                var at = PadMap.ActionAt(mapAt);
                if (at >= 0 && at == PadMap.Focus) PadMap.BeginCapture();
                else if (PadMap.FocusAt(mapAt)) followChosen = true;
                break;
            }
            case Button.LanguageRow:
            {
                // The head (the key, the current name) unfolds the grid and folds it again; on the rail another charm picks at once;
                // in the grid a cell picks and the grid stays.
                ActiveSettingsRow = LanguageRowId();
                var listed = FrontRoomsLanguage.ListedLanguages;
                var current = f.Row >= 0 && f.Row < listed.Count && listed[f.Row] == FrontRoomsLanguage.Current;
                if (f.Row == LanguageHead || (!LanguageUnfolded && current))
                {
                    SetLanguageUnfolded(!LanguageUnfolded);
                    if (LanguageUnfolded) FollowRow(ActiveSettingsRow);
                }
                else LanguageStrip.Pick(f.Row);
                break;
            }
            case Button.SettingsTab:
                if (f.Row >= 0 && f.Row < FrontRoomsSettingsMenu.Tabs.Length && SettingsTab != FrontRoomsSettingsMenu.Tabs[f.Row])
                {
                    SettingsTab = FrontRoomsSettingsMenu.Tabs[f.Row];
                    FrontRoomsSettingsMenu.LatchControlsOrder();
                    SettingsScroll = 0f;
                    scrollVelocity = 0f;
                    ActiveSettingsRow = -1;
                    CloseAllRows();
                }
                break;
            case Button.SettingsPreview:
                SettingsPeek = true;
                break;
            case Button.SettingsPeekBack:
                SettingsPeek = false;
                break;
        }
        if (f.Button != Button.Pause && f.Button != Button.SprintToggle) FrontRoomsMobileInteractionEvents.MenuConfirmed();
    }

    /// <summary>The other finger of the pinch, where it is now.</summary>
    Vector2 PinchOther(int id)
    {
        var other = id == pinchId ? lookId : pinchId;
        return fingers.TryGetValue(other, out var f) ? f.Last : Vector2.zero;
    }

    /// <summary>A pinch's spread change, as a fraction of a zoom step (+ is closer).</summary>
    void Pinch(Vector2 p, Vector2 other)
    {
        var spread = (p - other).magnitude;
        frameZoom += (spread - pinchSpread) / (PinchPointsPerStep * Mathf.Max(.01f, Layout.Scale));
        pinchSpread = spread;
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

    /// <summary>A43: as <see cref="ClearTouchState"/>, but the left finger stays on the stick (armed, so its pull-back presses once).</summary>
    void ClearTouchStateButStick()
    {
#if UNITY_IOS || UNITY_ANDROID || UNITY_EDITOR
        if (moveId >= 0 && fingers.TryGetValue(moveId, out var stick))
        {
            var id = moveId;
            var armed = shotBackArmed;
            ClearTouchState();
            fingers[id] = stick;
            moveId = id;
            shotBackArmed = armed;
            return;
        }
#endif
        ClearTouchState();
    }

    void ClearTouchState()
    {
        if (useId >= 0) ReleaseUse();
#if UNITY_IOS || UNITY_ANDROID || UNITY_EDITOR
        fingers.Clear();
#endif
        moveId = lookId = pinchId = -1;
        shotBackArmed = false;
        SetRunning(false);
        CurrentGait = FrontRoomsGait.Still;
        RunReach = 0f;
        PressedButton = Button.None;
        PressedSettingsRow = -1;
        frameMove = frameLook = Vector2.zero;
        frameZoom = 0f;
        frameZoomToggle = false;
        frameUseDown = framePauseDown = frameStartDown = frameSettingsDown = frameRestartDown = frameShotBack = false;
    }
}
