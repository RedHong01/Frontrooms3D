using FMOD.Studio;
using UnityEngine;

namespace FrontRooms.Audio
{
    /// <summary>
    /// The sound of a screen whose <c>SoundRole</c> is not None (tutorial T6 / S1-S5). The director attaches one per
    /// sounding <see cref="FrontRoomsScreenVideo"/>. While the set is on: the WTCZ station loop from the set itself, and
    /// its 15.7 kHz flyback whine within 2 m. E turns it on and off with the set's own power sounds.
    /// The broadcast is the clock (agreed with 游戏视觉 for V5): the shared ad player seeks to <see cref="LoopTime"/>
    /// when a sounding screen comes into view, because the audio keeps playing when the picture is unseen.
    /// TitleTV plays clear at the set. Beacon (each floor's station TV) is S2: in sight, the set itself; out of sight,
    /// the doorway the sound leaks through on the path to the set (关卡设计's beacon field, M5), with its level and
    /// top end from that path rather than the straight line; with no route it is silent and the loop keeps running.
    /// EXAMINE holds every sounding screen with its picture except the one inspected (R3, Red 10-29), which keeps playing.
    /// A Beacon waits for its floor's start door (the map's north start lamps stay at 0 until it opens: 关卡设计's A26 kit) and
    /// then starts without the power-on click, because the set was on before the player came. The key take powers the set
    /// off for good (游戏视觉's station dresser), which plays the power-off and its caption here (S4).
    /// </summary>
    public sealed class FrontRoomsScreenSound : MonoBehaviour
    {
        const int LoopMs = 16000;       // the WTCZ loop: 32 beats at 120 BPM
        const float CaptionRange = 8f, CaptionEvery = 20f;
        const float SightEvery = .1f, LevelSlew = 2.5f;   // line of sight 10 times a second, silence in or out over 0.4 s
        const float SightSettle = .25f;   // a change of sight must hold this long, so a doorway edge never flaps
        // S2's far half (tutorial 10_proposal §4.5): FMOD's own distance attenuation is overridden while the sound leaks
        // through a doorway, and BeaconGain / BeaconCutoff carry the path. Parameters slew over about 0.5 s (so opening
        // a door audibly opens the sound) and the emitter glides 0.4 s when the doorway changes.
        const float FarMinDistance = 95f, FarMaxDistance = 100f, EarHeight = 1.4f;
        const float ParamTau = .2f, GlideTau = .13f, Reach = 90f, ReachFade = 10f;
        const float DoorDb = -5f, DoorCutoff = .6f;
        const float HandheldFarFrom = 15f, HandheldFloorHz = 600f, HandheldFarDb = 4f;   // §4.5 Phones (SP)

        /// <summary>S3: the beacon's duck under the Relay's cues (dB, 0 or below), set by the director every frame:
        /// −6 dB under a W1 groan or W2 steps and hum, −12 dB in a chase.</summary>
        public static float DuckDb;

        public FrontRoomsScreenVideo screen;
        public Transform listener;
        public FrontRoomsMapWorld map;

        EventInstance broadcast, whine;
        bool on, primed, beaconNoted, inSight = true, far, farRoute, startHeld;
        float nextCaption, nextSight, level = 1f, sightSince;
        bool sightPending;
        float gainDb, cutoffHz = 22000f, farMetres;
        int farDoors;
        Vector3 emitter, portal;
        PARAMETER_ID gainId, cutoffId;
        bool idsReady;
        // Sounding screens currently playing. One plays at a time in the design (the title TV, then one station per
        // floor), so the first one is the clock; if it goes, the next playing one takes over.
        static readonly System.Collections.Generic.List<FrontRoomsScreenSound> active = new System.Collections.Generic.List<FrontRoomsScreenSound>(4);
        static readonly RaycastHit[] hits = new RaycastHit[16];
        static bool holding;
        static FrontRoomsScreenVideo holdExempt;

        // Statics survive a play session when domain reload is off: start each one clean.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { active.Clear(); holding = false; holdExempt = null; DuckDb = 0f; }

        /// <summary>Seconds 0-16 into the playing WTCZ loop (its FMOD timeline), or -1 while no sounding screen plays.
        /// A screen held by EXAMINE stops its timeline, so the clock is read from one still playing (the inspected set)
        /// first, and from a held one only when every screen is held (游戏视觉 saw it stall 1.27 s, 10-29).</summary>
        public static float LoopTime
        {
            get
            {
                for (var pass = 0; pass < 2; pass++)
                    for (var i = 0; i < active.Count; i++)
                    {
                        var c = active[i];
                        if (c == null || !c.broadcast.isValid()) continue;
                        if (pass == 0 && holding && c.screen != holdExempt) continue;
                        if (c.broadcast.getTimelinePosition(out var ms) == FMOD.RESULT.OK) return (ms % LoopMs) / 1000f;
                    }
                return -1f;
            }
        }

        /// <summary>A sounding screen is playing the loop.</summary>
        public static bool Playing => LoopTime >= 0f;

        /// <summary>
        /// EXAMINE (the director, on FrontRooms3DGame.Inspecting): while <paramref name="on"/>, every sounding screen
        /// pauses where it is, except <paramref name="exempt"/>, the screen being inspected, whose picture also keeps
        /// running (游戏视觉's C26 exemption). Off releases them all.
        /// </summary>
        public static void HoldForInspect(bool on, FrontRoomsScreenVideo exempt)
        {
            if (holding == on && holdExempt == (on ? exempt : null)) return;
            holding = on;
            holdExempt = on ? exempt : null;
            for (var i = 0; i < active.Count; i++) if (active[i] != null) active[i].ApplyHold();
            if (active.Count > 0)
                FrontRoomsFmod.Note(on ? "screens held for EXAMINE" + (exempt != null ? ", except the inspected " + exempt.name : "") : "screens released");
        }

        // S2's path level (dB re the set within 1 m), set against the room tone (FMOD renders, fmod_check beacon*, 10-29):
        // the inverse law to 6 m, where it meets the set's own attenuation at the doorway, then nearly flat, because the
        // low-pass carries the distance and the far bands must stay audible over the room tone (10_proposal 4.5), whose
        // strongest band (150-400 Hz, the HumBed) is where the muffled set lives. Measured on whole 16 s loops against the
        // room tone (LUFS): about +1 dB at 8 and 15 m, -1.5 dB at 30 m, -7 dB at 60 m, -10 dB at 90 m; ~6 dB per shut door.
        static readonly float[] GainMetres = { 6f, 15f, 30f, 60f, 90f };
        static readonly float[] GainDb = { -15.56f, -16f, -16.5f, -18f, -19.5f };

        /// <summary>S2's path gain (dB) for P metres along the path and the shut doors on it: −20 log10 P to 6 m, then
        /// GainDb in log distance to 90 m; −5 dB per shut door; fading out between 90 and 100 m.</summary>
        public static float PathGainDb(float metres, int doors, bool handheld)
        {
            var p = Mathf.Max(1f, metres);
            float db;
            if (p <= GainMetres[0]) db = -20f * Mathf.Log10(p);
            else
            {
                var i = 1;
                while (i < GainMetres.Length - 1 && p > GainMetres[i]) i++;
                var t = Mathf.Clamp01(Mathf.Log(p / GainMetres[i - 1]) / Mathf.Log(GainMetres[i] / GainMetres[i - 1]));
                db = Mathf.Lerp(GainDb[i - 1], GainDb[i], t);
            }
            db += DoorDb * Mathf.Max(0, doors);
            if (handheld && metres >= HandheldFarFrom) db += HandheldFarDb;
            if (metres > Reach) db -= 74f * Mathf.Clamp01((metres - Reach) / ReachFade);
            return Mathf.Clamp(db, -80f, 0f);
        }

        /// <summary>S2's path cut-off (Hz): the §4.5 bands (6 kHz at 6 m, 2.5 kHz at 15, 800 Hz at 30, 350 Hz at 60, 250 Hz
        /// at 90, interpolated in log frequency), times 0.6 per shut door; on phones never below 600 Hz past 15 m.</summary>
        public static float PathCutoffHz(float metres, int doors, bool handheld)
        {
            float lo, hi, a, b;
            var m = Mathf.Max(0f, metres);
            if (m <= 6f) { a = 0f; b = 6f; lo = 8000f; hi = 6000f; }
            else if (m <= 15f) { a = 6f; b = 15f; lo = 6000f; hi = 2500f; }
            else if (m <= 30f) { a = 15f; b = 30f; lo = 2500f; hi = 800f; }
            else if (m <= 60f) { a = 30f; b = 60f; lo = 800f; hi = 350f; }
            else { a = 60f; b = 90f; lo = 350f; hi = 250f; }
            var t = Mathf.Clamp01((m - a) / (b - a));
            var hz = Mathf.Exp(Mathf.Lerp(Mathf.Log(lo), Mathf.Log(hi), t));
            hz *= Mathf.Pow(DoorCutoff, Mathf.Max(0, doors));
            if (handheld && metres >= HandheldFarFrom) hz = Mathf.Max(hz, HandheldFloorHz);
            return Mathf.Clamp(hz, 100f, 22000f);
        }

        void ApplyHold()
        {
            var held = holding && screen != holdExempt;
            if (broadcast.isValid()) broadcast.setPaused(held);
            if (whine.isValid()) whine.setPaused(held);
        }

        void Update()
        {
            if (!FrontRoomsFmod.Ready || screen == null) return;
            var role = screen.SoundRole;
            if (role == FrontRoomsScreenSoundRole.Beacon && !beaconNoted)
            {
                beaconNoted = true;
                FrontRoomsFmod.Note("screen role Beacon: the set in sight, the doorway on the path otherwise (S2)");
            }
            // A Beacon waits for its floor's start door; held, it never clicks on or off.
            var held = role == FrontRoomsScreenSoundRole.Beacon && map != null && map.StartLampsNorth <= 0f;
            var want = screen.IsPoweredOn && role != FrontRoomsScreenSoundRole.None && !held;
            var p = screen.transform.position;
            if (want != on)
            {
                on = want;
                if (on) Begin(p, primed && !startHeld, role);
                else End(p, primed && !held);
            }
            if (held != startHeld)
            {
                startHeld = held;
                if (held) FrontRoomsFmod.Note("screen held until the start door opens");
            }
            primed = true;
            if (!on) return;
            var beacon = role == FrontRoomsScreenSoundRole.Beacon;
            if (beacon && Time.unscaledTime >= nextSight)
            {
                nextSight = Time.unscaledTime + SightEvery;
                var seen = InSight(p) || InStationNow();
                if (seen == inSight) sightPending = false;
                else if (!sightPending) { sightPending = true; sightSince = Time.unscaledTime; }
                else if (Time.unscaledTime - sightSince >= SightSettle) { inSight = seen; sightPending = false; }
                farRoute = !inSight && FarPath(out farMetres, out farDoors, out portal);
            }
            // Where the sound comes from and how much of it: the set in sight (or any TitleTV), else the doorway.
            var wantFar = beacon && !inSight && farRoute;
            var handheld = FrontRoomsHandheld.Active;
            var targetLevel = !beacon || inSight || farRoute ? 1f : 0f;
            var targetGain = wantFar ? PathGainDb(farMetres, farDoors, handheld) : 0f;
            var targetCutoff = wantFar ? PathCutoffHz(farMetres, farDoors, handheld) : 22000f;
            var targetPos = wantFar ? portal + Vector3.up * EarHeight : p;
            if (wantFar != far) SetFar(wantFar);
            var dt = Time.unscaledDeltaTime;
            var k = 1f - Mathf.Exp(-dt / ParamTau);
            gainDb += (targetGain + Mathf.Min(0f, DuckDb) - gainDb) * k;
            cutoffHz = Mathf.Exp(Mathf.Lerp(Mathf.Log(cutoffHz), Mathf.Log(targetCutoff), k));
            emitter = far ? Vector3.Lerp(emitter, targetPos, 1f - Mathf.Exp(-dt / GlideTau)) : p;
            if (level != targetLevel)
            {
                level = Mathf.MoveTowards(level, targetLevel, LevelSlew * dt);
                broadcast.setVolume(level);
                whine.setVolume(level);
            }
            if (idsReady)
            {
                broadcast.setParameterByID(gainId, Mathf.Clamp(gainDb, -80f, 0f));
                broadcast.setParameterByID(cutoffId, Mathf.Clamp(cutoffHz, 100f, 22000f));
            }
            FrontRoomsFmod.Move(broadcast, emitter);
            FrontRoomsFmod.Move(whine, p);
            if (level > .5f && listener != null && Time.unscaledTime >= nextCaption)
            {
                if (!far && Vector3.Distance(listener.position, p) <= CaptionRange)
                {
                    nextCaption = Time.unscaledTime + CaptionEvery;
                    FrontRoomsFmod.Caption("[TV]", p, 2.5f);
                }
                else if (far && gainDb > -45f)
                {
                    // S5: the HUD adds AHEAD / LEFT / RIGHT / BEHIND from the doorway's position.
                    nextCaption = Time.unscaledTime + CaptionEvery;
                    FrontRoomsFmod.Caption("[TV, MUFFLED]", emitter, 2.5f);
                }
            }
        }

        // Out of sight: FMOD's straight-line attenuation off (the path carries the level), back to the event's own range
        // in sight. The emitter starts its glide from the set, so the hand-over at a doorway pans instead of jumping.
        void SetFar(bool value)
        {
            far = value;
            if (!broadcast.isValid()) return;
            broadcast.setProperty(EVENT_PROPERTY.MINIMUM_DISTANCE, value ? FarMinDistance : -1f);
            broadcast.setProperty(EVENT_PROPERTY.MAXIMUM_DISTANCE, value ? FarMaxDistance : -1f);
            if (value) emitter = screen.transform.position;
            FrontRoomsFmod.Note(value ? "screen beacon far: " + farMetres.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " m, " + farDoors + " shut" : "screen beacon in sight");
        }

        /// <summary>The beacon field's route from the listener's cell (关卡设计's M5): false with no station target, the key
        /// taken, or no route.</summary>
        bool FarPath(out float metres, out int doors, out Vector3 at)
        {
            metres = float.PositiveInfinity;
            doors = 0;
            at = default;
            var way = FrontRoomsWayCueDriver.Current != null ? FrontRoomsWayCueDriver.Current.Way : null;
            if (way == null || map == null || listener == null) return false;
            return way.BeaconPath(map.CellOf(listener.position), out metres, out doors, out at);
        }

        // Inside the station the set is heard from itself, as in sight: the station's carve leaves it out of the map's
        // rooms, so the beacon field's portal there is a crossing inside the station, or the TV cell's centre inside the
        // riser (关卡设计, 10-29).
        bool InStationNow() => map != null && listener != null && map.InStation(map.CellOf(listener.position));

        /// <summary>No wall, shut door or window of the map between the listener and the set (an open leaf does not
        /// block: the Relay's own test). Without the map nothing counts as seen.</summary>
        bool InSight(Vector3 set)
        {
            if (listener == null || map == null) return false;
            var from = listener.position;
            var dir = set - from;
            var dist = dir.magnitude;
            if (dist < .01f) return true;
            var count = Physics.RaycastNonAlloc(from, dir / dist, hits, dist, ~0, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < count; i++)
            {
                var c = hits[i].collider;
                if (c == null || c.transform.IsChildOf(screen.transform)) continue;
                if (map.IsArchitecture(c) && !map.IsOpenDoorLeaf(c)) return false;
            }
            return true;
        }

        void Begin(Vector3 p, bool byPlayer, FrontRoomsScreenSoundRole role)
        {
            var beacon = role == FrontRoomsScreenSoundRole.Beacon;
            inSight = !beacon || InSight(p) || InStationNow();
            farRoute = beacon && !inSight && FarPath(out farMetres, out farDoors, out portal);
            nextSight = Time.unscaledTime + SightEvery;
            level = inSight || farRoute ? 1f : 0f;
            far = false;
            gainDb = farRoute ? PathGainDb(farMetres, farDoors, FrontRoomsHandheld.Active) : 0f;
            cutoffHz = farRoute ? PathCutoffHz(farMetres, farDoors, FrontRoomsHandheld.Active) : 22000f;
            emitter = p;
            broadcast = FrontRoomsFmod.Create(SoundIds.ScreenTitleTV, p);
            if (!idsReady)
            {
                gainId = FrontRoomsFmod.ParameterId(SoundIds.ScreenTitleTV, SoundIds.Param.BeaconGain);
                cutoffId = FrontRoomsFmod.ParameterId(SoundIds.ScreenTitleTV, SoundIds.Param.BeaconCutoff);
                idsReady = true;
            }
            if (broadcast.isValid())
            {
                broadcast.setVolume(level);
                broadcast.setParameterByID(gainId, gainDb);
                broadcast.setParameterByID(cutoffId, cutoffHz);
                broadcast.start();
            }
            whine = FrontRoomsFmod.Create(SoundIds.ScreenWhine, p);
            if (whine.isValid()) { whine.setVolume(level); whine.start(); }
            if (farRoute) SetFar(true);
            if (byPlayer) FrontRoomsFmod.OneShot(SoundIds.ScreenPowerOn, p);   // a set already on at load makes no click
            if (!active.Contains(this)) active.Add(this);
            ApplyHold();
            FrontRoomsFmod.Note("screen on (" + role + (beacon ? (inSight ? ", in sight" : farRoute ? ", far" : ", no route") : "") + ")");
        }

        void End(Vector3 p, bool byPlayer)
        {
            FrontRoomsFmod.Stop(ref broadcast, true);
            FrontRoomsFmod.Stop(ref whine, true);
            if (byPlayer)
            {
                FrontRoomsFmod.OneShot(SoundIds.ScreenPowerOff, p);
                FrontRoomsFmod.Caption("[TV SWITCHES OFF]", p, 2f);
            }
            active.Remove(this);
            far = false;
            FrontRoomsFmod.Note("screen off");
        }

        void OnDisable()
        {
            FrontRoomsFmod.Stop(ref broadcast, true);
            FrontRoomsFmod.Stop(ref whine, true);
            active.Remove(this);
            on = false;
            primed = false;
            far = false;
            startHeld = false;
        }
    }
}
