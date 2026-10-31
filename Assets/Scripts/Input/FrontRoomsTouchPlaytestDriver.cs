#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using InputTouchPhase = UnityEngine.InputSystem.TouchPhase;

/// <summary>
/// Editor-only touch playtest (FrontRoomsTouchPlaytest, Documentation/TOUCH_CONTROLS.md §11).
/// Forces a handheld profile, plays the game with injected multi-finger touches
/// on a virtual touchscreen, checks what each gesture did to the game, and
/// captures composited frames (room + HUD + touch layer) at fixed 60 fps steps
/// so every motion frame lands on its exact time.
/// </summary>
public sealed class FrontRoomsTouchPlaytestDriver : MonoBehaviour
{
    public const string ActiveKey = "FrontRooms.TouchPlaytest.Active";
    public const string OutKey = "FrontRooms.TouchPlaytest.Out";
    public const string ProfileKey = "FrontRooms.TouchPlaytest.Profile";
    public const string DoneKey = "FrontRooms.TouchPlaytest.Done";
    public const string FailedKey = "FrontRooms.TouchPlaytest.Failed";

    const float Step = 1f / 60f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        if (!UnityEditor.SessionState.GetBool(ActiveKey, false) || instance != null) return;
        FrontRoomsHandheld.Force(ProfileFor(UnityEditor.SessionState.GetString(ProfileKey, "iphone")));
        var go = new GameObject("FrontRooms touch playtest");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<FrontRoomsTouchPlaytestDriver>();
    }

    public static FrontRoomsHandheld.Profile ProfileFor(string name)
    {
        switch (name)
        {
            case "duo-outer": return FrontRoomsHandheld.IPhoneDuoOuter;
            case "duo-outer-flipped": return FrontRoomsHandheld.IPhoneDuoOuterFlipped;
            case "duo-outer-settled": return FrontRoomsHandheld.IPhoneDuoOuterFlippedSettled;
            case "duo-inner": return FrontRoomsHandheld.IPhoneDuoInner;
            case "duo-inner-portrait": return FrontRoomsHandheld.IPhoneDuoInnerPortrait;
            case "ipad": return FrontRoomsHandheld.IPad11;
            case "android": return FrontRoomsHandheld.Android20x9;
            default: return FrontRoomsHandheld.IPhonePro;
        }
    }

    static FrontRoomsTouchPlaytestDriver instance;

    Touchscreen screen;
    InputSettings savedInputSettings, harnessInputSettings;
    FrontRooms3DGame game;
    FrontRoomsTouchControls controls;
    Camera uiCam;
    RenderTexture uiTarget;
    string outDir;
    int shots;
    readonly List<string> report = new List<string>();
    readonly List<string> frames = new List<string>();
    readonly List<string> haptics = new List<string>();
    readonly HashSet<Canvas> converted = new HashSet<Canvas>();
    int failures;
    float t0;
    bool aborted;
    // X2's teaching flags (PlayerPrefs): the tester's own, given back at the end however the run ends.
    const string TaughtSlowKey = "FrontRooms.Touch.Taught.Slow";
    const string TaughtRunKey = "FrontRooms.Touch.Taught.Run";
    int savedTaughtSlow = int.MinValue, savedTaughtRun = int.MinValue;
    bool taughtRestored;

    static readonly BindingFlags Any = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;

    void Awake()
    {
        // Game time steps 1/60 per frame (captureDeltaTime) but unscaled time is wall time, which the
        // slow captures stretch: the touch layer's clock steps with the frames instead.
        var frame0 = Time.frameCount;
        var clock0 = Time.unscaledTime;
        FrontRoomsTouchMotion.EditorStep = Step;
        FrontRoomsTouchMotion.EditorClock = () => clock0 + (Time.frameCount - frame0) * Step;
    }

    IEnumerator Start()
    {
        outDir = UnityEditor.SessionState.GetString(OutKey, "Verification/touch-playtest");
        Directory.CreateDirectory(outDir);
        Time.captureDeltaTime = Step;
        // The Editor routes pointer input to Play Mode only while a Game view has focus, and batch mode has
        // none: run on a temporary copy of the input settings that sends every device to the game.
        savedInputSettings = InputSystem.settings;
        harnessInputSettings = Instantiate(savedInputSettings);
        harnessInputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        harnessInputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings = harnessInputSettings;
        screen = InputSystem.AddDevice<Touchscreen>("FrontRooms Playtest Touchscreen");
        FrontRoomsMobileHaptics.Played += OnHaptic;
        t0 = FrontRoomsTouchMotion.Now;
        savedTaughtSlow = PlayerPrefs.GetInt(TaughtSlowKey, -1);
        savedTaughtRun = PlayerPrefs.GetInt(TaughtRunKey, -1);
        PlayerPrefs.DeleteKey(TaughtSlowKey);
        PlayerPrefs.DeleteKey(TaughtRunKey);
        Note("profile", FrontRoomsHandheld.ForcedProfile.Name + " " + FrontRoomsHandheld.ScreenPixels + " @" + F(FrontRoomsHandheld.PointScale));

        // Nested steps (Tap, Capture, Drag…) run inline on this stack, so an exception anywhere fails the
        // run with its message instead of stalling a child coroutine until the timeout.
        var stack = new Stack<IEnumerator>();
        stack.Push(Scenario());
        while (stack.Count > 0)
        {
            object current;
            try
            {
                var top = stack.Peek();
                if (!top.MoveNext()) { stack.Pop(); continue; }
                current = top.Current;
            }
            catch (Exception e)
            {
                Check("scenario ran without exception", false, e.GetType().Name + ": " + e.Message + " @ " + e.StackTrace.Split('\n')[0]);
                break;
            }
            if (current is IEnumerator nested) { stack.Push(nested); continue; }
            yield return current;
        }
        Finish();
    }

    void OnHaptic(FrontRoomsMobileHaptics.Channel channel, FrontRoomsMobileHaptics.Style style, float intensity) =>
        haptics.Add(F(FrontRoomsTouchMotion.Now - t0) + "," + channel + "," + style + "," + F(intensity));

    IEnumerator Scenario()
    {
        // ---------------------------------------------------------------- title
        yield return WaitFor(() => (game = FindFirstObjectByType<FrontRooms3DGame>()) != null, 10f, "game exists");
        if (aborted) yield break;
        yield return WaitFor(() => (controls = game.GetComponent<FrontRoomsTouchControls>()) != null, 5f, "touch layer added on the handheld path");
        if (aborted) yield break;
        Check("handheld path is active", FrontRoomsHandheld.Active, FrontRoomsHandheld.Active.ToString());
        Check("touch view added", game.GetComponent<FrontRoomsTouchControlsView>() != null, "");
        SetupCapture();
        yield return ColdStartChecks();
        if (aborted) yield break;
        yield return Frames(3.6f);
        Check("title waits for a tap", Phase() == "Title", Phase());
        yield return Capture("01_title_tap_to_start");
        // The adaptive run also frames the title on every Duo pose (the Figma Duo flow), then comes back to its own profile.
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "adaptive") >= 0)
        {
            var own = FrontRoomsHandheld.ForcedProfile;
            var poses = new[] { FrontRoomsHandheld.IPhoneDuoOuter, FrontRoomsHandheld.IPhoneDuoInner, FrontRoomsHandheld.IPhoneDuoOuterFlipped, FrontRoomsHandheld.IPhoneDuoInnerPortrait };
            for (var p = 0; p < poses.Length; p++)
            {
                FrontRoomsHandheld.Force(poses[p]);
                SetupCapture();
                Canvas.ForceUpdateCanvases();
                game.GetType().GetMethod("ApplyMobileSafeAreaLayout", Any)?.Invoke(game, new object[] { true });
                yield return Frames(.5f);
                yield return Capture("adaptive_" + (p + 1) + "_title");
            }
            FrontRoomsHandheld.Force(own);
            SetupCapture();
            Canvas.ForceUpdateCanvases();
            game.GetType().GetMethod("ApplyMobileSafeAreaLayout", Any)?.Invoke(game, new object[] { true });
            yield return Frames(.5f);
        }
        X2TitleChecks();
        yield return CaptureDark("01z_title_dark");
        // The title is the root (only a fresh launch shows it; a retry goes straight back into play): Back hands
        // the app to the system on Android, and the title stays.
        var left = false;
        Action onLeft = () => left = true;
        FrontRoomsMobileBackBridge.LeftApp += onLeft;
        FrontRoomsMobileBackBridge.SimulateBackPressed();
        yield return Frames(.1f);
        FrontRoomsMobileBackBridge.LeftApp -= onLeft;
        Check("title: Back leaves the app (the root)", left && Phase() == "Title", "left " + left + ", " + Phase());

        yield return Tap(new Vector2(437f, 201f), 1);
        yield return WaitFor(() => Phase() == "Playing", 8f, "a tap on the title starts the run");
        if (aborted) yield break;
        yield return Frames(2.5f);
        Check("touch layer in Playing state", controls.CurrentMenuState == FrontRoomsTouchControls.MenuState.Playing, controls.CurrentMenuState.ToString());
        // X2 check 1 (with M1): the 9 s calm-hint card is gone on phones.
        var hint = game.GetType().GetField("contextText", Any)?.GetValue(game) as UnityEngine.UI.Text;
        Check("X2: no calm-hint card in the first 9 s", hint == null || string.IsNullOrEmpty(hint.text), hint == null ? "no contextText" : "'" + hint.text + "'");
        // The top band of TS 1–4 (Figma 2528:5403): the level label hangs from the top-left. From 10-04 to 10-22 the phone
        // layout anchored it to the bottom edge, so it was drawn below the screen and no check noticed.
        var roomRect = HudRect("Room typography");
        Check("HUD: the level label is on screen, top-left", roomRect.HasValue && roomRect.Value.xMin >= 0f && roomRect.Value.xMin < .5f && roomRect.Value.xMax <= 1f && roomRect.Value.yMin >= .5f && roomRect.Value.yMax <= 1f,
            roomRect.HasValue ? "rect " + F(roomRect.Value.xMin) + "," + F(roomRect.Value.yMin) + " – " + F(roomRect.Value.xMax) + "," + F(roomRect.Value.yMax) + " of the HUD (y up)" : "no Room typography");
        yield return Capture("02_calm_ghost_stick");

        var L = controls.Layout;
        HoldRelay();
        Note("layout", "rest " + V(L.StickRest) + " use " + V(L.UseCenter) + " pause " + V(L.PauseCenter) + " frame " + V(L.Frame));

        // Resize the live scene and capture the actual HUD/menu rendering, separately from geometry-only checks.
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "adaptive") >= 0)
        {
            yield return AdaptiveLayoutChecks();
            yield break;
        }

        // -touchFocus examine: only the EXAMINE checks, for quick rounds while building them (a full run still lands).
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-touchFocus") >= 0 && Array.IndexOf(Environment.GetCommandLineArgs(), "examine") >= 0)
        {
            Note("focus", "EXAMINE only (-touchFocus examine)");
            yield return ExamineChecks(L);
            yield return TerminalTouchChecks(L);
            yield break;
        }

        // ---------------------------------------------------------------- stick
        var origin = L.StickRest + new Vector2(40f, 10f);
        Send(2, origin, InputTouchPhase.Began);
        for (var i = 0; i < 10; i++)
        {
            yield return null;
            if (i == 0 || i == 2 || i == 5 || i == 9) yield return Capture("03_stick_in_f" + i.ToString("00"));
        }
        Check("stick takes the touch", controls.StickTouched, "");
        Check("floating stick centres on the thumb", (controls.StickOrigin - origin).magnitude < 1f, V(controls.StickOrigin));

        // Three gaits (Red, 2026-10-08). Slow: near the centre, 1.44 m/s, and the Relay hears nothing.
        var noises = new List<float>();
        Action<float> onNoise = r => noises.Add(r);
        FrontRooms3DGame.WalkNoiseEmitted += onNoise;
        yield return Drag(2, origin, origin + new Vector2(0f, 22f * L.Scale), .2f);
        var startPos = PlayerPos();
        noises.Clear();
        yield return Frames(1.0f);
        var crept = (PlayerPos() - startPos).magnitude;
        Check("slow: near the centre is the slow gait", controls.CurrentGait == FrontRoomsGait.Slow && FrontRooms3DGame.PlayerGait == FrontRoomsGait.Slow,
            controls.CurrentGait + " / " + FrontRooms3DGame.PlayerGait);
        Check("slow: player moved about Slow speed (1.44 m/s)", crept > 1.0f && crept < 1.9f, F(crept) + " m in 1 s");
        Check("slow: the Relay hears nothing", noises.Count == 0, noises.Count + " noises");
        yield return Capture("04s_slow");

        // Walk: out to the ring, the desktop walk, and every step is a noise the Relay can hear nearby.
        yield return Drag(2, origin + new Vector2(0f, 22f * L.Scale), origin + new Vector2(0f, 50f * L.Scale), .15f);
        startPos = PlayerPos();
        noises.Clear();
        yield return Frames(1.0f);
        Check("walk: forward push moves forward", FrontRoomsInput.Snapshot.Move.y > .9f, V(FrontRoomsInput.Snapshot.Move));
        var walked = (PlayerPos() - startPos).magnitude;
        Check("walk: player moved about Walk speed (3.2 m/s)", walked > 2.0f && walked < 4.0f, F(walked) + " m in 1 s");
        Check("walk: its steps are noises at the walk radius (9)", noises.Count >= 1 && noises.TrueForAll(n => Mathf.Approximately(n, 9f)),
            noises.Count + " noises " + FrontRooms3DGame.PlayerGait);
        yield return Capture("04_walk");
        // Past the ring but short of the line: still a walk (Alien: Isolation's sprint fired at the edge itself).
        yield return Drag(2, origin + new Vector2(0f, 50f * L.Scale), origin + new Vector2(0f, 74f * L.Scale), .12f);
        yield return Frames(.3f);
        Check("walk: past the ring but short of the line does not run", !controls.Running && !FrontRoomsInput.Snapshot.SprintHeld && controls.CurrentGait == FrontRoomsGait.Walk,
            controls.CurrentGait + ", reach " + F(controls.RunReach));
        yield return Capture("04r_reaching");

        // Run: across the line, ahead. One rigid click, then the desktop sprint's speed.
        var hapticsBefore = haptics.Count;
        var line = origin + new Vector2(0f, L.RunLineRadius + 10f);
        yield return Drag(2, origin + new Vector2(0f, 74f * L.Scale), line, .1f);
        for (var i = 0; i < 16; i++)
        {
            yield return null;
            if (i == 0 || i == 2 || i == 4 || i == 7 || i == 10 || i == 15) yield return Capture("05_run_f" + i.ToString("00"));
        }
        Check("run: past the line runs", controls.Running && controls.CurrentGait == FrontRoomsGait.Run, controls.CurrentGait.ToString());
        var clicks = haptics.GetRange(hapticsBefore, haptics.Count - hapticsBefore);
        Check("run: crossing the line clicks once (rigid, controls channel)", clicks.FindAll(h => h.Contains("Controls,Rigid")).Count == 1, string.Join(" ", clicks));
        startPos = PlayerPos();
        yield return Frames(1.0f);
        Check("run: the game runs", FrontRooms3DGame.PlayerSprinting && FrontRooms3DGame.PlayerGait == FrontRoomsGait.Run, FrontRooms3DGame.PlayerGait.ToString());
        var ran = (PlayerPos() - startPos).magnitude;
        Check("run: player moved about Run speed (5.5 m/s)", ran > 4.0f && ran < 6.5f, F(ran) + " m in 1 s");
        yield return Capture("06_running");
        // Steering: slide sideways, outside the ring but inside the line: the run holds.
        yield return Drag(2, line, origin + new Vector2(40f, 58f) * L.Scale, .2f);
        yield return Frames(.2f);
        Check("run: between the ring and the line it keeps running", controls.Running, V(controls.StickThumb - controls.StickOrigin));
        // Back inside the ring: walk again.
        yield return Drag(2, origin + new Vector2(40f, 58f) * L.Scale, origin + new Vector2(0f, 45f * L.Scale), .15f);
        yield return Frames(.1f);
        Check("run: back inside the ring walks", !controls.Running && controls.CurrentGait == FrontRoomsGait.Walk, controls.CurrentGait.ToString());
        // A hard strafe past the line never runs: the line is only ahead.
        var strafe = origin + new Vector2(L.RunLineRadius + 12f, 0f);
        yield return Drag(2, origin + new Vector2(0f, 45f * L.Scale), strafe, .15f);
        yield return Frames(.2f);
        Check("run: a strafe past the line does not run", !controls.Running && controls.CurrentGait == FrontRoomsGait.Walk, controls.CurrentGait.ToString());
        Send(2, strafe, InputTouchPhase.Ended);
        for (var i = 0; i < 18; i++)
        {
            yield return null;
            if (i == 0 || i == 3 || i == 6 || i == 10 || i == 17) yield return Capture("07_release_f" + i.ToString("00"));
        }
        Check("release: stick lets go", !controls.StickTouched && controls.CurrentGait == FrontRoomsGait.Still, controls.CurrentGait.ToString());

        // --------------------------------------------------------------- winded
        // Keep the thumb past the line until the 5 s of stamina run dry: the stick says WINDED, the run stops
        // (the desktop's Shift-on-an-empty-bar rule), then comes back by itself once a segment refills.
        Send(2, origin, InputTouchPhase.Began);
        yield return Frames(.1f);
        yield return Drag(2, origin, line, .15f);
        var windedAt = -1;
        for (var i = 0; i < 480 && windedAt < 0; i++)
        {
            yield return null;
            if (FrontRooms3DGame.PlayerWinded) windedAt = i;
        }
        Check("winded: the run runs dry", windedAt >= 0, windedAt >= 0 ? F(windedAt * Step) + " s more" : "never");
        for (var i = 0; i < 16; i++)
        {
            yield return null;
            if (i == 0 || i == 5 || i == 10 || i == 15) yield return Capture("07w_winded_f" + i.ToString("00"));
        }
        Check("winded: the stick says WINDED and the run stops", controls.Winded && !FrontRoomsInput.Snapshot.SprintHeld && !FrontRooms3DGame.PlayerSprinting, "winded " + controls.Winded);
        yield return CaptureDark("07z_label_winded_dark");
        Check("winded: soft double haptic", haptics.FindAll(h => h.Contains("Controls,Soft")).Count >= 2, "");
        var recovered = -1;
        for (var i = 0; i < 300 && recovered < 0; i++)
        {
            yield return null;
            if (!FrontRooms3DGame.PlayerWinded) recovered = i;
        }
        Check("winded: breath back after about 2 s", recovered >= 0, recovered >= 0 ? F(recovered * Step) + " s" : "never");
        // The line refused the thumb while winded; still past it, the run comes back (with its click) like a held Shift.
        yield return Frames(.3f);
        Check("winded: a thumb still past the line runs again", controls.Running && FrontRoomsInput.Snapshot.SprintHeld, "running " + controls.Running);
        yield return Capture("07x_run_again");
        yield return CaptureDark("07z_label_run_dark");
        Send(2, line, InputTouchPhase.Ended);
        FrontRooms3DGame.WalkNoiseEmitted -= onNoise;
        yield return Frames(.4f);
        // The long sprint ends against a wall: turn round so the next steps have room to walk. (No teleport back:
        // the start rooms keep the map's doors and panes asleep, as the desktop autopilot's glass scenario knows.)
        game.GetType().GetField("yaw", Any).SetValue(game, GetFloat("yaw") + 180f);
        yield return Frames(.2f);

        // ----------------------------------------------------------------- look
        var yaw0 = GetFloat("yaw");
        yield return Swipe(3, new Vector2(620f, 180f), new Vector2(720f, 180f), .25f);
        var turned = Mathf.DeltaAngle(yaw0, GetFloat("yaw"));
        Check("look: a 100 pt drag turns about 20 degrees", Mathf.Abs(turned - 20f) < 2.5f, F(turned) + " deg");

        // Two thumbs at once: walk with the left, look with the right.
        Send(4, origin, InputTouchPhase.Began);
        Send(5, new Vector2(650f, 160f), InputTouchPhase.Began);
        yield return null;
        startPos = PlayerPos();
        yaw0 = GetFloat("yaw");
        for (var i = 1; i <= 20; i++)
        {
            Send(4, origin + new Vector2(0f, Mathf.Min(60f, i * 6f) * L.Scale), InputTouchPhase.Moved);
            Send(5, new Vector2(650f + i * 3f, 160f), InputTouchPhase.Moved);
            yield return null;
        }
        Check("two thumbs: moves and looks together", (PlayerPos() - startPos).magnitude > .5f && Mathf.Abs(Mathf.DeltaAngle(yaw0, GetFloat("yaw"))) > 8f,
            F((PlayerPos() - startPos).magnitude) + " m, " + F(Mathf.DeltaAngle(yaw0, GetFloat("yaw"))) + " deg");
        Send(4, origin + new Vector2(0f, 60f * L.Scale), InputTouchPhase.Ended);
        Send(5, new Vector2(710f, 160f), InputTouchPhase.Ended);
        yield return Frames(.4f);

        // ------------------------------------------------------ X2: the stick's slot words
        yield return X2Teaching(L, origin);

        // ------------------------------------------------------------ pause etc
        // The settings sheet is the pause (Red, 2026-10-24): the pause button opens it straight away, no PAUSED card.
        yield return TapWithFrames(L.PauseCenter, 6, "08_pause", new[] { 1, 4, 8, 12, 18, 30 }, i => { });
        Check("pause button pauses", Phase() == "Paused", Phase());
        Check("pause opens the settings sheet straight away (no PAUSED card)",
            GetBool("displaySettingsOpen") && controls.CurrentMenuState == FrontRoomsTouchControls.MenuState.Settings, controls.CurrentMenuState.ToString());
        var resumeShown = controls.TryGetChip(FrontRoomsTouchControls.Button.SettingsClose, out var resumeChip, out _);
        var endShown = controls.TryGetChip(FrontRoomsTouchControls.Button.Restart, out var endChip, out _);
        Check("pause: RESUME and END RUN sit in the sheet's top-right corner",
            resumeShown && endShown && endChip.x < resumeChip.x && Mathf.Abs(endChip.y - resumeChip.y) < .5f && resumeChip.y > controls.Layout.Center.y,
            "RESUME " + V(resumeChip) + ", END RUN " + V(endChip));
        yield return Frames(.4f);
        // The tabbed sheet (设置菜单与画质预设, 2026-10-24): GRAPHICS first, a control per row, the explanation and preview.
        Check("settings: the sheet opens on GRAPHICS with QUALITY", controls.SettingsTab == FrontRoomsSettingsMenu.Tab.Graphics && FindRow("QUALITY").HasValue, controls.SettingsTab.ToString());
        var pixels = FrontRoomsHandheld.ScreenPixels;
        Note("settings", "render scale " + F(FrontRoomsQuality.EffectiveRenderScale) + " at " + pixels.x + " x " + pixels.y + " px, preset " + FrontRoomsQuality.CurrentPreset + (FrontRoomsQuality.IsCustom ? " (custom)" : ""));
        var px = pixels.x * pixels.y;
        Check("settings: RESOLUTION AUTO keeps a 3.2 Mpx budget (below it 100 %, above it less)",
            FrontRoomsQuality.Resolution != 0 || (px <= 3.3e6f ? FrontRoomsQuality.EffectiveRenderScale > .99f : FrontRoomsQuality.EffectiveRenderScale < .99f && FrontRoomsQuality.EffectiveRenderScale >= .5f),
            F(px / 1e6f) + " Mpx → " + F(FrontRoomsQuality.EffectiveRenderScale));
        var preset = FrontRoomsQuality.CurrentPreset;
        var qualityRow = FindRow("QUALITY");
        if (qualityRow.HasValue) yield return Tap(SegmentInView(qualityRow.Value, 0), 7);
        yield return Frames(.25f);
        Check("settings: tapping LOW on QUALITY picks the LOW preset", FrontRoomsQuality.CurrentPreset == FrontRoomsQuality.Preset.Low && !FrontRoomsQuality.IsCustom, FrontRoomsQuality.CurrentPreset.ToString());
        yield return Capture("09b_settings_graphics_low");
        qualityRow = FindRow("QUALITY");
        if (qualityRow.HasValue) yield return Tap(SegmentInView(qualityRow.Value, (int)preset), 7);
        yield return Frames(.25f);
        Check("settings: tapping it back restores the preset", FrontRoomsQuality.CurrentPreset == preset, FrontRoomsQuality.CurrentPreset.ToString());
        // Red 2026-10-24 (D2 · C, D4 · B): handheld presets by purpose; ProMotion frame rates 30 · 40 · 60 · 120.
        qualityRow = FindRow("QUALITY");
        Check("settings: handheld presets read BATTERY · BALANCED · QUALITY · ULTRA · CUSTOM",
            qualityRow.HasValue && string.Join(",", qualityRow.Value.Row.Options) == "BATTERY,BALANCED,QUALITY,ULTRA,CUSTOM", qualityRow.HasValue ? string.Join(",", qualityRow.Value.Row.Options) : "no row");
        var rateRow = FindRow("FRAME RATE");
        Check("settings: FRAME RATE offers 30 · 40 · 60 · 120 FPS on a ProMotion handheld (the Editor counts as one)",
            rateRow.HasValue && string.Join(",", rateRow.Value.Row.Options) == "30 FPS,40 FPS,60 FPS,120 FPS", rateRow.HasValue ? string.Join(",", rateRow.Value.Row.Options) : "no row");
        if (rateRow.HasValue) yield return Tap(SegmentInView(rateRow.Value, 1), 7);
        yield return Frames(.25f);
        Check("settings: tapping 40 FPS sets the game's target to 40", Application.targetFrameRate == 40, Application.targetFrameRate.ToString());
        rateRow = FindRow("FRAME RATE");
        if (rateRow.HasValue) yield return Tap(SegmentInView(rateRow.Value, 2), 7);
        yield return Frames(.25f);
        Check("settings: tapping 60 FPS sets it back", Application.targetFrameRate == 60, Application.targetFrameRate.ToString());
        // A tap on a row's label explains it: the explanation column on wide sheets, under the row on narrow ones.
        var resRow = FindRow("RESOLUTION");
        if (resRow.HasValue) yield return Tap(new Vector2(resRow.Value.Rect.xMin + 20f, RowInView(resRow.Value).y), 7);
        yield return Frames(.3f);
        var single = controls.Layout.SettingsSingleColumn;
        var detailSlot = (FrontRoomsTouchControls.SettingsSlot?)null;
        foreach (var d in controls.SettingsSlots) if (d.Detail) detailSlot = d;
        Check("settings: tapping a label explains the row (under it on a narrow sheet, in the column on a wide one)",
            single ? detailSlot.HasValue && detailSlot.Value.Label == "RESOLUTION" : resRow.HasValue && controls.ActiveSettingsRow == resRow.Value.Id,
            single ? (detailSlot.HasValue ? detailSlot.Value.Label : "no explanation row") : controls.ActiveSettingsRow.ToString());
        yield return Capture("09c_settings_explained");
        // The preview (Red, 2026-10-24): a tap shows the game full screen at these settings; another tap returns.
        var previewAt = single ? (detailSlot.HasValue ? RowInView(detailSlot.Value) : Vector2.zero) : controls.SettingsPreviewRect.center;
        if (previewAt != Vector2.zero) yield return Tap(previewAt, 7);
        yield return Frames(.4f);
        Check("settings: a tap on the preview shows the game full screen", controls.SettingsPeek, "peek " + controls.SettingsPeek);
        yield return Capture("09d_settings_preview_full_screen");
        yield return Tap(new Vector2(L.Frame.x * .5f, L.Frame.y * .5f), 7);
        yield return Frames(.4f);
        Check("settings: another tap brings the sheet back", !controls.SettingsPeek && controls.CurrentMenuState == FrontRoomsTouchControls.MenuState.Settings, "peek " + controls.SettingsPeek);
        // ASSIST: CAPTIONS is a switch; a tap on the row flips it.
        yield return Tap(controls.SettingsTabRects[3].center, 7);
        yield return Frames(.3f);
        Check("settings: the ASSIST tab opens", controls.SettingsTab == FrontRoomsSettingsMenu.Tab.Assist, controls.SettingsTab.ToString());
        var captions = FrontRoomsSettings.Captions;
        var captionsRow = FindRow("CAPTIONS");
        Check("settings: the desktop rows are on the touch card", captionsRow.HasValue, "");
        if (captionsRow.HasValue) yield return Tap(RowInView(captionsRow.Value), 7);
        yield return Frames(.25f);
        Check("settings: tapping CAPTIONS toggles it", FrontRoomsSettings.Captions != captions, FrontRoomsSettings.Captions.ToString());
        yield return Capture("10_settings_row_changed");
        if (captionsRow.HasValue) yield return Tap(RowInView(captionsRow.Value), 7);
        // CONTROLS: LOOK SPEED is a slider; a tap on its track jumps to the nearest stop.
        yield return Tap(controls.SettingsTabRects[2].center, 7);
        yield return Frames(.3f);
        Check("settings: the CONTROLS tab opens", controls.SettingsTab == FrontRoomsSettingsMenu.Tab.Controls, controls.SettingsTab.ToString());
        var look = FrontRoomsSettings.TouchLookSpeedPercent;
        var lookRow = FindRow("LOOK SPEED");
        if (lookRow.HasValue) yield return Tap(StopInView(lookRow.Value, lookRow.Value.Row.Index + 1), 7);
        yield return Frames(.2f);
        Check("settings: a tap further along LOOK SPEED's track turns it up", FrontRoomsSettings.TouchLookSpeedPercent > look, look + " → " + FrontRoomsSettings.TouchLookSpeedPercent);
        lookRow = FindRow("LOOK SPEED");
        if (lookRow.HasValue) yield return Tap(StopInView(lookRow.Value, lookRow.Value.Row.Index - 1), 7);
        yield return Frames(.2f);
        Check("settings: a tap back along the track turns it down again", FrontRoomsSettings.TouchLookSpeedPercent == look, FrontRoomsSettings.TouchLookSpeedPercent.ToString());
        yield return Capture("10b_settings_controls");
        // X2 check 13: STICK HINTS reads OFF with both words learned; a tap turns it ON and clears both, another retires both.
        FrontRoomsTouchControls.SetTaught(true, true);
        FrontRoomsTouchControls.SetTaught(false, true);
        yield return Frames(.2f);
        var hintsRow = FindRow("STICK HINTS");
        Check("X2 settings: STICK HINTS reads OFF with both words learned", hintsRow.HasValue && hintsRow.Value.Value == "OFF", hintsRow.HasValue ? hintsRow.Value.Value : "no row");
        if (hintsRow.HasValue)
        {
            yield return Tap(RowInView(hintsRow.Value), 7);
            yield return Frames(.3f);
            Check("X2 settings: a tap turns STICK HINTS ON and clears both words",
                !FrontRoomsTouchControls.TaughtSlow && !FrontRoomsTouchControls.TaughtRun && FindRow("STICK HINTS")?.Value == "ON", FindRow("STICK HINTS")?.Value);
            yield return Capture("x2_13_stick_hints_on");
            hintsRow = FindRow("STICK HINTS");
            yield return Tap(RowInView(hintsRow.Value), 7);
            yield return Frames(.3f);
            Check("X2 settings: another tap turns it OFF and retires both", FrontRoomsTouchControls.TaughtSlow && FrontRoomsTouchControls.TaughtRun, FindRow("STICK HINTS")?.Value);
        }
        FrontRoomsTouchControls.SetTaught(true, false);
        FrontRoomsTouchControls.SetTaught(false, false);
        SetSettingsScroll(0f);
        yield return Frames(.2f);
        var vp = controls.SettingsScrollViewport;
        yield return Swipe(8, new Vector2(vp.center.x, vp.yMin + 30f), new Vector2(vp.center.x, vp.yMin + 160f), .3f);
        yield return Frames(.3f);
        // The sheet runs to the bottom edge (Red, 2026-10-24): a tall window shows every row, so it only scrolls when it must.
        var mustScroll = controls.SettingsScrollMax > .5f;
        var lastRow = float.MaxValue;
        foreach (var s in controls.SettingsSlots) if (!s.Header) lastRow = Mathf.Min(lastRow, s.Rect.yMin);
        Check("settings: the list scrolls when it runs past the home inset, else every row shows",
            // A list a few points too long scrolls only those points: it must reach its end (or at least 20 pt).
            mustScroll ? controls.SettingsScroll >= Mathf.Min(20f, controls.SettingsScrollMax) - .5f : lastRow >= controls.Layout.SettingsBottomInset - .5f,
            F(controls.SettingsScroll) + " / " + F(controls.SettingsScrollMax) + ", last row " + F(lastRow) + ", inset " + F(controls.Layout.SettingsBottomInset));
        // Rounded corners (research/interaction/duo/rounded_corners): SETTINGS and the tabs keep 16 pt from a curve.
        var strip = controls.SettingsTabRects[0];
        for (var i = 1; i < controls.SettingsTabRects.Length; i++) strip = Rect.MinMaxRect(Mathf.Min(strip.xMin, controls.SettingsTabRects[i].xMin), strip.yMin, Mathf.Max(strip.xMax, controls.SettingsTabRects[i].xMax), strip.yMax);
        var cleared = FrontRoomsHandheld.ClearRect(strip, false, 12f, FrontRoomsHandheld.HudCornerGap);
        Check("settings: the tabs clear cameras and keep 16 pt from a rounded corner", Mathf.Abs(cleared.x - strip.x) < .6f, "strip x " + F(strip.x) + ", clear x " + F(cleared.x));
        yield return Capture("11_settings_scrolled");
        // BACK must be drawn, not just hit-testable: an opaque card once hid it while taps still worked.
        if (controls.TryGetChip(FrontRoomsTouchControls.Button.SettingsClose, out var backAt, out var backSize))
        {
            // A keyline chip with the word in paper (平面视觉 fix 2): some pixel across its middle is paper.
            var brightest = 0f;
            for (var dx = -backSize.x * .5f; dx <= backSize.x * .5f; dx += 1.5f) brightest = Mathf.Max(brightest, UiColourAt(backAt + new Vector2(dx, 0f)).grayscale);
            Check("settings: BACK is visible on the card", brightest > .75f, "brightest " + F(brightest));
        }
        yield return ChipTap(FrontRoomsTouchControls.Button.SettingsClose, "12_settings_close", new[] { 1, 6, 14 });
        Check("RESUME (the sheet's old BACK) resumes the run", !GetBool("displaySettingsOpen") && Phase() == "Playing", Phase());
        // The frost around the card resumes too.
        yield return Frames(.4f);
        yield return Tap(controls.Layout.PauseCenter, 6);
        yield return Frames(.5f);
        // Where the sheet leaves a frost margin a thumb can hit (44 pt), a tap there goes back. The Duo's outer display has
        // none since the sheet runs full bleed (Red, 2026-10-24): BACK is the way back there.
        var sheetL = controls.Layout;
        var sheet = new Rect(sheetL.SettingsCardCenter - sheetL.SettingsCardSize * .5f, sheetL.SettingsCardSize);
        if (sheet.xMin >= 44f)
        {
            yield return Tap(new Vector2(sheet.xMin * .5f, L.Frame.y * .5f), 7);
            yield return Frames(.3f);
            Check("settings: a tap on the frost outside the card resumes", !GetBool("displaySettingsOpen") && Phase() == "Playing", Phase());
        }
        else
        {
            Note("settings", "no frost margin to tap (sheet from x " + F(sheet.xMin) + "): BACK is the way back");
            yield return ChipTap(FrontRoomsTouchControls.Button.SettingsClose, null, null);
            yield return Frames(.3f);
            Check("settings: with no frost margin, RESUME resumes", !GetBool("displaySettingsOpen") && Phase() == "Playing", Phase());
        }
        yield return Frames(.4f);
        yield return Tap(controls.Layout.PauseCenter, 6);
        yield return Frames(.5f);
        // END RUN goes back to the title at once since Red's 2026-10-30 answer, so it is checked last (it ends the run).
        yield return ChipTap(FrontRoomsTouchControls.Button.SettingsClose, "14_resume", new[] { 1, 6, 12 });
        Check("RESUME returns to play", Phase() == "Playing", Phase());
        yield return Frames(.4f);

        // -------------------------------------------------------- Android back
        // The bridge's own queue (as the predictive-back callback fills it): one step back each time.
        FrontRoomsMobileBackBridge.SimulateBackPressed();
        yield return Frames(.1f);
        Check("back: pauses into the settings sheet", Phase() == "Paused" && GetBool("displaySettingsOpen"), Phase());
        yield return Frames(.3f);
        FrontRoomsMobileBackBridge.SimulateBackPressed();
        yield return Frames(.1f);
        Check("back: resumes from the sheet", Phase() == "Playing", Phase());
        yield return Frames(.4f);

        HoldRelay();

        // ------------------------------------------------------------- doors
        // A shut door faces the player, so the tap lands reliably: tap the door itself to open it (HIG: tap the
        // object), then USE shuts it again (the verb crossfades) and the latch is felt.
        // Never C22's EXIT door: a stand 1.4 m inside the stairwell changes the floor mid-test (关卡设计, 2026-10-22).
        // Tired at the door (stamina 2 of 5 s): the dashes show with the prompt, the frame 平面视觉 compares.
        game.GetType().GetField("stamina", Any)?.SetValue(game, 2f);
        if (StandAt(MapColliders("doorByCollider"), col => !Map.IsExitDoor(Map.DoorOf(col)) && Describe(col, out var hold) is string d && !hold && d.Contains("OPEN DOOR"), 1.4f, out var door))
        {
            for (var i = 0; i < 16; i++)
            {
                yield return null;
                if (i == 1 || i == 4 || i == 8 || i == 15) yield return Capture("15_use_in_f" + i.ToString("00"));
            }
            // 平面视觉 2026-10-22: on phones the stamina dashes have their own line at dot −58, under the prompt (−26).
            var promptRect = HudRect("Key prompt");
            var dashRect = HudRect("Stamina 1");
            Check("HUD: tired at a door, the stamina dashes sit under the prompt, not on its line", promptRect.HasValue && dashRect.HasValue && dashRect.Value.yMax <= promptRect.Value.yMin,
                promptRect.HasValue && dashRect.HasValue ? "dashes top " + F(dashRect.Value.yMax) + ", prompt bottom " + F(promptRect.Value.yMin) + " (share of the HUD, y up)" : "missing");
            // 平面视觉 2026-10-22 17:3x: phone dashes 20 x 5 on a 25 pt pitch (row 120), 1.2x the 100 x 3 hold bar: desktop's
            // 24 x 6 / 30 / 120 x 4 at the phone's x0.85 angular ramp. Ratios, so the check holds on every profile.
            var dash1 = HudRect("Stamina 1");
            var dash2 = HudRect("Stamina 2");
            var dash5 = HudRect("Stamina 5");
            var bar = HudRect("Hold bar");
            var sized = dash1.HasValue && dash2.HasValue && dash5.HasValue && bar.HasValue;
            var dashToPitch = sized ? dash1.Value.width / Mathf.Max(1e-5f, dash2.Value.xMin - dash1.Value.xMin) : 0f;
            var rowToBar = sized ? (dash5.Value.xMax - dash1.Value.xMin) / Mathf.Max(1e-5f, bar.Value.width) : 0f;
            var barToDash = sized ? (bar.Value.height / Mathf.Max(1e-5f, dash1.Value.height)) : 0f;
            Check("HUD: phone dashes are 20 on a 25 pitch, their row 1.2x the hold bar, the bar 3 to the dashes' 5",
                sized && Mathf.Abs(dashToPitch - .8f) < .02f && Mathf.Abs(rowToBar - 1.2f) < .02f && Mathf.Abs(barToDash - .6f) < .03f,
                sized ? "dash/pitch " + F(dashToPitch) + ", row/bar " + F(rowToBar) + ", bar/dash height " + F(barToDash) : "missing");
            // The tightest stack that shows on a phone: tired at a door with a caption up. The caption card (bottom 99 pt)
            // must stay under the dashes.
            var captionsWere = FrontRoomsSettings.Captions;
            FrontRoomsSettings.SetCaptions(true);
            var behind = PlayerRoot.position - Quaternion.Euler(0f, GetFloat("yaw"), 0f) * Vector3.forward * 3f;
            FrontRoomsCaptions.Post("STEPS", behind);
            yield return Frames(.3f);
            yield return Capture("15c_caption_tired_door");
            var cap = HudRect("HUD / Captions");
            var dashNow = HudRect("Stamina 1");
            // 平面视觉 2026-10-22 18:0x: the phone caption card hangs from a fixed top at dot -66, 5.5 pt under the dashes'
            // bottom (dot -60.5), on every aspect; one line is 30 tall (10 + 20 x lines).
            var hudH = HudHeight();
            var capGap = cap.HasValue && dashNow.HasValue ? (dashNow.Value.yMin - cap.Value.yMax) * hudH : -99f;
            var capTall = cap.HasValue ? cap.Value.height * hudH : 0f;
            Check("HUD: tired at a door with a caption up, the card hangs 5.5 pt under the dashes, one line 30 tall",
                Mathf.Abs(capGap - 5.5f) < .75f && Mathf.Abs(capTall - 30f) < .75f, "gap " + F(capGap) + " pt, card " + F(capTall) + " tall");
            FrontRoomsCaptions.Clear();
            FrontRoomsSettings.SetCaptions(captionsWere);
            Check("USE appears on a door", controls.CurrentUsePrompt.Visible && controls.CurrentUsePrompt.Kind == FrontRoomsTouchControls.UseKind.Open, controls.CurrentUsePrompt.Kind.ToString());
            if (TapPointOn(door, out var onDoor))
            {
                var tapped = haptics.Count;
                yield return Tap(onDoor, 15);
                yield return Frames(1.2f);
                var opened = Describe(door, out _);
                Check("tapping the door itself opens it", opened != null && opened.Contains("SHUT DOOR"), "now " + opened);
                Check("a tap on the door plays the USE haptic", haptics.FindIndex(tapped, h => h.Contains("Controls,Rigid")) >= 0, "");
                yield return Capture("18b_door_tapped_open");
            }
            else Check("door leaf on screen to tap", false, "no tap point on " + door.name);

            // The leaf swung away: stand where it is in reach again, then USE shuts it.
            if (StandAt(new[] { door }, col => Describe(col, out _) is string d && d.Contains("SHUT DOOR"), 1.2f, out _))
            {
                yield return Frames(.35f);
                Check("USE now says SHUT", controls.CurrentUsePrompt.Visible && controls.CurrentUsePrompt.Kind == FrontRoomsTouchControls.UseKind.Shut, controls.CurrentUsePrompt.Kind.ToString());
                var before = Describe(door, out _);
                var heard = haptics.Count;
                Send(9, L.UseCenter, InputTouchPhase.Began);
                for (var i = 0; i < 6; i++) { yield return null; if (i == 1 || i == 4) yield return Capture("16_use_press_f" + i.ToString("00")); }
                Send(9, L.UseCenter, InputTouchPhase.Ended);
                for (var i = 0; i < 12; i++) { yield return null; if (i == 3 || i == 11) yield return Capture("17_use_release_f" + i.ToString("00")); }
                yield return Frames(1.6f);
                var after = Describe(door, out _);
                Check("USE shuts the door", after != null && after.Contains("OPEN DOOR"), before + " → " + after);
                Check("USE press played a control haptic", haptics.FindIndex(heard, h => h.Contains("Controls,Rigid")) >= 0, "");
                Check("door: the latch is felt as it shuts", haptics.FindIndex(heard, h => h.Contains("Gameplay,Light")) >= 0, string.Join(" | ", haptics.GetRange(heard, haptics.Count - heard)));
                yield return Capture("18_door_shut_use_says_open");
            }
            else Check("open door back in reach", false, door.name);
        }
        else Check("found a door to test USE", false, "no OPEN DOOR on the map");

        // ------------------------------------------------------- USE: locked door
        // The shipped profile has doorsNeedKeys off, so no door is locked: switch keys on for this step only
        // (runtime field on this run's map; the profile asset is untouched) and put it back afterwards.
        var keysField = Map.GetType().GetField("doorsNeedKeys", Any);
        var keysWere = keysField != null && (bool)keysField.GetValue(Map);
        if (keysField != null && !keysWere) { keysField.SetValue(Map, true); Note("locked", "doorsNeedKeys forced on for this step"); }
        if (StandAt(MapColliders("doorByCollider"), col => !Map.IsExitDoor(Map.DoorOf(col)) && Describe(col, out var hold) is string d && d.StartsWith("LOCKED"), 1.4f, out var locked))
        {
            yield return Frames(.35f);
            Check("USE says LOCKED", controls.CurrentUsePrompt.Visible && controls.CurrentUsePrompt.Kind == FrontRoomsTouchControls.UseKind.Locked, controls.CurrentUsePrompt.Kind.ToString());
            var heard = haptics.Count;
            Send(16, L.UseCenter, InputTouchPhase.Began);
            for (var i = 0; i < 16; i++)
            {
                yield return null;
                if (i == 2 || i == 5 || i == 8 || i == 12) yield return Capture("18c_locked_shake_f" + i.ToString("00"));
            }
            Send(16, L.UseCenter, InputTouchPhase.Ended);
            yield return Frames(.8f);
            Check("locked: still shut", Describe(locked, out _)?.StartsWith("LOCKED") == true, "");
            Check("locked: the rattle is felt twice", haptics.GetRange(heard, haptics.Count - heard).FindAll(h => h.Contains("Gameplay,Rigid")).Count >= 2, string.Join(" | ", haptics.GetRange(heard, haptics.Count - heard)));
            // Holding this zone's key, a tap on the door itself goes the same road as E (关卡设计's UseDoor): the key turns
            // and the unlock shot runs, instead of the map holding the door for its watchdog.
            Map.GiveKeyForTools(Map.ZoneOf(Map.CellOf(PlayerRoot.position)).id);
            yield return Frames(.4f);
            if (TapPointOn(locked, out var onLock))
            {
                yield return Tap(onLock, 17);
                var shot = game.GetType().GetField("doorShot", Any)?.GetValue(game) as FrontRoomsDoorShot;
                var keyTurning = false;
                for (var i = 0; i < 30 && !keyTurning; i++) { yield return null; keyTurning = shot != null && shot.UnlockActive; }
                Check("locked: with the key, a tap on the door starts the unlock shot", keyTurning, shot == null ? "no door shot" : "live " + shot.Live);
                yield return Capture("18d_key_turn_tap");
                // The unlock is a takeover: the desktop HUD fades for it, and so must USE (it showed a stale OPEN, run 21).
                yield return Frames(.35f);
                Check("key turn: USE fades with the takeover", controls.Takeover > .9f && UseAlpha() < .1f,
                    "takeover " + F(controls.Takeover) + ", USE alpha " + F(UseAlpha()));
                yield return Capture("18e_key_turn_takeover");
                var ended = -1;
                for (var i = 0; i < 480 && ended < 0; i++) { yield return null; if (shot == null || !shot.UnlockActive) ended = i; }
                Check("locked: the key turn ends by itself", ended >= 0, ended >= 0 ? F(ended * Step) + " s" : "still turning after 8 s");
                yield return Frames(.4f);
            }
            else Note("locked", "no tap point on the locked door (key-turn tap skipped)");
        }
        else Note("locked", "no locked door on the map (skipped)");
        if (keysField != null && !keysWere) keysField.SetValue(Map, false);

        // ------------------------------------------------------- USE: broken door (交互设计, 2026-10-22)
        // Red: "手机上对着被撞坏的门时，USE 按钮显示门已经坏了". A door the Relay broke (here the map's own BreakDoor, from the
        // far side) hangs open for the run. USE says BROKEN; a press is the won't-shut try (the map answers Broken: no
        // door verb, no noise), USE swings once and one soft knock is felt; a press inside 0.55 s is dropped (the map's
        // guard); a tap on the leaf is the same try.
        var toBreak = Map.NearestClosedDoorForTools(PlayerRoot.position, false);
        // Never C22's EXIT door (it seals the floor). The other swing side gives the nearest other door.
        if (toBreak != null && Map.IsExitDoor(toBreak)) toBreak = Map.NearestClosedDoorForTools(PlayerRoot.position, true);
        if (toBreak != null && Map.IsExitDoor(toBreak)) toBreak = null;
        if (toBreak != null)
        {
            var tries = 0;
            Action<FrontRoomsMapWorld.Door, Vector3> onTry = (d, p) => { if (d != null && d.edge == toBreak.edge) tries++; };
            Map.DoorBrokenTried += onTry;
            Map.BreakDoor(toBreak, toBreak.position + (toBreak.position - PlayerRoot.position));
            yield return Frames(1.2f);
            Func<Collider, bool> isBroken = col => Map.DoorOf(col) is FrontRoomsMapWorld.Door d && d.edge == toBreak.edge && d.broken;
            Collider brokenLeaf = null;
            // The hanging leaf can be seen near edge-on: stand where the game's own crosshair lands on it.
            yield return StandAtSeen(MapColliders("doorByCollider"), isBroken, 1.4f, c => brokenLeaf = c, .35f);
            if (brokenLeaf == null) yield return StandAtSeen(MapColliders("doorByCollider"), isBroken, 1f, c => brokenLeaf = c, .35f);
            if (brokenLeaf != null)
            {
                yield return Frames(.35f);
                var usePrompt = controls.CurrentUsePrompt;
                Check("broken: the crosshair on the hanging leaf turns USE into BROKEN", usePrompt.Visible && usePrompt.Kind == FrontRoomsTouchControls.UseKind.Broken,
                    usePrompt.Kind + ", desktop prompt '" + Describe(brokenLeaf, out _) + "', crosshair on " + CrosshairHit());
                yield return Capture("18f_broken_use");
                var heard = haptics.Count;
                var maxSwing = 0f;
                Send(21, L.UseCenter, InputTouchPhase.Began);
                for (var i = 0; i < 45; i++)
                {
                    yield return null;
                    if (i == 4) Send(21, L.UseCenter, InputTouchPhase.Ended);
                    // A second press 0.25 s in, inside the 0.55 s window.
                    if (i == 15) Send(22, L.UseCenter, InputTouchPhase.Began);
                    if (i == 18) Send(22, L.UseCenter, InputTouchPhase.Ended);
                    maxSwing = Mathf.Max(maxSwing, Mathf.Abs(NodeAngle("USE")));
                    if (i == 3 || i == 8 || i == 14 || i == 24) yield return Capture("18g_broken_swing_f" + i.ToString("00"));
                }
                var brokenDoor = Map.DoorOf(brokenLeaf);
                Check("broken: a USE press is the won't-shut try (the map answers Broken once; the second press, 0.25 s in, is dropped)", tries == 1, tries + " tries");
                Check("broken: the door stays broken and open (no door verb)", brokenDoor != null && brokenDoor.broken && brokenDoor.open,
                    brokenDoor == null ? "no door" : "broken " + brokenDoor.broken + ", open " + brokenDoor.open);
                Check("broken: USE swings once from its top point (B·4's won't-shut), then rests", maxSwing >= 3f && Mathf.Abs(NodeAngle("USE")) < .01f,
                    "max " + F(maxSwing) + " deg, now " + F(NodeAngle("USE")) + " deg");
                var felt = haptics.GetRange(heard, haptics.Count - heard);
                var click = felt.Find(h => h.Contains("Controls,Rigid"));
                var knock = felt.Find(h => h.Contains("Gameplay,Soft"));
                var gap = click != null && knock != null ? float.Parse(knock.Split(',')[0], System.Globalization.CultureInfo.InvariantCulture) - float.Parse(click.Split(',')[0], System.Globalization.CultureInfo.InvariantCulture) : -1f;
                Check("broken: the press clicks, then one soft knock as the swing meets the frame (about 0.09 s)", felt.FindAll(h => h.Contains("Gameplay,Soft")).Count == 1 && gap >= .06f && gap <= .15f,
                    string.Join(" | ", felt));
                // Past the window, a press tries it again.
                Send(23, L.UseCenter, InputTouchPhase.Began);
                for (var i = 0; i < 4; i++) yield return null;
                Send(23, L.UseCenter, InputTouchPhase.Ended);
                yield return Frames(.7f);
                Check("broken: past 0.55 s a press tries it again", tries == 2, tries + " tries");
                // A tap on the leaf itself is the same try, and USE (naming this door) swings again.
                var swungAt = controls.BrokenTriedAt;
                if (TapPointOn(brokenLeaf, out var onLeaf))
                {
                    yield return Tap(onLeaf, 24);
                    yield return Frames(.1f);
                    Check("broken: a tap on the hanging leaf is the same try, and USE swings", tries == 3 && controls.BrokenTriedAt > swungAt, tries + " tries, tap at " + onLeaf);
                    yield return Capture("18h_broken_tapped");
                    yield return Frames(.6f);
                }
                else Note("broken", "no tap point on the broken leaf (tap case skipped)");
            }
            else Check("broken: a stand at the broken leaf", false, "no stand with the leaf in view");
            Map.DoorBrokenTried -= onTry;
        }
        else Note("broken", "no shut door to break (skipped)");

        // ------------------------------------------------------- keys: on purpose (C14 on phones)
        // Phones take keys on purpose too (2026-10-22): walking over one does nothing, USE says TAKE with the
        // crosshair on it, and a tap on it, or within KeyTapRadiusPoints of it, takes it.
        var keys = new List<Collider>(MapColliders("keyByCollider"));
        // The station's floor key (A26) belongs to the station section, which takes it from the take point.
        if (Map.HasStation && Map.StationAnchor(FrontRooms.Map.ModuleMarkerKind.KeySpot, out var stationSpot, out _, out _))
            keys.RemoveAll(k => k != null && (k.bounds.center - stationSpot).magnitude < .6f);
        keys.Sort((a, b) => (a.bounds.center - PlayerRoot.position).sqrMagnitude.CompareTo((b.bounds.center - PlayerRoot.position).sqrMagnitude));
        // Until 关卡设计's line (KeyPickupNeedsE on handhelds, queued with A12) lands, phones still walk over keys: skip these steps.
        if (Map.KeyPickupNeedsE && keys.Count == 0 && Map.HasStation) Note("keys", "the station's key is this floor's only key: the station section takes it");
        else if (Map.KeyPickupNeedsE) Check("keys: phones take keys on purpose (each key carries a look+tap trigger)", keys.Count > 0, keys.Count + " keys");
        else { Note("keys", "KeyPickupNeedsE is off on handhelds (walk-over keys): the key-tap steps are skipped"); keys.Clear(); }
        var keyIndex = 0;
        Collider NextKey(float distance)
        {
            for (; keyIndex < keys.Count; keyIndex++)
                if (keys[keyIndex] != null && PlaceAtKey(keys[keyIndex], distance)) return keys[keyIndex++];
            return null;
        }
        // The USE case needs the crosshair on the key: stand where the game's own key ray (KeyOnRay from the gameplay
        // eye) finds it, as StandAtSeen does for the broken leaf (a key is a small target).
        Collider keyA = null;
        for (var tried = 0; keyIndex < keys.Count && keyA == null && tried < 8; keyIndex++, tried++)
        {
            if (keys[keyIndex] == null) continue;
            var seen = false;
            var candidate = keys[keyIndex];
            yield return StandAtKeySeen(candidate, 1.2f, ok => seen = ok);
            if (seen) keyA = candidate;
        }
        if (keyA != null)
        {
            yield return Frames(.35f);
            var usePrompt = controls.CurrentUsePrompt;
            Check("keys: the crosshair on a key turns USE into TAKE", usePrompt.Visible && usePrompt.Kind == FrontRoomsTouchControls.UseKind.Take, usePrompt.Kind + " '" + usePrompt.Label + "'");
            yield return Capture("19a_key_use_take");
            Send(18, L.UseCenter, InputTouchPhase.Began);
            for (var i = 0; i < 4; i++) yield return null;
            Send(18, L.UseCenter, InputTouchPhase.Ended);
            yield return Frames(.5f);
            Check("keys: USE takes it", KeyGone(keyA), keyA != null ? keyA.name : "-");
        }
        else Note("keys", "no key to stand at for the USE case (skipped)");
        var keyB = NextKey(1.2f);
        if (keyB != null)
        {
            // Look past it to the left, so the key sits right of the crosshair in the look zone and the tap is the only way in.
            TurnBy(-14f);
            yield return Frames(.3f);
            var offAim = controls.CurrentUsePrompt.Kind != FrontRoomsTouchControls.UseKind.Take;
            var onOff = 0f;
            if (KeyTapPoint(keyB, ref onOff, out var onKey))
            {
                yield return Tap(onKey, 19);
                yield return Frames(.5f);
                Check("keys: a tap on the key takes it (crosshair elsewhere)", offAim && KeyGone(keyB), "crosshair off it " + offAim + ", tap at " + onKey);
                yield return Capture("19b_key_tapped");
            }
            else Note("keys", "no tap point on " + keyB.name + " (direct tap skipped)");
        }
        else Note("keys", "no second key for the direct tap (skipped)");
        var keyC = NextKey(1.2f);
        if (keyC != null)
        {
            // Walking over it does nothing on phones now.
            var over = keyC.bounds.center;
            var keyBody = (CharacterController)game.GetType().GetField("playerBody", Any).GetValue(game);
            var standOver = new Vector3(over.x, PlayerRoot.position.y, over.z);
            if (!Physics.CheckCapsule(standOver + Vector3.up * .35f, standOver + Vector3.up * 1.6f, .25f, ~0, QueryTriggerInteraction.Ignore))
            {
                var back = PlayerRoot.position;
                keyBody.enabled = false;
                PlayerRoot.position = standOver;
                keyBody.enabled = true;
                yield return Frames(.6f);
                Check("keys: walking over a key leaves it", !KeyGone(keyC), keyC.name);
                keyBody.enabled = false;
                PlayerRoot.position = back;
                keyBody.enabled = true;
            }
            else Note("keys", "no room to stand over " + keyC.name + " (walk-over case skipped)");
            PlaceAtKey(keyC, 1.2f);
            TurnBy(-14f);
            yield return Frames(.3f);
            var nearOff = 12f;
            if (KeyTapPoint(keyC, ref nearOff, out var nearKey))
            {
                yield return Tap(nearKey, 20);
                yield return Frames(.5f);
                Check("keys: a tap " + F(nearOff) + " pt off the key, where the finger's own ray misses it, still takes it (the finger target)", KeyGone(keyC), "tap at " + nearKey);
            }
            else Note("keys", "no off-key tap point near " + keyC.name + " (finger-target case skipped)");
        }
        else Note("keys", "no third key for the walk-over and finger-target cases (skipped)");

        // ------------------------------------------------------------ USE: glass
        // Stand as the desktop autopilot's glass scenario does: 0.9 m in front of the nearest intact pane,
        // facing it, aimed 1.3 m up (an oblique stand lets the glass shot's step-in swing the aim off the pane).
        var window = Map.NearestIntactWindowForTools(PlayerRoot.position);
        if (window != null && window.root != null && window.pane != null)
        {
            var root = window.root;
            var feet = root.position - root.forward * .9f;
            feet.y = PlayerRoot.position.y;
            Teleport(feet, Quaternion.LookRotation(root.forward, Vector3.up).eulerAngles.y, Mathf.Atan2(FrontRooms.Map.ModuleUnits.PlayerEye - 1.3f, .9f) * Mathf.Rad2Deg);
            Note("stand", "window " + window.a + "-" + window.b + " at 0.9 m, facing it");
            yield return Frames(.35f);
            Check("USE turns into BREAK on glass", controls.CurrentUsePrompt.Kind == FrontRoomsTouchControls.UseKind.Hold, controls.CurrentUsePrompt.Kind.ToString());
            yield return Capture("19_glass_ready");
            var heard = haptics.Count;
            Send(10, L.UseCenter, InputTouchPhase.Began);
            var brokeAt = -1;
            float heldAlpha = -1f, heldTakeover = -1f;
            for (var i = 0; i < 150 && brokeAt < 0; i++)
            {
                yield return null;
                if (i == 36) { heldAlpha = UseAlpha(); heldTakeover = controls.Takeover; }
                if (i == 18 || i == 36 || i == 50) yield return Capture("20_glass_hold_f" + i.ToString("000"));
                if (window.pane == null) brokeAt = i;
            }
            Send(10, L.UseCenter, InputTouchPhase.Ended);
            Check("glass: USE stays under the holding thumb through the takeover", heldAlpha > .9f,
                "USE alpha " + F(heldAlpha) + " at takeover " + F(heldTakeover));
            Check("holding USE breaks the glass", brokeAt >= 0, brokeAt >= 0 ? F(brokeAt * Step) + " s" : "progress " + F(controls.CurrentUsePrompt.Progress));
            var felt = haptics.GetRange(heard, haptics.Count - heard);
            Check("glass: rising buzz, two cracks and a heavy hit", felt.Exists(h => h.Contains("Gameplay,Light")) && felt.FindAll(h => h.Contains("Gameplay,Medium")).Count >= 2 && felt.Exists(h => h.Contains("Gameplay,Heavy")), string.Join(" | ", felt.GetRange(Mathf.Max(0, felt.Count - 6), Mathf.Min(6, felt.Count))));
            yield return Frames(.6f);
            yield return Capture("21_glass_broken");
            var flashCard = HudNode("Context panel");
            var flashText = flashCard != null ? flashCard.Find("Context") : null;
            var flashLine = flashText != null ? flashText.GetComponent<UnityEngine.UI.Text>() : null;
            Check("HUD: on phones the glass flash line is paper with the keyline, no dark card",
                flashCard != null && flashLine != null && !flashCard.GetComponent<UnityEngine.UI.Image>().enabled && flashText.GetComponent<UnityEngine.UI.Outline>() != null
                && flashCard.Find("Mobile hint rule") == null && flashLine.text.Length > 0,
                flashLine != null ? "'" + flashLine.text + "', card " + flashCard.GetComponent<UnityEngine.UI.Image>().enabled + ", keyline " + (flashText.GetComponent<UnityEngine.UI.Outline>() != null) : "missing");
            // Two captions at once during the glass flash (平面视觉's tightest case): the two-line card (50 tall) ends at least
            // 7 pt above the flash line (below ~8 the flash reads as a third caption line), which sits on its slot's bottom edge 29 up.
            var captionsWas = FrontRoomsSettings.Captions;
            FrontRoomsSettings.SetCaptions(true);
            var aheadYaw = Quaternion.Euler(0f, GetFloat("yaw"), 0f);
            FrontRoomsCaptions.Post("STEPS", PlayerRoot.position - aheadYaw * Vector3.forward * 3f);
            FrontRoomsCaptions.Post("DOOR", PlayerRoot.position - aheadYaw * Vector3.right * 4f);
            yield return Frames(.3f);
            yield return Capture("21c_flash_two_captions");
            var hudH2 = HudHeight();
            var card2 = HudRect("HUD / Captions");
            var flashBox = HudRect("Context");
            var flashTop = flashBox.HasValue && flashLine != null ? flashBox.Value.yMin + flashLine.preferredHeight / Mathf.Max(1f, hudH2) : 1f;
            var clear2 = card2.HasValue ? (card2.Value.yMin - flashTop) * hudH2 : -99f;
            Check("HUD: two captions during the glass flash, the 50 pt card ends at least 7 pt above the flash line",
                card2.HasValue && Mathf.Abs(card2.Value.height * hudH2 - 50f) < .75f && clear2 >= 7f && flashLine != null && flashLine.text.Length > 0,
                card2.HasValue ? "card " + F(card2.Value.height * hudH2) + " tall, " + F(clear2) + " pt above the flash text ('" + (flashLine != null ? flashLine.text.Replace("\n", " / ") : "") + "')" : "missing");
            FrontRoomsCaptions.Clear();
            FrontRoomsSettings.SetCaptions(captionsWas);
        }
        else Note("glass", "no intact window on the map (skipped)");

        // EXAMINE on touch (contract T1–T5, AT-06 §4 1–3 and 7).
        yield return ExamineChecks(L);

        // ------------------------------------------------------- station (A26, 关卡设计's KIT_NOTE item 2)
        // Before the Relay's real release below: the key's take (M6) sends its first leg while it is still held.
        yield return StationChecks(L);

        // ------------------------------------------------------- pads and keyboards (Red, 2026-10-29)
        yield return ExternalInputChecks(L);

        // X2 check 12, run last: the real release. The Relay is let go and the hook must see it leave Dormant.
        var hunter = game.GetType().GetField("relay", Any)?.GetValue(game) as FrontRoomsMapHunter;
        if (hunter != null)
        {
            game.GetType().GetField("mobileRelayEverReleased", Any).SetValue(game, false);
            hunter.GetType().GetField("caught", Any)?.SetValue(hunter, false);
            var released = -1;
            for (var i = 0; i < 600 && released < 0; i++) { yield return null; if (controls.Host != null && controls.Host.RelayEverReleased) released = i; }
            hunter.HeldForTools = true;
            if (released >= 0) Check("X2: the Relay's real release sets RelayEverReleased", true, F(released * Step) + " s after it was let go");
            else Note("X2", "the Relay did not leave Dormant within 10 s of being let go (its clock needs the start door shut behind the player); the seam covered check 5");
        }

        // ------------------------------------------------------------- caught
        game.GetType().GetMethod("End", Any).Invoke(game, null);
        for (var i = 0; i < 70; i++)
        {
            yield return null;
            if (i == 2 || i == 12 || i == 24 || i == 40 || i == 69) yield return Capture("22_caught_f" + i.ToString("00"));
            if (i == 18)
            {
                // Too early: TRY AGAIN ignores taps for 0.6 s.
                controls.TryGetChip(FrontRoomsTouchControls.Button.TryAgain, out var early, out _);
                Send(11, early, InputTouchPhase.Began);
            }
            if (i == 20) Send(11, L.Center + new Vector2(0f, -99f), InputTouchPhase.Ended);
            // Android Back is TRY AGAIN too, behind the same 0.6 s guard.
            if (i == 24) FrontRoomsMobileBackBridge.SimulateBackPressed();
        }
        Check("caught: an early tap or Back does not restart", Phase() == "Caught", Phase());
        Check("caught: heavy fade haptic", haptics.Exists(h => h.Contains("Gameplay,Heavy")), "");
        // Let the card settle (the subtitle fades in 0.16 s after CAUGHT, over 0.26 s) before reading it.
        yield return Frames(.6f);
        CaughtCardChecks(L);

        // ------------------------------------------------ reduce motion pass
        FrontRoomsHandheld.EditorReduceMotion = true;
        Note("reduce-motion", "on");
        var reloaded = false;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, m) => reloaded = true;
        FrontRoomsMobileBackBridge.SimulateBackPressed();
        yield return WaitFor(() => reloaded, 8f, "Back at caught (after 0.6 s) retries the run");
        if (aborted) yield break;
        game = null;
        yield return WaitFor(() => (game = FindFirstObjectByType<FrontRooms3DGame>()) != null && (controls = game.GetComponent<FrontRoomsTouchControls>()) != null, 10f, "new run's touch layer");
        if (aborted) yield break;
        converted.Clear();
        SetupCapture();
        yield return Frames(1f);
        // A retry skips the title and goes straight back into play.
        yield return WaitFor(() => Phase() == "Playing", 8f, "the retry goes straight back into play");
        if (aborted) yield break;
        yield return Frames(2f);
        var reducedY = new float[40];
        yield return TapWithFrames(controls.Layout.PauseCenter, 13, "23_reduced_pause", new[] { 1, 4, 8, 30 }, i => reducedY[i] = NodeY("Chip RESUME"));
        Check("reduce motion: paused", Phase() == "Paused", Phase());
        Check("reduce motion: the chips fade in place (no rise)", Mathf.Abs(reducedY[4] - reducedY[30]) < .01f && Mathf.Abs(reducedY[8] - reducedY[30]) < .01f, "f04 " + F(reducedY[4]) + " f30 " + F(reducedY[30]));
        FrontRoomsHandheld.EditorReduceMotion = false;

        // ---------------------------------------------- END RUN (Red, 2026-10-30: "在回退标题前不用确认一下")
        // Last, because it ends the run: from the pause, END RUN goes back to the title at once (no question), and the
        // title waits for its tap; a retry would go straight back into play.
        yield return Frames(.4f);
        var toTitle = false;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, m) => toTitle = true;
        // No frames on this tap: the scene reloads under it, so there is no camera to capture.
        yield return ChipTap(FrontRoomsTouchControls.Button.Restart, null, null);
        yield return WaitFor(() => toTitle, 8f, "END RUN goes back to the title at once, with no question");
        if (aborted) yield break;
        game = null;
        yield return WaitFor(() => (game = FindFirstObjectByType<FrontRooms3DGame>()) != null, 10f, "the title's game");
        if (aborted) yield break;
        yield return Frames(1.5f);
        Check("END RUN: back on the title, waiting for a tap (not a retry)", Phase() == "Title", Phase());
    }

    // ================================================================ pads and keyboards

    /// <summary>
    /// A pad or a keyboard on a phone (Red, 2026-10-29: "在手机/ipad上都可以用手柄/键盘游玩，如果玩家用手机玩的话虚拟摇杆就不会显示"):
    /// the first stick tilt or key hides the touch controls and the player walks (a small tilt is the slow gait); the
    /// prompt names the pad's button; MENU opens the sheet with its first row in focus, the d-pad moves it, RB changes tab,
    /// up past the first row is RESUME, B resumes; a touch on the move side brings the controls back. A keyboard: a key
    /// switches, W walks, the prompt keeps E.
    /// </summary>
    IEnumerator ExternalInputChecks(FrontRoomsTouchLayout L)
    {
        var view = game.GetComponent<FrontRoomsTouchControlsView>();
        var group = view != null ? view.GetType().GetField("controlsGroup", Any)?.GetValue(view) as CanvasGroup : null;
        float ControlsAlpha() => group != null ? group.alpha : -1f;
        string Shown(string prompt) => (string)game.GetType().GetMethod("DisplayPrompt", Any).Invoke(game, new object[] { prompt });
        FaceOpenWay("pad");
        yield return Frames(.4f);
        var pad = InputSystem.AddDevice<Gamepad>("Playtest Gamepad");
        try
        {
            var before = PlayerPos();
            InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = new Vector2(0f, 1f) });
            yield return Frames(1f);
            Check("pad: a stick tilt switches the phone to pad play", controls.Mode == FrontRoomsTouchControls.InputMode.Gamepad, controls.Mode.ToString());
            Check("pad: the stick, USE, RUN and pause fade out", ControlsAlpha() >= 0f && ControlsAlpha() < .05f, F(ControlsAlpha()));
            var walked = (PlayerPos() - before).magnitude;
            Check("pad: the left stick at full tilt walks (3.2 m/s)", walked > 2f && walked < 4.2f, F(walked) + " m in 1 s");
            FaceOpenWay("pad slow");
            before = PlayerPos();
            InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = new Vector2(0f, .4f) });
            yield return Frames(1f);
            var crept = (PlayerPos() - before).magnitude;
            Check("pad: a small tilt is the slow, unheard gait", FrontRooms3DGame.PlayerGait == FrontRoomsGait.Slow, FrontRooms3DGame.PlayerGait.ToString());
            // Red 2026-10-30 (三端保持一致): the touch stick's slow speed, not the tilt's share of the walk.
            Check("pad: the slow gait goes the touch stick's slow speed (1.44 m/s)", crept > 1f && crept < 1.9f, F(crept) + " m in 1 s");
            InputSystem.QueueStateEvent(pad, new GamepadState());
            yield return Frames(.2f);
            var south = FrontRoomsInput.PadName(FrontRoomsInput.PadButton.South);
            Check("pad: the prompt names the pad's button", Shown("E  ·  OPEN DOOR") == south + "  ·  OPEN DOOR" && Shown("HOLD E  ·  BREAK GLASS") == "HOLD " + south + "  ·  BREAK GLASS",
                Shown("E  ·  OPEN DOOR") + " | " + Shown("HOLD E  ·  BREAK GLASS"));
            yield return Capture("30a_pad_play");
            yield return PadPress(pad, GamepadButton.Start);
            yield return Frames(.5f);
            Check("pad: MENU opens the settings sheet", controls.CurrentMenuState == FrontRoomsTouchControls.MenuState.Settings, controls.CurrentMenuState.ToString());
            var first = controls.ActiveSettingsRow;
            Check("pad: the first row has the focus (its yellow rule)", first >= 0, first.ToString());
            yield return PadPress(pad, GamepadButton.DpadDown);
            yield return Frames(.2f);
            Check("pad: the d-pad moves the focus down", controls.ActiveSettingsRow >= 0 && controls.ActiveSettingsRow != first, first + " -> " + controls.ActiveSettingsRow);
            var tab = controls.SettingsTab;
            yield return PadPress(pad, GamepadButton.RightShoulder);
            yield return Frames(.2f);
            Check("pad: RB changes tab", controls.SettingsTab != tab, tab + " -> " + controls.SettingsTab);
            yield return Capture("30b_pad_settings_focus");
            for (var i = 0; i < 12 && controls.FocusChip == FrontRoomsTouchControls.Button.None; i++) { yield return PadPress(pad, GamepadButton.DpadUp); yield return Frames(.05f); }
            Check("pad: up past the first row focuses RESUME", controls.FocusChip == FrontRoomsTouchControls.Button.SettingsClose, controls.FocusChip.ToString());
            yield return Capture("30c_pad_resume_focus");
            yield return PadPress(pad, GamepadButton.B);
            yield return Frames(.6f);
            Check("pad: B resumes the run", controls.CurrentMenuState == FrontRoomsTouchControls.MenuState.Playing, controls.CurrentMenuState.ToString());
            Send(61, L.StickRest, InputTouchPhase.Began);
            yield return null;
            Send(61, L.StickRest, InputTouchPhase.Ended);
            yield return Frames(.4f);
            Check("touch: a touch on the move side brings the touch controls back", controls.Mode == FrontRoomsTouchControls.InputMode.Touch && ControlsAlpha() > .95f,
                controls.Mode + ", alpha " + F(ControlsAlpha()));
        }
        finally { InputSystem.RemoveDevice(pad); }

        var keyboard = InputSystem.AddDevice<Keyboard>("Playtest Keyboard");
        try
        {
            FaceOpenWay("keyboard");
            var before = PlayerPos();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            yield return Frames(1f);
            Check("keyboard: a key switches to keyboard play and hides the touch controls", controls.Mode == FrontRoomsTouchControls.InputMode.Keyboard && ControlsAlpha() < .05f,
                controls.Mode + ", alpha " + F(ControlsAlpha()));
            var walked = (PlayerPos() - before).magnitude;
            Check("keyboard: W walks", walked > 2f, F(walked) + " m in 1 s");
            Check("keyboard: the prompt keeps E", Shown("E  ·  OPEN DOOR") == "E  ·  OPEN DOOR", Shown("E  ·  OPEN DOOR"));
            // Red 2026-10-30: hold Shift to walk slowly ("在电脑版慢走的逻辑是按下shift触发"), hold Ctrl to run ("按住 Ctrl 奔跑"),
            // and the run only ahead ("奔跑时目前可以只能向前奔跑"), as on a Mac.
            game.GetType().GetField("stamina", Any).SetValue(game, 5f);
            game.GetType().GetField("winded", Any).SetValue(game, false);
            FaceOpenWay("keyboard slow");
            before = PlayerPos();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.LeftShift));
            yield return Frames(1f);
            var crept = (PlayerPos() - before).magnitude;
            Check("keyboard: Shift + W is the slow, unheard gait (1.44 m/s)", FrontRooms3DGame.PlayerGait == FrontRoomsGait.Slow && crept > 1f && crept < 1.9f,
                FrontRooms3DGame.PlayerGait + ", " + F(crept) + " m in 1 s");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A, Key.LeftCtrl));
            yield return Frames(.4f);
            Check("keyboard: Ctrl + A (a strafe) walks, never runs", FrontRooms3DGame.PlayerGait == FrontRoomsGait.Walk && !FrontRooms3DGame.PlayerSprinting, FrontRooms3DGame.PlayerGait.ToString());
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return Frames(.2f);
            game.GetType().GetField("stamina", Any).SetValue(game, 5f);
            FaceOpenWay("keyboard run");
            before = PlayerPos();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.LeftCtrl));
            yield return Frames(.5f);
            var ran = (PlayerPos() - before).magnitude;
            Check("keyboard: Ctrl + W runs ahead (5.5 m/s)", FrontRooms3DGame.PlayerGait == FrontRoomsGait.Run && ran > 2f, FrontRooms3DGame.PlayerGait + ", " + F(ran) + " m in 0.5 s");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return Frames(.2f);
            Send(62, L.StickRest, InputTouchPhase.Began);
            yield return null;
            Send(62, L.StickRest, InputTouchPhase.Ended);
            yield return Frames(.4f);
            Check("touch: back to touch after the keyboard", controls.Mode == FrontRoomsTouchControls.InputMode.Touch, controls.Mode.ToString());
        }
        finally { InputSystem.RemoveDevice(keyboard); }
    }

    IEnumerator PadPress(Gamepad pad, GamepadButton button)
    {
        InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(button));
        yield return null;
        yield return null;
        InputSystem.QueueStateEvent(pad, new GamepadState());
        yield return null;
    }

    // ================================================================ helpers

    string Phase() => game.GetType().GetField("phase", Any).GetValue(game).ToString();
    float GetFloat(string field) => (float)game.GetType().GetField(field, Any).GetValue(game);

    /// <summary>
    /// Turns the player toward the longest free walk (16 headings, cast with the body's own capsule), so a creep check
    /// is not stopped by a wall a metre away: a blocked creep rightly never counts.
    /// </summary>
    void FaceOpenWay(string why)
    {
        var body = (CharacterController)game.GetType().GetField("playerBody", Any).GetValue(game);
        var centre = body.transform.TransformPoint(body.center);
        var half = Mathf.Max(0f, body.height * .5f - body.radius);
        var bottom = centre - Vector3.up * (half - .05f);
        var top = centre + Vector3.up * half;
        float bestYaw = GetFloat("yaw"), best = -1f;
        for (var k = 0; k < 16; k++)
        {
            var yaw = k * 22.5f;
            var dir = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            var free = Physics.CapsuleCast(bottom, top, body.radius * .95f, dir, out var hit, 8f, ~0, QueryTriggerInteraction.Ignore) ? hit.distance : 8f;
            if (free > best) { best = free; bestYaw = yaw; }
        }
        game.GetType().GetField("yaw", Any).SetValue(game, bestYaw);
        Note("X2", why + ": faced the longest free walk, " + F(best) + " m");
    }
    bool GetBool(string field) => (bool)game.GetType().GetField(field, Any).GetValue(game);
    Transform PlayerRoot => (Transform)game.GetType().GetField("playerRoot", Any).GetValue(game);
    Vector3 PlayerPos() { var p = PlayerRoot.position; p.y = 0f; return p; }
    FrontRoomsMapWorld Map => (FrontRoomsMapWorld)game.GetType().GetField("map", Any).GetValue(game);
    string Describe(Collider c, out bool hold) { hold = false; return c == null || Map == null ? null : Map.Describe(c, out hold); }

    /// <summary>
    /// Where to tap a settings row: on a window whose list runs past the bottom edge (one column on the Duo's outer
    /// display), the list is scrolled first so the row sits fully above the home inset, as a player would drag it.
    /// </summary>
    Vector2 RowInView(FrontRoomsTouchControls.SettingsSlot s)
    {
        var rect = s.Rect;
        if (!controls.SettingsSlotScrolls(s)) return rect.center;
        var vp = controls.SettingsScrollViewport;
        var scroll = controls.SettingsScroll;
        if (rect.yMin + scroll < controls.Layout.SettingsBottomInset || rect.yMax + scroll > vp.yMax)
        {
            scroll = Mathf.Clamp(vp.yMax - 8f - rect.yMax, 0f, controls.SettingsScrollMax);
            SetSettingsScroll(scroll);
        }
        return rect.center + new Vector2(0f, scroll);
    }

    void SetSettingsScroll(float value) =>
        typeof(FrontRoomsTouchControls).GetProperty("SettingsScroll").GetSetMethod(true).Invoke(controls, new object[] { value });

    FrontRoomsTouchControls.SettingsSlot? FindRow(string label)
    {
        foreach (var s in controls.SettingsSlots) if (!s.Header && !s.Detail && s.Label == label) return s;
        return null;
    }

    /// <summary>The centre of a segment of a row's control, scrolled into view (the touch metrics, against the row's right edge).</summary>
    Vector2 SegmentInView(FrontRoomsTouchControls.SettingsSlot s, int index)
    {
        var c = RowInView(s);
        var row = s.Row;
        var rect = s.Rect;
        rect.y = c.y - rect.height * .5f;
        var right = FrontRoomsHandheld.ClearRect(rect, true).xMax;
        var m = FrontRoomsSettingsMenu.Touch;
        var x = right - FrontRoomsSettingsMenu.ControlWidth(row, m);
        for (var i = 0; i < index; i++) x += FrontRoomsSettingsMenu.CellWidth(row, i, m);
        return new Vector2(x + FrontRoomsSettingsMenu.CellWidth(row, index, m) * .5f, c.y);
    }

    /// <summary>Where a slider's stop sits on its track, scrolled into view.</summary>
    Vector2 StopInView(FrontRoomsTouchControls.SettingsSlot s, int index)
    {
        var c = RowInView(s);
        var rect = s.Rect;
        rect.y = c.y - rect.height * .5f;
        var right = FrontRoomsHandheld.ClearRect(rect, true).xMax;
        FrontRoomsSettingsMenu.TrackSpan(FrontRoomsSettingsMenu.Touch, out var low, out _);
        var i = Mathf.Clamp(index, 0, s.Row.Stops.Length - 1);
        return new Vector2(right - low + FrontRoomsSettingsMenu.Touch.Track * FrontRoomsSettingsMenu.StopPosition(s.Row, i), c.y);
    }

    /// <summary>Stands the player in front of the nearest collider that <paramref name="want"/> accepts, looking at it.</summary>
    bool StandAt(IEnumerable<Collider> candidates, Func<Collider, bool> want, float distance, out Collider target)
    {
        target = null;
        var feet = PlayerRoot.position;
        var found = new List<Collider>();
        var seen = 0;
        foreach (var c in candidates)
        {
            seen++;
            if (c != null && c.enabled && c.gameObject.activeInHierarchy && want(c)) found.Add(c);
        }
        Note("candidates", found.Count + " of " + seen + (GetBool("inStartRooms") ? " (player in the start rooms)" : ""));
        found.Sort((a, b) => (a.bounds.center - feet).sqrMagnitude.CompareTo((b.bounds.center - feet).sqrMagnitude));
        for (var n = 0; n < found.Count && n < 40; n++)
            if (PlaceAt(found[n], distance, feet)) { target = found[n]; return true; }
        return false;
    }

    /// <summary>
    /// StandAt for a thin target (a hanging door leaf): each spot is cleared with the controller's own radius, the body
    /// settles for two frames, and the game's own crosshair ray (its eye and AimCast) must land on the target. One
    /// re-aim from the real eye, else the next spot or candidate. 关卡设计's C22 run (2026-10-22) stood beside a leaf
    /// seen near edge-on, and the controller (r 0.30) could be pushed after the aim was set.
    /// </summary>
    /// <param name="hold">When above 0, a spot counts only if the crosshair is still on the target after this many seconds:
    /// a body set down against a door frame is pushed out over the next frames and the view drifts (run r2, 2026-10-24:
    /// the broken leaf, edge-on, was under the crosshair at the stand and gone 0.35 s later).</param>
    IEnumerator StandAtSeen(IEnumerable<Collider> candidates, Func<Collider, bool> want, float distance, Action<Collider> found, float hold = 0f)
    {
        var feet = PlayerRoot.position;
        var list = new List<Collider>();
        foreach (var c in candidates)
            if (c != null && c.enabled && c.gameObject.activeInHierarchy && want(c)) list.Add(c);
        list.Sort((a, b) => (a.bounds.center - feet).sqrMagnitude.CompareTo((b.bounds.center - feet).sqrMagnitude));
        var body = (CharacterController)game.GetType().GetField("playerBody", Any).GetValue(game);
        var radius = body.radius + .02f;
        var lost = 0;
        for (var n = 0; n < list.Count && n < 12; n++)
        for (var k = 0; k < 16; k++)
        {
            if (!PlaceAt(list[n], distance, feet, k, radius)) continue;
            yield return null;
            yield return null;
            var reaimed = false;
            if (!CrosshairOn(list[n]))
            {
                AimAt(list[n].bounds.center);
                yield return null;
                if (!CrosshairOn(list[n])) continue;
                reaimed = true;
            }
            if (hold > 0f)
            {
                yield return Frames(hold);
                if (!CrosshairOn(list[n])) { lost++; continue; }
            }
            found(list[n]);
            if (reaimed) Note("stand", "re-aimed from the eye");
            if (lost > 0) Note("stand", lost + " spot(s) lost the target within " + F(hold) + " s");
            yield break;
        }
        Note("stand", "no spot where the crosshair lands on " + (list.Count > 0 ? list[0].name : "the target") + (lost > 0 ? " and stays (" + lost + " lost within " + F(hold) + " s)" : ""));
    }

    // ------------------------------------------------------------ station (A26; 关卡设计's KIT_NOTE item 2, 2026-10-29)

    /// <summary>
    /// Floor 1's station, the first room (A26, when the profile's station module is on): the stick walks the 1.2 m
    /// centre aisle and through E1 (1.70 m) and E2 (1.20 m) at each gait, the run line names the room FRONT ROOMS, and a
    /// tap on the key at the KeySpot, from the take point, takes it. Room metres are from the station's south-west
    /// corner (research/station/70_station_look_v1_plan.md §2, §14.3).
    /// </summary>
    IEnumerator StationChecks(FrontRoomsTouchLayout L)
    {
        var map = Map;
        if (map == null || !map.HasStation) { Note("station", "no station on this floor (the profile's module is off): skipped"); yield break; }
        for (var i = 0; i < 300 && !map.StationReady; i++) yield return null;
        var rect = map.StationRect;
        const float cs = FrontRooms.Map.MapGrid.CellSize;
        Vector3 Room(float x, float z) => map.transform.TransformPoint(new Vector3(rect.x * cs + x, 0f, rect.y * cs + z));
        Vector2 Local(Vector3 world) { var l = map.transform.InverseTransformPoint(world); return new Vector2(l.x - rect.x * cs, l.z - rect.y * cs); }
        float Yaw(Vector3 mapDirection) { var d = map.transform.TransformDirection(mapDirection); return Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg; }
        Note("station", "cells " + rect + ", props ready " + map.StationReady);
        var gaits = new[] { FrontRoomsGait.Slow, FrontRoomsGait.Walk, FrontRoomsGait.Run };

        // The centre aisle (x 6.9–8.1) from past D's sweep, then E1 (x 6.65–8.35) to 1 m past the north wall.
        foreach (var gait in gaits)
        {
            Teleport(Room(7.5f, 1.9f) + Vector3.up * .05f, Yaw(Vector3.forward), 0f);
            yield return Frames(.4f);
            if (gait == FrontRoomsGait.Slow)
            {
                var meta = game.GetType().GetProperty("PauseMeta", Any)?.GetValue(game) as string;
                Check("station: the run line names it FRONT ROOMS (InFrontRooms)", meta != null && meta.Contains("FRONT ROOMS"), meta ?? "-");
            }
            var drift = 0f;
            var run = new StationRun();
            yield return HoldGait(L, gait, 11.1f, () => { var p = Local(PlayerRoot.position); drift = Mathf.Max(drift, Mathf.Abs(p.x - 7.5f)); return p.y >= 13f; }, run);
            var end = Local(PlayerRoot.position);
            Check("station: at " + gait + " the stick walks the 1.2 m centre aisle and through E1 (1.70 m)", run.Arrived && run.Gait == gait && drift < .3f,
                (run.Arrived ? "1 m past E1 in " + F(run.Seconds) + " s" : "stopped at room (" + F(end.x) + ", " + F(end.y) + ")") + ", gait " + run.Gait + ", off the axis " + F(drift) + " m");
        }

        // E2 (z 3.90–5.10) in the west wall, from 2.4 m inside to 1 m past it.
        foreach (var gait in gaits)
        {
            Teleport(Room(2.4f, 4.5f) + Vector3.up * .05f, Yaw(Vector3.left), 0f);
            yield return Frames(.4f);
            var drift = 0f;
            var run = new StationRun();
            yield return HoldGait(L, gait, 3.4f, () => { var p = Local(PlayerRoot.position); drift = Mathf.Max(drift, Mathf.Abs(p.y - 4.5f)); return p.x <= -1f; }, run);
            var end = Local(PlayerRoot.position);
            Check("station: at " + gait + " the stick goes through E2 (1.20 m)", run.Arrived && run.Gait == gait && drift < .3f,
                (run.Arrived ? "1 m past the wall in " + F(run.Seconds) + " s" : "stopped at room (" + F(end.x) + ", " + F(end.y) + ")") + ", gait " + run.Gait + ", off the axis " + F(drift) + " m");
        }

        // The floor key at the KeySpot, from the take point (9.73, 10.40): within 0.9 m, and a tap on it takes it.
        Collider key = null;
        var spot = Vector3.zero;
        if (map.StationAnchor(FrontRooms.Map.ModuleMarkerKind.KeySpot, out spot, out _, out _))
            foreach (var c in MapColliders("keyByCollider"))
                if (c != null && (c.bounds.center - spot).magnitude < .6f && (key == null || (c.bounds.center - spot).sqrMagnitude < (key.bounds.center - spot).sqrMagnitude)) key = c;
        if (key == null) { Check("station: the floor key hangs at the KeySpot", false, "no key within 0.6 m of the KeySpot " + spot.ToString("F2")); yield break; }
        var take = Room(9.73f, 10.40f);
        var flat = Vector2.Distance(new Vector2(take.x, take.z), new Vector2(key.bounds.center.x, key.bounds.center.z));
        Check("station: the key is within 0.9 m of the take point", flat <= .9f, F(flat) + " m");
        Teleport(take + Vector3.up * .05f, Yaw(Vector3.right), 0f);
        yield return Frames(.4f);
        AimAt(key.bounds.center);
        yield return null;
        TurnBy(-14f);
        yield return Frames(.3f);
        var off = 0f;
        if (KeyTapPoint(key, ref off, out var tapAt))
        {
            yield return Tap(tapAt, 62);
            yield return Frames(.5f);
            Check("station: a tap on the key at the KeySpot takes it (crosshair elsewhere)", KeyGone(key) && map.StationKeyIsTaken,
                "gone " + KeyGone(key) + ", StationKeyIsTaken " + map.StationKeyIsTaken + ", tap at " + V(tapAt));
        }
        else
        {
            AimAt(key.bounds.center);
            yield return Frames(.35f);
            var prompt = controls.CurrentUsePrompt;
            Send(62, L.UseCenter, InputTouchPhase.Began);
            for (var i = 0; i < 4; i++) yield return null;
            Send(62, L.UseCenter, InputTouchPhase.Ended);
            yield return Frames(.5f);
            Check("station: USE takes the key at the KeySpot (no tap point in the look zone)", KeyGone(key) && map.StationKeyIsTaken,
                "prompt " + prompt.Kind + " '" + prompt.Label + "', gone " + KeyGone(key) + ", StationKeyIsTaken " + map.StationKeyIsTaken);
        }
        yield return Capture("26_station_key");
    }

    sealed class StationRun { public bool Arrived; public float Seconds; public FrontRoomsGait Gait; }

    /// <summary>
    /// Holds the stick straight ahead at <paramref name="gait"/> (slow near the centre, walk at the ring, run past the
    /// run line, as the stick section does) until <paramref name="arrived"/>, for at most <paramref name="metres"/> at
    /// that gait's speed × 1.6 + 1 s; then lets go and settles. Reports the gait the game saw 0.2 s in.
    /// </summary>
    IEnumerator HoldGait(FrontRoomsTouchLayout L, FrontRoomsGait gait, float metres, Func<bool> arrived, StationRun run)
    {
        var origin = L.StickRest + new Vector2(40f, 10f);
        var reach = origin + new Vector2(0f, gait == FrontRoomsGait.Slow ? 22f * L.Scale : gait == FrontRoomsGait.Walk ? 50f * L.Scale : L.RunLineRadius + 10f);
        var speed = gait == FrontRoomsGait.Slow ? 1.44f : gait == FrontRoomsGait.Walk ? 3.2f : 5.5f;
        var frames = Mathf.RoundToInt((metres / speed * 1.6f + 1f) / Step);
        Send(61, origin, InputTouchPhase.Began);
        yield return null;
        yield return Drag(61, origin, reach, .12f);
        var i = 0;
        for (; i < frames; i++)
        {
            if (arrived()) { run.Arrived = true; break; }
            if (i == 12) run.Gait = FrontRooms3DGame.PlayerGait;
            yield return null;
        }
        if (i <= 12) run.Gait = FrontRooms3DGame.PlayerGait;
        run.Seconds = i * Step;
        Send(61, reach, InputTouchPhase.Ended);
        yield return Frames(.3f);
    }

    // ------------------------------------------------------------ EXAMINE (touch_inspect.md T1–T5)

    object GamePhase => game.GetType().GetField("phase", Any)?.GetValue(game);
    bool Inspecting => GamePhase != null && GamePhase.ToString() == "Inspect";

    /// <summary>
    /// Stands 1 m in front of the nearest close-up target, turned so it sits in the look zone, and returns the touch point
    /// over its face (null when there is no target or no floor).
    /// </summary>
    FrontRoomsInspectable examineTarget;
    Vector2? examineTap;

    /// <summary>
    /// Stands 1 m in front of the nearest close-up target (or <paramref name="keep"/>), square to its face and turned
    /// left by <paramref name="turn"/> degrees, lets the camera settle for two frames, then reads where the face lands on
    /// screen: <see cref="examineTarget"/> and <see cref="examineTap"/> (a point in the look zone), or nulls. Run e1
    /// (2026-10-22) read the screen point in the teleport's own frame, before the camera moved, and skipped every target.
    /// </summary>
    IEnumerator StandAtInspectableSeen(float turn = 12f, FrontRoomsInspectable keep = null, Func<FrontRoomsInspectable, bool> only = null)
    {
        examineTarget = null;
        examineTap = null;
        var all = new List<FrontRoomsInspectable>();
        if (keep != null) all.Add(keep);
        else FrontRoomsInspect.CopyTargets(all);
        if (only != null) all.RemoveAll(t => t == null || !only(t));
        var feet0 = PlayerRoot.position;
        all.Sort((a, b) => (a.Face.position - feet0).sqrMagnitude.CompareTo((b.Face.position - feet0).sqrMagnitude));
        var cam = (Camera)game.GetType().GetField("cam", Any).GetValue(game);
        var L = controls.Layout;
        int off = 0, noNormal = 0, noFloor = 0, behind = 0, outside = 0, tried = 0;
        var lastPoint = Vector2.zero;
        var lastWhy = "";
        foreach (var t in all)
        {
            if (t == null || !t.isActiveAndEnabled || (t.Available != null && !t.Available())) { off++; continue; }
            if (tried >= 12) break;
            var face = t.Face;
            var normal = Vector3.ProjectOnPlane(face.rotation * Vector3.forward, Vector3.up);
            if (normal.sqrMagnitude < .1f) { noNormal++; continue; }
            normal.Normalize();
            var stand = face.position + normal * 1f;
            // The floor under the stand, never the player's own capsule: a second stand at the same target found the
            // capsule's top at 1.54 m and lifted the player onto it (run f7).
            if (!FloorUnder(stand, out var feet)) { noFloor++; continue; }
            var eyeHeight = FrontRooms.Map.ModuleUnits.PlayerEye;
            // Aim at the aim box (what FrontRoomsInspect.Find tests), not the face anchor: the placard's box sits on the
            // kit's own origin (run f4: a ray through the face centre missed it).
            var aim = t.BoxCentreWorld;
            var pitch = Mathf.Atan2(feet.y + eyeHeight - aim.y, Vector3.ProjectOnPlane(aim - feet, Vector3.up).magnitude) * Mathf.Rad2Deg;
            // Turned off the face (12 degrees by default) so it lands right of centre, in the look zone; both ways are
            // tried, because the first run put the face outside it (run e2).
            foreach (var sign in turn > 0f ? new[] { 1f, -1f } : new[] { 0f })
            {
                var yaw = Quaternion.LookRotation(-normal, Vector3.up).eulerAngles.y - turn * sign;
                Teleport(feet, yaw, pitch);
                tried++;
                yield return null;
                yield return null;
                if (cam == null) yield break;
                // Viewport, not screen pixels: in batch mode the camera renders at the Editor's screen size, not the
                // forced profile's (run f2: the face read (85, 80) pt with the camera squarely on it).
                var vp = cam.WorldToViewportPoint(aim);
                if (vp.z <= 0f) { behind++; continue; }
                var pt = new Vector2(vp.x * L.Frame.x, vp.y * L.Frame.y);
                lastPoint = pt;
                lastWhy = t.Id + " box " + V3(aim) + ", cam " + V3(cam.transform.position);
                if (turn > 0f && !L.InLookZone(pt)) { outside++; continue; }
                examineTarget = t;
                examineTap = pt;
                yield break;
            }
        }
        Note("EXAMINE", "no stand: " + all.Count + " targets; off " + off + ", no normal " + noNormal + ", no floor " + noFloor + ", behind " + behind + ", outside the look zone " + outside + " (last face point " + V(lastPoint) + ", top band from y " + F(L.TopBandBottom) + ", mid x " + F(L.Frame.x * .5f) + "); " + lastWhy);
    }

    /// <summary>The floor point under <paramref name="stand"/> (a downward ray from 0.5 m above it, 3 m long), skipping the player.</summary>
    bool FloorUnder(Vector3 stand, out Vector3 feet)
    {
        feet = stand;
        var best = float.PositiveInfinity;
        foreach (var h in Physics.RaycastAll(stand + Vector3.up * .5f, Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (h.collider == null || h.collider.transform.IsChildOf(PlayerRoot) || h.distance >= best) continue;
            best = h.distance;
            feet = h.point;
        }
        return best < float.PositiveInfinity;
    }

    /// <summary>The game's tap-to-open gates for a tap at <paramref name="pt"/> (points), as HandleMobileLookTap runs them.</summary>
    string InspectGates(Vector2 pt, FrontRoomsInspectable want)
    {
        var cam = (Camera)game.GetType().GetField("cam", Any).GetValue(game);
        var L = controls.Layout;
        var ray = cam.ViewportPointToRay(new Vector3(pt.x / L.Frame.x, pt.y / L.Frame.y, 0f));
        var reach = 2.4f;
        var found = FrontRoomsInspect.Find(ray, reach, out var hit);
        var show = hit != null && (bool)game.GetType().GetMethod("CanShowInspect", Any).Invoke(game, new object[] { hit });
        var press = (bool)game.GetType().GetMethod("CanPressInspect", Any).Invoke(game, null);
        var rig = game.GetType().GetField("rig", Any)?.GetValue(game);
        var inShot = rig != null && (bool)rig.GetType().GetProperty("InShot", Any).GetValue(rig);
        return "Find " + found + " (" + (hit != null ? hit.Id : "-") + (hit == want ? ", the target" : "") + "), CanShow " + show + ", CanPress " + press + ", rig in shot " + inShot
            + ", useLatch " + game.GetType().GetField("useLatch", Any)?.GetValue(game) + ", cooldown " + game.GetType().GetField("inspectCooldown", Any)?.GetValue(game);
    }

    IEnumerator ExamineChecks(FrontRoomsTouchLayout L)
    {
        // A reading close-up (A43: one that pauses); the live side has its own checks at the end.
        yield return StandAtInspectableSeen(12f, null, t => t.Def != null && t.Def.Pauses);
        var target = examineTarget;
        var at = examineTap;
        if (!at.HasValue || target == null) { Note("EXAMINE", "no close-up target to stand at (skipped)"); yield break; }
        Note("EXAMINE", "target " + target.Id + " (" + target.Kind + ") at " + V(at.Value));
        yield return Frames(.4f);
        yield return StandAtInspectableSeen(12f, target);
        var tapAt = examineTap ?? at.Value;
        Note("EXAMINE", "before the tap: " + InspectGates(tapAt, target));
        // HUD key B touch contract T-2: when USE shows for a close-up target, its verb comes from the target's kind.
        var shown = controls.CurrentUsePrompt;
        var expected = target.Kind == InspectKind.Object ? FrontRoomsTouchControls.UseKind.Examine
            : target.Kit == FrontRoomsInspectCatalog.StationTvKit ? FrontRoomsTouchControls.UseKind.Watch : FrontRoomsTouchControls.UseKind.Read;
        Check("EXAMINE: USE names a close-up target by its kind (EXAMINE on an object, WATCH on the station TV, READ on a face)",
            !shown.Visible || shown.Kind == expected, target.Kind + " -> " + shown.Kind + " '" + shown.Label + "'");
        var heard = haptics.Count;
        yield return Tap(tapAt, 40);
        for (var i = 0; i < 40 && !Inspecting; i++) yield return null;
        Check("EXAMINE: a tap on the target opens the close-up", Inspecting && controls.CurrentMenuState == FrontRoomsTouchControls.MenuState.Inspect,
            GamePhase + " / " + controls.CurrentMenuState);
        if (!Inspecting) yield break;
        yield return Frames(.5f);
        yield return Capture("24_examine_open");
        var felt = haptics.GetRange(heard, haptics.Count - heard);
        Check("EXAMINE: the open is felt, one light tick", felt.Exists(h => h.Contains("Gameplay,Light")), string.Join(" | ", felt));
        var useNode = TouchNode("USE");
        Check("EXAMINE: USE is BACK, at the lower right", controls.CurrentUsePrompt.Kind == FrontRoomsTouchControls.UseKind.Back && UseAlpha() > .9f
            && useNode != null && (useNode.anchoredPosition - L.InspectBackCenter).magnitude < 1f,
            controls.CurrentUsePrompt.Kind + ", alpha " + F(UseAlpha()) + " at " + (useNode != null ? V(useNode.anchoredPosition) : "-") + " want " + V(L.InspectBackCenter));
        var view = game.GetComponent<FrontRoomsTouchControlsView>();
        var wash = view.GetType().GetField("washAlpha", Any)?.GetValue(view);
        var washNow = wash != null ? (float)wash.GetType().GetProperty("Current").GetValue(wash) : -1f;
        var pauseGroup = (CanvasGroup)view.GetType().GetField("pauseGroup", Any)?.GetValue(view);
        Check("EXAMINE: no frost over the close-up; pause and the stick are gone", washNow == 0f && pauseGroup != null && pauseGroup.alpha < .01f,
            "wash " + F(washNow) + ", pause alpha " + (pauseGroup != null ? F(pauseGroup.alpha) : "-"));

        // Pinch out on the look side: a continuous zoom, up to the step.
        var offered = FrontRoomsInspect.ZoomStep.offered;
        if (offered)
        {
            var a0 = new Vector2(L.Frame.x * .66f, L.Frame.y * .5f);
            var b0 = a0 + new Vector2(70f, 0f);
            Send(41, a0, InputTouchPhase.Began);
            yield return null;
            Send(42, b0, InputTouchPhase.Began);
            yield return null;
            var zoomMid = -1f;
            for (var i = 1; i <= 18; i++)
            {
                var spread = 100f * L.Scale * i / 18f;
                Send(41, a0 - new Vector2(spread, 0f), InputTouchPhase.Moved);
                Send(42, b0 + new Vector2(spread, 0f), InputTouchPhase.Moved);
                yield return null;
                if (i == 9) zoomMid = FrontRoomsInspect.ZoomLevel;
            }
            Send(41, a0 - new Vector2(100f * L.Scale, 0f), InputTouchPhase.Ended);
            Send(42, b0 + new Vector2(100f * L.Scale, 0f), InputTouchPhase.Ended);
            yield return Frames(.8f);
            var zoomEnd = FrontRoomsInspect.ZoomLevel;
            yield return Capture("24b_examine_pinched");
            Check("EXAMINE: a pinch out zooms in, continuously", zoomMid > .01f && zoomMid < zoomEnd && zoomEnd > .5f,
                "zoom " + F(zoomMid) + " half-way, " + F(zoomEnd) + " after");
            // A double tap on the look side toggles it back out.
            var dt = new Vector2(L.Frame.x * .7f, L.Frame.y * .45f);
            yield return Tap(dt, 43);
            yield return Frames(.08f);
            yield return Tap(dt, 44);
            yield return Frames(.8f);
            Check("EXAMINE: a double tap toggles the zoom", FrontRoomsInspect.ZoomLevel < .2f, "zoom " + F(FrontRoomsInspect.ZoomLevel));
        }
        else Note("EXAMINE", "this target offers no zoom step: pinch and double-tap checks skipped");

        // BACK leaves (after the 0.20 s guard).
        yield return Tap(L.InspectBackCenter, 45);
        for (var i = 0; i < 90 && Inspecting; i++) yield return null;
        Check("EXAMINE: BACK leaves the close-up", !Inspecting, GamePhase.ToString());
        yield return Frames(.8f);

        // The tap that opens a close-up never also zooms it: two quick taps on the target.
        yield return StandAtInspectableSeen(12f, target);
        tapAt = examineTap ?? tapAt;
        yield return Frames(.4f);
        yield return Tap(tapAt, 46);
        yield return Frames(.06f);
        yield return Tap(tapAt, 47);
        yield return Frames(.9f);
        Check("EXAMINE: a double tap on the target opens it without zooming it", Inspecting && FrontRoomsInspect.ZoomLevel < .05f,
            GamePhase + ", zoom " + F(FrontRoomsInspect.ZoomLevel));
        // The left stick's pull-back leaves too (the desktop S).
        if (Inspecting)
        {
            var o = L.StickRest + new Vector2(30f, 40f) * L.Scale;
            Send(48, o, InputTouchPhase.Began);
            yield return null;
            var deep = o - new Vector2(0f, L.ShotBackDepth + 20f * L.Scale);
            yield return Drag(48, o, deep, .12f);
            for (var i = 0; i < 90 && Inspecting; i++) yield return null;
            Check("EXAMINE: the left stick's pull-back leaves the close-up", !Inspecting, GamePhase.ToString());
            Check("EXAMINE: leaving a reading close-up latches the pull-back (C25): the finger does not walk on", !controls.StickTouched, "stick " + controls.StickTouched);
            Send(48, deep, InputTouchPhase.Ended);
            yield return Frames(.8f);
        }
        // Android Back leaves too.
        yield return StandAtInspectableSeen(12f, target);
        tapAt = examineTap ?? tapAt;
        yield return Frames(.4f);
        yield return Tap(tapAt, 49);
        for (var i = 0; i < 40 && !Inspecting; i++) yield return null;
        yield return Frames(.3f);
        if (Inspecting)
        {
            FrontRoomsMobileBackBridge.SimulateBackPressed();
            for (var i = 0; i < 90 && Inspecting; i++) yield return null;
            Check("EXAMINE: Android Back leaves the close-up", !Inspecting, GamePhase.ToString());
            yield return Frames(.8f);
        }
        else Note("EXAMINE", "could not reopen for the Android Back case");

        // READ (G10 / C14): square to the target, the crosshair on it, USE says READ, and its press opens the close-up.
        yield return StandAtInspectableSeen(0f, target);
        yield return Frames(.5f);
        var read = controls.CurrentUsePrompt.Kind == FrontRoomsTouchControls.UseKind.Read && UseAlpha() > .9f;
        yield return Capture("24c_examine_read");
        Check("EXAMINE: aimed at a readable, USE says READ", read, controls.CurrentUsePrompt.Kind + ", alpha " + F(UseAlpha()));
        if (read)
        {
            yield return Tap(L.UseCenter, 50);
            for (var i = 0; i < 40 && !Inspecting; i++) yield return null;
            Check("EXAMINE: READ opens the close-up", Inspecting, GamePhase + "");
            yield return Frames(.4f);
            if (Inspecting)
            {
                yield return Tap(L.InspectBackCenter, 51);
                for (var i = 0; i < 90 && Inspecting; i++) yield return null;
                yield return Frames(.8f);
            }
        }

        // T6/T7: the TEXT chip and the transcript panel (平面视觉 G9/G10).
        yield return ExamineTextChecks(L, target);
        // A43 + A44: a close-up that runs live.
        yield return LiveCloseUpChecks(L, target);
    }

    /// <summary>
    /// A44: the playing station TV's USE says WATCH. A43 (only careful reading pauses): a close-up that runs live keeps BACK,
    /// never offers TEXT and has no pause button, and the pull-back that leaves it is one move: the finger walks on backwards.
    /// At the station TV when it can be stood at, else at the reading target with its pause turned off.
    /// </summary>
    IEnumerator LiveCloseUpChecks(FrontRoomsTouchLayout L, FrontRoomsInspectable reading)
    {
        yield return StandAtInspectableSeen(0f, null, t => t.Kit == FrontRoomsInspectCatalog.StationTvKit);
        var live = examineTarget;
        var forced = live == null;
        var keptPause = reading.Def.pause;
        if (forced)
        {
            Note("A43", "no station TV to stand at: the reading target, its pause turned off");
            live = reading;
            live.Def.pause = InspectDef.PauseNever;
            yield return StandAtInspectableSeen(0f, live);
        }
        yield return Frames(.5f);
        var view = game.GetComponent<FrontRoomsTouchControlsView>();
        var shown = controls.CurrentUsePrompt;
        var word = (string)view.GetType().GetMethod("UseVerb", Any).Invoke(null, new object[] { shown });
        if (!forced)
        {
            yield return Capture("24f_watch_tv");
            Check("A44: aimed at the station TV, USE says WATCH", shown.Kind == FrontRoomsTouchControls.UseKind.Watch && word == "WATCH" && UseAlpha() > .9f,
                shown.Kind + " '" + word + "', alpha " + F(UseAlpha()));
        }
        yield return Tap(L.UseCenter, 60);
        for (var i = 0; i < 40 && !Inspecting; i++) yield return null;
        yield return Frames(.5f);
        Check("A43: a live close-up opens with the world running", Inspecting && !FrontRoomsInspect.CurrentPauses && Time.timeScale > 0f,
            GamePhase + ", pauses " + FrontRoomsInspect.CurrentPauses + ", timeScale " + F(Time.timeScale));
        if (Inspecting)
        {
            yield return Capture("24g_live_closeup");
            var pauseGroup = (CanvasGroup)view.GetType().GetField("pauseGroup", Any)?.GetValue(view);
            Check("A43: a live close-up keeps BACK, with no TEXT and no pause button",
                controls.CurrentUsePrompt.Kind == FrontRoomsTouchControls.UseKind.Back && !controls.TextChipShown && pauseGroup != null && pauseGroup.alpha < .01f,
                controls.CurrentUsePrompt.Kind + ", TEXT " + controls.TextChipShown + ", pause alpha " + (pauseGroup != null ? F(pauseGroup.alpha) : "-"));
            yield return Tap(L.PauseCenter, 61);
            yield return Frames(.2f);
            Check("A43: the pause button's place does not pause a live close-up", Inspecting, GamePhase.ToString());
            var o = L.StickRest + new Vector2(30f, 40f) * L.Scale;
            var deep = o - new Vector2(0f, L.ShotBackDepth + 20f * L.Scale);
            Send(62, o, InputTouchPhase.Began);
            yield return null;
            yield return Drag(62, o, deep, .12f);
            for (var i = 0; i < 90 && Inspecting; i++) yield return null;
            var left = PlayerRoot.position;
            yield return Frames(.8f);
            var walked = Vector3.ProjectOnPlane(PlayerRoot.position - left, Vector3.up).magnitude;
            Check("A43: the pull-back that leaves a live close-up keeps backing away (one move)", !Inspecting && controls.StickTouched && walked > .1f,
                GamePhase + ", stick " + controls.StickTouched + ", walked " + F(walked) + " m");
            Send(62, deep, InputTouchPhase.Ended);
            yield return Frames(.4f);
        }
        if (forced) reading.Def.pause = keptPause;
    }

    // ------------------------------------------------------------ terminals (TT1′, computer_ui 41 §5.4)

    /// <summary>The middle of a cell of the 80 x 25 grid on screen (points), from the picture's rect.</summary>
    Vector2 TerminalCell(FrontRoomsTouchLayout L, int col, int row)
    {
        var r = FrontRoomsInspectTerminal.PictureViewportRect;
        var u = (col + .5f) / FrontRoomsTerminalGrid.Cols;
        var v = 1f - (row + .5f) / FrontRoomsTerminalGrid.Rows;
        return new Vector2((r.xMin + u * r.width) * L.Frame.x, (r.yMin + v * r.height) * L.Frame.y);
    }

    List<FrontRoomsScreenVideo> MazeDeskScreens()
    {
        var found = new List<FrontRoomsScreenVideo>();
        foreach (var s in UnityEngine.Object.FindObjectsByType<FrontRoomsScreenVideo>(FindObjectsInactive.Exclude))
            if (s != null && s.isActiveAndEnabled && s.IsMazeDesk && s.TubeShaped) found.Add(s);
        return found;
    }

    /// <summary>Why the close-up's solver refuses this target from here ("" when it solves).</summary>
    string CloseUpRefusal(FrontRoomsInspectable target)
    {
        var rig = game.GetType().GetField("rig", Any)?.GetValue(game);
        var result = typeof(FrontRoomsInspect).GetMethod("SolveFor", Any).Invoke(null, new[] { target, rig });
        return (string)result.GetType().GetField("why", Any).GetValue(result);
    }

    /// <summary>Level design's stand box (20_design §2.2) comes with L2; until then the task chairs in front of the screen go.</summary>
    void ClearTerminalStand(FrontRoomsInspectable target)
    {
        var face = target.Face;
        var normal = Vector3.ProjectOnPlane(face.rotation * Vector3.forward, Vector3.up).normalized;
        var centre = face.position + normal * .8f;
        foreach (var c in Physics.OverlapBox(centre, new Vector3(.7f, 1.2f, .7f), Quaternion.LookRotation(normal, Vector3.up), ~0, QueryTriggerInteraction.Ignore))
            for (var p = c.transform; p != null; p = p.parent)
                if (p.name.IndexOf("TaskChair", StringComparison.Ordinal) >= 0) { p.gameObject.SetActive(false); break; }
        Physics.SyncTransforms();
    }

    /// <summary>The desk CRTs in the streamed world, moving to Office zones to find some (0 to 6 hops).</summary>
    List<FrontRoomsScreenVideo> terminalDesks;
    IEnumerator FindTerminalDesks()
    {
        terminalDesks = MazeDeskScreens();
        for (var attempt = 0; attempt < 6 && terminalDesks.Count == 0 && Map != null; attempt++)
        {
            var here = Map.CellOf(PlayerRoot.position);
            var moved = false;
            for (var r = 4 + attempt * 5; r < 70 && !moved; r++)
                for (var dx = -r; dx <= r && !moved; dx++)
                    foreach (var dy in new[] { -r, r })
                    {
                        var c = new FrontRooms.Map.GridCoord(here.x + dx, here.y + dy);
                        if (Map.InStartArea(c) || Map.Cache.ZoneOf(c).theme != FrontRooms.Map.ZoneTheme.Office) continue;
                        Teleport(Map.CellCenter(c), 0f, 0f);
                        moved = true;
                        break;
                    }
            yield return Frames(3f);
            terminalDesks = MazeDeskScreens();
        }
    }

    /// <summary>Opens the terminal's close-up straight through the game's own BeginInspect (the walk-up is 游戏视觉's harness).</summary>
    IEnumerator OpenTerminal(FrontRoomsTerminalScreen term)
    {
        yield return StandAtInspectableSeen(0f, term.ReadTarget);
        game.GetType().GetMethod("BeginInspect", Any).Invoke(game, new object[] { term.ReadTarget });
        for (var i = 0; i < 30 && !FrontRoomsInspectTerminal.Owns; i++) yield return null;
        yield return Frames(.8f);
    }

    IEnumerator TerminalSwipe(Vector2 from, Vector2 to, float seconds, int id)
    {
        Send(id, from, InputTouchPhase.Began);
        yield return null;
        yield return Drag(id, from, to, seconds);
        Send(id, to, InputTouchPhase.Ended);
        yield return Frames(.5f);
    }

    /// <summary>
    /// TT1′: a terminal close-up by touch (41 §5.4 and the conditions of 2026-10-30 13:22): the two-tap rule, swipes, soft keys
    /// with the margin below the picture, Android Back as CANCEL, and BACK and the pull-back as the ways out. The run's own
    /// terminal once level design marks desks (L2); until then one desk CRT is made a terminal here. EXAMINE focus runs only.
    /// </summary>
    IEnumerator TerminalTouchChecks(FrontRoomsTouchLayout L)
    {
        var term = FrontRoomsTerminalScreen.All.Count > 0 ? FrontRoomsTerminalScreen.All[0] : null;
        if (term == null)
        {
            yield return FindTerminalDesks();
            if (terminalDesks.Count == 0) { Note("TT1", "no desk CRT found (skipped)"); yield break; }
            foreach (var desk in terminalDesks)
            {
                var tried = FrontRoomsTerminalScreen.Attach(desk.gameObject, 0);
                yield return Frames(.3f);
                if (tried == null || tried.ReadTarget == null) continue;
                ClearTerminalStand(tried.ReadTarget);
                yield return StandAtInspectableSeen(0f, tried.ReadTarget);
                yield return Frames(.2f);
                var why = CloseUpRefusal(tried.ReadTarget);
                if (string.IsNullOrEmpty(why)) { term = tried; break; }
                Note("TT1", "desk at " + V3(desk.transform.position) + " refused: " + why);
            }
        }
        if (term == null || term.ReadTarget == null) { Note("TT1", "no terminal READ target that opens (skipped)"); yield break; }
        yield return OpenTerminal(term);
        Check("TT1': a terminal close-up opens on MAIN", FrontRoomsInspectTerminal.Owns && term.Machine.Panel == TerminalPanel.Main,
            GamePhase + ", owns " + FrontRoomsInspectTerminal.Owns + ", panel " + term.Machine.Panel);
        if (!FrontRoomsInspectTerminal.Owns) yield break;
        var m = term.Machine;
        yield return Capture("25_terminal_main");

        // The two-tap rule: a tap on another item moves the bar; a tap on the lit item opens it.
        if (m.Bar == 0)
        {
            yield return Tap(TerminalCell(L, 30, m.ItemRow(1)), 70);
            yield return Frames(.5f);
        }
        var first = TerminalCell(L, 30, m.ItemRow(0));
        yield return Tap(first, 71);
        yield return Frames(.5f);
        Check("TT1': a tap on an item moves the bar there and opens nothing", m.Bar == 0 && m.Panel == TerminalPanel.Main, "bar " + m.Bar + ", panel " + m.Panel);
        yield return Tap(first, 72);
        yield return Frames(.5f);
        Check("TT1': a tap on the lit item opens it", m.Panel == TerminalPanel.Doc, "panel " + m.Panel + ", memo " + m.Memo);
        yield return Capture("25b_terminal_doc");

        // Swipes resolve on touch-up: >= 40 pt in <= 0.35 s from the body rows; anything else does nothing.
        var mid = TerminalCell(L, 40, 14);
        var shown = m.Memo + "/" + m.Scroll;
        yield return TerminalSwipe(mid, mid + new Vector2(0f, 90f), .7f, 73);
        Check("TT1': a slow drag on the picture does nothing", m.Memo + "/" + m.Scroll == shown, shown + " -> " + m.Memo + "/" + m.Scroll);
        var top = TerminalCell(L, 40, 1);
        yield return TerminalSwipe(top, top - new Vector2(0f, 90f), .15f, 74);
        Check("TT1': a swipe from the top rows stays the system's", m.Memo + "/" + m.Scroll == shown, shown + " -> " + m.Memo + "/" + m.Scroll);
        yield return TerminalSwipe(mid, mid + new Vector2(0f, 90f), .15f, 75);
        var paged = m.Memo + "/" + m.Scroll;
        Check("TT1': a swipe up pages forward", paged != shown, shown + " -> " + paged);
        yield return TerminalSwipe(mid, mid + new Vector2(90f, 0f), .15f, 76);
        Check("TT1': a swipe right pages back", m.Memo + "/" + m.Scroll != paged, paged + " -> " + m.Memo + "/" + m.Scroll);

        // Soft keys: the box in row 25, and its hit running on below the picture.
        var footer = term.Footer;
        var at = footer.IndexOf("Cancel", StringComparison.Ordinal);
        Check("TT1': row 25 shows touch soft keys", at >= 0 && FrontRoomsInspectTerminal.CurrentNames == FrontRoomsInspectTerminal.Names.Touch, "'" + footer.Trim() + "' " + FrontRoomsInspectTerminal.CurrentNames);
        if (at >= 0)
        {
            var key = TerminalCell(L, at + 2, FrontRoomsTerminal.FooterRow);
            yield return Tap(key, 77);
            yield return Frames(.5f);
            Check("TT1': a tap on a soft key presses it (Cancel, back to MAIN)", m.Panel == TerminalPanel.Main, "panel " + m.Panel);
            yield return Tap(first, 78);
            yield return Frames(.5f);
            var r = FrontRoomsInspectTerminal.PictureViewportRect;
            var below = new Vector2(key.x, Mathf.Max(1f, (r.yMin - r.height * .02f) * L.Frame.y));
            yield return Tap(below, 79);
            yield return Frames(.5f);
            Check("TT1': a soft key's hit runs on below the picture", m.Panel == TerminalPanel.Main, "panel " + m.Panel + " at " + V(below));
        }

        // Android Back is CANCEL: one step back, and it leaves from MAIN.
        if (m.Panel == TerminalPanel.Main) { yield return Tap(first, 80); yield return Frames(.5f); }
        FrontRoomsMobileBackBridge.SimulateBackPressed();
        yield return Frames(.5f);
        Check("TT1': Android Back on a terminal is CANCEL: one step back, still open", Inspecting && m.Panel == TerminalPanel.Main, GamePhase + ", panel " + m.Panel);
        FrontRoomsMobileBackBridge.SimulateBackPressed();
        for (var i = 0; i < 90 && Inspecting; i++) yield return null;
        Check("TT1': Android Back on MAIN leaves", !Inspecting, GamePhase.ToString());
        yield return Frames(.8f);

        // BACK leaves, and so does the hidden stick's pull-back from outside the picture.
        yield return OpenTerminal(term);
        yield return Tap(L.InspectBackCenter, 81);
        for (var i = 0; i < 90 && Inspecting; i++) yield return null;
        Check("TT1': BACK leaves from any panel", !Inspecting, GamePhase.ToString());
        yield return Frames(.8f);
        yield return OpenTerminal(term);
        var side = FrontRoomsInspectTerminal.PictureViewportRect;
        var o = new Vector2(side.xMin * L.Frame.x * .5f, L.Frame.y * .55f);
        if (FrontRoomsInspectTerminal.Owns && L.InMoveZone(o))
        {
            var deep = o - new Vector2(0f, L.ShotBackDepth + 20f * L.Scale);
            yield return TerminalSwipe(o, deep, .3f, 82);
            for (var i = 0; i < 90 && Inspecting; i++) yield return null;
            Check("TT1': the pull-back from outside the picture leaves", !Inspecting, GamePhase.ToString());
        }
        else Note("TT1", "no room beside the picture for the pull-back on this profile (picture from x " + F(side.xMin) + ")");
    }

    /// <summary>
    /// EXAMINE T6/T7 on the placard: the TEXT chip in the pause slot, the panel at the right 36 % of the safe area, region heads
    /// in Plex Mono, glow lines only below the ink gate with their rule, a drag on the panel scrolls it and a drag beside it does
    /// not, TEXT again closes it, and leaving the close-up undoes the chip. The placard's transcript is swapped for a test one
    /// in 游戏视觉's format (U+0002 heads, U+0001 glow) for these checks only, and put back.
    /// </summary>
    IEnumerator ExamineTextChecks(FrontRoomsTouchLayout L, FrontRoomsInspectable target)
    {
        var id = target != null && target.Def != null ? target.Def.transcript : null;
        var table = typeof(FrontRoomsInspectCatalog).GetField("Transcripts", Any)?.GetValue(null) as Dictionary<string, string[]>;
        if (string.IsNullOrEmpty(id) || table == null) { Note("EXAMINE", "the target has no transcript id (T6/T7 skipped)"); yield break; }
        var hadShipped = table.TryGetValue(id, out var shipped);
        table[id] = new[]
        {
            "\u0002PLAN", "EVACUATION PLAN · LEVEL 0", "NO REENTRY (the door into this floor)", "YOU ARE HERE", "\u0001EXIT (over the stair door)", "STAIR A",
            "\u0002LEGEND", "LEGEND: YOU ARE HERE · STAIR (EXIT), ONE FLOOR DOWN · WALL ARROWS, POINT THE WAY OUT", "TYPICAL FLOOR · NOT TO SCALE", "THIS FLOOR MAY DIFFER FROM PLAN",
            "\u0002IN CASE OF ALARM", "ON ALARM, A FLOOR WARDEN CHECKS EVERY ROOM.", "1 WALK, DO NOT RUN.", "2 CLOSE DOORS BEHIND YOU.",
            "3 IF THE LIGHTS DIM, LEAVE THE AREA.", "4 EXIT BY STAIR, ONE FLOOR DOWN.",
            "\u0002TITLE BLOCK", "LEVEL 0 · SHEET A-2 OF 4 · PRINTED 11/89",
        };
        yield return StandAtInspectableSeen(12f, target);
        if (examineTap.HasValue)
        {
            yield return Tap(examineTap.Value, 52);
            for (var i = 0; i < 40 && !Inspecting; i++) yield return null;
        }
        yield return Frames(.6f);
        if (!Inspecting)
        {
            Check("EXAMINE: reopened for the TEXT checks", false, GamePhase.ToString());
            if (hadShipped) table[id] = shipped; else table.Remove(id);
            yield break;
        }
        var chip = TouchNode("TEXT chip");
        var chipGroup = chip != null ? chip.GetComponent<CanvasGroup>() : null;
        Check("EXAMINE: the TEXT chip shows in the pause slot when the close-up has a transcript",
            controls.TextChipShown && chipGroup != null && chipGroup.alpha > .9f && (chip.anchoredPosition - L.TextChipCenter).magnitude < 1f,
            "shown " + controls.TextChipShown + ", alpha " + (chipGroup != null ? F(chipGroup.alpha) : "-") + " at " + (chip != null ? V(chip.anchoredPosition) : "-") + " want " + V(L.TextChipCenter));
        yield return Tap(L.TextChipCenter, 53);
        yield return Frames(.5f);
        var panel = TouchNode("Transcript panel");
        var panelGroup = panel != null ? panel.GetComponent<CanvasGroup>() : null;
        var r = L.TranscriptPanel;
        Check("EXAMINE: TEXT opens the transcript panel at the right 36 % of the safe area",
            FrontRoomsInspect.ShowTranscript && panelGroup != null && panelGroup.alpha > .9f && (panel.anchoredPosition - r.center).magnitude < 1f && (panel.sizeDelta - r.size).magnitude < 1f,
            "on " + FrontRoomsInspect.ShowTranscript + ", alpha " + (panelGroup != null ? F(panelGroup.alpha) : "-") + ", rect x " + F(r.xMin) + "–" + F(r.xMax) + " y " + F(r.yMin) + "–" + F(r.yMax));
        Check("EXAMINE: the panel ends above BACK's hit circle", r.yMin >= L.InspectBackCenter.y + L.UseHitRadius, F(r.yMin) + " vs " + F(L.InspectBackCenter.y + L.UseHitRadius));
        var fill = chip != null ? chip.Find("chip")?.GetComponent<UnityEngine.UI.Image>() : null;
        var word = chip != null ? chip.Find("label")?.GetComponent<UnityEngine.UI.Text>() : null;
        Check("EXAMINE: on, TEXT is a paper chip with the word in ink", fill != null && fill.color.a > .9f && word != null && word.color.r < .1f,
            "fill alpha " + (fill != null ? F(fill.color.a) : "-") + ", word " + (word != null ? word.color.ToString() : "-"));
        var texts = panel != null ? panel.GetComponentsInChildren<UnityEngine.UI.Text>(true) : new UnityEngine.UI.Text[0];
        int heads = 0, glowLines = 0, lines = 0;
        foreach (var t in texts)
        {
            if (t.name == "head" && t.font != null && t.font.name.Contains("Plex")) heads++;
            else if (t.name == "glow line") glowLines++;
            else if (t.name == "line" && t.font != null && t.font.name.Contains("Serif")) lines++;
        }
        var rules = 0;
        if (panel != null) foreach (var img in panel.GetComponentsInChildren<UnityEngine.UI.Image>(true)) if (img.name == "glow rule") rules++;
        var legible = FrontRoomsInspectLook.OwnLight < .15f;
        Check("EXAMINE: four region heads in Plex Mono, the lines in Source Serif", heads == 4 && lines == 13, heads + " heads, " + lines + " lines");
        var alarm = 0;
        foreach (var t in texts) if (t.text.Trim().Equals("IN CASE OF ALARM", StringComparison.OrdinalIgnoreCase)) alarm++;
        Check("EXAMINE: a head that its region's first line repeats is said once (IN CASE OF ALARM)", alarm == 1, alarm + " x IN CASE OF ALARM");
        Check("EXAMINE: the glow line shows only below the ink gate, with its rule", legible ? glowLines == 1 && rules == 1 : glowLines == 0 && rules == 0,
            "own light " + F(FrontRoomsInspectLook.OwnLight) + ", glow lines " + glowLines + ", rules " + rules);
        yield return Capture("24d_examine_text");
        if (controls.TranscriptScrollMax > 1f)
        {
            var from = r.center;
            Send(54, from, InputTouchPhase.Began);
            for (var i = 1; i <= 10; i++) { yield return null; Send(54, from + new Vector2(0f, i * 8f), InputTouchPhase.Moved); }
            yield return null;
            Send(54, from + new Vector2(0f, 80f), InputTouchPhase.Ended);
            yield return Frames(.2f);
            var scrolled = controls.TranscriptScroll;
            Check("EXAMINE: a drag on the panel scrolls it", scrolled > 10f, F(scrolled) + " of " + F(controls.TranscriptScrollMax));
            yield return Capture("24e_examine_text_scrolled");
            var beside = new Vector2(r.xMin - 40f, r.center.y);
            Send(55, beside, InputTouchPhase.Began);
            for (var i = 1; i <= 10; i++) { yield return null; Send(55, beside + new Vector2(0f, -i * 8f), InputTouchPhase.Moved); }
            yield return null;
            Send(55, beside + new Vector2(0f, -80f), InputTouchPhase.Ended);
            yield return Frames(.2f);
            Check("EXAMINE: a drag beside the panel does not scroll it", Mathf.Abs(controls.TranscriptScroll - scrolled) < .01f, F(controls.TranscriptScroll) + " vs " + F(scrolled));
        }
        else Note("EXAMINE", "the test transcript fits the panel (scroll not driven)");
        yield return Tap(L.TextChipCenter, 56);
        yield return Frames(.5f);
        Check("EXAMINE: TEXT again closes the panel", !FrontRoomsInspect.ShowTranscript && panelGroup != null && panelGroup.alpha < .01f,
            "on " + FrontRoomsInspect.ShowTranscript + ", alpha " + (panelGroup != null ? F(panelGroup.alpha) : "-"));
        yield return Tap(L.TextChipCenter, 57);
        yield return Frames(.3f);
        yield return Tap(L.InspectBackCenter, 58);
        for (var i = 0; i < 90 && Inspecting; i++) yield return null;
        yield return Frames(.5f);
        Check("EXAMINE: leaving the close-up turns the chip's transcript off", !Inspecting && !FrontRoomsInspect.ShowTranscript, GamePhase + ", on " + FrontRoomsInspect.ShowTranscript);
        if (hadShipped) table[id] = shipped; else table.Remove(id);
    }

    /// <summary>The game's gameplay eye: the camera rig's BaseEye, else the camera (as UpdateAim reads it).</summary>
    Pose GameEye()
    {
        var rig = game.GetType().GetField("rig", Any)?.GetValue(game);
        if (rig != null && rig.GetType().GetProperty("BaseEye", Any)?.GetValue(rig) is Pose pose) return pose;
        var cam = (Camera)game.GetType().GetField("cam", Any).GetValue(game);
        return new Pose(cam.transform.position, cam.transform.rotation);
    }

    /// <summary>True when the game's own crosshair ray (the gameplay eye through AimCast) hits <paramref name="c"/> first.</summary>
    /// <summary>What the game's crosshair ray hits now (for a failing check's detail).</summary>
    string CrosshairHit()
    {
        var aim = game.GetType().GetMethod("AimCast", Any);
        if (aim == null) return "no AimCast";
        var eye = GameEye();
        var args = new object[] { new Ray(eye.position, eye.rotation * Vector3.forward), null };
        return (bool)aim.Invoke(game, args) ? ((RaycastHit)args[1]).collider.name + " at " + F(((RaycastHit)args[1]).distance) + " m" : "nothing in reach";
    }

    bool CrosshairOn(Collider c)
    {
        var aim = game.GetType().GetMethod("AimCast", Any);
        if (aim == null) return false;
        var eye = GameEye();
        var args = new object[] { new Ray(eye.position, eye.rotation * Vector3.forward), null };
        return (bool)aim.Invoke(game, args) && ((RaycastHit)args[1]).collider == c;
    }

    /// <summary>Turns the view so the gameplay eye looks at <paramref name="point"/>.</summary>
    void AimAt(Vector3 point)
    {
        var from = GameEye().position;
        var to = point - from;
        game.GetType().GetField("yaw", Any).SetValue(game, Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg);
        game.GetType().GetField("pitch", Any).SetValue(game, Mathf.Atan2(-to.y, new Vector2(to.x, to.z).magnitude) * Mathf.Rad2Deg);
    }

    bool PlaceAt(Collider target, float distance, Vector3 feet, int onlySpot = -1, float radius = .25f)
    {
        var center = target.bounds.center;
        var body = (CharacterController)game.GetType().GetField("playerBody", Any).GetValue(game);
        for (var k = 0; k < 16; k++)
        {
            if (onlySpot >= 0 && k != onlySpot) continue;
            var a = k * Mathf.PI / 8f;
            var spot = new Vector3(center.x + Mathf.Sin(a) * distance, feet.y, center.z + Mathf.Cos(a) * distance);
            var eye = spot + Vector3.up * 1.6f;
            if (Physics.CheckCapsule(spot + Vector3.up * .35f, spot + Vector3.up * 1.6f, radius, ~0, QueryTriggerInteraction.Ignore)) continue;
            if (!Physics.Raycast(eye, (center - eye).normalized, out var hit, distance + 1f, ~0, QueryTriggerInteraction.Ignore) || hit.collider != target) continue;
            body.enabled = false;
            PlayerRoot.position = spot;
            body.enabled = true;
            var to = center - eye;
            var yaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
            var pitch = Mathf.Atan2(-to.y, new Vector2(to.x, to.z).magnitude) * Mathf.Rad2Deg;
            game.GetType().GetField("yaw", Any).SetValue(game, yaw);
            game.GetType().GetField("pitch", Any).SetValue(game, pitch);
            Note("stand", target.name + " at " + F(distance) + " m, " + F((spot - feet).magnitude) + " m from where the player was");
            return true;
        }
        return false;
    }

    /// <summary>
    /// A dark room for 平面视觉's label audit: the frame pushed down 6 EV by a temporary global volume, for one
    /// capture. Exposure rather than switching lamps off, because the lamp system drives its own lights each frame.
    /// The touch layer is drawn over the frame, so it is unaffected.
    /// </summary>
    IEnumerator CaptureDark(string name)
    {
        var go = new GameObject("Touch playtest dark room");
        var volume = go.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10000f;
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.Add<ColorAdjustments>(true).postExposure.Override(-6f);
        volume.sharedProfile = profile;
        yield return null;
        yield return null;
        yield return Capture(name);
        Destroy(go);
        Destroy(profile);
        yield return null;
    }

    /// <summary>
    /// Keeps the Relay dormant for the touch steps (harness only, a runtime flag on this run's Relay: its Tick
    /// returns at once). The steps teleport the player around a live map; a chase would cancel the glass shot and
    /// a catch would end the run halfway (run 9). The caught step calls the game's End() itself.
    /// </summary>
    void HoldRelay()
    {
        var relay = game.GetType().GetField("relay", Any)?.GetValue(game);
        if (relay == null) return;
        var state = relay.GetType().GetProperty("State", Any)?.GetValue(relay)?.ToString();
        if (state != "Dormant") { Note("relay", "already " + state + " (not held)"); return; }
        relay.GetType().GetField("caught", Any)?.SetValue(relay, true);
        Note("relay", "held dormant for the touch steps");
    }

    void Teleport(Vector3 feet, float yaw, float pitch)
    {
        var body = (CharacterController)game.GetType().GetField("playerBody", Any).GetValue(game);
        body.enabled = false;
        PlayerRoot.position = feet;
        body.enabled = true;
        Physics.SyncTransforms();
        game.GetType().GetField("yaw", Any).SetValue(game, yaw);
        game.GetType().GetField("pitch", Any).SetValue(game, pitch);
    }

    /// <summary>A key is taken: its look+tap trigger left the map's registry, or the key itself is gone.</summary>
    bool KeyGone(Collider key)
    {
        if (key == null) return true;
        foreach (var c in MapColliders("keyByCollider")) if (c == key) return false;
        return true;
    }

    /// <summary>
    /// PlaceAtKey, then the body settles for two frames and the game's own key ray (KeyOnRay from the gameplay eye,
    /// within the game's Reach) must find the key; one re-aim from the real eye, else the next spot.
    /// </summary>
    IEnumerator StandAtKeySeen(Collider key, float distance, Action<bool> seen)
    {
        var body = (CharacterController)game.GetType().GetField("playerBody", Any).GetValue(game);
        var radius = body.radius + .02f;
        for (var k = 0; k < 16; k++)
        {
            if (!PlaceAtKey(key, distance, k, radius)) continue;
            yield return null;
            yield return null;
            if (KeyUnderCrosshair(key)) { seen(true); yield break; }
            AimAt(key.bounds.center);
            yield return null;
            if (KeyUnderCrosshair(key)) { seen(true); Note("stand", "re-aimed from the eye at " + key.name); yield break; }
        }
        Note("stand", "no spot where the crosshair finds " + key.name);
        seen(false);
    }

    /// <summary>True when the game's key ray from the gameplay eye (KeyOnRay within Reach) finds <paramref name="key"/>.</summary>
    bool KeyUnderCrosshair(Collider key)
    {
        var eye = GameEye();
        var reachField = game.GetType().GetField("Reach", Any);
        var reach = reachField != null ? Convert.ToSingle(reachField.GetValue(reachField.IsStatic ? null : game)) : 2.4f;
        return Map.KeyOnRay(new Ray(eye.position, eye.rotation * Vector3.forward), reach, PlayerRoot) == key;
    }

    /// <summary>Stands the player <paramref name="distance"/> m from a key, on the floor, looking at it, when the key is in plain view (KeyOnRay).</summary>
    bool PlaceAtKey(Collider key, float distance, int onlySpot = -1, float radius = .25f)
    {
        var center = key.bounds.center;
        var feet = PlayerRoot.position;
        var body = (CharacterController)game.GetType().GetField("playerBody", Any).GetValue(game);
        for (var k = 0; k < 16; k++)
        {
            if (onlySpot >= 0 && k != onlySpot) continue;
            var a = k * Mathf.PI / 8f;
            var spot = new Vector3(center.x + Mathf.Sin(a) * distance, feet.y, center.z + Mathf.Cos(a) * distance);
            var eye = spot + Vector3.up * 1.6f;
            if (Physics.CheckCapsule(spot + Vector3.up * .35f, spot + Vector3.up * 1.6f, radius, ~0, QueryTriggerInteraction.Ignore)) continue;
            if (Map.KeyOnRay(new Ray(eye, (center - eye).normalized), distance + 1.5f, PlayerRoot) != key) continue;
            body.enabled = false;
            PlayerRoot.position = spot;
            body.enabled = true;
            Physics.SyncTransforms();
            var to = center - eye;
            game.GetType().GetField("yaw", Any).SetValue(game, Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg);
            game.GetType().GetField("pitch", Any).SetValue(game, Mathf.Atan2(-to.y, new Vector2(to.x, to.z).magnitude) * Mathf.Rad2Deg);
            Note("stand", key.name + " at " + F(distance) + " m");
            return true;
        }
        return false;
    }

    /// <summary>Turns the view by <paramref name="degrees"/> of yaw (the crosshair leaves what it was on).</summary>
    void TurnBy(float degrees)
    {
        var field = game.GetType().GetField("yaw", Any);
        field.SetValue(game, (float)field.GetValue(game) + degrees);
    }

    /// <summary>
    /// A point (pt) in the look zone, clear of USE: on the key's projected centre (<paramref name="off"/> 0), or
    /// the nearest point from <paramref name="off"/> pt out (2 pt steps, up to the 22 pt finger radius) where the
    /// finger's own ray misses the key (the finger-target case); <paramref name="off"/> returns the distance used.
    /// </summary>
    bool KeyTapPoint(Collider key, ref float off, out Vector2 points)
    {
        points = default;
        var cam = (Camera)game.GetType().GetField("cam", Any).GetValue(game);
        var L = controls.Layout;
        var v = cam.WorldToViewportPoint(key.bounds.center);
        if (v.z <= 0f) return false;
        var center = new Vector2(v.x * L.Frame.x, v.y * L.Frame.y);
        for (var r = off; r <= 22f + 1e-3f; r += 2f)
        for (var k = 0; k < 8; k++)
        {
            var a = k * Mathf.PI / 4f;
            var p = center + (off > 0f ? new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r : Vector2.zero);
            if (!L.InLookZone(p) || FrontRoomsTouchLayout.Contains(L.UseCenter, L.UseHitRadius + 6f, p)) continue;
            var hit = Map.KeyOnRay(cam.ViewportPointToRay(new Vector3(p.x / L.Frame.x, p.y / L.Frame.y, 0f)), 2.4f, PlayerRoot);
            if (off > 0f ? hit == null : hit == key) { points = p; off = r; return true; }
            if (off <= 0f) return false;
        }
        return false;
    }

    /// <summary>The map's own registry of doors or windows (reflection: the harness reads, never changes, it).</summary>
    IEnumerable<Collider> MapColliders(string field)
    {
        var map = Map;
        var dict = map == null ? null : map.GetType().GetField(field, Any)?.GetValue(map) as IDictionary;
        var list = new List<Collider>();
        if (dict != null) foreach (var key in dict.Keys) if (key is Collider c && c != null) list.Add(c);
        return list;
    }

    /// <summary>A point (pt) on <paramref name="c"/> in the look zone, clear of USE, whose ray hits it within reach.</summary>
    bool TapPointOn(Collider c, out Vector2 points)
    {
        points = default;
        var cam = (Camera)game.GetType().GetField("cam", Any).GetValue(game);
        var L = controls.Layout;
        var b = c.bounds;
        for (var iy = 1; iy <= 3; iy++)
        for (var ix = 0; ix <= 4; ix++)
        for (var iz = 0; iz <= 4; iz++)
        {
            var w = new Vector3(Mathf.Lerp(b.min.x, b.max.x, ix / 4f), Mathf.Lerp(b.min.y, b.max.y, iy / 4f), Mathf.Lerp(b.min.z, b.max.z, iz / 4f));
            var v = cam.WorldToViewportPoint(w);
            if (v.z <= 0f || v.x < .05f || v.x > .95f || v.y < .1f || v.y > .9f) continue;
            var p = new Vector2(v.x * L.Frame.x, v.y * L.Frame.y);
            if (!L.InLookZone(p) || FrontRoomsTouchLayout.Contains(L.UseCenter, L.UseHitRadius + 6f, p)) continue;
            if (!Physics.Raycast(cam.ViewportPointToRay(new Vector3(v.x, v.y, 0f)), out var hit, 2.4f, ~0, QueryTriggerInteraction.Ignore) || hit.collider != c) continue;
            points = p;
            return true;
        }
        return false;
    }

    /// <summary>The y of a touch-layer node, by name (motion checks).</summary>
    float NodeY(string name)
    {
        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (canvas.name != "Handheld touch UI") continue;
            foreach (var t in canvas.GetComponentsInChildren<RectTransform>(true))
                if (t.name == name) return t.anchoredPosition.y;
        }
        return float.NaN;
    }

    /// <summary>A desktop-HUD node's rect as a share of the HUD canvas (0..1, y up), by name; null when it is not built.</summary>
    /// <summary>The game's HUD canvas height in its own units (points on the phone HUD).</summary>
    float HudHeight()
    {
        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (canvas.name == "Minimal HUD") return ((RectTransform)canvas.transform).rect.height;
        return 402f;
    }

    /// <summary>A node of the game's HUD canvas by name, inactive ones included.</summary>
    Transform HudNode(string name)
    {
        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (canvas.name != "Minimal HUD") continue;
            foreach (var t in canvas.GetComponentsInChildren<RectTransform>(true))
                if (t.name == name) return t;
        }
        return null;
    }

    Rect? HudRect(string name)
    {
        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (canvas.name != "Minimal HUD") continue;
            var root = (RectTransform)canvas.transform;
            var r = root.rect;
            foreach (var t in canvas.GetComponentsInChildren<RectTransform>(true))
            {
                if (t.name != name) continue;
                var corners = new Vector3[4];
                t.GetWorldCorners(corners);
                var a = root.InverseTransformPoint(corners[0]);
                var b = root.InverseTransformPoint(corners[2]);
                return Rect.MinMaxRect((a.x - r.xMin) / r.width, (a.y - r.yMin) / r.height, (b.x - r.xMin) / r.width, (b.y - r.yMin) / r.height);
            }
        }
        return null;
    }

    /// <summary>The z rotation (degrees, −180..180) of a touch-layer node, by name (the broken door's swing).</summary>
    float NodeAngle(string name)
    {
        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (canvas.name != "Handheld touch UI") continue;
            foreach (var t in canvas.GetComponentsInChildren<RectTransform>(true))
                if (t.name == name) return Mathf.DeltaAngle(0f, t.localEulerAngles.z);
        }
        return float.NaN;
    }

    void Send(int id, Vector2 points, InputTouchPhase phase)
    {
        var px = points * FrontRoomsHandheld.PointScale;
        InputSystem.QueueStateEvent(screen, new TouchState { touchId = id, phase = phase, position = px, pressure = 1f });
    }

    IEnumerator Tap(Vector2 p, int id)
    {
        Send(id, p, InputTouchPhase.Began);
        yield return null;
        yield return null;
        yield return null;
        Send(id, p, InputTouchPhase.Ended);
        yield return null;
        yield return null;
    }

    IEnumerator TapWithFrames(Vector2 p, int id, string prefix, int[] at, Action<int> onFrame = null)
    {
        Send(id, p, InputTouchPhase.Began);
        yield return null;
        yield return null;
        Send(id, p, InputTouchPhase.Ended);
        var last = 0;
        foreach (var f in at) last = Mathf.Max(last, f);
        for (var i = 0; i <= last; i++)
        {
            yield return null;
            onFrame?.Invoke(i);
            if (Array.IndexOf(at, i) >= 0) yield return Capture(prefix + "_f" + i.ToString("00"));
        }
    }

    IEnumerator ChipTap(FrontRoomsTouchControls.Button chip, string prefix, int[] at)
    {
        if (!controls.TryGetChip(chip, out var c, out _))
        {
            Check("chip " + chip + " is on screen", false, controls.CurrentMenuState.ToString());
            yield break;
        }
        if (prefix == null) yield return Tap(c, 14);
        else yield return TapWithFrames(c, 14, prefix, at);
    }

    IEnumerator Drag(int id, Vector2 from, Vector2 to, float seconds)
    {
        var n = Mathf.Max(1, Mathf.RoundToInt(seconds / Step));
        for (var i = 1; i <= n; i++)
        {
            Send(id, Vector2.Lerp(from, to, i / (float)n), InputTouchPhase.Moved);
            yield return null;
        }
    }

    IEnumerator Swipe(int id, Vector2 from, Vector2 to, float seconds)
    {
        Send(id, from, InputTouchPhase.Began);
        yield return null;
        yield return Drag(id, from, to, seconds);
        Send(id, to, InputTouchPhase.Ended);
        yield return null;
        yield return null;
    }

    IEnumerator Frames(float seconds)
    {
        var n = Mathf.Max(1, Mathf.RoundToInt(seconds / Step));
        for (var i = 0; i < n; i++) yield return null;
    }

    IEnumerator WaitFor(Func<bool> condition, float seconds, string what)
    {
        var n = Mathf.RoundToInt(seconds / Step);
        for (var i = 0; i < n; i++)
        {
            bool ok;
            try { ok = condition(); } catch (Exception) { ok = false; }
            if (ok) { Check(what, true, F(i * Step) + " s"); yield break; }
            yield return null;
        }
        Check(what, false, "timed out after " + F(seconds) + " s");
        aborted = true;
    }

    IEnumerator AdaptiveLayoutChecks()
    {
        var profiles = new[] {
            FrontRoomsHandheld.IPhonePro,
            FrontRoomsHandheld.IPhoneDuoOuter,
            FrontRoomsHandheld.IPhoneDuoInner,
            FrontRoomsHandheld.IPhoneDuoOuterFlipped,
            FrontRoomsHandheld.IPhoneDuoInnerPortrait,
            new FrontRoomsHandheld.Profile("Portrait split synthetic", 960, 1920, 3f, 12f, 16f, 8f, 21f, false),
            FrontRoomsHandheld.IPhoneDuoOuterFlippedSettled
        };
        for (var i = 0; i < profiles.Length; i++)
        {
            FrontRoomsHandheld.Force(profiles[i]);
            SetupCapture();
            Canvas.ForceUpdateCanvases();
            game.GetType().GetMethod("ApplyMobileSafeAreaLayout", Any)?.Invoke(game, new object[] { true });
            yield return Frames(.3f);
            Check("adaptive " + i + ": resize preserves playing", Phase() == "Playing", Phase());
            var room = HudRect("Room typography");
            Check("adaptive " + i + ": zone stays inside view", room.HasValue && room.Value.xMin >= 0f && room.Value.xMax <= 1f && room.Value.yMin >= 0f && room.Value.yMax <= 1f, room.ToString());
            // Red, 2026-10-24: what meets a camera or a rounded corner gives way (eased 0.28 s), the rest stays full bleed.
            yield return Frames(.25f);
            var shape = FrontRoomsHandheld.Shape;
            if (shape.Cutouts.Length > 0)
            {
                room = HudRect("Room typography");
                var roomPt = room.HasValue ? Rect.MinMaxRect(room.Value.xMin * shape.Frame.x, room.Value.yMin * shape.Frame.y, room.Value.xMax * shape.Frame.x, room.Value.yMax * shape.Frame.y) : default(Rect);
                Check("adaptive " + i + ": the zone clears the camera by 12 pt", room.HasValue && CutoutGap(roomPt) >= 11.5f, F(CutoutGap(roomPt)) + " pt, zone " + roomPt);
                var pause = controls.Layout.PauseCenter;
                Check("adaptive " + i + ": pause clears the camera and sits at the bleed unless a corner or the camera moves it",
                    CutoutGap(new Rect(pause - Vector2.one * 22f, Vector2.one * 44f)) >= 11.5f && Vector2.Distance(pause, controls.Layout.PauseCenterBase) < 24f &&
                    CornerGap(pause, 22f) >= FrontRoomsHandheld.HudCornerGap - .05f,
                    "pause " + pause + ", at the bleed " + controls.Layout.PauseCenterBase + ", " + F(CornerGap(pause, 22f)) + " pt from a rounded corner");
                var use = controls.Layout.UseCenter;
                Check("adaptive " + i + ": USE clears the camera", CutoutGap(new Rect(use - Vector2.one * controls.Layout.HoldRingRadius, Vector2.one * controls.Layout.HoldRingRadius * 2f)) >= 11.5f, "USE " + use);
            }
            yield return Capture("adaptive_" + i + "_playing");
            yield return Tap(controls.Layout.PauseCenter, 51);
            yield return Frames(.5f);
            Check("adaptive " + i + ": pause can be tapped", Phase() == "Paused", Phase());
            Check("adaptive " + i + ": the pause is the settings sheet", controls.CurrentMenuState == FrontRoomsTouchControls.MenuState.Settings, controls.CurrentMenuState.ToString());
            yield return Capture("adaptive_" + i + "_paused");
            yield return Frames(.2f);
            yield return Capture("adaptive_" + i + "_settings");
            {
                // RESOLUTION AUTO (settings line, 2026-10-24): one 3.2 Mpx budget per screen, recomputed on every fold.
                var px = FrontRoomsHandheld.ScreenPixels;
                var mpx = px.x * px.y / 1e6f;
                var scale = FrontRoomsQuality.EffectiveRenderScale;
                Check("adaptive " + i + ": RESOLUTION AUTO renders " + F(mpx) + " Mpx at " + F(scale),
                    FrontRoomsQuality.Resolution != 0 || (mpx <= 3.3f ? scale > .99f : scale < .99f && Mathf.Abs(scale - Mathf.Round(Mathf.Sqrt(3.2f / mpx) * 20f) / 20f) < .01f),
                    px.x + " x " + px.y + " px → " + F(scale));
            }
            var cameraHere = FrontRoomsHandheld.Shape.Cutouts.Length > 0;
            var worst = "none near";
            if (cameraHere) Check("adaptive " + i + ": the settings heading, BACK and every row clear the camera", SettingsClearOfCutouts(out worst), worst);
            // Scrolled to the end (every pose, for the Figma flow): where a camera is, the last rows pass it, each giving way
            // where it is now, eased.
            var vpEnd = controls.SettingsScrollViewport;
            for (var s = 0; s < 4 && controls.SettingsScroll < controls.SettingsScrollMax - .5f; s++)
                yield return Swipe(60 + s, new Vector2(vpEnd.center.x, vpEnd.yMin + 60f), new Vector2(vpEnd.center.x, vpEnd.yMin + 300f), .25f);
            yield return Frames(.5f);
            if (cameraHere) Check("adaptive " + i + ": scrolled to the end, every row still clears the camera", SettingsClearOfCutouts(out worst), worst + ", scroll " + F(controls.SettingsScroll) + " / " + F(controls.SettingsScrollMax));
            yield return Capture("adaptive_" + i + "_settings_end");
            if (i == 1)
            {
                // Change display with an open settings card; preserve menu state in the live scene.
                FrontRoomsHandheld.Force(profiles[2]);
                SetupCapture();
                Canvas.ForceUpdateCanvases();
                game.GetType().GetMethod("ApplyMobileSafeAreaLayout", Any)?.Invoke(game, new object[] { true });
                yield return Frames(.3f);
                Check("adaptive: unfold preserves open settings", controls.CurrentMenuState == FrontRoomsTouchControls.MenuState.Settings && Phase() == "Paused", controls.CurrentMenuState.ToString());
                yield return Capture("adaptive_settings_unfold");
            }
            yield return ChipTap(FrontRoomsTouchControls.Button.SettingsClose, null, null);
            yield return Frames(.3f);
            Check("adaptive " + i + ": RESUME plays again", Phase() == "Playing", Phase());
        }
    }

    // ================================================================ capture

    void SetupCapture()
    {
        var size = FrontRoomsHandheld.ScreenPixels;
        if (uiTarget == null || uiTarget.width != (int)size.x || uiTarget.height != (int)size.y)
        {
            if (uiTarget != null) uiTarget.Release();
            uiTarget = new RenderTexture((int)size.x, (int)size.y, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { name = "touch playtest UI" };
        }
        if (uiCam == null)
        {
            var go = new GameObject("Touch playtest UI camera");
            go.transform.position = new Vector3(0f, -10000f, 0f);
            DontDestroyOnLoad(go);
            uiCam = go.AddComponent<Camera>();
            uiCam.enabled = false;
            uiCam.orthographic = true;
            uiCam.cullingMask = 1 << 5;
            uiCam.clearFlags = CameraClearFlags.SolidColor;
            uiCam.allowHDR = false;
            uiCam.allowMSAA = false;
            var data = go.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = false;
            data.antialiasing = AntialiasingMode.None;
        }
        uiCam.targetTexture = uiTarget;
        var convertedNow = false;
        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (!canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay || converted.Contains(canvas)) continue;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = uiCam;
            canvas.planeDistance = 1f;
            foreach (var t in canvas.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 5;
            converted.Add(canvas);
            convertedNow = true;
        }
        // The game laid the phone HUD out against the batch window's overlay scale (about 0.8, so the 62 pt safe-area
        // inset came out near 220 pt). On the profile's own pixels, as on the device (×3), lay it out again.
        if (convertedNow)
        {
            Canvas.ForceUpdateCanvases();
            game.GetType().GetMethod("ApplyMobileSafeAreaLayout", Any)?.Invoke(game, new object[] { true });
        }
    }

    IEnumerator Capture(string name)
    {
        // Batch mode never resumes WaitForEndOfFrame: capture now, with the UI as the last LateUpdate left it.
        SetupCapture();
        Canvas.ForceUpdateCanvases();
        var w = uiTarget.width;
        var h = uiTarget.height;
        var cam = (Camera)game.GetType().GetField("cam", Any).GetValue(game);
        if (cam == null) cam = Camera.main;
        if (cam == null) { Check("capture " + name + " has a camera", false, ""); yield break; }
        var scene = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        var prev = cam.targetTexture;
        cam.targetTexture = scene;
        cam.Render();
        cam.targetTexture = prev;
        var sceneTex = Read(scene);
        RenderTexture.ReleaseTemporary(scene);
        // The UI over black and over white: the difference gives its exact coverage, so the composite is the real blend.
        uiCam.backgroundColor = Color.black;
        uiCam.Render();
        var black = Read(uiTarget);
        uiCam.backgroundColor = Color.white;
        uiCam.Render();
        var white = Read(uiTarget);
        var s = sceneTex.GetPixels32();
        var b = black.GetPixels32();
        var wpx = white.GetPixels32();
        var outPx = new Color32[s.Length];
        for (var i = 0; i < s.Length; i++)
        {
            var bl = new Vector3(Lin(b[i].r), Lin(b[i].g), Lin(b[i].b));
            var wl = new Vector3(Lin(wpx[i].r), Lin(wpx[i].g), Lin(wpx[i].b));
            var a = Mathf.Clamp01(1f - ((wl.x - bl.x) + (wl.y - bl.y) + (wl.z - bl.z)) / 3f);
            var sl = new Vector3(Lin(s[i].r), Lin(s[i].g), Lin(s[i].b));
            var o = bl + sl * (1f - a);
            outPx[i] = new Color32(Enc(o.x), Enc(o.y), Enc(o.z), 255);
        }
        var result = new Texture2D(w, h, TextureFormat.RGB24, false);
        result.SetPixels32(outPx);
        result.Apply();
        var file = name + ".png";
        File.WriteAllBytes(Path.Combine(outDir, file), result.EncodeToPNG());
        frames.Add(file + "," + F(Time.unscaledTime - t0));
        // The adaptive run also keeps the bare scene and every drawn UI element on the game's own numbers: the Figma Duo
        // screens are rebuilt from these as editable layers (the desktop masters' method).
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "adaptive") >= 0)
        {
            File.WriteAllBytes(Path.Combine(outDir, name + "_scene.png"), sceneTex.EncodeToPNG());
            File.WriteAllText(Path.Combine(outDir, name + "_ui.json"), DumpUi());
        }
        Destroy(result);
        Destroy(sceneTex);
        Destroy(black);
        Destroy(white);
        shots++;
        yield break;
    }

    /// <summary>
    /// Every UI element drawn now, in canvas points (bottom-left origin), in draw order: the canvas, the path, the rect,
    /// its rotation, its colour with the alpha it is drawn at (groups included), the clip of a RectMask2D above it, and
    /// for text the string, face, size used, alignment and keyline; for images the sprite. JSON, for the Figma rebuild.
    /// </summary>
    string DumpUi()
    {
        var sb = new StringBuilder();
        var frame = FrontRoomsHandheld.ScreenPoints;
        var shape = FrontRoomsHandheld.Shape;
        sb.Append("{\"frame\":[").Append(F(frame.x)).Append(',').Append(F(frame.y)).Append("],\"interface\":").Append(FrontRoomsHandheld.InterfaceOrientation)
          .Append(",\"corners\":[").Append(F(shape.Corners.x)).Append(',').Append(F(shape.Corners.y)).Append(',').Append(F(shape.Corners.z)).Append(',').Append(F(shape.Corners.w)).Append("],\"cutouts\":[");
        for (var i = 0; i < shape.Cutouts.Length; i++)
        {
            var c = shape.Cutouts[i];
            sb.Append(i > 0 ? "," : "").Append('[').Append(F(c.x)).Append(',').Append(F(c.y)).Append(',').Append(F(c.width)).Append(',').Append(F(c.height)).Append(']');
        }
        sb.Append("],\"items\":[");
        var first = true;
        var canvases = new List<Canvas>();
        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None)) if (canvas.isRootCanvas) canvases.Add(canvas);
        canvases.Sort((a, b) => a.sortingOrder.CompareTo(b.sortingOrder));
        foreach (var canvas in canvases)
        {
            var root = (RectTransform)canvas.transform;
            var rr = root.rect;
            var sx = frame.x / Mathf.Max(1f, rr.width);
            var sy = frame.y / Mathf.Max(1f, rr.height);
            foreach (var g in canvas.GetComponentsInChildren<UnityEngine.UI.Graphic>(false))
            {
                if (!g.isActiveAndEnabled) continue;
                var alpha = g.color.a;
                for (var p = g.transform; p != null; p = p.parent)
                {
                    var group = p.GetComponent<CanvasGroup>();
                    if (group != null) { alpha *= group.alpha; if (group.ignoreParentGroups) break; }
                }
                if (alpha < .01f) continue;
                var corners = new Vector3[4];
                g.rectTransform.GetWorldCorners(corners);
                var a0 = root.InverseTransformPoint(corners[0]);
                var a2 = root.InverseTransformPoint(corners[2]);
                var a1 = root.InverseTransformPoint(corners[1]);
                var r = Rect.MinMaxRect((Mathf.Min(a0.x, a2.x) - rr.xMin) * sx, (Mathf.Min(a0.y, a2.y) - rr.yMin) * sy, (Mathf.Max(a0.x, a2.x) - rr.xMin) * sx, (Mathf.Max(a0.y, a2.y) - rr.yMin) * sy);
                var size = g.rectTransform.rect.size;
                var rot = Mathf.Atan2(a1.x - a0.x, a1.y - a0.y) * -Mathf.Rad2Deg;
                var path = g.name;
                for (var p = g.transform.parent; p != null && p != root; p = p.parent) path = p.name + "/" + path;
                sb.Append(first ? "" : ",").Append("{\"c\":\"").Append(Esc(canvas.name)).Append("\",\"p\":\"").Append(Esc(path)).Append("\",\"r\":[")
                  .Append(F(r.x)).Append(',').Append(F(r.y)).Append(',').Append(F(r.width)).Append(',').Append(F(r.height)).Append("],\"size\":[")
                  .Append(F(size.x * sx)).Append(',').Append(F(size.y * sy)).Append("],\"rot\":").Append(F(rot))
                  .Append(",\"col\":[").Append(F(g.color.r)).Append(',').Append(F(g.color.g)).Append(',').Append(F(g.color.b)).Append(',').Append(F(alpha)).Append(']');
                first = false;
                var mask = g.GetComponentInParent<UnityEngine.UI.RectMask2D>();
                if (mask != null && mask.transform != g.transform)
                {
                    var mc = new Vector3[4];
                    ((RectTransform)mask.transform).GetWorldCorners(mc);
                    var m0 = root.InverseTransformPoint(mc[0]);
                    var m2 = root.InverseTransformPoint(mc[2]);
                    sb.Append(",\"clip\":[").Append(F((m0.x - rr.xMin) * sx)).Append(',').Append(F((m0.y - rr.yMin) * sy)).Append(',').Append(F((m2.x - m0.x) * sx)).Append(',').Append(F((m2.y - m0.y) * sy)).Append(']');
                }
                if (g is UnityEngine.UI.Text text)
                {
                    // Best fit reports the size it used in pixels of the scaled canvas: back to points.
                    var used = text.resizeTextForBestFit && text.cachedTextGenerator.fontSizeUsedForBestFit > 0
                        ? text.cachedTextGenerator.fontSizeUsedForBestFit / Mathf.Max(.01f, canvas.scaleFactor) : text.fontSize;
                    sb.Append(",\"t\":\"").Append(Esc(text.text)).Append("\",\"font\":\"").Append(Esc(text.font != null ? text.font.name : "")).Append("\",\"fs\":").Append(F(used))
                      .Append(",\"style\":\"").Append(text.fontStyle).Append("\",\"al\":\"").Append(text.alignment).Append("\",\"ls\":").Append(F(text.lineSpacing));
                    var outline = text.GetComponent<UnityEngine.UI.Outline>();
                    if (outline != null && outline.enabled) sb.Append(",\"ol\":[").Append(F(outline.effectColor.r)).Append(',').Append(F(outline.effectColor.g)).Append(',').Append(F(outline.effectColor.b)).Append(',').Append(F(outline.effectColor.a)).Append(',').Append(F(outline.effectDistance.x)).Append(']');
                }
                else if (g is UnityEngine.UI.Image image)
                {
                    sb.Append(",\"img\":\"").Append(Esc(image.sprite != null ? image.sprite.name : "")).Append("\",\"type\":\"").Append(image.type).Append("\"");
                    if (image.type == UnityEngine.UI.Image.Type.Filled) sb.Append(",\"fill\":").Append(F(image.fillAmount)).Append(",\"method\":\"").Append(image.fillMethod).Append("\"");
                }
                else if (g is UnityEngine.UI.RawImage raw)
                {
                    sb.Append(",\"raw\":\"").Append(Esc(raw.texture != null ? raw.texture.name : "")).Append("\"");
                }
                sb.Append('}');
            }
        }
        sb.Append("]}");
        return sb.ToString();
    }

    static string Esc(string s) => string.IsNullOrEmpty(s) ? "" : s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");

    /// <summary>
    /// The caught card's strings and layout (narrative 40 s6, FIX_CARD_VL217 A7): the subtitle under CAUGHT,
    /// KEYS TAKEN as the third number, the block pushed 10 pt, TRY AGAIN inside the safe area, and the
    /// sentence's capitals (floor tokens and the start rooms' name).
    /// </summary>
    /// <summary>
    /// The handheld cold start (Red, 2026-10-24): the logo on its black curtain while the title room loads behind it,
    /// TAP TO START and the legend hidden, a tap ignored; once loaded, the curtain is gone and the title starts.
    /// </summary>
    IEnumerator ColdStartChecks()
    {
        var view = game.GetComponent<FrontRoomsTouchControlsView>();
        object Member(object o, string name) => o?.GetType().GetField(name, Any)?.GetValue(o);
        float PromptAlpha() => (Member(Member(view, "titlePrompt"), "Group") as CanvasGroup)?.alpha ?? -1f;
        float Curtain() => game.GetType().GetField("startupCurtainAlpha", Any) is var f && f != null ? (float)f.GetValue(game) : -1f;
        if (FrontRoomsHandheld.StartupReady)
        {
            Check("cold start: still loading when the driver starts", false, "already ready: nothing to check");
            yield break;
        }
        Check("cold start: the logo's curtain covers the room while it loads", Curtain() > .99f, "curtain " + F(Curtain()));
        Check("cold start: TAP TO START hidden while loading", PromptAlpha() < .01f, "alpha " + F(PromptAlpha()));
        yield return Tap(new Vector2(437f, 201f), 1);
        yield return Frames(.1f);
        Check("cold start: a tap while loading does not start the run", Phase() == "Title", Phase());
        var from = FrontRoomsTouchMotion.Now;
        yield return WaitFor(() => FrontRoomsHandheld.StartupReady, 30f, "the handheld startup finishes");
        if (aborted) yield break;
        Check("cold start: loaded, the curtain is gone (the room shows)", Curtain() < .01f, "curtain " + F(Curtain()) + " after " + F(FrontRoomsTouchMotion.Now - from) + " s");
        Check("cold start: TAP TO START still hidden the moment it is ready", PromptAlpha() < .01f, "alpha " + F(PromptAlpha()));
        // It comes at the later of the title's 3 s and 1 s after the loads, then fades in over 0.6 s.
        var ready = FrontRoomsTouchMotion.Now;
        var shownAt = -1f;
        for (var i = 0; i < Mathf.RoundToInt(4.5f / Step) && PromptAlpha() < .99f; i++)
        {
            if (shownAt < 0f && PromptAlpha() > .01f) shownAt = FrontRoomsTouchMotion.Now - ready;
            yield return null;
        }
        Check("cold start: TAP TO START fades in once loaded (no sooner than 1 s after)", PromptAlpha() > .99f && shownAt >= .95f,
            "alpha " + F(PromptAlpha()) + ", first seen " + F(shownAt) + " s after the loads");
    }

    void CaughtCardChecks(FrontRoomsTouchLayout L)
    {
        var view = game.GetComponent<FrontRoomsTouchControlsView>();
        object Field(string name) => view.GetType().GetField(name, Any)?.GetValue(view);
        object Member(object o, string name) => o?.GetType().GetField(name, Any)?.GetValue(o);
        var sub = Field("caughtSubtitle");
        var subRect = Member(sub, "Rect") as RectTransform;
        var subText = subRect != null ? subRect.GetComponent<UnityEngine.UI.Text>() : null;
        var subGroup = Member(sub, "Group") as CanvasGroup;
        var subBase = sub != null ? (Vector2)Member(sub, "Base") : Vector2.zero;
        var want = L.FromMaster(437f, 97f);
        Check("caught: OCCUPANT ACCOUNTED FOR under CAUGHT", subText != null && subText.text == "OCCUPANT ACCOUNTED FOR" && subGroup != null && subGroup.alpha > .9f && (subBase - want).magnitude < .5f,
            subText == null ? "missing" : subText.text + " alpha " + F(subGroup != null ? subGroup.alpha : -1f) + " at " + subBase + " want " + want);
        var labels = Field("caughtLabels") as System.Array;
        var third = labels != null ? Member(labels.GetValue(2), "Rect") as RectTransform : null;
        var thirdText = third != null ? third.GetComponent<UnityEngine.UI.Text>() : null;
        var targets = Field("caughtTargets") as int[];
        var keys = game.CaughtStats.Keys;
        Check("caught: the third number is KEYS TAKEN, counting keys", thirdText != null && thirdText.text == "KEYS TAKEN" && targets != null && targets[2] == keys,
            (thirdText != null ? thirdText.text : "missing") + " target " + (targets != null ? targets[2].ToString() : "?") + " keys " + keys);
        var numbers = Field("caughtNumbers") as System.Array;
        var firstNumberBase = numbers != null ? (Vector2)Member(numbers.GetValue(0), "Base") : Vector2.zero;
        // The view's own columns: four on the phone master's 750 pt, two on a compact window (the Duo's outer display).
        var statColumns = L.CompactMenus ? 2 : 4;
        var numberWant = L.FromMaster(437f - L.MenuWidth * .5f + L.MenuWidth / statColumns * .5f, 135f);
        Check("caught: numbers pushed 10 pt under the subtitle", (firstNumberBase - numberWant).magnitude < .5f, firstNumberBase + " want " + numberWant);
        controls.TryGetChip(FrontRoomsTouchControls.Button.TryAgain, out var tryCenter, out var trySize);
        var tryBottom = tryCenter.y - trySize.y * .5f;
        Check("caught: TRY AGAIN 10 pt lower, still clear of the safe area", Mathf.Abs(tryCenter.y - L.FromMaster(437f, L.CompactMenus ? 338f : 300f).y) < .5f && tryBottom >= L.SafeBottom + 8f,
            "centre " + tryCenter + " bottom " + F(tryBottom) + " safe " + F(L.SafeBottom));
        var sentence = typeof(FrontRooms3DGame).GetMethod("SentenceCase", Any);
        string Case(string s) => sentence != null ? (string)sentence.Invoke(null, new object[] { s }) : null;
        var b1 = Case("LEVEL B1 / THE MAZE");
        var front = Case("LEVEL 0 / FRONT ROOMS");
        var lobby = Case("LEVEL 0 / THE LOBBY");
        Check("caught: the sentence keeps floor tokens and Front Rooms", b1 == "Level B1 / The maze" && front == "Level 0 / Front Rooms" && lobby == "Level 0 / The lobby",
            b1 + " | " + front + " | " + lobby);
    }

    /// <summary>The USE control's drawn alpha (its group), from the view.</summary>
    float UseAlpha()
    {
        var view = game != null ? game.GetComponent<FrontRoomsTouchControlsView>() : null;
        var group = view != null ? view.GetType().GetField("useGroup", Any)?.GetValue(view) as CanvasGroup : null;
        return group != null ? group.alpha : -1f;
    }

    /// <summary>The touch layer's own colour at a point (pt), drawn over black: a control must be seen, not just hit.</summary>
    Color UiColourAt(Vector2 pt)
    {
        SetupCapture();
        Canvas.ForceUpdateCanvases();
        uiCam.backgroundColor = Color.black;
        uiCam.Render();
        var tex = Read(uiTarget);
        var scale = FrontRoomsHandheld.PointScale;
        var c = tex.GetPixel(Mathf.RoundToInt(pt.x * scale), Mathf.RoundToInt(pt.y * scale));
        Destroy(tex);
        return c;
    }

    static Texture2D Read(RenderTexture rt)
    {
        var active = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        RenderTexture.active = active;
        return tex;
    }

    static readonly float[] linTable = BuildLin();

    static float[] BuildLin()
    {
        var t = new float[256];
        for (var i = 0; i < 256; i++)
        {
            var c = i / 255f;
            t[i] = c <= .04045f ? c / 12.92f : Mathf.Pow((c + .055f) / 1.055f, 2.4f);
        }
        return t;
    }

    static float Lin(byte v) => linTable[v];

    static byte Enc(float l)
    {
        l = Mathf.Clamp01(l);
        var c = l <= .0031308f ? l * 12.92f : 1.055f * Mathf.Pow(l, 1f / 2.4f) - .055f;
        return (byte)Mathf.Clamp(Mathf.RoundToInt(c * 255f), 0, 255);
    }

    // ================================================================= report

    void Check(string what, bool pass, string detail)
    {
        if (!pass) failures++;
        report.Add((pass ? "PASS" : "FAIL") + " · " + what + (string.IsNullOrEmpty(detail) ? "" : " · " + detail));
        Debug.Log("[TouchPlaytest] " + (pass ? "PASS " : "FAIL ") + what + " " + detail);
    }

    void Note(string what, string detail)
    {
        report.Add("NOTE · " + what + " · " + detail);
        Debug.Log("[TouchPlaytest] NOTE " + what + " " + detail);
    }

    void Finish()
    {
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine("  \"profile\": \"" + FrontRoomsHandheld.ForcedProfile.Name + "\",");
        sb.AppendLine("  \"failures\": " + failures + ",");
        sb.AppendLine("  \"checks\": [");
        for (var i = 0; i < report.Count; i++) sb.AppendLine("    \"" + report[i].Replace("\\", "/").Replace("\"", "'").Replace("\r", "").Replace("\n", " / ").Replace("\t", " ") + "\"" + (i < report.Count - 1 ? "," : ""));
        sb.AppendLine("  ],");
        sb.AppendLine("  \"frames\": [");
        for (var i = 0; i < frames.Count; i++) sb.AppendLine("    \"" + frames[i] + "\"" + (i < frames.Count - 1 ? "," : ""));
        sb.AppendLine("  ],");
        sb.AppendLine("  \"haptics\": [");
        for (var i = 0; i < haptics.Count; i++) sb.AppendLine("    \"" + haptics[i] + "\"" + (i < haptics.Count - 1 ? "," : ""));
        sb.AppendLine("  ]");
        sb.AppendLine("}");
        File.WriteAllText(Path.Combine(outDir, "report.json"), sb.ToString());
        Time.captureDeltaTime = 0f;
        RestoreInput();
        RestoreTaught();
        FrontRoomsHandheld.ClearForce();
        UnityEditor.SessionState.SetBool(FailedKey, failures > 0);
        UnityEditor.SessionState.SetBool(DoneKey, true);
        Debug.Log("[TouchPlaytest] done: " + failures + " failures, " + shots + " frames → " + outDir);
    }

    void RestoreInput()
    {
        FrontRoomsTouchMotion.EditorClock = null;
        FrontRoomsMobileHaptics.Played -= OnHaptic;
        if (screen != null && screen.added) InputSystem.RemoveDevice(screen);
        screen = null;
        if (savedInputSettings != null) InputSystem.settings = savedInputSettings;
        savedInputSettings = null;
        if (harnessInputSettings != null) Destroy(harnessInputSettings);
        harnessInputSettings = null;
    }

    // Leaving Play Mode early (Stop button, timeout) still hands the Editor its own input settings back, and the
    // tester's X2 flags.
    void OnDestroy()
    {
        RestoreInput();
        RestoreTaught();
    }

    void RestoreTaught()
    {
        if (taughtRestored || savedTaughtSlow == int.MinValue) return;
        taughtRestored = true;
        if (savedTaughtSlow < 0) PlayerPrefs.DeleteKey(TaughtSlowKey); else PlayerPrefs.SetInt(TaughtSlowKey, savedTaughtSlow);
        if (savedTaughtRun < 0) PlayerPrefs.DeleteKey(TaughtRunKey); else PlayerPrefs.SetInt(TaughtRunKey, savedTaughtRun);
        PlayerPrefs.Save();
    }

    // ------------------------------------------------------------------ X2

    /// <summary>X2 check 2: the title lockup where 平面视觉's board puts it, the legend word for word, row B clear of the home inset.</summary>
    void X2TitleChecks()
    {
        var L = controls.Layout;
        var root = TouchNode("Title prompt");
        var rowA = TouchNode("Title legend 1");
        var rowB = TouchNode("Title legend 2");
        if (root == null || rowA == null || rowB == null) { Check("X2 title: the lockup is built", false, "missing nodes"); return; }
        var want = L.FromMaster(437f, 306f);
        Check("X2 title: TAP TO START centred at master y 306", (root.anchoredPosition - want).magnitude < .5f, V(root.anchoredPosition) + " vs " + V(want));
        var margin = root.anchoredPosition.y + rowB.anchoredPosition.y - 11f - L.SafeBottom;
        Check("X2 title: row B's glyphs end ≥ 8 pt above the safe bottom", margin >= 8f, F(margin) + " pt");
        Check("X2 title: row A reads MOVE · DRAG · LOOK · SLOW · UNHEARD · the run entry",
            LegendText(rowA, "MOVE") && LegendText(rowA, "DRAG · LOOK") && LegendText(rowA, "SLOW · UNHEARD") && LegendText(rowA, controls.RunStickMode ? "PAST THE RING · RUN" : "RUN BUTTON"), "");
        Check("X2 title: TAP TO START lost its chip", root.Find("chip") == null, "");
    }

    /// <summary>
    /// X2's slot words on the stick (spec v3.3 §7, checks 3–10): SLOW · UNHEARD with its dwell and retiring, the RUN
    /// preview and its hand-over to the running RUN, flicker, BUTTON mode, CONTROLS OPACITY and a high origin.
    /// </summary>
    IEnumerator X2Teaching(FrontRoomsTouchLayout L, Vector2 origin)
    {
        const string Slow = "SLOW · UNHEARD";
        FrontRoomsTouchControls.SetTaught(true, false);
        FrontRoomsTouchControls.SetTaught(false, false);
        SetReleased(false);
        var slowAt = origin + new Vector2(0f, 22f * L.Scale);

        // 3. Before the release: a ≥ 3 s creep shows SLOW · UNHEARD above the thumb, and it does not retire.
        FaceOpenWay("check 3");
        yield return Frames(.1f);
        Send(20, origin, InputTouchPhase.Began);
        yield return null;
        yield return Drag(20, origin, slowAt, .1f);
        yield return Frames(.5f);
        var label = SlotLabel();
        var above = label != null && label.rectTransform.anchoredPosition.y > (controls.StickThumb - controls.StickOrigin).y + 20f;
        Check("X2: a creep shows SLOW · UNHEARD above the thumb, at full alpha", SlotWordNow() == Slow && SlotAlpha() > .99f && above,
            "'" + SlotWordNow() + "' alpha " + F(SlotAlpha()) + ", above the thumb " + above);
        yield return Capture("x2_03_slow_above");
        // A real creep (it counts), so the check proves the release gate, not a wall.
        var countedBefore = 0;
        for (var i = 0; i < 162; i++)
        {
            yield return null;
            if (controls.Host != null && controls.Host.CreepCounts) countedBefore++;
        }
        Check("X2: before the Relay's release a 3 s creep does not retire SLOW · UNHEARD", !FrontRoomsTouchControls.TaughtSlow && countedBefore >= 90,
            countedBefore + " counted frames");
        Send(20, slowAt, InputTouchPhase.Ended);
        yield return Frames(.4f);

        // 4. Twenty quick walk starts pass through Slow and never show it (the 0.25 s dwell).
        var slowFrames = 0;
        for (var n = 0; n < 20; n++)
        {
            Send(21, origin, InputTouchPhase.Began);
            yield return null;
            Send(21, origin + new Vector2(0f, 25f * L.Scale), InputTouchPhase.Moved);
            yield return null;
            Send(21, origin + new Vector2(0f, 50f * L.Scale), InputTouchPhase.Moved);
            for (var i = 0; i < 8; i++) { yield return null; if (SlotWordNow() == Slow) slowFrames++; }
            Send(21, origin + new Vector2(0f, 50f * L.Scale), InputTouchPhase.Ended);
            for (var i = 0; i < 4; i++) { yield return null; if (SlotWordNow() == Slow) slowFrames++; }
        }
        Check("X2: 20 quick walk starts never show SLOW · UNHEARD", slowFrames == 0, slowFrames + " frames");
        // Turn round: the creeps below need room to move.
        game.GetType().GetField("yaw", Any).SetValue(game, GetFloat("yaw") + 180f);
        yield return Frames(.2f);

        // 7. A thumb jittering ±3 pt on the slow/walk edge for 2 s: the word never flickers.
        Send(22, origin, InputTouchPhase.Began);
        yield return null;
        var changes = 0;
        var lastWord = SlotWordNow();
        for (var i = 0; i < 120; i++)
        {
            Send(22, origin + new Vector2(0f, L.SlowRadius + (i % 4 < 2 ? 3f : -3f)), InputTouchPhase.Moved);
            yield return null;
            var now = SlotWordNow();
            if (now != lastWord) { changes++; lastWord = now; }
        }
        Send(22, origin + new Vector2(0f, L.SlowRadius), InputTouchPhase.Ended);
        Check("X2: a thumb jittering on the 36 pt edge never flickers the word", changes == 0, changes + " changes");
        yield return Frames(.4f);

        // 5. After the release (the test seam), a moving creep of 1.5 s retires SLOW; it leaves from the next touch.
        SetReleased(true);
        FaceOpenWay("check 5");
        yield return Frames(.1f);
        Send(23, origin, InputTouchPhase.Began);
        yield return null;
        yield return Drag(23, origin, slowAt, .1f);
        var counted = 0;
        for (var i = 0; i < 150 && !FrontRoomsTouchControls.TaughtSlow; i++)
        {
            yield return null;
            if (controls.Host != null && controls.Host.CreepCounts) counted++;
        }
        Check("X2: after the release, a moving creep of 1.5 s retires SLOW · UNHEARD", FrontRoomsTouchControls.TaughtSlow, counted + " counted frames");
        Check("X2: the retired word stays for the rest of that touch", SlotWordNow() == Slow, "'" + SlotWordNow() + "'");
        Send(23, slowAt, InputTouchPhase.Ended);
        yield return Frames(.4f);
        Send(24, origin, InputTouchPhase.Began);
        yield return null;
        yield return Drag(24, origin, slowAt, .1f);
        yield return Frames(.6f);
        Check("X2: from the next touch SLOW · UNHEARD no longer shows", SlotWordNow() != Slow, "'" + SlotWordNow() + "'");
        Send(24, slowAt, InputTouchPhase.Ended);
        yield return Frames(.4f);
        Note("X2", "check 5's creep-into-a-wall and EXAMINE-hold cases are not driven in this run");

        // 6. The RUN preview: in the reach band at 100 %, the running RUN in place at the crossing, retired by it.
        // SLOW · UNHEARD teaches again too, for the step back into the slow band after the run.
        FrontRoomsTouchControls.SetTaught(true, false);
        FrontRoomsTouchControls.SetTaught(false, false);
        var reachAt = origin + new Vector2(0f, 70f * L.Scale);
        var crossAt = origin + new Vector2(0f, L.RunLineRadius + 6f);
        Send(25, origin, InputTouchPhase.Began);
        yield return null;
        yield return Drag(25, origin, reachAt, .1f);
        yield return Frames(.3f);
        Check("X2: the RUN preview shows in the reach band, in paper, at full alpha", SlotWordNow() == "RUN" && !SlotYellow() && SlotAlpha() > .99f && !controls.Running,
            "'" + SlotWordNow() + "' alpha " + F(SlotAlpha()) + ", yellow " + SlotYellow());
        yield return Capture("x2_06_preview");
        Send(25, crossAt, InputTouchPhase.Moved);
        var minAlpha = 1f;
        for (var i = 0; i < 12; i++) { yield return null; minAlpha = Mathf.Min(minAlpha, SlotAlpha()); }
        Check("X2: at the crossing the preview turns into the running RUN in place", controls.Running && SlotWordNow() == "RUN" && SlotYellow() && minAlpha > .99f,
            "running " + controls.Running + ", lowest alpha " + F(minAlpha));
        Check("X2: the first stick crossing retires the preview", FrontRoomsTouchControls.TaughtRun, "");
        // Back into the slow band in one frame (20 pt ahead; the centre is the dead zone): the yellow RUN only fades
        // out, never flashes back, and SLOW · UNHEARD comes in only after its 0.25 s dwell.
        Send(25, origin + new Vector2(0f, 20f * L.Scale), InputTouchPhase.Moved);
        var yellowWas = SlotYellow() && SlotWordNow() == "RUN" ? SlotAlpha() : 0f;
        var flashed = false;
        var slowIn = -1;
        for (var i = 0; i < 40; i++)
        {
            yield return null;
            var yellow = SlotYellow() && SlotWordNow() == "RUN" ? SlotAlpha() : 0f;
            if (yellow > yellowWas + .001f) flashed = true;
            yellowWas = yellow;
            if (slowIn < 0 && SlotWordNow() == Slow) slowIn = i;
        }
        Check("X2: back in the slow band, the yellow RUN only fades out (no flash)", !flashed && yellowWas <= 0f, "flash " + flashed + ", yellow alpha at 0.67 s " + F(yellowWas));
        Check("X2: back in the slow band, SLOW · UNHEARD comes in only after its dwell", slowIn >= 0 && slowIn * Step >= .25f,
            slowIn >= 0 ? "after " + F(slowIn * Step) + " s" : "never");
        Send(25, origin + new Vector2(0f, 20f * L.Scale), InputTouchPhase.Ended);
        yield return Frames(.5f);
        // The running RUN cuts in over SLOW · UNHEARD and is full within 0.14 s of the crossing.
        FrontRoomsTouchControls.SetTaught(true, false);
        Send(26, origin, InputTouchPhase.Began);
        yield return null;
        yield return Drag(26, origin, slowAt, .1f);
        yield return Frames(.5f);
        var slowBefore = SlotWordNow() == Slow;
        Send(26, crossAt, InputTouchPhase.Moved);
        int crossed = -1, full = -1;
        for (var i = 0; i < 24 && full < 0; i++)
        {
            yield return null;
            if (crossed < 0 && controls.Running) crossed = i;
            if (crossed >= 0 && SlotWordNow() == "RUN" && SlotYellow() && SlotAlpha() > .99f) full = i;
        }
        var took = crossed >= 0 && full >= 0 ? (full - crossed) * Step : -1f;
        Check("X2: the running RUN cuts in over SLOW · UNHEARD within 0.14 s", slowBefore && took >= 0f && took <= .14f + Step,
            "SLOW before " + slowBefore + ", RUN full after " + (took >= 0f ? F(took) + " s" : "never"));
        Send(26, crossAt, InputTouchPhase.Ended);
        yield return Frames(.5f);
        // A retired preview stays away from the next touch.
        Send(27, origin, InputTouchPhase.Began);
        yield return null;
        yield return Drag(27, origin, reachAt, .1f);
        yield return Frames(.3f);
        Check("X2: a retired preview no longer shows", SlotWordNow() != "RUN", "'" + SlotWordNow() + "'");
        Send(27, reachAt, InputTouchPhase.Ended);
        yield return Frames(.4f);
        // The preview's other way out: 4 s shown in all. It retires, and stays for the rest of that touch.
        FrontRoomsTouchControls.SetTaught(false, false);
        Send(40, origin, InputTouchPhase.Began);
        yield return null;
        yield return Drag(40, origin, reachAt, .1f);
        yield return Frames(.2f);
        // Most of the 4 s already shown (the harness skips ahead instead of walking the player 13 m).
        FrontRoomsTouchControls.PreviewShownFor = 3.8f;
        yield return Frames(.4f);
        Check("X2: shown 4 s in all, the RUN preview retires but stays for that touch", FrontRoomsTouchControls.TaughtRun && SlotWordNow() == "RUN",
            "taught " + FrontRoomsTouchControls.TaughtRun + ", '" + SlotWordNow() + "', shown " + F(FrontRoomsTouchControls.PreviewShownFor) + " s");
        Send(40, reachAt, InputTouchPhase.Ended);
        yield return Frames(.4f);
        // STICK HINTS back ON clears the flag: the preview gets a fresh 4 s, not the old clock.
        FrontRoomsTouchControls.SetTaught(false, false);
        Send(41, origin, InputTouchPhase.Began);
        yield return null;
        yield return Drag(41, origin, reachAt, .1f);
        yield return Frames(.5f);
        Check("X2: after STICK HINTS turns back on, the RUN preview teaches with a fresh 4 s",
            !FrontRoomsTouchControls.TaughtRun && SlotWordNow() == "RUN" && FrontRoomsTouchControls.PreviewShownFor < 1f,
            "taught " + FrontRoomsTouchControls.TaughtRun + ", '" + SlotWordNow() + "', shown " + F(FrontRoomsTouchControls.PreviewShownFor) + " s");
        Send(41, reachAt, InputTouchPhase.Ended);
        yield return Frames(.4f);
        FrontRoomsTouchControls.SetTaught(false, true);

        // 8. BUTTON mode: no preview and no slot RUN; with a fixed stick, SLOW · UNHEARD hangs below the ring.
        var socketWas = FrontRoomsSettings.TouchSprintSocket;
        var floatingWas = FrontRoomsSettings.TouchFloatingStick;
        FrontRoomsSettings.SetTouchSprintSocket(false);
        FrontRoomsTouchControls.SetTaught(true, false);
        FrontRoomsTouchControls.SetTaught(false, false);
        yield return Frames(.1f);
        Send(28, origin, InputTouchPhase.Began);
        yield return null;
        yield return Drag(28, origin, reachAt, .1f);
        yield return Frames(.3f);
        Check("X2 BUTTON: no RUN preview", SlotWordNow() != "RUN", "'" + SlotWordNow() + "'");
        Send(28, reachAt, InputTouchPhase.Ended);
        yield return Frames(.3f);
        FrontRoomsSettings.SetTouchFloatingStick(false);
        yield return Frames(.1f);
        var rest = controls.Layout.StickRest;
        Send(29, rest, InputTouchPhase.Began);
        yield return null;
        yield return Drag(29, rest, rest + new Vector2(0f, 22f * L.Scale), .1f);
        yield return Frames(.5f);
        label = SlotLabel();
        var below = label != null && label.rectTransform.anchoredPosition.y < -40f;
        Check("X2 BUTTON, fixed stick: SLOW · UNHEARD hangs below the ring", SlotWordNow() == Slow && below && SlotPlaceOf("Slow") == "Below",
            "'" + SlotWordNow() + "' at " + (label != null ? V(label.rectTransform.anchoredPosition) : "-") + ", " + SlotPlaceOf("Slow"));
        yield return Capture("x2_08_button_fixed_below");
        // 9. CONTROLS OPACITY at its lowest: the word keeps full alpha.
        var opacityWas = FrontRoomsSettings.TouchOpacityPercent;
        for (var i = 0; i < 20; i++) FrontRoomsSettings.StepTouchOpacity(-1);
        yield return Frames(.2f);
        Check("X2: at the lowest CONTROLS OPACITY the word keeps full alpha", SlotWordNow() == Slow && SlotAlpha() > .99f,
            FrontRoomsSettings.TouchOpacityPercent + "% → alpha " + F(SlotAlpha()));
        for (var i = 0; i < 20 && FrontRoomsSettings.TouchOpacityPercent < opacityWas; i++) FrontRoomsSettings.StepTouchOpacity(1);
        Send(29, rest + new Vector2(0f, 22f * L.Scale), InputTouchPhase.Ended);
        yield return Frames(.3f);
        FrontRoomsTouchControls.SetTaught(true, true);
        Check("X2 BUTTON: STICK HINTS reads OFF once SLOW is learned", FrontRoomsTouchControls.TouchRowValue(FrontRoomsTouchControls.TouchRow.StickHints) == "OFF",
            FrontRoomsTouchControls.TouchRowValue(FrontRoomsTouchControls.TouchRow.StickHints));
        FrontRoomsSettings.SetTouchFloatingStick(floatingWas);
        FrontRoomsSettings.SetTouchSprintSocket(socketWas);
        yield return Frames(.1f);

        // 10. A high origin (y 270): the RUN words never draw above the thumb; they go below or hide.
        FrontRoomsTouchControls.SetTaught(false, false);
        // As high as y 270 on the iPhone: 132 pt (x Scale) under the frame's top edge, on every device.
        var high = new Vector2(origin.x, L.Frame.y - 132f * L.Scale);
        Send(30, high, InputTouchPhase.Began);
        yield return Frames(.1f);
        // The ring starts under the finger, or as close as the screen edges, a camera and the rounded corners allow.
        var highTaken = controls.StickTouched && (controls.StickOrigin - L.FloatingOrigin(high)).magnitude < 1f;
        Check("X2: from a high origin the RUN words never draw above the thumb", highTaken && SlotPlaceOf("Preview") != "Above" && SlotPlaceOf("Run") != "Above",
            "origin " + V(controls.StickOrigin) + ", preview " + SlotPlaceOf("Preview") + ", run " + SlotPlaceOf("Run") + ", slow " + SlotPlaceOf("Slow") + ", winded " + SlotPlaceOf("Winded"));
        Send(30, high, InputTouchPhase.Ended);
        yield return Frames(.3f);
        FrontRoomsTouchControls.SetTaught(true, false);
        FrontRoomsTouchControls.SetTaught(false, false);
        Note("X2", "check 11 (legibility on lit ground) is read from frames x2_03 / x2_06 for 平面视觉; check 10's LEFT-HANDED and key-held origins are not driven here");
    }

    /// <summary>How far a rect (touch-canvas points) keeps from the nearest cutout; large when there is none near.</summary>
    /// <summary>How far a circle stays inside the nearest rounded corner's curve (999 when no corner reaches it).</summary>
    static float CornerGap(Vector2 c, float radius)
    {
        var s = FrontRoomsHandheld.Shape;
        var gap = 999f;
        for (var i = 0; i < 4; i++)
        {
            var round = s.Corners[i];
            if (round <= radius + FrontRoomsHandheld.HudCornerGap) continue;
            var left = i == 0 || i == 2;
            var bottom = i < 2;
            var k = new Vector2(left ? round : s.Frame.x - round, bottom ? round : s.Frame.y - round);
            if ((left ? c.x < k.x : c.x > k.x) && (bottom ? c.y < k.y : c.y > k.y)) gap = Mathf.Min(gap, round - radius - Vector2.Distance(c, k));
        }
        return gap;
    }

    static float CutoutGap(Rect r)
    {
        var gap = 999f;
        foreach (var c in FrontRoomsHandheld.Shape.Cutouts)
        {
            var dx = Mathf.Max(0f, Mathf.Max(c.xMin - r.xMax, r.xMin - c.xMax));
            var dy = Mathf.Max(0f, Mathf.Max(c.yMin - r.yMax, r.yMin - c.yMax));
            gap = Mathf.Min(gap, dx > 0f && dy > 0f ? Mathf.Sqrt(dx * dx + dy * dy) : Mathf.Max(dx, dy));
        }
        return gap;
    }

    /// <summary>The settings heading, BACK and every row and section drawn on screen keep 12 pt from every cutout.</summary>
    bool SettingsClearOfCutouts(out string worst)
    {
        var ok = true;
        var least = 999f;
        worst = "none near";
        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (canvas.name != "Handheld touch UI") continue;
            var root = (RectTransform)canvas.transform;
            foreach (var node in canvas.GetComponentsInChildren<RectTransform>(false))
            {
                if (node.name != "Row" && node.name != "Section" && node.name != "SETTINGS") continue;
                var corners = new Vector3[4];
                node.GetWorldCorners(corners);
                var a = root.InverseTransformPoint(corners[0]);
                var b = root.InverseTransformPoint(corners[2]);
                var r = Rect.MinMaxRect(a.x - root.rect.xMin, a.y - root.rect.yMin, b.x - root.rect.xMin, b.y - root.rect.yMin);
                if (node.name == "SETTINGS") r.width = Mathf.Min(r.width, node.GetComponent<UnityEngine.UI.Text>().preferredWidth);
                if (r.yMax < 0f || r.yMin > root.rect.height) continue;
                var g = CutoutGap(r);
                if (g < least) { least = g; worst = node.name + " " + F(g) + " pt at " + r; }
                ok &= g >= 11.5f;
            }
        }
        if (controls.TryGetChip(FrontRoomsTouchControls.Button.SettingsClose, out var back, out var size))
        {
            var g = CutoutGap(new Rect(back - size * .5f, size));
            if (g < least) { least = g; worst = "BACK " + F(g) + " pt"; }
            ok &= g >= 11.5f;
        }
        return ok;
    }

    RectTransform TouchNode(string name)
    {
        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (canvas.name != "Handheld touch UI") continue;
            foreach (var t in canvas.GetComponentsInChildren<RectTransform>(true)) if (t.name == name) return t;
        }
        return null;
    }

    static bool LegendText(RectTransform row, string text)
    {
        foreach (var t in row.GetComponentsInChildren<UnityEngine.UI.Text>(true)) if (t.text == text) return true;
        return false;
    }

    UnityEngine.UI.Text SlotLabel()
    {
        var stick = TouchNode("Stick / live");
        var label = stick != null ? stick.Find("state label") : null;
        return label != null ? label.GetComponent<UnityEngine.UI.Text>() : null;
    }

    string SlotWordNow()
    {
        var t = SlotLabel();
        return t != null && t.color.a > .01f ? t.text : string.Empty;
    }

    float SlotAlpha()
    {
        var t = SlotLabel();
        return t != null ? t.color.a : 0f;
    }

    // The running RUN is yellow #F4DF3B; every other word is paper #F4F1E8: the blue channel tells them apart.
    bool SlotYellow()
    {
        var t = SlotLabel();
        return t != null && t.color.b < .5f;
    }

    string SlotPlaceOf(string word)
    {
        var view = game.GetComponent<FrontRoomsTouchControlsView>();
        var places = view.GetType().GetField("slotPlaces", Any)?.GetValue(view) as Array;
        var words = view.GetType().GetNestedType("SlotWord", Any);
        return places == null || words == null ? "?" : places.GetValue((int)Enum.Parse(words, word)).ToString();
    }

    void SetReleased(bool on) => game.GetType().GetField("mobileRelayEverReleased", Any).SetValue(game, on);

    static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);
    static string V(Vector2 v) => "(" + F(v.x) + ", " + F(v.y) + ")";
    static string V3(Vector3 v) => "(" + F(v.x) + ", " + F(v.y) + ", " + F(v.z) + ")";
}
#endif
