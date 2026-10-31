using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>What a shot is, for listeners (the sound chat's snapshots, the visual chat's post).</summary>
public enum ShotKind { KeyPickup, Unlock, Open, Rattle, Glass, Climb, DoorBreak, Caught, Inspect }   // append-only (sound and touch read it by name)

/// <summary>How a shot may end early.</summary>
[Flags]
public enum ShotCancel
{
    None = 0,
    /// <summary>S or a second E, after FrontRoomsShotTimings.Unlock.CancelAfter (never Esc: Esc is pause).</summary>
    PlayerInput = 1,
    /// <summary>The Relay chasing or seeing the player frees them at once.</summary>
    Relay = 2,
}

/// <summary>Input the rig may swallow while a shot runs.</summary>
public enum ShotInput { Use, Back }

/// <summary>
/// How a shot's weight becomes the camera's pull. Cubic: the rig's ease-in-out both ways (every shot
/// before EXAMINE). Sine (research/inspect/10_design.md §3.5, C17b): in 0.5 - 0.5 cos(πw); out
/// 1 - cos(πw/2) from wherever the pull was when the shot began to end (fast start, settles), entered
/// over FrontRoomsShotTimings.Inspect.OutOnset (2 frames) so the speed never steps from 0 to its peak.
/// </summary>
public enum ShotEase { Cubic, Sine }

/// <summary>
/// A shot: where the camera goes (a world pose, asked every frame so it can
/// follow a moving anchor), how it blends in and out, and what it locks.
/// </summary>
public struct ShotSpec
{
    public ShotKind kind;
    /// <summary>The camera pose the shot blends to, in world space. Null: the shot plays in place.</summary>
    public Func<Pose> target;
    /// <summary>What the shot looks at, reported with ShotStarted / ShotEnded.</summary>
    public Vector3 focus;
    public float blendIn, blendOut;
    /// <summary>A delta from the player's field of view (negative narrows), kept within MaxFovDeltaDeg.</summary>
    public float fovDelta;
    /// <summary>A delta that changes during the shot (asked every frame, used instead of fovDelta when set).</summary>
    public Func<float> fov;
    public bool lockMove, lockLook;
    /// <summary>While look is not locked, how far it may wander from the shot's heading (degrees; 0 = free).</summary>
    public float lookCone;
    public ShotCancel cancel;
    /// <summary>The shot's length in seconds, for the push buffer of door shots (0 = open-ended: no buffer).</summary>
    public float duration;
    /// <summary>
    /// An age after which the shot no longer fades the HUD (0 = the whole shot). The unlock brings the
    /// HUD back with movement at 1.45 s while the shot stays alive to 1.55 s for the buffered push.
    /// </summary>
    public float hudUntil;
    /// <summary>
    /// The full pose and FOV at every Camera motion setting (C17): not scaled by the setting. At
    /// 0 &lt; motion &lt; 1 every blend time is ×FrontRoomsShotTimings.Inspect.HalfMotionTimeScale;
    /// at motion 0 the shot cuts in and out.
    /// </summary>
    public bool fullPose;
    /// <summary>The weight-to-pull curve (C17b). Cubic is the default.</summary>
    public ShotEase ease;
    /// <summary>Sine shots: rotation's own blend-out (seconds; 0 = blendOut). CancelShot shortens both (C17b).</summary>
    public float rotBlendOut;
    /// <summary>ShotKind.Inspect only: the lowest absolute FOV (degrees; 0 = BaseFov - MaxFovDeltaDeg), for the angular zoom (C17c).</summary>
    public float fovMin;
}

/// <summary>A running shot, to end or cancel it.</summary>
public sealed class ShotHandle
{
    internal ShotSpec spec;
    internal float weight, age;
    internal bool ending;
    internal float blendOut;
    // Sine shots (C17b): the rotation's weight, its blend-out, and the pulls held when the shot began to end.
    internal float rotWeight, rotBlendOut, outPull, outRotPull, eased, rotEased;
    internal bool outStarted;
    public ShotKind Kind => spec.kind;
    /// <summary>The raw blend weight, 0..1 (C18).</summary>
    public float Weight => weight;
    /// <summary>The position and FOV pull this frame, through the same ease the pose uses (C18): what a shot volume should follow.</summary>
    public float EasedWeight => eased;
    /// <summary>The rotation pull this frame (equals EasedWeight except while a Sine shot returns).</summary>
    public float RotationWeight => rotEased;
    public bool Ended { get; internal set; }
    public bool Cancelled { get; internal set; }
    /// <summary>The input that cancelled the shot through Consume (S or a second E); null when it was not cancelled by input.</summary>
    public ShotInput? CancelledBy { get; internal set; }
    /// <summary>Seconds since the shot began.</summary>
    public float Age => age;
}

/// <summary>
/// The player camera, in two layers (audit §3.0 and §6.4: camera language B).
///
/// BaseEye is the body plus eye height (minus a stance drop such as the
/// window-climb duck) with the base yaw and pitch: the ONLY pose gameplay
/// reads (the aim ray, the Relay's view of the player). The render camera is
/// BaseEye plus picture-only layers: a shot pose, decaying offsets (lean,
/// jab, dip), rotational trauma (shake) and FOV deltas. Every offset is
/// clamped against the world with a short spherecast, so the camera never
/// enters a wall.
///
/// It is a plain class ticked by its owner (FrontRooms3DGame, the test
/// walker), so pausing the owner pauses the shots. The Camera motion setting
/// scales every pose, shake and FOV change; at Off a shot plays in place with
/// look free (the objects still act).
/// </summary>
public sealed class FrontRoomsCameraRig
{
    /// <summary>A shot began or ended (cancelled shots end too): its kind and what it looks at.</summary>
    public static event Action<ShotKind, Vector3> ShotStarted, ShotEnded;

    readonly Transform body, eye;
    readonly Camera camera;
    readonly float eyeHeight;
    readonly List<ShotHandle> shots = new List<ShotHandle>();
    readonly List<(Vector3 local, float seconds, float age)> offsets = new List<(Vector3, float, float)>();
    readonly List<(float delta, float seconds, float age)> fovPushes = new List<(float, float, float)>();
    // FOV deltas easing back to nothing (ReleaseFov) and signed pitch kicks falling to nothing (Kick).
    readonly List<(float delta, float seconds, float age)> fovReleases = new List<(float, float, float)>();
    readonly List<(float degrees, float seconds, float age)> kicks = new List<(float, float, float)>();
    float trauma, traumaPeak, traumaDecay, traumaTime, hudFade, pitch, stanceDrop;
    readonly RaycastHit[] hits = new RaycastHit[16];   // C17d: never drop the nearest hit

    FrontRoomsCameraRig(Transform body, Transform eye, Camera camera, float eyeHeight)
    {
        this.body = body;
        this.eye = eye;
        this.camera = camera;
        this.eyeHeight = eyeHeight;
        BaseFov = camera != null ? camera.fieldOfView : 76f;
        authoredFov = BaseFov;
    }

    /// <summary>The field of view the layers last asked for, as authored at 16:9 (the camera draws its handheld aspect version).</summary>
    float authoredFov;

    /// <summary>
    /// Draw the last field of view again for the view's current shape: a fold or rotation changes the aspect while the
    /// rig is not ticking (title, pause). Handheld views squarer than 16:9 keep 16:9's horizontal field
    /// (FrontRoomsHandheld.AspectFov); desktop draws the authored value.
    /// </summary>
    public void ApplyAspectFov()
    {
        if (camera != null) camera.fieldOfView = FrontRoomsHandheld.AspectFov(authoredFov, camera.aspect);
    }

    /// <summary>
    /// Put an "Eye" pivot on the body at eye height and the camera under it at
    /// its origin. While no layer is active the camera sits exactly on the
    /// eye, so a game with the rig behaves as before.
    /// </summary>
    public static FrontRoomsCameraRig Attach(Transform body, Camera camera, float eyeHeight)
    {
        var eye = new GameObject("Eye").transform;
        eye.SetParent(body, false);
        eye.localPosition = new Vector3(0f, eyeHeight, 0f);
        eye.localRotation = Quaternion.identity;
        camera.transform.SetParent(eye, false);
        camera.transform.localPosition = Vector3.zero;
        camera.transform.localRotation = Quaternion.identity;
        return new FrontRoomsCameraRig(body, eye, camera, eyeHeight);
    }

    /// <summary>The player's own field of view: shots and pushes are deltas from it.</summary>
    public float BaseFov { get; set; }

    /// <summary>A drop of the eye that is part of the body's stance (the window-climb duck), so gameplay sees it too. Applied to the eye at once.</summary>
    public float StanceDrop
    {
        get => stanceDrop;
        set { stanceDrop = value; eye.localPosition = new Vector3(0f, eyeHeight - stanceDrop, 0f); }
    }

    /// <summary>The eye pivot (BaseEye's transform).</summary>
    public Transform Eye => eye;

    /// <summary>Body + eye height − stance drop, base yaw and pitch: the only pose gameplay rays read.</summary>
    public Pose BaseEye => new Pose(eye.position, eye.rotation);

    /// <summary>A shot is running (blending in, holding or blending out).</summary>
    public bool InShot => shots.Count > 0;

    /// <summary>Movement is suspended by a running shot.</summary>
    public bool MoveLocked { get; private set; }

    /// <summary>Look is suspended by a running shot (never at Camera motion Off).</summary>
    public bool LookLocked { get; private set; }

    /// <summary>The shake's current amplitude in degrees (0 = no shake).</summary>
    public float Trauma => trauma;

    /// <summary>0..1: how far the HUD (but not captions) is faded for a takeover. A close-up (ShotKind.Inspect) holds it
    /// until it starts home, then lets it back over HudFadeSeconds while the camera is still returning (O-25).</summary>
    public float HudFade => hudFade;

    /// <summary>A "push open" buffered by one E in the last moments of a door shot (read and cleared by the owner).</summary>
    public bool BufferedPush { get; set; }

    /// <summary>The newest shot's FOV delta as the last Tick applied it (after its blend weight and the Camera motion setting), for ReleaseFov.</summary>
    public float ShotFovDelta { get; private set; }

    /// <summary>The signed pitch the kicks add this frame (degrees, + = down), for tests.</summary>
    public float KickPitch { get; private set; }

    static float Motion => FrontRoomsSettings.CameraMotionScale;

    /// <summary>Set the base look's pitch (positive looks down); yaw is the body's rotation.</summary>
    public void SetBase(float basePitch)
    {
        pitch = basePitch;
        eye.localPosition = new Vector3(0f, eyeHeight - stanceDrop, 0f);
        eye.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    /// <summary>Begin a shot. Shots stack: the newest decides the pose.</summary>
    public ShotHandle BeginShot(ShotSpec spec)
    {
        spec.fovDelta = Mathf.Clamp(spec.fovDelta, -MaxFovDrop(spec), FrontRoomsShotTimings.MaxFovDeltaDeg);
        var handle = new ShotHandle { spec = spec, blendOut = spec.blendOut, rotBlendOut = spec.rotBlendOut > 0f ? spec.rotBlendOut : spec.blendOut };
        shots.Add(handle);
        BufferedPush = false;   // a push buffered in an earlier shot never carries over
        ShotStarted?.Invoke(spec.kind, spec.focus);
        return handle;
    }

    /// <summary>Blend a shot back to the eye over its blend-out.</summary>
    public void EndShot(ShotHandle handle)
    {
        if (handle == null || handle.ending) return;
        handle.ending = true;
    }

    /// <summary>
    /// End a shot that has already brought the camera home itself, over <paramref name="blendOut"/>
    /// seconds (0: ShotEnded on the next Tick). Not a cancel: Cancelled stays false. The unlock ends
    /// this way at 1.55 s, after its own return, so its buffered push window is not cut short.
    /// </summary>
    public void EndShot(ShotHandle handle, float blendOut)
    {
        if (handle == null || handle.ending) return;
        handle.ending = true;
        handle.blendOut = Mathf.Max(0f, blendOut);
        // A Sine shot's rotation returns on its own clock (rotBlendOut): never slower than the end asked for.
        handle.rotBlendOut = Mathf.Min(handle.rotBlendOut, handle.blendOut);
    }

    /// <summary>End a shot early, blending back over <paramref name="blendOut"/> seconds. A shot already on its way out only ever goes faster.</summary>
    public void CancelShot(ShotHandle handle, float blendOut = .2f)
    {
        if (handle == null) return;
        if (handle.ending)
        {
            handle.blendOut = Mathf.Min(handle.blendOut, blendOut);
            handle.rotBlendOut = Mathf.Min(handle.rotBlendOut, blendOut);
            return;
        }
        handle.Cancelled = true;
        handle.ending = true;
        handle.blendOut = Mathf.Min(handle.blendOut > 0f ? handle.blendOut : blendOut, blendOut);
        handle.rotBlendOut = Mathf.Min(handle.rotBlendOut > 0f ? handle.rotBlendOut : blendOut, blendOut);
    }

    /// <summary>Cancel every shot that the Relay may interrupt, fast. The game calls it on the hunter's Threat reading (RELAY §8, V2: TargetAcquired recently or Spotting; v1, a chase or its sight within 6 m).</summary>
    public void CancelForRelay()
    {
        foreach (var shot in shots)
            if ((shot.spec.cancel & ShotCancel.Relay) != 0) CancelShot(shot, FrontRoomsShotTimings.Unlock.ChaseFreeSnap);
    }

    /// <summary>A rotational shake of up to <paramref name="degrees"/>, falling linearly to nothing over <paramref name="decaySeconds"/>. Picture only.</summary>
    public void AddTrauma(float degrees, float decaySeconds)
    {
        degrees *= Motion;
        if (degrees <= trauma) return;
        trauma = traumaPeak = degrees;
        traumaDecay = Mathf.Max(.01f, decaySeconds);
    }

    /// <summary>A FOV change of <paramref name="delta"/> degrees there and back over <paramref name="seconds"/>.</summary>
    public void PushFov(float delta, float seconds)
    {
        fovPushes.Add((delta * Motion, Mathf.Max(.01f, seconds), 0f));
    }

    /// <summary>
    /// Hold a FOV delta the camera already shows (ShotFovDelta, so already scaled by Camera motion) and
    /// ease it back to nothing over <paramref name="seconds"/>, on a smoothstep. A cancelled shot's pose may
    /// snap back in 0.25 s, but its FOV must still take MinFovChangeSeconds (audit §3.0).
    /// </summary>
    public void ReleaseFov(float delta, float seconds)
    {
        if (Mathf.Abs(delta) < 1e-4f) return;
        fovReleases.Add((delta, Mathf.Max(.01f, seconds), 0f));
    }

    /// <summary>
    /// A signed pitch impulse (+ = forward, down) of <paramref name="pitchDeg"/> at once, falling
    /// linearly to nothing over <paramref name="decaySeconds"/>: the audit's "forward pitch impulse"
    /// (§3.3, §3.4), where AddTrauma is a shake. Picture only; scaled by Camera motion.
    /// </summary>
    public void Kick(float pitchDeg, float decaySeconds)
    {
        var degrees = pitchDeg * Motion;
        if (Mathf.Abs(degrees) < 1e-5f) return;
        kicks.Add((degrees, Mathf.Max(.01f, decaySeconds), 0f));
    }

    /// <summary>A camera-local offset (lean, jab, dip) there and back over <paramref name="seconds"/>. Clamped against the world.</summary>
    public void AddOffset(Vector3 local, float seconds)
    {
        offsets.Add((local * Motion, Mathf.Max(.01f, seconds), 0f));
    }

    /// <summary>
    /// Input during a shot: E and S are swallowed, not queued. E or S after
    /// the shot's cancel delay cancels a cancellable shot; one E in the last
    /// moments of a door shot is kept as a buffered push. True when the input
    /// was taken by the rig (the owner must not act on it).
    /// </summary>
    public bool Consume(ShotInput input)
    {
        if (shots.Count == 0) return false;
        var shot = shots[shots.Count - 1];
        if (shot.ending) return true;
        var door = shot.spec.kind == ShotKind.Open || shot.spec.kind == ShotKind.Unlock;
        if (input == ShotInput.Use && door && shot.spec.duration > 0f
            && shot.spec.duration - shot.age <= FrontRoomsShotTimings.DoorShotBufferedPush)
        {
            BufferedPush = true;   // the last 0.2 s of a door shot: kept as "push open", never a cancel
            return true;
        }
        if ((shot.spec.cancel & ShotCancel.PlayerInput) != 0 && shot.age >= FrontRoomsShotTimings.Unlock.CancelAfter)
        {
            CancelShot(shot, FrontRoomsShotTimings.Unlock.CancelCameraBack);
            shot.CancelledBy = input;   // the unlock opens or not after its commit by this
        }
        return true;   // otherwise swallowed, not queued
    }

    /// <summary>Keep the base look inside a shot's cone while look is free during it.</summary>
    public void ClampLook(ref float yaw, ref float lookPitch)
    {
        if (shots.Count == 0 || Motion <= 0f) return;   // at Camera motion Off look stays free
        var shot = shots[shots.Count - 1];
        if (shot.ending || shot.spec.lookCone <= 0f || shot.spec.target == null) return;   // an ending shot frees look at once
        var forward = shot.spec.target().rotation * Vector3.forward;
        var shotYaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        var shotPitch = -Mathf.Asin(Mathf.Clamp(forward.y, -1f, 1f)) * Mathf.Rad2Deg;
        yaw = shotYaw + Mathf.Clamp(Mathf.DeltaAngle(shotYaw, yaw), -shot.spec.lookCone, shot.spec.lookCone);
        lookPitch = Mathf.Clamp(lookPitch, shotPitch - shot.spec.lookCone, shotPitch + shot.spec.lookCone);
    }

    /// <summary>Drop every shot and layer at once (a run start, a teleport).</summary>
    public void ResetLayers()
    {
        foreach (var shot in shots) { shot.Ended = true; ShotEnded?.Invoke(shot.spec.kind, shot.spec.focus); }
        shots.Clear();
        offsets.Clear();
        fovPushes.Clear();
        fovReleases.Clear();
        kicks.Clear();
        ShotFovDelta = KickPitch = 0f;
        trauma = 0f;
        hudFade = 0f;
        BufferedPush = false;
        MoveLocked = LookLocked = false;
        if (camera != null)
        {
            camera.transform.localPosition = Vector3.zero;
            camera.transform.localRotation = Quaternion.identity;
            authoredFov = BaseFov;
            camera.fieldOfView = FrontRoomsHandheld.AspectFov(BaseFov, camera.aspect);
        }
    }

    /// <summary>Advance the layers and place the render camera. Call after gameplay has read BaseEye this frame.</summary>
    public void Tick(float dt)
    {
        if (camera == null) return;
        var motion = Motion;

        // Shots: blend weights, ends.
        MoveLocked = LookLocked = false;
        var anyHud = false;
        for (var i = shots.Count - 1; i >= 0; i--)
        {
            var shot = shots[i];
            shot.age += dt;
            if (shot.spec.ease == ShotEase.Sine)
            {
                if (TickSine(shot, dt, motion))
                {
                    shots.RemoveAt(i);
                    shot.Ended = true;
                    ShotEnded?.Invoke(shot.spec.kind, shot.spec.focus);
                    continue;
                }
            }
            else if (shot.ending)
            {
                shot.weight = shot.blendOut > 0f ? Mathf.MoveTowards(shot.weight, 0f, dt / shot.blendOut) : 0f;
                if (shot.weight <= 0f)
                {
                    shots.RemoveAt(i);
                    shot.Ended = true;
                    ShotEnded?.Invoke(shot.spec.kind, shot.spec.focus);
                    continue;
                }
            }
            else shot.weight = shot.spec.blendIn > 0f ? Mathf.MoveTowards(shot.weight, 1f, dt / shot.spec.blendIn) : 1f;
            if (shot.spec.ease != ShotEase.Sine) shot.eased = shot.rotEased = Ease(shot.weight);
            if (!shot.ending)
            {
                MoveLocked |= shot.spec.lockMove;
                LookLocked |= shot.spec.lockLook && motion > 0f;
            }
            // O-25 (crosshair grammar §2): control is back the frame a close-up starts home, so its return leg holds
            // no HUD; the HUD is back HudFadeSeconds (0.15 s) after that frame, not after the 0.40 s blend-out (+0.533 s).
            var closeUpHome = shot.ending && shot.spec.kind == ShotKind.Inspect;
            if (!closeUpHome && (shot.spec.hudUntil <= 0f || shot.age < shot.spec.hudUntil)) anyHud = true;
        }
        hudFade = Mathf.MoveTowards(hudFade, anyHud ? 1f : 0f, dt / FrontRoomsShotTimings.HudFadeSeconds);

        // The pose: the base eye, pulled toward the newest shot's target.
        var basePose = BaseEye;
        var position = basePose.position;
        var rotation = basePose.rotation;
        var fov = BaseFov;
        var shotFov = 0f;
        var fovFloor = BaseFov - FrontRoomsShotTimings.MaxFovDeltaDeg;
        if (shots.Count > 0)
        {
            var shot = shots[shots.Count - 1];
            var w = shot.eased;
            // A full-pose shot (C17) is never scaled by Camera motion: at Off it cuts.
            var scale = shot.spec.fullPose ? 1f : motion;
            if (shot.spec.target != null && scale > 0f)
            {
                var target = shot.spec.target();
                position = Vector3.Lerp(position, target.position, w * scale);
                rotation = Quaternion.Slerp(rotation, target.rotation, shot.rotEased * scale);
            }
            shotFov = (shot.spec.fov != null ? shot.spec.fov() : shot.spec.fovDelta) * w * scale;
            fov += shotFov;
            if (shot.spec.kind == ShotKind.Inspect) fovFloor = BaseFov - MaxFovDrop(shot.spec);
        }
        ShotFovDelta = shotFov;

        // Offsets: sine there and back, camera-local.
        var offset = Vector3.zero;
        for (var i = offsets.Count - 1; i >= 0; i--)
        {
            var (local, seconds, age) = offsets[i];
            age += dt;
            if (age >= seconds) { offsets.RemoveAt(i); continue; }
            offsets[i] = (local, seconds, age);
            offset += local * Mathf.Sin(age / seconds * Mathf.PI);
        }
        if (offset.sqrMagnitude > 0f) position += rotation * offset;

        // FOV pushes.
        for (var i = fovPushes.Count - 1; i >= 0; i--)
        {
            var (delta, seconds, age) = fovPushes[i];
            age += dt;
            if (age >= seconds) { fovPushes.RemoveAt(i); continue; }
            fovPushes[i] = (delta, seconds, age);
            fov += delta * Mathf.Sin(age / seconds * Mathf.PI);
        }
        // FOV releases: a held delta easing back to nothing.
        for (var i = fovReleases.Count - 1; i >= 0; i--)
        {
            var (delta, seconds, age) = fovReleases[i];
            age += dt;
            if (age >= seconds) { fovReleases.RemoveAt(i); continue; }
            fovReleases[i] = (delta, seconds, age);
            fov += delta * (1f - Mathf.SmoothStep(0f, 1f, age / seconds));
        }
        fov = Mathf.Clamp(fov, fovFloor, BaseFov + FrontRoomsShotTimings.MaxFovDeltaDeg);

        // Kicks: signed pitch, full at once, falling linearly to nothing.
        var kick = 0f;
        for (var i = kicks.Count - 1; i >= 0; i--)
        {
            var (degrees, seconds, age) = kicks[i];
            if (age >= seconds) { kicks.RemoveAt(i); continue; }
            kick += degrees * (1f - age / seconds);
            kicks[i] = (degrees, seconds, age + dt);
        }
        KickPitch = kick;
        if (kick != 0f) rotation *= Quaternion.Euler(kick, 0f, 0f);

        // Shake: smooth noise on pitch, yaw and roll, falling linearly so it ends at its decay time.
        if (trauma > 0f)
        {
            traumaTime += dt;
            var a = trauma;
            rotation *= Quaternion.Euler(
                (Mathf.PerlinNoise(traumaTime * 18f, 0f) * 2f - 1f) * a,
                (Mathf.PerlinNoise(0f, traumaTime * 18f) * 2f - 1f) * a,
                (Mathf.PerlinNoise(traumaTime * 18f, traumaTime * 18f) * 2f - 1f) * a * .5f);
            trauma = Mathf.MoveTowards(trauma, 0f, traumaPeak * dt / traumaDecay);
        }

        // Never into a wall: from the eye, a short spherecast to where the layers put the camera.
        position = ClampToWorld(basePose.position, position);
        camera.transform.SetPositionAndRotation(position, rotation);
        authoredFov = fov;
        camera.fieldOfView = FrontRoomsHandheld.AspectFov(fov, camera.aspect);
    }

    /// <summary>The furthest point toward <paramref name="to"/> a 0.1 m sphere reaches from <paramref name="from"/>, ignoring the body.</summary>
    public Vector3 ClampToWorld(Vector3 from, Vector3 to)
    {
        // Door vision (I-L3): the leaf being peeked through does not hold the camera off its glass (else 0.175 m, not 0.10 m).
        var peekLite = FrontRoomsInspect.PeekLite;
        var ignore = peekLite != null ? peekLite.LeafCollider : null;
        var d = to - from;
        var distance = d.magnitude;
        if (distance < 1e-4f) return to;
        var dir = d / distance;
        var count = Physics.SphereCastNonAlloc(from, FrontRoomsShotTimings.WorldClampRadius, dir, hits, distance, ~0, QueryTriggerInteraction.Ignore);
        var reach = distance;
        for (var i = 0; i < count; i++)
        {
            var c = hits[i].collider;
            if (c == null || c.transform.IsChildOf(body) || (ignore != null && c == ignore)) continue;
            // A hit at distance 0 is a sphere starting in contact: stay at the eye.
            reach = Mathf.Min(reach, hits[i].distance);
        }
        return from + dir * reach;
    }

    /// <summary>The rig's cubic ease-in-out on 0..1 (shot blends; the door shot's curves use it too).</summary>
    public static float Ease(float t) => t < .5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) * .5f;

    /// <summary>Sine in-out (C17b): peak slope π/2 = 1.57 × the average.</summary>
    public static float SineIn(float w) => .5f - .5f * Mathf.Cos(Mathf.PI * Mathf.Clamp01(w));
    /// <summary>Sine out (C17b): from 1 at w = 1 to 0 at w = 0; fastest at the start of the return, settling at the end.</summary>
    public static float SineOut(float w) => 1f - Mathf.Cos(Mathf.PI * .5f * Mathf.Clamp01(w));

    /// <summary>
    /// The sine-out leg entered smoothly (C17b, verifier 2026-10-08): the elapsed fraction s = 1 - w is warped by a
    /// quadratic onset over the first k of the leg (u = s²/2k, then s - k/2, normalised), so the speed ramps from 0 over
    /// k instead of starting at its peak. C1 at k; the leg still ends at w = 0. k = 0 is plain SineOut.
    /// </summary>
    public static float SineOutOnset(float w, float k)
    {
        k = Mathf.Clamp(k, 0f, .5f);
        if (k <= 0f) return SineOut(w);
        var s = 1f - Mathf.Clamp01(w);
        var u = (s < k ? s * s / (2f * k) : s - k * .5f) / (1f - k * .5f);
        return SineOut(1f - u);
    }

    /// <summary>How far below BaseFov a shot may narrow: MaxFovDeltaDeg, or down to fovMin for ShotKind.Inspect (C17c).</summary>
    float MaxFovDrop(ShotSpec spec) =>
        spec.kind == ShotKind.Inspect && spec.fovMin > 0f ? Mathf.Max(FrontRoomsShotTimings.MaxFovDeltaDeg, BaseFov - spec.fovMin) : FrontRoomsShotTimings.MaxFovDeltaDeg;

    /// <summary>A blend time at this Camera motion for a full-pose shot (C17): ×1.5 at 0 &lt; motion &lt; 1, 0 (a cut) at Off.</summary>
    static float Blend(ShotHandle shot, float seconds, float motion)
    {
        if (!shot.spec.fullPose) return seconds;
        if (motion <= 0f) return 0f;
        return motion < 1f ? seconds * FrontRoomsShotTimings.Inspect.HalfMotionTimeScale : seconds;
    }

    /// <summary>One Sine shot's weights (C17b). True when it has ended.</summary>
    static bool TickSine(ShotHandle shot, float dt, float motion)
    {
        if (!shot.ending)
        {
            var blendIn = Blend(shot, shot.spec.blendIn, motion);
            shot.weight = blendIn > 0f ? Mathf.MoveTowards(shot.weight, 1f, dt / blendIn) : 1f;
            shot.rotWeight = shot.weight;
            shot.eased = shot.rotEased = SineIn(shot.weight);
            return false;
        }
        if (!shot.outStarted)
        {
            // The return starts from the pulls the camera has now (an early exit returns from where it is).
            shot.outStarted = true;
            shot.outPull = shot.eased;
            shot.outRotPull = shot.rotEased;
            shot.weight = shot.rotWeight = 1f;
        }
        var outPos = Blend(shot, shot.blendOut, motion);
        var outRot = Blend(shot, shot.rotBlendOut > 0f ? shot.rotBlendOut : shot.blendOut, motion);
        shot.weight = outPos > 0f ? Mathf.MoveTowards(shot.weight, 0f, dt / outPos) : 0f;
        shot.rotWeight = outRot > 0f ? Mathf.MoveTowards(shot.rotWeight, 0f, dt / outRot) : 0f;
        var onset = FrontRoomsShotTimings.Inspect.OutOnset;
        shot.eased = shot.outPull * SineOutOnset(shot.weight, outPos > 0f ? onset / outPos : 0f);
        shot.rotEased = shot.outRotPull * SineOutOnset(shot.rotWeight, outRot > 0f ? onset / outRot : 0f);
        return shot.weight <= 0f && shot.rotWeight <= 0f;
    }
}
