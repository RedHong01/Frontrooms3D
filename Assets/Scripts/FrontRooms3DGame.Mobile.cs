using UnityEngine;

// Mobile glue kept in a separate partial so the desktop HUD and gameplay
// remain readable while the touch surface evolves against the Figma states.
public sealed partial class FrontRooms3DGame
{
    FrontRoomsTouchControls mobileTouch;
    bool mobileTouchEventsBound;

    void EnsureMobileTouchLayer()
    {
        if (mobileTouch != null) return;
        mobileTouch = GetComponent<FrontRoomsTouchControls>();
        if (mobileTouch == null) mobileTouch = gameObject.AddComponent<FrontRoomsTouchControls>();
        if (GetComponent<FrontRoomsTouchControlsView>() == null)
            gameObject.AddComponent<FrontRoomsTouchControlsView>();
        if (!mobileTouchEventsBound)
        {
            // Settings rows are the one menu action that is not represented by
            // a scalar Input facade edge. The touch layer sends the row index
            // directly; keyboard navigation continues through HandleSettingsKeys.
            mobileTouch.SettingsRowRequested += HandleMobileSettingsRow;
            mobileTouchEventsBound = true;
        }
        UpdateMobileTouchMenu();
    }

    void HandleMobileSettingsRow(int index)
    {
        if (phase != Phase.Paused || !displaySettingsOpen)
            return;
        var rows = SettingsRows();
        if (index < 0 || index >= rows.Length)
            return;
        settingsIndex = index;
        rows[index].step(2);
        UpdateDisplaySettingsText();
    }

    void UpdateMobileTouchMenu()
    {
        if (mobileTouch == null) return;
        var state = phase == Phase.Title
            ? FrontRoomsTouchControls.MenuState.Title
            : phase == Phase.Playing
                ? FrontRoomsTouchControls.MenuState.Playing
                : phase == Phase.Paused
                    ? (displaySettingsOpen ? FrontRoomsTouchControls.MenuState.Settings : FrontRoomsTouchControls.MenuState.Paused)
                    : FrontRoomsTouchControls.MenuState.Caught;
        mobileTouch.SetMenuState(state);
        mobileTouch.SetWinded(winded);
    }

    void UpdateMobileTouchPrompt()
    {
        if (mobileTouch == null) return;
        var visible = phase == Phase.Playing && aimed != null && !string.IsNullOrEmpty(prompt);
        var locked = visible && prompt.IndexOf("LOCK", System.StringComparison.OrdinalIgnoreCase) >= 0;
        var tapMode = FrontRoomsSettings.TapToBreak;
        mobileTouch.SetUsePrompt(new FrontRoomsTouchControls.UsePrompt
        {
            Visible = visible,
            Interactable = visible && !locked,
            Hold = aimedHold,
            Locked = locked,
            TapMode = tapMode,
            Label = prompt ?? string.Empty,
            Progress = holdProgress,
            Kind = locked ? FrontRoomsTouchControls.UseKind.Locked : aimedHold ? FrontRoomsTouchControls.UseKind.Hold : tapMode ? FrontRoomsTouchControls.UseKind.Tap : FrontRoomsTouchControls.UseKind.Open
        });
        mobileTouch.SetWinded(winded);
    }
}
