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
    // of the bots below for -autopilotMinutes of game time after the Relay's release (default 4, the staller 5),
    // so every run has the same exposure; the run's tier is forced to -autopilotTier and held there (the tier
    // lock; -autopilotTierFree lets the rise rule run). Fixed 1/60 s step, so a seed, a mode and a tier replay
    // the same run under any machine load. Only a catch ends a run sooner. It writes
    // Verification/relay-baseline/<seed>-<mode>-T<n>.json. Nothing here changes the game: the bots press only
    // what a player could (the walk, Shift, E through EDown/EHeld, doors through TryOpenDoor and Use as the
    // autopilot always has), and the Relay is only watched (its public state, its events, the map's API).
    // The one exception is evaderv2, which sets v2's G5 conditions on its own run (see below).
    //   quiet        walks, never sprints, never touches a window, opens only the doors its route needs;
    //   noisy        sprints on every straight, shuts each door right after passing it, and every 20 s walks
    //                to the nearest intact window it can reach and breaks it (from 0.9 m, inside the pane rule);
    //   evader03/06  walks; 0.3 / 0.6 s after the Relay starts a chase it turns away and sprints for the
    //                nearest cell out of its sight, shutting doors it passes, then walks away once unseen;
    //   evaderv2     evader06 under v2's G5 conditions (§8): a 2 s stamina bar and the chase capped at 4.4 m/s;
    //   staller      wanders inside the zone it first steps into, the whole run;
    //   shiftholder  walks its routes with Shift always held (the winded rule: sprint noise only while it sprints);
    //   doorspammer  stands at one door away from the start door and opens or shuts it every 0.8 s;
    //   edgerunner   walks north (the way the start door faces) as far as it can: the unbuilt-cell relay (A7b);
    //   closedzone   goes to the nearest low or standard zone with at least 2 border doors, opens them if
    //                fewer than 2 stand open, shuts every one, then sprints round inside the zone.
    // Each mode has a fidelity check (did it do its job), and a failed check fails the run (AutoBaseModeChecks).
    enum AutoBot { None, Quiet, Noisy, Evader03, Evader06, Staller, ShiftHolder, DoorSpammer, EdgeRunner, ClosedZone, EvaderV2 }
    static readonly string[] AutoBotNames = { "none", "quiet", "noisy", "evader03", "evader06", "staller", "shiftholder", "doorspammer", "edgerunner", "closedzone", "evaderv2" };
    AutoBot autoBot;
    bool autoBaseline;
    string autoBotUnknown;
    int autoBotTier = 1;
    // autoBotSeconds is when the run ends (play seconds): budget seconds after the release once it has happened.
    float autoBotSeconds, autoBotBudget;
    bool autoBotBudgetSet;
    // The tier lock: RaiseTier ignores every rise but the baseline's own (the guard sits in RaiseTier).
    bool autoTierLock = true;
    readonly float[] autoTierSeconds = new float[16];
    string autoBotArgs = "";

    // v2's G5 conditions (evaderv2): the stamina bar and the chase cap. The cap goes through the Inspector base
    // (hunterTuning), which the tier row scales every frame, so the Relay's speed is base × row ≤ the cap.
    const float AutoV2StaminaSeconds = 2f, AutoV2ChaseCap = 4.4f;
    float autoBaseChaseSpeed = -1f, autoChaseSpeedMax;
    bool AutoV2Conditions => autoBot == AutoBot.EvaderV2;
    bool AutoIsEvader => autoBot == AutoBot.Evader03 || autoBot == AutoBot.Evader06 || autoBot == AutoBot.EvaderV2;

    // The fixed step: game time no longer follows how fast frames come. Frame times are not reported (a batch
    // editor under load says nothing about the game's); the Relay's tick and the path field are timed instead.
    float autoPrevCaptureDelta;
    int autoPrevTargetFps;
    bool autoBaseTimeSet;
    readonly System.Diagnostics.Stopwatch autoBaseWall = new System.Diagnostics.Stopwatch();
    string autoBaseLoadStart;
    BaselineBuild autoBuild;

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
    float autoBaseReleaseAt = -1f, autoFirstHunt = -1f, autoFirstChase = -1f, autoReleaseStraight = -1f, autoReleaseWalk = -1f, autoReleaseF = -1f, autoReleaseW = -1f;
    readonly float[] autoStateSeconds = new float[16];
    readonly int[] autoNoiseEmitted = new int[4], autoNoiseHeard = new int[4], autoNoiseDeaf = new int[4], autoNoiseOutOfRange = new int[4], autoHuntRetargets = new int[4];
    readonly int[] autoHuntsBy = new int[6], autoBreakAttempts = new int[16], autoDoorsBrokenFrom = new int[16];
    int autoNoiseInFlight = -1, autoLastHeardFrame = int.MinValue / 2, autoLastHeardCause, autoWanderEncounters, autoCatches, autoChaseOpen = -1, autoHuntResumes, autoGlassBroken;
    // The call log: every noise the player made, and what the Relay did with it. The active call is the heard
    // noise (or the lost chase) the Relay is still acting on: set when it turns to hunt, kept through its search,
    // dropped when it goes back to Listen or Wander. A chase links to the call active when it starts.
    const int AutoCallLogMax = 4000;
    int autoActiveCall = -1, autoLastHeardCall = -1, autoInFlightCall = -1, autoCallsLogged;
    bool autoActiveRehunt;
    readonly List<BaselineCall> autoCallLog = new List<BaselineCall>();
    readonly List<BaselineHunt> autoHuntLog = new List<BaselineHunt>();
    readonly List<BaselineChase> autoChaseLog = new List<BaselineChase>();
    readonly List<BaselineRelay> autoRelayLog = new List<BaselineRelay>();
    // The Relay as it stood before this frame's Tick: a relay in that Tick is the unbuilt-cell one (A7b) when its cell was not built.
    Vector3 autoPrePosition;
    bool autoPreBuilt;

    // The warning ladder (v2 §7, §9.1), sampled every 0.25 s of game time on W = min(F, 3 × straight), F from the
    // game's own path field (FrontRoomsMapPathField: the hunter's hearing model, an intact window +9 m).
    // §9.1 (as clarified 2026-10-08): a stage starts on the first sample in its band (0 → 2 at once when W ≤ 18)
    // and lasts at least 4 s (stage 1) or 3 s (stage 2) from its start (a 2 → 1 drop starts stage 1's 4 s again);
    // after that it ends once W has stayed above its exit band for 1.0 s. Stage 2 drops to 1, or straight to 0
    // when W has also stayed above 36 m for that 1.0 s. An unbuilt Relay cell is stage 0 at once. The hunter's
    // own WarnStage is reported beside it, with how often the two agree.
    const float AutoSampleSeconds = .25f, AutoFieldCap = 60f, AutoWalkCap = 150f, AutoHeardCap = 150f, AutoCensorSeconds = 5f;
    FrontRoomsMapPathField autoField, autoWalkField, autoHeardField;
    float autoSampleClock, autoS1Since, autoS2Since, autoDwellSince, autoAbove1, autoAbove2, autoAbove36In2, autoFieldMsMax, autoLastW = -1f, autoLastF = -1f;
    int autoSamples, autoOnMapSamples, autoFUnreached, autoFieldBuilds, autoStage, autoStage1Entries;
    double autoFieldMsSum, autoFieldVisitedSum;
    readonly int[] autoBandF = new int[4], autoBandW = new int[4], autoBandStraight = new int[4];
    // Per on-map sample: the §9.1 stage and the hunter's, so shares can be cut short of a catch (survival censoring).
    readonly List<byte> autoStageTrace = new List<byte>(), autoHunterStageTrace = new List<byte>();
    // The hunter's WarnStage: entries into stage 1 (from 0) and when its current stage-1 and stage-2 runs began.
    int autoHunterStage1Entries;
    float autoHunterS1Since = -1f, autoHunterS2Since = -1f;

    // The bot's own counters.
    int autoBotSprintBursts, autoBotSprintNoiseNotSprinting, autoBotDoorsShut, autoBotDoorToggles, autoBotWindowsTried, autoBotWindowsBroken;
    int autoBotEvasions, autoBotEvadeOk, autoBotEvadeCaught, autoBotZoneDoors = -1, autoBotZoneDoorsOpen = -1, autoEdgeStalls, autoBotZoneDoorsOpened;
    float autoBotSprintSeconds, autoBotWindedSeconds, autoBotNotSprinting = 1e9f;
    bool autoBotWasSprinting;
    readonly List<string> autoBotNotes = new List<string>();
    readonly List<BaselineModeCheck> autoModeChecks = new List<BaselineModeCheck>();

    [Serializable]
    sealed class BaselineNoiseKinds { public int sprint, door, glass, otherNoise; }

    [Serializable]
    sealed class BaselineHuntCauses { public int sprint, door, glass, otherNoise, chaseLost, other; }

    [Serializable]
    sealed class BaselineStateTime { public string state; public float seconds, share; }

    [Serializable]
    sealed class BaselineStateCount { public string state; public int count; }

    [Serializable]
    sealed class BaselineCall
    {
        // Seconds after release (-1 before it); sprint, door or glass; what became of it: heard (it turned
        // to hunt the source), outOfRange, ignored (Chase or BreakDoor do not listen), dormant (not released yet);
        // the Relay's state before and after; metres from the Relay to the source, straight and (heard only) by
        // the hearing field.
        public float at;
        public string cause, outcome, stateBefore, stateAfter;
        public float straight, pathMetres = -1f;
    }

    [Serializable]
    sealed class BaselineHunt
    {
        // A new hunt (never the resume after a door it broke mid-hunt): seconds after release, cause (sprint, door,
        // glass, chaseLost = the re-hunt after a lost chase, unknown), the state before, and the call it answers.
        public float at;
        public string cause, from;
        public int call = -1;
        public float heardStraight = -1f, heardPath = -1f;
    }

    [Serializable]
    sealed class BaselineChase
    {
        // Seconds after release; the state it came from; why it came: call (a heard noise; see call), rehunt (the
        // hunt after a lost chase), wander (from Wander or Listen with no call), other; how it ended.
        public float startAt, seconds, straightAtStart;
        public string from, cause, end;
        public int call = -1;
        // G3: the ladder at the start: W and F (-1 unreached), the §9.1 stage and how long stage ≥ 1 and stage 2 had
        // run without a break; the same for the hunter's own WarnStage.
        public float wAtStart = -1f, fAtStart = -1f;
        public int stageAtStart, hunterStageAtStart;
        public float stage1Seconds, stage2Seconds, hunterStage1Seconds, hunterStage2Seconds;
        // Evaders: whether evasion began (only those chases grade G5), when, and every time sight broke while it ran:
        // "<t> <door|wall|prop> <player cell> from <Relay cell>".
        public bool evaded;
        public float evadeAt = -1f;
        public List<string> sightBreaks = new List<string>();
    }

    [Serializable]
    sealed class BaselineRelay
    {
        // Seconds after release; A7a (the 45 s timed leash) or A7b (its cell stopped being built); metres to the player
        // before (where it was) and after (where it appeared); walking -1 where the field could not reach (unbuilt, or over 150 m).
        public float at;
        public string kind, stateBefore, entry;
        public float straightBefore, walkBefore, straightAfter, walkAfter;
        // The ladder where it appeared: F, W (-1 unreached), the §9.1 stage and the hunter's.
        public float fAfter = -1f, wAfter = -1f;
        public int stageAfter, hunterStageAfter;
        // A7b only: chunks between where it stood and the player's chunk; past the build radius, the player's own
        // movement dropped its chunk (the edge runner's job).
        public int chunkGap = -1;
        public bool byPlayerPosition;
    }

    [Serializable]
    sealed class BaselineBands { public float le18, le30, from30to36, gt36; }

    [Serializable]
    sealed class BaselineStages
    {
        // Shares of on-map samples at stage 0, 1, 2 (2 includes chase time: there is no stage 3 here), over the whole
        // run and censored (without the last 5 s before a catch); stage-1 entries (from 0) per 10 min after release.
        public float stage0, stage1, stage2;
        public float censoredStage0, censoredStage1, censoredStage2;
        public int stage1Entries;
        public float stage1EntriesPer10Min;
    }

    [Serializable]
    sealed class BaselineModeCheck { public string check, detail; public bool ok, soft; }

    [Serializable]
    sealed class BaselineBuild
    {
        // The build the run measured: HEAD of the project's git, whether Assets/Packages/ProjectSettings differ from
        // it (iCloud "name 2" copies and untracked files aside), how many files, and a hash of that diff; the SHA-1
        // of the hunter and of this harness; the run's arguments.
        public string gitSha = "unknown", diffHash = "", hunterSha1 = "", harnessSha1 = "", unity = "", args = "";
        public bool gitDirty;
        public int dirtyFiles;
        public string harness = "step0 r2 (2026-10-08)";
    }

    [Serializable]
    sealed class BaselineBot
    {
        public float distanceWalked;
        public int cellsVisited, zonesVisited, routes;
        // A burst starts when it sprints after at least 0.3 s without sprinting.
        public int sprintBursts;
        public float sprintSeconds, windedSeconds;
        public int sprintNoises, sprintNoisesNotSprinting;
        public int doorsOpened, doorsShut, doorToggles;
        public int windowsTried, windowsBroken;
        public int evasionsAttempted, evasionsSucceeded, evasionsCaught;
        // Closed zone: the zone it chose, its border doors, how many stood open when it got there, how many it opened.
        public string zoneChosen = "";
        public int zoneDoors = -1, zoneDoorsOpenAtStart = -1, zoneDoorsOpened;
        // Door-spammer: doors it used, how many the Relay broke under it, seconds at a door, toggles a minute there.
        public int spamDoors, spamDoorsBroken;
        public float spamSeconds, spamTogglesPerMinute;
        public int edgeStalls;
        // Edge runner: the furthest north it got, in cells from where it left the start rooms.
        public int northCells;
        // evaderv2: the stamina bar and the chase cap it ran under, and the fastest chase speed the Relay had.
        public float staminaBarSeconds, chaseCap, chaseSpeedMax;
        public List<string> notes = new List<string>();
    }

    [Serializable]
    sealed class BaselinePerf
    {
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
        // Budget minutes after the release; whether the tier was held at its start value.
        public float minutes, playSeconds;
        public bool tierLocked;
        public string endedBy;
        public bool caught;
        // The bot left the start rooms on its fixed frame (AutoStartHoldSeconds): false means this run's start depended on load.
        public float startHoldEndedAt;
        public bool startOnFixedFrame;
        public float releasedAt = -1f, releaseStraightMetres = -1f, releaseWalkMetres = -1f, releaseF = -1f, releaseW = -1f;
        public float secondsAfterRelease;
        public List<BaselineStateTime> stateShares = new List<BaselineStateTime>();
        // v2's Away state does not exist in this Relay: always 0 here, kept so v2 runs report it in the same place.
        public float awaySeconds;
        public float firstHuntAfterRelease = -1f, firstChaseAfterRelease = -1f;
        // New hunts only (huntResumes: a hunt picked up again after a door it broke, not counted as one).
        public int hunts, huntResumes;
        public float huntsPerMinute;
        public BaselineHuntCauses huntsByCause = new BaselineHuntCauses();
        public BaselineNoiseKinds huntRetargets = new BaselineNoiseKinds();
        public List<BaselineHunt> huntLog = new List<BaselineHunt>();
        public int calls;
        public List<BaselineCall> callLog = new List<BaselineCall>();
        public int chases;
        public float chasesPerMinute;
        public int chasesLost, chasesRelayed, chasesCaught, chasesOpenAtEnd, wanderEncounters;
        public float chaseMeanSeconds, chaseMaxSeconds;
        public List<BaselineChase> chaseLog = new List<BaselineChase>();
        public int catches;
        public BaselineNoiseKinds noiseEmitted = new BaselineNoiseKinds(), noiseHeard = new BaselineNoiseKinds(), noiseOutOfRange = new BaselineNoiseKinds(), noiseIgnoredByState = new BaselineNoiseKinds();
        public int relaysAfterRelease, relaysA7a, relaysA7b, relaysA7bByPlayer;
        public List<BaselineRelay> relayLog = new List<BaselineRelay>();
        public int doorsBroken;
        public List<BaselineStateCount> doorsBrokenByState = new List<BaselineStateCount>(), breakAttemptsByState = new List<BaselineStateCount>();
        // Panes that broke (the map's GlassBroken), whoever broke them.
        public int glassBroken;
        public int tierFinal;
        public List<string> tierLog = new List<string>();
        public List<BaselineStateTime> tierSeconds = new List<BaselineStateTime>();
        // The ladder: F (the game's path field, capped 60 m), W = min(F, 3 × straight 3D) and the straight line, as
        // shares of the time the Relay was on the map; the stages by §9.1 and by the hunter's WarnStage.
        public float sampleSeconds = AutoSampleSeconds, fieldCapMetres = AutoFieldCap;
        public int samples, onMapSamples;
        public float fUnreachedShare;
        public BaselineBands fBands = new BaselineBands(), wBands = new BaselineBands(), straightBands = new BaselineBands();
        public BaselineStages stages = new BaselineStages(), hunterStages = new BaselineStages();
        // Both stage traces, one digit per on-map sample (0.25 s), so stagesAgree can be checked offline.
        public string stageTrace = "", hunterStageTrace = "";
        public BaselineBot bot = new BaselineBot();
        public List<BaselineModeCheck> modeChecks = new List<BaselineModeCheck>();
        public int errors;
        public List<string> errorLog = new List<string>();
        // FNV-1a of everything above: two runs that played the same have the same hash (build and perf are not in it).
        public string behaviourHash;
        public BaselineBuild build = new BaselineBuild();
        public BaselinePerf perf = new BaselinePerf();
    }

    void AutoBaseStart(string[] args)
    {
        string mode = null;
        var minutes = 0f;
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "-autopilotTierFree") autoTierLock = false;
            if (i + 1 >= args.Length) continue;
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
        autoBotBudget = 60f * (minutes > 0f ? minutes : autoBot == AutoBot.Staller ? 5f : 4f);
        // Until the release the run may last the budget plus 3 minutes; the release resets the end (AutoBaseTick).
        autoBotSeconds = autoBotBudget + 180f;
        var kept = new List<string>();
        for (var i = 0; i < args.Length; i++) if (args[i].StartsWith("-autopilot", StringComparison.Ordinal)) kept.Add(args[i] + (i + 1 < args.Length && !args[i + 1].StartsWith("-", StringComparison.Ordinal) ? " " + args[i + 1] : ""));
        autoBotArgs = string.Join(" ", kept);
        autoPrevCaptureDelta = Time.captureDeltaTime;
        autoPrevTargetFps = Application.targetFrameRate;
        Time.captureDeltaTime = 1f / 60f;
        Application.targetFrameRate = -1;
        autoBaseTimeSet = true;
        autoField = new FrontRoomsMapPathField(AutoFieldCap);
        autoWalkField = new FrontRoomsMapPathField(AutoWalkCap);
        autoHeardField = new FrontRoomsMapPathField(AutoHeardCap);
        autoBuild = AutoBaseBuildInfo();
        autoBaseLoadStart = AutoLoadAverage();
        autoBaseWall.Start();
    }

    /// <summary>The build this run measures (BaselineBuild): git HEAD and its diff over the game's sources, and file hashes.</summary>
    BaselineBuild AutoBaseBuildInfo()
    {
        var root = Directory.GetParent(Application.dataPath).FullName;
        var build = new BaselineBuild { unity = Application.unityVersion, args = autoBotArgs };
        var sha = AutoRun("/usr/bin/git", "-C \"" + root + "\" rev-parse HEAD");
        if (sha.Length == 40) build.gitSha = sha;
        var status = AutoRun("/usr/bin/git", "-C \"" + root + "\" status --porcelain --untracked-files=no -- Assets Packages ProjectSettings");
        foreach (var line in status.Split('\n'))
        {
            if (line.Length < 4) continue;
            // An iCloud conflict copy ("Name 2.ext") that the clone left out is not a change to the game.
            var path = line.Substring(3).Trim('"');
            if (line.StartsWith(" D", StringComparison.Ordinal) && System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileNameWithoutExtension(path), " [0-9]+$")) continue;
            build.dirtyFiles++;
        }
        build.gitDirty = build.dirtyFiles > 0;
        if (build.gitDirty) build.diffHash = AutoHash(AutoRun("/usr/bin/git", "-C \"" + root + "\" diff HEAD -- Assets Packages ProjectSettings"));
        build.hunterSha1 = AutoSha1(Path.Combine(Application.dataPath, "Scripts", "FrontRoomsMap", "FrontRoomsMapHunter.cs"));
        build.harnessSha1 = AutoSha1(Path.Combine(Application.dataPath, "Scripts", "FrontRooms3DGame.Baseline.cs"));
        return build;
    }

    /// <summary>A command's standard output, trimmed ("" when it cannot run).</summary>
    static string AutoRun(string file, string arguments)
    {
        try
        {
            var info = new System.Diagnostics.ProcessStartInfo(file, arguments) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
            using (var process = System.Diagnostics.Process.Start(info))
            {
                var text = process.StandardOutput.ReadToEnd();
                process.WaitForExit(10000);
                return text.Trim();
            }
        }
        catch (Exception)
        {
            return "";
        }
    }

    static string AutoSha1(string path)
    {
        try
        {
            using (var sha = System.Security.Cryptography.SHA1.Create())
                return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
        }
        catch (Exception)
        {
            return "";
        }
    }

    /// <summary>Hand the editor back its own frame step and frame-rate cap (once), and the Relay its own chase speed.</summary>
    void AutoBaseRestoreTime()
    {
        if (autoBaseChaseSpeed > 0f)
        {
            hunterTuning.chaseSpeed = autoBaseChaseSpeed;
            autoBaseChaseSpeed = -1f;
        }
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

    /// <summary>Each frame (the game's autopilot calls it). Frame times are no longer kept; see autoBaseWall.</summary>
    void AutoBaseFrame()
    {
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
        relay.WarnStageChanged += AutoBaseHunterStage;
        map.DoorBroken += AutoBaseDoorBroken;
        map.GlassBroken += AutoBaseGlassBroken;
        if (AutoV2Conditions)
        {
            autoBaseChaseSpeed = hunterTuning.chaseSpeed;
            AutoBaseApplyChaseCap();
        }
    }

    void AutoBaseGlassBroken(Vector3 at) => autoGlassBroken++;

    /// <summary>evaderv2: the Inspector base set so that the tier row's chase speed stays at or under the cap.</summary>
    void AutoBaseApplyChaseCap()
    {
        if (autoBaseChaseSpeed <= 0f) return;
        var row = Mathf.Max(.01f, TierRules.At(tier).chaseSpeed);
        hunterTuning.chaseSpeed = Mathf.Min(autoBaseChaseSpeed, AutoV2ChaseCap / row);
    }

    /// <summary>The hunter's own WarnStage (Step 1): entries into stage 1 and when its current runs began.</summary>
    void AutoBaseHunterStage(int stage)
    {
        var now = autoPlayClock;
        if (stage >= 1 && autoHunterS1Since < 0f)
        {
            autoHunterS1Since = now;
            autoHunterStage1Entries++;
        }
        if (stage >= 2 && autoHunterS2Since < 0f) autoHunterS2Since = now;
        if (stage < 2) autoHunterS2Since = -1f;
        if (stage < 1) autoHunterS1Since = -1f;
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
        if (cause == AutoCauseSprint && !PlayerSprinting) autoBotSprintNoiseNotSprinting++;
        autoNoiseInFlight = cause;
        var callIndex = autoCallLog.Count < AutoCallLogMax ? autoCallLog.Count : -1;
        autoCallsLogged++;
        autoInFlightCall = callIndex;
        relay.Noise(source, radius);
        autoNoiseInFlight = -1;
        autoInFlightCall = -1;
        var heard = relay.State == HunterState.Hunt && relay.StateTime == 0f && relay.ListenPoint.HasValue && relay.ListenPoint.Value == source;
        string outcome;
        if (heard)
        {
            outcome = "heard";
            autoNoiseHeard[cause]++;
            autoLastHeardFrame = Time.frameCount;
            autoLastHeardCause = cause;
            autoLastHeardCall = callIndex;
            if (before == HunterState.Hunt)
            {
                autoHuntRetargets[cause]++;
                // A re-target is the same hunt answering a newer call.
                autoActiveCall = callIndex;
                autoActiveRehunt = false;
            }
        }
        else if (!relay.Released)
        {
            outcome = "dormant";
            autoNoiseDeaf[cause]++;
        }
        else if (before == HunterState.Chase || before == HunterState.BreakDoor)
        {
            outcome = "ignored";
            autoNoiseDeaf[cause]++;
        }
        else
        {
            outcome = "outOfRange";
            autoNoiseOutOfRange[cause]++;
        }
        if (callIndex >= 0)
        {
            var call = new BaselineCall
            {
                at = autoBaseReleaseAt >= 0f ? autoPlayClock - autoBaseReleaseAt : -1f,
                cause = AutoCauseNames[cause], outcome = outcome, stateBefore = before.ToString(), stateAfter = relay.State.ToString(),
                straight = Vector3.Distance(relay.Position, source),
            };
            if (heard) call.pathMetres = AutoHeardMetres(source);
            autoCallLog.Add(call);
            if (heard && autoHuntLog.Count > 0 && autoHuntLog[autoHuntLog.Count - 1].call == callIndex)
            {
                autoHuntLog[autoHuntLog.Count - 1].heardStraight = call.straight;
                autoHuntLog[autoHuntLog.Count - 1].heardPath = call.pathMetres;
            }
        }
    }

    /// <summary>Metres from a heard noise to the Relay by the hearing field (the game's FrontRoomsMapPathField); -1 past 150 m or unbuilt.</summary>
    float AutoHeardMetres(Vector3 source)
    {
        autoHeardField.Build(map, map.CellOf(source));
        var d = autoHeardField.DistanceTo(map.CellOf(relay.Position));
        return float.IsInfinity(d) ? -1f : d;
    }

    bool AutoRelayChasing => relay != null && (relay.State == HunterState.Chase || (relay.State == HunterState.BreakDoor && autoBreakFrom == HunterState.Chase));

    void AutoBaseStateChanged(HunterState next)
    {
        var previous = autoBaseState;
        autoBaseState = next;
        var sinceRelease = autoPlayClock - autoBaseReleaseAt;
        if (next == HunterState.Hunt)
        {
            if (previous == HunterState.BreakDoor && autoBreakFrom == HunterState.Hunt)
            {
                // The hunt it broke a door for, picked up again: the same hunt, not a new one.
                autoHuntResumes++;
            }
            else
            {
                // A heard noise turns it to Hunt inside the Noise call; the tick right after one still takes its cause.
                // The hunt after a lost chase (LoseTrack) comes straight from Chase.
                int cause;
                var call = -1;
                if (autoNoiseInFlight >= 0)
                {
                    cause = autoNoiseInFlight;
                    call = autoInFlightCall;
                }
                else if (Time.frameCount - autoLastHeardFrame <= 1)
                {
                    cause = autoLastHeardCause;
                    call = autoLastHeardCall;
                }
                else cause = previous == HunterState.Chase ? AutoCauseChaseLost : AutoCauseUnknown;
                autoHuntsBy[cause]++;
                autoActiveCall = call;
                autoActiveRehunt = cause == AutoCauseChaseLost;
                autoHuntLog.Add(new BaselineHunt { at = sinceRelease, cause = cause == AutoCauseUnknown ? "unknown" : AutoCauseNames[cause], from = previous.ToString(), call = call });
                if (autoFirstHunt < 0f) autoFirstHunt = sinceRelease;
            }
        }
        if (next == HunterState.Listen || next == HunterState.Wander)
        {
            // It no longer acts on any call.
            autoActiveCall = -1;
            autoActiveRehunt = false;
        }
        if (next == HunterState.BreakDoor)
        {
            autoBreakFrom = previous;
            autoBreakAttempts[(int)previous]++;
        }
        var resumed = previous == HunterState.BreakDoor && autoBreakFrom == HunterState.Chase;
        if (next == HunterState.Chase && !resumed)
        {
            // A new chase (one resumed after a door it broke mid-chase is the same chase). Why it came: the call it
            // was answering, the hunt after a chase it lost, or nothing at all (Wander or Listen: a wander encounter).
            string cause;
            if (autoActiveRehunt) cause = "rehunt";
            else if (autoActiveCall >= 0) cause = "call";
            else if (previous == HunterState.Wander || previous == HunterState.Listen) cause = "wander";
            else cause = "other";
            if (cause == "wander") autoWanderEncounters++;
            var chase = new BaselineChase
            {
                startAt = sinceRelease, from = previous.ToString(), cause = cause, call = cause == "call" ? autoActiveCall : -1, end = "open",
                straightAtStart = Vector3.Distance(relay.Position, playerRoot.position),
                stageAtStart = autoStage, hunterStageAtStart = relay.WarnStage,
                stage1Seconds = autoStage >= 1 ? autoPlayClock - autoS1Since : 0f,
                stage2Seconds = autoStage >= 2 ? autoPlayClock - autoS2Since : 0f,
                hunterStage1Seconds = autoHunterS1Since >= 0f ? autoPlayClock - autoHunterS1Since : 0f,
                hunterStage2Seconds = autoHunterS2Since >= 0f ? autoPlayClock - autoHunterS2Since : 0f,
            };
            AutoLadderAt(relay.Position, out chase.fAtStart, out chase.wAtStart);
            autoChaseLog.Add(chase);
            autoChaseOpen = autoChaseLog.Count - 1;
            if (autoFirstChase < 0f) autoFirstChase = sinceRelease;
            if (AutoIsEvader) autoEvadeAt = autoPlayClock + (autoBot == AutoBot.Evader03 ? .3f : .6f);
        }
        else if (autoChaseOpen >= 0 && next != HunterState.Chase && !(next == HunterState.BreakDoor && previous == HunterState.Chase))
            AutoCloseChase(next == HunterState.Hunt ? "lost" : next == HunterState.Listen ? "relayed" : next.ToString());
    }

    /// <summary>F and W from the player to a point now (the game's field, capped 60 m; -1 where it does not reach).</summary>
    void AutoLadderAt(Vector3 point, out float f, out float w)
    {
        f = w = -1f;
        var playerCell = map.CellOf(playerRoot.position);
        if (!map.IsBuilt(playerCell)) return;
        autoField.Build(map, playerCell);
        var d = autoField.DistanceTo(map.CellOf(point));
        var straight = Vector3.Distance(point, playerRoot.position);
        if (!float.IsInfinity(d)) f = d;
        var wv = Mathf.Min(d, 3f * straight);
        if (!float.IsInfinity(wv)) w = wv;
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
            AutoLadderAt(feet, out autoReleaseF, out autoReleaseW);
            return;
        }
        var entry = new BaselineRelay
        {
            at = autoPlayClock - autoBaseReleaseAt,
            kind = autoPreBuilt ? "A7a" : "A7b",
            stateBefore = autoPreState.ToString(),
            entry = tag ?? "",
            straightBefore = Vector3.Distance(autoPrePosition, player),
            walkBefore = autoPreBuilt ? AutoWalkMetres(autoPrePosition) : -1f,
            straightAfter = Vector3.Distance(feet, player),
            walkAfter = AutoWalkMetres(feet),
            stageAfter = autoStage,
            hunterStageAfter = relay.WarnStage,
        };
        AutoLadderAt(feet, out entry.fAfter, out entry.wAfter);
        if (!autoPreBuilt)
        {
            // Its chunk was dropped: by the player walking away when that chunk lies past the build radius from theirs.
            var from = MapGrid.ChunkOf(map.CellOf(autoPrePosition));
            var at = MapGrid.ChunkOf(map.CellOf(player));
            entry.chunkGap = Mathf.Max(Mathf.Abs(from.x - at.x), Mathf.Abs(from.y - at.y));
            entry.byPlayerPosition = entry.chunkGap > map.BuildRadius;
        }
        autoRelayLog.Add(entry);
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
        // The run's budget counts from the release, so every run has the same exposure.
        if (!autoBotBudgetSet && autoBaseReleaseAt >= 0f)
        {
            autoBotBudgetSet = true;
            autoBotSeconds = autoBaseReleaseAt + autoBotBudget;
        }
        // The sprint rule's answer from the last step (PlayerSprinting, PlayerWinded), for the bot's own counters.
        // A burst is a sprint that starts after at least 0.3 s without one (not every cell's re-plan).
        if (PlayerSprinting)
        {
            autoBotSprintSeconds += dt;
            if (!autoBotWasSprinting && autoBotNotSprinting >= .3f) autoBotSprintBursts++;
            autoBotNotSprinting = 0f;
        }
        else autoBotNotSprinting += dt;
        autoBotWasSprinting = PlayerSprinting;
        if (PlayerWinded) autoBotWindedSeconds += dt;
        if (AutoV2Conditions)
        {
            // v2's 2 s bar: the stamina never holds more than 2 s of sprint (it drains and refills as the game's rule says).
            stamina = Mathf.Min(stamina, AutoV2StaminaSeconds);
            AutoBaseApplyChaseCap();
        }
        if (relay != null && relay.Released)
        {
            autoStateSeconds[(int)relay.State] += dt;
            autoTierSeconds[Mathf.Clamp(tier, 0, autoTierSeconds.Length - 1)] += dt;
            if (relay.State == HunterState.Chase) autoChaseSpeedMax = Mathf.Max(autoChaseSpeedMax, relayTuning.chaseSpeed);
        }
        if (autoBot == AutoBot.EdgeRunner && autoBotLeftAt >= 0f && !inStartRooms)
            autoBotNorth = Mathf.Max(autoBotNorth, map.CellOf(playerRoot.position).y - startDoorCell.y);
        AutoBotWatchSight();
        autoSampleClock += dt;
        if (autoSampleClock < AutoSampleSeconds - 1e-4f) return;
        autoSampleClock -= AutoSampleSeconds;
        AutoBaseSample();
    }

    /// <summary>
    /// One sample of the ladder v2 would have shown: F, the walking metres from the player's cell to the Relay's
    /// by the game's path field (capped at 60 m); W = min(F, 3 × the straight 3D distance); and the stages on W
    /// by §9.1 (see AutoSampleSeconds). No stage while the Relay is not on the map (Dormant).
    /// </summary>
    void AutoBaseSample()
    {
        autoSamples++;
        if (relay == null || !relay.Released)
        {
            AutoStageToZero();
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
        autoLastF = float.IsInfinity(f) ? -1f : f;
        autoLastW = float.IsInfinity(w) ? -1f : w;
        AutoBand(autoBandF, f);
        AutoBand(autoBandW, w);
        AutoBand(autoBandStraight, straight);
        if (map.IsBuilt(map.CellOf(relay.Position))) AutoStageStep(w);
        else AutoStageToZero();
        autoStageTrace.Add((byte)autoStage);
        autoHunterStageTrace.Add((byte)Mathf.Clamp(relay.WarnStage, 0, 2));
    }

    /// <summary>
    /// §9.1 on one sample (see AutoSampleSeconds). autoS1Since is the G3 run timer (stage ≥ 1 without a break; a
    /// 2 → 1 drop does not reset it); autoDwellSince is the current stage's minimum-time start (set on 0 → 1,
    /// 0 → 2 and 2 → 1).
    /// </summary>
    void AutoStageStep(float w)
    {
        var now = autoPlayClock;
        if (autoStage < 2 && w <= 18f)
        {
            if (autoStage < 1)
            {
                autoS1Since = now;
                autoStage1Entries++;
            }
            autoStage = 2;
            autoS2Since = autoDwellSince = now;
            autoAbove1 = autoAbove2 = autoAbove36In2 = 0f;
            return;
        }
        if (autoStage < 1 && w <= 30f)
        {
            autoStage = 1;
            autoS1Since = autoDwellSince = now;
            autoStage1Entries++;
            autoAbove1 = 0f;
            return;
        }
        if (autoStage == 2)
        {
            autoAbove2 = w > 24f ? autoAbove2 + AutoSampleSeconds : 0f;
            autoAbove36In2 = w > 36f ? autoAbove36In2 + AutoSampleSeconds : 0f;
            if (now - autoDwellSince >= 3f - 1e-4f && autoAbove2 >= 1f - 1e-4f)
            {
                if (autoAbove36In2 >= 1f - 1e-4f) AutoStageToZero();
                else
                {
                    autoStage = 1;
                    autoDwellSince = now;
                    autoAbove1 = 0f;
                }
            }
        }
        else if (autoStage == 1)
        {
            autoAbove1 = w > 36f ? autoAbove1 + AutoSampleSeconds : 0f;
            if (now - autoDwellSince >= 4f - 1e-4f && autoAbove1 >= 1f - 1e-4f) AutoStageToZero();
        }
    }

    void AutoStageToZero()
    {
        autoStage = 0;
        autoAbove1 = autoAbove2 = autoAbove36In2 = 0f;
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
                if (!AutoBotSeekWindow(dt, here, out local)) AutoBotRoam(dt, here, false, out local);
                sprint = AutoBotOnStraight(here);
                break;
            case AutoBot.Evader03:
            case AutoBot.Evader06:
            case AutoBot.EvaderV2:
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
    const float AutoPaneStand = .9f, AutoPaneAimHeight = 1.3f, AutoWindowEvery = 20f, AutoWindowSeekGiveUp = 40f;
    // The window it is walking to (AutoBotSeekWindow), the cell it breaks it from, and when it may look for the next.
    FrontRoomsMapWorld.Window autoBotWindowGoal;
    GridCoord autoBotWindowGoalCell;
    float autoBotWindowSeekAt, autoBotWindowGoalSince;
    int autoBotWindowRoutes;

    /// <summary>The intact window on the edge between two neighbouring cells, or null.</summary>
    FrontRoomsMapWorld.Window AutoWindowBetween(GridCoord a, GridCoord b)
    {
        var w = map.NearestIntactWindowForTools(map.CrossingPoint(a, b));
        return w != null && ((w.a == a && w.b == b) || (w.a == b && w.b == a)) ? w : null;
    }

    /// <summary>An intact window on one of the cell's edges that the bot has not tried yet, or null.</summary>
    FrontRoomsMapWorld.Window AutoUntriedWindowAt(GridCoord cell)
    {
        foreach (var step in AutoSteps)
        {
            var n = cell + step;
            if (!map.IsBuilt(cell) || !map.IsBuilt(n) || map.Cache.Edge(cell, n) != EdgeKind.Window || map.IsBrokenWindow(cell, n)) continue;
            var w = AutoWindowBetween(cell, n);
            if (w != null && w.pane != null && w.root != null && !autoBotWindowsSeen.Contains(w.edge)) return w;
        }
        return null;
    }

    /// <summary>
    /// The noisy bot's glass: every 20 s (from its last window) it walks to the nearest intact window it can
    /// reach (breadth-first over the cells it can walk, up to 30 steps) and breaks it from that cell. True while
    /// it walks there; AutoBotWindow takes over in the cell.
    /// </summary>
    bool AutoBotSeekWindow(float dt, GridCoord here, out Vector2 local)
    {
        local = Vector2.zero;
        if (autoBotWindowGoal != null && (autoBotWindowGoal.pane == null || autoPlayClock - autoBotWindowGoalSince > AutoWindowSeekGiveUp))
        {
            AutoBotNote("window " + autoBotWindowGoal.a + "-" + autoBotWindowGoal.b + (autoBotWindowGoal.pane == null ? ": broken before it got there" : ": not reached in 40 s"));
            autoBotWindowsSeen.Add(autoBotWindowGoal.edge);
            autoBotWindowGoal = null;
            autoRouteIndex = autoRoute.Count;
        }
        if (autoBotWindowGoal == null)
        {
            if (autoPlayClock < autoBotWindowSeekAt) return false;
            // None in reach now: look again in 2 s, from wherever the roam has taken it.
            autoBotWindowSeekAt = autoPlayClock + 2f;
            AutoBfs(here, 30, null);
            foreach (var cell in autoReached)
            {
                var w = AutoUntriedWindowAt(cell);
                if (w == null) continue;
                autoBotWindowGoal = w;
                autoBotWindowGoalCell = cell;
                autoBotWindowGoalSince = autoPlayClock;
                autoBotWindowRoutes++;
                AutoRouteTo(here, cell);
                break;
            }
            if (autoBotWindowGoal == null) return false;
        }
        if (here == autoBotWindowGoalCell) return false;
        if (autoRouteIndex >= autoRoute.Count)
        {
            AutoBfs(here, 40, null);
            AutoRouteTo(here, autoBotWindowGoalCell);
            if (autoRoute.Count == 0) return false;
        }
        AutoWalkRoute(dt, here, out local);
        return true;
    }

    /// <summary>
    /// The noisy bot breaks a window: the one it walked to, or an untried pane on an edge of a cell it enters.
    /// It stands 0.9 m in front (inside the pane rule's 1.2 m), aims 1.3 m up and holds E through the glass
    /// shot's own input path until the pane gives. True while it is busy with one.
    /// </summary>
    bool AutoBotWindow(float dt, GridCoord here, out Vector2 local)
    {
        local = Vector2.zero;
        if (autoBotWindow == null)
        {
            // Mid-climb or mid-step the cell is looked at again next frame.
            if (climbTime >= 0f || pullClear.HasValue) return false;
            FrontRoomsMapWorld.Window found = null;
            if (autoBotWindowGoal != null && here == autoBotWindowGoalCell)
            {
                found = autoBotWindowGoal;
                autoBotWindowGoal = null;
            }
            else if (!(autoBotWindowCellSet && here == autoBotWindowCell))
            {
                autoBotWindowCell = here;
                autoBotWindowCellSet = true;
                found = AutoUntriedWindowAt(here);
            }
            if (found == null || found.pane == null || found.root == null || (found.a != here && found.b != here)) return false;
            autoBotWindowsSeen.Add(found.edge);
            autoBotWindowSeekAt = autoPlayClock + AutoWindowEvery;
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
                if (autoChaseOpen >= 0)
                {
                    autoChaseLog[autoChaseOpen].evaded = true;
                    autoChaseLog[autoChaseOpen].evadeAt = autoPlayClock - autoBaseReleaseAt;
                }
                autoSawLast = relay.SeesPlayer;
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

    // Evaders: whether the Relay saw the player on the last frame of the evasion, for the sight-break log.
    bool autoSawLast;
    int autoBotNorth;

    /// <summary>
    /// While an evasion runs: the frame the Relay stops seeing the player, log what broke the line (the first
    /// thing between its eye and the player's: a door leaf, a wall, or a prop) and the two cells.
    /// </summary>
    void AutoBotWatchSight()
    {
        if (!autoEvading || relay == null || autoChaseOpen < 0) return;
        var sees = relay.SeesPlayer;
        if (autoSawLast && !sees)
        {
            var chase = autoChaseLog[autoChaseOpen];
            if (chase.sightBreaks.Count < 12)
            {
                var eye = relay.Position + Vector3.up * ModuleUnits.RelayEye;
                var to = playerRoot.position + Vector3.up * EyeHeight;
                var by = "none";
                var dir = to - eye;
                var distance = dir.magnitude;
                if (distance > .01f)
                {
                    var count = Physics.RaycastNonAlloc(eye, dir / distance, autoSightHits, distance, ~0, QueryTriggerInteraction.Ignore);
                    var nearest = float.MaxValue;
                    for (var i = 0; i < count; i++)
                    {
                        var c = autoSightHits[i].collider;
                        if (c == null || c == playerBody || (hunter != null && c.transform.IsChildOf(hunter)) || autoSightHits[i].distance >= nearest) continue;
                        nearest = autoSightHits[i].distance;
                        by = c.name.StartsWith("Door leaf", StringComparison.Ordinal) ? "door" : c.GetComponentInParent<FrontRoomsModulePropTag>() != null || c.name.StartsWith("Kit_", StringComparison.Ordinal) ? "prop" : "wall";
                    }
                }
                chase.sightBreaks.Add((autoPlayClock - autoBaseReleaseAt).ToString("0.00", CultureInfo.InvariantCulture) + " " + by + " " + map.CellOf(playerRoot.position) + " from " + map.CellOf(relay.Position));
            }
        }
        autoSawLast = sees;
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

    // ---------- Working a door (the closed-zone bot opens and shuts, the door-spammer toggles) ----------

    FrontRoomsMapWorld.Door autoBotDoor;
    GridCoord autoBotDoorNear, autoBotDoorFar;
    Vector3 autoBotDoorStand;
    float autoBotDoorClock, autoBotDoorScanAt, autoBotSpamAt, autoBotSpamSeconds, autoBotDoorWork;
    bool autoBotDoorArrived;
    readonly HashSet<long> autoBotDoorsSkipped = new HashSet<long>(), autoBotSpamDoorsUsed = new HashSet<long>();
    int autoBotSpamBroken;

    /// <summary>Go to work a door: stand 1.3 m back from its line in the near cell, on the opening's centre, clear of the leaf.</summary>
    void AutoBotTakeDoor(FrontRoomsMapWorld.Door door, GridCoord near, GridCoord far, GridCoord here)
    {
        autoBotDoor = door;
        autoBotDoorNear = near;
        autoBotDoorFar = far;
        autoBotDoorClock = 0f;
        autoBotDoorWork = 0f;
        autoBotDoorArrived = false;
        var into = map.CellCenter(far) - map.CellCenter(near);
        into.y = 0f;
        var stand = map.CrossingPoint(near, far) - into.normalized * 1.3f;
        stand.y = playerRoot.position.y;
        autoBotDoorStand = stand;
        AutoRouteTo(here, near);
    }

    /// <summary>
    /// Walk to the door's stand and face it. True once there (or, after 6 s, wherever it got to, if the door is
    /// within E's reach). A door it cannot reach in 20 s of walking is skipped for good. The clock stops once it
    /// has got there: working the door is the job, not a failure to reach it.
    /// </summary>
    bool AutoBotGoToDoor(float dt, GridCoord here, out Vector2 local)
    {
        local = Vector2.zero;
        var door = autoBotDoor;
        if (!autoBotDoorArrived)
        {
            autoBotDoorClock += dt;
            if (autoBotDoorClock > 20f)
            {
                AutoBotNote("door " + autoBotDoorNear + "-" + autoBotDoorFar + ": not reached in 20 s, skipped");
                autoBotDoorsSkipped.Add(door.edge);
                autoBotDoor = null;
                autoRouteIndex = autoRoute.Count;
                return false;
            }
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
        if (inReach) autoBotDoorArrived = true;
        return inReach;
    }

    // The closed-zone bot: its zone is chosen once (AutoPickClosedZone); while fewer than 2 of its border doors
    // stand open it opens shut ones, then it shuts every open one.
    bool autoBotZonePicked, autoBotZoneOpening = true;
    // The part of the zone it works: the cells it can walk to from its entry without leaving the zone (a zone can
    // come in pieces that only meet through other zones).
    readonly HashSet<GridCoord> autoBotZoneCells = new HashSet<GridCoord>();
    GridCoord autoBotZoneEntry;

    /// <summary>
    /// The closed-zone bot: walks into its chosen zone; every 2 s it looks for the next border door to work
    /// (to open while the zone has fewer than 2 open, then to shut); with none left, it sprints round inside.
    /// </summary>
    void AutoBotClosedZone(float dt, GridCoord here, out Vector2 local, out bool sprint)
    {
        local = Vector2.zero;
        sprint = false;
        if (!autoBotZonePicked)
        {
            autoBotZonePicked = true;
            AutoPickClosedZone(here);
        }
        if (autoBotDoor == null && autoBotZoneCells.Count > 0 && !autoBotZoneCells.Contains(here))
        {
            // On its way in: to the entry of the part of the zone it chose.
            if (autoRouteIndex >= autoRoute.Count)
            {
                AutoBfs(here, 60, null);
                AutoRouteTo(here, autoBotZoneEntry);
                if (autoRoute.Count == 0) AutopilotPlan(here);
            }
            AutoWalkRoute(dt, here, out local);
            return;
        }
        if (autoBotDoor == null && autoPlayClock >= autoBotDoorScanAt)
        {
            autoBotDoorScanAt = autoPlayClock + 2f;
            AutoFindZoneDoor(here);
        }
        var door = autoBotDoor;
        if (door != null)
        {
            // Done with it once it is as wanted (or broken, or its chunk rebuilt).
            var done = autoBotZoneOpening ? door.open : !door.open;
            if (door.broken || done || map.DoorBetween(autoBotDoorNear, autoBotDoorFar) != door)
            {
                autoBotDoor = null;
                autoRouteIndex = autoRoute.Count;
                return;
            }
            if (!AutoBotGoToDoor(dt, here, out local)) return;
            // At the door: a door that will not open (or shut) in 5 s of trying is skipped for good.
            autoBotDoorWork += dt;
            if (autoBotDoorWork > 5f)
            {
                AutoBotNote("closedzone: door " + autoBotDoorNear + "-" + autoBotDoorFar + " would not " + (autoBotZoneOpening ? "open" : "shut") + " in 5 s, skipped");
                autoBotDoorsSkipped.Add(door.edge);
                autoBotDoor = null;
                autoRouteIndex = autoRoute.Count;
                return;
            }
            if (autoBotZoneOpening)
            {
                if (!map.TryOpenDoor(autoBotDoorNear, autoBotDoorFar)) return;
                autoDoorsOpened++;
                autoBotZoneDoorsOpened++;
            }
            else
            {
                map.Use(door.leaf);
                if (door.open) return;
                autoBotDoorsShut++;
            }
            autoBotDoor = null;
            autoRouteIndex = autoRoute.Count;
            return;
        }
        AutoBotRoam(dt, here, true, out local);
        sprint = true;
    }

    /// <summary>
    /// The closed-zone bot's zone: of the zones it can walk to (40 steps), the nearest low or standard one with at
    /// least 2 unbroken border doors on the part of it reachable from its entry without leaving it (doors stand only
    /// where the ceiling height changes; tall zones have windows). None: the zone it stands in, noted (its fidelity
    /// check then fails unless it shuts a door anyway).
    /// </summary>
    void AutoPickClosedZone(GridCoord here)
    {
        AutoBfs(here, 40, null);
        // Each low or standard zone it can walk to, at the first cell it reaches of it (its entry).
        var entries = new List<GridCoord>();
        var zones = new List<GridCoord>();
        foreach (var cell in autoReached)
        {
            var zone = map.ZoneOf(cell);
            if (zone.height == ZoneHeight.Tall || zones.Contains(zone.id)) continue;
            zones.Add(zone.id);
            entries.Add(cell);
            if (zones.Count >= 8) break;
        }
        // Border doors are counted only from the cells reachable from the entry inside the zone.
        for (var z = 0; z < zones.Count; z++)
        {
            var id = zones[z];
            AutoBfs(entries[z], 40, c => map.ZoneOf(c).id == id);
            var doors = new HashSet<long>();
            foreach (var cell in autoReached)
                foreach (var step in AutoSteps)
                {
                    var n = cell + step;
                    if (!map.IsBuilt(n) || map.Cache.Edge(cell, n) != EdgeKind.Door || map.ZoneOf(n).id == id) continue;
                    var door = map.DoorBetween(cell, n);
                    if (door != null && !door.broken) doors.Add(door.edge);
                }
            if (doors.Count < 2) continue;
            autoBotZone = id;
            autoBotZoneSet = true;
            autoBotZoneEntry = entries[z];
            autoBotZoneCells.Clear();
            foreach (var cell in autoReached) autoBotZoneCells.Add(cell);
            AutoBotNote("closedzone: chose zone " + id + " (" + doors.Count + " border doors in its " + autoReached.Count + " cells from entry " + entries[z] + ")");
            autoRouteIndex = autoRoute.Count;
            return;
        }
        AutoBotNote("closedzone: no low or standard zone with 2 border doors within 40 steps; staying in zone " + autoBotZone);
    }

    /// <summary>The closed-zone bot's next door on its zone's border, nearest by walking inside the zone: a shut one while it is opening, an open one after.</summary>
    void AutoFindZoneDoor(GridCoord here)
    {
        AutoBfs(here, 40, InBotZone);
        int doors = 0, open = 0;
        FrontRoomsMapWorld.Door pickOpen = null, pickShut = null;
        GridCoord nearOpen = default, farOpen = default, nearShut = default, farShut = default;
        var seen = new HashSet<long>();
        foreach (var cell in autoReached)
            foreach (var step in AutoSteps)
            {
                var n = cell + step;
                if (InBotZone(n) || !map.IsBuilt(n) || map.Cache.Edge(cell, n) != EdgeKind.Door) continue;
                var door = map.DoorBetween(cell, n);
                if (door == null || door.broken || !seen.Add(door.edge)) continue;
                doors++;
                if (door.open) open++;
                if (autoBotDoorsSkipped.Contains(door.edge)) continue;
                if (door.open && pickOpen == null)
                {
                    pickOpen = door;
                    nearOpen = cell;
                    farOpen = n;
                }
                else if (!door.open && pickShut == null)
                {
                    pickShut = door;
                    nearShut = cell;
                    farShut = n;
                }
            }
        if (autoBotZoneDoors < 0)
        {
            autoBotZoneDoors = doors;
            autoBotZoneDoorsOpen = open;
            AutoBotNote("closedzone: zone " + autoBotZone + ", " + autoReached.Count + " cells reached, " + doors + " border doors, " + open + " open");
        }
        // Open shut doors until 2 stand open (or none is left to open), then shut them all.
        if (autoBotZoneOpening && (open >= 2 || pickShut == null)) autoBotZoneOpening = false;
        if (autoBotZoneOpening) AutoBotTakeDoor(pickShut, nearShut, farShut, here);
        else if (pickOpen != null) AutoBotTakeDoor(pickOpen, nearOpen, farOpen, here);
    }

    /// <summary>
    /// The door-spammer: one unbroken door away from the start door (both its cells 3 or more steps from the
    /// start door's cell, outside the start area, 6 m or more from the start door), reached without passing
    /// the start door's cell; it stands at it and opens or shuts it every 0.8 s. Only a door the Relay breaks
    /// sends it to another.
    /// </summary>
    void AutoBotDoorSpam(float dt, GridCoord here, out Vector2 local)
    {
        local = Vector2.zero;
        var door = autoBotDoor;
        if (door == null || door.broken || map.DoorBetween(autoBotDoorNear, autoBotDoorFar) != door)
        {
            if (door != null)
            {
                AutoBotNote("doorspammer: door " + autoBotDoorNear + "-" + autoBotDoorFar + (door.broken ? " broken by the Relay" : " rebuilt"));
                if (door.broken) autoBotSpamBroken++;
            }
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
        if (!AutoBotGoToDoor(dt, here, out local)) return;
        autoBotSpamSeconds += dt;
        if (autoPlayClock < autoBotSpamAt) return;
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

    static int AutoSteps1(GridCoord a, GridCoord b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

    void AutoFindSpamDoor(GridCoord here)
    {
        AutoBfs(here, 30, c => c != startDoorCell && !map.InStartArea(c));
        foreach (var cell in autoReached)
            foreach (var step in AutoSteps)
            {
                var n = cell + step;
                if (!map.IsBuilt(n) || map.Cache.Edge(cell, n) != EdgeKind.Door) continue;
                if (map.InStartArea(cell) || map.InStartArea(n) || AutoSteps1(cell, startDoorCell) < 3 || AutoSteps1(n, startDoorCell) < 3) continue;
                var door = map.DoorBetween(cell, n);
                if (door == null || door.broken || autoBotDoorsSkipped.Contains(door.edge) || Flat(door.position - startDoorPoint).magnitude < 6f) continue;
                AutoBotTakeDoor(door, cell, n, here);
                autoBotSpamDoorsUsed.Add(door.edge);
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
            minutes = autoBotBudget / 60f,
            tierLocked = autoTierLock,
            playSeconds = autoPlayClock,
            endedBy = reason,
            caught = phase == Phase.Caught,
            startHoldEndedAt = autoBotLeftAt,
            // The hold ends on the first frame at or past AutoStartHoldSeconds when nothing else held it.
            startOnFixedFrame = autoBotLeftAt >= 0f && autoBotLeftAt < AutoStartHoldSeconds + .02f,
            releasedAt = autoBaseReleaseAt,
            releaseStraightMetres = autoReleaseStraight,
            releaseWalkMetres = autoReleaseWalk,
            releaseF = autoReleaseF,
            releaseW = autoReleaseW,
            secondsAfterRelease = since,
            firstHuntAfterRelease = autoFirstHunt,
            firstChaseAfterRelease = autoFirstChase,
            catches = autoCatches,
            wanderEncounters = autoWanderEncounters,
            huntResumes = autoHuntResumes,
            huntLog = autoHuntLog,
            calls = autoCallsLogged,
            callLog = autoCallLog,
            chaseLog = autoChaseLog,
            relayLog = autoRelayLog,
            glassBroken = autoGlassBroken,
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
        for (var t = 1; t < autoTierSeconds.Length; t++)
            if (autoTierSeconds[t] > 0f) r.tierSeconds.Add(new BaselineStateTime { state = "T" + t, seconds = autoTierSeconds[t], share = since > 0f ? autoTierSeconds[t] / since : 0f });
        foreach (var n in autoHuntsBy) r.hunts += n;
        r.huntsPerMinute = r.hunts * perMinute;
        r.huntsByCause = new BaselineHuntCauses
        {
            sprint = autoHuntsBy[AutoCauseSprint], door = autoHuntsBy[AutoCauseDoor], glass = autoHuntsBy[AutoCauseGlass],
            otherNoise = autoHuntsBy[AutoCauseOther], chaseLost = autoHuntsBy[AutoCauseChaseLost], other = autoHuntsBy[AutoCauseUnknown],
        };
        r.huntRetargets = AutoKinds(autoHuntRetargets);
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
            else
            {
                r.relaysA7b++;
                if (relayed.byPlayerPosition) r.relaysA7bByPlayer++;
            }
        }
        r.fUnreachedShare = autoOnMapSamples > 0 ? autoFUnreached / (float)autoOnMapSamples : 0f;
        r.fBands = AutoBands(autoBandF);
        r.wBands = AutoBands(autoBandW);
        r.straightBands = AutoBands(autoBandStraight);
        r.stages = AutoStages(autoStageTrace, autoStage1Entries, since, r.caught);
        r.hunterStages = AutoStages(autoHunterStageTrace, autoHunterStage1Entries, since, r.caught);
        r.stageTrace = AutoTrace(autoStageTrace);
        r.hunterStageTrace = AutoTrace(autoHunterStageTrace);
        r.bot = new BaselineBot
        {
            distanceWalked = autoDistance, cellsVisited = autoCells.Count, zonesVisited = zonesVisited.Count, routes = autoRoutes,
            sprintBursts = autoBotSprintBursts, sprintSeconds = autoBotSprintSeconds, windedSeconds = autoBotWindedSeconds,
            sprintNoises = autoNoiseEmitted[AutoCauseSprint], sprintNoisesNotSprinting = autoBotSprintNoiseNotSprinting,
            doorsOpened = autoDoorsOpened, doorsShut = autoBotDoorsShut, doorToggles = autoBotDoorToggles,
            windowsTried = autoBotWindowsTried, windowsBroken = autoBotWindowsBroken,
            evasionsAttempted = autoBotEvasions, evasionsSucceeded = autoBotEvadeOk, evasionsCaught = autoBotEvadeCaught,
            zoneChosen = autoBot == AutoBot.ClosedZone ? autoBotZone.ToString() : "",
            zoneDoors = autoBotZoneDoors, zoneDoorsOpenAtStart = autoBotZoneDoorsOpen, zoneDoorsOpened = autoBotZoneDoorsOpened,
            spamDoors = autoBotSpamDoorsUsed.Count, spamDoorsBroken = autoBotSpamBroken, spamSeconds = autoBotSpamSeconds,
            spamTogglesPerMinute = autoBotSpamSeconds > 0f ? autoBotDoorToggles * 60f / autoBotSpamSeconds : 0f,
            edgeStalls = autoEdgeStalls, northCells = autoBotNorth,
            staminaBarSeconds = AutoV2Conditions ? AutoV2StaminaSeconds : StaminaSeconds,
            chaseCap = AutoV2Conditions ? AutoV2ChaseCap : -1f, chaseSpeedMax = autoChaseSpeedMax,
            notes = new List<string>(autoBotNotes),
        };
        AutoBaseModeChecks(r);
        r.modeChecks = new List<BaselineModeCheck>(autoModeChecks);
        var failed = new List<string>();
        foreach (var check in autoModeChecks) if (!check.ok && !check.soft) failed.Add(check.check);
        var ok = mapPlay && relay != null && relay.Released && autoErrors == 0 && failed.Count == 0;
        r.verdict = (ok ? "PASS" : "FAIL") + " · baseline " + r.mode + " T" + autoBotTier + " seed " + runSeed + " · ended by " + reason
            + (failed.Count > 0 ? " · checks failed: " + string.Join(", ", failed) : "");
        // The hash covers the behaviour only: build and perf are still their blank defaults here.
        r.behaviourHash = AutoHash(JsonUtility.ToJson(r));
        r.build = autoBuild ?? new BaselineBuild();
        r.perf = AutoBasePerf();
        var dir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Verification", "relay-baseline");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, runSeed + "-" + r.mode + "-T" + autoBotTier + ".json");
        File.WriteAllText(path, JsonUtility.ToJson(r, true));
        Log("BASELINE " + r.verdict + " · " + path);
        return r.verdict;
    }

    void AutoCheck(string check, bool ok, string detail, bool soft = false) => autoModeChecks.Add(new BaselineModeCheck { check = check, ok = ok, detail = detail, soft = soft });

    /// <summary>
    /// Did the run measure what it claims to: every hunt has a cause, a sprinting bot's sprint noise was counted,
    /// the tier held, and each mode did its job. Any failed check fails the run.
    /// </summary>
    void AutoBaseModeChecks(BaselineReport r)
    {
        autoModeChecks.Clear();
        AutoCheck("huntCauseKnown", autoHuntsBy[AutoCauseUnknown] == 0, autoHuntsBy[AutoCauseUnknown] + " hunts with no known cause");
        // Soft (reported, never fails the run): the §9.1 sampler and the hunter's WarnStage should agree on about
        // 95 % of on-map samples once the hunter runs the same rule (it rebuilds W per cell change, this samples at 4 Hz).
        var same = 0;
        var n = Mathf.Min(autoStageTrace.Count, autoHunterStageTrace.Count);
        for (var i = 0; i < n; i++) if (autoStageTrace[i] == autoHunterStageTrace[i]) same++;
        var agree = n > 0 ? same / (float)n : 1f;
        AutoCheck("stagesAgree", agree >= .95f, (agree * 100f).ToString("0.0", CultureInfo.InvariantCulture) + " % of " + n + " on-map samples (§9.1 sampler vs hunter WarnStage)", true);
        if (autoTierLock) AutoCheck("tierLocked", r.tierFinal == autoBotTier, "tier " + autoBotTier + " → " + r.tierFinal + (r.tierFinal != autoBotTier ? " (the RaiseTier guard is missing)" : ""));
        // Sprinting makes noise on every step: a run that sprinted with none counted lost them on the way.
        if (autoBotSprintSeconds > .5f)
            AutoCheck("sprintNoiseCounted", r.bot.sprintNoises > 0, r.bot.sprintNoises + " sprint noises in " + autoBotSprintSeconds.ToString("0.0", CultureInfo.InvariantCulture) + " s of sprint");
        if (autoBot == AutoBot.Noisy || autoBot == AutoBot.ShiftHolder)
            AutoCheck("sprinted", autoBotSprintSeconds > .5f, autoBotSprintSeconds.ToString("0.0", CultureInfo.InvariantCulture) + " s of sprint, " + autoBotSprintBursts + " bursts");
        switch (autoBot)
        {
            case AutoBot.Noisy:
                AutoCheck("windowBroken", autoBotWindowsBroken > 0, autoBotWindowsTried + " tried, " + autoBotWindowsBroken + " broken, " + autoBotWindowRoutes + " routes to a window");
                break;
            case AutoBot.ClosedZone:
                AutoCheck("zoneDoorShut", autoBotDoorsShut > 0, "zone " + autoBotZone + ": " + autoBotZoneDoors + " border doors, " + autoBotZoneDoorsOpen + " open at first look, " + autoBotZoneDoorsOpened + " opened, " + autoBotDoorsShut + " shut");
                break;
            case AutoBot.DoorSpammer:
            {
                var rate = r.bot.spamTogglesPerMinute;
                var moved = autoBotSpamDoorsUsed.Count - 1 - autoBotSpamBroken;
                AutoCheck("oneDoor", autoBotSpamDoorsUsed.Count >= 1 && moved <= 0, autoBotSpamDoorsUsed.Count + " doors used, " + autoBotSpamBroken + " broken by the Relay");
                // 0.8 s a toggle is 75 a minute; under 30 means it was not working its door.
                AutoCheck("toggleRate", autoBotDoorToggles > 0 && (autoBotSpamSeconds < 20f || rate >= 30f), autoBotDoorToggles + " toggles in " + autoBotSpamSeconds.ToString("0.0", CultureInfo.InvariantCulture) + " s at a door (" + rate.ToString("0", CultureInfo.InvariantCulture) + " a minute)");
                break;
            }
            case AutoBot.EdgeRunner:
                AutoCheck("edgeReached", autoEdgeStalls > 0 || r.relaysA7bByPlayer > 0, autoEdgeStalls + " edge stalls, " + r.relaysA7bByPlayer + " of " + r.relaysA7b + " A7b by the player's position, " + autoBotNorth + " cells north");
                break;
            case AutoBot.Evader03:
            case AutoBot.Evader06:
            case AutoBot.EvaderV2:
            {
                var long_ = 0;
                foreach (var chase in autoChaseLog) if (chase.seconds > (autoBot == AutoBot.Evader03 ? .3f : .6f) + .1f) long_++;
                AutoCheck("evaded", long_ == 0 || autoBotEvasions > 0, autoBotEvasions + " evasions in " + long_ + " chases that outlasted the reaction");
                if (AutoV2Conditions) AutoCheck("chaseCapped", autoChaseSpeedMax <= AutoV2ChaseCap + 1e-3f, "fastest chase speed " + autoChaseSpeedMax.ToString("0.00", CultureInfo.InvariantCulture) + " m/s, cap " + AutoV2ChaseCap);
                break;
            }
        }
    }

    static string AutoTrace(List<byte> trace)
    {
        var text = new System.Text.StringBuilder(trace.Count);
        foreach (var b in trace) text.Append((char)('0' + Mathf.Clamp(b, 0, 9)));
        return text.ToString();
    }

    /// <summary>Stage shares (whole, and without the last 5 s before a catch) and stage-1 entries per 10 min.</summary>
    static BaselineStages AutoStages(List<byte> trace, int entries, float since, bool caught)
    {
        var s = new BaselineStages { stage1Entries = entries, stage1EntriesPer10Min = since > 0f ? entries * 600f / since : 0f };
        var counts = new int[3];
        foreach (var b in trace) counts[Mathf.Clamp(b, 0, 2)]++;
        if (trace.Count > 0)
        {
            s.stage0 = counts[0] / (float)trace.Count;
            s.stage1 = counts[1] / (float)trace.Count;
            s.stage2 = counts[2] / (float)trace.Count;
        }
        var keep = caught ? Mathf.Max(0, trace.Count - Mathf.RoundToInt(AutoCensorSeconds / AutoSampleSeconds)) : trace.Count;
        if (keep > 0)
        {
            var c = new int[3];
            for (var i = 0; i < keep; i++) c[Mathf.Clamp(trace[i], 0, 2)]++;
            s.censoredStage0 = c[0] / (float)keep;
            s.censoredStage1 = c[1] / (float)keep;
            s.censoredStage2 = c[2] / (float)keep;
        }
        return s;
    }

    BaselinePerf AutoBasePerf()
    {
        return new BaselinePerf
        {
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
}
#endif
