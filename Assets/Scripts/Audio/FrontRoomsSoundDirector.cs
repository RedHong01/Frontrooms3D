using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using FrontRooms.Map;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FrontRooms.Audio
{
    /// <summary>
    /// Owns the FMOD side of the game without editing gameplay code:
    /// - starts the room tone (hum + air) and keeps the listener on the camera;
    /// - finds doors, lamps, the player and the Relay rig and attaches sound to them;
    /// - subscribes to the game's events (map run, doors, glass, keys, the Relay);
    /// - drives the global mix parameters (Tension, Zone) and the subjective layer.
    /// While FMOD is ready it mutes the legacy Unity audio; if FMOD is missing or
    /// fails, it stays inert and the legacy audio keeps playing.
    /// </summary>
    public sealed class FrontRoomsSoundDirector : MonoBehaviour
    {
        public static FrontRoomsSoundDirector Instance { get; private set; }

        // C30 (asked of Red through 系统设计, 2026-10-22): RELAY §13 items 3 and 5 now, or with v2. Off is the v1
        // behaviour, unchanged. HumByStage: the Relay's low hum only at warning stage 2 or in sight (Relay/Hum), in place
        // of the always-on Relay/Presence drone. TensionByStage: Tension from the warning stage, in place of the state
        // weight x straight-line proximity.
        public static readonly bool HumByStage = true, TensionByStage = true;

        [SerializeField] bool muteLegacyUnityAudio = true;
        [SerializeField] int fixtureVoices = 4;
        // Every fixture voice plays the same recorded loop. Started in the same frame they stay
        // sample-locked and add up in phase (6 lamps measured +12 dB over one, louder than the
        // player's steps). Starting them a non-multiple of the 4 s loop apart de-phases them.
        const float FixtureStagger = .53f;
        // FrontRoomsRoomStream names its double door's pivots so (read-only to the audio layer).
        const string StreamLeftHinge = "double door left hinge", StreamRightHinge = "double door right hinge";
        [SerializeField] float fixtureRadius = 10f;

        bool started, legacyRestored;
#if UNITY_WEBGL && !UNITY_EDITOR
        bool webUnlocked;
#endif
        float nextDiscover, nextFixtureUpdate, tension;
        EventInstance humBed, airBed, breath, heartbeat, windowStress;
        PARAMETER_ID staminaId, arousalId, stressProgressId, groanId;
        bool groanIdReady;
        float groan, groanSent, lastChase = float.NegativeInfinity;
        // RELAY §9.4: the heartbeat comes from exertion and the chase only, and fades over 3 s after a chase.
        const float ChaseFade = 3f;
        bool runIdsReady;

        FrontRoomsMapWorld map;
        FrontRoomsMapHunter relay;
        bool caught;
        bool keyInLock;   // the unlock shot's key, from its insert to its withdraw
        float nextBedCheck;
        Transform listener;
        FrontRoomsPlayerFootsteps player;
        FrontRoomsRelaySound relayBody;
        FrontRoomsStairwell stairwell;
        Vector3 lastBlowDoor;
        int blowCount;
        float lastStressProgress;

        readonly HashSet<int> seen = new HashSet<int>();
        readonly List<GameObject> roots = new List<GameObject>(32);
        readonly List<Transform> scan = new List<Transform>(4096);

        // Lamps: discovered Light components, read-only.
        readonly List<Light> lamps = new List<Light>(256);
        readonly List<float> lampMax = new List<float>(256);
        readonly List<float> lampPrev = new List<float>(256);
        readonly List<float> lampNextEvent = new List<float>(256);
        EventInstance[] fixtureVoice;
        int[] fixtureLamp;
        float[] fixtureStartAt;
        bool[] fixtureStarted;
        int[] nearest;
        float[] nearestDistance;
        PARAMETER_ID fixtureLevelId;
        bool fixtureIdReady;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var tracing = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-audioTrace") >= 0;
            if (Instance != null || (Application.isBatchMode && !tracing)) return;
            FrontRoomsFmod.Tracing = tracing;
            var go = new GameObject("FrontRooms Sound Director");
            DontDestroyOnLoad(go);
            go.AddComponent<FrontRoomsSoundDirector>();
            if (FrontRoomsAudioCapture.Requested) go.AddComponent<FrontRoomsAudioCapture>();
        }

        void Awake()
        {
            Instance = this;
            fixtureVoice = new EventInstance[fixtureVoices];
            fixtureLamp = new int[fixtureVoices];
            fixtureStartAt = new float[fixtureVoices];
            fixtureStarted = new bool[fixtureVoices];
            nearest = new int[fixtureVoices];
            nearestDistance = new float[fixtureVoices];
            for (var i = 0; i < fixtureVoices; i++) fixtureLamp[i] = -1;
        }

        void OnEnable()
        {
            FrontRooms3DGame.MapRunStarted += OnMapRunStarted;
            FrontRooms3DGame.MapRunEnded += OnMapRunEnded;
            FrontRooms3DGame.PlayerClimbed += OnPlayerClimbed;
            FrontRooms3DGame.Paused += OnPaused;
            FrontRooms3DGame.Inspecting += OnInspecting;
            FrontRooms3DGame.WarnBurst += OnWarnBurst;
            FrontRoomsDoorShot.Beat += OnDoorShotBeat;
            FrontRoomsKeyHud.SoundCue += OnHudSoundCue;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDisable()
        {
            FrontRooms3DGame.MapRunStarted -= OnMapRunStarted;
            FrontRooms3DGame.MapRunEnded -= OnMapRunEnded;
            FrontRooms3DGame.PlayerClimbed -= OnPlayerClimbed;
            FrontRooms3DGame.Paused -= OnPaused;
            FrontRooms3DGame.Inspecting -= OnInspecting;
            FrontRooms3DGame.WarnBurst -= OnWarnBurst;
            FrontRoomsDoorShot.Beat -= OnDoorShotBeat;
            FrontRoomsKeyHud.SoundCue -= OnHudSoundCue;
            if (stairwell != null) { stairwell.NorthDoorLatched -= OnNorthDoorLatched; stairwell = null; }
            OnPaused(false);
            OnInspecting(false);
            SceneManager.sceneLoaded -= OnSceneLoaded;
            OnMapRunEnded();
            StopRoomTone(true);
            for (var i = 0; i < fixtureVoices; i++) FrontRoomsFmod.Stop(ref fixtureVoice[i], true);
            if (muteLegacyUnityAudio) AudioListener.volume = 1f;
            if (FrontRoomsFmod.Tracing)
            {
                var args = System.Environment.GetCommandLineArgs();
                var seed = System.Array.IndexOf(args, "-autopilotSeed");
                var name = seed >= 0 && seed + 1 < args.Length ? "trace_seed" + args[seed + 1] + ".txt" : "trace.txt";
                FrontRoomsFmod.WriteTrace(System.IO.Path.Combine(System.IO.Directory.GetParent(Application.dataPath).FullName,
                    "Verification", "audio", name));
            }
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            seen.Clear();
            lamps.Clear(); lampMax.Clear(); lampPrev.Clear(); lampNextEvent.Clear();
            for (var i = 0; i < fixtureVoices; i++) fixtureLamp[i] = -1;
            listener = null;
            player = null;
            relayBody = null;
            nextDiscover = 0f;
            if (started) StartRoomTone();
        }

        void Update()
        {
            UnlockWebAudio();
            FrontRoomsFmod.EnsureStarted();
            if (FrontRoomsFmod.Failed)
            {
                // FMOD broke mid-session: give the sound back to the legacy Unity audio rather than go silent.
                if (started && muteLegacyUnityAudio && !legacyRestored) { AudioListener.volume = 1f; legacyRestored = true; }
                return;
            }
            var now = Time.unscaledTime;
            if (now >= nextDiscover) { nextDiscover = now + 1f; Discover(); }   // attach to the scene before FMOD is ready
            if (!FrontRoomsFmod.Ready) return;
            if (!started)
            {
                started = true;
                if (muteLegacyUnityAudio) AudioListener.volume = 0f;
                StartRoomTone();
                var stamp = FrontRoomsFmod.SoundSetStamp();
                FrontRoomsFmod.Note("ready; sound set " + stamp);
                Debug.Log("[FrontRoomsAudio] FMOD ready: " + stamp + ". Room tone on; Unity audio is off, so everything heard is FMOD.");
            }
            if (!caught && now >= nextBedCheck) { nextBedCheck = now + 1f; StartRoomTone(); }   // restarts a bed FMOD finished
            EnsureRunVoices();
            // Caught cuts everything to silence: the lamps must not come back under the tinnitus.
            if (!caught && now >= nextFixtureUpdate) { nextFixtureUpdate = now + .2f; UpdateFixtures(); }
            // EXAMINE (S1): the close-up holds the director's tension slide and the subjective layer where they were.
            if (!inspecting)
            {
                UpdateGlobals(Time.unscaledDeltaTime);
                UpdateSubjective();
            }
            UpdateGroan();
        }

        // ------------------------------------------------------------ room tone
        void StartRoomTone()
        {
            if (FrontRoomsFmod.Finished(ref humBed))
            {
                humBed = FrontRoomsFmod.Create2D(SoundIds.HumBed);
                if (humBed.isValid()) humBed.start();
            }
            if (FrontRoomsFmod.Finished(ref airBed)) { airBed = FrontRoomsFmod.Create2D(SoundIds.AirBed); if (airBed.isValid()) airBed.start(); }
        }

        void StopRoomTone(bool immediate)
        {
            FrontRoomsFmod.Stop(ref humBed, immediate);
            FrontRoomsFmod.Stop(ref airBed, immediate);
        }

        // ------------------------------------------------------------ discovery
        void Discover()
        {
            if (listener == null)
            {
                // The ears are the camera. FMOD's setup wizard can replace the scene's Unity
                // AudioListener with a StudioListener, so accept either, and add the FMOD one only if missing.
                var unityListener = FindAnyObjectByType<AudioListener>();
                var fmodListener = FindAnyObjectByType<StudioListener>();
                var ears = unityListener != null ? unityListener.transform : fmodListener != null ? fmodListener.transform : null;
                if (ears != null)
                {
                    listener = ears;
                    if (!FrontRoomsFmod.Failed && listener.GetComponent<StudioListener>() == null)
                        listener.gameObject.AddComponent<StudioListener>();
                }
            }
            // Exactly one FMOD listener: two would make FMOD blend 3D positions between two pairs of ears.
            if (listener != null)
                foreach (var extra in FindObjectsByType<StudioListener>(FindObjectsSortMode.None))
                    if (extra.enabled && extra.transform != listener)
                    {
                        extra.enabled = false;
                        FrontRoomsFmod.Note("disabled extra FMOD listener on " + extra.gameObject.name);
                    }

            if (player == null)
            {
                var body = FindAnyObjectByType<CharacterController>();
                if (body != null)
                {
                    player = body.GetComponent<FrontRoomsPlayerFootsteps>();
                    if (player == null) player = body.gameObject.AddComponent<FrontRoomsPlayerFootsteps>();
                    player.surfaceAt = SurfaceAt;
                    player.dampnessAt = DampnessAt;
                }
            }

            if (relayBody == null)
            {
                var rig = FindAnyObjectByType<FrontRoomsRelayRig>();
                if (rig != null)
                {
                    relayBody = rig.GetComponent<FrontRoomsRelaySound>();
                    if (relayBody == null) relayBody = rig.gameObject.AddComponent<FrontRoomsRelaySound>();
                }
            }
            if (relayBody != null) { relayBody.hunter = relay; relayBody.map = map; relayBody.listener = listener; relayBody.dampnessAt = DampnessAt; }

            // Sounding screens (tutorial T6 / S1-S5): a screen's SoundRole is set by its owner when it is attached, so
            // look for them on every pass rather than once per transform.
            foreach (var screen in FindObjectsByType<FrontRoomsScreenVideo>(FindObjectsSortMode.None))
            {
                if (screen.SoundRole == FrontRoomsScreenSoundRole.None) continue;
                var sound = screen.GetComponent<FrontRoomsScreenSound>();
                if (sound == null) { sound = screen.gameObject.AddComponent<FrontRoomsScreenSound>(); sound.screen = screen; }
                sound.listener = listener;
                sound.map = map;
            }

            var stair = FindAnyObjectByType<FrontRoomsStairwell>();
            if (stair != stairwell)
            {
                if (stairwell != null) stairwell.NorthDoorLatched -= OnNorthDoorLatched;
                stairwell = stair;
                if (stairwell != null) stairwell.NorthDoorLatched += OnNorthDoorLatched;
            }

            // Doors and lamps are built at runtime (chunks, pooled rooms): attach once per new transform.
            var scene = SceneManager.GetActiveScene();
            scene.GetRootGameObjects(roots);
            for (var r = 0; r < roots.Count; r++)
            {
                roots[r].GetComponentsInChildren(true, scan);
                for (var i = 0; i < scan.Count; i++)
                {
                    var t = scan[i];
                    if (!seen.Add(FrontRoomsObjectId.Of(t))) continue;
                    var name = t.name;
                    if (name.StartsWith("Door hinge"))
                        Attach(t, FrontRoomsDoorSound.Mode.Manual, 95f);
                    else if (name == StreamLeftHinge || name == StreamRightHinge)
                    {
                        // The stream's double door swings on a script, both leaves at once: one recorded swing
                        // per leaf. The left leaf leads, the right follows (other takes, 28 ms later), so a pair
                        // is two leaves, never one sound doubled.
                        var door = Attach(t, FrontRoomsDoorSound.Mode.Stream, 88f);
                        if (door == null) continue;
                        var left = name == StreamLeftHinge;
                        door.pairedHinge = t.parent != null ? t.parent.Find(left ? StreamRightHinge : StreamLeftHinge) : null;
                        door.leaf = left || door.pairedHinge == null ? SoundIds.Leaf.Lead : SoundIds.Leaf.Follow;
                    }
                    else if (name == "Light" || name.StartsWith("fluorescent light"))
                    {
                        var light = t.GetComponent<Light>();
                        if (light != null) { lamps.Add(light); lampMax.Add(Mathf.Max(.01f, light.intensity)); lampPrev.Add(-1f); lampNextEvent.Add(0f); }
                    }
                }
            }
        }

        static FrontRoomsDoorSound Attach(Transform hinge, FrontRoomsDoorSound.Mode mode, float limit)
        {
            if (hinge.GetComponent<FrontRoomsDoorSound>() != null) return null;
            var door = hinge.gameObject.AddComponent<FrontRoomsDoorSound>();
            door.mode = mode;
            door.openLimit = limit;
            return door;
        }

        SoundIds.Surface SurfaceAt(Vector3 feet)
        {
            if (map == null) return SoundIds.Surface.Carpet;
            var cell = map.CellOf(feet);
            if (map.InStartArea(cell)) return SoundIds.Surface.Carpet;            // the title's stream rooms: Level 0
            var zone = map.ZoneOf(cell);
            return zone.theme == ZoneTheme.Office ? SoundIds.Surface.CarpetTile : SoundIds.Surface.Carpet;
        }

        /// <summary>
        /// How wet the floor is underfoot. The map has no water data yet, so the
        /// zone stands in: low ceilings hold the deep, sticky carpet; offices are
        /// glue-down tile and nearly dry. Puddles would return 0.8-1 here.
        /// </summary>
        float DampnessAt(Vector3 feet)
        {
            if (map == null) return .4f;
            var cell = map.CellOf(feet);
            if (map.InStartArea(cell)) return .4f;
            var zone = map.ZoneOf(cell);
            if (zone.theme == ZoneTheme.Office) return .15f;
            return zone.height == ZoneHeight.Low ? .55f : zone.height == ZoneHeight.Tall ? .3f : .4f;
        }

        // ------------------------------------------------------------ lamps
        void UpdateFixtures()
        {
            if (listener == null) return;
            var origin = listener.position;
            var found = 0;
            var r2 = fixtureRadius * fixtureRadius;
            for (var i = 0; i < lamps.Count; i++)
            {
                var lamp = lamps[i];
                if (lamp == null) continue;
                var d = (lamp.transform.position - origin).sqrMagnitude;
                if (d > r2) continue;
                // insertion into the fixed-size nearest list
                var slot = found < nearest.Length ? found++ : nearest.Length;
                if (slot == nearest.Length)
                {
                    if (d >= nearestDistance[nearest.Length - 1]) continue;
                    slot = nearest.Length - 1;
                }
                while (slot > 0 && nearestDistance[slot - 1] > d)
                {
                    nearest[slot] = nearest[slot - 1];
                    nearestDistance[slot] = nearestDistance[slot - 1];
                    slot--;
                }
                nearest[slot] = i;
                nearestDistance[slot] = d;
            }

            for (var v = 0; v < fixtureVoices; v++)
            {
                if (v >= found) { FrontRoomsFmod.Stop(ref fixtureVoice[v]); fixtureLamp[v] = -1; continue; }
                var index = nearest[v];
                var lamp = lamps[index];
                var on = lamp.enabled && lamp.gameObject.activeInHierarchy;
                var raw = on ? lamp.intensity : 0f;
                if (raw > lampMax[index]) lampMax[index] = raw;
                var level = Mathf.Clamp01(raw / lampMax[index]);
                LampEvents(index, lamp.transform.position, level);
                if (fixtureStarted[v] && FrontRoomsFmod.Finished(ref fixtureVoice[v])) fixtureStarted[v] = false;
                if (!fixtureVoice[v].isValid())
                {
                    fixtureVoice[v] = FrontRoomsFmod.Create(SoundIds.Fixture, lamp.transform.position);
                    if (!fixtureVoice[v].isValid()) continue;
                    if (!fixtureIdReady) { fixtureLevelId = FrontRoomsFmod.ParameterId(SoundIds.Fixture, SoundIds.Param.Level); fixtureIdReady = true; }
                    fixtureStartAt[v] = Time.unscaledTime + v * FixtureStagger;
                    fixtureStarted[v] = false;
                }
                if (!fixtureStarted[v] && Time.unscaledTime >= fixtureStartAt[v])
                {
                    fixtureVoice[v].start();
                    fixtureStarted[v] = true;
                }
                fixtureLamp[v] = index;
                fixtureVoice[v].setParameterByID(fixtureLevelId, level);
                FrontRoomsFmod.Move(fixtureVoice[v], lamp.transform.position);
            }
        }

        void LampEvents(int index, Vector3 position, float level)
        {
            var previous = lampPrev[index];
            lampPrev[index] = level;
            if (previous < 0f) return;                                  // first sight of this lamp: no event
            var now = Time.time;
            if (now < lampNextEvent[index]) return;
            if (previous < .15f && level > .6f)
            {
                FrontRoomsFmod.OneShot(SoundIds.FixtureEvent, position, SoundIds.Param.FixtureEvent, (float)SoundIds.FixtureKind.Strike);
                lampNextEvent[index] = now + 1.2f;
            }
            else if (previous > .6f && level < .15f)
            {
                FrontRoomsFmod.OneShot(SoundIds.FixtureEvent, position, SoundIds.Param.FixtureEvent, (float)SoundIds.FixtureKind.Tick);
                lampNextEvent[index] = now + .3f;
            }
        }

        // ------------------------------------------------------------ globals
        void UpdateGlobals(float dt)
        {
            var target = .05f;
            float rate;
            // The stairwell is a safe zone (Red, 2026-10-22): inside it Tension settles to its floor, whatever the Relay does.
            var safe = ListenerInSafeZone();
            // S3 (tutorial 10_proposal §4.5): the station TV never masks a warning. It ducks 6 dB under the W1 groan and
            // the W2 steps and hum, 12 dB in a chase.
            var chase = relay != null && relay.Released && !safe && (relay.State == HunterState.Chase || relay.State == HunterState.BreakDoor);
            var warned = relay != null && relay.Released && !safe && (relay.WarnStage >= 2 || FrontRooms3DGame.WarnBursting);
            FrontRoomsScreenSound.DuckDb = chase ? -12f : warned ? -6f : 0f;
            if (TensionByStage)
            {
                // RELAY §9.4 (C30): Tension follows the warning stage only (0.05 / 0.2 / 0.4, 1.0 in a chase), never the
                // straight-line distance through walls. Rise 1/s, fall 0.25/s.
                if (relay != null && relay.Released && !safe)
                    target = relay.State == HunterState.Chase ? 1f : relay.WarnStage >= 2 ? .4f : relay.WarnStage == 1 ? .2f : .05f;
                rate = target > tension ? 1f : .25f;
            }
            else
            {
                if (relay != null && relay.Released && listener != null && !safe)
                {
                    var proximity = 1f - Mathf.Clamp01(Vector3.Distance(listener.position, relay.Position) / 30f);
                    float stateWeight;
                    switch (relay.State)
                    {
                        case HunterState.Chase: stateWeight = 1f; break;
                        case HunterState.BreakDoor: stateWeight = .8f; break;
                        case HunterState.Search: stateWeight = .55f; break;
                        case HunterState.Hunt: stateWeight = .45f; break;
                        case HunterState.Wander: stateWeight = .2f; break;   // roaming, not tracking you
                        default: stateWeight = .25f; break;
                    }
                    target = stateWeight * Mathf.Lerp(.5f, 1f, proximity);
                }
                rate = target > tension ? 2f : .5f;                      // rise fast, settle slowly
            }
            tension = Mathf.MoveTowards(tension, target, rate * dt);
            FrontRoomsFmod.SetGlobal(SoundIds.Param.Tension, tension);

            var zone = SoundIds.Zone.Standard;
            if (map != null && listener != null)
            {
                var cell = map.CellOf(listener.position);
                if (map.InStairwell(cell)) zone = SoundIds.Zone.Stair;     // C22's stairwells: this floor's X and the one it started in
                else if (!map.InStartArea(cell))
                {
                    var info = map.ZoneOf(cell);
                    zone = info.theme == ZoneTheme.Office ? SoundIds.Zone.Office
                        : info.height == ZoneHeight.Low ? SoundIds.Zone.Low
                        : info.height == ZoneHeight.Tall ? SoundIds.Zone.Tall : SoundIds.Zone.Standard;
                }
            }
            FrontRoomsFmod.SetGlobal(SoundIds.Param.Zone, (float)zone);
        }

        /// <summary>The listener is in the stairwell (the map's exit area X): the safe zone (Red, 2026-10-22).</summary>
        bool ListenerInSafeZone() => map != null && listener != null && map.InExitArea(map.CellOf(listener.position));

        void UpdateSubjective()
        {
            if (map == null || caught) return;
            if (breath.isValid() && player != null) breath.setParameterByID(staminaId, player.Stamina01);
            // RELAY §9.4 / C10: the heartbeat comes from exertion (winded: the slow beat) and the chase (the fast beat,
            // fading over 3 s after it), never from the straight-line distance to an unseen Relay.
            var now = Time.time;
            if (relay != null && relay.Released && relay.State == HunterState.Chase) lastChase = now;
            var chase = Mathf.Clamp01(1f - (now - lastChase) / ChaseFade);
            var exertion = FrontRooms3DGame.PlayerWinded ? .5f : .4f * Mathf.Clamp01(1f - FrontRooms3DGame.PlayerStamina01 / .25f);
            if (FrontRoomsFmod.Finished(ref heartbeat))
            {
                heartbeat = FrontRoomsFmod.Create2D(SoundIds.Heartbeat);
                if (heartbeat.isValid()) heartbeat.start();
            }
            heartbeat.setParameterByID(arousalId, Mathf.Max(chase, exertion));
        }

        // ------------------------------------------------------------ W1 ballast groan (RELAY §9.2)
        /// <summary>A warning burst began in the player's room: the room-tone hum sags with the lamps (driven by WarnBurst only).</summary>
        void OnWarnBurst(Vector3 roomCentre, int lamps)
        {
            if (!FrontRoomsFmod.Ready || map == null || caught || ListenerInSafeZone()) return;   // no warning in the stairwell
            FrontRoomsFmod.Note("warn burst, " + lamps + " lamps");
            FrontRoomsFmod.Caption(lamps > 0 ? "[LIGHTS DIP]" : "[HUM DROPS]", null, 2f);
        }

        /// <summary>The groan follows the burst: in over 0.1 s while its dips play, out over 0.3 s (scaled time, so a pause holds it).</summary>
        void UpdateGroan()
        {
            var target = FrontRooms3DGame.WarnBursting && map != null && !caught && !ListenerInSafeZone() ? 1f : 0f;
            var dt = Time.deltaTime;
            groan = Mathf.MoveTowards(groan, target, dt / (target > groan ? .10f : .30f));
            if (!humBed.isValid() || (groan <= 0f && groanSent <= 0f)) return;
            if (!groanIdReady) { groanId = FrontRoomsFmod.ParameterId(SoundIds.HumBed, SoundIds.Param.Groan); groanIdReady = true; }
            humBed.setParameterByID(groanId, groan);
            groanSent = groan;
        }

        // ------------------------------------------------------------ game events
        void OnMapRunStarted(FrontRoomsMapWorld world, FrontRoomsMapHunter hunter)
        {
            OnMapRunEnded();
            map = world;
            relay = hunter;
            lastChase = float.NegativeInfinity;
            caught = false;
            FrontRoomsFmod.Note("map run started");
            // Hook the run first: whether FMOD has finished loading must never decide whether a run is heard.
            if (started) StartRoomTone();
            map.GlassHold += OnGlassHold;
            map.GlassCracked += OnGlassCracked;
            map.GlassHoldReleased += OnGlassHoldReleased;
            map.GlassBroken += OnGlassBroken;
            map.DoorBroken += OnDoorBroken;
            map.DoorLocked += OnDoorLocked;
            map.DoorHandleTurned += OnDoorHandleTurned;
            map.DoorLatched += OnDoorLatched;
            map.KeyTaken += OnKeyTaken;
            relay.StateChanged += OnRelayState;
            relay.DoorBlow += OnDoorBlow;
            relay.Caught += OnCaught;
            relay.StoppedAtSafeDoor += OnRelayStoppedAtSafeDoor;
            if (relayBody != null) { relayBody.hunter = relay; relayBody.map = map; }
        }

        /// <summary>Run voices and parameter ids, created once FMOD is ready (and again after a catch).</summary>
        void EnsureRunVoices()
        {
            if (map == null) return;
            if (!runIdsReady)
            {
                staminaId = FrontRoomsFmod.ParameterId(SoundIds.Breath, SoundIds.Param.Stamina);
                arousalId = FrontRoomsFmod.ParameterId(SoundIds.Heartbeat, SoundIds.Param.Arousal);
                stressProgressId = FrontRoomsFmod.ParameterId(SoundIds.WindowStress, SoundIds.Param.Progress);
                runIdsReady = true;
            }
            if (!caught && FrontRoomsFmod.Finished(ref breath))
            {
                breath = FrontRoomsFmod.Create2D(SoundIds.Breath);
                if (breath.isValid()) breath.start();
            }
        }

        void OnMapRunEnded()
        {
            if (map != null)
            {
                map.GlassHold -= OnGlassHold;
                map.GlassCracked -= OnGlassCracked;
                map.GlassHoldReleased -= OnGlassHoldReleased;
                map.GlassBroken -= OnGlassBroken;
                map.DoorBroken -= OnDoorBroken;
                map.DoorLocked -= OnDoorLocked;
                map.DoorHandleTurned -= OnDoorHandleTurned;
                map.DoorLatched -= OnDoorLatched;
                map.KeyTaken -= OnKeyTaken;
            }
            if (relay != null)
            {
                relay.StateChanged -= OnRelayState;
                relay.DoorBlow -= OnDoorBlow;
                relay.Caught -= OnCaught;
                relay.StoppedAtSafeDoor -= OnRelayStoppedAtSafeDoor;
            }
            map = null;
            relay = null;
            FrontRoomsFmod.Stop(ref breath, true);
            FrontRoomsFmod.Stop(ref heartbeat, true);
            FrontRoomsFmod.Stop(ref windowStress, true);
        }

        void OnGlassHold(Vector3 position, float progress)
        {
            if (!windowStress.isValid())
            {
                windowStress = FrontRoomsFmod.Create(SoundIds.WindowStress, position);
                if (windowStress.isValid()) windowStress.start();
                if (progress < lastStressProgress) lastStressProgress = 0f;   // a resumed tap keeps its progress: no repeated cracks
            }
            if (windowStress.isValid()) windowStress.setParameterByID(stressProgressId, progress);
            // Cracks come from map.GlassCracked (once per stage per pane, never replayed on a resumed hold or a
            // rebuilt cracked pane); the hold only drives the stress loop.
            lastStressProgress = progress;
        }

        void OnGlassHoldReleased(Vector3 position) => FrontRoomsFmod.Stop(ref windowStress);

        void OnGlassBroken(Vector3 position)
        {
            FrontRoomsFmod.Stop(ref windowStress, true);
            FrontRoomsFmod.OneShot(SoundIds.WindowShatter, position);
            Cap("[GLASS SHATTERS]", position, 50f);
        }

        void OnDoorBroken(Vector3 position)
        {
            // The leaf is off its hinges now: its motion is the break, not a door being opened.
            FrontRoomsDoorSound.MarkBroken(position);
            FrontRoomsFmod.OneShot(SoundIds.DoorBreak, position);
            Cap("[DOOR BREAKS]", position, 40f);
            FrontRoomsFmod.OneShot(SoundIds.DoorStopLimit, position, SoundIds.Param.Impact, 1f);
            blowCount = 0;
        }

        /// <summary>Caption a sound only if the listener is within its audible range (captions follow what is heard).</summary>
        void Cap(string text, Vector3 position, float range)
        {
            if (listener != null && Vector3.Distance(listener.position, position) <= range) FrontRoomsFmod.Caption(text, position);
        }

        void OnGlassCracked(FrontRoomsMapWorld.Window window, int stage)
        {
            if (window == null) return;
            FrontRoomsFmod.OneShot(SoundIds.WindowCrack, window.position);
            Cap("[GLASS CRACKS]", window.position, 22f);
        }

        static readonly string[] PausedBuses = { SoundIds.BusSfx, SoundIds.BusAmbience, SoundIds.BusMusic, SoundIds.BusSubjective };

        /// <summary>The pause screen freezes the game: everything heard freezes with it, and resumes where it was.</summary>
        void OnPaused(bool paused)
        {
            pausedNow = paused;
            if (!FrontRoomsFmod.Ready) return;
            foreach (var path in PausedBuses)
            {
                try { Check(RuntimeManager.GetBus(path).setPaused(paused), "pause " + path); }
                catch (System.Exception e) { Debug.LogWarning("[FrontRoomsAudio] pause " + path + ": " + e.Message); }
            }
            FrontRoomsFmod.Note(paused ? "paused" : "resumed");
            // S2: a focus loss inside a close-up pauses everything; the resume goes back to the close-up's mix, or to
            // the open mix if the close-up ended under the pause screen.
            if (!paused) ApplyInspect(inspecting);
        }

        // ------------------------------------------------------------ EXAMINE (research/inspect/10_design.md §4.3, S1-S4)
        // Keyed on FrontRooms3DGame.Inspecting only: true at the E press, false as control returns (and on teardown).
        // The world is paused (timeScale 0), so the mix freezes with it: Foley, mechanisms, stingers and the subjective
        // layer pause where they are; the Relay bus holds its voices (the presence drone, the warning coil) 6 dB down
        // with no new steps; the room tone stays, 10 dB down, so the pause still sounds like the place (SOMA's Safe
        // Mode). The 3D voices that keep playing are attenuated from the body, not the travelling close-up camera.
        // Sounding screens hold with their pictures (游戏视觉's C26 world hold), all but the one being inspected: R3
        // (Red 10-29), the close-up of the playing station TV keeps its broadcast running, as its picture does.
        static readonly string[] InspectPausedBuses = { SoundIds.BusFoley, SoundIds.BusMechanism, SoundIds.BusMusic, SoundIds.BusSubjective };
        const float InspectRelayVolume = .501f, InspectAmbienceVolume = .316f;   // -6 dB, -10 dB
        bool inspecting, pausedNow;

        void OnInspecting(bool on)
        {
            if (inspecting == on) return;
            inspecting = on;
            ApplyInspect(on);
            FrontRoomsFmod.Note(on ? "inspect on" : "inspect off");
        }

        void ApplyInspect(bool on)
        {
            if (!FrontRoomsFmod.Ready) return;
            foreach (var path in InspectPausedBuses)
            {
                // Under the pause screen these stay paused: a close-up that ends there (an R restart) must not bring the
                // stingers or the heartbeat back over the menu. OnPaused(false) re-applies the mix on resume.
                try { Check(RuntimeManager.GetBus(path).setPaused(on || pausedNow), "inspect " + path); }
                catch (System.Exception e) { Debug.LogWarning("[FrontRoomsAudio] inspect " + path + ": " + e.Message); }
            }
            try { RuntimeManager.GetBus(SoundIds.BusRelay).setVolume(on ? InspectRelayVolume : 1f); }
            catch (System.Exception e) { Debug.LogWarning("[FrontRoomsAudio] inspect " + SoundIds.BusRelay + ": " + e.Message); }
            try { RuntimeManager.GetBus(SoundIds.BusAmbience).setVolume(on ? InspectAmbienceVolume : 1f); }
            catch (System.Exception e) { Debug.LogWarning("[FrontRoomsAudio] inspect " + SoundIds.BusAmbience + ": " + e.Message); }
            // FrontRoomsInspect.Open sets Current before Inspecting(true) fires; the TV's inspectable sits on its
            // FrontRoomsScreenVideo or under it (游戏视觉, research/inspect/NOTE_1029_R3_station_tv.md).
            var inspected = on && FrontRoomsInspect.Current != null ? FrontRoomsInspect.Current.GetComponentInParent<FrontRoomsScreenVideo>() : null;
            FrontRoomsScreenSound.HoldForInspect(on, inspected);
            // S4: the ears stay on the camera (panning follows the view); distance attenuation stays at the body's eye,
            // the camera's parent under the rig (FrontRoomsCameraRig parents the camera to its eye).
            var ears = listener != null ? listener.GetComponent<StudioListener>() : null;
            if (ears != null) ears.AttenuationObject = on && listener.parent != null ? listener.parent.gameObject : null;
        }

        /// <summary>An FMOD call that returned an error: logged and traced, never thrown into gameplay.</summary>
        static void Check(FMOD.RESULT result, string what)
        {
            if (result == FMOD.RESULT.OK) return;
            FrontRoomsFmod.Note(what + " " + result);
            Debug.LogWarning("[FrontRoomsAudio] " + what + ": " + result);
        }

        // Map doors (A12, double-acting): the lever 0.10 s before the leaf, at the handle on the opener's side; the close
        // compound as the leaf latches. FrontRoomsDoorSound (hinge-driven) plays the breakaway and the swing between.
        // A key unlock that drew the latch raises no DoorHandleTurned: its knob beat was the handle.
        // The stairwell EXIT door (C22) is steel: its maze face carries a crash bar that draws the latch itself, and it
        // shuts on a closer with the steel close. Its stair face has no bar, so an open from inside X keeps the lever.
        void OnDoorHandleTurned(FrontRoomsMapWorld.Door door, Vector3 lockPoint)
        {
            if (map != null && map.IsExitDoor(door) && listener != null && !map.InExitArea(map.CellOf(listener.position)))
            {
                FrontRoomsFmod.OneShot(SoundIds.DoorExitBar, lockPoint);
                FrontRoomsDoorSound.BarPushed(door.position);
                return;
            }
            FrontRoomsFmod.OneShot(SoundIds.DoorHandle, lockPoint);
        }

        void OnDoorLatched(FrontRoomsMapWorld.Door door, Vector3 centre) =>
            FrontRoomsDoorSound.Latched(centre, map != null && map.IsExitDoor(door) ? SoundIds.DoorExitClose : SoundIds.DoorLatchStrike);

        // The stairwell (C22) outlives each floor: its north door latches on its own event.
        void OnNorthDoorLatched(Vector3 point) => FrontRoomsDoorSound.Latched(point);

        void OnDoorLocked(Vector3 position) => FrontRoomsFmod.OneShot(SoundIds.DoorLocked, position);

        void OnKeyTaken(GridCoord zone) => FrontRoomsFmod.OneShot2D(SoundIds.KeyPickup);

        void OnDoorBlow(Vector3 position)
        {
            if ((position - lastBlowDoor).sqrMagnitude > .25f) blowCount = 0;
            lastBlowDoor = position;
            blowCount++;
            // The hunter numbers its blows (BlowIndex 0..BlowCount-1, the last one breaks through);
            // BlowCount shrinks at higher tiers, so the strength is the share of the way through.
            var damage = relay != null && relay.BlowIndex >= 0 && relay.BlowCount > 0
                ? (relay.BlowIndex + 1f) / relay.BlowCount
                : Mathf.Clamp01(blowCount / 5f);
            // A struck leaf jolts on its hinge: that is not someone opening the door.
            FrontRoomsDoorSound.Suppress(position, 1f);
            FrontRoomsFmod.OneShot(SoundIds.DoorBlow, position, SoundIds.Param.Damage, damage);
            Cap("[DOOR BLOWS]", position, 36f);
        }

        // C10 (RELAY §13 items 1 and 2): no 2D Hunt/Search/Chase/Lost stingers and no release clicks. They said
        // "near" at any range; the Relay is now heard only through its own 3D steps and hum (W2, FrontRoomsRelaySound).
        // L11: it stopped outside the stairwell door; its body sound marks the stop (heard only if its steps are).
        void OnRelayStoppedAtSafeDoor(Vector3 feet)
        {
            if (FrontRoomsFmod.Ready && relayBody != null) relayBody.Scuff(feet);
        }

        void OnRelayState(HunterState state) => FrontRoomsFmod.Note("relay " + state);

        void OnCaught()
        {
            caught = true;
            if (!FrontRoomsFmod.Ready) return;
            // Hard cut to silence, then a single ringing tone.
            StopRoomTone(true);
            for (var i = 0; i < fixtureVoices; i++) { FrontRoomsFmod.Stop(ref fixtureVoice[i], true); fixtureLamp[i] = -1; }
            FrontRoomsFmod.Stop(ref breath, true);
            FrontRoomsFmod.Stop(ref heartbeat, true);
            FrontRoomsFmod.Stop(ref windowStress, true);
            RuntimeManager.GetBus(SoundIds.BusSfx).stopAllEvents(STOP_MODE.IMMEDIATE);
            RuntimeManager.GetBus(SoundIds.BusAmbience).stopAllEvents(STOP_MODE.IMMEDIATE);
            RuntimeManager.GetBus(SoundIds.BusMusic).stopAllEvents(STOP_MODE.IMMEDIATE);
            FrontRoomsFmod.OneShot2D(SoundIds.Tinnitus);
        }

        /// <summary>
        /// The unlock shot's lock, beat by beat at the keyhole (FrontRoomsDoorShot.Beat; door_shots_plan §3.2): the key in at
        /// 0.55 s, turned at 0.68, the bolt drawn at 0.86 (the commit, DoorUnlocked's frame), the key out at 0.90 or on a
        /// cancel, and on a push the knob at 0.92 and its spring-back at 1.20. KeyPickup's Pocket stays unbound.
        /// A cancel raises the withdraw even before the key reached the keyway: then nothing comes out, so it is silent.
        /// </summary>
        void OnDoorShotBeat(ShotKind shot, DoorBeat beat, Vector3 at)
        {
            if (shot != ShotKind.Unlock) return;
            var wasIn = keyInLock;
            if (beat == DoorBeat.KeyApproach || beat == DoorBeat.KeyWithdraw) keyInLock = false;
            else if (beat == DoorBeat.KeyInsert) keyInLock = true;
            if (!FrontRoomsFmod.Ready || map == null || caught) return;
            string id;
            switch (beat)
            {
                case DoorBeat.KeyInsert: id = SoundIds.LockKeyInsert; break;
                case DoorBeat.KeyTurn: id = SoundIds.LockKeyTurn; break;
                case DoorBeat.BoltRetract: id = SoundIds.LockBoltRetract; break;
                case DoorBeat.KeyWithdraw: if (!wasIn) return; id = SoundIds.LockKeyWithdraw; break;
                case DoorBeat.KnobTurn: id = SoundIds.LockKnobTurn; break;
                case DoorBeat.KnobReturn: id = SoundIds.LockKnobReturn; break;
                default: return;
            }
            FrontRoomsFmod.OneShot(id, at);
        }

        /// <summary>
        /// HUD B's cues (FrontRoomsKeyHud.SoundCue), raised on the HUD's own clock at the start of each motion: 2D, on the
        /// UI bus, so they follow the panel, not the world. The unlock's push and click are left to the 3D lock beats.
        /// </summary>
        void OnHudSoundCue(FrontRoomsHudSoundCue cue)
        {
            if (!FrontRoomsFmod.Ready || caught) return;
            string id;
            switch (cue)
            {
                case FrontRoomsHudSoundCue.PickupFlight: id = SoundIds.HudKeyFlight; break;
                case FrontRoomsHudSoundCue.PickupLand: id = SoundIds.HudKeyLand; break;
                case FrontRoomsHudSoundCue.LockTug: id = SoundIds.HudLockTug; break;
                case FrontRoomsHudSoundCue.UnlockDrop: id = SoundIds.HudUnlockDrop; break;
                case FrontRoomsHudSoundCue.BrokenBuild: id = SoundIds.HudBrokenBuild; break;
                case FrontRoomsHudSoundCue.BrokenKnock: id = SoundIds.HudBrokenKnock; break;
                default: return;
            }
            FrontRoomsFmod.OneShot2D(id);
        }

        void OnPlayerClimbed(Vector3 position) => FrontRoomsFmod.OneShot(SoundIds.Cloth, position);

        // ------------------------------------------------------------ web audio
        void UnlockWebAudio()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (webUnlocked || !RuntimeManager.IsInitialized) return;
            if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
            {
                RuntimeManager.CoreSystem.mixerSuspend();
                RuntimeManager.CoreSystem.mixerResume();
                webUnlocked = true;
            }
#endif
        }
    }
}
