using System;
using System.Collections.Generic;
using System.Reflection;
using FrontRooms.Map;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Builds the generated Level 0 map as geometry around the player. The main
/// game creates it embedded (CreateEmbedded, then Begin) after the title's
/// noclip; FrontRoomsMapTest.unity runs it standalone with its own walker.
/// It keeps the chunks within buildRadius built (one new chunk per frame,
/// nearest first) and drops the rest; a chunk the player comes back to after
/// shiftAfterSeconds is rebuilt with a shifted interior. Every fixture runs its
/// own flicker. Office rooms and large halls are furnished through
/// FrontRoomsOfficeKit and FrontRoomsFurniturePile when those exist.
/// C22 (floors): a run keeps one map. Each floor leaves its EXIT's cell to
/// the stairwell (SetExitArea), and NextFloor reseeds the same map in place
/// for the floor below.
/// </summary>
public sealed class FrontRoomsMapWorld : MonoBehaviour
{
    // Sizes come from the modular unit spec (FrontRoomsModuleUnits.cs).
    const float WallThickness = ModuleUnits.WallThickness;
    const int BlockCells = 2;
    const float DoorWidth = ModuleUnits.DoorWidth, DoorHeight = ModuleUnits.DoorHeight;
    const float WindowWidth = ModuleUnits.WindowWidth, WindowSill = ModuleUnits.WindowSill, WindowTop = ModuleUnits.WindowTop;
    // Mesh-UV repeats. Only the fallback materials read them: the surface
    // materials are world-projected and use their own tile sizes.
    const float WallpaperRepeat = 1.2f, CarpetRepeat = 1.5f, CeilingRepeat = 1.2f;

    [SerializeField, Tooltip("The level's numbers (Assets/Levels). Empty: the code defaults.")] FrontRoomsLevelProfile profile;
    [SerializeField, Tooltip("Pick a new seed every time Play starts (standalone test scene only).")] bool randomSeedOnPlay = false;
    [SerializeField, Tooltip("On: the test scene, which spawns its own walker and sets fog. The main game creates the map embedded instead.")] bool standalone = true;

    // Working copies of the profile, taken when the map starts, so the asset
    // is never changed by a run.
    MapSettings settings;
    List<RoomModuleData> moduleLibrary;
    int buildRadius, chunksPerFrame;
    float shiftAfterSeconds, lightRadius, shadowRadius, pileChance;
    bool doorsNeedKeys, dressOffices;
    // The profile's tier table itself (not a copy): edits to it reach chunks built afterwards.
    FrontRoomsTierRules tierRules;

    public FrontRoomsMapCache Cache { get; private set; }

    /// <summary>
    /// The run's difficulty tier given to chunks generated from now on (1 = base):
    /// their module tier and Auto lamp odds. Chunks already generated keep theirs,
    /// so a rebuild is identical, and a revisit shift keeps the tier of the
    /// chunk's first generation (D6; V1, 2026-10-29: Cache.TierOf). Each zone
    /// keeps the tier the cache first met it at (Cache.ZoneTier).
    /// </summary>
    public int GenerationTier
    {
        get => Cache != null ? Cache.Tier : 1;
        set { if (Cache != null) Cache.Tier = Mathf.Max(1, value); }
    }
    public Transform Player => player;
    public int ShiftedChunks { get; private set; }
    public int KeysHeld => keysHeld.Count;
    /// <summary>The assigned profile, or the code defaults.</summary>
    public FrontRoomsLevelProfile Profile
    {
        get => profile != null ? profile : FrontRoomsLevelProfile.Default;
        set => profile = value;
    }
    public int Seed => settings != null ? settings.seed : Profile.generation.seed;
    public int BuiltChunkCount => built.Count;
    public int BuildRadius => settings != null ? buildRadius : Mathf.Max(1, Profile.buildRadius);
    public Color FogColor => FrontRoomsLook.FogColor;
    /// <summary>Topology revision for level-design path fields; streaming and edge-state changes advance it.</summary>
    public int PassageRevision { get; private set; }
    /// <summary>Camera far plane: just short of the first chunk that may not be built yet.</summary>
    public float SightDistance => BuildRadius * MapGrid.ChunkSize - 2f;

    /// <summary>A door's leaf started to move (opened or shut by the player): a noise at the door. An open raises it after the lever, when the leaf leaves the frame.</summary>
    public event Action<Vector3> DoorMoved;
    /// <summary>The player broke a window: the loudest noise in the game.</summary>
    public event Action<Vector3> GlassBroken;
    /// <summary>The Relay broke a door down.</summary>
    public event Action<Vector3> DoorBroken;
    /// <summary>
    /// The Relay broke a door down: the door, where the Relay stood, and true
    /// when it stood on the side the leaf swung into (the leaf came toward it)
    /// rather than the other (it burst the leaf away). Raised right after
    /// DoorBroken. Since A12 a shut leaf is always burst away from the Relay,
    /// so true only for a leaf caught partly open on the Relay's side.
    /// </summary>
    public event Action<Door, Vector3, bool> DoorBrokenFrom;
    /// <summary>
    /// A pull: the player shuts a door standing in the leaf's way, on the side
    /// it is open to. The door, and a clear standing point on the floor on the
    /// latch side, out of the leaf's sweep and inside the door's keep-clear
    /// strip. Raised as the leaf starts back, and only when the shut would meet
    /// the body (a player already clear is not moved), for the game to step the
    /// player there. A leaf that meets a body stops on it. Opens never pull
    /// (A12, Red 2026-10-22: a leaf always opens away from its opener).
    /// </summary>
    public event Action<Door, Vector3> DoorPulled;
    /// <summary>A shut leaf landed in its frame and latched (the end of a shut swing): the door, at its centre.</summary>
    public event Action<Door, Vector3> DoorLatched;
    /// <summary>
    /// The lever: the moment a door is opened (Use, TryOpenDoor, a key turn's release), before
    /// the latch frees and the leaf moves (DoorMoved, Open.SwingStart later; opens never pull since A12). Carries
    /// the lock and handle point on the opener's side, so the handle and latch sound land before the swing.
    /// Not raised when the latch is already drawn (the push-side unlock: its knob beat was the handle).
    /// </summary>
    public event Action<Door, Vector3> DoorHandleTurned;
    /// <summary>
    /// Every open, where DoorHandleTurned is raised (also when a drawn latch leaves it out): the door,
    /// what opened it, and whether the latch was already drawn (no lever wait, no DoorHandleTurned). Lets
    /// the sound skip its own handle on an unlock's open, and the game give an unlock's swing its own
    /// Relay radius once that is decided (RELAY_PURSUIT_REDESIGN §6). DoorMoved's 14 m noise is unchanged.
    /// </summary>
    public event Action<Door, DoorOpenSource, bool> DoorOpenBegan;
    /// <summary>
    /// The player picked up a zone key, once per zone. On the instant path (no key presenter, or one that
    /// shows no model: shipped play today) in the same call as KeyPickupStarted, at contact; when a presenter
    /// shows the key, at its arrival beat (KeyArrived, KeyPickup.Arrive after contact). The floor key (M3) is
    /// named FloorKeyZone here and in KeyPickupStarted, with StationKeyTaken raised between the two.
    /// </summary>
    public event Action<GridCoord> KeyTaken;
    /// <summary>
    /// The player reached a zone key (walked onto it, or E on it with KeyPickupNeedsE): the zone, and the
    /// key's world position at contact. Raised on every pickup, just before KeyTaken on the instant path.
    /// The key is held from here (HasKeyFor), whatever its presentation does.
    /// </summary>
    public event Action<GridCoord, Vector3> KeyPickupStarted;

    /// <summary>
    /// Who shows a picked-up key (the game's FrontRoomsDoorShot.BeginKeyPickup): the zone and the key's
    /// object. True: it takes the object over (the map moves it to its own root, so a chunk drop or rebuild
    /// cannot kill it) and calls KeyArrived when the key reaches the held pose. Null (tests, tools, the
    /// walker) or false (no model to show): the instant path, the object gone and KeyTaken at contact.
    /// </summary>
    public Func<GridCoord, Transform, bool> KeyPresenter { get; set; }

    /// <summary>
    /// Look+E keys (plan §3.3, built and off; later a profile bool): keys spawned while it is on carry a
    /// trigger the game's key query finds (KeyOnRay), Describe says "E  ·  TAKE KEY", Use takes them, and
    /// walking over them does not. No gameplay query sees a trigger (they all pass
    /// QueryTriggerInteraction.Ignore) and a trigger never blocks a body. Off: no collider on any key.
    /// </summary>
    public bool KeyPickupNeedsE { get; set; }

    /// <summary>
    /// Editor and dev only: with no key model, the presenter shows the cube key itself, shrunk to
    /// KeyPlaceholderSize, travelling to the held pose (timing captures). Off in shipped play, so a pickup
    /// stays instant; the game sets it at run start from FrontRooms/Map/Show placeholder key (never for the
    /// autopilot, whose door scenario turns it on for one case).
    /// </summary>
    public static bool ShowPlaceholderKey { get; set; }

    /// <summary>The placeholder key's size while presented (m): a real key's envelope, the cube shrunk to it over the travel.</summary>
    public static readonly Vector3 KeyPlaceholderSize = new Vector3(.065f, .022f, .004f);
    /// <summary>Every frame E is held on glass: the window's position and how far it is to breaking (0-1).</summary>
    public event Action<Vector3, float> GlassHold;
    /// <summary>E was let go before the glass broke (or, in tap mode, a tap's credit ran out). The progress is kept either way.</summary>
    public event Action<Vector3> GlassHoldReleased;
    /// <summary>
    /// The glass break (visual chat's plan §4.4, Red 2026-10-03). E went down on
    /// a pane: the window, the impact (the base-eye hit, kept 0.2 m inside the
    /// exposed glass; fixed once the pane has cracked) and the break's seed.
    /// </summary>
    public event Action<Window, Vector3, int> GlassHoldStarted;
    /// <summary>Every frame E is held on glass, with the window (GlassHold, kept for the sound layer, carries only its position).</summary>
    public event Action<Window, float> WindowHeld;
    /// <summary>The pane cracked further: stage 1 at 0.35, 2 at 0.70 of the hold. Once each; a resumed hold never replays one.</summary>
    public event Action<Window, int> GlassCracked;
    /// <summary>The pane gave (with GlassBroken): the window, the impact, and the strike's impulse on the pieces (base-eye forward at E-down × 3 m/s).</summary>
    public event Action<Window, Vector3, Vector3> WindowShattered;
    /// <summary>A window was built (broken ones too, with no pane): its break record, to restore the glass.</summary>
    public event Action<Window, GlassBreakRecord> WindowBuilt;
    /// <summary>A built window goes (its chunk dropped or rebuilt): clean up what hangs on its root.</summary>
    public event Action<Window> WindowReleased;
    /// <summary>The player tried a door that needs this zone's key (at the door's centre). Not raised again for a try within Rattle.Duration of the last.</summary>
    public event Action<Vector3> DoorLocked;
    /// <summary>The same try as DoorLocked, right after it: the door, and the lock point on the trier's face (LockPoint).</summary>
    public event Action<Door, Vector3> DoorRattled;
    /// <summary>
    /// A held key unlocked a locked door (doors need keys), once per door, for
    /// good from both sides: the door, the zone whose key opened it, and the
    /// keyhole on the opener's face (LockPose). With a key-turn owner (the
    /// game's door shot) it is raised at the shot's commit (Unlock.Commit,
    /// 0.86 s), never for a turn cancelled before it, and the open comes after
    /// (ReleaseKeyTurn). Without one (the walker, tools, TryOpenDoor) the
    /// door unlocks and opens in the same call.
    /// </summary>
    public event Action<Door, GridCoord, Vector3> DoorUnlocked;
    /// <summary>
    /// E on a broken door (Use answered Broken): the door and where the player stands. For the HUD's
    /// won't-shut reaction and a hanging-leaf Foley; never a Relay noise (RELAY §10, broken doors).
    /// Not raised again for a try within BrokenDoor.Duration of the last.
    /// </summary>
    public event Action<Door, Vector3> DoorBrokenTried;

    /// <summary>What E on something did (Use's answer). Broken: a broken door, which nothing opens or shuts again (Red, 2026-10-14).</summary>
    public enum UseResult { None, Opened, Shut, Rattled, KeyTurn, KeyTaken, Broken }

    /// <summary>
    /// What the crosshair is on (Kind), so the HUD and the touch layer key their states on a kind, never
    /// on the prompt string (crosshair grammar H1, research/computer_ui/30_crosshair_grammar.md §7).
    /// Append only. Open / Shut: a door E opens or shuts. Locked / LockedHaveKey: a shut door that needs
    /// a key, without or with it. Broken: a broken door (it has no prompt). Glass / GlassTap: a pane,
    /// held or tapped (Tap to break). TakeKey: a look+E key. Read / Examine: EXAMINE's aim registry.
    /// </summary>
    public enum AimKind { None, Open, Shut, Locked, LockedHaveKey, Broken, Glass, GlassTap, TakeKey, Read, Examine }

    /// <summary>
    /// What opened a door (DoorOpenBegan). Append only. Relay: Relay v2's OPEN (OpenAjar), never a noise to the
    /// Relay or the director (source Relay); nothing raises it until FRONTROOMS_RELAY_V2.
    /// </summary>
    public enum DoorOpenSource { Use, Tools, Unlock, Relay }

    /// <summary>
    /// The key turn's owner (the game's FrontRoomsDoorShot). While it is set, E on a keyable door
    /// holds the door for the unlock shot (Use returns KeyTurn) and the owner drives the commit and
    /// the release; it must call RefreshKeyTurn every tick, or the watchdog resolves the hold. Null
    /// (the walker, the Level Designer preview, tools, tests): a keyable door unlocks and opens at once.
    /// </summary>
    public object KeyTurnOwner { get; set; }

    /// <summary>
    /// The game is paused (or the run is over): door motion, leaf jolts and the key-turn watchdog
    /// freeze with it, so nothing swings (or sounds, FrontRoomsDoorSound reads the hinge) under
    /// the pause card. The Level Designer preview has no game and leaves it false.
    /// </summary>
    public bool DoorsPaused { get; set; }

    // ---------- Doors: double-acting (A12, Red 2026-10-22) ----------
    // A leaf swings both ways (no stops) and always away from whoever moves it:
    // from the frame it goes to the far side of its opener, and a leaf out of
    // the frame goes on away from them (further open, or back through the
    // frame when they stand ahead of it). It stays where it is left and
    // latches when shut. The side it went to is kept by edge (doorSides) for
    // rebuilds. The beats come from FrontRoomsShotTimings (Open, Rattle,
    // DoorBreak); these are the map's own.

    /// <summary>The old single-acting pull's extra wait after the lever. No open waits for it since A12 (opens never pull); kept for readers of the constant.</summary>
    public const float DoorPullBeatSeconds = .25f;
    /// <summary>
    /// The game's pull step (a shut from the leaf's side, DoorPulled): the
    /// player walks to the clear spot at PullStepSpeed (m/s) for at most
    /// PullStepSeconds. A spot further than PullStepSpeed × PullStepSeconds is
    /// not stepped to.
    /// </summary>
    public const float PullStepSeconds = .35f, PullStepSpeed = 4f;
    /// <summary>A built door is a way through (PassageBetween Open) from this leaf angle: the opening left beside the leaf is 0.64 m, so a 0.6 m body fits.</summary>
    public const float DoorPassableDegrees = 70f;
    /// <summary>The Relay's break throws the leaf this far, past the 95° stop, before it bounces back to DoorBreak.BounceRestDeg.</summary>
    public const float DoorBreakThrowDegrees = 105f;
    /// <summary>M4 (door break): a dressed double-acting leaf's throw ends here: past it the leaf's face runs into the casing's reveal arris (2 mm clear at 95.4°, 17 mm into it at 105°; pivot Z 0.036, 44 mm leaf, arris X 0.105). The cube leaf (EXIT, WebGL) has no casing to meet and keeps DoorBreakThrowDegrees.</summary>
    public const float DoorBreakStopDegreesDA = 95.4f;
    /// <summary>The bounce from the throw to rest, after DoorBreak.ThrowSeconds. The hunter waits for both before walking on.</summary>
    public const float DoorBreakBounceSeconds = .18f;
    /// <summary>The most a hinge turns in one frame, whatever the frame time (FrontRoomsDoorSound reads a jump over 45° as a recycle snap).</summary>
    public const float DoorMaxStepDegrees = 40f;

    /// <summary>
    /// Relay v2's OPEN leaves a door this far open (RELAY 5, 10; Ajar 30 deg, decided 2026-10-08): short of
    /// DoorPassableDegrees, so no body passes, and it costs AjarOpening in the path field.
    /// </summary>
    public const float AjarDegrees = 30f;
    /// <summary>Relay v2's OPEN: the push from the frame to AjarDegrees, in seconds (RELAY 5: a latch click and a 1.0 s push).</summary>
    public const float RelayOpenSeconds = 1f;

    /// <summary>
    /// What lies between two side-by-side cells right now. Append only. Ajar: a door the Relay OPENed (OpenAjar)
    /// and nobody has opened fully since: no way through for a body until it is pushed past DoorPassableDegrees.
    /// Only OpenAjar makes one, so nothing answers Ajar until FRONTROOMS_RELAY_V2.
    /// </summary>
    public enum Passage { Open, Wall, ClosedDoor, Glass, Ajar }

    sealed class Fixture
    {
        public Light light;
        public Renderer panel;
        public Color emission;
        public float baseIntensity;
        public bool castsShadow;
        public int mode;
        public uint rng;
        public float clock;
        public float nextEvent;
        public float eventEnd;
        public float phase;
        public float level = 1f;
        // The lamp-override layer: the lamp's cell, its level before overrides,
        // and the band of its last reported level (FixtureChanged; -1 = none yet).
        public GridCoord cell;
        public float baseLevel = 1f;
        public sbyte band = -1;
        // V1 (lamp check 10_lamp_look_check.md): the override layer's multiplier this frame, level / baseLevel
        // while the lamp's own level is lit; exactly 1 with no override (Dead and Off lamps too). Written to the
        // lens as _FR_LampDip, so the visual chat's lens shader can dip with the light.
        public float dip = 1f;
        // Near the start area (its light would reach through the stream rooms' walls): 1 north of the door line, 2 south of it.
        public byte startGroup;
        // C22: within its light's range of X, the EXIT's stairwell cell (its light would reach through the stairwell's walls): held by ExitLamps.
        public bool exitGroup;
        // WebGL (TickFixturesNearOnly): the light's position relative to its chunk
        // root (chunk roots only translate), what this map last set on the Light,
        // and the lens emission factor last written, so a far lamp costs no native
        // call per frame.
        public Vector3 offset;
        public bool lightOn = true;
        public LightShadows shadowMode = LightShadows.None;
        public float emissionWritten = -1f;
        // The ambient flicker (M2, RELAY §9.5), modes 1-3 only: the dip's gain on the mode's level this frame
        // (1: none); Stutter's episode (2 or 3 dips, 0 = none) and its shape seed; Failing's dropout; Dead's
        // strike (shown, or held dark) and the last one shown; SetAmbientHold; the flash limiter's track.
        public float gain = 1f;
        public byte episodeDips;
        public float episodeStart;
        public uint episodeSeed;
        public bool dropping;
        public float dropStart = float.NegativeInfinity, dropEnd = float.NegativeInfinity;
        public bool strikeShown;
        public float lastStrike = float.NegativeInfinity;
        public bool ambientHold;
        public FlashTrack flash;
        // M1 (lamp check 10_lamp_look_check.md): the cell's look hold (SetLampHold), null when none.
        public LampHold hold;
    }

    // M1: a Stutter or Failing lamp's look held at Steady or Dim (SetLampHold), kept by cell. weight: how far the
    // lamp has gone to the held look (0..1); source: what it comes from, the level it showed when the hold began
    // (0) or its own mode's calm level (1); in / out: the crossfades' seconds; delay: the wait before it lets go;
    // quiet: how long it has been back at its own level with the record still kept.
    sealed class LampHold
    {
        public GridCoord cell;
        public ModuleLamp look;
        public float weight, source, from, inSeconds, outSeconds, delay, quiet;
        public bool releasing;
    }

    // The flash limiter's state for one lamp: a zigzag over its level (turning points FlashStep apart), whether
    // the current swing has been counted, the times of the last FlashMaxTransitions counted swings (a ring,
    // oldest at head), the last level given out, and how many frames it has held one back (tools).
    sealed class FlashTrack
    {
        public bool begun;
        public sbyte dir;
        public float pivot, ext, lo, hi, last;
        public bool counted;
        public readonly float[] times = new float[FlashMaxTransitions];
        public int count, head, held;
    }

    public sealed class Door
    {
        public Transform hinge;
        public Quaternion closed;
        public Collider leaf;
        public GridCoord a, b;
        public long edge;
        public Vector3 position;
        /// <summary>Open or opening: set the moment it is opened (before the lever), cleared the moment it is shut (while the leaf still closes).</summary>
        public bool open;
        public bool broken;
        /// <summary>The hinge angle over 95°, held to 0..1 (older readers; <see cref="angle"/> is the leaf).</summary>
        public float progress;
        /// <summary>
        /// +1 or -1: the side the leaf swings into now. With +1 an east edge's
        /// leaf goes west (into a) and a north edge's goes north (into b). A12:
        /// set away from whoever moves the leaf while it is in the frame
        /// (SwingAwayFrom), turned over when it is pushed back through the
        /// frame, and kept by edge (doorSides) for rebuilds; a door never
        /// opened has its edge's hash (FixedSwing).
        /// </summary>
        public float swing = 1f;
        /// <summary>The hinge angle in degrees, on the swing side: 0 shut and latched, 95 open on its rest, past it only in a break throw.</summary>
        public float angle;
        /// <summary>The last shut came from the side the leaf is open to (a pull). Opens never pull (A12).</summary>
        public bool pull;

        // The swing in progress (TickDoors): what it is, the lever's wait, and its curve.
        internal DoorMotion motion;
        internal float wait, clock, from, to, seconds, shutPhase;
        internal bool bump;
        /// <summary>The visual chat's door rig (FrontRoomsKitSeam), or null until DressDoor lands (D1.6): the shots then fall back to the cube leaf.</summary>
        internal IFrontRoomsDoorRig rig;
        // The map clock (TickDoors) at the last locked rattle: a try within Rattle.Duration of it does nothing.
        internal float rattledAt = float.NegativeInfinity;
        // The map clock at the last E on it once broken: a try within BrokenDoor.Duration of it does nothing.
        internal float brokenTriedAt = float.NegativeInfinity;
        // The leaf child: where it hangs when whole, how crooked a broken one
        // hangs, the jolt playing (clock, size, spring) and the jolts to come.
        internal Vector3 leafHome;
        internal float crooked;
        // C23 round 2: an unscaled frame on the hinge that follows the leaf child (PoseLeaf), for hardware on the
        // leaf (the EXIT door's crossbar; round 3: the visual dressing, ExitDoorBuilt's "leaf"); the EXIT door
        // always has one (MountLeafRig), every other door none. At home it is the hinge's own frame.
        internal Transform leafRig;
        internal float joltClock = -1f, joltMetres, joltDegrees, joltSpring;
        internal readonly List<(float at, float metres, float degrees, float spring)> jolts = new List<(float, float, float, float)>();
    }

    // Through (A12): a leaf pushed from ahead of it runs back to the frame, then opens to the far side.
    internal enum DoorMotion { None, Lever, Swing, Shut, Throw, Bounce, Through }

    public sealed class Window
    {
        /// <summary>The gameplay box `Window pane {a}-{b}` (its collider is what E aims at); null once broken.</summary>
        public GameObject pane;
        /// <summary>`Window {a}-{b}`: unscaled, the opening's centre on the wall line at floor level, +Z into cell b. Built for broken windows too.</summary>
        public Transform root;
        public GridCoord a, b;
        public long edge;
        public Vector3 position;
        public float hold;
        // Pushed this frame or since the last pause (tap mode pauses between taps, keeping the progress).
        public bool pushing;
        /// <summary>The break so far: 0 intact, 1 and 2 cracked, 3 shattered (also kept by edge, GlassBreakRecord).</summary>
        public int stage;
        // A hold under way (BeginGlassHold until a release or the shatter), and the strike's direction.
        internal bool started;
        internal Vector3 strike;
        // WindowBuilt has gone out for it (so WindowReleased does too).
        internal bool announced;
    }

    /// <summary>
    /// A window's break, kept by edge like the broken windows, so a dropped,
    /// rebuilt or shifted chunk restores it. Stage 0 intact, 1 and 2 cracked,
    /// 3 shattered. The seed comes from the map seed and the edge. The impact
    /// is in the window root's frame (metres: x across, y up from the floor);
    /// impactUV is the same point over the exposed glass (1.367 × 1.617 m from
    /// x −0.6835, y 0.3665). Rotation (degrees) turns the crack pattern, from the
    /// seed. Side is +1 when struck from cell a (the glass goes toward b, the
    /// root's +Z), −1 from b. The impact is fixed once the pane has been
    /// struck (Red 2026-10-22: a knock is never undone). Hold is the progress
    /// so far (seconds of E held, 0–1 × HoldSeconds): kept through a release,
    /// a chunk drop and a rebuild, so the next hold resumes exactly there.
    /// </summary>
    public struct GlassBreakRecord
    {
        public int stage;
        public int seed;
        public Vector2 impact;
        public Vector2 impactUV;
        public float rotation;
        public int side;
        public float hold;
    }

    // The exposed glass in the window root's frame (the visual chat's 16 mm stops hide the rest of the 1.4 × 1.65 box).
    const float GlassExposedHalfWidth = .6835f, GlassExposedBottom = .3665f, GlassExposedTop = 1.9835f;

    sealed class BuiltChunk
    {
        public GameObject root;
        // Meshes this map generated for the chunk (shell blocks, collision): freed with it.
        public readonly List<Mesh> meshes = new List<Mesh>();
        public readonly List<Fixture> fixtures = new List<Fixture>();
        public readonly List<Door> doors = new List<Door>();
        public readonly List<Window> windows = new List<Window>();
        // Keys: a key on a module's key spot lies still; the others spin.
        public readonly List<(GameObject go, GridCoord zone, bool spin)> keys = new List<(GameObject, GridCoord, bool)>();
        // Module Relay entries in this chunk: world position on the floor, and the marker's tag.
        public readonly List<(Vector3 pos, string tag)> relayEntries = new List<(Vector3, string)>();
        // C23 step 1: the EXIT sign over the EXIT door, in X's chunk only (MountExitSign); goes with the chunk's objects.
        public FrontRoomsExitSign exitSign;
        // C23 round 2: the EXIT leaf's crossbar (MountExitCrossbar), on the leaf rig under the EXIT door's hinge.
        public GameObject exitCrossbar;
        // C23 round 3: the EXIT door and its "EXIT frame N-X" object, when this chunk built the EXIT edge (BuildEdge);
        // Build raises ExitDoorBuilt for them once the chunk is registered.
        public Door exitDoor;
        public Transform exitFrame;
    }

    sealed class MeshBuilder
    {
        public readonly List<Vector3> vertices = new List<Vector3>();
        public readonly List<Vector3> normals = new List<Vector3>();
        public readonly List<Vector2> uvs = new List<Vector2>();
        public readonly List<int> triangles = new List<int>();

        public void Box(Vector3 center, Vector3 size, Vector3 worldOffset, float repeat)
        {
            var h = size * .5f;
            Face(center + Vector3.right * h.x, Vector3.right, Vector3.up, Vector3.forward, h.y, h.z, worldOffset, repeat);
            Face(center - Vector3.right * h.x, Vector3.left, Vector3.forward, Vector3.up, h.z, h.y, worldOffset, repeat);
            Face(center + Vector3.up * h.y, Vector3.up, Vector3.forward, Vector3.right, h.z, h.x, worldOffset, repeat);
            Face(center - Vector3.up * h.y, Vector3.down, Vector3.right, Vector3.forward, h.x, h.z, worldOffset, repeat);
            Face(center + Vector3.forward * h.z, Vector3.forward, Vector3.right, Vector3.up, h.x, h.y, worldOffset, repeat);
            Face(center - Vector3.forward * h.z, Vector3.back, Vector3.up, Vector3.right, h.y, h.x, worldOffset, repeat);
        }

        // u x v points along the normal, so (0,1,2)(0,2,3) winds clockwise as seen from the front.
        void Face(Vector3 c, Vector3 normal, Vector3 u, Vector3 v, float uHalf, float vHalf, Vector3 worldOffset, float repeat)
        {
            var i = vertices.Count;
            Corner(c - u * uHalf - v * vHalf, normal, worldOffset, repeat);
            Corner(c + u * uHalf - v * vHalf, normal, worldOffset, repeat);
            Corner(c + u * uHalf + v * vHalf, normal, worldOffset, repeat);
            Corner(c - u * uHalf + v * vHalf, normal, worldOffset, repeat);
            triangles.Add(i); triangles.Add(i + 1); triangles.Add(i + 2);
            triangles.Add(i); triangles.Add(i + 2); triangles.Add(i + 3);
        }

        void Corner(Vector3 p, Vector3 normal, Vector3 worldOffset, float repeat)
        {
            vertices.Add(p);
            normals.Add(normal);
            var w = p + worldOffset;
            // World-space planar UVs: the print keeps one scale on every wall
            // and continues across chunk borders.
            var uv = Mathf.Abs(normal.x) > .5f ? new Vector2(w.z, w.y) : Mathf.Abs(normal.y) > .5f ? new Vector2(w.x, w.z) : new Vector2(w.x, w.y);
            uvs.Add(uv / repeat);
        }

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name };
            if (vertices.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }
    }

    sealed class ThemeMaterials
    {
        public Material wall, floor, ceiling, lens;
        public float lampIntensity;
        // N1/V5 lightlead: the tube colour belongs to the room theme.
        public Color lampColor = new Color(1f, .96f, .88f);
    }

    readonly Dictionary<GridCoord, BuiltChunk> built = new Dictionary<GridCoord, BuiltChunk>();
    readonly Dictionary<GridCoord, float> droppedAt = new Dictionary<GridCoord, float>();
    // Chunks whose build threw: left out (not retried every frame) until they leave the build radius.
    readonly HashSet<GridCoord> failedChunks = new HashSet<GridCoord>();
    readonly HashSet<GridCoord> keysHeld = new HashSet<GridCoord>();
    // The zones whose KeyTaken has been raised (KeyArrived announces each once), and the look+E keys' triggers by zone.
    readonly HashSet<GridCoord> keysAnnounced = new HashSet<GridCoord>();
    readonly Dictionary<Collider, GridCoord> keyByCollider = new Dictionary<Collider, GridCoord>();
    readonly RaycastHit[] keyHits = new RaycastHit[16];
    readonly HashSet<long> brokenWindows = new HashSet<long>();
    // Window breaks by edge (stage, seed, impact): kept through drops, rebuilds and shifts.
    readonly Dictionary<long, GlassBreakRecord> glassBreaks = new Dictionary<long, GlassBreakRecord>();
    // The glass kit's component on each window root, when the visual chat's glass exists (found by reflection).
    static Type glassBreakable;
    static bool glassBreakableResolved;
    readonly HashSet<long> openDoors = new HashSet<long>();
    // Relay v2: doors the Relay OPENed and left Ajar (OpenAjar), by edge, until the player opens one fully, a
    // break, a tools reset or the floor goes. Empty on v1 (nothing calls OpenAjar).
    readonly HashSet<long> ajarDoors = new HashSet<long>();
    readonly HashSet<long> brokenDoors = new HashSet<long>();
    // M2 (door break): the blows a door has taken while it still stands, kept by edge like brokenDoors (a rebuild keeps them).
    readonly Dictionary<long, int> keptBlows = new Dictionary<long, int>();
    // A12: the side (Door.swing) each door's leaf last went to, by edge, so an open or broken door is rebuilt on it.
    readonly Dictionary<long, float> doorSides = new Dictionary<long, float>();
    // Lamp modes set at run time (SetLampMode): kept by cell like brokenDoors, so a dropped,
    // rebuilt or shifted chunk comes back with them. Never in MapChunk.lamp (Shift regenerates it).
    readonly Dictionary<GridCoord, ModuleLamp> lampModes = new Dictionary<GridCoord, ModuleLamp>();
    // Lamps whose Dead strikes a warning holds dark (SetAmbientHold), by cell for the same reason.
    readonly HashSet<GridCoord> ambientHolds = new HashSet<GridCoord>();
    // M1: look holds (SetLampHold), by cell for the same reason; a lamp built during one shows it.
    readonly Dictionary<GridCoord, LampHold> lampHolds = new Dictionary<GridCoord, LampHold>();
    readonly List<GridCoord> lampHoldsDone = new List<GridCoord>();
    // Built lamps by cell, and the active lamp overrides (all, and by cell).
    readonly Dictionary<GridCoord, Fixture> fixtureByCell = new Dictionary<GridCoord, Fixture>();
    readonly List<LampOverride> lampOverrides = new List<LampOverride>();
    readonly Dictionary<GridCoord, List<LampOverride>> overridesByCell = new Dictionary<GridCoord, List<LampOverride>>();
    int nextLampOverride;
    // Doors the Relay is breaking (by edge, so a rebuilt chunk's new Door reads the same).
    readonly HashSet<long> breakingDoors = new HashSet<long>();
    readonly Dictionary<Collider, Door> doorByCollider = new Dictionary<Collider, Door>();
    readonly Dictionary<long, Door> doorByEdge = new Dictionary<long, Door>();
    // Doors a key has opened (they stay unlocked, from both sides), and the doors a key turn holds for
    // the unlock shot, by edge (so a rebuilt or shifted chunk keeps both).
    readonly HashSet<long> unlockedDoors = new HashSet<long>();
    readonly Dictionary<long, KeyTurnHold> keyTurns = new Dictionary<long, KeyTurnHold>();
    readonly List<long> keyTurnScratch = new List<long>();
    // The doors' own clock: advanced by TickDoors, so it stops with DoorsPaused (the rattle debounce, the watchdog).
    float doorClock;

    // A key turn under way: the zone whose key turns, where the opener stood, whether it has passed the commit, and the owner's last heartbeat.
    sealed class KeyTurnHold
    {
        public GridCoord zone;
        public Vector3 from;
        public bool committed;
        public float sinceRefresh;
    }
    readonly Dictionary<Collider, Window> windowByCollider = new Dictionary<Collider, Window>();
    readonly HashSet<Collider> shellColliders = new HashSet<Collider>();
    // Modules placed by hand (the Level Designer preview), stamped whenever their chunk is generated.
    readonly List<(RoomModuleData module, GridCoord chunk, int x, int y)> placedModules = new List<(RoomModuleData, GridCoord, int, int)>();
    Vector3? spawnOverride;
    float spawnYaw;
    // The start area (SetStartArea): cells the title's stream rooms stand in,
    // and the cell their door opens onto.
    RectInt startArea;
    bool hasStartArea;
    GridCoord startDoorCell;
    // World-XZ rects the start area keeps free of furnishing (visual Q16, research/placard/40_contract_map.md).
    readonly List<Rect> startKeepClear = new List<Rect>();
    // The exit area (SetExitArea, C22): X, the EXIT's stairwell cell; N = X + (0, -1), the cell in front of the
    // EXIT door; and the EXIT door's edge (N to X).
    GridCoord exitCell, exitFront;
    bool hasExitArea;
    long exitEdge;
    // The EXIT's seal (SealExit): None; Shutting (asked: the door shuts and nothing opens it again); Locked (it
    // latched, and is locked for good).
    enum ExitSeal { None, Shutting, Locked }
    ExitSeal exitSeal;
    // The EXIT's floor-key lock (C23 round 3, A13's seam; SetExitLocked): set by the game on a floor-key floor (M3),
    // false on every other. Kept by the map, not the door, so a rebuild keeps it; SetExitArea and ClearExitArea reset it.
    bool exitLocked;
    // The floor-1 start group's station (A26 step 3, SetStation): its layout (the plan's, map cells), its module (props and
    // markers), its set piece, its skin, the walk D to N, and the colliders registered as architecture (RegisterArchitecture).
    bool hasStation;
    StartGroupLayout station;
    RoomModuleData stationModule;
    FrontRoomsStationSet stationSet;
    ThemeMaterials stationSkin;
    readonly List<GridCoord> stationWalk = new List<GridCoord>();
    readonly HashSet<Collider> setPieceColliders = new HashSet<Collider>();
    Material stationWallForTools, stationFloorForTools;
    // StationPathDistance's breadth-first search, kept between calls.
    readonly Dictionary<GridCoord, int> stationSteps = new Dictionary<GridCoord, int>();
    readonly Queue<GridCoord> stationQueue = new Queue<GridCoord>();
    // M6's seam (TakeStationKey): the station's key taken on this floor, and where.
    bool stationKeyTaken;
    Vector3 stationTakePoint;

    /// <summary>
    /// Keep furnishing (office kits, piles and module props) out of a world-XZ rect
    /// while the start area stands: the evacuation placard's wall and the floor in
    /// front of it. Cleared by SetStartArea and ClearStartArea. MapRunStarted is
    /// early enough: rooms are dressed one a frame from Update. [Preserve]: the
    /// visual mount reaches it by reflection until it calls it directly, and WebGL
    /// (managed stripping High) / iOS (Low) may strip a method nothing calls.
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public void KeepClearAtStart(Rect worldXZ) => startKeepClear.Add(worldXZ);
    readonly List<GridCoord> scratch = new List<GridCoord>();
    // Outlets (FrontRoomsWallFixtures): every wall face BuildEdge built for the chunk being built.
    readonly List<FrontRoomsWallFixtures.WallFace> wallFaces = new List<FrontRoomsWallFixtures.WallFace>();
    readonly List<Door> movingDoors = new List<Door>();
    Transform player;
    MaterialPropertyBlock block;
    ThemeMaterials level0, office;
    Material trim, doorLeaf, glass, keyGlow;
    bool begun;

    static bool dressersResolved;
    static MethodInfo officeDress, officeDressOld, pileBuild;
    // Their time-sliced versions (FrontRoomsDressJob): a room's furniture is built a few ms per frame.
    static MethodInfo officeBegin, pileBegin;

    /// <summary>A room waiting to be furnished. Rooms are dressed one per frame after their chunk is built.</summary>
    struct DressJob
    {
        public BuiltChunk chunk;
        public MapChunk data;
        public int room;
    }
    readonly Queue<DressJob> dressQueue = new Queue<DressJob>();

    /// <summary>
    /// The room being furnished a little per frame, and which room it is.
    /// One at a time: the next room (its module props included) starts only
    /// when this one is done, so every chunk's children come out in the same
    /// order as furnishing each room in one frame.
    /// </summary>
    FrontRoomsDressJob currentJob;
    DressJob currentRoom;

    /// <summary>
    /// Milliseconds of furnishing per frame. A large Office room or pile cost
    /// 40-170 ms in one frame; as a job it is spread over frames at this
    /// budget (a step stops before a unit that would cross it, but one unit
    /// that runs long can still overrun it). WebGL keeps its own number,
    /// set in the WebGL build only.
    /// </summary>
#if UNITY_WEBGL && !UNITY_EDITOR
    float dressBudgetMs = 2f;
#else
    float dressBudgetMs = 3f;
#endif

    void TouchPassageRevision()
    {
        PassageRevision = PassageRevision == int.MaxValue ? 1 : PassageRevision + 1;
    }

    void OnEnable() => FrontRoomsLevelProfile.Changed += OnProfileChanged;

    void OnDisable() => FrontRoomsLevelProfile.Changed -= OnProfileChanged;

    void OnProfileChanged(FrontRoomsLevelProfile changed)
    {
        if (Application.isPlaying && begun && changed != null && changed == profile) ApplyLive(changed);
    }

    /// <summary>Raised after ApplyLive, so the game can move its camera's far plane with the build radius.</summary>
    public event Action LiveApplied;

    /// <summary>
    /// Take a profile's live numbers into the running map: build radius and
    /// chunks per frame (the next stream follows), the shift delay, the light
    /// and shadow radii, keys, Office dressing and the pile chance (rooms
    /// furnished from now on), and the tier table. The generation numbers and
    /// the module library stay as the map was made with: changing them under a
    /// running map would move what the player has seen.
    /// </summary>
    public void ApplyLive(FrontRoomsLevelProfile source)
    {
        if (source == null) return;
        buildRadius = Mathf.Max(1, source.buildRadius);
        chunksPerFrame = Mathf.Max(1, source.chunksPerFrame);
        shiftAfterSeconds = source.shiftAfterSeconds;
        lightRadius = source.lightRadius;
        shadowRadius = Mathf.Min(source.shadowRadius, source.lightRadius);
        doorsNeedKeys = source.doorsNeedKeys;
        dressOffices = source.dressOffices;
        pileChance = source.pileChance;
        tierRules = source.tiers ?? tierRules;
        // A key turn never outlives a live change of the rules: its owner sees the hold gone and cancels.
        keyTurns.Clear();
        var walker = player != null ? player.GetComponent<FrontRoomsMapWalker>() : null;
        if (walker != null) walker.RefreshView();
        LiveApplied?.Invoke();
    }

    /// <summary>
    /// Put a different module in a chunk's hand placement (the Level Designer
    /// preview in Play) and rebuild that chunk at once. Its borders are the
    /// chunk's own, so the neighbours stay as they are.
    /// </summary>
    public void ReplaceModule(GridCoord chunk, RoomModuleData module, int x, int y)
    {
        placedModules.RemoveAll(p => p.chunk == chunk);
        placedModules.Add((module, chunk, x, y));
        Cache?.ReplacePlacements(chunk, module, x, y);
        RebuildChunk(chunk);
    }

    /// <summary>Throw a built chunk away and build it again now (furnished), without counting it as dropped: no revisit shift.</summary>
    public void RebuildChunk(GridCoord coord)
    {
        if (built.TryGetValue(coord, out var old))
        {
            // Its colliders go this frame (a deferred Destroy would leave them for the walker to hit).
            if (old.root != null) old.root.SetActive(false);
            Unregister(old);
            built.Remove(coord);
            TouchPassageRevision();
        }
        failedChunks.Remove(coord);
        Build(coord);
        while (DressNext(all: true)) { }
    }

    void Awake()
    {
        if (settings == null)
        {
            var seed = Profile.generation.seed;
            if (randomSeedOnPlay && standalone) seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            TakeProfile(Profile, seed);
        }
        CreateCache();
        block = new MaterialPropertyBlock();
        BuildMaterials();
        if (!standalone || !Application.isPlaying) return;
        ApplyRenderSettings();
        Begin(FrontRoomsMapWalker.Spawn(this, SpawnWorldPosition, transform.eulerAngles.y + spawnYaw).transform);
    }

    /// <summary>
    /// Create the map inside the main game: no walker and no change to the
    /// scene's render settings. Place the player at SpawnWorldPosition, then
    /// call Begin.
    /// </summary>
    public static FrontRoomsMapWorld CreateEmbedded(Transform parent, FrontRoomsLevelProfile profile, int seed)
    {
        var go = new GameObject("Level 0 map");
        go.SetActive(false);
        go.transform.SetParent(parent, false);
        var world = go.AddComponent<FrontRoomsMapWorld>();
        world.profile = profile;
        world.standalone = false;
        world.TakeProfile(world.Profile, seed);
        // Awake runs here, with the fields above already set.
        go.SetActive(true);
        return world;
    }

    /// <summary>Copy the profile's numbers into this map, with the given seed.</summary>
    void TakeProfile(FrontRoomsLevelProfile source, int seed)
    {
        settings = source.Generation(seed);
        moduleLibrary = source.ModuleData();
        buildRadius = Mathf.Max(1, source.buildRadius);
        chunksPerFrame = Mathf.Max(1, source.chunksPerFrame);
        shiftAfterSeconds = source.shiftAfterSeconds;
        lightRadius = source.lightRadius;
        shadowRadius = source.shadowRadius;
        doorsNeedKeys = source.doorsNeedKeys;
        dressOffices = source.dressOffices;
        pileChance = source.pileChance;
        tierRules = source.tiers ?? new FrontRoomsTierRules();
    }

    /// <summary>
    /// Start streaming around the player. With <paramref name="buildAllNow"/>
    /// every chunk in range is built and furnished in this call, for builds
    /// nobody watches (the test scene, edit-mode captures). Without it they
    /// fill in from Update at the normal per-frame budget, nearest first;
    /// the main game does that while the player is still in the title's
    /// stream room, and opens the door into the map once Settled.
    /// </summary>
    public void Begin(Transform playerTransform, bool buildAllNow = true)
    {
        player = playerTransform;
        begun = true;
        keyTurns.Clear();
        if (buildAllNow)
        {
            Stream(ChunkOf(StreamCenter), int.MaxValue);
            while (DressNext(all: true)) { }
            // A26: the station's props at once too.
            if (stationSet != null) stationSet.CompleteProps();
        }
        TickFixtures(0f);
    }

    /// <summary>While set, chunks stream round this world point instead of round the player.</summary>
    public Vector3? StreamFocus { get; set; }

    const double FocusFrameBudgetMs = 8.0;

    Vector3 StreamCenter => StreamFocus ?? player.position;

    /// <summary>
    /// Every chunk within the build radius of the streaming centre is built
    /// (nothing unbuilt within sight), and none of the rooms within
    /// <paramref name="rings"/> chunks of it is still waiting to be furnished.
    /// A26: on a station floor, also not before the station's props stand (StationReady).
    /// </summary>
    public bool ReadyAround(int rings)
    {
        if (!begun || player == null) return false;
        if (!StationReady) return false;
        var center = ChunkOf(StreamCenter);
        for (var dy = -buildRadius; dy <= buildRadius; dy++)
        for (var dx = -buildRadius; dx <= buildRadius; dx++)
            if (!BuiltOrFailed(new GridCoord(center.x + dx, center.y + dy))) return false;
        foreach (var job in dressQueue)
            if (Waiting(job, center, rings)) return false;
        return currentJob == null || !Waiting(currentRoom, center, rings);
    }

    bool Waiting(DressJob job, GridCoord center, int rings)
    {
        var c = job.data.coord;
        return Mathf.Abs(c.x - center.x) <= rings && Mathf.Abs(c.y - center.y) <= rings
            && built.TryGetValue(c, out var standing) && standing == job.chunk;
    }

    /// <summary>Every chunk within the build radius of the streaming centre is built, and every queued room is furnished.</summary>
    public bool Settled
    {
        get
        {
            if (!begun || player == null || dressQueue.Count > 0 || currentJob != null) return false;
            var center = ChunkOf(StreamCenter);
            for (var dy = -buildRadius; dy <= buildRadius; dy++)
            for (var dx = -buildRadius; dx <= buildRadius; dx++)
                if (!BuiltOrFailed(new GridCoord(center.x + dx, center.y + dy))) return false;
            return true;
        }
    }

    // A chunk whose build failed counts as done: waiting for it would never end.
    bool BuiltOrFailed(GridCoord coord) => built.ContainsKey(coord) || failedChunks.Contains(coord);

    /// <summary>Chunks whose build threw and are left out until they leave range.</summary>
    public int FailedChunkCount => failedChunks.Count;

    // ---------- Start area: the title's stream rooms, left out of the map ----------

    /// <summary>
    /// Leave a rectangle of cells to the title's stream rooms, which the run
    /// starts in. Those cells get no floor, ceiling, lamp, key, column, grade
    /// or furniture; their west, east and south sides are walled; their north
    /// side is the stream room's end wall, whose door opens onto
    /// <paramref name="doorCell"/>. Render only: the generated data is not
    /// changed, but IsBuilt and PassageBetween treat the cells as no map, so
    /// the Relay and the autopilot never route through them. Call before Begin.
    /// </summary>
    public void SetStartArea(RectInt cells, GridCoord doorCell)
    {
        startKeepClear.Clear();
        startArea = cells;
        startDoorCell = doorCell;
        hasStartArea = cells.width > 0 && cells.height > 0;
    }

    /// <summary>
    /// Give the start area back to the map, once the stream rooms are gone.
    /// Only while StartAreaBuilt is false: a chunk built round the rooms keeps
    /// their hole and its walls until it is rebuilt. With an exit area set, the
    /// cells it gives back that lie in the exit lamp hold (which SetExitArea could
    /// not reach while they were reserved) are held Steady now (HoldExitLamps).
    /// A26: with a station, its set piece swaps the south liner for the static
    /// entrance (D's south edge is a data wall from the next build of D's chunk).
    /// </summary>
    public void ClearStartArea()
    {
        var givenBack = startArea;
        var hold = hasStartArea && hasExitArea;
        hasStartArea = false;
        startKeepClear.Clear();
        if (hold) HoldExitLamps(givenBack);
        if (stationSet != null) stationSet.StartAreaCleared();
    }

    public bool HasStartArea => hasStartArea;

    public bool InStartArea(GridCoord cell) =>
        hasStartArea && cell.x >= startArea.xMin && cell.x < startArea.xMax && cell.y >= startArea.yMin && cell.y < startArea.yMax;

    // C22: a one-cell start area is a stairwell (NextFloor: the last floor's X), never the title's stream rooms (a strip
    // five cells wide). Its north line is closed by FrontRoomsStairwell.JoinNextFloor's end wall, which runs only between
    // X's side walls (+-1.42 m), so those walls take their north corners by CornerReach, as every other wall end does.
    bool StartAreaIsStairwell => hasStartArea && startArea.width == 1 && startArea.height == 1;

    /// <summary>
    /// True while a built chunk has geometry shaped by the start area: one that
    /// overlaps it or the ring of cells round it (walls on its west and south
    /// sides belong to the neighbours; wall ends and grades change beside it).
    /// </summary>
    public bool StartAreaBuilt
    {
        get
        {
            if (!hasStartArea) return false;
            foreach (var coord in built.Keys)
            {
                var o = MapGrid.ChunkOrigin(coord);
                // The closed ring Build shapes round the area: its walls, the wall ends that stop short of it, the grades left off beside it.
                if (o.x <= startArea.xMax && startArea.xMin - 1 < o.x + MapGrid.ChunkCells
                    && o.y <= startArea.yMax && startArea.yMin - 1 < o.y + MapGrid.ChunkCells) return true;
            }
            return false;
        }
    }

    /// <summary>True when a column or bulkhead between two cell corners would touch the start area or its edge.</summary>
    bool TouchesStartArea(int x0, int y0, int x1, int y1) =>
        hasStartArea && x1 >= startArea.xMin && x0 <= startArea.xMax && y1 >= startArea.yMin && y0 <= startArea.yMax;

    bool RoomInStartArea(MapChunk data, CellRect room)
    {
        if (!hasStartArea) return false;
        var o = data.Origin;
        return o.x + room.x < startArea.xMax && startArea.xMin < o.x + room.x + room.w
            && o.y + room.y < startArea.yMax && startArea.yMin < o.y + room.y + room.h;
    }

    // ---------- Exit area: X, the EXIT's stairwell cell, left out of the map (C22) ----------

    /// <summary>
    /// C22 (Red, 2026-10-22; plan v2 c22_stairwell_plan_v2.md, D3-D7): leave cell X to the EXIT's stairwell,
    /// through which the run goes one floor deeper. X comes from FrontRoomsExitPlanner.ChooseExit, which reads
    /// the cache alone (chunk-local 1..6 on both axes, outside every room, no key, no column on its corners, off
    /// the start area and its ring, its chunk whole without it), and is set before Begin, like SetStartArea,
    /// so X's chunk is built reserved the first time and never has to be rebuilt for it. Render only, as for the
    /// start area: the generated data is not changed. X gets no floor, ceiling, lamp, key, column, grade,
    /// outlet or furniture, and it is no map to the Relay, the autopilot and hearing (IsBuilt false,
    /// PassageBetween Wall: a Relay-free safe cell, D3); the stairwell object stands in it. Its south edge,
    /// from N = X + (0, -1), is the EXIT door: a map door that swings into X, away from the player who opens it
    /// in N (A12), and is never locked by the zone-key rule (D6; its own lock is ExitLocked, A13's floor key: the game
    /// locks it after this call on a floor-key floor, M3); its west, east and north edges are walls in the outside cells' paper and height (EffectiveEdge). X's chunk never shifts (D5), so what the planner
    /// checked stays true for the floor. Lamps within light range of X are the exit group (ExitLamps). C23 step 1:
    /// the lamps within ExitLampHoldCells of N are held Steady for the floor (HoldExitLamps), and the EXIT door
    /// carries the floor's LIT-locked EXIT sign over its header on N's side, built with X's chunk (ExitSign).
    /// C23 round 3: each build of the EXIT door raises ExitDoorBuilt for the visual dressing; a new exit area
    /// starts unlocked (ExitLocked false, with no ExitLockChanged: no door of it is built yet).
    /// </summary>
    public void SetExitArea(GridCoord cell)
    {
        exitCell = cell;
        exitFront = new GridCoord(cell.x, cell.y - 1);
        exitEdge = EdgeId(exitFront, cell);
        hasExitArea = true;
        exitSeal = ExitSeal.None;
        exitLocked = false;
        ExitLamps = 1f;
        // Only chunk-local 1..6 keeps X's four edges, its corners and N in X's own chunk, the one chunk that never shifts.
        var o = MapGrid.ChunkOrigin(MapGrid.ChunkOf(cell));
        int i = cell.x - o.x, j = cell.y - o.y;
        if (i < 1 || i > MapGrid.ChunkCells - 2 || j < 1 || j > MapGrid.ChunkCells - 2)
            Debug.LogWarning("[FrontRoomsMap] Exit cell " + cell + " lies on its chunk's outer row or column (local " + i + ", " + j
                + "): a neighbour chunk builds some of its edges, and that chunk may shift. FrontRoomsExitPlanner never chooses such a cell.");
        RegroupExitLamps();
        HoldExitLamps();
    }

    /// <summary>How far round N, in cells (Chebyshev), the exit lamp hold reaches: a 5 x 5 square of cells centred on N.</summary>
    public const int ExitLampHoldCells = 2;

    /// <summary>
    /// The exit lamp hold (C23 step 1; the visual chat, research/stairwell/10_directions.md 1.3 and 7a, measured in 5.4: with
    /// the lamps round the EXIT Dead, a Relay standing at the door no longer reads against the paper): every map
    /// lamp within ExitLampHoldCells of N is set Steady through the per-cell lamp modes (SetLampMode), so it is
    /// built Steady and a lamp already built turns Steady at once. Like any lamp mode it is kept by cell for the
    /// floor (drops, rebuilds and shifts keep it; NextFloor clears every lamp mode; ClearExitArea leaves it).
    /// Never in X, nor in the start area while it stands (SetLampMode leaves reserved cells alone): the start
    /// area's cells in the 5 x 5 take the hold when ClearStartArea gives them back to the map (<paramref name="only"/>:
    /// just those cells; the title strip on floor 1, the stairwell the floor began in after). A cell whose lamp a
    /// module took out stays dark. The hold is a lamp's temperament only: the C22 blackout (ExitLamps) still takes
    /// these lamps down in the swap, since every one of them lies well within its light's range of X, and a Relay
    /// warning still dips them.
    /// </summary>
    void HoldExitLamps(RectInt? only = null)
    {
        for (var dy = -ExitLampHoldCells; dy <= ExitLampHoldCells; dy++)
        for (var dx = -ExitLampHoldCells; dx <= ExitLampHoldCells; dx++)
        {
            var cell = new GridCoord(exitFront.x + dx, exitFront.y + dy);
            if (only.HasValue && !only.Value.Contains(new Vector2Int(cell.x, cell.y))) continue;
            SetLampMode(cell, ModuleLamp.Steady);
        }
    }

    /// <summary>
    /// Give X back to the map, and forget its seal and its lock (NextFloor does both: the stairwell goes with the
    /// old floor). ExitLocked goes back to false with no ExitLockChanged: the door that was the EXIT is no longer
    /// one (or, in NextFloor, is torn down). A chunk built round X keeps its walls and door until it is rebuilt.
    /// </summary>
    public void ClearExitArea()
    {
        hasExitArea = false;
        exitSeal = ExitSeal.None;
        exitLocked = false;
        ExitLamps = 1f;
        RegroupExitLamps();
    }

    public bool HasExitArea => hasExitArea;

    /// <summary>X, the EXIT's stairwell cell (meaningful while HasExitArea).</summary>
    public GridCoord ExitCell => exitCell;

    /// <summary>N = X + (0, -1): the map cell in front of the EXIT door, where the player opens it (meaningful while HasExitArea).</summary>
    public GridCoord ExitFrontCell => exitFront;

    public bool InExitArea(GridCoord cell) => hasExitArea && cell == exitCell;

    /// <summary>
    /// A stairwell cell: this floor's X, or the one-cell start area that was the last floor's X (C22). For readers that
    /// treat both stairwells alike (声音设计's stair room tone). The walkable stair will make both 1 x 2.
    /// </summary>
    public bool InStairwell(GridCoord cell) => InExitArea(cell) || (StartAreaIsStairwell && InStartArea(cell));

    /// <summary>
    /// A cell the map leaves out: the start area (the title's stream rooms, or on later floors the stairwell the
    /// floor began in) or X (the EXIT's stairwell). No floor, ceiling, lamp, key, column or furniture is built
    /// there, and IsBuilt and PassageBetween treat it as no map.
    /// </summary>
    public bool IsReserved(GridCoord cell) => InStartArea(cell) || InExitArea(cell);

    /// <summary>The built EXIT door (on the edge N to X), or null when there is no exit area or X's chunk is not built.</summary>
    public Door ExitDoor => hasExitArea ? DoorBetween(exitFront, exitCell) : null;

    // ---------- The floor-1 start group's station (A26 step 3; notes/floor1_start_group_plan.md 4.4, 4.7, 9) ----------

    /// <summary>
    /// The station's own Surfaces (Resources/Surfaces, read with FrontRoomsSurfaces.TryGet): the visual chat's walls 2
    /// Post Room set (61 5). Until the wall and floor exist the station's cells take Level 0's (one warning per
    /// SetStation); the ceiling is Level 0's unless a Station_Ceiling is made (61 5: the same tiles as Level 0).
    /// </summary>
    public const string StationWallSurface = "Station_Wall", StationFloorSurface = "Station_Floor", StationCeilingSurface = "Station_Ceiling";

    /// <summary>
    /// True when this floor's station is planned (M4; research/tutorial/README_V4_FloorHasStation.md): SetStation sets
    /// it before Begin, ClearStation and NextFloor set it back to false, a key take never does. The visual chat's
    /// FrontRoomsStationPresence reads it by reflection at MapRunStarted and FloorChanged, so [Preserve] keeps it
    /// through WebGL's High stripping. Settable for a later floor's station, which needs no carve.
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public bool FloorHasStation { get; set; }

    /// <summary>
    /// Hold the floor-1 start group (A26): <paramref name="plan"/> is FrontRoomsStartGroup.Plan's on this map's cache
    /// (Ready: its carve on Cache, its EXIT found), <paramref name="module"/> the station module its template came from
    /// (its props and markers; null: none). Call after SetStartArea and before Begin, like SetExitArea. Until
    /// ClearStation or NextFloor:
    /// - FloorHasStation is true;
    /// - the station's cells are drawn in the station's skin (MaterialsOf: their faces, floor and ceiling, so a wall
    ///   between the station and the maze is split, station inside, zone paper outside), E1 and E2 keep their authored
    ///   spans (ArchOpening), and the station's chunk c and X's chunk c' never shift (Stream; the carve's pins);
    /// - wall fixtures keep out of the strip, the station, the corridor and X (FixtureKeepOut), no Relay entry lies in
    ///   the station (RelayEntries), and ReadyAround waits for the station's props (StationReady);
    /// - the set piece (FrontRoomsStationSet, a child of the map) stands for the floor: the module's props, spawned in
    ///   dressBudgetMs slices from Update (all at once with Begin's buildAllNow); the south liner over the stream
    ///   facade; the riser chase in the north-east cell, floor to ceiling, as architecture; and, from ClearStartArea,
    ///   the static entrance. It shows while c or c' is built.
    /// A chunk the carve touches that is already built is rebuilt. False, with a warning and nothing changed, for a
    /// plan that is not Ready or whose carve is not the one on this map's cache.
    /// </summary>
    public bool SetStation(StartGroupPlan plan, RoomModuleData module)
    {
        // An earlier station's state goes; the cache keeps the carve Plan just set (ClearStation would take it off).
        ForgetStation();
        if (plan == null || !plan.Ready || Cache == null || Cache.Carve == null || Cache.Carve != plan.mapCarve)
        {
            Debug.LogWarning("[FrontRoomsMap] SetStation: " + (plan == null ? "no plan" : !plan.Ready ? "the plan is not ready (" + (plan.error ?? "no EXIT") + ")"
                : "its carve is not on this map's cache (plan on Cache, then SetStation)") + "; no station.");
            return false;
        }
        hasStation = true;
        station = plan.layout;
        stationModule = module;
        FloorHasStation = true;
        // M3: a station with a KeySpot puts the floor under the floor key (ValidateStation demands exactly one, so this
        // is every station in practice; a key-less module never locks the EXIT with no key to open it). A13: Floor 1, whose
        // station this is, has no locked inner doors.
        floorKey = StationAnchor(ModuleMarkerKind.KeySpot, out _, out _, out _);
        noInteriorLocks = true;
        stationSkin = StationMaterials();
        stationWalk.Clear();
        for (var y = station.door.y; y <= station.front.y; y++) stationWalk.Add(new GridCoord(station.door.x, y));
        stationSet = FrontRoomsStationSet.Create(this, station, module, stationSkin.wall, trim, doorLeaf);
        foreach (var coord in Cache.Carve.Touched)
            if (built.ContainsKey(coord)) RebuildChunk(coord);
        UpdateStationVisibility();
        return true;
    }

    /// <summary>
    /// Forget the station (ChangeFloor before NextFloor; a start that falls back to today's): FloorHasStation false,
    /// the set piece and its architecture gone, and the carve off the cache, so the chunks it touched build raw again
    /// (a built one is rebuilt at once, unless <paramref name="rebuild"/> is false: ChangeFloor's call, right before
    /// NextFloor drops every chunk). A map with no station is left as it is.
    /// </summary>
    public void ClearStation(bool rebuild = true)
    {
        ForgetStation();
        var carve = Cache != null ? Cache.Carve : null;
        if (carve == null) return;
        Cache.ClearCarve();
        if (!rebuild) return;
        foreach (var coord in carve.Touched)
            if (built.ContainsKey(coord)) RebuildChunk(coord);
    }

    // The station's state and set piece, without touching the cache (NextFloor makes a new one).
    void ForgetStation()
    {
        FloorHasStation = false;
        stationKeyTaken = false;
        // M3: the floor key's rule goes with the station, and so does a floor key held (no zone ever holds its identity).
        floorKey = noInteriorLocks = false;
        keysHeld.Remove(FloorKeyZone);
        keysAnnounced.Remove(FloorKeyZone);
        if (!hasStation && stationSet == null) return;
        hasStation = false;
        station = default;
        stationModule = null;
        stationSkin = null;
        stationWalk.Clear();
        if (stationSet != null)
        {
            stationSet.Release();
            // Its colliders out of the physics scene at once (a deferred Destroy would leave them this frame).
            stationSet.gameObject.SetActive(false);
            Kill(stationSet.gameObject);
            stationSet = null;
        }
    }

    /// <summary>The map holds a station (SetStation): the floor-1 start group's room is carved, skinned and dressed.</summary>
    public bool HasStation => hasStation;

    /// <summary>The station's layout (map cells): D, the footprint, E1 and E2, the corridor, N and X, the strip. Default with no station.</summary>
    public StartGroupLayout StationLayout => station;

    /// <summary>The station's footprint in map cells (x D.x-2..D.x+1, y D.y..D.y+H-1); empty with no station.</summary>
    public RectInt StationRect => hasStation ? new RectInt(station.station.x, station.station.y, station.station.w, station.station.h) : default;

    /// <summary>The cell lies in the station's footprint. False with no station.</summary>
    public bool InStation(GridCoord cell) => hasStation && station.InStation(cell);

    /// <summary>The station's module (its props and markers), or null.</summary>
    public RoomModuleData StationModule => stationModule;

    /// <summary>The station's set piece (props, liner, riser block, static entrance), or null with no station.</summary>
    public FrontRoomsStationSet Station => stationSet;

    /// <summary>True with no station, or once every one of the station's props stands (the start door's hold waits for it, through ReadyAround).</summary>
    public bool StationReady => !hasStation || stationSet == null || stationSet.PropsDone;

    /// <summary>
    /// The walk from the start door to the EXIT, D to N (StationExitWalk[0] is D, the last N): the station's entrance
    /// column up to E1, then the corridor C1..N. Empty with no station. M7's first send keeps off it.
    /// </summary>
    public IReadOnlyList<GridCoord> StationExitWalk => stationWalk;

    /// <summary>
    /// Where a station marker stands, in world space (the module's first marker of that kind: KeySpot, StationBeacon,
    /// Placard, StationEntrance): its point (x, y, z in the module, from the station's south-west corner), its turn
    /// (the marker's yaw), and its map cell. False with no station or no such marker.
    /// </summary>
    public bool StationAnchor(ModuleMarkerKind kind, out Vector3 position, out Quaternion rotation, out GridCoord cell)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;
        cell = default;
        if (!hasStation || stationModule == null || stationModule.markers == null) return false;
        foreach (var mk in stationModule.markers)
        {
            if (mk.kind != kind) continue;
            var local = new Vector3(station.station.x * MapGrid.CellSize + mk.x, mk.y, station.station.y * MapGrid.CellSize + mk.z);
            position = transform.TransformPoint(local);
            rotation = transform.rotation * Quaternion.Euler(0f, mk.yaw, 0f);
            cell = CellAt(local);
            return true;
        }
        return false;
    }

    /// <summary>The cell of the station's beacon (the TV; M5 and the wayfinding seed), else the station's north-row axis cell (E1's inside).</summary>
    public GridCoord StationBeaconCell => StationAnchor(ModuleMarkerKind.StationBeacon, out _, out _, out var cell) ? cell : station.e1In;

    /// <summary>
    /// M5: the edge of StationBeaconCell the TV hangs on (0 +x, 1 +z, 2 -x, 3 -z, FrontRoomsWayfinding's dirs), from the
    /// beacon marker's turn and place (RoomModuleData.StationBeaconCell), when that edge is a wall on this map; -1 when the
    /// TV hangs on none (the wayfinding's station cell is then message 0: the TV is in view there), and with no station or
    /// no beacon marker.
    /// </summary>
    public int StationBeaconEdge
    {
        get
        {
            if (!hasStation || stationModule == null || Cache == null || !stationModule.StationBeaconCell(out _, out _, out var edge) || edge < 0) return -1;
            var cell = StationBeaconCell;
            var next = new GridCoord(cell.x + FrontRoomsWayfinding.DirX[edge], cell.y + FrontRoomsWayfinding.DirY[edge]);
            return Cache.Edge(cell, next) == EdgeKind.Wall ? edge : -1;
        }
    }

    /// <summary>
    /// Walking cells from the station's footprint to <paramref name="cell"/> (0 inside it), over passages that are Open
    /// now (PassageBetween: walls, shut doors and glass stop it), at most <paramref name="cap"/> deep: cap + 1 when the
    /// cell lies further or cannot be reached, or with no station. M7's gate: StationPathDistance(player, 3) >= 2.
    /// </summary>
    public int StationPathDistance(GridCoord cell, int cap)
    {
        if (!hasStation) return cap + 1;
        if (InStation(cell)) return 0;
        stationSteps.Clear();
        stationQueue.Clear();
        var s = station.station;
        for (var j = s.y; j < s.y + s.h; j++)
        for (var i = s.x; i < s.x + s.w; i++)
        {
            var c = new GridCoord(i, j);
            stationSteps[c] = 0;
            stationQueue.Enqueue(c);
        }
        while (stationQueue.Count > 0)
        {
            var c = stationQueue.Dequeue();
            var d = stationSteps[c];
            if (d >= cap) continue;
            for (var k = 0; k < 4; k++)
            {
                var n = k == 0 ? new GridCoord(c.x + 1, c.y) : k == 1 ? new GridCoord(c.x - 1, c.y) : k == 2 ? new GridCoord(c.x, c.y + 1) : new GridCoord(c.x, c.y - 1);
                if (stationSteps.ContainsKey(n) || PassageBetween(c, n) != Passage.Open) continue;
                if (n == cell) return d + 1;
                stationSteps[n] = d + 1;
                stationQueue.Enqueue(n);
            }
        }
        return cap + 1;
    }

    /// <summary>
    /// M6's seam (A26, plan 4.10): the station's key was taken, with the take point (world space, on the floor). Raised
    /// once a floor by TakeStationKey. The game sends the Relay's first leg there (FrontRoomsMapHunter.HuntStation) and
    /// M7's gate counts the key (StationKeyIsTaken). M3: the floor key's take (TakeKey, E on "Key · floor") raises it,
    /// after KeyPickupStarted and before KeyTaken (the same call on the instant path), at the KeySpot on the floor; the
    /// TV (V2) and the sound (S4) hear it there. A handler that throws is logged and the next one still runs.
    /// </summary>
    public event Action<Vector3> StationKeyTaken;

    /// <summary>The station's key was taken on this floor (TakeStationKey). False with no station, and from ClearStation, NextFloor or the next SetStation.</summary>
    public bool StationKeyIsTaken => hasStation && stationKeyTaken;

    /// <summary>Where the station's key was taken, on the floor (TakeStationKey's point): the first leg's goal. Meaningful while StationKeyIsTaken.</summary>
    public Vector3 StationTakePoint => stationTakePoint;

    /// <summary>C-1 (游戏视觉's station-look-v1 contract): how far in front of the key the take point stands, along the KeySpot's facing.</summary>
    public const float StationTakeStandOff = .5f;

    /// <summary>
    /// C-1: where the player stands to take the station's key, on the floor: StationTakeStandOff in front of the KeySpot
    /// along its facing (Station_FrontDesk_4x4: the key on the riser's plate at x 10.23, yaw 270, so (9.73, 10.40) in the
    /// room, west of the riser block). False with no station or no KeySpot.
    /// </summary>
    public bool StationTakeSpot(out Vector3 point)
    {
        point = Vector3.zero;
        if (!StationAnchor(ModuleMarkerKind.KeySpot, out var anchor, out var turn, out _)) return false;
        point = anchor + turn * Vector3.forward * StationTakeStandOff;
        point.y = CellCenter(CellAt(transform.InverseTransformPoint(point))).y;
        return true;
    }

    /// <summary>
    /// M6's seam: the station's key is taken at <paramref name="at"/> or, by default, at StationTakeSpot (C-1: 0.5 m in
    /// front of the KeySpot, on the floor; never the north-east cell's middle). Raises StationKeyTaken the first time on a floor. False, with nothing raised, with no station, with no
    /// point and no KeySpot, or once taken. The game's only caller is the floor key's take (TakeKey, M3); a call from
    /// tools raises the event but grants no key (the floor key stays at its KeySpot, StationKeyIsTaken turns true).
    /// </summary>
    public bool TakeStationKey(Vector3? at = null)
    {
        if (!hasStation || stationKeyTaken) return false;
        Vector3 point;
        if (at.HasValue) point = at.Value;
        else if (!StationTakeSpot(out point)) return false;
        stationKeyTaken = true;
        stationTakePoint = point;
        if (StationKeyTaken == null) return true;
        foreach (Action<Vector3> handler in StationKeyTaken.GetInvocationList())
        {
            try { handler(point); }
            catch (Exception e) { Debug.LogException(e); }
        }
        return true;
    }

    // ---------- M3: the floor key (A13, C15 as changed 10-22; notes/a26_group_m3_plan.md 2) ----------

    /// <summary>
    /// M3: the floor key's identity wherever a key is named (keysHeld, KeyPickupStarted, KeyTaken, DoorUnlocked, the
    /// key presenter, the HUD's StubIdentity). Never a zone: zones are named by chunk coordinates.
    /// </summary>
    public static readonly GridCoord FloorKeyZone = new GridCoord(int.MinValue, int.MinValue);

    /// <summary>
    /// The one-line prompts on a floor-key floor (A22, FIX_CARD_VL217 B): the locked door without the floor key, and
    /// with it. The same words as FrontRoomsKeyHud.Strings (Locked + NeedsKey joined; Unlock), which draws them as two
    /// lines whenever it owns the prompt; these show only where it does not (no key HUD, or its vector shader failed).
    /// </summary>
    public const string LockedNeedsStairKey = "LOCKED  ·  NEEDS STAIR KEY", UnlockPrompt = "E  ·  UNLOCK";

    // This floor's locks follow one floor key (M3): SetStation with a KeySpot sets it; ForgetStation clears it.
    bool floorKey;
    // A13: Floor 1 has no locked inner doors (the carved start group is floor 1's); SetStation sets it.
    bool noInteriorLocks;

    /// <summary>
    /// M3 is on for this floor: the floor has a station with a KeySpot (SetStation). Then one floor key lies at the
    /// KeySpot (BuildInto, "Key · floor", look+E like every key), the zone keys are not spawned, the floor key opens the
    /// EXIT and every locked door on the floor (KeyFor, Kind), and its take raises StationKeyTaken (M6). The game locks
    /// the EXIT from the floor's start (SetExitLocked, after SetExitArea). False with no station: every key rule is
    /// today's, unchanged.
    /// </summary>
    public bool FloorKeyRule => floorKey;

    /// <summary>The player holds this floor's floor key (M3). False with no floor key rule.</summary>
    public bool FloorKeyHeld => floorKey && keysHeld.Contains(FloorKeyZone);

    // Q1 B (Red 2026-10-29 17:21): this floor builds no zone keys (ZoneKeysOff). NextFloor clears it; the game sets it again.
    bool zoneKeysOff;

    /// <summary>
    /// Q1 B (Red 2026-10-29 17:21): no zone keys on this floor. The game sets it right after NextFloor (which clears it)
    /// on every floor below a station floor 1, so floors 2 and deeper of a station run build no zone key (BuildInto: no
    /// zone key cube), while their data keeps every key site (the carve hash, L5 and the verification are unchanged)
    /// and no inner door waits for a key that is never built (InteriorDoorsLocked false, whatever the keys flag). The
    /// floor key's rule (FloorKeyRule) is untouched; the EXIT stays unlocked. Set it before the floor's chunks build: a
    /// chunk already built keeps its key until it is rebuilt. False on every floor of a run with no station (today's).
    /// </summary>
    public bool ZoneKeysOff
    {
        get => zoneKeysOff;
        set => zoneKeysOff = value;
    }

    /// <summary>
    /// Inner (not EXIT) doors are locked by the level's rule: doors need keys (the profile's doorsNeedKeys, or
    /// DoorsNeedKeysForTools), except on a floor with no locked inner doors (A13: Floor 1, while the station is held;
    /// Q1 B: a floor with ZoneKeysOff). Equals DoorsNeedKeysForTools with no station and ZoneKeysOff false.
    /// </summary>
    public bool InteriorDoorsLocked => doorsNeedKeys && !noInteriorLocks && !zoneKeysOff;

    /// <summary>The key that opens a locked door for a player in <paramref name="cell"/>: the floor key on a floor-key floor, else that cell's zone key.</summary>
    public GridCoord KeyZoneAt(GridCoord cell) => floorKey ? FloorKeyZone : Cache.ZoneOf(cell).id;

    /// <summary>The player holds the key that opens a locked door from <paramref name="cell"/> (KeyZoneAt). False with no cache.</summary>
    public bool HoldsKeyAt(GridCoord cell) => Cache != null && keysHeld.Contains(KeyZoneAt(cell));

    /// <summary>Tools and tests only: lift Floor 1's "no locked inner doors" (A13) to exercise "the floor key opens every locked door on its floor".</summary>
    public bool NoInteriorLocksForTools
    {
        get => noInteriorLocks;
        set
        {
            if (noInteriorLocks == value) return;
            noInteriorLocks = value;
            TouchPassageRevision();
        }
    }

    /// <summary>
    /// A built chunk holding station cells is registered: the chunk, and its renderers drawn in the station's own
    /// Surfaces (none while those fall back to Level 0's). For the visual chat's rendering layers (61 7). The set
    /// piece's renderers are Station.Renderers. A handler that throws is logged and the next one still runs.
    /// </summary>
    public event Action<GridCoord, IReadOnlyList<Renderer>> StationShellBuilt;

    /// <summary>
    /// Count a collider as the map's architecture (IsArchitecture), like a shell wall: the Relay never walks through
    /// it, even where it passes through furniture (FrontRoomsMapHunter.Ignored). For the set pieces the map builds,
    /// the station's riser block first. Forgotten with UnregisterArchitecture, and all of them on NextFloor.
    /// </summary>
    public void RegisterArchitecture(Collider c)
    {
        if (c != null) setPieceColliders.Add(c);
    }

    public void UnregisterArchitecture(Collider c)
    {
        if (c != null) setPieceColliders.Remove(c);
    }

    /// <summary>Tools and tests: the station's wall and floor materials for the next SetStation (null: the Surfaces, as in play), so the skin split can be told from Level 0's.</summary>
    public void StationSkinForTools(Material wall, Material floor)
    {
        stationWallForTools = wall;
        stationFloorForTools = floor;
    }

    // The station's skin: the walls 2 Surfaces (or the tools' materials), Level 0's lens and lamp, Level 0's ceiling
    // unless the station has its own. Missing wall or floor fall back to Level 0's with one warning.
    ThemeMaterials StationMaterials()
    {
        var wall = stationWallForTools != null ? stationWallForTools : FrontRoomsSurfaces.TryGet(StationWallSurface);
        var floor = stationFloorForTools != null ? stationFloorForTools : FrontRoomsSurfaces.TryGet(StationFloorSurface);
        var ceiling = FrontRoomsSurfaces.TryGet(StationCeilingSurface);
        if (wall == null || floor == null)
            Debug.LogWarning("[FrontRoomsMap] Station surfaces " + (wall == null ? StationWallSurface + " " : "") + (floor == null ? StationFloorSurface + " " : "")
                + "missing (Resources/Surfaces): the station's cells take Level 0's until the visual chat's walls 2 set exists.");
        return new ThemeMaterials
        {
            wall = wall != null ? wall : level0.wall,
            floor = floor != null ? floor : level0.floor,
            ceiling = ceiling != null ? ceiling : level0.ceiling,
            lens = level0.lens,
            lampIntensity = level0.lampIntensity,
            lampColor = level0.lampColor,
        };
    }

    // The materials a cell's own faces, floor, ceiling and lamp take: the station's skin on the station's cells
    // (Cache.SkinOf, the carve's footprint), else its zone's theme. With no station: Theme(zone.theme), as before.
    ThemeMaterials MaterialsOf(GridCoord cell, ZoneInfo zone) =>
        stationSkin != null && Cache.SkinOf(cell) == MapSkin.Station ? stationSkin : Theme(zone.theme);

    // The set piece shows while the station's chunk c or X's chunk c' is built.
    void UpdateStationVisibility()
    {
        if (stationSet == null) return;
        stationSet.SetVisible(built.ContainsKey(station.DoorChunk) || built.ContainsKey(station.ExitChunk));
    }

    // One budget of the station's props (Update, before the rooms' furnishing): false when there is nothing to do.
    bool StepStation()
    {
        if (stationSet == null || stationSet.PropsDone) return false;
        stepWatch.Restart();
        stationSet.StepProps(dressBudgetMs);
        NoteWork("station props");
        return true;
    }

    void RaiseStationShellBuilt(GridCoord coord, BuiltChunk chunk)
    {
        if (!hasStation || StationShellBuilt == null || chunk.root == null) return;
        var o = MapGrid.ChunkOrigin(coord);
        var s = station.station;
        if (s.x >= o.x + MapGrid.ChunkCells || o.x >= s.x + s.w || s.y >= o.y + MapGrid.ChunkCells || o.y >= s.y + s.h) return;
        var own = new List<Renderer>();
        foreach (var r in chunk.root.GetComponentsInChildren<Renderer>(true))
        {
            var m = r.sharedMaterial;
            if (m == null) continue;
            if ((m == stationSkin.wall && m != level0.wall) || (m == stationSkin.floor && m != level0.floor) || (m == stationSkin.ceiling && m != level0.ceiling)) own.Add(r);
        }
        foreach (Action<GridCoord, IReadOnlyList<Renderer>> handler in StationShellBuilt.GetInvocationList())
        {
            try { handler(coord, own); }
            catch (Exception e) { Debug.LogException(e); }
        }
    }

    // ---------- The EXIT sign (C23 step 1, Red 2026-10-22; the visual chat, research/exit_sign/70_lit_lock.md) ----------

    /// <summary>The flat wall sign over the EXIT door, and the end-on blade that takes its place where a corridor runs past the door.</summary>
    public const string ExitSignKit = "Kit_ExitSign", ExitSignEndKit = "Kit_ExitSign_End";

    /// <summary>
    /// The steel frame kit's EXIT sign anchor (10_directions.md 1.2, C23 round 2): Kit_DoorFrame_Steel's exit_sign_p,
    /// at (-0.08, 2.18, 0.5) in its door root (origin the hinge-jamb edge on the wall centre line at the floor; +Z
    /// along the opening; +X the swing (pull) face, so -X is the push face, N's). The map mounts its sign there.
    /// </summary>
    public const string ExitSignFrameKit = "Kit_DoorFrame_Steel", ExitSignAnchor = "exit_sign_p";

    /// <summary>
    /// The sign's bottom over the floor: the anchor's height, 2.18 m (C23 round 2; was 2.674 m, the room stream's
    /// door-header placement). The map's head trim ends at DoorHeight + TrimFace = 2.17 m, so the sign sits 10 mm
    /// over it, its top at 2.392 m (Kit_ExitSign and Kit_ExitSign_End are 0.212 m tall).
    /// </summary>
    public const float ExitSignBottom = 2.18f;

    /// <summary>
    /// The sign's back, from the wall's centre line toward N: the anchor's 0.08 m, which is N's face of the map's
    /// 0.16 m wall (ModuleUnits.WallHalf). The back faces into the wall, so nothing of it shows from N.
    /// </summary>
    public const float ExitSignBack = .08f;

    /// <summary>The sign's diffuser emission (FrontRoomsExitSign.EmissionGain): 2.0 with the steel frame (10_directions.md 1.2 and 5.3: T2 2.44).</summary>
    public const float ExitSignEmissionGain = 2f;

    /// <summary>
    /// The floor's EXIT sign: the LIT-locked sign over the EXIT door on N's side, built with X's chunk (rebuilt with
    /// it, gone when it drops or the floor changes); null while that chunk is not built, or with no exit area.
    /// </summary>
    public FrontRoomsExitSign ExitSign => hasExitArea && built.TryGetValue(MapGrid.ChunkOf(exitCell), out var chunk) && chunk.exitSign != null ? chunk.exitSign : null;

    /// <summary>
    /// The corridor rule, from the edges as built (EffectiveEdge): N's west and east edges both let the player
    /// through (neither a wall nor glass), so the corridor runs past the EXIT door in the side of its wall and the
    /// sign over it is the end-on blade (Kit_ExitSign_End), readable down the corridor both ways; otherwise the
    /// flat sign faces the player coming up to the door. FrontRoomsExitPlanner.CorridorPastDoor reads the same
    /// rule from data (ExitPlan.corridor). False with no exit area.
    /// </summary>
    public bool ExitSignIsEnd => hasExitArea
        && FrontRoomsExitPlanner.Walkable(EffectiveEdge(exitFront, new GridCoord(exitFront.x - 1, exitFront.y)))
        && FrontRoomsExitPlanner.Walkable(EffectiveEdge(exitFront, new GridCoord(exitFront.x + 1, exitFront.y)));

    /// <summary>
    /// Mount the EXIT sign on the EXIT door's header (BuildEdge, as X's chunk builds the door): N's side only, the
    /// side the player comes up to the door from (the door's push face), at the steel frame's exit_sign_p anchor:
    /// centred on the opening, its bottom at ExitSignBottom (on the head trim), its back ExitSignBack from the wall's
    /// centre line (on N's face of the wall), its front facing into N. The end-on blade takes the same mount: its
    /// own origin is the wall plane at its housing bottom (Kit_ExitSign_End "wall" anchor), projecting into N.
    /// Spawned through the kit library like the map's other kits (no colliders), so the library gives it its lamp
    /// driver, which gets EmissionGain 2.0 and is locked LIT (LockLit: never HALF, LOOSE or Dead, and deaf to SetMains:
    /// brownouts, W1 and the title fade never dim it) and dealt Lit with no light of its own; none of its renderers
    /// casts a shadow (10_directions 1.2, the strip fix). X's side gets none: the stairwell carries no EXIT sign
    /// (10_directions 1.2). A wall too low for it gets none either, with a warning (the planner only picks a
    /// Standard N).
    /// </summary>
    FrontRoomsExitSign MountExitSign(BuiltChunk chunk, GridCoord a, GridCoord b, Vector3 start, Vector3 along, float c, float height)
    {
        // across points from a into b; the sign goes on N's face, toward N.
        var across = new Vector3(along.z, 0f, along.x);
        var intoN = a == exitFront ? -across : across;
        var end = ExitSignIsEnd;
        var kit = end ? ExitSignEndKit : ExitSignKit;
        var info = FrontRoomsKitLibrary.GetInfo(kit);
        var signHeight = info != null ? info.Size.y : .2124f;
        if (height < ExitSignBottom + signHeight)
        {
            Debug.LogWarning("[FrontRoomsMap] No EXIT sign over " + a + "-" + b + ": N's wall is " + height.ToString("0.00") + " m, the sign needs "
                + (ExitSignBottom + signHeight).ToString("0.000") + " m. FrontRoomsExitPlanner only picks a Standard N.");
            return null;
        }
        var position = start + along * c + intoN * ExitSignBack + Vector3.up * ExitSignBottom;
        var sign = FrontRoomsKitLibrary.Spawn(kit, chunk.root.transform, position, Quaternion.LookRotation(intoN, Vector3.up), null, false,
            "EXIT sign " + (end ? "end " : "") + a + "-" + b);
        if (sign == null)
        {
            Debug.LogWarning("[FrontRoomsMap] No EXIT sign over " + a + "-" + b + ": the kit " + kit + " is missing.");
            return null;
        }
        // The kit library turns shadows on for kits over 0.3 m (both signs are): the sign casts none.
        foreach (var r in sign.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = ShadowCastingMode.Off;
        // The kit library attaches the driver to exit_sign kits; the gain before the lock, which applies it, then the deal.
        var driver = sign.GetComponent<FrontRoomsExitSign>();
        if (driver == null) driver = sign.AddComponent<FrontRoomsExitSign>();
        driver.EmissionGain = ExitSignEmissionGain;
        driver.LockLit = true;
        driver.Configure(0u, FrontRoomsExitSign.Temperament.Lit, null, 0f);
        return driver;
    }

    /// <summary>
    /// The EXIT door's frame (its two jambs and head trim, built as every door's are) as a renderer of its own under
    /// X's chunk, "EXIT frame N-X", that casts no shadow (C23 round 2; 10_directions.md 1.2: the exit frame and the
    /// sign cast no shadow, the strip fix). It still receives shadows; every other door's frame is unchanged.
    /// Returns the object (C23 round 3: ExitDoorBuilt's "frame"): unscaled, at the chunk root's origin with no turn.
    /// </summary>
    Transform BuildExitFrame(BuiltChunk chunk, GridCoord a, GridCoord b, MeshBuilder trims, float height)
    {
        var go = new GameObject("EXIT frame " + a + "-" + b);
        go.transform.SetParent(chunk.root.transform, false);
        var mesh = AddRenderer(go, trim, trims, chunk.root.transform.position.y + HeightOfClass(HeightClass(height)));
        if (mesh == null) return go.transform;
        chunk.meshes.Add(mesh);
        foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = ShadowCastingMode.Off;
        return go.transform;
    }

    /// <summary>The exit device on the EXIT leaf's push face (10_directions.md 1.2).</summary>
    public const string ExitCrossbarKit = "Kit_ExitDevice_Crossbar";

    /// <summary>
    /// Where the crossbar's latch case stands on the leaf: 1.0 m up, and 0.1 m in from the leaf's latch edge (the
    /// kit's door (-0.022, 1.000, 0.900): 0.1 m in from the 1.0 m opening's latch edge; the map's leaf ends 0.01 m
    /// short of it, DoorLeafGap / 2, so its bar runs 0.89 m to 0.05 m from the hinge jamb).
    /// </summary>
    public const float ExitCrossbarHeight = 1f, ExitCrossbarLatchInset = .1f;

    static bool exitCrossbarMissingLogged;

    /// <summary>The crossbar's name under the EXIT door's leaf rig (C23 round 3: exactly this, so the dressing finds it with leaf.Find).</summary>
    public const string ExitCrossbarName = "EXIT crossbar";

    /// <summary>The name of the EXIT door's leaf rig under its hinge (ExitDoorBuilt's "leaf").</summary>
    public const string ExitLeafRigName = "Leaf rig";

    /// <summary>
    /// The EXIT door's leaf rig (C23 round 2; round 3: every EXIT door has one, crossbar kit or not): an unscaled
    /// object "Leaf rig" under the hinge that PoseLeaf keeps on the leaf child (the swing through the hinge, a jolt,
    /// a crooked hang); shut and still it is the hinge's own frame. Hardware on the leaf hangs here, never on the
    /// cube "Door leaf", which is scaled to the leaf's size.
    /// </summary>
    Transform MountLeafRig(Door door)
    {
        var rig = new GameObject(ExitLeafRigName).transform;
        rig.SetParent(door.hinge, false);
        door.leafRig = rig;
        // A door rebuilt open, broken or crooked: the rig takes the leaf's pose at once.
        PoseLeaf(door);
        return rig;
    }

    /// <summary>
    /// Mount Kit_ExitDevice_Crossbar on the EXIT leaf's push face, N's side (BuildEdge, right after the EXIT sign).
    /// The kit places it at (-0.022, 1.000, 0.900), Euler (0, -90, 0), scale (-1, 1, 1) in its door root: origin the
    /// hinge-jamb edge on the wall's centre line at the floor, +Z along the opening, +X the swing (pull) face. The
    /// map's hinge has the same origin, +Z along the opening and +Y up, but its +X is the push face when the door
    /// swings +1 (the EXIT always does: ExitSwing), so the kit's frame is the hinge's mirrored in X, and the same
    /// placement reads, in the hinge's frame, (+leaf face, 1.0, latch end) at Euler (0, 90, 0), scale (1, 1, 1): the
    /// bar from the latch case toward the hinge (-Z), its face out of the push face (+X), never mirrored (for a door
    /// swinging -1 the frames agree and the kit's own rotation and scale stand). The kit's 0.022 is its own leaf's
    /// face: the map's cube leaf is 0.05 m thick, so the case stands on its face at 0.025, 1.0 m up, its latch end
    /// ExitCrossbarLatchInset in from the leaf's latch edge. It hangs on the door's leaf rig (MountLeafRig, made just
    /// before it), named exactly ExitCrossbarName, so it swings, jolts and hangs crooked with the leaf, and it goes
    /// with X's chunk (dropped, rebuilt, NextFloor). No colliders, no EXAMINE. A missing kit is logged once and skipped
    /// (the leaf rig stays: the dressing hangs on it).
    /// </summary>
    GameObject MountExitCrossbar(Door door, float width)
    {
        if (!FrontRoomsKitLibrary.Has(ExitCrossbarKit))
        {
            if (!exitCrossbarMissingLogged) Debug.LogWarning("[FrontRoomsMap] No crossbar on the EXIT door: the kit " + ExitCrossbarKit + " is missing.");
            exitCrossbarMissingLogged = true;
            return null;
        }
        var rig = door.leafRig != null ? door.leafRig : MountLeafRig(door);
        // The push face is the hinge's +X when the leaf swings +1 (into b for a north edge), its -X when it swings -1.
        var push = door.swing >= 0f ? 1f : -1f;
        var position = new Vector3(push * ModuleUnits.DoorLeafThickness * .5f, ExitCrossbarHeight, width - ModuleUnits.DoorLeafGap * .5f - ExitCrossbarLatchInset);
        var scale = push > 0f ? Vector3.one : new Vector3(-1f, 1f, 1f);
        return FrontRoomsKitLibrary.Spawn(ExitCrossbarKit, rig, position, Quaternion.Euler(0f, 90f * push, 0f), scale, false, ExitCrossbarName, false);
    }

    /// <summary>The EXIT leaf's crossbar (MountExitCrossbar), or null while X's chunk is not built, with no exit area, or with no kit.</summary>
    public Transform ExitCrossbar => hasExitArea && built.TryGetValue(MapGrid.ChunkOf(exitCell), out var chunk) && chunk.exitCrossbar != null ? chunk.exitCrossbar.transform : null;

    // ---------- The EXIT door for the visual dressing, and its lock (C23 round 3, agreed with the visual chat) ----------

    /// <summary>
    /// C23 round 3 (agreed with the visual chat, 10_directions.md 7: a map event instead of more map-spawned kits): the
    /// EXIT door was built, for the dressing that hangs the steel leaf and frame materials, the closer, the NO RE-ENTRY
    /// plate and A13's chain and padlock on it. Arguments (door, hinge, leaf, frame):
    /// <list type="bullet">
    /// <item><description>door: the map's Door for the EXIT edge (N to X), the one ExitDoor returns now.</description></item>
    /// <item><description>hinge: door.hinge, unscaled, on the hinge-jamb edge at the wall's centre line and the floor; +Z
    /// along the opening, +Y up, +X the push face (N's side: the EXIT always swings +1, into X). It turns with the swing
    /// (its shut pose is door.closed), so frame hardware never hangs on it.</description></item>
    /// <item><description>leaf: the door's "Leaf rig" (ExitLeafRigName), an UNSCALED transform under the hinge that the map
    /// keeps on the leaf (PoseLeaf: the swing, a rattle's or a blow's jolt, a broken leaf's crooked hang); shut and still
    /// it is the hinge's own frame. Dressing on the leaf parents here. It is not door.leaf: that is the cube "Door
    /// leaf", scaled to the leaf's size (0.05 m thick), which would stretch anything under it. The map's crossbar is
    /// the rig's child ExitCrossbarName ("EXIT crossbar"; none when its kit is missing).</description></item>
    /// <item><description>frame: the "EXIT frame N-X" object under X's chunk root (the jambs and head trim, C23 round 2),
    /// unscaled, at the chunk root's origin with no turn; it never moves. Its space is the hinge's parent's, so the
    /// door's shut frame there is (door.hinge.localPosition, door.closed). Frame hardware parents here.</description></item>
    /// </list>
    /// When: once for every build of the EXIT edge: X's chunk's first build, RebuildChunk (a module swap), its return
    /// after a drop (a revisit), and every floor's EXIT after NextFloor (SetExitArea, then Begin builds it). BuildEdge
    /// makes the frame, the leaf, the leaf rig, the crossbar and the sign; the event goes out as that chunk's build
    /// completes and it is registered (where WindowBuilt goes out), so ExitDoor, ExitSign, ExitCrossbar and ExitLocked
    /// already answer inside the handler. A chunk whose build throws is undone and raises nothing. Read ExitLocked in
    /// the handler for the lock's state at the build; ExitLockChanged carries later changes.
    /// Lifetime: whatever the handler parents under leaf or frame is destroyed with X's chunk: when the chunk drops out
    /// of range, when it is rebuilt, and when the floor changes (NextFloor). No release event is raised; the next build
    /// makes fresh transforms and raises this again, so a handler dresses every build from scratch (idempotent per
    /// build) and keeps nothing from an earlier one (those objects are destroyed: Unity's == null says so). Subscribe
    /// before Begin: a handler added later hears the next build only.
    /// Handlers are called one at a time; one that throws is logged (Debug.LogException) and never breaks the build,
    /// the map's bookkeeping or the other handlers.
    /// </summary>
    public event Action<Door, Transform, Transform, Transform> ExitDoorBuilt;

    // ExitDoorBuilt for a chunk that built the EXIT edge, one handler at a time (as RaiseWindowBuilt).
    void RaiseExitDoorBuilt(BuiltChunk chunk)
    {
        var door = chunk.exitDoor;
        if (door == null || ExitDoorBuilt == null) return;
        foreach (Action<Door, Transform, Transform, Transform> handler in ExitDoorBuilt.GetInvocationList())
        {
            try { handler(door, door.hinge, door.leafRig, chunk.exitFrame); }
            catch (Exception e) { Debug.LogException(e); }
        }
    }

    /// <summary>
    /// C23 round 3 (A13's lock seam): the EXIT door is locked by its floor key. Locked, it is a locked map door: E rattles
    /// it without the key (UseResult.Rattled with DoorLocked, DoorRattled and the leaf's jolt, which the leaf rig and its
    /// chain follow), TryOpenDoor leaves it and LockedByRule is true for it. No zone key fits it (Kind Locked, KeyFor
    /// none, the zone keys' prompt). M3: on a floor-key floor (FloorKeyRule) the game locks it from the floor's start
    /// (after SetExitArea, before Begin) and the floor key opens it: Describe "LOCKED  ·  NEEDS STAIR KEY", then with the
    /// key Kind LockedHaveKey, "E  ·  UNLOCK", and E's key turn (the unlock shot) lifts the lock at its commit
    /// (CommitUnlock). False on every floor without a station. Kept by the map, not the door, so rebuilds and
    /// revisits keep it (read it in ExitDoorBuilt). A new or cleared exit area (SetExitArea, ClearExitArea, so every
    /// NextFloor) starts it false with no ExitLockChanged. Separate from the seal (ExitSealing, ExitSealed,
    /// ExitDoorSealed: the leaf latched behind the player at the floor's end), which works locked or not and is not
    /// changed by it. False with no exit area.
    /// </summary>
    public bool ExitLocked => hasExitArea && exitLocked;

    /// <summary>
    /// The EXIT's lock (ExitLocked) changed: the door (null when X's chunk is not built; its next ExitDoorBuilt reads
    /// ExitLocked) and the new state. Raised only on a real change, by SetExitLocked; never by SetExitArea,
    /// ClearExitArea or NextFloor, which put it back to false for a door that is not built yet or is gone. For A13's
    /// chain and padlock (locked: the chain round the crossbar; unlocked: dropped) and their sound. Handlers are
    /// called one at a time; one that throws is logged and never stops the change or the other handlers.
    /// </summary>
    public event Action<Door, bool> ExitLockChanged;

    /// <summary>
    /// A13 / C15's lock seam: lock or unlock the EXIT door (ExitLocked). True when the state changed, with one
    /// ExitLockChanged; false when it was so already (nothing raised), or with no exit area. Locking a door that is open
    /// or opening leaves it so (E still shuts it, the seal still shuts it); shut, it opens for nobody until it is
    /// unlocked. Locking never seals, and the seal never locks. M3: the game locks it on a floor-key floor; the floor
    /// key's committed key turn unlocks it (CommitUnlock: this, then DoorUnlocked, then the open). A direct
    /// SetExitLocked(false) (tools) only lifts the lock, and E opens it as any door.
    /// </summary>
    public bool SetExitLocked(bool locked)
    {
        if (!hasExitArea || exitLocked == locked) return false;
        exitLocked = locked;
        var door = ExitDoor;
        if (ExitLockChanged != null)
            foreach (Action<Door, bool> handler in ExitLockChanged.GetInvocationList())
            {
                try { handler(door, locked); }
                catch (Exception e) { Debug.LogException(e); }
            }
        return true;
    }

    /// <summary>True for the EXIT door (this map's exit edge, whichever chunk build made the Door object).</summary>
    public bool IsExitDoor(Door door) => hasExitArea && door != null && door.edge == exitEdge;

    bool IsExitEdge(GridCoord a, GridCoord b) => hasExitArea && ((a == exitFront && b == exitCell) || (a == exitCell && b == exitFront));

    /// <summary>
    /// The one place the EXIT door's side is decided (D6): it swings into X, away from the player, who opens it
    /// from N (A12's rule, Red 2026-10-22, holds for it before and after the double-acting doors land). X is N's
    /// north neighbour, so the edge is N's north edge, and Door.swing +1 sends a north edge's leaf north, into X.
    /// FixedSwing returns it for the exit edge; nothing else of the door changes.
    /// </summary>
    const float ExitSwing = 1f;

    bool IsExitChunk(GridCoord coord) => hasExitArea && MapGrid.ChunkOf(exitCell) == coord;

    // The sealed EXIT door: no prompt, no kind, nothing on E or from TryOpenDoor, from SealExit on.
    bool IsSealedExit(Door door) => exitSeal != ExitSeal.None && IsExitDoor(door);

    bool RoomHoldsExit(MapChunk data, CellRect room)
    {
        if (!hasExitArea) return false;
        var o = data.Origin;
        return room.Contains(exitCell.x - o.x, exitCell.y - o.y);
    }

    /// <summary>
    /// The start area's rules for a room (not furnished, no Relay entries, its key back at the site cell), and the
    /// same for a room holding X, which FrontRoomsExitPlanner never chooses (a stairwell in a carved room would
    /// stand among its furniture).
    /// </summary>
    bool RoomInReserved(MapChunk data, CellRect room) => RoomInStartArea(data, room) || RoomHoldsExit(data, room);

    /// <summary>True when a column or bulkhead between two cell corners would touch the start area, X, or their edges.</summary>
    bool TouchesReserved(int x0, int y0, int x1, int y1) =>
        TouchesStartArea(x0, y0, x1, y1) || (hasExitArea && x1 >= exitCell.x && x0 <= exitCell.x + 1 && y1 >= exitCell.y && y0 <= exitCell.y + 1);

    /// <summary>The chunk holding the cell is built, reserved cells included (IsBuilt is false for those): the stairwell shows itself while X's chunk stands.</summary>
    public bool IsChunkBuiltAt(GridCoord cell) => built.ContainsKey(MapGrid.ChunkOf(cell));

    /// <summary>
    /// The paper the map hangs on a cell's side of its walls (its zone's theme; the map owns the material), for
    /// a wall another object builds where the map leaves a gap: the stairwell's north end wall on the floor
    /// that starts in it, seen from its door cell (FrontRoomsStairwell.JoinNextFloor, C22 D8).
    /// </summary>
    public Material WallPaperOf(GridCoord cell) => MaterialsOf(cell, Cache.ZoneOf(cell)).wall;

    /// <summary>
    /// What crossing the EXIT door costs a sound right now, by the hearing field's door rule (OpeningCostBetween's
    /// for any other door): 0 while it is open past DoorPassableDegrees (or broken), AjarOpening while it is open
    /// short of that, ShutOpening while it is shut (sealed or not); a door whose chunk is not built reads its record.
    /// OpeningCostBetween itself reads N to X as no way (X is no map), so the Relay's hearing adds this to a noise
    /// made inside X, with the step from X to N. Infinity when the map has no exit area.
    /// </summary>
    public float ExitOpeningCost() => hasExitArea ? DoorOpeningCost(exitEdge) : float.PositiveInfinity;

    /// <summary>SealExit was called: the EXIT door is shut, or shutting, for good: no prompt, no kind, nothing on E.</summary>
    public bool ExitSealing => hasExitArea && exitSeal != ExitSeal.None;

    /// <summary>The EXIT door latched after SealExit and is locked: the floor can change.</summary>
    public bool ExitSealed => hasExitArea && exitSeal == ExitSeal.Locked;

    /// <summary>
    /// The EXIT door locked (SealExit): as it latched (right after DoorLatched), or at once when it was already
    /// shut and latched. The door (null when X's chunk is not built, sealed by its record alone) and the middle
    /// of the door's line. The sound layer's lock beat and the game's swap can key on it.
    /// </summary>
    public event Action<Door, Vector3> ExitDoorSealed;

    /// <summary>
    /// The seal (D7): the map shuts the EXIT door for the player, if it is open, and locks it once it has
    /// latched: from this call on, Use, Describe, Kind and TryOpenDoor leave it alone (no prompt, nothing on E),
    /// so nothing opens it again between the call and the latch. A body in the leaf's way holds the shut as a
    /// player's E would (the game only seals with the player well inside X). The shut is a door move like any
    /// other (DoorMoved: the Relay hears it, from N). A door already shut and latched locks at once; one whose
    /// chunk is not built is shut by its record and locked at once (it is rebuilt shut). True when the door is
    /// locked or will lock at its latch; false with no exit area, or for a broken door, which never shuts again
    /// (the Relay never reaches the EXIT door, X being no map to it, so that is only a guard). Option A's floor
    /// key locks it by rule instead (ExitLocked, SetExitLocked: LockedByRule's seam, C23 round 3), which this
    /// neither reads nor changes; this seal is the end of a floor either way.
    /// </summary>
    public bool SealExit()
    {
        if (!hasExitArea) return false;
        if (exitSeal == ExitSeal.Locked) return true;
        var door = ExitDoor;
        if (door == null)
        {
            if (brokenDoors.Contains(exitEdge)) return false;
            openDoors.Remove(exitEdge);
            LockExit(null);
            return true;
        }
        if (door.broken || IsBeingBroken(door)) return false;
        exitSeal = ExitSeal.Shutting;
        // It needs no key, so no key turn ever holds it; none may outlive the seal either way.
        keyTurns.Remove(exitEdge);
        if (door.open) ShutDoor(door, player != null ? player.position : door.position);
        else if (door.motion == DoorMotion.None) LockExit(door);
        // Otherwise it is shutting already (or resting on a body on its way shut): its latch locks it (TickSwing).
        return true;
    }

    void LockExit(Door door)
    {
        exitSeal = ExitSeal.Locked;
        ExitDoorSealed?.Invoke(door, door != null ? door.position : CrossingPoint(exitFront, exitCell));
    }

    /// <summary>
    /// Map lamps within their light's range of X (the exit group), held by the game like the start groups: a lamp
    /// with no shadow lights straight through the stairwell's walls, so the blackout swap (D9) takes them down
    /// with the stairwell's own lamp and the floor changes in the dark. 0 is dark, 1 normal; a new or cleared
    /// exit area starts at 1.
    /// </summary>
    public float ExitLamps { get; set; } = 1f;

    bool ExitLampGroup(GridCoord cell, float range)
    {
        if (!hasExitArea) return false;
        var cs = MapGrid.CellSize;
        var cx = (cell.x + .5f) * cs;
        var cz = (cell.y + .5f) * cs;
        var dx = Mathf.Max(exitCell.x * cs - cx, 0f, cx - (exitCell.x + 1) * cs);
        var dz = Mathf.Max(exitCell.y * cs - cz, 0f, cz - (exitCell.y + 1) * cs);
        return dx * dx + dz * dz < range * range;
    }

    // The exit group of the lamps already built (an exit area set or cleared while chunks stand; normally none are).
    void RegroupExitLamps()
    {
        foreach (var chunk in built.Values)
            foreach (var f in chunk.fixtures)
                f.exitGroup = f.light != null && ExitLampGroup(f.cell, f.light.range);
    }

    // ---------- The next floor, in place (C22, D1) ----------

    /// <summary>
    /// C22 (plan v2 D1): go one floor deeper in place. The same map object reseeds instead of a second map being
    /// made, so everything bound to it stays bound (the game's handlers, the Relay, the warning lamps, the door
    /// shot, the sound layer, EXAMINE, the glass kit). Every built chunk goes as a drop takes it (its windows'
    /// WindowReleased, its doors, colliders, lamps and keys forgotten, its meshes freed), out of the physics
    /// scene at once (a deferred Destroy would leave its colliders standing this frame). The furnishing job and
    /// queue are cancelled. Every record of the floor goes: open, broken and unlocked doors, doors being broken,
    /// key turns, broken windows and glass breaks, lamp modes, overrides and holds (ambient and look), keys held and announced, dropped
    /// and failed chunks, the start keep-clears, and the exit area with its seal and its lock (ExitLocked back to
    /// false with no ExitLockChanged for the torn-down door; the EXIT dressing went with X's chunk, and the next
    /// floor's EXIT raises ExitDoorBuilt when Begin builds it). Then the profile is taken again
    /// with <paramref name="seed"/> (the run's keys flag, which tools may set for the run, is kept) and a new
    /// cache made (hand-placed modules kept) that generates at <paramref name="tier"/> from its first chunk (the
    /// tier carries over: set before anything probes the cache, D13); the start area is set (floors 2 and up: the
    /// previous X, its door X + (0, 1)); and PassageRevision moves on, so every path cache of the old floor is
    /// stale. The root transform stays where it is (D2: the floor's seed is searched so the root need not move
    /// under the player). Run-wide state stays: KeyPickupNeedsE, the key presenter and turn owner, DoorsPaused,
    /// RelayBody, StreamFocus and the start lamp factors (the game's floor start sets both), the doors' clock,
    /// ShiftedChunks and the map's own materials. The map stops streaming until the caller, having chosen the
    /// floor's EXIT on Cache and set it (SetExitArea), calls Begin(player, false).
    /// </summary>
    public void NextFloor(int seed, int tier, RectInt startArea, GridCoord startDoorCell)
    {
        // A26: the station and its set piece go with the floor (FloorHasStation false); its carve dies with the old cache.
        ForgetStation();
        // Q1 B: the new floor's zone keys are today's unless the game sets ZoneKeysOff again (before Begin).
        zoneKeysOff = false;
        // The open furnishing job first: what it made goes with its chunk.
        if (currentJob != null) currentJob.Cancel();
        currentJob = null;
        currentRoom = default;
        dressQueue.Clear();
        // As Drop does: unregistered while still listed as built, then forgotten.
        var chunks = new List<BuiltChunk>(built.Values);
        foreach (var chunk in chunks)
        {
            if (chunk.root != null) chunk.root.SetActive(false);
            Unregister(chunk);
        }
        built.Clear();
        // Unregister forgets what each chunk built; these go too, whatever a chunk left behind.
        doorByCollider.Clear();
        doorByEdge.Clear();
        windowByCollider.Clear();
        shellColliders.Clear();
        setPieceColliders.Clear();
        keyByCollider.Clear();
        fixtureByCell.Clear();
        movingDoors.Clear();
        droppedAt.Clear();
        failedChunks.Clear();
        openDoors.Clear();
        ajarDoors.Clear();
        doorSides.Clear();
        brokenDoors.Clear();
        keptBlows.Clear();
        unlockedDoors.Clear();
        breakingDoors.Clear();
        keyTurns.Clear();
        brokenWindows.Clear();
        glassBreaks.Clear();
        lampModes.Clear();
        lampOverrides.Clear();
        overridesByCell.Clear();
        ambientHolds.Clear();
        lampHolds.Clear();
        keysHeld.Clear();
        keysAnnounced.Clear();
        ClearExitArea();
        var keys = doorsNeedKeys;
        TakeProfile(Profile, seed);
        doorsNeedKeys = keys;
        CreateCache();
        GenerationTier = tier;
        SetStartArea(startArea, startDoorCell);
        begun = false;
        TouchPassageRevision();
    }

    /// <summary>
    /// Edit-mode build of the chunks around the spawn, used for captures.
    /// Returns the eye position. The caller restores render settings.
    /// </summary>
    public Vector3 BuildForCapture()
    {
        TakeProfile(Profile, Profile.generation.seed);
        CreateCache();
        block = new MaterialPropertyBlock();
        BuildMaterials();
        ApplyRenderSettings();
        var eye = new GameObject("CAPTURE / eye").transform;
        eye.SetParent(transform, false);
        eye.localPosition = SpawnPoint();
        Begin(eye);
        return transform.TransformPoint(SpawnPoint() + Vector3.up * ModuleUnits.PlayerEye);
    }

    void FreeMeshes(BuiltChunk chunk)
    {
        foreach (var mesh in chunk.meshes) Kill(mesh);
        chunk.meshes.Clear();
    }

    // Materials this map made for itself (lens, key, glass, fallbacks): freed with it.
    readonly List<Material> ownedMaterials = new List<Material>();

    Material Own(Material material)
    {
        if (material != null) ownedMaterials.Add(material);
        return material;
    }

    void OnDestroy() => Release();

    /// <summary>
    /// Free the meshes and materials this map made. Runs on destroy in Play
    /// mode; edit-mode tools (the Level Designer preview) call it before
    /// DestroyImmediate, since edit mode never calls OnDestroy here.
    /// </summary>
    public void Release()
    {
        foreach (var chunk in built.Values) FreeMeshes(chunk);
        foreach (var material in ownedMaterials) Kill(material);
        ownedMaterials.Clear();
    }

    void CreateCache()
    {
        Cache = new FrontRoomsMapCache(settings, moduleLibrary);
        foreach (var p in placedModules) Cache.Place(p.chunk, p.module, p.x, p.y);
    }

    /// <summary>
    /// Stamp a room module into chunk <paramref name="chunk"/> with its
    /// south-west cell at chunk-local (x, y), whenever that chunk is built.
    /// Call before the map starts (Awake, BuildForCapture). See RoomModuleStamp.
    /// </summary>
    public void PlaceModule(RoomModuleData module, GridCoord chunk, int x, int y)
    {
        placedModules.Add((module, chunk, x, y));
        Cache?.Place(chunk, module, x, y);
    }

    /// <summary>The Level Designer sets this: module props get a FrontRoomsModulePropTag (module, prop index, room origin).</summary>
    public bool TagModuleProps { get; set; }

    /// <summary>Start the walker or capture eye here (map space) instead of the middle of chunk (0, 0).</summary>
    public void OverrideSpawn(Vector3 mapPosition, float yaw = 0f)
    {
        spawnOverride = mapPosition;
        spawnYaw = yaw;
    }

    static void Kill(UnityEngine.Object target)
    {
        if (target == null) return;
        if (Application.isPlaying) Destroy(target); else DestroyImmediate(target);
    }

    /// <summary>
    /// The test scene shares the game's ambient bounce and haze
    /// (FrontRoomsLook), so the maze looks the same there as in the game.
    /// </summary>
    void ApplyRenderSettings()
    {
        FrontRoomsLook.ApplyAmbient();
        RenderSettings.skybox = null;
    }

    Vector3 SpawnPoint()
    {
        if (spawnOverride.HasValue) return spawnOverride.Value;
        var mid = MapGrid.ChunkCells / 2;
        return new Vector3((mid + .5f) * MapGrid.CellSize, .05f, (mid + .5f) * MapGrid.CellSize);
    }

    /// <summary>Where the player starts: the middle of chunk (0, 0), in world space.</summary>
    public Vector3 SpawnWorldPosition => transform.TransformPoint(SpawnPoint());

    /// <summary>Map cell under a point given in map space (this object's local space).</summary>
    public static GridCoord CellAt(Vector3 mapPosition) =>
        new GridCoord(Mathf.FloorToInt(mapPosition.x / MapGrid.CellSize), Mathf.FloorToInt(mapPosition.z / MapGrid.CellSize));

    /// <summary>Map cell under a world-space point.</summary>
    public GridCoord CellOf(Vector3 worldPosition) => CellAt(transform.InverseTransformPoint(worldPosition));

    GridCoord ChunkOf(Vector3 worldPosition) => MapGrid.ChunkOf(CellOf(worldPosition));

    /// <summary>Centre of a cell on the floor, in world space.</summary>
    public Vector3 CellCenter(GridCoord cell) =>
        transform.TransformPoint(new Vector3((cell.x + .5f) * MapGrid.CellSize, 0f, (cell.y + .5f) * MapGrid.CellSize));

    /// <summary>The cell's chunk is built. Reserved cells never are: the stream rooms, or a stairwell, stand there (IsChunkBuiltAt reads the chunk alone).</summary>
    public bool IsBuilt(GridCoord cell) => !IsReserved(cell) && built.ContainsKey(MapGrid.ChunkOf(cell));

    /// <summary>
    /// The zone a cell belongs to. The start area reads as the zone its door opens onto (Standard Level 0, like
    /// the stream rooms), not the generated zone hidden under it; X reads as N's, the zone its door opens from
    /// (stepping into the stairwell is no new zone).
    /// </summary>
    public ZoneInfo ZoneOf(GridCoord cell) => Cache.ZoneOf(InStartArea(cell) ? startDoorCell : InExitArea(cell) ? exitFront : cell);

    /// <summary>
    /// True for the map's own architecture: chunk walls, floors and ceilings,
    /// doors and glass, and the set pieces it registers (RegisterArchitecture:
    /// the station's riser block). False for furniture and anything else the kits add.
    /// </summary>
    public bool IsArchitecture(Collider c) => c != null && (shellColliders.Contains(c) || doorByCollider.ContainsKey(c) || windowByCollider.ContainsKey(c) || setPieceColliders.Contains(c));

    /// <summary>True when two side-by-side cells share a window whose glass is broken: an opening to climb through.</summary>
    public bool IsBrokenWindow(GridCoord a, GridCoord b) =>
        Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1 && Cache.Edge(a, b) == EdgeKind.Window && brokenWindows.Contains(EdgeId(a, b));

    /// <summary>True for the leaf of a door that is open, opening or broken.</summary>
    public bool IsOpenDoorLeaf(Collider c) => c != null && doorByCollider.TryGetValue(c, out var door) && door.open;

    /// <summary>
    /// The v2 §7 cost of crossing a side-by-side edge. This stays separate
    /// from PassageBetween so an opening in motion can measure as Ajar (+4)
    /// before it is wide enough to pass (+0).
    /// </summary>
    public float OpeningCostBetween(GridCoord a, GridCoord b)
    {
        if (Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) != 1) return float.PositiveInfinity;
        if (IsReserved(a) || IsReserved(b)) return float.PositiveInfinity;
        switch (Cache.Edge(a, b))
        {
            case EdgeKind.Open:
            case EdgeKind.Arch:
                return 0f;
            case EdgeKind.Door:
                return DoorOpeningCost(EdgeId(a, b));
            case EdgeKind.Window:
                return brokenWindows.Contains(EdgeId(a, b)) ? 0f : FrontRoomsMapPathField.ShutOpening;
            default:
                return float.PositiveInfinity;
        }
    }

    // A door's crossing cost as it stands: 0 broken or open past DoorPassableDegrees, Ajar while it is open short of
    // that, Shut otherwise; a door whose chunk is not built reads its record. OpeningCostBetween's and ExitOpeningCost's.
    float DoorOpeningCost(long id)
    {
        if (brokenDoors.Contains(id)) return 0f;
        if (doorByEdge.TryGetValue(id, out var door))
        {
            if (door.broken || door.angle >= DoorPassableDegrees) return 0f;
            return door.open || ajarDoors.Contains(id) ? FrontRoomsMapPathField.AjarOpening : FrontRoomsMapPathField.ShutOpening;
        }
        return openDoors.Contains(id) || ajarDoors.Contains(id) ? FrontRoomsMapPathField.AjarOpening : FrontRoomsMapPathField.ShutOpening;
    }

    public bool HasKeyFor(GridCoord zone) => keysHeld.Contains(zone);

    /// <summary>
    /// A held key is announced: KeyTaken, once per zone. The instant pickup calls it at contact; a key
    /// presenter calls it when the shown key reaches the held pose (KeyPickup.Arrive), or sooner when its
    /// presentation is cut short. A zone the player does not hold, or one already announced: nothing.
    /// </summary>
    public void KeyArrived(GridCoord zone)
    {
        if (keysHeld.Contains(zone) && keysAnnounced.Add(zone)) KeyTaken?.Invoke(zone);
    }

    /// <summary>
    /// The look+E key query (KeyPickupNeedsE): the trigger of a key on <paramref name="ray"/> within
    /// <paramref name="reach"/>, the nearest one, when nothing solid is nearer; null otherwise. Other
    /// triggers are skipped, and so is <paramref name="self"/> (the asking body's own colliders). The game's
    /// aim and the walker use it beside their solid ray (QueryTriggerInteraction.Ignore).
    /// </summary>
    public Collider KeyOnRay(Ray ray, float reach, Transform self)
    {
        if (keyByCollider.Count == 0) return null;
        var count = Physics.RaycastNonAlloc(ray, keyHits, reach, ~0, QueryTriggerInteraction.Collide);
        Collider key = null;
        float keyDistance = float.MaxValue, solid = float.MaxValue;
        for (var i = 0; i < count; i++)
        {
            var c = keyHits[i].collider;
            if (c == null || (self != null && c.transform.IsChildOf(self))) continue;
            if (!c.isTrigger) solid = Mathf.Min(solid, keyHits[i].distance);
            else if (keyByCollider.ContainsKey(c) && keyHits[i].distance < keyDistance)
            {
                key = c;
                keyDistance = keyHits[i].distance;
            }
        }
        return key != null && keyDistance < solid ? key : null;
    }

    void Update()
    {
        if (!begun || player == null) return;
        frameWork.Clear();
        workWatch.Restart();
        Stream(ChunkOf(StreamCenter), chunksPerFrame);
        // While the game streams round a focus (the player still in the
        // title's stream room, watching), a heavy chunk build and a room's
        // furniture never share a frame. A26: the station's props, which the start door waits for, come first.
        if (StreamFocus == null || workWatch.Elapsed.TotalMilliseconds < FocusFrameBudgetMs)
            if (!StepStation()) DressNext();
        TickFixtures(Time.deltaTime);
        TickDoors(Time.deltaTime);
        CollectKeys();
        if (frameWork.Length > 0) frameWork.Append(" = ").Append(workWatch.Elapsed.TotalMilliseconds.ToString("0.0")).Append(" ms");
        previousWork = lastWork;
        previousWorkFrame = lastWorkFrame;
        lastWork = frameWork.ToString();
        lastWorkFrame = Time.frameCount;
    }

    // What streaming did in the last Update, for frame-time reports (the autopilot's spikes).
    readonly System.Text.StringBuilder frameWork = new System.Text.StringBuilder();
    readonly System.Diagnostics.Stopwatch workWatch = new System.Diagnostics.Stopwatch(), stepWatch = new System.Diagnostics.Stopwatch();
    string lastWork = "", previousWork = "";
    int lastWorkFrame = -1, previousWorkFrame = -1;

    /// <summary>The chunks built and rooms furnished in the Update of the given frame, with their times; empty when it did neither or is too old.</summary>
    public string WorkInFrame(int frame) => frame == lastWorkFrame ? lastWork : frame == previousWorkFrame ? previousWork : "";

    void NoteWork(string what)
    {
        if (frameWork.Length > 0) frameWork.Append(", ");
        frameWork.Append(what).Append(' ').Append(stepWatch.Elapsed.TotalMilliseconds.ToString("0.0")).Append(" ms");
    }

    void Stream(GridCoord center, int budget)
    {
        scratch.Clear();
        foreach (var coord in built.Keys)
            if (Mathf.Abs(coord.x - center.x) > buildRadius || Mathf.Abs(coord.y - center.y) > buildRadius) scratch.Add(coord);
        foreach (var coord in scratch) Drop(coord);
        failedChunks.RemoveWhere(c => Mathf.Abs(c.x - center.x) > buildRadius || Mathf.Abs(c.y - center.y) > buildRadius);
        // Nearest ring first, so the chunk the player walks into is never the one still waiting.
        for (var ring = 0; ring <= buildRadius && budget > 0; ring++)
        for (var dy = -ring; dy <= ring && budget > 0; dy++)
        for (var dx = -ring; dx <= ring && budget > 0; dx++)
        {
            if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != ring) continue;
            var coord = new GridCoord(center.x + dx, center.y + dy);
            if (built.ContainsKey(coord) || failedChunks.Contains(coord)) continue;
            // Decision 2: a chunk the player left long enough ago comes back
            // rearranged. It is always at least one chunk (24 m) away when it
            // is rebuilt, so the change is never seen. C22 (D5): X's chunk
            // never shifts: X stays a cell that passed the planner's rules (no
            // room, key or column there, and its chunk whole without it) for
            // as long as the floor lasts. A26: nor do the carve's pins (the
            // station's chunk and X's), which keep the revision it was planned on.
            if (droppedAt.TryGetValue(coord, out var left) && Time.time - left >= shiftAfterSeconds && !IsExitChunk(coord) && !Cache.IsPinned(coord))
            {
                Cache.Shift(coord);
                ShiftedChunks++;
            }
            droppedAt.Remove(coord);
            stepWatch.Restart();
            Build(coord);
            NoteWork("chunk " + coord);
            budget--;
        }
    }

    void Drop(GridCoord coord)
    {
        if (!built.TryGetValue(coord, out var chunk)) return;
        Unregister(chunk);
        built.Remove(coord);
        droppedAt[coord] = Time.time;
        TouchPassageRevision();
        UpdateStationVisibility();
    }

    /// <summary>Forget a chunk's doors, windows and shell colliders, and free its meshes and objects.</summary>
    void Unregister(BuiltChunk chunk)
    {
        foreach (var f in chunk.fixtures)
            if (fixtureByCell.TryGetValue(f.cell, out var current) && current == f) fixtureByCell.Remove(f.cell);
        foreach (var door in chunk.doors)
        {
            movingDoors.Remove(door);
            if (doorByEdge.TryGetValue(door.edge, out var current) && current == door) doorByEdge.Remove(door.edge);
            if (door.leaf != null) doorByCollider.Remove(door.leaf);
        }
        if (chunk.root != null)
            foreach (var c in chunk.root.GetComponentsInChildren<Collider>(true))
            {
                windowByCollider.Remove(c);
                shellColliders.Remove(c);
                keyByCollider.Remove(c);
            }
        // The glass kit cleans up after the map's own bookkeeping, while the objects still exist.
        foreach (var window in chunk.windows) RaiseWindowReleased(window);
        FreeMeshes(chunk);
        if (chunk.root != null) Kill(chunk.root);
    }

    /// <summary>Tools and tests: make the build of the chunks it returns true for throw, to check the guard round Build.</summary>
    public Func<GridCoord, bool> FailBuildForTools { get; set; }

    ThemeMaterials Theme(ZoneTheme theme) => theme == ZoneTheme.Office ? office : level0;

    /// <summary>
    /// Build one chunk. A build that throws is undone (nothing half built
    /// stays registered or in the scene), logged, and the chunk is left out
    /// until it leaves the build radius, instead of being tried again every frame.
    /// </summary>
    void Build(GridCoord coord)
    {
        var chunk = new BuiltChunk();
        try
        {
            BuildInto(coord, chunk);
        }
        catch (Exception e)
        {
            Unregister(chunk);
            built.Remove(coord);
            failedChunks.Add(coord);
            Debug.LogError("[FrontRoomsMap] Chunk " + coord + " failed to build and is left out until it leaves range: " + e);
            return;
        }
        // Announced once the chunk is registered, outside the guard round the build.
        foreach (var window in chunk.windows) RaiseWindowBuilt(window);
        // C23 round 3: the EXIT door, with its leaf rig, crossbar, frame and sign all standing (BuildEdge).
        RaiseExitDoorBuilt(chunk);
        // A26: the station's shell for the rendering layers, and the set piece shown with c or c'.
        RaiseStationShellBuilt(coord, chunk);
        UpdateStationVisibility();
        TouchPassageRevision();
    }

    void BuildInto(GridCoord coord, BuiltChunk chunk)
    {
        const int n = MapGrid.ChunkCells;
        var cs = MapGrid.CellSize;
        var data = Cache.Get(coord);
        var origin = new Vector3(coord.x * MapGrid.ChunkSize, 0f, coord.y * MapGrid.ChunkSize);
        chunk.root = new GameObject("Chunk " + coord + " · revision " + data.revision);
        chunk.root.transform.SetParent(transform, false);
        chunk.root.transform.localPosition = origin;
        if (FailBuildForTools != null && FailBuildForTools(coord)) throw new InvalidOperationException("tools: forced build failure");

        // One set of meshes per 6 m block and ceiling height, so each renderer
        // can carry its own _CeilingHeight for the surface shader's grime band.
        const int blocks = n / BlockCells;
        const int heights = 3;
        var builders = new Dictionary<Material, MeshBuilder>[blocks * blocks * heights];
        for (var b = 0; b < builders.Length; b++) builders[b] = new Dictionary<Material, MeshBuilder>();
        MeshBuilder Get(int blockIndex, Material material)
        {
            if (!builders[blockIndex].TryGetValue(material, out var builder)) builders[blockIndex][material] = builder = new MeshBuilder();
            return builder;
        }
        var collision = new MeshBuilder();
        wallFaces.Clear();
        int BlockOf(int i, int j, float ceiling) => (i / BlockCells + j / BlockCells * blocks) * heights + HeightClass(ceiling);

        void Solid(int blockIndex, Material material, Vector3 center, Vector3 size, float repeat)
        {
            Get(blockIndex, material).Box(center, size, origin, repeat);
            collision.Box(center, size, origin, 1f);
        }

        for (var j = 0; j < n; j++)
        for (var i = 0; i < n; i++)
        {
            var index = MapGrid.LocalIndex(i, j);
            var cell = data.Cell(i, j);
            var zone = Cache.ZoneOf(cell);
            // A26: a station cell takes the station's skin (MaterialsOf), its zone stays Level 0.
            var theme = MaterialsOf(cell, zone);
            var height = MapGrid.CeilingHeight(data.height[index]);
            var b = BlockOf(i, j, height);
            var cellCenter = new Vector3((i + .5f) * cs, 0f, (j + .5f) * cs);
            // The title's stream rooms stand in the start area, and the EXIT's stairwell in X: no map there.
            var reserved = IsReserved(cell);
            if (!reserved)
            {
                Solid(b, theme.floor, cellCenter + Vector3.down * (ModuleUnits.FloorSlab * .5f), new Vector3(cs, ModuleUnits.FloorSlab, cs), CarpetRepeat);
                Solid(b, theme.ceiling, cellCenter + Vector3.up * (height + ModuleUnits.CeilingSlab * .5f), new Vector3(cs, ModuleUnits.CeilingSlab, cs), CeilingRepeat);
            }

            // East and north edges of every cell. Chunk borders on the east and
            // north belong to this chunk; west and south ones to the neighbour.
            var east = new GridCoord(cell.x + 1, cell.y);
            var north = new GridCoord(cell.x, cell.y + 1);
            var eastZone = Cache.ZoneOf(east);
            var northZone = Cache.ZoneOf(north);
            var eastHeight = Mathf.Max(height, MapGrid.CeilingHeight(eastZone.height));
            var northHeight = Mathf.Max(height, MapGrid.CeilingHeight(northZone.height));
            Material eastA = theme.wall, eastB = MaterialsOf(east, eastZone).wall, northA = theme.wall, northB = MaterialsOf(north, northZone).wall;
            var eastKind = ReservedEdge(data.east[index], cell, east, false, ref eastHeight, ref eastA, ref eastB);
            var northKind = ReservedEdge(data.north[index], cell, north, true, ref northHeight, ref northA, ref northB);
            // A wall's ends reach half a thickness past its corner, except into the start area.
            // The start area's side walls also stop on the door line, where the stream room's end wall closes the corner.
            // (X is one cell: no wall of its own runs inside it, and its north wall closes its corners.)
            // C22: a stairwell's start area (floors 2+) is closed by its own end wall, which stops between its side
            // walls: they end at their north corners like any wall (CornerReach), so no notch is left there.
            var startSide = InStartArea(cell) != InStartArea(east) && !StartAreaIsStairwell;
            // N1/B0.2: a height-border wall face is assigned to its own block so the
            // grime band uses that side's ceiling height. -1 keeps the edge's block.
            // An edge of a reserved cell keeps one block: its walls take the outside cell's height.
            var sideBlocks = FrontRoomsTransitionKit.B0 && !IsReserved(cell);
            var eastSide = sideBlocks && !IsReserved(east);
            var northSide = sideBlocks && !IsReserved(north);
            BuildEdge(chunk, eastKind, cell, east, new Vector3((i + 1) * cs, 0f, j * cs), Vector3.forward,
                eastHeight, eastA, eastB, BlockOf(i, j, eastHeight), Get, Solid, origin,
                !BothInStartArea(new GridCoord(cell.x, cell.y - 1), new GridCoord(cell.x + 1, cell.y - 1)),
                !BothInStartArea(new GridCoord(cell.x, cell.y + 1), new GridCoord(cell.x + 1, cell.y + 1)) && !(startSide && cell.y + 1 == startArea.yMax),
                eastSide ? BlockOf(i, j, height) : -1, eastSide ? BlockOf(i, j, MapGrid.CeilingHeight(eastZone.height)) : -1,
                data, wallFaces);
            BuildEdge(chunk, northKind, cell, north, new Vector3(i * cs, 0f, (j + 1) * cs), Vector3.right,
                northHeight, northA, northB, BlockOf(i, j, northHeight), Get, Solid, origin,
                !BothInStartArea(new GridCoord(cell.x - 1, cell.y), new GridCoord(cell.x - 1, cell.y + 1)),
                !BothInStartArea(new GridCoord(cell.x + 1, cell.y), new GridCoord(cell.x + 1, cell.y + 1)),
                northSide ? BlockOf(i, j, height) : -1, northSide ? BlockOf(i, j, MapGrid.CeilingHeight(northZone.height)) : -1,
                data, wallFaces);

            if (data.pillar[i + j * (n + 1)] && !TouchesReserved(cell.x, cell.y, cell.x, cell.y))
            {
                // No column, cove or bulkhead reaches into the start area or X.
                var style = data.pillarStyle[i + j * (n + 1)];
                if (TouchesReserved(cell.x, cell.y, cell.x + 2, cell.y)) style &= unchecked((byte)~MapChunk.ColumnBeamEast);
                if (TouchesReserved(cell.x, cell.y, cell.x, cell.y + 2)) style &= unchecked((byte)~MapChunk.ColumnBeamNorth);
                BuildColumn(style, new Vector3(i * cs, 0f, j * cs), height, zone.theme, theme, b, Get, Solid, origin);
            }

            if (!reserved) BuildFixture(chunk, cell, cellCenter, height, theme, data.lamp[index], data.tier);
        }

        for (var b = 0; b < builders.Length; b++)
        {
            if (builders[b].Count == 0) continue;
            var ceiling = HeightOfClass(b % heights);
            var go = new GameObject("Block " + b / heights + " · " + ceiling.ToString("0.0") + " m");
            go.transform.SetParent(chunk.root.transform, false);
            foreach (var pair in builders[b])
            {
                var mesh = AddRenderer(go, pair.Key, pair.Value, chunk.root.transform.position.y + ceiling);
                if (mesh != null) chunk.meshes.Add(mesh);
            }
        }
        var col = new GameObject("Collision");
        col.transform.SetParent(chunk.root.transform, false);
        var shell = col.AddComponent<MeshCollider>();
        shell.sharedMesh = collision.ToMesh("Chunk collision " + coord);
        chunk.meshes.Add(shell.sharedMesh);
        shellColliders.Add(shell);

        SpawnKeyHost(chunk, data);   // D1.6 (contract M3): the key's host on every build, held key or not
        // M3: a floor-key floor has no zone keys (the data keeps them: the carve hash, L5 and the verification are unchanged).
        // Q1 B: nor has a floor below a station floor (ZoneKeysOff).
        if (data.hasKey && !floorKey && !zoneKeysOff && !keysHeld.Contains(data.ownZone.id))
        {
            // A key spot in a room the start area cuts (not furnished, maybe under the stream rooms): back to the site cell.
            var atSpot = data.keySpot && !KeyRoomInStartArea(data);
            if (!IsReserved(atSpot ? data.keyCell : data.keySiteCell)) SpawnKey(chunk, data, atSpot);
        }
        // M3: the floor key at the station's KeySpot, in the chunk holding that cell (c or c', both pinned), until taken: a
        // drop or a rebuild brings it back once, never twice.
        if (floorKey && !keysHeld.Contains(FloorKeyZone) && StationAnchor(ModuleMarkerKind.KeySpot, out var floorKeyAt, out var floorKeyTurn, out var floorKeyCell)
            && MapGrid.ChunkOf(floorKeyCell) == coord)
            SpawnFloorKey(chunk, floorKeyAt, floorKeyTurn);
        RegisterRelayEntries(chunk, data);
        AddZoneGrades(chunk, data);
        FrontRoomsWallFixtures.Install(chunk.root.transform, wallFaces, Cache.Generator.Seed, data, FixtureKeepOut(coord), chunk.meshes);
        Furnish(chunk, data);
        built[coord] = chunk;
    }

    static readonly int CeilingHeightId = Shader.PropertyToID("_CeilingHeight");

    static int HeightClass(float ceiling) => ceiling < 2.65f ? 0 : ceiling < 4f ? 1 : 2;
    static float HeightOfClass(int c) => c == 0 ? ModuleUnits.LowCeiling : c == 1 ? ModuleUnits.StandardCeiling : ModuleUnits.TallCeiling;

    /// <summary>
    /// A shell renderer. <paramref name="ceilingWorldY"/> is the world height
    /// of its ceiling plane: the surface shader grimes the 1.6 m under it.
    /// </summary>
    Mesh AddRenderer(GameObject parent, Material material, MeshBuilder builder, float ceilingWorldY)
    {
        if (builder.vertices.Count == 0 || material == null) return null;
        var go = new GameObject(material.name);
        go.transform.SetParent(parent.transform, false);
        var mesh = builder.ToMesh(parent.name + " " + material.name);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = material;
        r.shadowCastingMode = ShadowCastingMode.On;
        if (material.HasProperty(CeilingHeightId))
        {
            var props = new MaterialPropertyBlock();
            props.SetFloat(CeilingHeightId, ceilingWorldY);
            r.SetPropertyBlock(props);
        }
        return mesh;
    }

    delegate MeshBuilder BuilderFn(int blockIndex, Material material);
    delegate void SolidFn(int blockIndex, Material material, Vector3 center, Vector3 size, float repeat);

    /// <summary>
    /// An edge as built round the start area: nothing inside it, nothing on
    /// its north side (the stream room's end wall and door stand there), and
    /// a plain wall in the outside cell's paper and height where it meets the
    /// map on its west, east and south sides. Other edges pass through.
    /// </summary>
    EdgeKind StartAreaEdge(EdgeKind kind, GridCoord a, GridCoord b, bool northward, ref float height, ref Material wallA, ref Material wallB)
    {
        var inA = InStartArea(a);
        var inB = InStartArea(b);
        if (!inA && !inB) return kind;
        if (inA && inB) return EdgeKind.Open;
        if (northward && inA) return EdgeKind.Open;
        var outside = Cache.ZoneOf(inA ? b : a);
        height = MapGrid.CeilingHeight(outside.height);
        wallA = wallB = MaterialsOf(inA ? b : a, outside).wall;
        return EdgeKind.Wall;
    }

    /// <summary>
    /// An edge as built round X (C22): the EXIT door on the edge from N, and a plain wall on X's west, east and
    /// north sides, all in the outside cell's paper and height, as round the start area (the stairwell's inner
    /// shell stands inside them, inset, so nothing of X's own is built here). Other edges pass through.
    /// </summary>
    EdgeKind ExitAreaEdge(EdgeKind kind, GridCoord a, GridCoord b, ref float height, ref Material wallA, ref Material wallB)
    {
        var inA = InExitArea(a);
        if (!inA && !InExitArea(b)) return kind;
        var outside = Cache.ZoneOf(inA ? b : a);
        height = MapGrid.CeilingHeight(outside.height);
        wallA = wallB = MaterialsOf(inA ? b : a, outside).wall;
        return ExitKind(a, b, kind);
    }

    // The exit rule on an edge's kind: N to X is the EXIT door, X's other edges are walls, every other edge is as it was.
    EdgeKind ExitKind(GridCoord a, GridCoord b, EdgeKind kind)
    {
        if (!InExitArea(a) && !InExitArea(b)) return kind;
        return a == exitFront || b == exitFront ? EdgeKind.Door : EdgeKind.Wall;
    }

    // The start-area rule, then (for an edge that does not touch the start area) the exit rule. a is the west or
    // south cell; northward is true for its north edge. X never touches the start area or its ring (the planner).
    EdgeKind ReservedEdge(EdgeKind kind, GridCoord a, GridCoord b, bool northward, ref float height, ref Material wallA, ref Material wallB)
    {
        if (InStartArea(a) || InStartArea(b)) return StartAreaEdge(kind, a, b, northward, ref height, ref wallA, ref wallB);
        return ExitAreaEdge(kind, a, b, ref height, ref wallA, ref wallB);
    }

    /// <summary>
    /// The edge between two side-by-side cells as this map builds it (C22): the generated edge, with the start
    /// area's rule (nothing inside it; Open on its north side, where the stream room's end wall and door stand,
    /// or on later floors the stairwell's north door; a wall where it meets the map) and the exit rule (the EXIT
    /// door from N into X; walls on X's west, east and north sides). The build, the corner posts, CrossingPoint
    /// and the lamps' border rule read it; KeepClear reads the exit rule alone. For the stairwell and the
    /// autopilot. Not what the Relay walks: PassageBetween is Wall for every edge of a reserved cell.
    /// </summary>
    public EdgeKind EffectiveEdge(GridCoord a, GridCoord b)
    {
        if (b.x < a.x || b.y < a.y) { var t = a; a = b; b = t; }
        var height = 0f;
        Material wallA = null, wallB = null;
        return ReservedEdge(Cache.Edge(a, b), a, b, b.y > a.y, ref height, ref wallA, ref wallB);
    }

    /// <summary>
    /// The one rect FrontRoomsWallFixtures keeps its column plates and floor boxes out of (the visual chat's
    /// signature takes one RectInt): the start area, as before; in X's chunk, X as well. When the start area also
    /// reaches X's chunk the two are joined into their bounding rect, which can only leave out a few plates or
    /// floor boxes between them in that one chunk (a list parameter would be the visual chat's change to make).
    /// A26: on a station floor, in every chunk, the bounding rect of the strip (while it stands), the station, the
    /// corridor and X (plan 4.4), so no plate or box lands in the room, on its walk or round the EXIT.
    /// </summary>
    RectInt FixtureKeepOut(GridCoord coord)
    {
        if (hasStation)
        {
            var s = station.station;
            int sx0 = s.x, sy0 = s.y, sx1 = s.x + s.w, sy1 = station.exit.y + 1;
            if (hasStartArea)
            {
                sx0 = Mathf.Min(sx0, startArea.xMin);
                sy0 = Mathf.Min(sy0, startArea.yMin);
                sx1 = Mathf.Max(sx1, startArea.xMax);
                sy1 = Mathf.Max(sy1, startArea.yMax);
            }
            return new RectInt(sx0, sy0, sx1 - sx0, sy1 - sy0);
        }
        var area = hasStartArea ? startArea : default;
        if (!IsExitChunk(coord)) return area;
        var x = new RectInt(exitCell.x, exitCell.y, 1, 1);
        var o = MapGrid.ChunkOrigin(coord);
        // The planner's 2 x 2 blocks start one cell west and south of the chunk.
        if (!hasStartArea || !startArea.Overlaps(new RectInt(o.x - 1, o.y - 1, MapGrid.ChunkCells + 1, MapGrid.ChunkCells + 1))) return x;
        int xMin = Mathf.Min(x.xMin, startArea.xMin), yMin = Mathf.Min(x.yMin, startArea.yMin);
        int xMax = Mathf.Max(x.xMax, startArea.xMax), yMax = Mathf.Max(x.yMax, startArea.yMax);
        return new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
    }

    bool BothInStartArea(GridCoord a, GridCoord b) => InStartArea(a) && InStartArea(b);

    // N1/B0.1: describe the four wall pieces around a lattice corner and ask
    // the pure transition rule how each skin owns the 0.16 m post.
    readonly FrontRoomsTransitionKit.PostPiece[] postPieces = new FrontRoomsTransitionKit.PostPiece[4];

    void CornerReach(GridCoord k, int role, out int reachA, out int reachB)
    {
        GridCoord sw = new GridCoord(k.x - 1, k.y - 1), se = new GridCoord(k.x, k.y - 1), nw = new GridCoord(k.x - 1, k.y), ne = k;
        postPieces[FrontRoomsTransitionKit.South] = PostPieceAt(sw, se, false);
        postPieces[FrontRoomsTransitionKit.North] = PostPieceAt(nw, ne, false);
        postPieces[FrontRoomsTransitionKit.West] = PostPieceAt(sw, nw, true);
        postPieces[FrontRoomsTransitionKit.East] = PostPieceAt(se, ne, true);
        FrontRoomsTransitionKit.CornerReach(postPieces, role, out reachA, out reachB);
    }

    FrontRoomsTransitionKit.PostPiece PostPieceAt(GridCoord a, GridCoord b, bool northward)
    {
        var za = Cache.ZoneOf(a);
        var zb = Cache.ZoneOf(b);
        var ha = MapGrid.CeilingHeight(za.height);
        var hb = MapGrid.CeilingHeight(zb.height);
        var h = Mathf.Max(ha, hb);
        Material wa = MaterialsOf(a, za).wall, wb = MaterialsOf(b, zb).wall;
        var kind = ReservedEdge(Cache.Edge(a, b), a, b, northward, ref h, ref wa, ref wb);
        if (kind == EdgeKind.Open) return default;
        // A reserved cell's edge is built at the outside cell's height on both faces (StartAreaEdge, ExitAreaEdge).
        var reservedEdge = IsReserved(a) || IsReserved(b);
        var ca = HeightClass(reservedEdge ? h : ha);
        var cb = HeightClass(reservedEdge ? h : hb);
        return new FrontRoomsTransitionKit.PostPiece
        {
            present = true,
            finishA = ((long)FrontRoomsObjectId.Of(wa) << 4) | (long)ca,
            finishB = ((long)FrontRoomsObjectId.Of(wb) << 4) | (long)cb,
            officeA = wa == office.wall,
            officeB = wb == office.wall,
        };
    }

    /// <summary>
    /// One 3 m edge from <paramref name="start"/> along <paramref name="along"/>.
    /// Where the two sides are different themes the wall is split in two
    /// halves, each faced in its own room's paper. Doorway width and position
    /// come from the edge's hash, so no two doorways line up.
    /// </summary>
    void BuildEdge(BuiltChunk chunk, EdgeKind kind, GridCoord a, GridCoord b, Vector3 start, Vector3 along, float height,
        Material wallA, Material wallB, int blockIndex, BuilderFn get, SolidFn solid, Vector3 origin,
        bool mayExtendStart = true, bool mayExtendEnd = true, int blockA = -1, int blockB = -1,
        MapChunk faceData = null, List<FrontRoomsWallFixtures.WallFace> faces = null)
    {
        if (kind == EdgeKind.Open) return;
        var length = MapGrid.CellSize;
        // Positive "across" points from cell a into cell b.
        var across = new Vector3(along.z, 0f, along.x);
        var sideA = blockA >= 0 ? blockA : blockIndex;
        var sideB = blockB >= 0 ? blockB : blockIndex;
        var startA = WallThickness * .5f;
        var startB = startA;
        var endA = startA;
        var endB = startA;
        if (FrontRoomsTransitionKit.B0)
        {
            var alongX = along.x > .5f;
            CornerReach(alongX ? new GridCoord(a.x, a.y + 1) : new GridCoord(a.x + 1, a.y), alongX ? FrontRoomsTransitionKit.East : FrontRoomsTransitionKit.North, out var sa, out var sb);
            CornerReach(new GridCoord(a.x + 1, a.y + 1), alongX ? FrontRoomsTransitionKit.West : FrontRoomsTransitionKit.South, out var ea, out var eb);
            startA *= sa; startB *= sb; endA *= ea; endB *= eb;
        }
        void Piece(float from, float to, float bottom, float top, bool extendStart, bool extendEnd)
        {
            if (from > 0f || !mayExtendStart) extendStart = false;
            if (to < length || !mayExtendEnd) extendEnd = false;
            if (top - bottom < .05f) return;
            var fA = from - (extendStart ? startA : 0f);
            var tA = to + (extendEnd ? endA : 0f);
            var fB = from - (extendStart ? startB : 0f);
            var tB = to + (extendEnd ? endB : 0f);
            if (wallA == wallB && sideA == sideB && fA == fB && tA == tB)
            {
                if (tA - fA < .05f) return;
                var center = start + along * ((fA + tA) * .5f) + Vector3.up * ((bottom + top) * .5f);
                var size = along * (tA - fA) + across * WallThickness + Vector3.up * (top - bottom);
                solid(blockIndex, wallA, center, Abs(size), WallpaperRepeat);
                return;
            }
            void Skin(int block, Material material, float f, float t, float side)
            {
                if (t - f < .05f) return;
                var center = start + along * ((f + t) * .5f) + Vector3.up * ((bottom + top) * .5f) + across * (WallThickness * .25f * side);
                solid(block, material, center, Abs(along * (t - f) + across * (WallThickness * .5f) + Vector3.up * (top - bottom)), WallpaperRepeat);
            }
            Skin(sideA, wallA, fA, tA, -1f);
            Skin(sideB, wallB, fB, tB, 1f);
        }

        if (kind == EdgeKind.Wall)
        {
            Piece(0f, length, 0f, height, true, true);
            AddWallFaces(faces, faceData, kind, a, b, start, along, 0f, 0f, 0f, 0f);
            return;
        }

        float width, c, openingTop, sill = 0f;
        if (kind == EdgeKind.Arch)
        {
            ArchOpening(a, along.x > .5f, out width, out c);
            openingTop = Mathf.Min(ModuleUnits.ArchTop, height - ModuleUnits.ArchHeaderMin);
        }
        else if (kind == EdgeKind.Door)
        {
            width = DoorWidth;
            c = length * .5f;
            openingTop = DoorHeight;
        }
        else
        {
            width = WindowWidth;
            c = length * .5f;
            openingTop = WindowTop;
            sill = WindowSill;
        }
        Piece(0f, c - width * .5f, 0f, height, true, false);
        Piece(c + width * .5f, length, 0f, height, false, true);
        Piece(c - width * .5f, c + width * .5f, openingTop, height, false, false);
        if (sill > 0f) Piece(c - width * .5f, c + width * .5f, 0f, sill, false, false);
        AddWallFaces(faces, faceData, kind, a, b, start, along, c - width * .5f, c + width * .5f, sill, openingTop);

        if (kind == EdgeKind.Arch) return;

        // Frame: two jambs and a head in the trim colour. The EXIT door's frame is a renderer of its own that casts
        // no shadow (C23 round 2; 10_directions.md 1.2, the strip fix); every other frame joins its block's trim mesh.
        var exitFrame = kind == EdgeKind.Door && IsExitEdge(a, b);
        var trims = exitFrame ? new MeshBuilder() : get(blockIndex, trim);
        var frame = ModuleUnits.TrimFace;
        var jambDepth = WallThickness + ModuleUnits.TrimProud * 2f;
        foreach (var s in new[] { -1f, 1f })
        {
            var jc = start + along * (c + s * (width * .5f + frame * .5f)) + Vector3.up * ((sill + openingTop) * .5f);
            trims.Box(jc, Abs(along * frame + across * jambDepth + Vector3.up * (openingTop - sill)), origin, 1f);
        }
        trims.Box(start + along * c + Vector3.up * (openingTop + frame * .5f), Abs(along * (width + frame * 2f) + across * jambDepth + Vector3.up * frame), origin, 1f);
        if (exitFrame) chunk.exitFrame = BuildExitFrame(chunk, a, b, trims, height);

        var edge = EdgeId(a, b);
        var openingCenter = chunk.root.transform.TransformPoint(start + along * c);
        if (kind == EdgeKind.Door)
        {
            // The hinge sits on one jamb and faces along the wall, so the leaf
            // spans the opening on its local Z and swings about the hinge's Y.
            var hinge = new GameObject("Door hinge " + a + "-" + b).transform;
            hinge.SetParent(chunk.root.transform, false);
            hinge.localPosition = start + along * (c - width * .5f);
            hinge.localRotation = Quaternion.LookRotation(along, Vector3.up);
            var leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leaf.name = "Door leaf";
            leaf.transform.SetParent(hinge, false);
            leaf.transform.localPosition = new Vector3(0f, (DoorHeight - ModuleUnits.DoorLeafGap) * .5f, width * .5f);
            leaf.transform.localScale = new Vector3(ModuleUnits.DoorLeafThickness, DoorHeight - ModuleUnits.DoorLeafGap, width - ModuleUnits.DoorLeafGap);
            leaf.GetComponent<Renderer>().sharedMaterial = doorLeaf;
            var door = new Door { hinge = hinge, closed = hinge.localRotation, leaf = leaf.GetComponent<Collider>(), a = a, b = b, edge = edge, position = openingCenter,
                swing = doorSides.TryGetValue(edge, out var side) ? side : FixedSwing(a, b), leafHome = leaf.transform.localPosition };
            // Rebuilt as it was left, on the side it went to: open at rest, or broken at rest and hanging crooked.
            if (brokenDoors.Contains(edge))
            {
                door.broken = door.open = true;
                door.angle = FrontRoomsShotTimings.DoorBreak.BounceRestDeg;
                door.crooked = FrontRoomsShotTimings.DoorBreak.CrookedDeg;
            }
            else if (openDoors.Contains(edge))
            {
                door.open = true;
                door.angle = ModuleUnits.DoorSwingDegrees;
            }
            // Relay v2: left Ajar by the Relay's OPEN (not open: E opens it fully).
            else if (ajarDoors.Contains(edge)) door.angle = AjarDegrees;
            if (door.angle > 0f) PoseDoor(door);
            chunk.doors.Add(door);
            doorByCollider[door.leaf] = door;
            doorByEdge[edge] = door;
            // C23 step 1: the EXIT door carries the floor's LIT-locked EXIT sign over its header, on N's side;
            // round 2: and the crossbar on its leaf's push face (N's side); round 3: on its leaf rig, which every
            // EXIT door gets, and the chunk notes the door for ExitDoorBuilt (Build raises it once registered).
            if (IsExitEdge(a, b))
            {
                MountLeafRig(door);
                chunk.exitSign = MountExitSign(chunk, a, b, start, along, c, height);
                chunk.exitCrossbar = MountExitCrossbar(door, width);
                chunk.exitDoor = door;
            }
            // D1.6 (contract M1): every other map door gets the visual kit's double-acting door on its cube leaf (render
            // only; null keeps the cube). The EXIT door is the stairwell production's (ExitDoorBuilt), so not here.
            else
            {
                try { door.rig = FrontRoomsInteractableKit.DressDoor(this, door, chunk.root.transform); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }
        else
        {
            // The window root: unscaled, at the opening's centre on the wall line at floor level, +Z into cell b.
            var root = new GameObject("Window " + a + "-" + b).transform;
            root.SetParent(chunk.root.transform, false);
            root.localPosition = start + along * c;
            var intoB = new Vector3(b.x - a.x, 0f, b.y - a.y);
            root.localRotation = Quaternion.LookRotation(intoB, Vector3.up);
            var window = new Window { root = root, a = a, b = b, edge = edge, position = openingCenter };
            var record = GlassBreakOf(window);
            window.stage = record.stage;
            if (!brokenWindows.Contains(edge))
            {
                // The gameplay box, as before (world transform unchanged), now under the root.
                var pane = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pane.name = "Window pane " + a + "-" + b;
                pane.transform.SetParent(root, false);
                pane.transform.localPosition = Vector3.up * ((sill + openingTop) * .5f);
                pane.transform.localScale = new Vector3(width, openingTop - sill, ModuleUnits.GlassThickness);
                pane.GetComponent<Renderer>().sharedMaterial = glass;
                pane.AddComponent<FrontRoomsMetalGlassTarget>();
                window.pane = pane;
                // A struck pane resumes from the progress it reached (never below its stage: cracks do not heal).
                window.hold = Mathf.Max(StageFloor(window.stage), record.hold);
                windowByCollider[pane.GetComponent<Collider>()] = window;
            }
            // The visual chat's glass draws the slab and every stage on the root; the box keeps only its collider.
            var breakable = GlassBreakableType();
            if (breakable != null)
            {
                root.gameObject.AddComponent(breakable);
                if (window.pane != null) window.pane.GetComponent<Renderer>().enabled = false;
            }
            chunk.windows.Add(window);
        }
    }

    // The glass kit hears of windows one handler at a time: a handler that throws is logged and never
    // breaks the map's own building or bookkeeping (that code is not the map's).
    void RaiseWindowBuilt(Window window)
    {
        window.announced = true;
        var record = GlassBreakOf(window);
        try { FrontRoomsInteractableKit.DressWindow(this, window, record); }
        catch (Exception e) { Debug.LogException(e); }
        if (WindowBuilt == null) return;
        foreach (Action<Window, GlassBreakRecord> handler in WindowBuilt.GetInvocationList())
        {
            try { handler(window, record); }
            catch (Exception e) { Debug.LogException(e); }
        }
    }

    void RaiseWindowReleased(Window window)
    {
        // A pane torn down mid-hold keeps what it reached (a release normally stored it already).
        KeepHold(window);
        if (!window.announced || WindowReleased == null) return;
        window.announced = false;
        foreach (Action<Window> handler in WindowReleased.GetInvocationList())
        {
            try { handler(window); }
            catch (Exception e) { Debug.LogException(e); }
        }
    }

    static Type GlassBreakableType()
    {
        if (glassBreakableResolved) return glassBreakable;
        glassBreakableResolved = true;
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType("FrontRoomsGlassBreakable");
            if (type != null && typeof(Component).IsAssignableFrom(type)) { glassBreakable = type; break; }
        }
        return glassBreakable;
    }

    /// <summary>
    /// Outlets: list both faces of an edge BuildEdge just built, for
    /// FrontRoomsWallFixtures. Faces round the start area are left out. Side 0
    /// faces into a (this chunk's cell), side 1 into b; b is in the next chunk
    /// on the east and north border ("foreign": no room, seed-pure inputs only).
    /// Door and Window faces are listed with their kind; the planner puts nothing on them.
    /// </summary>
    void AddWallFaces(List<FrontRoomsWallFixtures.WallFace> faces, MapChunk data, EdgeKind kind, GridCoord a, GridCoord b,
        Vector3 start, Vector3 along, float openFrom, float openTo, float openBottom, float openTop)
    {
        if (faces == null || data == null || InStartArea(a) || InStartArea(b)) return;
        var across = new Vector3(along.z, 0f, along.x);
        var edge = EdgeId(a, b);
        var o = data.Origin;
        for (var side = 0; side < 2; side++)
        {
            var cell = side == 0 ? a : b;
            // C22: nothing on X's side of its walls and the EXIT door (the stairwell's inner shell stands there); the maze side keeps its own.
            if (InExitArea(cell)) continue;
            var zone = Cache.ZoneOf(cell);
            var normal = side == 0 ? -across : across;
            int li = cell.x - o.x, lj = cell.y - o.y;
            var own = li >= 0 && li < MapGrid.ChunkCells && lj >= 0 && lj < MapGrid.ChunkCells;
            var room = -1;
            if (own) for (var r = data.rooms.Length - 1; r >= 0; r--) if (data.rooms[r].Contains(li, lj)) { room = r; break; }
            faces.Add(new FrontRoomsWallFixtures.WallFace
            {
                start = start + normal * ModuleUnits.WallHalf, direction = along, length = MapGrid.CellSize, normal = normal,
                kind = kind, openFrom = openFrom, openTo = openTo, openBottom = openBottom, openTop = openTop,
                edgeId = edge, side = (byte)side, cell = cell, foreign = !own, room = room,
                zone = zone.id, theme = zone.theme, height = zone.height, ceiling = MapGrid.CeilingHeight(zone.height),
                zoneBorder = Cache.ZoneOf(side == 0 ? b : a).id != zone.id, baseTop = 0f,
            });
        }
    }

    static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

    /// <summary>
    /// A structural column on a 6 m grid corner (see MapSettings): faced in
    /// the room's wall material, on a cove base. Office columns carry a
    /// bulkhead to the next column on the grid. The bulkhead is
    /// above every head, so it has no collision.
    /// </summary>
    void BuildColumn(byte style, Vector3 at, float height, ZoneTheme zoneTheme, ThemeMaterials theme, int blockIndex, BuilderFn get, SolidFn solid, Vector3 origin)
    {
        var w = (style & MapChunk.ColumnLarge) != 0 ? ModuleUnits.ColumnLarge : ModuleUnits.ColumnSmall;
        solid(blockIndex, theme.wall, at + Vector3.up * (height * .5f), new Vector3(w, height, w), WallpaperRepeat);
        var cove = w + ModuleUnits.CoveProud * 2f;
        get(blockIndex, trim).Box(at + Vector3.up * (ModuleUnits.CoveHeight * .5f), new Vector3(cove, ModuleUnits.CoveHeight, cove), origin, 1f);
        if (zoneTheme != ZoneTheme.Office) return;
        var span = ModuleUnits.ColumnGrid - w;
        var y = height - ModuleUnits.BulkheadDepth * .5f;
        if ((style & MapChunk.ColumnBeamEast) != 0)
            get(blockIndex, theme.wall).Box(at + new Vector3(ModuleUnits.ColumnGrid * .5f, y, 0f), new Vector3(span, ModuleUnits.BulkheadDepth, w), origin, WallpaperRepeat);
        if ((style & MapChunk.ColumnBeamNorth) != 0)
            get(blockIndex, theme.wall).Box(at + new Vector3(0f, y, ModuleUnits.ColumnGrid * .5f), new Vector3(w, ModuleUnits.BulkheadDepth, span), origin, WallpaperRepeat);
    }

    /// <summary>
    /// Width and position (metres from the edge's start) of a doorless
    /// doorway, from the edge's hash. <paramref name="a"/> is the west or
    /// south cell; <paramref name="alongX"/> is true for its north edge.
    /// A26: the start group's E1 and E2 keep the spans the carve authors
    /// (Cache.ArchOverride), read before the hash; every other arch is hashed.
    /// </summary>
    void ArchOpening(GridCoord a, bool alongX, out float width, out float center)
    {
        if (Cache.ArchOverride(a, alongX ? new GridCoord(a.x, a.y + 1) : new GridCoord(a.x + 1, a.y), out width, out center)) return;
        var edgeHash = MapHash.Hash(Cache.Generator.Seed, a.x * 2 + (alongX ? 1 : 0), a.y, 97);
        width = ModuleUnits.ArchMinWidth + ModuleUnits.ArchWidthSpread * MapHash.Unit(edgeHash);
        var margin = ModuleUnits.ArchCornerMargin + width * .5f;
        center = Mathf.Lerp(margin, MapGrid.CellSize - margin, MapHash.Unit(MapHash.Hash((int)edgeHash, 3, 7, 11)));
    }

    // The door swing hash's salt (MapHash uses 11-73, this map 97, 211, 223, 307).
    const int DoorSwingSalt = 89;

    /// <summary>
    /// The starting side of a door on the edge between two neighbouring cells,
    /// as Door.swing (+1 or -1), until its leaf first moves: a hash of the
    /// seed and the edge, with no revision, so every build, rebuild and revisit
    /// shift agrees. Since A12 it only says which way a shut leaf rests; every
    /// open picks the far side of its opener (SwingAwayFrom).
    /// </summary>
    float FixedSwing(GridCoord a, GridCoord b)
    {
        // C22 (D6): the EXIT door has its own side (ExitSwing), whatever the hash says.
        if (IsExitEdge(a, b)) return ExitSwing;
        var lo = a;
        var hi = b;
        if (hi.x < lo.x || hi.y < lo.y) { lo = b; hi = a; }
        var eastEdge = hi.x != lo.x;
        var intoHi = MapHash.Unit(MapHash.Hash(Cache.Generator.Seed, lo.x * 2 + (eastEdge ? 0 : 1), lo.y, DoorSwingSalt)) < .5f;
        // Swing +1 sends an east edge's leaf west (into lo) and a north edge's leaf north (into hi).
        return eastEdge ? (intoHi ? -1f : 1f) : (intoHi ? 1f : -1f);
    }

    /// <summary>
    /// Where to cross from one cell into its neighbour, in world space: the
    /// middle of the doorway, door or window, or of the shared edge when it is
    /// open. Doorways sit off-centre, so walking centre to centre can hit wall.
    /// </summary>
    public Vector3 CrossingPoint(GridCoord a, GridCoord b)
    {
        var lo = a;
        var hi = b;
        if (hi.x < lo.x || hi.y < lo.y) { lo = b; hi = a; }
        var cs = MapGrid.CellSize;
        var eastward = hi.x > lo.x;
        var start = eastward ? new Vector3((lo.x + 1) * cs, 0f, lo.y * cs) : new Vector3(lo.x * cs, 0f, (lo.y + 1) * cs);
        var along = eastward ? Vector3.forward : Vector3.right;
        var center = cs * .5f;
        // As built (EffectiveEdge): the EXIT door is centred whatever was generated on its edge.
        if (EffectiveEdge(lo, hi) == EdgeKind.Arch) ArchOpening(lo, !eastward, out _, out center);
        return transform.TransformPoint(start + along * center);
    }

    static long EdgeId(GridCoord a, GridCoord b)
    {
        if (b.x < a.x || b.y < a.y) { var t = a; a = b; b = t; }
        var dir = b.x > a.x ? 1L : 0L;
        return ((long)a.x << 33) ^ ((long)(a.y & 0x7fffffff) << 1) ^ dir;
    }

    // ---------- Passage, for the Relay and the autopilot ----------

    /// <summary>What lies between two side-by-side cells right now.</summary>
    public Passage PassageBetween(GridCoord a, GridCoord b)
    {
        // Only side-by-side cells share an edge; anything else is no way through.
        if (Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) != 1) return Passage.Wall;
        // The start area is walled off from the map (its door is the stream's), and so is X (C22, D3: the
        // EXIT door is the player's way into the stairwell, never the Relay's or the autopilot's route).
        if (IsReserved(a) || IsReserved(b)) return Passage.Wall;
        switch (Cache.Edge(a, b))
        {
            case EdgeKind.Open:
            case EdgeKind.Arch:
                return Passage.Open;
            case EdgeKind.Door:
                var id = EdgeId(a, b);
                // A built door by its leaf, broken or not: swung far enough for a body to pass. A broken
                // leaf still falling, or resting on a body short of 70°, is no way through yet.
                // Relay v2: a door the Relay left Ajar, short of 70 deg (only OpenAjar makes one).
                if (doorByEdge.TryGetValue(id, out var door))
                    return door.angle >= DoorPassableDegrees ? Passage.Open : ajarDoors.Contains(id) ? Passage.Ajar : Passage.ClosedDoor;
                // Not built: as it was left.
                return brokenDoors.Contains(id) || openDoors.Contains(id) ? Passage.Open : ajarDoors.Contains(id) ? Passage.Ajar : Passage.ClosedDoor;
            case EdgeKind.Window:
                return brokenWindows.Contains(EdgeId(a, b)) ? Passage.Open : Passage.Glass;
            default:
                return Passage.Wall;
        }
    }

    /// <summary>The built door between two cells, or null.</summary>
    public Door DoorBetween(GridCoord a, GridCoord b) => doorByEdge.TryGetValue(EdgeId(a, b), out var door) ? door : null;

    /// <summary>
    /// The Relay forces a door from where it stands: it stays open for good.
    /// A shut leaf is burst away from the Relay, into the room it breaks into
    /// (A12); a leaf caught partly open keeps its side. Thrown past its rest in
    /// DoorBreak.ThrowSeconds, it bounces back to rest at
    /// DoorBreak.BounceRestDeg, hanging crooked, on that side for the run.
    /// </summary>
    public void BreakDoor(Door door, Vector3 from)
    {
        if (door == null) return;
        // The chunk was rebuilt mid-break: the edge's new Door is the one to break.
        if (doorByEdge.TryGetValue(door.edge, out var current)) door = current;
        breakingDoors.Remove(door.edge);
        if (door.broken) return;
        SwingAwayFrom(door, from);
        var fromSwingSide = OnSwingSide(door, from);
        door.broken = true;
        door.open = true;
        brokenDoors.Add(door.edge);
        keptBlows.Remove(door.edge);
        ajarDoors.Remove(door.edge);
        doorSides[door.edge] = door.swing;
        door.wait = 0f;
        door.motion = DoorMotion.Throw;
        door.from = door.angle;
        door.clock = 0f;
        if (!movingDoors.Contains(door)) movingDoors.Add(door);
        TouchPassageRevision();
        DoorBroken?.Invoke(door.position);
        DoorBrokenFrom?.Invoke(door, from, fromSwingSide);
    }

    /// <summary>M2 (door break, Red 2026-10-30 Q2): the blows this door has taken and kept while it still stands (0 once it breaks). A half-broken door stays weak: the Relay strikes on from them.</summary>
    public int BlowsKept(Door door) => door != null && keptBlows.TryGetValue(door.edge, out var blows) ? blows : 0;

    /// <summary>M2: the Relay leaves this door having struck this many blows in all (FrontRoomsMapHunter.StopBreaking).</summary>
    public void KeepBlows(Door door, int blows)
    {
        if (door == null) return;
        if (blows > 0 && !door.broken) keptBlows[door.edge] = blows;
        else keptBlows.Remove(door.edge);
    }

    /// <summary>
    /// Tools only (the autopilot, tests): open a door without aiming at it,
    /// standing in cell <paramref name="a"/>. Respects the key rule, but never
    /// starts a key turn: with the key, the door unlocks (DoorUnlocked at once)
    /// and opens in this call, with no shot. True when this call started it
    /// opening; false when it is locked (the EXIT door too while ExitLocked: no
    /// key opens it yet), already open or opening, held by a
    /// key turn, being broken, sealed (the EXIT door after SealExit), or not built.
    /// </summary>
    public bool TryOpenDoor(GridCoord a, GridCoord b)
    {
        var door = DoorBetween(a, b);
        if (door == null || door.open || door.broken || IsBeingBroken(door) || IsSealedExit(door)) return false;
        if (door.motion == DoorMotion.Lever || IsHeldForKey(door)) return false;
        var from = player != null && CellOf(player.position) == a ? player.position : CellCenter(a);
        if (NeedsKey(door))
        {
            var zone = KeyFor(door, from);
            if (!zone.HasValue) return false;
            UnlockAtOnce(door, zone.Value, from);
            return true;
        }
        OpenDoor(door, from, DoorOpenSource.Tools);
        return true;
    }

    /// <summary>
    /// Relay v2's OPEN (RELAY_PURSUIT_REDESIGN 5, 10; the V3b hunter calls it, nothing does before
    /// FRONTROOMS_RELAY_V2): the Relay standing at <paramref name="relayFeet"/> frees the latch and pushes the
    /// leaf out of the frame to AjarDegrees over RelayOpenSeconds, on the far side from it (A12). The door is
    /// then Ajar: PassageBetween answers Passage.Ajar and OpeningCostBetween AjarOpening at once, it is not
    /// open (Describe and Kind offer OPEN; the player's E opens it fully, an ease), and it stays Ajar by edge
    /// through drops, rebuilds and revisit shifts until that E, a break, ResetDoorForTools or NextFloor.
    /// Raises DoorOpenBegan(door, DoorOpenSource.Relay, false) and nothing else: no DoorHandleTurned (the
    /// game jolts the player's camera on it) and no DoorMoved (the game's door noise), because nothing the
    /// Relay does is a noise to the Relay or the director; the sound layer plays the latch and the push from
    /// DoorOpenBegan's source and the hinge. A body in the way stops the leaf on it, and it then goes on at
    /// the open's own speed. False, with nothing changed, for a door that is not built, open, Ajar already,
    /// broken, being broken, not latched in its frame, in its lever, held by a key turn, the EXIT door, or
    /// locked to the player (NeedsKey).
    /// </summary>
    public bool OpenAjar(Door door, Vector3 relayFeet)
    {
        if (door == null) return false;
        if (doorByEdge.TryGetValue(door.edge, out var current)) door = current;
        else return false;
        if (door.open || door.broken || ajarDoors.Contains(door.edge) || IsBeingBroken(door) || !IsLatched(door)
            || door.motion == DoorMotion.Lever || IsHeldForKey(door) || IsExitDoor(door) || NeedsKey(door)) return false;
        SwingAwayFrom(door, relayFeet);
        ajarDoors.Add(door.edge);
        doorSides[door.edge] = door.swing;
        door.pull = false;
        door.wait = 0f;
        StartSwing(door, AjarDegrees, false);
        door.seconds = RelayOpenSeconds;
        if (!movingDoors.Contains(door)) movingDoors.Add(door);
        TouchPassageRevision();
        DoorOpenBegan?.Invoke(door, DoorOpenSource.Relay, false);
        return true;
    }

    /// <summary>Relay v2: true while the door on this edge is Ajar (the Relay OPENed it and nobody has opened it fully since).</summary>
    public bool IsAjar(Door door) => door != null && ajarDoors.Contains(door.edge);

    /// <summary>Relay v2's push through an Ajar door (RELAY 10: "the Relay pushes through in 0.4 s", source Relay; step V3b), in seconds.</summary>
    public const float RelayPushSeconds = .4f;

    /// <summary>
    /// Relay v2 (V3b): a door the Relay may OPEN at all: built, not the EXIT door and not locked to the player (NeedsKey).
    /// The rest of OpenAjar's refusals pass with time (a leaf still moving, a key turn, a break). Nothing calls it before
    /// FRONTROOMS_RELAY_V2.
    /// </summary>
    public bool RelayMayOpen(Door door)
    {
        if (door == null) return false;
        if (doorByEdge.TryGetValue(door.edge, out var current)) door = current;
        else return false;
        return !IsExitDoor(door) && !NeedsKey(door);
    }

    /// <summary>
    /// Relay v2's push (RELAY 10; the V3b hunter calls it, nothing does before FRONTROOMS_RELAY_V2): the Relay at
    /// <paramref name="relayFeet"/> pushes an Ajar door's leaf on to its open rest (ModuleUnits.DoorSwingDegrees) over
    /// RelayPushSeconds, on the side it already hangs to (a leaf back in its frame turns away from the Relay first, A12),
    /// so its body passes once the leaf is past DoorPassableDegrees. The door stays Ajar by edge (not open: no openDoors,
    /// Describe still offers OPEN) and SettleAjar brings the leaf back to AjarDegrees once the Relay has gone through, so
    /// a door the Relay came through rests Ajar (RELAY 10: "the rest state of a door the Relay OPENed"). No event: nothing
    /// the Relay does is a noise to the Relay or the director; the sound layer hears the hinge (and the hunter raises its
    /// own RelayDoorPushed). A body in the way stops the leaf on it. False, with nothing changed, for a door that is not
    /// built, not Ajar, open, broken, being broken, in its lever or held by a key turn.
    /// </summary>
    public bool PushAjar(Door door, Vector3 relayFeet)
    {
        if (door == null) return false;
        if (doorByEdge.TryGetValue(door.edge, out var current)) door = current;
        else return false;
        if (door.open || door.broken || !ajarDoors.Contains(door.edge) || IsBeingBroken(door) || door.motion == DoorMotion.Lever || IsHeldForKey(door)) return false;
        SwingAwayFrom(door, relayFeet);
        doorSides[door.edge] = door.swing;
        door.pull = false;
        door.wait = 0f;
        StartSwing(door, ModuleUnits.DoorSwingDegrees, false);
        door.seconds = RelayPushSeconds;
        if (!movingDoors.Contains(door)) movingDoors.Add(door);
        return true;
    }

    /// <summary>
    /// Relay v2 (V3b): after its push the leaf of a door still Ajar by edge swings back to AjarDegrees over
    /// RelayOpenSeconds, stopping on any body in its way. False, with nothing changed, when the door is not built, no
    /// longer Ajar (the player opened or shut it), broken, being broken, in its lever, held by a key turn, or already
    /// at AjarDegrees or less.
    /// </summary>
    public bool SettleAjar(Door door)
    {
        if (door == null) return false;
        if (doorByEdge.TryGetValue(door.edge, out var current)) door = current;
        else return false;
        if (door.open || door.broken || !ajarDoors.Contains(door.edge) || IsBeingBroken(door) || door.motion == DoorMotion.Lever || IsHeldForKey(door)) return false;
        if (door.angle <= AjarDegrees + LatchedDegrees) return false;
        door.wait = 0f;
        StartSwing(door, AjarDegrees, false);
        door.seconds = RelayOpenSeconds;
        if (!movingDoors.Contains(door)) movingDoors.Add(door);
        return true;
    }

    // ---------- Fixtures: each lamp keeps its own state ----------

    void BuildFixture(BuiltChunk chunk, GridCoord cell, Vector3 localCenter, float height, ThemeMaterials theme, ModuleLamp lamp, int tier)
    {
        // A module can take a lamp out altogether.
        if (lamp == ModuleLamp.Off) return;
        var seed = Cache.Generator.Seed;
        var fixture = new Fixture { rng = MapHash.Hash(seed, cell.x, cell.y, 211) | 1u, cell = cell };
        var root = new GameObject("Fixture " + cell);
        root.transform.SetParent(chunk.root.transform, false);
        // The lens fills whole 0.6 m ceiling tiles: X [1.2, 1.8], Z [1.2, 2.4] of the cell.
        root.transform.localPosition = localCenter + new Vector3(0f, height, ModuleUnits.TrofferOffsetZ);
        // A box with metre UVs, so the lens textures tile as authored (0.6 x 1.2 m); a scaled cube stretched them.
        var panel = new GameObject("Lens");
        panel.transform.SetParent(root.transform, false);
        panel.transform.localPosition = new Vector3(0f, -ModuleUnits.TrofferDrop, 0f);
        panel.AddComponent<MeshFilter>().sharedMesh = FrontRoomsFilmMesh.GetPlanarBox(new Vector3(ModuleUnits.TrofferShort, ModuleUnits.TrofferLens, ModuleUnits.TrofferLong));
        var pr = panel.AddComponent<MeshRenderer>();
        pr.sharedMaterial = theme.lens;
        pr.shadowCastingMode = ShadowCastingMode.Off;
        fixture.panel = pr;
        fixture.emission = theme.lens.HasProperty("_EmissionColor") ? theme.lens.GetColor("_EmissionColor") : Color.black;

        // A troffer only throws light downward: a wide spot at the lens, as
        // in the room stream, so the lit lens reads against the ceiling.
        var lightGo = new GameObject("Light");
        lightGo.transform.SetParent(root.transform, false);
        lightGo.transform.localPosition = new Vector3(0f, -ModuleUnits.LampDrop, 0f);
        lightGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Spot;
        light.spotAngle = 162f;
        light.innerSpotAngle = 96f;
        light.color = theme.lampColor;
        light.range = height > 4f ? 12f : 10f;
        light.shadows = LightShadows.None;
        light.shadowStrength = .92f;
        light.shadowNearPlane = .1f;
        fixture.baseIntensity = theme.lampIntensity * (height > 4f ? 1.6f : 1f);
        fixture.light = light;
        fixture.offset = light.transform.position - chunk.root.transform.position;
        fixture.startGroup = StartLampGroup(cell, light.range);
        fixture.exitGroup = ExitLampGroup(cell, light.range);
        // About one lamp in three may cast shadows, and only near the player.
        fixture.castsShadow = MapHash.Unit(MapHash.Hash(seed, cell.x, cell.y, 223)) < .34f;

        // Lamp temperament, rolled once per lamp from the seed and its cell:
        // 0 steady, 1 stutters now and then, 2 failing, 3 dead with rare blinks, 4 dim.
        var roll = Rand(ref fixture.rng);
        // The odds come from the tier the chunk was generated at (tier 1: 62 % steady, 20 stutter, 10 failing, 5 dead, 3 dim).
        fixture.mode = (tierRules ?? new FrontRoomsTierRules()).At(tier).LampMode(roll);
        if (lamp == ModuleLamp.Auto && FrontRoomsTransitionLightLead.On && FrontRoomsTransitionLightLead.Borders)
        {
            var rolled = fixture.mode;
            var self = Cache.ZoneOf(cell).theme;
            fixture.mode = FrontRoomsTransitionLightLead.BorderMode(rolled, self, BorderThemes(cell, borderKinds), borderKinds);
            FrontRoomsTransitionLightLead.Record(cell, self, rolled, fixture.mode);
        }
        // A module's lamp: Steady..Dim map onto modes 0..4.
        if (lamp != ModuleLamp.Auto) fixture.mode = (int)lamp - 1;
        // A mode set at run time wins over both (the rolls above are still drawn, so the rest stay put).
        if (lampModes.TryGetValue(cell, out var set)) fixture.mode = (int)set - 1;
        fixture.ambientHold = ambientHolds.Contains(cell);
        fixture.hold = lampHolds.TryGetValue(cell, out var lookHold) ? lookHold : null;
        fixture.phase = Rand(ref fixture.rng) * 50f;
        fixture.nextEvent = 2f + Rand(ref fixture.rng) * 14f;
        chunk.fixtures.Add(fixture);
        fixtureByCell[cell] = fixture;
    }

    readonly ZoneTheme[] borderThemes = new ZoneTheme[4];
    readonly EdgeKind[] borderKinds = new EdgeKind[4];

    ZoneTheme[] BorderThemes(GridCoord cell, EdgeKind[] kinds)
    {
        for (var k = 0; k < 4; k++)
        {
            var n = k == 0 ? new GridCoord(cell.x + 1, cell.y) : k == 1 ? new GridCoord(cell.x - 1, cell.y) : k == 2 ? new GridCoord(cell.x, cell.y + 1) : new GridCoord(cell.x, cell.y - 1);
            borderThemes[k] = Cache.ZoneOf(n).theme;
            // The start area reads as walls all round (its north side is the stream room's end wall); X's edges as built (C22).
            kinds[k] = InStartArea(n) || InStartArea(cell) ? EdgeKind.Wall : ExitKind(cell, n, EdgeNoGenerate(cell, n));
        }
        return borderThemes;
    }

    // The edge between two side-by-side cells, read without generating a chunk (codex audit MAP-3), so
    // LampModeOf stays pure and a build reads no neighbour chunk. An edge on a chunk border is a pure
    // function of the seed (FrontRoomsMapGenerator.BorderEdge; FrontRoomsMapValidator checks that chunks
    // agree with it across neighbours and shifts). An inner edge comes from its own chunk; if the cache has
    // not generated that chunk, it reads as Wall (no crossing). Only LampModeOf's prediction for a far cell
    // meets that case: a chunk being built is always generated, so a build reads what Cache.Edge would.
    EdgeKind EdgeNoGenerate(GridCoord a, GridCoord b)
    {
        if (b.x < a.x || b.y < a.y) { var t = a; a = b; b = t; }
        var east = b.x > a.x;
        var chunkA = MapGrid.ChunkOf(a);
        if (chunkA != MapGrid.ChunkOf(b)) return Cache.Generator.BorderEdge(a, east);
        if (!Cache.TryGetGenerated(chunkA, out var data)) return EdgeKind.Wall;
        var o = data.Origin;
        var index = MapGrid.LocalIndex(a.x - o.x, a.y - o.y);
        return east ? data.east[index] : data.north[index];
    }

    /// <summary>
    /// Map lamps near the start area, held dark by the game while the stream
    /// rooms can be seen: a lamp with no shadow lights straight through the
    /// rooms' walls, so one switching on as its chunk streams in would
    /// brighten the room the player is standing in. North: lamps past the
    /// door line (they come up as the door opens). South: lamps beside and
    /// behind the rooms (they come up once the player has left the rooms).
    /// 0 is dark, 1 normal.
    /// </summary>
    public float StartLampsNorth { get; set; } = 1f;
    public float StartLampsSouth { get; set; } = 1f;

    // How far a lamp's groups hold it: its start group's factor, times ExitLamps in the exit group (C22). A lamp in
    // no exit group reads exactly what it read before the exit group existed.
    float Held(Fixture f)
    {
        var held = f.startGroup == 1 ? StartLampsNorth : f.startGroup == 2 ? StartLampsSouth : 1f;
        return f.exitGroup ? held * ExitLamps : held;
    }

    byte StartLampGroup(GridCoord cell, float range)
    {
        if (!hasStartArea) return 0;
        var cs = MapGrid.CellSize;
        var cx = (cell.x + .5f) * cs;
        var cz = (cell.y + .5f) * cs;
        var dx = Mathf.Max(startArea.xMin * cs - cx, 0f, cx - startArea.xMax * cs);
        var dz = Mathf.Max(startArea.yMin * cs - cz, 0f, cz - startArea.yMax * cs);
        if (dx * dx + dz * dz >= range * range) return 0;
        return cell.y >= startDoorCell.y ? (byte)1 : (byte)2;
    }

    /// <summary>
    /// WebGL only (from the platform; tools may force it): per-frame Light and
    /// property-block calls go only to the lamps that can be lit, within
    /// lightRadius of the player. A lamp further away keeps its flicker clock,
    /// holds its Light off, and rewrites its lens emission only when the glow
    /// moves by more than <see cref="FarEmissionStep"/> (a stutter, a dropout,
    /// a blink), never for the steady 2–3% shimmer. A chunk whose footprint is
    /// beyond lightRadius skips the distance test altogether. The desktop path
    /// below is unchanged.
    /// </summary>
    public static bool TickFixturesNearOnly { get; set; } = Application.platform == RuntimePlatform.WebGLPlayer;

    const float FarEmissionStep = .05f;
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    // V1: the lens's share of its lamp's override dip (Fixture.dip); 1 when idle, so the lens is unchanged outside overrides.
    static readonly int LampDipId = Shader.PropertyToID("_FR_LampDip");

    void TickFixtures(float dt)
    {
        TickLampOverrides(dt);
        TickLampHolds(dt);
        if (player == null) return;
        if (TickFixturesNearOnly) { TickFixturesNear(dt); return; }
        var p = player.position;
        foreach (var chunk in built.Values)
        foreach (var f in chunk.fixtures)
        {
            f.clock += dt;
            f.level = LampTick(f);
            var lp = f.light.transform.position;
            var d = Vector2.Distance(new Vector2(lp.x, lp.z), new Vector2(p.x, p.z));
            var held = Held(f);
            var fade = Mathf.Clamp01((lightRadius - d) / 3f) * held;
            var on = fade > 0f && f.level > .01f;
            if (f.light.enabled != on) f.light.enabled = on;
            if (on) f.light.intensity = f.baseIntensity * f.level * fade;
            var shadows = f.castsShadow && on && d < shadowRadius ? LightShadows.Soft : LightShadows.None;
            if (f.light.shadows != shadows) f.light.shadows = shadows;
            f.panel.GetPropertyBlock(block);
            block.SetColor("_EmissionColor", f.emission * Mathf.Max(.04f, f.level * held));
            block.SetFloat(LampDipId, f.dip);
            f.panel.SetPropertyBlock(block);
        }
    }

    void TickFixturesNear(float dt)
    {
        var p = player.position;
        var r2 = lightRadius * lightRadius;
        foreach (var chunk in built.Values)
        {
            if (chunk.fixtures.Count == 0) continue;
            var root = chunk.root.transform.position;
            // Nearest point of the chunk's footprint to the player, in XZ.
            var cx = Mathf.Max(root.x - p.x, 0f, p.x - (root.x + MapGrid.ChunkSize));
            var cz = Mathf.Max(root.z - p.z, 0f, p.z - (root.z + MapGrid.ChunkSize));
            var chunkFar = cx * cx + cz * cz >= r2;
            foreach (var f in chunk.fixtures)
            {
                f.clock += dt;
                f.level = LampTick(f);
                var held = Held(f);
                var d = lightRadius;
                if (!chunkFar) d = Vector2.Distance(new Vector2(root.x + f.offset.x, root.z + f.offset.z), new Vector2(p.x, p.z));
                var fade = Mathf.Clamp01((lightRadius - d) / 3f) * held;
                var on = fade > 0f && f.level > .01f;
                if (f.lightOn != on) { f.light.enabled = on; f.lightOn = on; }
                var factor = Mathf.Max(.04f, f.level * held);
                if (on)
                {
                    // Lit: the same per-frame updates as the desktop path.
                    f.light.intensity = f.baseIntensity * f.level * fade;
                    var shadows = f.castsShadow && d < shadowRadius ? LightShadows.Soft : LightShadows.None;
                    if (f.shadowMode != shadows) { f.light.shadows = shadows; f.shadowMode = shadows; }
                    WriteEmission(f, factor);
                    continue;
                }
                // Off: only a real change of glow reaches the renderer.
                if (f.shadowMode != LightShadows.None) { f.light.shadows = LightShadows.None; f.shadowMode = LightShadows.None; }
                if (Mathf.Abs(factor - f.emissionWritten) > FarEmissionStep) WriteEmission(f, factor);
            }
        }
    }

    void WriteEmission(Fixture f, float factor)
    {
        f.panel.GetPropertyBlock(block);
        block.SetColor(EmissionColorId, f.emission * factor);
        block.SetFloat(LampDipId, f.dip);
        f.panel.SetPropertyBlock(block);
        f.emissionWritten = factor;
    }

    public void TickFixturesForTools(float dt) => TickFixtures(dt);

    /// <summary>
    /// Tools and tests in edit mode, where Update does not run and Time.time stands still: stream round a world
    /// point as Update does, with every chunk in range built and furnished now. Every chunk the map remembers
    /// dropping is first made <paramref name="ageDrops"/> seconds older, so a test can bring a dropped chunk
    /// (DropChunkForTools) back as a revisit after shiftAfterSeconds (C22: X's chunk never shifts).
    /// </summary>
    public void StreamForTools(Vector3 worldCenter, float ageDrops)
    {
        var dropped = new List<GridCoord>(droppedAt.Keys);
        foreach (var coord in dropped) droppedAt[coord] -= ageDrops;
        Stream(ChunkOf(worldCenter), int.MaxValue);
        while (DressNext(all: true)) { }
    }

    /// <summary>
    /// Tools: every lamp's Light and lens against the lamp formula, read back
    /// from the components (lit within lightRadius with level above 0.01 and
    /// its start and exit groups not held, at baseIntensity × level × fade, soft shadows
    /// within shadowRadius when it casts; otherwise off). A lit lens carries
    /// exactly the current glow and its lamp's override dip (_FR_LampDip, V1); an
    /// unlit one within <see cref="FarEmissionStep"/> of it. Returns the number of
    /// disagreements, with what they were.
    /// </summary>
    public int CheckLampStatesForTools(out int lit, out int total, List<string> problems)
    {
        lit = 0; total = 0;
        var mismatches = 0;
        var p = player.position;
        var read = new MaterialPropertyBlock();
        foreach (var chunk in built.Values)
        foreach (var f in chunk.fixtures)
        {
            total++;
            var lp = f.light.transform.position;
            var d = Vector2.Distance(new Vector2(lp.x, lp.z), new Vector2(p.x, p.z));
            var held = Held(f);
            var fade = Mathf.Clamp01((lightRadius - d) / 3f) * held;
            var on = fade > 0f && f.level > .01f;
            var factor = Mathf.Max(.04f, f.level * held);
            f.panel.GetPropertyBlock(read);
            var glow = read.GetColor(EmissionColorId);
            var expectedGlow = f.emission * factor;
            string problem = null;
            if (f.light.enabled != on) problem = "enabled " + f.light.enabled + " expected " + on;
            else if (on && Mathf.Abs(f.light.intensity - f.baseIntensity * f.level * fade) > 1e-4f) problem = "intensity " + f.light.intensity + " expected " + f.baseIntensity * f.level * fade;
            else if (on && f.light.shadows != (f.castsShadow && d < shadowRadius ? LightShadows.Soft : LightShadows.None)) problem = "shadows " + f.light.shadows;
            else if (!on && f.light.shadows != LightShadows.None) problem = "shadows " + f.light.shadows + " while off";
            else if (on && (glow - expectedGlow).maxColorComponent > 1e-4f) problem = "lit glow " + glow + " expected " + expectedGlow;
            else if (on && Mathf.Abs(read.GetFloat(LampDipId) - f.dip) > 1e-5f) problem = "lens dip " + read.GetFloat(LampDipId) + " expected " + f.dip;
            else if (!on && Mathf.Abs(glow.maxColorComponent - expectedGlow.maxColorComponent) > FarEmissionStep * Mathf.Max(f.emission.maxColorComponent, 1e-6f) + 1e-4f) problem = "far glow " + glow + " expected about " + expectedGlow;
            if (on) lit++;
            if (problem == null) continue;
            mismatches++;
            if (problems != null && problems.Count < 12) problems.Add(f.light.transform.parent.name + " d " + d.ToString("0.0") + " level " + f.level.ToString("0.00") + ": " + problem);
        }
        return mismatches;
    }

    // ---------- Ambient flicker and its limits (M2: RELAY_PURSUIT_REDESIGN.md §9.5; lamp check 10_lamp_look_check.md) ----------
    // The measured problem (2026-10-22): a Stutter lamp ran a PerlinNoise(t·23) strobe between 0.95 and 0.05,
    // 5-14 flashes a second per lamp, and lights a whole wall (Office: > 3 flashes/s over 37 % of a 1/4 field).
    // Every flicker mode now stays at or under 2 flashes a second by design, and a per-lamp limiter holds back
    // any change that would make a 4th flash within a second. Steady, Dim and Off, and every lamp between its
    // events, are bit-identical to before; each mode keeps its timers and its own RNG, so it stays deterministic.

    /// <summary>A flash (WCAG 2.3.1): two opposing changes of at least FlashStep of full level, the darker side under FlashDark.</summary>
    public const float FlashStep = .10f, FlashDark = .80f;
    /// <summary>The flash limiter: at most FlashMaxTransitions changes (3 flashes) in any FlashWindow seconds; the next one waits.</summary>
    public const float FlashWindow = 1f;
    public const int FlashMaxTransitions = 6;
    /// <summary>
    /// Stutter (mode 1): an episode of 2 dips (the old stutter's draw under 0.5 s) or 3, one per StutterSlot
    /// (the 0.5 s slot plus W1's 0.05 s guard, so the first and third dips' edges are never 1 s apart), each down
    /// to StutterFloor..StutterFloor + StutterFloorSpread of the lamp's level over a 60-80 ms ramp, held
    /// 0.05-0.15 s, back over 60-80 ms: at most 1.41 s, then at least 3.7 s steady (the old 5-20 s gap).
    /// </summary>
    public const float StutterSlot = .55f, StutterRampMin = .06f, StutterRampMax = .08f, StutterHoldMin = .05f, StutterHoldMax = .15f;
    public const float StutterFloor = .5f, StutterFloorSpread = .1f;
    /// <summary>Failing (mode 2): a dropout is held at least FailingHoldMin, and the next starts at least FailingGap after one ends.</summary>
    public const float FailingHoldMin = .34f, FailingGap = 1.1f;
    /// <summary>Dead (mode 3): one strike, at most one per DeadStrikeGap seconds.</summary>
    public const float DeadStrikeGap = 4f;
    /// <summary>Reduce flashing: an ambient dip is one sag to this share of the lamp's level, in and out over ReducedFlashingEnvelopeSeconds.</summary>
    public const float ReducedAmbientDepth = .7f;

    static float Level(Fixture f)
    {
        var t = f.clock + f.phase;
        // Reduce flashing (Settings): no on/off strobe. Clocks and the RNG advance exactly as before.
        var calm = FrontRoomsSettings.ReduceFlashing;
        switch (f.mode)
        {
            case 1:
                return Limit(f, Stutter(f, t, calm));
            case 2:
                return Limit(f, Failing(f, t, calm));
            case 3:
                return Limit(f, DeadBlink(f, calm));
            case 4:
                return .42f + .03f * Mathf.Sin(t * 90f);
            case 5:
                // Killed at run time (SetLampMode Off): a dark lens, the troffer still there.
                return 0f;
            default:
                return .98f + .02f * Mathf.Sin(t * 110f);
        }
    }

    // M1: a held Stutter or Failing lamp (SetLampHold). Its own mode still runs unseen (timers, RNG, events), so
    // it is where it would have been when the hold lets go. The lamp shows a crossfade from its source (the level
    // it showed when the hold began, going over to its mode's calm level: Stutter between episodes, Failing's wave
    // with no dropout) to the held look (Steady's or Dim's own formula), through the flash limiter like the mode.
    // Fully held, it is the look exactly.
    static float HeldLevel(Fixture f)
    {
        var t = f.clock + f.phase;
        var calm = FrontRoomsSettings.ReduceFlashing;
        var h = f.hold;
        float own;
        if (f.mode == 1)
        {
            Stutter(f, t, calm);
            own = .97f + .03f * Mathf.Sin(t * 120f);
        }
        else
        {
            Failing(f, t, calm);
            var wave = .5f + .5f * Mathf.Sin(t * 3.1f) * (.7f + .3f * Mathf.Sin(t * 1.27f));
            own = .25f + .45f * wave;
        }
        var look = h.look == ModuleLamp.Dim ? .42f + .03f * Mathf.Sin(t * 90f) : .98f + .02f * Mathf.Sin(t * 110f);
        if (h.weight >= 1f) return Limit(f, look);
        var source = h.source >= 1f ? own : Mathf.Lerp(h.from, own, h.source);
        return Limit(f, Mathf.Lerp(source, look, h.weight));
    }

    // M1: a lamp's own mode between its events (where a hold may hand it back without a step).
    static bool OwnCalm(Fixture f) => f.mode == 1 ? f.episodeDips == 0 : f.mode != 2 || (!f.dropping && f.gain == 1f);

    // Steady, then a stutter at its own random interval: the old timers and draws, now an episode of dips.
    static float Stutter(Fixture f, float t, bool calm)
    {
        if (f.clock >= f.nextEvent && f.eventEnd <= f.clock)
        {
            f.eventEnd = f.clock + .15f + Rand(ref f.rng) * .7f;
            f.nextEvent = f.eventEnd + 5f + Rand(ref f.rng) * 15f;
            // The episode's shape comes from the RNG's state here without drawing from it, so every later
            // event keeps its old time.
            f.episodeStart = f.clock;
            f.episodeDips = (byte)(f.eventEnd - f.clock < .5f ? 2 : 3);
            f.episodeSeed = f.rng;
        }
        if (f.episodeDips == 0) return .97f + .03f * Mathf.Sin(t * 120f);
        f.gain = StutterGain(f, f.clock - f.episodeStart, calm);
        return (.97f + .03f * Mathf.Sin(t * 120f)) * f.gain;
    }

    // The episode's gain u seconds in (1 between dips); ends the episode after its last dip (or sag).
    static float StutterGain(Fixture f, float u, bool calm)
    {
        var n = f.episodeDips;
        if (calm)
        {
            // One sag for the whole episode, held as long as the dips would have played.
            StutterDip(f, n - 1, out var down0, out var hold0, out var up0, out _);
            var until = (n - 1) * StutterSlot + down0 + hold0 + up0;
            if (u >= Mathf.Max(until, ReducedFlashingEnvelopeSeconds) + ReducedFlashingEnvelopeSeconds) { f.episodeDips = 0; return 1f; }
            return ReducedSag(u, until);
        }
        var k = Mathf.Min((int)(u / StutterSlot), n - 1);
        var x = u - k * StutterSlot;
        StutterDip(f, k, out var down, out var hold, out var up, out var depth);
        if (x >= down + hold + up)
        {
            if (k == n - 1) f.episodeDips = 0;
            return 1f;
        }
        if (x < down) return Mathf.Lerp(1f, depth, x / down);
        if (x < down + hold) return depth;
        return Mathf.Lerp(depth, 1f, (x - down - hold) / up);
    }

    // Dip k of the current episode, from the episode's seed: the ramps and the hold are the episode's (so every
    // dip's edges stay StutterSlot apart, and the first and third never come within 1 s), the depth is the dip's.
    static void StutterDip(Fixture f, int k, out float down, out float hold, out float up, out float depth)
    {
        var seed = (int)f.episodeSeed;
        down = Mathf.Lerp(StutterRampMin, StutterRampMax, MapHash.Unit(MapHash.Hash(seed, 0, 0, 239)));
        hold = Mathf.Lerp(StutterHoldMin, StutterHoldMax, MapHash.Unit(MapHash.Hash(seed, 0, 1, 239)));
        up = Mathf.Lerp(StutterRampMin, StutterRampMax, MapHash.Unit(MapHash.Hash(seed, 0, 2, 239)));
        depth = StutterFloor + StutterFloorSpread * MapHash.Unit(MapHash.Hash(seed, k, 3, 239));
    }

    // Failing ballast: never settles, drops out at its own rhythm (the old PerlinNoise above 0.72), each dropout
    // held at least FailingHoldMin and the next at least FailingGap after it ends.
    static float Failing(Fixture f, float t, bool calm)
    {
        var wave = .5f + .5f * Mathf.Sin(t * 3.1f) * (.7f + .3f * Mathf.Sin(t * 1.27f));
        var want = Mathf.PerlinNoise(t * 1.4f, f.phase) > .72f;
        if (f.dropping)
        {
            if (!want && f.clock - f.dropStart >= FailingHoldMin) { f.dropping = false; f.dropEnd = f.clock; }
        }
        else if (want && f.clock - f.dropEnd >= FailingGap) { f.dropping = true; f.dropStart = f.clock; }
        if (!calm) f.gain = f.dropping ? .05f : 1f;
        else if (f.dropping) f.gain = ReducedSag(f.clock - f.dropStart, float.PositiveInfinity);
        else if (float.IsNegativeInfinity(f.dropStart)) f.gain = 1f;
        else
        {
            // The sag comes back over its release after the dropout ends.
            var until = f.dropEnd - f.dropStart;
            var u = f.clock - f.dropStart;
            f.gain = u < Mathf.Max(until, ReducedFlashingEnvelopeSeconds) + ReducedFlashingEnvelopeSeconds ? ReducedSag(u, until) : 1f;
        }
        return (.25f + .45f * wave) * f.gain;
    }

    // Dead tube that blinks once in a long while: one strike, at most one per DeadStrikeGap, and none while a
    // warning holds the lamp's room (SetAmbientHold): a strike that starts held stays dark to its end.
    static float DeadBlink(Fixture f, bool calm)
    {
        if (f.clock >= f.nextEvent && f.eventEnd <= f.clock)
        {
            f.eventEnd = f.clock + .06f + Rand(ref f.rng) * .12f;
            f.nextEvent = f.eventEnd + 8f + Rand(ref f.rng) * 20f;
            f.strikeShown = !f.ambientHold && f.clock - f.lastStrike >= DeadStrikeGap;
            if (f.strikeShown) f.lastStrike = f.clock;
        }
        return f.clock < f.eventEnd && !calm && f.strikeShown && !f.ambientHold ? .8f : 0f;
    }

    // Reduce flashing's ambient sag, u seconds in: down to ReducedAmbientDepth over the envelope (smoothstep),
    // held to `until` (never before it is all the way down), back over the envelope. At most 0.9/s of gain.
    static float ReducedSag(float u, float until)
    {
        var r = ReducedFlashingEnvelopeSeconds;
        var down = Smooth01(u / r);
        var back = Smooth01((u - Mathf.Max(until, r)) / r);
        return 1f - (1f - ReducedAmbientDepth) * down * (1f - back);
    }

    static float Smooth01(float x)
    {
        x = Mathf.Clamp01(x);
        return x * x * (3f - 2f * x);
    }

    // The flash limiter over a flicker mode's level (§9.5, per lamp): a swing of FlashStep or more between
    // turning points whose darker side is under FlashDark is one change, counted the frame it gets there; a level
    // that would make the 7th change (a 4th flash) within FlashWindow is held back, and the lamp keeps its last
    // level until the oldest change leaves the window. Under the cap it gives the mode's level back untouched.
    static float Limit(Fixture f, float v)
    {
        var z = f.flash;
        if (z == null) f.flash = z = new FlashTrack();
        if (!z.begun)
        {
            z.begun = true;
            z.dir = 0;
            z.pivot = z.ext = z.lo = z.hi = z.last = v;
            z.counted = false;
            return v;
        }
        var dir = z.dir;
        float pivot = z.pivot, ext = z.ext, lo = z.lo, hi = z.hi;
        var counted = z.counted;
        if (dir == 0)
        {
            lo = Mathf.Min(lo, v);
            hi = Mathf.Max(hi, v);
            if (v - lo >= FlashStep) { dir = 1; pivot = lo; ext = v; counted = false; }
            else if (hi - v >= FlashStep) { dir = -1; pivot = hi; ext = v; counted = false; }
        }
        else if (dir > 0)
        {
            if (v > ext) ext = v;
            else if (ext - v >= FlashStep) { dir = -1; pivot = ext; ext = v; counted = false; }
        }
        else
        {
            if (v < ext) ext = v;
            else if (v - ext >= FlashStep) { dir = 1; pivot = ext; ext = v; counted = false; }
        }
        var change = dir != 0 && !counted
            && (dir > 0 ? ext - pivot >= FlashStep && pivot < FlashDark : pivot - ext >= FlashStep && ext < FlashDark);
        // A closed window, 1 ms wider, so two changes a frame-rounded second apart still count as one second.
        if (change && z.count == FlashMaxTransitions && z.times[z.head] >= f.clock - FlashWindow - .001f)
        {
            z.held++;
            return z.last;
        }
        z.dir = dir;
        z.pivot = pivot;
        z.ext = ext;
        z.lo = lo;
        z.hi = hi;
        z.counted = counted || change;
        if (change)
        {
            if (z.count < FlashMaxTransitions) z.times[(z.head + z.count++) % FlashMaxTransitions] = f.clock;
            else
            {
                z.times[z.head] = f.clock;
                z.head = (z.head + 1) % FlashMaxTransitions;
            }
        }
        z.last = v;
        return v;
    }

    static float Rand(ref uint state)
    {
        state ^= state << 13;
        state ^= state >> 17;
        state ^= state << 5;
        return (state & 0xffffff) / 16777216f;
    }

    // ---------- Furnishing (through the visual session's kits, when present) ----------

    static void ResolveDressers()
    {
        if (dressersResolved) return;
        dressersResolved = true;
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var kit = assembly.GetType("FrontRoomsOfficeKit");
            if (kit != null && officeDress == null && officeDressOld == null)
            {
                // Two overloads share the name: bind by parameter types. The
                // newer one takes the room's columns as obstacles.
                officeDress = kit.GetMethod("Dress", BindingFlags.Public | BindingFlags.Static, null,
                    new[] { typeof(Transform), typeof(Rect), typeof(float), typeof(int), typeof(Rect[]), typeof(Rect[]) }, null);
                if (officeDress == null)
                    officeDressOld = kit.GetMethod("Dress", BindingFlags.Public | BindingFlags.Static, null,
                        new[] { typeof(Transform), typeof(Rect), typeof(float), typeof(int), typeof(Rect[]) }, null);
            }
            if (kit != null && officeBegin == null)
                officeBegin = kit.GetMethod("BeginDress", BindingFlags.Public | BindingFlags.Static, null,
                    new[] { typeof(Transform), typeof(Rect), typeof(float), typeof(int), typeof(Rect[]), typeof(Rect[]) }, null);
            var pile = assembly.GetType("FrontRoomsFurniturePile");
            if (pile != null && pileBuild == null)
                pileBuild = pile.GetMethod("Build", BindingFlags.Public | BindingFlags.Static, null,
                    new[] { typeof(Transform), typeof(Vector3), typeof(float), typeof(float), typeof(int) }, null);
            if (pile != null && pileBegin == null)
                pileBegin = pile.GetMethod("BeginBuild", BindingFlags.Public | BindingFlags.Static, null,
                    new[] { typeof(Transform), typeof(Vector3), typeof(float), typeof(float), typeof(int) }, null);
        }
    }

    /// <summary>
    /// Queue the chunk's rooms for furnishing: Office rooms get an office
    /// layout, large halls sometimes a furniture pile. Only rooms that are one
    /// open rectangle qualify: no later room cuts into them, and every cell
    /// has the same height and theme.
    /// </summary>
    void Furnish(BuiltChunk chunk, MapChunk data)
    {
        ResolveDressers();
        if (officeDress == null && officeDressOld == null && pileBuild == null) return;
        for (var r = 0; r < data.rooms.Length; r++)
            // A room cut by the start area is not one open space (and its props would land in the stream rooms); nor is one holding X.
            if (data.RoomIntact(r) && Cache.Generator.Uniform(data, data.rooms[r]) && !RoomInReserved(data, data.rooms[r]))
                dressQueue.Enqueue(new DressJob { chunk = chunk, data = data, room = r });
    }

    /// <summary>
    /// Furnish: carry on with the room being furnished (one budget's worth),
    /// or start the next queued room whose chunk is still standing. Returns
    /// false when nothing is left. With <paramref name="all"/> the room is
    /// finished at once (builds nobody watches: buildAllNow, RebuildChunk).
    /// </summary>
    bool DressNext(bool all = false)
    {
        if (currentJob != null)
        {
            stepWatch.Restart();
            var room = currentRoom;
            // A job whose chunk is gone is dropped without using up the frame.
            if (StepRoom(all))
            {
                NoteWork("room " + room.room + " of " + room.data.coord);
                return true;
            }
        }
        while (dressQueue.Count > 0)
        {
            var job = dressQueue.Dequeue();
            if (job.chunk.root == null || !built.TryGetValue(job.data.coord, out var current) || current != job.chunk) continue;
            stepWatch.Restart();
            // A room that gets nothing (no office, no pile, no module props) does not use up the frame.
            if (!Dress(job.chunk, job.data, job.room)) continue;
            if (currentJob != null)
            {
                currentRoom = job;
                StepRoom(all);
            }
            NoteWork("room " + job.room + " of " + job.data.coord);
            return true;
        }
        return false;
    }

    /// <summary>
    /// One budget of the open job (all of it with <paramref name="all"/>); lets
    /// it go once it is done, dropped or failed. False when its chunk was
    /// dropped or rebuilt meanwhile (nothing was done).
    /// </summary>
    bool StepRoom(bool all)
    {
        var room = currentRoom;
        var worked = true;
        try
        {
            // Its chunk was dropped or rebuilt meanwhile: stop (what it made goes with the old chunk).
            if (room.chunk.root == null || !built.TryGetValue(room.data.coord, out var standing) || standing != room.chunk)
            {
                currentJob.Cancel();
                worked = false;
            }
            else if (all) currentJob.Complete();
            else if (!currentJob.Step(dressBudgetMs)) return true;
        }
        catch (Exception e)
        {
            currentJob.Cancel();
            Debug.LogWarning("[FrontRoomsMap] Furnishing room " + room.room + " of chunk " + room.data.coord + " failed: " + e.Message);
        }
        currentJob = null;
        currentRoom = default;
        return worked;
    }

    /// <summary>Furnish room r. False when it placed nothing.</summary>
    bool Dress(BuiltChunk chunk, MapChunk data, int r)
    {
        var cs = MapGrid.CellSize;
        var room = data.rooms[r];
        var zone = Cache.ZoneOf(data.Cell(room.x, room.y));
        var height = MapGrid.CeilingHeight(zone.height);
        var roomSeed = (int)MapHash.Hash(Cache.Generator.Seed, data.coord.x * 16 + r, data.coord.y, 307, data.revision);
        var columns = Columns(data, room);
        var clear = new List<Rect>(KeepClear(data, room));
        var o = data.Origin;
        Rect? doorway = null;
        if (hasStartArea)
        {
            // The stream room's door opens onto this room: keep its swing and the way in clear.
            if (room.Contains(startDoorCell.x - o.x, startDoorCell.y - o.y))
            {
                var depth = ModuleUnits.DoorClearDepth + ModuleUnits.WallHalf;
                doorway = new Rect((startDoorCell.x - o.x) * cs, (startDoorCell.y - o.y) * cs, cs, depth);
                clear.Add(doorway.Value);
            }
            foreach (var w in startKeepClear)
            {
                var a = chunk.root.transform.InverseTransformPoint(new Vector3(w.xMin, 0f, w.yMin));
                var b = chunk.root.transform.InverseTransformPoint(new Vector3(w.xMax, 0f, w.yMax));
                var local = Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.z, b.z), Mathf.Max(a.x, b.x), Mathf.Max(a.z, b.z));
                if (local.Overlaps(new Rect(room.x * cs, room.y * cs, room.w * cs, room.h * cs))) clear.Add(local);
            }
        }
        else
        {
            // The player starts in the middle of chunk (0, 0): keep that spot clear too.
            var mid = MapGrid.ChunkCells / 2;
            if (data.coord.x == 0 && data.coord.y == 0 && room.Contains(mid, mid))
            {
                var spawn = SpawnPoint();
                clear.Add(new Rect(spawn.x - 1f, spawn.z - 1f, 2f, 2f));
            }
        }
        try
        {
            // A module's own props go in first; the kits fill round them.
            var module = data.ModuleOf(r);
            var obstacles = new List<Rect>(columns);
            if (module != null) obstacles.AddRange(PlaceProps(chunk, room, module, clear, doorway, columns));
            var worked = obstacles.Count > columns.Count || (module?.props != null && module.props.Length > 0);
            // The fill keeps off the zone key (a metre round it) and the Relay's entries (its body and a margin).
            if (data.hasKey && room.Contains(data.keyCell.x - o.x, data.keyCell.y - o.y))
            {
                var kx = data.keySpot ? data.keyX : (data.keyCell.x - o.x + .5f) * cs;
                var kz = data.keySpot ? data.keyZ : (data.keyCell.y - o.y + .5f) * cs;
                // A raised key needs no floor kept for it only when a module prop that was placed holds it up
                // (a prop over an opening or in a column is left out, and a raised spot may have nothing under it).
                var k = new Vector2(kx, kz);
                var onPlacedProp = data.keySpot && data.keyY >= .05f && obstacles.GetRange(columns.Count, obstacles.Count - columns.Count).Exists(f => f.Contains(k));
                if (!onPlacedProp) clear.Add(new Rect(kx - .5f, kz - .5f, 1f, 1f));
            }
            if (module != null && module.markers != null)
                foreach (var mk in module.markers)
                    if (mk.kind == ModuleMarkerKind.RelayEntry)
                        clear.Add(new Rect(room.x * cs + mk.x - .4f, room.y * cs + mk.z - .4f, .8f, .8f));
            if (module != null)
            {
                // The fill sees the module's inner walls (their 0.16 m bands) as obstacles and keeps its inner doorways clear.
                foreach (var s in module.InnerStrips())
                {
                    var strip = Rect.MinMaxRect(room.x * cs + s[0], room.y * cs + s[1], room.x * cs + s[2], room.y * cs + s[3]);
                    if (s[4] > 0f) obstacles.Add(strip); else clear.Add(strip);
                }
            }
            var fill = module == null ? ModuleFill.Auto : module.fill;
            var office = fill == ModuleFill.Office || (fill == ModuleFill.Auto && zone.theme == ZoneTheme.Office);
            if (office)
            {
                if (!dressOffices) return worked;
                // The clear floor between wall faces; keep-clear strips and columns are in the same chunk-local metres.
                var floor = new Rect(room.x * cs + ModuleUnits.WallHalf, room.y * cs + ModuleUnits.WallHalf,
                    room.w * cs - ModuleUnits.WallThickness, room.h * cs - ModuleUnits.WallThickness);
                var args = new object[] { chunk.root.transform, floor, height, roomSeed, clear.ToArray(), obstacles.ToArray() };
                // The job's "office dressing" root is made now; DressNext steps the rest over the next frames.
                if (officeBegin != null) currentJob = (FrontRoomsDressJob)officeBegin.Invoke(null, args);
                else if (officeDress != null) officeDress.Invoke(null, args);
                else if (officeDressOld != null) officeDressOld.Invoke(null, new object[] { chunk.root.transform, floor, height, roomSeed, clear.ToArray() });
                return officeBegin != null || officeDress != null || officeDressOld != null || worked;
            }
            var pile = fill == ModuleFill.Pile
                || (fill == ModuleFill.Auto && Mathf.Min(room.w, room.h) >= 4 && MapHash.Unit((uint)roomSeed) < (zone.height == ZoneHeight.Tall ? Mathf.Max(pileChance, .6f) : pileChance));
            if (pile && pileBuild != null)
            {
                // The pile keeps off the module's props as it does off the strips.
                var keepOff = new List<Rect>(clear);
                keepOff.AddRange(obstacles.GetRange(columns.Count, obstacles.Count - columns.Count));
                if (PileSpot(room, columns, keepOff, out var center, out var radius))
                {
                    var args = new object[] { chunk.root.transform, center, radius, height, roomSeed };
                    if (pileBegin != null) currentJob = (FrontRoomsDressJob)pileBegin.Invoke(null, args);
                    else pileBuild.Invoke(null, args);
                    worked = true;
                }
            }
            return worked;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[FrontRoomsMap] Furnishing room " + r + " of chunk " + data.coord + " failed: " + (e.InnerException ?? e).Message);
            return true;
        }
    }

    /// <summary>
    /// First-use loads the map would otherwise pay in the frames after the
    /// title's handoff, where the player is watching: the kit dressers found
    /// by reflection, every kit model, sidecar and slot material, one run of
    /// the dressers' code (FrontRoomsDressJob.WarmUp: off screen, changes
    /// nothing built later), the Office grade profile and, when the game set
    /// one, the start group's warm-up (StationWarmUp, A26). Call once while
    /// nothing is on screen (the game does it in Awake).
    /// </summary>
    public static void Prewarm()
    {
        ResolveDressers();
        FrontRoomsDressJob.WarmUp();
        Resources.Load<VolumeProfile>("Rendering/FrontRoomsPost_Office");
        var station = StationWarmUp;
        StationWarmUp = null;
        station?.Invoke();
    }

    /// <summary>
    /// A26: the floor-1 start group's warm-up, run once by the next Prewarm and cleared: the game sets it before
    /// Prewarm when floor 1 will have a station (one plan on a throwaway cache). Null: nothing, as before.
    /// </summary>
    public static Action StationWarmUp;

    /// <summary>
    /// A module's props, placed with the visual chat's kit library at their
    /// module positions. Returns their footprints in chunk-local metres.
    /// </summary>
    /// <param name="doorway">The stream door's opening and swing, when it opens onto this room: nothing goes there, at any height.</param>
    /// <param name="columns">The room's columns: they run floor to ceiling, so a prop on one is left out at any height.</param>
    List<Rect> PlaceProps(BuiltChunk chunk, CellRect room, RoomModuleData module, List<Rect> clear, Rect? doorway = null, List<Rect> columns = null)
    {
        var footprints = new List<Rect>();
        if (module.props == null || module.props.Length == 0) return footprints;
        var cs = MapGrid.CellSize;
        var root = new GameObject("module props").transform;
        root.SetParent(chunk.root.transform, false);
        float ox = room.x * cs, oz = room.y * cs;
        var leftOut = PropsLeftOut(module, ox, oz, clear, doorway, columns, null, out var rects);
        for (var index = 0; index < module.props.Length; index++)
        {
            var p = module.props[index];
            if (string.IsNullOrEmpty(p.kit)) continue;
            var f = KitFootprint(p.kit);
            var onFloor = p.y <= ModuleUnits.RelayHeight;
            var rect = rects[index] ?? default;
            if (leftOut[index] != null)
            {
                Debug.LogWarning("[FrontRoomsMap] Module prop " + p.kit + " at (" + p.x.ToString("0.00") + ", " + p.z.ToString("0.00") + ") " + leftOut[index] + " chunk " + chunk.root.name + "; left out.");
                continue;
            }
            var spawned = PlaceProp(module, index, root, ox, oz);
            if (spawned == null) continue;
            // Wall pieces above head height leave the floor free.
            if (f == null || !onFloor) continue;
            footprints.Add(rect);
        }
        return footprints;
    }

    /// <summary>
    /// Which of a module's props are left out, and why (null: placed), with their footprints (null for a prop with no
    /// known kit), in the metres of the room's parent (the room's south-west corner at (<paramref name="ox"/>,
    /// <paramref name="oz"/>)). Worked out for all of them first, so a desk-top item can go with the prop it stands on,
    /// whatever their order. A floor prop over a <paramref name="clear"/> rect, any prop over the
    /// <paramref name="doorway"/>, a prop in a column; then <paramref name="extra"/>'s reason (A26: the station's own
    /// rules), if any. Shared by PlaceProps and the station's set piece (FrontRoomsStationSet).
    /// </summary>
    internal static string[] PropsLeftOut(RoomModuleData module, float ox, float oz, List<Rect> clear, Rect? doorway, List<Rect> columns,
        Func<ModuleProp, float[], Rect, string> extra, out Rect?[] rects)
    {
        var count = module.props.Length;
        rects = new Rect?[count];
        var leftOut = new string[count];
        for (var index = 0; index < count; index++)
        {
            var p = module.props[index];
            var f = string.IsNullOrEmpty(p.kit) ? null : KitFootprint(p.kit);
            if (f == null) continue;
            RoomModuleData.Bounds(p, f, out var x0, out var z0, out var x1, out var z1);
            var rect = Rect.MinMaxRect(ox + x0, oz + z0, ox + x1, oz + z1);
            rects[index] = rect;
            // The map may open a wall the module drew (a chunk-border gate, a reconnection): never block it.
            if ((p.y <= ModuleUnits.RelayHeight && !p.noCollider && clear.Exists(s => s.Overlaps(rect))) || (doorway.HasValue && doorway.Value.Overlaps(rect)))
                leftOut[index] = "would block an opening of";
            // The map's columns (Auto, or the module's own) stand where they stand.
            else if (columns != null && columns.Exists(s => s.Overlaps(rect)))
                leftOut[index] = "stands in a column of";
            else if (extra != null)
                leftOut[index] = extra(p, f, rect);
        }
        for (var index = 0; index < count; index++)
        {
            // A raised item (on a desk, a cabinet) whose footprint is over a prop left out goes with it.
            var p = module.props[index];
            if (leftOut[index] != null || rects[index] == null || p.y <= .01f || p.y > ModuleUnits.RelayHeight) continue;
            for (var k = 0; k < count; k++)
                if (k != index && leftOut[k] != null && module.props[k].y <= .01f && rects[k].HasValue && rects[k].Value.Overlaps(rects[index].Value)) { leftOut[index] = "stood on a prop left out of"; break; }
        }
        return leftOut;
    }

    /// <summary>
    /// Spawn one module prop (the visual chat's kit library) under <paramref name="root"/>, the room's south-west corner
    /// at (<paramref name="ox"/>, <paramref name="oz"/>) in the root's metres, tagged for the Level Designer when
    /// TagModuleProps is on. Null when the kit library has no such kit. PlaceProps' per-prop step, shared with the
    /// station's set piece, which spawns a few a frame.
    /// </summary>
    internal GameObject PlaceProp(RoomModuleData module, int index, Transform root, float ox, float oz)
    {
        var p = module.props[index];
        var spawned = FrontRoomsKitLibrary.Spawn(p.kit, root, new Vector3(ox + p.x, p.y, oz + p.z), p.yaw, null, !p.noCollider, p.kit);
        if (spawned == null) return null;
        if (TagModuleProps)
        {
            var tag = spawned.AddComponent<FrontRoomsModulePropTag>();
            tag.module = module;
            tag.index = index;
            tag.roomOrigin = new Vector3(ox, 0f, oz);
        }
        return spawned;
    }

    static float Distance(Rect r, Vector2 p)
    {
        var dx = Mathf.Max(r.xMin - p.x, 0f, p.x - r.xMax);
        var dy = Mathf.Max(r.yMin - p.y, 0f, p.y - r.yMax);
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// A kit asset's footprint about its pivot and its height: (min x, min z, max x, max z, height), or null; a kit with a
    /// "screen" anchor (the CRT TV) adds its screen's centre about the pivot (x, y, z), for the station's R3 rules
    /// (RoomModuleData.ValidateStation).
    /// </summary>
    public static float[] KitFootprint(string kit)
    {
        var info = FrontRoomsKitLibrary.GetInfo(kit);
        if (info == null) return null;
        var f = info.Footprint;
        if (info.TryAnchor("screen", out var screen)) return new[] { f.xMin, f.yMin, f.xMax, f.yMax, info.Height, screen.x, screen.y, screen.z };
        return new[] { f.xMin, f.yMin, f.xMax, f.yMax, info.Height };
    }

    /// <summary>
    /// The visual chat's local Office grade (FrontRoomsPostStack.EnsureZoneVolume)
    /// over this chunk's Office cells: runs of Office cells per row, merged
    /// into rectangles down the rows, one volume each. Its 2.5 m blend hides
    /// the seams between them.
    /// </summary>
    void AddZoneGrades(BuiltChunk chunk, MapChunk data)
    {
        const int n = MapGrid.ChunkCells;
        var cs = MapGrid.CellSize;
        var open = new List<(int x0, int x1, int y0, int y1)>();
        var done = new List<(int x0, int x1, int y0, int y1)>();
        for (var j = 0; j <= n; j++)
        {
            var runs = new List<(int, int)>();
            for (var i = 0; j < n && i < n;)
            {
                if (!OfficeGraded(data.Cell(i, j))) { i++; continue; }
                var start = i;
                while (i < n && OfficeGraded(data.Cell(i, j))) i++;
                runs.Add((start, i));
            }
            var next = new List<(int x0, int x1, int y0, int y1)>();
            foreach (var rect in open)
            {
                var k = runs.IndexOf((rect.x0, rect.x1));
                if (k >= 0) { next.Add((rect.x0, rect.x1, rect.y0, j + 1)); runs.RemoveAt(k); }
                else done.Add(rect);
            }
            foreach (var run in runs) next.Add((run.Item1, run.Item2, j, j + 1));
            open = next;
        }
        for (var k = 0; k < done.Count; k++)
        {
            var rect = done[k];
            var size = new Vector3((rect.x1 - rect.x0) * cs, ModuleUnits.StandardCeiling, (rect.y1 - rect.y0) * cs);
            var center = new Vector3(rect.x0 * cs + size.x * .5f, size.y * .5f, rect.y0 * cs + size.z * .5f);
            // EnsureZoneVolume keeps one volume per parent, so each rectangle gets its own.
            var holder = new GameObject("Office grade " + k).transform;
            holder.SetParent(chunk.root.transform, false);
            FrontRoomsPostStack.EnsureZoneVolume(holder, "Office", new Bounds(center, size), 2.5f);
        }
    }

    // The stream rooms keep their own look: no Office grade over the start area,
    // nor over the cells beside it (a grade blends in 2.5 m before its box).
    // X has no grade of its own either (C22: the stairwell's look is the visual
    // chat's); the cells round it are the maze and keep theirs.
    bool OfficeGraded(GridCoord cell) =>
        !(hasStartArea && cell.x >= startArea.xMin - 1 && cell.x <= startArea.xMax && cell.y >= startArea.yMin - 1 && cell.y <= startArea.yMax)
        && !InExitArea(cell) && Cache.ZoneOf(cell).theme == ZoneTheme.Office;

    /// <summary>Footprints of the columns inside a room, in chunk-local metres.</summary>
    List<Rect> Columns(MapChunk data, CellRect room)
    {
        const int n = MapGrid.ChunkCells;
        var cs = MapGrid.CellSize;
        var result = new List<Rect>();
        for (var j = room.y + 1; j < room.y + room.h; j++)
        for (var i = room.x + 1; i < room.x + room.w; i++)
        {
            if (!data.pillar[i + j * (n + 1)]) continue;
            var w = (data.pillarStyle[i + j * (n + 1)] & MapChunk.ColumnLarge) != 0 ? ModuleUnits.ColumnLarge : ModuleUnits.ColumnSmall;
            result.Add(new Rect(i * cs - w * .5f, j * cs - w * .5f, w, w));
        }
        return result;
    }

    /// <summary>
    /// Where a furniture pile goes: the room centre, or with columns the
    /// centre of the 6 m bay nearest it, sized so the pile and its 0.6 m
    /// clear ring keep off the columns and the walls, and the pile itself off
    /// every keep-clear strip (openings, the spawn). False if no pile fits.
    /// </summary>
    bool PileSpot(CellRect room, List<Rect> columns, List<Rect> clear, out Vector3 center, out float radius)
    {
        const float ring = .6f, minRadius = 1.2f;
        var cs = MapGrid.CellSize;
        var min = new Vector2(room.x * cs + ModuleUnits.WallHalf, room.y * cs + ModuleUnits.WallHalf);
        var max = new Vector2((room.x + room.w) * cs - ModuleUnits.WallHalf, (room.y + room.h) * cs - ModuleUnits.WallHalf);
        var c = (min + max) * .5f;
        if (columns.Count > 0)
        {
            // Bay centres sit half a grid off the column lines (chunk origins are multiples of 24 m, so local = world phase).
            var g = ModuleUnits.ColumnGrid;
            c = new Vector2((Mathf.Floor(c.x / g) + .5f) * g, (Mathf.Floor(c.y / g) + .5f) * g);
        }
        radius = Mathf.Min(3.2f, Mathf.Min(room.w, room.h) * cs * .22f);
        radius = Mathf.Min(radius, Mathf.Min(Mathf.Min(c.x - min.x, max.x - c.x), Mathf.Min(c.y - min.y, max.y - c.y)) - ring);
        foreach (var col in columns) radius = Mathf.Min(radius, Distance(col, c) - ring);
        foreach (var strip in clear) radius = Mathf.Min(radius, Distance(strip, c));
        center = new Vector3(c.x, 0f, c.y);
        return radius >= minRadius;
    }

    /// <summary>
    /// Every passable stretch of the room's boundary (open, doorway, door or
    /// window) gets a strip of floor kept clear along the inside of that
    /// cell edge: <see cref="ModuleUnits.EntryClearDepth"/> deep, or
    /// <see cref="ModuleUnits.DoorClearDepth"/> for a door so its leaf can
    /// swing. Dress treats the boundary as wall wherever no strip touches it.
    /// The edges are read with the exit rule (C22): a room holding N keeps the
    /// EXIT door's strip, and X's neighbours keep none on what are walls now.
    /// The start area's rule is left out on purpose, so the furnishing round
    /// the title's stream rooms stays exactly as it was (its rooms keep strips
    /// on edges the map walls: floor that stays clear, nothing worse).
    /// </summary>
    Rect[] KeepClear(MapChunk data, CellRect room)
    {
        var cs = MapGrid.CellSize;
        var clear = new List<Rect>();
        for (var y = room.y; y < room.y + room.h; y++)
        for (var x = room.x; x < room.x + room.w; x++)
        {
            var cell = data.Cell(x, y);
            foreach (var d in new[] { new GridCoord(1, 0), new GridCoord(-1, 0), new GridCoord(0, 1), new GridCoord(0, -1) })
            {
                if (room.Contains(x + d.x, y + d.y)) continue;
                var kind = ExitKind(cell, cell + d, Cache.Edge(cell, cell + d));
                if (!MapGrid.Passable(kind)) continue;
                // Strips start on the cell line, so they reach their depth from the wall face.
                var depth = (kind == EdgeKind.Door ? ModuleUnits.DoorClearDepth : ModuleUnits.EntryClearDepth) + ModuleUnits.WallHalf;
                if (d.x != 0) clear.Add(new Rect(d.x > 0 ? (x + 1) * cs - depth : x * cs, y * cs, depth, cs));
                else clear.Add(new Rect(x * cs, d.y > 0 ? (y + 1) * cs - depth : y * cs, cs, depth));
            }
        }
        return clear.ToArray();
    }

    // ---------- Interaction ----------

    public string Describe(Collider c, out bool holdToUse)
    {
        holdToUse = false;
        if (c == null) return null;
        // A look+E key's trigger (KeyPickupNeedsE).
        if (keyByCollider.ContainsKey(c)) return "E  ·  TAKE KEY";
        if (doorByCollider.TryGetValue(c, out var door))
        {
            // Broken, the Relay is breaking it, a key is turning in it, or it is the sealed EXIT door: nothing to do with it.
            if (door.broken || IsBeingBroken(door) || IsHeldForKey(door) || IsSealedExit(door)) return null;
            if (door.open) return "E  ·  SHUT DOOR";
            // M3 (A22): on a floor-key floor a locked door names the stair key (the EXIT, and any inner door locked there).
            if (floorKey && NeedsKey(door)) return LockedHere(door) ? LockedNeedsStairKey : UnlockPrompt;
            return LockedHere(door) ? "LOCKED  ·  NEEDS THIS ZONE'S KEY" : "E  ·  OPEN DOOR";
        }
        if (windowByCollider.ContainsKey(c))
        {
            holdToUse = true;
            return "HOLD E  ·  BREAK GLASS";
        }
        return null;
    }

    /// <summary>
    /// H1: the kind of what <paramref name="c"/> is for the crosshair (AimKind); Describe gives its words.
    /// Not None exactly when Describe has a prompt, and for a broken door (Broken, no prompt). For a door
    /// that needs a key, keyZone is the zone whose key opens it from <paramref name="from"/> (KeyFor's
    /// rule: where the player stands) and haveKey whether that key is held (LockedHaveKey). M3: on a
    /// floor-key floor keyZone is FloorKeyZone for every locked door, the EXIT included.
    /// </summary>
    public AimKind Kind(Collider c, Vector3 from, out GridCoord keyZone, out bool haveKey)
    {
        keyZone = default;
        haveKey = false;
        if (c == null) return AimKind.None;
        if (keyByCollider.ContainsKey(c)) return AimKind.TakeKey;
        if (doorByCollider.TryGetValue(c, out var door))
        {
            // The sealed EXIT door (C22): no prompt, so no kind.
            if (IsSealedExit(door)) return AimKind.None;
            if (door.broken) return AimKind.Broken;
            if (IsBeingBroken(door) || IsHeldForKey(door)) return AimKind.None;
            if (door.open) return AimKind.Shut;
            if (!NeedsKey(door)) return AimKind.Open;
            // The locked EXIT (ExitLocked, C23 round 3): no zone key fits it, so never LockedHaveKey. The floor key does (M3).
            if (IsExitDoor(door) && !floorKey) return AimKind.Locked;
            if (Cache != null)
            {
                keyZone = KeyZoneAt(CellOf(from));
                haveKey = keysHeld.Contains(keyZone);
            }
            return haveKey ? AimKind.LockedHaveKey : AimKind.Locked;
        }
        if (windowByCollider.ContainsKey(c)) return FrontRoomsSettings.TapToBreak ? AimKind.GlassTap : AimKind.Glass;
        return AimKind.None;
    }

    /// <summary>
    /// E on a door: shut it, open it, rattle it (locked), or start a key turn
    /// (keyable, with a KeyTurnOwner: the door is held and nothing is raised;
    /// the owner's unlock shot commits and opens it). Without an owner a
    /// keyable door unlocks and opens at once. A broken door: no door action and
    /// no noise, Broken with DoorBrokenTried (None for a try within
    /// BrokenDoor.Duration of the last). One being broken,
    /// one whose lever is down, one held by a key turn, or a locked door tried
    /// again within Rattle.Duration of its last rattle: nothing (None). With
    /// KeyPickupNeedsE, E on a key's trigger takes the key (KeyTaken: it is
    /// held at once and goes to the presenter, as a walk-over would).
    /// </summary>
    public UseResult Use(Collider c)
    {
        if (c != null && keyByCollider.ContainsKey(c)) return TakeKeyByE(c) ? UseResult.KeyTaken : UseResult.None;
        if (c == null || !doorByCollider.TryGetValue(c, out var door)) return UseResult.None;
        // The sealed EXIT door (C22, D7): the map shut it for good; E does nothing, from the seal on.
        if (IsSealedExit(door)) return UseResult.None;
        // Broken for the run (Red, 2026-10-14): it can't be shut, so E is a failed try, never a door verb or a noise.
        if (door.broken)
        {
            if (doorClock - door.brokenTriedAt < FrontRoomsShotTimings.BrokenDoor.Duration) return UseResult.None;
            door.brokenTriedAt = doorClock;
            DoorBrokenTried?.Invoke(door, player != null ? player.position : door.position);
            return UseResult.Broken;
        }
        // The Relay is breaking it: hands off (Describe shows no prompt).
        if (IsBeingBroken(door)) return UseResult.None;
        // The lever is down (the door opens by itself), or a key is turning in it.
        if (door.motion == DoorMotion.Lever || IsHeldForKey(door)) return UseResult.None;
        // A try right after a rattle: no second rattle, so the jolts and the sounds never stack.
        if (doorClock - door.rattledAt < FrontRoomsShotTimings.Rattle.Duration) return UseResult.None;
        var from = player != null ? player.position : door.position;
        if (door.open)
        {
            ShutDoor(door, from);
            return UseResult.Shut;
        }
        if (NeedsKey(door))
        {
            var zone = KeyFor(door, from);
            if (!zone.HasValue)
            {
                door.rattledAt = doorClock;
                // A12: the trier pushes, so the leaf jolts away from them.
                SwingAwayFrom(door, from);
                DoorLocked?.Invoke(door.position);
                DoorRattled?.Invoke(door, LockPoint(door, from));
                Rattle(door);
                return UseResult.Rattled;
            }
            if (KeyTurnOwner != null)
            {
                // The owner's shot turns the key: the door waits, held, until it commits and releases.
                // A12: the leaf will open away from the key's side, so the shot is a push from any face.
                SwingAwayFrom(door, from);
                keyTurns[door.edge] = new KeyTurnHold { zone = zone.Value, from = from };
                return UseResult.KeyTurn;
            }
            UnlockAtOnce(door, zone.Value, from);
            return UseResult.Opened;
        }
        OpenDoor(door, from, DoorOpenSource.Use);
        return UseResult.Opened;
    }

    /// <summary>The door whose leaf this collider is, or null.</summary>
    public Door DoorOf(Collider c) => c != null && doorByCollider.TryGetValue(c, out var door) ? door : null;

    /// <summary>
    /// W5 seam: is this a locked door by the level's rule (position-independent)? Every inner door is when
    /// doors need keys (InteriorDoorsLocked: never on Floor 1 while its station is held, A13), except the EXIT
    /// door (C22, D6), which no zone key opens: its rule is its own floor-key lock, ExitLocked (C23 round 3,
    /// A13's seam, set by SetExitLocked: the game locks it on a floor-key floor, M3; whatever the keys flag),
    /// and only the floor key opens it (KeyFor). The seal (SealExit) is apart from this. It will pick the
    /// LOCKED kit member at build time, the short prompt, and is KeyFor's precondition.
    /// </summary>
    public bool LockedByRule(Door door) => door != null && (IsExitDoor(door) ? exitLocked : InteriorDoorsLocked);

    /// <summary>
    /// §7 Q3 seam, the only place the key rule lives: the zone whose key opens this door from
    /// <paramref name="from"/>, if it is held; null when none does, or when the door needs no key
    /// (not LockedByRule, or unlocked already). M3: on a floor-key floor (FloorKeyRule) the floor key
    /// (FloorKeyZone) opens the EXIT and every locked door on its floor, from anywhere, and no zone key
    /// fits. Otherwise the zone <paramref name="from"/> stands in, and never a key for the EXIT door: no
    /// zone key opens it (C23 round 3).
    /// </summary>
    public GridCoord? KeyFor(Door door, Vector3 from)
    {
        if (!NeedsKey(door) || Cache == null) return null;
        if (floorKey) return keysHeld.Contains(FloorKeyZone) ? FloorKeyZone : (GridCoord?)null;
        if (IsExitDoor(door)) return null;
        var zone = Cache.ZoneOf(CellOf(from)).id;
        return keysHeld.Contains(zone) ? zone : (GridCoord?)null;
    }

    // Locked by the rule and not yet opened by a key (from either side).
    bool NeedsKey(Door door) => LockedByRule(door) && !unlockedDoors.Contains(door.edge);

    /// <summary>True while a key turn holds this door (the unlock shot runs): Use and TryOpenDoor leave it, and Describe shows nothing.</summary>
    public bool IsHeldForKey(Door door) => door != null && keyTurns.ContainsKey(door.edge);

    /// <summary>The key-turn owner's heartbeat, every tick of its shot. A hold without one for Unlock.Duration + KeyTurnWatchdogGrace is resolved.</summary>
    public void RefreshKeyTurn(Door door)
    {
        if (door != null && keyTurns.TryGetValue(door.edge, out var hold)) hold.sinceRefresh = 0f;
    }

    /// <summary>
    /// The key has turned (the shot's Unlock.Commit): the door is unlocked for good, the leaf shifts
    /// Unlock.LeafShiftMetres as the bolt draws (the rig's leaf, or the cube's JoltLeaf), and
    /// DoorUnlocked is raised with the hold's zone and the keyhole. The deadbolt's slide is the
    /// shot's. The door stays held until ReleaseKeyTurn. M3: on the locked EXIT the floor key's commit
    /// lifts ExitLocked first (one ExitLockChanged(door, false): A13's chain drops), then DoorUnlocked.
    /// </summary>
    public void CommitUnlock(Door door)
    {
        if (door == null || !keyTurns.TryGetValue(door.edge, out var hold) || hold.committed) return;
        if (doorByEdge.TryGetValue(door.edge, out var current)) door = current;
        hold.committed = true;
        unlockedDoors.Add(door.edge);
        if (door.rig != null) door.rig.Jolt(FrontRoomsShotTimings.Unlock.LeafShiftMetres);
        else JoltLeaf(door, FrontRoomsShotTimings.Unlock.LeafShiftMetres, 0f, FrontRoomsShotTimings.Unlock.BoltJolt);
        if (IsExitDoor(door) && exitLocked) SetExitLocked(false);
        DoorUnlocked?.Invoke(door, hold.zone, LockPose(door, hold.from).position);
    }

    /// <summary>A key turn given up before its commit: the hold goes and the door is still locked. Nothing is raised.</summary>
    public void CancelKeyTurn(Door door)
    {
        if (door != null) keyTurns.Remove(door.edge);
    }

    /// <summary>
    /// A committed key turn is over: the hold goes. <paramref name="open"/>: the door opens from
    /// <paramref name="from"/> (DoorOpenSource.Unlock), unless it is open, broken or being broken,
    /// or its chunk is gone. <paramref name="latchDrawn"/> (the push-side unlock, after its knob
    /// beat): no lever wait and no DoorHandleTurned; the leaf leaves the frame at once.
    /// </summary>
    public void ReleaseKeyTurn(Door door, bool open, Vector3 from, bool latchDrawn = false)
    {
        if (door == null || !keyTurns.Remove(door.edge) || !open) return;
        if (!doorByEdge.TryGetValue(door.edge, out var current)) return;
        if (current.open || current.broken || IsBeingBroken(current) || current.motion == DoorMotion.Lever) return;
        OpenDoor(current, from, DoorOpenSource.Unlock, latchDrawn);
    }

    // No owner to turn the key (the walker, tools): unlock and open in one go, as a key turn would.
    void UnlockAtOnce(Door door, GridCoord zone, Vector3 from)
    {
        keyTurns[door.edge] = new KeyTurnHold { zone = zone, from = from };
        CommitUnlock(door);
        ReleaseKeyTurn(door, true, from);
    }

    /// <summary>Unit, flat: out of the leaf's face on <paramref name="from"/>'s side (toward the opener), as LockPoint picks the face.</summary>
    public static Vector3 LockNormal(Door door, Vector3 from)
    {
        var across = DoorRotation(door) * Vector3.right;
        across.y = 0f;
        across.Normalize();
        return Vector3.Dot(from - door.position, across) >= 0f ? across : -across;
    }

    /// <summary>
    /// The face <paramref name="from"/> stands on, in the kit's terms: S, the door root's +X face (the kit's
    /// authored S face), or P, its −X face. Since A12 the double-acting door is not mirrored for a handing and
    /// its leaf swings either way, so the face is fixed by where the point stands, as LockNormal picks it.
    /// </summary>
    public static DoorFace FaceOf(Door door, Vector3 from) => Vector3.Dot(from - door.position, DoorRotation(door) * Vector3.right) >= 0f ? DoorFace.S : DoorFace.P;

    /// <summary>Unit, flat: along the shut leaf from the latch jamb toward the hinge.</summary>
    public static Vector3 TowardHinge(Door door)
    {
        var along = DoorRotation(door) * Vector3.back;
        along.y = 0f;
        return along.normalized;
    }

    // The keyway with no rig: the spec's numbers (SPEC §6.4) on today's door root. LockPoint's height and
    // inset (1.0 m up, 0.92 m from the hinge), proud of the leaf's centre plane by half the leaf plus the
    // cylinder's 6.5 mm (±0.0315) instead of the handle's 0.03.
    const float KeyholeProud = .0065f;

    /// <summary>
    /// The keyway on <paramref name="from"/>'s face: position, and a rotation whose +Z goes into the
    /// lock and +Y is the keyhole's up. From the door's rig when it has one; otherwise the spec's
    /// numbers on the door root. LockPoint (DoorLocked, DoorHandleTurned) keeps the handle's point.
    /// </summary>
    public Pose LockPose(Door door, Vector3 from)
    {
        if (door == null) return new Pose(from, Quaternion.identity);
        if (door.rig != null)
        {
            var face = FaceOf(door, from);
            return new Pose(door.rig.Keyhole(face), Quaternion.LookRotation(door.rig.KeyholeDir(face), door.rig.KeyholeUp(face)));
        }
        var rotation = DoorRotation(door);
        var normal = LockNormal(door, from);
        var position = door.position + rotation * Vector3.forward * (DoorWidth * .5f - ModuleUnits.DoorHandleInset) + Vector3.up * ModuleUnits.DoorHandleHeight
            + normal * (ModuleUnits.DoorLeafThickness * .5f + KeyholeProud);
        return new Pose(position, Quaternion.LookRotation(-normal, Vector3.up));
    }

    /// <summary>A shut door's lock on the side of a point: in from the latch jamb, at handle height, on the leaf's face.</summary>
    public static Vector3 LockPoint(Door door, Vector3 from)
    {
        var rotation = (door.hinge != null && door.hinge.parent != null ? door.hinge.parent.rotation : Quaternion.identity) * door.closed;
        var along = rotation * Vector3.forward;   // hinge jamb to latch jamb
        var across = rotation * Vector3.right;
        var side = Vector3.Dot(from - door.position, across) >= 0f ? 1f : -1f;
        return door.position + along * (DoorWidth * .5f - ModuleUnits.DoorHandleInset) + Vector3.up * ModuleUnits.DoorHandleHeight
            + across * side * (ModuleUnits.DoorLeafThickness * .5f + ModuleUnits.DoorHandleProud);
    }

    // ---------- Doors: the swing side, opening, shutting, a body in the way ----------

    // The leaf as its swing sees it: its reach from the hinge axis and half its thickness.
    const float LeafLength = DoorWidth - ModuleUnits.DoorLeafGap * .5f, LeafHalfThickness = ModuleUnits.DoorLeafThickness * .5f;
    // The bodies a leaf stops on: the player's capsule plus the controller's 0.03 m skin and a centimetre; the Relay's plus a centimetre.
    // (The door shot's step-in keeps its stand point clear of a leaf by the player's.)
    internal const float PlayerClearance = ModuleUnits.PlayerRadius + .04f;
    const float RelayClearance = ModuleUnits.RelayRadius + .01f;
    // A leaf within this of shut is latched: opening it takes the lever first.
    const float LatchedDegrees = .5f;
    // A leaf with only a few degrees to go still eases over at least this long.
    const float MinSwingSeconds = .1f;
    // A clear spot keeps a hand's width inside the keep-clear strip's far edge, where furniture may stand.
    const float StripMargin = .1f;
    // The open's overshoot at the stop, on the swing's own clock: it starts this long before the
    // ease-out ends and lasts this long (Open.OvershootStart to OvershootEnd after the press).
    const float OvershootLead = FrontRoomsShotTimings.Open.SwingStart + FrontRoomsShotTimings.Open.SwingSeconds - FrontRoomsShotTimings.Open.OvershootStart;
    const float OvershootLength = FrontRoomsShotTimings.Open.OvershootEnd - FrontRoomsShotTimings.Open.OvershootStart;
    // A12's push back through the frame runs at the open's starting speed (its ease-out's slope at 0), so the
    // leaf passes the frame without a pause and the far side's open carries on at the same speed.
    const float ThroughDegreesPerSecond = 2f * FrontRoomsShotTimings.Open.SwingDeg / FrontRoomsShotTimings.Open.SwingSeconds;

    /// <summary>The Relay's feet while it is out (FrontRoomsMapHunter sets this), so a moving leaf stops on its body as on the player's.</summary>
    public Func<Vector3?> RelayBody { get; set; }

    /// <summary>True while the Relay is breaking this door (kept by edge, so a rebuilt chunk's new Door reads the same): no prompt, and E does nothing.</summary>
    public bool IsBeingBroken(Door door) => door != null && breakingDoors.Contains(door.edge);

    /// <summary>FrontRoomsMapHunter: it started (true) or gave up (false) breaking this door. BreakDoor clears it.</summary>
    public void MarkBeingBroken(Door door, bool breaking)
    {
        if (door == null) return;
        if (breaking) breakingDoors.Add(door.edge);
        else breakingDoors.Remove(door.edge);
    }

    // The shut hinge's world rotation: forward runs along the wall from the hinge jamb to the latch jamb.
    static Quaternion DoorRotation(Door door) =>
        (door.hinge != null && door.hinge.parent != null ? door.hinge.parent.rotation : Quaternion.identity) * door.closed;

    // The hinge axis on the floor, on the wall's centre line.
    static Vector3 HingePoint(Door door) =>
        door.hinge != null ? door.hinge.position : door.position - DoorRotation(door) * Vector3.forward * (DoorWidth * .5f);

    /// <summary>
    /// A point in the door's own floor plan, in metres: x along the wall from
    /// the hinge axis toward the latch jamb, y out from the wall's centre line
    /// into the side the leaf swings into (negative on the stop side).
    /// </summary>
    static Vector2 DoorPlan(Door door, Vector3 point)
    {
        var rotation = DoorRotation(door);
        var offset = point - HingePoint(door);
        // The leaf swings toward the hinge's local -X for swing +1 (TickDoors turns it by -angle * swing).
        return new Vector2(Vector3.Dot(offset, rotation * Vector3.forward), Vector3.Dot(offset, rotation * Vector3.right * -door.swing));
    }

    static Vector3 FromDoorPlan(Door door, Vector2 plan, float height)
    {
        var rotation = DoorRotation(door);
        var point = HingePoint(door) + rotation * Vector3.forward * plan.x + rotation * Vector3.right * (-door.swing * plan.y);
        point.y = height;
        return point;
    }

    /// <summary>True when a point stands on the side the door's leaf swings into now (the side it is open to, or would open to from the frame); false on the other.</summary>
    public static bool OnSwingSide(Door door, Vector3 point) => door != null && DoorPlan(door, point).y > 0f;

    /// <summary>True while the leaf is in the frame (latched, or within half a degree of it): its next open picks its side.</summary>
    public static bool IsLatched(Door door) => door != null && door.angle <= LatchedDegrees;

    // A12: the point stands ahead of a leaf out of the frame, where opening it further would carry the leaf
    // toward them: its bearing from the hinge axis in the door's floor plan is past the leaf's angle.
    // Anything on the other side of the frame is behind the leaf.
    static bool AheadOfLeaf(Door door, Vector3 point)
    {
        var p = DoorPlan(door, point);
        return Mathf.Atan2(p.y, p.x) * Mathf.Rad2Deg > door.angle;
    }

    /// <summary>A12 is in: doors are double-acting (always away from the opener). Read by reflection by the Relay
    /// baseline harness (FrontRooms3DGame.Baseline.cs) so its reports say which door rule they ran under.</summary>
    public static bool DoubleActingDoors => true;

    /// <summary>
    /// A12 (Red, 2026-10-22): a leaf in the frame swings to the far side of whoever moves it. While the
    /// leaf is latched and <paramref name="from"/> stands on its swing side, the swing side turns over
    /// (nothing moves: the leaf is shut). The map calls it for every open, rattle, key turn and break; the
    /// hunter calls it as a break begins and again before every blow, so the blows jolt the leaf away from it. A leaf out of the frame
    /// keeps its side.
    /// </summary>
    public void SwingAwayFrom(Door door, Vector3 from)
    {
        // The door standing on that edge now (a rebuild mid-break makes a new Door), as JoltLeaf and BreakDoor read it.
        if (door != null && doorByEdge.TryGetValue(door.edge, out var current)) door = current;
        if (door == null || !IsLatched(door) || !OnSwingSide(door, from)) return;
        door.swing = -door.swing;
        PoseDoor(door);
    }

    /// <summary>
    /// The nearest spot to <paramref name="near"/> where a body of this radius
    /// stands clear of the leaf's whole sweep: on the swing side's latch side,
    /// off the wall and the jamb, inside the door's 1.2 m keep-clear strip
    /// (0.1 m short of its far edge) and its 3 m edge, and off a column on the
    /// cell corner (the large one, in case). The height is kept. A point the
    /// leaf cannot touch on the arc this swing sweeps (<paramref name="arcFrom"/>
    /// to <paramref name="arcTo"/>; by default a whole open with its
    /// overshoot) comes back as it is: the stop side, out of reach, or beside
    /// the hinge past the open leaf.
    /// </summary>
    public static Vector3 ClearOfSwing(Door door, Vector3 near, float bodyRadius,
        float arcFrom = 0f, float arcTo = ModuleUnits.DoorSwingDegrees + FrontRoomsShotTimings.Open.OvershootDeg)
    {
        var p = DoorPlan(door, near);
        var reach = LeafLength + LeafHalfThickness + bodyRadius;
        if (p.y <= 0f || p.magnitude >= reach) return near;
        // The same contact test the leaf uses: nothing on this arc touches the body.
        if (Mathf.Abs(LimitByBody(door, arcFrom, arcTo, near, bodyRadius) - arcTo) < 1e-3f) return near;
        var outMin = ModuleUnits.WallHalf + ModuleUnits.TrimProud + bodyRadius;
        var outMax = ModuleUnits.WallHalf + ModuleUnits.DoorClearDepth - StripMargin - bodyRadius;
        // The door is centred on its edge: the cell corner is half a cell plus half a door from the hinge.
        var cornerAlong = (MapGrid.CellSize + DoorWidth) * .5f;
        var alongMax = cornerAlong - ModuleUnits.WallHalf - bodyRadius;
        var column = new Vector2(cornerAlong - ModuleUnits.ColumnLarge * .5f, ModuleUnits.ColumnLarge * .5f);
        var best = new Vector2(alongMax, outMax);
        var bestDistance = float.MaxValue;
        const float step = .02f;
        for (var o = outMin; o <= outMax + 1e-4f; o += step)
        for (var a = 0f; a <= alongMax + 1e-4f; a += step)
        {
            var q = new Vector2(a, o);
            if (q.sqrMagnitude < reach * reach) continue;
            var dx = Mathf.Max(column.x - a, 0f);
            var dy = Mathf.Max(o - column.y, 0f);
            if (dx * dx + dy * dy < bodyRadius * bodyRadius) continue;
            var d = (q - p).sqrMagnitude;
            if (d < bestDistance) { bestDistance = d; best = q; }
        }
        return FromDoorPlan(door, best, near.y);
    }

    /// <summary>
    /// The one way a door opens (Use, TryOpenDoor, a key turn's release), always away from its opener
    /// (A12, Red 2026-10-22). A leaf in the frame goes to the far side of the opener: the lever goes
    /// down, and Open.SwingStart later the leaf leaves the frame, easing out over Open.SwingSeconds onto
    /// its 95° rest with the Open overshoot. A leaf out of the frame (caught mid-shut, or resting on a
    /// body) has no latch to free: with the opener behind it, it turns back open at once; with the
    /// opener ahead of it, it runs on back through the frame and opens to the far side
    /// (DoorMotion.Through). Opens never pull, so DoorPulled is never raised here.
    /// <paramref name="latchDrawn"/>: the latch is already drawn (the unlock's knob beat), so there is
    /// no lever wait and no DoorHandleTurned.
    /// </summary>
    void OpenDoor(Door door, Vector3 from, DoorOpenSource source, bool latchDrawn = false)
    {
        SwingAwayFrom(door, from);
        var through = !IsLatched(door) && AheadOfLeaf(door, from);
        door.open = true;
        openDoors.Add(door.edge);
        ajarDoors.Remove(door.edge);
        doorSides[door.edge] = through ? -door.swing : door.swing;
        door.pull = false;
        door.wait = IsLatched(door) && !latchDrawn ? FrontRoomsShotTimings.Open.SwingStart : 0f;
        door.motion = DoorMotion.Lever;
        if (!movingDoors.Contains(door)) movingDoors.Add(door);
        TouchPassageRevision();
        DoorOpenBegan?.Invoke(door, source, latchDrawn);
        if (!latchDrawn) DoorHandleTurned?.Invoke(door, LockPoint(door, from));
        if (through) BeginThrough(door);
        else if (door.wait <= 0f) BeginOpenSwing(door, 0f);
    }

    // A12's push back through the frame: the leaf runs to the frame at the open's starting speed, then
    // TickSwing turns it over and the open's ease-out carries it to the far side. The noise goes with it.
    void BeginThrough(Door door)
    {
        door.motion = DoorMotion.Through;
        door.from = door.angle;
        door.clock = 0f;
        DoorMoved?.Invoke(door.position);
    }

    /// <summary>
    /// Shut a door from a point: the leaf turns back at once on today's
    /// smoothstep over Open.SwingSeconds (the close Foley is timed on it) and
    /// lands in the frame, latched. Standing in its way on the side it is open
    /// to raises DoorPulled (the game's step aside, kept by Red's A12 answer),
    /// and the leaf rests on the body until it is clear.
    /// </summary>
    void ShutDoor(Door door, Vector3 from)
    {
        door.open = false;
        openDoors.Remove(door.edge);
        ajarDoors.Remove(door.edge);
        door.pull = OnSwingSide(door, from);
        if (door.pull)
        {
            // Judged against the closing arc: a body the shutting leaf cannot reach stays put.
            var clear = ClearOfSwing(door, from, PlayerClearance, door.angle, 0f);
            if ((clear - from).sqrMagnitude > 1e-6f) DoorPulled?.Invoke(door, clear);
        }
        door.wait = 0f;
        door.motion = DoorMotion.Shut;
        door.shutPhase = InverseSmoothstep(Mathf.Clamp01(door.angle / ModuleUnits.DoorSwingDegrees));
        if (!movingDoors.Contains(door)) movingDoors.Add(door);
        TouchPassageRevision();
        DoorMoved?.Invoke(door.position);
    }

    // The latch is free: the leaf leaves the frame (or turns back) toward the stop. The noise goes with it.
    void BeginOpenSwing(Door door, float elapsed)
    {
        StartSwing(door, ModuleUnits.DoorSwingDegrees, true);
        door.clock = elapsed;
        DoorMoved?.Invoke(door.position);
    }

    // An eased-out swing from where the leaf is to an angle, over the share of Open.SwingSeconds its turn is of the full 95°.
    static void StartSwing(Door door, float to, bool bump)
    {
        door.motion = DoorMotion.Swing;
        door.from = door.angle;
        door.to = to;
        door.clock = 0f;
        door.seconds = Mathf.Max(MinSwingSeconds, FrontRoomsShotTimings.Open.SwingSeconds * Mathf.Abs(to - door.angle) / FrontRoomsShotTimings.Open.SwingDeg);
        door.bump = bump;
    }

    // The locked rattle: two jolts against the bolt at Rattle.Jolt1 and Jolt2, toward the swing side (the only way the stops let the leaf go).
    void Rattle(Door door)
    {
        door.jolts.Clear();
        door.jolts.Add((FrontRoomsShotTimings.Rattle.Jolt1, FrontRoomsShotTimings.Rattle.JoltMetres, FrontRoomsShotTimings.Rattle.JoltDeg, FrontRoomsShotTimings.Rattle.JoltSpring));
        door.jolts.Add((FrontRoomsShotTimings.Rattle.Jolt2, FrontRoomsShotTimings.Rattle.JoltMetres, FrontRoomsShotTimings.Rattle.JoltDeg, FrontRoomsShotTimings.Rattle.JoltSpring));
        if (!movingDoors.Contains(door)) movingDoors.Add(door);
    }

    /// <summary>
    /// Knock a door's leaf, for the picture only: the "Door leaf" child (not
    /// the hinge, so FrontRoomsDoorSound and the door's angle never see it)
    /// shifts <paramref name="metres"/> and turns <paramref name="degrees"/>
    /// on its hinge toward the swing side (A12: away from whoever moved it last), then
    /// springs back over <paramref name="springSeconds"/>. A new jolt replaces
    /// one still playing.
    /// </summary>
    public void JoltLeaf(Door door, float metres, float degrees, float springSeconds)
    {
        if (door == null) return;
        if (doorByEdge.TryGetValue(door.edge, out var current)) door = current;
        if (door.leaf == null) return;
        door.joltClock = 0f;
        door.joltMetres = Mathf.Abs(metres);
        door.joltDegrees = Mathf.Abs(degrees);
        door.joltSpring = Mathf.Max(.01f, springSeconds);
        if (!movingDoors.Contains(door)) movingDoors.Add(door);
    }

    /// <summary>
    /// The Relay's blow <paramref name="index"/> of <paramref name="count"/> on
    /// a door: a leaf jolt growing from DoorBreak.LeafJoltMinMetres to
    /// LeafJoltMaxMetres (turning at the rattle's degrees per metre, so
    /// 0.6–1.2°), springing back in DoorBreak.LeafJoltSpring.
    /// FrontRoomsMapHunter calls it at every DoorBlow; nothing else should.
    /// </summary>
    public void JoltForBlow(Door door, int index, int count)
    {
        var t = count > 1 ? Mathf.Clamp01(index / (float)(count - 1)) : 1f;
        var metres = Mathf.Lerp(FrontRoomsShotTimings.DoorBreak.LeafJoltMinMetres, FrontRoomsShotTimings.DoorBreak.LeafJoltMaxMetres, t);
        JoltLeaf(door, metres, metres * (FrontRoomsShotTimings.Rattle.JoltDeg / FrontRoomsShotTimings.Rattle.JoltMetres), FrontRoomsShotTimings.DoorBreak.LeafJoltSpring);
    }

    // Put the hinge at the door's angle (the transform FrontRoomsDoorSound reads) and the leaf child in its pose.
    static void PoseDoor(Door door)
    {
        door.progress = Mathf.Clamp01(door.angle / ModuleUnits.DoorSwingDegrees);
        if (door.hinge != null) door.hinge.localRotation = door.closed * Quaternion.Euler(0f, -door.angle * door.swing, 0f);
        PoseLeaf(door);
    }

    // The leaf child: at home; once broken, hanging crooked; plus any jolt.
    // M3 (door break, Red 2026-10-30 Q1 = pose (a)): a double-acting leaf hangs on pivots and the blow tears the top one: it leans
    // door.crooked out of its plane about the bottom pivot, toward the side it burst to (about 110 mm at the head), and sags
    // CrookedSagDeg in its plane; the dressed leaf's own pose is the same (FrontRoomsDoorBreakVisual.PoseOffset), so the
    // collider agrees with it. The EXIT door (leafRig, butt hinges) and WebGL's undressed cube leaf keep the turn in the leaf's
    // own plane about the top of its hinge edge.
    static void PoseLeaf(Door door)
    {
        if (door.leaf == null) return;
        var position = door.leafHome;
        var rotation = Quaternion.identity;
        if (door.crooked > 0f)
        {
            // The leaf stands on the floor, so its top is twice its centre's height.
            var pivot = new Vector3(0f, door.leafHome.y * 2f, 0f);
            var tilt = Quaternion.AngleAxis(-door.crooked, Vector3.right);
#if !UNITY_WEBGL
            if (door.leafRig == null)
            {
                var sag = door.crooked * (FrontRoomsShotTimings.DoorBreak.CrookedSagDeg / FrontRoomsShotTimings.DoorBreak.CrookedDeg);
                pivot = new Vector3(0f, .015f, 0f);
                tilt = Quaternion.AngleAxis(door.swing * door.crooked, Vector3.forward) * Quaternion.AngleAxis(sag, Vector3.right);
            }
#endif
            position = pivot + tilt * (position - pivot);
            rotation = tilt;
        }
        if (door.joltClock >= 0f)
        {
            var k = JoltShape(door.joltClock / door.joltSpring);
            var turn = Quaternion.AngleAxis(-door.swing * door.joltDegrees * k, Vector3.up);
            position = turn * position + Vector3.right * (-door.swing * door.joltMetres * k);
            rotation = turn * rotation;
        }
        door.leaf.transform.localPosition = position;
        door.leaf.transform.localRotation = rotation;
        // The leaf rig: the hinge's frame carried as the leaf child moved from its home (crooked, jolted).
        if (door.leafRig != null)
        {
            door.leafRig.localPosition = position - rotation * door.leafHome;
            door.leafRig.localRotation = rotation;
        }
    }

    // A jolt's size through its spring (0 to 1): out in the first fifth, then eased home.
    static float JoltShape(float s)
    {
        if (s <= 0f || s >= 1f) return 0f;
        if (s < .2f) return s / .2f;
        var back = 1f - (s - .2f) / .8f;
        return back * back;
    }

    // The smoothstep's input for an output in 0..1.
    static float InverseSmoothstep(float y) => .5f - Mathf.Sin(Mathf.Asin(1f - 2f * y) / 3f);

    // How far the leaf may go this frame from one angle toward another without entering the player's or the Relay's body.
    float LimitByBodies(Door door, float from, float to)
    {
        if (Mathf.Abs(to - from) < 1e-5f) return to;
        if (player != null) to = LimitByBody(door, from, to, player.position, PlayerClearance);
        var relay = RelayBody?.Invoke();
        if (relay.HasValue) to = LimitByBody(door, from, to, relay.Value, RelayClearance);
        return to;
    }

    /// <summary>
    /// Where a leaf turning from one angle toward another must stop for a
    /// round body standing at <paramref name="feet"/>: a test in the door's
    /// floor plan, the leaf a segment from the hinge axis. A body behind the
    /// moving leaf, out of its reach, or on the hinge itself does not stop it.
    /// </summary>
    static float LimitByBody(Door door, float from, float to, Vector3 feet, float radius)
    {
        var p = DoorPlan(door, feet);
        var r = p.magnitude;
        var touch = radius + LeafHalfThickness;
        if (r >= LeafLength + touch || r <= touch) return to;
        var bearing = Mathf.Atan2(p.y, p.x) * Mathf.Rad2Deg;
        // Half the angle over which the leaf touches the body: along its face, or (beyond its length) at its edge.
        var half = r <= LeafLength
            ? Mathf.Asin(touch / r) * Mathf.Rad2Deg
            : Mathf.Acos(Mathf.Clamp((LeafLength * LeafLength + r * r - touch * touch) / (2f * LeafLength * r), -1f, 1f)) * Mathf.Rad2Deg;
        if (to > from)
        {
            if (from < bearing - half) return Mathf.Min(to, bearing - half);
            return from <= bearing + half && bearing >= from ? from : to;
        }
        if (from > bearing + half) return Mathf.Max(to, bearing + half);
        return from >= bearing - half && bearing <= from ? from : to;
    }

    /// <summary>Tools and tests: the nearest built, unbroken window to a point, outside the start area (null when none).</summary>
    public Window NearestIntactWindowForTools(Vector3 near)
    {
        Window best = null;
        var bestDistance = float.MaxValue;
        foreach (var window in windowByCollider.Values)
        {
            if (window.pane == null || !IsBuilt(window.a) || !IsBuilt(window.b) || IsReserved(window.a) || IsReserved(window.b)) continue;
            var d = (window.position - near).sqrMagnitude;
            if (d < bestDistance) { bestDistance = d; best = window; }
        }
        return best;
    }

    /// <summary>A struck window's break record (stage, seed, impact, rotation, side); false for a window never struck (use GlassBreakRecordOf for its fresh record).</summary>
    public bool TryGetGlassBreak(long edge, out GlassBreakRecord record) => glassBreaks.TryGetValue(edge, out record);

    /// <summary>A window's break record: the stored one once struck, otherwise the fresh one WindowBuilt gave (its seed and rotation, stage 0 or 3).</summary>
    public GlassBreakRecord GlassBreakRecordOf(Window window) => GlassBreakOf(window);

    GlassBreakRecord GlassBreakOf(Window window)
    {
        if (glassBreaks.TryGetValue(window.edge, out var record)) return record;
        var seed = (int)MapHash.Hash(Cache.Generator.Seed, window.a.x * 7919 + window.b.x, window.a.y * 7919 + window.b.y, 307);
        return new GlassBreakRecord
        {
            stage = brokenWindows.Contains(window.edge) ? 3 : 0, seed = seed, impact = new Vector2(0f, (GlassExposedBottom + GlassExposedTop) * .5f),
            impactUV = new Vector2(.5f, .5f), rotation = MapHash.Unit((uint)seed) * 360f, side = 1,
        };
    }

    // The least a struck pane keeps: the stage reached (cracks do not heal). The kept progress (record.hold) is at least this.
    static float StageFloor(int stage) => stage >= 2 ? FrontRoomsShotTimings.GlassBreak.Crack2 : stage >= 1 ? FrontRoomsShotTimings.GlassBreak.Crack1 : 0f;

    // Keep a struck pane's progress with its record (Red 2026-10-22): on a release or a pause, and when the window is torn
    // down, never per Hold tick. The record exists from BeginGlassHold on; an unchanged hold writes nothing.
    void KeepHold(Window window)
    {
        if (window.hold <= 0f || !glassBreaks.TryGetValue(window.edge, out var record) || record.hold == window.hold) return;
        record.hold = window.hold;
        glassBreaks[window.edge] = record;
    }

    /// <summary>
    /// E went down on a pane: the hold begins. The impact is
    /// <paramref name="hitPoint"/> (the base-eye hit), kept 0.2 m inside the
    /// exposed glass, at the first strike; once the pane has been struck
    /// (any kept progress) that point stays. <paramref name="strike"/> is the base-eye
    /// forward (the pieces' impulse at the shatter). Raises GlassHoldStarted.
    /// Returns the window, or null when the collider is not a pane.
    /// </summary>
    public Window BeginGlassHold(Collider c, Vector3 hitPoint, Vector3 strike)
    {
        if (c == null || !windowByCollider.TryGetValue(c, out var window)) return null;
        var record = GlassBreakOf(window);
        if (record.stage == 0 && record.hold <= 0f)
        {
            var local = window.root.InverseTransformPoint(hitPoint);
            var m = FrontRoomsShotTimings.GlassBreak.ImpactMinFromFrame;
            var x = Mathf.Clamp(local.x, -GlassExposedHalfWidth + m, GlassExposedHalfWidth - m);
            var y = Mathf.Clamp(local.y, GlassExposedBottom + m, GlassExposedTop - m);
            record.impact = new Vector2(x, y);
            record.impactUV = new Vector2((x + GlassExposedHalfWidth) / (2f * GlassExposedHalfWidth), (y - GlassExposedBottom) / (GlassExposedTop - GlassExposedBottom));
        }
        // The side of this strike (a pane cracked from one side can be finished from the other): it says where the glass goes.
        var from = player != null ? player.position : hitPoint - strike;
        record.side = window.root.InverseTransformPoint(from).z <= 0f ? 1 : -1;
        glassBreaks[window.edge] = record;
        window.started = true;
        window.strike = strike.sqrMagnitude > 1e-6f ? strike.normalized : window.root.forward * record.side;
        GlassHoldStarted?.Invoke(window, ImpactWorld(window, record), record.seed);
        return window;
    }

    Vector3 ImpactWorld(Window window, GlassBreakRecord record) => window.root.TransformPoint(new Vector3(record.impact.x, record.impact.y, 0f));

    /// <summary>The impact of a window's break, in world space (the base-eye hit at the first strike; the cracks' centre once cracked).</summary>
    public Vector3 GlassImpact(Window window) => ImpactWorld(window, GlassBreakOf(window));

    /// <summary>Hold progress on a window from the base-eye hit; returns true the frame the glass breaks. Begins the hold if E-down did not.</summary>
    public bool Hold(Collider c, Vector3 hitPoint, float dt, out float progress)
    {
        progress = 0f;
        if (c == null || !windowByCollider.TryGetValue(c, out var window)) return false;
        if (!window.started)
        {
            var strike = player != null ? hitPoint - player.position : window.root.forward;
            strike.y = 0f;
            BeginGlassHold(c, hitPoint, strike);
        }
        return Hold(c, dt, out progress);
    }

    /// <summary>Hold progress on a window; returns true the frame the glass breaks.</summary>
    public bool Hold(Collider c, float dt, out float progress)
    {
        progress = 0f;
        if (c == null || !windowByCollider.TryGetValue(c, out var window)) return false;
        if (!window.started)
        {
            var strike = window.pane.transform.position - (player != null ? player.position : window.position - window.root.forward);
            strike.y = 0f;
            BeginGlassHold(c, window.pane.transform.position, strike);
        }
        window.hold += dt;
        window.pushing = true;
        progress = Mathf.Clamp01(window.hold / FrontRoomsShotTimings.GlassBreak.HoldSeconds);
        GlassHold?.Invoke(window.position, progress);
        WindowHeld?.Invoke(window, progress);
        var record = GlassBreakOf(window);
        var stage = FrontRoomsShotTimings.GlassBreak.StageAt(progress);
        // Each crack once: a hold that jumps both beats in one frame still cracks twice, in order.
        while (window.stage < Mathf.Min(stage, 2))
        {
            window.stage++;
            record.stage = window.stage;
            glassBreaks[window.edge] = record;
            GlassCracked?.Invoke(window, window.stage);
        }
        if (stage < 3) return false;
        window.stage = record.stage = 3;
        glassBreaks[window.edge] = record;
        window.started = window.pushing = false;
        brokenWindows.Add(window.edge);
        windowByCollider.Remove(c);
        TouchPassageRevision();
        // Only the collider object goes: the root (and the glass hung on it) stays.
        Kill(window.pane);
        window.pane = null;
        GlassBroken?.Invoke(window.position);
        WindowShattered?.Invoke(window, ImpactWorld(window, record), window.strike * FrontRoomsShotTimings.GlassBreak.ShatterImpulse);
        return true;
    }

    /// <summary>E let go (or the aim left the pane) before the break: the progress is kept (Red 2026-10-22), the next hold resumes from it.</summary>
    public void ReleaseHold(Collider c)
    {
        if (c == null || !windowByCollider.TryGetValue(c, out var window)) return;
        if (window.hold > 0f && window.pushing) GlassHoldReleased?.Invoke(window.position);
        KeepHold(window);
        window.pushing = false;
        window.started = false;
    }

    /// <summary>
    /// Tap mode between taps: the push stops but the progress stays. Raises
    /// GlassHoldReleased once (the stress stops); the next Hold resumes from
    /// the kept progress.
    /// </summary>
    public void PauseHold(Collider c)
    {
        if (c == null || !windowByCollider.TryGetValue(c, out var window)) return;
        if (window.pushing && window.hold > 0f) GlassHoldReleased?.Invoke(window.position);
        KeepHold(window);
        window.pushing = false;
    }

    // Shut and needing a key the player does not hold where they stand: doors need keys, no key has opened
    // this door yet, and KeyFor finds none (with no player there is nobody holding one).
    bool LockedHere(Door door) => NeedsKey(door) && (player == null || !KeyFor(door, player.position).HasValue);

    /// <summary>Tools and tests in edit mode, where Update does not run: move doors that are opening or breaking (DoorsPaused holds them too).</summary>
    public void TickDoorsForTools(float dt) => TickDoors(dt);

    void TickDoors(float dt)
    {
        // Paused: every leaf, jolt and key-turn hold waits with the game.
        if (DoorsPaused) return;
        doorClock += dt;
        TickKeyTurnWatchdog(dt);
        for (var i = movingDoors.Count - 1; i >= 0; i--)
        {
            if (i >= movingDoors.Count) continue;
            var door = movingDoors[i];
            if (door.hinge == null) { movingDoors.RemoveAt(i); continue; }
            var swinging = TickSwing(door, dt);
            var jolting = TickJolt(door, dt);
            if (!swinging && !jolting) movingDoors.Remove(door);
        }
    }

    // An orphaned key turn can never disable a door: a hold its owner stopped refreshing (it never
    // ticked, or lost the door) is resolved, as a cancel before the commit and a release (shut) after it.
    void TickKeyTurnWatchdog(float dt)
    {
        if (keyTurns.Count == 0) return;
        keyTurnScratch.Clear();
        foreach (var pair in keyTurns)
        {
            pair.Value.sinceRefresh += dt;
            if (pair.Value.sinceRefresh > FrontRoomsShotTimings.Unlock.Duration + FrontRoomsShotTimings.Unlock.KeyTurnWatchdogGrace) keyTurnScratch.Add(pair.Key);
        }
        foreach (var edge in keyTurnScratch)
        {
            var committed = keyTurns[edge].committed;
            keyTurns.Remove(edge);
            Debug.LogWarning("[FrontRoomsMap] A key turn on door " + edge + " was not refreshed by its owner for "
                + (FrontRoomsShotTimings.Unlock.Duration + FrontRoomsShotTimings.Unlock.KeyTurnWatchdogGrace).ToString("0.00") + " s: "
                + (committed ? "released, the door unlocked and shut." : "cancelled, the door still locked."));
        }
    }

    // One frame of a door's swing. False once the leaf has come to rest.
    bool TickSwing(Door door, float dt)
    {
        var wasPassable = door.angle >= DoorPassableDegrees;
        float target;
        var done = false;
        switch (door.motion)
        {
            case DoorMotion.Lever:
                door.wait -= dt;
                if (door.wait > 0f) return true;
                // The latch is free: the leaf starts, carrying the part of the frame past the lever.
                BeginOpenSwing(door, -door.wait);
                door.wait = 0f;
                return TickSwing(door, 0f);
            case DoorMotion.Swing:
            {
                door.clock += dt;
                var u = Mathf.Clamp01(door.clock / door.seconds);
                // Ease-out: a door starts fast and slows (Open), then the overshoot at the stop.
                target = door.from + (door.to - door.from) * (1f - (1f - u) * (1f - u));
                var bump = door.clock - (door.seconds - OvershootLead);
                if (door.bump && bump > 0f && bump < OvershootLength)
                    target += FrontRoomsShotTimings.Open.OvershootDeg * Mathf.Sin(Mathf.PI * bump / OvershootLength);
                done = door.clock >= door.seconds + (door.bump ? OvershootLength - OvershootLead : 0f);
                break;
            }
            case DoorMotion.Shut:
                // Smoothstep back to 0: it reaches the frame still moving, and stops there, latched.
                door.shutPhase = Mathf.Max(0f, door.shutPhase - dt / FrontRoomsShotTimings.Open.SwingSeconds);
                target = ModuleUnits.DoorSwingDegrees * door.shutPhase * door.shutPhase * (3f - 2f * door.shutPhase);
                done = door.shutPhase <= 0f;
                break;
            case DoorMotion.Throw:
            {
                // Broken: flung past the stop at full speed.
                door.clock += dt;
                var throwSeconds = FrontRoomsShotTimings.DoorBreak.ThrowSeconds;
                // M4: a dressed leaf stops on the casing (the rig is the visual's dressing; no rig, no casing).
                var throwTo = door.rig != null ? DoorBreakStopDegreesDA : DoorBreakThrowDegrees;
                target = Mathf.Lerp(door.from, throwTo, door.clock / throwSeconds);
                if (door.clock >= throwSeconds)
                {
                    // Off the stop: it bounces back to rest, tearing crooked on its hinges.
                    door.motion = DoorMotion.Bounce;
                    door.from = throwTo;
                    door.clock -= throwSeconds;
                }
                break;
            }
            case DoorMotion.Bounce:
            {
                door.clock += dt;
                var u = Mathf.Clamp01(door.clock / DoorBreakBounceSeconds);
                target = door.from + (FrontRoomsShotTimings.DoorBreak.BounceRestDeg - door.from) * (1f - (1f - u) * (1f - u));
                door.crooked = FrontRoomsShotTimings.DoorBreak.CrookedDeg * u;
                done = u >= 1f;
                break;
            }
            case DoorMotion.Through:
                // Pushed from ahead of it (A12): back to the frame at the open's starting speed; at the frame it turns over (below).
                door.clock += dt;
                target = Mathf.Max(0f, door.from - ThroughDegreesPerSecond * door.clock);
                done = target <= 0f;
                break;
            default:
                return false;
        }

        // A body in the way stops the leaf on it; never more than DoorMaxStepDegrees a frame.
        var allowed = LimitByBodies(door, door.angle, target);
        var blocked = Mathf.Abs(allowed - target) > 1e-3f;
        door.angle = Mathf.Clamp(allowed, door.angle - DoorMaxStepDegrees, door.angle + DoorMaxStepDegrees);
        if (wasPassable != (door.angle >= DoorPassableDegrees)) TouchPassageRevision();
        PoseDoor(door);
        if (blocked)
        {
            // Resting on the body: the swing starts again from here once it moves away.
            if (door.motion == DoorMotion.Shut) door.shutPhase = InverseSmoothstep(Mathf.Clamp01(door.angle / ModuleUnits.DoorSwingDegrees));
            else if (door.motion == DoorMotion.Swing) StartSwing(door, door.to, door.bump);
            else if (door.motion == DoorMotion.Through) { door.from = door.angle; door.clock = 0f; }
            else
            {
                StartSwing(door, FrontRoomsShotTimings.DoorBreak.BounceRestDeg, false);
                door.crooked = FrontRoomsShotTimings.DoorBreak.CrookedDeg;
            }
            return true;
        }
        if (!done || Mathf.Abs(door.angle - target) > 1e-3f) return true;
        if (door.motion == DoorMotion.Through)
        {
            // At the frame (A12): the far side becomes its side, and the open's ease-out carries it on, never latching.
            door.swing = -door.swing;
            PoseDoor(door);
            StartSwing(door, ModuleUnits.DoorSwingDegrees, true);
            return true;
        }
        var latched = door.motion == DoorMotion.Shut;
        door.motion = DoorMotion.None;
        if (latched) DoorLatched?.Invoke(door, door.position);
        // C22 (D7): the EXIT door the map was asked to seal locks for good as it latches.
        if (latched && exitSeal == ExitSeal.Shutting && IsExitDoor(door)) LockExit(door);
        return false;
    }

    // One frame of a door's leaf jolts. False once none is playing or waiting.
    bool TickJolt(Door door, float dt)
    {
        if (door.joltClock < 0f && door.jolts.Count == 0) return false;
        if (door.joltClock >= 0f && (door.joltClock += dt) >= door.joltSpring) door.joltClock = -1f;
        for (var i = 0; i < door.jolts.Count; i++)
        {
            var (at, metres, degrees, spring) = door.jolts[i];
            at -= dt;
            if (at > 0f) { door.jolts[i] = (at, metres, degrees, spring); continue; }
            door.jolts.RemoveAt(i--);
            door.joltClock = -at;
            door.joltMetres = metres;
            door.joltDegrees = degrees;
            door.joltSpring = Mathf.Max(.01f, spring);
        }
        PoseLeaf(door);
        return door.joltClock >= 0f || door.jolts.Count > 0;
    }

    /// <summary>
    /// The zone key of a chunk: at its module key spot (lying still, turned as
    /// the marker says), or spinning 1.05 m above its cell's centre. Its object is
    /// NewKey's, the one place a key is made, so the key model and its host plug in
    /// there. No collider, except the look+E trigger while KeyPickupNeedsE is on.
    /// </summary>
    void SpawnKey(BuiltChunk chunk, MapChunk data, bool atSpot)
    {
        var cs = MapGrid.CellSize;
        // D1.6 (contract M3): at a module key spot with a host (SpawnKeyHost), the dressed key hangs from the host's hook.
        var hung = atSpot && keyHostHook.HasValue;
        var key = NewKey(chunk, data.ownZone.id, "Key · zone " + data.ownZone.id, out var dressed, hung);
        if (dressed && hung)
            key.transform.SetPositionAndRotation(keyHostHook.Value.position, keyHostHook.Value.rotation);
        else if (atSpot)
        {
            // Resting on what is under it (the floor, or the top of the prop the marker sits on). D1.6: a dressed key's root
            // is the surface point itself (the cube's centre sits 6 cm up).
            key.transform.localPosition = new Vector3(data.keyX, data.keyY + (dressed ? 0f : .06f), data.keyZ);
            key.transform.localRotation = Quaternion.Euler(0f, data.keyYaw, 0f);
        }
        else
        {
            var origin = new Vector3(data.coord.x * MapGrid.ChunkSize, 0f, data.coord.y * MapGrid.ChunkSize);
            // D1.6: a dressed key lies on the floor of its site cell and never spins.
            key.transform.localPosition = new Vector3((data.keySiteCell.x + .5f) * cs, dressed ? 0f : 1.05f, (data.keySiteCell.y + .5f) * cs) - origin;
        }
        chunk.keys.Add((key, data.ownZone.id, !atSpot && !dressed));
    }

    /// <summary>
    /// A key's object under a chunk (zone keys and the floor key alike): the glowing cube (0.32 x 0.12 x 0.12 m), with
    /// its box as the look+E trigger (registered for <paramref name="zone"/>) while KeyPickupNeedsE is on, else no
    /// collider. The caller places it and lists it in chunk.keys.
    /// </summary>
    GameObject NewKey(BuiltChunk chunk, GridCoord zone, string name, out bool dressed, bool hung = false)
    {
        // D1.6 (contract M3): the visual kit's key when it is there (NewDressedKey; dressed = true), else the cube below.
        var kit = NewDressedKey(chunk, zone, name, hung);
        dressed = kit != null;
        if (dressed) return kit;
        var key = GameObject.CreatePrimitive(PrimitiveType.Cube);
        key.name = name;
        var keyCollider = key.GetComponent<Collider>();
        if (KeyPickupNeedsE)
        {
            // Look+E: the cube's own box (the key's envelope) as a trigger, for KeyOnRay only.
            keyCollider.isTrigger = true;
            keyByCollider[keyCollider] = zone;
        }
        else
        {
            // Out of the physics scene at once: a deferred Destroy would leave it for this frame's queries.
            keyCollider.enabled = false;
            Kill(keyCollider);
        }
        key.transform.SetParent(chunk.root.transform, false);
        key.transform.localScale = new Vector3(.32f, .12f, .12f);
        key.GetComponent<Renderer>().sharedMaterial = keyGlow;
        return key;
    }

    /// <summary>
    /// M3: the floor key ("Key · floor"), lying still at the station's KeySpot (<paramref name="at"/> 6 cm up, turned as
    /// the marker says), in the chunk that holds the KeySpot's cell. Taken like every key (KeyOnRay, Use, the touch key
    /// tap, TakeKeyByE): held as FloorKeyZone. D1.6's key model replaces the cube through NewKey.
    /// </summary>
    void SpawnFloorKey(BuiltChunk chunk, Vector3 at, Quaternion turn)
    {
        // D1.6 (contract M3): the station's KeySpot is the key's place on its hook (station-look-v1 C-2, plan P06), so a dressed
        // floor key hangs there: its root is the hook point in the KeySpot's frame (+Z = the room). The cube's centre sits 6 cm up.
        var key = NewKey(chunk, FloorKeyZone, FloorKeyName, out var dressed, true);
        key.transform.SetPositionAndRotation(dressed ? at : at + Vector3.up * .06f, turn);
        chunk.keys.Add((key, FloorKeyZone, false));
    }

    /// <summary>The floor key's object name (M3), for tools and tests.</summary>
    public const string FloorKeyName = "Key · floor";

    // D1.6 (contract M3): the dressed key, for zone keys and the floor key alike (NewKey). An unscaled empty named as the cube
    // would be ("Key · zone {id}", "Key · floor") holds ring, key and tag (FrontRoomsKeyAssembly; identity
    // FrontRoomsKeyIdentity.Stub(zone), the key HUD's StubIdentity hash, FloorKeyZone included). It lies flat with its root
    // on the surface point, or hangs from a key host's hook (hung). No key spins and nothing glows. The look+E trigger is on
    // the root itself (KeyOnRay, TakeKeyByE and CollectKeys read the GameObject's own collider). Null: no kit (or edit
    // mode), the cube.
    GameObject NewDressedKey(BuiltChunk chunk, GridCoord zone, string name, bool hung)
    {
        if (!FrontRoomsInteractableKit.KeyDressWanted) return null;
        var root = new GameObject(name).transform;
        root.SetParent(chunk.root.transform, false);
        FrontRoomsKeyAssembly assembly = null;
        try
        {
            assembly = FrontRoomsInteractableKit.DressKey(root, FrontRoomsKeyIdentity.Stub(zone),
                hung ? FrontRoomsInteractableKit.KeyPlacement.HostHung : FrontRoomsInteractableKit.KeyPlacement.Floor, KeyPickupNeedsE);
        }
        catch (Exception e) { Debug.LogException(e); }
        if (assembly == null)
        {
            Kill(root.gameObject);
            return null;
        }
        if (assembly.Trigger != null) keyByCollider[assembly.Trigger] = zone;
        return root.gameObject;
    }

    // D1.6 (contract M3): a module key spot's host (the key rack, a cabinet, a hook) is part of the room, so it is built on
    // every build of the chunk, the key held or not (BuildChunk calls this before the key); SpawnKey hangs a dressed key
    // from its hook. keyHostHook: that hook's world pose for the chunk being built, or null (no host, no kit, edit mode).
    Pose? keyHostHook;

    void SpawnKeyHost(BuiltChunk chunk, MapChunk data)
    {
        keyHostHook = null;
        if (!data.hasKey || !data.keySpot || string.IsNullOrEmpty(data.keyHost) || KeyRoomInStartArea(data) || IsReserved(data.keyCell)) return;
        try
        {
            if (FrontRoomsInteractableKit.DressKeyHost(chunk.root.transform, data.keyHost, new Vector3(data.keyX, 0f, data.keyZ), data.keyYaw, out var hook) != null)
                keyHostHook = hook;
        }
        catch (Exception e) { Debug.LogException(e); }
    }

    /// <summary>True when the room the key spot came from (the topmost room on the key's site cell) overlaps the start area, or holds X.</summary>
    bool KeyRoomInStartArea(MapChunk data)
    {
        if (!hasStartArea && !hasExitArea) return false;
        var o = data.Origin;
        int si = data.keySiteCell.x - o.x, sj = data.keySiteCell.y - o.y;
        for (var r = data.rooms.Length - 1; r >= 0; r--)
            if (data.rooms[r].Contains(si, sj)) return RoomInReserved(data, data.rooms[r]);
        return false;
    }

    /// <summary>The Relay entries of the chunk's intact module rooms (outside the start area and X), on the floor, in world space.</summary>
    void RegisterRelayEntries(BuiltChunk chunk, MapChunk data)
    {
        var cs = MapGrid.CellSize;
        for (var r = 0; r < data.rooms.Length; r++)
        {
            var module = data.ModuleOf(r);
            if (module == null || module.markers == null || !data.RoomIntact(r) || RoomInReserved(data, data.rooms[r])) continue;
            var room = data.rooms[r];
            foreach (var mk in module.markers)
            {
                if (mk.kind != ModuleMarkerKind.RelayEntry) continue;
                var local = new Vector3(room.x * cs + mk.x, 0f, room.y * cs + mk.z);
                chunk.relayEntries.Add((chunk.root.transform.TransformPoint(local), string.IsNullOrEmpty(mk.tag) ? null : mk.tag));
            }
        }
    }

    /// <summary>
    /// A lamp's temperament at a cell, as BuildFixture decides it: a mode set
    /// at run time (SetLampMode), else the module's lamp where a module sets
    /// one, else the Auto roll at the odds of the tier its chunk was generated
    /// at. Pure: valid for cells not built, and it never generates a chunk; for
    /// one the cache does not hold it predicts with Auto lamps at the tier the
    /// chunk will take (Cache.TierOf: its first generation's, kept through a
    /// revisit shift, else the current GenerationTier). Off where there is no
    /// lamp: a module's Off (no fixture, so SetLampMode cannot reach it), the
    /// start area, or X.
    /// </summary>
    public ModuleLamp LampModeOf(GridCoord cell) => LampModeOf(cell, true);

    ModuleLamp LampModeOf(GridCoord cell, bool withSet)
    {
        if (Cache == null || IsReserved(cell)) return ModuleLamp.Off;
        var lamp = ModuleLamp.Auto;
        var tier = Cache.TierOf(MapGrid.ChunkOf(cell));
        if (Cache.TryGetGenerated(MapGrid.ChunkOf(cell), out var data))
        {
            var o = data.Origin;
            lamp = data.lamp[MapGrid.LocalIndex(cell.x - o.x, cell.y - o.y)];
            tier = data.tier;
        }
        if (lamp == ModuleLamp.Off) return ModuleLamp.Off;
        if (withSet && lampModes.TryGetValue(cell, out var set)) return set;
        if (lamp != ModuleLamp.Auto) return lamp;
        var rng = MapHash.Hash(Cache.Generator.Seed, cell.x, cell.y, 211) | 1u;
        // Modes 0..4 (steady, stutter, failing, dead, dim) are ModuleLamp.Steady..Dim.
        var rolled = (tierRules ?? new FrontRoomsTierRules()).At(tier).LampMode(Rand(ref rng));
        if (FrontRoomsTransitionLightLead.On && FrontRoomsTransitionLightLead.Borders)
            rolled = FrontRoomsTransitionLightLead.BorderMode(rolled, Cache.ZoneOf(cell).theme, BorderThemes(cell, borderKinds), borderKinds);
        return (ModuleLamp)(rolled + 1);
    }

    // ---------- Lamp overrides (interface v1: the phosphor ink, the Relay warning) ----------
    // Agreed 2026-10-03 by the map chat, the wallpaper-print chat and 系统设计
    // (RELAY_PURSUIT_REDESIGN.md §7.4b; 20_level_design_phosphor.md §11.1 Step 1).
    // Everything here works on the lamps' logical level (f.level), which the
    // light, the lens, the hum and the tools all follow. With no override
    // active, both tick paths are bit-identical to before.

    /// <summary>What an override is for: Dip (first-dark dips, the chase wave), Sag (a power sag), Warn (the Relay warning's stage-1 bursts).</summary>
    public enum LampFx { Dip, Sag, Warn }

    /// <summary>
    /// How an override comes and goes: seconds to reach its multiplier, and to come back to 1. With a tail (M3,
    /// seconds, 0: none) the release is fast at first and eases out like a ballast recovering, exp(-u / tail)
    /// scaled to end at 1 exactly on time; with none it is linear. Reduce flashing always releases linearly.
    /// </summary>
    public readonly struct LampEnvelope
    {
        public readonly float attack, release, tail;
        public LampEnvelope(float attack, float release) { this.attack = attack; this.release = release; tail = 0f; }
        public LampEnvelope(float attack, float release, float tail) { this.attack = attack; this.release = release; this.tail = tail; }
    }

    /// <summary>Each kind's envelope when the caller passes none: Dip 0.3 / 1.2 s, Sag 0.6 / 1.5 s, Warn 0.08 / 0.2 s.</summary>
    public static LampEnvelope DefaultEnvelope(LampFx kind) => kind switch
    {
        LampFx.Sag => new LampEnvelope(.6f, 1.5f),
        LampFx.Warn => new LampEnvelope(.08f, .2f),
        _ => new LampEnvelope(.3f, 1.2f),
    };

    /// <summary>With Reduce flashing on, no override attacks or releases faster than this.</summary>
    public const float ReducedFlashingEnvelopeSeconds = .5f;

    /// <summary>LampLevel / LampBaseLevel for a cell with no built lamp (not built, the start area, a module's Off).</summary>
    public const float NoLamp = -1f;

    /// <summary>A lamp's logical level crossed 0.15, 0.55 or 0.8 (the ink gate's edges, R10b's "invisible" line), by at least 0.02 so a shimmer at a line does not chatter: its cell and the new level. Never per frame.</summary>
    public event Action<GridCoord, float> FixtureChanged;
    /// <summary>An override started on a built lamp: its cell and the light's position, so the fixture can buzz or tick.</summary>
    public event Action<GridCoord, Vector3> LampDipped;
    /// <summary>
    /// An override started on a built lamp, as LampDipped (right after it, from the same call), with its kind,
    /// so the sound layer can tell the Relay warning's stage-1 bursts (Warn) from first-dark dips and the chase
    /// wave (Dip) and power sags (Sag). Relay v2's V0 hook for the sound line (AUDIO_CONTRACT draft).
    /// </summary>
    public event Action<GridCoord, Vector3, LampFx> LampOverrideStarted;

    sealed class LampOverride
    {
        public int id;
        public GridCoord cell;
        public LampFx kind;
        public float multiplier, attack, hold, release, tail, age, weight;
        // Releasing (its hold ran out, or RemoveLampOverride): back to 1 from releaseFrom over the release.
        public bool releasing;
        public float releaseFrom, releaseAge;
    }

    /// <summary>A built lamp's logical level this frame, overrides included (0..1); NoLamp where there is none.</summary>
    public float LampLevel(GridCoord cell) => fixtureByCell.TryGetValue(cell, out var f) ? f.level : NoLamp;

    /// <summary>Tools and tests (V1): the _FR_LampDip a built lamp's lens carries now, read back from its renderer; NoLamp without one.</summary>
    public float LensDipForTools(GridCoord cell)
    {
        if (!fixtureByCell.TryGetValue(cell, out var f)) return NoLamp;
        var read = new MaterialPropertyBlock();
        f.panel.GetPropertyBlock(read);
        return read.GetFloat(LampDipId);
    }

    /// <summary>A built lamp's logical level this frame before any override (its own temperament); NoLamp where there is none.</summary>
    public float LampBaseLevel(GridCoord cell) => fixtureByCell.TryGetValue(cell, out var f) ? f.baseLevel : NoLamp;

    /// <summary>The overrides active now (tools and tests).</summary>
    public int LampOverrideCount => lampOverrides.Count;

    /// <summary>
    /// Set a lamp's temperament for good (Auto goes back to the map's own):
    /// the kill (Off: a dark lens, the troffer still there) or a promotion
    /// (e.g. Failing). Kept by cell, so it survives a chunk drop, a rebuild
    /// and a revisit shift; applied live to a built lamp, which starts a fresh
    /// cycle of its new mode. Nothing in the start area or X, and nothing where a
    /// module took the lamp out (there is no fixture to change).
    /// </summary>
    public void SetLampMode(GridCoord cell, ModuleLamp mode)
    {
        if (IsReserved(cell)) return;
        if (mode == ModuleLamp.Auto) lampModes.Remove(cell);
        else lampModes[cell] = mode;
        if (!fixtureByCell.TryGetValue(cell, out var f)) return;
        var next = (int)(mode == ModuleLamp.Auto ? LampModeOf(cell, false) : mode) - 1;
        if (next == f.mode) return;
        f.mode = next;
        // Level() reads these timers: a promoted lamp must not inherit the old mode's (a Steady lamp
        // turned Dead would blink at once on its old build-time event).
        f.eventEnd = f.clock;
        f.nextEvent = f.clock + 2f + Rand(ref f.rng) * 14f;
        // Nor the old mode's stutter, dropout or strike. The flash limiter starts its zigzag again from the new
        // level (the switch itself is not counted) but keeps the changes it counted (lastStrike likewise).
        f.gain = 1f;
        f.episodeDips = 0;
        f.dropping = false;
        f.dropStart = f.dropEnd = float.NegativeInfinity;
        f.strikeShown = false;
        if (f.flash != null) f.flash.begun = false;
    }

    /// <summary>
    /// M2 (RELAY §9.5): while a warning plays in the player's room, its Dead lamps strike no blink (held dark;
    /// FrontRoomsWarnLamps holds its room set from stage 1 and lets go at stage 0). Kept by cell, so a rebuilt or
    /// shifted chunk keeps it; a cell need not be built. Only Dead lamps read it: every other mode is unchanged.
    /// </summary>
    public void SetAmbientHold(GridCoord cell, bool hold)
    {
        if (hold) ambientHolds.Add(cell);
        else ambientHolds.Remove(cell);
        if (fixtureByCell.TryGetValue(cell, out var f)) f.ambientHold = hold;
    }

    /// <summary>SetAmbientHold for each of these cells.</summary>
    public void SetAmbientHold(IReadOnlyList<GridCoord> cells, bool hold)
    {
        if (cells == null) return;
        for (var i = 0; i < cells.Count; i++) SetAmbientHold(cells[i], hold);
    }

    /// <summary>Whether a cell's Dead strikes are held dark now (SetAmbientHold).</summary>
    public bool AmbientHeld(GridCoord cell) => ambientHolds.Contains(cell);

    /// <summary>The cells held now (tools and tests).</summary>
    public int AmbientHoldCount => ambientHolds.Count;

    /// <summary>
    /// M1: after a look hold's crossfade back, the lamp stays at its own mode's calm level at least this long, and
    /// then until its mode is between events, before the hold lets go of it; so the first stutter or dropout it
    /// plays again never shares a second with the crossfade.
    /// </summary>
    public const float LampHoldQuiet = .7f;

    /// <summary>
    /// M1 (lamp check 10_lamp_look_check.md; FrontRoomsWarnLamps holds its room's Stutter and Failing lamps): hold a
    /// lamp's look at <paramref name="look"/> (Steady, or Dim), crossfaded over <paramref name="seconds"/> (with
    /// Reduce flashing at least ReducedFlashingEnvelopeSeconds) from the level it shows now: no step. Its mode
    /// (LampModeOf) is unchanged and keeps running unseen. Kept by cell, so a rebuilt or shifted chunk keeps it; a
    /// lamp not built yet is held at once. Only Stutter and Failing lamps read it: every other mode is unchanged.
    /// Calling it again cancels a pending let-go and goes on from where the crossfade is (the look stays the first one).
    /// </summary>
    public void SetLampHold(GridCoord cell, ModuleLamp look, float seconds)
    {
        if (IsReserved(cell)) return;
        var inSeconds = Mathf.Max(seconds, 1e-3f);
        if (FrontRoomsSettings.ReduceFlashing) inSeconds = Mathf.Max(inSeconds, ReducedFlashingEnvelopeSeconds);
        if (!lampHolds.TryGetValue(cell, out var h))
        {
            h = new LampHold { cell = cell, look = look == ModuleLamp.Dim ? ModuleLamp.Dim : ModuleLamp.Steady, weight = 1f, source = 1f };
            if (fixtureByCell.TryGetValue(cell, out var f))
            {
                h.weight = 0f;
                h.source = 0f;
                h.from = f.baseLevel;
                f.hold = h;
            }
            lampHolds[cell] = h;
        }
        h.inSeconds = inSeconds;
        h.releasing = false;
        h.delay = 0f;
        h.quiet = 0f;
    }

    /// <summary>
    /// M1: let go of a look hold: after <paramref name="delay"/> seconds (still held), crossfade back over
    /// <paramref name="seconds"/> (at least 1 s) to the lamp's own mode at its calm level; it plays its own events
    /// again once LampHoldQuiet has passed and its mode is between events. A hold already letting go keeps its
    /// schedule. Unknown cells do nothing.
    /// </summary>
    public void ReleaseLampHold(GridCoord cell, float delay, float seconds)
    {
        if (!lampHolds.TryGetValue(cell, out var h) || h.releasing) return;
        h.releasing = true;
        h.delay = Mathf.Max(0f, delay);
        h.outSeconds = Mathf.Max(seconds, 1f);
        h.quiet = 0f;
    }

    /// <summary>M1: whether a cell has a look hold (held, or letting go).</summary>
    public bool LampHeld(GridCoord cell) => lampHolds.ContainsKey(cell);

    /// <summary>M1: how far a cell's look has gone to its hold (0..1; 0 with none).</summary>
    public float LampHoldWeight(GridCoord cell) => lampHolds.TryGetValue(cell, out var h) ? h.weight : 0f;

    /// <summary>M1: the look holds kept now (tools and tests).</summary>
    public int LampHoldCount => lampHolds.Count;

    /// <summary>
    /// Tools and tests (M1): a cell's look hold. <paramref name="weight"/> is how far it has gone to the look,
    /// <paramref name="source"/> how far its starting point has gone over to the mode's calm level, and
    /// <paramref name="releasing"/> whether it is letting go (<paramref name="delay"/> left before the crossfade
    /// back starts). False with none.
    /// </summary>
    public bool LampHoldForTools(GridCoord cell, out float weight, out float source, out bool releasing, out float delay)
    {
        weight = source = delay = 0f;
        releasing = false;
        if (!lampHolds.TryGetValue(cell, out var h)) return false;
        weight = h.weight;
        source = h.source;
        releasing = h.releasing;
        delay = h.delay;
        return true;
    }

    // M1: every hold, built or not, on toward its look, or (letting go, after its delay) back; one back at its own
    // level for LampHoldQuiet with its mode between events is dropped.
    void TickLampHolds(float dt)
    {
        if (lampHolds.Count == 0 || dt <= 0f) return;
        lampHoldsDone.Clear();
        foreach (var h in lampHolds.Values)
        {
            if (!h.releasing || h.delay > 0f)
            {
                if (h.releasing) h.delay -= dt;
                h.weight = Mathf.Min(1f, h.weight + dt / h.inSeconds);
                if (h.weight >= 1f) h.source = 1f;
                continue;
            }
            h.weight = Mathf.Max(0f, h.weight - dt / h.outSeconds);
            h.source = Mathf.Min(1f, h.source + dt / h.outSeconds);
            if (h.weight > 0f || h.source < 1f) continue;
            h.quiet += dt;
            if (h.quiet < LampHoldQuiet) continue;
            if (fixtureByCell.TryGetValue(h.cell, out var f) && !OwnCalm(f)) continue;
            lampHoldsDone.Add(h.cell);
        }
        foreach (var c in lampHoldsDone)
        {
            lampHolds.Remove(c);
            if (fixtureByCell.TryGetValue(c, out var f)) f.hold = null;
        }
    }

    /// <summary>
    /// Tools and tests (M2): a built lamp's flicker this frame. <paramref name="gain"/> is the ambient dip on its
    /// mode's level (1: none; Stutter's dips and sag, Failing's dropout); <paramref name="active"/> is a Stutter
    /// episode playing, a Failing dropout held, or a Dead strike lit; <paramref name="held"/> counts the frames
    /// the flash limiter has held the lamp back. False where there is no lamp.
    /// </summary>
    public bool LampAmbientForTools(GridCoord cell, out float gain, out bool active, out int held)
    {
        gain = 1f;
        active = false;
        held = 0;
        if (!fixtureByCell.TryGetValue(cell, out var f)) return false;
        gain = f.gain;
        active = f.mode == 1 ? f.episodeDips > 0 : f.mode == 2 ? f.dropping : f.mode == 3 && f.baseLevel > 0f;
        held = f.flash != null ? f.flash.held : 0;
        return true;
    }

    /// <summary>Tools and tests: a built lamp's own clock and phase (its temperament's time is clock + phase). False where there is no lamp.</summary>
    public bool LampClockForTools(GridCoord cell, out float clock, out float phase)
    {
        clock = phase = 0f;
        if (!fixtureByCell.TryGetValue(cell, out var f)) return false;
        clock = f.clock;
        phase = f.phase;
        return true;
    }

    /// <summary>
    /// Tools and tests (M2): run a level sequence, one per frame <paramref name="dt"/> apart, through the flash
    /// limiter as a fresh lamp would (no mode keeps it busy now, so this is how its hold is checked); the levels
    /// it gives out go to <paramref name="output"/>. Returns the frames it held back.
    /// </summary>
    public static int LimitFlashesForTools(IReadOnlyList<float> levels, float dt, List<float> output)
    {
        var f = new Fixture();
        output.Clear();
        for (var i = 0; i < levels.Count; i++)
        {
            f.clock += dt;
            output.Add(Limit(f, levels[i]));
        }
        return f.flash != null ? f.flash.held : 0;
    }

    /// <summary>
    /// A timed dip on one lamp: it eases to <paramref name="multiplier"/> of
    /// its own level over the envelope's attack, holds for
    /// <paramref name="hold"/> seconds (PositiveInfinity: until
    /// RemoveLampOverride), and comes back over the release. Overlapping
    /// overrides take the lowest multiplier, never the product. With Reduce
    /// flashing on, attack and release are at least 0.5 s. The cell need not
    /// be built (a lamp built during the override shows it); LampDipped fires
    /// for a built one. No delay argument: callers schedule the call, and keep
    /// any one lamp under 3 changes a second. Returns a handle (never 0).
    /// </summary>
    public int SetLampOverride(GridCoord cell, LampFx kind, float multiplier, float hold, LampEnvelope? envelope = null)
    {
        var e = envelope ?? DefaultEnvelope(kind);
        var attack = Mathf.Max(e.attack, 1e-3f);
        var release = Mathf.Max(e.release, 1e-3f);
        var tail = Mathf.Max(0f, e.tail);
        if (FrontRoomsSettings.ReduceFlashing)
        {
            attack = Mathf.Max(attack, ReducedFlashingEnvelopeSeconds);
            release = Mathf.Max(release, ReducedFlashingEnvelopeSeconds);
            tail = 0f;
        }
        var o = new LampOverride
        {
            id = ++nextLampOverride, cell = cell, kind = kind, multiplier = Mathf.Clamp01(multiplier),
            attack = attack, hold = Mathf.Max(0f, hold), release = release, tail = tail,
        };
        lampOverrides.Add(o);
        if (!overridesByCell.TryGetValue(cell, out var list)) overridesByCell[cell] = list = new List<LampOverride>(2);
        list.Add(o);
        if (fixtureByCell.TryGetValue(cell, out var f))
        {
            LampDipped?.Invoke(cell, f.light.transform.position);
            LampOverrideStarted?.Invoke(cell, f.light.transform.position, kind);
        }
        return o.id;
    }

    /// <summary>End an override early: it releases from where it is over its release time. Unknown or finished handles do nothing.</summary>
    public void RemoveLampOverride(int handle)
    {
        foreach (var o in lampOverrides)
        {
            if (o.id != handle) continue;
            if (!o.releasing) { o.releasing = true; o.releaseFrom = o.weight; o.releaseAge = 0f; }
            return;
        }
    }

    // Age every override (built or not): attack, hold, release; drop the finished ones.
    void TickLampOverrides(float dt)
    {
        for (var i = lampOverrides.Count - 1; i >= 0; i--)
        {
            var o = lampOverrides[i];
            if (!o.releasing)
            {
                o.age += dt;
                if (o.age < o.attack) { o.weight = o.age / o.attack; continue; }
                if (o.age < o.attack + o.hold) { o.weight = 1f; continue; }
                o.releasing = true;
                o.releaseFrom = 1f;
                o.releaseAge = o.age - o.attack - o.hold;
            }
            else o.releaseAge += dt;
            o.weight = o.tail > 0f ? o.releaseFrom * TailRelease(o.releaseAge / o.release, o.release / o.tail)
                : o.releaseFrom * (1f - Mathf.Clamp01(o.releaseAge / o.release));
            if (o.releaseAge < o.release) continue;
            lampOverrides.RemoveAt(i);
            if (overridesByCell.TryGetValue(o.cell, out var list) && list.Remove(o) && list.Count == 0) overridesByCell.Remove(o.cell);
        }
    }

    // M3's release with a tail: x of the release gone (0..1), k = release / tail. 1 at x = 0, 0 exactly at x = 1.
    static float TailRelease(float x, float k)
    {
        var end = Mathf.Exp(-k);
        return (Mathf.Exp(-k * Mathf.Clamp01(x)) - end) / (1f - end);
    }

    // A lamp's level this frame: its temperament, times the lowest active
    // override multiplier (none: exactly Level(f)). Reports band crossings.
    float LampTick(Fixture f)
    {
        // M1: a held Stutter or Failing lamp shows its crossfade (every other mode, and every lamp with no hold: Level).
        f.baseLevel = f.hold != null && (f.mode == 1 || f.mode == 2) ? HeldLevel(f) : Level(f);
        var level = f.baseLevel;
        f.dip = 1f;
        if (overridesByCell.Count > 0 && overridesByCell.TryGetValue(f.cell, out var list))
        {
            var lowest = 1f;
            foreach (var o in list) lowest = Mathf.Min(lowest, 1f + (o.multiplier - 1f) * o.weight);
            level *= lowest;
            f.dip = lowest;
        }
        if (f.band < 0)
        {
            f.band = (sbyte)(level < LampBands[0] ? 0 : level < LampBands[1] ? 1 : level < LampBands[2] ? 2 : 3);
            return level;
        }
        // A line counts as crossed once the level is past it by the hysteresis, so a lamp's shimmer at a line does not chatter.
        var band = f.band;
        while (band < 3 && level >= LampBands[band] + LampBandHysteresis) band++;
        while (band > 0 && level < LampBands[band - 1] - LampBandHysteresis) band--;
        if (band == f.band) return level;
        f.band = band;
        FixtureChanged?.Invoke(f.cell, level);
        return level;
    }

    // FixtureChanged's lines (the ink gate's edges 0.15 / 0.55, R10b's "invisible" 0.8) and how far past one a level must go.
    static readonly float[] LampBands = { .15f, .55f, .8f };
    const float LampBandHysteresis = .02f;

    /// <summary>Tools and tests: drop a built chunk as streaming does (it comes back with RebuildChunk).</summary>
    public void DropChunkForTools(GridCoord coord) => Drop(coord);

    /// <summary>
    /// Tools and tests (C22): what the map holds for the floor it is on, by name: the built and failed chunks
    /// and the dropped ones it remembers, the lookups a build fills (doors by leaf and by edge, windows, shell
    /// colliders, keys, lamps by cell, moving doors), every record of the floor (open, Ajar, broken, unlocked and
    /// breaking doors, key turns, broken windows, glass breaks, lamp modes, overrides and holds, keys held and
    /// announced, the start keep-clears) and the furnishing queue and job. Right after NextFloor every count
    /// is 0 (the start keep-clears too: SetStartArea clears them).
    /// </summary>
    public void FloorStateForTools(Dictionary<string, int> into)
    {
        into.Clear();
        into["built"] = built.Count;
        into["failedChunks"] = failedChunks.Count;
        into["droppedAt"] = droppedAt.Count;
        into["doorByCollider"] = doorByCollider.Count;
        into["doorByEdge"] = doorByEdge.Count;
        into["windowByCollider"] = windowByCollider.Count;
        into["shellColliders"] = shellColliders.Count;
        into["keyByCollider"] = keyByCollider.Count;
        into["fixtureByCell"] = fixtureByCell.Count;
        into["movingDoors"] = movingDoors.Count;
        into["openDoors"] = openDoors.Count;
        into["ajarDoors"] = ajarDoors.Count;
        into["brokenDoors"] = brokenDoors.Count;
        into["unlockedDoors"] = unlockedDoors.Count;
        into["breakingDoors"] = breakingDoors.Count;
        into["keyTurns"] = keyTurns.Count;
        into["brokenWindows"] = brokenWindows.Count;
        into["glassBreaks"] = glassBreaks.Count;
        into["lampModes"] = lampModes.Count;
        into["lampOverrides"] = lampOverrides.Count;
        into["overridesByCell"] = overridesByCell.Count;
        into["ambientHolds"] = ambientHolds.Count;
        into["lampHolds"] = lampHolds.Count;
        into["keysHeld"] = keysHeld.Count;
        into["keysAnnounced"] = keysAnnounced.Count;
        into["startKeepClear"] = startKeepClear.Count;
        into["dressQueue"] = dressQueue.Count;
        into["dressJob"] = currentJob != null ? 1 : 0;
    }

    // ---------- Doors and keys for tools (the autopilot's door scenario, tests) ----------

    /// <summary>Tools and tests: the run's working copy of "doors need keys" (the profile asset is never changed).</summary>
    public bool DoorsNeedKeysForTools
    {
        get => doorsNeedKeys;
        set => doorsNeedKeys = value;
    }

    /// <summary>Tools and tests: hold a zone's key, as walking over it would (without the pickup's event).</summary>
    public void GiveKeyForTools(GridCoord zone) => keysHeld.Add(zone);

    /// <summary>
    /// Tools and tests: the nearest built door to a point that is shut, whole, not being broken and not held,
    /// outside the start area, with the point on its swing side (<paramref name="swingSide"/>; with A12 any E
    /// from there first turns the leaf away, so both are pushes) or the other face. Null when there is none.
    /// </summary>
    public Door NearestClosedDoorForTools(Vector3 near, bool swingSide)
    {
        Door best = null;
        var bestDistance = float.MaxValue;
        foreach (var door in doorByEdge.Values)
        {
            if (door.hinge == null || door.open || door.broken || IsBeingBroken(door) || IsHeldForKey(door)) continue;
            if (!IsBuilt(door.a) || !IsBuilt(door.b) || OnSwingSide(door, near) != swingSide) continue;
            var d = (door.position - near).sqrMagnitude;
            if (d < bestDistance) { bestDistance = d; best = door; }
        }
        return best;
    }

    /// <summary>Tools: the doors' own clock (TickDoors; it stops with DoorsPaused).</summary>
    public float DoorClockForTools => doorClock;

    /// <summary>Tools and tests: a key has unlocked this door (it stays unlocked from both sides).</summary>
    public bool IsUnlockedForTools(Door door) => door != null && unlockedDoors.Contains(door.edge);

    /// <summary>Tools and tests: give a door a rig (a fake that records the shots' calls), or take it away (null).</summary>
    public void SetDoorRigForTools(Door door, IFrontRoomsDoorRig rig)
    {
        if (door != null) door.rig = rig;
    }

    /// <summary>
    /// Tests: put a door back as it was built, shut, latched and locked again (no unlock, no hold, no
    /// break, no jolt, no rattle stamp), so one door can serve many cases.
    /// </summary>
    public void ResetDoorForTools(Door door)
    {
        if (door == null) return;
        if (doorByEdge.TryGetValue(door.edge, out var current)) door = current;
        unlockedDoors.Remove(door.edge);
        keyTurns.Remove(door.edge);
        openDoors.Remove(door.edge);
        ajarDoors.Remove(door.edge);
        brokenDoors.Remove(door.edge);
        keptBlows.Remove(door.edge);
        breakingDoors.Remove(door.edge);
        door.open = door.broken = door.pull = false;
        door.angle = door.crooked = door.wait = 0f;
        door.motion = DoorMotion.None;
        door.jolts.Clear();
        door.joltClock = -1f;
        door.rattledAt = door.brokenTriedAt = float.NegativeInfinity;
        movingDoors.Remove(door);
        PoseDoor(door);
        TouchPassageRevision();
    }

    /// <summary>
    /// Every Relay entry of the built map (module markers), with its tag (null for none). The Relay prefers them when it
    /// appears. A26: never one in the station (none can be there, the carve drops every room over it: a guard).
    /// </summary>
    public void RelayEntries(List<(Vector3 pos, string tag)> into)
    {
        into.Clear();
        foreach (var chunk in built.Values)
        {
            if (!hasStation) { into.AddRange(chunk.relayEntries); continue; }
            foreach (var entry in chunk.relayEntries)
                if (!InStation(CellOf(entry.pos))) into.Add(entry);
        }
    }

    /// <summary>Tools and tests in edit mode, where Update does not run: turn the spinning keys and take the ones the player stands on, as Update does.</summary>
    public void CollectKeysForTools() => CollectKeys();

    /// <summary>
    /// Tools and tests: the nearest zone key still lying in a built chunk, its zone, and the chunk that
    /// built it. Null when there is none.
    /// </summary>
    public Transform NearestKeyForTools(Vector3 near, out GridCoord zone, out GridCoord chunk)
    {
        Transform best = null;
        zone = chunk = default;
        var bestDistance = float.MaxValue;
        foreach (var pair in built)
            foreach (var k in pair.Value.keys)
            {
                if (k.go == null) continue;
                var d = (k.go.transform.position - near).sqrMagnitude;
                if (d >= bestDistance) continue;
                bestDistance = d;
                best = k.go.transform;
                zone = k.zone;
                chunk = pair.Key;
            }
        return best;
    }

    // Keys are taken by walking onto them (0.9 m, flat), except a look+E key's trigger, which only E takes.
    void CollectKeys()
    {
        var p = player.position;
        foreach (var chunk in built.Values)
            for (var i = chunk.keys.Count - 1; i >= 0; i--)
            {
                var (go, zone, spin) = chunk.keys[i];
                if (go == null) { chunk.keys.RemoveAt(i); continue; }
                if (spin) go.transform.Rotate(0f, 90f * Time.deltaTime, 0f, Space.World);
                if (keyByCollider.Count > 0 && go.TryGetComponent<Collider>(out var trigger) && keyByCollider.ContainsKey(trigger)) continue;
                var d = go.transform.position - p;
                if (new Vector2(d.x, d.z).sqrMagnitude > .9f * .9f) continue;
                TakeKey(chunk, i);
            }
    }

    // E on a look+E key's trigger: the key it belongs to is taken. False when the key is no longer lying in a built chunk.
    bool TakeKeyByE(Collider trigger)
    {
        foreach (var chunk in built.Values)
            for (var i = 0; i < chunk.keys.Count; i++)
                if (chunk.keys[i].go != null && chunk.keys[i].go == trigger.gameObject)
                {
                    TakeKey(chunk, i);
                    return true;
                }
        keyByCollider.Remove(trigger);
        return false;
    }

    /// <summary>
    /// A key is taken (plan §3.3): off the chunk's list (only listed keys spin), held at once (a rebuild
    /// in the next moment cannot respawn it, and its doors open), KeyPickupStarted, then the presenter. A
    /// presented key moves to the map's root and the presenter announces it (KeyArrived); otherwise it is
    /// gone and announced now: the instant path, KeyTaken in the same call as KeyPickupStarted.
    /// </summary>
    void TakeKey(BuiltChunk chunk, int index)
    {
        var (go, zone, _) = chunk.keys[index];
        chunk.keys.RemoveAt(index);
        keysHeld.Add(zone);
        // A look+E trigger is spent: no query finds a key that is already taken.
        if (keyByCollider.Count > 0 && go.TryGetComponent<Collider>(out var trigger) && keyByCollider.Remove(trigger)) trigger.enabled = false;
        KeyPickupStarted?.Invoke(zone, go.transform.position);
        // M6 (M3's take): the floor key taken is the station's key taken: StationKeyTaken at the KeySpot on the floor,
        // before the presenter (on the instant path in the same call as KeyTaken, just before it).
        if (zone == FloorKeyZone) TakeStationKey();
        if (KeyPresenter != null && KeyPresenter(zone, go.transform))
        {
            if (go != null) go.transform.SetParent(transform, true);
            return;
        }
        Kill(go);
        KeyArrived(zone);
    }

    // ---------- Materials ----------

    /// <summary>
    /// The map uses the game's URP surfaces (Resources/Surfaces): Level 0
    /// chevron paper, loop-pile carpet and 2'x4' ceiling grid; the Office
    /// zones use the office drywall, carpet tiles and 2'x2' grid. All are
    /// projected in world metres by FrontRooms/Surface. Run FrontRooms →
    /// Rendering → Set up URP, post and surfaces once if they are missing.
    /// </summary>
    // The shared troffer lens's emission, scaled for the map to the flat lens's mean luminance (see BuildMaterials).
    const float Level0LensEmissionScale = 1.5f;

    void BuildMaterials()
    {
        foreach (var material in ownedMaterials) Kill(material);
        ownedMaterials.Clear();
        // Level 0's lens is the shared troffer lens (the title corridor's), as a map-owned copy whose
        // emission is raised to the mean brightness of the flat lens it replaces: the lens map averages
        // 0.45, so the shared (2.2, 2.1, 1.8) reads about 2.6 times dimmer (audit 1.5). The shared asset
        // is never changed: the title rooms keep their tuning.
        Material level0Lens = null;
        var troffer = FrontRoomsSurfaces.TrofferLens;
        if (troffer != null && troffer.HasProperty("_EmissionColor"))
        {
            level0Lens = Own(new Material(troffer) { name = "Map / Level 0 lens" });
            level0Lens.SetColor("_EmissionColor", troffer.GetColor("_EmissionColor") * Level0LensEmissionScale);
        }
        if (level0Lens == null) level0Lens = Own(FrontRoomsSurfaces.Lit("Map / Level 0 lens", new Color(1f, .98f, .92f), .1f, 0f, new Color(1f, .96f, .84f) * 2.6f));
        level0 = new ThemeMaterials
        {
            wall = FrontRoomsSurfaces.Room(RoomRule.Lobby, FrontRoomsSurfaces.Slot.Wall),
            floor = FrontRoomsSurfaces.Room(RoomRule.Lobby, FrontRoomsSurfaces.Slot.Floor),
            ceiling = FrontRoomsSurfaces.Room(RoomRule.Lobby, FrontRoomsSurfaces.Slot.Ceiling),
            lens = level0Lens,
            lampIntensity = 5f,
        };
        var officeLens = FrontRoomsSurfaces.OfficeLouver;
        office = new ThemeMaterials
        {
            wall = FrontRoomsSurfaces.Room(RoomRule.Office, FrontRoomsSurfaces.Slot.Wall),
            floor = FrontRoomsSurfaces.Room(RoomRule.Office, FrontRoomsSurfaces.Slot.Floor),
            ceiling = FrontRoomsSurfaces.Room(RoomRule.Office, FrontRoomsSurfaces.Slot.Ceiling),
            lens = officeLens != null && officeLens.HasProperty("_EmissionColor") ? officeLens : level0Lens,
            lampIntensity = 5.5f,
        };
        if (level0.wall == null) level0.wall = Own(FrontRoomsSurfaces.Lit("Map / wall fallback", new Color(.80f, .74f, .48f), .06f));
        if (level0.floor == null) level0.floor = Own(FrontRoomsSurfaces.Lit("Map / floor fallback", new Color(.55f, .49f, .30f), 0f));
        if (level0.ceiling == null) level0.ceiling = Own(FrontRoomsSurfaces.Lit("Map / ceiling fallback", new Color(.86f, .83f, .70f), .02f));
        if (office.wall == null) office.wall = level0.wall;
        if (office.floor == null) office.floor = level0.floor;
        if (office.ceiling == null) office.ceiling = level0.ceiling;
        // N1/V5 lightlead: colour and cool lens are an optional visual layer;
        // command-line flags keep the baseline available for comparison.
        if (FrontRoomsTransitionLightLead.On)
        {
            level0.lampColor = FrontRoomsTransitionLightLead.Level0Color;
            level0.lampIntensity = FrontRoomsTransitionLightLead.Level0Intensity;
            office.lampColor = FrontRoomsTransitionLightLead.OfficeColor;
            office.lampIntensity = FrontRoomsTransitionLightLead.OfficeIntensity;
            var coolLens = FrontRoomsTransitionLightLead.OfficeLens;
            if (coolLens != null && coolLens.HasProperty("_EmissionColor")) office.lens = coolLens;
        }
        trim = FrontRoomsSurfaces.CoveBase ?? Own(FrontRoomsSurfaces.Lit("Map test / frame", new Color(.55f, .50f, .36f), .2f));
        doorLeaf = FrontRoomsSurfaces.DoorVeneer ?? Own(FrontRoomsSurfaces.Lit("Map test / door", new Color(.72f, .66f, .50f), .25f));
        // Lit() returns a material from FrontRoomsSurfaces' static cache, shared with the next map after R or a caught
        // restart. Own a private copy: Release() destroys owned materials after the next map has taken the cached one.
        keyGlow = Own(new Material(FrontRoomsSurfaces.Lit("Map test / key", new Color(.96f, .87f, .23f), .4f, 0f, new Color(.96f, .87f, .23f) * .8f)));
        glass = Own(TransparentGlass("Map test / glass", new Color(.75f, .85f, .88f, .28f)));
    }

    static Material TransparentGlass(string name, Color color)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
        m.SetColor("_BaseColor", color);
        m.SetFloat("_Smoothness", .9f);
        m.SetFloat("_Surface", 1f);      // transparent
        m.SetFloat("_Blend", 0f);        // alpha
        m.SetFloat("_AlphaClip", 0f);
        m.SetFloat("_ZWrite", 0f);
        m.SetFloat("_SrcBlend", (float)BlendMode.One);
        m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        m.SetOverrideTag("RenderType", "Transparent");
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        m.renderQueue = (int)RenderQueue.Transparent;
        return m;
    }
}
