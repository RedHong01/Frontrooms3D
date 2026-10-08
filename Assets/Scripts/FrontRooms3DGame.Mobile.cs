using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The handheld (iOS / Android) glue, kept in its own partial so the desktop
// HUD and gameplay stay readable. Everything here runs only while
// FrontRoomsHandheld.Active: desktop, WebGL and normal Editor Play Mode never
// build the touch layer (Documentation/TOUCH_CONTROLS.md).
public sealed partial class FrontRooms3DGame : IFrontRoomsTouchHost
{
    FrontRoomsTouchControls mobileTouch;
    FrontRoomsMobileBackBridge mobileBackBridge;
    bool mobileTouchEventsBound;
    bool mobileRestartConfirmOpen;
    float mobileLastGlassBeat;
    bool mobileWasWinded;
    Image mobilePromptGlyph;
    FrontRoomsMapWorld mobileHapticMap;
    FrontRoomsMapHunter mobileHapticRelay;
    HunterState mobileRelayState;

    const string MobileCalmHintSocket = "Slide up into the socket to sprint. About 5 seconds, and it hears every step.";
    const string MobileCalmHintButton = "Tap SPRINT to run. About 5 seconds, and it hears every step.";

    /// <summary>The first-run hint: the touch gesture on a handheld, Shift on desktop.</summary>
    string CurrentCalmHint => FrontRoomsHandheld.Active
        ? (FrontRoomsSettings.TouchSprintSocket ? MobileCalmHintSocket : MobileCalmHintButton)
        : CalmHint;

    /// <summary>
    /// The prompt line under the dot. On a handheld the key drops out ("E · OPEN DOOR"
    /// becomes "OPEN DOOR", "HOLD E" becomes "HOLD") and a glyph of the USE button
    /// stands in front of it (Figma Touch / Prompt).
    /// </summary>
    string DisplayPrompt(string text)
    {
        if (!FrontRoomsHandheld.Active || string.IsNullOrEmpty(text)) return text;
        if (text.StartsWith("E  ·  ")) return text.Substring(6);
        if (text.StartsWith("HOLD E  ·  ")) return "HOLD  ·  " + text.Substring(11);
        if (text.StartsWith("TAP E  ·  ")) return "TAP  ·  " + text.Substring(10);
        return text;
    }

    void EnsureMobileTouchLayer()
    {
        if (!FrontRoomsHandheld.Active || mobileTouch != null) return;
        mobileTouch = GetComponent<FrontRoomsTouchControls>();
        if (mobileTouch == null) mobileTouch = gameObject.AddComponent<FrontRoomsTouchControls>();
        if (GetComponent<FrontRoomsTouchControlsView>() == null) gameObject.AddComponent<FrontRoomsTouchControlsView>();
        mobileBackBridge = GetComponent<FrontRoomsMobileBackBridge>();
        if (mobileBackBridge == null) mobileBackBridge = gameObject.AddComponent<FrontRoomsMobileBackBridge>();
        mobileTouch.Host = this;
        if (!mobileTouchEventsBound)
        {
            mobileTouch.SettingsRowRequested += HandleMobileSettingsRow;
            mobileTouch.RestartCancelRequested += CancelMobileRestartConfirmation;
            mobileTouch.LookTapped += HandleMobileLookTap;
            FrontRoomsMobileBackBridge.BackPressed += HandleMobileBack;
            MapRunStarted += OnMobileMapRunStarted;
            mobileTouchEventsBound = true;
        }
        UpdateMobileTouchMenu();
    }

    // ------------------------------------------------------------ host data

    public int GameSettingCount => SettingsRows().Length;
    public string GameSettingSection(int index) => SettingsRows()[index].section;
    public string GameSettingLabel(int index) => SettingsRows()[index].label;

    public string GameSettingValue(int index)
    {
        var value = SettingsRows()[index].value();
        // The desktop names the key; on touch it is the USE button.
        if (value == "HOLD E") return "HOLD USE";
        if (value == "TAP E") return "TAP USE";
        return value;
    }

    public bool GameSettingStepped(int index) => SettingsRows()[index].label == "CAMERA MOTION";

    public void StepGameSetting(int index, int direction)
    {
        var rows = SettingsRows();
        if (index < 0 || index >= rows.Length) return;
        rows[index].step(direction);
        UpdateDisplaySettingsText();
    }

    public string PauseMeta =>
        MobileZoneName() + "     ·     " + Mathf.RoundToInt(elapsed) + " S     ·     TIER " + tier;

    public Camera BackdropCamera => cam;

    public FrontRoomsTouchCaught CaughtStats => new FrontRoomsTouchCaught
    {
        Seconds = Mathf.RoundToInt(elapsed),
        Zones = zonesVisited.Count,
        Tier = tier,
        Keys = keysTaken,
        DoorsBroken = relay == null ? 0 : relay.DoorsBroken,
        Where = SentenceCase(MobileZoneName())
    };

    string MobileZoneName()
    {
        if (map == null || playerRoot == null || inStartRooms) return "LEVEL 0 / THE LOBBY";
        return ZoneName(map.ZoneOf(map.CellOf(playerRoot.position)));
    }

    // "LEVEL 0 / THE MAZE" → "Level 0 / The maze" for the caught card's sentence.
    static string SentenceCase(string upper)
    {
        if (string.IsNullOrEmpty(upper)) return upper;
        var chars = upper.ToLowerInvariant().ToCharArray();
        var start = true;
        for (var i = 0; i < chars.Length; i++)
        {
            if (start && char.IsLetter(chars[i])) { chars[i] = char.ToUpperInvariant(chars[i]); start = false; }
            if (chars[i] == '/') start = true;
        }
        return new string(chars);
    }

    // ------------------------------------------------------------- menus

    void HandleMobileBack()
    {
        if (this == null || phase == Phase.Title || phase == Phase.Caught) return;
        if (mobileRestartConfirmOpen) { CancelMobileRestartConfirmation(); return; }
        if (displaySettingsOpen) { ToggleDisplaySettings(); return; }
        if (phase == Phase.Playing) SetPhase(Phase.Paused);
        else if (phase == Phase.Paused) SetPhase(Phase.Playing);
    }

    void RequestMobileRestartConfirmation()
    {
        if (phase != Phase.Paused || mobileRestartConfirmOpen) return;
        mobileRestartConfirmOpen = true;
        mobileTouch?.SetRestartConfirmation(true);
    }

    void CancelMobileRestartConfirmation()
    {
        if (!mobileRestartConfirmOpen) return;
        mobileRestartConfirmOpen = false;
        mobileTouch?.SetRestartConfirmation(false);
    }

    void ConfirmMobileRestart()
    {
        mobileRestartConfirmOpen = false;
        mobileTouch?.SetRestartConfirmation(false);
        restart = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void HandleMobileSettingsRow(int id, int direction)
    {
        if (phase != Phase.Paused || !displaySettingsOpen) return;
        if (id >= FrontRoomsTouchControls.TouchRowBase)
            FrontRoomsTouchControls.StepTouchRow((FrontRoomsTouchControls.TouchRow)(id - FrontRoomsTouchControls.TouchRowBase), direction);
        else
            StepGameSetting(id, direction);
    }

    /// <summary>A quick tap on the look side uses what is under the finger (a door): Apple HIG's direct touch.</summary>
    void HandleMobileLookTap(Vector2 viewport)
    {
        if (phase != Phase.Playing || map == null || cam == null) return;
        var ray = cam.ViewportPointToRay(new Vector3(viewport.x, viewport.y, 0f));
        if (!Physics.Raycast(ray, out var hit, Reach, ~0, QueryTriggerInteraction.Ignore)) return;
        var description = map.Describe(hit.collider, out var hold);
        if (hold || string.IsNullOrEmpty(description)) return;
        FrontRoomsMobileInteractionEvents.UsePressed();
        map.Use(hit.collider);
    }

    void UpdateMobileTouchMenu()
    {
        if (mobileTouch == null) return;
        if (phase != Phase.Paused && mobileRestartConfirmOpen)
        {
            mobileRestartConfirmOpen = false;
            mobileTouch.SetRestartConfirmation(false);
        }
        var state = phase == Phase.Title ? FrontRoomsTouchControls.MenuState.Title
            : phase == Phase.Playing ? FrontRoomsTouchControls.MenuState.Playing
            : phase == Phase.Paused ? (displaySettingsOpen ? FrontRoomsTouchControls.MenuState.Settings : FrontRoomsTouchControls.MenuState.Paused)
            : FrontRoomsTouchControls.MenuState.Caught;
        mobileTouch.SetMenuState(state);
        mobileTouch.SetWinded(winded);
        // The touch layer draws the pause and caught cards (with their motion); the HUD's text copies stay hidden.
        if (mobilePauseTitle != null) mobilePauseTitle.gameObject.SetActive(false);
        if (mobilePauseRows != null) mobilePauseRows.gameObject.SetActive(false);
        if (mobileCaughtTitle != null) mobileCaughtTitle.gameObject.SetActive(false);
        if (mobileCaughtStats != null) mobileCaughtStats.gameObject.SetActive(false);
    }

    // ------------------------------------------------------------ the prompt

    void EmitMobileGlassBeat(float progress)
    {
        progress = Mathf.Clamp01(progress);
        if (progress <= 0f || progress - mobileLastGlassBeat < .2f) return;
        mobileLastGlassBeat = progress;
        FrontRoomsMobileInteractionEvents.GlassBeat(progress);
    }

    void UpdateMobileTouchPrompt()
    {
        if (mobileTouch == null) return;
        var visible = phase == Phase.Playing && aimed != null && !string.IsNullOrEmpty(prompt);
        var locked = visible && prompt.IndexOf("LOCKED", System.StringComparison.OrdinalIgnoreCase) >= 0;
        var kind = !visible ? FrontRoomsTouchControls.UseKind.Hidden
            : locked ? FrontRoomsTouchControls.UseKind.Locked
            : aimedHold ? FrontRoomsTouchControls.UseKind.Hold
            : prompt.IndexOf("SHUT", System.StringComparison.OrdinalIgnoreCase) >= 0 ? FrontRoomsTouchControls.UseKind.Shut
            : prompt.IndexOf("TAKE", System.StringComparison.OrdinalIgnoreCase) >= 0 ? FrontRoomsTouchControls.UseKind.Take
            : FrontRoomsTouchControls.UseKind.Open;
        mobileTouch.SetUsePrompt(new FrontRoomsTouchControls.UsePrompt
        {
            Visible = visible,
            // A locked door still takes the press: it rattles, as E does on desktop.
            Interactable = visible,
            Hold = visible && aimedHold,
            Locked = locked,
            TapMode = FrontRoomsSettings.TapToBreak,
            Label = prompt ?? string.Empty,
            Progress = holdProgress,
            Kind = kind
        });
        if (winded && !mobileWasWinded) FrontRoomsMobileInteractionEvents.Winded();
        mobileWasWinded = winded;
        mobileTouch.SetWinded(winded);
        UpdateMobilePromptGlyph(visible, locked);
    }

    /// <summary>The mini USE glyph (or the hold ring glyph) in front of the prompt under the dot.</summary>
    void UpdateMobilePromptGlyph(bool visible, bool locked)
    {
        if (promptText == null) return;
        if (mobilePromptGlyph == null)
        {
            var go = new GameObject("Touch prompt glyph", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(promptText.transform.parent, false);
            mobilePromptGlyph = go.GetComponent<Image>();
            mobilePromptGlyph.raycastTarget = false;
            var rt = mobilePromptGlyph.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
            rt.sizeDelta = new Vector2(18f, 18f);
        }
        var show = visible && !locked && !string.IsNullOrEmpty(promptText.text);
        mobilePromptGlyph.enabled = show;
        if (!show) return;
        mobilePromptGlyph.sprite = FrontRoomsTouchSprites.GlyphSprite(aimedHold ? FrontRoomsTouchSprites.Glyph.Hold : FrontRoomsTouchSprites.Glyph.Use);
        mobilePromptGlyph.color = promptText.color;
        var p = promptText.rectTransform.anchoredPosition;
        mobilePromptGlyph.rectTransform.anchoredPosition = new Vector2(p.x - promptText.preferredWidth * .5f - 15f, p.y);
    }

    // ------------------------------------------------------------- haptics

    // Haptics come from the game's own events (never audio callbacks, AUDIO_CONTRACT), only near the player.
    void OnMobileMapRunStarted(FrontRoomsMapWorld world, FrontRoomsMapHunter hunter)
    {
        if (this == null) { MapRunStarted -= OnMobileMapRunStarted; return; }
        DetachMobileHaptics();
        mobileHapticMap = world;
        mobileHapticRelay = hunter;
        if (world != null)
        {
            world.DoorLatched += OnMobileDoorLatched;
            world.DoorLocked += OnMobileDoorLocked;
            world.GlassCracked += OnMobileGlassCracked;
        }
        if (hunter != null)
        {
            mobileRelayState = hunter.State;
            hunter.StateChanged += OnMobileRelayState;
        }
    }

    void DetachMobileHaptics()
    {
        if (mobileHapticMap != null)
        {
            mobileHapticMap.DoorLatched -= OnMobileDoorLatched;
            mobileHapticMap.DoorLocked -= OnMobileDoorLocked;
            mobileHapticMap.GlassCracked -= OnMobileGlassCracked;
        }
        if (mobileHapticRelay != null) mobileHapticRelay.StateChanged -= OnMobileRelayState;
        mobileHapticMap = null;
        mobileHapticRelay = null;
    }

    bool NearPlayer(Vector3 p) => playerRoot != null && Flat(p - playerRoot.position).sqrMagnitude <= DoorJoltRange * DoorJoltRange;

    void OnMobileDoorLatched(FrontRoomsMapWorld.Door door, Vector3 p)
    {
        if (this != null && NearPlayer(p)) FrontRoomsMobileInteractionEvents.DoorLatched();
    }

    void OnMobileDoorLocked(Vector3 p)
    {
        // Two firm ticks on the camera's two rattle jolts.
        if (this != null && NearPlayer(p)) FrontRoomsMobileInteractionEvents.DoorRattled(FrontRoomsShotTimings.Rattle.Jolt1, FrontRoomsShotTimings.Rattle.Jolt2);
    }

    void OnMobileGlassCracked(FrontRoomsMapWorld.Window window, int stage)
    {
        if (this != null && window != null && NearPlayer(window.position)) FrontRoomsMobileInteractionEvents.GlassCracked(stage);
    }

    void OnMobileRelayState(HunterState next)
    {
        if (this == null) return;
        // The lock-on: the Relay starts the chase.
        if (next == HunterState.Chase && mobileRelayState != HunterState.Chase) FrontRoomsMobileInteractionEvents.LockOn();
        mobileRelayState = next;
    }

    void OnDestroyMobile()
    {
        DetachMobileHaptics();
        MapRunStarted -= OnMobileMapRunStarted;
    }
}
