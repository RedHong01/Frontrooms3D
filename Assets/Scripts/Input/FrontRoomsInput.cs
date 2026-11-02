using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

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
        /// <summary>EXAMINE's TEXT (the transcript): a pad's north button; touch has its TEXT chip.</summary>
        public readonly bool TextToggleDown;
        /// <summary>SLOW held (Red, 2026-10-30): the slow walk the Relay never hears. Shift on a keyboard; a pad's small stick
        /// tilt and the phone's ring are the slow gait on their own side.</summary>
        public readonly bool SlowHeld;

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
            bool zoomToggleDown = false,
            bool textToggleDown = false,
            bool slowHeld = false)
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
            TextToggleDown = textToggleDown;
            SlowHeld = slowHeld;
        }

        public static FrameSnapshot Empty => new FrameSnapshot(Vector2.zero, Vector2.zero);
    }

    const float DesktopLookScale = 2.1f;

    static InputActionMap gameplay;
    static InputAction moveAction;
    static InputAction lookAction;
    static InputAction sprintAction;
    static InputAction slowAction;
    static InputAction useAction;
    static InputAction pauseAction;
    static InputAction startAction;
    static InputAction settingsAction;
    static InputAction restartAction;
    static InputAction shotBackAction;
    static InputAction zoomAction;
    static InputAction zoomToggleAction;
    static InputAction lookStickAction;
    static InputAction textToggleAction;

    /// <summary>The right stick at full tilt, degrees a second (yaw, pitch); a squared response keeps small tilts fine.</summary>
    public static readonly Vector2 StickLookRate = new Vector2(160f, 110f);
    static int physicalNumber = -1;
    static FrameSnapshot physical;

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
    public static bool SlowHeld => ReadFrame().SlowHeld;
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
        // The device in use is followed in every frame, also while the key map listens (2026-10-31).
        TrackDevice();

#if UNITY_EDITOR
        if (editorSnapshotActive)
        {
            frame = editorSnapshot;
            frameNumber = now;
            frameReady = true;
            return frame;
        }
#endif

        frame = virtualActive ? virtualSnapshot : SuspendGameplay ? FrameSnapshot.Empty : PhysicalFrame();
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

    /// <summary>Swallows the rest of this frame's input (2026-10-31): the desktop sheet calls it when a press closes it (A or
    /// Enter on RESUME or END RUN, B at its top), because the game reads this frame again after the sheet, and A is the pad's
    /// USE, B its BACK. The next frame reads the devices as usual; a button still held then starts nothing, as every use
    /// waits for a fresh press.</summary>
    public static void ConsumeFrame()
    {
        EnsureInitialized();
        frame = FrameSnapshot.Empty;
        frameNumber = Time.frameCount;
        frameReady = true;
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

    /// <summary>Tests only (2026-11-01): the devices are looked at again on the next read. An EditMode batch runs in one frame,
    /// so a pad added or removed there is seen only after this.</summary>
    public static void TrackAgainForTests() => deviceFrame = -1;
#endif

    /// <summary>
    /// Release the action map. This is primarily useful for EditMode tests;
    /// normal scenes should leave the singleton map alive for the process.
    /// </summary>
    public static void Shutdown()
    {
        if (gameplay != null) gameplay.Disable();
        gameplay = null;
        moveAction = lookAction = sprintAction = slowAction = useAction = null;
        pauseAction = startAction = settingsAction = restartAction = null;
        shotBackAction = zoomAction = zoomToggleAction = null;
        scrollArmed = true;
        lastScrollTime = -1f;
        useKeyName = null;
        legendText = null;
        SuspendGameplay = false;
        SuspendRestart = false;
        DeviceInUse = Device.Keyboard;
        deviceFrame = -1;
        // The pads are learned again on the next look (2026-11-01): a pad that is in then is no news.
        change = DeviceChange.None;
        padsSeen.Clear();
        padsKnown = keyboardSeen = false;
        padInHand = null;
        LastChange = DeviceChange.None;
        LastChangeFamily = PadFamily.None;
        LastChangeAt = float.NegativeInfinity;
        padMenu = default(PadMenu);
        padMenuFrame = -1;
        padHeld = Vector2Int.zero;
        // PlayStationPadOverride stays, as AppleKeysOverride does: the tests reload through here and keep the forced names.
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

        // Red, 2026-10-30: hold Ctrl to run, hold Shift to walk slowly (unheard). Binding 0 of each is the player's key on
        // the key map, binding 1 its locked second key (Right Ctrl, or Right Option on Apple keyboards; Right Shift).
        sprintAction = gameplay.AddAction("Sprint", InputActionType.Button);
        sprintAction.AddBinding("<Keyboard>/leftCtrl");
        sprintAction.AddBinding(SprintLockPath);

        slowAction = gameplay.AddAction("Slow", InputActionType.Button);
        slowAction.expectedControlType = "Button";
        slowAction.AddBinding("<Keyboard>/leftShift");
        slowAction.AddBinding("<Keyboard>/rightShift");

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

        AddGamepadBindings();
        gameplay.Enable();
        initialized = true;
        // After initialized: applying a saved key goes through ActionOf, which would build the map again.
        LoadBindings();
    }

    /// <summary>
    /// The keyboard and mouse controls the gameplay actions are bound to, as lower-case control paths
    /// ("&lt;keyboard&gt;/w", "&lt;mouse&gt;/rightbutton"), for the settings key map. The old Settings key (O) is left out:
    /// a desktop pause opens the settings page itself, so O never does anything there. So is T, the handheld keyboard's
    /// TEXT (only the touch layer reads it), so the map does not show it in use with no action of its own. LEFT CLICK is in
    /// (2026-10-31): the close-up's full-view tap, which no binding holds.
    /// </summary>
    public static System.Collections.Generic.HashSet<string> DesktopBoundPaths()
    {
        EnsureInitialized();
        var paths = new System.Collections.Generic.HashSet<string>();
        foreach (var action in gameplay.actions)
        {
            if (action == settingsAction || action == textToggleAction) continue;
            foreach (var binding in action.bindings)
            {
                if (binding.isComposite) continue;
                var path = binding.effectivePath;
                if (string.IsNullOrEmpty(path)) continue;
                path = path.ToLowerInvariant();
                if (path.StartsWith("<keyboard>/") || path.StartsWith("<mouse>/")) paths.Add(path);
            }
        }
        // A left click in a close-up is its full-view tap (2026-10-30 21:38), read in ReadPhysicalFrame and not through a
        // binding: without this the map would show LEFT CLICK free.
        paths.Add("<mouse>/leftbutton");
        return paths;
    }

    /// <summary>
    /// The pad's controls the desktop game reads, as lower-case control paths ("&lt;gamepad&gt;/buttonsouth"), for the
    /// CONTROLLER map (2026-10-31). Left out, as on the keyboard: the old Settings action, TEXT (Y: only the touch layer
    /// reads it) and the title's START (A and MENU; MENU is PAUSE's anyway).
    /// </summary>
    public static System.Collections.Generic.HashSet<string> PadBoundPaths()
    {
        EnsureInitialized();
        var paths = new System.Collections.Generic.HashSet<string>();
        foreach (var action in gameplay.actions)
        {
            if (action == settingsAction || action == textToggleAction || action == startAction) continue;
            foreach (var binding in action.bindings)
            {
                if (binding.isComposite) continue;
                var path = Normalise(binding.effectivePath);
                if (path.StartsWith("<gamepad>/", System.StringComparison.Ordinal)) paths.Add(path);
            }
        }
        return paths;
    }

    // ---------------------------------------------------------------- key bindings (desktop)

    /// <summary>
    /// What a player can rebind on the desktop key map (CONTROLS / KEYBOARD &amp; MOUSE, Red 2026-10-29): the W A S D walk,
    /// SPRINT (Ctrl), SLOW (Shift, Red 2026-10-30), USE, END RUN (Restart: R) and the close-up ZOOM button. Esc (pause and
    /// back), the mouse look, the wheel and START (Space or Enter on the title) stay fixed; the arrows (MOVE), Right Ctrl
    /// (SPRINT; Right Option on Apple keyboards) and Right Shift (SLOW) stay as locked second keys, so a player can always
    /// get out, move, run and creep.
    /// Slow is last of the keys in the enum so saved names keep their values; <see cref="Binds"/> lists it after Sprint.
    /// The pad targets come after it (CONTROLS / CONTROLLER, 2026-10-31; Red 16:39 "键盘/手柄映射页…做完"): the pad's USE (A),
    /// SPRINT (LB), BACK (B: out of a close-up) and ZOOM (RT), saved under their own names. MENU and both sticks stay
    /// fixed, the d-pad's up and down step the zoom, and L3 stays SPRINT's locked second button.
    /// </summary>
    public enum Bind { Forward, Back, Left, Right, Sprint, Use, Restart, Zoom, Slow, PadUse, PadSprint, PadBack, PadZoom }

    /// <summary>What binding a key to a target would do (<see cref="CheckBind"/>).</summary>
    public enum BindCheck { Ok, Same, Taken, Locked, Reserved }

    /// <summary>The keyboard and mouse targets (CONTROLS / KEYBOARD &amp; MOUSE), in the page's order.</summary>
    public static readonly Bind[] Binds = { Bind.Forward, Bind.Back, Bind.Left, Bind.Right, Bind.Sprint, Bind.Slow, Bind.Use, Bind.Restart, Bind.Zoom };

    /// <summary>The pad targets (CONTROLS / CONTROLLER, 2026-10-31), in the order of the row's preview: A, LB, B, RT.</summary>
    public static readonly Bind[] PadBinds = { Bind.PadUse, Bind.PadSprint, Bind.PadBack, Bind.PadZoom };

    /// <summary>A pad target (bound to a pad button; a key never binds it).</summary>
    public static bool IsPad(Bind b) => b >= Bind.PadUse;

    /// <summary>A pad control ("&lt;Gamepad&gt;/buttonSouth", any spelling).</summary>
    public static bool IsPadPath(string path) => Normalise(path).StartsWith("<gamepad>/", System.StringComparison.Ordinal);

    /// <summary>SPRINT's locked second pad button: L3, the left stick pressed (the map shows it with SPRINT, fixed).</summary>
    public const string PadLockPath = "<Gamepad>/leftStickPress";

    // Conflicts and swaps are looked for on the target's own device only: a key never takes a pad target's place.
    static Bind[] BindsOf(Bind b) => IsPad(b) ? PadBinds : Binds;

    /// <summary>
    /// Apple keyboards (a Mac, or a keyboard on an iPhone or iPad): Option is the Alt key and Command the meta key, and a
    /// MacBook or a Magic Keyboard has no Right Ctrl, while Ctrl + arrows switches Spaces. So SPRINT's locked second key is
    /// Right Option there, beside the arrows, and Right Ctrl elsewhere. Read at run time, so the Mac editor and its tests see
    /// what a Mac player sees.
    /// </summary>
    public static bool AppleKeys => AppleKeysOverride ?? (Application.platform == RuntimePlatform.OSXPlayer
        || Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.IPhonePlayer);

    /// <summary>Tests only: forces <see cref="AppleKeys"/> (null: the platform's own). Call <see cref="Shutdown"/> after.</summary>
    public static bool? AppleKeysOverride { get; set; }

    /// <summary>Tests and the capture harness only: forces <see cref="PlayStationPad"/> (null: the pad in hand), so pad names
    /// read the same whatever pad is paired to the machine. Kept across <see cref="Shutdown"/>, as AppleKeysOverride is: the
    /// caller clears it.</summary>
    public static bool? PlayStationPadOverride { get; set; }

    /// <summary>SPRINT's locked second key: Right Option on Apple keyboards, Right Ctrl elsewhere.</summary>
    public static string SprintLockPath => AppleKeys ? "<Keyboard>/rightAlt" : "<Keyboard>/rightCtrl";

    /// <summary>The PlayerPrefs key that holds the player's changed keys ("Use=&lt;Keyboard&gt;/f|…"). Tests point it elsewhere.</summary>
    public static string BindingsPrefKey = "FrontRooms.KeyBindings";

    /// <summary>Raised after any binding changes (an assignment, a swap, a reset).</summary>
    public static event System.Action BindingsChanged;

    /// <summary>
    /// While true (the key map is listening for a key), the game reads no physical input: the press being bound must not
    /// also restart, pause or use.
    /// </summary>
    public static bool SuspendGameplay { get; set; }

    /// <summary>While true (a key map page is open on the desktop sheet, 2026-10-31), the END RUN key reads nothing: browsing
    /// the map with R must not end the run. The sheet's END RUN button still does. The desktop sheet sets it.</summary>
    public static bool SuspendRestart { get; set; }

    static string useKeyName;

    static InputAction ActionOf(Bind b)
    {
        EnsureInitialized();
        switch (b)
        {
            case Bind.Sprint: return sprintAction;
            case Bind.Slow: return slowAction;
            case Bind.Use: return useAction;
            case Bind.Restart: return restartAction;
            case Bind.Zoom: return zoomToggleAction;
            case Bind.PadUse: return useAction;
            case Bind.PadSprint: return sprintAction;
            case Bind.PadBack: return shotBackAction;
            case Bind.PadZoom: return zoomToggleAction;
            default: return moveAction;
        }
    }

    // The W A S D composite's parts are Move's bindings 1–4 (0 is the composite; the arrows' composite is 5–9). A pad
    // target is its action's pad binding, added after the keys (AddGamepadBindings): 1, and 2 for SPRINT (3 is its L3).
    static int IndexOf(Bind b) => b == Bind.Forward ? 1 : b == Bind.Back ? 2 : b == Bind.Left ? 3 : b == Bind.Right ? 4
        : b == Bind.PadSprint ? 2 : IsPad(b) ? 1 : 0;

    /// <summary>A control path in one spelling, for comparisons ("&lt;keyboard&gt;/leftshift").</summary>
    public static string Normalise(string path) => string.IsNullOrEmpty(path) ? "" : path.Trim().ToLowerInvariant();

    static bool Same(string a, string b) => Normalise(a) == Normalise(b);

    public static string PathOf(Bind b) => ActionOf(b).bindings[IndexOf(b)].effectivePath;
    public static string DefaultPathOf(Bind b) => ActionOf(b).bindings[IndexOf(b)].path;
    public static bool IsDefault(Bind b) => Same(PathOf(b), DefaultPathOf(b));

    /// <summary>Every key and mouse target on its default (the pad's buttons do not count).</summary>
    public static bool AllDefault
    {
        get
        {
            foreach (var b in Binds) if (!IsDefault(b)) return false;
            return true;
        }
    }

    /// <summary>Every pad target on its default button (CONTROLS / CONTROLLER, 2026-10-31).</summary>
    public static bool PadAllDefault
    {
        get
        {
            foreach (var b in PadBinds) if (!IsDefault(b)) return false;
            return true;
        }
    }

    /// <summary>The key that also steps back out of a shot or a close-up: it follows Back (S by default).</summary>
    public static string StepBackPath
    {
        get { EnsureInitialized(); return shotBackAction.bindings[0].effectivePath; }
    }

    /// <summary>Who keeps a fixed or locked key: PAUSE (Esc), MOVE (the arrows), SPRINT (Right Ctrl; Right Option on Apple
    /// keyboards, where Right Ctrl is an ordinary key), SLOW (Right Shift), ZOOM STEP (the wheel), LOOK (the mouse's
    /// movement), ZOOM (LEFT CLICK: the close-up's full-view tap, 2026-10-31); on a pad (CONTROLLER, 2026-10-31) PAUSE
    /// (MENU), MOVE (the left stick), LOOK (the right stick), ZOOM STEP (the d-pad's up and down) and SPRINT (L3); null for
    /// any other key or button.</summary>
    public static string LockedOwner(string path)
    {
        switch (Normalise(path))
        {
            case "<keyboard>/escape": return "PAUSE";
            case "<keyboard>/uparrow": case "<keyboard>/downarrow": case "<keyboard>/leftarrow": case "<keyboard>/rightarrow": return "MOVE";
            case "<keyboard>/rightctrl": return AppleKeys ? null : "SPRINT";
            case "<keyboard>/rightalt": return AppleKeys ? "SPRINT" : null;
            case "<keyboard>/rightshift": return "SLOW";
            case "<mouse>/scroll": case "<mouse>/scroll/y": case "<mouse>/scroll/x": return "ZOOM STEP";
            case "<mouse>/delta": case "<mouse>/position": return "LOOK";
            // A left click is the close-up's full-view tap since 2026-10-30 21:38, read outside the bindings: ZOOM's
            // locked second key, so no other action can take it (2026-10-31).
            case "<mouse>/leftbutton": return "ZOOM";
            case "<gamepad>/start": return "PAUSE";
            case "<gamepad>/leftstick": return "MOVE";
            case "<gamepad>/rightstick": return "LOOK";
            case "<gamepad>/dpad": case "<gamepad>/dpad/up": case "<gamepad>/dpad/down": return "ZOOM STEP";
            case "<gamepad>/leftstickpress": return "SPRINT";
        }
        return null;
    }

    /// <summary>
    /// What binding <paramref name="path"/> to <paramref name="b"/> would do: Same (it is b's already), Reserved (Esc,
    /// the wheel, the mouse's movement, a pad's MENU, its right stick and the d-pad's up and down: never; also a control of
    /// the other device), Locked (another action's locked second key, named in <paramref name="lockedName"/>), Taken
    /// (another target of the same device has it, <paramref name="owner"/>: the key map asks before it swaps), else Ok.
    /// </summary>
    public static BindCheck CheckBind(Bind b, string path, out Bind owner, out string lockedName)
    {
        owner = b;
        lockedName = LockedOwner(path);
        if (Normalise(path).Length == 0) return BindCheck.Reserved;
        // One device per map (2026-10-31): a pad button never binds a key target, a key never a pad target.
        if (IsPad(b) != IsPadPath(path)) return BindCheck.Reserved;
        if (Same(PathOf(b), path)) return BindCheck.Same;
        if (lockedName == "PAUSE" || lockedName == "ZOOM STEP" || lockedName == "LOOK") return BindCheck.Reserved;
        if (lockedName != null && !OwnsLocked(b, lockedName)) return BindCheck.Locked;
        foreach (var other in BindsOf(b))
        {
            if (other == b || !Same(PathOf(other), path)) continue;
            owner = other;
            return BindCheck.Taken;
        }
        return BindCheck.Ok;
    }

    // SPRINT may take its own locked key (Right Ctrl, Right Option on Apple) as its main key too, and the pad's SPRINT its
    // L3; SLOW its own (Right Shift); ZOOM LEFT CLICK.
    static bool OwnsLocked(Bind b, string lockedName) => (lockedName == "SPRINT" && (b == Bind.Sprint || b == Bind.PadSprint))
        || (b == Bind.Slow && lockedName == "SLOW") || (b == Bind.Zoom && lockedName == "ZOOM");

    /// <summary>Binds <paramref name="b"/> to <paramref name="path"/> as it is (the caller checked it), and saves.</summary>
    public static void Assign(Bind b, string path)
    {
        Apply(b, path);
        SaveBindings();
    }

    /// <summary><paramref name="b"/> takes <paramref name="path"/> and the target that had it takes b's old key (or button:
    /// a pad target swaps with pad targets only). A locked key b holds (ZOOM's LEFT CLICK, the pad SPRINT's L3, SPRINT's
    /// Right Ctrl, SLOW's Right Shift) never goes to a target that may not hold it (2026-10-31): that target goes back to its
    /// own default instead, and any clash that leaves is resolved as at load. Saved.</summary>
    public static void SwapInto(Bind b, string path)
    {
        var old = PathOf(b);
        var oldLock = LockedOwner(old);
        var fallback = false;
        foreach (var other in BindsOf(b))
        {
            if (other == b || !Same(PathOf(other), path)) continue;
            if (oldLock == null || OwnsLocked(other, oldLock)) Apply(other, old);
            else { Apply(other, DefaultPathOf(other)); fallback = true; }
        }
        Apply(b, path);
        if (fallback) ResolveClashes(BindsOf(b));
        SaveBindings();
    }

    /// <summary>One target back to its default key; a target that has that key now takes this one's key. Saved.</summary>
    public static void ResetBind(Bind b)
    {
        if (!IsDefault(b)) SwapInto(b, DefaultPathOf(b));
    }

    /// <summary>Every key back to the defaults; the pad's buttons stay (CONTROLLER has its own DEFAULTS). Saved.</summary>
    public static void ResetAllBinds()
    {
        EnsureInitialized();
        foreach (var b in Binds) Apply(b, DefaultPathOf(b));
        SaveBindings();
    }

    /// <summary>Every pad button back to the defaults (CONTROLLER's DEFAULTS, 2026-10-31); the keys stay. Saved.</summary>
    public static void ResetAllPadBinds()
    {
        EnsureInitialized();
        foreach (var b in PadBinds) Apply(b, DefaultPathOf(b));
        SaveBindings();
    }

    static void Apply(Bind b, string path)
    {
        var action = ActionOf(b);
        var index = IndexOf(b);
        if (Same(path, action.bindings[index].path)) action.RemoveBindingOverride(index);
        else action.ApplyBindingOverride(index, path);
        // S also steps back out of a shot or a close-up: that key follows Back.
        if (b != Bind.Back) return;
        if (Same(path, shotBackAction.bindings[0].path)) shotBackAction.RemoveBindingOverride(0);
        else shotBackAction.ApplyBindingOverride(0, path);
    }

    // Saved by target name, not with the Input System's JSON: that names bindings by id, and a map made in code gets new
    // ids at every launch.
    static void SaveBindings()
    {
        Persist();
        BeginFrame();
        BindingsChanged?.Invoke();
    }

    static void Persist()
    {
        useKeyName = null;
        legendText = null;
        try
        {
            var saved = new System.Text.StringBuilder();
            foreach (var b in Binds)
                if (!IsDefault(b)) saved.Append(saved.Length > 0 ? "|" : "").Append(b).Append('=').Append(PathOf(b));
            // The pad's buttons after the keys (2026-10-31), so a save with no pad change is written as it was before.
            foreach (var b in PadBinds)
                if (!IsDefault(b)) saved.Append(saved.Length > 0 ? "|" : "").Append(b).Append('=').Append(PathOf(b));
            if (saved.Length == 0) PlayerPrefs.DeleteKey(BindingsPrefKey);
            else PlayerPrefs.SetString(BindingsPrefKey, saved.ToString());
            PlayerPrefs.Save();
        }
        catch (System.Exception e) { Debug.LogWarning("[FrontRoomsInput] key bindings not saved: " + e.Message); }
    }

    static void LoadBindings()
    {
        useKeyName = null;
        legendText = null;
        string saved = null;
        try { saved = PlayerPrefs.GetString(BindingsPrefKey, ""); } catch (System.Exception) { }
        if (string.IsNullOrEmpty(saved)) return;
        var ignored = false;
        foreach (var entry in saved.Split('|'))
        {
            var cut = entry.IndexOf('=');
            if (cut <= 0) continue;
            // A target's name exactly as Persist writes it (2026-10-31): Enum.TryParse also takes numbers ("9") and
            // undefined values, which would land on a pad target or on binding 0 of Move (the W A S D composite). An
            // unknown name is skipped, as before.
            var name = entry.Substring(0, cut);
            if (!System.Enum.TryParse<Bind>(name, out var b) || !System.Enum.IsDefined(typeof(Bind), b) || b.ToString() != name) continue;
            var path = entry.Substring(cut + 1);
            var locked = LockedOwner(path);
            // A key on a key target, a pad button on a pad target, a fixed or locked control only for its owner.
            // LEFT CLICK (2026-10-31): from 10-29 the map could bind it to any target, and since 10-30 21:38 it is the
            // close-up's full-view tap, read outside the bindings. It is ZOOM's locked key now, so a USE, END RUN, SPRINT,
            // SLOW or walk key saved on it is ignored here: that action is back on its default key and the save is
            // rewritten without it (below). ZOOM may keep LEFT CLICK.
            if (Normalise(path).Length > 0 && IsPad(b) == IsPadPath(path) && (locked == null || OwnsLocked(b, locked))) Apply(b, path);
            else
            {
                Debug.LogWarning("[FrontRoomsInput] saved key ignored: " + entry);
                ignored = true;
            }
        }
        // A key saved before a default moved (2026-10-30: SPRINT to Ctrl, the new SLOW on Shift) can share a key with an
        // action still on its default. The default wins: the saved one goes back to its own default, and again until no
        // two actions share a key (a swap the player made, E and R traded, shares nothing and stays). The pad's buttons
        // the same way among themselves (2026-10-31). Both run (|, not ||); a resolved clash or an ignored entry rewrites
        // the save once, so its warning does not come back at every launch.
        var resolved = ResolveClashes(Binds) | ResolveClashes(PadBinds);
        if (resolved || ignored) Persist();
    }

    static bool ResolveClashes(Bind[] list)
    {
        var resolved = false;
        for (var round = 0; round < list.Length; round++)
        {
            var clash = false;
            foreach (var a in list)
            {
                if (IsDefault(a)) continue;
                foreach (var other in list)
                {
                    if (other == a || !Same(PathOf(a), PathOf(other))) continue;
                    Debug.LogWarning("[FrontRoomsInput] saved key " + a + "=" + PathOf(a) + " now belongs to " + other + ": " + a + " goes back to its default");
                    Apply(a, DefaultPathOf(a));
                    clash = true;
                    break;
                }
                if (clash) break;
            }
            if (!clash) break;
            resolved = true;
        }
        return resolved;
    }

    /// <summary>
    /// A control path as the prompts and the settings name it: a letter as the player's keyboard prints it ("Z" for the
    /// W key on an AZERTY board: the bindings are physical keys), words where Bayon has no glyph ("UP", "COMMA"),
    /// "LEFT CLICK" for mouse buttons, and a pad's controls as the pad in hand prints them (A or CROSS, LB or L1, LS or L3,
    /// D-PAD UP, LEFT STICK: CONTROLLER, 2026-10-31).
    /// </summary>
    public static string KeyName(string path)
    {
        var p = Normalise(path);
        if (p.Length == 0) return "UNBOUND";
        if (p.StartsWith("<mouse>/"))
        {
            switch (p.Substring(8))
            {
                case "leftbutton": return "LEFT CLICK";
                case "rightbutton": return "RIGHT CLICK";
                case "middlebutton": return "MIDDLE CLICK";
                case "backbutton": return "MOUSE 4";
                case "forwardbutton": return "MOUSE 5";
                case "scroll": case "scroll/y": return "WHEEL";
                case "delta": return "MOUSE";
            }
            return p.Substring(8).ToUpperInvariant();
        }
        if (p.StartsWith("<gamepad>/")) return PadPathName(p.Substring(10));
        if (!p.StartsWith("<keyboard>/")) return p.ToUpperInvariant();
        var k = p.Substring(11);
        switch (k)
        {
            case "leftshift": case "rightshift": return "SHIFT";
            case "leftctrl": case "rightctrl": return "CTRL";
            case "leftalt": case "rightalt": return AppleKeys ? "OPTION" : "ALT";
            case "leftmeta": case "rightmeta": return AppleKeys ? "CMD" : "WIN";
            case "contextmenu": return "MENU";
            case "space": return "SPACE";
            case "enter": return "ENTER";
            case "numpadenter": return "NUM ENTER";
            case "tab": return "TAB";
            case "backspace": return "BACKSPACE";
            case "capslock": return "CAPS";
            case "escape": return "ESC";
            case "uparrow": return "UP";
            case "downarrow": return "DOWN";
            case "leftarrow": return "LEFT";
            case "rightarrow": return "RIGHT";
            case "backquote": return "BACKQUOTE";
            case "minus": return "MINUS";
            case "equals": return "EQUALS";
            case "leftbracket": return "LEFT BRACKET";
            case "rightbracket": return "RIGHT BRACKET";
            case "backslash": return "BACKSLASH";
            case "semicolon": return "SEMICOLON";
            case "quote": return "QUOTE";
            case "comma": return "COMMA";
            case "period": return "PERIOD";
            case "slash": return "SLASH";
            case "delete": return "DELETE";
            case "insert": return "INSERT";
            case "home": return "HOME";
            case "end": return "END";
            case "pageup": return "PAGE UP";
            case "pagedown": return "PAGE DOWN";
        }
        if (k.StartsWith("numpad")) return "NUM " + k.Substring(6).ToUpperInvariant();
        if (k.Length == 1 && char.IsLetter(k[0]) && Keyboard.current != null && System.Enum.TryParse<Key>(k, true, out var key))
        {
            var shown = Keyboard.current[key].displayName;
            if (!string.IsNullOrEmpty(shown) && shown.Length == 1 && char.IsLetter(shown[0])) return shown.ToUpperInvariant();
        }
        return k.ToUpperInvariant();
    }

    // A pad control by its lower-case name after "<gamepad>/" (2026-10-31): the face buttons, MENU and the shoulders as
    // PadName prints them; VIEW (SHARE on a DualShock 4, CREATE on a DualSense); a stick pressed as Xbox names it, LS and RS
    // (L3 and R3 on PlayStation); words for the sticks and the d-pad.
    static string PadPathName(string control)
    {
        switch (control)
        {
            case "buttonsouth": return PadName(PadButton.South);
            case "buttoneast": return PadName(PadButton.East);
            case "buttonwest": return PadName(PadButton.West);
            case "buttonnorth": return PadName(PadButton.North);
            case "start": return PadName(PadButton.Menu);
            case "select": return PlayStationPad ? (DualSensePad ? "CREATE" : "SHARE") : "VIEW";
            case "leftshoulder": return PadName(PadButton.LeftShoulder);
            case "rightshoulder": return PadName(PadButton.RightShoulder);
            case "lefttrigger": return PadName(PadButton.LeftTrigger);
            case "righttrigger": return PadName(PadButton.RightTrigger);
            case "leftstickpress": return PlayStationPad ? "L3" : "LS";
            case "rightstickpress": return PlayStationPad ? "R3" : "RS";
            case "leftstick": return "LEFT STICK";
            case "rightstick": return "RIGHT STICK";
            case "dpad": return "D-PAD";
            case "dpad/up": return "D-PAD UP";
            case "dpad/down": return "D-PAD DOWN";
            case "dpad/left": return "D-PAD LEFT";
            case "dpad/right": return "D-PAD RIGHT";
        }
        return control.ToUpperInvariant();
    }

    /// <summary>A target's control as bound and named (a key, a mouse button or a pad button).</summary>
    public static string ControlName(Bind b) => KeyName(PathOf(b));

    /// <summary>The USE key's name (E unless the player changed it).</summary>
    public static string UseKey => useKeyName ?? (useKeyName = KeyName(PathOf(Bind.Use)));

    /// <summary>
    /// A desktop prompt with the USE key the player chose: "E  ·  OPEN DOOR" reads "F  ·  OPEN DOOR"; also "HOLD E  ·  …",
    /// "TAP E  ·  …" and the screens' "E  TURN OFF". Unchanged while USE is E.
    /// </summary>
    public static string WithKeyNames(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        var use = UseKey;
        if (use == "E") return text;
        if (text.StartsWith("E  ")) return use + text.Substring(1);
        if (text.StartsWith("HOLD E  ")) return "HOLD " + use + text.Substring(6);
        if (text.StartsWith("TAP E  ")) return "TAP " + use + text.Substring(5);
        return text;
    }

    /// <summary>A prompt with the pad's USE button as the player bound it on CONTROLS / CONTROLLER: "E  ·  OPEN DOOR" reads
    /// "A  ·  OPEN DOOR" (CROSS on PlayStation), "HOLD E  ·  …" HOLD A and "TAP E  ·  …" TAP A, those two words through the tables.</summary>
    public static string WithPadNames(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        var a = ControlName(Bind.PadUse);
        if (text.StartsWith("E  ")) return a + text.Substring(1);
        if (text.StartsWith("HOLD E  ")) return FrontRoomsLanguage.F("HOLD {0}", a) + text.Substring(6);
        if (text.StartsWith("TAP E  ")) return FrontRoomsLanguage.F("TAP {0}", a) + text.Substring(5);
        return text;
    }

    /// <summary>A prompt with the controls of the device in use (<see cref="PadInUse"/>): the pad's buttons or the keys, as bound.</summary>
    public static string WithControlNames(string text) => PadInUse ? WithPadNames(text) : WithKeyNames(text);

    /// <summary>The five pairs of the T5 legend (2026-11-01: the touch legend names a pad's or a keyboard's controls with them).</summary>
    public enum LegendPart { Move, Look, Sprint, Slow, Use }

    /// <summary>One pair of the T5 legend, with the player's bindings: on a pad "LEFT STICK  MOVE", "RIGHT STICK  LOOK", "LB  SPRINT",
    /// "HALF TILT  SLOW", "A  USE"; on a keyboard "WASD  MOVE" (the walk's four names run together while each is one letter:
    /// ZQSD on AZERTY), "MOUSE  LOOK", "CTRL  SPRINT", "SHIFT  SLOW", "E  USE". Each pair goes through the tables.</summary>
    public static string LegendPair(LegendPart part, bool pad)
    {
        switch (part)
        {
            case LegendPart.Move:
            {
                if (pad) return FrontRoomsLanguage.F("{0}  MOVE", FrontRoomsLanguage.T("LEFT STICK"));
                var f = ControlName(Bind.Forward); var l = ControlName(Bind.Left); var b = ControlName(Bind.Back); var r = ControlName(Bind.Right);
                return FrontRoomsLanguage.F("{0}  MOVE", f.Length == 1 && l.Length == 1 && b.Length == 1 && r.Length == 1 ? f + l + b + r : f + " " + l + " " + b + " " + r);
            }
            case LegendPart.Look: return FrontRoomsLanguage.F("{0}  LOOK", FrontRoomsLanguage.T(pad ? "RIGHT STICK" : "MOUSE"));
            case LegendPart.Sprint: return FrontRoomsLanguage.F("{0}  SPRINT", ControlName(pad ? Bind.PadSprint : Bind.Sprint));
            case LegendPart.Slow: return FrontRoomsLanguage.F("{0}  SLOW", pad ? FrontRoomsLanguage.T("HALF TILT") : ControlName(Bind.Slow));
            default: return FrontRoomsLanguage.F("{0}  USE", pad ? ControlName(Bind.PadUse) : UseKey);
        }
    }

    static string legendText;
    static bool legendPad, legendPlayStation;
    static FrontRoomsLanguage.Language legendLanguage;
    static Keyboard legendKeyboard;

    /// <summary>The T5 controls legend for the device in use, with the player's bindings (2026-10-31, for the title and pause
    /// legend): "WASD  MOVE    MOUSE  LOOK    CTRL  SPRINT    SHIFT  SLOW    E  USE" on a keyboard with the defaults, "LEFT STICK
    /// MOVE    RIGHT STICK  LOOK    LB  SPRINT    HALF TILT  SLOW    A  USE" on a pad: the five <see cref="LegendPair"/>s, four
    /// spaces apart.</summary>
    public static string ControlsLegend()
    {
        // Made again only when what it names changed (2026-11-01: the title and pause legends ask every frame): the bindings
        // (Persist clears it), the device in use, PlayStation names, the language and the keyboard (its printed letters).
        var pad = PadInUse;
        var ps = PlayStationPad;
        if (legendText != null && pad == legendPad && ps == legendPlayStation && legendLanguage == FrontRoomsLanguage.Current && legendKeyboard == Keyboard.current) return legendText;
        legendPad = pad; legendPlayStation = ps; legendLanguage = FrontRoomsLanguage.Current; legendKeyboard = Keyboard.current;
        return legendText = LegendPair(LegendPart.Move, pad) + "    " + LegendPair(LegendPart.Look, pad) + "    " + LegendPair(LegendPart.Sprint, pad)
            + "    " + LegendPair(LegendPart.Slow, pad) + "    " + LegendPair(LegendPart.Use, pad);
    }

    /// <summary>The caught card's line: "R  TRY AGAIN" with the END RUN key as bound; with a pad in use the menus' A (CROSS), which
    /// 关卡设计's suggested retry takes (NOTES_FOR_OWNERS.md §1.3).</summary>
    public static string TryAgainLine() => FrontRoomsLanguage.F("{0}  TRY AGAIN", PadInUse ? PadName(PadButton.South) : ControlName(Bind.Restart));

    /// <summary>
    /// A controller (Red, 2026-10-29: phones and iPads play with a pad or a keyboard too; Mac and Windows get it as well).
    /// Each pad binding comes after the action's own, so the keyboard bindings keep their indices. The left stick's tilt
    /// is the gait on handhelds (under 0.7: the slow, unheard walk); LB or a held L3 runs; A uses (held: breaks glass);
    /// MENU pauses; B steps back out of a close-up; Y is its TEXT; RT toggles its zoom and the d-pad steps it.
    /// Use's, Shot Back's and Zoom Toggle's binding 1 and Sprint's binding 2 are the pad targets the player can rebind
    /// (CONTROLLER, 2026-10-31: IndexOf): do not reorder or insert bindings here.
    /// </summary>
    static void AddGamepadBindings()
    {
        moveAction.AddBinding("<Gamepad>/leftStick");
        lookStickAction = gameplay.AddAction("Look Stick", InputActionType.Value);
        lookStickAction.expectedControlType = "Vector2";
        lookStickAction.AddBinding("<Gamepad>/rightStick");
        sprintAction.AddBinding("<Gamepad>/leftShoulder");
        sprintAction.AddBinding(PadLockPath);
        useAction.AddBinding("<Gamepad>/buttonSouth");
        pauseAction.AddBinding("<Gamepad>/start");
        startAction.AddBinding("<Gamepad>/buttonSouth");
        startAction.AddBinding("<Gamepad>/start");
        shotBackAction.AddBinding("<Gamepad>/buttonEast");
        zoomAction.AddCompositeBinding("1DAxis").With("Negative", "<Gamepad>/dpad/down").With("Positive", "<Gamepad>/dpad/up");
        zoomToggleAction.AddBinding("<Gamepad>/rightTrigger");
        textToggleAction = gameplay.AddAction("Text Toggle", InputActionType.Button);
        textToggleAction.expectedControlType = "Button";
        textToggleAction.AddBinding("<Gamepad>/buttonNorth");
        textToggleAction.AddBinding("<Keyboard>/t");
    }

    /// <summary>The physical devices this frame (keyboard, mouse, pad), whatever the touch layer publishes: the handheld
    /// touch layer merges it into its own snapshot while a pad or a keyboard is in use. Read once per frame.</summary>
    public static FrameSnapshot PhysicalFrame()
    {
        EnsureInitialized();
        if (physicalNumber == Time.frameCount) return physical;
        physical = ReadPhysicalFrame();
        physicalNumber = Time.frameCount;
        return physical;
    }

    /// <summary>An outside device used this frame (a handheld switches its controls on it).</summary>
    public enum Device { None, Gamepad, Keyboard }

    /// <summary>Which outside device was used this frame: a pad's stick, trigger or button, or a key pressed.</summary>
    public static Device ExternalActivity()
    {
        var pad = Gamepad.current;
        if (pad != null && pad.wasUpdatedThisFrame && PadUsed(pad)) return Device.Gamepad;
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.anyKey.wasPressedThisFrame) return Device.Keyboard;
        return Device.None;
    }

    static bool PadUsed(Gamepad pad)
    {
        if (pad.leftStick.ReadValue().sqrMagnitude > .09f || pad.rightStick.ReadValue().sqrMagnitude > .09f) return true;
        if (pad.leftTrigger.ReadValue() > .3f || pad.rightTrigger.ReadValue() > .3f) return true;
        foreach (var control in pad.allControls)
            if (control is ButtonControl button && !button.synthetic && button.wasPressedThisFrame) return true;
        return false;
    }

    /// <summary>The desktop player's device now (Mac, Windows, WebGL; prompts, the sheet's key lines and legends name its controls):
    /// the pad once its stick, trigger or a button is used, the keyboard and mouse again once a key is pressed, the mouse moves or
    /// clicks or scrolls. Starts as Keyboard; Keyboard again when the pad is unplugged, and when the pad in hand goes while another
    /// is still in (2026-11-01: that one's first press makes it the pad in hand). Tracked once per frame.</summary>
    public static Device DeviceInUse { get; private set; } = Device.Keyboard;
    static int deviceFrame = -1;

    static void TrackDevice()
    {
        if (deviceFrame == Time.frameCount) return;
        deviceFrame = Time.frameCount;
        // The device in use as last frame left it, before this frame's input moves it (2026-11-01): a pad, or a handheld's
        // keyboard, that goes while it is the one in use is a loss the game pauses for.
        var handheld = FrontRoomsHandheld.Active;
        var mode = FrontRoomsTouchControls.CurrentMode;
        var padWasInUse = handheld ? mode == FrontRoomsTouchControls.InputMode.Gamepad : DeviceInUse == Device.Gamepad;
        change = TrackPads(padWasInUse);
        // FrontRoomsHardwareKeyboard removes its keyboard when it is unplugged.
        var keyboard = Keyboard.current != null;
        if (change == DeviceChange.None && handheld && mode == FrontRoomsTouchControls.InputMode.Keyboard && keyboardSeen && !keyboard) change = DeviceChange.InUseLost;
        keyboardSeen = keyboard;
        var used = ExternalActivity();
        var mouse = Mouse.current;
        if (used == Device.None && mouse != null && (mouse.delta.ReadValue().sqrMagnitude > 4f || mouse.leftButton.wasPressedThisFrame
            || mouse.rightButton.wasPressedThisFrame || Mathf.Abs(mouse.scroll.y.ReadValue()) > .01f)) used = Device.Keyboard;
        if (used != Device.None) DeviceInUse = used;
        if (used == Device.Gamepad) padInHand = Gamepad.current;
        // The pad in hand went: the keys again, even with another pad still in (its first press makes it the pad in hand).
        if (change == DeviceChange.InUseLost && !handheld) DeviceInUse = Device.Keyboard;
        if (DeviceInUse == Device.Gamepad && Gamepad.current == null) DeviceInUse = Device.Keyboard;
    }

    // Gamepad.all against last frame's pads. The first look after a launch or a Shutdown only learns them: a pad already in at
    // launch is no news (CONTROLS says CONNECTED all the same).
    static DeviceChange TrackPads(bool padWasInUse)
    {
        var result = DeviceChange.None;
        for (var i = padsSeen.Count - 1; i >= 0; i--)
        {
            var pad = padsSeen[i];
            if (pad.added) continue;
            padsSeen.RemoveAt(i);
            var inHand = padWasInUse && (pad == padInHand || padInHand == null || !padInHand.added);
            if (inHand || result == DeviceChange.None) result = inHand ? DeviceChange.InUseLost : DeviceChange.PadDisconnected;
            if (pad == padInHand) padInHand = null;
            NoteChange(DeviceChange.PadDisconnected, pad);
        }
        var all = Gamepad.all;
        for (var i = 0; i < all.Count; i++)
        {
            if (padsSeen.Contains(all[i])) continue;
            padsSeen.Add(all[i]);
            if (!padsKnown) continue;
            if (result == DeviceChange.None) result = DeviceChange.PadConnected;
            NoteChange(DeviceChange.PadConnected, all[i]);
        }
        padsKnown = true;
        return result;
    }

    static void NoteChange(DeviceChange what, Gamepad pad)
    {
        LastChange = what;
        LastChangeFamily = FamilyOf(pad);
        LastChangeAt = Time.unscaledTime;
    }

    /// <summary>The player is on a pad: a handheld's touch layer says so (its mode, which the layer's Awake starts at Touch after
    /// every scene load since 2026-10-31), elsewhere <see cref="DeviceInUse"/>. The pad is Gamepad.current: the last one used,
    /// which the Input System moves to whichever pad sent input last, so with two pads the last one used is the one named;
    /// under Steam Input or in a browser a PlayStation pad may arrive as a generic pad and be named A B X Y. The pad in use going
    /// away is <see cref="DeviceChange.InUseLost"/> for that frame, and the game pauses the run for it (2026-11-01).</summary>
    public static bool PadInUse
    {
        get
        {
            if (FrontRoomsHandheld.Active) return FrontRoomsTouchControls.CurrentMode == FrontRoomsTouchControls.InputMode.Gamepad;
            TrackDevice();
            return DeviceInUse == Device.Gamepad;
        }
    }

    /// <summary>What a pad is (2026-11-01, Red: "如果当前设备已经链接了手柄，应该显示一个手柄以链接的状态"): the connected notice,
    /// CONTROLS' CONTROLLER row and the touch sheet's CONTROLLER list name it by its family.</summary>
    public enum PadFamily { None, Xbox, PlayStation, Generic }

    /// <summary>A pad's family by the test the button names use (<see cref="PlayStationPad"/>): a DualShock or a DualSense is
    /// PlayStation; an XInputController (the base of every Xbox layout on Windows, macOS and iOS) Xbox; any other pad (MFi, a
    /// browser's, Steam Input's) Generic. <see cref="PlayStationPadOverride"/> forces it as it forces the names: true
    /// PlayStation, false Xbox.</summary>
    public static PadFamily FamilyOf(Gamepad pad)
    {
        if (pad == null) return PadFamily.None;
        if (PlayStationPadOverride != null) return PlayStationPadOverride.Value ? PadFamily.PlayStation : PadFamily.Xbox;
        if (pad is UnityEngine.InputSystem.DualShock.DualShockGamepad) return PadFamily.PlayStation;
        return pad is UnityEngine.InputSystem.XInput.XInputController ? PadFamily.Xbox : PadFamily.Generic;
    }

    /// <summary>A pad is in (plugged or paired). A browser shows a pad only after its first press (the Gamepad API).</summary>
    public static bool PadConnected => Gamepad.all.Count > 0;

    /// <summary>The family of the pad in hand, else of the first pad in, else None.</summary>
    public static PadFamily Family => FamilyOf(Gamepad.current ?? (Gamepad.all.Count > 0 ? Gamepad.all[0] : null));

    /// <summary>A family as the player reads it: XBOX CONTROLLER, PLAYSTATION CONTROLLER, else CONTROLLER.</summary>
    public static string FamilyName(PadFamily family)
    {
        switch (family)
        {
            case PadFamily.Xbox: return FrontRoomsLanguage.T("XBOX CONTROLLER");
            case PadFamily.PlayStation: return FrontRoomsLanguage.T("PLAYSTATION CONTROLLER");
            default: return FrontRoomsLanguage.T("CONTROLLER");
        }
    }

    /// <summary>What happened to the devices this frame (2026-11-01): a pad came, a pad went while idle, or the device in use
    /// went (the pad in hand; on a handheld also the keyboard it plays with).</summary>
    public enum DeviceChange { None, PadConnected, PadDisconnected, InUseLost }

    /// <summary>This frame's device change: the same value for every reader in a frame (the touch layer, the game).</summary>
    public static DeviceChange ChangeThisFrame { get { TrackDevice(); return change; } }

    /// <summary>The last pad change: PadConnected or PadDisconnected (a pad lost while in use too), with its pad's family and
    /// its moment (unscaled seconds; -infinity until one). The notices read them.</summary>
    public static DeviceChange LastChange { get; private set; }
    public static PadFamily LastChangeFamily { get; private set; }
    public static float LastChangeAt { get; private set; } = float.NegativeInfinity;

    /// <summary>How long a pad's notice shows: the flash line in play, the desktop sheet's hint line.</summary>
    public const float NoticeSeconds = 2.5f;

    /// <summary>A pad's notice is still showing.</summary>
    public static bool NoticeLive => LastChange != DeviceChange.None && Time.unscaledTime - LastChangeAt < NoticeSeconds;

    /// <summary>The notice's words: "XBOX CONTROLLER CONNECTED", "CONTROLLER DISCONNECTED".</summary>
    public static string ChangeLine() => FrontRoomsLanguage.F(LastChange == DeviceChange.PadConnected ? "{0} CONNECTED" : "{0} DISCONNECTED", FamilyName(LastChangeFamily));

    static DeviceChange change;
    static readonly System.Collections.Generic.List<Gamepad> padsSeen = new System.Collections.Generic.List<Gamepad>(4);
    static bool padsKnown, keyboardSeen;
    // The pad last used while a pad was the device in use: the one whose going is a loss.
    static Gamepad padInHand;

    /// <summary>A pad's buttons by place, named as the pad in hand prints them.</summary>
    public enum PadButton { South, East, West, North, Menu, LeftShoulder, RightShoulder, LeftTrigger, RightTrigger }

    /// <summary>A PlayStation pad (its buttons are named by shape).</summary>
    public static bool PlayStationPad => PlayStationPadOverride ?? (Gamepad.current is UnityEngine.InputSystem.DualShock.DualShockGamepad);

    // A DualSense by its layout name (DualSenseGamepadHID on a Mac or a PC, DualSenseGampadiOS on iOS): a string test, so it compiles everywhere.
    static bool DualSensePad => PlayStationPadOverride == null && Gamepad.current != null && Gamepad.current.layout.IndexOf("DualSense", System.StringComparison.Ordinal) >= 0;

    /// <summary>A pad button's name: A B X Y MENU LB RB LT RT on Xbox and MFi pads, CROSS CIRCLE SQUARE TRIANGLE OPTIONS
    /// L1 R1 L2 R2 on PlayStation (words: the HUD's Bayon has no ✕ ○ □ △).</summary>
    public static string PadName(PadButton button)
    {
        var ps = PlayStationPad;
        switch (button)
        {
            case PadButton.South: return ps ? "CROSS" : "A";
            case PadButton.East: return ps ? "CIRCLE" : "B";
            case PadButton.West: return ps ? "SQUARE" : "X";
            case PadButton.North: return ps ? "TRIANGLE" : "Y";
            case PadButton.Menu: return ps ? "OPTIONS" : "MENU";
            case PadButton.LeftShoulder: return ps ? "L1" : "LB";
            case PadButton.RightShoulder: return ps ? "R1" : "RB";
            case PadButton.LeftTrigger: return ps ? "L2" : "LT";
            default: return ps ? "R2" : "RT";
        }
    }

    /// <summary>A pad's menu buttons this frame (the desktop sheet and its map pages, 2026-10-31; the touch sheet reads its own):
    /// fixed whatever the bindings, as menus are. Dir: the d-pad, or the left stick past half a tilt, as arrows (y up) once, again
    /// after 0.35 s, then every 0.12 s (real time); a direction already held when the menus start reading (a walk carried into
    /// the pause) waits for its release. Confirm A / CROSS, Back B / CIRCLE, Reset X / SQUARE, Defaults Y / TRIANGLE,
    /// Tab RB +1 / LB -1. The pad is Gamepad.current, the last one used (with two pads, the last one pressed; the other's first
    /// press makes it current). All zero without a pad. Read once per frame.</summary>
    public struct PadMenu { public Vector2Int Dir; public bool Confirm, Back, Reset, Defaults; public int Tab; }
    static PadMenu padMenu;
    static int padMenuFrame = -1;
    static Vector2Int padHeld;
    static float padRepeatAt;

    /// <summary>The direction held now waits for its release before it moves the menus again (2026-10-31): the desktop sheet
    /// calls it every frame a map page listens, so a d-pad direction just bound, still held, never repeats into the map.</summary>
    public static void HoldPadMenu() { padRepeatAt = float.PositiveInfinity; }

    public static PadMenu ReadPadMenu()
    {
        var now = Time.frameCount;
        if (padMenuFrame == now) return padMenu;
        // Not read last frame (the sheet was shut, or a shortcut took the frame): a tilt carried in from play waits for its
        // release, so opening the sheet mid-walk never steps a setting (2026-10-31).
        var fresh = padMenuFrame != now - 1;
        padMenuFrame = now;
        padMenu = new PadMenu();
        var pad = Gamepad.current;
        if (pad == null) { padHeld = Vector2Int.zero; return padMenu; }
        var held = pad.dpad.ReadValue();
        var stick = pad.leftStick.ReadValue();
        if (stick.sqrMagnitude > .25f) held += stick;
        var dir = Mathf.Abs(held.x) > Mathf.Abs(held.y) + .01f ? new Vector2Int(held.x > 0f ? 1 : -1, 0)
            : Mathf.Abs(held.y) > .01f ? new Vector2Int(0, held.y > 0f ? 1 : -1) : Vector2Int.zero;
        var t = Time.unscaledTime;
        if (fresh) { padHeld = dir; padRepeatAt = float.PositiveInfinity; }
        else if (dir == Vector2Int.zero) padHeld = Vector2Int.zero;
        else if (dir != padHeld) { padHeld = dir; padRepeatAt = t + .35f; padMenu.Dir = dir; }
        else if (t >= padRepeatAt) { padRepeatAt = t + .12f; padMenu.Dir = dir; }
        padMenu.Confirm = pad.buttonSouth.wasPressedThisFrame;
        padMenu.Back = pad.buttonEast.wasPressedThisFrame;
        padMenu.Reset = pad.buttonWest.wasPressedThisFrame;
        padMenu.Defaults = pad.buttonNorth.wasPressedThisFrame;
        padMenu.Tab = (pad.rightShoulder.wasPressedThisFrame ? 1 : 0) - (pad.leftShoulder.wasPressedThisFrame ? 1 : 0);
        return padMenu;
    }

    // ---------------------------------------------------------------- capture (the key map pages, the touch sheet's lists)

    /// <summary>The first key (never Esc: it cancels) or mouse button pressed this frame, as a binding path, or null. The key
    /// map's capture and the touch sheet's KEYBOARD list read it (moved here from FrontRoomsKeyMapView, 2026-11-01).</summary>
    public static string PressedKeyPath(Keyboard kb, Mouse ms)
    {
        if (kb != null)
            foreach (var key in kb.allKeys)
                if (key != null && key.wasPressedThisFrame && key != kb.escapeKey) return "<Keyboard>/" + key.name;
        if (ms == null) return null;
        if (ms.leftButton.wasPressedThisFrame) return "<Mouse>/leftButton";
        if (ms.rightButton.wasPressedThisFrame) return "<Mouse>/rightButton";
        if (ms.middleButton.wasPressedThisFrame) return "<Mouse>/middleButton";
        if (ms.backButton.wasPressedThisFrame) return "<Mouse>/backButton";
        if (ms.forwardButton.wasPressedThisFrame) return "<Mouse>/forwardButton";
        return null;
    }

    /// <summary>The first pad button pressed this frame, as a binding path, or null: the face buttons, the shoulders and
    /// triggers, both stick presses, VIEW and the d-pad. Never MENU (it cancels) and never a stick's tilt.</summary>
    public static string PressedPadPath(Gamepad gp)
    {
        if (gp == null) return null;
        if (gp.buttonSouth.wasPressedThisFrame) return "<Gamepad>/buttonSouth";
        if (gp.buttonEast.wasPressedThisFrame) return "<Gamepad>/buttonEast";
        if (gp.buttonWest.wasPressedThisFrame) return "<Gamepad>/buttonWest";
        if (gp.buttonNorth.wasPressedThisFrame) return "<Gamepad>/buttonNorth";
        if (gp.leftShoulder.wasPressedThisFrame) return "<Gamepad>/leftShoulder";
        if (gp.rightShoulder.wasPressedThisFrame) return "<Gamepad>/rightShoulder";
        if (gp.leftTrigger.wasPressedThisFrame) return "<Gamepad>/leftTrigger";
        if (gp.rightTrigger.wasPressedThisFrame) return "<Gamepad>/rightTrigger";
        if (gp.leftStickButton.wasPressedThisFrame) return "<Gamepad>/leftStickPress";
        if (gp.rightStickButton.wasPressedThisFrame) return "<Gamepad>/rightStickPress";
        if (gp.selectButton.wasPressedThisFrame) return "<Gamepad>/select";
        if (gp.dpad.up.wasPressedThisFrame) return "<Gamepad>/dpad/up";
        if (gp.dpad.down.wasPressedThisFrame) return "<Gamepad>/dpad/down";
        if (gp.dpad.left.wasPressedThisFrame) return "<Gamepad>/dpad/left";
        if (gp.dpad.right.wasPressedThisFrame) return "<Gamepad>/dpad/right";
        return null;
    }

    /// <summary>Any pad button held (the A that opened the capture must be released before it listens); false with no pad.</summary>
    public static bool PadButtonHeld(Gamepad gp)
    {
        if (gp == null) return false;
        foreach (var control in gp.allControls)
            if (control is ButtonControl button && !button.synthetic && button.isPressed) return true;
        return false;
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

        // The mouse is a distance, the right stick a rate (squared, so small tilts aim finely): both are degrees this frame.
        var stick = lookStickAction.ReadValue<Vector2>();
        var look = lookAction.ReadValue<Vector2>() * DesktopLookScale + Vector2.Scale(stick * stick.magnitude, StickLookRate) * Time.unscaledDeltaTime;

        return new FrameSnapshot(
            move,
            look,
            sprintAction.IsPressed(),
            useAction.WasPressedThisFrame(),
            useAction.IsPressed(),
            pauseAction.WasPressedThisFrame(),
            startAction.WasPressedThisFrame(),
            settingsAction.WasPressedThisFrame(),
            // Not while a key map page is open (SuspendRestart, 2026-10-31): the END RUN key pressed there must not end the run.
            restartAction.WasPressedThisFrame() && !SuspendRestart,
            shotBackAction.WasPressedThisFrame(),
            zoom,
            // A close-up's left click is a tap on it: the second tap (Red 2026-10-30), read only in a close-up.
            zoomToggleAction.WasPressedThisFrame() || (!FrontRoomsHandheld.Active && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame),
            textToggleAction.WasPressedThisFrame(),
            slowAction.IsPressed());
    }
}
