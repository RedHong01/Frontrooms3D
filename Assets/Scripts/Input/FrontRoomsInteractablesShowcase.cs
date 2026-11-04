using System;
using System.Collections.Generic;
using System.Reflection;
using FrontRooms.Map;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Scene 3 (Assets/Scenes/FrontRoomsScreenAdsDemo.unity): an example of every interactable of the game, tried with the
/// game's own controls and code (Red 2026-11-03: "在这里我认为应该是放置所有可交互的资产的example"; RULES 5b; 交互设计).
/// The one bootstrap object of the scene; nothing in the game scene or the shipped build refers to it.
///
/// Doors, windows, keys, the EXIT and the alarm devices are not stand-alone parts: they are FrontRoomsMapWorld's records,
/// and USE on them is FrontRooms3DGame's player loop (the unlock and glass shots, the pull step, pry, climb, body doors).
/// Their states also exclude each other (the station's floor key turns interior locks off; body doors turn E doors off),
/// so no hand-built room could hold them all without copying that loop. Instead a tour runs the game itself:
/// - Play opens a menu over the scene's CRT ad bay. 1 MEDIA BAY stays here: the screen ads on their own light rig
///   (FrontRoomsScreenAdsDemoRig), as before. 2-4 load the game scene (build index 0) in place of this one; this object
///   survives the load and stays with the game until Play stops or F2 brings the menu back.
/// - Before the run starts (the title, until SPACE) the tour hands the new FrontRooms3DGame a copy of its own level
///   profile with the tour's switches: AS SHIPPED none; EVERY DEVICE sash pry, foil, alarm doors and Run! rooms on (off
///   in the shipped profile); KEYS FLOOR no station, doors need keys, E doors. The shipped Level 0 asset is never
///   written: the copy is DontSave and dies with the tour. The game's restart reloads its scene and gets it again.
/// - From the run's start the guide lists every kind with the nearest example on this floor, found with the map's own
///   records and the game's own classifier (DoorBetween and EffectiveEdge on the built cells round the player, Kind on
///   what stands there, FrontRoomsInspect's registry), tags the examples in view, and jumps the player in front of one
///   (1-0, [ ] for the page, J the next one of the same kind). H holds the Relay (the hunter's own HeldForTools), B
///   breaks the door in the crosshair with the map's BreakDoor (the Relay's break) for the BROKEN example, V turns
///   FrontRoomsInspect.DevTargets on for the test-bed close-ups, G hides the list.
/// - 5 MUSEUM is the museum built in the game (.Museum.cs): hub 0 and rooms 1-4 carved into the game's own map from one
///   layout, walked by the game's own player with no title and no Relay; room 1's doors live, rooms 2-4 as framed codes.
///   P on its hub board (or P on this menu, without the game) opens the plan: the museum from FrontRoomsShowcaseTable,
///   every station with its code and placard; ENTER loads the station's tour and jumps to it past the start door (.Plan.cs).
/// - 6 ROOM TYPES is a fourth game tour on a copy tilted toward variety: every kind of room the game builds (R0-R13), found
///   on the built chunks round the player, a station card each, jumps behind a gate, K the B1 route, L a dark patch lit (.Rooms.cs).
/// Editor tool: the private fields it sets (levelProfile, doorLessonProfile, yaw, pitch) are found by name and checked;
/// a missing one is reported on screen and the tour runs on without that part.
/// </summary>
[AddComponentMenu("FrontRooms/Interactables Showcase (scene 3)")]
[DisallowMultipleComponent]
public sealed partial class FrontRoomsInteractablesShowcase : MonoBehaviour
{
    public enum Tour { Menu, MediaBay, Shipped, Devices, KeysFloor, MuseumPlan, Rooms, Museum }

    [Flags]
    enum Floors { None = 0, Shipped = 1, Devices = 2, Keys = 4, Station = Shipped | Devices, All = Shipped | Devices | Keys }

    enum Kind
    {
        BodyDoor, Door, LockedDoor, ZoneKey, KeyDoor, KeyTag, StairKey, Exit, StationTv, Placard, StationPaper,
        DoorSign, Peek, Window, Sash, FoiledWindow, AlarmDoor, BrokenDoor, RunDoor, Terminal, DevCloseUp, OtherCloseUp
    }

    const int KindCount = 22, PageSize = 10;
    const string ScenePath = "Assets/Scenes/FrontRoomsScreenAdsDemo.unity";
    const int GameBuildIndex = 0;
    /// <summary>Doors and windows: the built cells this far round the player (18 cells = 54 m), every ScanSeconds.</summary>
    const int EdgeCells = 18;
    const float ScanSeconds = .75f, NearRadius = 16f, CloseUpRange = 60f, TagRange = 30f;
    const float BodyRadius = .28f, BodyHeight = 1.8f;

    static readonly string[] Names =
    {
        "BODY DOOR", "E DOOR", "LOCKED DOOR", "ZONE KEY", "LOCKED DOOR, KEY IN HAND", "KEY TAG", "STAIR KEY", "EXIT DOOR",
        "STATION TV", "EVACUATION PLACARD", "STATION PAPER", "DOOR SIGN", "DOOR LITE", "WINDOW", "SASH LATCH",
        "FOILED WINDOW", "ALARM DOOR", "BROKEN DOOR", "RUN! DOOR", "OFFICE TERMINAL", "DEV CLOSE-UPS", "OTHER CLOSE-UPS"
    };

    static readonly string[] HowTo =
    {
        "Walk into it: it swings away. Sprint (Ctrl) into it: barge. Walk back from an open one: it shuts.",
        "E opens and shuts it (double-acting). Shut it from the leaf's side and you step clear of it.",
        "E without its key: the rattle, LOCKED.",
        "Look at it, E: the pickup, then the key HUD holds it.",
        "With its zone's key: E · UNLOCK, the key-turn shot, then it opens.",
        "E · EXAMINE KEY: turn it with the mouse, scroll for closer.",
        "On the station's key plate: E · TAKE KEY. Taking it signals the panel.",
        "Floor 1: stair key in hand, E · UNLOCK, the padlock cut, then the U1 chain fall. Keys floor: a plain door to the stair.",
        "E · WATCH TV: a live close-up, the world runs on.",
        "E · READ PLAN. T shows the text, E or S goes back.",
        "The desk's clipboards and register, dated by the clock.",
        "E · READ SIGN.",
        "E · PEEK through the wired glass.",
        "Hold E · BREAK GLASS from 1.2 m, then walk into the opening to climb through.",
        "Hold E · PRY: the sash opens for good, quiet but heard at 8 m.",
        "Foil tape and a sash contact: a pry or a break trips it and calls the Relay.",
        "A push-bar exit alarm: opening it trips the horn and calls the Relay.",
        "B breaks the door in the crosshair, as the Relay does. Then E: WON'T SHUT.",
        "An alarm corridor's goal door. This tour forces Run! rooms; they stand 70-120 m out: walk on.",
        "E · READ SCREEN. Inside, the terminal's own keys (F1 help, F3 exit).",
        "Desk phone, wall clock, vending machine, copier: E · LOOK. Test-bed blocks, never shipped (V).",
        "Any other close-up the game registers: E."
    };

    // The floors each kind is built on (Station: both floor-1 tours). Door signs hang only on locked doors, so only the keys
    // floor shows one; no game code places a terminal yet (None).
    static readonly Floors[] Where =
    {
        Floors.Station, Floors.Keys, Floors.Keys, Floors.Keys, Floors.Keys, Floors.Keys, Floors.Station, Floors.All,
        Floors.Station, Floors.All, Floors.Station, Floors.Keys, Floors.All, Floors.All, Floors.Devices,
        Floors.Devices, Floors.Devices, Floors.All, Floors.Devices, Floors.None, Floors.All, Floors.All
    };

    sealed class Example
    {
        public Kind kind;
        public Vector3 point;
        public Vector3 facing;
        public Collider collider;
        public FrontRoomsMapWorld.Door door;
        public FrontRoomsInspectable target;
        public float distance;
        public bool occluded;
    }

    static FrontRoomsInteractablesShowcase live;
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static readonly FieldInfo ProfileField = typeof(FrontRooms3DGame).GetField("levelProfile", Private);
    static readonly FieldInfo DoorLessonField = typeof(FrontRooms3DGame).GetField("doorLessonProfile", BindingFlags.Static | BindingFlags.NonPublic);
    static readonly FieldInfo YawField = typeof(FrontRooms3DGame).GetField("yaw", Private);
    static readonly FieldInfo PitchField = typeof(FrontRooms3DGame).GetField("pitch", Private);

    Tour tour = Tour.Menu;
    // Set by Choose until the game scene it loads is in: the old game's MapRunEnded (the museum's plan) keeps ENTER's jump.
    bool tourLoading;
    FrontRooms3DGame game;
    FrontRoomsLevelProfile tourProfile;
    FrontRoomsMapWorld map;
    FrontRoomsMapHunter relay;
    Transform player;
    Camera view;
    int floor = 1;
    bool paused, listShown = true, holdRelay = true, devTargets = true;
    int page;
    float scanClock;
    Kind lastKind;
    int lastIndex = -1;
    string toastTitle, toastLine, problem;
    float toastUntil;

    readonly List<Example>[] found = new List<Example>[KindCount];
    readonly List<Example> pool = new List<Example>();
    int poolUsed;
    readonly Collider[] hits = new Collider[2048];
    readonly Collider[] boxHits = new Collider[64];
    readonly List<FrontRoomsInspectable> targets = new List<FrontRoomsInspectable>();
    readonly HashSet<long> doorEdges = new HashSet<long>();
    readonly HashSet<long> windowEdges = new HashSet<long>();
    readonly HashSet<Collider> seen = new HashSet<Collider>();
    readonly Comparison<Example> byDistance = (x, y) => x.distance.CompareTo(y.distance);

    Font bayon, mono;
    GUIStyle menuTitleStyle, menuNameStyle, titleStyle, nameStyle, lineStyle, smallStyle, statusStyle, toastTitleStyle, toastLineStyle;
    float styleScale = -1f;
    static Texture2D white;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => live = null;

    bool InGameTour => tour == Tour.Shipped || tour == Tour.Devices || tour == Tour.KeysFloor || tour == Tour.Rooms || tour == Tour.Museum;

    // ROOM TYPES keeps the shipped station, its key and the EXIT chain: for the kinds' rules it is the shipped floor.
    Floors ThisFloor => tour == Tour.Shipped || tour == Tour.Rooms ? Floors.Shipped : tour == Tour.Devices ? Floors.Devices : tour == Tour.KeysFloor ? Floors.Keys : Floors.None;

    void Awake()
    {
        // One guide: the one that came along with the game wins over a fresh copy (F2 destroys the old one first).
        if (live != null && live != this)
        {
            Destroy(gameObject);
            return;
        }
        live = this;
        for (var k = 0; k < KindCount; k++) found[k] = new List<Example>();
        // The museum table (generated) names every kind once: say which one it lost, never stop the guide for it.
        for (var k = 0; k < KindCount; k++)
            if (FrontRoomsShowcaseTable.OfGuideKind(k) == null)
                Debug.LogWarning("Interactables showcase: FrontRoomsShowcaseTable has no row for " + Names[k] + " (kind " + k + "); regenerate it.");
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        FrontRooms3DGame.MapRunStarted += OnMapRunStarted;
        FrontRooms3DGame.MapRunEnded += OnMapRunEnded;
        FrontRooms3DGame.FloorChanged += OnFloorChanged;
        FrontRooms3DGame.Paused += OnPaused;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        FrontRooms3DGame.MapRunStarted -= OnMapRunStarted;
        FrontRooms3DGame.MapRunEnded -= OnMapRunEnded;
        FrontRooms3DGame.FloorChanged -= OnFloorChanged;
        FrontRooms3DGame.Paused -= OnPaused;
    }

    void OnDestroy()
    {
        if (live == this) live = null;
        if (tourProfile != null) Destroy(tourProfile);
        if (InGameTour) FrontRoomsInspect.DevTargets = false;
        DestroyPlanFont();
        RestoreLamps();
        // The museum's kit statics (DoorMemberOverride, DoorLockedOverride) back as they were.
        ClearMuseumTools();
    }

    // ---------- tours

    void Choose(Tour next)
    {
        // Leaving the museum (its plan's ENTER): the kit's statics go back before anything else is built.
        ClearMuseumTools();
        tour = next;
        if (!InGameTour) return;
        tourLoading = true;
        // The game scene replaces this one; the guide goes with it (a root object).
        DontDestroyOnLoad(gameObject);
        SceneManager.LoadScene(GameBuildIndex, LoadSceneMode.Single);
    }

    void BackToMenu()
    {
        // ROOM TYPES: a patch L lit goes back as built before the map is left; MUSEUM: the kit's statics.
        RestoreLamps();
        ClearMuseumTools();
#if UNITY_EDITOR
        // Scene 3 is not in the enabled build list (it never ships): only the editor loads it by path.
        live = null;
        EditorSceneManager.LoadSceneInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
        Destroy(gameObject);
#else
        tour = Tour.Menu;
#endif
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        tourLoading = false;
        if (!InGameTour) return;
        map = null;
        relay = null;
        player = null;
        floor = 1;
        paused = false;
        lastIndex = -1;
        ClearFound();
        game = FindAnyObjectByType<FrontRooms3DGame>();
        if (game == null)
        {
            problem = "No FrontRooms3DGame in " + scene.path + ".";
            return;
        }
        view = game.GetComponentInChildren<Camera>(true);
        ApplyTour();
        // MUSEUM: the Relay's shim, the run's start without the title, a run already going (.Museum.cs).
        if (tour == Tour.Museum) MuseumSceneLoaded();
    }

    /// <summary>The tour's copy of the game's own profile, handed over before the run reads it (StartRunInPlace, at SPACE).</summary>
    void ApplyTour()
    {
        problem = null;
        // Kits attach their test-bed close-ups as they spawn, so the switch is on before the first room is built.
        FrontRoomsInspect.DevTargets = devTargets;
        if (tour == Tour.Shipped) return;
        if (ProfileField == null || ProfileField.FieldType != typeof(FrontRoomsLevelProfile))
        {
            problem = "FrontRooms3DGame.levelProfile not found: this tour plays the shipped profile.";
            return;
        }
        var source = ProfileField.GetValue(game) as FrontRoomsLevelProfile;
        if (source == null) source = FrontRoomsLevelProfile.Default;
        if (tourProfile != null) Destroy(tourProfile);
        tourProfile = Instantiate(source);
        tourProfile.name = source.name + " (scene 3, " + TourName(tour) + ")";
        tourProfile.hideFlags = HideFlags.DontSave;
        if (tour == Tour.Devices)
        {
            // The Relay v2 devices that are off in the shipped profile (C12 sash pry, C13 foil, C16 alarm doors) and the
            // Run! rooms; the station, its key and the EXIT chain, and body doors as shipped.
            tourProfile.sashPry = tourProfile.foil = tourProfile.alarmDoor = tourProfile.runRooms = true;
        }
        else if (tour == Tour.Rooms) ApplyRoomsCopy(source);
        else if (tour == Tour.Museum) ApplyMuseumCopy();
        else
        {
            // No station: no floor-key rule, so interior doors lock and zone keys lie about; E doors (no body doors).
            tourProfile.stationModule = tourProfile.stationWaiting = tourProfile.stationDesk = null;
            tourProfile.doorsNeedKeys = true;
            tourProfile.bodyDoors = false;
            tourProfile.sashPry = tourProfile.foil = tourProfile.alarmDoor = tourProfile.runRooms = false;
        }
        ProfileField.SetValue(game, tourProfile);
        // The title's door lesson reads the profile it was built with (DL2); it follows the tour's.
        if (DoorLessonField != null && DoorLessonField.FieldType == typeof(FrontRoomsLevelProfile)) DoorLessonField.SetValue(null, tourProfile);
    }

    void OnMapRunStarted(FrontRoomsMapWorld startedMap, FrontRoomsMapHunter hunter)
    {
        if (!InGameTour) return;
        map = startedMap;
        relay = hunter;
        floor = 1;
        if (game == null) game = FindAnyObjectByType<FrontRooms3DGame>();
        if (view == null && game != null) view = game.GetComponentInChildren<Camera>(true);
        player = game != null ? game.transform.Find("Player") : null;
        if (tour == Tour.Devices && map != null) ForceDevices();
        if (tour == Tour.Rooms && map != null) RoomsRunStarted();
        if (tour == Tour.Museum)
        {
            // The carve now, the build and the walk in on the next frame; its own toast on arrival (.Museum.cs).
            if (map != null) MuseumRunStarted();
            return;
        }
        scanClock = 0f;
        Toast(TourName(tour), tour == Tour.Rooms ? "Every kind of room, R1-R13 on the list: 1-0 jumps, J the next variant, K the B1 route, L a dark patch lit."
            : tour == Tour.KeysFloor ? "E doors, locked doors, zone keys and the unlock shot. G lists them, 1-0 jumps."
            : "The station, its key, the EXIT, the TV and placard, doors and windows" + (tour == Tour.Devices ? " and every device." : ". G lists them, 1-0 jumps."));
        // A station picked on the museum plan with no automatic jump (1.6 RUN! DOOR, 3.6 the card): its note and placard.
        if (pendingStation != null && pendingKind < 0)
        {
            Toast(LabelOf(pendingStation), pendingStation.note ?? "");
            ShowPlacard(pendingStation);
            ClearPending();
        }
    }

    /// <summary>
    /// EVERY DEVICE: the tools' force on this floor's planners, so every zone planned from now on that may hold a foiled
    /// window, an alarm door or a Run! room gets one. The Run! force is the autopilot's own flag (FrontRoomsRunRules.force):
    /// it also lifts the tier-2 and first-send gates. Each floor makes new planners, so FloorChanged sets them again.
    /// </summary>
    void ForceDevices()
    {
        if (map.Foil != null) map.Foil.Rules.force = true;
        if (map.AlarmDoors != null) map.AlarmDoors.Rules.force = true;
        if (map.RunRooms != null) map.RunRooms.Rules.force = true;
    }

    void OnMapRunEnded()
    {
        map = null;
        relay = null;
        player = null;
        lastIndex = -1;
        ClearFound();
        // A tour chosen on the museum's plan unloads this game first: its jump waits for the next run.
        if (!tourLoading) ClearPending();
        RoomsRunEnded();
        MuseumRunEnded();
    }

    void OnFloorChanged(FrontRoomsMapWorld changedMap, FrontRoomsMapHunter hunter, int newFloor)
    {
        map = changedMap;
        relay = hunter;
        floor = newFloor;
        lastIndex = -1;
        ClearFound();
        scanClock = 0f;
        if (tour == Tour.Devices && map != null) ForceDevices();
        if (tour == Tour.Rooms && map != null) RoomsFloorChanged();
        if (tour == Tour.Museum) MuseumFloorChanged();
    }

    void OnPaused(bool on) => paused = on;

    static string TourName(Tour t) =>
        t == Tour.Shipped ? "FLOOR 1 · AS SHIPPED" : t == Tour.Devices ? "FLOOR 1 · EVERY DEVICE" : t == Tour.KeysFloor ? "KEYS FLOOR"
        : t == Tour.MediaBay ? "MEDIA BAY" : t == Tour.MuseumPlan ? "MUSEUM · PLAN" : t == Tour.Rooms ? "ROOM TYPES" : t == Tour.Museum ? "MUSEUM" : "TOURS";

    static Tour HomeOf(Floors where) =>
        (where & Floors.Shipped) != 0 ? Tour.Shipped : (where & Floors.Devices) != 0 ? Tour.Devices : Tour.KeysFloor;

    // ---------- per frame

    void Update()
    {
        var keys = Keyboard.current;
        if (tour == Tour.Menu)
        {
            if (keys == null) return;
            if (keys.digit1Key.wasPressedThisFrame || keys.numpad1Key.wasPressedThisFrame) Choose(Tour.MediaBay);
            else if (keys.digit2Key.wasPressedThisFrame || keys.numpad2Key.wasPressedThisFrame) Choose(Tour.Shipped);
            else if (keys.digit3Key.wasPressedThisFrame || keys.numpad3Key.wasPressedThisFrame) Choose(Tour.Devices);
            else if (keys.digit4Key.wasPressedThisFrame || keys.numpad4Key.wasPressedThisFrame) Choose(Tour.KeysFloor);
            else if (keys.digit5Key.wasPressedThisFrame || keys.numpad5Key.wasPressedThisFrame) Choose(Tour.Museum);
            else if (keys.digit6Key.wasPressedThisFrame || keys.numpad6Key.wasPressedThisFrame) Choose(Tour.Rooms);
            // The museum's plan alone, here in scene 3 (no game): also the way in when the game scene cannot build it.
            else if (keys.pKey.wasPressedThisFrame) Choose(Tour.MuseumPlan);
            return;
        }
        if (keys != null && keys.f2Key.wasPressedThisFrame && !FrontRooms3DGame.InspectingNow)
        {
            BackToMenu();
            return;
        }
        if (tour == Tour.MuseumPlan)
        {
            UpdatePlan(keys);
            return;
        }
        if (!InGameTour) return;

        if (relay != null) relay.HeldForTools = holdRelay;
        // The game's teardown of a close-up (FrontRoomsInspect.Abort) puts the switch back; keep it as the tour has it.
        FrontRoomsInspect.DevTargets = devTargets;
        // MUSEUM: its own start, build, keys and plan (.Museum.cs); none of the tours' list, jumps or tags.
        if (tour == Tour.Museum)
        {
            UpdateMuseum(keys);
            return;
        }
        if (map == null)
        {
            // ROOM TYPES' title: N picks the station of the floors below (the copy is read at SPACE).
            if (tour == Tour.Rooms) UpdateRoomsTitle(keys);
            return;
        }
        if (player == null && game != null) player = game.transform.Find("Player");
        scanClock -= Time.unscaledDeltaTime;
        if (scanClock <= 0f)
        {
            scanClock = ScanSeconds;
            Scan();
        }
        TryPending();
        if (tour == Tour.Rooms) TickRooms();
        if (keys == null || paused || FrontRooms3DGame.InspectingNow) return;
        if (keys.gKey.wasPressedThisFrame) listShown = !listShown;
        if (keys.hKey.wasPressedThisFrame)
        {
            holdRelay = !holdRelay;
            Toast(holdRelay ? "RELAY HELD" : "RELAY LIVE", holdRelay ? "It stands where it is and never moves or catches (the tools' hold)." : "The game's own Relay: it hunts as in a run.");
        }
        if (keys.vKey.wasPressedThisFrame)
        {
            devTargets = !devTargets;
            FrontRoomsInspect.DevTargets = devTargets;
            Toast(devTargets ? "DEV CLOSE-UPS ON" : "DEV CLOSE-UPS OFF", "Rooms built from now on attach them as their kits spawn.");
        }
        if (keys.bKey.wasPressedThisFrame) BreakDoorInView();
        if (tour == Tour.Rooms)
        {
            // The R list's own pages, jumps, J, K and L (.Rooms.cs).
            UpdateRoomsKeys(keys);
            return;
        }
        if (keys.leftBracketKey.wasPressedThisFrame || keys.pageUpKey.wasPressedThisFrame) page = (page + PageCount - 1) % PageCount;
        if (keys.rightBracketKey.wasPressedThisFrame || keys.pageDownKey.wasPressedThisFrame) page = (page + 1) % PageCount;
        // A jump key cancels ENTER's waiting jump: the manual one wins.
        if (keys.jKey.wasPressedThisFrame)
        {
            ClearPending();
            if (lastIndex >= 0) Jump(lastKind, lastIndex + 1);
        }
        var digit = DigitDown(keys);
        if (digit >= 0)
        {
            ClearPending();
            var k = page * PageSize + digit;
            if (k < KindCount) Jump((Kind)k, 0);
        }
    }

    static int PageCount => (KindCount + PageSize - 1) / PageSize;

    static int DigitDown(Keyboard keys)
    {
        if (keys.digit1Key.wasPressedThisFrame) return 0;
        if (keys.digit2Key.wasPressedThisFrame) return 1;
        if (keys.digit3Key.wasPressedThisFrame) return 2;
        if (keys.digit4Key.wasPressedThisFrame) return 3;
        if (keys.digit5Key.wasPressedThisFrame) return 4;
        if (keys.digit6Key.wasPressedThisFrame) return 5;
        if (keys.digit7Key.wasPressedThisFrame) return 6;
        if (keys.digit8Key.wasPressedThisFrame) return 7;
        if (keys.digit9Key.wasPressedThisFrame) return 8;
        if (keys.digit0Key.wasPressedThisFrame) return 9;
        return -1;
    }

    // ---------- finding the examples (the map's records and the game's own classifier)

    void ClearFound()
    {
        for (var k = 0; k < KindCount; k++) if (found[k] != null) found[k].Clear();
        poolUsed = 0;
    }

    Example Take(Kind kind)
    {
        if (poolUsed == pool.Count) pool.Add(new Example());
        var e = pool[poolUsed++];
        e.kind = kind;
        e.collider = null;
        e.door = null;
        e.target = null;
        e.occluded = false;
        found[(int)kind].Add(e);
        return e;
    }

    Vector3 Feet => player != null ? player.position : view != null ? view.transform.position : Vector3.zero;

    void Scan()
    {
        ClearFound();
        doorEdges.Clear();
        windowEdges.Clear();
        seen.Clear();
        var feet = Feet;
        ScanEdges(feet);
        ScanNear(feet);
        ScanStation(feet);
        ScanCloseUps(feet);
        for (var k = 0; k < KindCount; k++) found[k].Sort(byDistance);
        if (view != null) MarkOccluded();
    }

    /// <summary>Every door and window on the built cells round the player: the map's Door records and its edge kinds.</summary>
    void ScanEdges(Vector3 feet)
    {
        var centre = map.CellOf(feet);
        for (var dy = -EdgeCells; dy <= EdgeCells; dy++)
        for (var dx = -EdgeCells; dx <= EdgeCells; dx++)
        {
            var a = new GridCoord(centre.x + dx, centre.y + dy);
            if (!map.IsChunkBuiltAt(a)) continue;
            EdgeAt(a, new GridCoord(a.x + 1, a.y), feet);
            EdgeAt(a, new GridCoord(a.x, a.y + 1), feet);
        }
        // The EXIT stands on the stairwell's reserved cell: its door may be off the scan's cells.
        var exit = map.ExitDoor;
        if (exit != null && doorEdges.Add(exit.edge)) AddDoor(exit, feet);
    }

    void EdgeAt(GridCoord a, GridCoord b, Vector3 feet)
    {
        var door = map.DoorBetween(a, b);
        if (door != null)
        {
            if (doorEdges.Add(door.edge)) AddDoor(door, feet);
            return;
        }
        if (!map.IsChunkBuiltAt(b) || map.EffectiveEdge(a, b) != EdgeKind.Window) return;
        var edge = MapGrid.EdgeId(a, b);
        if (!windowEdges.Add(edge)) return;
        // The pane and the sash latch stand in the wall between the two cells: ask the game's classifier about each.
        var mid = (map.CellCenter(a) + map.CellCenter(b)) * .5f;
        var east = b.x != a.x;
        var half = east ? new Vector3(.3f, 1.1f, 1.3f) : new Vector3(1.3f, 1.1f, .3f);
        var count = Physics.OverlapBoxNonAlloc(mid + Vector3.up * 1.3f, half, boxHits, map.transform.rotation, ~0, QueryTriggerInteraction.Collide);
        var foiled = map.IsFoiledWindow(a, b);
        var facing = map.transform.rotation * (east ? Vector3.right : Vector3.forward);
        for (var i = 0; i < count; i++)
        {
            var c = boxHits[i];
            if (c == null || !c.enabled || map.DoorOf(c) != null || !seen.Add(c)) continue;
            var aim = map.Kind(c, feet, out _, out _);
            if (aim == FrontRoomsMapWorld.AimKind.Glass || aim == FrontRoomsMapWorld.AimKind.GlassTap)
                AddCollider(foiled ? Kind.FoiledWindow : Kind.Window, c, facing, feet);
            else if (aim == FrontRoomsMapWorld.AimKind.Pry) AddCollider(Kind.Sash, c, facing, feet);
            else if (aim == FrontRoomsMapWorld.AimKind.PryAlarmed && !foiled) AddCollider(Kind.FoiledWindow, c, facing, feet);
        }
    }

    /// <summary>Keys and anything else near (the game's classifier on what the body could touch): zone keys lie on desks and hooks.</summary>
    void ScanNear(Vector3 feet)
    {
        var count = Physics.OverlapSphereNonAlloc(feet, NearRadius, hits, ~0, QueryTriggerInteraction.Collide);
        for (var i = 0; i < count; i++)
        {
            var c = hits[i];
            if (c == null || !c.enabled || seen.Contains(c) || (player != null && c.transform.IsChildOf(player))) continue;
            if (map.DoorOf(c) != null) continue;
            var aim = map.Kind(c, feet, out _, out _);
            if (aim != FrontRoomsMapWorld.AimKind.TakeKey || !seen.Add(c)) continue;
            AddCollider(map.FloorKeyRule ? Kind.StairKey : Kind.ZoneKey, c, Flat(feet - c.bounds.center), feet);
        }
    }

    /// <summary>The station's key: the plate's KeySpot, until it is taken (its pickup may not be a collider of its own).</summary>
    void ScanStation(Vector3 feet)
    {
        if (!map.FloorKeyRule || map.StationKeyIsTaken || found[(int)Kind.StairKey].Count > 0) return;
        if (!map.StationAnchor(ModuleMarkerKind.KeySpot, out var keyAt, out var keyTurn, out _)) return;
        var e = Take(Kind.StairKey);
        e.point = keyAt;
        e.facing = Flat(keyTurn * Vector3.forward);
        e.distance = Vector3.Distance(feet, keyAt);
    }

    void ScanCloseUps(Vector3 feet)
    {
        targets.Clear();
        FrontRoomsInspect.CopyTargets(targets);
        for (var i = 0; i < targets.Count; i++)
        {
            var t = targets[i];
            if (t == null || !t.isActiveAndEnabled || t.Def == null) continue;
            if (!t.Def.ship && !FrontRoomsInspect.DevTargets) continue;
            var at = t.BoxCentreWorld;
            var d = Vector3.Distance(feet, at);
            if (d > CloseUpRange) continue;
            var e = Take(KindOf(t));
            e.target = t;
            e.point = at;
            e.facing = Flat(t.Face.rotation * Vector3.forward);
            e.distance = d;
        }
    }

    static Kind KindOf(FrontRoomsInspectable t)
    {
        var kit = t.Kit ?? "";
        if (kit == FrontRoomsInspectCatalog.StationTvKit) return Kind.StationTv;
        if (kit == FrontRoomsInspectCatalog.TerminalKit) return Kind.Terminal;
        if (kit == "Kit_EvacPlacard") return Kind.Placard;
        if (kit == "Kit_DoorSign") return Kind.DoorSign;
        if (t.Kind == InspectKind.Peek) return Kind.Peek;
        if (kit.StartsWith("Kit_KeyTag_", StringComparison.Ordinal) || kit.StartsWith("Kit_Key_Zone", StringComparison.Ordinal)) return Kind.KeyTag;
        if (FrontRoomsStationPaper.IsDatedKit(kit)) return Kind.StationPaper;
        if (!t.Def.ship) return Kind.DevCloseUp;
        return Kind.OtherCloseUp;
    }

    void AddDoor(FrontRoomsMapWorld.Door door, Vector3 feet)
    {
        Kind kind;
        if (map.IsExitDoor(door)) kind = Kind.Exit;
        else if (door.broken) kind = Kind.BrokenDoor;
        else if (door.alarm) kind = Kind.AlarmDoor;
        else if (map.IsRunDoor(door)) kind = Kind.RunDoor;
        else
        {
            var aim = door.leaf != null ? map.Kind(door.leaf, feet, out _, out _) : FrontRoomsMapWorld.AimKind.None;
            kind = aim == FrontRoomsMapWorld.AimKind.Locked ? Kind.LockedDoor
                : aim == FrontRoomsMapWorld.AimKind.LockedHaveKey ? Kind.KeyDoor
                : map.BodyDoors ? Kind.BodyDoor : Kind.Door;
        }
        var e = Take(kind);
        e.door = door;
        e.collider = door.leaf;
        var mid = (map.CellCenter(door.a) + map.CellCenter(door.b)) * .5f;
        e.point = mid + Vector3.up * 1.5f;
        e.facing = Flat(map.CellCenter(door.b) - map.CellCenter(door.a));
        e.distance = Vector3.Distance(feet, mid);
    }

    void AddCollider(Kind kind, Collider c, Vector3 facing, Vector3 feet)
    {
        var e = Take(kind);
        e.collider = c;
        e.point = c.bounds.center;
        e.facing = facing;
        e.distance = Vector3.Distance(feet, e.point);
    }

    void MarkOccluded()
    {
        var eye = view.transform.position;
        for (var k = 0; k < KindCount; k++)
        {
            if (found[k].Count == 0) continue;
            var e = found[k][0];
            if (e.distance > TagRange) continue;
            e.occluded = Physics.Linecast(eye, e.point, out var hit, ~0, QueryTriggerInteraction.Ignore)
                && hit.distance < Vector3.Distance(eye, e.point) - .25f && !BelongsTo(hit.collider, e);
        }
    }

    bool BelongsTo(Collider c, Example e)
    {
        if (c == null) return false;
        if (c == e.collider) return true;
        if (e.door != null && map != null && map.DoorOf(c) == e.door) return true;
        return e.target != null && c.transform.IsChildOf(e.target.transform);
    }

    // ---------- jumps and the door break

    /// <summary>The player in front of the kind's index-th nearest example, facing it; false (and the reason on screen) when not.</summary>
    bool Jump(Kind kind, int index)
    {
        var list = found[(int)kind];
        if (player == null || list.Count == 0)
        {
            Toast(Label((int)kind), list.Count == 0 ? NotHereLine(kind) : "The run has not started.");
            return false;
        }
        // The start rooms belong to the title's stream until the start door is behind the player: walk out first.
        if (map.InStartArea(map.CellOf(player.position)))
        {
            Toast(Label((int)kind), "Walk through the start door first; jumps work on the floor itself.");
            return false;
        }
        index %= list.Count;
        var e = list[index];
        if (!StandFor(e, out var feet))
        {
            Toast(Label((int)kind), "No clear floor in front of it: walk there (" + Mathf.RoundToInt(e.distance) + " m).");
            return false;
        }
        var body = player.GetComponent<CharacterController>();
        var was = body != null && body.enabled;
        if (body != null) body.enabled = false;
        player.position = feet;
        if (body != null) body.enabled = was;
        Physics.SyncTransforms();
        var look = e.point - (feet + Vector3.up * ModuleUnits.PlayerEye);
        var flat = Flat(look);
        if (flat.sqrMagnitude > 1e-4f && YawField != null && YawField.FieldType == typeof(float))
            YawField.SetValue(game, Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg);
        if (PitchField != null && PitchField.FieldType == typeof(float))
            PitchField.SetValue(game, Mathf.Clamp(-Mathf.Atan2(look.y, Mathf.Max(.1f, flat.magnitude)) * Mathf.Rad2Deg, -45f, 45f));
        lastKind = kind;
        lastIndex = index;
        Toast(Label((int)kind) + (list.Count > 1 ? "  " + (index + 1) + " / " + list.Count : ""), HowTo[(int)kind]);
        ShowPlacard(FrontRoomsShowcaseTable.OfGuideKind((int)kind));
        scanClock = .1f;
        return true;
    }

    /// <summary>
    /// A standing spot in front of the example, on its facing, on the side the player is on when it is clear: 1.4 m from a
    /// door's edge, 1.0 m from a pane or a latch (glass breaks from 1.2 m), about a metre from a close-up's face, 0.5 m
    /// from the stair key (the take spot). Clear means a built map floor under it and nothing solid in the body's capsule;
    /// otherwise the other side, then eight ways round it.
    /// </summary>
    bool StandFor(Example e, out Vector3 feet)
    {
        feet = default;
        var current = Feet;
        Vector3 anchor;
        float[] distances;
        if (e.door != null)
        {
            anchor = (map.CellCenter(e.door.a) + map.CellCenter(e.door.b)) * .5f;
            distances = new[] { 1.4f, 1.0f };
        }
        else if (e.target != null)
        {
            anchor = e.target.Face.position;
            distances = new[] { 1.0f, .8f, 1.3f };
        }
        else
        {
            anchor = e.point;
            distances = e.kind == Kind.StairKey ? new[] { .5f, .8f } : new[] { 1.0f, .8f };
        }
        var normal = e.facing;
        if (normal.sqrMagnitude < 1e-6f) normal = Flat(current - anchor);
        if (normal.sqrMagnitude < 1e-6f) normal = Vector3.forward;
        normal.Normalize();
        // A close-up is read from in front of its face, the stair key from its take side; anything else first from the player's side.
        var faced = e.target != null || e.kind == Kind.StairKey;
        var first = faced || Vector3.Dot(current - anchor, normal) >= 0f ? normal : -normal;
        for (var d = 0; d < distances.Length; d++)
        for (var side = 0; side < (faced ? 1 : 2); side++)
            if (TryFloor(Flat(anchor) + (side == 0 ? first : -first) * distances[d], out feet)) return true;
        // A key on a desk, a latch in a corner: eight ways round it, nearer first.
        for (var ring = 0; ring < 2; ring++)
        for (var a = 0; a < 8; a++)
            if (TryFloor(Flat(anchor) + Quaternion.Euler(0f, a * 45f, 0f) * first * (ring == 0 ? .9f : 1.3f), out feet)) return true;
        return false;
    }

    /// <summary>
    /// The map's floor under a spot (its cell's floor height, so a ray never starts above a ceiling or settles on a desk),
    /// in a built chunk outside the stream rooms and the stairwell (reserved cells), with room for the body's capsule.
    /// </summary>
    bool TryFloor(Vector3 spot, out Vector3 feet)
    {
        feet = default;
        var cell = map.CellOf(spot);
        if (!map.IsChunkBuiltAt(cell) || map.IsReserved(cell)) return false;
        var floorY = map.CellCenter(cell).y;
        if (!Physics.Raycast(new Vector3(spot.x, floorY + 1.2f, spot.z), Vector3.down, out var hit, 2.5f, ~0, QueryTriggerInteraction.Ignore)) return false;
        if (hit.point.y > floorY + .35f || (player != null && hit.collider.transform.IsChildOf(player))) return false;
        feet = hit.point;
        var bottom = feet + Vector3.up * (BodyRadius + .06f);
        var top = feet + Vector3.up * (BodyHeight - BodyRadius);
        var blocking = Physics.OverlapCapsuleNonAlloc(bottom, top, BodyRadius, hits, ~0, QueryTriggerInteraction.Ignore);
        for (var i = 0; i < blocking; i++)
            if (hits[i] != null && (player == null || !hits[i].transform.IsChildOf(player))) return false;
        return true;
    }

    void BreakDoorInView()
    {
        if (map == null || view == null) return;
        FrontRoomsMapWorld.Door door = null;
        if (Physics.Raycast(view.ViewportPointToRay(new Vector3(.5f, .5f, 0f)), out var hit, 6f, ~0, QueryTriggerInteraction.Ignore))
            door = map.DoorOf(hit.collider);
        if (door == null)
        {
            // Else the nearest door within 4 m that still stands.
            var list = found[(int)(map.BodyDoors ? Kind.BodyDoor : Kind.Door)];
            for (var i = 0; i < list.Count && door == null; i++) if (list[i].distance < 4f) door = list[i].door;
        }
        if (door == null || door.broken || map.IsExitDoor(door))
        {
            Toast(Names[(int)Kind.BrokenDoor], door != null && map.IsExitDoor(door) ? "Not the EXIT: the Relay never breaks it." : "Look at a door within 6 m, then B.");
            return;
        }
        // From the player's side, so the leaf is thrown away from the body (the Relay breaks into the room it stands in).
        map.BreakDoor(door, Feet);
        Toast(Names[(int)Kind.BrokenDoor], HowTo[(int)Kind.BrokenDoor]);
        scanClock = .2f;
    }

    string NotHereLine(Kind kind)
    {
        var where = Where[(int)kind];
        // In no tour (the terminal): HomeOf would name the keys floor.
        if (where == Floors.None) return "In no tour yet: level design has not picked the terminal desks (L2).";
        if ((where & ThisFloor) == 0)
        {
            var home = HomeOf(where);
            return "Not on this floor: " + TourName(home) + " (F2, then " + (home == Tour.Shipped ? "2" : home == Tour.Devices ? "3" : "4") + ").";
        }
        if (kind == Kind.BrokenDoor) return "None yet: look at a door and press B.";
        if (kind == Kind.DevCloseUp && !devTargets) return "V turns them on; rooms built after that carry them.";
        if (kind == Kind.RunDoor) return "Forced here, 70-120 m out: walk on.";
        return "None near yet: walk on, the floor builds round you.";
    }

    string StatusWhenNone(Kind kind)
    {
        var where = Where[(int)kind];
        if (where == Floors.None) return "NOT IN THE GAME YET";
        if ((where & ThisFloor) == 0) return TourName(HomeOf(where));
        if (kind == Kind.BrokenDoor) return "B MAKES ONE";
        return "NOT FOUND YET";
    }

    void Toast(string title, string line)
    {
        toastTitle = title;
        toastLine = line;
        toastUntil = Time.unscaledTime + 4f;
        // A station's placard lines come back only with ShowPlacard.
        toastWhat = toastDecide = null;
    }

    static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

    // ---------- drawing (the HUD's own faces and keyline: Bayon for names, IBM Plex Mono for lines; no backing card)

    void EnsureStyles()
    {
        var scale = Mathf.Max(.6f, Screen.height / 1080f);
        if (titleStyle != null && Mathf.Approximately(scale, styleScale)) return;
        styleScale = scale;
        if (bayon == null) bayon = Resources.Load<Font>("Fonts/Bayon-Regular");
        if (mono == null) mono = Resources.Load<Font>("Fonts/IBMPlexMono-Regular");
        menuTitleStyle = Style(bayon, 44f * scale, TextAnchor.UpperLeft);
        menuNameStyle = Style(bayon, 28f * scale, TextAnchor.UpperLeft);
        titleStyle = Style(bayon, 30f * scale, TextAnchor.UpperLeft);
        nameStyle = Style(bayon, 21f * scale, TextAnchor.UpperLeft);
        lineStyle = Style(mono, 15f * scale, TextAnchor.UpperLeft);
        smallStyle = Style(mono, 13f * scale, TextAnchor.UpperLeft);
        statusStyle = Style(mono, 13f * scale, TextAnchor.UpperRight);
        toastTitleStyle = Style(bayon, 30f * scale, TextAnchor.UpperCenter);
        toastLineStyle = Style(mono, 16f * scale, TextAnchor.UpperCenter);
        EnsureZhStyles(scale);
        if (white == null)
        {
            white = new Texture2D(1, 1, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            white.SetPixel(0, 0, Color.white);
            white.Apply();
        }
    }

    static GUIStyle Style(Font font, float size, TextAnchor anchor) => new GUIStyle(GUI.skin.label)
    {
        font = font,
        fontSize = Mathf.RoundToInt(size),
        wordWrap = true,
        richText = false,
        alignment = anchor,
        clipping = TextClipping.Overflow,
        padding = new RectOffset(0, 0, 0, 0),
        margin = new RectOffset(0, 0, 0, 0)
    };

    static readonly Color Paper = new Color(244f / 255f, 241f / 255f, 232f / 255f, 1f);   // FrontRoomsHudStyle.TextPaper
    static readonly Color Accent = new Color(244f / 255f, 223f / 255f, 59f / 255f, 1f);   // #F4DF3B, the brand accent on screen
    static readonly Color Ink = new Color(20f / 255f, 20f / 255f, 20f / 255f, 1f);        // FrontRoomsHudStyle.KeylineColor

    static void Text(Rect r, string s, GUIStyle style, Color color, float alpha = 1f)
    {
        // The HUD's keyline: the ink at 55 % one pixel round the letters, so they read on any wall without a card.
        style.normal.textColor = new Color(Ink.r, Ink.g, Ink.b, .55f * alpha);
        for (var dx = -1; dx <= 1; dx++)
        for (var dy = -1; dy <= 1; dy++)
            if (dx != 0 || dy != 0) GUI.Label(new Rect(r.x + dx, r.y + dy, r.width, r.height), s, style);
        style.normal.textColor = new Color(color.r, color.g, color.b, color.a * alpha);
        GUI.Label(r, s, style);
    }

    static void Fill(Rect r, Color c)
    {
        var old = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, white);
        GUI.color = old;
    }

    void OnGUI()
    {
        if (!Application.isPlaying || Event.current.type != EventType.Repaint) return;
        EnsureStyles();
        var s = styleScale;
        var margin = 72f * s;
        if (tour == Tour.Menu)
        {
            DrawMenu(margin, s);
            return;
        }
        if (tour == Tour.MediaBay)
        {
            Text(new Rect(18f, 72f * s, 600f * s, 24f * s), "F2 · TOURS", lineStyle, Paper, .85f);
            return;
        }
        if (tour == Tour.MuseumPlan)
        {
            DrawPlan(margin, s);
            return;
        }
        if (tour == Tour.Museum)
        {
            // The built museum: its framed codes, room list and placard, or the plan over the game with P (.Museum.cs).
            if (!paused && !FrontRooms3DGame.InspectingNow) DrawMuseum(margin, s);
            return;
        }
        if (paused || FrontRooms3DGame.InspectingNow) return;
        var w = 560f * s;
        var x = Screen.width - margin - w;
        var y = margin;
        if (map == null)
        {
            Text(new Rect(x, y, w, 40f * s), TourName(tour), titleStyle, Paper);
            Text(new Rect(x, y + 40f * s, w, 40f * s), "SPACE starts the run (the game's own title)  ·  F2 TOURS", lineStyle, Paper, .9f);
            if (problem != null) Text(new Rect(x, y + 70f * s, w, 60f * s), problem, smallStyle, Accent);
            if (tour == Tour.Rooms) DrawRoomsTitle(x, y + 136f * s, w, s);
            return;
        }
        if (tour == Tour.Rooms)
        {
            // ROOM TYPES: the R list on the right and the station card on the left; no kind tags (.Rooms.cs).
            if (listShown)
            {
                DrawRoomsList(x, y, w, s);
                DrawRoomCard(margin, s);
            }
            else Text(new Rect(x, y, w, 24f * s), "G · ROOM TYPES", lineStyle, Paper, .85f);
        }
        else
        {
            DrawTags(s);
            if (listShown) DrawList(x, y, w, s);
            else Text(new Rect(x, y, w, 24f * s), "G · INTERACTABLES", lineStyle, Paper, .85f);
        }
        if (Time.unscaledTime < toastUntil && toastTitle != null)
        {
            // Its title and line, then a station's two placard lines in Chinese when set (DrawToast, .Plan.cs).
            var tw = Mathf.Min(820f * s, Screen.width - 2f * margin);
            DrawToast((Screen.width - tw) * .5f, Screen.height * .2f, tw, s);
        }
    }

    void DrawMenu(float margin, float s)
    {
        var x = margin;
        var y = 128f * s;
        var w = Mathf.Min(980f * s, Screen.width - 2f * margin);
        Text(new Rect(x, y, w, 60f * s), "FRONTROOMS · INTERACTABLES", menuTitleStyle, Paper);
        y += 58f * s;
        Text(new Rect(x, y, w, 40f * s), "An example of every interactable, tried with the game's own controls and code.", lineStyle, Paper, .85f);
        y += 52f * s;
        y = MenuRow(x, y, w, s, "1", "MEDIA BAY", "Here: the CRT screen ads on their own light rig (WASD, mouse, E toggles a screen).");
        y = MenuRow(x, y, w, s, "2", "FLOOR 1 · AS SHIPPED", "The game exactly as it ships, with the guide on top: the station, stair key, EXIT chain and U1 fall, TV, placard, body doors, windows, door lites.");
        y = MenuRow(x, y, w, s, "3", "FLOOR 1 · EVERY DEVICE", "The same floor with what the shipped profile keeps off: sash latches, foiled windows, alarm doors, Run! rooms.");
        y = MenuRow(x, y, w, s, "4", "KEYS FLOOR", "No station: E doors, locked doors, zone keys, key tags and the unlock shot.");
        y = MenuRow(x, y, w, s, "5", "MUSEUM", "Built in the game from its own cells and doors: hub 0 and four rooms, room 1's doors live, rooms 2-4 as framed codes. P on its hub board opens the plan (P here: the plan alone, no game).");
        y = MenuRow(x, y, w, s, "6", "ROOM TYPES", "Every kind of room the game builds (R0-R13) on a floor tilted toward variety: zones, rooms, columns, piles, borders, dark lamps, Run!, the stairwell, the B1 station.");
        y += 12f * s;
        var keysLine = "In a tour: G list  ·  1-0 jump  ·  [ ] page  ·  J next one  ·  H Relay held / live  ·  B break a door  ·  V dev close-ups  ·  F2 tours"
            + "  ·  ROOM TYPES: K the B1 route, L a dark patch lit, N on its title the B1 station"
            + "  ·  MUSEUM: [ ] room, 1-7 station, 0 hub, R reset, B break, P plan";
        var h = smallStyle.CalcHeight(new GUIContent(keysLine), w);
        Text(new Rect(x, y, w, h), keysLine, smallStyle, Paper, .8f);
        y += h + 10f * s;
        var notLine = "Not in any tour: the office terminal (no game code places one yet), lockers, pools, bathrooms, slides and walkable stairs (none in the game). The ALARM ZONE card hangs at every Run! room in tour 3.";
        Text(new Rect(x, y, w, smallStyle.CalcHeight(new GUIContent(notLine), w)), notLine, smallStyle, Paper, .6f);
    }

    float MenuRow(float x, float y, float w, float s, string key, string name, string line)
    {
        // The pause card's marker (A1): a 4-wide yellow bar left of the line, as tall as the caps.
        Fill(new Rect(x, y + 9f * s, 4f * s, 18f * s), Accent);
        Text(new Rect(x + 16f * s, y, 40f * s, 36f * s), key, menuNameStyle, Accent);
        Text(new Rect(x + 52f * s, y, w - 52f * s, 36f * s), name, menuNameStyle, Paper);
        var h = lineStyle.CalcHeight(new GUIContent(line), w - 52f * s);
        Text(new Rect(x + 52f * s, y + 36f * s, w - 52f * s, h), line, lineStyle, Paper, .85f);
        return y + 36f * s + h + 18f * s;
    }

    void DrawList(float x, float y, float w, float s)
    {
        Text(new Rect(x, y, w, 40f * s), TourName(tour) + (floor > 1 ? "  ·  LEVEL B" + (floor - 1) : ""), titleStyle, Paper);
        y += 38f * s;
        var meta = "RELAY " + (holdRelay ? "HELD" : "LIVE") + " (H)  ·  DEV " + (devTargets ? "ON" : "OFF") + " (V)  ·  PAGE " + (page + 1) + "/" + PageCount + " [ ]  ·  G HIDE  ·  F2 TOURS";
        Text(new Rect(x, y, w, 22f * s), meta, smallStyle, Paper, .8f);
        y += 26f * s;
        y = DrawWaiting(x, y, w, s);
        if (problem != null)
        {
            var ph = smallStyle.CalcHeight(new GUIContent(problem), w);
            Text(new Rect(x, y, w, ph), problem, smallStyle, Accent);
            y += ph + 8f * s;
        }
        var first = page * PageSize;
        for (var i = 0; i < PageSize && first + i < KindCount; i++)
        {
            var k = first + i;
            var list = found[k];
            var here = list.Count > 0;
            var alpha = here ? 1f : .45f;
            var marked = lastIndex >= 0 && (int)lastKind == k;
            if (marked) Fill(new Rect(x - 14f * s, y + 6f * s, 4f * s, 14f * s), Accent);
            Text(new Rect(x, y, 24f * s, 26f * s), i == 9 ? "0" : (i + 1).ToString(), nameStyle, here ? Accent : Paper, alpha);
            Text(new Rect(x + 28f * s, y, w - 228f * s, 26f * s), Label(k), nameStyle, Paper, alpha);
            var status = here ? Mathf.RoundToInt(list[0].distance) + " M" + (list.Count > 1 ? "  ·  " + list.Count : "") : StatusWhenNone((Kind)k);
            Text(new Rect(x + w - 200f * s, y + 5f * s, 200f * s, 22f * s), status, statusStyle, Paper, here ? .9f : .5f);
            y += 28f * s;
            if (marked)
            {
                var h = smallStyle.CalcHeight(new GUIContent(HowTo[k]), w - 28f * s);
                Text(new Rect(x + 28f * s, y - 2f * s, w - 28f * s, h), HowTo[k], smallStyle, Paper, .85f);
                y += h + 6f * s;
                y = DrawRowDecide(k, x + 28f * s, y, w - 28f * s, s);
            }
        }
    }

    void DrawTags(float s)
    {
        if (view == null) return;
        var first = page * PageSize;
        var dot = 6f * s;
        for (var k = 0; k < KindCount; k++)
        {
            if (found[k].Count == 0) continue;
            var e = found[k][0];
            if (e.distance > TagRange || e.distance < .6f) continue;
            var sp = view.WorldToScreenPoint(e.point);
            if (sp.z <= .1f) continue;
            var gx = sp.x;
            var gy = Screen.height - sp.y;
            if (gx < 0f || gx > Screen.width || gy < 0f || gy > Screen.height) continue;
            var a = Mathf.Clamp01((TagRange - e.distance) / 6f) * (e.occluded ? .4f : 1f);
            Fill(new Rect(gx - dot * .5f - 1f, gy - dot * .5f - 1f, dot + 2f, dot + 2f), new Color(Ink.r, Ink.g, Ink.b, .55f * a));
            Fill(new Rect(gx - dot * .5f, gy - dot * .5f, dot, dot), new Color(Accent.r, Accent.g, Accent.b, a));
            var onPage = k >= first && k < first + PageSize;
            var label = (onPage ? (k - first == 9 ? "0" : (k - first + 1).ToString()) + "  " : "") + Label(k);
            Text(new Rect(gx + dot + 2f * s, gy - 9f * s, 360f * s, 22f * s), label, smallStyle, Paper, a);
        }
    }
}
