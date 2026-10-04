using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_IOS || UNITY_ANDROID
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using EnhancedTouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using InputTouchPhase = UnityEngine.InputSystem.TouchPhase;
#endif

/// <summary>
/// The touch-only input surface for FrontRooms.
///
/// This component deliberately does not know about FrontRooms3DGame, physics,
/// raycasts, or the existing desktop input path. The game can bind the
/// <see cref="Move"/>, <see cref="LookDelta"/>, and button properties to its
/// input facade, while the HUD can bind a contextual prompt with
/// <see cref="SetUsePrompt"/>.
///
/// EnhancedTouch is enabled only in iOS/Android players. On desktop and in
/// WebGL this class remains a safe no-op, so a scene may contain the component
/// without introducing a platform-specific compile dependency.
/// </summary>
[DefaultExecutionOrder(-500)]
public sealed class FrontRoomsTouchControls : MonoBehaviour
{
    public enum MenuState
    {
        Title,
        Playing,
        Paused,
        Settings,
        Caught
    }

    public enum UseKind
    {
        Hidden,
        Open,
        Shut,
        Take,
        Locked,
        Hold,
        Tap
    }

    /// <summary>What the contextual USE control currently represents.</summary>
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

        public bool AllowsPress => Visible && Interactable && !Locked && Kind != UseKind.Locked;

        public static UsePrompt Hidden => new UsePrompt
        {
            Visible = false,
            Interactable = false,
            Hold = false,
            Locked = false,
            TapMode = false,
            Label = string.Empty,
            Progress = 0f,
            Kind = UseKind.Hidden
        };

        public bool Equals(UsePrompt other)
        {
            return Visible == other.Visible
                && Interactable == other.Interactable
                && Hold == other.Hold
                && Locked == other.Locked
                && TapMode == other.TapMode
                && string.Equals(Label, other.Label, StringComparison.Ordinal)
                && Mathf.Approximately(Progress, other.Progress)
                && Kind == other.Kind;
        }

        public override bool Equals(object obj) => obj is UsePrompt && Equals((UsePrompt)obj);
        public override int GetHashCode() => (Visible ? 1 : 0) ^ (Interactable ? 2 : 0) ^ (Hold ? 4 : 0) ^ (Locked ? 8 : 0) ^ (TapMode ? 16 : 0) ^ (Label ?? string.Empty).GetHashCode() ^ Kind.GetHashCode();
        public static bool operator ==(UsePrompt left, UsePrompt right) => left.Equals(right);
        public static bool operator !=(UsePrompt left, UsePrompt right) => !left.Equals(right);
    }

    /// <summary>A per-frame copy that can be read by an input facade.</summary>
    [Serializable]
    public sealed class FrameState
    {
        public Vector2 Move;
        public Vector2 LookDelta;
        public bool SprintHeld;
        public bool UseHeld;
        public bool UsePressed;
        public bool UseReleased;
        public bool PausePressed;
        public bool StartPressed;
        public bool RestartPressed;
        public bool SettingsPressed;
        public bool RestartCancelPressed;
        public bool ShotBack;
        public int SettingsRow;
    }

    [Serializable]
    public sealed class SettingsRowBinding
    {
        public RectTransform HitArea;
        public int Index;
    }

    [Header("Screen and hit areas")]
    [SerializeField] Canvas uiCanvas;
    [SerializeField] RectTransform useHitArea;
    [SerializeField] RectTransform pauseHitArea;
    [SerializeField] RectTransform startHitArea;
    [SerializeField] RectTransform restartHitArea;
    [SerializeField] RectTransform caughtRestartHitArea;
    [SerializeField] RectTransform restartCancelHitArea;
    [SerializeField] RectTransform sprintButtonHitArea;
    [SerializeField] RectTransform settingsHitArea;
    [SerializeField] List<SettingsRowBinding> settingsRows = new List<SettingsRowBinding>();
    [SerializeField, Min(0.5f)] float controlsScale = 1f;
    [SerializeField] bool leftHanded;
    [SerializeField] MenuState menuState = MenuState.Title;

    [Header("Floating stick")]
    [SerializeField] bool floatingStick = true;
    [SerializeField] bool sprintSocketMode = true;
    [SerializeField, Min(1f)] float stickRadius = 60f;
    [SerializeField, Min(0f)] float stickDeadZone = 8f;
    [SerializeField, Min(1f)] float sprintSocketOffset = 92f;
    [SerializeField, Min(1f)] float sprintSocketRadius = 20f;
    [SerializeField, Min(0f)] float sprintLatchSeconds = 0.15f;
    [SerializeField, Min(0f)] float movementBandTop = 0.72f;

    [Header("Look and taps")]
    // EnhancedTouch reports screen points. A 200 pt thumb sweep should turn
    // roughly 24 degrees by default; the settings layer can expose this as
    // the 1–10 LOOK SPEED row later.
    [SerializeField, Min(0.01f)] float lookSensitivity = 0.12f;
    [SerializeField] bool invertLook;
    [SerializeField, Min(0f)] float tapMoveThreshold = 12f;
    [SerializeField, Min(0f)] float tapMaxSeconds = 0.2f;
    [SerializeField] bool winded;

    [Header("Inspector callbacks")]
    public UnityEvent onPausePressed = new UnityEvent();
    public UnityEvent onStartPressed = new UnityEvent();
    public UnityEvent onRestartPressed = new UnityEvent();
    public UnityEvent onSettingsPressed = new UnityEvent();
    public UnityEvent<int> onSettingsRowPressed = new UnityEvent<int>();
    public UnityEvent onRestartCancelPressed = new UnityEvent();
    public UnityEvent onUsePressed = new UnityEvent();
    public UnityEvent onUseReleased = new UnityEvent();

    /// <summary>Raised when the contextual prompt changes.</summary>
    public event Action<UsePrompt> PromptChanged;
    /// <summary>Raised when a touch requests the pause/resume action.</summary>
    public event Action PauseRequested;
    /// <summary>Raised by the title touch surface.</summary>
    public event Action StartRequested;
    /// <summary>Raised by a caught/pause restart chip.</summary>
    public event Action RestartRequested;
    /// <summary>Raised by the pause/settings chip.</summary>
    public event Action SettingsRequested;
    /// <summary>Raised by a settings row. The index is the binding's Index.</summary>
    public event Action<int> SettingsRowRequested;
    /// <summary>Raised when a pending pause restart is canceled.</summary>
    public event Action RestartCancelRequested;
    /// <summary>Raised on a fresh USE press (E down equivalent).</summary>
    public event Action UsePressed;
    /// <summary>Raised when the USE finger is lifted/canceled.</summary>
    public event Action UseReleased;
    /// <summary>Raised on a short, stationary touch in the look zone.</summary>
    public event Action<Vector2> LookTapped;
    /// <summary>Raised when Screen.safeArea or the display size changes.</summary>
    public event Action<Rect> SafeAreaChanged;
    /// <summary>Raised once when the sprint socket latch engages or disengages.</summary>
    public event Action<bool> SprintLatchChanged;

    public MenuState CurrentMenuState => menuState;
    public bool FloatingStick => floatingStick;
    public bool LeftHanded => leftHanded;
    public bool SprintSocketMode => sprintSocketMode;
    public Rect SafeAreaPixels { get; private set; }
    public UsePrompt CurrentUsePrompt { get; private set; } = UsePrompt.Hidden;
    public FrameState CurrentFrame { get; private set; } = new FrameState { SettingsRow = -1 };

    /// <summary>Normalized move intent in stick space (x right, y up).</summary>
    public Vector2 Move => CurrentFrame.Move;
    /// <summary>Screen-space look delta for this frame, before game yaw/pitch scaling.</summary>
    public Vector2 LookDelta => CurrentFrame.LookDelta;
    public bool SprintHeld => CurrentFrame.SprintHeld;
    public bool UseHeld => CurrentFrame.UseHeld;
    public bool UsePressedThisFrame => CurrentFrame.UsePressed;
    public bool UseReleasedThisFrame => CurrentFrame.UseReleased;
    public bool PausePressedThisFrame => CurrentFrame.PausePressed;
    public bool StartPressedThisFrame => CurrentFrame.StartPressed;
    public bool RestartPressedThisFrame => CurrentFrame.RestartPressed;
    public bool SettingsPressedThisFrame => CurrentFrame.SettingsPressed;
    public bool RestartCancelPressedThisFrame => CurrentFrame.RestartCancelPressed;
    /// <summary>Edge pulse produced when the move thumb is pulled back (S equivalent).</summary>
    public bool ShotBackPressedThisFrame => CurrentFrame.ShotBack;
    public int SettingsRowPressedThisFrame => CurrentFrame.SettingsRow;
    public bool SprintSocketLatched => sprintLatched;
    public bool RestartConfirmationOpen { get; private set; }
    public float SprintSocketProgress => sprintSocketProgress;
    public Vector2 StickOriginScreenPosition => stickOrigin;
    public Vector2 StickThumbScreenPosition => stickThumb;
    public RectTransform UseHitArea => useHitArea;
    public RectTransform PauseHitArea => pauseHitArea;
    public RectTransform SprintButtonHitArea => sprintButtonHitArea;

    /// <summary>Optional provider for a HUD/game loop that wants to refresh the prompt itself.</summary>
    public Func<UsePrompt> UsePromptProvider { get; set; }

#if UNITY_IOS || UNITY_ANDROID
    enum TouchRole
    {
        None,
        Move,
        Look,
        Use,
        Pause,
        Start,
        Restart,
        Settings,
        SettingsRow,
        RestartCancel,
        SprintButton
    }

    struct TouchState
    {
        public TouchRole Role;
        public Vector2 StartPosition;
        public Vector2 LastPosition;
        public float StartTime;
        public int SettingsRow;
    }

    readonly Dictionary<int, TouchState> touches = new Dictionary<int, TouchState>();
    readonly List<int> staleTouchIds = new List<int>();
    readonly HashSet<int> seenTouchIds = new HashSet<int>();
    bool enhancedTouchOwner;
#endif

    Rect lastSafeArea;
    int lastScreenWidth;
    int lastScreenHeight;
    bool sprintLatched;
    float sprintSocketProgress;
    bool moveTouchActive;
    int moveTouchId = -1;
    int lookTouchId = -1;
    int useTouchId = -1;
    int sprintButtonTouchId = -1;
    Vector2 stickOrigin;
    Vector2 stickThumb;
    bool shotBackArmed;
    bool usePromptWasVisible;

    void Awake()
    {
        ApplySavedSettings();
        UpdateSafeArea(true);
    }

    void OnEnable()
    {
#if UNITY_IOS || UNITY_ANDROID
        if (!EnhancedTouchSupport.enabled)
        {
            EnhancedTouchSupport.Enable();
            enhancedTouchOwner = true;
        }
#endif
        UpdateSafeArea(true);
    }

    void OnDisable()
    {
#if UNITY_IOS || UNITY_ANDROID
        ClearTouchState();
        if (enhancedTouchOwner)
        {
            EnhancedTouchSupport.Disable();
            enhancedTouchOwner = false;
        }
        FrontRoomsInput.ClearVirtualSnapshot();
#endif
    }

    void OnApplicationFocus(bool focused)
    {
        if (focused)
            return;
#if UNITY_IOS || UNITY_ANDROID
        ClearTouchState();
        FrontRoomsInput.ClearVirtualSnapshot();
#endif
    }

    void OnApplicationPause(bool paused)
    {
        if (!paused)
            return;
#if UNITY_IOS || UNITY_ANDROID
        ClearTouchState();
        FrontRoomsInput.ClearVirtualSnapshot();
#endif
    }

    void Update()
    {
        UpdateSafeArea(false);

        if (UsePromptProvider != null)
            SetUsePrompt(UsePromptProvider());

        ResetFramePulses();

#if UNITY_IOS || UNITY_ANDROID
        ProcessTouches();
        // Publish one complete virtual frame before the gameplay MonoBehaviour
        // runs (this component has a negative execution order). The facade is
        // kept out of the desktop/WebGL path so keyboard and mouse remain the
        // source of truth there.
        FrontRoomsInput.SetVirtualSnapshot(new FrontRoomsInput.FrameSnapshot(
            CurrentFrame.Move,
            CurrentFrame.LookDelta,
            CurrentFrame.SprintHeld,
            CurrentFrame.UsePressed,
            CurrentFrame.UseHeld,
            CurrentFrame.PausePressed,
            CurrentFrame.StartPressed,
            CurrentFrame.SettingsPressed,
            CurrentFrame.RestartPressed,
            CurrentFrame.ShotBack));
#endif
    }

    void ResetFramePulses()
    {
        CurrentFrame = new FrameState
        {
            Move = Vector2.zero,
            LookDelta = Vector2.zero,
            SprintHeld = sprintLatched && !winded,
            UseHeld = useTouchId >= 0,
            UsePressed = false,
            UseReleased = false,
            PausePressed = false,
            StartPressed = false,
            RestartPressed = false,
            SettingsPressed = false,
            RestartCancelPressed = false,
            ShotBack = false,
            SettingsRow = -1
        };
    }

    void UpdateSafeArea(bool force)
    {
        var area = Screen.safeArea;
        if (area.width <= 0f || area.height <= 0f)
            area = new Rect(0f, 0f, Screen.width, Screen.height);

        if (!force && area == lastSafeArea && Screen.width == lastScreenWidth && Screen.height == lastScreenHeight)
            return;

        lastSafeArea = area;
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
        SafeAreaPixels = area;
        SafeAreaChanged?.Invoke(area);
    }

    /// <summary>Switches the menu ownership and clears any finger that belonged to the old state.</summary>
    public void SetMenuState(MenuState state)
    {
        if (menuState == state)
            return;
        menuState = state;
#if UNITY_IOS || UNITY_ANDROID
        ClearTouchState();
#endif
    }

    public void SetLeftHanded(bool value)
    {
        if (leftHanded == value)
            return;
        leftHanded = value;
#if UNITY_IOS || UNITY_ANDROID
        ClearTouchState();
#endif
    }

    public void SetControlsScale(float value) => controlsScale = Mathf.Clamp(value, 0.8f, 1.4f);
    public void SetLookSensitivity(float value) => lookSensitivity = Mathf.Clamp(value, 0.01f, 0.5f);
    public void SetInvertLook(bool value) => invertLook = value;
    public void SetFloatingStick(bool value)
    {
        if (floatingStick == value) return;
        floatingStick = value;
#if UNITY_IOS || UNITY_ANDROID
        ClearTouchState();
#endif
    }
    public void SetSprintSocketMode(bool value)
    {
        if (sprintSocketMode == value) return;
        sprintSocketMode = value;
        SetSprintLatched(false);
        sprintSocketProgress = 0f;
#if UNITY_IOS || UNITY_ANDROID
        ClearTouchState();
#endif
    }
    public void SetSprintButtonHitArea(RectTransform area) => sprintButtonHitArea = area;

    /// <summary>Applies the persisted mobile controls preferences after settings load.</summary>
    public void ApplySavedSettings()
    {
        SetControlsScale(FrontRoomsSettings.TouchControlsScalePercent / 100f);
        SetLeftHanded(FrontRoomsSettings.TouchLeftHanded);
        SetLookSensitivity(.12f * FrontRoomsSettings.TouchLookSpeedPercent / 100f);
        SetInvertLook(FrontRoomsSettings.TouchInvertLook);
        floatingStick = FrontRoomsSettings.TouchFloatingStick;
        sprintSocketMode = FrontRoomsSettings.TouchSprintSocket;
        FrontRoomsMobileHaptics.Enabled = FrontRoomsSettings.TouchHaptics;
    }
    public void SetWinded(bool value)
    {
        if (winded == value)
            return;
        winded = value;
        if (winded)
            SetSprintLatched(false);
    }

    /// <summary>
    /// Binds the current aim result. Hidden or locked prompts never claim a
    /// touch, so the right half remains available for look.
    /// </summary>
    public void SetUsePrompt(UsePrompt prompt)
    {
        prompt.Progress = Mathf.Clamp01(prompt.Progress);
        if (prompt == CurrentUsePrompt)
            return;

        CurrentUsePrompt = prompt;
        // A target can disappear or become locked while the finger is still
        // down (for example when a door closes or a glass shot completes).
        // Release the virtual USE edge immediately so a stale hold cannot
        // leak into the next aim target.
        if (usePromptWasVisible && (!prompt.Visible || !prompt.AllowsPress))
        {
#if UNITY_IOS || UNITY_ANDROID
            if (useTouchId >= 0)
                ReleaseUse(useTouchId, true);
#endif
        }
        usePromptWasVisible = prompt.Visible;
        PromptChanged?.Invoke(prompt);
    }

    public void ClearUsePrompt() => SetUsePrompt(UsePrompt.Hidden);
    public void SetUseHitArea(RectTransform area) => useHitArea = area;
    public void SetPauseHitArea(RectTransform area) => pauseHitArea = area;
    public void SetStartHitArea(RectTransform area) => startHitArea = area;
    public void SetRestartHitArea(RectTransform area) => restartHitArea = area;
    public void SetCaughtRestartHitArea(RectTransform area) => caughtRestartHitArea = area;
    public void SetRestartCancelHitArea(RectTransform area) => restartCancelHitArea = area;
    public void SetSettingsHitArea(RectTransform area) => settingsHitArea = area;

    public void SetRestartConfirmation(bool open)
    {
        RestartConfirmationOpen = open;
        if (open)
            SetMenuState(MenuState.Paused);
    }

    public void BindSettingsRow(int index, RectTransform hitArea)
    {
        for (var i = 0; i < settingsRows.Count; i++)
        {
            if (settingsRows[i] != null && settingsRows[i].Index == index)
            {
                settingsRows[i].HitArea = hitArea;
                return;
            }
        }
        settingsRows.Add(new SettingsRowBinding { Index = index, HitArea = hitArea });
    }

    void InvokePause()
    {
        CurrentFrame.PausePressed = true;
        PauseRequested?.Invoke();
        onPausePressed?.Invoke();
    }

    void InvokeStart()
    {
        CurrentFrame.StartPressed = true;
        StartRequested?.Invoke();
        onStartPressed?.Invoke();
    }

    void InvokeRestart()
    {
        CurrentFrame.RestartPressed = true;
        RestartRequested?.Invoke();
        onRestartPressed?.Invoke();
    }

    void InvokeSettings()
    {
        CurrentFrame.SettingsPressed = true;
        SettingsRequested?.Invoke();
        onSettingsPressed?.Invoke();
    }

    void InvokeSettingsRow(int index)
    {
        // A row tap has its own indexed callback. Do not also publish the
        // generic Settings edge: FrontRooms3DGame uses that edge to toggle
        // the settings panel, while a row tap must keep the panel open.
        CurrentFrame.SettingsRow = index;
        SettingsRowRequested?.Invoke(index);
        onSettingsRowPressed?.Invoke(index);
    }

    void InvokeRestartCancel()
    {
        CurrentFrame.RestartCancelPressed = true;
        RestartCancelRequested?.Invoke();
        onRestartCancelPressed?.Invoke();
    }

    void InvokeUsePressed()
    {
        CurrentFrame.UsePressed = true;
        CurrentFrame.UseHeld = true;
        FrontRoomsMobileInteractionEvents.UsePressed();
        UsePressed?.Invoke();
        onUsePressed?.Invoke();
    }

    void InvokeUseReleased()
    {
        CurrentFrame.UseReleased = true;
        CurrentFrame.UseHeld = false;
        FrontRoomsMobileInteractionEvents.UseReleased();
        UseReleased?.Invoke();
        onUseReleased?.Invoke();
    }

    void SetSprintLatched(bool value)
    {
        if (sprintLatched == value)
            return;
        sprintLatched = value;
        sprintSocketProgress = value ? 1f : 0f;
        CurrentFrame.SprintHeld = sprintLatched && !winded;
        FrontRoomsMobileInteractionEvents.SprintLatched(value);
        SprintLatchChanged?.Invoke(value);
    }

    bool Contains(RectTransform hitArea, Vector2 screenPosition)
    {
        if (hitArea == null)
            return false;
        var camera = uiCanvas != null && uiCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? uiCanvas.worldCamera : null;
        return RectTransformUtility.RectangleContainsScreenPoint(hitArea, screenPosition, camera);
    }

    Rect DefaultPauseRect()
    {
        var size = 44f * controlsScale;
        var margin = 8f * controlsScale;
        return new Rect(SafeAreaPixels.xMax - margin - size, SafeAreaPixels.yMax - margin - size, size, size);
    }

    Rect DefaultUseRect()
    {
        var size = 88f * controlsScale;
        var x = leftHanded ? SafeAreaPixels.xMin + 134f * controlsScale : SafeAreaPixels.xMax - 134f * controlsScale;
        var y = SafeAreaPixels.yMin + SafeAreaPixels.height * 0.35f;
        return new Rect(x - size * .5f, y - size * .5f, size, size);
    }

    Rect DefaultStartRect() => SafeAreaPixels;

    bool IsPauseHit(Vector2 position)
    {
        return pauseHitArea != null ? Contains(pauseHitArea, position) : DefaultPauseRect().Contains(position);
    }

    bool IsUseHit(Vector2 position)
    {
        return useHitArea != null ? Contains(useHitArea, position) : DefaultUseRect().Contains(position);
    }

    bool IsSprintButtonHit(Vector2 position)
    {
        if (sprintButtonHitArea != null)
            return Contains(sprintButtonHitArea, position);
        var size = 72f * controlsScale;
        var x = leftHanded ? SafeAreaPixels.xMax - 134f * controlsScale : SafeAreaPixels.xMin + 134f * controlsScale;
        var y = SafeAreaPixels.yMin + SafeAreaPixels.height * .35f + sprintSocketOffset * controlsScale;
        return new Rect(x - size * .5f, y - size * .5f, size, size).Contains(position);
    }

    bool IsStartHit(Vector2 position)
    {
        return startHitArea != null ? Contains(startHitArea, position) : DefaultStartRect().Contains(position);
    }

    bool IsRestartHit(Vector2 position)
    {
        var area = menuState == MenuState.Caught && caughtRestartHitArea != null
            ? caughtRestartHitArea
            : restartHitArea;
        return Contains(area, position);
    }
    bool IsRestartCancelHit(Vector2 position) => Contains(restartCancelHitArea, position);
    bool IsSettingsHit(Vector2 position) => Contains(settingsHitArea, position);

    bool IsSettingsRowHit(Vector2 position, out int index)
    {
        for (var i = 0; i < settingsRows.Count; i++)
        {
            var row = settingsRows[i];
            if (row != null && row.HitArea != null && Contains(row.HitArea, position))
            {
                index = row.Index;
                return true;
            }
        }
        index = -1;
        return false;
    }

    bool IsMoveSide(Vector2 position)
    {
        var midpoint = SafeAreaPixels.center.x;
        return leftHanded ? position.x >= midpoint : position.x <= midpoint;
    }

    bool IsLookSide(Vector2 position)
    {
        var midpoint = SafeAreaPixels.center.x;
        return leftHanded ? position.x < midpoint : position.x > midpoint;
    }

    bool IsMoveStart(Vector2 position)
    {
        if (!SafeAreaPixels.Contains(position) || !IsMoveSide(position))
            return false;
        var top = SafeAreaPixels.yMin + SafeAreaPixels.height * Mathf.Clamp01(movementBandTop);
        return position.y <= top;
    }

    bool IsLookStart(Vector2 position)
    {
        if (!SafeAreaPixels.Contains(position) || !IsLookSide(position))
            return false;
        // Keep the top HUD read-only; pause is claimed before this check.
        var topBand = 64f * controlsScale;
        return position.y <= SafeAreaPixels.yMax - topBand;
    }

#if UNITY_IOS || UNITY_ANDROID
    void ProcessTouches()
    {
        seenTouchIds.Clear();
        var active = EnhancedTouch.activeTouches;
        for (var i = 0; i < active.Count; i++)
        {
            var touch = active[i];
            if (!touch.valid)
                continue;
            var id = touch.touchId;
            seenTouchIds.Add(id);

            if (touch.phase == InputTouchPhase.Began)
            {
                BeginTouch(id, touch.screenPosition);
                continue;
            }

            TouchState state;
            if (!touches.TryGetValue(id, out state))
                continue;

            var position = touch.screenPosition;
            var delta = position - state.LastPosition;
            state.LastPosition = position;
            touches[id] = state;
            UpdateTouch(id, state, position, delta);

            if (touch.phase == InputTouchPhase.Ended || touch.phase == InputTouchPhase.Canceled)
                EndTouch(id, state, position, touch.phase == InputTouchPhase.Canceled);
        }

        // A focus transition can drop an Ended/Canceled record. Do not leave a
        // move/use latch behind if the platform omitted the final touch event.
        staleTouchIds.Clear();
        foreach (var pair in touches)
        {
            if (!seenTouchIds.Contains(pair.Key))
                staleTouchIds.Add(pair.Key);
        }
        for (var i = 0; i < staleTouchIds.Count; i++)
        {
            TouchState state;
            if (touches.TryGetValue(staleTouchIds[i], out state))
                EndTouch(staleTouchIds[i], state, state.LastPosition, true);
        }

        if (moveTouchActive)
            CurrentFrame.Move = CalculateMove();
        CurrentFrame.SprintHeld = sprintLatched && !winded;
        CurrentFrame.UseHeld = useTouchId >= 0;
    }

    void BeginTouch(int id, Vector2 position)
    {
        var role = ClassifyTouch(position, out var rowIndex);
        if (role == TouchRole.None)
            return;

        var state = new TouchState
        {
            Role = role,
            StartPosition = position,
            LastPosition = position,
            StartTime = Time.unscaledTime,
            SettingsRow = rowIndex
        };
        touches[id] = state;

        switch (role)
        {
            case TouchRole.Move:
                if (moveTouchActive)
                {
                    touches.Remove(id);
                    return;
                }
                moveTouchActive = true;
                moveTouchId = id;
                stickOrigin = floatingStick ? position : GetFixedStickOrigin();
                stickThumb = position;
                sprintSocketProgress = 0f;
                break;
            case TouchRole.Look:
                if (lookTouchId >= 0)
                {
                    touches.Remove(id);
                    return;
                }
                lookTouchId = id;
                break;
            case TouchRole.Use:
                if (useTouchId >= 0 || !CurrentUsePrompt.AllowsPress)
                {
                    touches.Remove(id);
                    return;
                }
                useTouchId = id;
                InvokeUsePressed();
                break;
            case TouchRole.SprintButton:
                if (sprintButtonTouchId >= 0 || winded)
                {
                    touches.Remove(id);
                    return;
                }
                sprintButtonTouchId = id;
                SetSprintLatched(true);
                break;
            case TouchRole.Pause:
                InvokePause();
                touches.Remove(id);
                break;
            case TouchRole.Start:
                InvokeStart();
                touches.Remove(id);
                break;
            case TouchRole.Restart:
                InvokeRestart();
                touches.Remove(id);
                break;
            case TouchRole.Settings:
                InvokeSettings();
                touches.Remove(id);
                break;
            case TouchRole.SettingsRow:
                InvokeSettingsRow(rowIndex);
                touches.Remove(id);
                break;
            case TouchRole.RestartCancel:
                InvokeRestartCancel();
                touches.Remove(id);
                break;
        }
    }

    TouchRole ClassifyTouch(Vector2 position, out int rowIndex)
    {
        rowIndex = -1;
        switch (menuState)
        {
            case MenuState.Title:
                return IsStartHit(position) ? TouchRole.Start : TouchRole.None;
            case MenuState.Paused:
                if (RestartConfirmationOpen)
                {
                    if (IsRestartHit(position)) return TouchRole.Restart;
                    if (IsRestartCancelHit(position)) return TouchRole.RestartCancel;
                    return TouchRole.None;
                }
                if (IsSettingsHit(position)) return TouchRole.Settings;
                if (IsRestartHit(position)) return TouchRole.Restart;
                if (IsPauseHit(position)) return TouchRole.Pause;
                if (IsSettingsRowHit(position, out rowIndex)) return TouchRole.SettingsRow;
                return TouchRole.None;
            case MenuState.Settings:
                if (IsSettingsRowHit(position, out rowIndex)) return TouchRole.SettingsRow;
                if (IsSettingsHit(position) || IsPauseHit(position)) return TouchRole.Settings;
                return TouchRole.None;
            case MenuState.Caught:
                return IsRestartHit(position) ? TouchRole.Restart : TouchRole.None;
            case MenuState.Playing:
                if (IsPauseHit(position)) return TouchRole.Pause;
                if (CurrentUsePrompt.Visible && IsUseHit(position)) return TouchRole.Use;
                if (!sprintSocketMode && IsSprintButtonHit(position)) return TouchRole.SprintButton;
                if (IsMoveStart(position) && moveTouchId < 0) return TouchRole.Move;
                if (IsLookStart(position) && lookTouchId < 0) return TouchRole.Look;
                return TouchRole.None;
            default:
                return TouchRole.None;
        }
    }

    void UpdateTouch(int id, TouchState state, Vector2 position, Vector2 delta)
    {
        switch (state.Role)
        {
            case TouchRole.Move:
                stickThumb = position;
                var moveOffset = position - stickOrigin;
                if (sprintSocketMode)
                    UpdateSprintLatch(moveOffset);
                var pullingBack = moveOffset.y < -stickDeadZone;
                if (pullingBack && !shotBackArmed)
                {
                    CurrentFrame.ShotBack = true;
                    shotBackArmed = true;
                }
                else if (!pullingBack)
                {
                    shotBackArmed = false;
                }
                break;
            case TouchRole.Look:
            case TouchRole.Use:
                CurrentFrame.LookDelta += new Vector2(delta.x, invertLook ? -delta.y : delta.y) * lookSensitivity;
                break;
            case TouchRole.SprintButton:
                break;
        }
    }

    void EndTouch(int id, TouchState state, Vector2 position, bool canceled)
    {
        touches.Remove(id);
        switch (state.Role)
        {
            case TouchRole.Move:
                if (moveTouchId == id)
                {
                    moveTouchId = -1;
                    moveTouchActive = false;
                    stickThumb = stickOrigin;
                    shotBackArmed = false;
                    SetSprintLatched(false);
                    sprintSocketProgress = 0f;
                }
                break;
            case TouchRole.Look:
                if (lookTouchId == id)
                {
                    lookTouchId = -1;
                    if (!canceled && Time.unscaledTime - state.StartTime <= tapMaxSeconds && (position - state.StartPosition).sqrMagnitude <= tapMoveThreshold * tapMoveThreshold)
                        LookTapped?.Invoke(position);
                }
                break;
            case TouchRole.Use:
                if (useTouchId == id)
                {
                    useTouchId = -1;
                    InvokeUseReleased();
                }
                break;
            case TouchRole.SprintButton:
                if (sprintButtonTouchId == id)
                {
                    sprintButtonTouchId = -1;
                    SetSprintLatched(false);
                }
                break;
        }
    }

    void ReleaseUse(int id, bool canceled)
    {
        TouchState state;
        if (!touches.TryGetValue(id, out state) || state.Role != TouchRole.Use)
            return;
        EndTouch(id, state, state.LastPosition, canceled);
    }

    void ClearTouchState()
    {
        if (useTouchId >= 0)
            InvokeUseReleased();
        touches.Clear();
        seenTouchIds.Clear();
        staleTouchIds.Clear();
        moveTouchId = -1;
        lookTouchId = -1;
        useTouchId = -1;
        sprintButtonTouchId = -1;
        moveTouchActive = false;
        stickThumb = stickOrigin;
        shotBackArmed = false;
        SetSprintLatched(false);
        sprintSocketProgress = 0f;
        CurrentFrame.Move = Vector2.zero;
        CurrentFrame.LookDelta = Vector2.zero;
        CurrentFrame.SprintHeld = false;
        CurrentFrame.UseHeld = false;
        CurrentFrame.ShotBack = false;
    }

    Vector2 CalculateMove()
    {
        var raw = stickThumb - stickOrigin;
        var magnitude = raw.magnitude;
        if (magnitude <= stickDeadZone)
            return Vector2.zero;
        var normalized = Mathf.Clamp01((magnitude - stickDeadZone) / Mathf.Max(1f, stickRadius - stickDeadZone));
        return raw / magnitude * normalized;
    }

    void UpdateSprintLatch(Vector2 offset)
    {
        if (winded)
        {
            sprintSocketProgress = 0f;
            SetSprintLatched(false);
            return;
        }

        var socket = Vector2.up * sprintSocketOffset;
        if (!sprintLatched)
        {
            if (Vector2.Distance(offset, socket) <= sprintSocketRadius)
                sprintSocketProgress = Mathf.MoveTowards(sprintSocketProgress, 1f, Time.unscaledDeltaTime / Mathf.Max(0.01f, sprintLatchSeconds));
            else
                sprintSocketProgress = 0f;

            if (sprintSocketProgress >= 1f)
                SetSprintLatched(true);
        }
        else if (offset.y < 0f)
        {
            SetSprintLatched(false);
            sprintSocketProgress = 0f;
        }
    }

    Vector2 GetFixedStickOrigin()
    {
        var x = leftHanded ? SafeAreaPixels.xMax - 150f * controlsScale : SafeAreaPixels.xMin + 150f * controlsScale;
        var y = SafeAreaPixels.yMin + 112f * controlsScale;
        return new Vector2(x, y);
    }
#endif
}
