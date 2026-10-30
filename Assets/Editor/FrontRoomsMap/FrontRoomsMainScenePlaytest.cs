using System.IO;
using FrontRooms.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Plays the main scene unattended: title → Space → out of the stream room
/// into the maze → about 75 s there with the Relay, on FrontRooms3DGame's
/// editor-only autopilot (-autopilotSpaceAt N sets when Space is pressed).
/// Frames and report.json go to Verification/main-autopilot. In batch mode
/// the editor exits 0 on PASS and 1 otherwise.
/// Run with -executeMethod FrontRoomsMainScenePlaytest.RunBatch (no -quit);
/// add -autopilotSeed N to play a fixed maze. -autopilotExit (C22) goes three
/// floors down through the EXITs instead of the 75 s walk, so it gets a longer
/// deadline. -autopilotStation [depth] (A26) starts floor 1 in the map's
/// stand-in station (4 x 4, or 4 x 3 with depth 3), made in memory while the
/// profile has no station module: never an asset, never in the profile's pool.
/// -autopilotOnboardOff (the onboarding, Red 2026-10-30) turns floor 1's
/// onboarding gate off for the run (FrontRooms3DGame.OnboardOffForTools): the
/// first send comes at M7's old gate (the key and 2 path cells out, or 150 s),
/// so captures and smoke runs still meet the Relay early on floor 1.
/// </summary>
[InitializeOnLoad]
public static class FrontRoomsMainScenePlaytest
{
    const string ScenePath = "Assets/Scenes/FrontRooms3D.unity";
    const string ActiveKey = "FrontRooms.Playtest.Active";
    const string DeadlineKey = "FrontRooms.Playtest.Deadline";
    const string PlayedKey = "FrontRooms.Playtest.Played";
    // A26: the stand-in station's depth for this run (0: none), kept across the domain reload of entering Play.
    const string StationKey = "FrontRooms.Playtest.Station";
    // The onboarding (Red 2026-10-30): -autopilotOnboardOff for this run, kept across the domain reload of entering Play.
    const string OnboardOffKey = "FrontRooms.Playtest.OnboardOff";
    const float TimeoutSeconds = 240f;
    // C22: the EXIT scenario walks up to 240 s of game time (three floors) after entering Play.
    const float ExitTimeoutSeconds = 600f;

    // Entering Play reloads the domain; this re-attaches the watcher.
    static FrontRoomsMainScenePlaytest()
    {
        if (SessionState.GetBool(ActiveKey, false)) EditorApplication.update += Watch;
        HookStation();
    }

    [MenuItem("FrontRooms/Map/Play main scene on autopilot")]
    public static void Run()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var done = DonePath;
        if (File.Exists(done)) File.Delete(done);
        SessionState.SetBool(FrontRooms3DGame.AutopilotKey, true);
        SessionState.SetInt(FrontRooms3DGame.AutopilotSeedKey, SeedArgument());
        SessionState.SetBool(ActiveKey, true);
        SessionState.SetFloat(DeadlineKey, (float)EditorApplication.timeSinceStartup + Timeout);
        SessionState.EraseBool(PlayedKey);
        SessionState.SetInt(StationKey, StationArgument());
        SessionState.SetBool(OnboardOffKey, OnboardOffArgument());
        HookStation();
        EditorApplication.update -= Watch;
        EditorApplication.update += Watch;
        EditorApplication.EnterPlaymode();
    }

    public static void RunBatch() => Run();

    static int SeedArgument()
    {
        var args = System.Environment.GetCommandLineArgs();
        for (var i = 0; i < args.Length - 1; i++)
            if (args[i] == "-autopilotSeed" && int.TryParse(args[i + 1], out var seed)) return seed;
        return 0;
    }

    // -autopilotStation [3|4]: the stand-in's depth (4 when none is given); 0 without the flag.
    static int StationArgument()
    {
        var args = System.Environment.GetCommandLineArgs();
        var i = System.Array.IndexOf(args, "-autopilotStation");
        if (i < 0) return 0;
        return i + 1 < args.Length && int.TryParse(args[i + 1], out var depth) && (depth == 3 || depth == 4) ? depth : 4;
    }

    // -autopilotOnboardOff, in any letter case (lean.sh's play:<seed>:onboardOff passes -autopilotonboardOff).
    static bool OnboardOffArgument()
    {
        foreach (var arg in System.Environment.GetCommandLineArgs())
            if (string.Equals(arg, "-autopilotOnboardOff", System.StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    // The game's tools hook (FrontRooms3DGame.StationModuleForTools) for this run's stand-in, or none. Static fields do not
    // survive the domain reload of entering Play, so this runs again from the static constructor.
    static void HookStation()
    {
        var depth = SessionState.GetInt(StationKey, 0);
        FrontRooms3DGame.StationModuleForTools = depth > 0 ? (System.Func<RoomModuleData>)(() => StandInStation(depth)) : null;
        // The onboarding's tools switch (FrontRooms3DGame.OnboardOffForTools), likewise set again after each reload.
        FrontRooms3DGame.OnboardOffForTools = SessionState.GetBool(OnboardOffKey, false);
    }

    /// <summary>
    /// The stand-in station for a played run (A26 step 4): FrontRoomsMapVerification.StandInStation (the station
    /// contract, FD-b's markers and lamps) plus a few props for the set piece to place, in metres from the room's
    /// south-west corner: a ladder chair, a filing cabinet on the east wall, the CRT TV hung at 2.3 m (centre x 9.35, out
    /// of the axis column) and a wall clock on the north wall.
    /// </summary>
    public static RoomModuleData StandInStation(int depth)
    {
        var m = FrontRoomsMapVerification.StandInStation(depth);
        var top = m.DepthMetres;
        m.props = new[]
        {
            new ModuleProp { kit = "Kit_LadderChair", x = 4.5f, z = 4.5f, y = 0f, yaw = 180f },
            new ModuleProp { kit = "Kit_FilingCabinet", x = 11.5f, z = 5f, y = 0f, yaw = 270f },
            new ModuleProp { kit = "Kit_CRTTV", x = 9.35f, z = top - .4f, y = 2.3f, yaw = 180f },
            new ModuleProp { kit = "Kit_WallClock", x = 3f, z = top - ModuleUnits.WallHalf, y = 2.2f, yaw = 180f },
        };
        return m;
    }

    static float Timeout => System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-autopilotExit") >= 0 ? ExitTimeoutSeconds : TimeoutSeconds;

    static string DonePath => Path.Combine(Directory.GetParent(Application.dataPath).FullName, FrontRooms3DGame.AutopilotDoneFile);

    static void Watch()
    {
        if (EditorApplication.isPlaying) SessionState.SetBool(PlayedKey, true);
        var done = File.Exists(DonePath);
        var timedOut = EditorApplication.timeSinceStartup > SessionState.GetFloat(DeadlineKey, float.MaxValue);
        // Play was left before the autopilot finished: clean up now, so the next manual Play is a normal one.
        var aborted = !done && SessionState.GetBool(PlayedKey, false) && !EditorApplication.isPlayingOrWillChangePlaymode;
        if (!done && !timedOut && !aborted) return;
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
            return;
        }
        EditorApplication.update -= Watch;
        SessionState.EraseBool(ActiveKey);
        SessionState.EraseBool(PlayedKey);
        SessionState.EraseBool(FrontRooms3DGame.AutopilotKey);
        SessionState.EraseInt(FrontRooms3DGame.AutopilotSeedKey);
        SessionState.EraseInt(StationKey);
        SessionState.EraseBool(OnboardOffKey);
        FrontRooms3DGame.StationModuleForTools = null;
        FrontRooms3DGame.OnboardOffForTools = false;
        var verdict = done ? File.ReadAllText(DonePath)
            : aborted ? "FAIL · Play mode was left before the autopilot finished"
            : "FAIL · timed out after " + Timeout + " s";
        var passed = verdict.StartsWith("PASS");
        if (passed) Debug.Log("[FrontRoomsPlaytest] " + verdict);
        else Debug.LogError("[FrontRoomsPlaytest] " + verdict);
        if (Application.isBatchMode) EditorApplication.Exit(passed ? 0 : 1);
    }
}
