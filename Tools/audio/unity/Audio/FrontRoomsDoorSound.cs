using FMOD.Studio;
using UnityEngine;

namespace FrontRooms.Audio
{
    /// <summary>
    /// Door Foley driven by the hinge's actual rotation, read every frame, so
    /// the sound shares the door's own clock: it starts when the leaf starts
    /// moving and stops when it stops, whatever the trajectory (half open,
    /// re-pushed, slammed, broken open).
    ///
    /// Manual (map doors, wooden, double-acting since A12): the handle plays on
    /// MapWorld.DoorHandleTurned (the director), 0.10 s before the leaf; here the
    /// latch tongue breaks away (Unlatch) as the leaf leaves the frame, a Swing
    /// event follows |angular velocity| while it moves, and the close compound
    /// (LatchStrike) plays when MapWorld.DoorLatched says the leaf landed and
    /// latched (Latched), with its tier from the speed just before contact. A
    /// push-through crosses the closed angle at speed and latches nothing, so it
    /// is silent there. An ease into the open stop is silent too: StopLimit is
    /// for a real impact only (a door driven or broken into the wall). Doors
    /// with no latch hook fall back to the angle: LatchStrike when they come to
    /// rest shut. No closer, no motor, no mid-swing settle (DOOR_FOLEY_SEGMENTS §3.5). The one steel door, the
    /// stairwell's EXIT (C22), is told apart by the director: its crash bar draws the latch (BarPushed, so no Unlatch
    /// here) and its close is the steel ExitClose.
    ///
    /// Stream (the room stream's double doors): a scripted swing, 0 to 88
    /// degrees in 0.9 s on a smoothstep, both leaves together, pushed by
    /// nobody. Each leaf plays one recorded swing for the whole motion:
    /// StreamOpen out of the frame, StreamClose back into it. The left leaf
    /// is the Lead, the right the Follow (other takes, 28 ms later), so a
    /// pair never doubles; the pair's own layers ride on the Lead. The swing
    /// eases to zero speed, so nothing plays at rest, except StreamLock once
    /// the terminal door is shut for good. Snapping shut for recycling is a
    /// jump, not a motion: silent.
    /// </summary>
    public sealed class FrontRoomsDoorSound : MonoBehaviour
    {
        public enum Mode { Manual, Stream }

        const float MoveStart = 6f;        // deg/s to count as moving
        const float MoveStop = 2f;         // deg/s to count as stopped
        const float VelocityFull = 320f;   // deg/s that maps to AngularVelocity 1
        const float ClosedAngle = 1.5f;    // degrees from closed
        const float LockHeight = 1.05f;    // Stream: the lock sits at the meeting stiles, at hand height
        const float ImpactMin = 60f;       // deg/s still moving as the leaf stops at its limit: below this it eased in
        const float ContactAngle = 6f;     // degrees from closed where the speed into the latch is read

        public Mode mode = Mode.Manual;
        public float openLimit = 95f;
        /// <summary>Stream mode, set by the director: the pair's other hinge (may be null) and this leaf's role.</summary>
        public Transform pairedHinge;
        public SoundIds.Leaf leaf;

        Quaternion closed;
        float previousAngle, lastMovingSpeed, stillTime, contactSpeed, latchedAt = -10f, barAt = -10f;
        bool moving, initialised;
        EventInstance swing;
        PARAMETER_ID velocityId, opennessId;
        bool idsReady;
        bool broken;
        float suppressedUntil;

        // Stream mode
        Vector3 leafCentre;                // hinge-local centre of the leaf, measured shut
        FrontRoomsRoomStream stream;
        bool closing;

        static readonly System.Collections.Generic.List<FrontRoomsDoorSound> active = new System.Collections.Generic.List<FrontRoomsDoorSound>();
        const float DoorReach = 1.6f;      // a door's hinges sit within this of the position MapWorld reports for it

        /// <summary>The door at this position was broken down: its leaves no longer make door Foley.</summary>
        public static void MarkBroken(Vector3 doorPosition)
        {
            foreach (var d in active)
                if ((d.transform.position - doorPosition).sqrMagnitude < DoorReach * DoorReach) { d.broken = true; d.Silence(); }
        }

        /// <summary>The door at this position is being struck or rattled: ignore its leaf motion for a while.</summary>
        public static void Suppress(Vector3 doorPosition, float seconds)
        {
            foreach (var d in active)
                if ((d.transform.position - doorPosition).sqrMagnitude < DoorReach * DoorReach)
                {
                    d.suppressedUntil = Mathf.Max(d.suppressedUntil, Time.time + seconds);
                    d.Silence();
                }
        }

        /// <summary>
        /// A shut leaf landed in its frame and latched (MapWorld.DoorLatched, the stairwell's NorthDoorLatched): the
        /// close compound at the door (LatchStrike; the steel EXIT door's ExitClose), its tier from the nearest leaf's
        /// speed into the frame. That leaf's own rest then plays nothing.
        /// </summary>
        public static void Latched(Vector3 doorPosition, string closeEvent = SoundIds.DoorLatchStrike)
        {
            var best = NearestLeaf(doorPosition);
            var impact = best != null ? Mathf.Clamp01(best.contactSpeed / VelocityFull) : .1f;
            if (best != null) best.latchedAt = Time.time;
            FrontRoomsFmod.OneShot(closeEvent, doorPosition, SoundIds.Param.Impact, impact);
        }

        /// <summary>The steel EXIT door's crash bar drew the latch (the director, on DoorHandleTurned): no breakaway follows.</summary>
        public static void BarPushed(Vector3 doorPosition)
        {
            var best = NearestLeaf(doorPosition);
            if (best != null) best.barAt = Time.time;
        }

        static FrontRoomsDoorSound NearestLeaf(Vector3 doorPosition)
        {
            FrontRoomsDoorSound best = null;
            var bestDistance = DoorReach * DoorReach;
            foreach (var d in active)
            {
                if (d.mode != Mode.Manual || d.broken) continue;
                var distance = (d.transform.position - doorPosition).sqrMagnitude;
                if (distance < bestDistance) { best = d; bestDistance = distance; }
            }
            return best;
        }

        void Silence()
        {
            moving = false;
            FrontRoomsFmod.Stop(ref swing, true);
        }

        void OnEnable() => active.Add(this);

        public float Openness => Mathf.Clamp01(previousAngle / openLimit);

        void Start()
        {
            closed = transform.localRotation;
            previousAngle = 0f;
            if (mode == Mode.Stream)
            {
                leafCentre = LeafCentre();
                stream = GetComponentInParent<FrontRoomsRoomStream>();
            }
            initialised = true;
        }

        void LateUpdate()
        {
            if (!initialised) return;
            var dt = Time.deltaTime;
            if (dt <= 0f) return;
            var angle = Quaternion.Angle(closed, transform.localRotation);
            if (Mathf.Abs(angle - previousAngle) > 45f)
            {
                // A pooled room was recycled and its leaves snapped back: not a motion, no sound.
                // A recycled door is a new, intact door.
                broken = false;
                Silence();
                previousAngle = angle;
                return;
            }
            if (broken || Time.time < suppressedUntil)
            {
                previousAngle = angle;
                return;
            }
            var speed = Mathf.Abs(angle - previousAngle) / dt;

            if (!moving && speed > MoveStart)
                BeginMotion(angle);
            if (moving)
            {
                if (speed > MoveStop)
                {
                    stillTime = 0f;
                    lastMovingSpeed = speed;
                    if (angle < ContactAngle && angle < previousAngle) contactSpeed = speed;   // closing into the frame
                    UpdateSwing(angle, speed);
                }
                else if ((stillTime += dt) > .05f)
                {
                    EndMotion(angle);
                }
            }
            previousAngle = angle;
        }

        void BeginMotion(float angle)
        {
            moving = true;
            stillTime = 0f;
            if (mode == Mode.Stream)
            {
                BeginStreamSwing(angle);
                return;
            }
            var p = transform.position;
            contactSpeed = 0f;
            // Out of the frame: the tongue breaks away. The handle came 0.10 s earlier on DoorHandleTurned (none when a
            // key unlock drew the latch: its knob beat was the handle; none after a crash bar, which drew it).
            if (previousAngle < ClosedAngle && Time.time - barAt > .5f) FrontRoomsFmod.OneShot(SoundIds.DoorUnlatch, p);
            if (!idsReady && FrontRoomsFmod.Ready)
            {
                velocityId = FrontRoomsFmod.ParameterId(SoundIds.DoorSwing, SoundIds.Param.AngularVelocity);
                opennessId = FrontRoomsFmod.ParameterId(SoundIds.DoorSwing, SoundIds.Param.Openness);
                idsReady = true;
            }
            swing = FrontRoomsFmod.Create(SoundIds.DoorSwing, p);
            if (swing.isValid()) swing.start();
        }

        void UpdateSwing(float angle, float speed)
        {
            if (!swing.isValid()) return;
            swing.setParameterByID(velocityId, Mathf.Clamp01(speed / VelocityFull));
            swing.setParameterByID(opennessId, Mathf.Clamp01(angle / openLimit));
            FrontRoomsFmod.Move(swing, transform.position);
        }

        void EndMotion(float angle)
        {
            moving = false;
            if (mode == Mode.Stream)
            {
                EndStreamSwing(angle);
                return;
            }
            FrontRoomsFmod.Stop(ref swing);
            var p = transform.position;
            if (angle >= openLimit - 3f)
            {
                // Only a real hit: a leaf that eased into its stop (every A12 open) arrives at near zero speed.
                if (lastMovingSpeed > ImpactMin)
                    FrontRoomsFmod.OneShot(SoundIds.DoorStopLimit, p, SoundIds.Param.Impact, Mathf.Clamp01(lastMovingSpeed / VelocityFull));
            }
            else if (angle <= ClosedAngle && Time.time - latchedAt > .5f)
            {
                // No latch hook for this door (or it never latched): the rest in the frame is the close.
                FrontRoomsFmod.OneShot(SoundIds.DoorLatchStrike, p, SoundIds.Param.Impact, Mathf.Clamp01(contactSpeed / VelocityFull));
            }
            // Coming to rest mid-swing makes no sound (the old settle clicks were handle takes).
        }

        // ------------------------------------------------------------ stream doors
        /// <summary>
        /// One recorded swing per leaf covers the whole scripted motion, so no Swing loop and no
        /// operator. The direction picks the take; the event gives Lead and Follow disjoint takes
        /// and starts the Follow 28 ms later.
        /// </summary>
        void BeginStreamSwing(float angle)
        {
            closing = angle < previousAngle;
            FrontRoomsFmod.OneShot(closing ? SoundIds.DoorStreamClose : SoundIds.DoorStreamOpen,
                transform.TransformPoint(leafCentre), SoundIds.Param.Leaf, (float)leaf);
        }

        /// <summary>
        /// The scripted swing eases to zero speed: no stop, settle or latch hit (StreamClose carries
        /// its own seat on the stop). The one rest that is heard on its own is the terminal door
        /// shut for good behind the player, once per pair, from the Lead.
        /// </summary>
        void EndStreamSwing(float angle)
        {
            if (!closing || angle > ClosedAngle || leaf != SoundIds.Leaf.Lead) return;
            if (stream == null || !stream.TerminalDoorShut) return;
            var centre = pairedHinge != null ? (transform.position + pairedHinge.position) * .5f : transform.position;
            FrontRoomsFmod.OneShot(SoundIds.DoorStreamLock, centre + Vector3.up * LockHeight);
        }

        /// <summary>The leaf is the largest renderer under the hinge (the pull bars are small).</summary>
        Vector3 LeafCentre()
        {
            var best = 0f;
            var centre = Vector3.up * 1.2f;
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                var size = r.bounds.size;
                var volume = size.x * size.y * size.z;
                if (volume <= best) continue;
                best = volume;
                centre = transform.InverseTransformPoint(r.bounds.center);
            }
            return centre;
        }

        void OnDisable()
        {
            active.Remove(this);
            Silence();
        }
    }
}
