using System;
using System.Collections.Generic;
using FrontRooms.Map;
using UnityEngine;

/// <summary>
/// The Relay on the generated map. It keeps the Listen → Hunt → Search →
/// Chase → BreakDoor states of the stream hunter, re-expressed for an open
/// grid, and adds Wander:
/// - it is released after a short grace period, in a built cell 9–15 cells of
///   walking away that the player cannot see, preferably behind them;
/// - it does not know where the player is: between noises it wanders to
///   random reachable places (Listen → Wander), keeping to shut doors;
/// - it chases only once it sees the player (a ray at eye height). A noise it
///   hears it walks to and searches (Hunt), without running;
/// - losing sight in a chase, it goes where it last saw the player; if it saw
///   them go through a door in its last second of sight, it follows into the
///   room behind that door. Either way it then searches that room (a few spots,
///   listening at each) and gives up, unless it hears or sees them again;
/// - it paths through built cells with a breadth-first search, breaks shut
///   doors on a hunt or a chase, and cannot pass unbroken glass. A wander or
///   a search never breaks a door: one shut across its way it walks round,
///   and with no way round that leg is over;
/// - if the chase leaves it too far behind, it relays itself closer, unseen;
/// - it has a body (<see cref="ModuleUnits.RelayRadius"/>): it walks straight
///   while the way is clear and plans a detour on a 0.25 m grid around the
///   cell it is in and the next one when furniture, a column or an open door
///   leaf is in the way. With no way round furniture it passes through it,
///   still on a route that keeps out of walls;
/// - on floor 1 with a station (A26 M7) the game sends it at its own gate with
///   ReleaseNow: 8–10 cells away on the far side of the walk to the EXIT when it
///   can, never in the station, its ring or within 2 cells of that walk; its
///   first leg hunts to the station's key once that is taken; and no Arrive
///   ever puts it in the station or its ring. The gate is the onboarding's
///   (Red 2026-10-30): 150 cells of the floor explored or 180 s, never the key;
/// - the run's first send is gentle wherever it comes (floor 1 after the gate,
///   or the first floor below it): that ladder, from the player, behind them
///   with no station's layout (FirstSendPolicy.Generic); and it starts no chase
///   before the first lamp dip unless the player is within 6 m (ChaseGrace,
///   which the game holds for that send only);
/// - the player's side of sight (RELAY §8 amended 2026-10-14; V2, 2026-10-29): three named tests on RelayInView
///   (PlayerSeesRelay on a reference frustum, PlayerPerceivesRelay with RelayLit, RelayOnScreen on a device camera),
///   the one Threat reading camera shots cancel on, and the UI prompt gate ThreatPerceivable with its holds.
/// Positions are world space; the caller drives the rig.
/// Relay v2's director loop (RELAY 4, 5, 11; step V3a) is in FrontRoomsMapHunter.V2.cs and compiles only with
/// FRONTROOMS_RELAY_V2: Arm, Dispatch, SendAway, Arrive v2, the give-up beat, patrol and Withdraw. Without the
/// define (main's default) this file behaves exactly as before; the hooks into it below are all inside #if.
/// </summary>
public sealed partial class FrontRoomsMapHunter
{
    const float EyeHeight = ModuleUnits.RelayEye;
    // The body as it is tested: between knee and head height, so it fits a
    // broken window (sill 0.35, head 2.0) and a door (head 2.1).
    const float Radius = ModuleUnits.RelayRadius, ProbeBottom = .4f, ProbeTop = 1.95f;
    const float NodeStep = .25f, StraightRecheck = .25f;
    const int NodesPerCell = 12, MaxRegionCells = 4, MaxRegionNodes = NodesPerCell * NodesPerCell * MaxRegionCells * MaxRegionCells;
    const float BlowInterval = FrontRoomsShotTimings.DoorBreak.BlowInterval;
    // The last blow lands this long before the door gives, so the rig's break-through snap reads as the impact.
    const float FinalBlowLead = FrontRoomsShotTimings.DoorBreak.FinalBlowLead;
    // StateTime is summed frame by frame: 60 frames of 1/60 s fall just short of 1 s. Moments are met this early.
    const float TimeSlack = 1e-4f;
    // The rig's door squeeze: 1 on the door line, 0 from this far out.
    const float SqueezeReach = .6f;
    // After the last blow it waits for the leaf to fall open: the throw past the stop and the bounce to rest.
    const float DoorFallSeconds = FrontRoomsShotTimings.DoorBreak.ThrowSeconds + FrontRoomsMapWorld.DoorBreakBounceSeconds;
    // It shoulders a shut door this far in front of the opening (A12: the leaf bursts away from it); only for a leaf caught partly open on its side it stands beside the latch, out of the sweep.
    const float BreakFaceDistance = .45f;
    // L11: the stop at the stairwell's door, the walk off that ends the beat, and the most the beat may hold the seal.
    const float SafeDoorStopSeconds = 1.2f, SafeDoorLeaveMetres = 4f, SafeDoorBeatMaxSeconds = 8f;
    // C22: a noise within a leaf's thickness of the EXIT door's line is the door's own (DoorMoved fires at the
    // opening's centre); one further into X was made inside the stairwell (Noise).
    const float ExitLineSlack = ModuleUnits.DoorLeafThickness;
    const float ReplanSeconds = .35f;
    const float LeashCheckSeconds = 1f;
    const int SpawnMinCells = 9, SpawnMaxCells = 15, LeashCells = 30;
    // Wandering: a random reachable spot this many cells of walking away, at this share of hunt speed.
    const int WanderMinCells = 6, WanderMaxCells = 14;
    const float WanderPace = .8f;
    // Searching a room: up to this many spots, a pause to listen at each, at this share of hunt speed.
    const int SweepSpots = 3;
    // The listening pause and the give-up time are tuning (searchLookSeconds, searchMaxSeconds), so tiers can stretch them.
    const float SearchPace = .7f;
    // The long-range relay only once it has neither seen nor heard the player for this long.
    const float LeashQuietSeconds = 45f;
    // Losing sight, it follows through a door it saw the player go through at most this long before it last saw them.
    const float SeenDoorSeconds = 1f;
    // Step 1 warning bands. Holds are deliberately data-only: the caller can
    // decide how a stage is presented without the level field driving lamps or audio.
    // §9.1: enter at W ≤ 30 / 18 m, leave past 36 / 24 m; a stage lasts at least 4 / 3 s from entry, and ends only once W
    // has stayed past its exit band for 1 s (cell jitter is about ±3 m).
    const float WarnStage1Enter = 30f, WarnStage1Exit = 36f, WarnStage1MinSeconds = 4f;
    const float WarnStage2Enter = 18f, WarnStage2Exit = 24f, WarnStage2MinSeconds = 3f;
    const float WarnExitConfirmSeconds = 1f;
    const float PlayerFieldCap = 45f;
    const float SourceFieldCap = 90f;
    // RELAY §8 (amended 2026-10-14; V2, 2026-10-29): the three named sight tests share these parts. RelayInView needs
    // both of the Relay's points, each on its own ray, refreshed at 10 Hz.
    const float PlayerSeesRelayPeriod = .1f;
    /// <summary>RELAY §8: the Relay's two points over its feet (eye and head), both of which RelayInView needs.</summary>
    public const float RelayLowPoint = 1.6f, RelayHighPoint = 1.95f;
    /// <summary>
    /// RELAY §8's RelayLit: XZ within 16 m of the player (lamps fade from 13 m and are off past 16 m, A10) and the lamp
    /// level W = min(1, L + 0.02 ΣL_n) over its cell and 4 neighbours at least 0.15 (ESTIMATE, computer_ui 20_design §6.1).
    /// </summary>
    public const float RelayLitMetres = 16f, RelayLitLevel = .15f, LampNeighbourShare = .02f;
    /// <summary>
    /// ThreatPerceivable's holds and ranges (20_design §6.1, v1): the chase bed and heartbeat fade over 3 s after a chase;
    /// a blow is heard within 36 m straight (the FMOD event max and the caption radius), the break within 40 m.
    /// </summary>
    public const float ChaseFadeSeconds = 3f, BlowAudibleMetres = 36f, BreakAudibleMetres = 40f;

    static readonly GridCoord[] Steps = { new GridCoord(1, 0), new GridCoord(-1, 0), new GridCoord(0, 1), new GridCoord(0, -1) };

    readonly FrontRoomsMapWorld world;
    readonly FrontRoomsHunterTuning tuning;
    readonly Collider playerCollider;
    readonly Transform rig;
    readonly List<GridCoord> path = new List<GridCoord>();
    readonly Dictionary<GridCoord, GridCoord> cameFrom = new Dictionary<GridCoord, GridCoord>();
    readonly Dictionary<GridCoord, int> depth = new Dictionary<GridCoord, int>();
    readonly Queue<GridCoord> frontier = new Queue<GridCoord>();
    readonly List<GridCoord> candidates = new List<GridCoord>();
    // Arrive's scratch: the map's Relay entries (module markers), and the ones that pass, behind the player or anywhere.
    readonly List<(Vector3 pos, string tag)> entries = new List<(Vector3, string)>();
    readonly List<int> entriesBehind = new List<int>(), entriesAny = new List<int>();
    // A26 M7: the first send's rungs 1-3 (FirstSend).
    readonly List<GridCoord> rungFar = new List<GridCoord>(), rungNear = new List<GridCoord>(), rungWide = new List<GridCoord>();
    // A40: the player's route down the FLOW field to its target, for the generic ladder (FirstSend, FlowRoute).
    readonly List<GridCoord> flowRoute = new List<GridCoord>();
    readonly RaycastHit[] hits = new RaycastHit[16];
    readonly RaycastHit[] bodyHits = new RaycastHit[64];
    readonly Collider[] overlaps = new Collider[16];
    // Detour planning scratch: a region of at most 4 x 4 cells at 0.25 m.
    readonly List<Vector3> detour = new List<Vector3>();
    readonly List<int> nodePath = new List<int>();
    readonly List<int> heapNode = new List<int>();
    readonly List<float> heapKey = new List<float>();
    readonly sbyte[] nodeFree = new sbyte[MaxRegionNodes];
    readonly float[] nodeCost = new float[MaxRegionNodes];
    readonly int[] nodeParent = new int[MaxRegionNodes];
    readonly bool[] nodeClosed = new bool[MaxRegionNodes];
    Vector3 position, lastSeen, goal;
    int pathIndex;
    // Whether the current plan may go through shut doors (a hunt or a chase breaks them). Its replans keep the same rule.
    bool planThroughDoors = true;
    float releaseTimer, lostTime, replanTimer, leashTimer;
    int blowsStruck;
    // What it knows: when it last saw or heard the player, and the last door it saw the player go through.
    float clock, lastSeenTime = float.MinValue, lastContactTime;
    // The player's cell when it last saw them, and whether that was on the tick before this one:
    // a crossing counts only between two ticks in a row that saw them, so it never learns where they went unseen.
    GridCoord seenCell, doorInto;
    bool sawLastTick;
    float doorTime = float.MinValue;
    // The room it is searching: spots to look from, in order.
    readonly List<GridCoord> sweep = new List<GridCoord>();
    int sweepIndex, sweepPlanned = -1;
    float lookTimer, searchTime;
    // Steering: the aim the current plan was made for, the detour toward it
    // (empty = straight), and whether the detour ends short of the aim.
    Vector3 planTarget = new Vector3(float.MaxValue, 0f, 0f);
    int detourIndex;
    bool detourShort, unreachable, ghosting, ghostTouched;
    // Hunt watchdog: where it last made progress, and for how long it has not.
    Vector3 progressAnchor;
    float progressTime;
    const float StallSeconds = 3f;
    // The cell a crossing leg started from: a detour may cut through a
    // neighbouring cell, and that must not count as leaving the route.
    GridCoord legFrom, legTo;
    bool hasLeg;
    float straightCheck;
    FrontRoomsMapWorld.Door breakingDoor;
    // Where it faces while it breaks: the door; the lock for a leaf caught partly open on its side.
    Vector3 breakFacing;
    HunterState resumeState;
    bool caught;
    uint rng;
    readonly FrontRoomsMapPathField playerField = new FrontRoomsMapPathField(PlayerFieldCap);
    readonly FrontRoomsMapPathField sourceField = new FrontRoomsMapPathField(SourceFieldCap);
    readonly FrontRoomsMapPathField relayField = new FrontRoomsMapPathField(PlayerFieldCap);
    GridCoord playerFieldRoot;
    int playerFieldRevision = -1;
    bool playerFieldReady;
    GridCoord relayFieldRoot;
    int relayFieldRevision = -1;
    bool relayFieldReady;
    // Time in the current stage, and how long W has stayed past its exit band and past stage 1's (for 2 → 0).
    float warnStageSeconds, warnPastExit, warnPastClear;
    float playerSeesRelayTimer;
    // V2 sight: the player's inputs at the last tick (feet, base eye, its look), the 10 Hz refresh count that keys
    // RelayOnScreen's cache, and that cache (the camera, its shape when tested, the answer).
    Vector3 sightFeet, sightEye, sightForward = Vector3.forward;
    int sightTick;
    Camera onScreenCamera;
    int onScreenTick = -1;
    float onScreenFov, onScreenAspect;
    bool onScreen;
    // ThreatPerceivable's holds, on its own clock: when the last chase ended, the last blow and the break the player
    // heard; the Spotting hold, and whether the Relay has stood still since it began.
    float chaseEndedAt = float.NegativeInfinity, blowHeardAt = float.NegativeInfinity, breakHeardAt = float.NegativeInfinity;
    bool spotHeld, spotStill;

    public HunterState State { get; private set; } = HunterState.Dormant;
    public float StateTime { get; private set; }
    public Vector3 Position => position;
    public Vector3 Heading { get; private set; } = Vector3.forward;
    public bool Moving { get; private set; }
#if FRONTROOMS_RELAY_V2
    /// <summary>RELAY 4 (V3a): an alias of OnMap, so the rig, the door leaf's body, the sound and the assist keep reading it; Away and Arrive (backstage) are not released.</summary>
    public bool Released => OnMap;
    /// <summary>RELAY 4: armed = not Dormant (Away, Arrive or on the map). The autopilot reads it.</summary>
    public bool Armed => State != HunterState.Dormant;
    /// <summary>RELAY 4: Listen, Wander (patrol), Hunt, Search, Chase, BreakDoor or Withdraw.</summary>
    public bool OnMap => IsOnMap(State);
    /// <summary>RELAY 5 (plan 1.1 item 8): true under FRONTROOMS_RELAY_V2: Arrived fires at the step-in, Arrive and Away are silent through Released.</summary>
    public static bool UsesArrive => true;
#else
    public bool Released => State != HunterState.Dormant;
    /// <summary>Relay v2's name for "not Dormant" (RELAY 4); on v1 the same as Released. Read-only: nothing changes with it.</summary>
    public bool Armed => Released;
    /// <summary>Relay v2's name for "on the map" (RELAY 4); on v1 the same as Released (v1 never enters Away, Arrive or Withdraw).</summary>
    public bool OnMap => Released;
    /// <summary>RELAY 5: false on v1 (Arrived fires at placement); true under FRONTROOMS_RELAY_V2.</summary>
    public static bool UsesArrive => false;
#endif
    /// <summary>RELAY 4's on-map states: every state but Dormant, Away and Arrive.</summary>
    public static bool IsOnMap(HunterState state) =>
        state != HunterState.Dormant && state != HunterState.Away && state != HunterState.Arrive;
    public bool SeesPlayer { get; private set; }

    /// <summary>
    /// L11 (tutorial, on Red's safe zone, 2026-10-22): a Relay that chased the player into the stairwell walks to
    /// its stand at the door on N's side, stops there facing it, in sight and earshot, then turns and walks away;
    /// it never steps in and never vanishes, so the player learns the rule by surviving it once. Fired as it stops
    /// (the sound layer's foot scuff, RELAY 9.4 "it stops and turns"), with its feet.
    /// </summary>
    public event Action<Vector3> StoppedAtSafeDoor;

    /// <summary>L11 under way: from the chase's end until it has walked SafeDoorLeaveMetres off (at most SafeDoorBeatMaxSeconds). The seal waits for it, so the steps recede before it parks.</summary>
    public bool SafeDoorBeat => safeDoor != SafeDoorPhase.None;

    enum SafeDoorPhase : byte { None, Approach, Stop, Leave }
    SafeDoorPhase safeDoor;
    float safeDoorTime, safeDoorStopAt;
    Vector3 safeDoorFrom, safeDoorFacing;
    /// <summary>Player-rooted v2 §7 field distance to the Relay, in metres.</summary>
    public float PathDistanceToPlayer { get; private set; } = float.PositiveInfinity;
    /// <summary>Door/window opening cost on the shortest path represented by PathDistanceToPlayer.</summary>
    public float PathOpeningCost { get; private set; } = float.PositiveInfinity;
    /// <summary>Warning distance W = min(F, three times the straight-line 3D distance).</summary>
    public float WarningDistance { get; private set; } = float.PositiveInfinity;
    /// <summary>0, 1 or 2 by §9.1's bands, minimum times and exit confirm.</summary>
    public int WarnStage { get; private set; }
    /// <summary>Seconds stage ≥ 1 has run without a break (a 2 → 1 drop keeps it; a drop to 0 ends it), for the G3 gate.</summary>
    public float WarnRunSeconds { get; private set; }
    /// <summary>Seconds the current stage 2 has run (0 outside stage 2).</summary>
    public float WarnStage2RunSeconds { get; private set; }
    /// <summary>
    /// RELAY §8 (amended 2026-10-14; V2, 2026-10-29), fixed: RelayInView on the reference frustum (vertical 76°, 16:9, the
    /// player's base eye with its real yaw and pitch, out to SightDistance), with no light term, refreshed at 10 Hz. It
    /// never reads a device camera, so bots and every platform meet the same Relay. Read by W2's in-sight steps (the
    /// sound layer), and in v2 by Arrive, Withdraw, FADE and the director. Was a flat ±60° test with no pitch.
    /// </summary>
    public bool PlayerSeesRelay { get; private set; }
    /// <summary>RELAY §8: XZ within RelayLitMetres of the player and LampW over its cell at least RelayLitLevel (map.LampLevel), at 10 Hz.</summary>
    public bool RelayLit { get; private set; }
    /// <summary>RELAY §8: PlayerSeesRelay && RelayLit. Platform-identical; Pressure's sight input in v2 (NotifyPlayerPerceivesRelay).</summary>
    public bool PlayerPerceivesRelay => PlayerSeesRelay && RelayLit;

    /// <summary>
    /// RELAY §8: both Relay points (RelayLowPoint and RelayHighPoint over its feet) project inside <paramref name="frustum"/>
    /// and each is visible from its eye (two rays; any collider but the player and the Relay's own rig blocks). Not cached
    /// itself: the named tests ask it at most once per 10 Hz refresh.
    /// </summary>
    public bool RelayInView(in FrontRoomsRelayFrustum frustum)
    {
        if (!Released || world == null) return false;
        var low = position + Vector3.up * RelayLowPoint;
        var high = position + Vector3.up * RelayHighPoint;
        return frustum.Contains(low) && frustum.Contains(high) && Visible(frustum.Eye, low) && Visible(frustum.Eye, high);
    }

    /// <summary>
    /// RELAY §8: RelayInView on this device's own render camera (its FOV, aspect, clip planes and pose) && RelayLit. Per
    /// device and read only by UI gates (ThreatPerceivable: every EXAMINE prompt, the terminal, the live-mode exit), so a
    /// gate matches what this screen shows. Cached until the next 10 Hz refresh or a change of the camera's FOV or aspect.
    /// </summary>
    public bool RelayOnScreen(Camera camera)
    {
        if (camera == null || !Released || !RelayLit) return false;
        if (camera != onScreenCamera || onScreenTick != sightTick || camera.fieldOfView != onScreenFov || camera.aspect != onScreenAspect)
        {
            onScreenCamera = camera;
            onScreenTick = sightTick;
            onScreenFov = camera.fieldOfView;
            onScreenAspect = camera.aspect;
            onScreen = RelayInView(FrontRoomsRelayFrustum.Of(camera));
        }
        return onScreen;
    }

    /// <summary>
    /// RELAY §8 (V2, 2026-10-29): the one Threat reading camera shots cancel on (FrontRoomsDoorShot.Threatened, the game's
    /// rig.CancelForRelay): "TargetAcquired or Spotting ≥ 0.5, not on any glimpse". v1 stand-ins until V4b swaps the
    /// source (FrontRoomsRelayThreat.V1): a chase is on; it sees the player within 6 m flat. Nothing while not released.
    /// </summary>
    public FrontRoomsRelayThreat Threat => Released ? FrontRoomsRelayThreat.V1(State == HunterState.Chase, SeesPlayer, Flat(position - sightFeet).magnitude) : default;

    /// <summary>
    /// RELAY §9.3 (20_design §6.1, S-T2: one property for the gate and the sound): its steps have gain above 0 — stage 2
    /// with F inside the 24 m exit band (the straight line when no field reaches it), in the player's sight
    /// (PlayerSeesRelay), or its L11 beat at the stairwell door. FrontRoomsRelaySound still works this out itself.
    /// </summary>
    public bool StepsAudible
    {
        get
        {
            if (!Released) return false;
            if (PlayerSeesRelay || SafeDoorBeat) return true;
            var f = float.IsInfinity(PathDistanceToPlayer) ? Vector3.Distance(sightEye, position) : PathDistanceToPlayer;
            return WarnStage >= 2 && f < WarnStage2Exit;
        }
    }

    /// <summary>20_design §6.1: BlowGap, the longest gap between this break's blows (0.5 s; the last up to 0.65 s at some lengths) + 1 s (ESTIMATE).</summary>
    public float BlowGapSeconds => Mathf.Max(BlowInterval, BreakSeconds - FinalBlowLead - BlowInterval * (BlowCount - 1)) + 1f;

    /// <summary>
    /// 20_design §6.1 (v1): a door blow sounded within the last BlowGapSeconds within BlowAudibleMetres of the player's eye,
    /// and no break heard since. The gate's break term and its hold in one: it ends when the door gives within
    /// BreakAudibleMetres, or once no blow has been heard for BlowGapSeconds (the blows stopped).
    /// </summary>
    public bool BlowHeard => Released && clock - blowHeardAt < BlowGapSeconds && breakHeardAt < blowHeardAt;

    /// <summary>20_design §6.1: within ChaseFadeSeconds after a chase ended (the chase bed and heartbeat fading), on the hunter's clock.</summary>
    public bool ChaseFading => Released && State != HunterState.Chase && clock - chaseEndedAt < ChaseFadeSeconds;

    /// <summary>
    /// The UI prompt gate (RELAY §8 amended 2026-10-14; computer_ui 20_design §6.1, CONTRACT L4; V2, 2026-10-29), its v1
    /// form; true hides a close-up prompt. Only threats the player perceives: a chase (v1's lock-on; stage 3 in v2), a door
    /// break whose blow is heard (BlowHeard), Spotting while its steps are audible (Threat's stand-in; Spotting with the
    /// Relay on screen is RelayOnScreen anyway), RelayOnScreen on <paramref name="screen"/>. Each holds until the player
    /// perceives a change: 3 s after a chase (ChaseFading), the break heard or the blows stopped (BlowHeard), the steps
    /// stopping or starting again (the Spotting hold); RelayOnScreen has none (they see it leave the frame). Never SeesPlayer alone.
    /// </summary>
    public bool ThreatPerceivable(Camera screen) =>
        Released && (State == HunterState.Chase || ChaseFading || BlowHeard || spotHeld || RelayOnScreen(screen));

    /// <summary>RELAY §8: W = min(1, L + 0.02 ΣL_n) over a cell and its 4 neighbours from FrontRoomsMapWorld.LampLevel (overrides included); no lamp is dark. EXAMINE's adapt formula (FrontRoomsInspectLook.CellLightAt) without its start-area case: the Relay is never there.</summary>
    public static float LampW(FrontRoomsMapWorld world, GridCoord cell) => world == null ? 0f
        : LampW(world.LampLevel(cell), world.LampLevel(new GridCoord(cell.x + 1, cell.y)), world.LampLevel(new GridCoord(cell.x - 1, cell.y)),
            world.LampLevel(new GridCoord(cell.x, cell.y + 1)), world.LampLevel(new GridCoord(cell.x, cell.y - 1)));

    /// <summary>LampW from the five levels (the cell's own, then its 4 neighbours); NoLamp or anything below 0 counts as 0.</summary>
    public static float LampW(float own, float east, float west, float north, float south) =>
        Mathf.Min(1f, Mathf.Max(0f, own) + LampNeighbourShare * (Mathf.Max(0f, east) + Mathf.Max(0f, west) + Mathf.Max(0f, north) + Mathf.Max(0f, south)));

    /// <summary>RelayLit from its two measures: XZ metres to the player at most RelayLitMetres and W at least RelayLitLevel.</summary>
    public static bool LitBy(float flatMetres, float w) => flatMetres <= RelayLitMetres && w >= RelayLitLevel;
    /// <summary>Path distance from the Relay's current cell to a cell, using the same edge costs as F.</summary>
    public float PathDistanceFromRelay(GridCoord cell)
    {
        EnsureRelayField();
        return relayField.DistanceTo(cell);
    }
    /// <summary>Opening cost on the Relay-rooted shortest path to a cell.</summary>
    public float PathOpeningCostFromRelay(GridCoord cell)
    {
        EnsureRelayField();
        return relayField.OpeningCostTo(cell);
    }
    public int DoorsBroken { get; private set; }
    /// <summary>The last noise it heard (where its head turns while it listens or searches); null once it sees the player or relays.</summary>
    public Vector3? ListenPoint { get; private set; }
    /// <summary>The blow DoorBlow is raised for, 0-based; the last (BlowCount - 1) is the break-through, 0.1 s before the door gives.</summary>
    public int BlowIndex { get; private set; } = -1;
    /// <summary>Blows per door: one every 0.5 s of breakDoorSeconds (5 at 2.5 s), the last brought forward 0.1 s.</summary>
    public int BlowCount => Mathf.Max(1, Mathf.RoundToInt(BreakSeconds / BlowInterval));

    // A break keeps the length it started with, so a tier or tuning change mid-break never skips the last blow.
    float breakSeconds = -1f;
    float BreakSeconds => State == HunterState.BreakDoor && breakSeconds > 0f ? breakSeconds : tuning.breakDoorSeconds;
    /// <summary>0..1 while its body is in an open or broken door's opening (1 on the door line, 0 from 0.6 m out); 0 elsewhere. For the rig's squeeze pose.</summary>
    public float DoorSqueeze { get; private set; }
    public int Relays { get; private set; }
    /// <summary>True while it passes through furniture it found no way round, until its body is clear again.</summary>
    public bool Ghosting => ghosting;
    /// <summary>Tools: the route and steering state, for test reports.</summary>
    public string DebugSteering => "path " + pathIndex + "/" + path.Count + (pathIndex < path.Count ? " next " + path[pathIndex] : "") + " detour " + detourIndex + "/" + detour.Count
        + (detourIndex < detour.Count ? " aim " + detour[detourIndex].ToString("F2") : "") + (ghosting ? " ghosting" : "") + (detourShort ? " short" : "") + " plan " + planTarget.ToString("F2");
    /// <summary>Tools: what the body touched or the straight leg hit when it last chose to pass through.</summary>
    public string DebugBlocker { get; private set; } = "";
    /// <summary>Times it found no way round furniture and passed through it.</summary>
    public int Ghosts { get; private set; }

    public event Action<HunterState> StateChanged;
    public event Action<Vector3> DoorBlow;
    public event Action Caught;
    /// <summary>It appeared (released, or relayed): its feet, and the Relay entry's tag ("vent", "doorway", ...) or null for an unmarked cell.</summary>
    public event Action<Vector3, string> Arrived;
    public event Action<int> WarnStageChanged;

    // ---------- A26 M7: floor 1's first send with a station (notes/floor1_start_group_plan.md 4.10, 9, 10) ----------
    // ---------- and the onboarding (Red 2026-10-30): the gate is exploration or time; the run's first send is gentle ----------

    /// <summary>The old M7 gate's path (tools only since 2026-10-30, -autopilotOnboardOff): this many path cells out of the station (N = 2: entering C2, 3 m past E1, 9 m before the EXIT door; Red 10-22 61 17).</summary>
    public const int StationLeaveCells = FrontRoomsStartGroup.GateCells;
    /// <summary>
    /// Floor 1's onboarding gate (Red 2026-10-30, "按探索量延后"): the first send is due once the player has stood in this
    /// many unique cells of floor 1 (the game's tier cells: never the title's stream rooms, the station or X), about one
    /// tier's worth of walking.
    /// </summary>
    public const int StationExploreCells = 150;
    /// <summary>The onboarding gate's fallback: due this long after the start door shut behind the player (the game's clock, never from Space; 150 s until Red 2026-10-30), wherever the player is.</summary>
    public const float StationSendSeconds = 180f;
    /// <summary>The tools' old gate (-autopilotOnboardOff, the 5-argument StationSendDue): its fallback, M7's 150 s before the onboarding (A15, 61 9).</summary>
    public const float OnboardOffSendSeconds = 150f;
    /// <summary>
    /// False since the onboarding gate (Red 2026-10-30): taking the station's key never makes the first send due (it
    /// still sets the first leg: FirstSendPolicy.huntTo). M7's old gate needed it (Red 10-22 61 17: N = 2 and the key);
    /// the tools' -autopilotOnboardOff passes needsKey true to the 5-argument StationSendDue to get that gate back.
    /// </summary>
    public static readonly bool StationSendNeedsKey = false;
    /// <summary>The first send's grace (ChaseGrace): a player this close (metres, flat) is chased even before the first lamp dip.</summary>
    public const float GraceMetres = 6f;

    /// <summary>Why floor 1's first send is due (StationSendDue). The values keep their numbers (tests read them).</summary>
    public enum StationGate : byte
    {
        /// <summary>Not yet: fewer than StationExploreCells cells explored and less than StationSendSeconds since the start door shut (the tools' gate: not StationLeaveCells out with the key, less than OnboardOffSendSeconds).</summary>
        Waiting,
        /// <summary>Tools only (-autopilotOnboardOff, the 5-argument StationSendDue): the player StationLeaveCells path cells out of the station, through E1 or E2, with the key taken while it is needed. The player's gate never returns it.</summary>
        Left,
        /// <summary>StationSendSeconds since the start door shut (the tools' gate: OnboardOffSendSeconds).</summary>
        Fallback,
        /// <summary>
        /// A15's third-zone rung (V3a, 系统设计 P5, 10-29). Retired by the onboarding gate (Red 2026-10-30): nothing returns
        /// it any more; the value stays so the numbering does not move.
        /// </summary>
        ThirdZone,
        /// <summary>The onboarding gate (Red 2026-10-30): the player has stood in StationExploreCells unique cells of floor 1.</summary>
        Explored,
    }

    /// <summary>A15's retired third-zone rung (the 6-argument StationSendDue no longer reads it); kept for V3a's test code.</summary>
    public const int StationThirdZone = 3;

    /// <summary>
    /// Floor 1's first-send gate (Red 2026-10-30: the onboarding, "按探索量延后"), for the game to latch (once due it stays
    /// due): Explored once <paramref name="exploredCells"/> (the unique cells of floor 1 the player has stood in, counted
    /// as the tier counts them: never the title's stream rooms, the station or X) reaches StationExploreCells; else
    /// Fallback once <paramref name="elapsed"/> (the seconds since the start door shut behind the player, 0 before it)
    /// reaches StationSendSeconds; else Waiting. Where the player stands and the station's key play no part. With no
    /// station on the map it is always Waiting; the game asks only on floor 1 with a station plan.
    /// </summary>
    public static StationGate StationSendDue(FrontRoomsMapWorld world, float elapsed, int exploredCells)
    {
        if (world == null || !world.FloorHasStation) return StationGate.Waiting;
        if (exploredCells >= StationExploreCells) return StationGate.Explored;
        return elapsed >= StationSendSeconds ? StationGate.Fallback : StationGate.Waiting;
    }

    /// <summary>
    /// Retired (Red 2026-10-30): A15's third-zone rung no longer fires; <paramref name="zonesEntered"/> is ignored and this
    /// is the 5-argument tools gate. It stays only so V3a's test code compiles until those rows are replaced; nothing in
    /// the game calls it.
    /// </summary>
    public static StationGate StationSendDue(FrontRoomsMapWorld world, GridCoord playerCell, float elapsed, bool keyTaken, bool needsKey, int zonesEntered) =>
        StationSendDue(world, playerCell, elapsed, keyTaken, needsKey);

    /// <summary>
    /// Tools only since the onboarding gate (Red 2026-10-30): the game asks it under -autopilotOnboardOff, so captures and
    /// smoke runs can still meet the Relay early on floor 1. A26 M7's old gate (plan 4.10): Left when the player's cell is
    /// at least StationLeaveCells path cells from the station (FrontRoomsMapWorld.StationPathDistance: Open passages only,
    /// so a cell behind a shut door counts as far) and, while <paramref name="needsKey"/>, the key is taken; a cell of the
    /// start area (the stream rooms behind the start door, walled off from the map) is never out. Else Fallback once
    /// <paramref name="elapsed"/> (the seconds since the start door shut, 0 before it) reaches OnboardOffSendSeconds (150);
    /// else Waiting. With no station on the map it is always Waiting.
    /// </summary>
    public static StationGate StationSendDue(FrontRoomsMapWorld world, GridCoord playerCell, float elapsed, bool keyTaken, bool needsKey)
    {
        if (world == null || !world.FloorHasStation) return StationGate.Waiting;
        if ((keyTaken || !needsKey) && !world.InStartArea(playerCell)
            && world.StationPathDistance(playerCell, StationLeaveCells + 1) >= StationLeaveCells) return StationGate.Left;
        return elapsed >= OnboardOffSendSeconds ? StationGate.Fallback : StationGate.Waiting;
    }

    /// <summary>
    /// How ReleaseNow (and v2's FirstSend placement) places the run's first send. ForStation: floor 1's, made from the
    /// map's station (the start group's layout: the station, the walk D..N, N, X, the strip; and the first leg). Generic
    /// (the onboarding, Red 2026-10-30): no layout, for the first floor below the station when the player left floor 1
    /// before its gate; the same rungs from the player, rung 1's side behind the player.
    /// </summary>
    public sealed class FirstSendPolicy
    {
        /// <summary>The start group (FrontRoomsMapWorld.StationLayout); read only while <see cref="station"/>.</summary>
        public StartGroupLayout layout;
        /// <summary>True: <see cref="layout"/> is the map's station's (ForStation). False: no layout (Generic).</summary>
        public bool station;
        /// <summary>The first leg: a Hunt to this point once the station's key is taken (FrontRoomsMapWorld.StationTakePoint); null: Listen, as any release.</summary>
        public Vector3? huntTo;
        /// <summary>Tools: rungs 1-3 taken as empty (bit k - 1 for rung k), so a test can walk the ladder down to today's Arrive.</summary>
        public int emptyRungsForTools;

        /// <summary>The policy for the map's station (its layout; the hunt to the take point once its key is taken), or null with none.</summary>
        public static FirstSendPolicy ForStation(FrontRoomsMapWorld world) =>
            world == null || !world.HasStation ? null
                : new FirstSendPolicy { layout = world.StationLayout, station = true, huntTo = world.StationKeyIsTaken ? world.StationTakePoint : (Vector3?)null };

        /// <summary>The onboarding's first send with no station's layout (Red 2026-10-30): the ladder from the player, rung 1 behind them; the first leg Listen (or a Hunt to <paramref name="huntTo"/>).</summary>
        public static FirstSendPolicy Generic(Vector3? huntTo = null) => new FirstSendPolicy { station = false, huntTo = huntTo };
    }

    /// <summary>What the last ReleaseNow did (M8: the game's RELAY line and the autopilot's rows).</summary>
    public struct FirstSendReport
    {
        /// <summary>1-3: the ladder's rung (1: 8-10 walking cells on rung 1's side; 2: 6-11, any side; 3: 14-20); 4: today's Arrive (the station and its ring kept out); 0: none yet.</summary>
        public int rung;
        /// <summary>The cells each rung offered (unseen, the body fits, every exclusion kept); 0 each for a release with no policy.</summary>
        public int rung1, rung2, rung3;
        /// <summary>The arrival's cell, and its walking cells from the player (the Search that chose it, doors passable; -1 beyond it).</summary>
        public GridCoord cell;
        public int cells;
        /// <summary>Straight-line metres from the player's feet to the arrival.</summary>
        public float straight;
        /// <summary>The exit field from N at the arrival less at the player, metres (+infinity past the field's reach); NaN when no field decided the side (no layout, or the field did not reach the player: the behind test).</summary>
        public float margin;
        /// <summary>
        /// On rung 1's side. With the station's layout and the exit field from N reaching the player: the far side of the walk
        /// to the EXIT, margin at least FrontRoomsStartGroup.FarSideMetres. Without the layout and with a FLOW target whose
        /// field reaches the player (A40, 系统设计 10-30): the far side from that target, the same margin (flow). Otherwise:
        /// behind the player, dot(arrival - feet, the player's look) &lt; 0 on the flat.
        /// </summary>
        public bool farSide;
        /// <summary>
        /// A40: the generic ladder anchored on the current FLOW target (FlowTarget: the station's TV before its key is taken,
        /// N after it or on a floor without a station) and kept 2 cells off the player's route to it (LastFlowRoute).
        /// </summary>
        public bool flow;
        /// <summary>The ladder ran on the station's layout (ForStation); false: the generic ladder (no layout) or no policy.</summary>
        public bool station;
        /// <summary>At the placement neither its eye nor its head was in the player's sight (the ladder and Arrive both pick unseen; re-tested on the spot).</summary>
        public bool unseen;
        /// <summary>The first leg is a Hunt to the station's take point (its key taken), not Listen.</summary>
        public bool hunt;
    }

    /// <summary>What the last ReleaseNow did; rung 0 before one.</summary>
    public FirstSendReport LastFirstSend { get; private set; }

    /// <summary>
    /// The run's first send has been placed (Red 2026-10-30): ReleaseNow with a policy (v1: floor 1's gate, or the release
    /// below it through ReleasePolicy) or, under FRONTROOMS_RELAY_V2, a FirstSend dispatch. Run-level: Park and Disarm keep
    /// it (a floor change), a new run makes a new hunter. Every later send follows the normal rules.
    /// </summary>
    public bool FirstSendDone { get; private set; }

    /// <summary>
    /// The Relay on the map now came by the run's first send: from that placement until Park or Disarm (the floor is
    /// left), DebugPlace, or (v2) the next dispatch. The game holds ChaseGrace for this send only.
    /// </summary>
    public bool OnFirstSend { get; private set; }

    /// <summary>
    /// v1 only (the define off): how the Dormant release (releaseDelaySeconds of ticks after the floor's start door shut)
    /// places it. Null: today's Arrive, then Listen. A policy: ReleaseNow's ladder (the run's gentle first send below
    /// floor 1, Red 2026-10-30), the release timing unchanged. The game sets it while the Relay is Dormant.
    /// </summary>
    public FirstSendPolicy ReleasePolicy { get; set; }

    /// <summary>
    /// The first send's grace (A26 M7, plan 4.10; since Red 2026-10-30 the run's first send wherever it comes): while true,
    /// seeing the player starts no chase unless they are within GraceMetres; it goes on with its leg (and still catches at
    /// catchDistance). The game holds it while the Relay on the map came by the run's first send (OnFirstSend), until the
    /// first W1 burst after it; false everywhere else.
    /// </summary>
    public bool ChaseGrace { get; set; }

    public FrontRoomsMapHunter(FrontRoomsMapWorld world, FrontRoomsHunterTuning tuning, Collider playerCollider, Transform rig, int seed)
    {
        this.world = world;
        this.tuning = tuning ?? new FrontRoomsHunterTuning();
        this.playerCollider = playerCollider;
        this.rig = rig;
        rng = (uint)seed | 1u;
        // A door leaf the player swings stops on the Relay's body as on the player's.
        if (world != null) world.RelayBody = () => Released ? position : (Vector3?)null;
    }

    /// <summary>Tools (the autopilot's door scenario): while true, Tick does nothing and the Relay stands where it is, so it cannot cancel a shot under test.</summary>
    public bool HeldForTools { get; set; }

    public void Tick(float dt, Vector3 playerFeet, Vector3 playerEye, Vector3 playerForward)
    {
        if (caught || world == null || HeldForTools) return;
        sightFeet = playerFeet;
        sightEye = playerEye;
        sightForward = playerForward;
#if FRONTROOMS_RELAY_V2
        // RELAY 4, 5: no timed release. Dormant waits for Arm, Away for a dispatch, Arrive counts its delay to the step-in.
        if (TickOffMap(dt, playerFeet, playerEye)) return;
#else
        if (State == HunterState.Dormant)
        {
            ResetWarningMetrics();
            PlayerSeesRelay = RelayLit = false;
            playerSeesRelayTimer = 0f;
            releaseTimer += dt;
            if (releaseTimer >= tuning.releaseDelaySeconds)
            {
                // The onboarding (Red 2026-10-30): the run's first send below floor 1 keeps this timing; ReleasePolicy places it gently.
                if (ReleasePolicy != null) ReleaseNow(ReleasePolicy, playerFeet, playerEye, playerForward);
                else if (Arrive(playerFeet, playerEye, playerForward)) SetState(HunterState.Listen);
            }
            DoorSqueeze = 0f;
            return;
        }
#endif

        StateTime += dt;
        clock += dt;
        var playerCell = world.CellOf(playerFeet);
        var myCell = world.CellOf(position);
        EnsurePlayerField(playerCell);
        PathDistanceToPlayer = playerField.DistanceTo(myCell);
        PathOpeningCost = playerField.OpeningCostTo(myCell);
        WarningDistance = Mathf.Min(PathDistanceToPlayer, 3f * Vector3.Distance(position, playerFeet));
        UpdateWarnStage(dt, myCell);
        playerSeesRelayTimer -= dt;
        if (playerSeesRelayTimer <= 0f) RefreshSight();
        // A tick that relays it below does not look, so it breaks the run of ticks that saw the player.
        var sawBefore = sawLastTick;
        sawLastTick = false;

#if FRONTROOMS_RELAY_V2
        // RELAY 5: both leashes are deleted. An unbuilt cell under it is Away at once (LoseTrack first in a chase).
        if (!world.IsBuilt(myCell) && UnbuiltCell()) return;
#else
        // Never let the chase run off the built map, and never trail so far
        // behind that the player forgets it: relay closer, out of sight.
        leashTimer -= dt;
        if (!world.IsBuilt(myCell) || (State != HunterState.Chase && State != HunterState.BreakDoor && leashTimer <= 0f
            && clock - lastContactTime > LeashQuietSeconds && Distance(myCell, playerCell, LeashCells + 1) > LeashCells))
        {
            leashTimer = LeashCheckSeconds;
            if (Arrive(playerFeet, playerEye, playerForward)) SetState(HunterState.Listen);
            return;
        }
        if (leashTimer <= 0f) leashTimer = LeashCheckSeconds;
#endif

        // The stairwell is a safe zone (Red, 2026-10-22 15:47, 游戏视觉 chat: "这种楼梯间其实是安全区", "也就是说怪物是
        // 进不来的"): the player in X is never seen, so never caught, and a chase ends the moment they are in (it goes
        // where it last saw them, outside X, as when sight is lost). X is already no map to it (C22 D3).
        var playerSafe = world.InExitArea(playerCell);
        SeesPlayer = !playerSafe && Sees(playerEye);
        sawLastTick = SeesPlayer;
        if (playerSafe && State == HunterState.Chase) BeginSafeDoor();
        if (safeDoor != SafeDoorPhase.None)
        {
            safeDoorTime += dt;
            if (safeDoorTime > SafeDoorBeatMaxSeconds) safeDoor = SafeDoorPhase.None;
        }
        if (SeesPlayer)
        {
            // A door it watched the player go through: seen on one side last tick and on the other now.
            if (sawBefore && Mathf.Abs(playerCell.x - seenCell.x) + Mathf.Abs(playerCell.y - seenCell.y) == 1
                && world.Cache.Edge(seenCell, playerCell) == EdgeKind.Door)
            {
                doorInto = playerCell;
                doorTime = clock;
            }
            seenCell = playerCell;
            ListenPoint = null;
            lastSeen = playerFeet;
            lastSeenTime = lastContactTime = clock;
            lostTime = 0f;
            // A26 M7: floor 1's grace holds the first chase until the first lamp dip, unless the player is close.
            if (State != HunterState.Chase && State != HunterState.BreakDoor && (!ChaseGrace || Flat(position - playerFeet).magnitude <= GraceMetres)
#if FRONTROOMS_RELAY_V2
                && ChaseAllowedV2(playerFeet)
#endif
                )
            {
                replanTimer = 0f;
                safeDoor = SafeDoorPhase.None;
#if FRONTROOMS_RELAY_V2
                BeginChaseV2();
#endif
                SetState(HunterState.Chase);
            }
        }

        var before = position;
        switch (State)
        {
            case HunterState.Listen:
#if FRONTROOMS_RELAY_V2
                // RELAY 5, 11: the step-in's listen, then a hunt to the call's estimate; or the give-up beat, then patrol or Withdraw.
                TickListenV2(myCell);
#else
                // It does not know where the player is: after listening it wanders on.
                if (StateTime > tuning.listenSeconds) Wander(myCell);
#endif
                break;
            case HunterState.Wander:
#if FRONTROOMS_RELAY_V2
                // RELAY 11: Wander is the patrol after the give-up beat (tier 3 and up), then Withdraw.
                TickPatrolV2(myCell, dt);
#else
                if (Follow(tuning.huntSpeed * WanderPace, dt) || Stalled(dt))
                {
                    ResetSteering();
                    SetState(HunterState.Listen);
                }
#endif
                break;
#if FRONTROOMS_RELAY_V2
            case HunterState.Withdraw:
                TickWithdrawV2(dt, myCell, playerEye);
                break;
#endif
            case HunterState.Hunt:
                if (safeDoor == SafeDoorPhase.Approach || safeDoor == SafeDoorPhase.Stop)
                {
                    TickSafeDoor(myCell, dt);
                    break;
                }
                if (Follow(tuning.huntSpeed, dt)) BeginSearch(world.CellOf(goal));
                else if (Stalled(dt))
                {
                    // Held in place (a covered aim, a dropped chunk): search where it stands.
                    ResetSteering();
                    BeginSearch(myCell);
                }
                break;
            case HunterState.Search:
                TickSearch(myCell, dt);
                break;
            case HunterState.Chase:
                replanTimer -= dt;
                if (replanTimer <= 0f)
                {
                    replanTimer = ReplanSeconds;
                    Plan(RouteCell(myCell), world.CellOf(lastSeen), lastSeen);
                }
                if (SeesPlayer && myCell == playerCell) MoveDirect(playerFeet, tuning.chaseSpeed, dt);
                else Follow(tuning.chaseSpeed, dt);
                if (!SeesPlayer)
                {
                    lostTime += dt;
#if FRONTROOMS_RELAY_V2
                    // RELAY 4: past onMapCap + 15 s the first 0.5 s of broken sight ends it; a chase past chaseCap loses at 1.0 s.
                    if (lostTime > LoseSecondsV2) LoseTrack();
#else
                    if (lostTime > tuning.lostSightSeconds) LoseTrack();
#endif
                }
                break;
            case HunterState.BreakDoor:
                TickBreak(dt);
                break;
        }
        var step = position - before;
        step.y = 0f;
        Moving = step.sqrMagnitude > 1e-6f;
        if (State == HunterState.BreakDoor && breakingDoor != null) Heading = breakFacing;
        else if (safeDoor == SafeDoorPhase.Stop) Heading = safeDoorFacing;
        else if (Moving) Heading = step.normalized;
        if (safeDoor == SafeDoorPhase.Leave && Flat(position - safeDoorFrom).magnitude >= SafeDoorLeaveMetres) safeDoor = SafeDoorPhase.None;
        DoorSqueeze = DoorSqueezeAt(position);
        UpdateSpottingHold();

        if (SeesPlayer && Flat(position - playerFeet).magnitude < tuning.catchDistance)
        {
            caught = true;
            Caught?.Invoke();
        }
    }

    /// <summary>
    /// C22 (plan v2 D12): park the Relay for a floor change. The game calls it at the seal, once the player is
    /// shut in the stairwell (X, no map to the Relay) and before NextFloor reseeds the same map in place; the
    /// same hunter then serves the next floor, so every binding to it (the game's handlers, the sound layer,
    /// haptics, the bot) stays. It goes back to Dormant (StateChanged once, unless it already was) with its
    /// release timer at 0: the next floor releases it as the first one did, after releaseDelaySeconds of
    /// Dormant ticks (the game holds those ticks until the floor's start door has shut, D10). Everything it
    /// was doing or knew belongs to the old floor and goes: the route and its steering, a room search, a door
    /// it was breaking (the map's mark on it lifted, the blows not struck), the noise it was walking to, the
    /// player it saw and the door it saw them take, and the warning metrics (stage 0 at once, W and F
    /// infinite). Its path fields are dropped (NextFloor's PassageRevision bump would rebuild them anyway).
    /// The body is hidden (the rig object off; the game keeps it off while it is Dormant), and the map's
    /// doors no longer stop on it (RelayBody reads nothing while Dormant). Run-long counts stay (DoorsBroken,
    /// Relays, Ghosts), as do its random sequence, its tuning and HeldForTools; a caught Relay stays caught
    /// (the run is over; the seal never comes on a catch's tick, D14).
    /// </summary>
    public void Park()
    {
        StopBreaking();
        breakSeconds = -1f;
        blowsStruck = 0;
        BlowIndex = -1;
        path.Clear();
        pathIndex = 0;
        planThroughDoors = true;
        ResetSteering();
        sweep.Clear();
        sweepIndex = 0;
        sweepPlanned = -1;
        lookTimer = 0f;
        searchTime = 0f;
        ListenPoint = null;
        SeesPlayer = false;
        sawLastTick = false;
        seenCell = doorInto = default;
        lastSeenTime = doorTime = float.MinValue;
        lostTime = replanTimer = leashTimer = 0f;
        // Its clock stands while it is Dormant: the leash's quiet time counts from the next release, as at the first.
        lastContactTime = clock;
        Moving = false;
        DoorSqueeze = 0f;
        releaseTimer = 0f;
        // The onboarding (Red 2026-10-30): the first send's Relay belongs to the old floor; FirstSendDone (the run's) stays.
        OnFirstSend = false;
        playerFieldReady = relayFieldReady = false;
        playerFieldRevision = relayFieldRevision = -1;
        ResetWarningMetrics();
        PlayerSeesRelay = RelayLit = false;
        playerSeesRelayTimer = 0f;
        sightTick++;
        safeDoor = SafeDoorPhase.None;
#if FRONTROOMS_RELAY_V2
        // Disarm (RELAY 4): the call it answered, its Arrive, beat, patrol and Withdraw, and FADE all belong to the old floor.
        ResetEncounterV2();
#endif
        if (rig != null) rig.gameObject.SetActive(false);
        // Dormant also drops ThreatPerceivable's holds (SetState).
        SetState(HunterState.Dormant);
    }

    /// <summary>Tools and tests: stand released at a point (searching), with no plan.</summary>
    public void DebugPlace(Vector3 feet)
    {
        position = feet;
        // A Relay placed by tools did not come by the run's first send (FirstSendDone is left as it is).
        OnFirstSend = false;
        // Placed, it has not been watching: no run of seen ticks carries over.
        sawLastTick = false;
        safeDoor = SafeDoorPhase.None;
        path.Clear();
        pathIndex = 0;
        StopBreaking();
        ResetSteering();
        sweep.Clear();
        sweepIndex = 0;
        lookTimer = 0f;
        playerSeesRelayTimer = 0f;
        sightTick++;
        // Nor does a stage or its timers: the next tick enters the band the new spot is in, on its first sample.
        ClearWarnStage();
        DoorSqueeze = DoorSqueezeAt(position);
#if FRONTROOMS_RELAY_V2
        // Nor a call, a beat, a patrol, a Withdraw or FADE: a placed Relay is on the map, searching, with none of them.
        ResetEncounterV2();
#endif
        SetState(HunterState.Search);
        // Nor anything the player perceived of it before (a chase it was in, blows): a placed Relay starts with no hold.
        ClearThreatHolds();
    }

    /// <summary>Tools and tests: the 10 Hz sight refresh now (PlayerSeesRelay, RelayLit, RelayOnScreen's cache), from these player inputs, as the next tick's would be.</summary>
    public void DebugRefreshSight(Vector3 playerFeet, Vector3 playerEye, Vector3 playerForward)
    {
        sightFeet = playerFeet;
        sightEye = playerEye;
        sightForward = playerForward;
        RefreshSight();
    }

    /// <summary>A sound the player made. The Relay walks to the last one it heard, even mid-search.</summary>
    public void Noise(Vector3 source, float radius)
    {
        if (!Released || State == HunterState.Chase || State == HunterState.BreakDoor) return;
#if FRONTROOMS_RELAY_V2
        // RELAY 4, 11: on the way out it hears at half range, and under FADE a noise gets one cautious look at most.
        if (!HearsV2()) return;
        if (State == HunterState.Withdraw) radius *= .5f;
#endif
        var hearingRadius = Mathf.Max(0f, radius * tuning.hearing);
        var sourceCell = world.CellOf(source);
        // C22 (plan v2 D3): X, the EXIT's stairwell cell, is no map to the Relay. The stairwell is a safe zone (Red,
        // 2026-10-22): a noise made inside it, past the door's line (the player's steps or sprint), is never heard.
        // The EXIT door's own noises lie on its line, the map's side of the stairwell: heard from N, the cell in
        // front of it, as they are; the hunt goes to N's side of that door (Plan keeps every goal out of X).
        var fromExit = world.InExitArea(sourceCell);
        if (fromExit)
        {
            var front = world.ExitFrontCell;
            var into = Flat(world.CellCenter(sourceCell) - world.CellCenter(front)).normalized;
            if (Vector3.Dot(Flat(source - world.CrossingPoint(front, sourceCell)), into) > ExitLineSlack) return;
            sourceCell = front;
        }
        var myCell = world.CellOf(position);
        float distance;
        if (hearingRadius > SourceFieldCap)
        {
            // Very large-radius tool probes are intentionally broad; the
            // normal gameplay radii stay inside the one-off field's bound.
            distance = 0f;
        }
        else if (playerFieldReady && sourceCell == playerFieldRoot && playerFieldRevision == world.PassageRevision && hearingRadius <= PlayerFieldCap)
        {
            distance = playerField.DistanceTo(myCell);
        }
        else
        {
            // From X the noise is N's alone: the EXIT door's other side is the stairwell, no map to root a field in.
            GridCoord? edgeCell = null;
            if (!fromExit) TrySourceEdge(source, sourceCell, out edgeCell);
            sourceField.Build(world, sourceCell, edgeCell);
            distance = sourceField.DistanceTo(myCell);
        }
        if (distance > hearingRadius) return;
#if FRONTROOMS_RELAY_V2
        HeardV2();
#endif
        ListenPoint = source;
        lastContactTime = clock;
        HuntToward(sourceCell, source);
    }

    /// <summary>
    /// L11: the chase ends as the player steps into X. It walks to where it would stand to face the EXIT door, on
    /// N's side (the goal KeepOutOfExit gives any point in X), as a hunt.
    /// </summary>
    void BeginSafeDoor()
    {
        var front = world.ExitFrontCell;
        var exit = world.ExitCell;
        safeDoorFacing = Flat(world.CellCenter(exit) - world.CellCenter(front)).normalized;
        safeDoor = SafeDoorPhase.Approach;
        safeDoorTime = 0f;
        HuntToward(front, world.CrossingPoint(front, exit) - safeDoorFacing * BreakFaceDistance);
    }

    /// <summary>L11, approach and stop: at the stand (or held short of it) it stops facing the door, then wanders off.</summary>
    void TickSafeDoor(GridCoord myCell, float dt)
    {
        if (safeDoor == SafeDoorPhase.Approach)
        {
            if (!Follow(tuning.huntSpeed, dt) && !Stalled(dt)) return;
            ResetSteering();
            safeDoor = SafeDoorPhase.Stop;
            safeDoorStopAt = safeDoorTime;
            StoppedAtSafeDoor?.Invoke(position);
            return;
        }
        if (safeDoorTime - safeDoorStopAt < SafeDoorStopSeconds) return;
        // Turned away: it walks off as a wander does (never into X, which is no map to it), and the beat ends once it
        // is SafeDoorLeaveMetres from where it stopped.
        safeDoor = SafeDoorPhase.Leave;
        safeDoorFrom = position;
#if FRONTROOMS_RELAY_V2
        // Plan 1.1 item 4 (V3a): L11 ends in the give-up beat and Withdraw (no patrol): the walk off is the walk out.
        BeginGiveUpV2(false);
#else
        Wander(myCell);
#endif
    }

    /// <summary>
    /// Sight lost in a chase. If it saw the player go through a door in its
    /// last second of sight, it was right behind: it follows into the room
    /// behind that door. Otherwise it goes where it last saw them. It searches
    /// there and no further (BeginSearch). Only what it saw counts: a door the
    /// player goes through out of its sight reaches it only as a noise.
    /// </summary>
    void LoseTrack()
    {
#if FRONTROOMS_RELAY_V2
        // The chase is over: a chaseCap's 1.0 s lose threshold was for that chase only.
        chaseLoseCapV2 = -1f;
#endif
        if (doorTime >= lastSeenTime - SeenDoorSeconds)
        {
            HuntToward(doorInto, world.CellCenter(doorInto));
            return;
        }
        HuntToward(world.CellOf(lastSeen), lastSeen);
    }

    /// <summary>
    /// Search the room around a cell: if it is in a carved room, a few of that
    /// room's cells; otherwise the cells within two steps that need no door.
    /// It listens where it stands first, then walks from spot to spot.
    /// </summary>
    void BeginSearch(GridCoord around)
    {
        sweep.Clear();
        sweepIndex = 0;
        sweepPlanned = -1;
        lookTimer = tuning.searchLookSeconds;
        searchTime = 0f;
        candidates.Clear();
        if (!RoomCells(around, candidates))
        {
            Search(around, 2, false);
            foreach (var pair in depth) candidates.Add(pair.Key);
        }
        candidates.Remove(around);
#if FRONTROOMS_RELAY_V2
        // RELAY 4: under FADE (and on the way out) a lost chase or a heard noise gets a cautious look, one spot.
        var spots = fadingV2 ? 1 : SweepSpots;
#else
        var spots = SweepSpots;
#endif
        for (var k = 0; k < spots && candidates.Count > 0; k++)
        {
            var pick = (int)(Next() % (uint)candidates.Count);
            sweep.Add(candidates[pick]);
            candidates.RemoveAt(pick);
        }
        ResetSteering();
        SetState(HunterState.Search);
    }

    void TickSearch(GridCoord myCell, float dt)
    {
        searchTime += dt;
        if (lookTimer > 0f)
        {
            lookTimer -= dt;
            return;
        }
        if (sweepIndex >= sweep.Count || searchTime > tuning.searchMaxSeconds)
        {
#if FRONTROOMS_RELAY_V2
            // RELAY 5, 11: nothing found ends in the give-up beat (then patrol from tier 3, then Withdraw).
            BeginGiveUpV2(true);
#else
            // Nothing found: it gives up and wanders on.
            SetState(HunterState.Listen);
#endif
            return;
        }
        if (sweepPlanned != sweepIndex)
        {
            sweepPlanned = sweepIndex;
            // A spot that open ways no longer reach (a door shut since it chose them) is skipped, never walked at.
            if (!Plan(myCell, sweep[sweepIndex], world.CellCenter(sweep[sweepIndex]), false))
            {
                sweepIndex++;
                return;
            }
        }
        if (Follow(tuning.huntSpeed * SearchPace, dt) || Stalled(dt))
        {
            sweepIndex++;
            lookTimer = tuning.searchLookSeconds;
            ResetSteering();
        }
    }

    /// <summary>The built cells of the carved room (or module) that contains a cell, if any.</summary>
    bool RoomCells(GridCoord cell, List<GridCoord> into)
    {
        if (!world.IsBuilt(cell)) return false;
        var chunk = world.Cache.Get(MapGrid.ChunkOf(cell));
        var o = chunk.Origin;
        int lx = cell.x - o.x, ly = cell.y - o.y;
        for (var r = chunk.rooms.Length - 1; r >= 0; r--)
        {
            var rect = chunk.rooms[r];
            if (!chunk.RoomIntact(r) || !rect.Contains(lx, ly)) continue;
            for (var j = rect.y; j < rect.y + rect.h; j++)
            for (var i = rect.x; i < rect.x + rect.w; i++)
                into.Add(chunk.Cell(i, j));
            return true;
        }
        return false;
    }

    void HuntToward(GridCoord cell, Vector3? point)
    {
        Plan(RouteCell(world.CellOf(position)), cell, point ?? world.CellCenter(cell));
        SetState(HunterState.Hunt);
    }

    /// <summary>
    /// Arrive 9–15 cells of walking away from the player, where its body fits
    /// and the player cannot see it, preferring spots behind them: a designer's
    /// Relay entry behind the player, else a cell behind them, else an entry
    /// anywhere, else the nearest cell. Used for the first release and for
    /// every relay; raises Arrived. On floor 1 with a station, never in the
    /// station or its ring (IsArriveExcluded, A26 M7).
    /// </summary>
    bool Arrive(Vector3 playerFeet, Vector3 playerEye, Vector3 playerForward)
    {
        var start = world.CellOf(playerFeet);
        Search(start, SpawnMaxCells, true);
        // The entries are tested on the same walking distances (no other search in between).
        world.RelayEntries(entries);
        entriesBehind.Clear();
        entriesAny.Clear();
        for (var k = 0; k < entries.Count; k++)
        {
            var cell = world.CellOf(entries[k].pos);
            if (!depth.TryGetValue(cell, out var d) || d < SpawnMinCells || d > SpawnMaxCells || !world.IsBuilt(cell) || IsArriveExcluded(cell)) continue;
            var feet = EntryFeet(k, cell);
            if (!BodyFits(feet) || Visible(playerEye, feet + Vector3.up * EyeHeight) || Visible(playerEye, feet + Vector3.up * ProbeTop)) continue;
            entriesAny.Add(k);
            if (Vector3.Dot(Flat(feet - playerFeet), Flat(playerForward)) < 0f) entriesBehind.Add(k);
        }
        candidates.Clear();
        GridCoord fallback = start;
        var haveFallback = false;
        foreach (var pair in depth)
        {
            if (pair.Value < SpawnMinCells || pair.Value > SpawnMaxCells || !world.IsBuilt(pair.Key) || IsArriveExcluded(pair.Key)) continue;
            var center = world.CellCenter(pair.Key);
            // Unseen means the head too: a head above a cubicle panel gives it away (the rig stands up to 1.95 m).
            if (!BodyFits(center) || Visible(playerEye, center + Vector3.up * EyeHeight) || Visible(playerEye, center + Vector3.up * ProbeTop)) continue;
            if (!haveFallback) { fallback = pair.Key; haveFallback = true; }
            if (Vector3.Dot(Flat(center - playerFeet), Flat(playerForward)) < 0f) candidates.Add(pair.Key);
        }
        // One random draw at most, as before, so the rest of the run keeps its sequence.
        if (entriesBehind.Count > 0) return Appear(entriesBehind[(int)(Next() % (uint)entriesBehind.Count)]);
        if (candidates.Count > 0) return Appear(world.CellCenter(candidates[(int)(Next() % (uint)candidates.Count)]), null);
        if (entriesAny.Count > 0)
        {
            // The nearest by walking distance.
            var best = entriesAny[0];
            for (var i = 1; i < entriesAny.Count; i++)
                if (depth[world.CellOf(entries[entriesAny[i]].pos)] < depth[world.CellOf(entries[best].pos)]) best = entriesAny[i];
            return Appear(best);
        }
        if (haveFallback) return Appear(world.CellCenter(fallback), null);
        return false;
    }

    /// <summary>An entry's feet: its point on the map's floor (the marker's own height is not used).</summary>
    Vector3 EntryFeet(int k, GridCoord cell)
    {
        var feet = entries[k].pos;
        feet.y = world.CellCenter(cell).y;
        return feet;
    }

    bool Appear(int entry) => Appear(EntryFeet(entry, world.CellOf(entries[entry].pos)), entries[entry].tag);

    bool Appear(Vector3 feet, string tag)
    {
        position = feet;
        ResetSteering();
        path.Clear();
        pathIndex = 0;
        StopBreaking();
        ListenPoint = null;
        Relays++;
#if FRONTROOMS_RELAY_V2
        // RELAY 5: placement is backstage; Arrived fires at the step-in (StepInV2) with this tag.
        arriveTagV2 = tag;
#else
        Arrived?.Invoke(position, tag);
#endif
        return true;
    }

    /// <summary>A26 M7: no Arrive lands in floor 1's station or the ring of cells round it (plan 4.10); nothing is kept out with no station.</summary>
    bool IsArriveExcluded(GridCoord cell) => world.HasStation && FrontRoomsExitPlanner.InAreaOrRing(world.StationLayout.station, cell);

    /// <summary>
    /// A26 M7 (plan 4.10): send the Dormant Relay now, with no release delay (the game's gate is the delay, and the game
    /// does not Tick it while the gate waits). With a policy: the run's first send (Red 2026-10-30), the ladder (FirstSend:
    /// on the station's layout with ForStation's and a station on the map, else the generic ladder from the player), then
    /// the first leg, a Hunt to the policy's point (the station's take point once its key is taken, M6), else Listen as any
    /// release; FirstSendDone and OnFirstSend turn true. Without one: today's Arrive, then Listen. False, still Dormant,
    /// when no cell was found (the game asks again next frame). LastFirstSend says what it did.
    /// </summary>
    public bool ReleaseNow(FirstSendPolicy policy, Vector3 playerFeet, Vector3 playerEye, Vector3 playerForward)
    {
        if (caught || world == null || State != HunterState.Dormant) return false;
        var report = new FirstSendReport { rung = 4, cells = -1, margin = float.NaN };
        if (policy != null)
        {
            if (!FirstSend(policy, playerFeet, playerEye, playerForward, ref report)) return false;
        }
        else
        {
            if (!Arrive(playerFeet, playerEye, playerForward)) return false;
            report.cell = world.CellOf(position);
            report.cells = depth.TryGetValue(report.cell, out var d) ? d : -1;
            report.straight = Flat(position - playerFeet).magnitude;
            report.unseen = UnseenAt(position, playerEye);
        }
        report.hunt = policy != null && policy.huntTo.HasValue;
        LastFirstSend = report;
        if (policy != null) FirstSendDone = OnFirstSend = true;
        if (report.hunt) HuntStation(policy.huntTo.Value, true);
        else SetState(HunterState.Listen);
        return true;
    }

    /// <summary>
    /// M6's first leg (A26, plan 4.10): the station's key was taken (FrontRoomsMapWorld.StationKeyTaken). A released
    /// Relay hunts to the take point as to a noise it cannot miss (no hearing range), unless it is chasing or breaking a
    /// door; the hunt's last point is the take point itself, never its cell's middle (inside the riser block in FD-b). A
    /// Dormant one is not moved: it starts with this hunt when it is sent (FirstSendPolicy.huntTo).
    /// </summary>
    public void HuntStation(Vector3 takePoint) => HuntStation(takePoint, false);

    void HuntStation(Vector3 takePoint, bool sending)
    {
        if (!sending && (!Released || State == HunterState.Chase || State == HunterState.BreakDoor)) return;
#if FRONTROOMS_RELAY_V2
        // RELAY 6: on the way out or under FADE a device is queued by the director, not hunted (V3b re-dispatches).
        if (!sending && (State == HunterState.Withdraw || fadingV2)) return;
#endif
        ListenPoint = takePoint;
        lastContactTime = clock;
        HuntToward(world.CellOf(takePoint), takePoint);
    }

    /// <summary>
    /// A26 M7's ladder (plan 4.10), the run's first send (Red 2026-10-30: wherever it comes): from the player, the Relay's
    /// own Search (doors passable) out to 20 cells; the cells it could stand in unseen (as Arrive: the body fits, neither
    /// its eye nor its head in the player's sight), never in the station or its ring, the start area, a stairwell (X, or
    /// the one the floor began in) or within 2 cells (Chebyshev) of one; with the station's layout also never in the strip
    /// or within 2 cells of the walk D..N. Rung 1: 8-10 walking cells on rung 1's side: with the layout and the exit field
    /// from N reaching the player, the far side of the walk to the EXIT (the field's metres at least FarSideMetres beyond
    /// the player's); otherwise (no layout, or the player past the field's reach: far from the station) behind the
    /// player. Rung 2: 6-11 cells, any side; rung 3: 14-20; one random draw from the first that has any. None: rung 4,
    /// today's Arrive with the station and its ring kept out.
    /// </summary>
    bool FirstSend(FirstSendPolicy policy, Vector3 playerFeet, Vector3 playerEye, Vector3 playerForward, ref FirstSendReport report)
    {
        // A station policy with no station left on the map (ClearStation) runs as the generic ladder.
        var hasLayout = policy.station && world.HasStation;
        var l = policy.layout;
        var start = world.CellOf(playerFeet);
        var playerField = float.PositiveInfinity;
        flowRoute.Clear();
        if (hasLayout)
        {
            // The exit field from N (Noise's scratch field: Noise builds it before every read). M5's beacon walk shares the rule.
            sourceField.Build(world, l.front);
            playerField = sourceField.DistanceTo(start);
        }
        else if (FlowTarget(out var flow))
        {
            // A40 (系统设计 10-30, research/relay_pursuit/NOTE_1030_onboarding_system.md §4): off the station's layout the
            // ladder anchors on the player and the current FLOW target, as C32 anchored on the station: rung 1 on the far
            // side from the target, and no cell within 2 of the player's route to it (C32's walk back: never block the way
            // the arrows point). A target past the field's reach falls back to behind the player.
            sourceField.Build(world, flow);
            playerField = sourceField.DistanceTo(start);
            if (!float.IsPositiveInfinity(playerField)) FlowRoute(start);
        }
        var byField = !float.IsPositiveInfinity(playerField);
        Search(start, FrontRoomsStartGroup.Rung3Max, true);
        rungFar.Clear();
        rungNear.Clear();
        rungWide.Clear();
        foreach (var pair in depth)
        {
            var d = pair.Value;
            var near = d >= FrontRoomsStartGroup.Rung2Min && d <= FrontRoomsStartGroup.Rung2Max;
            var wide = d >= FrontRoomsStartGroup.Rung3Min && d <= FrontRoomsStartGroup.Rung3Max;
            if ((!near && !wide) || !world.IsBuilt(pair.Key) || FirstSendExcluded(hasLayout, l, pair.Key)) continue;
            var center = world.CellCenter(pair.Key);
            if (!BodyFits(center) || Visible(playerEye, center + Vector3.up * EyeHeight) || Visible(playerEye, center + Vector3.up * ProbeTop)) continue;
            if (near) rungNear.Add(pair.Key);
            if (wide) rungWide.Add(pair.Key);
            if (d >= FrontRoomsStartGroup.Rung1Min && d <= FrontRoomsStartGroup.Rung1Max && FarSide(pair.Key, playerField, byField, playerFeet, playerForward)) rungFar.Add(pair.Key);
        }
        report.rung1 = rungFar.Count;
        report.rung2 = rungNear.Count;
        report.rung3 = rungWide.Count;
        var off = policy.emptyRungsForTools;
        var pick = rungFar.Count > 0 && (off & 1) == 0 ? rungFar : rungNear.Count > 0 && (off & 2) == 0 ? rungNear : rungWide.Count > 0 && (off & 4) == 0 ? rungWide : null;
        if (pick != null)
        {
            report.rung = pick == rungFar ? 1 : pick == rungNear ? 2 : 3;
            // One random draw at most, as Arrive's, so the rest of the run keeps its sequence.
            var cell = pick[(int)(Next() % (uint)pick.Count)];
            report.cells = depth[cell];
            Appear(world.CellCenter(cell), null);
        }
        else
        {
            if (!Arrive(playerFeet, playerEye, playerForward)) return false;
            report.rung = 4;
            report.cells = depth.TryGetValue(world.CellOf(position), out var d4) ? d4 : -1;
        }
        report.cell = world.CellOf(position);
        report.straight = Flat(position - playerFeet).magnitude;
        report.margin = byField ? sourceField.DistanceTo(report.cell) - playerField : float.NaN;
        report.farSide = FarSide(report.cell, playerField, byField, playerFeet, playerForward);
        report.station = hasLayout;
        report.flow = !hasLayout && byField;
        report.unseen = UnseenAt(position, playerEye);
        return true;
    }

    /// <summary>A40: the player's route to the FLOW target the last generic first send kept clear (empty on the station's layout or with no target in reach).</summary>
    public IReadOnlyList<GridCoord> LastFlowRoute => flowRoute;

    // A40: the current FLOW target, as the wayfinding has it: the station's TV (its beacon cell) while the station's key is
    // not taken, else N (the EXIT's front) when the floor has an EXIT. False with neither.
    bool FlowTarget(out GridCoord target)
    {
        target = default;
        if (world.FloorHasStation && world.HasStation && !world.StationKeyIsTaken) { target = world.StationBeaconCell; return true; }
        if (world.HasExitArea) { target = world.ExitFrontCell; return true; }
        return false;
    }

    // A40: the player's route down sourceField (built from the FLOW target) to the target: steepest descent over the 8
    // neighbours from the player's cell, at most 64 cells. FirstSendExcluded keeps the ladder 2 cells off it.
    void FlowRoute(GridCoord from)
    {
        var cell = from;
        var here = sourceField.DistanceTo(cell);
        for (var i = 0; i < 64 && !float.IsPositiveInfinity(here); i++)
        {
            flowRoute.Add(cell);
            if (here <= 0f) break;
            var best = cell;
            var bestMetres = here;
            for (var dy = -1; dy <= 1; dy++)
            for (var dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0) continue;
                var next = new GridCoord(cell.x + dx, cell.y + dy);
                var metres = sourceField.DistanceTo(next);
                if (metres < bestMetres) { bestMetres = metres; best = next; }
            }
            if (best == cell) break;
            cell = best;
            here = bestMetres;
        }
    }

    // Rung 1's side. With a field at the player (the station's exit field from N, plan 4.10; or, off the layout, A40's FLOW
    // field from its target): the far side, the field's metres at the cell at least FarSideMetres beyond the player's (past
    // the field's reach is far). Otherwise (no target, or the player past the field's reach): behind the player,
    // dot(cell's centre - feet, the player's look) < 0 on the flat, Arrive's own behind test.
    bool FarSide(GridCoord cell, float playerField, bool byField, Vector3 playerFeet, Vector3 playerForward) =>
        byField ? sourceField.DistanceTo(cell) >= playerField + FrontRoomsStartGroup.FarSideMetres
            : Vector3.Dot(Flat(world.CellCenter(cell) - playerFeet), Flat(playerForward)) < 0f;

    // Never a first-send cell. Always: the station and its ring (any Arrive's rule), the start area, a stairwell (X, or the
    // one-cell start area the floor began in) and any cell within 2 (Chebyshev) of a stairwell cell. With the station's
    // layout (plan 4.10) also: the strip (the canonical R* rows), the plan's X and its band, and any cell within 2 of the
    // walk D..N.
    bool FirstSendExcluded(bool hasLayout, StartGroupLayout l, GridCoord cell)
    {
        if (IsArriveExcluded(cell) || world.InStartArea(cell) || world.InStairwell(cell)) return true;
        for (var dy = -2; dy <= 2; dy++)
        for (var dx = -2; dx <= 2; dx++)
            if (world.InStairwell(new GridCoord(cell.x + dx, cell.y + dy))) return true;
        if (!hasLayout)
        {
            // A40: 2 cells off the player's route to the FLOW target (C32's walk back, re-anchored).
            for (var i = 0; i < flowRoute.Count; i++)
                if (Mathf.Abs(cell.x - flowRoute[i].x) <= 2 && Mathf.Abs(cell.y - flowRoute[i].y) <= 2) return true;
            return false;
        }
        if (FrontRoomsExitPlanner.InAreaOrRing(l.station, cell) || l.InStrip(cell) || cell == l.exit) return true;
        if (Mathf.Abs(cell.x - l.exit.x) <= 2 && Mathf.Abs(cell.y - l.exit.y) <= 2) return true;
        var walk = world.StationExitWalk;
        for (var i = 0; i < walk.Count; i++)
            if (Mathf.Abs(cell.x - walk[i].x) <= 2 && Mathf.Abs(cell.y - walk[i].y) <= 2) return true;
        return false;
    }

    // Neither its eye nor its head in the player's sight from where it stands (the report's check at the placement).
    bool UnseenAt(Vector3 feet, Vector3 playerEye) =>
        !Visible(playerEye, feet + Vector3.up * EyeHeight) && !Visible(playerEye, feet + Vector3.up * ProbeTop);

    /// <summary>
    /// Walk to a random place 6–14 cells away that it can reach without
    /// breaking a door; nowhere to go, it keeps listening where it is.
    /// </summary>
    void Wander(GridCoord from)
    {
        Search(from, WanderMaxCells, false);
        candidates.Clear();
        foreach (var pair in depth) if (pair.Value >= WanderMinCells) candidates.Add(pair.Key);
        if (candidates.Count == 0) foreach (var pair in depth) if (pair.Value >= 2) candidates.Add(pair.Key);
        if (candidates.Count == 0)
        {
            SetState(HunterState.Listen);
            return;
        }
        var to = candidates[(int)(Next() % (uint)candidates.Count)];
        Plan(from, to, world.CellCenter(to), false);
        SetState(HunterState.Wander);
    }

    /// <summary>
    /// Breadth-first search over built cells. Doors count as passable (the
    /// Relay breaks them); unbroken glass and walls do not.
    /// </summary>
    /// <param name="throughDoors">Shut doors count as passable (it breaks them); off for wandering and searching.</param>
    void Search(GridCoord start, int maxDepth, bool throughDoors)
    {
        cameFrom.Clear();
        depth.Clear();
        frontier.Clear();
        depth[start] = 0;
        frontier.Enqueue(start);
        while (frontier.Count > 0)
        {
            var cell = frontier.Dequeue();
            var d = depth[cell];
            if (d >= maxDepth) continue;
            foreach (var s in Steps)
            {
                var next = cell + s;
                if (depth.ContainsKey(next) || !world.IsBuilt(next)) continue;
                var passage = world.PassageBetween(cell, next);
                if (passage == FrontRoomsMapWorld.Passage.Wall || passage == FrontRoomsMapWorld.Passage.Glass) continue;
                if (!throughDoors && passage == FrontRoomsMapWorld.Passage.ClosedDoor) continue;
                depth[next] = d + 1;
                cameFrom[next] = cell;
                frontier.Enqueue(next);
            }
        }
    }

    int Distance(GridCoord from, GridCoord to, int cap)
    {
        Search(from, cap, true);
        return depth.TryGetValue(to, out var d) ? d : int.MaxValue;
    }

    /// <summary>
    /// C22 (plan v2 D3): X, the EXIT's stairwell cell, is no map to the Relay, so no goal may lie in it. A goal
    /// cell or point there (the player seen or last seen inside, a door it watched them take into it, a noise in
    /// it) becomes N's side of the EXIT door, where it would stand to face that door: as close as the map lets it
    /// come. Search never reaches X, so without this a plan to X would end empty and the walk go straight at the
    /// goal, through the open doorway; inside X no Arrive could relay it out again (there is no map round it to
    /// search). It never sees a player in X (the safe zone), so it never catches one there, even in the doorway.
    /// </summary>
    void KeepOutOfExit(ref GridCoord cell, ref Vector3 point)
    {
        if (!world.InExitArea(cell) && !world.InExitArea(world.CellOf(point))) return;
        var front = world.ExitFrontCell;
        var exit = world.ExitCell;
        cell = front;
        point = world.CrossingPoint(front, exit) - Flat(world.CellCenter(exit) - world.CellCenter(front)).normalized * BreakFaceDistance;
    }

    /// <summary>Plan the cells to walk to <paramref name="to"/>. False when no route reaches it (the path is left empty); from == to is a route of no steps.</summary>
    bool Plan(GridCoord from, GridCoord to, Vector3 finalPoint, bool throughDoors = true)
    {
        KeepOutOfExit(ref to, ref finalPoint);
        goal = finalPoint;
        planThroughDoors = throughDoors;
        path.Clear();
        pathIndex = 0;
        if (from == to) return true;
        Search(from, 64, throughDoors);
        if (!depth.ContainsKey(to)) return false;
        for (var c = to; c != from; c = cameFrom[c]) path.Add(c);
        path.Reverse();
        return true;
    }

    /// <summary>
    /// Plan again from where it stands to the same goal, by the same door rule
    /// as the plan it replaces. False when a plan that keeps to shut doors has
    /// no way left: that leg is over. A plan through doors never ends here; with
    /// no route it walks straight at the goal, as before.
    /// </summary>
    bool Replan(GridCoord here) => Plan(here, path[path.Count - 1], goal, planThroughDoors) || planThroughDoors;

    /// <summary>
    /// Walk the planned path. Returns true once the goal is reached, or when a
    /// wander or search leg (a plan that keeps to shut doors) finds a door
    /// shut across its way and no way round it.
    /// </summary>
    bool Follow(float speed, float dt)
    {
        if (pathIndex >= path.Count) return MoveDirect(goal, speed, dt, true);
        var here = world.CellOf(position);
        var next = path[pathIndex];
        if (here == next)
        {
            pathIndex++;
            return pathIndex >= path.Count && MoveDirect(goal, speed, dt, true);
        }
        if (hasLeg && legTo == next && detourIndex < detour.Count) here = legFrom;
        else
        {
            legFrom = here;
            legTo = next;
            hasLeg = true;
        }
        if (Mathf.Abs(here.x - next.x) + Mathf.Abs(here.y - next.y) != 1)
        {
            // A detour or a pass-through left it off its path: plan again from where it stands.
            return !Replan(here);
        }
        var passage = world.PassageBetween(here, next);
        if (passage == FrontRoomsMapWorld.Passage.ClosedDoor)
        {
            // A wander or a search never breaks a door: one shut across its way since
            // it planned (the player shut it) it walks round, or the leg is over.
            if (!planThroughDoors) return !Replan(here);
            // Walk up to the door on this side, then break it down.
            var door = world.DoorBetween(here, next);
            if (door == null)
            {
                // The chunk that owns this door was dropped under the path: plan again over what is built.
                return !Replan(here);
            }
            if (door.broken || door.open)
            {
                // Already broken, or opened by the player (A12: a leaf opened from the far side swings toward it),
                // its leaf still swinging or resting on a body short of passable: wait at the stance, out of the
                // leaf's sweep when it is on this side, no blows. It walks through once the leaf swings clear.
                MoveDirect(BreakStance(door, here, next, out _), speed, dt);
                return false;
            }
            if (MoveDirect(BreakStance(door, here, next, out var facing), speed, dt))
            {
                breakingDoor = door;
                breakFacing = facing;
                // A12: a shut leaf goes away from it, so the blows jolt it that way and it bursts into the next room.
                world.SwingAwayFrom(door, position);
                world.MarkBeingBroken(door, true);
                blowsStruck = 0;
                BlowIndex = -1;
                breakSeconds = Mathf.Max(.05f, tuning.breakDoorSeconds);
                resumeState = State;
                SetState(HunterState.BreakDoor);
            }
            return false;
        }
        if (passage != FrontRoomsMapWorld.Passage.Open)
        {
            // The map changed under the path (a wall where a door was): plan again.
            return !Replan(here);
        }
        // Cross at the opening itself (doorways sit off-centre), aiming a
        // little past the edge so the next step starts inside the next cell.
        MoveDirect(world.CrossingPoint(here, next) + Flat(world.CellCenter(next) - world.CellCenter(here)).normalized * .35f, speed, dt, false, next);
        return false;
    }

    /// <summary>
    /// Walk toward a point. Returns true on arrival. With <paramref name="enter"/>
    /// the leg's purpose is to step into that cell (a crossing), so a detour
    /// may end anywhere inside it. A <paramref name="final"/> point that
    /// furniture covers counts as reached once it stands as close as it can,
    /// so it searches beside a desk rather than inside it.
    /// </summary>
    bool MoveDirect(Vector3 target, float speed, float dt, bool final = false, GridCoord? enter = null)
    {
        target.y = position.y;
        if (Flat(target - planTarget).sqrMagnitude > .25f) Steer(target, final, enter);
        else if (detour.Count == 0 && (straightCheck -= dt) <= 0f) Steer(target, final, enter);
        var onDetour = detourIndex < detour.Count;
        var aim = onDetour ? detour[detourIndex] : target;
        aim.y = position.y;
        // Doors swing and the player moves: keep checking the leg it is on.
        if (onDetour && (straightCheck -= dt) <= 0f)
        {
            straightCheck = StraightRecheck;
            var replan = !PathClear(position, aim, !ghosting);
            if (ghosting && !replan)
            {
                // Once past what it had to pass through, walk round the rest again.
                if (!BodyFits(position, true)) ghostTouched = true;
                else if (ghostTouched) replan = true;
            }
            if (replan)
            {
                Steer(target, final, enter);
                onDetour = detourIndex < detour.Count;
                aim = onDetour ? detour[detourIndex] : target;
                aim.y = position.y;
            }
        }
        if (unreachable)
        {
            // Nothing free near a final point: stop here, the hunt is over.
            unreachable = false;
            return true;
        }
        position = Vector3.MoveTowards(position, aim, speed * dt);
        if (onDetour && Flat(position - aim).sqrMagnitude < .0004f && ++detourIndex >= detour.Count)
        {
            detour.Clear();
            detourIndex = 0;
            straightCheck = 0f;
            if (ghosting && BodyFits(position, true)) ghosting = false;
            // The aim itself is covered: beside it is as close as it gets (a hunt's end, or a door face).
            if (detourShort && (final || !enter.HasValue)) return true;
        }
        return Flat(position - target).sqrMagnitude < .04f * .04f;
    }

    /// <summary>Hunt watchdog: true once it has moved less than 0.3 m for <see cref="StallSeconds"/>.</summary>
    bool Stalled(float dt)
    {
        if (Flat(position - progressAnchor).sqrMagnitude > .09f)
        {
            progressAnchor = position;
            progressTime = 0f;
            return false;
        }
        progressTime += dt;
        return progressTime > StallSeconds;
    }

    /// <summary>
    /// The cell a new route should start from. Mid-detour on a crossing leg
    /// the body may stand in a neighbouring cell; the route is still at the
    /// leg's start, as Follow treats it.
    /// </summary>
    GridCoord RouteCell(GridCoord body) =>
        hasLeg && detourIndex < detour.Count && pathIndex < path.Count && path[pathIndex] == legTo && body != legTo ? legFrom : body;

    void ResetSteering()
    {
        detour.Clear();
        detourIndex = 0;
        detourShort = unreachable = ghosting = ghostTouched = hasLeg = false;
        planTarget = new Vector3(float.MaxValue, 0f, 0f);
        straightCheck = 0f;
    }

    /// <summary>
    /// Decide how to reach the aim: straight if the body fits the whole way,
    /// else a detour round the furniture, else through the furniture on a
    /// route that still keeps out of walls. Standing inside furniture (after
    /// passing through, or when a door swings into it) also means passing through.
    /// </summary>
    void Steer(Vector3 target, bool final, GridCoord? enter)
    {
        planTarget = target;
        detour.Clear();
        detourIndex = 0;
        detourShort = unreachable = false;
        straightCheck = StraightRecheck;
        var inside = !BodyFits(position, true);
        if (!inside)
        {
            ghosting = false;
            if (Flat(target - position).sqrMagnitude < .0004f || PathClear(position, target, true)) return;
            if (PlanDetour(target, enter, true)) return;
            if (final)
            {
                unreachable = true;
                return;
            }
            Ghosts++;
            DebugBlocker = Blocker(position, target);
        }
        ghosting = true;
        ghostTouched = inside;
        if (!PathClear(position, target, false)) PlanDetour(target, enter, false);
    }

    /// <summary>
    /// A* on a 0.25 m grid over the cell the body is in and the aim's cell (or
    /// <paramref name="enter"/>), with one cell of margin round them. A node
    /// is free when the body fits there; with <paramref name="furniture"/>
    /// off only walls, columns, doors and glass count. The result is
    /// string-pulled into a few straight legs.
    /// </summary>
    bool PlanDetour(Vector3 target, GridCoord? enter, bool furniture)
    {
        var from = world.CellOf(position);
        var to = enter ?? world.CellOf(target);
        if (Mathf.Abs(from.x - to.x) > 1 || Mathf.Abs(from.y - to.y) > 1) to = from;
        var minCell = new GridCoord(Mathf.Min(from.x, to.x) - 1, Mathf.Min(from.y, to.y) - 1);
        var maxCell = new GridCoord(Mathf.Max(from.x, to.x) + 1, Mathf.Max(from.y, to.y) + 1);
        var cols = (maxCell.x - minCell.x + 1) * NodesPerCell;
        var rows = (maxCell.y - minCell.y + 1) * NodesPerCell;
        var count = cols * rows;
        var corner = world.CellCenter(minCell) - new Vector3(MapGrid.CellSize * .5f, 0f, MapGrid.CellSize * .5f);
        corner.y = position.y;
        Vector3 Node(int k) => corner + new Vector3((k % cols + .5f) * NodeStep, 0f, (k / cols + .5f) * NodeStep);
        bool Free(int k)
        {
            if (nodeFree[k] == 0)
            {
                var p = Node(k);
                nodeFree[k] = (sbyte)(world.IsBuilt(world.CellOf(p)) && BodyFits(p, furniture) ? 1 : -1);
            }
            return nodeFree[k] > 0;
        }
        bool Goal(int k)
        {
            if (enter.HasValue) return world.CellOf(Node(k)) == enter.Value;
            return Flat(Node(k) - target).sqrMagnitude <= .3f * .3f;
        }
        for (var k = 0; k < count; k++)
        {
            nodeFree[k] = 0;
            nodeCost[k] = float.MaxValue;
            nodeParent[k] = -1;
            nodeClosed[k] = false;
        }

        // Start from the free node nearest the body.
        var local = position - corner;
        int sx = Mathf.Clamp(Mathf.FloorToInt(local.x / NodeStep), 0, cols - 1), sz = Mathf.Clamp(Mathf.FloorToInt(local.z / NodeStep), 0, rows - 1);
        var start = -1;
        var startDistance = float.MaxValue;
        for (var dz = -3; dz <= 3; dz++)
        for (var dx = -3; dx <= 3; dx++)
        {
            int x = sx + dx, z = sz + dz;
            if (x < 0 || z < 0 || x >= cols || z >= rows) continue;
            var k = x + z * cols;
            var d = Flat(Node(k) - position).sqrMagnitude;
            if (d < startDistance && Free(k)) { start = k; startDistance = d; }
        }
        if (start < 0) return false;

        heapNode.Clear();
        heapKey.Clear();
        nodeCost[start] = 0f;
        Push(start, Flat(Node(start) - target).magnitude);
        var reached = -1;
        var best = start;
        var bestDistance = float.MaxValue;
        while (heapNode.Count > 0)
        {
            var k = Pop();
            if (nodeClosed[k]) continue;
            nodeClosed[k] = true;
            var distance = Flat(Node(k) - target).magnitude;
            if (distance < bestDistance) { bestDistance = distance; best = k; }
            if (Goal(k)) { reached = k; break; }
            int x = k % cols, z = k / cols;
            for (var dz = -1; dz <= 1; dz++)
            for (var dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dz == 0) continue;
                int nx = x + dx, nz = z + dz;
                if (nx < 0 || nz < 0 || nx >= cols || nz >= rows) continue;
                var n = nx + nz * cols;
                if (nodeClosed[n] || !Free(n)) continue;
                // No cutting a corner past a blocked node.
                if (dx != 0 && dz != 0 && (!Free(nx + z * cols) || !Free(x + nz * cols))) continue;
                var cost = nodeCost[k] + (dx != 0 && dz != 0 ? NodeStep * 1.4142f : NodeStep);
                if (cost >= nodeCost[n]) continue;
                nodeCost[n] = cost;
                nodeParent[n] = k;
                Push(n, cost + Flat(Node(n) - target).magnitude);
            }
        }
        // A final aim that no node reaches: settle for the closest free spot.
        if (reached < 0)
        {
            if (enter.HasValue || best == start) return false;
            reached = best;
            detourShort = true;
        }

        nodePath.Clear();
        for (var k = reached; k >= 0; k = nodeParent[k]) nodePath.Add(k);
        nodePath.Reverse();
        // String-pull: from where it stands, jump to the farthest node it can walk
        // to straight. The body is off the node grid, so its first leg is
        // checked down to the start node; from a node the next one is always
        // walkable (both free, no cut corner).
        var at = position;
        var i0 = -1;
        while (i0 < nodePath.Count - 1)
        {
            var next = -1;
            for (var j = nodePath.Count - 1; j > i0; j--)
                if ((i0 >= 0 && j == i0 + 1) || PathClear(at, Node(nodePath[j]), furniture)) { next = j; break; }
            if (next < 0)
            {
                detour.Clear();
                detourShort = false;
                return false;
            }
            at = Node(nodePath[next]);
            detour.Add(at);
            i0 = next;
        }
        if (!detourShort && !enter.HasValue)
        {
            if (PathClear(at, target, furniture)) detour.Add(target);
            else detourShort = true; // the aim is covered: the goal node beside it is as close as it gets
        }
        return true;
    }

    void Push(int node, float key)
    {
        heapNode.Add(node);
        heapKey.Add(key);
        var i = heapNode.Count - 1;
        while (i > 0)
        {
            var parent = (i - 1) / 2;
            if (heapKey[parent] <= heapKey[i]) break;
            Swap(i, parent);
            i = parent;
        }
    }

    int Pop()
    {
        var top = heapNode[0];
        var last = heapNode.Count - 1;
        Swap(0, last);
        heapNode.RemoveAt(last);
        heapKey.RemoveAt(last);
        var i = 0;
        while (true)
        {
            int l = i * 2 + 1, r = l + 1, m = i;
            if (l < heapNode.Count && heapKey[l] < heapKey[m]) m = l;
            if (r < heapNode.Count && heapKey[r] < heapKey[m]) m = r;
            if (m == i) break;
            Swap(i, m);
            i = m;
        }
        return top;
    }

    void Swap(int a, int b)
    {
        (heapNode[a], heapNode[b]) = (heapNode[b], heapNode[a]);
        (heapKey[a], heapKey[b]) = (heapKey[b], heapKey[a]);
    }

    /// <summary>
    /// True when the body can walk straight from a to b without touching
    /// anything but the player or its rig (with <paramref name="furniture"/>
    /// off, only walls, columns, doors and glass count).
    /// </summary>
    bool PathClear(Vector3 a, Vector3 b, bool furniture)
    {
        var dir = Flat(b - a);
        var distance = dir.magnitude;
        if (distance < 1e-4f) return true;
        dir /= distance;
        var count = Physics.CapsuleCastNonAlloc(a + Vector3.up * (ProbeBottom + Radius), a + Vector3.up * (ProbeTop - Radius), Radius, dir, bodyHits, distance, ~0, QueryTriggerInteraction.Ignore);
        // Hits come unordered: a full buffer may have dropped the one that matters.
        if (count >= bodyHits.Length) return false;
        for (var i = 0; i < count; i++)
        {
            var c = bodyHits[i].collider;
            if (Ignored(c, furniture)) continue;
            // A collider it already stands in (distance 0) does not stop it walking out.
            if (bodyHits[i].distance <= 0f) continue;
            return false;
        }
        return true;
    }

    /// <summary>True when the body standing here would touch nothing but the player or its own rig.</summary>
    bool BodyFits(Vector3 feet, bool furniture = true)
    {
        var count = Physics.OverlapCapsuleNonAlloc(feet + Vector3.up * (ProbeBottom + Radius), feet + Vector3.up * (ProbeTop - Radius), Radius, overlaps, ~0, QueryTriggerInteraction.Ignore);
        for (var i = 0; i < count; i++)
            if (!Ignored(overlaps[i], furniture)) return false;
        return true;
    }

    /// <summary>Tools: names what the straight leg hits, for reports.</summary>
    string Blocker(Vector3 a, Vector3 b)
    {
        var dir = Flat(b - a);
        var distance = dir.magnitude;
        if (distance < 1e-4f) return "";
        var count = Physics.CapsuleCastNonAlloc(a + Vector3.up * (ProbeBottom + Radius), a + Vector3.up * (ProbeTop - Radius), Radius, dir / distance, bodyHits, distance, ~0, QueryTriggerInteraction.Ignore);
        var names = "";
        for (var i = 0; i < count; i++)
            if (!Ignored(bodyHits[i].collider, true) && bodyHits[i].distance > 0f)
                names += bodyHits[i].collider.name + (bodyHits[i].collider.transform.parent != null ? "<" + bodyHits[i].collider.transform.parent.name : "") + "@" + bodyHits[i].distance.ToString("F2") + " ";
        return names;
    }

    bool Ignored(Collider c, bool furniture) =>
        c == null || c == playerCollider || (rig != null && c.transform.IsChildOf(rig)) || (!furniture && !world.IsArchitecture(c));

    /// <summary>
    /// Where to stand to break a shut door from cell <paramref name="here"/>,
    /// and which way to face. It shoulders the door in front of the opening,
    /// and the leaf bursts away from it: a leaf in the frame always goes to
    /// the far side (A12). Only a leaf caught partly open on its side comes
    /// toward it: then it stands beside the latch, out of the leaf's sweep,
    /// facing the lock, and rips it open.
    /// </summary>
    Vector3 BreakStance(FrontRoomsMapWorld.Door door, GridCoord here, GridCoord next, out Vector3 facing)
    {
        var into = Flat(world.CellCenter(next) - world.CellCenter(here)).normalized;
        var face = world.CrossingPoint(here, next) - into * BreakFaceDistance;
        facing = into;
        if (FrontRoomsMapWorld.IsLatched(door) || !FrontRoomsMapWorld.OnSwingSide(door, world.CellCenter(here))) return face;
        // The body plus 5 cm: MoveDirect calls a point reached 4 cm short of it.
        var stance = FrontRoomsMapWorld.ClearOfSwing(door, face, Radius + .05f);
        var toLock = Flat(FrontRoomsMapWorld.LockPoint(door, stance) - stance);
        if (toLock.sqrMagnitude > 1e-4f) facing = toLock.normalized;
        return stance;
    }

    // It leaves the door it was breaking (relayed, placed, or the door gave or opened).
    void StopBreaking()
    {
        if (breakingDoor != null) world.MarkBeingBroken(breakingDoor, false);
        breakingDoor = null;
    }

    void TickBreak(float dt)
    {
        // Opened under it (it was already swinging when the break began): nothing to break, walk on through.
        // A12: a leaf opened from the far side comes toward it and may rest on its body short of passable; it
        // is open all the same, so it stops and steps clear of the sweep (Follow) instead of breaking it.
        // Either way it goes back to what it was doing when it began (a hunt or a chase: nothing else breaks doors).
        if (breakingDoor != null && !breakingDoor.broken && (breakingDoor.open || world.PassageBetween(breakingDoor.a, breakingDoor.b) == FrontRoomsMapWorld.Passage.Open))
        {
            StopBreaking();
            SetState(resumeState);
            return;
        }
        // Blows every 0.5 s from 0.5 s in (0, 1, ... BlowCount - 2); the last one
        // (BlowCount - 1) a beat before the door gives at breakDoorSeconds.
        // Each jolts the leaf toward its swing side, away from the Relay since A12 (the map's picture of the blow).
        var final = BlowCount - 1;
        var due = StateTime + TimeSlack >= BreakSeconds - FinalBlowLead ? final : Mathf.Min(final - 1, Mathf.FloorToInt((StateTime + TimeSlack) / BlowInterval) - 1);
        while (breakingDoor != null && blowsStruck <= due)
        {
            BlowIndex = blowsStruck++;
            DoorBlow?.Invoke(breakingDoor.position);
            // 20_design §6.1: a blow the player hears (BlowHeard).
            if (Vector3.Distance(breakingDoor.position, sightEye) <= BlowAudibleMetres) blowHeardAt = clock;
            // Again at every blow, not only as the break begins: a leaf still shutting then (out of the frame, so it kept
            // its side) latches during the break and must then jolt away like any shut leaf (A12 review, finding 4).
            world.SwingAwayFrom(breakingDoor, position);
            world.JoltForBlow(breakingDoor, BlowIndex, BlowCount);
        }
        if (StateTime + TimeSlack < BreakSeconds) return;
        if (breakingDoor != null)
        {
            // The break the player hears ends BlowHeard at once (the door gave).
            if (Vector3.Distance(breakingDoor.position, sightEye) <= BreakAudibleMetres) breakHeardAt = clock;
            world.BreakDoor(breakingDoor, position);
            DoorsBroken++;
            breakingDoor = null;
        }
        if (StateTime + TimeSlack < BreakSeconds + DoorFallSeconds) return;
        SetState(resumeState);
    }

    /// <summary>How far into an open or broken door's opening a point is: 1 on the door line, 0 from 0.6 m out or beside the 1.0 m opening.</summary>
    float DoorSqueezeAt(Vector3 p)
    {
        var cell = world.CellOf(p);
        var best = 0f;
        foreach (var d in Steps)
        {
            var next = new GridCoord(cell.x + d.x, cell.y + d.y);
            if (world.Cache.Edge(cell, next) != EdgeKind.Door) continue;
            var door = world.DoorBetween(cell, next);
            if (door == null || !(door.open || door.broken)) continue;
            var normal = new Vector3(d.x, 0f, d.y);
            var offset = Flat(p - door.position);
            var across = Vector3.Dot(offset, normal);
            if ((offset - normal * across).magnitude > ModuleUnits.DoorWidth * .5f) continue;
            best = Mathf.Max(best, 1f - Mathf.Abs(across) / SqueezeReach);
        }
        return Mathf.Clamp01(best);
    }

    void EnsurePlayerField(GridCoord playerCell)
    {
        var revision = world.PassageRevision;
        if (playerFieldReady && playerFieldRoot == playerCell && playerFieldRevision == revision) return;
        playerField.Build(world, playerCell);
        playerFieldRoot = playerCell;
        playerFieldRevision = revision;
        playerFieldReady = true;
    }

    void EnsureRelayField()
    {
        var root = world.CellOf(position);
        var revision = world.PassageRevision;
        if (relayFieldReady && relayFieldRoot == root && relayFieldRevision == revision) return;
        relayField.Build(world, root);
        relayFieldRoot = root;
        relayFieldRevision = revision;
        relayFieldReady = true;
    }

    bool TrySourceEdge(Vector3 source, GridCoord sourceCell, out GridCoord? edgeCell)
    {
        edgeCell = null;
        if (!world.IsBuilt(sourceCell)) return false;
        var best = float.PositiveInfinity;
        foreach (var step in Steps)
        {
            var next = new GridCoord(sourceCell.x + step.x, sourceCell.y + step.y);
            if (!world.IsBuilt(next)) continue;
            var edge = world.Cache.Edge(sourceCell, next);
            if (edge != EdgeKind.Door && edge != EdgeKind.Window) continue;
            var d = Flat(world.CrossingPoint(sourceCell, next) - source).sqrMagnitude;
            if (d < best)
            {
                best = d;
                edgeCell = next;
            }
        }
        return edgeCell.HasValue;
    }

    void ResetWarningMetrics()
    {
        PathDistanceToPlayer = float.PositiveInfinity;
        PathOpeningCost = float.PositiveInfinity;
        WarningDistance = float.PositiveInfinity;
        ClearWarnStage();
    }

    /// <summary>Off the map (not released, or on an unbuilt cell) is stage 0 at once, with no hold.</summary>
    void UpdateWarnStage(float dt, GridCoord myCell) =>
        StepWarnStage(dt, Released && world.IsBuilt(myCell) ? WarningDistance : float.PositiveInfinity);

    /// <summary>Tests: one sample of W as a tick takes it (infinity = off the map), without a map or a release.</summary>
    public void DebugWarnSample(float w, float dt)
    {
        WarningDistance = w;
        StepWarnStage(dt, w);
    }

    /// <summary>
    /// §9.1. A stage starts on the first sample inside its band, and going up is never delayed (0 → 2 and 1 → 2 at once).
    /// Once in, stage 1 lasts ≥ 4 s and stage 2 ≥ 3 s from entry; after that a stage ends once W has stayed past its exit
    /// band for 1 s. Stage 2 drops to 1, or to 0 if W has also stayed past 36 m for that second.
    /// </summary>
    void StepWarnStage(float dt, float w)
    {
        if (float.IsPositiveInfinity(w))
        {
            ClearWarnStage();
            return;
        }
        var band = w <= WarnStage2Enter ? 2 : w <= WarnStage1Enter ? 1 : 0;
        if (band > WarnStage)
        {
            EnterWarnStage(band);
            return;
        }
        if (WarnStage == 0) return;
        warnStageSeconds += dt;
        WarnRunSeconds += dt;
        if (WarnStage == 2) WarnStage2RunSeconds += dt;
        warnPastExit = w > (WarnStage == 2 ? WarnStage2Exit : WarnStage1Exit) ? warnPastExit + dt : 0f;
        warnPastClear = w > WarnStage1Exit ? warnPastClear + dt : 0f;
        if (warnStageSeconds < (WarnStage == 2 ? WarnStage2MinSeconds : WarnStage1MinSeconds) || warnPastExit < WarnExitConfirmSeconds) return;
        EnterWarnStage(WarnStage == 2 && warnPastClear >= WarnExitConfirmSeconds ? 0 : WarnStage - 1);
    }

    /// <summary>Into a stage, up or down: its minimum time starts again (a 2 → 1 drop restarts stage 1's 4 s).</summary>
    void EnterWarnStage(int next)
    {
        if (next == 0 || WarnStage == 0) WarnRunSeconds = 0f;
        WarnStage2RunSeconds = 0f;
        warnStageSeconds = 0f;
        warnPastExit = 0f;
        warnPastClear = 0f;
        SetWarnStage(next);
    }

    void ClearWarnStage() => EnterWarnStage(0);

    void SetWarnStage(int next)
    {
        next = Mathf.Clamp(next, 0, 2);
        if (WarnStage == next) return;
        WarnStage = next;
        WarnStageChanged?.Invoke(next);
    }

    /// <summary>
    /// The 10 Hz refresh of RELAY §8's platform-identical tests from the last tick's player inputs: PlayerSeesRelay on the
    /// reference frustum (both points must be clear, so a shoulder-level glimpse through furniture or over a low sill is
    /// not sight) and RelayLit; RelayOnScreen's cache lapses with it.
    /// </summary>
    void RefreshSight()
    {
        playerSeesRelayTimer = PlayerSeesRelayPeriod;
        sightTick++;
        if (!Released || world == null)
        {
            PlayerSeesRelay = RelayLit = false;
            return;
        }
        PlayerSeesRelay = RelayInView(FrontRoomsRelayFrustum.Reference(sightEye, sightForward, world.SightDistance));
        RelayLit = LitBy(Flat(position - sightFeet).magnitude, LampW(world, world.CellOf(position)));
    }

    /// <summary>
    /// 20_design §6.1, the Spotting term's hold: Spotting while its steps are audible hides a prompt; after it ends the
    /// prompt stays hidden until the steps fall silent, or start again after the Relay has stood still. A lock-on
    /// (TargetAcquiredRecently) takes over from it, with its own hold (ChaseFading).
    /// </summary>
    void UpdateSpottingHold()
    {
        var threat = Threat;
        var audible = StepsAudible;
        if (threat.TargetAcquiredRecently) spotHeld = false;
        else if (threat.Spotting && audible)
        {
            spotHeld = true;
            spotStill = false;
        }
        else if (spotHeld)
        {
            if (!audible) spotHeld = false;
            else if (!Moving) spotStill = true;
            else if (spotStill) spotHeld = false;
        }
    }

    /// <summary>ThreatPerceivable's holds let go (Dormant, or placed by tools).</summary>
    void ClearThreatHolds()
    {
        chaseEndedAt = blowHeardAt = breakHeardAt = float.NegativeInfinity;
        spotHeld = spotStill = false;
    }

    bool Sees(Vector3 playerEye)
    {
        var eye = position + Vector3.up * EyeHeight;
        if ((playerEye - eye).magnitude > tuning.sightRange) return false;
        return Visible(eye, playerEye);
    }

    /// <summary>
    /// True when nothing but the player (or nothing at all) lies between the
    /// two points. Hits on the Relay's own rig are ignored.
    /// </summary>
    bool Visible(Vector3 from, Vector3 to)
    {
        var dir = to - from;
        var distance = dir.magnitude;
        if (distance < .01f) return true;
        var count = Physics.RaycastNonAlloc(from, dir / distance, hits, distance, ~0, QueryTriggerInteraction.Ignore);
        var nearest = float.MaxValue;
        Collider blocker = null;
        for (var i = 0; i < count; i++)
        {
            var c = hits[i].collider;
            if (c == null || (rig != null && c.transform.IsChildOf(rig))) continue;
            if (hits[i].distance < nearest) { nearest = hits[i].distance; blocker = c; }
        }
        return blocker == null || blocker == playerCollider;
    }

    void SetState(HunterState next)
    {
        progressAnchor = position;
        progressTime = 0f;
#if FRONTROOMS_RELAY_V2
        // A Listen's meaning (the step-in's, the give-up beat's) is set by its caller after SetState.
        listenKindV2 = ListenKindV2.Plain;
#endif
        if (next == State) { StateTime = 0f; return; }
        // 20_design §6.1: a chase's end starts ThreatPerceivable's fade hold; Dormant (parked, or before the release) drops every hold.
        if (State == HunterState.Chase) chaseEndedAt = clock;
        if (next == HunterState.Dormant) ClearThreatHolds();
        State = next;
        StateTime = 0f;
        StateChanged?.Invoke(next);
    }

    uint Next()
    {
        rng ^= rng << 13;
        rng ^= rng >> 17;
        rng ^= rng << 5;
        return rng;
    }

    static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
}

/// <summary>
/// RELAY §8 (amended 2026-10-14; V2, 2026-10-29): a view frustum for FrontRoomsMapHunter.RelayInView. Reference is the
/// platform-identical one PlayerSeesRelay uses (vertical 76°, 16:9, about ±54° across, the eye's real yaw and pitch,
/// out to SightDistance); Of is a device's own render camera (its FOV, aspect, clip planes and pose), for RelayOnScreen.
/// </summary>
public readonly struct FrontRoomsRelayFrustum
{
    /// <summary>The reference frustum: the game's authored 76° vertical at 16:9, and the game camera's near plane.</summary>
    public const float ReferenceFov = 76f, ReferenceAspect = 16f / 9f, ReferenceNear = .06f;

    public readonly Vector3 Eye;
    public readonly Quaternion Look;
    /// <summary>Vertical field of view in degrees; width over height; clip depths in metres along the look.</summary>
    public readonly float VerticalFov, Aspect, Near, Far;

    public FrontRoomsRelayFrustum(Vector3 eye, Quaternion look, float verticalFov, float aspect, float near, float far)
    {
        Eye = eye;
        Look = look;
        VerticalFov = verticalFov;
        Aspect = aspect;
        Near = near;
        Far = far;
    }

    /// <summary>The reference frustum at an eye looking along <paramref name="forward"/> (yaw and pitch; the game holds pitch within ±75°), no roll; along +Z for a forward with no flat part.</summary>
    public static FrontRoomsRelayFrustum Reference(Vector3 eye, Vector3 forward, float far)
    {
        var look = new Vector2(forward.x, forward.z).sqrMagnitude > 1e-8f ? Quaternion.LookRotation(forward, Vector3.up) : Quaternion.identity;
        return new FrontRoomsRelayFrustum(eye, look, ReferenceFov, ReferenceAspect, ReferenceNear, far);
    }

    /// <summary>A device's render camera as it stands: its pose, fieldOfView (vertical), aspect and clip planes.</summary>
    public static FrontRoomsRelayFrustum Of(Camera camera)
    {
        var t = camera.transform;
        return new FrontRoomsRelayFrustum(t.position, t.rotation, camera.fieldOfView, camera.aspect, camera.nearClipPlane, camera.farClipPlane);
    }

    /// <summary>The point projects inside the frustum: past the near plane, within the far one, within the half-angles across and up.</summary>
    public bool Contains(Vector3 point)
    {
        var local = Quaternion.Inverse(Look) * (point - Eye);
        if (local.z <= Near || local.z > Far) return false;
        var up = Mathf.Tan(VerticalFov * .5f * Mathf.Deg2Rad) * local.z;
        return Mathf.Abs(local.y) <= up && Mathf.Abs(local.x) <= up * Aspect;
    }
}

/// <summary>
/// RELAY §8 (V2, 2026-10-29): the hunter's one Threat reading (FrontRoomsMapHunter.Threat). Camera shots cancel on it,
/// "TargetAcquired or Spotting ≥ 0.5, not on any glimpse": FrontRoomsDoorShot.Threatened and the game's
/// rig.CancelForRelay read only this, so step V4b swaps its source (the lock-on and the spotting meter) without
/// touching them. Until then V1 fills it with v1's stand-ins and v1's numbers.
/// </summary>
public readonly struct FrontRoomsRelayThreat
{
    /// <summary>The v1 stand-in's Spotting radius: the unlock's old "sight within 6 m" (FrontRoomsShotTimings.Unlock.CancelRelayDistance).</summary>
    public const float SpottingStandInMetres = FrontRoomsShotTimings.Unlock.CancelRelayDistance;

    /// <summary>It acquired the player lately (v2: TargetAcquired, through the hold and the chase; v1: a chase is on, the window today's gates read).</summary>
    public readonly bool TargetAcquiredRecently;
    /// <summary>Its spotting meter is at 0.5 or more (v2); v1: it sees the player within SpottingStandInMetres, flat.</summary>
    public readonly bool Spotting;

    public FrontRoomsRelayThreat(bool targetAcquiredRecently, bool spotting)
    {
        TargetAcquiredRecently = targetAcquiredRecently;
        Spotting = spotting;
    }

    /// <summary>Either: what a camera shot cancels on.</summary>
    public bool Any => TargetAcquiredRecently || Spotting;

    /// <summary>v1's stand-ins: a chase is on; it sees the player within SpottingStandInMetres flat (<paramref name="flatMetres"/> below 0: not out).</summary>
    public static FrontRoomsRelayThreat V1(bool chase, bool sees, float flatMetres) =>
        new FrontRoomsRelayThreat(chase, sees && flatMetres >= 0f && flatMetres <= SpottingStandInMetres);

    public override string ToString() => (TargetAcquiredRecently ? "acquired" : "-") + "/" + (Spotting ? "spotting" : "-");
}
