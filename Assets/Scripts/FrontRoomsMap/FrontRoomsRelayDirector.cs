using System;
using UnityEngine;

/// <summary>How the hunter places a call (RELAY 5): Normal (Arrive v2, hard rules and soft rules a-f), Relaxed (the PENDING
/// fallback at 10 s: every soft rule dropped, W at least 36 m), FirstSend (the run's first dispatch, Red 2026-10-30: A26 M7's ladder, C32,
/// on floor 1's station or the generic ladder below it).</summary>
public enum FrontRoomsRelayPlacement
{
    Normal,
    Relaxed,
    FirstSend
}

/// <summary>
/// What FrontRoomsRelayDirector drives (RELAY 4: "the hunter owns senses, movement, the warning stage and Arrive placement").
/// FrontRoomsMapHunter implements it under FRONTROOMS_RELAY_V2 (FrontRoomsMapHunter.V2.cs); EditMode tests can hand the
/// director a fake. The director never hands the hunter the player's cell, only a call's estimate; the player's inputs it
/// passes to Dispatch are for staying away and unseen.
/// </summary>
public interface IFrontRoomsRelayHunter
{
    HunterState State { get; }
    /// <summary>Not Dormant: Away, Arrive or on the map.</summary>
    bool Armed { get; }
    /// <summary>Listen, Wander, Hunt, Search, Chase, BreakDoor or Withdraw.</summary>
    bool OnMap { get; }
    int WarnStage { get; }
    bool PlayerPerceivesRelay { get; }
    Vector3 Position { get; }
    /// <summary>Dormant to Away (hidden), at the floor's start-door shut.</summary>
    void Arm();
    /// <summary>Back to Dormant for a floor change (no WentAway).</summary>
    void Disarm();
    /// <summary>Place for a call while Away. True: placed, Arrive under way (backstage, the arrive delay to the step-in). False: nothing changed (retry).</summary>
    bool Dispatch(FrontRoomsRelayCall call, FrontRoomsRelayPlacement placement, Vector3 playerFeet, Vector3 playerEye, Vector3 playerForward);
    /// <summary>FADE (RELAY 4): no patrol, a cautious look after a lost chase, lock-on only within 6 m on the way out. <paramref name="hardEnd"/>: onMapCap + 15 s passed.</summary>
    void Fade(bool hardEnd);
    /// <summary>chaseCap reached: this chase's lose threshold drops to <paramref name="loseSeconds"/>.</summary>
    void CapChase(float loseSeconds);
    /// <summary>Off the map now (a test call): WentAway with the reason.</summary>
    void SendAway(FrontRoomsRelayAwayReason reason);
    /// <summary>It went Away (exit reached, build edge, unbuilt cell, watched then occluded, timeout, test).</summary>
    event Action<FrontRoomsRelayAwayReason> WentAway;
    /// <summary>The give-up beat began (RELAY 5: it reports Listen), with its feet.</summary>
    event Action<Vector3> GaveUp;
}

/// <summary>What an on-map device call can do now (RELAY 5, 6; step V3b), as the hunter judges it from its state.</summary>
public enum FrontRoomsRelayRedispatchKind
{
    /// <summary>Budget aside, it cannot take one (Arrive, Withdraw, FADE, the stairwell beat): the trip queues.</summary>
    Queue,
    /// <summary>
    /// In a chase or a door break: no re-dispatch and no budget (RELAY 5). V3c (systems design 2026-10-30, reading 2 changed):
    /// the trip is heard (the on-map ears may hear it) and queued (the slot, latest trip wins), out at the next Away.
    /// </summary>
    NoiseOnly,
    /// <summary>It walks to the device (Listen, Hunt, Search or Patrol).</summary>
    Walk,
    /// <summary>The handoff: investigating, the device more than 20 walking cells away, unseen, stage 0, W at least 30 m.</summary>
    Handoff
}

/// <summary>
/// Step V3b's half of the hunter contract (RELAY 5, 6), apart from IFrontRoomsRelayHunter so that a hunter without it
/// (the director tests' fake) keeps V3a's behaviour: device trips on the map queue, and the sweep is V3a's stand-in.
/// FrontRoomsMapHunter implements it under FRONTROOMS_RELAY_V2 (FrontRoomsMapHunter.V3b.cs).
/// </summary>
public interface IFrontRoomsRelayHunterV3b
{
    /// <summary>What a device tripped at <paramref name="point"/> can do now (the director checks the budget).</summary>
    FrontRoomsRelayRedispatchKind RedispatchKind(Vector3 point, Vector3 playerFeet, Vector3 playerEye);
    /// <summary>The handoff: from the map straight to Arrive at a new placement for <paramref name="call"/> (no Away, no WentAway). False: nothing changed.</summary>
    bool Handoff(FrontRoomsRelayCall call, Vector3 playerFeet, Vector3 playerEye, Vector3 playerForward);
    /// <summary>A re-dispatch on foot: it hunts to the call's estimate from where it stands. False: nothing changed.</summary>
    bool Redirect(FrontRoomsRelayCall call);
    /// <summary>
    /// The quiet-player sweep (Q2) while Away: picks the zone along <paramref name="playerHeading"/> and its 4 waypoints
    /// (W at least 21 m, the nearest 21-30 m), places for the first (Arrive's hard rules; <paramref name="relaxed"/>: the
    /// PENDING fallback's), then patrols them for the tier's sweep seconds. False: nothing changed.
    /// </summary>
    bool Sweep(FrontRoomsRelayCall call, bool relaxed, Vector3 playerFeet, Vector3 playerEye, Vector3 playerForward, Vector3 playerHeading);
    /// <summary>The last Sweep's first waypoint (the call's estimate as placed).</summary>
    Vector3 SweepTarget { get; }
}

/// <summary>
/// Relay v2's pacing director (RELAY_PURSUIT_REDESIGN section 4; step V3a), the adapter that owns
/// FrontRoomsRelayDirectorPolicy and drives the hunter through IFrontRoomsRelayHunter. Pure C#, ticked by
/// FrontRooms3DGame before the hunter, testable in EditMode with a fake hunter.
/// - Arm at each floor's start-door shut (systems design P2: every floor's arrival door counts as its start door) with the 60 s
///   first-run RELAX; on floor 1 with a station the first call is held (HoldFirstCall) until the game's gate
///   (ReleaseFirstCall: the onboarding's StationSendDue, Red 2026-10-30: 150 cells explored or 180 s). The run's first
///   dispatch, on whichever floor it comes, is placed through FirstSend (C32; FirstSendDone is run-level, Disarm keeps it).
/// - PENDING: the first try in the frame after the call (then every RetrySeconds); just before the policy's 10 s drop one
///   Relaxed try (W at least 36 m, every soft rule dropped); a drop is logged (CallDropped), and a dropped device call
///   goes out again RequeueSeconds later (ReleaseQueued), never lost.
/// - The encounter: SetOnMap while it is in Arrive or on the map (Attention frozen, devices queue); the stage and
///   PlayerPerceivesRelay to Pressure; TargetAcquired at a chase's start (V2's v1 stand-in: a chase is a lock-on until V4b);
///   chaseCap gives that chase a 1.0 s lose threshold; on the map onMapCap + HardEndSeconds is the hard end; FADE (the
///   policy's SendAwayRequested for any reason but GiveUp) reaches the hunter as Fade.
/// - Away (the hunter's WentAway) is NotifyAway: Restore if a warning was felt, Attention 20, RELAX (half after an
///   unbuilt-cell Away). The hunter's GaveUp is NotifyGaveUp.
/// - Step V3b, with a hunter that implements IFrontRoomsRelayHunterV3b (FrontRoomsMapHunter under the define):
///   - the sweep (the dry-spell call, Q2) is the hunter's: a zone along the player's recent heading (RecentHeading:
///     the last HeadingSamples seconds of the player's walk, else the look), 4 waypoints, W at least 21 m (the nearest
///     21-30 m), the tier's sweep seconds;
///   - a device tripped while the Relay is on the map re-dispatches while the encounter's budget lasts (RELAY 6): the
///     handoff (straight from the map to Arrive, no Away hooks, cause Handoff) when the hunter allows it, else a walk
///     to the device (cause Device); otherwise it waits in the queued slot (latest trip wins) and goes out at the next
///     Away with QueuedCallOut (the CALLED tick). V3c (systems design 2026-10-30): in a chase or a break (no re-dispatch, RELAY
///     5) it is heard and queued the same way (DevicesHeardOnly counts these trips; DevicesQueuedOnMap counts them too).
///   A hunter without it (the director tests' fake) keeps V3a's behaviour: device trips on the map queue, and the
///   sweep's estimate is SweepAheadMetres along the player's look, an ordinary Arrive (V3a's stand-in).
/// - Step V4b, under the define, with a hunter that implements IFrontRoomsRelayHunterV4b: the +15 comes at TargetAcquired, not
///   at the chase's start; stage 3 is the lock-on's latch; "almost" (meter 0.5) feeds Pressure; a lock-on counts toward
///   lockOnCap once its chase has lasted 5 s, and FADE then comes after that chase; the look on the way out counts too. A
///   hunter without it keeps the stand-in: a chase's start is the lock-on (the +15), counted the same way.
/// - Floor 1's hold (A40) is kept: a trip while the first call is held always queues until the gate.
/// Nothing calls this without FRONTROOMS_RELAY_V2.
/// </summary>
public sealed class FrontRoomsRelayDirector
{
    /// <summary>PENDING retry (RELAY 5).</summary>
    public const float RetrySeconds = .5f;
    /// <summary>The hard end comes this long after onMapCap (RELAY 4).</summary>
    public const float HardEndSeconds = 15f;
    /// <summary>A chase past chaseCap loses the player at this (RELAY 4: a logged backstop).</summary>
    public const float ChaseCapLoseSeconds = 1f;
    /// <summary>A device call dropped by PENDING goes out again this long after the drop (V3a: the queued slot is otherwise emptied only by the next Away).</summary>
    public const float RequeueSeconds = 10f;
    /// <summary>V3a's sweep stand-in: the estimate this far along the player's heading (a hunter without IFrontRoomsRelayHunterV3b; V3b's sweep is the hunter's).</summary>
    public const float SweepAheadMetres = 24f;
    /// <summary>V3b: the player's recent heading is their walk over this many 1 s samples (at least HeadingMinMetres of it), else their look.</summary>
    public const int HeadingSamples = 8;
    public const float HeadingSampleSeconds = 1f, HeadingMinMetres = 2f;
    /// <summary>The tools shim (V3a): FrontRoomsHunterTuning.releaseDelaySeconds at or above this still means "never arm" (EXAMINE and glass RT harnesses set 1e6).</summary>
    public const float NeverArmDelaySeconds = 1e5f;

    readonly FrontRoomsRelayDirectorRules rules;
    readonly FrontRoomsRelayDirectorPolicy policy;
    readonly IFrontRoomsRelayHunter hunter;
    // V3b's half of the hunter, or null (V3a's behaviour).
    readonly IFrontRoomsRelayHunterV3b hunterV3b;
#if FRONTROOMS_RELAY_V2
    // V4b's half of the hunter, or null (a chase's start is the lock-on).
    readonly IFrontRoomsRelayHunterV4b hunterV4b;
#endif
    Vector3 feet, eye, forward = Vector3.forward;
    int tier = 1;
    float retry, sinceDrop, onMapSeconds, chaseSeconds;
    bool firstSendNext, firstSendDone, hardEnd, inChase, chaseCapped;
    // V3b: the player's feet once a second (a ring of HeadingSamples), for RecentHeading.
    readonly Vector3[] headingRing = new Vector3[HeadingSamples];
    int headingCount, headingNext;
    float headingClock;

    public FrontRoomsRelayDirector(IFrontRoomsRelayHunter hunter, FrontRoomsRelayDirectorRules rules = null, int initialTier = 1)
    {
        this.hunter = hunter ?? throw new ArgumentNullException(nameof(hunter));
        hunterV3b = hunter as IFrontRoomsRelayHunterV3b;
#if FRONTROOMS_RELAY_V2
        hunterV4b = hunter as IFrontRoomsRelayHunterV4b;
        if (hunterV4b != null)
        {
            hunterV4b.TargetAcquired += OnTargetAcquired;
            hunterV4b.WayOutLook += OnWayOutLook;
        }
#endif
        this.rules = (rules ?? new FrontRoomsRelayDirectorRules()).Clone();
        tier = this.rules.ClampTier(initialTier);
        policy = new FrontRoomsRelayDirectorPolicy(this.rules, tier);
        policy.PhaseChanged += OnPhaseChanged;
        policy.Called += OnCalled;
        policy.PendingDropped += OnPendingDropped;
        policy.SendAwayRequested += OnSendAwayRequested;
        policy.Restore += OnRestore;
        policy.DeviceTriggered += OnDeviceTriggered;
        hunter.WentAway += OnWentAway;
        hunter.GaveUp += OnGaveUp;
    }

    /// <summary>The policy it owns (read it; drive it only through the director).</summary>
    public FrontRoomsRelayDirectorPolicy Policy => policy;
    public FrontRoomsRelayDirectorPhase Phase => policy.Phase;
    /// <summary>Armed since the floor's start-door shut (RELAY 4: Armed = not Dormant).</summary>
    public bool Armed => policy.Armed;
    /// <summary>Floor 1 with a station: the first call waits for ReleaseFirstCall.</summary>
    public bool FirstCallHeld => policy.FirstCallHeld;
    /// <summary>The next placement is the run's first send (FirstSend), until a call is placed.</summary>
    public bool FirstSendNext => firstSendNext;
    /// <summary>
    /// The onboarding (Red 2026-10-30): the run's first send has been dispatched (a FirstSend placement). Run-level: Disarm
    /// keeps it (a floor change), a new run makes a new director. Once true, no later Arm makes FirstSendNext.
    /// </summary>
    public bool FirstSendDone => firstSendDone;
    public int Tier => tier;
    /// <summary>Seconds the Relay has been in Arrive or on the map this encounter (the hard end's clock).</summary>
    public float OnMapSeconds => onMapSeconds;
    /// <summary>The hard end (onMapCap + HardEndSeconds) was sent this encounter.</summary>
    public bool HardEnd => hardEnd;
    /// <summary>Run totals (Disarm keeps them): calls placed, calls dropped by PENDING, placement tries that found nothing, PENDING seconds, chases counted as lock-ons, arms.</summary>
    public int Calls { get; private set; }
    public int Drops { get; private set; }
    public int FailedTries { get; private set; }
    public float PendingSecondsTotal { get; private set; }
    public int LockOns { get; private set; }
    public int Arms { get; private set; }
    /// <summary>The last call placed, and how.</summary>
    public FrontRoomsRelayCall LastCall { get; private set; }
    public FrontRoomsRelayPlacement LastPlacement { get; private set; }
    /// <summary>Why the Relay last went Away (Test before any).</summary>
    public FrontRoomsRelayAwayReason LastAwayReason { get; private set; } = FrontRoomsRelayAwayReason.Test;
    /// <summary>V3b run totals (Disarm keeps them): on-map device re-dispatches (handoffs included), handoffs, sweeps placed, trips in a chase or a break (V3c: heard and queued), trips queued while the Relay was on the map (those included).</summary>
    public int Redispatches { get; private set; }
    public int Handoffs { get; private set; }
    public int Sweeps { get; private set; }
    public int DevicesHeardOnly { get; private set; }
    public int DevicesQueuedOnMap { get; private set; }
    /// <summary>V3b: the hunter takes re-dispatches and runs the sweep itself (IFrontRoomsRelayHunterV3b).</summary>
    public bool UsesV3b => hunterV3b != null;

    /// <summary>
    /// V3b (Q2: "chosen along the player's recent heading, so walking forward doesn't dodge it"): the flat direction of the
    /// player's walk over the last HeadingSamples seconds when it covers at least HeadingMinMetres, else their flat look
    /// (+Z with neither).
    /// </summary>
    public Vector3 RecentHeading
    {
        get
        {
            if (headingCount > 0)
            {
                var oldest = headingRing[headingCount < HeadingSamples ? 0 : headingNext];
                var walk = new Vector3(feet.x - oldest.x, 0f, feet.z - oldest.z);
                if (walk.sqrMagnitude >= HeadingMinMetres * HeadingMinMetres) return walk.normalized;
            }
            var look = new Vector3(forward.x, 0f, forward.z);
            return look.sqrMagnitude > 1e-6f ? look.normalized : Vector3.forward;
        }
    }

    public event Action<FrontRoomsRelayDirectorPhase> PhaseChanged;
    /// <summary>RELAY 6's CALLED (sound design hook): a call was placed. Cause, explicit (a device), and its estimate (a device's own point).</summary>
    public event Action<FrontRoomsRelayCallCause, bool, Vector3> Called;
    /// <summary>The hunter took a call (placed, Arrive under way), and how; raised after Called.</summary>
    public event Action<FrontRoomsRelayCall, FrontRoomsRelayPlacement> Dispatched;
    /// <summary>RELAY 9 / 11's RESTORE (sound design hook): it truly left and the player felt stage 1 or more this encounter.</summary>
    public event Action Restore;
    /// <summary>A device was tripped (it sounds at once; the call may still be pending or queued).</summary>
    public event Action<FrontRoomsRelayCall> DeviceTriggered;
    /// <summary>PENDING found no Arrive in 10 s: the call was dropped (Attention = threshold - 20; a device call queued again).</summary>
    public event Action<FrontRoomsRelayCall> CallDropped;
    /// <summary>FADE began (any reason but the give-up beat's).</summary>
    public event Action<FrontRoomsRelayAwayReason> FadeBegan;
    /// <summary>The Relay went Away, and why.</summary>
    public event Action<FrontRoomsRelayAwayReason> Away;
    /// <summary>V3b: an on-map device call was re-dispatched (raised after Called): the call, and true for the handoff (straight to Arrive), false for the walk.</summary>
    public event Action<FrontRoomsRelayCall, bool> Redispatched;
    /// <summary>
    /// RELAY 6's CALLED for a queued device call (sound design hook, V3b): the trip waited in the queued slot and its call
    /// has now gone out (placed, after Called): play the CALLED tick, not the device's sound (that played at the trip).
    /// With the device's point.
    /// </summary>
    public event Action<Vector3> QueuedCallOut;

    /// <summary>
    /// The floor's start door has fully shut (RELAY 4, systems design P2): Armed and Away, 60 s RELAX. <paramref name="holdFirstCall"/>:
    /// floor 1 with a station; the first call is held until ReleaseFirstCall and placed through FirstSend. Once per floor.
    /// The same as Arm(holdFirstCall, holdFirstCall): the gentle first send only with the hold (V3a's behaviour).
    /// </summary>
    public void Arm(bool holdFirstCall) => Arm(holdFirstCall, holdFirstCall);

    /// <summary>
    /// The floor's start door has fully shut (RELAY 4, systems design P2): Armed and Away, 60 s RELAX. <paramref name="holdFirstCall"/>:
    /// floor 1 with a station; the first call is held until ReleaseFirstCall. <paramref name="gentleFirstSend"/> (the onboarding,
    /// Red 2026-10-30: the game passes "this run started on a station floor"): while the run's first send has not been
    /// dispatched (FirstSendDone), this floor's first placement is FirstSend, wherever it comes; after it every placement is
    /// Normal or Relaxed. Once per floor.
    /// </summary>
    public void Arm(bool holdFirstCall, bool gentleFirstSend)
    {
        if (policy.Armed) return;
        ResetEncounter();
        retry = 0f;
        sinceDrop = 0f;
        policy.Arm();
        policy.HoldFirstCall(holdFirstCall);
        firstSendNext = gentleFirstSend && !firstSendDone;
        headingCount = headingNext = 0;
        headingClock = 0f;
        hunter.Arm();
        Arms++;
    }

    /// <summary>The floor is left (C22: at the change, before the next floor is seeded): Dormant, only the tier and the run's totals carry over (Park; FirstSendDone stays).</summary>
    public void Disarm()
    {
        policy.Disarm();
        hunter.Disarm();
        firstSendNext = false;
        ResetEncounter();
        retry = 0f;
        sinceDrop = 0f;
    }

    /// <summary>
    /// Floor 1's gate is due (the onboarding, Red 2026-10-30: 150 cells of the floor explored, or 180 s since the shut; the
    /// tools' -autopilotOnboardOff: M7's old key and 2 path cells out, or 150 s): the hold lifts. A device call queued
    /// meanwhile (glass, the station take) goes out at once; with none, a Gate call to <paramref name="estimate"/> (the
    /// station's take point or spot). Either is placed through FirstSend while the run's first send is still to come.
    /// False when nothing was held or no call could start.
    /// </summary>
    public bool ReleaseFirstCall(Vector3 estimate)
    {
        if (!policy.Armed || !policy.FirstCallHeld) return false;
        policy.HoldFirstCall(false);
        if (policy.HasPendingCall) return true;
        return policy.ForceCall(estimate, 0, FrontRoomsRelayCallCause.Gate, false);
    }

    /// <summary>Tools (the autopilot's debug Summon): a call to <paramref name="estimate"/> now, while it is Away and no first call is held. False otherwise.</summary>
    public bool Summon(Vector3 estimate, int errorCells = 0) =>
        policy.ForceCall(estimate, errorCells, FrontRoomsRelayCallCause.Summon, false);

    /// <summary>RELAY 6: a player disturbance (only Player counts; RELAX, PENDING, the map and a sanctuary add nothing).</summary>
    public void Disturb(FrontRoomsRelayDisturbanceKind kind, Vector3 point, bool sanctuary) =>
        policy.Disturb(kind, point, FrontRoomsRelayDisturbanceSource.Player, sanctuary);

    /// <summary>
    /// RELAY 6: a device (glass, the station take, A14): it calls at once from RELAX or BUILD-UP, else waits in the queued
    /// slot (latest trip wins). V3b, with the Relay on the map and no first call held: a re-dispatch while the budget lasts
    /// (the handoff when the hunter allows it, else the walk; true), otherwise queued. V3c (systems design 2026-10-30, reading 2
    /// changed): in a chase or a break there is no re-dispatch (RELAY 5), so the trip is heard and queued (RELAY 4's slot):
    /// the device sounds once (DeviceTriggered), Attention is not counted (it is frozen on the map), and the call goes out
    /// at the next Away with QueuedCallOut; false. True when a call started or was re-dispatched.
    /// </summary>
    public bool DeviceTrigger(Vector3 point, string deviceId)
    {
        if (hunterV3b != null && policy.Armed && !policy.FirstCallHeld && hunter.OnMap)
        {
            var kind = hunterV3b.RedispatchKind(point, feet, eye);
            // V3c: a chase or a break never re-dispatches (RELAY 5); its trip is heard and queued below (no silent dead call).
            if (kind == FrontRoomsRelayRedispatchKind.NoiseOnly) DevicesHeardOnly++;
            else if ((kind == FrontRoomsRelayRedispatchKind.Walk || kind == FrontRoomsRelayRedispatchKind.Handoff) && policy.CanRedispatch)
            {
                // The handoff is placed first, so the call reports what really happened; a handoff with no Arrive walks.
                var handoff = kind == FrontRoomsRelayRedispatchKind.Handoff
                    && hunterV3b.Handoff(new FrontRoomsRelayCall(point, 0, FrontRoomsRelayCallCause.Handoff, true, false, policy.Attention), feet, eye, forward);
                var call = policy.RedispatchDevice(point, deviceId, handoff);
                if (call != null)
                {
                    if (!handoff) hunterV3b.Redirect(call);
                    Redispatches++;
                    if (handoff) Handoffs++;
                    LastCall = call;
                    LastPlacement = FrontRoomsRelayPlacement.Normal;
                    Redispatched?.Invoke(call, handoff);
                    return true;
                }
            }
            DevicesQueuedOnMap++;
        }
        return policy.DeviceTrigger(point, deviceId);
    }

    /// <summary>
    /// Once a frame, before the hunter's Tick: the run's tier, whether the player is in a sanctuary (InStairwell: gains 0,
    /// the dry clock paused), and the player's feet, eye and look (only for Dispatch's "away and unseen").
    /// </summary>
    public void Tick(float dt, int runTier, bool sanctuary, Vector3 playerFeet, Vector3 playerEye, Vector3 playerForward)
    {
        feet = playerFeet;
        eye = playerEye;
        forward = playerForward;
        if (!policy.Armed || dt <= 0f) return;
        SampleHeading(dt);
        tier = rules.ClampTier(runTier);
        policy.SetTier(tier);
        policy.SetSanctuary(sanctuary);
        var state = hunter.State;
        var present = state == HunterState.Arrive || hunter.OnMap;
        policy.SetOnMap(present);
        if (hunter.OnMap)
        {
#if FRONTROOMS_RELAY_V2
            // V4b: stage 3 is the lock-on's latch (RELAY 9.1); WarnStage stays 0 to 2. "Almost": the meter at 0.5.
            policy.NotifyStage(hunterV4b != null && hunterV4b.LockedOn ? 3 : hunter.WarnStage);
            policy.NotifyAlmostSeen(hunterV4b != null && hunterV4b.Spotting >= FrontRoomsMapHunter.AlmostLevel);
#else
            policy.NotifyStage(hunter.WarnStage);
#endif
            policy.NotifyPlayerPerceivesRelay(hunter.PlayerPerceivesRelay);
        }
        TrackChase(dt, state);
        if (present)
        {
            onMapSeconds += dt;
            if (!hardEnd && onMapSeconds >= rules.At(rules.onMapCap, tier, 75f) + HardEndSeconds)
            {
                hardEnd = true;
                hunter.Fade(true);
            }
        }
        if (policy.Phase == FrontRoomsRelayDirectorPhase.Pending && state == HunterState.Away)
        {
            PendingSecondsTotal += Mathf.Min(dt, 1f);
            retry -= dt;
            // The policy drops the call on the tick its PENDING passes the timeout: the fallback goes first.
            if (policy.PendingTime + Mathf.Min(dt, 1f) > rules.pendingTimeoutSeconds) TryPlace(true);
            else if (retry <= 0f)
            {
                retry = RetrySeconds;
                TryPlace(false);
            }
        }
        else if (policy.HasQueuedDevice && !policy.FirstCallHeld && state == HunterState.Away
            && (policy.Phase == FrontRoomsRelayDirectorPhase.Relax || policy.Phase == FrontRoomsRelayDirectorPhase.BuildUp))
        {
            sinceDrop += dt;
            if (sinceDrop >= RequeueSeconds)
            {
                sinceDrop = 0f;
                policy.ReleaseQueued();
            }
        }
        policy.Tick(dt);
    }

    bool TryPlace(bool relaxed)
    {
        var call = policy.PendingCall;
        if (call == null) return false;
        var placement = firstSendNext ? FrontRoomsRelayPlacement.FirstSend : relaxed ? FrontRoomsRelayPlacement.Relaxed : FrontRoomsRelayPlacement.Normal;
        if (call.Cause == FrontRoomsRelayCallCause.Sweep && hunterV3b != null && placement != FrontRoomsRelayPlacement.FirstSend)
        {
            // V3b (Q2): the hunter picks the zone along the player's recent heading and its waypoints, and places for the first.
            if (!hunterV3b.Sweep(call, relaxed, feet, eye, forward, RecentHeading))
            {
                FailedTries++;
                return false;
            }
            Sweeps++;
            call = new FrontRoomsRelayCall(hunterV3b.SweepTarget, 0, call.Cause, call.Explicit, call.Queued, call.AttentionAtCall);
        }
        else
        {
            // V3a's stand-in (a hunter without V3b, or the run's first send: the gentle ladder comes first).
            if (call.Cause == FrontRoomsRelayCallCause.Sweep)
                call = new FrontRoomsRelayCall(SweepEstimate(), 1, call.Cause, call.Explicit, call.Queued, call.AttentionAtCall);
            if (!hunter.Dispatch(call, placement, feet, eye, forward))
            {
                FailedTries++;
                return false;
            }
        }
        if (placement == FrontRoomsRelayPlacement.FirstSend) firstSendDone = true;
        firstSendNext = false;
        LastCall = call;
        LastPlacement = placement;
        ResetEncounter();
        policy.SetOnMap(true);
        policy.ResolvePending(true, call.Estimate, call.ErrorCells);
        Dispatched?.Invoke(call, placement);
        return true;
    }

    // V3a's stand-in for the quiet-player sweep (a hunter without V3b): along the player's look, never at the player.
    Vector3 SweepEstimate()
    {
        var ahead = new Vector3(forward.x, 0f, forward.z);
        if (ahead.sqrMagnitude < 1e-6f) ahead = Vector3.forward;
        return feet + ahead.normalized * SweepAheadMetres;
    }

    // V3b: the player's feet once every HeadingSampleSeconds, into the ring RecentHeading reads.
    void SampleHeading(float dt)
    {
        headingClock -= dt;
        if (headingCount > 0 && headingClock > 0f) return;
        headingClock = HeadingSampleSeconds;
        headingRing[headingNext] = feet;
        headingNext = (headingNext + 1) % HeadingSamples;
        if (headingCount < HeadingSamples) headingCount++;
    }

    void TrackChase(float dt, HunterState state)
    {
        var chase = state == HunterState.Chase || (state == HunterState.BreakDoor && inChase);
        if (chase && !inChase)
        {
            chaseSeconds = 0f;
            chaseCapped = false;
            LockOns++;
#if FRONTROOMS_RELAY_V2
            // V4b: TargetAcquired came at the fill (OnTargetAcquired); a hunter without it: the chase's start is the lock-on.
            if (hunterV4b == null) policy.NotifyTargetAcquired();
            policy.NotifyChaseStarted();
#else
            policy.NotifyTargetAcquired();
#endif
        }
        else if (!chase && inChase) policy.NotifyChaseEnded();
        inChase = chase;
        if (!chase) return;
        chaseSeconds += dt;
        if (!chaseCapped && chaseSeconds >= rules.At(rules.chaseCap, tier, 25f))
        {
            chaseCapped = true;
            hunter.CapChase(ChaseCapLoseSeconds);
        }
    }

    void ResetEncounter()
    {
        onMapSeconds = 0f;
        chaseSeconds = 0f;
        hardEnd = false;
        inChase = false;
        chaseCapped = false;
    }

    void OnPhaseChanged(FrontRoomsRelayDirectorPhase next)
    {
        // A call that starts pending is tried in the next director tick.
        if (next == FrontRoomsRelayDirectorPhase.Pending) retry = 0f;
        PhaseChanged?.Invoke(next);
    }

    void OnCalled(FrontRoomsRelayCall call)
    {
        Calls++;
        Called?.Invoke(call.Cause, call.Explicit, call.Estimate);
        // V3b: a queued device call going out plays the CALLED tick (RELAY 6), traced to the trip it waited for.
        if (call.Queued && call.Cause == FrontRoomsRelayCallCause.Device) QueuedCallOut?.Invoke(call.Estimate);
    }

    void OnPendingDropped(FrontRoomsRelayCall call)
    {
        Drops++;
        sinceDrop = 0f;
        CallDropped?.Invoke(call);
    }

    void OnSendAwayRequested(FrontRoomsRelayAwayReason reason)
    {
        // The give-up beat's own FADE: the hunter is already on its way out (patrol, then Withdraw).
        if (reason == FrontRoomsRelayAwayReason.GiveUp) return;
        hunter.Fade(false);
        FadeBegan?.Invoke(reason);
    }

    void OnRestore() => Restore?.Invoke();

    void OnDeviceTriggered(FrontRoomsRelayCall call) => DeviceTriggered?.Invoke(call);

    void OnWentAway(FrontRoomsRelayAwayReason reason)
    {
        LastAwayReason = reason;
        ResetEncounter();
        // A Relay put on the map by tools while a call was pending was never this call's: the call stays pending.
        if (policy.Phase != FrontRoomsRelayDirectorPhase.Pending) policy.NotifyAway(reason);
        Away?.Invoke(reason);
    }

    void OnGaveUp(Vector3 at) => policy.NotifyGaveUp();
#if FRONTROOMS_RELAY_V2

    // V4b (RELAY 4, 8): Pressure +15 once, at the fill.
    void OnTargetAcquired(Vector3 at) => policy.NotifyTargetAcquired();

    // V4b (RELAY 11): the look on the way out counts toward lockOnCap.
    void OnWayOutLook() => policy.NotifyWayOutLook();
#endif
}
