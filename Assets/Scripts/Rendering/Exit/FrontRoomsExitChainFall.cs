using UnityEngine;
using K = FrontRoomsExitChainKit;

/// <summary>
/// The EXIT unlock cut's chain fall after the slip (Red 2026-11-01: "...整个锁链和锁落下来的速度非常快，而且僵硬..."; of the five
/// options Red took U1, real gravity: "可以选择U1，但是我不喜欢后面它被force成直的这部分"; research/unlock/20_design.md §11).
/// Owner: the game visual line.
/// <list type="bullet">
/// <item><description>A position-based chain: the 35 link centres at real g (70 g a link) and the open padlock's centre of mass
/// (0.38 kg) on a rigid rod from the heel-side end link. Links resist stretch, not compression (they bunch to half a pitch).</description></item>
/// <item><description>The one cheat: the turn round the crossbar leaves the bar in 0.10 s on its far side, hidden from the dipped
/// camera (a wound turn cannot leave a bar fixed at both ends by gravity).</description></item>
/// <item><description>Then everything is physics: the long side falls from the bar and swings along the wall from the staple, the
/// padlock swings on the lower run, the long side's weight runs the chain through the staple (one coordinate, Coulomb
/// friction) and the padlock jams against it (the knock). From the knock the staple's link sits on the staple's wire,
/// the heel link and the long side hanging from its two ends, and link friction lets the sway die out.</description></item>
/// <item><description>No pull toward a drawn shape: the chain stops where it falls (twists, kinks, the tail on the carpet, the padlock
/// hanging tilted under the staple). It sleeps once nothing moves faster than 4 mm/s for 0.25 s.</description></item>
/// </list>
/// UNLOCKED is this fall's own rest (FrontRoomsExitChainKit.UnlockedNodes, baked from this class on the EXIT's standard
/// geometry). Once asleep, the last 0.4 s close the float-sized gap between this run and the bake (a run on another device
/// or frame rate differs by rounding only), so the cached UNLOCKED meshes take over with no pop.
/// Fixed 1/240 s steps advanced from the cut's clock: the same input gives the same motion at any frame rate (the leaf's
/// crack is read once a frame and interpolated). The same on desktop, iPhone and iPad. No allocation after construction.
/// </summary>
public sealed class FrontRoomsExitChainFall
{
    // ---------------------------------------------------------------- sync points this fall raises (names only)

    const string ChainRun = "Exit.ChainRun", PadlockKnock = "Exit.PadlockKnock", ChainSway = "Exit.ChainSway", ChainFloor = "Exit.ChainFloor";
    const string PadlockTap = "Exit.PadlockTap", ChainClink = "Exit.ChainClink";

    // ---------------------------------------------------------------- physics (SI, the door's shut frame)

    public const float Step = 1f / 240f;
    public const int Iterations = 12;
    public const float Gravity = 9.81f, LinkMass = .07f, PadlockMass = .38f;
    const float LinkRadius = .011f, PadlockRadius = .021f, BarContact = .029f, BarHingeZ = .25f;
    const float WallX = .08f, PlateX = .086f, PlateHalfY = .0635f, PlateHalfZ = .038f, CasingX = .105f, CasingZ0 = .995f, CasingZ1 = 1.075f;
    const float LeafX = .025f, LeafZ1 = .99f, LeafY1 = 2.1f;
    const float LatchX1 = .12f, LatchY0 = .92f, LatchY1 = 1.08f, LatchZ0 = .8525f, LatchZ1 = .955f;
    // friction: the bar, the carpet (a link that lands stays where it lands: static 1.4, sliding 1.0), the wall and the casing;
    // the staple's wire is smooth (the long side runs the chain through)
    const float BarMu = .25f, FloorMu = 1f, FloorMuS = 1.4f, WallMu = .3f, StapleMuS = .12f, StapleMuK = .08f;
    // damping (per second): in the air; from the knock (over RestDampingIn) link friction on the chain and shackle friction on
    // the padlock, so the hanging side dies out in about two swings and the padlock in a few; below StickSpeed the joints'
    // static friction takes the last of it (StickDamping), so the chain keeps the shape its last swing leaves
    const float AirDamping = .45f, RestDamping = 2f, LockRestDamping = 6f, RestDampingIn = .3f, PadlockRestitution = .2f;
    const float StickSpeed = .08f, StickDamping = 20f, StickIn = .3f;
    // the cheat: the turn's links are drawn forward and down off the bar's far side (shut frame), the end of the turn first
    const float CheatSeconds = .10f, CheatTau = .012f;
    static readonly Vector3 CheatPush = new Vector3(.04f, -.075f, 0f);
    // a haul that stalls is carried on from here (the haul is over by 1.67, so this only guards odd starts)
    const float AssistFrom = 1.8f, AssistSpeed = .45f;
    // the staple's link at the knock: its two end wires' centres 22.25 mm either side of the apex along the wall; a link
    // hanging on a wire has its centre 12.75 mm from it (inside half-length 17.5 mm less the wire's 4.75 mm). The heel
    // link comes to hang from the door side's end, the long side from the far end, over JamEase
    const float StapleEnd = .02225f, HangReach = .01275f, JamEase = .25f;
    // the rest: asleep when no link and not the padlock moves faster than this for SleepSeconds (or by LongestFall after the
    // slip); then CloseSeconds onto the baked rest
    const float SleepSpeed = .004f, SleepSeconds = .25f, CloseSeconds = .4f, LongestFall = 5.5f;
    // the links' rest twist comes in over the fall (they turn as they fall), whole by the knock
    const float TwistFrom = 1.0f, TwistSeconds = .6f;

    // ---------------------------------------------------------------- state

    readonly int n;
    readonly bool closeOntoRest;
    readonly float d0, slip, rodLength, sigmaStart;
    readonly Vector3[] x, prev, start, before, slipX, contactN, sleepX;
    readonly float[] w, contactD, contactMu;
    readonly bool[] ghost;
    readonly Vector3 heelLocal, dirLocal;
    Vector3 lockP, lockPrev, lockStart, lockN, lockVelocityIn, lockSleep;
    float lockD;
    Quaternion rotationRef, rotationSlip;
    float sigma, slideSpeed, simClock, simTarget, lastCut, cutAtJam = -1f, stapleAssist, stillFor, sleepClock = -1f;
    int wrapStartNode, wrapEndNode;
    Vector3 longVel, longAcc, shortVel, shortAcc;
    bool jammed, floorSeen, run1Seen, run2Seen, done;
    float lastTap = -1f, lastClink = -1f, endClock = -1f, jamSim = -1f, sleepCut = -1f;
    int clinks;
    // the leaf (bar, latch case, face): its pose in the shut frame now, one frame ago, and at the step
    Quaternion leafRot, leafRotPrev, leafRotSub;
    Vector3 leafPos, leafPosPrev, leafPosSub;
    // cues raised since the last ClearCues
    readonly string[] cueName = new string[16];
    readonly Vector3[] cueAt = new Vector3[16];
    int cueCount;

    /// <summary>The fall is over: the chain and the padlock are exactly UNLOCKED (the cut ends and the cached meshes take over).</summary>
    public bool Done => done;
    /// <summary>The cut clock at which the fall ended (-1 before).</summary>
    public float EndClock => endClock;
    /// <summary>The padlock has jammed against the staple (the knock), and when (cut clock; -1 before).</summary>
    public bool Jammed => jammed;
    public float JamClock => cutAtJam;
    /// <summary>The chain has come to rest, and when (cut clock; -1 before): from here it closes onto the baked rest.</summary>
    public bool Asleep => sleepClock >= 0f;
    public float SleepClock => sleepCut;
    /// <summary>The fall's own clock (the cut's, from the slip).</summary>
    public float SimClock => simClock;

    /// <summary>
    /// Take over at the slip (<paramref name="slipClock"/>, the cut's Slip) from the chain's path then (shut frame, ending on
    /// the end links), the padlock's pose then (top-face centre, rotation; the shackle open), and the leaf's pose in the
    /// shut frame (identity while shut). <paramref name="closeOntoRest"/>: false only for the tool that bakes the rest.
    /// </summary>
    public FrontRoomsExitChainFall(float slipClock, Vector3[] slipPath, Vector3 padlockTop, Quaternion padlockRotation, Matrix4x4 leafToShut, bool closeOntoRest = true)
    {
        this.closeOntoRest = closeOntoRest;
        slip = slipClock;
        n = K.Links;
        x = new Vector3[n]; prev = new Vector3[n]; start = new Vector3[n]; before = new Vector3[n]; slipX = new Vector3[n]; sleepX = new Vector3[n];
        contactN = new Vector3[n]; contactD = new float[n]; contactMu = new float[n]; w = new float[n]; ghost = new bool[n];
        // the links as they are at the slip (where K.Place puts them)
        var total = Length(slipPath);
        d0 = total / (n - 1);
        for (var i = 0; i < n; i++)
        {
            x[i] = PointAt(slipPath, i * d0);
            prev[i] = x[i];
            w[i] = 1f / LinkMass;
        }
        wrapStartNode = Mathf.FloorToInt(K.WrapStartFraction * (n - 1));
        wrapEndNode = Mathf.FloorToInt(K.WrapEndFraction * (n - 1)) + 1;
        sigma = sigmaStart = K.StapleFraction * (n - 1);
        // the padlock: a rigid rod from the heel-side end link to its centre of mass
        heelLocal = K.BowPin(K.OpenShackleLocal, K.HeelPinDeg).centre;
        var rod = K.BodyMassLocal - heelLocal;
        rodLength = rod.magnitude;
        dirLocal = rod / rodLength;
        rotationSlip = rotationRef = padlockRotation;
        // node 0 is the heel link: where the padlock carries it, the padlock's mass along the rod
        x[0] = prev[0] = padlockTop + padlockRotation * heelLocal;
        lockP = lockPrev = x[0] + padlockRotation * dirLocal * rodLength;
        SetLeaf(leafToShut, true);
        System.Array.Copy(x, slipX, n);
        simClock = simTarget = lastCut = slip;
        // the turn round the bar: its links leave the bar on its far side (no bar contact until clear)
        for (var i = wrapStartNode + 1; i < n; i++) ghost[i] = true;
    }

    // ---------------------------------------------------------------- the clock

    /// <summary>Step the fall to the cut clock <paramref name="cutClock"/> with the leaf's pose now (shut frame).</summary>
    public void Advance(float cutClock, Matrix4x4 leafToShut)
    {
        if (done) return;
        SetLeaf(leafToShut, false);
        if (cutClock <= lastCut) return;
        var simFrom = simTarget;
        simTarget += cutClock - lastCut;
        lastCut = cutClock;
        var steps = 0;
        while (simClock + Step <= simTarget + 1e-6f && steps < 64)
        {
            if (sleepClock < 0f)
            {
                var u = Mathf.Clamp01((simClock + Step - simFrom) / Mathf.Max(1e-6f, simTarget - simFrom));
                leafRotSub = Quaternion.Slerp(leafRotPrev, leafRot, u);
                leafPosSub = Vector3.Lerp(leafPosPrev, leafPos, u);
                SimStep(cutClock);
            }
            simClock += Step;
            steps++;
        }
        if (steps >= 64) simClock = simTarget;
        if (sleepClock >= 0f && (!closeOntoRest || simClock >= sleepClock + CloseSeconds))
        {
            done = true;
            endClock = cutClock;
        }
    }

    void SetLeaf(Matrix4x4 m, bool reset)
    {
        var r = Quaternion.LookRotation(m.MultiplyVector(Vector3.forward), m.MultiplyVector(Vector3.up));
        var p = m.MultiplyPoint3x4(Vector3.zero);
        if (reset)
        {
            leafRot = leafRotPrev = leafRotSub = r;
            leafPos = leafPosPrev = leafPosSub = p;
            return;
        }
        leafRotPrev = leafRot;
        leafPosPrev = leafPos;
        leafRot = r;
        leafPos = p;
    }

    Vector3 ToLeaf(Vector3 p) => Quaternion.Inverse(leafRotSub) * (p - leafPosSub);
    Vector3 FromLeaf(Vector3 q) => leafRotSub * q + leafPosSub;

    // ---------------------------------------------------------------- one step of the simulation

    void SimStep(float cut)
    {
        var t = simClock + Step;
        var h = Step;
        RunCue(t);
        var rest = jammed ? Mathf.Clamp01((t - jamSim) / RestDampingIn) : 0f;
        var damp = 1f - Mathf.Lerp(AirDamping, RestDamping, rest) * h;
        var g = new Vector3(0f, -Gravity * h * h, 0f);
        var stick = jammed ? Mathf.Clamp01((t - jamSim - RestDampingIn) / StickIn) : 0f;
        var stickStep = StickSpeed * h;
        for (var i = 0; i < n; i++)
        {
            before[i] = x[i];
            var v = (x[i] - prev[i]) * damp;
            if (stick > 0f && v.sqrMagnitude < stickStep * stickStep) v *= 1f - stick * StickDamping * h;
            prev[i] = x[i];
            start[i] = x[i];
            if (w[i] > 0f) x[i] += v + g;
            contactD[i] = 0f;
            contactMu[i] = 0f;
            contactN[i] = Vector3.zero;
        }
        lockVelocityIn = (lockP - lockPrev) * (1f - Mathf.Lerp(AirDamping, LockRestDamping, rest) * h) / h;
        if (stick > 0f && lockVelocityIn.sqrMagnitude < StickSpeed * StickSpeed) lockVelocityIn *= 1f - stick * StickDamping * h;
        lockPrev = lockP;
        lockStart = lockP;
        lockP += lockVelocityIn * h + g;
        lockD = 0f;
        lockN = Vector3.zero;

        Cheat(t);

        var sigma0 = sigma;
        // the run through the staple, in one coordinate: the two sides' pulls (Haul), Coulomb friction, the whole chain and
        // the padlock as the moving mass
        if (!jammed && t > slip + CheatSeconds) Haul(t);
        else slideSpeed = 0f;
        var staple = K.StapleApex;
        for (var it = 0; it < Iterations; it++)
        {
            var forward = (it & 1) == 0;
            for (var k = 0; k < n - 1; k++)
            {
                var i = forward ? k : n - 2 - k;
                Distance(i, i + 1);
            }
            Rod();
            if (jammed) StapleLink(staple, Mathf.SmoothStep(0f, 1f, (t - jamSim) / JamEase));
            else
            {
                Ring(sigma, staple);
                Attach(staple, sigma);
            }
            for (var i = 0; i < n; i++) CollideNode(i);
            CollideLock();
        }
        Friction();
        LockBounce();
        Events(t, cut, sigma0);
        Rest(t, cut);
    }

    void Distance(int i, int j)
    {
        var d = x[j] - x[i];
        var len = d.magnitude;
        if (len < 1e-7f) return;
        float target;
        if (len > d0) target = d0;
        else if (len < d0 * .5f) target = d0 * .5f;
        else return;
        var wi = w[i];
        var wj = w[j];
        if (wi + wj <= 0f) return;
        var c = (len - target) / (len * (wi + wj));
        x[i] += d * (c * wi);
        x[j] -= d * (c * wj);
    }

    // The padlock hangs on the heel-side end link (node 0) by its rod.
    void Rod()
    {
        var d = lockP - x[0];
        var len = d.magnitude;
        if (len < 1e-7f) return;
        var wl = 1f / PadlockMass;
        var w0 = w[0];
        var c = (len - rodLength) / (len * (wl + w0));
        x[0] += d * (c * w0);
        lockP -= d * (c * wl);
    }

    // Before the knock the chain passes the staple's apex at s (node units): its point there is held on the apex; along the
    // chain s moves only by the haul.
    void Ring(float s, Vector3 S)
    {
        var i = Mathf.Clamp((int)s, 0, n - 2);
        var a = Mathf.Clamp01(s - i);
        var seg = x[i + 1] - x[i];
        if (seg.sqrMagnitude < 1e-12f) return;
        var p = x[i] * (1f - a) + x[i + 1] * a;
        var e = S - p;
        var wi = w[i] * (1f - a);
        var wj = w[i + 1] * a;
        var W = wi * (1f - a) + wj * a;
        if (W < 1e-9f) return;
        x[i] += e * (wi / W);
        x[i + 1] += e * (wj / W);
    }

    // From the knock: the staple's link (node 1) sits on the staple's wire; the heel link (node 0) comes to hang from its
    // door-side end, the long side (node 2 on) from its far end, never farther from it than the chain between them. Over
    // the first JamEase (u 0 -> 1) the two hold points slide from the apex out to the link's ends and the reach tightens from
    // a pitch to a hang, so the long side is taken up its 2 cm a little each step (no snap, no kick).
    void StapleLink(Vector3 S, float u)
    {
        x[1] = S;
        var reach = Mathf.Lerp(d0, HangReach, u);
        Reach(0, S + new Vector3(0f, 0f, -StapleEnd * u), reach);
        Reach(2, S + new Vector3(0f, 0f, StapleEnd * u), reach);
        var a = x[2];
        for (var i = 3; i < n; i++) Reach(i, a, (i - 2) * d0 + .002f);
    }

    void Reach(int i, Vector3 P, float max)
    {
        var d = x[i] - P;
        var len = d.magnitude;
        if (len > max) x[i] = P + d * (max / len);
    }

    // The pull of each side on the staple: the force the staple must give that side's hanging mass for the acceleration it has
    // (M (a - g), along the line from the staple to its centre of mass). A side at rest pulls its weight, a falling side
    // nothing, a side caught at the bottom of its swing more than its weight.
    float SidePull(int from, int to, bool padlock, ref Vector3 velPrev, ref Vector3 accSmooth)
    {
        var S = K.StapleApex;
        var m = 0f;
        var c = Vector3.zero;
        var v = Vector3.zero;
        for (var i = from; i <= to; i++)
        {
            if (x[i].y <= K.FloorLinkHeight + .003f) continue;
            m += LinkMass;
            c += x[i] * LinkMass;
            v += (x[i] - prev[i]) * LinkMass;
        }
        if (padlock)
        {
            m += PadlockMass;
            c += lockP * PadlockMass;
            v += (lockP - lockPrev) * PadlockMass;
        }
        if (m <= 0f) return 0f;
        c /= m;
        v /= m * Step;
        var a = (v - velPrev) / Step;
        velPrev = v;
        accSmooth += (a - accSmooth) * .25f;
        var u = S - c;
        var len = u.magnitude;
        if (len < 1e-5f) return 0f;
        u /= len;
        var f = (accSmooth - new Vector3(0f, -Gravity, 0f)) * m;
        return Mathf.Max(0f, Vector3.Dot(f, u));
    }

    void Haul(float t)
    {
        var h = Step;
        var at = Mathf.Clamp((int)sigma, 0, n - 1);
        var longPull = SidePull(at + 1, n - 1, false, ref longVel, ref longAcc);
        var shortPull = SidePull(0, at, true, ref shortVel, ref shortAcc);
        var net = longPull - shortPull;
        var load = longPull + shortPull;
        var mass = n * LinkMass + PadlockMass;
        if (Mathf.Abs(slideSpeed) < .004f && Mathf.Abs(net) <= StapleMuS * load)
            slideSpeed = 0f;
        else
        {
            var dir = Mathf.Abs(slideSpeed) >= .004f ? Mathf.Sign(slideSpeed) : Mathf.Sign(net);
            var a = (net - dir * StapleMuK * load) / mass;
            var v = slideSpeed + a * h;
            // kinetic friction stops the run, it never reverses it
            if (Mathf.Abs(slideSpeed) >= .004f && Mathf.Sign(v) != dir) v = 0f;
            slideSpeed = v;
        }
        if (t >= AssistFrom)
        {
            stapleAssist = Mathf.Min(AssistSpeed, stapleAssist + h * 1.2f);
            if (slideSpeed < stapleAssist) slideSpeed = Mathf.Min(stapleAssist, slideSpeed + h * 2.5f);
        }
        // + slideSpeed runs the chain toward the long side: the staple's point moves toward the padlock end
        sigma = Mathf.Clamp(sigma - slideSpeed * h / d0, 1f, n - 1.5f);
    }

    // Long-range attachment from the staple: no link is farther from it than the chain between them (no stretch).
    void Attach(Vector3 S, float s)
    {
        for (var i = 0; i < n; i++) Reach(i, S, Mathf.Abs(i - s) * d0 + .002f);
    }

    // ---------------------------------------------------------------- contacts

    void Push(int i, Vector3 normal, float depth, float mu)
    {
        x[i] += normal * depth;
        contactN[i] = normal;
        if (depth > contactD[i]) contactD[i] = depth;
        if (mu > contactMu[i]) contactMu[i] = mu;
    }

    void CollideNode(int i)
    {
        if (w[i] <= 0f) return;
        var p = x[i];
        // floor
        if (p.y < K.FloorLinkHeight) Push(i, Vector3.up, K.FloorLinkHeight - p.y, FloorMu);
        p = x[i];
        // the wall past the opening, the staple plate on it
        if (p.z > CasingZ1)
        {
            var face = WallX;
            if (Mathf.Abs(p.y - K.StapleAnchor.y) < PlateHalfY + LinkRadius && Mathf.Abs(p.z - K.StapleAnchor.z) < PlateHalfZ + LinkRadius) face = PlateX;
            if (p.x < face + LinkRadius) Push(i, Vector3.right, face + LinkRadius - p.x, WallMu);
        }
        p = x[i];
        // the latch jamb's casing (shut frame box)
        if (p.x < CasingX + LinkRadius && p.z > CasingZ0 - LinkRadius && p.z < CasingZ1 + LinkRadius && p.x > -.2f)
        {
            var dx = CasingX + LinkRadius - p.x;
            var dz0 = p.z - (CasingZ0 - LinkRadius);
            var dz1 = CasingZ1 + LinkRadius - p.z;
            if (dx <= dz0 && dx <= dz1) Push(i, Vector3.right, dx, WallMu);
            else if (dz0 < dz1) Push(i, Vector3.back, dz0, WallMu);
            else Push(i, Vector3.forward, dz1, WallMu);
        }
        // the leaf, its latch case and its bar (leaf frame)
        var q = ToLeaf(x[i]);
        var changed = false;
        var nLeaf = Vector3.zero;
        var depth = 0f;
        var mu = WallMu;
        if (q.z > 0f && q.z < LeafZ1 && q.y > 0f && q.y < LeafY1 && q.x < LeafX + LinkRadius && q.x > -LeafX)
        {
            depth = LeafX + LinkRadius - q.x;
            q.x = LeafX + LinkRadius;
            nLeaf = Vector3.right;
            changed = true;
        }
        if (q.x < LatchX1 + LinkRadius && q.x > LeafX && q.y > LatchY0 - LinkRadius && q.y < LatchY1 + LinkRadius && q.z > LatchZ0 - LinkRadius && q.z < LatchZ1 + LinkRadius)
        {
            var dx = LatchX1 + LinkRadius - q.x;
            var dy0 = q.y - (LatchY0 - LinkRadius);
            var dy1 = LatchY1 + LinkRadius - q.y;
            var dz0 = q.z - (LatchZ0 - LinkRadius);
            var dz1 = LatchZ1 + LinkRadius - q.z;
            var m = Mathf.Min(Mathf.Min(dx, dy0), Mathf.Min(dy1, Mathf.Min(dz0, dz1)));
            if (m == dx) { q.x += dx; nLeaf = Vector3.right; }
            else if (m == dy1) { q.y += dy1; nLeaf = Vector3.up; }
            else if (m == dy0) { q.y -= dy0; nLeaf = Vector3.down; }
            else if (m == dz1) { q.z += dz1; nLeaf = Vector3.forward; }
            else { q.z -= dz0; nLeaf = Vector3.back; }
            depth += m;
            changed = true;
        }
        // the bar: a capsule; a link of the wound turn is a ghost until it is clear
        var bar = K.BarLatchEnd;
        var bz = Mathf.Clamp(q.z, BarHingeZ, bar.z);
        var radial = new Vector3(q.x - bar.x, q.y - bar.y, q.z - bz);
        var r = radial.magnitude;
        if (ghost[i])
        {
            if (r > BarContact + .004f && simClock > slip + CheatSeconds) ghost[i] = false;
        }
        else if (r < BarContact && r > 1e-6f)
        {
            var nb = radial / r;
            q += nb * (BarContact - r);
            depth += BarContact - r;
            nLeaf = nb;
            mu = BarMu;
            changed = true;
        }
        if (changed)
        {
            contactN[i] = leafRotSub * nLeaf;
            if (depth > contactD[i]) contactD[i] = depth;
            if (mu > contactMu[i]) contactMu[i] = mu;
            x[i] = FromLeaf(q);
        }
    }

    void CollideLock()
    {
        var p = lockP;
        var r = PadlockRadius;
        if (p.y < r) { lockP.y = r; LockContact(Vector3.up, r - p.y); }
        p = lockP;
        if (p.z > CasingZ1)
        {
            var face = WallX;
            if (Mathf.Abs(p.y - K.StapleAnchor.y) < PlateHalfY + r && Mathf.Abs(p.z - K.StapleAnchor.z) < PlateHalfZ + r) face = PlateX;
            if (p.x < face + r) { lockP.x = face + r; LockContact(Vector3.right, face + r - p.x); }
        }
        p = lockP;
        if (p.x < CasingX + r && p.z > CasingZ0 - r && p.z < CasingZ1 + r && p.x > -.2f)
        {
            var dx = CasingX + r - p.x;
            var dz0 = p.z - (CasingZ0 - r);
            var dz1 = CasingZ1 + r - p.z;
            if (dx <= dz0 && dx <= dz1) { lockP.x += dx; LockContact(Vector3.right, dx); }
            else if (dz0 < dz1) { lockP.z -= dz0; LockContact(Vector3.back, dz0); }
            else { lockP.z += dz1; LockContact(Vector3.forward, dz1); }
        }
        var q = ToLeaf(lockP);
        if (q.z > 0f && q.z < LeafZ1 && q.y > 0f && q.y < LeafY1 && q.x < LeafX + r && q.x > -LeafX)
        {
            var dx = LeafX + r - q.x;
            q.x += dx;
            lockP = FromLeaf(q);
            LockContact(leafRotSub * Vector3.right, dx);
        }
        if (q.x < LatchX1 + r && q.x > LeafX && q.y > LatchY0 - r && q.y < LatchY1 + r && q.z > LatchZ0 - r && q.z < LatchZ1 + r)
        {
            var dx = LatchX1 + r - q.x;
            var dy0 = q.y - (LatchY0 - r);
            var dz0 = q.z - (LatchZ0 - r);
            var dz1 = LatchZ1 + r - q.z;
            var m = Mathf.Min(Mathf.Min(dx, dy0), Mathf.Min(dz0, dz1));
            Vector3 nl;
            if (m == dx) { q.x += dx; nl = Vector3.right; }
            else if (m == dy0) { q.y -= dy0; nl = Vector3.down; }
            else if (m == dz1) { q.z += dz1; nl = Vector3.forward; }
            else { q.z -= dz0; nl = Vector3.back; }
            lockP = FromLeaf(q);
            LockContact(leafRotSub * nl, m);
        }
    }

    void LockContact(Vector3 normal, float depth)
    {
        lockN = normal;
        if (depth > lockD) lockD = depth;
    }

    // Coulomb friction on what each contact moved this step (static under mu x depth, kinetic above).
    void Friction()
    {
        for (var i = 0; i < n; i++)
        {
            if (contactD[i] <= 0f) continue;
            var nrm = contactN[i];
            var d = x[i] - start[i];
            var dt = d - nrm * Vector3.Dot(d, nrm);
            var m = dt.magnitude;
            if (m < 1e-9f) continue;
            var mu = contactMu[i];
            var muS = mu == FloorMu ? FloorMuS : mu * 1.3f;
            if (m < muS * contactD[i]) x[i] -= dt;
            else x[i] -= dt * Mathf.Min(1f, mu * contactD[i] / m);
        }
        if (lockD > 0f)
        {
            var d = lockP - lockStart;
            var dt = d - lockN * Vector3.Dot(d, lockN);
            var m = dt.magnitude;
            if (m > 1e-9f) lockP -= dt * Mathf.Min(1f, .4f * lockD / m);
        }
    }

    // The padlock bounces off what it hits by its restitution; a hit before the knock is a sync point (a tap).
    void LockBounce()
    {
        if (lockD <= 0f) return;
        var vn = Vector3.Dot(lockVelocityIn, lockN);
        if (vn >= -.05f) return;
        var v = (lockP - lockPrev) / Step;
        var vt = v - lockN * Vector3.Dot(v, lockN);
        var vOut = vt + lockN * (-vn * PadlockRestitution);
        lockPrev = lockP - vOut * Step;
        if (!jammed && -vn > .2f && simClock - lastTap > .12f)
        {
            lastTap = simClock;
            Cue(PadlockTap, lockP);
        }
    }

    // ---------------------------------------------------------------- the cheat

    // The wound turn leaves the bar: its links are drawn forward and down off the bar's far side (hidden from the dipped
    // camera), the end of the turn first, its start last.
    void Cheat(float t)
    {
        var cheatEnd = slip + CheatSeconds;
        if (t >= cheatEnd) return;
        var u = Mathf.Clamp01((t - slip) / CheatSeconds);
        var k = 1f - Mathf.Exp(-Step / CheatTau);
        for (var i = wrapStartNode + 1; i <= wrapEndNode && i < n; i++)
        {
            var lead = (wrapEndNode - i) / (float)Mathf.Max(1, wrapEndNode - wrapStartNode);
            var ui = Mathf.Clamp01((u - lead * .35f) / .65f);
            if (ui <= 0f) continue;
            var target = slipX[i] + CheatPush * ui;
            x[i] += (target - x[i]) * k;
        }
    }

    // ---------------------------------------------------------------- sync points and the rest

    void Events(float t, float cut, float sigma0)
    {
        // the haul: the chain starts to run through the staple toward the long side
        var speed = (sigma0 - sigma) * d0 / Step;
        if (!run2Seen && speed > .12f && t > slip + CheatSeconds)
        {
            run2Seen = true;
            Cue(ChainRun, K.StapleApex);
        }
        if (!jammed && sigma <= 1f + 1e-3f && t > slip + CheatSeconds)
        {
            // the knock: the open padlock hits the staple and stops dead; the staple's link takes the chain from here
            jammed = true;
            cutAtJam = cut;
            jamSim = t;
            sigma = 1f;
            w[1] = 0f;
            x[1] = prev[1] = K.StapleApex;
            lockPrev = Vector3.Lerp(lockPrev, lockP, .7f);
            for (var i = 0; i < 4 && i < n; i++) prev[i] = Vector3.Lerp(prev[i], x[i], .7f);
            Cue(PadlockKnock, K.StapleApex);
            Cue(ChainSway, K.StapleApex);
        }
        if (!floorSeen)
            for (var i = 0; i < n; i++)
                if (x[i].y <= K.FloorLinkHeight + 1e-4f)
                {
                    floorSeen = true;
                    Cue(ChainFloor, x[i]);
                    break;
                }
        // links knocking the wall or the casing after the knock (a few, spaced)
        if (jammed && clinks < 6 && t - lastClink > .2f)
            for (var i = 2; i < n; i++)
            {
                if (contactD[i] <= 0f || contactMu[i] != WallMu) continue;
                var v = (before[i] - prev[i]).magnitude / Step;
                if (v < .3f) continue;
                lastClink = t;
                clinks++;
                Cue(ChainClink, x[i]);
                break;
            }
    }

    // The chain starts to run off the bar 20 ms after the slip (the landed cut's first ChainRun, 0.95).
    void RunCue(float t)
    {
        if (run1Seen || t < slip + .02f) return;
        run1Seen = true;
        Cue(ChainRun, slipX[wrapStartNode]);
    }

    // Asleep once nothing moves faster than SleepSpeed for SleepSeconds after the knock (or at the latest LongestFall after
    // the slip): the simulation stops, its pose is kept for the close onto the baked rest.
    void Rest(float t, float cut)
    {
        if (sleepClock >= 0f || (!jammed && t < slip + LongestFall)) return;
        var limit = SleepSpeed * Step;
        var still = (lockP - lockStart).sqrMagnitude < limit * limit;
        for (var i = 0; i < n && still; i++) still = (x[i] - start[i]).sqrMagnitude < limit * limit;
        stillFor = still ? stillFor + Step : 0f;
        if (stillFor < SleepSeconds && t < slip + LongestFall) return;
        sleepClock = t;
        sleepCut = cut;
        System.Array.Copy(x, sleepX, n);
        lockSleep = lockP;
    }

    void Cue(string name, Vector3 at)
    {
        if (cueCount >= cueName.Length) return;
        cueName[cueCount] = name;
        cueAt[cueCount] = at;
        cueCount++;
    }

    /// <summary>The sync points raised since the last ClearCues, oldest first (shut frame); false when there is none.</summary>
    public bool TakeCue(int index, out string name, out Vector3 at)
    {
        name = null;
        at = Vector3.zero;
        if (index >= cueCount) return false;
        name = cueName[index];
        at = cueAt[index];
        return true;
    }

    public int CueCount => cueCount;
    public void ClearCues() => cueCount = 0;

    // ---------------------------------------------------------------- the picture

    /// <summary>
    /// The links' centres now (shut frame, K.Links of them, heel end link first: what K.PlaceNodes lays the links on), the
    /// padlock's pose (top-face centre, rotation), the staple link's pin weight, the round-the-bar weight and the share of
    /// the links' rest twist. Once asleep the pose closes onto UNLOCKED (K.UnlockedNodes, K.UnlockedPadlockAt / Rotation)
    /// and is exactly it when Done.
    /// </summary>
    public void Sample(Vector3[] nodes, out Vector3 top, out Quaternion rotation, out float staplePin, out float wrap, out float twist)
    {
        // the padlock: turned so that its mass hangs along the rod (keeping its own turn about the rod), its turn going from the
        // shut lock's to the open lock's hanging one as the haul draws it to the staple
        var rodNow = lockP - x[0];
        var dir = rodNow.sqrMagnitude > 1e-10f ? rodNow.normalized : Vector3.down;
        var haul = jammed ? 1f : Mathf.Clamp01((sigmaStart - sigma) / Mathf.Max(.5f, sigmaStart - 1f));
        rotationRef = Quaternion.Slerp(rotationSlip, K.OpenHangRotation, haul * haul);
        rotation = Quaternion.FromToRotation(rotationRef * dirLocal, dir) * rotationRef;
        top = x[0] - rotation * heelLocal;
        System.Array.Copy(x, nodes, n);
        if (sleepClock >= 0f && closeOntoRest)
        {
            var rest = K.UnlockedNodes;
            if (done)
            {
                System.Array.Copy(rest, nodes, n);
                top = K.UnlockedPadlockAt;
                rotation = K.UnlockedPadlockRotation;
            }
            else
            {
                var b = Mathf.Clamp01((simClock - sleepClock) / CloseSeconds);
                b = b * b * (3f - 2f * b);
                for (var i = 0; i < n; i++) nodes[i] = Vector3.LerpUnclamped(sleepX[i], rest[i], b);
                top = Vector3.LerpUnclamped(top, K.UnlockedPadlockAt, b);
                rotation = Quaternion.Slerp(rotation, K.UnlockedPadlockRotation, b);
            }
        }
        // the staple's link: held on the staple while the chain does not run through it, free while it runs, pinned again from
        // the knock
        staplePin = jammed ? Mathf.Clamp01((simClock - jamSim) / .15f) : 1f - Mathf.SmoothStep(0f, 1f, Mathf.Abs(sigma - sigmaStart) / .3f);
        if (done) staplePin = 1f;
        wrap = 1f - Mathf.Clamp01((simClock - slip) / CheatSeconds);
        twist = done ? 1f : Mathf.SmoothStep(0f, 1f, (simClock - TwistFrom) / TwistSeconds);
    }

    // ---------------------------------------------------------------- path helpers (no allocation)

    static float Length(Vector3[] path)
    {
        var length = 0f;
        for (var i = 1; i < path.Length; i++) length += (path[i] - path[i - 1]).magnitude;
        return length;
    }

    static Vector3 PointAt(Vector3[] path, float d)
    {
        var acc = 0f;
        for (var i = 0; i < path.Length - 1; i++)
        {
            var len = (path[i + 1] - path[i]).magnitude;
            if (acc + len >= d || i == path.Length - 2) return Vector3.Lerp(path[i], path[i + 1], len > 1e-7f ? Mathf.Clamp01((d - acc) / len) : 0f);
            acc += len;
        }
        return path[path.Length - 1];
    }

    /// <summary>Tools: a link's centre now (shut frame), the padlock's centre of mass, the chain's point at the staple (node units).</summary>
    public Vector3 Link(int i) => x[Mathf.Clamp(i, 0, n - 1)];
    public Vector3 PadlockCentre => lockP;
    public float StapleSigma => sigma;
}
