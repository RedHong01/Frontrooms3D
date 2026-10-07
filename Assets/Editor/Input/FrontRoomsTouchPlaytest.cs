using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// The handheld touch layer in the Editor (Documentation/TOUCH_CONTROLS.md §11).
///
/// FrontRooms 3D › Mobile › Touch preview: Play Mode on the handheld path with an
/// iPhone 16 Pro profile; the mouse is one finger (set the Game view to
/// 2622 × 1206 for the true layout). Off by default, never in batch mode or an
/// autopilot run, so normal Play Mode stays the desktop game.
///
/// FrontRooms 3D › Mobile › Run touch playtest (or batch
/// <c>-executeMethod FrontRoomsTouchPlaytest.RunBatch [-touchProfile iphone|ipad|android]
/// [-touchOut dir] [-touchIsolatePrefs]</c>, without -quit): plays the scenario in
/// FrontRoomsTouchPlaytestDriver with injected touches and writes frames plus
/// report.json. Run batch checks in a private copy of the project, never in the
/// project Red has open.
/// </summary>
[InitializeOnLoad]
public static class FrontRoomsTouchPlaytest
{
    const string PreviewPref = "FrontRooms.TouchPreview";
    const string MenuPreview = "FrontRooms 3D/Mobile/Touch preview in Play Mode (iPhone 16 Pro)";
    const string ScenePath = "Assets/Scenes/FrontRooms3D.unity";
    const string WatchKey = "FrontRooms.TouchPlaytest.Watch";
    const string DeadlineKey = "FrontRooms.TouchPlaytest.Deadline";
    const float TimeoutSeconds = 900f;

    static FrontRoomsTouchPlaytest()
    {
        FrontRoomsHandheld.EditorPreviewRequested = EditorPrefs.GetBool(PreviewPref, false);
        if (SessionState.GetBool(WatchKey, false))
        {
            EditorApplication.update -= Watch;
            EditorApplication.update += Watch;
        }
    }

    [MenuItem(MenuPreview)]
    static void TogglePreview()
    {
        var on = !EditorPrefs.GetBool(PreviewPref, false);
        EditorPrefs.SetBool(PreviewPref, on);
        FrontRoomsHandheld.EditorPreviewRequested = on;
        Debug.Log("[FrontRooms] Touch preview " + (on ? "on: enter Play Mode; the mouse is one finger." : "off."));
    }

    [MenuItem(MenuPreview, true)]
    static bool TogglePreviewValidate()
    {
        Menu.SetChecked(MenuPreview, EditorPrefs.GetBool(PreviewPref, false));
        return !EditorApplication.isPlayingOrWillChangePlaymode;
    }

    [MenuItem("FrontRooms 3D/Mobile/Run touch playtest (iPhone 16 Pro)")]
    static void RunFromMenu() => Begin("iphone", "Verification/touch-playtest/iphone", false);

    public static void RunBatch()
    {
        var profile = Arg("-touchProfile") ?? "iphone";
        var output = Arg("-touchOut") ?? "Verification/touch-playtest/" + profile;
        Begin(profile, output, Array.IndexOf(Environment.GetCommandLineArgs(), "-touchIsolatePrefs") >= 0);
    }

    static string Arg(string name)
    {
        var args = Environment.GetCommandLineArgs();
        for (var i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1];
        return null;
    }

    static void Begin(string profile, string output, bool isolatePrefs)
    {
        // A private copy's run keeps its PlayerPrefs apart from the real project's (same company/product = same prefs).
        if (isolatePrefs) PlayerSettings.productName = "FrontRooms3D TouchPlaytest";
        SessionState.SetBool(FrontRoomsTouchPlaytestDriver.ActiveKey, true);
        SessionState.SetString(FrontRoomsTouchPlaytestDriver.ProfileKey, profile);
        SessionState.SetString(FrontRoomsTouchPlaytestDriver.OutKey, output);
        SessionState.EraseBool(FrontRoomsTouchPlaytestDriver.DoneKey);
        SessionState.EraseBool(FrontRoomsTouchPlaytestDriver.FailedKey);
        SessionState.SetBool(WatchKey, true);
        SessionState.SetFloat(DeadlineKey, (float)EditorApplication.timeSinceStartup + TimeoutSeconds);
        EditorSceneManager.OpenScene(ScenePath);
        EditorApplication.update -= Watch;
        EditorApplication.update += Watch;
        EditorApplication.EnterPlaymode();
    }

    static void Watch()
    {
        var done = SessionState.GetBool(FrontRoomsTouchPlaytestDriver.DoneKey, false);
        var timedOut = EditorApplication.timeSinceStartup > SessionState.GetFloat(DeadlineKey, float.MaxValue);
        if (!done && !timedOut) return;
        if (EditorApplication.isPlaying)
        {
            EditorApplication.ExitPlaymode();
            return;
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        EditorApplication.update -= Watch;
        var failed = SessionState.GetBool(FrontRoomsTouchPlaytestDriver.FailedKey, true);
        SessionState.EraseBool(WatchKey);
        SessionState.EraseBool(FrontRoomsTouchPlaytestDriver.ActiveKey);
        Debug.Log("[TouchPlaytest] finished: " + (timedOut ? "timed out" : failed ? "failures" : "all checks passed"));
        if (Application.isBatchMode) EditorApplication.Exit(!timedOut && !failed ? 0 : 1);
    }
}
