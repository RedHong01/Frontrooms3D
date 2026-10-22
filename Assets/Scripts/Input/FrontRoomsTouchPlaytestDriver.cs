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
        yield return Frames(3.6f);
        Check("title waits for a tap", Phase() == "Title", Phase());
        yield return Capture("01_title_tap_to_start");
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
        // The top band of TS 1–4 (Figma 2528:5403): the level label hangs from the top-left. From 10-04 to 10-22 the phone
        // layout anchored it to the bottom edge, so it was drawn below the screen and no check noticed.
        var roomRect = HudRect("Room typography");
        Check("HUD: the level label is on screen, top-left", roomRect.HasValue && roomRect.Value.xMin >= 0f && roomRect.Value.xMin < .5f && roomRect.Value.xMax <= 1f && roomRect.Value.yMin >= .5f && roomRect.Value.yMax <= 1f,
            roomRect.HasValue ? "rect " + F(roomRect.Value.xMin) + "," + F(roomRect.Value.yMin) + " – " + F(roomRect.Value.xMax) + "," + F(roomRect.Value.yMax) + " of the HUD (y up)" : "no Room typography");
        yield return Capture("02_calm_ghost_stick");

        var L = controls.Layout;
        HoldRelay();
        Note("layout", "rest " + V(L.StickRest) + " use " + V(L.UseCenter) + " pause " + V(L.PauseCenter) + " frame " + V(L.Frame));

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
        FrontRooms3DGame.HandheldNoiseEmitted += onNoise;
        yield return Drag(2, origin, origin + new Vector2(0f, 22f), .2f);
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
        yield return Drag(2, origin + new Vector2(0f, 22f), origin + new Vector2(0f, 50f), .15f);
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
        yield return Drag(2, origin + new Vector2(0f, 50f), origin + new Vector2(0f, 74f), .12f);
        yield return Frames(.3f);
        Check("walk: past the ring but short of the line does not run", !controls.Running && !FrontRoomsInput.Snapshot.SprintHeld && controls.CurrentGait == FrontRoomsGait.Walk,
            controls.CurrentGait + ", reach " + F(controls.RunReach));
        yield return Capture("04r_reaching");

        // Run: across the line, ahead. One rigid click, then the desktop sprint's speed.
        var hapticsBefore = haptics.Count;
        var line = origin + new Vector2(0f, L.RunLineRadius + 10f);
        yield return Drag(2, origin + new Vector2(0f, 74f), line, .1f);
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
        yield return Drag(2, line, origin + new Vector2(40f, 58f), .2f);
        yield return Frames(.2f);
        Check("run: between the ring and the line it keeps running", controls.Running, V(controls.StickThumb - controls.StickOrigin));
        // Back inside the ring: walk again.
        yield return Drag(2, origin + new Vector2(40f, 58f), origin + new Vector2(0f, 45f), .15f);
        yield return Frames(.1f);
        Check("run: back inside the ring walks", !controls.Running && controls.CurrentGait == FrontRoomsGait.Walk, controls.CurrentGait.ToString());
        // A hard strafe past the line never runs: the line is only ahead.
        var strafe = origin + new Vector2(L.RunLineRadius + 12f, 0f);
        yield return Drag(2, origin + new Vector2(0f, 45f), strafe, .15f);
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
        FrontRooms3DGame.HandheldNoiseEmitted -= onNoise;
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
            Send(4, origin + new Vector2(0f, Mathf.Min(60f, i * 6f)), InputTouchPhase.Moved);
            Send(5, new Vector2(650f + i * 3f, 160f), InputTouchPhase.Moved);
            yield return null;
        }
        Check("two thumbs: moves and looks together", (PlayerPos() - startPos).magnitude > .5f && Mathf.Abs(Mathf.DeltaAngle(yaw0, GetFloat("yaw"))) > 8f,
            F((PlayerPos() - startPos).magnitude) + " m, " + F(Mathf.DeltaAngle(yaw0, GetFloat("yaw"))) + " deg");
        Send(4, origin + new Vector2(0f, 60f), InputTouchPhase.Ended);
        Send(5, new Vector2(710f, 160f), InputTouchPhase.Ended);
        yield return Frames(.4f);

        // ------------------------------------------------------------ pause etc
        var chipY = new float[40];
        yield return TapWithFrames(L.PauseCenter, 6, "08_pause", new[] { 1, 4, 8, 12, 18, 30 }, i => chipY[i] = NodeY("Chip RESUME"));
        Check("pause button pauses", Phase() == "Paused", Phase());
        Check("touch layer shows the pause card", controls.CurrentMenuState == FrontRoomsTouchControls.MenuState.Paused, controls.CurrentMenuState.ToString());
        // QUIT TO TITLE is a separate lower chip, on the dark ink chip, so it cannot be confused with RESTART.
        var titleShown = controls.TryGetChip(FrontRoomsTouchControls.Button.ReturnTitle, out var titleChip, out var titleChipSize);
        Check("pause: QUIT TO TITLE chip is hit-testable under the row",
            titleShown && titleChipSize.x >= 100f && titleChip.y < controls.Layout.Center.y - 100f,
            "center " + V(titleChip) + " size " + V(titleChipSize));
        if (titleShown)
        {
            // Drawn, and dark: its pixel against the frost just below it (both drawn by the touch layer, over black).
            var chipInk = UiColourAt(titleChip + new Vector2(-titleChipSize.x * .5f + 6f, 0f));
            var frostBelow = UiColourAt(titleChip + new Vector2(0f, -titleChipSize.y * .5f - 10f));
            Check("pause: QUIT TO TITLE is drawn on the dark chip", chipInk.grayscale < .1f && frostBelow.grayscale > .3f,
                "chip " + F(chipInk.grayscale) + ", frost " + F(frostBelow.grayscale));
        }
        // RESUME waits 160 ms, then rises 12 pt over 260 ms: still low at f08, nearly home at f18, home at f30.
        Check("motion: the chips rise into place", chipY[8] < chipY[18] - .5f && chipY[18] <= chipY[30] + .01f, "f08 " + F(chipY[8]) + " f18 " + F(chipY[18]) + " f30 " + F(chipY[30]));
        yield return ChipTap(FrontRoomsTouchControls.Button.Settings, "09_settings", new[] { 1, 4, 8, 14, 24 });
        Check("SETTINGS opens the settings card", GetBool("displaySettingsOpen") && controls.CurrentMenuState == FrontRoomsTouchControls.MenuState.Settings, "");
        yield return Frames(.4f);
        var captions = FrontRoomsSettings.Captions;
        var captionsRow = FindRow("CAPTIONS");
        Check("settings: the desktop rows are on the touch card", captionsRow.HasValue, "");
        if (captionsRow.HasValue) yield return Tap(captionsRow.Value.Rect.center, 7);
        yield return Frames(.25f);
        Check("settings: tapping CAPTIONS toggles it", FrontRoomsSettings.Captions != captions, FrontRoomsSettings.Captions.ToString());
        yield return Capture("10_settings_row_changed");
        if (captionsRow.HasValue) yield return Tap(captionsRow.Value.Rect.center, 7);
        var look = FrontRoomsSettings.TouchLookSpeedPercent;
        var lookRow = FindRow("LOOK SPEED");
        if (lookRow.HasValue) yield return Tap(new Vector2(lookRow.Value.Rect.xMax - 10f, lookRow.Value.Rect.center.y), 7);
        yield return Frames(.2f);
        Check("settings: › steps LOOK SPEED up", FrontRoomsSettings.TouchLookSpeedPercent > look, look + " → " + FrontRoomsSettings.TouchLookSpeedPercent);
        if (lookRow.HasValue) yield return Tap(new Vector2(lookRow.Value.Rect.xMax - 80f, lookRow.Value.Rect.center.y), 7);
        yield return Frames(.2f);
        Check("settings: ‹ steps LOOK SPEED back", FrontRoomsSettings.TouchLookSpeedPercent == look, FrontRoomsSettings.TouchLookSpeedPercent.ToString());
        var vp = controls.SettingsScrollViewport;
        yield return Swipe(8, new Vector2(vp.center.x, vp.yMin + 30f), new Vector2(vp.center.x, vp.yMin + 160f), .3f);
        yield return Frames(.3f);
        Check("settings: the TOUCH column scrolls", controls.SettingsScroll > 20f, F(controls.SettingsScroll) + " / " + F(controls.SettingsScrollMax));
        yield return Capture("11_settings_scrolled");
        // BACK must be drawn, not just hit-testable: an opaque card once hid it while taps still worked.
        if (controls.TryGetChip(FrontRoomsTouchControls.Button.SettingsClose, out var backAt, out var backSize))
        {
            var seen = UiColourAt(backAt + new Vector2(-backSize.x * .5f + 6f, 0f));
            Check("settings: BACK is visible on the card", seen.r > .8f && seen.g > .7f && seen.b < .45f, "rgb " + F(seen.r) + " " + F(seen.g) + " " + F(seen.b));
        }
        yield return ChipTap(FrontRoomsTouchControls.Button.SettingsClose, "12_settings_close", new[] { 1, 6, 14 });
        Check("BACK returns to the pause card", !GetBool("displaySettingsOpen") && controls.CurrentMenuState == FrontRoomsTouchControls.MenuState.Paused, "");
        // The frost around the card is a way back too.
        yield return Frames(.4f);
        yield return ChipTap(FrontRoomsTouchControls.Button.Settings, null, null);
        yield return Frames(.4f);
        yield return Tap(new Vector2(30f, L.Frame.y * .5f), 7);
        yield return Frames(.3f);
        Check("settings: a tap on the frost outside the card goes back", !GetBool("displaySettingsOpen") && controls.CurrentMenuState == FrontRoomsTouchControls.MenuState.Paused, "");
        yield return Frames(.4f);
        yield return ChipTap(FrontRoomsTouchControls.Button.Restart, "13_restart_confirm", new[] { 2, 14 });
        Check("RESTART asks first", controls.RestartConfirmationOpen && Phase() == "Paused", "");
        yield return ChipTap(FrontRoomsTouchControls.Button.RestartCancel, null, null);
        Check("CANCEL keeps the run", !controls.RestartConfirmationOpen && Phase() == "Paused", "");
        yield return Frames(.4f);
        yield return ChipTap(FrontRoomsTouchControls.Button.Resume, "14_resume", new[] { 1, 6, 12 });
        Check("RESUME returns to play", Phase() == "Playing", Phase());
        yield return Frames(.4f);

        // -------------------------------------------------------- Android back
        // The bridge's own queue (as the predictive-back callback fills it): one step back each time.
        FrontRoomsMobileBackBridge.SimulateBackPressed();
        yield return Frames(.1f);
        Check("back: pauses the run", Phase() == "Paused", Phase());
        yield return ChipTap(FrontRoomsTouchControls.Button.Settings, null, null);
        yield return Frames(.3f);
        FrontRoomsMobileBackBridge.SimulateBackPressed();
        yield return Frames(.1f);
        Check("back: closes settings first", !GetBool("displaySettingsOpen") && Phase() == "Paused", "");
        yield return ChipTap(FrontRoomsTouchControls.Button.Restart, null, null);
        yield return Frames(.2f);
        FrontRoomsMobileBackBridge.SimulateBackPressed();
        yield return Frames(.1f);
        Check("back: cancels the restart question", !controls.RestartConfirmationOpen && Phase() == "Paused", "");
        FrontRoomsMobileBackBridge.SimulateBackPressed();
        yield return Frames(.1f);
        Check("back: resumes", Phase() == "Playing", Phase());
        yield return Frames(.4f);

        HoldRelay();

        // ------------------------------------------------------------- doors
        // A shut door faces the player, so the tap lands reliably: tap the door itself to open it (HIG: tap the
        // object), then USE shuts it again (the verb crossfades) and the latch is felt.
        if (StandAt(MapColliders("doorByCollider"), col => Describe(col, out var hold) is string d && !hold && d.Contains("OPEN DOOR"), 1.4f, out var door))
        {
            for (var i = 0; i < 16; i++)
            {
                yield return null;
                if (i == 1 || i == 4 || i == 8 || i == 15) yield return Capture("15_use_in_f" + i.ToString("00"));
            }
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
        if (StandAt(MapColliders("doorByCollider"), col => Describe(col, out var hold) is string d && d.StartsWith("LOCKED"), 1.4f, out var locked))
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
        if (toBreak != null)
        {
            var tries = 0;
            Action<FrontRoomsMapWorld.Door, Vector3> onTry = (d, p) => { if (d != null && d.edge == toBreak.edge) tries++; };
            Map.DoorBrokenTried += onTry;
            Map.BreakDoor(toBreak, toBreak.position + (toBreak.position - PlayerRoot.position));
            yield return Frames(1.2f);
            Func<Collider, bool> isBroken = col => Map.DoorOf(col) is FrontRoomsMapWorld.Door d && d.edge == toBreak.edge && d.broken;
            Collider brokenLeaf = null;
            if (StandAt(MapColliders("doorByCollider"), isBroken, 1.4f, out brokenLeaf) || StandAt(MapColliders("doorByCollider"), isBroken, 1f, out brokenLeaf))
            {
                yield return Frames(.35f);
                var usePrompt = controls.CurrentUsePrompt;
                Check("broken: the crosshair on the hanging leaf turns USE into BROKEN", usePrompt.Visible && usePrompt.Kind == FrontRoomsTouchControls.UseKind.Broken,
                    usePrompt.Kind + ", desktop prompt '" + Describe(brokenLeaf, out _) + "'");
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
        keys.Sort((a, b) => (a.bounds.center - PlayerRoot.position).sqrMagnitude.CompareTo((b.bounds.center - PlayerRoot.position).sqrMagnitude));
        // Until 关卡设计's line (KeyPickupNeedsE on handhelds, queued with A12) lands, phones still walk over keys: skip these steps.
        if (Map.KeyPickupNeedsE) Check("keys: phones take keys on purpose (each key carries a look+tap trigger)", keys.Count > 0, keys.Count + " keys");
        else { Note("keys", "KeyPickupNeedsE is off on handhelds (walk-over keys): the key-tap steps are skipped"); keys.Clear(); }
        var keyIndex = 0;
        Collider NextKey(float distance)
        {
            for (; keyIndex < keys.Count; keyIndex++)
                if (keys[keyIndex] != null && PlaceAtKey(keys[keyIndex], distance)) return keys[keyIndex++];
            return null;
        }
        var keyA = NextKey(1.2f);
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
        }
        else Note("glass", "no intact window on the map (skipped)");

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
            if (i == 20) Send(11, L.Center + new Vector2(0f, -89f), InputTouchPhase.Ended);
            // Android Back is TRY AGAIN too, behind the same 0.6 s guard.
            if (i == 24) FrontRoomsMobileBackBridge.SimulateBackPressed();
        }
        Check("caught: an early tap or Back does not restart", Phase() == "Caught", Phase());
        Check("caught: heavy fade haptic", haptics.Exists(h => h.Contains("Gameplay,Heavy")), "");

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
    }

    // ================================================================ helpers

    string Phase() => game.GetType().GetField("phase", Any).GetValue(game).ToString();
    float GetFloat(string field) => (float)game.GetType().GetField(field, Any).GetValue(game);
    bool GetBool(string field) => (bool)game.GetType().GetField(field, Any).GetValue(game);
    Transform PlayerRoot => (Transform)game.GetType().GetField("playerRoot", Any).GetValue(game);
    Vector3 PlayerPos() { var p = PlayerRoot.position; p.y = 0f; return p; }
    FrontRoomsMapWorld Map => (FrontRoomsMapWorld)game.GetType().GetField("map", Any).GetValue(game);
    string Describe(Collider c, out bool hold) { hold = false; return c == null || Map == null ? null : Map.Describe(c, out hold); }

    FrontRoomsTouchControls.SettingsSlot? FindRow(string label)
    {
        foreach (var s in controls.SettingsSlots) if (!s.Header && s.Label == label) return s;
        return null;
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

    bool PlaceAt(Collider target, float distance, Vector3 feet)
    {
        var center = target.bounds.center;
        var body = (CharacterController)game.GetType().GetField("playerBody", Any).GetValue(game);
        for (var k = 0; k < 16; k++)
        {
            var a = k * Mathf.PI / 8f;
            var spot = new Vector3(center.x + Mathf.Sin(a) * distance, feet.y, center.z + Mathf.Cos(a) * distance);
            var eye = spot + Vector3.up * 1.6f;
            if (Physics.CheckCapsule(spot + Vector3.up * .35f, spot + Vector3.up * 1.6f, .25f, ~0, QueryTriggerInteraction.Ignore)) continue;
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

    /// <summary>Stands the player <paramref name="distance"/> m from a key, on the floor, looking at it, when the key is in plain view (KeyOnRay).</summary>
    bool PlaceAtKey(Collider key, float distance)
    {
        var center = key.bounds.center;
        var feet = PlayerRoot.position;
        var body = (CharacterController)game.GetType().GetField("playerBody", Any).GetValue(game);
        for (var k = 0; k < 16; k++)
        {
            var a = k * Mathf.PI / 8f;
            var spot = new Vector3(center.x + Mathf.Sin(a) * distance, feet.y, center.z + Mathf.Cos(a) * distance);
            var eye = spot + Vector3.up * 1.6f;
            if (Physics.CheckCapsule(spot + Vector3.up * .35f, spot + Vector3.up * 1.6f, .25f, ~0, QueryTriggerInteraction.Ignore)) continue;
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
        Destroy(result);
        Destroy(sceneTex);
        Destroy(black);
        Destroy(white);
        shots++;
        yield break;
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
        for (var i = 0; i < report.Count; i++) sb.AppendLine("    \"" + report[i].Replace("\\", "/").Replace("\"", "'") + "\"" + (i < report.Count - 1 ? "," : ""));
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

    // Leaving Play Mode early (Stop button, timeout) still hands the Editor its own input settings back.
    void OnDestroy() => RestoreInput();

    static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);
    static string V(Vector2 v) => "(" + F(v.x) + ", " + F(v.y) + ")";
}
#endif
