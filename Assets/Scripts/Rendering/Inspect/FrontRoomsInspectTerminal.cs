using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

/// <summary>
/// The EXAMINE terminal extension (game visual, 2026-10-30; research/computer_ui/41_c19_period_interaction.md sections 4-5,
/// 20_design section 9.4, 30_crosshair_grammar Q3 / A38). An Office terminal (FrontRoomsTerminalScreen) carries a READ target
/// built like the station TV's, so the game's own aim shows the E.1 eye and "E . READ SCREEN" on it and E opens main's EXAMINE
/// close-up. This class joins that close-up to the terminal's panel machine:
///   - Opened / Closed (FrontRoomsInspect) begin and end the terminal's reading; an aborted close-up ends it too (Watch);
///   - <see cref="Tick"/> (the game's Inspect branch, CONTRACT map L3') reads the verb set from the player's devices as held
///     levels (41 section 5.1: arrows and the bound MOVE keys, Enter / Space, Esc / Backspace, F3 and the USE key, F1,
///     PgUp / PgDn, Home / End, the wheel one step per gesture; pad d-pad, left stick, A B X, LB RB, MENU) and says when to
///     leave (EXIT, CANCEL on MAIN, MENU, a perceivable threat while the world runs);
///   - row 25 and HELP name the player's real control (41 section 5.2, the sibling of FrontRoomsKeyHud.WithUseControl): a PC
///     keyboard, a Mac keyboard, WebGL and a keyboard on a handheld, a pad (Xbox or PlayStation words), touch soft keys;
///   - touch is a hook for interaction design (TOUCH_NOTES): <see cref="TapViewport"/>, <see cref="Swipe"/>, <see cref="Back"/>;
///   - the phone close-up (Red 10-30 11:00 / 11:1x, mock EC03 v3): the picture at 95 % of the full height, centred, the dark
///     tube surround filling the sides (two plain bands under every HUD element), no TEXT chip, BACK the only control.
/// INERT until a desk is a terminal (FrontRoomsTerminalScreen.Attach; level design picks the desks): with no terminal the
/// hooks never find one and nothing else changes. No sound here (rule 24; sound design listens to the terminal's events).
/// Allocation-free per frame while reading (strings only when the screen changes).
/// </summary>
public static class FrontRoomsInspectTerminal
{
    /// <summary>A stick or the touch layer's MOVE counts as a held direction past this.</summary>
    public const float StickThreshold = .5f;
    /// <summary>One wheel gesture is one MOVE step; the wheel re-arms after this long without scrolling (FrontRoomsInput's rule).</summary>
    public const float WheelRearmSeconds = .2f;
    /// <summary>
    /// The dark glass beside the lit picture that the surround leaves showing (mm): the tube's real border is about 5.4 mm at
    /// the sides (30 px of a 1146 px picture on the iPhone 16 Pro, TB2 phone run 2), so the bands start 5 mm out and never
    /// show a sliver of the beige bezel (the mock painted 12 mm and any lit pixel in it).
    /// </summary>
    public const float SurroundBorderMm = 5f;
    /// <summary>The phone surround's colour (sRGB): the dark glass as it renders beside the lit picture (EC03 v3 measured 7 15 10).</summary>
    public static readonly Color32 SurroundColour = new Color32(7, 15, 10, 255);
    /// <summary>Touch: a tap this far below the picture (share of its height) still lands on row 24-25's soft keys (41 section 3.7).</summary>
    public const float SoftKeyMarginBelow = .03f;

    /// <summary>Whose words row 25 prints (41 section 5.2).</summary>
    public enum Names { Pc, Mac, Web, Pad, PadPlayStation, Touch }

    static FrontRoomsTerminalScreen reading;
    static FrontRoomsMapHunter relay;
    static bool refused;
    static bool deskPad;
    static bool wheelArmed = true;
    static float lastWheel = -1f;
    static string useKeyPath;
    static ButtonControl useKeyControl;
    static int transcriptVersion = -1;
    static float bandAlpha;
    static Canvas bandCanvas;
    static RectTransform bandLeft, bandRight;
    static Image bandLeftImage, bandRightImage;
#if UNITY_EDITOR
    static bool injected;
    static TerminalInput injectedHeld;
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Hook()
    {
        reading = null;
        relay = null;
        refused = false;
        deskPad = false;
        wheelArmed = true;
        lastWheel = -1f;
        useKeyPath = null;
        useKeyControl = null;
        transcriptVersion = -1;
        bandAlpha = 0f;
        bandCanvas = null;
        bandLeft = bandRight = null;
        bandLeftImage = bandRightImage = null;
#if UNITY_EDITOR
        injected = false;
#endif
        FrontRoomsInspect.Opened -= OnOpened;
        FrontRoomsInspect.Opened += OnOpened;
        FrontRoomsInspect.Closed -= OnClosed;
        FrontRoomsInspect.Closed += OnClosed;
        FrontRooms3DGame.MapRunStarted -= OnMapRunStarted;
        FrontRooms3DGame.MapRunStarted += OnMapRunStarted;
        FrontRooms3DGame.MapRunEnded -= OnMapRunEnded;
        FrontRooms3DGame.MapRunEnded += OnMapRunEnded;
        FrontRoomsInput.BindingsChanged -= OnBindingsChanged;
        FrontRoomsInput.BindingsChanged += OnBindingsChanged;
    }

    // ---------------------------------------------------------------- read-outs

    /// <summary>The close-up is open on a terminal (the game's Inspect branch reads the terminal's keys, CONTRACT L3').</summary>
    public static bool Owns => reading != null && FrontRoomsInspect.Active && !FrontRoomsInspect.Closing;

    /// <summary>The terminal being read (null when none).</summary>
    public static FrontRoomsTerminalScreen Current => reading;

    /// <summary>The terminal asks to leave: EXIT, CANCEL on MAIN, pad MENU, or a refused open (a perceivable threat).</summary>
    public static bool WantsExit => reading != null && (reading.WantsExit || refused);

    /// <summary>The words row 25 prints now.</summary>
    public static Names CurrentNames => NamesNow();

    /// <summary>
    /// RELAY section 8 / 20_design 6.1 (CONTRACT L4): the Relay is a threat the player perceives on this device's render camera.
    /// The READ prompt and the eye already hide on it (the game's CanShowInspect); a terminal also refuses to open on it and,
    /// if the world ever runs during a close-up (D2 live mode), leaves on it. The close-up pauses the world in v1, as every
    /// other EXAMINE close-up, so nothing new can appear while reading.
    /// </summary>
    public static bool ThreatPerceivable
    {
        get
        {
            if (relay == null) return false;
            var cam = FrontRoomsInspect.RigCamera != null ? FrontRoomsInspect.RigCamera : Camera.main;
            return cam != null && relay.ThreatPerceivable(cam);
        }
    }

    /// <summary>A handheld played by touch alone (no pad or keyboard in use): soft keys, taps and swipes; BACK leaves.</summary>
    public static bool TouchOnly => FrontRoomsHandheld.Active && FrontRoomsTouchControls.CurrentMode == FrontRoomsTouchControls.InputMode.Touch;

    /// <summary>Touch: the lit picture's viewport rect while a terminal is read (taps and swipes land on it); empty otherwise.</summary>
    public static Rect PictureViewportRect => Owns ? FrontRoomsInspect.CurrentFaceViewportRect : default;

    // ---------------------------------------------------------------- hooks

    static void OnMapRunStarted(FrontRoomsMapWorld world, FrontRoomsMapHunter hunter)
    {
        relay = hunter;
        // a new run: every unread mark and last-item memory resets (41 section 4.4)
        FrontRoomsTerminal.ResetRun();
    }

    static void OnMapRunEnded() => relay = null;

    static void OnBindingsChanged()
    {
        useKeyPath = null;
        useKeyControl = null;
    }

    static void OnOpened(FrontRoomsInspectable target)
    {
        var t = FrontRoomsTerminalScreen.Of(target);
        if (t == null) return;
        reading = t;
        refused = ThreatPerceivable;
        transcriptVersion = -1;
        ApplyNames(t, true);
        t.BeginReading();
        PushTranscript();
    }

    static void OnClosed(FrontRoomsInspectable target)
    {
        if (reading == null) return;
        End();
    }

    static void End()
    {
        var t = reading;
        reading = null;
        refused = false;
        FrontRoomsInspectCatalog.SetTranscript(FrontRoomsInspectCatalog.TerminalTranscript, null);
        transcriptVersion = -1;
        if (t != null) t.EndReading();
    }

    // ---------------------------------------------------------------- the close-up's frame (the game's Inspect branch)

    /// <summary>
    /// Every close-up frame on a terminal, from the game's Inspect branch (CONTRACT L3'), with the game's input snapshot:
    /// the keys as held levels, the wheel, the names, the transcript. True = leave the close-up now (the game ends it once
    /// FrontRoomsInspect.Age has passed CancelAfter, as every exit). On a terminal the snapshot's E, S and Esc are not
    /// "leave" (E is EXIT here, S is down, Esc is CANCEL: this class reads them itself); only on a touch-only handheld do the
    /// snapshot's USE (the BACK button) and shot-back (the hidden stick's pull-back) still leave. False when the close-up is
    /// not on a terminal.
    /// </summary>
    public static bool Tick(float unscaledDt, in FrontRoomsInput.FrameSnapshot input)
    {
        if (!Owns) return false;
        if (TouchOnly && (input.UseDown || input.ShotBackDown)) return true;
        var held = ReadHeld(out var wheel);
        TrackDeskSource();
        ApplyNames(reading, false);
        reading.Apply(held, unscaledDt);
        if (wheel != 0) reading.Press(wheel < 0 ? TerminalVerb.Up : TerminalVerb.Down);
        PushTranscript();
        if (WantsExit) return true;
        // live mode only (D2): a paused close-up keeps the world still, as every EXAMINE close-up
        return Time.timeScale > 0f && ThreatPerceivable;
    }

    /// <summary>Once per frame from FrontRoomsTerminalScreen (after the cameras moved): a close-up that ended without Closed
    /// (an abort: restart, caught, the target gone) ends the reading; the phone's surround follows the picture.</summary>
    public static void Watch(float unscaledDt)
    {
        if (reading != null && (!FrontRoomsInspect.Active || FrontRoomsTerminalScreen.Of(FrontRoomsInspect.Current) != reading)) End();
        TickSurround(unscaledDt);
    }

    // ---------------------------------------------------------------- touch (interaction design's routing, TOUCH_NOTES)

    /// <summary>
    /// Touch: a tap at a viewport point (0..1, y up). On the picture it becomes a cell of the 80 x 25 grid (the two-tap rule,
    /// soft keys in row 25 with row 24 and the dark margin below in their hit area). True when the tap was the terminal's.
    /// </summary>
    public static bool TapViewport(Vector2 viewport)
    {
        if (!Owns) return false;
        var r = FrontRoomsInspect.CurrentFaceViewportRect;
        if (r.width <= 0f || r.height <= 0f) return false;
        var u = (viewport.x - r.xMin) / r.width;
        var v = (viewport.y - r.yMin) / r.height;
        if (u < 0f || u >= 1f || v >= 1f || v < -SoftKeyMarginBelow) return false;
        var col = Mathf.Clamp(Mathf.FloorToInt(u * FrontRoomsTerminalGrid.Cols), 0, FrontRoomsTerminalGrid.Cols - 1);
        var row = v < 0f ? FrontRoomsTerminal.FooterRow : Mathf.Clamp(Mathf.FloorToInt((1f - v) * FrontRoomsTerminalGrid.Rows), 0, FrontRoomsTerminalGrid.Rows - 1);
        reading.Tap(col, row);
        return true;
    }

    /// <summary>Touch: a swipe on the picture (41 section 5.4): +1 = next (up or left), -1 = previous (down or right).</summary>
    public static void Swipe(int direction)
    {
        if (!Owns || direction == 0) return;
        reading.Press(direction > 0 ? TerminalVerb.PageDown : TerminalVerb.PageUp);
    }

    /// <summary>Touch: Android Back on a terminal is CANCEL (it leaves on MAIN). True when the terminal took it.</summary>
    public static bool Back()
    {
        if (!Owns) return false;
        reading.Press(TerminalVerb.Cancel);
        return true;
    }

#if UNITY_EDITOR
    /// <summary>Harness only: replace the devices with these held levels until <see cref="ClearInjected"/>.</summary>
    public static void Inject(TerminalInput held) { injected = true; injectedHeld = held; }
    public static void ClearInjected() { injected = false; injectedHeld = default; }
    /// <summary>Harness only: force the words of row 25 (null = the devices decide).</summary>
    public static Names? ForcedNames;
#endif

    // ---------------------------------------------------------------- devices -> held levels (41 section 5.1)

    static TerminalInput ReadHeld(out int wheel)
    {
        wheel = 0;
        var k = new TerminalInput();
#if UNITY_EDITOR
        if (injected) return injectedHeld;
#endif
        if (FrontRoomsInput.SuspendGameplay) return k;
        // MOVE and SIDE: the arrows, the player's bound W A S D (S is down on a terminal, O-3) and the left stick; on a
        // handheld the touch layer's snapshot while a pad or a keyboard is in use (it merges them). On touch alone the
        // picture is the input surface: a drag is a swipe or the hidden stick's pull-back (a leave), never MOVE.
        var move = TouchOnly ? Vector2.zero : FrontRoomsInput.ReadFrame().Move;
        k.Move = move.y > StickThreshold ? -1 : move.y < -StickThreshold ? 1 : 0;
        k.Side = move.x > StickThreshold ? 1 : move.x < -StickThreshold ? -1 : 0;
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.upArrowKey.isPressed) k.Move = -1; else if (kb.downArrowKey.isPressed) k.Move = 1;
            if (kb.leftArrowKey.isPressed) k.Side = -1; else if (kb.rightArrowKey.isPressed) k.Side = 1;
            k.Select = kb.enterKey.isPressed || kb.numpadEnterKey.isPressed || kb.spaceKey.isPressed;
            k.Cancel = kb.escapeKey.isPressed || kb.backspaceKey.isPressed;
            k.Exit = kb.f3Key.isPressed;
            k.Help = kb.f1Key.isPressed;
            k.Page = kb.pageUpKey.isPressed ? -1 : kb.pageDownKey.isPressed ? 1 : 0;
            k.Jump = kb.homeKey.isPressed ? -1 : kb.endKey.isPressed ? 1 : 0;
        }
        // EXIT: the USE key, wherever the player bound it (E by default), as in every close-up
        var use = UseKeyControl();
        if (use != null && use.isPressed) k.Exit = true;
        var pad = Gamepad.current;
        if (pad != null)
        {
            if (pad.dpad.up.isPressed) k.Move = -1; else if (pad.dpad.down.isPressed) k.Move = 1;
            if (pad.dpad.left.isPressed) k.Side = -1; else if (pad.dpad.right.isPressed) k.Side = 1;
            if (pad.buttonSouth.isPressed) k.Select = true;      // A / CROSS selects on a terminal (O-3)
            if (pad.buttonEast.isPressed) k.Cancel = true;       // B / CIRCLE
            if (pad.buttonWest.isPressed || pad.startButton.isPressed) k.Exit = true;   // X / SQUARE; MENU leaves as in every close-up
            if (pad.leftShoulder.isPressed) k.Page = -1; else if (pad.rightShoulder.isPressed) k.Page = 1;
        }
        // the wheel: one MOVE step per gesture (zoom is off on screens, 20_design 9.4)
        var mouse = Mouse.current;
        if (mouse != null)
        {
            var scroll = mouse.scroll.y.ReadValue();
            var now = Time.realtimeSinceStartup;
            if (Mathf.Abs(scroll) > .01f)
            {
                if (wheelArmed) { wheel = scroll > 0f ? -1 : 1; wheelArmed = false; }
                lastWheel = now;
            }
            else if (!wheelArmed && now - lastWheel >= WheelRearmSeconds) wheelArmed = true;
        }
        return k;
    }

    static ButtonControl UseKeyControl()
    {
        var path = FrontRoomsInput.PathOf(FrontRoomsInput.Bind.Use);
        if (path != useKeyPath)
        {
            useKeyPath = path;
            useKeyControl = string.IsNullOrEmpty(path) ? null : InputSystem.FindControl(path) as ButtonControl;
        }
        return useKeyControl;
    }

    // ---------------------------------------------------------------- names (41 section 5.2)

    static void TrackDeskSource()
    {
        if (FrontRoomsHandheld.Active) return;
        var used = FrontRoomsInput.ExternalActivity();
        if (used == FrontRoomsInput.Device.Gamepad) deskPad = true;
        else if (used == FrontRoomsInput.Device.Keyboard) deskPad = false;
    }

    static Names NamesNow()
    {
#if UNITY_EDITOR
        if (ForcedNames.HasValue) return ForcedNames.Value;
#endif
        if (FrontRoomsHandheld.Active)
        {
            switch (FrontRoomsTouchControls.CurrentMode)
            {
                case FrontRoomsTouchControls.InputMode.Gamepad: return FrontRoomsInput.PlayStationPad ? Names.PadPlayStation : Names.Pad;
                case FrontRoomsTouchControls.InputMode.Keyboard: return Names.Web;
                default: return Names.Touch;
            }
        }
        if (deskPad && Gamepad.current != null) return FrontRoomsInput.PlayStationPad ? Names.PadPlayStation : Names.Pad;
#if UNITY_WEBGL && !UNITY_EDITOR
        return Names.Web;
#else
        return FrontRoomsInput.AppleKeys ? Names.Mac : Names.Pc;
#endif
    }

    /// <summary>Row 25 and HELP name the player's controls on this terminal (re-laid only when the words change).</summary>
    public static void ApplyNames(FrontRoomsTerminalScreen t, bool force)
    {
        if (t == null) return;
        var n = NamesNow();
        if (!force && t.NamesApplied == (int)n) return;
        t.NamesApplied = (int)n;
        t.SetControlNames(NamerFor(n), n == Names.Touch);
    }

    static readonly TerminalNamer PcNamer = FrontRoomsTerminal.PeriodKeys;
    static readonly TerminalNamer MacNamer = MacNames;
    static readonly TerminalNamer WebNamer = WebNames;
    static readonly TerminalNamer PadNamer = PadNames;
    static readonly TerminalNamer PsNamer = PlayStationNames;
    static readonly TerminalNamer TouchNamer = TouchNames;

    public static TerminalNamer NamerFor(Names n)
    {
        switch (n)
        {
            case Names.Mac: return MacNamer;
            case Names.Web: return WebNamer;
            case Names.Pad: return PadNamer;
            case Names.PadPlayStation: return PsNamer;
            case Names.Touch: return TouchNamer;
            default: return PcNamer;
        }
    }

    const string Arrows = "↑ ↓", SideArrows = "← →", PageArrows = "←/→";

    /// <summary>A Mac keyboard: Return; EXIT is the USE key (F3 needs fn); PAGE prints the arrows (no PgUp on a MacBook); no F1.</summary>
    static string MacNames(TerminalAction a)
    {
        switch (a)
        {
            case TerminalAction.Move: return Arrows;
            case TerminalAction.Side: return SideArrows;
            case TerminalAction.Select: return "Return";
            case TerminalAction.Page: return PageArrows;
            case TerminalAction.Cancel: return "Esc";
            case TerminalAction.Exit: return FrontRoomsInput.UseKey;
            default: return null;
        }
    }

    /// <summary>WebGL, and a keyboard on a phone or iPad: Backspace cancels (Esc is the browser's), EXIT is the USE key, no F1.</summary>
    static string WebNames(TerminalAction a)
    {
        switch (a)
        {
            case TerminalAction.Move: return Arrows;
            case TerminalAction.Side: return SideArrows;
            case TerminalAction.Select: return "Enter";
            case TerminalAction.Page: return PageArrows;
            case TerminalAction.Cancel: return "Bksp";
            case TerminalAction.Exit: return FrontRoomsInput.UseKey;
            default: return null;
        }
    }

    static string PadNames(TerminalAction a)
    {
        switch (a)
        {
            case TerminalAction.Move: return "D-pad ↑↓";
            case TerminalAction.Side: return "D-pad ←→";
            case TerminalAction.Select: return "A";
            case TerminalAction.Page: return "LB/RB";
            case TerminalAction.Cancel: return "B";
            case TerminalAction.Exit: return "X";
            default: return null;
        }
    }

    static string PlayStationNames(TerminalAction a)
    {
        switch (a)
        {
            case TerminalAction.Move: return "D-pad ↑↓";
            case TerminalAction.Side: return "D-pad ←→";
            case TerminalAction.Select: return "CROSS";
            case TerminalAction.Page: return "L1/R1";
            case TerminalAction.Cancel: return "CIRCLE";
            case TerminalAction.Exit: return "SQUARE";
            default: return null;
        }
    }

    /// <summary>Touch: HELP names the gestures; row 25 is CUA's short form (soft keys, the machine's words).</summary>
    static string TouchNames(TerminalAction a)
    {
        switch (a)
        {
            case TerminalAction.Move: return "Tap an item";
            case TerminalAction.Select: return "Tap it again";
            case TerminalAction.Page: return "Swipe";
            case TerminalAction.Cancel: return "Cancel";
            case TerminalAction.Exit: return "BACK";
            case TerminalAction.Help: return "Help";
            default: return null;
        }
    }

    // ---------------------------------------------------------------- the transcript (desktop and iPad TEXT, C24)

    static void PushTranscript()
    {
        if (reading == null || reading.Grid == null) return;
        if (reading.ReadClassNow == FrontRoomsTerminalScreen.ReadClass.Phone) return;   // no TEXT on a phone terminal (Red 11:00)
        var v = reading.Grid.Version;
        if (v == transcriptVersion) return;
        transcriptVersion = v;
        var text = reading.PanelTranscript;
        FrontRoomsInspectCatalog.SetTranscript(FrontRoomsInspectCatalog.TerminalTranscript, string.IsNullOrEmpty(text) ? null : text.Split('\n'));
    }

    // ---------------------------------------------------------------- the phone's dark surround (A39)

    static void TickSurround(float dt)
    {
        var want = reading != null && FrontRoomsInspect.Active && !FrontRoomsInspect.Closing
            && reading.ReadClassNow == FrontRoomsTerminalScreen.ReadClass.Phone;
        // in with the camera's travel (0.15 .. 0.45 s of the close-up), out with the HUD's 0.15 s
        bandAlpha = want ? Mathf.Clamp01((FrontRoomsInspect.Age - .15f) / .30f) : Mathf.MoveTowards(bandAlpha, 0f, dt / .15f);
        if (bandAlpha <= 0f)
        {
            if (bandCanvas != null && bandCanvas.enabled) bandCanvas.enabled = false;
            return;
        }
        var r = FrontRoomsInspect.CurrentFaceViewportRect;
        if (r.width <= 0f)
        {
            if (bandCanvas != null && bandCanvas.enabled) bandCanvas.enabled = false;
            return;
        }
        EnsureSurround();
        var border = r.width * SurroundBorderMm / (FrontRoomsTerminalScreen.ReadSize.x * 1000f);
        var left = Mathf.Clamp01(r.xMin - border);
        var right = Mathf.Clamp01(r.xMax + border);
        bandLeft.anchorMin = Vector2.zero;
        bandLeft.anchorMax = new Vector2(left, 1f);
        bandRight.anchorMin = new Vector2(right, 0f);
        bandRight.anchorMax = Vector2.one;
        var c = (Color)SurroundColour;
        c.a = bandAlpha;
        bandLeftImage.color = c;
        bandRightImage.color = c;
        if (!bandCanvas.enabled) bandCanvas.enabled = true;
    }

    /// <summary>The surround's bands: a screen-space canvas under every HUD canvas (the touch HUD's BACK stays on top).</summary>
    public const int SurroundSortingOrder = -50;

    static void EnsureSurround()
    {
        if (bandCanvas != null) return;
        // no hide flags: DontDestroyOnLoad keeps it out of every saved scene, and capture tools (FindObjectsByType) see it
        var go = new GameObject("FrontRooms terminal surround (phone)");
        Object.DontDestroyOnLoad(go);
        bandCanvas = go.AddComponent<Canvas>();
        bandCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        bandCanvas.sortingOrder = SurroundSortingOrder;
        bandLeft = Band("left", go.transform, out bandLeftImage);
        bandRight = Band("right", go.transform, out bandRightImage);
    }

    static RectTransform Band(string name, Transform parent, out Image image)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        image = go.AddComponent<Image>();
        image.raycastTarget = false;
        var rt = (RectTransform)go.transform;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }
}
