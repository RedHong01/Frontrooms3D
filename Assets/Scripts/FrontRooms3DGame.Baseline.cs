#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using FrontRooms.Map;
using UnityEngine;

/// <summary>
/// Step 0 Relay pursuit baseline harness. Editor-only and deliberately separate from gameplay/rendering code.
/// It drives the existing player-facing controls and writes seeded reports under Verification/relay-baseline.
/// </summary>
public sealed partial class FrontRooms3DGame
{
    bool AutoWalkRoute(float dt, GridCoord here, out Vector2 local)
    {
        local = Vector2.zero;
        if (autoRouteIndex >= autoRoute.Count) return false;
        var next = autoRoute[autoRouteIndex];
        if (here == next)
        {
            autoRouteIndex++;
            return false;
        }
        // Never read the map past what is built: an unbuilt chunk would be generated for the asking.
        if (Mathf.Abs(here.x - next.x) + Mathf.Abs(here.y - next.y) != 1 || !map.IsBuilt(here) || !map.IsBuilt(next))
        {
            autoRouteIndex = autoRoute.Count;
            return false;
        }
        var passage = map.PassageBetween(here, next);
        if (passage == FrontRoomsMapWorld.Passage.ClosedDoor)
        {
            if (map.TryOpenDoor(here, next)) autoDoorsOpened++;
            // The leaf is on its way (after a pull, the game has stepped the walker clear): wait for it, facing the door.
            var opening = map.DoorBetween(here, next);
            if (opening != null && opening.open)
            {
                var face = map.CrossingPoint(here, next) - playerRoot.position;
                face.y = 0f;
                yaw = Mathf.MoveTowardsAngle(yaw, Mathf.Atan2(face.x, face.z) * Mathf.Rad2Deg, 300f * dt);
                return false;
            }
        }
        else if (passage != FrontRoomsMapWorld.Passage.Open)
        {
            autoRouteIndex = autoRoute.Count;
            return false;
        }
        var target = map.CrossingPoint(here, next) + (map.CellCenter(next) - map.CellCenter(here)).normalized * .45f;
        AutoSteerAt(dt, target, false, out local);
        return true;
    }

    /// <summary>Turn toward a point and walk at it, forward only once roughly facing it; with <paramref name="arrive"/> the last step lands on it (at walking pace).</summary>
    void AutoSteerAt(float dt, Vector3 point, bool arrive, out Vector2 local)
    {
        var to = point - playerRoot.position;
        to.y = 0f;
        var desiredYaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
        yaw = Mathf.MoveTowardsAngle(yaw, desiredYaw, 300f * dt);
        pitch = Mathf.MoveTowards(pitch, 0f, 60f * dt);
        var forward = Mathf.Abs(Mathf.DeltaAngle(yaw, desiredYaw)) < 45f ? 1f : .15f;
        if (arrive) forward = Mathf.Min(forward, to.magnitude / Mathf.Max(Walk * dt, 1e-4f));
        local = new Vector2(0f, forward);
    }

    // The planner's scratch: each cell's walking depth from the start, the cell it was reached from, and the cells in the order reached.
    readonly Dictionary<GridCoord, int> autoDepth = new Dictionary<GridCoord, int>();
    readonly Dictionary<GridCoord, GridCoord> autoCameFrom = new Dictionary<GridCoord, GridCoord>();
    readonly List<GridCoord> autoReached = new List<GridCoord>();
    readonly Queue<GridCoord> autoQueue = new Queue<GridCoord>();

    /// <summary>
    /// Breadth-first over built cells through open edges and doors (shut ones are opened on the walk), never
    /// walls or glass, to <paramref name="maxDepth"/> steps; <paramref name="keep"/> (null: any) limits the cells.
    /// </summary>
    void AutoBfs(GridCoord from, int maxDepth, Func<GridCoord, bool> keep)
    {
        autoDepth.Clear();
        autoCameFrom.Clear();
        autoReached.Clear();
        autoQueue.Clear();
        autoDepth[from] = 0;
        autoReached.Add(from);
        autoQueue.Enqueue(from);
        while (autoQueue.Count > 0)
        {
            var cell = autoQueue.Dequeue();
            var d = autoDepth[cell];
            if (d >= maxDepth) continue;
            foreach (var step in AutoSteps)
            {
                var n = cell + step;
                if (autoDepth.ContainsKey(n) || !map.IsBuilt(n) || (keep != null && !keep(n))) continue;
                var p = map.PassageBetween(cell, n);
                if (p == FrontRoomsMapWorld.Passage.Wall || p == FrontRoomsMapWorld.Passage.Glass) continue;
                autoDepth[n] = d + 1;
                autoCameFrom[n] = cell;
                autoReached.Add(n);
                autoQueue.Enqueue(n);
            }
        }
    }

    /// <summary>Make autoRoute the last search's way from <paramref name="from"/> to <paramref name="goal"/> (empty when it did not reach it).</summary>
    void AutoRouteTo(GridCoord from, GridCoord goal)
    {
        autoRoute.Clear();
        autoRouteIndex = 0;
        if (!autoDepth.ContainsKey(goal)) return;
        for (var c = goal; c != from; c = autoCameFrom[c]) autoRoute.Add(c);
        autoRoute.Reverse();
        autoRoutes++;
    }

    // ---------- Step 0 baseline (-autopilotBot <mode>): today's Relay, measured by seeded bots ----------
    // RELAY_PURSUIT_REDESIGN.md v2 §13 step 0 and §14, before anything of v2 lands. The autopilot plays as one
    // of the bots below for -autopilotMinutes of game time (default 4, the staller 5), with the run's tier forced
    // to -autopilotTier at the start (the rise rule still runs from there), on a fixed 1/60 s step, so a seed, a
    // mode and a tier replay the same run under any machine load. Only a catch ends a run sooner. It writes
    // Verification/relay-baseline/<seed>-<mode>-T<n>.json. Nothing here changes the game: the bots press only
    // what a player could (the walk, Shift, E through EDown/EHeld, doors through TryOpenDoor and Use as the
    // autopilot always has), and the Relay is only watched (its public state, its events, the map's API).
    //   quiet        walks, never sprints, never touches a window, opens only the doors its route needs;
    //   noisy        sprints on every straight, breaks every window it meets (from 0.9 m, inside the pane
    //                rule's 1.2 m), shuts each door right after passing it;
    //   evader03/06  walks; 0.3 / 0.6 s after the Relay starts a chase it turns away and sprints for the
    //                nearest cell out of its sight, shutting doors it passes, then walks away once unseen;
    //   staller      wanders inside the zone it first steps into, the whole run;
    //   shiftholder  walks its routes with Shift always held (the winded rule: sprint noise only while it sprints);
    //   doorspammer  stands at a door near the start and opens or shuts it every 0.8 s;
    //   edgerunner   walks north (the way the start door faces) as far as it can: the unbuilt-cell relay (A7b);
    //   closedzone   shuts every door of its zone that stands open, then sprints round inside the zone.
    enum AutoBot { None, Quiet, Noisy, Evader03, Evader06, Staller, ShiftHolder, DoorSpammer, EdgeRunner, ClosedZone }
    static readonly string[] AutoBotNames = { "none", "quiet", "noisy", "evader03", "evader06", "staller", "shiftholder", "doorspammer", "edgerunner", "closedzone" };
    AutoBot autoBot;
    bool autoBaseline;
    string autoBotUnknown;
    int autoBotTier = 1;
    float autoBotSeconds;

    // The fixed step and the frame clock: game time no longer follows how fast frames come, and frames come as
    // fast as the machine makes them, so the Stopwatch from one frame to the next is that frame's real cost.
    float autoPrevCaptureDelta;
    int autoPrevTargetFps;
    bool autoBaseTimeSet;
    readonly System.Diagnostics.Stopwatch autoBaseFrameWatch = new System.Diagnostics.Stopwatch(), autoBaseWall = new System.Diagnostics.Stopwatch();
    readonly List<float> autoBaseFrameMs = new List<float>();
    string autoBaseLoadStart;

    // The start hold: while the player is in the stream rooms the map furnishes on a wall-clock budget (8 ms a
    // frame), so the frame the stream door opens depends on machine load. The bot waits short of the door until
    // the map round it is built and furnished, the door has swung open and AutoStartHoldSeconds of play have
    // passed; from that fixed frame on, the run is replayed exactly.
    const float AutoStartHoldSeconds = 8f, AutoStartDoorSwing = 2f, AutoStartStandOff = 2.6f;
    float autoBotLeftAt = -1f;

    // Noise causes as the baseline counts them (an index into its counters); hunts add the chase it lost and "other".
    const int AutoCauseSprint = 0, AutoCauseDoor = 1, AutoCauseGlass = 2, AutoCauseOther = 3, AutoCauseChaseLost = 4, AutoCauseUnknown = 5;
    static readonly string[] AutoCauseNames = { "sprint", "door", "glass", "otherNoise", "chaseLost", "other" };

    // What the baseline watches. Clocks are game seconds from Space (autoPlayClock) unless said "after release".
    HunterState autoBaseState, autoBreakFrom, autoPreState;
    float autoBaseReleaseAt = -1f, autoFirstHunt = -1f, autoFirstChase = -1f, autoReleaseStraight = -1f, autoReleaseWalk = -1f;
    readonly float[] autoStateSeconds = new float[16];
    readonly int[] autoNoiseEmitted = new int[4], autoNoiseHeard = new int[4], autoNoiseDeaf = new int[4], autoNoiseOutOfRange = new int[4], autoHuntRetargets = new int[4];
    readonly int[] autoHuntsBy = new int[6], autoBreakAttempts = new int[16], autoDoorsBrokenFrom = new int[16];
    int autoNoiseInFlight = -1, autoLastHeardFrame = int.MinValue / 2, autoLastHeardCause, autoLastNoiseCause, autoWanderEncounters, autoCatches, autoChaseOpen = -1;
    float autoLastNoiseAt = -1e9f;
    readonly List<BaselineChase> autoChaseLog = new List<BaselineChase>();
    readonly List<BaselineRelay> autoRelayLog = new List<BaselineRelay>();
    // The Relay as it stood before this frame's Tick: a relay in that Tick is the unbuilt-cell one (A7b) when its cell was not built.
    Vector3 autoPrePosition;
    bool autoPreBuilt;

    // The hypothetical warning ladder (v2 §7, §9.1), sampled every 0.25 s of game time.
    const float AutoSampleSeconds = .25f, AutoFieldCap = 60f, AutoWalkCap = 150f;
    BaselinePathField autoField, autoWalkField;
    float autoSampleClock, autoS1Since, autoS2Since, autoFieldMsMax;
    int autoSamples, autoDormantSamples, autoOnMapSamples, autoFUnreached, autoFieldBuilds;
    double autoFieldMsSum, autoFieldVisitedSum;
    readonly int[] autoBandF = new int[4], autoBandW = new int[4], autoBandStraight = new int[4], autoStageSamples = new int[3];
    bool autoS1, autoS2;

    // The bot's own counters.
    int autoBotSprintBursts, autoBotSprintNoiseNotSprinting, autoBotDoorsShut, autoBotDoorToggles, autoBotWindowsTried, autoBotWindowsBroken;
    int autoBotEvasions, autoBotEvadeOk, autoBotEvadeCaught, autoBotZoneDoors = -1, autoBotZoneDoorsOpen = -1, autoEdgeStalls;
    float autoBotSprintSeconds, autoBotWindedSeconds;
    bool autoBotWasSprinting;
    readonly List<string> autoBotNotes = new List<string>();

    [Serializable]
    sealed class BaselineNoiseKinds { public int sprint, door, glass, otherNoise; }

    [Serializable]
    sealed class BaselineHuntCauses { public int sprint, door, glass, otherNoise, chaseLost, other; }

    [Serializable]
    sealed class BaselineCallCauses { public int sprint, door, glass, otherNoise, wanderEncounter, total; public float perMinute; }

    [Serializable]
    sealed class BaselineStateTime { public string state; public float seconds, share; }

    [Serializable]
    sealed class BaselineStateCount { public string state; public int count; }

    [Serializable]
    sealed class BaselineChase
    {
        // Seconds after release; the state it came from; the player's last noise in the 3 s before ("none": a wander encounter).
        public float startAt, seconds, straightAtStart;
        public string from, noiseBefore, end;
        public bool evaded;
    }

    [Serializable]
    sealed class BaselineRelay
    {
        // Seconds after release; A7a (the 45 s timed leash) or A7b (its cell stopped being built); metres to the player
        // before (where it was) and after (where it appeared); walking -1 where the field could not reach (unbuilt, or over 150 m).
        public float at;
        public string kind, stateBefore, entry;
        public float straightBefore, walkBefore, straightAfter, walkAfter;
    }

    [Serializable]
    sealed class BaselineBands { public float le18, le30, from30to36, gt36; }

    [Serializable]
    sealed class BaselineBot
    {
        public float distanceWalked;
        public int cellsVisited, zonesVisited, routes;
        public int sprintBursts;
        public float sprintSeconds, windedSeconds;
        public int sprintNoises, sprintNoisesNotSprinting;
        public int doorsOpened, doorsShut, doorToggles;
        public int windowsTried, windowsBroken;
        public int evasionsAttempted, evasionsSucceeded, evasionsCaught;
        public int zoneDoors = -1, zoneDoorsOpenAtStart = -1;
        public int edgeStalls;
        public List<string> notes = new List<string>();
    }

    [Serializable]
    sealed class BaselinePerf
    {
        public string frameClock = "real-time Stopwatch from frame to frame (from Space), frames unthrottled (targetFrameRate -1), game time on captureDeltaTime 1/60";
        public int frames;
        public float meanFrameMs, p50FrameMs, p95FrameMs, p99FrameMs, maxFrameMs;
        public string loadAverageStart, loadAverageEnd;
        public float wallSeconds;
        public float relayTickMaxMs;
        public int pathFieldBuilds;
        public float pathFieldMeanMs, pathFieldMaxMs, pathFieldMeanVisited;
        // Load-dependent moments of the start (the bot holds until they are past, so the run itself does not depend on them).
        public float startDoorOpenedAt, mapSettledAt;
    }

    [Serializable]
    sealed class BaselineReport
    {
        public string verdict;
        public string mode;
        public int seed, tier;
        public float minutes, playSeconds;
        public string endedBy;
        public bool caught;
        // The bot left the start rooms on its fixed frame (AutoStartHoldSeconds): false means this run's start depended on load.
        public float startHoldEndedAt;
        public bool startOnFixedFrame;
        public float releasedAt = -1f, releaseStraightMetres = -1f, releaseWalkMetres = -1f;
        public float secondsAfterRelease;
        public List<BaselineStateTime> stateShares = new List<BaselineStateTime>();
        public float firstHuntAfterRelease = -1f, firstChaseAfterRelease = -1f;
        public int hunts;
        public float huntsPerMinute;
        public BaselineHuntCauses huntsByCause = new BaselineHuntCauses();
        public BaselineNoiseKinds huntRetargets = new BaselineNoiseKinds();
        public BaselineCallCauses callsByCause = new BaselineCallCauses();
        public int chases;
        public float chasesPerMinute;
        public int chasesLost, chasesRelayed, chasesCaught, chasesOpenAtEnd, wanderEncounters;
        public float chaseMeanSeconds, chaseMaxSeconds;
        public List<BaselineChase> chaseLog = new List<BaselineChase>();
        public int catches;
        public BaselineNoiseKinds noiseEmitted = new BaselineNoiseKinds(), noiseHeard = new BaselineNoiseKinds(), noiseOutOfRange = new BaselineNoiseKinds(), noiseIgnoredByState = new BaselineNoiseKinds();
        public int relaysAfterRelease, relaysA7a, relaysA7b;
        public List<BaselineRelay> relayLog = new List<BaselineRelay>();
        public int doorsBroken;
        public List<BaselineStateCount> doorsBrokenByState = new List<BaselineStateCount>(), breakAttemptsByState = new List<BaselineStateCount>();
        public int glassBroken;
        public int tierFinal;
        public List<string> tierLog = new List<string>();
        // The ladder: F (octile path field, capped 60 m), W = min(F, 3 × straight 3D) and the straight line, as shares
        // of the time the Relay was on the map; the share it was not (Dormant); W's stages with §9.1's hysteresis and holds.
        public float sampleSeconds = AutoSampleSeconds, fieldCapMetres = AutoFieldCap;
        public int samples, onMapSamples;
        public float dormantShare, fUnreachedShare;
        public BaselineBands fBands = new BaselineBands(), wBands = new BaselineBands(), straightBands = new BaselineBands();
        public float wStage0, wStage1, wStage2, wStage0OfRun;
        public BaselineBot bot = new BaselineBot();
        public int errors;
        public List<string> errorLog = new List<string>();
        // FNV-1a of everything above: two runs that played the same have the same hash.
        public string behaviourHash;
        public BaselinePerf perf = new BaselinePerf();
    }

    void AutoBaseStart(string[] args)
    {
        string mode = null;
        var minutes = 0f;
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "-autopilotBot") mode = args[i + 1].Trim().ToLowerInvariant();
            else if (args[i] == "-autopilotTier" && int.TryParse(args[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var t)) autoBotTier = Mathf.Max(1, t);
            else if (args[i] == "-autopilotMinutes" && float.TryParse(args[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out var m)) minutes = m;
        }
        if (mode == null) return;
        var index = Array.IndexOf(AutoBotNames, mode);
        if (index <= 0)
        {
            autoBotUnknown = mode;
            return;
        }
        autoBot = (AutoBot)index;
        autoBaseline = true;
        autoBotSeconds = 60f * (minutes > 0f ? minutes : autoBot == AutoBot.Staller ? 5f : 4f);
        autoPrevCaptureDelta = Time.captureDeltaTime;
        autoPrevTargetFps = Application.targetFrameRate;
        Time.captureDeltaTime = 1f / 60f;
        Application.targetFrameRate = -1;
        autoBaseTimeSet = true;
        autoField = new BaselinePathField(AutoFieldCap);
        autoWalkField = new BaselinePathField(AutoWalkCap);
        autoBaseLoadStart = AutoLoadAverage();
        autoBaseWall.Start();
        autoBaseFrameWatch.Start();
    }

    /// <summary>Hand the editor back its own frame step and frame-rate cap (once).</summary>
    void AutoBaseRestoreTime()
    {
        if (!autoBaseTimeSet) return;
        autoBaseTimeSet = false;
        Time.captureDeltaTime = autoPrevCaptureDelta;
        Application.targetFrameRate = autoPrevTargetFps;
    }

    /// <summary>macOS's 1, 5 and 15 minute load averages, to read the frame times against.</summary>
    static string AutoLoadAverage()
    {
        try
        {
            var info = new System.Diagnostics.ProcessStartInfo("/usr/sbin/sysctl", "-n vm.loadavg") { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
            using (var process = System.Diagnostics.Process.Start(info))
            {
                var text = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit(2000);
                return text;
            }
        }
        catch (Exception e)
        {
            return "unavailable (" + e.GetType().Name + ")";
        }
    }

    /// <summary>Each frame: the real time since the frame before, from the frame Space is pressed on.</summary>
    void AutoBaseFrame()
    {
        var ms = (float)autoBaseFrameWatch.Elapsed.TotalMilliseconds;
        autoBaseFrameWatch.Restart();
        if (mapPlay) autoBaseFrameMs.Add(ms);
    }

    /// <summary>The start hold (see AutoStartHoldSeconds): true from the frame the bot may walk out of the stream rooms.</summary>
    bool AutoBotMayLeave()
    {
        if (autoBotLeftAt >= 0f) return true;
        if (autoPlayClock < AutoStartHoldSeconds || !map.Settled || autoStartDoorAt < 0f || autoPlayClock - autoStartDoorAt < AutoStartDoorSwing) return false;
        autoBotLeftAt = autoPlayClock;
        // The route picks restart from the run seed on this fixed frame.
        UnityEngine.Random.InitState(runSeed);
        return true;
    }

    /// <summary>The run has started (StartRunInPlace): the baseline forces its tier and starts watching the Relay.</summary>
    void AutopilotRunStarted()
    {
        if (!autoBaseline) return;
        // Through the game's own tier path: TierChanged, the map's generation tier, relayTuning rebuilt from the tier every frame.
        RaiseTier(autoBotTier, "baseline");
        relay.StateChanged += AutoBaseStateChanged;
        relay.Arrived += AutoBaseArrived;
        relay.Caught += AutoBaseCaught;
        map.DoorBroken += AutoBaseDoorBroken;
    }

    void AutoBaseBeforeRelayTick()
    {
        if (relay == null) return;
        autoPreState = relay.State;
        autoPrePosition = relay.Position;
        autoPreBuilt = map.IsBuilt(map.CellOf(relay.Position));
    }

    /// <summary>
    /// The game's relay.Noise, sent exactly as before. In a baseline run it also records the cause: emitted,
    /// heard (the Relay turned to hunt the source), ignored by a state that does not listen, or out of range.
    /// </summary>
    void AutopilotNoise(int cause, Vector3 source, float radius)
    {
        if (!autoBaseline || relay == null)
        {
            relay?.Noise(source, radius);
            return;
        }
        var before = relay.State;
        autoNoiseEmitted[cause]++;
        autoLastNoiseAt = autoPlayClock;
        autoLastNoiseCause = cause;
        if (cause == AutoCauseSprint && !PlayerSprinting) autoBotSprintNoiseNotSprinting++;
        autoNoiseInFlight = cause;
        relay.Noise(source, radius);
        autoNoiseInFlight = -1;
        var heard = relay.State == HunterState.Hunt && relay.StateTime == 0f && relay.ListenPoint.HasValue && relay.ListenPoint.Value == source;
        if (heard)
        {
            autoNoiseHeard[cause]++;
            autoLastHeardFrame = Time.frameCount;
            autoLastHeardCause = cause;
            if (before == HunterState.Hunt) autoHuntRetargets[cause]++;
        }
        else if (!relay.Released || before == HunterState.Chase || before == HunterState.BreakDoor) autoNoiseDeaf[cause]++;
        else autoNoiseOutOfRange[cause]++;
    }

    bool AutoRelayChasing => relay != null && (relay.State == HunterState.Chase || (relay.State == HunterState.BreakDoor && autoBreakFrom == HunterState.Chase));

    void AutoBaseStateChanged(HunterState next)
    {
        var previous = autoBaseState;
        autoBaseState = next;
        var sinceRelease = autoPlayClock - autoBaseReleaseAt;
        if (next == HunterState.Hunt)
        {
            // A heard noise turns it to Hunt inside the Noise call; the tick right after one still takes its cause.
            int cause;
            if (autoNoiseInFlight >= 0) cause = autoNoiseInFlight;
            else if (Time.frameCount - autoLastHeardFrame <= 1) cause = autoLastHeardCause;
            else cause = previous == HunterState.Chase || previous == HunterState.BreakDoor ? AutoCauseChaseLost : AutoCauseUnknown;
            autoHuntsBy[cause]++;
            if (autoFirstHunt < 0f) autoFirstHunt = sinceRelease;
        }
        if (next == HunterState.BreakDoor)
        {
            autoBreakFrom = previous;
            autoBreakAttempts[(int)previous]++;
        }
        var resumed = previous == HunterState.BreakDoor && autoBreakFrom == HunterState.Chase;
        if (next == HunterState.Chase && !resumed)
        {
            // A new chase (one resumed after a door it broke mid-chase is the same chase).
            var noise = autoPlayClock - autoLastNoiseAt <= 3f ? AutoCauseNames[autoLastNoiseCause] : "none";
            if (noise == "none") autoWanderEncounters++;
            autoChaseLog.Add(new BaselineChase
            {
                startAt = sinceRelease, from = previous.ToString(), noiseBefore = noise, end = "open",
                straightAtStart = Vector3.Distance(relay.Position, playerRoot.position),
            });
            autoChaseOpen = autoChaseLog.Count - 1;
            if (autoFirstChase < 0f) autoFirstChase = sinceRelease;
            if (autoBot == AutoBot.Evader03 || autoBot == AutoBot.Evader06) autoEvadeAt = autoPlayClock + (autoBot == AutoBot.Evader03 ? .3f : .6f);
        }
        else if (autoChaseOpen >= 0 && next != HunterState.Chase && !(next == HunterState.BreakDoor && previous == HunterState.Chase))
            AutoCloseChase(next == HunterState.Hunt ? "lost" : next == HunterState.Listen ? "relayed" : next.ToString());
    }

    void AutoCloseChase(string end)
    {
        var chase = autoChaseLog[autoChaseOpen];
        chase.end = end;
        chase.seconds = autoPlayClock - autoBaseReleaseAt - chase.startAt;
        if (chase.evaded)
        {
            if (end == "caught") autoBotEvadeCaught++;
            else if (end != "open") autoBotEvadeOk++;
        }
        autoChaseOpen = -1;
    }

    void AutoBaseCaught()
    {
        autoCatches++;
        if (autoChaseOpen >= 0) AutoCloseChase("caught");
    }

    void AutoBaseDoorBroken(Vector3 at) => autoDoorsBrokenFrom[(int)autoBreakFrom]++;

    /// <summary>It appeared: the first time is the release; every later one a relay, A7a or A7b by its cell on the tick before.</summary>
    void AutoBaseArrived(Vector3 feet, string tag)
    {
        var player = playerRoot.position;
        if (autoBaseReleaseAt < 0f)
        {
            autoBaseReleaseAt = autoPlayClock;
            autoReleaseStraight = Vector3.Distance(feet, player);
            autoReleaseWalk = AutoWalkMetres(feet);
            return;
        }
        autoRelayLog.Add(new BaselineRelay
        {
            at = autoPlayClock - autoBaseReleaseAt,
            kind = autoPreBuilt ? "A7a" : "A7b",
            stateBefore = autoPreState.ToString(),
            entry = tag ?? "",
            straightBefore = Vector3.Distance(autoPrePosition, player),
            walkBefore = autoPreBuilt ? AutoWalkMetres(autoPrePosition) : -1f,
            straightAfter = Vector3.Distance(feet, player),
            walkAfter = AutoWalkMetres(feet),
        });
    }

    /// <summary>Walking metres from the player to a point by the path field (capped at 150 m); -1 where it does not reach.</summary>
    float AutoWalkMetres(Vector3 point)
    {
        autoWalkField.Build(map, map.CellOf(playerRoot.position));
        var d = autoWalkField.DistanceTo(map.CellOf(point));
        return float.IsInfinity(d) ? -1f : d;
    }

    void AutoBaseTick(float dt)
    {
        // The sprint rule's answer from the last step (PlayerSprinting, PlayerWinded), for the bot's own counters.
        if (PlayerSprinting)
        {
            autoBotSprintSeconds += dt;
            if (!autoBotWasSprinting) autoBotSprintBursts++;
        }
        autoBotWasSprinting = PlayerSprinting;
        if (PlayerWinded) autoBotWindedSeconds += dt;
        if (relay != null && relay.Released) autoStateSeconds[(int)relay.State] += dt;
        autoSampleClock += dt;
        if (autoSampleClock < AutoSampleSeconds - 1e-4f) return;
        autoSampleClock -= AutoSampleSeconds;
        AutoBaseSample();
    }

    /// <summary>
    /// One sample of the ladder v2 would have shown: F, the walking metres from the player's cell to the Relay's
    /// by the path field (capped at 60 m); W = min(F, 3 × the straight 3D distance); and the stages on W with
    /// §9.1's bands, hysteresis and holds. No stage while the Relay is not on the map (Dormant).
    /// </summary>
    void AutoBaseSample()
    {
        autoSamples++;
        if (relay == null || !relay.Released)
        {
            autoDormantSamples++;
            autoS1 = autoS2 = false;
            return;
        }
        autoOnMapSamples++;
        var straight = Vector3.Distance(relay.Position, playerRoot.position);
        var f = float.PositiveInfinity;
        var playerCell = map.CellOf(playerRoot.position);
        // The field measures cell to cell: it can come out shorter than the straight line only by the bodies' offsets in their cells.
        if (straight <= AutoFieldCap + MapGrid.CellSize * 1.5f && map.IsBuilt(playerCell))
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            autoField.Build(map, playerCell);
            var ms = (float)watch.Elapsed.TotalMilliseconds;
            autoFieldBuilds++;
            autoFieldMsSum += ms;
            autoFieldMsMax = Mathf.Max(autoFieldMsMax, ms);
            autoFieldVisitedSum += autoField.Visited;
            f = autoField.DistanceTo(map.CellOf(relay.Position));
        }
        if (float.IsInfinity(f)) autoFUnreached++;
        var w = Mathf.Min(f, 3f * straight);
        AutoBand(autoBandF, f);
        AutoBand(autoBandW, w);
        AutoBand(autoBandStraight, straight);
        // Stage 2: in at W ≤ 18, out past 24 after ≥ 3 s; it includes stage 1. Stage 1: in at W ≤ 30, out past 36 after ≥ 4 s.
        var now = autoPlayClock;
        if (!autoS2 && w <= 18f)
        {
            autoS2 = true;
            autoS2Since = now;
            if (!autoS1)
            {
                autoS1 = true;
                autoS1Since = now;
            }
        }
        else if (autoS2 && w > 24f && now - autoS2Since >= 3f) autoS2 = false;
        if (!autoS1 && w <= 30f)
        {
            autoS1 = true;
            autoS1Since = now;
        }
        else if (autoS1 && !autoS2 && w > 36f && now - autoS1Since >= 4f) autoS1 = false;
        autoStageSamples[autoS2 ? 2 : autoS1 ? 1 : 0]++;
    }

    static void AutoBand(int[] band, float metres)
    {
        if (metres <= 18f) band[0]++;
        if (metres <= 30f) band[1]++;
        else if (metres <= 36f) band[2]++;
        else band[3]++;
    }

    // ---------- The bots ----------

    // The zone the bot first steps into (the staller's and the closed-zone bot's whole run), and the cell it stood in last frame.
    GridCoord autoBotZone, autoBotLastCell;
    bool autoBotZoneSet, autoBotLastCellSet;

    void AutoBotSteer(float dt, GridCoord here, out Vector2 local, out bool sprint)
    {
        local = Vector2.zero;
        sprint = false;
        if (!autoBotZoneSet)
        {
            autoBotZone = map.ZoneOf(here).id;
            autoBotZoneSet = true;
        }
        AutoBotTrackCrossing(here);
        AutoBotShutBehind(dt);
        switch (autoBot)
        {
            case AutoBot.Quiet:
                AutoBotRoam(dt, here, false, out local);
                break;
            case AutoBot.ShiftHolder:
                AutoBotRoam(dt, here, false, out local);
                sprint = true;
                break;
            case AutoBot.Noisy:
                if (AutoBotWindow(dt, here, out local)) break;
                AutoBotRoam(dt, here, false, out local);
                sprint = AutoBotOnStraight(here);
                break;
            case AutoBot.Evader03:
            case AutoBot.Evader06:
                AutoBotEvade(dt, here, out local, out sprint);
                break;
            case AutoBot.Staller:
                AutoBotRoam(dt, here, true, out local);
                break;
            case AutoBot.ClosedZone:
                AutoBotClosedZone(dt, here, out local, out sprint);
                break;
            case AutoBot.DoorSpammer:
                AutoBotDoorSpam(dt, here, out local);
                break;
            case AutoBot.EdgeRunner:
                if (autoRouteIndex >= autoRoute.Count) AutoPlanEdge(here);
                AutoWalkRoute(dt, here, out local);
                break;
        }
    }

    void AutoBotNote(string note)
    {
        if (autoBotNotes.Count < 40) autoBotNotes.Add(autoPlayClock.ToString("0.00", CultureInfo.InvariantCulture) + " s " + note);
    }

    /// <summary>Far routes (quiet, noisy, shift-holder, evaders between chases), or routes inside its first zone (staller, closed-zone).</summary>
    void AutoBotRoam(float dt, GridCoord here, bool inZone, out Vector2 local)
    {
        if (autoRouteIndex >= autoRoute.Count)
        {
            if (inZone) AutoPlanInZone(here);
            else AutopilotPlan(here);
        }
        AutoWalkRoute(dt, here, out local);
    }

    bool InBotZone(GridCoord cell) => map.ZoneOf(cell).id == autoBotZone;

    /// <summary>A route to a random cell of its zone at least 3 steps away (its zone has no doors inside: they sit on its borders).</summary>
    void AutoPlanInZone(GridCoord here)
    {
        autoRoute.Clear();
        autoRouteIndex = 0;
        AutoBfs(here, 30, InBotZone);
        var spots = new List<GridCoord>();
        foreach (var cell in autoReached) if (autoDepth[cell] >= 3) spots.Add(cell);
        if (spots.Count == 0)
            foreach (var cell in autoReached) if (autoDepth[cell] > 0) spots.Add(cell);
        if (spots.Count == 0) return;
        AutoRouteTo(here, spots[UnityEngine.Random.Range(0, spots.Count)]);
    }

    /// <summary>The edge runner's next leg: the reachable cell furthest north, keeping near the start door's column; none further north: a far route out of the pocket.</summary>
    void AutoPlanEdge(GridCoord here)
    {
        autoRoute.Clear();
        autoRouteIndex = 0;
        AutoBfs(here, 40, null);
        var best = here;
        var bestScore = AutoEdgeScore(here);
        foreach (var cell in autoReached)
        {
            var score = AutoEdgeScore(cell);
            if (score > bestScore)
            {
                bestScore = score;
                best = cell;
            }
        }
        if (best.y <= here.y)
        {
            autoEdgeStalls++;
            AutopilotPlan(here);
            return;
        }
        AutoRouteTo(here, best);
    }

    float AutoEdgeScore(GridCoord cell) => cell.y - .25f * Mathf.Abs(cell.x - startDoorCell.x);

    /// <summary>The noisy bot sprints on straights: its next two steps go the same way across open edges, and it already faces that way.</summary>
    bool AutoBotOnStraight(GridCoord here)
    {
        if (autoRouteIndex + 1 >= autoRoute.Count) return false;
        var a = autoRoute[autoRouteIndex];
        var b = autoRoute[autoRouteIndex + 1];
        int dx = a.x - here.x, dy = a.y - here.y;
        if (Mathf.Abs(dx) + Mathf.Abs(dy) != 1 || b.x - a.x != dx || b.y - a.y != dy) return false;
        if (!map.IsBuilt(a) || !map.IsBuilt(b) || map.PassageBetween(here, a) != FrontRoomsMapWorld.Passage.Open || map.PassageBetween(a, b) != FrontRoomsMapWorld.Passage.Open) return false;
        return Mathf.Abs(Mathf.DeltaAngle(yaw, Mathf.Atan2(dx, dy) * Mathf.Rad2Deg)) < 20f;
    }

    /// <summary>Turn to face a point at a pitch; true once within a degree of both.</summary>
    bool AutoFace(float dt, Vector3 point, float pitchTo)
    {
        var to = point - playerRoot.position;
        to.y = 0f;
        var desired = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
        yaw = Mathf.MoveTowardsAngle(yaw, desired, 300f * dt);
        pitch = Mathf.MoveTowards(pitch, pitchTo, 60f * dt);
        return Mathf.Abs(Mathf.DeltaAngle(yaw, desired)) < 1f && Mathf.Abs(pitch - pitchTo) < 1f;
    }

    // ---------- Shutting a door behind it (the noisy bot, and an evader while it evades) ----------

    FrontRoomsMapWorld.Door autoBotShut;
    float autoBotShutClock;

    /// <summary>A door it has just walked through (the last cell and this one share a door edge) is queued to be shut.</summary>
    void AutoBotTrackCrossing(GridCoord here)
    {
        var last = autoBotLastCell;
        var crossed = autoBotLastCellSet && here != last && Mathf.Abs(here.x - last.x) + Mathf.Abs(here.y - last.y) == 1;
        autoBotLastCell = here;
        autoBotLastCellSet = true;
        if (!crossed || !(autoBot == AutoBot.Noisy || autoEvading)) return;
        if (!map.IsBuilt(here) || !map.IsBuilt(last) || map.Cache.Edge(last, here) != EdgeKind.Door) return;
        var door = map.DoorBetween(last, here);
        if (door == null || door.broken) return;
        autoBotShut = door;
        autoBotShutClock = 0f;
    }

    /// <summary>Shut the queued door once a metre past its line and still within reach (E's 2.4 m), as a player turning round would.</summary>
    void AutoBotShutBehind(float dt)
    {
        var door = autoBotShut;
        if (door == null) return;
        autoBotShutClock += dt;
        var away = Flat(playerRoot.position - door.position).magnitude;
        if (door.broken || !door.open || door.leaf == null || autoBotShutClock > 2f || away > Reach)
        {
            autoBotShut = null;
            return;
        }
        if (away < 1f) return;
        // The lever may still be down (the leaf not yet free): Use does nothing then, and it tries again next frame.
        map.Use(door.leaf);
        if (door.open) return;
        autoBotDoorsShut++;
        autoBotShut = null;
    }

    // ---------- The noisy bot's windows ----------

    // The pane, its step (1 walk to 0.9 m in front of it, 2 aim, 3 hold E, 4 let the glass shot finish), the step's clock.
    FrontRoomsMapWorld.Window autoBotWindow;
    int autoBotWindowStep;
    float autoBotWindowClock;
    Vector3 autoBotWindowStand;
    GridCoord autoBotWindowCell;
    bool autoBotWindowCellSet;
    readonly HashSet<long> autoBotWindowsSeen = new HashSet<long>();
    const float AutoPaneStand = .9f, AutoPaneAimHeight = 1.3f;

    /// <summary>
    /// The noisy bot breaks every window it meets: once per cell it enters, a pane on one of that cell's edges
    /// it has not tried. It stands 0.9 m in front (inside the pane rule's 1.2 m), aims 1.3 m up and holds E
    /// through the glass shot's own input path until the pane gives. True while it is busy with one.
    /// </summary>
    bool AutoBotWindow(float dt, GridCoord here, out Vector2 local)
    {
        local = Vector2.zero;
        if (autoBotWindow == null)
        {
            if (autoBotWindowCellSet && here == autoBotWindowCell) return false;
            // Mid-climb or mid-step the cell is looked at again next frame.
            if (climbTime >= 0f || pullClear.HasValue) return false;
            autoBotWindowCell = here;
            autoBotWindowCellSet = true;
            var found = map.NearestIntactWindowForTools(playerRoot.position);
            if (found == null || found.pane == null || found.root == null || (found.a != here && found.b != here) || !autoBotWindowsSeen.Add(found.edge)) return false;
            // The root faces cell b (+Z): stand on this cell's side.
            var stand = found.root.position + found.root.forward * ((here == found.b ? 1f : -1f) * AutoPaneStand);
            stand.y = playerRoot.position.y;
            autoBotWindow = found;
            autoBotWindowStand = stand;
            autoBotWindowStep = 1;
            autoBotWindowClock = 0f;
            autoBotWindowsTried++;
        }
        var window = autoBotWindow;
        autoBotWindowClock += dt;
        switch (autoBotWindowStep)
        {
            case 1:
                if (Flat(autoBotWindowStand - playerRoot.position).magnitude > .1f)
                {
                    if (autoBotWindowClock > 4f) return AutoBotWindowDone("could not stand 0.9 m from it");
                    AutoSteerAt(dt, autoBotWindowStand, true, out local);
                    return true;
                }
                autoBotWindowStep = 2;
                autoBotWindowClock = 0f;
                return true;
            case 2:
            {
                var faced = AutoFace(dt, window.root.position, Mathf.Atan2(EyeHeight - AutoPaneAimHeight, AutoPaneStand) * Mathf.Rad2Deg);
                // The game decides whether E reaches it: the crosshair (last frame's aim) on this pane, within the pane rule.
                if (faced && aimedHold && aimed != null && window.pane != null && aimed.gameObject == window.pane)
                {
                    autoEDown = autoEHeld = true;
                    autoBotWindowStep = 3;
                    autoBotWindowClock = 0f;
                }
                else if (autoBotWindowClock > 1.5f) return AutoBotWindowDone("the crosshair never held it within the pane rule");
                return true;
            }
            case 3:
                autoEDown = false;
                if (map.IsBrokenWindow(window.a, window.b))
                {
                    autoEHeld = false;
                    autoBotWindowsBroken++;
                    autoBotWindowStep = 4;
                    autoBotWindowClock = 0f;
                }
                else if (autoBotWindowClock > 4f) return AutoBotWindowDone("it never gave");
                return true;
            default:
                // The glass shot hands the body back before it walks on.
                if ((glassShot != null && (glassShot.Active || glassShot.MoveLocked)) && autoBotWindowClock < 3f) return true;
                return AutoBotWindowDone(null);
        }
    }

    bool AutoBotWindowDone(string why)
    {
        autoEDown = autoEHeld = false;
        if (why != null) AutoBotNote("window " + autoBotWindow.a + "-" + autoBotWindow.b + ": " + why);
        autoBotWindow = null;
        // Plan afresh from where the window left it (the way through the frame is open now).
        autoRouteIndex = autoRoute.Count;
        return true;
    }

    // ---------- The evaders ----------

    float autoEvadeAt = -1f, autoEvadeUnseen;
    bool autoEvading, autoEvadeSprint;

    /// <summary>
    /// Walks like the quiet bot. When the Relay starts a chase it reacts after 0.3 or 0.6 s: it turns away and
    /// sprints for the nearest cell out of the Relay's sight (shutting doors it passes); once unseen for 0.3 s
    /// it walks away; seen again, it runs for the next corner. The evasion ends with the chase.
    /// </summary>
    void AutoBotEvade(float dt, GridCoord here, out Vector2 local, out bool sprint)
    {
        local = Vector2.zero;
        sprint = false;
        var chasing = AutoRelayChasing;
        if (autoEvadeAt >= 0f && autoPlayClock >= autoEvadeAt)
        {
            autoEvadeAt = -1f;
            if (chasing && !autoEvading)
            {
                autoEvading = true;
                autoEvadeSprint = true;
                autoEvadeUnseen = 0f;
                autoBotEvasions++;
                if (autoChaseOpen >= 0) autoChaseLog[autoChaseOpen].evaded = true;
                AutoPlanCover(here);
            }
        }
        if (autoEvading && !chasing)
        {
            // The chase is over (it lost the player, or relayed): walk on as before.
            autoEvading = false;
            autoEvadeSprint = false;
        }
        if (!autoEvading)
        {
            AutoBotRoam(dt, here, false, out local);
            return;
        }
        autoEvadeUnseen = relay.SeesPlayer ? 0f : autoEvadeUnseen + dt;
        if (autoEvadeSprint && autoEvadeUnseen > .3f)
        {
            autoEvadeSprint = false;
            AutoPlanAway(here);
        }
        else if (!autoEvadeSprint && relay.SeesPlayer)
        {
            autoEvadeSprint = true;
            AutoPlanCover(here);
        }
        else if (autoRouteIndex >= autoRoute.Count)
        {
            if (autoEvadeSprint) AutoPlanCover(here);
            else AutoPlanAway(here);
        }
        AutoWalkRoute(dt, here, out local);
        sprint = autoEvadeSprint;
    }

    /// <summary>
    /// The run for cover: the nearest cell (by walking) whose centre the Relay's eye cannot see at eye height,
    /// through cells no nearer the Relay than the player is now (it never runs past it). None: the reachable
    /// cell farthest from the Relay.
    /// </summary>
    void AutoPlanCover(GridCoord here)
    {
        var from = relay.Position;
        var eye = from + Vector3.up * ModuleUnits.RelayEye;
        var gap = Flat(playerRoot.position - from).magnitude;
        AutoBfs(here, 12, c => Flat(map.CellCenter(c) - from).magnitude >= gap - 1.5f);
        GridCoord? cover = null, farthest = null;
        var far = -1f;
        foreach (var cell in autoReached)
        {
            if (autoDepth[cell] == 0) continue;
            var centre = map.CellCenter(cell);
            var d = Flat(centre - from).magnitude;
            if (d > far)
            {
                far = d;
                farthest = cell;
            }
            if (AutoSightBlocked(eye, centre + Vector3.up * EyeHeight))
            {
                cover = cell;
                break;
            }
        }
        var goal = cover ?? farthest;
        if (goal.HasValue) AutoRouteTo(here, goal.Value);
        else
        {
            autoRoute.Clear();
            autoRouteIndex = 0;
        }
    }

    /// <summary>Out of sight, it walks away: the cell 8–30 steps off farthest from the Relay, through cells no nearer it than 3 m inside the gap.</summary>
    void AutoPlanAway(GridCoord here)
    {
        var from = relay.Position;
        var gap = Flat(playerRoot.position - from).magnitude;
        AutoBfs(here, 30, c => Flat(map.CellCenter(c) - from).magnitude >= gap - 3f);
        GridCoord? best = null;
        var bestDistance = -1f;
        foreach (var cell in autoReached)
        {
            if (autoDepth[cell] < 8) continue;
            var d = Flat(map.CellCenter(cell) - from).sqrMagnitude;
            if (d > bestDistance)
            {
                bestDistance = d;
                best = cell;
            }
        }
        if (best.HasValue) AutoRouteTo(here, best.Value);
        else AutopilotPlan(here);
    }

    readonly RaycastHit[] autoSightHits = new RaycastHit[16];

    /// <summary>True when anything but the player or the Relay's rig stands between the two points.</summary>
    bool AutoSightBlocked(Vector3 from, Vector3 to)
    {
        var dir = to - from;
        var distance = dir.magnitude;
        if (distance < .01f) return false;
        var count = Physics.RaycastNonAlloc(from, dir / distance, autoSightHits, distance, ~0, QueryTriggerInteraction.Ignore);
        for (var i = 0; i < count; i++)
        {
            var c = autoSightHits[i].collider;
            if (c == null || c == playerBody || (hunter != null && c.transform.IsChildOf(hunter))) continue;
            return true;
        }
        return false;
    }

    // ---------- Working a door (the closed-zone bot shuts, the door-spammer toggles) ----------

    FrontRoomsMapWorld.Door autoBotDoor;
    GridCoord autoBotDoorNear, autoBotDoorFar;
    Vector3 autoBotDoorStand;
    float autoBotDoorClock, autoBotDoorScanAt, autoBotSpamAt;
    readonly HashSet<long> autoBotDoorsSkipped = new HashSet<long>();

    /// <summary>Go to work a door: stand 1.3 m back from its line in the near cell, on the opening's centre, clear of the leaf.</summary>
    void AutoBotTakeDoor(FrontRoomsMapWorld.Door door, GridCoord near, GridCoord far, GridCoord here)
    {
        autoBotDoor = door;
        autoBotDoorNear = near;
        autoBotDoorFar = far;
        autoBotDoorClock = 0f;
        var into = map.CellCenter(far) - map.CellCenter(near);
        into.y = 0f;
        var stand = map.CrossingPoint(near, far) - into.normalized * 1.3f;
        stand.y = playerRoot.position.y;
        autoBotDoorStand = stand;
        AutoRouteTo(here, near);
    }

    /// <summary>
    /// Walk to the door's stand and face it. True once there (or, after 6 s, wherever it got to, if the door is
    /// within E's reach). A door it cannot reach in 20 s is skipped for good.
    /// </summary>
    bool AutoBotGoToDoor(float dt, GridCoord here, out Vector2 local)
    {
        local = Vector2.zero;
        var door = autoBotDoor;
        autoBotDoorClock += dt;
        if (autoBotDoorClock > 20f)
        {
            AutoBotNote("door " + autoBotDoorNear + "-" + autoBotDoorFar + ": not reached in 20 s, skipped");
            autoBotDoorsSkipped.Add(door.edge);
            autoBotDoor = null;
            autoRouteIndex = autoRoute.Count;
            return false;
        }
        var inReach = Flat(door.position - playerRoot.position).magnitude <= Reach;
        var late = autoBotDoorClock > 6f;
        if (here != autoBotDoorNear && !(late && inReach))
        {
            if (autoRouteIndex >= autoRoute.Count)
            {
                AutoBfs(here, 40, null);
                AutoRouteTo(here, autoBotDoorNear);
            }
            AutoWalkRoute(dt, here, out local);
            return false;
        }
        if (!late && Flat(autoBotDoorStand - playerRoot.position).magnitude > .1f)
        {
            AutoSteerAt(dt, autoBotDoorStand, true, out local);
            return false;
        }
        AutoFace(dt, door.position, 0f);
        return inReach;
    }

    /// <summary>
    /// The closed-zone bot: every 2 s it looks for a door on its zone's border that stands open (unbroken) and
    /// goes to shut it; with none left, it sprints round inside the zone.
    /// </summary>
    void AutoBotClosedZone(float dt, GridCoord here, out Vector2 local, out bool sprint)
    {
        local = Vector2.zero;
        sprint = false;
        if (autoBotDoor == null && autoPlayClock >= autoBotDoorScanAt)
        {
            autoBotDoorScanAt = autoPlayClock + 2f;
            AutoFindZoneDoor(here);
        }
        var door = autoBotDoor;
        if (door != null)
        {
            // Shut by now (or broken, or its chunk rebuilt): done with it.
            if (door.broken || !door.open || map.DoorBetween(autoBotDoorNear, autoBotDoorFar) != door)
            {
                autoBotDoor = null;
                autoRouteIndex = autoRoute.Count;
                return;
            }
            if (!AutoBotGoToDoor(dt, here, out local)) return;
            map.Use(door.leaf);
            if (door.open) return;
            autoBotDoorsShut++;
            autoBotDoor = null;
            autoRouteIndex = autoRoute.Count;
            return;
        }
        AutoBotRoam(dt, here, true, out local);
        sprint = true;
    }

    /// <summary>The closed-zone bot's next door: one of its zone's border doors that stands open, nearest by walking inside the zone.</summary>
    void AutoFindZoneDoor(GridCoord here)
    {
        AutoBfs(here, 40, InBotZone);
        int doors = 0, open = 0;
        FrontRoomsMapWorld.Door pick = null;
        GridCoord near = default, far = default;
        foreach (var cell in autoReached)
            foreach (var step in AutoSteps)
            {
                var n = cell + step;
                if (InBotZone(n) || !map.IsBuilt(n) || map.Cache.Edge(cell, n) != EdgeKind.Door) continue;
                var door = map.DoorBetween(cell, n);
                if (door == null) continue;
                doors++;
                if (!door.open || door.broken) continue;
                open++;
                if (pick != null || autoBotDoorsSkipped.Contains(door.edge)) continue;
                pick = door;
                near = cell;
                far = n;
            }
        if (autoBotZoneDoors < 0)
        {
            autoBotZoneDoors = doors;
            autoBotZoneDoorsOpen = open;
            AutoBotNote("closedzone: zone " + autoBotZone + ", " + autoReached.Count + " cells reached, " + doors + " border doors, " + open + " open");
        }
        if (pick != null) AutoBotTakeDoor(pick, near, far, here);
    }

    /// <summary>The door-spammer: the nearest unbroken door at least 6 m from the start door; it stands at it and opens or shuts it every 0.8 s. A door the Relay breaks sends it to the next.</summary>
    void AutoBotDoorSpam(float dt, GridCoord here, out Vector2 local)
    {
        local = Vector2.zero;
        var door = autoBotDoor;
        if (door == null || door.broken || map.DoorBetween(autoBotDoorNear, autoBotDoorFar) != door)
        {
            if (door != null) AutoBotNote("doorspammer: door " + autoBotDoorNear + "-" + autoBotDoorFar + (door.broken ? " broken by the Relay" : " rebuilt"));
            autoBotDoor = null;
            if (autoPlayClock >= autoBotDoorScanAt)
            {
                autoBotDoorScanAt = autoPlayClock + 2f;
                AutoFindSpamDoor(here);
            }
            if (autoBotDoor == null)
            {
                AutoBotRoam(dt, here, false, out local);
                return;
            }
            door = autoBotDoor;
        }
        if (!AutoBotGoToDoor(dt, here, out local) || autoPlayClock < autoBotSpamAt) return;
        autoBotSpamAt = autoPlayClock + .8f;
        if (door.open)
        {
            // Mid-lever the door ignores E: that beat is lost, as a player's would be.
            map.Use(door.leaf);
            if (door.open) return;
            autoBotDoorsShut++;
            autoBotDoorToggles++;
        }
        else if (map.TryOpenDoor(autoBotDoorNear, autoBotDoorFar))
        {
            autoDoorsOpened++;
            autoBotDoorToggles++;
        }
    }

    void AutoFindSpamDoor(GridCoord here)
    {
        AutoBfs(here, 30, null);
        foreach (var cell in autoReached)
            foreach (var step in AutoSteps)
            {
                var n = cell + step;
                if (!map.IsBuilt(n) || map.Cache.Edge(cell, n) != EdgeKind.Door) continue;
                var door = map.DoorBetween(cell, n);
                if (door == null || door.broken || autoBotDoorsSkipped.Contains(door.edge) || Flat(door.position - startDoorPoint).magnitude < 6f) continue;
                AutoBotTakeDoor(door, cell, n, here);
                AutoBotNote("doorspammer: door " + cell + "-" + n + ", " + autoDepth[cell] + " steps away");
                return;
            }
    }

    // ---------- The run's JSON ----------

    string AutoBaseFinish(string reason)
    {
        AutoBaseRestoreTime();
        if (autoChaseOpen >= 0) AutoCloseChase("open");
        var since = autoBaseReleaseAt >= 0f ? autoPlayClock - autoBaseReleaseAt : 0f;
        var perMinute = since > 0f ? 60f / since : 0f;
        var r = new BaselineReport
        {
            mode = AutoBotNames[(int)autoBot],
            seed = runSeed,
            tier = autoBotTier,
            minutes = autoBotSeconds / 60f,
            playSeconds = autoPlayClock,
            endedBy = reason,
            caught = phase == Phase.Caught,
            startHoldEndedAt = autoBotLeftAt,
            // The hold ends on the first frame at or past AutoStartHoldSeconds when nothing else held it.
            startOnFixedFrame = autoBotLeftAt >= 0f && autoBotLeftAt < AutoStartHoldSeconds + .02f,
            releasedAt = autoBaseReleaseAt,
            releaseStraightMetres = autoReleaseStraight,
            releaseWalkMetres = autoReleaseWalk,
            secondsAfterRelease = since,
            firstHuntAfterRelease = autoFirstHunt,
            firstChaseAfterRelease = autoFirstChase,
            catches = autoCatches,
            wanderEncounters = autoWanderEncounters,
            chaseLog = autoChaseLog,
            relayLog = autoRelayLog,
            glassBroken = autoNoiseEmitted[AutoCauseGlass],
            tierFinal = tier,
            tierLog = new List<string>(autoTiers),
            samples = autoSamples,
            onMapSamples = autoOnMapSamples,
            errors = autoErrors,
            errorLog = new List<string>(autoErrorLog),
        };
        foreach (HunterState state in Enum.GetValues(typeof(HunterState)))
        {
            var seconds = autoStateSeconds[(int)state];
            r.stateShares.Add(new BaselineStateTime { state = state.ToString(), seconds = seconds, share = since > 0f ? seconds / since : 0f });
            if (autoDoorsBrokenFrom[(int)state] > 0) r.doorsBrokenByState.Add(new BaselineStateCount { state = state.ToString(), count = autoDoorsBrokenFrom[(int)state] });
            if (autoBreakAttempts[(int)state] > 0) r.breakAttemptsByState.Add(new BaselineStateCount { state = state.ToString(), count = autoBreakAttempts[(int)state] });
            r.doorsBroken += autoDoorsBrokenFrom[(int)state];
        }
        foreach (var n in autoHuntsBy) r.hunts += n;
        r.huntsPerMinute = r.hunts * perMinute;
        r.huntsByCause = new BaselineHuntCauses
        {
            sprint = autoHuntsBy[AutoCauseSprint], door = autoHuntsBy[AutoCauseDoor], glass = autoHuntsBy[AutoCauseGlass],
            otherNoise = autoHuntsBy[AutoCauseOther], chaseLost = autoHuntsBy[AutoCauseChaseLost], other = autoHuntsBy[AutoCauseUnknown],
        };
        r.huntRetargets = AutoKinds(autoHuntRetargets);
        // What brought it to the player: a hunt on a noise, by the noise; a chase with no noise in the 3 s before, a wander encounter.
        r.callsByCause = new BaselineCallCauses
        {
            sprint = autoHuntsBy[AutoCauseSprint], door = autoHuntsBy[AutoCauseDoor], glass = autoHuntsBy[AutoCauseGlass],
            otherNoise = autoHuntsBy[AutoCauseOther], wanderEncounter = autoWanderEncounters,
        };
        r.callsByCause.total = r.callsByCause.sprint + r.callsByCause.door + r.callsByCause.glass + r.callsByCause.otherNoise + r.callsByCause.wanderEncounter;
        r.callsByCause.perMinute = r.callsByCause.total * perMinute;
        r.chases = autoChaseLog.Count;
        r.chasesPerMinute = r.chases * perMinute;
        foreach (var chase in autoChaseLog)
        {
            if (chase.end == "lost") r.chasesLost++;
            else if (chase.end == "relayed") r.chasesRelayed++;
            else if (chase.end == "caught") r.chasesCaught++;
            else if (chase.end == "open") r.chasesOpenAtEnd++;
            r.chaseMeanSeconds += chase.seconds / autoChaseLog.Count;
            r.chaseMaxSeconds = Mathf.Max(r.chaseMaxSeconds, chase.seconds);
        }
        r.noiseEmitted = AutoKinds(autoNoiseEmitted);
        r.noiseHeard = AutoKinds(autoNoiseHeard);
        r.noiseOutOfRange = AutoKinds(autoNoiseOutOfRange);
        r.noiseIgnoredByState = AutoKinds(autoNoiseDeaf);
        r.relaysAfterRelease = autoRelayLog.Count;
        foreach (var relayed in autoRelayLog)
        {
            if (relayed.kind == "A7a") r.relaysA7a++;
            else r.relaysA7b++;
        }
        r.dormantShare = autoSamples > 0 ? autoDormantSamples / (float)autoSamples : 0f;
        r.fUnreachedShare = autoOnMapSamples > 0 ? autoFUnreached / (float)autoOnMapSamples : 0f;
        r.fBands = AutoBands(autoBandF);
        r.wBands = AutoBands(autoBandW);
        r.straightBands = AutoBands(autoBandStraight);
        if (autoOnMapSamples > 0)
        {
            r.wStage0 = autoStageSamples[0] / (float)autoOnMapSamples;
            r.wStage1 = autoStageSamples[1] / (float)autoOnMapSamples;
            r.wStage2 = autoStageSamples[2] / (float)autoOnMapSamples;
        }
        r.wStage0OfRun = autoSamples > 0 ? (autoDormantSamples + autoStageSamples[0]) / (float)autoSamples : 0f;
        r.bot = new BaselineBot
        {
            distanceWalked = autoDistance, cellsVisited = autoCells.Count, zonesVisited = zonesVisited.Count, routes = autoRoutes,
            sprintBursts = autoBotSprintBursts, sprintSeconds = autoBotSprintSeconds, windedSeconds = autoBotWindedSeconds,
            sprintNoises = autoNoiseEmitted[AutoCauseSprint], sprintNoisesNotSprinting = autoBotSprintNoiseNotSprinting,
            doorsOpened = autoDoorsOpened, doorsShut = autoBotDoorsShut, doorToggles = autoBotDoorToggles,
            windowsTried = autoBotWindowsTried, windowsBroken = autoBotWindowsBroken,
            evasionsAttempted = autoBotEvasions, evasionsSucceeded = autoBotEvadeOk, evasionsCaught = autoBotEvadeCaught,
            zoneDoors = autoBotZoneDoors, zoneDoorsOpenAtStart = autoBotZoneDoorsOpen, edgeStalls = autoEdgeStalls,
            notes = new List<string>(autoBotNotes),
        };
        var ok = mapPlay && relay != null && relay.Released && autoErrors == 0;
        r.verdict = (ok ? "PASS" : "FAIL") + " · baseline " + r.mode + " T" + autoBotTier + " seed " + runSeed + " · ended by " + reason;
        // The hash covers the behaviour only: perf is still its blank default here.
        r.behaviourHash = AutoHash(JsonUtility.ToJson(r));
        r.perf = AutoBasePerf();
        var dir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Verification", "relay-baseline");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, runSeed + "-" + r.mode + "-T" + autoBotTier + ".json");
        File.WriteAllText(path, JsonUtility.ToJson(r, true));
        Log("BASELINE " + r.verdict + " · " + path);
        return r.verdict;
    }

    BaselinePerf AutoBasePerf()
    {
        var perf = new BaselinePerf
        {
            frames = autoBaseFrameMs.Count,
            p50FrameMs = Percentile(autoBaseFrameMs, .5f),
            p95FrameMs = Percentile(autoBaseFrameMs, .95f),
            p99FrameMs = Percentile(autoBaseFrameMs, .99f),
            loadAverageStart = autoBaseLoadStart,
            loadAverageEnd = AutoLoadAverage(),
            wallSeconds = (float)autoBaseWall.Elapsed.TotalSeconds,
            relayTickMaxMs = autoRelayTickMs,
            pathFieldBuilds = autoFieldBuilds,
            pathFieldMeanMs = autoFieldBuilds > 0 ? (float)(autoFieldMsSum / autoFieldBuilds) : 0f,
            pathFieldMaxMs = autoFieldMsMax,
            pathFieldMeanVisited = autoFieldBuilds > 0 ? (float)(autoFieldVisitedSum / autoFieldBuilds) : 0f,
            startDoorOpenedAt = autoStartDoorAt,
            mapSettledAt = autoSettledAt,
        };
        double sum = 0;
        foreach (var ms in autoBaseFrameMs)
        {
            sum += ms;
            perf.maxFrameMs = Mathf.Max(perf.maxFrameMs, ms);
        }
        perf.meanFrameMs = autoBaseFrameMs.Count > 0 ? (float)(sum / autoBaseFrameMs.Count) : 0f;
        return perf;
    }

    static BaselineNoiseKinds AutoKinds(int[] counts) => new BaselineNoiseKinds { sprint = counts[0], door = counts[1], glass = counts[2], otherNoise = counts[3] };

    static BaselineBands AutoBands(int[] band)
    {
        float total = band[1] + band[2] + band[3];
        return total <= 0f ? new BaselineBands() : new BaselineBands { le18 = band[0] / total, le30 = band[1] / total, from30to36 = band[2] / total, gt36 = band[3] / total };
    }

    /// <summary>FNV-1a, 64-bit, over the text.</summary>
    static string AutoHash(string text)
    {
        var h = 14695981039346656037UL;
        foreach (var c in text)
        {
            h ^= c;
            h *= 1099511628211UL;
        }
        return h.ToString("x16");
    }

    /// <summary>
    /// The v2 §7 path field, built only for the Step 0 baseline and its bench (nothing in the game reads it): an
    /// 8-neighbour (octile) Dijkstra from a root cell over built cells, in metres, capped. Orthogonal steps 3 m;
    /// a diagonal step 4.24 m only inside a room (the four edges round the 2 × 2 block are plain open edges, so
    /// it never cuts through an arch, a door or a window); crossing an open edge, an arch, an open or broken door
    /// or a broken window adds 0, a shut door or an intact window 9; walls block. It keeps its own arrays over a
    /// square window round the root, cleared by a stamp, so a rebuild allocates nothing; and it reads only built
    /// cells, so it never makes the map generate a chunk.
    /// </summary>
    public sealed class BaselinePathField
    {
        public const float Orthogonal = 3f, Diagonal = 4.2426407f, ShutOpening = 9f;
        static readonly int[] SideX = { 1, -1, 0, 0 }, SideY = { 0, 0, 1, -1 };
        public readonly float Cap;
        readonly int radius, side;
        readonly float[] distance;
        readonly int[] reached, settled, heapNode;
        readonly float[] heapKey;
        int heapCount, stamp;

        public GridCoord Root { get; private set; }
        /// <summary>The cells the last build settled.</summary>
        public int Visited { get; private set; }

        public BaselinePathField(float capMetres)
        {
            Cap = capMetres;
            // No cell further than Cap / 3 steps along either axis can be within Cap.
            radius = Mathf.CeilToInt(capMetres / Orthogonal) + 1;
            side = radius * 2 + 1;
            distance = new float[side * side];
            reached = new int[side * side];
            settled = new int[side * side];
            // Each settled cell pushes at most its eight neighbours.
            heapNode = new int[side * side * 8 + 1];
            heapKey = new float[heapNode.Length];
        }

        /// <summary>Metres from the root to the cell; infinity where the last build did not reach it within the cap.</summary>
        public float DistanceTo(GridCoord cell)
        {
            var i = Index(cell);
            return i >= 0 && settled[i] == stamp ? distance[i] : float.PositiveInfinity;
        }

        public void Build(FrontRoomsMapWorld map, GridCoord root)
        {
            stamp++;
            Root = root;
            Visited = 0;
            heapCount = 0;
            if (!map.IsBuilt(root)) return;
            Reach(Index(root), 0f);
            var cache = map.Cache;
            while (heapCount > 0)
            {
                var node = Pop(out var d);
                if (settled[node] == stamp || d > distance[node]) continue;
                settled[node] = stamp;
                Visited++;
                var cell = new GridCoord(node % side - radius + Root.x, node / side - radius + Root.y);
                // The four sides: the step across each, and which are plain open edges (for the diagonals).
                var plain = 0;
                for (var k = 0; k < 4; k++)
                {
                    var n = new GridCoord(cell.x + SideX[k], cell.y + SideY[k]);
                    if (!map.IsBuilt(n)) continue;
                    var edge = cache.Edge(cell, n);
                    if (edge == EdgeKind.Wall) continue;
                    if (edge == EdgeKind.Open) plain |= 1 << k;
                    // Doors and windows by their state now: swung open, broken or smashed through, or shut or intact.
                    var open = edge == EdgeKind.Open || edge == EdgeKind.Arch || map.PassageBetween(cell, n) == FrontRoomsMapWorld.Passage.Open;
                    var i = Index(n);
                    if (i >= 0 && settled[i] != stamp) Reach(i, d + Orthogonal + (open ? 0f : ShutOpening));
                }
                for (var k = 0; k < 4; k++)
                {
                    int dx = k < 2 ? 1 : -1, dy = (k & 1) == 0 ? 1 : -1;
                    if ((plain & (1 << (dx > 0 ? 0 : 1))) == 0 || (plain & (1 << (dy > 0 ? 2 : 3))) == 0) continue;
                    var corner = new GridCoord(cell.x + dx, cell.y + dy);
                    var i = Index(corner);
                    if (i < 0 || settled[i] == stamp || !map.IsBuilt(corner)) continue;
                    if (cache.Edge(new GridCoord(cell.x + dx, cell.y), corner) != EdgeKind.Open || cache.Edge(new GridCoord(cell.x, cell.y + dy), corner) != EdgeKind.Open) continue;
                    Reach(i, d + Diagonal);
                }
            }
        }

        int Index(GridCoord cell)
        {
            int x = cell.x - Root.x + radius, y = cell.y - Root.y + radius;
            return x < 0 || y < 0 || x >= side || y >= side ? -1 : x + y * side;
        }

        void Reach(int i, float d)
        {
            if (d > Cap || (reached[i] == stamp && d >= distance[i])) return;
            reached[i] = stamp;
            distance[i] = d;
            // A binary heap on the distance (stale entries are skipped when popped).
            var at = heapCount++;
            while (at > 0)
            {
                var parent = (at - 1) / 2;
                if (heapKey[parent] <= d) break;
                heapNode[at] = heapNode[parent];
                heapKey[at] = heapKey[parent];
                at = parent;
            }
            heapNode[at] = i;
            heapKey[at] = d;
        }

        int Pop(out float d)
        {
            var top = heapNode[0];
            d = heapKey[0];
            var lastNode = heapNode[--heapCount];
            var lastKey = heapKey[heapCount];
            var at = 0;
            while (true)
            {
                var child = at * 2 + 1;
                if (child >= heapCount) break;
                if (child + 1 < heapCount && heapKey[child + 1] < heapKey[child]) child++;
                if (heapKey[child] >= lastKey) break;
                heapNode[at] = heapNode[child];
                heapKey[at] = heapKey[child];
                at = child;
            }
            heapNode[at] = lastNode;
            heapKey[at] = lastKey;
            return top;
        }
    }
}
#endif
