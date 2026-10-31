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
/// -autopilotRunRooms [1|2] (Run! rooms, L7; any letter case) turns Run! rooms on for the run whatever the profile
/// says (FrontRoomsMapWorld.RunRoomsForTools): 2, the default, also forces placement (every zone met once the map is
/// ready counts as tier 2+ after the first send, and the budget roll passes), so a short run meets one; 1 plays the
/// profile's rules. The game logs every room planned, built, tripped and reset ("RUN" lines).
/// -autopilotFloorStations (BS1, BS2) gives the floors below the first their own
/// station while the profile has no floor station modules: the stand-ins
/// (StandInFloorStation, Station Waiting 3 x 2 and Station Desk 3 x 3), made
/// in memory through FrontRooms3DGame.FloorStationModuleForTools, never assets.
/// -autopilotExitTimeout [s] (any letter case; the level design research line, 2026-10-31) sets the deadline of a run that walks
/// the EXIT (-autopilotExit, or a baseline's -autopilotBotAfterExit) to s seconds instead of ExitTimeoutSeconds
/// (600), so define-on bot runs can play 10 min after floor 1's 180 s onboarding gate (DefaultLongExitTimeout,
/// 1200 s, when no number follows; lean's play:<seed>:...+autopilotExitTimeout 1500). Other runs keep their own.
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
    // Run! rooms (L7): -autopilotRunRooms for this run (0 none, 1 on, 2 on and forced), kept across the domain reload of entering Play.
    const string RunRoomsKey = "FrontRooms.Playtest.RunRooms";
    // BS1, BS2: -autopilotFloorStations for this run, kept across the domain reload of entering Play.
    const string FloorStationsKey = "FrontRooms.Playtest.FloorStations";
    const float TimeoutSeconds = 240f;
    // C22: the EXIT scenario walks up to 240 s of game time (three floors) after entering Play.
    const float ExitTimeoutSeconds = 600f;
    // -autopilotExitTimeout with no number after it (or one that is not a positive number).
    const float DefaultLongExitTimeout = 1200f;

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
        if (Timeout != ExitTimeoutSeconds && Timeout != TimeoutSeconds) Debug.Log("[FrontRoomsPlaytest] EXIT deadline " + Timeout + " s (-autopilotExitTimeout)");
        SessionState.EraseBool(PlayedKey);
        SessionState.SetInt(StationKey, StationArgument());
        SessionState.SetBool(OnboardOffKey, OnboardOffArgument());
        SessionState.SetInt(RunRoomsKey, RunRoomsArgument());
        SessionState.SetBool(FloorStationsKey, FlagArgument("-autopilotFloorStations"));
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

    // -autopilotRunRooms [1|2], in any letter case: 2 (on and forced) unless a 1 follows; 0 without the flag.
    static int RunRoomsArgument()
    {
        var args = System.Environment.GetCommandLineArgs();
        for (var i = 0; i < args.Length; i++)
            if (string.Equals(args[i], "-autopilotRunRooms", System.StringComparison.OrdinalIgnoreCase))
                return i + 1 < args.Length && args[i + 1] == "1" ? 1 : 2;
        return 0;
    }

    // A flag in any letter case (lean.sh's play:<seed>:<flag> passes -autopilot<flag>).
    static bool FlagArgument(string flag)
    {
        foreach (var arg in System.Environment.GetCommandLineArgs())
            if (string.Equals(arg, flag, System.StringComparison.OrdinalIgnoreCase)) return true;
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
        // Run! rooms' tools switch (FrontRoomsMapWorld.RunRoomsForTools), likewise.
        FrontRoomsMapWorld.RunRoomsForTools = SessionState.GetInt(RunRoomsKey, 0);
        // BS1, BS2: the floors below the first take the stand-in floor stations (FrontRooms3DGame.FloorStationModuleForTools).
        FrontRooms3DGame.FloorStationModuleForTools = SessionState.GetBool(FloorStationsKey, false)
            ? (System.Func<ZoneTheme, RoomModuleData>)(theme => StandInFloorStation(theme)) : null;
    }

    /// <summary>
    /// BS1's stand-in floor stations (BS4's footprints, before the visual line's modules exist), built in memory to the
    /// floor station contract (RoomModuleData.ValidateFloorStation), never assets. Station Waiting (a Level 0 zone):
    /// 3 x 2 cells, Standard, E1 an arch in the west wall's north cell (1.7 m, authored as edge records), E2 an arch in
    /// the south wall's east cell (1.2 m). Station Desk (an Office zone): 3 x 3, E1 in the south wall's east cell, E2 in
    /// the east wall's middle cell, both at the map's own width (no records). Both: columns None, fill None, weight 0,
    /// never turned; every lamp set, Steady along E1's wall; a key spot 1.0 m up with its take spot inside, the station
    /// beacon at a CRT TV hung on the north wall at 2.3 m (the one prop), a placard 1.56 m up on a wall, and the
    /// StationArch marker on E1's centre. Editor script: ASCII only, no tuples.
    /// </summary>
    public static RoomModuleData StandInFloorStation(ZoneTheme theme)
    {
        var desk = theme == ZoneTheme.Office;
        var m = new RoomModuleData
        {
            width = RoomModuleData.FloorStationWidth, depth = desk ? RoomModuleData.FloorDeskDepth : RoomModuleData.FloorWaitingDepth,
            height = ZoneHeight.Standard, theme = desk ? ZoneTheme.Office : ZoneTheme.Level0,
            columns = ModuleColumns.None, fill = ModuleFill.None, weight = 0f, allowRotate = false,
        };
        m.Normalize();
        RoomModuleData.FloorStationOpenings(m.theme, out var e1Side, out var e1Index, out var e2Side, out var e2Index);
        SetPerimeter(m, e1Side, e1Index, ModuleEdge.Arch);
        SetPerimeter(m, e2Side, e2Index, ModuleEdge.Arch);
        for (var k = 0; k < m.lamps.Length; k++) m.lamps[k] = ModuleLamp.Steady;
        // One dimmer cell away from E1's wall, so the table is the module's own and not all one kind.
        m.lamps[desk ? 1 + 1 * m.width : 2] = ModuleLamp.Dim;
        var cs = MapGrid.CellSize;
        var top = m.DepthMetres;
        if (!desk)
            m.edgeRecords = new[]
            {
                new ModuleEdgeRecord { side = e1Side, index = e1Index, archWidth = 1.7f },
                new ModuleEdgeRecord { side = e2Side, index = e2Index, archWidth = 1.2f },
            };
        var archX = e1Side == ModuleSide.West ? 0f : e1Side == ModuleSide.East ? m.WidthMetres : (e1Index + .5f) * cs;
        var archZ = e1Side == ModuleSide.South ? 0f : e1Side == ModuleSide.North ? top : (e1Index + .5f) * cs;
        m.markers = new[]
        {
            new ModuleMarker { kind = ModuleMarkerKind.KeySpot, x = 4.5f, z = top - 2.1f, y = 1.0f, yaw = 180f, tag = "", host = "" },
            new ModuleMarker { kind = ModuleMarkerKind.StationBeacon, x = 4.5f, z = top - .4f, y = 2.3f, yaw = 180f, tag = "", host = "" },
            desk ? new ModuleMarker { kind = ModuleMarkerKind.Placard, x = ModuleUnits.WallHalf, z = 4.5f, y = 1.56f, yaw = 90f, tag = "", host = "" }
                : new ModuleMarker { kind = ModuleMarkerKind.Placard, x = 1.5f, z = top - ModuleUnits.WallHalf, y = 1.56f, yaw = 180f, tag = "", host = "" },
            new ModuleMarker { kind = ModuleMarkerKind.StationArch, x = archX, z = archZ, y = 0f, yaw = 0f, tag = "", host = "" },
        };
        m.props = new[] { new ModuleProp { kit = "Kit_CRTTV", x = 4.5f, z = top - .4f, y = 2.3f, yaw = 180f } };
        return m;
    }

    static void SetPerimeter(RoomModuleData m, ModuleSide side, int index, ModuleEdge kind)
    {
        if (side == ModuleSide.South) m.south[index] = kind;
        else if (side == ModuleSide.North) m.north[index] = kind;
        else if (side == ModuleSide.West) m.west[index] = kind;
        else m.east[index] = kind;
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

    // A Relay baseline with -autopilotBotAfterExit (any letter case) walks floor 1's EXIT before its bot's budget: the EXIT's deadline too.
    // -autopilotExitTimeout [s] replaces the EXIT's deadline (ExitTimeoutArgument).
    static float Timeout => System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-autopilotExit") >= 0 || BotAfterExitArgument()
        ? ExitTimeoutArgument(ExitTimeoutSeconds) : TimeoutSeconds;

    // -autopilotExitTimeout [s], in any letter case: s seconds (a positive number, invariant culture), DefaultLongExitTimeout
    // when none follows; without the flag, fallback.
    static float ExitTimeoutArgument(float fallback)
    {
        var args = System.Environment.GetCommandLineArgs();
        for (var i = 0; i < args.Length; i++)
        {
            if (!string.Equals(args[i], "-autopilotExitTimeout", System.StringComparison.OrdinalIgnoreCase)) continue;
            if (i + 1 < args.Length && float.TryParse(args[i + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var seconds)
                && seconds > 0f) return seconds;
            return DefaultLongExitTimeout;
        }
        return fallback;
    }

    static bool BotAfterExitArgument()
    {
        foreach (var arg in System.Environment.GetCommandLineArgs())
            if (string.Equals(arg, "-autopilotBotAfterExit", System.StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

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
        SessionState.EraseInt(RunRoomsKey);
        SessionState.EraseBool(FloorStationsKey);
        FrontRooms3DGame.StationModuleForTools = null;
        FrontRooms3DGame.OnboardOffForTools = false;
        FrontRoomsMapWorld.RunRoomsForTools = 0;
        FrontRooms3DGame.FloorStationModuleForTools = null;
        var verdict = done ? File.ReadAllText(DonePath)
            : aborted ? "FAIL · Play mode was left before the autopilot finished"
            : "FAIL · timed out after " + Timeout + " s";
        var passed = verdict.StartsWith("PASS");
        if (passed) Debug.Log("[FrontRoomsPlaytest] " + verdict);
        else Debug.LogError("[FrontRoomsPlaytest] " + verdict);
        if (Application.isBatchMode) EditorApplication.Exit(passed ? 0 : 1);
    }
}
