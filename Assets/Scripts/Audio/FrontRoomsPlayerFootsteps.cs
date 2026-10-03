using System;
using UnityEngine;

namespace FrontRooms.Audio
{
    /// <summary>
    /// Player foot contacts from distance travelled, not a timer: the cadence
    /// follows the real speed (wall slides, stamina drop, stopping). Sprinting
    /// and stamina are read from the game's own rule (FrontRooms3DGame.PlayerSprinting /
    /// PlayerStamina01 / PlayerWinded), so the breath and the run gait never drift from it.
    /// Shoes carry wetness: after a soaked patch the damp layers fade over
    /// ~30 s instead of switching off at the zone border.
    /// </summary>
    public sealed class FrontRoomsPlayerFootsteps : MonoBehaviour
    {
        const float WalkSpeed = 3.2f, RunSpeed = 5.5f;
        const float WalkStride = 1.45f, RunStride = 1.72f;
        const float DefaultDampness = .4f, ShoeDryTime = 30f;

        public Func<Vector3, SoundIds.Surface> surfaceAt;
        public Func<Vector3, float> dampnessAt;

        CharacterController body;
        Vector3 last;
        float distance, speed, shoe;
        bool wasMoving;

        public float Speed => speed;
        public float Stamina01 => FrontRooms3DGame.PlayerStamina01;
        public bool Winded => FrontRooms3DGame.PlayerWinded;
        public bool Sprinting => FrontRooms3DGame.PlayerSprinting;

        void Awake()
        {
            body = GetComponent<CharacterController>();
            last = transform.position;
        }

        void Update()
        {
            var dt = Time.deltaTime;
            if (dt <= 0f) return;
            var p = transform.position;
            var delta = p - last;
            delta.y = 0f;
            last = p;
            var step = delta.magnitude;
            if (step > 2f) { distance = 0f; return; }                // respawn / teleport

            speed = Mathf.Lerp(speed, step / dt, 1f - Mathf.Exp(-dt * 12f));
            shoe *= Mathf.Exp(-dt / ShoeDryTime);

            var grounded = body == null || body.isGrounded;
            var moving = grounded && speed > .4f;
            if (moving)
            {
                var stride = Mathf.Lerp(WalkStride, RunStride, Mathf.InverseLerp(WalkSpeed, RunSpeed, speed));
                if (!wasMoving) distance = stride * .6f;              // the first step lands soon after starting
                distance += step;
                if (distance >= stride)
                {
                    distance -= stride;
                    Contact(Sprinting ? SoundIds.Gait.Run : SoundIds.Gait.Walk);
                }
                wasMoving = true;
            }
            else if (wasMoving && speed < .25f)
            {
                Contact(SoundIds.Gait.Stop);
                wasMoving = false;
                distance = 0f;
            }
        }

        Vector3 Feet()
        {
            if (body == null) return transform.position;
            return transform.TransformPoint(body.center) - Vector3.up * (body.height * .5f);
        }

        void Contact(SoundIds.Gait gait)
        {
            var feet = Feet();
            var surface = surfaceAt != null ? surfaceAt(feet) : SoundIds.Surface.Carpet;
            shoe = Mathf.Max(shoe, dampnessAt != null ? dampnessAt(feet) : DefaultDampness);
            FrontRoomsFmod.OneShot(SoundIds.Footstep, feet,
                SoundIds.Param.Surface, (float)surface, SoundIds.Param.Gait, (float)gait, SoundIds.Param.Dampness, shoe);
        }
    }
}
