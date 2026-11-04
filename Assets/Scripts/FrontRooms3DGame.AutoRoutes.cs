#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using FrontRooms.Map;
using UnityEngine;

/// <summary>
/// The tools' routes for the three rows of the combined test the plain autopilot never reaches (W/combined_test_1102,
/// FIXES rows X10, X11 and X16; the map line's brief integration/s71_auto_routes, 2026-11-03). Editor only, as every
/// autopilot path: nothing here exists in a player, and with none of the three flags no field here is set and every
/// hook in FrontRooms3DGame.cs reads false, so the run is today's.
/// One route per run, and only once floor 1's station rows are in (the plain walk writes them as always: the start door,
/// the EXIT in view, the floor key's take and its panel, M5's route and its flip), so the station's verdict is never cut
/// short. Until then, and between its own steps, the walk is the usual one.
/// - -autopilotRunRoom (X10): Run! rooms on and forced and floor 1's onboarding off (FrontRoomsMainScenePlaytest sets
///   FrontRoomsMapWorld.RunRoomsForTools 2 and OnboardOffForTools), so the run's first send comes on floor 1 and the
///   director can take the Relay away again. Once the first send is out and the Relay has been on the map, the Relay is
///   held where it stands (HeldForTools, as the door and EXIT scenarios hold it, so a catch cannot cut the loop), the walk
///   keeps out of every Run! room's cells (far routes, or toward the nearest room still to be built), and the walker goes
///   to the nearest built, Armed room's arch (another room after 60 s, three at most), lines up on its axis 1.2 m outside
///   and walks in. Every route walk is the EXIT walker's AutoExitWalk (it lines up before each door). Rows: armed (the room built and Armed, no trip yet), the trip (RoomTriggered once,
///   1.5 m in), the sprint (Shift held from the trip to the goal door), the goal (through the goal door into the far
///   cell: the body's push, or the tools' TryOpenDoor once the leaf has not moved for 2.5 s) and the reset (RoomReset by
///   the director's Away through the game's own handler, ResetRunRooms; the held Relay cannot withdraw by itself, so once
///   it is in Arrive or on the map the route sends it Away, SendAway(Test), as DebugPlaceOnMap does). Frames: the arch
///   armed, tripped and reset, the corridor, the goal door from the far cell.
/// - -autopilotSash (X11): the sash pry on (FrontRoomsMapWorld.SashPryForTools) and the onboarding off. Once the first
///   send is out, the Relay is held, and the walker goes to the nearest plain window it can reach (an intact pane with
///   a latch and no foil; another window when it cannot get there or aim at the latch, three at most), stands 0.8 m off
///   it (placed there by tools when the walk has not reached the spot in 5 s), faces the latch and holds E through the
///   player's own input path (EDown,
///   EHeld; UpdateAim, UpdatePry, map.Pry). The Relay is placed by tools (DebugPlaceOnMap) one open cell off and held
///   there, so the pry's noise has an ear. Rows: the window met, the progress banked (E let go after 1.5 s:
///   SashPryReleased, the sash keeps it), the hold resumed from it, the sash giving at 4 s of hold, SashPried once, the
///   noise heard (v2 loudness 8), and the climb through the pried frame into the far cell.
/// - -autopilotStartFloor N (X16): the run goes down to floor N (2 to 99) by the game's own path, floor by floor: the
///   walker is placed in X, 0.35 m past its middle (where the EXIT walker seals), and the game's gate seals the EXIT
///   behind it (ExitSealAllowed lets it through for this route), blacks the stairwell out and changes the floor
///   (ChangeFloor: the next seed from FrontRoomsExitPlanner.SeedFor of the run seed, the floor and X; NextFloor), so
///   floor N's seed is the one a descent at the run's tier gives. On floor N it walks out of the stairwell as after any
///   descent. Rows: each change (its seed, the candidate it was), floor N reached, the landing placard (its sheet,
///   B10+ from floor 11), the HUD there (the room line, the floor token), the north door's shut. Frames of the placard
///   and the landing. Then the usual walk until the Relay is on the map (the floor's first send).
/// Rows go to report.json (runRoomChecks, sashChecks, startFloorChecks), ok or FAIL; one FAIL, or a route that did not
/// finish, fails the run. A route whose rows are in ends the run. Editor script: ASCII only, no tuples.
/// </summary>
public sealed partial class FrontRooms3DGame
{
    enum ToolRoute { None, RunRoom, Sash, StartFloor }
    ToolRoute toolRoute;
    // A route was asked (AutopilotTick, AutopilotSteer and the play limit read it), the route holds E (EDown and EHeld then
    // read autoEDown and autoEHeld, as for the glass and door scenarios), and X16 lets the EXIT's seal through.
    bool toolRouteOn, toolRouteE, toolRouteSeal;
    // The step, its seconds, done (its rows are in, or a FAIL ended it), why, and whether the route holds the Relay.
    int toolRouteStep;
    float toolRouteClock;
    bool toolRouteDone, toolRouteHeld;
    string toolRouteEnd = "";
    // X16: the floor asked.
    int toolRouteTarget;
    readonly List<string> runRoomRows = new List<string>();
    readonly List<string> sashRows = new List<string>();
    readonly List<string> startFloorRows = new List<string>();
    // A walk that has not moved 0.4 m in 2.5 s plans again; how often it was stuck on the way to the route's goal.
    float toolRouteStuckClock, toolRouteReplanAt;
    Vector3 toolRouteStuckAt;
    int toolRouteStucks;
    // The run's limit with a route (game seconds of map play; FrontRoomsMainScenePlaytest gives it the EXIT's deadline).
    const float ToolRoutePlaySeconds = 360f;
    // Floor 1's rows and the first send: how long a route waits for them before it fails.
    const float ToolRouteReadySeconds = 150f;

    /// <summary>The run's play limit: a route's own, else AutopilotPlaySeconds as before.</summary>
    float AutoRoutesPlaySeconds => toolRouteOn ? ToolRoutePlaySeconds : AutopilotPlaySeconds;

    string ToolRouteTag => toolRoute == ToolRoute.RunRoom ? "RUN ROOM" : toolRoute == ToolRoute.Sash ? "SASH" : "START FLOOR";

    string ToolRouteName => toolRoute == ToolRoute.RunRoom ? "X10 Run! room (-autopilotRunRoom)" : toolRoute == ToolRoute.Sash ? "X11 sash pry (-autopilotSash)"
        : "X16 start floor " + toolRouteTarget + " (-autopilotStartFloor)";

    List<string> ToolRouteRows => toolRoute == ToolRoute.RunRoom ? runRoomRows : toolRoute == ToolRoute.Sash ? sashRows : startFloorRows;

    /// <summary>From AutopilotStart, once the other flags are read: which route this run drives (none without its flag).</summary>
    void AutoRoutesStart(string[] args)
    {
        toolRoute = ToolRoute.None;
        toolRouteOn = toolRouteE = toolRouteSeal = toolRouteDone = toolRouteHeld = false;
        var runRoom = false;
        var sash = false;
        var startFloor = 0;
        for (var i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], "-autopilotRunRoom", StringComparison.OrdinalIgnoreCase)) runRoom = true;
            else if (string.Equals(args[i], "-autopilotSash", StringComparison.OrdinalIgnoreCase)) sash = true;
            else if (string.Equals(args[i], "-autopilotStartFloor", StringComparison.OrdinalIgnoreCase))
                startFloor = i + 1 < args.Length && int.TryParse(args[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : -1;
        }
        var asked = (runRoom ? 1 : 0) + (sash ? 1 : 0) + (startFloor != 0 ? 1 : 0);
        if (asked == 0) return;
        if (autoBaseline || autoExitWanted || autoGlassWanted || autoDoorWanted)
        {
            Log("AUTOPILOT ROUTE ignored: -autopilotRunRoom, -autopilotSash and -autopilotStartFloor each run on their own, not with a baseline, -autopilotExit, -autopilotGlass or -autopilotDoors");
            return;
        }
        if (runRoom) toolRoute = ToolRoute.RunRoom;
        else if (sash) toolRoute = ToolRoute.Sash;
        else if (startFloor >= 2)
        {
            toolRoute = ToolRoute.StartFloor;
            toolRouteTarget = Mathf.Min(startFloor, 99);
        }
        else
        {
            Log("AUTOPILOT ROUTE -autopilotStartFloor needs a floor of 2 or more after it (got " + startFloor + "): ignored");
            return;
        }
        toolRouteOn = true;
        toolRouteStep = 0;
        toolRouteClock = 0f;
        Log("AUTOPILOT ROUTE " + ToolRouteName + " on" + (asked > 1 ? " (one route per run: the other route flags are ignored)" : "")
            + (toolRoute == ToolRoute.StartFloor ? "" : " - onboarding off: " + OnboardOffForTools) + " - after floor 1's station rows; up to "
            + AutoF(ToolRoutePlaySeconds, "0") + " s of play");
    }

    /// <summary>Each autopilot frame (AutopilotTick, before the game's play update): the route's clock, checks and steps.</summary>
    void AutoRoutesTick(float dt)
    {
        if (toolRouteDone || autoFinished || !mapPlay || playerRoot == null || map == null || phase != Phase.Playing) return;
        toolRouteClock += dt;
        if (toolRouteHeld && relay != null && !relay.HeldForTools) relay.HeldForTools = true;
        if (toolRoute == ToolRoute.RunRoom) RunRoomTick(dt);
        else if (toolRoute == ToolRoute.Sash) SashTick();
        else if (toolRoute == ToolRoute.StartFloor) StartFloorTick();
        if (!toolRouteDone) return;
        ToolRouteRelease();
        AutopilotFinish(toolRouteEnd);
    }

    /// <summary>From AutopilotSteer, after the floor key's take: true when the route steers this frame, false for the usual walk.</summary>
    bool AutoRoutesSteer(float dt, out Vector2 local, out bool sprint)
    {
        local = Vector2.zero;
        sprint = false;
        if (toolRouteDone || playerRoot == null || map == null) return false;
        if (toolRoute == ToolRoute.RunRoom) return RunRoomSteer(dt, out local, out sprint);
        if (toolRoute == ToolRoute.Sash) return SashSteer(dt, out local);
        if (toolRoute == ToolRoute.StartFloor) return StartFloorSteer(dt, out local);
        return false;
    }

    /// <summary>From AutopilotFinish, before the verdict: the route's rows into the report; true when it finished with every row ok.</summary>
    bool AutoRoutesFinish(AutopilotReport report)
    {
        if (!toolRouteOn) return true;
        var rows = ToolRouteRows;
        if (!toolRouteDone)
            ToolRouteRow(rows, false, ToolRouteName + ": the route did not finish (step " + toolRouteStep + ", " + AutoF(toolRouteClock, "0.0") + " s in it, floor " + floor + ")");
        ToolRouteRelease();
        report.runRoomChecks = runRoomRows;
        report.sashChecks = sashRows;
        report.startFloorChecks = startFloorRows;
        return rows.Count > 0 && rows.TrueForAll(c => c.StartsWith("ok", StringComparison.Ordinal));
    }

    // ---------- Shared ----------

    void ToolRouteRow(List<string> rows, bool ok, string what)
    {
        rows.Add((ok ? "ok   " : "FAIL ") + what);
        Log("AUTOPILOT " + ToolRouteTag + " " + (ok ? "ok   " : "FAIL ") + what);
    }

    void ToolRouteNext(int step)
    {
        toolRouteStep = step;
        toolRouteClock = 0f;
        toolRouteStuckClock = 0f;
        toolRouteReplanAt = 0f;
        toolRouteStucks = 0;
        if (playerRoot != null) toolRouteStuckAt = playerRoot.position;
    }

    // The route's rows are in (or a FAIL ended it): the run ends with it, from AutoRoutesTick.
    void ToolRouteEnd(string why)
    {
        toolRouteDone = true;
        toolRouteEnd = why;
    }

    void ToolRouteFail(string what)
    {
        ToolRouteRow(ToolRouteRows, false, what);
        ToolRouteEnd(ToolRouteName + " failed");
    }

    // The Relay held where it stands (HeldForTools: its tick does nothing, so it neither moves nor catches; it still hears).
    void ToolRouteHold(bool on)
    {
        toolRouteHeld = on;
        if (relay != null) relay.HeldForTools = on;
    }

    // Everything the route holds is let go: E, the Relay, the seal, its events.
    void ToolRouteRelease()
    {
        if (toolRouteE) autoEDown = autoEHeld = false;
        toolRouteE = false;
        toolRouteSeal = false;
        if (toolRouteHeld) ToolRouteHold(false);
        RunRoomUnhook();
        SashUnhook();
    }

    // Floor 1's station rows (A26) are in, or this floor has none; and the player is out of the title's rooms with its door shut.
    bool ToolRouteFloorReady => !inStartRooms && autoStartShutAt >= 0f && (floor != 1 || stationPlan == null
        || (autoStationPlanned && autoStationOpened && autoStationSight && autoStationShut && autoStationTake >= 4 && autoStationPanelRow && autoRouteStage >= 3));

    // The run's first send is out (a station run: with floor 1's row written, so no tools placement ever stands in for it).
    bool ToolRouteFirstSendOut => relay != null && (stationRun ? relay.FirstSendDone && (floor != 1 || stationPlan == null || autoStationSent) : relay.Released);

    // The Relay has been on the map at least once this run (v2: what PASS needs; v1: released).
    bool ToolRouteRelayMet
    {
        get
        {
#if FRONTROOMS_RELAY_V2
            return autoV2OnMapAt >= 0f;
#else
            return autoReleaseTime >= 0f;
#endif
        }
    }

    // A placement (a tool's, as the glass and door scenarios place the player): no walk, so the distance walked skips it.
    void ToolRoutePlace(Vector3 feet, float faceYaw)
    {
        playerBody.enabled = false;
        playerRoot.position = feet;
        playerBody.enabled = true;
        Physics.SyncTransforms();
        yaw = faceYaw;
        pitch = 0f;
        fallSpeed = 0f;
        autoLastFlat = Flat(feet);
        autoLastPosition = feet;
        toolRouteStuckAt = feet;
        toolRouteStuckClock = 0f;
    }

    // Every 2.5 s: a walk that has not moved 0.4 m plans again (the usual walk's rule). True the frame it was found stuck.
    bool ToolRouteStuck(float dt)
    {
        toolRouteStuckClock += dt;
        if (toolRouteStuckClock < 2.5f) return false;
        toolRouteStuckClock = 0f;
        var stuck = (playerRoot.position - toolRouteStuckAt).magnitude < .4f;
        toolRouteStuckAt = playerRoot.position;
        if (!stuck) return false;
        autoRouteIndex = autoRoute.Count;
        toolRouteStucks++;
        return true;
    }

    // Cells a route may walk: never a Run! room's (a route that crossed one would trip it).
    bool ToolRouteKeep(GridCoord cell) => map.RunRoomAt(cell) == null;

    // autoRoute becomes the last search's way to the goal, walked by the EXIT walker's AutoExitWalk (it lines up 1.3 m before
    // each door, out of any leaf's reach, and opens a shut one as the usual walk does).
    void ToolRouteTo(GridCoord here, GridCoord goal)
    {
        AutoRouteTo(here, goal);
        autoExitAtWaypoint = false;
    }

    /// <summary>
    /// A far route (depth 18 to 30, as AutopilotPlan's) over built cells that keeps out of every Run! room, walked with the
    /// EXIT walker's AutoExitWalk (doors opened on the way as the usual walk opens them).
    /// </summary>
    bool ToolRouteExplore(float dt, GridCoord here, out Vector2 local)
    {
        local = Vector2.zero;
        ToolRouteStuck(dt);
        if (autoRouteIndex >= autoRoute.Count)
        {
            AutoBfs(here, 30, ToolRouteKeep);
            var far = new List<GridCoord>();
            foreach (var c in autoReached) if (autoDepth[c] >= 18) far.Add(c);
            if (far.Count == 0) foreach (var c in autoReached) if (autoDepth[c] > 0) far.Add(c);
            if (far.Count == 0) return true;
            ToolRouteTo(here, far[UnityEngine.Random.Range(0, far.Count)]);
        }
        AutoExitWalk(dt, here, out local);
        return true;
    }

    /// <summary>
    /// The tick's half of a walk to <paramref name="goal"/> along autoRoute: plans again (at most once a second) when the
    /// route ran out or broke, and after two stucks places the walker at <paramref name="fallback"/> (true then, with the
    /// row's note). False while it walks; the caller moves on once the walker stands in the goal cell.
    /// </summary>
    bool ToolRouteWalkTick(GridCoord goal, Vector3 fallback, float fallbackYaw, ref string note)
    {
        var here = map.CellOf(playerRoot.position);
        if (here == goal) return false;
        if (toolRouteStucks >= 2)
        {
            note = "walked " + AutoF(toolRouteClock, "0.0") + " s, then stuck twice in " + here + ": placed at the stand by tools";
            ToolRoutePlace(fallback, fallbackYaw);
            return true;
        }
        if (autoRouteIndex < autoRoute.Count || toolRouteClock < toolRouteReplanAt) return false;
        toolRouteReplanAt = toolRouteClock + 1f;
        AutoBfs(here, 90, ToolRouteKeep);
        ToolRouteTo(here, goal);
        return false;
    }

    // ---------- X10: -autopilotRunRoom, a forced Run! room's loop ----------

    const int RrReady = 0, RrMeet = 1, RrFind = 2, RrWalk = 3, RrLineUp = 4, RrIn = 5, RrSprint = 6, RrGoal = 7, RrReset = 8, RrEnd = 9;
    // How far outside the arch the walker lines up (metres, on the axis, inside the arch's outside cell).
    const float RunLineUpMetres = 1.2f;
    FrontRoomsRunRoom runRoom;
    // Rooms the walk could not get to (by id): the next pick skips them; after three the route fails.
    readonly HashSet<int> runSkipped = new HashSet<int>();
    bool runHooked, runLined, runFacing, runToolOpened;
    int runTrips, runResets, runPickDepth, runSentAway, runCallsAtTrip, runApproachStucks;
    float runTripZ, runTripAt = -1f, runGoalAt = -1f, runSprintSeconds, runTopSpeed, runLastDt, runDoorStall, runSendAwayAt = -1f, runNextLook, runAwayAt = -1f;
    Vector3 runLastFeet;
    string runDoorVerb = "", runWalkNote = "", runTripText = "", runAwayText = "";

    void RunRoomHook()
    {
        if (runHooked) return;
        runHooked = true;
        map.RoomTriggered += RunRoomTriggered;
        map.RoomReset += RunRoomReset;
        map.DoorSwingStarted += RunRoomSwing;
    }

    void RunRoomUnhook()
    {
        if (!runHooked || map == null) return;
        runHooked = false;
        map.RoomTriggered -= RunRoomTriggered;
        map.RoomReset -= RunRoomReset;
        map.DoorSwingStarted -= RunRoomSwing;
    }

    void RunRoomTriggered(ModuleTrigger trigger, FrontRoomsRunRoom room, string tag)
    {
        if (room != runRoom || playerRoot == null) return;
        runTrips++;
        if (runTripAt >= 0f) return;
        runTripAt = autoPlayClock;
        runTripZ = RunLocalZ(out var x);
        runTripText = "RoomTriggered(" + trigger + ", room " + room.Id + ", \"" + tag + "\") with the walker " + AutoF(runTripZ) + " m in, " + AutoF(x) + " m off the axis";
    }

    void RunRoomReset(FrontRoomsRunRoom room)
    {
        if (room != runRoom) return;
        runResets++;
        if (runAwayAt >= 0f) return;
        runAwayAt = autoPlayClock;
#if FRONTROOMS_RELAY_V2
        runAwayText = "the director's Away (" + (director != null ? director.LastAwayReason.ToString() : "no director") + ")";
#else
        runAwayText = "v1's calm bridge (" + AutoF(RunResetCalmSecondsV1, "0") + " s of a calm Relay after the trip)";
#endif
    }

    void RunRoomSwing(FrontRoomsMapWorld.Door door, FrontRoomsMapWorld.DoorSwingKind kind)
    {
        if (runRoom == null || door == null || runDoorVerb.Length > 0) return;
        if (door == map.DoorBetween(runRoom.GoalCell, runRoom.Beyond)) runDoorVerb = kind.ToString();
    }

    // The walker's corridor-local z (from the entrance line toward the goal) and x (off the axis), in metres.
    float RunLocalZ(out float x)
    {
        var p = map.transform.InverseTransformPoint(playerRoot.position);
        runRoom.ToLocal(p.x, p.z, out x, out var z);
        return z;
    }

    Vector3 RunPoint(float x, float y, float z) => map.RunRoomPoint(runRoom, new Vector3(x, y, z));

    string RunRoomText() => "Run! room " + runRoom.Id + " (zone " + runRoom.Zone + " " + runRoom.Theme + ", cells " + runRoom.Cells[0] + " -> " + runRoom.GoalCell
        + ", yaw " + AutoF(runRoom.Yaw, "0") + ", arch from " + runRoom.Outside + ", goal door to " + runRoom.Beyond + " " + runRoom.BeyondHeight + ")";

    // A room the route may still use: Armed, never tripped, not one it gave up on.
    bool RunRoomUsable(FrontRoomsRunRoom r) => r.State == RunRoomState.Armed && !r.WaitForExit && r.Trips == 0 && !runSkipped.Contains(r.Id);

    // The nearest built, usable room whose arch the walk reaches without crossing any Run! room (an Office room counts 12 cells
    // further: its cubicles may stand round the arch); false when none.
    bool RunRoomPick()
    {
        var rooms = map.RunRooms.Rooms;
        var here = map.CellOf(playerRoot.position);
        AutoBfs(here, 90, ToolRouteKeep);
        FrontRoomsRunRoom best = null;
        var bestScore = int.MaxValue;
        for (var i = 0; i < rooms.Count; i++)
        {
            var r = rooms[i];
            if (!RunRoomUsable(r) || !map.IsRunRoomBuilt(r) || !autoDepth.TryGetValue(r.Outside, out var d)) continue;
            var score = d + (r.Theme == ZoneTheme.Office ? 12 : 0);
            if (score >= bestScore) continue;
            best = r;
            bestScore = score;
            runPickDepth = d;
        }
        if (best == null) return false;
        runRoom = best;
        ToolRouteTo(here, best.Outside);
        return true;
    }

    /// <summary>
    /// While no room can be picked: a route toward the nearest usable room that is planned but not reachable yet (its chunks
    /// not built, or the way not open), to the reached cell nearest its arch, so the map builds round it. False when there is
    /// none or no reached cell is nearer it than the walker (the far routes then go on).
    /// </summary>
    bool RunRoomApproach(GridCoord here)
    {
        var rooms = map.RunRooms.Rooms;
        FrontRoomsRunRoom near = null;
        var nearCells = int.MaxValue;
        for (var i = 0; i < rooms.Count; i++)
        {
            var r = rooms[i];
            if (!RunRoomUsable(r)) continue;
            var m = Mathf.Abs(r.Outside.x - here.x) + Mathf.Abs(r.Outside.y - here.y);
            if (m < nearCells)
            {
                near = r;
                nearCells = m;
            }
        }
        if (near == null || nearCells == 0) return false;
        AutoBfs(here, 40, ToolRouteKeep);
        var goal = here;
        var best = nearCells;
        foreach (var c in autoReached)
        {
            var m = Mathf.Abs(near.Outside.x - c.x) + Mathf.Abs(near.Outside.y - c.y);
            if (m < best)
            {
                best = m;
                goal = c;
            }
        }
        if (goal == here) return false;
        ToolRouteTo(here, goal);
        return true;
    }

    void RunRoomFrames(string state)
    {
        var tag = "run_room" + runRoom.Id + "_";
        // From inside the arch's outside cell (3 m deep), on the axis: the arch and the shared tell hung at it.
        AutopilotLookFrom(RunPoint(0f, EyeHeight, -2.6f), RunPoint(0f, 1.6f, .2f), tag + "arch_" + state);
    }

    void RunRoomTick(float dt)
    {
        var here = map.CellOf(playerRoot.position);
        switch (toolRouteStep)
        {
            case RrReady:
                if (!ToolRouteFloorReady || !ToolRouteFirstSendOut)
                {
                    if (toolRouteClock > ToolRouteReadySeconds)
                        ToolRouteFail("not ready " + AutoF(ToolRouteReadySeconds, "0") + " s into the route: floor 1's station rows " + (ToolRouteFloorReady ? "in" : "NOT in") + ", the run's first send "
                            + (ToolRouteFirstSendOut ? "out" : "NOT out") + " (onboarding off: " + OnboardOffForTools + ")");
                    return;
                }
                if (!map.RunRoomsOn)
                {
                    ToolRouteFail("Run! rooms are off on this map: -autopilotRunRoom turns them on through FrontRoomsMainScenePlaytest (FrontRoomsMapWorld.RunRoomsForTools 2)");
                    return;
                }
                ToolRouteNext(RrMeet);
                return;
            case RrMeet:
                // The Relay on the map once before it is held (PASS needs it there once; held in Arrive it would never step in).
                if (!ToolRouteRelayMet && toolRouteClock < 45f) return;
                ToolRouteHold(true);
                RunRoomHook();
                ToolRouteRow(runRoomRows, map.RunRoomsOn && map.RunRooms.Rules.force, "Run! rooms on, placement " + (map.RunRooms.Rules.force ? "forced" : "NOT forced") + " (tools); the run's first send out, the Relay "
                    + (ToolRouteRelayMet ? "on the map once" : "NOT yet on the map after 45 s") + "; now held where it stands (" + (relay != null ? relay.State.ToString() : "none")
                    + ", HeldForTools) for the loop; " + map.RunRooms.Placed + " room(s) planned so far on floor " + floor);
                runNextLook = 0f;
                ToolRouteNext(RrFind);
                return;
            case RrFind:
                if (toolRouteClock < runNextLook) return;
                runNextLook = toolRouteClock + .5f;
                if (RunRoomPick())
                {
                    Log("AUTOPILOT RUN ROOM going to " + RunRoomText() + ", " + runPickDepth + " cells to its arch");
                    runWalkNote = "";
                    ToolRouteNext(RrWalk);
                    return;
                }
                if (toolRouteClock > ToolRouteReadySeconds)
                    ToolRouteFail("no built, Armed Run! room the walk could reach in " + AutoF(ToolRouteReadySeconds, "0") + " s (" + map.RunRooms.Placed + " planned on floor " + floor + ")");
                return;
            case RrWalk:
            {
                if (runRoom.State != RunRoomState.Armed || runRoom.Trips > 0 || !map.IsRunRoomBuilt(runRoom))
                {
                    Log("AUTOPILOT RUN ROOM room " + runRoom.Id + " is no longer built and Armed (" + runRoom.State + "): looking again");
                    ToolRouteNext(RrFind);
                    return;
                }
                var line = RunPoint(0f, 0f, -RunLineUpMetres);
                if (here == runRoom.Outside || ToolRouteWalkTick(runRoom.Outside, line, runRoom.Yaw, ref runWalkNote))
                {
                    runLined = runFacing = false;
                    ToolRouteNext(RrLineUp);
                    return;
                }
                if (toolRouteClock <= 60f) return;
                runSkipped.Add(runRoom.Id);
                if (runSkipped.Count >= 3)
                {
                    ToolRouteFail("could not walk to the arch of " + RunRoomText() + " in 60 s (in " + here + "), the third room the walk could not get to");
                    return;
                }
                Log("AUTOPILOT RUN ROOM could not walk to the arch of room " + runRoom.Id + " in 60 s (in " + here + "): another room");
                ToolRouteNext(RrFind);
                return;
            }
            case RrLineUp:
                if (!runLined)
                {
                    if (Flat(RunPoint(0f, 0f, -RunLineUpMetres) - playerRoot.position).magnitude < .12f || toolRouteClock > 5f) runLined = true;
                    return;
                }
                if (!runFacing && toolRouteClock < 7f) return;
                RunLocalZ(out var lineX);
                ToolRouteRow(runRoomRows, runRoom.State == RunRoomState.Armed && runRoom.Trips == 0 && !runRoom.WaitForExit && map.IsRunRoomBuilt(runRoom) && runTrips == 0,
                    "armed: " + RunRoomText() + " built and " + runRoom.State + ", " + runRoom.Trips + " trips; the walker " + runPickDepth + " cells away when it was picked, now lined up "
                    + AutoF(-RunLocalZ(out _)) + " m outside its arch, " + AutoF(lineX) + " m off the axis" + (runWalkNote.Length > 0 ? " (" + runWalkNote + ")" : ""));
                RunRoomFrames("armed");
                AutopilotLookFrom(RunPoint(0f, EyeHeight, -.6f), RunPoint(0f, 1.2f, runRoom.LengthMetres), "run_room" + runRoom.Id + "_corridor_armed");
#if FRONTROOMS_RELAY_V2
                runCallsAtTrip = director != null ? director.Calls + director.Redispatches : 0;
#endif
                ToolRouteNext(RrIn);
                return;
            case RrIn:
            {
                if (runTripAt >= 0f)
                {
                    var calls = "";
#if FRONTROOMS_RELAY_V2
                    calls = "; the device's call: director calls and re-dispatches " + runCallsAtTrip + " -> " + (director != null ? director.Calls + director.Redispatches : 0) + " (" + (director != null ? director.Phase.ToString() : "none") + ")";
#endif
                    ToolRouteRow(runRoomRows, runTrips == 1 && runRoom.State == RunRoomState.Tripped && runRoom.Trips == 1 && runTripZ >= FrontRoomsRunRooms.ArmDepth - .05f && runTripZ <= FrontRoomsRunRooms.ArmDepth + .6f,
                        "the trip: " + runTripText + " (it arms " + AutoF(FrontRoomsRunRooms.ArmDepth, "0.0") + " m in); the room " + runRoom.State + ", " + runRoom.Trips + " trip" + (runRoom.Trips == 1 ? "" : "s") + calls);
                    RunRoomFrames("tripped");
                    runSprintSeconds = runTopSpeed = runDoorStall = 0f;
                    runLastFeet = playerRoot.position;
                    runLastDt = dt;
                    ToolRouteNext(RrSprint);
                    return;
                }
                var z = RunLocalZ(out _);
                if (z > FrontRoomsRunRooms.ArmDepth + 2.5f || toolRouteClock > 10f)
                    ToolRouteFail("no trip: the walker " + AutoF(z) + " m into " + RunRoomText() + " (it arms " + AutoF(FrontRoomsRunRooms.ArmDepth, "0.0") + " m in), " + runRoom.State + ", " + runTrips + " RoomTriggered");
                return;
            }
            case RrSprint:
            {
                var feet = playerRoot.position;
                if (runLastDt > 1e-4f) runTopSpeed = Mathf.Max(runTopSpeed, Flat(feet - runLastFeet).magnitude / runLastDt);
                runLastFeet = feet;
                runLastDt = dt;
                if (PlayerSprinting) runSprintSeconds += dt;
                var last = runRoom.GoalCell;
                if (here == runRoom.Beyond)
                {
                    runGoalAt = autoPlayClock;
                    var took = runGoalAt - runTripAt;
                    ToolRouteRow(runRoomRows, runSprintSeconds >= 1f && runTopSpeed >= Run - .6f, "the sprint: Shift held from the trip to the goal door, " + AutoF(runSprintSeconds) + " s of it sprinting in "
                        + AutoF(took) + " s, top speed " + AutoF(runTopSpeed) + " m/s (run " + AutoF(Run, "0.0") + ", walk " + AutoF(Walk, "0.0") + "), stamina left " + AutoF(stamina) + " s");
                    ToolRouteRow(runRoomRows, map.PassageBetween(last, runRoom.Beyond) == FrontRoomsMapWorld.Passage.Open && runTrips == 1,
                        "the goal: through the goal door " + last + "-" + runRoom.Beyond + " (" + (runToolOpened ? "opened by the tools' TryOpenDoor after the leaf stood 2.5 s" : runDoorVerb.Length > 0 ? runDoorVerb + " by the body" : "open")
                        + ") into the far cell " + runRoom.Beyond + " (" + runRoom.BeyondHeight + ") " + AutoF(took) + " s after the trip");
                    ToolRouteNext(RrGoal);
                    return;
                }
                if (here == last && map.PassageBetween(last, runRoom.Beyond) == FrontRoomsMapWorld.Passage.ClosedDoor)
                {
                    runDoorStall += dt;
                    if (!map.BodyDoors)
                    {
                        // No body doors: the map's TryOpenDoor is the usual walk's way through a shut door.
                        if (map.TryOpenDoor(last, runRoom.Beyond))
                        {
                            autoDoorsOpened++;
                            runDoorVerb = "TryOpenDoor (no body doors on this map, the usual walk's open)";
                        }
                    }
                    // Body doors: the sprint's push opens it; a leaf that has not moved in 2.5 s gets the tools' open.
                    else if (!runToolOpened && runDoorStall > 2.5f && map.TryOpenDoor(last, runRoom.Beyond))
                    {
                        runToolOpened = true;
                        autoDoorsOpened++;
                    }
                }
                if (toolRouteClock > 15f) ToolRouteFail("did not reach the far cell " + runRoom.Beyond + " within 15 s of the trip (in " + here + ", " + AutoF(RunLocalZ(out _)) + " m in, the goal door "
                    + map.PassageBetween(last, runRoom.Beyond) + ")");
                return;
            }
            case RrGoal:
                if (RunLocalZ(out _) < runRoom.LengthMetres + 1.6f && toolRouteClock < 3f) return;
                AutopilotLookFrom(RunPoint(0f, EyeHeight, runRoom.LengthMetres + 2.6f), RunPoint(0f, 1.4f, runRoom.LengthMetres), "run_room" + runRoom.Id + "_goal_door");
#if !FRONTROOMS_RELAY_V2
                // v1 resets on its calm bridge (the Relay calm 20 s after the trip): it must be free to calm down.
                ToolRouteHold(false);
#endif
                runSendAwayAt = -1f;
                ToolRouteNext(RrReset);
                return;
            case RrReset:
                if (runResets > 0)
                {
                    ToolRouteRow(runRoomRows, runRoom.State == RunRoomState.Armed && !runRoom.WaitForExit && runRoom.Trips == 1 && runResets == 1,
                        "the reset: RoomReset " + AutoF(runAwayAt - runTripAt, "0.0") + " s after the trip, by " + runAwayText + " and the game's own ResetRunRooms"
                        + (runSentAway > 0 ? " (the route sent the held Relay Away, SendAway(Test), " + runSentAway + "x: held, it cannot withdraw by itself)" : "")
                        + "; the room " + runRoom.State + " again, WaitForExit " + runRoom.WaitForExit + " (the walker in " + here + "), " + runRoom.Trips + " trip");
                    RunRoomFrames("reset");
                    ToolRouteHold(false);
                    ToolRouteNext(RrEnd);
                    return;
                }
#if FRONTROOMS_RELAY_V2
                if (toolRouteClock > 1f && relay != null && (relay.State == HunterState.Arrive || relay.OnMap) && (runSendAwayAt < 0f || toolRouteClock - runSendAwayAt > 3f))
                {
                    runSendAwayAt = toolRouteClock;
                    runSentAway++;
                    Log("AUTOPILOT RUN ROOM tools: SendAway(Test) for the held Relay (" + relay.State + "), so the director's Away runs the zone reset");
                    relay.SendAway(FrontRoomsRelayAwayReason.Test);
                }
#endif
                if (toolRouteClock > 45f)
                    ToolRouteFail("no reset 45 s after the goal: " + RunRoomText() + " " + runRoom.State + ", the Relay " + (relay != null ? relay.State.ToString() : "none")
#if FRONTROOMS_RELAY_V2
                        + ", director " + (director != null ? director.Phase.ToString() : "none")
#endif
                        + ", " + runSentAway + " SendAway(Test)");
                return;
            case RrEnd:
                if (!ToolRouteRelayMet) ToolRouteRow(runRoomRows, false, "the Relay was never on the map this run (PASS needs it once)");
                ToolRouteEnd("run room loop done");
                return;
        }
    }

    bool RunRoomSteer(float dt, out Vector2 local, out bool sprint)
    {
        local = Vector2.zero;
        sprint = false;
        var here = map.CellOf(playerRoot.position);
        switch (toolRouteStep)
        {
            case RrReady:
                // Floor 1's rows first (the usual walk takes the floor key); then far routes that keep out of every Run! room
                // (forced, rooms are planned in zones met before the first send too, and the usual walk would trip them).
                return ToolRouteFloorReady && ToolRouteExplore(dt, here, out local);
            case RrMeet:
                return ToolRouteExplore(dt, here, out local);
            case RrFind:
                // Toward the nearest room still to be built when there is one; far routes otherwise, and one far route after
                // each stuck on the way toward a room.
                if (autoRouteIndex >= autoRoute.Count)
                {
                    if (toolRouteStucks == runApproachStucks) RunRoomApproach(here);
                    runApproachStucks = toolRouteStucks;
                }
                return ToolRouteExplore(dt, here, out local);
            case RrWalk:
                ToolRouteStuck(dt);
                AutoExitWalk(dt, here, out local);
                return true;
            case RrLineUp:
                if (!runLined) AutoSteerAt(dt, RunPoint(0f, 0f, -RunLineUpMetres), true, out local);
                else runFacing = AutoFace(dt, RunPoint(0f, 0f, 8f), 0f);
                return true;
            case RrIn:
            case RrSprint:
            case RrGoal:
                // Along the axis: the arch, the corridor and the goal door, into the far cell (a step on from the door's line).
                AutoSteerAt(dt, RunPoint(0f, 0f, runRoom.LengthMetres + 2.2f), false, out local);
                sprint = toolRouteStep == RrSprint;
                if (toolRouteStep == RrGoal && RunLocalZ(out _) >= runRoom.LengthMetres + 1.6f) local = Vector2.zero;
                return true;
            case RrReset:
                return true;
        }
        return false;
    }

    // ---------- X11: -autopilotSash, a plain window's sash pried by E ----------

    const int SaReady = 0, SaFind = 1, SaWalk = 2, SaStand = 3, SaHold1 = 4, SaLetGo = 5, SaHold2 = 6, SaClimb = 7, SaDone = 8;
    // The stand off the window's line (metres; the latch then about 0.9 m from the eye, inside GlassBreak.Reach 1.2 m, and the
    // climb's 0.95 m), and the first hold before E is let go (seconds).
    const float SashStandOff = .8f, SashFirstHold = 1.5f;
    FrontRoomsMapWorld.Window sashWindow;
    // Windows (by edge) the walk could not get to or aim at: the next search skips them; after three the route fails.
    readonly HashSet<long> sashSkipped = new HashSet<long>();
    GridCoord sashCell, sashFar;
    Vector3 sashStand, sashLine, sashInto;
    bool sashHooked, sashAtStand, sashPressed, sashHeardOk, sashClimbing, sashPlacedRow;
    int sashProgressFrames, sashReleases, sashPried, sashClimbs, sashDepth, sashLastProgressFrame = -1;
    float sashStepped, sashLastStep, sashBanked, sashResumed = -1f, sashTotal, sashNextLook, sashGaveAt;
    string sashWalkNote = "", sashHeard = "";

    void SashHook()
    {
        if (sashHooked) return;
        sashHooked = true;
        map.SashPryProgress += SashProgress;
        map.SashPryReleased += SashReleased;
        map.SashPried += SashGave;
        PlayerClimbed += SashClimbed;
    }

    void SashUnhook()
    {
        if (!sashHooked) return;
        sashHooked = false;
        if (map != null)
        {
            map.SashPryProgress -= SashProgress;
            map.SashPryReleased -= SashReleased;
            map.SashPried -= SashGave;
        }
        PlayerClimbed -= SashClimbed;
    }

    void SashProgress(FrontRoomsMapWorld.Window w, float progress)
    {
        if (w != sashWindow) return;
        sashProgressFrames++;
        sashLastProgressFrame = Time.frameCount;
        // The game's step this frame (UpdateMapPlay's dt), so the sum is the sash's own hold.
        sashLastStep = Mathf.Min(Time.deltaTime, .1f);
        sashStepped += sashLastStep;
        if (toolRouteStep == SaHold2 && sashResumed < 0f) sashResumed = progress;
    }

    void SashReleased(FrontRoomsMapWorld.Window w)
    {
        if (w == sashWindow) sashReleases++;
    }

    // After the game's own OnSashPried (subscribed at the run's start, so its noise has gone to the Relay): what the Relay did with it.
    void SashGave(FrontRoomsMapWorld.Window w)
    {
        if (w != sashWindow) return;
        sashPried++;
        if (sashPried > 1) return;
        sashTotal = sashStepped;
        sashGaveAt = autoPlayClock;
        if (relay == null)
        {
            sashHeard = "no Relay";
            return;
        }
#if FRONTROOMS_RELAY_V2
        var heard = relay.LastHeard;
        sashHeardOk = heard == FrontRoomsRelayHeard.Linked || heard == FrontRoomsRelayHeard.Retargeted || heard == FrontRoomsRelayHeard.Repeat;
        sashHeard = "the Relay (" + relay.State + ") " + heard + (sashHeardOk ? ": " + AutoF(relay.LastHeardMetres, "0.0") + " m of path inside its range " + AutoF(relay.LastHeardRange, "0.0")
            + " m (loudness " + AutoF(hunterTuning.pryLoudness, "0.#") + " x hearing " + AutoF(relay.HearingV2) + "), the estimate " + relay.LastHeardK + " cells off" : "");
#else
        sashHeardOk = relay.State == HunterState.Hunt && relay.ListenPoint.HasValue && (relay.ListenPoint.Value - w.position).sqrMagnitude < 1e-4f;
        sashHeard = "the Relay " + relay.State + (sashHeardOk ? ", hunting the window (v1: Noise of " + AutoF(hunterTuning.pryLoudness, "0.#") + " m)" : "");
#endif
    }

    void SashClimbed(Vector3 at)
    {
        if (this == null)
        {
            PlayerClimbed -= SashClimbed;
            return;
        }
        if (toolRouteStep == SaClimb) sashClimbs++;
    }

    // A hold under way that has had no progress for 3 frames while the latch is under the crosshair: E goes down again this frame.
    bool SashPressAgain => sashProgressFrames > 0 && sashPried == 0 && Time.frameCount - sashLastProgressFrame > 3 && sashWindow != null && aimed == sashWindow.latch;

    // The latch's centre (on the window's line, at the meeting rail) and the pitch that puts it under the crosshair from the stand.
    Vector3 SashLatchPoint => sashWindow != null && sashWindow.latch != null ? sashWindow.latch.transform.position : sashLine + Vector3.up * 1.2f;

    float SashPitch()
    {
        var to = SashLatchPoint - playerRoot.position;
        var flat = new Vector2(to.x, to.z).magnitude;
        return Mathf.Atan2(EyeHeight - to.y, Mathf.Max(.1f, flat)) * Mathf.Rad2Deg;
    }

    /// <summary>
    /// The nearest plain window the walk reaches (an intact pane with a latch, not foiled), breadth-first over built cells
    /// through open edges and doors, never into a Run! room: the cell on the walker's side, the far cell, the route there.
    /// </summary>
    bool SashFind()
    {
        var here = map.CellOf(playerRoot.position);
        AutoBfs(here, 60, ToolRouteKeep);
        foreach (var cell in autoReached)
            foreach (var step in AutoSteps)
            {
                var far = cell + step;
                if (!map.IsBuilt(far) || map.PassageBetween(cell, far) != FrontRoomsMapWorld.Passage.Glass) continue;
                var w = map.NearestIntactWindowForTools(map.CrossingPoint(cell, far));
                if (w == null || w.latch == null || w.foiled || w.pane == null || w.root == null || sashSkipped.Contains(w.edge)) continue;
                if (!((w.a == cell && w.b == far) || (w.a == far && w.b == cell))) continue;
                sashWindow = w;
                sashCell = cell;
                sashFar = far;
                sashDepth = autoDepth[cell];
                sashLine = map.CrossingPoint(cell, far);
                sashInto = map.CellCenter(far) - map.CellCenter(cell);
                sashInto.y = 0f;
                sashInto.Normalize();
                sashStand = sashLine - sashInto * SashStandOff;
                sashStand.y = playerRoot.position.y;
                ToolRouteTo(here, cell);
                return true;
            }
        return false;
    }

    // One open cell off the stand's, as far from the window as it can be: the Relay placed there by tools and held, so the
    // pry's noise (path metres well inside its 8 m) has an ear. Under the define DebugPlaceOnMap is the director's.
    void SashPlaceRelay()
    {
        var found = false;
        var best = sashCell;
        var bestScore = float.NegativeInfinity;
        foreach (var step in AutoSteps)
        {
            var m = sashCell + step;
            if (m == sashFar || !map.IsBuilt(m) || map.PassageBetween(sashCell, m) != FrontRoomsMapWorld.Passage.Open || !ToolRouteKeep(m)) continue;
            var dir = map.CellCenter(m) - map.CellCenter(sashCell);
            dir.y = 0f;
            var score = -Vector3.Dot(dir.normalized, sashInto);
            if (found && score <= bestScore) continue;
            found = true;
            best = m;
            bestScore = score;
        }
        var placed = found && DebugPlaceOnMap(map.CellCenter(best));
        if (relay != null) relay.HeldForTools = true;
        ToolRouteRow(sashRows, placed && relay != null && relay.Released, "the Relay placed by tools (DebugPlaceOnMap) in " + (found ? best.ToString() : "NO open cell") + ", one cell off the stand's "
            + sashCell + ", and held there (" + (relay != null ? relay.State.ToString() : "none") + "), so the pry's noise has an ear in range");
    }

    // Before E ever went down on it: this window is skipped and the next one looked for; the third such window fails the route.
    void SashSkip(string why)
    {
        sashSkipped.Add(sashWindow.edge);
        if (sashSkipped.Count >= 3)
        {
            ToolRouteFail(why + "; the third window the route could not use");
            return;
        }
        Log("AUTOPILOT SASH " + why + ": another window");
        sashNextLook = 0f;
        ToolRouteNext(SaFind);
    }

    void SashTick()
    {
        var here = map.CellOf(playerRoot.position);
        switch (toolRouteStep)
        {
            case SaReady:
                if (!map.SashPry)
                {
                    ToolRouteFail("the sash pry is off on this map: -autopilotSash turns it on through FrontRoomsMainScenePlaytest (FrontRoomsMapWorld.SashPryForTools)");
                    return;
                }
                if (FrontRoomsSettings.TapToBreak)
                {
                    ToolRouteFail("tap mode is on (FrontRoomsSettings.TapToBreak): this route holds E");
                    return;
                }
                if (!ToolRouteFloorReady || !ToolRouteFirstSendOut)
                {
                    if (toolRouteClock > ToolRouteReadySeconds)
                        ToolRouteFail("not ready " + AutoF(ToolRouteReadySeconds, "0") + " s into the route: floor 1's station rows " + (ToolRouteFloorReady ? "in" : "NOT in") + ", the run's first send "
                            + (ToolRouteFirstSendOut ? "out" : "NOT out") + " (onboarding off: " + OnboardOffForTools + ")");
                    return;
                }
                ToolRouteHold(true);
                SashHook();
                sashNextLook = 0f;
                ToolRouteNext(SaFind);
                return;
            case SaFind:
                if (toolRouteClock < sashNextLook) return;
                sashNextLook = toolRouteClock + .5f;
                if (SashFind())
                {
                    Log("AUTOPILOT SASH going to the window " + sashWindow.a + "-" + sashWindow.b + ", " + sashDepth + " cells to its side " + sashCell);
                    sashWalkNote = "";
                    ToolRouteNext(SaWalk);
                    return;
                }
                if (toolRouteClock > ToolRouteReadySeconds) ToolRouteFail("no plain window (an intact pane with a latch, no foil) the walk could reach in " + AutoF(ToolRouteReadySeconds, "0") + " s");
                return;
            case SaWalk:
                if (sashWindow.latch == null || sashWindow.pane == null)
                {
                    Log("AUTOPILOT SASH the window " + sashWindow.a + "-" + sashWindow.b + " went (its chunk dropped or rebuilt): looking again");
                    ToolRouteNext(SaFind);
                    return;
                }
                if (here == sashCell || ToolRouteWalkTick(sashCell, sashStand, Mathf.Atan2(sashInto.x, sashInto.z) * Mathf.Rad2Deg, ref sashWalkNote))
                {
                    sashAtStand = false;
                    ToolRouteNext(SaStand);
                    return;
                }
                if (toolRouteClock > 60f) SashSkip("could not walk to the window " + sashWindow.a + "-" + sashWindow.b + " in 60 s (in " + here + ")");
                return;
            case SaStand:
                if (sashWindow.latch == null || sashWindow.pane == null)
                {
                    ToolRouteNext(SaFind);
                    return;
                }
                if (!sashAtStand)
                {
                    if (Flat(sashStand - playerRoot.position).magnitude < .08f) sashAtStand = true;
                    else if (toolRouteClock > 5f)
                    {
                        sashWalkNote += (sashWalkNote.Length > 0 ? "; " : "") + "the stand not reached by walking in 5 s: placed there by tools";
                        ToolRoutePlace(sashStand, Mathf.Atan2(sashInto.x, sashInto.z) * Mathf.Rad2Deg);
                        sashAtStand = true;
                    }
                    return;
                }
                if (aimed != sashWindow.latch || aimKind != FrontRoomsMapWorld.AimKind.Pry)
                {
                    if (toolRouteClock > 9f) SashSkip("the latch of " + sashWindow.a + "-" + sashWindow.b + " never came under the crosshair from the stand (aimed " + (aimed != null ? aimed.name : "nothing") + ", " + aimKind + ")");
                    return;
                }
                var eye = playerRoot.position + Vector3.up * EyeHeight;
                ToolRouteRow(sashRows, true, "the window " + sashWindow.a + "-" + sashWindow.b + " (the walker's side " + sashCell + ", far cell " + sashFar + "): a plain sash, its latch in and no foil, "
                    + sashDepth + " cells from where the route began" + (sashWalkNote.Length > 0 ? " (" + sashWalkNote + ")" : "") + "; the walker " + AutoF(SashStandOff, "0.0") + " m off its line, the latch under the crosshair ("
                    + aimKind + ", \"" + (prompt ?? "") + "\") " + AutoF(Vector3.Distance(eye, SashLatchPoint)) + " m from the eye");
                if (!sashPlacedRow)
                {
                    sashPlacedRow = true;
                    SashPlaceRelay();
                }
                sashPressed = false;
                ToolRouteNext(SaHold1);
                return;
            case SaHold1:
                if (!sashPressed)
                {
                    sashPressed = true;
                    toolRouteE = true;
                    autoEDown = autoEHeld = true;
                    return;
                }
                // E stays held; a hold the aim let go of (no progress for 3 frames, the latch under the crosshair again) is
                // pressed afresh, as a player would.
                autoEDown = SashPressAgain;
                if (sashProgressFrames == 0 && toolRouteClock > .4f)
                {
                    ToolRouteFail("E on the latch started no pry (aimed " + (aimed != null ? aimed.name : "nothing") + ", " + aimKind + ")");
                    return;
                }
                if (sashStepped < SashFirstHold) return;
                autoEDown = autoEHeld = false;
                ToolRouteNext(SaLetGo);
                return;
            case SaLetGo:
                if (toolRouteClock < .5f) return;
                sashBanked = sashWindow.sashHold;
                ToolRouteRow(sashRows, sashReleases >= 1 && sashPried == 0 && sashWindow.latch != null && sashWindow.pane != null && Mathf.Abs(sashBanked - sashStepped) < .002f,
                    "the progress banks: E let go after " + AutoF(sashStepped) + " s of hold (" + sashProgressFrames + " frames of SashPryProgress); SashPryReleased " + sashReleases + "x; 0.5 s off E the sash keeps "
                    + AutoF(sashBanked) + " s (" + AutoF(sashBanked / FrontRoomsMapWorld.SashPryHoldSeconds * 100f, "0") + " % of " + AutoF(FrontRoomsMapWorld.SashPryHoldSeconds, "0") + " s), latch and pane still in");
                sashPressed = false;
                sashResumed = -1f;
                ToolRouteNext(SaHold2);
                return;
            case SaHold2:
                if (!sashPressed)
                {
                    if (aimed != sashWindow.latch)
                    {
                        if (toolRouteClock > 2f) ToolRouteFail("the latch left the crosshair while E was off (aimed " + (aimed != null ? aimed.name : "nothing") + ")");
                        return;
                    }
                    sashPressed = true;
                    autoEDown = autoEHeld = true;
                    return;
                }
                autoEDown = SashPressAgain;
                if (sashPried > 0)
                {
                    autoEHeld = false;
                    toolRouteE = false;
                    var hold = FrontRoomsMapWorld.SashPryHoldSeconds;
                    ToolRouteRow(sashRows, sashResumed > sashBanked / hold, "the hold resumed from the bank: the second hold's first progress " + AutoF(sashResumed, "0.000") + " (the bank alone "
                        + AutoF(sashBanked / hold, "0.000") + ")");
                    ToolRouteRow(sashRows, sashTotal >= hold - .001f && sashTotal - sashLastStep < hold, "the sash gave at " + AutoF(hold, "0") + " s: SashPried after " + AutoF(sashTotal, "0.000")
                        + " s of hold in all (" + AutoF(sashBanked) + " s, let go, then " + AutoF(sashTotal - sashBanked) + " s), on the first frame it could (the frame before: " + AutoF(sashTotal - sashLastStep, "0.000") + " s)");
                    ToolRouteRow(sashRows, sashHeardOk && Mathf.Abs(hunterTuning.pryLoudness - 8f) < 1e-3f, "the noise heard: " + sashHeard + " (v2 loudness 8; the tuning's pryLoudness "
                        + AutoF(hunterTuning.pryLoudness, "0.#") + ")");
                    sashClimbing = false;
                    ToolRouteNext(SaClimb);
                    return;
                }
                if (toolRouteClock > FrontRoomsMapWorld.SashPryHoldSeconds + 1.5f)
                    ToolRouteFail("the sash did not give: " + AutoF(sashStepped) + " s of hold in all, " + sashProgressFrames + " progress frames (aimed " + (aimed != null ? aimed.name : "nothing") + ")");
                return;
            case SaClimb:
                if (climbTime >= 0f) sashClimbing = true;
                if (sashClimbing && climbTime < 0f)
                {
                    ToolRouteRow(sashRows, here == sashFar && sashClimbs == 1 && map.IsOpenWindow(sashCell, sashFar) && map.IsSashPried(sashWindow.edge),
                        "the climb: walked into the pried frame and climbed through to " + here + " (the far cell " + sashFar + "), PlayerClimbed " + sashClimbs + "x, the window open (pried, not broken: "
                        + !map.IsBrokenWindow(sashCell, sashFar) + ")");
                    ToolRouteNext(SaDone);
                    return;
                }
                if (toolRouteClock > 4f) ToolRouteFail("no climb through the pried frame within 4 s (in " + here + ", " + AutoF(Vector3.Dot(playerRoot.position - sashLine, sashInto)) + " m from its line)");
                return;
            case SaDone:
                ToolRouteRow(sashRows, sashPried == 1, "SashPried raised once for the window (" + sashPried + "), " + AutoF(autoPlayClock - sashGaveAt, "0.0") + " s after it gave");
                ToolRouteHold(false);
                if (!ToolRouteRelayMet) ToolRouteRow(sashRows, false, "the Relay was never on the map this run (PASS needs it once)");
                ToolRouteEnd("sash route done");
                return;
        }
    }

    bool SashSteer(float dt, out Vector2 local)
    {
        local = Vector2.zero;
        switch (toolRouteStep)
        {
            case SaWalk:
                ToolRouteStuck(dt);
                AutoExitWalk(dt, map.CellOf(playerRoot.position), out local);
                return true;
            case SaStand:
                if (!sashAtStand) AutoSteerAt(dt, sashStand, true, out local);
                else AutoFace(dt, SashLatchPoint, SashPitch());
                return true;
            case SaHold1:
            case SaLetGo:
            case SaHold2:
                // Standing, the crosshair kept on the latch.
                AutoFace(dt, SashLatchPoint, SashPitch());
                return true;
            case SaClimb:
                // Into the open frame: TryStartClimb takes it from 0.95 m; the climb carries the body through.
                if (climbTime < 0f && !sashClimbing) AutoSteerAt(dt, sashLine + sashInto * 1.5f, false, out local);
                return true;
            case SaDone:
                return true;
        }
        return false;
    }

    // ---------- X16: -autopilotStartFloor N, down to floor N by the game's own seal and change ----------

    const int SfReady = 0, SfDescend = 1, SfOut = 2, SfLook = 3, SfRelay = 4;
    // The floor the walker was last placed in X on (0: none under way), the changes made, and the depth past the EXIT's line.
    int startFloorFrom, startFloorChanges;
    float startFloorDepth;
    bool startFloorHudRow;

    // Which of the floor's candidate seeds the game took (SeedFor tries k = 0, 1, ...): -1 when none of them.
    int StartFloorCandidate(int f, int seed)
    {
        for (var k = 0; k < FrontRoomsExitPlanner.MaxSeedTries; k++)
            if (FrontRoomsExitPlanner.FloorSeed(runSeed, f, k) == seed) return k;
        return -1;
    }

    void StartFloorTick()
    {
        switch (toolRouteStep)
        {
            case SfReady:
                if (!ToolRouteFloorReady && toolRouteClock < ToolRouteReadySeconds) return;
                ToolRouteRow(startFloorRows, ToolRouteFloorReady, "floor 1 first, as usual: its station rows " + (ToolRouteFloorReady ? "in" : "NOT in") + " " + AutoF(autoPlayClock, "0.0")
                    + " s after Space; now down to floor " + toolRouteTarget + " (" + FloorLabel(toolRouteTarget) + ") by the game's own EXIT seal and change of floor, one floor at a time (run seed " + runSeed + ")");
                toolRouteSeal = true;
                startFloorFrom = 0;
                ToolRouteNext(SfDescend);
                return;
            case SfDescend:
                StartFloorDescend();
                return;
            case SfOut:
                if (!startFloorHudRow && map.CellOf(playerRoot.position) == startDoorCell)
                {
                    startFloorHudRow = true;
                    StartFloorHudRow();
                }
                if (floorStartShut)
                {
                    if (!startFloorHudRow)
                    {
                        startFloorHudRow = true;
                        StartFloorHudRow();
                    }
                    ToolRouteRow(startFloorRows, startStairwell == null || startStairwell.NorthDoorLocked, "walked out of the stairwell as after any descent: the north door held " + AutoF(floorHeldFor)
                        + " s" + (floorOpenedLate ? " (its limit)" : "") + ", then shut and locked behind the walker " + AutoF(Vector2.Distance(Flat(playerRoot.position), Flat(startDoorPoint))) + " m out");
                    ToolRouteNext(SfLook);
                    return;
                }
                if (toolRouteClock > 25f) ToolRouteFail("did not walk out of the stairwell on floor " + floor + " within 25 s (the north door opened " + floorDoorOpened + ", in " + map.CellOf(playerRoot.position) + ")");
                return;
            case SfLook:
                StartFloorFrames();
                ToolRouteNext(SfRelay);
                return;
            case SfRelay:
                // The usual walk on floor N until the Relay has been on the map (the floor's first send; PASS needs it once).
                if (ToolRouteRelayMet)
                {
                    ToolRouteEnd("start floor " + floor + " reached");
                    return;
                }
                if (toolRouteClock > 80f) ToolRouteFail("the Relay was not on the map on floor " + floor + " within 80 s of the north door's shut (" + (relay != null ? relay.State.ToString() : "none") + ")");
                return;
        }
    }

    void StartFloorDescend()
    {
        if (startFloorFrom != 0)
        {
            if (floor == startFloorFrom)
            {
                // Placed in X: the seal and the change are the game's (UpdateExitSeal: the gate, SealExit, the blackout, ChangeFloor).
                if (toolRouteClock > 4f) ToolRouteFail("floor " + floor + ": no change of floor 4 s after the walker stood " + AutoF(startFloorDepth) + " m into X (" + floorSwap + ", the EXIT sealing "
                    + map.ExitSealing + ", sealed " + map.ExitSealed + ")");
                return;
            }
            startFloorChanges++;
            var k = StartFloorCandidate(floor, floorSeed);
            ToolRouteRow(startFloorRows, floor == startFloorFrom + 1 && k >= 0 && map.HasStartArea && map.InStartArea(map.CellOf(playerRoot.position)) && startStairwell != null && startStairwell.Joined,
                "floor " + startFloorFrom + " -> " + floor + " (" + FloorLabel(floor) + "): the game's gate sealed the EXIT behind the walker " + AutoF(startFloorDepth) + " m into X, the floor changed in "
                + AutoF(floorSwapMs, "0.0") + " ms; seed " + floorSeed + " (SeedFor's candidate " + k + " of the run seed, this floor and X), tier " + tier + "; the walker in the start stairwell; the next EXIT "
                + (exitPlan.found ? exitPlan.exitCell + ", " + exitPlan.distance + " walking cells" : "NONE") + (map.FloorHasStation ? "; this floor has its own station" : ""));
            startFloorFrom = 0;
            if (floor >= toolRouteTarget) StartFloorArrived();
            else ToolRouteNext(SfDescend);
            return;
        }
        if (floor >= toolRouteTarget)
        {
            StartFloorArrived();
            return;
        }
        // This floor's EXIT planned, no change under way, a moment after the last change (the floor start), nothing holding the body.
        if (floorSwap != FloorSwap.None || exitStairwell == null || !map.HasExitArea || exitRefused || map.ExitSealing)
        {
            if (toolRouteClock > 15f) ToolRouteFail("floor " + floor + ": no EXIT to go down by 15 s after the floor began (planned " + exitPlan.found + ", refused " + exitRefused + ")");
            return;
        }
        if (floor >= 2 && floorClock < .35f) return;
        if (climbTime >= 0f || pullClear.HasValue || (rig != null && rig.InShot) || (glassShot != null && (glassShot.Active || glassShot.Blending))
            || (doorShot != null && (doorShot.UnlockActive || doorShot.KeyClock >= 0f))) return;
        // As a player who has walked into X: 0.35 m past its middle, facing north (where the EXIT walker stands for the seal).
        var north = AutoExitNorth();
        var feet = map.CellCenter(map.ExitCell) + north * .35f;
        ToolRoutePlace(feet, Mathf.Atan2(north.x, north.z) * Mathf.Rad2Deg);
        startFloorDepth = ExitDepth(feet);
        startFloorFrom = floor;
        ToolRouteNext(SfDescend);
    }

    void StartFloorArrived()
    {
        toolRouteSeal = false;
        ToolRouteRow(startFloorRows, floor == toolRouteTarget && startFloorChanges == toolRouteTarget - 1, "floor " + floor + " reached (" + FloorLabel(floor) + "), " + startFloorChanges
            + " changes of floor, " + AutoF(autoPlayClock, "0.0") + " s after Space: seed " + floorSeed + ", tier " + tier + " (the seed a descent at this tier gives), the walker in the start stairwell");
        StartFloorPlacardRow();
        startFloorHudRow = false;
        ToolRouteNext(SfOut);
    }

    // The landing placard on this floor (BS6: B1 to B9, B10+ from floor 11; full with a station and a locked EXIT, else Phase 0).
    void StartFloorPlacardRow()
    {
        var p = FrontRoomsLandingPlacard.Of(map);
        var full = map.FloorHasStation && map.ExitLocked;
        var want = FrontRoomsLandingPlacardSheets.For(floor - 1, full);
        var ok = p != null && p.Floor == floor && p.Sheet != null && want != null && p.Sheet.level == want.level && p.Sheet.full == full;
        ToolRouteRow(startFloorRows, ok, "the landing placard on " + FloorLabel(floor) + ": " + (p == null ? "NO landing placard on the map" : "hung for floor " + p.Floor + ", sheet "
            + (p.Sheet != null ? p.Sheet.level + (p.Sheet.full ? " full (station, EXIT locked)" : " Phase 0") + " from " + p.Sheet.folder + ", print " + (p.PrintMode ?? "none")
            : "NONE (" + (p.NoSheetReason ?? "no reason given") + "; another -autopilotSeed may hang one)")) + "; this floor's sheet is " + (want != null ? want.level + (full ? " full" : " Phase 0") : "none"));
    }

    // The HUD out of the stairwell, in the start door's cell: the room line (FloorLabel; an Office zone names LEVEL 4), the
    // floor token the key HUD prints for this floor (FrontRoomsKeyHud.FloorToken), and the key identity's floor.
    void StartFloorHudRow()
    {
        var cell = map.CellOf(playerRoot.position);
        var zone = map.ZoneOf(cell);
        var label = FloorLabel(floor);
        var text = roomText != null ? roomText.text : "";
        var office = zone.theme == ZoneTheme.Office;
        var token = FrontRoomsKeyHud.FloorToken(floor - 1);
        var number = HudIdentity(FrontRoomsMapWorld.FloorKeyZone).number;
        var ok = (office ? text == ZoneName(zone) : text.Contains(label)) && token == "B" + (floor - 1) && number == floor - 1 && FrontRoomsKeyIdentity.CurrentFloor == floor;
        ToolRouteRow(startFloorRows, ok, "the HUD on " + label + " in " + cell + ": the room line \"" + text + "\"" + (office ? " (an Office zone: LEVEL 4)" : "") + ", the floor token " + token
            + " (the key HUD's identity number " + number + "), the key identity's floor " + FrontRoomsKeyIdentity.CurrentFloor);
    }

    // Frames: the landing placard head on from 1.6 m (its frame's centre), and the landing from the floor (the north door, the copy at its west).
    void StartFloorFrames()
    {
        var p = FrontRoomsLandingPlacard.Of(map);
        var north = AutoExitNorthOf(startDoorPoint);
        var shown = p != null && p.Frame != null && p.Root != null && p.Root.activeSelf;
        if (shown)
        {
            var any = false;
            var bounds = new Bounds(p.Frame.transform.position, Vector3.zero);
            foreach (var r in p.Frame.GetComponentsInChildren<Renderer>(true))
            {
                if (!any) bounds = r.bounds;
                else bounds.Encapsulate(r.bounds);
                any = true;
            }
            var centre = bounds.center;
            var from = centre + north * 1.6f;
            from.y = map.CellCenter(startDoorCell).y + EyeHeight;
            AutopilotLookFrom(from, centre, "startfloor" + floor + "_landing_placard");
        }
        AutopilotLookFrom(startDoorPoint + north * 3.2f + Vector3.up * EyeHeight, startDoorPoint + Vector3.up * 1.3f, "startfloor" + floor + "_landing");
        ToolRouteRow(startFloorRows, shown, "the landing placard " + (shown ? "shows (its wall's chunks built)" : "does NOT show" + (p == null ? " (none)" : p.Placed ? " (hung, its chunks not built)" : " (not hung)"))
            + "; frames " + (shown ? "startfloor" + floor + "_landing_placard and " : "") + "startfloor" + floor + "_landing (" + autoShots + " frames so far)");
    }

    bool StartFloorSteer(float dt, out Vector2 local)
    {
        local = Vector2.zero;
        switch (toolRouteStep)
        {
            case SfDescend:
            case SfLook:
                // Standing: placed in X by the tick, the seal and the change are the game's.
                return true;
            case SfOut:
                StartFloorWalkOut(dt, out local);
                return true;
        }
        // Floor 1 before the descent, and floor N once out: the usual walk.
        return false;
    }

    /// <summary>Out of the stairwell as the EXIT walker leaves it (its ExitOut step): the leaf, the door cell, the cell beyond.</summary>
    void StartFloorWalkOut(float dt, out Vector2 local)
    {
        local = Vector2.zero;
        if (startStairwell == null || !floorDoorOpened) return;
        if (map.BodyDoors ? !startStairwell.NorthDoorReady && !floorStartShut : startStairwell.NorthDoorAngle < FrontRoomsMapWorld.DoorPassableDegrees && !floorStartShut) return;
        var feet = playerRoot.position;
        var here = map.CellOf(feet);
        var north = AutoExitNorthOf(feet);
        var door = startDoorCell;
        var ahead = new GridCoord(door.x, door.y + 1);
        var reach = map.DoorBetween(door, ahead) != null ? 0f : 1.2f;
        if (map.InStartArea(here) || (here == door && Vector3.Dot(feet - map.CellCenter(door), north) < reach - .2f))
        {
            AutoSteerAt(dt, map.CellCenter(door) + north * reach, false, out local);
            return;
        }
        if (here == door)
        {
            if (autoRouteIndex >= autoRoute.Count || autoRoute[autoRouteIndex] != ahead)
            {
                autoRoute.Clear();
                autoRoute.Add(ahead);
                autoRouteIndex = 0;
                autoExitAtWaypoint = false;
            }
            if (!AutoExitWalk(dt, here, out local)) AutoSteerAt(dt, map.CellCenter(ahead), false, out local);
            return;
        }
        if (here == ahead) AutoSteerAt(dt, map.CellCenter(ahead) + north * .5f, true, out local);
    }
}
#endif
