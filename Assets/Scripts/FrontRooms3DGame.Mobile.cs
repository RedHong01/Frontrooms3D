using UnityEngine;
using UnityEngine.SceneManagement;

// Mobile glue kept in a separate partial so the desktop HUD and gameplay
// remain readable while the touch surface evolves against the Figma states.
public sealed partial class FrontRooms3DGame
{
    FrontRoomsTouchControls mobileTouch;
    FrontRoomsMobileBackBridge mobileBackBridge;
    bool mobileTouchEventsBound;
    bool mobileRestartConfirmOpen;
    float mobileLastGlassBeat;

    void EnsureMobileTouchLayer()
    {
        if (mobileTouch != null) return;
        mobileTouch = GetComponent<FrontRoomsTouchControls>();
        if (mobileTouch == null) mobileTouch = gameObject.AddComponent<FrontRoomsTouchControls>();
        if (GetComponent<FrontRoomsTouchControlsView>() == null)
            gameObject.AddComponent<FrontRoomsTouchControlsView>();
        mobileBackBridge = GetComponent<FrontRoomsMobileBackBridge>();
        if (mobileBackBridge == null)
            mobileBackBridge = gameObject.AddComponent<FrontRoomsMobileBackBridge>();
        if (!mobileTouchEventsBound)
        {
            // Settings rows are the one menu action that is not represented by
            // a scalar Input facade edge. The touch layer sends the row index
            // directly; keyboard navigation continues through HandleSettingsKeys.
            mobileTouch.SettingsRowRequested += HandleMobileSettingsRow;
            mobileTouch.RestartCancelRequested += CancelMobileRestartConfirmation;
            mobileTouch.LookTapped += HandleMobileLookTap;
            FrontRoomsMobileBackBridge.BackPressed += HandleMobileBack;
            mobileTouchEventsBound = true;
        }
        UpdateMobileTouchMenu();
    }

    void HandleMobileBack()
    {
        if (phase == Phase.Title || phase == Phase.Caught)
            return;
        if (mobileRestartConfirmOpen)
        {
            CancelMobileRestartConfirmation();
            return;
        }
        if (displaySettingsOpen)
        {
            ToggleDisplaySettings();
            return;
        }
        if (phase == Phase.Playing)
            SetPhase(Phase.Paused);
        else if (phase == Phase.Paused)
            SetPhase(Phase.Playing);
    }

    void RequestMobileRestartConfirmation()
    {
        if (phase != Phase.Paused || mobileRestartConfirmOpen)
            return;
        mobileRestartConfirmOpen = true;
        mobileTouch?.SetRestartConfirmation(true);
        FrontRoomsMobileInteractionEvents.MenuConfirmed();
        UpdateMobileTouchMenu();
    }

    void CancelMobileRestartConfirmation()
    {
        if (!mobileRestartConfirmOpen)
            return;
        mobileRestartConfirmOpen = false;
        mobileTouch?.SetRestartConfirmation(false);
        UpdateMobileTouchMenu();
    }

    void ConfirmMobileRestart()
    {
        mobileRestartConfirmOpen = false;
        mobileTouch?.SetRestartConfirmation(false);
        FrontRoomsMobileInteractionEvents.MenuConfirmed();
        restart = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void HandleMobileSettingsRow(int index)
    {
        if (phase != Phase.Paused || !displaySettingsOpen)
            return;
        // Mobile settings are deliberately a compact touch-only list. Keep
        // desktop's keyboard rows independent so the existing HUD layout does
        // not acquire tiny, hard-to-hit controls.
        if (index < 0 || index >= 8)
            return;
        switch (index)
        {
            case 0: FrontRoomsSettings.StepTouchControlsScale(2); break;
            case 1: FrontRoomsSettings.StepTouchOpacity(2); break;
            case 2: FrontRoomsSettings.SetTouchLeftHanded(!FrontRoomsSettings.TouchLeftHanded); break;
            case 3: FrontRoomsSettings.StepTouchLookSpeed(2); break;
            case 4: FrontRoomsSettings.SetTouchInvertLook(!FrontRoomsSettings.TouchInvertLook); break;
            case 5: FrontRoomsSettings.SetTouchFloatingStick(!FrontRoomsSettings.TouchFloatingStick); break;
            case 6: FrontRoomsSettings.SetTouchSprintSocket(!FrontRoomsSettings.TouchSprintSocket); break;
            case 7: FrontRoomsSettings.SetTouchHaptics(!FrontRoomsSettings.TouchHaptics); break;
        }
        mobileTouch?.ApplySavedSettings();
        UpdateDisplaySettingsText();
        FrontRoomsMobileInteractionEvents.MenuConfirmed();
    }

    void HandleMobileLookTap(Vector2 screenPosition)
    {
        if (phase != Phase.Playing || map == null || cam == null)
            return;
        var ray = cam.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(ray, out var hit, Reach, ~0, QueryTriggerInteraction.Ignore))
            return;
        var description = map.Describe(hit.collider, out var hold);
        if (hold || string.IsNullOrEmpty(description))
            return;
        FrontRoomsMobileInteractionEvents.UsePressed();
        map.Use(hit.collider);
        FrontRoomsMobileInteractionEvents.UseReleased();
    }

    void UpdateMobileTouchMenu()
    {
        if (mobileTouch == null) return;
        if (phase != Phase.Paused && mobileRestartConfirmOpen)
        {
            mobileRestartConfirmOpen = false;
            mobileTouch.SetRestartConfirmation(false);
        }
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

    void EmitMobileGlassBeat(float progress)
    {
        progress = Mathf.Clamp01(progress);
        if (progress <= 0f || progress - mobileLastGlassBeat < .2f)
            return;
        mobileLastGlassBeat = progress;
        FrontRoomsMobileInteractionEvents.GlassBeat(progress);
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
