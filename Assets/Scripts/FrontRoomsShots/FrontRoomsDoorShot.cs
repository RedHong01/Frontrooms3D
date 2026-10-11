using System;
using FrontRooms.Map;
using UnityEngine;
using KP = FrontRoomsShotTimings.KeyPickup;
using U = FrontRoomsShotTimings.Unlock;

/// <summary>A beat of a door shot, for listeners (the sound chat's one-shots and snapshot, the visual chat's glints and post).</summary>
public enum DoorBeat { Pocket, KeyApproach, KeyInsert, PinCatch, KeyTurn, BoltRetract, KeyWithdraw, KnobTurn, KnobReturn, Return, Open }

/// <summary>
/// The door shots on the camera rig (the door-shot plan, door_shots_plan.md §3.2; the audit's §3.2, camera
/// language B: the camera moves and the objects act, no hands). This is the unlock: Red's head dip.
///
/// E on a locked door with its key (FrontRoomsMapWorld.Use answering KeyTurn) holds the door and starts the
/// shot. The camera dips from the eye to pose P, fixed in the world at E-down: 0.45 m out from the keyhole
/// (flat), 0.25 m below the eye, yawed 12° toward the hinge so a key would read at insertion, looking at
/// the keyhole (0.58 m, 39° down), with the FOV 14° narrower. The key turns on the shot's clock and the
/// door commits at 0.86 s (DoorUnlocked, the bolt's jolt); a push-side door's knob turns and the leaf leaves
/// the frame at 1.00 s, away from the camera; the camera returns over 1.15–1.55 s, and a pull-side door
/// opens normally at 1.55 s, once the camera is home (the leaf never moves toward the camera in the shot).
///
/// The driver owns the curves (the rig holds weight 1), the locks (move until the commit, and to 1.45 s
/// while there is a picture to protect; look to 1.30 s, back by 1.50 s) and the cancel policy: S or a
/// second E after 0.2 s, the Relay (a chase, or its sight within 6 m before the commit; a chase or any
/// sight after it), or the door itself (broken, being broken, rebuilt, its hold gone). A cancel before the
/// commit leaves the door locked; after it, unlocked and shut, except a second E, which opens it. The
/// FOV always takes 0.4 s to come back (rig.ReleaseFov). Camera motion scales the dip (the rig); at Off,
/// or started in place (chunks still building, a climb), it plays in place with the same beats and only
/// the gameplay move lock (to the commit).
///
/// Built like FrontRoomsGlassShot: a plain class ticked by the game in Phase.Playing only, so Esc
/// freezes it, with a static Weight and Beat for the post and the sound. The lock's moving parts go
/// through the visual chat's rig (FrontRoomsKitSeam) when the door has one; without one the beats
/// still fire and the keyhole comes from the spec's numbers on the cube leaf. Gameplay never reads
/// this camera: only BaseEye.
///
/// It also presents a picked-up key (plan §3.3; the audit's 3.1), as the map's KeyPresenter, and only
/// when there is a model to show: the key's IFrontRoomsKeyRig (the facade's dressed key) or the dev
/// placeholder (FrontRoomsMapWorld.ShowPlaceholderKey). No rig shot, no lock, no HUD fade: the key lifts
/// to a point low and right in the rendered view (0–0.25 s), KeyTaken at its arrival, held to 0.75 s,
/// into the pocket by 0.95 s (Beat Pocket at 0.85), with a small aim assist toward a key in view.
/// Without a model it declines, and the map keeps today's instant pickup.
/// </summary>
public sealed class FrontRoomsDoorShot
{
    /// <summary>A beat of a door shot landed this frame: the shot, the beat, and where (the keyhole; the key for KeyPickup's Pocket).</summary>
    public static event Action<ShotKind, DoorBeat, Vector3> Beat;

    /// <summary>
    /// How much of the unlock shot is on screen this frame, 0..1: its blend weight × the Camera motion
    /// setting × what is left before its return (1.15–1.55 s); 0 when none runs. For the visual chat's
    /// post stack, as FrontRoomsGlassShot.Weight. Updated in Tick.
    /// </summary>
    public static float Weight { get; private set; }

    // The plug turns back to 0 first (KeyOutStart to here), then the key withdraws by KeyOutEnd.
    const float PlugBackEnd = U.KeyOutStart + .10f;
    // The pin's catch: the insert runs InsertStart..InsertEnd with a PinCatch hold at PinCatchAt of the depth.
    const float PinCatchTime = U.InsertStart + U.PinCatchAt * (U.InsertEnd - U.InsertStart - U.PinCatch);
    // The handheld drift: in over 0.30–0.40 s, out over 0.45–0.55 s (the key's approach).
    const float DriftInStart = .30f, DriftInEnd = .40f, DriftOutStart = .45f, DriftOutEnd = .55f, DriftNoiseHz = 1.3f;
    // The step-in's body clearance from a leaf: the map's own (the player's radius and the controller's skin).
    const float LeafClearance = FrontRoomsMapWorld.PlayerClearance;

    readonly FrontRoomsCameraRig rig;
    readonly FrontRoomsMapWorld map;
    readonly Camera camera;
    readonly Transform body;

    ShotHandle handle;
    FrontRoomsMapWorld.Door door;
    DoorFace face;
    Pose lockPose, poseP;
    Vector3 feetAtBegin;
    bool pull, inPlace, live, committed, opened, cancelled, fovReleased;
    float t, travel, fovIn;
    int landed;
    // What the door's rig was last told, and the ease back to rest after a cancel (-1: none).
    float plugSent, knobSent, latchSent, boltSent;
    float tailClock = -1f, plugFrom, knobFrom, latchFrom, boltFrom, boltTo;

    // The key pickup: the presented key and its zone, its dressed rig (null: the placeholder cube), its own
    // clock, its beats, and where it lay at contact (the travel starts there; the placeholder shrinks from its scale).
    Transform key;
    IFrontRoomsKeyRig keyRig;
    GridCoord keyZone;
    bool presenting, keyArrived, keyPocketed;
    float keyT;
    Vector3 keyFrom, keyFromScale;
    Quaternion keyFromRotation;
    // The aim assist toward the last presented key: the pitch it adds in all (degrees, + = down) and its clock (-1: none).
    float assistDegrees, assistClock = -1f;

    public FrontRoomsDoorShot(FrontRoomsCameraRig rig, FrontRoomsMapWorld map, Camera camera, Transform body)
    {
        this.rig = rig;
        this.map = map;
        this.camera = camera;
        this.body = body;
    }

    static float Motion => FrontRoomsSettings.CameraMotionScale;

    /// <summary>From BeginUnlock until the shot has ended (EndShot at 1.55 s, or the end of a cancel's blend).</summary>
    public bool UnlockActive => handle != null && !handle.Ended;
    /// <summary>The shot is running and not on its way out.</summary>
    public bool Live => live;
    /// <summary>The key has turned (DoorUnlocked raised): the door stays unlocked whatever happens next.</summary>
    public bool Committed => committed;
    /// <summary>The shot's clock (s from E-down); it stops when the shot ends or is cancelled.</summary>
    public float Clock => t;
    /// <summary>The door the running (or last) unlock is on.</summary>
    public FrontRoomsMapWorld.Door Door => door;

    // At Camera motion Off, or started in place, there is no picture to protect: only the gameplay lock.
    bool OffPolicy => Motion <= 0f || inPlace;

    /// <summary>
    /// Movement is held: until the commit in every case (the key is turning: gameplay), then to
    /// Unlock.MoveLockEnd only while there is a picture to protect (not at Camera motion Off, not in
    /// place). Free the frame a cancel lands.
    /// </summary>
    public bool MoveLocked => live && handle != null && !handle.Cancelled && (t < U.Commit || (!OffPolicy && t < U.MoveLockEnd));

    /// <summary>The share of mouse look that reaches the base look: 0 to Unlock.LookLockEnd (dropped, not queued), back to 1 over LookBlendBack; always 1 at Off, in place or once cancelled.</summary>
    public float LookScale => !live || handle == null || handle.Cancelled || OffPolicy ? 1f
        : t < U.LookLockEnd ? 0f : Mathf.Clamp01((t - U.LookLockEnd) / U.LookBlendBack);

    // ------------------------------------------------------------------ pure parts (tests)

    /// <summary>
    /// Pose P (plan §3.2.2): from the keyhole <paramref name="lockPose"/>, out along the flat face normal
    /// <paramref name="normal"/> (toward the opener) yawed Unlock.PoseYawTowardHingeDeg toward the hinge,
    /// Unlock.DipDistanceFromLock flat, at <paramref name="eyeY"/> minus Unlock.DipEyeDrop, looking at the keyhole.
    /// </summary>
    public static Pose UnlockPoseP(Pose lockPose, Vector3 normal, Vector3 towardHinge, float eyeY)
    {
        normal.y = 0f;
        towardHinge.y = 0f;
        var n = normal.sqrMagnitude > 1e-8f ? normal.normalized : Vector3.forward;
        var h = towardHinge.sqrMagnitude > 1e-8f ? towardHinge.normalized : Vector3.zero;
        var yaw = U.PoseYawTowardHingeDeg * Mathf.Deg2Rad;
        var out_ = (n * Mathf.Cos(yaw) + h * Mathf.Sin(yaw)).normalized;
        var l = lockPose.position;
        var position = new Vector3(l.x, 0f, l.z) + out_ * U.DipDistanceFromLock + Vector3.up * (eyeY - U.DipEyeDrop);
        return new Pose(position, Quaternion.LookRotation(l - position, Vector3.up));
    }

    /// <summary>The camera's travel to pose P for a distance (m): TravelBase + TravelPerMetre per metre, held to TravelMin..TravelMax.</summary>
    public static float UnlockTravel(float distance) => Mathf.Clamp(U.TravelBase + U.TravelPerMetre * distance, U.TravelMin, U.TravelMax);

    /// <summary>
    /// The unlock's cancel rule from the Relay (audit §3.2; plan §3.2.7), in one place: before the commit a
    /// chase, or its sight within Unlock.CancelRelayDistance (−1: not out); after it, a chase or any sight.
    /// The game feeds it (FrontRooms3DGame.UnlockThreat); v2's "target acquired / spotting" replaces the inputs there.
    /// </summary>
    public static bool Threatened(bool committed, bool chase, bool sees, float relayDistance) =>
        chase || (sees && (committed || (relayDistance >= 0f && relayDistance <= U.CancelRelayDistance)));

    /// <summary>
    /// The key turn's step-in (plan §3.2.1): where the body would stand, Unlock.StandDistance from the lock
    /// along the face normal (on the pull side, clear of the leaf's sweep). True with <paramref name="stand"/>
    /// only when that is a step toward or along the door within Unlock.StepInMaxForward / StepInMaxLateral and
    /// more than 5 cm; never while a door pull's step is running (it keeps the mover).
    /// </summary>
    public static bool UnlockStepIn(FrontRoomsMapWorld.Door door, Vector3 feet, Vector3 lockPoint, Vector3 normal, bool pull, bool pullStepRunning, out Vector3 stand)
    {
        stand = feet;
        if (door == null || pullStepRunning) return false;
        normal.y = 0f;
        if (normal.sqrMagnitude < 1e-8f) return false;
        var n = normal.normalized;
        var target = new Vector3(lockPoint.x, feet.y, lockPoint.z) + n * U.StandDistance;
        if (pull) target = FrontRoomsMapWorld.ClearOfSwing(door, target, LeafClearance);
        var d = target - feet;
        d.y = 0f;
        // In the door's frame: forward is toward the door, lateral along it. A target the caps would move is out of reach: no step.
        var forward = Vector3.Dot(d, -n);
        var lateral = Vector3.Dot(d, Vector3.Cross(Vector3.up, n));
        if (forward < -1e-4f || forward > U.StepInMaxForward + 1e-4f || Mathf.Abs(lateral) > U.StepInMaxLateral + 1e-4f) return false;
        if (d.magnitude <= .05f) return false;
        stand = target;
        return true;
    }

    /// <summary>The plug's (and key's) turn at shot time <paramref name="time"/>, degrees: slow through the first 10° (u²), then easing out to 90° by TurnEnd; back to 0 by PlugBackEnd.</summary>
    public static float PlugDegrees(float time)
    {
        if (time <= U.TurnStart) return 0f;
        if (time < U.TurnEnd)
        {
            var s = (time - U.TurnStart) / (U.TurnEnd - U.TurnStart);
            if (s < U.TurnResistShare)
            {
                var r = s / U.TurnResistShare;
                return U.TurnResistDeg * r * r;
            }
            var e = (s - U.TurnResistShare) / (1f - U.TurnResistShare);
            return U.TurnResistDeg + (U.TurnDeg - U.TurnResistDeg) * (1f - (1f - e) * (1f - e));
        }
        if (time < U.KeyOutStart) return U.TurnDeg;
        return U.TurnDeg * (1f - Mathf.SmoothStep(0f, 1f, (time - U.KeyOutStart) / (PlugBackEnd - U.KeyOutStart)));
    }

    /// <summary>The knob on the opener's face (push side only), degrees: 0 → KnobDeg over KnobTurnStart..End, held, back over KnobReturnStart..End.</summary>
    public static float KnobDegrees(float time)
    {
        if (time <= U.KnobTurnStart) return 0f;
        if (time < U.KnobReturnStart) return U.KnobDeg * Mathf.SmoothStep(0f, 1f, (time - U.KnobTurnStart) / (U.KnobTurnEnd - U.KnobTurnStart));
        return U.KnobDeg * (1f - Mathf.SmoothStep(0f, 1f, (time - U.KnobReturnStart) / (U.KnobReturnEnd - U.KnobReturnStart)));
    }

    // The return: 0 until ReturnStart, 1 at ReturnEnd (the rig's cubic ease).
    static float Ret(float time) => FrontRoomsCameraRig.Ease(Mathf.Clamp01((time - U.ReturnStart) / (U.ReturnEnd - U.ReturnStart)));

    // ------------------------------------------------------------------ the unlock

    /// <summary>
    /// E on a keyable door was answered with a key turn: start the shot. <paramref name="lockPose"/> is the
    /// keyhole on the opener's face (FrontRoomsMapWorld.LockPose), <paramref name="normal"/> its flat face
    /// normal toward the opener, <paramref name="pull"/> true on the swing side (face S).
    /// <paramref name="inPlace"/>: no pose and no FOV (chunks still building, a climb), with the Off locks.
    /// </summary>
    public void BeginUnlock(FrontRoomsMapWorld.Door door, DoorFace face, Pose lockPose, Vector3 normal, bool pull, bool inPlace)
    {
        if (door == null || rig == null || map == null) return;
        // A key still being presented is finished at once (its KeyTaken, then gone): the unlock has the frame.
        FinishKey();
        // One unlock at a time: a new one resolves the old (its door keeps whatever its key did), and puts
        // the old door's parts to rest if a cancel was still easing them back. The new door's parts start at rest.
        if (live) Abort();
        else if (tailClock >= 0f) SnapHardwareToRest();
        if (handle != null && !handle.Ended) rig.CancelShot(handle, 0f);
        this.door = door;
        this.face = face;
        this.lockPose = lockPose;
        this.pull = pull;
        this.inPlace = inPlace;
        feetAtBegin = body != null ? body.position : lockPose.position;
        var eye = rig.BaseEye;
        poseP = UnlockPoseP(lockPose, normal, FrontRoomsMapWorld.TowardHinge(door), eye.position.y);
        travel = UnlockTravel((poseP.position - eye.position).magnitude);
        fovIn = Mathf.Max(travel, U.FovSeconds);
        t = 0f;
        landed = 0;
        live = true;
        committed = opened = cancelled = fovReleased = false;
        plugSent = knobSent = latchSent = boltSent = 0f;
        tailClock = -1f;
        handle = rig.BeginShot(new ShotSpec
        {
            kind = ShotKind.Unlock, focus = lockPose.position,
            // In place: no pose and no FOV, the rig plays it where the camera is.
            target = inPlace ? null : (Func<Pose>)UnlockPose,
            fov = inPlace ? null : (Func<float>)UnlockFov,
            blendIn = 0f,                        // the driver's curves bring the camera in
            blendOut = U.CancelCameraBack,       // a player cancel's way back; the driver's own cancels pick theirs
            lockMove = false, lockLook = false,  // the driver owns the locks (they outlast the camera)
            cancel = ShotCancel.PlayerInput,     // no Relay flag: the driver applies the 6 m and commit rules
            duration = U.Duration,               // the buffered push window, 1.35–1.55 s
            hudUntil = U.MoveLockEnd,            // the HUD comes back with movement
        });
        Weight = handle.weight * Motion;
    }

    /// <summary>
    /// Advance the shot one frame (Phase.Playing only). <paramref name="chase"/>: the Relay chases;
    /// <paramref name="sees"/>: it sees the player; <paramref name="relayDistance"/>: how far it is (−1:
    /// not out). The driver reads the door's state itself, and refreshes the map's key-turn hold. A
    /// presented key's clock and beats advance here too (PlaceKey puts it on the rendered camera).
    /// </summary>
    public void Tick(float dt, bool chase, bool sees, float relayDistance)
    {
        TickTail(dt);
        TickKey(dt);
        if (!live)
        {
            UpdateWeight();
            return;
        }
        // Run start, teleport: the rig dropped the shot.
        if (handle == null || handle.Ended)
        {
            Abort();
            return;
        }
        t += dt;
        map.RefreshKeyTurn(door);

        // A cancel through the rig (S, or a second E after 0.2 s): what it does to the door depends on the commit and the input.
        if (handle.Cancelled)
        {
            if (!committed) map.CancelKeyTurn(door);
            else if (!opened) map.ReleaseKeyTurn(door, handle.CancelledBy == ShotInput.Use, Feet);
            if (committed && handle.CancelledBy == ShotInput.Use) opened = true;
            Finish(true);
            UpdateWeight();
            return;
        }
        // The Relay: before the commit a chase or sight within 6 m, after it a chase or any sight. Never an open after it.
        if (Threatened(committed, chase, sees, relayDistance))
        {
            CancelHere(committed ? U.ChaseFreeSnap : U.CancelCameraBack);
            UpdateWeight();
            return;
        }
        // The door went from under the shot: broken, being broken, its chunk dropped or rebuilt, or its hold gone (watchdog, ApplyLive).
        if (!opened && DoorLost())
        {
            CancelHere(committed ? U.ChaseFreeSnap : U.CancelCameraBack);
            UpdateWeight();
            return;
        }

        Land(DoorBeat.KeyApproach, U.KeyApproach);
        Land(DoorBeat.KeyInsert, U.InsertStart);
        Land(DoorBeat.PinCatch, PinCatchTime);
        Land(DoorBeat.KeyTurn, U.TurnStart);
        if (!committed && t >= U.Commit)
        {
            committed = true;
            map.CommitUnlock(door);
            rig.AddTrauma(U.BoltJoltDeg, U.BoltJolt);
            Land(DoorBeat.BoltRetract, U.Commit);
        }
        Land(DoorBeat.KeyWithdraw, U.KeyOutStart);
        if (!pull)
        {
            Land(DoorBeat.KnobTurn, U.KnobTurnStart);
            if (!opened && t >= U.KnobTurnEnd)
            {
                // Push side: the knob was the handle; the leaf leaves the frame at once, away from the camera.
                opened = true;
                map.ReleaseKeyTurn(door, true, Feet, true);
                Land(DoorBeat.Open, U.KnobTurnEnd);
            }
        }
        Land(DoorBeat.Return, U.ReturnStart);
        if (!pull) Land(DoorBeat.KnobReturn, U.KnobReturnStart);
        DriveHardware();

        if (t >= U.ReturnEnd)
        {
            // Pull side: the camera is home, so the leaf may come toward it now: a normal open (the
            // handle, then DoorPulled if the body is in the sweep, and the leaf after the pull beat).
            if (!opened)
            {
                opened = true;
                map.ReleaseKeyTurn(door, true, Feet);
                Land(DoorBeat.Open, U.ReturnEnd);
            }
            // The buffered push has no effect while the door opens by itself (Q4's Ajar would use it): read and cleared.
            rig.BufferedPush = false;
            rig.EndShot(handle, 0f);
            Finish(false);
        }
        UpdateWeight();
    }

    /// <summary>
    /// Drop the shot without a camera move of its own (Caught, a run reset, a teleport): the hold is
    /// resolved (before the commit the door stays locked; after it, unlocked and shut), the lock's parts
    /// go to rest, and no later beat fires. The camera stays where it is while the rig is not ticked.
    /// A presented key is finished at once (announced if it had not arrived, then gone).
    /// </summary>
    public void Abort()
    {
        FinishKey();
        if (live)
        {
            if (!committed) map.CancelKeyTurn(door);
            else if (!opened) map.ReleaseKeyTurn(door, false, Feet);
            live = false;
            cancelled = true;
            if (handle != null && !handle.Ended)
            {
                ReleaseFovOnce();
                rig.CancelShot(handle, U.CancelCameraBack);
            }
        }
        SnapHardwareToRest();
        Weight = 0f;
    }

    Vector3 Feet => body != null ? body.position : feetAtBegin;

    bool DoorLost() =>
        door.hinge == null || map.DoorBetween(door.a, door.b) != door || door.broken || map.IsBeingBroken(door) || !map.IsHeldForKey(door);

    // A cancel the driver decides: the camera back over blendOut (the FOV over 0.4 s), the door by the commit rule (no open).
    void CancelHere(float blendOut)
    {
        if (!committed) map.CancelKeyTurn(door);
        else if (!opened) map.ReleaseKeyTurn(door, false, Feet);
        ReleaseFovOnce();
        rig.CancelShot(handle, blendOut);
        Finish(true);
    }

    // The shot stops running. A cancel raises the key's withdraw and the return if they have not landed, and eases the lock's parts to rest.
    void Finish(bool cancel)
    {
        live = false;
        cancelled = cancel;
        if (!cancel) return;
        ReleaseFovOnce();
        Raise(DoorBeat.KeyWithdraw);
        Raise(DoorBeat.Return);
        StartTail();
    }

    void Land(DoorBeat beat, float at)
    {
        if (t + 1e-5f >= at) Raise(beat);
    }

    void Raise(DoorBeat beat)
    {
        var bit = 1 << (int)beat;
        if ((landed & bit) != 0) return;
        landed |= bit;
        Beat?.Invoke(ShotKind.Unlock, beat, lockPose.position);
    }

    void UpdateWeight()
    {
        if (handle == null || handle.Ended) { Weight = 0f; return; }
        Weight = handle.weight * Motion * (1f - Ret(t));
    }

    // The FOV a cancelled shot showed eases back over MinFovChangeSeconds, whatever its pose does.
    void ReleaseFovOnce()
    {
        if (fovReleased) return;
        fovReleased = true;
        if (!inPlace) rig.ReleaseFov(rig.ShotFovDelta, FrontRoomsShotTimings.MinFovChangeSeconds);
    }

    // ------------------------------------------------------------------ the key pickup

    /// <summary>A key of this zone is being presented (from contact to KeyPickup.PocketEnd): the game's key panel waits for it.</summary>
    public bool Presenting(GridCoord zone) => presenting && keyZone == zone;

    /// <summary>The presented key's clock (s from contact), or −1 when none is presented.</summary>
    public float KeyClock => presenting ? keyT : -1f;

    /// <summary>
    /// The map's KeyPresenter (plan §3.3): the player reached a zone key. With a model to show (the key
    /// carries the facade's IFrontRoomsKeyRig, or FrontRoomsMapWorld.ShowPlaceholderKey is on) the driver
    /// takes the key over and answers true. It travels from where it lay to the held point (ease-out over
    /// KeyPickup.TravelToHeld), arrives at KeyPickup.Arrive (map.KeyArrived: KeyTaken; the tag sways), is
    /// held in the rendered view to HoldUntil, drops out of the frame over PocketStart..PocketEnd (Beat
    /// Pocket at PocketSound) and is destroyed at PocketEnd. Without a model it answers false and the map
    /// keeps today's instant pickup. A presentation still running is finished first (a second pickup).
    /// A key within KeyPickup.AimAssistConeDeg of the view arms the aim assist (AimAssistStep).
    /// </summary>
    public bool BeginKeyPickup(GridCoord zone, Transform key)
    {
        FinishKey();
        if (key == null || map == null || rig == null || camera == null) return false;
        var dressed = key.GetComponentInChildren<IFrontRoomsKeyRig>();
        if (dressed == null && !FrontRoomsMapWorld.ShowPlaceholderKey) return false;
        this.key = key;
        keyRig = dressed;
        keyZone = zone;
        presenting = true;
        keyArrived = keyPocketed = false;
        keyT = 0f;
        keyFrom = key.position;
        keyFromRotation = key.rotation;
        keyFromScale = key.localScale;
        // The aim assist: only for a key near the view, never at Camera motion Off.
        var eye = rig.BaseEye;
        var forward = eye.rotation * Vector3.forward;
        var to = keyFrom - eye.position;
        if (Motion > 0f && to.sqrMagnitude > 1e-6f && Vector3.Angle(forward, to) <= KP.AimAssistConeDeg)
        {
            var pitchNow = -Mathf.Asin(Mathf.Clamp(forward.y, -1f, 1f)) * Mathf.Rad2Deg;
            var pitchTo = Mathf.Atan2(-to.y, new Vector2(to.x, to.z).magnitude) * Mathf.Rad2Deg;
            assistDegrees = Mathf.Clamp(pitchTo - pitchNow, -KP.AimAssistPitchDeg, KP.AimAssistPitchDeg) * Motion;
            assistClock = 0f;
        }
        return true;
    }

    /// <summary>
    /// The key pickup's aim assist (plan §3.3): degrees for the game to add to its base pitch this frame
    /// (+ = down), toward a presented key that lay within KeyPickup.AimAssistConeDeg of the view: at most
    /// KeyPickup.AimAssistPitchDeg × Camera motion in all, eased out over KeyPickup.AimAssistSeconds, and
    /// only while the mouse stays idle (<paramref name="mouseIdle"/>): any look ends it. 0 otherwise (no
    /// presented key, one beside or behind, Camera motion Off). No lock, no HUD fade; E stays free.
    /// </summary>
    public float AimAssistStep(float dt, bool mouseIdle)
    {
        if (assistClock < 0f) return 0f;
        if (!mouseIdle)
        {
            assistClock = -1f;
            return 0f;
        }
        var from = EaseOut(assistClock / KP.AimAssistSeconds);
        assistClock = Mathf.Min(assistClock + dt, KP.AimAssistSeconds);
        var step = assistDegrees * (EaseOut(assistClock / KP.AimAssistSeconds) - from);
        if (assistClock >= KP.AimAssistSeconds) assistClock = -1f;
        return step;
    }

    /// <summary>
    /// Put the presented key on the rendered camera (call after the rig's Tick, so it never lags a frame):
    /// on its way from where it lay to the held point, the held point (viewport KeyPickup.HeldViewportX,
    /// HeldViewportY at HeldDistance, kept short of the world by the rig's spherecast), or on its way down
    /// out of the frame. It faces the lens: the dressed key through KeyRig.PoseHeld, the placeholder cube
    /// itself, shrinking to FrontRoomsMapWorld.KeyPlaceholderSize over the travel.
    /// </summary>
    public void PlaceKey()
    {
        if (!presenting) return;
        if (key == null)
        {
            // The object went from under the presentation (the map was torn down): nothing to show.
            FinishKey();
            return;
        }
        var lens = camera.transform;
        var y = keyT < KP.PocketStart ? KP.HeldViewportY
            : Mathf.Lerp(KP.HeldViewportY, KP.PocketViewportY, EaseIn((keyT - KP.PocketStart) / (KP.PocketEnd - KP.PocketStart)));
        var held = rig.ClampToWorld(lens.position, camera.ViewportToWorldPoint(new Vector3(KP.HeldViewportX, y, KP.HeldDistance)));
        var facing = Quaternion.LookRotation(lens.position - held, lens.up);
        var k = EaseOut(keyT / KP.TravelToHeld);
        var position = Vector3.Lerp(keyFrom, held, k);
        var rotation = Quaternion.Slerp(keyFromRotation, facing, k);
        if (keyRig != null) keyRig.PoseHeld(new Pose(position, rotation));
        else
        {
            key.SetPositionAndRotation(position, rotation);
            key.localScale = Vector3.Lerp(keyFromScale, FrontRoomsMapWorld.KeyPlaceholderSize, k);
        }
    }

    // The presented key's clock and beats: the arrival (KeyTaken), the pocket's beat, the end.
    void TickKey(float dt)
    {
        if (!presenting) return;
        if (key == null)
        {
            FinishKey();
            return;
        }
        keyT += dt;
        if (!keyArrived && keyT + 1e-5f >= KP.Arrive) ArriveKey();
        if (!keyPocketed && keyT + 1e-5f >= KP.PocketSound)
        {
            keyPocketed = true;
            Beat?.Invoke(ShotKind.KeyPickup, DoorBeat.Pocket, key.position);
        }
        if (keyT + 1e-5f >= KP.PocketEnd) FinishKey();
    }

    // The key reaches the held pose: the map announces it (KeyTaken) and the tag swings, damped.
    void ArriveKey()
    {
        keyArrived = true;
        if (keyRig != null) keyRig.Sway(KP.TagSwingDeg, KP.TagSwingHz, KP.TagSwingDecay);
        map.KeyArrived(keyZone);
    }

    // The presentation is over (PocketEnd) or cut short (a second pickup, an unlock, Abort): the key is
    // announced if it had not arrived yet (Presenting still true for KeyTaken's listeners), then destroyed.
    void FinishKey()
    {
        assistClock = -1f;
        if (!presenting) return;
        if (!keyArrived) ArriveKey();
        presenting = false;
        if (keyRig != null) keyRig.Release();
        if (key != null)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(key.gameObject);
            else UnityEngine.Object.DestroyImmediate(key.gameObject);
        }
        key = null;
        keyRig = null;
    }

    static float EaseOut(float u)
    {
        u = Mathf.Clamp01(u);
        return 1f - (1f - u) * (1f - u);
    }

    static float EaseIn(float u)
    {
        u = Mathf.Clamp01(u);
        return u * u;
    }

    // ------------------------------------------------------------------ the camera

    Pose UnlockPose()
    {
        var e = rig.BaseEye;
        var k = FrontRoomsCameraRig.Ease(Mathf.Clamp01(t / travel)) * (1f - Ret(t));
        var rotation = poseP.rotation * Quaternion.Euler(DriftAt(t).x, DriftAt(t).y, -RollAt(t));
        return new Pose(Vector3.Lerp(e.position, poseP.position, k), Quaternion.Slerp(e.rotation, rotation, k));
    }

    float UnlockFov()
    {
        // Cancelled (by the rig's Consume this very frame, or by the driver): the delta goes to ReleaseFov.
        if (cancelled || (handle != null && handle.Cancelled))
        {
            ReleaseFovOnce();
            return 0f;
        }
        return U.FovDeltaDeg * FrontRoomsCameraRig.Ease(Mathf.Clamp01(t / fovIn)) * (1f - Ret(t));
    }

    // A tenth of a degree of handheld drift on pitch and yaw while the key approaches.
    static Vector2 DriftAt(float time)
    {
        var fade = Mathf.SmoothStep(0f, 1f, (time - DriftInStart) / (DriftInEnd - DriftInStart)) * (1f - Mathf.SmoothStep(0f, 1f, (time - DriftOutStart) / (DriftOutEnd - DriftOutStart)));
        if (fade <= 0f) return Vector2.zero;
        return new Vector2(Mathf.PerlinNoise(time * DriftNoiseHz, .37f) * 2f - 1f, Mathf.PerlinNoise(.71f, time * DriftNoiseHz) * 2f - 1f) * (U.HandheldDriftDeg * fade);
    }

    // The 0.2° roll follows the key's turn, then eases back by the key's withdraw.
    static float RollAt(float time)
    {
        if (time <= U.TurnStart) return 0f;
        if (time < U.TurnEnd) return U.TurnRollDeg * PlugDegrees(time) / U.TurnDeg;
        return U.TurnRollDeg * (1f - FrontRoomsCameraRig.Ease(Mathf.Clamp01((time - U.TurnEnd) / (U.KeyOutEnd - U.TurnEnd))));
    }

    // ------------------------------------------------------------------ the lock's parts (through the kit seam)

    IFrontRoomsDoorRig DoorRig => door != null && door.hinge != null && map.DoorBetween(door.a, door.b) == door ? door.rig : null;

    void DriveHardware()
    {
        var r = DoorRig;
        if (r == null) return;
        Send(r, 0, PlugDegrees(t), ref plugSent);
        Send(r, 3, committed ? Mathf.Clamp01((t - U.Commit) / U.BoltJolt) : 0f, ref boltSent);
        if (pull) return;
        var knob = KnobDegrees(t);
        Send(r, 1, knob, ref knobSent);
        Send(r, 2, knob / U.KnobDeg, ref latchSent);
    }

    // Tell the rig a part's new value (0 plug, 1 knob, 2 latch, 3 deadbolt) only when it changed.
    void Send(IFrontRoomsDoorRig r, int part, float value, ref float sent)
    {
        if (Mathf.Abs(value - sent) < 1e-4f) return;
        sent = value;
        switch (part)
        {
            case 0: r.SetPlug(face, value); break;
            case 1: r.SetKnob(face, value); break;
            case 2: r.SetLatch(value); break;
            default: r.SetDeadbolt(value); break;
        }
    }

    // After a cancel the key comes out in CancelKeyOut: the plug, knob and latch go back to rest, and a drawn bolt finishes drawing.
    void StartTail()
    {
        plugFrom = plugSent;
        knobFrom = knobSent;
        latchFrom = latchSent;
        boltFrom = boltSent;
        boltTo = committed ? 1f : 0f;
        tailClock = 0f;
    }

    void TickTail(float dt)
    {
        if (tailClock < 0f) return;
        tailClock += dt;
        var r = DoorRig;
        if (r == null) { tailClock = -1f; return; }
        var u = Mathf.SmoothStep(0f, 1f, tailClock / U.CancelKeyOut);
        Send(r, 0, Mathf.Lerp(plugFrom, 0f, u), ref plugSent);
        Send(r, 1, Mathf.Lerp(knobFrom, 0f, u), ref knobSent);
        Send(r, 2, Mathf.Lerp(latchFrom, 0f, u), ref latchSent);
        Send(r, 3, Mathf.Lerp(boltFrom, boltTo, u), ref boltSent);
        if (tailClock >= U.CancelKeyOut) tailClock = -1f;
    }

    // Abort and a new shot: the parts at rest now (a drawn bolt stays drawn: the door is unlocked).
    void SnapHardwareToRest()
    {
        tailClock = -1f;
        var r = DoorRig;
        if (r == null) return;
        Send(r, 0, 0f, ref plugSent);
        Send(r, 1, 0f, ref knobSent);
        Send(r, 2, 0f, ref latchSent);
        if (committed) Send(r, 3, 1f, ref boltSent);
    }
}
