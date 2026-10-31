using System.Collections.Generic;
using FrontRooms.Map;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// A13 R1 (Red 2026-10-31, the lock's structure; research/unlock/10_research.md §5, 20_design.md §2): the geometry of the EXIT
/// door's after-hours chain lock, for FrontRoomsExitChain (the states) and FrontRoomsExitUnlockCut (the micro cut scene).
/// Owner: the game visual line.
///
/// The lock is built as a real one is, so that every part fits the next and gravity alone can finish the release:
/// <list type="bullet">
/// <item><description>a 3/8 in grade 30 proof-coil chain (wire 9.5 mm, link 54 x 34 mm), threaded through a 1/2 in
/// staple welded to a steel plate on N's wall face, 0.14 m past the latch side of the opening, 1.00 m up
/// (Kit_ChainStaple; it replaces A13's eye bolt);</description></item>
/// <item><description>its lower run goes from the staple across the jamb and the latch case to the padlock, its upper
/// run back over the latch case to the crossbar, one turn round the bar 0.10-0.155 m in from the latch case, and back
/// down to the padlock: the two END links ride the padlock's shackle (the heel-side and the toe-side end of the
/// bow), so the loop holds the bar to the wall;</description></item>
/// <item><description>a 2 in laminated padlock (Kit_Padlock_Body / _Shackle / _Plug; the HUD padlock family's 52 x 46 x
/// 26 mm) hanging on the two end links just in front of the latch case, keyway on its bottom, tipped 6 degrees
/// toward the corridor and turned 10 degrees toward the hinge (the dipped camera's side).</description></item>
/// </list>
/// UNLOCKED: the shackle open, the toe-side end slipped off, the chain run through the staple until the open padlock
/// jammed against it; the long side hangs from the staple down the wall, its tail on the floor.
///
/// Frames (metres), as before: LOCKED in the EXIT leaf rig's frame, UNLOCKED and the staple in the door's shut frame
/// on the EXIT frame object; both are the hinge's frame when the leaf is shut: origin the hinge-jamb edge on the wall's
/// centre line at the floor, +Y up, +Z along the opening (hinge 0, latch DoorWidth), +X the push face (N's side) for a
/// door swinging +1 (FrontRoomsExitChain mirrors the groups for -1).
///
/// The chain is not a kit: its links follow a path that moves in the cut. Every path is resampled to PathPoints points
/// at equal arc length; the links (one per ChainPitch) are placed on it by Place and meshed by ChainMesher, the same
/// code for the cached LOCKED / UNLOCKED meshes and for the cut's live mesh, so the first and last live frames are the
/// cached ones. Nothing here allocates per frame. The padlock and the staple are the Blender kit generator's
/// (Tools/Blender/frontrooms_kit/assets/exit_padlock_*.py, exit_chain_staple.py; numbers in exit_lock_common.py,
/// mirrored below); a plain stand-in mesh is made here when a kit is missing.
/// </summary>
public static class FrontRoomsExitChainKit
{
    // ---------------------------------------------------------------- the decided values

    /// <summary>The chain: dark oxidised proof coil, #34302B (A13's). The padlock body and the staple: laminated steel, #9A9C97.</summary>
    public const string ChainSurface = "Prop_ChainOxide", SteelSurface = "Prop_SteelLaminated";

    /// <summary>The kits (Blender kit generator, R1).</summary>
    public const string BodyKit = "Kit_Padlock_Body", ShackleKit = "Kit_Padlock_Shackle", PlugKit = "Kit_Padlock_Plug", StapleKit = "Kit_ChainStaple";

    /// <summary>3/8 in grade 30 proof coil: wire 9.5 mm, link 54 x 34 mm outside, pitch (inside length) 34 mm.</summary>
    public const float ChainWire = .0095f, ChainLength = .054f, ChainWidth = .034f, ChainPitch = .034f;

    /// <summary>The crossbar's bar line in the leaf rig (the map's latch case + the kit's bar_centre, 0.07 m out), its 32 mm bar.</summary>
    public const float BarOut = .07f, BarRadius = .016f;

    /// <summary>One turn round the bar, 0.10 to 0.155 m in from the latch case, centre-line radius 31 mm (links crossed 45 degrees: 2 mm clear of the bar).</summary>
    public const float WrapIn = .10f, WrapOut = .155f, WrapRadius = .031f;

    /// <summary>The staple plate: 0.14 m past the latch edge of the opening, 1.0 m up, on N's wall face; the staple's apex 37.65 mm out of the wall.</summary>
    public const float StapleAlong = .14f, StapleHeight = 1f, StapleApexOut = .03765f;

    /// <summary>The padlock (exit_lock_common.py): body 52 x 46 x 26 mm; shackle 9.6 mm, legs 34 mm apart, bow centre 12.8 mm over the top face.</summary>
    public const float BodyW = .052f, BodyH = .046f, BodyD = .026f;
    public const float ShackleHalf = .017f, ShackleWireR = .0048f, ShackleBowC = .0128f, HeelX = -.017f;
    /// <summary>The release: the shackle's travel (the body's drop) and the swivel about the heel (toe out toward the camera).</summary>
    public const float PopTravel = .011f, SwivelDeg = -110f;
    /// <summary>Where the end links ride the bow, degrees from its apex: + toward the heel, - toward the toe.</summary>
    public const float HeelPinDeg = 35f, ToePinDeg = -35f;

    /// <summary>The shut padlock's top-face centre (just in front of the latch case: the end links' wires clear its face by 6 mm), turned toward the hinge and tipped (bottom toward the corridor).</summary>
    public static readonly Vector3 LockedPadlockAt = new Vector3(.152f, .938f, .903f);
    public const float LockedPadlockYawDeg = 10f, LockedPadlockTiltDeg = -6f, UnlockedPadlockYawDeg = 10f;
    /// <summary>UNLOCKED: the heel-side end link one pitch from the staple's link, toward the door, sagging.</summary>
    public static readonly Vector3 UnlockedHeelOffset = new Vector3(.004f, -.017f, -.029f);

    /// <summary>A link lying on the floor crossed 45 degrees to it: its centre line's height.</summary>
    public const float FloorLinkHeight = .0175f;
    /// <summary>Points per resampled path.</summary>
    public const int PathPoints = 64;

    /// <summary>Level-of-detail screen heights (LODGroup, relative): LOD0 for close looks (the unlock shot), LOD1, LOD2 (never culled: the lock reads at 30 m).</summary>
    public static readonly float[] LodHeights = { .12f, .04f, 0f };

    public static float LatchZ => ModuleUnits.DoorWidth - ModuleUnits.DoorLeafGap * .5f - FrontRoomsMapWorld.ExitCrossbarLatchInset;
    public static float LeafFace => ModuleUnits.DoorLeafThickness * .5f;
    public static Vector3 BarLatchEnd => new Vector3(LeafFace + BarOut, FrontRoomsMapWorld.ExitCrossbarHeight, LatchZ);
    /// <summary>The staple plate's back centre on the wall face (Kit_ChainStaple's origin), and the staple's apex (the chain's link threads it).</summary>
    public static Vector3 StapleAnchor => new Vector3(ModuleUnits.WallHalf, StapleHeight, ModuleUnits.DoorWidth + StapleAlong);
    public static Vector3 StapleApex => StapleAnchor + new Vector3(StapleApexOut, 0f, 0f);
    /// <summary>The staple kit's rotation: its +Z out of the wall (+X).</summary>
    public static Quaternion StapleRotation => Quaternion.LookRotation(Vector3.right, Vector3.up);

    static Vector3 Facing(float yawTowardHingeDeg)
    {
        var a = yawTowardHingeDeg * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(a), 0f, -Mathf.Sin(a));
    }

    /// <summary>The shut padlock's rotation (its +Z the lock's face, +Y up, +X toward the toe = the hinge side).</summary>
    public static Quaternion LockedPadlockRotation => Quaternion.LookRotation(Facing(LockedPadlockYawDeg), Vector3.up) * Quaternion.Euler(LockedPadlockTiltDeg, 0f, 0f);

    /// <summary>The shackle's frame in the body's (Kit_Padlock_Shackle's origin = the heel axis on the top face): popped by <paramref name="pop"/>, swivelled about the heel.</summary>
    public static Matrix4x4 ShackleLocal(float pop, float swivelDeg) => Matrix4x4.TRS(new Vector3(HeelX, pop, 0f), Quaternion.Euler(0f, swivelDeg, 0f), Vector3.one);

    /// <summary>The plug face's centre on the bottom face, in the body's frame (Kit_Padlock_Plug's origin; the key goes in along +Y).</summary>
    public static Vector3 PlugLocal => new Vector3(0f, -BodyH, 0f);

    /// <summary>The padlock's centre of mass in the body's frame (it hangs under its suspension point).</summary>
    public static Vector3 BodyMassLocal => new Vector3(0f, -BodyH * .5f, 0f);

    public static Matrix4x4 LockedBody => Matrix4x4.TRS(LockedPadlockAt, LockedPadlockRotation, Vector3.one);

    /// <summary>
    /// The shut padlock's keyway in the leaf rig's frame: the plug face's centre; +Z into the lock (the body's up), +Y the
    /// lock's face. FrontRoomsExitChain.Keyway gives it to the map's LockPose (the contract, unlock_contract C1).
    /// </summary>
    public static Pose LockedKeyway => new Pose(LockedPadlockAt + LockedPadlockRotation * PlugLocal, LockedPadlockRotation * Quaternion.LookRotation(Vector3.up, Vector3.forward));

    // ---------------------------------------------------------------- the unlocked padlock (computed once)

    static bool posesReady;
    static Vector3 unlockedAt;
    static Quaternion unlockedRotation;

    /// <summary>UNLOCKED: the open padlock's top-face centre and rotation (shut frame), hanging under its heel-side end link by the staple.</summary>
    public static Vector3 UnlockedPadlockAt { get { Ready(); return unlockedAt; } }
    public static Quaternion UnlockedPadlockRotation { get { Ready(); return unlockedRotation; } }
    public static Matrix4x4 UnlockedBody => Matrix4x4.TRS(UnlockedPadlockAt, UnlockedPadlockRotation, Vector3.one);
    public static Matrix4x4 OpenShackleLocal => ShackleLocal(PopTravel, SwivelDeg);

    static void Ready()
    {
        if (posesReady) return;
        var open = OpenShackleLocal;
        unlockedRotation = Hang(Quaternion.LookRotation(Facing(UnlockedPadlockYawDeg), Vector3.up), open, HeelPinDeg);
        unlockedAt = StapleApex + UnlockedHeelOffset - unlockedRotation * BowPin(open, HeelPinDeg).centre;
        posesReady = true;
    }

    /// <summary>A padlock of rotation <paramref name="r0"/> turned so that its mass hangs under the bow at <paramref name="pinDeg"/> (the shackle at <paramref name="shackleLocal"/>).</summary>
    public static Quaternion Hang(Quaternion r0, Matrix4x4 shackleLocal, float pinDeg)
    {
        var d = r0 * (BodyMassLocal - BowPoint(shackleLocal, pinDeg));
        return d.sqrMagnitude < 1e-10f ? r0 : Quaternion.FromToRotation(d, Vector3.down) * r0;
    }

    // ---------------------------------------------------------------- pins

    /// <summary>A link placed exactly: its centre, its long axis, and its width direction (its plane holds both).</summary>
    public struct Pin
    {
        public Vector3 centre, along, width;
        public Pin(Vector3 centre, Vector3 along, Vector3 width)
        {
            this.centre = centre;
            this.along = along.normalized;
            this.width = width.normalized;
        }
    }

    /// <summary>An end link on the bow: its inner end round the shackle's wire (0.3 mm play), its long axis along the bow's radial.</summary>
    public const float HookReach = ChainLength * .5f - ChainWire - ShackleWireR - .0003f;

    static Vector3 Radial(float deg)
    {
        var a = deg * Mathf.Deg2Rad;
        return new Vector3(-Mathf.Sin(a), Mathf.Cos(a), 0f);
    }

    /// <summary>The bow's centre line at <paramref name="deg"/> from its apex (+ toward the heel), through <paramref name="shackle"/> (the shackle's frame to the wanted frame).</summary>
    public static Vector3 BowPoint(Matrix4x4 shackle, float deg) => shackle.MultiplyPoint3x4(new Vector3(ShackleHalf, ShackleBowC, 0f) + Radial(deg) * ShackleHalf);

    /// <summary>The end link riding the bow at <paramref name="deg"/>: its plane square to the shackle's wire (width along the shackle's +Z).</summary>
    public static Pin BowPin(Matrix4x4 shackle, float deg)
    {
        var n = Radial(deg);
        var c = new Vector3(ShackleHalf, ShackleBowC, 0f) + n * (ShackleHalf + HookReach);
        return new Pin(shackle.MultiplyPoint3x4(c), shackle.MultiplyVector(n), shackle.MultiplyVector(Vector3.forward));
    }

    /// <summary>The link threaded on the staple: the staple's apex wire (vertical) through the middle of its opening, its long axis along the wall.</summary>
    public static Pin StaplePin => new Pin(StapleApex, Vector3.forward, Vector3.right);

    public static Pin LockedHeelPin => BowPin(LockedBody * ShackleLocal(0f, 0f), HeelPinDeg);
    public static Pin LockedToePin => BowPin(LockedBody * ShackleLocal(0f, 0f), ToePinDeg);
    public static Pin UnlockedHeelPin => BowPin(UnlockedBody * OpenShackleLocal, HeelPinDeg);

    // ---------------------------------------------------------------- the paths

    static Vector3[] lockedPath, unlockedPath;
    static float lockedLength, wrapStartS, wrapEndS, stapleS;

    /// <summary>The chain's length (m), the same in every state: the LOCKED path's.</summary>
    public static float Length { get { Paths(); return lockedLength; } }
    /// <summary>Links in the chain (one per pitch, both ends included).</summary>
    public static int Links => Mathf.Max(2, Mathf.FloorToInt(Length / ChainPitch) + 1);
    /// <summary>Along the LOCKED path (fractions of Length): where the turn round the bar starts and ends, and the staple's link.</summary>
    public static float WrapStartFraction { get { Paths(); return wrapStartS / lockedLength; } }
    public static float WrapEndFraction { get { Paths(); return wrapEndS / lockedLength; } }
    public static float StapleFraction { get { Paths(); return stapleS / lockedLength; } }

    /// <summary>LOCKED (leaf rig frame), resampled: heel end link -> staple -> upper run -> one turn round the bar -> toe end link.</summary>
    public static Vector3[] LockedPath { get { Paths(); return lockedPath; } }
    /// <summary>UNLOCKED (shut frame), resampled to the same length: heel end link on the open padlock -> the staple's link -> down the wall -> the tail on the floor.</summary>
    public static Vector3[] UnlockedPath { get { Paths(); return unlockedPath; } }

    static void Paths()
    {
        if (lockedPath != null) return;
        var raw = LockedPathRaw(out var wrapStart, out var wrapEnd, out var staple);
        lockedLength = PathLength(raw);
        wrapStartS = LengthTo(raw, wrapStart);
        wrapEndS = LengthTo(raw, wrapEnd);
        stapleS = LengthTo(raw, staple);
        var locked = new Vector3[PathPoints];
        Resample(raw, locked);
        var unlocked = new Vector3[PathPoints];
        Resample(UnlockedPathRaw(lockedLength), unlocked);
        unlockedPath = unlocked;
        lockedPath = locked;
    }

    /// <summary>
    /// The LOCKED centre line (leaf rig frame) through its pinned links. Clearances (computed for the build stage,
    /// 20_design.md §2.4; to be proved on the combined test round as A13's were): every wire 1 mm or more off the bar
    /// (32 mm), the latch case (front X 0.12, door Z 0.8525-0.955), the jamb casing (X 0.105 to Z 1.075), the staple
    /// plate (front X 0.086), the leaf (X 0.025), and the runs 40 mm or more apart where they pass each other.
    /// </summary>
    public static List<Vector3> LockedPathRaw(out int wrapStart, out int wrapEnd, out int staple)
    {
        var bar = BarLatchEnd;
        var heel = LockedHeelPin;
        var toe = LockedToePin;
        var s = StapleApex;
        var p = new List<Vector3>
        {
            // the lower run: from the heel-side end link up over the latch case and the jamb to the staple
            heel.centre, heel.centre + heel.along * ChainPitch,
            new Vector3(.158f, .997f, .99f), new Vector3(.140f, .998f, 1.03f), new Vector3(.128f, .999f, 1.075f), new Vector3(.121f, 1f, 1.108f),
        };
        staple = p.Count;
        p.Add(s);
        // through the staple, a loop beyond it, and the upper run back over the latch case toward the bar
        p.Add(s + new Vector3(.0015f, -.003f, .032f));
        p.Add(new Vector3(.128f, 1.016f, 1.21f));
        p.Add(new Vector3(.137f, 1.050f, 1.215f));
        p.Add(new Vector3(.142f, 1.070f, 1.18f));
        p.Add(new Vector3(.144f, 1.072f, 1.12f));
        p.Add(new Vector3(.147f, 1.066f, 1.06f));
        p.Add(new Vector3(.152f, 1.060f, 1.00f));
        p.Add(new Vector3(.156f, 1.055f, .94f));
        p.Add(new Vector3(.150f, 1.049f, .88f));
        p.Add(new Vector3(.135f, 1.045f, .835f));
        p.Add(new Vector3(.114f, 1.040f, .805f));
        // one turn round the bar, from its front-top over the top and behind it, toward the hinge
        wrapStart = p.Count;
        const int turnSteps = 24;
        for (var i = 0; i <= turnSteps; i++)
        {
            var f = i / (float)turnSteps;
            var th = -.25f * Mathf.PI + f * 2f * Mathf.PI;
            p.Add(new Vector3(bar.x - WrapRadius * Mathf.Sin(th), bar.y + WrapRadius * Mathf.Cos(th), bar.z - Mathf.Lerp(WrapIn, WrapOut, f)));
        }
        wrapEnd = p.Count - 1;
        // out of the turn at the front, back toward the latch under the upper run, to the toe-side end link
        p.Add(new Vector3(.132f, 1.004f, .762f));
        p.Add(new Vector3(.150f, .990f, .815f));
        p.Add(toe.centre + toe.along * ChainPitch);
        p.Add(toe.centre);
        return p;
    }

    /// <summary>The UNLOCKED centre line (shut frame) for a chain of <paramref name="length"/>: heel end link, the staple's link, down the wall, the rest on the floor.</summary>
    public static List<Vector3> UnlockedPathRaw(float length)
    {
        var heel = UnlockedHeelPin;
        var s = StapleApex;
        var p = new List<Vector3>
        {
            heel.centre, s, s + new Vector3(.0025f, -.005f, .032f), new Vector3(.124f, .975f, 1.192f), new Vector3(.126f, .94f, 1.198f),
            new Vector3(.127f, .70f, 1.20f), new Vector3(.128f, .40f, 1.20f), new Vector3(.129f, .12f, 1.20f), new Vector3(.131f, .045f, 1.196f),
            new Vector3(.136f, FloorLinkHeight, 1.178f),
        };
        // the tail on the floor: a loose curl toward the door, kept off the wall (X >= 0.12)
        var c = new Vector3(.19f, FloorLinkHeight, 1.14f);
        var a = Mathf.Atan2(1.178f - c.z, .136f - c.x);
        var guard = 0;
        while (PathLength(p) < length && guard++ < 200)
        {
            a -= .35f;
            var r = .055f + .012f * guard / 6f;
            var q = c + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            q.x = Mathf.Max(q.x, .12f);
            p.Add(q);
        }
        // trim the last segment to the length exactly
        var over = PathLength(p) - length;
        if (over > 0f && p.Count > 2)
        {
            var last = p[p.Count - 1];
            var prev = p[p.Count - 2];
            var seg = (last - prev).magnitude;
            p[p.Count - 1] = Vector3.Lerp(prev, last, Mathf.Clamp01((seg - over) / Mathf.Max(seg, 1e-6f)));
        }
        return p;
    }

    public static float PathLength(IList<Vector3> path)
    {
        var length = 0f;
        for (var i = 1; i < path.Count; i++) length += (path[i] - path[i - 1]).magnitude;
        return length;
    }

    static float LengthTo(IList<Vector3> path, int index)
    {
        var length = 0f;
        for (var i = 1; i <= index && i < path.Count; i++) length += (path[i] - path[i - 1]).magnitude;
        return length;
    }

    /// <summary>Resample <paramref name="src"/> into <paramref name="dst"/> at equal arc length (no allocation).</summary>
    public static void Resample(IList<Vector3> src, Vector3[] dst)
    {
        var total = PathLength(src);
        var n = dst.Length;
        var seg = 0;
        var acc = 0f;
        for (var k = 0; k < n; k++)
        {
            var d = total * k / (n - 1);
            while (seg < src.Count - 2 && acc + (src[seg + 1] - src[seg]).magnitude < d)
            {
                acc += (src[seg + 1] - src[seg]).magnitude;
                seg++;
            }
            var len = (src[seg + 1] - src[seg]).magnitude;
            dst[k] = Vector3.Lerp(src[seg], src[seg + 1], len > 1e-7f ? Mathf.Clamp01((d - acc) / len) : 0f);
        }
    }

    // ---------------------------------------------------------------- link placement

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

    static float Length0(Vector3[] path)
    {
        var length = 0f;
        for (var i = 1; i < path.Length; i++) length += (path[i] - path[i - 1]).magnitude;
        return length;
    }

    /// <summary>
    /// Each link's centre, long axis and width direction along <paramref name="path"/> (one per centres slot, the first
    /// and last at the ends), in alternating planes carried from link to link (the first one's from the wall's normal,
    /// so a hanging chain never flips), round the crossbar crossed 45 degrees to it (weighted by <paramref name="wrapWeight"/>),
    /// on the floor crossed 45 degrees to it. Then each pin with a weight above 0 moves the link nearest to it onto
    /// it (by that weight). No allocation.
    /// </summary>
    public static void Place(Vector3[] path, Pin[] pins, float[] pinWeights, int pinCount, float wrapWeight, Vector3[] centres, Vector3[] tangents, Vector3[] widths)
    {
        var n = centres.Length;
        var total = Length0(path);
        var carried = Vector3.zero;
        var bar = BarLatchEnd;
        var zFar = bar.z - WrapOut - .03f;
        var zNear = bar.z - WrapIn + .03f;
        for (var k = 0; k < n; k++)
        {
            var d = n > 1 ? k * total / (n - 1) : 0f;
            var c = PointAt(path, d);
            // the tangent from a chord a link long, so a link follows the curve it sits on
            var t = PointAt(path, Mathf.Min(total, d + ChainLength * .5f)) - PointAt(path, Mathf.Max(0f, d - ChainLength * .5f));
            t = t.sqrMagnitude > 1e-12f ? t.normalized : Vector3.forward;
            var reference = k == 0 ? (Mathf.Abs(Vector3.Dot(t, Vector3.right)) > .9f ? Vector3.up : Vector3.right) : carried;
            var plane = Vector3.ProjectOnPlane(reference, t);
            if (plane.sqrMagnitude < 1e-10f) plane = Vector3.ProjectOnPlane(Vector3.up, t);
            if (plane.sqrMagnitude < 1e-10f) plane = Vector3.ProjectOnPlane(Vector3.forward, t);
            plane.Normalize();
            carried = plane;
            var u = (k & 1) == 0 ? plane : Vector3.Cross(t, plane).normalized;
            if (wrapWeight > 0f && c.z > zFar && c.z < zNear)
            {
                var onAxis = new Vector3(bar.x, bar.y, c.z);
                var radial = Vector3.ProjectOnPlane(c - onAxis, t);
                if ((c - onAxis).magnitude < WrapRadius + .02f && radial.sqrMagnitude > 1e-10f)
                {
                    var w = Quaternion.AngleAxis((k & 1) == 0 ? 45f : -45f, t) * radial.normalized;
                    u = Vector3.Lerp(u, w, wrapWeight);
                }
            }
            if (c.y < FloorLinkHeight + .012f)
            {
                var flat = Vector3.ProjectOnPlane(Vector3.up, t);
                if (flat.sqrMagnitude > 1e-10f) u = Quaternion.AngleAxis((k & 1) == 0 ? 45f : -45f, t) * flat.normalized;
            }
            centres[k] = c;
            tangents[k] = t;
            widths[k] = u;
        }
        for (var p = 0; p < pinCount; p++)
        {
            var w = pinWeights != null ? pinWeights[p] : 1f;
            if (w <= 0f) continue;
            var pin = pins[p];
            var best = 0;
            for (var k = 1; k < n; k++)
                if ((centres[k] - pin.centre).sqrMagnitude < (centres[best] - pin.centre).sqrMagnitude) best = k;
            centres[best] = Vector3.Lerp(centres[best], pin.centre, w);
            var along = Vector3.Dot(tangents[best], pin.along) < 0f ? -pin.along : pin.along;
            tangents[best] = Vector3.Lerp(tangents[best], along, w).normalized;
            widths[best] = Vector3.Lerp(widths[best], pin.width, w);
        }
        for (var k = 0; k < n; k++)
        {
            var u = Vector3.ProjectOnPlane(widths[k], tangents[k]);
            widths[k] = u.sqrMagnitude > 1e-12f ? u.normalized : Vector3.ProjectOnPlane(Vector3.up, tangents[k]).normalized;
        }
    }

    /// <summary>A link's wire centre line (m points round its stadium): for clearance checks (tools).</summary>
    public static void LinkCentreLine(Vector3 c, Vector3 t, Vector3 u, int m, List<Vector3> into)
    {
        var rc = (ChainWidth - ChainWire) * .5f;
        var hs = (ChainLength - ChainWidth) * .5f;
        for (var i = 0; i < m; i++)
        {
            var a = i * Mathf.PI * 2f / m;
            var x = Mathf.Cos(a);
            into.Add(c + t * (x * rc + Mathf.Sign(x) * hs) + u * (Mathf.Sin(a) * rc));
        }
    }

    // ---------------------------------------------------------------- meshing

    /// <summary>
    /// One chain's mesh for a fixed link count: each link a stadium of round wire, LOD0 12 x 6 segments (144 triangles),
    /// LOD1 8 x 4 (64), LOD2 6 x 3 (36). The indices and UVs are written once; Write fills positions and normals into
    /// buffers kept from the first call (no allocation per frame). One sub-mesh (Prop_ChainOxide).
    /// </summary>
    public sealed class ChainMesher
    {
        public readonly Mesh mesh;
        readonly int links, along, sides;
        readonly float wire;
        readonly List<Vector3> v, nrm;
        readonly Vector3[] loop, loopTangent;
        bool topology;

        public ChainMesher(int links, int lod, string name, bool dynamic)
        {
            this.links = Mathf.Max(1, links);
            along = lod <= 0 ? 12 : lod == 1 ? 8 : 6;
            sides = lod <= 0 ? 6 : lod == 1 ? 4 : 3;
            // the wire's polygon keeps a round wire's mean width (circumradius r pi / (n sin(pi / n))): the coarser levels
            // keep the chain's silhouette at 12-30 m
            wire = ChainWire * .5f * Mathf.PI / (sides * Mathf.Sin(Mathf.PI / sides));
            var count = this.links * (along + 1) * (sides + 1);
            v = new List<Vector3>(count);
            nrm = new List<Vector3>(count);
            for (var i = 0; i < count; i++) { v.Add(Vector3.zero); nrm.Add(Vector3.up); }
            loop = new Vector3[along + 1];
            loopTangent = new Vector3[along + 1];
            mesh = new Mesh { name = name, hideFlags = HideFlags.DontSave };
            if (count > 65000) mesh.indexFormat = IndexFormat.UInt32;
            if (dynamic) mesh.MarkDynamic();
        }

        public int Triangles => links * along * sides * 2;

        /// <summary>Write the links (centres, long axes, width directions) into the mesh.</summary>
        public void Write(Vector3[] centres, Vector3[] tangents, Vector3[] widths)
        {
            var rc = (ChainWidth - ChainWire) * .5f;
            var hs = (ChainLength - ChainWidth) * .5f;
            var ring = sides + 1;
            var vi = 0;
            var n = Mathf.Min(links, centres.Length);
            for (var k = 0; k < links; k++)
            {
                var kk = Mathf.Min(k, n - 1);
                var c = centres[kk];
                var t = tangents[kk];
                var u = widths[kk];
                var b = Vector3.Cross(t, u).normalized;
                for (var i = 0; i <= along; i++)
                {
                    var a = (i % along) * Mathf.PI * 2f / along;
                    var x = Mathf.Cos(a);
                    var y = Mathf.Sin(a);
                    loop[i] = c + t * (x * rc + Mathf.Sign(x) * hs) + u * (y * rc);
                    loopTangent[i] = (t * (-y) + u * x).normalized;
                }
                for (var i = 0; i <= along; i++)
                {
                    var tan = loopTangent[i];
                    var nn = Vector3.Cross(tan, b).normalized;
                    var bin = Vector3.Cross(tan, nn);
                    for (var s = 0; s <= sides; s++)
                    {
                        var a = s * Mathf.PI * 2f / sides;
                        var dir = nn * Mathf.Cos(a) + bin * Mathf.Sin(a);
                        v[vi] = loop[i] + dir * wire;
                        nrm[vi] = dir;
                        vi++;
                    }
                }
            }
            mesh.SetVertices(v);
            mesh.SetNormals(nrm);
            if (!topology)
            {
                topology = true;
                var uv = new List<Vector2>(v.Count);
                var tri = new List<int>(Triangles * 3);
                for (var k = 0; k < links; k++)
                {
                    var baseIndex = k * (along + 1) * ring;
                    for (var i = 0; i <= along; i++)
                        for (var s = 0; s <= sides; s++)
                            uv.Add(new Vector2(i * .016f, s * Mathf.PI * 2f * wire / sides));
                    for (var i = 0; i < along; i++)
                        for (var s = 0; s < sides; s++)
                        {
                            var a = baseIndex + i * ring + s;
                            var bb = a + ring;
                            tri.Add(a); tri.Add(a + 1); tri.Add(bb);
                            tri.Add(bb); tri.Add(a + 1); tri.Add(bb + 1);
                        }
                }
                mesh.SetUVs(0, uv);
                mesh.subMeshCount = 1;
                mesh.SetTriangles(tri, 0);
            }
            mesh.RecalculateBounds();
        }
    }

    // ---------------------------------------------------------------- the cached states

    static readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
    static Material chain, steel;

    /// <summary>Meshes made so far (tools: the cache must not grow with builds).</summary>
    public static int MeshCount => meshes.Count;

    /// <summary>Resources/Surfaces/Prop_ChainOxide (a plain stand-in of the same value when it is missing).</summary>
    public static Material Chain
    {
        get
        {
            if (chain == null) chain = FrontRoomsSurfaces.TryGet(ChainSurface) ?? FrontRoomsSurfaces.Lit("A13 chain (stand-in)", new Color(.204f, .188f, .169f), .35f, .55f);
            return chain;
        }
    }

    /// <summary>Resources/Surfaces/Prop_SteelLaminated (a plain stand-in of the same value when it is missing).</summary>
    public static Material Steel
    {
        get
        {
            if (steel == null) steel = FrontRoomsSurfaces.TryGet(SteelSurface) ?? FrontRoomsSurfaces.Lit("A13 laminated steel (stand-in)", new Color(.604f, .612f, .592f), .55f, .85f);
            return steel;
        }
    }

    // reused by the static builds (main thread only)
    static Vector3[] sc, st, su;
    static readonly Pin[] pinBuffer = new Pin[3];

    static void Buffers()
    {
        var n = Links;
        if (sc == null || sc.Length != n)
        {
            sc = new Vector3[n];
            st = new Vector3[n];
            su = new Vector3[n];
        }
    }

    /// <summary>The links of LOCKED (leaf rig frame): the heel and toe end links on the shut bow, the staple's link.</summary>
    public static int LockedPins(Pin[] into)
    {
        into[0] = LockedHeelPin;
        into[1] = StaplePin;
        into[2] = LockedToePin;
        return 3;
    }

    /// <summary>The links of UNLOCKED (shut frame): the heel end link on the open bow, the staple's link.</summary>
    public static int UnlockedPins(Pin[] into)
    {
        into[0] = UnlockedHeelPin;
        into[1] = StaplePin;
        return 2;
    }

    /// <summary>LOCKED's chain (leaf rig frame), one mesh per level of detail.</summary>
    public static Mesh Locked(int lod) => Cached("A13 R1 locked lod" + lod, lod, true);

    /// <summary>UNLOCKED's chain (shut frame), one mesh per level of detail.</summary>
    public static Mesh Unlocked(int lod) => Cached("A13 R1 unlocked lod" + lod, lod, false);

    static Mesh Cached(string key, int lod, bool locked)
    {
        if (meshes.TryGetValue(key, out var m) && m != null) return m;
        Buffers();
        var count = locked ? LockedPins(pinBuffer) : UnlockedPins(pinBuffer);
        Place(locked ? LockedPath : UnlockedPath, pinBuffer, null, count, locked ? 1f : 0f, sc, st, su);
        var mesher = new ChainMesher(Links, lod, key, false);
        mesher.Write(sc, st, su);
        m = mesher.mesh;
        m.RecalculateTangents();
        meshes[key] = m;
        return m;
    }

    // ---------------------------------------------------------------- stand-ins (a kit missing)

    /// <summary>A plain stand-in for a lock kit missing from Resources (one mesh): the body's plates, the shackle's J, the plug's disc, the staple's plate and U.</summary>
    public static Mesh StandIn(string kit)
    {
        var key = "A13 stand-in " + kit;
        if (meshes.TryGetValue(key, out var m) && m != null) return m;
        var mb = new MB();
        if (kit == BodyKit)
        {
            for (var i = 0; i < 7; i++)
            {
                var h = BodyH / 7f;
                var inset = i % 2 == 0 ? 0f : .0007f;
                mb.Box(Matrix4x4.identity, new Vector3(0f, -BodyH + (i + .5f) * h, 0f), new Vector3(BodyW - inset, h - .0003f, BodyD - inset));
            }
        }
        else if (kit == ShackleKit)
        {
            var line = new List<Vector3> { new Vector3(0f, -.03f, 0f), new Vector3(0f, ShackleBowC, 0f) };
            for (var i = 1; i < 10; i++)
            {
                var a = Mathf.PI - i * Mathf.PI / 10f;
                line.Add(new Vector3(ShackleHalf + Mathf.Cos(a) * ShackleHalf, ShackleBowC + Mathf.Sin(a) * ShackleHalf, 0f));
            }
            line.Add(new Vector3(2f * ShackleHalf, ShackleBowC, 0f));
            line.Add(new Vector3(2f * ShackleHalf, -.009f, 0f));
            mb.Tube(line, ShackleWireR, 10, false);
        }
        else if (kit == PlugKit)
        {
            mb.Cylinder(new Vector3(0f, -.0001f, 0f), new Vector3(0f, .002f, 0f), .0065f, 16);
        }
        else
        {
            mb.Box(Matrix4x4.identity, new Vector3(0f, 0f, .003f), new Vector3(.076f, .127f, .006f));
            var u = new List<Vector3> { new Vector3(0f, -.01885f, .0045f) };
            for (var i = 0; i <= 12; i++)
            {
                var a = -Mathf.PI * .5f + i * Mathf.PI / 12f;
                u.Add(new Vector3(0f, .01885f * Mathf.Sin(a), .0188f + .01885f * Mathf.Cos(a)));
            }
            u.Add(new Vector3(0f, .01885f, .0045f));
            mb.Tube(u, .00635f, 10, false);
        }
        m = mb.ToMesh(key);
        meshes[key] = m;
        return m;
    }

    /// <summary>A small mesh builder (one sub-mesh) for the stand-ins.</summary>
    public sealed class MB
    {
        public readonly List<Vector3> v = new List<Vector3>();
        public readonly List<Vector3> n = new List<Vector3>();
        public readonly List<Vector2> uv = new List<Vector2>();
        readonly List<int> tri = new List<int>();

        void Tri(int a, int b, int c, Vector3 normal)
        {
            if (Vector3.Dot(Vector3.Cross(v[b] - v[a], v[c] - v[a]), normal) >= 0f) { tri.Add(a); tri.Add(b); tri.Add(c); }
            else { tri.Add(a); tri.Add(c); tri.Add(b); }
        }

        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
        {
            var i = v.Count;
            v.Add(a); v.Add(b); v.Add(c); v.Add(d);
            for (var k = 0; k < 4; k++) { n.Add(normal); uv.Add(Vector2.zero); }
            Tri(i, i + 1, i + 2, normal);
            Tri(i, i + 2, i + 3, normal);
        }

        /// <summary>A box (centre, size) in the frame <paramref name="m"/> (rotation and translation only).</summary>
        public void Box(Matrix4x4 m, Vector3 c, Vector3 size)
        {
            var h = size * .5f;
            Vector3 P(float x, float y, float z) => m.MultiplyPoint3x4(c + new Vector3(x * h.x, y * h.y, z * h.z));
            Vector3 N(Vector3 dir) => m.MultiplyVector(dir).normalized;
            Quad(P(1, -1, 1), P(-1, -1, 1), P(-1, 1, 1), P(1, 1, 1), N(Vector3.forward));
            Quad(P(-1, -1, -1), P(1, -1, -1), P(1, 1, -1), P(-1, 1, -1), N(Vector3.back));
            Quad(P(1, -1, -1), P(1, -1, 1), P(1, 1, 1), P(1, 1, -1), N(Vector3.right));
            Quad(P(-1, -1, 1), P(-1, -1, -1), P(-1, 1, -1), P(-1, 1, 1), N(Vector3.left));
            Quad(P(-1, 1, -1), P(1, 1, -1), P(1, 1, 1), P(-1, 1, 1), N(Vector3.up));
            Quad(P(-1, -1, 1), P(1, -1, 1), P(1, -1, -1), P(-1, -1, -1), N(Vector3.down));
        }

        /// <summary>A tube of radius r along a polyline, closed (a ring) or open.</summary>
        public void Tube(IList<Vector3> path, float r, int sides, bool closed)
        {
            var n0 = path.Count;
            if (n0 < 2) return;
            var count = closed ? n0 + 1 : n0;
            var baseIndex = v.Count;
            var prevNormal = Vector3.zero;
            for (var i = 0; i < count; i++)
            {
                var p = path[i % n0];
                var tangent = closed ? (path[(i + 1) % n0] - path[(i - 1 + n0) % n0]).normalized
                    : (path[Mathf.Min(i + 1, n0 - 1)] - path[Mathf.Max(i - 1, 0)]).normalized;
                var nrm = i == 0 ? Vector3.Cross(tangent, Mathf.Abs(Vector3.Dot(tangent, Vector3.up)) > .9f ? Vector3.forward : Vector3.up).normalized
                    : Vector3.ProjectOnPlane(prevNormal, tangent).normalized;
                if (nrm.sqrMagnitude < 1e-6f) nrm = Vector3.Cross(tangent, Vector3.right).normalized;
                prevNormal = nrm;
                var bin = Vector3.Cross(tangent, nrm);
                for (var s = 0; s <= sides; s++)
                {
                    var a = s * Mathf.PI * 2f / sides;
                    var dir = nrm * Mathf.Cos(a) + bin * Mathf.Sin(a);
                    v.Add(p + dir * r);
                    n.Add(dir);
                    uv.Add(new Vector2(i * .01f, s / (float)sides));
                }
            }
            var ring = sides + 1;
            for (var i = 0; i < count - 1; i++)
                for (var s = 0; s < sides; s++)
                {
                    var a = baseIndex + i * ring + s;
                    var b = a + ring;
                    tri.Add(a); tri.Add(a + 1); tri.Add(b);
                    tri.Add(b); tri.Add(a + 1); tri.Add(b + 1);
                }
        }

        /// <summary>A capped cylinder from a to b.</summary>
        public void Cylinder(Vector3 a, Vector3 b, float r, int sides)
        {
            Tube(new List<Vector3> { a, b }, r, sides, false);
            var axis = (b - a).normalized;
            Disc(a, -axis, r, sides);
            Disc(b, axis, r, sides);
        }

        void Disc(Vector3 c, Vector3 normal, float r, int sides)
        {
            var u = Vector3.Cross(normal, Mathf.Abs(Vector3.Dot(normal, Vector3.up)) > .9f ? Vector3.forward : Vector3.up).normalized;
            var w = Vector3.Cross(normal, u);
            var i0 = v.Count;
            v.Add(c); n.Add(normal); uv.Add(Vector2.zero);
            for (var s = 0; s <= sides; s++)
            {
                var a = s * Mathf.PI * 2f / sides;
                v.Add(c + (u * Mathf.Cos(a) + w * Mathf.Sin(a)) * r);
                n.Add(normal);
                uv.Add(Vector2.zero);
            }
            for (var s = 0; s < sides; s++) Tri(i0, i0 + 1 + s, i0 + 2 + s, normal);
        }

        public Mesh ToMesh(string name)
        {
            var m = new Mesh { name = name, hideFlags = HideFlags.DontSave };
            m.SetVertices(v);
            m.SetNormals(n);
            m.SetUVs(0, uv);
            m.SetTriangles(tri, 0);
            m.RecalculateBounds();
            return m;
        }
    }
}
