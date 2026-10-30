using System.Collections.Generic;
using FrontRooms.Map;
using Unity.Collections;
using UnityEngine;

/// <summary>
/// The wallpaper cue's writer (A + M4 + V1; Documentation/research/cue_driver/CUE_DRIVER_INTERFACE.md v0.2.4, the
/// pursuit rules of RELAY_PURSUIT_REDESIGN.md 9.6 and G7): a state machine per map cell that turns the arrows of the
/// walls along the route (FrontRoomsWayfinding) and steps their rows, written to the shader as globals:
///   _FR_WayCells  64 x 64 RGBA8, one texel per cell, toroidal (cell.x &amp; 63, cell.y &amp; 63): R from-state, G
///                 to-state, B the cross-fade (0..255), A route dir + 4 x message
///   _FR_WayOrigin xy the world x/z of the map root (cell 0's corner; whole 192 m periods from any other), z 3 m, w 1
/// States: 0 the live print, 1-3 the three 30-degree turn clicks (3 fitted), 4-8 the flow clicks (6 a loop).
/// - Stage 1 (WarnStageChanged 0 to 1 or 2): the room set's cells (FrontRoomsWayfinding.RoomSet, the warning lamps'
///   own) turn on the calm route, clicks at +0.20 / +0.50 / +0.80 s, no stepping; never a cell within 2 cells of the
///   Relay or in its room, tested once, as a cell starts to turn (a turned cell holds when it comes near; one held
///   back is not tried again while it stays in the set, so its turning never reads out the Relay leaving). The set
///   follows the player: cells it leaves turn back, cells it gains turn. 2 to 1 changes nothing; 0 turns back every
///   cell that is not retracting (0.15 s clicks).
/// - Chase (StateChanged(Chase); idempotent, BreakDoor inside it changes nothing): a wave leaves the room set at
///   8 m/s over open ways (cells no open way reaches: 1.4 x their straight distance). A cell turns as it arrives
///   (0.12 s clicks) on the pressure route where it reaches while a door qualifies, else the calm one
///   (FrontRoomsWayfinding.ChaseRoute), then steps 4 clicks a second. No Relay exclusion: in a chase the player has
///   seen it. A row steps only where FrontRoomsWayfinding.StepAllowed says so (系统设计's 9.6 ruling, 2026-10-22: a
///   stepping row never urges the player toward the Relay): with no door that qualifies the room set keeps its calm
///   turn and no row steps, though the wave still turns the cells; a row whose way on comes to lead toward the Relay
///   as it moves stops on its fitted state while the wave runs (the pressure route is kept current through the wave).
///   Through the retract a row finishes its loop unasked (interface 3; review m3 is open with 平面视觉 and 系统设计).
/// - Any exit from {Chase, BreakDoor}: the wave retracts outside-in over 2 s; each cell finishes its loop, then
///   turns back (0.15 s clicks), except the room set while the stage is 1 or 2, which lands
///   turned on the calm route (turning back first when the pressure dir differs; a wave turn waiting on a turn-back
///   starts one back-click after it, so the last fade to the print finishes). Cells the wave had not reached stay put.
/// - Precedence: Caught (freeze: no new clicks, a cross-fade under way finishes) over the wave over the stage; the
///   stage seen during a wave is applied as the retract lands.
/// - Reduce Motion (CAMERA MOTION off, FrontRoomsPrintDriver's switch) and the WebGL tier: turn only, no flow clicks,
///   the wave a one-time turn as it passes. Same states, same timing.
/// - Reset to all 0 on a run start, a floor change (FloorChanged: the new floor's EXIT, its station only if one was
///   set for that EXIT, the key not taken, no tie memory), the Relay parked or not yet out (StateChanged(Dormant): its
///   warning metrics reset) and a dropped or re-windowed cell (a field rebuild after streaming). Game time in
///   LateUpdate (after the map and the Relay have ticked; nothing while paused); TickForTools(dt) runs the same step.
///   Started from FrontRooms3DGame.MapRunStarted by a static hook (no game-file edit, registered at
///   SubsystemRegistration, so it binds before any listener subscribed later in the play), on the map's object, so it
///   goes with the map.
/// - M5 (tutorial 20_requests.md; MAP_ROUTE_SPEC 1, 5; A20): while the map holds a station (FloorHasStation and
///   HasStation: A26's floor-1 start group) the field's station target is its TV, StationBeaconCell on the edge
///   StationBeaconEdge, so the arrows and BeaconPath lead to the TV; the map's StationKeyTaken (its StationKeyIsTaken
///   read each step as a net) turns the key taken, and the route to the EXIT's N. Taken up at Bind and whenever the map
///   takes a station; dropped when it lets it go (ClearStation, NextFloor). A station sent by SetStationTarget (tools,
///   a later floor's M4) is left alone. No station on the map: nothing changes.
/// It reads no lamp levels (the full and near-only fixture paths behave the same) and never times a click by the
/// Relay's speed or distance. While any cell is off 0 it holds the ambient print on K00 (FrontRoomsPrintDriver
/// SetHold "cue"), since the cue slices are authored on K00. Past FrontRoomsWayfinding.MaxBuildRadius the cue is off
/// (_FR_WayOrigin.w 0): the 64-cell window no longer holds every built cell.
/// </summary>
public sealed class FrontRoomsWayCueDriver : MonoBehaviour
{
    public const int Side = FrontRoomsWayfinding.Side, Slots = FrontRoomsWayfinding.Slots;
    /// <summary>SP: while a chase runs, the pressure route is rebuilt at most this often as the Relay moves; cells take it as the wave reaches them.</summary>
    public const float PressureRefreshSeconds = .25f;
    /// <summary>The wave's step count for a cell no open way reaches from the room set: its straight distance in cells times this (the preview's rule).</summary>
    public const float UnreachedWaveFactor = 1.4f;
    /// <summary>Stage turns keep this far (cells, Chebyshev) from the Relay, as the warning lamps do.</summary>
    public const int RelayClearCells = 2;

    const byte Idle = 0, Up = 1, Held = 2, Flow = 3, Back = 4;
    const byte StageTurn = 0, WaveTurn = 1;
    const byte RetractNone = 0, RetractBack = 1, RetractStage = 2;
    const float Never = float.PositiveInfinity;

    static readonly int CellsId = Shader.PropertyToID("_FR_WayCells");
    static readonly int OriginId = Shader.PropertyToID("_FR_WayOrigin");

    /// <summary>The run's driver (null outside a map run): M4/M6 set the station on its Way, M5 reads BeaconPath there.</summary>
    public static FrontRoomsWayCueDriver Current { get; private set; }
    /// <summary>Tests: force Reduce Motion on or off (null: the setting).</summary>
    public static bool? ReduceMotionForTools { get; set; }
    /// <summary>The chase's pressure route (on); off, a chase keeps the calm route.</summary>
    public static bool UsePressureRoute { get; set; } = true;

#if UNITY_WEBGL && !UNITY_EDITOR
    static bool WebGLTier => true;
#else
    static bool WebGLTier => false;
#endif

    /// <summary>Turn only: Reduce Motion (the same switch as FrontRoomsPrintDriver.ReduceMotion) or the WebGL tier.</summary>
    public static bool TurnOnly => (ReduceMotionForTools ?? FrontRoomsSettings.CameraMotionPercent == 0) || WebGLTier;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Boot()
    {
        // Once per Play (with or without a domain reload): one subscription.
        FrontRooms3DGame.MapRunStarted -= OnMapRunStarted;
        FrontRooms3DGame.MapRunStarted += OnMapRunStarted;
        Current = null;
    }

    static void OnMapRunStarted(FrontRoomsMapWorld map, FrontRoomsMapHunter relay)
    {
        if (map == null) return;
        var driver = map.GetComponent<FrontRoomsWayCueDriver>();
        if (driver == null) driver = map.gameObject.AddComponent<FrontRoomsWayCueDriver>();
        driver.Bind(map, relay);
    }

    FrontRoomsMapWorld world;
    FrontRoomsMapHunter hunter;
    Texture2D tex;
    NativeArray<Color32> px;
    bool texDirty, originSet, holdSet, bound, tooWide;
    Vector3 origin;
    float clock, nextLog;
    int stage, waveId;
    bool wave, frozen, paused;
    // The EXIT the station was set under (M4 may set it from a FloorChanged handler that runs before the driver's).
    GridCoord? stationExit;
    // M5: the field's station is the map's own (SyncMapStation), not one sent by SetStationTarget.
    bool mapStation;

    // Per slot (cell & 63): the cell it holds and that cell's machine.
    readonly GridCoord[] cell = new GridCoord[Slots];
    readonly bool[] used = new bool[Slots], isActive = new bool[Slots], flowAfter = new bool[Slots], pending = new bool[Slots], pflow = new bool[Slots];
    readonly byte[] from = new byte[Slots], to = new byte[Slots], mode = new byte[Slots], kind = new byte[Slots], cdir = new byte[Slots];
    readonly byte[] clicks = new byte[Slots], retractMode = new byte[Slots], pdir = new byte[Slots], pkind = new byte[Slots];
    readonly float[] fadeAt = new float[Slots], baseAt = new float[Slots], next = new float[Slots], arrive = new float[Slots], retractAt = new float[Slots];
    readonly int[] flowClicks = new int[Slots], waveTag = new int[Slots], waveDist = new int[Slots];
    readonly int[] active = new int[Slots];
    int activeCount;
    // Room-set cells the Relay exclusion held back at their turn start: not tried again while they stay in the set
    // (cleared on a stage entry or turn-back, a retract's landing, and when the cell leaves the set).
    readonly bool[] excluded = new bool[Slots];
    readonly GridCoord[] excludedCell = new GridCoord[Slots];

    // The room set, and which slots are in it.
    List<GridCoord> room = new List<GridCoord>(64), oldRoom = new List<GridCoord>(64);
    readonly int[] roomMark = new int[Slots];
    readonly GridCoord[] roomMarkCell = new GridCoord[Slots];
    GridCoord roomFor;
    bool roomValid;
    int roomBuild, roomStamp;

    // The wave's walk from the room set.
    readonly int[] bfsQueue = new int[Slots], bfsStamp = new int[Slots], bfsDist = new int[Slots];
    int bfsId;

    // The Relay as a stage turn reads it (once per step), and the pressure route's last build.
    float relayAt = -1f;
    GridCoord relayCell;
    int relayRoomX, relayRoomY, relayRoomW, relayRoomH;
    bool relayOn;
    float pressureAt = float.NegativeInfinity;
    GridCoord pressureRelay, pressurePlayer;
    int pressureRevision = -1;

    readonly System.Diagnostics.Stopwatch stepWatch = new System.Diagnostics.Stopwatch();

    public FrontRoomsWayfinding Way { get; private set; }
    public FrontRoomsMapWorld World => world;
    public int Stage => stage;
    public bool WaveActive => wave;
    public bool Frozen => frozen;
    public float Clock => clock;
    public int ActiveCells => activeCount;
    public IReadOnlyList<GridCoord> Room => room;
    public Texture2D CellsTexture => tex;
    /// <summary>The slowest Step so far (field rebuilds included), in ms.</summary>
    public float MaxStepMs { get; private set; }

    /// <summary>
    /// M4/M6 (A20): the floor's station, the TV's cell and the cell edge it hangs on (-1: none); the arrows then lead to
    /// the TV until StationKeyTaken, then to the EXIT. Set it once the floor's EXIT is planned: at MapRunStarted (the
    /// driver is bound by then) or from FloorChanged, in either order with the driver's own handler: the station is
    /// kept by the EXIT it was set under, so a floor change drops only one left from the floor before (review m6).
    /// Sending it again on the same floor never un-takes the key (m5).
    /// </summary>
    public void SetStationTarget(GridCoord? tvCell, int tvEdgeDir)
    {
        if (Way == null) return;
        stationExit = ExitNow;
        Way.SetStationTarget(tvCell, tvEdgeDir);
    }

    GridCoord? ExitNow => world != null && world.HasExitArea ? world.ExitCell : (GridCoord?)null;

    /// <summary>M6: the floor key is taken (A20): the field switches to the EXIT on the next step, and the room set re-points.</summary>
    public void StationKeyTaken()
    {
        if (Way != null) Way.SetStationKeyTaken(true);
    }

    /// <summary>M5: the field's station target is the map's station (SyncMapStation), set from FloorHasStation and HasStation.</summary>
    public bool StationFromMap => mapStation;

    // M5: follow the map's station. It takes one (Bind, SetStation): the TV's cell and edge, under the EXIT now; it lets it
    // go (ClearStation, NextFloor): no station, the key untaken. The key from the map's take, raised (OnStationKeyTaken) or
    // read (StationKeyIsTaken, each step: a take before the driver heard it). Cheap: two bools a step while nothing moves.
    void SyncMapStation()
    {
        if (Way == null || world == null) return;
        var has = world.FloorHasStation && world.HasStation;
        if (has != mapStation)
        {
            mapStation = has;
            if (has)
            {
                stationExit = ExitNow;
                var tv = world.StationBeaconCell;
                var edge = world.StationBeaconEdge;
                Way.SetStationTarget(tv, edge);
                Debug.Log("[FrontRoomsWayCue] station (M5): the route leads to the TV in " + tv + (edge >= 0 ? ", on its " + "+x+z-x-z".Substring(edge * 2, 2) + " wall" : ", on no wall")
                    + " until the floor key is taken, then to the EXIT's N");
            }
            else
            {
                Way.ClearStation();
                stationExit = null;
            }
        }
        if (has && world.StationKeyIsTaken && !Way.StationKeyTaken)
        {
            Way.SetStationKeyTaken(true);
            Debug.Log("[FrontRoomsWayCue] station key taken (M5, A20): the route leads to the EXIT's N, the beacon is off");
        }
    }

    void OnStationKeyTaken(Vector3 at) => SyncMapStation();

    /// <summary>Bind to a map run: the field, the texture and the globals, the Relay's events, the game's floor and pause events.</summary>
    public void Bind(FrontRoomsMapWorld map, FrontRoomsMapHunter relay)
    {
        Unbind();
        world = map;
        hunter = relay;
        bound = true;
        Way = new FrontRoomsWayfinding(new FrontRoomsWayfinding.WorldMap(map));
        if (relay != null) Way.RelayDistance = relay.PathDistanceFromRelay;
        tex = new Texture2D(Side, Side, TextureFormat.RGBA32, false, true)
        {
            name = "FR_WayCells", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat, hideFlags = HideFlags.DontSave,
        };
        px = tex.GetPixelData<Color32>(0);
        if (relay != null)
        {
            relay.WarnStageChanged += OnWarnStage;
            relay.StateChanged += OnHunterState;
            relay.Caught += OnCaught;
        }
        FrontRooms3DGame.FloorChanged += OnFloorChanged;
        FrontRooms3DGame.Paused += OnPaused;
        FrontRooms3DGame.MapRunEnded += OnRunEnded;
        map.StationKeyTaken += OnStationKeyTaken;
        Current = this;
        clock = 0f;
        nextLog = 60f;
        frozen = paused = tooWide = false;
        stationExit = null;
        mapStation = false;
        SyncMapStation();
        ResetCells();
        stage = relay != null && relay.Released ? relay.WarnStage : 0;
        Shader.SetGlobalTexture(CellsId, tex);
        // The cue array (_FR_Cue) and its switch (_FR_CueReady) are the visual half's (Scripts/Rendering/FrontRoomsCueInk.cs):
        // it loads in the background and the walls follow the cells from the frame it is bound. False (WebGL, phones, no print
        // driver) keeps every wall on the plain print; the cells, the hold and the beacon path run the same either way.
        FrontRoomsCueInk.Bind();
        originSet = false;
        if (map.transform.rotation != Quaternion.identity)
            Debug.LogWarning("[FrontRoomsWayCue] the map root is rotated (" + map.transform.eulerAngles + "): _FR_WayOrigin assumes world x/z = map x/z, as the game places it");
        Debug.Log("[FrontRoomsWayCue] on: map seed " + map.Seed + ", " + Side + " x " + Side + " toroidal cells, turn only " + TurnOnly + ", pressure route " + UsePressureRoute);
    }

    /// <summary>Let go of the run: events, the print hold, the texture; the shader's w goes to 0 (the plain print).</summary>
    public void Unbind()
    {
        if (!bound) return;
        bound = false;
        if (hunter != null)
        {
            hunter.WarnStageChanged -= OnWarnStage;
            hunter.StateChanged -= OnHunterState;
            hunter.Caught -= OnCaught;
        }
        FrontRooms3DGame.FloorChanged -= OnFloorChanged;
        FrontRooms3DGame.Paused -= OnPaused;
        FrontRooms3DGame.MapRunEnded -= OnRunEnded;
        if (world != null) world.StationKeyTaken -= OnStationKeyTaken;
        mapStation = false;
        if (Way != null) LogStats("off");
        if (holdSet)
        {
            var print = FrontRoomsPrintDriver.Instance;
            if (print != null) print.SetHold("cue", false);
            holdSet = false;
        }
        if (Current == this)
        {
            Current = null;
            Shader.SetGlobalVector(OriginId, Vector4.zero);
            Shader.SetGlobalTexture(CellsId, Texture2D.blackTexture);
            FrontRoomsCueInk.Unbind();   // _FR_CueReady 0; the array stays loaded for the next run
        }
        if (tex != null)
        {
            if (Application.isPlaying) Destroy(tex); else DestroyImmediate(tex);
            tex = null;
        }
        world = null;
        hunter = null;
        Way = null;
    }

    void OnDestroy() => Unbind();

    void OnRunEnded() => Unbind();

    void OnPaused(bool on) => paused = on;

    void OnWarnStage(int s) => SetStage(s);

    void OnHunterState(HunterState s) => SetHunterState(s);

    void OnCaught() => frozen = true;

    void OnFloorChanged(FrontRoomsMapWorld map, FrontRoomsMapHunter relay, int floor)
    {
        if (map != world) return;
        LogStats("floor " + floor);
        ResetForFloor(false);
        // M5: the new floor's station, if the map holds one by now (else the next step takes it up).
        SyncMapStation();
    }

    void LateUpdate()
    {
        if (!bound || paused) return;
        Step(Mathf.Min(Time.deltaTime, .1f));
    }

    // ---------- Tools (the same paths as the events and the frame) ----------

    /// <summary>Tools and captures: one step of dt seconds, exactly as LateUpdate runs it.</summary>
    public void TickForTools(float dt) => Step(dt);
    public void WarnStageForTools(int s) => SetStage(s);
    public void HunterStateForTools(HunterState s) => SetHunterState(s);
    public void CaughtForTools() => frozen = true;
    /// <summary>Tools: what a floor change does (every cell to 0, no station, the key not taken, no tie memory, the field rebuilt for the map's EXIT).</summary>
    public void ResetForTools() => ResetForFloor(true);

    /// <summary>Tools: the current wave's number (a new chase counts one up; BreakDoor and a repeated Chase do not).</summary>
    public int WaveIdForTools => waveId;

    /// <summary>Tools: a cell's from- and to-state (0, 0 when no machine holds it).</summary>
    public int StateOf(GridCoord c)
    {
        var s = FrontRoomsWayfinding.Slot(c);
        return used[s] && cell[s] == c ? to[s] : 0;
    }

    /// <summary>Tools: the texel written for a cell's slot (what the shader reads after the next Apply).</summary>
    public Color32 TexelOf(GridCoord c) => tex != null ? px[FrontRoomsWayfinding.Slot(c)] : default;

    /// <summary>Tools: when a cell's retract is due (+inf: none), and whether its machine is stepping its rows.</summary>
    public float RetractAtForTools(GridCoord c)
    {
        var s = FrontRoomsWayfinding.Slot(c);
        return used[s] && cell[s] == c ? retractAt[s] : Never;
    }

    public bool SteppingForTools(GridCoord c)
    {
        var s = FrontRoomsWayfinding.Slot(c);
        return used[s] && cell[s] == c && mode[s] == Flow;
    }

    /// <summary>Tools: the wave's step count to a cell from the room set (its arrival is that many cells at 8 m/s), or -1 when this wave took no machine there.</summary>
    public int WaveStepsForTools(GridCoord c)
    {
        var s = FrontRoomsWayfinding.Slot(c);
        return used[s] && cell[s] == c && waveTag[s] == waveId && waveId > 0 ? waveDist[s] : -1;
    }

    /// <summary>Tools: the dir a cell's machine is turned to (-1 while it is at rest).</summary>
    public int TurnedDir(GridCoord c)
    {
        var s = FrontRoomsWayfinding.Slot(c);
        return used[s] && cell[s] == c && mode[s] != Idle ? cdir[s] : -1;
    }

    // ---------- The step ----------

    void Step(float dt)
    {
        if (!bound || world == null || world.Cache == null || Way == null) return;
        stepWatch.Restart();
        clock += Mathf.Max(0f, dt);
        // Past MaxBuildRadius built cells 64 apart would share a texel (wrong arrows on far walls): the cue is off.
        var wide = world.BuildRadius > FrontRoomsWayfinding.MaxBuildRadius;
        if (wide != tooWide)
        {
            tooWide = wide;
            originSet = false;
            if (wide) Debug.LogWarning("[FrontRoomsWayCue] buildRadius " + world.BuildRadius + " is past the cue window's " + FrontRoomsWayfinding.MaxBuildRadius + ": the cue is off until it comes back");
        }
        SyncMapStation();
        Way.SetExit(ExitNow);
        if (Way.Refresh()) FieldRebuilt();
        if (!frozen)
        {
            UpdateRoom();
            if (wave) UpdatePressure(false);
        }
        Advance();
        Flush();
        var ms = (float)stepWatch.Elapsed.TotalMilliseconds;
        if (ms > MaxStepMs) MaxStepMs = ms;
        if (clock >= nextLog)
        {
            nextLog = clock + 60f;
            LogStats("t " + clock.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + " s");
        }
    }

    void SetStage(int value)
    {
        value = Mathf.Clamp(value, 0, 2);
        var was = stage;
        stage = value;
        // Caught over the wave over the stage: a stage seen during a wave is applied as the retract lands.
        if (!bound || frozen || wave) return;
        if (was == 0 && value >= 1)
        {
            System.Array.Clear(excluded, 0, Slots);
            UpdateRoom();
            for (var i = 0; i < room.Count; i++) TryTurn(room[i], false);
        }
        else if (was >= 1 && value == 0)
        {
            System.Array.Clear(excluded, 0, Slots);
            BackAll();
        }
    }

    void SetHunterState(HunterState s)
    {
        if (!bound || frozen) return;
        // Parked (a floor change) or placed back before its release: its warning metrics were reset, so are the walls.
        if (s == HunterState.Dormant)
        {
            ResetCells();
            stage = 0;
            return;
        }
        if (s == HunterState.Chase) StartWave();
        // A door broken inside a chase is still the chase; one broken on a hunt moves nothing.
        else if (s == HunterState.BreakDoor) return;
        else if (wave) EndWave();
    }

    // ---------- Cells: claiming, the machine, the texel ----------

    void ResetCells()
    {
        for (var s = 0; s < Slots; s++)
        {
            ResetSlot(s);
            isActive[s] = false;
            // Every cell back on the live print; its A channel keeps the route the field holds now.
            used[s] = Way != null;
            if (Way != null) cell[s] = Way.CellAtSlot(s);
            if (tex != null) px[s] = new Color32(0, 0, 0, Way != null ? Way.TexelA(s) : (byte)0);
        }
        activeCount = 0;
        System.Array.Clear(excluded, 0, Slots);
        wave = false;
        roomValid = false;
        room.Clear();
        oldRoom.Clear();
        pressureRevision = -1;
        pressureAt = float.NegativeInfinity;
        texDirty = tex != null;
    }

    // A new floor. A station belongs to its floor: one set under the new floor's EXIT stays (M4 answered FloorChanged
    // before the driver did), any other goes (clearStation: always, for tools). The key starts untaken either way,
    // and the tie memory goes with the old floor (R8: a floor's arrows are a fresh build's).
    void ResetForFloor(bool clearStation)
    {
        ResetCells();
        frozen = false;
        stage = hunter != null && hunter.Released ? hunter.WarnStage : 0;
        if (Way != null)
        {
            if (clearStation || !Way.Station.HasValue || stationExit != ExitNow)
            {
                Way.ClearStation();
                stationExit = null;
                // M5: a map's station is taken up again from the map (SyncMapStation), never kept from the floor before.
                mapStation = false;
            }
            else Way.SetStationKeyTaken(false);
            Way.ResetHysteresis();
            Way.Invalidate();
        }
    }

    void ResetSlot(int s)
    {
        from[s] = to[s] = 0;
        mode[s] = Idle;
        clicks[s] = 0;
        flowClicks[s] = 0;
        flowAfter[s] = pending[s] = false;
        arrive[s] = retractAt[s] = Never;
        retractMode[s] = RetractNone;
        fadeAt[s] = float.NegativeInfinity;
        waveTag[s] = 0;
    }

    // The slot of a cell, taken over when it holds another cell at rest; -1 while another cell's machine runs there
    // (only past buildRadius 3, when the built map is wider than the 64-cell window).
    int Claim(GridCoord c)
    {
        var s = FrontRoomsWayfinding.Slot(c);
        if (used[s] && cell[s] == c) return s;
        if (used[s] && (mode[s] != Idle || arrive[s] < Never || retractAt[s] < Never)) return -1;
        ResetSlot(s);
        used[s] = true;
        cell[s] = c;
        return s;
    }

    void Activate(int s)
    {
        if (isActive[s]) return;
        isActive[s] = true;
        active[activeCount++] = s;
    }

    void Click(int s, int state, float t)
    {
        from[s] = to[s];
        to[s] = (byte)state;
        fadeAt[s] = t;
    }

    static int FlowState(int q)
    {
        var k = q % FrontRoomsCueTiming.FlowLoop;
        return k == 0 ? FrontRoomsCueTiming.TurnStates : FrontRoomsCueTiming.TurnStates + k;
    }

    float UpTime(int s, int i) => kind[s] == StageTurn
        ? baseAt[s] + FrontRoomsCueTiming.StageTurnAt[i]
        : baseAt[s] + i * FrontRoomsCueTiming.WaveTurnStep;

    void StartUp(int s, int dir, byte turn, float t, bool flow)
    {
        mode[s] = Up;
        cdir[s] = (byte)dir;
        kind[s] = turn;
        baseAt[s] = t;
        clicks[s] = 0;
        flowAfter[s] = flow;
        Activate(s);
    }

    // Turn back from where the cell is: a stepping cell first finishes its loop (as a retract does).
    void StartBack(int s, float t)
    {
        pending[s] = false;
        switch (mode[s])
        {
            case Idle:
            case Back:
                return;
            case Flow:
                retractAt[s] = Mathf.Min(retractAt[s], t);
                retractMode[s] = RetractBack;
                return;
            default:
                flowAfter[s] = false;
                // Not clicked yet: nothing to turn back.
                if (to[s] == 0) { mode[s] = Idle; return; }
                mode[s] = Back;
                next[s] = t;
                return;
        }
    }

    // Turn the cell to a dir: from rest at once; turned another way, back first; turning back, after it.
    void Order(int s, int dir, byte turn, float t, bool flow)
    {
        if (mode[s] != Idle && mode[s] != Back)
        {
            if (cdir[s] == dir)
            {
                if (flow) flowAfter[s] = true;
                return;
            }
            StartBack(s, t);
        }
        if (mode[s] == Idle)
        {
            StartUp(s, dir, turn, t, flow);
            return;
        }
        pending[s] = true;
        pdir[s] = (byte)dir;
        pkind[s] = turn;
        pflow[s] = flow;
        Activate(s);
    }

    // A stage turn of one room-set cell: only on a route, and never near the Relay (tested here, as it starts, once:
    // on a retry, a recompute of the set, a cell held back stays at 0 while it stays in the set; review m7).
    void TryTurn(GridCoord c, bool retry)
    {
        var e = FrontRoomsWayfinding.Slot(c);
        if (retry && excluded[e] && excludedCell[e] == c) return;
        if (!Way.Route(c, out var dir)) return;
        if (NearRelay(c))
        {
            excluded[e] = true;
            excludedCell[e] = c;
            return;
        }
        var s = Claim(c);
        if (s < 0 || retractAt[s] < Never) return;
        Order(s, dir, StageTurn, clock, false);
    }

    // Stage 0: every cell that is not retracting turns back (G7: at stage 0 with no chase, every cell is 0 or retracting).
    void BackAll()
    {
        for (var i = 0; i < activeCount; i++)
        {
            var s = active[i];
            if (retractAt[s] < Never) continue;
            StartBack(s, clock);
        }
    }

    void Advance()
    {
        for (var i = 0; i < activeCount;)
        {
            var s = active[i];
            AdvanceCell(s);
            WriteTexel(s);
            if (mode[s] == Idle && arrive[s] == Never && retractAt[s] == Never
                && (from[s] == to[s] || clock - fadeAt[s] >= FrontRoomsCueTiming.CrossFade))
            {
                isActive[s] = false;
                active[i] = active[--activeCount];
            }
            else i++;
        }
    }

    void AdvanceCell(int s)
    {
        for (var guard = 0; guard < 64; guard++)
        {
            if (frozen) return;
            if (arrive[s] <= clock)
            {
                var ta = arrive[s];
                arrive[s] = Never;
                WaveArrive(s, ta);
                continue;
            }
            float t;
            switch (mode[s])
            {
                case Up:
                    t = UpTime(s, clicks[s]);
                    if (t > clock) return;
                    clicks[s]++;
                    Click(s, clicks[s], t);
                    if (clicks[s] < FrontRoomsCueTiming.TurnStates) continue;
                    // Stepping starts only inside the wave (a turn finishing during the retract just holds).
                    if (flowAfter[s] && wave && StepOk(s))
                    {
                        mode[s] = Flow;
                        flowClicks[s] = 0;
                        next[s] = t + FrontRoomsCueTiming.WaveTurnStep;
                    }
                    else
                    {
                        mode[s] = Held;
                        next[s] = t;
                    }
                    continue;
                case Held:
                    if (retractAt[s] > clock) return;
                    Resolve(s, Mathf.Max(retractAt[s], next[s]));
                    continue;
                case Flow:
                    t = next[s];
                    if (t > clock) return;
                    if (flowClicks[s] % FrontRoomsCueTiming.FlowLoop == 0 && t >= retractAt[s])
                    {
                        Resolve(s, t);
                        continue;
                    }
                    if (TurnOnly || (wave && !StepOk(s)))
                    {
                        // Reduce Motion switched on mid-step, or, inside the wave, the way on now leads toward the Relay
                        // (9.6): settle on the fitted state, one click; a retract then turns it back at its time. Through
                        // the retract a row finishes its loop unasked (interface 3; review m3, left open).
                        if (to[s] != FrontRoomsCueTiming.TurnStates) Click(s, FrontRoomsCueTiming.TurnStates, t);
                        flowClicks[s] = 0;
                        mode[s] = Held;
                        next[s] = t;
                        continue;
                    }
                    flowClicks[s]++;
                    Click(s, FlowState(flowClicks[s]), t);
                    next[s] = t + FrontRoomsCueTiming.FlowClick;
                    continue;
                case Back:
                    t = next[s];
                    if (t > clock) return;
                    if (to[s] == 0)
                    {
                        mode[s] = Idle;
                        if (pending[s]) StartPending(s, t);
                        continue;
                    }
                    Click(s, to[s] - 1, t);
                    next[s] = t + FrontRoomsCueTiming.TurnBackStep;
                    if (to[s] > 0) continue;
                    // A wave turn waiting on this turn-back starts one back-click later, so the last cross-fade to the
                    // print finishes under the old dir first (review m2; a stage turn's first click is 0.20 s on already).
                    if (pending[s] && pkind[s] == WaveTurn) continue;
                    mode[s] = Idle;
                    if (pending[s]) StartPending(s, t);
                    continue;
                default:
                    return;
            }
        }
    }

    // After a turn back: the turn it was waiting for, if it still holds. A stage turn is a new turn start: it needs
    // the stage, no wave, the cell's route now, and no Relay near; a wave turn needs the wave.
    void StartPending(int s, float t)
    {
        pending[s] = false;
        if (pkind[s] == StageTurn)
        {
            if (stage == 0 || wave || !Way.Route(cell[s], out var dir)) return;
            if (NearRelay(cell[s]))
            {
                excluded[s] = true;
                excludedCell[s] = cell[s];
                return;
            }
            StartUp(s, dir, StageTurn, t, false);
        }
        else if (wave) StartUp(s, pdir[s], WaveTurn, t, pflow[s]);
    }

    // The wave reaches a cell: it turns on the pressure route where that reaches, else the calm one, then steps.
    void WaveArrive(int s, float ta)
    {
        if (!WaveDir(cell[s], out var dir)) return;
        // Rows step only where 9.6 allows it (StepOk is asked again at every click).
        var flow = !TurnOnly && Way.StepAllowed(cell[s], dir);
        switch (mode[s])
        {
            case Idle:
                StartUp(s, dir, WaveTurn, ta, flow);
                return;
            case Held:
                if (cdir[s] != dir) break;
                if (flow)
                {
                    mode[s] = Flow;
                    flowClicks[s] = 0;
                    next[s] = ta;
                }
                flowAfter[s] = flow;
                return;
            case Up:
                if (cdir[s] != dir) break;
                flowAfter[s] = flow;
                return;
            case Flow:
                return;
        }
        Order(s, dir, WaveTurn, ta, flow);
    }

    // The pressure dir only while a door qualifies for the player, else the calm one (9.6; review M1).
    bool WaveDir(GridCoord c, out int dir) => UsePressureRoute ? Way.ChaseRoute(c, out dir) : Way.Route(c, out dir);

    // A cell may step now: not turn only, and its turned dir is one 9.6 lets step (in the wave and through its retract).
    bool StepOk(int s) => !TurnOnly && Way.StepAllowed(cell[s], cdir[s]);

    // A cell's retract moment (the end of its loop at or after its time): the room set lands on the stage, on the
    // calm route; everything else turns back.
    void Resolve(int s, float t)
    {
        var how = retractMode[s];
        retractMode[s] = RetractNone;
        retractAt[s] = Never;
        flowAfter[s] = false;
        flowClicks[s] = 0;
        if (how == RetractStage && stage >= 1 && !wave && InRoom(cell[s]) && Way.Route(cell[s], out var dir))
        {
            if (dir == cdir[s])
            {
                mode[s] = Held;
                next[s] = t;
                return;
            }
            mode[s] = Back;
            next[s] = t;
            pending[s] = true;
            pkind[s] = StageTurn;
            pdir[s] = (byte)dir;
            pflow[s] = false;
            return;
        }
        mode[s] = Back;
        next[s] = t;
        pending[s] = false;
    }

    void WriteTexel(int s)
    {
        var k = from[s] == to[s] ? 1f : Mathf.Clamp01((clock - fadeAt[s]) / FrontRoomsCueTiming.CrossFade);
        // The turned dir until the last fade back to the print is done: else that fade would run under another dir's
        // faces, or pop where the route is message 0 (review m1).
        var a = mode[s] == Idle && to[s] == 0 && (from[s] == 0 || k >= 1f) ? Way.TexelA(s) : (byte)(cdir[s] + 4);
        var v = to[s] == 0 && k >= 1f ? new Color32(0, 0, 0, a) : new Color32(from[s], to[s], (byte)Mathf.RoundToInt(k * 255f), a);
        var old = px[s];
        if (old.r == v.r && old.g == v.g && old.b == v.b && old.a == v.a) return;
        px[s] = v;
        texDirty = true;
    }

    // ---------- Field rebuilds: dropped cells, new dirs, the A channel ----------

    void FieldRebuilt()
    {
        for (var s = 0; s < Slots; s++)
        {
            var c = Way.CellAtSlot(s);
            if (isActive[s] || mode[s] != Idle)
            {
                // A chunk dropped (or the window moved past the cell): its texel goes back to the print at once.
                if (cell[s] != c || !world.IsBuilt(c))
                {
                    ResetSlot(s);
                    cell[s] = c;
                    used[s] = true;
                }
            }
            else
            {
                used[s] = true;
                cell[s] = c;
            }
            if (!isActive[s]) WriteTexel(s);
        }
        roomValid = false;
        if (wave || stage == 0 || frozen) return;
        // At stage 1-2 the room set follows the route: a turned cell whose dir changed turns back and turns again
        // (R5: within one rebuild); one that lost its route turns back.
        for (var i = 0; i < room.Count; i++)
        {
            var c = room[i];
            var s = FrontRoomsWayfinding.Slot(c);
            if (!used[s] || cell[s] != c || retractAt[s] < Never || (mode[s] != Up && mode[s] != Held)) continue;
            if (!Way.Route(c, out var dir)) StartBack(s, clock);
            else if (dir != cdir[s]) Order(s, dir, StageTurn, clock, false);
        }
    }

    // ---------- The room set ----------

    bool InRoom(GridCoord c)
    {
        var s = FrontRoomsWayfinding.Slot(c);
        return roomMark[s] == roomStamp && roomMarkCell[s] == c;
    }

    void UpdateRoom()
    {
        var player = world.Player;
        if (player == null) return;
        var p = world.CellOf(player.position);
        if (roomValid && p == roomFor && roomBuild == Way.Revision) return;
        roomValid = true;
        roomFor = p;
        roomBuild = Way.Revision;
        var swap = oldRoom;
        oldRoom = room;
        room = swap;
        FrontRoomsWayfinding.RoomSet(world, p, room);
        roomStamp++;
        for (var i = 0; i < room.Count; i++)
        {
            var s = FrontRoomsWayfinding.Slot(room[i]);
            roomMark[s] = roomStamp;
            roomMarkCell[s] = room[i];
        }
        // A cell that leaves the set is tested afresh if it joins again (a new turn start).
        for (var i = 0; i < oldRoom.Count; i++)
        {
            var c = oldRoom[i];
            var e = FrontRoomsWayfinding.Slot(c);
            if (!InRoom(c) && excludedCell[e] == c) excluded[e] = false;
        }
        if (frozen || wave || stage == 0) return;
        // At stage 1-2 the set follows the player: cells left behind turn back, cells gained turn.
        for (var i = 0; i < oldRoom.Count; i++)
        {
            var c = oldRoom[i];
            if (InRoom(c)) continue;
            var s = FrontRoomsWayfinding.Slot(c);
            if (used[s] && cell[s] == c && retractAt[s] == Never) StartBack(s, clock);
        }
        for (var i = 0; i < room.Count; i++) TryTurn(room[i], true);
    }

    // ---------- The Relay (stage turns' exclusion; the pressure route) ----------

    bool NearRelay(GridCoord c)
    {
        if (relayAt != clock)
        {
            relayAt = clock;
            relayOn = hunter != null && hunter.Released;
            relayRoomW = 0;
            if (relayOn)
            {
                relayCell = world.CellOf(hunter.Position);
                var chunk = MapGrid.ChunkOf(relayCell);
                if (world.Cache.TryGetGenerated(chunk, out var data))
                {
                    var o = MapGrid.ChunkOrigin(chunk);
                    for (var r = data.rooms.Length - 1; r >= 0; r--)
                    {
                        var rect = data.rooms[r];
                        if (!rect.Contains(relayCell.x - o.x, relayCell.y - o.y)) continue;
                        relayRoomX = o.x + rect.x;
                        relayRoomY = o.y + rect.y;
                        relayRoomW = rect.w;
                        relayRoomH = rect.h;
                        break;
                    }
                }
            }
        }
        if (!relayOn) return false;
        if (Mathf.Max(Mathf.Abs(c.x - relayCell.x), Mathf.Abs(c.y - relayCell.y)) <= RelayClearCells) return true;
        return relayRoomW > 0 && c.x >= relayRoomX && c.x < relayRoomX + relayRoomW && c.y >= relayRoomY && c.y < relayRoomY + relayRoomH;
    }

    /// <summary>Tools: the Relay cell's exclusion as a stage turn reads it now.</summary>
    public bool NearRelayForTools(GridCoord c)
    {
        relayAt = -1f;
        return NearRelay(c);
    }

    void UpdatePressure(bool force)
    {
        if (!UsePressureRoute || hunter == null || !hunter.Released) return;
        // A calm rebuild drops the pressure route: build it again at once, or every row would stop stepping.
        if (!Way.PressureValid) force = true;
        if (!force && clock - pressureAt < PressureRefreshSeconds) return;
        var rc = world.CellOf(hunter.Position);
        var pc = world.Player != null ? world.CellOf(world.Player.position) : rc;
        if (!force && Way.PressureValid && rc == pressureRelay && pc == pressurePlayer && Way.Revision == pressureRevision) return;
        Way.BuildPressure(rc, pc);
        pressureAt = clock;
        pressureRelay = rc;
        pressurePlayer = pc;
        pressureRevision = Way.Revision;
    }

    // ---------- The wave ----------

    void StartWave()
    {
        if (wave) return;
        wave = true;
        waveId++;
        UpdateRoom();
        UpdatePressure(true);
        var t0 = clock;
        // Steps from the room set over open ways (PassageBetween Open), inside the window.
        bfsId++;
        int head = 0, tail = 0;
        var player = world.Player != null ? world.CellOf(world.Player.position) : default;
        if (room.Count > 0)
        {
            for (var i = 0; i < room.Count; i++)
                if (Way.InWindow(room[i])) Enqueue(room[i], 0, ref tail);
        }
        else if (Way.InWindow(player) && world.IsBuilt(player)) Enqueue(player, 0, ref tail);
        while (head < tail)
        {
            var s = bfsQueue[head++];
            var c = Way.CellAtSlot(s);
            for (var k = 0; k < 4; k++)
            {
                var n = new GridCoord(c.x + FrontRoomsWayfinding.DirX[k], c.y + FrontRoomsWayfinding.DirY[k]);
                if (!Way.InWindow(n)) continue;
                var ns = FrontRoomsWayfinding.Slot(n);
                if (bfsStamp[ns] == bfsId || !world.IsBuilt(n) || world.PassageBetween(c, n) != FrontRoomsMapWorld.Passage.Open) continue;
                Enqueue(n, bfsDist[s] + 1, ref tail);
            }
        }
        for (var s = 0; s < Slots; s++)
        {
            var c = Way.CellAtSlot(s);
            if (!WaveDir(c, out _)) continue;
            int d;
            if (bfsStamp[s] == bfsId) d = bfsDist[s];
            else
            {
                float dx = c.x - player.x, dy = c.y - player.y;
                d = Mathf.RoundToInt(Mathf.Sqrt(dx * dx + dy * dy) * UnreachedWaveFactor);
            }
            var slot = Claim(c);
            if (slot < 0) continue;
            waveTag[slot] = waveId;
            waveDist[slot] = d;
            arrive[slot] = t0 + d * MapGrid.CellSize / FrontRoomsCueTiming.WaveSpeed;
            // A new chase takes over a retract still running.
            retractAt[slot] = Never;
            retractMode[slot] = RetractNone;
            Activate(slot);
        }
    }

    void Enqueue(GridCoord c, int d, ref int tail)
    {
        var s = FrontRoomsWayfinding.Slot(c);
        if (bfsStamp[s] == bfsId) return;
        bfsStamp[s] = bfsId;
        bfsDist[s] = d;
        bfsQueue[tail++] = s;
    }

    // Outside-in over RetractWindow: the furthest cell the wave reached first, the room set (step 0) last.
    void EndWave()
    {
        wave = false;
        var t = clock;
        var dmax = 1;
        for (var i = 0; i < activeCount; i++)
        {
            var s = active[i];
            if (waveTag[s] == waveId && arrive[s] == Never && mode[s] != Idle && waveDist[s] > dmax) dmax = waveDist[s];
        }
        for (var i = 0; i < activeCount; i++)
        {
            var s = active[i];
            if (waveTag[s] != waveId) continue;
            if (arrive[s] < Never) { arrive[s] = Never; continue; }
            if (mode[s] == Idle) continue;
            if (mode[s] == Back)
            {
                // Turning back to take the wave's turn: the wave is over, so it just turns back.
                if (pending[s] && pkind[s] == WaveTurn) pending[s] = false;
                continue;
            }
            retractAt[s] = t + (1f - waveDist[s] / (float)dmax) * FrontRoomsCueTiming.RetractWindow;
            retractMode[s] = RetractStage;
        }
        // A cell the wave never took (its route went during the chase) answers to the stage as it stands now.
        for (var i = 0; i < activeCount; i++)
        {
            var s = active[i];
            if (waveTag[s] == waveId || retractAt[s] < Never) continue;
            if (stage == 0 || !InRoom(cell[s])) StartBack(s, t);
        }
        // A stage that began during the wave: the room set's cells at rest make the stage turn now (a new turn start).
        if (stage >= 1)
        {
            System.Array.Clear(excluded, 0, Slots);
            UpdateRoom();
            for (var i = 0; i < room.Count; i++) TryTurn(room[i], false);
        }
    }

    // ---------- Out to the shader ----------

    void Flush()
    {
        if (tex == null) return;
        var root = world.transform.position;
        if (!originSet || root != origin)
        {
            origin = root;
            originSet = true;
            Shader.SetGlobalVector(OriginId, new Vector4(root.x, root.z, MapGrid.CellSize, tooWide ? 0f : 1f));
        }
        if (texDirty)
        {
            tex.Apply(false);
            texDirty = false;
        }
        // The cue slices are authored on K00: the ambient print holds there while any cell is off 0.
        var any = activeCount > 0 && !tooWide;
        if (any != holdSet && Application.isPlaying)
        {
            var print = FrontRoomsPrintDriver.Instance;
            if (print != null) print.SetHold("cue", any);
            holdSet = any;
        }
    }

    void LogStats(string when)
    {
        if (Way == null) return;
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        Debug.Log("[FrontRoomsWayCue] " + when + ": field " + Way.Builds + " rebuilds, max " + Way.MaxBuildMs.ToString("0.00", inv) + " ms, mean "
            + (Way.Builds > 0 ? Way.TotalBuildMs / Way.Builds : 0f).ToString("0.00", inv) + " ms, last " + Way.LastBuildMs.ToString("0.00", inv) + " ms ("
            + Way.FrontierBuilds + " with the frontier rule, " + Way.CapBuilds + " past the cap); target "
            + (Way.HasTarget ? (Way.TargetIsStation ? "station " : "N ") + Way.Target : "none") + ", " + Way.RoutedCells + " cells routed; pressure "
            + Way.PressureBuilds + " builds, max " + Way.MaxPressureMs.ToString("0.00", inv) + " ms" + (Way.PressureValid ? (Way.PressureReachesPlayer ? ", a door qualifies" : ", no door qualifies") : "")
            + "; " + Way.FrontierCells + " frontier-routed of " + Way.BuiltCells + " built; driver max step " + MaxStepMs.ToString("0.00", inv)
            + " ms, " + activeCount + " cells active, stage " + stage + (wave ? ", wave" : "") + (frozen ? ", frozen" : ""));
    }
}
