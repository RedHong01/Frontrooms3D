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
            bool textToggleDown = false)
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
        useKeyName = null;
        SuspendGameplay = false;
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
    /// TEXT (only the touch layer reads it), so the map does not show it in use with no action of its own.
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
        return paths;
    }

    // ---------------------------------------------------------------- key bindings (desktop)

    /// <summary>
    /// What a player can rebind on the desktop key map (CONTROLS / KEYBOARD &amp; MOUSE, Red 2026-10-29): the W A S D walk,
    /// SPRINT, USE, RESTART and the close-up ZOOM button. Esc (pause and back), the mouse look, the wheel and START (Space
    /// or Enter on the title) stay fixed; the arrows (MOVE) and Right Shift (SPRINT) stay as locked second keys, so a
    /// player can always get out, move and run.
    /// </summary>
    public enum Bind { Forward, Back, Left, Right, Sprint, Use, Restart, Zoom }

    /// <summary>What binding a key to a target would do (<see cref="CheckBind"/>).</summary>
    public enum BindCheck { Ok, Same, Taken, Locked, Reserved }

    public static readonly Bind[] Binds = { Bind.Forward, Bind.Back, Bind.Left, Bind.Right, Bind.Sprint, Bind.Use, Bind.Restart, Bind.Zoom };

    /// <summary>The PlayerPrefs key that holds the player's changed keys ("Use=&lt;Keyboard&gt;/f|…"). Tests point it elsewhere.</summary>
    public static string BindingsPrefKey = "FrontRooms.KeyBindings";

    /// <summary>Raised after any binding changes (an assignment, a swap, a reset).</summary>
    public static event System.Action BindingsChanged;

    /// <summary>
    /// While true (the key map is listening for a key), the game reads no physical input: the press being bound must not
    /// also restart, pause or use.
    /// </summary>
    public static bool SuspendGameplay { get; set; }

    static string useKeyName;

    static InputAction ActionOf(Bind b)
    {
        EnsureInitialized();
        switch (b)
        {
            case Bind.Sprint: return sprintAction;
            case Bind.Use: return useAction;
            case Bind.Restart: return restartAction;
            case Bind.Zoom: return zoomToggleAction;
            default: return moveAction;
        }
    }

    // The W A S D composite's parts are Move's bindings 1–4 (0 is the composite; the arrows' composite is 5–9).
    static int IndexOf(Bind b) => b == Bind.Forward ? 1 : b == Bind.Back ? 2 : b == Bind.Left ? 3 : b == Bind.Right ? 4 : 0;

    /// <summary>A control path in one spelling, for comparisons ("&lt;keyboard&gt;/leftshift").</summary>
    public static string Normalise(string path) => string.IsNullOrEmpty(path) ? "" : path.Trim().ToLowerInvariant();

    static bool Same(string a, string b) => Normalise(a) == Normalise(b);

    public static string PathOf(Bind b) => ActionOf(b).bindings[IndexOf(b)].effectivePath;
    public static string DefaultPathOf(Bind b) => ActionOf(b).bindings[IndexOf(b)].path;
    public static bool IsDefault(Bind b) => Same(PathOf(b), DefaultPathOf(b));

    public static bool AllDefault
    {
        get
        {
            foreach (var b in Binds) if (!IsDefault(b)) return false;
            return true;
        }
    }

    /// <summary>The key that also steps back out of a shot or a close-up: it follows Back (S by default).</summary>
    public static string StepBackPath
    {
        get { EnsureInitialized(); return shotBackAction.bindings[0].effectivePath; }
    }

    /// <summary>Who keeps a fixed or locked key: PAUSE (Esc), MOVE (the arrows), SPRINT (Right Shift), ZOOM STEP (the
    /// wheel), LOOK (the mouse's movement); null for any other key.</summary>
    public static string LockedOwner(string path)
    {
        switch (Normalise(path))
        {
            case "<keyboard>/escape": return "PAUSE";
            case "<keyboard>/uparrow": case "<keyboard>/downarrow": case "<keyboard>/leftarrow": case "<keyboard>/rightarrow": return "MOVE";
            case "<keyboard>/rightshift": return "SPRINT";
            case "<mouse>/scroll": case "<mouse>/scroll/y": case "<mouse>/scroll/x": return "ZOOM STEP";
            case "<mouse>/delta": case "<mouse>/position": return "LOOK";
        }
        return null;
    }

    /// <summary>
    /// What binding <paramref name="path"/> to <paramref name="b"/> would do: Same (it is b's already), Reserved (Esc,
    /// the wheel, the mouse's movement: never), Locked (another action's locked second key, named in
    /// <paramref name="lockedName"/>), Taken (another target has it, <paramref name="owner"/>: the key map asks before it
    /// swaps), else Ok.
    /// </summary>
    public static BindCheck CheckBind(Bind b, string path, out Bind owner, out string lockedName)
    {
        owner = b;
        lockedName = LockedOwner(path);
        if (Normalise(path).Length == 0) return BindCheck.Reserved;
        if (Same(PathOf(b), path)) return BindCheck.Same;
        if (lockedName == "PAUSE" || lockedName == "ZOOM STEP" || lockedName == "LOOK") return BindCheck.Reserved;
        if (lockedName != null && !(lockedName == "SPRINT" && b == Bind.Sprint)) return BindCheck.Locked;
        foreach (var other in Binds)
        {
            if (other == b || !Same(PathOf(other), path)) continue;
            owner = other;
            return BindCheck.Taken;
        }
        return BindCheck.Ok;
    }

    /// <summary>Binds <paramref name="b"/> to <paramref name="path"/> as it is (the caller checked it), and saves.</summary>
    public static void Assign(Bind b, string path)
    {
        Apply(b, path);
        SaveBindings();
    }

    /// <summary><paramref name="b"/> takes <paramref name="path"/> and the target that had it takes b's old key. Saved.</summary>
    public static void SwapInto(Bind b, string path)
    {
        var old = PathOf(b);
        foreach (var other in Binds)
            if (other != b && Same(PathOf(other), path)) Apply(other, old);
        Apply(b, path);
        SaveBindings();
    }

    /// <summary>One target back to its default key; a target that has that key now takes this one's key. Saved.</summary>
    public static void ResetBind(Bind b)
    {
        if (!IsDefault(b)) SwapInto(b, DefaultPathOf(b));
    }

    /// <summary>Every key back to the defaults. Saved.</summary>
    public static void ResetAllBinds()
    {
        EnsureInitialized();
        gameplay.RemoveAllBindingOverrides();
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
        useKeyName = null;
        try
        {
            var saved = new System.Text.StringBuilder();
            foreach (var b in Binds)
                if (!IsDefault(b)) saved.Append(saved.Length > 0 ? "|" : "").Append(b).Append('=').Append(PathOf(b));
            if (saved.Length == 0) PlayerPrefs.DeleteKey(BindingsPrefKey);
            else PlayerPrefs.SetString(BindingsPrefKey, saved.ToString());
            PlayerPrefs.Save();
        }
        catch (System.Exception e) { Debug.LogWarning("[FrontRoomsInput] key bindings not saved: " + e.Message); }
        BeginFrame();
        BindingsChanged?.Invoke();
    }

    static void LoadBindings()
    {
        useKeyName = null;
        string saved = null;
        try { saved = PlayerPrefs.GetString(BindingsPrefKey, ""); } catch (System.Exception) { }
        if (string.IsNullOrEmpty(saved)) return;
        foreach (var entry in saved.Split('|'))
        {
            var cut = entry.IndexOf('=');
            if (cut <= 0 || !System.Enum.TryParse<Bind>(entry.Substring(0, cut), out var b)) continue;
            var path = entry.Substring(cut + 1);
            if (LockedOwner(path) == null || (b == Bind.Sprint && LockedOwner(path) == "SPRINT")) Apply(b, path);
            else Debug.LogWarning("[FrontRoomsInput] saved key ignored: " + entry);
        }
    }

    /// <summary>
    /// A control path as the prompts and the settings name it: a letter as the player's keyboard prints it ("Z" for the
    /// W key on an AZERTY board: the bindings are physical keys), words where Bayon has no glyph ("UP", "COMMA"), and
    /// "LEFT CLICK" for mouse buttons.
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
        if (!p.StartsWith("<keyboard>/")) return p.ToUpperInvariant();
        var k = p.Substring(11);
        switch (k)
        {
            case "leftshift": case "rightshift": return "SHIFT";
            case "leftctrl": case "rightctrl": return "CTRL";
            case "leftalt": case "rightalt": return "ALT";
            case "leftmeta": case "rightmeta": return Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXEditor ? "CMD" : "WIN";
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

    /// <summary>
    /// A controller (Red, 2026-10-29: phones and iPads play with a pad or a keyboard too; Mac and Windows get it as well).
    /// Each pad binding comes after the action's own, so the keyboard bindings keep their indices. The left stick's tilt
    /// is the gait on handhelds (under 0.7: the slow, unheard walk); LB or a held L3 runs; A uses (held: breaks glass);
    /// MENU pauses; B steps back out of a close-up; Y is its TEXT; RT toggles its zoom and the d-pad steps it.
    /// </summary>
    static void AddGamepadBindings()
    {
        moveAction.AddBinding("<Gamepad>/leftStick");
        lookStickAction = gameplay.AddAction("Look Stick", InputActionType.Value);
        lookStickAction.expectedControlType = "Vector2";
        lookStickAction.AddBinding("<Gamepad>/rightStick");
        sprintAction.AddBinding("<Gamepad>/leftShoulder");
        sprintAction.AddBinding("<Gamepad>/leftStickPress");
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

    /// <summary>A pad's buttons by place, named as the pad in hand prints them.</summary>
    public enum PadButton { South, East, West, North, Menu, LeftShoulder, RightShoulder, LeftTrigger, RightTrigger }

    /// <summary>A PlayStation pad (its buttons are named by shape).</summary>
    public static bool PlayStationPad => Gamepad.current is UnityEngine.InputSystem.DualShock.DualShockGamepad;

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
            restartAction.WasPressedThisFrame(),
            shotBackAction.WasPressedThisFrame(),
            zoom,
            zoomToggleAction.WasPressedThisFrame(),
            textToggleAction.WasPressedThisFrame());
    }
}
