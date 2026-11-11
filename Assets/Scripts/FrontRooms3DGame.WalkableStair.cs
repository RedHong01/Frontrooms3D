using System;
using System.Globalization;
using FrontRooms.Map;
using UnityEngine;

// ---------- The walkable stair, the game's half (contract Documentation/research/stairwell/60_walkable_stair_for_map.md v1.1) ----------
// Red, 2026-10-22 13:00 in the visual chat: "...选 A、C22 合进去之后做真楼梯..."; restarted 2026-11-10 ("现在就开"). MAP_GENERATION.md,
// "Walkable stair", has the cells, the edges and the order of the change. Everything here runs only on a run that asked for it
// (FrontRoomsLevelProfile.walkableStair, or the tools' -autopilotWalkableStair) with a shell factory set by the visual line's stair
// package (FrontRoomsStairShellHost.Create). Without both, stairRun is false, exitShell and startShell stay null, and every line of
// this file is skipped: the run is C22's, byte for byte.
//
// A walkable-stair floor: the EXIT door from N opens onto X1, the shell's exit landing; X2 is the turn end. Stepping 1.40 m past the
// EXIT line seals it as C22 does, but the player walks on: a player-only blocker 0.1 m inside the line keeps them from the closing
// leaf, EXAMINE waits, and at least StairLatchToChange after the latch, with the feet in X1 or X2, the floor changes in one frame
// (variant B: the map root never moves, the shell and the player move up FloorToFloor). The walk down the flights covers the new
// floor's build; the far door, directly under the EXIT, opens on arrival at the far landing once the map round it is ready, or
// after a hold of at most StartDoorHoldLimit.
public sealed partial class FrontRooms3DGame
{
    /// <summary>
    /// The walkable stair's change of floor moved the player, and the shell round them, by this much (+FloorToFloor, up): raised
    /// once per change, after the move and Physics.SyncTransforms and before FloorChanged. The map root never moves (variant B),
    /// so the map, GlassRT and the wall fixtures need nothing; anything holding a world position of the player or of the shell
    /// (a one-shot tail parented to nothing, a camera layer with a world target) moves it by the delta. Never raised by a C22
    /// change, where the player does not move.
    /// </summary>
    public static event Action<Vector3> PlayerRebased;

    /// <summary>
    /// The far door of the shell the floor began in left its frame (the open; closing false) or started back into it (the shut;
    /// closing true), at its door point (FarDoorPoint: the middle of its line on the new floor's floor). C22's
    /// FrontRoomsStairwell.NorthDoorMoved (with its NorthDoorClosing) for the walkable stair: the same sound beats and haptics
    /// (交互设计 2026-11-10 rule 4). Never raised on a C22 floor.
    /// </summary>
    public static event Action<Vector3, bool> StairFarDoorMoved;

    /// <summary>That far door latched after its shut and is locked for good: C22's NorthDoorLatched for the walkable stair.</summary>
    public static event Action<Vector3> StairFarDoorLatched;

    /// <summary>At least this long from the EXIT's latch to the change (the latch Foley plays out; contract 60 section 3).</summary>
    const float StairLatchToChange = .3f;
    /// <summary>The re-entry blocker stands this far inside the EXIT door's line, into the stair.</summary>
    const float StairBlockerInset = .1f;
    /// <summary>How far along the walk down (PathProgress) also counts as arrival on the far landing, besides its trigger.</summary>
    const float StairFarArrivalProgress = .9f;
    /// <summary>CloseFarDoor with no FarDoorLatched after this long: the floor start goes on (a warning), so the Relay still arms.</summary>
    const float StairLatchTimeout = 4f;

    // The run plays the walkable stair (StairRunBegin, once at the run's start): the profile's switch with a shell factory set.
    bool stairRun;
    // This floor's EXIT shell (a walkable-stair floor) and the one this floor began in (after a walkable stair). Null otherwise.
    IFrontRoomsStairShell exitShell, startShell;
    // This floor began on a walkable stair (its start door N' faces south out of the shell).
    bool floorStartedOnStair;
    // The next floor's seed, searched at the seal (the tier cannot change inside the stair), for that floor and tier.
    FloorSeedPlan stairNextSeed;
    int stairNextSeedFloor = -1, stairNextSeedTier = -1;
    float stairSeedMs;
    // The far door on a floor that began on the stair: the far landing reached (floorClock then), the hold shown, CloseFarDoor
    // asked (floorClock then), the shell's FarDoorLatched seen.
    bool stairFarArrived, stairFarHeld, stairFarCloseAsked, stairFarLatched;
    float stairFarArrivedAt, stairFarCloseAt;
    // The change came due with the feet outside X1-X2 (logged once; the change waits for them).
    bool stairHoldLogged;
    // The re-entry blocker in the EXIT doorway (a child of the exit shell's root, so it moves and dies with the shell).
    GameObject stairBlocker;
    // The handlers on the start shell's FarDoorMoved and FarDoorLatched, kept to take them off again.
    Action stairLatchHandler, stairMovedHandler;
    IFrontRoomsStairShell stairLatchShell;
    // The shells' lamp levels as last set (SetLampLevel; -1 none yet): the exit shell's and the start shell's.
    float exitShellLamp = -1f, startShellLamp = -1f;
    // The shells' visibility as last set (IFrontRoomsStairShellVisible only): 0 unknown, 1 shown, 2 hidden.
    int exitShellShown, startShellShown;
    // The last change's move (the autopilot's row and the log).
    Vector3 stairRebaseDelta;

    /// <summary>The walkable stair is on for this run (the profile's switch, a shell factory): every EXIT is 1 x 2.</summary>
    public bool WalkableStairRun => stairRun;

    // S4 (游戏视觉's walk harness, 11-10): the 32 deg flights drop 2.0 m/s at Walk and 3.4 m/s at Run, so the grounded stick of
    // -1 m/s left the body airborne 0.22 / 0.50 s at the top of each flight and the camera hopped. In the stair (the feet in
    // X1-X2, or in the start shell after the change) the stick is -6 m/s (airborne 0.05 s); everywhere else it stays -1.
    const float StairStickSpeed = -6f;
    float StairGroundStick()
    {
        if (!stairRun || map == null || playerRoot == null) return -1f;
        var cell = map.CellOf(playerRoot.position);
        var inStair = (exitShell != null && map.InExitArea(cell)) || (startShell != null && floorStartedOnStair && map.StartIsStair && map.InStartArea(cell));
        return inStair ? StairStickSpeed : -1f;
    }

    /// <summary>The shell the current floor began in (after a walkable stair), null otherwise; its FarDoorPoint is the floor's start door.</summary>
    public IFrontRoomsStairShell StartStairShell => startShell;

    /// <summary>This floor's EXIT shell (a walkable-stair floor), null otherwise.</summary>
    public IFrontRoomsStairShell ExitStairShell => exitShell;

    // From the seal of a walkable-stair EXIT to the change: no move freeze there; EXAMINE waits instead.
    bool StairSwap => floorSwap != FloorSwap.None && exitShell != null && exitPlan.stair;

    /// <summary>
    /// The run's start (StartRunInPlace, before floor 1's PlanExit): the walkable stair is on when the profile asks for it (or
    /// the autopilot's -autopilotWalkableStair) and a shell factory is set. Read once: a factory set later in the run changes nothing.
    /// </summary>
    void StairRunBegin(FrontRoomsLevelProfile profile)
    {
        var asked = profile != null && profile.walkableStair;
#if UNITY_EDITOR
        if (autopilot && Array.IndexOf(Environment.GetCommandLineArgs(), "-autopilotWalkableStair") >= 0) asked = true;
#endif
        stairRun = asked && FrontRoomsStairShellHost.Available;
        floorStartedOnStair = false;
        stairNextSeedFloor = stairNextSeedTier = -1;
        exitShellShown = startShellShown = 0;
        if (asked && !stairRun) Log("STAIR · walkableStair is on but no shell factory is set (FrontRoomsStairShellHost.Create): every floor keeps C22's vestibule");
        else if (stairRun) Log("STAIR · the walkable stair is on: 1 x 2 EXIT areas (X1, X2), each next floor starts in them with its door on N, the change rebases the shell and the player");
    }

    /// <summary>
    /// The shell for this floor's walkable-stair EXIT (exitPlan.stair), from the factory, at the middle of X1's south line on the
    /// map's floor, facing +z; the numerals set. Null when the factory gives none (PlanExit then plans C22's vestibule).
    /// </summary>
    IFrontRoomsStairShell CreateStairShell()
    {
        var create = FrontRoomsStairShellHost.Create;
        if (create == null) return null;
        var n = exitPlan.frontCell;
        var cs = MapGrid.CellSize;
        // N's north line, centred: the EXIT door is always built centred on it (EffectiveEdge Door).
        var edge = map.transform.TransformPoint(new Vector3((n.x + .5f) * cs, 0f, (n.y + 1) * cs));
        var normal = map.CellCenter(exitPlan.exitCell) - map.CellCenter(n);
        normal.y = 0f;
        normal.Normalize();
        IFrontRoomsStairShell shell;
        try { shell = create(transform, edge, normal); }
        catch (Exception e) { Debug.LogException(e); return null; }
        if (shell == null || shell.Root == null) return null;
        try { shell.SetFloor(floor, FloorLabel(floor)); }
        catch (Exception e) { Debug.LogException(e); }
        exitShellShown = 0;
        exitShellLamp = -1f;
        StairShellLamp(shell, ref exitShellLamp, 1f);
        return shell;
    }

    /// <summary>
    /// The shell's lights (SetLampLevel), set where C22 drives its vestibule's LampLevel: at the shell's making, through the seal
    /// and the change (UpdateExitSeal's blackout in C22; the walk replaces it, contract 60 section 4, so the level stays 1) and in
    /// the floor start (UpdateFloorStart's lamp back in C22). The stair is a safe zone (Red 2026-10-22 22:47): no warn burst or
    /// strike hold reaches it, only a power state would go through here. A call that changes nothing is skipped.
    /// </summary>
    void StairShellLamp(IFrontRoomsStairShell shell, ref float applied, float level)
    {
        level = Mathf.Clamp01(level);
        if (shell == null || Mathf.Abs(applied - level) < 1e-4f) return;
        applied = level;
        try { shell.SetLampLevel(level); }
        catch (Exception e) { Debug.LogException(e); }
    }

    /// <summary>Floor 1's start-group EXIT on a walkable-stair run: the station's X is X1 when X2 fits as well (StairFromStartGroup); else C22's, logged.</summary>
    ExitPlan StairFromStation(CellRect startArea, ExitPlan plan)
    {
        var stair = FrontRoomsExitPlanner.StairFromStartGroup(map.Cache, startArea, plan, out var why);
        if (!stair.stair) Log("STAIR · floor 1's start-group EXIT X " + plan.exitCell + " cannot be the walkable stair's X1 (" + why + "): floor 1 keeps C22's vestibule");
        return stair;
    }

    /// <summary>The factory gave no shell for a planned walkable-stair EXIT: this floor, and the rest of the run, plan C22's vestibule.</summary>
    ExitPlan StairFallback(CellRect startArea, GridCoord startDoor)
    {
        Debug.LogWarning("[FrontRooms3D] STAIR · the shell factory gave no shell for X1 " + exitPlan.exitCell + " (floor " + floor + "): this floor and the rest of the run keep C22's vestibule");
        stairRun = false;
        if (floor == 1 && stationPlan != null) return stationPlan.exit;
        FrontRoomsExitPlanner.BandFor(floor, out var bandMin, out var bandMax);
        return FrontRoomsExitPlanner.ChooseExit(map.Cache, startArea, startDoor, floorSeed, bandMin, bandMax);
    }

    /// <summary>
    /// The seal and the change on a walkable-stair floor, each frame of play after relay.Tick (UpdateExitSeal hands over): C22's
    /// gate and SealExit, then no freeze and no blackout on desktop. From the seal: the re-entry blocker, the next floor's seed
    /// searched (SeedForStair). From the latch (OnStairExitSealed): at least StairLatchToChange, then the change as soon as the
    /// feet stand in X1 or X2 (a hard hold otherwise). WebGL takes the map's exit lamps to 0 over BlackoutSeconds from the latch,
    /// as C22 does (no forced shadows there).
    /// </summary>
    void UpdateStairSeal(float dt)
    {
        if (floorSwap == FloorSwap.None)
        {
            if (!ExitSealAllowed()) return;
            floorSwap = FloorSwap.Sealing;
            floorSwapClock = 0f;
            if (!map.SealExit())
            {
                floorSwap = FloorSwap.None;
                exitRefused = true;
                Debug.LogWarning("[FrontRooms3D] EXIT · the EXIT door at " + map.ExitCell + " cannot be sealed (broken); the floor keeps it open");
                return;
            }
            StairBlockerOn();
            StairPrecomputeSeed();
            stairHoldLogged = false;
            Event("exit", "sealing at " + ExitDepth(playerRoot.position).ToString("0.00", CultureInfo.InvariantCulture) + " m into the stair (next seed "
                + stairSeedMs.ToString("0.0", CultureInfo.InvariantCulture) + " ms)");
            return;
        }
        floorSwapClock += dt;
        if (floorSwap == FloorSwap.Sealing)
        {
            if (floorSwapClock > 3f && floorSwapClock - dt <= 3f) Debug.LogWarning("[FrontRooms3D] EXIT · the EXIT door has not latched 3 s after the seal");
            return;
        }
        // Where C22's blackout takes its vestibule's LampLevel to 0 (UpdateExitSeal): the walk keeps the stair lit, on every platform.
        StairShellLamp(exitShell, ref exitShellLamp, 1f);
#if UNITY_WEBGL && !UNITY_EDITOR
        map.ExitLamps = Mathf.Clamp01(1f - floorSwapClock / BlackoutSeconds);
#endif
        if (floorSwapClock < StairLatchToChange) return;
        if (!map.InExitArea(map.CellOf(playerRoot.position)))
        {
            if (!stairHoldLogged)
            {
                stairHoldLogged = true;
                Debug.LogWarning("[FrontRooms3D] STAIR · the change waits: the player is not in X1-X2 " + map.ExitRect + " (" + playerRoot.position + ")");
            }
            return;
        }
        ChangeFloor();
    }

    /// <summary>
    /// The map locked a walkable-stair EXIT at its latch (OnExitDoorSealed hands over): the shell's static shut leaf stands, the
    /// title's rooms go if they still stand, and the walk goes on. False when this floor's EXIT is C22's (nothing done).
    /// </summary>
    bool OnStairExitSealed()
    {
        if (exitShell == null || !exitPlan.stair) return false;
        try { exitShell.ShowExitLeafShut(true); }
        catch (Exception e) { Debug.LogException(e); }
        EndTitleRooms();
        floorSwap = FloorSwap.StairWalk;
        floorSwapClock = 0f;
        Event("exit", "sealed (the walk down goes on)");
        return true;
    }

    // From the seal: a box across the EXIT doorway 0.1 m inside its line (Ignore Raycast: aim and sight rays pass it, the body
    // does not), so nobody walks back into the closing leaf. A child of the shell's root: it moves with the shell at the change and
    // keeps the exit landing's doorway shut a floor up, and goes with the shell.
    void StairBlockerOn()
    {
        if (stairBlocker != null || exitShell == null || exitShell.Root == null) return;
        var forward = exitShell.ExitDoorForward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 1e-6f) return;
        forward.Normalize();
        stairBlocker = new GameObject("Stair re-entry blocker");
        stairBlocker.layer = 2;
        stairBlocker.transform.SetParent(exitShell.Root, true);
        stairBlocker.transform.SetPositionAndRotation(exitShell.ExitDoorPoint + forward * (StairBlockerInset + .025f) + Vector3.up * 1.1f, Quaternion.LookRotation(forward, Vector3.up));
        var box = stairBlocker.AddComponent<BoxCollider>();
        box.size = new Vector3(ModuleUnits.DoorWidth + .2f, 2.2f, .05f);
        Physics.SyncTransforms();
    }

    // At the seal: the next floor's seed (up to tens of ms; the change's frame is not hidden by a blackout any more).
    void StairPrecomputeSeed()
    {
        var profile = map.Profile;
        var watch = System.Diagnostics.Stopwatch.StartNew();
        stairNextSeed = FrontRoomsExitPlanner.SeedForStair(runSeed, floor + 1, map.ExitCell, profile.Generation(runSeed), profile.ModuleData(), tier, out _);
        stairNextSeedFloor = floor + 1;
        stairNextSeedTier = tier;
        stairSeedMs = (float)watch.Elapsed.TotalMilliseconds;
    }

    // ChangeFloor (the floor already moved on): the seed searched at the seal, or searched again if the tier changed since.
    FloorSeedPlan StairSeedFor(GridCoord x, FrontRoomsLevelProfile profile)
    {
        var plan = stairNextSeedFloor == floor && stairNextSeedTier == tier ? stairNextSeed
            : FrontRoomsExitPlanner.SeedForStair(runSeed, floor, x, profile.Generation(runSeed), profile.ModuleData(), tier, out _);
        stairNextSeedFloor = stairNextSeedTier = -1;
        return plan;
    }

    /// <summary>
    /// ChangeFloor's walkable-stair step (variant B), right after NextFloor(X1-X2, N): the shell two floors back goes (or C22's
    /// stairwell); this floor's EXIT shell becomes the start shell; the shell's root and the player move up FloorToFloor, the
    /// CharacterController off round the move and every collider synced (auto-sync is off); PlayerRebased; the shell's Rebased,
    /// its numerals and the far wall's paper (N''s, from the new map). The far door's floor start begins (UpdateStairFloorStart).
    /// </summary>
    void StairJoinNextFloor(GridCoord door)
    {
        if (startStairwell != null)
        {
            Destroy(startStairwell.gameObject);
            startStairwell = null;
        }
        DestroyStartShell();
        startShell = exitShell;
        exitShell = null;
        stairBlocker = null;
        startShellShown = exitShellShown;
        exitShellShown = 0;
        startShellLamp = exitShellLamp;
        exitShellLamp = -1f;
        var delta = Vector3.up * Mathf.Max(0f, startShell.FloorToFloor);
        startShell.Root.position += delta;
        playerBody.enabled = false;
        playerRoot.position += delta;
        playerBody.enabled = true;
        Physics.SyncTransforms();
        stairRebaseDelta = delta;
        var handlers = PlayerRebased;
        if (handlers != null)
            foreach (var d in handlers.GetInvocationList())
            {
                try { ((Action<Vector3>)d)(delta); }
                catch (Exception e) { Debug.LogException(e); }
            }
        try
        {
            startShell.Rebased(delta);
            startShell.SetFloor(floor, FloorLabel(floor));
            // N''s ceiling for the far wall's grime band (Standard 2.9 m unless SeedForStair fell back to another N').
            startShell.FarCeiling = MapGrid.CeilingHeight(map.ZoneOf(door).height);
            startShell.SetFarWallPaper(map.WallPaperOf(door));
        }
        catch (Exception e) { Debug.LogException(e); }
        stairFarArrived = stairFarHeld = stairFarCloseAsked = stairFarLatched = false;
        stairFarArrivedAt = stairFarCloseAt = 0f;
        stairHoldLogged = false;
        WatchFarLatch(startShell);
    }

    // The start shell's far door, watched: its latch ends the floor start (as C22's NorthDoorLocked), and both its events go
    // out as StairFarDoorMoved / StairFarDoorLatched (C22's NorthDoorMoved / NorthDoorLatched: the sound beats, the haptics).
    void WatchFarLatch(IFrontRoomsStairShell shell)
    {
        UnwatchFarLatch();
        if (shell == null) return;
        stairLatchShell = shell;
        stairLatchHandler = () =>
        {
            stairFarLatched = true;
            RaiseStairFarDoor(shell, false, true);
        };
        // The open's FarDoorMoved comes before any CloseFarDoor, the shut's from inside it (stairFarCloseAsked is set first).
        stairMovedHandler = () => RaiseStairFarDoor(shell, stairFarCloseAsked, false);
        shell.FarDoorLatched += stairLatchHandler;
        shell.FarDoorMoved += stairMovedHandler;
    }

    void UnwatchFarLatch()
    {
        if (stairLatchShell != null && stairLatchHandler != null) stairLatchShell.FarDoorLatched -= stairLatchHandler;
        if (stairLatchShell != null && stairMovedHandler != null) stairLatchShell.FarDoorMoved -= stairMovedHandler;
        stairLatchShell = null;
        stairLatchHandler = stairMovedHandler = null;
    }

    // One far-door event to the listeners, each on its own (a listener's exception never reaches the shell's door tick).
    static void RaiseStairFarDoor(IFrontRoomsStairShell shell, bool closing, bool latched)
    {
        Vector3 point;
        try { point = shell.FarDoorPoint; }
        catch (Exception e) { Debug.LogException(e); return; }
        if (latched)
        {
            var handlers = StairFarDoorLatched;
            if (handlers == null) return;
            foreach (var d in handlers.GetInvocationList())
            {
                try { ((Action<Vector3>)d)(point); }
                catch (Exception e) { Debug.LogException(e); }
            }
            return;
        }
        var moved = StairFarDoorMoved;
        if (moved == null) return;
        foreach (var d in moved.GetInvocationList())
        {
            try { ((Action<Vector3, bool>)d)(point, closing); }
            catch (Exception e) { Debug.LogException(e); }
        }
    }

    // The shell the floor began in goes (two floors back, or behind its locked far door out of sight).
    void DestroyStartShell()
    {
        if (startShell == null) return;
        UnwatchFarLatch();
        var root = startShell.Root;
        startShell = null;
        startShellShown = 0;
        startShellLamp = -1f;
        if (root != null) Destroy(root.gameObject);
    }

    /// <summary>
    /// The floor start after a walkable stair (UpdateFloorStart hands over): UpdateFloorStart's steps with the far door, which
    /// opens by itself (交互设计 2026-11-10; Red's door rule of 10-31: no USE, no prompt). Arrival is the far landing's trigger
    /// (OnFarLanding: the first step off flight 2, about 2 m from the far hinge, so outside the 0.98 m leaf's sweep) or
    /// StairFarArrivalProgress of the walk down; then the far door starts its open at once if the map round it is built and
    /// furnished (ReadyAround), else the hold pose (HoldFarDoor: lever down, 1-2 mm, silent, no prompt, no line) and the open
    /// StartDoorHoldLimit (3 s) after arrival at the latest (floorOpenedLate). The leaf yields at a body on its own (the shell:
    /// it never steps into the CharacterController, so it neither shoves the capsule nor clips the camera). The lamps past the
    /// door line rise with the swing, the ones behind it once the player is out; the far door shuts for good once the player is
    /// out of X1-X2 and StartDoorShutDistance (flat) from it, and its latch (FarDoorLatched, or StairLatchTimeout after
    /// CloseFarDoor) arms the Relay as C22's north door does; when the map has dropped every chunk round X1-X2, the shell goes
    /// and the map takes the cells back. The far door's moves and latch go out as StairFarDoorMoved / StairFarDoorLatched.
    /// </summary>
    void UpdateStairFloorStart(float dt)
    {
        floorClock += dt;
        var feet = playerRoot.position;
        var inside = map.InStartArea(map.CellOf(feet));
        // Where C22 brings its vestibule's LampLevel back after the blackout: the stair's level back to 1 (it stays 1 through the walk).
        StairShellLamp(startShell, ref startShellLamp, startShellLamp < 0f ? 1f : Mathf.MoveTowards(startShellLamp, 1f, dt / BlackoutSeconds));
        if (!inside && !floorLeftStart)
        {
            floorLeftStart = true;
            map.StreamFocus = null;
            Event("stairwell", "left");
        }
        if (floorLeftStart) map.StartLampsSouth = Mathf.MoveTowards(map.StartLampsSouth, 1f, dt / StreamFadeSeconds);
        if (!floorDoorOpened)
        {
            if (!stairFarArrived && (startShell.OnFarLanding(feet) || startShell.PathProgress(feet) >= StairFarArrivalProgress))
            {
                stairFarArrived = true;
                stairFarArrivedAt = floorClock;
                Event("stairwell", "far landing");
            }
            if (!stairFarArrived) return;
            var ready = map.ReadyAround(map.BuildRadius);
            var waited = floorClock - stairFarArrivedAt;
            if (!ready && waited <= StartDoorHoldLimit)
            {
                if (!stairFarHeld)
                {
                    stairFarHeld = true;
                    try { startShell.HoldFarDoor(); }
                    catch (Exception e) { Debug.LogException(e); }
                    Event("start door", "held");
                }
                return;
            }
            if (!ready) Log("FLOOR " + floor + " · map round the far door not ready " + StartDoorHoldLimit + " s after the far landing; opening anyway");
            try { startShell.OpenFarDoor(); }
            catch (Exception e) { Debug.LogException(e); }
            floorDoorOpened = true;
            floorOpenedLate = !ready;
            floorHeldFor = waited;
            Event("start door", "opens");
            return;
        }
        if (!floorStartShut)
        {
            map.StartLampsNorth = Mathf.MoveTowards(map.StartLampsNorth, 1f, dt / StartLampsRiseSeconds);
            if (!stairFarCloseAsked && !inside && Vector2.Distance(Flat(feet), Flat(startShell.FarDoorPoint)) >= StartDoorShutDistance)
            {
                stairFarCloseAsked = true;
                stairFarCloseAt = floorClock;
                try { startShell.CloseFarDoor(); }
                catch (Exception e) { Debug.LogException(e); }
            }
            if (!stairFarLatched && stairFarCloseAsked && floorClock - stairFarCloseAt > StairLatchTimeout)
            {
                stairFarLatched = true;
                Debug.LogWarning("[FrontRooms3D] STAIR · no FarDoorLatched " + StairLatchTimeout + " s after CloseFarDoor; the floor start goes on");
            }
            if (!stairFarLatched) return;
            floorStartShut = true;
            map.StartLampsNorth = 1f;
            Event("start door", "shut");
            return;
        }
        if (map.StartAreaBuilt) return;
        DestroyStartShell();
        map.ClearStartArea();
        Event("stairwell", "removed");
    }

    // UpdateStairwells for the shells that implement IFrontRoomsStairShellVisible: the EXIT's while X1's chunk is built or the
    // seal is under way; the start's while X1's chunk is built or the player is in X1-X2. Colliders are never touched.
    void UpdateStairShells()
    {
        if (map == null) return;
        if (exitShell is IFrontRoomsStairShellVisible exitVisible)
        {
            var show = map.IsChunkBuiltAt(exitPlan.exitCell) || floorSwap != FloorSwap.None;
            var state = show ? 1 : 2;
            if (state != exitShellShown)
            {
                exitShellShown = state;
                try { exitVisible.SetVisible(show); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }
        if (startShell is IFrontRoomsStairShellVisible startVisible)
        {
            var x1 = new GridCoord(startDoorCell.x, startDoorCell.y + 1);
            var show = map.IsChunkBuiltAt(x1) || (playerRoot != null && map.InStartArea(map.CellOf(playerRoot.position)));
            var state = show ? 1 : 2;
            if (state != startShellShown)
            {
                startShellShown = state;
                try { startVisible.SetVisible(show); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }
    }

#if UNITY_EDITOR
    // ---------- The EXIT scenario on a walkable stair (-autopilotExit with -autopilotWalkableStair) ----------

    /// <summary>The walk down and the hold on the far landing may take this much longer than C22's hold in place.</summary>
    const float AutoStairWalkSeconds = 8f;
    /// <summary>After the far door opens, the walker waits this long for the leaf to swing into the stair before going through.</summary>
    const float AutoStairLeafSeconds = 1f;
    // The waypoint the walk down is heading for, and the shell it belongs to (a new EXIT shell starts again at 0).
    int autoStairWaypoint;
    IFrontRoomsStairShell autoStairShell;

    /// <summary>
    /// AutoExitSteer's walkable-stair steps: from ExitEnter's line-up in N, along the shell's PathWaypoints (through the seal and
    /// the change, which no longer stop the player) to the last one, on the far door's latch side; then, once the far door has
    /// opened and AutoStairLeafSeconds passed, out through it into N' and on south past StartDoorShutDistance. False for a step
    /// it leaves to the C22 walk (the route to N, the door's opening, the line-up, the unlock).
    /// </summary>
    bool AutoStairSteer(float dt, out Vector2 local)
    {
        local = Vector2.zero;
        IFrontRoomsStairShell shell = null;
        if (autoExitStep == ExitEnter && autoExitLined && exitShell != null && exitPlan.stair) shell = exitShell;
        else if (autoExitStep == ExitSeal && exitShell != null && exitPlan.stair) shell = exitShell;
        else if (autoExitStep == ExitHold && startShell != null) shell = startShell;
        else if (autoExitStep == ExitOut && startShell != null && floorStartedOnStair) return AutoStairOut(dt, out local);
        if (shell == null) return false;
        if (!ReferenceEquals(shell, autoStairShell))
        {
            if (autoExitStep == ExitEnter) autoStairWaypoint = 0;
            autoStairShell = shell;
        }
        var points = shell.PathWaypoints;
        if (points == null || points.Length == 0) return false;
        var feet = playerRoot.position;
        while (autoStairWaypoint < points.Length - 1 && Flat(points[autoStairWaypoint] - feet).magnitude < .35f) autoStairWaypoint++;
        var index = Mathf.Min(autoStairWaypoint, points.Length - 1);
        var last = index == points.Length - 1;
        if (last && Flat(points[index] - feet).magnitude < .12f) return true;
        AutoSteerAt(dt, points[index], last, out local);
        return true;
    }

    // Out through the far door: its middle and on into N', then (C22's walk out turned round) south across N' into the cell
    // beyond it, to its middle (StartDoorShutDistance is 4 m).
    bool AutoStairOut(float dt, out Vector2 local)
    {
        local = Vector2.zero;
        if (!floorDoorOpened || autoExitClock < AutoStairLeafSeconds) return true;
        var feet = playerRoot.position;
        var here = map.CellOf(feet);
        var south = -AutoExitNorthOf(feet);
        var door = startDoorCell;
        var ahead = new GridCoord(door.x, door.y - 1);
        if (map.InStartArea(here))
        {
            AutoSteerAt(dt, startShell.FarDoorPoint + south * .6f, false, out local);
            return true;
        }
        var reach = map.DoorBetween(door, ahead) != null ? 0f : 1.2f;
        if (here == door && Vector3.Dot(feet - map.CellCenter(door), south) < reach - .2f)
        {
            AutoSteerAt(dt, map.CellCenter(door) + south * reach, false, out local);
            return true;
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
            return true;
        }
        if (here == ahead) AutoSteerAt(dt, map.CellCenter(ahead) + south * .5f, true, out local);
        return true;
    }

    /// <summary>
    /// AutopilotFloorStarted's check for a walkable-stair change, in its own frame: one floor down, the map root where it was, the old
    /// X1-X2 the start area (its door N' south of X1) with the player in it, the shell moved up by its FloorToFloor and the player by
    /// the same delta, the Relay parked, the next EXIT planned (and its shell made, when it is a stair too). Then ExitSeal goes to
    /// ExitHold, where the walk down goes on.
    /// </summary>
    void AutoStairFloorStarted(bool rootKept, bool parked, GridCoord feetCell)
    {
        var shell = startShell;
        var moved = shell != null && Mathf.Abs(stairRebaseDelta.y - shell.FloorToFloor) < 1e-4f && stairRebaseDelta.x == 0f && stairRebaseDelta.z == 0f;
        var area = map.HasStartArea && map.StartIsStair && map.InStartArea(autoExitX) && map.InStartArea(new GridCoord(autoExitX.x, autoExitX.y + 1))
            && startDoorCell == new GridCoord(autoExitX.x, autoExitX.y - 1);
        var next = map.HasExitArea && map.ExitCell != autoExitX && (exitPlan.stair ? exitShell != null && map.ExitIsStair : exitStairwell != null);
        var ok = autoExitStep == ExitSeal && floor == autoExitFloorFrom + 1 && rootKept && area && map.InStartArea(feetCell) && parked && moved && next && map.StreamFocus.HasValue;
        AutoExitCheck(ok, "floor " + autoExitFloorFrom + " -> " + floor + " (" + FloorLabel(floor) + ", seed " + floorSeed + ", walkable stair): changed in " + AutoF(floorSwapMs, "0.0")
            + " ms; root " + (rootKept ? "kept" : "MOVED") + "; old X1-X2 " + autoExitX + " is the start area " + (area ? "with its door on N'" : "NOT as asked")
            + (map.InStartArea(feetCell) ? ", the player in it" : ", the player OUT of it (" + feetCell + ")") + "; shell and player moved " + AutoF(stairRebaseDelta.y) + " m (FloorToFloor "
            + (shell != null ? AutoF(shell.FloorToFloor) : "none") + "); Relay " + (relay != null ? relay.State.ToString() : "none") + (parked ? ", parked" : ", NOT parked")
            + "; next EXIT " + (map.HasExitArea ? map.ExitCell + (map.ExitIsStair ? " (stair)" : " (C22)") : "none"));
        if (autoExitStep == ExitSeal) AutoExitNext(ExitHold);
        else AutoExitFail("floor " + floor + ": the floor changed during the step '" + AutoExitStepNames[autoExitStep] + "'");
    }
#endif
}
