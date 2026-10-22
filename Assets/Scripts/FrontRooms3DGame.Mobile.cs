using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// The player's gait, quietest to loudest after Still (Red, 2026-10-08: "跑步-声音最大，正常行进-中等声音，
/// 慢慢走-几乎没有声音"). The touch stick makes all three (TOUCH_CONTROLS.md §2); desktop has walk and run.
/// </summary>
public enum FrontRoomsGait { Still, Slow, Walk, Run }

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
    // The broken door USE names (BROKEN), or null.
    Collider mobileBrokenAimed;

    const string MobileCalmHintStick = "Push past the ring to run: it hears every step. Stay near the centre to creep unheard.";
    const string MobileCalmHintButton = "Tap RUN to run: it hears every step. Stay near the centre to creep unheard.";

    /// <summary>The first-run hint: the touch gesture on a handheld, Shift on desktop.</summary>
    string CurrentCalmHint => FrontRoomsHandheld.Active
        ? (FrontRoomsSettings.TouchSprintSocket ? MobileCalmHintStick : MobileCalmHintButton)
        : CalmHint;

    // ------------------------------------------------------------- gaits

    /// <summary>
    /// A handheld walk step's noise radius. With hearing ×1.4 that is 12.6 m of walking path at tier 1, so a shut door
    /// between you (+9 m) all but hides it. The run keeps the desktop sprint's radius; the slow gait makes none.
    /// </summary>
    const float HandheldWalkNoiseRadius = 9f;

    /// <summary>The player's gait this frame, for the sound layer's footsteps (AUDIO_CONTRACT). Slow happens only on a handheld.</summary>
    public static FrontRoomsGait PlayerGait { get; private set; }

#if UNITY_EDITOR
    /// <summary>Each handheld walk noise (its radius), for the touch playtest.</summary>
    internal static event System.Action<float> HandheldNoiseEmitted;
#endif

    /// <summary>
    /// The gait from the sprint rule and the move's size. The touch stick sends one size per gait (slow 0.45, walk 1),
    /// so a handheld move under 0.7 is the slow creep; desktop keys and pads keep walk and run, as before.
    /// </summary>
    static FrontRoomsGait GaitOf(bool sprinting, float wishSqrMagnitude)
    {
        if (wishSqrMagnitude <= .01f) return FrontRoomsGait.Still;
        if (sprinting) return FrontRoomsGait.Run;
        return FrontRoomsHandheld.Active && wishSqrMagnitude < .49f ? FrontRoomsGait.Slow : FrontRoomsGait.Walk;
    }

    /// <summary>A handheld walk footstep reaches the Relay's ears. Desktop walking stays silent; the slow gait is silent everywhere.</summary>
    void HandheldStepNoise()
    {
        if (!FrontRoomsHandheld.Active || PlayerGait != FrontRoomsGait.Walk || relay == null || playerRoot == null) return;
#if UNITY_EDITOR
        // Through the bots' wrapper, like door, glass and sprint, so a baseline run can name the hunt it starts
        // (关卡设计调研, 2026-10-08). Outside a baseline it is relay.Noise.
        AutopilotNoise(AutoCauseOther, playerRoot.position, HandheldWalkNoiseRadius);
        HandheldNoiseEmitted?.Invoke(HandheldWalkNoiseRadius);
#else
        relay.Noise(playerRoot.position, HandheldWalkNoiseRadius);
#endif
    }

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
            mobileTouch.ReturnTitleRequested += ReturnToMobileTitle;
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
        if (this == null) return;
        // Every screen has a way back. The title is the root: Back leaves the game (Android's convention).
        if (phase == Phase.Title)
        {
            FrontRoomsMobileBackBridge.LeaveApp();
            return;
        }
        // Caught: Back is TRY AGAIN, after the chip's own 0.6 s guard.
        if (phase == Phase.Caught)
        {
            if (mobileTouch == null || !mobileTouch.TryAgainReady) return;
            FrontRoomsMobileInteractionEvents.MenuConfirmed();
            restart = true;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }
        if (mobileRestartConfirmOpen) { CancelMobileRestartConfirmation(); return; }
        if (displaySettingsOpen) { ToggleDisplaySettings(); return; }
        // C15: Back leaves a close-up; from Paused it resumes where the pause came from (never a raw SetPhase(Playing)).
        if (phase == Phase.Inspect) { if (FrontRoomsInspect.Age >= FrontRoomsShotTimings.Inspect.CancelAfter) EndInspect(false); return; }
        if (phase == Phase.Playing) SetPhase(Phase.Paused);
        else if (phase == Phase.Paused) ResumeFromPause();
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

    /// <summary>Returns to the authored title corridor from the mobile pause card.</summary>
    void ReturnToMobileTitle()
    {
        if (phase != Phase.Paused || mobileRestartConfirmOpen) return;
        // A title return is a fresh scene, rather than a run restart: leave the
        // static restart latch clear so Awake builds the normal title corridor.
        restart = false;
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

    /// <summary>
    /// A quick tap on the look side uses what is under the finger, a door or a key: Apple HIG's direct touch.
    /// A key (C14 on phones, 交互设计 2026-10-22) needs no exact hit: when the finger's own ray finds nothing
    /// to use, a key within KeyTapRadiusPoints of the tap is taken. A broken door answers a tap as it answers
    /// USE: it won't shut.
    /// </summary>
    void HandleMobileLookTap(Vector2 viewport)
    {
        if (phase != Phase.Playing || map == null || cam == null || playerRoot == null) return;
        // C15: no taps through a shot camera (it may sit 0.36 m from a leaf); a tap on a close-up target opens it through the gate.
        if (rig != null && rig.InShot) return;
        var ray = cam.ViewportPointToRay(new Vector3(viewport.x, viewport.y, 0f));
        if (FrontRoomsInspect.Find(ray, Reach, out var tapTarget) && CanShowInspect(tapTarget))
        {
            if (CanPressInspect()) pendingInspect = tapTarget;
            return;
        }
        // A10: the finger's ray never stops on the player's own body (looking down at a key on the floor).
        var key = TapKey(ray);
        var hitAny = AimCast(ray, out var hit);
        string description = null;
        var hold = false;
        if (key == null && hitAny) description = map.Describe(hit.collider, out hold);
        var usable = key != null || (!hold && !string.IsNullOrEmpty(description));
        // A broken door has no prompt, but a tap on it is the won't-shut try; a key near the finger comes first.
        if (!usable && (key = TapKeyNear(viewport)) == null
            && (!hitAny || map.Kind(hit.collider, playerRoot.position, out _, out _) != FrontRoomsMapWorld.AimKind.Broken)) return;
        FrontRoomsMobileInteractionEvents.UsePressed();
        // The same road as E (UpdateAim): a shot that takes USE swallows the tap, a key turn starts the unlock shot, a key is taken.
        if (rig == null || !rig.Consume(ShotInput.Use)) UseDoor(key != null ? key : hit.collider);
    }

    // A key's finger target (Apple HIG: 44 pt): a tap this close to one takes it when nothing usable is under the finger.
    const float KeyTapRadiusPoints = 22f;

    // A key under the finger: its trigger on the ray, nearer than anything solid (KeyOnRay), while keys are taken on purpose (C14).
    Collider TapKey(Ray ray) => map.KeyPickupNeedsE ? map.KeyOnRay(ray, Reach, playerRoot) : null;

    // Two rings of rays round the tap, at half and the whole of KeyTapRadiusPoints, the nearer ring first.
    Collider TapKeyNear(Vector2 viewport)
    {
        if (!map.KeyPickupNeedsE) return null;
        var points = FrontRoomsHandheld.ScreenPoints;
        for (var ring = 1; ring <= 2; ring++)
        for (var k = 0; k < 8; k++)
        {
            var a = k * Mathf.PI * .25f;
            var r = KeyTapRadiusPoints * ring * .5f;
            var v = viewport + new Vector2(Mathf.Cos(a) * r / Mathf.Max(1f, points.x), Mathf.Sin(a) * r / Mathf.Max(1f, points.y));
            var key = TapKey(cam.ViewportPointToRay(new Vector3(v.x, v.y, 0f)));
            if (key != null) return key;
        }
        return null;
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
            : phase == Phase.Playing || phase == Phase.Inspect ? FrontRoomsTouchControls.MenuState.Playing   // C13 interim: MenuState.Inspect lands with T1
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

    /// <summary>
    /// USE mirrors what the crosshair is on, its verb keyed on the map's AimKind (H4, 30_crosshair_grammar §6), never
    /// on the prompt's words. A broken door has no prompt, so E's aim (UpdateAim) passes it by: USE finds it on the
    /// same ray and says BROKEN, and a press is the won't-shut try (Red, 2026-10-22, 交互设计).
    /// </summary>
    void UpdateMobileTouchPrompt()
    {
        if (mobileTouch == null) return;
        var playing = phase == Phase.Playing && map != null && playerRoot != null;
        var target = playing ? aimed : null;
        // Nothing usable aimed and no close-up target: the base eye's ray, cast as UpdateAim casts it.
        if (playing && target == null && inspectAimed == null && cam != null)
        {
            var eye = rig != null ? rig.BaseEye : new Pose(cam.transform.position, cam.transform.rotation);
            if (AimCast(new Ray(eye.position, eye.rotation * Vector3.forward), out var hit)) target = hit.collider;
        }
        var aimKind = target != null ? map.Kind(target, playerRoot.position, out _, out _) : FrontRoomsMapWorld.AimKind.None;
        // Past E's aim only a broken door counts: anything else there has no prompt for a reason (glass beyond 1.2 m).
        if (aimed == null && aimKind != FrontRoomsMapWorld.AimKind.Broken) aimKind = FrontRoomsMapWorld.AimKind.None;
        mobileBrokenAimed = aimKind == FrontRoomsMapWorld.AimKind.Broken ? target : null;
        var kind = UseKindOf(aimKind);
        var visible = kind != FrontRoomsTouchControls.UseKind.Hidden;
        var refused = kind == FrontRoomsTouchControls.UseKind.Locked || kind == FrontRoomsTouchControls.UseKind.Broken;
        mobileTouch.SetUsePrompt(new FrontRoomsTouchControls.UsePrompt
        {
            Visible = visible,
            // A locked door still takes the press: it rattles, as E does on desktop. A broken one won't shut.
            Interactable = visible,
            Hold = visible && aimedHold,
            Locked = kind == FrontRoomsTouchControls.UseKind.Locked,
            TapMode = FrontRoomsSettings.TapToBreak,
            Label = prompt ?? string.Empty,
            Progress = holdProgress,
            Kind = kind
        });
        // USE on a broken door: E's road (UpdateAim) had nothing aimed, so the try is made here, as a tap makes it. A
        // shot running took the press there; a broken door that E's road aims at itself is tried there.
        if (mobileBrokenAimed != null && aimed == null && FrontRoomsInput.UseDown && (rig == null || !rig.InShot)) UseDoor(mobileBrokenAimed);
        if (winded && !mobileWasWinded) FrontRoomsMobileInteractionEvents.Winded();
        mobileWasWinded = winded;
        mobileTouch.SetWinded(winded);
        // A shot fades the HUD (ApplyGameplayHudAlpha); USE follows, so it never shows a verb a press wouldn't do.
        mobileTouch.SetTakeover(rig != null ? rig.HudFade : 0f);
        UpdateMobilePromptGlyph(visible, refused);
    }

    /// <summary>USE's verb for the map's kind (H4). LockedHaveKey stays OPEN until the A22 strings land (phone UNLOCK).</summary>
    static FrontRoomsTouchControls.UseKind UseKindOf(FrontRoomsMapWorld.AimKind kind)
    {
        switch (kind)
        {
            case FrontRoomsMapWorld.AimKind.Open:
            case FrontRoomsMapWorld.AimKind.LockedHaveKey: return FrontRoomsTouchControls.UseKind.Open;
            case FrontRoomsMapWorld.AimKind.Shut: return FrontRoomsTouchControls.UseKind.Shut;
            case FrontRoomsMapWorld.AimKind.TakeKey: return FrontRoomsTouchControls.UseKind.Take;
            case FrontRoomsMapWorld.AimKind.Locked: return FrontRoomsTouchControls.UseKind.Locked;
            case FrontRoomsMapWorld.AimKind.Broken: return FrontRoomsTouchControls.UseKind.Broken;
            case FrontRoomsMapWorld.AimKind.Glass:
            case FrontRoomsMapWorld.AimKind.GlassTap: return FrontRoomsTouchControls.UseKind.Hold;
            default: return FrontRoomsTouchControls.UseKind.Hidden;
        }
    }

    /// <summary>The mini USE glyph (or the hold ring glyph) in front of the prompt under the dot; none for a refusal.</summary>
    void UpdateMobilePromptGlyph(bool visible, bool refused)
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
        var show = visible && !refused && !string.IsNullOrEmpty(promptText.text);
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
            world.DoorBrokenTried += OnMobileDoorBrokenTried;
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
            mobileHapticMap.DoorBrokenTried -= OnMobileDoorBrokenTried;
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

    void OnMobileDoorBrokenTried(FrontRoomsMapWorld.Door door, Vector3 p)
    {
        if (this == null || !NearPlayer(p)) return;
        // One soft knock as the swing meets the frame (the map's guard already dropped a repeat inside 0.55 s).
        FrontRoomsMobileInteractionEvents.DoorBrokenTried(FrontRoomsTouchControls.BrokenKnockSeconds);
        // USE swings only for the door it names: a tap may try another broken door.
        if (mobileTouch != null && mobileBrokenAimed != null && mobileHapticMap.DoorOf(mobileBrokenAimed) == door) mobileTouch.NoteBrokenTried();
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
