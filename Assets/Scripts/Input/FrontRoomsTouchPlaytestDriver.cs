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
        FrontRoomsMobileHaptics.Played += (c, s, i) => haptics.Add(F(Time.unscaledTime - t0) + "," + c + "," + s + "," + F(i));
        t0 = Time.unscaledTime;
        Note("profile", FrontRoomsHandheld.ForcedProfile.Name + " " + FrontRoomsHandheld.ScreenPixels + " @" + F(FrontRoomsHandheld.PointScale));

        var scenario = Scenario();
        while (true)
        {
            object current;
            try
            {
                if (!scenario.MoveNext()) break;
                current = scenario.Current;
            }
            catch (Exception e)
            {
                Check("scenario ran without exception", false, e.GetType().Name + ": " + e.Message + " @ " + e.StackTrace.Split('\n')[0]);
                break;
            }
            yield return current;
        }
        Finish();
    }

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
        yield return TapWithFrames(L.PauseCenter, 6, "08_pause", new[] { 1, 4, 8, 12, 18, 30 });
        Check("pause button pauses", Phase() == "Paused", Phase());
        Check("touch layer shows the pause card", controls.CurrentMenuState == FrontRoomsTouchControls.MenuState.Paused, controls.CurrentMenuState.ToString());
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

        // ------------------------------------------------------------- USE: door
        if (StandAt(col => Describe(col, out var hold) is string d && !hold && d.Contains("OPEN DOOR"), 1.4f, out var door))
        {
            for (var i = 0; i < 16; i++)
            {
                yield return null;
                if (i == 1 || i == 4 || i == 8 || i == 15) yield return Capture("15_use_in_f" + i.ToString("00"));
            }
            Check("USE appears on a door", controls.CurrentUsePrompt.Visible && controls.CurrentUsePrompt.Kind == FrontRoomsTouchControls.UseKind.Open, controls.CurrentUsePrompt.Kind.ToString());
            var before = Describe(door, out _);
            Send(9, L.UseCenter, InputTouchPhase.Began);
            for (var i = 0; i < 6; i++) { yield return null; if (i == 1 || i == 4) yield return Capture("16_use_press_f" + i.ToString("00")); }
            Send(9, L.UseCenter, InputTouchPhase.Ended);
            for (var i = 0; i < 12; i++) { yield return null; if (i == 3 || i == 11) yield return Capture("17_use_release_f" + i.ToString("00")); }
            yield return Frames(1.2f);
            var after = Describe(door, out _);
            Check("USE opens the door", after != before, before + " → " + after);
            Check("USE press played a control haptic", haptics.Exists(h => h.Contains("Controls,Rigid")), "");
            yield return Capture("18_door_opened_use_says_shut");
        }
        else Check("found a door to test USE", false, "no OPEN DOOR within 30 m");

        // ------------------------------------------------------------ USE: glass
        if (StandAt(col => Describe(col, out var hold) != null && hold, .95f, out var pane))
        {
            yield return Frames(.35f);
            Check("USE turns into BREAK on glass", controls.CurrentUsePrompt.Kind == FrontRoomsTouchControls.UseKind.Hold, controls.CurrentUsePrompt.Kind.ToString());
            yield return Capture("19_glass_ready");
            Send(10, L.UseCenter, InputTouchPhase.Began);
            var broke = false;
            for (var i = 0; i < 120 && !broke; i++)
            {
                yield return null;
                if (i == 18 || i == 36 || i == 50) yield return Capture("20_glass_hold_f" + i.ToString("000"));
                broke = pane == null || Describe(pane, out _) == null;
            }
            Send(10, L.UseCenter, InputTouchPhase.Ended);
            Check("holding USE breaks the glass", broke, "");
            Check("glass: rising buzz and a heavy hit", haptics.Exists(h => h.Contains("Gameplay,Light")) && haptics.Exists(h => h.Contains("Gameplay,Heavy")), "");
            yield return Frames(.6f);
            yield return Capture("21_glass_broken");
        }
        else Note("glass", "no pane within 30 m (skipped)");

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
        yield return TapWithFrames(controls.Layout.PauseCenter, 13, "23_reduced_pause", new[] { 1, 4, 8 });
        Check("reduce motion: the card is in place by 0.13 s (no slide)", Phase() == "Paused", "");
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
    bool StandAt(Func<Collider, bool> want, float distance, out Collider target)
    {
        target = null;
        var feet = PlayerRoot.position;
        var best = float.MaxValue;
        foreach (var c in Physics.OverlapSphere(feet, 30f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (!want(c)) continue;
            var d = (c.bounds.center - feet).sqrMagnitude;
            if (d < best) { best = d; target = c; }
        }
        if (target == null) return false;
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
            Note("stand", target.name + " at " + F(distance) + " m");
            return true;
        }
        return false;
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

    IEnumerator TapWithFrames(Vector2 p, int id, string prefix, int[] at)
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
