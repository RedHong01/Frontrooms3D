using UnityEngine;

/// <summary>
/// The light in the Relay's hood (production_b/10_plan.md section 1.5; 20_build.md "Lens rework"): a lamp behind ribbed
/// glass, set deep in the hood; one hot core that falls away, never a lit panel.
/// The level is driven ONLY by the rig's MotionState, the time in it, latched Search snaps and door blows. It never
/// reads the distance to the player, the warning stage, a path length or the light around it: a state light, never
/// a radar (the EditMode test runs the same script with the camera at 2 m and at 30 m and compares the series).
///
///   IdleListen  0.06 standby; from a lit state a 1.0 s phosphor decay
///   Search      0.06 base; up to 3 starter attempts in its first 2.5 s, one per latched head snap (to 0.35)
///   Walk        0.25 steady, a 1.6 m self-light (desktop only)
///   Run         strike 0.9 / 0.4 / 1.0 (3 transitions), then steady, a 3 m light
///   BreakDoor   1.0; each blow a -8 % shudder (below the 10 % flash threshold); the final blow dips to 0.2
///               for 0.3 s and re-strikes
///   Stagger     drops to 0.1, holds 0.3 s, re-strikes
///
/// Photosafety: at most 3 transitions (monotonic swings of 10 % of full or more) in any sliding 1 s window; an
/// extra swing is held below the threshold until the window frees. With Reduce Flashing every pattern is a slow
/// fade and every swing is a ramp of at least 0.5 s that is never cut short (MapWorld.ReducedFlashingEnvelopeSeconds).
/// Pure C#: no allocation per Tick.
/// </summary>
public sealed class FrontRoomsRelayLens
{
    public const float StandbyLevel = .06f, WalkLevel = .25f, FullLevel = 1f;
    public const float TransitionThreshold = .10f, WindowSeconds = 1f, ReducedEnvelopeSeconds = .5f;
    public const int MaxTransitionsPerWindow = 3;
    /// <summary>Full, as a material colour would be set (gamma): 0.36 x Troffer_Lens (2.2, 2.1, 1.8) per channel, about 0.11 of
    /// its linear emission. The plan's 0.6 x clipped to a flat white panel under the game's grade (measured 10-22: the lens at
    /// 205/255 everywhere, no tube band, no tell): exactly the screen read rule 4 forbids, and over the troffers' p99. At this
    /// level the band peaks near white and the gaps and the face-shadow stay visibly darker.</summary>
    public static readonly Color FullEmission = new Color(.80f, .76f, .65f, 1f);
    /// <summary>The self-light at full (Run): a wide spot from the lens along its normal, so the hood's own rim beside the lens
    /// stays dark (a point light 0.25 m out blew the rim out). Walk is 0.25 of it. At 1 m ahead the carpet gets at most about half
    /// a troffer pool (troffer spot 5 at 2.84 m vs this light at ~1.9 m, range 3 m).</summary>
    public const float FullLightIntensity = 1.6f, WalkLightRange = 1.6f, RunLightRange = 3f;
    public static readonly Color LightColour = new Color(1f, .93f, .80f, 1f);

    struct Segment { public float to, seconds; public bool hold; }

    readonly Segment[] queue = new Segment[8];
    int queueHead, queueCount;
    float segFrom, segT;
    bool segActive;
    Segment seg;

    FrontRoomsRelayRig.MotionState state = FrontRoomsRelayRig.MotionState.IdleListen;
    bool hasState;
    float stateTime, baseLevel = StandbyLevel, approachSeconds = 1f;
    float envelope = StandbyLevel;
    float preStaggerBase = WalkLevel;
    int searchPulses;
    bool reducedSearchSwell;

    readonly Limiter lensOut = new Limiter(StandbyLevel);
    readonly Limiter lightOut = new Limiter(0f);
    float clock;

    /// <summary>The lens level, 0..1 of full, after the photosafety limiter.</summary>
    public float Level => lensOut.Output;
    /// <summary>The pattern level before the limiter (for tests and plots).</summary>
    public float Envelope => envelope;
    public FrontRoomsRelayRig.MotionState State => state;
    /// <summary>Counted transitions inside the last 1 s window.</summary>
    public int TransitionsInWindow => lensOut.CountInWindow(clock);
    /// <summary>The self-light, 0..1 of full, after its own photosafety limiter.</summary>
    public float Light01 => lightOut.Output;

    /// <summary>Arrival / re-activation: standby at once (the relay click at the arrival point is the map's lamp, not the body).</summary>
    public void Reset()
    {
        queueHead = queueCount = 0; segActive = false;
        state = FrontRoomsRelayRig.MotionState.IdleListen; hasState = true; stateTime = 0f;
        baseLevel = StandbyLevel; approachSeconds = 1f; envelope = StandbyLevel;
        lensOut.Reset(StandbyLevel);
        lightOut.Reset(0f);
        searchPulses = 0; reducedSearchSwell = false;
    }

    void Push(float to, float seconds, bool hold = false)
    {
        if (queueCount >= queue.Length) return;
        queue[(queueHead + queueCount) % queue.Length] = new Segment { to = to, seconds = Mathf.Max(seconds, 1e-4f), hold = hold };
        queueCount++;
    }

    void ClearQueue() { queueHead = queueCount = 0; segActive = false; }

    /// <summary>A latched head snap while searching: one starter attempt (at most 3 in the first 2.5 s).</summary>
    public void SearchSnap(bool reduceFlashing)
    {
        if (state != FrontRoomsRelayRig.MotionState.Search) return;
        if (reduceFlashing)
        {
            if (!reducedSearchSwell) { reducedSearchSwell = true; baseLevel = .25f; approachSeconds = ReducedEnvelopeSeconds; }
            return;
        }
        if (stateTime > 2.5f || searchPulses >= 3) return;
        searchPulses++;
        ClearQueue();
        Push(.35f, .12f);
        Push(baseLevel, .35f);
    }

    /// <summary>A door blow's impact; the last one is the break-through.</summary>
    public void DoorBlow(bool final, bool reduceFlashing)
    {
        ClearQueue();
        if (final)
        {
            if (reduceFlashing) { Push(.6f, ReducedEnvelopeSeconds); Push(FullLevel, ReducedEnvelopeSeconds); }
            else { Push(.2f, .06f); Push(.2f, .3f, true); Push(FullLevel, .08f); }
        }
        else if (!reduceFlashing)
        {
            var from = Mathf.Max(envelope, .5f);
            Push(from * .92f, .05f);
            Push(baseLevel, .15f);
        }
    }

    void EnterState(FrontRoomsRelayRig.MotionState next, bool reduceFlashing)
    {
        var previous = state;
        var previousBase = baseLevel;
        state = next;
        stateTime = 0f;
        searchPulses = 0;
        reducedSearchSwell = false;
        ClearQueue();
        switch (next)
        {
            case FrontRoomsRelayRig.MotionState.IdleListen:
            case FrontRoomsRelayRig.MotionState.Search:
                baseLevel = StandbyLevel; approachSeconds = 1f;        // the phosphor decay
                break;
            case FrontRoomsRelayRig.MotionState.Walk:
                baseLevel = WalkLevel; approachSeconds = .3f;
                break;
            case FrontRoomsRelayRig.MotionState.Run:
                baseLevel = FullLevel; approachSeconds = .3f;
                if (previous != FrontRoomsRelayRig.MotionState.BreakDoor && previous != FrontRoomsRelayRig.MotionState.Stagger && envelope < .85f)
                {
                    if (reduceFlashing) Push(FullLevel, ReducedEnvelopeSeconds);
                    else { Push(.9f, .08f); Push(.4f, .12f); Push(FullLevel, .08f); }   // the strike: 3 transitions
                }
                break;
            case FrontRoomsRelayRig.MotionState.BreakDoor:
                baseLevel = FullLevel; approachSeconds = .3f;
                break;
            case FrontRoomsRelayRig.MotionState.Stagger:
                preStaggerBase = previousBase;
                baseLevel = previousBase; approachSeconds = .3f;
                if (reduceFlashing) { Push(Mathf.Max(.1f, previousBase * .5f), ReducedEnvelopeSeconds); Push(previousBase, ReducedEnvelopeSeconds); }
                else { Push(.1f, .05f); Push(.1f, .3f, true); Push(preStaggerBase, .08f); }
                break;
        }
    }

    /// <summary>One frame. The state is the rig's MotionState; nothing positional goes in.</summary>
    public void Tick(float dt, FrontRoomsRelayRig.MotionState next, bool reduceFlashing)
    {
        dt = Mathf.Max(0f, dt);
        clock += dt;
        if (!hasState) { hasState = true; state = next; EnterState(next, reduceFlashing); }
        else if (next != state) EnterState(next, reduceFlashing);
        stateTime += dt;

        // ---- envelope: queued segments, then the state's base level
        var left = dt;
        while (left > 0f)
        {
            if (!segActive)
            {
                if (queueCount == 0) break;
                seg = queue[queueHead];
                queueHead = (queueHead + 1) % queue.Length;
                queueCount--;
                segActive = true;
                segFrom = envelope;
                segT = 0f;
            }
            var step = Mathf.Min(left, seg.seconds - segT);
            segT += step;
            left -= step;
            envelope = seg.hold ? seg.to : Mathf.Lerp(segFrom, seg.to, segT / seg.seconds);
            if (segT >= seg.seconds - 1e-6f) { envelope = seg.to; segActive = false; }
        }
        if (!segActive && queueCount == 0 && left > 0f)
        {
            // linear approach at the state's rate (a full 0.94 swing in approachSeconds)
            var rate = .94f / Mathf.Max(approachSeconds, 1e-3f);
            envelope = Mathf.MoveTowards(envelope, baseLevel, rate * left);
        }

        // ---- outputs: the lens, and the self-light through its own limiter (its on/off follows the state)
        lensOut.Step(envelope, dt, clock, reduceFlashing, approachSeconds);
        var lightTarget = LightAllowed(allowWalkLight) ? lensOut.Output : 0f;
        lightOut.Step(lightTarget, dt, clock, reduceFlashing, .3f);
    }

    bool allowWalkLight = true;

    /// <summary>The platform tier: false on WebGL (the Chase light only). Set before Tick.</summary>
    public bool AllowWalkLight { get => allowWalkLight; set => allowWalkLight = value; }

    bool LightAllowed(bool walk)
    {
        switch (state)
        {
            case FrontRoomsRelayRig.MotionState.Walk: return walk;
            case FrontRoomsRelayRig.MotionState.Run:
            case FrontRoomsRelayRig.MotionState.BreakDoor:
            case FrontRoomsRelayRig.MotionState.Stagger: return true;
            default: return false;        // Listen and Search: no own light
        }
    }

    /// <summary>The self-light is drawn while its limited level is above 2 % of full.</summary>
    public bool LightOn => lightOut.Output > .02f;
    public float LightIntensity => lightOut.Output * FullLightIntensity;
    public float LightRange => Mathf.Lerp(WalkLightRange, RunLightRange, Mathf.InverseLerp(WalkLevel, FullLevel, lensOut.Output));

    /// <summary>
    /// Photosafety for one signal (0..1 of full). Normal: at most 3 transitions (monotonic swings of 10 % of full or
    /// more from the last turning point) in any sliding 1 s window; a fourth swing is held just under the threshold
    /// until the oldest stamp ages out. Reduce Flashing: every change is a linear ramp of at least 0.5 s that finishes
    /// before the next one starts, so no swing is shorter than 0.5 s. No allocation.
    /// </summary>
    sealed class Limiter
    {
        float output, extremum, lastY;
        int direction;
        bool counted;
        readonly float[] stamps = new float[16];
        int head, count;
        float rampFrom, rampTo, rampT, rampSeconds;
        bool ramping;

        public Limiter(float start) { Reset(start); }
        public float Output => output;

        public void Reset(float v)
        {
            output = extremum = lastY = rampTo = v;
            direction = 0; counted = false; head = count = 0; ramping = false;
        }

        public int CountInWindow(float clock) { Prune(clock); return count; }

        public void Step(float desired, float dt, float clock, bool reduced, float approachSeconds)
        {
            if (reduced) Ramp(desired, dt, approachSeconds);
            else Limit(desired, clock);
            Track(clock);
        }

        void Ramp(float desired, float dt, float approachSeconds)
        {
            if (!ramping && Mathf.Abs(desired - output) > .004f)
            {
                ramping = true;
                rampFrom = output;
                rampTo = desired;
                rampT = 0f;
                rampSeconds = Mathf.Max(ReducedEnvelopeSeconds + .02f, Mathf.Abs(rampTo - rampFrom) / .94f * approachSeconds);   // + a frame of margin
            }
            if (!ramping) return;
            rampT += dt;
            output = Mathf.Lerp(rampFrom, rampTo, Mathf.Clamp01(rampT / rampSeconds));
            if (rampT >= rampSeconds) { output = rampTo; ramping = false; }
        }

        void Limit(float desired, float clock)
        {
            var dir = desired > output + 1e-5f ? 1 : desired < output - 1e-5f ? -1 : 0;
            if (dir != 0)
            {
                var fresh = direction == 0 || dir != direction;
                var from = fresh ? output : extremum;
                var wouldCount = Mathf.Abs(desired - from) >= TransitionThreshold && (fresh || !counted);
                Prune(clock);
                if (wouldCount && count >= MaxTransitionsPerWindow) desired = from + dir * (TransitionThreshold - .002f);
            }
            output = desired;
        }

        void Track(float clock)
        {
            var y = output;
            if (direction == 0)
            {
                if (Mathf.Abs(y - extremum) > 1e-5f) { direction = y > extremum ? 1 : -1; counted = false; }
            }
            else if (direction > 0 && y < lastY - 1e-5f) { extremum = lastY; direction = -1; counted = false; }
            else if (direction < 0 && y > lastY + 1e-5f) { extremum = lastY; direction = 1; counted = false; }
            if (direction != 0 && !counted && Mathf.Abs(y - extremum) >= TransitionThreshold)
            {
                counted = true;
                stamps[(head + count) % stamps.Length] = clock;
                if (count < stamps.Length) count++; else head = (head + 1) % stamps.Length;
            }
            lastY = y;
        }

        void Prune(float clock)
        {
            while (count > 0 && clock - stamps[head] > WindowSeconds - 1e-4f)
            {
                head = (head + 1) % stamps.Length;
                count--;
            }
        }
    }
}
