// FrontRoomsKeyBindingTests: the desktop key bindings behind CONTROLS / KEYBOARD & MOUSE and CONTROLS / CONTROLLER
// (FrontRoomsInput's Bind API, Red 2026-10-29: players remap their keys on the map page; 2026-10-30: SPRINT on Ctrl, SLOW on
// Shift; 2026-10-31: the pad's buttons, LEFT CLICK kept for ZOOM, the legend lines, the pad's menu buttons, locked keys kept
// out of swaps and resets; 2026-11-01: a pad's family, a pad coming and going, the cached legend, the touch sheet's capture).
// EditMode, no scene. Batch: -executeMethod FrontRoomsKeyBindingTests.RunBatch -quit. Uses its own
// PlayerPrefs key and deletes it, so the player's own bindings are never read or written. Pins Xbox pad names and English
// while it runs (a PlayStation pad paired to the Mac, an Editor left in another language: the same result), and reads the
// legend's letters as the keyboard in use prints them (AZERTY: the same result). A check this machine cannot make (a pad
// paired where none must be, a virtual pad the batch cannot press) is skipped with a note, never passed. ASCII, no tuples
// (editor-script rule).
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Bind = FrontRoomsInput.Bind;
using BindCheck = FrontRoomsInput.BindCheck;

public static class FrontRoomsKeyBindingTests
{
    const string TestPrefKey = "FrontRooms.KeyBindings.Tests";
    static readonly List<string> lines = new List<string>();
    static int failed, skipped;

    public static void RunBatch()
    {
        var ok = Run();
        EditorApplication.Exit(ok ? 0 : 1);
    }

    [MenuItem("FrontRooms/Tests/Key Bindings")]
    public static void RunFromMenu() => Run();

    static bool Run()
    {
        lines.Clear();
        failed = skipped = 0;
        var playerKey = FrontRoomsInput.BindingsPrefKey;
        var language = FrontRoomsLanguage.Current;
        FrontRoomsInput.BindingsPrefKey = TestPrefKey;
        PlayerPrefs.DeleteKey(TestPrefKey);
        try
        {
            FrontRoomsInput.PlayStationPadOverride = false;     // Xbox names whatever pad is paired (P4, P15)
            FrontRoomsLanguage.PreviewForTest(FrontRoomsLanguage.All[0]);  // English (P15, P31, P32)
            FrontRoomsInput.Shutdown();
            Defaults();
            Names();
            Checks();
            AssignAndPrompts();
            Persistence();
            SwapAndStepBack();
            Resets();
            Suspension();
            Migration();
            Layouts();
            Pads();
            LeftClickAndLegends();
            LockedSwaps();
            DeviceState();
        }
        catch (Exception e)
        {
            Check(false, "no exception: " + e.GetType().Name + " " + e.Message);
        }
        finally
        {
            FrontRoomsBindCapture.Cancel();
            FrontRoomsInput.AppleKeysOverride = null;
            FrontRoomsInput.PlayStationPadOverride = null;
            FrontRoomsInput.Shutdown();
            FrontRoomsInput.ResetAllBinds();
            FrontRoomsInput.ResetAllPadBinds();
            PlayerPrefs.DeleteKey(TestPrefKey);
            FrontRoomsInput.BindingsPrefKey = playerKey;
            FrontRoomsInput.Shutdown();
            FrontRoomsLanguage.PreviewForTest(language);
        }
        var verdict = (failed == 0 ? "PASS: " : "FAIL: ") + (lines.Count - failed - skipped) + " passed, " + failed + " failed"
            + (skipped > 0 ? ", " + skipped + " skipped" : "");
        var report = "[KeyBindingTests] " + verdict + "\n" + string.Join("\n", lines.ToArray());
        if (failed == 0) Debug.Log(report); else Debug.LogError(report);
        return failed == 0;
    }

    static void Defaults()
    {
        Check(Eq(FrontRoomsInput.PathOf(Bind.Use), "<Keyboard>/e"), "USE starts on E (" + FrontRoomsInput.PathOf(Bind.Use) + ")");
        Check(Eq(FrontRoomsInput.PathOf(Bind.Forward), "<Keyboard>/w") && Eq(FrontRoomsInput.PathOf(Bind.Back), "<Keyboard>/s")
            && Eq(FrontRoomsInput.PathOf(Bind.Left), "<Keyboard>/a") && Eq(FrontRoomsInput.PathOf(Bind.Right), "<Keyboard>/d"),
            "the walk starts on W A S D");
        Check(Eq(FrontRoomsInput.PathOf(Bind.Sprint), "<Keyboard>/leftCtrl") && Eq(FrontRoomsInput.PathOf(Bind.Slow), "<Keyboard>/leftShift")
            && Eq(FrontRoomsInput.PathOf(Bind.Restart), "<Keyboard>/r") && Eq(FrontRoomsInput.PathOf(Bind.Zoom), "<Mouse>/rightButton"),
            "SPRINT Left Ctrl, SLOW Left Shift, END RUN R, ZOOM the right button");
        Check(Array.IndexOf(FrontRoomsInput.Binds, Bind.Slow) == Array.IndexOf(FrontRoomsInput.Binds, Bind.Sprint) + 1, "SLOW comes after SPRINT in Binds");
        Check(FrontRoomsInput.AllDefault, "all default at first");
        Check(FrontRoomsInput.UseKey == "E" && FrontRoomsInput.WithKeyNames("E  \u00B7  OPEN DOOR") == "E  \u00B7  OPEN DOOR", "prompts unchanged while USE is E");
        Check(Eq(FrontRoomsInput.StepBackPath, "<Keyboard>/s"), "S steps back out of a close-up");
    }

    static void Names()
    {
        string[,] table =
        {
            { "<Keyboard>/leftShift", "SHIFT" }, { "<Keyboard>/rightShift", "SHIFT" }, { "<Keyboard>/leftCtrl", "CTRL" }, { "<Keyboard>/upArrow", "UP" },
            { "<Keyboard>/comma", "COMMA" }, { "<Keyboard>/f5", "F5" }, { "<Keyboard>/numpad4", "NUM 4" }, { "<Keyboard>/space", "SPACE" },
            { "<Keyboard>/1", "1" }, { "<Mouse>/leftButton", "LEFT CLICK" }, { "<Mouse>/rightButton", "RIGHT CLICK" },
            { "<Mouse>/forwardButton", "MOUSE 5" }, { "<Keyboard>/escape", "ESC" }, { "", "UNBOUND" },
        };
        var wrong = new List<string>();
        for (var i = 0; i < table.GetLength(0); i++)
        {
            var got = FrontRoomsInput.KeyName(table[i, 0]);
            if (got != table[i, 1]) wrong.Add(table[i, 0] + " -> " + got);
        }
        Check(wrong.Count == 0, "key names (words where Bayon has no glyph)" + (wrong.Count > 0 ? ": " + string.Join(", ", wrong.ToArray()) : ""));
    }

    static void Checks()
    {
        Bind owner; string locked;
        Check(FrontRoomsInput.CheckBind(Bind.Use, "<Keyboard>/escape", out owner, out locked) == BindCheck.Reserved, "Esc is never bound (PAUSE keeps it)");
        Check(FrontRoomsInput.CheckBind(Bind.Use, "<Mouse>/scroll/y", out owner, out locked) == BindCheck.Reserved, "the wheel is never bound (ZOOM STEP keeps it)");
        Check(FrontRoomsInput.CheckBind(Bind.Use, "<Keyboard>/upArrow", out owner, out locked) == BindCheck.Locked && locked == "MOVE", "an arrow is MOVE's locked key (" + locked + ")");
        var sprintLock = FrontRoomsInput.SprintLockPath;
        Check(FrontRoomsInput.CheckBind(Bind.Use, sprintLock, out owner, out locked) == BindCheck.Locked && locked == "SPRINT", "Right " + FrontRoomsInput.KeyName(sprintLock) + " is SPRINT's locked key");
        Check(FrontRoomsInput.CheckBind(Bind.Use, "<Keyboard>/rightShift", out owner, out locked) == BindCheck.Locked && locked == "SLOW", "Right Shift is SLOW's locked key");
        Check(FrontRoomsInput.CheckBind(Bind.Sprint, sprintLock, out owner, out locked) == BindCheck.Ok, "SPRINT may take its own locked key");
        Check(FrontRoomsInput.CheckBind(Bind.Slow, "<Keyboard>/rightShift", out owner, out locked) == BindCheck.Ok, "SLOW may take its own locked key");
        Check(FrontRoomsInput.CheckBind(Bind.Sprint, "<Keyboard>/rightShift", out owner, out locked) == BindCheck.Locked && locked == "SLOW", "SPRINT may not take SLOW's locked key");
        Check(FrontRoomsInput.CheckBind(Bind.Use, "<Keyboard>/leftShift", out owner, out locked) == BindCheck.Taken && owner == Bind.Slow, "Left Shift is taken by SLOW");
        Check(FrontRoomsInput.CheckBind(Bind.Use, "<Keyboard>/leftCtrl", out owner, out locked) == BindCheck.Taken && owner == Bind.Sprint, "Left Ctrl is taken by SPRINT");
        Check(FrontRoomsInput.CheckBind(Bind.Use, "<keyboard>/E", out owner, out locked) == BindCheck.Same, "E for USE is the same (any spelling)");
        Check(FrontRoomsInput.CheckBind(Bind.Use, "<Keyboard>/r", out owner, out locked) == BindCheck.Taken && owner == Bind.Restart, "R is taken by RESTART");
        Check(FrontRoomsInput.CheckBind(Bind.Use, "<Keyboard>/f", out owner, out locked) == BindCheck.Ok, "F is free");
        Check(FrontRoomsInput.CheckBind(Bind.Use, "<Keyboard>/space", out owner, out locked) == BindCheck.Ok, "Space is free in play (START only reads it on the title)");
    }

    static void AssignAndPrompts()
    {
        var changed = 0;
        Action count = () => changed++;
        FrontRoomsInput.BindingsChanged += count;
        FrontRoomsInput.Assign(Bind.Use, "<Keyboard>/f");
        FrontRoomsInput.BindingsChanged -= count;
        Check(Eq(FrontRoomsInput.PathOf(Bind.Use), "<Keyboard>/f") && !FrontRoomsInput.IsDefault(Bind.Use) && !FrontRoomsInput.AllDefault, "USE on F");
        Check(changed == 1, "BindingsChanged once (" + changed + ")");
        Check(FrontRoomsInput.UseKey == "F", "USE key reads F");
        Check(FrontRoomsInput.WithKeyNames("E  \u00B7  OPEN DOOR") == "F  \u00B7  OPEN DOOR", "prompt: F  .  OPEN DOOR");
        Check(FrontRoomsInput.WithKeyNames("HOLD E  \u00B7  BREAK GLASS") == "HOLD F  \u00B7  BREAK GLASS", "prompt: HOLD F  .  BREAK GLASS");
        Check(FrontRoomsInput.WithKeyNames("TAP E  \u00B7  BREAK GLASS") == "TAP F  \u00B7  BREAK GLASS", "prompt: TAP F  .  BREAK GLASS");
        Check(FrontRoomsInput.WithKeyNames("E  TURN OFF") == "F  TURN OFF", "screen prompt: F  TURN OFF");
        Check(FrontRoomsInput.WithKeyNames("EXIT") == "EXIT" && FrontRoomsInput.WithKeyNames("LOCKED  \u00B7  NEEDS THIS ZONE'S KEY") == "LOCKED  \u00B7  NEEDS THIS ZONE'S KEY", "other words untouched");
        Check(PlayerPrefs.HasKey(TestPrefKey) && PlayerPrefs.GetString(TestPrefKey) == "Use=<Keyboard>/f", "saved to PlayerPrefs (" + PlayerPrefs.GetString(TestPrefKey) + ")");
    }

    static void Persistence()
    {
        FrontRoomsInput.Shutdown();
        Check(Eq(FrontRoomsInput.PathOf(Bind.Use), "<Keyboard>/f") && FrontRoomsInput.UseKey == "F", "USE on F after a restart (loaded from PlayerPrefs)");
        Check(Eq(FrontRoomsInput.PathOf(Bind.Restart), "<Keyboard>/r"), "RESTART untouched after the reload");
    }

    static void SwapAndStepBack()
    {
        FrontRoomsInput.SwapInto(Bind.Restart, "<Keyboard>/f");
        Check(Eq(FrontRoomsInput.PathOf(Bind.Restart), "<Keyboard>/f") && Eq(FrontRoomsInput.PathOf(Bind.Use), "<Keyboard>/r"), "swap: RESTART takes F, USE takes RESTART's R");
        FrontRoomsInput.Assign(Bind.Back, "<Keyboard>/x");
        Check(Eq(FrontRoomsInput.StepBackPath, "<Keyboard>/x"), "the close-up step back follows Back (X)");
        FrontRoomsInput.ResetBind(Bind.Back);
        Check(Eq(FrontRoomsInput.PathOf(Bind.Back), "<Keyboard>/s") && Eq(FrontRoomsInput.StepBackPath, "<Keyboard>/s"), "Back and the step back return to S");
    }

    static void Resets()
    {
        FrontRoomsInput.Assign(Bind.Sprint, "<Keyboard>/e");
        FrontRoomsInput.ResetBind(Bind.Use);
        Check(Eq(FrontRoomsInput.PathOf(Bind.Use), "<Keyboard>/e") && Eq(FrontRoomsInput.PathOf(Bind.Sprint), "<Keyboard>/r"),
            "USE back to E; SPRINT, which had E, takes USE's R (" + FrontRoomsInput.PathOf(Bind.Sprint) + ")");
        FrontRoomsInput.ResetAllBinds();
        Check(FrontRoomsInput.AllDefault && Eq(FrontRoomsInput.PathOf(Bind.Restart), "<Keyboard>/r") && Eq(FrontRoomsInput.PathOf(Bind.Sprint), "<Keyboard>/leftCtrl")
            && Eq(FrontRoomsInput.PathOf(Bind.Slow), "<Keyboard>/leftShift"), "all defaults again");
        Check(!PlayerPrefs.HasKey(TestPrefKey), "nothing saved while everything is default");
        FrontRoomsInput.Shutdown();
        Check(FrontRoomsInput.AllDefault, "still default after a reload");
    }

    static void Suspension()
    {
        FrontRoomsInput.SuspendGameplay = true;
        FrontRoomsInput.BeginFrame();
        var f = FrontRoomsInput.ReadFrame();
        FrontRoomsInput.SuspendGameplay = false;
        FrontRoomsInput.BeginFrame();
        Check(f.Move == Vector2.zero && !f.UseDown && !f.RestartDown && !f.PauseDown && !f.ShotBackDown && !f.SprintHeld && !f.SlowHeld, "a suspended frame reads nothing");
        var g = new FrontRoomsInput.FrameSnapshot(Vector2.zero, Vector2.zero, textToggleDown: true, slowHeld: true);
        Check(g.SlowHeld && g.TextToggleDown && !FrontRoomsInput.FrameSnapshot.Empty.SlowHeld, "SlowHeld is the snapshot's last parameter, false when empty");
    }

    // Keys saved before 2026-10-30 (SPRINT moved to Ctrl, SLOW took Shift): a saved key a default now uses goes back to its
    // own default; a swap the player made stays; a key that is now locked is ignored.
    static void Migration()
    {
        Check(Reload("Use=<Keyboard>/leftCtrl") && FrontRoomsInput.AllDefault && !PlayerPrefs.HasKey(TestPrefKey),
            "USE saved on Left Ctrl: back to E, SPRINT keeps Ctrl, the stale save is cleared");
        Check(Reload("Sprint=<Keyboard>/e|Use=<Keyboard>/leftShift") && FrontRoomsInput.AllDefault,
            "the old Shift/E swap: all defaults (" + FrontRoomsInput.PathOf(Bind.Use) + ", " + FrontRoomsInput.PathOf(Bind.Sprint) + ")");
        Check(Reload("Use=<Keyboard>/r|Restart=<Keyboard>/e") && Eq(FrontRoomsInput.PathOf(Bind.Use), "<Keyboard>/r") && Eq(FrontRoomsInput.PathOf(Bind.Restart), "<Keyboard>/e"),
            "a player's E/R swap stays");
        Check(Reload("Sprint=<Keyboard>/f") && Eq(FrontRoomsInput.PathOf(Bind.Sprint), "<Keyboard>/f") && Eq(FrontRoomsInput.PathOf(Bind.Slow), "<Keyboard>/leftShift"),
            "SPRINT saved on F stays; SLOW keeps Shift");
        Check(Reload("Use=" + FrontRoomsInput.SprintLockPath) && FrontRoomsInput.IsDefault(Bind.Use), "USE saved on SPRINT's lock: ignored");
        FrontRoomsInput.ResetAllBinds();
    }

    // SPRINT's locked second key by keyboard (2026-10-30): Right Option on Apple keyboards (no Right Ctrl on a MacBook or a
    // Magic Keyboard; Ctrl + arrows switches Spaces), Right Ctrl elsewhere. Both layouts, whatever machine runs the test.
    static void Layouts()
    {
        foreach (var apple in new[] { true, false })
        {
            FrontRoomsInput.AppleKeysOverride = apple;
            FrontRoomsInput.Shutdown();
            var name = apple ? "Apple: " : "PC: ";
            var lockPath = apple ? "<Keyboard>/rightAlt" : "<Keyboard>/rightCtrl";
            var other = apple ? "<Keyboard>/rightCtrl" : "<Keyboard>/rightAlt";
            Bind owner; string locked;
            Check(Eq(FrontRoomsInput.SprintLockPath, lockPath) && FrontRoomsInput.DesktopBoundPaths().Contains(FrontRoomsInput.Normalise(lockPath))
                && !FrontRoomsInput.DesktopBoundPaths().Contains(FrontRoomsInput.Normalise(other)), name + "SPRINT's second key is " + lockPath);
            Check(FrontRoomsInput.LockedOwner(lockPath) == "SPRINT" && FrontRoomsInput.LockedOwner(other) == null, name + "only that key is locked");
            Check(FrontRoomsInput.CheckBind(Bind.Use, other, out owner, out locked) == BindCheck.Ok, name + other + " is free");
            Check(FrontRoomsInput.KeyName("<Keyboard>/rightAlt") == (apple ? "OPTION" : "ALT") && FrontRoomsInput.KeyName("<Keyboard>/leftMeta") == (apple ? "CMD" : "WIN"),
                name + "names " + FrontRoomsInput.KeyName("<Keyboard>/rightAlt") + ", " + FrontRoomsInput.KeyName("<Keyboard>/leftMeta"));
            Check(Reload("Use=" + lockPath) && FrontRoomsInput.IsDefault(Bind.Use), name + "USE saved on the lock is ignored");
            Check(Reload("Use=" + other) && Eq(FrontRoomsInput.PathOf(Bind.Use), other), name + "USE saved on " + other + " stays");
            FrontRoomsInput.ResetAllBinds();
        }
        FrontRoomsInput.AppleKeysOverride = null;
        FrontRoomsInput.Shutdown();
    }

    // The pad's buttons on CONTROLS / CONTROLLER (2026-10-31): four targets of their own, saved after the keys under their own
    // names; MENU, the right stick and the d-pad's up and down are never bound, L3 is SPRINT's locked button, and a map binds
    // its own device only. Names are an Xbox pad's (Run pins them).
    static void Pads()
    {
        Bind owner; string locked;
        Check(Eq(FrontRoomsInput.PathOf(Bind.PadUse), "<Gamepad>/buttonSouth") && Eq(FrontRoomsInput.PathOf(Bind.PadSprint), "<Gamepad>/leftShoulder")
            && Eq(FrontRoomsInput.PathOf(Bind.PadBack), "<Gamepad>/buttonEast") && Eq(FrontRoomsInput.PathOf(Bind.PadZoom), "<Gamepad>/rightTrigger"),
            "the pad starts on A, LB, B and RT");
        var apart = FrontRoomsInput.PadBinds.Length == 4 && FrontRoomsInput.PadAllDefault;
        foreach (var b in FrontRoomsInput.PadBinds) apart = apart && FrontRoomsInput.IsPad(b);
        foreach (var b in FrontRoomsInput.Binds) apart = apart && !FrontRoomsInput.IsPad(b);
        Check(apart, "four pad targets, apart from the keys");
        var bound = FrontRoomsInput.PadBoundPaths();
        var inUse = new[] { "<gamepad>/buttonsouth", "<gamepad>/buttoneast", "<gamepad>/leftshoulder", "<gamepad>/leftstickpress", "<gamepad>/righttrigger",
            "<gamepad>/start", "<gamepad>/leftstick", "<gamepad>/rightstick", "<gamepad>/dpad/down", "<gamepad>/dpad/up" };
        var free = new[] { "<gamepad>/buttonwest", "<gamepad>/buttonnorth", "<gamepad>/rightshoulder", "<gamepad>/lefttrigger", "<gamepad>/select" };
        var wrongUse = new List<string>();
        foreach (var path in inUse) if (!bound.Contains(path)) wrongUse.Add(path + " free");
        foreach (var path in free) if (bound.Contains(path)) wrongUse.Add(path + " in use");
        Check(wrongUse.Count == 0, "the pad's buttons in use on the desktop" + Report(wrongUse));
        string[,] table =
        {
            { "<Gamepad>/buttonSouth", "A" }, { "<Gamepad>/buttonEast", "B" }, { "<Gamepad>/buttonWest", "X" }, { "<Gamepad>/buttonNorth", "Y" },
            { "<Gamepad>/leftShoulder", "LB" }, { "<Gamepad>/rightShoulder", "RB" }, { "<Gamepad>/leftTrigger", "LT" }, { "<Gamepad>/rightTrigger", "RT" },
            { "<Gamepad>/leftStickPress", "LS" }, { "<Gamepad>/rightStickPress", "RS" }, { "<Gamepad>/select", "VIEW" }, { "<Gamepad>/start", "MENU" },
            { "<Gamepad>/dpad/up", "D-PAD UP" }, { "<Gamepad>/dpad/left", "D-PAD LEFT" }, { "<Gamepad>/leftStick", "LEFT STICK" }, { "<Gamepad>/rightStick", "RIGHT STICK" },
        };
        var wrong = Misnamed(table);
        if (FrontRoomsInput.ControlName(Bind.PadUse) != "A") wrong.Add("PadUse -> " + FrontRoomsInput.ControlName(Bind.PadUse));
        Check(wrong.Count == 0, "pad names (Xbox, forced)" + Report(wrong));
        Check(FrontRoomsInput.CheckBind(Bind.PadUse, "<Gamepad>/start", out owner, out locked) == BindCheck.Reserved, "MENU is never bound (PAUSE keeps it)");
        Check(FrontRoomsInput.CheckBind(Bind.PadUse, "<Gamepad>/rightStick", out owner, out locked) == BindCheck.Reserved
            && FrontRoomsInput.CheckBind(Bind.PadUse, "<Gamepad>/dpad/up", out owner, out locked) == BindCheck.Reserved
            && FrontRoomsInput.CheckBind(Bind.PadUse, "<Gamepad>/dpad/down", out owner, out locked) == BindCheck.Reserved,
            "the right stick and the d-pad's up and down are never bound");
        Check(FrontRoomsInput.CheckBind(Bind.PadUse, "<Gamepad>/leftStickPress", out owner, out locked) == BindCheck.Locked && locked == "SPRINT", "L3 is SPRINT's locked button (" + locked + ")");
        Check(FrontRoomsInput.CheckBind(Bind.PadSprint, "<Gamepad>/leftStickPress", out owner, out locked) == BindCheck.Ok, "SPRINT may take L3");
        Check(FrontRoomsInput.CheckBind(Bind.PadUse, "<Gamepad>/leftShoulder", out owner, out locked) == BindCheck.Taken && owner == Bind.PadSprint, "LB is taken by SPRINT (" + owner + ")");
        Check(FrontRoomsInput.CheckBind(Bind.PadUse, "<Gamepad>/buttonWest", out owner, out locked) == BindCheck.Ok
            && FrontRoomsInput.CheckBind(Bind.PadUse, "<Gamepad>/dpad/left", out owner, out locked) == BindCheck.Ok, "X and the d-pad's left are free");
        Check(FrontRoomsInput.CheckBind(Bind.Use, "<Gamepad>/buttonWest", out owner, out locked) == BindCheck.Reserved
            && FrontRoomsInput.CheckBind(Bind.PadUse, "<Keyboard>/f", out owner, out locked) == BindCheck.Reserved, "one device per map: no pad button for a key, no key for a button");
        Check(FrontRoomsInput.CheckBind(Bind.PadUse, "<gamepad>/BUTTONSOUTH", out owner, out locked) == BindCheck.Same, "A for USE is the same (any spelling)");

        FrontRoomsInput.Assign(Bind.PadUse, "<Gamepad>/buttonWest");
        Check(Eq(FrontRoomsInput.PathOf(Bind.PadUse), "<Gamepad>/buttonWest") && !FrontRoomsInput.PadAllDefault && FrontRoomsInput.AllDefault, "USE on X; the keys untouched");
        Check(PlayerPrefs.GetString(TestPrefKey) == "PadUse=<Gamepad>/buttonWest", "saved under the pad's own name (" + PlayerPrefs.GetString(TestPrefKey) + ")");
        Check(FrontRoomsInput.ControlName(Bind.PadUse) == "X" && FrontRoomsInput.WithPadNames("E  \u00B7  OPEN DOOR") == "X  \u00B7  OPEN DOOR"
            && FrontRoomsInput.WithPadNames("HOLD E  \u00B7  BREAK GLASS") == "HOLD X  \u00B7  BREAK GLASS" && FrontRoomsInput.WithKeyNames("E  \u00B7  OPEN DOOR") == "E  \u00B7  OPEN DOOR",
            "pad prompts: X  .  OPEN DOOR, HOLD X  .  BREAK GLASS; the keys' prompt keeps E");
        FrontRoomsInput.Assign(Bind.Use, "<Keyboard>/f");
        Check(PlayerPrefs.GetString(TestPrefKey) == "Use=<Keyboard>/f|PadUse=<Gamepad>/buttonWest", "keys first, then the pad (" + PlayerPrefs.GetString(TestPrefKey) + ")");
        FrontRoomsInput.Shutdown();
        Check(Eq(FrontRoomsInput.PathOf(Bind.Use), "<Keyboard>/f") && Eq(FrontRoomsInput.PathOf(Bind.PadUse), "<Gamepad>/buttonWest"), "USE on F and the pad's USE on X after a restart");
        FrontRoomsInput.SwapInto(Bind.PadBack, "<Gamepad>/buttonWest");
        Check(Eq(FrontRoomsInput.PathOf(Bind.PadBack), "<Gamepad>/buttonWest") && Eq(FrontRoomsInput.PathOf(Bind.PadUse), "<Gamepad>/buttonEast"),
            "swap: BACK takes X, USE takes BACK's B (" + FrontRoomsInput.PathOf(Bind.PadUse) + ")");
        FrontRoomsInput.ResetBind(Bind.PadUse);
        Check(Eq(FrontRoomsInput.PathOf(Bind.PadUse), "<Gamepad>/buttonSouth") && Eq(FrontRoomsInput.PathOf(Bind.PadBack), "<Gamepad>/buttonWest"), "USE back to A; BACK keeps X");
        FrontRoomsInput.ResetAllPadBinds();
        Check(FrontRoomsInput.PadAllDefault && Eq(FrontRoomsInput.PathOf(Bind.Use), "<Keyboard>/f"), "the pad's DEFAULTS leaves the keys");
        FrontRoomsInput.Assign(Bind.PadZoom, "<Gamepad>/leftTrigger");
        FrontRoomsInput.ResetAllBinds();
        Check(FrontRoomsInput.AllDefault && Eq(FrontRoomsInput.PathOf(Bind.PadZoom), "<Gamepad>/leftTrigger"), "the keys' DEFAULTS leaves the pad");
        FrontRoomsInput.ResetAllPadBinds();
        Check(FrontRoomsInput.PadAllDefault && !PlayerPrefs.HasKey(TestPrefKey), "nothing saved once the pad is default too");

        Check(Reload("PadUse=<Gamepad>/start") && FrontRoomsInput.PadAllDefault && !PlayerPrefs.HasKey(TestPrefKey), "a saved MENU is ignored; the save is rewritten without it");
        Check(Reload("PadUse=<Keyboard>/f|Use=<Gamepad>/buttonWest|9=<Gamepad>/buttonNorth") && FrontRoomsInput.PadAllDefault && FrontRoomsInput.AllDefault
            && !PlayerPrefs.HasKey(TestPrefKey), "a save on the wrong device, or under a number, is ignored");
        Check(Reload("PadUse=<Gamepad>/leftShoulder") && FrontRoomsInput.PadAllDefault && !PlayerPrefs.HasKey(TestPrefKey),
            "a saved button that SPRINT's default uses goes back; the save is cleared");
        Check(Reload("PadUse=<Gamepad>/buttonEast|PadBack=<Gamepad>/buttonSouth") && Eq(FrontRoomsInput.PathOf(Bind.PadUse), "<Gamepad>/buttonEast")
            && Eq(FrontRoomsInput.PathOf(Bind.PadBack), "<Gamepad>/buttonSouth"), "a player's A/B swap stays");
        var useOnL3 = Reload("PadUse=<Gamepad>/leftStickPress") && FrontRoomsInput.IsDefault(Bind.PadUse);
        var sprintOnL3 = Reload("PadSprint=<Gamepad>/leftStickPress") && Eq(FrontRoomsInput.PathOf(Bind.PadSprint), "<Gamepad>/leftStickPress");
        Check(useOnL3 && sprintOnL3, "USE saved on L3 is ignored; SPRINT saved on L3 stays");
        FrontRoomsInput.ResetAllPadBinds();
    }

    // LEFT CLICK is the close-up's full-view tap (2026-10-30 21:38), ZOOM's locked key since 2026-10-31; the legend and the
    // caught card's line follow the bindings; the pad's menu buttons and SuspendRestart start clear; PlayStation names.
    static void LeftClickAndLegends()
    {
        Bind owner; string locked;
        Check(FrontRoomsInput.LockedOwner("<Mouse>/leftButton") == "ZOOM" && FrontRoomsInput.DesktopBoundPaths().Contains("<mouse>/leftbutton"),
            "LEFT CLICK is ZOOM's locked key and shown in use");
        var useRefused = FrontRoomsInput.CheckBind(Bind.Use, "<Mouse>/leftButton", out owner, out locked) == BindCheck.Locked && locked == "ZOOM";
        Check(useRefused && FrontRoomsInput.CheckBind(Bind.Zoom, "<Mouse>/leftButton", out owner, out locked) == BindCheck.Ok, "USE may not take LEFT CLICK; ZOOM may");
        Check(Reload("Use=<Mouse>/leftButton") && FrontRoomsInput.IsDefault(Bind.Use) && !PlayerPrefs.HasKey(TestPrefKey),
            "USE saved on LEFT CLICK before 2026-10-31: back to E, the save rewritten");
        // The letters as the keyboard in use prints them (ZQSD on AZERTY): the menu run has a keyboard, the batch none.
        var walk = Key("w") + Key("a") + Key("s") + Key("d");
        var legend = FrontRoomsInput.ControlsLegend();
        var again = FrontRoomsInput.TryAgainLine();
        Check(legend == walk + "  MOVE    MOUSE  LOOK    CTRL  SPRINT    SHIFT  SLOW    " + Key("e") + "  USE" && again == Key("r") + "  TRY AGAIN",
            "the legend and R  TRY AGAIN with the defaults (" + legend + " / " + again + ")");
        FrontRoomsInput.Assign(Bind.Use, "<Keyboard>/f");
        FrontRoomsInput.Assign(Bind.Sprint, "<Keyboard>/q");
        legend = FrontRoomsInput.ControlsLegend();
        Check(legend == walk + "  MOVE    MOUSE  LOOK    " + Key("q") + "  SPRINT    SHIFT  SLOW    " + Key("f") + "  USE", "the legend follows the bindings (" + legend + ")");
        FrontRoomsInput.ResetAllBinds();
        FrontRoomsInput.Shutdown();
        var menu = FrontRoomsInput.ReadPadMenu();
        Check(menu.Dir == Vector2Int.zero && !menu.Confirm && !menu.Back && !menu.Reset && !menu.Defaults && menu.Tab == 0
            && FrontRoomsInput.DeviceInUse == FrontRoomsInput.Device.Keyboard, "no pad pressed: no menu input; the keyboard after Shutdown");
        FrontRoomsInput.SuspendRestart = true;
        FrontRoomsInput.Shutdown();
        Check(!FrontRoomsInput.SuspendRestart, "Shutdown clears SuspendRestart");
        FrontRoomsInput.PlayStationPadOverride = true;
        string[,] table =
        {
            { "<Gamepad>/buttonSouth", "CROSS" }, { "<Gamepad>/leftShoulder", "L1" }, { "<Gamepad>/start", "OPTIONS" }, { "<Gamepad>/select", "SHARE" },
            { "<Gamepad>/rightTrigger", "R2" }, { "<Gamepad>/leftStickPress", "L3" },
        };
        var wrong = Misnamed(table);
        if (FrontRoomsInput.ControlName(Bind.PadUse) != "CROSS") wrong.Add("PadUse -> " + FrontRoomsInput.ControlName(Bind.PadUse));
        FrontRoomsInput.PlayStationPadOverride = false;
        Check(wrong.Count == 0, "PlayStation names (forced)" + Report(wrong));
    }

    // A swap or a one-action reset never hands a locked key or button to an action that may not hold it (2026-10-31): ZOOM's
    // LEFT CLICK and the pad SPRINT's L3 stay theirs, the action that gave way goes back to its own default, and no two
    // actions end on one key.
    static void LockedSwaps()
    {
        FrontRoomsInput.Assign(Bind.Zoom, "<Mouse>/leftButton");
        FrontRoomsInput.SwapInto(Bind.Zoom, "<Keyboard>/e");
        var swapped = Eq(FrontRoomsInput.PathOf(Bind.Use), "<Keyboard>/e") && !Eq(FrontRoomsInput.PathOf(Bind.Zoom), "<Keyboard>/e");
        FrontRoomsInput.Assign(Bind.Zoom, "<Mouse>/leftButton");
        FrontRoomsInput.Assign(Bind.Use, "<Mouse>/rightButton");
        FrontRoomsInput.ResetBind(Bind.Zoom);
        var reset = Eq(FrontRoomsInput.PathOf(Bind.Zoom), "<Mouse>/rightButton") && Eq(FrontRoomsInput.PathOf(Bind.Use), "<Keyboard>/e");
        Check(swapped && reset, "ZOOM's LEFT CLICK never goes to USE in a swap or a reset (USE " + FrontRoomsInput.PathOf(Bind.Use) + ")");
        FrontRoomsInput.ResetAllBinds();
        FrontRoomsInput.Assign(Bind.PadSprint, FrontRoomsInput.PadLockPath);
        FrontRoomsInput.SwapInto(Bind.PadSprint, "<Gamepad>/buttonSouth");
        var padSwapped = Eq(FrontRoomsInput.PathOf(Bind.PadUse), "<Gamepad>/buttonSouth") && !Eq(FrontRoomsInput.PathOf(Bind.PadSprint), "<Gamepad>/buttonSouth");
        FrontRoomsInput.Assign(Bind.PadSprint, FrontRoomsInput.PadLockPath);
        FrontRoomsInput.Assign(Bind.PadUse, "<Gamepad>/leftShoulder");
        FrontRoomsInput.ResetBind(Bind.PadSprint);
        var padReset = Eq(FrontRoomsInput.PathOf(Bind.PadSprint), "<Gamepad>/leftShoulder") && Eq(FrontRoomsInput.PathOf(Bind.PadUse), "<Gamepad>/buttonSouth");
        Check(padSwapped && padReset, "the pad SPRINT's L3 never goes to USE in a swap or a reset (USE " + FrontRoomsInput.PathOf(Bind.PadUse) + ")");
        FrontRoomsInput.ResetAllPadBinds();
    }

    // Device state (2026-11-01, Red: a connected pad shows as connected; the scheme in hand comes first on CONTROLS, marked):
    // a pad's family by the names' override; a pad coming and going (a virtual pad: the first look only learns the pads in,
    // and an idle pad going is no loss); the cached legend; the touch sheet's capture (a pad row with no pad does not listen,
    // a key row listens and the game reads nothing meanwhile); the pad in hand going away, where the batch can move a pad.
    static void DeviceState()
    {
        var pad = InputSystem.AddDevice<Gamepad>("KeyBindingTests Gamepad");
        try
        {
            FrontRoomsInput.PlayStationPadOverride = true;
            var ps = FrontRoomsInput.FamilyOf(pad);
            FrontRoomsInput.PlayStationPadOverride = false;
            var xbox = FrontRoomsInput.FamilyOf(pad);
            FrontRoomsInput.PlayStationPadOverride = null;
            var plain = FrontRoomsInput.FamilyOf(pad);
            Check(ps == FrontRoomsInput.PadFamily.PlayStation && xbox == FrontRoomsInput.PadFamily.Xbox && plain == FrontRoomsInput.PadFamily.Generic
                && FrontRoomsInput.FamilyOf(null) == FrontRoomsInput.PadFamily.None, "pad family: forced PlayStation and Xbox, a plain pad Generic (" + ps + ", " + xbox + ", " + plain + ")");
        }
        finally { FrontRoomsInput.PlayStationPadOverride = false; InputSystem.RemoveDevice(pad); }
        Check(FrontRoomsInput.FamilyName(FrontRoomsInput.PadFamily.Xbox) == "XBOX CONTROLLER" && FrontRoomsInput.FamilyName(FrontRoomsInput.PadFamily.PlayStation) == "PLAYSTATION CONTROLLER"
            && FrontRoomsInput.FamilyName(FrontRoomsInput.PadFamily.Generic) == "CONTROLLER", "family names: XBOX CONTROLLER, PLAYSTATION CONTROLLER, CONTROLLER");

        // No override while a pad comes and goes, so the virtual pad reads CONTROLLER whatever pad is paired to the machine.
        FrontRoomsInput.PlayStationPadOverride = null;
        FrontRoomsInput.Shutdown();
        FrontRoomsInput.TrackAgainForTests();
        var learned = FrontRoomsInput.ChangeThisFrame;
        pad = InputSystem.AddDevice<Gamepad>("KeyBindingTests Gamepad");
        var gone = false;
        try
        {
            FrontRoomsInput.TrackAgainForTests();
            var came = FrontRoomsInput.ChangeThisFrame;
            var cameLine = FrontRoomsInput.ChangeLine();
            var shown = FrontRoomsInput.NoticeLive && FrontRoomsInput.PadConnected;
            InputSystem.RemoveDevice(pad);
            gone = true;
            FrontRoomsInput.TrackAgainForTests();
            var went = FrontRoomsInput.ChangeThisFrame;
            Check(learned == FrontRoomsInput.DeviceChange.None, "the first look learns the pads in: no news (" + learned + ")");
            Check(came == FrontRoomsInput.DeviceChange.PadConnected && cameLine == "CONTROLLER CONNECTED" && shown, "a pad comes: " + came + ", " + cameLine + (shown ? ", its notice live" : ", no notice"));
            Check(went == FrontRoomsInput.DeviceChange.PadDisconnected && FrontRoomsInput.ChangeLine() == "CONTROLLER DISCONNECTED" && FrontRoomsInput.DeviceInUse == FrontRoomsInput.Device.Keyboard,
                "an idle pad goes: " + went + ", " + FrontRoomsInput.ChangeLine() + ", the device in use stays " + FrontRoomsInput.DeviceInUse);
        }
        finally { if (!gone) InputSystem.RemoveDevice(pad); FrontRoomsInput.PlayStationPadOverride = false; }

        // The legend is its five pairs, made once and kept until what it names changes (here a binding).
        var pairs = FrontRoomsInput.LegendPair(FrontRoomsInput.LegendPart.Move, false) + "    " + FrontRoomsInput.LegendPair(FrontRoomsInput.LegendPart.Look, false)
            + "    " + FrontRoomsInput.LegendPair(FrontRoomsInput.LegendPart.Sprint, false) + "    " + FrontRoomsInput.LegendPair(FrontRoomsInput.LegendPart.Slow, false)
            + "    " + FrontRoomsInput.LegendPair(FrontRoomsInput.LegendPart.Use, false);
        var legend = FrontRoomsInput.ControlsLegend();
        var cached = ReferenceEquals(legend, FrontRoomsInput.ControlsLegend());
        FrontRoomsInput.Assign(Bind.Use, "<Keyboard>/f");
        var moved = FrontRoomsInput.ControlsLegend();
        FrontRoomsInput.ResetAllBinds();
        Check(legend == pairs && cached && moved != legend && moved.EndsWith(Key("f") + "  USE"), "the legend: its five pairs, made once, made again after a binding changes (" + moved + ")");

        // The touch sheet's capture: a pad row with no pad in does not listen and says why.
        if (Gamepad.current == null && Gamepad.all.Count == 0)
        {
            var begun = FrontRoomsBindCapture.Begin(Bind.PadUse);
            Check(!begun && !FrontRoomsBindCapture.Busy && FrontRoomsBindCapture.CellText(Bind.PadUse) == "CONNECT A CONTROLLER",
                "a pad row with no pad does not listen and reads CONNECT A CONTROLLER (" + FrontRoomsBindCapture.CellText(Bind.PadUse) + ")");
        }
        else Skip("a pad row with no pad: this machine has a pad in");
        // A key row listens (on a virtual keyboard where the batch has none), and the game reads nothing meanwhile.
        var board = Keyboard.current == null ? InputSystem.AddDevice<Keyboard>("KeyBindingTests Keyboard") : null;
        try
        {
            var listens = FrontRoomsBindCapture.Begin(Bind.Use);
            var busy = FrontRoomsBindCapture.Busy && FrontRoomsInput.SuspendGameplay && FrontRoomsBindCapture.CellText(Bind.Use) == "PRESS A KEY";
            FrontRoomsBindCapture.Cancel();
            Check(listens && busy && !FrontRoomsBindCapture.Busy && !FrontRoomsInput.SuspendGameplay && FrontRoomsBindCapture.CellText(Bind.Use) == Key("e"),
                "a key row listens (PRESS A KEY, the game suspended); Cancel ends both (" + FrontRoomsBindCapture.CellText(Bind.Use) + ")");
        }
        finally { FrontRoomsBindCapture.Cancel(); if (board != null) InputSystem.RemoveDevice(board); }

        // The pad in hand going away is a loss (the game pauses for it) and the keys are the device again. ExternalActivity reads
        // the pad's own update, so this needs the batch to move a virtual pad's stick; where it cannot, it is skipped.
        pad = InputSystem.AddDevice<Gamepad>("KeyBindingTests Gamepad");
        gone = false;
        try
        {
            FrontRoomsInput.TrackAgainForTests();
            var arrived = FrontRoomsInput.ChangeThisFrame;
            InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = new Vector2(0f, 1f) });
            InputSystem.Update();
            FrontRoomsInput.TrackAgainForTests();
            var inHand = FrontRoomsInput.ChangeThisFrame == FrontRoomsInput.DeviceChange.None && FrontRoomsInput.DeviceInUse == FrontRoomsInput.Device.Gamepad;
            if (inHand)
            {
                InputSystem.RemoveDevice(pad);
                gone = true;
                FrontRoomsInput.TrackAgainForTests();
                var lost = FrontRoomsInput.ChangeThisFrame;
                Check(lost == FrontRoomsInput.DeviceChange.InUseLost && FrontRoomsInput.DeviceInUse == FrontRoomsInput.Device.Keyboard
                    && FrontRoomsInput.LastChange == FrontRoomsInput.DeviceChange.PadDisconnected, "the pad in hand goes: " + lost + ", the keys again (" + FrontRoomsInput.DeviceInUse + ")");
            }
            else Skip("the pad in hand going away: the batch could not move a virtual pad (" + arrived + ", " + FrontRoomsInput.DeviceInUse + "); the combined test has it");
        }
        catch (Exception e) { Skip("the pad in hand going away: " + e.GetType().Name + " " + e.Message); }
        finally { if (!gone) InputSystem.RemoveDevice(pad); }
        FrontRoomsInput.PlayStationPadOverride = false;
        FrontRoomsInput.Shutdown();
    }

    // A key's name as the keyboard in use prints it ("<Keyboard>/" + its control name).
    static string Key(string control) => FrontRoomsInput.KeyName("<Keyboard>/" + control);

    // The rows of a { path, name } table that KeyName names otherwise, as "path -> name".
    static List<string> Misnamed(string[,] table)
    {
        var wrong = new List<string>();
        for (var i = 0; i < table.GetLength(0); i++)
        {
            var got = FrontRoomsInput.KeyName(table[i, 0]);
            if (got != table[i, 1]) wrong.Add(table[i, 0] + " -> " + got);
        }
        return wrong;
    }

    static string Report(List<string> wrong) => wrong.Count > 0 ? ": " + string.Join(", ", wrong.ToArray()) : "";

    static bool Reload(string saved)
    {
        PlayerPrefs.SetString(TestPrefKey, saved);
        FrontRoomsInput.Shutdown();
        FrontRoomsInput.BeginFrame();
        return true;
    }

    static bool Eq(string a, string b) => FrontRoomsInput.Normalise(a) == FrontRoomsInput.Normalise(b);

    static void Check(bool ok, string what)
    {
        lines.Add((ok ? "ok   " : "FAIL ") + what);
        if (!ok) failed++;
    }

    // A check this machine cannot make: listed, counted apart, neither passed nor failed.
    static void Skip(string what)
    {
        lines.Add("skip " + what);
        skipped++;
    }
}
