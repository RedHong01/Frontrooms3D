using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The director's policy vocabulary.  This file deliberately has no
/// MonoBehaviour and does not drive the current timed-release hunter.  It is
/// the deterministic system layer that the v2 director adapter can call once
/// the map-owned Arrive/path contracts are ready.
/// </summary>
public enum FrontRoomsRelayDirectorPhase
{
    Dormant,
    Relax,
    BuildUp,
    Pending,
    Encounter,
    Sustain,
    Fade
}

public enum FrontRoomsRelayCallCause
{
    Attention,
    Device,
    Sweep,
    Handoff
}

public enum FrontRoomsRelayDisturbanceKind
{
    Walk,
    Sprint,
    EaseDoor,
    Unlock,
    Barge,
    Slam,
    Pry,
    Other
}

public enum FrontRoomsRelayDisturbanceSource
{
    Player,
    Relay,
    World
}

public enum FrontRoomsRelayAwayReason
{
    ExitReached,
    BuildEdge,
    UnbuiltCell,
    Watched,
    Timeout,
    GiveUp,
    Test
}

/// <summary>A call handed to the hunter.  The estimate is a sound estimate,
/// never the player's current cell.</summary>
public sealed class FrontRoomsRelayCall
{
    public readonly Vector3 Estimate;
    public readonly int ErrorCells;
    public readonly FrontRoomsRelayCallCause Cause;
    public readonly bool Explicit;
    public readonly bool Queued;
    public readonly float AttentionAtCall;

    public FrontRoomsRelayCall(Vector3 estimate, int errorCells,
        FrontRoomsRelayCallCause cause, bool explicitCall, bool queued,
        float attentionAtCall)
    {
        Estimate = estimate;
        ErrorCells = Mathf.Max(0, errorCells);
        Cause = cause;
        Explicit = explicitCall;
        Queued = queued;
        AttentionAtCall = attentionAtCall;
    }
}

/// <summary>
/// The tunable part of the v2 pacing proposal.  Values are copied on
/// construction so a scene or tier asset cannot mutate a running policy
/// halfway through a call.
/// </summary>
[Serializable]
public sealed class FrontRoomsRelayDirectorRules
{
    public float[] callThreshold = { 0f, 60f, 56f, 52f, 48f, 45f };
    public float[] attentionDecay = { 0f, 3f, 2.75f, 2.5f, 2.25f, 2f };
    public float[] peakThreshold = { 0f, 100f, 105f, 110f, 115f, 120f };
    public float[] onMapCap = { 0f, 75f, 80f, 90f, 100f, 110f };
    public int[] redispatchBudget = { 0, 1, 1, 2, 2, 3 };
    public int[] lockOnCap = { 0, 2, 2, 2, 3, 3 };
    public float[] chaseCap = { 0f, 25f, 27f, 30f, 32f, 35f };
    public float[] relaxSeconds = { 0f, 100f, 90f, 80f, 70f, 60f };
    public float[] drySpellSeconds = { 0f, 240f, 210f, 180f, 160f, 140f };

    public float firstRunGraceSeconds = 60f;
    public float pendingTimeoutSeconds = 10f;
    public float pendingDropAttention = 20f;
    public float sustainSeconds = 4f;
    public float initialAttention = 20f;
    public float maxAttention = 100f;

    public FrontRoomsRelayDirectorRules Clone()
    {
        var copy = new FrontRoomsRelayDirectorRules
        {
            firstRunGraceSeconds = firstRunGraceSeconds,
            pendingTimeoutSeconds = pendingTimeoutSeconds,
            pendingDropAttention = pendingDropAttention,
            sustainSeconds = sustainSeconds,
            initialAttention = initialAttention,
            maxAttention = maxAttention
        };
        copy.callThreshold = (float[])callThreshold.Clone();
        copy.attentionDecay = (float[])attentionDecay.Clone();
        copy.peakThreshold = (float[])peakThreshold.Clone();
        copy.onMapCap = (float[])onMapCap.Clone();
        copy.redispatchBudget = (int[])redispatchBudget.Clone();
        copy.lockOnCap = (int[])lockOnCap.Clone();
        copy.chaseCap = (float[])chaseCap.Clone();
        copy.relaxSeconds = (float[])relaxSeconds.Clone();
        copy.drySpellSeconds = (float[])drySpellSeconds.Clone();
        return copy;
    }

    public int ClampTier(int tier)
    {
        var count = Mathf.Min(callThreshold.Length, Mathf.Min(attentionDecay.Length,
            Mathf.Min(peakThreshold.Length, Mathf.Min(onMapCap.Length,
            Mathf.Min(redispatchBudget.Length, Mathf.Min(lockOnCap.Length,
            Mathf.Min(chaseCap.Length, Mathf.Min(relaxSeconds.Length, drySpellSeconds.Length))))))));
        return Mathf.Clamp(tier, 1, Mathf.Max(1, count - 1));
    }

    public float At(float[] values, int tier, float fallback)
    {
        if (values == null || values.Length == 0) return fallback;
        if (values.Length == 1) return values[0];
        var i = Mathf.Clamp(tier, 1, values.Length - 1);
        return values[i];
    }

    public int At(int[] values, int tier, int fallback)
    {
        if (values == null || values.Length == 0) return fallback;
        if (values.Length == 1) return values[0];
        var i = Mathf.Clamp(tier, 1, values.Length - 1);
        return values[i];
    }
}

/// <summary>
/// Pure pacing policy for Relay Pursuit v2.
///
/// The current game still uses FrontRoomsMapHunter directly.  This class is
/// intentionally an isolated seam: the future adapter will feed it map
/// availability and hunter callbacks, while the policy remains testable
/// without a scene, NavMesh, audio source, or visual asset.
/// </summary>
public sealed class FrontRoomsRelayDirectorPolicy
{
    sealed class Disturbance
    {
        public Vector3 point;
        public float weight;
        public int errorCells;
        public float time;
    }

    readonly FrontRoomsRelayDirectorRules rules;
    readonly List<Disturbance> disturbances = new List<Disturbance>();

    bool armed;
    bool firstRunGrace;
    bool onMap;
    bool inSanctuary;
    bool warningFelt;
    bool almostSeen;
    bool playerSeesRelay;
    bool queuedDevice;
    FrontRoomsRelayCall pending;
    Vector3 queuedPoint;
    string queuedDeviceId;
    float now;
    float phaseTime;
    float relaxRemaining;
    float pendingTime;
    float drySpell;
    float encounterTime;
    float sustainRemaining;
    float lastGainTime = float.NegativeInfinity;
    float attention;
    float pressure;
    int tier = 1;
    int redispatches;
    int lockOns;
    float currentChaseTime;
    int currentStage;

    public FrontRoomsRelayDirectorPhase Phase { get; private set; }
    public float Attention => attention;
    public float Pressure => pressure;
    public float DrySpell => drySpell;
    public float EncounterTime => encounterTime;
    public float RelaxRemaining => relaxRemaining;
    public float PendingTime => pendingTime;
    public float PhaseTime => phaseTime;
    public int Tier => tier;
    public int CurrentStage => currentStage;
    public bool FirstRunGrace => firstRunGrace;
    public bool Armed => armed;
    public bool OnMap => onMap;
    public bool WarningFelt => warningFelt;
    public bool HasPendingCall => pending != null;
    public bool HasQueuedDevice => queuedDevice;
    public string QueuedDeviceId => queuedDeviceId;
    public FrontRoomsRelayCall PendingCall => pending;

    public event Action<FrontRoomsRelayDirectorPhase> PhaseChanged;
    /// <summary>Raised at the device, immediately when a readable device is
    /// tripped.  It is separate from Called: placement may still be pending.</summary>
    public event Action<FrontRoomsRelayCall> DeviceTriggered;
    public event Action<FrontRoomsRelayCall> CallQueued;
    public event Action<FrontRoomsRelayCall> Called;
    public event Action<FrontRoomsRelayCall> DispatchRequested;
    public event Action<FrontRoomsRelayCall> PendingDropped;
    public event Action<FrontRoomsRelayCall> SweepRequested;
    public event Action<FrontRoomsRelayAwayReason> SendAwayRequested;
    public event Action Restore;

    public FrontRoomsRelayDirectorPolicy(FrontRoomsRelayDirectorRules source = null, int initialTier = 1)
    {
        rules = (source ?? new FrontRoomsRelayDirectorRules()).Clone();
        tier = rules.ClampTier(initialTier);
        attention = rules.initialAttention;
        Phase = FrontRoomsRelayDirectorPhase.Dormant;
    }

    public void SetTier(int value)
    {
        tier = rules.ClampTier(value);
    }

    /// <summary>Starts the run in the 60 s first-run RELAX window.</summary>
    public void Arm()
    {
        if (armed) return;
        armed = true;
        firstRunGrace = true;
        attention = rules.initialAttention;
        pressure = 0f;
        drySpell = 0f;
        relaxRemaining = Mathf.Max(0f, rules.firstRunGraceSeconds);
        SetPhase(FrontRoomsRelayDirectorPhase.Relax);
    }

    public void Disarm()
    {
        armed = false;
        firstRunGrace = false;
        onMap = false;
        pending = null;
        queuedDevice = false;
        attention = rules.initialAttention;
        pressure = 0f;
        drySpell = 0f;
        SetPhase(FrontRoomsRelayDirectorPhase.Dormant);
    }

    public void SetOnMap(bool value)
    {
        onMap = value;
    }

    public void SetSanctuary(bool value)
    {
        inSanctuary = value;
    }

    /// <summary>Advance clocks.  All calls are emitted as commands/events;
    /// no hunter or map object is touched here.</summary>
    public void Tick(float deltaTime)
    {
        if (!armed || deltaTime <= 0f) return;
        var dt = Mathf.Min(deltaTime, 1f);
        now += dt;
        phaseTime += dt;

        PruneDisturbances();

        if (Phase == FrontRoomsRelayDirectorPhase.Relax)
        {
            relaxRemaining = Mathf.Max(0f, relaxRemaining - dt);
            if (relaxRemaining <= 0f)
            {
                firstRunGrace = false;
                drySpell = 0f;
                SetPhase(FrontRoomsRelayDirectorPhase.BuildUp);
            }
            return;
        }

        if (Phase == FrontRoomsRelayDirectorPhase.BuildUp)
        {
            if (!float.IsNegativeInfinity(lastGainTime) && now - lastGainTime >= 3f)
                attention = Mathf.Max(0f, attention - rules.At(rules.attentionDecay, tier, 0f) * dt);

            if (!inSanctuary)
            {
                drySpell += dt;
                var dryLimit = rules.At(rules.drySpellSeconds, tier, 240f);
                if (drySpell >= dryLimit) RequestSweep();
            }

            if (attention >= rules.At(rules.callThreshold, tier, 60f))
                RequestAttentionCall();
            return;
        }

        if (Phase == FrontRoomsRelayDirectorPhase.Pending)
        {
            pendingTime += dt;
            if (pendingTime > rules.pendingTimeoutSeconds)
                DropPending();
            return;
        }

        if (Phase == FrontRoomsRelayDirectorPhase.Encounter || Phase == FrontRoomsRelayDirectorPhase.Sustain)
        {
            encounterTime += dt;
            AddPressure(dt);
            if (Phase == FrontRoomsRelayDirectorPhase.Encounter)
            {
                var peak = rules.At(rules.peakThreshold, tier, 100f);
                var cap = rules.At(rules.onMapCap, tier, 75f);
                if (pressure >= peak || encounterTime >= cap)
                    EnterSustain();
            }
            else
            {
                sustainRemaining = Mathf.Max(0f, sustainRemaining - dt);
                if (sustainRemaining <= 0f) EnterFade();
            }
            if (currentChaseTime > 0f)
            {
                currentChaseTime += dt;
                if (currentChaseTime >= rules.At(rules.chaseCap, tier, 25f))
                    EnterFade(FrontRoomsRelayAwayReason.Timeout);
            }
            return;
        }

        if (Phase == FrontRoomsRelayDirectorPhase.Fade)
        {
            // The hunter owns the actual exit.  We only keep the phase stable
            // until NotifyAway is received, preventing a second call.
            return;
        }
    }

    public void Disturb(FrontRoomsRelayDisturbanceKind kind, Vector3 point,
        FrontRoomsRelayDisturbanceSource source = FrontRoomsRelayDisturbanceSource.Player,
        bool sanctuary = false)
    {
        if (!armed || source != FrontRoomsRelayDisturbanceSource.Player) return;
        // Attention is a BUILD-UP clock.  It is frozen in RELAX, PENDING and
        // every on-map phase while the hunter's own ears are authoritative.
        if (Phase != FrontRoomsRelayDirectorPhase.BuildUp || onMap) return;

        var gain = Gain(kind);
        if (sanctuary || inSanctuary) gain = 0f;
        if (gain <= 0f) return;

        attention = Mathf.Clamp(attention + gain, 0f, rules.maxAttention);
        lastGainTime = now;
        disturbances.Add(new Disturbance { point = point, weight = gain, errorCells = ErrorCells(kind), time = now });
        if (Phase == FrontRoomsRelayDirectorPhase.BuildUp && attention >= rules.At(rules.callThreshold, tier, 60f))
            RequestAttentionCall();
    }

    /// <summary>Readable devices bypass RELAX, but cannot create a second
    /// dispatch while the hunter is on the map or withdrawing.  One queued
    /// slot is kept and the latest trip wins.</summary>
    public bool DeviceTrigger(Vector3 point, string deviceId = null)
    {
        if (!armed) return false;
        var call = new FrontRoomsRelayCall(point, 0, FrontRoomsRelayCallCause.Device,
            true, false, attention);
        DeviceTriggered?.Invoke(call);
        if (!onMap && Phase != FrontRoomsRelayDirectorPhase.Pending && Phase != FrontRoomsRelayDirectorPhase.Fade)
        {
            BeginPending(call);
            return true;
        }

        queuedDevice = true;
        queuedPoint = point;
        queuedDeviceId = deviceId;
        CallQueued?.Invoke(new FrontRoomsRelayCall(point, 0, FrontRoomsRelayCallCause.Device,
            true, true, attention));
        return false;
    }

    /// <summary>Called by the map-owned Arrive solver after a valid candidate
    /// exists.  A false result leaves the request pending for another retry.</summary>
    public bool ResolvePending(bool accepted, Vector3 estimate = default(Vector3), int errorCells = 0)
    {
        if (Phase != FrontRoomsRelayDirectorPhase.Pending || pending == null) return false;
        if (!accepted) return false;

        var call = pending;
        if (estimate != default(Vector3))
            call = new FrontRoomsRelayCall(estimate, errorCells, call.Cause, call.Explicit, call.Queued, call.AttentionAtCall);
        pending = null;
        pendingTime = 0f;
        drySpell = 0f;
        SetPhase(FrontRoomsRelayDirectorPhase.Encounter);
        encounterTime = 0f;
        redispatches += call.Cause == FrontRoomsRelayCallCause.Handoff ? 1 : 0;
        Called?.Invoke(call);
        DispatchRequested?.Invoke(call);
        if (call.Cause == FrontRoomsRelayCallCause.Sweep) SweepRequested?.Invoke(call);
        return true;
    }

    public void NotifyStage(int stage)
    {
        if (!armed || !onMap) return;
        currentStage = Mathf.Clamp(stage, 0, 3);
        if (currentStage >= 1) warningFelt = true;
    }

    public void NotifyPlayerSeesRelay(bool value)
    {
        playerSeesRelay = value;
    }

    public void NotifyAlmostSeen(bool value)
    {
        almostSeen = value;
    }

    public void NotifyTargetAcquired()
    {
        if (!armed || !onMap || (Phase != FrontRoomsRelayDirectorPhase.Encounter
            && Phase != FrontRoomsRelayDirectorPhase.Sustain)) return;
        warningFelt = true;
        pressure = Mathf.Min(rules.At(rules.peakThreshold, tier, 100f), pressure + 15f);
        lockOns++;
        currentChaseTime = 0.0001f;
        var cap = rules.At(rules.lockOnCap, tier, 2);
        if (lockOns >= cap) EnterFade();
    }

    public void NotifyChaseEnded()
    {
        currentChaseTime = 0f;
    }

    public bool RequestHandoff(Vector3 estimate, int errorCells = 0)
    {
        if (!armed || !onMap || Phase != FrontRoomsRelayDirectorPhase.Encounter ||
            currentStage != 0 || redispatches >= rules.At(rules.redispatchBudget, tier, 1)) return false;
        var call = new FrontRoomsRelayCall(estimate, errorCells, FrontRoomsRelayCallCause.Handoff,
            true, false, attention);
        redispatches++;
        Called?.Invoke(call);
        DispatchRequested?.Invoke(call);
        return true;
    }

    public void NotifyGaveUp()
    {
        if (!armed || (Phase != FrontRoomsRelayDirectorPhase.Encounter && Phase != FrontRoomsRelayDirectorPhase.Fade)) return;
        EnterFade(FrontRoomsRelayAwayReason.GiveUp);
    }

    /// <summary>Completes Withdraw.  This is the only transition that starts
    /// RELAX and the only place that emits RESTORE.</summary>
    public void NotifyAway(FrontRoomsRelayAwayReason reason)
    {
        if (!armed) return;
        onMap = false;
        var shouldRestore = warningFelt;
        warningFelt = false;
        currentStage = 0;
        almostSeen = false;
        playerSeesRelay = false;
        pressure = 0f;
        encounterTime = 0f;
        sustainRemaining = 0f;
        lockOns = 0;
        redispatches = 0;
        currentChaseTime = 0f;
        attention = rules.initialAttention;
        drySpell = 0f;
        relaxRemaining = rules.At(rules.relaxSeconds, tier, 100f);
        if (reason == FrontRoomsRelayAwayReason.UnbuiltCell)
            relaxRemaining *= .5f;
        SetPhase(FrontRoomsRelayDirectorPhase.Relax);
        if (shouldRestore) Restore?.Invoke();

        if (queuedDevice)
        {
            var queued = new FrontRoomsRelayCall(queuedPoint, 0, FrontRoomsRelayCallCause.Device,
                true, true, attention);
            queuedDevice = false;
            queuedDeviceId = null;
            // Device calls are allowed to leave RELAX immediately.  The
            // player still gets the CALLED event, rather than a silent wake.
        BeginPending(queued);
        }
    }

    void RequestAttentionCall()
    {
        if (Phase != FrontRoomsRelayDirectorPhase.BuildUp || onMap || pending != null) return;
        var estimate = EstimateRecent(out var error);
        BeginPending(new FrontRoomsRelayCall(estimate, error, FrontRoomsRelayCallCause.Attention,
            false, false, attention));
    }

    void RequestSweep()
    {
        if (Phase != FrontRoomsRelayDirectorPhase.BuildUp || onMap || pending != null) return;
        drySpell = 0f;
        var estimate = EstimateRecent(out var ignored);
        BeginPending(new FrontRoomsRelayCall(estimate, 0, FrontRoomsRelayCallCause.Sweep,
            false, false, attention));
    }

    void BeginPending(FrontRoomsRelayCall call)
    {
        pending = call;
        pendingTime = 0f;
        SetPhase(FrontRoomsRelayDirectorPhase.Pending);
        CallQueued?.Invoke(call);
    }

    void DropPending()
    {
        if (pending == null) return;
        var dropped = pending;
        pending = null;
        pendingTime = 0f;
        attention = Mathf.Max(0f, rules.At(rules.callThreshold, tier, 60f) - rules.pendingDropAttention);
        SetPhase(FrontRoomsRelayDirectorPhase.BuildUp);
        PendingDropped?.Invoke(dropped);
        if (dropped.Cause == FrontRoomsRelayCallCause.Device)
        {
            queuedDevice = true;
            queuedPoint = dropped.Estimate;
        }
    }

    void EnterSustain()
    {
        if (Phase != FrontRoomsRelayDirectorPhase.Encounter) return;
        sustainRemaining = Mathf.Max(0f, rules.sustainSeconds);
        SetPhase(FrontRoomsRelayDirectorPhase.Sustain);
    }

    void EnterFade(FrontRoomsRelayAwayReason reason = FrontRoomsRelayAwayReason.Timeout)
    {
        if (Phase == FrontRoomsRelayDirectorPhase.Fade) return;
        SetPhase(FrontRoomsRelayDirectorPhase.Fade);
        SendAwayRequested?.Invoke(reason);
    }

    void SetPhase(FrontRoomsRelayDirectorPhase next)
    {
        if (Phase == next) return;
        Phase = next;
        phaseTime = 0f;
        PhaseChanged?.Invoke(next);
    }

    void AddPressure(float dt)
    {
        if (currentStage >= 2) pressure += 2.5f * dt;
        else if (currentStage == 1) pressure += 1f * dt;
        if (playerSeesRelay) pressure += 4f * dt;
        if (almostSeen && (currentStage >= 2 || playerSeesRelay)) pressure += 5f * dt;
        pressure = Mathf.Min(rules.At(rules.peakThreshold, tier, 100f), pressure);
    }

    float Gain(FrontRoomsRelayDisturbanceKind kind)
    {
        switch (kind)
        {
            case FrontRoomsRelayDisturbanceKind.Sprint: return 3f;
            case FrontRoomsRelayDisturbanceKind.EaseDoor:
            case FrontRoomsRelayDisturbanceKind.Unlock: return 1f;
            case FrontRoomsRelayDisturbanceKind.Barge: return 8f;
            case FrontRoomsRelayDisturbanceKind.Slam: return 12f;
            case FrontRoomsRelayDisturbanceKind.Pry: return 8f;
            default: return 0f;
        }
    }

    int ErrorCells(FrontRoomsRelayDisturbanceKind kind)
    {
        switch (kind)
        {
            case FrontRoomsRelayDisturbanceKind.Sprint: return 2;
            case FrontRoomsRelayDisturbanceKind.EaseDoor:
            case FrontRoomsRelayDisturbanceKind.Unlock:
            case FrontRoomsRelayDisturbanceKind.Barge:
            case FrontRoomsRelayDisturbanceKind.Slam:
            case FrontRoomsRelayDisturbanceKind.Pry: return 1;
            default: return 0;
        }
    }

    Vector3 EstimateRecent(out int errorCells)
    {
        PruneDisturbances();
        var sum = Vector3.zero;
        var total = 0f;
        errorCells = 0;
        for (var i = 0; i < disturbances.Count; i++)
        {
            var d = disturbances[i];
            sum += d.point * d.weight;
            total += d.weight;
            errorCells = Mathf.Max(errorCells, d.errorCells);
        }
        return total > 0f ? sum / total : Vector3.zero;
    }

    void PruneDisturbances()
    {
        for (var i = disturbances.Count - 1; i >= 0; i--)
            if (now - disturbances[i].time > 10f) disturbances.RemoveAt(i);
    }
}
