using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>What a shot is, for listeners (the sound chat's snapshots, the visual chat's post).</summary>
public enum ShotKind { KeyPickup, Unlock, Open, Rattle, Glass, Climb, DoorBreak, Caught }

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
    public bool lockMove, lockLook;
    /// <summary>While look is not locked, how far it may wander from the shot's heading (degrees; 0 = free).</summary>
    public float lookCone;
    public ShotCancel cancel;
    /// <summary>The shot's length in seconds, for the push buffer of door shots (0 = open-ended: no buffer).</summary>
    public float duration;
}

/// <summary>A running shot, to end or cancel it.</summary>
public sealed class ShotHandle
{
    internal ShotSpec spec;
    internal float weight, age;
    internal bool ending;
    internal float blendOut;
    public ShotKind Kind => spec.kind;
    public bool Ended { get; internal set; }
    public bool Cancelled { get; internal set; }
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
    float trauma, traumaPeak, traumaDecay, traumaTime, hudFade, pitch, stanceDrop;
    readonly RaycastHit[] hits = new RaycastHit[8];

    FrontRoomsCameraRig(Transform body, Transform eye, Camera camera, float eyeHeight)
    {
        this.body = body;
        this.eye = eye;
        this.camera = camera;
        this.eyeHeight = eyeHeight;
        BaseFov = camera != null ? camera.fieldOfView : 76f;
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

    /// <summary>0..1: how far the HUD (but not captions) is faded for a takeover.</summary>
    public float HudFade => hudFade;

    /// <summary>A "push open" buffered by one E in the last moments of a door shot (read and cleared by the owner).</summary>
    public bool BufferedPush { get; set; }

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
        spec.fovDelta = Mathf.Clamp(spec.fovDelta, -FrontRoomsShotTimings.MaxFovDeltaDeg, FrontRoomsShotTimings.MaxFovDeltaDeg);
        var handle = new ShotHandle { spec = spec, blendOut = spec.blendOut };
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

    /// <summary>End a shot early, blending back over <paramref name="blendOut"/> seconds.</summary>
    public void CancelShot(ShotHandle handle, float blendOut = .2f)
    {
        if (handle == null || handle.ending) return;
        handle.Cancelled = true;
        handle.ending = true;
        handle.blendOut = Mathf.Min(handle.blendOut > 0f ? handle.blendOut : blendOut, blendOut);
    }

    /// <summary>Cancel every shot that the Relay may interrupt (it chases or sees the player), fast.</summary>
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
            CancelShot(shot, FrontRoomsShotTimings.Unlock.CancelCameraBack);
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
        trauma = 0f;
        hudFade = 0f;
        BufferedPush = false;
        MoveLocked = LookLocked = false;
        if (camera != null)
        {
            camera.transform.localPosition = Vector3.zero;
            camera.transform.localRotation = Quaternion.identity;
            camera.fieldOfView = BaseFov;
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
            if (shot.ending)
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
            if (!shot.ending)
            {
                MoveLocked |= shot.spec.lockMove;
                LookLocked |= shot.spec.lockLook && motion > 0f;
            }
            anyHud = true;
        }
        hudFade = Mathf.MoveTowards(hudFade, anyHud ? 1f : 0f, dt / FrontRoomsShotTimings.HudFadeSeconds);

        // The pose: the base eye, pulled toward the newest shot's target.
        var basePose = BaseEye;
        var position = basePose.position;
        var rotation = basePose.rotation;
        var fov = BaseFov;
        if (shots.Count > 0)
        {
            var shot = shots[shots.Count - 1];
            var w = Ease(shot.weight);
            if (shot.spec.target != null && motion > 0f)
            {
                var target = shot.spec.target();
                var pull = w * motion;
                position = Vector3.Lerp(position, target.position, pull);
                rotation = Quaternion.Slerp(rotation, target.rotation, pull);
            }
            fov += shot.spec.fovDelta * w * motion;
        }

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
        fov = Mathf.Clamp(fov, BaseFov - FrontRoomsShotTimings.MaxFovDeltaDeg, BaseFov + FrontRoomsShotTimings.MaxFovDeltaDeg);

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
        camera.fieldOfView = fov;
    }

    /// <summary>The furthest point toward <paramref name="to"/> a 0.1 m sphere reaches from <paramref name="from"/>, ignoring the body.</summary>
    public Vector3 ClampToWorld(Vector3 from, Vector3 to)
    {
        var d = to - from;
        var distance = d.magnitude;
        if (distance < 1e-4f) return to;
        var dir = d / distance;
        var count = Physics.SphereCastNonAlloc(from, FrontRoomsShotTimings.WorldClampRadius, dir, hits, distance, ~0, QueryTriggerInteraction.Ignore);
        var reach = distance;
        for (var i = 0; i < count; i++)
        {
            var c = hits[i].collider;
            if (c == null || c.transform.IsChildOf(body)) continue;
            // A hit at distance 0 is a sphere starting in contact: stay at the eye.
            reach = Mathf.Min(reach, hits[i].distance);
        }
        return from + dir * reach;
    }

    static float Ease(float t) => t < .5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) * .5f;
}
