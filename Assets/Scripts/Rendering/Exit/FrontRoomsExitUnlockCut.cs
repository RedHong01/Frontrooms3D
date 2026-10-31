using System;
using FrontRooms.Map;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using K = FrontRoomsExitChainKit;
using KP = FrontRoomsShotTimings.KeyPickup;
using U = FrontRoomsShotTimings.Unlock;

/// <summary>
/// The EXIT's unlock micro cut scene (Red 2026-10-31: "...钥匙是怎么打开锁的这个micro cut scene"; research/unlock/
/// 10_research.md §6-§8, 20_design.md §3-§6). Owner: the game visual line. The camera is the map's unlock shot
/// (FrontRoomsDoorShot, untouched); this plays the lock, the chain and the key on that shot's clock:
/// <list type="table">
/// <item><term>0.30-0.55</term><description>the key rises from the lower right of the view to 3 cm under the keyway (bow edge-on).</description></item>
/// <item><term>0.55-0.68</term><description>insert 25 mm in five pin ticks (0.57-0.65), seat at 0.68; the hanging body is pushed up 1.5 mm.</description></item>
/// <item><term>0.68-0.86</term><description>the turn, 90 degrees, the first 10 stiff; the body twists 4 degrees with the torque and back; the plug turns with the key.</description></item>
/// <item><term>0.86</term><description>the release = the map's commit (BoltRetract, ExitLockChanged(false)): the body drops 11 mm off the shackle in 2 frames, a 1.5 mm rebound.</description></item>
/// <item><term>0.87-0.925</term><description>the shackle swivels 110 degrees about its heel (toe out toward the camera); the toe-side end link rides to the toe.</description></item>
/// <item><term>0.88-1.00</term><description>the key turns back and comes out straight down, then leaves to the lower right by 1.20.</description></item>
/// <item><term>0.93</term><description>the toe-side end link slips off; the free end falls and the turn opens off the bar by 1.00 (before the leaf leaves the frame).</description></item>
/// <item><term>1.00-1.22</term><description>the free chain hangs from the staple and hauls the chain through it until the open padlock jams against the staple (1.22, knock).</description></item>
/// <item><term>1.22-4.2</term><description>the long side swings along the wall from the staple (period 1.4 s, dying out), its tail landing on the floor (about 1.38); then UNLOCKED.</description></item>
/// </list>
/// A cancel before the release puts the key back out in 0.15 s and the lock back to rest, with no pop. After the release
/// the cut runs to its end on its own clock whatever the shot does. All motion is procedural and the same on desktop,
/// iPhone and iPad (three-platform parity). At Camera motion Off the map's shot plays in place: the cut adds a hard
/// cross-cut to a still close-up of the lock (0.55-1.00) and back, no camera move (CrossCutAtMotionOff).
///
/// The sound line's sync points are raised as Cue(name, where) on this clock (names only; 声音设计 owns the events).
/// No allocation per frame (the chain's mesh is rewritten in place).
/// </summary>
public sealed class FrontRoomsExitUnlockCut
{
    // ---------------------------------------------------------------- beats (s on the unlock shot's clock)

    public const float KeyApproach = U.KeyApproach, KeyAtKeyway = U.KeyAtKeyway, InsertStart = U.InsertStart, InsertEnd = U.InsertEnd;
    public const float TurnStart = U.TurnStart, Release = U.Commit;
    /// <summary>The body's drop off the shackle: 2 frames at 60 fps (free fall plus the spring over 11 mm: 30-45 ms).</summary>
    public const float DropSeconds = .033f;
    public const float SwivelStart = .87f, SwivelEnd = .925f, Slip = .93f;
    public const float PlugBackStart = .88f, PlugBackEnd = .92f, KeyOutEnd = 1.0f, KeyExitSeconds = .20f, KeyOutMetres = .06f;
    public const float Loosen = .965f, OffBar = 1.0f, Haul = 1.10f, Jam = 1.22f, FloorTouch = 1.38f;
    public const float SwayDeg = 12f, SwayPeriod = 1.4f, SwayDecay = .8f, SwaySeconds = 3.0f;
    public const float LiftMetres = .0015f, TickMetres = .0003f, TwistDeg = 4f, ReboundMetres = .0015f;
    public static readonly float[] PinTicks = { .57f, .59f, .61f, .63f, .65f };
    public const float CancelSeconds = U.CancelKeyOut;
    /// <summary>The cut's end on its clock: the sway has died out (UNLOCKED takes over).</summary>
    public const float End = Jam + SwaySeconds;

    // ---------------------------------------------------------------- the reduced-motion cross-cut

    /// <summary>At Camera motion Off: a hard cut to a still close-up of the lock over CrossCutIn..CrossCutOut, then back.</summary>
    public static bool CrossCutAtMotionOff = true;
    public const float CrossCutIn = .55f, CrossCutOut = 1.0f, CrossCutFov = 32f;
    public const float CrossCutDistance = .28f, CrossCutBelow = .035f, CrossCutSide = .07f;
    /// <summary>The cross-cut's render scale on phones and tablets (1 on desktop).</summary>
    public const float CrossCutMobileScale = .75f;

    // ---------------------------------------------------------------- sound sync points (names only)

    public static class Cues
    {
        public const string PinTick = "Unlock.PinTick", KeySeat = "Unlock.KeySeat", KeyTurn = "Unlock.KeyTurn", ShackleRelease = "Unlock.ShackleRelease";
        public const string KeyOut = "Unlock.KeyOut", ShackleSwivel = "Exit.ShackleSwivel", ChainSlip = "Exit.ChainSlip", ChainRun = "Exit.ChainRun";
        public const string PadlockKnock = "Exit.PadlockKnock", ChainFloor = "Exit.ChainFloor", ChainSway = "Exit.ChainSway";
    }

    /// <summary>A sync point of the cut landed this frame: its name (Cues) and where (world).</summary>
    public static event Action<string, Vector3> Cue;

    static readonly float[] cueTimes = { .57f, .59f, .61f, .63f, .65f, .68f, .68f, Release, .88f, PlugBackEnd, Slip, .95f, Haul, Jam, Jam, FloorTouch };
    static readonly string[] cueNames =
    {
        Cues.PinTick, Cues.PinTick, Cues.PinTick, Cues.PinTick, Cues.PinTick, Cues.KeySeat, Cues.KeyTurn, Cues.ShackleRelease,
        Cues.ShackleSwivel, Cues.KeyOut, Cues.ChainSlip, Cues.ChainRun, Cues.ChainRun, Cues.PadlockKnock, Cues.ChainSway, Cues.ChainFloor,
    };

    // ---------------------------------------------------------------- the chain's keyframes (shut frame, computed once)

    static Vector3[] k0, k0b, k1, k2, k3, path;
    static Vector3[] centres, tangents, widths;
    static readonly K.Pin[] pins = new K.Pin[3];
    static readonly float[] pinWeights = new float[3];
    static K.ChainMesher mesher;
    static float wrapStart, wrapEnd, stapleAt;

    static void Keyframes()
    {
        if (k3 != null) return;
        var n = K.PathPoints;
        k0 = (Vector3[])K.LockedPath.Clone();
        k3 = (Vector3[])K.UnlockedPath.Clone();
        wrapStart = K.WrapStartFraction;
        wrapEnd = K.WrapEndFraction;
        stapleAt = K.StapleFraction;
        var length = K.Length;
        var bar = K.BarLatchEnd;
        k0b = new Vector3[n];
        k1 = new Vector3[n];
        k2 = new Vector3[n];
        var drape = k0[Mathf.Clamp(Mathf.RoundToInt(wrapStart * (n - 1)), 0, n - 1)];
        var hang0 = K.StapleApex + new Vector3(.006f, -.008f, .03f);
        var swing = new Vector3(.02f, -Mathf.Cos(25f * Mathf.Deg2Rad), -Mathf.Sin(25f * Mathf.Deg2Rad)).normalized;
        for (var i = 0; i < n; i++)
        {
            var s = i / (float)(n - 1);
            var p = k0[i];
            // 0.965: the turn loosened (the falling end pulls it open: 55 mm off the bar's axis), the return run dropping
            var q = p;
            if (s >= wrapStart - .02f && s <= wrapEnd + .02f)
            {
                var axis = new Vector3(bar.x, bar.y, p.z);
                var d = p - axis;
                q = axis + (d.sqrMagnitude > 1e-10f ? d.normalized : Vector3.up) * .055f + new Vector3(0f, -.005f, 0f);
            }
            else if (s > wrapEnd) q = p + new Vector3(.01f, -.025f, 0f);
            k0b[i] = q;
            // 1.00: the turn is off; the free part hangs straight down in front of the bar from where the turn began
            if (s < wrapStart) k1[i] = p;
            else
            {
                var l = (s - wrapStart) * length;
                k1[i] = drape + new Vector3(.022f, -l, .006f);
            }
            // 1.10: the bar has gone with the leaf; everything past the staple hangs from it, trailing toward the door
            if (s <= stapleAt) k2[i] = p;
            else
            {
                var l = (s - stapleAt) * length;
                k2[i] = l < .03f ? Vector3.Lerp(K.StapleApex, hang0, l / .03f) : hang0 + swing * (l - .03f);
                if (k2[i].y < K.FloorLinkHeight) k2[i].y = K.FloorLinkHeight;
            }
        }
        path = new Vector3[n];
        var links = K.Links;
        centres = new Vector3[links];
        tangents = new Vector3[links];
        widths = new Vector3[links];
        mesher = new K.ChainMesher(links, 0, "A13 cut chain", true);
    }

    // ---------------------------------------------------------------- state

    readonly FrontRoomsMapWorld map;
    readonly FrontRoomsMapWorld.Door door;
    readonly Transform frame;
    readonly FrontRoomsExitPadlock padlock;
    readonly GameObject chainObject;
    readonly float startClock;
    FrontRoomsKeyAssembly key;
    float t, cancelClock = -1f;
    bool ownClock, committed, keySwayed, keyFrozen, keyExiting, chainWritten, slipSeen, hangReady;
    int nextCue;
    // the padlock's pose (shut frame) this frame, and what the cancel eases back from
    Vector3 top;
    Quaternion rotation;
    float pop, swivel, plugDeg, cancelLift, cancelTwist;
    // the body at the key's withdraw (the hand pulls straight out of where the plug was), the key's exit
    Vector3 frozenKeyway, frozenUp, frozenRight, keyExitFrom, cancelKeyFrom;
    // after the slip: the heel-side contact (shut frame) and the rotations the padlock passes through
    Vector3 contactSlip, contactJam;
    Quaternion rotationSlip, rotationHang, rotationJam;
    // the reduced-motion cross-cut
    Camera crossCamera;
    RenderTexture crossTexture;
    GameObject crossCanvas;

    /// <summary>The cut's clock (s on the unlock shot's clock, then its own).</summary>
    public float Clock => t;
    /// <summary>The release has happened (the map's commit): the cut runs to its end.</summary>
    public bool Committed => committed;
    /// <summary>A cancel before the release is easing the lock and key back.</summary>
    public bool Cancelling => cancelClock >= 0f;
    /// <summary>The live chain (tools).</summary>
    public GameObject ChainObject => chainObject;
    /// <summary>The cut's key while shown (tools), else null.</summary>
    public FrontRoomsKeyAssembly Key => key;
    /// <summary>The cross-cut is on screen (tools), and the picture it shows (tools: captures read it).</summary>
    public bool CrossCutShown => crossCanvas != null;
    public RenderTexture CrossCutTexture => crossTexture;

    /// <summary>
    /// Start on <paramref name="door"/> (the dressed EXIT) at the shot's clock <paramref name="clock"/>: the UNLOCKED group's
    /// padlock <paramref name="padlock"/> becomes the cut's (it starts at the LOCKED pose), the live chain is made under
    /// <paramref name="frame"/> (the frame dress: the shut frame, mirrored as the door), the key is dressed.
    /// </summary>
    public FrontRoomsExitUnlockCut(FrontRoomsMapWorld map, FrontRoomsMapWorld.Door door, Transform frame, FrontRoomsExitPadlock padlock, float clock)
    {
        Keyframes();
        this.map = map;
        this.door = door;
        this.frame = frame;
        this.padlock = padlock;
        t = startClock = Mathf.Max(0f, clock);
        // cues from before the start are not raised (a cut picked up late)
        while (nextCue < cueTimes.Length && cueTimes[nextCue] < startClock - .05f) nextCue++;
        chainObject = new GameObject(FrontRoomsExitChain.CutChainName);
        chainObject.transform.SetParent(frame, false);
        chainObject.AddComponent<MeshFilter>().sharedMesh = mesher.mesh;
        var r = chainObject.AddComponent<MeshRenderer>();
        r.sharedMaterial = K.Chain;
        r.shadowCastingMode = ShadowCastingMode.On;
        r.lightProbeUsage = LightProbeUsage.Off;
        if (padlock != null) padlock.ForceLod0(true);
        DressKey();
        Pose(true);
    }

    void DressKey()
    {
        var cam = FrontRoomsHeldItem.ViewCamera;
        var from = cam != null ? cam.transform.position : frame.position;
        var zone = map != null ? map.KeyFor(door, from) : null;
        key = FrontRoomsInteractableKit.DressShotKey(null, FrontRoomsKeyIdentity.Stub(zone ?? new GridCoord(0, 0)));
        if (key != null) key.gameObject.SetActive(false);
    }

    // ---------------------------------------------------------------- the clock

    /// <summary>One frame (the dresser's LateUpdate, after the game ticked the shot). False when the cut is over.</summary>
    public bool Step(float dt)
    {
        if (frame == null) { Kill(); return false; }
        var shot = map != null ? map.KeyTurnOwner as FrontRoomsDoorShot : null;
        var mine = shot != null && shot.Door != null && shot.Door.edge == door.edge;
        if (cancelClock >= 0f)
        {
            cancelClock += dt;
            Pose(false);
            if (cancelClock >= CancelSeconds) { Kill(); return false; }
            return true;
        }
        if (!ownClock && mine && shot.Live)
        {
            t = Mathf.Max(t, shot.Clock);
            if (shot.Committed) committed = true;
        }
        else if (!ownClock)
        {
            // the shot let go: ended, cancelled or aborted. After the release the cut runs on; before it, it eases back.
            if ((mine && shot.Committed) || (map != null && !map.ExitLocked && t >= Release - .05f)) committed = true;
            if (committed || t >= Release) ownClock = true;
            else
            {
                BeginCancel();
                return true;
            }
        }
        if (ownClock) t += dt;
        if (t >= Release && !committed && map != null && !map.ExitLocked) committed = true;
        Pose(false);
        RaiseCues();
        if (t >= End) { Kill(); return false; }
        return true;
    }

    void BeginCancel()
    {
        cancelClock = 0f;
        cancelLift = Lift(t);
        cancelTwist = Twist(t);
        cancelKeyFrom = key != null ? key.Root.position : Vector3.zero;
        CrossCut(false);
    }

    void RaiseCues()
    {
        while (nextCue < cueTimes.Length && t >= cueTimes[nextCue])
        {
            var at = cueTimes[nextCue] >= Haul ? frame.TransformPoint(K.StapleApex) : frame.TransformPoint(top);
            if (cueNames[nextCue] == Cues.ChainFloor) at = frame.TransformPoint(k3[k3.Length - 1]);
            Cue?.Invoke(cueNames[nextCue], at);
            nextCue++;
        }
    }

    /// <summary>The cut is over or cut short: the key and the cross-cut go, the padlock's levels are given back, the live chain is destroyed.</summary>
    public void Kill()
    {
        CrossCut(false);
        if (padlock != null) padlock.ForceLod0(false);
        if (key != null)
        {
            key.Release();
            Destroy(key.Root.gameObject);
            key = null;
        }
        if (chainObject != null) Destroy(chainObject);
    }

    static void Destroy(GameObject go)
    {
        if (go == null) return;
        if (Application.isPlaying) UnityEngine.Object.Destroy(go);
        else UnityEngine.Object.DestroyImmediate(go);
    }

    // ---------------------------------------------------------------- the lock's motion (pure, on the clock)

    /// <summary>The body pushed up by the key (m): 1.5 mm through the insert with a 0.3 mm bump at each pin tick, settling to 55 % while the key turns.</summary>
    public static float Lift(float time)
    {
        if (time < InsertStart) return 0f;
        if (time < InsertEnd)
        {
            var lift = LiftMetres * Mathf.SmoothStep(0f, 1f, (time - InsertStart) / (InsertEnd - InsertStart));
            foreach (var tick in PinTicks) lift += TickMetres * Mathf.Max(0f, 1f - Mathf.Abs(time - tick) / .008f);
            return lift;
        }
        if (time < Release) return LiftMetres * (.55f + .45f * Mathf.Exp(-(time - InsertEnd) / .04f));
        return LiftMetres * .55f;
    }

    /// <summary>The body's twist with the key's torque (degrees about its up): out and back over the turn.</summary>
    public static float Twist(float time) => time > TurnStart && time < Release ? TwistDeg * Mathf.Sin(Mathf.PI * (time - TurnStart) / (Release - TurnStart)) : 0f;

    /// <summary>The share of the release's drop done (0..1): no easing, 2 frames.</summary>
    public static float DropShare(float time) => time <= Release ? 0f : Mathf.Min(1f, Sq((time - Release) / DropSeconds));

    /// <summary>The body's rebound after the drop (m, up): one 1.5 mm hop, still by about 0.95.</summary>
    public static float Rebound(float time)
    {
        var t1 = Release + DropSeconds;
        if (time <= t1) return 0f;
        return ReboundMetres * Mathf.Exp(-(time - t1) / .015f) * Mathf.Abs(Mathf.Sin(Mathf.PI * (time - t1) / .028f));
    }

    /// <summary>The shackle's swivel about its heel (degrees): a quick out with a little overshoot, held from SwivelEnd.</summary>
    public static float Swivel(float time)
    {
        if (time <= SwivelStart) return 0f;
        if (time >= SwivelEnd) return K.SwivelDeg;
        var u = (time - SwivelStart) / (SwivelEnd - SwivelStart);
        const float c1 = 1.2f, c3 = c1 + 1f;
        var e = 1f + c3 * Mathf.Pow(u - 1f, 3f) + c1 * Mathf.Pow(u - 1f, 2f);
        return K.SwivelDeg * e;
    }

    /// <summary>The key's (and the plug's) turn (degrees): slow through the first 10 (the stiff start), easing out to 90 at the release; back to 0 by PlugBackEnd.</summary>
    public static float TurnDegrees(float time)
    {
        if (time <= TurnStart) return 0f;
        if (time < Release)
        {
            var s = (time - TurnStart) / (Release - TurnStart);
            if (s < U.TurnResistShare) return U.TurnResistDeg * Sq(s / U.TurnResistShare);
            var e = (s - U.TurnResistShare) / (1f - U.TurnResistShare);
            return U.TurnResistDeg + (U.TurnDeg - U.TurnResistDeg) * (1f - Sq(1f - e));
        }
        if (time < PlugBackStart) return U.TurnDeg;
        return U.TurnDeg * (1f - Mathf.SmoothStep(0f, 1f, (time - PlugBackStart) / (PlugBackEnd - PlugBackStart)));
    }

    /// <summary>How far the key's shoulder is out of the plug face (m), from KeyAtKeyway: the standoff closed, the 25 mm insert in five ticks and the seat; 0 while turned; out 60 mm by KeyOutEnd.</summary>
    public static float KeyOut(float time)
    {
        var depth = U.InsertDepth;
        var standoff = U.KeyStandoff;
        if (time <= KeyAtKeyway) return standoff + depth;
        if (time < InsertEnd)
        {
            var closed = Mathf.SmoothStep(0f, 1f, (time - KeyAtKeyway) / .015f);
            var steps = 0f;
            foreach (var tick in PinTicks) steps += Mathf.SmoothStep(0f, 1f, (time - (tick - .012f)) / .012f);
            steps += Mathf.SmoothStep(0f, 1f, (time - (InsertEnd - .012f)) / .012f);
            return standoff * (1f - closed) + depth * (1f - steps / (PinTicks.Length + 1f));
        }
        if (time < PlugBackEnd) return 0f;
        return KeyOutMetres * Mathf.SmoothStep(0f, 1f, (time - PlugBackEnd) / (KeyOutEnd - PlugBackEnd));
    }

    /// <summary>The live chain's sway about the staple (degrees, + = its foot toward the door), from Jam.</summary>
    public static float Sway(float time)
    {
        if (time <= Jam) return 0f;
        var e = time - Jam;
        return -SwayDeg * Mathf.Exp(-e / SwayDecay) * Mathf.Sin(2f * Mathf.PI * e / SwayPeriod);
    }

    static float Sq(float x) => x * x;
    static float Smooth(float x) => Mathf.SmoothStep(0f, 1f, x);

    // ---------------------------------------------------------------- posing

    void Pose(bool first)
    {
        var time = t;
        var rLock = K.LockedPadlockRotation;
        var cancelU = cancelClock >= 0f ? Smooth(cancelClock / CancelSeconds) : 0f;
        if (cancelClock >= 0f)
        {
            // back to rest: the lift and twist ease out, no pop
            var lift = Mathf.Lerp(cancelLift, 0f, cancelU);
            rotation = rLock * Quaternion.Euler(0f, Mathf.Lerp(cancelTwist, 0f, cancelU), 0f);
            top = K.LockedPadlockAt + rLock * new Vector3(0f, lift, 0f);
            pop = 0f;
            swivel = 0f;
            plugDeg = Mathf.Lerp(TurnDegrees(t), 0f, cancelU);
        }
        else if (time < Slip)
        {
            // the shackle stays where the links hold it; the body drops off it by the shackle's travel (and hops back once)
            var share = DropShare(time);
            var rebound = Rebound(time);
            rotation = rLock * Quaternion.Euler(0f, Twist(time), 0f);
            top = K.LockedPadlockAt + rLock * new Vector3(0f, Lift(time) - K.PopTravel * share + rebound, 0f);
            pop = K.PopTravel * share - rebound;
            swivel = Swivel(time);
            plugDeg = TurnDegrees(time);
            // what the slip starts from
            var open = K.OpenShackleLocal;
            contactSlip = top + rotation * K.BowPoint(open, K.HeelPinDeg);
            rotationSlip = rotation;
            slipSeen = true;
        }
        else
        {
            if (!hangReady) PrepareHang();
            var open = K.OpenShackleLocal;
            var contactLocal = K.BowPoint(open, K.HeelPinDeg);
            Vector3 c;
            if (time < Haul)
            {
                // hanging from the heel-side end link alone: it swings to hang under it, past and back
                var u = Mathf.Clamp01((time - Slip) / (Haul - Slip));
                var swing = 1f - Mathf.Exp(-u * 5f) * Mathf.Cos(u * 9f);
                rotation = Quaternion.SlerpUnclamped(rotationSlip, rotationHang, swing);
                c = contactSlip;
            }
            else if (time < Jam)
            {
                // hauled up the lower run to the staple
                var u = Sq(Mathf.Clamp01((time - Haul) / (Jam - Haul)));
                var mid = (contactSlip + contactJam) * .5f + new Vector3(.01f, .03f, 0f);
                c = Vector3.Lerp(Vector3.Lerp(contactSlip, mid, u), Vector3.Lerp(mid, contactJam, u), u);
                rotation = Quaternion.Slerp(rotationHang, rotationJam, u);
            }
            else
            {
                // jammed against the staple: one knock, a small wobble dying out
                var e = time - Jam;
                var wobble = 6f * Mathf.Exp(-e / .07f) * Mathf.Sin(2f * Mathf.PI * e / .14f);
                rotation = rotationJam * Quaternion.AngleAxis(wobble, Vector3.forward);
                c = contactJam;
            }
            top = c - rotation * contactLocal;
            pop = K.PopTravel;
            swivel = K.SwivelDeg;
            plugDeg = 0f;
        }
        if (padlock != null) padlock.Pose(top, rotation, pop, swivel, plugDeg);
        PoseChain(first);
        PoseKey();
        UpdateCrossCut();
    }

    void PrepareHang()
    {
        var open = K.OpenShackleLocal;
        if (!slipSeen)
        {
            // the cut was picked up after the slip: start from the shut padlock's pose
            rotationSlip = K.LockedPadlockRotation;
            contactSlip = K.LockedPadlockAt + rotationSlip * K.BowPoint(open, K.HeelPinDeg);
        }
        rotationHang = K.Hang(rotationSlip, open, K.HeelPinDeg);
        rotationJam = K.UnlockedPadlockRotation;
        contactJam = K.UnlockedPadlockAt + rotationJam * K.BowPoint(open, K.HeelPinDeg);
        hangReady = true;
    }

    // the padlock's shackle frame in the shut frame
    Matrix4x4 ShackleFrame() => Matrix4x4.TRS(top, rotation, Vector3.one) * K.ShackleLocal(pop, swivel);

    void PoseChain(bool first)
    {
        var time = cancelClock >= 0f ? Mathf.Min(t, Release) : t;
        // before the insert nothing on the chain moves: write the LOCKED arrangement once
        if (time < InsertStart && chainWritten && !first) return;
        var n = path.Length;
        if (time < Slip) Array.Copy(k0, path, n);
        else if (time < Loosen)
        {
            var u = Smooth((time - Slip) / (Loosen - Slip));
            for (var i = 0; i < n; i++) path[i] = Vector3.LerpUnclamped(k0[i], k0b[i], u);
        }
        else if (time < OffBar)
        {
            var u = Mathf.Clamp01((time - Loosen) / (OffBar - Loosen));
            var ux = Mathf.Pow(u, 1.5f);
            var uy = Mathf.Pow(u, .7f);
            for (var i = 0; i < n; i++)
            {
                var a = k0b[i];
                var b = k1[i];
                path[i] = new Vector3(Mathf.Lerp(a.x, b.x, ux), Mathf.Lerp(a.y, b.y, uy), Mathf.Lerp(a.z, b.z, ux));
            }
        }
        else if (time < Haul)
        {
            var u = Smooth((time - OffBar) / (Haul - OffBar));
            for (var i = 0; i < n; i++) path[i] = Vector3.LerpUnclamped(k1[i], k2[i], u);
        }
        else if (time < Jam)
        {
            var u = Sq(Mathf.Clamp01((time - Haul) / (Jam - Haul)));
            for (var i = 0; i < n; i++) path[i] = Vector3.LerpUnclamped(k2[i], k3[i], u);
        }
        else
        {
            // the long side swings along the wall about the staple; the part on the floor stays
            var a = Sway(time) * Mathf.Deg2Rad;
            var cos = Mathf.Cos(a);
            var sin = Mathf.Sin(a);
            var pivot = K.StapleApex;
            // UNLOCKED: the padlock's end link and the staple's link (the first pitch) are jammed and do not swing
            var from = Mathf.CeilToInt(K.ChainPitch / K.Length * (n - 1));
            for (var i = 0; i < n; i++)
            {
                var p = k3[i];
                if (i <= from) { path[i] = p; continue; }
                var w = Smooth((p.y - .02f) / .25f);
                var dy = p.y - pivot.y;
                var dz = p.z - pivot.z;
                var q = new Vector3(p.x, pivot.y + dy * cos - dz * sin, pivot.z + dy * sin + dz * cos);
                path[i] = Vector3.Lerp(p, q, w);
            }
        }
        // the heel-side end link rides the padlock's bow; the toe-side one until the slip; the staple's link while the chain is on it
        var shackle = ShackleFrame();
        var heel = K.BowPin(shackle, K.HeelPinDeg);
        Follow(heel.centre, true);
        var count = 0;
        pins[count] = heel;
        pinWeights[count++] = 1f;
        pins[count] = K.StaplePin;
        pinWeights[count++] = time < OffBar ? 1f : time < 1.25f ? 0f : Smooth((time - 1.25f) / .15f);
        if (time < Slip)
        {
            var slide = Smooth((time - SwivelStart) / (SwivelEnd - SwivelStart));
            var toe = K.BowPin(shackle, Mathf.Lerp(K.ToePinDeg, -85f, slide));
            Follow(toe.centre, false);
            pins[count] = toe;
            pinWeights[count++] = 1f;
        }
        var wrap = time < Slip ? 1f : 1f - Smooth((time - Slip) / (OffBar - Slip));
        K.Place(path, pins, pinWeights, count, wrap, centres, tangents, widths);
        mesher.Write(centres, tangents, widths);
        chainWritten = true;
    }

    // Bend the path's first (or last) 0.12 m so that its end sits on the link the padlock carries.
    void Follow(Vector3 to, bool start)
    {
        var n = path.Length;
        var end = start ? 0 : n - 1;
        var delta = to - path[end];
        if (delta.sqrMagnitude < 1e-12f) return;
        var reach = Mathf.Max(2, Mathf.RoundToInt(.12f / K.Length * (n - 1)));
        for (var k = 0; k <= reach && k < n; k++)
        {
            var w = Sq(1f - k / (float)reach);
            path[start ? k : n - 1 - k] += delta * w;
        }
    }

    // ---------------------------------------------------------------- the key

    void PoseKey()
    {
        if (key == null) return;
        var time = t;
        if (cancelClock < 0f && time >= KeyOutEnd + KeyExitSeconds)
        {
            key.Release();
            Destroy(key.Root.gameObject);
            key = null;
            return;
        }
        var show = cancelClock >= 0f || time >= KeyApproach;
        if (key.gameObject.activeSelf != show) key.gameObject.SetActive(show);
        if (!show) return;
        // the keyway in the world, from the body as it is now (or as it was when the key started out)
        Vector3 keyway, up, right;
        if (!keyFrozen || cancelClock >= 0f)
        {
            keyway = frame.TransformPoint(top + rotation * K.PlugLocal);
            up = frame.TransformDirection(rotation * Vector3.up).normalized;
            right = frame.TransformDirection(rotation * Vector3.right).normalized;
            if (time >= PlugBackEnd && cancelClock < 0f)
            {
                keyFrozen = true;
                frozenKeyway = keyway;
                frozenUp = up;
                frozenRight = right;
            }
        }
        else
        {
            keyway = frozenKeyway;
            up = frozenUp;
            right = frozenRight;
        }
        var turn = cancelClock >= 0f ? plugDeg : TurnDegrees(time);
        var keyRotation = Quaternion.LookRotation(up, right) * Quaternion.AngleAxis(turn, Vector3.forward);
        var cam = FrontRoomsHeldItem.ViewCamera;
        var outside = cam != null ? cam.ViewportToWorldPoint(new Vector3(U.KeyEnterViewportX, U.KeyEnterViewportY, U.KeyEnterDistance)) : keyway - up * .3f;
        Vector3 at;
        if (cancelClock >= 0f)
            at = Vector3.Lerp(cancelKeyFrom, keyway - up * KeyOutMetres, Smooth(cancelClock / CancelSeconds));
        else if (time < KeyAtKeyway)
            at = Vector3.Lerp(outside, keyway - up * (U.KeyStandoff + U.InsertDepth), FrontRoomsCameraRig.Ease(Mathf.Clamp01((time - KeyApproach) / (KeyAtKeyway - KeyApproach))));
        else if (time < KeyOutEnd)
            at = keyway - up * KeyOut(time);
        else
        {
            if (!keyExiting)
            {
                keyExiting = true;
                keyExitFrom = keyway - up * KeyOutMetres;
            }
            at = Vector3.Lerp(keyExitFrom, outside, FrontRoomsCameraRig.Ease(Mathf.Clamp01((time - KeyOutEnd) / KeyExitSeconds)));
        }
        key.PoseInLock(new Pose(at, keyRotation));
        if (!keySwayed && time >= TurnStart && cancelClock < 0f)
        {
            // the ring and tag swing with the turn (the map's numbers for its own key at half the angle)
            keySwayed = true;
            key.Sway(KP.TagSwingDeg * .5f, KP.TagSwingHz, KP.TagSwingDecay);
        }
    }

    // ---------------------------------------------------------------- the reduced-motion cross-cut

    void UpdateCrossCut()
    {
        var want = CrossCutAtMotionOff && FrontRoomsSettings.CameraMotionScale <= 0f && cancelClock < 0f && t >= CrossCutIn && t < CrossCutOut && Application.isPlaying;
        CrossCut(want);
        if (crossCamera == null) return;
        // a still close-up (it never moves while it is on screen): three-quarter on the shut lock's face from just under its keyway
        var shut = K.LockedPadlockRotation;
        var face = frame.TransformDirection(shut * Vector3.forward).normalized;
        var up = frame.TransformDirection(shut * Vector3.up).normalized;
        var side = frame.TransformDirection(shut * Vector3.right).normalized;
        var keyway = frame.TransformPoint(K.LockedPadlockAt + K.LockedPadlockRotation * K.PlugLocal);
        var centre = frame.TransformPoint(K.LockedPadlockAt + K.LockedPadlockRotation * K.BodyMassLocal);
        var eye = keyway + face * CrossCutDistance - up * CrossCutBelow + side * CrossCutSide;
        crossCamera.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(centre - eye, Vector3.up));
    }

    void CrossCut(bool on)
    {
        if (on == (crossCanvas != null)) return;
        if (!on)
        {
            if (crossCamera != null) Destroy(crossCamera.gameObject);
            if (crossCanvas != null) Destroy(crossCanvas);
            if (crossTexture != null)
            {
                crossTexture.Release();
                if (Application.isPlaying) UnityEngine.Object.Destroy(crossTexture);
                else UnityEngine.Object.DestroyImmediate(crossTexture);
            }
            crossCamera = null;
            crossCanvas = null;
            crossTexture = null;
            return;
        }
        var view = FrontRoomsHeldItem.ViewCamera;
        if (view == null) return;
        var scale = Application.isMobilePlatform ? CrossCutMobileScale : 1f;
        crossTexture = new RenderTexture(Mathf.Max(64, Mathf.RoundToInt(Screen.width * scale)), Mathf.Max(64, Mathf.RoundToInt(Screen.height * scale)), 24, RenderTextureFormat.Default)
        {
            name = "A13 cross-cut", hideFlags = HideFlags.DontSave,
        };
        crossTexture.Create();
        var camGo = new GameObject("A13 cross-cut camera") { hideFlags = HideFlags.DontSave };
        crossCamera = camGo.AddComponent<Camera>();
        crossCamera.CopyFrom(view);
        crossCamera.targetTexture = crossTexture;
        crossCamera.fieldOfView = CrossCutFov;
        crossCamera.nearClipPlane = .01f;
        crossCamera.farClipPlane = 6f;
        crossCamera.depth = view.depth - 1f;
        var data = crossCamera.GetUniversalAdditionalCameraData();
        var viewData = view.GetUniversalAdditionalCameraData();
        if (data != null && viewData != null)
        {
            data.renderPostProcessing = viewData.renderPostProcessing;
            data.antialiasing = viewData.antialiasing;
            data.renderShadows = viewData.renderShadows;
        }
        crossCanvas = new GameObject("A13 cross-cut") { hideFlags = HideFlags.DontSave };
        var canvas = crossCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -50;   // under every HUD canvas: captions stay on top
        var image = new GameObject("picture").AddComponent<RawImage>();
        image.transform.SetParent(crossCanvas.transform, false);
        image.texture = crossTexture;
        image.raycastTarget = false;
        var rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
