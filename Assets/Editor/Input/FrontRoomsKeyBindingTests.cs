// FrontRoomsKeyBindingTests: the desktop key bindings behind CONTROLS / KEYBOARD & MOUSE (FrontRoomsInput's Bind API,
// Red 2026-10-29: players remap their keys on the map page; 2026-10-30: SPRINT on Ctrl, SLOW on Shift). EditMode, no scene.
// Batch: -executeMethod FrontRoomsKeyBindingTests.RunBatch -quit. Uses its own PlayerPrefs key and deletes it, so the
// player's own bindings are never read or written. ASCII, no tuples (editor-script rule).
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Bind = FrontRoomsInput.Bind;
using BindCheck = FrontRoomsInput.BindCheck;

public static class FrontRoomsKeyBindingTests
{
    const string TestPrefKey = "FrontRooms.KeyBindings.Tests";
    static readonly List<string> lines = new List<string>();
    static int failed;

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
        failed = 0;
        var playerKey = FrontRoomsInput.BindingsPrefKey;
        FrontRoomsInput.BindingsPrefKey = TestPrefKey;
        PlayerPrefs.DeleteKey(TestPrefKey);
        try
        {
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
        }
        catch (Exception e)
        {
            Check(false, "no exception: " + e.GetType().Name + " " + e.Message);
        }
        finally
        {
            FrontRoomsInput.AppleKeysOverride = null;
            FrontRoomsInput.Shutdown();
            FrontRoomsInput.ResetAllBinds();
            PlayerPrefs.DeleteKey(TestPrefKey);
            FrontRoomsInput.BindingsPrefKey = playerKey;
            FrontRoomsInput.Shutdown();
        }
        var verdict = (failed == 0 ? "PASS: " : "FAIL: ") + (lines.Count - failed) + " passed, " + failed + " failed";
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
}
