using System;
using FrontRooms.Map;
using UnityEngine;

/// <summary>
/// The Level Designer's live preview (Assets/Scenes/FrontRoomsLevelDesigner.unity):
/// it builds the real game map around one room module, with the same
/// builder, lamps, kits and look as the game, and rebuilds whenever the
/// module changes. In Play only the room's chunk is rebuilt and the walker
/// stays where it stands (RebuildLive); a new size, height, theme or seed
/// rebuilds everything and starts it at the entrance again.
///
/// The module is stamped into chunk (0, 0), centred. With the Same
/// surroundings every zone of the maze takes the module's height and theme;
/// with Mixed ones the profile's own zones stand round it (the seed searched
/// from Seed, MixedSeed), so the map's doors, windows and Office | Level 0
/// transitions show where the game would build them. Either way the map's
/// own rules (columns, dressing, lamps) apply exactly as they will in the
/// game. Its props carry a FrontRoomsModulePropTag, and ModuleToWorld /
/// WorldToModule convert between module metres and the scene, so the Level
/// Designer's Scene view tools can write edits back to the module.
///
/// The game's rooms that are not plain stamps are built as the game builds
/// them, through the map's own floor calls: a station module (stationAsInGame)
/// as floor 1's start group or as a floor station on LEVEL B1, the Stairwell
/// view (floor 1's EXIT and its stairwell X), and the RunRoom view (a Run!
/// corridor, placement forced as the tools force it). With gameDressing the
/// visual line's dressers are attached through their tool entry points, as the
/// game attaches them at FrontRooms3DGame.MapRunStarted; their per-frame parts
/// (closers, the TV picture, the paper's date, rig ticks) run in Play only.
/// </summary>
[ExecuteAlways]
public sealed class FrontRoomsModulePreview : MonoBehaviour
{
    /// <summary>What the preview shows: a room module, or one of the game's rooms that is no module.</summary>
    public enum RoomView { Module, Stairwell, RunRoom }

    [Tooltip("The room module to preview. Edit it in the Inspector; the preview follows.")]
    public FrontRoomsRoomModule module;
    [Tooltip("Generation and look. Empty: the project's level profile. With the Same surroundings the preview overrides zone heights and theme to match the module.")]
    public FrontRoomsLevelProfile profile;
    [Tooltip("Seed of the maze around the room, the lamps' temperaments and the kits' rolls. Mixed surroundings, stations, the stairwell and Run! rooms search on from it.")]
    public int seed = 1;
    [Range(0, 3), Tooltip("Quarter turns clockwise, to check the room as the generator may place it.")]
    public int rotation;
    [Tooltip("Camera at eye height in the room's entrance, for the Game view.")]
    public Camera eye;
    [Tooltip("Module: the room module. Stairwell: floor 1's EXIT and the stairwell behind it. Run Room: a Run! alarm corridor (placement forced).")]
    public RoomView view = RoomView.Module;
    [Tooltip("Off: every zone takes the module's height and theme. On: the profile's own zones, the seed searched until zones of another height (and theme) stand round the room, so the map's doors, windows and Office | Level 0 transitions show.")]
    public bool mixedSurroundings;
    [Tooltip("A station module as the game builds it: floor 1's start group, or a floor station on LEVEL B1 (station walls, authored arches, set piece, floor key, locked EXIT). Off: stamped as a plain room, which the Scene view tools edit.")]
    public bool stationAsInGame = true;
    [Tooltip("Attach the visual line's dressers as the game does at MapRunStarted (stairwell look A, A13's chain, the station dress and paper, Run! rigs). Their per-frame parts run in Play only.")]
    public bool gameDressing = true;
    [Min(1), Tooltip("Stairwell: the floor whose EXIT it is (1 = LEVEL 0, its stair down to B1). The plates and signs follow it.")]
    public int stairFloor = 1;
    [Tooltip("Stairwell: the EXIT locked by the floor key (A13's chain and padlock), as the player first finds it.")]
    public bool exitLocked = true;
    [Tooltip("Stairwell: the eye inside the stairwell, facing the EXIT door's stair side, instead of in front of the door.")]
    public bool eyeInStairwell;
    [Tooltip("Run Room: the corridor opens off an Office zone (C39) instead of a Level 0 one.")]
    public bool runInOffice;

    const string MapName = "PREVIEW / map", GameRootName = "PREVIEW / game root";
    /// <summary>Seeds tried from Seed: mixed surroundings, then the station, stairwell and Run! plans (each a cache or a map).</summary>
    const int MixedTries = 4000, StationTries = 24, StairTries = 24, RunTries = 8;
    /// <summary>How far round the room (cells) a zone of another height or theme counts as its surroundings.</summary>
    const int MixedReach = 4;
    /// <summary>Floor 1's start door row at the title's door column, as the validator's EXIT chain plans it.</summary>
    const int StationDoorRow = 6;
    /// <summary>The one-cell stairwell a floor below the first starts in (floor stations), and the stairwell view's walk start.</summary>
    static readonly GridCoord ArrivalCell = new GridCoord(4, 4);

    FrontRoomsMapWorld world;
    FrontRoomsLevelProfile previewProfile;
    GameObject gameRoot;
    FrontRoomsStairwell stairwell;
    bool dirty = true;
    Bounds focus;
    bool hasFocus;

    /// <summary>Raised before a rebuild throws the old map away (the Scene view tools note which props were selected) and after the new one is built.</summary>
    public static event Action<FrontRoomsModulePreview> Rebuilding, Rebuilt;

    /// <summary>The map world currently built, or null.</summary>
    public FrontRoomsMapWorld World => world;

    /// <summary>A rebuild is queued for the next editor tick (MarkDirty) and has not run yet.</summary>
    public bool RebuildQueued => dirty;

    /// <summary>The module data the current map was stamped with (turned), or null. The props it placed carry this very object in their tag.</summary>
    public RoomModuleData Stamped { get; private set; }

    /// <summary>The module is stamped in chunk (0, 0) with tagged props (the Module view, not a station built as in the game): the Scene view tools and the live rebuild work on this build only.</summary>
    public bool StampedAtOrigin { get; private set; }

    /// <summary>What the last build made, in one line for the Level Designer window (the seed it used, where the room stands, what was refused).</summary>
    public string Status { get; private set; }

    /// <summary>The seed the current map was generated with (Seed itself, or the one a search found).</summary>
    public int BuiltSeed { get; private set; }

    /// <summary>The Stairwell view's stairwell, or null.</summary>
    public FrontRoomsStairwell Stairwell => stairwell;

    /// <summary>Where the previewed room stands (world), for framing; false with nothing built.</summary>
    public bool TryGetFocus(out Bounds bounds)
    {
        bounds = focus;
        return hasFocus && world != null;
    }

    int Turns => ((rotation % 4) + 4) % 4;

    FrontRoomsLevelProfile Source => profile != null ? profile : FrontRoomsLevelProfile.Default;

    /// <summary>Where the module sits: chunk (0, 0), centred.</summary>
    public static void Placement(RoomModuleData module, out int x, out int y) => Placement(module.width, module.depth, out x, out y);

    static void Placement(int width, int depth, out int x, out int y)
    {
        x = (MapGrid.ChunkCells - width) / 2;
        y = (MapGrid.ChunkCells - depth) / 2;
    }

    // ---------- Module space <-> scene (the module must be set) ----------

    /// <summary>
    /// A point in module metres (x, z from the south-west corner of the
    /// module as authored, as props store it) to world space, through the
    /// stamp's turn and placement. A clockwise quarter turn maps (x, z) to
    /// (z, W - x), W the width before the turn, as RoomModuleData.Rotated does.
    /// </summary>
    public Vector3 ModuleToWorld(Vector2 moduleXZ, float height = 0f)
    {
        var m = module.data;
        float x = moduleXZ.x, z = moduleXZ.y, w = m.WidthMetres, d = m.DepthMetres;
        for (var t = 0; t < Turns; t++)
        {
            (x, z) = (z, w - x);
            (w, d) = (d, w);
        }
        return transform.TransformPoint(StampOrigin() + new Vector3(x, height, z));
    }

    /// <summary>The inverse of ModuleToWorld on the floor: a world point to module metres (height dropped).</summary>
    public Vector2 WorldToModule(Vector3 world)
    {
        var local = transform.InverseTransformPoint(world) - StampOrigin();
        var m = module.data;
        float x = local.x, z = local.z;
        // The turned footprint: odd turns swap width and depth.
        float w = Turns % 2 == 0 ? m.WidthMetres : m.DepthMetres, d = Turns % 2 == 0 ? m.DepthMetres : m.WidthMetres;
        for (var t = 0; t < Turns; t++)
        {
            // Undo one turn: the frame before it was d wide, so (x, z) came from (d - z, x).
            (x, z) = (d - z, x);
            (w, d) = (d, w);
        }
        return new Vector2(x, z);
    }

    /// <summary>A module prop's yaw as a world rotation: the stamp adds 90 degrees per quarter turn, then the preview's own rotation.</summary>
    public Quaternion ModuleToWorldRotation(float yaw) => transform.rotation * Quaternion.Euler(0f, yaw + 90f * Turns, 0f);

    /// <summary>The module yaw of a world rotation: its heading on the floor (a tilt is ignored), back through the preview and the turns.</summary>
    public float WorldToModuleYaw(Quaternion rotation)
    {
        var forward = Quaternion.Inverse(transform.rotation) * rotation * Vector3.forward;
        return Mathf.Repeat(Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg - 90f * Turns, 360f);
    }

    /// <summary>The floor the room stands on (the preview's own y = 0 plane).</summary>
    public Plane FloorPlane => new Plane(transform.up, transform.position);

    /// <summary>The stamped (turned) module's south-west corner in the preview's local space: chunk (0, 0) starts at its origin.</summary>
    Vector3 StampOrigin()
    {
        var m = module.data;
        var odd = Turns % 2 == 1;
        Placement(odd ? m.depth : m.width, odd ? m.width : m.depth, out var x0, out var y0);
        return new Vector3(x0 * MapGrid.CellSize, 0f, y0 * MapGrid.CellSize);
    }

    void OnEnable()
    {
        FrontRoomsRoomModule.Changed += OnModuleChanged;
        FrontRoomsLevelProfile.Changed += OnProfileChanged;
        MarkDirty();
    }

    void OnDisable()
    {
        FrontRoomsRoomModule.Changed -= OnModuleChanged;
        FrontRoomsLevelProfile.Changed -= OnProfileChanged;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.update -= WaitForProfile;
#endif
        Clear();
    }

    // Edit mode: a profile edit is the next build's look, rebuilt once the edits stop (a held slider sends one per frame).
    // Play: the running preview map takes the profile's live numbers. Its world runs on a clone
    // (previewProfile), so the world's own check never sees the asset; the preview keeps its own
    // build radius (1) and its zone and module overrides (ApplyLive does not read the generation).
    void OnProfileChanged(FrontRoomsLevelProfile changed)
    {
        if (changed == null || changed != profile) return;
        if (!Application.isPlaying)
        {
#if UNITY_EDITOR
            profileSettleAt = UnityEditor.EditorApplication.timeSinceStartup + ProfileSettle;
            UnityEditor.EditorApplication.update -= WaitForProfile;
            UnityEditor.EditorApplication.update += WaitForProfile;
#endif
            return;
        }
        if (world == null || previewProfile == null) return;
        previewProfile.chunksPerFrame = changed.chunksPerFrame;
        previewProfile.shiftAfterSeconds = changed.shiftAfterSeconds;
        previewProfile.lightRadius = changed.lightRadius;
        previewProfile.shadowRadius = changed.shadowRadius;
        previewProfile.doorsNeedKeys = changed.doorsNeedKeys;
        previewProfile.dressOffices = changed.dressOffices;
        previewProfile.pileChance = changed.pileChance;
        previewProfile.transitions = changed.transitions;              // TRANSITION (T8): chunks built from now on
        previewProfile.renovationShare = changed.renovationShare;
        previewProfile.tiers = changed.tiers;
        world.ApplyLive(previewProfile);
    }

#if UNITY_EDITOR
    /// <summary>How long (s) a profile must stay unedited in edit mode before the preview rebuilds.</summary>
    const double ProfileSettle = .4;
    double profileSettleAt;

    void WaitForProfile()
    {
        if (this == null)
        {
            UnityEditor.EditorApplication.update -= WaitForProfile;
            return;
        }
        if (UnityEditor.EditorApplication.timeSinceStartup < profileSettleAt) return;
        UnityEditor.EditorApplication.update -= WaitForProfile;
        MarkDirty();
    }
#endif

    void OnValidate() => MarkDirty();

    void OnModuleChanged(FrontRoomsRoomModule changed)
    {
        // In Play only the module's chunk is rebuilt and the walker stays where it stands (RebuildLive).
        if (changed == module && view == RoomView.Module) MarkDirty();
    }

    /// <summary>Rebuild on the next editor tick (edits come in bursts while dragging).</summary>
    public void MarkDirty()
    {
        dirty = true;
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.EditorApplication.delayCall -= RebuildIfDirty;
            UnityEditor.EditorApplication.delayCall += RebuildIfDirty;
        }
#endif
    }

    void Update()
    {
        if (Application.isPlaying) RebuildIfDirty();
    }

    void RebuildIfDirty()
    {
        if (this == null || !dirty || !isActiveAndEnabled) return;
        dirty = false;
        if (Application.isPlaying && RebuildLive()) return;
        Rebuild();
    }

    // The seed, profile and surroundings the current map was built with: any of them is a new maze or look, so a full rebuild.
    int builtSeed;
    FrontRoomsLevelProfile builtProfile;
    bool builtMixed;

    /// <summary>
    /// Play: stamp the edited module into the running map's chunk (0, 0) and
    /// rebuild only that chunk, keeping the walker where it stands (back at the
    /// entrance if the edit put something where it stood). False when the turned
    /// room's size, height or theme, or the seed, profile or surroundings, changed, or
    /// the map is not a plain stamp (a station as in the game, the other views): that needs a full rebuild.
    /// </summary>
    bool RebuildLive()
    {
        if (view != RoomView.Module || !StampedAtOrigin || mixedSurroundings != builtMixed) return false;
        if (world == null || Stamped == null || module == null || module.data == null || seed != builtSeed || profile != builtProfile) return false;
        var data = module.data.Rotated(rotation);
        data.Normalize();
        if (data.width != Stamped.width || data.depth != Stamped.depth || data.height != Stamped.height || data.theme != Stamped.theme) return false;
        Rebuilding?.Invoke(this);
        Placement(data, out var x0, out var y0);
        world.ReplaceModule(new GridCoord(0, 0), data, x0, y0);
        Stamped = data;
        KeepWalker(data, x0, y0);
        Rebuilt?.Invoke(this);
        return true;
    }

    /// <summary>After a live rebuild: if the walker's body now overlaps something (a moved prop, a new inner wall), it goes back to the entrance.</summary>
    void KeepWalker(RoomModuleData data, int x0, int y0)
    {
        var walker = world.Player;
        if (walker == null) return;
        Physics.SyncTransforms();
        var r = ModuleUnits.PlayerRadius;
        var feet = walker.position;
        var blocked = false;
        foreach (var c in Physics.OverlapCapsule(feet + Vector3.up * (r + .05f), feet + Vector3.up * (ModuleUnits.PlayerHeight - r), r, ~0, QueryTriggerInteraction.Ignore))
            if (!c.transform.IsChildOf(walker)) { blocked = true; break; }
        if (!blocked && world.Cache != null)
        {
            // The shell is one non-convex mesh: an overlap only meets its faces, so a body wholly inside a new column is caught here.
            var chunk = world.Cache.Get(new GridCoord(0, 0));
            var local = world.transform.InverseTransformPoint(feet);
            const int n = MapGrid.ChunkCells;
            for (var j = 0; j <= n && !blocked; j++)
            for (var i = 0; i <= n && !blocked; i++)
            {
                var k = i + j * (n + 1);
                if (!chunk.pillar[k]) continue;
                var reach = ((chunk.pillarStyle[k] & MapChunk.ColumnLarge) != 0 ? ModuleUnits.ColumnLarge : ModuleUnits.ColumnSmall) * .5f + r;
                blocked = Mathf.Abs(local.x - i * MapGrid.CellSize) < reach && Mathf.Abs(local.z - j * MapGrid.CellSize) < reach;
            }
        }
        if (!blocked) return;
        Entrance(data, x0, y0, out var spawn, out var look);
        var position = world.transform.TransformPoint(spawn);
        var heading = world.transform.TransformDirection(look - spawn);
        var yaw = heading.sqrMagnitude > 1e-6f ? Mathf.Atan2(heading.x, heading.z) * Mathf.Rad2Deg : walker.eulerAngles.y;
        // The walker keeps its own yaw and look: tell it, or it turns back on the next frame.
        var mover = walker.GetComponent<FrontRoomsMapWalker>();
        if (mover != null) { mover.Teleport(position, yaw); return; }
        var body = walker.GetComponent<CharacterController>();
        if (body != null) body.enabled = false;
        walker.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        if (body != null) body.enabled = true;
    }

    /// <summary>Throw the old map away and build the view's again.</summary>
    public void Rebuild()
    {
        // Any build consumes a pending one (Open and the captures build at once).
        dirty = false;
        Rebuilding?.Invoke(this);
        Clear();
        Status = null;
        if (view == RoomView.Stairwell) BuildStairwell();
        else if (view == RoomView.RunRoom) BuildRunRoom();
        else if (module == null || module.data == null) return;
        else if (stationAsInGame && module.data.IsStation) BuildStation();
        else BuildModule();
        FrontRoomsPostStack.Ensure(transform);
        Rebuilt?.Invoke(this);
    }

    // ---------- Module: the room stamped in chunk (0, 0) ----------

    void BuildModule()
    {
        var data = module.data.Rotated(rotation);
        data.Normalize();
        Placement(data, out var x0, out var y0);
        var g = Source.Generation(seed);
        if (mixedSurroundings)
        {
            g.seed = MixedSeed(data, x0, y0, g, out var tried, out var around);
            Status = around != null ? "Mixed surroundings: seed " + g.seed + " (" + tried + " tried from " + seed + "): " + around + "."
                : "Mixed surroundings: no seed in " + MixedTries + " from " + seed + " stands another height round this " + data.height + " room; seed " + g.seed + " fits it.";
        }
        else
        {
            // A copy of the profile whose maze takes the module's height and theme everywhere.
            g.lowShare = data.height == ZoneHeight.Low ? 1f : 0f;
            g.standardShare = data.height == ZoneHeight.Standard ? 1f : 0f;
            g.tallShare = data.height == ZoneHeight.Tall ? 1f : 0f;
            g.officeShare = data.theme == ZoneTheme.Office ? 1f : 0f;
            Status = "Same surroundings: every zone " + data.height + (data.theme == ZoneTheme.Office ? " Office" : " Level 0") + ", seed " + seed + " (no doors, windows or transitions: they stand only where heights or themes meet).";
        }
        // Only this module: the generator's own modules would put other designer rooms around it.
        g.moduleChance = 0f;
        NewPreviewProfile(g);

        var go = NewMap();
        // Props carry their module index, so Scene view edits can be written back.
        world.TagModuleProps = true;
        world.PlaceModule(data, new GridCoord(0, 0), x0, y0);
        Stamped = data;
        StampedAtOrigin = true;
        builtSeed = seed;
        builtProfile = profile;
        builtMixed = mixedSurroundings;
        BuiltSeed = g.seed;
        Entrance(data, x0, y0, out var spawn, out var look);
        var heading = look - spawn;
        world.OverrideSpawn(spawn, Mathf.Atan2(heading.x, heading.z) * Mathf.Rad2Deg);
        StartMap(go);
        MarkDontSave();
        if (eye != null)
        {
            // In Play the walker's camera is the Game view.
            eye.enabled = !Application.isPlaying;
            eye.transform.position = transform.TransformPoint(spawn + Vector3.up * ModuleUnits.PlayerEye);
            eye.transform.rotation = Quaternion.LookRotation(transform.TransformDirection(look - spawn), Vector3.up) * Quaternion.Euler(4f, 0f, 0f);
            eye.farClipPlane = world.SightDistance;
            FrontRoomsPostStack.ConfigureCamera(eye);
        }
        var cs = MapGrid.CellSize;
        SetFocus(transform.TransformPoint(new Vector3((x0 + data.width * .5f) * cs, 1f, (y0 + data.depth * .5f) * cs)), Mathf.Max(data.width, data.depth) * cs);
    }

    /// <summary>
    /// Mixed surroundings: from the profile's numbers at Seed on, the first seed whose zones give every cell of the
    /// room its height and theme (so the stamp holds), with a zone of another height within MixedReach cells of it
    /// and, for a Standard room, one of the other theme too. If none in MixedTries does, the first that fits.
    /// Zones are a function of the seed alone (FrontRoomsMapGenerator.ZoneOf), so nothing is generated here.
    /// </summary>
    static int MixedSeed(RoomModuleData m, int x0, int y0, MapSettings from, out int tried, out string around)
    {
        var fallback = from.seed;
        var fits = false;
        around = null;
        tried = 0;
        for (var k = 0; k < MixedTries; k++)
        {
            tried++;
            var g = from.Clone();
            g.seed = unchecked(from.seed + k);
            var gen = new FrontRoomsMapGenerator(g);
            if (!RoomInOneKind(gen, m, x0, y0)) continue;
            if (!fits) { fits = true; fallback = g.seed; }
            around = Surroundings(gen, m, x0, y0);
            if (around != null) return g.seed;
        }
        around = null;
        return fallback;
    }

    static bool RoomInOneKind(FrontRoomsMapGenerator gen, RoomModuleData m, int x0, int y0)
    {
        for (var j = 0; j < m.depth; j++)
        for (var i = 0; i < m.width; i++)
        {
            var zone = gen.ZoneOf(new GridCoord(x0 + i, y0 + j));
            if (zone.height != m.height || zone.theme != m.theme) return false;
        }
        return true;
    }

    // The other heights (and themes) of the zones round the room, as a phrase, or null when they do not make it mixed.
    static string Surroundings(FrontRoomsMapGenerator gen, RoomModuleData m, int x0, int y0)
    {
        bool low = false, standard = false, tall = false, office = false, level0 = false;
        for (var j = y0 - MixedReach; j < y0 + m.depth + MixedReach; j++)
        for (var i = x0 - MixedReach; i < x0 + m.width + MixedReach; i++)
        {
            var zone = gen.ZoneOf(new GridCoord(i, j));
            if (zone.height != m.height)
            {
                low |= zone.height == ZoneHeight.Low;
                standard |= zone.height == ZoneHeight.Standard;
                tall |= zone.height == ZoneHeight.Tall;
            }
            else if (zone.theme != m.theme)
            {
                office |= zone.theme == ZoneTheme.Office;
                level0 |= zone.theme == ZoneTheme.Level0;
            }
        }
        if (!low && !standard && !tall) return null;
        if (m.height == ZoneHeight.Standard && !office && !level0) return null;
        // Between heights the taller side decides: a window wherever a Tall zone meets another height, a door otherwise.
        var lower = m.height == ZoneHeight.Tall ? " (windows)" : " (doors)";
        var kinds = new System.Collections.Generic.List<string>();
        if (low) kinds.Add("Low" + lower);
        if (standard) kinds.Add("Standard" + lower);
        if (tall) kinds.Add("Tall (windows)");
        if (office) kinds.Add("Office (transitions)");
        if (level0) kinds.Add("Level 0 (transitions)");
        return string.Join(", ", kinds.ToArray()) + " zones within " + MixedReach + " cells";
    }

    // ---------- A station module as the game builds it ----------

    /// <summary>
    /// Floor 1's start group (FrontRoomsStartGroup.Plan at the title's door, SetStartArea, SetStation, SetExitArea) or,
    /// for a floor station, LEVEL B1 from a one-cell stairwell start (ChooseExit, SetExitArea, FrontRoomsFloorStation.Plan
    /// with the module in its theme's slot, SetStation), the EXIT locked by the floor key as the game locks it. The seed is
    /// searched from Seed on the floor's own cache until the plan holds. A module the station contract refuses, or one
    /// that fits nowhere, is shown as a plain room.
    /// </summary>
    void BuildStation()
    {
        var data = module.data.Clone();
        data.Normalize();
        var source = Source;
        var modules = source.ModuleData();
        var floorStation = data.IsFloorStation;
        var floor = floorStation ? 2 : 1;
        string why;
        StartGroupTemplate start = null;
        FloorStationTemplate floorTemplate = null;
        if (floorStation) floorTemplate = FloorStationTemplate.From(data, FrontRoomsMapWorld.KitFootprint, out why);
        else start = StartGroupTemplate.From(data, FrontRoomsMapWorld.KitFootprint, out why);
        if (start == null && floorTemplate == null)
        {
            BuildModule();
            Status = "The station contract refuses this module (" + why + "): shown as a plain room.";
            return;
        }
        var titleDoor = new GridCoord(Mathf.FloorToInt(FrontRooms3DGame.TitleCenterX / MapGrid.CellSize), StationDoorRow);
        var floorSeed = int.MinValue;
        string error = null;
        for (var k = 0; k < StationTries && floorSeed == int.MinValue; k++)
        {
            var s = unchecked(seed + k);
            var cache = FrontRoomsExitPlanner.CacheFor(source.generation, modules, s, floor);
            var trial = floorStation ? FloorStationPlan(cache, floorTemplate, s, out _) : FrontRoomsStartGroup.Plan(cache, start, titleDoor);
            if (trial != null && trial.Ready) floorSeed = s;
            else if (trial != null) error = trial.error;
        }
        if (floorSeed == int.MinValue)
        {
            BuildModule();
            Status = "No " + (floorStation ? floorTemplate.Name + " fit on LEVEL B1" : "start group") + " in " + StationTries + " seeds from " + seed
                + (error != null ? " (" + error + ")" : "") + ": shown as a plain room.";
            return;
        }

        NewPreviewProfile(source.Generation(floorSeed));
        var go = NewMap();
        StartMap(go);
        BuiltSeed = floorSeed;
        StartGroupPlan plan;
        if (floorStation)
        {
            world.KeyPickupNeedsE = true;
            world.NextFloor(floorSeed, floor, new RectInt(ArrivalCell.x, ArrivalCell.y, 1, 1), ArrivalDoor);
            world.ZoneKeysOff = true;
            plan = FloorStationPlan(world.Cache, floorTemplate, floorSeed, out var exit);
            if (exit.found) world.SetExitArea(exit.exitCell);
        }
        else
        {
            world.NextFloor(floorSeed, floor, new RectInt(0, 0, 0, 0), default(GridCoord));
            plan = FrontRoomsStartGroup.Plan(world.Cache, start, titleDoor);
            if (plan.Ready)
            {
                var strip = plan.layout.strip;
                world.SetStartArea(new RectInt(strip.x, strip.y, strip.w, strip.h), plan.layout.door);
            }
        }
        var held = plan != null && plan.Ready && world.SetStation(plan, floorStation ? plan.floor.module : data);
        if (held && !floorStation) world.SetExitArea(plan.exit.exitCell);
        if (held && world.FloorKeyRule) world.SetExitLocked(true);

        var cs = MapGrid.CellSize;
        Vector3 from, look;
        if (held)
        {
            var l = plan.layout;
            var s = l.station;
            look = (world.CellCenter(new GridCoord(s.x, s.y)) + world.CellCenter(new GridCoord(s.x + s.w - 1, s.y + s.h - 1))) * .5f;
            // Floor 1: just inside D, the way the player comes in; a floor station: in E1, looking into the room.
            from = floorStation ? Vector3.Lerp(world.CellCenter(l.e1Out), world.CellCenter(l.e1In), .75f) : world.CellCenter(l.door);
            if (!floorStation) look = world.CellCenter(l.e1In);
            SetFocus((world.CellCenter(new GridCoord(s.x, s.y)) + world.CellCenter(new GridCoord(s.x + s.w - 1, s.y + s.h - 1))) * .5f + transform.up, Mathf.Max(s.w, s.h) * cs);
            Status = (floorStation ? floorTemplate.Name + " on LEVEL B1" : "Floor 1's start group") + " as the game builds it: seed " + floorSeed + (floorSeed != seed ? " (searched from " + seed + ")" : "")
                + ", the room at " + s.x + ".." + (s.x + s.w - 1) + " x " + s.y + ".." + (s.y + s.h - 1) + ", E1 " + l.e1In + ", EXIT X " + plan.exit.exitCell
                + (world.ExitLocked ? " locked by the floor key" : "") + (plan.warning != null ? " · " + plan.warning : "") + ".";
        }
        else
        {
            from = world.SpawnWorldPosition;
            look = from + transform.forward;
            Status = "The map refused the station (seed " + floorSeed + (plan != null && plan.error != null ? ": " + plan.error : "") + ").";
        }
        PlacePlayer(from, look);
        world.Begin(world.Player, true);
        if (held && gameDressing)
        {
            Dress("station dress", () => FrontRoomsStationDress.Attach(world, floor));
            Dress("station paper", () => FrontRoomsStationPaper.Attach(world));
        }
        MarkDontSave();
        PlaceEye(from, look);
    }

    static GridCoord ArrivalDoor => new GridCoord(ArrivalCell.x, ArrivalCell.y + 1);

    /// <summary>LEVEL B1 from a one-cell stairwell start at ArrivalCell: the floor's EXIT on the cache, then the floor station with the template in its theme's slot alone. Null with no EXIT.</summary>
    static StartGroupPlan FloorStationPlan(FrontRoomsMapCache cache, FloorStationTemplate t, int floorSeed, out ExitPlan exit)
    {
        var area = new CellRect(ArrivalCell.x, ArrivalCell.y, 1, 1);
        FrontRoomsExitPlanner.BandFor(2, out var lo, out var hi);
        exit = FrontRoomsExitPlanner.ChooseExit(cache, area, ArrivalDoor, floorSeed, lo, hi);
        if (!exit.found) return null;
        var office = t.theme == ZoneTheme.Office;
        return FrontRoomsFloorStation.Plan(cache, office ? null : t, office ? t : null, area, ArrivalDoor, exit);
    }

    // ---------- The stairwell: a floor's EXIT and X, as the game makes them ----------

    void BuildStairwell()
    {
        var source = Source;
        var modules = source.ModuleData();
        var floor = Mathf.Max(1, stairFloor);
        var none = new CellRect(0, 0, 0, 0);
        FrontRoomsExitPlanner.BandFor(floor, out var lo, out var hi);
        var floorSeed = int.MinValue;
        for (var k = 0; k < StairTries && floorSeed == int.MinValue; k++)
        {
            var s = unchecked(seed + k);
            if (FrontRoomsExitPlanner.ChooseExit(FrontRoomsExitPlanner.CacheFor(source.generation, modules, s, floor), none, ArrivalCell, s, lo, hi).found) floorSeed = s;
        }
        if (floorSeed == int.MinValue)
        {
            Status = "No EXIT found in " + StairTries + " seeds from " + seed + ".";
            return;
        }

        NewPreviewProfile(source.Generation(floorSeed));
        var go = NewMap();
        StartMap(go);
        BuiltSeed = floorSeed;
        // The floor as the game makes it: X set and the stairwell made before the build (C22, plan v2 D8).
        world.NextFloor(floorSeed, floor, new RectInt(0, 0, 0, 0), default(GridCoord));
        var exit = FrontRoomsExitPlanner.ChooseExit(world.Cache, none, ArrivalCell, floorSeed, lo, hi);
        world.SetExitArea(exit.exitCell);
        gameRoot = new GameObject(GameRootName);
        gameRoot.transform.SetParent(transform, false);
        stairwell = FrontRoomsStairwell.Create(world, exit.exitCell, gameRoot.transform);
        if (gameDressing)
        {
            // Before Begin, as the game's MapRunStarted: the EXIT's build is dressed as it comes.
            Dress("stairwell look A", () => FrontRoomsStairwellDresser.Attach(world).SetFloorForTools(floor));
            Dress("A13 chain", () => FrontRoomsExitChain.Attach(world));
        }
        if (exitLocked) world.SetExitLocked(true);

        var x = world.CellCenter(exit.exitCell);
        var n = world.CellCenter(exit.frontCell);
        var north = x - n;
        Vector3 from, look;
        // Inside: near X's north wall, facing the EXIT door's stair side. Outside: in N, a step back from the door.
        if (eyeInStairwell) { from = x + north * .3f; look = n; }
        else { from = n - north * .25f; look = x; }
        PlacePlayer(from, look);
        world.Begin(world.Player, true);
        stairwell.SetVisible(world.IsChunkBuiltAt(exit.exitCell));
        MarkDontSave();
        PlaceEye(from, look);
        SetFocus((x + n) * .5f + transform.up, MapGrid.CellSize * 2.5f);
        Status = "Floor " + floor + "'s EXIT and stairwell: seed " + floorSeed + (floorSeed != seed ? " (searched from " + seed + ")" : "") + ", X " + exit.exitCell + ", N " + exit.frontCell
            + ", " + exit.distance + " walking cells from " + ArrivalCell + (exit.widened ? " (band widened)" : "") + (world.ExitLocked ? ", locked by the floor key" : "") + "."
            + (gameDressing && !Application.isPlaying ? " Look A's own lights follow the stairwell in Play only: inside it reads dark here." : "");
    }

    // ---------- A Run! corridor (placement forced, as -autopilotRunRooms 2) ----------

    void BuildRunRoom()
    {
#if UNITY_EDITOR
        var source = Source;
        for (var k = 0; k < RunTries; k++)
        {
            if (k > 0) Clear();
            var g = source.Generation(unchecked(seed + k));
            g.officeShare = runInOffice ? 1f : 0f;
            NewPreviewProfile(g);
            previewProfile.runRooms = true;
            previewProfile.runInOffice = runInOffice;
            var go = NewMap();
            var keep = FrontRoomsMapWorld.RunRoomsForTools;
            FrontRoomsMapWorld.RunRoomsForTools = 2;
            try { StartMap(go); }
            finally { FrontRoomsMapWorld.RunRoomsForTools = keep; }
            var room = NearestRunRoom();
            if (room == null) continue;
            BuiltSeed = g.seed;
            var from = world.RunRoomPoint(room, new Vector3(0f, 0f, 1.2f));
            var look = world.RunRoomPoint(room, new Vector3(0f, 0f, room.LengthMetres));
            PlacePlayer(from, look);
            // Both of its chunks, round the corridor's middle.
            world.StreamForTools(world.RunRoomPoint(room, new Vector3(0f, 0f, room.LengthMetres * .5f)), 0f);
            if (gameDressing) Dress("Run! rig", () => FrontRoomsRunRigBinder.Bind(world, world.Player));
            MarkDontSave();
            PlaceEye(from, look);
            SetFocus(world.RunRoomPoint(room, new Vector3(0f, 1f, room.LengthMetres * .5f)), room.LengthMetres);
            Status = "Run! corridor " + room.Id + " (27 m) off a" + (runInOffice ? "n Office" : " Level 0") + " zone: seed " + g.seed + (g.seed != seed ? " (searched from " + seed + ")" : "")
                + (world.IsRunRoomBuilt(room) ? "" : ", not all of it built") + ". Placement forced; Run! rooms are off in the game's profile.";
            return;
        }
        Status = "No Run! corridor planned in " + RunTries + " seeds from " + seed + ".";
#else
        Status = "Run! rooms are previewed in the editor only.";
#endif
    }

    // The planned Run! room nearest the map's player, built ones first.
    FrontRoomsRunRoom NearestRunRoom()
    {
        var run = world != null ? world.RunRooms : null;
        if (run == null || world.Player == null) return null;
        FrontRoomsRunRoom best = null;
        var bestScore = float.MaxValue;
        var at = world.Player.position;
        for (var i = 0; i < run.Rooms.Count; i++)
        {
            var room = run.Rooms[i];
            var score = (world.RunRoomPoint(room, new Vector3(0f, 0f, room.LengthMetres * .5f)) - at).sqrMagnitude + (world.IsRunRoomBuilt(room) ? 0f : 1e6f);
            if (score < bestScore) { bestScore = score; best = room; }
        }
        return best;
    }

    // ---------- Shared steps ----------

    /// <summary>The build's own copy of the profile (never the asset), with the build radius kept at one chunk.</summary>
    void NewPreviewProfile(MapSettings generation)
    {
        previewProfile = Instantiate(Source);
        previewProfile.hideFlags = HideFlags.DontSave;
        previewProfile.generation = generation;
        previewProfile.buildRadius = 1;
    }

    /// <summary>The map object, inactive until StartMap, so its fields are set before Awake (as CreateEmbedded does).</summary>
    GameObject NewMap()
    {
        var go = new GameObject(MapName);
        go.SetActive(false);
        go.transform.SetParent(transform, false);
        world = go.AddComponent<FrontRoomsMapWorld>();
        world.Profile = previewProfile;
        return go;
    }

    /// <summary>Play: Awake builds the map standalone, with the walker at the spawn. Edit mode: BuildForCapture, its eye the map's player.</summary>
    void StartMap(GameObject go)
    {
        go.SetActive(true);
        if (!Application.isPlaying) world.BuildForCapture();
    }

    /// <summary>
    /// The map's player (edit mode: the capture eye; Play: the walker) at a floor point, facing a point. Floors set up
    /// after NextFloor stream round it from Begin on.
    /// </summary>
    void PlacePlayer(Vector3 floorPoint, Vector3 lookAt)
    {
        var p = world.Player;
        if (p == null) return;
        var heading = lookAt - floorPoint;
        var yaw = heading.x * heading.x + heading.z * heading.z > 1e-6f ? Mathf.Atan2(heading.x, heading.z) * Mathf.Rad2Deg : p.eulerAngles.y;
        var walker = p.GetComponent<FrontRoomsMapWalker>();
        if (walker != null) walker.Teleport(floorPoint + Vector3.up * .05f, yaw);
        else p.SetPositionAndRotation(floorPoint, Quaternion.Euler(0f, yaw, 0f));
    }

    /// <summary>The Game view's eye at eye height over a floor point, facing a point level.</summary>
    void PlaceEye(Vector3 floorPoint, Vector3 lookAt)
    {
        if (eye == null || world == null) return;
        eye.enabled = !Application.isPlaying;
        var heading = lookAt - floorPoint;
        heading -= transform.up * Vector3.Dot(heading, transform.up);
        if (heading.sqrMagnitude < 1e-6f) heading = transform.forward;
        eye.transform.position = floorPoint + transform.up * ModuleUnits.PlayerEye;
        eye.transform.rotation = Quaternion.LookRotation(heading, transform.up) * Quaternion.Euler(4f, 0f, 0f);
        eye.farClipPlane = world.SightDistance;
        FrontRoomsPostStack.ConfigureCamera(eye);
    }

    void SetFocus(Vector3 centre, float size)
    {
        focus = new Bounds(centre, Vector3.one * Mathf.Max(1f, size));
        hasFocus = true;
    }

    /// <summary>Edit mode: what this build made is never saved with the scene.</summary>
    void MarkDontSave()
    {
        if (Application.isPlaying) return;
        if (world != null) foreach (var t in world.GetComponentsInChildren<Transform>(true)) t.gameObject.hideFlags = HideFlags.DontSave;
        if (gameRoot != null) foreach (var t in gameRoot.GetComponentsInChildren<Transform>(true)) t.gameObject.hideFlags = HideFlags.DontSave;
    }

    /// <summary>A visual-line dresser through its tool entry point; one that throws leaves the map as built and says so.</summary>
    void Dress(string what, Action attach)
    {
        try { attach(); }
        catch (Exception e)
        {
            Debug.LogWarning("[FrontRoomsModulePreview] " + what + " was not attached: " + e.Message);
            Status = (Status ?? "") + " (" + what + " failed: see the console)";
        }
    }

    /// <summary>
    /// A standing spot just inside the room's first opening (south, west,
    /// north, east, in that order), looking at the room's centre; the room's
    /// centre if it has none. Map space.
    /// </summary>
    public static void Entrance(RoomModuleData m, int x0, int y0, out Vector3 spawn, out Vector3 look)
    {
        var cs = MapGrid.CellSize;
        look = new Vector3((x0 + m.width * .5f) * cs, 0f, (y0 + m.depth * .5f) * cs);
        spawn = look;
        // The whole 0.3 m body stays inside the floor kept clear behind the opening (1.08 m from the cell line).
        const float inside = ModuleUnits.EntryClearDepth + ModuleUnits.WallHalf - ModuleUnits.PlayerRadius;
        for (var i = 0; i < m.width; i++)
            if (m.south[i] != ModuleEdge.Wall) { spawn = new Vector3((x0 + i + .5f) * cs, 0f, y0 * cs + inside); break; }
        if (spawn != look) { spawn.y = .05f; return; }
        for (var j = 0; j < m.depth; j++)
            if (m.west[j] != ModuleEdge.Wall) { spawn = new Vector3(x0 * cs + inside, 0f, (y0 + j + .5f) * cs); break; }
        if (spawn != look) { spawn.y = .05f; return; }
        for (var i = 0; i < m.width; i++)
            if (m.north[i] != ModuleEdge.Wall) { spawn = new Vector3((x0 + i + .5f) * cs, 0f, (y0 + m.depth) * cs - inside); break; }
        if (spawn != look) { spawn.y = .05f; return; }
        for (var j = 0; j < m.depth; j++)
            if (m.east[j] != ModuleEdge.Wall) { spawn = new Vector3((x0 + m.width) * cs - inside, 0f, (y0 + j + .5f) * cs); break; }
        spawn.y = .05f;
        if (spawn == new Vector3(look.x, .05f, look.z)) look += Vector3.forward;
    }

    void Clear()
    {
        if (world != null)
        {
            // Play mode: the standalone map spawned its walker at the scene root, not under the map.
            if (Application.isPlaying && world.Player != null && world.Player.GetComponent<FrontRoomsMapWalker>() != null) Destroy(world.Player.gameObject);
            world.Release();
            if (Application.isPlaying) Destroy(world.gameObject); else DestroyImmediate(world.gameObject);
        }
        world = null;
        Stamped = null;
        StampedAtOrigin = false;
        hasFocus = false;
        stairwell = null;
        if (gameRoot != null)
        {
            if (Application.isPlaying) Destroy(gameRoot); else DestroyImmediate(gameRoot);
        }
        gameRoot = null;
        // Leftovers from a domain reload.
        for (var i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (child.name != MapName && child.name != GameRootName) continue;
            if (Application.isPlaying) Destroy(child.gameObject); else DestroyImmediate(child.gameObject);
        }
        if (previewProfile != null)
        {
            if (Application.isPlaying) Destroy(previewProfile); else DestroyImmediate(previewProfile);
        }
        previewProfile = null;
    }
}
