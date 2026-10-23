using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The input boundary for FrontRooms.
///
/// Gameplay code should read one <see cref="FrameSnapshot"/> per frame instead
/// of reading a platform input API directly. Desktop input is supplied by the
/// new Input System; touch UI and Editor autopilot can replace that snapshot
/// through the virtual/injected APIs below.
/// </summary>
public static class FrontRoomsInput
{
    /// <summary>All input values needed by the current FrontRooms gameplay loop.</summary>
    public readonly struct FrameSnapshot
    {
        public readonly Vector2 Move;
        public readonly Vector2 Look;
        public readonly bool SprintHeld;
        public readonly bool UseDown;
        public readonly bool UseHeld;
        public readonly bool PauseDown;
        public readonly bool StartDown;
        public readonly bool SettingsDown;
        public readonly bool RestartDown;
        public readonly bool ShotBackDown;
        /// <summary>EXAMINE zoom, in steps (+ is closer): ±1 per scroll gesture on desktop, a continuous fraction while
        /// pinching on touch. Only the close-up reads it (contract T3).</summary>
        public readonly float Zoom;
        /// <summary>EXAMINE zoom toggle: the right mouse button on desktop, a double tap on touch.</summary>
        public readonly bool ZoomToggleDown;

        public FrameSnapshot(
            Vector2 move,
            Vector2 look,
            bool sprintHeld = false,
            bool useDown = false,
            bool useHeld = false,
            bool pauseDown = false,
            bool startDown = false,
            bool settingsDown = false,
            bool restartDown = false,
            bool shotBackDown = false,
            float zoom = 0f,
            bool zoomToggleDown = false)
        {
            Move = Vector2.ClampMagnitude(move, 1f);
            Look = look;
            SprintHeld = sprintHeld;
            UseDown = useDown;
            UseHeld = useHeld;
            PauseDown = pauseDown;
            StartDown = startDown;
            SettingsDown = settingsDown;
            RestartDown = restartDown;
            ShotBackDown = shotBackDown;
            Zoom = zoom;
            ZoomToggleDown = zoomToggleDown;
        }

        public static FrameSnapshot Empty => new FrameSnapshot(Vector2.zero, Vector2.zero);
    }

    const float DesktopLookScale = 2.1f;

    static InputActionMap gameplay;
    static InputAction moveAction;
    static InputAction lookAction;
    static InputAction sprintAction;
    static InputAction useAction;
    static InputAction pauseAction;
    static InputAction startAction;
    static InputAction settingsAction;
    static InputAction restartAction;
    static InputAction shotBackAction;
    static InputAction zoomAction;
    static InputAction zoomToggleAction;

    // A scroll gesture is one zoom step: the first notch counts, then the wheel re-arms after 0.2 s without scrolling
    // (real time: the close-up runs at timeScale 0).
    const float ScrollRearmSeconds = .2f;
    static bool scrollArmed = true;
    static float lastScrollTime = -1f;

    static bool initialized;
    static bool frameReady;
    static int frameNumber = -1;
    static FrameSnapshot frame;

    // The virtual layer is intended for the mobile UI. It is an entire
    // snapshot so touch code can update held and edge-triggered values in one
    // operation and avoid coupling this facade to a particular Canvas system.
    static bool virtualActive;
    static FrameSnapshot virtualSnapshot;

#if UNITY_EDITOR
    // Editor-only injection is deliberately separate from the runtime virtual
    // layer. Autopilot can therefore replace all physical input for a frame
    // without accidentally shipping an editor test state in a player build.
    static bool editorSnapshotActive;
    static FrameSnapshot editorSnapshot;
#endif

    /// <summary>The latest snapshot. Reads physical devices once per Unity frame.</summary>
    public static FrameSnapshot Snapshot => ReadFrame();

    /// <summary>Short alias for callers that prefer an explicit per-frame poll.</summary>
    public static FrameSnapshot Poll() => ReadFrame();

    // Short property aliases keep call sites readable while the Snapshot form
    // remains available when a caller needs several values in one frame.
    public static Vector2 Move => ReadFrame().Move;
    public static Vector2 Look => ReadFrame().Look;
    public static bool SprintHeld => ReadFrame().SprintHeld;
    public static bool Use => ReadFrame().UseDown;
    public static bool UseDown => ReadFrame().UseDown;
    public static bool UseHeld => ReadFrame().UseHeld;
    public static bool Pause => ReadFrame().PauseDown;
    public static bool PauseDown => ReadFrame().PauseDown;
    public static bool Start => ReadFrame().StartDown;
    public static bool StartDown => ReadFrame().StartDown;
    public static bool Settings => ReadFrame().SettingsDown;
    public static bool SettingsDown => ReadFrame().SettingsDown;
    public static bool Restart => ReadFrame().RestartDown;
    public static bool RestartDown => ReadFrame().RestartDown;
    public static bool ShotBack => ReadFrame().ShotBackDown;
    public static bool ShotBackDown => ReadFrame().ShotBackDown;
    public static float Zoom => ReadFrame().Zoom;
    public static bool ZoomToggleDown => ReadFrame().ZoomToggleDown;

    /// <summary>
    /// Build and enable the action map before the first scene starts. The map
    /// is created in code so this facade does not require a generated asset or
    /// a scene-specific PlayerInput component.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void InitializeOnLoad() => EnsureInitialized();

    /// <summary>Read the physical, virtual, or Editor-injected frame state.</summary>
    public static FrameSnapshot ReadFrame()
    {
        EnsureInitialized();

        var now = Time.frameCount;
        if (frameReady && frameNumber == now) return frame;

#if UNITY_EDITOR
        if (editorSnapshotActive)
        {
            frame = editorSnapshot;
            frameNumber = now;
            frameReady = true;
            return frame;
        }
#endif

        frame = virtualActive ? virtualSnapshot : ReadPhysicalFrame();
        frameNumber = now;
        frameReady = true;
        return frame;
    }

    /// <summary>
    /// Explicitly invalidate the cached frame. Call this at the top of a
    /// custom player loop when it evaluates input outside Unity's normal
    /// Update ordering (tests and deterministic replay do this).
    /// </summary>
    public static void BeginFrame()
    {
        frameReady = false;
        frameNumber = -1;
    }

    /// <summary>Set the complete current virtual (normally touch) snapshot.</summary>
    public static void SetVirtualSnapshot(FrameSnapshot snapshot)
    {
        virtualSnapshot = snapshot;
        virtualActive = true;
        BeginFrame();
    }

    /// <summary>Stop overriding physical input with the virtual snapshot.</summary>
    public static void ClearVirtualSnapshot()
    {
        virtualSnapshot = FrameSnapshot.Empty;
        virtualActive = false;
        BeginFrame();
    }

    /// <summary>Whether a touch/controller layer currently owns the snapshot.</summary>
    public static bool VirtualSnapshotActive => virtualActive;

#if UNITY_EDITOR
    /// <summary>Whether Editor autopilot currently owns the snapshot.</summary>
    public static bool EditorSnapshotActive => editorSnapshotActive;

    /// <summary>Replace physical input for the current and subsequent frames.</summary>
    public static void InjectSnapshot(FrameSnapshot snapshot)
    {
        editorSnapshot = snapshot;
        editorSnapshotActive = true;
        BeginFrame();
    }

    /// <summary>Alias for autopilot call sites that make the target explicit.</summary>
    public static void InjectEditorSnapshot(FrameSnapshot snapshot) => InjectSnapshot(snapshot);

    /// <summary>Return control to physical/virtual input after an Editor test.</summary>
    public static void ClearInjectedSnapshot()
    {
        editorSnapshot = FrameSnapshot.Empty;
        editorSnapshotActive = false;
        BeginFrame();
    }

    /// <summary>Alias for autopilot cleanup.</summary>
    public static void ClearEditorSnapshot() => ClearInjectedSnapshot();
#endif

    /// <summary>
    /// Release the action map. This is primarily useful for EditMode tests;
    /// normal scenes should leave the singleton map alive for the process.
    /// </summary>
    public static void Shutdown()
    {
        if (gameplay != null) gameplay.Disable();
        gameplay = null;
        moveAction = lookAction = sprintAction = useAction = null;
        pauseAction = startAction = settingsAction = restartAction = null;
        shotBackAction = zoomAction = zoomToggleAction = null;
        scrollArmed = true;
        lastScrollTime = -1f;
        initialized = false;
        virtualActive = false;
        virtualSnapshot = FrameSnapshot.Empty;
#if UNITY_EDITOR
        editorSnapshotActive = false;
        editorSnapshot = FrameSnapshot.Empty;
#endif
        BeginFrame();
    }

    static void EnsureInitialized()
    {
        if (initialized && gameplay != null) return;

        gameplay = new InputActionMap("FrontRooms Gameplay");

        // Movement preserves the existing WASD desktop contract and accepts
        // arrow keys as the old map-test walker did.
        moveAction = gameplay.AddAction("Move", InputActionType.Value);
        moveAction.expectedControlType = "Vector2";
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");

        // Mouse delta is scaled to match the former Input.GetAxisRaw values in
        // FrontRooms3DGame (2.1 degrees per raw axis unit).
        lookAction = gameplay.AddAction("Look", InputActionType.Value);
        lookAction.expectedControlType = "Vector2";
        lookAction.AddBinding("<Mouse>/delta");
        lookAction.AddBinding("<Gamepad>/rightStick");

        sprintAction = gameplay.AddAction("Sprint", InputActionType.Button);
        sprintAction.AddBinding("<Keyboard>/leftShift");
        sprintAction.AddBinding("<Keyboard>/rightShift");

        useAction = gameplay.AddAction("Use", InputActionType.Button);
        useAction.expectedControlType = "Button";
        useAction.AddBinding("<Keyboard>/e");

        pauseAction = gameplay.AddAction("Pause", InputActionType.Button);
        pauseAction.expectedControlType = "Button";
        pauseAction.AddBinding("<Keyboard>/escape");

        startAction = gameplay.AddAction("Start", InputActionType.Button);
        startAction.expectedControlType = "Button";
        startAction.AddBinding("<Keyboard>/space");
        startAction.AddBinding("<Keyboard>/enter");

        settingsAction = gameplay.AddAction("Settings", InputActionType.Button);
        settingsAction.expectedControlType = "Button";
        settingsAction.AddBinding("<Keyboard>/o");

        restartAction = gameplay.AddAction("Restart", InputActionType.Button);
        restartAction.expectedControlType = "Button";
        restartAction.AddBinding("<Keyboard>/r");

        shotBackAction = gameplay.AddAction("Shot Back", InputActionType.Button);
        shotBackAction.expectedControlType = "Button";
        shotBackAction.AddBinding("<Keyboard>/s");

        // EXAMINE zoom (contract T3): the wheel steps it, the right button toggles it. Touch sends pinch and double tap
        // through the virtual snapshot instead.
        zoomAction = gameplay.AddAction("Zoom", InputActionType.Value);
        zoomAction.expectedControlType = "Axis";
        zoomAction.AddBinding("<Mouse>/scroll/y");

        zoomToggleAction = gameplay.AddAction("Zoom Toggle", InputActionType.Button);
        zoomToggleAction.expectedControlType = "Button";
        zoomToggleAction.AddBinding("<Mouse>/rightButton");

        gameplay.Enable();
        initialized = true;
    }

    static FrameSnapshot ReadPhysicalFrame()
    {
        var move = moveAction.ReadValue<Vector2>();
        if (move.sqrMagnitude > 1f) move.Normalize();

        var zoom = 0f;
        var scroll = zoomAction.ReadValue<float>();
        var now = Time.realtimeSinceStartup;
        if (Mathf.Abs(scroll) > .01f)
        {
            if (scrollArmed) { zoom = Mathf.Sign(scroll); scrollArmed = false; }
            lastScrollTime = now;
        }
        else if (!scrollArmed && now - lastScrollTime >= ScrollRearmSeconds) scrollArmed = true;

        return new FrameSnapshot(
            move,
            lookAction.ReadValue<Vector2>() * DesktopLookScale,
            sprintAction.IsPressed(),
            useAction.WasPressedThisFrame(),
            useAction.IsPressed(),
            pauseAction.WasPressedThisFrame(),
            startAction.WasPressedThisFrame(),
            settingsAction.WasPressedThisFrame(),
            restartAction.WasPressedThisFrame(),
            shotBackAction.WasPressedThisFrame(),
            zoom,
            zoomToggleAction.WasPressedThisFrame());
    }
}
