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
    /// so a rebuild is identical; a revisit shift takes the tier of its time.
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
    /// when it stood on the swing side (the leaf came toward it) rather than
    /// the stop side (it burst the leaf away). Raised right after DoorBroken.
    /// </summary>
    public event Action<Door, Vector3, bool> DoorBrokenFrom;
    /// <summary>
    /// A pull: the player opens a door from the side its leaf swings into, or
    /// shuts one standing in the leaf's way. The door, and a clear standing
    /// point on the floor on the latch side, out of the leaf's sweep and
    /// inside the door's keep-clear strip. Raised before the leaf moves, and
    /// only when the swing would meet the body (a player already clear is not
    /// moved); an open from the swing side waits
    /// <see cref="DoorPullBeatSeconds"/> longer than a push either way, for
    /// the game to step the player there. A leaf that meets a body stops on it.
    /// </summary>
    public event Action<Door, Vector3> DoorPulled;
    /// <summary>A shut leaf landed in its frame and latched (the end of a shut swing): the door, at its centre.</summary>
    public event Action<Door, Vector3> DoorLatched;
    /// <summary>
    /// The lever: the moment a door is opened (Use, TryOpenDoor, a key's door after its delay), before
    /// the latch frees and the leaf moves (DoorMoved, Open.SwingStart later; longer on a pull). Carries
    /// the lock and handle point on the opener's side, so the handle and latch sound land before the swing.
    /// </summary>
    public event Action<Door, Vector3> DoorHandleTurned;
    /// <summary>The player picked up a zone key.</summary>
    public event Action<GridCoord> KeyTaken;
    /// <summary>Every frame E is held on glass: the window's position and how far it is to breaking (0-1).</summary>
    public event Action<Vector3, float> GlassHold;
    /// <summary>E was let go before the glass broke (or, in tap mode, a tap's credit ran out: the progress is kept).</summary>
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
    /// <summary>The player tried a door that needs this zone's key.</summary>
    public event Action<Vector3> DoorLocked;
    /// <summary>
    /// A held key opened a locked door (doors need keys), once per door: the
    /// door, the zone whose key opened it, and the lock's world point on the
    /// opener's side. Raised before the leaf swings; the open (the lever,
    /// then the leaf, to the door's swing side) starts after UnlockSwingDelay.
    /// </summary>
    public event Action<Door, GridCoord, Vector3> DoorUnlocked;
    /// <summary>Seconds from DoorUnlocked to the open starting (the key turning in the lock); the lever's Open.SwingStart follows. 0 opens at once.</summary>
    public float UnlockSwingDelay { get; set; }

    // ---------- Doors: single-acting (Red, 2026-10-02) ----------
    // Every door swings into one fixed side (a hash of its edge), against
    // stops on the other face, and latches when shut. The beats come from
    // FrontRoomsShotTimings (Open, Rattle, DoorBreak); these are the map's own.

    /// <summary>A pull's extra wait after the lever, before the leaf moves, for the game to step the player to DoorPulled's point.</summary>
    public const float DoorPullBeatSeconds = .25f;
    /// <summary>
    /// The game's pull step: the player walks to DoorPulled's clear spot at
    /// PullStepSpeed (m/s) for at most PullStepSeconds (the lever plus the
    /// pull beat, so they are clear before the leaf moves). A spot further
    /// than PullStepSpeed × PullStepSeconds is not stepped to.
    /// </summary>
    public const float PullStepSeconds = .35f, PullStepSpeed = 4f;
    /// <summary>A built door is a way through (PassageBetween Open) from this leaf angle: the opening left beside the leaf is 0.64 m, so a 0.6 m body fits.</summary>
    public const float DoorPassableDegrees = 70f;
    /// <summary>The Relay's break throws the leaf this far, past the 95° stop, before it bounces back to DoorBreak.BounceRestDeg.</summary>
    public const float DoorBreakThrowDegrees = 105f;
    /// <summary>The bounce from the throw to rest, after DoorBreak.ThrowSeconds. The hunter waits for both before walking on.</summary>
    public const float DoorBreakBounceSeconds = .18f;
    /// <summary>The most a hinge turns in one frame, whatever the frame time (FrontRoomsDoorSound reads a jump over 45° as a recycle snap).</summary>
    public const float DoorMaxStepDegrees = 40f;

    /// <summary>What lies between two side-by-side cells right now.</summary>
    public enum Passage { Open, Wall, ClosedDoor, Glass }

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
        // Near the start area (its light would reach through the stream rooms' walls): 1 north of the door line, 2 south of it.
        public byte startGroup;
        // WebGL (TickFixturesNearOnly): the light's position relative to its chunk
        // root (chunk roots only translate), what this map last set on the Light,
        // and the lens emission factor last written, so a far lamp costs no native
        // call per frame.
        public Vector3 offset;
        public bool lightOn = true;
        public LightShadows shadowMode = LightShadows.None;
        public float emissionWritten = -1f;
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
        /// +1 or -1: the side the leaf swings into, fixed for the door's edge
        /// (FixedSwing). With +1 an east edge's leaf goes west (into a) and a
        /// north edge's goes north (into b). The stops are on the other face.
        /// </summary>
        public float swing = 1f;
        /// <summary>The hinge angle in degrees: 0 shut and latched, 95 open on the stop, past it only in a break throw.</summary>
        public float angle;
        /// <summary>The last open or shut came from the swing side (a pull).</summary>
        public bool pull;

        // The swing in progress (TickDoors): what it is, the lever's wait, and its curve.
        internal DoorMotion motion;
        internal float wait, clock, from, to, seconds, shutPhase;
        internal bool bump;
        // The leaf child: where it hangs when whole, how crooked a broken one
        // hangs, the jolt playing (clock, size, spring) and the jolts to come.
        internal Vector3 leafHome;
        internal float crooked;
        internal float joltClock = -1f, joltMetres, joltDegrees, joltSpring;
        internal readonly List<(float at, float metres, float degrees, float spring)> jolts = new List<(float, float, float, float)>();
    }

    internal enum DoorMotion { None, Lever, Swing, Shut, Throw, Bounce }

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
    /// root's +Z), −1 from b. The impact is fixed once the pane has cracked.
    /// </summary>
    public struct GlassBreakRecord
    {
        public int stage;
        public int seed;
        public Vector2 impact;
        public Vector2 impactUV;
        public float rotation;
        public int side;
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
    readonly HashSet<long> brokenWindows = new HashSet<long>();
    // Window breaks by edge (stage, seed, impact): kept through drops, rebuilds and shifts.
    readonly Dictionary<long, GlassBreakRecord> glassBreaks = new Dictionary<long, GlassBreakRecord>();
    // The glass kit's component on each window root, when the visual chat's glass exists (found by reflection).
    static Type glassBreakable;
    static bool glassBreakableResolved;
    readonly HashSet<long> openDoors = new HashSet<long>();
    readonly HashSet<long> brokenDoors = new HashSet<long>();
    // Lamp modes set at run time (SetLampMode): kept by cell like brokenDoors, so a dropped,
    // rebuilt or shifted chunk comes back with them. Never in MapChunk.lamp (Shift regenerates it).
    readonly Dictionary<GridCoord, ModuleLamp> lampModes = new Dictionary<GridCoord, ModuleLamp>();
    // Built lamps by cell, and the active lamp overrides (all, and by cell).
    readonly Dictionary<GridCoord, Fixture> fixtureByCell = new Dictionary<GridCoord, Fixture>();
    readonly List<LampOverride> lampOverrides = new List<LampOverride>();
    readonly Dictionary<GridCoord, List<LampOverride>> overridesByCell = new Dictionary<GridCoord, List<LampOverride>>();
    int nextLampOverride;
    // Doors the Relay is breaking (by edge, so a rebuilt chunk's new Door reads the same).
    readonly HashSet<long> breakingDoors = new HashSet<long>();
    readonly Dictionary<Collider, Door> doorByCollider = new Dictionary<Collider, Door>();
    readonly Dictionary<long, Door> doorByEdge = new Dictionary<long, Door>();
    // Doors a key has opened (they stay unlocked), and those whose key is still turning (seconds left).
    readonly HashSet<long> unlockedDoors = new HashSet<long>();
    readonly List<(Door door, float left)> unlocking = new List<(Door, float)>();
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
    readonly List<GridCoord> scratch = new List<GridCoord>();
    readonly List<Door> movingDoors = new List<Door>();
    Transform player;
    MaterialPropertyBlock block;
    ThemeMaterials level0, office;
    Material trim, doorLeaf, glass, keyGlow;
    bool begun;

    static bool dressersResolved;
    static MethodInfo officeDress, officeDressOld, pileBuild;

    /// <summary>A room waiting to be furnished. Rooms are dressed one per frame after their chunk is built.</summary>
    struct DressJob
    {
        public BuiltChunk chunk;
        public MapChunk data;
        public int room;
    }
    readonly Queue<DressJob> dressQueue = new Queue<DressJob>();

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
        while (DressNext()) { }
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
        if (buildAllNow)
        {
            Stream(ChunkOf(StreamCenter), int.MaxValue);
            while (DressNext()) { }
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
    /// </summary>
    public bool ReadyAround(int rings)
    {
        if (!begun || player == null) return false;
        var center = ChunkOf(StreamCenter);
        for (var dy = -buildRadius; dy <= buildRadius; dy++)
        for (var dx = -buildRadius; dx <= buildRadius; dx++)
            if (!BuiltOrFailed(new GridCoord(center.x + dx, center.y + dy))) return false;
        foreach (var job in dressQueue)
        {
            var c = job.data.coord;
            if (Mathf.Abs(c.x - center.x) <= rings && Mathf.Abs(c.y - center.y) <= rings
                && built.TryGetValue(c, out var standing) && standing == job.chunk) return false;
        }
        return true;
    }

    /// <summary>Every chunk within the build radius of the streaming centre is built, and every queued room is furnished.</summary>
    public bool Settled
    {
        get
        {
            if (!begun || player == null || dressQueue.Count > 0) return false;
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
        startArea = cells;
        startDoorCell = doorCell;
        hasStartArea = cells.width > 0 && cells.height > 0;
    }

    /// <summary>
    /// Give the start area back to the map, once the stream rooms are gone.
    /// Only while StartAreaBuilt is false: a chunk built round the rooms keeps
    /// their hole and its walls until it is rebuilt.
    /// </summary>
    public void ClearStartArea() => hasStartArea = false;

    public bool HasStartArea => hasStartArea;

    public bool InStartArea(GridCoord cell) =>
        hasStartArea && cell.x >= startArea.xMin && cell.x < startArea.xMax && cell.y >= startArea.yMin && cell.y < startArea.yMax;

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

    /// <summary>The cell's chunk is built. Start-area cells never are: the stream rooms stand there.</summary>
    public bool IsBuilt(GridCoord cell) => !InStartArea(cell) && built.ContainsKey(MapGrid.ChunkOf(cell));

    /// <summary>The zone a cell belongs to. The start area reads as the zone its door opens onto (Standard Level 0, like the stream rooms), not the generated zone hidden under it.</summary>
    public ZoneInfo ZoneOf(GridCoord cell) => Cache.ZoneOf(InStartArea(cell) ? startDoorCell : cell);

    /// <summary>
    /// True for the map's own architecture: chunk walls, floors and ceilings,
    /// doors and glass. False for furniture and anything else the kits add.
    /// </summary>
    public bool IsArchitecture(Collider c) => c != null && (shellColliders.Contains(c) || doorByCollider.ContainsKey(c) || windowByCollider.ContainsKey(c));

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
        if (InStartArea(a) || InStartArea(b)) return float.PositiveInfinity;
        switch (Cache.Edge(a, b))
        {
            case EdgeKind.Open:
            case EdgeKind.Arch:
                return 0f;
            case EdgeKind.Door:
            {
                var id = EdgeId(a, b);
                if (brokenDoors.Contains(id)) return 0f;
                if (doorByEdge.TryGetValue(id, out var door))
                {
                    if (door.broken || door.angle >= DoorPassableDegrees) return 0f;
                    return door.open ? FrontRoomsMapPathField.AjarOpening : FrontRoomsMapPathField.ShutOpening;
                }
                return openDoors.Contains(id) ? FrontRoomsMapPathField.AjarOpening : FrontRoomsMapPathField.ShutOpening;
            }
            case EdgeKind.Window:
                return brokenWindows.Contains(EdgeId(a, b)) ? 0f : FrontRoomsMapPathField.ShutOpening;
            default:
                return float.PositiveInfinity;
        }
    }

    public bool HasKeyFor(GridCoord zone) => keysHeld.Contains(zone);

    void Update()
    {
        if (!begun || player == null) return;
        frameWork.Clear();
        workWatch.Restart();
        Stream(ChunkOf(StreamCenter), chunksPerFrame);
        // While the game streams round a focus (the player still in the
        // title's stream room, watching), a heavy chunk build and a room's
        // furniture never share a frame.
        if (StreamFocus == null || workWatch.Elapsed.TotalMilliseconds < FocusFrameBudgetMs) DressNext();
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
            // is rebuilt, so the change is never seen.
            if (droppedAt.TryGetValue(coord, out var left) && Time.time - left >= shiftAfterSeconds)
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
            var theme = Theme(zone.theme);
            var height = MapGrid.CeilingHeight(data.height[index]);
            var b = BlockOf(i, j, height);
            var cellCenter = new Vector3((i + .5f) * cs, 0f, (j + .5f) * cs);
            // The title's stream rooms stand in the start area: no map there.
            var reserved = InStartArea(cell);
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
            Material eastA = theme.wall, eastB = Theme(eastZone.theme).wall, northA = theme.wall, northB = Theme(northZone.theme).wall;
            var eastKind = StartAreaEdge(data.east[index], cell, east, false, ref eastHeight, ref eastA, ref eastB);
            var northKind = StartAreaEdge(data.north[index], cell, north, true, ref northHeight, ref northA, ref northB);
            // A wall's ends reach half a thickness past its corner, except into the start area.
            // The start area's side walls also stop on the door line, where the stream room's end wall closes the corner.
            var startSide = InStartArea(cell) != InStartArea(east);
            // N1/B0.2: a height-border wall face is assigned to its own block so the
            // grime band uses that side's ceiling height. -1 keeps the edge's block.
            var sideBlocks = FrontRoomsTransitionKit.B0 && !InStartArea(cell);
            var eastSide = sideBlocks && !InStartArea(east);
            var northSide = sideBlocks && !InStartArea(north);
            BuildEdge(chunk, eastKind, cell, east, new Vector3((i + 1) * cs, 0f, j * cs), Vector3.forward,
                eastHeight, eastA, eastB, BlockOf(i, j, eastHeight), Get, Solid, origin,
                !BothInStartArea(new GridCoord(cell.x, cell.y - 1), new GridCoord(cell.x + 1, cell.y - 1)),
                !BothInStartArea(new GridCoord(cell.x, cell.y + 1), new GridCoord(cell.x + 1, cell.y + 1)) && !(startSide && cell.y + 1 == startArea.yMax),
                eastSide ? BlockOf(i, j, height) : -1, eastSide ? BlockOf(i, j, MapGrid.CeilingHeight(eastZone.height)) : -1);
            BuildEdge(chunk, northKind, cell, north, new Vector3(i * cs, 0f, (j + 1) * cs), Vector3.right,
                northHeight, northA, northB, BlockOf(i, j, northHeight), Get, Solid, origin,
                !BothInStartArea(new GridCoord(cell.x - 1, cell.y), new GridCoord(cell.x - 1, cell.y + 1)),
                !BothInStartArea(new GridCoord(cell.x + 1, cell.y), new GridCoord(cell.x + 1, cell.y + 1)),
                northSide ? BlockOf(i, j, height) : -1, northSide ? BlockOf(i, j, MapGrid.CeilingHeight(northZone.height)) : -1);

            if (data.pillar[i + j * (n + 1)] && !TouchesStartArea(cell.x, cell.y, cell.x, cell.y))
            {
                // No column, cove or bulkhead reaches into the start area.
                var style = data.pillarStyle[i + j * (n + 1)];
                if (TouchesStartArea(cell.x, cell.y, cell.x + 2, cell.y)) style &= unchecked((byte)~MapChunk.ColumnBeamEast);
                if (TouchesStartArea(cell.x, cell.y, cell.x, cell.y + 2)) style &= unchecked((byte)~MapChunk.ColumnBeamNorth);
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

        if (data.hasKey && !keysHeld.Contains(data.ownZone.id))
        {
            // A key spot in a room the start area cuts (not furnished, maybe under the stream rooms): back to the site cell.
            var atSpot = data.keySpot && !KeyRoomInStartArea(data);
            if (!InStartArea(atSpot ? data.keyCell : data.keySiteCell)) SpawnKey(chunk, data, atSpot);
        }
        RegisterRelayEntries(chunk, data);
        AddZoneGrades(chunk, data);
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
        wallA = wallB = Theme(outside.theme).wall;
        return EdgeKind.Wall;
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
        Material wa = Theme(za.theme).wall, wb = Theme(zb.theme).wall;
        var kind = StartAreaEdge(Cache.Edge(a, b), a, b, northward, ref h, ref wa, ref wb);
        if (kind == EdgeKind.Open) return default;
        var startEdge = InStartArea(a) || InStartArea(b);
        var ca = HeightClass(startEdge ? h : ha);
        var cb = HeightClass(startEdge ? h : hb);
        return new FrontRoomsTransitionKit.PostPiece
        {
            present = true,
            finishA = ((long)wa.GetInstanceID() << 4) | (long)ca,
            finishB = ((long)wb.GetInstanceID() << 4) | (long)cb,
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
        bool mayExtendStart = true, bool mayExtendEnd = true, int blockA = -1, int blockB = -1)
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

        if (kind == EdgeKind.Wall) { Piece(0f, length, 0f, height, true, true); return; }

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

        if (kind == EdgeKind.Arch) return;

        // Frame: two jambs and a head in the trim colour.
        var trims = get(blockIndex, trim);
        var frame = ModuleUnits.TrimFace;
        var jambDepth = WallThickness + ModuleUnits.TrimProud * 2f;
        foreach (var s in new[] { -1f, 1f })
        {
            var jc = start + along * (c + s * (width * .5f + frame * .5f)) + Vector3.up * ((sill + openingTop) * .5f);
            trims.Box(jc, Abs(along * frame + across * jambDepth + Vector3.up * (openingTop - sill)), origin, 1f);
        }
        trims.Box(start + along * c + Vector3.up * (openingTop + frame * .5f), Abs(along * (width + frame * 2f) + across * jambDepth + Vector3.up * frame), origin, 1f);

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
                swing = FixedSwing(a, b), leafHome = leaf.transform.localPosition };
            // Rebuilt as it was left: open on its stop, or broken at rest and hanging crooked.
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
            if (door.angle > 0f) PoseDoor(door);
            chunk.doors.Add(door);
            doorByCollider[door.leaf] = door;
            doorByEdge[edge] = door;
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
                // A cracked pane resumes from the stage it reached.
                window.hold = StageFloor(window.stage);
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
    /// </summary>
    void ArchOpening(GridCoord a, bool alongX, out float width, out float center)
    {
        var edgeHash = MapHash.Hash(Cache.Generator.Seed, a.x * 2 + (alongX ? 1 : 0), a.y, 97);
        width = ModuleUnits.ArchMinWidth + ModuleUnits.ArchWidthSpread * MapHash.Unit(edgeHash);
        var margin = ModuleUnits.ArchCornerMargin + width * .5f;
        center = Mathf.Lerp(margin, MapGrid.CellSize - margin, MapHash.Unit(MapHash.Hash((int)edgeHash, 3, 7, 11)));
    }

    // The door swing hash's salt (MapHash uses 11-73, this map 97, 211, 223, 307).
    const int DoorSwingSalt = 89;

    /// <summary>
    /// The side a door on the edge between two neighbouring cells swings
    /// into, as Door.swing (+1 or -1): a hash of the seed and the edge, with
    /// no revision, so every build, rebuild and revisit shift agrees. Either
    /// side is equally likely, so pulls come as often as pushes.
    /// </summary>
    float FixedSwing(GridCoord a, GridCoord b)
    {
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
        if (Cache.Edge(lo, hi) == EdgeKind.Arch) ArchOpening(lo, !eastward, out _, out center);
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
        // The start area is walled off from the map (its door is the stream's).
        if (InStartArea(a) || InStartArea(b)) return Passage.Wall;
        switch (Cache.Edge(a, b))
        {
            case EdgeKind.Open:
            case EdgeKind.Arch:
                return Passage.Open;
            case EdgeKind.Door:
                var id = EdgeId(a, b);
                // A built door by its leaf, broken or not: swung far enough for a body to pass. A broken
                // leaf still falling, or resting on a body short of 70°, is no way through yet.
                if (doorByEdge.TryGetValue(id, out var door))
                    return door.angle >= DoorPassableDegrees ? Passage.Open : Passage.ClosedDoor;
                // Not built: as it was left.
                return brokenDoors.Contains(id) || openDoors.Contains(id) ? Passage.Open : Passage.ClosedDoor;
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
    /// Whichever side the Relay is on, the leaf goes to its swing side (the
    /// stops hold the other way): thrown past the stop in DoorBreak.ThrowSeconds,
    /// it bounces back to rest at DoorBreak.BounceRestDeg, hanging crooked.
    /// </summary>
    public void BreakDoor(Door door, Vector3 from)
    {
        if (door == null) return;
        // The chunk was rebuilt mid-break: the edge's new Door is the one to break.
        if (doorByEdge.TryGetValue(door.edge, out var current)) door = current;
        breakingDoors.Remove(door.edge);
        if (door.broken) return;
        var fromSwingSide = OnSwingSide(door, from);
        door.broken = true;
        door.open = true;
        brokenDoors.Add(door.edge);
        door.wait = 0f;
        door.motion = DoorMotion.Throw;
        door.from = door.angle;
        door.clock = 0f;
        if (!movingDoors.Contains(door)) movingDoors.Add(door);
        TouchPassageRevision();
        DoorBroken?.Invoke(door.position);
        DoorBrokenFrom?.Invoke(door, from, fromSwingSide);
    }

    /// <summary>
    /// Open a door without aiming at it (autopilot), standing in cell
    /// <paramref name="a"/>. Respects the key rule. True when this call
    /// started it opening (or its key turning); false when it is locked,
    /// already open or opening, being broken, or not built.
    /// </summary>
    public bool TryOpenDoor(GridCoord a, GridCoord b)
    {
        var door = DoorBetween(a, b);
        if (door == null || door.open || door.broken || IsBeingBroken(door)) return false;
        if (LockedHere(door) || door.motion == DoorMotion.Lever || Unlocking(door)) return false;
        var from = player != null && CellOf(player.position) == a ? player.position : CellCenter(a);
        if (!Unlock(door, from)) OpenDoor(door, from);
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
            kinds[k] = InStartArea(n) || InStartArea(cell) ? EdgeKind.Wall : Cache.Edge(cell, n);
        }
        return borderThemes;
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

    void TickFixtures(float dt)
    {
        TickLampOverrides(dt);
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
            var held = f.startGroup == 1 ? StartLampsNorth : f.startGroup == 2 ? StartLampsSouth : 1f;
            var fade = Mathf.Clamp01((lightRadius - d) / 3f) * held;
            var on = fade > 0f && f.level > .01f;
            if (f.light.enabled != on) f.light.enabled = on;
            if (on) f.light.intensity = f.baseIntensity * f.level * fade;
            var shadows = f.castsShadow && on && d < shadowRadius ? LightShadows.Soft : LightShadows.None;
            if (f.light.shadows != shadows) f.light.shadows = shadows;
            f.panel.GetPropertyBlock(block);
            block.SetColor("_EmissionColor", f.emission * Mathf.Max(.04f, f.level * held));
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
                var held = f.startGroup == 1 ? StartLampsNorth : f.startGroup == 2 ? StartLampsSouth : 1f;
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
        f.panel.SetPropertyBlock(block);
        f.emissionWritten = factor;
    }

    public void TickFixturesForTools(float dt) => TickFixtures(dt);

    /// <summary>
    /// Tools: every lamp's Light and lens against the lamp formula, read back
    /// from the components (lit within lightRadius with level above 0.01 and
    /// its start group not held, at baseIntensity × level × fade, soft shadows
    /// within shadowRadius when it casts; otherwise off). A lit lens carries
    /// exactly the current glow; an unlit one within <see cref="FarEmissionStep"/>
    /// of it. Returns the number of disagreements, with what they were.
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
            var held = f.startGroup == 1 ? StartLampsNorth : f.startGroup == 2 ? StartLampsSouth : 1f;
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
            else if (!on && Mathf.Abs(glow.maxColorComponent - expectedGlow.maxColorComponent) > FarEmissionStep * Mathf.Max(f.emission.maxColorComponent, 1e-6f) + 1e-4f) problem = "far glow " + glow + " expected about " + expectedGlow;
            if (on) lit++;
            if (problem == null) continue;
            mismatches++;
            if (problems != null && problems.Count < 12) problems.Add(f.light.transform.parent.name + " d " + d.ToString("0.0") + " level " + f.level.ToString("0.00") + ": " + problem);
        }
        return mismatches;
    }

    static float Level(Fixture f)
    {
        var t = f.clock + f.phase;
        // Reduce flashing (Settings): no on/off strobe. Clocks and the RNG advance exactly as before.
        var calm = FrontRoomsSettings.ReduceFlashing;
        switch (f.mode)
        {
            case 1:
                // Steady, then a short stutter at its own random interval.
                if (f.clock >= f.nextEvent && f.eventEnd <= f.clock)
                {
                    f.eventEnd = f.clock + .15f + Rand(ref f.rng) * .7f;
                    f.nextEvent = f.eventEnd + 5f + Rand(ref f.rng) * 15f;
                }
                if (f.clock < f.eventEnd) return calm ? .7f : Mathf.PerlinNoise(t * 23f, f.phase) > .45f ? .95f : .05f;
                return .97f + .03f * Mathf.Sin(t * 120f);
            case 2:
                // Failing ballast: never settles, drops out at its own rhythm.
                var wave = .5f + .5f * Mathf.Sin(t * 3.1f) * (.7f + .3f * Mathf.Sin(t * 1.27f));
                var dropout = Mathf.PerlinNoise(t * 1.4f, f.phase) > .72f ? (calm ? .7f : .05f) : 1f;
                return (.25f + .45f * wave) * dropout;
            case 3:
                // Dead tube that blinks once in a long while.
                if (f.clock >= f.nextEvent && f.eventEnd <= f.clock)
                {
                    f.eventEnd = f.clock + .06f + Rand(ref f.rng) * .12f;
                    f.nextEvent = f.eventEnd + 8f + Rand(ref f.rng) * 20f;
                }
                return f.clock < f.eventEnd && !calm ? .8f : 0f;
            case 4:
                return .42f + .03f * Mathf.Sin(t * 90f);
            case 5:
                // Killed at run time (SetLampMode Off): a dark lens, the troffer still there.
                return 0f;
            default:
                return .98f + .02f * Mathf.Sin(t * 110f);
        }
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
            var pile = assembly.GetType("FrontRoomsFurniturePile");
            if (pile != null && pileBuild == null)
                pileBuild = pile.GetMethod("Build", BindingFlags.Public | BindingFlags.Static, null,
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
            // A room cut by the start area is not one open space (and its props would land in the stream rooms).
            if (data.RoomIntact(r) && Cache.Generator.Uniform(data, data.rooms[r]) && !RoomInStartArea(data, data.rooms[r]))
                dressQueue.Enqueue(new DressJob { chunk = chunk, data = data, room = r });
    }

    /// <summary>Furnish the next queued room whose chunk is still standing. Returns false when the queue is empty.</summary>
    bool DressNext()
    {
        while (dressQueue.Count > 0)
        {
            var job = dressQueue.Dequeue();
            if (job.chunk.root == null || !built.TryGetValue(job.data.coord, out var current) || current != job.chunk) continue;
            stepWatch.Restart();
            // A room that gets nothing (no office, no pile, no module props) does not use up the frame.
            if (!Dress(job.chunk, job.data, job.room)) continue;
            NoteWork("room " + job.room + " of " + job.data.coord);
            return true;
        }
        return false;
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
                if (officeDress != null) officeDress.Invoke(null, new object[] { chunk.root.transform, floor, height, roomSeed, clear.ToArray(), obstacles.ToArray() });
                else if (officeDressOld != null) officeDressOld.Invoke(null, new object[] { chunk.root.transform, floor, height, roomSeed, clear.ToArray() });
                return officeDress != null || officeDressOld != null || worked;
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
                    pileBuild.Invoke(null, new object[] { chunk.root.transform, center, radius, height, roomSeed });
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
    /// by reflection, every kit model and sidecar (the furniture pile reads
    /// them all on its first pile) and the Office grade profile. Call once
    /// while nothing is on screen (the game does it in Awake).
    /// </summary>
    public static void Prewarm()
    {
        ResolveDressers();
        foreach (var name in FrontRoomsKitLibrary.AllNames()) FrontRoomsKitLibrary.GetInfo(name);
        Resources.Load<VolumeProfile>("Rendering/FrontRoomsPost_Office");
    }

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
        // First which props are left out, so a desk-top item can go with the prop it stands on, whatever their order.
        var count = module.props.Length;
        var rects = new Rect?[count];
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
        }
        for (var index = 0; index < count; index++)
        {
            // A raised item (on a desk, a cabinet) whose footprint is over a prop left out goes with it.
            var p = module.props[index];
            if (leftOut[index] != null || rects[index] == null || p.y <= .01f || p.y > ModuleUnits.RelayHeight) continue;
            for (var k = 0; k < count; k++)
                if (k != index && leftOut[k] != null && module.props[k].y <= .01f && rects[k].HasValue && rects[k].Value.Overlaps(rects[index].Value)) { leftOut[index] = "stood on a prop left out of"; break; }
        }
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
            var spawned = FrontRoomsKitLibrary.Spawn(p.kit, root, new Vector3(ox + p.x, p.y, oz + p.z), p.yaw, null, !p.noCollider, p.kit);
            if (spawned == null) continue;
            if (TagModuleProps)
            {
                var tag = spawned.AddComponent<FrontRoomsModulePropTag>();
                tag.module = module;
                tag.index = index;
                tag.roomOrigin = new Vector3(ox, 0f, oz);
            }
            // Wall pieces above head height leave the floor free.
            if (f == null || !onFloor) continue;
            footprints.Add(rect);
        }
        return footprints;
    }

    static float Distance(Rect r, Vector2 p)
    {
        var dx = Mathf.Max(r.xMin - p.x, 0f, p.x - r.xMax);
        var dy = Mathf.Max(r.yMin - p.y, 0f, p.y - r.yMax);
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>A kit asset's footprint about its pivot and its height: (min x, min z, max x, max z, height), or null.</summary>
    public static float[] KitFootprint(string kit)
    {
        var info = FrontRoomsKitLibrary.GetInfo(kit);
        if (info == null) return null;
        var f = info.Footprint;
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
    bool OfficeGraded(GridCoord cell) =>
        !(hasStartArea && cell.x >= startArea.xMin - 1 && cell.x <= startArea.xMax && cell.y >= startArea.yMin - 1 && cell.y <= startArea.yMax)
        && Cache.ZoneOf(cell).theme == ZoneTheme.Office;

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
                var kind = Cache.Edge(cell, cell + d);
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
        if (doorByCollider.TryGetValue(c, out var door))
        {
            // Broken, or the Relay is breaking it: nothing to do with it.
            if (door.broken || IsBeingBroken(door)) return null;
            if (door.open) return "E  ·  SHUT DOOR";
            return LockedHere(door) ? "LOCKED  ·  NEEDS THIS ZONE'S KEY" : "E  ·  OPEN DOOR";
        }
        if (windowByCollider.ContainsKey(c))
        {
            holdToUse = true;
            return "HOLD E  ·  BREAK GLASS";
        }
        return null;
    }

    public void Use(Collider c)
    {
        if (c == null || !doorByCollider.TryGetValue(c, out var door) || door.broken) return;
        // The Relay is breaking it: hands off (Describe shows no prompt).
        if (IsBeingBroken(door)) return;
        // The lever is down or the key is still turning: the door opens by itself.
        if (door.motion == DoorMotion.Lever || Unlocking(door)) return;
        if (!door.open && LockedHere(door))
        {
            DoorLocked?.Invoke(door.position);
            Rattle(door);
            return;
        }
        var from = player != null ? player.position : door.position;
        if (door.open) ShutDoor(door, from);
        else if (!Unlock(door, from)) OpenDoor(door, from);
    }

    bool Unlocking(Door door) => unlocking.Exists(u => u.door == door);

    /// <summary>
    /// The first time a key opens this door: raise DoorUnlocked. True when the
    /// leaf waits for UnlockSwingDelay (TickDoors opens it); false when the
    /// caller opens it now.
    /// </summary>
    bool Unlock(Door door, Vector3 from)
    {
        if (!doorsNeedKeys || !unlockedDoors.Add(door.edge)) return false;
        var zone = Cache.ZoneOf(CellOf(player != null ? player.position : from)).id;
        DoorUnlocked?.Invoke(door, zone, LockPoint(door, from));
        if (UnlockSwingDelay <= 0f) return false;
        unlocking.Add((door, UnlockSwingDelay));
        return true;
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
    const float PlayerClearance = ModuleUnits.PlayerRadius + .04f, RelayClearance = ModuleUnits.RelayRadius + .01f;
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

    /// <summary>True when a point stands on the side the door's leaf swings into (opening it from there is a pull); false on the stop side.</summary>
    public static bool OnSwingSide(Door door, Vector3 point) => door != null && DoorPlan(door, point).y > 0f;

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
    /// The one way a door opens (Use, TryOpenDoor, a key): the lever goes
    /// down, and Open.SwingStart later the leaf leaves the frame toward its
    /// swing side, easing out over Open.SwingSeconds onto the 95° stop with
    /// the Open overshoot. From the swing side it is a pull: DoorPulled now,
    /// and DoorPullBeatSeconds more before the leaf moves. A leaf caught
    /// mid-shut has no latch to free, so a push turns it back at once.
    /// </summary>
    void OpenDoor(Door door, Vector3 from)
    {
        door.open = true;
        openDoors.Add(door.edge);
        door.pull = OnSwingSide(door, from);
        door.wait = (door.angle <= LatchedDegrees ? FrontRoomsShotTimings.Open.SwingStart : 0f) + (door.pull ? DoorPullBeatSeconds : 0f);
        door.motion = DoorMotion.Lever;
        if (!movingDoors.Contains(door)) movingDoors.Add(door);
        TouchPassageRevision();
        DoorHandleTurned?.Invoke(door, LockPoint(door, from));
        if (door.pull)
        {
            // A step only when the opening leaf would meet the body.
            var clear = ClearOfSwing(door, from, PlayerClearance, door.angle);
            if ((clear - from).sqrMagnitude > 1e-6f) DoorPulled?.Invoke(door, clear);
        }
        if (door.wait <= 0f) BeginOpenSwing(door, 0f);
    }

    /// <summary>
    /// Shut a door from a point: the leaf turns back at once on today's
    /// smoothstep over Open.SwingSeconds (the close Foley is timed on it) and
    /// lands in the frame, latched; the stops never let it through. Standing
    /// in its way on the swing side raises DoorPulled, and the leaf rests on
    /// the body until it is clear.
    /// </summary>
    void ShutDoor(Door door, Vector3 from)
    {
        door.open = false;
        openDoors.Remove(door.edge);
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
    /// on its hinge toward the swing side (the stops hold the other way), then
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

    // The leaf child: at home; once broken, hanging crooked (turned in its own
    // plane about the top of its hinge edge, so the bottom kicks out from the
    // jamb and nothing goes into the floor); plus any jolt.
    static void PoseLeaf(Door door)
    {
        if (door.leaf == null) return;
        var position = door.leafHome;
        var rotation = Quaternion.identity;
        if (door.crooked > 0f)
        {
            // The leaf stands on the floor, so its top is twice its centre's height.
            var top = new Vector3(0f, door.leafHome.y * 2f, 0f);
            var tilt = Quaternion.AngleAxis(-door.crooked, Vector3.right);
            position = top + tilt * (position - top);
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
            if (window.pane == null || !IsBuilt(window.a) || !IsBuilt(window.b) || InStartArea(window.a) || InStartArea(window.b)) continue;
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

    // Where a hold resumes after a release: the stage reached (cracks do not heal).
    static float StageFloor(int stage) => stage >= 2 ? FrontRoomsShotTimings.GlassBreak.Crack2 : stage >= 1 ? FrontRoomsShotTimings.GlassBreak.Crack1 : 0f;

    /// <summary>
    /// E went down on a pane: the hold begins. The impact is
    /// <paramref name="hitPoint"/> (the base-eye hit), kept 0.2 m inside the
    /// exposed glass, while the pane is uncracked; once it has cracked the
    /// cracks' own centre stays. <paramref name="strike"/> is the base-eye
    /// forward (the pieces' impulse at the shatter). Raises GlassHoldStarted.
    /// Returns the window, or null when the collider is not a pane.
    /// </summary>
    public Window BeginGlassHold(Collider c, Vector3 hitPoint, Vector3 strike)
    {
        if (c == null || !windowByCollider.TryGetValue(c, out var window)) return null;
        var record = GlassBreakOf(window);
        if (record.stage == 0)
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

    /// <summary>E let go (or the aim left the pane) before the break: the hold drops back to the stage reached, never below it.</summary>
    public void ReleaseHold(Collider c)
    {
        if (c == null || !windowByCollider.TryGetValue(c, out var window)) return;
        if (window.hold > 0f && window.pushing) GlassHoldReleased?.Invoke(window.position);
        window.hold = StageFloor(window.stage);
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
        window.pushing = false;
    }

    bool HasKeyHere() => player != null && keysHeld.Contains(Cache.ZoneOf(CellOf(player.position)).id);

    // Shut and needing a key here: doors need keys, no key has opened this door yet, and the player holds none for this zone.
    bool LockedHere(Door door) => doorsNeedKeys && !unlockedDoors.Contains(door.edge) && !HasKeyHere();

    /// <summary>Tools and tests in edit mode, where Update does not run: move doors that are opening or breaking.</summary>
    public void TickDoorsForTools(float dt) => TickDoors(dt);

    void TickDoors(float dt)
    {
        for (var i = unlocking.Count - 1; i >= 0; i--)
        {
            var (door, left) = unlocking[i];
            // Rebuilt, broken or opened meanwhile: nothing left to open.
            if (door.hinge == null || door.broken || door.open) { unlocking.RemoveAt(i); continue; }
            left -= dt;
            if (left > 0f) { unlocking[i] = (door, left); continue; }
            unlocking.RemoveAt(i);
            // The key has turned: it opens from where the player stands now (a pull is judged at the swing, not the press).
            OpenDoor(door, player != null ? player.position : door.position);
        }
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
                target = Mathf.Lerp(door.from, DoorBreakThrowDegrees, door.clock / throwSeconds);
                if (door.clock >= throwSeconds)
                {
                    // Off the stop: it bounces back to rest, tearing crooked on its hinges.
                    door.motion = DoorMotion.Bounce;
                    door.from = DoorBreakThrowDegrees;
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
            else
            {
                StartSwing(door, FrontRoomsShotTimings.DoorBreak.BounceRestDeg, false);
                door.crooked = FrontRoomsShotTimings.DoorBreak.CrookedDeg;
            }
            return true;
        }
        if (!done || Mathf.Abs(door.angle - target) > 1e-3f) return true;
        var latched = door.motion == DoorMotion.Shut;
        door.motion = DoorMotion.None;
        if (latched) DoorLatched?.Invoke(door, door.position);
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
    /// the marker says), or spinning 1.05 m above its cell's centre. The one
    /// place a key is made, so the key model and its host plug in here.
    /// </summary>
    void SpawnKey(BuiltChunk chunk, MapChunk data, bool atSpot)
    {
        var cs = MapGrid.CellSize;
        var key = GameObject.CreatePrimitive(PrimitiveType.Cube);
        key.name = "Key · zone " + data.ownZone.id;
        // Out of the physics scene at once: a deferred Destroy would leave it for this frame's queries.
        var keyCollider = key.GetComponent<Collider>();
        keyCollider.enabled = false;
        Kill(keyCollider);
        key.transform.SetParent(chunk.root.transform, false);
        key.transform.localScale = new Vector3(.32f, .12f, .12f);
        if (atSpot)
        {
            // Resting on what is under it (the floor, or the top of the prop the marker sits on).
            key.transform.localPosition = new Vector3(data.keyX, data.keyY + .06f, data.keyZ);
            key.transform.localRotation = Quaternion.Euler(0f, data.keyYaw, 0f);
        }
        else
        {
            var origin = new Vector3(data.coord.x * MapGrid.ChunkSize, 0f, data.coord.y * MapGrid.ChunkSize);
            key.transform.localPosition = new Vector3((data.keySiteCell.x + .5f) * cs, 1.05f, (data.keySiteCell.y + .5f) * cs) - origin;
        }
        key.GetComponent<Renderer>().sharedMaterial = keyGlow;
        chunk.keys.Add((key, data.ownZone.id, !atSpot));
    }

    /// <summary>True when the room the key spot came from (the topmost room on the key's site cell) overlaps the start area.</summary>
    bool KeyRoomInStartArea(MapChunk data)
    {
        if (!hasStartArea) return false;
        var o = data.Origin;
        int si = data.keySiteCell.x - o.x, sj = data.keySiteCell.y - o.y;
        for (var r = data.rooms.Length - 1; r >= 0; r--)
            if (data.rooms[r].Contains(si, sj)) return RoomInStartArea(data, data.rooms[r]);
        return false;
    }

    /// <summary>The Relay entries of the chunk's intact module rooms (outside the start area), on the floor, in world space.</summary>
    void RegisterRelayEntries(BuiltChunk chunk, MapChunk data)
    {
        var cs = MapGrid.CellSize;
        for (var r = 0; r < data.rooms.Length; r++)
        {
            var module = data.ModuleOf(r);
            if (module == null || module.markers == null || !data.RoomIntact(r) || RoomInStartArea(data, data.rooms[r])) continue;
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
    /// one the cache has not generated yet it predicts at the current
    /// GenerationTier with Auto lamps. Off where there is no lamp: a module's
    /// Off (no fixture, so SetLampMode cannot reach it), or the start area.
    /// </summary>
    public ModuleLamp LampModeOf(GridCoord cell) => LampModeOf(cell, true);

    ModuleLamp LampModeOf(GridCoord cell, bool withSet)
    {
        if (Cache == null || InStartArea(cell)) return ModuleLamp.Off;
        var lamp = ModuleLamp.Auto;
        var tier = GenerationTier;
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

    /// <summary>How an override comes and goes: seconds to reach its multiplier, and to come back to 1.</summary>
    public readonly struct LampEnvelope
    {
        public readonly float attack, release;
        public LampEnvelope(float attack, float release) { this.attack = attack; this.release = release; }
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

    sealed class LampOverride
    {
        public int id;
        public GridCoord cell;
        public LampFx kind;
        public float multiplier, attack, hold, release, age, weight;
        // Releasing (its hold ran out, or RemoveLampOverride): back to 1 from releaseFrom over the release.
        public bool releasing;
        public float releaseFrom, releaseAge;
    }

    /// <summary>A built lamp's logical level this frame, overrides included (0..1); NoLamp where there is none.</summary>
    public float LampLevel(GridCoord cell) => fixtureByCell.TryGetValue(cell, out var f) ? f.level : NoLamp;

    /// <summary>A built lamp's logical level this frame before any override (its own temperament); NoLamp where there is none.</summary>
    public float LampBaseLevel(GridCoord cell) => fixtureByCell.TryGetValue(cell, out var f) ? f.baseLevel : NoLamp;

    /// <summary>The overrides active now (tools and tests).</summary>
    public int LampOverrideCount => lampOverrides.Count;

    /// <summary>
    /// Set a lamp's temperament for good (Auto goes back to the map's own):
    /// the kill (Off: a dark lens, the troffer still there) or a promotion
    /// (e.g. Failing). Kept by cell, so it survives a chunk drop, a rebuild
    /// and a revisit shift; applied live to a built lamp, which starts a fresh
    /// cycle of its new mode. Nothing in the start area, and nothing where a
    /// module took the lamp out (there is no fixture to change).
    /// </summary>
    public void SetLampMode(GridCoord cell, ModuleLamp mode)
    {
        if (InStartArea(cell)) return;
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
        if (FrontRoomsSettings.ReduceFlashing)
        {
            attack = Mathf.Max(attack, ReducedFlashingEnvelopeSeconds);
            release = Mathf.Max(release, ReducedFlashingEnvelopeSeconds);
        }
        var o = new LampOverride
        {
            id = ++nextLampOverride, cell = cell, kind = kind, multiplier = Mathf.Clamp01(multiplier),
            attack = attack, hold = Mathf.Max(0f, hold), release = release,
        };
        lampOverrides.Add(o);
        if (!overridesByCell.TryGetValue(cell, out var list)) overridesByCell[cell] = list = new List<LampOverride>(2);
        list.Add(o);
        if (fixtureByCell.TryGetValue(cell, out var f)) LampDipped?.Invoke(cell, f.light.transform.position);
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
            o.weight = o.releaseFrom * (1f - Mathf.Clamp01(o.releaseAge / o.release));
            if (o.releaseAge < o.release) continue;
            lampOverrides.RemoveAt(i);
            if (overridesByCell.TryGetValue(o.cell, out var list) && list.Remove(o) && list.Count == 0) overridesByCell.Remove(o.cell);
        }
    }

    // A lamp's level this frame: its temperament, times the lowest active
    // override multiplier (none: exactly Level(f)). Reports band crossings.
    float LampTick(Fixture f)
    {
        f.baseLevel = Level(f);
        var level = f.baseLevel;
        if (overridesByCell.Count > 0 && overridesByCell.TryGetValue(f.cell, out var list))
        {
            var lowest = 1f;
            foreach (var o in list) lowest = Mathf.Min(lowest, 1f + (o.multiplier - 1f) * o.weight);
            level *= lowest;
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

    /// <summary>Every Relay entry of the built map (module markers), with its tag (null for none). The Relay prefers them when it appears.</summary>
    public void RelayEntries(List<(Vector3 pos, string tag)> into)
    {
        into.Clear();
        foreach (var chunk in built.Values) into.AddRange(chunk.relayEntries);
    }

    void CollectKeys()
    {
        var p = player.position;
        foreach (var chunk in built.Values)
            for (var i = chunk.keys.Count - 1; i >= 0; i--)
            {
                var (go, zone, spin) = chunk.keys[i];
                if (go == null) { chunk.keys.RemoveAt(i); continue; }
                if (spin) go.transform.Rotate(0f, 90f * Time.deltaTime, 0f, Space.World);
                var d = go.transform.position - p;
                if (new Vector2(d.x, d.z).sqrMagnitude > .9f * .9f) continue;
                keysHeld.Add(zone);
                Kill(go);
                chunk.keys.RemoveAt(i);
                KeyTaken?.Invoke(zone);
            }
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
        keyGlow = Own(FrontRoomsSurfaces.Lit("Map test / key", new Color(.96f, .87f, .23f), .4f, 0f, new Color(.96f, .87f, .23f) * .8f));
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
