using UnityEditor;
using UnityEngine;

/// <summary>
/// FrontRooms/Map/Play with keys on: a switch for this editor session. While it is ticked, every run's map
/// needs keys (FrontRoomsMapWorld.DoorsNeedKeysForTools, set by the game at run start), so the key turn
/// and its unlock shot can be played by hand. The level profile (Levels/FrontRoomsLevel0.asset keeps
/// doorsNeedKeys off) is never changed, and the autopilot ignores the switch. Ticking it enters Play.
///
/// FrontRooms/Map/Show placeholder key: the same kind of switch for the key pickup's presentation. While it
/// is ticked, a picked-up key is presented with the cube itself (FrontRoomsMapWorld.ShowPlaceholderKey, set
/// by the game at run start and at once in a running game), for timing captures before the key models land.
/// The autopilot ignores it.
/// </summary>
public static class FrontRoomsKeysOnPlay
{
    const string MenuPath = "FrontRooms/Map/Play with keys on";
    const string PlaceholderMenuPath = "FrontRooms/Map/Show placeholder key";

    [MenuItem(PlaceholderMenuPath)]
    static void TogglePlaceholder()
    {
        var on = !SessionState.GetBool(FrontRooms3DGame.PlaceholderKeySessionKey, false);
        SessionState.SetBool(FrontRooms3DGame.PlaceholderKeySessionKey, on);
        FrontRoomsMapWorld.ShowPlaceholderKey = on;
        Menu.SetChecked(PlaceholderMenuPath, on);
        Debug.Log("[FrontRoomsMap] Show placeholder key: " + (on ? "ON (pickups present the cube key)" : "off (pickups are instant without a key model)"));
    }

    [MenuItem(PlaceholderMenuPath, true)]
    static bool ValidatePlaceholder()
    {
        Menu.SetChecked(PlaceholderMenuPath, SessionState.GetBool(FrontRooms3DGame.PlaceholderKeySessionKey, false));
        return true;
    }

    [MenuItem(MenuPath)]
    static void Toggle()
    {
        var on = !SessionState.GetBool(FrontRooms3DGame.KeysOnSessionKey, false);
        SessionState.SetBool(FrontRooms3DGame.KeysOnSessionKey, on);
        Menu.SetChecked(MenuPath, on);
        Debug.Log("[FrontRoomsMap] Play with keys on: " + (on ? "ON (runs started from now on need keys; the profile is unchanged)" : "off"));
        if (on && !EditorApplication.isPlayingOrWillChangePlaymode) EditorApplication.EnterPlaymode();
    }

    [MenuItem(MenuPath, true)]
    static bool Validate()
    {
        Menu.SetChecked(MenuPath, SessionState.GetBool(FrontRooms3DGame.KeysOnSessionKey, false));
        return true;
    }
}
