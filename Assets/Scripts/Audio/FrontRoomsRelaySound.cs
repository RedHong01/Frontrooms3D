using FMOD.Studio;
using UnityEngine;

namespace FrontRooms.Audio
{
    /// <summary>
    /// The Relay's body sound. Footsteps come from the rig's own Step event (a foot
    /// plant from the gait phase), so the footfall can never drift from the
    /// animation.
    /// W2 (RELAY §9.3, C10): its steps are heard only at warning stage 2 or while the
    /// player sees it, from its real position. The level comes from the path the sound
    /// travels (the hunter's path distance F), never from the straight line through
    /// walls, and the shut openings on that path muffle it (low-pass only), plus a
    /// distance low-pass, so the first steps read muffled.
    /// Its low sound (C30, FrontRoomsSoundDirector.HumByStage): on, the Relay/Hum event
    /// follows the same rules as the steps; off, the v1 Relay/Presence drone plays
    /// whenever it is released, from straight-line proximity and the walls between.
    /// </summary>
    public sealed class FrontRoomsRelaySound : MonoBehaviour
    {
        public FrontRoomsMapHunter hunter;
        public FrontRoomsMapWorld map;
        public Transform listener;
        public System.Func<Vector3, float> dampnessAt;

        // RELAY §9.3: gain 0.55 + 0.45 (1 - F/18) in stage 2 (a floor, so the first steps are heard), fading to 0
        // across the 18-24 m exit band; in sight it falls off as 18/F past 18 m instead.
        const float NearBand = 18f, ExitBand = 24f, GainFloor = .55f;

        FrontRoomsRelayRig rig;
        Vector3 lastPosition;
        float speed, muffle, gain, humGain, humMuffle, wallOcclusion, nextOcclusionCheck, liteOcclusion, nextLiteCheck;
        const float ShutOpeningCost = 9f, PeekEar = .15f;   // FrontRoomsMapPathField's cost of one shut opening; muffle taken off during a peek
        bool audible;
        readonly RaycastHit[] hits = new RaycastHit[12];
        EventInstance presence;
        PARAMETER_ID occlusionId, gainId, proximityId;
        bool idsReady;

        /// <summary>The steps' muffle passed to FMOD as Occlusion (0 clear .. 0.85 two or more shut openings).</summary>
        public float Occlusion => muffle;
        public float Speed => speed;
        /// <summary>The level from the path, 0..1 (FMOD PathGain).</summary>
        public float PathGain => gain;
        /// <summary>Heard at all: warning stage 2, in the player's sight, or walking its L11 beat at the stairwell door.</summary>
        public bool Audible => audible;

        /// <summary>The listener is in the stairwell (the map's exit area X): the safe zone (Red, 2026-10-22).</summary>
        bool ListenerInSafeZone => map != null && listener != null && map.InExitArea(map.CellOf(listener.position));

        void Awake()
        {
            rig = GetComponent<FrontRoomsRelayRig>();
            lastPosition = transform.position;
        }

        void OnEnable()
        {
            if (rig != null) rig.Step += OnStep;
            // The rig is off while the Relay is Dormant, and C22 parks it at every floor change: what it heard on the
            // old floor goes, so no step plays at the old level before UpdateHearing runs at the new release point.
            lastPosition = transform.position;
            speed = 0f;
            audible = false;
            gain = 0f;
        }

        void OnDisable()
        {
            if (rig != null) rig.Step -= OnStep;
            FrontRoomsFmod.Stop(ref presence, true);
        }

        void Update()
        {
            var dt = Time.deltaTime;
            if (dt <= 0f) return;
            var p = transform.position;
            var delta = p - lastPosition;
            delta.y = 0f;
            lastPosition = p;
            if (delta.magnitude > 3f) return;                         // relayed / teleported closer
            speed = Mathf.Lerp(speed, delta.magnitude / dt, 1f - Mathf.Exp(-dt * 10f));
            UpdateHearing();
            if (FrontRoomsSoundDirector.HumByStage) UpdateHum(dt);
            else
            {
                UpdateWallOcclusion();
                UpdatePresenceV1(hunter != null && hunter.Released);
            }
        }

        void OnStep(int foot, Vector3 position)
        {
            if (hunter == null || !hunter.Released || !audible || gain <= .001f) return;   // past the exit band: silent, no caption
            var gait = SoundIds.RelayGait.Walk;
            if (hunter.State == HunterState.Chase) gait = SoundIds.RelayGait.Run;
            else if (speed < 1.2f) gait = SoundIds.RelayGait.Drag;
            var damp = dampnessAt != null ? dampnessAt(position) : .4f;
            FrontRoomsFmod.OneShot(SoundIds.RelayFootstep, position,
                SoundIds.Param.RelayGait, (float)gait, SoundIds.Param.Occlusion, muffle, SoundIds.Param.Dampness, damp,
                p4: SoundIds.Param.PathGain, v4: gain);
            FrontRoomsFmod.Caption(muffle > .2f ? "[MUFFLED HEAVY STEPS]" : "[HEAVY STEPS]", position, 1.2f);
        }

        /// <summary>W2: whether it is heard, how loud and how muffled, from the hunter's path fields (refreshed per cell).</summary>
        void UpdateHearing()
        {
            var seen = hunter != null && hunter.Released && hunter.PlayerSeesRelay;
            // L11 (the stairwell is a safe zone): from the chase's end at the door until it has walked off, its steps are
            // heard whatever the stage, so the player hears it come to the door, stop outside and go.
            var beat = hunter != null && hunter.Released && hunter.SafeDoorBeat;
            var now = hunter != null && hunter.Released && (hunter.WarnStage >= 2 || seen || beat);
            if (now != audible)
            {
                audible = now;
                FrontRoomsFmod.Note(audible ? "relay audible (stage " + hunter.WarnStage + (seen ? ", seen)" : beat ? ", safe door)" : ")") : "relay inaudible");
            }
            if (!audible) { gain = 0f; return; }
            var f = hunter.PathDistanceToPlayer;
            if (float.IsInfinity(f) || float.IsNaN(f))
                f = listener != null ? Vector3.Distance(listener.position, transform.position) : NearBand;
            float g;
            if (f <= NearBand) g = GainFloor + (1f - GainFloor) * (1f - f / NearBand);
            else if (seen) g = GainFloor * NearBand / f;
            else g = GainFloor * Mathf.Clamp01(1f - (f - NearBand) / (ExitBand - NearBand));
            gain = Mathf.Clamp01(g);
            float openings;
            if (seen) openings = LiteOcclusion();
            else if (ListenerInSafeZone)
            {
                // In the stairwell (X is no map, so no path reaches it) the EXIT door's state muffles the steps, and so do the
                // walls between while it still walks up round a corner (the v1 count, where an open leaf is no wall).
                UpdateWallOcclusion();
                openings = Mathf.Max(MuffleOf(map.ExitOpeningCost()), wallOcclusion);
            }
            else openings = MuffleOf(hunter.PathOpeningCost);
            // A peek has the head against the leaf, so the far side comes a little closer (door_vision/60_landing.md §7).
            if (FrontRoomsInspect.PeekActive) openings = Mathf.Max(0f, openings - PeekEar);
            muffle = Mathf.Max(openings, .5f * Mathf.Clamp01((f - 3f) / 21f));
        }

        /// <summary>Door vision: a Relay seen through a shut door's lite counts as seen, but the leaf still stands between them.
        /// Sight passes only a lited leaf, so the shut lited leaves on the line are what muffles it (0 for a clear view).</summary>
        float LiteOcclusion()
        {
            if (listener == null || Time.time < nextLiteCheck) return liteOcclusion;
            nextLiteCheck = Time.time + .1f;
            var dir = transform.position + Vector3.up * 1.4f - listener.position;
            var dist = dir.magnitude;
            if (dist < .01f) return liteOcclusion;
            var count = Physics.RaycastNonAlloc(listener.position, dir / dist, hits, dist, ~0, QueryTriggerInteraction.Ignore);
            var cost = 0f;
            for (var i = 0; i < count; i++)
            {
                var c = hits[i].collider;
                if (FrontRoomsDoorLite.OfCollider(c) != null && (map == null || !map.IsOpenDoorLeaf(c))) cost += ShutOpeningCost;
            }
            liteOcclusion = MuffleOf(cost);
            return liteOcclusion;
        }

        /// <summary>Path opening cost (FrontRoomsMapPathField: ajar 4, shut 9 per opening) to muffle: 0.3 ajar, 0.55 one shut, 0.85 two or more.</summary>
        static float MuffleOf(float cost)
        {
            if (float.IsInfinity(cost) || float.IsNaN(cost)) return .85f;
            if (cost <= 0f) return 0f;
            if (cost <= 4f) return .3f * cost / 4f;
            if (cost <= 9f) return .3f + .25f * (cost - 4f) / 5f;
            return Mathf.Min(.85f, .55f + .3f * (cost - 9f) / 9f);
        }

        /// <summary>C30 on: the low hum (Relay/Hum) only while the steps are audible, at their gain and muffle.</summary>
        void UpdateHum(float dt)
        {
            if (!FrontRoomsFmod.Ready || listener == null) return;
            if (!audible || ListenerInSafeZone) { FrontRoomsFmod.Stop(ref presence); return; }   // fades out (AHDSR release); never in the stairwell
            if (FrontRoomsFmod.Finished(ref presence))
            {
                presence = FrontRoomsFmod.Create(SoundIds.RelayHum, transform.position);
                if (!presence.isValid()) return;
                if (!idsReady)
                {
                    occlusionId = FrontRoomsFmod.ParameterId(SoundIds.RelayHum, SoundIds.Param.Occlusion);
                    gainId = FrontRoomsFmod.ParameterId(SoundIds.RelayHum, SoundIds.Param.PathGain);
                    idsReady = true;
                }
                humGain = gain;
                humMuffle = muffle;
                presence.start();
                FrontRoomsFmod.Caption("[LOW HUM]", transform.position, 3f);
            }
            // F moves a cell at a time: glide the continuous hum so a cell change never steps it.
            humGain = Mathf.MoveTowards(humGain, gain, dt * 1.5f);
            humMuffle = Mathf.MoveTowards(humMuffle, muffle, dt * 2f);
            presence.setParameterByID(gainId, humGain);
            presence.setParameterByID(occlusionId, humMuffle);
            FrontRoomsFmod.Move(presence, transform.position + Vector3.up * 1.2f);
        }

        /// <summary>L11: the Relay stopped at the stairwell door (StoppedAtSafeDoor): one dragged step where it stands, at the steps' level and muffle.</summary>
        public void Scuff(Vector3 feet)
        {
            if (hunter == null || !hunter.Released || !audible || gain <= .001f) return;
            var damp = dampnessAt != null ? dampnessAt(feet) : .4f;
            FrontRoomsFmod.OneShot(SoundIds.RelayFootstep, feet,
                SoundIds.Param.RelayGait, (float)SoundIds.RelayGait.Drag, SoundIds.Param.Occlusion, muffle, SoundIds.Param.Dampness, damp,
                p4: SoundIds.Param.PathGain, v4: gain);
            FrontRoomsFmod.Note("relay stopped at the safe door");
        }

        // ------------------------------------------------------------ C30 off: the v1 presence drone, unchanged
        void UpdateWallOcclusion()
        {
            if (listener == null || Time.time < nextOcclusionCheck) return;
            nextOcclusionCheck = Time.time + .1f;
            var from = listener.position;
            var to = transform.position + Vector3.up * 1.4f;
            var dir = to - from;
            var dist = dir.magnitude;
            if (dist < .01f) return;
            var count = Physics.RaycastNonAlloc(from, dir / dist, hits, dist, ~0, QueryTriggerInteraction.Ignore);
            var walls = 0;
            for (var i = 0; i < count; i++)
            {
                var c = hits[i].collider;
                if (c.transform.IsChildOf(transform)) continue;
                // Count only the map's walls, doors and windows, and not an open door's leaf (the hunter's own test).
                if (map != null ? !map.IsArchitecture(c) || map.IsOpenDoorLeaf(c) : c.transform.IsChildOf(listener)) continue;
                walls++;
            }
            var target = walls == 0 ? 0f : walls == 1 ? .55f : .85f;
            wallOcclusion = Mathf.MoveTowards(wallOcclusion, target, .35f);
        }

        void UpdatePresenceV1(bool released)
        {
            if (!FrontRoomsFmod.Ready || listener == null) return;
            if (!released) { FrontRoomsFmod.Stop(ref presence); return; }
            if (FrontRoomsFmod.Finished(ref presence))
            {
                presence = FrontRoomsFmod.Create(SoundIds.RelayPresence, transform.position);
                FrontRoomsFmod.Caption("[LOW DRONE]", null, 3f);
                if (!presence.isValid()) return;
                if (!idsReady)
                {
                    proximityId = FrontRoomsFmod.ParameterId(SoundIds.RelayPresence, SoundIds.Param.Proximity);
                    occlusionId = FrontRoomsFmod.ParameterId(SoundIds.RelayPresence, SoundIds.Param.Occlusion);
                    idsReady = true;
                }
                presence.start();
            }
            var proximity = 1f - Mathf.Clamp01(Vector3.Distance(listener.position, transform.position) / 30f);
            presence.setParameterByID(proximityId, proximity);
            presence.setParameterByID(occlusionId, wallOcclusion);
            FrontRoomsFmod.Move(presence, transform.position + Vector3.up * 1.2f);
        }
    }
}
