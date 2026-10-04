using System;
using UnityEngine;
using G = FrontRoomsShotTimings.GlassBreak;

/// <summary>
/// The glass break's shot, "Brace, strike, flinch" (the visual chat's glass
/// destruction plan §3, decided by Red on 2026-10-03), on the camera rig.
///
/// The shot's clock t is the hold's progress (× 1 s) up to the shatter, real
/// time after it. The camera braces, winds up and strikes three times (the
/// impacts land on the sound chat's beats, 0.35 / 0.70 / 1.00, with a recoil
/// and a rotation-only shake), follows through, flinches, looks down at the
/// sill and returns by 1.65 s. Offsets are targets added to BaseEye and turned
/// toward the impact (at most 6° of yaw, 8° of pitch); the rig scales them by
/// the Camera motion setting, so at Off the shot plays in place.
///
/// Locks (§3.3): while held, move is locked and look is free in a ±5° cone;
/// 1.00–1.10 both are locked; until 1.30 any move key, and until 1.40 a look
/// of more than 3°, ends the shot. Letting go of E before the shatter cancels
/// it (0.2 s back; the cracks stay). The Relay's sight or a chase demotes the
/// shot to the audit's first design at half strength instead of cancelling
/// the hold; after the shatter it skips to the end in 0.12 s. A climb takes
/// over in 0.10 s. Gameplay never reads this camera: only BaseEye.
/// </summary>
public sealed class FrontRoomsGlassShot
{
    /// <summary>
    /// A beat landed this frame: 1 Crack1, 2 Crack2, 3 Shatter, with an
    /// intensity of the Camera motion setting × the shot's strength (1, 0.5 when
    /// demoted, 0 once skipped). The visual chat's post stack scales its CA
    /// pulse by it (and drops the pulse with Reduce flashing).
    /// </summary>
    public static event Action<int, float> Beat;

    /// <summary>
    /// How much of the glass shot is on screen this frame, 0..1: its blend weight × the Camera motion
    /// setting × its strength (0.5 when demoted); 0 when no glass shot runs or blends. For the visual
    /// chat's post stack (the brace vignette, and the CA pulse as Beat × Weight). Updated in Tick.
    /// </summary>
    public static float Weight { get; private set; }

    readonly FrontRoomsCameraRig rig;
    ShotHandle handle;
    Vector3 impact, inward;
    float t, age, demote, coneYaw, conePitch, softLook;
    bool live, shattered, threatened;
    int beatsLanded;
    readonly float[] recoilClock = { -1f, -1f, -1f };

    public FrontRoomsGlassShot(FrontRoomsCameraRig rig) => this.rig = rig;

    /// <summary>The shot is running and not on its way out.</summary>
    public bool Active => live;
    /// <summary>Let go, but still blending back to the eye: E on the same pane picks the shot up again from where the camera is.</summary>
    public bool Blending => !live && handle != null && !handle.Ended;
    /// <summary>The glass has given: the shot runs on in real time, and E belongs to nothing.</summary>
    public bool Shattered => live && shattered;
    /// <summary>The shot's clock: the hold's progress (s), real time after the shatter.</summary>
    public float Clock => t;
    /// <summary>0 for the full shot, 1 when a chase or the Relay's sight has demoted it.</summary>
    public float Demote => demote;
    /// <summary>
    /// Movement is held: through the hold (that is breaking the glass, at any Camera motion), and after the
    /// shatter until 1.30 s (a move key ends it from 1.10) when there is a pose to protect.
    /// </summary>
    public bool MoveLocked => live && (!shattered || (t < G.MoveSoftLockEnd && Motion > 0f));
    /// <summary>Look is held only 1.00–1.10 s, the hard lock round the shatter, and never at Camera motion Off (look stays free).</summary>
    public bool LookLocked => live && shattered && t < G.HardLockEnd && Motion > 0f;

    static float Motion => FrontRoomsSettings.CameraMotionScale;

    /// <summary>
    /// Where the body steps to as the hold starts (§3.2): 0.55 m from the pane
    /// along its normal, at most 0.65 m forward and 0.25 m sideways toward the
    /// impact; the feet where they are when no step is needed. Swept by the
    /// caller's controller.
    /// </summary>
    public static Vector3 StandPoint(Vector3 feet, Vector3 impact, Vector3 inward)
    {
        inward.y = 0f;
        if (inward.sqrMagnitude < 1e-6f) return feet;
        inward.Normalize();
        var across = Vector3.Cross(Vector3.up, inward);
        var to = impact - feet;
        var forward = Mathf.Clamp(Vector3.Dot(to, inward) - G.StandDistance, 0f, G.StepInMaxForward);
        var side = Mathf.Clamp(Vector3.Dot(to, across), -G.StepInMaxLateral, G.StepInMaxLateral);
        return feet + inward * forward + across * side;
    }

    /// <summary>
    /// E went down on a pane: start (or resume) the shot at the hold's
    /// <paramref name="progress"/>. Beats already behind it (a cracked pane)
    /// do not land again.
    /// </summary>
    public void Begin(Vector3 impactWorld, Vector3 inwardDir, float progress)
    {
        impact = impactWorld;
        inwardDir.y = 0f;
        inward = inwardDir.sqrMagnitude > 1e-6f ? inwardDir.normalized : Vector3.forward;
        t = Mathf.Clamp(progress, 0f, G.Shatter);
        beatsLanded = Mathf.Min(2, G.StageAt(t));
        for (var i = 0; i < recoilClock.Length; i++) recoilClock[i] = -1f;
        shattered = threatened = false;
        demote = 0f;
        softLook = 0f;
        age = 0f;
        // How far the look was from the impact at E-down: the cone starts that wide and narrows to ±5° over the brace.
        var e = rig.BaseEye;
        var angles = e.rotation.eulerAngles;
        TowardImpact(e.position, out var toYaw, out var toPitch);
        coneYaw = Mathf.Max(G.LookCone, Mathf.Abs(Mathf.DeltaAngle(toYaw, angles.y)));
        conePitch = Mathf.Max(G.LookCone, Mathf.Abs(Mathf.DeltaAngle(toPitch, Mathf.DeltaAngle(0f, angles.x))));
        // A shot still blending out (let go a moment ago) hands its weight on, so the camera never snaps.
        var carry = 0f;
        if (handle != null && !handle.Ended)
        {
            carry = handle.weight;
            rig.CancelShot(handle, 0f);
        }
        handle = rig.BeginShot(new ShotSpec
        {
            kind = ShotKind.Glass, target = ShotPose, fov = ShotFov, focus = impact,
            blendIn = G.PoseIn, blendOut = G.CancelBlend, duration = G.ReleaseEnd,
        });
        handle.weight = carry;
        live = true;
    }

    /// <summary>The hold's progress this frame (0..1, before the shatter): the clock follows it and the beats land on it.</summary>
    public void Hold(float progress)
    {
        if (!live || shattered) return;
        t = Mathf.Clamp(progress, t, G.Shatter);
        LandBeats();
    }

    /// <summary>The glass gave: the third beat lands and the clock runs in real time from here.</summary>
    public void Shatter()
    {
        if (!live) return;
        t = G.Shatter;
        shattered = true;
        LandBeats();
        // Shattered under a chase or the Relay's eye: the shot only stands in the way now.
        if (threatened) End(G.ChaseSkipBlend);
    }

    /// <summary>E let go before the shatter: back to the eye in 0.2 s (the cracks stay).</summary>
    public void Release()
    {
        if (!live || shattered) return;
        End(G.CancelBlend);
    }

    /// <summary>A climb through the frame starts: hand the camera over in 0.1 s.</summary>
    public void ClimbStarted()
    {
        if (live) End(G.ClimbHandOff);
        // Ended this very frame by the move that starts the climb: hurry its blend to the hand-off.
        else if (Blending) rig.CancelShot(handle, G.ClimbHandOff);
    }

    /// <summary>Drop the shot at once (a run start, a teleport).</summary>
    public void Reset()
    {
        if (handle != null && !handle.Ended) rig.CancelShot(handle, 0f);
        handle = null;
        live = false;
        Weight = 0f;
    }

    /// <summary>
    /// Advance the shot. <paramref name="threat"/>: the Relay sees the player
    /// or chases. <paramref name="moveIntent"/>: the player is asking to move
    /// (a move key or axis held this frame, before any lock). <paramref name="lookDegrees"/>:
    /// how far the mouse turned the base look this frame.
    /// </summary>
    public void Tick(float dt, bool threat, bool moveIntent, float lookDegrees)
    {
        for (var i = 0; i < recoilClock.Length; i++) if (recoilClock[i] >= 0f) recoilClock[i] += dt;
        Weight = handle != null && !handle.Ended ? handle.weight * Motion * Mathf.Lerp(1f, G.ChaseDemoteScale, demote) : 0f;
        if (!live) return;
        age += dt;
        if (threat) threatened = true;
        // After the shatter the shot only stands between the player and the Relay: skip to the end.
        if (shattered && threatened) { End(G.ChaseSkipBlend); return; }
        demote = Mathf.MoveTowards(demote, threatened ? 1f : 0f, dt / G.ChaseSkipBlend);
        if (!shattered) return;
        t += dt;
        if (t >= G.HardLockEnd)
        {
            if (moveIntent && t < G.MoveSoftLockEnd) { End(G.LookBlendBack); return; }
            if (t < G.LookSoftLockEnd)
            {
                softLook += Mathf.Abs(lookDegrees);
                if (softLook > G.SoftBreakLookDeg) { End(G.LookBlendBack); return; }
            }
        }
        if (t >= G.ReleaseEnd) End(.05f);
    }

    /// <summary>
    /// While the hold runs, keep the base look within ±5° of the impact, seen from where the eye is now (the
    /// step-in moves it). The cone starts as wide as the look was off at E-down and narrows over the brace,
    /// so it never yanks the view.
    /// </summary>
    public void ClampLook(ref float yaw, ref float pitch)
    {
        if (!live || shattered || Motion <= 0f) return;   // at Camera motion Off look stays free
        TowardImpact(rig.BaseEye.position, out var toYaw, out var toPitch);
        var k = Mathf.SmoothStep(0f, 1f, age / G.PoseIn);
        var yawCone = Mathf.Lerp(coneYaw, G.LookCone, k);
        var pitchCone = Mathf.Lerp(conePitch, G.LookCone, k);
        yaw = toYaw + Mathf.Clamp(Mathf.DeltaAngle(toYaw, yaw), -yawCone, yawCone);
        pitch = Mathf.Clamp(pitch, toPitch - pitchCone, toPitch + pitchCone);
    }

    // The yaw and pitch (+ = down) from a point to the impact.
    void TowardImpact(Vector3 from, out float yaw, out float pitch)
    {
        var to = impact - from;
        yaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
        pitch = -Mathf.Atan2(to.y, new Vector2(to.x, to.z).magnitude) * Mathf.Rad2Deg;
    }

    // The handle is kept while it blends out (Blending), so a quick re-press can pick the shot up again.
    void End(float blendOut)
    {
        live = false;
        if (handle != null && !handle.Ended) rig.CancelShot(handle, blendOut);
    }

    void LandBeats()
    {
        var beat = G.StageAt(t);
        while (beatsLanded < beat)
        {
            var i = beatsLanded++;
            var strength = Mathf.Lerp(1f, G.ChaseDemoteScale, demote);
            if (demote > .5f)
            {
                rig.AddTrauma(G.DemoteShakeDeg[i] * G.ChaseDemoteScale, G.DemoteShakeDecay[i]);
                rig.AddOffset(Vector3.forward * G.DemoteJabFwd * G.ChaseDemoteScale, G.DemoteJab);
            }
            else
            {
                rig.AddTrauma(G.ShakeDeg[i], G.ShakeDecay[i]);
                recoilClock[i] = 0f;
            }
            Beat?.Invoke(i + 1, FrontRoomsSettings.CameraMotionScale * strength);
        }
    }

    // ---------- The pose: keyframes of (fwd, drop, yaw, roll, pitch), smoothstep between them ----------

    struct Key
    {
        public float t, fwd, drop, yaw, roll, pitch;
        public Key(float t, float fwd, float drop, float yaw, float roll, float pitch) { this.t = t; this.fwd = fwd; this.drop = drop; this.yaw = yaw; this.roll = roll; this.pitch = pitch; }
    }

    static readonly Key[] Keys =
    {
        new Key(0f, 0f, 0f, 0f, 0f, 0f),
        new Key(G.WindupStart[0], G.BraceFwd, G.BraceDrop, G.BraceYaw, G.BraceRoll, 0f),
        new Key(G.StrikeStart[0], G.WindupFwd[0], G.BraceDrop, G.WindupYaw[0], G.WindupRoll[0], G.WindupPitch[0]),
        new Key(G.Crack1, G.StrikeFwd[0], G.BraceDrop, G.StrikeYaw[0], G.StrikeRoll[0], 0f),
        new Key(G.WindupStart[1], G.BraceFwd, G.BraceDrop, G.BraceYaw, G.BraceRoll, 0f),
        new Key(G.StrikeStart[1], G.WindupFwd[1], G.BraceDrop, G.WindupYaw[1], G.WindupRoll[1], G.WindupPitch[1]),
        new Key(G.Crack2, G.StrikeFwd[1], G.BraceDrop, G.StrikeYaw[1], G.StrikeRoll[1], 0f),
        new Key(G.WindupStart[2], G.BraceFwd, G.BraceDropAfterCrack2, G.BraceYaw, G.BraceRoll, 0f),
        new Key(G.StrikeStart[2], G.WindupFwd[2], G.BraceDropAfterCrack2, G.WindupYaw[2], G.WindupRoll[2], G.WindupPitch[2]),
        new Key(G.Shatter, G.StrikeFwd[2], G.BraceDropAfterCrack2, G.StrikeYaw[2], G.StrikeRoll[2], 0f),
        new Key(G.Shatter + G.FollowThrough, G.FollowThroughFwd, G.BraceDropAfterCrack2, G.StrikeYaw[2], G.StrikeRoll[2], 0f),
        new Key(G.FlinchEnd, G.FlinchFwd, G.FlinchDrop, G.FlinchYaw, G.FlinchRoll, G.FlinchPitch),
        new Key(G.LookEnd, 0f, G.LookDrop, 0f, 0f, G.LookPitch),
        new Key(G.ReleaseEnd, 0f, 0f, 0f, 0f, 0f),
    };

    /// <summary>The pose offsets at shot time <paramref name="time"/> (Design 2, before the demote and recoil): fwd, drop (m), yaw, roll, pitch (°).</summary>
    public static (float fwd, float drop, float yaw, float roll, float pitch) PoseAt(float time)
    {
        if (time <= Keys[0].t) return (0f, 0f, 0f, 0f, 0f);
        for (var i = 1; i < Keys.Length; i++)
        {
            if (time > Keys[i].t) continue;
            var a = Keys[i - 1];
            var b = Keys[i];
            var u = Mathf.SmoothStep(0f, 1f, (time - a.t) / Mathf.Max(1e-4f, b.t - a.t));
            return (Mathf.Lerp(a.fwd, b.fwd, u), Mathf.Lerp(a.drop, b.drop, u), Mathf.Lerp(a.yaw, b.yaw, u), Mathf.Lerp(a.roll, b.roll, u), Mathf.Lerp(a.pitch, b.pitch, u));
        }
        return (0f, 0f, 0f, 0f, 0f);
    }

    /// <summary>The FOV delta at shot time <paramref name="time"/> (Design 2): −5° over the first 0.4 s, a −3° punch at the shatter, back to −5° by 1.40 and to 0 by 1.65.</summary>
    public static float FovAt(float time)
    {
        var pose = G.PoseFovDeg * Mathf.SmoothStep(0f, 1f, time / G.PoseFov);
        if (time >= G.LookEnd) pose = G.PoseFovDeg * (1f - Mathf.SmoothStep(0f, 1f, (time - G.LookEnd) / (G.ReleaseEnd - G.LookEnd)));
        var punch = 0f;
        if (time >= G.Shatter && time < G.FlinchEnd) punch = G.ShatterFovPunchDeg * Mathf.Clamp01((time - G.Shatter) / G.ShatterFovPunch);
        else if (time >= G.FlinchEnd && time < G.LookEnd) punch = G.ShatterFovPunchDeg * (1f - Mathf.SmoothStep(0f, 1f, (time - G.FlinchEnd) / (G.LookEnd - G.FlinchEnd)));
        return pose + punch;
    }

    // How far the view turns toward the impact (eased in with the brace, out with the release).
    static float LookAtWeight(float time) =>
        time < G.LookEnd ? Mathf.SmoothStep(0f, 1f, time / G.PoseIn) : 1f - Mathf.SmoothStep(0f, 1f, (time - G.LookEnd) / (G.ReleaseEnd - G.LookEnd));

    float ShotFov() => Mathf.Lerp(FovAt(t), G.DemoteFovDeg * G.ChaseDemoteScale * Mathf.SmoothStep(0f, 1f, t / G.PoseFov), demote);

    Pose ShotPose()
    {
        var e = rig.BaseEye;
        var p = PoseAt(t);
        // Demoted: the audit's first design at half strength (a lean toward the pane, no pose).
        var lean = G.DemoteLeanFwd * G.ChaseDemoteScale * Mathf.SmoothStep(0f, 1f, t / G.PoseIn);
        var keep = 1f - demote;
        var fwd = Mathf.Lerp(lean, p.fwd, keep);
        // The recoil: a critically damped kick back from each landed strike, peaking 1/ω after it.
        for (var i = 0; i < recoilClock.Length; i++)
        {
            if (recoilClock[i] < 0f) continue;
            var x = G.RecoilOmega * recoilClock[i];
            fwd -= G.Recoil[i] * x * Mathf.Exp(1f - x) * keep;
        }
        var position = e.position + inward * fwd + Vector3.down * (p.drop * keep);
        // Turned toward the impact by at most 6° of yaw and 8° of pitch.
        var to = impact - e.position;
        var baseAngles = e.rotation.eulerAngles;
        var baseYaw = baseAngles.y;
        var basePitch = Mathf.DeltaAngle(0f, baseAngles.x);
        var toYaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
        var toPitch = -Mathf.Atan2(to.y, new Vector2(to.x, to.z).magnitude) * Mathf.Rad2Deg;
        var w = LookAtWeight(t) * keep;
        var yaw = baseYaw + Mathf.Clamp(Mathf.DeltaAngle(baseYaw, toYaw), -G.LookAtMaxYaw, G.LookAtMaxYaw) * w;
        var pitch = basePitch + Mathf.Clamp(Mathf.DeltaAngle(basePitch, toPitch), -G.LookAtMaxPitch, G.LookAtMaxPitch) * w;
        // yaw + turns away from the striking (right) side: left. roll + tips toward the right shoulder. pitch + looks down.
        var rotation = Quaternion.Euler(pitch + p.pitch * keep, yaw - p.yaw * keep, -p.roll * keep);
        return new Pose(position, rotation);
    }
}
