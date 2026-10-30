// FrontRoomsKeyBindingTests: the desktop key bindings behind CONTROLS / KEYBOARD & MOUSE (FrontRoomsInput's Bind API,
// Red 2026-10-29: players remap their keys on the map page). EditMode, no scene.
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
        }
        catch (Exception e)
        {
            Check(false, "no exception: " + e.GetType().Name + " " + e.Message);
        }
        finally
        {
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
        Check(Eq(FrontRoomsInput.PathOf(Bind.Sprint), "<Keyboard>/leftShift") && Eq(FrontRoomsInput.PathOf(Bind.Restart), "<Keyboard>/r")
            && Eq(FrontRoomsInput.PathOf(Bind.Zoom), "<Mouse>/rightButton"), "SPRINT Left Shift, RESTART R, ZOOM the right button");
        Check(FrontRoomsInput.AllDefault, "all default at first");
        Check(FrontRoomsInput.UseKey == "E" && FrontRoomsInput.WithKeyNames("E  \u00B7  OPEN DOOR") == "E  \u00B7  OPEN DOOR", "prompts unchanged while USE is E");
        Check(Eq(FrontRoomsInput.StepBackPath, "<Keyboard>/s"), "S steps back out of a close-up");
    }

    static void Names()
    {
        string[,] table =
        {
            { "<Keyboard>/leftShift", "SHIFT" }, { "<Keyboard>/rightShift", "SHIFT" }, { "<Keyboard>/upArrow", "UP" },
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
        Check(FrontRoomsInput.CheckBind(Bind.Use, "<Keyboard>/rightShift", out owner, out locked) == BindCheck.Locked && locked == "SPRINT", "Right Shift is SPRINT's locked key");
        Check(FrontRoomsInput.CheckBind(Bind.Sprint, "<Keyboard>/rightShift", out owner, out locked) == BindCheck.Ok, "SPRINT may take its own locked key");
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
        Check(FrontRoomsInput.AllDefault && Eq(FrontRoomsInput.PathOf(Bind.Restart), "<Keyboard>/r") && Eq(FrontRoomsInput.PathOf(Bind.Sprint), "<Keyboard>/leftShift"), "all defaults again");
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
        Check(f.Move == Vector2.zero && !f.UseDown && !f.RestartDown && !f.PauseDown && !f.ShotBackDown, "a suspended frame reads nothing");
    }

    static bool Eq(string a, string b) => FrontRoomsInput.Normalise(a) == FrontRoomsInput.Normalise(b);

    static void Check(bool ok, string what)
    {
        lines.Add((ok ? "ok   " : "FAIL ") + what);
        if (!ok) failed++;
    }
}
