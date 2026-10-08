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

        yield return Tap(new Vector2(437f, 201f), 1);
        yield return WaitFor(() => Phase() == "Playing", 8f, "a tap on the title starts the run");
        if (aborted) yield break;
        yield return Frames(2.5f);
        Check("touch layer in Playing state", controls.CurrentMenuState == FrontRoomsTouchControls.MenuState.Playing, controls.CurrentMenuState.ToString());
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

        // A forward push to the ring's edge walks; it must never sprint (Alien: Isolation's failure).
        yield return Drag(2, origin, origin + new Vector2(0f, 62f), .25f);
        var startPos = PlayerPos();
        yield return Frames(1.0f);
        Check("walk: forward push moves forward", FrontRoomsInput.Snapshot.Move.y > .9f, V(FrontRoomsInput.Snapshot.Move));
        Check("walk: a push to the ring edge does not sprint", !controls.SprintLatched && !FrontRoomsInput.Snapshot.SprintHeld, "latched " + controls.SprintLatched);
        var walked = (PlayerPos() - startPos).magnitude;
        Check("walk: player moved about Walk speed (3.2 m/s)", walked > 2.0f && walked < 4.0f, F(walked) + " m in 1 s");
        yield return Capture("04_walk");

        // The socket: reach past the ring, hold 150 ms, then sprint.
        var socket = origin + new Vector2(0f, L.SocketOffset);
        yield return Drag(2, origin + new Vector2(0f, 62f), socket, .12f);
        for (var i = 0; i < 24; i++)
        {
            yield return null;
            if (i == 1 || i == 4 || i == 7 || i == 9 || i == 11 || i == 14 || i == 20) yield return Capture("05_socket_f" + i.ToString("00"));
        }
        Check("socket: dwell latches sprint", controls.SprintLatched, "arm " + F(controls.SocketArm));
        startPos = PlayerPos();
        yield return Frames(1.0f);
        Check("sprint: game is sprinting", FrontRooms3DGame.PlayerSprinting, "");
        var ran = (PlayerPos() - startPos).magnitude;
        Check("sprint: player moved about Run speed (5.5 m/s)", ran > 4.0f && ran < 6.5f, F(ran) + " m in 1 s");
        Check("sprint: socket latch played a control haptic", haptics.Exists(h => h.Contains("Controls,Light")), haptics.Count + " pulses");
        yield return Capture("06_sprinting");
        // Steering while latched: slide sideways but stay forward: still sprinting.
        yield return Drag(2, socket, socket + new Vector2(30f, -40f), .2f);
        yield return Frames(.2f);
        Check("sprint: steering in the upper half keeps sprint", controls.SprintLatched, V(controls.StickThumb - controls.StickOrigin));

        // Pull back: walks again (and is the desktop S for shots).
        yield return Drag(2, socket + new Vector2(30f, -40f), origin + new Vector2(0f, -45f), .2f);
        yield return Frames(.1f);
        Check("pull back ends the sprint", !controls.SprintLatched, "");
        Send(2, origin + new Vector2(0f, -45f), InputTouchPhase.Ended);
        for (var i = 0; i < 18; i++)
        {
            yield return null;
            if (i == 0 || i == 3 || i == 6 || i == 10 || i == 17) yield return Capture("07_release_f" + i.ToString("00"));
        }
        Check("release: stick lets go", !controls.StickTouched, "");

        // --------------------------------------------------------------- winded
        // Keep the thumb in the socket until the 5 s of stamina run dry: the stick says WINDED, sprint stops
        // (the desktop's Shift-on-an-empty-bar rule), then comes back by itself once a segment refills.
        Send(2, origin, InputTouchPhase.Began);
        yield return Frames(.1f);
        yield return Drag(2, origin, socket, .15f);
        var windedAt = -1;
        for (var i = 0; i < 480 && windedAt < 0; i++)
        {
            yield return null;
            if (FrontRooms3DGame.PlayerWinded) windedAt = i;
        }
        Check("winded: the sprint runs dry", windedAt >= 0, windedAt >= 0 ? F(windedAt * Step) + " s more" : "never");
        for (var i = 0; i < 16; i++)
        {
            yield return null;
            if (i == 0 || i == 5 || i == 10 || i == 15) yield return Capture("07w_winded_f" + i.ToString("00"));
        }
        Check("winded: the stick says WINDED and sprint stops", controls.Winded && !FrontRoomsInput.Snapshot.SprintHeld && !FrontRooms3DGame.PlayerSprinting, "winded " + controls.Winded);
        yield return CaptureDark("07z_label_winded_dark");
        Check("winded: soft double haptic", haptics.FindAll(h => h.Contains("Controls,Soft")).Count >= 2, "");
        var recovered = -1;
        for (var i = 0; i < 300 && recovered < 0; i++)
        {
            yield return null;
            if (!FrontRooms3DGame.PlayerWinded) recovered = i;
        }
        Check("winded: breath back after about 2 s", recovered >= 0, recovered >= 0 ? F(recovered * Step) + " s" : "never");
        // The socket refused the thumb while winded; still held up, it re-arms (150 ms) like a held Shift.
        yield return Frames(.3f);
        Check("winded: a thumb still in the socket sprints again", controls.SprintLatched && FrontRoomsInput.Snapshot.SprintHeld, "latched " + controls.SprintLatched);
        yield return Capture("07x_sprint_again");
        yield return CaptureDark("07z_label_sprint_dark");
        Send(2, socket, InputTouchPhase.Ended);
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
        yield return ChipTap(FrontRoomsTouchControls.Button.SettingsClose, "12_settings_close", new[] { 1, 6, 14 });
        Check("CLOSE returns to the pause card", !GetBool("displaySettingsOpen") && controls.CurrentMenuState == FrontRoomsTouchControls.MenuState.Paused, "");
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
        }
        else Note("locked", "no locked door on the map (skipped)");
        if (keysField != null && !keysWere) keysField.SetValue(Map, false);

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
            for (var i = 0; i < 150 && brokeAt < 0; i++)
            {
                yield return null;
                if (i == 18 || i == 36 || i == 50) yield return Capture("20_glass_hold_f" + i.ToString("000"));
                if (window.pane == null) brokeAt = i;
            }
            Send(10, L.UseCenter, InputTouchPhase.Ended);
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
        }
        Check("caught: an early tap does not restart", Phase() == "Caught", Phase());
        Check("caught: heavy fade haptic", haptics.Exists(h => h.Contains("Gameplay,Heavy")), "");

        // ------------------------------------------------ reduce motion pass
        FrontRoomsHandheld.EditorReduceMotion = true;
        Note("reduce-motion", "on");
        var reloaded = false;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, m) => reloaded = true;
        yield return ChipTap(FrontRoomsTouchControls.Button.TryAgain, null, null);
        yield return WaitFor(() => reloaded, 8f, "TRY AGAIN restarts the run");
        if (aborted) yield break;
        game = null;
        yield return WaitFor(() => (game = FindFirstObjectByType<FrontRooms3DGame>()) != null && (controls = game.GetComponent<FrontRoomsTouchControls>()) != null, 10f, "new run's touch layer");
        if (aborted) yield break;
        converted.Clear();
        SetupCapture();
        yield return Frames(1f);
        yield return Tap(new Vector2(437f, 201f), 12);
        yield return WaitFor(() => Phase() == "Playing", 8f, "second run starts");
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
        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (!canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay || converted.Contains(canvas)) continue;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = uiCam;
            canvas.planeDistance = 1f;
            foreach (var t in canvas.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 5;
            converted.Add(canvas);
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
