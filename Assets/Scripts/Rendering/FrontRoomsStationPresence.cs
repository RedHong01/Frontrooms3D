using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Scripting;

/// <summary>
/// Whether the floor the player is on has a station (tutorial T3 / V4: Office desk CRTs show still screens
/// only on such a floor; Documentation/research/tutorial/PREBUILD_v4desk.md).
///
/// A READER, owned by 游戏视觉. The map owns the fact: <c>public bool FloorHasStation</c> on
/// <see cref="FrontRoomsMapWorld"/>, which 关卡设计 adds with M4. It is set before <c>Begin</c> when the floor's
/// station is planned, cleared by NextFloor and the other floor resets, and false on a map with no station. The
/// map never references this class, never calls a setter here and raises no event for it
/// (contract: Documentation/research/tutorial/README_V4_FloorHasStation.md).
///
/// The value is read by reflection (the PropertyInfo is cached; a public bool field of that name also works) from
/// the map the game hands out with <see cref="FrontRooms3DGame.MapRunStarted"/> and
/// <see cref="FrontRooms3DGame.FloorChanged"/> (map, relay, floor; C22, in main since 2026-10-22 15:39). The map
/// member is looked up by name, so this compiles and runs on a main without it: the value stays false, so every
/// desk keeps playing the ad as before. Both events are subscribed directly (not by reflection): no other code adds
/// a FloorChanged handler, and the WebGL player strips at High, which would drop an add accessor only reflection
/// reaches (verify 2026-10-22 20:3x). It goes back to false when the run is torn down (MapRunEnded).
///
/// R3 (the playing station TV, research/inspect/NOTE_1029_R3_station_tv.md) also reads whether the floor's key is taken:
/// <c>public bool StationKeyIsTaken</c> on the map (关卡设计, MapWorld 527ddb39: true after the take, false with no
/// station), by reflection like FloorHasStation, each time the map is handed out. The map's
/// <c>event Action&lt;Vector3&gt; StationKeyTaken</c> (subscribed directly, for the WebGL reason above) makes it true at
/// once; the event is never read alone, because it misses a take made before this subscribed.
/// </summary>
public static class FrontRoomsStationPresence
{
    /// <summary>The map member this reads (agreed with 关卡设计, 2026-10-22 15:2x).</summary>
    public const string MemberName = "FloorHasStation";
    /// <summary>The game event (C22) that announces a new floor: Action&lt;FrontRoomsMapWorld, FrontRoomsMapHunter, int&gt; (subscribed directly).</summary>
    public const string FloorChangedEventName = "FloorChanged";
    /// <summary>R3: the map member that says the floor's key is taken (关卡设计, 2026-10-29 15:3x).</summary>
    public const string KeyTakenMemberName = "StationKeyIsTaken";

    static bool value;
    static int floor = 1;
    static int version;
    static bool resolved;
    static PropertyInfo property;
    static FieldInfo field;
    static string hookState = "not hooked";
#if UNITY_EDITOR
    static bool? testOverride;
#endif
    // R3: the key-taken bool, its reflected member, and the map whose StationKeyTaken event is subscribed.
    static bool keyTaken;
    static bool keyResolved;
    static PropertyInfo keyProperty;
    static FieldInfo keyField;
    static FrontRoomsMapWorld keyMap;
#if UNITY_EDITOR
    static bool? keyTestOverride;
#endif

    /// <summary>True when the current floor has a station. False with no run, and while the map has no such member.</summary>
    public static bool FloorHasStation
    {
        get
        {
#if UNITY_EDITOR
            if (testOverride.HasValue) return testOverride.Value;
#endif
            return value;
        }
    }

    /// <summary>R3: the floor's station key is taken (the TV stops being the playing station TV). False with no run or no station.</summary>
    public static bool StationKeyIsTaken
    {
        get
        {
#if UNITY_EDITOR
            if (keyTestOverride.HasValue) return keyTestOverride.Value;
#endif
            return keyTaken;
        }
    }

    /// <summary>The run's floor: 1 at the run start (the title's floor), then what FloorChanged reports (2, 3, ...).</summary>
    public static int Floor => floor;

    /// <summary>Bumps whenever <see cref="FloorHasStation"/> or <see cref="Floor"/> changes.</summary>
    public static int Version => version;

    /// <summary>How the map member was bound: "property", "field" or "absent" (read as false).</summary>
    public static string Binding
    {
        get
        {
            Resolve();
            return property != null ? "property" : field != null ? "field" : "absent";
        }
    }

    /// <summary>"subscribed" once the hook ran (FloorChanged and MapRunStarted are subscribed directly), else "not hooked".</summary>
    public static string FloorChangedHook => hookState;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Hook()
    {
        value = false;
        floor = 1;
        version++;
#if UNITY_EDITOR
        testOverride = null;
        keyTestOverride = null;
#endif
        keyTaken = false;
        WatchKey(null);
        FrontRooms3DGame.MapRunStarted -= OnMapRunStarted;
        FrontRooms3DGame.MapRunStarted += OnMapRunStarted;
        FrontRooms3DGame.MapRunEnded -= OnMapRunEnded;
        FrontRooms3DGame.MapRunEnded += OnMapRunEnded;
        // C22's floor change, subscribed directly: a reflection-only AddEventHandler finds no add accessor in a WebGL
        // player (High stripping keeps only the accessors some code calls, and no other code adds a handler).
        FrontRooms3DGame.FloorChanged -= OnFloorChanged;
        FrontRooms3DGame.FloorChanged += OnFloorChanged;
        hookState = "subscribed";
    }

    static void OnMapRunStarted(FrontRoomsMapWorld map, FrontRoomsMapHunter relay)
    {
        SetFloor(1);
        Refresh(map);
    }

    [Preserve]
    static void OnFloorChanged(FrontRoomsMapWorld map, FrontRoomsMapHunter relay, int newFloor)
    {
        SetFloor(newFloor);
        Refresh(map);
    }

    static void OnMapRunEnded()
    {
        SetFloor(1);
        Set(false);
        SetKeyTaken(false);
        WatchKey(null);
    }

    static void SetFloor(int f)
    {
        if (f == floor) return;
        floor = f;
        version++;
    }

    static void Set(bool v)
    {
        if (v == value) return;
        value = v;
        version++;
    }

    /// <summary>Re-reads the member on <paramref name="map"/> (the events call this; so may a test).</summary>
    public static void Refresh(FrontRoomsMapWorld map)
    {
        Set(Read(map));
        // R3: the bool first (a take made before the subscription), then the event for a take from now on.
        SetKeyTaken(ReadKeyTaken(map));
        WatchKey(map);
        if (FloorHasStation) FrontRoomsDeskStill.Prepare();
    }

    static void SetKeyTaken(bool v)
    {
        if (v == keyTaken) return;
        keyTaken = v;
        version++;
    }

    static void WatchKey(FrontRoomsMapWorld map)
    {
        if (keyMap == map) return;
        if (keyMap != null) keyMap.StationKeyTaken -= OnStationKeyTaken;
        keyMap = map;
        if (keyMap != null) keyMap.StationKeyTaken += OnStationKeyTaken;
    }

    // The take (M3 raises it once a floor with the bool already true): react at once.
    [Preserve]
    static void OnStationKeyTaken(Vector3 at) => SetKeyTaken(true);

    /// <summary>R3: the map's StationKeyIsTaken; false when the map is null or has no such member.</summary>
    public static bool ReadKeyTaken(FrontRoomsMapWorld map)
    {
        if (map == null) return false;
        ResolveKey();
        try
        {
            if (keyProperty != null) return (bool)keyProperty.GetValue(map, null);
            if (keyField != null) return (bool)keyField.GetValue(map);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[FrontRoomsStationPresence] could not read FrontRoomsMapWorld." + KeyTakenMemberName + ": " + e.Message);
        }
        return false;
    }

    static void ResolveKey()
    {
        if (keyResolved) return;
        keyResolved = true;
        var t = typeof(FrontRoomsMapWorld);
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
        keyProperty = t.GetProperty(KeyTakenMemberName, flags);
        if (keyProperty != null && (keyProperty.PropertyType != typeof(bool) || !keyProperty.CanRead || keyProperty.GetIndexParameters().Length != 0)) keyProperty = null;
        if (keyProperty == null)
        {
            keyField = t.GetField(KeyTakenMemberName, flags);
            if (keyField != null && keyField.FieldType != typeof(bool)) keyField = null;
        }
    }

    /// <summary>R3: how StationKeyIsTaken was bound: "property", "field" or "absent" (read as false).</summary>
    public static string KeyBinding
    {
        get
        {
            ResolveKey();
            return keyProperty != null ? "property" : keyField != null ? "field" : "absent";
        }
    }

    /// <summary>The member's value on <paramref name="map"/>; false when the map is null or has no such member.</summary>
    public static bool Read(FrontRoomsMapWorld map)
    {
        if (map == null) return false;
        Resolve();
        try
        {
            if (property != null) return (bool)property.GetValue(map, null);
            if (field != null) return (bool)field.GetValue(map);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[FrontRoomsStationPresence] could not read FrontRoomsMapWorld." + MemberName + ": " + e.Message);
        }
        return false;
    }

    static void Resolve()
    {
        if (resolved) return;
        resolved = true;
        var t = typeof(FrontRoomsMapWorld);
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
        property = t.GetProperty(MemberName, flags);
        if (property != null && (property.PropertyType != typeof(bool) || !property.CanRead || property.GetIndexParameters().Length != 0)) property = null;
        if (property == null)
        {
            field = t.GetField(MemberName, flags);
            if (field != null && field.FieldType != typeof(bool)) field = null;
        }
    }

#if UNITY_EDITOR
    /// <summary>EDITOR ONLY, for tests and captures: force the answer (null = read the map again).</summary>
    public static void SetForTest(bool? forced)
    {
        testOverride = forced;
        version++;
        if (FloorHasStation) FrontRoomsDeskStill.Prepare();
    }

    /// <summary>EDITOR ONLY, for tests: force StationKeyIsTaken (null = the map's answer again).</summary>
    public static void SetKeyTakenForTest(bool? forced)
    {
        keyTestOverride = forced;
        version++;
    }
#endif
}
